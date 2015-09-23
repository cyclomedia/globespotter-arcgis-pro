using System.Windows.Navigation;

namespace GlobeSpotterArcGISPro.AddIns.Views
{
  /// <summary>
  /// Interaction logic for About.xaml
  /// </summary>
  public partial class About
  {
    public About()
    {
      InitializeComponent();
    }

    private void EventClickUri(object sender, RequestNavigateEventArgs e)
    {
      var window = new NavigationWindow {Source = e.Uri};
      window.Show();
    }
  }
}
