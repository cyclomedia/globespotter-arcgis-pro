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
using System.Xml.Serialization;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

using ArcGISSpatialReference = ArcGIS.Core.Geometry.SpatialReference;

namespace GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference
{
  public delegate void SpatialReferenceExistsInAreaDelegate(SpatialReference spatialReference, bool exists);

  public class SpatialReference
  {
    #region Events

    public event SpatialReferenceExistsInAreaDelegate ExistsInAreaEvent;

    #endregion

    #region Members

    private ArcGISSpatialReference _spatialReference;

    #endregion

    #region Properties

    // ReSharper disable InconsistentNaming
    public string Name { get; set; }

    public string SRSName { get; set; }

    public string Units { get; set; }

    public Bounds NativeBounds { get; set; }

    public string ESRICompatibleName { get; set; }

    public string CompatibleSRSNames { get; set; }
    // ReSharper restore InconsistentNaming

    [XmlIgnore]
    public bool CanMeasuring
    {
      get { return ((Units == "m") || (Units == "ft")); }
    }

    /// <summary>
    /// asynchronous function to request this or spatial reference exists in the current area
    /// </summary>
    public void ExistsInArea()
    {
      if (_spatialReference == null)
      {
        CreateSpatialReference();
      }
      else
      {
        FilterSpatialReference();
      }
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return string.Format("{0} ({1})", (string.IsNullOrEmpty(ESRICompatibleName) ? Name : ESRICompatibleName), SRSName);
    }

    #endregion

    #region tasks

    private void CreateSpatialReference()
    {
      QueuedTask.Run(() =>
      {
        if (string.IsNullOrEmpty(SRSName))
        {
          _spatialReference = null;
        }
        else
        {
          int srs;
          string strsrs = SRSName.Replace("EPSG:", string.Empty);

          if (int.TryParse(strsrs, out srs))
          {
            try
            {
              _spatialReference = SpatialReferenceBuilder.CreateSpatialReference(srs);
            }
            catch (ArgumentException)
            {
              if (string.IsNullOrEmpty(CompatibleSRSNames))
              {
                _spatialReference = null;
              }
              else
              {
                strsrs = CompatibleSRSNames.Replace("EPSG:", string.Empty);

                if (int.TryParse(strsrs, out srs))
                {
                  try
                  {
                    _spatialReference = SpatialReferenceBuilder.CreateSpatialReference(srs);
                  }
                  catch (ArgumentException)
                  {
                    _spatialReference = null;
                  }
                }
                else
                {
                  _spatialReference = null;
                }
              }
            }
          }
          else
          {
            _spatialReference = null;
          }
        }

        FilterSpatialReference();
      });
    }

    private void FilterSpatialReference()
    {
      QueuedTask.Run(() =>
      {
        if (_spatialReference != null)
        {
          MapView activeView = MapView.Active;
          Envelope envelope = activeView.Extent;
          ArcGISSpatialReference spatEnv = envelope.SpatialReference;
          int spatEnvFactoryCode = (spatEnv == null) ? 0 : spatEnv.Wkid;

          if ((spatEnv != null) && (spatEnvFactoryCode != _spatialReference.Wkid))
          {
            try
            {
              ProjectionTransformation projection = ProjectionTransformation.Create(envelope.SpatialReference,
                _spatialReference);
              var copyEnvelope = GeometryEngine.ProjectEx(envelope, projection) as Envelope;

              if ((copyEnvelope == null) || (copyEnvelope.IsEmpty))
              {
                _spatialReference = null;
              }
              else
              {
                if (NativeBounds != null)
                {
                  double xMin = NativeBounds.MinX;
                  double yMin = NativeBounds.MinY;
                  double xMax = NativeBounds.MaxX;
                  double yMax = NativeBounds.MaxY;

                  if ((copyEnvelope.XMin < xMin) || (copyEnvelope.XMax > xMax) || (copyEnvelope.YMin < yMin) ||
                      (copyEnvelope.YMax > yMax))
                  {
                    _spatialReference = null;
                  }
                }
              }
            }
            catch (ArgumentException)
            {
              _spatialReference = null;
            }
          }
        }

        ExistsInAreaEvent(this, (_spatialReference != null));
      });
    }

    #endregion
  }
}
