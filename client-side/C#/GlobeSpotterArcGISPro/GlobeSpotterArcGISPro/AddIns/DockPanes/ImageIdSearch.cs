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

using System.Collections.ObjectModel;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using GlobeSpotterArcGISPro.Configuration.Remote.Recordings;

namespace GlobeSpotterArcGISPro.AddIns.DockPanes
{
  internal class ImageIdSearch : DockPane
  {
    #region Constants

    private const string DockPaneId = "GlobeSpotterArcGISPro_ImageIdSearch";

    #endregion

    #region Members

    private ObservableCollection<Recording> _imageInfo;

    #endregion

    #region Properties

    public string ImageId { get; set; }

    public ObservableCollection<Recording> ImageInfo
      => _imageInfo ?? (_imageInfo = new ObservableCollection<Recording>());

    #endregion

    #region Constructor

    protected ImageIdSearch() { }

    #endregion

    #region Functions

    internal static void Show()
    {
      DockPane pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
      pane?.Activate();
    }

    #endregion
  }
}
