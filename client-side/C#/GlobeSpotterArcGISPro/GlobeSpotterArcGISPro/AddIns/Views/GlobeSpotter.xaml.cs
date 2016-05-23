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
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GlobeSpotterAPI;
using GlobeSpotterArcGISPro.Configuration.File;
using GlobeSpotterArcGISPro.Configuration.Remote.GlobeSpotter;
using GlobeSpotterArcGISPro.Configuration.Resource;
using GlobeSpotterArcGISPro.CycloMediaLayers;
using GlobeSpotterArcGISPro.Overlays;
using GlobeSpotterArcGISPro.Utilities;
using GlobeSpotterArcGISPro.VectorLayers;

using FileSettings = GlobeSpotterArcGISPro.Configuration.File.Settings;
using FileLogin = GlobeSpotterArcGISPro.Configuration.File.Login;
using FileConfiguration = GlobeSpotterArcGISPro.Configuration.File.Configuration;
using ThisResources = GlobeSpotterArcGISPro.Properties.Resources;
using DockPaneGlobeSpotter = GlobeSpotterArcGISPro.AddIns.DockPanes.GlobeSpotter;
using ModuleGlobeSpotter = GlobeSpotterArcGISPro.AddIns.Modules.GlobeSpotter;
using MySpatialReference = GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference.SpatialReference;

namespace GlobeSpotterArcGISPro.AddIns.Views
{
  /// <summary>
  /// Interaction logic for GlobeSpotter.xaml
  /// </summary>
  public partial class GlobeSpotter : IAPIClient
  {
    #region Members

    private readonly ApiKey _apiKey;
    private readonly FileSettings _settings;
    private readonly FileConfiguration _configuration;
    private readonly ConstantsViewer _constants;
    private readonly FileLogin _login;
    private readonly HistoricalRecordings _historicalRecordings;
    private readonly List<string> _openNearest;
    private readonly List<CycloMediaLayer> _layers;
    private readonly ViewerList _viewerList;

    private API _api;
    private CrossCheck _crossCheck;
    private SpatialReference _lastSpatialReference;

    #endregion

    #region Constructor

    public GlobeSpotter()
    {
      InitializeComponent();
      _apiKey = ApiKey.Instance;
      _settings = FileSettings.Instance;
      _constants = ConstantsViewer.Instance;
      _historicalRecordings = HistoricalRecordings.Instance;

      _login = FileLogin.Instance;
      _login.PropertyChanged += OnLoginPropertyChanged;

      _configuration = FileConfiguration.Instance;
      _configuration.PropertyChanged += OnConfigurationPropertyChanged;

      _openNearest = new List<string>();
      _crossCheck = null;
      _lastSpatialReference = null;
      _layers = new List<CycloMediaLayer>();
      _viewerList = new ViewerList();
      Initialize();
    }

    #endregion

    #region Events API

    public void OnComponentReady()
    {
      string epsgCode = CoordSystemUtils.CheckCycloramaSpatialReference();

      if (_api != null)
      {
        _api.SetAPIKey(_apiKey.Value);
        _api.SetUserNamePassword(_login.Username, _login.Password);
        _api.SetSrsNameViewer(epsgCode);
        _api.SetSrsNameAddress(epsgCode);
        _api.SetAdressLanguageCode(_constants.AddressLanguageCode);

        if (!_configuration.UseDefaultBaseUrl)
        {
          _api.SetServiceURL(_configuration.BaseUrlLocation, ServiceUrlType.URL_BASE);
        }
      }
    }

