using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace GlobeSpotterArcGISPro.AddIns.Modules
{
  internal class GlobeSpotter : Module
  {
    private static GlobeSpotter _this;

    /// <summary>
    /// Retrieve the singleton instance to this module here
    /// </summary>
    public static GlobeSpotter Current
    {
      get
      {
        return _this ??
               (_this = (GlobeSpotter) FrameworkApplication.FindModule("GlobeSpotterArcGISPro_Module"));
      }
    }

    #region Overrides

    /// <summary>
    /// Called by Framework when ArcGIS Pro is closing
    /// </summary>
    /// <returns>False to prevent Pro from closing, otherwise True</returns>
    protected override bool CanUnload()
    {
      return true;
    }

    #endregion Overrides
  }
}
