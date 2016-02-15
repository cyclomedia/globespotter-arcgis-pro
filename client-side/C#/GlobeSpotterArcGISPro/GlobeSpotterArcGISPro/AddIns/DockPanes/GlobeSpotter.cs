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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace GlobeSpotterArcGISPro.AddIns.DockPanes
{
  internal class GlobeSpotter : DockPane, INotifyPropertyChanged
  {
    #region Constants

    private const string DockPaneId = "globeSpotterArcGISPro_globeSpotterDockPane";

    #endregion

    #region Events

    public new event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region Members

    private string _imageId;
    private bool _isActive;

    #endregion

    #region Properties

    public string ImageId
    {
      get { return _imageId; }
      set
      {
        if (_imageId != value)
        {
          _imageId = value;
          NotifyPropertyChanged();
        }
      }
    }

    public bool IsActive
    {
      get { return _isActive; }
      set
      {
        if (_isActive != value)
        {
          _isActive = value;
          NotifyPropertyChanged();
        }
      }
    }

    #endregion

    #region Overrides

    protected override void OnActivate(bool isActive)
    {
      IsActive = isActive || _isActive;
      base.OnActivate(isActive);
    }

    protected override void OnHidden()
    {
      IsActive = false;
      base.OnHidden();
    }

    #endregion

    #region Functions

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal static GlobeSpotter Show()
    {
      DockPane pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
      pane?.Activate();
      return pane as GlobeSpotter;
    }

    public static void ShowLocation(string imageId)
    {
      bool globeSpotterDockPane = false;

      foreach (var dockPane in FrameworkApplication.DockPaneManager.DockPanes)
      {
        if ((dockPane is GlobeSpotter) && (dockPane.IsVisible))
        {
          globeSpotterDockPane = true;
          (dockPane as GlobeSpotter).ImageId = imageId;
        }
      }

      if (!globeSpotterDockPane)
      {
        GlobeSpotter globeSpotter = Show();

        if (globeSpotter != null)
        {
          globeSpotter.ImageId = imageId;
        }
      }
    }

    #endregion
  }
}
