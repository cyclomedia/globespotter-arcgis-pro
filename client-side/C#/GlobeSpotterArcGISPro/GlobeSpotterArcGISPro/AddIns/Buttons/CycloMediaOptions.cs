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

using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using GlobeSpotterArcGISPro.Configuration.File;

namespace GlobeSpotterArcGISPro.AddIns.Buttons
{
  internal class CycloMediaOptions : Button
  {
    #region Overrides

    protected override void OnClick()
    {
      Agreement agreement = Agreement.Instance;
      Login login = Login.Instance;

      if (agreement.Value && (!login.Credentials))
      {
        PropertySheet.ShowDialog("globeSpotterArcGISPro_optionsPropertySheet", "globeSpotterArcGISPro_loginPage");
      }
      else
      {
        PropertySheet.ShowDialog("globeSpotterArcGISPro_optionsPropertySheet");
      }
    }

    #endregion
  }
}
