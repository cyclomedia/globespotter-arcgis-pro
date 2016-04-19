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

using System.ComponentModel;
using System.Linq;
using ArcGIS.Desktop.Framework.Contracts;
using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.Layers;

namespace GlobeSpotterArcGISPro.AddIns.Buttons
{
  internal class RecentRecordingLayer : Button
  {
    #region Constants

    private const string LayerName = "Recent Recordings";

    #endregion

    #region Constructors

    protected RecentRecordingLayer()
    {
      IsChecked = false;
      GlobeSpotter globeSpotter = GlobeSpotter.Current;
      CycloMediaGroupLayer groupLayer = globeSpotter.CycloMediaGroupLayer;

      if (groupLayer != null)
      {
        foreach (var layer in groupLayer)
        {
          if (layer.IsRemoved)
          {
            IsChecked = (layer.Name != LayerName) && IsChecked;
          }
          else
          {
            IsChecked = (layer.Name == LayerName) || IsChecked;
          }
        }

        groupLayer.PropertyChanged += OnLayerPropertyChanged;
      }
    }

    #endregion

    #region Overrides

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

    #endregion

    #region Event handlers

    private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      CycloMediaGroupLayer groupLayer = sender as CycloMediaGroupLayer;

      if ((groupLayer != null) && (args.PropertyName == "Count"))
      {
        IsChecked = groupLayer.Aggregate(false, (current, layer) => (layer.Name == LayerName) || current);
      }
    }

    #endregion
  }
}
