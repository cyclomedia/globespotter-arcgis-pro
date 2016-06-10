/*
 * Integration in ArcMap for Cycloramas
 * Copyright (c) 2015 - 2016, CycloMedia, All rights reserved.
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

using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace GlobeSpotterArcGISPro.AddIns.DockPanes
{
  internal class MeasurementDetail : DockPane
  {
    #region Constants

    private const string DockPaneId = "GlobeSpotterArcGISPro_MeasurementDetail";

    #endregion

    #region Constructor

    protected MeasurementDetail() { }

    #endregion

    #region Functions

    internal static MeasurementDetail Get()
    {
      return (FrameworkApplication.DockPaneManager.Find(DockPaneId) as MeasurementDetail);
    }

    public static void OpenHide()
    {
      MeasurementDetail pane = Get();

      if (pane.IsVisible)
      {
        pane.Hide();
      }
      else
      {
        pane.Activate();
      }
    }

    #endregion
  }
}
