using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Data;
using DevExpress.XtraPrinting.Native;
using Domain;
using System.Text;
using Common;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
    public class Tab_OrariRepository : GenericRepository<Tab_Orari>, ITab_OrariRepository
    {
        public Tab_OrariRepository(PowerWebEntities context)
            : base(context)
        {
        }


        #region Private Constants

        /// <summary>
        /// Il nome entità dei collaboratori utilizzata per le ricerche cantiere/collaboratore
        /// </summary>
        private const string ColEntityName = "Col";

        /// <summary>
        /// Il nome entità dei cantieri utilizzata per le ricerche cantiere/collaboratore
        /// </summary>
        private const string CantEntityName = "Can";

        /// <summary>
        /// La chiave per il cartellino diurno
        /// </summary>
        private const string DayTimesheetKey = "Diurno";

        /// <summary>
        /// La chiave per il cartellino notturno
        /// </summary>
        private const string NightTimesheetKey = "Notturno";

        #endregion

        /// <summary>Ottiene le tabelle che deve usare in formato lista.
        /// Per ottimizzare la velocità salva l'oggetto nella sessione corrente
        /// in modo che la lista sia già in memoria quando viene richiesta più volte.
        /// </summary>
        private static List<Cant> Cants
        {
            get
            {

                List<Cant> oLista = PowerWebContext.GetFromSession<List<Cant>>("Cant_TabOrariRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.CantRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cant>>("Cant_TabOrariRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Col> Cols
        {
            get
            {
                List<Col> oLista = PowerWebContext.GetFromSession<List<Col>>("Cant_TabOrariRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.ColRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Col>>("Col_TabOrariRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Tab_Orari_Tipo> Tab_Orari_Tipos
        {
            get
            {

                List<Tab_Orari_Tipo> oLista = PowerWebContext.GetFromSession<List<Tab_Orari_Tipo>>("Tab_Orari_Tipo_TabOrariRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_OrariTipoRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Orari_Tipo>>("Tab_Orari_Tipo_tabOrariRepo", oLista);
                }
                return oLista;
            }
        }

        private static void ResetSession()
        {
        }

        public override Tab_Orari Init()
        {
            Tab_Orari oNewRecord = base.Init();
            return oNewRecord;
        }

        public override Dictionary<string, string> Check(Tab_Orari entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession();
            try
            {
                //  
                //NON ESEGUO il Controllo sulla DataOraUltimaModifica perchè questa tabella NON ha questa informazione
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //            
                if (CommonService.Nz(entity.Tab_Orari_Tipo_Id, 0) != 0 &&
                    CommonService.Nz(entity.Data_Inizio, new DateTime(1, 1, 1)) != new DateTime(1, 1, 1) &&
                    CommonService.Nz(entity.Ora_E, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00) &&
                    CommonService.Nz(entity.Ora_U, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00) &&
                    CommonService.Nz(entity.Durata_Minuti, 0) != 0 &&
                    CommonService.Nz(entity.G1, false) != false &&
                    CommonService.Nz(entity.G2, false) != false &&
                    CommonService.Nz(entity.G3, false) != false &&
                    CommonService.Nz(entity.G4, false) != false &&
                    CommonService.Nz(entity.G5, false) != false &&
                    CommonService.Nz(entity.G6, false) != false &&
                    CommonService.Nz(entity.G7, false) != false)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Id),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_DATI_NECESSARI_MANCANTI));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.Tab_OrariRepo.SingleOrDefault(u => u.Tab_Orari_Tipo_Id == entity.Tab_Orari_Tipo_Id &&
                            u.Data_Inizio == entity.Data_Inizio &&
                            u.Ora_E == entity.Ora_E &&
                            u.Ora_U == entity.Ora_U &&
                            u.Cant_Id == entity.Cant_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Id),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.Tab_OrariRepo.SingleOrDefault(u => u.Tab_Orari_Tipo_Id == entity.Tab_Orari_Tipo_Id &&
                            u.Tab_Orari_Id != entity.Tab_Orari_Id &&
                            u.Data_Inizio == entity.Data_Inizio &&
                            u.Ora_E == entity.Ora_E &&
                            u.Ora_U == entity.Ora_U &&
                            u.Cant_Id == entity.Cant_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Id),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //
                if (CommonService.Nz(entity.Tab_Orari_Tipo_Id, 0) == 0)
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Id),
                     BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                     PowerWebResources.FLD_TAB_ORARI_TIPO_ID));
                }
                else
                {
                    if (Tab_Orari_Tipos.SingleOrDefault(u => u.Tab_Orari_Tipo_Id == entity.Tab_Orari_Tipo_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Id),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                            PowerWebResources.FLD_TAB_ORARI_TIPO_ID, PowerWebResources.STR_TAB_ORARI_TIPO));
                }
                if (CommonService.Nz(entity.Data_Inizio, new DateTime(1, 1, 1)) == new DateTime(1, 1, 1))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Inizio),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_DATA_INIZIO));
                // si procede alla verifica di obbligatorietà dell'ora d'entrata e di uscita solamente se
                // il valore della durata è ancora a 0
                if (CommonService.Nz(entity.Durata_Minuti, 0) == 0)
                {
                    if (CommonService.Nz(entity.Ora_E, new TimeSpan(00, 00, 00)) == new TimeSpan(00, 00, 00))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Ora_E),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                          PowerWebResources.FLD_ORA_E));
                    if (CommonService.Nz(entity.Ora_U, new TimeSpan(00, 00, 00)) == new TimeSpan(00, 00, 00))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Ora_U),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                          PowerWebResources.FLD_ORA_U));
                }
               
                if (CommonService.Nz(entity.G1, false) == false &&
                    CommonService.Nz(entity.G2, false) == false &&
                    CommonService.Nz(entity.G3, false) == false &&
                    CommonService.Nz(entity.G4, false) == false &&
                    CommonService.Nz(entity.G5, false) == false &&
                    CommonService.Nz(entity.G6, false) == false &&
                    CommonService.Nz(entity.G7, false) == false &&
                    CommonService.Nz(entity.Orario_Mensile, false) == false)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.G1),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_DATI_NECESSARI_MANCANTI,
                      PowerWebResources.FLD_G1));
                else
                if (CommonService.Nz(entity.Ripetizione, 0) == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Ripetizione),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                      PowerWebResources.FLD_RIPETIZIONE, PowerWebResources.VALORE_1));
                //
                //3) verifico, per una serie di campi, che il valore del campo sia corretto
                //               
                // si effettua il controllo sulle date solamente se i record non sono null

                // inizializzazione delle configurazioni del presenti nella scheda parametri
                bool nocturneModuleActive = RepoManager.ParamRepo.ParametersRow.Abilita_Notturno;
                // tipo notturno
                NocturneTypeEnum nocturneTypeParam = nocturneModuleActive ? (NocturneTypeEnum)RepoManager.ParamRepo.ParametersRow.TipoNotturno : NocturneTypeEnum.None;
                if (entity.Ora_E != null && entity.Ora_U != null)

                    //se non è abilitato il notturno viene controllato che l'ora di uscita sia maggiore di quella dell'entrata
                    if (nocturneTypeParam != NocturneTypeEnum.OverMidnight)
                    {
                        if (CommonService.Nz(entity.Ora_E, new TimeSpan(00, 00, 00)) >= CommonService.Nz(entity.Ora_U, new TimeSpan(00, 00, 00)))
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Ora_E),
                                  BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                                  PowerWebResources.FLD_ORA_E, PowerWebResources.FLD_ORA_U));
                    }

                //Verifico che se si sta MODIFICANDO un Tipo Orario associato ad un Collaboratore per il quale è ATTIVATA la Gestione Monte Ore
                //ALLORA NON é POSSIBILE MODIFICARLO se ha una DATA INIZIO < ALLA DATA BLOCCO
                //Solo se è attiva la Gestione Monte Ore in PARAM
                if (RepoManager.ParamRepo.ParametersRow.MonthlyHoursEnum != MothlyHoursEnum.None)
                {
                    var colOrario = new List<Col>();
                    //Leggo i Collaboratori che hanno quel Tipo Orario
                    colOrario = RepoManager.ColRepo.Find(c => c.Tab_Orari_Tipo_Id == entity.Tab_Orari_Tipo_Id).ToList();
                    if (colOrario.Count == 0)
                        //Se ci sono Collaboratori con quel Tipo Orario La Data Inizio dell'Orario deve essere MAGGIORE Della DATA BLOCCO
                        if (RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.HasValue &&
                            entity.Data_Inizio <= RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Inizio),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_INIZIO_ORARIO_DEVE_ESSERE_MAGGIORE_DELLA_DATA_BLOCCO));
                }

                // è possibile inserire date di inzio dell'orario soalmente se corrispondono con un lunedì
                if (entity.Data_Inizio.DayOfWeek != DayOfWeek.Monday)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Inizio), BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_INIZIO_DEVE_ESSERE_LUNEDI));

                //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);
            }
            catch (Exception ex)
            {
                var CodErr = "Tab_Orari_Id: " + entity.Tab_Orari_Id;
                throw ex;
            }
            return result;
        }

        public bool IsToApplyTimesheet(Tab_Orari timesheet, DateTime i)
        {
            // sposto avanti la data di inzio in base alla sequenza di modo da poter
            // gestire le alternanze rispetto alla data di inzio stessa
            DateTime newDtInizio = timesheet.Data_Inizio.AddDays(timesheet.Sequenza * 7); ;
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportStr) == 1) {
                newDtInizio = timesheet.Data_Inizio.AddDays(timesheet.Sequenza * 1);
            }
            TimeSpan delta = i - newDtInizio;
            return ((delta.Days / 7) % timesheet.Ripetizione) == 0 && delta.Days >= 0;
        }

        #region Gestione recupero informazioni orari per periodo di tempo

        /// <summary>
        /// Per il collaboratore e l'intervallo di date specificato questo metodo si occupa di ricercare all'interno
        /// della tab orari quanto configurato e ritorna un elenco di date in cui, per ogni data, è specificato il numero di ore previsto.
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere da ricercare.</param>
        /// <param name="startDate">La data di partenza per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="endDate">La data di termine per la costruzuoione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="dateStartCol">La data di inizio disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="dateEndCol">la data di fine disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="isFromFreeTimesheet">Ritorna <c>true</c> se l'orario è stato calcolato a partire da un orario preso da col_orario; altrimenti <c>false</c></param>
        /// <param name="freeTimesheetId">Ritorna l'eventuale id della tabella col_orario da cui è sono state recuperate le ore</param>
        /// <param name="isByOtherEntity"><c>true</c> se deve essere effettuata la divisione degli orari per cantiere/collaboratore, altrimenti <c>false</c></param>
        /// <param name="referenceEntity">L'entità di riferimento per la generazione del timesheet; se non specificata si tratta il cartellino per collaboratore.</param>
        /// <param name="requestedForWeeklyTotals">Indica che il piano è richiesto per un calcolo che prevede i totali settimanali.</param>
        /// <returns>
        /// Una dizionario la cui chiave è l'id cantiere di riferimento per l'orario e il valore è un altro dizionario con chiave la data dell'intervallo e come valore una tuple i cui valori rappresentano:
        /// il numero di previsto per quel collaboratore per quella giornata; l'ora di inzio del notturno (se previsto, altrimenti null); l'ora di fine del notturno (se previsto, altrimenti null);
        /// in caso di problemi nel calcolo (periodo errato o dati non presenti, viene restituito un dizionario vuoto).
        /// </returns>
        public Dictionary<int, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> GetPlanMinutes(int entityId, DateTime startDate, DateTime endDate, DateTime? dateStartCol, DateTime? dateEndCol,
            out bool isFromFreeTimesheet, out int freeTimesheetId, bool isByOtherEntity, string referenceEntity = "Col", bool requestedForWeeklyTotals = false)
        {
            // inizializzazione del valore di ritorno del metodo
            var returnDictionary = new Dictionary<int, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>>();

            // recupero dell'id della tab orari tipo a partire dall'id collaboratore/cantiere passato come parametro
            int tabOrariTipoId = 0;
            tabOrariTipoId = GetTabOrariTipoIdFromEntity(entityId, referenceEntity);

            /* se è richiesto di costruire i dati per un orario con totali settimanali si verifica se si sta processando
            * l'inizio e la fine del mese; in questo caso, se necessario si aggiornano le date per comprendere l'inizio e la fine della settimana
            * del mese precedente e successivo */

            // se è richiesto il piano per la gestione di orari settimanali, la data di inizio è l'inizio del mese e la data di inizio non è un lunedì
            // allora si modifica la data di inizio periodo l'ultimo lunedì del mese precedente
            if (requestedForWeeklyTotals && startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);

            // se è richiesto il piano per la gestione degli orari settimanali, la data di fine è la fine del mese e non si tratta di una domenica
            // allora si recupera la data di fine periodo la prima domenica del mese successivo
            if (requestedForWeeklyTotals && endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
            {
                endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                // la data di fine viene portata alle 23:59 così da recuperare anche le timbrature della giornata di fine
                endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);
            }

            // calcolo dell'elenco di date del periodo
            List<DateTime> periodDates = CommonService.GetDatesFromPeriod(startDate, endDate);

            #region Calcolo delle date di validità di periodo in base alle date di disponibilità del collaboratore

            // le date di inizio e fine validità hanno senso solamente se si sta processando un piano per collaboratore
            Tuple<DateTime, DateTime> newValidDates = FilterPeriodWithColDispDates(startDate, endDate, dateStartCol, dateEndCol);
            DateTime startValidDate = new DateTime();
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportStr) == 1)
            {
                startValidDate = startDate;
            }
            else {
                startValidDate = referenceEntity == ColEntityName ? newValidDates.Item1 : startDate;
            }
            DateTime endValidDate = referenceEntity == ColEntityName ? newValidDates.Item2 : endDate;

            #endregion

            // inizializzazione dei valori di ritorno riguardo all'utilizzo del Col_Orario
            isFromFreeTimesheet = false;
            freeTimesheetId = 0;

            // se è stato generato un periodo di date valido
            if (periodDates.Any())
            {
                // inizializzazione delle variaibli utilizzate per la gestione dell'orario notturno
                TimeSpan? nocturnInitHour = null;
                TimeSpan? nocturnEndHour = null;

                // se non si deve elaborare per cantiere o se il collaboratore non ha un tipo orario collegato, allora si ritorna la somma globale
                if (!isByOtherEntity || tabOrariTipoId == 0)
                {
                    // inizializzazione del dizionario che si sta generando
                    var timesheetDictionary = new Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>();

                    #region Generazione degli orari generali (senza divisione per cantiere)

                    // dal tipo orario calcolato si recuperano le eventuali date di inizio e fine notturno (solo se il tipo orario risulta presente)
                    if (tabOrariTipoId != 0)
                    {
                        var tipoOrario = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(to => to.Tab_Orari_Tipo_Id == tabOrariTipoId);
                        nocturnInitHour = tipoOrario.Tab_Orari_Tipo_Inizio_Not;
                        nocturnEndHour = tipoOrario.Tab_Orari_Tipo_Fine_Not;
                    }
                    else
                    {
                        // se il calcolo non viene effettuato da una tabella orari allora
                        // sicuramente si recuperano i dati dalla tabella Col_Orario
                        isFromFreeTimesheet = true;
                    }

                    // inserimento del periodo di date all'interno del dizionario
                    periodDates.ForEach(pDate => timesheetDictionary.Add(pDate, new Tuple<double, TimeSpan?, TimeSpan?>(0, null, null)));

                    // per ogni data da elaborare
                    foreach (var pDate in periodDates)
                    {
                        // per la data in elaborazione si recupera l'elenco degli orari
                        var validTimeSheets = GetDatePlanDetail(pDate, tabOrariTipoId, entityId, referenceEntity);

                        // viene calcolata la durata totale di tutti i timesheet calcolati (a partire dall'Tab_Orari se è stato inserito o altrimenti dal Col_Orario);
                        // si inserisce una durata che non sia 0 solamente se la data attualmente in elaborazione è compresa nel periodo di validità del collaboratore
                        double dayDuration = 0;

                        bool nocturneModuleActive = RepoManager.ParamRepo.ParametersRow.Abilita_Notturno;
                        // tipo notturno
                        NocturneTypeEnum nocturneTypeParam = nocturneModuleActive ? (NocturneTypeEnum)RepoManager.ParamRepo.ParametersRow.TipoNotturno : NocturneTypeEnum.None;

                        TimeSpan almostMidnight = new TimeSpan(23, 59, 0);
                        TimeSpan midnight = new TimeSpan(0, 0, 0);


                        if (pDate >= startValidDate && pDate < endValidDate)
                        {
                            if (tabOrariTipoId != 0)
                            {
                                if (nocturneTypeParam == NocturneTypeEnum.None || nocturneTypeParam == NocturneTypeEnum.Disabled)
                                {
                                    dayDuration = validTimeSheets.Select(ts => ts.Ora_U != null && ts.Ora_E != null ? (ts.Ora_U.Value.Subtract(ts.Ora_E.Value).TotalMinutes) : Convert.ToDouble(ts.Durata_Minuti)).Sum();
                                }

                                else
                                {

                                    //TRADUZIONE BLOCCO DI CODICE: Se i campi entrata uscita dell'orario sono diversi da null 
                                                                    //allora se l'entrata è maggiore dell'uscita(orario notturno)
                                                                                //allora determino l'orario del giorno con il calcolo fino a mezzanotte e da mezzanotte in poi
                                                                                //altrimenti la durata della giornata è determinata come differenza tra uscita ed entrata
                                                                    //altrimenti se lentrata e l'uscita sono null
                                                                                //allora la durata è data la durata del piano orario
                                     dayDuration = validTimeSheets.Select(ts =>

                                        ts.Ora_E != null && ts.Ora_U != null ?
                                        ts.Ora_E.Value > ts.Ora_U.Value ?
                                        ((almostMidnight.Subtract(ts.Ora_E.Value)).TotalMinutes + 1) + (ts.Ora_U.Value.Subtract(midnight).TotalMinutes) :
                                        ts.Ora_U.Value.Subtract(ts.Ora_E.Value).TotalMinutes:
                                        Convert.ToDouble(ts.Durata_Minuti)
                                        ).Sum();
                                }
                            }

                            else
                            {
                                dayDuration = RepoManager.ColCantOrarioRepo.GetPlannedMinutesFromTimesheet(pDate, entityId, referenceEntity, out freeTimesheetId);
                            }
                        }

                        // inserimento della durata prevista all'interno del dizionario di ritorno alla data
                        // in elaborazione
                        timesheetDictionary[pDate] = new Tuple<double, TimeSpan?, TimeSpan?>(dayDuration, nocturnInitHour, nocturnEndHour);
                    }

                    // si aggiunge l'orario calcolato alla lista di ritorno
                    returnDictionary.Add(0, timesheetDictionary);

                    #endregion
                }
                else // se si fa la divisione del cantiere e c'è un tipo orario collegato
                {
                    #region gestione degli orari suddivisi per cantiere/collaboratore a seconda dell'entità

                    // sicuramente l'orario non è stato generato a partire dalla tabella Col_Orari
                    freeTimesheetId = 0;

                    // per ogni data in elaborazione
                    foreach (var pDate in periodDates)
                    {
                        // recupero tutti gli orari validi per cantiere
                        var validTimeSheets = GetDatePlanDetail(pDate, tabOrariTipoId, entityId, referenceEntity);

                        // ciclo su tutti gli orari validi per la data suddivisa per cantiere o collaboratore a seconda dell'entità
                        var tabOraris = validTimeSheets as IList<Tab_Orari> ?? validTimeSheets.ToList();
                        foreach (var otherEntityId in referenceEntity == ColEntityName ? tabOraris.Select(ts => ts.Cant_Id).Distinct().ToList() : tabOraris.Select(ts => ts.Col_Id).Distinct().ToList())
                        {
                            // preparo il cantId/colId a 0 in caso il valore sia null
                            int currentOtherEntityId = otherEntityId ?? 0;

                            // se il cantiere non è già presente nel dizionario di rientro allora lo aggiungo
                            if (!returnDictionary.ContainsKey(currentOtherEntityId))
                                returnDictionary.Add(currentOtherEntityId, new Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>());

                            // per il tipo orario attualmente in elaborazione si recupera l'orario di inizio e fine notturno
                            var tipoOrario = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(to => to.Tab_Orari_Tipo_Id == tabOrariTipoId);
                            nocturnInitHour = tipoOrario.Tab_Orari_Tipo_Inizio_Not;
                            nocturnEndHour = tipoOrario.Tab_Orari_Tipo_Fine_Not;

                            // se per il cantiere in elaborazione non esiste la data che si sta processando
                            // allora si provvede a generare la corrispettiva entry nel dizionario
                            if (!returnDictionary[currentOtherEntityId].ContainsKey(pDate))
                                returnDictionary[currentOtherEntityId].Add(pDate, new Tuple<double, TimeSpan?, TimeSpan?>(0, nocturnInitHour, nocturnEndHour));

                            // calcolo il totale della giornata solamente se la data in elaborazione è
                            // una data in cui il collaboratore risulta disponibile
                            double dayDuration = 0;
                            if (pDate >= startValidDate && pDate <= endValidDate)
                                dayDuration = tabOraris.Where(ts =>
                                {
                                    bool retVal;
                                    if (referenceEntity == ColEntityName)
                                        retVal = ts.Cant_Id == otherEntityId;
                                    else
                                        retVal = ts.Col_Id == otherEntityId;
                                    return retVal;
                                }).Select(ts => ts.Ora_U != null && ts.Ora_E != null ? (ts.Ora_U.Value.Subtract(ts.Ora_E.Value).TotalMinutes) : Convert.ToDouble(ts.Durata_Minuti)).Sum();

                            // inserisco la durata calcolata all'interno del dizionario di ritorno
                            returnDictionary[currentOtherEntityId][pDate] = new Tuple<double, TimeSpan?, TimeSpan?>(dayDuration, nocturnInitHour, nocturnEndHour);
                        }
                    }

                    #endregion
                }
            }

            // ritorno del valore del metodo
             return returnDictionary;
        }


        /// <summary>
        /// Per il collaboratore e l'intervallo di date specificato questo metodo si occupa di ricercare all'interno
        /// della tab orari quanto configurato e ritorna un elenco di date in cui, per ogni data, è specificato il numero di ore previsto.
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere da ricercare.</param>
        /// <param name="startDate">La data di partenza per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="endDate">La data di termine per la costruzuoione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="dateStartCol">La data di inizio disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="dateEndCol">la data di fine disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="isFromFreeTimesheet">Ritorna <c>true</c> se l'orario è stato calcolato a partire da un orario preso da col_orario; altrimenti <c>false</c></param>
        /// <param name="freeTimesheetId">Ritorna l'eventuale id della tabella col_orario da cui è sono state recuperate le ore</param>
        /// <param name="referenceEntity">L'entità di riferimento per la generazione del timesheet; se non specificata si tratta il cartellino per collaboratore.</param>
        /// <returns>
        /// Una dizionario la cui chiave è l'id cantiere di riferimento per l'orario e il valore è un altro dizionario con chiave la data dell'intervallo e come valore una tuple i cui valori rappresentano:
        /// il numero di previsto per quel collaboratore per quella giornata; l'ora di inzio del notturno (se previsto, altrimenti null); l'ora di fine del notturno (se previsto, altrimenti null);
        /// in caso di problemi nel calcolo (periodo errato o dati non presenti, viene restituito un dizionario vuoto).
        /// </returns>
        public Dictionary<string, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> GetDevidedPlanMinutes(int colId, DateTime startDate, DateTime endDate, DateTime? dateStartCol, DateTime? dateEndCol,
            out bool isFromFreeTimesheet, out int freeTimesheetId)
        {
            // inizializzazione del valore di ritorno del metodo
            var returnDictionary = new Dictionary<string, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>>();

            // recupero dell'id della tab orari tipo a partire dall'id collaboratore passato come parametro
            int tabOrariTipoId = 0;
            tabOrariTipoId = GetTabOrariTipoIdFromEntity(colId);

            // calcolo dell'elenco di date del periodo
            List<DateTime> periodDates = CommonService.GetDatesFromPeriod(startDate, endDate);

            #region Calcolo delle date di validità di periodo in base alle date di disponibilità del collaboratore

            // le date di inizio e fine validità hanno senso solamente se si sta processando un piano per collaboratore
            Tuple<DateTime, DateTime> newValidDates = FilterPeriodWithColDispDates(startDate, endDate, dateStartCol, dateEndCol);
            DateTime startValidDate = newValidDates.Item1;
            DateTime endValidDate = newValidDates.Item2;

            #endregion

            // inizializzazione dei valori di ritorno riguardo all'utilizzo del Col_Orario
            isFromFreeTimesheet = false;
            freeTimesheetId = 0;

            // se è stato generato un periodo di date valido
            if (periodDates.Any())
            {
                // inizializzazione delle variaibli utilizzate per la gestione dell'orario notturno con i parametri generali
                TimeSpan? nocturnInitHour = RepoManager.ParamRepo.ParametersRow.Cartellino_Inizio_Notturno;
                TimeSpan? nocturnEndHour = RepoManager.ParamRepo.ParametersRow.Cartellino_Fine_Notturno;

                // se il collaboratore non ha un tipo orario collegato, allora si ritorna la somma globale
                if (true)
                {
                    // inizializzazione del dizionario che si sta generando
                    var timesheetDictionaryDay = new Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>();
                    var timesheetDictionaryNight = new Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>();

                    #region Generazione degli orari generali (senza divisione per cantiere)

                    // dal tipo orario calcolato si recuperano le eventuali date di inizio e fine notturno (solo se il tipo orario risulta presente)
                    if (tabOrariTipoId != 0)
                    {
                        var tipoOrario = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(to => to.Tab_Orari_Tipo_Id == tabOrariTipoId);

                        //Se sono stati impostati degli orari di inizio e fine notturno specifici per l'orario in questione, vengono rimpiazzati quelli dei parametri generali
                        if (tipoOrario.Tab_Orari_Tipo_Inizio_Not != null && tipoOrario.Tab_Orari_Tipo_Fine_Not != null)
                        {
                            nocturnInitHour = tipoOrario.Tab_Orari_Tipo_Inizio_Not;
                            nocturnEndHour = tipoOrario.Tab_Orari_Tipo_Fine_Not;
                        }
                    }
                    else
                    {
                        // se il calcolo non viene effettuato da una tabella orari allora
                        // sicuramente si recuperano i dati dalla tabella Col_Orario
                        isFromFreeTimesheet = true;
                    }

                    // inserimento del periodo di date all'interno del dizionario dell'orario diurno
                    periodDates.ForEach(pDate => timesheetDictionaryDay.Add(pDate, new Tuple<double, TimeSpan?, TimeSpan?>(0, null, null)));
                    periodDates.ForEach(pDate => timesheetDictionaryNight.Add(pDate, new Tuple<double, TimeSpan?, TimeSpan?>(0, null, null)));

                    // per ogni data da elaborare
                    foreach (var pDate in periodDates)
                    {
                        // per la data in elaborazione si recupera l'elenco degli orari
                        var validTimeSheets = GetDatePlanDetail(pDate, tabOrariTipoId, colId);

                        // viene calcolata la durata totale di tutti i timesheet calcolati (a partire dall'Tab_Orari se è stato inserito o altrimenti dal Col_Orario);
                        // si inserisce una durata che non sia 0 solamente se la data attualmente in elaborazione è compresa nel periodo di validità del collaboratore
                        double doubleDayDuration = 0;
                        double doubleNightDuration = 0;
                        TimeSpan dayDuration = TimeSpan.Zero;
                        TimeSpan nightDuration = TimeSpan.Zero;

                        bool nocturneModuleActive = RepoManager.ParamRepo.ParametersRow.Abilita_Notturno;
                        // tipo notturno
                        NocturneTypeEnum nocturneTypeParam = nocturneModuleActive ? (NocturneTypeEnum)RepoManager.ParamRepo.ParametersRow.TipoNotturno : NocturneTypeEnum.None;

                        //Mezzodì
                        TimeSpan noon = new TimeSpan(12, 0, 0);


                        if (pDate >= startValidDate && pDate <= endValidDate)
                        {
                            if (tabOrariTipoId != 0)
                            {
                                if (nocturneTypeParam == NocturneTypeEnum.None || nocturneTypeParam == NocturneTypeEnum.Disabled || nocturnInitHour == null || nocturnEndHour == null)
                                {
                                    doubleDayDuration = validTimeSheets.Select(ts => ts.Ora_U != null && ts.Ora_E != null ? (ts.Ora_U.Value.Subtract(ts.Ora_E.Value).TotalMinutes) : Convert.ToDouble(ts.Durata_Minuti)).Sum();
                                }

                                else
                                {
                                    TimeSpan? tempOraE = TimeSpan.Zero,
                                              tempOraU = TimeSpan.Zero,
                                              tempNocturnInitHour = TimeSpan.Zero,
                                              tempNocturnEndHour = TimeSpan.Zero;
                                    TimeSpan totalDayDuration = TimeSpan.Zero;
                                    TimeSpan totalNightDuration = TimeSpan.Zero;
                                   

                                    foreach (var timesheet in validTimeSheets)
                                    {
                                        tempNocturnInitHour = nocturnInitHour;
                                        tempNocturnEndHour = nocturnEndHour;

                                        //Se l'orario ha entrata&uscita, uso quelli per calcolare la divisione tra i piani
                                        if (timesheet.Ora_E.HasValue && timesheet.Ora_U.HasValue)
                                        {
                                            tempOraE = timesheet.Ora_E.Value;
                                            tempOraU = timesheet.Ora_U.Value;

                                            //Se l'uscita è nel giorno successivo, vengono aggiunte 24h per uniformità nei calcoli
                                            if (timesheet.Ora_E.Value > timesheet.Ora_U.Value)
                                            {
                                                tempOraU = timesheet.Ora_U.Value + new TimeSpan(1, 0, 0, 0);
                                            }

                                            else if (tempOraU <= noon && tempOraE <= tempNocturnEndHour && tempOraU >= tempNocturnEndHour)
                                            {
                                                tempOraE = timesheet.Ora_E.Value + new TimeSpan(1, 0, 0, 0);
                                                tempOraU = timesheet.Ora_U.Value + new TimeSpan(1, 0, 0, 0);
                                            }

                                            if (tempNocturnInitHour > tempNocturnEndHour)
                                            {
                                                tempNocturnEndHour = tempNocturnEndHour.Value + new TimeSpan(1, 0, 0, 0);
                                            }

                                            if ((tempNocturnInitHour <= tempOraE) && (tempOraE <= tempNocturnEndHour) && (tempNocturnEndHour <= tempOraU))
                                            {
                                                nightDuration = tempNocturnEndHour.Value.Subtract(tempOraE.Value);
                                            }

                                            if ((tempOraE <= tempOraU) && (tempOraU <= tempNocturnInitHour) && (tempNocturnInitHour <= tempNocturnEndHour))
                                            {
                                                nightDuration = TimeSpan.Zero;
                                            }

                                            else if ((tempOraE <= tempNocturnInitHour) && (tempNocturnInitHour <= tempOraU) && (tempOraU <= tempNocturnEndHour))
                                            {
                                                nightDuration = tempOraU.Value.Subtract(tempNocturnInitHour.Value);
                                            }

                                            else if ((tempNocturnInitHour <= tempOraE) && (tempOraE <= tempOraU) && (tempOraU <= tempNocturnEndHour))
                                            {
                                                nightDuration = tempOraU.Value.Subtract(tempOraE.Value);
                                            }

                                            else if ((tempNocturnInitHour <= tempOraE) && (tempOraE <= tempNocturnEndHour) && (tempNocturnEndHour <= tempOraU))
                                            {
                                                nightDuration = tempNocturnEndHour.Value.Subtract(tempOraE.Value);
                                            }

                                            else if ((tempNocturnInitHour <= tempNocturnEndHour) && (tempNocturnEndHour <= tempOraE) && (tempOraE <= tempOraU))
                                            {
                                                nightDuration = TimeSpan.Zero;
                                            }

                                            else if ((tempOraE <= tempNocturnInitHour) && (tempNocturnInitHour <= tempNocturnEndHour) && (tempNocturnEndHour <= tempOraU))
                                            {
                                                nightDuration = tempNocturnEndHour.Value.Subtract(tempNocturnInitHour.Value);
                                            }

                                            dayDuration = tempOraU.Value.Subtract(tempOraE.Value).Subtract(nightDuration);
                                        }

                                        //Se l'orario ha solo la durata, essa è totalmente diurna
                                        else
                                        {
                                            dayDuration = new TimeSpan(0, timesheet.Durata_Minuti, 0);
                                            nightDuration = new TimeSpan(0, 0, 0);
                                        }

                                        totalDayDuration = totalDayDuration.Add(dayDuration);
                                        totalNightDuration = totalNightDuration.Add(nightDuration);
                                    }

                                    doubleDayDuration = totalDayDuration.TotalMinutes;
                                    doubleNightDuration = totalNightDuration.TotalMinutes;
                                }
                            }

                            //Se il cartellino è free
                            else
                            {
                               // dayDuration = RepoManager.ColCantOrarioRepo.GetPlannedMinutesFromTimesheet(pDate, entityId, referenceEntity, out freeTimesheetId);
                            }
                        }

                        // inserimento della durata prevista all'interno del dizionario di ritorno alla data
                        // in elaborazione
                        timesheetDictionaryDay[pDate] = new Tuple<double, TimeSpan?, TimeSpan?>(doubleDayDuration, nocturnInitHour, nocturnEndHour);
                        timesheetDictionaryNight[pDate] = new Tuple<double, TimeSpan?, TimeSpan?>(doubleNightDuration, nocturnInitHour, nocturnEndHour);
                    }
                    //Aggiunge gli orari al dictionary da tornare
                    returnDictionary.Add(DayTimesheetKey, timesheetDictionaryDay);
                    returnDictionary.Add(NightTimesheetKey, timesheetDictionaryNight);
                    #endregion
                }
            }
            
            return returnDictionary;
        }

        /// <summary>
        /// Metodo che per il periodo passato come parametro si occupa di generare un piano completamente vuoto (e senza previsione di notturno).
        /// </summary>
        /// <param name="startDate">La data di inizio per la generazione del piano</param>
        /// <param name="endDate">La data di fine per la generazione del piano</param>
        /// <param name="requestedForWeeklyTotals">Indica che il piano è richiesto per un calcolo che prevede i totali settimanali.</param>
        /// <returns>Un dizionario con chiave la data dell'intervallo e come valore una tuple i cui valori rappresentano: il numero di minuti previsto per la giornata (0); l'ora di inzio
        /// del notturno (null); l'ora di fine del notturno (null).</returns>
        public Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>> GetEmptyMinutesPlan(DateTime startDate, DateTime endDate, bool requestedForWeeklyTotals = false)
        {
            // innizializzazione del valore di ritorno del metodo
            var returnDictionary = new Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>();

            /* se è richiesto di costruire i dati per un orario con totali settimanali si verifica se si sta processando
            * l'inizio e la fine del mese; in questo caso, se necessario si aggiornano le date per comprendere l'inizio e la fine della settimana
            * del mese precedente e successivo */

            // se è richiesto il piano per la gestione di orari settimanali, la data di inizio è l'inizio del mese e la data di inizio non è un lunedì
            // allora si modifica la data di inizio periodo l'ultimo lunedì del mese precedente
            if (requestedForWeeklyTotals && startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);

            // se è richiesto il piano per la gestione degli orari settimanali, la data di fine è la fine del mese e non si tratta di una domenica
            // allora si recupera la data di fine periodo la prima domenica del mese successivo
            if (requestedForWeeklyTotals && endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

            // calcolo dell'elenco di date del periodo
            var periodDates = CommonService.GetDatesFromPeriod(startDate, endDate);

            // se è stato generato un periodo di date valido
            // allora si provve all'inserimento del periodo di date all'interno del dizionario
            if (periodDates.Any())
                periodDates.ForEach(pDate => returnDictionary.Add(pDate, new Tuple<double, TimeSpan?, TimeSpan?>(0, null, null)));

            // ritorno del valore del metodo
            return returnDictionary;
        }

        /// <summary>
        /// Ricalcola il periodo di ricerca specificato utilizzando le date di disponibilità del collaboratore.
        /// </summary>
        /// <param name="startDate">La data di inizio della ricerca da ricalcolare.</param>
        /// <param name="endDate">La data di fine della ricerca da ricalcolare.</param>
        /// <param name="dateStartCol">La data di inizio di disponibilità del collaboratore.</param>
        /// <param name="dateEndCol">La data di fine di disponibilità del collaboratore.</param>
        /// <returns>Ritorna una tuple che contiene nel primo item la data di inizio ricalcolata e come secondo parametro la data di fine ricalcolata.</returns>
        public Tuple<DateTime, DateTime> FilterPeriodWithColDispDates(DateTime startDate, DateTime endDate, DateTime? dateStartCol, DateTime? dateEndCol)
        {
            // sono calcolate le date limite in cui generare ore previste, contenendo il perido tra startDate ed endDate
            // con le eventuali date di inizio e fine disponibilità del collaboratore
            var returnDates = new Tuple<DateTime, DateTime>(startDate, endDate);

            // se la data di inizio validità è valorizzata ed è superiore alla data di inizio del periodo da calcolare, allora la data di inizio limite è impostata
            // al valore di inizio disponibilità del collaboratore
            // (se il collaboratore è stato assunto dopo la data di inzio di produzione del piano allora le ore previste prima della data di assunzione sono a 0)
            if (dateStartCol.HasValue && dateStartCol.Value > startDate)
                returnDates = new Tuple<DateTime, DateTime>(dateStartCol.Value, returnDates.Item2);

            // se la data di fine validità è valorizzata ed è inferiore alla data di fine del periodo da calcolare, allora la data di fine limite è impostata
            // al valore di fine disponibilità del collaboratore
            // (se il collaboratore è stato licenziato prima della data di fine di produzione del piano allora le ore previste dopo la data di licenziamento sono a 0)
            if (dateEndCol.HasValue && dateEndCol.Value < endDate)
                returnDates = new Tuple<DateTime, DateTime>(returnDates.Item1, dateEndCol.Value);

            // se il collaboratore ha una data di inizio disponibilità superiore alla data di termine del periodo da processare
            // o se ha una data di termine disponibilità inferiore alla data di inizio del periodo da elaborare
            // allora non dovrà essere generata nessuna ora prevista
            // (se il collaboratore è stato licenziato prima del periodo in ricerca o se è stato assunto dopo il periodo in ricerca, tutte le ore previste sono a 0
            if ((dateStartCol.HasValue && dateStartCol.Value > endDate) || (dateEndCol.HasValue && dateEndCol.Value < startDate))
            {
                returnDates = new Tuple<DateTime, DateTime>(DateTime.MaxValue, DateTime.MinValue);
            }

            // ritorno del valore del metodo
            return returnDates;
        }

        /// <summary>
        /// Recupera il piano di dettaglio per il giorno e l'entità indicata.
        /// Questo metodo non prende in considerazione gli orari di sola durata.
        /// </summary>
        /// <param name="dateToSearch">La data di cui ricercare il piano di dettaglio.</param>
        /// <param name="entityId">L'indentificativo univoco dell'entità di cui effettuare la ricerca.</param>
        /// <param name="referenceEntity">Il tipo di entità di riferimento per l'orario.</param>
        /// <returns>Un'elenco contente l'id dell'altra entità di riferimento (0 in caso di orario generico), l'ora di inzio e ora di fine previsto.</returns>
        public List<Tuple<int, TimeSpan, TimeSpan>> GetDayPlanDetail(DateTime dateToSearch, int entityId, string referenceEntity = "Col")
        {
            // inizializzazione del valore di ritorno del metodo
            var dayDetail = new List<Tuple<int, TimeSpan, TimeSpan>>();

            // recupero dell'id della tab orari tipo a partire dall'id collaboratore/cantiere passato come parametro passato come parametro
            int tabOrariTipoId = 0;
            tabOrariTipoId = GetTabOrariTipoIdFromEntity(entityId, referenceEntity);

            // si procede solamente se l'orario è stato correttamente trovato
            if (tabOrariTipoId != 0)
            {
                // calcolo dell'elenenco degli orari validi per data ed entità e ciclo si ognuno di essi
                foreach (Tab_Orari validTimeSheet in GetDatePlanDetail(dateToSearch, tabOrariTipoId, entityId, referenceEntity))
                {
                    // se l'orario ha valorizzato entrata e uscita allora si aggiunge il dato di dettaglio alla lista di ritorno
                    if (validTimeSheet.Ora_E != null && validTimeSheet.Ora_U != null)
                    {
                        // calcolo dell'id dell'altra entità da processare
                        int otherEntityId = 0;
                        if (referenceEntity == ColEntityName)
                            otherEntityId = validTimeSheet.Cant_Id ?? 0;
                        else
                            otherEntityId = validTimeSheet.Col_Id ?? 0;

                        dayDetail.Add(new Tuple<int, TimeSpan, TimeSpan>(otherEntityId, validTimeSheet.Ora_E.Value, validTimeSheet.Ora_U.Value));
                    }
                }
            }

            // ritorno del valore calcolato dal metodo
            return dayDetail;
        }


        /// <summary>
        /// Restituisce la durata totale del campo
        /// </summary>
        /// <param name="dateToSearch">The date to search.</param>
        /// <param name="entityId">The entity identifier.</param>
        /// <param name="referenceEntity">The reference entity.</param>
        /// <returns></returns>
        public int GetMonthlyPlanDuration(DateTime dateToSearch, int entityId, string referenceEntity = "Col")
        {
            // inizializzazione del valore di ritorno del metodo
            var duration = 0;

            // recupero dell'id della tab orari tipo a partire dall'id collaboratore/cantiere passato come parametro passato come parametro
            int tabOrariTipoId = 0;
            tabOrariTipoId = GetTabOrariTipoIdFromEntity(entityId, referenceEntity);

            // se sono presenti degli orari per l'id passato come parametro validi per la data passata come parametro (si recupera sempre l'ultima versione valida)
            var validTimesheet = tabOrariTipoId != 0
                ? Find(tor => tor.Tab_Orari_Tipo_Id == tabOrariTipoId && dateToSearch >= tor.Data_Inizio && tor.Orario_Mensile).ToList()
                : GetStandardTimeTable(dateToSearch, entityId);

            if (validTimesheet.Any())
            {
                var lastTimesheetDate = validTimesheet.Max(tor => tor.Data_Inizio);
                validTimesheet = validTimesheet.Where(tor => tor.Data_Inizio == lastTimesheetDate).ToList();

                //per ognuno degli orrai mensili viene sommata la durata totale del mese
                validTimesheet.ForEach(ts =>
                {
                    duration += ts.Durata_Minuti;
                   
                });
            }
                        
            // ritorno del valore calcolato dal metodo
            return duration;
        }





        /// <summary>
        /// Per il collaboratore e l'intervallo di date specificato questo metodo si occupa di ricercare all'interno
        /// della tab orari quanto configurato e ritorna un elenco di date in cui, per ogni data, sono specificati gli orari previsti.
        /// </summary>
        /// <param name="colId">L'id del collaboratore da ricercare.</param>
        /// <param name="startDate">La data di partenza per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="endDate">La data di termine per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="dateStartCol">La data di inizio disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="dateEndCol">la data di fine disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <returns>
        /// Un dizionario con chiave la data dell'intervallo e come valore una lista di coppie di ore entrata/uscita e l'ora di inizio e fine notturno; in caso
        /// di problemi nel calcolo (periodo errato o dati non presenti, viene restituito un dizionario vuoto).
        /// </returns>
        public Dictionary<DateTime, List<Tuple<TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>>> GetPlanTimes(int colId, DateTime startDate, DateTime endDate, DateTime? dateStartCol, DateTime? dateEndCol)
        {
            // inizializzazione del valore di ritorno del metodo
            var returnDictionary = new Dictionary<DateTime, List<Tuple<TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>>>();

            // recupero dell'id della tab orari tipo a partire dall'id collaboratore passato come parametro
            int tabOrariTipoId = GetTabOrariTipoIdFromEntity(colId);

            // calcolo dell'elenco di date del periodo
            var periodDates = CommonService.GetDatesFromPeriod(startDate, endDate);

            #region Calcolo delle date di validità di periodo in base alle date di disponibilità del collaboratore

            // sono calcolate le date limite in cui generare ore previste, contenendo il perido tra startDate ed endDate
            // con le eventuali date di inizio e fine disponibilità del collaboratore
            var startValidDate = startDate;
            var endValidDate = endDate;

            // se la data di inizio validità è valorizzata ed è superiore alla data di inizio del periodo da calcolare, allora la data di inizio limite è impostata
            // al valore di inizio disponibilità del collaboratore
            // (se il collaboratore è stato assunto dopo la data di inzio di produzione del piano allora le ore previste prima della data di assunzione sono a 0)
            if (dateStartCol.HasValue && dateStartCol.Value > startDate)
                startValidDate = dateStartCol.Value;

            // se la data di fine validità è valorizzata ed è inferiore alla data di fine del periodo da calcolare, allora la data di fine limite è impostata
            // al valore di fine disponibilità del collaboratore
            // (se il collaboratore è stato licenziato prima della data di fine di produzione del piano allora le ore previste dopo la data di licenziamento sono a 0)
            if (dateEndCol.HasValue && dateEndCol.Value < endDate)
                endValidDate = dateEndCol.Value;

            // se il collaboratore ha una data di inizio disponibilità superiore alla data di termine del periodo da processare
            // o se ha una data di termine disponibilità inferiore alla data di inizio del periodo da elaborare
            // allora non dovrà essere generata nessuna ora prevista
            // (se il collaboratore è stato licenziato prima del periodo in ricerca o se è stato assunto dopo il periodo in ricerca, tutte le ore previste sono a 0
            if ((dateStartCol.HasValue && dateStartCol.Value > endDate) || (dateEndCol.HasValue && dateEndCol.Value < startDate))
            {
                startValidDate = DateTime.MaxValue;
                endValidDate = DateTime.MinValue;
            }

            #endregion

            // se è stato generato un periodo di date valido
            if (periodDates.Any())
            {
                // dal tipo orario calcolato si recuperano le eventuali date di inizio e fine notturno
                var tipoOrario = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(to => to.Tab_Orari_Tipo_Id == tabOrariTipoId);

                // si procede con l'elaborazione solamente se l'orario è stato trovato
                if (tipoOrario != default(Tab_Orari_Tipo))
                {

                    TimeSpan? nocturnInitHour = tipoOrario.Tab_Orari_Tipo_Inizio_Not;
                    TimeSpan? nocturnEndHour = tipoOrario.Tab_Orari_Tipo_Fine_Not;

                    // viene recuperato dai parametri l'eventuale default di inzio della giornata
                    var defaultInitDay = RepoManager.ParamRepo.ParametersRow.Default_Ora_Inizio_Giornata ?? new TimeSpan(0, 0, 0);

                    // inserimento del periodo di date all'interno del dizionario
                    periodDates.ForEach(pDate => returnDictionary.Add(pDate, new List<Tuple<TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>>()));

                    // per ogni data da elaborare
                    periodDates.ForEach(pDate =>
                    {
                        // per la data in elaborazione si recupera l'elenco degli orari
                        var validTimeSheet = GetDatePlanDetail(pDate, tabOrariTipoId, colId);

                        // inizializzazione della lista da aggiungere al dizionario;
                        // si inserisce una durata che non sia 0 solamente se la data attualmente in elaborazione è compresa nel periodo di validità del collaboratore
                        var plan = validTimeSheet.Select(timeSheet =>
                            {
                                if (pDate >= startValidDate && pDate <= endValidDate)
                                    if (timeSheet.Ora_E != null && timeSheet.Ora_U != null)
                                        return new Tuple<TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>(timeSheet.Ora_E.Value,
                                            timeSheet.Ora_U.Value, nocturnInitHour, nocturnEndHour);
                                    else
                                        return new Tuple<TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>(defaultInitDay,
                                            defaultInitDay.Add(timeSheet.DisplayedDuration), nocturnInitHour, nocturnEndHour);
                                else // in caso non si sia nell'intervallo valido si ritorna il valore null
                                    return null;
                            }
                            ).ToList();

                        // aggiunta della lista calcolata a partire dai timesheet nel dizionario nella data in elaborazione
                        returnDictionary[pDate] = plan;

                    });
                }
            }

            // ritorno del valore del metodo
            return returnDictionary;
        }

        /// <summary>
        /// Per il collaboratore e l'intervallo di date specificato questo metodo si occupa di ricercare all'interno
        /// della tab orari quanto configurato e ritorna un elenco di date in cui, per ogni data, sono specificati gli orari previsti.
        /// </summary>
        /// <param name="colId">L'id del collaboratore da ricercare.</param>
        /// <param name="startDate">La data di partenza per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="endDate">La data di termine per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="dateStartCol">La data di inizio disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="dateEndCol">la data di fine disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <returns>
        /// Un dizionario con chiave la data dell'intervallo e come valore una lista di coppie di ore entrata/uscita e l'ora di inizio e fine notturno; in caso
        /// di problemi nel calcolo (periodo errato o dati non presenti, viene restituito un dizionario vuoto).
        /// </returns>
        public Dictionary<DateTime, List<Tuple<int,TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>>> GetPlanTimesNew(int colId, DateTime startDate, DateTime endDate, DateTime? dateStartCol, DateTime? dateEndCol)
        {
            // inizializzazione del valore di ritorno del metodo
            var returnDictionary = new Dictionary<DateTime, List<Tuple<int,TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>>>();

            // recupero dell'id della tab orari tipo a partire dall'id collaboratore passato come parametro
            int tabOrariTipoId = GetTabOrariTipoIdFromEntity(colId);

            // calcolo dell'elenco di date del periodo
            var periodDates = CommonService.GetDatesFromPeriod(startDate, endDate);

            #region Calcolo delle date di validità di periodo in base alle date di disponibilità del collaboratore

            // sono calcolate le date limite in cui generare ore previste, contenendo il perido tra startDate ed endDate
            // con le eventuali date di inizio e fine disponibilità del collaboratore
            var startValidDate = startDate;
            var endValidDate = endDate;

            // se la data di inizio validità è valorizzata ed è superiore alla data di inizio del periodo da calcolare, allora la data di inizio limite è impostata
            // al valore di inizio disponibilità del collaboratore
            // (se il collaboratore è stato assunto dopo la data di inzio di produzione del piano allora le ore previste prima della data di assunzione sono a 0)
            if (dateStartCol.HasValue && dateStartCol.Value > startDate)
                startValidDate = dateStartCol.Value;

            // se la data di fine validità è valorizzata ed è inferiore alla data di fine del periodo da calcolare, allora la data di fine limite è impostata
            // al valore di fine disponibilità del collaboratore
            // (se il collaboratore è stato licenziato prima della data di fine di produzione del piano allora le ore previste dopo la data di licenziamento sono a 0)
            if (dateEndCol.HasValue && dateEndCol.Value < endDate)
                endValidDate = dateEndCol.Value;

            // se il collaboratore ha una data di inizio disponibilità superiore alla data di termine del periodo da processare
            // o se ha una data di termine disponibilità inferiore alla data di inizio del periodo da elaborare
            // allora non dovrà essere generata nessuna ora prevista
            // (se il collaboratore è stato licenziato prima del periodo in ricerca o se è stato assunto dopo il periodo in ricerca, tutte le ore previste sono a 0
            if ((dateStartCol.HasValue && dateStartCol.Value > endDate) || (dateEndCol.HasValue && dateEndCol.Value < startDate))
            {
                startValidDate = DateTime.MaxValue;
                endValidDate = DateTime.MinValue;
            }

            #endregion

            // se è stato generato un periodo di date valido
            if (periodDates.Any())
            {
                // dal tipo orario calcolato si recuperano le eventuali date di inizio e fine notturno
                var tipoOrario = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(to => to.Tab_Orari_Tipo_Id == tabOrariTipoId);

                // si procede con l'elaborazione solamente se l'orario è stato trovato
                if (tipoOrario != default(Tab_Orari_Tipo))
                {

                    TimeSpan? nocturnInitHour = tipoOrario.Tab_Orari_Tipo_Inizio_Not;
                    TimeSpan? nocturnEndHour = tipoOrario.Tab_Orari_Tipo_Fine_Not;

                    // viene recuperato dai parametri l'eventuale default di inzio della giornata
                    var defaultInitDay = RepoManager.ParamRepo.ParametersRow.Default_Ora_Inizio_Giornata ?? new TimeSpan(0, 0, 0);

                    // inserimento del periodo di date all'interno del dizionario
                    periodDates.ForEach(pDate => returnDictionary.Add(pDate, new List<Tuple<int,TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>>()));

                    // per ogni data da elaborare
                    periodDates.ForEach(pDate =>
                    {
                        // per la data in elaborazione si recupera l'elenco degli orari
                        var validTimeSheet = GetDatePlanDetail(pDate, tabOrariTipoId, colId);

                        // inizializzazione della lista da aggiungere al dizionario;
                        // si inserisce una durata che non sia 0 solamente se la data attualmente in elaborazione è compresa nel periodo di validità del collaboratore
                        var plan = validTimeSheet.Select(timeSheet =>
                        {
                            if (pDate >= startValidDate && pDate <= endValidDate)
                                if (timeSheet.Ora_E != null && timeSheet.Ora_U != null) {
                                    if (timeSheet.Cant_Id != null)
                                    {
                                        return new Tuple<int, TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>(timeSheet.Cant_Id.Value, timeSheet.Ora_E.Value,
                                        timeSheet.Ora_U.Value, nocturnInitHour, nocturnEndHour);
                                    }
                                    else
                                    {
                                        return new Tuple<int, TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>(0, timeSheet.Ora_E.Value,
                                        timeSheet.Ora_U.Value, nocturnInitHour, nocturnEndHour);
                                    }
                                }
                                else {
                                    if (timeSheet.Cant_Id != null)
                                    {
                                        return new Tuple<int, TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>(timeSheet.Cant_Id.Value, defaultInitDay,
                                        defaultInitDay.Add(timeSheet.DisplayedDuration), nocturnInitHour, nocturnEndHour);
                                    }
                                    else {
                                        return new Tuple<int, TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>(0, defaultInitDay,
                                        defaultInitDay.Add(timeSheet.DisplayedDuration), nocturnInitHour, nocturnEndHour);
                                    }
                                }
                                    
                            else // in caso non si sia nell'intervallo valido si ritorna il valore null
                                return null;
                        }
                            ).ToList();

                        // aggiunta della lista calcolata a partire dai timesheet nel dizionario nella data in elaborazione
                        returnDictionary[pDate] = plan;

                    });
                }
            }

            // ritorno del valore del metodo
            return returnDictionary;
        }

        /// <summary>
        /// Recupera l'id della tab orari tipo collegata al collaboratore passato come parametro.
        /// In caso il collaboratore non sia stato trovato o la tab_orari configurata nel collaboratore
        /// non sia presente il metodo ritorna valore 0
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere da ricercare</param>
        /// <param name="entityName">La stringa che rappresenta l'entità (collaboratore/cantiere) per cui si sta effettuando la ricerca</param>
        /// <returns>L'id del record in tab orari collegato al collaboratore il cui id è passato come parametro; in caso
        /// il collaboratore non sia presente o non sia stato trovato il record in tab orari viene ritornato il valore 0</returns>
        public int GetTabOrariTipoIdFromEntity(int entityId, string entityName = "Col")
        {
            // inizializzazione del valore di ritorno del metodo
            var tabOrariId = 0;

            // se è richiesto di processare un collaboratore
            if (entityName == ColEntityName)
            {
                // recupero del collaboratore utilizzando l'id passato come parametro
                var currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == entityId);

                // se è stato trovato un collaboratore e
                // se i collaboratore ha collegato un record di tab orari si ritorna quel valore;
                // in caso contrario si ritorna il valore di default
                if (currentCol != null)
                    tabOrariId = currentCol.Tab_Orari_Tipo_Id.HasValue ? Convert.ToInt32(currentCol.Tab_Orari_Tipo_Id) : tabOrariId;
            }
            else if (entityName == CantEntityName) // se invece è richiesto di processare un cantiere
            {
                // recupero del cantiere utilizzando l'id passato come parametro
                var currentCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == entityId);

                // se è stato trovato un cantiere e
                // se i cantiere ha collegato un record di tab orari si ritorna quel valore;
                // in caso contrario si ritorna il valore di default
                if (currentCant != null)
                    tabOrariId = currentCant.Tab_Orari_Tipo_Id.HasValue ? Convert.ToInt32(currentCant.Tab_Orari_Tipo_Id) : tabOrariId;
            }

            // ritorno del valore del metodo
            return tabOrariId;
        }

        /// <summary>
        /// A partire dalla data passata come parametro e dall'id del tipo orario passato come parametro,
        /// si occupa di ricercare e restituire gli orari validi per tali dati; in caso di non presenza di dati
        /// o di problemi nella lettura viene restituita una lista vuota; in caso di <see cref="tabOrariTipoId"/> vuoto (valore 0)
        /// allora viene ritornato l'orario definito come standard
        /// </summary>
        /// <param name="dateToSearch">La data di cui recuperare gli orari.</param>
        /// <param name="tabOrariTipoId">Il tipo orario da cui recuperare gli orari.</param>
        /// <param name="colId">L'id del collaboratore per cui ricercare il pano di dettaglio (utilizzato in caso di orario di default)</param>
        /// <param name="entityType">Il tipo di entità (collaboratore/cantiere) a cui fa riferimento il dato</param>
        /// <returns>La lista degli orari validi per lo specifico tipico e data; in caso di non presenza di dati
        /// o di problemi nella lettura viene restituita una lista vuota.</returns>
        private IEnumerable<Tab_Orari> GetDatePlanDetail(DateTime dateToSearch, int tabOrariTipoId, int colId, string entityType = "Col")
        {
            // inizializzazione del valore di ritorno del metodo
            var returnTimesheet = new List<Tab_Orari>();

            // se sono presenti degli orari per l'id passato come parametro validi per la data passata come parametro (si recupera sempre l'ultima versione valida)
            var validTimesheet = tabOrariTipoId != 0
                ? Find(tor => tor.Tab_Orari_Tipo_Id == tabOrariTipoId && dateToSearch >= tor.Data_Inizio).ToList()
                : GetStandardTimeTable(dateToSearch, colId);

            if (validTimesheet.Any())
            {
                var lastTimesheetDate = validTimesheet.Max(tor => tor.Data_Inizio);
                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportStr) == 0) {
                    validTimesheet = validTimesheet.Where(tor => tor.Data_Inizio == lastTimesheetDate).ToList();
                }
                // per ognuno degli orari recuperati viene verificato se si tratta di un orario valido per la data
                // (cioè se rispetta giorno/ripetizione, non si tratta di un giorno festivo (solo per i collaboratori) e sia flaggato il giorno corretto); se si tratta di un orario
                // valido allora lo si aggiunge all'elenco
                validTimesheet.ForEach(ts =>
                {
                    if (IsToApplyTimesheet(ts, dateToSearch) && (!RepoManager.Tab_FestiviRepo.DbSet.Any(hol => hol.Giorno_Tab_Festivi == dateToSearch.Date) || entityType == CantEntityName))
                    {
                        bool toAddTimeSheet = (dateToSearch.DayOfWeek == DayOfWeek.Monday && ts.G1) ||
                            (dateToSearch.DayOfWeek == DayOfWeek.Tuesday && ts.G2) ||
                            (dateToSearch.DayOfWeek == DayOfWeek.Wednesday && ts.G3) ||
                            (dateToSearch.DayOfWeek == DayOfWeek.Thursday && ts.G4) ||
                            (dateToSearch.DayOfWeek == DayOfWeek.Friday && ts.G5) ||
                            (dateToSearch.DayOfWeek == DayOfWeek.Saturday && ts.G6) ||
                            (dateToSearch.DayOfWeek == DayOfWeek.Sunday && ts.G7);

                        if (toAddTimeSheet)
                            returnTimesheet.Add(ts);
                    }
                });
            }

            // ritorno del valore calcolato nel metodo
            return returnTimesheet;
        }

        /// <summary>
        /// Genera e restituisce un orario standard per un collaboratore senza orario collegato.
        /// L'orario sarà da lunedì a venerdì dalle 09:00 alle 13:00 e dalle 14:00 alle 18:00.
        /// </summary>
        /// <param name="startDate">La data di partenza dell'orario standard.</param>
        /// <param name="colId">L'id del collaboratore di cui controllare la durata giornaliera; in caso l'id sia 0 o il collaboratore non sia trovato
        /// allora viene utilizzato il valore inserito nella scheda parametri.</param>
        /// <returns>L'orario standard calcolato (da lunedì a venerdì dalle 09:00 alle 13:00 e dalle 14:00 alle 18:00).</returns>
        private List<Tab_Orari> GetStandardTimeTable(DateTime startDate, int colId)
        {
            // recupero l'ora di inizio dalla tab parametri, se null allora si procede di imperio alle 09:00
            var oraEntrata = RepoManager.ParamRepo.ParametersRow.Default_Ora_Inizio_Giornata ?? new TimeSpan(9, 0, 0);

            // recupero della durata giornaliera di default dalla tab parametri (se null impostata a 8 ore)
            var durataGiorno = RepoManager.ParamRepo.ParametersRow.Default_Durata_Giornata ?? new TimeSpan(8, 0, 0);

            // se è stato passato un id collaboratore valido
            if (colId != 0)
            {
                // se il collaboratore è presente nell'elenco dei collaboratori
                var currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == colId);

                // se il collaboratore è stato trovato
                // e se il collaboratore ha un valore come ore massime di giornata si utilizza quel parametro
                if (currentCol != null)
                    durataGiorno = currentCol.OreMaxGG_Col ?? durataGiorno;
            }

            // calcolo dell'ora di default di uscita sommando all'ora d'entrata la durata del giorno
            var oraUscita = oraEntrata.Add(durataGiorno);

            // costrzione dell'orario mattutino
            var morningTimeSheet = new Tab_Orari()
            {
                Data_Inizio = startDate,
                Ora_E = oraEntrata,
                Ora_U = oraUscita,
                G1 = true,
                G2 = true,
                G3 = true,
                G4 = true,
                G5 = true,
                G6 = false,
                G7 = false,
                Ripetizione = 1
            };

            return new List<Tab_Orari>() { morningTimeSheet };
        }

        #endregion

        /// <summary>
        /// Richiamato nel momento in cui si sta per aggiornare o inserire una <see cref="Tab_Orari"/>;
        /// Questo metodo si occupa, nel caso, di verificare e gestire il calcolo della durata.
        /// </summary>
        /// <param name="entity">L'entità da processare</param>
        public override void SetEntityBeforeAddOrUpdate(Tab_Orari entity)
        {
            // se l'ora di inzio e di fine non sono null allora si calcola la durata a partire dalla differenza di tali dati;
            // in caso contrario si provvede a mantenere la durata inserita
            if (entity.Ora_E != null && entity.Ora_U != null)
            {
                bool nocturneModuleActive = RepoManager.ParamRepo.ParametersRow.Abilita_Notturno;
                // tipo notturno
                NocturneTypeEnum nocturneTypeParam = nocturneModuleActive ? (NocturneTypeEnum)RepoManager.ParamRepo.ParametersRow.TipoNotturno : NocturneTypeEnum.None;

                TimeSpan almostMidnight = new TimeSpan(23, 59, 0);
                TimeSpan midnight = new TimeSpan(0, 0, 0);

                if (nocturneTypeParam != NocturneTypeEnum.OverMidnight)
                    entity.Durata_Minuti = CommonService.GetMinutesFromTimeSpan(entity.Ora_U.Value.Subtract(entity.Ora_E.Value));
                else if (nocturneTypeParam == NocturneTypeEnum.OverMidnight)
                {
                    if (entity.Ora_E.Value > entity.Ora_U.Value)
                    {
                        int partialMinutes = CommonService.GetMinutesFromTimeSpan(almostMidnight.Subtract(entity.Ora_E.Value)) + 1;
                        entity.Durata_Minuti = CommonService.GetMinutesFromTimeSpan(entity.Ora_U.Value.Subtract(midnight)) + partialMinutes;


                    }
                    else
                        entity.Durata_Minuti = CommonService.GetMinutesFromTimeSpan(entity.Ora_U.Value.Subtract(entity.Ora_E.Value));

                }
            }
        }
    }
}