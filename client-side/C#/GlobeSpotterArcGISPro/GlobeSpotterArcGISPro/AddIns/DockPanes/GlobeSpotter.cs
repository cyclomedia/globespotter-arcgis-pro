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
using GlobeSpotterAPI;

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

    private string _location;
    private bool _isActive;
    private bool _replace;
    private bool _nearest;
    private Point3D _lookAt;

    #endregion

    #region Properties

    public string Location
    {
      get { return _location; }
      set
      {
        if (_location != value)
        {
          _location = value;
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

    public bool Replace
    {
      get { return _replace; }
      set
      {
        if (_replace != value)
        {
          _replace = value;
          NotifyPropertyChanged();
        }
      }
    }

    public bool Nearest
    {
      get { return _nearest; }
      set
      {
        if (_nearest != value)
        {
          _nearest = value;
          NotifyPropertyChanged();
        }
      }
    }

    public Point3D LookAt
    {
      get { return _lookAt; }
      set
      {
        _lookAt = value;
        NotifyPropertyChanged();
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
      _location = string.Empty;
      _replace = false;
      _nearest = false;
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
      GlobeSpotter globeSpotter = FrameworkApplication.DockPaneManager.Find(DockPaneId) as GlobeSpotter;

      if (!(globeSpotter?.IsVisible ?? true))
      {
        globeSpotter.Activate();
      }

      return globeSpotter;
    }

    #endregion
  }
}
