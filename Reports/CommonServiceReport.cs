using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Business;
using Common;
using DevExpress.XtraReports.UI;
using System.Web;
using System.IO;
using DevExpress.XtraPrinting;
using System.Globalization;
using System.Drawing;

namespace Reports
{
    public enum ReportTypeEnum
    {
        XRCant,
        XRCant_Fru,
        XRCant_Note,
        XRCli,
        XRCol,
        XRCol_Note,
        XRCol_Pru,
        XRFru,
        XRFru_Cant,
        XRParam,
        XRPru,
        XRPru_Col,
        XRTab_Aut,
        XRTab_Decod,
        XRUtenti,
    }

    public static class CommonServiceReport
    {
        public const float TABLE_WIDTHF = 650.0f;
        public const float TABLE_LOCATIONF_X = 9.5f;


        public enum ReportExportType
        {
            Xls,
            Xlsx,
            Pdf,
            Rtf,
            Csv,
        }

        public static MemoryStream CreateReport(HttpResponse response, string fileName, bool writeOnReponse, XtraReport currentReport, ReportExportType exportType = ReportExportType.Pdf)
        {
            if (String.IsNullOrEmpty(fileName))
                fileName = string.Format("Report.{0}", exportType.ToString().ToLowerInvariant());

            SetReportOption(currentReport);

            var stream = ExportReportToStream(currentReport, exportType);
            if (writeOnReponse)
                ExportToResponse(stream, response, String.Format("{0}.pdf", fileName), exportType, false);

            return stream;
        }

        private static void SetReportOption(XtraReport report)
        {
            var option = new PdfExportOptions
            {
                Compressed = true,
                ImageQuality = PdfJpegImageQuality.Medium,
                NeverEmbeddedFonts = string.Empty
            };
            report.ExportOptions.Pdf.Assign(option);
        }

        public static void ExportReport(XtraReport report, HttpResponse response, string fileName, ReportExportType reportExportType, bool inline)
        {
            var stream = ExportReportToStream(report, reportExportType);
            ExportToResponse(stream, response, fileName, reportExportType, inline);
        }

        public static MemoryStream ExportReportToStream(XtraReport report, ReportExportType reportExportType)
        {
            var stream = new MemoryStream();

            switch (reportExportType)
            {
                case ReportExportType.Csv:
                    report.ExportToCsv(stream);
                    break;

                case ReportExportType.Pdf:
                    report.ExportToPdf(stream);
                    break;

                case ReportExportType.Rtf:
                    report.ExportToRtf(stream);
                    break;

                case ReportExportType.Xls:
                    report.ExportToXls(stream);
                    break;

                case ReportExportType.Xlsx:
                    report.ExportToXlsx(stream);
                    break;
            }

            return stream;
        }

        public static void ExportToResponse(MemoryStream stream, HttpResponse response, string fileName, ReportExportType reportExportType, bool inline)
        {
            try
            {
                response.Clear();

                response.ContentType = "application/" + reportExportType;
                response.AddHeader("Accept-Header", stream.Length.ToString(CultureInfo.InvariantCulture));
                response.AddHeader("Content-Disposition", (inline ? "Inline" : "Attachment") + "; filename=" + fileName);
                response.Cache.SetCacheability(HttpCacheability.NoCache);
                response.BinaryWrite(stream.ToArray());
                response.AddHeader("Content-Length", stream.Length.ToString(CultureInfo.InvariantCulture));
                response.End();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
            finally
            {
                stream.Close();
                stream.Dispose();
            }
        }

        public static void DisableSections(XtraReport report, List<String> sections)
        {
            foreach (String section in sections)
            {
                XRControl currentControl = report.FindControl(section, false);
                if (currentControl != null)
                {
                    RecursiveCanShrink(currentControl);
                    currentControl.BeforePrint += currentControl_BeforePrint;
                    currentControl.PrintOnPage += currentControl_PrintOnPage;
                }
            }
        }

        static void currentControl_PrintOnPage(object sender, PrintOnPageEventArgs e)
        {
            e.Cancel = true;
        }

        static void currentControl_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            XRControl currentSender = sender as XRControl;
            if (currentSender != null)
                RemoveText(currentSender);
        }

        private static void RecursiveCanShrink(XRControl currentControl)
        {
            if (currentControl != null)
            {
                currentControl.CanShrink = true;
                foreach (XRControl child in currentControl.Controls)
                    RecursiveCanShrink(child);
            }
        }
        private static void RemoveText(XRControl currentSender)
        {
            if (currentSender != null)
            {
                currentSender.Text = String.Empty;
                foreach (XRControl control in currentSender.Controls)
                    RemoveText(control);
            }
        }

        public static void TimeSpanFormatter_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var currentCell = sender as XRTableCell;
            String result = String.Empty;
            if (currentCell != null && !String.IsNullOrEmpty(currentCell.Text))
            {
                DateTime dateTimeValue = DateTime.MinValue;
                if (DateTime.TryParse(currentCell.Text, out dateTimeValue))
                {
                    if (dateTimeValue != DateTime.MinValue)
                        result = String.Format("{0:HH:mm}", dateTimeValue);
                }
            }
            currentCell.Text = result;
        }

