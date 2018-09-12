﻿/*
 * Integration in ArcMap for Cycloramas
 * Copyright (c) 2015 - 2018, CycloMedia, All rights reserved.
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3.0 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library.
 */

using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using GlobeSpotterArcGISPro.Utilities;

using ApiMeasurementPoint = GlobeSpotterAPI.MeasurementPoint;

namespace GlobeSpotterArcGISPro.AddIns.Views.Converters
{
  class MeasurementRelEst : IValueConverter
  {
    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      ApiMeasurementPoint point = value as ApiMeasurementPoint;
      bool reliableEstimate = point?.reliableEstimate ?? false;
      bool stdef = point != null && !double.IsNaN(point.Std_x) && !double.IsNaN(point.Std_y) &&
                   !double.IsNaN(point.Std_z);      
      var circle = new Bitmap(18, 18);

      using (var ga = Graphics.FromImage(circle))
      {
        ga.Clear(Color.Transparent);
        Brush color = reliableEstimate ? Brushes.Green : (stdef ? Brushes.Red : Brushes.Gray);
        ga.DrawEllipse(new Pen(color, 1), 2, 2, 14, 14);
        ga.FillEllipse(color, 2, 2, 14, 14);
      }

      return circle.ToBitmapSource();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    #endregion
  }
}
