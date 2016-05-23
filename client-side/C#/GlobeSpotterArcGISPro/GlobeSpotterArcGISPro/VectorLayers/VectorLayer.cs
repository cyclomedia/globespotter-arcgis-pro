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
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GlobeSpotterAPI;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Configuration.File.Layers;
using GlobeSpotterArcGISPro.Overlays;

using MySpatialReference = GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference.SpatialReference;

namespace GlobeSpotterArcGISPro.VectorLayers
{
  public class VectorLayer : INotifyPropertyChanged
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

    private bool _isVisibleInGlobespotter;
    private string _gml;
    private IList<Feature> _editFeatures;

    #endregion

    #region Constructor

    public VectorLayer(FeatureLayer layer)
    {
      Layer = layer;
      _settings = Settings.Instance;
      _storedLayerList = StoredLayerList.Instance;
      _isVisibleInGlobespotter = _storedLayerList.Get(Layer?.Name ?? string.Empty);
      LayerId = null;
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

    public IList<Feature> EditFeatures => _editFeatures ?? (_editFeatures = new List<Feature>());

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

    #endregion

    #region Functions

    public async Task<string> GetGmlFromLocationAsync(ICollection<Viewer> viewers)
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

      await QueuedTask.Run(() =>
      {
        SpatialReference layerSpatRef = Layer.GetSpatialReference();
        IList<IList<Segment>> geometries = new List<IList<Segment>>();

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
              Feature feature = row as Feature;

              if (!EditFeatures.Contains(feature))
              {
                long objectId = row.GetObjectID();
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
                          $"<gml:Point {GmlDimension(copyGeometry)}><gml:coordinates>{GmlPoint(point)}</gml:coordinates></gml:Point>";
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
                              gml = $"{gml}{((i == 0) ? string.Empty : " ")}{GmlPoint(polygonPoint)}";

                              if (i == (segments.Count - 1))
                              {
                                polygonPoint = segments[i].EndPoint;
                                gml = $"{gml} {GmlPoint(polygonPoint)}";
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
                              gml = $"{gml}{((i == 0) ? string.Empty : " ")}{GmlPoint(linePoint)}";

                              if (i == (segments.Count - 1))
                              {
                                linePoint = segments[i].EndPoint;
                                gml = $"{gml} {GmlPoint(linePoint)}";
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

    private string GmlPoint(MapPoint point)
    {
      CultureInfo ci = CultureInfo.InvariantCulture;
      return $"{point.X.ToString(ci)},{point.Y.ToString(ci)}{(point.HasZ ? $",{point.Z.ToString(ci)}" : string.Empty)}";
    }

    private string GmlDimension(Geometry geometry)
    {
      return $"srsDimension = \"{(geometry.HasZ ? 3 : 2)}\"";
    }

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