    public async void OnAPIReady()
    {
      if (_api != null)
      {
        GlobeSpotterConfiguration.Load();
        _api.SetMaxViewers((uint) _constants.MaxViewers);
        _api.SetCloseViewerEnabled(true);
        _api.SetViewerToolBarVisible(false);
        _api.SetViewerToolBarButtonsVisible(true);
        _api.SetViewerTitleBarVisible(false);
        _api.SetViewerWindowBorderVisible(false);
        _api.SetHideOverlaysWhenMeasuring(false);
        _api.SetImageInformationEnabled(true);
        _api.SetViewerBrightnessEnabled(true);
        _api.SetViewerSaveImageEnabled(true);
        _api.SetViewerOverlayAlphaEnabled(true);
        _api.SetViewerShowLocationEnabled(true);
        _api.SetViewerDetailImagesVisible(_settings.ShowDetailImages);
        _api.SetContextMenuEnabled(true);
        _api.SetKeyboardEnabled(true);
        _api.SetViewerRotationEnabled(true);
        _api.SetWindowingMode(MDIWindowingMode.VERTICAL);

        _api.SetMultiWindowCount((uint) _settings.CtrlClickHashTag);
        _api.SetWindowSpread((uint) _settings.CtrlClickDelta);

        if (GlobeSpotterConfiguration.AddLayerWfs)
        {
          _api.SetViewerOverlayDrawDistanceEnabled(true);
        }

        if (GlobeSpotterConfiguration.MeasurePermissions)
        {
          _api.SetMeasurementSeriesModeEnabled(true);
        }

        if (GlobeSpotterConfiguration.MeasureSmartClick)
        {
          _api.SetMeasurementSmartClickModeEnabled(_settings.EnableSmartClickMeasurement);
        }

        DockPaneGlobeSpotter globeSpotter = ((dynamic) DataContext);
        string location = globeSpotter.Location;

        globeSpotter.PropertyChanged += OnGlobeSpotterPropertyChanged;
        _settings.PropertyChanged += OnSettingsPropertyChanged;

        ModuleGlobeSpotter moduleGlobeSpotter = ModuleGlobeSpotter.Current;
        CycloMediaGroupLayer groupLayer = moduleGlobeSpotter.CycloMediaGroupLayer;
        groupLayer.PropertyChanged += OnGroupLayerPropertyChanged;

        foreach (CycloMediaLayer layer in groupLayer)
        {
          if (!_layers.Contains(layer))
          {
            _layers.Add(layer);
            UpdateRecordingLayer(layer);
            layer.PropertyChanged += OnLayerPropertyChanged;
          }
        }

        if (string.IsNullOrEmpty(location))
        {
          globeSpotter.Hide();
        }
        else
        {
          await OpenImageAsync(false);
        }

        VectorLayerList vectorLayerList = moduleGlobeSpotter.VectorLayerList;
        vectorLayerList.LayerAdded += OnAddVectorLayer;
        vectorLayerList.LayerRemoved += OnRemoveVectorLayer;
        vectorLayerList.LayerUpdated += OnUpdatedVectorLayer;

        foreach (var vectorLayer in vectorLayerList)
        {
          vectorLayer.PropertyChanged += OnVectorLayerPropertyChanged;
        }
      }
    }

    public void OnAPIFailed()
    {
      MessageBox.Show(ThisResources.Globespotter_OnAPIFailed_Initialize_);
      RemoveApi();
    }

    public void OnOpenImageFailed(string input)
    {
    }

    public void OnOpenImageResult(string input, bool opened, string imageId)
    {
      if (!string.IsNullOrEmpty(input) && input.Contains("Vector3D") && opened && (_api != null))
      {
        input = input.Remove(0, 9);
        input = input.Remove(input.Length - 1, 1);
        var seperator = new[] {", "};
        string[] split = input.Split(seperator, StringSplitOptions.None);

        if (split.Length == 3)
        {
          CultureInfo ci = CultureInfo.InvariantCulture;
          var point3D = new Point3D(double.Parse(split[0], ci), double.Parse(split[1], ci),
            double.Parse(split[2], ci));
          int viewerId = _api.GetActiveViewer();

          if (viewerId != -1)
          {
            OnShowLocationRequested((uint) viewerId, point3D);
          }
        }
      }
    }

    public async void OnOpenNearestImageResult(string input, bool opened, string imageId, Point3D location)
    {
      if (opened)
      {
        if (_crossCheck == null)
        {
          _crossCheck = new CrossCheck();
        }

        double size = _constants.CrossCheckSize;
        await _crossCheck.UpdateAsync(location.x, location.y, size);
        _openNearest.Add(imageId);
      }
    }

    public void OnImageChanged(uint viewerId)
    {
      Viewer viewer = _viewerList.Get(viewerId);

      if ((viewer != null) && (_api != null))
      {
        string imageId = _api.GetImageID(viewerId);
        viewer.ImageId = imageId;

        if (viewer.HasMarker)
        {
          viewer.HasMarker = false;
          List<Viewer> markerViewers = _viewerList.MarkerViewers;

          if ((markerViewers.Count == 0) && (_crossCheck != null))
          {
            _crossCheck.Dispose();
            _crossCheck = null;
          }
        }
      }
    }

