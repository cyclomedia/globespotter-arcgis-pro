/*
 * Integration in ArcMap for Cycloramas
 * Copyright (c) 2015 - 2016, CycloMedia, All rights reserved.
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3.0 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using GlobeSpotterAPI;
using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Utilities;

using ApiMeasurementPoint = GlobeSpotterAPI.MeasurementPoint;
using Geometry = ArcGIS.Core.Geometry.Geometry;
using Point = System.Windows.Point;
using Polygon = ArcGIS.Core.Geometry.Polygon;

namespace GlobeSpotterArcGISPro.Overlays.Measurement
{
  public class MeasurementPoint : Dictionary<string, MeasurementObservation>, IDisposable
  {
    #region Members

    private readonly Measurement _measurement;
    private readonly ConstantsViewer _constants;
    private readonly CultureInfo _ci;

    private int _index;
    private bool _added;
    private bool _open;
    private IDisposable _disposeText;

    #endregion

    #region Properties

    public int IntId { private get; set; }

    public int PointId { get; }

    public int M => (_index != 0) ? _index : (_index = _measurement.GetMeasurementPointIndex(PointId));

    public double Z => Point?.Z ?? 0.0;

    public MapPoint Point { get; private set; }

    public bool NotCreated => (Point == null);

    public bool IsFirstNumber => (IntId == 1);

    public bool IsLastNumber => ((_measurement == null) || (IntId == _measurement.Count));

    public MeasurementPoint PreviousPoint
      => ((_measurement != null) && (!IsFirstNumber)) ? _measurement.GetPointByNr(IntId - 2) : null;

    public MeasurementPoint NextPoint
      => ((_measurement != null) && (!IsLastNumber)) ? _measurement.GetPointByNr(IntId) : null;

    #endregion

    #region Constructor

    public MeasurementPoint(int pointId, int intId, Measurement measurement)
    {
      _measurement = measurement;
      _index = 0;
      IntId = intId;
      Point = null;
      PointId = pointId;
      _added = false;
      _open = false;
      _constants = ConstantsViewer.Instance;
      _ci = CultureInfo.InvariantCulture;
    }

    #endregion

    #region Functions

    public async Task UpdateObservationAsync(string imageId, double x, double y, double z)
    {
      MapPoint point = await CoordSystemUtils.CycloramaToMapPointAsync(x, y, z);

      if (ContainsKey(imageId))
      {
        this[imageId].Point = point;
        this[imageId].ImageId = imageId;
        await this[imageId].RedrawObservationAsync();
      }
      else
      {
        MeasurementObservation measurementObservation = new MeasurementObservation(this, imageId, point);
        Add(imageId, measurementObservation);
        await measurementObservation.RedrawObservationAsync();
      }
    }

    public void RemoveObservation(string imageId)
    {
      if (ContainsKey(imageId))
      {
        this[imageId].Dispose();
        Remove(imageId);
      }
    }

    public bool CheckSelected()
    {
      // todo: make this function is working
      // todo: =============================
//      IEditor3 editor = ArcUtils.Editor;
//      var sketch = editor as IEditSketch3;
      bool result = false;

//      if (sketch != null)
//      {
//        result = sketch.IsVertexSelected(0, (_intId - 1));
//      }

      return result;
    }

    public void Closed()
    {
      _open = false;
    }

    public void Opened()
    {
      _open = true;
    }

    public void OpenPoint()
    {
      _open = true;
      _measurement?.OpenPoint(PointId);
    }

    public void ClosePoint()
    {
      if (_open)
      {
        _open = false;
        _measurement?.ClosePoint(PointId);
      }
    }

    public void Dispose()
    {
      MapViewCameraChangedEvent.Unsubscribe(OnMapViewCameraChanged);
      _disposeText?.Dispose();
    }

    public async Task UpdatePointAsync(PointMeasurementData measurementData, int index)
    {
      _index = index;
      bool notCreated = NotCreated;

      ApiMeasurementPoint measurementPoint = measurementData.measurementPoint;
      double x = measurementPoint.x;
      double y = measurementPoint.y;
      double z = measurementPoint.z;
      Point = await CoordSystemUtils.CycloramaToMapPointAsync(x, y, z);

      if (!notCreated)
      {
        MapViewCameraChangedEvent.Unsubscribe(OnMapViewCameraChanged);
      }

      MapViewCameraChangedEvent.Subscribe(OnMapViewCameraChanged);
      MapView thisView = MapView.Active;
      Geometry geometry = await thisView.GetCurrentSketchAsync();

      if (geometry != null)
      {
        var ptColl = await _measurement.ToPointCollectionAsync(geometry);
        int nrPoints = _measurement.PointNr;

        if ((ptColl != null) && _measurement.IsSketch)
        {
          if (IntId <= nrPoints)
          {
            MapPoint pointC = ptColl[IntId - 1];

            if (!IsSame(pointC))
            {
              await QueuedTask.Run(() =>
              {
                MapPoint point = MapPointBuilder.CreateMapPoint(Point.X, Point.Y, Point.Z, Point.M,
                  geometry.SpatialReference);

                if (_measurement.IsPointMeasurement)
                {
                  thisView.SetCurrentSketchAsync(point);
                }
                else
                {
                  ptColl[IntId - 1] = point;

                  if ((IntId == 1) && ((nrPoints + 1) == ptColl.Count))
                  {
                    ptColl[ptColl.Count - 1] = point;
                  }

                  if (_measurement.IsGeometryType(GeometryType.Polygon))
                  {
                    geometry = PolygonBuilder.CreatePolygon(ptColl, geometry.SpatialReference);
                  }
                  else if (_measurement.IsGeometryType(GeometryType.Polyline))
                  {
                    geometry = PolylineBuilder.CreatePolyline(ptColl, geometry.SpatialReference);
                  }

                  thisView.SetCurrentSketchAsync(geometry);
                }
              });
            }
          }
          else
          {
            await QueuedTask.Run(() =>
            {
              MapPoint point = MapPointBuilder.CreateMapPoint(Point.X, Point.Y, Point.Z, Point.M,
                geometry.SpatialReference);
              int nrPoints2 = ptColl.Count;

              switch (nrPoints2)
              {
                case 0:
                  ptColl.Add(point);

                  if (geometry is Polygon)
                  {
                    ptColl.Add(point);
                  }

                  break;
                case 1:
                  ptColl.Add(point);
                  break;
                default:
                  if (IntId <= (nrPoints + 1))
                  {
                    if ((IntId - 1) != nrPoints2)
                    {
                      ptColl.Insert((IntId - 1), point);
                    }
                    else
                    {
                      ptColl.Add(point);
                    }
                  }

                  break;
              }

              if (_measurement.IsGeometryType(GeometryType.Polygon))
              {
                geometry = PolygonBuilder.CreatePolygon(ptColl, geometry.SpatialReference);
              }
              else if (_measurement.IsGeometryType(GeometryType.Polyline))
              {
                geometry = PolylineBuilder.CreatePolyline(ptColl, geometry.SpatialReference);
              }

              thisView.SetCurrentSketchAsync(geometry);
            });
          }
        }
        else
        {
          if (geometry is MapPoint)
          {
            await QueuedTask.Run(() =>
            {
              if (geometry.IsEmpty)
              {
                if ((!double.IsNaN(Point.X)) && (!double.IsNaN(Point.Y)))
                {
                  if (!_added)
                  {
                    _added = true;
                    // todo: Add things with z offset
                    // todo: ========================
                    MapPoint point = MapPointBuilder.CreateMapPoint(Point.X, Point.Y, Point.Z, _index);
                    thisView.SetCurrentSketchAsync(point);
                    _added = false;
                  }
                }
              }
              else
              {
                var pointC = geometry as MapPoint;

                if (!IsSame(pointC))
                {
                  if ((!double.IsNaN(Point.X)) && (!double.IsNaN(Point.Y)))
                  {
                    MapPoint point = MapPointBuilder.CreateMapPoint(Point.X, Point.Y, Point.Z, _index);
                    thisView.SetCurrentSketchAsync(point);
                  }
                }
              }
            });
          }
        }
      }

      await RedrawPointAsync();
    }

    public bool IsSame(MapPoint point)
    {
      return IsSame(point, (Point != null) && (!Point.IsEmpty) && (!double.IsNaN(Point.Z)));
    }

    public bool IsSame(MapPoint point, bool includeZ)
    {
      const double distance = 0.01;
      return InsideDistance(point, distance, includeZ);
    }

    private bool InsideDistance(MapPoint point, double dinstance, bool includeZ)
    {
      return ((Point != null) && (point != null) && (!Point.IsEmpty) && (!point.IsEmpty)) &&
             ((Math.Abs(Point.X - point.X) < dinstance) && (Math.Abs(Point.Y - point.Y) < dinstance) &&
              ((!includeZ) || (Math.Abs(Point.Z - point.Z) < dinstance)));
    }

    public async Task RedrawPointAsync()
    {
      await QueuedTask.Run(() =>
      {
        GlobeSpotter globeSpotter = GlobeSpotter.Current;
        _disposeText?.Dispose();

        if (globeSpotter.InsideScale())
        {
          if ((Point != null) && (_measurement?.IsOpen ?? false) && (_measurement?.DrawPoint ?? false) &&
              (!double.IsNaN(Point.X)) && (!double.IsNaN(Point.Y)))
          {
            MapView thisView = MapView.Active;
            Point winPoint = thisView.MapToScreen(Point);

            double fontSize = _constants.MeasurementFontSize;
            CIMColor color = ColorFactory.Green;
            //            CIMSymbol textSymbol = SymbolFactory.ConstructTextSymbol(color, fontSize);
            
            CIMMarker marker = SymbolFactory.ConstructMarker(color, fontSize);// .ConstructPointSymbol();
            CIMSymbol textSymbol = SymbolFactory.ConstructPointSymbol(marker);

            //            CIMCharacterMarker marker = new CIMCharacterMarker();

            //            CIMTextSymbol textSymbol = new CIMTextSymbol {FontFamilyName = "Ariel"};
            //            textSymbol.SetSize(fontSize);
            CIMSymbolReference textSymbolReference = textSymbol.MakeSymbolReference();

            double pointSize = _constants.MeasurementPointSize;
            double pointSizePoint = (pointSize*3)/4;
            Point winPointText = new Point {X = winPoint.X + pointSizePoint, Y = winPoint.Y + pointSizePoint};

            // todo: draw the text to the map
            // todo: ========================
            string text = _index.ToString(_ci);
            MapPoint pointText = thisView.ScreenToMap(winPointText);
            //_disposeText = thisView.AddOverlay(pointText, textSymbolReference);
          }
        }
      });
    }

    #endregion

    #region Event handlers

    private async void OnMapViewCameraChanged(MapViewCameraChangedEventArgs args)
    {
      await RedrawPointAsync();
    }

    #endregion
  }
}
