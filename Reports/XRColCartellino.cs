using Business.BusinessExtension;
using Common;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraReports.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;

namespace Reports
{
    /// <summary>
    /// Classe che rappresenta il report del cartellino presenz per i collaboe dell'applicativo
    /// </summary>
    public partial class XRColCartellino : DevExpress.XtraReports.UI.XtraReport
    {

        #region Enums

        /// <summary>
        /// L'enum utilizzato per indicare il tipo di totale da stampare
        /// </summary>
        private enum TotalTypeEnum
        {
            /// <summary>
            /// Totale delle ore
            /// </summary>
            Total,

            /// <summary>
            /// Delta delle ore
            /// </summary>
            Delta,

            /// <summary>
            /// Ore con motivazione
            /// </summary>
            Justification,

            /// <summary>
            /// Ore con motivazione
            /// </summary>
            Arrot,

            /// <summary>
            /// Ore di straordinario notturno
            /// </summary>
            NocturneOvertime,

            /// <summary>
            /// Ore di straordinario
            /// </summary>
            Overtime,

            /// <summary>
            /// Ore ordinarie
            /// </summary>
            Ordinary,

            /// <summary>
            /// Ore di piano
            /// </summary>
            Plan
        }

        #endregion

        #region Private Constants

        /// <summary>
        /// La formattazione stringa da impotare sui numeri del totale
        /// </summary>
        private const string ToStringTotalFormat = "00.00";

        /// <summary>
        /// La stringa da utilizzare per l'impostazione di valori 0
        /// </summary>
        private const string ZeroNormalValue = "-";

        /// <summary>
        /// La stringa delle celle con valore zero
        /// </summary>
        private const string ZeroDetailCellValue = "00,00";

        /// <summary>
        /// Il formato con cui visualizzare le date
        /// </summary>
        //TODO: gestire con la cultura utente
        private const string DateFormat = "dd/MM/yyyy";

        #endregion

        #region Private Fields

        /// <summary>
        /// L'elenco del mone minuti per collaboratore (chiave => collaboratore; valore => monte minuti)
        /// </summary>
        private IEnumerable<TimesheetModuleItem> _totalCartellino;

        /// <summary>
        /// L'elenco del monteminuti per collaboratore (chiave => collaboratore; valore => monte minuti)
        /// </summary>
        private Dictionary<int, double> _lastMinutesAmmoutByCol;

        /// <summary>
        /// Il dizionario che contiene come chiave il numero del giorno di stampa e come valore
        /// la stringa da riportare in intestazione della colonna giorno
        /// </summary>
        private readonly Dictionary<int, string> _groupDaysString = new Dictionary<int, string>();

        /// <summary>
        /// L'elenco dei giorni week end presenti nel mese
        /// </summary>
        private readonly List<int> _monthWeekendDays = new List<int>();

        /// <summary>
        /// Indica se visualizzare o meno il cantiere nel report corrente
        /// </summary>
        private bool _showCant = true;

