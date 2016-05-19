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

using System.IO;
using System.Xml.Serialization;
using GlobeSpotterArcGISPro.Utilities;

using SystemIOFile = System.IO.File;

namespace GlobeSpotterArcGISPro.Configuration.File
{
  [XmlRoot("ConstantsViewer")]
  public class ConstantsViewer
  {
    #region Members

    private static readonly XmlSerializer XmlConstantsViewer;
    private static ConstantsViewer _constantsViewer;

    #endregion

    #region Constructors

    static ConstantsViewer()
    {
      XmlConstantsViewer = new XmlSerializer(typeof(ConstantsViewer));
    }

    #endregion

    #region Properties

    /// <summary>
    /// Max viewers
    /// </summary>
    public int MaxViewers { get; set; }

    /// <summary>
    /// Overlay draw distance
    /// </summary>
    public double OverlayDrawDistance { get; set; }

    /// <summary>
    /// Min vectorlayer zoomlevel
    /// </summary>
    public int MinVectorLayerZoomLevel { get; set; }

    /// <summary>
    /// Address language code
    /// </summary>
    public string AddressLanguageCode { get; set; }

    /// <summary>
    /// Size of the Cross Check
    /// </summary>
    public double CrossCheckSize { get; set; }

    public static ConstantsViewer Instance
    {
      get
      {
        if (_constantsViewer == null)
        {
          Load();
        }

        return _constantsViewer ?? (_constantsViewer = Create());
      }
    }

    private static string FileName => Path.Combine(FileUtils.FileDir, "ConstantsViewer.xml");

    #endregion

    #region Functions

    public void Save()
    {
      FileStream streamFile = SystemIOFile.Open(FileName, FileMode.Create);
      XmlConstantsViewer.Serialize(streamFile, this);
      streamFile.Close();
    }

    private static ConstantsViewer Load()
    {
      if (SystemIOFile.Exists(FileName))
      {
        var streamFile = new FileStream(FileName, FileMode.OpenOrCreate);
        _constantsViewer = (ConstantsViewer) XmlConstantsViewer.Deserialize(streamFile);
        streamFile.Close();
      }

      return _constantsViewer;
    }

    private static ConstantsViewer Create()
    {
      var result = new ConstantsViewer
      {
        MinVectorLayerZoomLevel = 7,
        MaxViewers = 4,
        OverlayDrawDistance = 30.0,
        AddressLanguageCode = "nl",
        CrossCheckSize = 10.0
      };

      result.Save();
      return result;
    }

    #endregion
  }
}
