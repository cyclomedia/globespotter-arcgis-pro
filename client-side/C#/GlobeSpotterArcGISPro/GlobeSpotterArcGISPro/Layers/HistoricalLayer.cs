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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Configuration.Remote.Recordings;

namespace GlobeSpotterArcGISPro.Layers
{
  public class HistoricalLayer : CycloMediaLayer
  {
    #region Members

    private static List<int> _yearPip;
    private static List<int> _yearForbidden;
    private static List<int> _years;
    private static double _minimumScale;

    private static readonly ConstantsRecordingLayer Constants;

    #endregion

    #region Properties

    public override string Name => Constants.HistoricalRecordingLayerName;
    public override string FcName => Constants.HistoricalRecordingLayerFeatureClassName;
    public override bool UseDateRange => true;

    public override string WfsRequest
      =>
        "<wfs:GetFeature service=\"WFS\" version=\"1.1.0\" resultType=\"results\" outputFormat=\"text/xml; subtype=gml/3.1.1\" xmlns:wfs=\"http://www.opengis.net/wfs\">" +
        "<wfs:Query typeName=\"atlas:Recording\" srsName=\"{0}\" xmlns:atlas=\"http://www.cyclomedia.com/atlas\"><ogc:Filter xmlns:ogc=\"http://www.opengis.net/ogc\">" +
        "<ogc:And><ogc:BBOX><gml:Envelope srsName=\"{0}\" xmlns:gml=\"http://www.opengis.net/gml\"><gml:lowerCorner>{1} {2}</gml:lowerCorner>" +
        "<gml:upperCorner>{3} {4}</gml:upperCorner></gml:Envelope></ogc:BBOX><ogc:PropertyIsBetween><ogc:PropertyName>recordedAt</ogc:PropertyName><ogc:LowerBoundary>" +
        "<ogc:Literal>1991-12-31T23:00:00-00:00</ogc:Literal></ogc:LowerBoundary><ogc:UpperBoundary><ogc:Literal>{5}</ogc:Literal></ogc:UpperBoundary></ogc:PropertyIsBetween>" +
        "</ogc:And></ogc:Filter></wfs:Query></wfs:GetFeature>";

    public override double MinimumScale
    {
      get { return _minimumScale; }
      set { _minimumScale = value; }
    }

    private static List<int> Years => _years ?? (_years = new List<int>());

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
          result = YearInsideRange(year, month);

