using System;
using DevExpress.XtraReports.UI;
using System.Collections.Generic;
using Domain;
using Common;
using Business.Repository;
using Business.BusinessExtension;
using System.Linq;


namespace Reports
{
    public partial class XRRegSkCol : DevExpress.XtraReports.UI.XtraReport
    {
        private Reg_V _regvStub = null;

        public XRRegSkCol()
        {
            InitializeComponent();
        }

        public XRRegSkCol(List<Reg_V> regvs, List<String> sections = null, Dictionary<string, int> reportOptions = null)
            : this()
        {
            #region Nascondimento delle band mai visibili

            var groupColBand = Bands["Inizio_Col"];
            groupColBand.Visible = false;

            #endregion

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

        // Calcolo Durata Complessiva
        int totalFigGGMinutes = 0;
        int totalFigMMMinutes = 0;
        int totalFigColMinutes = 0;
        int totalFisGGMinutes = 0;
        int totalFisMMMinutes = 0;
        int totalFisColMinutes = 0;
        int totalDeltaFigFisGGMinutes = 0;
        int totalDeltaFigFisMMMinutes = 0;
        int totalDeltaFigFisColMinutes = 0;
        decimal totalKmGG = 0M;
        decimal totalKmMM = 0M;
        decimal totalKmCol = 0M;

        #region Calcolo Totali Ore FIS/FIG x Collaboratore/Anno/Mese/Giorno
        // TOTALI FIG X COLLABORATORE
        private void xrcTotalDurataFigCol_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            e.Result = CommonService.GetHHMMStringFormMinutes(totalFigColMinutes);
            e.Handled = true;
        }
        private void xrcTotalDurataFigCol_SummaryReset(object sender, EventArgs e)
        {
            totalFigColMinutes = 0;
        }
        private void xrcTotalDurataFigCol_SummaryRowChanged(object sender, EventArgs e)
        {
            object value = GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Durata_Fig));
            totalFigColMinutes += Convert.ToInt32(value);
        }
        // TOTALI FIS X COLLABORATORE       
        private void xrcTotalDurataFisCol_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            e.Result = CommonService.GetHHMMStringFormMinutes(totalFisColMinutes);
            e.Handled = true;
        }
        private void xrcTotalDurataFisCol_SummaryReset(object sender, EventArgs e)
        {
            totalFisColMinutes = 0;
        }
        private void xrcTotalDurataFisCol_SummaryRowChanged(object sender, EventArgs e)
        {
            object value = GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Durata_Fis));
            totalFisColMinutes += Convert.ToInt32(value);
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
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.SumJustificationsInRegVReport) == (int)SumJustificationsInRegVReport.Sum ||
                GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Motivazione_Reg_Id)) == null)
            {
                totalFigMMMinutes += Convert.ToInt32(value);
            }
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
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.SumJustificationsInRegVReport) == (int)SumJustificationsInRegVReport.Sum ||
                GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Motivazione_Reg_Id)) == null)
            { 
                totalFisMMMinutes += Convert.ToInt32(value);
            }
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
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.SumJustificationsInRegVReport) == (int)SumJustificationsInRegVReport.Sum ||
                GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Motivazione_Reg_Id)) == null)
            {
                totalFigGGMinutes += Convert.ToInt32(value);
            }
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
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.SumJustificationsInRegVReport) == (int)SumJustificationsInRegVReport.Sum ||
                GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Motivazione_Reg_Id)) == null)
            {
                totalFisGGMinutes += Convert.ToInt32(value);
            }
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

        // TOTALI DELTA FIG/FIS PER COLLABORATORE
        private void xrcTotalDeltaFigFisUMaster_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            string returnString = CommonService.GetHHMMStringFormMinutes(Math.Abs(totalDeltaFigFisColMinutes));
            e.Result = String.Format("{0}{1}", totalDeltaFigFisColMinutes < 0 ? "-" : String.Empty, returnString);
            e.Handled = true;
        }

        private void xrcTotalDeltaFigFisUMaster_SummaryReset(object sender, EventArgs e)
        {
            totalDeltaFigFisColMinutes = 0;
        }

        private void xrcTotalDeltaFigFisUMaster_SummaryRowChanged(object sender, EventArgs e)
        {
            totalDeltaFigFisColMinutes += CommonService.GetMinutesNumberFromHHMMString((string)GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.Delta_Fig_Fis)));
        }

        //TOTALI KM PER GIORNO
        private void xrcTotalKmGG_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            string returnString = Math.Abs(totalKmGG).ToString();
            e.Result = String.Format("{0}{1}", totalKmGG < 0 ? "-" : String.Empty, returnString);
            e.Handled = true;
        }

        private void xrcTotalKmGG_SummaryReset(object sender, EventArgs e)
        {
            totalKmGG = 0;
        }

        private void xrcTotalKmGG_SummaryRowChanged(object sender, EventArgs e)
        {
            if (GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.KM_Reg)) != null)
            {
                totalKmGG += (decimal)GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.KM_Reg));
            }
        }

        //TOTALI KM PER MESE
        private void xrcTotalKmMM_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            string returnString = Math.Abs(totalKmMM).ToString();
            e.Result = String.Format("{0}{1}", totalKmMM < 0 ? "-" : String.Empty, returnString);
            e.Handled = true;
        }

        private void xrcTotalKmMM_SummaryReset(object sender, EventArgs e)
        {
            totalKmMM = 0;
        }

        private void xrcTotalKmMM_SummaryRowChanged(object sender, EventArgs e)
        {
            if (GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.KM_Reg)) != null)
            {
                totalKmMM += (decimal)GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.KM_Reg));
            }
        }

        // TOTALI KM PER COLLABORATORE
        private void xrcTotalKmMaster_SummaryGetResult(object sender, SummaryGetResultEventArgs e)
        {
            string returnString = (Math.Abs(totalKmCol)).ToString();
            e.Result = String.Format("{0}{1}", totalKmCol < 0 ? "-" : String.Empty, returnString);
            e.Handled = true;
        }

        private void xrcTotalKmMaster_SummaryReset(object sender, EventArgs e)
        {
            totalKmCol = 0;
        }

        private void xrcTotalKmMaster_SummaryRowChanged(object sender, EventArgs e)
        {
            if(GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.KM_Reg)) != null)
            { 
                totalKmCol += (decimal)GetCurrentColumnValue(CommonService.GetPropertyName(() => _regvStub.KM_Reg));
            }
        }

        #endregion

        private void TimeSpan_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.TimeSpanFormatter_BeforePrint(sender, e);
        }

        /// <summary>
        /// Handles the BeforePrint event of the hColCell control [codice collaboratore in testata].
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Drawing.Printing.PrintEventArgs"/> instance containing the event data.</param>
        private void hColCell_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
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
        /// Handles the BeforePrint event of the tcLabel control [Suffisso totale del collaboratore].
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Drawing.Printing.PrintEventArgs"/> instance containing the event data.</param>
        private void tcLabel_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            CommonServiceReport.AddPrefixTotal_BeforePrint(sender, e);
        }

    }


}
