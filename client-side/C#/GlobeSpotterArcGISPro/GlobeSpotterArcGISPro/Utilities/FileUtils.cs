using System;
using System.IO;

namespace GlobeSpotterArcGISPro.Utilities
{
  internal class FileUtils
  {
    #region Properties

    public static string FileDir
    {
      get
      {
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string result = Path.Combine(folder, "GlobeSpotterArcGISPro");

        if (!Directory.Exists(result))
        {
          Directory.CreateDirectory(result);
        }

        return result;
      }
    }

    #endregion
  }
}
