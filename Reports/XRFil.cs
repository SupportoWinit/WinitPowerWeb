using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;

namespace Reports
{
    public partial class XRFil : XtraReport
    {
        private XRFil()
        {
            InitializeComponent();
        }

        public XRFil(List<Fil> fil, List<String> sections, Dictionary<string, int> reportOptions = null)
            : this()
        {
            DataSource = fil;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }
    }
}
