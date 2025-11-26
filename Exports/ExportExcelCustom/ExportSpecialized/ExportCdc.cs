
using Common;
using Domain;
using Business.Repository;
using Business.BusinessExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Drawing;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using DevExpress.XtraSpreadsheet.Model;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using OfficeOpenXml.Style;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /// <summary>
    /// Classe utilizzata per la gestione dell'export per fatture
    /// </summary>
    public class ExportCdc : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
    {
        #region Public Properties
        /// <summary>
        /// Recupera o imposta il percorso del modello EXCEL
        /// </summary>   
        /// <value>
        /// Percorso modello EXCEL su disco.
        /// </value>
        public string ExcelModelFilePath { get; set; }

        /// <summary>
        /// Recupera o imposta il periodo (mese/anno) di riferimento dell'export.
        /// </summary>
        /// <value>
        /// Periodo (mese/anno) di riferimento dell'export
        /// </value>
        public DateTime ExportPeriod { get; set; }

        public int rowIndex = 1;

        public int columnIndex = 1;

        private OfficeOpenXml.Style.ExcelBorderStyle borderStyle = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        private OfficeOpenXml.Style.ExcelFillStyle fillStyle = OfficeOpenXml.Style.ExcelFillStyle.Solid;

        private Color borderColor = Color.Black;

        #endregion

        #region Public Methods

        /// <summary>
        /// Esegue la preparazione dell'export con i dati passati come parametro
        /// </summary>
        /// <param name="entitiesToExport">Elenco delle entità da esportare</param>
        public override void LaunchExport(IQueryable<Reg_V> entitiesToExport)
        {
            //List<CentroDiCosto> centro = RepoManager.CentroDiCostoRepo.GetAllQueryable(c => c.Descrizione == "PULIZIE CIVILI").ToList();
            List<Reg_V> regVsPregis = RepoManager.Reg_VRepo.GetAllQueryable(r => r.CentroDiCosto_Id == 4 && r.Qualifica_Col != "0" && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 10)).ToList();
            var exportRegVsPregis = regVsPregis.GroupBy(c => c.Cant_Id);

            DateTime minDate = CommonService.GetFirstMonthDay(ExportPeriod);
            DateTime monthLastDate = CommonService.GetLastMonthDay(minDate);
            DateTime maxDate = new DateTime(monthLastDate.Year, monthLastDate.Month, monthLastDate.Day, 23, 59, 59);
            ExcelWorkbookGenerateNew(ExcelModelFilePath);
            List<DateTime> monthDays = CommonService.GetDatesFromPeriod(CommonService.GetFirstMonthDay(ExportPeriod), CommonService.GetLastMonthDay(ExportPeriod));
            rowIndex = 2;
            foreach (var regs in exportRegVsPregis)
            {
                List<Cant> currentCant = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regs.Key).ToList();

                CellInsertValue(1, 1, rowIndex, currentCant.First().Descrizione_Can + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 1, rowIndex, 1, rowIndex, 11);
                RangeSetWrapText(1, 1, rowIndex, 1, rowIndex, true);

                CellInsertValue(1, 2, rowIndex, "PREGIS", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 2, rowIndex, 2, rowIndex, 11);
                RangeSetWrapText(1, 2, rowIndex, 2, rowIndex, true);
                int interventi = 0;
                TimeSpan totaleMensile = new TimeSpan();
                foreach (DateTime day in monthDays)
                {
                    //vado a fare il ciclo per ogni giorno e recupero le timbrature solo della giornata corrente
                    DateTime tomorrow = day.AddDays(1);
                    var dayReg = regs.Where(r => r.Data_Reg.Value.Year == day.Year && r.Data_Reg.Value.Month == day.Month && r.Data_Reg.Value.Day == day.Day).ToList();
                    int daySum = 0;
                    string tot = "-- --";
                    foreach (Reg_V reg in dayReg)
                    {
                        if (reg.Durata_Fig != null)
                        {
                            daySum += reg.Durata_Fig.Value;
                        }
                    }
                    if (daySum > 0)
                    {
                        interventi++;
                        TimeSpan totalDuration = TimeSpan.FromMinutes(daySum);
                        totaleMensile = totaleMensile + totalDuration;
                        tot = String.Format("{0}.{1}", (totalDuration.Days * 24) + totalDuration.Hours, Math.Abs(totalDuration.Minutes).ToString("00"));
                    }
                    if (CommonService.GetLastMonthDay(ExportPeriod) == day)
                    {
                        tot = String.Format("{0}.{1}", (totaleMensile.Days * 24) + totaleMensile.Hours, Math.Abs(totaleMensile.Minutes).ToString("00"));
                        RangeSetBorders(1, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(1, 3, rowIndex, 3, rowIndex, 11);

                        RangeSetWrapText(1, 3, rowIndex, 3, rowIndex, true);
                        CellInsertValue(1, 3, rowIndex, interventi, ExcelInsertTypeEnum.Content);

                        RangeSetBorders(1, 4, rowIndex, 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(1, 4, rowIndex, 4, rowIndex, 11);

                        RangeSetWrapText(1, 4, rowIndex, 4, rowIndex, true);
                        CellInsertValue(1, 4, rowIndex, tot, ExcelInsertTypeEnum.Content);
                    }
                }
                rowIndex++;
            }
            List<Reg_V> regVsGardascuole = RepoManager.Reg_VRepo.GetAllQueryable(r => r.CentroDiCosto_Id == 3 && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 10)).ToList();
            var exportRegVsGardascuole = regVsGardascuole.GroupBy(c => c.Cant_Id);
            foreach (var regs in exportRegVsGardascuole)
            {
                if (regs.Key != null) 
                {
                    List<Cant> currentCant = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regs.Key).ToList();

                    CellInsertValue(1, 1, rowIndex, currentCant.First().Descrizione_Can + " ", ExcelInsertTypeEnum.Content);
                    RangeSetBorders(1, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(1, 1, rowIndex, 1, rowIndex, 11);
                    RangeSetWrapText(1, 1, rowIndex, 1, rowIndex, true);

                    CellInsertValue(1, 2, rowIndex, "GARDASCUOLE", ExcelInsertTypeEnum.Content);
                    RangeSetBorders(1, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(1, 2, rowIndex, 2, rowIndex, 11);
                    RangeSetWrapText(1, 2, rowIndex, 2, rowIndex, true);
                    int interventi = 0;
                    TimeSpan totaleMensile = new TimeSpan();
                    foreach (DateTime day in monthDays)
                    {
                        //vado a fare il ciclo per ogni giorno e recupero le timbrature solo della giornata corrente
                        DateTime tomorrow = day.AddDays(1);
                        var dayReg = regs.Where(r => r.Data_Reg.Value.Year == day.Year && r.Data_Reg.Value.Month == day.Month && r.Data_Reg.Value.Day == day.Day).ToList();
                        int daySum = 0;
                        string tot = "-- --";
                        foreach (Reg_V reg in dayReg)
                        {
                            if (reg.Durata_Fig != null)
                            {
                                daySum += reg.Durata_Fig.Value;
                            }
                        }
                        if (daySum > 0)
                        {
                            interventi++;
                            TimeSpan totalDuration = TimeSpan.FromMinutes(daySum);
                            totaleMensile = totaleMensile + totalDuration;
                            tot = String.Format("{0}.{1}", (totalDuration.Days * 24) + totalDuration.Hours, Math.Abs(totalDuration.Minutes).ToString("00"));
                        }
                        if (CommonService.GetLastMonthDay(ExportPeriod) == day)
                        {
                            tot = String.Format("{0}.{1}", (totaleMensile.Days * 24) + totaleMensile.Hours, Math.Abs(totaleMensile.Minutes).ToString("00"));
                            RangeSetBorders(1, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            RangeSetFontSize(1, 3, rowIndex, 3, rowIndex, 11);

                            RangeSetWrapText(1, 3, rowIndex, 3, rowIndex, true);
                            CellInsertValue(1, 3, rowIndex, interventi, ExcelInsertTypeEnum.Content);

                            RangeSetBorders(1, 4, rowIndex, 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            RangeSetFontSize(1, 4, rowIndex, 4, rowIndex, 11);

                            RangeSetWrapText(1, 4, rowIndex, 4, rowIndex, true);
                            CellInsertValue(1, 4, rowIndex, tot, ExcelInsertTypeEnum.Content);
                        }
                    }
                    rowIndex++;
                }
                
            }
            List<Reg_V> regVsMarketing = RepoManager.Reg_VRepo.GetAllQueryable(r => r.CentroDiCosto_Id == 5 && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 10)).ToList();
            var exportRegVsMarketing = regVsMarketing.GroupBy(c => c.Cant_Id);
            foreach (var regs in exportRegVsMarketing)
            {
                List<Cant> currentCant = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regs.Key).ToList();

                CellInsertValue(1, 1, rowIndex, currentCant.First().Descrizione_Can + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 1, rowIndex, 1, rowIndex, 11);
                RangeSetWrapText(1, 1, rowIndex, 1, rowIndex, true);

                CellInsertValue(1, 2, rowIndex, "TRENTINO MARKETING", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 2, rowIndex, 2, rowIndex, 11);
                RangeSetWrapText(1, 2, rowIndex, 2, rowIndex, true);
                int interventi = 0;
                TimeSpan totaleMensile = new TimeSpan();
                foreach (DateTime day in monthDays)
                {
                    //vado a fare il ciclo per ogni giorno e recupero le timbrature solo della giornata corrente
                    DateTime tomorrow = day.AddDays(1);
                    var dayReg = regs.Where(r => r.Data_Reg.Value.Year == day.Year && r.Data_Reg.Value.Month == day.Month && r.Data_Reg.Value.Day == day.Day).ToList();
                    int daySum = 0;
                    string tot = "-- --";
                    foreach (Reg_V reg in dayReg)
                    {
                        if (reg.Durata_Fig != null)
                        {
                            daySum += reg.Durata_Fig.Value;
                        }
                    }
                    if (daySum > 0)
                    {
                        interventi++;
                        TimeSpan totalDuration = TimeSpan.FromMinutes(daySum);
                        totaleMensile = totaleMensile + totalDuration;
                        tot = String.Format("{0}.{1}", (totalDuration.Days * 24) + totalDuration.Hours, Math.Abs(totalDuration.Minutes).ToString("00"));
                    }
                    if (CommonService.GetLastMonthDay(ExportPeriod) == day)
                    {
                        tot = String.Format("{0}.{1}", (totaleMensile.Days * 24) + totaleMensile.Hours, Math.Abs(totaleMensile.Minutes).ToString("00"));
                        RangeSetBorders(1, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(1, 3, rowIndex, 3, rowIndex, 11);

                        RangeSetWrapText(1, 3, rowIndex, 3, rowIndex, true);
                        CellInsertValue(1, 3, rowIndex, interventi, ExcelInsertTypeEnum.Content);

                        RangeSetBorders(1, 4, rowIndex, 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(1, 4, rowIndex, 4, rowIndex, 11);

                        RangeSetWrapText(1, 4, rowIndex, 4, rowIndex, true);
                        CellInsertValue(1, 4, rowIndex, tot, ExcelInsertTypeEnum.Content);
                    }
                }
                rowIndex++;
            }
            List<Reg_V> regVsMarr = RepoManager.Reg_VRepo.GetAllQueryable(r => r.CentroDiCosto_Id == 11 && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 10)).ToList();
            var exportRegVsMarr = regVsMarr.GroupBy(c => c.Cant_Id);
            foreach (var regs in exportRegVsMarr)
            {
                List<Cant> currentCant = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regs.Key).ToList();

                CellInsertValue(1, 1, rowIndex, currentCant.First().Descrizione_Can + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 1, rowIndex, 1, rowIndex, 11);
                RangeSetWrapText(1, 1, rowIndex, 1, rowIndex, true);

                CellInsertValue(1, 2, rowIndex, "MARR", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 2, rowIndex, 2, rowIndex, 11);
                RangeSetWrapText(1, 2, rowIndex, 2, rowIndex, true);
                int interventi = 0;
                TimeSpan totaleMensile = new TimeSpan();
                foreach (DateTime day in monthDays)
                {
                    //vado a fare il ciclo per ogni giorno e recupero le timbrature solo della giornata corrente
                    DateTime tomorrow = day.AddDays(1);
                    var dayReg = regs.Where(r => r.Data_Reg.Value.Year == day.Year && r.Data_Reg.Value.Month == day.Month && r.Data_Reg.Value.Day == day.Day).ToList();
                    int daySum = 0;
                    string tot = "-- --";
                    foreach (Reg_V reg in dayReg)
                    {
                        if (reg.Durata_Fig != null)
                        {
                            daySum += reg.Durata_Fig.Value;
                        }
                    }
                    if (daySum > 0)
                    {
                        interventi++;
                        TimeSpan totalDuration = TimeSpan.FromMinutes(daySum);
                        totaleMensile = totaleMensile + totalDuration;
                        tot = String.Format("{0}.{1}", (totalDuration.Days * 24) + totalDuration.Hours, Math.Abs(totalDuration.Minutes).ToString("00"));
                    }
                    if (CommonService.GetLastMonthDay(ExportPeriod) == day)
                    {
                        tot = String.Format("{0}.{1}", (totaleMensile.Days * 24) + totaleMensile.Hours, Math.Abs(totaleMensile.Minutes).ToString("00"));
                        RangeSetBorders(1, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(1, 3, rowIndex, 3, rowIndex, 11);

                        RangeSetWrapText(1, 3, rowIndex, 3, rowIndex, true);
                        CellInsertValue(1, 3, rowIndex, interventi, ExcelInsertTypeEnum.Content);

                        RangeSetBorders(1, 4, rowIndex, 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(1, 4, rowIndex, 4, rowIndex, 11);

                        RangeSetWrapText(1, 4, rowIndex, 4, rowIndex, true);
                        CellInsertValue(1, 4, rowIndex, tot, ExcelInsertTypeEnum.Content);
                    }
                }
                rowIndex++;
            }

            ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

            ExcelWorkbookDispose();
        }

        private void WriteTimesheetColHeader()
        {
            List<DateTime> days = CommonService.GetDatesFromPeriod(ExportPeriod, ExportPeriod.AddMonths(1).AddDays(-1)); //Calcolo i giorni per l'header

            RangeSetFontBold(1, columnIndex + 1, rowIndex, days.Count + 3, rowIndex);

            days.ForEach(day =>
            {
                //WriteHeaderDayCell(day);

                WriteHeaderDayCellDay(day);

                rowIndex++;
            });

            WriteHeaderTotalCell();

            rowIndex++;

            WriteHeaderTotalDaysCell();

            rowIndex++;

            //WriteHeaderTotalFest();

            //columnIndex++;

            //WriteHeaderTotalFer();

            columnIndex = 2;

            rowIndex++;
        }

        private void WriteHeaderDayCellDay(DateTime date)
        {
            String giorno = "";
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    giorno = "L " + date.Day;
                    break;
                case DayOfWeek.Tuesday:
                    giorno = "MA " + date.Day;
                    break;
                case DayOfWeek.Wednesday:
                    giorno = "ME " + date.Day;
                    break;
                case DayOfWeek.Thursday:
                    giorno = "G " + date.Day;
                    break;
                case DayOfWeek.Friday:
                    giorno = "V " + date.Day;
                    break;
                case DayOfWeek.Saturday:
                    giorno = "S " + date.Day;
                    break;
                case DayOfWeek.Sunday:
                    giorno = "D " + date.Day;
                    break;
            }

            CellInsertValue(1, rowIndex + 1, columnIndex, giorno, ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, "0");
            //RangeSetBackgroundColor(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day, Color.LightGray, fillStyle);
            RangeSetFontSize(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, 12);
            RangeSetWrapText(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, true);
            ColumnsSetWidth(1, rowIndex + 1, rowIndex + 1, 8);

            RangeSetFontBold(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1);
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex, Color.Red);
            }
        }

        private void WriteHeaderTotalCell()
        {

            CellInsertValue(1, rowIndex + 1, columnIndex, "Tot", ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex);

        }


        private void WriteHeaderDayCell(DateTime date)
        {
            CellInsertValue(1, rowIndex, columnIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex, columnIndex, rowIndex, date.Day, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(1, rowIndex, columnIndex, rowIndex, date.Day, "0");
            RangeSetBackgroundColor(1, rowIndex, columnIndex, rowIndex, date.Day, Color.LightGray, fillStyle);
            RangeSetWrapText(1, rowIndex, columnIndex, rowIndex, date.Day, true);
            ColumnsSetWidth(1, rowIndex, rowIndex, 6);
            RangeSetFontBold(1, rowIndex, columnIndex, rowIndex, date.Day);

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(1, rowIndex, columnIndex, rowIndex, columnIndex, Color.Red);
            }
        }

        private void WriteHeaderTotalDaysCell()
        {
            CellInsertValue(1, rowIndex + 1, columnIndex, "Tot.G", ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);

        }
        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="selectedColIds">L'elenco degli id collaboratore selezionati per l'export.</param>
        /// <param name="selectedCantIds">L'elenco degli id cantiere selezionati per l'export.</param>
        /// <param name="selectedCliIds">L'elenco degli id cliente selezionati per l'export.</param>
        public override void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds, IEnumerable<int> selectedCliIds)
        {
            //    if (selectedCliIds.Any())
            //    {
            //        DateTime firstMonthDate = CommonService.GetFirstMonthDay(ExportPeriod);
            //        DateTime lastMonthDate = CommonService.GetLastMonthDay(ExportPeriod);
            //        Col col;

            //        ExcelWorkbookGenerateNew(ExcelModelFilePath);

            //        foreach (int cliId in selectedCliIds)
            //        {
            //            foreach (int colid in selectedColIds)
            //            {
            //                IEnumerable<Reg_V> colRegVs = GetProcessableColRegVs(colid, firstMonthDate, lastMonthDate); // modificare con ID collaboratore (where the fuck i can find it?)

            //                if (colRegVs.Any())
            //                {
            //                    col = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colid);

            //                }
            //            }
            //        }
            //    }
            throw new NotImplementedException();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Recupera tutte le registrazioni processabili del collaboratore X per il periodo specificato.
        /// </summary>
        /// <param name="colId">L'identificativo del collaboratore per cui effettuare la ricerca.</param>
        /// <param name="startPeriod">La data di inizio del periodo in cui effettuare la ricerca.</param>
        /// <param name="endPeriod">La data di fine del periodo in cui effettuare la ricerca.</param>
        /// <returns>L'elenco delle registrazioni da processare per il collaboratore e il periodo scelto.</returns>
        private IEnumerable<Reg_V> GetProcessableColRegVs(int colId, DateTime startPeriod, DateTime endPeriod)
        {
            return RepoManager.Reg_VRepo.Find(regv => regv.Col_Id == colId && regv.Data_Reg >= startPeriod && regv.Data_Reg <= endPeriod);
        }

        #endregion
    }
}
