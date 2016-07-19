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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using GlobeSpotterAPI;

using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Configuration.File.Layers;
using GlobeSpotterArcGISPro.Configuration.Remote.GlobeSpotter;
using GlobeSpotterArcGISPro.Overlays;
using GlobeSpotterArcGISPro.Overlays.Measurement;

using MySpatialReference = GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference.SpatialReference;

namespace GlobeSpotterArcGISPro.VectorLayers
{
  public class VectorLayer : INotifyPropertyChanged, IDisposable
  {
    #region Consts

    public const string FieldUri = "URI";
    public const string FieldObjectId = "ObjectId";

    #endregion

    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region Members

    private readonly Settings _settings;
    private readonly StoredLayerList _storedLayerList;
    private readonly ViewerList _viewerList;
    private readonly MeasurementList _measurementList;
    private readonly VectorLayerList _vectorLayerList;
    private readonly CultureInfo _ci;

    private bool _isVisibleInGlobespotter;
    private string _gml;
    private IList<long> _selection;
    private List<Measurement> _measurements;
    private bool _updateMeasurements;

    #endregion

    #region Constructor

    public VectorLayer(FeatureLayer layer, VectorLayerList vectorLayerList)
    {
      _vectorLayerList = vectorLayerList;
      Layer = layer;
      _settings = Settings.Instance;
      _storedLayerList = StoredLayerList.Instance;
      _isVisibleInGlobespotter = _storedLayerList.Get(Layer?.Name ?? string.Empty);
      LayerId = null;
      _selection = null;
      _updateMeasurements = false;

      GlobeSpotter globeSpotter = GlobeSpotter.Current;
      _viewerList = globeSpotter.ViewerList;
      _measurementList = globeSpotter.MeasurementList;
      _ci = CultureInfo.InvariantCulture;

      MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
      DrawCompleteEvent.Subscribe(OnDrawCompleted);

      QueuedTask.Run(async () =>
      {
        var table = layer.GetTable();
        RowChangedEvent.Subscribe(OnRowChanged, table);
        RowDeletedEvent.Subscribe(OnRowDeleted, table);
        RowCreatedEvent.Subscribe(OnRowCreated, table);
        await LoadMeasurementsAsync();
      });
    }

    #endregion

    #region Properties

    public string Gml
    {
      get { return _gml; }
      private set
      {
        _gml = value;
        NotifyPropertyChanged();
      }
    }

    public Color Color { get; private set; }

    public FeatureLayer Layer { get; }

    public uint? LayerId { get; set; }

    public bool GmlChanged { get; private set; }

    public string Name => (Layer?.Name ?? string.Empty);

    public bool IsVisible => (Layer != null) && Layer.IsVisible;

    public bool IsVisibleInGlobespotter
    {
      get { return (_isVisibleInGlobespotter && IsVisible); }
      set
      {
        _isVisibleInGlobespotter = value;
        _storedLayerList.Update(Name, value);
        NotifyPropertyChanged();
      }
    }

    public List<Measurement> Measurements
    {
      get { return _measurements; }
      set
      {
        _measurements = value;
        NotifyPropertyChanged();
      }
    }

    #endregion

    #region Functions

