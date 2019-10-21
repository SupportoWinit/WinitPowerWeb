using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Business;
using DevExpress.XtraReports.UI;
using Domain;
using Business.Repository.Custom;
using System.Data.Common;
using Business.Repository;
using System.Data.Entity.Core.Objects;
using Common;

namespace Reports
{
    public class CustomXRTableCell : XRTableCell
    {
        public List<Tab_GridLookup> TabGridLookups { get; private set; }
        public Tab_GridLookup TabGridLookup { get; private set; }

        public CustomXRTableCell(List<Tab_GridLookup> tabGridLookups, Tab_GridLookup tabGridLookup)
        {
            TabGridLookups = tabGridLookups;
            TabGridLookup = tabGridLookup;
            BeforePrint += new System.Drawing.Printing.PrintEventHandler(CustomXRTableCell_BeforePrint);
        }

        public CustomXRTableCell(List<Tab_GridLookup> tabGridLookups)
        {
            TabGridLookups = tabGridLookups;
            BeforePrint += new System.Drawing.Printing.PrintEventHandler(CustomXRTableCell_BeforePrint);
        }

        void CustomXRTableCell_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (TabGridLookup != null)
                ProcessBeforePrintMultipleCells();
            else
                ProcessBeforePrintSingleCell();
        }

        private void ProcessBeforePrintSingleCell()
        {
            StringBuilder cellValue = new StringBuilder(String.Format("{0}: ", BusinessService.GetLocalizedString(TabGridLookups.FirstOrDefault().Nome_Risorsa)));

            var u = Report.GetCurrentRow();

            foreach (var tabGridLookup in TabGridLookups)
            {
                var keyTabValue = u.GetType().GetProperty(tabGridLookup.NomeRicerca).GetValue(u, null);

                if (keyTabValue != null)
                {
                    String keyTabValueString = keyTabValue.ToString();

                    var tmpText = keyTabValueString;

                    var queryList = RepoManager.Tab_GridLookupRepo.SearchByFieldAndValue(TabGridLookups, tabGridLookup.NomeRicerca,
                        keyTabValueString, isForReport: true);

                    if (queryList.Count > 0)
                    {
                        foreach (var qlist in queryList)
                        {
                            var value = qlist.GetType().GetProperty(tabGridLookup.NomeCampo).GetValue(queryList[0], null);
                            cellValue.AppendFormat("{0} ", value.ToString().Trim());
                        }
                        
                    }
                }
            }

            Text = cellValue.ToString().Trim();
        }

        private void ProcessBeforePrintMultipleCells()
        {
            var u = Report.GetCurrentRow();

            var keyTabValue = u.GetType().GetProperty(TabGridLookup.NomeRicerca).GetValue(u, null);

            if (keyTabValue != null)
            {
                String keyTabValueString = keyTabValue.ToString();

                Text = keyTabValueString;

                var queryList = RepoManager.Tab_GridLookupRepo.SearchByFieldAndValue(TabGridLookups, TabGridLookup.NomeRicerca,
                    keyTabValueString, isForReport: true);

                if (queryList.Count > 0)
                {
                    var value = queryList[0].GetType().GetProperty(TabGridLookup.NomeCampo).GetValue(queryList[0], null);
                    Text = value.ToString();
                }
            }
        }
    }
}
