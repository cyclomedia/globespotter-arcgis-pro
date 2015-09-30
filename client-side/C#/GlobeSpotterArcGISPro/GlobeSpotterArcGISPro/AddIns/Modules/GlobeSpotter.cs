/*
 * Integration in ArcMap for Cycloramas
 * Copyright (c) 2015, CycloMedia, All rights reserved.
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

namespace GlobeSpotterArcGISPro.AddIns.Modules
{
  internal class GlobeSpotter : Module
  {
    #region Members

    private static GlobeSpotter _globeSpotter;

    #endregion

    #region Properties

    /// <summary>
    /// Retrieve the singleton instance to this module here
    /// </summary>
    public static GlobeSpotter Current
    {
      get
      {
        return _globeSpotter ??
               (_globeSpotter = (GlobeSpotter) FrameworkApplication.FindModule("GlobeSpotterArcGISPro_Module"));
      }
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Called by Framework when ArcGIS Pro is closing
    /// </summary>
    /// <returns>False to prevent Pro from closing, otherwise True</returns>
    protected override bool CanUnload()
    {
      return true;
    }

    #endregion
  }
}