    public async void OnImagePreviewCompleted(uint viewerId)
    {
      Viewer viewer = _viewerList.Get(viewerId);

      if ((viewer != null) && (_api != null))
      {
        CurrentCult cult = CurrentCult.Get();
        string dateFormat = cult.DateFormat;
        _api.SetDateFormat(dateFormat);
        string timeFormat = cult.TimeFormat;
        _api.SetTimeFormat(timeFormat);

        RecordingLocation location = _api.GetRecordingLocation(viewerId);
        double angle = _api.GetYaw(viewerId);
        double hFov = _api.GetHFov(viewerId);
        Color color = _api.GetViewerBorderColor(viewerId);
        await viewer.SetAsync(location, angle, hFov, color);
        string imageId = viewer.ImageId;

        if (GlobeSpotterConfiguration.AddLayerWfs)
        {
          await UpdateVectorLayerAsync();
          double overlayDrawDistance = _constants.OverlayDrawDistance;
          _api.SetOverlayDrawDistance(viewerId, overlayDrawDistance);
        }

        if (_openNearest.Contains(imageId))
        {
          double pitch = _api.GetPitch(viewerId);
          double v = 90 - pitch;
          _api.SetDrawingLayerVisible(true);
          _api.SetDrawingMode(DrawingMode.CROSS_HAIR);
          double size = _constants.CrossCheckSize;

          _api.SetMarkerSize(size + 1);
          _api.SetMarkerColor(Color.Bisque);
          _api.DrawMarkerAtHV(viewerId, angle, v);

          _api.SetMarkerSize(size - 1);
          _api.SetMarkerColor(Color.Black);
          _api.DrawMarkerAtHV(viewerId, angle, v);

          viewer.HasMarker = true;
          _openNearest.Remove(imageId);
        }
      }

      await MoveToLocationAsync(viewerId);
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
      Viewer viewer = _viewerList.Get(viewerId);

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
      MapView mapView = MapView.Active;
      Map map = mapView?.Map;

      string uri = feature[VectorLayer.FieldUri];
      FeatureLayer layer = map?.FindLayer(uri) as FeatureLayer;

      string objectIdStr = feature[VectorLayer.FieldObjectId];
      long objectId = long.Parse(objectIdStr);
      mapView?.FlashFeature(layer, objectId);
      mapView?.ShowPopup(layer, objectId);
    }

    public void OnViewerAdded(uint viewerId)
    {
      if (_api != null)
      {
        string imageId = _api.GetImageID(viewerId);
        double overLayDrawDistance = _constants.OverlayDrawDistance;
        _viewerList.Add(viewerId, imageId, overLayDrawDistance);
        _api.SetActiveViewerReplaceMode(true);
        int nrImages = _viewerList.Count;
        _api.SetViewerWindowBorderVisible(nrImages >= 2);
      }
    }

    public void OnViewerRemoved(uint viewerId)
    {
      if (_api != null)
      {
        Viewer viewer = _viewerList.Get(viewerId);
        bool hasMarker = viewer.HasMarker;

        _viewerList.Delete(viewerId);
        int nrImages = _viewerList.Count;
        _api.SetViewerWindowBorderVisible(nrImages >= 2);
        uint? nrviewers = _api?.GetViewerCount();

        if (hasMarker)
        {
          List<Viewer> markerViewers = _viewerList.MarkerViewers;

          if ((markerViewers.Count == 0) && (_crossCheck != null))
          {
            _crossCheck.Dispose();
            _crossCheck = null;
          }
        }

        if (nrviewers == 0)
        {
          DockPaneGlobeSpotter globeSpotter = ((dynamic) DataContext);
          globeSpotter.Hide();
          _lastSpatialReference = null;
        }
      }
    }

    public async void OnViewerActive(uint viewerId)
    {
      await MoveToLocationAsync(viewerId);
      Viewer viewer = _viewerList.Get(viewerId);

      if (viewer != null)
      {
        await viewer.SetActiveAsync(true);
      }
    }

    public async void OnViewerInactive(uint viewerId)
    {
      Viewer viewer = _viewerList.Get(viewerId);

      if (viewer != null)
      {
        await viewer.SetActiveAsync(false);
      }
    }

    public async void OnImageDistanceSliderChanged(uint viewerId, double distance)
    {
      double e = 0.0;

      if (Math.Abs(distance - _constants.OverlayDrawDistance) > e)
      {
        _constants.OverlayDrawDistance = distance;
        _constants.Save();
      }

      Viewer viewer = _viewerList.Get(viewerId);
      viewer.OverlayDrawDistance = distance;
      await UpdateVectorLayerAsync();
    }

