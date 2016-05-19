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

using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GlobeSpotterArcGISPro.Configuration.File;

using MySpatialReference = GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference.SpatialReference;
using MySpatialReferenceList = GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference.SpatialReferenceList;

namespace GlobeSpotterArcGISPro.Utilities
{
  class CoordSystemUtils
  {
    public static async Task<MapPoint> CycloramaToMapPointAsync(double x, double y, double z)
    {
      Settings settings = Settings.Instance;
      MySpatialReference spatRel = settings.CycloramaViewerCoordinateSystem;
      SpatialReference mapSpatialReference = MapView.Active?.Map.SpatialReference;
      SpatialReference gsSpatialReference = (spatRel == null)
        ? mapSpatialReference
        : (spatRel.ArcGisSpatialReference ?? (await spatRel.CreateArcGisSpatialReferenceAsync()));
      MapPoint point = null;

      await QueuedTask.Run(() =>
      {
        MapPoint mapPoint = MapPointBuilder.CreateMapPoint(x, y, z, gsSpatialReference);

        if ((mapSpatialReference != null) && (gsSpatialReference != null) &&
            (gsSpatialReference.Wkid != mapSpatialReference.Wkid))
        {
          ProjectionTransformation projection = ProjectionTransformation.Create(gsSpatialReference, mapSpatialReference);
          point = GeometryEngine.ProjectEx(mapPoint, projection) as MapPoint;
        }
        else
        {
          point = (MapPoint)mapPoint.Clone();
        }
      });

      return point;
    }

    public static async Task<bool> CheckInAreaCycloramaSpatialReferenceAsync()
    {
      bool result = false;
      Settings settings = Settings.Instance;
      MySpatialReference spatialReference = settings.CycloramaViewerCoordinateSystem;

      if (spatialReference != null)
      {
        result = await spatialReference.ExistsInAreaAsync();

        if (!result)
        {
          CheckCycloramaSpatialReference(null);
        }
      }

      return result;
    }

    public static string CheckCycloramaSpatialReference()
    {
      Settings settings = Settings.Instance;
      MySpatialReference spatialReference = settings.CycloramaViewerCoordinateSystem;
      return CheckCycloramaSpatialReference(spatialReference);
    }

    private static string CheckCycloramaSpatialReference(MySpatialReference spatialReference)
    {
      Settings settings = Settings.Instance;
      MySpatialReference recordingSpatialReference = settings.RecordingLayerCoordinateSystem;
      string epsgCode = (spatialReference == null)
        ? ((recordingSpatialReference == null)
          ? ($"EPSG:{MapView.Active?.Map?.SpatialReference.Wkid ?? 0}")
          : recordingSpatialReference.SRSName)
        : spatialReference.SRSName;

      if (spatialReference?.ArcGisSpatialReference == null)
      {
        MySpatialReferenceList spatialReferences = MySpatialReferenceList.Instance;
        spatialReference = spatialReferences.GetItem(epsgCode);

        if (spatialReference == null)
        {
          spatialReference = spatialReferences.Aggregate<MySpatialReference, MySpatialReference>(null,
            (current, spatialReferenceComp) =>
              (spatialReferenceComp.ArcGisSpatialReference != null) ? spatialReferenceComp : current);
        }

        if (spatialReference != null)
        {
          epsgCode = spatialReference.SRSName;
          settings.CycloramaViewerCoordinateSystem = spatialReference;
          settings.Save();
        }
      }

      return epsgCode;
    }
  }
}
