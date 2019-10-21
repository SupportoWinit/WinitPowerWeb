using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;

namespace Reports
{
    public partial class XRCol_Pru : DevExpress.XtraReports.UI.XtraReport
    {
        public XRCol_Pru()
        {
            InitializeComponent();
        }

        public XRCol_Pru(List<Col> cols, List<String> sections = null, Dictionary<string, int> reportOptions = null)
            : this()
        {
          DataSource = cols;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }
    }
}