        /// <summary>
        /// Indica se visualizzare o meno gli zeri come stringhe vuote nel report corrente
        /// </summary>
        private bool _showZeroAsEmptyString = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Inizializza una nuova istanza della classe <see cref="XRColTimesheet"/>.
        /// </summary>
        public XRColCartellino()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Inizializza una nuova istanza della classe <see cref="XRColTimesheet"/>.
        /// </summary>
        /// <param name="timesheets">L'elenco degli oggetti cartellino del mese da riportare nel report.</param>
        /// <param name="tsTotalController">Il controller per il calcolo dei totali da inserire nel report.</param>
        /// <param name="sections">L'elenco delle sezioni da nascondere nel report.</param>
        /// <param name="reportOptions">Le opzioni da applicare al report.</param>
        public XRColCartellino(IEnumerable<TimesheetModuleItem> cartellini, IEnumerable<TimesheetModuleItem> totalCartellini, List<string> sections = null, dynamic reportOptions = null)
            : this()
        {

            // il data source del report sono gli oggetti timesheets passati come parametro
            DataSource = cartellini;

            // salvataggio nell'oggetto corrente del total controller
            _totalCartellino = totalCartellini;

            // eventuale disabilitazione delle sezioni da non visualizzare
            if (sections != null)
                CommonServiceReport.DisableSections(this, sections);

            // si calcolano le stringhe da visualizzare nella raggruppamento dove si visualizzano i giorni
            PopulateDayDatas(cartellini);

            // gestione delle opzioni del report
            ManageReportOptions(reportOptions);

            // calcolo del monte minuti per collaboraotre
            PopulateMonthMinutesDictionary(cartellini);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Popola il dizionario dei monte minuti per collaboratore in base ai timesheet specificati.
        /// </summary>
        /// <param name="timesheets">L'elenco dei timesheets da cui estrarre il monte minuti.</param>
        private void PopulateMonthMinutesDictionary(IEnumerable<TimesheetModuleItem> timesheets)
        {
            _lastMinutesAmmoutByCol = new Dictionary<int, double>();
            IEnumerable<TimesheetModuleItem> monthMinutesItem = timesheets.Where(tsm => tsm.LastMonthlyHours != 0);
            if (monthMinutesItem.Any())
                monthMinutesItem.ForEach(tsm => _lastMinutesAmmoutByCol.Add(tsm.ColId, tsm.LastMonthlyHours));
        }

        /// <summary>
        /// Gestisce sul report corrente l'attivazione delle opzioni passate come parametro.
        /// </summary>
        /// <param name="reportOptions">Le opzioni report da applicare.</param>
        private void ManageReportOptions(dynamic reportOptions)
        {
            reportOptions = new
            {
                showStrNot = false,
                opzShowZeroAsEmptyString = false,
                showDelta = true,
                showStr = false,
                showPiano = true,
                showTotale = true,
                showAdditional = false,
                showJust = true,
                showArrot = false,
                showWeeklyTotals = false,
                opzHideSection = false,
                OPZ_NO_CANT = false,
            };
            // se non ci sono delle opzioni passate come parametro
            if (reportOptions != null)
            {
                // se è presente l'opzione di nascondimento del cantiere si imposta il campo relativo
                _showCant = reportOptions.OPZ_NO_CANT ?? false;

                // se è presente l'opzione di visualizzare le ore a zero come stringhe vuote, si imposta il campo relativo    
                _showZeroAsEmptyString = reportOptions.opzShowZeroAsEmptyString ?? false;

                // se è richiesto il nascondimento del delta allora si procede al suo nascondimento
                if (!(reportOptions.showDelta ?? true))
                {
                    rfDeltaMonth.Visible = false;
                    chWeeklyTotalsDelta.Visible = false;
                    cfWeek1DeltaTotal.Visible = false;
                    cfWeek2DeltaTotal.Visible = false;
                    cfWeek3DeltaTotal.Visible = false;
                    cfWeek4DeltaTotal.Visible = false;
                    cfWeek5DeltaTotal.Visible = false;
                    cfWeek6DeltaTotal.Visible = false;
                }
                // se è richiesto il nascondimento delle straordinarie allora si procede al loro nascondimento
                if (!(reportOptions.showStr ?? true))
                {
                    rfStrMonth.Visible = false;
                    chWeeklyTotalsStr.Visible = false;
                    cfWeek1StrTotal.Visible = false;
                    cfWeek2StrTotal.Visible = false;
                    cfWeek3StrTotal.Visible = false;
                    cfWeek4StrTotal.Visible = false;
                    cfWeek5StrTotal.Visible = false;
                    cfWeek6StrTotal.Visible = false;
                }

                // se è richiesto il nascondimento del piano allora si procede al suo nascondimento
                if (!(reportOptions.showPiano ?? true))
                {
                    rfPlanMonth.Visible = false;
                    chWeeklyTotalsPlan.Visible = false;
                    cfWeek1PlanTotal.Visible = false;
                    cfWeek2PlanTotal.Visible = false;
                    cfWeek3PlanTotal.Visible = false;
                    cfWeek4PlanTotal.Visible = false;
                    cfWeek5PlanTotal.Visible = false;
                    cfWeek6PlanTotal.Visible = false;
                }

                // se è richiesto il nascondimento del totale allora si procede al suo nascondimento
                if (!(reportOptions.showTotale ?? true))
                {
                    rfTotMonth.Visible = false;
                    chWeeklyTotalsTot.Visible = false;
                    cfWeek1TotTotal.Visible = false;
                    cfWeek2TotTotal.Visible = false;
                    cfWeek3TotTotal.Visible = false;
                    cfWeek4TotTotal.Visible = false;
                    cfWeek5TotTotal.Visible = false;
                    cfWeek6TotTotal.Visible = false;
                }

                // se è richiesto il nascondimento dei totali aggiuntivi allora si procede al loro nascondimento
                if (!(reportOptions.showAdditional ?? true))
                {
                    rfStrMonth.Visible = false;
                    rfStrNocMonth.Visible = false;
                    rfOrdinaryMonth.Visible = false;
                    rfJustMonth.Visible = false;

                    chWeeklyTotalsStr.Visible = false;
                    chWeeklyTotalsStrNoc.Visible = false;
                    chWeeklyTotalsJust.Visible = false;
                    chWeeklyTotalsOrdinary.Visible = false;

                    cfWeek1StrTotal.Visible = false;
                    cfWeek2StrTotal.Visible = false;
                    cfWeek3StrTotal.Visible = false;
                    cfWeek4StrTotal.Visible = false;
                    cfWeek5StrTotal.Visible = false;
                    cfWeek6StrTotal.Visible = false;

                    cfWeek1StrNocTotal.Visible = false;
                    cfWeek2StrNocTotal.Visible = false;
                    cfWeek3StrNocTotal.Visible = false;
                    cfWeek4StrNocTotal.Visible = false;
                    cfWeek5StrNocTotal.Visible = false;
                    cfWeek6StrNocTotal.Visible = false;

                    cfWeek1JustTotal.Visible = false;
                    cfWeek2JustTotal.Visible = false;
                    cfWeek3JustTotal.Visible = false;
                    cfWeek4JustTotal.Visible = false;
                    cfWeek5JustTotal.Visible = false;
                    cfWeek6JustTotal.Visible = false;

                    cfWeek1OrdTotal.Visible = false;
                    cfWeek2OrdTotal.Visible = false;
                    cfWeek3OrdTotal.Visible = false;
                    cfWeek4OrdTotal.Visible = false;
                    cfWeek5OrdTotal.Visible = false;
                    cfWeek6OrdTotal.Visible = false;
                }

                // se è richiesto il nascondimento delle motivazioni dal totale allora si procede al loro nascondimento
                if (!(reportOptions.showJust ?? true))
                {
                    rfJustMonth.Visible = false;
                    chWeeklyTotalsJust.Visible = false;
                    cfWeek1JustTotal.Visible = false;
                    cfWeek2JustTotal.Visible = false;
                    cfWeek3JustTotal.Visible = false;
                    cfWeek4JustTotal.Visible = false;
                    cfWeek5JustTotal.Visible = false;
                    cfWeek6JustTotal.Visible = false;
                }

                // se è richiesto il nascondimento delle motivazioni dal totale allora si procede al loro nascondimento
                if (!(reportOptions.showStrNot ?? true))
                {
                    rfStrNocMonth.Visible = false;
                    chWeeklyTotalsStrNoc.Visible = false;
                    cfWeek1StrNocTotal.Visible = false;
                    cfWeek2StrNocTotal.Visible = false;
                    cfWeek3StrNocTotal.Visible = false;
                    cfWeek4StrNocTotal.Visible = false;
                    cfWeek5StrNocTotal.Visible = false;
                    cfWeek6StrNocTotal.Visible = false;
                }
                // se è richiesto il nascondimento degli arrotondamenti dal totale allora si procede al loro nascondimento
                if (!(reportOptions.showArrot ?? true))
                {
                    rfArrotMonth.Visible = false;
                }

                // se è richiesto di non far vedere i totali per settimana allora si nascondono dal report
                if (!(reportOptions.showWeeklyTotals ?? true))
                {
                    weeklyTotalTileLabel.Visible = false;
                    WeeklyTotalsTable.Visible = false;
                }

                if (!(reportOptions.opzHideSection ?? false))
                {
                    xrLabel1.Visible = false;
                    footerTotTable.Visible = false;
                }
                // gestisce le sezioni da visualizzare nel report
                /*
                if (!(reportOptions.opzHideSection ?? false))
                {
                    Detail.Visible = false;
                    xrTableRow2.Visible = false;
                }
                else if (reportOptions.opzHideSection == 2)
                {
                    File_Col.Visible = false;
                }*/
            }
        }

        /// <summary>
        /// Popola il dizionario con i nomi dei giorni del mese da riportare in testata report e l'elenco del numero di giorni fine settimana all'interno dell'apposita lista.
        /// </summary>
        /// <param name="timesheets">L'elenco degli oggetti timesheet con cui calcolare i dati.</param>
        private void PopulateDayDatas(IEnumerable<TimesheetModuleItem> timesheets)
        {
            DateTime firstMonthDate = !timesheets.Any() ? CommonService.GetFirstMonthDay(DateTime.Today) : timesheets.First().StartDate;
            DateTime lastMonthDate = CommonService.GetLastMonthDay(firstMonthDate);
            foreach (DateTime monthDate in CommonService.EachDay(firstMonthDate, lastMonthDate))
            {
                _groupDaysString.Add(monthDate.Day, String.Format("{0}{1}{2}", monthDate.ToString("dd"), Environment.NewLine, CommonService.GetDayShortName(monthDate)));

                if (monthDate.DayOfWeek == DayOfWeek.Saturday || monthDate.DayOfWeek == DayOfWeek.Sunday)
                    _monthWeekendDays.Add(monthDate.Day);
            }
        }

        /// <summary>
        /// Popola una cella con il suo titolo precedentemente salvato nell'apposito dizionario, utilizzando in ricerca il proprio id.
        /// </summary>
        /// <param name="cell">La cella su cui scrivere il valore.</param>
        private void PopulateCellDayTitle(object cell)
        {
            var currentCell = GetCellFromObject(cell);
            if (currentCell != null)
            {
                int currentDay = GetCurrentDayFromCell(currentCell);
                currentCell.Text = _groupDaysString.ContainsKey(currentDay) ? _groupDaysString[currentDay] : String.Empty;
            }

        }

        /// <summary>
        /// Popola una cella preparata per il totale di ore di giornata con il suo relativo totale.
        /// Viene anche effettuata l'eventuale colorazione dei giorni fine settimana
        /// </summary>
        /// <param name="cell">La cella di cui popolare il valore.</param>
        /// <param name="totalType">Il tipo di totale da inserire nella cella</param>
        private void PopulateDayTotalCell(object cell, TotalTypeEnum totalType)
        {
            // recupero del collaboratore attualmente in processo
            int colId = GetCurrentGroupedColId();

            // recupero della cella da processare
            var currentCell = GetCellFromObject(cell);

            // se la cella è stata reciperata
            if (currentCell != null)
            {
                // recupero il giorno attualmente in elaborazione dal nome della cella
                int currentDay = GetCurrentDayFromCell(currentCell);

                // si procede alla scrittura del valore solamente se il giorno è presente tra gli abilitati del mese
                if (_groupDaysString.ContainsKey(currentDay))
                {
                    // inizializzo il valore del totale da processare
                    double totalValue = 0d;

                    // in base al tipo passato come parametro viene calcolato il totale
                    switch (totalType)
                    {
                        /*
                        case TotalTypeEnum.Total:
                            totalValue = _tsTotalController.GetDayTotalHours(currentDay, colId, null);
                            break;
                        case TotalTypeEnum.Delta:
                            totalValue = _tsTotalController.GetDeltaHours(currentDay, colId, null);
                            break;
                        case TotalTypeEnum.Justification:
                            totalValue = _tsTotalController.GetDayJustificationHours(currentDay, colId, null);
                            break;
                        case TotalTypeEnum.Arrot:
                            totalValue = _tsTotalController.GetDayArrotHours(currentDay, colId, null);
                            break;
                        case TotalTypeEnum.NocturneOvertime:
                            totalValue = _tsTotalController.GetNocturneStrHours(currentDay, colId, null);
                            break;
                        case TotalTypeEnum.Overtime:
                            totalValue = _tsTotalController.GetDayStrHours(currentDay, colId, null);
                            break;
                        case TotalTypeEnum.Ordinary:
                            totalValue = _tsTotalController.GetDayOrdinaryHours(currentDay, colId, null);
                            break;
                        case TotalTypeEnum.Plan:
                            totalValue = _tsTotalController.GetDayPlanHours(currentDay, colId, null);
                            break;
                            */
                    }

                    // impostazione del totale sulla cella (se il risultato è zero ed è richiesto si stampa stringa vuota)
                    if (_showZeroAsEmptyString && CommonService.IsDoubleZero(totalValue))
                        currentCell.Text = String.Empty;
                    else
                        currentCell.Text = totalValue == 0d ? ZeroNormalValue : totalValue.ToString(ToStringTotalFormat);
                }

                ManageWeekendColor(currentDay, currentCell);
            }
        }

        /// <summary>
        /// Gestisce la colorazione della cella per il weekend in base ai dati passati come parametro.
        /// </summary>
        /// <param name="currentDay">Il numero del giorno nel mese (che determina se si tratta di un weekend o meno).</param>
        /// <param name="currentCell">La cella su cui processare l'eventuale colorazione.</param>
        private void ManageWeekendColor(int currentDay, XRTableCell currentCell)
        {
            if (_monthWeekendDays.Contains(currentDay))
                currentCell.BackColor = Color.LightGray;
        }

        /// <summary>
        /// Popola una cella preparata per il totale di ore del mese con il suo relativo totale.
        /// </summary>
        /// <param name="cell">La cella di cui popolare il valore.</param>
        /// <param name="totalType">Il tipo di totale da inserire nella cella</param>
        private void PopulateMonthTotalCell(object cell, TotalTypeEnum totalType)
        {
            // recupero del collaboratore attualmente in processo
            int colId = GetCurrentGroupedColId();

            // recupero della cella corrente
            var currentCell = GetCellFromObject(cell);

            // se la cella risulta valorizzata, in base al totale si imposta il val
            if (currentCell != null)
            {
                // inizializzo il valore del totale da processare
                double totalValue = 0d;

                // in base al tipo passato come parametro viene calcolato il totale
                switch (totalType)
                {
                    /*
                    case TotalTypeEnum.Total:
                        totalValue = _tsTotalController.GetMonthTotalHours(colId, null);
                        break;
                    case TotalTypeEnum.Delta:
                        totalValue = _tsTotalController.GetMonthDeltaHours(colId, null);
                        break;
                    case TotalTypeEnum.Justification:
                        totalValue = _tsTotalController.GetMonthJustificationHours(colId, null);
                        break;
                    case TotalTypeEnum.Arrot:
                        totalValue = _tsTotalController.GetMonthArrotHours(colId, null);
                        break;
                    case TotalTypeEnum.NocturneOvertime:
                        totalValue = _tsTotalController.GetMonthNocturneStrHours(colId, null);
                        break;
                    case TotalTypeEnum.Overtime:
                        totalValue = _tsTotalController.GetMonthStrHours(colId, null);
                        break;
                    case TotalTypeEnum.Ordinary:
                        totalValue = _tsTotalController.GetMonthOrdinaryHours(colId, null);
                        break;
                    case TotalTypeEnum.Plan:
                        totalValue = _tsTotalController.GetMonthPlanHours(colId, null);
                        break;
                        */
                }

                // impostazione del totale sulla cella (se il risultato è zero ed è richiesto si stampa stringa vuota)
                if (_showZeroAsEmptyString && CommonService.IsDoubleZero(totalValue))
                    currentCell.Text = String.Empty;
                else
                    currentCell.Text = CommonService.IsDoubleZero(totalValue) ? ZeroNormalValue : totalValue.ToString(ToStringTotalFormat);
            }
        }

        /// <summary>
        /// Recupera il numero del giorno a partire dal nome della cella passata come parametro.
        /// </summary>
        /// <param name="currentCell">The current cell.</param>
        /// <returns></returns>
        private static int GetCurrentDayFromCell(XRTableCell currentCell)
        {
            return Convert.ToInt32(currentCell.Name.Substring(currentCell.Name.Length - 2, 2));
        }

        /// <summary>
        /// Recupera l'id del collaboratore correntemente raggruppato nel report.
        /// </summary>
        /// <returns>L'identificativo univoco del collaboratore attualmente raggruppato</returns>
        private int GetCurrentGroupedColId()
        {
            int colId = GetCurrentColumnValue<int>("ColId");
            return colId;
        }

        /// <summary>
        /// Recupera la data rappresentante il mese per i dati attualmente in elaborazione dal report.
        /// </summary>
        /// <returns>La data rappresentante il mese per i dati attualmente in elaborazione dal report.</returns>
        private DateTime GetCurrentMonthDate()
        {
            return GetCurrentColumnValue<DateTime>("StartDate");
        }

        /// <summary>
        /// Ritorna le date limite della settimana rispetto al numero settimana specificato nel mese in elaborazione.
        /// </summary>
        /// <param name="weekNumber">il numero della settimana nel mese in elaborazione di cui recuperare le date limite.</param>
        /// <returns>
        /// Nel Item1 viene riportata la data di inzio della settimana (lunedì); 
        /// Nel Item2 viene riportata la data di fine settimana (domenica);
        /// Viene ritornato null in caso no sia presente il numero settimana richiesto.
        /// </returns>
        private Tuple<DateTime, DateTime> GetWeekLimit(int weekNumber)
        {
            // inizializzazione del valore calcolato dal metodo
            Tuple<DateTime, DateTime> returnValues = null;

            // si calcolano tutte le domeniche (chiusura delle settimane) del mese
            IEnumerable<DateTime> monthSundays = GetAllMonthWeekClosures(GetCurrentMonthDate());

            // si procede alla verifica solamente se è presente almeno il numero
            // di settimane richiesto
            if (weekNumber <= monthSundays.Count())
                return new Tuple<DateTime, DateTime>(monthSundays.ElementAt(weekNumber - 1).AddDays(-6), monthSundays.ElementAt(weekNumber - 1));

            // ritorno del valore calcolato dal metodo
            return returnValues;
        }

        /// <summary>
        /// Recupera l'oggetto cella del report a partire dall'oggetto passato come parametro.
        /// </summary>
        /// <param name="cellObject">L'oggetto cella da convertire.</param>
        /// <returns>La cella corrispondere all'oggetto passato come parametro.</returns>
        private static XRTableCell GetCellFromObject(object cellObject)
        {
            return cellObject as XRTableCell;
        }

        /// <summary>
        /// Recuper il riporto ore mese precedente dai dati del collaboratore passato come parametro.
        /// </summary>
        /// <param name="colId">L'id del collaboratore di cui recuperare il monte minuti.</param>
        /// <returns>Il monte minuti mese precedente da visualizzare</returns>
        private double GetLastMonthMinutesAmmount(int colId)
        {
            double returnValue = 0;

            if (_lastMinutesAmmoutByCol.ContainsKey(colId))
                returnValue = _lastMinutesAmmoutByCol[colId];

            return returnValue;

        }

        /// <summary>
        /// Recupera il numero di settimane nel periodo del mese espresso dalla data specificata.
        /// </summary>
        /// <param name="monthDate">La data che rappresenta il mese di cui calcolare il numero di settimane.</param>
        /// <returns>Il numero di settimane nel periodo del mese.</returns>
        private int GetNumberOfCurrentMonthWeek(DateTime monthDate)
        {
            return GetAllMonthWeekClosures(monthDate).Count();
        }

        /// <summary>
        /// Recupera tutti i giorni di chiusura di settimana (domeniche) di un mese specificato.
        /// </summary>
        /// <param name="monthDate">La data che rappresenta il mese.</param>
        /// <returns>L'elenco di tutti i giorni di chiusura di settimana (domeniche) di un mese specificato</returns>
        private IEnumerable<DateTime> GetAllMonthWeekClosures(DateTime monthDate)
        {
            // inizializzazione di inizio e fine mese
            DateTime firstMonthDate = CommonService.GetFirstMonthDay(monthDate);
            DateTime startDate = firstMonthDate;
            DateTime endDate = CommonService.GetLastMonthDay(startDate);
            DateTime lastMonthDate = endDate;

            // eventuale aggiustamento delle date del mese con la chiusura della settimana iniziale e finale
            if (startDate.Date == firstMonthDate && startDate.DayOfWeek != DayOfWeek.Monday)
                startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
            if (endDate.Date == lastMonthDate && endDate.DayOfWeek != DayOfWeek.Sunday)
                endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

            // si calcolano le date del periodo e si ritornano le domeniche
            return CommonService.GetDatesFromPeriod(startDate, endDate).Where(dt => dt.DayOfWeek == DayOfWeek.Sunday);
        }

        /// <summary>
        /// Gestisce le operazioni dell'evento BeforePrint per una riga di totale della settimana.
        /// </summary>
        /// <param name="sender">Il mittente scatenante l'evento.</param>
        /// <param name="e">L'istanza del tipo <see cref="PrintEventArgs"/> contenente i dati dell'evento.</param>
        private void ManageTotalWeekRowPrint(object sender, PrintEventArgs e)
        {
            string currentRowName = ((XRTableRow)sender).Name;
            int weekRowNumber = GetWeekNumberFromName(currentRowName);

            if (weekRowNumber > GetNumberOfCurrentMonthWeek(GetCurrentMonthDate()))
            {
                ((XRTableRow)sender).Visible = false;
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Gestisce le operazioni dell'evento BeforePrint per una cella di visualizzazione degli estremi della settimana.
        /// </summary>
        /// <param name="sender">Il mittente scatenante l'evento.</param>
        /// <param name="e">L'istanza del tipo <see cref="PrintEventArgs"/> contenente i dati dell'evento.</param>
        private void ManageWeekLimitPrint(object sender, PrintEventArgs e)
        {
            // di default si impedisce la stampa della cella
            e.Cancel = true;

            var currentCell = (XRTableCell)sender;
            string currentCellName = currentCell.Name;
            int weekNumber = GetWeekNumberFromName(currentCellName);

            if (weekNumber != 0)
            {
                Tuple<DateTime, DateTime> currentWeekLimit = GetWeekLimit(weekNumber);

                if (currentWeekLimit != null)
                {
                    currentCell.Text = String.Format("{0} - {1}", currentWeekLimit.Item1.ToString(DateFormat)
                                                                , currentWeekLimit.Item2.ToString(DateFormat));

                    // nel caso si riporti il dato si permette la stampa della cella
                    e.Cancel = false;
                }
            }

        }

        /// <summary>
        /// Gestisce le operazioni dell'evento BeforePrint per una cella di visualizzazione di un totale settimanale.
        /// </summary>
        /// <param name="sender">Il mittente scatenante l'evento.</param>
        /// <param name="totalType">Il tipo di totale da impostare</param>
        private void ManageWeekTotal(object sender, TotalTypeEnum totalType)
        {
            var currentCell = (XRTableCell)sender;
            int weekNumber = GetWeekNumberFromName(currentCell.Name);
            Tuple<DateTime, DateTime> weekLimit = GetWeekLimit(weekNumber);

            if (weekLimit != null)
            {

                int currentColId = GetCurrentGroupedColId();
                DateTime lastMonthDate = CommonService.GetLastMonthDay(GetCurrentMonthDate());

                // calcolo del giorno per il mese successivo 
                int currentWeekLimit = weekLimit.Item2.Month == lastMonthDate.AddMonths(1).Month ? 100 + weekLimit.Item2.Day : weekLimit.Item2.Day;

                double totalToShow = 0d;

                switch (totalType)
                {
                    /*
                    case TotalTypeEnum.Total:
                        totalToShow = _tsTotalController.GetWeekTotalHours(currentColId, null, currentWeekLimit, lastMonthDate.Day);
                        break;
                    case TotalTypeEnum.Delta:
                        totalToShow = _tsTotalController.GetWeekDeltaHours(currentColId, null, currentWeekLimit, lastMonthDate.Day);
                        break;
                    case TotalTypeEnum.Justification:
                        totalToShow = _tsTotalController.GetWeekJustificationHours(currentColId, null, currentWeekLimit, lastMonthDate.Day);
                        break;
                    case TotalTypeEnum.NocturneOvertime:
                        totalToShow = _tsTotalController.GetWeekNocturneStrHours(currentColId, null, currentWeekLimit, lastMonthDate.Day);
                        break;
                    case TotalTypeEnum.Overtime:
                        totalToShow = _tsTotalController.GetWeekStrHours(currentColId, null, currentWeekLimit, lastMonthDate.Day);
                        break;
                    case TotalTypeEnum.Ordinary:
                        totalToShow = _tsTotalController.GetWeekOrdinaryHour(currentColId, null, currentWeekLimit, lastMonthDate.Day);
                        break;
                    case TotalTypeEnum.Plan:
                        totalToShow = _tsTotalController.GetWeekPlanHour(currentColId, null, currentWeekLimit, lastMonthDate.Day);
                        break;
                        */
                }

                currentCell.Text = CommonService.IsDoubleZero(totalToShow) ? ZeroNormalValue : totalToShow.ToString(ToStringTotalFormat); ;

            }
        }

        /// <summary>
        /// Ritorna il numero della settimana a partire dal nome dell'elemento di visualizzazione richiesto.
        /// </summary>
        /// <param name="name">Il nome da cui estrarre il numero della settimana.</param>
        /// <returns>Il numero della settimana presente nel nome; se non presente si ritorna 0.</returns>
        private int GetWeekNumberFromName(string name)
        {
            int weekNumber = 0;
            if (name.Contains("k"))
                Int32.TryParse(name.Substring(name.IndexOf("k") + 1, 1), out weekNumber);
            return weekNumber;
        }

        #endregion

        #region Eventi elementi del report

        /// <summary>
        /// Handles the BeforePrint event of the chGgxx control (where xx is the number of the day).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void chGgxx_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateCellDayTitle(sender);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfTotGxx control (where xx is the number of the day).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfTotGxx_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateDayTotalCell(sender, TotalTypeEnum.Total);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfGTot control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfGTot_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateMonthTotalCell(sender, TotalTypeEnum.Total);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfDeltaGxx control (where xx is the number of the day).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfDeltaGxx_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateDayTotalCell(sender, TotalTypeEnum.Delta);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfDeltaTot control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfDeltaTot_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateMonthTotalCell(sender, TotalTypeEnum.Delta);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfJustGxx control (where xx is the number of the day).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfJustGxx_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateDayTotalCell(sender, TotalTypeEnum.Justification);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfJustTot control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfJustTot_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateMonthTotalCell(sender, TotalTypeEnum.Justification);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfJustGxx control (where xx is the number of the day).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfArrotGxx_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateDayTotalCell(sender, TotalTypeEnum.Arrot);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfJustTot control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfArrotTot_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateMonthTotalCell(sender, TotalTypeEnum.Arrot);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfStrNocGxx control (where xx is the number of the day).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfStrNocGxx_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateDayTotalCell(sender, TotalTypeEnum.NocturneOvertime);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfStrNocTot control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfStrNocTot_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateMonthTotalCell(sender, TotalTypeEnum.NocturneOvertime);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfStrGxx control (where xx is the number of the day).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfStrGxx_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateDayTotalCell(sender, TotalTypeEnum.Overtime);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfStrTot control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfStrTot_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateMonthTotalCell(sender, TotalTypeEnum.Overtime);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfOrdinaryGxx control (where xx is the number of the day).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfOrdinaryGxx_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateDayTotalCell(sender, TotalTypeEnum.Ordinary);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfOrdinaryTot control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfOrdinaryTot_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateMonthTotalCell(sender, TotalTypeEnum.Ordinary);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfPlanGxx control  (where xx is the number of the day).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfPlanGxx_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateDayTotalCell(sender, TotalTypeEnum.Plan);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfPlanTot control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfPlanTot_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateMonthTotalCell(sender, TotalTypeEnum.Plan);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfTitleGxx control (where xx is the number of the day).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfTitleGxx_BeforePrint(object sender, PrintEventArgs e)
        {
            PopulateCellDayTitle(sender);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cdGxx control  (where xx is the number of the day).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cdGxx_BeforePrint(object sender, PrintEventArgs e)
        {
            // recupero la cella attualmente in elaborazione
            var currentCell = GetCellFromObject(sender);

            // se la cella è stata trovata
            if (currentCell != null)
            {
                // recupero il giorno attualmente in elaborazione
                int dayNumber = GetCurrentDayFromCell(currentCell);

                // se il giorno non è presente nell'elenco dei giorni da processare si imposta la visualizzazione di una stringa vuota
                if (!_groupDaysString.ContainsKey(dayNumber))
                    currentCell.Text = String.Empty;

                // se è richiesto di visualizzare gli zeri come stringhe vuote ed e la cella corrente è uno zero si converte il valore stampato
                if (_showZeroAsEmptyString && currentCell.Text == ZeroDetailCellValue)
                    currentCell.Text = String.Empty;
                else if (currentCell.Text == ZeroDetailCellValue)
                    currentCell.Text = ZeroNormalValue;

                // gestione della colorazione per il weekend
                ManageWeekendColor(dayNumber, currentCell);
            }
        }

        /// <summary>
        /// Handles the BeforePrint event of the rWeekxxTotal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void rWeekXTotal_BeforePrint(object sender, PrintEventArgs e)
        {
            ManageTotalWeekRowPrint(sender, e);
        }

        /// <summary>
        /// Gestisce gli eventi di stampa del cantiere nel report corrente.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void CantCell_BeforePrint(object sender, PrintEventArgs e)
        {
            // se è richiesto di non stampare il cantiere
            if (!_showCant)
            {
                // recupero la cella attualmente in elaborazione
                var currentCell = GetCellFromObject(sender);

                // se la cella è stata trovata
                if (currentCell != null)
                    currentCell.Text = String.Empty;
            }
        }

        /// <summary>
        /// Handles the BeforePrint event of the xhCellMonteOre control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void xhCellMonteOre_BeforePrint(object sender, PrintEventArgs e)
        {
            // recupero la cella corrente
            var currentCell = GetCellFromObject(sender);

            // imposto sulla cella corrente, se trovata, il valore del monte minuti per il collaboratore corrente
            currentCell.Text = GetLastMonthMinutesAmmount(GetCurrentGroupedColId()).ToString(ToStringTotalFormat);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfWeekX control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfWeekX_BeforePrint(object sender, PrintEventArgs e)
        {
            ManageWeekLimitPrint(sender, e);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfWeek1PlanTotal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfWeekXPlanTotal_BeforePrint(object sender, PrintEventArgs e)
        {
            ManageWeekTotal(sender, TotalTypeEnum.Plan);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfWeek1OrdTotal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfWeekXOrdTotal_BeforePrint(object sender, PrintEventArgs e)
        {
            ManageWeekTotal(sender, TotalTypeEnum.Ordinary);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfWeek1StrTotal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfWeekXStrTotal_BeforePrint(object sender, PrintEventArgs e)
        {
            ManageWeekTotal(sender, TotalTypeEnum.Overtime);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfWeek1StrNocTotal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfWeekXStrNocTotal_BeforePrint(object sender, PrintEventArgs e)
        {
            ManageWeekTotal(sender, TotalTypeEnum.NocturneOvertime);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfWeek1JustTotal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfWeekXJustTotal_BeforePrint(object sender, PrintEventArgs e)
        {
            ManageWeekTotal(sender, TotalTypeEnum.Justification);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfWeek1DeltaTotal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfWeekXDeltaTotal_BeforePrint(object sender, PrintEventArgs e)
        {
            ManageWeekTotal(sender, TotalTypeEnum.Delta);
        }

        /// <summary>
        /// Handles the BeforePrint event of the cfWeek1TotTotal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PrintEventArgs"/> instance containing the event data.</param>
        private void cfWeekXTotTotal_BeforePrint(object sender, PrintEventArgs e)
        {
            ManageWeekTotal(sender, TotalTypeEnum.Total);
        }

        #endregion

    }

}
