﻿/*
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

using System.IO;
using System.Xml.Serialization;
using GlobeSpotterArcGISPro.Configuration.Remote.SpatialReference;
using GlobeSpotterArcGISPro.Utilities;

using SystemIOFile = System.IO.File;

namespace GlobeSpotterArcGISPro.Configuration.File
{
  [XmlRoot("Settings")]
  public class Settings
  {
    #region Members

    private static readonly XmlSerializer XmlSettings;
    private static Settings _settings;
    private SpatialReference _recordingLayerCoordinateSystem;
    private SpatialReference _cycloramaViewerCoordinateSystem;

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
          SpatialReferences spatialReferences = SpatialReferences.Instance;
          _recordingLayerCoordinateSystem = value;

          foreach (var spatialReference in spatialReferences)
          {
            if (spatialReference.SRSName == value.SRSName)
            {
              _recordingLayerCoordinateSystem = spatialReference;
            }
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
          SpatialReferences spatialReferences = SpatialReferences.Instance;
          _cycloramaViewerCoordinateSystem = value;

          foreach (var spatialReference in spatialReferences)
          {
            if (spatialReference.SRSName == value.SRSName)
            {
              _cycloramaViewerCoordinateSystem = spatialReference;
            }
          }
        }
      }
    }

    /// <summary>
    /// CTRL-CLICK #
    /// </summary>
    public int CtrlClickHashTag { get; set; }

    /// <summary>
    /// CTRL-CLICK Δ
    /// </summary>
    public int CtrlClickDelta { get; set; }

    /// <summary>
    /// Show detail images
    /// </summary>
    public bool ShowDetailImages { get; set; }

    /// <summary>
    /// Enable smart click measurement
    /// </summary>
    public bool EnableSmartClickMeasurement { get; set; }

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
