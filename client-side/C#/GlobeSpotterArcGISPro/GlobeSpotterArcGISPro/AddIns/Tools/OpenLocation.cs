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

using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.Configuration.Remote.Recordings;
using GlobeSpotterArcGISPro.Layers;

using DockPaneGlobeSpotter = GlobeSpotterArcGISPro.AddIns.DockPanes.GlobeSpotter;

namespace GlobeSpotterArcGISPro.AddIns.Tools
{
  class OpenLocation : MapTool
  {
    #region Members

    private string _imageId;

    #endregion

    #region Constructor

    public OpenLocation()
    {
      IsSketchTool = true;
      SketchType = SketchGeometryType.Point;
    }

    #endregion

    #region Overrides

    protected override void OnToolMouseUp(MapViewMouseButtonEventArgs e)
    {
      if (!string.IsNullOrEmpty(_imageId))
      {
        DockPaneGlobeSpotter.ShowLocation(_imageId);
        _imageId = string.Empty;
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
        var features = MapView.Active?.GetFeatures(geometry);
        GlobeSpotter globeSpotter = GlobeSpotter.Current;
        CycloMediaGroupLayer groupLayer = globeSpotter?.CycloMediaGroupLayer;

        if ((features != null) && (groupLayer != null))
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
                _imageId = recording.ImageId;
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