    public async Task<string> GenerateGmlAsync()
    {
      MapView mapView = MapView.Active;
      Map map = mapView?.Map;
      SpatialReference mapSpatRef = map?.SpatialReference;
      MySpatialReference myCyclSpatRef = _settings.CycloramaViewerCoordinateSystem;

      SpatialReference cyclSpatRef = (myCyclSpatRef == null)
        ? mapSpatRef
        : (myCyclSpatRef.ArcGisSpatialReference ?? (await myCyclSpatRef.CreateArcGisSpatialReferenceAsync()));

      Unit unit = cyclSpatRef?.Unit;
      double factor = unit?.ConversionFactor ?? 1;
      Color color = Color.White;
      string result =
        "<wfs:FeatureCollection xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:wfs=\"http://www.opengis.net/wfs\" xmlns:gml=\"http://www.opengis.net/gml\">";

      await QueuedTask.Run(async () =>
      {
        SpatialReference layerSpatRef = Layer.GetSpatialReference();
        IList<IList<Segment>> geometries = new List<IList<Segment>>();
        ICollection<Viewer> viewers = _viewerList.Viewers;

        foreach (var viewer in viewers)
        {
          double distance = viewer.OverlayDrawDistance;
          RecordingLocation recordingLocation = viewer.Location;

          if (recordingLocation != null)
          {
            if (cyclSpatRef?.IsGeographic ?? true)
            {
              distance = distance*factor;
            }
            else
            {
              distance = distance/factor;
            }

            double x = recordingLocation.X;
            double y = recordingLocation.Y;
            double xMin = x - distance;
            double xMax = x + distance;
            double yMin = y - distance;
            double yMax = y + distance;

            Envelope envelope = EnvelopeBuilder.CreateEnvelope(xMin, yMin, xMax, yMax, cyclSpatRef);
            Envelope copyEnvelope = envelope;

            if (layerSpatRef.Wkid != 0)
            {
              ProjectionTransformation projection = ProjectionTransformation.Create(cyclSpatRef, layerSpatRef);
              copyEnvelope = GeometryEngine.ProjectEx(envelope, projection) as Envelope;
            }

            Polygon copyPolygon = PolygonBuilder.CreatePolygon(copyEnvelope, layerSpatRef);
            ReadOnlyPartCollection polygonParts = copyPolygon.Parts;
            IEnumerator<ReadOnlySegmentCollection> polygonSegments = polygonParts.GetEnumerator();
            IList<Segment> segments = new List<Segment>();

            while (polygonSegments.MoveNext())
            {
              ReadOnlySegmentCollection polygonSegment = polygonSegments.Current;

              foreach (Segment segment in polygonSegment)
              {
                segments.Add(segment);
              }
            }

            geometries.Add(segments);
          }
        }

        GC.Collect();
        Polygon polygon = PolygonBuilder.CreatePolygon(geometries, layerSpatRef);

        using (FeatureClass featureClass = Layer?.GetFeatureClass())
        {
          string uri = Layer?.URI;

          SpatialQueryFilter spatialFilter = new SpatialQueryFilter
          {
            FilterGeometry = polygon,
            SpatialRelationship = SpatialRelationship.Intersects,
            SubFields = "*"
          };

          using (RowCursor existsResult = featureClass?.Search(spatialFilter, false))
          {
            while (existsResult?.MoveNext() ?? false)
            {
              Row row = existsResult.Current;
              long objectId = row.GetObjectID();

              if ((_selection == null) || (!_selection.Contains(objectId)))
              {
                Feature feature = row as Feature;
                var fieldvalues = new Dictionary<string, string> {{FieldUri, uri}, {FieldObjectId, objectId.ToString()}};

                Geometry geometry = feature?.GetShape();
                GeometryType geometryType = geometry?.GeometryType ?? GeometryType.Unknown;
                Geometry copyGeometry = geometry;

                if ((geometry != null) && (layerSpatRef.Wkid != 0))
                {
                  ProjectionTransformation projection = ProjectionTransformation.Create(layerSpatRef, cyclSpatRef);
                  copyGeometry = GeometryEngine.ProjectEx(geometry, projection);
                }

                if (copyGeometry != null)
                {
                  string gml = string.Empty;

                  switch (geometryType)
                  {
                    case GeometryType.Envelope:
                      break;
                    case GeometryType.Multipatch:
                      break;
                    case GeometryType.Multipoint:
                      break;
                    case GeometryType.Point:
                      MapPoint point = copyGeometry as MapPoint;

                      if (point != null)
                      {
                        gml =
                          $"<gml:Point {GmlDimension(copyGeometry)}><gml:coordinates>{await GmlPointAsync(point)}</gml:coordinates></gml:Point>";
                      }

                      break;
                    case GeometryType.Polygon:
                      Polygon polygonGml = copyGeometry as Polygon;

                      if (polygonGml != null)
                      {
                        ReadOnlyPartCollection polygonParts = polygonGml.Parts;
                        IEnumerator<ReadOnlySegmentCollection> polygonSegments = polygonParts.GetEnumerator();

                        while (polygonSegments.MoveNext())
                        {
                          ReadOnlySegmentCollection segments = polygonSegments.Current;

                          gml =
                            $"{gml}<gml:MultiPolygon><gml:PolygonMember><gml:Polygon {GmlDimension(copyGeometry)}><gml:outerBoundaryIs><gml:LinearRing><gml:coordinates>";

                          for (int i = 0; i < segments.Count; i++)
                          {
                            if (segments[i].SegmentType == SegmentType.Line)
                            {
                              MapPoint polygonPoint = segments[i].StartPoint;
                              gml = $"{gml}{((i == 0) ? string.Empty : " ")}{await GmlPointAsync(polygonPoint)}";

                              if (i == (segments.Count - 1))
                              {
                                polygonPoint = segments[i].EndPoint;
                                gml = $"{gml} {await GmlPointAsync(polygonPoint)}";
                              }
                            }
                          }

                          gml =
                            $"{gml}</gml:coordinates></gml:LinearRing></gml:outerBoundaryIs></gml:Polygon></gml:PolygonMember></gml:MultiPolygon>";
                        }
                      }
                      break;
                    case GeometryType.Polyline:
                      Polyline polylineGml = copyGeometry as Polyline;

                      if (polylineGml != null)
                      {
                        ReadOnlyPartCollection polylineParts = polylineGml.Parts;
                        IEnumerator<ReadOnlySegmentCollection> polylineSegments = polylineParts.GetEnumerator();

                        while (polylineSegments.MoveNext())
                        {
                          ReadOnlySegmentCollection segments = polylineSegments.Current;
                          gml =
                            $"{gml}<gml:MultiLineString><gml:LineStringMember><gml:LineString {GmlDimension(copyGeometry)}><gml:coordinates>";

                          for (int i = 0; i < segments.Count; i++)
                          {
                            if (segments[i].SegmentType == SegmentType.Line)
                            {
                              MapPoint linePoint = segments[i].StartPoint;
                              gml = $"{gml}{((i == 0) ? string.Empty : " ")}{await GmlPointAsync(linePoint)}";

                              if (i == (segments.Count - 1))
                              {
                                linePoint = segments[i].EndPoint;
                                gml = $"{gml} {await GmlPointAsync(linePoint)}";
                              }
                            }
                          }

                          gml = $"{gml}</gml:coordinates></gml:LineString></gml:LineStringMember></gml:MultiLineString>";
                        }
                      }

                      break;
                    case GeometryType.Unknown:
                      break;
                  }

                  string fieldValueStr = fieldvalues.Aggregate(string.Empty,
                    (current, fieldvalue) =>
                      string.Format("{0}<{1}>{2}</{1}>", current, fieldvalue.Key, fieldvalue.Value));
                  result =
                    $"{result}<gml:featureMember><xs:Geometry>{fieldValueStr}{gml}</xs:Geometry></gml:featureMember>";
                }
              }
            }
          }
        }

        CIMRenderer renderer = Layer.GetRenderer();
        CIMSimpleRenderer simpleRenderer = renderer as CIMSimpleRenderer;
        CIMUniqueValueRenderer uniqueValueRendererRenderer = renderer as CIMUniqueValueRenderer;
        CIMSymbolReference symbolRef = simpleRenderer?.Symbol ?? uniqueValueRendererRenderer?.DefaultSymbol;
        CIMSymbol symbol = symbolRef?.Symbol;
        CIMColor cimColor = symbol?.GetColor();
        double[] colorValues = cimColor?.Values;

        int red = ((colorValues != null) && (colorValues.Length >= 1)) ? ((int) colorValues[0]) : 255;
        int green = ((colorValues != null) && (colorValues.Length >= 2)) ? ((int) colorValues[1]) : 255;
        int blue = ((colorValues != null) && (colorValues.Length >= 3)) ? ((int) colorValues[2]) : 255;
        int alpha = ((colorValues != null) && (colorValues.Length >= 4)) ? ((int) colorValues[3]) : 255;
        color = Color.FromArgb(alpha, red, green, blue);
      });

      GmlChanged = (Color != color);
      Color = color;
      string newGml = $"{result}</wfs:FeatureCollection>";
      GmlChanged = ((newGml != Gml) || GmlChanged);
      return (Gml = newGml);
    }

