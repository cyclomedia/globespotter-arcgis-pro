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

using System.ComponentModel;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Contracts;

using FileConfiguration = GlobeSpotterArcGISPro.Configuration.File.Configuration;
using FileLogin = GlobeSpotterArcGISPro.Configuration.File.Login;

namespace GlobeSpotterArcGISPro.AddIns.Pages
{
  internal class Configuration: Page, INotifyPropertyChanged
  {
    #region Events

    public new event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region Members

    private readonly FileConfiguration _configuration;
    private readonly FileLogin _login;

    private readonly bool _useDefaultBaseUrl;
    private readonly string _baseUrlLocation;

    private readonly bool _useDefaultSwfUrl;
    private readonly string _swfLocation;

    private readonly bool _useProxyServer;
    private readonly string _proxyAddress;
    private readonly int _proxyPort;
    private readonly bool _proxyBypassLocalAddresses;
    private readonly bool _proxyUseDefaultCredentials;
    private readonly string _proxyUsername;
    private readonly string _proxyPassword;
    private readonly string _proxyDomain;

    #endregion

    #region Constructors

    protected Configuration()
    {
      _configuration = FileConfiguration.Instance;
      _login = FileLogin.Instance;

      _useDefaultBaseUrl = _configuration.UseDefaultBaseUrl;
      _baseUrlLocation = _configuration.BaseUrlLocation;

      _useDefaultSwfUrl = _configuration.UseDefaultSwfUrl;
      _swfLocation = _configuration.SwfLocation;

      _useProxyServer = _configuration.UseProxyServer;
      _proxyAddress = _configuration.ProxyAddress;
      _proxyPort = _configuration.ProxyPort;
      _proxyBypassLocalAddresses = _configuration.ProxyBypassLocalAddresses;
      _proxyUseDefaultCredentials = _configuration.ProxyUseDefaultCredentials;
      _proxyUsername = _configuration.ProxyUsername;
      _proxyPassword = _configuration.ProxyPassword;
      _proxyDomain = _configuration.ProxyDomain;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Base url
    /// </summary>
    public bool UseDefaultBaseUrl
    {
      get { return _configuration.UseDefaultBaseUrl; }
      set
      {
        if (_configuration.UseDefaultBaseUrl != value)
        {
          IsModified = true;
          _configuration.UseDefaultBaseUrl = value;
          NotifyPropertyChanged("UseDefaultBaseUrl");
        }
      }
    }

    public string BaseUrlLocation
    {
      get { return _configuration.BaseUrlLocation; }
      set
      {
        if (_configuration.BaseUrlLocation != value)
        {
          IsModified = true;
          _configuration.BaseUrlLocation = value;
          NotifyPropertyChanged("BaseUrlLocation");
        }
      }
    }

    /// <summary>
    /// Swf url
    /// </summary>
    public bool UseDefaultSwfUrl
    {
      get { return _configuration.UseDefaultSwfUrl; }
      set
      {
        if (_configuration.UseDefaultSwfUrl != value)
        {
          IsModified = true;
          _configuration.UseDefaultSwfUrl = value;
          NotifyPropertyChanged("UseDefaultSwfUrl");
        }
      }
    }

    public string SwfLocation
    {
      get { return _configuration.SwfLocation; }
      set
      {
        if (_configuration.SwfLocation != value)
        {
          IsModified = true;
          _configuration.SwfLocation = value;
          NotifyPropertyChanged("SwfLocation");
        }
      }
    }

    /// <summary>
    /// Proxy service
    /// </summary>
    public bool UseProxyServer
    {
      get { return _configuration.UseProxyServer; }
      set
      {
        if (_configuration.UseProxyServer != value)
        {
          IsModified = true;
          _configuration.UseProxyServer = value;
          NotifyPropertyChanged("UseProxyServer");
        }
      }
    }

    public string ProxyAddress
    {
      get { return _configuration.ProxyAddress; }
      set
      {
        if (_configuration.ProxyAddress != value)
        {
          IsModified = true;
          _configuration.ProxyAddress = value;
          NotifyPropertyChanged("ProxyAddress");
        }
      }
    }

    public int ProxyPort
    {
      get { return _configuration.ProxyPort; }
      set
      {
        if (_configuration.ProxyPort != value)
        {
          IsModified = true;
          _configuration.ProxyPort = value;
          NotifyPropertyChanged("ProxyPort");
        }
      }
    }

    public bool ProxyBypassLocalAddresses
    {
      get { return _configuration.ProxyBypassLocalAddresses; }
      set
      {
        if (_configuration.ProxyBypassLocalAddresses != value)
        {
          IsModified = true;
          _configuration.ProxyBypassLocalAddresses = value;
          NotifyPropertyChanged("ProxyBypassLocalAddresses");
        }
      }
    }

    public bool ProxyUseDefaultCredentials
    {
      get { return _configuration.ProxyUseDefaultCredentials; }
      set
      {
        if (_configuration.ProxyUseDefaultCredentials != value)
        {
          IsModified = true;
          _configuration.ProxyUseDefaultCredentials = value;
          NotifyPropertyChanged("ProxyUseDefaultCredentials");
        }
      }
    }

    public string ProxyUsername
    {
      get { return _configuration.ProxyUsername; }
      set
      {
        if (_configuration.ProxyUsername != value)
        {
          IsModified = true;
          _configuration.ProxyUsername = value;
          NotifyPropertyChanged("ProxyUsername");
        }
      }
    }

    public string ProxyPassword
    {
      get { return _configuration.ProxyPassword; }
      set
      {
        if (_configuration.ProxyPassword != value)
        {
          IsModified = true;
          _configuration.ProxyPassword = value;
          NotifyPropertyChanged("ProxyPassword");
        }
      }
    }

    public string ProxyDomain
    {
      get { return _configuration.ProxyDomain; }
      set
      {
        if (_configuration.ProxyDomain != value)
        {
          IsModified = true;
          _configuration.ProxyDomain = value;
          NotifyPropertyChanged("ProxyDomain");
        }
      }
    }

    #endregion

    #region Overrides

    protected override Task CommitAsync()
    {
      Save();
      return base.CommitAsync();
    }

    protected override Task CancelAsync()
    {
      _configuration.UseDefaultBaseUrl = _useDefaultBaseUrl;
      _configuration.BaseUrlLocation = _baseUrlLocation;

      _configuration.UseDefaultSwfUrl = _useDefaultSwfUrl;
      _configuration.SwfLocation = _swfLocation;

      _configuration.UseProxyServer = _useProxyServer;
      _configuration.ProxyAddress = _proxyAddress;
      _configuration.ProxyPort = _proxyPort;
      _configuration.ProxyBypassLocalAddresses = _proxyBypassLocalAddresses;
      _configuration.ProxyUseDefaultCredentials = _proxyUseDefaultCredentials;
      _configuration.ProxyUsername = _proxyUsername;
      _configuration.ProxyPassword = _proxyPassword;
      _configuration.ProxyDomain = _proxyDomain;

      Save();
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

    private void Save()
    {
      _configuration.Save();
      _login.Check();
    }

    #endregion
  }
}
