using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;

namespace Reports
{
  public partial class XRPru : DevExpress.XtraReports.UI.XtraReport
  {
      public XRPru()
    {
      InitializeComponent();
    }
      public XRPru(List<Pru> prus, List<String> sections, Dictionary<string, int> reportOptions = null)
            : this()
        {
            DataSource = prus;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }
  }
}
