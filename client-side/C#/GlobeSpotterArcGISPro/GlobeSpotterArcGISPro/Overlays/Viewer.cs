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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using GlobeSpotterAPI;

namespace GlobeSpotterArcGISPro.Overlays
{
  public class Viewer : ViewingCone
  {
    #region Members

    private static readonly Dictionary<uint, Viewer> Viewers;

    private RecordingLocation _location;

    #endregion

    #region Properties

    public string ImageId { get; private set; }

    public uint ViewerId { get; private set; }

    public bool HasMarker { get; set; }

    public static List<string> ImageIds => Viewers.Select(viewer => viewer.Value.ImageId).ToList();

    public static List<RecordingLocation> Locations => Viewers.Select(viewer => viewer.Value._location).ToList();

    public static List<Viewer> MarkerViewers
      => (from viewer in Viewers where viewer.Value.HasMarker select viewer.Value).ToList();

    #endregion

    #region Constructors

    static Viewer()
    {
      Viewers = new Dictionary<uint, Viewer>();
    }

    protected Viewer(uint viewerId, string imageId)
    {
      _location = null;
      ViewerId = viewerId;
      ImageId = imageId;
      HasMarker = false;
    }

    #endregion

    #region Functions

    public async Task SetAsync(RecordingLocation location, double angle, double hFov, Color color)
    {
      Dispose();
      _location = location;
      await InitializeAsync(location, angle, hFov, color);
    }

    public void Update(string imageId)
    {
      ImageId = imageId;
    }

    public static void Clear()
    {
      foreach (var viewer in Viewers)
      {
        Viewer myViewer = viewer.Value;
        myViewer.Dispose();
      }

      Viewers.Clear();
    }

    public static Viewer Get(uint viewerId)
    {
      return Viewers.ContainsKey(viewerId) ? Viewers[viewerId] : null;
    }

    public static void Add(uint viewerId, string imageId)
    {
      Viewers.Add(viewerId, new Viewer(viewerId, imageId));
    }

    public static void Delete(uint viewerId)
    {
      if (Viewers.ContainsKey(viewerId))
      {
        Viewers[viewerId].Dispose();
        Viewers.Remove(viewerId);
      }
    }

    #endregion
  }
}
