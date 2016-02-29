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

using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Configuration.Remote.Recordings;
using GlobeSpotterArcGISPro.Layers;

using DockPaneGlobeSpotter = GlobeSpotterArcGISPro.AddIns.DockPanes.GlobeSpotter;
using MySpatialReference = GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference.SpatialReference;
using MySpatialReferences = GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference.SpatialReferences;
using WinPoint = System.Windows.Point;

namespace GlobeSpotterArcGISPro.AddIns.Tools
{
  class OpenLocation : MapTool
  {
    #region Members

    private string _location;
    private bool _nearest;

    #endregion

    #region Constructor

    public OpenLocation()
    {
      IsSketchTool = true;
      SketchType = SketchGeometryType.Point;
      _location = string.Empty;
      _nearest = false;
    }

    #endregion

    #region Overrides

    protected override void OnToolMouseUp(MapViewMouseButtonEventArgs e)
    {
      if (!string.IsNullOrEmpty(_location))
      {
        bool replace = (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)));
        DockPaneGlobeSpotter.OpenLocation(_location, replace, _nearest);
        _location = string.Empty;
        _nearest = false;
      }
    }
/*
    protected override async void OnToolMouseMove(MapViewMouseEventArgs e)
    {
      await QueuedTask.Run(() =>
      {
        CycloMediaLayer layer;
        //string imageId = GetImageIdFromPoint(arg, out layer);
        //Cursor = (string.IsNullOrEmpty(imageId) || (layer == null)) ? _thisCursor : Cursors.Arrow;
        WinPoint point = e.ClientPoint;
        WinPoint minPoint = new WinPoint(point.X - 7, point.Y - 7);
        WinPoint maxPoint = new WinPoint(point.X + 7, point.Y + 7);
        MapPoint minPoint2 = ActiveMapView.ClientToMap(minPoint);
        MapPoint maxPoint2 = ActiveMapView.ClientToMap(maxPoint);
        Envelope envelope = EnvelopeBuilder.CreateEnvelope(minPoint2, maxPoint2);
        var features = MapView.Active?.GetFeatures(envelope, true, true);

        Cursor = Cursors.Arrow;
        if (features != null)
        {
          Cursor = (features.Count == 0) ? _thisCursor : Cursors.Arrow;
        }
      });

      base.OnToolMouseMove(e);
    }
*/
    protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
    {
      return QueuedTask.Run(() =>
      {
        MapPoint point = geometry as MapPoint;
        MapView activeView = MapView.Active;

        if ((point != null) && (activeView != null))
        {
          var constants = ConstantsRecordingLayer.Instance;
          double size = constants.SizeLayer;
          double halfSize = size/2;

          SpatialReference pointSpatialReference = point.SpatialReference;
          var pointScreen = activeView.MapToScreen(point);
          double x = pointScreen.X;
          double y = pointScreen.Y;
          WinPoint pointScreenMin = new WinPoint(x - halfSize, y - halfSize);
          WinPoint pointScreenMax = new WinPoint(x + halfSize, y + halfSize);
          var pointMapMin = activeView.ScreenToMap(pointScreenMin);
          var pointMapMax = activeView.ScreenToMap(pointScreenMax);

          Envelope envelope = EnvelopeBuilder.CreateEnvelope(pointMapMin, pointMapMax, pointSpatialReference);
          var features = activeView.GetFeatures(envelope);

          GlobeSpotter globeSpotter = GlobeSpotter.Current;
          CycloMediaGroupLayer groupLayer = globeSpotter?.CycloMediaGroupLayer;

          if ((features != null) && (groupLayer != null))
          {
            _nearest = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));

            if (_nearest)
            {
              Settings settings = Settings.Instance;
              MySpatialReference cycloCoordSystem = settings.CycloramaViewerCoordinateSystem;

              if (cycloCoordSystem?.ArcGisSpatialReference == null)
              {
                string epsgCode = (cycloCoordSystem == null)
                  ? $"EPSG:{MapView.Active?.Map?.SpatialReference.Wkid ?? 0}"
                  : cycloCoordSystem.SRSName;

                MySpatialReferences spatialReferences = MySpatialReferences.Instance;
                cycloCoordSystem = spatialReferences.GetItem(epsgCode);

                if (cycloCoordSystem == null)
                {
                  cycloCoordSystem = spatialReferences.Aggregate<MySpatialReference, MySpatialReference>(null,
                    (current, spatialReferenceComp) =>
                      (spatialReferenceComp.ArcGisSpatialReference != null) ? spatialReferenceComp : current);
                }

                if (cycloCoordSystem != null)
                {
                  settings.CycloramaViewerCoordinateSystem = cycloCoordSystem;
                  settings.Save();
                }
              }

              if (cycloCoordSystem != null)
              {
                SpatialReference cycloSpatialReference = cycloCoordSystem.ArcGisSpatialReference ??
                                                         cycloCoordSystem.CreateArcGisSpatialReferenceAsync().Result;

                if (pointSpatialReference.Wkid != cycloSpatialReference.Wkid)
                {
                  ProjectionTransformation projection = ProjectionTransformation.Create(pointSpatialReference,
                    cycloSpatialReference);
                  point = GeometryEngine.ProjectEx(point, projection) as MapPoint;
                }

                if (point != null)
                {
                  CultureInfo ci = CultureInfo.InvariantCulture;
                  _location = string.Format(ci, "{0},{1}", point.X, point.Y);

                  if (!globeSpotter.InsideScale())
                  {
                    double minimumScale = ConstantsRecordingLayer.Instance.MinimumScale;
                    double scale = minimumScale/2;
                    Camera camera = new Camera(point.X, point.Y, scale, 0.0);
                    MapView.Active?.ZoomTo(camera);
                  }
                }
              }
            }
            else
            {
              foreach (var feature in  features)
              {
                Layer layer = feature.Key;
                CycloMediaLayer cycloMediaLayer = groupLayer.GetLayer(layer);

                foreach (long uid in feature.Value)
                {
                  Recording recording = cycloMediaLayer.GetRecordingAsync(uid).Result;

                  if ((recording.IsAuthorized == null) || ((bool) recording.IsAuthorized))
                  {
                    _location = recording.ImageId;
                  }
                }
              }
            }
          }
        }

        return true;
      });
    }

    #endregion
  }
}
