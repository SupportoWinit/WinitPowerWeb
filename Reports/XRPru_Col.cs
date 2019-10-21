using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;

namespace Reports
{
    public partial class XRPru_Col : DevExpress.XtraReports.UI.XtraReport
    {
        public XRPru_Col()
        {
            InitializeComponent();
        }

        public XRPru_Col(List<Pru> prus, List<String> sections = null, Dictionary<string, int> reportOptions = null)
            : this()
        {
            DataSource = prus;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }
    }
}