    public async Task<double> GetOffsetZAsync()
    {
      return await QueuedTask.Run(() =>
      {
        CIMBaseLayer cimBaseLayer = Layer?.GetDefinition();
        CIMBasicFeatureLayer cimBasicFeatureLayer = cimBaseLayer as CIMBasicFeatureLayer;
        CIMLayerElevationSurface layerElevation = cimBasicFeatureLayer?.LayerElevation;
        return (layerElevation?.OffsetZ ?? 0.0);
      });
    }

    private async Task<string> GmlPointAsync(MapPoint point)
    {
      bool hasZ = point.HasZ;
      double z = hasZ ? (point.Z + (await GetOffsetZAsync())) : 0.0;
      return $"{point.X.ToString(_ci)},{point.Y.ToString(_ci)}{(hasZ ? $",{z.ToString(_ci)}" : string.Empty)}";
    }

    private string GmlDimension(Geometry geometry)
    {
      return $"srsDimension = \"{(geometry.HasZ ? 3 : 2)}\"";
    }

    public async Task LoadMeasurementsAsync()
    {
      await ReloadSelectionAsync();

      if (_measurementList.Count >= 1)
      {
        _selection = new List<long>();
      }

      foreach (KeyValuePair<int, Measurement> keyValue in _measurementList)
      {
        Measurement measurement = keyValue.Value;
        long? objectId = measurement?.ObjectId;

        if (objectId != null)
        {
          _selection.Add((long)objectId);
        }
      }
    }

