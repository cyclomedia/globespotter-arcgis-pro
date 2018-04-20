/*
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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using GlobeSpotterArcGISPro.Configuration.Remote.Recordings;
using GlobeSpotterArcGISPro.CycloMediaLayers;

using ModuleGlobeSpotter = GlobeSpotterArcGISPro.AddIns.Modules.GlobeSpotter;

namespace GlobeSpotterArcGISPro.AddIns.Views.Converters
{
  class MeasurementImageStd : IValueConverter
  {
    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      string result = string.Empty;
      string imageId = value as string;
      ModuleGlobeSpotter globeSpotter = ModuleGlobeSpotter.Current;
      CycloMediaGroupLayer groupLayer = globeSpotter.CycloMediaGroupLayer;
      AutoResetEvent taskWaiter = new AutoResetEvent(false);
      const int timeOut = 3000;

      Task.Run(async () =>
      {
        Recording recording = await groupLayer.GetRecordingAsync(imageId);
        double stdX = (recording == null) ? 0 : (recording.LongitudePrecision ?? 0);
        double stdY = (recording == null) ? 0 : (recording.LatitudePrecision ?? 0);
        double stdZ = (recording == null) ? 0 : (recording.HeightPrecision ?? 0);
        result = $"{stdX:0.00} {stdY:0.00} {stdZ:0.00}";
        taskWaiter.Set();
      });

      taskWaiter.WaitOne(timeOut);
      return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    #endregion
  }
}
