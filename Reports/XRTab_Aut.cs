using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;

namespace Reports
{
    public partial class XRTab_Aut : DevExpress.XtraReports.UI.XtraReport
    {
        private XRTab_Aut()
        {
            InitializeComponent();
        }

        public XRTab_Aut(List<Tab_Aut> tabauts, List<String> sections = null, Dictionary<string, int> reportOptions = null)
            : this()
        {
          DataSource = tabauts;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }
    }
}