    public void OnMaxViewers()
    {
      MessageBox.Show(ThisResources.Globespotter_OnMaxViewers_Failed);
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

    public async void OnShowLocationRequested(uint viewerId, Point3D point3D)
    {
      MapView thisView = MapView.Active;
      Envelope envelope = thisView?.Extent;

      if ((envelope != null) && (point3D != null))
      {
        MapPoint point = await CoordSystemUtils.CycloramaToMapPointAsync(point3D.x, point3D.y, point3D.z);

        if (point != null)
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

    public void OnDetailImagesVisibilityChanged(bool value)
    {
      _settings.ShowDetailImages = value;
      _settings.Save();
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

    #region Events Properties

    private void OnGroupLayerPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      CycloMediaGroupLayer groupLayer = sender as CycloMediaGroupLayer;

      if ((groupLayer != null) && (args.PropertyName == "Count"))
      {
        foreach (CycloMediaLayer layer in groupLayer)
        {
          if (!_layers.Contains(layer))
          {
            _layers.Add(layer);
            layer.PropertyChanged += OnLayerPropertyChanged;
          }
        }
      }
    }

    private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      CycloMediaLayer layer = sender as CycloMediaLayer;

      if ((_api != null) && (layer != null))
      {
        switch (args.PropertyName)
        {
          case "Visible":
          case "IsVisibleInGlobespotter":
            UpdateRecordingLayer(layer);
            break;
        }
      }
    }

    private void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      switch (args.PropertyName)
      {
        case "UseDefaultSwfUrl":
        case "SwfLocation":
        case "UseDefaultBaseUrl":
        case "BaseUrlLocation":
        case "UseProxyServer":
        case "ProxyAddress":
        case "ProxyPort":
        case "ProxyBypassLocalAddresses":
        case "ProxyUseDefaultCredentials":
        case "ProxyUsername":
        case "ProxyPassword":
        case "ProxyDomain":
          RestartGlobeSpotter();
          break;
      }
    }

    private void OnLoginPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      switch (args.PropertyName)
      {
        case "Credentials":
          if ((!_login.Credentials) && (_api != null) && (_api.GetAPIReadyState()))
          {
            DockPaneGlobeSpotter globeSpotter = ((dynamic)DataContext);
            globeSpotter.Hide();
          }

          if (_login.Credentials)
          {
            RestartGlobeSpotter();
          }

          break;
      }
    }

    private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      if (_api != null)
      {
        switch (args.PropertyName)
        {
          case "CtrlClickHashTag":
            _api.SetMultiWindowCount((uint) _settings.CtrlClickHashTag);
            break;
          case "CtrlClickDelta":
            _api.SetWindowSpread((uint) _settings.CtrlClickDelta);
            break;
          case "ShowDetailImages":
            _api.SetViewerDetailImagesVisible(_settings.ShowDetailImages);
            break;
          case "EnableSmartClickMeasurement":
            if (GlobeSpotterConfiguration.MeasureSmartClick)
            {
              _api.SetMeasurementSmartClickModeEnabled(_settings.EnableSmartClickMeasurement);
            }

            break;
          case "CycloramaViewerCoordinateSystem":
            RestartGlobeSpotter();
            break;
        }
      }
    }

    private async void OnGlobeSpotterPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      switch (args.PropertyName)
      {
        case "Location":
          bool replace = ((dynamic) DataContext).Replace;
          await OpenImageAsync(replace);
          break;
        case "IsActive":
          bool isActive = ((dynamic) DataContext).IsActive;

          if ((!isActive) && (_api != null))
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

    private async void OnVectorLayerPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      if (GlobeSpotterConfiguration.AddLayerWfs)
      {
        VectorLayer vectorLayer = sender as VectorLayer;

        if (vectorLayer?.IsVisibleInGlobespotter ?? false)
        {
          switch (args.PropertyName)
          {
            case "IsVisibleInGlobespotter":
              await vectorLayer.GetGmlFromLocationAsync(_viewerList.Viewers);
              break;
            case "Gml":
              if ((vectorLayer.LayerId == null) || vectorLayer.GmlChanged)
              {
                RemoveVectorLayer(vectorLayer);
                AddVectorLayer(vectorLayer);
              }

              break;
          }
        }
        else
        {
          RemoveVectorLayer(vectorLayer);
        }
      }
    }

    #endregion

    #region Functions

