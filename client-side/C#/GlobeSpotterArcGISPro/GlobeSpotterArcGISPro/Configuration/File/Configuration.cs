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
  [XmlRoot("Configuration")]
  public class Configuration
  {
    #region Members

    private static readonly XmlSerializer XmlConfiguration;
    private static Configuration _configuration;

    #endregion

    #region Constructors

    static Configuration()
    {
      XmlConfiguration = new XmlSerializer(typeof(Configuration));
    }

    #endregion

    #region Properties

    /// <summary>
    /// Base url
    /// </summary>
    public bool UseDefaultBaseUrl { get; set; }

    public string BaseUrlLocation { get; set; }

    /// <summary>
    /// Swf url
    /// </summary>
    public bool UseDefaultSwfUrl { get; set; }

    public string SwfLocation { get; set; }

    /// <summary>
    /// Proxy service
    /// </summary>
    public bool UseProxyServer { get; set; }

    public string ProxyAddress { get; set; }

    public int ProxyPort { get; set; }

    public bool ProxyBypassLocalAddresses { get; set; }

    public bool ProxyUseDefaultCredentials { get; set; }

    public string ProxyUsername { get; set; }

    public string ProxyPassword { get; set; }

    public string ProxyDomain { get; set; }

    public static Configuration Instance
    {
      get
      {
        if (_configuration == null)
        {
          Load();
        }

        return _configuration ?? (_configuration = Create());
      }
    }

    private static string FileName => Path.Combine(FileUtils.FileDir, "Configuration.xml");

    #endregion

    #region Functions

    public void Save()
    {
      FileStream streamFile = SystemIOFile.Open(FileName, FileMode.Create);
      XmlConfiguration.Serialize(streamFile, this);
      streamFile.Close();
    }

    private static Configuration Load()
    {
      if (SystemIOFile.Exists(FileName))
      {
        var streamFile = new FileStream(FileName, FileMode.OpenOrCreate);
        _configuration = (Configuration) XmlConfiguration.Deserialize(streamFile);
        streamFile.Close();
      }

      return _configuration;
    }

    private static Configuration Create()
    {
      var result = new Configuration
      {
        UseDefaultBaseUrl = true,
        BaseUrlLocation = string.Empty,
        UseDefaultSwfUrl = true,
        SwfLocation = string.Empty,
        UseProxyServer = false,
        ProxyAddress = string.Empty,
        ProxyPort = 80,
        ProxyBypassLocalAddresses = false,
        ProxyUseDefaultCredentials = true,
        ProxyUsername = string.Empty,
        ProxyPassword = string.Empty,
        ProxyDomain = string.Empty
      };

      result.Save();
      return result;
    }

    #endregion
  }
}
