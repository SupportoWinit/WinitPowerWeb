using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Business.Repository;
using System.Web;
using System.IO;
using Business.BusinessExtension;
using Business;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /// <summary>
    /// Classe utilizzata per la gestione dell'export del rapporto ore
    /// </summary>
    public class ExportImporti : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
    {

        #region Enums

        /// <summary>
        /// Identifica i tipi di totale durata calcolabili all'interno dell'export
        /// </summary>
        private enum TotalTypeEnum
        {

            /// <summary>
            /// Totale delle ore senza motivazione
            /// </summary>
            TotalWithoutJustification,

            /// <summary>
            /// Totale delle ore con motivazione
            /// </summary>
            TotalWithJustification

        }

        #endregion

        #region Constants

        /// <summary>
        /// Il nome del worksheet modello da utilizzare nella generazione del foglio excel
        /// </summary>
        private const string ModelWorksheetName = "Model";

        /// <summary>
        /// La prima riga del worksheet scrivibile con i dati dei giorni
        /// </summary>
        private const int FirstDayIndex = 10;

        #endregion

        #region Public Properties

        /// <summary>
        /// Recupera o imposta lo specifico calcolo da utilizzare (figurative/fisiche); utilizato solo se <see cref="UseCalculationType" /> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Lo specifico calcolo da utilizzare (figurative/fisiche); utilizato solo se <see cref="UseCalculationType" /> è valorizzato a <c>true</c>.
        /// </value>
        public ExportRegVCalculationTypeEnum CalculationType { get; set; }

        /// <summary>
        /// Recupera o imposta il valore in minuti della tolleranza sulla durata utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseDurationTollerance" /> è valorizzato
        /// a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il valore in minuti della tolleranza sulla durata utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseDurationTollerance" /> è valorizzato a <c>true</c>.
        /// </value>
        public int DurationTollerance { get; set; }

        /// <summary>
        /// Recupera o imposta il valore in minuti della tolleranza sull'entrata/uscita utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseEUTollerance" /> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il valore in minuti della tolleranza sull'entrata/uscita utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseEUTollerance" /> è valorizzato a <c>true</c>.
        /// </value>
        public int EUTollerance { get; set; }

        /// <summary>
        /// Recupera o imposta un valore ch indica se utilizzare oppure no l'export del confronto ore budget dettagliato
        /// </summary>
        /// <value>
        /// <c>true</c> se si deve utilizzare oppure no l'export dettagliato; altrimenti, <c>false</c>.
        /// </value>
        public bool UseExportDetail { get; set; }

        /// <summary>
        /// Recupera o imposta il percorso del modello excel su disco.
        /// </summary>
        /// <value>
        /// Il percorso del modello excel su disco.
        /// </value>
        public string ExcelModelFilePath { get; set; }

        /// <summary>
        /// Recupera o imposta il periodo (mese/anno) di riferimento dell'export.
        /// </summary>
        /// <value>
        /// Il periodo (mese/anno) di riferimento dell'export.
        /// </value>
        public DateTime ExportPeriod { get; set; }

        /// <summary>
        /// Recupera o imposta il tipo di calcolo specifico delle ore (solo durata/con entrata uscita); utilizzato solo se <see cref="UseHourType" /> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il tipo di calcolo specifico delle ore (solo durata/con entrata uscita); utilizzato solo se <see cref="UseHourType" /> è valorizzato a <c>true</c>.
        /// </value>
        public ExportRegVHourTypeEnum HourType { get; set; }

        /// <summary>
        /// Recupera o imposta la stringa che rappresenta l'entità di primo riferimento per selezione del modello excel.
        /// </summary>
        /// <value>
        /// La stringa che rappresenta l'entità di primo riferimento per la selezione del modello excel.
        /// </value>
        public ExcelModelSelectionTypeEnum ModelFirstEntity { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che indica se è necessario impostare uno specifico calcolo (figurative/fisiche).
        /// </summary>
        /// <value>
        /// <c>true</c> se è necessario impostare uno specifico calcolo (figurative/fisiche); altrimenti, <c>false</c>.
        /// </value>
        public bool UseCalculationType { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che determina se utilizzare o meno la tolleranza della durata registrazione in fase di elaborazione.
        /// </summary>
        /// <value>
        /// <c>true</c> se si utilizzerà la tolleranza della durata registrazione in fase di elaborazione; altrimenti, <c>false</c>.
        /// </value>
        public bool UseDurationTollerance { get; set; }

        /// <summary>
        /// Recupera o imposta un valore ch indica quando utilizzare in fase di elaborazione la tolleranza sui valori di entrata/uscita
        /// </summary>
        /// <value>
        /// <c>true</c> se si deve utilizzare in fase di elaborazione la tolleranza sui valori di entrata/uscita; altrimenti, <c>false</c>.
        /// </value>
        public bool UseEUTollerance { get; set; }


        /// <summary>
        /// Recupera o imposta il valore che indica se è necessario utilizzare un calcolo specifico di ore (solo durata/con entrata uscita).
        /// </summary>
        /// <value>
        /// <c>true</c> se è necessario utilizzare un calcolo specifico di ore (solo durata/con entrata uscita); altrimenti, <c>false</c>.
        /// </value>
        public bool UseHourType { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="entitiesToExport">L'elenco delle entità da esportare</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void LaunchExport(IQueryable<Reg_V> entitiesToExport)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="selectedColIds">L'elenco degli id collaboratore selezionati per l'export.</param>
        /// <param name="selectedCantIds">L'elenco degli id cantiere selezionati per l'export.</param>
        public override void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds)
        {
            // si procede con l'elaborazione solamente se sono stati selezionati dei collaboratori
            if (selectedColIds.Any())
            {
                // calcolo delle date presenti nel mese periodo in d'elaborazione
                DateTime firstPeriodDate = CommonService.GetFirstMonthDay(ExportPeriod);
                DateTime lastPeriodDate = CommonService.GetLastMonthDay(ExportPeriod);
                List<DateTime> periodDates;
                List<TimesheetModuleItem> currentMonthTimesheets, lastMonthTimesheets, nextMonthTimesheets;
                TimesheetModuleItem colPlan, lastMonthPlan, nextMonthPlan, currentMonthDiurno, currentMonthNotturno;
                Col col;
                bool isFromFreeTimesheet;
                int freeTimesheetId,
                    dayIndex;
                double importo,
                       totalMonthImporto,
                       totalWeekImporto,
                       durata1Hours,
                       durata2Hours;
                TimeSpan durata1,
                         durata2,
                         totalMonthDurata1,
                         totalMonthDurata2,
                         totalWeekDurata1,
                         totalWeekDurata2;

                // calcolo del nome dell'azienda che sta effettuando l'elaborazione
                string companyName = RepoManager.ParamRepo.ParametersRow.CompanyName;

                // generazione dell'excel su cui lavorare a partire dal percorso del modello
                ExcelWorkbookGenerateNew(ExcelModelFilePath);

                // per ogni collaboratore da processare
                foreach (int colId in selectedColIds)
                {
                    // è recuperato l'elenco delle registrazioni processabili
                    IEnumerable<Reg_V> colRegVs = GetProcessableColRegVs(colId, firstPeriodDate, lastPeriodDate);

                    // si procede all'elaborazione del collaboratore solamente se sono presenti delle registrazioni
                    if (colRegVs.Any())
                    {
                        lastMonthTimesheets = nextMonthTimesheets = null;

                        col = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colId);

                        //Se il collaboratore ha associato un piano, lo recupero, altrimenti lo tengo a null
                        colPlan = lastMonthPlan = nextMonthPlan = default(TimesheetModuleItem);
                        if (col.Tab_Orari_Tipo_Id.HasValue)
                        {
                            colPlan = TimesheetModuleItem.GenerateNewPlanTimesheet(false, RepoManager.Tab_OrariRepo.GetPlanMinutes(colId, firstPeriodDate, lastPeriodDate, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col, out isFromFreeTimesheet, out freeTimesheetId, false).First().Value, isFromFreeTimesheet, freeTimesheetId, colId, firstPeriodDate, 0);
                        }

                        //Recupera i cartellini del mese corrente divisi per giorno/notte
                        currentMonthTimesheets = TimesheetModuleItem.GenerateDayNightTimesheets(firstPeriodDate, false, colId, colPlan).ToList();

                        DateTime lastMonthEnd = firstPeriodDate.AddDays(-1);
                        DateTime nextMonthInit = lastPeriodDate.AddDays(1);
                        //Se sono richiesti i dettagli per settimana, recupero i cartellini del mese precedente e successivo per completare le settimane a cavallo
                        if (UseExportDetail)
                        {
                            //Se il primo giorno del mese NON è un lunedì, recupera i cartellini del mese precedente
                            if (firstPeriodDate.DayOfWeek != DayOfWeek.Monday)
                            {
                                DateTime lastMonthInit = firstPeriodDate.AddMonths(-1);
                                if (col.Tab_Orari_Tipo_Id.HasValue)
                                {
                                    lastMonthPlan = TimesheetModuleItem.GenerateNewPlanTimesheet(false, RepoManager.Tab_OrariRepo.GetPlanMinutes(colId, lastMonthInit, lastMonthEnd, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col, out isFromFreeTimesheet, out freeTimesheetId, false).First().Value, isFromFreeTimesheet, freeTimesheetId, colId, firstPeriodDate, 0);
                                }

                                lastMonthTimesheets = TimesheetModuleItem.GenerateDayNightTimesheets(lastMonthInit, false, colId, lastMonthPlan);
                            }

                            //Se l'ultimo giorno del mese NON è una domenica, recupera i cartellini del mese successivo 
                            if (lastPeriodDate.DayOfWeek != DayOfWeek.Sunday)
                            {
                                DateTime nextMonthEnd = lastPeriodDate.AddMonths(1);

                                if (col.Tab_Orari_Tipo_Id.HasValue)
                                {
                                    nextMonthPlan = TimesheetModuleItem.GenerateNewPlanTimesheet(false, RepoManager.Tab_OrariRepo.GetPlanMinutes(colId, nextMonthInit, nextMonthEnd, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col, out isFromFreeTimesheet, out freeTimesheetId, false).First().Value, isFromFreeTimesheet, freeTimesheetId, colId, firstPeriodDate, 0);
                                }

                                nextMonthTimesheets = TimesheetModuleItem.GenerateDayNightTimesheets(nextMonthInit, false, colId, nextMonthPlan);
                            }
                        }

                        if (currentMonthTimesheets.Count == 2)
                        {
                            currentMonthDiurno = currentMonthTimesheets.First();
                            currentMonthNotturno = currentMonthTimesheets.Last();

                            #region TESTATA
                            string colCode = col.Codice_Collaboratore,
                                   qualificaCode = col.Qualifica_Col,
                                   qualificaDesc = "",
                                   importo1label = "",
                                   importo2label = "",
                                   durata1header = "",
                                   durata2header = "";

                            double importo1 = 0d,
                                   importo2 = 0d;

                            //Imposta importi a seconda della qualifica del collaboratore
                            if (qualificaCode.Trim().Equals("0")) /* 0 => AUTISTA */
                            {
                                /* RETRIBUZIONE ORARIA => IMPORTO DIURNO || RETRIBUZIONE STRAORDINARIA => IMPORTO NOTTURNO */
                                importo1label = BusinessService.GetLocalizedString(PowerWebResources.FLD_RETRIBUZIONE_ORARIA_COL);
                                importo2label = BusinessService.GetLocalizedString(PowerWebResources.FLD_RETRIBUZIONE_STRAORDINARIA_COL);
                                importo1 = col.Retribuzione_Oraria_Col ?? 0d;
                                importo2 = col.Retribuzione_Straordinaria_Col ?? 0d;
                                durata1header = "Ore Diurne";
                                durata2header = "Ore Notturne";
                            }

                            else if (qualificaCode.Trim().Equals("1")) /* 1 => COMMESSO */
                            {
                                /* RETRIBUZIONE NETTA => IMPORTO FERIALE || RETRIBUZIONE LORDA => IMPORTO FESTIVO */
                                importo1label = BusinessService.GetLocalizedString(PowerWebResources.FLD_RETRIBUZIONE_NETTA_COL);
                                importo2label = BusinessService.GetLocalizedString(PowerWebResources.FLD_RETRIBUZIONE_LORDA_COL);
                                importo1 = col.Retribuzione_Netta_Col ?? 0d;
                                importo2 = col.Retribuzione_Lorda_Col ?? 0d;
                                durata1header = "Ore Feriali";
                                durata2header = "Ore Festive";
                            }

                            qualificaDesc = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Nome_Tab.Equals("QUALIFICHE_COL") && td.Chiave_Tab.Trim().Equals(qualificaCode)).Decodifica_Tab;

                            // viene copiato il foglio excel modello per far scrivere i dati 
                            WorksheetCopy(ModelWorksheetName, colCode);

                            // di tutti i campi fissi di testata
                            CellInsertValue(colCode, 1, 1, companyName, ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 2, 5, ExportPeriod.ToString("MMMM yyyy").ToUpper(), ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 4, 5, col.CognomeNome_Col, ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 2, 6, col.LastPruCode, ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 4, 6, qualificaDesc, ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 1, 7, importo1label, ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 3, 7, importo2label, ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 2, 7, importo1, ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 4, 7, importo2, ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 2, 9, durata1header, ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 3, 9, durata2header, ExcelInsertTypeEnum.Content);

                            #endregion
                            
                            #region GIORNI
                            totalMonthDurata1 = totalMonthDurata2 = totalWeekDurata1 = totalWeekDurata2 = TimeSpan.Zero;
                            totalMonthImporto = totalWeekImporto = 0d;
                            dayIndex = FirstDayIndex;

                            //Accumula tutte le date da elaborare (mese prima e mese dopo se servono)
                            periodDates = new List<DateTime>();

                            #region ULTIMA SETTIMANA MESE PRECEDENTE
                            if (UseExportDetail && firstPeriodDate.DayOfWeek != DayOfWeek.Monday && lastMonthTimesheets != null && lastMonthTimesheets.Count == 2)
                            {
                                TimesheetModuleItem lastMonthDiurno = lastMonthTimesheets.First();
                                TimesheetModuleItem lastMonthNotturno = lastMonthTimesheets.Last();

                                //Prende i giorni che vanno dall'ultimo lunedì del mese scorso fino alla fine del mese
                                periodDates = CommonService.GetDatesFromPeriod(lastMonthEnd.AddDays(DayOfWeek.Monday - lastMonthEnd.DayOfWeek), lastMonthEnd);

                                foreach (DateTime periodDate in periodDates)
                                {
                                    durata1 = TimeSpan.FromMinutes(lastMonthDiurno.GetDayMinutes(periodDate.Day));
                                    durata2 = TimeSpan.FromMinutes(lastMonthNotturno.GetDayMinutes(periodDate.Day));

                                    if (qualificaCode.Trim().Equals("0"))
                                    {
                                        durata1Hours = CommonService.GetDoubleFromMinutes(lastMonthDiurno.GetDayMinutes(periodDate.Day), true);
                                        durata2Hours = CommonService.GetDoubleFromMinutes(lastMonthNotturno.GetDayMinutes(periodDate.Day), true);
                                    }

                                    else if (qualificaCode.Trim().Equals("1"))
                                    {
                                        if (RepoManager.Tab_FestiviRepo.IsHolidayOrNotWorkDays(periodDate))
                                        {
                                            durata2Hours = CommonService.GetDoubleFromMinutes(lastMonthDiurno.GetDayMinutes(periodDate.Day) + lastMonthNotturno.GetDayMinutes(periodDate.Day), true);
                                            durata2 = durata1 + durata2;
                                            durata1Hours = 0d;
                                            durata1 = TimeSpan.Zero;
                                        }

                                        else
                                        {
                                            durata1Hours = CommonService.GetDoubleFromMinutes(lastMonthDiurno.GetDayMinutes(periodDate.Day) + lastMonthNotturno.GetDayMinutes(periodDate.Day), true);
                                            durata1 = durata1 + durata2;
                                            durata2Hours = 0d;
                                            durata2 = TimeSpan.Zero;
                                        }
                                    }

                                    else
                                    {
                                        durata1Hours = 0d;
                                        durata2Hours = 0d;
                                    }

                                    importo = Math.Round(durata1Hours * importo1 + durata2Hours * importo2, 2);

                                    //Aggiorna i totali (La settimana del mese precedente viene conteggiata solo nei totali settimanali, non in quelli mensili)
                                    //totalMonthDurata1 = totalMonthDurata1.Add(durata1);
                                    totalWeekDurata1 = totalWeekDurata1.Add(durata1);
                                    //totalMonthDurata2 = totalMonthDurata2.Add(durata2);
                                    totalWeekDurata2 = totalWeekDurata2.Add(durata2);
                                    //totalMonthImporto += importo;
                                    totalWeekImporto += importo;

                                    // si inserisce il giorno in processo
                                    CellInsertValue(colCode, 1, dayIndex, string.Format("{0} {1} {2}", CommonService.GetDayShortName(periodDate), periodDate.Day.ToString("00"), CommonService.GetMonthShortName(periodDate)), ExcelInsertTypeEnum.Content);
                                    CellInsertValue(colCode, 2, dayIndex, durata1, ExcelInsertTypeEnum.HhmmTime);
                                    CellInsertValue(colCode, 3, dayIndex, durata2, ExcelInsertTypeEnum.HhmmTime);
                                    CellInsertValue(colCode, 4, dayIndex, Convert.ToString(importo), ExcelInsertTypeEnum.Content);
                                    CellSetNumberFormat(colCode, 4, dayIndex, "€ @");
                                    dayIndex++;
                                }
                            }
                            #endregion

                            periodDates = CommonService.GetDatesFromPeriod(firstPeriodDate, lastPeriodDate);

                            #region MESE CORRENTE
                            foreach (DateTime periodDate in periodDates)
                            {
                                durata1 = TimeSpan.FromMinutes(currentMonthDiurno.GetDayMinutes(periodDate.Day));
                                durata2 = TimeSpan.FromMinutes(currentMonthNotturno.GetDayMinutes(periodDate.Day));

                                if (qualificaCode.Trim().Equals("0"))
                                {
                                    durata1Hours = CommonService.GetDoubleFromMinutes(currentMonthDiurno.GetDayMinutes(periodDate.Day), true);
                                    durata2Hours = CommonService.GetDoubleFromMinutes(currentMonthNotturno.GetDayMinutes(periodDate.Day), true);
                                }

                                else if (qualificaCode.Trim().Equals("1"))
                                {
                                    if (RepoManager.Tab_FestiviRepo.IsHolidayOrNotWorkDays(periodDate))
                                    {
                                        durata2Hours = CommonService.GetDoubleFromMinutes(currentMonthDiurno.GetDayMinutes(periodDate.Day) + currentMonthNotturno.GetDayMinutes(periodDate.Day), true);
                                        durata2 = durata1 + durata2;
                                        durata1Hours = 0d;
                                        durata1 = TimeSpan.Zero;
                                    }

                                    else
                                    {
                                        durata1Hours = CommonService.GetDoubleFromMinutes(currentMonthDiurno.GetDayMinutes(periodDate.Day) + currentMonthNotturno.GetDayMinutes(periodDate.Day), true);
                                        durata1 = durata1 + durata2;
                                        durata2Hours = 0d;
                                        durata2 = TimeSpan.Zero;
                                    }
                                }

                                else
                                {
                                    durata1Hours = 0d;
                                    durata2Hours = 0d;
                                }

                                importo = Math.Round(durata1Hours * importo1 + durata2Hours * importo2, 2);

                                //Aggiorna i totali
                                totalMonthDurata1 = totalMonthDurata1.Add(durata1);
                                totalWeekDurata1 = totalWeekDurata1.Add(durata1);
                                totalMonthDurata2 = totalMonthDurata2.Add(durata2);
                                totalWeekDurata2 = totalWeekDurata2.Add(durata2);
                                totalMonthImporto += importo;
                                totalWeekImporto += importo;

                                // si inserisce il giorno in processo
                                CellInsertValue(colCode, 1, dayIndex, string.Format("{0} {1}", periodDate.Day.ToString("00"), CommonService.GetDayShortName(periodDate)), ExcelInsertTypeEnum.Content);
                                CellInsertValue(colCode, 2, dayIndex, durata1, ExcelInsertTypeEnum.HhmmTime);
                                CellInsertValue(colCode, 3, dayIndex, durata2, ExcelInsertTypeEnum.HhmmTime);
                                CellInsertValue(colCode, 4, dayIndex, Convert.ToString(importo), ExcelInsertTypeEnum.Content);
                                CellSetNumberFormat(colCode, 4, dayIndex, "€ @");
                                dayIndex++;

                                //Se richiesto e se è una domenica, scrive i totali settimanali e resetta i totali settimanali
                                if (periodDate.DayOfWeek == DayOfWeek.Sunday || (periodDate.Equals(periodDates.Last()) && (nextMonthTimesheets == null || nextMonthTimesheets.Count != 2)))
                                {
                                    if (UseExportDetail)
                                    {
                                        CellInsertValue(colCode, 1, dayIndex, BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_SETTIMANA), ExcelInsertTypeEnum.Content);
                                        CellInsertValue(colCode, 2, dayIndex, totalWeekDurata1, ExcelInsertTypeEnum.HhmmTime);
                                        CellInsertValue(colCode, 3, dayIndex, totalWeekDurata2, ExcelInsertTypeEnum.HhmmTime);
                                        CellInsertValue(colCode, 4, dayIndex, Convert.ToString(totalWeekImporto), ExcelInsertTypeEnum.Content);
                                        CellSetNumberFormat(colCode, 4, dayIndex, "€ @");
                                        RangeSetFontBold(colCode, 1, dayIndex, 4, dayIndex);
                                        RangeSetBorders(colCode, 1, dayIndex, 4, dayIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.White, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.White, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.White, OfficeOpenXml.Style.ExcelBorderStyle.None);
                                        dayIndex += 2;
                                    }

                                    totalWeekDurata1 = totalWeekDurata2 = TimeSpan.Zero;
                                    totalWeekImporto = 0d;
                                }
                            }
                            #endregion
                            
                            #region PRIMA SETTIMANA MESE SUCESSIVO
                            if (UseExportDetail && lastPeriodDate.DayOfWeek != DayOfWeek.Sunday && nextMonthTimesheets != null && nextMonthTimesheets.Count == 2)
                            {
                                TimesheetModuleItem nextMonthDiurno = nextMonthTimesheets.First();
                                TimesheetModuleItem nextMonthNotturno = nextMonthTimesheets.Last();
                                {
                                    //Prende i giorni che vanno dal primo giorno del mese prossimo fino alla prima domenica del mese prossimo
                                    periodDates = CommonService.GetDatesFromPeriod(nextMonthInit, nextMonthInit.AddDays(7 - (int)nextMonthInit.DayOfWeek));

                                    foreach (DateTime periodDate in periodDates)
                                    {
                                        durata1 = TimeSpan.FromMinutes(nextMonthDiurno.GetDayMinutes(periodDate.Day));
                                        durata2 = TimeSpan.FromMinutes(nextMonthNotturno.GetDayMinutes(periodDate.Day));

                                        if (qualificaCode.Trim().Equals("0"))
                                        {
                                            durata1Hours = CommonService.GetDoubleFromMinutes(nextMonthDiurno.GetDayMinutes(periodDate.Day), true);
                                            durata2Hours = CommonService.GetDoubleFromMinutes(nextMonthNotturno.GetDayMinutes(periodDate.Day), true);
                                        }

                                        else if (qualificaCode.Trim().Equals("1"))
                                        {
                                            if (RepoManager.Tab_FestiviRepo.IsHolidayOrNotWorkDays(periodDate))
                                            {
                                                durata2Hours = CommonService.GetDoubleFromMinutes(nextMonthDiurno.GetDayMinutes(periodDate.Day) + nextMonthNotturno.GetDayMinutes(periodDate.Day), true);
                                                durata2 = durata1 + durata2;
                                                durata1Hours = 0d;
                                                durata1 = TimeSpan.Zero;
                                            }

                                            else
                                            {
                                                durata1Hours = CommonService.GetDoubleFromMinutes(nextMonthDiurno.GetDayMinutes(periodDate.Day) + nextMonthNotturno.GetDayMinutes(periodDate.Day), true);
                                                durata1 = durata1 + durata2;
                                                durata2Hours = 0d;
                                                durata2 = TimeSpan.Zero;
                                            }
                                        }

                                        else
                                        {
                                            durata1Hours = 0d;
                                            durata2Hours = 0d;
                                        }

                                        importo = Math.Round(durata1Hours * importo1 + durata2Hours * importo2, 2);

                                        //Aggiorna i totali (La settimana del mese successivo viene conteggiata solo nei totali settimanali, non in quelli mensili)
                                        //totalMonthDurata1 = totalMonthDurata1.Add(durata1);
                                        totalWeekDurata1 = totalWeekDurata1.Add(durata1);
                                        //totalMonthDurata2 = totalMonthDurata2.Add(durata2);
                                        totalWeekDurata2 = totalWeekDurata2.Add(durata2);
                                        //totalMonthImporto += importo;
                                        totalWeekImporto += importo;

                                        // si inserisce il giorno in processo
                                        CellInsertValue(colCode, 1, dayIndex, string.Format("{0} {1} {2}", CommonService.GetDayShortName(periodDate), periodDate.Day.ToString("00"), CommonService.GetMonthShortName(periodDate)), ExcelInsertTypeEnum.Content);
                                        CellInsertValue(colCode, 2, dayIndex, durata1, ExcelInsertTypeEnum.HhmmTime);
                                        CellInsertValue(colCode, 3, dayIndex, durata2, ExcelInsertTypeEnum.HhmmTime);
                                        CellInsertValue(colCode, 4, dayIndex, Convert.ToString(importo), ExcelInsertTypeEnum.Content);
                                        CellSetNumberFormat(colCode, 4, dayIndex, "€ @");
                                        dayIndex++;

                                        //Se richiesto e se è una domenica, scrive i totali settimanali e resetta i totali settimanali
                                        if (periodDate.DayOfWeek == DayOfWeek.Sunday)
                                        {
                                            CellInsertValue(colCode, 1, dayIndex, BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_SETTIMANA), ExcelInsertTypeEnum.Content);
                                            CellInsertValue(colCode, 2, dayIndex, totalWeekDurata1, ExcelInsertTypeEnum.HhmmTime);
                                            CellInsertValue(colCode, 3, dayIndex, totalWeekDurata2, ExcelInsertTypeEnum.HhmmTime);
                                            CellInsertValue(colCode, 4, dayIndex, Convert.ToString(totalWeekImporto), ExcelInsertTypeEnum.Content);
                                            RangeSetFontBold(colCode, 1, dayIndex, 4, dayIndex);
                                            RangeSetBorders(colCode, 1, dayIndex, 4, dayIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.White, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.White, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.White, OfficeOpenXml.Style.ExcelBorderStyle.None);
                                            dayIndex += 2;
                                        }
                                    }
                                }
                            }
                            #endregion

                            #region TOTALONE MENSILE
                            //Inserisce il totale
                            dayIndex++;
                            CellInsertValue(colCode, 1, dayIndex, BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_MESE), ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 2, dayIndex, totalMonthDurata1, ExcelInsertTypeEnum.HhmmTime);
                            CellInsertValue(colCode, 3, dayIndex, totalMonthDurata2, ExcelInsertTypeEnum.HhmmTime);
                            CellInsertValue(colCode, 4, dayIndex, Convert.ToString(totalMonthImporto), ExcelInsertTypeEnum.Content);
                            RangeSetFontBold(colCode, 1, dayIndex, 4, dayIndex);
                            RangeSetBorders(colCode, 1, dayIndex, 4, dayIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Double, System.Drawing.Color.White, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.White, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.White, OfficeOpenXml.Style.ExcelBorderStyle.None);
                            CellSetNumberFormat(colCode, 4, dayIndex, "€ @");
                            #endregion

                            #endregion
                        }
                    }
                }

                // al termine dell'operazione si cancella il modello utilizzato per la copia
                WorksheetDelete(ModelWorksheetName);
                // esporto quanto generato (in caso di assenza reg_v il file modello) sulla risposta del browser
                ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

                // una volta salvato l'oggetto excel viene cancellato dalla memoria
                ExcelWorkbookDispose();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Recupera tutte le registrazioni processabili del collaboratore per il periodo specificato.
        /// </summary>
        /// <param name="colId">L'identificativo del collaboratore per cui effettuare la ricerca.</param>
        /// <param name="startPeriod">La data di inizio del periodo in cui effettuare la ricerca.</param>
        /// <param name="endPeriod">La data di fine del periodo in cui effettuare la ricerca.</param>
        /// <returns>L'elenco delle registrazioni da processare per il collaboratore e periodo perscelto.</returns>
        private IEnumerable<Reg_V> GetProcessableColRegVs(int colId, DateTime startPeriod, DateTime endPeriod)
        {
            return RepoManager.Reg_VRepo.Find(regv => regv.Col_Id == colId && regv.Data_Reg >= startPeriod && regv.Data_Reg <= endPeriod
                                                      && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass
                                                      && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass);
        }
        #endregion
    }
}
