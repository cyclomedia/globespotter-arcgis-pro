﻿/*
 * Integration in ArcMap for Cycloramas
 * Copyright (c) 2015 - 2017, CycloMedia, All rights reserved.
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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Configuration.Remote.Recordings;

namespace GlobeSpotterArcGISPro.CycloMediaLayers
{
  public class CycloMediaGroupLayer: List<CycloMediaLayer>, INotifyPropertyChanged
  {
    #region Members

    private readonly ConstantsRecordingLayer _constants;
    private IList<CycloMediaLayer> _allLayers;
    private bool _updateVisibility;

    #endregion

    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region Properties

    public GroupLayer GroupLayer { get; private set; }

    public IList<CycloMediaLayer> AllLayers => _allLayers ?? (_allLayers = new List<CycloMediaLayer>
    {
      new RecordingLayer(this),
      new HistoricalLayer(this)
    });

    public bool ContainsLayers => (Count != 0);

    public bool InsideScale
    {
      get { return this.Aggregate(false, (current, layer) => layer.InsideScale || current); }
    }

    #endregion

    #region Constructor

    public CycloMediaGroupLayer()
    {
      _constants = ConstantsRecordingLayer.Instance;
    }

    #endregion

    #region Functions

    public async Task InitializeAsync()
    {
      _updateVisibility = false;
      GroupLayer = null;
      Clear();
      Map map = MapView.Active?.Map;
      string name = _constants.CycloMediaLayerName;

      if (map != null)
      {
        var layers = map.GetLayersAsFlattenedList();
        var layersForGroupLayer = map.FindLayers(name);
        bool leave = false;

        foreach (Layer layer in layersForGroupLayer)
        {
          if (layer is GroupLayer)
          {
            if (!leave)
            {
              GroupLayer = layer as GroupLayer;
              leave = true;
            }
          }
          else
          {
            await QueuedTask.Run(() =>
            {
              map.RemoveLayer(layer);
            });
          }
        }

        if (GroupLayer == null)
        {
          await QueuedTask.Run(() =>
          {
            GroupLayer = LayerFactory.Instance.CreateGroupLayer(map, 0, name);
            GroupLayer.SetExpanded(true);
          });
        }

        foreach (Layer layer in layers)
        {
          await AddLayerAsync(layer.Name);
        }
      }

      MapMemberPropertiesChangedEvent.Subscribe(OnMapMemberPropertiesChanged);
      await CheckVisibilityLayersAsync();
    }

    public CycloMediaLayer GetLayer(Layer layer)
    {
      return this.Aggregate<CycloMediaLayer, CycloMediaLayer>(null,
        (current, layerCheck) => (layerCheck.Layer == layer) ? layerCheck : current);
    }

    public async Task<CycloMediaLayer> AddLayerAsync(string name)
    {
      CycloMediaLayer thisLayer = null;

      if (!this.Aggregate(false, (current, cycloLayer) => (cycloLayer.Name == name) || current))
      {
        thisLayer = AllLayers.Aggregate<CycloMediaLayer, CycloMediaLayer>
          (null, (current, checkLayer) => (checkLayer.Name == name) ? checkLayer : current);

        if (thisLayer != null)
        {
          Add(thisLayer);
          await thisLayer.AddToLayersAsync();
          // ReSharper disable once ExplicitCallerInfoArgument
          NotifyPropertyChanged(nameof(Count));
          FrameworkApplication.State.Activate("globeSpotterArcGISPro_recordingLayerEnabledState");

          if (thisLayer.UseDateRange)
          {
            FrameworkApplication.State.Activate("globeSpotterArcGISPro_historicalLayerEnabledState");
          }          
        }
      }

      return thisLayer;
    }

    public async Task RemoveLayerAsync(string name, bool fromGroup)
    {
      CycloMediaLayer layer = this.Aggregate<CycloMediaLayer, CycloMediaLayer>
        (null, (current, checkLayer) => (checkLayer.Name == name) ? checkLayer : current);

      if (layer != null)
      {
        Remove(layer);
        await layer.DisposeAsync(fromGroup);
        // ReSharper disable once ExplicitCallerInfoArgument
        NotifyPropertyChanged(nameof(Count));

        if (Count == 0)
        {
          FrameworkApplication.State.Deactivate("globeSpotterArcGISPro_recordingLayerEnabledState");
        }

        if (layer.UseDateRange)
        {
          FrameworkApplication.State.Deactivate("globeSpotterArcGISPro_historicalLayerEnabledState");
        }
      }
    }

    public bool IsKnownName(string name)
    {
      return this.Aggregate((name == _constants.CycloMediaLayerName), (current, layer) => (layer.Name == name) || current);
    }

    public async Task DisposeAsync(bool fromMap)
    {
      while (Count > 0)
      {
        await RemoveLayerAsync(this[0].Name, false);
      }

      if (fromMap)
      {
        await QueuedTask.Run(() =>
        {
          Map map = MapView.Active?.Map;

          if ((map != null) && (GroupLayer != null))
          {
            map.RemoveLayer(GroupLayer);
          }
        });
      }

      MapMemberPropertiesChangedEvent.Unsubscribe(OnMapMemberPropertiesChanged);
    }

    public async Task<Recording> GetRecordingAsync(string imageId)
    {
      Recording result = null;

      foreach (CycloMediaLayer layer in this)
      {
        Recording recording = await layer.GetRecordingAsync(imageId);
        result = recording ?? result;
      }

      return result;
    }

    public async Task<double?> GetHeightAsync(double x, double y)
    {
      double? result = null;
      int count = 0;

      foreach (CycloMediaLayer layer in this)
      {
        double? height = await layer.GetHeightAsync(x, y);

        if (height != null)
        {
          result = result ?? 0.0;
          result = result + height;
          count++;
        }
      }

      result = (result != null) ? (result/Math.Max(count, 1)) : null;
      return result;
    }

    private async Task CheckVisibilityLayersAsync()
    {
      if (!_updateVisibility)
      {
        _updateVisibility = true;

        foreach (var layer in AllLayers)
        {
          if (!this.Aggregate(false, (current, visLayer) => current || (visLayer == layer)))
          {
            await layer.SetVisibleAsync(true);
            layer.Visible = false;
          }
        }

        CycloMediaLayer changedLayer = this.Aggregate<CycloMediaLayer, CycloMediaLayer>
          (null, (current, layer) => (layer.IsVisible && (!layer.Visible)) ? layer : current);
        CycloMediaLayer refreshLayer = null;

        foreach (var layer in this)
        {
          if (layer.IsInitialized)
          {
            bool visible = ((changedLayer == null) || (layer == changedLayer)) && layer.IsVisible;
            refreshLayer = (layer.IsVisible != visible) ? layer : refreshLayer;
            await layer.SetVisibleAsync(visible);
            layer.Visible = layer.IsVisible;
          }
        }

        _updateVisibility = false;
      }
    }

    #endregion

    #region Event handlers

    private async void OnMapMemberPropertiesChanged(MapMemberPropertiesChangedEventArgs args)
    {
      await CheckVisibilityLayersAsync();
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
