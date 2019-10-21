using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using Domain;
using System.Collections.Generic;

namespace Reports
{
  public partial class XRCli : DevExpress.XtraReports.UI.XtraReport
  {
      public XRCli()
    {
      InitializeComponent();
    }
      public XRCli(List<Cli> clis, List<String> sections, Dictionary<string, int> reportOptions = null)
      : this()
    {
      DataSource = clis;
      if (sections != null)
        CommonServiceReport.DisableSections(this, sections);
    }

  }
}
