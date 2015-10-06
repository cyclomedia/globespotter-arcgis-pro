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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference
{
  [XmlType(AnonymousType = true, Namespace = "https://www.globespotter.com/gsc")]
  [XmlRoot(Namespace = "https://www.globespotter.com/gsc", IsNullable = false)]
  public class SpatialReferences : List<SpatialReference>
  {
    #region Members

    private static readonly XmlSerializer XmlSpatialReferences;
    private static readonly Web Web;

    private static SpatialReferences _spatialReferences;

    #endregion

    #region Constructor

    static SpatialReferences()
    {
      XmlSpatialReferences = new XmlSerializer(typeof (SpatialReferences));
      Web = Web.Instance;
    }

    #endregion

    #region properties

    public SpatialReference[] SpatialReference
    {
      get { return ToArray(); }
      set
      {
        if (value != null)
        {
          AddRange(value);
        }
      }
    }

    public static SpatialReferences Instance
    {
      get
      {
        if ((_spatialReferences == null) || (_spatialReferences.Count == 0))
        {
          try
          {
            Load();
          }
          // ReSharper disable once EmptyGeneralCatchClause
          catch
          {
          }
        }

        return _spatialReferences ?? (_spatialReferences = new SpatialReferences());
      }
    }

    #endregion

    #region Functions

    public SpatialReference GetItem(string srsName)
    {
      return this.Aggregate<SpatialReference, SpatialReference>
        (null, (current, spatialReference) => (spatialReference.SRSName == srsName) ? spatialReference : current);
    }

    public SpatialReference GetCompatibleSrsNameItem(string srsName)
    {
      return this.Aggregate<SpatialReference, SpatialReference>
        (null, (current, spatialReference) => (spatialReference.CompatibleSRSNames == srsName) ? spatialReference : current);
    }

    public string ToKnownSrsName(string srsName)
    {
      SpatialReference spatRef = GetCompatibleSrsNameItem(srsName);
      return (spatRef == null) ? srsName : spatRef.SRSName;
    }

    public static SpatialReferences Load()
    {
      Stream spatialRef = Web.SpatialReferences();

      if (spatialRef != null)
      {
        spatialRef.Position = 0;
        _spatialReferences = (SpatialReferences) XmlSpatialReferences.Deserialize(spatialRef);
        spatialRef.Close();
      }

      return _spatialReferences;
    }

    #endregion
  }
}
