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
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GlobeSpotterAPI;
using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Configuration.Remote.GlobeSpotter;
using GlobeSpotterArcGISPro.VectorLayers;

using MySpatialReference = GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference.SpatialReference;
using ApiMeasurementPoint = GlobeSpotterAPI.MeasurementPoint;

namespace GlobeSpotterArcGISPro.Overlays.Measurement
{
  #region Delegates

  public delegate void MeasurementPointDelegate(int entityId, int pointId);

  #endregion

  public class Measurement : SortedDictionary<int, MeasurementPoint>, IDisposable
  {
    #region Events

    public event MeasurementPointDelegate PointOpened;
    public event MeasurementPointDelegate PointClosed;

    #endregion

    #region Members

    private readonly GeometryType _geometryType;
    private readonly int _entityId;
    private readonly Settings _settings;
    private readonly MeasurementList _measurementList;
    private readonly API _api;

    private bool _updateMeasurement;

    #endregion

    #region Properties

    public VectorLayer VectorLayer { get; set; }

    public bool DrawPoint { get; private set; }

    public int PointNr { get; private set; }

    public long? ObjectId { get; set; }

    public bool IsPointMeasurement => (_geometryType == GeometryType.Point);

    public bool IsSketch => (_measurementList.Sketch == this);

    public bool IsOpen => (_measurementList.Open == this);

    #endregion

    #region Constructor

    public Measurement(int entityId, string entityType, bool drawPoint, API api)
    {
      GlobeSpotter globeSpotter = GlobeSpotter.Current;
      _measurementList = globeSpotter.MeasurementList;

      _api = api;
      _settings = Settings.Instance;
      _entityId = entityId;
      DrawPoint = drawPoint;
      _updateMeasurement = false;

      switch (entityType)
      {
        case "pointMeasurement":
          _geometryType = GeometryType.Point;
          break;
        case "surfaceMeasurement":
          _geometryType = GeometryType.Polygon;
          break;
        case "lineMeasurement":
          _geometryType = GeometryType.Polyline;
          break;
        default:
          _geometryType = GeometryType.Unknown;
          break;
      }
    }

    #endregion

    #region Functions

    public void Dispose()
    {
      foreach (var element in this)
      {
        MeasurementPoint measurementPoint = element.Value;
        measurementPoint.Dispose();
      }

      _measurementList.Open = IsOpen ? null : _measurementList.Open;
      _measurementList.Sketch = IsSketch ? null : _measurementList.Sketch;

      if (_measurementList.ContainsKey(_entityId))
      {
        _measurementList.Remove(_entityId);
      }
    }

    public bool IsGeometryType(GeometryType geometryType)
    {
      return (_geometryType == geometryType);
    }

    public MeasurementPoint GetPointByNr(int nr)
    {
      return Values.ElementAt(nr);
    }

    public void Close()
    {
      _measurementList.Open = null;
      DrawPoint = true;
    }

    public void Open()
    {
      _measurementList.Open = this;
    }

    public void SetSketch()
    {
      _measurementList.Sketch = this;
    }

    public void AddPoint(int pointId)
    {
      Add(pointId, new MeasurementPoint(pointId, (Count + 1), this));
    }

    public void OpenPoint()
    {
      if (Values.Count == 1)
      {
        MeasurementPoint measurementPoint = GetPointByNr(0);
        measurementPoint.OpenPoint();
      }
    }

    public void OpenPoint(int pointId)
    {
      if (GlobeSpotterConfiguration.MeasurePermissions)
      {
        _api?.OpenMeasurementPoint(_entityId, pointId);
        PointOpened?.Invoke(_entityId, pointId);
      }
    }

    public ApiMeasurementPoint PointData(int pointId)
    {
      PointMeasurementData pointMeasurementData = _api.GetMeasurementPointData(_entityId, pointId);
      return pointMeasurementData.measurementPoint;
    }

    public void ClosePoint(int pointId)
    {
      if (GlobeSpotterConfiguration.MeasurePermissions)
      {
        _api?.CloseMeasurementPoint(_entityId, pointId);
        PointClosed?.Invoke(_entityId, pointId);
      }
    }

