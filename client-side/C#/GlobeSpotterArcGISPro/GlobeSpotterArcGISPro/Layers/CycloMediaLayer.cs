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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Configuration.Remote.Recordings;
using GlobeSpotterArcGISPro.Utilities;
using MySpatialReference = GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference.SpatialReference;
using RecordingPoint = GlobeSpotterArcGISPro.Configuration.Remote.Recordings.Point;
using SpatialReference = ArcGIS.Core.Geometry.SpatialReference;

namespace GlobeSpotterArcGISPro.Layers
{
  public delegate void CycloMediaLayerAddedDelegate(CycloMediaLayer cycloMediaLayer);
  public delegate void CycloMediaLayerChangedDelegate(CycloMediaLayer cycloMediaLayer);
  public delegate void CycloMediaLayerRemoveDelegate(CycloMediaLayer cycloMediaLayer);
  public delegate void HistoricalDateDelegate(SortedDictionary<int, int> yearMonth);

  public abstract class CycloMediaLayer
  {
    #region Members

    public static event CycloMediaLayerAddedDelegate LayerAddedEvent;
    public static event CycloMediaLayerChangedDelegate LayerChangedEvent;
    public static event CycloMediaLayerRemoveDelegate LayerRemoveEvent;
    public static event HistoricalDateDelegate HistoricalDateChanged;

    private static SortedDictionary<int, int> _yearMonth;

    private readonly CycloMediaGroupLayer _cycloMediaGroupLayer;

    private Envelope _lastextent;
    private FeatureCollection _addData;
    private bool _isVisibleInGlobespotter;

    #endregion

    #region Properties

    public abstract string Name { get; }
    public abstract string FcName { get; }
    public abstract bool UseDateRange { get; }
    public abstract string WfsRequest { get; }
    public abstract Color Color { get; set; }
    public abstract double MinimumScale { get; set; }

    public bool Visible { get; set; }
    public bool IsRemoved { get; set; }

    public int SizeLayer => 7;

    public FeatureLayer Layer { get; private set; }

    public bool IsVisible => (Layer != null) && Layer.IsVisible;

    public bool IsVisibleInGlobespotter
    {
      get { return (_isVisibleInGlobespotter && IsVisible); }
      set
      {
        _isVisibleInGlobespotter = value;
        OnContentChanged(null);
      }
    }

    public bool InsideScale
    {
      get
      {
        Camera camera = MapView.Active?.Camera;
        return (camera != null) && (Math.Floor(camera.Scale) <= (MinimumScale = Layer.MinScale));
      }
    }

    protected static SortedDictionary<int, int> YearMonth => _yearMonth ?? (_yearMonth = new SortedDictionary<int, int>());

    #endregion

    #region Constructor

    protected CycloMediaLayer(CycloMediaGroupLayer layer)
    {
      _cycloMediaGroupLayer = layer;
      _isVisibleInGlobespotter = true;
      Visible = false;
      IsRemoved = true;
      _lastextent = MapView.Active.Extent;
    }

    #endregion

    #region Functions

    protected abstract bool Filter(Recording recording);
    protected abstract void PostEntryStep();

    public async Task SetVisibleAsync(bool value)
    {
      await QueuedTask.Run(() => Layer?.SetVisibility(value));
    }

    private async Task<Envelope> GetExtentAsync(Envelope envelope)
    {
      return await QueuedTask.Run(() =>
      {
        FeatureClass featureClass = Layer?.GetFeatureClass();
        FeatureClassDefinition featureClassDefinition = featureClass?.GetDefinition();
        SpatialReference spatialReference = featureClassDefinition?.GetSpatialReference();
        SpatialReference envSpat = envelope.SpatialReference;
        Envelope result;

        if ((spatialReference != null) && (envSpat.Wkid != spatialReference.Wkid))
        {
          ProjectionTransformation projection = ProjectionTransformation.Create(envSpat, spatialReference);
          result = GeometryEngine.ProjectEx(envelope, projection) as Envelope;
        }
        else
        {
          result = (Envelope) envelope.Clone();
        }

        return result;
      });
    }

    public async Task UpdateLayerAsync()
    {
      if (Layer != null)
      {
        Layer oldLayer = Layer;
        await CreateFeatureLayerAsync();
        await RemoveLayerAsync(_cycloMediaGroupLayer.GroupLayer, oldLayer);
        await Refresh();
      }
    }

