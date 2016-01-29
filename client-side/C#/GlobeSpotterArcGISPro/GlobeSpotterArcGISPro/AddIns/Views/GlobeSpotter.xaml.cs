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
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GlobeSpotterAPI;
using GlobeSpotterArcGISPro.Configuration.Resource;
using GlobeSpotterArcGISPro.Overlays;

using Constants = GlobeSpotterArcGISPro.Configuration.File.Constants;
using FileSettings = GlobeSpotterArcGISPro.Configuration.File.Settings;
using FileLogin = GlobeSpotterArcGISPro.Configuration.File.Login;
using ThisResources = GlobeSpotterArcGISPro.Properties.Resources;
using DockPaneGlobeSpotter = GlobeSpotterArcGISPro.AddIns.DockPanes.GlobeSpotter;
using MySpatialReference = GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference.SpatialReference;

namespace GlobeSpotterArcGISPro.AddIns.Views
{
  /// <summary>
  /// Interaction logic for GlobeSpotter.xaml
  /// </summary>
  public partial class GlobeSpotter : IAPIClient
  {
    #region Members

    private readonly API _api;
    private readonly ApiKey _apiKey;
    private readonly FileSettings _settings;
    private readonly Constants _constants;
    private readonly FileLogin _login;

    #endregion

    #region Constructor

    public GlobeSpotter()
    {
      _api = new API(InitType.REMOTE);
      _apiKey = ApiKey.Instance;
      _settings = FileSettings.Instance;
      _constants = Constants.Instance;
      _login = FileLogin.Instance;
      InitializeComponent();
      GlobeSpotterForm.Child.Controls.Add(_api.gui);
      _api.Initialize(this);
    }

    #endregion

    #region Events API

    public void OnComponentReady()
    {
      MySpatialReference spatialReference = _settings.CycloramaViewerCoordinateSystem;
      _api.SetAPIKey(_apiKey.Value);
      _api.SetUserNamePassword(_login.Username, _login.Password);
      _api.SetSrsNameViewer(spatialReference.SRSName);
      _api.SetSrsNameAddress(spatialReference.SRSName);
      _api.SetAdressLanguageCode(_constants.AddressLanguageCode);
    }

    public void OnAPIReady()
    {
      DockPaneGlobeSpotter globeSpotter = ((dynamic) DataContext);
      string imageId = globeSpotter.ImageId;

      _api.SetViewerToolBarVisible(false);
      _api.SetViewerWindowBorderVisible(false);
      globeSpotter.PropertyChanged += OnGlobeSpotterPropertyChanged;

      if (string.IsNullOrEmpty(imageId))
      {
        globeSpotter.Hide();
      }
      else
      {
        _api.OpenImage(imageId);
      }
    }

    public void OnAPIFailed()
    {
      MessageBox.Show(ThisResources.Globespotter_OnAPIFailed_Initialize_);
    }

    public void OnOpenImageFailed(string input)
    {
    }

    public void OnOpenImageResult(string input, bool opened, string imageId)
    {
    }

    public void OnOpenNearestImageResult(string input, bool opened, string imageId)
    {
    }

    public void OnImageChanged(uint viewerId)
    {
    }

    public async void OnImagePreviewCompleted(uint viewerId)
    {
      Viewer viewer = Viewer.Get(viewerId);

      if ((viewer != null) && (_api != null))
      {
        RecordingLocation location = _api.GetRecordingLocation(viewerId);
        double angle = _api.GetYaw(viewerId);
        double hFov = _api.GetHFov(viewerId);
        Color color = _api.GetViewerBorderColor(viewerId);
        await viewer.SetAsync(location, angle, hFov, color);
      }

      await MoveToLocation(viewerId);
    }

    public void OnImageSegmentLoaded(uint viewerId)
    {
    }

    public void OnImageCompleted(uint viewerId)
    {
    }

    public void OnImageFailed(uint viewerId)
    {
    }

    public void OnViewLoaded(uint viewerId)
    {
    }

    public async void OnViewChanged(uint viewerId, double yaw, double pitch, double hFov)
    {
      Viewer viewer = Viewer.Get(viewerId);

      if (viewer != null)
      {
        await viewer.UpdateAsync(yaw, hFov);
      }
    }

    public void OnViewClicked(uint viewerId, double[] mouseCoords)
    {
    }

    public void OnMarkerClicked(uint viewerId, uint drawingId, double[] markerCoords)
    {
    }

    public void OnEntityDataChanged(int entityId, EntityData data)
    {
    }

    public void OnEntityFocusChanged(int entityId)
    {
    }

    public void OnFocusPointChanged(double x, double y, double z)
    {
    }

    public void OnFeatureClicked(Dictionary<string, string> feature)
    {
    }

