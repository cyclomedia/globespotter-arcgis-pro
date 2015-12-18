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

using FileConfiguration = GlobeSpotterArcGISPro.Configuration.File.Configuration;

namespace GlobeSpotterArcGISPro.Configuration.Remote
{
  /// <summary>
  /// This file contains default URLs
  /// </summary>
  public class Urls
  {
    #region Constants

    // ReSharper disable InconsistentNaming
    private const string baseUrl = "https://atlas.cyclomedia.com";
    private const string apiUrl = "https://globespotter.cyclomedia.com/v285/api";
    private const string configurationRequest = "{0}/configuration/configuration/API";
    private const string apiSwf = "/viewer_api.swf";
    private const string spatialReferencesXml = "/config/srs/globespotterspatialreferences.xml";
    // ReSharper restore InconsistentNaming

    #endregion

    #region Members

    protected readonly FileConfiguration Configuration;

    #endregion

    #region Constructor

    protected Urls()
    {
      Configuration = FileConfiguration.Instance;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Base url
    /// </summary>
    private string BaseUrl => Configuration.UseDefaultBaseUrl ? baseUrl : Configuration.BaseUrlLocation;

    /// <summary>
    /// Configuration URL
    /// </summary>
    protected string ConfigurationUrl => string.Format(configurationRequest, BaseUrl);

    /// <summary>
    /// Spatialreferences URL
    /// </summary>
    protected string SpatialReferenceUrl => Configuration.UseDefaultSwfUrl
      ? string.Concat(apiUrl, spatialReferencesXml)
      : Configuration.SwfLocation.Replace(apiSwf, spatialReferencesXml);

    /// <summary>
    /// Recordings URL
    /// </summary>
    protected string RecordingServiceUrl => $"{BaseUrl}/recordings/wfs";

    #endregion
  }
}