    public async Task AddFeatureAsync(Geometry geometry)
    {
      var editOperation = new EditOperation
      {
        Name = $"Add feature to layer: {Name}",
        SelectNewFeatures = true,
        ShowModalMessageAfterFailure = false
      };

      editOperation.Create(Layer, geometry);
      await editOperation.ExecuteAsync();
    }

    public async Task UpdateFeatureAsync(long uid, Geometry geometry)
    {
      await QueuedTask.Run(() =>
      {
        using (FeatureClass featureClass = Layer.GetFeatureClass())
        {
          FeatureClassDefinition definition = featureClass?.GetDefinition();
          string objectIdField = definition?.GetObjectIDField();
          QueryFilter filter = new QueryFilter {WhereClause = $"{objectIdField} = {uid}"};

          using (RowCursor existsResult = featureClass?.Search(filter, false))
          {
            while (existsResult?.MoveNext() ?? false)
            {
              using (Row row = existsResult.Current)
              {
                Feature feature = row as Feature;
                feature?.SetShape(geometry);
                feature?.Store();
              }
            }
          }
        }
      });
    }

    private async Task<List<Measurement>> ReloadSelectionAsync()
    {
      List<Measurement> result = new List<Measurement>();
      bool thisMeasurement = false;

      await QueuedTask.Run(async () =>
      {
        Selection selectionFeatures = Layer?.GetSelection();

        using (RowCursor rowCursur = selectionFeatures?.Search())
        {
          while (rowCursur?.MoveNext() ?? false)
          {
            Row row = rowCursur.Current;
            Feature feature = row as Feature;
            Geometry geometry = feature?.GetShape();
            long objectId = feature?.GetObjectID() ?? -1;

            if ((geometry != null) && (objectId != -1))
            {
              Measurement measurement = _measurementList.Get(objectId) ?? (await _measurementList.GetAsync(geometry));
              thisMeasurement = true;
              _measurementList.DrawPoint = false;
              measurement = _measurementList.StartMeasurement(geometry, measurement, false, objectId, this);
              _measurementList.DrawPoint = true;

              if (measurement != null)
              {
                await measurement.UpdateMeasurementPointsAsync(geometry);
                measurement.CloseMeasurement();
                result.Add(measurement);
              }
            }
          }
        }
      });

      if (thisMeasurement && (_vectorLayerList.EditTool == EditTools.SketchPointTool))
      {
        _vectorLayerList.SketchFinished();
        await _vectorLayerList.StartSketchToolAsync();
      }

      return result;
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Edit events

    private async void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
    {
      bool contains = false;

      foreach (var selection in args.Selection)
      {
        MapMember mapMember = selection.Key;
        FeatureLayer layer = mapMember as FeatureLayer;

        if (layer == Layer)
        {
          _selection = selection.Value;
          contains = true;
          await GenerateGmlAsync();

          if (IsVisibleInGlobespotter)
          {
            if (_vectorLayerList.EditTool != EditTools.SketchPointTool)
            {
              Measurements = await ReloadSelectionAsync();
            }
            else
            {
              _updateMeasurements = true;
            }
          }
        }
      }

      if ((!contains) && (_selection != null))
      {
        Measurements = null;
        _selection = null;
        await GenerateGmlAsync();
      }
    }

    private async void OnRowChanged(RowChangedEventArgs args)
    {
      if (IsVisibleInGlobespotter)
      {
        await QueuedTask.Run(async () =>
        {
          Row row = args.Row;
          Feature feature = row as Feature;
          Geometry geometry = feature?.GetShape();
          long objectId = feature?.GetObjectID() ?? -1;
          Measurement measurement = _measurementList.Get(objectId);
          _measurementList.DrawPoint = false;
          measurement = _measurementList.StartMeasurement(geometry, measurement, false, objectId, this);
          _measurementList.DrawPoint = true;

          if (measurement != null)
          {
            await measurement.UpdateMeasurementPointsAsync(geometry);
            measurement.CloseMeasurement();
          }
        });
      }
    }

    private async void OnRowDeleted(RowChangedEventArgs args)
    {
      await QueuedTask.Run(() =>
      {
        Row row = args.Row;
        Feature feature = row as Feature;
        long objectId = feature?.GetObjectID() ?? -1;

        if (_selection?.Contains(objectId) ?? false)
        {
          _selection.Remove(objectId);
        }

        if (IsVisibleInGlobespotter && GlobeSpotterConfiguration.MeasurePermissions)
        {
          Measurement measurement = _measurementList.Get(objectId);
          measurement?.RemoveMeasurement();
        }
      });
    }

    private async void OnRowCreated(RowChangedEventArgs args)
    {
      await QueuedTask.Run(async () =>
      {
        Row row = args.Row;
        Feature feature = row as Feature;
        Geometry geometry = feature?.GetShape();
        const double e = 0.1;

        if (geometry?.GeometryType == GeometryType.Point)
        {
          MapPoint srcPoint = geometry as MapPoint;

          if ((srcPoint != null) && (Math.Abs(srcPoint.Z) < e))
          {
            MapPoint dstPoint = await _vectorLayerList.AddHeightToMapPointAsync(srcPoint);
            ElevationCapturing.ElevationConstantValue = dstPoint.Z;
            feature.SetShape(dstPoint);
          }
        }
      });
    }

    private async void OnDrawCompleted(MapViewEventArgs args)
    {
      MapView mapView = args.MapView;
      Geometry sketchGeometry = await mapView.GetCurrentSketchAsync();

      if (sketchGeometry == null)
      {
        await GenerateGmlAsync();
      }

      if (_updateMeasurements)
      {
        _updateMeasurements = false;
        Measurements = await ReloadSelectionAsync();
      }
    }

    #endregion

    #region Disposable

    public void Dispose()
    {
      MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChanged);
      DrawCompleteEvent.Unsubscribe(OnDrawCompleted);
    }

    #endregion
  }
}
