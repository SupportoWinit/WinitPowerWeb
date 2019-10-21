using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;

namespace Reports
{
    public partial class XRCant_Fru : DevExpress.XtraReports.UI.XtraReport
    {
        public XRCant_Fru()
        {
            InitializeComponent();
        }

        public XRCant_Fru(List<Cant> cants, List<String> sections = null, Dictionary<string, int> reportOptions = null)
            : this()
        {
            DataSource = cants;
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }        
    }
}
