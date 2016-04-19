﻿/*
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

using System.Collections.Generic;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.Layers;

namespace GlobeSpotterArcGISPro.AddIns.Buttons
{
  internal class ShowInCyclorama : Button
  {
    #region Members

    private CycloMediaLayer _cycloMediaLayer;

    #endregion

    #region Overrides

    protected override void OnClick()
    {
      IsChecked = !IsChecked;

      if (_cycloMediaLayer != null)
      {
        _cycloMediaLayer.IsVisibleInGlobespotter = IsChecked;
      }
    }

    protected override void OnUpdate()
    {
      GlobeSpotter globeSpotter = GlobeSpotter.Current;
      MapView mapView = MapView.Active;
      IReadOnlyList<Layer> layers = mapView.GetSelectedLayers();

      if (layers.Count == 1)
      {
        Layer layer = layers[0];
        CycloMediaGroupLayer groupLayer = globeSpotter.CycloMediaGroupLayer;
        _cycloMediaLayer = groupLayer?.GetLayer(layer);

        if (_cycloMediaLayer != null)
        {
          IsChecked = _cycloMediaLayer.IsVisibleInGlobespotter;
          Enabled = _cycloMediaLayer.IsVisible;
        }
        else
        {
          Enabled = false;
        }
      }
      else
      {
        Enabled = false;
      }

      base.OnUpdate();
    }

    #endregion
  }
}