    private async Task CreateFeatureLayerAsync()
    {
      Map map = MapView.Active?.Map;
      SpatialReference spatialReference = null;
      Settings config = Settings.Instance;
      MySpatialReference spatialReferenceRecording = config.RecordingLayerCoordinateSystem;

      if (spatialReferenceRecording == null)
      {
        if (map != null)
        {
          spatialReference = map.SpatialReference;
        }
      }
      else
      {
        spatialReference = spatialReferenceRecording.ArcGisSpatialReference ??
                           await spatialReferenceRecording.CreateArcGisSpatialReferenceAsync();
      }

      int wkid = spatialReference?.Wkid ?? 0;
      string fcNameWkid = string.Concat(FcName, wkid);
      Project project = CoreModule.CurrentProject;
      await CreateFeatureClassAsync(project, fcNameWkid, spatialReference);
      FeatureLayer tempLayer = await CreateLayerAsync(project, fcNameWkid, map);
      Layer = await CreateLayerAsync(project, fcNameWkid, _cycloMediaGroupLayer.GroupLayer);
      await RemoveLayerAsync(map, tempLayer);
      await MakeEmptyAsync();
      CreateUniqueValueRenderer();
    }

    public async Task AddToLayersAsync()
    {
      Layer = null;
      Map map = MapView.Active?.Map;

      if (map != null)
      {
        var layersByName = map.FindLayers(Name);
        bool leave = false;

        foreach (Layer layer in layersByName)
        {
          if (layer is FeatureLayer)
          {
            if (!leave)
            {
              Layer = layer as FeatureLayer;
              leave = true;
            }
          }
          else
          {
            await RemoveLayerAsync(map, layer);
          }
        }
      }

      if (Layer == null)
      {
        await CreateFeatureLayerAsync();
      }
      else
      {
        CreateUniqueValueRenderer();
      }

      LayerAddedEvent?.Invoke(this);
      IsRemoved = false;
      MapViewCameraChangedEvent.Subscribe(OnMapViewCameraChanged);
      TOCSelectionChangedEvent.Subscribe(OnContentChanged);
      LayersRemovedEvent.Subscribe(OnLayersRemoved);
      await Refresh();
    }

    private async Task<FeatureLayer> CreateLayerAsync(Project project, string fcName, ILayerContainerEdit layerContainer)
    {
      return await QueuedTask.Run(() =>
      {
        string featureClassUrl = $@"{project.DefaultGeodatabasePath}\{fcName}";
        Uri uri = new Uri(featureClassUrl);
        FeatureLayer result = LayerFactory.CreateFeatureLayer(uri, layerContainer);
        result.SetName(Name);
        result.SetMinScale(MinimumScale);
        result.SetVisibility(true);
        result.SetEditable(true);
        return result;
      });
    }

    private async Task RemoveLayerAsync(ILayerContainerEdit layerContainer, Layer layer)
    {
      await QueuedTask.Run(() =>
      {
        if (layerContainer?.Layers.Contains(layer) ?? false)
        {
          layerContainer.RemoveLayer(layer);
        }
      });
    }

    public async Task DisposeAsync(bool fromGroup)
    {
      if (fromGroup)
      {
        await RemoveLayerAsync(_cycloMediaGroupLayer.GroupLayer, Layer);
      }

      Remove();
    }

    public string GetFeatureFromPoint(int x, int y)
    {
      // toDo: later
      return string.Empty;
    }

    public async Task<Recording> GetRecordingAsync(long uid)
    {
      return await QueuedTask.Run(() =>
      {
        Recording result = null;

        using (FeatureClass featureClass = Layer?.GetFeatureClass())
        {
          if (featureClass != null)
          {
            var fields = Recording.Fields;
            string shapeFieldName = Recording.ShapeFieldName;
            FeatureClassDefinition definition = featureClass.GetDefinition();
            string objectIdField = definition.GetObjectIDField();

            QueryFilter filter = new QueryFilter
            {
              WhereClause = $"{objectIdField} = {uid}",
              SubFields =
                $"{fields.Aggregate(string.Empty, (current, field) => $"{current}{(string.IsNullOrEmpty(current) ? string.Empty : ", ")}{field.Key}")}, {shapeFieldName}"
            };

            using (RowCursor existsResult = featureClass.Search(filter, false))
            {
              while (existsResult.MoveNext())
              {
                using (Row row = existsResult.Current)
                {
                  if (row != null)
                  {
                    result = new Recording();

                    foreach (var field in fields)
                    {
                      string name = field.Key;
                      int nameId = existsResult.FindField(name);
                      object item = row.GetOriginalValue(nameId);
                      result.UpdateItem(name, item);
                    }

                    int shapeId = row.FindField(shapeFieldName);
                    object point = row.GetOriginalValue(shapeId);
                    result.UpdateItem(shapeFieldName, point);
                  }
                }
              }
            }
          }
        }

        return result;
      });
    }

