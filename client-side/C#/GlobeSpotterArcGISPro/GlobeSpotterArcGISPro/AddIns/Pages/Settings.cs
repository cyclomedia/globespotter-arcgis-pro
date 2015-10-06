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
using System.ComponentModel;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Contracts;
using GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference;

using FileSettings = GlobeSpotterArcGISPro.Configuration.File.Settings;

namespace GlobeSpotterArcGISPro.AddIns.Pages
{
  internal class Settings: Page, INotifyPropertyChanged
  {
    #region Events

    public new event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region Members

    private readonly FileSettings _settings;

    private readonly SpatialReference _recordingLayerCoordinateSystem;
    private readonly SpatialReference _cycloramaViewerCoordinateSystem;

    private readonly int _ctrlClickHashTag;
    private readonly int _ctrlClickDelta;
    private readonly bool _showDetailImages;
    private readonly bool _enableSmartClickMeasurement;

    private List<SpatialReference> _existsInAreaSpatialReferences;

    #endregion

    #region Constructors

    protected Settings()
    {
      _settings = FileSettings.Instance;

      _recordingLayerCoordinateSystem = _settings.RecordingLayerCoordinateSystem;
      _cycloramaViewerCoordinateSystem = _settings.CycloramaViewerCoordinateSystem;

      _ctrlClickHashTag = _settings.CtrlClickHashTag;
      _ctrlClickDelta = _settings.CtrlClickDelta;
      _showDetailImages = _settings.ShowDetailImages;
      _enableSmartClickMeasurement = _settings.EnableSmartClickMeasurement;
    }

    #endregion

    #region Properties

    /// <summary>
    /// All supporting spatial references
    /// </summary>
    public List<SpatialReference> ExistsInAreaSpatialReferences
    {
      get
      {
        if (_existsInAreaSpatialReferences == null)
        {
          _existsInAreaSpatialReferences = new List<SpatialReference>();
          SpatialReferences spatialReferences = SpatialReferences.Instance;

          foreach (var spatialReference in spatialReferences)
          {
            spatialReference.ExistsInArea();
            spatialReference.ExistsInAreaEvent += ExistsInAreaListner;
          }
        }

        return _existsInAreaSpatialReferences;
      }
    }

    /// <summary>
    /// Recording layer coordinate system
    /// </summary>
    public SpatialReference RecordingLayerCoordinateSystem
    {
      get { return _settings.RecordingLayerCoordinateSystem; }
      set
      {
        if (_settings.RecordingLayerCoordinateSystem != value)
        {
          IsModified = true;
          _settings.RecordingLayerCoordinateSystem = value;
          NotifyPropertyChanged("RecordingLayerCoordinateSystem");
        }
      }
    }

    /// <summary>
    /// Cyclorama viewer coordinate system
    /// </summary>
    public SpatialReference CycloramaViewerCoordinateSystem
    {
      get { return _settings.CycloramaViewerCoordinateSystem; }
      set
      {
        if (_settings.CycloramaViewerCoordinateSystem != value)
        {
          IsModified = true;
          _settings.CycloramaViewerCoordinateSystem = value;
          NotifyPropertyChanged("CycloramaViewerCoordinateSystem");
        }
      }
    }

    /// <summary>
    /// CTRL-CLICK #
    /// </summary>
    public int CtrlClickHashTag
    {
      get { return (_settings.CtrlClickHashTag - 1); }
      set
      {
        if ((_settings.CtrlClickHashTag - 1) != value)
        {
          IsModified = true;
          _settings.CtrlClickHashTag = value + 1;
          NotifyPropertyChanged("CtrlClickHashTag");
        }
      }
    }

    /// <summary>
    /// CTRL-CLICK Δ
    /// </summary>
    public int CtrlClickDelta
    {
      get { return (_settings.CtrlClickDelta - 1); }
      set
      {
        if ((_settings.CtrlClickDelta - 1) != value)
        {
          IsModified = true;
          _settings.CtrlClickDelta = value + 1;
          NotifyPropertyChanged("ctrlClickDelta");
        }
      }
    }

    /// <summary>
    /// Show detail images
    /// </summary>
    public bool ShowDetailImages
    {
      get { return _settings.ShowDetailImages; }
      set
      {
        if (_settings.ShowDetailImages != value)
        {
          IsModified = true;
          _settings.ShowDetailImages = value;
          NotifyPropertyChanged("ShowDetailImages");
        }
      }
    }

    /// <summary>
    /// Enable smart click measurement
    /// </summary>
    public bool EnableSmartClickMeasurement
    {
      get { return _settings.EnableSmartClickMeasurement; }
      set
      {
        if (_settings.EnableSmartClickMeasurement != value)
        {
          IsModified = true;
          _settings.EnableSmartClickMeasurement = value;
          NotifyPropertyChanged("EnableSmartClickMeasurement");
        }
      }
    }

    #endregion

    #region Overrides

    protected override Task CommitAsync()
    {
      _settings.Save();
      return base.CommitAsync();
    }

    protected override Task CancelAsync()
    {
      _settings.RecordingLayerCoordinateSystem = _recordingLayerCoordinateSystem;
      _settings.CycloramaViewerCoordinateSystem = _cycloramaViewerCoordinateSystem;

      _settings.CtrlClickHashTag = _ctrlClickHashTag;
      _settings.CtrlClickDelta = _ctrlClickDelta;
      _settings.ShowDetailImages = _showDetailImages;
      _settings.EnableSmartClickMeasurement = _enableSmartClickMeasurement;

      _settings.Save();
      return base.CancelAsync();
    }

    #endregion

    #region Functions

    private void NotifyPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    #endregion

    #region Eventlistners

    private void ExistsInAreaListner(SpatialReference spatialReference, bool exists)
    {
      if (_existsInAreaSpatialReferences != null)
      {
        if (exists && (!_existsInAreaSpatialReferences.Contains(spatialReference)))
        {
          _existsInAreaSpatialReferences.Add(spatialReference);
        }
        
        if ((!exists) && (_existsInAreaSpatialReferences.Contains(spatialReference)))
        {
          _existsInAreaSpatialReferences.Remove(spatialReference);
        }

        if ((RecordingLayerCoordinateSystem != null) && (spatialReference == RecordingLayerCoordinateSystem))
        {
          NotifyPropertyChanged("RecordingLayerCoordinateSystem");
        }

        if ((CycloramaViewerCoordinateSystem != null) && (spatialReference == CycloramaViewerCoordinateSystem))
        {
          NotifyPropertyChanged("CycloramaViewerCoordinateSystem");
        }
      }

      spatialReference.ExistsInAreaEvent -= ExistsInAreaListner;
    }

    #endregion
  }
}