          if (!YearMonth.ContainsKey(year))
          {
            YearMonth.Add(year, month);
            // ReSharper disable once ExplicitCallerInfoArgument
            NotifyPropertyChanged(nameof(YearMonth));
          }
          else
          {
            YearMonth[year] = month;
          }
        }
      }

      return result;
    }

    protected override async Task PostEntryStepAsync(Envelope envelope)
    {
      await QueuedTask.Run(() =>
      {
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
              SubFields = $"{Recording.FieldRecordedAt},{Recording.FieldPip},{Recording.FieldIsAuthorized}"
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
                    int month = dateTime.Month;
                    int calcYear = (year*4) + (int) Math.Floor(((double) (month - 1))/3);

                    if ((!Years.Contains(calcYear)) && (!added.Contains(calcYear)))
                    {
                      added.Add(calcYear);
                    }

                    object pipValue = row?.GetOriginalValue(pipId);

                    if (pipValue != null)
                    {
                      bool pip = bool.Parse((string) pipValue);

                      if (pip && (!YearPip.Contains(calcYear)) && (!pipAdded.Contains(calcYear)))
                      {
                        pipAdded.Add(calcYear);
                      }
                    }

                    object forbiddenValue = row?.GetOriginalValue(forbiddenId);

                    if (forbiddenValue != null)
                    {
                      bool forbidden = !bool.Parse((string) forbiddenValue);

                      if (forbidden && (!YearForbidden.Contains(calcYear)) && (!forbiddenAdded.Contains(calcYear)))
                      {
                        forbiddenAdded.Add(calcYear);
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
            bool realAdd = true;
            var newValue = (int) Math.Floor(((double) value)/4);

            for (int i = (newValue * 4); i < ((newValue * 4) + 4); i++)
            {
              realAdd = (!Years.Contains(i)) && realAdd;
            }

            Years.Add(value);

            if (realAdd)
            {
              Color color = GetCol(newValue);
              CIMColor cimColor = ColorFactory.CreateColor(color);
              var pointSymbol = SymbolFactory.ConstructPointSymbol(cimColor, Constants.SizeLayer, SimpleMarkerStyle.Circle);
              var pointSymbolReference = pointSymbol.MakeSymbolReference();

              CIMUniqueValue uniqueValue = new CIMUniqueValue
              {
                FieldValues = new[] {newValue.ToString(), false.ToString(), true.ToString()}
              };

              var uniqueValueClass = new CIMUniqueValueClass
              {
                Editable = true,
                Visible = true,
                Values = new[] {uniqueValue},
                Symbol = pointSymbolReference,
                Label = newValue.ToString(CultureInfo.InvariantCulture)
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
          }

          foreach (var value in pipAdded)
          {
            bool realAdd = true;
            var newValue = (int) Math.Floor(((double) value)/4);

            for (int i = (newValue * 4); i < ((newValue * 4) + 4); i++)
            {
              realAdd = (!YearPip.Contains(i)) && realAdd;
            }

            YearPip.Add(value);

            if (realAdd)
            {
              // ToDo: Add a rotation to the PIP symbols
              Color color = GetCol(newValue);
              CIMMarker marker = GetPipSymbol(color).Result;
              var pointSymbol = SymbolFactory.ConstructPointSymbol(marker);
              var pointSymbolReference = pointSymbol.MakeSymbolReference();

              CIMUniqueValue uniqueValue = new CIMUniqueValue
              {
                FieldValues = new[] {newValue.ToString(), true.ToString(), true.ToString()}
              };

              var uniqueValueClass = new CIMUniqueValueClass
              {
                Editable = true,
                Visible = true,
                Values = new[] {uniqueValue},
                Symbol = pointSymbolReference,
                Label = $"{newValue} (Detail images)"
              };

              CIMUniqueValueGroup uniqueValueGroup = new CIMUniqueValueGroup
              {
                Classes = new[] { uniqueValueClass },
                Heading = string.Empty
              };

              var groups = uniqueValueRenderer.Groups?.ToList() ?? new List<CIMUniqueValueGroup>();
              groups.Add(uniqueValueGroup);
              uniqueValueRenderer.Groups = groups.ToArray();
              Layer.SetRenderer(uniqueValueRenderer);
            }
          }

          foreach (var value in forbiddenAdded)
          {
            bool realAdd = true;
            var newValue = (int) Math.Floor(((double) value)/4);

            for (int i = (newValue * 4); i < ((newValue * 4) + 4); i++)
            {
              realAdd = (!YearForbidden.Contains(i)) && realAdd;
            }

            YearForbidden.Add(value);

            if (realAdd)
            {
              Color color = GetCol(newValue);
              CIMMarker marker = GetForbiddenSymbol(color).Result;
              var pointSymbol = SymbolFactory.ConstructPointSymbol(marker);
              var pointSymbolReference = pointSymbol.MakeSymbolReference();

              CIMUniqueValue uniqueValue = new CIMUniqueValue
              {
                FieldValues = new[] {newValue.ToString(), false.ToString(), false.ToString()}
              };

              CIMUniqueValue uniqueValuePip = new CIMUniqueValue
              {
                FieldValues = new[] {newValue.ToString(), true.ToString(), false.ToString()}
              };

              var uniqueValueClass = new CIMUniqueValueClass
              {
                Editable = true,
                Visible = true,
                Values = new[] {uniqueValue, uniqueValuePip},
                Symbol = pointSymbolReference,
                Label = $"{newValue} (No Authorization)"
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
          }

          var removed = (from yearColor in Years
                         select yearColor
                         into year
                         where ((!YearInsideRange((int) Math.Floor(((double) year)/4), (((year%4)*3) + 1))) && (!added.Contains(year)))
                         select year).ToList();

          foreach (var year in removed)
          {
            var newYear = (int) Math.Floor(((double) year)/4);

            if (YearPip.Contains(year))
            {
              string classValuePip = $"{newYear}, {true}, {true}";
              CIMUniqueValueGroup foundGroupPip =
                uniqueValueRenderer.Groups.Aggregate<CIMUniqueValueGroup, CIMUniqueValueGroup>(null,
                  (current3, @group) =>
                    @group.Classes.Aggregate(current3,
                      (current2, valueClass) =>
                        valueClass.Values.Aggregate(current2,
                          (current1, uniqueValue) =>
                            uniqueValue.FieldValues.Aggregate(current1,
                              (current, fieldValue) => (fieldValue == classValuePip) ? @group : current))));

              if (foundGroupPip != null)
              {
                var groups = uniqueValueRenderer.Groups.ToList();
                groups.Remove(foundGroupPip);
                uniqueValueRenderer.Groups = groups.ToArray();
                Layer.SetRenderer(uniqueValueRenderer);
              }

              YearPip.Remove(year);
            }

            string classValue = $"{newYear}, {false}, {true}";
            CIMUniqueValueGroup foundGroup =
              uniqueValueRenderer.Groups.Aggregate<CIMUniqueValueGroup, CIMUniqueValueGroup>(null,
                (current3, @group) =>
                  @group.Classes.Aggregate(current3,
                    (current2, valueClass) =>
                      valueClass.Values.Aggregate(current2,
                        (current1, uniqueValue) =>
                          uniqueValue.FieldValues.Aggregate(current1,
                            (current, fieldValue) => (fieldValue == classValue) ? @group : current))));

            if (foundGroup != null)
            {
              var groups = uniqueValueRenderer.Groups.ToList();
              groups.Remove(foundGroup);
              uniqueValueRenderer.Groups = groups.ToArray();
              Layer.SetRenderer(uniqueValueRenderer);
            }

            Years.Remove(year);
          }
        }
      });
    }

    protected override void Remove()
    {
      base.Remove();
      ClearYears();
    }

    protected override void ClearYears()
    {
      Years.Clear();
      YearPip.Clear();
      YearForbidden.Clear();
    }

    private bool YearInsideRange(int year, int month)
    {
      HistoricalRecordings historicalRecordings = HistoricalRecordings.Instance;
      var fromDateTime = historicalRecordings.DateFrom;
      var toDateTime = historicalRecordings.DateTo;
      var checkDateTime = new DateTime(year, month, 1);
      return (checkDateTime.CompareTo(fromDateTime) >= 0) && (checkDateTime.CompareTo(toDateTime) < 0);
    }

    #endregion

    #region Constructors

    static HistoricalLayer()
    {
      Constants = ConstantsRecordingLayer.Instance;
      _minimumScale = Constants.MinimumScale;
    }

    public HistoricalLayer(CycloMediaGroupLayer layer)
      : base(layer)
    {
    }

    #endregion
  }
}
