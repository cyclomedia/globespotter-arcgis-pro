using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace GlobeSpotterArcGISPro.AddIns.Buttons
{
  internal class CycloMediaOptions : Button
  {
    protected override void OnClick()
    {
      PropertySheet.ShowDialog("PropertyScreen_options");
    }
  }
}