    public MeasurementPoint GetPoint(MapPoint point)
    {
      return Values.Aggregate<MeasurementPoint, MeasurementPoint>
        (null, (current, value) => value.IsSame(point) ? value : current);
    }

    public MeasurementPoint GetPoint(MapPoint point, bool includeZ)
    {
      return Values.Aggregate<MeasurementPoint, MeasurementPoint>
        (null, (current, value) => value.IsSame(point, includeZ) ? value : current);
    }

    public async Task UpdatePointAsync(int pointId, PointMeasurementData measurementData, int index)
    {
      if (!ContainsKey(pointId))
      {
        AddPoint(pointId);
      }

      if (ContainsKey(pointId))
      {
        MeasurementPoint measurementPoint = this[pointId];
        await measurementPoint.UpdatePointAsync(measurementData, index);
      }
    }

    public void CloseMeasurement()
    {
      if (IsOpen && GlobeSpotterConfiguration.MeasurePermissions)
      {
        _measurementList.Open = null;
        _api?.CloseMeasurement(_entityId);
      }
    }

    public void DisableMeasurementSeries()
    {
      _measurementList.DisableMeasurementSeries();
    }

    public void EnableMeasurementSeries()
    {
      _measurementList.EnableMeasurementSeries(_entityId);
    }

    public void OpenMeasurement()
    {
      if ((!IsOpen) && GlobeSpotterConfiguration.MeasurePermissions)
      {
        _measurementList.Open?.CloseMeasurement();
        _measurementList.Open = this;
        _measurementList.OpenMeasurement(_entityId);
      }

      if (IsPointMeasurement && GlobeSpotterConfiguration.MeasurePermissions)
      {
        _api?.SetFocusEntity(_entityId);
        _measurementList.AddMeasurementPoint(_entityId);
      }
    }

    public void RemoveMeasurement()
    {
      if (IsOpen)
      {
        CloseMeasurement();
      }

      if (GlobeSpotterConfiguration.MeasurePermissions)
      {
        _api?.RemoveEntity(_entityId);
      }

      Dispose();
    }

    public void RemovePoint(int pointId)
    {
      MeasurementPoint measurementPoint = this[pointId];
      measurementPoint.Dispose();
      Remove(pointId);

      for (int i = 0; i < Count; i++)
      {
        MeasurementPoint msPoint = GetPointByNr(i);
        msPoint.IntId = i + 1;
      }
    }

    public bool CheckSelectedVertex()
    {
      bool opened = false;

      foreach (var value in Values)
      {
        bool selected = value.CheckSelected();

        if (selected)
        {
          if (!opened)
          {
            value.OpenPoint();
            opened = true;
          }
        }
        else
        {
          value.ClosePoint();
        }
      }

      return (Values.Count != 0);
    }

    public async Task<List<MapPoint>> ToPointCollectionAsync(Geometry geometry)
    {
      List<MapPoint> result = null;
      PointNr = 0;

      if (geometry != null)
      {
        result = new List<MapPoint>();
        GeometryType geometryType = geometry.GeometryType;

        switch (geometryType)
        {
          case GeometryType.Point:
            if ((!geometry.IsEmpty) && IsPointMeasurement)
            {
              MapPoint mapPoint = geometry as MapPoint;

              if (mapPoint != null)
              {
                result.Add(await AddZOffsetAsync(mapPoint));
              }
            }

            break;
          case GeometryType.Polygon:
          case GeometryType.Polyline:
            Multipart multipart = geometry as Multipart;

            if (multipart != null)
            {
              ReadOnlyPointCollection points = multipart.Points;
              IEnumerator<MapPoint> enumPoints = points.GetEnumerator();

              while (enumPoints.MoveNext())
              {
                MapPoint mapPointPart = enumPoints.Current;
                result.Add(await AddZOffsetAsync(mapPointPart));
              }
            }

            break;
        }

        PointNr = result.Count;

        if ((PointNr >= 2) && (geometryType == GeometryType.Polygon))
        {
          MapPoint point1 = result[0];
          MapPoint point2 = result[PointNr - 1];
          PointNr = point1.IsEqual(point2) ? (PointNr - 1) : PointNr;
        }
      }

      return result;
    }

