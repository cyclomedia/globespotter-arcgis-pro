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
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GlobeSpotterArcGISPro.Configuration.Remote.Recordings;

namespace GlobeSpotterArcGISPro.Layers
{
  public class RecordingLayer : CycloMediaLayer
  {
    #region Members

    private static List<int> _yearPip;
    private static List<int> _yearForbidden;
    private static SortedDictionary<int, Color> _yearToColor;
    private static double _minimumScale;

    #endregion

    #region Properties

    public override string Name => "Recent Recordings";
    public override string FcName => "FCRecentRecordings";
    public override bool UseDateRange => false;

    public override string WfsRequest
      =>
        "<wfs:GetFeature service=\"WFS\" version=\"1.1.0\" resultType=\"results\" outputFormat=\"text/xml; subtype=gml/3.1.1\" xmlns:wfs=\"http://www.opengis.net/wfs\">" +
        "<wfs:Query typeName=\"atlas:Recording\" srsName=\"{0}\" xmlns:atlas=\"http://www.cyclomedia.com/atlas\"><ogc:Filter xmlns:ogc=\"http://www.opengis.net/ogc\">" +
        "<ogc:And><ogc:BBOX><gml:Envelope srsName=\"{0}\" xmlns:gml=\"http://www.opengis.net/gml\"><gml:lowerCorner>{1} {2}</gml:lowerCorner>" +
        "<gml:upperCorner>{3} {4}</gml:upperCorner></gml:Envelope></ogc:BBOX><ogc:PropertyIsNull><ogc:PropertyName>expiredAt</ogc:PropertyName></ogc:PropertyIsNull>" +
        "</ogc:And></ogc:Filter></wfs:Query></wfs:GetFeature>";

    public override double MinimumScale
    {
      get { return _minimumScale; }
      set { _minimumScale = value; }
    }

    private static SortedDictionary<int, Color> YearToColor => _yearToColor ?? (_yearToColor = new SortedDictionary<int, Color>());

    private static List<int> YearPip => _yearPip ?? (_yearPip = new List<int>());

    private static List<int> YearForbidden => _yearForbidden ?? (_yearForbidden = new List<int>());

    #endregion

    #region Functions

    protected override bool Filter(Recording recording)
    {
      bool result = (recording != null);

      if (result)
      {
        DateTime? recordedAt = recording.RecordedAt;
        result = (recordedAt != null);

        if (result)
        {
          var dateTime = (DateTime) recordedAt;
          int year = dateTime.Year;
          int month = dateTime.Month;

          if (!YearMonth.ContainsKey(year))
          {
            YearMonth.Add(year, month);
            ChangeHistoricalDate();
          }
          else
          {
            YearMonth[year] = month;
          }
        }
      }

      return result;
    }

    protected async override Task PostEntryStepAsync()
    {
      await QueuedTask.Run(() =>
      {
        MapView activeView = MapView.Active;
        Envelope envelope = activeView.Extent;

        var added = new List<int>();
        var pipAdded = new List<int>();
        var forbiddenAdded = new List<int>();

        using (FeatureClass featureClass = Layer?.GetFeatureClass())
        {
          if (featureClass != null)
          {
            SpatialQueryFilter spatialFilter = new SpatialQueryFilter
            {
              FilterGeometry = envelope,
              SpatialRelationship = SpatialRelationship.Contains,
              SubFields = $"{Recording.FieldRecordedAt},{Recording.FieldRecordedAt},{Recording.FieldIsAuthorized}"
            };

            using (RowCursor existsResult = featureClass.Search(spatialFilter, false))
            {
              int imId = existsResult.FindField(Recording.FieldRecordedAt);
              int pipId = existsResult.FindField(Recording.FieldPip);
              int forbiddenId = existsResult.FindField(Recording.FieldIsAuthorized);

              while (existsResult.MoveNext())
              {
                using (Row row = existsResult.Current)
                {
                  object value = row?.GetOriginalValue(imId);

                  if (value != null)
                  {
                    var dateTime = (DateTime) value;
                    int year = dateTime.Year;

                    if (!YearToColor.ContainsKey(year))
                    {
                      YearToColor.Add(year, Color.Transparent);
                      added.Add(year);
                    }

                    object pipValue = row?.GetOriginalValue(pipId);

                    if (pipValue != null)
                    {
                      bool pip = bool.Parse((string) pipValue);

                      if (pip && (!YearPip.Contains(year)))
                      {
                        YearPip.Add(year);
                        pipAdded.Add(year);
                      }
                    }

                    object forbiddenValue = row?.GetOriginalValue(forbiddenId);

                    if (forbiddenValue != null)
                    {
                      bool forbidden = !bool.Parse((string) forbiddenValue);

                      if (forbidden && (!YearForbidden.Contains(year)))
                      {
                        YearForbidden.Add(year);
                        forbiddenAdded.Add(year);
                      }
                    }
                  }
                }
              }
            }
          }
        }

        CIMRenderer featureRenderer = Layer?.GetRenderer();
        var uniqueValueRenderer = featureRenderer as CIMUniqueValueRenderer;

        if (uniqueValueRenderer != null)
        {
          foreach (var value in added)
          {
            Color color = GetCol(value);
            CIMColor cimColor = ColorFactory.CreateColor(color);
            var pointSymbol = SymbolFactory.ConstructPointSymbol(cimColor, SizeLayer, SimpleMarkerStyle.Circle);
            var pointSymbolReference = pointSymbol.MakeSymbolReference();

            CIMUniqueValue uniqueValue = new CIMUniqueValue
            {
              FieldValues = new[] {value.ToString(), false.ToString(), true.ToString()}
            };

            var uniqueValueClass = new CIMUniqueValueClass
            {
              Values = new[] {uniqueValue},
              Symbol = pointSymbolReference,
              Label = value.ToString(CultureInfo.InvariantCulture)
            };

            CIMUniqueValueGroup uniqueValueGroup = new CIMUniqueValueGroup
            {
              Classes = new[] {uniqueValueClass},
              Heading = string.Empty
            };

            var groups = uniqueValueRenderer.Groups?.ToList() ?? new List<CIMUniqueValueGroup>();
            groups.Add(uniqueValueGroup);
            uniqueValueRenderer.Groups = groups.ToArray();
            Layer.SetRenderer(uniqueValueRenderer);
          }

          foreach (var value in pipAdded)
          {
            // toDo: PIP images
          }

          foreach (var value in forbiddenAdded)
          {
            // toDo: Forbidden images
          }
        }
      });
    }

    protected override void Remove()
    {
      base.Remove();
      YearToColor.Clear();
      YearPip.Clear();
      YearForbidden.Clear();
    }

    public override void UpdateColor(Color color, int? year)
    {
      // todo: Add this function
    }

    public override DateTime? GetDate()
    {
      // toDo: Add this function
      return null;
    }

    public override double GetHeight(double x, double y)
    {
      // toDo: Add this function
      return 0.0;
    }

    #endregion

    #region Constructors

    static RecordingLayer()
    {
      _minimumScale = 2000.0;
    }

    public RecordingLayer(CycloMediaGroupLayer layer)
      : base(layer)
    {
    }

    #endregion
  }
}