    public Recording GetRecording(string imageId)
    {
      // toDo: later
      return null;
    }

    public async Task Refresh()
    {
      await QueuedTask.Run(() =>
      {
        Envelope extent = MapView.Active?.Extent;

        if (extent != null)
        {
          _lastextent = extent;
          _lastextent = _lastextent.Expand(5, 5, true);
          OnMapViewCameraChanged(null);
        }
      });
    }

    public void AddZToSketch()
    {
      // toDo: kijken of deze nog nodig is
    }

    public virtual void UpdateColor(Color color, int? year)
    {
      // toDo: later
    }

    public virtual DateTime? GetDate()
    {
      return null;
    }

    public virtual double GetHeight(double x, double y)
    {
      return 0.0;
    }

    protected virtual void Remove()
    {
      Layer = null;
      IsRemoved = true;
      LayerRemoveEvent?.Invoke(this);
      MapViewCameraChangedEvent.Unsubscribe(OnMapViewCameraChanged);
      TOCSelectionChangedEvent.Unsubscribe(OnContentChanged);
      LayersRemovedEvent.Unsubscribe(OnLayersRemoved);
    }

    private void CreateUniqueValueRenderer()
    {
      // todo: later
    }

    private async Task CreateFeatureClassAsync(Project project, string fcName, SpatialReference spatialReference)
    {
      bool createNewFeatureClass = false;

      await QueuedTask.Run(() =>
      {
        string location = project.DefaultGeodatabasePath;

        using (var geodatabase = new Geodatabase(location))
        {
          try
          {
            using (geodatabase.OpenDataset<FeatureClass>(fcName))
            {
            }
          }
          catch (GeodatabaseException)
          {
            createNewFeatureClass = true;
          }
        }
      });

      if (createNewFeatureClass)
      {
        FileUtils.GetFileFromAddIn("Recordings.FCRecordings.dbf", @"Recordings\FCRecordings.dbf");
        FileUtils.GetFileFromAddIn("Recordings.FCRecordings.shp", @"Recordings\FCRecordings.shp");
        FileUtils.GetFileFromAddIn("Recordings.FCRecordings.shx", @"Recordings\FCRecordings.shx");
        string template = Path.Combine(FileUtils.FileDir, @"Recordings\FCRecordings.shp");
        await CreateFeatureClass(project, fcName, template, spatialReference);

        await QueuedTask.Run(() =>
        {
          Map map = MapView.Active?.Map;

          if (map != null)
          {
            Layer thisLayer =
              (map.GetLayersAsFlattenedList().OfType<FeatureLayer>()).FirstOrDefault(
                checkLayer => checkLayer.Name == fcName);
            map.RemoveLayer(thisLayer);
          }
        });
      }
    }

    private async Task<IGPResult> CreateFeatureClass(Project project, string fcName, string template, SpatialReference spatialReference)
    {
      var arguments = new List<object>
      {
        project.DefaultGeodatabasePath,
        fcName,
        "POINT",
        template,
        "DISABLED",
        "ENABLED",
        spatialReference
      };

      return await
        Geoprocessing.ExecuteToolAsync("CreateFeatureclass_management",
          Geoprocessing.MakeValueArray(arguments.ToArray()));
    }

    public async Task<bool> MakeEmptyAsync()
    {
      return await QueuedTask.Run(() =>
      {
        var editOperation = new EditOperation {Name = Name};

        using (FeatureClass featureClass = Layer?.GetFeatureClass())
        {
          using (RowCursor rowCursor = featureClass?.Search(null, false))
          {
            if (rowCursor != null)
            {
              while (rowCursor.MoveNext())
              {
                using (Row row = rowCursor.Current)
                {
                  editOperation.Delete(Layer, row.GetObjectID());
                }
              }
            }
          }
        }

        return editOperation.IsEmpty ? Task.FromResult(true) : editOperation.ExecuteAsync();
      });
    }