    private async Task<MapPoint> AddZOffsetAsync(MapPoint mapPoint)
    {
      return await QueuedTask.Run(async () => mapPoint.HasZ
        ? MapPointBuilder.CreateMapPoint(mapPoint.X, mapPoint.Y,
          mapPoint.Z + ((VectorLayer != null) ? await VectorLayer.GetOffsetZAsync() : 0),
          mapPoint.SpatialReference)
        : MapPointBuilder.CreateMapPoint(mapPoint.X, mapPoint.Y, mapPoint.SpatialReference));
    }

    public int GetMeasurementPointIndex(int pointId)
    {
      return GlobeSpotterConfiguration.MeasurePermissions ? (_api?.GetMeasurementPointIndex(_entityId, pointId) ?? 0) : 0;
    }

    public async Task UpdateMeasurementPointsAsync(Geometry geometry)
    {
      if ((geometry != null) && (!_updateMeasurement))
      {
        _updateMeasurement = true;
        List<MapPoint> ptColl = await ToPointCollectionAsync(geometry);

        if (ptColl != null)
        {
          int msPoints = Count;
          var toRemove = new Dictionary<MeasurementPoint, bool>();
          var toAdd = new List<MapPoint>();
          bool toRemoveFrom = false;

          for (int i = 0; i < msPoints; i++)
          {
            MeasurementPoint measurementPoint = GetPointByNr(i);

            if (!measurementPoint.NotCreated)
            {
              toRemove.Add(measurementPoint, true);
            }
          }

          for (int j = 0; j < PointNr; j++)
          {
            MapPoint point = ptColl[j];
            var measurementPoint = GetPoint(point);

            if (measurementPoint == null)
            {
              toAdd.Add(point);
              toRemoveFrom = true;
            }
            else
            {
              if (!toRemoveFrom)
              {
                if (toRemove.ContainsKey(measurementPoint))
                {
                  toRemove[measurementPoint] = false;
                }
              }
              else
              {
                toAdd.Add(point);
              }
            }
          }

          if (toRemove.Aggregate(false, (current, remove) => remove.Value || current) || (toAdd.Count >= 1))
          {
            if (!IsPointMeasurement)
            {
              DisableMeasurementSeries();
            }

            foreach (var elem in toRemove)
            {
              if (elem.Value && GlobeSpotterConfiguration.MeasurePermissions)
              {
                MeasurementPoint msPoint = elem.Key;
                int pointId = msPoint.PointId;
                _api?.RemoveMeasurementPoint(_entityId, pointId);
              }
            }

            foreach (var point in toAdd)
            {
              MapView mapView = MapView.Active;
              Map map = mapView?.Map;
              SpatialReference mapSpatRef = map?.SpatialReference;

              MySpatialReference myCyclSpatRef = _settings.CycloramaViewerCoordinateSystem;
              SpatialReference cyclSpatRef = (myCyclSpatRef == null)
                ? mapSpatRef
                : (myCyclSpatRef.ArcGisSpatialReference ?? (await myCyclSpatRef.CreateArcGisSpatialReferenceAsync()));
              SpatialReference layerSpatRef = point.SpatialReference;
              MapPoint copyGsPoint = null;

              await QueuedTask.Run(() =>
              {
                ProjectionTransformation projection = ProjectionTransformation.Create(layerSpatRef, cyclSpatRef);
                copyGsPoint = GeometryEngine.ProjectEx(point, projection) as MapPoint;
              });

              if ((GlobeSpotterConfiguration.MeasurePermissions) && (copyGsPoint != null))
              {
                var point3D = new Point3D(copyGsPoint.X, copyGsPoint.Y, copyGsPoint.Z);
                _api?.CreateMeasurementPoint(_entityId, point3D);
              }
            }

            if (!IsPointMeasurement)
            {
              EnableMeasurementSeries();
            }
          }
        }

        _updateMeasurement = false;
      }
    }

    #endregion
  }
}