    private void UpdateRecordingLayer(CycloMediaLayer layer)
    {
      if (layer.IsVisible)
      {
        _api.SetRecordingLocationsVisible(layer.IsVisibleInGlobespotter);
        _api.SetUseDateRange(layer.UseDateRange);

        if (layer.UseDateRange)
        {
          DateTime dateFrom = _historicalRecordings.DateFrom;
          DateTime dateTo = _historicalRecordings.DateTo;
          _api.SetDateFrom($"{dateFrom.Year}-{dateFrom.Month}-{dateFrom.Day}");
          _api.SetDateTo($"{dateTo.Year}-{dateTo.Month}-{dateTo.Day}");
        }
      }
      else if (!_layers.Aggregate(false, (current, cyclLayer) => (cyclLayer.IsVisible || current)))
      {
        _api.SetRecordingLocationsVisible(false);
      }
    }

    private void Initialize()
    {
      if (_login.Credentials)
      {
        _api = _configuration.UseDefaultSwfUrl
          ? new API(InitType.REMOTE)
          : (!string.IsNullOrEmpty(_configuration.SwfLocation))
            ? new API(InitType.REMOTE, _configuration.SwfLocation)
            : null;

        if (_api != null)
        {
          GlobeSpotterForm.Child.Controls.Add(_api.gui);
          _api.Initialize(this);
        }
      }
      else
      {
        DockPaneGlobeSpotter globeSpotter = ((dynamic) DataContext);
        globeSpotter.Hide();
      }
    }

    private async Task OpenImageAsync(bool replace)
    {
      if (_api != null)
      {
        string location = ((dynamic) DataContext).Location;
        bool nearest = ((dynamic) DataContext).Nearest;
        _api.SetActiveViewerReplaceMode(replace);

        if (nearest)
        {
          MySpatialReference spatialReference = _settings.CycloramaViewerCoordinateSystem;
          SpatialReference thisSpatialReference = spatialReference.ArcGisSpatialReference ??
                                                    (await spatialReference.CreateArcGisSpatialReferenceAsync());

          if ((_lastSpatialReference != null) && (thisSpatialReference.Wkid != _lastSpatialReference.Wkid))
          {
            string[] splitLoc = location.Split(',');
            CultureInfo ci = CultureInfo.InvariantCulture;
            double x = double.Parse(((splitLoc.Length >= 1) ? splitLoc[0] : "0.0"), ci);
            double y = double.Parse(((splitLoc.Length >= 2) ? splitLoc[1] : "0.0"), ci);
            MapPoint point = null;

            await QueuedTask.Run(() =>
            {
              point = MapPointBuilder.CreateMapPoint(x, y, _lastSpatialReference);
              ProjectionTransformation projection = ProjectionTransformation.Create(_lastSpatialReference,
                thisSpatialReference);
              point = GeometryEngine.ProjectEx(point, projection) as MapPoint;
            });

            if (point != null)
            {
              location = string.Format(ci, "{0},{1}", point.X, point.Y);
              DockPaneGlobeSpotter globeSpotter = ((dynamic) DataContext);
              globeSpotter.PropertyChanged -= OnGlobeSpotterPropertyChanged;
              ((dynamic) DataContext).Location = location;
              globeSpotter.PropertyChanged += OnGlobeSpotterPropertyChanged;
            }
          }

          _api.OpenNearestImage(location, (_settings.CtrlClickHashTag * _settings.CtrlClickDelta));
        }
        else
        {
          _api.OpenImage(location);
        }

        MySpatialReference cycloSpatialReference = _settings.CycloramaViewerCoordinateSystem;
        _lastSpatialReference = cycloSpatialReference.ArcGisSpatialReference ??
                                (await cycloSpatialReference.CreateArcGisSpatialReferenceAsync());
      }
    }

    private void RestartGlobeSpotter()
    {
      if ((_api == null) || (_api.GetAPIReadyState()))
      {
        DockPaneGlobeSpotter globeSpotter = ((dynamic) DataContext);
        globeSpotter.PropertyChanged -= OnGlobeSpotterPropertyChanged;
        _settings.PropertyChanged -= OnSettingsPropertyChanged;

        ModuleGlobeSpotter moduleGlobeSpotter = ModuleGlobeSpotter.Current;
        CycloMediaGroupLayer groupLayer = moduleGlobeSpotter.CycloMediaGroupLayer;
        groupLayer.PropertyChanged -= OnGroupLayerPropertyChanged;

        VectorLayerList vectorLayerList = moduleGlobeSpotter.VectorLayerList;
        vectorLayerList.LayerAdded -= OnAddVectorLayer;
        vectorLayerList.LayerRemoved -= OnRemoveVectorLayer;
        vectorLayerList.LayerUpdated -= OnUpdatedVectorLayer;

        foreach (var vectorLayer in vectorLayerList)
        {
          vectorLayer.PropertyChanged -= OnVectorLayerPropertyChanged;
        }

        foreach (var vectorLayer in vectorLayerList)
        {
          uint? vectorLayerId = vectorLayer.LayerId;

          if (vectorLayerId != null)
          {
            _api?.RemoveLayer((uint) vectorLayerId);
            vectorLayer.LayerId = null;
          }
        }

        _viewerList.RemoveViewers();

        if ((_api != null) && (_api.GetAPIReadyState()))
        {
          int[] viewerIds = _api.GetViewerIDs();

          foreach (int viewerId in viewerIds)
          {
            _api.CloseImage((uint) viewerId);
          }

          RemoveApi();
        }

        Initialize();
      }
    }

