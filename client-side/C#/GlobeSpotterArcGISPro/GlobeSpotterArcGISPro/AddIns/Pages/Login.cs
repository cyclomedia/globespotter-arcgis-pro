using ArcGIS.Desktop.Framework.Contracts;

namespace GlobeSpotterArcGISPro.AddIns.Pages
{
  internal class Login: Page
  {
    protected Login()
    {
      // empty
    }

    public string Username { get; set; }
    public string Password { get; set; }
    public string Status { get; set; }
  }
}