    public async Task<bool> SaveFeatureMembersAsync(FeatureCollection featureCollection, Envelope envelope)
    {
      return await QueuedTask.Run(() =>
      {
        var editOperation = new EditOperation
        {
          Name = Name,
          SelectNewFeatures = false,
          ShowModalMessageAfterFailure = false
        };

        if ((featureCollection != null) && (featureCollection.NumberOfFeatures >= 1))
        {
          FeatureMembers featureMembers = featureCollection.FeatureMembers;
          Recording[] recordings = featureMembers?.Recordings;

          if ((Layer != null) && (recordings != null))
          {
            string idField = Recording.ObjectId;
            var exists = new Dictionary<string, long>();

            using (FeatureClass featureClass = Layer?.GetFeatureClass())
            {
              if (featureClass != null)
              {
                SpatialQueryFilter spatialFilter = new SpatialQueryFilter
                {
                  FilterGeometry = envelope,
                  SpatialRelationship = SpatialRelationship.Contains,
                  SubFields = idField
                };

                using (RowCursor existsResult = featureClass.Search(spatialFilter, false))
                {
                  int imId = existsResult.FindField(idField);

                  while (existsResult.MoveNext())
                  {
                    using (Row row = existsResult.Current)
                    {
                      string recValue = row?.GetOriginalValue(imId) as string;

                      if ((recValue != null) && (!exists.ContainsKey(recValue)))
                      {
                        exists.Add(recValue, row.GetObjectID());
                      }
                    }
                  }
                }

                FeatureClassDefinition definition = featureClass.GetDefinition();
                SpatialReference spatialReference = definition.GetSpatialReference();

                foreach (Recording recording in recordings)
                {
                  Location location = recording?.Location;
                  RecordingPoint point = location?.Point;

                  if ((location != null) && (point != null))
                  {
                    if (!exists.ContainsKey((string) recording.FieldToItem(idField)))
                    {
                      if (Filter(recording))
                      {
                        Dictionary<string, object> toAddFields = Recording.Fields.ToDictionary(fieldId => fieldId.Key,
                          fieldId => recording.FieldToItem(fieldId.Key));

                        MapPoint newPoint = MapPointBuilder.CreateMapPoint(point.X, point.Y, point.Z, spatialReference);
                        toAddFields.Add(Recording.ShapeFieldName, newPoint);
                        editOperation.Create(Layer, toAddFields);
                      }
                    }
                    else
                    {
                      if (Filter(recording))
                      {
                        exists.Remove((string) recording.FieldToItem(idField));
                      }
                    }
                  }
                }

                foreach (var row in exists)
                {
                  editOperation.Delete(Layer, row.Value);
                }
              }
            }
          }
        }

        return editOperation.IsEmpty ? Task.FromResult(true) : editOperation.ExecuteAsync();
      });
    }

    protected static void ChangeHistoricalDate()
    {
      HistoricalDateChanged?.Invoke(YearMonth);
    }

    public static void ResetYears()
    {
      YearMonth.Clear();
    }

    #endregion

    #region Event handlers

    private async void OnMapViewCameraChanged(MapViewCameraChangedEventArgs args)
    {
      if (InsideScale)
      {
        FrameworkApplication.State.Activate("globeSpotterArcGISPro_InsideScaleState");
        MapView mapView = MapView.Active;

        if ((mapView != null) && (Layer != null) && (_cycloMediaGroupLayer != null) && (_addData == null))
        {
          const double epsilon = 0.0;
          var extent = mapView.Extent;

          if (((Math.Abs(extent.XMax - _lastextent.XMax) > epsilon) ||
               (Math.Abs(extent.YMin - _lastextent.YMin) > epsilon) ||
               (Math.Abs(extent.XMin - _lastextent.XMin) > epsilon) ||
               (Math.Abs(extent.YMax - _lastextent.YMax) > epsilon)))
          {
            _lastextent = extent;
            Envelope thisEnvelope = await GetExtentAsync(extent);

            if (thisEnvelope != null)
            {
              _addData = FeatureCollection.Load(thisEnvelope, WfsRequest);

              if ((_addData != null) && (_addData.NumberOfFeatures >= 1))
              {
                await SaveFeatureMembersAsync(_addData, thisEnvelope);
              }

              _addData = null;
              PostEntryStep();
            }
          }
        }
      }
      else
      {
        _addData = null;
        YearMonth.Clear();
        FrameworkApplication.State.Deactivate("globeSpotterArcGISPro_InsideScaleState");
      }
    }

    private void OnContentChanged(MapViewEventArgs mapViewEventArgs)
    {
      // todo: get color from layer
      LayerChangedEvent?.Invoke(this);
    }

    private async void OnLayersRemoved(LayerEventsArgs args)
    {
      IEnumerable<Layer> layers = args.Layers;
      bool contains = layers.Aggregate(false, (current, layer) => (layer == Layer) || current);

      if (contains)
      {
        await _cycloMediaGroupLayer.RemoveLayerAsync(Name, false);
      }
    }

    #endregion
  }
}
