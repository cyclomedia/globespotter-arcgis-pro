using System;
using System.Globalization;
using System.Windows.Data;

namespace GlobeSpotterArcGISPro.AddIns.Views.Converters
{
  public class InverseBoolean : IValueConverter
  {
    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return !((bool) value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    #endregion
  }
}
