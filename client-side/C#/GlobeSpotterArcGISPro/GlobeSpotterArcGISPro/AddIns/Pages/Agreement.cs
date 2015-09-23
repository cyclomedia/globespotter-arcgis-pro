using System;
using System.IO;
using System.Reflection;
using ArcGIS.Desktop.Framework.Contracts;

namespace GlobeSpotterArcGISPro.AddIns.Pages
{
  internal class Agreement: Page
  {
    protected Agreement()
    {
      // empty
    }

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
  }
}
