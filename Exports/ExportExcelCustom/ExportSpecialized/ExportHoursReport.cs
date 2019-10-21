using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class ExportHoursReport : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
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
        private const int FirstDayIndex = 11;

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
                IEnumerable<DateTime> periodDates = CommonService.GetDatesFromPeriod(firstPeriodDate, lastPeriodDate);

                // calcolo del nome dell'azienda che sta effettuando l'elaborazione
                string companyName = RepoManager.ParamRepo.ParametersRow.CompanyName;

                // generazione dell'excel su cui lavorare a partire dal percorso del modello
                ExcelWorkbookGenerateNew(ExcelModelFilePath);

                // per ogni collaboratore da processare
                foreach (int colId in selectedColIds)
                {
                    // è recuperato l'elenco delle registrazioni processabili
                    IEnumerable<Reg_V> colRegVs = GetProcessableColRegVs(colId, firstPeriodDate, lastPeriodDate);

                    //double nullable
                    double? nullDouble = null;

                    double importo_Orario = 0d;

                    //viene recuperato l'importo dal collaboratore
                    double? importo_Orario_Null = RepoManager.ColRepo.FirstOrDefault(s => s.Col_Id == colId).Retribuzione_Oraria_Col ;

                    //viene controllato se l'importo è null
                    if (importo_Orario_Null.HasValue)
                        importo_Orario = importo_Orario_Null.Value;
                    else
                        importo_Orario = 0d;

                        // si procede all'elaborazione del collaboratore solamente se sono presenti delle registrazioni
                        if (colRegVs.Any())
                    {

                        // calcolo dei dati cartellino del collaboratore per il mese in elaborazione
                        List<TimesheetModuleItem> colTimesheets = TimesheetModuleItem.GenerateTimeSheet(firstPeriodDate, false, false, String.Empty, false, new List<int>() { colId },
                                                        referenceEntity: "Col", timesheetOptions: new List<string>(), hasWeeklyTotals: false);

                        // dai dati cartellino del collaboratore si procede a recuperare quello relativo al piano
                        TimesheetModuleItem colPlanTimesheet = colTimesheets.FirstOrDefault(tsm => tsm.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN));

                        // si procede con l'elaborazione solamente se il collaboratore ha un piano collegato
                        if (colPlanTimesheet != default(TimesheetModuleItem))
                        {
                            // preparazione del gestore dei totali per il collaboratori
                            TimesheetTotalController colTotalController = new TimesheetTotalController(colTimesheets, false, "Col");

                            // calcolo dei dati del collaboratore da utilizzare nella compilazione del foglio
                            Col currentCol = RepoManager.ColRepo.First(col => col.Col_Id == colId);
                            string colCode = currentCol.Codice_Collaboratore;
                            string colDes = currentCol.CognomeNome_Col;
                            string colBadge = currentCol.LastPruCode; // TODO: non va usato l'ultimo ma quello assegnato nel giorno in processo
                            DateTime? employmentDate = currentCol.Data_Disponibilita_Inizio_Col;
                            string matriculationNumber = currentCol.Matricola_Col;

                            // viene copiato il foglio excel modello per far scrivere i dati 
                            WorksheetCopy(ModelWorksheetName, colCode);

                            // inserimento della testata relativa all'azienda
                            CellInsertValue(colCode, 1, 1, companyName, ExcelInsertTypeEnum.Content);

                            // inserimento della testata relativa al collaboratore
                            CellInsertValue(colCode, 3, 5, ExportPeriod.ToString("MMMM yyyy").ToUpper(), ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 3, 6, colBadge, ExcelInsertTypeEnum.Content);
                            if (employmentDate.HasValue)
                                CellInsertValue(colCode, 3, 7, employmentDate, ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 7, 5, colDes, ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 7, 6, matriculationNumber, ExcelInsertTypeEnum.Content);

                            //se è abilitata la visualizzazione degli importi
                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CalcoloImporti) == (int)CalcoloImporti.Show)
                            {
                                CellInsertValue(colCode, 7, 7, Convert.ToString(importo_Orario), ExcelInsertTypeEnum.Content);
                                CellSetNumberFormat(colCode, 7, 7, "€ @");
                            }

                            // inizializzazione delle variabili che conterranno il totale di quanto riportato
                            double totalMinutesDuration = 0d;
                            double totalDeltaPlusValue = 0d;
                            double totalDeltaMinusValue = 0d;


                            // per ogni giorno del mese
                            int dayIndex = FirstDayIndex;
                            foreach (DateTime periodDate in periodDates)
                            {
                                // si inserisce il giorno in processo
                                CellInsertValue(colCode, 1, dayIndex, String.Format("{0} {1}", periodDate.Day.ToString("00"), CommonService.GetDayShortName(periodDate)), ExcelInsertTypeEnum.Content);

                                // recupero del numero di ore previste per il collaboratore nel giorno in processo
                                double dayPlanHours = (double)CommonService.GetPropertyValue(colPlanTimesheet, String.Format("Day{0}", periodDate.Day.ToString("00")));

                                // si inseriscono i dati del giorno se è presente un piano (altrimenti si è di riposo)
                                if (!CommonService.IsDoubleZero(dayPlanHours))
                                {
                                    IEnumerable<Reg_V> dayRegVs = colRegVs.Where(regv => regv.Data_Reg == periodDate && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Duration)
                                                                          .OrderBy(regv => regv.Data_Ora_Fis_E);
                                    Reg_V firstDayRegV = dayRegVs.FirstOrDefault();
                                    Reg_V lastDayRegV = dayRegVs.LastOrDefault();
                                    if (firstDayRegV != default(Reg_V))
                                        if (firstDayRegV.Data_Ora_Fig_E.HasValue)
                                            CellInsertValue(colCode, 2, dayIndex, firstDayRegV.Data_Ora_Fig_E.Value.TimeOfDay, ExcelInsertTypeEnum.HhmmTime);
                                    if (lastDayRegV != default(Reg_V))
                                        if (lastDayRegV.Data_Ora_Fig_U.HasValue)
                                            CellInsertValue(colCode, 3, dayIndex, lastDayRegV.Data_Ora_Fig_U.Value.TimeOfDay, ExcelInsertTypeEnum.HhmmTime);

                                    TimeSpan dayDuration = GetDayTotal(colTimesheets, periodDate.Day, TotalTypeEnum.TotalWithoutJustification);
                                    if (dayDuration != TimeSpan.Zero)
                                    {
                                        CellInsertValue(colCode, 4, dayIndex, dayDuration, ExcelInsertTypeEnum.HhmmTime);
                                        totalMinutesDuration += dayDuration.TotalMinutes;
                                    }



                                    double colDayDeltaValue = colTotalController.GetDeltaHours(periodDate.Day, colId, null);
                                    if (!CommonService.IsDoubleZero(colDayDeltaValue))
                                    {
                                        TimeSpan currentDeltaMinutes = CommonService.GetTimeSpanFromMinutes(CommonService.FromHoursToMinutes(Math.Abs(colDayDeltaValue)));

                                        // si inserisce il delta in una colonna specifica se si tratta di un delta positivo o negativo
                                        CellInsertValue(colCode, colDayDeltaValue > 0.00d ? 6 : 5, dayIndex, currentDeltaMinutes, ExcelInsertTypeEnum.HhmmTime);

                                        if (colDayDeltaValue > 0.00d)
                                            totalDeltaPlusValue += colDayDeltaValue;
                                        else
                                            totalDeltaMinusValue += colDayDeltaValue;
                                    }

                                    TimeSpan justDayDuration = GetDayTotal(colTimesheets, periodDate.Day, TotalTypeEnum.TotalWithJustification);
                                    if (justDayDuration != TimeSpan.Zero)
                                    {
                                        CellInsertValue(colCode, 7, dayIndex, justDayDuration, ExcelInsertTypeEnum.HhmmTime);

                                        CellInsertValue(colCode, 8, dayIndex, GetJustificationsDescriptionString(colTimesheets, periodDate.Day), ExcelInsertTypeEnum.Content);
                                    }


                                }
                                //Personalizzazione comoda che permette di mostrare comunque le ore lavorate anche senza il piano orario 
                                else if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CalcoloImporti) == (int)CalcoloImporti.Show)
                                {
                                    IEnumerable<Reg_V> dayRegVs = colRegVs.Where(regv => regv.Data_Reg == periodDate && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Duration)
                                                                         .OrderBy(regv => regv.Data_Ora_Fis_E);
                                    Reg_V firstDayRegV = dayRegVs.FirstOrDefault();
                                    Reg_V lastDayRegV = dayRegVs.LastOrDefault();

                                    if (firstDayRegV != default(Reg_V))
                                        if (firstDayRegV.Data_Ora_Fig_E.HasValue)
                                            CellInsertValue(colCode, 2, dayIndex, firstDayRegV.Data_Ora_Fig_E.Value.TimeOfDay, ExcelInsertTypeEnum.HhmmTime);

                                    if (lastDayRegV != default(Reg_V))
                                        if (lastDayRegV.Data_Ora_Fig_U.HasValue)
                                            CellInsertValue(colCode, 3, dayIndex, lastDayRegV.Data_Ora_Fig_U.Value.TimeOfDay, ExcelInsertTypeEnum.HhmmTime);

                                    TimeSpan dayDuration = GetDayTotal(colTimesheets, periodDate.Day, TotalTypeEnum.TotalWithoutJustification);
                                    if (dayDuration != TimeSpan.Zero)
                                    {
                                        CellInsertValue(colCode, 4, dayIndex, dayDuration, ExcelInsertTypeEnum.HhmmTime);
                                        totalMinutesDuration += dayDuration.TotalMinutes;
                                    }

                                    TimeSpan justDayDuration = GetDayTotal(colTimesheets, periodDate.Day, TotalTypeEnum.TotalWithJustification);
                                    if (justDayDuration != TimeSpan.Zero)
                                    {
                                        CellInsertValue(colCode, 7, dayIndex, justDayDuration, ExcelInsertTypeEnum.HhmmTime);

                                        CellInsertValue(colCode, 8, dayIndex, GetJustificationsDescriptionString(colTimesheets, periodDate.Day), ExcelInsertTypeEnum.Content);
                                    }

                                }

                                else
                                    CellInsertValue(colCode, 2, dayIndex, BusinessService.GetLocalizedString(PowerWebResources.STR_RIPOSO), ExcelInsertTypeEnum.Content);

                                // incremento dell'indice giorno di scrittura
                                dayIndex++;
                            }

                            // al termine dei giorni del mese si inserisce il totale del mese
                            CellInsertValue(colCode, 1, dayIndex, BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE), ExcelInsertTypeEnum.Content);
                            CellInsertValue(colCode, 4, dayIndex, String.Format("=SUM(D{0}:D{1})", FirstDayIndex, dayIndex - 1), ExcelInsertTypeEnum.Formula);
                            CellSetNumberFormat(colCode, 4, dayIndex, "[h]:mm;@");

                            CellInsertValue(colCode, 5, dayIndex, String.Format("=SUM(E{0}:E{1})", FirstDayIndex, dayIndex - 1), ExcelInsertTypeEnum.Formula);
                            CellSetNumberFormat(colCode, 5, dayIndex, "[h]:mm;@");
                            CellInsertValue(colCode, 6, dayIndex, String.Format("=SUM(F{0}:F{1})", FirstDayIndex, dayIndex - 1), ExcelInsertTypeEnum.Formula);
                            CellSetNumberFormat(colCode, 6, dayIndex, "[h]:mm;@");


                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CalcoloImporti) == (int)CalcoloImporti.Show)
                            {

                                //inizializzazione variabili
                                double importoMensilePerMinuti = nullDouble ?? 0d;
                                double totaleImporto = nullDouble ?? 0d;

                                //viene calcolato l'importo in base ai minuti totali svolti nel mese
                                importoMensilePerMinuti = importo_Orario / 60;
                                totaleImporto = totalMinutesDuration * importoMensilePerMinuti;
                                totaleImporto = Math.Round(totaleImporto, 2);

                                // incremento dell'indice della riga
                                dayIndex++;

                                CellInsertValue(colCode, 1, dayIndex, BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_IMPORTI), ExcelInsertTypeEnum.Content);
                                CellInsertValue(colCode, 4, dayIndex, Convert.ToString(totaleImporto), ExcelInsertTypeEnum.Content);
                                CellSetNumberFormat(colCode, 4, dayIndex, "€ @");

                            }
                        }
                        else
                        {

                        }

                    }
                }

                // al termine dell'operazione si cancella il modello utilizzato per la copia
                WorksheetDelete(ModelWorksheetName);

            }

            // esporto quanto generato (in caso di assenza reg_v il file modello) sulla risposta del browser
            ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

            // una volta salvato l'oggetto excel viene cancellato dalla memoria
            ExcelWorkbookDispose();
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
                                                      && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass).ToList();
        }

        /// <summary>
        /// Recupera il totale indicato per il giorno e i timesheet specificati.
        /// </summary>
        /// <param name="timesheets">L'elenco dei timesheet da processare.</param>
        /// <param name="dayIndex">L'indice del giorno di cui calcolare il totale.</param>
        /// <param name="totalType">Il tipo di totale da processare.</param>
        /// <returns>Il totale calcolato per i dati specificati. In caso di mancato calcolo (mancanza di timesheet) allora si ritorna il valore <see cref="TimeSpan.Zero"/>.</returns>
        private TimeSpan GetDayTotal(IEnumerable<TimesheetModuleItem> timesheets, int dayIndex, TotalTypeEnum totalType)
        {
            // inizializzazione del valore di ritorno del metodo
            TimeSpan dayTotal = TimeSpan.Zero;

            // in base al tipo di totale si recuperano i timesheet da processare
            IEnumerable<TimesheetModuleItem> timesheetsToProcess = Enumerable.Empty<TimesheetModuleItem>();
            switch (totalType)
            {
                case TotalTypeEnum.TotalWithoutJustification: // tutti i timesheet con il totale delle ore senza motivazione (viaggi inclusi)
                    timesheetsToProcess = timesheets.Where(ts => ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) ||
                                                                 ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE) ||
                                                                 ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE) ||
                                                                 ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI) ||
                                                                 ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE));
                    break;
                case TotalTypeEnum.TotalWithJustification: // tutti i timesheet con il totale delle ore con motivazione (viaggi esclusi)
                    timesheetsToProcess = timesheets.Where(ts => ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ONL) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN));
                    break;
            }

            // se ci sono dei timesheet processabili per la richiesta
            if (timesheetsToProcess.Any())
            {
                // ciclo di calcolo dei totali per il giorno
                int totalMinutes = 0;
                foreach (TimesheetModuleItem timesheetToProcess in timesheetsToProcess)
                {
                    totalMinutes += timesheetToProcess.GetDayMinutes(dayIndex);
                }

                dayTotal = CommonService.GetTimeSpanFromMinutes(totalMinutes);
            }


            // ritorno del totale calcolato dal metodo
            return dayTotal;
        }

        /// <summary>
        /// Recupera tutte le motivazioni presenti nell'elenco di timesheet proposto per l'indice giorno specificato e le prepara per la stampa nell'export.
        /// </summary>
        /// <param name="timesheets">L'elenco dei timesheets da processare.</param>
        /// <param name="dayIndex">L'indice del giorno da verificare.</param>
        /// <returns>La stringa con l'elenco delle motivazioni presenti quel giorno.</returns>
        private string GetJustificationsDescriptionString(IEnumerable<TimesheetModuleItem> timesheets, int dayIndex)
        {
            // calcolo di tutti i timesheets processabili
            IEnumerable<TimesheetModuleItem> timesheetsToProcess = timesheets.Where(ts => ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ONL) &&
                                                                 ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN));

            StringBuilder justificationsString = new StringBuilder();

            // per ogni timesheet da processare si verifica il valore nel giorno; in caso il valore non sia zero allora si aggiunge la descrizione
            foreach (TimesheetModuleItem timesheet in timesheetsToProcess)
            {
                int dayMinutes = timesheet.GetDayMinutes(dayIndex);

                if (dayMinutes != 0)
                {
                    if (!String.IsNullOrEmpty(justificationsString.ToString()))
                        justificationsString.AppendFormat(", ");

                    justificationsString.AppendFormat(timesheet.Justification);
                }
            }

            return justificationsString.ToString();
        }

        #endregion
    }
}
