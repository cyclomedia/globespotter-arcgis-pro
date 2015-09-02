using System.ComponentModel;
using ArcGIS.Desktop.Framework.Contracts;

namespace GlobeSpotterArcGISPro.AddIns.Pages
{
  internal class Configuration: Page, INotifyPropertyChanged
  {
    public new event PropertyChangedEventHandler PropertyChanged;

    private bool _useDefaultBaseUrl;
    private bool _useDefaultSwfUrl;
    private bool _useProxyService;
    private bool _proxyUseDefaultCredentials;

    protected Configuration()
    {
      UseDefaultBaseUrl = true;
      UseDefaultSwfUrl = true;
      UseProxyService = false;
      ProxyBypassLocalAddresses = false;
      ProxyUseDefaultCredentials = true;
    }

    // Base url
    public bool UseDefaultBaseUrl
    {
      get { return _useDefaultBaseUrl; }
      set
      {
        _useDefaultBaseUrl = value;
        NotifyPropertyChanged("UseDefaultBaseUrl");
      }
    }

    public string BaseUrlLocation { get; set; }

    // Swf url
    public bool UseDefaultSwfUrl
    {
      get { return _useDefaultSwfUrl; }
      set
      {
        _useDefaultSwfUrl = value;
        NotifyPropertyChanged("UseDefaultSwfUrl");
      }
    }

    public string SwfLocation { get; set; }

    // Proxy service
    public bool UseProxyService
    {
      get { return _useProxyService; }
      set
      {
        _useProxyService = value;
        NotifyPropertyChanged("UseProxyService");
      }
    }

    public string ProxyAddress { get; set; }
    public string ProxyPort { get; set; }
    public bool ProxyBypassLocalAddresses { get; set; }

    public bool ProxyUseDefaultCredentials
    {
      get { return _proxyUseDefaultCredentials; }
      set
      {
        _proxyUseDefaultCredentials = value;
        NotifyPropertyChanged("ProxyUseDefaultCredentials");
      }
    }

    public string ProxyUsername { get; set; }
    public string ProxyPassword { get; set; }
    public string ProxyDomain { get; set; }

    public void NotifyPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }
  }
}
