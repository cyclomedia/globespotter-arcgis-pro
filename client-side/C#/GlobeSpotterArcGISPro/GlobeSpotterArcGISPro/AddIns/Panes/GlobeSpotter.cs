﻿/*
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

using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;

namespace GlobeSpotterArcGISPro.AddIns.Panes
{
  internal class GlobeSpotter : ViewStatePane
  {
    private const string ViewPaneId = "GlobeSpotterArcGISPro_GlobeSpotter";

    /// <summary>
    /// Consume the passed in CIMView. Call the base constructor to wire up the CIMView.
    /// </summary>
    public GlobeSpotter(CIMView view) : base(view)
    {
      // empty
    }

    /// <summary>
    /// Create a new instance of the pane.
    /// </summary>
    internal static GlobeSpotter Create()
    {
      var view = new CIMGenericView {ViewType = ViewPaneId};
      return FrameworkApplication.Panes.Create(ViewPaneId, new object[] { view }) as GlobeSpotter;
    }

    #region Pane Overrides

    /// <summary>
    /// Must be overridden in child classes used to persist the state of the view to the CIM.
    /// </summary>
    public override CIMView ViewState
    {
      get
      {
        _cimView.InstanceID = (int) InstanceID;
        return _cimView;
      }
    }

    /// <summary>
    /// Called when the pane is initialized.
    /// </summary>
    protected async override Task InitializeAsync()
    {
      await base.InitializeAsync();
    }

    /// <summary>
    /// Called when the pane is uninitialized.
    /// </summary>
    protected async override Task UninitializeAsync()
    {
      await base.UninitializeAsync();
    }

    #endregion Pane Overrides
  }
}