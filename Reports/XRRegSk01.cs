using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;
using Common;
using System.Linq;

namespace Reports
{
    public partial class XRRegSk01 : DevExpress.XtraReports.UI.XtraReport
    {
        public XRRegSk01()
        {
            InitializeComponent();
        }

        public XRRegSk01(List<Reg_V> regvs, List<String> sections = null, Dictionary<string, int> reportOptions = null)
            : this()
        {
            // Gestione delle opzioni report
            CommonServiceReport.ManageRegVReportOptions(this, reportOptions);

            DataSource = regvs;
            //Espongo nella Copertina del Report i Limiti dei Record selezionati
            //1) Ricavo la Descrizione del Collaboratore Min/Max presente sulle REG presenti nella DataGrid ricevuta dal Report
            var orderedByColDescRegvs = regvs.OrderBy(regv => regv.Col_Desc);
            Par_FromColDesc.Value = orderedByColDescRegvs.First().Col_Desc;
            Par_ToColDesc.Value = orderedByColDescRegvs.Last().Col_Desc;
            //2) Ricavo la Data Minima e Massima delle REG presenti nella DataGrid ricevuta dal Report 
            var orderedByDateRegvs = regvs.OrderBy(regv => regv.Data_Reg);
            Par_FromDataReg.Value = orderedByDateRegvs.First().Data_Reg;
            Par_ToDataReg.Value = orderedByDateRegvs.Last().Data_Reg;
            //3) Ricavo il Codice Collaboratore Min/Max presente sulle REG presenti nella DataGrid ricevuta dal Report
            var orderedByColCodRegvs = regvs.OrderBy(regv => regv.Col_Mnemonic);
            Par_FromColCod.Value = orderedByColCodRegvs.First().Col_Mnemonic;
            Par_ToColCod.Value = orderedByColCodRegvs.Last().Col_Mnemonic;

            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }

        private void TimeSpan_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.TimeSpanFormatter_BeforePrint(sender, e);
        }

        private void xrTableCell40_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            // se si sta stampando un cantiere vuoto allora si procede alla scrittura della stringa vuota con tag non presente
            var currentCell = sender as XRTableCell;
            String result = String.Empty;
            if (currentCell != null)
            {

                var currentRegV = (Reg_V)GetCurrentRow();

                string matricolaFru = String.IsNullOrEmpty(currentRegV.Codice_Fru) ? "<Manuale>" : currentRegV.Codice_Fru;

                if (String.IsNullOrEmpty(currentCell.Text))
                    result = String.Format("TAG N° {0} NON ASSOCIATO A NESSUN ASSISTITO", matricolaFru);
                else
                    result = currentCell.Text;
            }
            currentCell.Text = result;
        }

    }


}
