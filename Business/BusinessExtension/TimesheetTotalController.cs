using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Business;
using Business.Repository;
using Common;

namespace Business.BusinessExtension
{
    /// <summary>
    /// Classe utilizzata per maneggiare i totali di visualizzazione nella griglia
    /// del timesheet
    /// </summary>
    public class TimesheetTotalController
    {
        #region Private Constants

        /// <summary>
        /// Il nome dell'entità collaboratore utilizzata per la verifica della distinzione dei totali
        /// </summary>
        private const string ColEntityName = "Col";

        #endregion

        #region Fields

        /// <summary>
        /// L'elenco degli oggetti timesheet di cui si devono calcolare i totali
        /// </summary>
        private List<TimesheetModuleItem> _tsmItems;

        /// <summary>
        /// Il valore che segnala l'entità di riferimento dei totali (collaboratori/cantieri).
        /// </summary>
        private string _referenceEntity = "Col";

        /// <summary>
        /// Il tipo di recupero ore da abbinare a un'eventuale gestione dell'autorizzazione straordinario
        /// </summary>
        private readonly TimesheetRecoveryHoursTypeEnum _hoursRecoveryType = (TimesheetRecoveryHoursTypeEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheetRecoveryHoursTypeEnum);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TimesheetTotalController"/> class.
        /// </summary>
        /// <param name="tsmItems">L'elenco degli oggetti timesheet di cui calcolare il totale.</param>
        /// <param name="isDecimalHour"><c>true</c> se le ore andranno espresse in centesimi; <c>false</c> in caso di sessantesimi.</param>
        /// <param name="entityName">L'entità a cui fanno riferimento i totali maneggiati dal corrente oggetto</param>
        public TimesheetTotalController(List<TimesheetModuleItem> tsmItems, bool isDecimalHour, string entityName)
        {
            TsmItems = tsmItems;
            IsDecimalHour = isDecimalHour;
            ReferenceEntity = entityName;
        }

        #endregion

        #region Properties

        #region Private Properties

        /// <summary>
        /// Recupera la data di start del primo timesheet configurato; se non presente <see cref="DateTime.MinValue"/>.
        /// </summary>
        /// <value>
        /// La data di start del primo timesheet configurato; se non presente <see cref="DateTime.MinValue"/>.
        /// </value>
        private DateTime FirstTimesheetStartDate
        {
            get
            {
                DateTime returnDate = DateTime.MinValue;

                if (TsmItems != null)
                {
                    TimesheetModuleItem firstTsm = TsmItems.FirstOrDefault();
                    if (firstTsm != default(TimesheetModuleItem))
                        returnDate = firstTsm.StartDate;
                }

                return returnDate;
            }
        }


        /// <summary>
        /// Recupera la data di start del primo timesheet configurato; se non presente <see cref="DateTime.MinValue"/>.
        /// </summary>
        /// <value>
        /// La data di start del primo timesheet configurato; se non presente <see cref="DateTime.MinValue"/>.
        /// </value>
        private DateTime LastTimeShettStartDate
        {
            get
            {
                DateTime returnDate = DateTime.MinValue;

                if (TsmItems != null)
                {
                    TimesheetModuleItem lastTsm = TsmItems.LastOrDefault();
                    if (lastTsm != default(TimesheetModuleItem))
                        returnDate = lastTsm.StartDate;
                }

                return returnDate;
            }
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Recupera o imposta l'elenco degli oggetti timesheet di cui si devono calcolare i totali.
        /// </summary>
        /// <value>
        /// L'elenco degli oggetti timesheet di cui si devono calcolare i valori.
        /// </value>
        public List<TimesheetModuleItem> TsmItems
        {
            get { return _tsmItems; }
            set { _tsmItems = value; }
        }

        /// <summary>
        /// Recupera o imposta il valore che segnala se l'oggetto corrente deve esporre le ore in centesimo o in sessantesimi.
        /// </summary>
        /// <value>
        /// <c>true</c> se l'istanza corrente deve esporre le ore in censtesimi; altrimenti, per i sessantesimi, <c>false</c>.
        /// </value>
        public bool IsDecimalHour { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che segnala l'entità di riferimento dei totali (collaboratori/cantieri).
        /// </summary>
        /// <value>
        /// Il valore che segnala l'entità di riferimento dei totali (collaboratori/cantieri).
        /// </value>
        public string ReferenceEntity
        {
            get
            {
                return _referenceEntity;
            }

            set
            {
                _referenceEntity = value;
            }
        }

        #endregion

        #endregion

        #region Public Methods

        #region Gestione totali giorno

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore effettuate per il giorno specificato per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno nel mese da cercare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetDayTotalHours(int dayNumber, int entityId, int? otherEntityId)
        {
            double totalMinutes = GetTotalMinutes(dayNumber, entityId, otherEntityId);

            // se richiesto dalla customizzazione, recupero le ore di motivazione come ore lavorate
            double justificationHours = 0d;
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.JustificationHourIsWorkedHoursEnum);
            if (customizationVersion == (int)JustificationHourIsWorkedHoursEnum.Use)
                justificationHours = GetJustificationMinutes(dayNumber, entityId, otherEntityId);


            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(totalMinutes + justificationHours), IsDecimalHour), 2); ;
        }

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore giorno effettuate per il giorno specificato per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno nel mese da cercare.</param>
        /// <param name="entityId">L'id del collaboratore/cantieri per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetDayStrHours(int dayNumber, int entityId, int? otherEntityId)
        {
            // recupero il numero di minuti pianificate per il giorno/collaboratore o cantiere
            var plannedMinutes = GetTotalPlanMinutes(dayNumber, entityId, otherEntityId);

            // recupero il numero di minuti effettuati nel giorno per il giorno/collaboratore o cantiere
            var totalDayMinutes = GetTotalDayMinutes(dayNumber, entityId, otherEntityId);

            // la differenza tra quanto effettuato e quanto previsto, se maggiore di 0, è lo straordinario (con passaggio di aggiustamento per autorizzazione)
            double returnValue = GetDeltaMinutesWithAutStr(Convert.ToInt32(totalDayMinutes - plannedMinutes), dayNumber, entityId, otherEntityId);
            returnValue = returnValue < 0 ? 0 : Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2);

