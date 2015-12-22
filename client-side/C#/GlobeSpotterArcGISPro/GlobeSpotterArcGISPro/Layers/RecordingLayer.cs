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

using System;
using System.Collections.Generic;
using System.Drawing;
using GlobeSpotterArcGISPro.Configuration.Remote.Recordings;

namespace GlobeSpotterArcGISPro.Layers
{
  public class RecordingLayer : CycloMediaLayer
  {
    #region Members

    private static Color _color;
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

    public override Color Color
    {
      get { return _color; }
      set { _color = value; }
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

    protected override void PostEntryStep()
    {
      // todo: Add this function
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
      _color = Color.Transparent;
      _minimumScale = 2000.0;
    }

    public RecordingLayer(CycloMediaGroupLayer layer)
      : base(layer)
    {
    }

    #endregion
  }
}
