
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
    public class ExportManutentori : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
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
            DateTime minDate = CommonService.GetFirstMonthDay(ExportPeriod);
            DateTime monthLastDate = CommonService.GetLastMonthDay(minDate);
            DateTime maxDate = new DateTime(monthLastDate.Year, monthLastDate.Month, monthLastDate.Day, 23, 59, 59);
            ExcelWorkbookGenerateNew(ExcelModelFilePath);
            List<Reg_V> regVs = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Data_Reg >= minDate && r.Data_Reg <= maxDate && r.Qualifica_Col == "0" && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2)).ToList();
            //List<Reg_V> regVs = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Data_Reg > minDate && r.Data_Reg < maxDate && r.Qualifica_Col == "0").ToList();
            var exportRegVs = regVs.GroupBy(c => c.Cant_Id);
            List<DateTime> monthDays = CommonService.GetDatesFromPeriod(CommonService.GetFirstMonthDay(ExportPeriod), CommonService.GetLastMonthDay(ExportPeriod)); 
            //ordino le ore in base alla ora della registrazione e le reggruppo per i cantieri
            rowIndex = 2;
            List<Cant> cantieri = RepoManager.CantRepo.GetAllQueryable(c => c.Tipologia_Can == "ATT").ToList();
            //creo un dictionary per immagazzinare le ore, la prima key sara la descrizione del cantiere, la seconda l'attivita e l'intero il totale delle ore
            List<Dictionary<string, Dictionary<string, int>>> listaAttivita = new List<Dictionary<string, Dictionary<string, int>>>();
            List<Dictionary<string, Dictionary<string, int>>> tmpAttivita = new List<Dictionary<string, Dictionary<string, int>>>();
            string lastAtt = "";
            string lastCant = "";
            int lastDurata = 0;
            //vado a fare un foreach per ogni cantiere
            foreach (var reg in regVs.OrderBy(r => r.Col_Id).ThenBy(r => r.Data_Ora_Fis_E))
            { 
                List<Cant> currentCant = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                //controllo se la timbratura è un attività
                if (reg.Registrazione_Tipo_Reg == 2)
                {
                    //recupero la lista delle attività
                    List<Cant> attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                    //controllo se la timbratura è associata o meno
                    if (reg.Registrazione_Stato_Reg == 1)
                    {
                        //inizializzo la variabile per controllare se ho aggiornato la lista oppure devo creare una nuova tupla
                        bool aggiornato = false;
                        //ciclo tutte le attività che ho recuperato in precedenza
                        if (listaAttivita.Count() > 0)
                        {
                            bool esiste = false;
                            Dictionary<string, int> tmpDic = new Dictionary<string, int>();
                            foreach (var att in listaAttivita)
                            {
                                if (att.First().Key == lastCant)
                                {
                                    if (!esiste) {
                                        //inizializzo un dictionary temporaneo contenente come chiave attivita e valore le ore
                                        tmpDic = att.First().Value;
                                        foreach (var lista in tmpDic)
                                        {
                                            if (lista.Key == attivita.First().Descrizione_Can)
                                            {
                                                esiste = true;
                                            }
                                        }
                                    }  
                                }
                            }
                            if (esiste)
                            {
                                //recupero il totale delle ore lavorate su quel cantiere facendo una determinata attivita
                                int tmp = tmpDic[attivita.First().Descrizione_Can];
                                //incremento il totale delle ore mensili
                                tmpDic[attivita.First().Descrizione_Can] = tmp + lastDurata;
                                //azzero tute le variabili
                                lastAtt = "";
                                lastDurata = 0;
                                aggiornato = true;
                            }
                            else
                            {
                                if (lastDurata > 0) {
                                    Dictionary<string, Dictionary<string, int>> tmpDi = new Dictionary<string, Dictionary<string, int>>();
                                    tmpDi[lastCant] = new Dictionary<string, int>() { { attivita.First().Descrizione_Can, lastDurata } };
                                    tmpAttivita.Add(tmpDi);
                                }   
                                aggiornato = true;
                                lastAtt = "";
                                lastDurata = 0;
                            }
                            if (!aggiornato) {
                                if (lastDurata > 0) {
                                    var tmpdic = new Dictionary<string, Dictionary<string, int>>();
                                    tmpdic[lastCant] = new Dictionary<string, int>() { { attivita.First().Descrizione_Can, lastDurata } };
                                    tmpAttivita.Add(tmpdic);
                                }
                                lastAtt = "";
                            }
                        }
                        else {
                            if (lastDurata > 0) {
                                Dictionary<string, Dictionary<string, int>> tmpDic = new Dictionary<string, Dictionary<string, int>>();
                                tmpDic[lastCant] = new Dictionary<string, int>() { { attivita.First().Descrizione_Can, lastDurata } };
                                tmpAttivita.Add(tmpDic);
                            }
                            lastDurata = 0;
                            lastAtt = "";
                        }
                        
                    }
                    else {
                        if (lastAtt != "") {
                            attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Descrizione_Can == lastAtt).ToList();
                            //inizializzo la variabile per controllare se ho aggiornato la lista oppure devo creare una nuova tupla
                            bool aggiornato = false;
                            bool esiste = false;
                            Dictionary<string, int> tmpDic = new Dictionary<string, int>();
                            foreach (var att in listaAttivita)
                            {
                                if (att.First().Key == lastCant)
                                {
                                    if (!esiste)
                                    {
                                        //inizializzo un dictionary temporaneo contenente come chiave attivita e valore le ore
                                        tmpDic = att.First().Value;
                                        foreach (var lista in tmpDic)
                                        {
                                            if (lista.Key == attivita.First().Descrizione_Can)
                                            {
                                                esiste = true;
                                            }
                                        }
                                    }
                                }
                            }
                            if (esiste)
                            {
                                //recupero il totale delle ore lavorate su quel cantiere facendo una determinata attivita
                                int tmp = tmpDic[attivita.First().Descrizione_Can];
                                //incremento il totale delle ore mensili
                                tmpDic[attivita.First().Descrizione_Can] = tmp + lastDurata;
                                //azzero tute le variabili
                                lastAtt = "";
                                lastDurata = 0;
                                aggiornato = true;
                            }
                            else
                            {
                                if (lastDurata > 0) {
                                    Dictionary<string, Dictionary<string, int>> tmpDi = new Dictionary<string, Dictionary<string, int>>();
                                    tmpDi[lastCant] = new Dictionary<string, int>() { { attivita.First().Descrizione_Can, lastDurata } };
                                    tmpAttivita.Add(tmpDi);
                                }
                                aggiornato = true;
                                lastAtt = "";
                                lastDurata = 0;
                            }
                            if (!aggiornato)
                            {
                                if (lastDurata > 0) {
                                    Dictionary<string, Dictionary<string, int>> tmpdic = new Dictionary<string, Dictionary<string, int>>();
                                    tmpdic[currentCant.First().Descrizione_Can] = new Dictionary<string, int>() { { attivita.First().Descrizione_Can, lastDurata } };
                                    tmpAttivita.Add(tmpdic);
                                }
                                lastAtt = "";
                                lastDurata = 0;
                            }
                        }
                        attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                        lastAtt = attivita.First().Descrizione_Can;
                    }
                } else if (reg.Registrazione_Tipo_Reg == 0) {
                    if (reg.Durata_Fig != null) {
                        lastDurata = reg.Durata_Fig.Value;
                        lastCant = reg.Cant_Desc;
                    }
                }
                listaAttivita = tmpAttivita;
                //List<Cant> currentCant = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regs.Key).ToList();
                //string lastAtt = "";
                //int lastDurata = 0;
                //foreach (Reg_V reg in regs)
                //{
                //    if (reg.Registrazione_Tipo_Reg == 2) {
                //        List<Cant> attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                //        if (reg.Registrazione_Stato_Reg == 1)
                //        {
                //            foreach (var att in listaAttivita) {
                //                if (att.First().Key == attivita.First().Descrizione_Can) {
                //                    int tmp = att[attivita.First().Descrizione_Can];
                //                    att[attivita.First().Descrizione_Can] = tmp + lastDurata;
                //                    lastAtt = "";
                //                    lastDurata = 0;
                //                }
                //            }
                //        }
                //        else {
                //            if (lastAtt != "") {
                //                attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Descrizione_Can == lastAtt).ToList();
                //                foreach (var att in listaAttivita)
                //                {
                //                    if (att.First().Key == attivita.First().Descrizione_Can)
                //                    {
                //                        int tmp = att[attivita.First().Descrizione_Can];
                //                        att[attivita.First().Descrizione_Can] = tmp + lastDurata;
                //                        lastAtt = "";
                //                        lastDurata = 0;
                //                    }
                //                }
                //            }
                //            attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                //            lastAtt = attivita.First().Descrizione_Can;
                //        }
                //    } else if (reg.Registrazione_Tipo_Reg == 0) {
                //        if (reg.Durata_Fig != null) {
                //            lastDurata = reg.Durata_Fig.Value;
                //        }
                //    }
                //    CellInsertValue(1, 1, rowIndex, currentCant.First().Descrizione_Can + " ", ExcelInsertTypeEnum.Content);
                //    RangeSetBorders(1, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                //    RangeSetFontSize(1, 1, rowIndex, 1, rowIndex, 11);
                //    RangeSetWrapText(1, 1, rowIndex, 1, rowIndex, true);
                //}

                //CellInsertValue(1, 1, rowIndex, currentCant.First().Descrizione_Can + " ", ExcelInsertTypeEnum.Content);
                //RangeSetBorders(1, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                //RangeSetFontSize(1, 1, rowIndex, 1, rowIndex, 11);
                //RangeSetWrapText(1, 1, rowIndex, 1, rowIndex, true);
                //
                //CellInsertValue(1, 2, rowIndex, "PULIZIE CIVILI", ExcelInsertTypeEnum.Content);
                //RangeSetBorders(1, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                //RangeSetFontSize(1, 2, rowIndex, 2, rowIndex, 11);
                //RangeSetWrapText(1, 2, rowIndex, 2, rowIndex, true);

                //TimeSpan totaleMensile = new TimeSpan();
                //foreach (DateTime day in monthDays)
                //{
                //    //vado a fare il ciclo per ogni giorno e recupero le timbrature solo della giornata corrente
                //    DateTime tomorrow = day.AddDays(1);
                //    var dayReg = regs.Where(r => r.Data_Ora_Fis_E > day && r.Data_Ora_Fis_U < tomorrow).ToList();
                //    int daySum = 0;
                //    string tot = "-- --";
                //    foreach (Reg_V reg in dayReg)
                //    {
                //        if (reg.Durata_Fig != null)
                //        {
                //            daySum += reg.Durata_Fig.Value;
                //        }
                //    }
                //    if (daySum > 0)
                //    {
                //        TimeSpan totalDuration = TimeSpan.FromMinutes(daySum);
                //        totaleMensile = totaleMensile + totalDuration;
                //        tot = String.Format("{0}.{1}", (totalDuration.Days * 24) + totalDuration.Hours, Math.Abs(totalDuration.Minutes).ToString("00"));
                //    }
                //    if (CommonService.GetLastMonthDay(ExportPeriod) == day)
                //    {
                //        tot = String.Format("{0}.{1}", (totaleMensile.Days * 24) + totaleMensile.Hours, Math.Abs(totaleMensile.Minutes).ToString("00"));
                //
                //        RangeSetBorders(1, 4, rowIndex, 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                //        RangeSetFontSize(1, 4, rowIndex, 4, rowIndex, 11);
                //
                //        RangeSetWrapText(1, 4, rowIndex, 4, rowIndex, true);
                //        CellInsertValue(1, 4, rowIndex, tot, ExcelInsertTypeEnum.Content);
                //    }
                //}
                //rowIndex++;
            }
            foreach (var prova in listaAttivita.OrderBy(p => p.First().Key)) {
                CellInsertValue(1, 1, rowIndex, prova.First().Key + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 1, rowIndex, 1, rowIndex, 11);
                RangeSetWrapText(1, 1, rowIndex, 1, rowIndex, true);

                foreach (var inner in prova.First().Value) {
                    List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Descrizione_Can == inner.Key).ToList();

                    CellInsertValue(1, 2, rowIndex, cants.First().Note_Can + " ", ExcelInsertTypeEnum.Content);
                    RangeSetBorders(1, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(1, 2, rowIndex, 2, rowIndex, 11);
                    RangeSetWrapText(1, 2, rowIndex, 2, rowIndex, true);

                    CellInsertValue(1, 3, rowIndex, inner.Key + " ", ExcelInsertTypeEnum.Content);
                    RangeSetBorders(1, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(1, 3, rowIndex, 3, rowIndex, 11);
                    RangeSetWrapText(1, 3, rowIndex, 3, rowIndex, true);

                    TimeSpan durata = TimeSpan.FromMinutes(inner.Value);
                    string totale = String.Format("{0}.{1}", (durata.Days * 24) + durata.Hours, Math.Abs(durata.Minutes).ToString("00"));
                    CellInsertValue(1, 4, rowIndex, totale + " ", ExcelInsertTypeEnum.Content);
                    RangeSetBorders(1, 4, rowIndex, 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(1, 4, rowIndex, 4, rowIndex, 11);
                    RangeSetWrapText(1, 4, rowIndex, 4, rowIndex, true);
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
