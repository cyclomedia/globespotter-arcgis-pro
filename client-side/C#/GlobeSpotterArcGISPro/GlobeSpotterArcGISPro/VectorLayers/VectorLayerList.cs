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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.CycloMediaLayers;
using GlobeSpotterArcGISPro.Overlays.Measurement;
using GlobeSpotterArcGISPro.Utilities;

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

    #region Properties

    private readonly MeasurementList _measurementList;
    private readonly CycloMediaGroupLayer _cycloMediaGroupLayer;
    private EditTools _editTool;

    #endregion

    #region Constructor

    public VectorLayerList()
    {
      GlobeSpotter moduleGlobeSpotter = GlobeSpotter.Current;
      _measurementList = moduleGlobeSpotter.MeasurementList;
      _cycloMediaGroupLayer = moduleGlobeSpotter.CycloMediaGroupLayer;
      _editTool = EditTools.NoEditTool;
      DetectVectorLayers(true);
    }

    #endregion

    #region Functions

    public VectorLayer GetLayer(Layer layer)
    {
      return this.Aggregate<VectorLayer, VectorLayer>(null,
        (current, layerCheck) => (layerCheck.Layer == layer) ? layerCheck : current);
    }

    public async Task LoadMeasurementsAsync()
    {
      foreach (VectorLayer vectorLayer in this)
      {
        await vectorLayer.LoadMeasurementsAsync();
      }
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
          vectorLayer.PropertyChanged += OnVectorLayerPropertyChanged;
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
      DrawCompleteEvent.Subscribe(OnDrawCompleted);
      ActiveToolChangedEvent.Subscribe(OnActiveToolChangedEvent);
      EditCompletedEvent.Subscribe(OnEditCompleted);
    }

    private async Task StartMeasurementSketchAsync(VectorLayer vectorLayer)
    {
      Measurement measurement = _measurementList.Sketch;
      MapView mapView = MapView.Active;
      Geometry geometry = await mapView.GetCurrentSketchAsync();
      _measurementList.StartMeasurement(geometry, measurement, true, null, vectorLayer);
    }

    private async Task StartSketchToolAsync()
    {
      EditingTemplate editingFeatureTemplate = EditingTemplate.Current;
      Layer layer = editingFeatureTemplate?.Layer;
      VectorLayer vectorLayer = GetLayer(layer);

      if (vectorLayer.IsVisibleInGlobespotter)
      {
        await StartMeasurementSketchAsync(vectorLayer);
      }
    }

    private async void AddHeightToMeasurement(Geometry geometry, MapView mapView)
    {
      const double e = 0.1;

      switch (geometry.GeometryType)
      {
        case GeometryType.Point:
          MapPoint srcPoint = geometry as MapPoint;

          if ((srcPoint != null) && Math.Abs(srcPoint.Z) < e)
          {
            srcPoint = await AddHeightToMapPoint(srcPoint);
            await mapView.SetCurrentSketchAsync(srcPoint);
          }

          break;
        case GeometryType.Polyline:
          Polyline polyline = geometry as Polyline;
          List<MapPoint> mapLinePoints = new List<MapPoint>();
          bool changesLine = false;

          if (polyline != null)
          {
            foreach (MapPoint point in polyline.Points)
            {
              if (Math.Abs(point.Z) < e)
              {
                changesLine = true;
                MapPoint srcLinePoint = await AddHeightToMapPoint(point);
                mapLinePoints.Add(srcLinePoint);
              }
              else
              {
                mapLinePoints.Add(point);
              }
            }

            if (changesLine)
            {
              await QueuedTask.Run(() =>
              {
                polyline = PolylineBuilder.CreatePolyline(mapLinePoints, polyline.SpatialReference);
              });

              await mapView.SetCurrentSketchAsync(polyline);
            }
          }

          break;
        case GeometryType.Polygon:
          // todo: make this function for add a z to a polygon is working
          // ============================================================
          /*
          Polygon polygon = geometry as Polygon;
          List<MapPoint> mapPolygonPoints = new List<MapPoint>();
          bool changesPolygon = false;

          if (polygon != null)
          {
            for(int j = 0; j < polygon.Points.Count; j++)
            {
              MapPoint mapPoint = polygon.Points[j];

              if ((Math.Abs(mapPoint.Z) < e) && (j <= (polygon.Points.Count - 2)))
              {
                changesPolygon = true;
                MapPoint srcPolygonPoint = await AddHeightToMapPoint(mapPoint);
                mapPolygonPoints.Add(srcPolygonPoint);
              }
              else if (changesPolygon && (j == (polygon.Points.Count - 1)))
              {
                mapPolygonPoints.Add(mapPolygonPoints[0]);
              }
              else
              {
                mapPolygonPoints.Add(mapPoint);
              }
            }

            if (changesPolygon)
            {
              await QueuedTask.Run(() =>
              {
                polyline = PolylineBuilder.CreatePolyline(mapPolygonPoints, polygon.SpatialReference);
              });

              await mapView.SetCurrentSketchAsync(polygon);
            }
          }*/

          break;
      }
    }

    private async Task<MapPoint> AddHeightToMapPoint(MapPoint srcPoint)
    {
      return await QueuedTask.Run(async () =>
      {
        SpatialReference srcSpatialReference = srcPoint.SpatialReference;
        SpatialReference dstSpatialReference = await CoordSystemUtils.CycloramaSpatialReferenceAsync();

        ProjectionTransformation dstProjection = ProjectionTransformation.Create(srcSpatialReference,
          dstSpatialReference);
        MapPoint dstPoint = GeometryEngine.ProjectEx(srcPoint, dstProjection) as MapPoint;

        if (dstPoint != null)
        {
          double? height = await _cycloMediaGroupLayer.GetHeightAsync(dstPoint.X, dstPoint.Y);

          if (height != null)
          {
            dstPoint = MapPointBuilder.CreateMapPoint(dstPoint.X, dstPoint.Y, ((double)height), dstSpatialReference);
            ProjectionTransformation srcProjection = ProjectionTransformation.Create(dstSpatialReference,
              srcSpatialReference);
            srcPoint = GeometryEngine.ProjectEx(dstPoint, srcProjection) as MapPoint;
          }
        }

        return srcPoint;
      });
    }

    private void SketchFinished()
    {
      Measurement sketch = _measurementList.Sketch;

      if (sketch != null)
      {
        sketch.CloseMeasurement();
        _measurementList.SketchFinished();
      }
    }

    #endregion

    #region Edit events

    protected async void OnActiveToolChangedEvent(ToolEventArgs args)
    {
      string currentId = args.CurrentID;

      switch (currentId)
      {
        case "esri_editing_ModifyFeatureImpl":
          _editTool = EditTools.ModifyFeatureImpl;
          break;
        case "esri_editing_ReshapeFeature":
          _editTool = EditTools.ReshapeFeature;
          break;
        case "esri_editing_SketchLineTool":
          _editTool = EditTools.SketchLineTool;
          await StartSketchToolAsync();
          break;
        case "esri_editing_SketchPolygonTool":
          _editTool = EditTools.SketchPolygonTool;
          await StartSketchToolAsync();
          break;
        case "esri_editing_SketchPointTool":
          _editTool = EditTools.SketchPointTool;
          await StartSketchToolAsync();
          break;
      }
    }

    protected async void OnDrawCompleted(MapViewEventArgs args)
    {
      MapView mapView = args.MapView;
      Geometry geometry = await mapView.GetCurrentSketchAsync();

      if (geometry != null)
      {
        switch (_editTool)
        {
          case EditTools.ModifyFeatureImpl:
            if (_measurementList.Count == 1)
            {
              KeyValuePair<int, Measurement> firstElement = _measurementList.ElementAt(0);
              Measurement measurement = firstElement.Value;
              measurement.SetSketch();
              VectorLayer vectorLayer = measurement.VectorLayer;

              if (geometry.PointCount == 0)
              {
                await StartMeasurementSketchAsync(vectorLayer);
              }
              else if (geometry.HasZ)
              {
                AddHeightToMeasurement(geometry, mapView);
              }

              _measurementList.SketchModified(geometry, measurement.VectorLayer);
            }

            break;
          case EditTools.SketchLineTool:
          case EditTools.SketchPolygonTool:
          case EditTools.SketchPointTool:
            if (geometry.HasZ)
            {
              AddHeightToMeasurement(geometry, mapView);
            }

            VectorLayer thisVectorLayer = _measurementList.Sketch?.VectorLayer;
            _measurementList.SketchModified(geometry, thisVectorLayer);
            break;
        }
      }
      else
      {
        SketchFinished();
      }
    }

    protected Task OnEditCompleted(EditCompletedEventArgs args)
    {
      SketchFinished();
      return Task.FromResult(0);
    }

    #endregion

    #region Event handlers

    private void OnVectorLayerPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      if (args.PropertyName == "Measurements")
      {
        List<Measurement> totalMeasurements = new List<Measurement>();

        foreach (var vectorLayer in this)
        {
          List<Measurement> measurements = vectorLayer.Measurements;

          if (measurements != null)
          {
            totalMeasurements.AddRange(measurements);
          }
        }

        _measurementList.RemoveUnusedMeasurements(totalMeasurements);
      }
    }

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
      ActiveToolChangedEvent.Unsubscribe(OnActiveToolChangedEvent);
      EditCompletedEvent.Unsubscribe(OnEditCompleted);
      DrawCompleteEvent.Unsubscribe(OnDrawCompleted);

      while (Count >= 1)
      {
        VectorLayer vectorLayer = this[0];
        vectorLayer.PropertyChanged -= OnVectorLayerPropertyChanged;
        LayerRemoved?.Invoke(vectorLayer);
        vectorLayer.Dispose();
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
              VectorLayer vectorLayer = this[i];
              vectorLayer.PropertyChanged -= OnVectorLayerPropertyChanged;
              LayerRemoved?.Invoke(vectorLayer);
              vectorLayer.Dispose();
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
