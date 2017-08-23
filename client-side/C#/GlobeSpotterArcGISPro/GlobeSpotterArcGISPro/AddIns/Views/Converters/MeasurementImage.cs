﻿/*
 * Integration in ArcMap for Cycloramas
 * Copyright (c) 2015 - 2017, CycloMedia, All rights reserved.
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
using GlobeSpotterArcGISPro.Overlays;
using GlobeSpotterArcGISPro.Utilities;

using ModuleGlobeSpotter = GlobeSpotterArcGISPro.AddIns.Modules.GlobeSpotter;

using Brush = System.Drawing.Brush;
using Brushes = System.Drawing.Brushes;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;

namespace GlobeSpotterArcGISPro.AddIns.Views.Converters
{
  class MeasurementImage : IValueConverter
  {
    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      string imageId = value as string;
      var image = new Bitmap(80, 20);
      string strParameter = parameter.ToString();
      float fontSize = float.Parse(strParameter);

      ModuleGlobeSpotter globeSpotter = ModuleGlobeSpotter.Current;
      ViewerList viewerList = globeSpotter.ViewerList;
      Viewer thisViewer = viewerList.Get(imageId);
      Color color = thisViewer?.Color ?? Color.Gray;
      Brush brush = new SolidBrush(Color.FromArgb(255, color));

      using (var sf = new StringFormat())
      {
        using (var ga = Graphics.FromImage(image))
        {
          ga.Clear(Color.Transparent);
          Rectangle rectangle = new Rectangle(2, 2, 76, 16);
          ga.DrawRectangle(new Pen(Brushes.Black, 1), rectangle);
          ga.FillRectangle(brush, rectangle);
          sf.Alignment = StringAlignment.Center;
          Font font = new Font("Arial", fontSize);
          ga.DrawString(imageId, font, Brushes.Black, rectangle, sf);
        }
      }

      return image.ToBitmapSource();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    #endregion
  }
}
