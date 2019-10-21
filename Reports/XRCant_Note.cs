using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;

namespace Reports
{
    public partial class XRCant_Note : DevExpress.XtraReports.UI.XtraReport
    {
        public XRCant_Note()
        {
            InitializeComponent();
        }

        public XRCant_Note(List<Cant> cants, List<String> sections = null, Dictionary<string, int> reportOptions = null)
            : this()
        {
            DataSource = cants;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }        
    }
}
