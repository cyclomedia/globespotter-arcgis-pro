/*
 * Integration in ArcMap for Cycloramas
 * Copyright (c) 2015, CycloMedia, All rights reserved.
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
using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Layers;

namespace GlobeSpotterArcGISPro.AddIns.Buttons
{
  internal class RecentRecordingLayer : Button
  {
    #region Constants

    private const string LayerName = "Recent Recordings";

    #endregion

    #region Members

    private readonly Agreement _agreement;

    #endregion

    #region Constructors

    protected RecentRecordingLayer()
    {
      _agreement = Agreement.Instance;
      IsChecked = false;
      GlobeSpotter globeSpotter = GlobeSpotter.Current;
      CycloMediaGroupLayer groupLayer = globeSpotter.CycloMediaGroupLayer;

      if (groupLayer != null)
      {
        IList<CycloMediaLayer> layers = groupLayer.Layers;

        foreach (var layer in layers)
        {
          if (layer.IsRemoved)
          {
            CycloMediaLayerRemoved(layer);
          }
          else
          {
            CycloMediaLayerAdded(layer);
          }
        }
      }

      CycloMediaLayer.LayerAddedEvent += CycloMediaLayerAdded;
      CycloMediaLayer.LayerRemoveEvent += CycloMediaLayerRemoved;
    }

    #endregion

    #region Event handlers

    protected async override void OnClick()
    {
      OnUpdate();
      GlobeSpotter globeSpotter = GlobeSpotter.Current;

      if (IsChecked)
      {
        await globeSpotter.RemoveLayerAsync(LayerName);
      }
      else
      {
        await globeSpotter.AddLayersAsync(LayerName);
      }
    }

    protected override void OnUpdate()
    {
      // todo: verander dit stuk code door in daml een state, condition waarde toe te voegen
      Enabled = _agreement.Value;
      base.OnUpdate();
    }

    #endregion

    #region Other event handlers

    private void CycloMediaLayerAdded(CycloMediaLayer layer)
    {
      if (layer != null)
      {
        IsChecked = (layer.Name == LayerName) || IsChecked;
      }
    }

    private void CycloMediaLayerRemoved(CycloMediaLayer layer)
    {
      if (layer != null)
      {
        IsChecked = (layer.Name != LayerName) && IsChecked;
      }
    }

    #endregion
  }
}
