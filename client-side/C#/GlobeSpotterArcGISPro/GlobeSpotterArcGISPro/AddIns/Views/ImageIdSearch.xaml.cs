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

using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using GlobeSpotterArcGISPro.Configuration.Remote.Recordings;
using GlobeSpotterArcGISPro.CycloMediaLayers;

using ModuleGlobeSpotter = GlobeSpotterArcGISPro.AddIns.Modules.GlobeSpotter;
using PaneImageIdSearch = GlobeSpotterArcGISPro.AddIns.DockPanes.ImageIdSearch;
using DockPaneGlobeSpotter = GlobeSpotterArcGISPro.AddIns.DockPanes.GlobeSpotter;

namespace GlobeSpotterArcGISPro.AddIns.Views
{
  /// <summary>
  /// Interaction logic for ImageIdSearch.xaml
  /// </summary>
  public partial class ImageIdSearch
  {
    public ImageIdSearch()
    {
      InitializeComponent();
    }

    private void OnMatchesMouseDoubleClicked(object sender, MouseButtonEventArgs e)
    {
      ListBox listBox = sender as ListBox;

      if (listBox != null)
      {
        foreach (Recording selectedItem in listBox.SelectedItems)
        {
          DockPaneGlobeSpotter globeSpotter = DockPaneGlobeSpotter.Show();

          if (globeSpotter != null)
          {
            globeSpotter.LookAt = null;
            globeSpotter.Replace = true;
            globeSpotter.Nearest = false;
            globeSpotter.Location = selectedItem.ImageId;
          }
        }
      }
    }

    private async void OnImageIdChanged(object sender, TextChangedEventArgs e)
    {
      TextBox textBox = sender as TextBox;
      string imageId = textBox?.Text ?? string.Empty;
      PaneImageIdSearch paneImageIdSearch = ((dynamic)DataContext);
      paneImageIdSearch.ImageInfo.Clear();

      if (imageId.Length == 8)
      {
        ModuleGlobeSpotter globeSpotter = ModuleGlobeSpotter.Current;
        CycloMediaGroupLayer groupLayer = globeSpotter.CycloMediaGroupLayer;

        foreach (var layer in groupLayer)
        {
          SpatialReference spatialReference = await layer.GetSpatialReferenceAsync();
          string epsgCode = $"EPSG:{spatialReference.Wkid}";
          FeatureCollection featureCollection = FeatureCollection.Load(imageId, epsgCode);

          if (featureCollection.NumberOfFeatures >= 1)
          {
            foreach (Recording recording in featureCollection.FeatureMembers.Recordings)
            {
              paneImageIdSearch.ImageInfo.Add(recording);
            }
          }
        }
      }
    }
  }
}
