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
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using GlobeSpotterArcGISPro.AddIns.Modules;

using Color = System.Drawing.Color;
using Point = System.Windows.Point;

namespace GlobeSpotterArcGISPro.Overlays.Measurement
{
  public class MeasurementObservation: IDisposable
  {
    #region Constants

    private const double InnerLineSize = 0.75;
    private const double OuterLineSize = 1.25;

    #endregion

    #region Members

    private readonly MeasurementPoint _measurementPoint;

    private IDisposable _disposeInnerLine;
    private IDisposable _disposeOuterLine;

    #endregion

    #region Properties

    public string ImageId { private get; set; }
    public MapPoint Point { private get; set; }

    #endregion

    #region Constructor

    public MeasurementObservation(MeasurementPoint measurementPoint, string imageId, MapPoint observationPoint)
    {
      _measurementPoint = measurementPoint;
      ImageId = imageId;
      Point = observationPoint;
      MapViewCameraChangedEvent.Subscribe(OnMapViewCameraChanged);
    }

    #endregion

    #region Functions

    public async void Dispose()
    {
      MapViewCameraChangedEvent.Unsubscribe(OnMapViewCameraChanged);
      await RedrawObservationAsync();
    }

    public async Task RedrawObservationAsync()
    {
      await QueuedTask.Run(() =>
      {
        GlobeSpotter globeSpotter = GlobeSpotter.Current;

        if (globeSpotter.InsideScale())
        {
          MapView thisView = MapView.Active;
          MapPoint measPoint = _measurementPoint.Point;
          Point winMeasPoint = thisView.MapToScreen(measPoint);
          Point winObsPoint = thisView.MapToScreen(Point);

          double xdir = (winMeasPoint.X - winObsPoint.X) / 2;
          double ydir = (winMeasPoint.Y - winObsPoint.Y) / 2;
          Point point1 = new Point(winObsPoint.X + xdir, winObsPoint.Y + ydir);
          Point point2 = new Point(winObsPoint.X, winObsPoint.Y);
          MapPoint mapPoint1 = thisView.ScreenToMap(point1);
          MapPoint mapPoint2 = thisView.ScreenToMap(point2);

          IList<MapPoint> linePointList = new List<MapPoint>();
          linePointList.Add(mapPoint1);
          linePointList.Add(mapPoint2);
          Polyline polyline = PolylineBuilder.CreatePolyline(linePointList);

          ViewerList viewerList = globeSpotter.ViewerList;
          ICollection<Viewer> viewers = viewerList.Viewers;
          Viewer thisViewer = viewers.Aggregate<Viewer, Viewer>(null,
            (current, viewer) => (viewer.ImageId == ImageId) ? viewer : current);

          Color outerColorLine = thisViewer?.Color ?? Color.DarkGray;
          CIMColor cimOuterColorLine = ColorFactory.CreateColor(outerColorLine);
          CIMLineSymbol cimOuterLineSymbol = SymbolFactory.DefaultLineSymbol;
          cimOuterLineSymbol.SetColor(cimOuterColorLine);
          cimOuterLineSymbol.SetSize(OuterLineSize);
          CIMSymbolReference cimOuterLineSymbolRef = cimOuterLineSymbol.MakeSymbolReference();
          _disposeOuterLine = thisView.AddOverlay(polyline, cimOuterLineSymbolRef);

          Color innerColorLine = Color.LightGray;
          CIMColor cimInnerColorLine = ColorFactory.CreateColor(innerColorLine);
          CIMLineSymbol cimInnerLineSymbol = SymbolFactory.DefaultLineSymbol;
          cimInnerLineSymbol.SetColor(cimInnerColorLine);
          cimInnerLineSymbol.SetSize(InnerLineSize);
          CIMSymbolReference cimInnerLineSymbolRef = cimInnerLineSymbol.MakeSymbolReference();
          _disposeInnerLine = thisView.AddOverlay(polyline, cimInnerLineSymbolRef);
        }
        else
        {
          _disposeInnerLine?.Dispose();
          _disposeOuterLine?.Dispose();
        }
      });
    }

    #endregion

    #region Event handlers

    private async void OnMapViewCameraChanged(MapViewCameraChangedEventArgs args)
    {
      await RedrawObservationAsync();
    }

    #endregion
  }
}