    public void OnViewerAdded(uint viewerId)
    {
      if (_api != null)
      {
        string imageId = _api.GetImageID(viewerId);
        Viewer.Add(viewerId, imageId);
        _api.SetActiveViewerReplaceMode(true);
        int nrImages = Viewer.ImageIds.Count;
        _api.SetViewerWindowBorderVisible(nrImages >= 2);
      }
    }

    public void OnViewerRemoved(uint viewerId)
    {
      Viewer.Delete(viewerId);
      int nrImages = Viewer.ImageIds.Count;
      _api.SetViewerWindowBorderVisible(nrImages >= 2);
    }

    public void OnViewerActive(uint viewerId)
    {
    }

    public void OnViewerInactive(uint viewerId)
    {
    }

    public void OnMaxViewers(uint maxViewers)
    {
    }

    public void OnMeasurementCreated(int entityId, string entityType)
    {
    }

    public void OnMeasurementClosed(int entityId, EntityData data)
    {
    }

    public void OnMeasurementOpened(int entityId, EntityData data)
    {
    }

    public void OnMeasurementCanceled(int entityId)
    {
    }

    public void OnMeasurementModeChanged(bool mode)
    {
    }

    public void OnMeasurementPointAdded(int entityId, int pointId)
    {
    }

    public void OnMeasurementPointUpdated(int entityId, int pointId)
    {
    }

    public void OnMeasurementPointRemoved(int entityId, int pointId)
    {
    }

    public void OnMeasurementPointOpened(int entityId, int pointId)
    {
    }

    public void OnMeasurementPointClosed(int entityId, int pointId)
    {
    }

    public void OnMeasurementPointObservationAdded(int entityId, int pointId, string imageId, Bitmap match)
    {
    }

    public void OnMeasurementPointObservationUpdated(int entityId, int pointId, string imageId)
    {
    }

    public void OnMeasurementPointObservationRemoved(int entityId, int pointId, string imageId)
    {
    }

    public void OnDividerPositionChanged(double position)
    {
    }

    public void OnMapClicked(Point2D point)
    {
    }

    public void OnMapExtentChanged(MapExtent extent, Point2D mapCenter, uint zoomLevel)
    {
    }

    public void OnAutoCompleteResult(string request, string[] results)
    {
    }

    public void OnMapInitialized()
    {
    }

    public void OnShowLocationRequested(uint viewerId, Point3D point3D)
    {
    }

    public void OnDetailImagesVisibilityChanged(bool value)
    {
    }

    public void OnMeasurementHeightLevelChanged(int entityId, double level)
    {
    }

    public void OnMeasurementPointHeightLevelChanged(int entityId, int pointId, double level)
    {
    }

    public void OnMapBrightnessChanged(double value)
    {
    }

    public void OnMapContrastChanged(double value)
    {
    }

    public void OnObliqueImageChanged()
    {
    }

    #endregion

    #region Events GlobeSpotter

    private void OnGlobeSpotterPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      switch (args.PropertyName)
      {
        case "ImageId":
          string imageId = ((dynamic)DataContext).ImageId;
          _api.OpenImage(imageId);
          break;
        case "IsActive":
          bool isActive = ((dynamic) DataContext).IsActive;

          if (!isActive)
          {
            int[] viewerIds = _api.GetViewerIDs();

            foreach (var viewerId in viewerIds)
            {
              _api.CloseImage((uint) viewerId);
            }
          }

          break;
      }
    }

    #endregion

    #region Functions

    private async Task MoveToLocation(uint viewerId)
    {
      RecordingLocation location = _api?.GetRecordingLocation(viewerId);

      if (location != null)
      {
        MapPoint point = await GsToMapPoint(location.X, location.Y, location.Z);
        MapView thisView = MapView.Active;
        Envelope envelope = thisView?.Extent;

        if ((point != null) && (envelope != null))
        {
          const double percent = 10.0;
          double xBorder = ((envelope.XMax - envelope.XMin) * percent) / 100;
          double yBorder = ((envelope.YMax - envelope.YMin) * percent) / 100;
          bool inside = (point.X > (envelope.XMin + xBorder)) && (point.X < (envelope.XMax - xBorder)) &&
                        (point.Y > (envelope.YMin + yBorder)) && (point.Y < (envelope.YMax - yBorder));

          if (!inside)
          {
            Camera camera = new Camera
            {
              X = point.X,
              Y = point.Y,
              Z = point.Z,
              SpatialReference = point.SpatialReference
            };

            await QueuedTask.Run(() =>
            {
              thisView.PanTo(camera);
            });
          }
        }
      }
    }

    private static async Task<MapPoint> GsToMapPoint(double x, double y, double z)
    {
      FileSettings settings = FileSettings.Instance;
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
          point = (MapPoint) mapPoint.Clone();
        }
      });

      return point;
    }

    #endregion
  }
}
