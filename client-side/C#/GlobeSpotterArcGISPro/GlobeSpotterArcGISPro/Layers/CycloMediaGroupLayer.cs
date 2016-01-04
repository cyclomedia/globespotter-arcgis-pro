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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using GlobeSpotterArcGISPro.Configuration.Remote.Recordings;

namespace GlobeSpotterArcGISPro.Layers
{
  public class CycloMediaGroupLayer
  {
    #region Members

    private IList<CycloMediaLayer> _allLayers;

    #endregion

    #region Properties

    public string Name => "CycloMedia";

    public GroupLayer GroupLayer { get; private set; }

    public IList<CycloMediaLayer> AllLayers => _allLayers ?? (_allLayers = new List<CycloMediaLayer>
    {
      new RecordingLayer(this),
      new HistoricalLayer(this)
    });

    public IList<CycloMediaLayer> Layers { get; private set; }

    public bool ContainsLayers => (Layers.Count != 0);

    public bool InsideScale
    {
      get { return Layers.Aggregate(false, (current, layer) => layer.InsideScale || current); }
    }

    #endregion

    #region Functions

    public async Task InitializeAsync()
    {
      GroupLayer = null;
      Layers = new List<CycloMediaLayer>();
      Map map = MapView.Active?.Map;

      if (map != null)
      {
        var layers = map.Layers;
        var layersForGroupLayer = map.FindLayers(Name);
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
            GroupLayer = LayerFactory.CreateGroupLayer(map, 0, Name);
            GroupLayer.SetExpanded(true);
          });
        }

        foreach (Layer layer in layers)
        {
          await AddLayerAsync(layer.Name);
        }
      }

      TOCSelectionChangedEvent.Subscribe(OnTocSelectionChanged);
    }

    public CycloMediaLayer GetLayer(Layer layer)
    {
      return Layers.Aggregate<CycloMediaLayer, CycloMediaLayer>(null,
        (current, layerCheck) => (layerCheck.Layer == layer) ? layerCheck : current);
    }

    public async Task<CycloMediaLayer> AddLayerAsync(string name)
    {
      CycloMediaLayer thisLayer = null;

      if (!Layers.Aggregate(false, (current, cycloLayer) => (cycloLayer.Name == name) || current))
      {
        thisLayer = AllLayers.Aggregate<CycloMediaLayer, CycloMediaLayer>
          (null, (current, checkLayer) => (checkLayer.Name == name) ? checkLayer : current);

        if (thisLayer != null)
        {
          Layers.Add(thisLayer);
          await thisLayer.AddToLayersAsync();
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
      CycloMediaLayer layer = Layers.Aggregate<CycloMediaLayer, CycloMediaLayer>
        (null, (current, checkLayer) => (checkLayer.Name == name) ? checkLayer : current);

      if (layer != null)
      {
        Layers.Remove(layer);
        await layer.DisposeAsync(fromGroup);
        FrameworkApplication.State.Deactivate("globeSpotterArcGISPro_recordingLayerEnabledState");

        if (layer.UseDateRange)
        {
          FrameworkApplication.State.Deactivate("globeSpotterArcGISPro_historicalLayerEnabledState");
        }
      }
    }

    public bool IsKnownName(string name)
    {
      return Layers.Aggregate((name == Name), (current, layer) => (layer.Name == name) || current);
    }

    public async Task DisposeAsync(bool fromMap)
    {
      while (Layers.Count > 0)
      {
        await RemoveLayerAsync(Layers[0].Name, false);
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

      TOCSelectionChangedEvent.Unsubscribe(OnTocSelectionChanged);
    }

    public string GetFeatureFromPoint(int x, int y, out CycloMediaLayer layer)
    {
      string result = string.Empty;
      layer = null;

      foreach (var layert in Layers)
      {
        if (string.IsNullOrEmpty(result))
        {
          if (layert.IsVisible)
          {
            layer = layert;
            result = layer.GetFeatureFromPoint(x, y);
          }
        }
      }

      return result;
    }

    public Recording GetRecording(string imageId)
    {
      return Layers.Select(layer => layer.GetRecording(imageId)).Aggregate<Recording, Recording>
        (null, (current, featureCollection) => featureCollection ?? current);
    }

    public async Task<bool> MakeEmptyAsync()
    {
      bool result = true;

      foreach (var layer in AllLayers)
      {
        result = result && (await layer.MakeEmptyAsync());
      }

      return result;
    }

    #endregion

    #region Event handlers

    private async void OnTocSelectionChanged(MapViewEventArgs args)
    {
      /*
      foreach (var layer in AllLayers)
      {
        if (!Layers.Aggregate(false, (current, visLayer) => current || (visLayer == layer)))
        {
          await layer.SetVisibleAsync(true);
          layer.Visible = false;
        }
      }

      CycloMediaLayer changedLayer = Layers.Aggregate<CycloMediaLayer, CycloMediaLayer>
        (null, (current, layer) => (layer.IsVisible && (!layer.Visible)) ? layer : current);
      CycloMediaLayer refreshLayer = null;

      foreach (var layer in Layers)
      {
        bool visible = ((changedLayer == null) || (layer == changedLayer)) && layer.IsVisible;
        refreshLayer = (layer.IsVisible != visible) ? layer : refreshLayer;
        await layer.SetVisibleAsync(visible);
        layer.Visible = layer.IsVisible;
      }
      */
    }

    #endregion
  }
}
