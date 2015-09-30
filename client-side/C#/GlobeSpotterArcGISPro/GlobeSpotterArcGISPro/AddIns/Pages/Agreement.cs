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

using System;
using System.IO;
using System.Reflection;
using ArcGIS.Desktop.Framework.Contracts;

namespace GlobeSpotterArcGISPro.AddIns.Pages
{
  internal class Agreement: Page
  {
    #region Constructors

    protected Agreement()
    {
      // empty
    }

    #endregion

    #region Properties

    public string AgreementText
    {
      get
      {
        Type type = GetType();
        Assembly assembly = type.Assembly;
        const string agreementPath = "GlobeSpotterArcGISPro.Doc.Agreement.txt";
        Stream agreementStream = assembly.GetManifestResourceStream(agreementPath);
        string result = string.Empty;

        if (agreementStream != null)
        {
          var reader = new StreamReader(agreementStream);
          result = reader.ReadToEnd();
          reader.Close();
        }

        return result;
      }
    }

    #endregion
  }
}
