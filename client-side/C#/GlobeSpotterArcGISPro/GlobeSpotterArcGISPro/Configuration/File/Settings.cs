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

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference;
using GlobeSpotterArcGISPro.Utilities;

using SystemIOFile = System.IO.File;

namespace GlobeSpotterArcGISPro.Configuration.File
{
  [XmlRoot("Settings")]
  public class Settings: INotifyPropertyChanged
  {
    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region Members

    private static readonly XmlSerializer XmlSettings;
    private static Settings _settings;
    private SpatialReference _recordingLayerCoordinateSystem;
    private SpatialReference _cycloramaViewerCoordinateSystem;
    private int _ctrlClickHashTag;
    private int _ctrlClickDelta;
    private bool _showDetailImages;
    private bool _enableSmartClickMeasurement;

    #endregion

    #region Constructors

    static Settings()
    {
      XmlSettings = new XmlSerializer(typeof(Settings));
    }

    #endregion

    #region Properties

    /// <summary>
    /// Recording layer coordinate system
    /// </summary>
    public SpatialReference RecordingLayerCoordinateSystem
    {
      get { return _recordingLayerCoordinateSystem; }
      set
      {
        if (value != null)
        {
          bool changed = (value != _recordingLayerCoordinateSystem);
          SpatialReferenceList spatialReferenceList = SpatialReferenceList.Instance;
          _recordingLayerCoordinateSystem = value;

          foreach (var spatialReference in spatialReferenceList)
          {
            if (spatialReference.SRSName == value.SRSName)
            {
              _recordingLayerCoordinateSystem = spatialReference;
            }
          }

          if (changed)
          {
            OnPropertyChanged();
          }
        }
      }
    }

    /// <summary>
    /// Cyclorama viewer coordinate system
    /// </summary>
    public SpatialReference CycloramaViewerCoordinateSystem
    {
      get { return _cycloramaViewerCoordinateSystem; }
      set
      {
        if (value != null)
        {
          bool changed = (value != _cycloramaViewerCoordinateSystem);
          SpatialReferenceList spatialReferenceList = SpatialReferenceList.Instance;
          _cycloramaViewerCoordinateSystem = value;

          foreach (var spatialReference in spatialReferenceList)
          {
            if (spatialReference.SRSName == value.SRSName)
            {
              _cycloramaViewerCoordinateSystem = spatialReference;
            }
          }

          if (changed)
          {
            OnPropertyChanged();
          }
        }
      }
    }

    /// <summary>
    /// CTRL-CLICK #
    /// </summary>
    public int CtrlClickHashTag
    {
      get { return _ctrlClickHashTag; }
      set
      {
        if (_ctrlClickHashTag != value)
        {
          _ctrlClickHashTag = value;
          OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// CTRL-CLICK Δ
    /// </summary>
    public int CtrlClickDelta
    {
      get { return _ctrlClickDelta; }
      set
      {
        if (_ctrlClickDelta != value)
        {
          _ctrlClickDelta = value;
          OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Show detail images
    /// </summary>
    public bool ShowDetailImages
    {
      get { return _showDetailImages; }
      set
      {
        if (_showDetailImages != value)
        {
          _showDetailImages = value;
          OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Enable smart click measurement
    /// </summary>
    public bool EnableSmartClickMeasurement
    {
      get
      {
        return _enableSmartClickMeasurement;
      }
      set
      {
        if (_enableSmartClickMeasurement != value)
        {
          _enableSmartClickMeasurement = value;
          OnPropertyChanged();
        }
      }
    }

    public static Settings Instance
    {
      get
      {
        if (_settings == null)
        {
          Load();
        }

        return _settings ?? (_settings = Create());
      }
    }

    private static string FileName => Path.Combine(FileUtils.FileDir, "Settings.xml");

    #endregion

    #region Functions

    public void Save()
    {
      FileStream streamFile = SystemIOFile.Open(FileName, FileMode.Create);
      XmlSettings.Serialize(streamFile, this);
      streamFile.Close();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static Settings Load()
    {
      if (SystemIOFile.Exists(FileName))
      {
        var streamFile = new FileStream(FileName, FileMode.OpenOrCreate);
        _settings = (Settings) XmlSettings.Deserialize(streamFile);
        streamFile.Close();
      }

      return _settings;
    }

    private static Settings Create()
    {
      var result = new Settings
      {
        RecordingLayerCoordinateSystem = null,
        CycloramaViewerCoordinateSystem = null,
        CtrlClickHashTag = 3,
        CtrlClickDelta = 1,
        ShowDetailImages = false,
        EnableSmartClickMeasurement = true
      };

      result.Save();
      return result;
    }

    #endregion
  }
}
