using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;

namespace Reports
{
    public partial class XRParam : XtraReport
    {
        private XRParam()
        {
            InitializeComponent();
        }

        public XRParam(List<Param> parametris, List<String> sections = null, Dictionary<string, int> reportOptions = null)
            : this()
        {
            DataSource = parametris;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }       

        private void TimeSpan_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.TimeSpanFormatter_BeforePrint(sender, e);
        }        
    }
}
