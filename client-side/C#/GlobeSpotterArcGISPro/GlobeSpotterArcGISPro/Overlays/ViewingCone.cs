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
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using GlobeSpotterAPI;
using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.Configuration.File;

using MySpatialReference = GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference.SpatialReference;
using WinPoint = System.Windows.Point;

namespace GlobeSpotterArcGISPro.Overlays
{
  public class ViewingCone
  {
    #region Constants

    private const double BorderSizeArrow = 1.5;
    private const double BorderSizeBlinkingArrow = 2.5;
    private const double ArrowSize = 64.0;
    private const byte BlinkAlpha = 0;
    private const byte NormalAlpha = 192;
    private const int BlinkTime = 200;

    #endregion

    #region Members

    private MapPoint _mapPoint;
    private Color _color;
    private double _angle;
    private double _hFov;
    private bool _blinking;
    private Timer _blinkTimer;
    private bool _active;
    private bool _isInitialized;
    private IDisposable _disposePolygon;
    private IDisposable _disposePolyLine;

    #endregion Members

    #region Functions

    protected async Task InitializeAsync(RecordingLocation location, double angle, double hFov, Color color)
    {
      _angle = angle;
      _hFov = hFov;
      _color = color;
      _blinking = false;
      _active = true;
      _isInitialized = true;

      double x = location.X;
      double y = location.Y;
      Settings settings = Settings.Instance;
      MySpatialReference spatRel = settings.CycloramaViewerCoordinateSystem;

      await QueuedTask.Run(() =>
      {
        Map map = MapView.Active?.Map;
        SpatialReference mapSpatialReference = map?.SpatialReference;
        SpatialReference spatialReference = spatRel?.ArcGisSpatialReference ?? mapSpatialReference;
        MapPoint point = MapPointBuilder.CreateMapPoint(x, y, spatialReference);

        if ((mapSpatialReference != null) && (spatialReference.Wkid != mapSpatialReference.Wkid))
        {
          ProjectionTransformation projection = ProjectionTransformation.Create(spatialReference, mapSpatialReference);
          _mapPoint = GeometryEngine.ProjectEx(point, projection) as MapPoint;
        }
        else
        {
          _mapPoint = (MapPoint) point.Clone();
        }
      });

      MapViewCameraChangedEvent.Subscribe(OnMapViewCameraChanged);
      await RedrawConeAsync();
    }

    protected void Dispose()
    {
      _disposePolygon?.Dispose();
      _disposePolyLine?.Dispose();

      if (_isInitialized)
      {
        MapViewCameraChangedEvent.Unsubscribe(OnMapViewCameraChanged);
        _isInitialized = false;
      }
    }

    public async Task UpdateAsync(double angle, double hFov)
    {
      if (_isInitialized)
      {
        const double epsilon = 1.0;
        bool update = (!(Math.Abs(_angle - angle) < epsilon)) || (!(Math.Abs(_hFov - hFov) < epsilon));

        if (update)
        {
          _hFov = hFov;
          _angle = angle;
          await RedrawConeAsync();
        }
      }
    }

    protected async Task SetActiveAsync(bool active)
    {
      if (_isInitialized)
      {
        _blinking = active;
        _active = active;

        if (active)
        {
          MapViewCameraChangedEvent.Unsubscribe(OnMapViewCameraChanged);
          MapViewCameraChangedEvent.Subscribe(OnMapViewCameraChanged);
        }

        await RedrawConeAsync();
      }
    }

    private async Task RedrawConeAsync()
    {
      await QueuedTask.Run(() =>
      {
        GlobeSpotter globeSpotter = GlobeSpotter.Current;

        if ((globeSpotter.InsideScale()) && (!_mapPoint.IsEmpty))
        {
          MapView thisView = MapView.Active;
          WinPoint point = thisView.MapToScreen(_mapPoint);

          double angleh = (_hFov * Math.PI) / 360;
          double angle = (((270 + _angle) % 360) * Math.PI) / 180;
          double angle1 = angle - angleh;
          double angle2 = angle + angleh;
          double x = point.X;
          double y = point.Y;
          double size = ArrowSize / 2;

          WinPoint screenPoint1 = new WinPoint((x + (size * Math.Cos(angle1))), (y + (size * Math.Sin(angle1))));
          WinPoint screenPoint2 = new WinPoint((x + (size * Math.Cos(angle2))), (y + (size * Math.Sin(angle2))));
          MapPoint point1 = thisView.ScreenToMap(screenPoint1);
          MapPoint point2 = thisView.ScreenToMap(screenPoint2);

          IList<MapPoint> polygonPointList = new List<MapPoint>();
          polygonPointList.Add(_mapPoint);
          polygonPointList.Add(point1);
          polygonPointList.Add(point2);
          polygonPointList.Add(_mapPoint);
          Polygon polygon = PolygonBuilder.CreatePolygon(polygonPointList);

          Color colorPolygon = Color.FromArgb(_blinking ? BlinkAlpha : NormalAlpha, _color);
          CIMColor cimColorPolygon = ColorFactory.CreateColor(colorPolygon);
          CIMPolygonSymbol polygonSymbol = SymbolFactory.DefaultPolygonSymbol;
          polygonSymbol.SetColor(cimColorPolygon);
          polygonSymbol.SetOutlineColor(null);
          CIMSymbolReference polygonSymbolReference = polygonSymbol.MakeSymbolReference();
          IDisposable disposePolygon = thisView.AddOverlay(polygon, polygonSymbolReference);

          IList<MapPoint> linePointList = new List<MapPoint>();
          linePointList.Add(point1);
          linePointList.Add(_mapPoint);
          linePointList.Add(point2);
          Polyline polyline = PolylineBuilder.CreatePolyline(linePointList);

          Color colorLine = _active ? Color.Yellow : Color.Gray;
          CIMColor cimColorLine = ColorFactory.CreateColor(colorLine);
          CIMLineSymbol cimLineSymbol = SymbolFactory.DefaultLineSymbol;
          cimLineSymbol.SetColor(cimColorLine);
          cimLineSymbol.SetSize(_blinking ? BorderSizeBlinkingArrow : BorderSizeArrow);
          CIMSymbolReference lineSymbolReference = cimLineSymbol.MakeSymbolReference();
          IDisposable disposePolyLine = thisView.AddOverlay(polyline, lineSymbolReference);

          _disposePolygon?.Dispose();
          _disposePolygon = disposePolygon;
          _disposePolyLine?.Dispose();
          _disposePolyLine = disposePolyLine;

          if (_blinking)
          {
            var blinkEvent = new AutoResetEvent(true);
            var blinkTimerCallBack = new TimerCallback(ResetBlinking);
            _blinkTimer = new Timer(blinkTimerCallBack, blinkEvent, BlinkTime, -1);
          }
        }
      });
    }

    #endregion

    #region Thread functions

    private async void ResetBlinking(object args)
    {
      _blinking = false;
      await RedrawConeAsync();
    }

    #endregion

    #region Event handlers

    private async void OnMapViewCameraChanged(MapViewCameraChangedEventArgs args)
    {
      await RedrawConeAsync();
    }

    #endregion
  }
}
