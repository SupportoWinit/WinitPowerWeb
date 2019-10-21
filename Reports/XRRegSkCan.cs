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
    public partial class XRRegSkCan : DevExpress.XtraReports.UI.XtraReport
    {
        private Reg_V _regvStub = null;

        public XRRegSkCan()
        {
            InitializeComponent();
        }

        public XRRegSkCan(List<Reg_V> regvs, List<String> sections = null, Dictionary<string, int> reportOptions = null)
            : this()
        {
            #region Nascondimento delle band mai visibili

            var groupCanBand = Bands["Inizio_Can"];
            groupCanBand.Visible = false;

            #endregion

            #region Gestione delle opzioni report

            // Gestione delle opzioni report
            CommonServiceReport.ManageRegVReportOptions(this, reportOptions);

            #endregion

            DataSource = regvs;
            //Espongo nella Copertina del Report i Limiti dei Record selezionati
            //1) Ricavo la Descrizione del Cantiere Min/Max presente sulle REG presenti nella DataGrid ricevuta dal Report
            var orderedByCantDescRegvs = regvs.OrderBy(regv => regv.Cant_Desc);
            Par_FromCantDesc.Value = orderedByCantDescRegvs.First().Cant_Desc;
            Par_ToCantDesc.Value = orderedByCantDescRegvs.Last().Cant_Desc;
            //2) Ricavo la Data Minima e Massima delle REG presenti nella DataGrid ricevuta dal Report 
            var orderedByDateRegvs = regvs.OrderBy(regv => regv.Data_Reg);
            Par_FromDataReg.Value = orderedByDateRegvs.First().Data_Reg;
            Par_ToDataReg.Value = orderedByDateRegvs.Last().Data_Reg;
            //3) Ricavo il Codice Cantiere Min/Max presente sulle REG presenti nella DataGrid ricevuta dal Report
            var orderedByCantCodRegvs = regvs.OrderBy(regv => regv.Cant_Mnemonic);
            Par_FromCantCod.Value = orderedByCantCodRegvs.First().Cant_Mnemonic;
            Par_ToCantCod.Value = orderedByCantCodRegvs.Last().Cant_Mnemonic;

            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);
        }

        // Calcolo Durata Complessiva
        int totalFigGGMinutes = 0;
        int totalFigMMMinutes = 0;
        int totalFigCanMinutes = 0;
        int totalFisGGMinutes = 0;
        int totalFisMMMinutes = 0;
        int totalFisCanMinutes = 0;
        int totalDeltaFigFisGGMinutes = 0;
        int totalDeltaFigFisMMMinutes = 0;
        int totalDeltaFigFisCantMinutes = 0;


        #region Calcolo Totali Ore FIS/FIG x Cantiere/Anno/Mese/Giorno
        // TOTALI FIG X CAntiere
        private void xrcTotalDurataFigCan_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            e.Result = CommonService.GetHHMMStringFormMinutes(totalFigCanMinutes);
            e.Handled = true;
        }
        private void xrcTotalDurataFigCan_SummaryReset(object sender, EventArgs e)
        {
            totalFigCanMinutes = 0;
        }
        private void xrcTotalDurataFigCan_SummaryRowChanged(object sender, EventArgs e)
        {
            object value = GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Durata_Fig));
            totalFigCanMinutes += Convert.ToInt32(value);
        }
        // TOTALI FIS X CANTIERE    
        private void xrcTotalDurataFisCan_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            e.Result = CommonService.GetHHMMStringFormMinutes(totalFisCanMinutes);
            e.Handled = true;
        }
        private void xrcTotalDurataFisCan_SummaryReset(object sender, EventArgs e)
        {
            totalFisCanMinutes = 0;
        }
        private void xrcTotalDurataFisCan_SummaryRowChanged(object sender, EventArgs e)
        {
            object value = GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Durata_Fis));
            totalFisCanMinutes += Convert.ToInt32(value);
        }
        // TOTALI FIG X MESE
        private void xrcTotalDurataFigMM_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            e.Result = CommonService.GetHHMMStringFormMinutes(totalFigMMMinutes);
            e.Handled = true;
        }
        private void xrcTotalDurataFigMM_SummaryReset(object sender, EventArgs e)
        {
            totalFigMMMinutes = 0;
        }
        private void xrcTotalDurataFigMM_SummaryRowChanged(object sender, EventArgs e)
        {
            object value = GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Durata_Fig));
            totalFigMMMinutes += Convert.ToInt32(value);
        }
        // TOTALI FIS X MESE        

        private void xrcTotalDurataFisMM_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            e.Result = CommonService.GetHHMMStringFormMinutes(totalFisMMMinutes);
            e.Handled = true;
        }
        private void xrcTotalDurataFisMM_SummaryReset(object sender, EventArgs e)
        {
            totalFisMMMinutes = 0;
        }
        private void xrcTotalDurataFisMM_SummaryRowChanged(object sender, EventArgs e)
        {
            object value = GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Durata_Fis));
            totalFisMMMinutes += Convert.ToInt32(value);
        }
        // TOTALI FIG X GIORNO

        private void xrcTotalDurataFigGG_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            e.Result = CommonService.GetHHMMStringFormMinutes(totalFigGGMinutes);
            e.Handled = true;
        }
        private void xrcTotalDurataFigGG_SummaryReset(object sender, EventArgs e)
        {
            totalFigGGMinutes = 0;
        }
        private void xrcTotalDurataFigGG_SummaryRowChanged(object sender, EventArgs e)
        {
            object value = GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Durata_Fig));
            totalFigGGMinutes += Convert.ToInt32(value);
        }
        // TOTALI FIS X GIORNO          
        private void xrcTotalDurataFisGG_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            e.Result = CommonService.GetHHMMStringFormMinutes(totalFisGGMinutes);
            e.Handled = true;
        }
        private void xrcTotalDurataFisGG_SummaryReset(object sender, EventArgs e)
        {
            totalFisGGMinutes = 0;
        }
        private void xrcTotalDurataFisGG_SummaryRowChanged(object sender, EventArgs e)
        {
            object value = GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Durata_Fis));
            totalFisGGMinutes += Convert.ToInt32(value);
        }

        //TOTALI DELTA FIG/FIS PER GIORNO
        private void xrcTotalDeltaFigFisUGG_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            string returnString = CommonService.GetHHMMStringFormMinutes(Math.Abs(totalDeltaFigFisGGMinutes));
            e.Result = String.Format("{0}{1}", totalDeltaFigFisGGMinutes < 0 ? "-" : String.Empty, returnString);
            e.Handled = true;
        }

        private void xrcTotalDeltaFigFisUGG_SummaryReset(object sender, EventArgs e)
        {
            totalDeltaFigFisGGMinutes = 0;
        }

        private void xrcTotalDeltaFigFisUGG_SummaryRowChanged(object sender, EventArgs e)
        {
            totalDeltaFigFisGGMinutes += CommonService.GetMinutesNumberFromHHMMString((string)GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Delta_Fig_Fis)));
        }

        //TOTALI DELTA FIG/FIS PER MESE
        private void xrcTotalDeltaFigFisUMM_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            string returnString = CommonService.GetHHMMStringFormMinutes(Math.Abs(totalDeltaFigFisMMMinutes));
            e.Result = String.Format("{0}{1}", totalDeltaFigFisMMMinutes < 0 ? "-" : String.Empty, returnString);
            e.Handled = true;
        }

        private void xrcTotalDeltaFigFisUMM_SummaryReset(object sender, EventArgs e)
        {
            totalDeltaFigFisMMMinutes = 0;
        }

        private void xrcTotalDeltaFigFisUMM_SummaryRowChanged(object sender, EventArgs e)
        {
            totalDeltaFigFisMMMinutes += CommonService.GetMinutesNumberFromHHMMString((string)GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Delta_Fig_Fis)));
        }

        //TOTALI DELTA FIG/FIS PER CANTIERE
        private void xrcTotalDeltaFigFisUMaster_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            string returnString = CommonService.GetHHMMStringFormMinutes(Math.Abs(totalDeltaFigFisCantMinutes));
            e.Result = String.Format("{0}{1}", totalDeltaFigFisCantMinutes < 0 ? "-" : String.Empty, returnString);
            e.Handled = true;
        }

        private void xrcTotalDeltaFigFisUMaster_SummaryReset(object sender, EventArgs e)
        {
            totalDeltaFigFisCantMinutes = 0;
        }

        private void xrcTotalDeltaFigFisUMaster_SummaryRowChanged(object sender, EventArgs e)
        {
            totalDeltaFigFisCantMinutes += CommonService.GetMinutesNumberFromHHMMString((string)GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Delta_Fig_Fis)));
        }

        #endregion

        private void TimeSpan_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.TimeSpanFormatter_BeforePrint(sender, e);
        }

        /// <summary>
        /// Handles the BeforePrint event of the hCanCell control [codice cantiere in testata].
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Drawing.Printing.PrintEventArgs"/> instance containing the event data.</param>
        private void hCanCell_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.StringTrimmer_BeforePrint(sender, e);
        }

        /// <summary>
        /// Handles the BeforePrint event of the tchfisPre2 control [codice cantiere in totali].
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Drawing.Printing.PrintEventArgs"/> instance containing the event data.</param>
        private void tchfisPre2_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.StringTrimmer_BeforePrint(sender, e);
        }

        /// <summary>
        /// Handles the BeforePrint event of the hLbl_Data_Reg control [Suffisso Mese in lingua su cella in testata tabella].
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Drawing.Printing.PrintEventArgs"/> instance containing the event data.</param>
        private void hLbl_Data_Reg_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.AddMonthLabel_BeforePrint(sender, e);
        }

        /// <summary>
        /// Handles the BeforePrint event of the tgLabel control [Suffisso totale del giorno].
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Drawing.Printing.PrintEventArgs"/> instance containing the event data.</param>
        private void tgLabel_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.AddPrefixTotal_BeforePrint(sender, e);
        }

        /// <summary>
        /// Handles the BeforePrint event of the tmLabel control [Suffisso totale del mese].
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Drawing.Printing.PrintEventArgs"/> instance containing the event data.</param>
        private void tmLabel_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.AddPrefixTotal_BeforePrint(sender, e);
        }

        /// <summary>
        /// Handles the BeforePrint event of the tcLabel control [Suffisso totale del cantiere].
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Drawing.Printing.PrintEventArgs"/> instance containing the event data.</param>
        private void tcLabel_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.AddPrefixTotal_BeforePrint(sender, e);
        }
               
    }


}
