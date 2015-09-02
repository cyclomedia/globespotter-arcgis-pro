using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace GlobeSpotterArcGISPro.AddIns.Views.Converters
{
  public class CombineBoolean : IMultiValueConverter
  {
    #region IMultiValueConverter Members

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      return values.Aggregate(true, (current, t) => current && ((bool) t));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    #endregion
  }
}
