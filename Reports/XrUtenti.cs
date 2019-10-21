using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;

namespace Reports
{
    public partial class XRUtenti : XtraReport
    {
        private XRUtenti()
        {
            InitializeComponent();
        }

        public XRUtenti(List<Utenti> users, List<String> sections, Dictionary<string, int> reportOptions = null)
            : this()
        {
            DataSource = users;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }
    }
}
