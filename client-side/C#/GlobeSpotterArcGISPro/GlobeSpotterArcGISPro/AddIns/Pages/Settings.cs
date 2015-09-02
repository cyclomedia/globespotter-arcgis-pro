using ArcGIS.Desktop.Framework.Contracts;

namespace GlobeSpotterArcGISPro.AddIns.Pages
{
  internal class Settings: Page
  {
    protected Settings()
    {
      EnableSmartClickMeasurement = true;
    }

    public bool ShowDetailImages { get; set; }
    public bool EnableSmartClickMeasurement { get; set; }
  }
}
