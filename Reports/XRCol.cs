using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using Domain;
using System.Collections.Generic;

namespace Reports
{
  public partial class XRCol : DevExpress.XtraReports.UI.XtraReport
  {
    private XRCol()
    {
      InitializeComponent();
    }

    public XRCol(List<Col_V> cols, List<String> sections, Dictionary<string, int> reportOptions = null)
      : this()
    {
      DataSource = cols;
      if (sections != null)
        CommonServiceReport.DisableSections(this, sections);
    }

    public XRCol(List<Col> cols, List<String> sections, Dictionary<string, int> reportOptions = null)
        : this()
    {
        DataSource = cols;
        if (sections != null)
            CommonServiceReport.DisableSections(this, sections);
    }


    private void TimeSpan_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
    {
      CommonServiceReport.TimeSpanFormatter_BeforePrint(sender, e);
    }             
  }
}