        /// <summary>
        /// Richiato da un evento before print, questo metodo trimma il valore che andrà stampato.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Drawing.Printing.PrintEventArgs"/> instance containing the event data.</param>
        public static void StringTrimmer_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var currentCell = sender as XRTableCell;
            currentCell.Text = String.IsNullOrEmpty(currentCell.Text) ? currentCell.Text : currentCell.Text.Trim();
        }

        /// <summary>
        /// Aggiunge alla cella passata come parametro l'etichetta suffisso in lingua del mese.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Drawing.Printing.PrintEventArgs"/> instance containing the event data.</param>
        public static void AddMonthLabel_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var currentCell = sender as XRTableCell;
            currentCell.Text = String.Format("{0} {1}", BusinessService.GetLocalizedString(PowerWebResources.LBL_MESE), currentCell.Text);
        }

        /// <summary>
        /// Aggiunge il suffisso totale per la cella passata come parametro.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Drawing.Printing.PrintEventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        public static void AddPrefixTotal_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var currentCell = sender as XRTableCell;
            currentCell.Text = String.Format("{0} {1}", BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE), currentCell.Text);
        }

        public static string TimeSpanToStringHhMm(int minutes)
        {
            //se i minuti sono maggiorni di zero converto i minutio in formato time span e quest'ultimo lo converto in stringa  
            return minutes > 0 ? TimeSpan.FromMinutes(minutes).ToString(@"hh\:mm") : string.Empty;
        }

        /// <summary>
        /// Gestisce le opzioni dei report delle reg_v per cantiere/collaboratore.
        /// </summary>
        /// <param name="reportToProcess">Il report su cui processare le informazioni.</param>
        /// <param name="reportOptions">Le opzioni da applicare al report.</param>
        public static void ManageRegVReportOptions(XtraReport reportToProcess, Dictionary<string, int> reportOptions)
        {
            // se sono state passate delle opzioni al report
            if (reportOptions != null)
            {
                if (reportOptions.Any())
                {
                    // calcolo della band di testata e di dettaglio
                    var detailBand = reportToProcess.Bands["Detail"];
                    var totalDayBand = reportToProcess.Bands["Totale_Giorno"];
                    var totalMonthBand = reportToProcess.Bands["Totale_Mese"];
                    var totalMasterDataBand = reportToProcess.Bands["Totale_Master"];
                    var groupMonthBand = reportToProcess.Bands["Inizio_Mese"];
                    var groupDayBand = reportToProcess.Bands["Inizio_Giorno"];

                    // calcolo della tabella di di testata, di dettaglio e dei totali
                    var headerTable = groupMonthBand.Controls["tbHeaderDetail"] as XRTable;
                    var detailTable = detailBand.Controls["tbDetail"] as XRTable;
                    var totalDayTable = totalDayBand.Controls["tbTotGiorno"] as XRTable;
                    var totalMonthTable = totalMonthBand.Controls["tbTotMese"] as XRTable;
                    var totalMasterDataTable = totalMasterDataBand.Controls["tbTotMaster"] as XRTable;

                    // calcolo della riga della tabella di testata e di dettaglio
                    var headerTableRow = headerTable.Controls[0] as XRTableRow;
                    var detailTableRow = detailTable.Controls[0] as XRTableRow;
                    var totalDayTableRow = totalDayTable.Controls[0] as XRTableRow;
                    var totalMonthTableRow = totalMonthTable.Controls[0] as XRTableRow;
                    var totalMasterDataTableRow = totalMasterDataTable.Controls[0] as XRTableRow;

                    // calcolo delle righe di demarcazione dei totali
                    var totDayLine = totalDayBand.Controls["totDayLine"];
                    var totMonthLine = totalMonthBand.Controls["totMonthLine"];
                    var totMasterLineUp = totalMasterDataBand.Controls["totMasterLineUp"];
                    var totMasterLineDown = totalMasterDataBand.Controls["totMasterLineDown"];

                    XRTableCell cellToRemove = null;

                    bool optCliCode = false;

                    // ciclo su tutte le opzioni passate come parametro al report
                    foreach (var rptOpt in reportOptions)
                    {
                        // in base all'opzionie
                        switch (rptOpt.Key)
                        {
                            case "OPZ_ORE_FIS":
                                // se è richiesto di nascondere le ore fisiche, allora le si tolgono dalla testata e dal dettaglio
                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento delle ore fisiche

                                    // cancellazione delle celle di testata delle ore fisiche
                                    cellToRemove = headerTableRow.Controls["hLbl_E_Fis"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);
                                    cellToRemove = headerTableRow.Controls["hLbl_U_Fis"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);
                                    cellToRemove = headerTableRow.Controls["hLbl_Durata_Fis"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle di riga delle ore fisiche
                                    cellToRemove = detailTableRow.Controls["dData_Ora_Fis_E"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);
                                    cellToRemove = detailTableRow.Controls["dData_Ora_Fis_U"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);
                                    cellToRemove = detailTableRow.Controls["dDurata_Fis_HH_S"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione della cella di totale e della cella di allineamento
                                    cellToRemove = totalDayTableRow.Controls["tghfisPre1"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalDayTableRow.Controls["tghfisPre2"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalDayTableRow.Controls["xrcTotalDurataFisGG"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMonthTableRow.Controls["tmhfisPre1"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMonthTableRow.Controls["tmhfisPre2"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMonthTableRow.Controls["xrcTotalDurataFisMM"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMasterDataTableRow.Controls["tchfisPre1"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMasterDataTableRow.Controls["tchfisPre2"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMasterDataTableRow.Controls["xrcTotalDurataFisMaster"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);



                                    #endregion

                                }
                                break;

                            case "OPZ_ORE_FIG":
                                // se è richiesto di nascondere le ore figurative, allora le si tolgono dalla testata e dal dettaglio
                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento delle ore figurative

                                    // cancellazione delle celle di testata delle ore figurative
                                    cellToRemove = headerTableRow.Controls["hLbl_E_Fig"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);
                                    cellToRemove = headerTableRow.Controls["hLbl_U_Fig"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);
                                    cellToRemove = headerTableRow.Controls["hLbl_Durata_Fig"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle di riga delle ore figurative
                                    cellToRemove = detailTableRow.Controls["dData_Ora_Fig_E"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);
                                    cellToRemove = detailTableRow.Controls["dData_Ora_Fig_U"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);
                                    cellToRemove = detailTableRow.Controls["dDurata_Fig_HH_S"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione della cella di totale e della cella di allineamento
                                    cellToRemove = totalDayTableRow.Controls["tghfigPre5"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalDayTableRow.Controls["tghfigPre6"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalDayTableRow.Controls["xrcTotalDurataFigGG"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMonthTableRow.Controls["tmhfigPre5"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMonthTableRow.Controls["tmhfigPre6"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMonthTableRow.Controls["xrcTotalDurataFigMM"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMasterDataTableRow.Controls["tchfigPre5"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMasterDataTableRow.Controls["tchfigPre6"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMasterDataTableRow.Controls["xrcTotalDurataFigMaster"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);

                                    #endregion

                                }
                                break;

                            case "OPZ_MOTIVAZIONE":
                                // se è richiesto di nascondere la motivazione, allora si toglie il dato dalla testata e dal dettaglio
                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento della motivazione

                                    // cancellazione delle celle di testata della motivazione
                                    cellToRemove = headerTableRow.Controls["hLbl_Mot"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle di riga della motivazione
                                    cellToRemove = detailTableRow.Controls["dMotivazione_Reg_Cod"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione dei corrispondenti nelle righe di totale
                                    cellToRemove = totalDayTableRow.Controls["tghfigPre1"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMonthTableRow.Controls["tmhfigPre1"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMasterDataTableRow.Controls["tchfigPre1"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);

                                    #endregion

                                }
                                break;

                            case "OPZ_TIPO_MODIFICA":
                                // se è richiesto di nascondere il tipo modifica, allora si toglie il dato dalla testata e dal dettaglio
                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento del tipo modifica

                                    // cancellazione delle celle di testata del tipo modifica
                                    cellToRemove = headerTableRow.Controls["hLbl_Mod"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle di riga del tipo modifica
                                    cellToRemove = detailTableRow.Controls["dTipo_Modifica"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione dei corrispondenti nelle righe di totale
                                    cellToRemove = totalDayTableRow.Controls["tghfigPre2"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMonthTableRow.Controls["tmhfigPre2"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMasterDataTableRow.Controls["tchfigPre2"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);

                                    #endregion

                                }
                                break;

                            case "OPZ_TIPO_REG":
                                // se è richiesto di nascondere il tipo registrazione, allora si toglie il dato dalla testata e dal dettaglio
                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento del tipo modifica

                                    // cancellazione delle celle di testata del tipo registrazione
                                    cellToRemove = headerTableRow.Controls["hLbl_Tipo"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle di riga del tipo registrazione
                                    cellToRemove = detailTableRow.Controls["dRegistrazione_Tipo_Reg"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione dei corrispondenti nelle righe di totale
                                    cellToRemove = totalDayTableRow.Controls["tghfigPre3"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMonthTableRow.Controls["tmhfigPre3"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMasterDataTableRow.Controls["tchfigPre3"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);

                                    #endregion

                                }
                                break;

                            case "OPZ_STATO_REG":
                                // se è richiesto di nascondere lo stato registrazione, allora si toglie il dato dalla testata e dal dettaglio
                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento del tipo modifica

                                    // cancellazione delle celle di testata dello stato registrazione
                                    cellToRemove = headerTableRow.Controls["hLbl_Stato"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle di riga dello stato registrazione
                                    cellToRemove = detailTableRow.Controls["dRegistrazione_Stato_Reg"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione dei corrispondenti nelle righe di totale
                                    cellToRemove = totalDayTableRow.Controls["tghfigPre4"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMonthTableRow.Controls["tmhfigPre4"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMasterDataTableRow.Controls["tchfigPre4"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);

                                    #endregion

                                }
                                break;

                            case "OPZ_TOT_GIORNO":
                                // se è richiesto di nascondere il totale del giorno, allora si toglie la band di visualizzazione del dato
                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento del totale giorno

                                    totalDayTableRow.HeightF = 0.0f;
                                    totalDayTable.HeightF = 0.0f;
                                    totalDayTable.Visible = false;
                                    totDayLine.Visible = false;
                                    totalDayBand.HeightF = 0.0f;
                                    #endregion

                                }
                                break;

                            case "OPZ_TOT_MESE":
                                // se è richiesto di nascondere il totale del mese, allora si toglie la band di visualizzazione del dato
                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento del totale del mese

                                    totalMonthTableRow.HeightF = 0.0f;
                                    totalMonthTable.HeightF = 0.0f;
                                    totalMonthTable.Visible = false;
                                    if (totMonthLine != null)
                                        totMonthLine.Visible = false;
                                    totalMonthBand.HeightF = 0.0f;

                                    #endregion

                                }
                                break;

                            case "OPZ_TOT_COL":
                                // se è richiesto di nascondere il totale del collaboratore, allora si toglie la band di visualizzazione del dato
                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento del totale collaboratore

                                    totalMasterDataTableRow.HeightF = 0.0f;
                                    totalMasterDataTable.HeightF = 0.0f;
                                    totalMasterDataTable.Visible = false;
                                    totMasterLineUp.Visible = false;
                                    totMasterLineDown.Visible = false;
                                    totalMasterDataBand.HeightF = 0.0f;

                                    #endregion

                                }
                                break;

                            case "OPZ_TOT_CAN":
                                // se è richiesto di nascondere il totale del cantiere, allora si toglie la band di visualizzazione del dato
                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento del totale cantiere

                                    totalMasterDataTableRow.HeightF = 0.0f;
                                    totalMasterDataTable.HeightF = 0.0f;
                                    totalMasterDataTable.Visible = false;
                                    totMasterLineUp.Visible = false;
                                    totMasterLineDown.Visible = false;
                                    totalMasterDataBand.HeightF = 0.0f;

                                    #endregion

                                }
                                break;

                            case "OPZ_TESTATA_GIORNO":
                                // se è richiesto di nascondere la band del raggruppamento del giorno, allora si nasconde la band di visualizzazione del dato
                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento del raggruppamento per giorno

                                    groupDayBand.Visible = false;

                                    #endregion

                                }

                                break;

                            case "OPZ_BORDI_COL": //opzione che permette la tracciatura dei bordi in alcune celle del report per collaboratore
                                if (Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Gestione della tracciatura dei bordi

                                    var dataOraFisE = detailTableRow.Controls["dData_Ora_Fis_E"] as XRTableCell ?? default(XRTableCell);
                                    var dataOraFisU = detailTableRow.Controls["dData_Ora_Fis_U"] as XRTableCell ?? default(XRTableCell);
                                    var dataOraFigE = detailTableRow.Controls["dData_Ora_Fig_E"] as XRTableCell ?? default(XRTableCell);
                                    var dataOraFigU = detailTableRow.Controls["dData_Ora_Fig_U"] as XRTableCell ?? default(XRTableCell);
                                    var motiv = detailTableRow.Controls["dMotivazione_Reg_Cod"] as XRTableCell ?? default(XRTableCell);
                                    var tipoMod = detailTableRow.Controls["dTipo_Modifica"] as XRTableCell ?? default(XRTableCell);
                                    var tipoReg = detailTableRow.Controls["dRegistrazione_Tipo_Reg"] as XRTableCell ?? default(XRTableCell);
                                    var statoReg = detailTableRow.Controls["dRegistrazione_Stato_Reg"] as XRTableCell ?? default(XRTableCell);
                                    var cantDesc = detailTableRow.Controls["dCant_Desc"] as XRTableCell ?? default(XRTableCell);
                                    var noteReg = detailTableRow.Controls["dNoteReg"] as XRTableCell ?? default(XRTableCell);
                                    var km = detailTableRow.Controls["dKm"] as XRTableCell ?? default(XRTableCell);
                                    var flagEntrataUscitaE = detailTableRow.Controls["dEntrataUscitaE"] as XRTableCell ?? default(XRTableCell);
                                    var flagEntrataUscitaU = detailTableRow.Controls["dEntrataUscitaU"] as XRTableCell ?? default(XRTableCell);
                                    var deltaFigFis = detailTableRow.Controls["dDeltaFigFis"] as XRTableCell ?? default(XRTableCell);

                                    var dataReg = totalDayTableRow.Controls["tgLabel"] as XRTableCell ?? default(XRTableCell);
                                    var emptyCel1 = totalDayTableRow.Controls["tghfisPre1"] as XRTableCell ?? default(XRTableCell);
                                    var emptyCel2 = totalDayTableRow.Controls["tghfisPre2"] as XRTableCell ?? default(XRTableCell);
                                    var emptyCelFig1 = totalDayTableRow.Controls["tghfigPre1"] as XRTableCell ?? default(XRTableCell);
                                    var emptyCelFig2 = totalDayTableRow.Controls["tghfigPre2"] as XRTableCell ?? default(XRTableCell);
                                    var emptyCelFig3 = totalDayTableRow.Controls["tghfigPre3"] as XRTableCell ?? default(XRTableCell);
                                    var emptyCelFig4 = totalDayTableRow.Controls["tghfigPre4"] as XRTableCell ?? default(XRTableCell);
                                    var emptyCelFig5 = totalDayTableRow.Controls["tghfigPre5"] as XRTableCell ?? default(XRTableCell);
                                    var emptyCelFig6 = totalDayTableRow.Controls["tghfigPre6"] as XRTableCell ?? default(XRTableCell);
                                    var colDesc = totalDayTableRow.Controls["xrTableCell1"] as XRTableCell ?? default(XRTableCell);
                                    var noteRegDay = totalDayTableRow.Controls["xrTableCell5"] as XRTableCell ?? default(XRTableCell);
                                    var flagEntrataUscitaEDay = totalDayTableRow.Controls["xrcTotalEntrataUscitaEGG"] as XRTableCell ?? default(XRTableCell);
                                    var flagEntrataUscitaUDay = totalDayTableRow.Controls["xrcTotalEntrataUscitaUGG"] as XRTableCell ?? default(XRTableCell);
                                    var deltaFigFisDay = totalDayTableRow.Controls["xrcTotalDeltaFigFisUGG"] as XRTableCell ?? default(XRTableCell);
                                    var sumFigCel = totalDayTableRow.Controls["xrcTotalDurataFigGG"] as XRTableCell ?? default(XRTableCell);
                                    var sumCel = totalDayTableRow.Controls["xrcTotalDurataFisGG"] as XRTableCell ?? default(XRTableCell);
                                    var sumKmCel = totalDayTableRow.Controls["xrcTotalKmGG"] as XRTableCell ?? default(XRTableCell);

                                    #region TOTALE GIORNO
                                    if (dataReg != default(XRTableCell))
                                    {
                                        dataReg.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        dataReg.BorderWidth = 1;
                                        dataReg.BorderColor = Color.Black;

                                        if (emptyCel1 != default(XRTableCell))
                                        {
                                            emptyCel1.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                            emptyCel1.BorderWidth = 1;
                                            emptyCel1.BorderColor = Color.Black;
                                        }

                                        if (emptyCel2 != default(XRTableCell))
                                        {
                                            emptyCel2.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                            emptyCel2.BorderWidth = 1;
                                            emptyCel2.BorderColor = Color.Black;
                                        }


                                    }

                                    if (sumCel != default(XRTableCell) && dataOraFisE != default(XRTableCell))
                                    {
                                        sumCel.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        sumCel.BorderWidth = 1;
                                        sumCel.BorderColor = Color.Black;

                                    }

                                    if (sumKmCel != default(XRTableCell))
                                    {
                                        sumKmCel.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        sumKmCel.BorderWidth = 1;
                                        sumKmCel.BorderColor = Color.Black;
                                    }

                                    if (sumFigCel != default(XRTableCell) && dataOraFigE != default(XRTableCell))
                                    {
                                        emptyCelFig5.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        emptyCelFig5.BorderWidth = 1;
                                        emptyCelFig5.BorderColor = Color.Black;

                                        emptyCelFig6.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        emptyCelFig6.BorderWidth = 1;
                                        emptyCelFig6.BorderColor = Color.Black;


                                        sumFigCel.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        sumFigCel.BorderWidth = 1;
                                        sumFigCel.BorderColor = Color.Black;

                                    }

                                    if (emptyCelFig1 != default(XRTableCell) && motiv != default(XRTableCell))
                                    {
                                        emptyCelFig1.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        emptyCelFig1.BorderWidth = 1;
                                        emptyCelFig1.BorderColor = Color.Black;

                                    }

                                    if (emptyCelFig2 != default(XRTableCell) && tipoMod != default(XRTableCell))
                                    {
                                        emptyCelFig2.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        emptyCelFig2.BorderWidth = 1;
                                        emptyCelFig2.BorderColor = Color.Black;

                                    }

                                    if (emptyCelFig3 != default(XRTableCell) && tipoReg != default(XRTableCell))
                                    {
                                        emptyCelFig3.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        emptyCelFig3.BorderWidth = 1;
                                        emptyCelFig3.BorderColor = Color.Black;

                                    }


                                    if (emptyCelFig4 != default(XRTableCell) && statoReg != default(XRTableCell))
                                    {
                                        emptyCelFig4.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        emptyCelFig4.BorderWidth = 1;
                                        emptyCelFig4.BorderColor = Color.Black;

                                    }

                                    if (colDesc != default(XRTableCell) && cantDesc != default(XRTableCell))
                                    {
                                        colDesc.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        colDesc.BorderWidth = 1;
                                        colDesc.BorderColor = Color.Black;


                                    }
                                    #endregion

                                    if (dataOraFisE != default(XRTableCell))
                                    {
                                        dataOraFisE.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        dataOraFisU.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        dataOraFisE.BorderWidth = 1;
                                        dataOraFisU.BorderWidth = 1;
                                        dataOraFisE.BorderColor = Color.DarkGray;
                                        dataOraFisU.BorderColor = Color.DarkGray;

                                    }
                                    if (dataOraFigE != default(XRTableCell))
                                    {
                                        dataOraFigE.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        dataOraFigU.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        dataOraFigE.BorderWidth = 1;
                                        dataOraFigU.BorderWidth = 1;
                                        dataOraFigE.BorderColor = Color.DarkGray;
                                        dataOraFigU.BorderColor = Color.DarkGray;
                                    }
                                    if (motiv != default(XRTableCell))
                                    {
                                        motiv.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        motiv.BorderWidth = 1;
                                        motiv.BorderColor = Color.DarkGray;
                                    }

                                    if (tipoMod != default(XRTableCell))
                                    {
                                        tipoMod.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        tipoMod.BorderWidth = 1;
                                        tipoMod.BorderColor = Color.DarkGray;
                                    }
                                    if (tipoReg != default(XRTableCell))
                                    {
                                        tipoReg.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        tipoReg.BorderWidth = 1;
                                        tipoReg.BorderColor = Color.DarkGray;
                                    }

                                    if (statoReg != default(XRTableCell))
                                    {
                                        statoReg.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        statoReg.BorderWidth = 1;
                                        statoReg.BorderColor = Color.DarkGray;
                                    }

                                    if (cantDesc != default(XRTableCell))
                                    {
                                        cantDesc.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        cantDesc.BorderWidth = 1;
                                        cantDesc.BorderColor = Color.DarkGray;
                                    }
                                    if (flagEntrataUscitaE != default(XRTableCell))
                                    {
                                        flagEntrataUscitaE.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        flagEntrataUscitaE.BorderWidth = 1;
                                        flagEntrataUscitaE.BorderColor = Color.DarkGray;

                                        flagEntrataUscitaEDay.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        flagEntrataUscitaEDay.BorderWidth = 1;
                                        flagEntrataUscitaEDay.BorderColor = Color.Black;
                                    }
                                    if (flagEntrataUscitaU != default(XRTableCell))
                                    {
                                        flagEntrataUscitaU.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        flagEntrataUscitaU.BorderWidth = 1;
                                        flagEntrataUscitaU.BorderColor = Color.DarkGray;

                                        flagEntrataUscitaUDay.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        flagEntrataUscitaUDay.BorderWidth = 1;
                                        flagEntrataUscitaUDay.BorderColor = Color.Black;
                                    }
                                    if (deltaFigFis != default(XRTableCell))
                                    {
                                        deltaFigFis.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        deltaFigFis.BorderWidth = 1;
                                        deltaFigFis.BorderColor = Color.DarkGray;

                                        deltaFigFisDay.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        deltaFigFisDay.BorderWidth = 1;
                                        deltaFigFisDay.BorderColor = Color.Black;
                                    }
                                    if (noteReg != default(XRTableCell))
                                    {
                                        noteReg.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        noteReg.BorderWidth = 1;
                                        noteReg.BorderColor = Color.DarkGray;

                                        noteRegDay.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                                        noteRegDay.BorderWidth = 1;
                                        noteRegDay.BorderColor = Color.Black;
                                    }

                                    detailTable.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;

                                    detailTable.BorderWidth = 1;
                                    detailTable.BorderColor = Color.DarkGray;

                                    #endregion

                                }
                                break;

                            case "OPZ_BORDI_CAN":
                                if (Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Gestione della tracciatura dei bordi

                                    var dataOraFisE = detailTableRow.Controls["dData_Ora_Fis_E"] as XRTableCell ?? default(XRTableCell);
                                    var dataOraFisU = detailTableRow.Controls["dData_Ora_Fis_U"] as XRTableCell ?? default(XRTableCell);
                                    var dataOraFigE = detailTableRow.Controls["dData_Ora_Fig_E"] as XRTableCell ?? default(XRTableCell);
                                    var dataOraFigU = detailTableRow.Controls["dData_Ora_Fig_U"] as XRTableCell ?? default(XRTableCell);
                                    var motiv = detailTableRow.Controls["dMotivazione_Reg_Cod"] as XRTableCell ?? default(XRTableCell);
                                    var tipoMod = detailTableRow.Controls["dTipo_Modifica"] as XRTableCell ?? default(XRTableCell);
                                    var tipoReg = detailTableRow.Controls["dRegistrazione_Tipo_Reg"] as XRTableCell ?? default(XRTableCell);
                                    var statoReg = detailTableRow.Controls["dRegistrazione_Stato_Reg"] as XRTableCell ?? default(XRTableCell);
                                    var colDesc = detailTableRow.Controls["dCol_Desc"] as XRTableCell ?? default(XRTableCell);
                                    var flagEntrataUscitaE = detailTableRow.Controls["dEntrataUscitaE"] as XRTableCell ?? default(XRTableCell);
                                    var flagEntrataUscitaU = detailTableRow.Controls["dEntrataUscitaU"] as XRTableCell ?? default(XRTableCell);
                                    var deltaFigFis = detailTableRow.Controls["dDeltaFigFis"] as XRTableCell ?? default(XRTableCell);


                                    if (dataOraFisE != default(XRTableCell))
                                    {
                                        dataOraFisE.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        dataOraFisU.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        dataOraFisE.BorderWidth = 1;
                                        dataOraFisU.BorderWidth = 1;
                                        dataOraFisE.BorderColor = Color.DarkGray;
                                        dataOraFisU.BorderColor = Color.DarkGray;

                                    }
                                    if (dataOraFigU != default(XRTableCell))
                                    {
                                        dataOraFigE.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        dataOraFigU.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        dataOraFigE.BorderWidth = 1;
                                        dataOraFigU.BorderWidth = 1;
                                        dataOraFigE.BorderColor = Color.DarkGray;
                                        dataOraFigU.BorderColor = Color.DarkGray;
                                    }
                                    if (motiv != default(XRTableCell))
                                    {
                                        motiv.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        motiv.BorderWidth = 1;
                                        motiv.BorderColor = Color.DarkGray;
                                    }

                                    if (tipoMod != default(XRTableCell))
                                    {
                                        tipoMod.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        tipoMod.BorderWidth = 1;
                                        tipoMod.BorderColor = Color.DarkGray;
                                    }
                                    if (tipoReg != default(XRTableCell))
                                    {
                                        tipoReg.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        tipoReg.BorderWidth = 1;
                                        tipoReg.BorderColor = Color.DarkGray;
                                    }

                                    if (statoReg != default(XRTableCell))
                                    {
                                        statoReg.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        statoReg.BorderWidth = 1;
                                        statoReg.BorderColor = Color.DarkGray;
                                    }

                                    if (colDesc != default(XRTableCell))
                                    {
                                        colDesc.Borders = DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        colDesc.BorderWidth = 1;
                                        colDesc.BorderColor = Color.DarkGray;
                                    }
                                    if (flagEntrataUscitaE != default(XRTableCell))
                                    {
                                        flagEntrataUscitaE.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        flagEntrataUscitaE.BorderWidth = 1;
                                        flagEntrataUscitaE.BorderColor = Color.DarkGray;
                                    }
                                    if (flagEntrataUscitaU != default(XRTableCell))
                                    {
                                        flagEntrataUscitaU.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        flagEntrataUscitaU.BorderWidth = 1;
                                        flagEntrataUscitaU.BorderColor = Color.DarkGray;
                                    }
                                    if (deltaFigFis != default(XRTableCell))
                                    {
                                        deltaFigFis.Borders = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom;
                                        deltaFigFis.BorderWidth = 1;
                                        deltaFigFis.BorderColor = Color.DarkGray;
                                    }

                                    detailTable.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;

                                    detailTable.BorderWidth = 1;
                                    detailTable.BorderColor = Color.DarkGray;

                                    #endregion

                                }
                                break;

                            case "OPZ_FLAG_EU": // visualizzazione o meno del flag in entrata
                                if (!Convert.ToBoolean(rptOpt.Value)) // se è richiesto il nascondimento dei flag di entrata e uscita
                                {

                                    #region Flag di stampa del flag di entrata/uscita

                                    // cancellazione delle celle nella tabella di testata
                                    cellToRemove = headerTableRow.Controls["hEntrataUscitaE"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);
                                    cellToRemove = headerTableRow.Controls["hEntrataUscitaU"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nella tabella di dettaglio
                                    cellToRemove = detailTableRow.Controls["dEntrataUscitaE"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);
                                    cellToRemove = detailTableRow.Controls["dEntrataUscitaU"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nelle tabelle di totale
                                    cellToRemove = totalDayTableRow.Controls["xrcTotalEntrataUscitaEGG"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalDayTableRow.Controls["xrcTotalEntrataUscitaUGG"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMonthTableRow.Controls["xrcTotalEntrataUscitaEMM"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMonthTableRow.Controls["xrcTotalEntrataUscitaUMM"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMasterDataTableRow.Controls["xrcTotalEntrataUscitaEMaster"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalMasterDataTableRow.Controls["xrcTotalEntrataUscitaUMaster"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);

                                    #endregion

                                }
                                break;

                            case "OPZ_SHOW_DELTA_FIG_FIS": // visualizzazione o meno del delta delle ore figurative/fisiche

                                if (!Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Flag di stampa del delta delle ore figurative/fisiche

                                    // cancellazione delle celle nella tabella di testata
                                    cellToRemove = headerTableRow.Controls["hDeltaFigFis"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nella tabella di dettaglio
                                    cellToRemove = detailTableRow.Controls["dDeltaFigFis"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nelle tabelle di totale
                                    cellToRemove = totalDayTableRow.Controls["xrcTotalDeltaFigFisUGG"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMonthTableRow.Controls["xrcTotalDeltaFigFisUMM"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMasterDataTableRow.Controls["xrcTotalDeltaFigFisUMaster"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);

                                    #endregion
                                }
                                break;

                            case "OPZ_ONLY_DURATION": // visualizzazione della sola durata nel report

                                if (Convert.ToBoolean(rptOpt.Value))
                                {

                                    #region Nascondimento della sola durata

                                    // cancellazione delle celle di testata delle ore fisiche
                                    if (headerTableRow.Controls["hLbl_E_Fis"] != null)
                                    {
                                        cellToRemove = headerTableRow.Controls["hLbl_E_Fis"] as XRTableCell;
                                        headerTable.DeleteColumn(cellToRemove);
                                    }

                                    if (headerTableRow.Controls["hLbl_U_Fis"] != null)
                                    {
                                        cellToRemove = headerTableRow.Controls["hLbl_U_Fis"] as XRTableCell;
                                        headerTable.DeleteColumn(cellToRemove);
                                    }

                                    // cancellazione delle celle di riga delle ore fisiche
                                    if (detailTableRow.Controls["dData_Ora_Fis_E"] != null)
                                    {
                                        cellToRemove = detailTableRow.Controls["dData_Ora_Fis_E"] as XRTableCell;
                                        detailTable.DeleteColumn(cellToRemove);
                                    }

                                    if (detailTableRow.Controls["dData_Ora_Fis_U"] != null)
                                    {
                                        cellToRemove = detailTableRow.Controls["dData_Ora_Fis_U"] as XRTableCell;
                                        detailTable.DeleteColumn(cellToRemove);
                                    }


                                    // cancellazione della cella di totale e della cella di allineamento
                                    if (totalDayTableRow.Controls["tghfisPre1"] != null)
                                    {
                                        cellToRemove = totalDayTableRow.Controls["tghfisPre1"] as XRTableCell;
                                        totalDayTable.DeleteColumn(cellToRemove);
                                    }
                                    if (totalDayTableRow.Controls["tghfisPre2"] != null)
                                    {
                                        cellToRemove = totalDayTableRow.Controls["tghfisPre2"] as XRTableCell;
                                        totalDayTable.DeleteColumn(cellToRemove);
                                    }

                                    if (totalMonthTableRow.Controls["tmhfisPre1"] != null)
                                    {
                                        cellToRemove = totalMonthTableRow.Controls["tmhfisPre1"] as XRTableCell;
                                        totalMonthTable.DeleteColumn(cellToRemove);
                                    }
                                    if (totalMonthTableRow.Controls["tmhfisPre2"] != null)
                                    {
                                        cellToRemove = totalMonthTableRow.Controls["tmhfisPre2"] as XRTableCell;
                                        totalMonthTable.DeleteColumn(cellToRemove);
                                    }

                                    if (totalMasterDataTableRow.Controls["tchfisPre1"] != null)
                                    {
                                        cellToRemove = totalMasterDataTableRow.Controls["tchfisPre1"] as XRTableCell;
                                        totalMasterDataTable.DeleteColumn(cellToRemove);
                                    }
                                    if (totalMasterDataTableRow.Controls["tchfisPre2"] != null)
                                    {
                                        cellToRemove = totalMasterDataTableRow.Controls["tchfisPre2"] as XRTableCell;
                                        totalMasterDataTable.DeleteColumn(cellToRemove);
                                    }

                                    // cancellazione delle celle di testata delle ore figurative
                                    if (headerTableRow.Controls["hLbl_E_Fig"] != null)
                                    {
                                        cellToRemove = headerTableRow.Controls["hLbl_E_Fig"] as XRTableCell;
                                        headerTable.DeleteColumn(cellToRemove);
                                    }
                                    if (headerTableRow.Controls["hLbl_U_Fig"] != null)
                                    {
                                        cellToRemove = headerTableRow.Controls["hLbl_U_Fig"] as XRTableCell;
                                        headerTable.DeleteColumn(cellToRemove);
                                    }

                                    // cancellazione delle celle di riga delle ore figurative
                                    if (detailTableRow.Controls["dData_Ora_Fig_E"] != null)
                                    {
                                        cellToRemove = detailTableRow.Controls["dData_Ora_Fig_E"] as XRTableCell;
                                        detailTable.DeleteColumn(cellToRemove);
                                    }
                                    if (detailTableRow.Controls["dData_Ora_Fig_U"] != null)
                                    {
                                        cellToRemove = detailTableRow.Controls["dData_Ora_Fig_U"] as XRTableCell;
                                        detailTable.DeleteColumn(cellToRemove);
                                    }

                                    // cancellazione della cella di totale e della cella di allineamento
                                    if (totalDayTableRow.Controls["tghfigPre5"] != null)
                                    {
                                        cellToRemove = totalDayTableRow.Controls["tghfigPre5"] as XRTableCell;
                                        totalDayTable.DeleteColumn(cellToRemove);
                                    }
                                    if (totalDayTableRow.Controls["tghfigPre6"] != null)
                                    {
                                        cellToRemove = totalDayTableRow.Controls["tghfigPre6"] as XRTableCell;
                                        totalDayTable.DeleteColumn(cellToRemove);
                                    }

                                    if (totalMonthTableRow.Controls["tmhfigPre5"] != null)
                                    {
                                        cellToRemove = totalMonthTableRow.Controls["tmhfigPre5"] as XRTableCell;
                                        totalMonthTable.DeleteColumn(cellToRemove);
                                    }
                                    if (totalMonthTableRow.Controls["tmhfigPre6"] != null)
                                    {
                                        cellToRemove = totalMonthTableRow.Controls["tmhfigPre6"] as XRTableCell;
                                        totalMonthTable.DeleteColumn(cellToRemove);
                                    }

                                    if (totalMasterDataTableRow.Controls["tchfigPre5"] != null)
                                    {
                                        cellToRemove = totalMasterDataTableRow.Controls["tchfigPre5"] as XRTableCell;
                                        totalMasterDataTable.DeleteColumn(cellToRemove);
                                    }
                                    if (totalMasterDataTableRow.Controls["tchfigPre6"] != null)
                                    {
                                        cellToRemove = totalMasterDataTableRow.Controls["tchfigPre6"] as XRTableCell;
                                        totalMasterDataTable.DeleteColumn(cellToRemove);
                                    }

                                    #endregion

                                }
                                break;

                            case "OPZ_KM": // visualizzazione dei kilometri nel report

                                if (!Convert.ToBoolean(rptOpt.Value))
                                {
                                    #region Nascondimento dei kilometri
                                    // cancellazione delle celle nella tabella di testata
                                    cellToRemove = headerTableRow.Controls["hLbl_Km"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nella tabella di dettaglio
                                    cellToRemove = detailTableRow.Controls["dKm"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nelle tabelle di totale
                                    cellToRemove = totalDayTableRow.Controls["xrcTotalKmGG"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMonthTableRow.Controls["xrcTotalKmMM"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMasterDataTableRow.Controls["xrcTotalKmMaster"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);
                                    #endregion
                                }
                                break;

                            case "OPZ_NOTES": // visualizzazione delle note nel report

                                if (!Convert.ToBoolean(rptOpt.Value))
                                {
                                    #region Nascondimento delle note
                                    // cancellazione delle celle nella tabella di testata
                                    cellToRemove = headerTableRow.Controls["hNoteReg"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nella tabella di dettaglio
                                    cellToRemove = detailTableRow.Controls["dNoteReg"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nelle tabelle di totale
                                    cellToRemove = totalDayTableRow.Controls["xrTableCell5"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMonthTableRow.Controls["xrTableCell6"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMasterDataTableRow.Controls["xrTableCell7"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);
                                    #endregion
                                }
                                break;


                            case "OPZ_CANT": // visualizzazione del cantiere nel report

                                if (!Convert.ToBoolean(rptOpt.Value))
                                {
                                    #region Nascondimento dell cantiere
                                    // cancellazione delle celle nella tabella di testata
                                    cellToRemove = headerTableRow.Controls["hEti_Cant_Desc"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nella tabella di dettaglio
                                    cellToRemove = detailTableRow.Controls["dCant_Desc"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nelle tabelle di totale
                                    cellToRemove = totalDayTableRow.Controls["xrTableCell1"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMonthTableRow.Controls["xrTableCell4"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMasterDataTableRow.Controls["totColDesCell"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);
                                    #endregion
                                }
                                break;

                            case "OPZ_CLIENTE": // visualizzazione del cliente nel report

                                optCliCode = Convert.ToBoolean(rptOpt.Value);

                                if (!optCliCode)
                                {
                                    #region Nascondimento dell cantiere

                                    cellToRemove = headerTableRow.Controls["hCliReg"] as XRTableCell;
                                    headerTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nella tabella di dettaglio
                                    cellToRemove = detailTableRow.Controls["dCli_Code"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    cellToRemove = detailTableRow.Controls["dCli_Nome"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    cellToRemove = detailTableRow.Controls["dCli_Cognome"] as XRTableCell;
                                    detailTable.DeleteColumn(cellToRemove);

                                    // cancellazione delle celle nelle tabelle di totale
                                    cellToRemove = totalDayTableRow.Controls["xrTableCell8"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMonthTableRow.Controls["xrTableCell13"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMasterDataTableRow.Controls["xrTableCell14"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalDayTableRow.Controls["xrTableCell17"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMonthTableRow.Controls["xrTableCell18"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMasterDataTableRow.Controls["xrTableCell19"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);
                                    cellToRemove = totalDayTableRow.Controls["xrTableCell25"] as XRTableCell;
                                    totalDayTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMonthTableRow.Controls["xrTableCell33"] as XRTableCell;
                                    totalMonthTable.DeleteColumn(cellToRemove);

                                    cellToRemove = totalMasterDataTableRow.Controls["xrTableCell34"] as XRTableCell;
                                    totalMasterDataTable.DeleteColumn(cellToRemove);
                                    #endregion
                                }
                                break;
                                
                        }
                    }
                }
            }
        }

    }
}
