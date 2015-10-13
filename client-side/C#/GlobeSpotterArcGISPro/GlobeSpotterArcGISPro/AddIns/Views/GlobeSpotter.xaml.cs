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
using System.Drawing;
using ArcGIS.Desktop.Framework.Dialogs;
using GlobeSpotterAPI;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference;
using GlobeSpotterArcGISPro.Configuration.Resource;

using FileSettings = GlobeSpotterArcGISPro.Configuration.File.Settings;
using FileLogin = GlobeSpotterArcGISPro.Configuration.File.Login;
using ThisResources = GlobeSpotterArcGISPro.Properties.Resources;

namespace GlobeSpotterArcGISPro.AddIns.Views
{
  /// <summary>
  /// Interaction logic for GlobeSpotter.xaml
  /// </summary>
  public partial class GlobeSpotter : IAPIClient
  {
    private readonly API _api;
    private readonly ApiKey _apiKey;
    private readonly FileSettings _settings;
    private readonly Constants _constants;
    private readonly FileLogin _login;

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

    public void OnComponentReady()
    {
      SpatialReference spatialReference = _settings.CycloramaViewerCoordinateSystem;
      _api.SetAPIKey(_apiKey.Value);
      _api.SetUserNamePassword(_login.Username, _login.Password);
      _api.SetSrsNameViewer(spatialReference.SRSName);
      _api.SetSrsNameAddress(spatialReference.SRSName);
      _api.SetAdressLanguageCode(_constants.AddressLanguageCode);
    }

    public void OnAPIReady()
    {
      _api.SetViewerToolBarVisible(false);
      _api.SetViewerWindowBorderVisible(false);
      _api.OpenNearestImage("Treurenburgstraat 26, Eindhoven", 1);
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

    public void OnImagePreviewCompleted(uint viewerId)
    {
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

    public void OnViewChanged(uint viewerId, double yaw, double pitch, double hFov)
    {
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
    }

    public void OnViewerRemoved(uint viewerId)
    {
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
  }
}
