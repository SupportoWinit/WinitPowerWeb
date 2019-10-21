using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using Domain;
using System.Collections.Generic;

namespace Reports
{
    public partial class XRCant : DevExpress.XtraReports.UI.XtraReport
    {
        public XRCant()
        {
            InitializeComponent();
        }

        public XRCant(List<Cant_V> cants, List<String> sections, Dictionary<string, int> reportOptions = null)
            : this()
        {
            DataSource = cants;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }

        public XRCant(List<Cant> cants, List<String> sections, Dictionary<string, int> reportOptions = null)
            : this()
        {
            DataSource = cants;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }

        private void TimeSpan_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.TimeSpanFormatter_BeforePrint(sender, e);
        }
    }
}
