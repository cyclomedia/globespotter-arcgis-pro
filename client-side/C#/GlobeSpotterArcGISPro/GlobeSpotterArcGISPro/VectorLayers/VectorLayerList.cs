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
using System.Collections.ObjectModel;
using System.Linq;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.CycloMediaLayers;

namespace GlobeSpotterArcGISPro.VectorLayers
{
  #region Delegates

  public delegate void VectorLayerDelegate(VectorLayer layer);
  public delegate void VectorUpdatedDelegate();

  #endregion

  public class VectorLayerList : List<VectorLayer>
  {
    #region Events

    public event VectorLayerDelegate LayerAdded;
    public event VectorLayerDelegate LayerRemoved;
    public event VectorUpdatedDelegate LayerUpdated;

    #endregion

    #region Constructor

    public VectorLayerList()
    {
      DetectVectorLayers(true);
    }

    #endregion

    #region Functions

    public VectorLayer GetLayer(Layer layer)
    {
      return this.Aggregate<VectorLayer, VectorLayer>(null,
        (current, layerCheck) => (layerCheck.Layer == layer) ? layerCheck : current);
    }

    private void DetectVectorLayers(bool initEvents)
    {
      Clear();
      MapView mapView = MapView.Active;
      Map map = mapView?.Map;
      ReadOnlyObservableCollection<Layer> layers = map?.Layers;

      if (layers != null)
      {
        foreach (var layer in layers)
        {
          AddLayer(layer);
        }
      }

      if (initEvents)
      {
        AddEvents();
        MapViewInitializedEvent.Subscribe(OnMapViewInitialized);
        MapClosedEvent.Subscribe(OnMapClosed);
      }
    }

    private void AddLayer(Layer layer)
    {
      FeatureLayer featureLayer = layer as FeatureLayer;
      GlobeSpotter globeSpotter = GlobeSpotter.Current;
      CycloMediaGroupLayer cycloGrouplayer = globeSpotter?.CycloMediaGroupLayer;

      if ((featureLayer != null) && (cycloGrouplayer != null) && (!cycloGrouplayer.IsKnownName(featureLayer.Name)))
      {
        if (!this.Aggregate(false, (current, vecLayer) => (vecLayer.Layer == layer) || current))
        {
          var vectorLayer = new VectorLayer(featureLayer);
          Add(vectorLayer);
          LayerAdded?.Invoke(vectorLayer);
        }
      }
    }

    private void AddEvents()
    {
      LayersAddedEvent.Subscribe(OnLayersAdded);
      LayersMovedEvent.Subscribe(OnLayersMoved);
      LayersRemovedEvent.Subscribe(OnLayersRemoved);
      MapMemberPropertiesChangedEvent.Subscribe(OnMapMemberPropertiesChanged);
      TOCSelectionChangedEvent.Subscribe(OnTocSelectionChanged);
    }

    #endregion

    #region Event handlers

    private void OnTocSelectionChanged(MapViewEventArgs args)
    {
      LayerUpdated?.Invoke();
    }

    private void OnMapMemberPropertiesChanged(MapMemberPropertiesChangedEventArgs args)
    {
      LayerUpdated?.Invoke();
    }

    private void OnMapViewInitialized(MapViewEventArgs args)
    {
      DetectVectorLayers(false);
      AddEvents();
    }

    private void OnMapClosed(MapClosedEventArgs args)
    {
      LayersAddedEvent.Unsubscribe(OnLayersAdded);
      LayersMovedEvent.Unsubscribe(OnLayersMoved);
      LayersRemovedEvent.Unsubscribe(OnLayersRemoved);
      MapMemberPropertiesChangedEvent.Unsubscribe(OnMapMemberPropertiesChanged);
      TOCSelectionChangedEvent.Unsubscribe(OnTocSelectionChanged);

      while (Count >= 1)
      {
        LayerRemoved?.Invoke(this[0]);
        RemoveAt(0);
      }
    }

    private void OnLayersAdded(LayerEventsArgs args)
    {
      foreach (Layer layer in args.Layers)
      {
        AddLayer(layer);
      }
    }

    private void OnLayersMoved(LayerEventsArgs args)
    {
      LayerUpdated?.Invoke();
    }

    private void OnLayersRemoved(LayerEventsArgs args)
    {
      foreach (Layer layer in args.Layers)
      {
        FeatureLayer featureLayer = layer as FeatureLayer;

        if (featureLayer != null)
        {
          int i = 0;

          while (Count > i)
          {
            if (this[i].Layer == featureLayer)
            {
              LayerRemoved?.Invoke(this[i]);
              RemoveAt(i);
            }
            else
            {
              i++;
            }
          }
        }
      }
    }

    #endregion
  }
}
