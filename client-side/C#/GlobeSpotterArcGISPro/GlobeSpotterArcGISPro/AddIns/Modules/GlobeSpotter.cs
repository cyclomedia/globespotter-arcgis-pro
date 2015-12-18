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

using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Layers;

namespace GlobeSpotterArcGISPro.AddIns.Modules
{
  internal class GlobeSpotter : Module
  {
    #region Members

    private static GlobeSpotter _globeSpotter;

    #endregion

    #region Properties

    /// <summary>
    /// Retrieve the singleton instance to this module here
    /// </summary>
    public static GlobeSpotter Current
      =>
        _globeSpotter ??
        (_globeSpotter = (GlobeSpotter) FrameworkApplication.FindModule("GlobeSpotterArcGISPro_Module"));

    public CycloMediaGroupLayer CycloMediaGroupLayer { get; private set; }

    #endregion

    #region Constructor

    public GlobeSpotter()
    {
      Agreement agreement = Agreement.Instance;

      if (!agreement.Value)
      {
        // ToDo: open agreement form
      }

      MapViewInitializedEvent.Subscribe(OnOpenDocument);
      MapClosedEvent.Subscribe(OnCloseDocument);
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Called by Framework when ArcGIS Pro is closing
    /// </summary>
    /// <returns>False to prevent Pro from closing, otherwise True</returns>
    protected override bool CanUnload()
    {
      return true;
    }

    #endregion

    #region Functions

    internal bool InsideScale()
    {
      return (CycloMediaGroupLayer != null) && CycloMediaGroupLayer.InsideScale;
    }

    private bool ContainsCycloMediaLayer()
    {
      return
        MapView.Active?.Map?.Layers.Aggregate(false,
          (current, layer) => (CycloMediaGroupLayer?.IsKnownName(layer.Name) ?? (layer.Name == "CycloMedia")) || current) ??
        false;
    }

    private async Task CloseCycloMediaLayerAsync(bool closeDocument)
    {
      if (CycloMediaGroupLayer != null)
      {
        if ((!ContainsCycloMediaLayer()) || closeDocument)
        {
          await RemoveLayersAsync();
        }
      }

      if (closeDocument)
      {
        LayersRemovedEvent.Unsubscribe(OnLayerRemoved);
        DrawCompleteEvent.Unsubscribe(OnDrawComplete);
        CycloMediaLayer.LayerRemoveEvent -= OnLayerRemoved;
      }
    }

    public async Task AddLayersAsync()
    {
      await AddLayersAsync(null);
    }

    public async Task AddLayersAsync(string name)
    {
      if (CycloMediaGroupLayer == null)
      {
        CycloMediaGroupLayer = new CycloMediaGroupLayer();
        await CycloMediaGroupLayer.InitializeAsync();
      }

      if (!string.IsNullOrEmpty(name))
      {
        await CycloMediaGroupLayer.AddLayerAsync(name);
      }
    }

    public async Task RemoveLayerAsync(string name)
    {
      if (CycloMediaGroupLayer != null)
      {
        await CycloMediaGroupLayer.RemoveLayerAsync(name);
      }
    }

    public async Task RemoveLayersAsync()
    {
      if (CycloMediaGroupLayer != null)
      {
        CycloMediaGroupLayer cycloLayer = CycloMediaGroupLayer;
        CycloMediaGroupLayer = null;
        await cycloLayer.DisposeAsync();
      }
    }


    protected async override void Uninitialize()
    {
      await RemoveLayersAsync();
      MapViewInitializedEvent.Unsubscribe(OnOpenDocument);
      MapClosedEvent.Unsubscribe(OnCloseDocument);
      base.Uninitialize();
    }

    #endregion

    #region Event handlers

    private async void OnOpenDocument(MapViewEventArgs args)
    {
      CycloMediaLayer.ResetYears();
      LayersRemovedEvent.Subscribe(OnLayerRemoved);
      DrawCompleteEvent.Subscribe(OnDrawComplete);

      if (ContainsCycloMediaLayer())
      {
        await AddLayersAsync();
      }

      CycloMediaLayer.LayerRemoveEvent += OnLayerRemoved;
    }

    private async void OnCloseDocument(MapClosedEventArgs args)
    {
      await CloseCycloMediaLayerAsync(true);
    }

    private async void OnLayerRemoved(LayerEventsArgs args)
    {
      await CloseCycloMediaLayerAsync(false);
    }

    private void OnDrawComplete(MapViewEventArgs args)
    {
      // toDo: is this function necessary?
    }

    private async void OnLayerRemoved(CycloMediaLayer cycloMediaLayer)
    {
      if (CycloMediaGroupLayer != null)
      {
        if (!CycloMediaGroupLayer.ContainsLayers)
        {
          await RemoveLayersAsync();
        }
      }
    }

    #endregion
  }
}