    private void RemoveApi()
    {
      if (_api?.gui != null)
      {
        if (GlobeSpotterForm.Child.Controls.Contains(_api.gui))
        {
          GlobeSpotterForm.Child.Controls.Remove(_api.gui);
          _api.gui.Dispose();
        }
      }

      _api = null;
    }

    private async Task MoveToLocationAsync(uint viewerId)
    {
      RecordingLocation location = _api?.GetRecordingLocation(viewerId);

      if (location != null)
      {
        MapPoint point = await CoordSystemUtils.CycloramaToMapPointAsync(location.X, location.Y, location.Z);
        MapView thisView = MapView.Active;
        Envelope envelope = thisView?.Extent;

        if ((point != null) && (envelope != null))
        {
          const double percent = 10.0;
          double xBorder = ((envelope.XMax - envelope.XMin)*percent)/100;
          double yBorder = ((envelope.YMax - envelope.YMin)*percent)/100;
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

    #endregion

    #region Vector layer events

    private async void OnUpdatedVectorLayer()
    {
      if (GlobeSpotterConfiguration.AddLayerWfs)
      {
        await UpdateVectorLayerAsync();
      }
    }

    private async void OnAddVectorLayer(VectorLayer vectorLayer)
    {
      if (GlobeSpotterConfiguration.AddLayerWfs)
      {
        vectorLayer.PropertyChanged += OnVectorLayerPropertyChanged;

        if (vectorLayer.IsVisibleInGlobespotter)
        {
          await vectorLayer.GetGmlFromLocationAsync(_viewerList.Viewers);
        }
        else
        {
          RemoveVectorLayer(vectorLayer);
        }
      }
    }

    private void OnRemoveVectorLayer(VectorLayer vectorLayer)
    {
      if (GlobeSpotterConfiguration.AddLayerWfs)
      {
        vectorLayer.PropertyChanged -= OnVectorLayerPropertyChanged;
        RemoveVectorLayer(vectorLayer);
      }
    }

    #endregion

    #region vector layer functions

    private async Task UpdateVectorLayerAsync()
    {
      ModuleGlobeSpotter globeSpotterModule = ModuleGlobeSpotter.Current;
      VectorLayerList vectorLayerList = globeSpotterModule.VectorLayerList;

      // ReSharper disable once ForCanBeConvertedToForeach
      for(int i = 0; i < vectorLayerList.Count; i++)
      {
        VectorLayer vectorLayer = vectorLayerList[i];

        if (vectorLayer.IsVisibleInGlobespotter)
        {
          await vectorLayer.GetGmlFromLocationAsync(_viewerList.Viewers);
        }
        else
        {
          RemoveVectorLayer(vectorLayer);
        }
      }
    }

    private void AddVectorLayer(VectorLayer vectorLayer)
    {
      try
      {
        int minZoomLevel = _constants.MinVectorLayerZoomLevel;

        MySpatialReference cyclSpatRel = _settings.CycloramaViewerCoordinateSystem;
        string srsName = cyclSpatRel.SRSName;

        string layerName = vectorLayer.Name;
        string gml = vectorLayer.Gml;
        Color color = vectorLayer.Color;

        uint? layerId = _api?.AddGMLLayer(layerName, gml, srsName, color, true, false, minZoomLevel);
        vectorLayer.LayerId = layerId;
      }
      catch
      {
        // ignored
      }
    }

    private void RemoveVectorLayer(VectorLayer vectorLayer)
    {
      uint? layerId = vectorLayer?.LayerId;

      if (layerId != null)
      {
        _api?.RemoveLayer((uint) layerId);
        vectorLayer.LayerId = null;
      }
    }

    #endregion
  }
}
