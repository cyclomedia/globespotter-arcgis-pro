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

using System.IO;
using System.Xml.Serialization;
using GlobeSpotterArcGISPro.Utilities;

using SystemIOFile = System.IO.File;

namespace GlobeSpotterArcGISPro.Configuration.File
{
  [XmlRoot("Constants")]
  public class Constants
  {
    #region Members

    private static readonly XmlSerializer XmlConstants;
    private static Constants _constants;

    #endregion

    #region Constructors

    static Constants()
    {
      XmlConstants = new XmlSerializer(typeof(Constants));
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
    public int OverlayDrawDistance { get; set; }

    /// <summary>
    /// Address language code
    /// </summary>
    public string AddressLanguageCode { get; set; }

    public static Constants Instance
    {
      get
      {
        if (_constants == null)
        {
          Load();
        }

        return _constants ?? (_constants = Create());
      }
    }

    private static string FileName => Path.Combine(FileUtils.FileDir, "Constants.xml");

    #endregion

    #region Functions

    public void Save()
    {
      FileStream streamFile = SystemIOFile.Open(FileName, FileMode.Create);
      XmlConstants.Serialize(streamFile, this);
      streamFile.Close();
    }

    private static Constants Load()
    {
      if (SystemIOFile.Exists(FileName))
      {
        var streamFile = new FileStream(FileName, FileMode.OpenOrCreate);
        _constants = (Constants) XmlConstants.Deserialize(streamFile);
        streamFile.Close();
      }

      return _constants;
    }

    private static Constants Create()
    {
      var result = new Constants
      {
        MaxViewers = 4,
        OverlayDrawDistance = 30,
        AddressLanguageCode = "nl"
      };

      result.Save();
      return result;
    }

    #endregion
  }
}
