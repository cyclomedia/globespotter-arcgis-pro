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

using PaneGlobeSpotter = GlobeSpotterArcGISPro.AddIns.Panes.GlobeSpotter;

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
        PaneGlobeSpotter.ShowLocation(_imageId);
        _imageId = string.Empty;
      }
    }

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
