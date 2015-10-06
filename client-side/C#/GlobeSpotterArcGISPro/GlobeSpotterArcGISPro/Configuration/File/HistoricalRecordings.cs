/*
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
using System.Xml.Serialization;
using GlobeSpotterArcGISPro.Utilities;

using SystemIOFile = System.IO.File;

namespace GlobeSpotterArcGISPro.Configuration.File
{
  [XmlRoot("HistoricalRecordings")]
  public class HistoricalRecordings
  {
    #region Members

    private static readonly XmlSerializer XmlHistoricalRecordings;
    private static HistoricalRecordings _historicalRecordings;

    #endregion

    #region Constructors

    static HistoricalRecordings()
    {
      XmlHistoricalRecordings = new XmlSerializer(typeof(HistoricalRecordings));
    }

    #endregion

    #region Properties

    /// <summary>
    /// Date from
    /// </summary>
    public DateTime DateFrom { get; set; }

    /// <summary>
    /// Date to
    /// </summary>
    public DateTime DateTo { get; set; }

    public static HistoricalRecordings Instance
    {
      get
      {
        if (_historicalRecordings == null)
        {
          Load();
        }

        return _historicalRecordings ?? (_historicalRecordings = Create());
      }
    }

    private static string FileName
    {
      get { return Path.Combine(FileUtils.FileDir, "HistoricalRecordings.xml"); }
    }

    #endregion

    #region Functions

    public void Save()
    {
      FileStream streamFile = SystemIOFile.Open(FileName, FileMode.Create);
      XmlHistoricalRecordings.Serialize(streamFile, this);
      streamFile.Close();
    }

    private static HistoricalRecordings Load()
    {
      if (SystemIOFile.Exists(FileName))
      {
        var streamFile = new FileStream(FileName, FileMode.OpenOrCreate);
        _historicalRecordings = (HistoricalRecordings) XmlHistoricalRecordings.Deserialize(streamFile);
        streamFile.Close();
      }

      return _historicalRecordings;
    }

    private static HistoricalRecordings Create()
    {
      DateTime now = DateTime.Now;

      var result = new HistoricalRecordings
      {
        DateFrom = now.AddYears(-3),
        DateTo = now
      };

      result.Save();
      return result;
    }

    #endregion
  }
}
