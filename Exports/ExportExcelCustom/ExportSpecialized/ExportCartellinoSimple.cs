using Business.BusinessExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Business;
using System.Drawing;
using Business.Repository;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    public class ExportCartellinoSimple : ExcelToolbox<TimesheetModuleItem>, IExportExcelCustom<TimesheetModuleItem>
    {

        #region Constants

        /// <summary>
        /// Indica la riga di partenza per la scrittura dei collaboratori
        /// </summary>
        private const int StartColRowIndex = 6;

        /// <summary>
        /// Indica la colonna di partenza per la scrittura dei dati e degli header dei giorni
        /// </summary>
        private const int FirstDayColumnIndex = 2;

        /// <summary>
        /// Indica la colonna di arrivo per la scrittura dei dati e degli header dei giorni
        /// </summary>
        private int lastDayColumnIndex = 3;

        /// <summary>
        /// Il tipo di bordo utilizzato dalle tabelle presenti nel foglio excel
        /// </summary>
        private const OfficeOpenXml.Style.ExcelBorderStyle TablesBordersStyle = OfficeOpenXml.Style.ExcelBorderStyle.Thin;



        #endregion

        #region Fields

        /// <summary>
        /// L'elenco delle date del periodo in elaborazione
        /// </summary>
        private List<DateTime> _exportPeriodDates = null;

        /// <summary>
        /// Il colore del bordo utilizzato dalle tabelle presenti nel foglio excel
        /// </summary>
        private readonly Color _tableBordersColor = Color.Black;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Recupera l'elenco delle date del periodo in elaborazione.
        /// </summary>
        /// <value>
        /// L'elenco delle date del periodo in elaborazione.
        /// </value>
        protected IEnumerable<DateTime> ExportPeriodDates
        {
            get
            {
                // se il periodo del mese non è già stato calcolato, lo si popola
                if (_exportPeriodDates == null)
                {
                    DateTime firstMonthDate = CommonService.GetFirstMonthDay(ExportPeriod);
                    DateTime lastMonthDate = CommonService.GetLastMonthDay(ExportPeriod);
                    _exportPeriodDates = CommonService.GetDatesFromPeriod(firstMonthDate, lastMonthDate);
                }

                // ritorno del periodo del mese in elaborazione
                return _exportPeriodDates;
            }
        }

        #endregion

        #region Public Properties

        public string Path { get; set; }
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
        /// <valu>
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

        /// <summary>
        /// Booleano per la compressione del file
        /// </summary>
        public bool Compress { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="entitiesToExport">L'elenco delle entità da esportare</param>
        /// <exception cref="NullReferenceException">Total controller is null</exception>
        public override void LaunchExport(IQueryable<TimesheetModuleItem> entitiesToExport)
        {
            // in caso non sia stato impostato il total controller si procede alla generazione di un'ecccezione
            //if (TsTotalController == null)
            //    throw new NullReferenceException("Total controller is null");

            // per prima cosa si procede all'apertura del modello
            ExcelWorkbookGenerateNew(ExcelModelFilePath);

            // una volta generato il file, se ci sono elementi da processare
            if (entitiesToExport.Any())
            {
                //Estrae il metodo di arrotondamento richiesto ed eventualmente ordina la lista
                int sortExportTimesheetSimple = (int)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.SortExportTimesheetSimple);
                if (sortExportTimesheetSimple == (int)SortExportTimesheetSimple.SurnameName)
                {
                    entitiesToExport = entitiesToExport.OrderBy(ts => ts.ColDesc);
                }

                // si scrive la testata dell'export
                CellInsertValue(1, 1, 1, ExportPeriod.ToString("MMMM yyyy"), ExcelInsertTypeEnum.Content);

                // si inizializza la posizione di scrittura dei collabotori
                int currentColRowIndex = StartColRowIndex;

                // si raggruppano gli item da processare per codice collaboratore gli elementi da visualizzare e si cicla su di essi
                foreach (IGrouping<string, TimesheetModuleItem> itemsGroupedByCodCol in entitiesToExport.GroupBy(tsm => tsm.ColMnemonic))
                {
                    // si scrive il cartellino per il collaboratore in elaborazione e si recupera l'ultima posizione da lui scritta
                    int lastWrittenRowIndex = WriteColTsmItems(itemsGroupedByCodCol.AsEnumerable(), currentColRowIndex);


                    // se sono state scritte delle righe si posizionano due righe vuote prima della prossima scrittura di dati
                    if (lastWrittenRowIndex != currentColRowIndex)
                        currentColRowIndex = lastWrittenRowIndex + 2;
                }
            }



        }

        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="selectedColIds">L'elenco degli id collaboratore selezionati per l'export.</param>
        /// <param name="selectedCantIds">L'elenco degli id cantiere selezionati per l'export.</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds, IEnumerable<int> selectedCliIds)
        {
            if (selectedColIds.Count() > 0)
            {
                List<TimesheetModuleItem> timesheetList = new List<TimesheetModuleItem>();

                List<Domain.Col> collaboratori = RepoManager.ColRepo.Find(c => selectedColIds.Contains(c.Col_Id)).ToList();

                foreach (Domain.Col col in collaboratori)
                {
                    if (RepoManager.ParamRepo.ParametersRow.Cartellino_Abilita_Stampa_Cart_Editabile)
                    {
                        var cart = TimesheetModuleItem.GenerateCartellino(ExportPeriod, col, calculateOrdStrTimesheet: true);
                        timesheetList.AddRange(cart["justification"]);
                        timesheetList.AddRange(cart["straordinari"]);
                    }
                    else
                    {
                        timesheetList.AddRange(TimesheetModuleItem.GenerateCartellino(ExportPeriod, col)["justification"]);
                    }

                }

                LaunchExport(timesheetList.AsQueryable());
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Scrive alla posizione del collaboratore indicata l'elenco dei timesheet con relativi dati.
        /// </summary>
        /// <param name="colTsmItems">L'elenco dei dati timesheet da scrivere.</param>
        /// <param name="currentColRowIndex">L'indice di riga del collaboratore da cui cominciare a scrivere.</param>
        /// <returns>L'ultima riga utilizzata per la scrittura del dato di timesheets</returns>
        private int WriteColTsmItems(IEnumerable<TimesheetModuleItem> colTsmItems, int currentColRowIndex)
        {
            // di default il metodo ritorna il punto in cui ha cominciato a scrivere
            int returnRowIndex = currentColRowIndex;

            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.FormattingExportTimeSheetSimple);

            //Recupera la customization che indica quali cartellini esportare
            int timesheetCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheetSimpleExport);

            if (timesheetCustomization == (int)TimesheetSimpleExport.OnlyFigTimesheetFormulaTotal)
            {
                colTsmItems = colTsmItems.Where(ts => ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE));
            }

            // se sono presenti elementi da processare
            if (colTsmItems.Any())
            {
                // scrittura del collaboratore di cui si sta scrivendo il cartellino
                TimesheetModuleItem firstTsmItem = colTsmItems.First();

                string colDes = String.Format("{0} - {1}", firstTsmItem.ColMnemonic, firstTsmItem.ColDesc);
                CellInsertValue(1, 1, returnRowIndex, String.Format("{0}: {1}"
                                                                        , BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE)
                                                                        , colDes)
                                                    , ExcelInsertTypeEnum.Content);
                RangeUnion(1, 1, returnRowIndex, 25, returnRowIndex);

                returnRowIndex++;

                // intestazione del tipo motivazione
                CellInsertValue(1, 1, returnRowIndex, BusinessService.GetLocalizedString(PowerWebResources.LBL_TIPO).ToUpper(), ExcelInsertTypeEnum.Content);

                // compilazione della testata del totale dei giorni
                int lastDataColumn = FirstDayColumnIndex;
                foreach (DateTime periodDate in ExportPeriodDates)
                    CellInsertValue(1, lastDataColumn++, returnRowIndex,
                                    String.Format("{0} {1}", periodDate.Day.ToString("00"), CommonService.GetDayShortName(periodDate)), ExcelInsertTypeEnum.Content);

                lastDayColumnIndex = lastDataColumn - 1;
                // compilazione della colonna totale con il totale dei giorni lavorati
                CellInsertValue(1, lastDataColumn++, returnRowIndex, BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE), ExcelInsertTypeEnum.Content);
                CellInsertValue(1, lastDataColumn++, returnRowIndex, BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_GIORNI_PERIODO), ExcelInsertTypeEnum.Content);

                RangeSetBackgroundColor(1, FirstDayColumnIndex, returnRowIndex, lastDataColumn - 1, returnRowIndex, Color.FromArgb(217, 217, 217), OfficeOpenXml.Style.ExcelFillStyle.Solid);
                RangeSetBorders(1, FirstDayColumnIndex, returnRowIndex, lastDataColumn - 1, returnRowIndex, _tableBordersColor, TablesBordersStyle, _tableBordersColor,
                                TablesBordersStyle, _tableBordersColor, TablesBordersStyle, _tableBordersColor, TablesBordersStyle);
                RangeSetFontBold(1, FirstDayColumnIndex, returnRowIndex, lastDataColumn - 1, returnRowIndex);

                // incremento della linea in scrittura
                returnRowIndex++;

                // se è presente un piano lo si riporta tra le ore nel report
                TimesheetModuleItem planTimesheet = colTsmItems.FirstOrDefault(tsm => tsm.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN));
                if (planTimesheet != default(TimesheetModuleItem))
                {
                    CellInsertValue(1, 1, returnRowIndex, BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN).ToUpper(), ExcelInsertTypeEnum.Content);

                    lastDataColumn = FirstDayColumnIndex;
                    foreach (DateTime periodDate in ExportPeriodDates)
                    {
                        //Recupera il valore del cartellino corrispondente al giorno in elaborazione
                        double valueToInsert = (double)CommonService.GetPropertyValue(planTimesheet, String.Format("Day{0}", periodDate.Day.ToString("00")));

                        //se è attiva la personalizzazione dell'inclusione delle ore notturne nel totale delle ore lavoarate
                        if (customizationVersion == (int)FormattingExportTimeSheetSimple.Hour)
                        {
                            TimeSpan timespan = doubleToTimespan(valueToInsert);

                            //Inserisce il valore nell'apposita cella
                            CellInsertValue(1, lastDataColumn++, returnRowIndex, timespan, ExcelInsertTypeEnum.HhmmTime);
                        }
                        else
                            //Inserisce il valore nell'apposita cella
                            CellInsertValue(1, lastDataColumn++, returnRowIndex, doubleValueToWrite(valueToInsert).ToString(), ExcelInsertTypeEnum.Content);
                    }

                    //se è attiva la personalizzazione dell'inclusione delle ore notturne nel totale delle ore lavoarate
                    if (customizationVersion == (int)FormattingExportTimeSheetSimple.Hour)
                    {
                        CellInsertValue(1, lastDataColumn, returnRowIndex, String.Format("=SUM({0}:{1})", CommonService.GetColumnName(FirstDayColumnIndex - 1) + returnRowIndex.ToString(), CommonService.GetColumnName(lastDayColumnIndex - 1) + returnRowIndex.ToString()), ExcelInsertTypeEnum.Formula);
                        CellSetNumberFormat(1, lastDataColumn++, returnRowIndex, "[h]:mm");
                    }
                    else
                    {
                        //totale delle ore di piano
                        CellInsertValue(1, lastDataColumn, returnRowIndex, planTimesheet.TotalHours.ToString(), ExcelInsertTypeEnum.Content);
                        CellSetNumberFormat(1, lastDataColumn++, returnRowIndex, "00.00");
                    }


                    CellInsertValue(1, lastDataColumn++, returnRowIndex, planTimesheet.TotalDays, ExcelInsertTypeEnum.Content);

                    RangeSetBorders(1, FirstDayColumnIndex, returnRowIndex, lastDataColumn - 1, returnRowIndex, _tableBordersColor, TablesBordersStyle, _tableBordersColor,
                                TablesBordersStyle, _tableBordersColor, TablesBordersStyle, _tableBordersColor, TablesBordersStyle);

                    // incremento dell'indice di scrittura della riga
                    returnRowIndex++;
                }

                // ... dopo di che per il collaboratore si esportano i restanti timesheet
                List<TimesheetModuleItem> remainingTimesheets = colTsmItems.Where(tsm => tsm.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN)).ToList();

                if (remainingTimesheets.Any())
                {
                    foreach (TimesheetModuleItem itemToExport in remainingTimesheets)
                    {
                        // Scrive il riporto ore precedenti se richiesto
                        if (itemToExport.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA) && RepoManager.ParamRepo.ParametersRow.Abilita_Monte_Minuti && RepoManager.ParamRepo.ParametersRow.Flag_Monte_Ore != (int)MothlyHoursEnum.None)
                        {
                            CellInsertValue(1, 29, currentColRowIndex, string.Format("{0}: {1}"
                                                                        , BusinessService.GetLocalizedString(PowerWebResources.LBL_RIPORTO_ORE_PRECEDENTI)
                                                                        , CommonService.formatDoubleToHourString(itemToExport.LastMonthlyHours, isDecimalHours: true, doNotShowZero: true))
                                                    , ExcelInsertTypeEnum.Content);
                            RangeUnion(1, 29, currentColRowIndex, 34, currentColRowIndex);
                        }

                        CellInsertValue(1, 1, returnRowIndex, itemToExport.Justification.ToUpper(), ExcelInsertTypeEnum.Content);

                        lastDataColumn = FirstDayColumnIndex;
                        foreach (DateTime periodDate in ExportPeriodDates)
                        {
                            //Recupera il valore del cartellino corrispondente al giorno in elaborazione
                            double valueToInsert = (double)CommonService.GetPropertyValue(itemToExport, String.Format("Day{0}", periodDate.Day.ToString("00")));

                            //se è attiva la personalizzazione dell'inclusione delle ore notturne nel totale delle ore lavoarate
                            if (customizationVersion == (int)FormattingExportTimeSheetSimple.Hour)
                            {
                                TimeSpan timespan = doubleToTimespan(valueToInsert);

                                //Inserisce il valore nell'apposita cella
                                CellInsertValue(1, lastDataColumn++, returnRowIndex, timespan, ExcelInsertTypeEnum.HhmmTime);
                            }
                            else
                                //Inserisce il valore nell'apposita cella
                                CellInsertValue(1, lastDataColumn++, returnRowIndex, doubleValueToWrite(valueToInsert).ToString(), ExcelInsertTypeEnum.Content);

                        }

                        //se è attiva la personalizzazione dell'inclusione delle ore notturne nel totale delle ore lavoarate
                        if (customizationVersion == (int)FormattingExportTimeSheetSimple.Hour)
                        {
                            CellInsertValue(1, lastDataColumn, returnRowIndex, String.Format("=SUM({0}:{1})", CommonService.GetColumnName(FirstDayColumnIndex - 1) + returnRowIndex.ToString(), CommonService.GetColumnName(lastDayColumnIndex - 1) + returnRowIndex.ToString()), ExcelInsertTypeEnum.Formula);
                            CellSetNumberFormat(1, lastDataColumn++, returnRowIndex, "[h]:mm");
                        }
                        else
                        {

                            CellInsertValue(1, lastDataColumn, returnRowIndex, itemToExport.TotalHours.ToString(), ExcelInsertTypeEnum.Content);
                            CellSetNumberFormat(1, lastDataColumn++, returnRowIndex, "00.00");
                        }

                        CellInsertValue(1, lastDataColumn++, returnRowIndex, itemToExport.TotalDays, ExcelInsertTypeEnum.Content);

                        RangeSetBorders(1, FirstDayColumnIndex, returnRowIndex, lastDataColumn - 1, returnRowIndex, _tableBordersColor, TablesBordersStyle, _tableBordersColor,
                                    TablesBordersStyle, _tableBordersColor, TablesBordersStyle, _tableBordersColor, TablesBordersStyle);

                        //incremento dell'indice di riga
                        if (itemToExport.Justification == "Delta")
                        {
                            returnRowIndex++;
                        }
                        returnRowIndex++;
                    }
                }
            }

            // ritorno dell'ultimo indice scritto dal metodo
            return returnRowIndex;
        }

        /// <summary>
        /// Prepara il valore da scrivere su excel partendo dal valore double (rappresentante le ore) da scrivere.
        /// </summary>
        /// <param name="valueToWrite">Il valore double da scrivere nell'export.</param>
        /// <returns>il valore risultante dal processo del double da inserire</returns>
        private object doubleValueToWrite(double valueToWrite)
        {
            //if (CommonService.IsDoubleZero(valueToWrite))
            //    return "00:00";
            //else
            return valueToWrite;
        }

        /// <summary>
        /// Prepara il valore da scrivere su excel partendo dal valore double (rappresentante le ore) da scrivere.
        /// </summary>
        /// <param name="valueToWrite">Il valore double da scrivere nell'export.</param>
        /// <returns>il valore risultante dal processo del double da inserire</returns>
        private TimeSpan doubleToTimespan(double value)
        {
            String toTimeSpan = doubleValueToWrite(value).ToString();

            //Se il numero è intero, inserisce gli mette la virgola
            if (!toTimeSpan.Contains(','))
            {
                toTimeSpan = String.Concat(toTimeSpan, ",00");
            }

            //Sostituisce ',' con ':'
            toTimeSpan = toTimeSpan.Replace(',', ':');

            //Se ci sono meno di 5 caratteri (quindi l'ultima cifra decimale è 0 ed è stata troncata)
            if (toTimeSpan.Length < 5)
            {
                //Aggiunge zeri fino ad arrivare alla lunghezza di 5 caratteri
                for (int n = toTimeSpan.Substring(toTimeSpan.IndexOf(':') + 1).Length; n < 2; n++)
                {
                    toTimeSpan = String.Concat(toTimeSpan, "0");
                }
            }

            //Aggiunge i secondi in fondo
            toTimeSpan = String.Concat(toTimeSpan, ":00");


            int firstColon = toTimeSpan.IndexOf(':');
            int hours = Int32.Parse(toTimeSpan.Substring(0, firstColon));
            if (hours > 23)
            {
                int days = hours / 24;
                hours = hours % 24;

                toTimeSpan = String.Concat(days, '.', hours, ':', toTimeSpan.Substring(++firstColon));
            }

            //Se il valore è negativo, aggiunge il segno meno
            //if (value < 0d)
            //{
            //    toTimeSpan = String.Concat("-", toTimeSpan);
            //}

            return TimeSpan.Parse(toTimeSpan);
        }
        #endregion
    }
}
