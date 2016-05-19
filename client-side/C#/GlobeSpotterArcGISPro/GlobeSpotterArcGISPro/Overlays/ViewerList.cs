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
using System.Linq;

namespace GlobeSpotterArcGISPro.Overlays
{
  public class ViewerList : Dictionary<uint, Viewer>
  {
    public List<Viewer> MarkerViewers => (from viewer in this where viewer.Value.HasMarker select viewer.Value).ToList();

    public ICollection<Viewer> Viewers => Values;

    public void RemoveViewers()
    {
      foreach (var viewer in this)
      {
        Viewer myViewer = viewer.Value;
        myViewer.Dispose();
      }

      Clear();
    }

    public Viewer Get(uint viewerId)
    {
      return ContainsKey(viewerId) ? this[viewerId] : null;
    }

    public void Add(uint viewerId, string imageId, double overlayDrawDistance)
    {
      Add(viewerId, new Viewer(viewerId, imageId, overlayDrawDistance));
    }

    public void Delete(uint viewerId)
    {
      if (ContainsKey(viewerId))
      {
        this[viewerId].Dispose();
        Remove(viewerId);
      }
    }
  }
}