            // ritorno del valore del metodo
            return returnValue;
        }

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore notture effettuate per il giorno specificato per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno nel mese da cercare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetNocturneStrHours(int dayNumber, int entityId, int? otherEntityId)
        {
            // recupero il numero di minuti pianificate per il giorno/collaboratore o cantiere
            double plannedMinutes = GetTotalPlanMinutes(dayNumber, entityId, otherEntityId);

            // recupero il numero di minuti diurni effettuati nel giorno per il collaboratore o cantiere
            double totalDayMinutes = GetTotalDayMinutes(dayNumber, entityId, otherEntityId);

            // recupero il numero di minuti notturni effettuati nel giorno per il collaboratore o cantiere
            double totalNocturnMinutes = GetTotalNocturnMinutes(dayNumber, entityId, otherEntityId);

            // calcolo degli eventuali straordinari diurni ripassati dalla gestione dell'autorizzazione straordinari
            double dayStrMinutes = GetDeltaMinutesWithAutStr(Convert.ToInt32(totalDayMinutes - plannedMinutes), dayNumber, entityId, otherEntityId);

            // calcolo degli straordinari complessivi (diurni + notturni) ripassati dalla gestione dell'autorizzazione straordinari
            double totalStrMinutes = GetDeltaMinutesWithAutStr(Convert.ToInt32((totalDayMinutes + totalNocturnMinutes) - plannedMinutes), dayNumber, entityId, otherEntityId);

            // ci sono degli straordinari notturni se e solo se la
            // ci sono già degli straordinari diurni o se gli straordinari diurni sono
            // inferiori al previsto e si supera la soglia del previsto con i notturni
            double returnValue = 0;
            if ((dayStrMinutes > 0) ||// ci sono degli straordinari diurni o
                ((dayStrMinutes <= 0) && (totalStrMinutes > 0)) // non ci sono straordinari diurni ma con i notturni si sfora il previsto
                )
            {
                // se ci sono degli stroardinari diurni la quota di notturno è tutta quella effetuata
                if (dayStrMinutes > 0)
                    returnValue = totalNocturnMinutes;
                else // se non ci sono straordinari diurni i notturni sono la rimanenza
                    returnValue = totalStrMinutes;
            }

            returnValue = returnValue < 0 ? 0 : Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2);

            return returnValue;
        }

        /// <summary>
        /// Metodo che recupera e ritorna il delta per il giorno specificato per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno nel mese da cercare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetDeltaHours(int dayNumber, int entityId, int? otherEntityId)
        {
            // calcolo del valore del delta in minuti senza autorizzazione straordinario
            int deltaMinutesWithoutAutStr = GetDeltaMinutesWithoutAutStr(dayNumber, entityId, otherEntityId);

            // calcolo del delta come differenza tra quanto fatto e quando previsto (con passaggio per verifica autorizzazione straordinario)
            int deltaMinutes = GetDeltaMinutesWithAutStr(deltaMinutesWithoutAutStr, dayNumber, entityId, otherEntityId);

            // ritorno del valore calcolato dal metodo
            return Math.Round(CommonService.GetDoubleFromMinutes(deltaMinutes, IsDecimalHour), 2);
        }

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore piano per il giorno specificato per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno nel mese da cercare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetDayPlanHours(int dayNumber, int entityId, int? otherEntityId)
        {
            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(GetTotalPlanMinutes(dayNumber, entityId, otherEntityId)), IsDecimalHour), 2); ;
        }

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore ordinarie effettuate per il giorno specificato per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno nel mese da cercare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetDayOrdinaryHours(int dayNumber, int entityId, int? otherEntityId)
        {
            // recupero il numero di minuti pianificate per il giorno/collaboratore o cantiere
            var plannedMinutes = GetTotalPlanMinutes(dayNumber, entityId, otherEntityId);
            double totalDayMinutes;

            // Se è attiva la personalizzazione che prevede che gli arrotondamenti non siano contati nelle ore ordinarie, recupero le ore senza arrotondamenti
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoArrotInOrdinaryEnum) == (int)NoArrotInOrdinaryEnum.Enabled)
            {
                totalDayMinutes = GetTotalMinutesNoArrot(dayNumber, entityId, otherEntityId);
            }
            // Altrimenti prendo le ore con gli arrotondamenti
            else
            { 
                // recupero il numero di minuti effettuati nel giorno per il giorno/collaboratore o cantiere
                totalDayMinutes = GetTotalMinutes(dayNumber, entityId, otherEntityId);
            }

            // il numero di ore ordinarie è calcolato come il numero di ore piano se il numero di ore totali supera il piano
            // (e cioè se la differenza tra le ore effettuate e le ore pianificate è >= 0) oppure come il numero di ore effettuate
            // se il numero di ore totali NON supera il piano (e cioè se la differenza tra le ore effettuate e le ore pianficate è < 0)
            var planDifference = totalDayMinutes - plannedMinutes;
            var returnValue = planDifference >= 0 ? plannedMinutes : totalDayMinutes;
            returnValue = returnValue < 0 ? 0 : Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2);

            // ritorno del valore del metodo
            return returnValue;
        }

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore effettuate con motivazione per il giorno specificato per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno nel mese da cercare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetDayJustificationHours(int dayNumber, int entityId, int? otherEntityId)
        {
            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(GetJustificationMinutes(dayNumber, entityId, otherEntityId)), IsDecimalHour), 2); ;
        }

        /// <summary>
        /// Metodo che recupera e ritorna l'ammontare di arrotondamenti per il giorno specificato per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno nel mese da cercare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetDayArrotHours(int dayNumber, int entityId, int? otherEntityId)
        {
            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(GetArrotMinutes(dayNumber, entityId, otherEntityId)), IsDecimalHour), 2);
        }

        #endregion

        #region Gestione totali mese

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore totali effettuate per il mese oggetto del controller per il collaboratore/cantiere specificato viene richiamato quando si categorizza
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetMonthTotalHours(int entityId, int? otherEntityId)
        {
            double returnValue = 0d;

            double monteMinuti = 0d;

            for (int dayNumber = 1; dayNumber <= 31; dayNumber++)
            {
                returnValue += GetTotalMinutes(dayNumber, entityId, otherEntityId);

                // se devo aggiungere al totale anche le motivazioni
                int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.JustificationHourIsWorkedHoursEnum);
                if (customizationVersion == (int)JustificationHourIsWorkedHoursEnum.Use)
                    returnValue += GetJustificationMinutes(dayNumber, entityId, otherEntityId);
            }

            // al totale dei minuti è aggiunto anche il monte minuti (se presente)
            monteMinuti= GetLastMinutesAmmount(entityId, otherEntityId);
            returnValue += monteMinuti;

            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2); ;
        }

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore di straordinario del mese (soolo differenza positiva) e a cui viene sommato il monte minuti. Viene chiamato solo quando si categorizza.
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantieri per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetMonthStrHours(int entityId, int? otherEntityId)
        {

            double returnValue = 0d;

            double monteMinuti = 0d;

            for (int dayNumber = 1; dayNumber <= 31; dayNumber++)
            {
                // recupero il numero di minuti pianificate per il giorno/collaboratore o cantiere
                double plannedMinutes = 0d;

                plannedMinutes += GetTotalPlanMinutes(dayNumber, entityId, otherEntityId);

                // recupero il numero di minuti effettuati nel giorno per il giorno/collaboratore o cantiere
                double totalDayMinutes = 0d;
                totalDayMinutes += GetTotalDayMinutes(dayNumber, entityId, otherEntityId);

                // la differenza tra quanto effettuato e quanto previsto, se maggiore di 0, è lo straordinario
                double partialReturnValue = totalDayMinutes - plannedMinutes;

                returnValue += partialReturnValue < 0 ? 0 : partialReturnValue;
            }

            // al totale dei minuti è aggiunto anche il monte minuti (se presente)
            monteMinuti= GetLastMinutesAmmount(entityId, otherEntityId);

            returnValue += monteMinuti;

            returnValue = Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2);

            // ritorno del valore del metodo
            return returnValue;
        }

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore notture effettuate per il mese oggetto del controller per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetMonthNocturneStrHours(int entityId, int? otherEntityId)
        {
            double returnValue = 0;

            for (int dayNumber = 1; dayNumber <= 31; dayNumber++)
            {

                // recupero il numero di minuti pianificate per il giorno/collaboratore o cantiere
                double plannedMinutes = 0d;
                plannedMinutes += GetTotalPlanMinutes(dayNumber, entityId, otherEntityId);

                // recupero il numero di minuti diurni effettuati nel giorno per il collaboratore o cantiere
                double totalDayMinutes = 0d;
                totalDayMinutes += GetTotalDayMinutes(dayNumber, entityId, otherEntityId);

                // recupero il numero di minuti notturni effettuati nel giorno per il collaboratore o cantiere
                double totalNocturnMinutes = 0d;
                totalNocturnMinutes += GetTotalNocturnMinutes(dayNumber, entityId, otherEntityId);

                // ci sono degli straordinari notturni se e solo se la
                // ci sono già degli straordinari diurni o se gli straordinari diurni sono
                // inferiori al previsto e si supera la soglia del previsto con i notturni
                double partialReturnValue = 0d;
                if ((totalDayMinutes - plannedMinutes > 0) || // ci sono degli straordinari diurni o
                    ((totalDayMinutes - plannedMinutes <= 0) && ((totalDayMinutes + totalNocturnMinutes) - plannedMinutes > 0)) // non ci sono straordinari diurni ma con i notturni si sfora il previsto
                    )
                {
                    // se ci sono degli stroardinari diurni la quota di notturno è tutta quella effetuata
                    if (totalDayMinutes - plannedMinutes > 0)
                        partialReturnValue += totalNocturnMinutes;
                    else // se non ci sono straordinari diurni i notturni sono la rimanenza
                        partialReturnValue += (totalDayMinutes + totalNocturnMinutes) - plannedMinutes;
                }

                returnValue += partialReturnValue;
            }

            returnValue = Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2);

            return returnValue;
        }

        /// <summary>
        /// Metodo che recupera e ritorna il delta per il mese oggetto del controller per il collaboratore/cantiere specificato tenendo conto 
        /// dell'autorizzazione straordinari e sommando il monte minuti (riporto ore precedenti). viene richiamato solo quando si categorizza
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetMonthDeltaHours(int entityId, int? otherEntityId)
        {

            double returnValue = 0d;

            double monteMinuti = 0d;

            //si va a ciclare su tutto il mese viene calcolato il delta me
            for (int dayNumber = 1; dayNumber <= 31; dayNumber++)
            {
                // calcolo del valore del delta in minuti senza autorizzazione straordinario
                int deltaMinutesWithoutAutStr = GetDeltaMinutesWithoutAutStr(dayNumber, entityId, otherEntityId);

                // aggiunta del calcolo al totale con passaggio per verifica autorizzazione straordinari
                returnValue += GetDeltaMinutesWithAutStr(deltaMinutesWithoutAutStr, dayNumber, entityId, otherEntityId);
            }

            monteMinuti = GetLastMinutesAmmount(entityId, otherEntityId);
            // al totale dei minuti è aggiunto anche il monte minuti (se presente)
            returnValue += monteMinuti;

            // ritorno la differenza tra quanto fatto e previsto
            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2);
        }

        /// <summary>
        /// Recupera il delta totale del mese utilizzato per il calcolo del monte minuti in caso di autorizzazione straordinario.
        /// </summary>
        /// <param name="colId">L'identificativo dell'entità da processare.</param>
        /// <returns>Il numero di minuti di delta del mese gestito dal total controller per l'entità specificata.</returns>
        public int GetMonthDeltaHoursForMonthlyMinutesWithAutStr(int colId, DateTime from, DateTime to)
        {

            // di default il numero di minuti è 0
            int monthDelta = 0;

            // recupero tutte le date di inizio mese tra la data di partenza e la data di arrivo
            var firstMonthDateList = CommonService.GetDatesFromPeriod(from, to).Where(dt => dt.Day == 1).ToList();

            for (int dayNumber = 1; dayNumber <= 31; dayNumber++)
            {
                // recupero i minuti effettuati
                double workedMinutes = 0d;

                workedMinutes = GetTotaleMinutesForLastMonthAmmount(dayNumber, colId, from,to);

                // recupero le ore previste
                double planMinutes = 0d;
                planMinutes = GetTotalPlanMinutes(dayNumber, colId, null);

                // aggiunta del calcolo al totale con passaggio per verifica autorizzazione straordinari
                monthDelta += GetDeltaMinutesWithAutStr(Convert.ToInt32(workedMinutes - planMinutes), dayNumber, colId, null);

            }

          
            // ritorno del valore del delta calcolato
            return monthDelta;

        }

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore piano per il mese oggetto del controller per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetMonthPlanHours(int entityId, int? otherEntityId)
        {
            double returnValue = 0d;

            for (int dayNumber = 1; dayNumber <= 31; dayNumber++)
                returnValue += GetTotalPlanMinutes(dayNumber, entityId, otherEntityId);

            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2); ;
        }

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore ordinarie effettuate per il mese oggetto del controller per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetMonthOrdinaryHours(int entityId, int? otherEntityId)
        {
            double returnValue = 0d;

            for (int dayNumber = 1; dayNumber <= 31; dayNumber++)
            {
                // recupero il numero di minuti pianificate per il giorno/collaboratore o cantiere
                double plannedMinutes = 0d;

                plannedMinutes += GetTotalPlanMinutes(dayNumber, entityId, otherEntityId);

                // recupero il numero di minuti effettuati nel giorno per il giorno/collaboratore o cantiere
                var totalDayMinutes = 0d;

                // Se è attiva la personalizzazione che prevede che gli arrotondamenti non siano contati nelle ore ordinarie, recupero le ore senza arrotondamenti
                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoArrotInOrdinaryEnum) == (int)NoArrotInOrdinaryEnum.Enabled)
                {
                    totalDayMinutes += GetTotalMinutesNoArrot(dayNumber, entityId, otherEntityId);
                }
                // Altrimenti prendo le ore con gli arrotondamenti
                else
                {
                    totalDayMinutes += GetTotalMinutes(dayNumber, entityId, otherEntityId);
                }

                // il numero di ore ordinarie è calcolato come il numero di ore piano se il numero di ore totali supera il piano
                // (e cioè se la differenza tra le ore effettuate e le ore pianificate è >= 0) oppure come il numero di ore effettuate
                // se il numero di ore totali NON supera il piano (e cioè se la differenza tra le ore effettuate e le ore pianficate è < 0)
                var planDifference = totalDayMinutes - plannedMinutes;
                double partialReturnValue = planDifference >= 0 ? plannedMinutes : totalDayMinutes;
                returnValue += partialReturnValue < 0 ? 0 : partialReturnValue;
            }

            returnValue = Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2);

            // ritorno del valore del metodo
            return returnValue;
        }

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore effettuate con motivazione per il mese oggetto del controller per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetMonthJustificationHours(int entityId, int? otherEntityId)
        {
            double returnValue = 0d;

            for (int dayNumber = 1; dayNumber <= 31; dayNumber++)
                returnValue += GetJustificationMinutes(dayNumber, entityId, otherEntityId);

            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2); ;
        }

        /// <summary>
        /// Metodo che recupera e ritorna il numero di ore effettuate con motivazione per il mese oggetto del controller per il collaboratore/cantiere specificato
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il valore in formato centesimi o sessantesimi calcolato</returns>
        public double GetMonthArrotHours(int entityId, int? otherEntityId)
        {
            double returnValue = 0d;

            for (int dayNumber = 1; dayNumber <= 31; dayNumber++)
                returnValue += GetArrotMinutes(dayNumber, entityId, otherEntityId);

            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2); ;
        }

        #endregion

        #region Gestione totali per settimana

        /// <summary>
        /// Recupera il numero di ore effettuate per la settimana di cui è specificata la domenica (per collaboratore/cantiere).
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>Il numero di ore effettuate in formato centesimi o sessantesimi calcolato.</returns>
        public double GetWeekTotalHours(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {
            // inizializzazione del valore di ritorno del metodo
            double returnValue = 0d;

            // si recupera l'elenco degli indici giorno su cui cilcare
            IEnumerable<int> weekDays = GetWeekDaysList(sundayDayNumber, lastMonthDay);

            // si cicla su ogni giorno della settimana
            foreach (int weekDay in weekDays)
            {
                returnValue += GetTotalMinutes(weekDay, entityId, otherEntityId);

                // se devo aggiungere al totale anche le motivazioni
                int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.JustificationHourIsWorkedHoursEnum);
                if (customizationVersion == (int)JustificationHourIsWorkedHoursEnum.Use)
                    returnValue += GetJustificationMinutes(weekDay, entityId, otherEntityId);

            }

            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2); ;
        }

        /// <summary>
        /// Recupera il numero di ore straordinarie per la settimana di cui è specificata la domenica (per collaboratore/cantiere).
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>Il numero di ore straordinarie in formato centesimi o sessantesimi calcolato.</returns>
        public double GetWeekStrHours(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {
            // inizializzazione del valore di ritorno del metodo
            double returnValue = 0d;

            // si recupera l'elenco degli indici giorno su cui cilcare
            IEnumerable<int> weekDays = GetWeekDaysList(sundayDayNumber, lastMonthDay);

            // si cicla su ogni giorno della settimana
            foreach (int weekDay in weekDays)
            {
                // recupero il numero di minuti pianificate per il giorno/collaboratore o cantiere
                double plannedMinutes = 0d;

                plannedMinutes += GetTotalPlanMinutes(weekDay, entityId, otherEntityId);

                // recupero il numero di minuti effettuati nel giorno per il giorno/collaboratore o cantiere
                double totalDayMinutes = 0d;
                totalDayMinutes += GetTotalDayMinutes(weekDay, entityId, otherEntityId);

                // la differenza tra quanto effettuato e quanto previsto, se maggiore di 0, è lo straordinario (passato per la eventuale gestione approvazione straordinari)
                double partialReturnValue = GetDeltaMinutesWithAutStr(Convert.ToInt32(totalDayMinutes - plannedMinutes), weekDay, entityId, otherEntityId);
                returnValue += partialReturnValue < 0 ? 0 : partialReturnValue;
            }

            returnValue = Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2);

            // ritorno del valore del metodo
            return returnValue;
        }

        /// <summary>
        /// Recupera il numero di ore straordinarie notturne per la settimana di cui è specificata la domenica (per collaboratore/cantiere).
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>Il numero di ore straordinarie notturne in formato centesimi o sessantesimi calcolato.</returns>
        public double GetWeekNocturneStrHours(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {
            // inizializzazione del valore di ritorno del metodo
            double returnValue = 0d;

            // si recupera l'elenco degli indici giorno su cui cilcare
            IEnumerable<int> weekDays = GetWeekDaysList(sundayDayNumber, lastMonthDay);

            // si cicla su ogni giorno della settimana
            foreach (int weekDay in weekDays)
            {
                // recupero il numero di minuti pianificate per il giorno/collaboratore o cantiere
                double plannedMinutes = 0d;
                plannedMinutes += GetTotalPlanMinutes(weekDay, entityId, otherEntityId);

                // recupero il numero di minuti diurni effettuati nel giorno per il collaboratore o cantiere
                double totalDayMinutes = 0d;
                totalDayMinutes += GetTotalDayMinutes(weekDay, entityId, otherEntityId);

                // recupero il numero di minuti notturni effettuati nel giorno per il collaboratore o cantiere
                double totalNocturnMinutes = 0d;
                totalNocturnMinutes += GetTotalNocturnMinutes(weekDay, entityId, otherEntityId);

                // calcolo degli eventuali straordinari diurni ripassati dalla gestione dell'autorizzazione straordinari
                double dayStrMinutes = GetDeltaMinutesWithAutStr(Convert.ToInt32(totalDayMinutes - plannedMinutes), weekDay, entityId, otherEntityId);

                // calcolo degli straordinari complessivi (diurni + notturni) ripassati dalla gestione dell'autorizzazione straordinari
                double totalStrMinutes = GetDeltaMinutesWithAutStr(Convert.ToInt32((totalDayMinutes + totalNocturnMinutes) - plannedMinutes), weekDay, entityId, otherEntityId);

                // ci sono degli straordinari notturni se e solo se la
                // ci sono già degli straordinari diurni o se gli straordinari diurni sono
                // inferiori al previsto e si supera la soglia del previsto con i notturni
                double partialReturnValue = 0d;
                if ((dayStrMinutes > 0) || // ci sono degli straordinari diurni o
                    ((dayStrMinutes <= 0) && (totalStrMinutes > 0)) // non ci sono straordinari diurni ma con i notturni si sfora il previsto
                    )
                {
                    // se ci sono degli stroardinari diurni la quota di notturno è tutta quella effetuata
                    if (dayStrMinutes > 0)
                        partialReturnValue += totalNocturnMinutes;
                    else // se non ci sono straordinari diurni i notturni sono la rimanenza
                        partialReturnValue += totalStrMinutes;
                }

                returnValue += partialReturnValue;
            }

            returnValue = Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2);

            return returnValue;
        }

        /// <summary>
        /// Recupera il numero di ore di delta per la settimana di cui è specificata la domenica (per collaboratore/cantiere).
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>Il numero di ore di delta in formato centesimi o sessantesimi calcolato.</returns>
        public double GetWeekDeltaHours(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {
            // inizializzazione del valore di ritorno del metodo
            double returnValue = 0d;

            // si recupera l'elenco degli indici giorno su cui cilcare
            IEnumerable<int> weekDays = GetWeekDaysList(sundayDayNumber, lastMonthDay);

            // si cicla su ogni giorno della settimana
            foreach (int weekDay in weekDays)
            {
                // calcolo del valore del delta in minuti senza autorizzazione straordinario
                int deltaMinutesWithoutAutStr = GetDeltaMinutesWithoutAutStr(weekDay, entityId, otherEntityId);

                // si aggiunge il totale al calcolo processando previamente l'autorizzazione straordinari
                returnValue += GetDeltaMinutesWithAutStr(deltaMinutesWithoutAutStr, weekDay, entityId, otherEntityId);
            }

            // ritorno la differenza tra quanto fatto e previsto
            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2);
        }

        /// <summary>
        /// Recupera il numero di ore piano per la settimana di cui è specificata la domenica (per collaboratore/cantiere).
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>Il numero di ore piano in formato centesimi o sessantesimi calcolato.</returns>
        public double GetWeekPlanHour(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {

            // si recupera l'elenco degli indici giorno su cui cilcare
            IEnumerable<int> weekDays = GetWeekDaysList(sundayDayNumber, lastMonthDay);

            // si recupera la somma dei piani della settimana
            double returnValue = weekDays.Sum(weekDay => GetTotalPlanMinutes(weekDay, entityId, otherEntityId));

            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2); ;
        }

        /// <summary>
        /// Recupera il numero di ore ordinarie per la settimana di cui è specificata la domenica (per collaboratore/cantiere).
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>Il numero di ore ordinarie in formato centesimi o sessantesimi calcolato.</returns>
        public double GetWeekOrdinaryHour(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {
            // inizializzazione del valore di ritorno del metodo
            double returnValue = 0d;

            // si recupera l'elenco degli indici giorno su cui cilcare
            IEnumerable<int> weekDays = GetWeekDaysList(sundayDayNumber, lastMonthDay);

            // si cicla su ogni giorno della settimana
            foreach (int weekDay in weekDays)
            {
                // recupero il numero di minuti pianificate per il giorno/collaboratore o cantiere
                double plannedMinutes = 0d;

                plannedMinutes += GetTotalPlanMinutes(weekDay, entityId, otherEntityId);

                // recupero il numero di minuti effettuati nel giorno per il giorno/collaboratore o cantiere
                var totalDayMinutes = 0d;

                // Se è attiva la personalizzazione che prevede che gli arrotondamenti non siano contati nelle ore ordinarie, recupero le ore senza arrotondamenti
                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoArrotInOrdinaryEnum) == (int)NoArrotInOrdinaryEnum.Enabled)
                {
                    totalDayMinutes += GetTotalMinutesNoArrot(weekDay, entityId, otherEntityId);
                }
                // Altrimenti prendo le ore con gli arrotondamenti
                else
                {
                    totalDayMinutes += GetTotalMinutes(weekDay, entityId, otherEntityId);
                }

                // il numero di ore ordinarie è calcolato come il numero di ore piano se il numero di ore totali supera il piano
                // (e cioè se la differenza tra le ore effettuate e le ore pianificate è >= 0) oppure come il numero di ore effettuate
                // se il numero di ore totali NON supera il piano (e cioè se la differenza tra le ore effettuate e le ore pianficate è < 0)
                var planDifference = totalDayMinutes - plannedMinutes;
                double partialReturnValue = planDifference >= 0 ? plannedMinutes : totalDayMinutes;
                returnValue += partialReturnValue < 0 ? 0 : partialReturnValue;
            }

            returnValue = Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2);

            // ritorno del valore del metodo
            return returnValue;
        }

        /// <summary>
        /// Recupera il numero di ore con motivazione per la settimana di cui è specificata la domenica (per collaboratore/cantiere).
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>Il numero di ore con motivazione in formato centesimi o sessantesimi calcolato.</returns>
        public double GetWeekJustificationHours(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {
            // si recupera l'elenco degli indici giorno su cui cilcare
            IEnumerable<int> weekDays = GetWeekDaysList(sundayDayNumber, lastMonthDay);

            // si sommano in valori dei singoli giorni della settimana
            double returnValue = weekDays.Sum(weekDay => GetJustificationMinutes(weekDay, entityId, otherEntityId));

            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2); ;
        }

        /// <summary>
        /// Recupera il numero di ore arrotondamenti per la settimana di cui è specificata la domenica (per collaboratore/cantiere).
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>Il numero di ore arrotondamenti in formato centesimi o sessantesimi calcolato.</returns>
        public double GetWeekArrotHours(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {
            // si recupera l'elenco degli indici giorno su cui cilcare
            IEnumerable<int> weekDays = GetWeekDaysList(sundayDayNumber, lastMonthDay);

            // si sommano in valori dei singoli giorni della settimana
            double returnValue = weekDays.Sum(weekDay => GetArrotMinutes(weekDay, entityId, otherEntityId));

            return Math.Round(CommonService.GetDoubleFromMinutes(Convert.ToInt32(returnValue), IsDecimalHour), 2); ;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Recupeera la lista degli indici giorno della settimana che si conclude per la specifica domenica (per utilizzo in calcolo chiavi recupero ore).
        /// </summary>
        /// <param name="sundayDayNumber">La domenica di fine della settimana da calcolare.</param>
        /// <param name="lastMonthDay">L'ultimo giorno del mese oggetto del cartellino (utilizzato per le settimane a cavallo del mese stesso.</param>
        /// <returns>L'elenco degli indici giorno della settimana la cui domenica è specificata.</returns>
        public static IEnumerable<int> GetWeekDaysList(int sundayDayNumber, int lastMonthDay)
        {

            // in base al numero della domenica si calcola la lista con gli indici giorno della settimana:
            // - se il numero richiesto è inferiore a 100 basta sottrarre 7 a quanto richiesto
            // - se invece il numero richiesto è superiore a 100 si può andare a ritroso fino al 101 e successivamente riprendere dall'ultimo giorno del mese

            var weekDays = new List<int>();
            if (sundayDayNumber < 100)
                for (int dayIndex = sundayDayNumber; dayIndex > sundayDayNumber - 7; dayIndex--)
                    if (dayIndex <= 0)
                        weekDays.Add(dayIndex - 1);
                    else
                        weekDays.Add(dayIndex);
            else
            {
                for (int dayIndex = sundayDayNumber; dayIndex > sundayDayNumber - 7; dayIndex--)
                {
                    if (dayIndex > 100)
                        weekDays.Add(dayIndex);
                    else
                        weekDays.Add(lastMonthDay - (100 - dayIndex));
                }
            }
            return weekDays;
        }

        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Recupera la domenica di riferimento per il giorno specificato.
        /// </summary>
        /// <param name="dayNumber">Il giorno di cui ricercare la domenica di riferimento.</param>
        /// <returns>Il numero del giorno che identifica la domenica di riferimento del giorno specificato.</returns>
        private int GetCurrentSundayNumber(int dayNumber)
        {
            int returnDay = 0;

            // inizializzazione della data che indica il giorno espresso dal numero
            DateTime currentDay = GetDateFromIndex(dayNumber);

            if (currentDay != DateTime.MinValue)
            {
                // si recupera la domenica successiva al giorno corrente
                DateTime nextSunday = currentDay.DayOfWeek != DayOfWeek.Sunday ? currentDay.NextDayOfWeek(DayOfWeek.Sunday) : currentDay;

                // si ritorna il valore del numero della domenica in base al mese di riferimento
                // della stessa rispetto a quanto in processo
                if (nextSunday.Month == LastTimeShettStartDate.Month) // stesso mese
                    returnDay = nextSunday.Day;
                else if (nextSunday.Month > LastTimeShettStartDate.Month) // mese diverso - mese successivo
                    returnDay = nextSunday.Day + 100;
                else // mese diverso - mese precedente
                {
                    // si recupera la fine del mese della domenica e si calcola la differenza tra la domenica e 
                    // la sua fine mese, il valore portato in negativo sarà il numero del giorno
                    returnDay = -CommonService.GetDayDifference(nextSunday, CommonService.GetLastMonthDay(nextSunday));
                }
            }

            return returnDay;
        }

        /// <summary>
        /// Gestisce l'applicazione dell'eventuale autorizzazione straordinario sui dati specificati.
        /// </summary>
        /// <param name="originalDeltaMinutes">Il delta (in minuti) originale da trattare.</param>
        /// <param name="dayNumber">Il numero del giorno nel mese da processare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare il processo.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca.</param>
        /// <returns>Il monte minuti adatattato all'eventuale autorizzazione straordinario</returns>
        private int GetDeltaMinutesWithAutStr(int originalDeltaMinutes, int dayNumber, int entityId, int? otherEntityId)
        {
            // per default si ritornano i minuti passati come parametro
            int processedDeltaMinutes = originalDeltaMinutes;

            // si verifica se è usabile la configurazione dell'autorizzazione straordinari
            bool autStrUsable = IsAutStrUsable(otherEntityId, originalDeltaMinutes);

            if (autStrUsable)
            {
                // calcolo della data che si sta attualmente processando
                DateTime processingDate = GetDateFromIndex(dayNumber);

                if (processingDate != DateTime.MinValue)
                {
                    // si recupera per quanti minuti quel collaboratore risulta autorizzato agli straordinari in quella data
                    int autorizedMinutes = RepoManager.Aut_StrRepo.AutStrColAuthorization(entityId, processingDate) * 60;

                    #region Calcolo della gestione del recupero dei minuti

                    // se è richiesto l'utilizzo del recupero settimanale delle ore e sulla giornata corrente è presente un delta positivo
                    if (_hoursRecoveryType == TimesheetRecoveryHoursTypeEnum.WeeklyHoursRecovery && originalDeltaMinutes > 0 && autorizedMinutes == 0)
                    {
                        // calcolo della domenica di riferimento rispetto al giorno in elaborazione
                        int sundayDayNumber = GetCurrentSundayNumber(dayNumber);

                        // calcolo dell'ultimo giorno del mese in elaborazione
                        int lastMonthDay = CommonService.GetLastMonthDay(LastTimeShettStartDate).Day;

                        // si calcola il numero di minuti da recuperare in settimana
                        int weekRecoveryMinutes = CalculateRecoveryMinutes(entityId, otherEntityId, sundayDayNumber, lastMonthDay);

                        // si procede con l'elaborazione solamente se ci sono dei minuti da ripartire in settimana
                        if (weekRecoveryMinutes != 0)
                        {
                            // inizializzazione del residuo di smistamento del recupero al giorno precedente a quello attualmente in elaborazione
                            int residualRecoveryMinutes = weekRecoveryMinutes;

                            // si cicla sui giorni della settimana fino al giorno antecedente al corrente
                            // (se non si sta processando il primo giorno della settimana)
                            IEnumerable<int> weekDayList = GetWeekDaysList(sundayDayNumber, lastMonthDay).OrderBy(dayVal => dayVal).ToList();
                            if (weekDayList.First() != dayNumber)
                                foreach (int dayIndex in weekDayList)
                                {
                                    if (dayIndex >= dayNumber)
                                        break;

                                    //vengono estratti il numero di minuti di straordinari non autorizzati
                                    int unautorizedDeltaDayMinutes = GetUnautorizedDeltaMinutes(dayIndex, entityId, otherEntityId);

                                    //se il numero totale di minuti non autorizzati è maggiore di 0
                                    if (unautorizedDeltaDayMinutes > 0)
                                        //si va a sottrarre alla "banca" dei minuti di recupero i minuti NON autorizati di straordinario
                                        residualRecoveryMinutes -= unautorizedDeltaDayMinutes;
                                }

                            // se è ancora presente del residuo da processare, allora lo si autorizza
                            // per quanto è possibile nel giorno in processo (per quanto delta positivo è eventualmente possibile)
                            if (residualRecoveryMinutes > 0)
                                autorizedMinutes = originalDeltaMinutes <= residualRecoveryMinutes ? originalDeltaMinutes : residualRecoveryMinutes;
                        }
                    }

                    #endregion
                    // se il numero di minuti autorizzati è inferiore a quanto effetuato allora lo si riporta a quanto autorizzato
                    //riposro il delta al tetto massimo autorizzato
                    if (autorizedMinutes < originalDeltaMinutes)
                        processedDeltaMinutes = autorizedMinutes;
                }
            }

            // ritorno del delta calcolato dal metodo
            return processedDeltaMinutes;

        }

        /// <summary>
        /// Calcola e restituisce il numero di minuti da recuperare in base ai parametri specificati.
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca..</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>
        /// Il numero di minuti che è possibile recuperare.
        /// </returns>
        private int CalculateRecoveryMinutes(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {
            int returnValue = 0;

            // si calcola il numero di straordinari autorizzati effettuati in settimana
            int autorizedDeltaMinutes = GetWeekAutorizedDeltaMinutes(entityId, otherEntityId, sundayDayNumber, lastMonthDay);

            // si calcola il numero di minuti delta non autorizzati per la settimana (delta positivo)
            int unautorizedDeltaMinutes = GetWeekUnautorizedDeltaMinutes(entityId, otherEntityId, sundayDayNumber, lastMonthDay);

            // si calcola il numero di minuti con delta negativo nel corso della settimana
            int negativeDeltaMinutes = GetWeekDeltaNegativeMinutes(entityId, otherEntityId, sundayDayNumber, lastMonthDay);

            // si hanno dei minuti da recuperare solamente se ci sono dei minuti da ripartire (straordinari non autorizzati) e dei luoghi
            // in cui ripartirli (giorni con delta negativo); inoltre viene verificato che già il numero di minuti di delta negativo non superi
            // quanto già effettuato come straordinario autorizzato
            if (unautorizedDeltaMinutes != 0 && negativeDeltaMinutes != 0 && autorizedDeltaMinutes < negativeDeltaMinutes)
            {
                // calcolo il totale dei minuti da recuperare a completamento dei delta negativi:
                // 1- se il numero di minuti non autorizzati è inferiore o uguale al numero di minuti in delta negativo, il
                //   numero di minuti da ripartire è il numero di minuti non autorizzati come straordinario (si recupera tutto il recuperabile)
                // 2- se il numero di minuti non autorizzati è maggiore al numero di minuti in delta negativo, il numero di minuti
                //   da ripartire è il numero di minuti di delta negativo (si recupera quanto è stato fatto in meno)
                returnValue = unautorizedDeltaMinutes <= negativeDeltaMinutes - autorizedDeltaMinutes ? unautorizedDeltaMinutes : negativeDeltaMinutes;

            }

            return returnValue;
        }

        /// <summary>
        /// Recupera il numero di minuti di delta in una giornata senza tenere conto dell'eventuale autorizzazione straordinari configurata.
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno nel mese da processare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare il processo.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca.</param>
        /// <returns>Il valore di delta per i dati specificati che non tiene conto dell'eventuale autorizzazione straordinari.</returns>
        private int GetDeltaMinutesWithoutAutStr(int dayNumber, int entityId, int? otherEntityId)
        {
            // recupero i minuti effettuati
            var workedMinutes = GetTotalMinutes(dayNumber, entityId, otherEntityId);

            // recupero le ore previste
            var planMinutes = GetTotalPlanMinutes(dayNumber, entityId, otherEntityId);

            // se richiesto dalla customizzazione, recupero le ore di motivazione come ore lavorate
            double justificationHours = 0d;
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.JustificationHourIsWorkedHoursEnum);
            if (customizationVersion == (int)JustificationHourIsWorkedHoursEnum.Use)
                justificationHours = GetJustificationMinutes(dayNumber, entityId, otherEntityId);

            return Convert.ToInt32((workedMinutes + justificationHours) - planMinutes);
        }

        /// <summary>
        /// Recupera per il giorno e le entità specificate il numero di minuti delta positivi non autorizzati come straordinario.
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno del mese da processare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare il processo.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca.</param>
        /// <returns>Il numero di minuti delta non autorizzati nel giorno; sarà 0 se non è abilitata l'autorizzazione degli straordinari o il delta è negativo.</returns>
        private int GetUnautorizedDeltaMinutes(int dayNumber, int entityId, int? otherEntityId)
        {
            int unautorizedDeltaMinutes = 0;

            // calcolo del delta senza autorizzazione straordinari per il giorno in corso
            int dayDeltaMinutes = GetDeltaMinutesWithoutAutStr(dayNumber, entityId, otherEntityId);

            // si verifica se è usabile la configurazione dell'autorizzazione straordinari
            bool autStrUsable = IsAutStrUsable(otherEntityId, dayDeltaMinutes);

            if (autStrUsable)
            {
                // calcolo della data che si sta attualmente processando
                DateTime processingDate = GetDateFromIndex(dayNumber);

                if (processingDate != DateTime.MinValue)
                {
                    // si recupera per quanti minuti quel collaboratore risulta autorizzato agli straordinari in quella data
                    int autorizedMinutes = RepoManager.Aut_StrRepo.AutStrColAuthorization(entityId, processingDate) * 60;

                    // il numero dei minuti che sono non autorizzati sono la differenza tra il delta e l'autorizzazione, se positivo
                    unautorizedDeltaMinutes = dayDeltaMinutes - autorizedMinutes > 0 ? dayDeltaMinutes - autorizedMinutes : 0;
                }
            }

            return unautorizedDeltaMinutes;

        }

        /// <summary>
        /// Recupera per il giorno e le entità specificate il numero di minuti delta positivi autorizzati come straordinario.
        /// </summary>
        /// <param name="dayNumber">Il numero del giorno del mese da processare.</param>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare il processo.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca.</param>
        /// <returns>Il numero di minuti delta autorizzati nel giorno; sarà 0 se non è abilitata l'autorizzazione degli straordinari o il delta è negativo.</returns>
        private int GetAutorizedDeltaMinutes(int dayNumber, int entityId, int? otherEntityId)
        {
            int autorizedDeltaMinutes = 0;

            // calcolo del delta senza autorizzazione straordinari per il giorno in corso
            int dayDeltaMinutes = GetDeltaMinutesWithoutAutStr(dayNumber, entityId, otherEntityId);

            // si verifica se è usabile la configurazione dell'autorizzazione straordinari
            bool autStrUsable = IsAutStrUsable(otherEntityId, dayDeltaMinutes);

            if (autStrUsable)
            {
                // calcolo della data che si sta attualmente processando
                DateTime processingDate = GetDateFromIndex(dayNumber);
                if (processingDate != DateTime.MinValue)
                {
                    // si recupera per quanti minuti quel collaboratore risulta autorizzato agli straordinari in quella data
                    int autorizedMinutes = RepoManager.Aut_StrRepo.AutStrColAuthorization(entityId, processingDate) * 60;

                    // calcolo dell'autorizzazione giornaliera in base a se è presente più delta o più autorizzazione
                    if (autorizedMinutes < dayDeltaMinutes)
                        autorizedDeltaMinutes = autorizedMinutes;
                    else
                        autorizedDeltaMinutes = dayDeltaMinutes;
                }
            }

            return autorizedDeltaMinutes;
        }

        /// <summary>
        /// Recupera il numero di minuti di delta autorizzati per la settimana di cui è specificata la domenica.
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca..</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>Il numero di minuti di delta autorizzati nella settimana specificata.</returns>
        private int GetWeekAutorizedDeltaMinutes(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {
            int returnValue = 0;

            // si ritorna la somma dei delta autorizzati di tutta la settimana
            foreach (int weekDay in GetWeekDaysList(sundayDayNumber, lastMonthDay))
                returnValue += GetAutorizedDeltaMinutes(weekDay, entityId, otherEntityId);

            return returnValue;
        }

        /// <summary>
        /// Recupera il numero di minuti di delta non autorizzati per la settimana.
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca..</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>Il numero di minuti di delta non autorizzati nella settimana specificata.</returns>
        private int GetWeekUnautorizedDeltaMinutes(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {
            int returnValue = 0;

            // si ritorna la somma dei delta non autorizzati di tutta la settimana
            foreach (int weekDay in GetWeekDaysList(sundayDayNumber, lastMonthDay))
                returnValue += GetUnautorizedDeltaMinutes(weekDay, entityId, otherEntityId);

            return returnValue;
        }

        /// <summary>
        /// Recupera il numero di minuti di delta negativi nel corso della settimana.
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere per cui effettuare la ricerca.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca..</param>
        /// <param name="sundayDayNumber">Il numero di giorno della domenica che chiude la settimana di cui calcolare il totale.</param>
        /// <param name="lastMonthDay">Il numero dell'ultimo giorno del mese oggetto del cartellino.</param>
        /// <returns>Il numero di minuti di delta negativi incontrati nel corso della settimana.</returns>
        private int GetWeekDeltaNegativeMinutes(int entityId, int? otherEntityId, int sundayDayNumber, int lastMonthDay)
        {
            int returnValue = 0;

            // si ritorna la somma dei delta negativi presenti nel corso della settimana
            foreach (int weekDay in GetWeekDaysList(sundayDayNumber, lastMonthDay))
            {
                int deltaMinutes = GetDeltaMinutesWithoutAutStr(weekDay, entityId, otherEntityId);
                if (deltaMinutes < 0)
                    returnValue += Math.Abs(deltaMinutes);
            }

            return returnValue;
        }

        /// <summary>
        /// Determina se per gli specifici dati in processo è utilizzabile la gesione dell'autorizzazione straordinario.
        /// </summary>
        /// <param name="otherEntityId">L'identificativo dell'eventuale altra entità in processo.</param>
        /// <param name="deltaMinutes">The original delta minutes.</param>
        /// <returns><c>true</c> se l'autorizzazione straordinari è utilizzabile; altrimenti <c>false</c></returns>
        private bool IsAutStrUsable(int? otherEntityId, int deltaMinutes)
        {
            // di default l'autorizzazione straordinari non è utilizzabile
            bool isUsable = false;

            // si procede alla verifica dell'usabilità solamente se il modulo di gestione dell'autorizzazione straordinari è abilitato e
            // si sta processando un collaboratore con totale completo
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Aut_Str && otherEntityId == null && ReferenceEntity == ColEntityName)
            {
                // se la configurazione prevede l'utilizzo dell'autorizzazione straordinari e il delta è superiore a 0
                // allora la configurazione è usabile
                if (RepoManager.ParamRepo.ParametersRow.Aut_Str_Tipo.HasValue)
                    isUsable = RepoManager.ParamRepo.ParametersRow.Aut_Str_Tipo.Value == 1 && deltaMinutes > 0;
            }

            // ritorno del valore calcolato dal metodo
            return isUsable;
        }

        /// <summary>
        /// Ritorna il totale in minuti delle ore lavorate per il collaboratore o cantiere/giorno richiesto.
        /// </summary>
        /// <param name="keyValue">Il numero del giorno del mese di cui recuperare il numero di ore.</param>
        /// <param name="entityId">l'id del collaboratore/cantiere da ricercare all'interno dei timesheet item.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il numero di minuti delle ore lavorate per lo specifico collaboratore o cantiere/giorno.</returns>
        private double GetTotalMinutes(int keyValue, int entityId, int? otherEntityId)
        {
            // inizializzazione del valore di ritorno del metodo
            double totaleDiurno = 0;

            // recupero i timesheet del collaboratore/cantiere che compongono le ore lavorate,
            // cioé le ore che hanno come motivazione Ore figurative, ore diurne, ore notturne, ore viaggi, ore rettifica
            var tsCol = ReferenceEntity == ColEntityName
                ? TsmItems.Where(ts => ts.ColId == entityId && (otherEntityId == null || ts.CantId == Convert.ToInt32(otherEntityId)) && (ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT)
                  )).AsQueryable()
                : TsmItems.Where(ts => ts.CantId == entityId && (otherEntityId == null || ts.ColId == Convert.ToInt32(otherEntityId)) && (ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT)
                  )).AsQueryable();

            // se ci sono dei dati da processare    
            // sommo il totale delle ore calcolate (ore figurative)
            if (tsCol.Any())
            {
                var last = tsCol.Last();
                DateTime lastDate = last.StartDate;

                var lastMonth = tsCol.Where(ts => ts.StartDate == lastDate);

                // verifico la presenza della customizzazione riguardante la visualizzazione delle ore separate notturne/diurne
                int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowNocturnAndDayTimesheetEnum);

                //se la personalizzazione di inclusione delle ore notturne nel diurno è attivo
                if (customizationVersion == (int)IncludeNocturnInDayHours.Include)
                {
                    //vengono estratti tutti i cartellini tranne il cartellino che rappresenta le ore notturne
                    var cartellinoDiurno = lastMonth.Where(ts => ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE)).AsQueryable();
                    
                    //viene calcolato il totale delle ore per il giorno selezionato
                    totaleDiurno = cartellinoDiurno.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();

                }

                //se la personalizzaizone non è attiva
                else
                    totaleDiurno = lastMonth.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();

            }
            // ritorno del valore del metodo
            return totaleDiurno;
        }


        /// <summary>
        /// Ritorna il totale in minuti delle ore lavorate per il collaboratore o cantiere/giorno richiesto.
        /// </summary>
        /// <param name="keyValue">Il numero del giorno del mese di cui recuperare il numero di ore.</param>
        /// <param name="entityId">l'id del collaboratore/cantiere da ricercare all'interno dei timesheet item.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il numero di minuti delle ore lavorate per lo specifico collaboratore o cantiere/giorno.</returns>
        private double GetTotalMinutesNoArrot(int keyValue, int entityId, int? otherEntityId)
        {
            // inizializzazione del valore di ritorno del metodo
            double totaleDiurno = 0;


            // recupero i timesheet del collaboratore/cantiere che compongono le ore lavorate,
            // cioé le ore che hanno come motivazione Ore figurative, ore diurne, ore notturne, ore viaggi, ore rettifica
            var tsCol = ReferenceEntity == ColEntityName
                ? TsmItems.Where(ts => ts.ColId == entityId && (otherEntityId == null || ts.CantId == Convert.ToInt32(otherEntityId)) && (ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE)
                  )).AsQueryable()
                : TsmItems.Where(ts => ts.CantId == entityId && (otherEntityId == null || ts.ColId == Convert.ToInt32(otherEntityId)) && (ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE)
                  )).AsQueryable();

            // se ci sono dei dati da processare    
            // sommo il totale delle ore calcolate
            if (tsCol.Any())
            {
                var last = tsCol.Last();
                DateTime lastDate = last.StartDate;

                var lastMonth = tsCol.Where(ts => ts.StartDate == lastDate);

                // verifico la presenza della customizzazione riguardante la visualizzazione delle ore separate notturne/diurne
                int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowNocturnAndDayTimesheetEnum);

                //se la personalizzazione di inclusione delle ore notturne nel diurno è attivo
                if (customizationVersion == (int)IncludeNocturnInDayHours.Include)
                {
                    //vengono estratti tutti i cartellini tranne il cartellino che rappresenta le ore notturne
                    var cartellinoDiurno = lastMonth.Where(ts => ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE)).AsQueryable();

                    //viene calcolato il totale delle ore per il giorno selezionato
                    totaleDiurno = cartellinoDiurno.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();

                }

                //se la personalizzaizone non è attiva
                else
                    totaleDiurno = lastMonth.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();


                totaleDiurno = lastMonth.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();

            }
                

            // ritorno del valore del metodo
            return totaleDiurno;
        }


        /// <summary>
        /// Recupera il totale dei minuti per il giorno specificato per l'entità speicficata.
        /// </summary>
        /// <param name="keyValue">Il numero del giorno del mese di cui recuperare il numero di ore.</param>
        /// <param name="colId">L'id del collaboratore da ricercare all'interno dei timesheet item.</param>
        /// <returns>Il numero di minuti delle ore lavorate per lo specifico collaboratore utilizzato per il monte minuti.</returns>
        private double GetTotaleMinutesForLastMonthAmmount(int keyValue, int colId, DateTime from, DateTime to)
        {
            // inizializzazione del valore di ritorno del metodo
            double value = 0;

            

            // recupero i timesheet del collaboratore che compongono le ore lavorate,
            // cioé le ore non  hanno come motivazione ore piano o ore non lavorate
            var tsCol = TsmItems.Where(ts => ts.ColId == colId && ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ONL)
                  ).AsQueryable();

            // se ci sono dei dati da processare    
            // sommo il totale delle ore calcolate
            if (tsCol.Any())
            {
                var last = tsCol.Where(ts => ts.StartDate == to.AddDays(1).AddMonths(-1)).ToList();
                value = last.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();
            }
            // ritorno del valore del metodo
            return value;
        }

        /// <summary>
        /// Ritorna il totale in minuti delle ore previste per il collaboratore o cantiere/giorno richiesto.
        /// </summary>
        /// <param name="keyValue">Il numero del giorno del mese di cui recuperare il numero di ore.</param>
        /// <param name="entityId">l'id del collaboratore/cantiere da ricercare all'interno dei timesheet item.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il numero di minuti delle ore previste per lo specifico collaboratore o cantiere/giorno.</returns>
        private double GetTotalPlanMinutes(int keyValue, int entityId, int? otherEntityId)
        {
            // inizializzazione del valore di ritorno del metodo
            double value = 0;
            

            // recupero i timesheet del collaboratore/cantiere che compongono le previste,
            // cioé le ore che hanno come motivazione Piano
            var tsCol = ReferenceEntity == ColEntityName
                ? TsmItems.Where(ts => ts.ColId == entityId && (otherEntityId == null || ts.CantId == Convert.ToInt32(otherEntityId)) && (ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN))).AsQueryable()
                : TsmItems.Where(ts => ts.CantId == entityId && (otherEntityId == null || ts.ColId == Convert.ToInt32(otherEntityId)) && (ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN))).AsQueryable();

            // se ci sono dei dati da processare    
            // sommo il totale delle ore calcolate
            if (tsCol.Any())
            {
                var last = tsCol.Last();
                DateTime lastDate = last.StartDate;

                var lastMonth = tsCol.Where(ts => ts.StartDate == lastDate);
                value = lastMonth.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();

            }
          

            // ritorno del valore del metodo
            return value;
        }

        /// <summary>
        /// Ritorna il totale in minuti delle ore diurne effettuate per il collaboratore o cantiere/giorno richiesto.
        /// </summary>
        /// <param name="keyValue">Il numero del giorno del mese di cui recuperare il numero di ore.</param>
        /// <param name="entityId">l'id del collaboratore/cantiere da ricercare all'interno dei timesheet item.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il numero di minuti delle ore diurne lavorate per lo specifico collaboratore o cantiere/giorno.</returns>
        private double GetTotalDayMinutes(int keyValue, int entityId, int? otherEntityId)
        {
            // inizializzazione del valore di ritorno del metodo
            double totaelDiurno = 0;
            double totaleNotturno = 0;

            // recupero i timesheet del collaboratore che compongono le ore lavorate diurne,
            // cioé le ore che hanno come motivazione Ore figurative, ore diurne, viaggi, rettifiche
            var tsCol = ReferenceEntity == ColEntityName
                ? TsmItems.Where(ts => ts.ColId == entityId && (otherEntityId == null || ts.CantId == Convert.ToInt32(otherEntityId)) && (ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE)||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE)
                  )).AsQueryable()
                : TsmItems.Where(ts => ts.CantId == entityId && (otherEntityId == null || ts.ColId == Convert.ToInt32(otherEntityId)) && (ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI) ||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE)||
                  ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE)
                  )).AsQueryable();


            // se ci sono dei dati da processare    
            // sommo il totale delle ore calcolate
            if (tsCol.Any())
            {

                var last = tsCol.Last();
                DateTime lastDate = last.StartDate;

                var lastMonth = tsCol.Where(ts => ts.StartDate == lastDate);

                // verifico la presenza della customizzazione riguardante la visualizzazione delle ore separate notturne/diurne
                int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowNocturnAndDayTimesheetEnum);

                //se è attiva la personalizzazione dell'inclusione delle ore notturne nel totale delle ore lavoarate
                if (customizationVersion == (int)IncludeNocturnInDayHours.Include)
                {
                    //vengono estratti tutti i cartellini tranne i notturni
                    var cartellinoDiurno= lastMonth.Where(ts => ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE)).AsQueryable();

                    //viene estratto il cartellino del notturno
                    var cartellinoNotturno = lastMonth.Where(ts => ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE)).AsQueryable();

                    //viene calcolato il totale delle ore (notturne + diurne)
                    totaelDiurno= cartellinoDiurno.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();

                    //viene estratto il totale delle ore notturne
                    totaleNotturno = cartellinoNotturno.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();

                    //viene calcolato il torale delle ore diurne
                    totaelDiurno -= totaleNotturno;

                }
               
                else
                totaelDiurno = lastMonth.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();
                                
            }

            // ritorno del valore del metodo
            return totaelDiurno;
        }

        /// <summary>
        /// Ritorna il totale in minuti delle ore notturne effettuate per il collaboratore o cantiere/giorno richiesto.
        /// </summary>
        /// <param name="keyValue">Il numero del giorno del mese di cui recuperare il numero di ore.</param>
        /// <param name="colId">l'id del collaboratore/cantiere da ricercare all'interno dei timesheet item.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il numero di minuti delle ore notturne effettuate per lo specifico collaboratore o cantiere/giorno.</returns>
        private double GetTotalNocturnMinutes(int keyValue, int colId, int? otherEntityId)
        {
            // inizializzazione del valore di ritorno del metodo
            double value = 0;

            // recupero i timesheet del collaboratore/cantiere che compongono le ore lavorate diurne,
            // cioé le ore che hanno come motivazione Ore figurative, ore diurne, viaggi, rettifiche
            var tsCol = ReferenceEntity == ColEntityName
            ? TsmItems.Where(ts => ts.ColId == colId && (otherEntityId == null || ts.CantId == Convert.ToInt32(otherEntityId)) && ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE)).AsQueryable()
            : TsmItems.Where(ts => ts.CantId == colId && (otherEntityId == null || ts.ColId == Convert.ToInt32(otherEntityId)) && ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE)).AsQueryable();

            // se ci sono dei dati da processare    
            // sommo il totale delle ore calcolate
            if (tsCol.Any())
            {

                var last = tsCol.Last();
                DateTime lastDate = last.StartDate;

                var lastMonth = tsCol.Where(ts => ts.StartDate == lastDate);
                value = lastMonth.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();

            }
            // ritorno del valore del metodo
            return value;
        }

        /// <summary>
        /// Ritorna il totale in minuti delle ore con motivazione (definite in reg_v) per il collaboratore o cantiere/giorno richiesto.
        /// </summary>
        /// <param name="keyValue">Il numero del giorno del mese di cui recuperare il numero di ore.</param>
        /// <param name="entityId">l'id del collaboratore/cantiere da ricercare all'interno dei timesheet item.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il numero di minuti delle ore piano per lo specifico collaboratore o cantiere/giorno.</returns>
        private double GetJustificationMinutes(int keyValue, int entityId, int? otherEntityId)
        {
            // inizializzazione del valore di ritorno del metodo
            double value = 0;

            // recupero i timesheet del collaboratore/cantiere che compongono le ore con motivazione,
            // cioé le ore che non hanno come motivazione ore figurative, ore diurne, ore notturne, viaggi, rettifiche, piano
            var tsCol = ReferenceEntity == ColEntityName
                ? TsmItems.Where(ts => ts.ColId == entityId && (otherEntityId == null || ts.CantId == Convert.ToInt32(otherEntityId)) && (ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ONL) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT)
                  )).AsQueryable()
                : TsmItems.Where(ts => ts.CantId == entityId && (otherEntityId == null || ts.ColId == Convert.ToInt32(otherEntityId)) && (ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_DIURNE) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_NOTTURNE) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ONL) &&
                  ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT)
                  )).AsQueryable();

            // se ci sono dei dati da processare    
            // sommo il totale delle ore calcolate
            if (tsCol.Any())
            {
                var last = tsCol.Last();
                DateTime lastDate = last.StartDate;

                var lastMonth = tsCol.Where(ts => ts.StartDate == lastDate);
                value = lastMonth.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();

            }

            // ritorno del valore del metodo
            return value;
        }

        /// <summary>
        /// Ritorna il totale in minuti degli arrotondamenti per il collaboratore e giorno richiesto.
        /// </summary>
        /// <param name="keyValue">Il numero del giorno del mese di cui recuperare il numero di ore.</param>
        /// <param name="entityId">l'id del collaboratore/cantiere da ricercare all'interno dei timesheet item.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/cantiere per cui effettuare la sottoricerca</param>
        /// <returns>Il totale in minuti degli arrotondamenti per il collaboratore e giorno richiesto.</returns>
        private double GetArrotMinutes(int keyValue, int entityId, int? otherEntityId)
        {
            //Inizializzo il valore di ritorno a 0
            double value = 0d;

            //Controllo di aver abilitato gli arrotondamenti per durata
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Arrotondamenti && (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento == RoundingMethodEnum.Duration)
            {
                //Recupero i cartellini con gli arrotondamenti per durata
                var tsCol = ReferenceEntity == ColEntityName
                ? TsmItems.Where(ts => ts.ColId == entityId && (otherEntityId == null || ts.CantId == Convert.ToInt32(otherEntityId)) && (ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT)
                  )).AsQueryable()
                : null;

                //Se ci sono cartellini, va a calcolare gli arrotondamenti
                if (tsCol != null && tsCol.Any())
                {
                    var last = tsCol.Last();
                    DateTime lastDate = last.StartDate;

                    var lastMonth = tsCol.Where(ts => ts.StartDate == lastDate);
                    value = lastMonth.Select(ts => ts.DaysHours.ContainsKey(keyValue) ? ts.DaysHours[keyValue].Item1 : 0d).Sum();

                }
            }
            return value;
        }

        /// <summary>
        /// Ritorna il totale del monte minuti mesi precedenti per il collaboratore/cantiere/giorno richiesto.
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere da ricercare all'interno dei timesheet item.</param>
        /// <param name="otherEntityId">L'id dell'entità opposta al collaboratore/canteiere per cui effettuare la sottoricerca.</param>
        /// <returns>Il numero di minuti accumulato nei mesi precedenti e salvato negli elementi cartellino (la somma di quelli trovati)</returns>
        private double GetLastMinutesAmmount(int entityId, int? otherEntityId)
        {
            // inizializzazione del valore di ritorno del metodo
            double value = 0;

            // recupero i timesheet del collaboratore/cantiere che compongono le ore con motivazione,
            // cioé le ore che non hanno come motivazione ore figurative, ore diurne, ore notturne, viaggi, rettifiche, piano
            var tsCol = ReferenceEntity == ColEntityName
                ? TsmItems.Where(ts => ts.ColId == entityId && (otherEntityId == null || ts.CantId == Convert.ToInt32(otherEntityId)) &&
                    Math.Abs(ts.LastMonthlyMinutes) > 0
                    ).AsQueryable()
                : TsmItems.Where(ts => ts.CantId == entityId && (otherEntityId == null || ts.ColId == Convert.ToInt32(otherEntityId)) &&
                    Math.Abs(ts.LastMonthlyMinutes) > 0
                    ).AsQueryable();

            // se ci sono dei dati da processare    
            // sommo il totale delle ore calcolate
            if (tsCol.Any())
            {
                
                  value = tsCol.Select(ts => ts.LastMonthlyMinutes).Sum();
            }
            // ritorno del valore del metodo
            return value;
        }

        /// <summary>
        /// Recupera la data rappresentata dall'indice giorno specificato.
        /// </summary>
        /// <param name="dayNumber">L'indice giorno da processare.</param>
        /// <returns>La data rappresentata dall'indice giorno</returns>
        private DateTime GetDateFromIndex(int dayNumber)
        {
            // di default si ritorna il min value
            DateTime returnDate = DateTime.MinValue;

            // si procede alla verifica solamente se è presente una start date processabile
            DateTime startDate = LastTimeShettStartDate;
            if (startDate != DateTime.MinValue)
            {
                // se l'indice è maggiore di 100 allora si tratta dei giorni del mese successivo
                if (dayNumber > 100)
                    returnDate = CommonService.GetFirstMonthDay(startDate.AddMonths(1)).AddDays(dayNumber - 101);
                else if (dayNumber < 0) // se l'indice è minore di 0 allora si sta trattando dei giorni del mese precedente
                    returnDate = CommonService.GetFirstMonthDay(startDate).AddDays(dayNumber);
                else if (dayNumber <= CommonService.GetLastMonthDay(startDate).Day)  // se l'indice invece è in normalità si tratta di un giorno del mese in processo
                    returnDate = new DateTime(startDate.Year, startDate.Month, dayNumber);
            }

            // ritorno del valore calcolato dal metodo
            return returnDate;
        }

        #endregion

    }
}
