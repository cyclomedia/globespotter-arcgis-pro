using System;
using System.Diagnostics;
using System.Reflection;
using ArcGIS.Desktop.Framework.Contracts;

namespace GlobeSpotterArcGISPro.AddIns.Pages
{
  internal class About: Page
  {
    protected About()
    {
      // empty
    }

    public string AboutText
    {
      get
      {
        // Assembly info
        Type type = GetType();
        Assembly assembly = type.Assembly;
        string location = assembly.Location;
        FileVersionInfo info = FileVersionInfo.GetVersionInfo(location);
        AssemblyName assName = assembly.GetName();

        // Version info
        string product = info.ProductName;
        string copyright = info.LegalCopyright;
        Version version = assName.Version;

        return string.Format("{0}{3}{1}{3}Version: {2}.", product, copyright, version, Environment.NewLine);
      }
    }
  }
}
