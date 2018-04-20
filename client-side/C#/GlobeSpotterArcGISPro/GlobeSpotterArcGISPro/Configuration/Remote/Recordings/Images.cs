﻿/*
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

using System.Collections.Generic;
using System.Xml.Serialization;

namespace GlobeSpotterArcGISPro.Configuration.Remote.Recordings
{
  [XmlType(AnonymousType = true, Namespace = "http://www.cyclomedia.com/atlas")]
  [XmlRoot(Namespace = "http://www.cyclomedia.com/atlas", IsNullable = false)]
  public class Images
  {
    private List<Image> _images;

    #region Properties

    [XmlElement("Image", Namespace = "http://www.cyclomedia.com/atlas")]
    public Image[] Image
    {
      get { return _images?.ToArray() ?? new Image[0]; }
      set
      {
        if (value != null)
        {
          if (_images == null)
          {
            _images = new List<Image>();
          }

          _images.AddRange(value);
        }
      }
    }

    #endregion
  }
}
