using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;

namespace Reports
{
    public partial class XRResp : XtraReport
    {
        private XRResp()
        {
            InitializeComponent();
        }

        public XRResp(List<Resp> resp, List<String> sections, Dictionary<string, int> reportOptions = null)
            : this()
        {
            DataSource = resp;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }
    }
}
