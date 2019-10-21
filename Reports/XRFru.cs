using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;

namespace Reports
{
  public partial class XRFru : DevExpress.XtraReports.UI.XtraReport
  {
        private XRFru()
        {
            InitializeComponent();
        }

        public XRFru(List<Fru> frus, List<String> sections, Dictionary<string, int> reportOptions = null)
            : this()
        {
            DataSource = frus;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }
  }
}
