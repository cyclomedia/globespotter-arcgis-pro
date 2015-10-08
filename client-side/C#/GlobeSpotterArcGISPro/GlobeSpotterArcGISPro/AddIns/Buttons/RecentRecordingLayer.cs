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

using ArcGIS.Desktop.Framework.Contracts;
using GlobeSpotterArcGISPro.AddIns.Modules;
using GlobeSpotterArcGISPro.Configuration.File;

namespace GlobeSpotterArcGISPro.AddIns.Buttons
{
  internal class RecentRecordingLayer : Button
  {
    #region Members

    private readonly Agreement _agreement;

    #endregion

    #region Constructors

    protected RecentRecordingLayer()
    {
      _agreement = Agreement.Instance;
    }

    #endregion

    #region Event handlers

    protected override void OnClick()
    {
      GlobeSpotter.AddRecentRecordingLayer();
    }

    protected override void OnUpdate()
    {
      Enabled = _agreement.Value;
      base.OnUpdate();
    }

    #endregion
  }
}