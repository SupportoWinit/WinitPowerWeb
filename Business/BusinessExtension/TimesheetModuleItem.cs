using Business.Repository;
using Common;
using Domain;
using log4net;
using Microsoft.Practices.ObjectBuilder2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Business.BusinessExtension
{
    public class TimesheetModuleItem
    {

        #region Private Static Constants

        private static readonly ILog _log = LogManager.GetLogger(typeof(TimesheetModuleItem));

        private const string NoTripHoursOptions = "OPZTS_NO_ORE_VIAGGIO";

        /// <summary>
        /// La chiave per il cartellino diurno
        /// </summary>
        private const string DayTimesheetKey = "Diurno";

        /// <summary>
        /// La chiave per il cartellino notturno
        /// </summary>
        private const string NightTimesheetKey = "Notturno";

        /// <summary>
        /// Il default per la tabella Col_Monte_Minuti a DB
        /// </summary>
        public const int DEFAULT_MONTEMINUTI = -999999999;

        #endregion

        #region Private Fields

        /// <summary>
        /// L'elenco delle ore giorno per giorno riguardanti il mese di competenza dell'oggetto timesheet corrente;
        /// come chiave del dizionario è rappresentato il numero di giorno nel mese (es: 1 sta per il 1° giorno del mese oggetto del cartellino);
        /// come valori è invece utilizzata una touple che ha come valori: il numero di minuti previsti per il giorno che fa da chiave; 
        /// l'ora di inzio del notturno l'ora di fine del notturno (null se il valore non è espresso);
        /// l'ora di fine del notturno (null se il valore non è espresso);
        /// </summary>
        private Dictionary<int, Tuple<double, TimeSpan?, TimeSpan?>> _daysHours = new Dictionary<int, Tuple<double, TimeSpan?, TimeSpan?>>();

        /// <summary>
        /// L'eventuale monte minuti (espresso in minuti) oggetto del timesheet
        /// </summary>
        private int _lastMonthlyHours = 0;

        /// <summary>
        /// Un dizionario contenente l'elenco del dettaglio ore per l'orario corrente;
        /// questo campo è utilizzato solamente in caso l'orario rappresenti un piano e si accede dall'esterno tramite un metodo pubblico.
        /// </summary>
        private Dictionary<int, List<KeyValuePair<TimeSpan, TimeSpan>>> _planDaysDetail = new Dictionary<int, List<KeyValuePair<TimeSpan, TimeSpan>>>();

        #endregion

        #region Private Static Fieds

        /// <summary>
        /// L'elenco delle opzioni attivate dall'utente nel calcolo del cartellino
        /// </summary>
        private static List<string> _timesheetOptions;
        #endregion

        #region Public Static Field

        /// <summary>
        /// Variabile statica utilizzata per la gestione di un id fittizzio nella visualizzazione a video degli elementi cartellino
        /// </summary>
        public static long Counter = 0;

        #endregion

        #region Private Static Properties

        /// <summary>
        /// Recupera o imposta l'elenco delle opzioni attivate dall'utente nel calcolo del cartellino.
        /// </summary>
        /// <value>
        /// L'elenco delle opzioni attivate dall'utente nel calcolo del cartellino.
        /// </value>
        private static List<string> TimesheetOptions
        {
            get { return _timesheetOptions ?? (_timesheetOptions = new List<string>()); }
            set { _timesheetOptions = value; }
        }

        #endregion

        #region Costructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TimesheetModuleItem"/> class.
        /// </summary>
        /// <param name="isDecimalHours">Se impostato a <c>true</c> le ore saranno visualizzate in formato decimale; in caso di <c>false</c> saranno visualizzate in formato sessantesimi.</param>
        public TimesheetModuleItem(bool isDecimalHours)
        {
            // impostazione dell'id fittizio di identificazione dell'oggetto di visualizzazione del cartellino
            ID = Counter++;

            // impostazione della visualizzazione o meno in formato decimale/sessantesimi dei totali per giornos
            IsDecimalHours = isDecimalHours;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Recupera o imposta l'identificativo (fittizio, non utilizzato nel database) del corrente oggetto timehseet.
        /// </summary>
        /// <value>
        /// L'identificativo (fittizzio, non utilizzato nel database) del corrente oggetto timehseet.
        /// </value>
        public long ID { get; set; }

        /// <summary>
        /// Recupera o imposta l'id del cantiere a cui fa riferimento il corrente oggetto di timesheet.
        /// </summary>
        /// <value>
        /// L'id del cantiere a cui fa riferimento il corrente oggetto di timesheet.
        /// </value>
        public int CantId { get; set; }

        /// <summary>
        /// Recupera o imposta il codice del cantiere a cui fa riferimento il corrente oggetto di timesheet.
        /// </summary>
        /// <value>
        /// Il codice del cantiere a cui fa riferimento il corrente oggetto di timesheet.
        /// </value>
        public string CantMnemonic { get; set; }

        /// <summary>
        /// Recupera o imposta la descrizione del cantiere a cui fa riferimento il corrente oggetto di timesheet.
        /// </summary>
        /// <value>
        /// La descrizione del cantiere a cui fa riferimento il corrente oggetto di timesheet.
        /// </value>
        public string CantDesc { get; set; }

        /// <summary>
        /// Recupera o imposta l'id del collaboratore a cui fa riferimento il corrente oggetto di timesheet.
        /// </summary>
        /// <value>
        /// L'id del collaboratore a cui fa riferimento il corrente oggetto di timesheet.
        /// </value>
        public int ColId { get; set; }

        /// <summary>
        /// Recupera o imposta il codice del collaboratore a cui fa riferimento il corrente oggetto di timesheet.
        /// </summary>
        /// <value>
        /// Il codice del collaboratore a cui fa riferimento il corrente oggetto di timesheet.
        /// </value>
        public string ColMnemonic { get; set; }

        /// <summary>
        /// Recupera o imposta la descrizione del collaboratore a cui fa riferimento il corrente oggetto di timesheet.
        /// </summary>
        /// <value>
        /// La descrizione del collaboratore a cui fa riferimento il corrente oggetto di timesheet.
        /// </value>
        public string ColDesc { get; set; }

        /// <summary>
        /// Recupera o imposta la motificazione a cui fa riferimento il corrente oggetto di timesheet.
        /// </summary>
        /// <value>
        /// La motivazione a cui fa riferimento il corrente oggetto di timesheet.
        /// </value>
        public string Justification { get; set; }

        /// <summary>
        /// Recupera o imposta la data che determina il periodo (mese/anno) di competenza del corrente oggetto di timesheet.
        /// </summary>
        /// <value>
        /// La data che determina il periodo (mese/anno) di competenza del corrente oggetto di timesheet.
        /// </value>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Recupera o imposta il tipo di motivazione (piano, ore ecc.) che determina cosa il corrente oggetto di timesheet visualizza.
        /// </summary>
        /// <value>
        /// Il tipo di motivfazione (piano, ore ecc.) che determina cosa il corrente oggetto di timesheet visualizza.
        /// </value>
        public JustificationTypeEnum JustificationType
        {
            get
            {

                switch (Justification)
                {
                    case "Plan":
                        return JustificationTypeEnum.Plan;
                    case null:
                        return JustificationTypeEnum.None;
                    default:
                        return JustificationTypeEnum.Just;
                }
            }
        }

        /// <summary>
        /// Recupera o imposta il valore che identifica il formato di output del numero di ore per giorno; <c>true</c> per i centesimi e <c>false</c> per i sessantesimi.
        /// </summary>
        /// <value>
        /// Il valore che identifica il formato di output del numero di ore per giorno; <c>true</c> per i centesimi e <c>false</c> per i sessantesimi.
        /// </value>
        public bool IsDecimalHours { get; set; }

        /// <summary>
        /// Recupera o imposta l'ordine di visualizzazione all'interno del collaboratore di riferimento per l'oggetto timesheet corrente.
        /// </summary>
        /// <value>
        /// L'ordine di visualizzazione all'interno del collaboratore di riferimento per l'oggetto timesheet corrente.
        /// </value>
        public int Order { get; set; }

        /// <summary>
        /// Recupera o imposta l'elenco delle ore giorno per giorno riguardanti il mese di competenza dell'oggetto timesheet corrente;
        /// come chiave del dizionario è rappresentato il numero di giorno nel mese (es: 1 sta per il 1° giorno del mese oggetto del cartellino);
        /// come valori è invece utilizzata una touple che ha come valori: il numero di minuti previsti per il giorno che fa da chiave; 
        /// l'ora di inzio del notturno l'ora di fine del notturno (null se il valore non è espresso);
        /// l'ora di fine del notturno (null se il valore non è espresso);
        /// l'ora di fine del notturno (null se il valore non è espresso);
        /// </summary>
        /// <value>
        /// L'elenco delle ore giorno per giorno riguardanti il mese di competenza dell'oggetto timesheet corrente;
        /// come chiave del dizionario è rappresentato il numero di giorno nel mese (es: 1 sta per il 1° giorno del mese oggetto del cartellino);
        /// come valori è invece utilizzata una touple che ha come valori: il numero di minuti previsti per il giorno che fa da chiave; 
        /// l'ora di inzio del notturno l'ora di fine del notturno (null se il valore non è espresso);
        /// l'ora di fine del notturno (null se il valore non è espresso);
        /// l'ora di fine del notturno (null se il valore non è espresso);
        /// </value>
        public Dictionary<int, Tuple<double, TimeSpan?, TimeSpan?>> DaysHours
        {
            get
            {
                return _daysHours;
            }

            set
            {
                _daysHours = value;
            }
        }

        /// <summary>
        /// Recupera il totale delle ore registrate all'interno della proprietà DaysHours.
        /// </summary>
        /// <value>
        /// Il totale delle ore registrate all'interno della proprietà DaysHours.
        /// </value>
        public double TotalHours
        {
            get
            {
                int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheeetTotalWithoutMonthlyMinutes);

                int lastMonthlyMinutesToSum = (TimesheeetTotalWithoutMonthlyMinutes)customizationVersion == TimesheeetTotalWithoutMonthlyMinutes.Show
                    ? Convert.ToInt32(LastMonthlyMinutes)
                    : 0;

                return CommonService.GetDoubleFromMinutes(DaysHours.Select(dh => Convert.ToInt32(dh.Value.Item1)).Sum() + lastMonthlyMinutesToSum, IsDecimalHours);
            }
        }

        /// <summary>
        /// Recupera il totale delle ore registrate all'interno della proprietà DaysHours.
        /// </summary>
        /// <value>
        /// Il totale delle ore registrate all'interno della proprietà DaysHours.
        /// </value>
        public double TotalHoursWithoutPrevNextWeek
        {
            get
            {
                int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheeetTotalWithoutMonthlyMinutes);

                int lastMonthlyMinutesToSum = (TimesheeetTotalWithoutMonthlyMinutes)customizationVersion == TimesheeetTotalWithoutMonthlyMinutes.Show
                    ? Convert.ToInt32(LastMonthlyMinutes)
                    : 0;

                return CommonService.GetDoubleFromMinutes(DaysHours.Where(d => d.Key > 0 && d.Key < 100).Select(dh => Convert.ToInt32(dh.Value.Item1)).Sum() + lastMonthlyMinutesToSum, IsDecimalHours);
            }
        }

        /// <summary>
        /// Recupera il numero totale di minuti per il mese rappresentato dall'oggetto cartellino corrente.
        /// </summary>
        /// <value>
        /// Il numero totale di minuti per il mese rappresentato dall'oggetto cartellino corrente.
        /// </value>
        public int TotalMinutes
        {
            get
            {
                return DaysHours.Select(dh => Convert.ToInt32(dh.Value.Item1)).Sum() + Convert.ToInt32(LastMonthlyMinutes);
            }
        }

        /// <summary>
        /// Recupera il numero totale di minuti per il mese rappresentato dall'oggetto cartellino corrente senza il precedente monte minuti.
        /// Utilizzato nel caloclo del monte minuti stesso
        /// </summary>
        /// <value>
        /// Il numero totale di minuti per il mese rappresentato dall'oggetto cartellino corrente senza il precedente monte minuti.
        /// Utilizzato nel caloclo del monte minuti stesso
        /// </value>
        public int TotalMinutesForMonthlyMinutes
        {
            get
            {
                return DaysHours.Select(dh => Convert.ToInt32(dh.Value.Item1)).Sum();
            }
        }

        /// <summary>
        /// Recupera il numero di giorni con ore tra i dati immagazzinati per il mese settato.
        /// </summary>
        /// <value>
        /// Il numero di giorni con ore tra i dati immagazzinati per il mese settato.
        /// </value>
        public int TotalDays
        {
            get
            {
                return DaysHours.Count(dh => dh.Value.Item1 > 0 || dh.Value.Item1 < 0);
            }
        }

        /// <summary>
        /// Recupera o imposta il valore che indica se l'oggetto corrente proviene da un orario standard oppure inserito per collaborartore mese
        /// nella Col_Orari.
        /// </summary>
        /// <value>
        /// <c>true</c> l'oggetto corrente proviene da un orario inserito per collaboratore/mese nella Col_Orari; altrimenti, <c>false</c>.
        /// </value>
        public bool IsFromFreeTimeSheet { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che indica l'id del'orario rappresentato dall'oggetto corrente nella Col_Orari;
        /// questo valore ha senso solamente se il valore di <see cref="IsFromFreeTimeSheet"/> è <c>true</c>.
        /// </summary>
        /// <value>
        /// Il valore che indica l'id del'orario rappresentato dall'oggetto corrente nella Col_Orari;
        /// questo valore ha senso solamente se il valore di <see cref="IsFromFreeTimeSheet"/> è <c>true</c>.
        /// </value>
        public int FreeTimeSheetId { get; set; }

        /// <summary>
        /// Recupera l'oa di inizio del notturno per l'oggetto cartellino corrente.
        /// </summary>
        /// <value>
        /// L'oa di inizio del notturno per l'oggetto cartellino corrente.
        /// </value>
        public TimeSpan? NocturnsStartHour
        {
            get
            {
                TimeSpan? returnValue = null;

                if (DaysHours.Any())
                {
                    returnValue = DaysHours.FirstOrDefault().Value.Item2;
                }

                return returnValue;
            }
        }

        /// <summary>
        /// Recupera l'oa di fine del notturno per l'oggetto cartellino corrente.
        /// </summary>
        /// <value>
        /// L'oa di fine del notturno per l'oggetto cartellino corrente.
        /// </value>
        public TimeSpan? NocturnEndHour
        {
            get
            {
                TimeSpan? returnValue = null;

                if (DaysHours.Any())
                {
                    returnValue = DaysHours.FirstOrDefault().Value.Item3;
                }

                return returnValue;
            }
        }

        /// <summary>
        /// Recupera l'eventuale monte minuti (formato ore decimali o sessantesimi) per il mese/collaboratore oggetto del timesheet.
        /// </summary>
        /// <value>
        /// L'eventuale monte minuti (formato ore decimali o sessantesimi) per il mese/collaboratore oggetto del timesheet.
        /// </value>
        public double LastMonthlyHours
        {
            get
            {
                return CommonService.GetDoubleFromMinutes(_lastMonthlyHours, IsDecimalHours);
            }
        }

        /// <summary>
        /// Recupera l'eventuale monte minuti (in minuti) per il mese/collaboratore oggetto del timesheet.
        /// </summary>
        /// <value>
        /// L'eventuale monte minuti (in minuti) per il mese/collaboratore oggetto del timesheet.
        /// </value>
        public double LastMonthlyMinutes
        {
            get
            {
                return _lastMonthlyHours;
            }
        }

        /// <summary>
        /// Recupera il totale della prima settimana espressa dal timesheet.
        /// </summary>
        /// <value>
        /// Il totale della prima settimana espressa dal timesheet.
        /// </value>
        public double TotalWeek1
        {
            get
            {
                return CommonService.GetDoubleFromMinutes(GetWeekTotal(1), IsDecimalHours);
            }
        }

        /// <summary>
        /// Recupera il totale della seconda settimana espressa dal timesheet.
        /// </summary>
        /// <value>
        /// Il totale della seconda settimana espressa dal timesheet.
        /// </value>
        public double TotalWeek2
        {
            get
            {
                return CommonService.GetDoubleFromMinutes(GetWeekTotal(2), IsDecimalHours);
            }
        }

        /// <summary>
        /// Recupera il totale della terza settimana espressa dal timesheet.
        /// </summary>
        /// <value>
        /// Il totale della terza settimana espressa dal timesheet.
        /// </value>
        public double TotalWeek3
        {
            get
            {
                return CommonService.GetDoubleFromMinutes(GetWeekTotal(3), IsDecimalHours);
            }
        }

        /// <summary>
        /// Recupera il totale della quarta settimana espressa dal timesheet.
        /// </summary>
        /// <value>
        /// Il totale della quarta settimana espressa dal timesheet.
        /// </value>
        public double TotalWeek4
        {
            get
            {
                return CommonService.GetDoubleFromMinutes(GetWeekTotal(4), IsDecimalHours);
            }
        }

        /// <summary>
        /// Recupera il totale della quinta settimana espressa dal timesheet.
        /// </summary>
        /// <value>
        /// Il totale della quinta settimana espressa dal timesheet.
        /// </value>
        public double TotalWeek5
        {
            get
            {
                return CommonService.GetDoubleFromMinutes(GetWeekTotal(5), IsDecimalHours);
            }
        }

        /// <summary>
        /// Recupera il totale della sesta settimana espressa dal timesheet.
        /// </summary>
        /// <value>
        /// Il totale della sesta settimana espressa dal timesheet.
        /// </value>
        public double TotalWeek6
        {
            get
            {
                return CommonService.GetDoubleFromMinutes(GetWeekTotal(6), IsDecimalHours);
            }
        }


        #region Proprietà utilizzate per la visualizzazione dei dati dei giorni nel cartellino

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per l'ultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per l'ultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayMinus1
        {
            get
            {
                return GetHoursValue(-1, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(-1, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il penultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il penultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayMinus2
        {
            get
            {
                return GetHoursValue(-2, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(-2, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il terzultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il terzultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayMinus3
        {
            get
            {
                return GetHoursValue(-3, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(-3, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il quartultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il quartultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayMinus4
        {
            get
            {
                return GetHoursValue(-4, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(-4, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il quintultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il quintultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayMinus5
        {
            get
            {
                return GetHoursValue(-5, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(-5, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il sestultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il sestultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayMinus6
        {
            get
            {
                return GetHoursValue(-6, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(-6, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il settultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il settultimo giorno del mese precedente rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayMinus7
        {
            get
            {
                return GetHoursValue(-7, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(-7, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 1° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 1° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day01
        {
            get
            {
                return GetHoursValue(1, IsDecimalHours);
            }

            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(1, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 2° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 2° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day02
        {
            get
            {
                return GetHoursValue(2, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(2, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 3° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 3° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day03
        {
            get
            {
                return GetHoursValue(3, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(3, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 4° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 4° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day04
        {
            get
            {
                return GetHoursValue(4, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(4, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 5° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 5° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day05
        {
            get
            {
                return GetHoursValue(5, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(5, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 6° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 6° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day06
        {
            get
            {
                return GetHoursValue(6, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(6, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 7° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 7° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day07
        {
            get
            {
                return GetHoursValue(7, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(7, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 8° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 8° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day08
        {
            get
            {
                return GetHoursValue(8, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(8, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 9° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 9° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day09
        {
            get
            {
                return GetHoursValue(9, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(9, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 10° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 10° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day10
        {
            get
            {
                return GetHoursValue(10, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(10, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 11° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 11° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day11
        {
            get
            {
                return GetHoursValue(11, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(11, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 12° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 12° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day12
        {
            get
            {
                return GetHoursValue(12, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(12, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 13° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 13° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day13
        {
            get
            {
                return GetHoursValue(13, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(13, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 14° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 14° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day14
        {
            get
            {
                return GetHoursValue(14, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(14, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 15° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 15° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day15
        {
            get
            {
                return GetHoursValue(15, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(15, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 16° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 16° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day16
        {
            get
            {
                return GetHoursValue(16, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(16, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 17° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 17° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day17
        {
            get
            {
                return GetHoursValue(17, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(17, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 18° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 18° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day18
        {
            get
            {
                return GetHoursValue(18, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(18, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 19° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 19° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day19
        {
            get
            {
                return GetHoursValue(19, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(19, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 20° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 20° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day20
        {
            get
            {
                return GetHoursValue(20, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(20, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 21° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 21° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day21
        {
            get
            {
                return GetHoursValue(21, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(21, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 22° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 22° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day22
        {
            get
            {
                return GetHoursValue(22, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(22, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 23° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 23° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day23
        {
            get
            {
                return GetHoursValue(23, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(23, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 24° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 24° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day24
        {
            get
            {
                return GetHoursValue(24, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(24, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 25° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 25° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day25
        {
            get
            {
                return GetHoursValue(25, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(25, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 26° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 26° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day26
        {
            get
            {
                return GetHoursValue(26, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(26, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 27° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 27° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day27
        {
            get
            {
                return GetHoursValue(27, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(27, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 28° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 28° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day28
        {
            get
            {
                return GetHoursValue(28, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(28, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 29° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 29° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day29
        {
            get
            {
                return GetHoursValue(29, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(29, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 30° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 30° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day30
        {
            get
            {
                return GetHoursValue(30, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(30, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 31° giorno del mese oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il 31° giorno del mese oggetto del timesheet.
        /// </value>
        public double Day31
        {
            get
            {
                return GetHoursValue(31, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(31, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il primo giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il primo giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayPlus1
        {
            get
            {
                return GetHoursValue(101, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(101, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il secondo giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il secondo giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayPlus2
        {
            get
            {
                return GetHoursValue(102, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(102, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il terzo giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il terzo giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayPlus3
        {
            get
            {
                return GetHoursValue(103, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(103, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il quarto giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il quarto giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayPlus4
        {
            get
            {
                return GetHoursValue(104, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(104, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il quinto giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il quinto giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayPlus5
        {
            get
            {
                return GetHoursValue(105, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(105, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il sesto giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il sesto giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayPlus6
        {
            get
            {
                return GetHoursValue(106, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(106, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        /// <summary>
        /// Recupera il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il settimo giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </summary>
        /// <value>
        /// Il numero di ore (nel formato specificato dalla proprietà IsDecimalHours) per il settimo giorno del mese successivo rispetto a quello dell'oggetto del timesheet.
        /// </value>
        public double DayPlus7
        {
            get
            {
                return GetHoursValue(107, IsDecimalHours);
            }
            set
            {
                double trunc = Math.Truncate(value);
                SetHoursValue(107, (trunc * 60) + ((value - trunc) * 100));
            }
        }

        #endregion

        #region Proprietà utilizzate per la modifica in timesheet module

        public double DayMinus7Edit { get; set; }
        public double DayMinus6Edit { get; set; }
        public double DayMinus5Edit { get; set; }
        public double DayMinus4Edit { get; set; }
        public double DayMinus3Edit { get; set; }
        public double DayMinus2Edit { get; set; }
        public double DayMinus1Edit { get; set; }
        public double Day01Edit { get; set; }
        public double Day02Edit { get; set; }
        public double Day03Edit { get; set; }
        public double Day04Edit { get; set; }
        public double Day05Edit { get; set; }
        public double Day06Edit { get; set; }
        public double Day07Edit { get; set; }
        public double Day08Edit { get; set; }
        public double Day09Edit { get; set; }
        public double Day10Edit { get; set; }
        public double Day11Edit { get; set; }
        public double Day12Edit { get; set; }
        public double Day13Edit { get; set; }
        public double Day14Edit { get; set; }
        public double Day15Edit { get; set; }
        public double Day16Edit { get; set; }
        public double Day17Edit { get; set; }
        public double Day18Edit { get; set; }
        public double Day19Edit { get; set; }
        public double Day20Edit { get; set; }
        public double Day21Edit { get; set; }
        public double Day22Edit { get; set; }
        public double Day23Edit { get; set; }
        public double Day24Edit { get; set; }
        public double Day25Edit { get; set; }
        public double Day26Edit { get; set; }
        public double Day27Edit { get; set; }
        public double Day28Edit { get; set; }
        public double Day29Edit { get; set; }
        public double Day30Edit { get; set; }
        public double Day31Edit { get; set; }
        public double DayPlus1Edit { get; set; }
        public double DayPlus2Edit { get; set; }
        public double DayPlus3Edit { get; set; }
        public double DayPlus4Edit { get; set; }
        public double DayPlus5Edit { get; set; }
        public double DayPlus6Edit { get; set; }
        public double DayPlus7Edit { get; set; }

        #endregion

        #endregion

        #region Public Methods


        /// <summary>
        /// Popola le ore previste nel cartellino utilizzando i dati espressi in giorni (nella classe sono espressi indicando il numero del giorno nel mese).
        /// </summary>
        /// <param name="datesToInsert">Un dizionario la cui chiave è la data da inserire e il valore è una tuple che contiene il numero di minuti previsti per quella giornata,
        /// l'ora di inizio notturno e l'ora di fine notturno.</param>
        public void PopulateHoursWithDate(Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>> datesToInsert)
        {
            // generazione del nuovo oggetto che andrà a popolare i giorni dell'oggetto timesheet attuale
            var newDaysHours = new Dictionary<int, Tuple<double, TimeSpan?, TimeSpan?>>();

            // per ogni data passata come parametro
            foreach (var dayHours in datesToInsert)
            {
                // se il mese e l'anno che si stanno processando sono gli stessi, si procede al relativo inserimento
                // nei campi standard; altrimenti, se si tratta del mese successivo a quello in elaborazione allora 
                // si sta trattando un timesheet con totali settimanali e si aggiunge il dato con valore dal 100 in poi;
                // altrimenti se si tratta del mese precedente a quello in elaborazione allora si sta trattando
                // un timesheet con totali settimanali e si aggiunge il dato con valore negativo
                if (dayHours.Key.Month == StartDate.Month && dayHours.Key.Year == StartDate.Year)
                {
                    // aggiunta del numero di ore nell'oggetto timesheet corrente
                    newDaysHours.Add(dayHours.Key.Day, dayHours.Value);
                }
                else if (dayHours.Key.Month == StartDate.AddMonths(1).Month && dayHours.Key.Year == StartDate.AddMonths(1).Year)
                {
                    // per il mese successivo si riporta il giorno più cento;
                    // cosi p.e. il 1° giorno del mese successivo sia 101
                    newDaysHours.Add(dayHours.Key.Day + 100, dayHours.Value);
                }
                else if (dayHours.Key.Month == StartDate.AddMonths(-1).Month && dayHours.Key.Year == StartDate.AddMonths(-1).Year)
                {
                    // per il mese precedente si utilizzano i numeri negativi;
                    // così p.e. l'ultimo giorno del mese precedente è -1

                    // si recupera l'ultimo giorno del mese da inserire
                    int lastMonthDay = CommonService.GetLastMonthDay(dayHours.Key).Day;

                    // per ottenere il numero negativo si sottrae il giorno in processo
                    // all'ultimo giorno del mese + 1
                    newDaysHours.Add(dayHours.Key.Day - (lastMonthDay + 1), dayHours.Value);
                }

            }

            // inserimento delle ore calcolate all'interno dell'oggetto timesheet corrente
            DaysHours = newDaysHours;

        }

        public void PopulateHoursWithDateNew(List<TimeSpan> ore, List<DateTime> giorni)
        {
            // generazione del nuovo oggetto che andrà a popolare i giorni dell'oggetto timesheet attuale
            var newDaysHours = new Dictionary<int, Tuple<double, TimeSpan?, TimeSpan?>>();
            int i = 0;

            // per ogni data passata come parametro
            foreach (var day in giorni)
            {
                int j = 0;
                foreach (var hours in ore)
                {
                    //controllo di essere nel punto giusto della lista
                    if (i == j)
                    {
                        //se ho un valore lo inserisco nella tabella altrimenti inserisco un valore a zero
                        if (hours.TotalMinutes > 0)
                        {
                            Tuple<double, TimeSpan?, TimeSpan?> prova1 = new Tuple<double, TimeSpan?, TimeSpan?>(hours.TotalMinutes, null, null);
                            newDaysHours.Add(day.Day, prova1);
                        }
                        else
                        {
                            Tuple<double, TimeSpan?, TimeSpan?> prova1 = new Tuple<double, TimeSpan?, TimeSpan?>(0, null, null);
                            newDaysHours.Add(day.Day, prova1);
                        }
                    }
                    j++;
                }
                i++;
            }

            // inserimento delle ore calcolate all'interno dell'oggetto timesheet corrente
            DaysHours = newDaysHours;

        }

        /// <summary>
        /// Metodo che inserisce nell'oggetto timesheet corrente i valori del collaboratore passato come parametro.
        /// </summary>
        /// <param name="colIdToInsert">L'id del collaboratore da cui estrarre i dati da inserire.</param>
        public void InsertColValues(int colIdToInsert)
        {
            // se è stato passato un codice collaboratore valido
            if (colIdToInsert != 0)
            {
                // recupero dal repository il collaboratore corrente
                var currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == colIdToInsert);

                // inserimento nell'oggetto corrente dei dati del collaboratore
                ColId = colIdToInsert;
                ColMnemonic = currentCol.Codice_Collaboratore;
                ColDesc = currentCol.CognomeNome_Col;
            }
        }

        /// <summary>
        /// Inserisce nell'oggetto timesheet corrente i valori del cantiere il cui id è passato come parametro.
        /// </summary>
        /// <param name="cantIdToInsert">L'id del cantiere da inserire; se l'id è 0 l'operazione di inserimento non viene effettuata.</param>
        public void InsertCantValues(int cantIdToInsert)
        {
            // se è stato passato un codice cantiere valido
            if (cantIdToInsert != 0)
            {
                // inserisco l'id del cantiere nell'oggetto corrente
                CantId = cantIdToInsert;

                // recupero dal repository il cantiere corrente
                var currentCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == cantIdToInsert);

                // con il cantiere recuperato dal repository allora provvedo all'inserimento dei dati di cantiere
                CantDesc = currentCant != null ? currentCant.Descrizione_Can : String.Empty;
                CantMnemonic = currentCant != null ? currentCant.Codice_Cantiere : String.Empty;
            }
        }

        /// <summary>
        /// Metodo che calcola e imposta sul timesheet corrente il monte minuti residuo all'ultimo mese,
        /// recuperando il dato salvato nel database.
        /// </summary>
        public void SetLastMonthlyMinutes(bool addBlockedDateMinutes)
        {
            // si prosegue con l'elaborazione solamente se il timesheet corrente ha collegato un collaboratore
            if (ColId != 0)
            {
                // se la gestione dei monte minuti è disabilitata o il collaboratore oggetto del timesheet è escluso dalla gestione
                // del monte minuti allora il valore viene impostato a 0
                if (RepoManager.ParamRepo.ParametersRow.Abilita_Monte_Minuti && RepoManager.ParamRepo.ParametersRow.Flag_Monte_Ore != (int)MothlyHoursEnum.None)
                {
                    // recupero il collaboratore collegato a questo timesheet
                    Col currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == ColId);

                    // se è stato trovato un collaboratore
                    if (currentCol != default(Col))
                    {
                        // verifico che il collaboratore rientri nella gestione dei monte minuti
                        // e se è così, procedo al calcolo
                        if ((RepoManager.ParamRepo.ParametersRow.Flag_Monte_Ore == (int)MothlyHoursEnum.Inclusive && currentCol.Flag_Monte_Ore)
                            || (RepoManager.ParamRepo.ParametersRow.Flag_Monte_Ore == (int)MothlyHoursEnum.Exclusive && !currentCol.Flag_Monte_Ore))
                        {
                            // si procede al calcolo del monte ore seguendo questa logica:
                            // a. Se il mese in fase di processo è maggiore o uguale alla data blocco
                            //   1. fino alla data blocco il monte ore è indicato nella scheda collaboratore
                            //   2. dalla data blocco in poi il dato è calcolato dalle timbrature
                            // b. Se il mese in fase di processo è minore della da blocco
                            //   1. Si caclola il monte minuti pregresso sommando il totale del monte minuti salvato nella tabella Col_Monte_Minuti

                            // inizializzazione del monte minuti accumulato
                            int lastMonthMinutes = 0;

                            // calcolo della data blocco (se vuota si prende la data minima disponibile
                            DateTime blockedDate = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg ?? DateTime.MinValue;

                            // se si sta processando un mese dopo la data blocco
                            // dalla data di blocco in poi il monte minuti va calcolato "al volo"
                            if (StartDate >= blockedDate)
                            {
                                // fino alla data blocco il monte minuti è dato dal valore salvato sul collaboraotre
                                //Inizializzazione del valore minimo della registrazione
                                DateTime firstRegVDate = DateTime.MinValue;

                                // e per evitare di ciclare a vuoto, se la data blocco è antecedente alla data della prima reg_v nel database
                                // allora si parte dalla prima data reg

                                if (blockedDate == DateTime.MinValue)
                                    //viene estratta la data minima delle registrazioni
                                    firstRegVDate = Convert.ToDateTime(RepoManager.Reg_VRepo.Min(regv => regv.Data_Reg));

                                //se la data blocco è minore della data minima allora allora come nuova data blocco viene impostata la data minima
                                if (blockedDate < firstRegVDate)
                                    blockedDate = firstRegVDate;

                                //if (addBlockedDateMinutes)
                                //{


                                //    foreach (DateTime firstMonthDay in CommonService.EachMonth(blockedDate, StartDate.AddDays(-1)))
                                //    {
                                //        lastMonthMinutes += TimesheetModuleItem.GetLastMonthMinutesAmmount(currentCol, firstMonthDay, CommonService.GetLastMonthDay(firstMonthDay), false);
                                //    }
                                //}


                                if ((StartDate.Month == blockedDate.AddMonths(1).Month) && (StartDate.Year == blockedDate.AddMonths(1).Year))
                                {
                                    lastMonthMinutes = currentCol.Monte_Minuti;
                                }
                                else if ((StartDate.Month == blockedDate.AddMonths(2).Month) && (StartDate.Year == blockedDate.AddMonths(2).Year))
                                    //viene calcolato l'ultimo monte minuti dato dalla somma akgebrica del riporto ore mese precedentie  il delta calcolato
                                    lastMonthMinutes += TimesheetModuleItem.GetLastMonthMinutesAmmount(currentCol, blockedDate, StartDate.AddDays(-1), false) + currentCol.Monte_Minuti;

                                else
                                {
                                    lastMonthMinutes += TimesheetModuleItem.GetLastMonthMinutesAmmount(currentCol, blockedDate, StartDate.AddDays(-1), false);
                                }



                                if ((StartDate.Month == blockedDate.AddMonths(1).Month) && (StartDate.Year == blockedDate.AddMonths(1).Year))
                                {
                                    CommonService.residualMonthMinutes = lastMonthMinutes;
                                }

                                else if ((StartDate.Month == blockedDate.AddMonths(2).Month) && (StartDate.Year == blockedDate.AddMonths(2).Year))
                                    CommonService.residualMonthMinutes = lastMonthMinutes;

                                else
                                {
                                    CommonService.residualMonthMinutes += lastMonthMinutes;
                                }

                                // imposto il valore calcolato del rimasto a fine mese sulla proprietà apposita
                                _lastMonthlyHours = CommonService.residualMonthMinutes;
                            }

                            #region CALCOLO CARTELLINO ANTECEDENTE DATA BLOCCO
                            else // se si sta processando un mese antencedente alla data blocco
                            {
                                // dalla alla data blocco il monte minuti è dato dal valore salvato sul collaboraotre
                                lastMonthMinutes = currentCol.Monte_Minuti;

                                // allora il monte minuti è la sottrazione di quanto salvato alla data blocco nel collaboratore fino all'inzio mese richiesto
                                IEnumerable<Col_Monte_Minuti> monthMinutes = RepoManager.Col_Monte_MinutiRepo.Find(monthMin => monthMin.Col_Id == currentCol.Col_Id && monthMin.Anno_Col_Monte_Minuti <= blockedDate.Year && monthMin.Anno_Col_Monte_Minuti >= StartDate.Year).ToList();

                                // se sono stati trovati dei dati salvati nel col_monte_minuti
                                if (monthMinutes.Any())
                                {
                                    // ciclo su ogni monte minuti recuperato e sommo i minuti accumulati fino
                                    // al mese in processo
                                    foreach (var monthMinute in monthMinutes.OrderByDescending(mm => mm.Anno_Col_Monte_Minuti))
                                    {
                                        // calcolo i mesi da sommare al monte minuti per i monte minuti recuerati:
                                        //   1. se l'anno del monte minuti in elaborazione è maggiore dell'anno in processo ma diverso dalla data blocco allora sommo tutto l'anno al monte minuti
                                        //   2. se l'anno del monte minuti in elaborazione è maggiore dell'anno in processo ma uguale alla data blocco, si prende dal mese della data blocco fino a inizio anno
                                        //   3. altrimenti (in caso di anno uguale quindi) si prende dalla fine dell'anno fino al mese in processo
                                        int upperMonthBound = 12;
                                        int lowerMonthBound = StartDate.Month;


                                        if (monthMinute.Anno_Col_Monte_Minuti > StartDate.Year && blockedDate.Year != monthMinute.Anno_Col_Monte_Minuti)
                                        {
                                            upperMonthBound = 12;
                                            lowerMonthBound = 1;
                                        }
                                        else if (monthMinute.Anno_Col_Monte_Minuti > StartDate.Year && blockedDate.Year == monthMinute.Anno_Col_Monte_Minuti)
                                        {
                                            upperMonthBound = blockedDate.Month;
                                            lowerMonthBound = 1;

                                        }
                                        else if (monthMinute.Anno_Col_Monte_Minuti == StartDate.Year && blockedDate.Year == monthMinute.Anno_Col_Monte_Minuti)
                                        {
                                            upperMonthBound = blockedDate.Month;
                                            lowerMonthBound = StartDate.Month;

                                        }

                                        // ciclo su tutti i mesi da processare per il monte minuti in elaborazione e lo sommo al monte minuti da ritornare
                                        for (int monthIndex = upperMonthBound; monthIndex >= lowerMonthBound; monthIndex--)
                                        {
                                            int meseMonteMinuti = (int)CommonService.GetPropertyValue(monthMinute, String.Format("M{0}_Col_Monte_Minuti", monthIndex.ToString("00")));

                                            if (meseMonteMinuti == -999999999)
                                                meseMonteMinuti = 0;

                                            lastMonthMinutes -= meseMonteMinuti;
                                        }
                                    }
                                }

                                // imposto il valore calcolato del rimasto a fine mese sulla proprietà apposita
                                _lastMonthlyHours = lastMonthMinutes;
                            }
                            #endregion

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Metodo che calcola e imposta sul cartellino corrente il monte minuti residuo all'ultimo mese,
        /// recuperando il dato salvato nel database.
        /// </summary>
        private void setLastMonthMonteMinuti()
        {
            _lastMonthlyHours = 0;

            if (ColId != 0)
            {
                if (RepoManager.ParamRepo.ParametersRow.Abilita_Monte_Minuti && RepoManager.ParamRepo.ParametersRow.Flag_Monte_Ore != (int)MothlyHoursEnum.None)
                {
                    // Recupera i monteminuti dell'anno del mese precedente
                    DateTime lastMonth = StartDate.AddMonths(-1);
                    Col_Monte_Minuti mm_row = RepoManager.Col_Monte_MinutiRepo.FirstOrDefault(m => m.Col_Id == ColId && m.Anno_Col_Monte_Minuti == lastMonth.Year);
                    int mm = 0;
                    if (mm_row != default(Col_Monte_Minuti))
                    {
                        //Se il mese scorso ha un monteminuti impostato, lo recupero, altrimenti il monteminuti precedente resta 0
                        int retrievedMm = (int)CommonService.GetPropertyValue(mm_row, string.Format("M{0}_Col_Monte_Minuti", lastMonth.Month.ToString("00")));
                        mm = retrievedMm == DEFAULT_MONTEMINUTI ? 0 : retrievedMm;
                    }

                    _lastMonthlyHours = mm;
                }
            }
        }
        public static void updateMonteMinuti(int colId, DateTime month, int minutes)
        {
            int mm_delta = 0,
                currentMm = 0;

            Col_Monte_Minuti current_mm_row = RepoManager.Col_Monte_MinutiRepo.FirstOrDefault(m => m.Col_Id == colId && m.Anno_Col_Monte_Minuti == month.Year);
            if (current_mm_row != default(Col_Monte_Minuti))
            {
                int retrievedMm = (int)CommonService.GetPropertyValue(current_mm_row, string.Format("M{0}_Col_Monte_Minuti", month.Month.ToString("00")));
                currentMm = retrievedMm == DEFAULT_MONTEMINUTI ? 0 : retrievedMm;
            }

            //Il monteminuti da salvare per il mese corrente
            int mm = minutes;
            //Calcola il delta tra il monteminuti appena calcolato (mm) e quello vecchio (currentMm)
            mm_delta = mm - currentMm;

            bool isNew = false;

            //Crea un anno nuovo se non esiste già
            if (current_mm_row == default(Col_Monte_Minuti))
            {
                current_mm_row = RepoManager.Col_Monte_MinutiRepo.Init();
                string prName = "";
                for (int i = 1; i <= 12; i++)
                {
                    prName = string.Format("M{0}_Col_Monte_Minuti", i.ToString("00"));
                    CommonService.SetPropertyValue(current_mm_row, prName, DEFAULT_MONTEMINUTI);
                }

                current_mm_row.Anno_Col_Monte_Minuti = month.Year;
                current_mm_row.Col_Id = colId;
                isNew = true;
            }
            //Imposta il monteminuti del mese corrente
            CommonService.SetPropertyValue(current_mm_row, string.Format("M{0}_Col_Monte_Minuti", month.Month.ToString("00")), mm);

            //Salva/aggiorna il repository
            if (isNew)
            {
                RepoManager.Col_Monte_MinutiRepo.Add(current_mm_row, true);
            }
            else
            {
                RepoManager.Col_Monte_MinutiRepo.Update(current_mm_row, true);
            }

            DateTime processingMonth = month.AddMonths(1);
            string propertyName = "";
            //TODO TOGLIERE STO TRUE, CAMBIARE CONDIZIONE DI USCITA
            while (true)
            {
                if (current_mm_row.Anno_Col_Monte_Minuti != processingMonth.Year)
                {
                    current_mm_row = RepoManager.Col_Monte_MinutiRepo.FirstOrDefault(m => m.Col_Id == colId && m.Anno_Col_Monte_Minuti == processingMonth.Year);
                }

                propertyName = string.Format("M{0}_Col_Monte_Minuti", processingMonth.Month.ToString("00"));

                //Se il prossimo mese non è ancora stato elaborato, mi fermo ed esco dal triciclo
                if (current_mm_row == default(Col_Monte_Minuti) || (int)CommonService.GetPropertyValue(current_mm_row, propertyName) == DEFAULT_MONTEMINUTI)
                {
                    break;
                }

                //Imposta il nuovo monteminuti aggiungendo il delta del monteminuti appena calcolto con quello vecchio
                CommonService.SetPropertyValue(current_mm_row, propertyName, (int)CommonService.GetPropertyValue(current_mm_row, propertyName) + mm_delta);

                //Se siamo a dicembre, salva l'anno
                if (processingMonth.Month == 12)
                {
                    RepoManager.Col_Monte_MinutiRepo.Update(current_mm_row, true);
                    RepoManager.Col_Monte_MinutiRepo.SaveChanges();
                }

                processingMonth = processingMonth.AddMonths(1);
            }
            if (current_mm_row != default(Col_Monte_Minuti))
            {
                RepoManager.Col_Monte_MinutiRepo.Update(current_mm_row, true);
                RepoManager.Col_Monte_MinutiRepo.SaveChanges();
            }
        }

        /// <summary>
        /// Metodo che calcola e imposta sul database il delta del mese corrente, modificando quello dei cartellini futuri se già elaborati.
        /// </summary>
        private void setCurrentMonthMonteMinuti()
        {
            if (ColId != 0)
            {
                if (RepoManager.ParamRepo.ParametersRow.Abilita_Monte_Minuti && RepoManager.ParamRepo.ParametersRow.Flag_Monte_Ore != (int)MothlyHoursEnum.None)
                {
                    //Imposta il monteminuti del mese precedente come riporto ore da aggiungere al mese corrente.
                    setLastMonthMonteMinuti();

                    updateMonteMinuti(ColId, StartDate, TotalMinutes);

                }
            }
        }

        /// <summary>
        /// Recupera il numero di minuti previsti per il corrente orario nel giorno del mese specificato.
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno nel mese di cui recuperare il dato.</param>
        /// <returns>
        /// Il numero di minuti previsti per il giorno del mese specifico nel corrente orario.
        /// </returns>
        public int GetDayMinutes(int dayNumber)
        {
            // ritorno del valore del metodo
            double minutesAmmount = DaysHours.ContainsKey(dayNumber) ? DaysHours[dayNumber].Item1 : 0f;
            return Convert.ToInt32(minutesAmmount);
        }

        /// <summary>
        /// Recupera il dettaglio della durata impostata dall'elenco di orari passati come parametro.
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno di cui inserire il dato.</param>
        /// <param name="tabOraris">L'elenco degli orari salvati a database da processare.</param>
        public void GetDetailFromTabOrari(int dayNumber, IEnumerable<Tab_Orari> tabOraris)
        {

        }

        #endregion

        #region Public & Private Static Methods

        /// <summary>
        /// Recupera il delta tra i mesi espressi dalle due date passate come parametri e per il collaboratore indicato.
        /// </summary>
        /// <param name="col">Il collaboratore per cui calcolare il monte minuti</param>
        /// <param name="from">La data che rappresenta il mese di partenza per il calcolo del monte minuti.</param>
        /// <param name="to">La data che rappresenta il mese di arrivo per il calcolo del monte minuti.</param>
        /// <param name="isDecimalHour"><c>true</c> se le ore vanno espresse in centesimi; <c>false</c> per sessantesimi</param>
        /// <returns>Il totale di minuti che formano il monte minuti per i dati passati come parametro</returns>
        public static int GetLastMonthMinutesAmmount(Col col, DateTime from, DateTime to, bool addBlockedDateMinutes)
        {
            // inizializzazione del valore di ritorno del metodo
            int returnValue = 0;

            // inizializzazione del valore del numero totale di minuti mensile
            int totalMinutes = 0;

            // recupero tutte le date di inizio mese tra la data di partenza e la data di arrivo
            var firstMonthDateList = CommonService.GetDatesFromPeriod(from, to).Where(dt => dt.Day == 1).ToList();

            // per ogni primo giorno del mese genero i relativi timesheet
            var timesheetList = new List<TimesheetModuleItem>();

            foreach (var firstMonthDate in firstMonthDateList)
            {
                timesheetList.AddRange(GenerateTimeSheet(firstMonthDate, false, false, "", false, null, col, addBlockedDateMinutes: addBlockedDateMinutes));

                TimesheetTotalController totalController = new TimesheetTotalController(timesheetList, false, "Col");
                var res = totalController.GetMonthDeltaHoursForMonthlyMinutesWithAutStr(col.Col_Id, from, to);

            }



            // si verifica se è attiva la gestione dell'autorizzazione straordinari
            bool isAutStrConfigured = IsAuthStrConfigured();

            // se non è attiva la gestione dell'approvazione straordinari allora si procede alla semplice differenza tra quanto fatto e previsto nel mese
            if (!isAutStrConfigured)
            {
                //se la lista dei cartellini non è vuota
                if (timesheetList.Count > 0)
                {
                    //viene estratto l'utlimo timesheet in questo modo viene processato solo il mese
                    var lastTimeSheet = timesheetList.Last();
                    DateTime lastDateTime = lastTimeSheet.StartDate;

                    timesheetList = timesheetList.Where(ts => ts.StartDate == lastDateTime).ToList();

                    // una volta ottenuta la lista di timehseet ed epurati da ore non lavorate e piani, effettuo il calcolo del totale dei minuti lavorati
                    totalMinutes = timesheetList.Where(ts => ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN) &&
                                                             ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ONL)).Select(ts => ts.TotalMinutesForMonthlyMinutes).Sum();

                    // ... e il totale dei minuti di piano
                    int totalPlanMinutes = timesheetList.Where(ts => ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN)).Select(ts => ts.TotalMinutesForMonthlyMinutes).Sum();

                    // // una volta ottenuta la lista di timehseet ed epurati da ore non lavorate e piani, effettuo il calcolo del totale dei minuti lavorati
                    //double  lastMonteMinuti = timesheetList.Where(ts => ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN) &&
                    //                                          ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ONL)).Select(ts => ts.LastMonthlyMinutes).Su;

                    // si sottrae da quanto fatto quanto previsto per ottenere il monte minuti. Delta del mese
                    returnValue = (totalMinutes - totalPlanMinutes);
                }
            }
            else
            {
                // altrimenti se è attiva la configurazione dell'autorizzazione straordinario si genera il total controller da cui è possibile controllare l'autorizzazione straordinario
                TimesheetTotalController totalController = new TimesheetTotalController(timesheetList, false, "Col");
                returnValue = totalController.GetMonthDeltaHoursForMonthlyMinutesWithAutStr(col.Col_Id, from, to);

            }

            // ritorno del valore del metodo (la differenza tra i minuti effettuati ed il piano) (delta)
            return returnValue;
        }

        /// <summary>
        /// Metodo che si occupa di generare la lista di oggetti timesheet con cui comporre il cartellino presenze.
        /// </summary>
        /// <param name="selectedDate">La data selezionata utilizzata per comporre il mese/anno del cartellino.</param>
        /// <param name="isDecimalHours"><c>true</c> se le ore che compongono il cartellino andranno espresse in centesimi; <c>false</c> se le ore che compongono il cartellino andranno
        /// espresse in sessantesimi.</param>
        /// <param name="isByOtherEntity">Indica se estrarre la lista di timesheet separati per cantiere o collaboratore (l'inverso dell'entità di selezione).</param>
        /// <param name="sessionMinDateKey">La chiave per riportare in sessione la data di inizio mese; riportata solo se divresa da stringa vuota.</param>
        /// <param name="showOnlyPlan"><c>true</c> se si desidera visualizzato solamente il piano; <c>false</c> in caso contrario</param>
        /// <param name="entityIdsToProcess">L'elenco degli id delle entità da processare</param>
        /// <param name="colToProcess">L'eventuale collaboratore da processare; la mancanza di questo parametro indica la volontà di processare i collaboratori nel parametro entityIdsToProcess</param>
        /// <param name="referenceEntity">L'entità di riferimento per la generazione del timesheet; se non specificata si tratta il cartellino per collaboratore.</param>
        /// <param name="timesheetOptions">L'elenco delle opzioni selezionate dall'utente per la costruzione del cartellino</param>
        /// <param name="hasWeeklyTotals">Indica se i timesheet resitutiti dovranno gestire il totale per settimana.</param>
        /// <returns>
        /// La lista di oggetti timesheet che comporranno il cartellino presenze
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Entity Col for Col To Process</exception>
        public static List<TimesheetModuleItem> GenerateTimeSheet(DateTime selectedDate, bool isDecimalHours, bool isByOtherEntity, string sessionMinDateKey, bool showOnlyPlan,
            List<int> entityIdsToProcess, Col colToProcess = null, string referenceEntity = "Col", List<string> timesheetOptions = null, bool hasWeeklyTotals = false, bool addBlockedDateMinutes = true, bool usaFisiche = false)
        {
            const string colEntityName = "Col";
            const string cantEntityName = "Can";

            // inserimento delle opzioni selezionate dall'utente
            TimesheetOptions = timesheetOptions;

            // se è richiesto di processare una singola entità ed è richiesto un collaboratore allora si genera un'eccezione
            if (colToProcess != null && referenceEntity != colEntityName)
                throw new InvalidOperationException("Entity Col for Col To Process");

            // calcolo, a partire dalla data passata come parametro, l'inzio e la fine del mese in elaborazione
            DateTime minDate = CommonService.GetFirstMonthDay(selectedDate);
            DateTime monthLastDate = CommonService.GetLastMonthDay(minDate);
            DateTime maxDate = new DateTime(monthLastDate.Year, monthLastDate.Month, monthLastDate.Day, 23, 59, 59);

            // salvataggio della della data di inizio mese in sessione (utilizzata nella colorazione del weekend)
            if (sessionMinDateKey != String.Empty)
                PowerWebContext.SetToSession<DateTime>(sessionMinDateKey, minDate);

            // inizializzazione della lista di oggetti timesheet che sarà il ritorno del metodo
            var tsmItems = new List<TimesheetModuleItem>();

            // se si è richiesto di processare dei collaboratori, si procede alla produzione di tale dato
            if (referenceEntity == colEntityName)
            {

                #region Gestione della produzione dei timesheet item per collaboratore

                // se si sta processando un cartellino per collaboratore ed è attiva l'autorizzazione degli straordinari
                // ed è impostato un recupero settimanale allora il cartellino si calcola comunque per settimana al fine di gestire il recupero
                if (!hasWeeklyTotals)
                    hasWeeklyTotals = IsAuthStrConfigured() && (TimesheetRecoveryHoursTypeEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheetRecoveryHoursTypeEnum) != TimesheetRecoveryHoursTypeEnum.NoHoursRecovery;

                // recupero della lista con tutti i collaboratori selezionati a video dall'utente
                var colsToProcessList = new List<Col>();
                if (colToProcess != null)
                    colsToProcessList.Add(colToProcess);
                List<Col> cols = null;
                cols = colToProcess != null ? colsToProcessList : RepoManager.ColRepo.Find(col => entityIdsToProcess.Contains(col.Col_Id), true).ToList();

                // se sono stati trovati dei collaboratori
                if (cols.Any())
                {
                    // per ogni collaboratore da processare
                    foreach (Col col in cols)
                    {
                        // si elabora il collaboratore solamente se le date di disponibilità dello stesso sono valide nel periodo richiesto
                        if (HasColValidDates(col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col, minDate, maxDate))
                        {
                            #region 1 .Calcolo di tutte le ore piano per ogni collaboratore

                            // 1. Calcolo di tutte le ore piano per ogni collaboratore
                            // recupero l'elenco delle ore previste per il collaboratore e aggiungo quanto creato all'interno della liste di oggetti timesheet da ritornare
                            bool isFromFreeTimesheet;
                            int freeTimesheetId;
                            Dictionary<int, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> planMinutes = RepoManager.Tab_OrariRepo.GetPlanMinutes(col.Col_Id,
                                                                                    minDate, maxDate, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col,
                                                                                    out isFromFreeTimesheet, out freeTimesheetId, isByOtherEntity, requestedForWeeklyTotals: hasWeeklyTotals);

                            // se è richiesta la divisione per cantiere allora procedo a un inserimento suddiviso
                            if (isByOtherEntity)
                            {
                                // per ogni cantiere inserito all'interno del dizionario di rientro
                                foreach (var cantMonthValues in planMinutes)
                                {
                                    var colPlan = GenerateNewPlanTimesheet(isDecimalHours, cantMonthValues.Value, isFromFreeTimesheet, freeTimesheetId, col.Col_Id, minDate, cantMonthValues.Key);
                                    tsmItems.Add(colPlan);
                                }
                            }
                            else // altrimenti si effettua un inserimento selezionando solo il primo valore
                            {
                                var colPlan = GenerateNewPlanTimesheet(isDecimalHours, planMinutes.First().Value, isFromFreeTimesheet, freeTimesheetId, col.Col_Id, minDate, planMinutes.First().Key);
                                tsmItems.Add(colPlan);
                            }

                            // una volta generati i piani recupero (per uso successivo) il primo piano inserito
                            TimesheetModuleItem firstPlan = tsmItems.FirstOrDefault();
                            TimeSpan? nocturnStartHour = null;
                            TimeSpan? nocturnStartHourModify = null;
                            TimeSpan? nocturnEndHour = null;

                            if (firstPlan != default(TimesheetModuleItem))
                            {
                                nocturnStartHour = firstPlan.NocturnsStartHour;
                                nocturnEndHour = firstPlan.NocturnEndHour;
                            }

                            #endregion

                            // si procede a elaborare le ore solamente se non è richiesto di visualizzare solamente le ore piano
                            if (!showOnlyPlan)
                            {

                                #region 2. Recupero tutte la lista di reg_v del periodo specificato per il collaboratore in elaborazione

                                IEnumerable<Reg_V> baseColRegVs = GetPeriodColRegVs(col, minDate, maxDate, hasWeeklyTotals).OrderBy(r => r.Data_Reg).ToList();


                                // se il collaboratore ha delle reg_v nel periodo specificato
                                if (baseColRegVs.Any())
                                {
                                    // inizializzazione dell'ordine di visualizzazione
                                    int tsOrder = 0;

                                    #region Calcolo di tutte le ore (diurne, notturne, non viaggi, senza motivazione)

                                    // 3. Calcolo di tutte le ore (ore diurne, ore notturne, non viaggi e senza motivazione)
                                    // recupero di tutte le ore lavorate, non viaggi, per il periodo e il collaboratore prescelto
                                    List<Reg_V> workedRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.WorkedRegs, baseColRegVs);

                                    // se sono state trovate delle ore lavorate allora genero il relativo oggetto di cartellino
                                    //if (workedRegVs.Any())
                                    {
                                        // si provvede all'eventuale separazione delle ore notturne dalle ore diurne solo in caso l'orario di riferimento preveda
                                        // la distinzione di tale dato (leggo il primo piano recuperato in quanto il dato di notturno è in testata ed è uguale per tuttli gli orari

                                        #region Calcolo ore senza inizio/fine notturno

                                        if (nocturnStartHour == null || nocturnEndHour == null)
                                        {
                                            // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                            if (!isByOtherEntity)
                                                tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, workedRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE), minDate, maxDate, ++tsOrder, 0, hasWeeklyTotals, usaFisiche: usaFisiche));
                                            else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(workedRegVs, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE), minDate, maxDate, ++tsOrder, hasWeeklyTotals));

                                            // alle ore figurative aggiungo il riporto delle ore precedenti
                                            // if (!addBlockedDateMinutes)
                                            tsmItems.Last().SetLastMonthlyMinutes(addBlockedDateMinutes);

                                        }
                                        #endregion

                                        #region Calcolo ore con inizio/fine notturno
                                        else
                                        {
                                            // verifico la presenza della customizzazione riguardante la visualizzazione delle ore separate notturne/diurne
                                            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowNocturnAndDayTimesheetEnum);

                                            // se è richiesta la visualizzazione separata di ore notturne e ore diurne allora procedo al calcolo, altrimenti effettuo il calcolo come
                                            // se tale distinzione, anche se presente nella tab_orari_tipo, non esistesse
                                            if (customizationVersion == (int)ShowNocturnAndDayTimesheetEnum.ShowIfPresent)
                                            {

                                                if (customizationVersion == (int)IncludeNocturnInDayHours.Include)
                                                {

                                                    // calcolo delle ore diurne tra le ore lavorate calcolate in precedenza
                                                    // le ore diurne sono quelle che cominciano o finiscono nel lasso di tempo non notturno
                                                    var dayRegVsAll = workedRegVs.Where(regv => regv.Data_Ora_Fig_U != null && !regv.IsOnlyDuration).ToList();

                                                    //vengono estratte le registrazioni di sola durata
                                                    var dayRegVsDuration = workedRegVs.Where(regv => regv.Data_Ora_Fig_U == null && regv.IsOnlyDuration).ToList();

                                                    //vengono estratte sono le registrazioni che non sono durata
                                                    var dayRegVs = dayRegVsAll.Where(regv => regv.Data_Ora_Fig_U != null && !regv.IsOnlyDuration).ToList();

                                                    //se si hanno nel giorno registrazioni di sola durata vengono aggiunte alla lista totale dell ore lavorate giornaliere
                                                    if (dayRegVsDuration.Any())
                                                        dayRegVs.AddRange(dayRegVsDuration);

                                                    // inserimento delle ore diurne calcolate, se trovate
                                                    if (dayRegVs.Any())
                                                    {
                                                        // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                                        if (!isByOtherEntity)
                                                            tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, dayRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE), minDate, maxDate, ++tsOrder, 0, hasWeeklyTotals, usaFisiche: usaFisiche));
                                                        else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                            tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(dayRegVs, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));

                                                    }

                                                }

                                                else
                                                {
                                                    // calcolo delle ore diurne tra le ore lavorate calcolate in precedenza
                                                    // le ore diurne sono quelle che cominciano o finiscono nel lasso di tempo non notturno
                                                    var dayRegVsAll = workedRegVs.Where(regv => regv.Data_Ora_Fig_E.Value.TimeOfDay < nocturnStartHour.Value || regv.Data_Ora_Fig_E.Value.TimeOfDay > nocturnEndHour.Value ||
                                                        (regv.Data_Ora_Fig_U != null && regv.Data_Ora_Fig_U.Value.TimeOfDay < nocturnStartHour.Value) || (regv.Data_Ora_Fig_U != null && regv.Data_Ora_Fig_U.Value.TimeOfDay > nocturnEndHour.Value) && !regv.IsOnlyDuration).ToList();

                                                    //vengono estratte le registrazioni di sola durata
                                                    var dayRegVsDuration = workedRegVs.Where(regv => regv.Data_Ora_Fig_U == null && regv.IsOnlyDuration).ToList();

                                                    //vengono estratte sono le registrazioni che non sono durata
                                                    var dayRegVs = dayRegVsAll.Where(regv => regv.Data_Ora_Fig_U != null && !regv.IsOnlyDuration).ToList();


                                                    #region Aggiustamento delle reg a cavallo dell'orario notturno

                                                    // inizializzazione della lista di reg_v aggiuntive da aggiungere al giorno
                                                    var dayRegVsToAdd = new List<Reg_V>();

                                                    //viene controllato se l'ora di inizio notturno è uguale alla mezzanotte
                                                    if (nocturnStartHour == TimeSpan.Zero)
                                                        nocturnStartHourModify = new TimeSpan(23, 59, 59);
                                                    else
                                                        nocturnStartHourModify = nocturnStartHour;

                                                    // per calcolare correttamente il numero di ore diurne devo aggiustare l'entrata o l'uscita di quanto selezionato ai parametri di inzio/fine notturno
                                                    dayRegVs.ForEach(regv =>
                                                    {
                                                        // si trattano solamente le registrazioni che hanno un'uscita (potrebbero essere presenti anche registrazioni solo durata)
                                                        if (regv.Data_Ora_Fis_U.HasValue)
                                                        {

                                                            // inizializzazione del valore che indica se la reg_v è stata modificata o meno
                                                            bool regVEdited = false;

                                                            // 1.se la reg_v comincia nella fascia di notturno, per il calcolo delle ore del cartellino la si fa cominciare al termine del notturno
                                                            if (regv.Data_Ora_Fig_E.Value.TimeOfDay >= nocturnStartHourModify || regv.Data_Ora_Fig_E.Value.TimeOfDay <= nocturnEndHour)
                                                            {
                                                                // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                                                                // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                                                regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                                                                regv.TmpDurataFigU = regv.Durata_Fig;

                                                                regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                                                    regv.Data_Ora_Fig_E.Value.Month, regv.Data_Ora_Fig_E.Value.Day,
                                                                    nocturnEndHour.Value.Hours,
                                                                    nocturnEndHour.Value.Minutes,
                                                                    nocturnEndHour.Value.Seconds);

                                                                if (regv.Data_Ora_Fig_U.Value.Date != null)
                                                                {
                                                                    if (regv.Data_Ora_Fig_U.Value.Date > regv.TmpDataOraFigE.Value.Date)
                                                                        regv.Data_Ora_Fig_E = regv.Data_Ora_Fig_E.Value.AddDays(1);
                                                                }

                                                                regVEdited = true;
                                                            }

                                                            // 2.se la reg_v finisce nella fascia di notturno, per il calcolo delle ore del cartellino la si fa finire all'inizio del notturno
                                                            if ((regv.Data_Ora_Fig_U.Value.TimeOfDay >= nocturnStartHourModify || regv.Data_Ora_Fig_U.Value.TimeOfDay <= nocturnEndHour) && !regVEdited)
                                                            {
                                                                // salvo l'attuale data figurativa in una extension, così da poterla poi recuperare
                                                                // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                                                regv.TmpDataOraFigU = regv.Data_Ora_Fig_U;
                                                                regv.TmpDurataFigU = regv.Durata_Fig;

                                                                regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                                                    regv.Data_Ora_Fig_U.Value.Month, regv.Data_Ora_Fig_U.Value.Day,
                                                                    nocturnStartHour.Value.Hours,
                                                                    nocturnStartHour.Value.Minutes,
                                                                    nocturnStartHour.Value.Seconds);

                                                                if ((regv.Data_Ora_Fig_E.Value.Date < regv.TmpDataOraFigU.Value.Date) && nocturnStartHour != TimeSpan.Zero)
                                                                    regv.Data_Ora_Fig_U = regv.Data_Ora_Fig_U.Value.AddDays(-1);

                                                                regVEdited = true;
                                                            }

                                                            // 3.se la reg_v non inizia ne finisce ne finisce nella fascia di notturno ma la attraversa (giorni differenti, entrata minore inizio notturno
                                                            // e fine maggiore di fine notturno) allora si procede alla generazione di due reg_v
                                                            Reg_V newRegV = default(Reg_V);
                                                            if (regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date && regv.Data_Ora_Fig_E.Value.TimeOfDay < nocturnStartHourModify && regv.Data_Ora_Fig_U.Value.TimeOfDay > nocturnEndHour)
                                                            {
                                                                newRegV = RepoManager.Reg_VRepo.Init();

                                                                // procedo alla modifica dell'entrata (salvando l'attuale data figurativa in una extension, così da poterla poi recuperare
                                                                // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali
                                                                regv.TmpDataOraFigU = regv.Data_Ora_Fig_U;
                                                                regv.TmpDurataFigU = regv.Durata_Fig;

                                                                regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                                                    regv.Data_Ora_Fig_E.Value.Month, regv.Data_Ora_Fig_E.Value.Day,
                                                                    nocturnStartHour.Value.Hours,
                                                                    nocturnStartHour.Value.Minutes,
                                                                    nocturnStartHour.Value.Seconds);

                                                                //se l'inizio del notturno è uguale alla mezzanotte(00:00) e la Reg di entrata avviene nel giorno prima di quella dell uscita
                                                                if ((regv.Data_Ora_Fig_E.Value.Date < regv.TmpDataOraFigU.Value.Date) && nocturnStartHour == TimeSpan.Zero)
                                                                    //l'ora di entrata è pari alla mezzanotte ma del giorno stesso della reg di uscita
                                                                    regv.Data_Ora_Fig_U = regv.Data_Ora_Fig_U.Value.AddDays(1);


                                                                // genero la nuova registrazione del giorno che parte dalla fine del notturno e arriva alla chiusura della stessa
                                                                CommonService.DuplicateEntity(regv, newRegV);

                                                                newRegV.RegE = 0;
                                                                newRegV.Data_Ora_Fig_U = newRegV.TmpDataOraFigU;
                                                                newRegV.Durata_Fig = newRegV.TmpDurataFigU;

                                                                newRegV.TmpDataOraFigE = newRegV.Data_Ora_Fig_E;
                                                                newRegV.TmpDurataFigU = newRegV.Durata_Fig;

                                                                newRegV.Data_Ora_Fig_E = new DateTime(newRegV.Data_Ora_Fig_U.Value.Year,
                                                                    newRegV.Data_Ora_Fig_U.Value.Month, newRegV.Data_Ora_Fig_U.Value.Day,
                                                                    nocturnEndHour.Value.Hours,
                                                                    nocturnEndHour.Value.Minutes,
                                                                    nocturnEndHour.Value.Seconds);

                                                                regVEdited = true;
                                                            }

                                                            regv.Durata_Fig = regVEdited ? Convert.ToInt32(regv.Data_Ora_Fig_U.Value.Subtract(regv.Data_Ora_Fig_E.Value).TotalMinutes) : regv.Durata_Fig;
                                                            if (newRegV != default(Reg_V))
                                                            {
                                                                newRegV.Durata_Fig = Convert.ToInt32(newRegV.Data_Ora_Fig_U.Value.Subtract(newRegV.Data_Ora_Fig_E.Value).TotalMinutes);
                                                                dayRegVsToAdd.Add(newRegV);
                                                            }
                                                        }
                                                    });

                                                    //se si hanno registrazioni da aggiungere vengono aggiunte al totale del giorno
                                                    if (dayRegVsToAdd.Any())
                                                        dayRegVs.AddRange(dayRegVsToAdd);


                                                    //se si hanno nel giorno registrazioni di sola durata vengono aggiunte alla lista totale dell ore lavorate giornaliere
                                                    if (dayRegVsDuration.Any())
                                                        dayRegVs.AddRange(dayRegVsDuration);

                                                    #endregion

                                                    // inserimento delle ore diurne calcolate, se trovate
                                                    if (dayRegVs.Any())
                                                    {
                                                        // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                                        if (!isByOtherEntity)
                                                            tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, dayRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE), minDate, maxDate, ++tsOrder, 0, hasWeeklyTotals, usaFisiche: usaFisiche));
                                                        else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                            tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(dayRegVs, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));

                                                    }


                                                    // alle ore figurative aggiungo il riporto delle ore precedenti
                                                    tsmItems.Last().SetLastMonthlyMinutes(addBlockedDateMinutes);

                                                    #region Eventuale ripristino delle date figurative delle reg modificate

                                                    // prima di procedere al calcolo delle reg_v notturne metto a posto la lista delle reg_v diurne eventualmente modificate sopra
                                                    // (controllo tutte le date temporanee diverse da null perché quando sono qua, ho solo reg abbinate, quindi con entrata e uscita);
                                                    // sono anche cancellate le registrazioni a che comprendono il notturno (id a 0 perché generate appositamente sopra)
                                                    dayRegVs = dayRegVs.Where(regv => regv.RegE != 0).ToList();
                                                    dayRegVs.Where(regv => regv.TmpDataOraFigE != null || regv.TmpDataOraFigU != null).ForEach(regv =>
                                                    {
                                                        regv.Data_Ora_Fig_E = regv.TmpDataOraFigE ?? regv.Data_Ora_Fig_E;
                                                        regv.Data_Ora_Fig_U = regv.TmpDataOraFigU ?? regv.Data_Ora_Fig_U;
                                                        regv.Durata_Fig = regv.Durata_Fig;
                                                    });
                                                }

                                                #endregion

                                                #region calcolo delle ore notturne  


                                                //se l'inizio del notturno è uguale alla mezzanotte (00:00)
                                                if (nocturnStartHour == TimeSpan.Zero)
                                                    //allora inserisco un valore inferiore alla mezzanotte che serve solo nei controlli
                                                    nocturnStartHourModify = new TimeSpan(23, 59, 59);
                                                else
                                                    //altrimenti viene mantenuto il vlaore originale
                                                    nocturnStartHourModify = nocturnStartHour;

                                                // calcolo delle ore notturne tra le ore lavorate calcolate in precedenza
                                                // le ore notturne sono ore che cominciano o finiscono all'interno della fascia di notturno
                                                // oppure avendo date diverse lo attraversano
                                                var nocturnRegVs = workedRegVs.Where(regv => regv.Data_Ora_Fis_U.HasValue).
                                                    Where(regv => (regv.Data_Ora_Fig_E.Value.TimeOfDay >= nocturnStartHourModify.Value ||
                                                                     regv.Data_Ora_Fig_E.Value.TimeOfDay <= nocturnEndHour.Value)
                                                                     || (regv.Data_Ora_Fig_U.Value.TimeOfDay >= nocturnStartHourModify.Value
                                                                     || regv.Data_Ora_Fig_U.Value.TimeOfDay <= nocturnEndHour.Value) ||
                                                                    (regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date && regv.Data_Ora_Fig_E.Value.TimeOfDay <= nocturnStartHourModify.Value && regv.Data_Ora_Fig_U.Value.TimeOfDay >= nocturnEndHour.Value)).ToList();


                                                #region Aggiustamento delle reg a cavallo dell'orario notturno

                                                // per calcolare correttamente il numero di ore notturne devo aggiustare l'entrata o l'uscita di quanto selezionato ai parametri di inzio/fine notturno
                                                nocturnRegVs.ForEach(regv =>
                                                {
                                                    // inizializzazione del valore che indica se la reg_v è stata modificata o meno
                                                    bool regVEdited = false;

                                                    #region CASO 1
                                                    // se le date sono diverse e l'entrata è prima dell'inzio del notturno e l'uscita dopo la fine significa che sto comprendendo il notturno e quindi
                                                    // la registrazione di riferimento è il notturno
                                                    if (regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date && regv.Data_Ora_Fig_E.Value.TimeOfDay <= nocturnStartHourModify.Value && regv.Data_Ora_Fig_U.Value.TimeOfDay >= nocturnEndHour.Value)
                                                    {
                                                        // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                                                        // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                                        regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                                                        regv.TmpDataOraFigU = regv.Data_Ora_Fig_U;
                                                        regv.TmpDurataFigU = regv.Durata_Fig;

                                                        // l'entrata e l'uscita sono l'inizio e la fine del notturno
                                                        regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                                                    regv.Data_Ora_Fig_E.Value.Month,
                                                                    regv.Data_Ora_Fig_E.Value.Day,
                                                                    nocturnStartHour.Value.Hours,
                                                                    nocturnStartHour.Value.Minutes,
                                                                    nocturnStartHour.Value.Seconds);

                                                        //se l'inizio del notturno è uguale alla mezzanotte(00:00) e la Reg di entrata avviene nel giorno prima di quella dell uscita
                                                        if ((regv.Data_Ora_Fig_E.Value.Date < regv.TmpDataOraFigU.Value.Date) && nocturnStartHour == TimeSpan.Zero)
                                                            //l'ora di entrata è pari alla mezzanotte ma del giorno stesso della reg di uscita
                                                            regv.Data_Ora_Fig_E = regv.Data_Ora_Fig_E.Value.AddDays(1);

                                                        regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                                                    regv.Data_Ora_Fig_U.Value.Month,
                                                                    regv.Data_Ora_Fig_U.Value.Day,
                                                                    nocturnEndHour.Value.Hours,
                                                                    nocturnEndHour.Value.Minutes,
                                                                    nocturnEndHour.Value.Seconds);

                                                        regVEdited = true;
                                                    }
                                                    #endregion

                                                    #region CASO 2
                                                    else
                                                    {

                                                        // altrimenti se la reg_v comincia prima della fascia di notturno la si fa cominciare all'inzio notturno (solo per il calcolo del cartellino)
                                                        // viene controllata l'ora nuda e cruda, ma se la data di inizio è inferiore alla data di fine allora si è sicuramente a cavallo della notte
                                                        if (regv.Data_Ora_Fig_E.Value.TimeOfDay < nocturnStartHourModify && regv.Data_Ora_Fig_E.Value.Date <= regv.Data_Ora_Fig_U.Value.Date)
                                                        {
                                                            // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                                                            // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                                            regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                                                            regv.TmpDataOraFigU = regv.Data_Ora_Fig_U;
                                                            regv.TmpDurataFigU = regv.Durata_Fig;

                                                            // se il notturno (valore assoluto ora) comincia prima dell'ora da modificare allora significa che sto cambiando giorno e quindi la data di entrata va normalizzata
                                                            if (regv.Data_Ora_Fig_E.Value.TimeOfDay < nocturnStartHourModify)

                                                                regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                                                    regv.Data_Ora_Fig_E.Value.Month,
                                                                    regv.Data_Ora_Fig_E.Value.Day,
                                                                    nocturnStartHour.Value.Hours,
                                                                    nocturnStartHour.Value.Minutes,
                                                                    nocturnStartHour.Value.Seconds);
                                                            else
                                                                regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                                                    regv.Data_Ora_Fig_U.Value.Month,
                                                                    regv.Data_Ora_Fig_U.Value.Day,
                                                                    nocturnStartHour.Value.Hours,
                                                                    nocturnStartHour.Value.Minutes,
                                                                    nocturnStartHour.Value.Seconds);


                                                            //se l'inizio del notturno è uguale alla mezzanotte(00:00) e la Reg di entrata avviene nel giorno prima di quella dell uscita
                                                            if ((regv.Data_Ora_Fig_E.Value.Date < regv.TmpDataOraFigU.Value.Date) && nocturnStartHour == TimeSpan.Zero)
                                                                //l'ora di entrata è pari alla mezzanotte ma del giorno stesso della reg di uscita
                                                                regv.Data_Ora_Fig_E = regv.Data_Ora_Fig_E.Value.AddDays(1);


                                                            regVEdited = true;
                                                        }
                                                        #endregion

                                                        // se la reg_v finisce dopo la fascia di notturno la si fa terminare alla fine del notturno (solo per il calcolo del cartellino)
                                                        if ((regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date) && (regv.Data_Ora_Fig_U.Value.TimeOfDay > nocturnEndHour))
                                                        {
                                                            // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                                                            // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                                            regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                                                            regv.TmpDurataFigU = regv.Durata_Fig;

                                                            regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                                                regv.Data_Ora_Fig_U.Value.Month, regv.Data_Ora_Fig_U.Value.Day,
                                                                nocturnEndHour.Value.Hours,
                                                                nocturnEndHour.Value.Minutes,
                                                                nocturnEndHour.Value.Seconds);
                                                            regVEdited = true;
                                                        }

                                                        //nel caso in cui le reg_v sia completamente nel range del notturno
                                                        if (regv.Data_Ora_Fig_U.Value.TimeOfDay <= firstPlan.NocturnEndHour && regv.Data_Ora_Fig_E.Value.TimeOfDay >= firstPlan.NocturnEndHour)
                                                        {
                                                            regVEdited = true;
                                                        }
                                                    }
                                                    //se non vi sono ore che cadono nell'intervallo noturno
                                                    if (!regVEdited)
                                                        regv.Durata_Fig = 0;

                                                    // se la reg_v è stata modificata allora si procede al ricalcolo della durata
                                                    regv.Durata_Fig = regVEdited ? Convert.ToInt32(regv.Data_Ora_Fig_U.Value.Subtract(regv.Data_Ora_Fig_E.Value).TotalMinutes) : regv.Durata_Fig;

                                                });
                                                #endregion

                                                #endregion

                                                // inserimento delle ore notturne calcolate, se trovate
                                                if (nocturnRegVs.Any())
                                                {
                                                    // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                                    if (!isByOtherEntity)
                                                        tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, nocturnRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE), minDate, maxDate, ++tsOrder, 0, hasWeeklyTotals, usaFisiche: usaFisiche));
                                                    else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                        tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(nocturnRegVs, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));
                                                }

                                                #region Eventuale ripristino delle date figurative delle reg modificate

                                                // prima di procedere al calcolo delle reg_v notturne metto a posto la lista delle reg_v diurne eventualmente modificate sopra
                                                // (controllo tutte le date temporanee diverse da null perché quando sono qua, ho solo reg abbinate, quindi con entrata e uscita)
                                                nocturnRegVs.Where(regv => regv.TmpDataOraFigE != null || regv.TmpDataOraFigU != null).ForEach(regv =>
                                                {
                                                    regv.Data_Ora_Fig_E = regv.TmpDataOraFigE ?? regv.Data_Ora_Fig_E;
                                                    regv.Data_Ora_Fig_U = regv.TmpDataOraFigU ?? regv.Data_Ora_Fig_U;
                                                    regv.Durata_Fig = regv.Durata_Fig;
                                                });

                                                #endregion

                                            }
                                            else // se è richiesto di non separare ore notturne e ore diurne allora si procede all'inserimento delle sole ore figurative
                                            {
                                                // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                                if (!isByOtherEntity)
                                                    tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, workedRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE), minDate, maxDate, ++tsOrder, 0, hasWeeklyTotals, usaFisiche: usaFisiche));
                                                else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                    tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(workedRegVs, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));
                                            }
                                        }
                                        #endregion

                                    }

                                    #endregion

                                    #region Calcolo delle ore viaggi

                                    // si procede al calcolo delle ore viaggio solamente se tra le opzioni non è esplicitamente richiesto di toglierlo
                                    if (TimesheetOptions.All(tsopt => tsopt != NoTripHoursOptions))
                                    {
                                        // 4. Calcolo di tutte le ore viaggi
                                        // recupero tutti i viaggi presenti nel periodo calcolato per il collaboratore in elaborazione
                                        var tripRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.TripRegs, baseColRegVs);

                                        // se sono stati trovati dei viaggi allora genero il relativo oggetto di cartellino
                                        if (tripRegVs.Any())
                                        {
                                            // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                            if (!isByOtherEntity)
                                                tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, tripRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI), minDate, maxDate, ++tsOrder, 0, hasWeeklyTotals, usaFisiche: usaFisiche));
                                            else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(tripRegVs, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));
                                        }
                                    }

                                    #endregion

                                    #region Calcolo delle ore per ogni motivazione

                                    // 5. Calcolo del numero di ore per ogni motivazione
                                    // recupero tutte le registrazioni di ore con motivazione presenti nel periodo calcolato per il collaboratore in elaborazione
                                    var justificationRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.JustificationRegs, baseColRegVs);

                                    // se sono stati trovati delle registrazioni ora con motivazione  allora genero i relativi oggetti di cartellino
                                    if (justificationRegVs.Any())
                                    {
                                        foreach (string justification in justificationRegVs.Select(regv => regv.Motivazione_Reg_Cod).Distinct())
                                        {
                                            // con il codice della motivazione si ricercano i dati nell'apposita tabella per il recupero della descrizione se richiesto da apposita personalizzazione
                                            string displayJustfification = justification;
                                            ShowDescriptionJustTimesheet showDescriptionJust = (ShowDescriptionJustTimesheet)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowDescriptionJustTimesheet);
                                            if (showDescriptionJust == ShowDescriptionJustTimesheet.Show) // se la personalizzazione risulta attiva
                                            {
                                                Tab_Decod justConf = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "MOTIVAZIONI" && td.Chiave_Tab == justification);
                                                if (justConf != default(Tab_Decod))
                                                    displayJustfification = justConf.Decodifica_Tab;
                                            }


                                            // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                            if (!isByOtherEntity)
                                            {
                                                tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id,
                                                    isDecimalHours,
                                                    justificationRegVs.Where(regv => regv.Motivazione_Reg_Cod == justification).ToList(),
                                                    displayJustfification,
                                                    minDate,
                                                    maxDate,
                                                    ++tsOrder,
                                                    0,
                                                    hasWeeklyTotals,
                                                    usaFisiche: usaFisiche));
                                            }
                                            else
                                            {
                                                // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(justificationRegVs.Where(regv => regv.Motivazione_Reg_Cod == justification).ToList(),
                                                    col,
                                                    isDecimalHours,
                                                    justification,
                                                    minDate,
                                                    maxDate,
                                                    ++tsOrder,
                                                    hasWeeklyTotals,
                                                    usaFisiche: usaFisiche));
                                            }
                                        }
                                    }

                                    #endregion

                                    #region Calcolo di tutte le ore rettifica

                                    // 6. Calcolo di tutte le ore rettifica
                                    // si procede al calcolo e alla visualizzazione delle rettifiche solamente se è richiesto dalla relativa customizzazione
                                    int correctionCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowCorrectionOnTimesheetEnum);

                                    if (correctionCustomization == (int)ShowCorrectionOnTimesheetEnum.Show)
                                    {
                                        // recupero tutte le rettifiche presenti nel periodo calcolato per il collaboratore in elaborazione
                                        var correctionRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.CorrectionRegs, baseColRegVs);

                                        // se sono stati trovati delle rettifiche allora genero il relativo oggetto di cartellino
                                        if (correctionRegVs.Any())
                                        {
                                            // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                            if (!isByOtherEntity)
                                                tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, correctionRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE), minDate, maxDate, ++tsOrder, 0, hasWeeklyTotals, usaFisiche: usaFisiche));
                                            else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(correctionRegVs, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));
                                        }
                                    }

                                    #endregion

                                    #region Calcolo di tutte le ore arrotondamento per durata

                                    // 7. Calcolo di tutte gli arrotondamenti per durata
                                    // si procede alla visualizzazione degli arrotondamenti per durata solamente se è richiesto dalla relativa customizzazione
                                    ShowDurationRoundingsheetEnum roundingCustomization = (ShowDurationRoundingsheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowDurationRoundingsheetEnum);

                                    if (roundingCustomization == ShowDurationRoundingsheetEnum.Show)
                                    {
                                        // recupero tutte le rettifiche presenti nel periodo calcolato per il collaboratore in elaborazione
                                        var roundingRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.DurationRoundingRegs, baseColRegVs);

                                        // se sono stati trovati delle rettifiche allora genero il relativo oggetto di cartellino
                                        if (roundingRegVs.Any())
                                        {
                                            // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                            if (!isByOtherEntity)
                                                tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, roundingRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT), minDate, maxDate, ++tsOrder, 0, hasWeeklyTotals, usaFisiche: usaFisiche));
                                            else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(roundingRegVs, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));
                                        }
                                    }
                                    #endregion

                                    #region Calcolo di tutte le ore ONL

                                    // 8. Calcolo di tutte le ore ONL (ore non lavorate)
                                    // si procede al calcolo e alla visualizzazione delle rettifiche solamente se è richiesto dalla relativa customizzazione
                                    int onlCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowONLOnTimesheetEnum);

                                    if (onlCustomization == (int)ShowONLOnTimesheetEnum.Show)
                                    {
                                        // recupero tutte le ore non lavorate presenti nel periodo calcolato per il collaboratore in elaborazione
                                        var onlRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.OnlRegs, baseColRegVs);

                                        // se sono stati trovate delle ore non lavorate allora genero il relativo oggetto di cartellino
                                        if (onlRegVs.Any())
                                        {
                                            // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                            if (!isByOtherEntity)
                                                tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, onlRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ONL), minDate, maxDate, ++tsOrder, 0, hasWeeklyTotals, usaFisiche: usaFisiche));
                                            else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(onlRegVs, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ONL), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));
                                        }
                                    }

                                    #endregion
                                }
                                #endregion

                            }
                        }
                    }
                }

                #endregion

            }
            else if (referenceEntity == cantEntityName)// se è invece richiesto di processare i dati dei cantieri, si procede al recupero e al processo di tale dato
            {

                #region Gestione della produzione dei timehseet item per cantiere

                // recupero della lista con tutti i cantieri selezionati a video dall'utente
                var cants = RepoManager.CantRepo.Find(cant => entityIdsToProcess.Contains(cant.Cant_Id), true).ToList();

                // se sono stati trovati dei cantieri
                if (cants.Any())
                {
                    // per ogni cantiere da processare
                    foreach (var cant in cants)
                    {
                        #region Calcolo di tutte le ore piano per ogni cantiere

                        // 1. Calcolo di tutte le ore piano per ogni cantiere
                        // recupero l'elenco delle ore previste per il collaboratore e aggiungo quanto creato all'interno della liste di oggetti timesheet da ritornare
                        bool isFromFreeTimesheet;
                        int freeTimesheetId;
                        Dictionary<int, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> planMinutes = RepoManager.Tab_OrariRepo.GetPlanMinutes(cant.Cant_Id,
                                                                                        minDate, maxDate, null, null, out isFromFreeTimesheet, out freeTimesheetId, isByOtherEntity,
                                                                                        cantEntityName, requestedForWeeklyTotals: hasWeeklyTotals);

                        // se è richiesta la divisione per collaboratore allora procedo a un inserimento suddiviso
                        if (isByOtherEntity)
                        {
                            // per ogni collaboratore inserito all'interno del dizionario di rientro
                            foreach (var colMonthValues in planMinutes)
                            {
                                var cantPlan = GenerateNewPlanTimesheet(isDecimalHours, colMonthValues.Value, isFromFreeTimesheet, freeTimesheetId, colMonthValues.Key, minDate, cant.Cant_Id);
                                tsmItems.Add(cantPlan);
                            }
                        }
                        else // altrimenti si effettua un inserimento selezionando solo il primo valore
                        {
                            var colPlan = GenerateNewPlanTimesheet(isDecimalHours, planMinutes.First().Value, isFromFreeTimesheet, freeTimesheetId, 0, minDate, cant.Cant_Id);
                            tsmItems.Add(colPlan);
                        }



                        // una volta generati i piani recupero (per uso successivo) il primo piano inserito
                        var firstPlan = tsmItems.Any() ? tsmItems.First() : null;

                        #endregion

                        // si procede a elaborare le ore solamente se non è richiesto di visualizzare solamente le ore piano
                        if (!showOnlyPlan && firstPlan != null)
                        {
                            #region 2. Recupero tutte la lista di reg_v del periodo specificato per il cantiere in elaborazione
                            var baseColRegVs = GetPeriodCantRegVs(cant, minDate, maxDate, hasWeeklyTotals);
                            #endregion
                            // se il cantiere ha delle reg_v nel periodo specificato
                            if (baseColRegVs.Any())
                            {
                                // inizializzazione dell'ordine di visualizzazione
                                int tsOrder = 0;

                                #region 3. Calcolo di tutte le ore (ore diurne, ore notturne, non viaggi e senza motivazione)
                                // recupero di tutte le ore lavorate, non viaggi, per il periodo e il collaboratore prescelto
                                var workedRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.WorkedRegs, baseColRegVs);

                                // se sono state trovate delle ore lavorate allora genero il relativo oggetto di cartellino
                                if (workedRegVs.Any())
                                {
                                    // si provvede all'eventuale separazione delle ore notturne dalle ore diurne solo in caso l'orario di riferimento preveda
                                    // la distinzione di tale dato (leggo il primo piano recuperato in quanto il dato di notturno è in testata ed è uguale per tuttli gli orari

                                    if (firstPlan.NocturnsStartHour == null || firstPlan.NocturnEndHour == null)
                                    {
                                        // se è prevista la divisione per collaboratore, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                        if (!isByOtherEntity)
                                            tsmItems.Add(GenerateNewRegTimesheet(0, isDecimalHours, workedRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE), minDate, maxDate, ++tsOrder, cant.Cant_Id, hasWeeklyTotals, usaFisiche: usaFisiche));
                                        else // se è richiesta la divisione per collaboratore allora si provvede a creare un timesheet per ogni cantiere previsto
                                            tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(workedRegVs, cant, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));
                                    }
                                    else
                                    {
                                        // verifico la presenza della customizzazione riguardante la visualizzazione delle ore separate notturne/diurne
                                        int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowNocturnAndDayTimesheetEnum);

                                        // se è richiesta la visualizzazione separata di ore notturne e ore diurne allora procedo al calcolo, altrimenti effettuo il calcolo come
                                        // se tale distinzione, anche se presente nella tab_orari_tipo, non esistesse
                                        if (customizationVersion == (int)ShowNocturnAndDayTimesheetEnum.ShowIfPresent)
                                        {

                                            // calcolo delle ore diurne tra le ore lavorate calcolate in precedenza
                                            // le ore diurne sono quelle che cominciano o finiscono nel lasso di tempo non notturno


                                            var dayRegVs = workedRegVs.Where(regv => regv.Data_Ora_Fig_E.Value.TimeOfDay < firstPlan.NocturnsStartHour.Value || regv.Data_Ora_Fig_E.Value.TimeOfDay > firstPlan.NocturnEndHour.Value ||
                                                regv.Data_Ora_Fig_U.Value.TimeOfDay < firstPlan.NocturnsStartHour.Value || regv.Data_Ora_Fig_U.Value.TimeOfDay > firstPlan.NocturnEndHour.Value).ToList();

                                            #region Aggiustamento delle reg a cavallo dell'orario notturno

                                            // inizializzazione della lista di reg_v aggiuntive da aggiungere al giorno
                                            var dayRegVsToAdd = new List<Reg_V>();


                                            // per calcolare correttamente il numero di ore diurne devo aggiustare l'entrata o l'uscita di quanto selezionato ai parametri di inzio/fine notturno
                                            dayRegVs.ForEach(regv =>
                                            {
                                                // inizializzazione del valore che indica se la reg_v è stata modificata o meno
                                                bool regVEdited = false;

                                                // se la reg_v comincia nella fascia di notturno e dal recupero delle reg_v finisce al di fuori,
                                                // per il calcolo delle ore del cartellino la si fa cominciare al termine del notturno
                                                if (regv.Data_Ora_Fig_E.Value.TimeOfDay >= firstPlan.NocturnsStartHour || regv.Data_Ora_Fig_E.Value.TimeOfDay <= firstPlan.NocturnEndHour)
                                                {
                                                    // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                                                    // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                                    regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                                                    regv.TmpDurataFigU = regv.Durata_Fig;

                                                    regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                                        regv.Data_Ora_Fig_E.Value.Month, regv.Data_Ora_Fig_E.Value.Day,
                                                        firstPlan.NocturnEndHour.Value.Hours,
                                                        firstPlan.NocturnEndHour.Value.Minutes,
                                                        firstPlan.NocturnEndHour.Value.Seconds);

                                                    if (regv.Data_Ora_Fig_U.Value.Date != null)
                                                    {
                                                        if (regv.Data_Ora_Fig_U.Value.Date > regv.TmpDataOraFigE.Value.Date)
                                                            regv.Data_Ora_Fig_E = regv.Data_Ora_Fig_E.Value.AddDays(1);
                                                    }

                                                    regVEdited = true;
                                                }

                                                // se la reg_v finisce nella fascia di notturno, per il calcolo delle ore del cartellino la si fa finire all'inizio del notturno
                                                if ((regv.Data_Ora_Fig_U.Value.TimeOfDay >= firstPlan.NocturnsStartHour || regv.Data_Ora_Fig_U.Value.TimeOfDay <= firstPlan.NocturnEndHour) && !regVEdited)
                                                {
                                                    // salvo l'attuale data figurativa in una extension, così da poterla poi recuperare
                                                    // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                                    regv.TmpDataOraFigU = regv.Data_Ora_Fig_U;
                                                    regv.TmpDurataFigU = regv.Durata_Fig;

                                                    regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                                        regv.Data_Ora_Fig_U.Value.Month, regv.Data_Ora_Fig_U.Value.Day,
                                                        firstPlan.NocturnsStartHour.Value.Hours,
                                                        firstPlan.NocturnsStartHour.Value.Minutes,
                                                        firstPlan.NocturnsStartHour.Value.Seconds);

                                                    if (regv.Data_Ora_Fig_E.Value.Date < regv.TmpDataOraFigU.Value.Date)
                                                        regv.Data_Ora_Fig_U = regv.Data_Ora_Fig_U.Value.AddDays(-1);

                                                    regVEdited = true;
                                                }

                                                // se la reg_v non inizia ne finisce ne finisce nella fascia di notturno ma la attraversa (giorni differenti, entrata minore inizio notturno
                                                // e fine maggiore di fine notturno) allora si procede alla generazione di due reg_v
                                                Reg_V newRegV = default(Reg_V);
                                                if (regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date && regv.Data_Ora_Fig_E.Value.TimeOfDay < firstPlan.NocturnsStartHour && regv.Data_Ora_Fig_U.Value.TimeOfDay > firstPlan.NocturnEndHour)
                                                {
                                                    newRegV = RepoManager.Reg_VRepo.Init();

                                                    // procedo alla modifica dell'entrata (salvando l'attuale data figurativa in una extension, così da poterla poi recuperare
                                                    // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali
                                                    regv.TmpDataOraFigU = regv.Data_Ora_Fig_U;
                                                    regv.TmpDurataFigU = regv.Durata_Fig;

                                                    regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                                        regv.Data_Ora_Fig_E.Value.Month, regv.Data_Ora_Fig_E.Value.Day,
                                                        firstPlan.NocturnsStartHour.Value.Hours,
                                                        firstPlan.NocturnsStartHour.Value.Minutes,
                                                        firstPlan.NocturnsStartHour.Value.Seconds);

                                                    // genero la nuova registrazione del giorno che parte dalla fine del notturno e arriva alla chiusura della stessa
                                                    CommonService.DuplicateEntity(regv, newRegV);

                                                    newRegV.RegE = 0;
                                                    newRegV.Data_Ora_Fig_U = newRegV.TmpDataOraFigU;
                                                    newRegV.Durata_Fig = newRegV.TmpDurataFigU;

                                                    newRegV.TmpDataOraFigE = newRegV.Data_Ora_Fig_E;
                                                    newRegV.TmpDurataFigU = newRegV.Durata_Fig;

                                                    newRegV.Data_Ora_Fig_E = new DateTime(newRegV.Data_Ora_Fig_U.Value.Year,
                                                        newRegV.Data_Ora_Fig_U.Value.Month, newRegV.Data_Ora_Fig_U.Value.Day,
                                                        firstPlan.NocturnEndHour.Value.Hours,
                                                        firstPlan.NocturnEndHour.Value.Minutes,
                                                        firstPlan.NocturnEndHour.Value.Seconds);

                                                    regVEdited = true;
                                                }

                                                regv.Durata_Fig = regVEdited ? Convert.ToInt32(regv.Data_Ora_Fig_U.Value.Subtract(regv.Data_Ora_Fig_E.Value).TotalMinutes) : regv.Durata_Fig;
                                                if (newRegV != default(Reg_V))
                                                {
                                                    newRegV.Durata_Fig = Convert.ToInt32(newRegV.Data_Ora_Fig_U.Value.Subtract(newRegV.Data_Ora_Fig_E.Value).TotalMinutes);
                                                    dayRegVsToAdd.Add(newRegV);
                                                }
                                            });

                                            if (dayRegVsToAdd.Any())
                                                dayRegVs.AddRange(dayRegVsToAdd);

                                            #endregion

                                            // inserimento delle ore diurne calcolate, se trovate
                                            if (dayRegVs.Any())
                                            {
                                                // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                                if (!isByOtherEntity)
                                                    tsmItems.Add(GenerateNewRegTimesheet(0, isDecimalHours, dayRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE), minDate, maxDate, ++tsOrder, cant.Cant_Id, hasWeeklyTotals, usaFisiche: usaFisiche));
                                                else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                    tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(dayRegVs, cant, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));

                                            }

                                            #region Eventuale ripristino delle date figurative delle reg modificate

                                            // prima di procedere al calcolo delle reg_v notturne metto a posto la lista delle reg_v diurne eventualmente modificate sopra
                                            // (controllo tutte le date temporanee diverse da null perché quando sono qua, ho solo reg abbinate, quindi con entrata e uscita)
                                            // sono anche cancellate le registrazioni a che comprendono il notturno (id a 0 perché generate appositamente sopra)
                                            dayRegVs = dayRegVs.Where(regv => regv.RegE != 0).ToList();
                                            dayRegVs.Where(regv => regv.TmpDataOraFigE != null || regv.TmpDataOraFigU != null).ForEach(regv =>
                                            {
                                                regv.Data_Ora_Fig_E = regv.TmpDataOraFigE ?? regv.Data_Ora_Fig_E;
                                                regv.Data_Ora_Fig_U = regv.TmpDataOraFigU ?? regv.Data_Ora_Fig_U;
                                                regv.Durata_Fig = regv.Durata_Fig;
                                            });

                                            #endregion

                                            // calcolo delle ore notturne tra le ore lavorate calcolate in precedenza
                                            // le ore notturne sono ore che cominciano o finiscono all'interno della fascia di notturno
                                            var nocturnRegVs = workedRegVs.Where(regv => (regv.Data_Ora_Fig_E.Value.TimeOfDay >= firstPlan.NocturnsStartHour.Value || regv.Data_Ora_Fig_E.Value.TimeOfDay <= firstPlan.NocturnEndHour.Value)
                                                || (regv.Data_Ora_Fig_U.Value.TimeOfDay >= firstPlan.NocturnsStartHour.Value || regv.Data_Ora_Fig_U.Value.TimeOfDay <= firstPlan.NocturnEndHour.Value) ||
                                                (regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date && regv.Data_Ora_Fig_E.Value.TimeOfDay <= firstPlan.NocturnsStartHour.Value && regv.Data_Ora_Fig_U.Value.TimeOfDay >= firstPlan.NocturnEndHour.Value)).ToList();

                                            #region Aggiustamento delle reg a cavallo dell'orario notturno

                                            // per calcolare correttamente il numero di ore notturne devo aggiustare l'entrata o l'uscita di quanto selezionato ai parametri di inzio/fine notturno
                                            nocturnRegVs.ForEach(regv =>
                                            {
                                                // inizializzazione del valore che indica se la reg_v è stata modificata o meno
                                                bool regVEdited = false;

                                                // se le date sono diverse e l'entrata è prima dell'inzio del notturno e l'uscita dopo la fine significa che sto comprendendo il notturno e quindi
                                                // la registrazione di riferimento è il notturno
                                                if (regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date && regv.Data_Ora_Fig_E.Value.TimeOfDay <= firstPlan.NocturnsStartHour.Value && regv.Data_Ora_Fig_U.Value.TimeOfDay >= firstPlan.NocturnEndHour.Value)
                                                {
                                                    // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                                                    // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                                    regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                                                    regv.TmpDataOraFigU = regv.Data_Ora_Fig_U;
                                                    regv.TmpDurataFigU = regv.Durata_Fig;

                                                    // l'entrata e l'uscita sono l'inizio e la fine del notturno
                                                    regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                                                    regv.Data_Ora_Fig_E.Value.Month,
                                                                    regv.Data_Ora_Fig_E.Value.Day,
                                                                    firstPlan.NocturnsStartHour.Value.Hours,
                                                                    firstPlan.NocturnsStartHour.Value.Minutes,
                                                                    firstPlan.NocturnsStartHour.Value.Seconds);

                                                    regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                                                regv.Data_Ora_Fig_U.Value.Month,
                                                                regv.Data_Ora_Fig_U.Value.Day,
                                                                firstPlan.NocturnEndHour.Value.Hours,
                                                                firstPlan.NocturnEndHour.Value.Minutes,
                                                                firstPlan.NocturnEndHour.Value.Seconds);

                                                    regVEdited = true;
                                                }
                                                else
                                                {
                                                    // se la reg_v comincia prima della fascia di notturno la si fa cominciare all'inzio notturno (solo per il calcolo del cartellino)
                                                    // viene controllata l'ora nuda e cruda, ma se la data di inizio è inferiore alla data di fine allora si è sicuramente a cavallo della notte
                                                    if (regv.Data_Ora_Fig_E.Value.TimeOfDay < firstPlan.NocturnsStartHour && regv.Data_Ora_Fig_E.Value.Date < regv.Data_Ora_Fig_U.Value.Date)
                                                    {
                                                        // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                                                        // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                                        regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                                                        regv.TmpDurataFigU = regv.Durata_Fig;

                                                        // se il notturno (valore assoluto ora) comincia prima dell'ora da modificare allora significa che sto cambiando giorno e quindi la data di entrata va normalizzata
                                                        if (regv.Data_Ora_Fig_E.Value.TimeOfDay < firstPlan.NocturnsStartHour)
                                                            regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                                                                                regv.Data_Ora_Fig_E.Value.Month,
                                                                                                regv.Data_Ora_Fig_E.Value.Day,
                                                                                                firstPlan.NocturnsStartHour.Value.Hours,
                                                                                                firstPlan.NocturnsStartHour.Value.Minutes,
                                                                                                firstPlan.NocturnsStartHour.Value.Seconds);
                                                        else
                                                            regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                                                                                regv.Data_Ora_Fig_U.Value.Month,
                                                                                                regv.Data_Ora_Fig_U.Value.Day,
                                                                                                firstPlan.NocturnsStartHour.Value.Hours,
                                                                                                firstPlan.NocturnsStartHour.Value.Minutes,
                                                                                                firstPlan.NocturnsStartHour.Value.Seconds);

                                                        regVEdited = true;
                                                    }

                                                    // se la reg_v finisce dopo la fascia di notturno la si fa terminare alla fine del notturno (solo per il calcolo del cartellino)
                                                    if ((regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date) && (regv.Data_Ora_Fig_U.Value.TimeOfDay > firstPlan.NocturnEndHour))
                                                    {
                                                        // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                                                        // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                                        regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                                                        regv.TmpDurataFigU = regv.Durata_Fig;

                                                        regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                                            regv.Data_Ora_Fig_U.Value.Month, regv.Data_Ora_Fig_U.Value.Day,
                                                            firstPlan.NocturnEndHour.Value.Hours,
                                                            firstPlan.NocturnEndHour.Value.Minutes,
                                                            firstPlan.NocturnEndHour.Value.Seconds);
                                                        regVEdited = true;
                                                    }

                                                    //nel caso in cui le reg_v sia completamente nel range del notturno
                                                    if (regv.Data_Ora_Fig_U.Value.TimeOfDay <= firstPlan.NocturnEndHour && regv.Data_Ora_Fig_E.Value.TimeOfDay >= firstPlan.NocturnEndHour)
                                                    {
                                                        regVEdited = true;
                                                    }
                                                }
                                                //se non vi sono ore che cadono nell'intervallo noturno
                                                if (!regVEdited)
                                                    regv.Durata_Fig = 0;
                                                else
                                                    // se la reg_v è stata modificata allora si procede al ricalcolo della durata
                                                    regv.Durata_Fig = regVEdited ? Convert.ToInt32(regv.Data_Ora_Fig_U.Value.Subtract(regv.Data_Ora_Fig_E.Value).TotalMinutes) : regv.Durata_Fig;


                                            });

                                            #endregion

                                            // inserimento delle ore notturne calcolate, se trovate
                                            if (nocturnRegVs.Any())
                                            {
                                                // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                                if (!isByOtherEntity)
                                                    tsmItems.Add(GenerateNewRegTimesheet(0, isDecimalHours, nocturnRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE), minDate, maxDate, ++tsOrder, cant.Cant_Id, hasWeeklyTotals, usaFisiche: usaFisiche));
                                                else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                    tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(nocturnRegVs, cant, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));
                                            }

                                            #region Eventuale ripristino delle date figurative delle reg modificate

                                            // prima di procedere al calcolo delle reg_v notturne metto a posto la lista delle reg_v diurne eventualmente modificate sopra
                                            // (controllo tutte le date temporanee diverse da null perché quando sono qua, ho solo reg abbinate, quindi con entrata e uscita)
                                            nocturnRegVs.Where(regv => regv.TmpDataOraFigE != null || regv.TmpDataOraFigU != null).ForEach(regv =>
                                            {
                                                regv.Data_Ora_Fig_E = regv.TmpDataOraFigE ?? regv.Data_Ora_Fig_E;
                                                regv.Data_Ora_Fig_U = regv.TmpDataOraFigU ?? regv.Data_Ora_Fig_U;
                                                regv.Durata_Fig = regv.Durata_Fig;
                                            });

                                            #endregion

                                        }
                                        else // se è richiesto di non separare ore notturne e ore diurne allora si procede all'inserimento delle sole ore figurative
                                        {
                                            // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                            if (!isByOtherEntity)
                                                tsmItems.Add(GenerateNewRegTimesheet(0, isDecimalHours, workedRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE), minDate, maxDate, ++tsOrder, cant.Cant_Id, hasWeeklyTotals, usaFisiche: usaFisiche));
                                            else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                                tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(workedRegVs, cant, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));
                                        }
                                    }
                                }

                                #endregion

                                #region Calcolo delle ore viaggi

                                // si procede al calcolo delle ore viaggio solamente se tra le opzioni non è esplicitamente richiesto di toglierlo
                                if (TimesheetOptions.All(tsopt => tsopt != NoTripHoursOptions))
                                {
                                    // 4. Calcolo di tutte le ore viaggi
                                    // recupero tutti i viaggi presenti nel periodo calcolato per il collaboratore in elaborazione
                                    var tripRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.TripRegs, baseColRegVs);

                                    // se sono stati trovati dei viaggi allora genero il relativo oggetto di cartellino
                                    if (tripRegVs.Any())
                                    {
                                        // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                        if (!isByOtherEntity)
                                            tsmItems.Add(GenerateNewRegTimesheet(0, isDecimalHours, tripRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI), minDate, maxDate, ++tsOrder, cant.Cant_Id, hasWeeklyTotals, usaFisiche: usaFisiche));
                                        else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                            tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(tripRegVs, cant, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));

                                    }
                                }

                                #endregion

                                #region Calcolo delle ore per ogni motivazione

                                // 5. Calcolo del numero di ore per ogni motivazione
                                // recupero tutte le registrazioni di ore con motivazione presenti nel periodo calcolato per il collaboratore in elaborazione
                                var justificationRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.JustificationRegs, baseColRegVs);

                                // se sono stati trovati delle registrazioni ora con motivazione  allora genero i relativi oggetti di cartellino
                                if (justificationRegVs.Any())
                                {
                                    foreach (var justification in justificationRegVs.Select(regv => regv.Motivazione_Reg_Cod).Distinct())
                                    {
                                        // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                        if (!isByOtherEntity)
                                        {
                                            tsmItems.Add(GenerateNewRegTimesheet(0,
                                                isDecimalHours,
                                                justificationRegVs.Where(regv => regv.Motivazione_Reg_Cod == justification).ToList(),
                                                justification,
                                                minDate,
                                                maxDate,
                                                ++tsOrder,
                                                cant.Cant_Id,
                                                hasWeeklyTotals,
                                                usaFisiche: usaFisiche));
                                        }
                                        else
                                        {
                                            // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                            tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(justificationRegVs.Where(regv => regv.Motivazione_Reg_Cod == justification).ToList(),
                                                cant,
                                                isDecimalHours,
                                                justification,
                                                minDate,
                                                maxDate,
                                                ++tsOrder,
                                                hasWeeklyTotals,
                                                usaFisiche: usaFisiche));
                                        }
                                    }
                                }

                                #endregion

                                #region Calcolo di tutte le ore rettifica

                                // 6. Calcolo di tutte le ore rettifica
                                // si procede al calcolo e alla visualizzazione delle rettifiche solamente se è richiesto dalla relativa customizzazione
                                int correctionCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowCorrectionOnTimesheetEnum);

                                if (correctionCustomization == (int)ShowCorrectionOnTimesheetEnum.Show)
                                {
                                    // recupero tutte le rettifiche presenti nel periodo calcolato per il collaboratore in elaborazione
                                    var correctionRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.CorrectionRegs, baseColRegVs);

                                    // se sono stati trovati delle rettifiche allora genero il relativo oggetto di cartellino
                                    if (correctionRegVs.Any())
                                    {
                                        // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                        if (!isByOtherEntity)
                                            tsmItems.Add(GenerateNewRegTimesheet(0, isDecimalHours, correctionRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE), minDate, maxDate, ++tsOrder, cant.Cant_Id, hasWeeklyTotals, usaFisiche: usaFisiche));
                                        else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                            tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(correctionRegVs, cant, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));
                                    }
                                }

                                #endregion

                                #region Calcolo di tutte le ore ONL

                                // 7. Calcolo di tutte le ore ONL (ore non lavorate)
                                // si procede al calcolo e alla visualizzazione delle rettifiche solamente se è richiesto dalla relativa customizzazione
                                int onlCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowONLOnTimesheetEnum);

                                if (onlCustomization == (int)ShowONLOnTimesheetEnum.Show)
                                {
                                    // recupero tutte le ore non lavorate presenti nel periodo calcolato per il collaboratore in elaborazione
                                    var onlRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.OnlRegs, baseColRegVs);

                                    // se sono stati trovate delle ore non lavorate allora genero il relativo oggetto di cartellino
                                    if (onlRegVs.Any())
                                    {
                                        // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                                        if (!isByOtherEntity)
                                            tsmItems.Add(GenerateNewRegTimesheet(0, isDecimalHours, onlRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ONL), minDate, maxDate, ++tsOrder, cant.Cant_Id, hasWeeklyTotals, usaFisiche: usaFisiche));
                                        else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                                            tsmItems.AddRange(GenerateRegVTimesheetsByOtherEntity(onlRegVs, cant, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ONL), minDate, maxDate, ++tsOrder, hasWeeklyTotals, usaFisiche: usaFisiche));
                                    }
                                }

                                #endregion
                            }
                        }
                    }
                }

                #endregion

            }

            // ritnro del valore del metodo
            return tsmItems;

        }

        public static List<TimesheetModuleItem> GenerateDayNightTimesheets(DateTime selectedDate, bool isDecimalHours, int colId, TimesheetModuleItem colPlan)
        {
            // calcolo, a partire dalla data passata come parametro, l'inzio e la fine del mese in elaborazione
            DateTime minDate = CommonService.GetFirstMonthDay(selectedDate);
            DateTime monthLastDate = CommonService.GetLastMonthDay(minDate);
            DateTime maxDate = new DateTime(monthLastDate.Year, monthLastDate.Month, monthLastDate.Day, 23, 59, 59);

            // inizializzazione della lista di oggetti timesheet che sarà il ritorno del metodo
            var tsmItems = new List<TimesheetModuleItem>();

            Col col = RepoManager.ColRepo.FirstOrDefault(c => c.Col_Id == colId);

            // se è stato trovato il collaboratore e le date di disponibilità dello stesso sono valide nel periodo richiesto
            if (col != default(Col) && HasColValidDates(col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col, minDate, maxDate))
            {
                // una volta generati i piani recupero (per uso successivo) il primo piano inserito
                TimeSpan? nocturnStartHour = RepoManager.ParamRepo.ParametersRow.Cartellino_Inizio_Notturno ?? TimeSpan.FromHours(0);
                TimeSpan? nocturnStartHourModify = null;
                TimeSpan? nocturnEndHour = RepoManager.ParamRepo.ParametersRow.Cartellino_Fine_Notturno ?? TimeSpan.FromHours(6); ;
                if (colPlan != default(TimesheetModuleItem))
                {
                    nocturnStartHour = colPlan.NocturnsStartHour ?? nocturnStartHour;
                    nocturnEndHour = colPlan.NocturnEndHour ?? nocturnEndHour;
                }

                var baseColRegVs = GetPeriodColRegVs(col, minDate, maxDate, false).OrderBy(r => r.Data_Reg).ToList();
                List<Reg_V> workedRegVs = new List<Reg_V>();

                TimeSpan oraIn_Nott = nocturnStartHour.Value;
                TimeSpan oraOut_Nott = nocturnEndHour.Value > nocturnStartHour.Value ? nocturnEndHour.Value : nocturnEndHour.Value.Add(new TimeSpan(1, 0, 0, 0));

                // se il collaboratore ha delle reg_v nel periodo specificato
                if (baseColRegVs.Any())
                {
                    // 3. Calcolo di tutte le ore (ore diurne, ore notturne, non viaggi e senza motivazione)
                    // recupero di tutte le ore lavorate, non viaggi, per il periodo e il collaboratore prescelto
                    workedRegVs = baseColRegVs.ToList();
                    // calcolo delle ore diurne tra le ore lavorate calcolate in precedenza
                    // le ore diurne sono quelle che cominciano o finiscono nel lasso di tempo non notturno
                    var dayRegVsAll = workedRegVs.Where(regv =>
                    {
                        if (!regv.Data_Ora_Fig_U.HasValue || !regv.Data_Ora_Fig_E.HasValue)  //Se non abbiamo E o U
                        {
                            return true;
                        }

                        TimeSpan oraE = regv.Data_Ora_Fig_E.Value.TimeOfDay;
                        TimeSpan oraU = regv.Data_Ora_Fig_U.Value.TimeOfDay;

                        if (regv.Data_Ora_Fig_U.Value.Date > regv.Data_Ora_Fig_E.Value.Date) //Se l' U è maggiore dell'entrata
                        {
                            oraU = regv.Data_Ora_Fig_U.Value.TimeOfDay.Add(new TimeSpan(1, 0, 0, 0)); //Aggiungo un giorno 
                        }
                        else
                        {
                            if (oraE <= nocturnEndHour.Value && oraU <= nocturnEndHour.Value)
                            {
                                oraE = oraE.Add(new TimeSpan(1, 0, 0, 0));
                                oraU = oraU.Add(new TimeSpan(1, 0, 0, 0));
                            }
                        }


                        /*  if ((regv.Data_Ora_Fig_E != null && regv.Data_Ora_Fig_E.Value.TimeOfDay < nocturnStartHour.Value) ||
                              (regv.Data_Ora_Fig_U != null && regv.Data_Ora_Fig_U.Value.TimeOfDay > nocturnEndHour.Value && regv.Data_Ora_Fig_U.Value.DayOfYear != regv.Data_Ora_Fig_E.Value.DayOfYear) ||
                              (regv.Data_Ora_Fig_U != null && regv.Data_Ora_Fig_U.Value.TimeOfDay < nocturnStartHour.Value && regv.Data_Ora_Fig_U.Value.DayOfYear == regv.Data_Ora_Fig_E.Value.DayOfYear) && !regv.IsOnlyDuration)
                          {
                              return true;
                          }
                          */

                        if (oraE < oraIn_Nott || oraU > oraOut_Nott)
                        {
                            return true;
                        }
                        return false;

                    }).ToList();

                    //vengono estratte le registrazioni di sola durata
                    var dayRegVsDuration = workedRegVs.Where(regv => regv.Data_Ora_Fig_U == null && regv.IsOnlyDuration).ToList();

                    //vengono estratte sono le registrazioni che non sono durata
                    var dayRegVs = dayRegVsAll.Where(regv => regv.Data_Ora_Fig_U != null && !regv.IsOnlyDuration).ToList();

                    #region Aggiustamento delle reg a cavallo dell'orario notturno

                    // inizializzazione della lista di reg_v aggiuntive da aggiungere al giorno
                    var dayRegVsToAdd = new List<Reg_V>();

                    //viene controllato se l'ora di inizio notturno è uguale alla mezzanotte
                    if (nocturnStartHour == TimeSpan.Zero)
                        nocturnStartHourModify = new TimeSpan(23, 59, 59);
                    else
                        nocturnStartHourModify = nocturnStartHour;

                    // per calcolare correttamente il numero di ore diurne devo aggiustare l'entrata o l'uscita di quanto selezionato ai parametri di inzio/fine notturno
                    dayRegVs.ForEach(regv =>
                    {
                        // si trattano solamente le registrazioni che hanno un'uscita (potrebbero essere presenti anche registrazioni solo durata)
                        if (regv.Data_Ora_Fis_U.HasValue)
                        {
                            // inizializzazione del valore che indica se la reg_v è stata modificata o meno
                            bool regVEdited = false;

                            // se la reg_v comincia nella fascia di notturno, per il calcolo delle ore del cartellino la si fa cominciare al termine del notturno
                            if (regv.Data_Ora_Fig_E.Value.TimeOfDay >= nocturnStartHourModify || regv.Data_Ora_Fig_E.Value.TimeOfDay <= nocturnEndHour)
                            {
                                // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                                // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                                regv.TmpDurataFigU = regv.Durata_Fig;

                                regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                    regv.Data_Ora_Fig_E.Value.Month, regv.Data_Ora_Fig_E.Value.Day,
                                    nocturnEndHour.Value.Hours,
                                    nocturnEndHour.Value.Minutes,
                                    nocturnEndHour.Value.Seconds);

                                if (regv.Data_Ora_Fig_U.Value.Date != null)
                                {
                                    if (regv.Data_Ora_Fig_U.Value.Date > regv.TmpDataOraFigE.Value.Date)
                                        regv.Data_Ora_Fig_E = regv.Data_Ora_Fig_E.Value.AddDays(1);
                                }

                                regVEdited = true;
                            }

                            // se la reg_v finisce nella fascia di notturno, per il calcolo delle ore del cartellino la si fa finire all'inizio del notturno
                            if ((regv.Data_Ora_Fig_U.Value.TimeOfDay >= nocturnStartHourModify || regv.Data_Ora_Fig_U.Value.TimeOfDay <= nocturnEndHour) && !regVEdited)
                            {
                                // salvo l'attuale data figurativa in una extension, così da poterla poi recuperare
                                // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                                regv.TmpDataOraFigU = regv.Data_Ora_Fig_U;
                                regv.TmpDurataFigU = regv.Durata_Fig;

                                regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                    regv.Data_Ora_Fig_U.Value.Month, regv.Data_Ora_Fig_U.Value.Day,
                                    nocturnStartHour.Value.Hours,
                                    nocturnStartHour.Value.Minutes,
                                    nocturnStartHour.Value.Seconds);

                                if ((regv.Data_Ora_Fig_E.Value.Date < regv.TmpDataOraFigU.Value.Date) && nocturnStartHour != TimeSpan.Zero)
                                    regv.Data_Ora_Fig_U = regv.Data_Ora_Fig_U.Value.AddDays(-1);

                                regVEdited = true;
                            }

                            // se la reg_v non inizia ne finisce ne finisce nella fascia di notturno ma la attraversa (giorni differenti, entrata minore inizio notturno
                            // e fine maggiore di fine notturno) allora si procede alla generazione di due reg_v
                            Reg_V newRegV = default(Reg_V);
                            if (regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date && regv.Data_Ora_Fig_E.Value.TimeOfDay < nocturnStartHourModify && regv.Data_Ora_Fig_U.Value.TimeOfDay > nocturnEndHour)
                            {
                                newRegV = RepoManager.Reg_VRepo.Init();

                                // procedo alla modifica dell'entrata (salvando l'attuale data figurativa in una extension, così da poterla poi recuperare
                                // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali
                                regv.TmpDataOraFigU = regv.Data_Ora_Fig_U;
                                regv.TmpDurataFigU = regv.Durata_Fig;

                                regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                    regv.Data_Ora_Fig_E.Value.Month, regv.Data_Ora_Fig_E.Value.Day,
                                    nocturnStartHour.Value.Hours,
                                    nocturnStartHour.Value.Minutes,
                                    nocturnStartHour.Value.Seconds);

                                //se l'inizio del notturno è uguale alla mezzanotte(00:00) e la Reg di entrata avviene nel giorno prima di quella dell uscita
                                if ((regv.Data_Ora_Fig_E.Value.Date < regv.TmpDataOraFigU.Value.Date) && nocturnStartHour == TimeSpan.Zero)
                                    //l'ora di entrata è pari alla mezzanotte ma del giorno stesso della reg di uscita
                                    regv.Data_Ora_Fig_U = regv.Data_Ora_Fig_U.Value.AddDays(1);


                                // genero la nuova registrazione del giorno che parte dalla fine del notturno e arriva alla chiusura della stessa
                                CommonService.DuplicateEntity(regv, newRegV);

                                newRegV.RegE = 0;
                                newRegV.Data_Ora_Fig_U = newRegV.TmpDataOraFigU;
                                newRegV.Durata_Fig = newRegV.TmpDurataFigU;

                                newRegV.TmpDataOraFigE = newRegV.Data_Ora_Fig_E;
                                newRegV.TmpDurataFigU = newRegV.Durata_Fig;

                                newRegV.Data_Ora_Fig_E = new DateTime(newRegV.Data_Ora_Fig_U.Value.Year,
                                    newRegV.Data_Ora_Fig_U.Value.Month, newRegV.Data_Ora_Fig_U.Value.Day,
                                    nocturnEndHour.Value.Hours,
                                    nocturnEndHour.Value.Minutes,
                                    nocturnEndHour.Value.Seconds);

                                regVEdited = true;
                            }

                            regv.Durata_Fig = regVEdited ? Convert.ToInt32(regv.Data_Ora_Fig_U.Value.Subtract(regv.Data_Ora_Fig_E.Value).TotalMinutes) : regv.Durata_Fig;
                            if (newRegV != default(Reg_V))
                            {
                                newRegV.Durata_Fig = Convert.ToInt32(newRegV.Data_Ora_Fig_U.Value.Subtract(newRegV.Data_Ora_Fig_E.Value).TotalMinutes);
                                dayRegVsToAdd.Add(newRegV);
                            }
                        }
                    });

                    //se si hanno registrazioni da aggiungere vengono aggiunte al totale del giorno
                    if (dayRegVsToAdd.Any())
                    {
                        dayRegVs.AddRange(dayRegVsToAdd);
                    }

                    //se si hanno nel giorno registrazioni di sola durata vengono aggiunte alla lista totale dell ore lavorate giornaliere
                    if (dayRegVsDuration.Any())
                    {
                        dayRegVs.AddRange(dayRegVsDuration);
                    }

                    #endregion

                    int customizationVersionTrip = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheeetTotalWithoutMonthlyMinutes);
                    if (customizationVersionTrip == (int)TripHourIsWorkedHoursEnum.DoNotUse)
                        dayRegVs = dayRegVs.Where(d => d.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip).ToList();

                    int customizationVersionJustification = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.JustificationHourIsWorkedHoursEnum);
                    if (customizationVersionJustification == (int)JustificationHourIsWorkedHoursEnum.DoNotUse)
                        dayRegVs = dayRegVs.Where(d => d.Motivazione_Reg_Id == null).ToList();
                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PausaPranzoIsWorkedHoursEnum) == 0)
                        dayRegVs = dayRegVs.Where(d => d.Motivazione_Reg_Cod != "Pausa Pranzo").ToList();

                    // inserimento delle ore diurne calcolate,
                    tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, dayRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE), minDate, maxDate, 0, 0, false));

                    #region Eventuale ripristino delle date figurative delle reg modificate

                    // prima di procedere al calcolo delle reg_v notturne metto a posto la lista delle reg_v diurne eventualmente modificate sopra
                    // (controllo tutte le date temporanee diverse da null perché quando sono qua, ho solo reg abbinate, quindi con entrata e uscita);
                    // sono anche cancellate le registrazioni a che comprendono il notturno (id a 0 perché generate appositamente sopra)
                    dayRegVs = dayRegVs.Where(regv => regv.RegE != 0).ToList();
                    dayRegVs.Where(regv => regv.TmpDataOraFigE != null || regv.TmpDataOraFigU != null).ForEach(regv =>
                    {
                        regv.Data_Ora_Fig_E = regv.TmpDataOraFigE ?? regv.Data_Ora_Fig_E;
                        regv.Data_Ora_Fig_U = regv.TmpDataOraFigU ?? regv.Data_Ora_Fig_U;
                        regv.Durata_Fig = regv.Durata_Fig;
                    });
                    #endregion
                }


                #region calcolo delle ore notturne  
                //se l'inizio del notturno è uguale alla mezzanotte (00:00)
                if (nocturnStartHour == TimeSpan.Zero)
                    //allora inserisco un valore inferiore alla mezzanotte che serve solo nei controlli
                    nocturnStartHourModify = new TimeSpan(23, 59, 59);
                else
                    //altrimenti viene mantenuto il vlaore originale
                    nocturnStartHourModify = nocturnStartHour;

                // calcolo delle ore notturne tra le ore lavorate calcolate in precedenza
                // le ore notturne sono ore che cominciano o finiscono all'interno della fascia di notturno
                // oppure avendo date diverse lo attraversano
                var nocturnRegVs = workedRegVs.Where(regv => regv.Data_Ora_Fis_U.HasValue).
                    Where(regv =>
                    {
                        /*
                    if ((regv.Data_Ora_Fig_E.Value.TimeOfDay >= nocturnStartHourModify.Value ||
                                     regv.Data_Ora_Fig_E.Value.TimeOfDay <= nocturnEndHour.Value)
                                     || (regv.Data_Ora_Fig_U.Value.TimeOfDay >= nocturnStartHourModify.Value
                                     || regv.Data_Ora_Fig_U.Value.TimeOfDay <= nocturnEndHour.Value) ||
                                    (regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date && regv.Data_Ora_Fig_E.Value.TimeOfDay <= nocturnStartHourModify.Value && regv.Data_Ora_Fig_U.Value.TimeOfDay >= nocturnEndHour.Value))
                    */

                        //Se per qualche motivo la registrazione non ha entrata, non è notturna
                        if (!regv.Data_Ora_Fig_E.HasValue)
                        {
                            return false;
                        }

                        /*Aggiunge 1 giorno a entrata e uscita se richiesto in modo da poterle confrontare agevolmente con inizio/fine notturno*/

                        TimeSpan oraE = regv.Data_Ora_Fig_E.Value.TimeOfDay;
                        TimeSpan oraU = regv.Data_Ora_Fig_U.Value.TimeOfDay;

                        //Se entrata e uscita sono su due giorni diversi, l'uscita avrà un giorno in più
                        if (regv.Data_Ora_Fig_U.Value.Date > regv.Data_Ora_Fig_E.Value.Date)
                        {
                            oraU = regv.Data_Ora_Fig_U.Value.TimeOfDay.Add(new TimeSpan(1, 0, 0, 0));
                        }

                        else
                        {
                            //Se E e U sono sullo stesso giorno e vengono entrambe prima della fine del notturno, sono entrambe nel secondo giorno
                            if (oraE <= nocturnEndHour.Value && oraU <= nocturnEndHour.Value)
                            {
                                oraE = oraE.Add(new TimeSpan(1, 0, 0, 0));
                                oraU = oraU.Add(new TimeSpan(1, 0, 0, 0));
                            }
                        }

                        //C'è del notturno nei casi in cui non c'è diurno (se la registrazione è completamente a 'sinistra' o a 'destra' del notturno
                        if ((!((oraE < oraIn_Nott && oraU <= oraIn_Nott) || (oraE >= oraOut_Nott && oraU > oraOut_Nott))) ||
                                oraE < nocturnEndHour)
                        {
                            return true;
                        }

                        return false;


                    }).ToList();


                #region Aggiustamento delle reg a cavallo dell'orario notturno

                // per calcolare correttamente il numero di ore notturne devo aggiustare l'entrata o l'uscita di quanto selezionato ai parametri di inzio/fine notturno
                nocturnRegVs.ForEach(regv =>
                {
                    // inizializzazione del valore che indica se la reg_v è stata modificata o meno
                    bool regVEdited = false;

                    // se le date sono diverse e l'entrata è prima dell'inzio del notturno e l'uscita dopo la fine significa che sto comprendendo il notturno e quindi
                    // la registrazione di riferimento è il notturno
                    if (regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date && regv.Data_Ora_Fig_E.Value.TimeOfDay <= nocturnStartHourModify.Value && regv.Data_Ora_Fig_U.Value.TimeOfDay >= nocturnEndHour.Value)
                    {
                        // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                        // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                        regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                        regv.TmpDataOraFigU = regv.Data_Ora_Fig_U;
                        regv.TmpDurataFigU = regv.Durata_Fig;

                        // l'entrata e l'uscita sono l'inizio e la fine del notturno
                        regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                    regv.Data_Ora_Fig_E.Value.Month,
                                    regv.Data_Ora_Fig_E.Value.Day,
                                    nocturnStartHour.Value.Hours,
                                    nocturnStartHour.Value.Minutes,
                                    nocturnStartHour.Value.Seconds);

                        //se l'inizio del notturno è uguale alla mezzanotte(00:00) e la Reg di entrata avviene nel giorno prima di quella dell uscita
                        if ((regv.Data_Ora_Fig_E.Value.Date < regv.TmpDataOraFigU.Value.Date) && nocturnStartHour == TimeSpan.Zero)
                            //l'ora di entrata è pari alla mezzanotte ma del giorno stesso della reg di uscita
                            regv.Data_Ora_Fig_E = regv.Data_Ora_Fig_E.Value.AddDays(1);

                        regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                    regv.Data_Ora_Fig_U.Value.Month,
                                    regv.Data_Ora_Fig_U.Value.Day,
                                    nocturnEndHour.Value.Hours,
                                    nocturnEndHour.Value.Minutes,
                                    nocturnEndHour.Value.Seconds);

                        regVEdited = true;
                    }
                    else
                    {
                        // altrimenti se la reg_v comincia prima della fascia di notturno la si fa cominciare all'inzio notturno (solo per il calcolo del cartellino)
                        // viene controllata l'ora nuda e cruda, ma se la data di inizio è inferiore alla data di fine allora si è sicuramente a cavallo della notte
                        if (regv.Data_Ora_Fig_E.Value.TimeOfDay <= nocturnStartHourModify && (regv.Data_Ora_Fig_E.Value.Date < regv.Data_Ora_Fig_U.Value.Date || regv.Data_Ora_Fig_U.Value.TimeOfDay > nocturnStartHourModify))
                        {
                            // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                            // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                            regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                            regv.TmpDurataFigU = regv.Durata_Fig;

                            // se il notturno (valore assoluto ora) comincia prima dell'ora da modificare allora significa che sto cambiando giorno e quindi la data di entrata va normalizzata
                            if (regv.Data_Ora_Fig_E.Value.TimeOfDay <= nocturnStartHourModify)

                                regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_E.Value.Year,
                                    regv.Data_Ora_Fig_E.Value.Month,
                                    regv.Data_Ora_Fig_E.Value.Day,
                                    nocturnStartHour.Value.Hours,
                                    nocturnStartHour.Value.Minutes,
                                    nocturnStartHour.Value.Seconds);
                            else
                                regv.Data_Ora_Fig_E = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                    regv.Data_Ora_Fig_U.Value.Month,
                                    regv.Data_Ora_Fig_U.Value.Day,
                                    nocturnStartHour.Value.Hours,
                                    nocturnStartHour.Value.Minutes,
                                    nocturnStartHour.Value.Seconds);


                            //se l'inizio del notturno è uguale alla mezzanotte(00:00) e la Reg di entrata avviene nel giorno prima di quella dell uscita
                            if (regv.TmpDataOraFigU.HasValue && (regv.Data_Ora_Fig_E.Value.Date < regv.TmpDataOraFigU.Value.Date) && nocturnStartHour == TimeSpan.Zero)
                                //l'ora di entrata è pari alla mezzanotte ma del giorno stesso della reg di uscita
                                regv.Data_Ora_Fig_E = regv.Data_Ora_Fig_E.Value.AddDays(1);


                            regVEdited = true;
                        }

                        // se la reg_v finisce dopo la fascia di notturno la si fa terminare alla fine del notturno (solo per il calcolo del cartellino)
                        if (regv.Data_Ora_Fig_U.Value.TimeOfDay > nocturnEndHour && regv.Data_Ora_Fig_U.Value.TimeOfDay < nocturnStartHourModify && (regv.Data_Ora_Fig_E.Value.TimeOfDay > nocturnStartHourModify || regv.Data_Ora_Fig_E.Value.TimeOfDay < nocturnEndHour))
                        {
                            // salvo l'attuale data figurativa e la durata in una extension, così da poterla poi recuperare
                            // (in quanto le liste hanno puntatori agli oggetti la lista successiva avrà i valori modificati e non originali)
                            regv.TmpDataOraFigE = regv.Data_Ora_Fig_E;
                            regv.TmpDurataFigU = regv.Durata_Fig;

                            regv.Data_Ora_Fig_U = new DateTime(regv.Data_Ora_Fig_U.Value.Year,
                                regv.Data_Ora_Fig_U.Value.Month, regv.Data_Ora_Fig_U.Value.Day,
                                nocturnEndHour.Value.Hours,
                                nocturnEndHour.Value.Minutes,
                                nocturnEndHour.Value.Seconds);
                            regVEdited = true;
                        }

                        //nel caso in cui le reg_v sia completamente nel range del notturno
                        if (
                            (
                                (regv.Data_Ora_Fig_E.Value.Date == regv.Data_Ora_Fig_U.Value.Date)
                                &&
                                ((regv.Data_Ora_Fig_E.Value.TimeOfDay > nocturnStartHourModify && regv.Data_Ora_Fig_U.Value.TimeOfDay > nocturnStartHourModify) || (regv.Data_Ora_Fig_E.Value.TimeOfDay < nocturnEndHour && regv.Data_Ora_Fig_U.Value.TimeOfDay <= nocturnEndHour))
                            )
                            ||
                            (
                                (regv.Data_Ora_Fig_E.Value.Date != regv.Data_Ora_Fig_U.Value.Date)
                                &&
                                (regv.Data_Ora_Fig_E.Value.TimeOfDay > nocturnStartHourModify && regv.Data_Ora_Fig_U.Value.TimeOfDay < nocturnStartHourModify)
                            )
                        )
                        {
                            regVEdited = true;
                        }
                    }
                    //se non vi sono ore che cadono nell'intervallo notturno
                    if (!regVEdited)
                        regv.Durata_Fig = 0;

                    // se la reg_v è stata modificata allora si procede al ricalcolo della durata
                    regv.Durata_Fig = regVEdited ? Convert.ToInt32(regv.Data_Ora_Fig_U.Value.Subtract(regv.Data_Ora_Fig_E.Value).TotalMinutes) : regv.Durata_Fig;

                });
                #endregion

                #endregion

                // inserimento delle ore notturne calcolate
                tsmItems.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, nocturnRegVs, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE), minDate, maxDate, 1, 0, false));

                #region Eventuale ripristino delle date figurative delle reg modificate

                // prima di procedere al calcolo delle reg_v notturne metto a posto la lista delle reg_v diurne eventualmente modificate sopra
                // (controllo tutte le date temporanee diverse da null perché quando sono qua, ho solo reg abbinate, quindi con entrata e uscita)
                nocturnRegVs.Where(regv => regv.TmpDataOraFigE != null || regv.TmpDataOraFigU != null).ForEach(regv =>
                {
                    regv.Data_Ora_Fig_E = regv.TmpDataOraFigE ?? regv.Data_Ora_Fig_E;
                    regv.Data_Ora_Fig_U = regv.TmpDataOraFigU ?? regv.Data_Ora_Fig_U;
                    regv.Durata_Fig = regv.Durata_Fig;
                });
                #endregion
            }
            else
            {
                _log.InfoFormat("Il collaboratore {0} ha date di assunzione/licenziamento non congruenti con il periodo richiesto", col.Codice_Collaboratore);
            }

            return tsmItems;
        }


        /// <summary>
        /// Genera un nuovo timesheet item di tipo piano a partire dai dati passati come parametro
        /// </summary>
        /// <param name="isDecimalHours">Indica se impostare la visualizzazione del timesheet item in decimali o sessantesimi</param>
        /// <param name="planMinutes">L'elenco dei minuti per mese da inserire in orario</param>
        /// <param name="isFromFreeTimesheet"><c>true</c> se il dato proviene dalla tabella Col_Orario; altrimenti <c>false</c></param>
        /// <param name="freeTimesheetId">L'id dell'eventuale record della tabella Col_Orario di provenienza.</param>
        /// <param name="colId">L'id del collaboratore a cui l'elemento timesheet in generazione fa riferimento</param>
        /// <param name="firstMonthDate">Il primo giorno del mese oggetto del timesheet item in generazione</param>
        /// <param name="cantId">L'id del cantiere da inserire nell'oggetto timesheet in generazione (se nessun cantiere l'id sarà 0)</param>
        /// <returns>L'oggetto timesheet item generato utilizzando i parametri generati</returns>
        public static TimesheetModuleItem GenerateNewPlanTimesheet(bool isDecimalHours, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>> planMinutes,
            bool isFromFreeTimesheet, int freeTimesheetId, int colId, DateTime firstMonthDate, int cantId, PlanTypeEnum planType = PlanTypeEnum.Normal)
        {
            var planTimesheetItem = new TimesheetModuleItem(isDecimalHours);
            planTimesheetItem.StartDate = firstMonthDate;
            planTimesheetItem.PopulateHoursWithDate(planMinutes);
            planTimesheetItem.IsFromFreeTimeSheet = isFromFreeTimesheet;
            planTimesheetItem.FreeTimeSheetId = freeTimesheetId;
            planTimesheetItem.InsertColValues(colId);
            planTimesheetItem.InsertCantValues(cantId);
            planTimesheetItem.Order = 0;

            switch (planType)
            {
                case PlanTypeEnum.Normal:
                    planTimesheetItem.Justification = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN);
                    break;
                case PlanTypeEnum.Day:
                    planTimesheetItem.Justification = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_DAY);
                    break;
                case PlanTypeEnum.Night:
                    planTimesheetItem.Justification = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_NIGHT);
                    break;
                default:
                    break;
            }
            return planTimesheetItem;
        }

            public static TimesheetModuleItem GenerateNewPlanTimesheetExport(bool isDecimalHours,
            bool isFromFreeTimesheet, int freeTimesheetId, int colId, DateTime firstMonthDate, int cantId, PlanTypeEnum planType = PlanTypeEnum.Normal)
            {
                var planTimesheetItem = new TimesheetModuleItem(isDecimalHours);
                planTimesheetItem.StartDate = firstMonthDate;
                //planTimesheetItem.PopulateHoursWithDate();
                planTimesheetItem.IsFromFreeTimeSheet = isFromFreeTimesheet;
                planTimesheetItem.FreeTimeSheetId = freeTimesheetId;
                planTimesheetItem.InsertColValues(colId);
                planTimesheetItem.InsertCantValues(cantId);
                planTimesheetItem.Order = 0;

                switch (planType)
                {
                    case PlanTypeEnum.Normal:
                        planTimesheetItem.Justification = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN);
                        break;
                    case PlanTypeEnum.Day:
                        planTimesheetItem.Justification = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_DAY);
                        break;
                    case PlanTypeEnum.Night:
                        planTimesheetItem.Justification = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_NIGHT);
                        break;
                    default:
                        break;
                }
                return planTimesheetItem;
            }

            /// <summary>
            /// Genera un nuovo timesheet item di tipo ore corrette a partire dai dati passati come parametro
            /// </summary>
            /// <param name="isDecimalHours">Indica se impostare la visualizzazione del timesheet item in decimali o sessantesimi</param>
            /// <param name="planMinutes">L'elenco dei minuti per mese da inserire in orario</param>
            /// <param name="isFromFreeTimesheet"><c>true</c> se il dato proviene dalla tabella Col_Orario; altrimenti <c>false</c></param>
            /// <param name="freeTimesheetId">L'id dell'eventuale record della tabella Col_Orario di provenienza.</param>
            /// <param name="colId">L'id del collaboratore a cui l'elemento timesheet in generazione fa riferimento</param>
            /// <param name="firstMonthDate">Il primo giorno del mese oggetto del timesheet item in generazione</param>
            /// <param name="cantId">L'id del cantiere da inserire nell'oggetto timesheet in generazione (se nessun cantiere l'id sarà 0)</param>
            /// <returns>L'oggetto timesheet item generato utilizzando i parametri generati</returns>
            public static TimesheetModuleItem GenerateNewPartPermTimesheet(bool isDecimalHours, List<TimeSpan> ore, List<DateTime> giorni,
            bool isFromFreeTimesheet, int freeTimesheetId, int colId, DateTime firstMonthDate, int cantId, PlanTypeEnum planType = PlanTypeEnum.Normal)
        {
            var planTimesheetItem = new TimesheetModuleItem(isDecimalHours);
            planTimesheetItem.StartDate = firstMonthDate;
            planTimesheetItem.PopulateHoursWithDateNew(ore, giorni);
            planTimesheetItem.IsFromFreeTimeSheet = false;
            planTimesheetItem.FreeTimeSheetId = 0;
            planTimesheetItem.InsertColValues(colId);
            planTimesheetItem.InsertCantValues(cantId);
            planTimesheetItem.Order = 0;

            planTimesheetItem.Justification = BusinessService.GetLocalizedString(PowerWebResources.LBL_PARTPERM);
            return planTimesheetItem;
        }


        private static List<TimesheetModuleItem> GenerateRegVTimesheetsByOtherEntity(List<Reg_V> regVsToSplit, Col col, bool isDecimalHours, string timesheetJustification, DateTime firstMonthDate,
            DateTime lastMonthDate, int timesheetOrder, bool requestedForWeeklyTotals, bool usaFisiche = false, List<int> cantList = null)
        {
            // inizializzazione del valore di ritorno del metodo
            var returnList = new List<TimesheetModuleItem>();

            // recupero tutti gli id cantiere presenti all'interno della lista passata come parametro
            var cantIdList = regVsToSplit.Select(regv => regv.Cant_Id).Distinct().ToList();
            if (timesheetJustification == "Rettifiche Manu." || timesheetJustification == "Rettifiche Auto.")
            {
                cantIdList = cantList.Cast<int?>().ToList();
            }
            // per ogni id cantiere presente nella lista
            foreach (var listCantId in cantIdList)
            {
                // calcolo l'id cantiere facendo si di convertire in 0 i valori null
                var currentCantId = listCantId ?? 0;

                // aggiungo il timesheet specifico del cantiere alla list di ritorno
                returnList.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, regVsToSplit.Where(regv => regv.Cant_Id == listCantId).ToList(), timesheetJustification, firstMonthDate, lastMonthDate, timesheetOrder, currentCantId, requestedForWeeklyTotals, usaFisiche: usaFisiche));
            }

            // ritorno del valore calcolato dal metodo
            return returnList;
        }

        private static TimesheetModuleItem GenerateRegVTimesheetsByOtherEntityPausa(List<Reg_V> regVsToSplit, Col col, bool isDecimalHours, string timesheetJustification, DateTime firstMonthDate,
            DateTime lastMonthDate, int timesheetOrder, bool requestedForWeeklyTotals,int colId, int cantId, bool usaFisiche = false, List<int> cantList = null)
        {
            // inizializzazione del valore di ritorno del metodo
            var newTimesheet = new TimesheetModuleItem(isDecimalHours);

            // inserimento della data che indica il mese di elaborazione
            newTimesheet.StartDate = firstMonthDate;
            DateTime processingDate = firstMonthDate;

            // inizializzazione del piano vuoto in cui andare a compilare i totali per giornata
            var daysMinutes = RepoManager.Tab_OrariRepo.GetEmptyMinutesPlan(firstMonthDate, lastMonthDate, requestedForWeeklyTotals);

            // inizializzazione del valore di ritorno del metodo
            var returnList = new List<TimesheetModuleItem>();

            // recupero tutti gli id cantiere presenti all'interno della lista passata come parametro
            var cantIdList = regVsToSplit.Select(regv => regv.Cant_Id).Distinct().ToList();

            if (timesheetJustification == "Rettifiche Manu." || timesheetJustification == "Rettifiche Auto.")
            {
                cantIdList = cantList.Cast<int?>().ToList();
            }
            // per ogni id cantiere presente nella lista
            foreach (var listCantId in cantIdList)
            {
                // calcolo l'id cantiere facendo si di convertire in 0 i valori null
                var currentCantId = listCantId ?? 0;

                // aggiungo il timesheet specifico del cantiere alla list di ritorno
                returnList.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, regVsToSplit.Where(regv => regv.Cant_Id == listCantId).ToList(), timesheetJustification, firstMonthDate, lastMonthDate, timesheetOrder, currentCantId, requestedForWeeklyTotals, usaFisiche: usaFisiche));
            }
            for (int i = 1;i <= lastMonthDate.Day;i++) {
                double dayTotal = 0;
                string dayName = "Day" + i.ToString("00");
                double pausa = 0;
                foreach (var total in returnList) {
                    dayTotal += CommonService.FromHoursToMinutes((double)total[dayName], isDecimalHours);
                    List<Cant> cantiere = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == total.CantId).ToList();
                    pausa += cantiere.First().Importo1.Value;
                }
                if (daysMinutes.ContainsKey(processingDate))
                {
                    daysMinutes[processingDate] = new Tuple<double, TimeSpan?, TimeSpan?>(dayTotal, null, null);
                }
                processingDate = processingDate.AddDays(1);
            }

            newTimesheet.PopulateHoursWithDate(daysMinutes);
            newTimesheet.IsFromFreeTimeSheet = false;
            newTimesheet.FreeTimeSheetId = 0;
            newTimesheet.InsertColValues(colId);
            newTimesheet.InsertCantValues(cantId);
            newTimesheet.Justification = timesheetJustification;
            newTimesheet.Order = timesheetOrder;

            return newTimesheet;
        }

        private static List<TimesheetModuleItem> GenerateRegVTimesheetsByOtherEntityExportStr(List<Reg_V> regVsToSplit, Col col, bool isDecimalHours, string timesheetJustification, DateTime firstMonthDate,
           DateTime lastMonthDate, int timesheetOrder, bool requestedForWeeklyTotals, Dictionary<int, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> plan, bool usaFisiche = false, List<int> cantList = null)
        {
            // inizializzazione del valore di ritorno del metodo
            var returnList = new List<TimesheetModuleItem>();
            //
            // recupero tutti gli id cantiere presenti all'interno della lista passata come parametro
            var cantIdList = regVsToSplit.Select(regv => regv.Cant_Id).Distinct().ToList();
            //if (timesheetJustification == "Rettifiche Manu." || timesheetJustification == "Rettifiche Auto.")
            //{
            //    cantIdList = cantList.Cast<int?>().ToList();
            //}
            //List<int> cantIdList = new List<int>();
            var prova = plan.Select(regv => regv.Key).Distinct().ToList();
            
            if (timesheetJustification == "Rettifiche Manu." || timesheetJustification == "Rettifiche Auto.")
            {
                cantIdList = cantIdList.Cast<int?>().ToList();
            }
            foreach (var piano in plan)
            {
                if (!cantIdList.Contains(piano.Key))
                {
                    cantIdList.Add(piano.Key);
                }
            }
            // per ogni id cantiere presente nella lista
            foreach (var listCantId in cantIdList)
            {
                // calcolo l'id cantiere facendo si di convertire in 0 i valori null
                var currentCantId = listCantId  ?? 0;

                // aggiungo il timesheet specifico del cantiere alla list di ritorno
                returnList.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, regVsToSplit.Where(regv => regv.Cant_Id == listCantId).ToList(), timesheetJustification, firstMonthDate, lastMonthDate, timesheetOrder, currentCantId, requestedForWeeklyTotals, usaFisiche: usaFisiche));
            }

            // ritorno del valore calcolato dal metodo
            return returnList;
        }

        private static List<TimesheetModuleItem> GenerateDeltaRegVTimesheetsByOtherEntity(List<Reg_V> regVsToSplit, Col col, bool isDecimalHours, string timesheetJustification, DateTime firstMonthDate,
            DateTime lastMonthDate, int timesheetOrder, bool requestedForWeeklyTotals, Dictionary<int, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> minutes, TimesheetModuleItem colRigth, int freeTimesheetId, bool isFromFreeTimesheet, List<TimesheetModuleItem> cartellini, bool usaFisiche = false, List<int> cantList = null)
        {
            // inizializzazione del valore di ritorno del metodo
            var returnList = new List<TimesheetModuleItem>();

            // recupero tutti gli id cantiere presenti all'interno della lista passata come parametro
            //var cantIdList = regVsToSplit.Select(regv => regv.Cant_Id).Distinct().ToList();
            List<int> cantIdList = new List<int>();
            if (timesheetJustification == "Rettifiche Manu." || timesheetJustification == "Rettifiche Auto.")
            {
                cantIdList = cantList.Cast<int>().ToList();
            }
            foreach (var piano in minutes)
            {
                if (!cantIdList.Contains(piano.Key))
                {
                    cantIdList.Add(piano.Key);
                }
            }
            // per ogni id cantiere presente nella lista
            foreach (var listCantId in cantIdList)
            {
                // calcolo l'id cantiere facendo si di convertire in 0 i valori null
                var currentCantId = listCantId;
                bool controllo = false;
                TimesheetModuleItem colPlan = new TimesheetModuleItem(isDecimalHours);

                try {
                    var temp = minutes.First(m => m.Key == currentCantId);
                    colPlan = GenerateNewPlanTimesheet(isDecimalHours, temp.Value, isFromFreeTimesheet, freeTimesheetId, col.Col_Id, firstMonthDate, minutes.First().Key);

                    TimesheetModuleItem colTotal = GenerateNewTotalTimesheet(col.Col_Id, isDecimalHours, cartellini.Where(c => c.CantId == currentCantId).ToList(), BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE), firstMonthDate, lastMonthDate, ++timesheetOrder, 0, requestedForWeeklyTotals);
                    // aggiungo il timesheet specifico del cantiere alla list di ritorno
                    //returnList.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, regVsToSplit.Where(regv => regv.Cant_Id == listCantId).ToList(), timesheetJustification, firstMonthDate, lastMonthDate, timesheetOrder, currentCantId, requestedForWeeklyTotals, usaFisiche: usaFisiche));
                    returnList.Add(subtractTimesheetsExport(col.Col_Id, isDecimalHours, colTotal, colPlan, colRigth, BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA), firstMonthDate, lastMonthDate, ++timesheetOrder, currentCantId, requestedForWeeklyTotals));
                }
                catch (Exception e) { 
                }
            }
            //TimesheetModuleItem colDelta = subtractTimesheets(col.Col_Id, isDecimalHours, colTotal, colPlan, colRigth, BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA), firstMonthDate, lastMonthDate, ++timesheetOrder, 0, requestedForWeeklyTotals);

            // ritorno del valore calcolato dal metodo
            return returnList;
        }

        /// <summary>
        /// Genera e ritorna una lista di timesheets utilizzando le registrazioni passate come parametro, splittandole per collaboratore, per la motivazione passata come parametro.
        /// </summary>
        /// <param name="regVsToSplit">L'elenco delle Reg_V da splittare per cantiere e con cui generare, per ogni cantiere, un oggetto timesheet</param>
        /// <param name="cant">Il cantiere per cui è generato il nuovo timesheet</param>
        /// <param name="isDecimalHours">Indica se impostare la visualizzazione del timesheet in decimali o sessantesimi.</param>
        /// <param name="timesheetJustification">La motivazione da inserire all'interno dell'oggetto timesheet da generare.</param>
        /// <param name="firstMonthDate">La 1° data del mese di riferimento del timesheet.</param>
        /// <param name="lastMonthDate">L'ultima data del mese di rifermneto del timesheet.</param>
        /// <param name="timesheetOrder">L'ordine di visualizzazione da inserire nel timehseet.</param>
        /// <param name="requestedForWeeklyTotals">Indica che il piano è richiesto per un calcolo che prevede i totali settimanali.</param>
        /// <returns>L'oggetto timesheet rapporesentante i parametri passati al metodo.</returns>
        private static List<TimesheetModuleItem> GenerateRegVTimesheetsByOtherEntity(List<Reg_V> regVsToSplit, Cant cant, bool isDecimalHours,
            string timesheetJustification, DateTime firstMonthDate, DateTime lastMonthDate, int timesheetOrder, bool requestedForWeeklyTotals, bool usaFisiche = false)
        {
            // inizializzazione del valore di ritorno del metodo
            var returnList = new List<TimesheetModuleItem>();

            // recupero tutti gli di collaboratore presenti all'interno della lista passata come parametro
            var colIdsList = regVsToSplit.Select(regv => regv.Col_Id).Distinct().ToList();

            // per ogni id collaboratore presente nella lista
            foreach (var listColId in colIdsList)
            {
                // calcolo l'id collaboratore facendo si di convertire in 0 i valori null
                var currentColId = listColId ?? 0;

                // aggiungo il timesheet specifico del collaboratore alla list di ritorno
                returnList.Add(GenerateNewRegTimesheet(currentColId, isDecimalHours, regVsToSplit.Where(regv => regv.Col_Id == listColId).ToList(), timesheetJustification, firstMonthDate, lastMonthDate, timesheetOrder, cant.Cant_Id, requestedForWeeklyTotals, usaFisiche: usaFisiche));
            }

            // ritorno del valore calcolato dal metodo
            return returnList;
        }

        /// <summary>
        /// Genera un nuovo timesheet di utilizzando le registrazioni passate come parametro per la motivazione passata come parametro.
        /// </summary>
        /// <param name="colId">L'id del collaboratore per cui è generato il nuovo timesheet</param>
        /// <param name="isDecimalHours">Indica se impostare la visualizzazione del timesheet in decimali o sessantesimi.</param>
        /// <param name="timesheetRegVs">La lista di registrazioni con cui compilare il timesheet.</param>
        /// <param name="timesheetJustification">La motivazione da inserire all'interno dell'oggetto timesheet da generare.</param>
        /// <param name="firstMonthDate">La 1° data del mese di riferimento del timesheet.</param>
        /// <param name="lastMonthDate">L'ultima data del mese di rifermneto del timesheet.</param>
        /// <param name="timesheetOrder">L'ordine di visualizzazione da inserire nel timehseet.</param>
        /// <param name="cantId">L'id del cantiere a cui fanno riferimento le registrazioni da inserire nel timesheet; se 0 sarà trattato come non specificato.</param>
        /// <param name="requestedForWeeklyTotals">Indica che il piano è richiesto per un calcolo che prevede i totali settimanali.</param>
        /// <returns>L'oggetto timesheet rapporesentante i parametri passati al metodo.</returns>
        private static TimesheetModuleItem GenerateNewRegTimesheet(int colId, bool isDecimalHours, List<Reg_V> timesheetRegVs, string timesheetJustification, DateTime firstMonthDate,
            DateTime lastMonthDate, int timesheetOrder, int cantId, bool requestedForWeeklyTotals, bool usaFisiche = false)
        {
            // inizializzazione del valore di ritorno del metodo
            var newTimesheet = new TimesheetModuleItem(isDecimalHours);

            // inserimento della data che indica il mese di elaborazione
            newTimesheet.StartDate = firstMonthDate;

            // inizializzazione del piano vuoto in cui andare a compilare i dati calcolati dalle reg_v
            var daysMinutes = RepoManager.Tab_OrariRepo.GetEmptyMinutesPlan(firstMonthDate, lastMonthDate, requestedForWeeklyTotals);

            // per ogni giorno delle reg_v passate da processare si compila il piano inizializzato
            foreach (var dataReg in timesheetRegVs.Select(regv => regv.Data_Reg).Distinct().ToList())
            {
                int totalDayMinutes;
                int totalPausaPranzo;
                // Se viene richiesto di calcolare il cartellino con le ore fisiche, si calcola la somma totale delle durate fisiche
                if (usaFisiche)
                {
                    totalDayMinutes = timesheetRegVs.Where(regv => regv.Data_Reg == dataReg.Value).Select(regv => regv.Durata_Fis ?? 0).Sum();
                }
                //Altrimenti si calcola la somma totale delle durate figurative
                else
                {
                    totalDayMinutes = timesheetRegVs.Where(regv => regv.Data_Reg == dataReg.Value).Select(regv => regv.Durata_Fig ?? 0).Sum();
                }

                // inserisco la somma nella posizione corretta del dizionario con le durate
                if (daysMinutes.ContainsKey(dataReg.Value))
                    daysMinutes[dataReg.Value] = new Tuple<double, TimeSpan?, TimeSpan?>(Convert.ToDouble(totalDayMinutes), null, null);
            }

            // inserimento del calcolo dei totali all'interno dell'oggetto timesheet
            newTimesheet.PopulateHoursWithDate(daysMinutes);
            newTimesheet.IsFromFreeTimeSheet = false;
            newTimesheet.FreeTimeSheetId = 0;
            newTimesheet.InsertColValues(colId);
            newTimesheet.InsertCantValues(cantId);
            newTimesheet.Justification = timesheetJustification;
            newTimesheet.Order = timesheetOrder;
            // ritorno del valore del metodo
            return newTimesheet;
        }

        private static TimesheetModuleItem GenerateNewRegTimesheetTotal(int colId, bool isDecimalHours, List<Reg_V> timesheetRegVs, string timesheetJustification, DateTime firstMonthDate,
            DateTime lastMonthDate, int timesheetOrder, int cantId, bool requestedForWeeklyTotals, bool usaFisiche = false)
        {
            // inizializzazione del valore di ritorno del metodo
            var newTimesheet = new TimesheetModuleItem(isDecimalHours);

            // inserimento della data che indica il mese di elaborazione
            newTimesheet.StartDate = firstMonthDate;

            // inizializzazione del piano vuoto in cui andare a compilare i dati calcolati dalle reg_v
            var daysMinutes = RepoManager.Tab_OrariRepo.GetEmptyMinutesPlan(firstMonthDate, lastMonthDate, requestedForWeeklyTotals);

            // per ogni giorno delle reg_v passate da processare si compila il piano inizializzato
            foreach (var dataReg in timesheetRegVs.Select(regv => regv.Data_Reg).Distinct().ToList())
            {
                int totalDayMinutes;
                int totalPausaPranzo;
                // Se viene richiesto di calcolare il cartellino con le ore fisiche, si calcola la somma totale delle durate fisiche
                if (usaFisiche)
                {
                    totalDayMinutes = timesheetRegVs.Where(regv => regv.Data_Reg == dataReg.Value).Select(regv => regv.Durata_Fis ?? 0).Sum();
                }
                //Altrimenti si calcola la somma totale delle durate figurative
                else
                {
                    totalDayMinutes = timesheetRegVs.Where(regv => regv.Data_Reg == dataReg.Value).Select(regv => regv.Durata_Fig ?? 0).Sum();
                }

                // inserisco la somma nella posizione corretta del dizionario con le durate
                if (daysMinutes.ContainsKey(dataReg.Value))
                    daysMinutes[dataReg.Value] = new Tuple<double, TimeSpan?, TimeSpan?>(Convert.ToDouble(totalDayMinutes), null, null);
            }

            // inserimento del calcolo dei totali all'interno dell'oggetto timesheet
            newTimesheet.PopulateHoursWithDate(daysMinutes);
            newTimesheet.IsFromFreeTimeSheet = false;
            newTimesheet.FreeTimeSheetId = 0;
            newTimesheet.InsertColValues(colId);
            newTimesheet.InsertCantValues(cantId);
            newTimesheet.Justification = timesheetJustification;
            newTimesheet.Order = timesheetOrder;
            // ritorno del valore del metodo
            return newTimesheet;
        }

        /// <summary>
        /// Genera un nuovo timesheetcome somma dei timesheet passati come parametro.
        /// </summary>
        /// <param name="colId">L'id del collaboratore per cui è generato il nuovo timesheet</param>
        /// <param name="isDecimalHours">Indica se impostare la visualizzazione del timesheet in decimali o sessantesimi.</param>
        /// <param name="timesheetsToTotalize">La lista dei timesheet da sommare.</param>
        /// <param name="timesheetJustification">La motivazione da inserire all'interno dell'oggetto timesheet da generare.</param>
        /// <param name="firstMonthDate">La 1° data del mese di riferimento del timesheet.</param>
        /// <param name="lastMonthDate">L'ultima data del mese di rifermneto del timesheet.</param>
        /// <param name="timesheetOrder">L'ordine di visualizzazione da inserire nel timehseet.</param>
        /// <param name="cantId">L'id del cantiere a cui fanno riferimento le registrazioni da inserire nel timesheet; se 0 sarà trattato come non specificato.</param>
        /// <param name="requestedForWeeklyTotals">Indica che il piano è richiesto per un calcolo che prevede i totali settimanali.</param>
        /// <returns>L'oggetto timesheet rapporesentante i parametri passati al metodo.</returns>
        private static TimesheetModuleItem GenerateNewTotalTimesheet(int colId, bool isDecimalHours, List<TimesheetModuleItem> timesheetsToTotalize, string timesheetJustification, DateTime firstMonthDate,
            DateTime lastMonthDate, int timesheetOrder, int cantId, bool requestedForWeeklyTotals, bool usaFisiche = true)
        {
            // inizializzazione del valore di ritorno del metodo
            var newTimesheet = new TimesheetModuleItem(isDecimalHours);

            // inserimento della data che indica il mese di elaborazione
            newTimesheet.StartDate = firstMonthDate;
            DateTime processingDate = firstMonthDate;

            // inizializzazione del piano vuoto in cui andare a compilare i totali per giornata
            var daysMinutes = RepoManager.Tab_OrariRepo.GetEmptyMinutesPlan(firstMonthDate, lastMonthDate, requestedForWeeklyTotals);

            //Somma i giorni della settimana antecedente al mese in elaborazione

            if (requestedForWeeklyTotals)
            {
                for (int i = -7; i < 0; i++)
                {
                    double dayTotal = 0;
                    string dayName = "DayMinus" + Math.Abs(i).ToString();
                    DateTime currDate = firstMonthDate.AddDays(i);

                    foreach (var timesheet in timesheetsToTotalize)
                    {
                        dayTotal += CommonService.FromHoursToMinutes((double)timesheet[dayName], isDecimalHours);
                    }

                    if (daysMinutes.ContainsKey(currDate))
                    {
                        daysMinutes[currDate] = new Tuple<double, TimeSpan?, TimeSpan?>(dayTotal, null, null);
                    }
                }
            }


            // per ogni giorno del mese calcolo i totali e inserisco nel 'piano'
            for (int i = 1; i <= lastMonthDate.Day; i++)
            {
                double dayTotal = 0;
                string dayName = "Day" + i.ToString("00");

                foreach (var timesheet in timesheetsToTotalize)
                {
                    //dayTotal += (Math.Truncate((double)timesheet[dayName]) * 60) + Math.Round(((double)timesheet[dayName] - Math.Truncate((double)timesheet[dayName])) * 100);
                    //con questo if non sommo al totale il tempo corretto
                    if (timesheet.Justification == "PAU" && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.SubstractPausaPranzo) == 1)
                    {
                        dayTotal -= CommonService.FromHoursToMinutes((double)timesheet[dayName], isDecimalHours) * 2;
                    }
                    if (timesheet.Justification != "Tempo Corretto") 
                    { 
                        dayTotal += CommonService.FromHoursToMinutes((double)timesheet[dayName], isDecimalHours);
                    }
                }
                if (daysMinutes.ContainsKey(processingDate))
                {
                    daysMinutes[processingDate] = new Tuple<double, TimeSpan?, TimeSpan?>(dayTotal, null, null);
                }

                processingDate = processingDate.AddDays(1);
            }

            if (requestedForWeeklyTotals)
            {
                //Somma i giorni della settimana successiva al mese in elaborazione
                for (int i = 1; i <= 7; i++)
                {
                    double dayTotal = 0;
                    string dayName = "DayPlus" + Math.Abs(i).ToString();
                    DateTime currDate = lastMonthDate.Date.AddDays(i);

                    foreach (var timesheet in timesheetsToTotalize)
                    {
                        //dayTotal += (Math.Truncate((double)timesheet[dayName]) * 60) + Math.Round(((double)timesheet[dayName] - Math.Truncate((double)timesheet[dayName])) * 100);
                        if (timesheet.Justification == "PAU" && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.SubstractPausaPranzo) == 1)
                        {
                            dayTotal -= CommonService.FromHoursToMinutes((double)timesheet[dayName], isDecimalHours) * 2;
                        }
                        if (timesheet.Justification != "Tempo Corretto")
                        {
                            dayTotal += CommonService.FromHoursToMinutes((double)timesheet[dayName], isDecimalHours);
                        }
                    }
                    if (daysMinutes.ContainsKey(currDate))
                    {
                        daysMinutes[currDate] = new Tuple<double, TimeSpan?, TimeSpan?>(dayTotal, null, null);
                    }
                }
            }

            // inserimento del calcolo dei totali all'interno dell'oggetto timesheet
            newTimesheet.PopulateHoursWithDate(daysMinutes);
            newTimesheet.IsFromFreeTimeSheet = false;
            newTimesheet.FreeTimeSheetId = 0;
            newTimesheet.InsertColValues(colId);
            newTimesheet.InsertCantValues(cantId);
            newTimesheet.Justification = timesheetJustification;
            newTimesheet.Order = timesheetOrder;

            return newTimesheet;
        }

        /// <summary>
        /// Genera i cartellini categorizzati per diurne/notturne, ordinarie/straordinarie e festive.
        /// </summary>
        /// <param name="colId">L'id del collaboratore per cui è generato il nuovo timesheet</param>
        /// <param name="isDecimalHours">Indica se impostare la visualizzazione del timesheet in decimali o sessantesimi.</param>
        /// <param name="timesheetsToTotalize">La lista dei timesheet da sommare.</param>
        /// <param name="timesheetJustification">La motivazione da inserire all'interno dell'oggetto timesheet da generare.</param>
        /// <param name="firstMonthDate">La 1° data del mese di riferimento del timesheet.</param>
        /// <param name="lastMonthDate">L'ultima data del mese di rifermneto del timesheet.</param>
        /// <param name="timesheetOrder">L'ordine di visualizzazione da inserire nel timehseet.</param>
        /// <param name="cantId">L'id del cantiere a cui fanno riferimento le registrazioni da inserire nel timesheet; se 0 sarà trattato come non specificato.</param>
        /// <param name="requestedForWeeklyTotals">Indica che il piano è richiesto per un calcolo che prevede i totali settimanali.</param>
        /// <returns>La lista degli oggetti timesheet categorizzati per diurne/notturne, ordinarie/straordinarie e festive.</returns>
        private static List<TimesheetModuleItem> GenerateNewEditableTimesheet(int colId, bool isDecimalHours, DateTime firstMonthDate, DateTime lastMonthDate, TimesheetModuleItem plan)
        {
            // inizializzazione del valore di ritorno del metodo
            List<TimesheetModuleItem> cartelliniList = new List<TimesheetModuleItem>();
            
            // dichiarazione dei vari cartellini da utilizzare e visualizzare
            TimesheetModuleItem dayPlan,
                nightPlan,
                dayWorked,
                nightWorked,                
                tsOrdDiurne = new TimesheetModuleItem(isDecimalHours),
                tsStrDiurne = new TimesheetModuleItem(isDecimalHours),
                tsFestDiu = new TimesheetModuleItem(isDecimalHours),
                tsStrFest = new TimesheetModuleItem(isDecimalHours),
                tsOrdNot = new TimesheetModuleItem(isDecimalHours),
                tsStrNot = new TimesheetModuleItem(isDecimalHours),
                tsStrNotFest = new TimesheetModuleItem(isDecimalHours),
                tsOrdNotFest = new TimesheetModuleItem(isDecimalHours);

            // recupara il collaboratore da elaborare
            Col col = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colId);

            // calcola e assegna alle rispettive variabili i piani divisi per diurne e notturne
            bool isFreeTimesheet = false;
            int freeTimesheetId = 0;
            var plans = RepoManager.Tab_OrariRepo.GetDevidedPlanMinutes(colId, firstMonthDate, lastMonthDate, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col, out isFreeTimesheet, out freeTimesheetId);
            int tot_id = RepoManager.Tab_OrariRepo.GetTabOrariTipoIdFromEntity(colId);
            Tab_Orari_Tipo tot = null;
            if (tot_id != 0)
            {
                tot = RepoManager.Tab_OrariTipoRepo.First(ot => ot.Tab_Orari_Tipo_Id == tot_id);
            }
            bool assignNotDiuAutomatically;

            if (tot != null)
            {
                assignNotDiuAutomatically = tot.Tab_Orari_Tipo_NotDiu_Auto;
            }
            else
            {
                assignNotDiuAutomatically = true;
            }


            dayPlan = GenerateNewPlanTimesheet(isDecimalHours, plans[DayTimesheetKey], isFreeTimesheet, freeTimesheetId, colId, firstMonthDate, 0, PlanTypeEnum.Day);
            nightPlan = GenerateNewPlanTimesheet(isDecimalHours, plans[NightTimesheetKey], isFreeTimesheet, freeTimesheetId, colId, firstMonthDate, 0, PlanTypeEnum.Night);

            // calcola e assegna alle rispettive variabili i cartellini delle ore lavorate divise per diurne e notturne
            List<TimesheetModuleItem> dayNightTimesheets = GenerateDayNightTimesheets(firstMonthDate, isDecimalHours, colId, plan);
            if (!dayNightTimesheets.Any())
            {
                return cartelliniList;
            }
            dayWorked = dayNightTimesheets.First();
            nightWorked = dayNightTimesheets.Last();

            double ordDiu, strDiu, ordNot, strNot;
            DateTime processingDate = firstMonthDate;

            //Calcola i cartellini per ogni giorno del mese
            for (int i = 1, lastDay = lastMonthDate.Day; i <= lastDay; i++)
            {
                ordDiu = 0;
                strDiu = 0;
                ordNot = 0;
                strNot = 0;
                double pianoNotturne = (double)nightPlan["Day" + i.ToString("00")];
                double pianoDiurne = (double)dayPlan["Day" + i.ToString("00")];
                double effettiveDiurne = (double)dayWorked["Day" + i.ToString("00")];
                double effettiveNotturne = (double)nightWorked["Day" + i.ToString("00")];


                /* ASSEGNAZIONE AUTOMATICA PIANO DIURNO/NOTTURNO */
                // Se il flag sul orarioTipo è attivato e l'orario è solo durata
                // decide se il piano è diurno o notturno a seconda delle ore lavorative diurne e notturne effettuate
                if (assignNotDiuAutomatically)
                {
                    if (pianoNotturne == 0 && effettiveNotturne > effettiveDiurne)
                    {
                        pianoNotturne = pianoDiurne;
                        pianoDiurne = 0;
                    }
                }

                /* AGGIUSTAMENTI PIANI */
                //Se ho fatto meno diurne XOR meno notturne del previsto, bisogna aggiustare i piani
                //per evitare di segnare straordinarie che non sono state fatte

                if (effettiveDiurne <= pianoDiurne && effettiveNotturne > pianoNotturne)
                {
                    pianoNotturne = CommonService.SumDoubleHours(pianoNotturne, CommonService.SubtractDoubleHours(pianoDiurne, effettiveDiurne));
                }

                else if (effettiveNotturne <= pianoNotturne && effettiveDiurne > pianoDiurne)
                {
                    pianoDiurne = CommonService.SumDoubleHours(pianoDiurne, CommonService.SubtractDoubleHours(pianoNotturne, effettiveNotturne));
                }

                /* DIVISIONE ORDINARIE/STRAORDINARIE */
                //Se ci sono più ore lavorate che piano, assegna lo straordinario
                //Altrimenti non c'è straordinario

                if (effettiveDiurne > pianoDiurne)
                {
                    strDiu = CommonService.SubtractDoubleHours(effettiveDiurne, pianoDiurne);
                    ordDiu = pianoDiurne;
                }
                else
                {
                    strDiu = 0;
                    ordDiu = effettiveDiurne;
                }

                if (effettiveNotturne > pianoNotturne)
                {
                    strNot = CommonService.SubtractDoubleHours(effettiveNotturne, pianoNotturne);
                    ordNot = pianoNotturne;
                }
                else
                {
                    strNot = 0;
                    ordNot = effettiveNotturne;
                }


                /* ASSEGNAZIONE FESTIVO */
                //Se la giornata in elaborazione è festiva, le ore appena suddivise vanno nei cartellini festivi
                //Altrimenti vanno nei cartellini non festivi

                if (RepoManager.Tab_FestiviRepo.IsHolidayOrNotWorkDays(processingDate))
                {
                    tsOrdDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsOrdDiurne["Day" + i.ToString("00")] = 0;
                    tsStrDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrDiurne["Day" + i.ToString("00")] = 0;
                    tsOrdNot.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsOrdNot["Day" + i.ToString("00")] = 0;
                    tsStrNot.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrNot["Day" + i.ToString("00")] = 0;
                    tsFestDiu.SetHoursValue(i, CommonService.FromHoursToMinutes(ordDiu, isDecimalHours));
                    //tsFestDiu["Day" + i.ToString("00")] = ordDiu;
                    tsStrFest.SetHoursValue(i, CommonService.FromHoursToMinutes(strDiu, isDecimalHours));
                    //tsStrFest["Day" + i.ToString("00")] = strDiu;
                    tsOrdNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(ordNot, isDecimalHours));
                    //tsOrdNotFest["Day" + i.ToString("00")] = ordNot;
                    tsStrNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(strNot, isDecimalHours));
                    //tsStrNotFest["Day" + i.ToString("00")] = strNot;
                }
                else
                {
                    tsOrdDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(ordDiu, isDecimalHours));
                    //tsOrdDiurne["Day" + i.ToString("00")] = ordDiu;
                    tsStrDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(strDiu, isDecimalHours));
                    //tsStrDiurne["Day" + i.ToString("00")] = strDiu;
                    tsOrdNot.SetHoursValue(i, CommonService.FromHoursToMinutes(ordNot, isDecimalHours));
                    //tsOrdNot["Day" + i.ToString("00")] = ordNot;
                    tsStrNot.SetHoursValue(i, CommonService.FromHoursToMinutes(strNot, isDecimalHours));
                    //tsStrNot["Day" + i.ToString("00")] = strNot;
                    tsFestDiu.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsFestDiu["Day" + i.ToString("00")] = 0;
                    tsStrFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrFest["Day" + i.ToString("00")] = 0;
                    tsOrdNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsOrdNotFest["Day" + i.ToString("00")] = 0;
                    tsStrNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrNotFest["Day" + i.ToString("00")] = 0;
                }

                //Aumenta di un giorno la data in elaborazione
                processingDate = processingDate.AddDays(1);
            }

            //Imposta la motivazione dei cartellini utilizzando la Tab_Decod come resources (ci sarà bisogno di fare il processo inverso al salvataggio del cartellino)
            tsOrdDiurne.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_DIU").Decodifica_Tab;
            tsStrDiurne.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_DIU").Decodifica_Tab;
            tsOrdNot.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_NOT").Decodifica_Tab;
            tsStrNot.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_NOT").Decodifica_Tab;
            tsFestDiu.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_DIU_FEST").Decodifica_Tab;
            tsStrFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_DIU_FEST").Decodifica_Tab;
            tsOrdNotFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_NOT_FEST").Decodifica_Tab;
            tsStrNotFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_NOT_FEST").Decodifica_Tab;

            //Aggiunge tutti i cartellini alla lista di ritorno
            cartelliniList.Add(tsOrdDiurne);
            cartelliniList.Add(tsStrDiurne);
            cartelliniList.Add(tsOrdNot);
            cartelliniList.Add(tsStrNot);
            cartelliniList.Add(tsFestDiu);
            cartelliniList.Add(tsStrFest);
            cartelliniList.Add(tsOrdNotFest);
            cartelliniList.Add(tsStrNotFest);

            //Assegna a tutti i cartellini le impostazioni di base
            foreach (var cartellino in cartelliniList)
            {
                cartellino.IsFromFreeTimeSheet = false;
                cartellino.FreeTimeSheetId = 0;
                cartellino.InsertColValues(colId);
                cartellino.StartDate = firstMonthDate;
            }

            return cartelliniList;
        }

        private static TimesheetModuleItem GenerateNewEditableTimesheetExport(int colId, int cantId, bool isDecimalHours, DateTime firstMonthDate, DateTime lastMonthDate, TimesheetModuleItem plan, TimesheetModuleItem worked)
        {
            // inizializzazione del valore di ritorno del metodo
            List<TimesheetModuleItem> cartelliniList = new List<TimesheetModuleItem>();

            // dichiarazione dei vari cartellini da utilizzare e visualizzare
            TimesheetModuleItem dayPlan,
                nightPlan,
                dayWorked,
                nightWorked,
                tsOrdDiurne = new TimesheetModuleItem(isDecimalHours),
                tsStrDiurne = new TimesheetModuleItem(isDecimalHours),
                tsFestDiu = new TimesheetModuleItem(isDecimalHours),
                tsStrFest = new TimesheetModuleItem(isDecimalHours),
                tsOrdNot = new TimesheetModuleItem(isDecimalHours),
                tsStrNot = new TimesheetModuleItem(isDecimalHours),
                tsStrNotFest = new TimesheetModuleItem(isDecimalHours),
                tsOrdNotFest = new TimesheetModuleItem(isDecimalHours);

            // recupara il collaboratore da elaborare
            Col col = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colId);

            // calcola e assegna alle rispettive variabili i piani divisi per diurne e notturne
            bool isFreeTimesheet = false;
            int freeTimesheetId = 0;
            var plans = RepoManager.Tab_OrariRepo.GetDevidedPlanMinutes(colId, firstMonthDate, lastMonthDate, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col, out isFreeTimesheet, out freeTimesheetId);
            int tot_id = RepoManager.Tab_OrariRepo.GetTabOrariTipoIdFromEntity(colId);
            Tab_Orari_Tipo tot = null;
            if (tot_id != 0)
            {
                tot = RepoManager.Tab_OrariTipoRepo.First(ot => ot.Tab_Orari_Tipo_Id == tot_id);
            }
            bool assignNotDiuAutomatically;

            if (tot != null)
            {
                assignNotDiuAutomatically = tot.Tab_Orari_Tipo_NotDiu_Auto;
            }
            else
            {
                assignNotDiuAutomatically = true;
            }


            dayPlan = plan;
            nightPlan = GenerateNewPlanTimesheet(isDecimalHours, plans[NightTimesheetKey], isFreeTimesheet, freeTimesheetId, colId, firstMonthDate, 0, PlanTypeEnum.Night);

            // calcola e assegna alle rispettive variabili i cartellini delle ore lavorate divise per diurne e notturne
            List<TimesheetModuleItem> dayNightTimesheets = GenerateDayNightTimesheets(firstMonthDate, isDecimalHours, colId, plan);
            //if (!dayNightTimesheets.Any())
            //{
            //    return cartelliniList;
            //}
            dayWorked = worked;
            nightWorked = dayNightTimesheets.Last();

            double ordDiu, strDiu, ordNot, strNot;
            DateTime processingDate = firstMonthDate;

            //Calcola i cartellini per ogni giorno del mese
            for (int i = 1, lastDay = lastMonthDate.Day; i <= lastDay; i++)
            {
                ordDiu = 0;
                strDiu = 0;
                ordNot = 0;
                strNot = 0;
                double pianoNotturne = (double)nightPlan["Day" + i.ToString("00")];
                double pianoDiurne = (double)dayPlan["Day" + i.ToString("00")];
                double effettiveDiurne = (double)dayWorked["Day" + i.ToString("00")];
                double effettiveNotturne = (double)nightWorked["Day" + i.ToString("00")];


                /* ASSEGNAZIONE AUTOMATICA PIANO DIURNO/NOTTURNO */
                // Se il flag sul orarioTipo è attivato e l'orario è solo durata
                // decide se il piano è diurno o notturno a seconda delle ore lavorative diurne e notturne effettuate
                if (assignNotDiuAutomatically)
                {
                    if (pianoNotturne == 0 && effettiveNotturne > effettiveDiurne)
                    {
                        pianoNotturne = pianoDiurne;
                        pianoDiurne = 0;
                    }
                }

                /* AGGIUSTAMENTI PIANI */
                //Se ho fatto meno diurne XOR meno notturne del previsto, bisogna aggiustare i piani
                //per evitare di segnare straordinarie che non sono state fatte

                if (effettiveDiurne <= pianoDiurne && effettiveNotturne > pianoNotturne)
                {
                    pianoNotturne = CommonService.SumDoubleHours(pianoNotturne, CommonService.SubtractDoubleHours(pianoDiurne, effettiveDiurne));
                }

                else if (effettiveNotturne <= pianoNotturne && effettiveDiurne > pianoDiurne)
                {
                    pianoDiurne = CommonService.SumDoubleHours(pianoDiurne, CommonService.SubtractDoubleHours(pianoNotturne, effettiveNotturne));
                }

                /* DIVISIONE ORDINARIE/STRAORDINARIE */
                //Se ci sono più ore lavorate che piano, assegna lo straordinario
                //Altrimenti non c'è straordinario

                if (effettiveDiurne > pianoDiurne)
                {
                    strDiu = CommonService.SubtractDoubleHours(effettiveDiurne, pianoDiurne, isDecimalHours);
                    ordDiu = pianoDiurne;
                }
                else
                {
                    strDiu = 0;
                    ordDiu = effettiveDiurne;
                }

                if (effettiveNotturne > pianoNotturne)
                {
                    strNot = CommonService.SubtractDoubleHours(effettiveNotturne, pianoNotturne);
                    ordNot = pianoNotturne;
                }
                else
                {
                    strNot = 0;
                    ordNot = effettiveNotturne;
                }


                /* ASSEGNAZIONE FESTIVO */
                //Se la giornata in elaborazione è festiva, le ore appena suddivise vanno nei cartellini festivi
                //Altrimenti vanno nei cartellini non festivi

                if (RepoManager.Tab_FestiviRepo.IsHolidayOrNotWorkDays(processingDate))
                {
                    tsOrdDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsOrdDiurne["Day" + i.ToString("00")] = 0;
                    tsStrDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(strDiu, isDecimalHours));
                    //tsStrDiurne["Day" + i.ToString("00")] = 0;
                    tsOrdNot.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsOrdNot["Day" + i.ToString("00")] = 0;
                    tsStrNot.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrNot["Day" + i.ToString("00")] = 0;
                    tsFestDiu.SetHoursValue(i, CommonService.FromHoursToMinutes(ordDiu, isDecimalHours));
                    //tsFestDiu["Day" + i.ToString("00")] = ordDiu;
                    tsStrFest.SetHoursValue(i, CommonService.FromHoursToMinutes(strDiu, isDecimalHours));
                    //tsStrFest["Day" + i.ToString("00")] = strDiu;
                    tsOrdNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(ordNot, isDecimalHours));
                    //tsOrdNotFest["Day" + i.ToString("00")] = ordNot;
                    tsStrNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(strNot, isDecimalHours));
                    //tsStrNotFest["Day" + i.ToString("00")] = strNot;
                }
                else
                {
                    tsOrdDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(ordDiu, isDecimalHours));
                    //tsOrdDiurne["Day" + i.ToString("00")] = ordDiu;
                    tsStrDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(strDiu, isDecimalHours));
                    //tsStrDiurne["Day" + i.ToString("00")] = strDiu;
                    tsOrdNot.SetHoursValue(i, CommonService.FromHoursToMinutes(ordNot, isDecimalHours));
                    //tsOrdNot["Day" + i.ToString("00")] = ordNot;
                    tsStrNot.SetHoursValue(i, CommonService.FromHoursToMinutes(strNot, isDecimalHours));
                    //tsStrNot["Day" + i.ToString("00")] = strNot;
                    tsFestDiu.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsFestDiu["Day" + i.ToString("00")] = 0;
                    tsStrFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrFest["Day" + i.ToString("00")] = 0;
                    tsOrdNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsOrdNotFest["Day" + i.ToString("00")] = 0;
                    tsStrNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrNotFest["Day" + i.ToString("00")] = 0;
                }

                //Aumenta di un giorno la data in elaborazione
                processingDate = processingDate.AddDays(1);
            }

            //Imposta la motivazione dei cartellini utilizzando la Tab_Decod come resources (ci sarà bisogno di fare il processo inverso al salvataggio del cartellino)
            tsOrdDiurne.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_DIU").Decodifica_Tab;
            tsStrDiurne.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_DIU").Decodifica_Tab;
            tsOrdNot.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_NOT").Decodifica_Tab;
            tsStrNot.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_NOT").Decodifica_Tab;
            tsFestDiu.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_DIU_FEST").Decodifica_Tab;
            tsStrFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_DIU_FEST").Decodifica_Tab;
            tsOrdNotFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_NOT_FEST").Decodifica_Tab;
            tsStrNotFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_NOT_FEST").Decodifica_Tab;

            //Aggiunge tutti i cartellini alla lista di ritorno
            //cartelliniList.Add(tsOrdDiurne);
            cartelliniList.Add(tsStrDiurne);
            //cartelliniList.Add(tsOrdNot);
            //cartelliniList.Add(tsStrNot);
            //cartelliniList.Add(tsFestDiu);
            //cartelliniList.Add(tsStrFest);
            //cartelliniList.Add(tsOrdNotFest);
            //cartelliniList.Add(tsStrNotFest);

            //Assegna a tutti i cartellini le impostazioni di base
            foreach (var cartellino in cartelliniList)
            {
                cartellino.IsFromFreeTimeSheet = false;
                cartellino.FreeTimeSheetId = 0;
                cartellino.InsertColValues(colId);
                cartellino.StartDate = firstMonthDate;
                cartellino.InsertCantValues(cantId);
            }

            return tsStrDiurne;
        }

        private static TimesheetModuleItem GenerateNewEditableTimesheetExportStr(int colId, int cantId, bool isDecimalHours, DateTime firstMonthDate, DateTime lastMonthDate, TimesheetModuleItem worked)
        {
            // inizializzazione del valore di ritorno del metodo
            List<TimesheetModuleItem> cartelliniList = new List<TimesheetModuleItem>();

            // dichiarazione dei vari cartellini da utilizzare e visualizzare
            TimesheetModuleItem dayPlan,
                nightPlan,
                dayWorked,
                nightWorked,
                tsOrdDiurne = new TimesheetModuleItem(isDecimalHours),
                tsStrDiurne = new TimesheetModuleItem(isDecimalHours),
                tsFestDiu = new TimesheetModuleItem(isDecimalHours),
                tsStrFest = new TimesheetModuleItem(isDecimalHours),
                tsOrdNot = new TimesheetModuleItem(isDecimalHours),
                tsStrNot = new TimesheetModuleItem(isDecimalHours),
                tsStrNotFest = new TimesheetModuleItem(isDecimalHours),
                tsOrdNotFest = new TimesheetModuleItem(isDecimalHours);

            // recupara il collaboratore da elaborare
            Col col = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colId);

            // calcola e assegna alle rispettive variabili i piani divisi per diurne e notturne
            bool isFreeTimesheet = false;
            int freeTimesheetId = 0;
            var plans = RepoManager.Tab_OrariRepo.GetDevidedPlanMinutes(colId, firstMonthDate, lastMonthDate, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col, out isFreeTimesheet, out freeTimesheetId);
            int tot_id = RepoManager.Tab_OrariRepo.GetTabOrariTipoIdFromEntity(colId);
            Tab_Orari_Tipo tot = null;
            if (tot_id != 0)
            {
                tot = RepoManager.Tab_OrariTipoRepo.First(ot => ot.Tab_Orari_Tipo_Id == tot_id);
            }
            bool assignNotDiuAutomatically;

            if (tot != null)
            {
                assignNotDiuAutomatically = tot.Tab_Orari_Tipo_NotDiu_Auto;
            }
            else
            {
                assignNotDiuAutomatically = true;
            }

            nightPlan = GenerateNewPlanTimesheet(isDecimalHours, plans[NightTimesheetKey], isFreeTimesheet, freeTimesheetId, colId, firstMonthDate, 0, PlanTypeEnum.Night);

            // calcola e assegna alle rispettive variabili i cartellini delle ore lavorate divise per diurne e notturne
            //if (!dayNightTimesheets.Any())
            //{
            //    return cartelliniList;
            //}
            dayWorked = worked;

            double ordDiu, strDiu, ordNot, strNot;
            DateTime processingDate = firstMonthDate;

            //Calcola i cartellini per ogni giorno del mese
            for (int i = 1, lastDay = lastMonthDate.Day; i <= lastDay; i++)
            {
                double pianoNotturne = (double)nightPlan["Day" + i.ToString("00")];
                double effettiveDiurne = (double)dayWorked["Day" + i.ToString("00")];
                strDiu = 0;
                ordDiu = effettiveDiurne;

                /* ASSEGNAZIONE FESTIVO */
                //Se la giornata in elaborazione è festiva, le ore appena suddivise vanno nei cartellini festivi
                //Altrimenti vanno nei cartellini non festivi

                if (RepoManager.Tab_FestiviRepo.IsHolidayOrNotWorkDays(processingDate))
                {
                    tsOrdDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(ordDiu, isDecimalHours));
                    //tsOrdDiurne["Day" + i.ToString("00")] = 0;
                    tsStrDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(ordDiu, isDecimalHours));
                    //tsStrDiurne["Day" + i.ToString("00")] = 0;
                    tsOrdNot.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsOrdNot["Day" + i.ToString("00")] = 0;
                    tsStrNot.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrNot["Day" + i.ToString("00")] = 0;
                    tsFestDiu.SetHoursValue(i, CommonService.FromHoursToMinutes(ordDiu, isDecimalHours));
                    //tsFestDiu["Day" + i.ToString("00")] = ordDiu;
                    tsStrFest.SetHoursValue(i, CommonService.FromHoursToMinutes(strDiu, isDecimalHours));
                    //tsStrFest["Day" + i.ToString("00")] = strDiu;
                }
                else
                {
                    tsOrdDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(ordDiu, isDecimalHours));
                    //tsOrdDiurne["Day" + i.ToString("00")] = ordDiu;
                    tsStrDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(ordDiu, isDecimalHours));
                    //tsStrDiurne["Day" + i.ToString("00")] = strDiu;
                    tsFestDiu.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsFestDiu["Day" + i.ToString("00")] = 0;
                    tsStrFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrFest["Day" + i.ToString("00")] = 0;
                    tsOrdNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsOrdNotFest["Day" + i.ToString("00")] = 0;
                    tsStrNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrNotFest["Day" + i.ToString("00")] = 0;
                }

                //Aumenta di un giorno la data in elaborazione
                processingDate = processingDate.AddDays(1);
            }

            //Imposta la motivazione dei cartellini utilizzando la Tab_Decod come resources (ci sarà bisogno di fare il processo inverso al salvataggio del cartellino)
            tsOrdDiurne.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_DIU").Decodifica_Tab;
            tsStrDiurne.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_DIU").Decodifica_Tab;
            tsOrdNot.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_NOT").Decodifica_Tab;
            tsStrNot.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_NOT").Decodifica_Tab;
            tsFestDiu.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_DIU_FEST").Decodifica_Tab;
            tsStrFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_DIU_FEST").Decodifica_Tab;
            tsOrdNotFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_NOT_FEST").Decodifica_Tab;
            tsStrNotFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_NOT_FEST").Decodifica_Tab;

            //Aggiunge tutti i cartellini alla lista di ritorno
            cartelliniList.Add(tsOrdDiurne);
            //cartelliniList.Add(tsStrDiurne);
            //cartelliniList.Add(tsOrdNot);
            //cartelliniList.Add(tsStrNot);
            //cartelliniList.Add(tsFestDiu);
            //cartelliniList.Add(tsStrFest);
            //cartelliniList.Add(tsOrdNotFest);
            //cartelliniList.Add(tsStrNotFest);

            //Assegna a tutti i cartellini le impostazioni di base
            foreach (var cartellino in cartelliniList)
            {
                cartellino.IsFromFreeTimeSheet = false;
                cartellino.FreeTimeSheetId = 0;
                cartellino.InsertColValues(colId);
                cartellino.StartDate = firstMonthDate;
                cartellino.InsertCantValues(cantId);
                tsOrdDiurne = cartellino;
            }

            return tsOrdDiurne;
        }

        /// <summary>
        /// Genera i cartellini categorizzati per diurne/notturne, ordinarie/straordinarie e festive senza inserire le ore negli strardinari.
        /// </summary>
        /// <param name="colId">L'id del collaboratore per cui è generato il nuovo timesheet</param>
        /// <param name="isDecimalHours">Indica se impostare la visualizzazione del timesheet in decimali o sessantesimi.</param>
        /// <param name="timesheetsToTotalize">La lista dei timesheet da sommare.</param>
        /// <param name="timesheetJustification">La motivazione da inserire all'interno dell'oggetto timesheet da generare.</param>
        /// <param name="firstMonthDate">La 1° data del mese di riferimento del timesheet.</param>
        /// <param name="lastMonthDate">L'ultima data del mese di rifermneto del timesheet.</param>
        /// <param name="timesheetOrder">L'ordine di visualizzazione da inserire nel timehseet.</param>
        /// <param name="cantId">L'id del cantiere a cui fanno riferimento le registrazioni da inserire nel timesheet; se 0 sarà trattato come non specificato.</param>
        /// <param name="requestedForWeeklyTotals">Indica che il piano è richiesto per un calcolo che prevede i totali settimanali.</param>
        /// <returns>La lista degli oggetti timesheet categorizzati per diurne/notturne, ordinarie/straordinarie e festive.</returns>
        private static List<TimesheetModuleItem> GenerateNewEditableTimesheetNoStr(int colId, bool isDecimalHours, DateTime firstMonthDate, DateTime lastMonthDate, TimesheetModuleItem plan)
        {
            // inizializzazione del valore di ritorno del metodo
            List<TimesheetModuleItem> cartelliniList = new List<TimesheetModuleItem>();

            // dichiarazione dei vari cartellini da utilizzare e visualizzare
            TimesheetModuleItem dayPlan,
                nightPlan,
                dayWorked,
                nightWorked,
                tsOrdDiurne = new TimesheetModuleItem(isDecimalHours),
                tsStrDiurne = new TimesheetModuleItem(isDecimalHours),
                tsFestDiu = new TimesheetModuleItem(isDecimalHours),
                tsStrFest = new TimesheetModuleItem(isDecimalHours),
                tsOrdNot = new TimesheetModuleItem(isDecimalHours),
                tsStrNot = new TimesheetModuleItem(isDecimalHours),
                tsStrNotFest = new TimesheetModuleItem(isDecimalHours),
                tsOrdNotFest = new TimesheetModuleItem(isDecimalHours);

            // recupara il collaboratore da elaborare
            Col col = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colId);

            // calcola e assegna alle rispettive variabili i piani divisi per diurne e notturne
            bool isFreeTimesheet = false;
            int freeTimesheetId = 0;
            var plans = RepoManager.Tab_OrariRepo.GetDevidedPlanMinutes(colId, firstMonthDate, lastMonthDate, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col, out isFreeTimesheet, out freeTimesheetId);
            int tot_id = RepoManager.Tab_OrariRepo.GetTabOrariTipoIdFromEntity(colId);
            Tab_Orari_Tipo tot = null;
            if (tot_id != 0)
            {
                tot = RepoManager.Tab_OrariTipoRepo.First(ot => ot.Tab_Orari_Tipo_Id == tot_id);
            }
            bool assignNotDiuAutomatically;

            if (tot != null)
            {
                assignNotDiuAutomatically = tot.Tab_Orari_Tipo_NotDiu_Auto;
            }
            else
            {
                assignNotDiuAutomatically = true;
            }


            dayPlan = GenerateNewPlanTimesheet(isDecimalHours, plans[DayTimesheetKey], isFreeTimesheet, freeTimesheetId, colId, firstMonthDate, 0, PlanTypeEnum.Day);
            nightPlan = GenerateNewPlanTimesheet(isDecimalHours, plans[NightTimesheetKey], isFreeTimesheet, freeTimesheetId, colId, firstMonthDate, 0, PlanTypeEnum.Night);

            // calcola e assegna alle rispettive variabili i cartellini delle ore lavorate divise per diurne e notturne
            List<TimesheetModuleItem> dayNightTimesheets = GenerateDayNightTimesheets(firstMonthDate, isDecimalHours, colId, plan);
            if (!dayNightTimesheets.Any())
            {
                return cartelliniList;
            }
            dayWorked = dayNightTimesheets.First();
            nightWorked = dayNightTimesheets.Last();

            double ordDiu, strDiu, ordNot, strNot;
            DateTime processingDate = firstMonthDate;

            //Calcola i cartellini per ogni giorno del mese
            for (int i = 1, lastDay = lastMonthDate.Day; i <= lastDay; i++)
            {
                ordDiu = 0;
                strDiu = 0;
                ordNot = 0;
                strNot = 0;
                double pianoNotturne = (double)nightPlan["Day" + i.ToString("00")];
                double pianoDiurne = (double)dayPlan["Day" + i.ToString("00")];
                double effettiveDiurne = (double)dayWorked["Day" + i.ToString("00")];
                double effettiveNotturne = (double)nightWorked["Day" + i.ToString("00")];


                /* ASSEGNAZIONE AUTOMATICA PIANO DIURNO/NOTTURNO */
                // Se il flag sul orarioTipo è attivato e l'orario è solo durata
                // decide se il piano è diurno o notturno a seconda delle ore lavorative diurne e notturne effettuate
                if (assignNotDiuAutomatically)
                {
                    if (pianoNotturne == 0 && effettiveNotturne > effettiveDiurne)
                    {
                        pianoNotturne = pianoDiurne;
                        pianoDiurne = 0;
                    }
                }

                /* AGGIUSTAMENTI PIANI */
                //Se ho fatto meno diurne XOR meno notturne del previsto, bisogna aggiustare i piani
                //per evitare di segnare straordinarie che non sono state fatte

                if (effettiveDiurne <= pianoDiurne && effettiveNotturne > pianoNotturne)
                {
                    pianoNotturne = CommonService.SumDoubleHours(pianoNotturne, CommonService.SubtractDoubleHours(pianoDiurne, effettiveDiurne));
                }

                else if (effettiveNotturne <= pianoNotturne && effettiveDiurne > pianoDiurne)
                {
                    pianoDiurne = CommonService.SumDoubleHours(pianoDiurne, CommonService.SubtractDoubleHours(pianoNotturne, effettiveNotturne));
                }

                /* DIVISIONE ORDINARIE/STRAORDINARIE */
                //Se ci sono più ore lavorate che piano, assegna lo straordinario
                //Altrimenti non c'è straordinario

                if (effettiveDiurne > pianoDiurne)
                {
                    strDiu = CommonService.SubtractDoubleHours(effettiveDiurne, pianoDiurne);
                    ordDiu = pianoDiurne;
                }
                else
                {
                    strDiu = 0;
                    ordDiu = effettiveDiurne;
                }

                if (effettiveNotturne > pianoNotturne)
                {
                    strNot = CommonService.SubtractDoubleHours(effettiveNotturne, pianoNotturne);
                    ordNot = pianoNotturne;
                }
                else
                {
                    strNot = 0;
                    ordNot = effettiveNotturne;
                }


                /* ASSEGNAZIONE FESTIVO */
                //Se la giornata in elaborazione è festiva, le ore appena suddivise vanno nei cartellini festivi
                //Altrimenti vanno nei cartellini non festivi
                ordNot = ordNot + strNot;
                if (RepoManager.Tab_FestiviRepo.IsHolidayOrNotWorkDays(processingDate))
                {
                    tsOrdDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsOrdDiurne["Day" + i.ToString("00")] = 0;
                    tsStrDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrDiurne["Day" + i.ToString("00")] = 0;
                    tsOrdNot.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsOrdNot["Day" + i.ToString("00")] = 0;
                    tsStrNot.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrNot["Day" + i.ToString("00")] = 0;
                    tsFestDiu.SetHoursValue(i, CommonService.FromHoursToMinutes(ordDiu, isDecimalHours));
                    //tsFestDiu["Day" + i.ToString("00")] = ordDiu;
                    //tsStrFest.SetHoursValue(i, CommonService.FromHoursToMinutes(strDiu, isDecimalHours));
                    //tsStrFest["Day" + i.ToString("00")] = strDiu;
                    tsOrdNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(ordNot, isDecimalHours));
                    //tsOrdNotFest["Day" + i.ToString("00")] = ordNot;
                    tsStrNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(strNot, isDecimalHours));
                    //tsStrNotFest["Day" + i.ToString("00")] = strNot;
                }
                else
                {
                    tsOrdDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(ordDiu, isDecimalHours));
                    //tsOrdDiurne["Day" + i.ToString("00")] = ordDiu;
                    tsStrDiurne.SetHoursValue(i, CommonService.FromHoursToMinutes(strDiu, isDecimalHours));
                    //tsStrDiurne["Day" + i.ToString("00")] = strDiu;
                    tsOrdNot.SetHoursValue(i, CommonService.FromHoursToMinutes(ordNot, isDecimalHours));
                    //tsOrdNot["Day" + i.ToString("00")] = ordNot;
                    //tsStrNot.SetHoursValue(i, CommonService.FromHoursToMinutes(strNot, isDecimalHours));
                    //tsStrNot["Day" + i.ToString("00")] = strNot;
                    tsFestDiu.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsFestDiu["Day" + i.ToString("00")] = 0;
                    tsStrFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrFest["Day" + i.ToString("00")] = 0;
                    tsOrdNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsOrdNotFest["Day" + i.ToString("00")] = 0;
                    tsStrNotFest.SetHoursValue(i, CommonService.FromHoursToMinutes(0, isDecimalHours));
                    //tsStrNotFest["Day" + i.ToString("00")] = 0;
                }

                //Aumenta di un giorno la data in elaborazione
                processingDate = processingDate.AddDays(1);
            }

            //Imposta la motivazione dei cartellini utilizzando la Tab_Decod come resources (ci sarà bisogno di fare il processo inverso al salvataggio del cartellino)
            tsOrdDiurne.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_DIU").Decodifica_Tab;
            tsStrDiurne.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_DIU").Decodifica_Tab;
            tsOrdNot.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_NOT").Decodifica_Tab;
            //tsStrNot.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_NOT").Decodifica_Tab;
            tsFestDiu.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_DIU_FEST").Decodifica_Tab;
            tsStrFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_DIU_FEST").Decodifica_Tab;
            tsOrdNotFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_NOT_FEST").Decodifica_Tab;
            //tsStrNotFest.Justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_NOT_FEST").Decodifica_Tab;
            
            //Aggiunge tutti i cartellini alla lista di ritorno
            cartelliniList.Add(tsOrdDiurne);
            cartelliniList.Add(tsStrDiurne);
            cartelliniList.Add(tsOrdNot);
            //cartelliniList.Add(tsStrNot);
            cartelliniList.Add(tsFestDiu);
            cartelliniList.Add(tsStrFest);
            cartelliniList.Add(tsOrdNotFest);
            //cartelliniList.Add(tsStrNotFest);

            //Assegna a tutti i cartellini le impostazioni di base
            foreach (var cartellino in cartelliniList)
            {
                cartellino.IsFromFreeTimeSheet = false;
                cartellino.FreeTimeSheetId = 0;
                cartellino.InsertColValues(colId);
                cartellino.StartDate = firstMonthDate;
            }

            return cartelliniList;
        }


        /// <summary>
        /// Genera un nuovo timesheet di utilizzando le registrazioni passate come parametro per la motivazione passata come parametro.
        /// </summary>
        /// <param name="colId">L'id del collaboratore per cui è generato il nuovo timesheet</param>
        /// <param name="isDecimalHours">Indica se impostare la visualizzazione del timesheet in decimali o sessantesimi.</param>
        /// <param name="timesheetRegVs">La lista di registrazioni con cui compilare il timesheet.</param>
        /// <param name="timesheetJustification">La motivazione da inserire all'interno dell'oggetto timesheet da generare.</param>
        /// <param name="firstMonthDate">La 1° data del mese di riferimento del timesheet.</param>
        /// <param name="lastMonthDate">L'ultima data del mese di rifermneto del timesheet.</param>
        /// <param name="timesheetOrder">L'ordine di visualizzazione da inserire nel timehseet.</param>
        /// <param name="cantId">L'id del cantiere a cui fanno riferimento le registrazioni da inserire nel timesheet; se 0 sarà trattato come non specificato.</param>
        /// <param name="requestedForWeeklyTotals">Indica che il piano è richiesto per un calcolo che prevede i totali settimanali.</param>
        /// <returns>L'oggetto timesheet rapporesentante i parametri passati al metodo.</returns>
        private static TimesheetModuleItem subtractTimesheets(int colId, bool isDecimalHours, TimesheetModuleItem timesheet1, TimesheetModuleItem timesheet2,TimesheetModuleItem timesheet3, string timesheetJustification, DateTime firstMonthDate,
            DateTime lastMonthDate, int timesheetOrder, int cantId, bool requestedForWeeklyTotals)
        {
            // inizializzazione del valore di ritorno del metodo
            var newTimesheet = new TimesheetModuleItem(isDecimalHours);

            // inserimento della data che indica il mese di elaborazione
            newTimesheet.StartDate = firstMonthDate;
            DateTime processingDate = firstMonthDate;
            DateTime startDate = firstMonthDate;
            DateTime endDate = lastMonthDate;

            //Se il collaboratore non ha piano orario non deve essere calcolato il delta;
            //Per fare ciò viene settato un booleano che verrà utilizzato durante il relat. calcolo
            var currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == colId);
            bool has_orario = currentCol.Tab_Orari_Tipo_Id.HasValue ? true : false;

            if (requestedForWeeklyTotals && startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
            {
                startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
            }

            if (requestedForWeeklyTotals && endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
            {
                endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                // la data di fine viene portata alle 23:59 così da recuperare anche le timbrature della giornata di fine
                endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);
            }

            // inizializzazione del piano vuoto in cui andare a compilare i totali per giornata
            var daysMinutes = RepoManager.Tab_OrariRepo.GetEmptyMinutesPlan(startDate, endDate, requestedForWeeklyTotals);
            double dayTotal, today1, today2, today3;

            if (startDate < firstMonthDate)
            {
                for (int i = firstMonthDate.AddTicks(-1 * startDate.Ticks).Day - 1; i > 0; i--)
                {
                    //Recupera le ore di piano e le ore lavorate del giorno in elaborazione
                    today1 = CommonService.FromHoursToMinutes((double)timesheet1["DayMinus" + i], isDecimalHours);// (Math.Truncate((double)timesheet1["DayMinus" + i]) * 60) + Math.Round(((double)timesheet1["DayMinus" + i] - Math.Truncate((double)timesheet1["DayMinus" + i])) * 100);
                    today2 = CommonService.FromHoursToMinutes((double)timesheet2["DayMinus" + i], isDecimalHours);// (Math.Truncate((double)timesheet2["DayMinus" + i]) * 60) + Math.Round(((double)timesheet2["DayMinus" + i] - Math.Truncate((double)timesheet2["DayMinus" + i])) * 100);
                    today3 = CommonService.FromHoursToMinutes((double)timesheet3["DayMinus" + i], isDecimalHours);// (Math.Truncate((double)timesheet2["DayMinus" + i]) * 60) + Math.Round(((double)timesheet2["DayMinus" + i] - Math.Truncate((double)timesheet2["DayMinus" + i])) * 100);
                    //Se la differenza è positiva, si fa la sottrazione, altrimenti si lascia 0; Se non ha orario prestabilito è 0
                    if (has_orario)
                    {
                        dayTotal = today1 - today2;
                        dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                    }
                    else if (today3 != 0) {
                        dayTotal = today1 - today3;
                        dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                    }
                    else
                    {
                        dayTotal = 0;
                    }


                    if (daysMinutes.ContainsKey(firstMonthDate.AddDays(-1 * i).Date))
                    {
                        daysMinutes[firstMonthDate.AddDays(-1 * i).Date] = new Tuple<double, TimeSpan?, TimeSpan?>(dayTotal, null, null);
                    }
                    startDate = startDate.AddDays(1);
                }
            }

            // per ogni giorno del mese calcolo i totali e inserisco nel 'piano'
            for (int i = 1; i <= lastMonthDate.Day; i++)
            {
                //Recupera le ore di piano e le ore lavorate del giorno in elaborazione
                today1 = CommonService.FromHoursToMinutes((double)timesheet1["Day" + i.ToString("00")], isDecimalHours);// (Math.Truncate((double)timesheet1["Day" + i.ToString("00")]) * 60) + Math.Round(((double)timesheet1["Day" + i.ToString("00")] - Math.Truncate((double)timesheet1["Day" + i.ToString("00")])) * 100);
                today2 = CommonService.FromHoursToMinutes((double)timesheet2["Day" + i.ToString("00")], isDecimalHours);// (Math.Truncate((double)timesheet2["Day" + i.ToString("00")]) * 60) + Math.Round(((double)timesheet2["Day" + i.ToString("00")] - Math.Truncate((double)timesheet2["Day" + i.ToString("00")])) * 100);
                today3 = CommonService.FromHoursToMinutes((double)timesheet3["Day" + i.ToString("00")], isDecimalHours);// (Math.Truncate((double)timesheet2["Day" + i.ToString("00")]) * 60) + Math.Round(((double)timesheet2["Day" + i.ToString("00")] - Math.Truncate((double)timesheet2["Day" + i.ToString("00")])) * 100);
                                                                                                                        //Se la differenza è positiva, si fa la sottrazione, altrimenti si lascia 0; Se non ha orario prestabilito è 0
                if (has_orario)
                {
                    dayTotal = today1 - today2;
                    dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                }
                else if (today3 != 0)
                {
                    dayTotal = today1 - today3;
                    dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                }
                else
                {
                    dayTotal = 0;
                }


                if (daysMinutes.ContainsKey(processingDate))
                {
                    daysMinutes[processingDate] = new Tuple<double, TimeSpan?, TimeSpan?>(dayTotal, null, null);
                }

                processingDate = processingDate.AddDays(1);
            }

            if (endDate > lastMonthDate)
            {
                for (int i = 1, j = endDate.Day; i <= j; i++)
                {
                    //Recupera le ore di piano e le ore lavorate del giorno in elaborazione
                    today1 = CommonService.FromHoursToMinutes((double)timesheet1["DayPlus" + i], isDecimalHours); // (Math.Truncate((double)timesheet1["DayPlus" + i]) * 60) + Math.Round(((double)timesheet1["DayPlus" + i] - Math.Truncate((double)timesheet1["DayPlus" + i])) * 100);
                    today2 = CommonService.FromHoursToMinutes((double)timesheet2["DayPlus" + i], isDecimalHours); // (Math.Truncate((double)timesheet2["DayPlus" + i]) * 60) + Math.Round(((double)timesheet2["DayPlus" + i] - Math.Truncate((double)timesheet2["DayPlus" + i])) * 100);
                    today3 = CommonService.FromHoursToMinutes((double)timesheet3["DayPlus" + i], isDecimalHours); // (Math.Truncate((double)timesheet2["DayMinus" + i]) * 60) + Math.Round(((double)timesheet2["DayMinus" + i] - Math.Truncate((double)timesheet2["DayMinus" + i])) * 100);
                    //Se la differenza è positiva, si fa la sottrazione, altrimenti si lascia 0; Se non ha orario prestabilito è 0
                    if (has_orario)
                    {
                        dayTotal = today1 - today2;
                        dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                    }
                    else if (today3 != 0)
                    {
                        dayTotal = today1 - today3;
                        dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                    }
                    else
                    {
                        dayTotal = 0;
                    }


                    if (daysMinutes.ContainsKey(lastMonthDate.AddDays(i).Date))
                    {
                        daysMinutes[lastMonthDate.AddDays(i).Date] = new Tuple<double, TimeSpan?, TimeSpan?>(dayTotal, null, null);
                    }

                    endDate.AddDays(1);
                }
            }

            // inserimento del calcolo dei totali all'interno dell'oggetto timesheet
            newTimesheet.PopulateHoursWithDate(daysMinutes);
            newTimesheet.IsFromFreeTimeSheet = false;
            newTimesheet.FreeTimeSheetId = 0;
            newTimesheet.InsertColValues(colId);
            newTimesheet.InsertCantValues(cantId);
            newTimesheet.Justification = timesheetJustification;
            newTimesheet.Order = timesheetOrder;

            return newTimesheet;
        }

        private static TimesheetModuleItem subtractTimesheetsExport(int colId, bool isDecimalHours, TimesheetModuleItem timesheet1, TimesheetModuleItem timesheet2, TimesheetModuleItem timesheet3, string timesheetJustification, DateTime firstMonthDate,
            DateTime lastMonthDate, int timesheetOrder, int cantId, bool requestedForWeeklyTotals)
        {
            // inizializzazione del valore di ritorno del metodo
            var newTimesheet = new TimesheetModuleItem(isDecimalHours);

            // inserimento della data che indica il mese di elaborazione
            newTimesheet.StartDate = firstMonthDate;
            DateTime processingDate = firstMonthDate;
            DateTime startDate = firstMonthDate;
            DateTime endDate = lastMonthDate;

            //Se il collaboratore non ha piano orario non deve essere calcolato il delta;
            //Per fare ciò viene settato un booleano che verrà utilizzato durante il relat. calcolo
            var currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == colId);
            bool has_orario = currentCol.Tab_Orari_Tipo_Id.HasValue ? true : false;

            if (requestedForWeeklyTotals && startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
            {
                startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
            }

            if (requestedForWeeklyTotals && endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
            {
                endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                // la data di fine viene portata alle 23:59 così da recuperare anche le timbrature della giornata di fine
                endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);
            }

            // inizializzazione del piano vuoto in cui andare a compilare i totali per giornata
            var daysMinutes = RepoManager.Tab_OrariRepo.GetEmptyMinutesPlan(startDate, endDate, requestedForWeeklyTotals);
            double dayTotal, today1, today2, today3;

            if (startDate < firstMonthDate)
            {
                for (int i = firstMonthDate.AddTicks(-1 * startDate.Ticks).Day - 1; i > 0; i--)
                {
                    //Recupera le ore di piano e le ore lavorate del giorno in elaborazione
                    today1 = CommonService.FromHoursToMinutes((double)timesheet1["DayMinus" + i], isDecimalHours);// (Math.Truncate((double)timesheet1["DayMinus" + i]) * 60) + Math.Round(((double)timesheet1["DayMinus" + i] - Math.Truncate((double)timesheet1["DayMinus" + i])) * 100);
                    today2 = CommonService.FromHoursToMinutes((double)timesheet2["DayMinus" + i], isDecimalHours);// (Math.Truncate((double)timesheet2["DayMinus" + i]) * 60) + Math.Round(((double)timesheet2["DayMinus" + i] - Math.Truncate((double)timesheet2["DayMinus" + i])) * 100);
                    today3 = CommonService.FromHoursToMinutes((double)timesheet3["DayMinus" + i], isDecimalHours);// (Math.Truncate((double)timesheet2["DayMinus" + i]) * 60) + Math.Round(((double)timesheet2["DayMinus" + i] - Math.Truncate((double)timesheet2["DayMinus" + i])) * 100);
                    //Se la differenza è positiva, si fa la sottrazione, altrimenti si lascia 0; Se non ha orario prestabilito è 0
                    if (has_orario)
                    {
                        dayTotal = today1 - today2;
                        dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                    }
                    else if (today3 != 0)
                    {
                        dayTotal = today1 - today3;
                        dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                    }
                    else
                    {
                        dayTotal = 0;
                    }


                    if (daysMinutes.ContainsKey(firstMonthDate.AddDays(-1 * i).Date))
                    {
                        daysMinutes[firstMonthDate.AddDays(-1 * i).Date] = new Tuple<double, TimeSpan?, TimeSpan?>(dayTotal, null, null);
                    }
                    startDate = startDate.AddDays(1);
                }
            }

            // per ogni giorno del mese calcolo i totali e inserisco nel 'piano'
            for (int i = 1; i <= lastMonthDate.Day; i++)
            {
                //Recupera le ore di piano e le ore lavorate del giorno in elaborazione
                today1 = CommonService.FromHoursToMinutes((double)timesheet1["Day" + i.ToString("00")], isDecimalHours);// (Math.Truncate((double)timesheet1["Day" + i.ToString("00")]) * 60) + Math.Round(((double)timesheet1["Day" + i.ToString("00")] - Math.Truncate((double)timesheet1["Day" + i.ToString("00")])) * 100);
                today2 = CommonService.FromHoursToMinutes((double)timesheet2["Day" + i.ToString("00")], isDecimalHours);// (Math.Truncate((double)timesheet2["Day" + i.ToString("00")]) * 60) + Math.Round(((double)timesheet2["Day" + i.ToString("00")] - Math.Truncate((double)timesheet2["Day" + i.ToString("00")])) * 100);
                today3 = CommonService.FromHoursToMinutes((double)timesheet3["Day" + i.ToString("00")], isDecimalHours);// (Math.Truncate((double)timesheet2["Day" + i.ToString("00")]) * 60) + Math.Round(((double)timesheet2["Day" + i.ToString("00")] - Math.Truncate((double)timesheet2["Day" + i.ToString("00")])) * 100);
                                                                                                                        //Se la differenza è positiva, si fa la sottrazione, altrimenti si lascia 0; Se non ha orario prestabilito è 0
                if (has_orario)
                {
                    dayTotal = today1 - today2;
                    dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                }
                else if (today3 != 0)
                {
                    dayTotal = today1 - today3;
                    dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                }
                else
                {
                    dayTotal = 0;
                }


                if (daysMinutes.ContainsKey(processingDate))
                {
                    daysMinutes[processingDate] = new Tuple<double, TimeSpan?, TimeSpan?>(dayTotal, null, null);
                }

                processingDate = processingDate.AddDays(1);
            }

            if (endDate > lastMonthDate)
            {
                for (int i = 1, j = endDate.Day; i <= j; i++)
                {
                    //Recupera le ore di piano e le ore lavorate del giorno in elaborazione
                    today1 = CommonService.FromHoursToMinutes((double)timesheet1["DayPlus" + i], isDecimalHours); // (Math.Truncate((double)timesheet1["DayPlus" + i]) * 60) + Math.Round(((double)timesheet1["DayPlus" + i] - Math.Truncate((double)timesheet1["DayPlus" + i])) * 100);
                    today2 = CommonService.FromHoursToMinutes((double)timesheet2["DayPlus" + i], isDecimalHours); // (Math.Truncate((double)timesheet2["DayPlus" + i]) * 60) + Math.Round(((double)timesheet2["DayPlus" + i] - Math.Truncate((double)timesheet2["DayPlus" + i])) * 100);
                    today3 = CommonService.FromHoursToMinutes((double)timesheet3["DayPlus" + i], isDecimalHours); // (Math.Truncate((double)timesheet2["DayMinus" + i]) * 60) + Math.Round(((double)timesheet2["DayMinus" + i] - Math.Truncate((double)timesheet2["DayMinus" + i])) * 100);
                    //Se la differenza è positiva, si fa la sottrazione, altrimenti si lascia 0; Se non ha orario prestabilito è 0
                    if (has_orario)
                    {
                        dayTotal = today1 - today2;
                        dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                    }
                    else if (today3 != 0)
                    {
                        dayTotal = today1 - today3;
                        dayTotal = getDeltaMinutesWithAutStr((int)dayTotal, startDate, colId);
                    }
                    else
                    {
                        dayTotal = 0;
                    }


                    if (daysMinutes.ContainsKey(lastMonthDate.AddDays(i).Date))
                    {
                        daysMinutes[lastMonthDate.AddDays(i).Date] = new Tuple<double, TimeSpan?, TimeSpan?>(dayTotal, null, null);
                    }

                    endDate.AddDays(1);
                }
            }

            // inserimento del calcolo dei totali all'interno dell'oggetto timesheet
            newTimesheet.PopulateHoursWithDate(daysMinutes);
            newTimesheet.IsFromFreeTimeSheet = false;
            newTimesheet.FreeTimeSheetId = 0;
            newTimesheet.InsertColValues(colId);
            newTimesheet.InsertCantValues(cantId);
            newTimesheet.Justification = timesheetJustification;
            newTimesheet.Order = timesheetOrder;

            return newTimesheet;
        }



        /// <summary>
        /// Indica se le date di inzio e fine disponibilità del collaboratore sono valide nel periodo specificato.
        /// </summary>
        /// <param name="dataDisponibilitaInizioCol">La data di inizio disponibilità del collaboratore.</param>
        /// <param name="dataDisponibilitaFineCol">La data di fine disponibilità del collaboratore.</param>
        /// <param name="from">La data di inizio del periodo di ricerca.</param>
        /// <param name="to">La data di fine del periodo di ricerca</param>
        /// <returns><c>true</c> se le date permettono l'elaborazione del collaboratore; altrimenti <c>false</c></returns>
        private static bool HasColValidDates(DateTime? dataDisponibilitaInizioCol, DateTime? dataDisponibilitaFineCol, DateTime from, DateTime to)
        {
            // si designano come collaboratori validi solamente quelli che:
            //       NON hanno Data Fine   Disponibilità 
            //oppure     hanno Data Fine   Disponibilità che è >= Data Dal del periodo richiesto
            //ed     NON hanno Data Inizio Disponibilità
            //Oppure     hanno DATA Inizio Disponibilità che è <= Data Al  del periodo richiesto
            // SPIEGAZIONE LOGICA:
            bool isValidDates = (dataDisponibilitaFineCol == null || (dataDisponibilitaFineCol.Value >= @from)) && (dataDisponibilitaInizioCol == null || dataDisponibilitaInizioCol.Value <= to);

            // ritorno del valore del metodo
            return isValidDates;
        }

        /// <summary>
        /// Ricerca e restituisce tutte le registrazioni utilizzando in parametri di ricerca passati come parametro
        /// </summary>
        /// <param name="searchType">Il tipo di ricerca da effettuare.</param>
        /// <param name="baseRegVs">La lista di tutte le registrazioni per il collaboratore nel periodo specificato su cui effettuare la ricerca.</param>
        /// <returns>La lista di Reg_V risultato della ricerca effettuata</returns>
        private static List<Reg_V> GetRegVToProcess(RegSearchTypeForTimesheetEnum searchType, IEnumerable<Reg_V> baseRegVs)
        {
            // inizializzazione del valore di ritorno del metodo
            var searchedRegVs = new List<Reg_V>();

            // verifico la presenza della customizzazione riguardante la visualizzazione delle ore viaggio unite/separate rispetto alle ore lavorate
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowTimesheetTripHoursSameRowEnum);

            // sono aggiunge le condizioni specifiche di ricerca in base al tipo di ricerca richiesta
            switch (searchType)
            {
                case RegSearchTypeForTimesheetEnum.WorkedRegs: // ore lavorate (a cui sono aggiunte le ore solo durata)

                    // se è richiesto dalla customizzazione e non sono state esplicitamente rifiutate da un'opzione, le ore lavorate, oltre allo standard, prevedono anche i viaggi
                    // si procede al calcolo delle ore viaggio solamente se tra le opzioni non è esplicitamente richiesto di toglierlo
                    if (customizationVersion == (int)ShowTimesheetTripHoursSameRowEnum.TwoRows ||
                        TimesheetOptions.Any(tsopt => tsopt == NoTripHoursOptions))
                    {
                        // le ore lavorate sono ore di tipo Ora E/U, abbinate, dove il cantiere non è ONL e la motivazione non è valorizzata
                        searchedRegVs = baseRegVs.ToList().Where(regv =>
                        {
                            //
                            var currCant = regv.Cant_Id.HasValue ? RepoManager.CantRepo.DbSet.FirstOrDefault(cant => cant.Cant_Id == regv.Cant_Id) : null;

                            string tipoCantiere = currCant != null ? currCant.Tipo_Cantiere_Can : String.Empty;

                            return (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration) && regv.Registrazione_Stato_Reg != (int)RegStateEnum.None && !regv.Motivazione_Reg_Id.HasValue && tipoCantiere != "ONL";
                        }).ToList();
                    }
                    else
                    {
                        // le ore lavorate sono ore di tipo Ora E/U, abbinate, dove il cantiere non è ONL e la motivazione non è valorizzata, più i viaggi
                        searchedRegVs = baseRegVs.Where(regv =>
                        {
                            var currCant = regv.Cant_Id.HasValue ? RepoManager.CantRepo.DbSet.FirstOrDefault(cant => cant.Cant_Id == regv.Cant_Id) : null;
                            string tipoCantiere = currCant != null ? currCant.Tipo_Cantiere_Can : String.Empty;
                            return ((regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration) && regv.Registrazione_Stato_Reg != (int)RegStateEnum.None && !regv.Motivazione_Reg_Id.HasValue && tipoCantiere != "ONL") || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip;
                        }).ToList();
                    }

                    break;
                case RegSearchTypeForTimesheetEnum.TripRegs: // ore viaggio
                    // se è richiesto dalla customizzazione che le ore viaggio siano accorpate alle ore lavorate allora si ritorna in questo caso
                    // una lista vuota
                    if (customizationVersion == (int)ShowTimesheetTripHoursSameRowEnum.TwoRows)
                    {
                        // le ore viaggio sono le ore di tipo viaggio
                        searchedRegVs = baseRegVs.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip).ToList();
                    }

                    break;
                case RegSearchTypeForTimesheetEnum.JustificationRegs: // ore con motivazione
                    // le ore con motivazione sono ore di tipo Ora E/U, abbinate, dove il campo di motivazione è valorizzato
                    searchedRegVs = baseRegVs.Where(regv => (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration) && regv.Registrazione_Stato_Reg != (int)RegStateEnum.None && regv.Motivazione_Reg_Id.HasValue).ToList();
                    break;
                case RegSearchTypeForTimesheetEnum.CorrectionRegs: // ore rettifiche
                    // le ore rettifiche sono le registrazioni di tipo rettifica
                    searchedRegVs = baseRegVs.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimesheet).ToList();
                    break;
                case RegSearchTypeForTimesheetEnum.CorrectionRegsManu: // ore rettifiche manuali
                    // le ore rettifiche sono le registrazioni di tipo rettifica
                    searchedRegVs = baseRegVs.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual).ToList();
                    break;
                case RegSearchTypeForTimesheetEnum.DurationRoundingRegs: // arrotondamenti per durata
                    // gli arrotondamenti per durata sono le registrazioni di tipo arrotondamenti per durata
                    searchedRegVs = baseRegVs.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.ArrotDur).ToList();
                    break;
                case RegSearchTypeForTimesheetEnum.OnlRegs:
                    // le ore non lavorate sono ore di tipo Ora E/U o durata, abbinate, dove il cantiere è ONL e la motivazione non è valorizzata
                    searchedRegVs = baseRegVs.Where(regv =>
                    {
                        var currCant = regv.Cant_Id.HasValue ? RepoManager.CantRepo.DbSet.FirstOrDefault(cant => cant.Cant_Id == regv.Cant_Id) : null;
                        string tipoCantiere = currCant != null ? currCant.Tipo_Cantiere_Can : String.Empty;
                        return (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration) && regv.Registrazione_Stato_Reg != (int)RegStateEnum.None && !regv.Motivazione_Reg_Id.HasValue && tipoCantiere == "ONL";
                    }).ToList();
                    break;
            }

            // ritorno del valore del metodo
            return searchedRegVs;
        }

        /// <summary>
        /// Ricerca e ritorna per il periodo specificato e il collaboratore indicato tutte le registrazioni presenti.
        /// </summary>
        /// <param name="colToSearch">Il collaboratore di cui cercare le registrazioni</param>
        /// <param name="startPeriod">La data di inzio della ricerca.</param>
        /// <param name="endPeriod">La data di fine della ricerca</param>
        /// <param name="requestedForWeeklyTotals">Indica se il dato è richiesto per la costruzione di un piano che richiede totali settimanali.</param>
        /// <returns>L'elenco di Reg_V trovate nel database che rispondo ai criteri di ricerca.</returns>
        private static IQueryable<Reg_V> GetPeriodColRegVs(Col colToSearch, DateTime startPeriod, DateTime endPeriod, bool requestedForWeeklyTotals)
        {
            /* se è richiesto di costruire i dati per un orario con totali settimanali si verifica se si sta processando
            * l'inizio e la fine del mese; in questo caso, se necessario si aggiornano le date per comprendere l'inizio e la fine della settimana
            * del mese precedente e successivo */

            // se è richiesto il piano per la gestione di orari settimanali, la data di inizio è l'inizio del mese e la data di inizio non è un lunedì
            // allora si modifica la data di inizio periodo l'ultimo lunedì del mese precedente
            if (requestedForWeeklyTotals && startPeriod.Date == CommonService.GetFirstMonthDay(startPeriod) && startPeriod.DayOfWeek != DayOfWeek.Monday)
                startPeriod = CommonService.GetLastDayOfWeekInMonth(startPeriod.AddMonths(-1), DayOfWeek.Monday);

            // se è richiesto il piano per la gestione degli orari settimanali, la data di fine è la fine del mese e non si tratta di una domenica
            // allora si recupera la data di fine periodo la prima domenica del mese successivo
            if (requestedForWeeklyTotals && endPeriod.Date == CommonService.GetLastMonthDay(endPeriod) && endPeriod.DayOfWeek != DayOfWeek.Sunday)
                endPeriod = CommonService.GetFirstDayOfWeekInMonth(endPeriod.AddMonths(1), DayOfWeek.Sunday);

            // si ricalcola il periodo di ricerca delle reg_v a partire dalle date di disponibilità del collaboratore
            Tuple<DateTime, DateTime> validSearchDates = RepoManager.Tab_OrariRepo.FilterPeriodWithColDispDates(startPeriod, endPeriod, colToSearch.Data_Disponibilita_Inizio_Col, colToSearch.Data_Disponibilita_Fine_Col);
            DateTime newFirstMonthDate = new DateTime();
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportStr) == 1)
            {
                newFirstMonthDate = startPeriod;
            }
            else {
                newFirstMonthDate = validSearchDates.Item1;
            }
            // si aggiunge un giorno nella verifica del fine mese in quanto alla mezzanotte di fine mese mancano ancora 24 ore di timbrature:
            // in pratica se cerco tutte le timbrature con data minore di 30/06 00:00 mi perdo tutte le timbrature dal 30/06 00:00 al 30/06 23:29
            DateTime newLastMonthDate = validSearchDates.Item2;

            // ritorno delle registrazioni calcolate con i parametri spassati come parametro che siano associate, non attività
            return RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Col_Id == colToSearch.Col_Id
                && (regv.Data_Reg >= newFirstMonthDate && regv.Data_Reg < newLastMonthDate)
                && regv.Registrazione_Stato_Reg != (int)RegStateEnum.None && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att, true);
        }

        /// <summary>
        /// Ricerca e ritorna per il periodo specificato e il cantiere indicato tutte le registrazioni presenti.
        /// </summary>
        /// <param name="cantTo">Il cantiere di cui cercare le registrazioni</param>
        /// <param name="startPeriod">La data di inzio della ricerca.</param>
        /// <param name="endPeriod">La data di fine della ricerca</param>
        /// <param name="requestedForWeeklyTotals">Indica se il dato è richiesto per la costruzione di un piano che richiede totali settimanali.</param>
        /// <returns>La lista di Reg_V trovate nel database che rispondo ai criteri di ricerca.</returns>
        private static IQueryable<Reg_V> GetPeriodCantRegVs(Cant cantTo, DateTime startPeriod, DateTime endPeriod, bool requestedForWeeklyTotals)
        {
            /* se è richiesto di costruire i dati per un orario con totali settimanali si verifica se si sta processando
            * l'inizio e la fine del mese; in questo caso, se necessario si aggiornano le date per comprendere l'inizio e la fine della settimana
            * del mese precedente e successivo */

            // se è richiesto il piano per la gestione di orari settimanali, la data di inizio è l'inizio del mese e la data di inizio non è un lunedì
            // allora si modifica la data di inizio periodo l'ultimo lunedì del mese precedente
            if (requestedForWeeklyTotals && startPeriod.Date == CommonService.GetFirstMonthDay(startPeriod) && startPeriod.DayOfWeek != DayOfWeek.Monday)
                startPeriod = CommonService.GetLastDayOfWeekInMonth(startPeriod.AddMonths(-1), DayOfWeek.Monday);

            // se è richiesto il piano per la gestione degli orari settimanali, la data di fine è la fine del mese e non si tratta di una domenica
            // allora si recupera la data di fine periodo la prima domenica del mese successivo
            if (requestedForWeeklyTotals && endPeriod.Date == CommonService.GetLastMonthDay(endPeriod) && endPeriod.DayOfWeek != DayOfWeek.Sunday)
                endPeriod = CommonService.GetFirstDayOfWeekInMonth(endPeriod.AddMonths(1), DayOfWeek.Sunday);

            // ritorno delle registrazioni calcolate con i parametri spassati come parametro
            return RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Cant_Id == cantTo.Cant_Id && regv.Data_Reg >= startPeriod && regv.Data_Reg < endPeriod, true);
        }

        /// <summary>
        /// Determina se l'autorizzazione degli straordinari risulta configurata e abilitata.
        /// </summary>
        /// <returns><c>true</c> il caso l'autorizzazione degli straordinari risulta correttamente configurata e abilitata.</returns>
        private static bool IsAuthStrConfigured()
        {
            bool isAutStrConfigured = false;
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Aut_Str && RepoManager.ParamRepo.ParametersRow.Aut_Str_Tipo.HasValue)
                isAutStrConfigured = RepoManager.ParamRepo.ParametersRow.Aut_Str_Tipo.Value == 1;
            return isAutStrConfigured;
        }

        public static Dictionary<string, List<TimesheetModuleItem>> GenerateCartellino(DateTime selectedDate, Col col, bool isByOtherEntity, bool showWeeklyTotal, bool calculateOrdStrTimesheet, bool isDecimalHours = false,  bool calculateWorkedHours = true, bool calculateJustifications = true, bool calculateTrips = true, bool calculateDelta = true, bool devidePlanByDayNight = false, bool insertCorrectionRow = false, bool showPiano = true)
        {
            // calcolo, a partire dalla data passata come parametro, l'inzio e la fine del mese in elaborazione
            DateTime minDate = CommonService.GetFirstMonthDay(selectedDate);
            DateTime monthLastDate = CommonService.GetLastMonthDay(minDate);
            DateTime maxDate = new DateTime(monthLastDate.Year, monthLastDate.Month, monthLastDate.Day, 23, 59, 59);
            List<Reg_V> workedRegVs = new List<Reg_V>();
            List<Reg_V> test = new List<Reg_V>();
            string just = "";



            //Dictionary da ritornare
            Dictionary<string, List<TimesheetModuleItem>> cartellini = new Dictionary<string, List<TimesheetModuleItem>>();

            #region CARTELLINI PER MOTIVAZIONE
            //Lista dei cartellini delle motivazioni
            List<TimesheetModuleItem> justificationCartellini = new List<TimesheetModuleItem>();

            #region ORE PIANO
            bool isFromFreeTimesheet;
            int freeTimesheetId;
            //Recupera il piano ore del collaboratore
            Dictionary<int, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> planMinutes = RepoManager.Tab_OrariRepo.GetPlanMinutes(col.Col_Id,
                                                                    minDate, maxDate, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col,
                                                                    out isFromFreeTimesheet, out freeTimesheetId, isByOtherEntity, "Col", showWeeklyTotal);
            var colPlan = GenerateNewPlanTimesheet(isDecimalHours, planMinutes.First().Value, isFromFreeTimesheet, freeTimesheetId, col.Col_Id, minDate, planMinutes.First().Key);


            if (!isByOtherEntity && showPiano)
            {
                //Trasforma il piano recuperato in cartellino e lo aggiunge alla lista

                if (devidePlanByDayNight)
                {
                    Dictionary<string, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> dayNightPlan = RepoManager.Tab_OrariRepo.GetDevidedPlanMinutes(col.Col_Id, planMinutes.First().Value.First().Key, planMinutes.First().Value.Last().Key, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col, out isFromFreeTimesheet, out freeTimesheetId);
                    justificationCartellini.Add(GenerateNewPlanTimesheet(isDecimalHours, dayNightPlan[DayTimesheetKey], isFromFreeTimesheet, freeTimesheetId, col.Col_Id, minDate, 0, PlanTypeEnum.Day));
                    justificationCartellini.Add(GenerateNewPlanTimesheet(isDecimalHours, dayNightPlan[NightTimesheetKey], isFromFreeTimesheet, freeTimesheetId, col.Col_Id, minDate, 0, PlanTypeEnum.Night));
                }
                else
                {
                    justificationCartellini.Add(colPlan);
                }
            }
            #endregion

            //Estrapolo ora inizio e fine del notturno, se presenti
            TimeSpan? nocturnStartHour = null;
            TimeSpan? nocturnEndHour = null;
            TimesheetModuleItem firstPlan = justificationCartellini.FirstOrDefault();
            if (justificationCartellini.FirstOrDefault() != default(TimesheetModuleItem))
            {
                nocturnStartHour = justificationCartellini.FirstOrDefault().NocturnsStartHour;
                nocturnEndHour = justificationCartellini.FirstOrDefault().NocturnEndHour;
            }

            #region ORE LAVORATE
            //Recupera e ordina per data tutte le reg del collaboratore
            IQueryable<Reg_V> baseColRegVs = GetPeriodColRegVs(col, minDate, maxDate, showWeeklyTotal).OrderBy(r => r.Data_Reg);
            workedRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.WorkedRegs, baseColRegVs);
            var cantIdList1 = workedRegVs.Select(regv => regv.Cant_Id).Distinct().ToList();

            // inizializzazione dell'ordine di visualizzazione
            int tsOrder = 0;

            if (calculateWorkedHours)
            {
                workedRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.WorkedRegs, baseColRegVs);
                test = baseColRegVs.ToList();
                string workedJust = BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_LAVORATE);
                Tab_Decod td = RepoManager.Tab_DecodRepo.FirstOrDefault(t => t.Nome_Tab == "MOTIVAZIONI" && t.Decodifica_Tab == workedJust);
                just = td != default(Tab_Decod) ? td.Chiave_Tab : workedJust;
                if (isByOtherEntity)
                {
                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportStr) == 1)
                    {
                        justificationCartellini.AddRange(GenerateRegVTimesheetsByOtherEntityExportStr(baseColRegVs.ToList(), col, isDecimalHours, just, minDate, maxDate, ++tsOrder, showWeeklyTotal, planMinutes));
                    }
                    else 
                    {
                        justificationCartellini.AddRange(GenerateRegVTimesheetsByOtherEntity(workedRegVs, col, isDecimalHours, just, minDate, maxDate, ++tsOrder, showWeeklyTotal));
                    }
                   
                }
                else
                {
                    justificationCartellini.Add(GenerateNewRegTimesheetTotal(col.Col_Id, isDecimalHours, workedRegVs, just, minDate, maxDate, ++tsOrder, 0, showWeeklyTotal));
                }
            }
            #endregion
            
            #region MOTIVAZIONI

            if (calculateJustifications)
            {
                //Recupera le registrazioni di tipo motivazione
                var justificationRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.JustificationRegs, baseColRegVs);

                //Se sono presenti motivazioni, genera i relativi cartellini
                if (justificationRegVs.Any())
                {
                    //Cicla per ogni singola motivazione e ne crea il cartellino
                    foreach (string justification in justificationRegVs.Select(regv => regv.Motivazione_Reg_Cod).Distinct().ToList())
                    {
                        
                        // con il codice della motivazione si ricercano i dati nell'apposita tabella per il recupero della descrizione se richiesto da apposita personalizzazione
                        string displayJustfification = justification;
                        ShowDescriptionJustTimesheet showDescriptionJust = (ShowDescriptionJustTimesheet)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowDescriptionJustTimesheet);
                        if (showDescriptionJust == ShowDescriptionJustTimesheet.Show) // se la personalizzazione risulta attiva
                        {
                            Tab_Decod justConf = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "MOTIVAZIONI" && td.Chiave_Tab == justification);
                            if (justConf != default(Tab_Decod))
                                displayJustfification = justConf.Decodifica_Tab;
                        }
                        
                        if (isByOtherEntity)
                        {
                            // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                            justificationCartellini.AddRange(GenerateRegVTimesheetsByOtherEntity(justificationRegVs.Where(regv => regv.Motivazione_Reg_Cod == justification).ToList(),
                                col,
                                isDecimalHours,
                                justification,
                                minDate,
                                maxDate,
                                ++tsOrder,
                                showWeeklyTotal,
                                usaFisiche: false));
                        }
                        else
                        {
                            justificationCartellini.Add(GenerateNewRegTimesheet(col.Col_Id,
                                isDecimalHours,
                                justificationRegVs.Where(regv => regv.Motivazione_Reg_Cod == justification).ToList(),
                                displayJustfification,
                                minDate,
                                maxDate,
                                ++tsOrder,
                                0,
                                requestedForWeeklyTotals: showWeeklyTotal,
                                usaFisiche: false));
                        }
                         
                    }
                }
            }
            #endregion

            #region TEMPO CORRETTO
            TimesheetModuleItem colRigth = null;
            if (calculateWorkedHours && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportRigthTime) == 1)
            {
                //Recupera le registrazioni di tipo motivazione
                var justificationRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.JustificationRegs, baseColRegVs);
                Dictionary<int, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> OreCorrette = null;
                //Se sono presenti motivazioni, genera i relativi cartellini
                if (justificationRegVs.Any())
                {
                    //inizializzo le varibili: i cantieri, l'inizio del mese, le liste 
                    var cant = RepoManager.CantRepo.GetAll();
                    DateTime data = minDate;
                    int CantId = 0;
                    int ColId = 0;
                    List <TimeSpan> list = new List<TimeSpan>();
                    List <DateTime> giorni = new List<DateTime>();
                    //ciclo per i giorni del mese
                    foreach (var date in planMinutes[0])
                    {
                        List<int> cantieri = new List<int>();
                        float totale = 0;
                        //ciclo in base alle registrazioni del collaboratore ed in base al giorno corrente
                        foreach (Reg_V registration in justificationRegVs.Where(regv => regv.Col_Id == colPlan.ColId && DateTime.Compare(regv.Data_Ora_Fig_EDate.Value, date.Key) == 0))
                        {
                            if (ColId == 0)
                            {
                                ColId = registration.Col_Id.Value;
                            }
                            int i = 1;
                            //creo ii totale per cantiere
                            foreach (var cants in cant.Where(cantv => cantv.Cant_Id == registration.Cant_Id))
                            {
                                bool single = true;
                                if (cantieri.Count == 0) {
                                    cantieri.Add(cants.Cant_Id);
                                }
                                else {
                                    foreach (int id in cantieri) {
                                        if (cants.Cant_Id == id) {
                                            single = false;
                                        }
                                    } 
                                    cantieri.Add(cants.Cant_Id);
                                }

                                if (single) {
                                    if (CantId == 0)
                                    {
                                        CantId = cants.Cant_Id;
                                    }
                                    //creo la lista contente quante persone hanno lavorato nella stessa stanza lo stesso giorno
                                    List<Reg> reg = new List<Reg>();
                                    foreach (Reg Try in RepoManager.RegRepo.GetAll().Where(regv => regv.Col_Id != registration.Col_Id && regv.Cant_Id == cants.Cant_Id && regv.Registrazione_Data_Ora_Fis_Reg.DayOfYear.Equals(date.Key.DayOfYear)))
                                    {
                                        reg.Add(Try);
                                    }
                                    int collab = 0;
                                    //se altri hanno lavorato nella stessa stanza divido il tempo previsto
                                    foreach (Reg ciclo in reg)
                                    {
                                        if (collab == 0)
                                        {
                                            i++;
                                            collab = ciclo.Col_Id.Value;
                                        }
                                        else if (collab != 0)
                                        {
                                            if (collab != ciclo.Col_Id.Value)
                                            {
                                                collab = ciclo.Col_Id.Value;
                                                i++;
                                            }
                                        }
                                    }
                                    //se la durata della registrazione è minore di 5 non inserisco il tempo stimato nel tempo corretto
                                    if (registration.Durata_Fig > 5) { 
                                        //in base alla motivazione prendo una quantità di tempo diversa
                                        if (registration.Motivazione_Reg_Cod == "PART")
                                        {
                                            if (cants.Turno1_Can.Value != null)
                                            {
                                                int conversione = (int)cants.Turno1_Can.Value.TotalMinutes;
                                                float ris = conversione / i;
                                                if (conversione % i == 1) {
                                                    ris += 1;
                                                }
                                                //TimeSpan? converted = TimeSpan.FromMinutes(ris);
                                                if (totale == 0)
                                                {
                                                    totale = ris;
                                                }
                                                else
                                                {
                                                    totale += ris;
                                                }
                                            }
                                        }
                                        else if (registration.Motivazione_Reg_Cod == "FERM")
                                        {
                                            int conversione = (int)cants.Turno2_Can.Value.TotalMinutes;
                                            float ris = conversione / i;
                                            if (conversione % i == 1)
                                            {
                                                ris += 1;
                                            }
                                            if (cants.Turno1_Can.Value != null)
                                            {
                                                if (totale == 0)
                                                {
                                                    totale = ris;
                                                }
                                                else
                                                {
                                                    totale += ris;
                                                }
                                            }
                                        }
                                    }
                                    
                                }
                            } 
                        }
                        //creo le liste con i dati e le date
                        if (totale != 0)
                        {
                            TimeSpan? converted = TimeSpan.FromMinutes(totale);
                            list.Add(converted.Value);
                        }
                        else {
                            list.Add(TimeSpan.MinValue);
                        }
                        giorni.Add(data);
                        data = data.AddDays(1);
                    }
                    //OreCorrette.Add(0, planMinutes);
                    colRigth = GenerateNewPartPermTimesheet(isDecimalHours, list,giorni, true, 0, ColId, minDate, CantId);
                    justificationCartellini.Add(colRigth);
                }
            }
            #endregion

            #region VIAGGI
            //Se sono abilitati i viaggi, vengono estrapolati e aggiunti al cartellino
            if (calculateTrips && RepoManager.ParamRepo.ParametersRow.Abilita_Viaggi)
            {
                var trips = GetRegVToProcess(RegSearchTypeForTimesheetEnum.TripRegs, baseColRegVs);
                if (trips.Any())
                {
                    if (!isByOtherEntity)
                    {
                        justificationCartellini.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, trips, BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal, usaFisiche: false));
                    }
                    else
                    {
                        justificationCartellini.AddRange(GenerateRegVTimesheetsByOtherEntity(trips, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI), minDate, maxDate, ++tsOrder, showWeeklyTotal, usaFisiche: false));
                    }
                }
            }
            #endregion

            #region ARROTONDAMENTI per DURATA
            List<Reg_V> rounding = GetRegVToProcess(RegSearchTypeForTimesheetEnum.DurationRoundingRegs, baseColRegVs);
            if (rounding.Any())
            {
                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.RimozionePausaHotel) == 0)
                {
                    // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                    if (isByOtherEntity)
                    {
                        justificationCartellini.AddRange(GenerateRegVTimesheetsByOtherEntity(rounding, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT), minDate, maxDate, ++tsOrder, showWeeklyTotal, usaFisiche: false));
                    }
                    else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                    {
                        justificationCartellini.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, rounding, BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT_DURATA), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal, usaFisiche: false));
                    }
                }
                else {
                    // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                    if (isByOtherEntity)
                    {
                        justificationCartellini.AddRange(GenerateRegVTimesheetsByOtherEntity(rounding, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_PAUSA_PRANZO), minDate, maxDate, ++tsOrder, showWeeklyTotal, usaFisiche: false));
                    }
                    else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                    {
                        justificationCartellini.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, rounding, BusinessService.GetLocalizedString(PowerWebResources.LBL_PAUSA_PRANZO), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal, usaFisiche: false));
                    }
                }
                // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
            }
            #endregion

            #region RETTIFICHE
            //Se sono abilitate le rettifiche, vengono estrapolate e aggiunte al cartellino
            if (RepoManager.ParamRepo.ParametersRow.Cartellino_Usa_Rettifiche_Auto || RepoManager.ParamRepo.ParametersRow.Cartellino_Usa_Rettifiche_Manuali)
            {
                List<Reg_V> rettifiche_Auto = GetRegVToProcess(RegSearchTypeForTimesheetEnum.CorrectionRegs, baseColRegVs);
                List<Reg_V> rettifiche_Manu = GetRegVToProcess(RegSearchTypeForTimesheetEnum.CorrectionRegsManu, baseColRegVs);

                // se è prevista la divisione per cantiere, allora si procede a separare per questo dato ulteriormente le ore, altrimenti tutto finisce in unico calderone
                if (isByOtherEntity)
                {
                    List<int> cantList = justificationCartellini.Select(tmi => tmi.CantId).Where(c => c != 0).Distinct().ToList();
                    if (RepoManager.ParamRepo.ParametersRow.Cartellino_Usa_Rettifiche_Auto)
                    {
                        justificationCartellini.AddRange(GenerateRegVTimesheetsByOtherEntity(rettifiche_Auto, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE), minDate, maxDate, ++tsOrder, showWeeklyTotal, usaFisiche: false, cantList: cantList));
                    }
                    if (RepoManager.ParamRepo.ParametersRow.Cartellino_Usa_Rettifiche_Manuali)
                    {
                        justificationCartellini.AddRange(GenerateRegVTimesheetsByOtherEntity(rettifiche_Manu, col, isDecimalHours, BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE_MANUALI), minDate, maxDate, ++tsOrder, showWeeklyTotal, usaFisiche: false, cantList: cantList));
                    }

                }
                else // se è richiesta la divisione per cantiere allora si provvede a creare un timesheet per ogni cantiere previsto
                {
                    if (RepoManager.ParamRepo.ParametersRow.Cartellino_Usa_Rettifiche_Auto)
                    {
                        justificationCartellini.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, rettifiche_Auto, BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal, usaFisiche: false));
                    }

                    if (RepoManager.ParamRepo.ParametersRow.Cartellino_Usa_Rettifiche_Manuali)
                    {
                        justificationCartellini.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, rettifiche_Manu, BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE_MANUALI), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal, usaFisiche: false));
                    }

                }
            }
            #endregion

            #region TOTALE
            //Filtra i cartellini su cui calcolare il totale
            var cartelliniToTotalize = justificationCartellini.Where(ts => ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN) && ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_DAY) && ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_NIGHT) && ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA)).ToList();

            int customizationVersionJustification = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.JustificationHourIsWorkedHoursEnum);
            if (customizationVersionJustification == (int)JustificationHourIsWorkedHoursEnum.DoNotUse && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PausaPranzoIsWorkedHoursEnum) == 0)
            {
                cartelliniToTotalize = cartelliniToTotalize.Where(c => c.Justification == "OL" || c.Justification == "Ore Viaggi").ToList();
            }
            else if (customizationVersionJustification == (int)JustificationHourIsWorkedHoursEnum.DoNotUse && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PausaPranzoIsWorkedHoursEnum) == 1)
            {
                cartelliniToTotalize = cartelliniToTotalize.Where(c => c.Justification == "OL" || c.Justification == "Ore Viaggi" || c.Justification == "Pausa").ToList();
            }
            else if(customizationVersionJustification == 1 && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TripHourIsWorkedHoursEnum) == 0) {
                cartelliniToTotalize = cartelliniToTotalize.Where(c => c.Justification != "Ore Viaggi").ToList();
            }else if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PausaPranzoIsWorkedHoursEnum) == 0) {
                cartelliniToTotalize = cartelliniToTotalize.Where(c => c.Justification != "Pausa").ToList();
            }else if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ReperibilitaTotale) == 0) {
                cartelliniToTotalize = cartelliniToTotalize.Where(c => c.Justification != "REP").ToList();
            }

            TimesheetModuleItem colTotal = null;

            if (colTotal == null) {
                colTotal = GenerateNewTotalTimesheet(col.Col_Id, isDecimalHours, cartelliniToTotalize, BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal);
            }
            if (!isByOtherEntity)
            {
                justificationCartellini.Add(colTotal);
            }
            #endregion

            #region DELTA

            if (calculateDelta)
            {
                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportRigthTime) == 1 && colRigth != null)
                {
                    TimesheetModuleItem colDelta = subtractTimesheets(col.Col_Id, isDecimalHours, colTotal, colPlan, colRigth, BusinessService.GetLocalizedString(PowerWebResources.LBL_ECCEDENZA), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal);
                    colDelta.setCurrentMonthMonteMinuti();
                    if (!isByOtherEntity)
                    {
                        justificationCartellini.Add(colDelta);
                    }
                }
                else {
                    colRigth = colPlan;
                    TimesheetModuleItem colDelta = subtractTimesheets(col.Col_Id, isDecimalHours, colTotal, colPlan,colRigth, BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal);
                    colDelta.setCurrentMonthMonteMinuti();
                    if (!isByOtherEntity)
                    {
                        justificationCartellini.Add(colDelta);
                    }
                }
                
            }
            #endregion

            cartellini.Add("justification", justificationCartellini);
            #endregion


            List<TimesheetModuleItem> straordinariCartellini = new List<TimesheetModuleItem>();
            List<TimesheetModuleItem> listaOrari = new List<TimesheetModuleItem>();

            #region CARTELLINI PER ORDINARIE/STRAORDINARIE
            if (calculateOrdStrTimesheet)
            {


                #region CARTELLINI DA ELABORARE
                //se la personalizzazione di pulitait è attiva vado a calcolare gli straordinari in base ai vari cantieri
                if(RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportStr) == 1 && colPlan.CantDesc != null) {
                    List<int> cantList = null;
                    var returnList = new List<TimesheetModuleItem>();

                    // recupero tutti gli id cantiere presenti all'interno della lista passata come parametro
                    var cantIdList = baseColRegVs.Select(regv => regv.Cant_Id).Distinct().ToList();
                    if (just == "Rettifiche Manu." || just == "Rettifiche Auto.")
                    {
                        cantIdList = cantList.Cast<int?>().ToList();
                    }
                    foreach (var piano in planMinutes)
                    {
                        if (!cantIdList.Contains(piano.Key))
                        {
                            cantIdList.Add(piano.Key);
                        }
                    }
                    // per ogni id cantiere presente nella lista
                    foreach (var listCantId in cantIdList)
                    {
                        // calcolo l'id cantiere facendo si di convertire in 0 i valori null
                        var currentCantId = listCantId ?? 0;
                        bool error = false;
                        //controllo se il cantiere ha un orario assegnato, in caso non lo abbia vado ad identificare tutte le ore fatte nel cantiere come straordinarie
                        try {
                            var temp = planMinutes.First(m => m.Key == currentCantId);
                        }
                        catch (Exception e) {
                            error = true;
                        }
                        if (error)
                        {
                            TimesheetModuleItem Total = GenerateNewTotalTimesheet(col.Col_Id, isDecimalHours, cartelliniToTotalize.Where(c => c.CantId == currentCantId).ToList(), BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal);
                            // aggiungo il timesheet specifico del cantiere alla list di ritorno
                            //returnList.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, regVsToSplit.Where(regv => regv.C ant_Id == listCantId).ToList(), timesheetJustification, firstMonthDate, lastMonthDate, timesheetOrder, currentCantId, requestedForWeeklyTotals, usaFisiche: usaFisiche));
                            returnList.Add(GenerateNewEditableTimesheetExportStr(col.Col_Id, currentCantId, isDecimalHours, minDate, maxDate, Total));
                        }
                        else 
                        {
                            TimesheetModuleItem Plans = GenerateNewPlanTimesheet(isDecimalHours, planMinutes.First(m => m.Key == currentCantId).Value, isFromFreeTimesheet, freeTimesheetId, col.Col_Id, minDate, planMinutes.First(m => m.Key == currentCantId).Key);
                            TimesheetModuleItem Totals = GenerateNewTotalTimesheet(col.Col_Id, isDecimalHours, cartelliniToTotalize.Where(c => c.CantId == currentCantId).ToList(), BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal);
                            // aggiungo il timesheet specifico del cantiere alla list di ritorno
                            //returnList.Add(GenerateNewRegTimesheet(col.Col_Id, isDecimalHours, regVsToSplit.Where(regv => regv.C ant_Id == listCantId).ToList(), timesheetJustification, firstMonthDate, lastMonthDate, timesheetOrder, currentCantId, requestedForWeeklyTotals, usaFisiche: usaFisiche));
                            returnList.Add(GenerateNewEditableTimesheetExport(col.Col_Id, currentCantId, isDecimalHours, minDate, maxDate, Plans, Totals));
                        }
                        
                    }
                    //List<TimesheetModuleItem> generatedStraordinariCartellini = GenerateNewEditableTimesheet(col.Col_Id, isDecimalHours, minDate, maxDate, colPlan);
                    // Salva immediatamente i cartellini a db
                    straordinariCartellini.AddRange(returnList);
                }
                else if (colPlan != null)
                {
                    List<TimesheetModuleItem> generatedStraordinariCartellini = new List<TimesheetModuleItem>();
                    //se è attiva la customization di non contare gli straordinari notturni (inizialmente ideata per Cl) utilizzo un metodo di calcolo degli straordinari apposito
                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoNocturneStr) == 1)
                    {
                        generatedStraordinariCartellini = GenerateNewEditableTimesheetNoStr(col.Col_Id, isDecimalHours, minDate, maxDate, colPlan);
                    }
                    else {
                        generatedStraordinariCartellini = GenerateNewEditableTimesheet(col.Col_Id, isDecimalHours, minDate, maxDate, colPlan);
                    }
                    // Salva immediatamente i cartellini a db
                    straordinariCartellini.AddRange(generatedStraordinariCartellini);
                }

                #endregion

                cartellini.Add("straordinari", straordinariCartellini);
            }
            #endregion

            #region CARTELLINO PER EXPORT DIVISO PER CANTIERE E PER GIORNO
                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportStr) == 1 && colPlan.CantDesc != null)
                {                       
                    List<TimesheetModuleItem> deltaCartellini = new List<TimesheetModuleItem>();
                    deltaCartellini.AddRange(GenerateDeltaRegVTimesheetsByOtherEntity(workedRegVs, col, isDecimalHours, just, minDate, maxDate, ++tsOrder, showWeeklyTotal, planMinutes, colRigth, freeTimesheetId, isFromFreeTimesheet, cartelliniToTotalize));
                    cartellini.Add("delta", deltaCartellini);

                    //cartelliniToTotalize = justificationCartellini.Where(ts => ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN) && ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_DAY) && ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_NIGHT) && ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA)).ToList();
                    //if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportStr) == 1)
                    //{
                    //    cartelliniToTotalize = cartelliniToTotalize.Where(c => c.Justification == "OL" || c.Justification == "Ore Viaggi").ToList();
                    //
                    //}
                    var cartelliniToInitializeExport = new List<TimesheetModuleItem>();
                    workedRegVs = GetRegVToProcess(RegSearchTypeForTimesheetEnum.WorkedRegs, baseColRegVs);
                    string workedJust = BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_LAVORATE);
                    Tab_Decod td = RepoManager.Tab_DecodRepo.FirstOrDefault(t => t.Nome_Tab == "MOTIVAZIONI" && t.Decodifica_Tab == workedJust);
                    just = td != default(Tab_Decod) ? td.Chiave_Tab : workedJust;
                    if (isByOtherEntity)
                    {
                        cartelliniToInitializeExport.AddRange(GenerateRegVTimesheetsByOtherEntity(workedRegVs, col, isDecimalHours, just, minDate, maxDate, ++tsOrder, showWeeklyTotal));
                    }
                    cartelliniToInitializeExport = cartelliniToInitializeExport.Where(c => c.Justification == "OL" || c.Justification == "Ore Viaggi").ToList();
                    colTotal = GenerateNewTotalTimesheet(col.Col_Id, isDecimalHours, cartelliniToInitializeExport, BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal);
                    List<TimesheetModuleItem> TotaleCartellini = new List<TimesheetModuleItem>();
                    TotaleCartellini.Add(colTotal);
                    cartellini.Add("totale", TotaleCartellini);

                    TimesheetModuleItem colTotalStr = GenerateNewTotalTimesheet(col.Col_Id, isDecimalHours, straordinariCartellini, BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal);

                    List<TimesheetModuleItem> StrCartellini = new List<TimesheetModuleItem>();
                    StrCartellini.Add(colTotalStr);
                    cartellini.Add("Totstraordinari", StrCartellini);

                    TimesheetModuleItem colTotalDelta = GenerateNewTotalTimesheet(col.Col_Id, isDecimalHours, deltaCartellini, BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE), minDate, maxDate, ++tsOrder, 0, showWeeklyTotal);

                    List<TimesheetModuleItem> DeltaCartellini = new List<TimesheetModuleItem>();
                    DeltaCartellini.Add(colTotalDelta);
                    cartellini.Add("TotDelta", DeltaCartellini);
            }
            #endregion


            return cartellini;
        }
        public static int durataCamere(int colId, List<Reg_V> lista)
        {
            int somma = 0;

            var data = lista.GroupBy(x => x.Data_Ora_Fig_E)
                        .Select(x => x.Key).ToList();


            return somma;
        }

        private static void saveStrordinariCartellini(List<TimesheetModuleItem> generatedStraordinariCartellini)
        {
            List<Timesheet> timesheetsToSave = new List<Timesheet>();
            foreach (TimesheetModuleItem tmi in generatedStraordinariCartellini)
            {
                Col col = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == tmi.ColId);
                timesheetsToSave.Add(ConvertTimesheetModuleItemToTimesheet(tmi));

                //Se il cartellino appena salvato è successivo a quello precedentemente elaborato, aggiorna la data
                if (col != default(Col) && (col.Data_Ultimo_Cartellino_Elaborato == null || col.Data_Ultimo_Cartellino_Elaborato < tmi.StartDate))
                {
                    col.Data_Ultimo_Cartellino_Elaborato = tmi.StartDate;
                }
            }

            RepoManager.TimesheetRepo.Add(timesheetsToSave, true);
            RepoManager.ColRepo.SaveChanges();
        }

        public object this[string propertyName]
        {
            get { return GetType().GetProperty(propertyName).GetValue(this, null); }
            set { GetType().GetProperty(propertyName).SetValue(this, value, null); }
        }

        public static TimesheetModuleItem ConvertTimesheetToTimesheetModuleItem(Timesheet timesheet, bool isDecimalHours)
        {
            // inizializzazione del valore di ritorno del metodo
            var newTimesheet = new TimesheetModuleItem(isDecimalHours);
            // inserimento della data che indica il mese di elaborazione
            newTimesheet.StartDate = timesheet.Month;

            var daysMinutes = RepoManager.Tab_OrariRepo.GetEmptyMinutesPlan(timesheet.Month, CommonService.GetLastMonthDay(timesheet.Month), false);
            DateTime processingDate = timesheet.Month;

            for (int i = 1, z = CommonService.GetLastMonthDay(timesheet.Month).Day; i <= z; i++)
            {
                if (daysMinutes.ContainsKey(processingDate))
                {
                    daysMinutes[processingDate] = new Tuple<double, TimeSpan?, TimeSpan?>(timesheet["Day" + i.ToString("00")] != null ? ((TimeSpan)timesheet["Day" + i.ToString("00")]).TotalMinutes : 0, null, null);
                }

                processingDate = processingDate.AddDays(1);
            }

            // inserimento del calcolo dei totali all'interno dell'oggetto timesheet
            newTimesheet.PopulateHoursWithDate(daysMinutes);
            newTimesheet.IsFromFreeTimeSheet = false;
            newTimesheet.FreeTimeSheetId = 0;
            newTimesheet.InsertColValues(timesheet.ColId);
            newTimesheet.InsertCantValues(0);

            var mot_tmp = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == timesheet.Justification);
            if (mot_tmp == null)
            {
                newTimesheet.Justification = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "MOTIVAZIONI" && td.Chiave_Tab == timesheet.Justification).Decodifica_Tab;
            }
            else
            {
                newTimesheet.Justification = mot_tmp.Decodifica_Tab;
            }
            return newTimesheet;
        }

        public static Timesheet ConvertTimesheetModuleItemToTimesheet(TimesheetModuleItem tmi)
        {
            Timesheet timesheet = new Timesheet();
            timesheet.ColId = tmi.ColId;
            timesheet.Justification = RepoManager.Tab_DecodRepo.First(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Decodifica_Tab == tmi.Justification).Chiave_Tab;
            timesheet.Month = tmi.StartDate;

            for (int i = 1, z = CommonService.GetLastMonthDay(tmi.StartDate).Day; i <= z; i++)
            {
                string dayString = "Day" + i.ToString("00");

                timesheet[dayString] = TimeSpan.FromMinutes(Convert.ToInt32(CommonService.FromHoursToMinutes((double)tmi[dayString])));
            }

            return timesheet;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ritorna l'ora da visualizzare per il giorno specificato nel formato specificato.
        /// </summary>
        /// <param name="keyValue">Il numero del giorno del mese di cui recuperare il numero di ore.</param>
        /// <param name="isDecimalHours">Se impostato a <c>true</c> allora le ore saranno ritornate in centesimi; se impostato a <c>false</c> il ritorno sarà in sessantesimi.</param>
        /// <returns>Le ore da visualizzare per il giorno specificato nel formato specificato.</returns>
        private double GetHoursValue(int keyValue, bool isDecimalHours)
        {
            // inizializzazione del valore di ritorno del metodo
            double value = 0;

            // se è presente tra i dati il giorno richiesto allora si ritorna il valore salvato;
            // in caso il giorno non sia presente si ritorna il valore 0 (e cioè si lascia ritornare il valore di default)
            if (DaysHours.ContainsKey(keyValue))
            {
                value = CommonService.GetDoubleFromMinutes(GetDayMinutes(keyValue), isDecimalHours);
            }

            // ritorno del valore del metodo
            return value;
        }

        /// <summary>
        /// Imposta l'ora per il giorno specificato.
        /// </summary>
        /// <param name="key">Il numero del giorno del mese di cui impostare il numero di ore.</param>
        /// <param name="value">Il valore da impostrare.</param>
        /// <returns>Le ore da impostare per il giorno specificato.</returns>
        public void SetHoursValue(int key, double value)
        {
            DaysHours[key] = Tuple.Create<double, TimeSpan?, TimeSpan?>(value, null, null);
        }

        /// <summary>
        /// Calcola il totale (in minuti) della settimana specificata.
        /// </summary>
        /// <param name="weekNumber">il numero della settimana da processare.</param>
        /// <returns>Il totale (in minuti) della settimana specificata.</returns>
        private int GetWeekTotal(int weekNumber)
        {
            // inizializzazione del valore di ritorno del metodo
            int returnValue = 0;

            // inizializzazione di inizio e fine mese
            DateTime firstMonthDate = StartDate;
            DateTime startDate = firstMonthDate;
            DateTime endDate = CommonService.GetLastMonthDay(startDate);
            DateTime lastMonthDate = endDate;

            // eventuale aggiustamento delle date del mese con la chiusura della settimana iniziale e finale
            if (startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
            if (endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

            // recupero delle date nel periodo richiest
            IEnumerable<DateTime> sundayInPeriod = CommonService.GetDatesFromPeriod(startDate, endDate).Where(dt => dt.DayOfWeek == DayOfWeek.Sunday).ToList();

            // se sono state trovate delle settimane e ne sono presenti in numero richiesto
            if (sundayInPeriod.Any() && sundayInPeriod.Count() >= weekNumber)
            {
                int sundayDayNumber = sundayInPeriod.ElementAt(weekNumber - 1).Day;

                // sono recuperati i giorni da processare per il totale
                IEnumerable<int> weekDays = TimesheetTotalController.GetWeekDaysList(sundayDayNumber, lastMonthDate.Day).ToList();

                // per ogni giorno da processare, si somma il valore di ritorno
                returnValue = weekDays.Aggregate(returnValue, (current, weekDay) => current + GetDayMinutes(weekDay));
            }

            return returnValue;
        }

        /// <summary>
        /// Gestisce l'applicazione dell'eventuale autorizzazione straordinario sui dati specificati.
        /// </summary>
        /// <param name="originalDeltaMinutes">Il delta (in minuti) originale da trattare.</param>
        /// <param name="dayNumber">Il numero del giorno nel mese da processare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare il processo.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca.</param>
        /// <returns>Il monte minuti adatattato all'eventuale autorizzazione straordinario</returns>
        private static int getDeltaMinutesWithAutStr(int originalDeltaMinutes, DateTime processingDate, int entityId)
        {
            // per default si ritornano i minuti passati come parametro
            int processedDeltaMinutes = originalDeltaMinutes;

            // si verifica se è usabile la configurazione dell'autorizzazione straordinari
            bool autStrUsable = isAutStrUsable(originalDeltaMinutes);

            if (autStrUsable)
            {
                // si recupera per quanti minuti quel collaboratore risulta autorizzato agli straordinari in quella data
                int authorizedMinutes = RepoManager.Aut_StrRepo.AutStrColAuthorization(entityId, processingDate) * 60;

                if (processedDeltaMinutes > authorizedMinutes)
                {
                    processedDeltaMinutes = authorizedMinutes;
                }
            }
            // ritorno del delta calcolato dal metodo
            return processedDeltaMinutes;
        }

        /// <summary>
        /// Determina se per gli specifici dati in processo è utilizzabile la gesione dell'autorizzazione straordinario.
        /// </summary>
        /// <param name="otherEntityId">L'identificativo dell'eventuale altra entità in processo.</param>
        /// <param name="deltaMinutes">The original delta minutes.</param>
        /// <returns><c>true</c> se l'autorizzazione straordinari è utilizzabile; altrimenti <c>false</c></returns>
        private static bool isAutStrUsable(int deltaMinutes)
        {
            // di default l'autorizzazione straordinari non è utilizzabile
            bool isUsable = false;

            // si procede alla verifica dell'usabilità solamente se il modulo di gestione dell'autorizzazione straordinari è abilitato e
            // si sta processando un collaboratore con totale completo
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Aut_Str)
            {
                // se la configurazione prevede l'utilizzo dell'autorizzazione straordinari e il delta è superiore a 0
                // allora la configurazione è usabile
                if (RepoManager.ParamRepo.ParametersRow.Aut_Str_Tipo.HasValue)
                {
                    isUsable = RepoManager.ParamRepo.ParametersRow.Aut_Str_Tipo.Value == 1 && deltaMinutes > 0;
                }
            }

            // ritorno del valore calcolato dal metodo
            return isUsable;
        }

        #endregion

    }
}
