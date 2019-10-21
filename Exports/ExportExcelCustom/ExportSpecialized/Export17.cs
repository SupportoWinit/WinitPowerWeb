using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Business;
using Business.Repository;
using Business.XmlExportsData.Perfetto;
using Common;
using Domain;
using OfficeOpenXml.Style;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /// <summary>
    /// Classe utilizzata per la produzione e l'estazione verso l'utente dell'export 17
    /// </summary>
    public class Export17 : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
    {
        #region Private Constants

        /// <summary>
        /// Il nome del worksheet che fa da modello per tutti gli altri in fase di generazione
        /// </summary>
        private const string ModelWorksheetName = "Modello";

        /// <summary>
        /// Il numero di riga in cui partire a scrivere i dati di dettaglio
        /// </summary>
        private const int ModelFirstDetailRow = 11;

        #endregion

        #region Public Properties

        /// <summary>
        /// Recupera o imposta il periodo (mese/anno) di riferimento dell'export.
        /// </summary>
        /// <value>
        /// Il periodo (mese/anno) di riferimento dell'export.
        /// </value>
        public DateTime ExportPeriod { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che indica se è necessario impostare uno specifico calcolo (figurative/fisiche).
        /// </summary>
        /// <value>
        /// <c>true</c> se è necessario impostare uno specifico calcolo (figurative/fisiche); altrimenti, <c>false</c>.
        /// </value>
        public bool UseCalculationType { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che indica se è necessario utilizzare un calcolo specifico di ore (solo durata/con entrata uscita).
        /// </summary>
        /// <value>
        /// <c>true</c> se è necessario utilizzare un calcolo specifico di ore (solo durata/con entrata uscita); altrimenti, <c>false</c>.
        /// </value>
        public bool UseHourType { get; set; }

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
        /// Recupera o imposta un valore ch indica se utilizzare oppure no l'export del confronto ore budget dettagliato
        /// </summary>
        /// <value>
        /// <c>true</c> se si deve utilizzare oppure no l'export dettagliato; altrimenti, <c>false</c>.
        /// </value>
       public bool UseExportDetail { get; set; }

        /// <summary>
        /// Recupera o imposta lo specifico calcolo da utilizzare (figurative/fisiche); utilizato solo se <see cref="UseCalculationType" /> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Lo specifico calcolo da utilizzare (figurative/fisiche); utilizato solo se <see cref="UseCalculationType" /> è valorizzato a <c>true</c>.
        /// </value>
        public ExportRegVCalculationTypeEnum CalculationType { get; set; }

        /// <summary>
        /// Recupera o imposta il tipo di calc
        /// </summary>
        /// <value>
        /// Il tipo di calcolo sp
        /// </value>
        public ExportRegVHourTypeEnum HourType { get; set; }

        /// <summary>
        /// Recupera o imposta il valore in minuti della tolleranza sulla durata utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="IExportExcelCustom{T}.UseDurationTollerance"/> è valorizzato
        /// a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il valore in minuti della tolleranza sulla durata utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="IExportExcelCustom{T}.UseDurationTollerance"/> è valorizzato a <c>true</c>.
        /// </value>
        public int DurationTollerance { get; set; }

        /// <summary>
        /// Recupera o imposta il valore in minuti della tolleranza sull'entrata/uscita utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="IExportExcelCustom{T}.UseEUTollerance"/> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il valore in minuti della tolleranza sull'entrata/uscita utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="IExportExcelCustom{T}.UseEUTollerance"/> è valorizzato a <c>true</c>.
        /// </value>
        public int EUTollerance { get; set; }
       

        /// <summary>
        /// Recupera o imposta la stringa che rappresenta l'entità di primo riferimento per selezione del modello excel.
        /// </summary>
        /// <value>
        /// La stringa che rappresenta l'entità di primo riferimento per la selezione del modello excel.
        /// </value>
        public ExcelModelSelectionTypeEnum ModelFirstEntity { get; set; }

        /// <summary>
        /// Recupera o imposta il percorso del modello excel su disco.
        /// </summary>
        /// <value>
        /// Il percorso del modello excel su disco.
        /// </value>
        public string ExcelModelFilePath { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Metodo utilizzato per il lancio dell'export.
        /// </summary>
        /// <param name="entitiesToExport">Le entità da esportare sull'export.</param>
        public override void LaunchExport(IQueryable<Reg_V> entitiesToExport)
        {
            // inizializzazione del foglio excel da processare
            ExcelWorkbookGenerateNew(ExcelModelFilePath);

            // si procede con l'export solamente se ci sono delle entità da estrarre
            if (entitiesToExport.Any())
            {
                // le reg_v passate come parametro sono raggruppate per codice collaboratore e ordinate per data/ora
                // una volta epurate da tutto ciò che non va esportato (devono rimanere solamente reg_v abbinate e viaggi
                var groupedEntitiesByCol = entitiesToExport.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip ||
                    (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None && regv.Registrazione_Stato_Reg != (int)RegStateEnum.None)).GroupBy(regv => regv.Col_Mnemonic);

                // calcolo la lista di date che compongono il periodo (mese/anno) di export
                var periodDates = CommonService.GetDatesFromPeriod(CommonService.GetFirstMonthDay(ExportPeriod), CommonService.GetLastMonthDay(ExportPeriod));

                var colIdsWithRegVs = new List<int>();

                // per ogni collaboratore da processare
                foreach (var regvsGroupedByCol in groupedEntitiesByCol)
                {
                    // calcolo del codice collaboratore utilizzato nel calcolo dei fogli
                    var codCol = regvsGroupedByCol.Key.Trim();

                    // calcolo della descrizione del collaboratore utilizzato per la compilazione della testata
                    var desCol = regvsGroupedByCol.First().Col_Desc;

                    // copia del worksheet modello come nuovo elemento
                    WorksheetCopy(ModelWorksheetName, codCol);

                    // scrittura dell'header del worksheet
                    WriteWorksheetHeader(codCol, desCol);

                    // impostazione dei bordi della prima riga vuota
                    RangeSetBorders(codCol, 1, ModelFirstDetailRow - 1, 13, ModelFirstDetailRow - 1, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin,
                        Color.Black, ExcelBorderStyle.Thin);

                    // scrivo le reg_v sul foglio
                    WriteRegVs(periodDates, codCol, regvsGroupedByCol.Select(regv => regv));

                    // dopo aver scritto le reg_v sul foglio si procede all'auto dimensionamento delle colonne
                    ColumnsSetWidth(codCol, 1, 1, 9d);
                    ColumnsSetWidth(codCol, 2, 2, 35d);
                    ColumnsSetWidth(codCol, 3, 7, 9d);
                    ColumnsSetWidth(codCol, 13, 15, 0d);

                    int currentColId = Convert.ToInt32(regvsGroupedByCol.FirstOrDefault().Col_Id);
                    if (!colIdsWithRegVs.Contains(currentColId) && currentColId != 0)

                        //mi creo una lista con tutti i collaboratori che presentano delle regV
                        colIdsWithRegVs.Add(Convert.ToInt32(regvsGroupedByCol.FirstOrDefault().Col_Id));
                }

                //Collezione che mi restituisce tutti i collaboratri che sono attivi ma non presentano RegV
                IEnumerable<Col> colWithoutRegVs = RepoManager.ColRepo.Find(col => !col.DisAbilitazione_Col && !colIdsWithRegVs.Contains(col.Col_Id));

                //Inserisco un foglio per  ogni collaboratore che non presenta delle RegV 
                foreach (var col in colWithoutRegVs)
                {
                    // calcolo del codice collaboratore utilizzato nel calcolo dei fogli
                    var codCol = col.Codice_Collaboratore;

                    // calcolo della descrizione del collaboratore utilizzato per la compilazione della testata
                    var desCol = col.CognomeNome_Col;

                    // copia del worksheet modello come nuovo elemento
                    WorksheetCopy(ModelWorksheetName, codCol);

                    // scrittura dell'header del worksheet
                    WriteWorksheetHeader(codCol, desCol);

                    // impostazione dei bordi della prima riga vuota
                    RangeSetBorders(codCol, 1, ModelFirstDetailRow - 1, 13, ModelFirstDetailRow - 1, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin,
                        Color.Black, ExcelBorderStyle.Thin);


                    // dopo aver scritto le reg_v sul foglio si procede all'auto dimensionamento delle colonne
                    ColumnsSetWidth(codCol, 1, 1, 9d);
                    ColumnsSetWidth(codCol, 2, 2, 35d);
                    ColumnsSetWidth(codCol, 3, 7, 9d);
                    ColumnsSetWidth(codCol, 11, 13, 0d);


                }

                // al termine della generazione dei worksheet cancello il worksheet modello
                WorksheetDelete(ModelWorksheetName);
            }
            else
            {
                CellInsertValue(ModelWorksheetName, 1, ModelFirstDetailRow - 1, BusinessService.GetLocalizedString(PowerWebResources.ERR_NESSUN_RECORD_PER_LE_CONDIZIONI_INDICATE), ExcelInsertTypeEnum.Content);
                RangeSetFontSize(ModelWorksheetName, 1, ModelFirstDetailRow - 1, 1, ModelFirstDetailRow - 1, 18);
                RangeSetFontColor(ModelWorksheetName, 1, ModelFirstDetailRow - 1, 1, ModelFirstDetailRow - 1, Color.Red);
            }

            // esporto quanto generato (in caso di assenza reg_v il file modello) sulla risposta del browser
            ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

            // una volta salvato l'oggetto excel viene cancellato dalla memoria
            ExcelWorkbookDispose();

        }

        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="selectedColIds">L'elenco degli id collaboratore selezionati per l'export.</param>
        /// <param name="selectedCantIds">L'elenco degli id cantiere selezionati per l'export.</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Compila l'header del modello sul foglio passato come parametro.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="colDesc">La descrizione del collaboratore da inserire in testata</param>
        private void WriteWorksheetHeader(string worksheetName, string colDesc)
        {
            // 1. Ditta
            CellInsertValue(worksheetName, 2, 6, RepoManager.ParamRepo.ParametersRow.CompanyName, ExcelInsertTypeEnum.Content);

            // 2. Mese
            CellInsertValue(worksheetName, 2, 7, CommonService.GetMonthName(ExportPeriod).ToUpper(), ExcelInsertTypeEnum.Content);

            // 3. Collaboratore
            CellInsertValue(worksheetName, 5, 6, colDesc, ExcelInsertTypeEnum.Content);

        }

        /// <summary>
        /// Scrive l'elenco di reg_v per i giorni passati come parametro all'interno di uno specifico worksheet.
        /// </summary>
        /// <param name="periodDates">La lista di date da scrivere.</param>
        /// <param name="worksheetName">Il nome del worksheet su cui scrivere i dati.</param>
        /// <param name="regVstWrite">Le reg_v da scrivere.</param>
        private void WriteRegVs(IEnumerable<DateTime> periodDates, string worksheetName, IEnumerable<Reg_V> regVstWrite)
        {
            int currentRow = ModelFirstDetailRow;

            // per ogni data da processare
            foreach (var date in periodDates)
            {
                // recupero le reg_v corrispondenti alla data che si sta processando
                DateTime currentDate = date; // suggerito da resharper (copia variabile locale) per problemi di compatibilità tra i compilatori.
                var dayRegVs = regVstWrite.Where(regv => regv.Data_Reg == currentDate).OrderBy(regv => regv.Data_Ora_Fis_E);

                // calcolo del valore del giorno da scrivere composto da numero del giorno più nome corto (upper case)
                string outputDayValue = String.Format("{0} - {1}", date.Day.ToString("00"), CommonService.GetDayShortName(currentDate).ToUpper());

                // se ci sono delle reg_v da stampare
                if (dayRegVs.Any())
                {
                    // salvo la posizione della prima riga scritta nel giorno
                    // e inizializzo l'ultima riga scritta per il giorno
                    int firstDayRow = currentRow;
                    int lastDayRow = currentRow;

                    // inizializzo la variabile che indica se processare ancora i viaggi trasformati in ore lavorate per sottokilometraggio
                    // (se si sta processando un sabato o una domenica allora non si utilizzano i viaggi sotto i 10 Km)
                    bool noMoreUnderKmTrips = currentDate.DayOfWeek == DayOfWeek.Sunday || currentDate.DayOfWeek == DayOfWeek.Saturday;

                    // inizializzazione del valore che calcola il totale 
                    // dei minuti in giornata (utilizzato per scartare eventuali viaggi  trattati come ore lavorate
                    int dayTotalWorkingHours = 0;

                    // viene recuperato il parametro della personalizzazione di export xml che indica sotto quale soglia kilometrica trattare i viaggi come ore lavorate
                    string kmParam = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.RegExportToXmlEnum, "TreatTripAsWorkedUnderKM");
                    decimal kmThreshold = 0m;
                    if (!String.IsNullOrEmpty(kmParam))
                        decimal.TryParse(kmParam, out kmThreshold);

                    // viene recuperato il parametro della personalizzazione di export xml che indica se trattare o meno i viaggi di inizio fine/giornata nello stesso
                    // comune sotto il numero di km del threshold
                    string doNotTreatStartEndTripUnderKmParam = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.RegExportToXmlEnum, "DoNotTreatStartEndTripUnderKM");
                    bool doNotTreatStartEndTripUnderKm = true;
                    if (!String.IsNullOrEmpty(doNotTreatStartEndTripUnderKmParam))
                        bool.TryParse(doNotTreatStartEndTripUnderKmParam, out doNotTreatStartEndTripUnderKm);

                    // viene recuperato il parametro della personalizzazione di export xml che indica se trattare solo come ore ordinarie (a riempimento) i viaggi
                    // nello stesso comune sotto il threshold di Km
                    string doNotTreatTripUnderKmAsOvertineParam = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.RegExportToXmlEnum, "DoNotTreatTripUnderKmAsOvertine");
                    bool doNotTreatTripUnderKmAsOvertime = false;
                    if (!String.IsNullOrEmpty(doNotTreatTripUnderKmAsOvertineParam))
                        bool.TryParse(doNotTreatTripUnderKmAsOvertineParam, out doNotTreatTripUnderKmAsOvertime);

                    // viene effettuato un pre-ciclo delle ore giornaliere al fine di verificare ed eventualmente rimuovere le ore viaggio
                    // trattate come ore lavorate
                    var dayRegVsFiltered = new List<Reg_V>();
                    foreach (Reg_V dayRegV in dayRegVs)
                    {
                        // inizializzazione della variabile che indica l'intenzione di processare la registrazione (di default la si processa)
                        bool isToProcess = true;

                        // se richiesto dai parametri dell'export non si trattano i viaggi di inizio/fine giornata nello stesso comune sotto i 10 km
                        if (doNotTreatStartEndTripUnderKm && dayRegV.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip)
                        {
                            // si sta trattando un viaggio di inizio/fine giornata, sotto i 10 Km nello stesso comune
                            // allora si segnala l'intenzione di processare il record in base al parametro
                            if (RepoManager.Reg_VRepo.IsTripOnSameMunicipality(dayRegV, dayRegVs)
                                && RepoManager.Reg_VRepo.IsTripOnStartEnd(dayRegV, dayRegVs)
                                && dayRegV.KM_Reg <= kmThreshold)
                                isToProcess = !doNotTreatStartEndTripUnderKm;
                        }

                        // se è stata indicata l'intenzione di processare il record
                        if (isToProcess)
                        {
                            Reg_V regVToAdd = null;

                            // inizializzazione della variabile che indica che la registrazione in processo è una registrazione ore trasformata in viaggio
                            bool wasTrip = false;

                            // in caso stia elaborando un viaggio e questo viaggio sia nello stesso comune ed inferiore ai km configurati,
                            // lo si tratta da ore lavorate e non da viaggio
                            if (dayRegV.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip)
                            {
                                if (dayRegV.KM_Reg < kmThreshold && RepoManager.Reg_VRepo.IsTripOnSameMunicipality(dayRegV, dayRegVs))
                                {
                                    dayRegV.Registrazione_Tipo_Reg = (int)RegTypeEnum.None;

                                    // se la registrazione era un viaggio poi passato a ore lavorate per sottokilometraggio
                                    // lo si segna in una variabile di modo da effettuare delle successive post elaborazioni
                                    wasTrip = true;

                                    dayRegV.WasTrip = true;
                                }
                            }

                            // si aggiunge all'elenco la riga corrente solamente se non è un viaggio sotto km oppure 
                            // oppure, se è un viaggio sotto km non si vogliono più viaggi sotto km
                            if (!wasTrip || !noMoreUnderKmTrips)
                            {
                                regVToAdd = dayRegV;

                                // la durata della registrazione è espressa in secondi
                                int startHour = Convert.ToInt32((new TimeSpan(dayRegV.Data_Ora_Fis_E.Hour, dayRegV.Data_Ora_Fis_E.Minute, 0)).TotalMinutes) * 60;
                                int endHour = dayRegV.Data_Ora_Fis_U != null ? Convert.ToInt32((new TimeSpan(dayRegV.Data_Ora_Fis_U.Value.Hour, dayRegV.Data_Ora_Fis_U.Value.Minute, 0)).TotalMinutes) * 60 : 0;

                                // calcolo i totali parziali per la gestione della riga (solo se non fine settimana, in questo caso va tutto trattato)
                                if (dayRegV.Data_Reg.Value.DayOfWeek != DayOfWeek.Sunday && dayRegV.Data_Reg.Value.DayOfWeek != DayOfWeek.Saturday)
                                    dayTotalWorkingHours += dayRegV.Registrazione_Tipo_Reg == (int)RegTypeEnum.None ? endHour - startHour : 0;

                                if (dayRegV.Registrazione_Tipo_Reg == (int)RegTypeEnum.None && String.IsNullOrEmpty(dayRegV.Motivazione_Reg_Cod)) // se la registrazione è un'ora normale (cioè non un viaggio senza motivazione)
                                {
                                    // se la durata totale del giorno è maggiore del numero di ore ordinarie configurate
                                    if (dayTotalWorkingHours > XmlToPerfettoConstants.OrdinaryHours)
                                    {
                                        if (dayRegV.WasTrip && doNotTreatTripUnderKmAsOvertime)
                                        {
                                            // si marca la registrazione per la non creazione
                                            regVToAdd = null;

                                            // si toglie dalla durata calcoata quella appena aggiunta
                                            dayTotalWorkingHours -= endHour - startHour;

                                            // si segnala che non si vogliono inserire altri viaggi trattati come ore lavorate
                                            noMoreUnderKmTrips = true;
                                        }
                                        else
                                        {
                                            // in caso non si stia processando un viaggio sotto kilometrato allora si tolgono gli elementi
                                            // viaggio già creati fino a esaurimento o rientro in ordinario
                                            if (dayRegVsFiltered.Any(regRow => regRow.WasTrip) && doNotTreatTripUnderKmAsOvertime)
                                            {
                                                // inizializzazione della lista di elementi da rimuovere dalla lista
                                                var regvRowsToRemove = new List<Reg_V>();

                                                // ciclo di elaborazione dei viaggi sotto kilometraggio già inseriti
                                                foreach (Reg_V regvRow in dayRegVsFiltered.Where(regRow => regRow.WasTrip).OrderByDescending(regRow => regRow.Data_Ora_Fis_E))
                                                {
                                                    // procedo a elaborare solamente se non sono già a posto con le ore
                                                    if (dayTotalWorkingHours > XmlToPerfettoConstants.OrdinaryHours)
                                                    {
                                                        // la durata della registrazione è espressa in secondi
                                                        startHour = Convert.ToInt32((new TimeSpan(regvRow.Data_Ora_Fis_E.Hour, regvRow.Data_Ora_Fis_E.Minute, 0)).TotalMinutes) * 60;
                                                        endHour = dayRegV.Data_Ora_Fis_U != null ? Convert.ToInt32((new TimeSpan(regvRow.Data_Ora_Fis_U.Value.Hour, regvRow.Data_Ora_Fis_U.Value.Minute, 0)).TotalMinutes) * 60 : 0;

                                                        // tolgo le ore della registrrazione corrente e la marco da cancellare
                                                        dayTotalWorkingHours -= endHour - startHour;
                                                        regvRowsToRemove.Add(regvRow);
                                                    }
                                                    else // se invece sono a posto smetto di ciclare, ho tolto il necessario
                                                        break;
                                                }

                                                // al termine dell'elaborazione, se ci sono da eliminare delle righe con le ore provenienti da viaggi sotto kilometrati
                                                // lo effettuo
                                                if (regvRowsToRemove.Any())
                                                    regvRowsToRemove.ForEach(regvRow => dayRegVsFiltered.Remove(regvRow));
                                            }
                                        }
                                    }
                                }
                            }

                            if (regVToAdd != null)
                                dayRegVsFiltered.Add(regVToAdd);
                        }
                    }

                    // per ogni reg_v da scrivere (ordinate per data/ora entrata)
                    foreach (var regV in dayRegVsFiltered)
                    {
                        // scrivo la riga di reg_V:
                        // Giorno
                        CellInsertValue(worksheetName, 1, currentRow, outputDayValue, ExcelInsertTypeEnum.Content);

                        // Codice e descrizione cantiere
                        CellInsertValue(worksheetName, 2, currentRow, String.Format("{0} {1}", regV.Cant_Mnemonic, regV.Cant_Desc), ExcelInsertTypeEnum.Content);

                        // Tipo di registrazione 
                        if (regV.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip)
                            CellInsertValue(worksheetName, 3, currentRow, "V", ExcelInsertTypeEnum.Content);
                        else if (regV.Registrazione_Tipo_Reg == (int)RegTypeEnum.None) // in base al codice tipo motivazione si imposta una "O" per ore normali, "FP" per ferie e permessi e "MI" per malattie e infortunii
                            CellInsertValue(worksheetName, 3, currentRow, String.IsNullOrEmpty(regV.Motivazione_Reg_Cod) ? "O" : regV.Motivazione_Reg_Cod, ExcelInsertTypeEnum.Content);

                        // Ora di entrata
                        CellInsertValue(worksheetName, 4, currentRow, regV.Data_Ora_Fis_E.TimeOfDay, ExcelInsertTypeEnum.HhmmTime);

                        // Ora di uscita
                        CellInsertValue(worksheetName, 5, currentRow, regV.Data_Ora_Fis_U != null ? regV.Data_Ora_Fis_U.Value.TimeOfDay : new TimeSpan(0, 0, 0), ExcelInsertTypeEnum.HhmmTime);

                        // Durata della giornata
                        var dayDuration = CommonService.GetDateTimeFromMinutes(regV.Durata_Fis ?? 0).TimeOfDay;
                        CellInsertValue(worksheetName, 6, currentRow, dayDuration, ExcelInsertTypeEnum.HhmmTime);


                        // Viaggio giornaliero
                        if (regV.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip)
                        {
                            //durata totale del viaggio
                            var tripDuration = CommonService.GetDateTimeFromMinutes(regV.Durata_Fis ?? 0).TimeOfDay;
                            CellInsertValue(worksheetName, 13, currentRow, tripDuration, ExcelInsertTypeEnum.HhmmTime);
                        }

                        // al termine della salvo l'ultima riga scritta e incrementato l'indice di riga
                        lastDayRow = currentRow;
                        currentRow++;
                    }

                    // finita la scrittura del giorno si scrive (in formula) il totale della giornata e la formula di calcolo degli straordinari
                    var holidays = RepoManager.Tab_FestiviRepo.Find(hol => hol.Giorno_Tab_Festivi >= date.Date, true).ToList();

                    //calcolo della durata in caso di sabato e domenica, tutte le ore sono straordinarie
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    {
                        var dayTotalNotWorkingDayFormula = new StringBuilder("=");
                        for (int i = firstDayRow; i <= lastDayRow; i++)
                            dayTotalNotWorkingDayFormula.AppendFormat("{0}F{1}", dayTotalNotWorkingDayFormula.ToString() == "=" ? String.Empty : "+", i);
                        CellInsertValue(worksheetName, 8, lastDayRow, dayTotalNotWorkingDayFormula, ExcelInsertTypeEnum.Formula);
                        CellSetNumberFormat(worksheetName, 8, lastDayRow, "[h]:mm;@");
                    }
                    else if (holidays.All(hol => hol.Giorno_Tab_Festivi != date.Date))
                    {
                        // Totale del giorno senza tener conto dei viaggi
                        var dayTotalFormula = new StringBuilder("=");
                        for (int i = firstDayRow; i <= lastDayRow; i++)
                            dayTotalFormula.AppendFormat("{0} IF(C{1} =\"O\", F{1}, 0)", dayTotalFormula.ToString() == "=" ? String.Empty : "+", i);
                        CellInsertValue(worksheetName, 15, lastDayRow, dayTotalFormula, ExcelInsertTypeEnum.Formula);
                        CellSetNumberFormat(worksheetName, 15, lastDayRow, "[h]:mm;@");

                        // Gestione ferie e permessi
                        var dayTotalVacation = new StringBuilder("=");
                        for (int i = firstDayRow; i <= lastDayRow; i++)
                            dayTotalVacation.AppendFormat("{0} IF(C{1} =\"FP\", F{1}, 0)", dayTotalVacation.ToString() == "=" ? String.Empty : "+", i);
                        CellInsertValue(worksheetName, 9, lastDayRow, dayTotalVacation, ExcelInsertTypeEnum.Formula);
                        CellSetNumberFormat(worksheetName, 9, lastDayRow, "[h]:mm;@");

                        // Gestione malattie
                        var dayTotalSick = new StringBuilder("=");
                        for (int i = firstDayRow; i <= lastDayRow; i++)
                            dayTotalSick.AppendFormat("{0} IF(C{1} =\"M\", F{1}, 0)", dayTotalSick.ToString() == "=" ? String.Empty : "+", i);
                        CellInsertValue(worksheetName, 10, lastDayRow, dayTotalSick, ExcelInsertTypeEnum.Formula);
                        CellSetNumberFormat(worksheetName, 10, lastDayRow, "[h]:mm;@");

                        // Gestione infortunii e congedi
                        var dayTotalInjury = new StringBuilder("=");
                        for (int i = firstDayRow; i <= lastDayRow; i++)
                            dayTotalInjury.AppendFormat("{0} IF(C{1} =\"IC\", F{1}, 0)", dayTotalInjury.ToString() == "=" ? String.Empty : "+", i);
                        CellInsertValue(worksheetName, 11, lastDayRow, dayTotalInjury, ExcelInsertTypeEnum.Formula);
                        CellSetNumberFormat(worksheetName, 11, lastDayRow, "[h]:mm;@");

                        // delta della giornata
                        string dayDeltaFormula = String.Format("=IF((HOUR(O{0})*3600 + MINUTE(O{0})*60 + SECOND(O{0}))-(HOUR(H7)*3600 + MINUTE(H7)*60 + SECOND(H7))>(HOUR(O9)*3600 + MINUTE(O9)*60 + SECOND(O9)),O{0}-H7,O9)", lastDayRow);
                        CellInsertValue(worksheetName, 8, lastDayRow, dayDeltaFormula, ExcelInsertTypeEnum.Formula);
                        CellSetNumberFormat(worksheetName, 8, lastDayRow, "[h]:mm;@");

                        // Totale dei viaggi del giorno
                        var dayTotalTripFormula = new StringBuilder("=");
                        for (int i = firstDayRow; i <= lastDayRow; i++)
                            dayTotalTripFormula.AppendFormat("{0}M{1}", dayTotalTripFormula.ToString() == "=" ? String.Empty : "+", i);
                        CellInsertValue(worksheetName, 14, lastDayRow, dayTotalTripFormula, ExcelInsertTypeEnum.Formula);
                        CellSetNumberFormat(worksheetName, 14, lastDayRow, "[h]:mm;@");

                        // DOPO LE ORE 8 85%
                        string overTime = String.Format("=IF((HOUR(N{0})*3600 + MINUTE(N{0})*60 + SECOND(N{0}))+(HOUR(O{0})*3600 + MINUTE(O{0})*60 + SECOND(O{0}))-(HOUR(H7)*3600 + MINUTE(H7)*60 + SECOND(H7))>(HOUR(O9)*3600 + MINUTE(O9)*60 + SECOND(O9)),(N{0}+O{0})-H7-H{0}, O9)", lastDayRow);
                        CellInsertValue(worksheetName, 12, lastDayRow, overTime, ExcelInsertTypeEnum.Formula);
                        CellSetNumberFormat(worksheetName, 12, lastDayRow, "[h]:mm;@");

                        // Totale del giorno tenedo conto dei viaggi
                        string ordinaryHourTime = String.Format("=IF(((HOUR(N{0})*3600 + MINUTE(N{0})*60 + SECOND(N{0}))+(HOUR(O{0})*3600 + MINUTE(O{0})*60 + SECOND(O{0})))>=(HOUR(H7)*3600 + MINUTE(H7)*60 + SECOND(H7)),H7,N{0}+O{0})", lastDayRow);
                        CellInsertValue(worksheetName, 7, lastDayRow, ordinaryHourTime, ExcelInsertTypeEnum.Formula);
                        CellSetNumberFormat(worksheetName, 7, lastDayRow, "[h]:mm;@");


                    }

                    // inserisco i bordi delle celle appena scritte
                    RangeSetBorders(worksheetName, 1, firstDayRow, 15, lastDayRow, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin,
                        Color.Black, ExcelBorderStyle.Thin);

                }
                else // non ci sono delle reg_v da stampare allora stampo una riga vuota (con solo l'indicazione del giorno)
                {
                    CellInsertValue(worksheetName, 1, currentRow, outputDayValue, ExcelInsertTypeEnum.Content);

                    // inserisco i bordi delle celle appena scritte
                    RangeSetBorders(worksheetName, 1, currentRow, 15, currentRow, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin,
                        Color.Black, ExcelBorderStyle.Thin);

                    // incremento del contatore di riga
                    currentRow++;
                }
            }

            // inserimento dei totali in coda all'export
            var formulaString = new StringBuilder("=");
            for (int i = ModelFirstDetailRow; i < currentRow; i++)
            {
                if (formulaString.ToString() != "=")
                    formulaString.Append('+');
                formulaString.AppendFormat("{0}{1}", "{0}", i);
            }
            CellInsertValue(worksheetName, 7, currentRow, String.Format(formulaString.ToString(), "G"), ExcelInsertTypeEnum.Formula);
            CellSetNumberFormat(worksheetName, 7, currentRow, "[h]:mm;@");
            CellInsertValue(worksheetName, 8, currentRow, String.Format(formulaString.ToString(), "H"), ExcelInsertTypeEnum.Formula);
            CellSetNumberFormat(worksheetName, 8, currentRow, "[h]:mm;@");
            CellInsertValue(worksheetName, 9, currentRow, String.Format(formulaString.ToString(), "I"), ExcelInsertTypeEnum.Formula);
            CellSetNumberFormat(worksheetName, 9, currentRow, "[h]:mm;@");
            CellInsertValue(worksheetName, 10, currentRow, String.Format(formulaString.ToString(), "J"), ExcelInsertTypeEnum.Formula);
            CellSetNumberFormat(worksheetName, 10, currentRow, "[h]:mm;@");
            CellInsertValue(worksheetName, 11, currentRow, String.Format(formulaString.ToString(), "K"), ExcelInsertTypeEnum.Formula);
            CellSetNumberFormat(worksheetName, 11, currentRow, "[h]:mm;@");
            CellInsertValue(worksheetName, 12, currentRow, String.Format(formulaString.ToString(), "L"), ExcelInsertTypeEnum.Formula);
            CellSetNumberFormat(worksheetName, 12, currentRow, "[h]:mm;@");
        }

        #endregion

    }
}
