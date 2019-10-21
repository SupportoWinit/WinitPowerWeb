using BingMapsRESTToolkit;

using Business.BusinessServices.ElaborateService.Helpers;
using Business.Repository;
using Common;
using Common.Properties;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.ElaborateService
{
    public class ElaborateService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ElaborateService));

        private ICollection<KeyValuePair<string, string>> _errors;

        public IEnumerable<KeyValuePair<string, string>> Errors
        {
            get
            {
                return _errors;
            }
        }

        public void ClearErrors()
        {
            _errors.Clear();
        }

        public ElaborateService()
        {
            _errors = new List<KeyValuePair<string, string>>();
        }

        #region Lock - unlock processo

        public void LockProcess()
        {
            RepoManager.ParamRepo.LockElaboration();
        }

        public void UnlockProcess()
        {
            RepoManager.ParamRepo.UnLockElaboration();
        }

        #endregion

        public IEnumerable<KeyValuePair<string, string>> Elaborate(DateTime minDate, DateTime maxDate)
        {
            var regsToElaborate = RepoManager.RegRepo.GetRegsToElaborate(minDate, maxDate);

            return Elaborate(regsToElaborate);
        }

        public IEnumerable<KeyValuePair<string, string>> Elaborate(Col col, DateTime minDate, DateTime maxDate)
        {
            var regsToElaborate = RepoManager.RegRepo.GetRegsToElaborate(minDate, maxDate, col);

            return Elaborate(regsToElaborate);
        }

        public IEnumerable<KeyValuePair<string, string>> Elaborate(IEnumerable<Reg> regs)
        {
            ClearErrors();

            #region 1. Rimozione delle registrazioni da non processare ed eventuale accoppiamento delle bloccate marcate

            //Accoppiamento delle bloccate con codice temporaneo
            _log.Info("Rimozione delle registrazioni da non processare ed eventuale accoppiamento delle bloccate marcate.");

            regs = CoupleBlockedRegs(regs);

            #endregion

            #region 2. Esecuzione dei pre-processi custom provenienti da personalizzazione

            // una volta scremeto l'elenco di registrazioni che si desidera processare
            // si effettua un pre processo custom delle elaborazioni se richiesto da qualche personalizzazione applicativa

            _log.Info("Esecuzione dei pre - processi custom provenienti da personalizzazione.");

            //errors.AddRange(ManagePreCustomElaborateRegs(regs, isToSaveChanges));

            #endregion

            #region 3. Cancellazione delle timbrature automatiche a chiusura causali e arrotondamenti per durata

            _log.Info("Cancellazione delle timbrature automatiche a chiusura causali e arrotondamenti per durata");

            // dalle registrazioni che si stanno processando si eliminano, se il modulo attività risulta abilitato,
            // tutte le timbrature generate automaticamente a chiusura delle attività
            regs = DeleteAllActivitiesAutoClosures(regs);

            // dalle registrazioni che si stanno processando si eliminano gli arrotondamenti per durata
            regs = DeleteDurationRounding(regs);

            #endregion

            #region 4. Associazione Pru-Fru

            _log.Info(String.Format("Inizio associazione anagrafiche di {0} regs.", regs.Count()));

            AssociatePruFru(regs);

            _log.Info(String.Format("Associazione anagrafiche di {0} regs completata.", regs.Count()));

            #endregion

            #region 5. Inserimento delle chiusure automatiche a chiusura causali

            _log.Info("Inserimento delle chiusure automatiche a chiusura causali");

            ManageActivitiesAutoClosures(regs);

            #endregion

            #region 7. Disaccoppiamento registrazioni e definizione tipi di base

            // sono disaccoppiate le registrazioni da processare e sulle stesse è impostato il tipo base (attività/ore)
            _log.Info("Inizio disaccoppiamento registrazioni e definizione tipi di base.");

            DecoupleRegsAndSetType(regs);

            _log.Info("Disaccoppiamento regs terminato.");

            #endregion

            #region 8. Abbinamento delle registrazioni di tipo e definizione dei passaggi

            // sono accoppiate tra di loro tutte le registrazioni ora da processare; inoltre sono gestiti
            // e preparati gli eventuli passaggi presenti
            _log.Info("Inizio accoppiamento registrazioni.");

            CoupleHourRegsAndManagePassages(regs);

            _log.Info("Accoppiamento registrazioni terminato.");

            #endregion

            #region 9. Salvataggio dei dati modificati a database e cancellazione viaggi

            // aggiornamento delle registrazione e cancellazione dei viaggi
            UpdateDataAndDeleteTrips(regs);

            #endregion

            #region 10. Arrotondamenti

            Round(regs);

            #endregion

            #region 11. Gestione Sovrapposizioni

            CheckOverlaps(regs);

            #endregion

            #region 12 Abbinamento delle attività

            ElaborateActivities(regs);

            #endregion

            #region 13 Gestione dei viaggi

            ElaborateTrips(regs);

            #endregion

            #region 14 Gestione degli arrotondamenti per durata

            RoundDuration(regs);

            #endregion

            #region 15 Cancellazione delle causali tappo marcate per la chiusura

            DeleteAllActivitiesMarkedForDeletion();

            #endregion

            #region 16 Elaborazione attività per app

            //_elaborationService.ManagePostElaborateRegs(regs);

            #endregion

            return Errors;
        }

        public IEnumerable<Reg> CoupleBlockedRegs(IEnumerable<Reg> regs)
        {
            if (regs.Any())
            {
                //Si accoppiano le registrazioni macate per essere bloccate dal codice di acooppiamento
                ElaborateServiceHelper.CoupleBlockedRegs(regs.Where(reg => !String.IsNullOrEmpty(reg.Codice_Accoppiamento)).ToList());
            }

            return regs;
        }

        public IEnumerable<Reg> DeleteAllActivitiesAutoClosures(IEnumerable<Reg> regs)
        {
            // si procede solamente se il modulo delle attività risulta correnttamente attivato
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Att)
            {
                // se sono presenti delle registrazioni provenienti da causali nell'elenco passato come parametro
                if (regs.Any(reg => reg.Custom_Data_Reg == Common.Properties.Settings.Default.ActivityAutoClosureCustomData))
                {
                    // si tolgono tutti i riferimenti alle registrazioni da cancellare dalle registrazioni ad esse abbinate per evitare errori di integrità
                    // referenziale
                    IEnumerable<int> regIdsToDelete = regs
                        .Where(reg => reg.Custom_Data_Reg == Common.Properties.Settings.Default.ActivityAutoClosureCustomData)
                        .Select(reg => reg.Reg_Id)
                        .ToList();

                    var regsToUpdate = RepoManager.RegRepo.Find(dbReg => regIdsToDelete.Contains((int)dbReg.RiferimentoRRN_Reg) || regIdsToDelete.Contains((int)dbReg.RiferimentoRRN_Att)).ToList();

                    regsToUpdate.ForEach(reg => { reg.RiferimentoRRN_Reg = null; reg.RiferimentoRRN_Att = null; });

                    RepoManager.RegRepo.BulkUpdate(regsToUpdate);

                    var regsToDelete = regs.Where(reg => reg.Custom_Data_Reg == Common.Properties.Settings.Default.ActivityAutoClosureCustomData).ToList();

                    RepoManager.RegRepo.BulkDelete(regsToDelete);

                    // inoltre alla lista passata come parametro si procede a togliere 
                    // le registrazioni cancellate dal database
                    return regs.Where(reg => reg.Custom_Data_Reg != Common.Properties.Settings.Default.ActivityAutoClosureCustomData).ToList();
                }
            }

            // ritorno del valore calcolato dal metodo
            return regs;
        }

        public IEnumerable<Reg> DeleteDurationRounding(IEnumerable<Reg> regs)
        {
            var regsToDelete = regs.Where(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.ArrotDur).ToList();

            // Filtra le regv selezionando solo quelle di tipo arrotondamento per durata e cancella direttamente
            RepoManager.RegRepo.BulkDelete(regsToDelete);

            return regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.ArrotDur).ToList();

        }



        /// <summary>
        /// Associa le pru e le fru della registrazione alla loro relativa anagrafica
        /// </summary>
        /// <param name="regs">Registrazioni da associare</param>
        public void AssociatePruFru(IEnumerable<Reg> regs)
        {

            // se ci sono delle registrazioni su cui effettuare l'abbinamento
            if (regs.Any())
            {
                // calcolo della data massima presenti tra le registrazioni passate come parameto
                DateTime regMaxDate = regs.Max(reg => reg.Registrazione_Data_Ora_Orig_Reg).Date;

                // sono lette tutte le matricole portatili e fisse non disabilitate con data di associazione inferiore o uguale alla data massima da processare;
                // le anagrafiche così recuperate sono ordinate in senso discendente per data abilitazione, di modo da avere le più recenti in cima alla lista
                var pruCols = RepoManager.Pru_ColRepo.GetPruColLessThanDateOrderedDescByAbilitazione(regMaxDate).ToLookup(pru => pru.Pru_Id);

                var fruCants = RepoManager.Fru_CantRepo.GetFruCantLessThanDateOrderedDescByAbilitazione(regMaxDate).ToLookup(fru => fru.Fru_Id);

                // si cicla su tutte le registrazioni da associare (si considerano registrazioni da associare tutte le registrazioni
                // che hanno almeno o la Pru o la Fru)
                foreach (Reg reg in regs.Where(rg => rg.Fru_Id != null || rg.Pru_Id != null).ToList())
                {
                    // se la registrazione da processare ha collegata un'unità portatile
                    if (reg.Pru_Id != null)
                    {
                        #region Associazione del collaboratore

                        // in ogni caso si reinizializza sulla registrazione il collaboratore
                        reg.Col_Id = null;

                        // inizializzazione della variaibile di appoggio dell'associazione pru_col da impostare
                        Pru_Col currentPruCol = null;

                        // se nel dizionario con l'elenco delle unità portatili è presente l'unità portatile della registrazione
                        // allora si procede a ricercare la prima anagrafica con data assegnazione inferiore o uguale alla data ora fisica della timbratura
                        if (pruCols.Contains(reg.Pru_Id.Value))
                            currentPruCol = pruCols[reg.Pru_Id.Value].FirstOrDefault(pruCol => pruCol.Abilitazione_Data_Inizio_Pru_Col <= reg.Registrazione_Data_Ora_Fis_Reg);

                        // se è stata trovata l'associazione a un'unità portatile allora si imposta il collaboratore sulla registrazione;
                        // altrimenti si scrive l'errore
                        if (currentPruCol != null)
                            reg.Col_Id = currentPruCol.Col_Id;
                        else
                        {
                            string errorUserString = String.Format("Reg: {0} Matricola: {1} non associata", reg.Reg_Id, reg.Pru == null
                                ? Convert.ToInt32(reg.Pru_Id).ToString()
                                : reg.Pru.Codice_Pru);
                            _errors.Add(new KeyValuePair<string, string>(FunctionMessageEnum.Elaborate.ToString(), errorUserString));
                        }

                        #endregion

                    }

                    // se la registrazione da processare ha collegata un'unità fissa
                    if (reg.Fru_Id != null)
                    {

                        #region Associazione del cantiere

                        // in ogni caso si reinizializza sulla registrazione il cantiere
                        reg.Cant_Id = null;

                        // inizializzazione della variabile di appoggio dell'associazione cant_fru da impostare
                        Fru_Cant currentFruCant = null;

                        // se nel dizionario con l'elenco delle unità fisse è presente l'unità fissa della registrazione
                        // allora si procede a ricercare la prima anagrafica con data assegnazione inferiore o uguale alla data ora fisica della timbratura
                        if (fruCants.Contains(reg.Fru_Id.Value))
                            currentFruCant = fruCants[reg.Fru_Id.Value].FirstOrDefault(fruCant => fruCant.Abilitazione_Data_Inizio_Fru_Can <= reg.Registrazione_Data_Ora_Fis_Reg);

                        // se è stata trovata l'associazione a un'unità fissa allora si imposta il cantiere sulla registrazione;
                        // altrimenti si scrive l'errore
                        if (currentFruCant != null)
                        {
                            reg.Cant_Id = currentFruCant.Cant_Id;
                        }
                        else
                        {
                            string errorUserString = String.Format("Reg: {0} Matricola: {1} non associata", reg.Reg_Id, reg.Fru == null
                                ? Convert.ToInt32(reg.Pru_Id).ToString()
                                : reg.Fru.Codice_Fru);
                            _errors.Add(new KeyValuePair<string, string>(FunctionMessageEnum.Elaborate.ToString(), errorUserString));
                        }

                        #endregion

                    }
                }
            }
        }


        public void ManageActivitiesAutoClosures(IEnumerable<Reg> regs)
        {

            #region CHIUSURA AUTOMATICA CAUSALI
            // si procede ad effettuare le operazioni di generazione della chiusura automatica delle causali
            // solo se il modulo attività risulta abilitato
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Att)
            {

                // inizializzazione dell'elenco di registrazione nuove da aggiungere alle attualmente da processare
                var closures = new List<Reg>();

                // sono recuperati dall'elenco tutte le registrazioni provenienti da una causale
                IEnumerable<Reg> activityRegs = regs.Where(reg => reg.IsFromDeviceActivity);

                // inizializzazione delle attività di chiusura da cancellare
                var activitiesToDelete = new List<Reg>();

                // calcolo del tipo di chiusura delle causali configurato
                DeviceActivityClosingTypeEnum closingType = RepoManager.ParamRepo.ParametersRow.DeviceActivityClosingType;

                // si procede all'elaborazione solamente se è richiesto di effettuare la chiusura delle causali
                if (closingType != DeviceActivityClosingTypeEnum.NoClosure)
                {
                    // inizializzazione delle configurazioni del presenti nella scheda parametri
                    bool nocturneModuleActive = RepoManager.ParamRepo.ParametersRow.Abilita_Notturno;
                    NocturneTypeEnum nocturneTypeParam = nocturneModuleActive ? (NocturneTypeEnum)RepoManager.ParamRepo.ParametersRow.TipoNotturno : NocturneTypeEnum.None; // tipo notturno
                    TimeSpan nocturneParamTs = RepoManager.ParamRepo.ParametersRow.Default_Durata_Max_Gruppo_Notte_Ril ?? TimeSpan.Zero; // threshold del notturno (nuova mezzanotte) 

                    // per ognuna delle registrazioni provenienti da causali si procede, se le condizioni lo permettono,
                    // a generare la relativa chiusura di blocco
                    foreach (Reg activityReg in activityRegs)
                    {

                        #region Calcolo parametri specifici notturno per la regisrtazione

                        // inizializzazione del tipo di notturno utilizzato in fase di abbinamento (di default non impostato)
                        NocturneTypeEnum nocturneType = NocturneTypeEnum.None;

                        // inizializzazione della soglia (nuova mezzanotte) del notturno utilizzato in fase di abbinamento (di default a mezzanotte)
                        TimeSpan nocturneThreshold = TimeSpan.Zero;

                        // se è attivo il modulo del notturno
                        if (nocturneModuleActive)
                        {
                            #region Recupero cantiere e collaboratore della registrazione corrente

                            // recupero il cantiere relativo alla registrazione in elaborazione
                            int currentCantId = activityReg.Cant_Id ?? 0;
                            Cant currentCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == currentCantId);

                            // se il cantiere non risulta presente allora si passa al processo della registrazione successiva
                            if (currentCant == default(Cant))
                                continue;

                            // si recupera il collaboratore collegato al gruppo in processo
                            int colId = activityReg.Col_Id ?? 0;
                            Col currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == colId);

                            // se il collaboratore non risulta presente allora si passa al processo della registrazione successiva
                            if (currentCol == default(Col))
                                continue;

                            #endregion

                            // sono per prima cosa impostate le configurazioni del collaboratore;
                            // se il collaboratore non è stato configurato si scala sul cantiere, recuperando le sue configurazioni;
                            // in caso anche il cantiere non sia stato configurato si scala sui parametri generati, recuperando quelle configurazioni

                            // impostazione della configurazione del collaboratore
                            nocturneType = currentCol.NocturneTypeEnum;
                            nocturneThreshold = currentCol.Durata_Max_Gruppo_Notte_Ril_Col ?? TimeSpan.Zero;

                            // se il collaboratore non risulta configurato allora si procede al recupero delle configurazioni del cantiere
                            if (nocturneType == NocturneTypeEnum.None || nocturneType == NocturneTypeEnum.Disabled)
                            {
                                nocturneType = currentCant.NocturneTypeEnum;
                                nocturneThreshold = currentCant.Durata_Max_Gruppo_Notte_Ril_Can ?? TimeSpan.Zero;
                            }

                            // se il collaboratore e il cantiere non risultano configurati allora si procede al recupero delle configurazioni generali nei parametri
                            // (si controlla solamente il valore "None" perché se disabilitato su cantiere e collaboratore allora non lo si processa)
                            if (nocturneType == NocturneTypeEnum.None)
                            {
                                nocturneType = nocturneTypeParam;
                                nocturneThreshold = nocturneParamTs;
                            }
                        }

                        #endregion

                        // dall'elenco delle registrazioni è recuperato l'intero giorno dell'activity per il collaboratore specificato:
                        // - se il notturno risulta disabilitato allora si procede al recupero dei dati utilizzando il giorno solare;
                        // - se invece il notturno risulta disabilitato allora si procede al recupero a partire dalla nuova mezzanotte del giorno corrente per arrivare alla nuova mezzanontte
                        //   del giorno successivo (se poi l'ultima registrazione del giorno esteso è un'entrata e la prima del giorno successivo è un'uscita, si procede ad incorporare anche tale dato
                        List<Reg> colDayRegs = new List<Reg>();

                        if (nocturneType == NocturneTypeEnum.None || nocturneType == NocturneTypeEnum.Disabled)
                        {
                            colDayRegs = regs.Where(reg => reg.Col_Id == activityReg.Col_Id && reg.Registrazione_Data_Ora_Fis_Reg.Date == activityReg.Registrazione_Data_Ora_Fis_Reg.Date)
                                .OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();
                        }
                        //nel caso di notturno con NUOVA MEZZANOTTE
                        else if (nocturneType == NocturneTypeEnum.OverMidnight && nocturneThreshold.Ticks > 0)
                        {
                            // si calcolano le date di ricerca: la nuova mezzanotte della data dell'activity e la nuova mezzanotte della data del giorno successivo
                            var newStartActivityDay = new DateTime(activityReg.Registrazione_Data_Ora_Fis_Reg.Year, activityReg.Registrazione_Data_Ora_Fis_Reg.Month,
                                activityReg.Registrazione_Data_Ora_Fis_Reg.Day, nocturneThreshold.Hours, nocturneThreshold.Minutes, 0);


                            DateTime newEndActivityDay = newStartActivityDay.AddDays(1);

                            // si recuperano le registrazioni nell'intervallo del giorno spostato per il notturno
                            colDayRegs = regs.Where(reg => reg.Col_Id == activityReg.Col_Id && reg.Registrazione_Data_Ora_Fis_Reg >= newStartActivityDay
                                                           && reg.Registrazione_Data_Ora_Fis_Reg < newEndActivityDay).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                            // se l'ultima registrazione recuperata per il giorno è un'entrata
                            if (colDayRegs.Any())
                                if (colDayRegs.Last().FlagEURegTypeEnum == FlagEURegTypeEnum.E)
                                {
                                    // si recuperano i dati del giorno successivo e se la prima registrazione del girono è un'entrata
                                    // allora la si prende in carico per l'elaborazione
                                    DateTime nextDayStartDate = newStartActivityDay.AddDays(1);
                                    DateTime nextDayEndDate = newEndActivityDay.AddDays(1);
                                    List<Reg> nextDayRegs = regs.Where(reg => reg.Col_Id == activityReg.Col_Id && reg.Registrazione_Data_Ora_Fis_Reg >= nextDayStartDate
                                                                              && reg.Registrazione_Data_Ora_Fis_Reg < nextDayEndDate).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();
                                    if (nextDayRegs.Any())
                                        if (nextDayRegs.First().FlagEURegTypeEnum == FlagEURegTypeEnum.U)
                                            colDayRegs.Add(nextDayRegs.First());
                                }
                        }
                        //ATTENZIONE TODO:gestire il caso di notturno per durata
                        else if (nocturneType == NocturneTypeEnum.Duration)
                        {

                            //TODO DA

                            //List<Reg> regByCol = RepoManager.RegRepo.Find(r => r.Col_Id == activityReg.Col_Id &&
                            //r.Registrazione_Data_Ora_Fis_Reg.Date == activityReg.Registrazione_Data_Ora_Fis_Reg.Date).OrderBy(r => r.Registrazione_Data_Ora_Fis_Reg).ToList();

                            //if (regByCol.Any())
                            //{
                            //    //caso 1: Le registrazioni caadono tutte nello stesso giorno
                            //    if (regByCol.First().RiferimentoRRN_Reg == null)
                            //    {
                            //        var newStartActivityDay = regByCol.First().Registrazione_Data_Ora_Fis_Reg;

                            //        if (regByCol.Last().RiferimentoRRN_Reg != null)
                            //        {
                            //            var newEndActivityDay = regByCol.Last().Registrazione_Data_Ora_Fis_Reg;
                            //        }
                            //    }
                            //    //caso 2:
                            //}

                        }


                        // caricamento della registrazione successiva a quella proveniente da cusale in processo
                        int nextRegPosition = colDayRegs.IndexOf(activityReg) + 1;
                        Reg nextReg = nextRegPosition >= colDayRegs.Count ? default(Reg) : colDayRegs.ElementAt(nextRegPosition);

                        // caricamento della registrazione precedente 
                        int prevRegPosition = colDayRegs.IndexOf(activityReg) - 1;
                        Reg prevReg = prevRegPosition < 0 ? default(Reg) : colDayRegs.ElementAt(prevRegPosition);

                        // si procede con l'elaborazione solamente se sia la registrazione precedente che la successiva sono presenti
                        // (cioè se non si sta trattando la prima o l'ultima registrazione della giornata)
                        if (nextReg != default(Reg) && prevReg != default(Reg))
                        {
                            // se la registrazione successiva è un'uscita e la precedente è un'entrata e ha lo stesso cantiere della precedente
                            // allora si tratta della chiusura di un dispositivo
                            // fisso che non ha l'attività tappo, in questo caso non è necessario proseguire generando chiusure e si passa all'elaborazione
                            // dell'acitvity successiva
                            if (nextReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U && prevReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E && nextReg.Cant_Id == prevReg.Cant_Id)
                                continue;

                            // si marca la registrazione come da cancellare se si tratta di una registrazione di chiusura;
                            // si tratta di una registrazione di chiusura quando la reg precedente o la successiva sono un'uscita è un'uscita o
                            // se si tratta di un'attività tappo
                            if (prevReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U || activityReg.IsEndDeviceActivity)
                                activitiesToDelete.Add(activityReg);
                            else // altrimenti, se non si tratta di una chiusura
                            {

                                // si recupera la registrazione successiva alla prossima e si verifica se si tratta di una attività tappo
                                // o se si tratta di un'attività causale
                                int nextRegToNextPosition = colDayRegs.IndexOf(nextReg) + 1;
                                Reg nextRegToNext = nextRegToNextPosition >= colDayRegs.Count ? default(Reg) : colDayRegs.ElementAt(nextRegToNextPosition);
                                bool isNextToNextEndActivity = false;
                                if (nextRegToNext != default(Reg))
                                    isNextToNextEndActivity = nextRegToNext.IsEndDeviceActivity;
                                bool isNextToNextActivity = false;
                                if (nextRegToNext != default(Reg))
                                    isNextToNextActivity = nextRegToNext.IsFromDeviceActivity;

                                // si procede alla generazione della chiusura solamente se la registrazione successiva non è accompagnata da una chiusura;
                                // in quel caso sarà quella la chiusura della timbratura esistente
                                if (!isNextToNextEndActivity)
                                {
                                    // recupero dell'id del cantiere della registrazione precedente e successiva alla causale in processo
                                    int nextRegCantId = nextReg.Cant_Id ?? 0;
                                    int prevRegCantId = nextReg.Cant_Id ?? 0;

                                    // recupero dei cantieri della registrazione precedente e successiva alla causale in processo
                                    Cant nextRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == nextRegCantId);
                                    Cant prevRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == prevRegCantId);

                                    // si procede alla generazione della chiusura solamente se i cantieri della registrazione precedente e successiva non sono
                                    // attività
                                    if (nextRegCant != default(Cant) && prevRegCant != default(Cant))
                                        if (!nextRegCant.IsActivity && !prevRegCant.IsActivity)
                                        {

                                            // se la registrzione successiva è una possibile chiusura e quella successiva alla prossima è un'attività tappo, non è presente oppure non è
                                            // una causale, si passa all'attività successiva in quanto non è necesasrio effettuare una chiusura
                                            if (isNextToNextEndActivity || !isNextToNextActivity || nextRegToNext == default(Reg))
                                                continue;

                                            // si genera la chiusura solamente se è stato configurato di racchiudere in singoli blocche le causali;
                                            // in caso contrario si procede a marcare la registrazione successiva da cancellare (in quando quello che importa è l'attività nella chiusura
                                            // delle causali in una reg unica)
                                            if (closingType == DeviceActivityClosingTypeEnum.SingleActivty)
                                            {
                                                // a questo punto tutte le condizioni per la chiusura sono verificate;
                                                // si genera allora una registrazione di chiusura della precedente all'ora della successiva,
                                                // la quale a sua volta viene avanzata di un secondo se necessario

                                                // generazione di una nuova reg a chiusura con i dati d'entrata tranne l'uscita
                                                Reg newReg = RepoManager.RegRepo.Init();
                                                CommonService.DuplicateEntity(prevReg, newReg);
                                                newReg.RiferimentoRRN_Reg = null;
                                                newReg.Reg_Id = 0;
                                                newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(nextReg.Registrazione_Data_Ora_Fis_Reg.Year, nextReg.Registrazione_Data_Ora_Fis_Reg.Month,
                                                    nextReg.Registrazione_Data_Ora_Fis_Reg.Day, nextReg.Registrazione_Data_Ora_Fis_Reg.Hour, nextReg.Registrazione_Data_Ora_Fis_Reg.Minute, 0);
                                                newReg.Data_Registrazione_Reg = DateTime.Now;
                                                newReg.Custom_Data_Reg = Common.Properties.Settings.Default.ActivityAutoClosureCustomData;
                                                newReg.Codice_Accoppiamento = Common.Properties.Settings.Default.ActivityAutoClosureCustomData;
                                                newReg.Flag_EU_Reg = prevReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? "U" : null;

                                                // aggiunta della registrazione generata all'elenco
                                                closures.Add(newReg);

                                                // se la registrazione nuova e quella successiva hanno la stessa ora, si aggiunge un secondo alla successiva per evitare sovrapposzione
                                                if (nextReg.Registrazione_Data_Ora_Fis_Reg == newReg.Registrazione_Data_Ora_Fis_Reg)
                                                    nextReg.Registrazione_Data_Ora_Fis_Reg = nextReg.Registrazione_Data_Ora_Fis_Reg.AddSeconds(1);
                                            }
                                            else
                                            {
                                                // in caso di chiusura delle causali in un'unica registrazione si procede a cancellare ciò che si sarebbe dovuto chiudere, di modo da lasciare solamente
                                                // l'attività e l'entrata/uscita che le contiene (sono svuotati gli abbinamenti per evitare problemi di coerenza in cancellazione).
                                                // si procede alla cancellazione naturalmente solamente non si tratta di un nuovo inizio e quindi se la registrazione precedente non è un'attività
                                                // tappo o un'uscita
                                                if (!prevReg.IsEndDeviceActivity && prevReg.FlagEURegTypeEnum != FlagEURegTypeEnum.U)
                                                    activitiesToDelete.Add(nextReg);
                                            }
                                        }
                                }
                            }
                        }
                        else
                        {
                            // se la registrazione successiva risulta vuota (ultima della giornata) e la corrente è un'attività tappo allora la si marca per la cancellazione
                            if (activityReg.IsEndDeviceActivity)
                                activitiesToDelete.Add(activityReg);
                        }
                    }

                    /*
                     * 
                     * 
                     * 
                     * CONTROLLARE QUESTA PARTE!!!!!!!!!!!!!!!!!!!!!
                     * 
                     * 
                     * 
                     */

                    //// marcatura per la cancellazione delle eventuali attività di chiusura trovate sia dall'elenco da elaboarare che da database
                    //if (activitiesToDelete.Any())
                    //{
                    //    foreach (Reg reg in activitiesToDelete)
                    //    {
                    //        regs.Remove(reg);
                    //        reg.Custom_Data_Reg = Settings.Default.ActivityMarkedForDeletionCustomData;
                    //    }
                    //    Update(activitiesToDelete, true);
                    //}

                    //// se sono state generate delle registrazioni di confronto
                    //// allora le si aggiungono alle registrazioni dopo averle salvate a database come le altre (utilizzando il codice di accoppiamento temporaneo precedentemente utilizzato)
                    //Add(closures, true);
                    //closures = Find(reg => reg.Codice_Accoppiamento == Common.Properties.Settings.Default.ActivityAutoClosureCustomData).ToList();
                    //closures.ForEach(reg => reg.Codice_Accoppiamento = null);
                    //regs.AddRange(closures);
                }
            }
            #endregion

            #region CHIUSURA AUTOMATICA REGISTRAIZONI


            #region CHIUSURA AUTOMATICA SUL CANTIERE SEDE
            // Se è attiva la personalizzazione che prevede  la chiusura automatica sul cantiere sede
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresEnum) == (int)AutoClosuresEnum.Sede)
            {

                // inizializzazione dell'elenco di registrazione nuove da aggiungere alle attualmente da processare
                var closures = new List<Reg>();

                //vengono recuperate solo le registrazioni no passaggi, viaggi, attività...
                IEnumerable<Reg> regsToClose = RepoManager.RegRepo.Find(reg => reg.Registrazione_Tipo_Reg == 0).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg);

                string tmpCoupleCode = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "TmpCoupleCode");
                string autoGeneratedDataStart = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "AutoGeneratedRegCustomData");


                // inizializzazione delle configurazioni del presenti nella scheda parametri
                //viene controllato se è abilitato il nattoruno
                bool nocturneModuleActive = RepoManager.ParamRepo.ParametersRow.Abilita_Notturno;
                // tipo notturno
                NocturneTypeEnum nocturneTypeParam = nocturneModuleActive ? (NocturneTypeEnum)RepoManager.ParamRepo.ParametersRow.TipoNotturno : NocturneTypeEnum.None;
                // threshold del notturno (nuova mezzanotte) 
                TimeSpan nocturneParamTs = RepoManager.ParamRepo.ParametersRow.Default_Durata_Max_Gruppo_Notte_Ril ?? TimeSpan.Zero;


                // per ognuna delle registrazioni provenienti da causali si procede, se le condizioni lo permettono,
                // a generare la relativa chiusura di blocco

                //le registrazioni vengono regruppate per collaboratore
                foreach (var regByCol in regsToClose.GroupBy(reg => reg.Col_Id))
                {
                    //le registrazioni raggruppate per collaboratore vengono ragruppate per data
                    foreach (var regByColDate in regByCol.GroupBy(reg => reg.Registrazione_Data_Ora_Fis_Reg.Date))
                    {
                        //indice delle registrazioni allinterno della lista
                        int index = 0;

                        //variabile booleana che indica se chiudere oppure no la timbratura
                        bool doNotClose = false;

                        //viene presa la singola registrazione del giorno per il collaboratore specifico
                        foreach (var reg in regByColDate)
                        {

                            // caricamento della registrazione successiva a quella proveniente da timbratura
                            int currentRegPosition = index;

                            //viene estratta la registrazione corrente
                            Reg currentReg = reg;

                            // caricamento della registrazione successiva a quella proveniente da timbratura
                            int nextRegPosition = currentRegPosition + 1;

                            //se l'indice della registrazione seguente va oltre il numero totale di registrazioni allora restituisco una registrazione di default
                            //altrimenti restiruisco la registrazione corrispondente all'indice
                            Reg nextReg = nextRegPosition >= regByColDate.Count() ? default(Reg) : regByColDate.ElementAt(nextRegPosition);

                            //viene incrementato l'indice delle registrazioni all'interno della lista
                            index++;

                            //viene controllato se la registrazione corrente è valida altrimenti non faccio nulla
                            if (currentReg != default(Reg))
                            {
                                //viene controllato se la registrazione successiva è valida 
                                if (nextReg != default(Reg))
                                {
                                    // recupero dell'id del cantiere della registrazione precedente e successiva alla causale in processo
                                    int nextRegCantId = nextReg.Cant_Id ?? 0;
                                    int currentRegCantId = currentReg.Cant_Id ?? 0;

                                    // recupero dei cantieri della registrazione precedente e successiva alla causale in processo
                                    Cant nextRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == nextRegCantId);

                                    Cant currentRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == currentRegCantId);

                                    //se il cantiere attuale è una sede e il successivo no viene effettuata una chiususra
                                    if ((nextRegCant != default(Cant) && currentRegCant != default(Cant)))
                                    {
                                        //se la registrazione corrente è una sede
                                        if (currentRegCant.Tipo_Cantiere_Can == "SEDE")
                                        {
                                            //se il cantiere successivo NON è una sede
                                            if (nextRegCant.Tipo_Cantiere_Can != "SEDE")
                                            {
                                                //se è richiesta la chiusura
                                                if (!doNotClose)
                                                {

                                                    // generazione di una nuova reg a chiusura con i dati d'entrata tranne l'uscita
                                                    Reg newReg = RepoManager.RegRepo.Init();

                                                    //duplicazione delle reg passate come parametro
                                                    CommonService.DuplicateEntity(currentReg, newReg);
                                                    newReg.RiferimentoRRN_Reg = null;
                                                    newReg.Reg_Id = 0;
                                                    newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month,
                                                        currentReg.Registrazione_Data_Ora_Fis_Reg.Day, currentReg.Registrazione_Data_Ora_Fis_Reg.Hour, currentReg.Registrazione_Data_Ora_Fis_Reg.Minute, currentReg.Registrazione_Data_Ora_Fis_Reg.Second + 1);
                                                    newReg.Data_Registrazione_Reg = DateTime.Now;
                                                    newReg.Codice_Accoppiamento = tmpCoupleCode; // inserisco nella registrazione un codice accoppiamento fittizio per poi recuperarle dopo l'inserimento a db
                                                    newReg.Custom_Data_Reg = autoGeneratedDataStart;
                                                    newReg.Flag_EU_Reg = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? "U" : null;

                                                    // aggiunta della registrazione generata all'elenco
                                                    closures.Add(newReg);

                                                }
                                                else
                                                    doNotClose = false;
                                            }
                                            //se il cantiere successivo è sede e anche quello corrente è sede allora non è necessaria fare la chiusura
                                            else
                                                doNotClose = !doNotClose;
                                        }
                                        else
                                            doNotClose = false;
                                    }
                                }

                                else
                                {

                                    // recupero dell'id del cantiere della registrazione successiva alla timbrature processata
                                    int currentCantId = currentReg.Cant_Id ?? 0;

                                    //viene recuperato il cantiere corrispondente all'unica registrazione presente
                                    Cant currentRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == currentCantId);

                                    // si procede alla generazione della chiusura solamente se i cantieri della registrazione attuale è una sede e la successiva no
                                    if (currentRegCant != default(Cant))
                                    {
                                        //se si tratta di un cantiere tipo sede
                                        if (currentRegCant.Tipo_Cantiere_Can == "SEDE" && !doNotClose)
                                        {

                                            // generazione di una nuova reg a chiusura con i dati d'entrata tranne l'uscita
                                            Reg newReg = RepoManager.RegRepo.Init();
                                            //duplicazione delle reg passate come parametro
                                            CommonService.DuplicateEntity(currentReg, newReg);
                                            newReg.Reg_Id = 0;
                                            //data ore uguali alla reg precedente +59 secondi
                                            newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month,
                                                currentReg.Registrazione_Data_Ora_Fis_Reg.Day, currentReg.Registrazione_Data_Ora_Fis_Reg.Hour, currentReg.Registrazione_Data_Ora_Fis_Reg.Minute, currentReg.Registrazione_Data_Ora_Fis_Reg.Second + 1);
                                            newReg.Data_Registrazione_Reg = DateTime.Now;
                                            newReg.Codice_Accoppiamento = tmpCoupleCode; // inserisco nella registrazione un codice accoppiamento fittizio per poi recuperarle dopo l'inserimento a db
                                            newReg.Custom_Data_Reg = autoGeneratedDataStart;
                                            newReg.Flag_EU_Reg = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? "U" : null;

                                            // aggiunta della registrazione generata all'elenco
                                            closures.Add(newReg);

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                #endregion

                //le registrazioni che effettuano una chiusura vengono aggiunte al database
                RepoManager.RegRepo.BulkInsert(closures);

                //vengono estratte dal db solo le registrazioni di chiusura e il codice di accopiamento viene messo a null
                closures = RepoManager.RegRepo.Find(reg => reg.Codice_Accoppiamento == tmpCoupleCode).ToList();
                closures.ForEach(reg => reg.Codice_Accoppiamento = null);

                //vengonoa aggiunte le registrazioni di chiusura nella lista delle registrazioni da processare.
                //regs.AddRange(closures);  /(Ocio anche qui)
            }
        }

        /// <summary>
        /// Disaccoppia le specifiche registrazioni impostando il tipo registrazione ora o attività in base al cantiere di riferimento.
        /// </summary>
        /// <param name="regs">Le registrazioni da processare.</param>
        /// <param name="startDate">La prima data da cui partono le registrazioni dell'elaborazione che si sta trattando.</param>
        /// <param name="endDate">L'ultima data da cui partono le registrazioni dell'elaborazione che si sta trattando.</param>
        public void DecoupleRegsAndSetType(IEnumerable<Reg> regs)
        {
            // si recupera l'abilitazione o meno del modulo attività
            bool attModuleActive = RepoManager.ParamRepo.ParametersRow.Abilita_Att;

            // si recuperano tutti i cantierei attività presenti all'interno dell'applicativo
            HashSet<int> activityCantIds = RepoManager.CantRepo.GetAllActivitiesCantIds().ToHashSet();

            // si recuperano i parametri generali di notturno
            Tuple<bool, NocturneTypeEnum, TimeSpan, TimeSpan> nocturneGlobalConfiguration = RepoManager.ParamRepo.NocturneGeneralConfiguration;

            // per ogni registrazione da processare (cioè non viaggio)
            regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip).ToList().ForEach(reg =>
            {
                // si procede a disaccoppiare la registrazione solamente se risulta necessario farlo
                if (IsToDecuple(reg, regs.Min(r => r.Registrazione_Data_Ora_Fis_Reg), regs.Max(r => r.Registrazione_Data_Ora_Fis_Reg), nocturneGlobalConfiguration))
                {
                    // la timbratura processata è disassociata, e cioè gli sono tolti tutti i riferimenti
                    // di collegamento ad altre timbrature
                    reg.RiferimentoRRN_Reg = null; // si svuota l'eventuale collegamento della registrazione con un'altra registrazione
                    reg.RiferimentoRRN_Att = null; // si svuota l'eventuale collegamento dell'attività con una registrazione
                    reg.Registrazione_Stato_Reg = (int)RegStateEnum.None; // si imposta lo stato della registrazione a non associato

                    // in base al tipo di cantiere si imposta il tipo della registrazione
                    // - se il cantiere non è valorizzato e non è registrato tra le attività allora il tipo base è "ore"
                    // - se invece il cantiere è valorizzato ed è registrato tra le attività allora il tipo è attività

                    // calcolo dell'id cantiere da verificare nell'anagrafica cantieri; se il cantiere non è valorizzato sulla registrazione
                    // allora si imposta il valore 0 (cioè un valore mai presente tra gli id)
                    int cantIdToSearch = reg.Cant_Id ?? 0;

                    // si imposta il tipo attività se il cantiere da ricercare è presente tra quelli marcati come attività e il modulo attività è attivo;
                    // altrimenti si imposta il tipo ore
                    reg.Registrazione_Tipo_Reg = activityCantIds.Contains(cantIdToSearch) && attModuleActive ? (int)RegTypeEnum.Att : (int)RegTypeEnum.None;

                    // si svuota l'eventuale collegamento dell'attività con il cantiere di riferimento della timbratura associata se non si tratta di una attività;
                    // se si tratta di un'ora normale si imposta al cantiere attuale
                    reg.Att_Id = reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Att ? null : reg.Cant_Id;
                }
            });
        }

        public void CoupleHourRegsAndManagePassages(IEnumerable<Reg> regs)
        {
            // si inizializza la variabile che indica l'abilitazione del modulo di gestione dei passaggi
            bool passModuleActive = RepoManager.ParamRepo.ParametersRow.Abilita_Pass;
            Tuple<bool, NocturneTypeEnum, TimeSpan, TimeSpan> nocturneGlobalConfiguration = RepoManager.ParamRepo.NocturneGeneralConfiguration;
            bool nocturneModuleActive = nocturneGlobalConfiguration.Item1;

            // inizializzazione delle configurazioni del presenti nella scheda parametri
            NocturneTypeEnum nocturneTypeParam = nocturneGlobalConfiguration.Item2; // tipo notturno
            TimeSpan nocturneParamTs = nocturneGlobalConfiguration.Item3; // threshold del notturno (nuova mezzanotte) 
            //durata del notturno
            TimeSpan nocturneDurationParamTs = nocturneGlobalConfiguration.Item4;
            TimeSpan maxElapsedParam = RepoManager.ParamRepo.ParametersRow.Default_Durata_Max_Ril ?? TimeSpan.Zero; // durata massima della registrazione

            // si processano solamente le registrazioni abbinabili (cioè non attività, non viaggi, con collaboratore e cantiere)
            IEnumerable<Reg> regsToCouple = regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att && reg.Cant_Id.HasValue && reg.Col_Id.HasValue).ToList();

            // le registrazioni sono processate per collaboratore
            IEnumerable<IGrouping<Col, Reg>> regsGroupedByCol = regsToCouple.GroupBy(reg => reg.Col).ToList();

            // si cicla su tutte le registrazioni da accoppiare raggruppate per collaboratore
            foreach (IGrouping<Col, Reg> colGroup in regsGroupedByCol)
            {
                Col currentCol = colGroup.Key;
                
                // si ordinano le registrazioni del gruppo per motivazione e data/ora
                // di modo da abbinare le timbrature coerentemente con la motivazione inserita
                List<Reg> orderedColRegs = colGroup.OrderBy(reg => reg.Motivazione_Reg_Id).ThenBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                // inizializzazione della variabile utilizzata per salvare la timbratura precedente rispetto a quella
                // correntemente processata dal ciclo
                Reg lastOpen = null;

                #region Gestione passaggi ed abbinamento delle registrazioni per collaboratore


                // ciclo di elaborazione di tutte le registrazione del collaboratore ordinate per motivazione e data ora
                foreach (Reg currentReg in orderedColRegs)
                {
                    int currentCantId = currentReg.Cant_Id ?? 0;
                    Cant currentCant = currentReg.Cant;
                    

                    // inizializzazione della variabile che segnala se la registrazione corrente risulta da processare
                    bool isToElaborate = true;

                    #region Gestione dei passaggi

                    // se il modulo dei passaggi risulta abilitato e la registrazione corrente non è marcata come attività
                    if (passModuleActive)
                    {
                        isToElaborate = CoupleRegsHelper.CoupleIfPassage(currentReg);
                    }

                    #endregion

                    #region Verifica conformità registrazione per elaborazione

                    // inizializzazione del tipo di notturno utilizzato in fase di abbinamento (di default non impostato)
                    NocturneTypeEnum nocturneType = CoupleRegsHelper.GelLocalNoctureType(currentCol, currentCant);

                    // inizializzazione della soglia (nuova mezzanotte) del notturno utilizzato in fase di abbinamento (di default a mezzanotte)
                    TimeSpan nocturneThreshold = CoupleRegsHelper.GetNocturneLocalThreshold(currentCol, currentCant);

                    //inizilizzazione della durata del notturno
                    TimeSpan nocturneDuration = CoupleRegsHelper.GetLocalNocturneDuration(currentCol, currentCant);

                    // inizializzazione dei valori di durata massima e minima della registrazione
                    TimeSpan maxElapsed = CoupleRegsHelper.GetLocalMaxDurataReg(currentCol, currentCant);
                    TimeSpan minElapsed = CoupleRegsHelper.GetLocalMinDurataReg(currentCol, currentCant);

                    // si procede alla verifica della confromità per l'elaborazione e il calcolo dei parametri solamente se precedentemente
                    // il dato non è stati marcato come passaggio
                    if (isToElaborate)
                    {
                        #region Controllo numero dispari in base a customizzazione

                        // si calcolano gli estremi da verificare in base al parametro notturno impostato:
                        // 1. se il notturno non è impostato allora si procede al calcolo con il giorno corrente (00:00 - 23:59)
                        // 2. se il notturno è abilitato si calcola il giorno corrente (dal threshold) al giorno seguente (al threshold)
                        DateTime minSearchDate = CoupleRegsHelper.GetNocturneMinSeachDate(currentReg, nocturneType, nocturneThreshold);
                        DateTime maxSearchDate = CoupleRegsHelper.GetNocturneMaxSeachDate(currentReg, nocturneType, nocturneThreshold);


                        //TODO: nell'eventualità di notturno per durata  bisognerebbe prima accopiare il tutto e calcolare poi a posteriori i limiti di tempo

                        // se la registrazione è stata marcata per essere processata ed è attiva la personalizzazione relativa al non accoppiamento delle registrazioni
                        // dispari (il numero delle registrazioni da accopiare deve essere pari)
                        int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DoNotCoupleIfDayOddsRegsEnum);

                        if (isToElaborate && customizationVersion == (int)DoNotCoupleIfDayOddsRegsEnum.DoNotCouple && nocturneType != NocturneTypeEnum.Duration)
                        {
                            // se il numero di registrazioni indicate nel giorno non bloccate per lo stesso collaboratore/cantiere/data è dispari allora
                            // si segna come la registrazione come da non accoppiare
                            int regsToCoupleCount = orderedColRegs.Count(reg => !reg.Registrazione_Bloccata
                                && reg.Registrazione_Data_Ora_Fis_Reg >= minSearchDate && reg.Registrazione_Data_Ora_Fis_Reg <= maxSearchDate
                                && reg.Col_Id == currentReg.Col_Id && reg.Cant_Id == currentReg.Cant_Id);

                            if (regsToCoupleCount % 2 != 0)
                                isToElaborate = false;

                        }

                        // se è richiesto di non accoppiare le registrazioni per un collaboratore che nel giorno sono sotto una certa soglia (ad esempio vengono accopiate solamente se le registrazioni sono almeno 4)
                        int numberRegCustVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DoNotCoupleIfDaysRegVUnderEnum);
                        if (isToElaborate && numberRegCustVersion == (int)DoNotCoupleIfDaysRegVUnderEnum.DoNotCoupleIfUnder && nocturneType != NocturneTypeEnum.Duration)
                        {
                            // se il giorno che si sta bloccando non è un fine settimana (sabato o domenica) oppure un festivo
                            if (minSearchDate.DayOfWeek != DayOfWeek.Saturday
                                && minSearchDate.DayOfWeek != DayOfWeek.Sunday
                                && !RepoManager.Tab_FestiviRepo.DbSet.Any(tf => tf.Giorno_Tab_Festivi == minSearchDate.Date))
                            {
                                // se il numero di registrazioni indicate nel giorno non bloccate per lo stesso collaboratore sono sotto la soglia di parametro
                                // allora non si procede all'abbinamento
                                int regsToCoupleCount = orderedColRegs.Count(reg => !reg.Registrazione_Bloccata
                                                                                 && reg.Registrazione_Data_Ora_Fis_Reg >= minSearchDate && reg.Registrazione_Data_Ora_Fis_Reg <= maxSearchDate
                                                                                 && reg.Col_Id == currentReg.Col_Id);
                                if (regsToCoupleCount < Convert.ToInt32(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.DoNotCoupleIfDaysRegVUnderEnum, "MinimunReg")))
                                    isToElaborate = false;
                            }
                        }


                        #endregion
                    }

                    #endregion

                    if (CoupleRegsHelper.IsCurrentRegAssociated(currentReg))
                        continue;

                    // se è stato superato il controllo di conformità allora si procede all'elaborazione dei dati
                    if (!isToElaborate)
                        continue;

                    #region Abbinamento delle registrazioni

                    // se si sta processando una nuova reg, e cioè:
                    //   - si tratta di una registrazione di entrata o senza flag di direzione
                    //   - ... e si tratta della prima elaborazione per una coppia
                    // allora la si setta come entrata di una possibile coppia
                    // altrimenti, se non si tratta di una prima registrazione di coppia ed è un'uscita o è senza direzione, si cerca di effettuare l'abbinamento
                    // in base ai parametri specifcati;
                    // se nessuna delle due precedenti condizioi risulta verficata allora si tratta dell'entrata di una nuova coppia e come tale la si setta
                    if (CoupleRegsHelper.IsCurrentRegEntranceByFlag(currentReg) && lastOpen == null)
                    {
                        lastOpen = currentReg;
                    }
                    else if (CoupleRegsHelper.IsCurrentRegExitByFlag(currentReg))
                    {
                        // se non è stata già indicata un'entrata allora si può procedere con il tentativo di abbinamento
                        if (lastOpen == null)
                            continue;

                        // se il notturno risulta abilitato e correttamente configurato (cioè non riporta la mezzanotte)
                        if (CoupleRegsHelper.IsNocturneConfigured(nocturneType, nocturneThreshold, nocturneDuration))
                        {

                            #region Abbinamento delle registrazioni in caso di notturno abilitato con NUOVA MEZZANOTTE

                            // nel caso il notturno sia configurato come x ore dopo mezzanotte (per ora viene gestitio solo questo tipo di notturno,
                            // ma in futuro potranno essere distinti)
                            if (nocturneType == NocturneTypeEnum.OverMidnight)
                            {
                                lastOpen = CoupleRegsHelper.ManageMidnightNocturneAssociation(lastOpen, currentReg, minElapsed, maxElapsed, nocturneThreshold, nocturneType);
                            }

                            #endregion

                            #region Abbinamento delle registrazioni in caso di notturno abilitato per DURATA

                            // nel caso il notturno sia configurato il calcolo del notturno per durata
                            else if (nocturneType == NocturneTypeEnum.Duration)
                            {
                                lastOpen = CoupleRegsHelper.ManageNocturneDurationAssociation(lastOpen, currentReg, minElapsed, maxElapsed,nocturneDuration, nocturneThreshold, nocturneType);

                            }

                            #endregion

                        }
                        else
                        {

                            #region Abbinamento delle registrazioni in caso di notturno disabilitato

                            lastOpen = CoupleRegsHelper.ManageNoNocturneAssociation(currentReg, lastOpen, minElapsed, maxElapsed, nocturneType);

                            #endregion
                        }
                    }
                    else
                    {
                        // registrazione non coerente con flag entrata e/o processo; la si tratta come una nuova entrata
                        lastOpen = currentReg;
                    }

                    #endregion

                }

                #endregion

            }
        }


        /// <summary>
        /// Aggiorna a database le registraioni passate come parametro cancellato, in caso di abilitazione viaggi, i viaggi in esse contenuti.
        /// </summary>
        /// <param name="regs">Le registrazioni da aggiornare e di cui cancellare i viaggi.</param>
        public void UpdateDataAndDeleteTrips(IEnumerable<Reg> regs)
        {

            #region Cancellazione dei viaggi

            // sono cancellati i viaggi solamente se il flag di generazione automaticae ed il modulo dei viaggi sono attivi
            if (RepoManager.ParamRepo.ParametersRow.Flag_Calcolo_Viaggi && RepoManager.ParamRepo.ParametersRow.Abilita_Viaggi)
            {
                // se ci sono dei viaggi da cancellare, procedo alla loro eliminazione
                IEnumerable<Reg> tripsToDelete = regs.Where(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip).ToList();

                if (tripsToDelete.Any())
                    RepoManager.RegRepo.BulkDelete(tripsToDelete);
            }

            #endregion

            #region Salvataggio registrazioni aggiornate

            // se ci sono delle registrazioni da salvare, le salvo
            IEnumerable<Reg> regsToUpdate = regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip).ToList();

            if (regsToUpdate.Any())
                RepoManager.RegRepo.BulkUpdate(regsToUpdate);


            #endregion

        }

        /// <summary>
        /// Recupera tutte le reg_v su cui effettuare i post processi a partire dalle registrazioni specifiche.
        /// </summary>
        /// <param name="regs">Le registrazioni specifiche da post processare.</param>
        /// <returns>L'elenco delle reg_v su cui effettuare i post processi.</returns>
        public IEnumerable<Reg_V> GetRegVsToPostProcess(IEnumerable<Reg> regs)
        {
            // inizializzazione dell'elenco di reg_v ritorno del metodo
            List<Reg_V> regVs = new List<Reg_V>();

            // si procede all'elaborazione solamente se ci sono delle registrazioni passate come parametro
            if (regs.Any())
            {

                // recupero di tutti gli id collaboratori presenti nelle registrazioni passate come parametro
                HashSet<int> colIds = regs.Where(reg => reg.Col_Id != null).Select(reg => reg.Col_Id).Distinct().Cast<int>().ToHashSet();

                // calcolo della data registrazione minima e massima presente nell'elenco (aggiungendo alla data di destinazione
                // un giorno per poter recuperare i dati dalla mezzanotte del giorno successivo indietro
                DateTime from = regs.Min(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date;
                DateTime to = regs.Max(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date.AddDays(1);

                // leggo le reg_v che rientrano nei giorni richiesti, associati e con il collaboratore presente nelle registrazioni
                // passate come paremtro e non bloccate (ottimizzando la ricerca per un solo collaboratore e per più di un
                // collaboratore) e non attività

                regVs = RepoManager.Reg_VRepo.Find(regv => regv.Data_Ora_Fis_E >= from && (regv.Data_Ora_Fis_U <= to || (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass && regv.Data_Ora_Fis_E <= to)) &&
                    regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass && colIds.Contains((int)regv.Col_Id) &&
                    !regv.Registrazione_Bloccata && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att, true).ToList();



            }


            // si ritornano le reg_v così calcolate
            return regVs;
        }

        /// <summary>
        /// Recupera tutte le reg_v su cui effettuare i post processi a partire dalle registrazioni specifiche.
        /// </summary>
        /// <param name="regs">Le registrazioni specifiche da post processare.</param>
        /// <returns>L'elenco delle reg_v su cui effettuare i post processi.</returns>
        public IEnumerable<Reg_V> GetRegVsForRounding(IEnumerable<Reg> regs)
        {
            // inizializzazione dell'elenco di reg_v ritorno del metodo
            IEnumerable<Reg_V> regVs = Enumerable.Empty<Reg_V>();

            // si procede all'elaborazione solamente se ci sono delle registrazioni passate come parametro
            if (regs.Any())
            {

                // recupero di tutti gli id collaboratori presenti nelle registrazioni passate come parametro
                IEnumerable<int?> colIds = regs.Select(reg => reg.Col_Id).Distinct().ToList();

                // calcolo della data registrazione minima e massima presente nell'elenco (aggiungendo alla data di destinazione
                // un giorno per poter recuperare i dati dalla mezzanotte del giorno successivo indietro
                DateTime from = regs.Min(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date;
                DateTime to = regs.Max(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date.AddDays(1);

                // leggo le reg_v che rientrano nei giorni richiesti, associati e con il collaboratore presente nelle registrazioni
                // passate come paremtro e non bloccate (ottimizzando la ricerca per un solo collaboratore e per più di un
                // collaboratore) e non attività

                regVs = RepoManager.Reg_VRepo.Find(regv =>
                                regv.Data_Ora_Fis_E >= from
                                && (regv.Data_Ora_Fis_U <= to
                                || (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass && regv.Data_Ora_Fis_E <= to)
                                || (regv.Data_Ora_Fis_U == null && regv.Registrazione_Stato_Reg == (int)RegStateEnum.None && regv.Data_Ora_Fis_E < to))
                                && colIds.Contains(regv.Col_Id)
                                && !regv.Registrazione_Bloccata
                                && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att, true).ToList();


            }

            // si ritornano le reg_v così calcolate
            return regVs;
        }

        public void Round(IEnumerable<Reg> regs)
        {
            if (!RepoManager.ParamRepo.ParametersRow.Abilita_Arrotondamenti)
                return;

            HashSet<Cant> cants = regs.Select(reg => reg.Cant).ToHashSet();
            HashSet<Col> cols = regs.Select(reg => reg.Col).ToHashSet();

            var regIds = regs.ToDictionary(reg => reg.Reg_Id);

            var regVs = GetRegVsForRounding(regs);

            if (regVs.Count() > 0)
                return;

            var groupByColRegs = regVs.GroupBy(reg => reg.Col_Id);

            //Recupero i Parametri di Arrotondamento Generali da Scheda parametri
            RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;
            int paramThresholdStart = RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_I.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_I.Value : -1;
            int paramThresholdEnd = RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_F.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_F.Value : -1;
            int paramTinutesStart = RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_I.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_I.Value : -1;
            int paramTinutesEnd = RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_F.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_F.Value : -1;
            int utilizzoLimiteEntrata = RepoManager.ParamRepo.ParametersRow.Utilizzo_Limite_Entrata;
            int utilizzoLimiteUscita = RepoManager.ParamRepo.ParametersRow.Utilizzo_Limite_Uscita;
            int delayTollerance;

            // calcolo del mezzogiorno (utilizzato per la divisione mattutina e pomeridiana del limite d'entrata)
            TimeSpan midDay = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Inizio_Pomeriggio ?? new TimeSpan(12, 0, 0);
            TimeSpan midNight = new TimeSpan(0, 0, 0);

            foreach (var colGroup in groupByColRegs)
            {

                int colGroupId = colGroup.Key.HasValue ? colGroup.Key.Value : -1;

                if (colGroupId != -1)
                {
                    Col currentCol = cols.SingleOrDefault(col => col.Col_Id == colGroupId);

                    //Recupera la tolleranza del ritardo dal COL o dai PARAM, altrimenti la setta a 0
                    delayTollerance = currentCol.Ritardo_Tolleranza_Minuti_Col ?? (RepoManager.ParamRepo.ParametersRow.Ritardo_Tolleranza_Minuti ?? 0);

                    if (currentCol != null)
                    {
                        var groupByCantRegs = colGroup.GroupBy(reg => reg.Cant_Id);

                        foreach (var cantGroup in groupByCantRegs)
                        {
                            int cantGroupId = cantGroup.Key.HasValue ? cantGroup.Key.Value : -1;

                            if (cantGroupId != -1)
                            {
                                Cant currentCant = cants.SingleOrDefault(cant => cant.Cant_Id == cantGroupId);

                                if (currentCant != null)
                                {
                                    TimeSpan minEntryHour = currentCant.Limite_Entrata_Mattina_Cant.HasValue ? currentCant.Limite_Entrata_Mattina_Cant.Value : TimeSpan.Zero;

                                    //Recupero i Parametri di Arrotondamento del Collaboratore
                                    RoundingMethodEnum roundingEnum = currentCol.RoundingMethodEnum;
                                    int thresholdStart = currentCol.SogliaI_Col.HasValue ? currentCol.SogliaI_Col.Value : -1;
                                    int thresholdEnd = currentCol.SogliaF_Col.HasValue ? currentCol.SogliaF_Col.Value : -1;
                                    int minutesStart = currentCol.ArrotI_Col.HasValue ? currentCol.ArrotI_Col.Value : -1;
                                    int minutesEnd = currentCol.ArrotF_Col.HasValue ? currentCol.ArrotF_Col.Value : -1;

                                    if (roundingEnum == RoundingMethodEnum.None)
                                    {
                                        //Recupero i Parametri di Arrotondamento del Cantiere
                                        roundingEnum = currentCant.RoundingMethodEnum;
                                        thresholdStart = currentCant.Soglia_Arrot_Fig_Can.HasValue ? currentCant.Soglia_Arrot_Fig_Can.Value : -1;
                                        thresholdEnd = currentCant.Soglia_Arrot_Fig_F_Can.HasValue ? currentCant.Soglia_Arrot_Fig_F_Can.Value : -1;

                                        minutesStart = currentCant.Minuti_Tolleranza_Can.HasValue ? currentCant.Minuti_Tolleranza_Can.Value : -1;
                                        minutesEnd = currentCant.Minuti_Tolleranza_F_Can.HasValue ? currentCant.Minuti_Tolleranza_F_Can.Value : -1;
                                    }

                                    if (roundingEnum == RoundingMethodEnum.None)
                                    {
                                        roundingEnum = roundingParamEnum;

                                        thresholdStart = paramThresholdStart;
                                        thresholdEnd = paramThresholdEnd;
                                        minutesStart = paramTinutesStart;
                                        minutesEnd = paramTinutesEnd;
                                    }

                                    List<Reg_V> currentRegVs = cantGroup.OrderBy(regV => regV.Data_Ora_Fis_E).ToList();

                                    foreach (Reg_V currentRegV in currentRegVs)
                                    {
                                        Reg currentRegE = regIds[currentRegV.RegE];
                                        Reg currentRegU = null;

                                        if (currentRegE.Registrazione_Tipo_RegEnum != RegTypeEnum.Att && currentRegE.Registrazione_Tipo_RegEnum != RegTypeEnum.Pass && currentRegV.RegU != null)
                                        //Se NON è una Attività allora imposto DataOra Fig Uscita = Data Ora Fis Uscita
                                        {
                                            //currentRegU = regs.Single(reg => reg.Reg_Id == currentRegV.RegU);
                                            currentRegU = regIds[currentRegV.RegU.Value];
                                            currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fis_Reg;
                                        }

                                        //viene impostata la data ed ora FIGURATIVA uguale a quella fisica
                                        currentRegE.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fis_Reg;

                                        #region ARROTONDAMENTO ENTRATA/USCITA
                                        //se si è nel caso di arrotondamento sull'entrata ed uscita
                                        if (roundingEnum == RoundingMethodEnum.StartEnd)
                                        {
                                            #region 1.Arrotondo la Registrazione di Entrata

                                            //vengono estratti i minuti della corrente registrazione di entrata
                                            int currentRegEMin = currentRegE.Registrazione_Data_Ora_Fis_Reg.Minute;

                                            //creo una nuova variabile che rappresenterà il modulo dei minuti
                                            int moduleRegEMin = currentRegEMin;

                                            //viene controllato se i minuti di start inseriti sono maggiori di 0 (minuti arrotondamento entrata)
                                            if (minutesStart > 0)
                                                //viene calcolato il valore come resto della divisione tra i minuti reali e il valore dei parametri
                                                moduleRegEMin = currentRegEMin % minutesStart;

                                            //se si è in presenza di un valore della soglia sull'entrata valido(Soglia di entrata) 
                                            //se la soglia è uguale a zero si arrotonda sempre al limite successivo
                                            if (thresholdStart >= 0)
                                            {
                                                //se il modulo dei minuti è maggiore della soglia di entrata impostata nei parametri
                                                if (moduleRegEMin > thresholdStart)
                                                    //i nuovi minuti della registrazione figurativa di entrata sono uguali alla differenza tra minutesStart - moduleRegEMin
                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(minutesStart - moduleRegEMin);
                                                else
                                                    //altrimenti i minuti della registrazione figurativa di entrata sono uguali al modulo * -1
                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(moduleRegEMin * -1);
                                            }
                                            #endregion

                                            #region 2.Arrotondo la Registrazione di Uscita
                                            //se sono in presenza di una registrazione di uscita
                                            if (currentRegU != null)
                                            {
                                                //vengono estratti i minuti della registrazione di uscita
                                                int currentRegUMin = currentRegU.Registrazione_Data_Ora_Fis_Reg.Minute;
                                                int moduleRegUMin = currentRegUMin;

                                                // viene controllato se i minuti di start inseriti sono maggiori di 0
                                                if (minutesEnd > 0)
                                                    //viene calcolato il valore come resto della divisione tra i minuti reali e il valore dei parametri
                                                    moduleRegUMin = currentRegUMin % minutesEnd;

                                                //se la soglia di uscita è valorizzata
                                                if (thresholdEnd > 0)
                                                {
                                                    //se il modulo dei minuti è maggiore della soglia di uscita impostata nei parametri
                                                    if (moduleRegUMin > thresholdEnd)
                                                        //i nuovi minuti della registrazione figurativa di uscita sono uguali alla differenza tra minutesStart - moduleRegEMin
                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(minutesEnd - moduleRegUMin);
                                                    else
                                                        //altrimenti i minuti della registrazione figurativa di entrata sono uguali al modulo * -1
                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(moduleRegUMin * -1);
                                                }
                                            }
                                            #endregion

                                            #region 3.Gestione del limite d'entrata


                                            if ((utilizzoLimiteEntrata == (int)UtilizzoLimiteEntrata.LimiteEntrata) ||
                                                   (utilizzoLimiteEntrata == (int)UtilizzoLimiteEntrata.LimiteEntrataERitardo))

                                            {
                                                // calcolo dei dati di limite d'entrata riguardo la registrazione che si sta processando
                                                Dictionary<EntryLimitTypeEnum, EntryLimitData> entryLimitConfig = GetEntryLimitConifg(currentCant, currentCol, currentRegV.Data_Reg.Value, midDay);

                                                //primo limite della mattina, se non si è valorizzato il campo del limite resituisce mezzogiorno
                                                TimeSpan fistMorningLimit = new TimeSpan(12, 0, 0);

                                                // se è configurata la gestione del limite d'entrata (valorizzata o per la mattina, per il pomeriggio o per orario) e se la registrazione 
                                                // risulta abbinata allora si procede (se la registrazione d'entrata risulta presente) come segue:
                                                // - in caso di registrazione mattutina (ante metà giornata configurata) allora si verifica che, se configurato il limite d'entrata mattutino,
                                                //   la registrazione sia antecedente a tale ora; in questo caso l'ora figurativa dell'entrata viene spostata al limite d'entrata.
                                                // - in caso non si tratti di una registrazione mattutina (post metà giornata configurata) allora si verifica che, 
                                                //   se configurato il limite d'entrata pomeridiano e la registrazione abbinata sia a cavallo del limite e nella tolleranza esplicitata 
                                                //   (se non configurata si è sicuramente fuori tolleranza) allora si procede allo spostamento dell'ora figurativa d'entrata al limite d'entrata
                                                // al termine dell'operazione in ogni caso, se l'uscita risulta inferiore all'entrata, si procede al suo spostamento per far coincidere il dato.

                                                // se la registrazione risulta abbinata e ci sono dei limiti d'entrata configurati ed esiste una registrazione d'entrata
                                                if (currentRegV.Registrazione_Stato_Reg == (int)RegStateEnum.Ass && entryLimitConfig.Any(kvp => kvp.Value.IsConfigured) && currentRegE != null)
                                                {
                                                    // se la registrazione risulta essere mattutina ed è configurato il limite d'entrata mattutino,
                                                    // altrimenti se la registrazione risulta essere pomeridiana e risulta configurato n limite d'entrata pomeridiano
                                                    if (currentRegV.Data_Ora_Fis_E.TimeOfDay < midDay && entryLimitConfig[EntryLimitTypeEnum.Morning].IsConfigured)
                                                    {
                                                        //se si ha il limite d'entrata configurato viene impostato come limite mattutino il limite d'entrata
                                                        if (entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime != null)
                                                            fistMorningLimit = entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value;

                                                        // se l'ora figurativa dell'entrata è inferiore al limite d'entrata allora viene spostata al limite d'entrata;
                                                        if (currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value)
                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Hours,
                                                                entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Minutes,
                                                                0);
                                                    }
                                                    else if (currentRegV.Data_Ora_Fis_E.TimeOfDay >= midDay && entryLimitConfig[EntryLimitTypeEnum.Afternoon].IsConfigured)
                                                    {
                                                        // viene recuperata la tolleranza del limite d'entrata (se non configurata si contano le 12 ore per coprire l'intera mezza giornata)
                                                        TimeSpan entryLimitTollerance = entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTollerance ?? new TimeSpan(12, 0, 0);

                                                        // se la registrazione è a cavallo del limite d'entrata e in tolleranza
                                                        // allora l'ora figurativa viene spostata al limite d'entrata
                                                        if (currentRegV.Data_Ora_Fis_E.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value
                                                            && currentRegV.Data_Ora_Fis_U.Value.TimeOfDay > entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value
                                                            && (entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_E.TimeOfDay.Ticks) <= entryLimitTollerance.Ticks)
                                                        {
                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Hours,
                                                                entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Minutes,
                                                                0);
                                                        }

                                                        //se si è in presenza di una registrazione notturna
                                                        if (currentRegV.Data_Ora_Fis_U.Value.Date > currentRegV.Data_Ora_Fis_E.Date)
                                                        {
                                                            //se il limite pomeridiano è uguale o maggiore alla mezzanotte ma inferiore al limite mattutino ed entro la tolleranza
                                                            if (entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value >= midNight
                                                                     && entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value < fistMorningLimit
                                                                     && (entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_E.TimeOfDay.Ticks) <= entryLimitTollerance.Ticks)
                                                            {
                                                                //come registrazione figurativa di entrata viene presa la data dell'uscita e i minuti dati dal limite pomeridiano impostatao dai parametri
                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Hours,
                                                                entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Minutes,
                                                                0);

                                                            }

                                                            //se si ha una registrazione notturna ma il limite cade prima della mezzanotte
                                                            else if (currentRegV.Data_Ora_Fis_E.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value
                                                                && (entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_E.TimeOfDay.Ticks) <= entryLimitTollerance.Ticks)
                                                            {
                                                                //la registrazione figurativa prende la data dalla registrazione di entrata
                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Hours,
                                                                entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Minutes,
                                                                0);

                                                            }
                                                        }
                                                    }

                                                    // in ogni caso, al termine dell'operazione, se è presente una registrazione d'uscita
                                                    // e l'ora figurativa di questa è inferiore all'ora figurativa dell'entrata allora 
                                                    // si porta l'ora d'uscita all'ora d'entrata (a patto che si trovino nella stessa data - questione degli arrotondamenti a 00:00)
                                                    if (currentRegU != null)
                                                        if (currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay < currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay
                                                            && currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Date == currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Date)
                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fig_Reg;
                                                }
                                            }

                                            #endregion

                                            #region 4.Gestione limite di uscita

                                            if (utilizzoLimiteUscita == (int)UtilizzoLimiteUscita.LimiteUscita)
                                            {
                                                // calcolo dei dati di limite d'entrata riguardo la registrazione che si sta processando
                                                Dictionary<ExitLimitTypeEnum, ExitLimitData> exitLimitConfig = GetExitLimitConifg(currentCant, currentCol, currentRegV.Data_Reg.Value, midDay);

                                                //primo limite della mattina, se non si è valorizzato il campo del limite resituisce mezzogiorno
                                                TimeSpan fistMorningLimit = new TimeSpan(12, 0, 0);

                                                // se è configurata la gestione del limite d'uscita (valorizzata o per la mattina, per il pomeriggio o per orario) e se la registrazione 
                                                // risulta abbinata allora si procede (se la registrazione d'entrata risulta presente) come segue:
                                                // - in caso di registrazione mattutina (ante metà giornata configurata) allora si verifica che, se configurato il limite d'uscita mattutino,
                                                //   la registrazione sia seguente a tale ora; in questo caso l'ora figurativa dell'uscita viene spostata al limite d'uscita.
                                                // - in caso non si tratti di una registrazione mattutina (post metà giornata configurata) allora si verifica che, 
                                                //   se configurato il limite d'uscita pomeridiano e la registrazione abbinata sia a cavallo del limite e nella tolleranza esplicitata 
                                                //   (se non configurata si è sicuramente fuori tolleranza) allora si procede allo spostamento dell'ora figurativa d'uscita al limite d'entrata
                                                // al termine dell'operazione in ogni caso, se l'uscita risulta inferiore all'entrata, si procede al suo spostamento per far coincidere il dato.

                                                // se la registrazione risulta abbinata e ci sono dei limiti d'entrata configurati ed esiste una registrazione d'entrata
                                                if (currentRegV.Registrazione_Stato_Reg == (int)RegStateEnum.Ass && exitLimitConfig.Any(kvp => kvp.Value.IsConfigured) && currentRegU != null)
                                                {
                                                    // se la registrazione risulta essere mattutina ed è configurato il limite d'uscita mattutino,
                                                    // altrimenti se la registrazione risulta essere pomeridiana e risulta configurato n limite d'uscita pomeridiano
                                                    if (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay < midDay && exitLimitConfig[ExitLimitTypeEnum.Morning].IsConfigured)
                                                    {
                                                        TimeSpan exitLimitMorningTollerance = exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTollerance ?? new TimeSpan(12, 0, 0);

                                                        //se si ha il limite d'uscita configurato viene impostato come limite mattutino il limite d'uscita
                                                        if (exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime != null)
                                                            fistMorningLimit = exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value;

                                                        // se l'ora figurativa dell'uscita è maggiore del limite d'uscita allora viene spostata al limite d'uscita;
                                                        if (currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay > exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value
                                                            && (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Subtract(exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value) <= exitLimitMorningTollerance.Duration()))
                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value.Hours,
                                                                exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value.Minutes,
                                                                0);
                                                    }
                                                    else if (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay >= midDay && exitLimitConfig[ExitLimitTypeEnum.Afternoon].IsConfigured)
                                                    {
                                                        // viene recuperata la tolleranza del limite d'uscita (se non configurata si contano le 12 ore per coprire l'intera mezza giornata)
                                                        TimeSpan exitLimitAfternoonTollerance = exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTollerance ?? new TimeSpan(12, 0, 0);

                                                        // se la registrazione è a cavallo del limite d'uscita e in tolleranza
                                                        // allora l'ora figurativa viene spostata al limite d'uscita
                                                        if ((currentRegV.Data_Ora_Fis_U.Value.TimeOfDay > exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value)
                                                            && (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Subtract(exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value) <= exitLimitAfternoonTollerance.Duration()))
                                                        {
                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Hours,
                                                                exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Minutes,
                                                                0);
                                                        }

                                                        //se si è in presenza di una registrazione notturna
                                                        if (currentRegV.Data_Ora_Fis_U.Value.Date > currentRegV.Data_Ora_Fis_U.Value.Date)
                                                        {
                                                            //se il limite pomeridiano è uguale o maggiore alla mezzanotte ma inferiore al limite mattutino ed entro la tolleranza
                                                            if (exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value >= midNight
                                                                     && exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value < fistMorningLimit
                                                                     && (exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Ticks) <= exitLimitAfternoonTollerance.Ticks)
                                                            {
                                                                //come registrazione figurativa di entrata viene presa la data dell'uscita e i minuti dati dal limite pomeridiano impostatao dai parametri
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Hours,
                                                                exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Minutes,
                                                                0);

                                                            }

                                                            //se si ha una registrazione notturna ma il limite cade prima della mezzanotte
                                                            else if (currentRegV.Data_Ora_Fis_E.TimeOfDay < exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value
                                                                && (exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Ticks) <= exitLimitAfternoonTollerance.Ticks)
                                                            {
                                                                //la registrazione figurativa prende la data dalla registrazione di entrata
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Hours,
                                                                exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Minutes,
                                                                0);

                                                            }
                                                        }
                                                    }

                                                    // in ogni caso, al termine dell'operazione, se è presente una registrazione d'uscita
                                                    // e l'ora figurativa di questa è inferiore all'ora figurativa dell'entrata allora 
                                                    // si porta l'ora d'uscita all'ora d'entrata (a patto che si trovino nella stessa data - questione degli arrotondamenti a 00:00)
                                                    if (currentRegU != null)
                                                        if (currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay < currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay
                                                            && currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Date == currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Date)
                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fig_Reg;
                                                }
                                            }

                                            #endregion

                                        }
                                        #endregion


                                    }
                                }
                            }
                        }

                        #region 4.Gestione dei ritardi

                        if (utilizzoLimiteEntrata == (int)UtilizzoLimiteEntrata.Ritardo ||
                            utilizzoLimiteEntrata == (int)UtilizzoLimiteEntrata.LimiteEntrataERitardo)
                        {
                            bool isfirstAfternoonReg = true,
                                 isFirstMorningReg = true;
                            var regsByDate = colGroup.GroupBy(r => r.Data_Ora_Fig_EDate);

                            foreach (var dateGroup in regsByDate)
                            {
                                List<Reg_V> delayRegVs = dateGroup.OrderBy(regV => regV.Data_Ora_Fis_E).ToList();

                                isfirstAfternoonReg = true;
                                isFirstMorningReg = true;
                                Dictionary<EntryLimitTypeEnum, EntryLimitData> entryLimitConfig;
                                midDay = new TimeSpan(12, 0, 0);

                                foreach (Reg_V currentRegV in delayRegVs)
                                {
                                    Reg currentRegE = regIds[currentRegV.RegE];
                                    Cant currentCant = RepoManager.CantRepo.Single(c => c.Cant_Id == currentRegE.Cant_Id);

                                    // calcolo dei dati di limite d'entrata riguardo la registrazione che si sta processando
                                    entryLimitConfig = GetEntryLimitConifg(currentCant, currentCol, currentRegE.Registrazione_Data_Ora_Fis_Reg.Date, midDay);

                                    // se ci sono dei limiti d'entrata configurati ed esiste una registrazione d'entrata
                                    if (entryLimitConfig.Any(kvp => kvp.Value.IsConfigured) && currentRegE != null)
                                    {
                                        TimeSpan delayLimit;
                                        TimeSpan? morningDelayLimit,
                                            afternoonDelayLimit;
                                        int delayDuration = 0;

                                        //recupera i limiti d'entrata dai parametri...
                                        if (entryLimitConfig[EntryLimitTypeEnum.MorningDealyLimitList].EntryLimitTimeList == null && entryLimitConfig[EntryLimitTypeEnum.AfternoonDealyLimitList].EntryLimitTimeList == null)
                                        {
                                            morningDelayLimit = entryLimitConfig[EntryLimitTypeEnum.Morning].IsConfigured ? entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime : null;
                                            afternoonDelayLimit = entryLimitConfig[EntryLimitTypeEnum.Afternoon].IsConfigured ? entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime : null;
                                        }
                                        //...o dagli orari
                                        else
                                        {
                                            morningDelayLimit = entryLimitConfig[EntryLimitTypeEnum.MorningDealyLimitList].EntryLimitTimeList.FirstOrDefault();
                                            afternoonDelayLimit = entryLimitConfig[EntryLimitTypeEnum.AfternoonDealyLimitList].EntryLimitTimeList.FirstOrDefault();
                                        }

                                        if (afternoonDelayLimit != null && afternoonDelayLimit != TimeSpan.Zero)
                                        {
                                            midDay = afternoonDelayLimit.Value;
                                        }

                                        else
                                        {
                                            afternoonDelayLimit = null;
                                        }

                                        //Se è la prima registrazione della mattina, ne calcola il ritardo
                                        if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < midDay && morningDelayLimit != null && isFirstMorningReg)
                                        {
                                            isFirstMorningReg = false;

                                            delayLimit = morningDelayLimit.Value.Add(TimeSpan.FromMinutes(delayTollerance));

                                            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > delayLimit)
                                            {
                                                delayDuration = Convert.ToInt32(Math.Floor(currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.TotalMinutes - morningDelayLimit.Value.TotalMinutes));
                                            }
                                        }

                                        //Se è la prima registrazione del pomeriggio, ne calcola il ritardo
                                        else if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= midDay && afternoonDelayLimit != null && isfirstAfternoonReg)
                                        {
                                            isfirstAfternoonReg = false;

                                            delayLimit = afternoonDelayLimit.Value.Add(TimeSpan.FromMinutes(delayTollerance));

                                            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > delayLimit)
                                            {
                                                delayDuration = Convert.ToInt32(Math.Floor(currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.TotalMinutes - afternoonDelayLimit.Value.TotalMinutes));
                                            }
                                        }

                                        currentRegE.Ritardo_Durata = delayDuration;
                                    }
                                }
                            }
                        }

                        #endregion

                    }
                }
            }

            RepoManager.RegRepo.Context.BulkUpdate(regs);

        }

        public void RoundDuration(IEnumerable<Reg> regs)
        {
            // Lista che conteerrà gli errori di elaborazione
            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();
            // Lista che conterrà gli arrotondamenti da aggiungere a db
            List<Reg> roundingsToAdd = new List<Reg>();

            // Recupero il metodo di arrotondamento dalla scheda parametri
            RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;

            // Filtra le regv selezionando solo quelle 'lavorative' (ore e viaggi)
            var filteredRegVs = RepoManager.Reg_VRepo.Find(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip).ToList();

            // Controllo che mi siano state passate delle regv e che nei parametri sia attivato l'arrotondamento per durata
            if (filteredRegVs.Count() > 0 && roundingParamEnum == RoundingMethodEnum.Duration)
            {
                // Raggruppa le registrazioni per collaboratore
                var regsByCol = filteredRegVs.GroupBy(reg => reg.Col_Id).ToList();

                //Recupero i Parametri di Arrotondamento Generali da Scheda parametri
                int paramMinutesDuration = RepoManager.ParamRepo.ParametersRow.Default_Minuti_Durata.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Minuti_Durata.Value : -1;
                int paramThresholdDuration = RepoManager.ParamRepo.ParametersRow.Default_Soglia_Durata.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Soglia_Durata.Value : -1;
                int paramFromHourThresholdDuration = RepoManager.ParamRepo.ParametersRow.Soglia_Minima_Arrotondamento_Durata.HasValue ? RepoManager.ParamRepo.ParametersRow.Soglia_Minima_Arrotondamento_Durata.Value : 60;

                double totalCol = regsByCol.Count();
                double percCol = 0;
                double countCol = 1;

                foreach (var colGroup in regsByCol)
                {
                    percCol = (countCol / totalCol) * 100;
                    //emetto messaggio di Elaborazione del COL "N"

                    countCol++;

                    var currColId = colGroup.Key.HasValue ? colGroup.Key : -1;

                    if (currColId != -1)
                    {
                        Col currentCol = RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == currColId);

                        // Recupera i parametri dal collaboratore. Se il collaboratore non ha parametri impostati, li prende dalla scheda parametri
                        int thresholdDuration = currentCol.Arrot_Durata_Col.HasValue ? currentCol.Arrot_Durata_Col.Value : paramThresholdDuration;
                        int minutesDuration = currentCol.Soglia_Durata_Col.HasValue ? currentCol.Soglia_Durata_Col.Value : paramMinutesDuration;
                        int fromHourThresholdDuration = currentCol.Soglia_Minima_Arrotondamento_Durata_Col.HasValue ? currentCol.Soglia_Minima_Arrotondamento_Durata_Col.Value : paramFromHourThresholdDuration;

                        if (currentCol != default(Col))
                        {
                            // Raggruppa le registrazioni per data (giorno)
                            var regsByColDate = colGroup.GroupBy(reg => reg.Data_Reg).ToList();

                            foreach (var colDateGroup in regsByColDate)
                            {
                                // Accumulatore delle durate per il giorno corrente
                                int workDayDuration = 0;
                                foreach (Reg_V regv in colDateGroup)
                                {
                                    // Accumula la durata delle registrazioni della giornata
                                    workDayDuration += regv.Durata_Fis.GetValueOrDefault();
                                }

                                if (workDayDuration > 0)
                                {
                                    // Ore e minuti lavorati nella giornata corrente
                                    int hoursWorked = workDayDuration / 60;
                                    int minutesWorked = workDayDuration % 60;

                                    if (minutesDuration == 0)
                                    {
                                        // Se il parametro è a 0, lo porto a 60 per poter fare i calcoli
                                        minutesDuration = 60;
                                    }

                                    if (minutesWorked > fromHourThresholdDuration) //Inserire parametro
                                        continue;

                                    int moduleMinutes = minutesWorked % minutesDuration;

                                    //Se ci sono minuti in esubero rispetto al parametro, genero la regv di arrotondamento
                                    if (moduleMinutes != 0)
                                    {
                                        //Se sono sopra alla soglia, genero una regv di arrotondamento positiva
                                        if (moduleMinutes > thresholdDuration)
                                        {
                                            // La reg di arrotondamento avrà durata tale da portare la durata totale di giornata al parametro superiore specificato
                                            TimeSpan roundingTime = new TimeSpan(0, minutesDuration - moduleMinutes, 0);
                                            roundingsToAdd.Add(RepoManager.RegRepo.GenerateRoundingReg(currColId.GetValueOrDefault(), colDateGroup.Key.Value, RoundingTypeEnum.RoundingPlus, roundingTime));
                                        }
                                        //Se sono sotto alla soglia, genero una regv di arrotondamento negativa
                                        else
                                        {
                                            // La reg di arrotondamento avrà durata tale da portare la durata totale di giornata al parametro inferiore specificato
                                            TimeSpan roundingTime = new TimeSpan(0, moduleMinutes, 0);
                                            roundingsToAdd.Add(RepoManager.RegRepo.GenerateRoundingReg(currColId.GetValueOrDefault(), colDateGroup.Key.Value, RoundingTypeEnum.RoundingMinus, roundingTime));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // se al termine del ciclo sono state generate delle rettifiche allora si procede alla loro scrittura nel database
                if (roundingsToAdd.Any())
                    RepoManager.RegRepo.BulkInsert(roundingsToAdd);

            }
        }

        public void CheckOverlaps(IEnumerable<Reg> regs)
        {
            //// inizializazione della stub utilizzata per recuperare il nome delle proprietà
            //Reg_V regVStub = Init();

            //List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();
            ////Elimino le eventuali REGV NON ABBINATE (senza Ora di uscita)
            //// se sono online, cioè a video controllo la presenza della data e ora di uscita (nella fase di check sui valori non ancora salvati la reg_v non ha ancora un id)
            //// se invece sono batch (cioè provengo dall'elaborate) allora controllo la presenza della regu (il riaccoppiamento è già stato effettuato)
            //if (isOnLine)
            //    regVs = regVs.Where(reg => reg.Data_Ora_Fis_U != null).ToList();
            //else
            //    regVs = regVs.Where(reg => reg.RegU != null).ToList();

            //var groupByColRegs = regVs.GroupBy(regV => regV.Col_Id);

            //List<Reg_V> isOverlappingRegVs = new List<Reg_V>();
            //List<Reg_V> isNotOverlappingRegVs = new List<Reg_V>();

            //foreach (var colGroup in groupByColRegs)
            //{
            //    List<Reg_V> currentRegVs = colGroup.OrderBy(reg => reg.Data_Ora_Fis_E).ToList();

            //    DateTime lastFisU = DateTime.MinValue;

            //    foreach (Reg_V currentRegV in currentRegVs)
            //    {
            //        //if (currentRegV.Data_Ora_Fis_E.Hour >= lastFisU.Hour && currentRegV.Data_Ora_Fis_E.Minute >= lastFisU.Minute)
            //        var currentDataOraE = new DateTime(currentRegV.Data_Ora_Fis_E.Year, currentRegV.Data_Ora_Fis_E.Month, currentRegV.Data_Ora_Fis_E.Day,
            //            currentRegV.Data_Ora_Fis_E.Hour, currentRegV.Data_Ora_Fis_E.Minute, currentRegV.Data_Ora_Fis_E.Second);
            //        if (currentDataOraE >= lastFisU)
            //        {
            //            isNotOverlappingRegVs.Add(currentRegV);
            //            lastFisU = currentRegV.Data_Ora_Fis_U != null ? currentRegV.Data_Ora_Fis_U.Value : lastFisU;
            //        }
            //        else
            //            isOverlappingRegVs.Add(currentRegV);
            //    }
            //}

            //var regIds = isOverlappingRegVs.Select(regv => regv.RegE).ToList();
            //// vengono recuperati gli id della regu solo se non null (in quanto nella versione con isOnline = true può capitare, non essendo ancora scritte sul database)
            //regIds.AddRange(isOverlappingRegVs.Where(regv => regv.RegU != null).Select(regv => regv.RegU.Value));

            //// in questo ciclo si trattano solamente le reg_v non nuove
            //IEnumerable<Reg> isOverlappingRegs = RepoManager.RegRepo.Find(reg => regIds.Where(regId => regId != 0).ToList().Contains(reg.Reg_Id), true).ToList();
            //foreach (var isOverlappingReg in isOverlappingRegs)
            //{
            //    // viene impostato il nuovo stato della registrazione solamente se non si è online, visto che un'eventuale modifica della
            //    // reg_v online prima del salvataggio sul db genera un errore al primo savechanges del contesto
            //    if (!isOnLine)
            //        isOverlappingReg.Registrazione_Stato_RegEnum |= RegStateEnum.Overlap;

            //    // se sono online allora devo passare un nome di colonna valida altrimenti l'errore non viene preso in considerazione;
            //    // in conseguenza, in online, per la chiave viene impostato il campo Data_Reg
            //    if (isOnLine)
            //        errors.Add(new KeyValuePair<string, string>(CommonService.GetPropertyName(() => regVStub.Data_Reg),
            //        BusinessService.GetLocalizedString(PowerWebResources.ERR_OVERLAP_DELLA_REG) + isOverlappingReg.Reg_Id));
            //    else
            //        errors.Add(new KeyValuePair<string, string>(FunctionMessageEnum.CheckOverlaps.ToString(),
            //            BusinessService.GetLocalizedString(PowerWebResources.ERR_OVERLAP_DELLA_REG) + isOverlappingReg.Reg_Id));
            //}

            //// se si sta processando online e sono presenti delle reg nuove
            //if (isOnLine && regIds.Contains(0))
            //{
            //    // si aggiunge un errore specifico per la reg nuova
            //    errors.Add(new KeyValuePair<string, string>(CommonService.GetPropertyName(() => regVStub.Data_Reg),
            //        BusinessService.GetLocalizedString(PowerWebResources.ERR_OVERLAP_DELLA_REG) + "[NUOVA]"));
            //}
        }

        public void ElaborateActivities(IEnumerable<Reg> regs)
        {

            IEnumerable<Reg_V> regvs = GetRegVsToPostProcess(regs);

            // se non ci sono reg da processare allora si esce senza effettuare nessuna operazione
            if (!regvs.Any())
                return;

            DateTime? from = regvs.Min(r => r.Data_Ora_Fis_E);
            DateTime? to = regvs.Max(r => r.Data_Ora_Fis_U);

            //Nel caso in cui l'ultima registrazione sia un passaggio (ha solo RegE e non RegU), il massimo delle regE sarà maggiore del massimo delle regU, quindi prendo il massimo delle regE come data termine
            if (to.HasValue)
            {
                if (regvs.Max(r => r.Data_Ora_Fis_E) > to.Value)
                {
                    to = regvs.Max(r => r.Data_Ora_Fis_E);
                    to = to.Value.AddDays(1);
                    to = to.Value.Date;
                }
            }

            //Se non ho nessuna regU vuol dire che tutte le registrazioni sono passaggi, allora la data termine sarà la maggiore delle regE
            else
            {
                to = regvs.Max(r => r.Data_Ora_Fis_E);
                to = to.Value.AddDays(1);
                to = to.Value.Date;
            }

            //Hashset per ottimizzare il costo di "coldIs.Contains(reg.Col_Id)"
            HashSet<int?> coldIs = regvs.Select(reg => reg.Col_Id).Distinct().ToHashSet();

            //Removing associated Activities
            List<Reg> atts = RepoManager.RegRepo.Find(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Att && reg.Registrazione_Data_Ora_Fis_Reg >= from &&
                reg.Registrazione_Data_Ora_Fis_Reg <= to && reg.Col_Id.HasValue && coldIs.Contains(reg.Col_Id) &&
                !reg.Registrazione_Bloccata, true).ToList();

            List<Reg> toUpdateAtts = new List<Reg>();

            if (atts.Count > 0)
            {
                var attByColDic = new Dictionary<int, List<Reg>>();

                atts.ForEach(att =>
                {
                    att.Att_Id = null;
                    att.RiferimentoRRN_Att = null;
                    att.Registrazione_Stato_Reg = (int)RegStateEnum.None;

                    if (!attByColDic.ContainsKey(att.Col_Id.Value))
                        attByColDic.Add(att.Col_Id.Value, new List<Reg>());

                    attByColDic[att.Col_Id.Value].Add(att);
                });

                var regvsByCol = regvs.GroupBy(r => r.Col_Id).ToList();

                double colsTotal = regvsByCol.Count();
                double colCount = 1;
                double percRec = 0;

                foreach (var currentRegvByCol in regvsByCol)
                {
                    percRec = (colCount / colsTotal) * 100;
                    colCount++;

                    var regvsByColByDate = currentRegvByCol.GroupBy(r => r.Data_Ora_Fis_E).ToList();

                    foreach (var currentRegByColByDate in regvsByColByDate)
                    {
                        var orderedRegsByColByDate = currentRegByColByDate.OrderBy(r => r.Data_Ora_Fis_E).ToList();
                        foreach (var currentRegv in orderedRegsByColByDate)
                        {
                            // elaboro solo le attività per le regv che hanno entrata e uscita
                            if (currentRegv.Data_Ora_Fis_U != null)
                            {
                                currentRegv.Data_Ora_Fis_U = AdjustMinutes(currentRegv.Data_Ora_Fis_U.Value);

                                if (attByColDic.ContainsKey(currentRegv.Col_Id.Value))
                                {
                                    List<Reg> associatedAtt = attByColDic[currentRegv.Col_Id.Value].Where(r => r.Col_Id == currentRegv.Col_Id && r.Registrazione_Data_Ora_Fis_Reg >= currentRegv.Data_Ora_Fis_E && r.Registrazione_Data_Ora_Fis_Reg <= currentRegv.Data_Ora_Fis_U).ToList();

                                    associatedAtt.ForEach(reg =>
                                    {
                                        reg.Att_Id = currentRegv.Cant_Id;
                                        reg.RiferimentoRRN_Att = currentRegv.RegE;
                                        reg.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                                    });

                                }
                            }
                        }
                    }
                }

                RepoManager.RegRepo.Context.BulkUpdate(atts);
            }

        }

        /// <summary>
        /// Effettua la cancellazione delle regitrazioni attività tappo marcate per la cancellazione dalle precedenti funzion di gestione.
        /// </summary>
        public void DeleteAllActivitiesMarkedForDeletion()
        {

            // si procede ad effettuare la modifica solamente se il modulo delle attività è abilitato
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Att)
            {
                // sono recuperate tutte le registrazioni marcate per la cancellazione
                RepoManager.RegRepo.DbSet.Where(reg => reg.Custom_Data_Reg == Settings.Default.ActivityMarkedForDeletionCustomData).DeleteFromQuery();

            }

        }

        public void ElaborateTrips(IEnumerable<Reg> regs)
        {
            var errors = new List<KeyValuePair<String, String>>();

            var regvs = GetRegVsToPostProcess(regs);

            // si elaborano i viaggi solamente se sono tra i moduli abilitati
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Viaggi)
            {
                BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(0, "Inizio elaborazione Viaggi");


                // se sono state passate delle regv allora si tolgono dall'elaborazione tutte quelle che corrispondono a delle rettifiche, che sono delle
                // semplici durate o che sono degli arrotondamenti per durata
                if (regvs.Any())
                    regvs = regvs.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip).ToList();


                regvs = regvs.Where(regv => regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att).ToList();



                //Serve SOLO per Cancellare le eventuali REG Viaggi Esistente tra la Data (senza Ora) di Inizio e la Data (senza Ora) di Fine 
                //ricavate dalla Lista di REG Ricevute
                DateTime? from = regvs.Min(r => r.Data_Ora_Fis_E);
                //se l'ultima timbratura di giornata è un passaggio (non ha RegU), prendo l'entrata.
                DateTime? to = regvs.Max(r => r.Data_Ora_Fis_U) ?? regvs.Max(r => r.Data_Ora_Fis_E);

                var colIds = regvs.Select(regv => regv.Col_Id).Distinct().ToList();

                //se i campi from e to sono valorizzati
                if (from.HasValue && to.HasValue)
                {

                    //Identifica le Registrazioni Viaggi esistenti nella VL REGV per Cancellarle prima di ricrearle
                    var regvTrips = regvs.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip).ToList();

                    //se vi sono dei viaggi
                    if (regvTrips.Count > 0)
                    //nel caso in cui c'erano Registrazioni Viaggi da Cancellare Le Cancella
                    {
                        try
                        {
                            var toDelete = RepoManager.RegRepo.DbSet.Where(r => r.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip && (r.Registrazione_Data_Ora_Fis_Reg >= from && r.Registrazione_Data_Ora_Fis_Reg <= to)).ToList();
                            RepoManager.RegRepo.Delete(toDelete, true);
                        }
                        catch (Exception ex)
                        {
                            //_log.ErrorFormat("Errori durante la cancellazione dei viaggi : {0}", ex.Message);
                        }
                        //procede a cancellare tutte le precedenti Registrazioni di VIAGGI che rientrano nei limiti di data ricevuti

                        //la nuova lista di registrazioni da procesare non contiene i viaggi
                        regvs = regvs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip).ToList();
                    }

                    //verifico se ho ricevuto delle REG NON ABBINATE
                    var oddRegvs = regvs.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None && regv.Registrazione_Stato_Reg == (int)RegStateEnum.None).ToList();



                    if (oddRegvs.Count() > 0)
                        errors.Add(new KeyValuePair<String, String>(FunctionMessageEnum.ElaborateTrips.ToString(), BusinessService.GetLocalizedString(PowerWebResources.ERR_PRESENZA_TIMBRATURE_DISPARI)));
                    else
                    {


                        //Estrae SOLO le REG che hanno l'Ora di Fine (Associate) + le RegV dei Passaggi                    
                        regvs = regvs.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass || (regv.RegU != null && (regv.Registrazione_Stato_Reg & (int)RegStateEnum.Ass) == (int)RegStateEnum.Ass)).ToList();
                        //recupero da Param i Flag_Ore_Viaggi (se vale 0 NON devo trattare i Viaggi)
                        int paramTripHours = RepoManager.ParamRepo.ParametersRow.Flag_Ore_Viaggi.HasValue ? RepoManager.ParamRepo.ParametersRow.Flag_Ore_Viaggi.Value : 0; // 0 means skip checks
                                                                                                                                                                           //recupero da Param la Durata_Pausa
                        TimeSpan paramPauseTime = RepoManager.ParamRepo.ParametersRow.Durata_Pausa.HasValue ? RepoManager.ParamRepo.ParametersRow.Durata_Pausa.Value : TimeSpan.Zero;
                        //Crea la Lista vuota in cui scrivere le Nuove Registrazioni dei Viaggi
                        var trips = new List<Reg>();

                        //Tratto SOLO le REG SENZA MOTIVAZIONE (NO DEVE TRATTARE ANCHE I VIAGGI FRA REG CON/SENZA MOTIAVZIONE)
                        //var tripsWithoutMot = regvs.Where(regv => regv.Motivazione_Reg_Id == null);                                       

                        #region Raggruppamento delle REG e Generazione dei Viaggi fra le Registrazioni per Collaboratore

                        var tripsByCol = regvs.Where(reg => reg.Col_Id != null && reg.Col_Id != 0).GroupBy(reg => reg.Col_Id).ToList();



                        double colsTotal = tripsByCol.Count();
                        double colCount = 1;
                        double percRec = 0;


                        //Tratta le Regv per Collaboratore
                        foreach (var currentTripByCol in tripsByCol)
                        {
                            percRec = (colCount / colsTotal) * 100;
                            BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec, String.Format("Elaboro viaggi del collaboratore {0} di {1}", colCount, colsTotal));
                            BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec, String.Format("Elaboro viaggi del collaboratore {0} di {1}", colCount, colsTotal));
                            colCount++;

                            //identifica il primo Collaboratore da Trattare
                            var firstRegVByCol = currentTripByCol.First();

                            Col currentCol = RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == firstRegVByCol.Col_Id, false);

                            //recupera dal Collaboratore l'eventuale Durata Pausa (se NON è NULL) altrimenti lascia quella dei Parametri
                            TimeSpan currentColPauseTime = currentCol.Durata_Pausa_Col.HasValue ? currentCol.Durata_Pausa_Col.Value : paramPauseTime; // overriding paramPauseTime                    

                            //Raggruppa le REG x Col/Data (Data senza tenere conto del NOTTURNO)
                            var tripsByColByDate = currentTripByCol.GroupBy(reg => reg.Data_Ora_Fis_E.Date).ToList();

                            //Tratta le Regv per Collaboratore/Data_Ora Entrata
                            foreach (var currentTripsByColByDate in tripsByColByDate)
                            {
                                if (paramTripHours != (int)FlagTripHoursParamEnum.None)
                                //nel caso in cui il Flag della tab PARAM abiliti i Viaggi (<> 0)
                                {
                                    //Ordina le Regv per Collaboratore/Data-Ora Entrata                                                                                              
                                    var orderedCurrentTripsByColByDate = currentTripsByColByDate.OrderBy(regv => regv.Data_Ora_Fis_E);

                                    if (paramTripHours == (int)FlagTripHoursParamEnum.AllExceptTwo)
                                    //Nel caso in cui in Scheda PARAM sia abilitata la gestione per Tutti Eccetto i Col Esclusi
                                    {
                                        if (currentCol.Flag_Ore_Viaggi_Col != (int)FlagTripHoursColEnum.AllExceptTwo)
                                            //Tratta solo i Collaboratore che NON sono da Escludere
                                            trips.AddRange(ElaborateInternalTrips(orderedCurrentTripsByColByDate, currentCol, currentColPauseTime));
                                    }
                                    else if (paramTripHours == (int)FlagTripHoursParamEnum.OnlyOne)
                                    //Nel caso in cui in Scheda PARAM sia abilitata la gestione per SOLO i Collaboratori ABILITATI
                                    {
                                        if (currentCol.Flag_Ore_Viaggi_Col == (int)FlagTripHoursColEnum.OnlyOne)
                                            //Tratta solo i Collaboratore che sono ABILITATI
                                            trips.AddRange(ElaborateInternalTrips(orderedCurrentTripsByColByDate, currentCol, currentColPauseTime));
                                    }
                                }
                            }
                        }

                        #endregion

                        RepoManager.RegRepo.Context.BulkInsert(trips);

                        //se vi sono viaggi
                        if (trips.Count > 0)
                        {
                            //determino l'intervallo da...a in cui vengono calcolati i viaggi
                            from = trips.Min(reg => reg.Registrazione_Data_Ora_Fis_Reg);
                            to = trips.Max(reg => reg.Registrazione_Data_Ora_Fis_Reg);

                            //vengono estratti tutti gli utlimi viaggi (capisco che sono gli ultimi viaggi inseriti nel db perchè RiferimentoRRN_Att è diverso da NULL )                            
                            var addedTrips = RepoManager.RegRepo.Find(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip && reg.Registrazione_Data_Ora_Fis_Reg >= from && reg.Registrazione_Data_Ora_Fis_Reg <= to && reg.RiferimentoRRN_Att != null, true).ToList();

                            //raggruppo i viaggi per riferimento dell'attività(viene usato per poter lavorare con le stored procedure)
                            var tripsByRef = addedTrips.GroupBy(reg => reg.RiferimentoRRN_Att);

                            //ciclo su tutti i viaggi estratti
                            foreach (var trip in tripsByRef)
                            {
                                //il viaggio viene orinato per data ora(è una RegV cioè una coppia di Reg)
                                var tripByFis = trip.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();
                                int? lastTripId = null;

                                //per ogni valore della RegV , cioè per ogni reg si vanno a mettere a NULL i riferimenti verso le attività,
                                //mentre la RegE del viaggio a riferimento NULL, la RegU del viaggio ha riferimento alla RegE
                                tripByFis.ForEach(reg =>
                                {
                                    reg.RiferimentoRRN_Reg = lastTripId;
                                    reg.RiferimentoRRN_Att = null;
                                    lastTripId = reg.Reg_Id;
                                });
                            }

                            //viene fatto l'aggiornamento dei viaggi già inseriti nel database ma in cui sono stati modificati i riferimenti
                            RepoManager.RegRepo.Update(addedTrips, true);
                        }
                    }
                }
            }
        }

        private IEnumerable<Reg> ElaborateInternalTrips(IEnumerable<Reg_V> orderedCurrentTripsByMotByColByDate, Col currentCol, TimeSpan paramPauseTime)
        {
            //Ricava da PARAM l'eventuale Codice Cantiere a cui intestare i Viaggi (se caricato)
            int? dummyCantId = RepoManager.ParamRepo.ParametersRow.Cant_Id;
            //int? dummyFruId = RepoManager.ParamRepo.ParametersRow.Fru_Id;
            //Ricava da PARAM l'eventuale DURATA MIN E MAX dei Viaggi ( se caricata)
            TimeSpan paramMaxTripTime = RepoManager.ParamRepo.ParametersRow.Durata_Massima_Viaggio.HasValue ? RepoManager.ParamRepo.ParametersRow.Durata_Massima_Viaggio.Value : TimeSpan.Zero;
            TimeSpan paramMinTripTime = RepoManager.ParamRepo.ParametersRow.Durata_Minima_Viaggio.HasValue ? RepoManager.ParamRepo.ParametersRow.Durata_Minima_Viaggio.Value : TimeSpan.Zero;

            Reg_V lastRegV = null;
            List<Trip> tripList = new List<Trip>();

            // inizializzazione dell'indice di posizionamento del ciclo che aiuta a recuperare
            // la registrazione successiva a quella in elaborazione
            int position = 0;

            //viene estrato il valore dalla scheda parametri se è permesso il viaggio nello stesso cantiere
            int paramTripType = RepoManager.ParamRepo.ParametersRow.Tipo_Viaggio;

            //Recupera da PARAM il Flag_Ore_Viaggi di Inizio/Fine Giornata per decidere se deve o meno gestire anche i Viaggi da Casa al 1° Cantiere e dall'Ultimo Cantiere a Casa
            var paramTripHours = RepoManager.ParamRepo.ParametersRow.Flag_Ore_Viaggi_Inizio_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Flag_Ore_Viaggi_Inizio_Fine.Value : 0; // 0 means skip checks

            //Recupera il parametro della generazione di viaggi di inizio/fine giornata dal collaboratore o dai parametri. Se il parametro è = 3, genera i viaggi di inizio/fine giornata SOLO kilometrici dal/al cantiere SEDE. Se la prima/ultima regv è sul cantiere SEDE, il viaggio sarà comunque solo kilometrico.
            int startEndParam = currentCol.Flag_Viaggio_InizioFine_GIS.HasValue ? currentCol.Flag_Viaggio_InizioFine_GIS.Value : RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti_Inizio_Fine_G;

            //Tratta le Singole REGV del Collaboratore ricevuto in Ordine di Data/Ora
            foreach (var currentRegV in orderedCurrentTripsByMotByColByDate)
            {
                if (lastRegV != null)
                {
                    //Se sono attivi i viaggi di inizio/fine giornata e vengono generati dalla sede, evito di generare il primo e l'ultimo viaggio dalla/per la sede
                    if (paramTripHours != (int)FlagTripHoursParamEnum.None && startEndParam == (int)TripAssignmentTypeEnum.CalculateFromHeadquarter)
                    {

                        Cant prevCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == lastRegV.Cant_Id);
                        //Se questa è la seconda regv
                        if (position == 1)
                        {
                            //Se la regv precedente (la prima) è stata fatta nel cantiere SEDE e il cantiere corrente non è la sede
                            if (prevCant.Tipo_Cantiere_Can == "SEDE" && currentRegV.Cant_Id != prevCant.Cant_Id)
                            {
                                //Aggiorno le variabili e vado al prossimo passo del for
                                position++;
                                lastRegV = currentRegV;
                                continue;
                            }
                        }

                        //Se questa è l'ultima regv
                        if (position == orderedCurrentTripsByMotByColByDate.Count() - 1)
                        {
                            Cant currCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == currentRegV.Cant_Id);
                            //Se la regv precedente (la prima) è stata fatta nel cantiere SEDE
                            if (currCant.Tipo_Cantiere_Can == "SEDE" && prevCant.Cant_Id != currCant.Cant_Id)
                            {
                                //Aggiorno le variabili e vado al prossimo passo del for
                                position++;
                                lastRegV = currentRegV;
                                continue;
                            }
                        }
                    }

                    //ottengo true se il cantiere di arrivo è diverso da quello di partenza
                    bool differentCant = (lastRegV.Cant_Id != currentRegV.Cant_Id);

                    #region 1.CALCOLO VIAGGI TENENDO PRESENTE SE CALCOLARE VIAGGI SULLO STESSO CANTIERE

                    //se i cantieri sono differenti e non ho attivo il parametro ->CALCOLO VIAGGIO
                    //se i cantieri sono UGUALI e NON ho attivo il parametro ->NO CALCOLO VIAGGIO
                    //se i cantieri sono UGUALI ed E' attivo il parametro ->CALCOLO VAGGIO

                    if (differentCant || paramTripType == (int)FlagTripTypeEnum.SameCant)
                    //Crea un Viaggio nel caso in cui il Cantiere sia Cambiato oppure se sono previsti anche i Viaggi fra Cantieri Uguali 
                    {
                        //Inizializza i Valori di Default delle 2 nuove Registrazioni (Entrata + Uscita) che deve creare
                        Reg newRegE = RepoManager.RegRepo.Init();
                        Reg newRegU = RepoManager.RegRepo.Init();

                        // calcolo del cantiere della reg precedente e successiva al fine di gestire le ore non lavorate
                        var currentCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == currentRegV.Cant_Id);
                        var lastCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == lastRegV.Cant_Id);

                        // calcolo della partenza e/o arrivo da ONL
                        bool isFromOnl = lastCant.Tipo_Cantiere_Can == "ONL";
                        bool isToOnl = currentCant.Tipo_Cantiere_Can == "ONL";

                        //Intesta le nuove Registrazioni al Cantiere (di Default da Param o di Fine Viaggio).
                        // se il cantiere viene preso dalla param si recupera quel cantiere;
                        // in caso contrario si utilizza il cantiere standard solamente se non si ha una destinazione un cantiere con tipo
                        // ONL (ore non lavorate); in questo caso la destinazione il cantiere della registrazione successiva a quella in processo
                        if (dummyCantId != null) // recupero del cantiere dai parametri
                        {
                            newRegE.Cant_Id = newRegU.Cant_Id = dummyCantId.Value;
                        }
                        else // recupero del cantiere dalla destinazione
                        {
                            // si imposta il cantiere di destinazione con il cantiere di reg_v solamente se il cantiere di destnazione non è
                            // di tipo ONL (ore non lavorate)
                            if (!isToOnl)
                            {
                                //Il cantiere di destinazione della nuova reg è uguale al cantiere della registrazione corrente
                                newRegE.Cant_Id = newRegU.Cant_Id = currentRegV.Cant_Id;
                            }
                            else
                            {
                                // Se il cantiere è ONL e quindi il cantiere di destinazione del viaggio deve essere quello
                                // della regV successiva
                                var nextRegV = (position + 1) < orderedCurrentTripsByMotByColByDate.Count() ? orderedCurrentTripsByMotByColByDate.ElementAt(position + 1) : null;

                                // se esiste una regv successiva utilizzo quel cantiere altrimenti procedo con lo standard
                                if (nextRegV != null)
                                    newRegE.Cant_Id = newRegU.Cant_Id = nextRegV.Cant_Id;
                                else
                                    newRegE.Cant_Id = newRegU.Cant_Id = currentRegV.Cant_Id;
                            }
                        }

                        // viene calcolato il cantiere di partenza utilizzato per la generazione del viaggio
                        // se il cantiere di partenza è di tipo ore non lavorate allora viene impostato come cantiere di partenza
                        // il cantiere della regV precedente a quella di partenza; altrimenti viene impostato il cantiere dell'ultima regV
                        int? fromCantId = null;
                        //se il cantiere di partenza è ONL
                        if (isFromOnl)
                        {
                            // recupero della regV precedente
                            var previousRegV = (position - 2) >= 0 ? orderedCurrentTripsByMotByColByDate.ElementAt(position - 2) : null;

                            // se la regv precedente è presente allora si utilizza quel cantiere
                            if (previousRegV != null)
                                fromCantId = previousRegV.Cant_Id;
                            else // altrimenti si utilizza comunque quello dell'ultima regV
                                fromCantId = lastRegV.Cant_Id;
                        }
                        else
                            //se il cantiere di partenza non è ONL allora uso il cantiere della registrazione precedentre
                            fromCantId = lastRegV.Cant_Id;

                        //viene calcolata la durata del viaggio come differenza tra l'ora di entrata della destinazione successiva con l'ora di uscita della destinazione precedente
                        DateTime start = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? lastRegV.Data_Ora_Fig_U.Value : lastRegV.Data_Ora_Fis_E;
                        var duration = currentRegV.Data_Ora_Fig_E.Value.Subtract(start);

                        //viene calcolata l'ora fisica di inizio viaggio come l'ora di uscita della registrazione precedente (A)
                        var tripEFisDateTime = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? new DateTime(lastRegV.Data_Ora_Fis_U.Value.Year, lastRegV.Data_Ora_Fis_U.Value.Month, lastRegV.Data_Ora_Fis_U.Value.Day, lastRegV.Data_Ora_Fis_U.Value.Hour, lastRegV.Data_Ora_Fis_U.Value.Minute, 59) : new DateTime(lastRegV.Data_Ora_Fis_E.Year, lastRegV.Data_Ora_Fis_E.Month, lastRegV.Data_Ora_Fis_E.Day, lastRegV.Data_Ora_Fis_E.Hour, lastRegV.Data_Ora_Fis_E.Minute, 59);
                        //l'ora figurativa di inizio viaggio è calcolata dall 'ora figurativa della ragistrazione precedente
                        var tripEFigDateTime = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? lastRegV.Data_Ora_Fis_U : lastRegV.Data_Ora_Fis_E; //DA VERIFICARE
                                                                                                                                                             //var tripEFigDateTime =lastRegV.Data_Ora_Fig_U;

                        //se il viaggio ha un'ora valida di inizio
                        //viene calcolata l'ora di entrata figurativa prendendola dalle ore figurative
                        if (tripEFigDateTime.HasValue)
                            tripEFigDateTime = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? new DateTime(lastRegV.Data_Ora_Fis_U.Value.Year, lastRegV.Data_Ora_Fis_U.Value.Month, lastRegV.Data_Ora_Fis_U.Value.Day, lastRegV.Data_Ora_Fig_U.Value.Hour, lastRegV.Data_Ora_Fig_U.Value.Minute, 59) : new DateTime(lastRegV.Data_Ora_Fis_E.Year, lastRegV.Data_Ora_Fis_E.Month, lastRegV.Data_Ora_Fis_E.Day, lastRegV.Data_Ora_Fig_E.Value.Hour, lastRegV.Data_Ora_Fig_E.Value.Minute, 59);

                        //Carica i Dati della Registrazione di Entrata (Inizio Viaggio) (Prendo i valori della lastReg_v)
                        newRegE.Registrazione_Data_Ora_Orig_Reg = tripEFisDateTime;
                        newRegE.Registrazione_Data_Ora_Fis_Reg = tripEFisDateTime;
                        newRegE.Registrazione_Data_Ora_Fig_Reg = tripEFigDateTime;
                        newRegE.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                        newRegE.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                        newRegE.Col_Id = currentCol.Col_Id;
                        newRegE.Att_Id = newRegE.Cant_Id;

                        //Carica i Dati della Registrazione di Uscita (Fine Viaggio) (Prendo i valori della currentReg_v)
                        newRegU.ParentReg = newRegE;
                        newRegU.Registrazione_Data_Ora_Orig_Reg = currentRegV.Data_Ora_Fis_E;
                        newRegU.Registrazione_Data_Ora_Fis_Reg = currentRegV.Data_Ora_Fis_E;
                        newRegU.Registrazione_Data_Ora_Fig_Reg = duration <= TimeSpan.Zero ? tripEFigDateTime : currentRegV.Data_Ora_Fis_E;
                        newRegU.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                        newRegU.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                        newRegU.Col_Id = currentCol.Col_Id;
                        newRegU.Att_Id = newRegU.Cant_Id;

                        //per l'uso della stored procedure inserico i riferimenti 
                        newRegE.RiferimentoRRN_Att = lastRegV.RegE;
                        newRegU.RiferimentoRRN_Att = lastRegV.RegE;

                        // se i viaggi sono nello stesso minuto e l'entrata risulta maggiore dell'uscita
                        // allora si tratta di un viaggio con durata zero (utilizzato da alcuni clienti per i km da cantieri ONL, come la pausa);
                        // in quest caso reg e e reg_v vanno invertiti

                        //controllo se i viaggi sono nello stesso minuto
                        if (newRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Hours == newRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Hours
                            && newRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Minutes == newRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Minutes)
                        {

                            if (newRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Ticks > newRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Ticks)
                            {
                                Reg tmpReg = newRegE;
                                newRegE = newRegU;
                                newRegU = tmpReg;
                            }
                        }

                        // recupero la presenza o meno della customizzazione che mi impone di SALTARE  o FARE il controllo di durata per i viaggi che iniziano
                        // in un cantiere ONL
                        int customizationEnum = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoTripsMaxMinDurationControlONLCantEnum);


                        //se nella personalizzazione si è scelto di saltare il controllo sulla durata max e minima del viaggio 
                        if (customizationEnum == (int)NoTripsMaxMinDurationControlONLCantEnum.SkipControl || isFromOnl)
                        {
                            //Carica le 2 nuove registrazioni del Viaggio con la Durata Calcolata
                            tripList.Add(new Trip
                            {
                                RegE = newRegE,
                                RegU = newRegU,
                                cantIdStart = fromCantId,
                                IsFromOnl = isFromOnl,
                                IsToOnl = isToOnl
                            });
                        }

                        //Controllo della personalizzazione che controlla la durata per viaggi che iniziano su un cantiere ONL
                        else if (customizationEnum == (int)NoTripsMaxMinDurationControlONLCantEnum.DoControl)
                        {
                            //viene controllato se i valori della massima durata e minima sono valorizzati
                            if ((paramMaxTripTime != TimeSpan.Zero || paramMinTripTime != TimeSpan.Zero))
                            {
                                //viene calcolata la durata del viaggio tra cantiere ONL(A) e cantiere normale(B)
                                DateTime partenza = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? lastRegV.Data_Ora_Fig_U.Value : lastRegV.Data_Ora_Fis_E;
                                TimeSpan tripDuration = currentRegV.Data_Ora_Fis_E.Subtract(partenza);

                                //se la durata rientra nel range tra durata minima e massima allora vengono create le registrazioni di viaggio
                                if (tripDuration >= paramMinTripTime && (tripDuration <= paramMaxTripTime || paramMaxTripTime == TimeSpan.Zero))
                                {
                                    //Carica le 2 nuove registrazioni del Viaggio con la Durata Calcolata
                                    tripList.Add(new Trip
                                    {
                                        RegE = newRegE,
                                        RegU = newRegU,
                                        cantIdStart = fromCantId,
                                        IsFromOnl = isFromOnl,
                                        IsToOnl = isToOnl
                                    });
                                }
                            }
                        }
                        //se è attiva la personalizzazione che va a troncare il viaggio al valore di durata massima viaggi
                        else if (customizationEnum == (int)NoTripsMaxMinDurationControlONLCantEnum.TruncateToMax)
                        {
                            //viene calcolata la durata del viaggio
                            DateTime partenza = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? lastRegV.Data_Ora_Fig_U.Value : lastRegV.Data_Ora_Fis_E;
                            TimeSpan tripDuration = currentRegV.Data_Ora_Fis_E.Subtract(partenza);

                            //se la durata del viaggio è minore della durata massimo
                            if (tripDuration <= paramMaxTripTime)
                            {
                                //Carica le 2 nuove registrazioni del Viaggio con la Durata Calcolata
                                tripList.Add(new Trip
                                {
                                    RegE = newRegE,
                                    RegU = newRegU,
                                    cantIdStart = fromCantId,
                                    IsFromOnl = isFromOnl,
                                    IsToOnl = isToOnl
                                });
                            }
                            else
                            {
                                //se ladurata del viaggio supera la durata massima allora come durata del viaggio viene impostata la durata massima
                                newRegU.Registrazione_Data_Ora_Fis_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg.Add(paramMaxTripTime);

                                //Carica le 2 nuove registrazioni del Viaggio con la Durata Calcolata
                                tripList.Add(new Trip
                                {
                                    RegE = newRegE,
                                    RegU = newRegU,
                                    cantIdStart = fromCantId,
                                    IsFromOnl = isFromOnl,
                                    IsToOnl = isToOnl
                                });
                            }
                        }

                    }
                    #endregion
                }
                //la registrazione precedente è uguale alla successiva del caso prima (cioè passo alla registrazione successiva)
                lastRegV = currentRegV;

                // incremento dell'indice di elaborazione che serve a recuperare la registrazione successiva a quella esistente
                position++;
            }


            #region Gestione Pausa da VIAGGI

            //se vi sono viaggi all'interno della lista E il tipo di detrazione pausa è impostato DA VIAGGIO (1) E la pausa ha un valore
            if (tripList.Count > 0 && /*RepoManager.ParamRepo.ParametersRow.Detrazione_Pausa == 1 &&*/ paramPauseTime != TimeSpan.Zero)
            {
                //flag utilizzato per rilevare la presenza di viaggi sullo stesso cantiere (utilizzato in congiunzione alla customization PauseDeductionOnlyFascia5) (CG2 fa schifo)
                bool isPresentTripsSameCant = false;

                //vengono ordinati i viaggi per durata
                tripList = tripList.OrderByDescending(trip => trip.TripDuration).ToList();
                var pauseTrips = tripList;

                //se è attiva la customization che detrae la pausa solo dai viaggi fatti nella 5^ fascia, estrapola solo i viaggi che ricadono in 5^ fascia
                int customizationEnum = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PauseDeductionOnlyFascia5);
                if (customizationEnum == (int)PauseDeductionOnlyFascia5.Enabled)
                {

                    // Recupera la fascia 5 prima dai parametri o, se presente, dal collaboratore
                    TimeSpan paramTripRange5Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Inizio.Value : TimeSpan.Zero;
                    TimeSpan paramTripRange5End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Fine.Value : TimeSpan.Zero;
                    TimeSpan colTripRange5Start = currentCol.Fascia_Ore_Viaggi_5_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_5_Inizio_Col.Value : paramTripRange5Start;
                    TimeSpan colTripRange5End = currentCol.Fascia_Ore_Viaggi_5_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_5_Fine_Col.Value : paramTripRange5End;

                    //Controllo che almeno una fascia sia valorizzata e che la fine sia maggiore dell'inizio
                    if ((colTripRange5Start != TimeSpan.Zero || colTripRange5End != TimeSpan.Zero) && colTripRange5End > colTripRange5Start)
                    {
                        //Tiene solo i viaggi con partenza ricadente all'interno della 5^ fascia
                        pauseTrips = tripList.Where(trip => (IsStartTripInRange(colTripRange5Start, colTripRange5End, trip))).ToList();

                        //Se, tra i viaggi filtrati per fascia 5, ci sono viaggi sullo stesso cantiere, non serve detrarre la pausa da altri viaggi
                        //lo si segnala tramite l'apposito flag
                        if (pauseTrips.Any(trip => trip.cantIdStart == trip.RegU.Cant_Id))
                        {
                            isPresentTripsSameCant = true;
                        }
                    }
                }
                //istanzio un nuo hashSet(Rappresenta un insieme di valori di tipo viaggio SENZA DUPLICATI)
                HashSet<Trip> toRemove = new HashSet<Trip>();

                //Se è attivo il flag di viaggi sullo stesso cantiere (ideato per CG2 merda), elimino il viaggio sullo stesso cantiere più lungo
                if (isPresentTripsSameCant)
                {
                    //Usa una First e non una FirstOrDefault perché il flag è a true solo se sono presenti viaggi sullo stesso cantiere
                    toRemove.Add(pauseTrips.First(trip => trip.cantIdStart == trip.RegU.Cant_Id));
                }

                //Altrimenti detrae la pausa dai viaggi più lunghi
                else
                {
                    //si vanno a scorrere tutti i viaggi della lista
                    for (int i = 0; i < pauseTrips.Count && paramPauseTime != TimeSpan.Zero; i++)
                    {
                        //viene estratto il viaggio corrispondente all'indice del ciclo
                        Trip currTrip = pauseTrips[i];

                        //viene estratta la durata del viaggio
                        TimeSpan tripDurationMins = currTrip.TripDuration;

                        //se la durata del viaggio è maggiore della pausa
                        if (tripDurationMins > paramPauseTime)
                        {
                            //determino la differenza tra durata del viaggio e pausa
                            TimeSpan diff = tripDurationMins.Subtract(paramPauseTime);

                            //come ora finale del viaggio viene impostata l'ora di enttrata + la differenza tra durata totale del viaggio e pausa
                            currTrip.RegU.Registrazione_Data_Ora_Fig_Reg = currTrip.RegE.Registrazione_Data_Ora_Fig_Reg.Value.Add(diff);
                            currTrip.RegU.Registrazione_Data_Ora_Fis_Reg = currTrip.RegE.Registrazione_Data_Ora_Fis_Reg.Add(diff);

                            //viene impostata a zero la pausa
                            paramPauseTime = TimeSpan.Zero;
                        }
                        //se la durata del viaggio è uguale alla pausa
                        else if (tripDurationMins == paramPauseTime)
                        {
                            //viene azzerta la pausa
                            paramPauseTime = TimeSpan.Zero;

                            //aggiungo il viaggio da rimuovere
                            toRemove.Add(currTrip);
                        }
                        //se la durata del viaggio è minore della pausa
                        else if (tripDurationMins < paramPauseTime)
                        {
                            //viene calcolata la differenza tra la durata della pausa e la durata del viaggio
                            TimeSpan diff = paramPauseTime.Subtract(tripDurationMins);

                            //la nuova durata della pausa è uguale alla differenza
                            paramPauseTime = diff;

                            //il viaggio con durata minore della pausa va rimosso
                            toRemove.Add(currTrip);
                        }

                        //Se si vuole detrarre la pausa SOLO dal viaggio più lungo, sia essa esaurita o meno, si esce dal ciclo
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PauseDetractionOnlyLongestTrip) == (int)PauseDetractionOnlyLongestTrip.OnlyLongest)
                        {
                            break;
                        }
                    }
                }

                //si rimuovono i viaggi che non rispettano le condizioni(viaggi presenti nella lista toRemove)
                tripList.RemoveAll(x => toRemove.Contains(x));
            }

            #endregion

            #region Gestione Fasce Viaggi

            //Recupera da PARAM se vanno o meno trattate le FASCE VIAGGI
            int paramUseTripRange = 1;

            //viene controllato nella scheda parametri se si usano le fasce di viaggio
            if (RepoManager.ParamRepo.ParametersRow.Utilizzo_Fasce_Viaggi.HasValue)
            {
                paramUseTripRange = RepoManager.ParamRepo.ParametersRow.Utilizzo_Fasce_Viaggi.Value;
            }

            //se si hanno viaggi e vengono usate le fasce di viaggio
            if (tripList.Count > 0 && paramUseTripRange != 1)
            {
                #region FASCE VIAGGI DA SCHEDA PARAMETRI
                TimeSpan paramTripRange1Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_1_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_1_Inizio.Value : TimeSpan.Zero;
                TimeSpan paramTripRange1End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_1_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_1_Fine.Value : TimeSpan.Zero;

                TimeSpan paramTripRange2Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_2_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_2_Inizio.Value : TimeSpan.Zero;
                TimeSpan paramTripRange2End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_2_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_2_Fine.Value : TimeSpan.Zero;

                TimeSpan paramTripRange3Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_3_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_3_Inizio.Value : TimeSpan.Zero;
                TimeSpan paramTripRange3End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_3_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_3_Fine.Value : TimeSpan.Zero;

                TimeSpan paramTripRange4Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_4_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_4_Inizio.Value : TimeSpan.Zero;
                TimeSpan paramTripRange4End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_4_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_4_Fine.Value : TimeSpan.Zero;

                TimeSpan paramTripRange5Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Inizio.Value : TimeSpan.Zero;
                TimeSpan paramTripRange5End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Fine.Value : TimeSpan.Zero;
                #endregion

                #region FASCE VIAGGI DA SCHEDA COLLABORATORE
                TimeSpan colTripRange1Start = currentCol.Fascia_Ore_Viaggi_1_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_1_Inizio_Col.Value : paramTripRange1Start;
                TimeSpan colTripRange1End = currentCol.Fascia_Ore_Viaggi_1_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_1_Fine_Col.Value : paramTripRange1End;
                TimeSpan colTripRange2Start = currentCol.Fascia_Ore_Viaggi_2_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_2_Inizio_Col.Value : paramTripRange2Start;
                TimeSpan colTripRange2End = currentCol.Fascia_Ore_Viaggi_2_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_2_Fine_Col.Value : paramTripRange2End;
                TimeSpan colTripRange3Start = currentCol.Fascia_Ore_Viaggi_3_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_3_Inizio_Col.Value : paramTripRange3Start;
                TimeSpan colTripRange3End = currentCol.Fascia_Ore_Viaggi_3_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_3_Fine_Col.Value : paramTripRange3End;
                TimeSpan colTripRange4Start = currentCol.Fascia_Ore_Viaggi_4_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_4_Inizio_Col.Value : paramTripRange4Start;
                TimeSpan colTripRange4End = currentCol.Fascia_Ore_Viaggi_4_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_4_Fine_Col.Value : paramTripRange5End;
                TimeSpan colTripRange5Start = currentCol.Fascia_Ore_Viaggi_5_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_5_Inizio_Col.Value : paramTripRange5Start;
                TimeSpan colTripRange5End = currentCol.Fascia_Ore_Viaggi_5_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_5_Fine_Col.Value : paramTripRange5End;
                #endregion

                //viene istanziata una nuova lista di viaggi accettati, cioè che cadono nelle fasce viaggi consentite
                List<Trip> acceptedTrips = new List<Trip>();

                //per ogni viaggio della lista viene controllato se cade nella fascia viaggi consentita e viene aggiunto alla lista di viaggi accettati
                foreach (Trip trip in tripList)
                {
                    if (IsTripInRange(colTripRange1Start, colTripRange1End, trip))
                    {
                        acceptedTrips.Add(trip);
                    }
                    else if (IsTripInRange(colTripRange2Start, colTripRange2End, trip))
                    {
                        acceptedTrips.Add(trip);
                    }
                    else if (IsTripInRange(colTripRange3Start, colTripRange3End, trip))
                    {
                        acceptedTrips.Add(trip);
                    }
                    else if (IsTripInRange(colTripRange4Start, colTripRange4End, trip))
                    {
                        acceptedTrips.Add(trip);
                    }
                    else if (IsTripInRange(colTripRange5Start, colTripRange5End, trip))
                    {
                        acceptedTrips.Add(trip);
                    }
                }

                //la nuova lista di viaggi si trasforma nella lista di viaggi consetiti
                tripList = acceptedTrips;
            }
            #endregion

            #region Gestione Tabella Distanze KM/Ore

            //Recupera da PARAM se sul Viaggio vanno Gestite ANCHE le Informazioni relative AI KM/ORE presenti nella TAB_DISTANZE e come vanno gestite
            TripAssignmentTypeEnum paramTripAssignement = (TripAssignmentTypeEnum)RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti; // means skip check

            //se la lista di viaggi è valorizzata e si ha la necessita di assegnare i KM e le ore al viaggio
            if (tripList.Count > 0 && paramTripAssignement != TripAssignmentTypeEnum.None)
            //Caso di Viaggio da Intestare al Cantiere Viaggi di PARAM
            {
                // inizializzazione della lista da utilizzare nell'eventuale cancellazione dei viaggi con calcolo gis
                // senza una corrispondente riga nella tabella distanze
                List<Trip> tripsToDelete = new List<Trip>();


                foreach (Trip trip in tripList)
                {
                    //viene etratto il cantiere di partenza viaggio
                    var cantE = RepoManager.CantRepo.Single(c => c.Cant_Id == trip.cantIdStart);
                    //il cantiere di partenza corrisponde a quello di fine 
                    var cantU = cantE;
                    if (trip.cantIdStart != trip.RegU.Cant_Id)
                        cantU = RepoManager.CantRepo.Single(c => c.Cant_Id == trip.RegU.Cant_Id);

                    Tab_Dist distRow = null;

                    //Se in PARAM c'è il Tipo Assegnazione KM/Minuti = FIND (1) 
                    if (paramTripAssignement == TripAssignmentTypeEnum.Find)

                    //i Dati vengono cercati per Codice Cantiere 
                    //se non trovato per Codice Cantiere allora la ricerca viene effettuata anche per CAP)
                    //se NON trovato per CAP allora la ricerca viene effettuata anche per Luogo)
                    {
                        //vengono estratti i codici cantiere
                        var cantEValue = cantE.Cant_Id.ToString();
                        var cantUValue = cantU.Cant_Id.ToString();

                        #region 1.RICERCA PER CODICE CANTIERE 

                        //ricerco per codice cantiere nella tabella diatanze partendo dal cantiere di inizio a quello di fine
                        distRow = RepoManager.Tab_DistRepo.FindInTab_Dist("C", cantEValue, cantUValue);
                        //se non viene trovato il valore allore si procede alla ricera partendo dal cantire di fine a quello di inizio
                        if (distRow == null)
                            distRow = RepoManager.Tab_DistRepo.FindInTab_Dist("C", cantUValue, cantEValue);
                        #endregion

                        #region 2.RICERCA PER CAP
                        //se la ricera per codice cantiere non è andata a buon fine si ricerca per CAP
                        if (distRow == null)
                        {
                            //viene estratto il CAP dai cantieri
                            cantEValue = cantE.Cap_Can;
                            cantUValue = cantU.Cap_Can;

                            //ricerca mediante il CAP tra cantiere iniziale e finale
                            distRow = RepoManager.Tab_DistRepo.FindInTab_Dist("K", cantEValue, cantUValue);
                            if (distRow == null)
                                //ricerca per cantiere finale e iniziale
                                distRow = RepoManager.Tab_DistRepo.FindInTab_Dist("K", cantUValue, cantEValue);
                            #endregion

                            #region 3.RICERCA PER LUOGO
                            //se la ricerca per CAP non ha dato risultati viene ricercato il tutto per luogo
                            if (distRow == null)
                            {
                                //viene estratto il luogo dei cantieri
                                cantEValue = cantE.Luogo_Can;
                                cantUValue = cantU.Luogo_Can;

                                //viene ricercato per luogo cantiere iniziale e luogo cantiere finale
                                distRow = RepoManager.Tab_DistRepo.FindInTab_Dist("P", cantEValue, cantUValue);
                                if (distRow == null)
                                    //viene ricarcato per luogo cantiere fianle e cantiere iniziale
                                    distRow = RepoManager.Tab_DistRepo.FindInTab_Dist("P", cantUValue, cantEValue);
                            }

                        }
                        #endregion

                    }

                    //se nella scheda param ho Tipo_Assegnazione_KMMinuti=2
                    if (paramTripAssignement == TripAssignmentTypeEnum.Calculate)
                    {
                        //Viene controllato nella tab decod se è presente la gestione mediante GIS
                        var tdCant = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Nome_Tab == "TIPO_DISTANZA" && td.Chiave_Tab == "G");

                        //se ho la gestione mediante GIS
                        if (tdCant != null)
                        {
                            //viene creato l'indirizzo del cantiere di partenza e di fine compatibile con le richieste di GIS
                            string cantEAddress = string.Format("{0}|{1}|{2}", cantE.Luogo_Can, cantE.Indirizzo_Can, cantE.Cap_Can);
                            string cantUAddress = string.Format("{0}|{1}|{2}", cantU.Luogo_Can, cantU.Indirizzo_Can, cantU.Cap_Can);

                            #region RICERCA DEL VIAGGIO NELLA TAB DISTANZE

                            //cerco il viaggio da cantiereE a cantiereU nella tabella distanze
                            distRow = RepoManager.Tab_DistRepo.FirstOrDefault(d => d.Tab_Decod_Id == tdCant.Tab_Decod_Id && d.Partenza_Tab_Dist.ToUpper() == cantEAddress.ToUpper() && d.Arrivo_Tab_Dist.ToUpper() == cantUAddress.ToUpper());

                            //se non ho trovato il viaggio da cantiereE a cantiereU, provo da cantiereU a cantiereE
                            if (distRow == default(Tab_Dist))
                                distRow = RepoManager.Tab_DistRepo.FirstOrDefault(d => d.Tab_Decod_Id == tdCant.Tab_Decod_Id && d.Partenza_Tab_Dist.ToUpper() == cantUAddress.ToUpper() && d.Arrivo_Tab_Dist.ToUpper() == cantEAddress.ToUpper());

                            #endregion

                            #region GENERAZIONE VIAGGIO DA GIS

                            //altrimenti genero il viaggio da GIS (solo se il flag gis è attivo)
                            if (distRow == default(Tab_Dist) && RepoManager.ParamRepo.ParametersRow.Flag_GPS != 0)
                            {
                                //inizializzo le variabili di inizio e fine viaggio

                                Coordinate newStartRequest = new Coordinate();
                                Coordinate newEndRequest = new Coordinate();

                                if (CommonService.Nz(cantE.LatitudineGps_Can, 0) == 0 || CommonService.Nz(cantE.LongitudineGps_Can, 0) == 0)
                                //Se Il Cantiere di INIZIO VIAGGIO (ENTRATA) NON ha la LATITUDINE o la LONGITUDINE la cerca in base ai dati di ubicazione con BING
                                // e approfitta per aggiornarle anche in Anagrafica CANT
                                {
                                    RepoManager.CantRepo.UpdateGeoLocation(cantE);
                                }



                                newStartRequest.Latitude = cantE.LatitudineGps_Can;
                                newStartRequest.Longitude = cantE.LongitudineGps_Can;


                                if (CommonService.Nz(cantU.LatitudineGps_Can, 0) == 0 || CommonService.Nz(cantU.LongitudineGps_Can, 0) == 0)
                                //Se Il Cantiere di FINE VIAGGIO (USCITA) NON ha la LATITUDINE o la LONGITUDINE la cerca in base ai dati di ubicazione con BING
                                // e approfitta per aggiornarle anche in Anagrafica CANT
                                {
                                    RepoManager.CantRepo.UpdateGeoLocation(cantU);
                                }

                                newEndRequest.Latitude = cantU.LatitudineGps_Can;
                                newEndRequest.Longitude = cantU.LongitudineGps_Can;

                                //Se sono disponibili LAT/LONG sia del Cantiere di Inizio Viaggio (Entrata) sia del Cantiere di Fine Viaggio (Uscita)
                                //Allora calcola con BING il Percorso fra il Cantiere di Inizio Viaggio e quello di Fine Viaggio ottenenedone i KM e la Durata da BING
                                if (CommonService.Nz(cantE.LatitudineGps_Can, 0) != 0 &&
                                    CommonService.Nz(cantE.LongitudineGps_Can, 0) != 0 &&
                                    CommonService.Nz(cantU.LatitudineGps_Can, 0) != 0 &&
                                    CommonService.Nz(cantU.LongitudineGps_Can, 0) != 0)
                                {
                                    // se il cantiere ha impostato la latitudine e la longitudine ma non ha un indirizzo, un cap e un luogo
                                    // allora non si genera il record in tab distanze
                                    if (CommonService.Nz(cantE.Indirizzo_Can, String.Empty) != String.Empty &&
                                        CommonService.Nz(cantE.Cap_Can, String.Empty) != String.Empty &&
                                        CommonService.Nz(cantE.Luogo_Can, String.Empty) != String.Empty &&
                                        CommonService.Nz(cantU.Indirizzo_Can, String.Empty) != String.Empty &&
                                        CommonService.Nz(cantU.Cap_Can, String.Empty) != String.Empty &&
                                        CommonService.Nz(cantU.Luogo_Can, String.Empty) != String.Empty)
                                    {
                                        //Calcolo rotta tra i due punti
                                        Route routeResult = BusinessService.GetRoute(new Coordinate[] { newStartRequest, newEndRequest });

                                        if (routeResult != null)
                                        //Se è riuscito a Calcolare con BING i KM e la Durata del Viaggio allora crea il REcord della TAB_DISTANZA con Tipo = "G"
                                        {

                                            Tab_Decod tabDecod = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Nome_Tab.ToUpper() == "TIPO_DISTANZA" && td.Chiave_Tab.ToUpper() == "G");

                                            if (tabDecod != null)
                                            {

                                                distRow = new Tab_Dist()
                                                {
                                                    Partenza_Tab_Dist = string.Format("{0}|{1}|{2}", cantE.Luogo_Can, cantE.Indirizzo_Can, cantE.Cap_Can),
                                                    Arrivo_Tab_Dist = string.Format("{0}|{1}|{2}", cantU.Luogo_Can, cantU.Indirizzo_Can, cantU.Cap_Can),
                                                    Tab_Decod_Id = tabDecod.Tab_Decod_Id,
                                                    KM_Tab_Dist = (decimal)routeResult.TravelDistance,
                                                    Minuti_Tab_Dist = (int)routeResult.TravelDuration / 60,
                                                };

                                                var errorTab_DistRepo = RepoManager.Tab_DistRepo.Check(distRow, true);
                                                if (!errorTab_DistRepo.Any())
                                                {
                                                    try
                                                    {
                                                        RepoManager.Tab_DistRepo.Add(distRow, true);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        //_log.ErrorFormat("Errore durante l'inserimento nella tab. distanze di un nuovo record a causa dell exception {0}", ex.InnerException);
                                                    }

                                                }

                                                else
                                                {
                                                    //Log Error
                                                }
                                            }
                                        }
                                        else
                                        //Se NON è risucito a Calcolare il Percorso con Bing allora scrive un messaggio di errore nella TAB_MESSAGGI con Riferimento RouteCalculate
                                        {
                                            //Log error
                                        }
                                    }
                                    else
                                    {
                                        //Log error
                                    }
                                }
                                else
                                //Se NON è risucito a Calcolare il Percorso con Bing allora scrive un messaggio di errore nella TAB_MESSAGGI con Riferimento RouteCalculate
                                {
                                    //Log error
                                }
                            }
                            #endregion
                        }
                    }

                    //ho un viaggio con distanza non nulla
                    if (distRow != null)
                    {
                        // se è attiva la personalizzazione che prevede la non generazione dei viaggi nello stesso comune se inferiori e il kilometraggio espresso
                        // nella tabella distanze è inferiore a tale cifra, allora si provvede a marcare il viaggio che si sta generando per la cancellazione

                        // se il viaggio che si sta generando parte e arriva nello stesso comune, è attiva la personalizzazione del controllo di km in viaggi per stesso comune e i km
                        // assegnati alla tab distanze sono inferiori alla soglia, allora si procede a marcare il viaggio per la cancellazione; in caso contrario si procede alla sua generazione
                        if (IsTripSameMunicipalityToDelete(distRow))
                        {
                            trip.RegE.Codice_Accoppiamento = "1";
                            tripsToDelete.Add(trip);
                        }
                        else
                        {
                            // si calcola la durata dei viaggi utilizzando la tab distanze solamente se
                            // espresso dal livelli di personalizzazione
                            int customizationEnum = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CalculateTripDataEnum);
                            if (customizationEnum != (int)CalculateTripDataEnum.OnlyKm)
                            {
                                if (trip.TripDuration.TotalMinutes > distRow.Minuti_Tab_Dist)
                                {
                                    trip.RegU.Registrazione_Data_Ora_Fig_Reg = trip.RegE.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(distRow.Minuti_Tab_Dist);
                                    trip.RegU.Registrazione_Data_Ora_Fis_Reg = trip.RegE.Registrazione_Data_Ora_Fis_Reg.AddMinutes(distRow.Minuti_Tab_Dist);

                                    trip.RegE.Note_Reg = "Durata viaggio presa da tabella distanze";

                                }
                            }

                            // si procede alla verifica e all'inserimento dei km solamente se
                            // la destinazione non è un cantiere ore non lavorate
                            if (distRow.KM_Tab_Dist > default(decimal) && !trip.IsToOnl)
                                trip.RegE.KM_Reg = distRow.KM_Tab_Dist;
                            else
                                trip.RegE.KM_Reg = 0;
                        }
                    }
                    else
                    {
                        // se sto utilizzando il gis nel calcolo dei viaggi
                        if (paramTripAssignement == TripAssignmentTypeEnum.Calculate)
                        {
                            // se è attivata la personalizzaziontre per la non creazione dei viaggi con calcolo gis senza riga in tabella distanze
                            // allora marco per la cancellazione il viaggio in elaborazione
                            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoCreateTripsWithoutTabDistRowEnum);
                            if (customizationVersion == (int)NoCreateTripsWithoutTabDistRowEnum.DoNotCreate)
                            {
                                trip.RegE.Codice_Accoppiamento = "1";
                                tripsToDelete.Add(trip);
                            }
                        }
                    }
                }

                // al termine dell'elaborazione, se sono presenti dei viaggi da cancellare allora
                // procedo all'eliminazione
                if (tripsToDelete.Any())
                    tripList = tripList.Where(trip => trip.RegE.Codice_Accoppiamento != "1").ToList();

            }
            #endregion

            #region Gestione Viaggi Inizio/Fine Giornata

            if (paramTripHours != (int)FlagTripHoursParamEnum.None)
            //nel caso in cui il Flag della tab PARAM abiliti i Viaggi di Inizio/Fine Giornata da/a Casa (<> 0)
            {
                // Altrimenti controlla se sono attivi i viaggi di inizio/fine "standard"
                if (paramTripHours == (int)FlagTripHoursParamEnum.AllExceptTwo)   //(1)
                                                                                  //Nel caso in cui in Scheda PARAM sia abilitata la gestione dei Viaggi da/a Casa per Tutti Eccetto i Col Esclusi
                {
                    if (currentCol.Flag_Ore_Viaggi_Col_Inizio_Fine != (int)FlagTripHoursColStartEndEnum.AllExceptTwo)  // <> 2
                                                                                                                       //Tratta solo i Collaboratore che NON sono da Escludere
                        tripList.AddRange(ElaborateStartEndTrips(orderedCurrentTripsByMotByColByDate, currentCol));
                }
                else if (paramTripHours == (int)FlagTripHoursParamEnum.OnlyOne)
                //Nel caso in cui in Scheda PARAM sia abilitata la gestione dei Viaggi da/a Casa per i Soli Col Abilitati
                {
                    if (currentCol.Flag_Ore_Viaggi_Col_Inizio_Fine == (int)FlagTripHoursColStartEndEnum.OnlyOne)
                        //Tratta solo i Collaboratore Abilitati
                        tripList.AddRange(ElaborateStartEndTrips(orderedCurrentTripsByMotByColByDate, currentCol));
                }
            }

            #endregion

            List<Reg> trips = new List<Reg>();

            // mi creo una lista di viaggi da elaborare poi
            foreach (var trip in tripList)
                trips.AddRange(trip.Regs);

            return trips;
        }

        private List<Trip> ElaborateStartEndTrips(IEnumerable<Reg_V> orderedCurrentTripsByColByDate, Col currentCol)
        //Elabora i Viaggi di Inizio/Fine Giornata (in base ai FLAG_ORE_VIAGGI_INIZIO_FINE di PARAM e al FLAG_ORE_VIAGGI_COL_INIZIO_FINE)
        {
            List<Trip> tripList = new List<Trip>();

            if (orderedCurrentTripsByColByDate.Any())
            {
                Reg_V regE = orderedCurrentTripsByColByDate.ElementAt(0);

                Reg_V regU = orderedCurrentTripsByColByDate.ElementAt(orderedCurrentTripsByColByDate.Count() - 1);

                var threshold = DateTime.Now.Date.TimeOfDay;
                if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.None && RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.Disabled)
                {
                    if (RepoManager.ParamRepo.ParametersRow.Default_Durata_Max_Gruppo_Notte_Ril.HasValue)
                        threshold = RepoManager.ParamRepo.ParametersRow.Default_Durata_Max_Gruppo_Notte_Ril.Value;

                    if (currentCol.TipoNotturno_Col != (int)NocturneTypeEnum.None && currentCol.Durata_Max_Gruppo_Notte_Ril_Col.HasValue)
                        threshold = currentCol.Durata_Max_Gruppo_Notte_Ril_Col.Value;
                }

                var firstCant = RepoManager.CantRepo.Single(c => c.Cant_Id == regE.Cant_Id, true);
                var lastCant = RepoManager.CantRepo.Single(c => c.Cant_Id == regU.Cant_Id, true);

                //Se il collaboratore ha il flag valorizzato, prendo il suo altrimenti lo prendo dai parametri generali
                int tipo_Assegnazione_KMMinuti_Inizio_Fine_G = currentCol.Flag_Viaggio_InizioFine_GIS.HasValue ? currentCol.Flag_Viaggio_InizioFine_GIS.Value : RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti_Inizio_Fine_G;

                Cant sede = new Cant();
                //Se il parametro dei viaggio inizio/fine giornata è = 3 (viaggia da/per la sede), calcola i viaggi dalla sede
                if (tipo_Assegnazione_KMMinuti_Inizio_Fine_G == (int)TripAssignmentTypeEnum.CalculateFromHeadquarter)
                {
                    /* --- GESTIONE VIAGGIO INIZIO GIORNATA --- */

                    //Se la prima timbratura di giornata è stata fatta nella sede, prendo come primo cantiere quello della seconda regv di giornata
                    if (firstCant.Tipo_Cantiere_Can == "SEDE")
                    {
                        sede = firstCant;
                        //Se ho più di una regv, vado a prendere la seconda
                        if (orderedCurrentTripsByColByDate.Count() > 1)
                        {
                            regE = orderedCurrentTripsByColByDate.ElementAt(1);
                            firstCant = RepoManager.CantRepo.SingleOrDefault(c => c.Cant_Id == regE.Cant_Id, true);
                        }
                    }

                    else
                    {
                        //Recupero il cantiere marcato come sede. PUO' ESSERE SOLO UNO PER CLIENTE!
                        var headquartiers = RepoManager.CantRepo.Find(c => c.Tipo_Cantiere_Can == "SEDE");
                        if (headquartiers.Count() == 1)
                        {
                            sede = headquartiers.ElementAt(0);
                        }
                        else
                        {
                            //_log.Error("Durante la generazione dei viaggi sono stati trovati più cantieri marcati come SEDE");
                        }
                    }

                    //Se non mi trovo in sede (quindi la prima o la seconda timbratura non sono state fatte in sede), calcolo il viaggio di inizio giornata
                    if (firstCant.Tipo_Cantiere_Can != "SEDE" && firstCant != default(Cant))
                    {
                        var firstTrip = GenerateStartEndTripToHeadQuarter(sede, firstCant, regE, threshold);
                        if (firstTrip != null)
                        {
                            tripList.Add(firstTrip);
                        }
                    }


                    /* --- GESTIONE VIAGGIO FINE GIORNATA --- */
                    //Se l'ultima timbratura di giornata è stata fatta nella sede, prendo come ultimo cantiere quello della penultima regv di giornata
                    if (lastCant.Tipo_Cantiere_Can == "SEDE")
                    {
                        //Se ho più di una regv, vado a prendere la penultima
                        if (orderedCurrentTripsByColByDate.Count() > 1)
                        {
                            regU = orderedCurrentTripsByColByDate.ElementAt(orderedCurrentTripsByColByDate.Count() - 2);
                            lastCant = RepoManager.CantRepo.SingleOrDefault(c => c.Cant_Id == regU.Cant_Id, true);
                        }
                    }

                    //Se non mi trovo in sede (quindi l'ultima o la penultima timbratura non sono state fatte in sede), calcolo il viaggio di fine giornata
                    if (lastCant.Tipo_Cantiere_Can != "SEDE" && lastCant != default(Cant))
                    {
                        var lastTrip = GenerateStartEndTripToHeadQuarter(sede, lastCant, regU, threshold, true);
                        if (lastTrip != null)
                        {
                            tripList.Add(lastTrip);
                        }
                    }
                }

                else
                {
                    var firstTrip = GenerateStartEndTrip(firstCant, currentCol, regE, threshold);
                    if (firstTrip != null)
                        tripList.Add(firstTrip);

                    var lastTrip = GenerateStartEndTrip(lastCant, currentCol, regU, threshold, true);
                    if (lastTrip != null)
                        tripList.Add(lastTrip);
                }
            }

            return tripList;
        }

        private Trip GenerateStartEndTrip(Cant currentCant, Col currentCol, Reg_V regE, TimeSpan threshold, bool isRegU = false)
        //Generazione dei Viaggi di Inizio e Fine Giornata
        {
            Trip trip = null;
            //cercain Tab Distanze un Record valido con Chiave Z/K/P di Col/Cant (x Viaggio Inizio GG) e/o Cant/Col (x Viaggio Fine GG) 
            //e viceversa se non trovato
            var distRow = RepoManager.Tab_DistRepo.FindValidTab_Dist(currentCant, currentCol, isRegU);

            if (distRow != null)
            {
                // viene generato il viaggio solamente se non ci sono impedimenti dalle personalizzazioni per i viaggi nello stesso comune
                if (!IsTripSameMunicipalityToDelete(distRow))
                {
                    var from = regE.Data_Reg.Value.Add(threshold);
                    var to = from.Date.AddDays(1);
                    if (threshold > DateTime.Now.Date.TimeOfDay)
                        to = to.Add(threshold);

                    Reg newRegE = RepoManager.RegRepo.Init();
                    Reg newRegU = RepoManager.RegRepo.Init();
                    newRegU.ParentReg = newRegE;

                    DateTime timeDiffFig;
                    DateTime timeDiffFis;

                    //Se la regv è un passaggio (non ha regU), per evitar rogne imposto la regU uguale alla regE
                    if (regE.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass)
                    {
                        regE.Data_Ora_Fig_U = regE.Data_Ora_Fig_E;
                        regE.Data_Ora_Fis_U = regE.Data_Ora_Fis_E;
                    }

                    if (!isRegU)
                    {
                        timeDiffFig = regE.Data_Ora_Fig_E.Value.Subtract(new TimeSpan(0, (int)distRow.Minuti_Tab_Dist, 0));
                        timeDiffFis = regE.Data_Ora_Fis_E.Subtract(new TimeSpan(0, (int)distRow.Minuti_Tab_Dist, 0));

                        timeDiffFig = new DateTime(timeDiffFig.Year, timeDiffFig.Month, timeDiffFig.Day, timeDiffFig.Hour, timeDiffFig.Minute, 59);
                        timeDiffFis = new DateTime(timeDiffFis.Year, timeDiffFis.Month, timeDiffFis.Day, timeDiffFis.Hour, timeDiffFis.Minute, 59);

                        newRegE.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;

                        newRegU.Registrazione_Data_Ora_Orig_Reg = regE.Data_Ora_Fis_E;
                        newRegU.Registrazione_Data_Ora_Fis_Reg = regE.Data_Ora_Fis_E;
                        newRegU.Registrazione_Data_Ora_Fig_Reg = regE.Data_Ora_Fig_E;
                    }
                    else
                    {
                        timeDiffFig = regE.Data_Ora_Fig_U.Value.Add(new TimeSpan(0, distRow.Minuti_Tab_Dist, 0));
                        timeDiffFis = regE.Data_Ora_Fis_U.Value.Add(new TimeSpan(0, distRow.Minuti_Tab_Dist, 0));

                        var tripUFisDateTime = new DateTime(regE.Data_Ora_Fis_U.Value.Year, regE.Data_Ora_Fis_U.Value.Month, regE.Data_Ora_Fis_U.Value.Day, regE.Data_Ora_Fis_U.Value.Hour, regE.Data_Ora_Fis_U.Value.Minute, 59);
                        var tripUFigDateTime = regE.Data_Ora_Fig_U;
                        if (tripUFigDateTime.HasValue)
                            tripUFigDateTime = new DateTime(regE.Data_Ora_Fis_U.Value.Year, regE.Data_Ora_Fis_U.Value.Month, regE.Data_Ora_Fis_U.Value.Day, regE.Data_Ora_Fig_U.Value.Hour, regE.Data_Ora_Fig_U.Value.Minute, 59);

                        newRegE.Registrazione_Data_Ora_Orig_Reg = tripUFisDateTime;
                        newRegE.Registrazione_Data_Ora_Fis_Reg = tripUFisDateTime;
                        newRegE.Registrazione_Data_Ora_Fig_Reg = tripUFigDateTime;

                        newRegU.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;
                    }

                    if (newRegE.Registrazione_Data_Ora_Fis_Reg > from && newRegU.Registrazione_Data_Ora_Fis_Reg < to)
                    {
                        newRegE.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                        newRegE.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                        newRegE.Col_Id = currentCol.Col_Id;
                        newRegE.Cant_Id = currentCant.Cant_Id;
                        newRegE.Att_Id = newRegE.Cant_Id;

                        newRegU.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                        newRegU.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                        newRegU.Col_Id = currentCol.Col_Id;
                        newRegU.Cant_Id = currentCant.Cant_Id;
                        newRegU.Att_Id = newRegU.Cant_Id;

                        if (distRow.KM_Tab_Dist > default(decimal))
                            newRegE.KM_Reg = distRow.KM_Tab_Dist;
                        else
                            newRegE.KM_Reg = 0;

                        int coupleNumber = CommonService.GetRandomNumber();
                        newRegE.RiferimentoRRN_Att = coupleNumber;
                        newRegU.RiferimentoRRN_Att = coupleNumber;

                        trip = new Trip { RegE = newRegE, RegU = newRegU };
                    }
                }
            }
            return trip;
        }


        /// <summary>
        /// Genera i viaggi di inizio/fine giornata dalla/alla sede.
        /// </summary>
        /// <param name="currentCant">The current cant.</param>
        /// <param name="currentCol">The current col.</param>
        /// <param name="regE">The reg e.</param>
        /// <param name="threshold">The threshold.</param>
        /// <param name="elaborateUserId">The elaborate user identifier.</param>
        /// <param name="elaborateDateTime">The elaborate date time.</param>
        /// <param name="application">The application.</param>
        /// <param name="isRegU">if set to <c>true</c> [is reg u].</param>
        /// <returns></returns>
        private Trip GenerateStartEndTripToHeadQuarter(Cant sede, Cant cant, Reg_V regE, TimeSpan threshold, bool tripEnd = false)
        //Generazione dei Viaggi di Inizio e Fine Giornata
        {
            Trip trip = null;
            Col currentCol = RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == regE.Col_Id.Value);
            //cercain Tab Distanze un Record valido con Chiave Z/K/P di Col/Cant (x Viaggio Inizio GG) e/o Cant/Col (x Viaggio Fine GG) 
            //e viceversa se non trovato
            var distRow = RepoManager.Tab_DistRepo.FindCantCantTab_Dist(sede, cant, currentCol, tripEnd);

            if (distRow != null)
            {
                // viene generato il viaggio solamente se non ci sono impedimenti dalle personalizzazioni per i viaggi nello stesso comune
                if (!IsTripSameMunicipalityToDelete(distRow))
                {
                    var from = regE.Data_Reg.Value.Add(threshold);
                    var to = from.Date.AddDays(1);
                    if (threshold > DateTime.Now.Date.TimeOfDay)
                        to = to.Add(threshold);

                    Reg newRegE = RepoManager.RegRepo.Init();
                    Reg newRegU = RepoManager.RegRepo.Init();
                    newRegU.ParentReg = newRegE;

                    DateTime timeDiffFig;
                    DateTime timeDiffFis;

                    if (!tripEnd)
                    {
                        // Imposto come ora di inizio viaggio il minuto precedente la regv
                        timeDiffFig = regE.Data_Ora_Fig_E.Value.AddMinutes(-1);
                        timeDiffFis = regE.Data_Ora_Fis_E.AddMinutes(-1);

                        timeDiffFig = new DateTime(timeDiffFig.Year, timeDiffFig.Month, timeDiffFig.Day, timeDiffFig.Hour, timeDiffFig.Minute, 59);
                        timeDiffFis = new DateTime(timeDiffFis.Year, timeDiffFis.Month, timeDiffFis.Day, timeDiffFis.Hour, timeDiffFis.Minute, 59);

                        newRegE.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;

                        // Imposto come ora di fine viaggio l'ora di entrata nel prossimo cantiere
                        newRegU.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;
                    }
                    else
                    {
                        timeDiffFig = regE.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? regE.Data_Ora_Fig_U.Value : regE.Data_Ora_Fig_E.Value;
                        timeDiffFis = regE.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? regE.Data_Ora_Fis_U.Value : regE.Data_Ora_Fis_E;

                        timeDiffFig = new DateTime(timeDiffFig.Year, timeDiffFig.Month, timeDiffFig.Day, timeDiffFig.Hour, timeDiffFig.Minute, 59);
                        timeDiffFis = new DateTime(timeDiffFis.Year, timeDiffFis.Month, timeDiffFis.Day, timeDiffFis.Hour, timeDiffFis.Minute, 59);

                        newRegE.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;

                        newRegU.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;
                    }

                    if (newRegE.Registrazione_Data_Ora_Fis_Reg > from && newRegU.Registrazione_Data_Ora_Fis_Reg < to)
                    {
                        if (!tripEnd)
                        {
                            newRegE.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                            newRegE.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                            newRegE.Col_Id = regE.Col_Id;
                            //Se sto elaborando il viaggio di inizio giornata, parte dalla sede
                            newRegE.Cant_Id = sede.Cant_Id;
                            newRegE.Att_Id = newRegE.Cant_Id;

                            newRegU.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                            newRegU.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                            newRegU.Col_Id = regE.Col_Id;
                            //Se sto elaborando il viaggio di inizio giornata, arriva al primo cantiere lavorativo
                            newRegU.Cant_Id = cant.Cant_Id;
                            newRegU.Att_Id = newRegU.Cant_Id;
                        }

                        else
                        {
                            newRegE.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                            newRegE.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                            newRegE.Col_Id = regE.Col_Id;
                            //Se sto elaborando il viaggio di fine giornata, parte dall'ultimo cantiere lavorativo
                            newRegE.Cant_Id = cant.Cant_Id;
                            newRegE.Att_Id = newRegE.Cant_Id;

                            newRegU.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                            newRegU.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                            newRegU.Col_Id = regE.Col_Id;
                            //Se sto elaborando il viaggio di fine giornata, arriva alla sede
                            newRegU.Cant_Id = sede.Cant_Id;
                            newRegU.Att_Id = newRegU.Cant_Id;
                        }

                        if (distRow.KM_Tab_Dist > default(decimal))
                            newRegE.KM_Reg = distRow.KM_Tab_Dist;
                        else
                            newRegE.KM_Reg = 0;

                        int coupleNumber = CommonService.GetRandomNumber();
                        newRegE.RiferimentoRRN_Att = coupleNumber;
                        newRegU.RiferimentoRRN_Att = coupleNumber;

                        trip = new Trip { RegE = newRegE, RegU = newRegU };
                    }
                }
            }
            return trip;
        }


        /// <summary>
        /// Determina se un viaggio è totalmente compreso in una fascia
        /// </summary>
        /// <param name="start">L'inizio della fascia.</param>
        /// <param name="end">La fine della fascia.</param>
        /// <param name="trip">Il viaggio.</param>
        /// <returns><c>true</c> se il viaggio è totalmente compreso in una fascia; altrimenti <c>false</c></returns>
        private bool IsTripInRange(TimeSpan start, TimeSpan end, Trip trip)
        {
            if (start == TimeSpan.Zero && end == TimeSpan.Zero)
                return false;
            if (trip.RegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= start && trip.RegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= end)
                return true;
            else
                return false;
        }

        private DateTime AdjustMinutes(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 59);
        }

        private bool IsToDecuple(Reg regToCheck, DateTime startDate, DateTime endDate, Tuple<bool, NocturneTypeEnum, TimeSpan, TimeSpan> nocturneGlobalConfiguration)
        {
            // viene negata, in caso di abilitazione notturno, la possibilità di disaccoppiare registrazioni che siano già accoppiate, nel primo e ultimo giorno d'elaborazione,
            // e fuori da parametro del notturno specificato; questo per evitare che si disaccoppino registrazioni abbinate a giorni non trattati dall'elaborate

            // di default la registrazione specificata risulta atta al disaccoppiamento
            bool isRegToDecuple = true;

            // si procede ad effettuare la verifica solamente se il modulo notturno risulta abilitato e 
            if (nocturneGlobalConfiguration.Item1)
            {
                // se si sta processando una registrazione che si incontra nel primo o ultimo giorno d'elaborazione risulta abbinata
                if (regToCheck.Registrazione_Data_Ora_Fis_Reg.Date == startDate || regToCheck.Registrazione_Data_Ora_Fis_Reg == endDate
                    && regToCheck.Registrazione_Stato_Reg == (int)RegStateEnum.Ass)
                {

                    #region Calcolo dei parametri del notturno attualizzati su anagrafiche registrazione

                    // per la registrazione si recupera il cantiere e il collaboratore
                    Col currentCol = regToCheck.Col;
                    Cant currentCant = regToCheck.Cant;

                    // sono per prima cosa impostate le configurazioni del collaboratore;
                    // se il collaboratore non è stato configurato si scala sul cantiere, recuperando le sue configurazioni;
                    // in caso anche il cantiere non sia stato configurato si scala sui parametri generati, recuperando quelle configurazioni
                    NocturneTypeEnum nocturneType = NocturneTypeEnum.None;
                    TimeSpan nocturneThreshold = TimeSpan.Zero;
                    TimeSpan nocturneDuration = TimeSpan.Zero;

                    // impostazione della configurazione del collaboratore, se presente
                    if (currentCol != default(Col))
                    {
                        nocturneType = currentCol.NocturneTypeEnum;
                        nocturneThreshold = currentCol.Durata_Max_Gruppo_Notte_Ril_Col ?? TimeSpan.Zero;
                    }

                    // se il collaboratore non risulta configurato allora si procede al recupero delle configurazioni del cantiere, se presente
                    if ((nocturneType == NocturneTypeEnum.None || nocturneType == NocturneTypeEnum.Disabled) && currentCant != default(Cant))
                    {
                        nocturneType = currentCant.NocturneTypeEnum;
                        nocturneThreshold = currentCant.Durata_Max_Gruppo_Notte_Ril_Can ?? TimeSpan.Zero;
                    }

                    // se il collaboratore e il cantiere non risultano configurati allora si procede al recupero delle configurazioni generali nei parametri
                    // (si controlla solamente il valore "None" perché se disabilitato su cantiere e collaboratore allora non lo si processa)
                    if (nocturneType == NocturneTypeEnum.None)
                    {
                        nocturneType = nocturneGlobalConfiguration.Item2;
                        nocturneThreshold = nocturneGlobalConfiguration.Item3;
                    }

                    #endregion

                    // se per la registrazione il notturno risulta abilitato
                    if (nocturneType == NocturneTypeEnum.OverMidnight)
                    {
                        // se la registrazione è nel primo giorno e antecedente alla mezzanotte di notturno o nell'ultimo giorno successivamente
                        // alla mezzanotte di notturno allora non la si deve disaccoppiare
                        if ((regToCheck.Registrazione_Data_Ora_Fis_Reg.Date == startDate && regToCheck.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < nocturneThreshold) ||
                            (regToCheck.Registrazione_Data_Ora_Fis_Reg.Date == endDate && regToCheck.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > nocturneThreshold))
                        {
                            isRegToDecuple = false;
                        }
                    }

                    // se per la registrazione il notturno risulta abilitato
                    else if (nocturneType == NocturneTypeEnum.Duration && regToCheck.Registrazione_Data_Ora_Fis_Reg.Date == startDate)
                    {

                        //viene estratto l'rrn della registrazione di riferimento
                        int rrnReg = 0;

                        if (regToCheck.RiferimentoRRN_Reg.HasValue)
                            rrnReg = regToCheck.RiferimentoRRN_Reg.Value;

                        Reg regE = new Reg();

                        //se l'rrn non è null si tratta di una registrazione di uscita
                        if (rrnReg != 0)
                        {
                            //viene estratta la registrazione di entrata corrispondente all'
                            regE = RepoManager.RegRepo.FirstOrDefault(r => r.Reg_Id == rrnReg);

                            if (regE != default(Reg))
                            {
                                if (regE.Registrazione_Data_Ora_Fis_Reg.Date == startDate.AddDays(-1))
                                    isRegToDecuple = false;
                            }

                            //ATTENZIONE:potrebbe esserci un problema da provare con molte registrazioni

                            //// se la registrazione è nel primo giorno e antecedente alla mezzanotte di notturno o nell'ultimo giorno successivamente
                            //// alla mezzanotte di notturno allora non la si deve disaccoppiare
                            //if ((regToCheck.Registrazione_Data_Ora_Fis_Reg.Date == startDate && regToCheck.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < nocturneThreshold) ||
                            //    (regToCheck.Registrazione_Data_Ora_Fis_Reg.Date == endDate && regToCheck.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > nocturneThreshold))
                            //{
                            //    isRegToDecuple = false;
                            //}
                        }
                    }
                }
            }
            // ritorno del valore che indica se la registrazione dovrà essere disaccoppiata
            return isRegToDecuple;
        }

        /// <summary>
        /// Abbina le registrazioni specificate se i parametri lo permettono.
        /// </summary>
        /// <param name="lastOpenInit">La registrazione d'entrata da accoppiare.</param>
        /// <param name="currentRegEnd">La registrazione d'uscita da accoppiare.</param>
        /// <param name="maxElapsed">La durata massima della registrazione.</param>
        /// <param name="minElapsed">La durata minima della registrazione.</param>
        /// <param name="nocturneEnum">Il tipo di notturno configurato.</param>
        /// <returns>L'elenco di eventuali errori riscontrati durante il processo</returns>
        private IEnumerable<KeyValuePair<string, string>> AssociateReg(Reg lastOpenInit, Reg currentRegEnd, TimeSpan maxElapsed, TimeSpan minElapsed, NocturneTypeEnum nocturneEnum)
        {
            var errors = new List<KeyValuePair<string, string>>();

            currentRegEnd.RiferimentoRRN_Reg = lastOpenInit.Reg_Id;
            TimeSpan delta = new TimeSpan(0, 0, 0);

            if ((nocturneEnum != NocturneTypeEnum.None) && (nocturneEnum != NocturneTypeEnum.Disabled))
                //Nel caso di Notturno Abilitato Verifico se l'Ora di Inizio è maggiore o minore dell'Ora di Fine
                if (currentRegEnd.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < lastOpenInit.Registrazione_Data_Ora_Fis_Reg.TimeOfDay)
                    //Se Uscita < Entrata --> Si tratta di 2 Registrazioni a Cavallo di Giorni diversi e quindi si fa  Entrata - Uscita
                    delta = (lastOpenInit.Registrazione_Data_Ora_Fis_Reg - currentRegEnd.Registrazione_Data_Ora_Fis_Reg).Duration();
                else
                    //Si tratta di 2 Registrazioni dello stesso Giorno e quindi si fa Uscita - Entrata
                    delta = currentRegEnd.Registrazione_Data_Ora_Fis_Reg - lastOpenInit.Registrazione_Data_Ora_Fis_Reg;
            else
                //Se non è attivo il Notturno si fa sempre e comunque Uscita - Entrata
                delta = currentRegEnd.Registrazione_Data_Ora_Fis_Reg - lastOpenInit.Registrazione_Data_Ora_Fis_Reg;

            if (delta > maxElapsed)
            {
                errors.Add(new KeyValuePair<string, string>(FunctionMessageEnum.AssociateReg.ToString(), BusinessService.GetLocalizedString(PowerWebResources.ERR_MAX_DURATA_REG) + lastOpenInit.Reg_Id));
                lastOpenInit.Registrazione_Stato_RegEnum |= RegStateEnum.ErrMax;
                currentRegEnd.Registrazione_Stato_RegEnum |= RegStateEnum.ErrMax;
            }
            else if (delta < minElapsed)
            {
                errors.Add(new KeyValuePair<string, string>(FunctionMessageEnum.AssociateReg.ToString(), BusinessService.GetLocalizedString(PowerWebResources.ERR_MIN_DURATA_REG) + lastOpenInit.Reg_Id));
                lastOpenInit.Registrazione_Stato_RegEnum |= RegStateEnum.ErrMin;
                currentRegEnd.Registrazione_Stato_RegEnum |= RegStateEnum.ErrMin;
            }
            else
            {
                lastOpenInit.Registrazione_Stato_RegEnum |= RegStateEnum.Ass;
                currentRegEnd.Registrazione_Stato_RegEnum |= RegStateEnum.Ass;
                lastOpenInit.Registrazione_Tipo_RegEnum = currentRegEnd.Registrazione_Tipo_RegEnum = RegTypeEnum.None;
                currentRegEnd.KM_Reg = lastOpenInit.KM_Reg;
                //currentRegEnd.Motivazione_Reg_Id = lastOpenInit.Motivazione_Reg_Id;
                currentRegEnd.Note_Reg = lastOpenInit.Note_Reg;
            }
            return errors;
        }


        /// <summary>
        /// Determina se un viaggio ha la partenza compresa in una fascia
        /// </summary>
        /// <param name="start">L'inizio della fascia.</param>
        /// <param name="start">La fine della fascia.</param>
        /// <param name="trip">Il viaggio.</param>
        /// <returns><c>true</c> se la partenza del viaggio è all'interno in una fascia; altrimenti <c>false</c></returns>
        private bool IsStartTripInRange(TimeSpan start, TimeSpan end, Trip trip)
        {
            return trip.RegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= start && trip.RegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= end;
        }

        /// <summary>
        /// Determina se, secondo le personalizzazioni, il viaggio generato per la specifica tabella distanze è nello stesso comune e da cancellare o meno.
        /// </summary>
        /// <param name="distRow">La tabella distanze da processare.</param>
        /// <returns><c>true</c> se il viaggio identificato dalla tabella distanze è da cancellare (stesso comune, km inferiori a parametro e personalizzazione attiva); altrimenti <c>false</c></returns>
        private bool IsTripSameMunicipalityToDelete(Tab_Dist distRow)
        {
            // inizializzazione del valore di ritorno del metodo
            bool isToDelete = false;

            // calcolo della personalizzazione
            int municipalityCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoCreateTripOnSameMunicipalityUnderKmEnum);

            // se la personalizzazione risulta attiva si calcola dai parametri della customization i km soglia
            decimal kmThreshold = 0;
            if (municipalityCustomization == (int)NoCreateTripOnSameMunicipalityUnderKmEnum.CreateOnlyIfInParam)
                kmThreshold = Convert.ToDecimal(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.NoCreateTripOnSameMunicipalityUnderKmEnum, "KmThreshold"));

            // - il viaggio risulta nello stesso comune solamente se il tipo tab distanza è P (Comune) o G (GIS);
            // - Se il tipo distanza è P arrivo e partenza devono essere uguali affinchè arrivo e partenza siano nello stesso comune;
            // - Se il tipo distanza è G allora arrivo e partenza devono avere la stringa antecedente al primo "|" uguali affinché risulti il viaggio nello stesso comune
            bool isSameMunicipality = false;
            if (distRow.DistType == DistTypeEnum.GIS || distRow.DistType == DistTypeEnum.Place)
            {
                string start = distRow.DistType == DistTypeEnum.GIS ? distRow.Partenza_Tab_Dist.Substring(0, distRow.Partenza_Tab_Dist.IndexOf('|')).ToUpper() : distRow.Partenza_Tab_Dist.ToUpper();
                string end = distRow.DistType == DistTypeEnum.GIS ? distRow.Arrivo_Tab_Dist.Substring(0, distRow.Arrivo_Tab_Dist.IndexOf('|')).ToUpper() : distRow.Arrivo_Tab_Dist.ToUpper();
                isSameMunicipality = start == end;
            }

            // se il viaggio che si sta generando parte e arriva nello stesso comune, è attiva la personalizzazione del controllo di km in viaggi per stesso comune e i km
            // assegnati alla tab distanze sono inferiori alla soglia, allora si procede a marcare il viaggio per la cancellazione; in caso contrario si procede alla sua generazione
            isToDelete = municipalityCustomization == (int)NoCreateTripOnSameMunicipalityUnderKmEnum.CreateOnlyIfInParam && isSameMunicipality && distRow.KM_Tab_Dist < kmThreshold;

            //Se il parametro di viaggio inizio/fine giornata è = 3 (generazione dei viaggi dalla sede), i viaggi non vengono generati se minori della soglia
            if (RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti_Inizio_Fine_G == (int)TripAssignmentTypeEnum.CalculateFromHeadquarter)
            {
                isToDelete = distRow.KM_Tab_Dist < kmThreshold;
            }

            // ritorno del valore del metodo
            return isToDelete;
        }


        private Dictionary<EntryLimitTypeEnum, EntryLimitData> GetEntryLimitConifg(Cant cant, Col col, DateTime date, TimeSpan midDay)
        {
            // inizializzazione del dizionario che conterrà le confgiurazioni da ritornare
            var returnDic = new Dictionary<EntryLimitTypeEnum, EntryLimitData>();

            // si calcolano i parametri del limite d'entrata recuperando i dati dai 3 elementi che li contengono e privilegiando la
            // gerarchia collaboratore, cantiere, parametri se non richiesto di utilizzare l'eventuale orario collegato al collaboratore;
            // in definitiva il limite d'entrata mattutino e pomeridiano è dato:
            // - in caso sia richiesto il recupero da orario e il collaboratore abbia un orario collegato, con definizione di entrata:
            //      - il limite d'entrata mattutino è dato dalla prima entrata pre metà giornata
            //      - il limite d'entrata pomeridiano è dato dalla prima entrata post metà giornata
            // - in caso non sia configurato il calcolo del limite d'entrata con l'orario si procede alla lettura dei
            //   parametri utilizzando la gerarchia:
            //      - collaboratore
            //      - cantiere
            //      - parametri

            // inizializzazione dei valori che conterranno i dati da restituire nel dizionario
            TimeSpan? morningEntryLimit = null;
            List<TimeSpan> morningEntryLimitList = null;
            List<TimeSpan> afternoonEntryLimitList = null;
            TimeSpan? afternoonEntryLimit = null;
            TimeSpan? afternoonEntryLimitTollerance = GetEntryLimitTolleranceValue(col, cant);

            #region LIMITE DI ENTRATA DA ORARIO
            // se è configurato l'utilizzo dell'orario per il calcolo del limite d'entrata
            if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Usa_Orario && col.Tab_Orari_Tipo_Id.HasValue)
            {
                // calcolo del piano di dettaglio per il giorno/collaboratore
                List<Tuple<int, TimeSpan, TimeSpan>> dayColPlanDetail = RepoManager.Tab_OrariRepo.GetDayPlanDetail(date, col.Col_Id);

                // se sono presenti dei piani con entrata e uscita per piano collaboratore
                if (dayColPlanDetail.Any())
                {
                    // selezione delle ore d'entrata e loro ordinamento
                    var sortedEntryTimes = dayColPlanDetail.Select(dayDetail => dayDetail.Item2).OrderBy(entryTime => entryTime).ToList();

                    // il limite d'entrata mattutino, se presente, è il primo valore nella prima metà della giornata
                    morningEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime < midDay);


                    //nel caso in cui vi siano più orari
                    if (sortedEntryTimes.Count >= 1)
                        //vengono estratti tutti i limiti di entrata mattutini
                        morningEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime < midDay).ToList();



                    // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                    afternoonEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime >= midDay);

                    //nel caso in cui vi siano più orari
                    if (sortedEntryTimes.Count >= 1)
                        //vengono estratti tutti i limiti di entrata pomeridiani
                        afternoonEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime >= midDay).ToList();

                }
            }
            #endregion

            //se non vengono estratti i limiti dal piano orario
            else
            {
                // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                if (col != null)
                {
                    // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                    if (col.Limite_Entrata_Mattina_Col.HasValue)
                        morningEntryLimit = col.Limite_Entrata_Mattina_Col;

                    // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                    if (col.Limite_Entrata_Pomeriggio_Col.HasValue)
                    {
                        afternoonEntryLimit = col.Limite_Entrata_Pomeriggio_Col;
                    }
                }

                // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                if (cant != null)
                {
                    // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                    if (!morningEntryLimit.HasValue && cant.Limite_Entrata_Mattina_Cant.HasValue)
                        morningEntryLimit = cant.Limite_Entrata_Mattina_Cant;

                    // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                    if (!afternoonEntryLimit.HasValue && cant.Limite_Entrata_Pomeriggio_Cant.HasValue)
                    {
                        afternoonEntryLimit = cant.Limite_Entrata_Pomeriggio_Cant;
                    }
                }

                // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                // si procede all'impostazione della variabile con il dato di configurazione centrale
                if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue && !morningEntryLimit.HasValue)
                    morningEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina;

                // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                // si procede all'impostazione delle variabili con il dato di configurazione centrale
                if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue && !afternoonEntryLimit.HasValue)
                {
                    afternoonEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio;
                }

            }

            // costruzione dei dati di ritorno con i calcoli precedentemente effettuati
            // (la tolleranza del limite mattutino è impostata a null in quanto non presente)
            returnDic.Add(EntryLimitTypeEnum.Morning, new EntryLimitData() { EntryLimitTime = morningEntryLimit, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.Afternoon, new EntryLimitData() { EntryLimitTime = afternoonEntryLimit, EntryLimitTollerance = afternoonEntryLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningDealyLimitList, new EntryLimitData() { EntryLimitTimeList = morningEntryLimitList, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonDealyLimitList, new EntryLimitData() { EntryLimitTimeList = afternoonEntryLimitList, EntryLimitTollerance = afternoonEntryLimitTollerance });


            // ritorno delle configurazioni calcolate dal metodo
            return returnDic;

        }

        private Dictionary<ExitLimitTypeEnum, ExitLimitData> GetExitLimitConifg(Cant cant, Col col, DateTime date, TimeSpan midDay)
        {
            var returnDic = new Dictionary<ExitLimitTypeEnum, ExitLimitData>();

            // si calcolano i parametri del limite d'entrata recuperando i dati dai 3 elementi che li contengono e privilegiando la
            // gerarchia collaboratore, cantiere, parametri se non richiesto di utilizzare l'eventuale orario collegato al collaboratore;
            // in definitiva il limite d'entrata mattutino e pomeridiano è dato:
            // - in caso sia richiesto il recupero da orario e il collaboratore abbia un orario collegato, con definizione di entrata:
            //      - il limite d'entrata mattutino è dato dalla prima entrata pre metà giornata
            //      - il limite d'entrata pomeridiano è dato dalla prima entrata post metà giornata
            // - in caso non sia configurato il calcolo del limite d'entrata con l'orario si procede alla lettura dei
            //   parametri utilizzando la gerarchia:
            //      - collaboratore
            //      - cantiere
            //      - parametri

            // inizializzazione dei valori che conterranno i dati da restituire nel dizionario
            TimeSpan? morningExitLimit = null;
            TimeSpan? morningExitLimitTollerance = GetExitLimitMorningTolleranceValue(col, cant);
            List<TimeSpan> morningExitLimitList = null;
            List<TimeSpan> afternoonExitLimitList = null;
            TimeSpan? afternoonExitLimit = null;
            TimeSpan? afternoonExitLimitTollerance = GetExitLimitAfternoonTolleranceValue(col, cant);

            #region LIMITE DI USCITA DA ORARIO
            // se è configurato l'utilizzo dell'orario per il calcolo del limite d'entrata
            if (RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Usa_Orario && col.Tab_Orari_Tipo_Id.HasValue)
            {
                // calcolo del piano di dettaglio per il giorno/collaboratore
                List<Tuple<int, TimeSpan, TimeSpan>> dayColPlanDetail = RepoManager.Tab_OrariRepo.GetDayPlanDetail(date, col.Col_Id);

                // se sono presenti dei piani con entrata e uscita per piano collaboratore
                if (dayColPlanDetail.Any())
                {
                    // selezione delle ore d'uscita e loro ordinamento
                    var sortedEntryTimes = dayColPlanDetail.Select(dayDetail => dayDetail.Item3).OrderBy(exitTime => exitTime).ToList();

                    // il limite d'uscita mattutino, se presente, è il primo valore nella prima metà della giornata
                    morningExitLimit = sortedEntryTimes.FirstOrDefault(exitTime => exitTime > midDay);


                    //nel caso in cui vi siano più orari
                    if (sortedEntryTimes.Count >= 1)
                        //vengono estratti tutti i limiti di uscita mattutini
                        morningExitLimitList = sortedEntryTimes.Where(exitTime => exitTime < midDay).ToList();



                    // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                    afternoonExitLimit = sortedEntryTimes.FirstOrDefault(exitTime => exitTime >= midDay);

                    //nel caso in cui vi siano più orari
                    if (sortedEntryTimes.Count >= 1)
                        //vengono estratti tutti i limiti di entrata pomeridiani
                        afternoonExitLimitList = sortedEntryTimes.Where(exitTime => exitTime >= midDay).ToList();

                }
            }
            #endregion

            //se non vengono estratti i limiti dal piano orario
            else
            {
                // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                if (col != null)
                {
                    // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                    if (col.Limite_Uscita_Mattina_Col.HasValue)
                        morningExitLimit = col.Limite_Uscita_Mattina_Col;

                    // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                    if (col.Limite_Uscita_Pomeriggio_Col.HasValue)
                    {
                        afternoonExitLimit = col.Limite_Uscita_Pomeriggio_Col;
                    }
                }

                // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                if (cant != null)
                {
                    // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                    if (!morningExitLimit.HasValue && cant.Limite_Uscita_Mattina_Cant.HasValue)
                        morningExitLimit = cant.Limite_Uscita_Mattina_Cant;

                    // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                    if (!afternoonExitLimit.HasValue && cant.Limite_Uscita_Pomeriggio_Cant.HasValue)
                    {
                        afternoonExitLimit = cant.Limite_Uscita_Pomeriggio_Cant;
                    }
                }

                // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                // si procede all'impostazione della variabile con il dato di configurazione centrale
                if (RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Mattina.HasValue && !morningExitLimit.HasValue)
                    morningExitLimit = RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Mattina;

                // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                // si procede all'impostazione delle variabili con il dato di configurazione centrale
                if (RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Pomeriggio.HasValue && !afternoonExitLimit.HasValue)
                {
                    afternoonExitLimit = RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Pomeriggio;
                }

            }

            // costruzione dei dati di ritorno con i calcoli precedentemente effettuati
            // (la tolleranza del limite mattutino è impostata a null in quanto non presente)
            returnDic.Add(ExitLimitTypeEnum.Morning, new ExitLimitData() { ExitLimitTime = morningExitLimit, ExitLimitTollerance = morningExitLimitTollerance });
            returnDic.Add(ExitLimitTypeEnum.Afternoon, new ExitLimitData() { ExitLimitTime = afternoonExitLimit, ExitLimitTollerance = afternoonExitLimitTollerance });
            returnDic.Add(ExitLimitTypeEnum.MorningDealyLimitList, new ExitLimitData() { ExitLimitTimeList = morningExitLimitList, ExitLimitTollerance = morningExitLimitTollerance });
            returnDic.Add(ExitLimitTypeEnum.AfternoonDealyLimitList, new ExitLimitData() { ExitLimitTimeList = afternoonExitLimitList, ExitLimitTollerance = afternoonExitLimitTollerance });


            // ritorno delle configurazioni calcolate dal metodo
            return returnDic;
        }

        /// <summary>
        /// Recupera la tolleranza del limite d'entrata utilizzando i dati specificati.
        /// </summary>
        /// <param name="col">Il collaboratore da cui estrarre in gerarchia la tolleranza del limite d'entrata pomeridiano.</param>
        /// <param name="cant">Il cantiere da cui estrarre in gerarchia la tolleranza del limite d'entrata pomeridiano.</param>
        /// <returns>La tolleranza del limite d'entrata pomeridiano presente nei dati specificati.</returns>
        private TimeSpan? GetEntryLimitTolleranceValue(Col col, Cant cant)
        {
            // la tolleranza del limite d'entrata pomeridiano viene calcolata seguendo la seguente gerarchia:
            // - quella del collaboratore se presente
            // - quella del cantiere se presente
            // - quella della scheda parametri se presente

            TimeSpan? entryLimitTollerance = null;

            if (col.Tolleranza_Limite_Entrata_Pomeriggio_Col.HasValue)
                entryLimitTollerance = col.Tolleranza_Limite_Entrata_Pomeriggio_Col;

            if (!entryLimitTollerance.HasValue && cant.Tolleranza_Limite_Entrata_Pomeriggio_Cant.HasValue)
                entryLimitTollerance = cant.Tolleranza_Limite_Entrata_Pomeriggio_Cant;

            if (!entryLimitTollerance.HasValue && RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Entrata_Pomeriggio.HasValue)
                entryLimitTollerance = RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Entrata_Pomeriggio;

            return entryLimitTollerance;
        }

        private TimeSpan? GetExitLimitMorningTolleranceValue(Col col, Cant cant)
        {
            TimeSpan? exitLimitMorningTollerance = null;

            if (col.Tolleranza_Limite_Uscita_Mattina_Col.HasValue)
                exitLimitMorningTollerance = col.Tolleranza_Limite_Uscita_Mattina_Col;

            if (!exitLimitMorningTollerance.HasValue && cant.Tolleranza_Limite_Uscita_Mattina_Cant.HasValue)
                exitLimitMorningTollerance = cant.Tolleranza_Limite_Uscita_Mattina_Cant;

            if (!exitLimitMorningTollerance.HasValue && RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Uscita_Mattina.HasValue)
                exitLimitMorningTollerance = RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Uscita_Mattina;

            return exitLimitMorningTollerance;
        }

        private TimeSpan? GetExitLimitAfternoonTolleranceValue(Col col, Cant cant)
        {
            TimeSpan? exitLimitAfternoonTollerance = null;

            if (col.Tolleranza_Limite_Uscita_Pomeriggio_Col.HasValue)
                exitLimitAfternoonTollerance = col.Tolleranza_Limite_Uscita_Pomeriggio_Col;

            if (!exitLimitAfternoonTollerance.HasValue && cant.Tolleranza_Limite_Uscita_Pomeriggio_Cant.HasValue)
                exitLimitAfternoonTollerance = cant.Tolleranza_Limite_Uscita_Pomeriggio_Cant;

            if (!exitLimitAfternoonTollerance.HasValue && RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Uscita_Pomeriggio.HasValue)
                exitLimitAfternoonTollerance = RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Uscita_Pomeriggio;

            return exitLimitAfternoonTollerance;
        }

        /// <summary>
        /// Entità utilizzata per rappresentare un viaggio
        /// </summary>
        private class Trip
        {
            public Reg RegE { get; set; }
            public Reg RegU { get; set; }

            public int? cantIdStart { get; set; }

            /// <summary>
            /// Recupera o imposta un valore che indica se il viaggio che si sta generando ha come partenza un cantiere di ore non lavorate.
            /// </summary>
            /// <value>
            /// <c>true</c> se il viaggio che si sta generando ha come partenza un cantiere di ore non lavorate; altrimenti, <c>false</c>.
            /// </value>
            public bool IsFromOnl { get; set; }

            /// <summary>
            /// Recupera o imposta un valore che indica se il viaggio che si sta generando ha come arrivo un cantiere di ore non lavorate.
            /// </summary>
            /// <value>
            /// <c>true</c> se il viaggio che si sta generando ha come arrivo un cantiere di ore non lavorate; altrimenti, <c>false</c>.
            /// </value>
            public bool IsToOnl { get; set; }

            public TimeSpan TripDuration
            {
                get
                {
                    return RegU.Registrazione_Data_Ora_Fis_Reg.Subtract(RegE.Registrazione_Data_Ora_Fis_Reg);
                }
            }

            public List<Reg> Regs
            {
                get
                {
                    return new List<Reg> { RegE, RegU };
                }
            }
        }

        /// <summary>
        /// Classe che rappresenta i dati di calcolo del limite d'entrata
        /// </summary>
        public class EntryLimitData
        {

            #region Fields

            /// <summary>
            /// L'ora che indica il limite d'entrata
            /// </summary>
            private TimeSpan? _entryLimitTime = null;

            /// <summary>
            /// Lista  di ore che indica il limite d'entrata
            /// </summary>
            private List<TimeSpan> _entryLimitTimeList = null;

            /// <summary>
            /// Il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata
            /// </summary>
            private TimeSpan? _entryLimitTollerance = null;

            #endregion

            #region Properties

            /// <summary>
            /// Recupera o imposta l'ora che indica il limite d'entrata.
            /// </summary>
            /// <value>
            /// L'ora che indica il limite d'entrata.
            /// </value>
            public TimeSpan? EntryLimitTime
            {
                get
                {
                    return _entryLimitTime;
                }
                set
                {
                    _entryLimitTime = value;
                }
            }

            /// <summary>
            /// Recupera o imposta la lista delle ora che indica il limite d'entrata.
            /// </summary>
            /// <value>
            /// Lista di ore che indica il limite d'entrata.
            /// </value>
            public List<TimeSpan> EntryLimitTimeList
            {
                get
                {
                    return _entryLimitTimeList;
                }
                set
                {
                    _entryLimitTimeList = value;
                }
            }

            /// <summary>
            /// Recupera o imposta il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata.
            /// </summary>
            /// <value>
            /// Il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata.
            /// </value>
            public TimeSpan? EntryLimitTollerance
            {
                get
                {
                    return _entryLimitTollerance;
                }
                set
                {
                    _entryLimitTollerance = value;
                }
            }

            /// <summary>
            /// Recupera il valore che indica se l'istanza corrente è un limite d'entrata configurato.
            /// </summary>
            /// <value>
            /// <c>true</c> se l'istanza corrente è un limite d'entrata configurato; altrimenti, <c>false</c>.
            /// </value>
            public bool IsConfigured
            {
                get
                {
                    // l'istanza corrente risulta configurata se il limite d'entrata impostato ha un valore
                    return EntryLimitTime.HasValue;
                }
            }

            #endregion

        }

        public class ExitLimitData
        {
            #region Fields

            /// <summary>
            /// L'ora che indica il limite d'entrata
            /// </summary>
            private TimeSpan? _exitLimitTime = null;

            /// <summary>
            /// Lista  di ore che indica il limite d'entrata
            /// </summary>
            private List<TimeSpan> _exitLimitTimeList = null;

            /// <summary>
            /// Il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata
            /// </summary>
            private TimeSpan? _exitLimitTollerance = null;

            #endregion

            #region Properties

            /// <summary>
            /// Recupera o imposta l'ora che indica il limite d'entrata.
            /// </summary>
            /// <value>
            /// L'ora che indica il limite d'entrata.
            /// </value>
            public TimeSpan? ExitLimitTime
            {
                get
                {
                    return _exitLimitTime;
                }
                set
                {
                    _exitLimitTime = value;
                }
            }

            /// <summary>
            /// Recupera o imposta la lista delle ora che indica il limite d'entrata.
            /// </summary>
            /// <value>
            /// Lista di ore che indica il limite d'entrata.
            /// </value>
            public List<TimeSpan> ExitLimitTimeList
            {
                get
                {
                    return _exitLimitTimeList;
                }
                set
                {
                    _exitLimitTimeList = value;
                }
            }

            /// <summary>
            /// Recupera o imposta il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata.
            /// </summary>
            /// <value>
            /// Il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata.
            /// </value>
            public TimeSpan? ExitLimitTollerance
            {
                get
                {
                    return _exitLimitTollerance;
                }
                set
                {
                    _exitLimitTollerance = value;
                }
            }

            /// <summary>
            /// Recupera il valore che indica se l'istanza corrente è un limite d'entrata configurato.
            /// </summary>
            /// <value>
            /// <c>true</c> se l'istanza corrente è un limite d'entrata configurato; altrimenti, <c>false</c>.
            /// </value>
            public bool IsConfigured
            {
                get
                {
                    // l'istanza corrente risulta configurata se il limite d'entrata impostato ha un valore
                    return ExitLimitTime.HasValue;
                }
            }

            #endregion
        }

        /// <summary>
        /// Rappresenta il tipo di limite d'entrata utilizzabile dall'applicativo
        /// </summary>
        public enum EntryLimitTypeEnum
        {

            /// <summary>
            /// Limite d'entrata mattutino
            /// </summary>
            Morning,

            /// <summary>
            /// Limite d'entrata pomeridiano
            /// </summary>
            Afternoon,

            /// <summary>
            /// Lista dei limiti mattutini per determinare gli orari
            /// </summary>
            MorningDealyLimitList,

            /// <summary>
            /// Lista dei limiti pomeridiani per determinare gli orari
            /// </summary>
            AfternoonDealyLimitList

        }

        public enum ExitLimitTypeEnum
        {
            /// <summary>
            /// Limite d'entrata mattutino
            /// </summary>
            Morning,

            /// <summary>
            /// Limite d'entrata pomeridiano
            /// </summary>
            Afternoon,

            /// <summary>
            /// Lista dei limiti mattutini per determinare gli orari
            /// </summary>
            MorningDealyLimitList,

            /// <summary>
            /// Lista dei limiti pomeridiani per determinare gli orari
            /// </summary>
            AfternoonDealyLimitList
        }
    }
}

