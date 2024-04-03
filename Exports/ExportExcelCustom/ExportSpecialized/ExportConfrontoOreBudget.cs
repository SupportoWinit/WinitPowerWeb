using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Windows.Forms;
using BingMapsRESTToolkit;
using Business;
using Business.BusinessExtension;
using Business.Repository;
using Business.Repository.Custom;
using Common;
using DevExpress.XtraSpreadsheet.Layout;
using Domain;
using OfficeOpenXml.Style;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    public class ExportConfrontoOreBudget : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
    {

        #region Private Constants

        /// <summary>
        /// Il nome del foglio che fa da modello nel file excel originale.
        /// </summary>
        private const string ModelWorksheetName = "Sheet1";

        /// <summary>
        /// L'indice della prima riga della testata della tabella con i dati.
        /// </summary>
        private const int FirstWorksheetRow = 10;

        /// <summary>
        /// Il formato da applicare alle colonne che visualizzano la data al'interno dell'export
        /// </summary>
        private const string DateNumberFormat = "[$-410]d-mmm-yyyy;@";

        /// <summary>
        /// Il number format da applicare alle celle excel che visualizzano il numero di ore/minuti negativo
        /// </summary>
        private const string NegativeTimeFormat = "-[h]:mm";

        /// <summary>
        /// Il colore utilizzato per le colonne delta anomale
        /// </summary>
        private readonly Color _anomalyDeltaColor = Color.Red;

        /// <summary>
        /// Il colore utilizzato per colonne delta normali
        /// </summary>
        private readonly Color _normalDeltaColor = Color.Green;

        /// <summary>
        /// Il colore utilizzato per colonne normali
        private readonly Color _normalPrevisionalHour = Color.Black;

        #endregion

        #region Private Fields

        /// <summary>
        /// Indica lo stato della personalizzazione che segnala se visualizzare o meno i colori di sfondo da codice all'interno dell'export corrente;
        /// Da non leggere direttamente: per utilizzare il dato recuperare la proprietà <see cref="ShowBackgroundColorCustomization"/>.
        /// </summary>
        private CustomExportsBackgroundColorEnum? _showBackgroundColorCustomization;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Recupera lo stato della personalizzazione che segnala se visualizzare o meno i colori di sfondo da codice all'interno dell'export corrente.
        /// </summary>
        /// <value>
        /// Lo stato della personalizzazione che segnala se visualizzare o meno i colori di sfondo da codice all'interno dell'export corrente.
        /// </value>
        protected CustomExportsBackgroundColorEnum ShowBackgroundColorCustomization
        {
            get
            {
                if (!_showBackgroundColorCustomization.HasValue)
                    _showBackgroundColorCustomization = (CustomExportsBackgroundColorEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomExportsBackgroundColorEnum);

                return (CustomExportsBackgroundColorEnum)_showBackgroundColorCustomization;
            }
        }

        #endregion

        #region Public Properties

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
        /// Recupera o imposta lo specifico calcolo da utilizzare (figurative/fisiche); utilizato solo se <see cref="IExportExcelCustom{T}.UseCalculationType"/> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Lo specifico calcolo da utilizzare (figurative/fisiche); utilizato solo se <see cref="IExportExcelCustom{T}.UseCalculationType"/> è valorizzato a <c>true</c>.
        /// </value>
        public ExportRegVCalculationTypeEnum CalculationType { get; set; }

        /// <summary>
        /// Recupera o imposta il tipo di calcolo specifico delle ore (solo durata/con entrata uscita); utilizzato solo se <see cref="IExportExcelCustom{T}.UseHourType"/> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il tipo di calcolo specifico delle ore (solo durata/con entrata uscita); utilizzato solo se <see cref="IExportExcelCustom{T}.UseHourType"/> è valorizzato a <c>true</c>.
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
        /// Il tolale del delta di confronto per l'entità
        /// </summary>
        /// <value>
        /// Il tolale del delta di confronto per l'entità.
        /// </value>
        public int entityTotalConfrontationDuration { get; set; }

        /// <summary>
        /// Il tolale delle ore aggiuntive da aggiungere al mese
        /// </summary>
        /// <value>
        /// Il tolale delle ore aggiuntive da aggiungere al mese.
        /// </value>
        public int oreAggiuntive { get; set; }


        /// <summary>
        /// Il tolale delle durate mensieli delle ore effettive
        /// </summary>
        /// <value>
        /// Il tolale delle durate mensieli delle ore effettive
        /// </value>
        public int entityTotalEffectiveDuration { get; set; }

        /// <summary>
        /// Il tolale del delta di confronto per l'entità
        /// </summary>
        /// <value>
        /// Il tolale del delta di confronto per l'entità.
        /// </value>
        public int entityTotalPrevisionalDuration { get; set; }

        /// <summary>
        /// Il tolale del delta di confronto entrata per l'entità
        /// </summary>
        /// <value>
        /// Il tolale del delta di confronto entrata per l'entità.
        /// </value>
        public int entityTotalConfrontationE { get; set; }

        /// <summary>
        /// Il tolale del delta di confronto uscita per l'entità
        /// </summary>
        /// <value>
        /// Il tolale del delta di confronto uscita per l'entità.
        /// </value>
        public int entityTotalConfrontationU { get; set; }

        /// <summary>
        /// Rappresenta il numero di interventi totali
        /// </summary>
        /// <value>
        /// Il numero interventi totali.
        /// </value>
        public int numeroInterventiTotali { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        public override void LaunchExport(IQueryable<Reg_V> entitiesToExport)
        {
            // si procede all'elaborazione solamente se sono presenti dei dati
            if (entitiesToExport.Any())
            {
                // dalle reg_v da processare sono estratte tutte le registrazioni raggruppate per entitià di riferimento (collaboratore/cantiere)
                // in un dizionario
                Dictionary<int, List<Reg_V>> groupedRegVs = GetRegVsGroupedByEntity(entitiesToExport);

                // se il dizionario ha dei dati presenti allora si procede alla generazione del folgio excel
                if (groupedRegVs.Any())
                {
                    // inizializzazione del foglio excel da processare
                    ExcelWorkbookGenerateNew(ExcelModelFilePath);

                    // per ogni entità da processare
                    foreach (KeyValuePair<int, List<Reg_V>> groupedRegV in groupedRegVs)
                    {
                        entityTotalConfrontationDuration =
                            entityTotalConfrontationE =
                            entityTotalConfrontationU =
                            entityTotalEffectiveDuration =
                            entityTotalPrevisionalDuration =
                            numeroInterventiTotali = 0;

                        // si elabora il raggruppamento solamente se sono presenti registrazioni
                        if (groupedRegV.Value.Any())
                        {
                            // calcolo per il mese in elaborazione il timesheet dell'entità da processare
                            List<TimesheetModuleItem> entityMonthTimesheets = GetEntityMonthTimesheet(groupedRegV.Key);

                            Col collaboratore = null;
                            Cant cantiere = null;

                            // calcolo del nome del foglio di lavoro da utilizzare
                            string currentWorksheetName = "";
                            string nome = "";
                            if (ModelFirstEntity == ExcelModelSelectionTypeEnum.Cant)
                            {
                                collaboratore = RepoManager.ColRepo.GetAll().Where(c => c.Col_Id == groupedRegV.Value.First().Col_Id.Value).First();
                                cantiere = RepoManager.CantRepo.GetAll().Where(c => c.Cant_Id == groupedRegV.Value.First().Cant_Id.Value).First();
                                if (cantiere.Codice_Cantiere.Length < 31)
                                {
                                    currentWorksheetName = cantiere.Codice_Cantiere;
                                }
                                else {
                                    nome = cantiere.Descrizione_Can.Substring(0,31);
                                    currentWorksheetName = nome;
                                }
                            }
                            else
                            {
                                collaboratore = RepoManager.ColRepo.GetAll().Where(c => c.Codice_Collaboratore == GetWorksheetName(groupedRegV.Value.First())).First();
                                cantiere = RepoManager.CantRepo.GetAll().Where(c => c.Codice_Cantiere == GetWorksheetName(groupedRegV.Value.First())).First();
                                if (collaboratore.CognomeNome_Col.Length < 31)
                                {
                                    currentWorksheetName = collaboratore.CognomeNome_Col;
                                }
                                else {
                                    nome = collaboratore.CognomeNome_Col.Substring(0, 31);
                                    currentWorksheetName = nome;
                                }
                            }
                            

                            // si recupera la descrizione dell'entità che si sta processando
                            string entityDescription = GetEntityDescription(groupedRegV.Value.First());

                            //inizializzazione delle ore aggiuntive
                            oreAggiuntive = 0;

                            // RepoManager.Tab_OrariRepo.Find(t=>t.Cant_Id==)

                            // copia del worksheet modello in un nuovo foglio di lavoro con il nome precedentemente calcolato
                            WorksheetCopy(ModelWorksheetName, currentWorksheetName);

                            // scrittura dell'header del nuovo worksheet
                            WriteWorksheetHeader(currentWorksheetName, entityDescription);

                            // si cicla per ogni giorno del mese da processare
                            DateTime firstMonthDate = CommonService.GetFirstMonthDay(ExportPeriod);
                            DateTime lastMonthDate = CommonService.GetLastMonthDay(ExportPeriod);
                            int rowIndex = FirstWorksheetRow;

                            if (ModelFirstEntity == ExcelModelSelectionTypeEnum.Cant)
                                oreAggiuntive = RepoManager.Tab_OrariRepo.GetMonthlyPlanDuration(firstMonthDate, groupedRegV.Key, "Can");


                            foreach (DateTime monthDay in CommonService.EachDay(firstMonthDate, lastMonthDate))
                            {
                                // inizializzazione del piano di dettaglio
                                var detailPlan = new List<Tuple<int, TimeSpan, TimeSpan>>();


                                // salvo la data che si sta processando in una variabile così da evitare segnalazione di resharper
                                // per compatibilità tra compilatori
                                DateTime currentMonthDay = monthDay;

                                // se è richiesto di valutare l'entrata e l'uscita  o entrabi allora si calcola anche il piano di dettaglio
                                if (UseHourType && (HourType == ExportRegVHourTypeEnum.Both || HourType == ExportRegVHourTypeEnum.Eu))
                                    detailPlan = RepoManager.Tab_OrariRepo.GetDayPlanDetail(currentMonthDay, groupedRegV.Key, ModelFirstEntity == ExcelModelSelectionTypeEnum.Col ? "Col" : "Can");

                                // si procede ad inserire una nuova tabella solamente se per il giorno in questione sono presenti delle registrazioni
                                // o degli orari
                                List<Reg_V> dayRegVs = groupedRegV.Value.Where(regv => regv.Data_Reg == currentMonthDay).ToList();

                                bool elaborateDay = false;
                                switch (HourType)
                                {
                                    case ExportRegVHourTypeEnum.OnlyDuration:
                                        elaborateDay = dayRegVs.Any() || entityMonthTimesheets.Any(tsm => tsm.GetDayMinutes(monthDay.Day) != 0);
                                        break;
                                    case ExportRegVHourTypeEnum.Eu:
                                    case ExportRegVHourTypeEnum.Both:
                                        elaborateDay = dayRegVs.Any() || detailPlan.Any();
                                        break;
                                }

                                // se è richiesta l'elaborazione del giorno
                                if (elaborateDay)
                                {
                                    // scrittura dell'intestazione della tabella del foglio
                                    int originalHeaderRowIndex = rowIndex;
                                    //ritorna la nuova riga per andare avanti con la scrittura dell'export
                                    rowIndex = WriteWorksheetTableHeader(currentWorksheetName, rowIndex);

                                    int oldRowIndex = rowIndex;
                                    // scrittura dei dati delle registrazioni nel foglio
                                    rowIndex = WriteWorksheetRegVs(currentWorksheetName, monthDay, dayRegVs, rowIndex, entityMonthTimesheets, detailPlan);

                                    // se sono state aggiunge delle righe allora
                                    // una volta scritta la riga procedo ad aggiungere una riga di pausa per la scrittura
                                    // altrimenti in caso contrario al prossimo giro si deve riscrivere l'header a partire dalla posizione originale,
                                    // e per questo viene ripristinata
                                    rowIndex = oldRowIndex != rowIndex ? ++rowIndex : originalHeaderRowIndex;
                                }
                            }

                            rowIndex = WriteWorksheetMonthlyTotal(currentWorksheetName, rowIndex);

                        }
                    }

                    // cancellazione del foglio modello utilizzato per la costruzione del foglio excel
                    WorksheetDelete(ModelWorksheetName);

                    // esporto quanto generato (in caso di assenza reg_v il file modello) sulla risposta del browser
                    ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

                    // una volta salvato l'oggetto excel viene cancellato dalla memoria
                    ExcelWorkbookDispose();
                }
            }
            //else {
            //    MessageBox.Show("Nessuna timbratura presente");
            //}
        }

        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="selectedColIds">L'elenco degli id collaboratore selezionati per l'export.</param>
        /// <param name="selectedCantIds">L'elenco degli id cantiere selezionati per l'export.</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds, IEnumerable<int> selectedCliIds)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Recupera e restituisce le registrazioni da processare raggruppate per l'id dell'entità di riferimento
        /// (collaboratore o cantiere) in base a quanto previsto dal modello. Le registrazioni ritornate sono solo quelle in stato abbinato.
        /// </summary>
        /// <param name="regVsToGroup">Le registrazioni da raggruppare.</param>
        /// <returns>
        /// Un dizionario la cui chiave è l'id dell'entità di riferimento e come valore ha una lista con le corrispondenti registrazioni.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private Dictionary<int, List<Reg_V>> GetRegVsGroupedByEntity(IQueryable<Reg_V> regVsToGroup)
        {
            // inzializzazione del ritorno del metodo
            Dictionary<int, List<Reg_V>> groupedRegVs;

            switch (ModelFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Col:
                    groupedRegVs = regVsToGroup.Where(regv => regv.Col_Id.HasValue && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass).GroupBy(regv => regv.Col_Id)
                        .ToDictionary(group => Convert.ToInt32(group.Key), group => group.ToList());
                    break;
                case ExcelModelSelectionTypeEnum.Cant:
                    groupedRegVs = regVsToGroup.Where(regv => regv.Cant_Id.HasValue && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass).GroupBy(regv => regv.Cant_Id)
                        .ToDictionary(group => Convert.ToInt32(group.Key), group => group.ToList());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(ExcelModelFilePath);
            }


            // ritorno del valore calcolato dal metodo
            return groupedRegVs;
        }

        /// <summary>
        /// Recupera la descrizione dell'entità che si sta processando alla registrazione passata come parametro.
        /// </summary>
        /// <param name="entityRegV">La registrazione da cui estrapolare i dati.</param>
        /// <returns>
        /// La descrizione dell'entità in elaborazione per la specifica registrazione
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private string GetEntityDescription(Reg_V entityRegV)
        {
            // inizializzazione del valore di ritorno del metodo
            string entityDescription;

            switch (ModelFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Col:
                    entityDescription = entityRegV.Col_Desc;
                    break;
                case ExcelModelSelectionTypeEnum.Cant:
                    entityDescription = entityRegV.Cant_Desc;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            // ritorno del valore calcolato dal metodo
            return entityDescription;
        }

        /// <summary>
        /// Recupera la descrizione dell'entità opposta a quella da processare (cantiere o collaboratore) recuperandola da una specifica registrazione.
        /// </summary>
        /// <param name="entityRegV">La registrazione da cui estrarre il dato.</param>
        /// <returns>
        /// La descrizione dell'entità opposta a quella in elaborazione (cantiere o collaboratore) per la specifica registrazione.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private string GetOtherEntityDescription(Reg_V entityRegV)
        {
            // inizializzazione del valore di ritorno del metodo
            string otherEntityDescription;

            switch (ModelFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Col:
                    otherEntityDescription = entityRegV.Cant_Desc;
                    break;
                case ExcelModelSelectionTypeEnum.Cant:
                    otherEntityDescription = entityRegV.Col_Desc;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // ritorno del valore calcolato dal metodo
            return otherEntityDescription;
        }

        /// <summary>
        /// Recupera la descrizione dell'entità opposta a quella da processare (cantiere o collaboratore) recuperandola da una specifica registrazione.
        /// </summary>
        /// <param name="entityTimesheet">L'orario da cui estrarre il dato.</param>
        /// <returns>La descrizione dell'entità opposta a quella in elaborazione (cantiere o collaboratore) per lo specifico orario.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private string GetOtherEntityDescription(TimesheetModuleItem entityTimesheet)
        {
            // inizializzazione del valore di ritorno del metodo
            string otherEntityDescription;

            switch (ModelFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Col:
                    otherEntityDescription = entityTimesheet.CantDesc;
                    break;
                case ExcelModelSelectionTypeEnum.Cant:
                    otherEntityDescription = entityTimesheet.ColDesc;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // ritorno del valore calcolato dal metodo
            return otherEntityDescription;
        }

        /// <summary>
        /// Recupera la descrizione dell'entità opposta a quella da processare (cantiere o collaboratore) recuperandola a partire dal suo id.
        /// </summary>
        /// <param name="otherEntityId">L'identificativo univoco dell'entità da ricercare.</param>
        /// <returns>
        /// La descrizione dell'entità opposta a quella in elaborazione (cantiere o collaboratore) corrispondente allo specifico id
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private string GetOtherEntityDescription(int otherEntityId)
        {
            string returnDescription = String.Empty;

            // in base all'entità di riferimento recupero il dato dal repository
            switch (ModelFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Col:
                    Cant currentCant = RepoManager.CantRepo.FirstOrDefault(cnt => cnt.Cant_Id == otherEntityId);
                    if (currentCant != default(Cant))
                        returnDescription = currentCant.Descrizione_Can;
                    break;
                case ExcelModelSelectionTypeEnum.Cant:
                    Col currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == otherEntityId);
                    if (currentCol != default(Col))
                        returnDescription = currentCol.CognomeNome_Col;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return returnDescription;
        }

        /// <summary>
        /// Recupera l'id dell'entità opposta a quella da processare (cantiere o collaboratore) recuperandola da una specifica registrazione.
        /// </summary>
        /// <param name="entitRegV">La registrazione da cui estrarre il dato.</param>
        /// <returns>
        /// L'id dell'entità opposta a quella in elaborazione (cantiere o collaboratore) per la specifica registrazione.
        /// Viene ritornato 0 in caso l'id non sia presente.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private int GetOtherEntityId(Reg_V entityRegV)
        {
            // inizializzazione del valore di ritorno del metodo
            int otherEntityId;

            switch (ModelFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Col:
                    otherEntityId = entityRegV.Cant_Id ?? 0;
                    break;
                case ExcelModelSelectionTypeEnum.Cant:
                    otherEntityId = entityRegV.Col_Id ?? 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // ritorno del valore calcolato dal metodo
            return otherEntityId;
        }

        /// <summary>
        /// Recupera l'id dell'entità opposta a quella da processare (cantiere o collaboratore) recuperandola da uno specifico orario.
        /// </summary>
        /// <param name="entityTimesheet">L'orario da cui estrarre i dato.</param>
        /// <returns>L'id dell'entità opposta a quella in elaborazione (cantiere o collaboratore) per la specifica registrazione.
        /// Viene ritornato 0 in caso l'id non sia presente.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private int GetOtherEntityId(TimesheetModuleItem entityTimesheet)
        {
            // inizializzazione del valore di ritorno del metodo
            int otherEntityId;

            switch (ModelFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Col:
                    otherEntityId = entityTimesheet.CantId;
                    break;
                case ExcelModelSelectionTypeEnum.Cant:
                    otherEntityId = entityTimesheet.ColId;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // ritorno del valore calcolato dal metodo
            return otherEntityId;
        }

        /// <summary>
        /// Recupera il nome del foglio in cui si stanno scrivendo i dati della serie di cui la specifica registrazione fa parte.
        /// </summary>
        /// <param name="nameRegV">La registrazione da cui estrarre il nome del foglio di lavoro.</param>
        /// <returns>La stringa che indica il nome del foglio di lavoro</returns>
        private string GetWorksheetName(Reg_V nameRegV)
        {
            string worksheetName;

            switch (ModelFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Col:
                    worksheetName = nameRegV.Col_Mnemonic;
                    break;
                case ExcelModelSelectionTypeEnum.Cant:
                    worksheetName = nameRegV.Cant_Mnemonic;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // ritorno del valore calcolato dal metodo (eliminando tutti i caratterni non ammessi)
            return worksheetName.Replace('\\', '-').Replace('/', '-');
        }

        /// <summary>
        /// Scrive la testata del foglio specificato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="entityDescription">La descrizione dell'entità che si sta processando.</param>
        private void WriteWorksheetHeader(string worksheetName, string entityDescription)
        {
            // inserimento della descrizione dell'entità del foglio
            CellInsertValue(worksheetName, 3, 8, entityDescription, ExcelInsertTypeEnum.Content);

            // inserimento del periodo del foglio (con il mese in formato testuale)
            CellInsertValue(worksheetName, 3, 9, ExportPeriod.ToString("MMMM yyyy", PowerWebContext.Current.UserCultureInfo), ExcelInsertTypeEnum.Content);

        }

        /// <summary>
        /// Scrive l'header della tabella dei dati per il worksheet specificato a partire dalla posizione specificata.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet in cui scrivere.</param>
        /// <param name="rowIndex">La posizione di partenza di scrittura del table header</param>
        /// <returns>Il nuovo row index che corrisponde alla prima posizione utile per l'inserimento dei dati del giorno</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private int WriteWorksheetTableHeader(string worksheetName, int rowIndex)
        {
            // calcolo della prima e seconda riga dell'header della tabella
            int firstTableHeaderRow = rowIndex + 1;
            int secondTableHeaderRow = rowIndex + 2;
                     

                // altra entità di riferimento timbratura
                string otherEntityTitle = String.Empty;
                switch (ModelFirstEntity)
                {
                    case ExcelModelSelectionTypeEnum.Col:
                        otherEntityTitle = BusinessService.GetLocalizedString(PowerWebResources.STR_CANTIERE).ToUpper();
                        break;
                    case ExcelModelSelectionTypeEnum.Cant:
                        otherEntityTitle = BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE).ToUpper();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            if(UseExportDetail)
                CellInsertValue(worksheetName, 2, secondTableHeaderRow, otherEntityTitle, ExcelInsertTypeEnum.Content);

                // calcolo delle colonne di esecuzione, confronto, previsione
                int? confrontationEColumn = null;
                int? confrontationUColumn = null;
                int? confrontationDurationColumn = null;
                int? previsionalDurationColumn = null;
                int? previsionalEColumn = null;
                int? previsionalUColumn = null;
                int? previsionalEntityDesColumn = null;
                int? executionEColumn = null;
                int? executionUColumn = null;
                int? executionDurationColumn = null;
                int? numeroInterventiColum = null;
                CalculateExportColumns(ref executionDurationColumn, ref previsionalDurationColumn, ref confrontationDurationColumn, ref executionEColumn, ref executionUColumn,
                    ref previsionalEColumn, ref previsionalUColumn, ref previsionalEntityDesColumn, ref confrontationEColumn, ref confrontationUColumn, ref numeroInterventiColum);

                // valori effettivi timbratura
                CellInsertValue(worksheetName, 3, firstTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_ESEGUITO).ToUpper(), ExcelInsertTypeEnum.Content);
                CellInsertValue(worksheetName, 3, secondTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_DATA).ToUpper(), ExcelInsertTypeEnum.Content);
                if (executionEColumn != null)
                    CellInsertValue(worksheetName, executionEColumn.Value, secondTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_ORA_INIZIO).ToUpper(), ExcelInsertTypeEnum.Content);
                if (executionUColumn != null)
                    CellInsertValue(worksheetName, executionUColumn.Value, secondTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_ORA_FINE).ToUpper(), ExcelInsertTypeEnum.Content);
                if (executionDurationColumn != null)
                    CellInsertValue(worksheetName, executionDurationColumn.Value, secondTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_DURATA).ToUpper(), ExcelInsertTypeEnum.Content);
                int lastExecutionColumn = 4;
                if (executionDurationColumn != null)
                    lastExecutionColumn = executionDurationColumn.Value;
                else if (executionUColumn != null)
                    lastExecutionColumn = executionUColumn.Value;
                else if (executionEColumn != null)
                    lastExecutionColumn = executionEColumn.Value;
                RangeUnion(worksheetName, 3, firstTableHeaderRow, lastExecutionColumn, firstTableHeaderRow);
                RangeSetTextHorizontalAlignment(worksheetName, 3, firstTableHeaderRow, lastExecutionColumn, firstTableHeaderRow, ExcelHorizontalAlignment.Center);

                // valori previsti di timbratura
                int firstPrevisionalColumn = lastExecutionColumn + 1;
                int lastPrevisionalColumn = firstPrevisionalColumn;
                if (previsionalEntityDesColumn != null && UseExportDetail )
                    lastPrevisionalColumn = previsionalEntityDesColumn.Value;
                else if (previsionalDurationColumn != null)
                    lastPrevisionalColumn = previsionalDurationColumn.Value;
                else if (previsionalUColumn != null)
                    lastPrevisionalColumn = previsionalUColumn.Value;
                else if (previsionalEColumn != null)
                    lastPrevisionalColumn = previsionalEColumn.Value;

                CellInsertValue(worksheetName, firstPrevisionalColumn, firstTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_PREVISTO).ToUpper(), ExcelInsertTypeEnum.Content);
                if (previsionalEColumn != null)
                    CellInsertValue(worksheetName, previsionalEColumn.Value, secondTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_ORA_INIZIO).ToUpper(), ExcelInsertTypeEnum.Content);
                if (previsionalUColumn != null)
                    CellInsertValue(worksheetName, previsionalUColumn.Value, secondTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_ORA_FINE).ToUpper(), ExcelInsertTypeEnum.Content);
                if (previsionalDurationColumn != null)
                    CellInsertValue(worksheetName, previsionalDurationColumn.Value, secondTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_DURATA).ToUpper(), ExcelInsertTypeEnum.Content);


            if (previsionalEntityDesColumn != null && UseExportDetail)
                    CellInsertValue(worksheetName, previsionalEntityDesColumn.Value, secondTableHeaderRow, otherEntityTitle, ExcelInsertTypeEnum.Content);

                if (firstPrevisionalColumn != lastPrevisionalColumn)
                    RangeUnion(worksheetName, firstPrevisionalColumn, firstTableHeaderRow, lastPrevisionalColumn, firstTableHeaderRow);
                RangeSetTextHorizontalAlignment(worksheetName, firstPrevisionalColumn, firstTableHeaderRow, lastPrevisionalColumn, firstTableHeaderRow, ExcelHorizontalAlignment.Center);

                // inserimento della sezione di confronto (con inserimento o meno colonne in caso di sola durata/solo Eu o entrambi)
                int firstConfrontationColumn = lastPrevisionalColumn + 1;
                int lastConfrontationColumn = firstConfrontationColumn;
                if (confrontationDurationColumn != null)
                    lastConfrontationColumn = confrontationDurationColumn.Value;
                else if (confrontationUColumn != null)
                    lastConfrontationColumn = confrontationUColumn.Value;
                else if (confrontationEColumn != null)
                    lastConfrontationColumn = confrontationEColumn.Value;

                CellInsertValue(worksheetName, firstConfrontationColumn, firstTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_CONFRONTO).ToUpper(), ExcelInsertTypeEnum.Content);
                if (confrontationEColumn != null)
                    CellInsertValue(worksheetName, confrontationEColumn.Value, secondTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_DIFFERENZA_ENTRATA).ToUpper(), ExcelInsertTypeEnum.Content);
                if (confrontationUColumn != null)
                    CellInsertValue(worksheetName, confrontationUColumn.Value, secondTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_DIFFERENZA_USCITA).ToUpper(), ExcelInsertTypeEnum.Content);
                if (confrontationDurationColumn != null)
                    CellInsertValue(worksheetName, confrontationDurationColumn.Value, secondTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_DIFFERENZA_DURATA).ToUpper(), ExcelInsertTypeEnum.Content);
                if (firstConfrontationColumn != lastConfrontationColumn)
                    RangeUnion(worksheetName, firstConfrontationColumn, firstTableHeaderRow, lastConfrontationColumn, firstTableHeaderRow);
                RangeSetTextHorizontalAlignment(worksheetName, firstConfrontationColumn, firstTableHeaderRow, lastConfrontationColumn, firstTableHeaderRow, ExcelHorizontalAlignment.Center);

                //inserimento della sezione che riguarda il numero di interventi

                if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi))
                {
                    int firstNumeroInterventiColum = lastConfrontationColumn + 1;
                    int lastNumeroInterventiColum = firstNumeroInterventiColum;

                    if (numeroInterventiColum != null)
                        lastNumeroInterventiColum = numeroInterventiColum.Value;


                    // CellInsertValue(worksheetName, firstNumeroInterventiColum, firstTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_CONFRONTO).ToUpper(), ExcelInsertTypeEnum.Content);
                    if (numeroInterventiColum != null)
                        CellInsertValue(worksheetName, numeroInterventiColum.Value, secondTableHeaderRow, BusinessService.GetLocalizedString(PowerWebResources.STR_NUMERO_INTERVENTI).ToUpper(), ExcelInsertTypeEnum.Content);

                    if (firstNumeroInterventiColum != lastNumeroInterventiColum)
                        RangeUnion(worksheetName, firstNumeroInterventiColum, firstTableHeaderRow, lastNumeroInterventiColum, firstTableHeaderRow);
                    RangeSetTextHorizontalAlignment(worksheetName, firstNumeroInterventiColum, firstTableHeaderRow, lastNumeroInterventiColum, firstTableHeaderRow, ExcelHorizontalAlignment.Center);

                    // impostazione dei bordi della testata della tabella
                    RangeSetBorders(worksheetName, 3, secondTableHeaderRow, 3, secondTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);
                    RangeSetBorders(worksheetName, 4, secondTableHeaderRow, lastNumeroInterventiColum - 1, secondTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                    RangeSetBorders(worksheetName, lastNumeroInterventiColum, secondTableHeaderRow, lastNumeroInterventiColum, secondTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);

                    // impostazione dei colori della testata del numero di interventi
                    if (ShowBackgroundColorCustomization == CustomExportsBackgroundColorEnum.ShowColor)
                    {

                        RangeSetBackgroundColor(worksheetName, lastNumeroInterventiColum, secondTableHeaderRow, lastNumeroInterventiColum, secondTableHeaderRow, Color.Coral, ExcelFillStyle.Solid);
                    }
                }

            if (UseExportDetail)
            {
                // impostazione dei bordi della testata generale
                RangeSetBorders(worksheetName, 2, secondTableHeaderRow, 2, secondTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium);
                RangeSetBorders(worksheetName, 3, firstTableHeaderRow, lastExecutionColumn, firstTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);
                RangeSetBorders(worksheetName, firstPrevisionalColumn, firstTableHeaderRow, lastPrevisionalColumn, firstTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                RangeSetBorders(worksheetName, firstConfrontationColumn, firstTableHeaderRow, lastConfrontationColumn, firstTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
            }
            else
            {
                // impostazione dei bordi della testata generale
               // RangeSetBorders(worksheetName, 2, secondTableHeaderRow, 2, secondTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium);
                RangeSetBorders(worksheetName, 3, firstTableHeaderRow, lastExecutionColumn, firstTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium);
                RangeSetBorders(worksheetName, firstPrevisionalColumn, firstTableHeaderRow, lastPrevisionalColumn, firstTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                RangeSetBorders(worksheetName, firstConfrontationColumn, firstTableHeaderRow, lastConfrontationColumn, firstTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
            }
                // impostazione dei bordi della testata della tabella
                RangeSetBorders(worksheetName, 3, secondTableHeaderRow, 3, secondTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);
                RangeSetBorders(worksheetName, 4, secondTableHeaderRow, lastConfrontationColumn - 1, secondTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Escludi))
                    RangeSetBorders(worksheetName, lastConfrontationColumn, secondTableHeaderRow, lastConfrontationColumn, secondTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                else
                    RangeSetBorders(worksheetName, lastConfrontationColumn, secondTableHeaderRow, lastConfrontationColumn, secondTableHeaderRow, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                // impostazione dei colori della testata
                if (ShowBackgroundColorCustomization == CustomExportsBackgroundColorEnum.ShowColor)
                {
                    RangeSetBackgroundColor(worksheetName, 3, firstTableHeaderRow, lastExecutionColumn, secondTableHeaderRow, Color.CornflowerBlue, ExcelFillStyle.Solid);
                    RangeSetBackgroundColor(worksheetName, firstPrevisionalColumn, firstTableHeaderRow, lastPrevisionalColumn, secondTableHeaderRow, Color.FromArgb(108, 189, 116), ExcelFillStyle.Solid);
                    RangeSetBackgroundColor(worksheetName, firstConfrontationColumn, firstTableHeaderRow, lastConfrontationColumn, secondTableHeaderRow, Color.DarkOrange, ExcelFillStyle.Solid);
                }

                // impostazione della lunghezza delle colonne in base al tipo di ora
                if (UseHourType)
                {
                    switch (HourType)
                    {
                        case ExportRegVHourTypeEnum.OnlyDuration:
                            ColumnsSetWidth(worksheetName, 10, 10, 15);
                            break;
                        case ExportRegVHourTypeEnum.Eu:
                            ColumnsSetWidth(worksheetName, 10, 10, 14.5);
                            ColumnsSetWidth(worksheetName, 11, 11, 14.5);
                            break;
                        case ExportRegVHourTypeEnum.Both:
                            ColumnsSetWidth(worksheetName, 12, 12, 15);
                            ColumnsSetWidth(worksheetName, 13, 13, 14.5);
                            ColumnsSetWidth(worksheetName, 14, 14, 14.5);
                            break;
                    }
                }
           
            // ritorno del nuovo row index calcolato
            return secondTableHeaderRow + 1;


        }

        /// <summary>
        /// Scrive sullo specifico foglio di lavoro le registrazioni passate come parametro (di una specifica entità per uno specifico giorno).
        /// </summary>
        /// <param name="worksheetName">Il nome del foglio di lavoro in cui inserire le registrazioni.</param>
        /// <param name="dateToElaborate">La data che si sta processando.</param>
        /// <param name="worksheetDayRegVs">Le registrazioni da inserire nel foglio.</param>
        /// <param name="rowIndex">L'indice di riga da cui cominciare a scrivere.</param>
        /// <param name="dayTimesheets">L'elenco degli orari per l'entità in processo per il mese in elaborazione</param>
        /// <param name="dayDetailPlan">Il piano di dettaglio (se richiesto per l'analisi e presente) per il giorno in elaborazione.</param>
        /// <returns>Il nuovo valore dell'indice di riga da cui è possibile ricominciare a scrivere.</returns>
        private int WriteWorksheetRegVs(string worksheetName, DateTime dateToElaborate, IEnumerable<Reg_V> worksheetDayRegVs, int rowIndex, IList<TimesheetModuleItem> dayTimesheets,
            IEnumerable<Tuple<int, TimeSpan, TimeSpan>> dayDetailPlan)
        {
            if (UseExportDetail)
            {
                // se è richiesto un confronto di ore
                if (UseHourType)
                {
                    // calcolo degli oggetti di confronto per i dati attuali
                    var cObjects = new List<ConfrontationObject>();
                    switch (HourType)
                    {
                        case ExportRegVHourTypeEnum.OnlyDuration:
                            cObjects = CalculateOnlyDurationDayConfrontations(dateToElaborate, worksheetDayRegVs, dayTimesheets).ToList();
                            break;
                        case ExportRegVHourTypeEnum.Eu:
                            cObjects = CalculateEuDayConfrontations(dateToElaborate, worksheetDayRegVs, dayDetailPlan).ToList();
                            break;
                        case ExportRegVHourTypeEnum.Both:
                            cObjects = CalculateBothDayConfrontations(dateToElaborate, worksheetDayRegVs, dayDetailPlan).ToList();
                            break;
                    }

                    // se sono stati trovati degli oggetti di confronto da processare e l'elenco ha oggetti fuori tolleranza, si procede alla loro scrittura sul foglio excel
                    if (cObjects.Any() && HasOutOfTollerance(cObjects))
                    {
                        // calcolo delle colonne di esecuzione, confronto, previsione
                        int? confrontationEColumn = null;
                        int? confrontationUColumn = null;
                        int? confrontationDurationColumn = null;
                        int? previsionalDurationColumn = null;
                        int? previsionalEColumn = null;
                        int? previsionalUColumn = null;
                        int? previsionalEntityDesColumn = null;
                        int? executionEColumn = null;
                        int? executionUColumn = null;
                        int? executionDurationColumn = null;
                        int? numeroInterventiColumn = null;
                        CalculateExportColumns(ref executionDurationColumn, ref previsionalDurationColumn, ref confrontationDurationColumn, ref executionEColumn, ref executionUColumn,
                            ref previsionalEColumn, ref previsionalUColumn, ref previsionalEntityDesColumn, ref confrontationEColumn, ref confrontationUColumn, ref numeroInterventiColumn);

                        // salvataggio della linea di scrittura originale
                        int originalRowIndex = rowIndex;

                        // calcolo delle prime e ultime colonne di ogni sessione
                        const int firstExecutionColumn = 3;
                        int lastExecutionColumn = firstExecutionColumn;
                        if (executionDurationColumn.HasValue)
                            lastExecutionColumn = executionDurationColumn.Value;
                        else if (executionUColumn.HasValue)
                            lastExecutionColumn = executionUColumn.Value;
                        else if (executionEColumn.HasValue)
                            lastExecutionColumn = executionEColumn.Value;

                        int firstPrevisionalColumn = lastExecutionColumn + 1;
                        int lastPrevisionalColumn = firstPrevisionalColumn;

                        if (previsionalEntityDesColumn != null)
                            lastPrevisionalColumn = previsionalEntityDesColumn.Value;
                        else if (previsionalDurationColumn.HasValue)
                            lastPrevisionalColumn = previsionalDurationColumn.Value;
                        else if (previsionalUColumn.HasValue)
                            lastPrevisionalColumn = previsionalUColumn.Value;
                        else if (previsionalEColumn.HasValue)
                            lastPrevisionalColumn = previsionalEColumn.Value;

                        int firstConfrontationColumn = lastPrevisionalColumn + 1;
                        int lastConfrontationColumn = firstConfrontationColumn;
                        if (confrontationDurationColumn.HasValue)
                            lastConfrontationColumn = confrontationDurationColumn.Value;
                        else if (confrontationUColumn.HasValue)
                            lastConfrontationColumn = confrontationUColumn.Value;
                        else if (confrontationEColumn.HasValue)
                            lastConfrontationColumn = confrontationEColumn.Value;

                        if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi))
                        {
                            int firstNumeroInterventiColum = lastConfrontationColumn + 1;
                            int lastNumeroInterventiColum = firstNumeroInterventiColum;
                            if (numeroInterventiColumn.HasValue)
                                lastConfrontationColumn = numeroInterventiColumn.Value;
                        }


                        // inizializzazione dei contatori di totale durata eseguita, prevista e delta in giornoata
                        int totalExecutionDuration = 0;
                        int totalPrevisionalDuration = 0;
                        int totalConfrontationDuration = 0;
                        int totaleInterventi = 0;


                        int firstObjectRow = rowIndex;


                        // ciclo di elaborazione degli oggetti da scrivere
                        foreach (ConfrontationObject cObject in cObjects)
                        {
                            // scrittura dei dati degli oggetti di confronto con attenzione alla posizione delle colonne che non si vedono
                            CellInsertValue(worksheetName, 2, rowIndex, cObject.ExecutionEntityDes, ExcelInsertTypeEnum.Content);
                            CellInsertValue(worksheetName, 3, rowIndex, cObject.Date, ExcelInsertTypeEnum.Content);
                            CellSetNumberFormat(worksheetName, 3, rowIndex, DateNumberFormat);

                            if (executionEColumn != null)
                                CellInsertValue(worksheetName, executionEColumn.Value, rowIndex, cObject.ExecutionHourE, ExcelInsertTypeEnum.HhmmTime);
                            if (executionUColumn != null)
                                CellInsertValue(worksheetName, executionUColumn.Value, rowIndex, cObject.ExecutionHourU, ExcelInsertTypeEnum.HhmmTime);
                            if (executionDurationColumn != null)
                            {
                                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ScSExportBudget) == 1)
                                {
                                    var durataCentTime = CommonService.GetDoubleFromTimeSpan((TimeSpan)cObject.ExecutionDuration, true);
                                    CellInsertValue(worksheetName, executionDurationColumn.Value, rowIndex, durataCentTime, ExcelInsertTypeEnum.Content);
                                }
                                else
                                {
                                    CellInsertValue(worksheetName, executionDurationColumn.Value, rowIndex, cObject.ExecutionDuration, ExcelInsertTypeEnum.HhmmTime);
                                }
                                // se sto inserendo la durata di esecizione allora la sommo ai minuti totali della giornata
                                totalExecutionDuration += Convert.ToInt32(cObject.ExecutionDuration.HasValue ? cObject.ExecutionDuration.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes);
                            }
                            if (previsionalEColumn != null)
                                CellInsertValue(worksheetName, previsionalEColumn.Value, rowIndex, cObject.PrevisionalHourE, ExcelInsertTypeEnum.HhmmTime);
                            if (previsionalUColumn != null)
                                CellInsertValue(worksheetName, previsionalUColumn.Value, rowIndex, cObject.PrevisionalHourU, ExcelInsertTypeEnum.HhmmTime);
                            if (previsionalDurationColumn != null)
                            {
                                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ScSExportBudget) == 1)
                                {
                                    var durataCentTime = CommonService.GetDoubleFromTimeSpan((TimeSpan)cObject.PrevisionalDuration, true);
                                    CellInsertValue(worksheetName, previsionalDurationColumn.Value, rowIndex, durataCentTime, ExcelInsertTypeEnum.Content);
                                }
                                else
                                {
                                    CellInsertValue(worksheetName, previsionalDurationColumn.Value, rowIndex, cObject.PrevisionalDuration, ExcelInsertTypeEnum.HhmmTime);
                                }  
                                // se sto inserendo la durata di previsione allora la sommo ai minuti totali della giornata
                                totalPrevisionalDuration += Convert.ToInt32(cObject.PrevisionalDuration.HasValue ? cObject.PrevisionalDuration.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes);
                            }
                            if (previsionalEntityDesColumn != null)
                                CellInsertValue(worksheetName, previsionalEntityDesColumn.Value, rowIndex, cObject.PrevisionalEntityDes, ExcelInsertTypeEnum.Content);

                            if (confrontationEColumn != null)
                            {
                                CellInsertValue(worksheetName, confrontationEColumn.Value, rowIndex, cObject.ConfrontationHourE, ExcelInsertTypeEnum.HhmmTime);

                                entityTotalConfrontationE += Convert.ToInt32(cObject.ConfrontationHourE.HasValue ? cObject.ConfrontationHourE.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes);

                                if (!String.IsNullOrEmpty(cObject.ConfrontationHourENumberFormat))
                                    CellSetNumberFormat(worksheetName, confrontationEColumn.Value, rowIndex, cObject.ConfrontationHourENumberFormat);
                            }

                            if (confrontationUColumn != null)
                            {
                                CellInsertValue(worksheetName, confrontationUColumn.Value, rowIndex, cObject.ConfrontationHourU, ExcelInsertTypeEnum.HhmmTime);
                                entityTotalConfrontationU += Convert.ToInt32(cObject.ConfrontationHourU.HasValue ? cObject.ConfrontationHourU.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes);
                                if (!String.IsNullOrEmpty(cObject.ConfrontationHourUNumberFormat))
                                    CellSetNumberFormat(worksheetName, confrontationUColumn.Value, rowIndex, cObject.ConfrontationHourUNumberFormat);
                            }
                            if (confrontationDurationColumn != null)
                            {
                                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ScSExportBudget) == 1)
                                {
                                    var durataCentTime = CommonService.GetDoubleFromTimeSpan((TimeSpan)cObject.ConfrontationDuration, true);
                                    CellInsertValue(worksheetName, confrontationDurationColumn.Value, rowIndex, durataCentTime, ExcelInsertTypeEnum.Content);
                                    if (!String.IsNullOrEmpty(cObject.ConfrontationDurationNumberFormat))
                                        CellInsertValue(worksheetName, confrontationDurationColumn.Value, rowIndex, -durataCentTime, ExcelInsertTypeEnum.Content);
                                }
                                else
                                {
                                    CellInsertValue(worksheetName, confrontationDurationColumn.Value, rowIndex, cObject.ConfrontationDuration, ExcelInsertTypeEnum.HhmmTime);
                                    if (!String.IsNullOrEmpty(cObject.ConfrontationDurationNumberFormat))
                                        CellSetNumberFormat(worksheetName, confrontationDurationColumn.Value, rowIndex, cObject.ConfrontationDurationNumberFormat);
                                }                               
                                

                                // se sto inserendo la durata di previsione allora la sommo ai minuti totali della giornata
                                int confDurationPartial = Convert.ToInt32(cObject.ConfrontationDuration.HasValue ? cObject.ConfrontationDuration.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes);
                                if (!String.IsNullOrEmpty(cObject.ConfrontationDurationNumberFormat))
                                    confDurationPartial = confDurationPartial * -1;
                                totalConfrontationDuration += confDurationPartial;

                            }

                            //viene controllata se la personalizzazione sul numero di interventi è attiva
                            if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi))
                            {
                                if (numeroInterventiColumn != null)
                                {
                                    CellInsertValue(worksheetName, numeroInterventiColumn.Value, rowIndex, cObject.NumeroInterventi, ExcelInsertTypeEnum.Content);

                                    // totale del numero di interventi
                                    totaleInterventi += cObject.NumeroInterventi;


                                }
                            }


                            // dopo l'inserimento di una riga si procede con l'incremento dell'indice di riga stesso
                            rowIndex++;
                        }


                        // inserimento dei bordi di quanto scritto (se l'ultima colonna risulta processabilie)
                        // ... e inserimento colorazione di sfondo
                        if (lastConfrontationColumn != 0)
                        {
                            // gestione dei bordi
                            RangeSetBorders(worksheetName, 2, originalRowIndex, 2, originalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);
                            if (originalRowIndex + 1 <= rowIndex - 1)
                                RangeSetBorders(worksheetName, 2, originalRowIndex + 1, 2, rowIndex - 1, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);
                            RangeSetBorders(worksheetName, 2, rowIndex - 1, 2, rowIndex - 1, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);

                            RangeSetBorders(worksheetName, 3, originalRowIndex, lastConfrontationColumn - 1, originalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                            if (originalRowIndex + 1 <= rowIndex - 1)
                                RangeSetBorders(worksheetName, 3, originalRowIndex + 1, lastConfrontationColumn - 1, rowIndex - 1, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                            RangeSetBorders(worksheetName, 3, rowIndex - 1, lastConfrontationColumn - 1, rowIndex - 1, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                            RangeSetBorders(worksheetName, 3, originalRowIndex, lastConfrontationColumn - 1, originalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                            if (originalRowIndex + 1 <= rowIndex - 1)
                                RangeSetBorders(worksheetName, 3, originalRowIndex + 1, lastConfrontationColumn - 1, rowIndex - 1, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                            RangeSetBorders(worksheetName, 3, rowIndex - 1, lastConfrontationColumn - 1, rowIndex - 1, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                            RangeSetBorders(worksheetName, lastConfrontationColumn, originalRowIndex, lastConfrontationColumn, originalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                            if (originalRowIndex + 1 <= rowIndex - 1)
                                RangeSetBorders(worksheetName, lastConfrontationColumn, originalRowIndex + 1, lastConfrontationColumn, rowIndex - 1, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                            RangeSetBorders(worksheetName, lastConfrontationColumn, rowIndex - 1, lastConfrontationColumn, rowIndex - 1, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);

                            // colorazione dello sfondo
                            if (ShowBackgroundColorCustomization == CustomExportsBackgroundColorEnum.ShowColor)
                            {
                                RangeSetBackgroundColor(worksheetName, firstExecutionColumn, originalRowIndex, lastExecutionColumn, rowIndex - 1, Color.LightBlue, ExcelFillStyle.Solid);
                                RangeSetBackgroundColor(worksheetName, firstPrevisionalColumn, originalRowIndex, lastPrevisionalColumn, rowIndex - 1, Color.FromArgb(208, 255, 212), ExcelFillStyle.Solid);
                                RangeSetBackgroundColor(worksheetName, firstConfrontationColumn, originalRowIndex, lastConfrontationColumn, rowIndex - 1, Color.FromArgb(255, 228, 196), ExcelFillStyle.Solid);
                            }

                            // autofit delle colonne
                            ColumnsSetAutoWidth(worksheetName, 2, lastConfrontationColumn);
                        }

                        // se è richiesto l'inserimento dei totali per giorno si procede al loro inserimento
                        // e si incrementa il corrispondente numero di riga
                        if (HourType == ExportRegVHourTypeEnum.OnlyDuration || HourType == ExportRegVHourTypeEnum.Both)
                        {
                            CellInsertValue(worksheetName, 2, rowIndex, BusinessService.GetLocalizedString(PowerWebResources.STR_TOTALE).ToUpper(), ExcelInsertTypeEnum.Content);
                            CellInsertValue(worksheetName, 3, rowIndex, dateToElaborate, ExcelInsertTypeEnum.Content);
                            CellSetNumberFormat(worksheetName, 3, rowIndex, DateNumberFormat);
                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ScSExportBudget) == 1)
                            {
                                CellInsertValue(worksheetName, executionDurationColumn.Value, rowIndex, String.Format("=SUM({0}{1}:{0}{2})", CommonService.GetColumnName(executionDurationColumn.Value - 1), firstObjectRow, rowIndex - 1), ExcelInsertTypeEnum.Formula);
                                CellInsertValue(worksheetName, previsionalDurationColumn.Value, rowIndex, String.Format("=SUM({0}{1}:{0}{2})", CommonService.GetColumnName(previsionalDurationColumn.Value - 1), firstObjectRow, rowIndex - 1), ExcelInsertTypeEnum.Formula);
                                TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(totalConfrontationDuration));
                                var durataCentTime = CommonService.GetDoubleFromTimeSpan((TimeSpan)time, true);
                                CellInsertValue(worksheetName, confrontationDurationColumn.Value, rowIndex, durataCentTime, ExcelInsertTypeEnum.Content);
                            }
                            else
                            {
                                //Inserisce un Timespan fittizzio per far prendere alla cella il formato giusto, poi la sovrascrive con la formula
                                CellInsertValue(worksheetName, executionDurationColumn.Value, rowIndex, new TimeSpan(0), ExcelInsertTypeEnum.HhmmTime);
                                CellInsertValue(worksheetName, executionDurationColumn.Value, rowIndex, String.Format("=SUM({0}{1}:{0}{2})", CommonService.GetColumnName(executionDurationColumn.Value - 1), firstObjectRow, rowIndex - 1), ExcelInsertTypeEnum.Formula);
                                //Inserisce un Timespan fittizzio per far prendere alla cella il formato giusto, poi la sovrascrive con la formula
                                CellInsertValue(worksheetName, previsionalDurationColumn.Value, rowIndex, new TimeSpan(0), ExcelInsertTypeEnum.HhmmTime);
                                CellInsertValue(worksheetName, previsionalDurationColumn.Value, rowIndex, String.Format("=SUM({0}{1}:{0}{2})", CommonService.GetColumnName(previsionalDurationColumn.Value - 1), firstObjectRow, rowIndex - 1), ExcelInsertTypeEnum.Formula);
                                //Inserisce un Timespan fittizzio per far prendere alla cella il formato giusto, poi la sovrascrive con la stringa
                                CellInsertValue(worksheetName, confrontationDurationColumn.Value, rowIndex, new TimeSpan(0), ExcelInsertTypeEnum.HhmmTime);
                                TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(totalConfrontationDuration));
                                CellInsertValue(worksheetName, confrontationDurationColumn.Value, rowIndex, String.Format("{0}{1}:{2}", totalConfrontationDuration < 0 ? "-" : "", (int)time.TotalHours, time.Minutes.ToString("00")), ExcelInsertTypeEnum.HhmmTime);
                            }                               
                           

                            if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi))
                            {
                                //formula per il totale del numero di interventi
                                CellInsertValue(worksheetName, numeroInterventiColumn.Value, rowIndex, String.Format("=SUM({0}{1}:{0}{2})", CommonService.GetColumnName(numeroInterventiColumn.Value - 1), firstObjectRow, rowIndex - 1), ExcelInsertTypeEnum.Formula);

                                // aggiunta dei bordi del totale
                                RangeSetBorders(worksheetName, numeroInterventiColumn.Value, rowIndex, numeroInterventiColumn.Value, rowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                            }

                            // accumula il totale del delta mensile
                            entityTotalConfrontationDuration += totalConfrontationDuration;

                            //totale mensile delle ore previste
                            entityTotalPrevisionalDuration += totalPrevisionalDuration;

                            //totale mensile delle ore effettive
                            entityTotalEffectiveDuration += totalExecutionDuration;

                            //totale mensile del numero di interventi
                            numeroInterventiTotali += totaleInterventi;


                            // formatta la cella a seconda del valore
                            if (totalConfrontationDuration < 0)
                            {
                                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ScSExportBudget) == 1)
                                {
                                    TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(totalConfrontationDuration));
                                    var durataCentTime = CommonService.GetDoubleFromTimeSpan((TimeSpan)time, true);
                                    CellInsertValue(worksheetName, confrontationDurationColumn.Value, rowIndex, -durataCentTime, ExcelInsertTypeEnum.Content);
                                    RangeSetFontColor(worksheetName, confrontationDurationColumn.Value, rowIndex, confrontationDurationColumn.Value, rowIndex, _anomalyDeltaColor);
                                }
                                else
                                {
                                    CellSetNumberFormat(worksheetName, confrontationDurationColumn.Value, rowIndex, NegativeTimeFormat);
                                    RangeSetFontColor(worksheetName, confrontationDurationColumn.Value, rowIndex, confrontationDurationColumn.Value, rowIndex, _anomalyDeltaColor);
                                }                                
                            }
                            else
                                RangeSetFontColor(worksheetName, confrontationDurationColumn.Value, rowIndex, confrontationDurationColumn.Value, rowIndex, _normalDeltaColor);

                            // aggiunta dei bordi del totale
                            RangeSetBorders(worksheetName, 2, rowIndex, 2, rowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);
                            RangeSetBorders(worksheetName, 3, rowIndex, confrontationDurationColumn.Value - 1, rowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                            if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Escludi))
                                RangeSetBorders(worksheetName, confrontationDurationColumn.Value, rowIndex, confrontationDurationColumn.Value, rowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                            else
                                RangeSetBorders(worksheetName, confrontationDurationColumn.Value, rowIndex, confrontationDurationColumn.Value, rowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                        }
                    }
                }

            }
            //nel caso in cui non sia richiesto l'export excel dettaglaito
            else
            {
                // se è richiesto un confronto di ore
                if (UseHourType)
                {
                    // calcolo degli oggetti di confronto per i dati attuali
                    var cObjects = new List<ConfrontationObject>();
                    switch (HourType)
                    {
                        case ExportRegVHourTypeEnum.OnlyDuration:
                            cObjects = CalculateOnlyDurationDayConfrontations(dateToElaborate, worksheetDayRegVs, dayTimesheets).ToList();
                            break;
                        case ExportRegVHourTypeEnum.Eu:
                            cObjects = CalculateEuDayConfrontations(dateToElaborate, worksheetDayRegVs, dayDetailPlan).ToList();
                            break;
                        case ExportRegVHourTypeEnum.Both:
                            cObjects = CalculateBothDayConfrontations(dateToElaborate, worksheetDayRegVs, dayDetailPlan).ToList();
                            break;
                    }

                    // se sono stati trovati degli oggetti di confronto da processare e l'elenco ha oggetti fuori tolleranza, si procede alla loro scrittura sul foglio excel
                    if (cObjects.Any() && HasOutOfTollerance(cObjects))
                    {
                        // calcolo delle colonne di esecuzione, confronto, previsione
                        int? confrontationEColumn = null;
                        int? confrontationUColumn = null;
                        int? confrontationDurationColumn = null;
                        int? previsionalDurationColumn = null;
                        int? previsionalEColumn = null;
                        int? previsionalUColumn = null;
                        int? previsionalEntityDesColumn = null;
                        int? executionEColumn = null;
                        int? executionUColumn = null;
                        int? executionDurationColumn = null;
                        int? numeroInterventiColumn = null;
                        CalculateExportColumns(ref executionDurationColumn, ref previsionalDurationColumn, ref confrontationDurationColumn, ref executionEColumn, ref executionUColumn,
                            ref previsionalEColumn, ref previsionalUColumn, ref previsionalEntityDesColumn, ref confrontationEColumn, ref confrontationUColumn, ref numeroInterventiColumn);

                        // calcolo delle prime e ultime colonne di ogni sessione
                        const int firstExecutionColumn = 3;
                        int lastExecutionColumn = firstExecutionColumn;
                        if (executionDurationColumn.HasValue)
                            lastExecutionColumn = executionDurationColumn.Value;
                        else if (executionUColumn.HasValue)
                            lastExecutionColumn = executionUColumn.Value;
                        else if (executionEColumn.HasValue)
                            lastExecutionColumn = executionEColumn.Value;

                        int firstPrevisionalColumn = lastExecutionColumn + 1;
                        int lastPrevisionalColumn = firstPrevisionalColumn;

                        if (previsionalEntityDesColumn != null)
                            lastPrevisionalColumn = previsionalEntityDesColumn.Value;
                        else if (previsionalDurationColumn.HasValue)
                            lastPrevisionalColumn = previsionalDurationColumn.Value;
                        else if (previsionalUColumn.HasValue)
                            lastPrevisionalColumn = previsionalUColumn.Value;
                        else if (previsionalEColumn.HasValue)
                            lastPrevisionalColumn = previsionalEColumn.Value;

                        int firstConfrontationColumn = lastPrevisionalColumn + 1;
                        int lastConfrontationColumn = firstConfrontationColumn;
                        if (confrontationDurationColumn.HasValue)
                            lastConfrontationColumn = confrontationDurationColumn.Value;
                        else if (confrontationUColumn.HasValue)
                            lastConfrontationColumn = confrontationUColumn.Value;
                        else if (confrontationEColumn.HasValue)
                            lastConfrontationColumn = confrontationEColumn.Value;

                        if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi))
                        {
                            int firstNumeroInterventiColum = lastConfrontationColumn + 1;
                            int lastNumeroInterventiColum = firstNumeroInterventiColum;
                            if (numeroInterventiColumn.HasValue)
                                lastConfrontationColumn = numeroInterventiColumn.Value;
                        }

                        // inizializzazione dei contatori di totale durata eseguita, prevista e delta in giornoata
                        int totalExecutionDuration = 0;
                        int totalPrevisionalDuration = 0;
                        int totalConfrontationDuration = 0;
                        int totaleInterventi = 0;

                        // ciclo di elaborazione degli oggetti da scrivere
                        foreach (ConfrontationObject cObject in cObjects)
                        {

                            if (executionDurationColumn != null)
                            {
                                // se sto inserendo la durata di esecizione allora la sommo ai minuti totali della giornata
                                totalExecutionDuration += Convert.ToInt32(cObject.ExecutionDuration.HasValue ? cObject.ExecutionDuration.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes);
                            }

                            if (previsionalDurationColumn != null)
                            {
                                // se sto inserendo la durata di previsione allora la sommo ai minuti totali della giornata
                                totalPrevisionalDuration += Convert.ToInt32(cObject.PrevisionalDuration.HasValue ? cObject.PrevisionalDuration.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes);
                            }

                            if (confrontationEColumn != null)
                            {

                                entityTotalConfrontationE += Convert.ToInt32(cObject.ConfrontationHourE.HasValue ? cObject.ConfrontationHourE.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes);

                            }

                            if (confrontationUColumn != null)
                            {

                                entityTotalConfrontationU += Convert.ToInt32(cObject.ConfrontationHourU.HasValue ? cObject.ConfrontationHourU.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes);

                            }
                            if (confrontationDurationColumn != null)
                            {

                                // se sto inserendo la durata di previsione allora la sommo ai minuti totali della giornata
                                int confDurationPartial = Convert.ToInt32(cObject.ConfrontationDuration.HasValue ? cObject.ConfrontationDuration.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes);
                                if (!String.IsNullOrEmpty(cObject.ConfrontationDurationNumberFormat))
                                    confDurationPartial = confDurationPartial * -1;
                                totalConfrontationDuration += confDurationPartial;

                            }

                            //viene controllata se la personalizzazione sul numero di interventi è attiva
                            if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi))
                            {
                                if (numeroInterventiColumn != null)
                                {
                                    // totale del numero di interventi
                                    totaleInterventi += cObject.NumeroInterventi;

                                }
                            }

                        }


                        // se è richiesto l'inserimento dei totali per giorno si procede al loro inserimento
                        // e si incrementa il corrispondente numero di riga
                        if (HourType == ExportRegVHourTypeEnum.OnlyDuration || HourType == ExportRegVHourTypeEnum.Both)
                        {
                            //CellInsertValue(worksheetName, 2, rowIndex, BusinessService.GetLocalizedString(PowerWebResources.STR_TOTALE).ToUpper(), ExcelInsertTypeEnum.Content);
                            //CellInsertValue(worksheetName, 3, rowIndex, dateToElaborate, ExcelInsertTypeEnum.Content);
                            //CellSetNumberFormat(worksheetName, 3, rowIndex, DateNumberFormat);
                            ////Inserisce un Timespan fittizzio per far prendere alla cella il formato giusto, poi la sovrascrive con la formula
                            //CellInsertValue(worksheetName, executionDurationColumn.Value, rowIndex, new TimeSpan(0), ExcelInsertTypeEnum.HhmmTime);
                            //CellInsertValue(worksheetName, executionDurationColumn.Value, rowIndex, String.Format("=SUM({0}{1}:{0}{2})", CommonService.GetColumnName(executionDurationColumn.Value - 1), firstObjectRow, rowIndex - 1), ExcelInsertTypeEnum.Formula);
                            ////Inserisce un Timespan fittizzio per far prendere alla cella il formato giusto, poi la sovrascrive con la formula
                            //CellInsertValue(worksheetName, previsionalDurationColumn.Value, rowIndex, new TimeSpan(0), ExcelInsertTypeEnum.HhmmTime);
                            //CellInsertValue(worksheetName, previsionalDurationColumn.Value, rowIndex, String.Format("=SUM({0}{1}:{0}{2})", CommonService.GetColumnName(previsionalDurationColumn.Value - 1), firstObjectRow, rowIndex - 1), ExcelInsertTypeEnum.Formula);
                            ////Inserisce un Timespan fittizzio per far prendere alla cella il formato giusto, poi la sovrascrive con la stringa
                            //CellInsertValue(worksheetName, confrontationDurationColumn.Value, rowIndex, new TimeSpan(0), ExcelInsertTypeEnum.HhmmTime);
                            //TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(totalConfrontationDuration));
                            //CellInsertValue(worksheetName, confrontationDurationColumn.Value, rowIndex, String.Format("{0}{1}:{2}", totalConfrontationDuration < 0 ? "-" : "", (int)time.TotalHours, time.Minutes.ToString("00")), ExcelInsertTypeEnum.HhmmTime);

                            //if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi))
                            //{
                            //    //formula per il totale del numero di interventi
                            //    CellInsertValue(worksheetName, numeroInterventiColumn.Value, rowIndex, String.Format("=SUM({0}{1}:{0}{2})", CommonService.GetColumnName(numeroInterventiColumn.Value - 1), firstObjectRow, rowIndex - 1), ExcelInsertTypeEnum.Formula);

                            //    // aggiunta dei bordi del totale
                            //    RangeSetBorders(worksheetName, numeroInterventiColumn.Value, rowIndex, numeroInterventiColumn.Value, rowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                            //}

                            // accumula il totale del delta mensile
                            entityTotalConfrontationDuration += totalConfrontationDuration;

                            //totale mensile delle ore previste
                            entityTotalPrevisionalDuration += totalPrevisionalDuration;

                            //totale mensile delle ore effettive
                            entityTotalEffectiveDuration += totalExecutionDuration;

                            //totale mensile del numero di interventi
                            numeroInterventiTotali += totaleInterventi;


                            //// formatta la cella a seconda del valore
                            //if (totalConfrontationDuration < 0)
                            //{
                            //    CellSetNumberFormat(worksheetName, confrontationDurationColumn.Value, rowIndex, NegativeTimeFormat);
                            //    RangeSetFontColor(worksheetName, confrontationDurationColumn.Value, rowIndex, confrontationDurationColumn.Value, rowIndex, _anomalyDeltaColor);
                            //}
                            //else
                            //    RangeSetFontColor(worksheetName, confrontationDurationColumn.Value, rowIndex, confrontationDurationColumn.Value, rowIndex, _normalDeltaColor);

                            //// aggiunta dei bordi del totale
                            //RangeSetBorders(worksheetName, 2, rowIndex, 2, rowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);
                            //RangeSetBorders(worksheetName, 3, rowIndex, confrontationDurationColumn.Value - 1, rowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                            //if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Escludi))
                            //    RangeSetBorders(worksheetName, confrontationDurationColumn.Value, rowIndex, confrontationDurationColumn.Value, rowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                            //else
                            //    RangeSetBorders(worksheetName, confrontationDurationColumn.Value, rowIndex, confrontationDurationColumn.Value, rowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                        }
                    }
                }

            }
            return rowIndex;
        }

        /// <summary>
        /// Metodo di scrittura dei totali mensili
        /// </summary>
        /// <param name="worksheetName">Name of the worksheet.</param>
        /// <param name="rowIndex">Index of the row.</param>
        /// <returns></returns>
        private int WriteWorksheetMonthlyTotal(string worksheetName, int rowIndex)
        {
            if (UseExportDetail)
            {
                // se è richiesto un confronto di ore
                if (UseHourType)
                {

                    int totalRowIndex = rowIndex + 1;

                    // calcolo delle colonne di esecuzione, confronto, previsione
                    int? confrontationEColumn = null;
                    int? confrontationUColumn = null;
                    int? confrontationDurationColumn = null;
                    int? previsionalDurationColumn = null;
                    int? previsionalEColumn = null;
                    int? previsionalUColumn = null;
                    int? previsionalEntityDesColumn = null;
                    int? executionEColumn = null;
                    int? executionUColumn = null;
                    int? executionDurationColumn = null;
                    int? numeroInterventiColumn = null;
                    CalculateExportColumns(ref executionDurationColumn, ref previsionalDurationColumn, ref confrontationDurationColumn, ref executionEColumn, ref executionUColumn,
                        ref previsionalEColumn, ref previsionalUColumn, ref previsionalEntityDesColumn, ref confrontationEColumn, ref confrontationUColumn, ref numeroInterventiColumn);

                    // calcolo delle prime e ultime colonne di ogni sessione
                    const int firstExecutionColumn = 3;
                    int lastExecutionColumn = firstExecutionColumn;
                    if (executionDurationColumn.HasValue)
                        lastExecutionColumn = executionDurationColumn.Value;
                    else if (executionUColumn.HasValue)
                        lastExecutionColumn = executionUColumn.Value;
                    else if (executionEColumn.HasValue)
                        lastExecutionColumn = executionEColumn.Value;

                    int firstPrevisionalColumn = lastExecutionColumn + 1;
                    int lastPrevisionalColumn = firstPrevisionalColumn;
                    if (previsionalEntityDesColumn != null)
                        lastPrevisionalColumn = previsionalEntityDesColumn.Value;
                    else if (previsionalDurationColumn.HasValue)
                        lastPrevisionalColumn = previsionalDurationColumn.Value;
                    else if (previsionalUColumn.HasValue)
                        lastPrevisionalColumn = previsionalUColumn.Value;
                    else if (previsionalEColumn.HasValue)
                        lastPrevisionalColumn = previsionalEColumn.Value;

                    int firstConfrontationColumn = lastPrevisionalColumn + 1;
                    int lastConfrontationColumn = firstConfrontationColumn;
                    if (confrontationDurationColumn.HasValue)
                        lastConfrontationColumn = confrontationDurationColumn.Value;
                    else if (confrontationUColumn.HasValue)
                        lastConfrontationColumn = confrontationUColumn.Value;
                    else if (confrontationEColumn.HasValue)
                        lastConfrontationColumn = confrontationEColumn.Value;

                    //viene controllata se la personalizzazione sul numero di interventi è attiva
                    if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi))
                    {
                        int firstNumeroInterventiColumn = lastConfrontationColumn + 1;
                        int lastNumeroInterventiColumn = firstNumeroInterventiColumn;
                        if (numeroInterventiColumn.HasValue)
                            lastNumeroInterventiColumn = numeroInterventiColumn.Value;
                    }

                    #region GESTIONE ORE AGGIUNTIVE MENSILI
                    //se sono presenti delle ore aggiuntive
                    if (oreAggiuntive > 0)
                    {
                        //inserimento dell'etichetta del totale
                        CellInsertValue(worksheetName, 2, totalRowIndex, "ORE AGGIUNTIVE", ExcelInsertTypeEnum.Content);
                        CellInsertValue(worksheetName, 3, totalRowIndex, ExportPeriod.ToString("Y"), ExcelInsertTypeEnum.Content);

                        //vengono costruiti i bordi delle celle
                        RangeSetBorders(worksheetName, 2, totalRowIndex, 2, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);
                        RangeSetBorders(worksheetName, 3, totalRowIndex, 3, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //calcolo dei totali delle ore previste
                        if (previsionalDurationColumn != null)
                        {
                            TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(oreAggiuntive));
                            CellInsertValue(worksheetName, previsionalDurationColumn.Value, totalRowIndex, String.Format("{0}{1}:{2}", entityTotalPrevisionalDuration < 0 ? "-" : "", (int)time.TotalHours, time.Minutes.ToString("00")), ExcelInsertTypeEnum.HhmmTime);

                            //vengono inseite formattati i totali delle ore previste
                            RangeSetFontColor(worksheetName, previsionalDurationColumn.Value, totalRowIndex, previsionalDurationColumn.Value, totalRowIndex, _normalPrevisionalHour);

                            RangeSetBorders(worksheetName, previsionalDurationColumn.Value, totalRowIndex, previsionalDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        }
                        //calcolo dei totali della durata mensile
                        if (executionDurationColumn != null)
                            RangeSetBorders(worksheetName, executionDurationColumn.Value, totalRowIndex, executionDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //ore previste di entrata
                        if (previsionalEColumn != null)
                            RangeSetBorders(worksheetName, previsionalEColumn.Value, totalRowIndex, previsionalEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //ore previste di uscita
                        if (previsionalUColumn != null)
                            RangeSetBorders(worksheetName, previsionalUColumn.Value, totalRowIndex, previsionalUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //ore eseguite di entarta
                        if (executionEColumn != null)
                            RangeSetBorders(worksheetName, executionEColumn.Value, totalRowIndex, executionEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //ore eseguite di entarta
                        if (executionUColumn != null)
                            RangeSetBorders(worksheetName, executionUColumn.Value, totalRowIndex, executionUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //descrizione del collaboratore
                        if (previsionalEntityDesColumn != null)
                            RangeSetBorders(worksheetName, previsionalEntityDesColumn.Value, totalRowIndex, previsionalEntityDesColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        // Scrive il totale mensile del confronto della durata
                        if (confrontationDurationColumn != null && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Escludi)
                            RangeSetBorders(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);

                        // Scrive il totale mensile del confronto della durata
                        if (confrontationDurationColumn != null && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi)
                            RangeSetBorders(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);


                        if (numeroInterventiColumn != null && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi)
                            RangeSetBorders(worksheetName, numeroInterventiColumn.Value, totalRowIndex, numeroInterventiColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);

                        if (confrontationEColumn != null)
                            RangeSetBorders(worksheetName, confrontationEColumn.Value, totalRowIndex, confrontationEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        if (confrontationUColumn != null && confrontationDurationColumn == null)
                            RangeSetBorders(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                        else if (confrontationUColumn != null)
                            RangeSetBorders(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        totalRowIndex++;

                        //vengono aggiunte alle ore previste totali le ore aggiuntive
                        entityTotalPrevisionalDuration += oreAggiuntive;

                        //calcolo del delta totale con le ore mensili aggiuntive
                        entityTotalConfrontationDuration = entityTotalEffectiveDuration - entityTotalPrevisionalDuration;

                    }
                    #endregion

                    #region GESTIONE TOTALI MENSILI
                    //inserimento dell'etichetta del totale
                    CellInsertValue(worksheetName, 2, totalRowIndex, "TOTALE MENSILE", ExcelInsertTypeEnum.Content);
                    CellInsertValue(worksheetName, 3, totalRowIndex, ExportPeriod.ToString("Y"), ExcelInsertTypeEnum.Content);

                    //vengono costruiti i bordi delle celle
                    RangeSetBorders(worksheetName, 2, totalRowIndex, 2, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);
                    RangeSetBorders(worksheetName, 3, totalRowIndex, 3, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    //calcolo dei totali della durata mensile
                    if (executionDurationColumn != null)
                    {
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ScSExportBudget) == 1)
                        {
                            TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalEffectiveDuration));
                            var durataCentTime = CommonService.GetDoubleFromTimeSpan(time, true);
                            CellInsertValue(worksheetName, executionDurationColumn.Value, totalRowIndex, durataCentTime, ExcelInsertTypeEnum.Content);
                        }
                        else
                        {
                            TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalEffectiveDuration));
                            CellInsertValue(worksheetName, executionDurationColumn.Value, totalRowIndex, String.Format("{0}{1}:{2}", entityTotalEffectiveDuration < 0 ? "-" : "", (int)time.TotalHours, time.Minutes.ToString("00")), ExcelInsertTypeEnum.HhmmTime);
                        }                            

                        //vengono inseite formattati i totali delle ore previste
                        RangeSetFontColor(worksheetName, executionDurationColumn.Value, totalRowIndex, executionDurationColumn.Value, totalRowIndex, _normalPrevisionalHour);

                        RangeSetBorders(worksheetName, executionDurationColumn.Value, totalRowIndex, executionDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                    }


                    //calcolo dei totali delle ore previste
                    if (previsionalDurationColumn != null)
                    {
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ScSExportBudget) == 1)
                        {
                            TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalPrevisionalDuration));
                            var durataCentTime = CommonService.GetDoubleFromTimeSpan(time, true);
                            CellInsertValue(worksheetName, previsionalDurationColumn.Value, totalRowIndex, durataCentTime, ExcelInsertTypeEnum.Content);
                        }
                        else
                        {
                            TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalPrevisionalDuration));
                            CellInsertValue(worksheetName, previsionalDurationColumn.Value, totalRowIndex, String.Format("{0}{1}:{2}", entityTotalPrevisionalDuration < 0 ? "-" : "", (int)time.TotalHours, time.Minutes.ToString("00")), ExcelInsertTypeEnum.HhmmTime);
                        }                        

                        //vengono inseite formattati i totali delle ore previste
                        RangeSetFontColor(worksheetName, previsionalDurationColumn.Value, totalRowIndex, previsionalDurationColumn.Value, totalRowIndex, _normalPrevisionalHour);

                        RangeSetBorders(worksheetName, previsionalDurationColumn.Value, totalRowIndex, previsionalDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    }

                    //ore previste di entrata
                    if (previsionalEColumn != null)
                        RangeSetBorders(worksheetName, previsionalEColumn.Value, totalRowIndex, previsionalEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    //ore previste di uscita
                    if (previsionalUColumn != null)
                        RangeSetBorders(worksheetName, previsionalUColumn.Value, totalRowIndex, previsionalUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    //ore eseguite di entarta
                    if (executionEColumn != null)
                        RangeSetBorders(worksheetName, executionEColumn.Value, totalRowIndex, executionEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    //ore eseguite di entarta
                    if (executionUColumn != null)
                        RangeSetBorders(worksheetName, executionUColumn.Value, totalRowIndex, executionUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    //descrizione del collaboratore
                    if (previsionalEntityDesColumn != null)
                        RangeSetBorders(worksheetName, previsionalEntityDesColumn.Value, totalRowIndex, previsionalEntityDesColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    if (confrontationEColumn != null)
                        RangeSetBorders(worksheetName, confrontationEColumn.Value, totalRowIndex, confrontationEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    if (confrontationUColumn != null && confrontationDurationColumn == null)
                        RangeSetBorders(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                    else if (confrontationUColumn != null)
                        RangeSetBorders(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    // Scrive il totale mensile del confronto della durata
                    if (confrontationDurationColumn != null)
                    {
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ScSExportBudget) == 1)
                        {
                            TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalConfrontationDuration));
                            var durataCentTime = CommonService.GetDoubleFromTimeSpan(time, true);
                            CellInsertValue(worksheetName, confrontationDurationColumn.Value, totalRowIndex, durataCentTime, ExcelInsertTypeEnum.Content);
                        }
                        else
                        {
                            TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalConfrontationDuration));
                            CellInsertValue(worksheetName, confrontationDurationColumn.Value, totalRowIndex, String.Format("{0}{1}:{2}", entityTotalConfrontationDuration < 0 ? "-" : "", (int)time.TotalHours, time.Minutes.ToString("00")), ExcelInsertTypeEnum.HhmmTime);
                        }
                        

                        // Formatta la cella a seconda del valore del totale
                        if (entityTotalConfrontationDuration < 0)
                        {
                            //CellSetNumberFormat(worksheetName, confrontationDurationColumn.Value, totalRowIndex, NegativeTimeFormat);
                            RangeSetFontColor(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, _anomalyDeltaColor);
                        }
                        else
                        {
                            RangeSetFontColor(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, _normalDeltaColor);
                        }
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Escludi)
                            RangeSetBorders(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                        else
                            RangeSetBorders(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                    }

                    //se è attiva la personalizzazione per includere il numero di interventi
                    if (numeroInterventiColumn != null && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi)
                    {
                        //viene inserito il numero di interventi
                        CellInsertValue(worksheetName, numeroInterventiColumn.Value, totalRowIndex, numeroInterventiTotali, ExcelInsertTypeEnum.Content);

                        //vengono inseite formattati i totali delle ore previste
                        RangeSetFontColor(worksheetName, numeroInterventiColumn.Value, totalRowIndex, numeroInterventiColumn.Value, totalRowIndex, _normalPrevisionalHour);
                        RangeSetBorders(worksheetName, numeroInterventiColumn.Value, totalRowIndex, numeroInterventiColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);

                    }
                    #endregion
                    /*          TOTALE PER MESE DI CONFRONTO ENTRATA E USCITA ---- DA PERFEZIONARE -----
                    if (confrontationEColumn != null)
                    {
                        CellInsertValue(worksheetName, confrontationEColumn.Value, totalRowIndex, CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalConfrontationE)), ExcelInsertTypeEnum.HhmmTime);

                        // Formatta la cella a seconda del valore del totale
                        if (entityTotalConfrontationE < 0)
                        {
                            CellSetNumberFormat(worksheetName, confrontationEColumn.Value, totalRowIndex, NegativeTimeFormat);
                            RangeSetFontColor(worksheetName, confrontationEColumn.Value, totalRowIndex, confrontationEColumn.Value, totalRowIndex, _anomalyDeltaColor);
                        }
                        else
                        {
                            RangeSetFontColor(worksheetName, confrontationEColumn.Value, totalRowIndex, confrontationEColumn.Value, totalRowIndex, _normalDeltaColor);
                        }
                    }

                    if (confrontationUColumn != null)
                    {
                        CellInsertValue(worksheetName, confrontationUColumn.Value, totalRowIndex, CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalConfrontationU)), ExcelInsertTypeEnum.HhmmTime);

                        // Formatta la cella a seconda del valore del totale
                        if (entityTotalConfrontationU < 0)
                        {
                            CellSetNumberFormat(worksheetName, confrontationUColumn.Value, totalRowIndex, NegativeTimeFormat);
                            RangeSetFontColor(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, _anomalyDeltaColor);
                        }
                        else
                        {
                            RangeSetFontColor(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, _normalDeltaColor);
                        }
                    }
                    */
                }
            }

            else
            {    // se è richiesto un confronto di ore
                if (UseHourType)
                {

                    //int totalRowIndex = rowIndex + 1;

                    int totalRowIndex = rowIndex + 3;

                    // calcolo delle colonne di esecuzione, confronto, previsione
                    int? confrontationEColumn = null;
                    int? confrontationUColumn = null;
                    int? confrontationDurationColumn = null;
                    int? previsionalDurationColumn = null;
                    int? previsionalEColumn = null;
                    int? previsionalUColumn = null;
                    int? previsionalEntityDesColumn = null;
                    int? executionEColumn = null;
                    int? executionUColumn = null;
                    int? executionDurationColumn = null;
                    int? numeroInterventiColumn = null;
                    CalculateExportColumns(ref executionDurationColumn, ref previsionalDurationColumn, ref confrontationDurationColumn, ref executionEColumn, ref executionUColumn,
                        ref previsionalEColumn, ref previsionalUColumn, ref previsionalEntityDesColumn, ref confrontationEColumn, ref confrontationUColumn, ref numeroInterventiColumn);

                    // calcolo delle prime e ultime colonne di ogni sessione
                    const int firstExecutionColumn = 3;
                    int lastExecutionColumn = firstExecutionColumn;
                    if (executionDurationColumn.HasValue)
                        lastExecutionColumn = executionDurationColumn.Value;
                    else if (executionUColumn.HasValue)
                        lastExecutionColumn = executionUColumn.Value;
                    else if (executionEColumn.HasValue)
                        lastExecutionColumn = executionEColumn.Value;

                    int firstPrevisionalColumn = lastExecutionColumn + 1;
                    int lastPrevisionalColumn = firstPrevisionalColumn;
                    if (previsionalEntityDesColumn != null)
                        lastPrevisionalColumn = previsionalEntityDesColumn.Value;
                    else if (previsionalDurationColumn.HasValue)
                        lastPrevisionalColumn = previsionalDurationColumn.Value;
                    else if (previsionalUColumn.HasValue)
                        lastPrevisionalColumn = previsionalUColumn.Value;
                    else if (previsionalEColumn.HasValue)
                        lastPrevisionalColumn = previsionalEColumn.Value;

                    int firstConfrontationColumn = lastPrevisionalColumn + 1;
                    int lastConfrontationColumn = firstConfrontationColumn;
                    if (confrontationDurationColumn.HasValue)
                        lastConfrontationColumn = confrontationDurationColumn.Value;
                    else if (confrontationUColumn.HasValue)
                        lastConfrontationColumn = confrontationUColumn.Value;
                    else if (confrontationEColumn.HasValue)
                        lastConfrontationColumn = confrontationEColumn.Value;

                    //viene controllata se la personalizzazione sul numero di interventi è attiva
                    if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi))
                    {
                        int firstNumeroInterventiColumn = lastConfrontationColumn + 1;
                        int lastNumeroInterventiColumn = firstNumeroInterventiColumn;
                        if (numeroInterventiColumn.HasValue)
                            lastNumeroInterventiColumn = numeroInterventiColumn.Value;
                    }
                  

                    #region GESTIONE ORE AGGIUNTIVE MENSILI
                    //se sono presenti delle ore aggiuntive
                    if (oreAggiuntive > 0)
                    {
                        //inserimento dell'etichetta del totale
                        CellInsertValue(worksheetName, 2, totalRowIndex, "ORE AGGIUNTIVE", ExcelInsertTypeEnum.Content);
                        CellInsertValue(worksheetName, 3, totalRowIndex, ExportPeriod.ToString("Y"), ExcelInsertTypeEnum.Content);

                        //vengono costruiti i bordi delle celle
                        RangeSetBorders(worksheetName, 2, totalRowIndex, 2, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);
                        RangeSetBorders(worksheetName, 3, totalRowIndex, 3, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //calcolo dei totali delle ore previste
                        if (previsionalDurationColumn != null)
                        {
                            TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(oreAggiuntive));
                            CellInsertValue(worksheetName, previsionalDurationColumn.Value, totalRowIndex, String.Format("{0}{1}:{2}", entityTotalPrevisionalDuration < 0 ? "-" : "", (int)time.TotalHours, time.Minutes.ToString("00")), ExcelInsertTypeEnum.HhmmTime);

                            //vengono inseite formattati i totali delle ore previste
                            RangeSetFontColor(worksheetName, previsionalDurationColumn.Value, totalRowIndex, previsionalDurationColumn.Value, totalRowIndex, _normalPrevisionalHour);

                            RangeSetBorders(worksheetName, previsionalDurationColumn.Value, totalRowIndex, previsionalDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        }
                        //calcolo dei totali della durata mensile
                        if (executionDurationColumn != null)
                            RangeSetBorders(worksheetName, executionDurationColumn.Value, totalRowIndex, executionDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //ore previste di entrata
                        if (previsionalEColumn != null)
                            RangeSetBorders(worksheetName, previsionalEColumn.Value, totalRowIndex, previsionalEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //ore previste di uscita
                        if (previsionalUColumn != null)
                            RangeSetBorders(worksheetName, previsionalUColumn.Value, totalRowIndex, previsionalUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //ore eseguite di entarta
                        if (executionEColumn != null)
                            RangeSetBorders(worksheetName, executionEColumn.Value, totalRowIndex, executionEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //ore eseguite di entarta
                        if (executionUColumn != null)
                            RangeSetBorders(worksheetName, executionUColumn.Value, totalRowIndex, executionUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        //descrizione del collaboratore
                        if (previsionalEntityDesColumn != null)
                            RangeSetBorders(worksheetName, previsionalEntityDesColumn.Value, totalRowIndex, previsionalEntityDesColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        // Scrive il totale mensile del confronto della durata
                        if (confrontationDurationColumn != null && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Escludi)
                            RangeSetBorders(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);

                        // Scrive il totale mensile del confronto della durata
                        if (confrontationDurationColumn != null && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi)
                            RangeSetBorders(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);


                        if (numeroInterventiColumn != null && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi)
                            RangeSetBorders(worksheetName, numeroInterventiColumn.Value, totalRowIndex, numeroInterventiColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);

                        if (confrontationEColumn != null)
                            RangeSetBorders(worksheetName, confrontationEColumn.Value, totalRowIndex, confrontationEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        if (confrontationUColumn != null && confrontationDurationColumn == null)
                            RangeSetBorders(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                        else if (confrontationUColumn != null)
                            RangeSetBorders(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                        totalRowIndex++;

                        //vengono aggiunte alle ore previste totali le ore aggiuntive
                        entityTotalPrevisionalDuration += oreAggiuntive;

                        //calcolo del delta totale con le ore mensili aggiuntive
                        entityTotalConfrontationDuration = entityTotalEffectiveDuration - entityTotalPrevisionalDuration;

                    }
                    #endregion

                    #region GESTIONE TOTALI MENSILI
                    //inserimento dell'etichetta del totale
                    CellInsertValue(worksheetName, 2, totalRowIndex, "TOTALE MENSILE", ExcelInsertTypeEnum.Content);
                    CellInsertValue(worksheetName, 3, totalRowIndex, ExportPeriod.ToString("Y"), ExcelInsertTypeEnum.Content);

                    //vengono costruiti i bordi delle celle
                    RangeSetBorders(worksheetName, 2, totalRowIndex, 2, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin);
                    RangeSetBorders(worksheetName, 3, totalRowIndex, 3, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    //calcolo dei totali della durata mensile
                    if (executionDurationColumn != null)
                    {
                        TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalEffectiveDuration));
                        CellInsertValue(worksheetName, executionDurationColumn.Value, totalRowIndex, String.Format("{0}{1}:{2}", entityTotalEffectiveDuration < 0 ? "-" : "", (int)time.TotalHours, time.Minutes.ToString("00")), ExcelInsertTypeEnum.HhmmTime);

                        //vengono inseite formattati i totali delle ore previste
                        RangeSetFontColor(worksheetName, executionDurationColumn.Value, totalRowIndex, executionDurationColumn.Value, totalRowIndex, _normalPrevisionalHour);

                        RangeSetBorders(worksheetName, executionDurationColumn.Value, totalRowIndex, executionDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                    }


                    //calcolo dei totali delle ore previste
                    if (previsionalDurationColumn != null)
                    {
                        TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalPrevisionalDuration));
                        CellInsertValue(worksheetName, previsionalDurationColumn.Value, totalRowIndex, String.Format("{0}{1}:{2}", entityTotalPrevisionalDuration < 0 ? "-" : "", (int)time.TotalHours, time.Minutes.ToString("00")), ExcelInsertTypeEnum.HhmmTime);

                        //vengono inseite formattati i totali delle ore previste
                        RangeSetFontColor(worksheetName, previsionalDurationColumn.Value, totalRowIndex, previsionalDurationColumn.Value, totalRowIndex, _normalPrevisionalHour);

                        RangeSetBorders(worksheetName, previsionalDurationColumn.Value, totalRowIndex, previsionalDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    }

                    //ore previste di entrata
                    if (previsionalEColumn != null)
                        RangeSetBorders(worksheetName, previsionalEColumn.Value, totalRowIndex, previsionalEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    //ore previste di uscita
                    if (previsionalUColumn != null)
                        RangeSetBorders(worksheetName, previsionalUColumn.Value, totalRowIndex, previsionalUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    //ore eseguite di entarta
                    if (executionEColumn != null)
                        RangeSetBorders(worksheetName, executionEColumn.Value, totalRowIndex, executionEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    //ore eseguite di entarta
                    if (executionUColumn != null)
                        RangeSetBorders(worksheetName, executionUColumn.Value, totalRowIndex, executionUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    //descrizione del collaboratore
                    if (previsionalEntityDesColumn != null)
                        RangeSetBorders(worksheetName, previsionalEntityDesColumn.Value, totalRowIndex, previsionalEntityDesColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    if (confrontationEColumn != null)
                        RangeSetBorders(worksheetName, confrontationEColumn.Value, totalRowIndex, confrontationEColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    if (confrontationUColumn != null && confrontationDurationColumn == null)
                        RangeSetBorders(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                    else if (confrontationUColumn != null)
                        RangeSetBorders(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                    // Scrive il totale mensile del confronto della durata
                    if (confrontationDurationColumn != null)
                    {
                        TimeSpan time = CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalConfrontationDuration));
                        CellInsertValue(worksheetName, confrontationDurationColumn.Value, totalRowIndex, String.Format("{0}{1}:{2}", entityTotalConfrontationDuration < 0 ? "-" : "", (int)time.TotalHours, time.Minutes.ToString("00")), ExcelInsertTypeEnum.HhmmTime);

                        // Formatta la cella a seconda del valore del totale
                        if (entityTotalConfrontationDuration < 0)
                        {
                            //CellSetNumberFormat(worksheetName, confrontationDurationColumn.Value, totalRowIndex, NegativeTimeFormat);
                            RangeSetFontColor(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, _anomalyDeltaColor);
                        }
                        else
                        {
                            RangeSetFontColor(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, _normalDeltaColor);
                        }
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Escludi)
                            RangeSetBorders(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
                        else
                            RangeSetBorders(worksheetName, confrontationDurationColumn.Value, totalRowIndex, confrontationDurationColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);
                    }

                    //se è attiva la personalizzazione per includere il numero di interventi
                    if (numeroInterventiColumn != null && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi)
                    {
                        //viene inserito il numero di interventi
                        CellInsertValue(worksheetName, numeroInterventiColumn.Value, totalRowIndex, numeroInterventiTotali, ExcelInsertTypeEnum.Content);

                        //vengono inseite formattati i totali delle ore previste
                        RangeSetFontColor(worksheetName, numeroInterventiColumn.Value, totalRowIndex, numeroInterventiColumn.Value, totalRowIndex, _normalPrevisionalHour);
                        RangeSetBorders(worksheetName, numeroInterventiColumn.Value, totalRowIndex, numeroInterventiColumn.Value, totalRowIndex, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);

                    }
                    #endregion
                    /*          TOTALE PER MESE DI CONFRONTO ENTRATA E USCITA ---- DA PERFEZIONARE -----
                    if (confrontationEColumn != null)
                    {
                        CellInsertValue(worksheetName, confrontationEColumn.Value, totalRowIndex, CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalConfrontationE)), ExcelInsertTypeEnum.HhmmTime);

                        // Formatta la cella a seconda del valore del totale
                        if (entityTotalConfrontationE < 0)
                        {
                            CellSetNumberFormat(worksheetName, confrontationEColumn.Value, totalRowIndex, NegativeTimeFormat);
                            RangeSetFontColor(worksheetName, confrontationEColumn.Value, totalRowIndex, confrontationEColumn.Value, totalRowIndex, _anomalyDeltaColor);
                        }
                        else
                        {
                            RangeSetFontColor(worksheetName, confrontationEColumn.Value, totalRowIndex, confrontationEColumn.Value, totalRowIndex, _normalDeltaColor);
                        }
                    }

                    if (confrontationUColumn != null)
                    {
                        CellInsertValue(worksheetName, confrontationUColumn.Value, totalRowIndex, CommonService.GetTimeSpanFromMinutes(Math.Abs(entityTotalConfrontationU)), ExcelInsertTypeEnum.HhmmTime);

                        // Formatta la cella a seconda del valore del totale
                        if (entityTotalConfrontationU < 0)
                        {
                            CellSetNumberFormat(worksheetName, confrontationUColumn.Value, totalRowIndex, NegativeTimeFormat);
                            RangeSetFontColor(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, _anomalyDeltaColor);
                        }
                        else
                        {
                            RangeSetFontColor(worksheetName, confrontationUColumn.Value, totalRowIndex, confrontationUColumn.Value, totalRowIndex, _normalDeltaColor);
                        }
                    }
                    */
                }


            }
            return rowIndex;

        }

        /// <summary>
        /// Calcola il posizionamento delle colonne in base al tipo di confronto da effettuare.
        /// </summary>
        /// <param name="executionDurationColumn">La colonna che contiene la durata d'esecuzione.</param>
        /// <param name="previsionalDurationColumn">La colonna che contiene la durata prevista.</param>
        /// <param name="confrontationDurationColumn">La colonna che contiene la durata di confronto.</param>
        /// <param name="executionEColumn">La colonna che contiene l'ora di entrata in esecuzione.</param>
        /// <param name="executionUColumn">La colonna che contiene l'ora di uscita in esecuzione.</param>
        /// <param name="previsionalEColumn">La colonna che contiene l'ora di entrata prevista.</param>
        /// <param name="previsionalUColumn">La colonna che contiene l'ora di uscita prevista.</param>
        /// <param name="previsionalEntityDesColumn">La colonna che contiene l'eventuale descrizione dell'altra entità da visualizzare</param>
        /// <param name="confrontationEColumn">La colonna che contiene l'ora di entrata confrontata.</param>
        /// <param name="confrontationUColumn">La colonna che contiene l'ora di uscita confrontata.</param>
        private void CalculateExportColumns(ref int? executionDurationColumn, ref int? previsionalDurationColumn, ref int? confrontationDurationColumn, ref int? executionEColumn,
            ref int? executionUColumn, ref int? previsionalEColumn, ref int? previsionalUColumn, ref int? previsionalEntityDesColumn, ref int? confrontationEColumn, ref int? confrontationUColumn, ref int? numeroInterventi)
        {
            if (UseHourType)
            {
                switch (HourType)
                {
                    case ExportRegVHourTypeEnum.OnlyDuration:
                        executionDurationColumn = 4;
                        previsionalDurationColumn = 5;
                        confrontationDurationColumn = 6;
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi)
                            numeroInterventi = 7;
                        break;
                    case ExportRegVHourTypeEnum.Eu:
                        executionEColumn = 4;
                        executionUColumn = 5;
                        previsionalEColumn = 6;
                        previsionalUColumn = 7;
                        previsionalEntityDesColumn = 8;
                        confrontationEColumn = 9;
                        confrontationUColumn = 10;
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi)
                            numeroInterventi = 11;
                        break;
                    case ExportRegVHourTypeEnum.Both:
                        executionEColumn = 4;
                        executionUColumn = 5;
                        executionDurationColumn = 6;
                        previsionalEColumn = 7;
                        previsionalUColumn = 8;
                        previsionalDurationColumn = 9;
                        previsionalEntityDesColumn = 10;
                        confrontationEColumn = 11;
                        confrontationUColumn = 12;
                        confrontationDurationColumn = 13;
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.InserimentoNumeroInterventi) == (int)InserimentoNumeroInterventi.Includi)
                            numeroInterventi = 14;

                        break;
                }
            }
        }

        /// <summary>
        /// Calcola e restituisce l'elenco degli orari del mese in elaborazione per l'entità passata come parametro (sempre suddivise a loro volta per altra entità).
        /// L'orario prevede la presenza unicamente delle ore previste (piano).
        /// </summary>
        /// <param name="entityId">L'id dell'entità di cui generare il cartellino.</param>
        /// <returns>La lista degli orari dell'entità per il periodo specificato.</returns>
        private List<TimesheetModuleItem> GetEntityMonthTimesheet(int entityId)
        {
            string entityType = String.Empty;
            switch (ModelFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Col:
                    entityType = "Col";
                    break;
                case ExcelModelSelectionTypeEnum.Cant:
                    entityType = "Can";
                    break;
            }
            return TimesheetModuleItem.GenerateTimeSheet(ExportPeriod, false, true, String.Empty, false, new List<int>() { entityId }, null, entityType)
                        .Where(tsm => tsm.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN)).ToList();
        }

        /// <summary>
        /// Calcola e restituisce l'elenco di oggetti di confronto (calcolati per differenziazione entrata/uscita)
        /// per uno specifico giorno con delle specifiche registrazioni e orari.
        /// </summary>
        /// <param name="dateToConfront">La data oggetto del confronto.</param>
        /// <param name="allDayRegVs">Le registrazioni del giorno su cui effettuare il confronto.</param>
        /// <param name="dayTimesheets">L'elenco degli orari utilizati nel giorno ed utili al confronto.</param>
        /// <returns>Un elenco di oggetti di confronto risultato dell'analisi differenziale per durata dei parametri.</returns>
        private IEnumerable<ConfrontationObject> CalculateOnlyDurationDayConfrontations(DateTime dateToConfront, IEnumerable<Reg_V> allDayRegVs, IList<TimesheetModuleItem> dayTimesheets)
        {
            // per durata le registrazioni sono confrontate (e raggruppate) per l'entità (collaboratore/cantiere) opposta a quella attualmente in processo
            // il confronto viene quindi generalizzato a livello di altra entità

            // per il confronto solo durata sono trattate solamente le registrazioni che non sono attività o passaggi e che sono abbinate
            IEnumerable<Reg_V> dayRegVs = allDayRegVs.Where(regv => regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass
                                                                    && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass);

            //se la custumization è attiva si escludono le reg 'TRASFERTA'
            if(RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExcludeAwayHoursInExport) == 1)
                dayRegVs = dayRegVs.Where(r => r.Motivazione_Reg_Cod != "TRA");

            // inizializzazione del valore di ritorno del metodo
            var confrontations = new List<ConfrontationObject>();

            // inizializzazione dell'array che raggruppa gli oggetti di confronto per altra entità
            var confrontationsByOtherEntity = new Dictionary<int, ConfrontationObject>();

            //inizializzazione del nuemro di interventi
            int numeroInterventi = 0;

            // inizializzazione della lista che contiene l'elenco dei timesheets utilizzati nell'elaborazione del giorno
            var usedTimesheetIds = new List<long>();

             

            #region Ealborazione delle registrazioni

            // per ogni registrazione da processare
            foreach (var dayRegV in dayRegVs)
            {

                // calcolo dell'id dell'altra entità rispetto a quella attualmente in processo
                int otherEntityId = GetOtherEntityId(dayRegV);

                // se non esiste nel dizionario un'entry con quest'entità ne viene generata una
                if (!confrontationsByOtherEntity.ContainsKey(otherEntityId))
                {
                    #region Aggiunta nuovo oggetto di confronto al dizionario

                    numeroInterventi = 0;

                    // calcolo della descrizione dell'entità opposta a quella in elaborazione
                    string otherEntityDes = otherEntityId != 0 ? GetOtherEntityDescription(dayRegV) : String.Empty;

                    // creazione di un nuovo oggetto di confronto per la durata
                    var newConfObject = ConfrontationObject.NewConfrontationObjectForDuration(otherEntityDes, dateToConfront);

                    //inserisco il numero di interventi nell'oggetto che scrivo sull'export
                    numeroInterventi++;

                    newConfObject.NumeroInterventi = numeroInterventi;

                    // aggiunta al dizionario del nuovo oggetto generato
                    confrontationsByOtherEntity.Add(otherEntityId, newConfObject);

                    #endregion
                }
                else
                {
                    numeroInterventi++;
                    confrontationsByOtherEntity[otherEntityId].NumeroInterventi = numeroInterventi;
                }

                // aggiunta dei minuti di durata della registrazione all'apposita proprietà nell'oggetto di confronto
                int? regvDuration = 0;
                if (UseCalculationType)
                    switch (CalculationType)
                    {
                        case ExportRegVCalculationTypeEnum.Physical:
                            regvDuration = dayRegV.Durata_Fis;
                            break;
                        case ExportRegVCalculationTypeEnum.Rounded:
                            regvDuration = dayRegV.Durata_Fig;
                            break;
                    }
                DateTime minutesToAdd = DateTime.Today.AddMinutes(Convert.ToDouble(regvDuration));
                if (regvDuration > 0)
                {                    
                    confrontationsByOtherEntity[otherEntityId].ExecutionDuration = confrontationsByOtherEntity[otherEntityId].ExecutionDuration == null
                                                                                        ? TimeSpan.Zero.Add(minutesToAdd.TimeOfDay)
                                                                                        : confrontationsByOtherEntity[otherEntityId].ExecutionDuration.Value.Add(minutesToAdd.TimeOfDay);
                }
                else
                {
                     confrontationsByOtherEntity[otherEntityId].ExecutionDuration = confrontationsByOtherEntity[otherEntityId].ExecutionDuration == null
                                                                                        ? TimeSpan.Zero.Add(minutesToAdd.TimeOfDay)
                                                                                        : confrontationsByOtherEntity[otherEntityId].ExecutionDuration.Value.Add(TimeSpan.FromMinutes((double)regvDuration));
                }


                // si recupera l'orario associato alla presente registrazione; nel caso dell'analisi per durata si tratta dell'orario
                // con lo stesso cantiere non già precedentemente utilizzato per confronto
                TimesheetModuleItem currentTimesheet = dayTimesheets.Where(tsm => !usedTimesheetIds.Contains(tsm.ID)).FirstOrDefault(tsm => GetOtherEntityId(tsm) == otherEntityId);


                // aggiunta dei minuti di confronto all'apposita proprietà nell'oggetto di confronto (se trovato un valido orario)
                if (currentTimesheet != default(TimesheetModuleItem))
                {
                    // aggiunta del timesheet tra gli id dei timesheets utilizzati
                    usedTimesheetIds.Add(currentTimesheet.ID);

                    // aggiunta dei minuti di confronto
                    minutesToAdd = DateTime.Today.AddMinutes(Convert.ToDouble(currentTimesheet.GetDayMinutes(dateToConfront.Day)));
                    confrontationsByOtherEntity[otherEntityId].PrevisionalDuration = confrontationsByOtherEntity[otherEntityId].PrevisionalDuration == null
                                                                                        ? TimeSpan.Zero.Add(minutesToAdd.TimeOfDay)
                                                                                        : confrontationsByOtherEntity[otherEntityId].PrevisionalDuration.Value.Add(minutesToAdd.TimeOfDay);
                }
            }

            #endregion

            #region Elaborazione degli orari non processati

            // per ogni orario non utilizzato viene generato corrispettivo oggetto di confronto
            foreach (TimesheetModuleItem timesheet in dayTimesheets.Where(tsm => !usedTimesheetIds.Contains(tsm.ID)))
            {
                // calcolo dell'id dell'altra entità rispetto a quella attualmente in processo
                int otherEntityId = GetOtherEntityId(timesheet);

                // se non esiste nel dizionario un'entry con quest'entità ne viene generata una
                if (!confrontationsByOtherEntity.ContainsKey(otherEntityId))
                {
                    #region Aggiunta nuovo oggetto di confronto al dizionario

                    // calcolo della descrizione dell'entità opposta a quella in elaborazione
                    string otherEntityDes = otherEntityId != 0 ? GetOtherEntityDescription(timesheet) : String.Empty;

                    // creazione di un nuovo oggetto di confronto per la durata
                    var newConfObject = ConfrontationObject.NewConfrontationObjectForDuration(otherEntityDes, dateToConfront);

                    // aggiunta al dizionario del nuovo oggetto generato
                    confrontationsByOtherEntity.Add(otherEntityId, newConfObject);

                    #endregion


                    // aggiunta dei minuti di confronto
                    DateTime minutesToAdd = DateTime.Today.AddMinutes(Convert.ToDouble(timesheet.GetDayMinutes(dateToConfront.Day)));
                    confrontationsByOtherEntity[otherEntityId].PrevisionalDuration = confrontationsByOtherEntity[otherEntityId].PrevisionalDuration == null
                                                                                        ? TimeSpan.Zero.Add(minutesToAdd.TimeOfDay)
                                                                                        : confrontationsByOtherEntity[otherEntityId].PrevisionalDuration.Value.Add(minutesToAdd.TimeOfDay);
                }
            }

            #endregion

            // al termine della costruzione degli oggetti di confronto per altra entità
            // si procede al calcolo della differenza
            foreach (KeyValuePair<int, ConfrontationObject> cObject in confrontationsByOtherEntity)
            {
                // calcolo della differenza in minuti tra la durata eseguita e quella prevista
                int executionMinutesDuration = cObject.Value.ExecutionDuration != null ? Convert.ToInt32(cObject.Value.ExecutionDuration.Value.TotalMinutes) : 0;
                int previsionalMinutesDuration = cObject.Value.PrevisionalDuration != null ? Convert.ToInt32(cObject.Value.PrevisionalDuration.Value.TotalMinutes) : 0;
                int delta = executionMinutesDuration - previsionalMinutesDuration;

                // impostazione del confronto di durata
                cObject.Value.ConfrontationDuration = DateTime.Today.AddMinutes(Math.Abs(delta)).TimeOfDay;
                if (delta < 0)
                    cObject.Value.ConfrontationDurationNumberFormat = NegativeTimeFormat;
            }

            // se ci sono dei valori da riportare li si prepara per il ritorno del metodo
            if (confrontationsByOtherEntity.Any())
                confrontations.AddRange(confrontationsByOtherEntity.Select(co => co.Value));

            // ritorno del valore di calcolato dal metodo
            return confrontations;
        }

        /// <summary>
        /// Calcola e restituisce l'elenco degli oggetti di confronto (calcolati per differenziazione entrata/uscita)
        /// per uno specifico giorno con delle specifiche regisrazioni e orari di dettaglio.
        /// </summary>
        /// <param name="dateToConfront">La data oggetto del confronto.</param>
        /// <param name="allDayRegVs">Le registrazioni del giorno su cui effettuare il confronto.</param>
        /// <param name="dayDetailPlan">L'elenco degli orari di dettaglio della giornata utili al confronto.</param>
        /// <returns>Un elenco di oggetti di confronto risultato dell'anslisi differenziale per entrata/uscita dei parametri.</returns>
        private IEnumerable<ConfrontationObject> CalculateEuDayConfrontations(DateTime dateToConfront, IEnumerable<Reg_V> allDayRegVs, IEnumerable<Tuple<int, TimeSpan, TimeSpan>> dayDetailPlan)
        {
            // per entrata/uscita le registrazioni sono confrontate in ordine cronologico durante il giorno in elaborazione
            // al memomento senza raggruppare o conteggiare i dati per altra entità

            // per il confronto solo entrata/uscita sono trattate solamente le registrazioni che non sono attività o passaggi e che sono abbinate
            // inoltre, vista la specifica tipologia di abbinanmento per orario sono ordinate per ora di entrata (figurativa o fisica a seconda della richiesta)
            IEnumerable<Reg_V> dayRegVs = allDayRegVs.Where(regv => regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass
                                                                    && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass)
                                                                    .OrderBy(regv => CalculationType == ExportRegVCalculationTypeEnum.Physical ? regv.Data_Ora_Fis_E : regv.Data_Ora_Fig_E);

            // l'elenco degli orari di dettaglio, se presenti, viene ordinato per ora di entrata
            IList<Tuple<int, TimeSpan, TimeSpan>> orderedDetailPlan = dayDetailPlan.OrderBy(tpl => tpl.Item2).ToList();

            // inizializzazione del valore di ritorno del metodo
            var confrontations = new List<ConfrontationObject>();

            // inizializzazione dell'indice di gestione del piano di dettaglio
            int dayPlanIndex = 0;

            // per ogni registrazione da processare
            foreach (var dayRegV in dayRegVs)
            {
                // calcolo dell'id dell'altra entità rispetto a quella attualmente in processo
                int otherEntityId = GetOtherEntityId(dayRegV);

                // calcolo della descrizione dell'altra entità rispetto a quella attualmente in processo
                string otherEntityDes = otherEntityId != 0 ? GetOtherEntityDescription(dayRegV) : String.Empty;

                // nel caso di confronto per enrtrata/uscita ogni registrazione in processo ha un suo oggetto di confronto
                ConfrontationObject newConfrontation = ConfrontationObject.NewConfrontationObjectFoEu(otherEntityDes, dateToConfront);

                // inserimento dei dati di esecuzione nell'oggetto di confronto
                if (UseCalculationType)
                    switch (CalculationType)
                    {
                        case ExportRegVCalculationTypeEnum.Physical:
                            newConfrontation.ExecutionHourE = dayRegV.Data_Ora_Fis_E.TimeOfDay;
                            newConfrontation.ExecutionHourU = dayRegV.Data_Ora_Fis_U != null ? dayRegV.Data_Ora_Fis_U.Value.TimeOfDay : TimeSpan.Zero;
                            break;
                        case ExportRegVCalculationTypeEnum.Rounded:
                            newConfrontation.ExecutionHourE = dayRegV.Data_Ora_Fig_E != null ? dayRegV.Data_Ora_Fig_E.Value.TimeOfDay : TimeSpan.Zero;
                            newConfrontation.ExecutionHourU = dayRegV.Data_Ora_Fig_U != null ? dayRegV.Data_Ora_Fig_U.Value.TimeOfDay : TimeSpan.Zero;
                            break;
                    }

                // si recupera il dettaglio di orario associato alla presente registrazione; nel caso dell'analisi per sola entrata/uscita
                // si accopiano gli orari con le registrazioni nell'ordine della giornata (crescente)
                Tuple<int, TimeSpan, TimeSpan> currentPlanDetail = null;
                if (dayPlanIndex < orderedDetailPlan.Count) // se sono nell'indice degli orari
                {
                    currentPlanDetail = new Tuple<int, TimeSpan, TimeSpan>(orderedDetailPlan.ElementAt(dayPlanIndex).Item1, orderedDetailPlan.ElementAt(dayPlanIndex).Item2, orderedDetailPlan.ElementAt(dayPlanIndex).Item3);
                    dayPlanIndex++;
                }

                // se è stato trovato un dettaglio da accoppiare alla registrazione corrente
                // si imposta il dato e il confronto utilizzando tali dati
                if (currentPlanDetail != null)
                {
                    // inserimento delle eventuali differenze di altra entità
                    newConfrontation.PrevisionalEntityDes = GetOtherEntityDescription(currentPlanDetail.Item1);
                    newConfrontation.ExecutionAndProvisionalSameEntity = currentPlanDetail.Item1 == otherEntityId;

                    // inserimento delle ore nel timesheet
                    newConfrontation.PrevisionalHourE = currentPlanDetail.Item2;
                    newConfrontation.PrevisionalHourU = currentPlanDetail.Item3;

                    // calcolo del delta tra entrata e uscita
                    int deltaMinutesE = newConfrontation.ExecutionHourE != null
                        ? Convert.ToInt32(newConfrontation.ExecutionHourE.Value.TotalMinutes - newConfrontation.PrevisionalHourE.Value.TotalMinutes)
                        : Convert.ToInt32(TimeSpan.Zero.TotalMinutes - newConfrontation.PrevisionalHourE.Value.TotalMinutes);
                    int deltaMinutesU = newConfrontation.ExecutionHourU != null
                        ? Convert.ToInt32(newConfrontation.ExecutionHourU.Value.TotalMinutes - newConfrontation.PrevisionalHourU.Value.TotalMinutes)
                        : Convert.ToInt32(TimeSpan.Zero.TotalMinutes - newConfrontation.PrevisionalHourU.Value.TotalMinutes);

                    // inserimento nell'oggetto di confronto del delta per entrata/uscita
                    newConfrontation.ConfrontationHourE = DateTime.Today.AddMinutes(Math.Abs(deltaMinutesE)).TimeOfDay;
                    if (deltaMinutesE < 0)
                        newConfrontation.ConfrontationHourENumberFormat = NegativeTimeFormat;
                    newConfrontation.ConfrontationHourU = DateTime.Today.AddMinutes(Math.Abs(deltaMinutesU)).TimeOfDay;
                    if (deltaMinutesU < 0)
                        newConfrontation.ConfrontationHourUNumberFormat = NegativeTimeFormat;
                }
                else // altrimenti, se non c'è un piano di dettaglio abbinabile si segnalano i dati
                {
                    // inserimento delle eventuali differenze di altra entità
                    newConfrontation.PrevisionalEntityDes = null;
                    newConfrontation.ExecutionAndProvisionalSameEntity = false;

                    // inserimento delle ore di confronto assumento un valore negativo dell'ora effettuata
                    newConfrontation.ConfrontationHourE = newConfrontation.ExecutionHourE != null ? newConfrontation.ExecutionHourE.Value : TimeSpan.Zero;
                    newConfrontation.ConfrontationHourU = newConfrontation.ExecutionHourU != null ? newConfrontation.ExecutionHourU.Value : TimeSpan.Zero;
                }

                // aggiunta dell'oggetto di confronto alla lista di ritorno
                confrontations.Add(newConfrontation);

            }

            #region Elaborazione degli orari non processati

            // se sono rimasti ancora degli orari non processati
            if (dayPlanIndex < orderedDetailPlan.Count)
            {
                // allora li si aggiunge come oggetti di confronto negativi all'elenco
                for (int planIndex = dayPlanIndex; planIndex < orderedDetailPlan.Count; planIndex++)
                {
                    // generazione del nuovo oggetto di confronto
                    ConfrontationObject newConfrontation = ConfrontationObject.NewConfrontationObjectFoEu(GetOtherEntityDescription(orderedDetailPlan.ElementAt(planIndex).Item1), dateToConfront);

                    // aggiunta dei valori di previsione
                    newConfrontation.PrevisionalHourE = orderedDetailPlan.ElementAt(planIndex).Item2;
                    newConfrontation.PrevisionalHourU = orderedDetailPlan.ElementAt(planIndex).Item3;

                    // aggiunta dei valori di confronto
                    newConfrontation.ConfrontationHourE = orderedDetailPlan.ElementAt(planIndex).Item2;
                    newConfrontation.ConfrontationHourENumberFormat = NegativeTimeFormat;
                    newConfrontation.ConfrontationHourU = orderedDetailPlan.ElementAt(planIndex).Item3;
                    newConfrontation.ConfrontationHourUNumberFormat = NegativeTimeFormat;

                    // aggiunta dell'oggetto di confronto all'elenco di ritorno
                    confrontations.Add(newConfrontation);
                }
            }

            #endregion

            // ritorno del valore di calcolato dal metodo
            return confrontations;
        }

        /// <summary>
        /// Calcola e restituisce l'elenco degli oggetti di confronto (calcolati per differenziazione entrata/uscita e durata)
        /// per uno specifico giorno con delle specifiche regisrazioni e orari di dettaglio.
        /// </summary>
        /// <param name="dateToConfront">La data oggetto del confronto.</param>
        /// <param name="allDayRegVs">Le registrazioni del giorno su cui effettuare il confronto.</param>
        /// <param name="dayDetailPlan">L'elenco degli orari di dettaglio della giornata utili al confronto.</param>
        /// <returns>Un elenco di oggetti di confronto risultato dell'anslisi differenziale per entrata/uscita e durata dei parametri.</returns>
        private IEnumerable<ConfrontationObject> CalculateBothDayConfrontations(DateTime dateToConfront, IEnumerable<Reg_V> allDayRegVs, IEnumerable<Tuple<int, TimeSpan, TimeSpan>> dayDetailPlan)
        {
            // per entrata/uscita e durata le registrazioni sono confrontate in ordine cronologico durante il giorno in elaborazione
            // al memomento senza raggruppare o conteggiare i dati per altra entità

            // per il confronto per entrata/uscita e durata durata sono trattate solamente le registrazioni che non sono attività o passaggi e che sono abbinate;
            // inoltre, vista la specifica tipologia di abbinanmento per orario sono ordinate per ora di entrata (figurativa o fisica a seconda della richiesta)
            IEnumerable<Reg_V> dayRegVs = allDayRegVs.Where(regv => regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass
                                                                    && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass)
                                                                    .OrderBy(regv => CalculationType == ExportRegVCalculationTypeEnum.Physical ? regv.Data_Ora_Fis_E : regv.Data_Ora_Fig_E);

            // l'elenco degli orari di dettaglio, se presenti, viene ordinato per ora di entrata
            IList<Tuple<int, TimeSpan, TimeSpan>> orderedDetailPlan = dayDetailPlan.OrderBy(tpl => tpl.Item2).ToList();

            // inizializzazione del valore di ritorno del metodo
            var confrontations = new List<ConfrontationObject>();

            // inizializzazione dell'indice di gestione del piano di dettaglio
            int dayPlanIndex = 0;

            bool nocturneModuleActive = RepoManager.ParamRepo.ParametersRow.Abilita_Notturno;
            // tipo notturno
            NocturneTypeEnum nocturneTypeParam = nocturneModuleActive ? (NocturneTypeEnum)RepoManager.ParamRepo.ParametersRow.TipoNotturno : NocturneTypeEnum.None;

            TimeSpan almostMidnight = new TimeSpan(23, 59, 0);
            TimeSpan midnight = new TimeSpan(0, 0, 0);


            Tuple<int, TimeSpan, TimeSpan> currentPlanDetail = null;

            // per ogni registrazione da processare
            foreach (var dayRegV in dayRegVs)
            {
                // calcolo dell'id dell'altra entità rispetto a quella attualmente in processo
                int otherEntityId = GetOtherEntityId(dayRegV);

                // calcolo della descrizione dell'altra entità rispetto a quella attualmente in processo
                string otherEntityDes = otherEntityId != 0 ? GetOtherEntityDescription(dayRegV) : String.Empty;

                // nel caso di confronto per enrtrata/uscita e durata ogni registrazione in processo ha un suo oggetto di confronto
                ConfrontationObject newConfrontation = ConfrontationObject.NewConfrontationObjectFoEu(otherEntityDes, dateToConfront);

                // inserimento dei dati di esecuzione nell'oggetto di confronto
                if (UseCalculationType)
                    switch (CalculationType)
                    {
                        case ExportRegVCalculationTypeEnum.Physical:
                            newConfrontation.ExecutionHourE = dayRegV.Data_Ora_Fis_E.TimeOfDay;
                            newConfrontation.ExecutionHourU = dayRegV.Data_Ora_Fis_U != null ? dayRegV.Data_Ora_Fis_U.Value.TimeOfDay : TimeSpan.Zero;
                            newConfrontation.ExecutionDuration = dayRegV.Durata_Fis != null ? CommonService.GetTimeSpanFromMinutes(dayRegV.Durata_Fis.Value) : TimeSpan.Zero;
                            break;
                        case ExportRegVCalculationTypeEnum.Rounded:
                            newConfrontation.ExecutionHourE = dayRegV.Data_Ora_Fig_E != null ? dayRegV.Data_Ora_Fig_E.Value.TimeOfDay : TimeSpan.Zero;
                            newConfrontation.ExecutionHourU = dayRegV.Data_Ora_Fig_U != null ? dayRegV.Data_Ora_Fig_U.Value.TimeOfDay : TimeSpan.Zero;
                            newConfrontation.ExecutionDuration = dayRegV.Durata_Fig != null ? CommonService.GetTimeSpanFromMinutes(dayRegV.Durata_Fig.Value) : TimeSpan.Zero;
                            break;
                    }

                // si recupera il dettaglio di orario associato alla presente registrazione; nel caso dell'analisi per entrata/uscita e durata
                // si accopiano gli orari con le registrazioni nell'ordine della giornata (crescente)
                if (dayPlanIndex < orderedDetailPlan.Count) // se sono nell'indice degli orari
                {
                    currentPlanDetail = new Tuple<int, TimeSpan, TimeSpan>(orderedDetailPlan.ElementAt(dayPlanIndex).Item1, orderedDetailPlan.ElementAt(dayPlanIndex).Item2, orderedDetailPlan.ElementAt(dayPlanIndex).Item3);
                    dayPlanIndex++;
                }

                // se è stato trovato un dettaglio da accoppiare alla registrazione corrente
                // si imposta il dato e il confronto utilizzando tali dati
                if (currentPlanDetail != null)
                {
                    // inserimento delle eventuali differenze di altra entità
                    newConfrontation.PrevisionalEntityDes = GetOtherEntityDescription(currentPlanDetail.Item1);
                    newConfrontation.ExecutionAndProvisionalSameEntity = currentPlanDetail.Item1 == otherEntityId;

                    // inserimento delle ore nel timesheet
                    newConfrontation.PrevisionalHourE = currentPlanDetail.Item2;
                    newConfrontation.PrevisionalHourU = currentPlanDetail.Item3;

                    // inserimento della durata prevista
                    if (nocturneTypeParam != NocturneTypeEnum.OverMidnight && nocturneTypeParam != NocturneTypeEnum.Duration)
                    {
                        newConfrontation.PrevisionalDuration = CommonService.GetTimeSpanFromMinutes(Convert.ToInt32(currentPlanDetail.Item3.TotalMinutes - currentPlanDetail.Item2.TotalMinutes));
                    }
                    //se è attivao il notturno dopo mezzanote
                    else if (nocturneTypeParam == NocturneTypeEnum.OverMidnight || nocturneTypeParam == NocturneTypeEnum.Duration)
                    {
                        //controllo che l'entrata prevista nell'orario sia antecedente alla mezzanotte
                        if (currentPlanDetail.Item2 > currentPlanDetail.Item3)
                        {
                            //calcolo la durata facendo la differenza tra l'ora di entrata e la mezzanotte e l'ora di uscita e la mezzanotte
                            newConfrontation.PrevisionalDuration = CommonService.GetTimeSpanFromMinutes((Convert.ToInt32(almostMidnight.TotalMinutes - currentPlanDetail.Item2.TotalMinutes) + 1
                                + Convert.ToInt32(currentPlanDetail.Item3.TotalMinutes - midnight.TotalMinutes)));

                        }
                        //se l'entrata prevista nell'orario è dopo mezzanotte
                        else
                            //l'ora di uscita risulta maggiore dell'ora di entrata
                            newConfrontation.PrevisionalDuration = CommonService.GetTimeSpanFromMinutes(Convert.ToInt32(currentPlanDetail.Item3.TotalMinutes - currentPlanDetail.Item2.TotalMinutes));

                    }

                    // calcolo del delta tra entrata e uscita e durata
                    int deltaMinutesE = newConfrontation.ExecutionHourE != null
                        ? Convert.ToInt32(newConfrontation.ExecutionHourE.Value.TotalMinutes - newConfrontation.PrevisionalHourE.Value.TotalMinutes)
                        : Convert.ToInt32(TimeSpan.Zero.TotalMinutes - newConfrontation.PrevisionalHourE.Value.TotalMinutes);
                    int deltaMinutesU = newConfrontation.ExecutionHourU != null
                        ? Convert.ToInt32(newConfrontation.ExecutionHourU.Value.TotalMinutes - newConfrontation.PrevisionalHourU.Value.TotalMinutes)
                        : Convert.ToInt32(TimeSpan.Zero.TotalMinutes - newConfrontation.PrevisionalHourU.Value.TotalMinutes);
                    int deltaDuration = newConfrontation.ExecutionDuration != null
                        ? Convert.ToInt32(newConfrontation.ExecutionDuration.Value.TotalMinutes - newConfrontation.PrevisionalDuration.Value.TotalMinutes)
                        : Convert.ToInt32(TimeSpan.Zero.TotalMinutes - newConfrontation.PrevisionalDuration.Value.TotalMinutes);


                    // inserimento nell'oggetto di confronto del delta per entrata/uscita e durata
                    newConfrontation.ConfrontationHourE = DateTime.Today.AddMinutes(Math.Abs(deltaMinutesE)).TimeOfDay;
                    if (deltaMinutesE < 0)
                        newConfrontation.ConfrontationHourENumberFormat = NegativeTimeFormat;
                    newConfrontation.ConfrontationHourU = DateTime.Today.AddMinutes(Math.Abs(deltaMinutesU)).TimeOfDay;
                    if (deltaMinutesU < 0)
                        newConfrontation.ConfrontationHourUNumberFormat = NegativeTimeFormat;
                    newConfrontation.ConfrontationDuration = DateTime.Today.AddMinutes(Math.Abs(deltaDuration)).TimeOfDay;
                    if (deltaDuration < 0)
                        newConfrontation.ConfrontationDurationNumberFormat = NegativeTimeFormat;
                }
                else // altrimenti, se non c'è un piano di dettaglio abbinabile si segnalano i dati
                {
                    // inserimento delle eventuali differenze di altra entità
                    newConfrontation.PrevisionalEntityDes = null;
                    newConfrontation.ExecutionAndProvisionalSameEntity = false;

                    // inserimento delle ore di confronto assumento un valore negativo dell'ora effettuata
                    newConfrontation.ConfrontationHourE = newConfrontation.ExecutionHourE != null ? newConfrontation.ExecutionHourE.Value : TimeSpan.Zero;
                    newConfrontation.ConfrontationHourU = newConfrontation.ExecutionHourU != null ? newConfrontation.ExecutionHourU.Value : TimeSpan.Zero;
                    newConfrontation.ConfrontationDuration = newConfrontation.ExecutionDuration != null ? newConfrontation.ExecutionDuration.Value : TimeSpan.Zero;
                }

                // aggiunta dell'oggetto di confronto alla lista di ritorno
                confrontations.Add(newConfrontation);

            }

            #region Elaborazione degli orari non processati

            // se sono rimasti ancora degli orari non processati
            if (dayPlanIndex < orderedDetailPlan.Count)
            {
                // allora li si aggiunge come oggetti di confronto negativi all'elenco
                for (int planIndex = dayPlanIndex; planIndex < orderedDetailPlan.Count; planIndex++)
                {
                    // generazione del nuovo oggetto di confronto
                    ConfrontationObject newConfrontation = ConfrontationObject.NewConfrontationObjectFoEu(GetOtherEntityDescription(orderedDetailPlan.ElementAt(planIndex).Item1), dateToConfront);

                    // aggiunta dei valori di previsione
                    newConfrontation.PrevisionalHourE = orderedDetailPlan.ElementAt(planIndex).Item2;
                    newConfrontation.PrevisionalHourU = orderedDetailPlan.ElementAt(planIndex).Item3;

                    //se è NON è attivato il notturno dopo mezzanotte
                    if (nocturneTypeParam != NocturneTypeEnum.OverMidnight)
                        newConfrontation.PrevisionalDuration = CommonService.GetTimeSpanFromMinutes(Convert.ToInt32(orderedDetailPlan.ElementAt(planIndex).Item3.TotalMinutes - orderedDetailPlan.ElementAt(planIndex).Item2.TotalMinutes));

                    //se è attivao il notturno dopo mezzanote
                    else if (nocturneTypeParam == NocturneTypeEnum.OverMidnight || nocturneTypeParam == NocturneTypeEnum.Duration)
                    {
                        //controllo che l'entrata prevista nell'orario sia antecedente alla mezzanotte
                        if (orderedDetailPlan.ElementAt(planIndex).Item2 > orderedDetailPlan.ElementAt(planIndex).Item3)
                        {
                            newConfrontation.PrevisionalDuration = CommonService.GetTimeSpanFromMinutes((Convert.ToInt32(almostMidnight.TotalMinutes - orderedDetailPlan.ElementAt(planIndex).Item2.TotalMinutes) + 1
                                + Convert.ToInt32(orderedDetailPlan.ElementAt(planIndex).Item3.TotalMinutes - midnight.TotalMinutes)));

                        }
                        //se l'entrata prevista nell'orario è dopo mezzanotte
                        else
                            newConfrontation.PrevisionalDuration = CommonService.GetTimeSpanFromMinutes(Convert.ToInt32(orderedDetailPlan.ElementAt(planIndex).Item3.TotalMinutes - orderedDetailPlan.ElementAt(planIndex).Item2.TotalMinutes));

                    }

                    // aggiunta dei valori di confronto
                    newConfrontation.ConfrontationHourE = orderedDetailPlan.ElementAt(planIndex).Item2;
                    newConfrontation.ConfrontationHourENumberFormat = NegativeTimeFormat;
                    newConfrontation.ConfrontationHourU = orderedDetailPlan.ElementAt(planIndex).Item3;
                    newConfrontation.ConfrontationHourUNumberFormat = NegativeTimeFormat;
                    newConfrontation.ConfrontationDuration = newConfrontation.PrevisionalDuration;
                    newConfrontation.ConfrontationDurationNumberFormat = NegativeTimeFormat;

                    // aggiunta dell'oggetto di confronto all'elenco di ritorno
                    confrontations.Add(newConfrontation);
                }
            }

            #endregion

            // ritorno del valore di calcolato dal metodo
            return confrontations;
        }

        /// <summary>
        /// Determina se in uno specifico elenco di oggetti di confronto esistono elementi con tempi al di fuori della tolleranza per l'elaborazione in corso.
        /// </summary>
        /// <param name="confObjects">Gli oggetti di confronto di cui verificare la tolleranza.</param>
        /// <returns><c>true</c> se l'elenco contiene uno o più oggetti fuori tolleranza; altrimenti <c>false</c></returns>
        private bool HasOutOfTollerance(IEnumerable<ConfrontationObject> confObjects)
        {
            return confObjects.Any(IsOutOfTollerance);
        }

        /// <summary>
        /// Determina se lo specifico oggetto di confronto ha all'esterno del range di tolleranza per l'elaborazione in corso;
        /// per i confronti di tipo entrata/uscita o entrambi è considerata anomalia anche un cantiere diverso anche se non vuoto nel piano
        /// </summary>
        /// <param name="confObject">L'oggetto di confronto di cui controllare la conformità con la tolleranza.</param>
        /// <returns><c>true</c> se il valore risulta fuori tolleranza; altrimenti <c>false</c></returns>
        private bool IsOutOfTollerance(ConfrontationObject confObject)
        {
            // inizializzazione del valore di ritorno del metodo (per default il valore passato è fuori tolleranza)
            bool outOfTollerance = true;

            // la tolleranza va calcolata in base al tipo di ora processata
            switch (HourType)
            {
                case ExportRegVHourTypeEnum.OnlyDuration:
                    // si effettua il calcolo solo se richiesto
                    if (UseDurationTollerance)
                        outOfTollerance = Convert.ToInt32(confObject.ConfrontationDuration.HasValue ? confObject.ConfrontationDuration.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes) >= DurationTollerance;
                    break;
                case ExportRegVHourTypeEnum.Eu:
                    // si effettua il caclolo solo se richiesto
                    if (UseEUTollerance)
                    {
                        // in caso di controllo di entrata e uscita basta che uno dei due valori superi la tolleranza per
                        // poter determinare tutto l'oggetto fuori tolleranza
                        outOfTollerance = Convert.ToInt32(confObject.ConfrontationHourE.HasValue ? confObject.ConfrontationHourE.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes) >= EUTollerance;
                        if (!outOfTollerance)
                            outOfTollerance = Convert.ToInt32(confObject.ConfrontationHourU.HasValue ? confObject.ConfrontationHourU.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes) >= EUTollerance;
                    }
                    break;
                case ExportRegVHourTypeEnum.Both:
                    // si effettua il calcolo solo se richiesto
                    if (UseDurationTollerance)
                    {
                        // in caso di controllo di entrambi i parametri basta che uno solo dei due sia
                        // fuori tolleranza perché l'intero oggetto lo sia
                        outOfTollerance = Convert.ToInt32(confObject.ConfrontationDuration.HasValue ? confObject.ConfrontationDuration.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes) >= DurationTollerance;
                    }

                    // si effettua il calcolo solo se richiesto
                    if (UseEUTollerance)
                    {
                        if (!outOfTollerance)
                            outOfTollerance = Convert.ToInt32(confObject.ConfrontationHourE.HasValue ? confObject.ConfrontationHourE.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes) >= EUTollerance;
                        if (!outOfTollerance)
                            outOfTollerance = Convert.ToInt32(confObject.ConfrontationHourU.HasValue ? confObject.ConfrontationHourU.Value.TotalMinutes : TimeSpan.Zero.TotalMinutes) >= EUTollerance;
                    }
                    break;
            }

            // nel caso di confronto per entrata/uscita o entrambi è considerata anomalia anche la differenza di altra entità quando quella prevista è diversa da stringa vuota
            if (!outOfTollerance && (HourType == ExportRegVHourTypeEnum.Both || HourType == ExportRegVHourTypeEnum.Eu))
                outOfTollerance = !String.IsNullOrEmpty(confObject.PrevisionalEntityDes) && !confObject.ExecutionAndProvisionalSameEntity;

            // ritorno del valore calcolato dal metodo
            return outOfTollerance;
        }

        #endregion

    }

    #region Strumenti aggiuntivi export

    /// <summary>
    /// Classe utilizzata per rappresentare un oggetto da stampare di confronto ore/budget
    /// </summary>
    internal class ConfrontationObject
    {

        #region Public Properties

        /// <summary>
        /// Recupera o imposta la descrizione dell'entità di esecuzione della registrazione.
        /// </summary>
        /// <value>
        /// La descrizione dell'entità di esecuzione della registrazione.
        /// </value>
        public string ExecutionEntityDes { get; set; }

        /// <summary>
        /// Recupera o imposta la data del confronto tra registrazione e piano.
        /// </summary>
        /// <value>
        /// La data del confronto tra registrazione e piano.
        /// </value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Recupera o imposta l'ora di entrata della registrazione eseguita.
        /// </summary>
        /// <value>
        /// L'ora di entrata della registrazione eseguita.
        /// </value>
        public TimeSpan? ExecutionHourE { get; set; }

        /// <summary>
        /// Recupera o imposta l'ora di uscita della registrazione eseguita.
        /// </summary>
        /// <value>
        /// L'ora di uscita della registrazione eseguita.
        /// </value>
        public TimeSpan? ExecutionHourU { get; set; }

        /// <summary>
        /// Recupera o imposta la durata della registrazione eseguita.
        /// </summary>
        /// <value>
        /// La durata della registrazione eseguita.
        /// </value>
        public TimeSpan? ExecutionDuration { get; set; }

        /// <summary>
        /// Recupera o imposta l'ora di entrata del confronto previsionale.
        /// </summary>
        /// <value>
        /// L'ora di entrata del confronto previsionale.
        /// </value>
        public TimeSpan? PrevisionalHourE { get; set; }

        /// <summary>
        /// Recupera o imposta l'ora di uscita del confronto previsionale.
        /// </summary>
        /// <value>
        /// L'ora di uscita del confronto previsionale.
        /// </value>
        public TimeSpan? PrevisionalHourU { get; set; }

        /// <summary>
        /// Recupera o imposta la durata del confronto previsionale.
        /// </summary>
        /// <value>
        /// La durata del confronto previsionale.
        /// </value>
        public TimeSpan? PrevisionalDuration { get; set; }

        /// <summary>
        /// Recupera o imposta la descrizione dell'entità riportata nel previsionale.
        /// </summary>
        /// <value>
        /// La descrizione dell'entità riportata nel previsionale.
        /// </value>
        public string PrevisionalEntityDes { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che determina se l'entità dell'eseguito e del previsionale sono le stesse.
        /// </summary>
        /// <value>
        /// <c>true</c> se l'entità dell'eseguito e del previsionale sono le stesse; altrimenti, <c>false</c>.
        /// </value>
        public bool ExecutionAndProvisionalSameEntity { get; set; }

        /// <summary>
        /// Recupera o imposta il valore di confronto per l'ora d'entrata.
        /// </summary>
        /// <value>
        /// Il valore di confronto per l'ora d'entrata.
        /// </value>
        public TimeSpan? ConfrontationHourE { get; set; }

        /// <summary>
        /// Recupera o imposta il number format da applicare al <see cref="ConfrontationHourE"/> in fase di scrittura.
        /// </summary>
        /// <value>
        /// Il number format da applicare al <see cref="ConfrontationHourE"/> in fase di scrittura.
        /// </value>
        public string ConfrontationHourENumberFormat { get; set; }

        /// <summary>
        /// Recupera o imposta il valore di confronto per l'ora d'uscita.
        /// </summary>
        /// <value>
        /// Il valore di confronto per l'ora d'uscita.
        /// </value>
        public TimeSpan? ConfrontationHourU { get; set; }

        /// <summary>
        /// Recupera o imposta il number format da applicare al <see cref="ConfrontationHourU"/> in fase di scrittura.
        /// </summary>
        /// <value>
        /// Il number format da applicare al <see cref="ConfrontationHourU"/> in fase di scrittura.
        /// </value>
        public string ConfrontationHourUNumberFormat { get; set; }

        /// <summary>
        /// Recupera o imposta il valore di confronto per la durata.
        /// </summary>
        /// <value>
        /// Il valore di confronto per la durata.
        /// </value>
        public TimeSpan? ConfrontationDuration { get; set; }

        /// <summary>
        /// Recupera o imposta il number format da applicare a <see cref="ConfrontationDuration"/> in fase di scrittura.
        /// </summary>
        /// <value>
        /// Il number format da applicare a <see cref="ConfrontationDuration"/> in fase di scrittura.
        /// </value>
        public string ConfrontationDurationNumberFormat { get; set; }

        /// <summary>
        /// Recupera o imposta il numero di minuti di confronto con l'ora d'entrata;
        /// questo valore non è utilizzato in fase di scrittura.
        /// </summary>
        /// <value>
        /// Il numero di minuti di confronto con l'ora d'entrata;
        /// questo valore non è utilizzato in fase di scrittura.
        /// </value>
        public int? ConfrontationHourEMinutes { get; set; }

        /// <summary>
        /// Recupera o imposta il numero di minuti di confronto con l'ora d'uscita;
        /// questo valore non è utilizzato in fase di scrittura.
        /// </summary>
        /// <value>
        /// Il numero di minuti di confronto con l'ora d'uscita;
        /// questo valore non è utilizzato in fase di scrittura.
        /// </value>
        public int ConfrontationHourUMinutes { get; set; }

        /// <summary>
        /// Recupera o imposta il numero di minuti di confronto con la durata;
        /// questo valore non è utilizzato in fase di scrittura.
        /// </summary>
        /// <value>
        /// Recupera o imposta il numero di minuti di confronto con la durata;
        /// questo valore non è utilizzato in fase di scrittura.
        /// </value>
        public int ConfrontationDurationMinutes { get; set; }



        /// <summary>
        /// Ottiene il numero di interventi
        /// </summary>
        /// <value>
        /// The numero interventi.
        /// </value>
        public int NumeroInterventi { get; set; }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Genera un nuovo oggetto di confronto utilizzato in un'analisi per sola durata.
        /// </summary>
        /// <param name="otherEntityDes">La descrizione dell'entità opposta a quella in elaborazione da impostare.</param>
        /// <param name="dateToConfront">La data di confronto da impostare nell'oggetto.</param>
        /// <returns>Un nuovo oggetto di confronto utilizzabile in un'analisi per sola durata.</returns>
        public static ConfrontationObject NewConfrontationObjectForDuration(string otherEntityDes, DateTime dateToConfront)
        {
            return new ConfrontationObject()
            {
                ExecutionEntityDes = otherEntityDes,
                Date = dateToConfront,
                ExecutionHourE = null,
                ExecutionHourU = null,
                ExecutionDuration = TimeSpan.Zero,
                PrevisionalHourE = null,
                PrevisionalHourU = null,
                PrevisionalDuration = TimeSpan.Zero,
                PrevisionalEntityDes = otherEntityDes,
                ExecutionAndProvisionalSameEntity = true,
                ConfrontationHourE = null,
                ConfrontationHourENumberFormat = String.Empty,
                ConfrontationHourU = null,
                ConfrontationHourUNumberFormat = String.Empty,
                ConfrontationDuration = TimeSpan.Zero,
                ConfrontationDurationNumberFormat = String.Empty,
                ConfrontationHourEMinutes = 0,
                ConfrontationHourUMinutes = 0,
                ConfrontationDurationMinutes = 0,
                NumeroInterventi = 0

            };
        }

        /// <summary>
        /// Genera un nuovo oggetto di confronto utilizzato in un'analisi per sola entrata/uscita.
        /// </summary>
        /// <param name="otherEntityDes">La descrizione dell'entità opposta a quella in elaborazione da impostare.</param>
        /// <param name="dateToConfront">La data di confronto da imposta nell'oggetto.</param>
        /// <returns>Un nuovo oggetto di confronto utilizzabile in un'analisi per sola entrata/uscita.</returns>
        public static ConfrontationObject NewConfrontationObjectFoEu(string otherEntityDes, DateTime dateToConfront)
        {
            return new ConfrontationObject()
            {
                ExecutionEntityDes = otherEntityDes,
                Date = dateToConfront,
                ExecutionHourE = null,
                ExecutionHourU = null,
                ExecutionDuration = null,
                PrevisionalHourE = null,
                PrevisionalHourU = null,
                PrevisionalDuration = null,
                PrevisionalEntityDes = null,
                ExecutionAndProvisionalSameEntity = false,
                ConfrontationHourE = null,
                ConfrontationHourENumberFormat = String.Empty,
                ConfrontationHourU = null,
                ConfrontationHourUNumberFormat = String.Empty,
                ConfrontationDuration = null,
                ConfrontationDurationNumberFormat = String.Empty,
                ConfrontationHourEMinutes = 0,
                ConfrontationHourUMinutes = 0,
                ConfrontationDurationMinutes = 0,
                NumeroInterventi = 0
            };
        }

        #endregion

    }

    #endregion

}
