using Business.DataClasses.SupportClasses;
using Business.MDBSchema;
using Common;
using Common.Properties;
using Data;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraRichEdit.Layout;
using Domain;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Business.Repository.Custom
{
    public class RegRepository : GenericRepository<Reg>, IRegRepository
    {
        public RegRepository(PowerWebEntities context)
            : base(context)
        {
        }

        private static readonly ILog _log = LogManager.GetLogger(typeof(Reg));
        private Reg _regStub = null;

        private static Dictionary<Utenti, KeyValuePair<double, string>> _elaborateStatusDictionary = new Dictionary<Utenti, KeyValuePair<double, string>>();

        private static List<Tab_Decod> Tab_Decods
        {
            get
            {
                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_Reg");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_DecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Reg", oLista);
                }
                return oLista;
            }
        }
        private static List<Fru> Frus
        {
            get
            {
                List<Fru> oLista = PowerWebContext.GetFromSession<List<Fru>>("Frus_RegRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.FruRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Fru>>("Frus_RegRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Pru> Prus
        {
            get
            {
                List<Pru> oLista = PowerWebContext.GetFromSession<List<Pru>>("Prus_RegRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.PruRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Pru>>("Prus_RegRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Col> Cols
        {
            get
            {
                List<Col> oLista = PowerWebContext.GetFromSession<List<Col>>("Cols_RegRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.ColRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Col>>("Cols_RegRepo", oLista);
                }
                return oLista;
            }
        }

        #region Dati elaborate per tab messaggi

        /// <summary>
        /// L'id dell'utente che ha lanciato l'elaborazione
        /// </summary>
        private int? _elaborateUserId;

        /// <summary>
        /// La data e ora di avvio dell'elaborazione
        /// </summary>
        private DateTime? _elaborateDateTime;

        #endregion

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Fru>>("Frus_RegRepo", null);
            PowerWebContext.SetToSession<List<Pru>>("Prus_RegRepo", null);
            PowerWebContext.SetToSession<List<Cant>>("Cants_RegRepo", null);
            PowerWebContext.SetToSession<List<Col>>("Cols_RegRepo", null);
            PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_RegRepo", null);
        }

        private static Fru GetCurrentFru(String machineRef)
        //Restituisce NULL se il Codice ricevuto NON è presente come Portatile in PRU
        {
            return RepoManager.FruRepo.SingleOrDefault(fr => fr.Codice_Fru == machineRef);
        }

        private static Pru GetCurrentPru(String machineRef)
        //Resituisce NULL se il Codice ricevuto NON è presente come Portatile in PRU
        {
            return RepoManager.PruRepo.SingleOrDefault(pr => pr.Codice_Pru == machineRef);
        }

        /// <summary>
        /// Restituisce l'oggetto anagrafica PRU o FRU per la matricola specifica.
        /// </summary>
        /// <param name="machineRef">La matricola da ricercare.</param>
        /// <returns>L'anagrafica PRU o FRU corrispondente alla matricola specificata; null in caso di non presenza in nessuna delle due anagrafiche</returns>
        public static Object GetPruFruFromRef(String machineRef)
        {
            Object currentMachine = GetCurrentFru(machineRef);

            if (currentMachine == null)
                //Verifica se esiste come Portatile in Tab PRU
                currentMachine = GetCurrentPru(machineRef);


            return currentMachine;
        }

        /// <summary>
        /// Aggiorna le date di inizio e fine periodo da elaborare con le eventuali date presenti nella tabella PendingElab se queste sono minori o maggiori.
        /// </summary>
        /// <param name="originalFrom">Il valore data di inizio ricerca originale.</param>
        /// <param name="originalTo">Il valore data di fine ricerca originale.</param>
        /// <param name="newFrom">Il valore data di inizio ricerca aggiornato.</param>
        /// <param name="newTo">Il valore data di fine ricerca aggiornato.</param>
        /// <returns>Ritorna un booleano che segnala se le date originarie sono state modificate per la presenza di un periodo di elaborazione</returns>
        public bool UpdatePeriodoWithPendingElabDates(DateTime originalFrom, DateTime originalTo, out DateTime newFrom, out DateTime newTo)
        //Aggiorna le date di inizio e fine periodo da elaborare con le eventuali date presenti nella tabella PendingElab se queste sono minori o maggiori.
        {
            // inizializzazione del valore di ritorno del metodo
            bool dateChanged = false;

            newFrom = originalFrom;
            newTo = originalTo;

            // recupero di eventuali elaborazioni pendenti recuperate
            var toIncludePendingElab = RepoManager.PendingElabRepo.Find(pe => pe.ElaborateDate_PendingElab == null).ToList();

            // se ci sono delle elaborazioni da prendere in carico
            if (toIncludePendingElab.Count > 0)
            {
                foreach (var pendingElab in toIncludePendingElab)
                {
                    if (pendingElab.FromDate_PendingElab.HasValue && pendingElab.FromDate_PendingElab < originalFrom)
                    {
                        newFrom = pendingElab.FromDate_PendingElab.Value;
                        dateChanged = true;
                    }

                    if (pendingElab.ToDate_PendingElab.HasValue && pendingElab.ToDate_PendingElab > originalTo)
                    {
                        newTo = pendingElab.ToDate_PendingElab.Value;
                        dateChanged = true;
                    }
                }
            }

            return dateChanged;
        }

        /// <summary>
        /// Metodo che data una reg prima dell'update si occupa di verificare un eventuale cambio di cant e/o col e di conseguenza ne cancella i valori di pru e fru.
        /// Il controllo viene effettuato prima della scrittura su database e quindi non funziona su reg già scritte su db (con parametri opzionali non specificati).
        /// Con i parametri opzionali specificati (devono essere entrambi diversi da stringa vuota) si procede alla verifica non più sul db ma utilizzando quei valori.
        /// </summary>
        /// <param name="regToUpdate">La reg su cui effettuare le verifiche ed eventualmente le correzioni.</param>
        /// <param name="oldReg">La vecchia reg (opzionale) su cui effettuare i controlli.</param>
        public void ManageCantColChangesBeforeUpdate(Reg regToUpdate, Reg oldReg = null)
        {
            // si procede con l'elaborazione solamente se non si sta processando una reg nuova e non sono stati passati
            // come parametro un cant_id e un col_id validi
            if (regToUpdate.Reg_Id != 0 || oldReg != null)
            {
                // calcolo del codice cantiere e del codice collaboratore in base ai parametri passati al metodo
                int cantIdToCheck = 0;
                int colIdToCheck = 0;
                if (oldReg != null)
                {
                    cantIdToCheck = Convert.ToInt32(oldReg.Cant_Id);
                    colIdToCheck = Convert.ToInt32(oldReg.Col_Id);
                }
                else
                {
                    // viene recuperata l'attuale reg dal database
                    Reg currentReg = SingleOrDefault(reg => reg.Reg_Id == regToUpdate.Reg_Id);

                    cantIdToCheck = Convert.ToInt32(currentReg.Cant_Id);
                    colIdToCheck = Convert.ToInt32(currentReg.Col_Id);
                }

                // se nella reg da processare è cambiato il cant_id allora si procede all'annullamento del valore di fru_id
                if (cantIdToCheck != regToUpdate.Cant_Id)
                    regToUpdate.Fru_Id = null;

                // se nella reg da processare è cambiato il col_id allora si procede all'annullamento del valore di pru_id
                if (colIdToCheck != regToUpdate.Col_Id)
                    regToUpdate.Pru_Id = null;
            }
        }

        /// <summary>
        /// Gestisce il trasbordo/generazione delle dati originali per la reg in entrata e in uscita passata come parametro.
        /// Metodo che lavora solamente nei moduli onlie utilizzando i valori passati per il row updating.
        /// </summary>
        /// <param name="regEToProcess">La reg in entrata da processare.</param>
        /// <param name="regEToProcessId">l'id della reg in entrata da processare (per capire se si tratta di una nuova reg o meno).</param>
        /// <param name="regEOriginalDate">La data originale della reg in entrata da processare.</param>
        /// <param name="regUToProcess">La reg in uscita da processare.</param>
        /// <param name="regUToProcessId">l'id della reg in uscita da processare (per capire se si tratta di una nuova reg o meno).</param>
        /// <param name="regUOriginalDate">La data originale della reg in entrata da processare.</param>
        /// <param name="isSameDay"><c>true</c> se le due registrazioni sono dello stesso giorno, altrimenti <c>false</c></param>
        public void ManageOrigDates(Reg regEToProcess, int regEToProcessId,
            DateTime regEOriginalDate, Reg regUToProcess, int regUToProcessId, DateTime regUOriginalDate, bool isSameDay)
        {
            // nelle nuove reg da salvare sono riportate le date/ore originali (se si stanno inserendo reg nuove si inserisce la data ora originale attuale)
            regEToProcess.Registrazione_Data_Ora_Orig_Reg = regEToProcessId == 0 ? regEToProcess.Registrazione_Data_Ora_Fis_Reg : regEOriginalDate;

            // viene processata l'uscita solamente se diversa da null
            if (regUToProcess != null)
                regUToProcess.Registrazione_Data_Ora_Orig_Reg = regUToProcessId == 0 ? regUToProcess.Registrazione_Data_Ora_Fis_Reg : regUOriginalDate;

            // se è prevista la reg in uscita
            if (regUToProcess != null)
            {
                // se la data/ora fisica d'uscita è inferiore della data/ora fisica d'entrata
                if (regUToProcess.Registrazione_Data_Ora_Fis_Reg < regEToProcess.Registrazione_Data_Ora_Fis_Reg)
                {
                    // se sono due registrazioni dello stesso giorno e la reg in uscita risulta nuova allora gli si cambia la data
                    // originale così da far apparire un'entrata modifica
                    if (isSameDay)
                        regUToProcess.Registrazione_Data_Ora_Orig_Reg = regEToProcess.Registrazione_Data_Ora_Fis_Reg.AddSeconds(-1);
                }
            }


            // se le registrazioni sono nuove allora vanno annullati i valori di pru e fru
            // e la data origine è la data minima
            if (regEToProcessId == 0)
            {
                regEToProcess.Pru_Id = null;
                regEToProcess.Fru_Id = null;
                regEToProcess.Registrazione_Data_Ora_Orig_Reg = regEToProcess.Registrazione_Data_Ora_Fis_Reg.AddSeconds(-1);
            }

            // viene processata l'uscita solamente se diversa da null
            // e la data origine è la data minima
            if (regUToProcess != null)
            {
                if (regUToProcessId == 0)
                {
                    regUToProcess.Pru_Id = null;
                    regUToProcess.Fru_Id = null;
                    regUToProcess.Registrazione_Data_Ora_Orig_Reg = regEToProcess.Registrazione_Data_Ora_Fis_Reg.AddSeconds(-1);
                }
            }
        }

        #region Elaborazione delle timbrature

        /// <summary>
        /// Elabora le specifiche registrazioni effettuando gli abbinamenti, gli arrotondamenti, la generazione dei viaggi e tutti i processi necessari alla trasformazione
        /// delle timbrature da file di testo in registrazioni vere e proprie con tutte le loro caratteristiche.
        /// ATTENZIONE: questo metodo potrebbe generare anomalie (in particolare nell'associazione delle attività e nella generazione dei viaggi) se le registrazioni passate
        ///             come parametro per l'elaborazione non sono tutte quelle presenti nel periodo che si intende elaborare.
        /// </summary>
        /// <param name="regs">Le registrazioni passate come parametro (devono necessariamente essere di un periodo continuo).</param>
        /// <param name="fromDate">La data di inzio del periodo di elaborazione.</param>
        /// <param name="toDate">La data di fine del periodo di elaborazione.</param>
        /// <param name="isToSaveChanges">se impostato a <c>true</c> indica di salvare i dati modificati a database.</param>
        /// <param name="isToAssociatePruFru">Se impostato a <c>true</c> indica che l'elaborate deve anche effettuare l'associazione delle unità fisse con le unità portatili.</param>
        /// <param name="elaborateUserId">L'identificativo dell'utente che ha lanciato l'operazione (dato utilizzato per la scrittura nella tabella messaggi).</param>
        /// <param name="elaborateDateTime">La data/ora di lancio dell'operazione che ha scatenato l'elaborate (dato utilizzato per la scrittura nella tabella messaggi).</param>
        /// <param name="application">L'applicazione di lancio dell'operazione di elaborate (dato utilizzato per la scrittura nella tabella messaggi).</param>
        /// <returns>La lista di errori riscontrati durante l'elaborazione delle timbrature.</returns>
        public List<KeyValuePair<string, string>> Elaborate(ICollection<Reg> regs, DateTime fromDate, DateTime toDate, Boolean isToSaveChanges = true,
            Boolean isToAssociatePruFru = false, int? elaborateUserId = null, DateTime? elaborateDateTime = null, ApplicationMessageEnum? application = null)
        {
           // RepoManager.Reg_VRepo.delete10mins();
            _log.Info(String.Format("Inizio elaborazione di {0} regs", regs.Count));

            // inizializzazione dei dati utilizzati per la scrittura nella tabella messaggi e recupero dell'applicazione attuale (da utilizzare in fase di scrittura tab messaggi)
            ApplicationMessageEnum currentApplication = InitializeElaborateMessagesParameters(elaborateUserId, elaborateDateTime, application);

            // inizializzazione dell'elenco di errori riscontrati durante l'elaborazione
            var errors = new List<KeyValuePair<string, string>>();

            #region 1. Rimozione delle registrazioni da non processare ed eventuale accoppiamento delle bloccate marcate

            // rimozione dalle registrazioni passate come parametro di tutte le reg non processabili
            // e accoppiamento delle bloccate con codice temporaneo
            _log.Info("Rimozione delle registrazioni da non processare ed eventuale accoppiamento delle bloccate marcate.");

            regs = RemoveAndProcessNonUsedInElaborateRegs(regs);


            #endregion

            #region 2. Esecuzione dei pre-processi custom provenienti da personalizzazione

            // una volta scremeto l'elenco di registrazioni che si desidera processare
            // si effettua un pre processo custom delle elaborazioni se richiesto da qualche personalizzazione applicativa

            _log.Info("Esecuzione dei pre - processi custom provenienti da personalizzazione.");

            errors.AddRange(ManagePreCustomElaborateRegs(regs, isToSaveChanges));

            #endregion

            #region 3. Cancellazione delle timbrature automatiche a chiusura causali e arrotondamenti per durata

            _log.Info("Cancellazione delle timbrature automatiche a chiusura causali e arrotondamenti per durata");

            // dalle registrazioni che si stanno processando si eliminano, se il modulo attività risulta abilitato,
            // tutte le timbrature generate automaticamente a chiusura delle attività
            regs = DeleteAllActivitiesAutoClosures(regs, isToSaveChanges);

            // dalle registrazioni che si stanno processando si eliminano gli arrotondamenti per durata
            RepoManager.Reg_VRepo.DeleteDurationRounding(regs);
            var tmpRegs = regs;
            //regs = regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.ArrotDur).ToList();
            regs = newAdjustFisRegByCol(regs.OrderBy(r => r.Registrazione_Data_Ora_Fis_Reg));
            //regs = tmpRegs;
            #endregion

            if (regs.Any())
            {

                #region 4. Associazione delle anagrafiche fisse e portatili alle registrazioni

                // se alla funzione di elaborate è richiesto di effettuare l'associazione delle unità portatili
                // e fisse, si procede all'operazione
                if (isToAssociatePruFru)
                {
                    _log.Info(String.Format("Inizio associazione anagrafiche di {0} regs.", regs.Count));
                    errors.AddRange(AssociatePruFru(regs));
                    _log.Info(String.Format("Associazione anagrafiche di {0} regs completata.", regs.Count));
                }

                #endregion

                #region 5. Esecuzione processi di elaborazione custom post associazione anagrafiche

                _log.Info("Esecuzione processi di elaborazione custom post associazione anagrafiche.");

                // esecuzione delle operazioni custom (provenienti da qualche personalizzazione) da eseguire dopo l'associazione pru/fru
                errors.AddRange(ManagePostPruFruCustomElaborateRegs(regs, isToSaveChanges));

                #endregion

                #region 6. Inserimento delle chiusure automatiche a chiusura causali

                _log.Info("Inserimento delle chiusure automatiche a chiusura causali");

                ManageActivitiesAutoClosures(ref regs, currentApplication, fromDate, toDate);

                #endregion

                #region 7. Disaccoppiamento registrazioni e definizione tipi di base

                // sono disaccoppiate le registrazioni da processare e sulle stesse è impostato il tipo base (attività/ore)
                _log.Info("Inizio disaccoppiamento registrazioni e definizione tipi di base.");

                DecoupleRegsAndSetType(regs, fromDate, toDate);

                _log.Info("Disaccoppiamento regs terminato.");


                #endregion

                #region 8. Abbinamento delle registrazioni di tipo e definizione dei passaggi

                // sono accoppiate tra di loro tutte le registrazioni ora da processare; inoltre sono gestiti
                // e preparati gli eventuli passaggi presenti
                _log.Info("Inizio accoppiamento registrazioni.");

                CoupleHourRegsAndManagePassages(regs, errors);

                _log.Info("Accoppiamento registrazioni terminato.");

                #endregion

                #region 9. Salvataggio dei dati modificati a database e cancellazione viaggi

                // si gestiscono eventuali errori di scrittura
                try
                {
                    // se è richiesto all'elaborate di salvare i dati processati
                    if (isToSaveChanges)
                    {
                        // si segnala l'inizio delle operazioni per l'eventuale rollback
                        BeginWork();

                        // aggiornamento delle registrazione e cancellazione dei viaggi
                        UpdateDataAndDeleteTrips(regs);


                        // al termine delle operazioni si effettua la commit del lavoro effettuato
                        CommitWork();
                    }
                }
                catch (Exception ex)
                {
                    ManageElaborateMessageDictionaries(100d, "Elaborazione interrotta per errori");
                    errors.Add(new KeyValuePair<String, String>(FunctionMessageEnum.Elaborate.ToString(), "RollbackWork"));

                    if (IsInTransaction)
                        RollbackWork();

                    var result = new Dictionary<String, String>();
                    var strError = String.Format("Errore:  {0},", ex.Message);
                    result.AddOrAppend(BusinessService.GetLocalizedString(PowerWebResources.STR_ERRORE), strError);

                    WriteCheckLog(new Reg_V(), result, Log);

                    throw ex;
                }

                #endregion

                #region 10. Raccolta delle reg_v su cui effettuare le post elaborazioni

                // recupero di tutte le reg_v da post processare rispetto alle registrazioni precedentemente processate
                IEnumerable<Reg_V> regVs = GetRegVsToPostProcess(regs);

                #endregion

                #region 11. Post processo delle reg_v

                // se sono state recuperate delle reg_v da processare
                if (regs.Any())
                {
                    try
                    {
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NotificaRitardo) == 1) {
                            List<Reg_V> delayReg = new List<Reg_V>();
                            var orariId = RepoManager.CantRepo.GetAllQueryable().GroupBy(c => c.Tab_Orari_Tipo_Id).ToList();
                            foreach (var orario in orariId) {
                                List<Tab_Orari> orari = RepoManager.Tab_OrariRepo.GetAllQueryable(t => t.Tab_Orari_Tipo_Id == orario.Key).OrderBy(t => t.Data_Inizio).ToList();
                                if (orari.Count() > 0) {
                                    if (orari.Last() != default(Tab_Orari))
                                    {
                                        String entrata = "";
                                        switch (DateTime.Today.DayOfWeek.ToString())
                                        {
                                            case "Monday":
                                                if (orari.Last().G1 != false)
                                                {
                                                    entrata = orari.Last().G1.ToString();
                                                }
                                                break;
                                            case "Tuesday":
                                                if (orari.Last().G2 != false)
                                                {
                                                    entrata = orari.Last().G2.ToString();
                                                }
                                                break;
                                            case "Wednesday":
                                                if (orari.Last().G3 != false)
                                                {
                                                    entrata = orari.Last().G3.ToString();
                                                }
                                                break;
                                            case "Thursday":
                                                if (orari.Last().G4 != false)
                                                {
                                                    entrata = orari.Last().G4.ToString();
                                                }
                                                break;
                                            case "Friday":
                                                if (orari.Last().G5 != false)
                                                {
                                                    entrata = orari.Last().G5.ToString();
                                                }
                                                break;
                                            case "Saturday":
                                                if (orari.Last().G6 != false)
                                                {
                                                    entrata = orari.Last().G6.ToString();
                                                }
                                                break;
                                            case "Sunday":
                                                if (orari.Last().G7 != false)
                                                {
                                                    entrata = orari.Last().G7.ToString();
                                                }
                                                break;
                                        }
                                        if (entrata != "")
                                        {
                                            List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == orario.Key).ToList();
                                            if (cants.Count() > 0) {
                                                var newRounding = new Reg_V();
                                                newRounding.Cant_Id = cants.First().Cant_Id;
                                                delayReg.Add(newRounding);
                                            }
                                        }
                                    }
                                }
                               
                            }
                            regVs = GetRegVsForRounding(regs);

                            BeginWork();

                            HashSet<int> regColIds = new HashSet<int>();

                            regVs.Where(r => r.Col_Id != null).Select(regV => regV.Col_Id).Cast<int>().ToList().ForEach(colId =>
                            {
                                regColIds.Add(colId);
                            });

                            HashSet<int> regCantIds = new HashSet<int>();

                            regVs.Where(r => r.Cant_Id != null).Select(regV => regV.Cant_Id).Cast<int>().ToList().ForEach(colId =>
                            {
                                regCantIds.Add(colId);
                            });

                            //IEnumerable<int> regColIds = new HashSet<int>(regVs.Where(r => r.Col_Id != null).Select(regV => regV.Col_Id).Distinct().Cast<int>().ToArray());
                            //IEnumerable<int> regCantIds = regVs.Where(r => r.Cant_Id != null).Select(regV => regV.Cant_Id).Distinct().Cast<int>().ToList();

                            // dagli id dei collaboratori e dei cantieri precedentemente recuperati si recuperano le anagrafiche

                            _log.Info("Accesso a database per la raccolta di cantieri e collaboratori");

                            List<Col> regCols = RepoManager.ColRepo.Find(col => regColIds.Contains(col.Col_Id), true).ToList();
                            List<Cant> regCants = RepoManager.CantRepo.Find(cant => regCantIds.Contains(cant.Cant_Id), true).ToList();


                            // recupera il metodo di arrotondamento impostato nei parametri
                            RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;

                            // applicazione degli arrotondamenti per inizio-fine
                            _log.Info(String.Format("Inizio Controllo Ritardo di {0} regV", regVs.Count()));
                            errors.AddRange(RepoManager.Reg_VRepo.CheckDeelay(regVs, regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att).ToList()
                                , regCants, regCols, _elaborateUserId, _elaborateDateTime, currentApplication));

                            CommitWork();
                        }

                        #region 11.1 Gestione degli arrotondamenti per inizio/fine

                        // si gestiscono gli arrotondamenti solamente se c'è lo specifico flag abilitato nella param
                        if (RepoManager.ParamRepo.ParametersRow.Abilita_Arrotondamenti)
                        {
                             regVs = GetRegVsForRounding(regs);

                            BeginWork();

                            // calcolo gli id dei collaboratori e dei cantieri collegati alle registrazioni correnti

                            HashSet<int> regColIds = new HashSet<int>();

                            regVs.Where(r => r.Col_Id != null).Select(regV => regV.Col_Id).Cast<int>().ToList().ForEach(colId =>
                            {
                                regColIds.Add(colId);
                            });

                            HashSet<int> regCantIds = new HashSet<int>();

                            regVs.Where(r => r.Cant_Id != null).Select(regV => regV.Cant_Id).Cast<int>().ToList().ForEach(colId =>
                            {
                                regCantIds.Add(colId);
                            });

                            //IEnumerable<int> regColIds = new HashSet<int>(regVs.Where(r => r.Col_Id != null).Select(regV => regV.Col_Id).Distinct().Cast<int>().ToArray());
                            //IEnumerable<int> regCantIds = regVs.Where(r => r.Cant_Id != null).Select(regV => regV.Cant_Id).Distinct().Cast<int>().ToList();

                            // dagli id dei collaboratori e dei cantieri precedentemente recuperati si recuperano le anagrafiche

                            _log.Info("Accesso a database per la raccolta di cantieri e collaboratori");

                            List<Col> regCols = RepoManager.ColRepo.Find(col => regColIds.Contains(col.Col_Id), true).ToList();
                            List<Cant> regCants = RepoManager.CantRepo.Find(cant => regCantIds.Contains(cant.Cant_Id), true).ToList();


                            // recupera il metodo di arrotondamento impostato nei parametri
                            RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;

                            // se sono impostati gli arrotondamenti per inizio-fine
                            if (roundingParamEnum == RoundingMethodEnum.StartEnd || roundingParamEnum == RoundingMethodEnum.None || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.UseEUDurationRounding) == 1)
                            {
                                // applicazione degli arrotondamenti per inizio-fine
                                _log.Info(String.Format("Inizio arrotondamento di {0} regV", regVs.Count()));
                                errors.AddRange(RepoManager.Reg_VRepo.Rounding(regVs, regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att).ToList()
                                    , regCants, regCols, _elaborateUserId, _elaborateDateTime, currentApplication,false));
                                _log.Info(String.Format("Arrotondamento di {0} regs terminato", regVs.Count()));
                            } else if (roundingParamEnum == RoundingMethodEnum.Disabled) {
                                //se non servono gli arrotondamenti imposto i parametri a zero così da toglierli
                                RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento = 0;
                                RepoManager.ParamRepo.ParametersRow.Utilizzo_Limite_Entrata = 0;
                                RepoManager.ParamRepo.ParametersRow.Utilizzo_Limite_Uscita = 0;
                                RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Inizio_Pomeriggio = TimeSpan.MinValue;
                                RepoManager.ParamRepo.ParametersRow.Ritardo_Tolleranza_Minuti = 0;
                                RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Entrata = TimeSpan.MinValue;
                                _log.Info(String.Format("Tolgo gli arrotondamenti a {0} regV", regVs.Count()));
                                errors.AddRange(RepoManager.Reg_VRepo.Rounding(regVs, regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att).ToList()
                                    , regCants, regCols, _elaborateUserId, _elaborateDateTime, currentApplication,false));

                                _log.Info(String.Format("Arrotondamento tolti per {0} regs", regVs.Count()));
                            }

                            CommitWork();
                            //RepoManager.Reg_VRepo.InviaRitardi();
                            //RepoManager.Reg_VRepo.InviaRitardi();
                        }

                        regVs = GetRegVsToPostProcess(regs);

                        #endregion

                        #region 11.2 Gestione delle sovrapposizioni

                        BeginWork();

                        // verifica delle sovrapposizioni sulle reg_v
                        _log.Info(String.Format("Verifica sovrapposizioni regV", regVs.Count()));

                        List<KeyValuePair<string, string>> overlapErrors = RepoManager.Reg_VRepo.CheckOverlaps(regVs);

                        // se sono state trovate delle registrazioni in sovrapposizione
                        if (overlapErrors.Any() && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.RimozionePausaHotel) == 0)
                        {
                            HashSet<int> regColIds = new HashSet<int>();

                            regVs.Where(r => r.Col_Id != null).Select(regV => regV.Col_Id).Cast<int>().ToList().ForEach(colId =>
                            {
                                regColIds.Add(colId);
                            });
                            List<Col> regCols = RepoManager.ColRepo.Find(col => regColIds.Contains(col.Col_Id), true).ToList();
                            errors.AddRange(RepoManager.Reg_VRepo.AdjustOverlappedRegs(regVs, regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att).ToList()
                                    , regCols, _elaborateUserId, _elaborateDateTime, currentApplication, false));
                            // si aggiunge l'errore all'elenco degli errori da ritornare
                            errors.AddRange(overlapErrors);

                            // ... e per tutte le registrazioni abbinate trovate si procede ad impostare lo stato di errore per sovrapposizione

                            // recupero di tutti gli id registrazione in sovrapposizione
                            IEnumerable<int> overlappedRegsId = overlapErrors.Select(kvp =>
                            {
                                int regId = 0;
                                Int32.TryParse(kvp.Value.Substring(kvp.Value.LastIndexOf(": ") + 1), out regId);
                                return regId;
                            }).ToList();

                            // per ogni registrazione in sovrapposizione con un id comprensibile nel messaggio
                            var overlappedRegsToUpdate = new List<Reg>();
                            foreach (int regId in overlappedRegsId.Where(regId => regId != 0).ToList())
                            {
                                // la si recupera da database e se trovata si procede all'impostazione del suo stato
                                // come errore in sovrapposizione (aggiungedola all'elenco delle registrazioni da processare)
                                Reg overlappedReg = FirstOrDefault(reg => reg.Reg_Id == regId);
                                if (overlappedReg != default(Reg))
                                    if (overlappedReg.Registrazione_Stato_Reg == (int)RegStateEnum.Ass)
                                    {
                                        overlappedReg.Registrazione_Stato_Reg = (int)RegStateEnum.Overlap;
                                        overlappedRegsToUpdate.Add(overlappedReg);
                                    }
                            }

                            // se sono rimaste delle registrzioni da modificare allora si aggiornano a database
                            if (overlappedRegsToUpdate.Any())
                                Context.BulkUpdate(overlappedRegsToUpdate);
                        }


                        _log.Info(String.Format("Verifica sovrapposizioni di {0} regV completata", regVs.Count()));

                        CommitWork();

                        #endregion

                        IEnumerable<Reg_V> tripRegVs = GetRegVsToPostProcess(regs);

                        // se è richiesto anche il salvataggio delle modifiche
                        if (isToSaveChanges)
                        {

                            #region 11.3 Abbinamento delle attività

                            BeginWork();

                            // effettuazione dell'abbinamento delle attività
                            _log.Info(String.Format("Inizio abbinamento di {0} regV con relative attività", regVs.Count()));

                            errors.AddRange(RepoManager.Reg_VRepo.ElaborateActivities(regVs));

                            _log.Info("Attività elaborate correttamente");

                            CommitWork();

                            #endregion

                            #region 11.4 Gestione dei viaggi

                            // se il calcolo dei viaggi è configurato
                            if (RepoManager.ParamRepo.ParametersRow.Flag_Ore_Viaggi != (int)FlagTripHoursParamEnum.None && RepoManager.ParamRepo.ParametersRow.Flag_Calcolo_Viaggi)
                            {
                                // sono rilette le registrazioni per interecettare eventuali modifiche di arrotondamento 
                                // o altro interevenute precedentemente
                                regVs = GetRegVsToPostProcess(regs);
                                _log.Info("Registrazioni raccolte " + regVs.Count());
                                tripRegVs = regVs;
                                //se ci sono delle registrazioni lette da database si richiama l'elaborazione dei viaggi
                                if (regVs.Any())
                                {
                                    _log.Info("Inizio elaborazione viaggi");

                                    errors.AddRange(RepoManager.Reg_VRepo.ElaborateTrips(regVs));

                                    _log.Info("Elaborazione viaggi terminata");
                                }
                            }

                            #endregion

                        }

                        #region 11.5 Gestione degli arrotondamenti per durata

                        // si gestiscono gli arrotondamenti solamente se c'è lo specifico flag abilitato nella param
                        if (RepoManager.ParamRepo.ParametersRow.Abilita_Arrotondamenti)
                        {

                            // recupera il metodo di arrotondamento impostato nei parametri
                            RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;

                            // se sono impostati gli arrotondamenti per durata
                            if (roundingParamEnum == RoundingMethodEnum.Duration)
                            {
                                var roundingRegVs = regVs.Where(r => r.Registrazione_Tipo_Reg == 0).ToList();
                                // Recupera i viaggi appena creati  
                                var tripsRegvs = RepoManager.Reg_VRepo.Find(regv => regv.Data_Ora_Fis_E >= fromDate && regv.Data_Ora_Fis_U <= toDate &&
                                                    regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip);

                                //roundingRegVs.AddRange(tripsRegvs);

                                // applicazione degli arrotondamenti per durata
                                _log.Info(String.Format("starting rounding duration regVs at {0}", regVs.Count()));
                                errors.AddRange(RepoManager.Reg_VRepo.DurationRounding(roundingRegVs, roundingParamEnum));
                                _log.Info(String.Format("finished rounding duration regVs at {0}", regVs.Count()));
                            }
                        }
                        #endregion

                        #region 11.6 Creazione pausa per Hotel

                        //in caso sia abilitata la personalizzazione vado a creare per i cantieri con il parametro inserito una timbratura di durata negativa
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.RimozionePausaHotel) == 1 && currentApplication == ApplicationMessageEnum.Elaborate) {
                            //regs.AddRange(tmpRegs.Where(r => r.Registrazione_Tipo_Reg != 0));
                            //regs = DeleteCopertureSerali(regs, isToSaveChanges);

                            // dalle registrazioni che si stanno processando si eliminano gli arrotondamenti per durata
                            RepoManager.Reg_VRepo.DeletePausaPranzo(tmpRegs);
                            //regs = regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.ArrotDur).ToList();
                            var roundingRegVs1 = regVs.ToList();
                            // Recupera i viaggi appena creati  
                            var tripsRegvs1 = RepoManager.Reg_VRepo.Find(regv => regv.Data_Ora_Fis_E >= fromDate && regv.Data_Ora_Fis_U <= toDate &&
                                                regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip);

                            //roundingRegVs1.AddRange(tripsRegvs1);
                            errors.AddRange(RepoManager.Reg_VRepo.PausaPranzoKomplett(roundingRegVs1));
                        }
                        #endregion

                        #region 11.7 Creazione pausa per Cantiere

                        //in caso sia abilitata la personalizzazione vado a creare per i cantieri con il parametro inserito una timbratura di durata negativa
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.RimozionePausaPranzo) == 1)
                        {
                            regs = DeleteCopertureSerali(regs, isToSaveChanges);

                            // dalle registrazioni che si stanno processando si eliminano gli arrotondamenti per durata
                            RepoManager.Reg_VRepo.DeletePausaPranzo(tmpRegs);
                            regs = regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.ArrotDur).ToList();
                            var roundingRegVs1 = regVs.ToList();
                            // Recupera i viaggi appena creati  
                            var tripsRegvs1 = RepoManager.Reg_VRepo.Find(regv => regv.Data_Ora_Fis_E >= fromDate && regv.Data_Ora_Fis_U <= toDate &&
                                                regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip);

                            roundingRegVs1.AddRange(tripsRegvs1);
                            errors.AddRange(RepoManager.Reg_VRepo.PausaPranzo(roundingRegVs1));
                        }
                        #endregion

                        #region 11.8 Rigenerazione viaggi in caso di modifica komplett

                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.RimozionePausaHotel) == 1 && currentApplication == ApplicationMessageEnum.Elaborate)
                        {
                            var allRegs = RepoManager.RegRepo.Find(reg => reg.Registrazione_Data_Ora_Fig_Reg >= fromDate && reg.Registrazione_Data_Ora_Fig_Reg <= toDate).ToList();

                            errors.AddRange(AssociatePruFru(allRegs));
                            // si gestiscono eventuali errori di scrittura
                            try
                            {
                                // se è richiesto all'elaborate di salvare i dati processati
                                if (isToSaveChanges)
                                {
                                    // si segnala l'inizio delle operazioni per l'eventuale rollback
                                    BeginWork();

                                    // aggiornamento delle registrazione e cancellazione dei viaggi
                                    UpdateDataAndDeleteTrips(allRegs);

                                    // sono rilette le registrazioni per interecettare eventuali modifiche di arrotondamento 
                                    // o altro interevenute precedentemente
                                    regVs = GetRegVsToPostProcess(regs);
                                    //se ci sono delle registrazioni lette da database si richiama l'elaborazione dei viaggi
                                    if (regVs.Any())
                                    {
                                        _log.Info("Registrazioni raccolte " + regVs.Count());

                                        _log.Info("Inizio elaborazione viaggi");

                                        errors.AddRange(RepoManager.Reg_VRepo.ElaborateTrips(regVs));

                                        _log.Info("Elaborazione viaggi terminata");
                                    }


                                    // al termine delle operazioni si effettua la commit del lavoro effettuato
                                    CommitWork();
                                }
                            }
                            catch (Exception ex)
                            {
                                ManageElaborateMessageDictionaries(100d, "Elaborazione interrotta per errori");
                                errors.Add(new KeyValuePair<String, String>(FunctionMessageEnum.Elaborate.ToString(), "RollbackWork"));

                                if (IsInTransaction)
                                    RollbackWork();

                                var result = new Dictionary<String, String>();
                                var strError = String.Format("Errore:  {0},", ex.Message);
                                result.AddOrAppend(BusinessService.GetLocalizedString(PowerWebResources.STR_ERRORE), strError);

                                WriteCheckLog(new Reg_V(), result, Log);

                                throw ex;
                            }
                        }

                        #endregion

                        #region (Personalizzazione) Coperture Serali
                        // se la personalizzazione è attiva controllo se le timbrature
                        // sono coperture serali
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CopertureSerali) == 1)
                        {
                            regVs = GetRegVsForRounding(regs);

                            BeginWork();

                            // calcolo gli id dei collaboratori e dei cantieri collegati alle registrazioni correnti

                            HashSet<int> regColIds = new HashSet<int>();

                            regVs.Where(r => r.Col_Id != null).Select(regV => regV.Col_Id).Cast<int>().ToList().ForEach(colId =>
                            {
                                regColIds.Add(colId);
                            });

                            HashSet<int> regCantIds = new HashSet<int>();

                            regVs.Where(r => r.Cant_Id != null).Select(regV => regV.Cant_Id).Cast<int>().ToList().ForEach(colId =>
                            {
                                regCantIds.Add(colId);
                            });

                            // dagli id dei collaboratori e dei cantieri precedentemente recuperati si recuperano le anagrafiche

                            _log.Info("Accesso a database per la raccolta di cantieri e collaboratori");

                            List<Col> regCols = RepoManager.ColRepo.Find(col => regColIds.Contains(col.Col_Id), true).ToList();
                            List<Cant> regCants = RepoManager.CantRepo.Find(cant => regCantIds.Contains(cant.Cant_Id), true).ToList();


                            // recupera il metodo di arrotondamento impostato nei parametri
                            RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;

                            // Controllo coperture serali
                            _log.Info(String.Format("Inizio controllo coperture serali di {0} regV", regVs.Count()));
                            errors.AddRange(RepoManager.Reg_VRepo.CopertureSerali(regVs, regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att).ToList()
                                , regCants, regCols, _elaborateUserId, _elaborateDateTime, currentApplication, false));

                            // se sono impostati gli arrotondamenti per inizio-fine
                            //if (roundingParamEnum == RoundingMethodEnum.StartEnd || roundingParamEnum == RoundingMethodEnum.None)
                            //{
                            //    // applicazione degli arrotondamenti per inizio-fine
                            //    _log.Info(String.Format("Inizio arrotondamento di {0} regV", regVs.Count()));
                            //    RepoManager.Reg_VRepo.DeleteDurationRounding(regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att).ToList());
                            //    errors.AddRange(RepoManager.Reg_VRepo.Rounding(regVs, regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att).ToList()
                            //        , regCants, regCols, _elaborateUserId, _elaborateDateTime, currentApplication, false));
                            //    _log.Info(String.Format("Arrotondamento di {0} regs terminato", regVs.Count()));
                            //}
                            //else if (roundingParamEnum == RoundingMethodEnum.Disabled)
                            //{
                            //    //se non servono gli arrotondamenti imposto i parametri a zero così da toglierli
                            //    RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento = 0;
                            //    RepoManager.ParamRepo.ParametersRow.Utilizzo_Limite_Entrata = 0;
                            //    RepoManager.ParamRepo.ParametersRow.Utilizzo_Limite_Uscita = 0;
                            //    RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Inizio_Pomeriggio = TimeSpan.MinValue;
                            //    RepoManager.ParamRepo.ParametersRow.Ritardo_Tolleranza_Minuti = 0;
                            //    RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Entrata = TimeSpan.MinValue;
                            //    _log.Info(String.Format("Tolgo gli arrotondamenti a {0} regV", regVs.Count()));
                            //    errors.AddRange(RepoManager.Reg_VRepo.Rounding(regVs, regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att).ToList()
                            //        , regCants, regCols, _elaborateUserId, _elaborateDateTime, currentApplication, false));
                            //
                            //    _log.Info(String.Format("Arrotondamento tolti per {0} regs", regVs.Count()));
                            //}


                            //se cìè la modifica delle coperture serali elaboro le attività per mostrarle nella manutenzione timbrature
                            errors.AddRange(RepoManager.Reg_VRepo.ElaborateActivities(regVs));

                            _log.Info(String.Format("Controllo coperture serali di {0} regs terminato", regVs.Count()));

                            CommitWork();
                            //RepoManager.Reg_VRepo.InviaRitardi();
                        }
                        #endregion

                    }
                    catch (Exception ex)
                    {
                        //_//Client.sendMessage(JsonConvert.SerializeObject(CommonService.signalRMessage(DateTime.Now, "ERROR", "Elaborazione interrotta durante la fase di post-processing")));
                        ManageElaborateMessageDictionaries(100d, "Elaborazione interrotta per errori"); 
                        errors.Add(new KeyValuePair<String, String>(FunctionMessageEnum.Elaborate.ToString(), "RollbackWork"));

                        if (IsInTransaction)
                            RollbackWork();

                        var result = new Dictionary<String, String>();
                        var strError = String.Format("Errore:  {0},", ex.Message);
                        result.AddOrAppend(BusinessService.GetLocalizedString(PowerWebResources.STR_ERRORE), strError);

                        WriteCheckLog(new Reg_V(), result, Log);

                        throw ex;
                    }

                    //_//Client.updatePartialPhase("Post-elaborazione registrazioni terminata");
                }

                #endregion

                #region 12. Cancellazione delle causali tappo marcate per la chiusura

                //ManageElaborateMessageDictionaries(91.63d, "Inizio cancellazione attività tappo marcate per l'eliminazione");
                DeleteAllActivitiesMarkedForDeletion();
                //ManageElaborateMessageDictionaries(91.63d, "Termine cancellazione attività tappo marcate per l'eliminazione");

                #endregion

                #region 13. Elaborazione attività per app

                errors.AddRange(ManagePostElaborateRegs(regs, isToSaveChanges));

                #endregion

            }

            // se è richiesto di salvare le modifiche allora si inseriscono i messaggi di elaborazione con gli errori riscontrati

            if (isToSaveChanges && errors.Count != 0)
            {
                //ManageElaborateMessageDictionaries(95.87d, "Scrittura messagi elaborazione in apposita tabella");
                RepoManager.Tab_MessaggiRepo.InsertMessages(errors, currentApplication, FunctionMessageEnum.Elaborate, _elaborateUserId, _elaborateDateTime);
            }


            // si segnala il termine dell'operazione a video
            ManageElaborateMessageDictionaries(100d, "Elaborazione terminata");

            // l'elaborate ritorna gli errori riscontrati durante l'elaborazione
            return errors;
        }

        /// <summary>
        /// Recupera tutte le reg_v su cui effettuare i post processi a partire dalle registrazioni specifiche.
        /// </summary>
        /// <param name="regs">Le registrazioni specifiche da post processare.</param>
        /// <returns>L'elenco delle reg_v su cui effettuare i post processi.</returns>
        private IEnumerable<Reg_V> GetRegVsToPostProcess(IEnumerable<Reg> regs)
        {
            // inizializzazione dell'elenco di reg_v ritorno del metodo
            List<Reg_V> regVs = new List<Reg_V>();

            // si procede all'elaborazione solamente se ci sono delle registrazioni passate come parametro
            if (regs.Any())
            {
                _log.Info("Inizio raccolta regV per post-elaborazione");

                // recupero di tutti gli id collaboratori presenti nelle registrazioni passate come parametro
                HashSet<int> colIds = new HashSet<int>(regs.Where(reg => reg.Col_Id != null).Select(reg => reg.Col_Id).Distinct().Cast<int>().ToList());

                // calcolo della data registrazione minima e massima presente nell'elenco (aggiungendo alla data di destinazione
                // un giorno per poter recuperare i dati dalla mezzanotte del giorno successivo indietro
                DateTime from = regs.Min(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date;
                DateTime to = regs.Max(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date.AddDays(1);

                // leggo le reg_v che rientrano nei giorni richiesti, associati e con il collaboratore presente nelle registrazioni
                // passate come paremtro e non bloccate (ottimizzando la ricerca per un solo collaboratore e per più di un
                // collaboratore) e non attività

                if (colIds.Count() > 0)
                    regVs = RepoManager.Reg_VRepo.Find(regv => regv.Data_Ora_Fis_E >= from && (regv.Data_Ora_Fis_U <= to || (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass && regv.Data_Ora_Fis_E <= to)) &&
                          regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass && colIds.Contains((int)regv.Col_Id) &&
                         !regv.Registrazione_Bloccata && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att, true).ToList();
                
                else
                {
                    int colId = 0;
                    regVs = RepoManager.Reg_VRepo.Find(regv => regv.Data_Ora_Fis_E >= from && (regv.Data_Ora_Fis_U <= to || (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass && regv.Data_Ora_Fis_E <= to)) &&
                        regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass && regv.Col_Id == colId &&
                        !regv.Registrazione_Bloccata && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att, true).ToList();
                }


            }

            _log.Info("Raccolta regV terminata");

            // si ritornano le reg_v così calcolate
            return regVs;
        }


        /// <summary>
        /// Recupera tutte le reg_v su cui effettuare i post processi a partire dalle registrazioni specifiche.
        /// </summary>
        /// <param name="regs">Le registrazioni specifiche da post processare.</param>
        /// <returns>L'elenco delle reg_v su cui effettuare i post processi.</returns>
        private IEnumerable<Reg_V> GetRegVsForRounding(IEnumerable<Reg> regs)
        {
            // inizializzazione dell'elenco di reg_v ritorno del metodo
            IEnumerable<Reg_V> regVs = Enumerable.Empty<Reg_V>();

            // si procede all'elaborazione solamente se ci sono delle registrazioni passate come parametro
            if (regs.Any())
            {
                _log.Info("Inizio raccolta regVs per arrotondamenti");

                // recupero di tutti gli id collaboratori presenti nelle registrazioni passate come parametro
                IEnumerable<int?> colIds = regs.Select(reg => reg.Col_Id).Distinct().ToList();

                // calcolo della data registrazione minima e massima presente nell'elenco (aggiungendo alla data di destinazione
                // un giorno per poter recuperare i dati dalla mezzanotte del giorno successivo indietro
                DateTime from = regs.Min(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date;
                DateTime to = regs.Max(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date.AddDays(1);



                // leggo le reg_v che rientrano nei giorni richiesti, associati e con il collaboratore presente nelle registrazioni
                // passate come paremtro e non bloccate (ottimizzando la ricerca per un solo collaboratore e per più di un
                // collaboratore) e non attività
                if (colIds.Count() > 1)   
                    regVs = RepoManager.Reg_VRepo.Find(regv => regv.Data_Ora_Fis_E >= from && (regv.Data_Ora_Fis_U <= to || (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass && regv.Data_Ora_Fis_E <= to) || (regv.Data_Ora_Fis_U == null && regv.Registrazione_Stato_Reg == (int)RegStateEnum.None && regv.Data_Ora_Fis_E < to)) &&
                          colIds.Contains(regv.Col_Id) &&
                          !regv.Registrazione_Bloccata && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att, true).ToList();
                else
                {
                    int colId = colIds.FirstOrDefault() ?? 0;
                    regVs = RepoManager.Reg_VRepo.Find(regv => regv.Data_Ora_Fis_E >= from && (regv.Data_Ora_Fis_U <= to || (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass && regv.Data_Ora_Fis_E <= to) || (regv.Data_Ora_Fis_U == null && regv.Registrazione_Stato_Reg == (int)RegStateEnum.None && regv.Data_Ora_Fis_E < to)) &&
                        regv.Col_Id == colId &&
                        !regv.Registrazione_Bloccata && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att, true).ToList();
                }

                _log.Info(string.Format("Raccolta di {0} regs per arrotondamenti terminata", regVs.Count()));
            }

            // si ritornano le reg_v così calcolate
            return regVs;
        }

        /// <summary>
        /// Aggiorna a database le registraioni passate come parametro cancellato, in caso di abilitazione viaggi, i viaggi in esse contenuti.
        /// </summary>
        /// <param name="regs">Le registrazioni da aggiornare e di cui cancellare i viaggi.</param>
        private void UpdateDataAndDeleteTrips(ICollection<Reg> regs)
        {

            #region Cancellazione dei viaggi

            // sono cancellati i viaggi solamente se il flag di generazione automaticae ed il modulo dei viaggi sono attivi
            if (/*RepoManager.ParamRepo.ParametersRow.Flag_Calcolo_Viaggi &&*/ RepoManager.ParamRepo.ParametersRow.Abilita_Viaggi)
            {
                // se ci sono dei viaggi da cancellare, procedo alla loro eliminazione
                IEnumerable<Reg> tripsToDelete = regs.Where(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip).ToList();
                if (tripsToDelete.Any())
                {
                    _log.Info("Inizio cancellazione viaggi.");

                    RepoManager.RegRepo.Context.BulkDelete(tripsToDelete);

                    BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(20, String.Format("Cancellazione dei {0} viaggi terminata", tripsToDelete.Count()));

                    _log.Info(String.Format("Eliminati {0} viaggi", tripsToDelete.Count()));
                }
            }

            #endregion

            #region Salvataggio registrazioni aggiornate

            // se ci sono delle registrazioni da salvare, le salvo
            IEnumerable<Reg> regsToUpdate = regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip).ToList();
            if (regsToUpdate.Any())
            {
                _log.Info(String.Format("Inizio bulk update di {0} regs", regs.Count));

                BulkUpdate(regsToUpdate);

                _log.Info(String.Format("Terminato update di {0} regs", regsToUpdate.Count()));

            }

            #endregion

        }

        private void UpdateData(ICollection<Reg> regs)
        {
            #region Salvataggio registrazioni aggiornate

            // se ci sono delle registrazioni da salvare, le salvo
            IEnumerable<Reg> regsToUpdate = regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip && reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Duration).ToList();
            if (regsToUpdate.Any())
            {
                _log.Info(String.Format("Inizio Update post generazione pause", regs.Count));

                BulkUpdate(regsToUpdate);

                _log.Info(String.Format("Terminato update"));

            }

            #endregion

        }

        /// <summary>
        /// Accoppia le registrazioni di tipo ora specificate gestendo anche i passaggi presenti.
        /// </summary>
        /// <param name="regs">L'elenco delle registrazioni da processare.</param>
        /// <param name="processErrors">Gli errori riscontrati durante la fase di processo.</param>
        private void CoupleHourRegsAndManagePassages(IEnumerable<Reg> regs, List<KeyValuePair<string, string>> processErrors)
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
            TimeSpan minElapsedParam = RepoManager.ParamRepo.ParametersRow.Default_Durata_Min_Ril ?? TimeSpan.Zero; // durata minima della registrazione

            // si processano solamente le registrazioni abbinabili (cioè non attività, non viaggi, con collaboratore e cantiere)
            IEnumerable<Reg> regsToCouple = regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att && reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip
                && reg.Cant_Id.HasValue && reg.Col_Id.HasValue).ToList();

            // calcolo gli id dei collaboratori e dei cantieri collegati alle registrazioni correnti
            IEnumerable<int?> regColIds = regsToCouple.Select(reg => reg.Col_Id).Distinct().ToList();
            IEnumerable<int?> regCantIds = regsToCouple.Select(reg => reg.Cant_Id).Distinct().ToList();

            // dagli id dei collaboratori e dei cantieri precedentemente recuperati si recuperano le anagrafiche
            List<Col> regCols = RepoManager.ColRepo.Find(col => regColIds.Contains(col.Col_Id), true).ToList();
            List<Cant> regCants = RepoManager.CantRepo.Find(cant => regCantIds.Contains(cant.Cant_Id), true).ToList();

            // le registrazioni sono processate per collaboratore
            IEnumerable<IGrouping<int?, Reg>> regsGroupedByCol = regsToCouple.GroupBy(reg => reg.Col_Id).ToList();

            // inzializzazione dei dati utilizzati per i messaggi di interfaccia grafica


            // si cicla su tutte le registrazioni da accoppiare raggruppate per collaboratore
            foreach (IGrouping<int?, Reg> colGroup in regsGroupedByCol)
            {

                // si recupera il collaboratore collegato al gruppo in processo
                int colId = colGroup.Key ?? 0;
                Col currentCol = regCols.FirstOrDefault(col => col.Col_Id == colId);

                // se non è stato trovato il collaboratore allora si passa al processo del gruppo successivo
                if (currentCol == default(Col))
                    continue;

                // si ordinano le registrazioni del gruppo per motivazione e data/ora
                // di modo da abbinare le timbrature coerentemente con la motivazione inserita
                //List<Reg> orderedColRegs = colGroup.OrderBy(reg => reg.Motivazione_Reg_Id).ThenBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();
                List<Reg> orderedColRegs = colGroup.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();
                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.OrderElaborateRegByCant) == 1)
                {
                    orderedColRegs = orderedColRegs.OrderBy(reg => reg.Cant_Id).ToList();
                }


                // inizializzazione della variabile utilizzata per salvare la timbratura precedente rispetto a quella
                // correntemente processata dal ciclo
                Reg lastOpen = null;

                #region Gestione passaggi ed abbinamento delle registrazioni per collaboratore


                // ciclo di elaborazione di tutte le registrazione del collaboratore ordinate per motivazione e data ora
                foreach (Reg currentReg in orderedColRegs)
                {

                    // recupero il cantiere relativo alla registrazione in elaborazione
                    int currentCantId = currentReg.Cant_Id ?? 0;
                    Cant currentCant = regCants.FirstOrDefault(cant => cant.Cant_Id == currentCantId);

                    // se il cantiere non risulta presente allora si passa al processo della registrazione successiva
                    if (currentCant == default(Cant))
                        continue;

                    // inizializzazione della variabile che segnala se la registraizione corrente risulta da processare
                    bool isToElaborate = true;

                    #region Gestione dei passaggi

                    // se il modulo dei passaggi risulta abilitato e la registrazione corrente non è marcata come attività
                    if (passModuleActive && currentReg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att)
                    {
                        // se il cantiere o il collaboratore è configurato per la gestione dei passaggi
                        // allora si marca la registrazione corrente come un passaggio abbinato 
                        // e si marca la registrazione corrente come da non elaborare;
                        // altrimenti se procede normalmente
                        if (currentCol.Singola_Reg || currentCant.Singola_Reg)
                        {
                            currentReg.Registrazione_Tipo_Reg = (int)RegTypeEnum.Pass;
                            currentReg.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                            isToElaborate = false;
                        }

                    }

                    #endregion

                    #region Verifica conformità registrazione per elaborazione

                    // inizializzazione del tipo di notturno utilizzato in fase di abbinamento (di default non impostato)
                    NocturneTypeEnum nocturneType = NocturneTypeEnum.None;

                    // inizializzazione della soglia (nuova mezzanotte) del notturno utilizzato in fase di abbinamento (di default a mezzanotte)
                    TimeSpan nocturneThreshold = TimeSpan.Zero;

                    //inizilizzazione della durata del notturno
                    TimeSpan nocturneDuration = TimeSpan.Zero;

                    // inizializzazione dei valori di durata massima e minima della registrazione
                    TimeSpan maxElapsed = TimeSpan.Zero;
                    TimeSpan minElapsed = TimeSpan.Zero;

                    // si procede alla verifica della confromità per l'elaborazione e il calcolo dei parametri solamente se precedentemente
                    // il dato non è stati marcato come passaggio
                    if (isToElaborate)
                    {

                        #region Calcolo dei parametri del notturno attualizzati su anagrafiche registrazione

                        // se è attivo il modulo del notturno
                        if (nocturneModuleActive)
                        {
                            // sono per prima cosa impostate le configurazioni del collaboratore;
                            // se il collaboratore non è stato configurato si scala sul cantiere, recuperando le sue configurazioni;
                            // in caso anche il cantiere non sia stato configurato si scala sui parametri generati, recuperando quelle configurazioni

                            // impostazione della configurazione del collaboratore
                            nocturneType = currentCol.NocturneTypeEnum;

                            //viene estratta la durata del notturno e la nuova mezzanotte
                            nocturneDuration = currentCol.Durata_Notturno_Col ?? TimeSpan.Zero;
                            nocturneThreshold = currentCol.Durata_Max_Gruppo_Notte_Ril_Col ?? TimeSpan.Zero;

                            // se il collaboratore non risulta configurato allora si procede al recupero delle configurazioni del cantiere
                            if (nocturneType == NocturneTypeEnum.None || nocturneType == NocturneTypeEnum.Disabled)
                            {
                                //vengono estratti i praemtri dal cantiere
                                nocturneType = currentCant.NocturneTypeEnum;
                                nocturneDuration = currentCant.Durata_Notturno_Can ?? TimeSpan.Zero;
                                nocturneThreshold = currentCant.Durata_Max_Gruppo_Notte_Ril_Can ?? TimeSpan.Zero;
                            }

                            // se il collaboratore e il cantiere non risultano configurati allora si procede al recupero delle configurazioni generali nei parametri
                            // (si controlla solamente il valore "None" perché se disabilitato su cantiere e collaboratore allora non lo si processa)
                            if (nocturneType == NocturneTypeEnum.None)
                            {
                                //vengono estratti i parametri del notturno dalla scheda parametri
                                nocturneType = nocturneTypeParam;
                                nocturneDuration = nocturneDurationParamTs;
                                nocturneThreshold = nocturneParamTs;
                            }

                        }

                        #endregion

                        #region Calcolo dei parametri specifici delle registrazioni

                        // si calcola la durata minima e massima ammessa per le registrazioni
                        // i dati sono calcolati in gerarchia:
                        // - si recuperano i dati del collaboratore;
                        // - se non configurati si passa a recuperarli dal cantiere.
                        // - se anche sul cantiere non sono configurati allora li si recupera dai parametri

                        // sono recuperati dal collaboratore i parametri di durata massima e minima della timbratura
                        maxElapsed = currentCol.Durata_Max_Ril_Col ?? TimeSpan.Zero;
                        minElapsed = currentCol.Durata_Min_Ril_Col ?? TimeSpan.Zero;

                        // se il parametro di durata massima della timbratura non è configurato sul collaboratore, si procede al suo recupero dal cantiere
                        if (maxElapsed == TimeSpan.Zero)
                            maxElapsed = currentCant.Durata_Max_Ril_Can ?? TimeSpan.Zero;

                        // se il parametro di durata minima della timbratura non è configurato sul collaboratore, si procede al suo recupero dal cantiere
                        if (minElapsed == TimeSpan.Zero)
                            minElapsed = currentCant.Durata_Min_Ril_Can ?? TimeSpan.Zero;

                        // se il parametro di durata massima della timbratura non è configurato ne sul collaboratore ne sul cantiere,
                        // si procede al suo recupero dalla scheda parametri generali
                        if (maxElapsed == TimeSpan.Zero)
                            maxElapsed = maxElapsedParam;

                        // se il parametro di durata minima della timbratura non è configurato ne sul collaboratore ne sul cantiere,
                        // si procede al suo recupero dalla scheda parametri generali
                        if (minElapsed == TimeSpan.Zero)
                            minElapsed = minElapsedParam;

                        #endregion

                        #region Controllo numero dispari in base a customizzazione

                        // si calcolano gli estremi da verificare in base al parametro notturno impostato:
                        // 1. se il notturno non è impostato allora si procede al calcolo con il giorno corrente (00:00 - 23:59)
                        // 2. se il notturno è abilitato si calcola il giorno corrente (dal threshold) al giorno seguente (al threshold)
                        DateTime minSearchDate = DateTime.MinValue;
                        DateTime maxSearchDate = DateTime.MinValue;
                        if (nocturneType == NocturneTypeEnum.None || nocturneType == NocturneTypeEnum.Disabled || nocturneThreshold.Ticks <= 0)
                        {
                            minSearchDate = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month, currentReg.Registrazione_Data_Ora_Fis_Reg.Day, 0, 0, 0);
                            maxSearchDate = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month, currentReg.Registrazione_Data_Ora_Fis_Reg.Day, 23, 59, 59);
                        }

                        //se è attivo il notturno con "nuova M
                        else if (nocturneType == NocturneTypeEnum.OverMidnight && nocturneThreshold.Ticks > 0)
                        {
                            //calcolo il giorno successivo alla registrazione corrente
                            DateTime nextRegDate = currentReg.Registrazione_Data_Ora_Fis_Reg.AddDays(1);

                            //vengono create le date minime e massime di ricerca in base al valore della soglia
                            minSearchDate = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month, currentReg.Registrazione_Data_Ora_Fis_Reg.Day, nocturneThreshold.Hours, nocturneThreshold.Minutes, nocturneThreshold.Seconds);
                            maxSearchDate = new DateTime(nextRegDate.Year, nextRegDate.Month, nextRegDate.Day, nocturneThreshold.Hours, nocturneThreshold.Minutes, nocturneThreshold.Seconds);
                        }

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

                    if (currentReg.Registrazione_Stato_Reg == (int)RegStateEnum.Ass)
                    {
                        isToElaborate = false;
                    }

                    // se è stato superato il controllo di conformità allora si procede all'elaborazione dei dati
                    if (isToElaborate)
                    {
                        #region Abbinamento delle registrazioni

                        // se si sta processando una nuova reg, e cioè:
                        //   - si tratta di una registrazione di entrata o senza flag di direzione
                        //   - ... e si tratta della prima elaborazione per una coppia
                        // allora la si setta come entrata di una possibile coppia
                        // altrimenti, se non si tratta di una prima registrazione di coppia ed è un'uscita o è senza direzione, si cerca di effettuare l'abbinamento
                        // in base ai parametri specifcati;
                        // se nessuna delle due precedenti condizioi risulta verficata allora si tratta dell'entrata di una nuova coppia e come tale la si setta
                        if ((currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E) && lastOpen == null)
                            lastOpen = currentReg;
                        else if (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U)
                        {
                            // se è stata già indicata un'entrata allora si può procedere con il tentativo di abbinamento
                            if (lastOpen != null)
                            {
                                // se il notturno risulta abilitato e correttamente configurato (cioè non riporta la mezzanotte)
                                if (((nocturneType != NocturneTypeEnum.None) && ((nocturneType != NocturneTypeEnum.Disabled))) && (nocturneThreshold.Ticks > 0 || nocturneDuration.Ticks > 0))
                                {

                                    #region Abbinamento delle registrazioni in caso di notturno abilitato con NUOVA MEZZANOTTE

                                    // nel caso il notturno sia configurato come x ore dopo mezzanotte (per ora viene gestitio solo questo tipo di notturno,
                                    // ma in futuro potranno essere distinti)
                                    if (nocturneType == NocturneTypeEnum.OverMidnight)
                                    {
                                        // in caso di notturno le registrazioni risultano abbinabili se:
                                        // - la registrazione marcata come uscita è nello stesso o successivo giorno rispetto all'entrata,
                                        //   ha lo stesso cantiere dell'entrata ed è identificata come uscita o senza direzione;
                                        // altrimenti, se la registrazione non risulta abbinabile:
                                        // - se l'ultima registrazione risulta essere una potenziale entrata (senza direzione o con direzione E) allora la si tratta come tale
                                        // - altrimenti si riparte scartando l'intero tenativo di abbinamento

                                        if ((currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date.AddDays(1) || currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date)
                                            && currentReg.Cant_Id == lastOpen.Cant_Id && (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U))
                                        {

                                            // se le registrazioni sono nello stesso giorno allora si verifca la coerenza della possibile uscita con il threshold del notturno:
                                            // - se l'uscita è successiva al threshold allora si verifica che anche lentrata lo sia (caso di registrazione diurna in notturnO) e,
                                            //   in caso di stessa motivazione si procede al tentativo di abbinamento, azzerando il gruppo per ripartire
                                            //   da una nuova entrata (in caso le motivazioni non siano coerenti tra le due timbrature si passa ad elaborare il 
                                            //   gurppo successivo eventualmente mantenendo la presente registrazione in elaborazione come entrata).
                                            // - se invece l'uscita è successiva al threshold e l'entrata anche (caso di timbratura a cavallo del limite di notturno) 
                                            //   si procede al loro tentativo di abbinamento (con attenzione alle motivazioni come di cui sopra solamente se sono entrata e uscita.
                                            // - in caso l'uscita sia inferiore o uguale al threshold e lo sia anche l'entrata (caso di timbratura dopo mezzanotte ma inferiore
                                            //   al limite di notturno) allora si procede al tentativo di abbinamento delle timbrature) con l'attezione alla motivazione
                                            //   di cui sopra.
                                            // - altrimenti ci si ritrova nel caso in cui la registrazione è a in giornata ma sicuramente antecedente al threshold, in questo
                                            //   si tenta l'abbinamento con la solita attenzione alle motivazioni
                                            // se le registrazioni invece sono di giorni diversi (l'uscita nel giorno successivo all'entrata) si verifica la conformità dell'uscita
                                            // al threshold:
                                            // - se l'uscita risulta inferiore al threshold allora, coerentemente con le motivazioni, si procede al tentativo di abbinamento;
                                            // - se le timbrature sono a cavallo del notturno ma la precedente è un'entrata e la successiva un'uscita allora si procede al loro abbinamento (di modo
                                            //   da riuscire a gestire con la direzione turni consecutivi di lavoro)
                                            // - altrimenti si riprate con un nuovo gruppo

                                            // se le due regitrazioni si trovano nella stessa data
                                            if (currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date)
                                            {
                                                // se l'ora d'uscita è superiore al limite di notturno
                                                if (currentReg.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > nocturneThreshold)
                                                {
                                                    // se l'ora d'entrata è superiore al limite di notturno (timbrature nello stesso giorno sopra il limite di notturno)
                                                    if (lastOpen.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > nocturneThreshold)
                                                    {
                                                        // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                                        if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                                        {
                                                            // tentativo di abbinamento delle registrazioni
                                                            processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                            // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                                                            lastOpen = null;
                                                        }
                                                        else // se le motivazioni non sono le stesse si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                                            lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                    }
                                                    else // se l'ora d'entrata è maggiore del limite notturno ma l'entrata non lo è (timbratura a cavallo dell'ora di notturno nella stessa giornata)
                                                    {
                                                        // se l'entrata ha il flag di entrata e l'uscita ha il flag di uscita
                                                        if (lastOpen.FlagEURegTypeEnum == FlagEURegTypeEnum.E && currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U)
                                                        {
                                                            // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                                            if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                                            {
                                                                // in caso non ci sia concordanza di threshold, siamo comunque nello stesso giorno e, se si sta trattando un'entrata e un'uscita
                                                                // allora si procede all'abbinamento
                                                                processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                                // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                                                                lastOpen = null;
                                                            }
                                                            else // se le motivazioni non sono le stesse si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                                                lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                        }
                                                        else // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                                            lastOpen = currentReg.FlagEURegTypeEnum != FlagEURegTypeEnum.U ? currentReg : null;
                                                    }
                                                }
                                                else if ((currentReg.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= nocturneThreshold) && (lastOpen.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= nocturneThreshold))
                                                {
                                                    // se l'entrata e l'uscita sono nello stesso giorno ed entrambe sono minori o uguali al limite del notturno allora devo tentare di abbinarle

                                                    // se esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                                    if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                                    {
                                                        // tentativo di abbinamento delle registrazioni
                                                        processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                        // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                                                        lastOpen = null;
                                                    }
                                                    else // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                                        lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                }
                                                else if (lastOpen.FlagEURegTypeEnum == FlagEURegTypeEnum.E && currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U)
                                                {
                                                    // se l'entrata e l'uscita sono nello stesso giorno ma a cavallo del threshold allora si tenta l'abbinamento solamente se 
                                                    // l'entrata è marcata come direzione entrata e l'uscita è marcata come direzione uscita

                                                    // se esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                                    if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                                    {
                                                        // tentativo di abbinamento delle registrazioni
                                                        processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                        // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                                                        lastOpen = null;
                                                    }
                                                    else // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                                        lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                }
                                            }
                                            else if (currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date.AddDays(1))
                                            {
                                                // altrimeni se la registrazione d'uscita risulta essere il giorno successivo all'entrata

                                                // si procede all'abbinamento delle reg solamente se l'uscita è compresa prima del threshold
                                                if (currentReg.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= nocturneThreshold)
                                                {
                                                    // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                                    if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                                    {
                                                        // tentativo di abbinamento delle registrazioni
                                                        processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                        // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                                                        lastOpen = null;
                                                    }
                                                    else // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                                        lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                }
                                                else if (lastOpen.FlagEURegTypeEnum == FlagEURegTypeEnum.E && currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U)
                                                {
                                                    // se l'entrata e l'uscita sono in giorni diversi ma a cavallo del threshold allora si tenta l'abbinamento solamente se 
                                                    // l'entrata è marcata come direzione entrata e l'uscita è marcata come direzione uscita

                                                    // se esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                                    if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                                    {
                                                        // tentativo di abbinamento delle registrazioni
                                                        processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                        // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                                                        lastOpen = null;
                                                    }
                                                    else // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                                        lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                }

                                                else // altrimenti si riparte con una nuova coppia di entrata e uscita
                                                    lastOpen = currentReg;
                                            }
                                        }
                                        else if (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E)
                                        {
                                            // se le registrazioni non sono abbinabili secondo i parametri del notturno impsotati
                                            // e la registrzione corrente è marcata con direzione entrata o senza direzione
                                            // allora si procede a impostare la registrazione corrente come nuova entrata
                                            lastOpen = currentReg;
                                        }
                                        else // altrimenti si riparte con una nuova coppia di entrata e uscita
                                            lastOpen = null;
                                    }

                                    #endregion

                                    #region Abbinamento delle registrazioni in caso di notturno abilitato per DURATA

                                    // nel caso il notturno sia configurato il calcolo del notturno per durata
                                    else if (nocturneType == NocturneTypeEnum.Duration)
                                    {
                                        // in caso di notturno per durata le registrazioni risultano abbinabili se:
                                        // - la registrazione marcata come uscita è nello stesso o successivo giorno rispetto all'entrata e ,
                                        //   ha lo stesso cantiere dell'entrata ed è identificata come uscita o senza direzione;
                                        // altrimenti, se la registrazione non risulta abbinabile:
                                        // - se l'ultima registrazione risulta essere una potenziale entrata (senza direzione o con direzione E) allora la si tratta come tale
                                        // - altrimenti si riparte scartando l'intero tenativo di abbinamento

                                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CloseDifferentCant) == 0)
                                        {
                                            //viene controllato se la registrazione che si sta processando è nel giorno successivo all'ultima reg processata e l'ultima reg processata sia diversa da un'uscita
                                            if ((currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date.AddDays(1))
                                                && currentReg.Cant_Id == lastOpen.Cant_Id)
                                            {

                                                //Viene per prima cosa controlalta che la registrazione di uscita sia nell'intervallo che scatta dall'entrata fino alla durata massima del notturno
                                                //se così non è si passa alla registrazione successiva

                                                DateTime nocturnBoundMax = new DateTime();

                                                //viene sommata all'ultima registrazione la durata massima del notturno per verificare che la registrazione successiva ricada nel range
                                                nocturnBoundMax = lastOpen.Registrazione_Data_Ora_Fis_Reg.AddMinutes(nocturneDuration.TotalMinutes);

                                                // si procede all'abbinamento delle reg solamente se l'uscita è compresa prima del threshold
                                                if (currentReg.Registrazione_Data_Ora_Fis_Reg <= nocturnBoundMax)
                                                {
                                                    // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                                    if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                                    {
                                                        // tentativo di abbinamento delle registrazioni
                                                        processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                        // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                                                        lastOpen = null;
                                                    }
                                                    else
                                                        // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                                        lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                }
                                                //se le registrazione cadono furoi dalla durata massima del nottunro non vengono abbinate
                                                else
                                                {
                                                    lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                }
                                            }

                                            //se le registrazioni contigue non apparetnego a giorni diversi  ma apprtengono allo stesso giorno
                                            else
                                            {
                                                // le registrazioni in porcesso risultano abbinabili solamente se:
                                                // - le due registrazioni sono nella stessa data
                                                // - le due registrazioni hanno lo stesso cantiere
                                                // - la registrazione di uscita è marcata come uscita o senza direzione
                                                if (currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date && currentReg.Cant_Id == lastOpen.Cant_Id &&
                                                (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U))
                                                {
                                                    // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                                    if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                                    {
                                                        // tentativo di abbinamento delle registrazioni
                                                        processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                        // una volta effettuato il tentativo di abbinamento si riparte da una nuova coppia di entrata/uscita
                                                        lastOpen = null;
                                                    }
                                                    else // se le motivazioni non sono le stesse si procede alla coppia di reg successiva (eventualmente mantenendo la corrente come entrata)
                                                        lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                }
                                                else if (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E)
                                                {
                                                    // se le registrazioni non sono abbinabili secondo i parametri del notturno impsotati
                                                    // e la registrzione corrente è marcata con direzione entrata o senza direzione
                                                    // allora si procede a impostare la registrazione corrente come nuova entrata
                                                    lastOpen = currentReg;
                                                }
                                                else // altrimenti si riparte con una nuova coppia di entrata e uscita
                                                    lastOpen = null;

                                            }
                                        }
                                        else {
                                            //viene controllato se la registrazione che si sta processando è nel giorno successivo all'ultima reg processata e l'ultima reg processata sia diversa da un'uscita
                                            if ((currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date.AddDays(1)))
                                            {
                                                //Viene per prima cosa controlalta che la registrazione di uscita sia nell'intervallo che scatta dall'entrata fino alla durata massima del notturno
                                                //se così non è si passa alla registrazione successiva

                                                DateTime nocturnBoundMax = new DateTime();

                                                //viene sommata all'ultima registrazione la durata massima del notturno per verificare che la registrazione successiva ricada nel range
                                                nocturnBoundMax = lastOpen.Registrazione_Data_Ora_Fis_Reg.AddMinutes(nocturneDuration.TotalMinutes);

                                                // si procede all'abbinamento delle reg solamente se l'uscita è compresa prima del threshold
                                                if (currentReg.Registrazione_Data_Ora_Fis_Reg <= nocturnBoundMax)
                                                {
                                                    // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                                    if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                                    {
                                                        // tentativo di abbinamento delle registrazioni
                                                        processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                        // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                                                        lastOpen = null;
                                                    }
                                                    else
                                                        // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                                        lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                }
                                                //se le registrazione cadono furoi dalla durata massima del nottunro non vengono abbinate
                                                else
                                                {
                                                    lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                }
                                            }

                                            //se le registrazioni contigue non apparetnego a giorni diversi  ma apprtengono allo stesso giorno
                                            else
                                            {
                                                // le registrazioni in porcesso risultano abbinabili solamente se:
                                                // - le due registrazioni sono nella stessa data
                                                // - le due registrazioni hanno lo stesso cantiere
                                                // - la registrazione di uscita è marcata come uscita o senza direzione
                                                if (currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date &&
                                                (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U))
                                                {
                                                    // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                                    if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                                    {
                                                        // tentativo di abbinamento delle registrazioni
                                                        processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                        // una volta effettuato il tentativo di abbinamento si riparte da una nuova coppia di entrata/uscita
                                                        lastOpen = null;
                                                    }
                                                    else // se le motivazioni non sono le stesse si procede alla coppia di reg successiva (eventualmente mantenendo la corrente come entrata)
                                                        lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                                }
                                                else if (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E)
                                                {
                                                    // se le registrazioni non sono abbinabili secondo i parametri del notturno impsotati
                                                    // e la registrzione corrente è marcata con direzione entrata o senza direzione
                                                    // allora si procede a impostare la registrazione corrente come nuova entrata
                                                    lastOpen = currentReg;
                                                }
                                                else // altrimenti si riparte con una nuova coppia di entrata e uscita
                                                    lastOpen = null;

                                            }
                                        }
                                    }

                                    #endregion

                                }
                                else
                                {

                                    #region Abbinamento delle registrazioni in caso di notturno disabilitato

                                    //se la personalizzazione per abbinare le registrazioni anche se non sono sullo stesso cantiere non è attiva procedo con un associazione standard
                                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CloseDifferentCant) == 0)
                                    {
                                        // le registrazioni in processo risultano abbinabili solamente se:
                                        // - le due registrazioni sono nella stessa data
                                        // - le due registrazioni hanno lo stesso cantiere
                                        // - la registrazione di uscita è marcata come uscita o senza direzione
                                        if (currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date && currentReg.Cant_Id == lastOpen.Cant_Id &&
                                                (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U))
                                        {
                                            // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                            if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                            {
                                                // tentativo di abbinamento delle registrazioni
                                                processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                // una volta effettuato il tentativo di abbinamento si riparte da una nuova coppia di entrata/uscita
                                                lastOpen = null;
                                            }
                                            else // se le motivazioni non sono le stesse si procede alla coppia di reg successiva (eventualmente mantenendo la corrente come entrata)
                                                lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                        }
                                        else if (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E)
                                        {
                                            // se le registrazioni non sono abbinabili secondo i parametri del notturno impsotati
                                            // e la registrzione corrente è marcata con direzione entrata o senza direzione
                                            // allora si procede a impostare la registrazione corrente come nuova entrata
                                            lastOpen = currentReg;
                                        }
                                        else // altrimenti si riparte con una nuova coppia di entrata e uscita
                                            lastOpen = null;
                                    }
                                    //se la personalizzazione è attiva rimuovo il controllo sullo stesso cantiere per associare le timbrature
                                    else {
                                        // le registrazioni in processo risultano abbinabili solamente se:
                                        // - le due registrazioni sono nella stessa data
                                        // - le due registrazioni hanno lo stesso cantiere
                                        // - la registrazione di uscita è marcata come uscita o senza direzione
                                        if (currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date && 
                                                (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U))
                                        {
                                            // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                            if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                            {
                                                // tentativo di abbinamento delle registrazioni
                                                processErrors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                                // una volta effettuato il tentativo di abbinamento si riparte da una nuova coppia di entrata/uscita
                                                lastOpen = null;
                                            }
                                            else // se le motivazioni non sono le stesse si procede alla coppia di reg successiva (eventualmente mantenendo la corrente come entrata)
                                                lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                                        }
                                        else if (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E)
                                        {
                                            // se le registrazioni non sono abbinabili secondo i parametri del notturno impsotati
                                            // e la registrzione corrente è marcata con direzione entrata o senza direzione
                                            // allora si procede a impostare la registrazione corrente come nuova entrata
                                            lastOpen = currentReg;
                                        }
                                        else // altrimenti si riparte con una nuova coppia di entrata e uscita
                                            lastOpen = null;
                                    }                                    
                                    

                                    #endregion
                                }
                            }
                        }
                        else // registrazione non coerente con flag entrata e/o processo; la si tratta come una nuova entrata
                            lastOpen = currentReg;

                        #endregion
                    }
                }

                #endregion

            }
        }

        /// <summary>
        /// Disaccoppia le specifiche registrazioni impostando il tipo registrazione ora o attività in base al cantiere di riferimento.
        /// </summary>
        /// <param name="regs">Le registrazioni da processare.</param>
        /// <param name="startDate">La prima data da cui partono le registrazioni dell'elaborazione che si sta trattando.</param>
        /// <param name="endDate">L'ultima data da cui partono le registrazioni dell'elaborazione che si sta trattando.</param>
        private void DecoupleRegsAndSetType(IEnumerable<Reg> regs, DateTime startDate, DateTime endDate)
        {
            // si recupera l'abilitazione o meno del modulo attività
            bool attModuleActive = RepoManager.ParamRepo.ParametersRow.Abilita_Att;

            // si recuperano tutti i cantierei attività presenti all'interno dell'applicativo
            HashSet<int> activityCantIds = new HashSet<int>(RepoManager.CantRepo.DbSet.AsNoTracking().Where(cant => cant.Tipologia_Can.ToUpper() == "ATT").Select(cant => cant.Cant_Id).ToList());

            // si recuperano i parametri generali di notturno
            Tuple<bool, NocturneTypeEnum, TimeSpan, TimeSpan> nocturneGlobalConfiguration = RepoManager.ParamRepo.NocturneGeneralConfiguration;

            // per ogni registrazione da processare (cioè non viaggio)
            regs.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip && reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Duration).ToList().ForEach(reg =>
            {
                // si procede a disaccoppiare la registrazione solamente se risulta necessario farlo
                if (IsToDecuple(reg, startDate, endDate, nocturneGlobalConfiguration))
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

        /// <summary>
        /// Determina se la specifica registrazione risulta tra le registrazioni da disaccoppiare o meno.
        /// </summary>
        /// <param name="regToCheck">La registrazione da processare.</param>
        /// <param name="startDate">La data di inizio dell'elaborazione.</param>
        /// <param name="endDate">La data di fine dell'elaborazione.</param>
        /// <param name="nocturneGlobalConfiguration">La configurazione globale del notturno.</param>
        /// <returns><c>true</c> se la registrazione specificata sarà da disaccoppiare; altrimenti <c>false</c></returns>
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
                    Col currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == regToCheck.Col_Id);
                    Cant currentCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == regToCheck.Cant_Id);

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
        /// Esegue l'associazione delle unità fisse e delle unità portatili per le registrazioni specifiche.
        /// </summary>
        /// <param name="regs">L'elenco delle registrazioni su cui effettuare l'associazione delle unità portatili e fisse.</param>
        /// <returns>L'elenco degli errori riscontrati durante l'associazione delle unità portatili e fisse.</returns>
        private IEnumerable<KeyValuePair<string, string>> AssociatePruFru(ICollection<Reg> regs)
        {
            // inizializzazione dell'elenco di errori da ritornare
            var errors = new List<KeyValuePair<string, string>>();

            // se ci sono delle registrazioni su cui effettuare l'abbinamento
            if (regs.Any())
            {
                // calcolo della data massima presenti tra le registrazioni passate come parameto
                DateTime regMaxDate = regs.Max(reg => reg.Registrazione_Data_Ora_Orig_Reg).Date;

                // sono lette tutte le matricole portatili e fisse non disabilitate con data di associazione inferiore o uguale alla data massima da processare;
                // le anagrafiche così recuperate sono ordinate in senso discendente per data abilitazione, di modo da avere le più recenti in cima alla lista
                var pruCols = RepoManager.Pru_ColRepo.Find(pruCol => !pruCol.DisAbilitazione_Pru_Col, true)
                    .OrderByDescending(pru => pru.Abilitazione_Data_Inizio_Pru_Col).ToList();
                var fruCants = RepoManager.Fru_CantRepo.Find(fruCant => !fruCant.DisAbilitazione_Fru_Can, true)
                    .OrderByDescending(fru => fru.Abilitazione_Data_Inizio_Fru_Can).ToList();

                // con i dati delle anagrafiche sono costruiti dei dizionari che come chiave hanno l'id dell'anagrafca e come valore l'elenco delle associazioni
                // ordinate per data associazione discendente
                var pruColDic = new Dictionary<int, List<Pru_Col>>();
                pruCols.ForEach(pruCol =>
                {
                    if (!pruColDic.ContainsKey(pruCol.Pru_Id))
                        pruColDic.Add(pruCol.Pru_Id, new List<Pru_Col>());
                    pruColDic[pruCol.Pru_Id].Add(pruCol);
                });

                var fruCantDic = new Dictionary<int, List<Fru_Cant>>();
                fruCants.ForEach(fruCant =>
                {
                    if (!fruCantDic.ContainsKey(fruCant.Fru_Id))
                        fruCantDic.Add(fruCant.Fru_Id, new List<Fru_Cant>());
                    fruCantDic[fruCant.Fru_Id].Add(fruCant);
                });

                // si cicla su tutte le registrazioni da associare (si considerano registrazioni da associare tutte le registrazioni
                // che hanno almeno o la Pru o la Fru)
                foreach (Reg reg in regs.Where(rg => rg.Fru_Id != null || rg.Pru_Id != null || rg.Cant_Id != null).ToList())
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
                        if (pruColDic.ContainsKey(reg.Pru_Id.Value))
                            currentPruCol = pruColDic[reg.Pru_Id.Value].FirstOrDefault(pruCol => pruCol.Abilitazione_Data_Inizio_Pru_Col <= reg.Registrazione_Data_Ora_Fis_Reg);

                        // se è stata trovata l'associazione a un'unità portatile allora si imposta il collaboratore sulla registrazione;
                        // altrimenti si scrive l'errore
                        if (currentPruCol != null)
                            reg.Col_Id = currentPruCol.Col_Id;
                        else
                        {
                            string errorUserString = String.Format("Reg: {0} Matricola: {1} non associata", reg.Reg_Id, reg.Pru == null
                                ? Convert.ToInt32(reg.Pru_Id).ToString()
                                : reg.Pru.Codice_Pru);
                            errors.Add(new KeyValuePair<string, string>(FunctionMessageEnum.Elaborate.ToString(), errorUserString));
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
                        if (fruCantDic.ContainsKey(reg.Fru_Id.Value))
                            currentFruCant = fruCantDic[reg.Fru_Id.Value].FirstOrDefault(fruCant => fruCant.Abilitazione_Data_Inizio_Fru_Can <= reg.Registrazione_Data_Ora_Fis_Reg);

                        // se è stata trovata l'associazione a un'unità fissa allora si imposta il cantiere sulla registrazione;
                        // altrimenti si scrive l'errore
                        if (currentFruCant != null)
                        {
                            reg.Cant_Id = currentFruCant.Cant_Id;
                            var allCdc = RepoManager.CentroDiCostoRepo.GetAll().ToList();//.Select(r => r.Cant_CentroDiCosto.Where(c => c.Cant_Id == currentFruCant.Cant_Id)).ToList();
                            if (allCdc.Count() > 0) {
                                bool centro = false;
                                int cid = 0;
                                foreach (var tmp in allCdc)
                                {
                                    if (centro == false)
                                    {
                                        var test = tmp.Cant_CentroDiCosto.Where(t => t.Cant_Id == currentFruCant.Cant_Id);
                                        foreach (var test1 in test)
                                        {
                                            if (test1.Cant_Id == currentFruCant.Cant_Id)
                                            {
                                                centro = true;
                                                cid = test1.CentroDiCosto_Id;
                                            }
                                        }
                                    }
                                }
                                var temp = allCdc.First().Cant_CentroDiCosto;
                                var cdc = temp.Where(c => c.Cant_Id == currentFruCant.Cant_Id);
                                if (centro)
                                {
                                    reg.CentroDiCosto_Id = cid;
                                }
                                else {
                                    reg.CentroDiCosto_Id = null;
                                }
                            }
                        }
                        else
                        {
                            string errorUserString = String.Format("Reg: {0} Matricola: {1} non associata", reg.Reg_Id, reg.Fru == null
                                ? Convert.ToInt32(reg.Pru_Id).ToString()
                                : reg.Fru.Codice_Fru);
                            errors.Add(new KeyValuePair<string, string>(FunctionMessageEnum.Elaborate.ToString(), errorUserString));
                        }

                        #endregion

                    }

                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.MantainCoordinateModifiedRegs) == 0) {
                        //se la registrazione è gps andiamo a controllare se con i nuovi parametri il cantiere più vicino rimane lo stesso
                        if ((reg.Registrazione_Lat_Orig != null && reg.Registrazione_Long_Orig != null && reg.Registrazione_Lat_Orig != 0 && reg.Registrazione_Long_Orig != 0) && reg.Fru_Id == null)
                        {

                            #region Associazione del cantiere
                            int cantId = 0;
                            cantId = GetGpsCantId(reg.Registrazione_Lat_Orig.Value, reg.Registrazione_Long_Orig.Value);
                            //in caso il cantiere sia cambiato o non ce ne sia uno vicino andiamo ad assocciare il nuovo cantiere
                            if (cantId != 0 && cantId != reg.Cant_Id.Value)
                            {
                                reg.Cant_Id = cantId;
                            }
                            #endregion

                        }
                    }

                    if (reg.Cant_Id != null) {
                        var allCdc = RepoManager.CentroDiCostoRepo.GetAll().ToList();//.Select(r => r.Cant_CentroDiCosto.Where(c => c.Cant_Id == currentFruCant.Cant_Id)).ToList();
                        if (allCdc.Count() > 0)
                        {
                            bool centro = false;
                            int cid = 0;
                            foreach (var tmp in allCdc)
                            {
                                if (centro == false)
                                {
                                    var test = tmp.Cant_CentroDiCosto.Where(t => t.Cant_Id == reg.Cant_Id);
                                    foreach (var test1 in test)
                                    {
                                        if (test1.Cant_Id == reg.Cant_Id)
                                        {
                                            centro = true;
                                            cid = test1.CentroDiCosto_Id;
                                        }
                                    }
                                }
                            }
                            var temp = allCdc.First().Cant_CentroDiCosto;
                            var cdc = temp.Where(c => c.Cant_Id == reg.Cant_Id);
                            if (centro)
                            {
                                reg.CentroDiCosto_Id = cid;
                            }
                            else {
                                reg.CentroDiCosto_Id = null;
                            }
                        }
                    }
                }




            }

            // ritorno del valore calcolato dal metodo
            return errors;
        }

        /// <summary>
        /// Restituisce l'elenco di registrazioni processabili per l'elaborate tra quelle passate come parametro.
        /// Sono quindi tolte dall'elenco passato come parametro tutte le registrazioni sempre abbinate, le registrazioni antecedenti alla data blocco le registrazioni bloccate.
        /// Le registrazioni bloccate sono anche ccoppiate tra di loro secondo il codice temporaneo in esse impostato.
        /// </summary>
        /// <param name="regs">Le registrazioni da cui estrarre quelle da elaborare.</param>
        /// <returns>Le registrazioni processabili dall'elaborate tra quelle passate come parametro.</returns>
        private ICollection<Reg> RemoveAndProcessNonUsedInElaborateRegs(ICollection<Reg> regs)
        {
            // si procede ad effettuare qualsiasi elaborazione se sono presenti delle registrazioni
            if (regs.Any())
            {
                // per prima cosa si accoppiano le registrazioni macate per essere bloccate dal codice di acooppiamento
                CoupleBlockedRegs(regs.Where(reg => !String.IsNullOrEmpty(reg.Codice_Accoppiamento)).ToList());

                // calcolo della data blocco da utilizzare per filtrare leregistrazioni
                DateTime blockDate = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg != null
                    ? RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.Value.AddDays(1)
                    : DateTime.MinValue;



                // dalle registrazioni passate come parametro si tolgono le registrazioni dopo la data blocco, le rettifiche, le solo durata, le registrazioni bloccate
                regs = regs.Where(reg => reg.Registrazione_Data_Ora_Fis_Reg >= blockDate && reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.RettTimesheet
                    /*&& reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Duration*/ &&
                    reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.RettTimeSheetManual &&
                    !reg.Registrazione_Bloccata).ToList();
            }

            // ritorno della lista processata
            return regs;
        }

        /// <summary>
        /// Inizializza per il metodo di elaborate i parametri utilizzati per la scrittura della tabella messaggi e ritorna l'applicazione corrente.
        /// </summary>
        /// <param name="elaborateUserId">L'identificativo dell'utente da utilizzare nella tabella messaggi (null se non ancora impostato).</param>
        /// <param name="elaborateDateTime">La data e ora elaborazione da utilizzare nella tabella messaggi (nul se non ancora impostata).</param>
        /// <param name="application">L'applicazione di lancio dell'elaborazione da utilizzare nella tabella messaggi (null se non ancora impostata).</param>
        /// <returns>L'applicazione da utilizzare risultante dal calcolo del metodo.</returns>
        private ApplicationMessageEnum InitializeElaborateMessagesParameters(int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum? application)
        {
            // inizializzazione della definizione dell'applicativo per la scrittura dei messaggi nell'apposita tabella
            var currentApplication = application ?? ApplicationMessageEnum.Elaborate;

            // inizializzazione dell'utente e della data/ora di avvio dell'elaborazione
            _elaborateUserId = elaborateUserId ?? PowerWebContext.Current.User.Utenti_Id;
            _elaborateDateTime = elaborateDateTime ?? DateTime.Now;

            // ritorno dell'applicazione
            return currentApplication;
        }

        /// <summary>
        /// Aggiorna i dizionari utilizzati per mandare messaggi all'interfaccia grafica con la percentuale e la stringa indicata.
        /// </summary>
        /// <param name="percVal">Il valore della percentuale da impostare.</param>
        /// <param name="message">La stringa da impostare come messaggio.</param>
        private void ManageElaborateMessageDictionaries(double percVal, string message)
        {
            if (PowerWebContext.Current.User == null)
                return;

            BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percVal, message);
            BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percVal, message);
        }

        /// <summary>
        /// Metodo che si occupa di accoppiare le reg bloccate come parametro e legate da un codice temporaneo inserito nel campo Codice_Accoppiamento.
        /// Questo metodo si occupa, al termine dell'elaborazione, di cancellare il codice temporaneo dalle reg.
        /// Questo metodo salva da se i dati nel contesto (non dipende quindi da dove è richiamato)
        /// </summary>
        /// <param name="regsToCouple">La lista di reg da accoppiare (si da per assodato che all'entrata abbiano tutte il codice accoppiamento.</param>
        private void CoupleBlockedRegs(IEnumerable<Reg> regsToCouple)
        {
            // se le reg passate come parametro non sono null
            if (regsToCouple != null)
            {
                var hoursToCouple = regsToCouple.Where(reg => !reg.Cant.IsActivity).ToList();
                var activitiesToCouple = regsToCouple.Where(reg => reg.Cant.IsActivity).ToList();

                #region Scrittura dell'id corretto nelle attività bloccate e accoppiate o meno

                // se ci sono delle attività bloccate da processare
                if (activitiesToCouple.Count > 0)
                {
                    for (int i = 0; i < activitiesToCouple.Count; i++)
                    {
                        // si verifica che la attività in elaborazione abbia il codice di accoppiamento,
                        // che potrebbe essere stato tolto in giro precedente del ciclo
                        if (!String.IsNullOrEmpty(activitiesToCouple[i].Codice_Accoppiamento))
                        {
                            // recupero tutte le ore collegate all'attività
                            var linkedRegs = hoursToCouple.Where(reg => reg.Codice_Accoppiamento == activitiesToCouple[i].Codice_Accoppiamento).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                            // se ho delle reg ore collegate allora significa che ho anche un'entrata/uscita e quindi inserisco sull'attività
                            // il riferimento alla registrazione di entrata a cui è abbinata
                            if (linkedRegs.Count > 0)
                            {
                                var regE = linkedRegs.First();
                                activitiesToCouple[i].RiferimentoRRN_Att = regE.Reg_Id;
                                activitiesToCouple[i].Codice_Accoppiamento = null;
                                activitiesToCouple[i].Registrazione_Tipo_Reg = (int)RegTypeEnum.Att;
                            }
                            else
                            {
                                // se invece non ci sono ore collegate significa che sto processando una attività singola,
                                // e quindi mi limito a svuotare il codice di accoppiamento.
                                activitiesToCouple[i].Codice_Accoppiamento = null;
                                activitiesToCouple[i].Registrazione_Tipo_Reg = (int)RegTypeEnum.Att;
                            }
                        }
                    }

                    // al termine del trattamento delle attività bloccate salvo sul db quanto fatto
                    Update(activitiesToCouple, true);
                }

                #endregion

                #region Scrittura dell'id corretto nelle reg bloccate e accoppiate

                if (hoursToCouple.Count > 0)
                {
                    // sono elaborate tutte le ore passate come parametro
                    for (int i = 0; i < hoursToCouple.Count; i++)
                    {
                        // si verifica che la reg in elaborazione abbia il codice di accoppiamento,
                        // che potrebbe essere stato tolto in giro precedente del ciclo
                        if (!String.IsNullOrEmpty(hoursToCouple[i].Codice_Accoppiamento))
                        {
                            // si recuperano le due reg che hanno quel codice di acccoppiamento
                            var hourCouple =
                                hoursToCouple.Where(
                                    reg => reg.Codice_Accoppiamento == hoursToCouple[i].Codice_Accoppiamento).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                            // se sono state trovate due reg da accoppiare (situazione normale)
                            if (hourCouple.Count == 2)
                            {
                                // recupero di reg in entrata e in uscita
                                Reg regEToCouple = hourCouple.First();
                                Reg regUToCouple = hourCouple.Last();

                                // aggiornamento del relative record number di riferimento sulla reg in uscita
                                // e svuotamento per entrambe le reg del codice di accoppiamento
                                regUToCouple.RiferimentoRRN_Reg = regEToCouple.Reg_Id;
                                regEToCouple.Codice_Accoppiamento = null;
                                regUToCouple.Codice_Accoppiamento = null;

                                // le reg sono indicate come accoppiate
                                regEToCouple.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                                regUToCouple.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;

                            }
                            else
                            // altrimenti, sono registrazioni singole e quindi si procede allo svuotamento del codice di accoppiamento
                            {
                                foreach (var reg in hourCouple)
                                {
                                    reg.Codice_Accoppiamento = null;
                                }
                            }
                        }
                    }

                    // aggiornamento delle reg di ore modificate (se presenti)
                    Update(hoursToCouple, true);
                }



                #endregion

            }
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

        #region Gestione delle causali importate da dispositivo

        /// <summary>
        /// Elimina dalle registrazioni attuali e dal database tutte le registrazioni generate automaticamente a chiusura delle causali.
        /// </summary>
        /// <param name="regs">Le registrazioni da prendere in carico in processo da parte dell'elaborate.</param>
        /// <param name="saveChanges">se impostato a <c>true</c> salva le modifiche apportate al database.</param>
        /// <returns>L'elenco di registrazioni senza le cancellate (quelle generate automaticamente a chiusura delle causali)</returns>
        private ICollection<Reg> DeleteAllActivitiesAutoClosures(ICollection<Reg> regs, bool saveChanges)
        {
            // inzializzazione del valore di ritorno del metodo
            ICollection<Reg> returnRegs = regs;

            // si procede solamente se il modulo delle attività risulta correnttamente attivato
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Att || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosures) == 1 || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresFirstLast) == 1 || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresEnum) == 1 || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresXMinuteEnum) == 1)
            {
                // se sono presenti delle registrazioni provenienti da causali nell'elenco passato come parametro
                if (regs.Any(reg => reg.Custom_Data_Reg == Common.Properties.Settings.Default.ActivityAutoClosureCustomData))
                {
                    // si tolgono tutti i riferimenti alle registrazioni da cancellare dalle registrazioni ad esse abbinate per evitare errori di integrità
                    // referenziale
                    IEnumerable<Reg> regsToDelete = regs.Where(reg => reg.Custom_Data_Reg == Common.Properties.Settings.Default.ActivityAutoClosureCustomData).ToList();
                    var regsToUpdate = new List<Reg>();
                    regsToDelete.ForEach(reg => regsToUpdate.AddRange(Find(dbReg => dbReg.RiferimentoRRN_Reg == reg.Reg_Id || dbReg.RiferimentoRRN_Att == reg.Reg_Id)));
                    regsToUpdate.ForEach(reg => { reg.RiferimentoRRN_Reg = null; reg.RiferimentoRRN_Att = null;});
                    Context.BulkUpdate(regsToUpdate);

                    // si elminano da database tutte le registraizoni provenienti da causali presenti nell'elenco passato come parametro
                    Context.BulkDelete(regs.Where(reg => reg.Custom_Data_Reg == Common.Properties.Settings.Default.ActivityAutoClosureCustomData));

                    // inoltre alla lista passata come parametro si procede a togliere 
                    // le registrazioni cancellate dal database
                    returnRegs = regs.Where(reg => reg.Custom_Data_Reg != Common.Properties.Settings.Default.ActivityAutoClosureCustomData).ToList();
                }
            }            

            // ritorno del valore calcolato dal metodo
            return returnRegs;
        }

        /// <summary>
        /// Elimina dalle registrazioni attuali e dal database tutte le registrazioni generate automaticamente a chiusura delle causali.
        /// </summary>
        /// <param name="regs">Le registrazioni da prendere in carico in processo da parte dell'elaborate.</param>
        /// <param name="saveChanges">se impostato a <c>true</c> salva le modifiche apportate al database.</param>
        /// <returns>L'elenco di registrazioni senza le cancellate (quelle generate automaticamente a chiusura delle causali)</returns>
        private ICollection<Reg> DeleteCopertureSerali(ICollection<Reg> regs, bool saveChanges)
        {
            // inzializzazione del valore di ritorno del metodo
            ICollection<Reg> returnRegs = regs;
            // se sono presenti delle registrazioni provenienti da causali nell'elenco passato come parametro
            if (regs.Any(reg => reg.Turno == "Coperture Serali"))
            {
                // si tolgono tutti i riferimenti alle registrazioni da cancellare dalle registrazioni ad esse abbinate per evitare errori di integrità
                // referenziale
                IEnumerable<Reg> regsToDelete = regs.Where(reg => reg.Turno == "Coperture Serali").ToList();
                var regsToUpdate = new List<Reg>();
                //regsToDelete.ForEach(reg => regsToUpdate.AddRange(Find(dbReg => dbReg.RiferimentoRRN_Reg == reg.Reg_Id || dbReg.RiferimentoRRN_Att == reg.Reg_Id)));
                //regsToUpdate.ForEach(reg => { reg.RiferimentoRRN_Reg = null; reg.RiferimentoRRN_Att = null; reg.Registrazione_Stato_Reg = 0; });
                //Context.BulkUpdate(regsToUpdate);

                // si elminano da database tutte le registraizoni provenienti da causali presenti nell'elenco passato come parametro
                Context.BulkDelete(regs.Where(reg => reg.Turno == "Coperture Serali" && (reg.Registrazione_Tipo_Reg == 10 || reg.Registrazione_Tipo_Reg == 8)));

                // inoltre alla lista passata come parametro si procede a togliere 
                // le registrazioni cancellate dal database
                returnRegs = regs.Where(reg => !(reg.Turno == "Coperture Serali" && (reg.Registrazione_Tipo_Reg == 10 || reg.Registrazione_Tipo_Reg == 8))).ToList();
            }

            // ritorno del valore calcolato dal metodo
            return returnRegs;
        }

        /// <summary>
        /// Gestisce la generazione delle chiusure automatiche delle causali O delle registrazioni per le registrazioni nell'elenco specificato.
        /// </summary>
        /// <param name="regs">Le registrazioni in carico all'elaborate su cui effettuare le auto chiusure.</param>
        private void ManageActivitiesAutoClosures(ref ICollection<Reg> regs, ApplicationMessageEnum mode,DateTime from, DateTime to)
        {
            // inizializzazione dell'elenco di registrazione nuove da aggiungere alle attualmente da processare
            var closures = new List<Reg>();
            string tmpCoupleCode = "";

            #region CHIUSURA AUTOMATICA CAUSALI
            // si procede ad effettuare le operazioni di generazione della chiusura automatica delle causali
            // solo se il modulo attività risulta abilitato
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Att)
            {
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
                                                Reg newReg = Init();
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

                    // marcatura per la cancellazione delle eventuali attività di chiusura trovate sia dall'elenco da elaboarare che da database
                    if (activitiesToDelete.Any())
                    {
                        foreach (Reg reg in activitiesToDelete)
                        {
                            regs.Remove(reg);
                            reg.Custom_Data_Reg = Settings.Default.ActivityMarkedForDeletionCustomData;
                        }
                        Update(activitiesToDelete, true);
                    }

                    // se sono state generate delle registrazioni di confronto
                    // allora le si aggiungono alle registrazioni dopo averle salvate a database come le altre (utilizzando il codice di accoppiamento temporaneo precedentemente utilizzato)
                    Add(closures, true);
                    closures = Find(reg => reg.Codice_Accoppiamento == Common.Properties.Settings.Default.ActivityAutoClosureCustomData).ToList();
                    closures.ForEach(reg => reg.Codice_Accoppiamento = null);
                    regs.AddRange(closures);
                }
                if (closures.Count != 0)
                {
                    //le registrazioni che effettuano una chiusura vengono aggiunte al database
                    Add(closures, true);
                    //vengono estratte dal db solo le registrazioni di chiusura e il codice di accopiamento viene messo a null
                    closures = Find(reg => reg.Codice_Accoppiamento == tmpCoupleCode).ToList();
                    closures.ForEach(reg => reg.Codice_Accoppiamento = null);

                    //vengonoa aggiunte le registrazioni di chiusura nella lista delle registrazioni da processare.
                    regs.AddRange(closures);
                }
            }
            #endregion

            #region CHIUSURA AUTOMATICA REGISTRAIZONI


            #region CHIUSURA AUTOMATICA SUL CANTIERE SEDE
            // Se è attiva la personalizzazione che prevede  la chiusura automatica sul cantiere sede
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresEnum) == (int)AutoClosuresEnum.Sede && mode == Common.ApplicationMessageEnum.Elaborate)
            {
                closures = new List<Reg>();

                //vengono recuperate solo le registrazioni no passaggi, viaggi, attività...
                IEnumerable<Reg> regsToClose = regs.Where(reg => reg.Registrazione_Tipo_Reg == 0 && (reg.Registrazione_Data_Ora_Fis_Reg > from && reg.Registrazione_Data_Ora_Fis_Reg < to)).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                tmpCoupleCode = "ActivityAutoClosure";
                //string autoGeneratedDataStart = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "AutoGeneratedRegCustomData");


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
                    if (regByCol.Key != null) {
                        var a = regByCol.Key.Value;

                        List<Col> collaboratori = RepoManager.ColRepo.GetAllQueryable().Where(c => c.Col_Id == a).ToList();

                        int day = a;

                        Col collaboratore = collaboratori.First();

                        if (collaboratore.Raggruppamento1_Col != null)
                        {
                            if (collaboratore.Raggruppamento1_Col.Equals("1"))
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
                                                                Reg newReg = Init();

                                                                //duplicazione delle reg passate come parametro
                                                                CommonService.DuplicateEntity(currentReg, newReg);
                                                                newReg.RiferimentoRRN_Reg = null;
                                                                newReg.Col = currentReg.Col;
                                                                newReg.Reg_Id = 0;
                                                                newReg.Pru_Id = currentReg.Pru_Id;
                                                                newReg.Fru_Id = currentReg.Fru_Id;
                                                                newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month,
                                                                    currentReg.Registrazione_Data_Ora_Fis_Reg.Day, currentReg.Registrazione_Data_Ora_Fis_Reg.Hour, currentReg.Registrazione_Data_Ora_Fis_Reg.Minute, currentReg.Registrazione_Data_Ora_Fis_Reg.Second + 1);
                                                                newReg.Data_Registrazione_Reg = DateTime.Now;
                                                                newReg.Codice_Accoppiamento = tmpCoupleCode; // inserisco nella registrazione un codice accoppiamento fittizio per poi recuperarle dopo l'inserimento a db
                                                                newReg.Custom_Data_Reg = tmpCoupleCode;
                                                                newReg.Flag_EU_Reg = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? "U" : null;
                                                                newReg.Cant = currentReg.Cant;

                                                                // aggiunta della registrazione generata all'elenco
                                                                closures.Add(newReg);

                                                            }

                                                            else
                                                            {
                                                                doNotClose = false;
                                                            }
                                                        }
                                                        //se il cantiere successivo è sede e anche quello corrente è sede allora non è necessaria fare la chiusura
                                                        else
                                                        {
                                                            doNotClose = !doNotClose;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        doNotClose = false;
                                                    }
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
                                                        Reg newReg = Init();
                                                        //duplicazione delle reg passate come parametro
                                                        CommonService.DuplicateEntity(currentReg, newReg);
                                                        newReg.Reg_Id = 0;
                                                        //data ore uguali alla reg precedente +59 secondi
                                                        newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month,
                                                            currentReg.Registrazione_Data_Ora_Fis_Reg.Day, currentReg.Registrazione_Data_Ora_Fis_Reg.Hour, currentReg.Registrazione_Data_Ora_Fis_Reg.Minute, currentReg.Registrazione_Data_Ora_Fis_Reg.Second + 1);
                                                        newReg.Data_Registrazione_Reg = DateTime.Now;
                                                        newReg.Codice_Accoppiamento = tmpCoupleCode; // inserisco nella registrazione un codice accoppiamento fittizio per poi recuperarle dopo l'inserimento a db
                                                        newReg.Custom_Data_Reg = tmpCoupleCode;
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
                        }
                    }
                }
                if (closures.Count != 0)
                {
                    //le registrazioni che effettuano una chiusura vengono aggiunte al database
                    Add(closures, true);
                    //vengono estratte dal db solo le registrazioni di chiusura e il codice di accopiamento viene messo a null
                    closures = Find(reg => reg.Codice_Accoppiamento == tmpCoupleCode).ToList();
                    closures.ForEach(reg => reg.Codice_Accoppiamento = null);

                    //vengonoa aggiunte le registrazioni di chiusura nella lista delle registrazioni da processare.
                    regs.AddRange(closures);
                }
                

            

            

            }
            #endregion

            #region CHIUSURA AUTOMATICA DELLA PRIMA E DELL'ULTIMA REG DEL GIORNO

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresFirstLast) == 1 && mode == Common.ApplicationMessageEnum.Elaborate)
            {

                // inizializzazione dell'elenco di registrazione nuove da aggiungere alle attualmente da processare
                closures = new List<Reg>();

                //vengono recuperate solo le registrazioni no passaggi, viaggi, attività...
                IEnumerable<Reg> regsToClose = RepoManager.RegRepo.Find(reg => reg.Registrazione_Tipo_Reg == 0 && (reg.Registrazione_Data_Ora_Fis_Reg > from && reg.Registrazione_Data_Ora_Fis_Reg < to)).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();
                
                tmpCoupleCode = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "TmpCoupleCode");
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
                    var a = regByCol.Key;

                    if (a != null) {
                        List<Col> collaboratori = RepoManager.ColRepo.GetAllQueryable().Where(c => c.Col_Id == a).ToList();

                        Col collaboratore = collaboratori.First();

                        int day = 0;
                        if (collaboratore.Raggruppamento1_Col != null)
                        {
                            if (collaboratore.Raggruppamento1_Col.Equals("1"))
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
                                            // recupero dell'id del cantiere della registrazione precedente e successiva alla causale in processo

                                            int currentRegCantId = currentReg.Cant_Id ?? 0;

                                            Cant nextRegCant = null;

                                            Cant currentRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == currentRegCantId);
                                            int dayAfter = 0;
                                            if (nextReg != null)
                                            {
                                                int nextRegCantId = nextReg.Cant_Id ?? 0;
                                                // recupero dei cantieri della registrazione precedente e successiva alla causale in processo
                                                nextRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == nextRegCantId);
                                                dayAfter = nextReg.Registrazione_Data_Ora_Fis_Reg.Day;
                                            }
                                            else
                                            {
                                                nextRegCant = currentRegCant;
                                                dayAfter = day + 1;
                                            }
                                            //se il cantiere attuale è una sede e il successivo no viene effettuata una chiususra
                                            if ((nextRegCant != default(Cant) && currentRegCant != default(Cant)))
                                            {
                                                //se è richiesta la chiusura
                                                if (!doNotClose)
                                                {
                                                    //vado a controllare se è la prima o l'ultima registrazione del giorno e se non è accoppiata con nessuna registrazione
                                                    if ((day < currentReg.Registrazione_Data_Ora_Fis_Reg.Day || currentReg.Registrazione_Data_Ora_Fis_Reg.Day != dayAfter) && currentReg.Custom_Data_Reg != "ActivityAutoClosure" && currentReg.Registrazione_Stato_Reg != 1)
                                                    {
                                                        // generazione di una nuova reg a chiusura con i dati d'entrata tranne l'uscita
                                                        Reg newReg = Init();
                                                        //duplicazione delle reg passate come parametro
                                                        CommonService.DuplicateEntity(currentReg, newReg);
                                                        newReg.Reg_Id = 0;
                                                        newReg.Cant_Id = currentReg.Cant_Id.Value;
                                                        newReg.Col_Id = currentReg.Col_Id.Value;
                                                        newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month,
                                                            currentReg.Registrazione_Data_Ora_Fis_Reg.Day, currentReg.Registrazione_Data_Ora_Fis_Reg.Hour, currentReg.Registrazione_Data_Ora_Fis_Reg.Minute, currentReg.Registrazione_Data_Ora_Fis_Reg.Second + 1);
                                                        newReg.Data_Registrazione_Reg = DateTime.Now;
                                                        newReg.Codice_Accoppiamento = tmpCoupleCode; // inserisco nella registrazione un codice accoppiamento fittizio per poi recuperarle dopo l'inserimento a db
                                                        newReg.Custom_Data_Reg = autoGeneratedDataStart;
                                                        newReg.Flag_EU_Reg = "";
                                                        newReg.Cant = currentReg.Cant;
                                                        //imposto una stringa per capire in fase di eliminazione quali timbrature sono autochiusure
                                                        newReg.Custom_Data_Reg = "ActivityAutoClosure";
                                                        newReg.Registrazione_Badge_Originale = currentReg.Registrazione_Badge_Originale;
                                                        newReg.ParentReg = currentReg;

                                                        // aggiunta della registrazione generata all'elenco
                                                        closures.Add(newReg);
                                                    }
                                                    day = currentReg.Registrazione_Data_Ora_Fis_Reg.Day;
                                                }

                                                else
                                                {
                                                    doNotClose = false;
                                                }
                                            }
                                            else
                                            {

                                                // recupero dell'id del cantiere della registrazione successiva alla timbrature processata
                                                int currentCantId = currentReg.Cant_Id ?? 0;

                                                //viene recuperato il cantiere corrispondente all'unica registrazione presente
                                                currentRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == currentCantId);

                                                // si procede alla generazione della chiusura solamente se i cantieri della registrazione attuale è una sede e la successiva no
                                                if (currentRegCant != default(Cant))
                                                {

                                                    //se si tratta di un cantiere tipo sede
                                                    if ((day < currentReg.Registrazione_Data_Ora_Fis_Reg.Day || currentReg.Registrazione_Data_Ora_Fis_Reg.Day != dayAfter) && currentReg.Custom_Data_Reg != "ActivityAutoClosure" && currentReg.Registrazione_Stato_Reg != 1)
                                                    {
                                                        // generazione di una nuova reg a chiusura con i dati d'entrata tranne l'uscita
                                                        Reg newReg = Init();
                                                        //duplicazione delle reg passate come parametro
                                                        CommonService.DuplicateEntity(currentReg, newReg);
                                                        newReg.Reg_Id = 0;
                                                        newReg.Cant_Id = currentReg.Cant_Id.Value;
                                                        newReg.Col_Id = currentReg.Col_Id.Value;
                                                        newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month,
                                                            currentReg.Registrazione_Data_Ora_Fis_Reg.Day, currentReg.Registrazione_Data_Ora_Fis_Reg.Hour, currentReg.Registrazione_Data_Ora_Fis_Reg.Minute, currentReg.Registrazione_Data_Ora_Fis_Reg.Second + 1);
                                                        newReg.Data_Registrazione_Reg = DateTime.Now;
                                                        newReg.Codice_Accoppiamento = tmpCoupleCode; // inserisco nella registrazione un codice accoppiamento fittizio per poi recuperarle dopo l'inserimento a db
                                                        newReg.Custom_Data_Reg = autoGeneratedDataStart;
                                                        newReg.Flag_EU_Reg = "";
                                                        newReg.Cant = currentReg.Cant;
                                                        newReg.Custom_Data_Reg = "ActivityAutoClosure";
                                                        newReg.ParentReg = currentReg;

                                                        // aggiunta della registrazione generata all'elenco
                                                        closures.Add(newReg);

                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
            }

                if (closures.Count != 0)
                {
                    //le registrazioni che effettuano una chiusura vengono aggiunte al database
                    Add(closures, true);
                    //vengono estratte dal db solo le registrazioni di chiusura e il codice di accopiamento viene messo a null
                    closures = Find(reg => reg.Codice_Accoppiamento == tmpCoupleCode).ToList();
                    closures.ForEach(reg => reg.Codice_Accoppiamento = null);

                    //vengonoa aggiunte le registrazioni di chiusura nella lista delle registrazioni da processare.
                    regs.AddRange(closures);
                }

            }
            #endregion

            #region CHIUSURA AUTOMATICA DI TUTTE LE REG

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosures) == 1 && mode == Common.ApplicationMessageEnum.Elaborate)
            {
                int minuti = int.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutoClosures, "minuti"));
                // inizializzazione dell'elenco di registrazione nuove da aggiungere alle attualmente da processare
                closures = new List<Reg>();

                //vengono recuperate solo le registrazioni no passaggi, viaggi, attività...
                IEnumerable<Reg> regsToClose = RepoManager.RegRepo.Find(reg => reg.Registrazione_Tipo_Reg == 0 && (reg.Registrazione_Data_Ora_Fis_Reg > from && reg.Registrazione_Data_Ora_Fis_Reg < to)).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                tmpCoupleCode = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "TmpCoupleCode");
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
                    var a = regByCol.Key;

                      if (a != null)
                      {
                          List<Col> collaboratori = RepoManager.ColRepo.GetAllQueryable().Where(c => c.Col_Id == a).ToList();

                          Col collaboratore = collaboratori.First();

                          int day = 0;
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
                                  // recupero dell'id del cantiere della registrazione precedente e successiva alla causale in processo

                                  int currentRegCantId = currentReg.Cant_Id ?? 0;

                                  Cant nextRegCant = null;

                                  Cant currentRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == currentRegCantId);
                                  int dayAfter = 0;
                                  if (nextReg != null)
                                  {
                                      int nextRegCantId = nextReg.Cant_Id ?? 0;
                                      // recupero dei cantieri della registrazione precedente e successiva alla causale in processo
                                      nextRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == nextRegCantId);
                                      dayAfter = nextReg.Registrazione_Data_Ora_Fis_Reg.Day;
                                  }
                                  else
                                  {
                                      nextRegCant = currentRegCant;
                                      dayAfter = day + 1;
                                  }
                                  //se il cantiere attuale è una sede e il successivo no viene effettuata una chiususra
                                  if ((nextRegCant != default(Cant) && currentRegCant != default(Cant)))
                                  {
                                      //se è richiesta la chiusura
                                      if (!doNotClose)
                                      {
                                            if (currentReg.Registrazione_Stato_Reg != 1) {
                                                // generazione di una nuova reg a chiusura con i dati d'entrata tranne l'uscita
                                                Reg newReg = Init();
                                                //duplicazione delle reg passate come parametro
                                                CommonService.DuplicateEntity(currentReg, newReg);
                                                newReg.Reg_Id = 0;
                                                newReg.Cant_Id = currentReg.Cant_Id.Value;
                                                newReg.Col_Id = currentReg.Col_Id.Value;
                                                newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month,
                                                    currentReg.Registrazione_Data_Ora_Fis_Reg.Day, currentReg.Registrazione_Data_Ora_Fis_Reg.Hour, currentReg.Registrazione_Data_Ora_Fis_Reg.Minute, currentReg.Registrazione_Data_Ora_Fis_Reg.Second + 1);
                                                newReg.Data_Registrazione_Reg = DateTime.Now;
                                                newReg.Codice_Accoppiamento = tmpCoupleCode; // inserisco nella registrazione un codice accoppiamento fittizio per poi recuperarle dopo l'inserimento a db
                                                newReg.Custom_Data_Reg = autoGeneratedDataStart;
                                                newReg.Flag_EU_Reg = "";
                                                newReg.Cant = currentReg.Cant;
                                                //imposto una stringa per capire in fase di eliminazione quali timbrature sono autochiusure
                                                newReg.Custom_Data_Reg = "ActivityAutoClosure";
                                                newReg.Registrazione_Badge_Originale = currentReg.Registrazione_Badge_Originale;
                                                newReg.ParentReg = currentReg;

                                                // aggiunta della registrazione generata all'elenco
                                                closures.Add(newReg);

                                                day = currentReg.Registrazione_Data_Ora_Fis_Reg.Day;
                                            }
                                      }

                                      else
                                      {
                                          doNotClose = false;
                                      }
                                  }
                                  else
                                  {

                                      // recupero dell'id del cantiere della registrazione successiva alla timbrature processata
                                      int currentCantId = currentReg.Cant_Id ?? 0;

                                      //viene recuperato il cantiere corrispondente all'unica registrazione presente
                                      currentRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == currentCantId);

                                      // si procede alla generazione della chiusura solamente se i cantieri della registrazione attuale è una sede e la successiva no
                                      if (currentRegCant != default(Cant))
                                      {
                                            if (currentReg.Registrazione_Stato_Reg != 1) {
                                                // generazione di una nuova reg a chiusura con i dati d'entrata tranne l'uscita
                                                Reg newReg = Init();
                                                //duplicazione delle reg passate come parametro
                                                CommonService.DuplicateEntity(currentReg, newReg);
                                                newReg.Reg_Id = 0;
                                                newReg.Cant_Id = currentReg.Cant_Id.Value;
                                                newReg.Col_Id = currentReg.Col_Id.Value;
                                                newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month,
                                                    currentReg.Registrazione_Data_Ora_Fis_Reg.Day, currentReg.Registrazione_Data_Ora_Fis_Reg.Hour, currentReg.Registrazione_Data_Ora_Fis_Reg.Minute, currentReg.Registrazione_Data_Ora_Fis_Reg.Second + 1);
                                                newReg.Data_Registrazione_Reg = DateTime.Now;
                                                newReg.Codice_Accoppiamento = tmpCoupleCode; // inserisco nella registrazione un codice accoppiamento fittizio per poi recuperarle dopo l'inserimento a db
                                                newReg.Custom_Data_Reg = autoGeneratedDataStart;
                                                newReg.Flag_EU_Reg = "";
                                                newReg.Cant = currentReg.Cant;
                                                newReg.Custom_Data_Reg = "ActivityAutoClosure";
                                                newReg.ParentReg = currentReg;

                                                // aggiunta della registrazione generata all'elenco
                                                closures.Add(newReg);
                                            }
                                              
                                           
                                      }
                                  }
                              }
                          }
                      }
                    }
                }
                if (closures.Count != 0)
                {
                    //le registrazioni che effettuano una chiusura vengono aggiunte al database
                    Add(closures, true);
                    //vengono estratte dal db solo le registrazioni di chiusura e il codice di accopiamento viene messo a null
                    closures = Find(reg => reg.Codice_Accoppiamento == tmpCoupleCode).ToList();
                    closures.ForEach(reg => reg.Codice_Accoppiamento = null);

                    //vengonoa aggiunte le registrazioni di chiusura nella lista delle registrazioni da processare.
                    regs.AddRange(closures);
                }

            }



            #endregion

            #region CHIUSURA AUTOMATICA DI X MINUTI
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresXMinuteEnum) == 1 && mode == Common.ApplicationMessageEnum.Elaborate)
            {
                // inizializzazione dell'elenco di registrazione nuove da aggiungere alle attualmente da processare
                closures = new List<Reg>();

                //vengono recuperate solo le registrazioni no passaggi, viaggi, attività...
                IEnumerable<Reg> regsToClose = RepoManager.RegRepo.Find(reg => reg.Registrazione_Tipo_Reg == 0 && (reg.Registrazione_Data_Ora_Fis_Reg > from && reg.Registrazione_Data_Ora_Fis_Reg < to)).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                tmpCoupleCode = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "TmpCoupleCode");
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
                    var a = regByCol.Key;

                    if (a != null)
                    {
                        List<Col> collaboratori = RepoManager.ColRepo.GetAllQueryable().Where(c => c.Col_Id == a).ToList();

                        Col collaboratore = collaboratori.First();

                        int day = 0;
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
                                    // recupero dell'id del cantiere della registrazione precedente e successiva alla causale in processo

                                    int currentRegCantId = currentReg.Cant_Id ?? 0;

                                    Cant nextRegCant = null;

                                    Cant currentRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == currentRegCantId);
                                    int dayAfter = 0;
                                    if (nextReg != null)
                                    {
                                        int nextRegCantId = nextReg.Cant_Id ?? 0;
                                        // recupero dei cantieri della registrazione precedente e successiva alla causale in processo
                                        nextRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == nextRegCantId);
                                        dayAfter = nextReg.Registrazione_Data_Ora_Fis_Reg.Day;
                                    }
                                    else
                                    {
                                        nextRegCant = currentRegCant;
                                        dayAfter = day + 1;
                                    }
                                    //se il cantiere attuale è una sede e il successivo no viene effettuata una chiususra
                                    if ((nextRegCant != default(Cant) && currentRegCant != default(Cant)))
                                    {
                                        //se è richiesta la chiusura
                                        if (!doNotClose)
                                        {
                                            if (currentReg.Registrazione_Stato_Reg != 1 && currentRegCant.Importo6 != null)
                                            {
                                                if (currentRegCant.Importo6.Value > 0) {
                                                    DateTime dataReg = currentReg.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(currentRegCant.Importo6.Value);
                                                    // generazione di una nuova reg a chiusura con i dati d'entrata tranne l'uscita
                                                    Reg newReg = Init();
                                                    //duplicazione delle reg passate come parametro
                                                    CommonService.DuplicateEntity(currentReg, newReg);
                                                    newReg.Reg_Id = 0;
                                                    newReg.Cant_Id = currentReg.Cant_Id.Value;
                                                    newReg.Col_Id = currentReg.Col_Id.Value;
                                                    newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(dataReg.Year, dataReg.Month,
                                                        dataReg.Day, dataReg.Hour, dataReg.Minute, dataReg.Second);
                                                    newReg.Data_Registrazione_Reg = DateTime.Now;
                                                    newReg.Codice_Accoppiamento = tmpCoupleCode; // inserisco nella registrazione un codice accoppiamento fittizio per poi recuperarle dopo l'inserimento a db
                                                    newReg.Custom_Data_Reg = autoGeneratedDataStart;
                                                    newReg.Flag_EU_Reg = "";
                                                    newReg.Cant = currentReg.Cant;
                                                    //imposto una stringa per capire in fase di eliminazione quali timbrature sono autochiusure
                                                    newReg.Custom_Data_Reg = "ActivityAutoClosure";
                                                    newReg.Registrazione_Badge_Originale = currentReg.Registrazione_Badge_Originale;
                                                    newReg.ParentReg = currentReg;

                                                    // aggiunta della registrazione generata all'elenco
                                                    closures.Add(newReg);

                                                    day = currentReg.Registrazione_Data_Ora_Fis_Reg.Day;
                                                }     
                                            }
                                        }

                                        else
                                        {
                                            doNotClose = false;
                                        }
                                    }
                                    else
                                    {

                                        // recupero dell'id del cantiere della registrazione successiva alla timbrature processata
                                        int currentCantId = currentReg.Cant_Id ?? 0;

                                        //viene recuperato il cantiere corrispondente all'unica registrazione presente
                                        currentRegCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == currentCantId);

                                        // si procede alla generazione della chiusura solamente se i cantieri della registrazione attuale è una sede e la successiva no
                                        if (currentRegCant != default(Cant))
                                        {
                                            if (currentReg.Registrazione_Stato_Reg != 1 && currentRegCant.Importo3 != null)
                                            {
                                                if (currentRegCant.Importo3.Value > 0)
                                                {
                                                    DateTime dataReg = currentReg.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(currentRegCant.Importo3.Value);
                                                    // generazione di una nuova reg a chiusura con i dati d'entrata tranne l'uscita
                                                    Reg newReg = Init();
                                                    //duplicazione delle reg passate come parametro
                                                    CommonService.DuplicateEntity(currentReg, newReg);
                                                    newReg.Reg_Id = 0;
                                                    newReg.Cant_Id = currentReg.Cant_Id.Value;
                                                    newReg.Col_Id = currentReg.Col_Id.Value;
                                                    newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(dataReg.Year, dataReg.Month,
                                                        dataReg.Day, dataReg.Hour, dataReg.Minute, dataReg.Second);
                                                    newReg.Data_Registrazione_Reg = DateTime.Now;
                                                    newReg.Codice_Accoppiamento = tmpCoupleCode; // inserisco nella registrazione un codice accoppiamento fittizio per poi recuperarle dopo l'inserimento a db
                                                    newReg.Custom_Data_Reg = autoGeneratedDataStart;
                                                    newReg.Flag_EU_Reg = "";
                                                    newReg.Cant = currentReg.Cant;
                                                    newReg.Custom_Data_Reg = "ActivityAutoClosure";
                                                    newReg.ParentReg = currentReg;

                                                    // aggiunta della registrazione generata all'elenco
                                                    closures.Add(newReg);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (closures.Count != 0)
                {
                    //le registrazioni che effettuano una chiusura vengono aggiunte al database
                    Add(closures, true);
                    //vengono estratte dal db solo le registrazioni di chiusura e il codice di accopiamento viene messo a null
                    closures = Find(reg => reg.Codice_Accoppiamento == tmpCoupleCode).ToList();
                    closures.ForEach(reg => reg.Codice_Accoppiamento = null);

                    //vengonoa aggiunte le registrazioni di chiusura nella lista delle registrazioni da processare.
                    regs.AddRange(closures);
                }

            }
            #endregion

        }

        /// <summary>
        /// Effettua la cancellazione delle regitrazioni attività tappo marcate per la cancellazione dalle precedenti funzion di gestione.
        /// </summary>
        private void DeleteAllActivitiesMarkedForDeletion()
        {

            // si procede ad effettuare la modifica solamente se il modulo delle attività è abilitato
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Att)
            {
                // sono recuperate tutte le registrazioni marcate per la cancellazione
                DbSet.Where(reg => reg.Custom_Data_Reg == Settings.Default.ActivityMarkedForDeletionCustomData).DeleteFromQuery();
                //IEnumerable<Reg> regsToDelete = Find(reg => reg.Custom_Data_Reg == Settings.Default.ActivityMarkedForDeletionCustomData).ToList();

                // cancellazione delle registrazioni recuperate, se presenti
                //if (regsToDelete.Any())
                //{
                //    Delete(regsToDelete);
                //    SaveChanges();
                //}
            }

        }

        #endregion

        #region Custom Elaborate Regs

        /// <summary>
        /// Gestisce le operazioni di elaborate custom prima dell'elaborazione standard.
        /// </summary>
        /// <param name="regs">Le registrazioni da processare.</param>
        /// <param name="isToSaveChanges">Se impostato a <c>true</c> indica che è necessario aggiornare il database.</param>
        /// <returns>L'elenco degli errori da successivamente inserire nella tabella messaggi.</returns>
        private IEnumerable<KeyValuePair<string, string>> ManagePreCustomElaborateRegs(ICollection<Reg> regs, bool isToSaveChanges)
        {
            return ExecuteCustomElaborateRegs(regs, isToSaveChanges, CustomElaborateRegsTypeEnum.Pre);
        }

        /// <summary>
        /// Gestisce le operazioni di elaborate custom dopo l'elaborazione standard.
        /// </summary>
        /// <param name="regs">Le registrazioni da processare.</param>
        /// <param name="isToSaveChanges">Se impostato a <c>true</c> indica che è necessario aggiornare il database.</param>
        /// <returns>L'elenco degli errori da successivamente inserire nella tabella messaggi.</returns>
        private IEnumerable<KeyValuePair<string, string>> ManagePostPruFruCustomElaborateRegs(ICollection<Reg> regs, bool isToSaveChanges)
        {
            return ExecuteCustomElaborateRegs(regs, isToSaveChanges, CustomElaborateRegsTypeEnum.PostPruFru);
        }

        /// <summary>
        /// Gestisce le operazioni di elaborate custom dopo l'elaborazione standard.
        /// </summary>
        /// <param name="regs">Le registrazioni da processare.</param>
        /// <param name="isToSaveChanges">Se impostato a <c>true</c> indica che è necessario aggiornare il database.</param>
        /// <returns>L'elenco degli errori da successivamente inserire nella tabella messaggi.</returns>
        private IEnumerable<KeyValuePair<string, string>> ManagePostElaborateRegs(ICollection<Reg> regs, bool isToSaveChanges)
        {
            return ExecuteCustomElaborateRegs(regs, isToSaveChanges, CustomElaborateRegsTypeEnum.PostElaborate);
        }


        /// <summary>
        /// Esegue l'eventuale elaborate custom utilizzando i dati specificati.
        /// </summary>
        /// <param name="regs">Le registrazioni da processare.</param>
        /// <param name="isToSaveChanges">Se impostato a <c>true</c> indica che è necessario aggiornare il database.</param>
        /// <param name="customElaborate">Il tipo di custom elaborate da processare.</param>
        /// <returns>L'elenco dei messaggi d'errore eventualmente ottenuti in fase di elaborate custom.</returns>
        private IEnumerable<KeyValuePair<string, string>> ExecuteCustomElaborateRegs(ICollection<Reg> regs, bool isToSaveChanges, CustomElaborateRegsTypeEnum customElaborate)
        {
            // inizializzazione dei messaggi di ritorno del metodo
            var returnMessages = new List<KeyValuePair<string, string>>();

            // si procede con l'elaborazione solamente se è la customizzazione delle elaborazioni custom non indica lo standard
            var customizationVersion = (CustomElaborateRegs)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomElaborateRegs);
            if (customizationVersion != CustomElaborateRegs.Standard)
            {
                switch (customizationVersion)
                {
                    case CustomElaborateRegs.Dugoni:
                        switch (customElaborate)
                        {
                            case CustomElaborateRegsTypeEnum.Pre:
                                returnMessages = ExecuteDugoniPreCustomElaborateRegs(regs, isToSaveChanges);
                                break;
                            case CustomElaborateRegsTypeEnum.PostPruFru:
                                returnMessages = ExecuteDugoniPostCustomElaborateRegs(regs, isToSaveChanges);
                                break;
                            default:
                                returnMessages.Add(new KeyValuePair<string, string>(FunctionMessageEnum.CustomElaboratePre.ToString(), BusinessService.GetLocalizedString(PowerWebResources.ERR_CUSTOM_ELABORATE_NON_VALIDA)));
                                break;
                        }
                        break;

                    case CustomElaborateRegs.AppWithActivity:
                        switch (customElaborate)
                        {
                            case CustomElaborateRegsTypeEnum.PostElaborate:
                                returnMessages = ExecuteMiorelliPostCustomElaborateRegs(regs);
                                break;

                            default:
                                returnMessages.Add(new KeyValuePair<string, string>(FunctionMessageEnum.CustomElaboratePre.ToString(), BusinessService.GetLocalizedString(PowerWebResources.ERR_CUSTOM_ELABORATE_NON_VALIDA)));
                                break;
                        }
                        break;


                    default:
                        returnMessages.Add(new KeyValuePair<string, string>(FunctionMessageEnum.CustomElaboratePre.ToString(), BusinessService.GetLocalizedString(PowerWebResources.ERR_CUSTOM_ELABORATE_NON_VALIDA)));
                        break;
                }
            }

            // ritorno dei messaggi calcolati dal metodo
            return returnMessages;
        }

        /// <summary>
        /// Esegue l'elaborate custom pre elaborazione standard per la personalizzazione del //Cliente Dugoni.
        /// Il metodo si occupa di prepararere le registrazioni per l'elaborazione standard. Affinché tutto funzioni correttamente è necessario ripristinare la situazione
        /// originale e quindi:
        /// 1. Le registrazioni marcate con inserimento automatico siano cancellate
        /// 2. Le registrazioni che hanno subito un cambio di cantiere devono ripristinare il cantiere originale
        /// </summary>
        /// <param name="regs">Le registrazioni da processare.</param>
        /// <param name="isToSaveChanges">Se impostato a <c>true</c> indica che è necessario aggiornare il database.</param>
        /// <returns>L'elenco dei messaggi d'errore eventualmente ottenuti in fase di elaborate custom.</returns>
        private List<KeyValuePair<string, string>> ExecuteDugoniPreCustomElaborateRegs(ICollection<Reg> regs, bool isToSaveChanges)
        {
            // inizializzazione dei messaggi di ritorno del metodo
            var returnMessages = new List<KeyValuePair<string, string>>();

            // sono recuperati i parametri che nella customizzazione indicano i tag utilizzati per marcare le timbrature generate
            // dall'elaborate custom stessa o che hanno subito dall'elaborate un cambio di cantiere
            string autoGeneratedDataStart = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "AutoGeneratedRegCustomData");
            string cantChangedDataStart = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "ChangedRegCantCustomData");

            // recupero delle reg da cancellare da database
            List<Reg> regsToDelete = regs.Where(reg => !String.IsNullOrEmpty(reg.Custom_Data_Reg) && reg.Custom_Data_Reg.StartsWith(autoGeneratedDataStart)).ToList();

            // dall'elenco delle registrazioni da processare sono tolte le registrazioni generate dall'elaborazione custom
            regsToDelete.ForEach(reg => regs.Remove(reg));

            regs = regs.Where(reg => String.IsNullOrEmpty(reg.Custom_Data_Reg) || !reg.Custom_Data_Reg.StartsWith(autoGeneratedDataStart)).ToList();

            // cancellazione delle registrazioni recuperate per la cancellazione
            // (se ce ne sono presenti)
            if (regsToDelete.Any())
                Delete(regsToDelete, isToSaveChanges);

            // recupero tutte le registrazioni che hanno cambiato cantiere
            List<Reg> regsWithCanghedCant = regs.Where(reg => !String.IsNullOrEmpty(reg.Custom_Data_Reg) && reg.Custom_Data_Reg.StartsWith(cantChangedDataStart)).ToList();

            if (regsWithCanghedCant.Any())
                regsWithCanghedCant.ForEach(reg =>
                {
                    reg.Cant_Id = Convert.ToInt32(reg.Custom_Data_Reg.Substring(reg.Custom_Data_Reg.IndexOf(cantChangedDataStart.Last()) + 1));
                    reg.Custom_Data_Reg = null;
                });

            // ritorno dei messaggi calcolati dal metodo
            return returnMessages;
        }

        /// <summary>
        /// Esegue l'elaborate custom post elaborazione per le app che prevedono attività che possono essere visualizzate anche quando è presente solo l'entrata
        /// della registrazione a cui sono associate.
        /// Il metodo si occupa di individuare la registrazione di tipo ore e di associare ad essa tutte le attività svolte dallo stesso collaboratore nella stessa data-ora-minuto
        /// </summary>
        /// <param name="regs">Le registrazioni da processare.</param>
        /// <param name="isToSaveChanges">Se impostato a <c>true</c> indica che è necessario aggiornare il database.</param>
        /// <returns>L'elenco dei messaggi d'errore eventualmente ottenuti in fase di elaborate custom.</returns>
        private List<KeyValuePair<string, string>> ExecuteMiorelliPostCustomElaborateRegs(ICollection<Reg> regs)
        {
            var returnMessages = new List<KeyValuePair<string, string>>();
            ICollection<Reg> regUpdated = regs;
            List<Reg> tempList = new List<Reg>();

            foreach (IGrouping<int?, Reg> regsByCol in regUpdated.Where(reg => reg.Col_Id != null && reg.Cant_Id != null).GroupBy(reg => reg.Col_Id))
            {
                foreach (IGrouping<DateTime, Reg> regsByColAndDate in regsByCol.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg.Date).GroupBy(reg => reg.Registrazione_Data_Ora_Fis_Reg.Date))
                {
                    foreach (IGrouping<int, Reg> regsByColAndDateHH in regsByColAndDate.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg.Hour).GroupBy(reg => reg.Registrazione_Data_Ora_Fis_Reg.Hour))
                    {
                        foreach (IGrouping<int, Reg> regsByColAndDateHHMM in regsByColAndDateHH.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg.Minute).GroupBy(reg => reg.Registrazione_Data_Ora_Fis_Reg.Minute))
                        {
                            //ordina per secondo le registrazioni del collaboratore nello stesso minuto. In questo modo la registrazione di tipo ore sarà 
                            //la prima (la app scrive prima la timbratura e poi le attività)
                            IEnumerable<Reg> orderRegBySecond = regsByColAndDateHHMM.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg.Second).ThenBy(reg => reg.Registrazione_Tipo_Reg);
                            bool isFirst = true;
                            int cantId = 0;
                            String flagEU = null;
                            int rrn = 0;
                            int statoReg = 0;

                            foreach (Reg reg in orderRegBySecond)
                            {

                                //se è il primo record del gruppo ed è di tipo ore, mi salvo i suoi campi per assegnarli poi alle attività
                                if (reg.Registrazione_Tipo_Reg == 0 && isFirst)
                                {
                                    cantId = (int)reg.Cant_Id;
                                    flagEU = reg.Flag_EU_Reg;
                                    rrn = reg.Reg_Id;
                                    statoReg = reg.Registrazione_Stato_Reg;
                                    isFirst = false;
                                }
                                else if (reg.Registrazione_Tipo_Reg == 2) //se è un'attività setto i campi per associarla alla timbratura corrispondente
                                {
                                    reg.Att_Id = cantId;
                                    reg.Flag_EU_Reg = flagEU;
                                    reg.RiferimentoRRN_Att = rrn;


                                    // Se la registrazione a cui si riferisce l'attività è abbinata e l'attività è stata timbrata in entrata
                                    // segnalo che non voglio vederla in visualizzazione
                                    if (statoReg == (int)RegStateEnum.Ass && reg.Flag_EU_Reg != null && reg.Flag_EU_Reg.Equals("E"))
                                    {
                                        reg.Stato_Attivita = (int)ActivityStatusEnum.NotVisible;
                                    }

                                    // Negli altri casi, segnalo che voglio vedere l'attività in visualizzazione
                                    else
                                    {
                                        reg.Stato_Attivita = null;
                                    }
                                    tempList.Add(reg);

                                }
                            }
                        }
                    }
                }
            }
            Update(tempList, false);


            return returnMessages;
        }

        /// <summary>
        /// Esegue l'elaborate custom post elaborazione standard per la personalizzazione del clietnte Dugoni.
        /// L'elaborazione per il //Cliente dugoni si occuperà di chiudere le registrazioni di linea e quelle dei cantieri veri e propri
        /// generando timbrature e modificando i cantieri delle stesse
        /// </summary>
        /// <param name="regs">Le registrazioni da processare.</param>
        /// <param name="isToSaveChanges">Se impostato a <c>true</c> indica che è necessario aggiornare il database.</param>
        /// <returns>L'elenco dei messaggi d'errore eventualmente ottenuti in fase di elaborazione custom</returns>
        private List<KeyValuePair<string, string>> ExecuteDugoniPostCustomElaborateRegs(ICollection<Reg> regs, bool isToSaveChanges)
        {
            // TODO: gestione del notturno
            // TODO: ottimizzazione del recupero del cantiere con una estensione?

            // inizializzazione dei messaggi di ritorno del metodo
            var returnMessages = new List<KeyValuePair<string, string>>();

            // inizializzazione della delle reg da aggiungere alla lista processata
            var regsToAdd = new List<Reg>();

            // sono recuperati i parametri che nella customizzazione indicano i tag utilizzati per marcare le timbrature generate
            // dall'elaborate custom stessa o che hanno subito dall'elaborate un cambio di cantiere e il tipo di cantiere linea
            string autoGeneratedDataStart = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "AutoGeneratedRegCustomData");
            string cantChangedDataStart = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "ChangedRegCantCustomData");
            string lineCantType = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "LineCantTypeCode");
            string tmpCoupleCode = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.CustomElaborateRegs, "TmpCoupleCode");

            // si processano le registrazioni raggruppate per collaboratore e giorno ed ordinate per data ora fisica
            // (sono processate in ogni caso solamente le registrazioni di tipo ora con valorizzato cantiere e collaboratore)
            foreach (IGrouping<int?, Reg> regsByCol in regs.Where(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None && reg.Col_Id != null && reg.Cant_Id != null)
                .GroupBy(reg => reg.Col_Id)) // ciclo raggruppato per collaboratore
            {
                // ciclo raggruppato per data
                foreach (IGrouping<DateTime, Reg> regsByColAndDate in regsByCol.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg.Date).GroupBy(reg => reg.Registrazione_Data_Ora_Fis_Reg.Date))
                {
                    // inizializzazione della variabile che indica l'incontro della prima registrazione di inzio del giorno
                    bool firstDayNotLineEncountered = false;

                    // inizializzazione della registrazione e del cantiere precedenti in registrazione
                    Reg prevReg = default(Reg);
                    Cant prevCant = default(Cant);

                    // inizializzazione della variabile che indica di aver già processato un inizio
                    bool firstNonLineProcessing = false;

                    // ciclo delle registrazioni ordinate per data/ora fisica
                    List<Reg> dayRegs = regsByColAndDate.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();
                    foreach (Reg reg in dayRegs)
                    {
                        // si processano solamente giorni che contengono dei cantieri linea
                        if (dayRegs.Any(regToCheck => regToCheck.TipoInterventoCan == lineCantType))
                        {

                            // caricamento dei dati della registrazione successiva
                            int nextRegPosition = dayRegs.IndexOf(reg) + 1;
                            Reg nextReg = nextRegPosition >= dayRegs.Count ? default(Reg) : dayRegs.ElementAt(nextRegPosition);

                            // caricamento dei dati della registrazione successiva alla successiva
                            int nextRegToNextPosition = nextRegPosition + 1;
                            Reg nextRegToNext = nextRegToNextPosition >= dayRegs.Count ? default(Reg) : dayRegs.ElementAt(nextRegToNextPosition);

                            // viene recuperato il cantiere collegato alla registrazione che si sta processando e alla successiva
                            Cant currentCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == reg.Cant_Id);
                            Cant nextCant = nextReg != default(Reg) ? RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == nextReg.Cant_Id) : default(Cant);
                            Cant nextCantToNext = nextRegToNext != default(Reg) ? RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == nextRegToNext.Cant_Id) : default(Cant);

                            // si definisce se il cantiere della registrazione successiva alla corrente appartiene
                            // ad un cantiere linea o meno
                            bool isNextRegOnLineCant;
                            if (nextCant == default(Cant))
                                isNextRegOnLineCant = false;
                            else
                                isNextRegOnLineCant = nextCant.Tipo_Interv_Can == lineCantType;

                            // si definisce se il cantiere della registrazione successiva alla corrente appartiene
                            // ad un cantiere linea o meno
                            bool isNextRegToNextOnLineCant;
                            if (nextCantToNext == default(Cant))
                                isNextRegToNextOnLineCant = false;
                            else
                                isNextRegToNextOnLineCant = nextCantToNext.Tipo_Interv_Can == lineCantType;

                            // si procede solamente se il cantiere è stato trovato
                            if (currentCant != default(Cant))
                            {
                                // non si prendono in considerazione le registrazioni linea che non sono precedute da almeno un cantiere non linea
                                if (currentCant.Tipo_Interv_Can != lineCantType || firstDayNotLineEncountered)
                                {
                                    // aggiorno l'indicazione del fatto che per il giorno è già stato trovato un cantiere non linea
                                    if (!firstDayNotLineEncountered)
                                        firstDayNotLineEncountered = currentCant.Tipo_Interv_Can != lineCantType;

                                    // si prende in carico la registrazione solamente se nel giorno/collaboratore è la stessa con quelle ore/minuti
                                    bool canProcessReg = dayRegs.Count(regToProcess => regToProcess.Registrazione_Data_Ora_Fis_Reg.Hour == reg.Registrazione_Data_Ora_Fis_Reg.Hour &&
                                        regToProcess.Registrazione_Data_Ora_Fis_Reg.Minute == reg.Registrazione_Data_Ora_Fis_Reg.Minute) <= 1;

                                    // si procede solamente se la registrazione è processabile
                                    if (canProcessReg)
                                    {
                                        // se si sta processando un cantiere non linea
                                        if (currentCant.Tipo_Interv_Can != lineCantType)
                                        {
                                            // se non è ancora stato preso in carico un inizio allora si tratta di un inizio e lo si segnala;
                                            // altrimenti si tratta sicuramente di un'uscita e quindi segnalo che è necessario processare tale data,
                                            // svuotando al contempo la variabile che dice che si è processato un inizio
                                            if (!firstNonLineProcessing)
                                                firstNonLineProcessing = true;
                                            else
                                                firstNonLineProcessing = false;

                                            // se c'è un inizio deve esserci anche una fine successiva al a quello trovato
                                            // altrimenti si tratta la registrazione in processo come un non inizio
                                            if (firstNonLineProcessing)
                                            {
                                                List<Reg> dayRegsAfterCurrent = dayRegs.GetRange(dayRegs.IndexOf(reg) + 1, dayRegs.Count - dayRegs.IndexOf(reg) - 1);

                                                firstNonLineProcessing = dayRegsAfterCurrent.Any(regToCheck => regToCheck.TipoInterventoCan != lineCantType);
                                            }

                                            // non si tratta come inizio una registrazione con cantiere non linea che abbia una registrazione non linea come successiva
                                            if (firstNonLineProcessing)
                                                firstNonLineProcessing = isNextRegOnLineCant;

                                            // per evitare problemi di chiusura anomala si evitano di trattare blocchi composti da tre timbrature così strutturati:
                                            // - registrazione con cantiere non linea/registrazione con cantiere linea/registrazione con cantiere non linea
                                            // quindi se verifica questa configurazione per trattare lo stesso come inizio
                                            if (firstNonLineProcessing && isNextRegOnLineCant && !isNextRegToNextOnLineCant)
                                                firstNonLineProcessing = false;
                                        }

                                        // se ho già processato un inizio e la timbratura successiva è una timbratura di linea allora 
                                        // si chiude la timbratura attuale alla stessa ora della timbratura di linea successiva (spostando la stessa di un secondo se
                                        // necessario) utilizzando il cantiere della timbratura corrente;
                                        // altrimenti se è stato processato un inizio e la registrazione successiva è un finale (non linea, presente) si cambia della registrazione 
                                        // che si sta processando il cantiere utilizzando il presente sulla registrazione successiva successivo;
                                        // in ogni caso si procede a processare la chiusura per cambio cantiere e/o inserimento registrazione a chiusura solamente se il cantiere della
                                        // registrazione che si sta chiudendo è diverso rispetto alla precedente (così da evitare di disturbare corrette elaborazioni)
                                        int prevCantId = prevCant == default(Cant) ? 0 : prevCant.Cant_Id;
                                        if (currentCant.Cant_Id != prevCantId)
                                        {
                                            if (firstNonLineProcessing && isNextRegOnLineCant)
                                            {
                                                Reg newReg = Init();
                                                CommonService.DuplicateEntity(reg, newReg);

                                                newReg.Reg_Id = 0;
                                                newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(nextReg.Registrazione_Data_Ora_Fis_Reg.Year, nextReg.Registrazione_Data_Ora_Fis_Reg.Month,
                                                    nextReg.Registrazione_Data_Ora_Fis_Reg.Day, nextReg.Registrazione_Data_Ora_Fis_Reg.Hour, nextReg.Registrazione_Data_Ora_Fis_Reg.Minute, 0);
                                                newReg.Data_Registrazione_Reg = DateTime.Now;
                                                newReg.Codice_Accoppiamento = tmpCoupleCode; // inserisco nella registrazione un codice accoppiamento fittizio per poi recuperarle dopo l'inserimento a db
                                                newReg.Custom_Data_Reg = autoGeneratedDataStart;
                                                nextReg.Registrazione_Data_Ora_Fis_Reg = newReg.Registrazione_Data_Ora_Fis_Reg == nextReg.Registrazione_Data_Ora_Fis_Reg ?
                                                    nextReg.Registrazione_Data_Ora_Fis_Reg.AddSeconds(1) : nextReg.Registrazione_Data_Ora_Fis_Reg;

                                                regsToAdd.Add(newReg);
                                            }
                                            else if (firstNonLineProcessing && nextReg != default(Reg))
                                            {
                                                int oldCantId = Convert.ToInt32(reg.Cant_Id);

                                                reg.Cant_Id = nextReg.Cant_Id;
                                                reg.Custom_Data_Reg = String.Format("{0}{1}", cantChangedDataStart, oldCantId);
                                                reg.DataOraUltimaModifica_Reg = DateTime.Now;
                                            }
                                        }
                                    }
                                }
                            }

                            // al termine dell'elaborazione di una registrazione i dati della reg corrente diventano i dati della reg precedente
                            prevReg = reg;
                            prevCant = currentCant;
                        }
                    }
                }
            }

            Add(regsToAdd, true);
            regsToAdd = Find(reg => reg.Codice_Accoppiamento == tmpCoupleCode).ToList();
            regsToAdd.ForEach(reg => reg.Codice_Accoppiamento = null);
            regs.AddRange(regsToAdd);

            // ritorno dei messaggi calcolati dal metodo
            return returnMessages;
        }

        #endregion

        #endregion

        private List<Reg> AdjustFisRegByColG4(List<Reg> regsToProcess)
        //Nel caso di 2 Registrazioni con lo Stesso Orario Aggiunge i Secondi necessari per distinguerle (operazione effettuata nella lista stessa)
        {
            int sec = Int32.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.ImportDouble, "secondi"));
            var regsByDate = regsToProcess.GroupBy(r => r.Registrazione_Data_Ora_Fis_Reg).ToList();
            regsByDate.ForEach(byDateList =>
            {
                // se sono presenti delle reg da shiftare
                if (byDateList.Count() > 1)
                {
                    // per ogi reg da shiftare viene aggiunto un secondo
                    int secondsToAdd = -59;
                    byDateList.ForEach(regToShift => regToShift.Registrazione_Data_Ora_Fis_Reg = regToShift.Registrazione_Data_Ora_Fis_Reg.AddSeconds(secondsToAdd += sec));
                }
            });
            return new List<Reg>(regsByDate.SelectMany(s => s).ToList());

        }

        private HashSet<Reg> AdjustFisRegByCol(IEnumerable<Reg> regsToProcess)
        //Nel caso di 2 Registrazioni con lo Stesso Orario Aggiunge i Secondi necessari per distinguerle (operazione effettuata nella lista stessa)
        {
            //regsToProcess = regsToProcess.OrderBy(r => r.Registrazione_Data_Ora_Fig_Reg).ToList();//.ThenBy(r => r.Fru).ThenBy(r => r.Fru_Id).ToList();
            //regsToProcess = regsToProcess.OrderBy(r => r.Fru_Id == regsToProcess.First().Fru_Id).ToList();
            //regsToProcess = regsToProcess.OrderBy(r => r.Fru_Id).ToList();
            var regsByDate = regsToProcess.GroupBy(r => r.Registrazione_Data_Ora_Fig_Reg).ToList();
            regsByDate.ForEach(byDateList =>
            {
                // se sono presenti delle reg da shiftare
                if (byDateList.Count() > 1)
                {
                    // per ogi reg da shiftare viene aggiunto un secondo
                    int secondsToAdd = 1;
                    byDateList.ForEach(regToShift => regToShift.Registrazione_Data_Ora_Fis_Reg = regToShift.Registrazione_Data_Ora_Fis_Reg.AddSeconds(secondsToAdd++));
                }
            });
            return new HashSet<Reg>(regsByDate.SelectMany(s => s).ToList());

        }

        private HashSet<Reg> newAdjustFisRegByCol(IEnumerable<Reg> regsToProcess)
        //Nel caso di 2 Registrazioni con lo Stesso Orario Aggiunge i Secondi necessari per distinguerle (operazione effettuata nella lista stessa)
        {
            List<Reg> returnList = new List<Reg>();
            Reg reg1 = null;
            Reg reg2 = null;
            Reg reg3 = null;
            foreach (var regsGroupByCol in regsToProcess.Where(r => r.Registrazione_Tipo_Reg == 0).GroupBy(r => r.Col_Id)) {
                foreach (var regs in regsGroupByCol.GroupBy(r => r.Registrazione_Data_Ora_Fis_Reg.Date)) {
                    foreach (Reg reg in regs.OrderBy(r => r.Registrazione_Data_Ora_Fis_Reg))
                    {
                        if (reg1 == null)
                        {
                            //inizializzo la prima reg del gruppo nel caso sia il primo accesso oppure il gruppo sia stato azzerato
                            reg1 = reg;
                            if (reg == regs.Last()) {
                                returnList.Add(reg);
                                reg1 = null;
                            }
                        }
                        else
                        {
                            if (reg2 == null)
                            {
                                //se la prima reg del gruppo è valorizzata controllo se le due reg sono consecutive
                                if (reg1.Cant_Id == reg.Cant_Id)
                                {
                                    //se le reg sono consecutive azzero il gruppo e popolo la lista di ritorno con lo stesso ordine
                                    returnList.Add(reg1);
                                    returnList.Add(reg);
                                    reg1 = null;
                                }
                                else
                                {
                                    //in caso contrario valorizzo la seconda reg del gruppo per continuare il controllo
                                    reg2 = reg;
                                    if (reg == regs.Last()) {
                                        returnList.Add(reg1);
                                        returnList.Add(reg);
                                        reg1 = null;
                                        reg2 = null;
                                    }
                                }
                            }
                            else
                            {
                                //nel caso in cui la seconda non sia null controllo se ci sono delle coppie consecutive
                                if (reg1.Cant_Id == reg.Cant_Id)
                                {
                                    //se la prima e la terza sono dello stesso cantiere cambio l'ordine della lista e tengo la seconda memorizzata
                                    returnList.Add(reg1);
                                    returnList.Add(reg);
                                    reg1 = reg2;
                                    reg2 = null;
                                    if (reg == regs.Last()) {
                                        returnList.Add(reg1);
                                        reg1 = null;
                                    }
                                }
                                else if (reg2.Cant_Id == reg.Cant_Id)
                                {
                                    //se la seconda e la terza sono uguali vuol dire che la prima è singole, popolo la lista con l'ordine normale
                                    returnList.Add(reg1);
                                    returnList.Add(reg2);
                                    returnList.Add(reg);
                                    reg1 = null;
                                    reg2 = null;
                                }
                                else
                                {
                                    returnList.Add(reg1);
                                    reg1 = reg2;
                                    reg2 = reg;
                                    if (reg == regs.Last()) {
                                        returnList.Add(reg1);
                                        returnList.Add(reg2);
                                        reg1 = null;
                                        reg2 = null;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            var regsByDate = returnList.GroupBy(r => r.Registrazione_Data_Ora_Fis_Reg).ToList();
            regsByDate.ForEach(byDateList =>
            {
                // se sono presenti delle reg da shiftare
                if (byDateList.Count() > 1)
                {
                    // per ogi reg da shiftare viene aggiunto un secondo
                    int secondsToAdd = 1;
                    //byDateList.ForEach(regToShift => regToShift.Registrazione_Data_Ora_Fis_Reg = regToShift.Registrazione_Data_Ora_Orig_Reg.AddSeconds(secondsToAdd++));
                    byDateList.ForEach(regToShift => regToShift.Registrazione_Data_Ora_Fis_Reg = new DateTime(regToShift.Registrazione_Data_Ora_Fis_Reg.Year, regToShift.Registrazione_Data_Ora_Fis_Reg.Month, regToShift.Registrazione_Data_Ora_Fis_Reg.Day, regToShift.Registrazione_Data_Ora_Fis_Reg.Hour, regToShift.Registrazione_Data_Ora_Fis_Reg.Minute,0).AddSeconds(secondsToAdd++));
                }
            });        
            return new HashSet<Reg>(regsByDate.SelectMany(s => s).ToList());
        }

        public override Reg Init()
        {
            Reg newReg = base.Init();
            newReg.Data_Registrazione_Reg = DateTime.UtcNow;
            newReg.DataOraUltimaModifica_Reg = DateTime.UtcNow;
            return newReg;
        }

        public override Dictionary<string, string> Check(Reg entity, bool isNew = false, bool isResetSession = true)
        //Esegue i Controlli di Validazione Standard sul Record REG ricevuto
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (isResetSession)
                ResetSession();
            try
            {
                //PER PRIMA COSA VERIFICO CHE LA DATA DELLA REGISTRAZIONE FISICA NON SIA MINORE/UGUALE ALLA DATA DI BLOCCO (se impostata)
                //Nel caso in cui lo sia segnalo errore perchè NON posso toccarla
                var paramRow = RepoManager.ParamRepo.ParametersRow;
                if (paramRow.Data_Blocco_Reg != new DateTime())
                {

                    if (CommonService.Nz(entity.Registrazione_Data_Ora_Fis_Reg, new DateTime(1, 1, 1)) <= paramRow.Data_Blocco_Reg)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Registrazione_Data_Ora_Fis_Reg),
                       BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_REG_MINORE_DI_DATA_BLOCCO));
                }

                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                //Nel caso in cui NON siano presenti nè il Codice FRu nè il Codice PRU allora devono esistere sia il Codice Cant sia il Cod.Col
                //in quanto si tratta di una REG o di una ATTIVITA' o di un PASSAGGIO MANUALE 

                if (CommonService.Nz(entity.Pru_Id, 0) == 0 && CommonService.Nz(entity.Fru_Id, 0) == 0)
                {
                    if (CommonService.Nz(entity.Cant_Id, 0) == 0 || CommonService.Nz(entity.Col_Id, 0) == 0)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_SE_CODICE_PRU_E_FRU_ASSENTI_CANTIERE_E_COLLAB_OBBLIGATORI));
                }
                else
                {

                    if (CommonService.Nz(entity.Fru_Id, 0) == 0)
                    {
                        if (CommonService.Nz(entity.Cant_Id, 0) == 0)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_ALMENO_UN_CAMPO_TRA_X_E_Y_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_FRU, PowerWebResources.FLD_CODICE_CANTIERE));
                    }

                    if (CommonService.Nz(entity.Pru_Id, 0) == 0)
                    {
                        if (CommonService.Nz(entity.Col_Id, 0) == 0)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Col_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_ALMENO_UN_CAMPO_TRA_X_E_Y_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_PRU, PowerWebResources.FLD_CODICE_COLLABORATORE));
                    }
                }


                //
                //Nel caso in cui NON siano presenti nè il Codice Collab nè il Codice Cant. allora devono esistere sia il Codice PRU sia il Cod.PRU
                //in quanto si tratta di una REG provenienti dagli Apparecchi
                //
                if (CommonService.Nz(entity.Cant_Id, 0) == 0 && CommonService.Nz(entity.Col_Id, 0) == 0)
                {
                    if (CommonService.Nz(entity.Pru_Id, 0) == 0 || CommonService.Nz(entity.Fru_Id, 0) == 0)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_SE_CODICE_CAN_E_COL_ASSENTI_FRU_E_PRU_OBBLIGATORI));
                }
                else
                {
                    if (CommonService.Nz(entity.Cant_Id, 0) == 0)
                    {
                        if (CommonService.Nz(entity.Fru_Id, 0) == 0)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fru_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_ALMENO_UN_CAMPO_TRA_X_E_Y_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_FRU, PowerWebResources.FLD_CODICE_CANTIERE));
                    }

                    if (CommonService.Nz(entity.Col_Id, 0) == 0)
                    {
                        if (CommonService.Nz(entity.Pru_Id, 0) == 0)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pru_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_ALMENO_UN_CAMPO_TRA_X_E_Y_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_COLLABORATORE, PowerWebResources.FLD_CODICE_PRU));
                    }
                }



                //
                //Se esiste il Codice Cantiere ma risulta variato rispetto al valore precedente allora ANNULLO il Codice FRU
                //
                //
                //Se esiste il Codice Collaboratore ma risulta variato rispetto al valore precedente alloa ANNULL il Codice PRU
                //
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella            
                //
                if ((CommonService.Nz(entity.Pru_Id, 0) != 0) && (Prus.SingleOrDefault(u => u.Pru_Id == entity.Pru_Id) == null))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pru_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                      PowerWebResources.FLD_PRU_ID, PowerWebResources.STR_PRU));
                if ((CommonService.Nz(entity.Fru_Id, 0) != 0) && (Frus.SingleOrDefault(u => u.Fru_Id == entity.Fru_Id) == null))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fru_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                      PowerWebResources.FLD_FRU_ID, PowerWebResources.STR_FRU));
                if ((CommonService.Nz(entity.Cant_Id, 0) != 0) && (RepoManager.CantRepo.FirstOrDefault(u => u.Cant_Id == entity.Cant_Id) == null))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                      PowerWebResources.FLD_CANT_ID, PowerWebResources.STR_CANTIERI));
                if ((CommonService.Nz(entity.Col_Id, 0) != 0) && (Cols.SingleOrDefault(u => u.Col_Id == entity.Col_Id) == null))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Col_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                      PowerWebResources.FLD_COL_ID, PowerWebResources.STR_COLLABORATORI));

                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //   
                if (CommonService.Nz(entity.Registrazione_Data_Ora_Fis_Reg, new DateTime(1, 1, 1)) == new DateTime(1, 1, 1))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Registrazione_Data_Ora_Fis_Reg),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                   PowerWebResources.FLD_REGISTRAZIONE_DATA_ORA_FIS_REG));
                //Nel caso di una REG accoppiata allora diventa Obbligatorio avere anche una DATA/ORA FIGURATIVA
                if ((entity.Registrazione_Stato_RegEnum & RegStateEnum.Ass) == RegStateEnum.Ass)
                {
                    if (CommonService.Nz(entity.Registrazione_Data_Ora_Fig_Reg, new DateTime(1, 1, 1)) == new DateTime(1, 1, 1))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Registrazione_Data_Ora_Fig_Reg),
                       BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                       PowerWebResources.FLD_REGISTRAZIONE_DATA_ORA_FIG_REG));
                }
                //4.1 verifico, per una serie di campi, che il valore del campo sia minore o minore di un certo valore
                //
                if (CommonService.Nz(entity.Flag_EU_Reg, "") != "")
                    if (entity.Flag_EU_Reg.Length > 1)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_EU_Reg),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                           PowerWebResources.FLD_FLAG_EU_REG, PowerWebResources.VALORE_1));
                //
                //5) verifico, per una serie di campi, che il valore del campo sia presente nella relativa Tabella
                // 
                if (CommonService.Nz(entity.Flag_EU_Reg, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.FLAG_EU_REG.ToString() && x.Chiave_Tab == entity.Flag_EU_Reg.ToString().ToUpper()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_EU_Reg),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_FLAG_EU_REG));
                if (CommonService.Nz(entity.Motivazione_Reg_Id, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.MOTIVAZIONI.ToString() && x.Tab_Decod_Id == entity.Motivazione_Reg_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Motivazione_Reg_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_MOTIVAZIONE_REG_ID));
                if (CommonService.Nz(entity.Registrazione_Tipo_Reg, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_REGISTRAZIONE.ToString() && x.Chiave_Tab == entity.Registrazione_Tipo_Reg.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Registrazione_Tipo_Reg),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_REGISTRAZIONE_TIPO_REG));
                if (CommonService.Nz(entity.Registrazione_Stato_Reg, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.STATO_REGISTRAZIONE.ToString() && x.Chiave_Tab == entity.Registrazione_Stato_Reg.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Registrazione_Stato_Reg),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_REGISTRAZIONE_STATO_REG));

                WriteCheckLog(entity, result, Log);
                return result;
            }
            catch (Exception ex)
            {
                var CodErr = "Reg_Id: " + entity.Reg_Id;
                throw ex;
            }
        }


        public override Dictionary<string, string> CheckForImport(Reg entity)
        //Controlli aggiuntivi SOLO per gli Import  BATCH da ACCESS
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            //
            //Controlli specifici da fare solo x IMPORT BATCH
            //

            //Nel caso dell'IMPORT BATCH DA ACCESS devono esistere sia il Codice Cant sia il Codice Collaboratore           
            //            
            // non si procede alla verifica del cantiere in caso si stia trattando una rettifica, una registrazione di sola durata o un arrotondamento per durata
            if (entity.Registrazione_Tipo_Reg != (int)RegTypeEnum.RettTimesheet && entity.Registrazione_Tipo_Reg != (int)RegTypeEnum.Duration && entity.Registrazione_Tipo_Reg != (int)RegTypeEnum.ArrotDur)
            {
                if (CommonService.Nz(entity.Cant_Id, 0) == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                        PowerWebResources.FLD_CANT_ID));
            }
            if (CommonService.Nz(entity.Col_Id, 0) == 0)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Col_Id),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                    PowerWebResources.FLD_COL_ID));

            if (entity.Registrazione_Data_Ora_Orig_Reg == DateTime.MinValue)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Registrazione_Data_Ora_Orig_Reg),
                BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                PowerWebResources.FLD_REGISTRAZIONE_DATA_ORA_ORIG_REG));
            if (entity.Registrazione_Data_Ora_Fis_Reg == DateTime.MinValue)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Registrazione_Data_Ora_Fis_Reg),
                BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                PowerWebResources.FLD_REGISTRAZIONE_DATA_ORA_FIS_REG));
            if (entity.Registrazione_Tipo_Reg == Int32.MinValue)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Registrazione_Tipo_Reg),
                BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                PowerWebResources.FLD_REGISTRAZIONE_TIPO_REG));
            if (entity.Registrazione_Stato_Reg == Int32.MinValue)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Registrazione_Stato_Reg),
                BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                PowerWebResources.FLD_REGISTRAZIONE_STATO_REG));

            //verifico SOLO x IMPORT la validità delle eventuali Date Ricevute
            if (entity.Data_Registrazione_Reg < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Reg),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_REG));
            if (entity.DataOraUltimaModifica_Reg < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Reg),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMDOIFICA_REG));
            WriteCheckLog(entity, result, Log);
            return result;
        }

        #region IMPORT DA DATASET

        public List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet dataSet, String tableName, bool onlyErrors = false)
        {
            // stringhe che contengono l'identificativo della motivazione delle rettifiche all'interno delle tabell REG_FIS
            const string correctionPlusJustificationName = "RE+";
            const string correctionMinusJustificationName = "RE-";

            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Reg");
            List<Dictionary<string, string>> resultList = new List<Dictionary<string, string>>();
            List<Reg> toImport = new List<Reg>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();

            // inizializzazione dell'elenco dei relative record number access da accoppiare
            List<int> regsToCouple = new List<int>();

            // inizializzazione della dimensione dei chunk per la scrittura delle reg
            int size = 1;
            // inizializzazione dell'indice di partenza per l'elaborazione del chunk
            int startIndex = 0;

            string lastKey = "";
            double nRec = 0;
            double countRec = 0;
            double percRec = 0;

            #region Importazione tabella RIL

            if (tableName == "Ril")
            {
                List<PowerMDBDataSet.RilRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData<PowerMDBDataSet.RilRow>
                        (dataSet.Ril.ToList(), tableName, "RRN").OrderBy(acd => acd.RRN).ToList();
                List<Dictionary<string, string>> currentDictionaries = new List<Dictionary<string, string>>();
                nRec = accessData.Count;
                //----------------------------------------------------------TRATTO I RECORD DI RIL ------------------------------------------------------               
                foreach (PowerMDBDataSet.RilRow row in accessData)
                //Per ogni RIL ricevuta scrivo due differenti RECORD di REG (E+U)
                {
                    lastKey = row.RRN.ToString();
                    countRec = countRec + 1;
                    percRec = (countRec / nRec) * 100;
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                       BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "REG",
                       "17", "17", row.RRN.ToString(), countRec.ToString(), nRec.ToString()));
                    //Inizializza Reg di ENTRATA
                    Reg newRegE = this.Init();

                    //Verifica e REcupera ID Cantiere
                    if (!row.IsCodice_Cantiere_RilNull())
                    {
                        var currentCant = RepoManager.CantRepo.FirstOrDefault(x => x.Codice_Cantiere == row.Codice_Cantiere_Ril);
                        if (currentCant != null)
                            newRegE.Cant_Id = currentCant.Cant_Id;
                    }
                    //Verifica e Recupera ID Collaboratore
                    if (!row.IsCodice_Collaboratore_RilNull())
                    {
                        var currentCol = RepoManager.ColRepo.FirstOrDefault(x => x.Codice_Collaboratore == row.Codice_Collaboratore_Ril);
                        if (currentCol != null)
                            newRegE.Col_Id = currentCol.Col_Id;
                    }
                    //Verifica e REcupera ID Collaboratore
                    if (!row.IsCodice_FRU_RilNull())
                    {
                        var currentFru = RepoManager.FruRepo.FirstOrDefault(x => x.Codice_Fru == row.Codice_FRU_Ril);
                        if (currentFru != null)
                            newRegE.Fru_Id = currentFru.Fru_Id;
                    }
                    //Verifica e REcupera ID Collaboratore
                    if (!row.IsCodice_PRU_RilNull())
                    {
                        var currentPru = RepoManager.PruRepo.FirstOrDefault(x => x.Codice_Pru == row.Codice_PRU_Ril);
                        if (currentPru != null)
                            newRegE.Pru_Id = currentPru.Pru_Id;
                    }

                    //Carico i KM                     
                    newRegE.KM_Reg = (decimal)row.KM_Ril;

                    // LE MOTIVAZIONI VG* di ACCESS DIVENTANO MOTIVAZIONI=BLANKS + REGISTRAZIONE_TIPO_REG = 4 (TRIP)
                    // LE ALTRE MOTIVAZIONI VENGONO VERIFICA NELLA TAB_DECOD e NE VIENE RECUPERATO il relativo ID della Motivazione
                    if (!row.IsTipo_RilevazioneNull())
                    {
                        if (row.Tipo_Rilevazione.IndexOf("VG") != -1)
                        {
                            //Si tratta di un VIAGGIO - Imposto il Tipo Rilevazione di conseguenza.
                            newRegE.Registrazione_Tipo_Reg = (int)RegTypeEnum.Trip;

                        }
                        else
                        {
                            //NON si tratta di un Viaggio
                            Tab_Decod currentTabDecod = Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                                && x.Nome_Tab == TabDecodNameEnum.MOTIVAZIONI.ToString() && x.Chiave_Tab == row.Tipo_Rilevazione);
                            //Recupero l'ID Corrispondente alla Motivazione
                            if (currentTabDecod != null)
                                newRegE.Motivazione_Reg_Id = currentTabDecod.Tab_Decod_Id;
                        }
                    }

                    if (!row.IsNote_RilNull())
                        newRegE.Note_Reg = row.Note_Ril;

                    // Imposta Data/Ora Orig e FIS e FIG della REG di Entrata
                    newRegE.Registrazione_Data_Ora_Fis_Reg = row.Prestazione_Data_Ril.AddMinutes(Math.Truncate(row.Prestazione_HHMM_Inizio_Ril.TimeOfDay.TotalMinutes));
                    newRegE.Registrazione_Data_Ora_Fig_Reg = row.Prestazione_Data_Ril.AddMinutes(Math.Truncate(row.Prestazione_HHMM_Inizio_Fig_Ril.TimeOfDay.TotalMinutes));
                    newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;

                    //Inizializzo la Data Originaria Della ENTRATA UGUALE alla FISICA o meno in base al Valore del campo Flag_Man_Ril: 
                    // 1= Modificata Ora Fine  -  2= Modificata Ora INizio  -  3= Originale/Manuale  -  4= Modificate entrambe
                    if (!row.IsFlag_Man_RilNull() && !String.IsNullOrEmpty(row.Flag_Man_Ril)) // se il flag ha un valore
                    {
                        if (row.Flag_Man_Ril == "2" || row.Flag_Man_Ril == "4")
                            newRegE.Registrazione_Data_Ora_Orig_Reg = new DateTime(2002, 1, 1);
                    }

                    // inserimento del relative record number di access per procedere a successivo accoppiamento dopo inserimento
                    newRegE.RiferimentoRRN_Att = row.RRN;

                    // aggiornamento dell'elenco dei relative record number di access da accoppiare
                    regsToCouple.Add(row.RRN);

                    //-----------------------------------------------Inizializza REG di USCITA -----------------------------
                    Reg newRegU = this.Init();
                    newRegU.Col_Id = newRegE.Col_Id;
                    newRegU.Cant_Id = newRegE.Cant_Id;
                    newRegU.Fru_Id = newRegE.Fru_Id;
                    newRegU.Pru_Id = newRegE.Pru_Id;
                    newRegU.Motivazione_Reg_Id = newRegE.Motivazione_Reg_Id;
                    // LE MOTIVAZIONI VG* di ACCESS DIVENTANO MOTIVAZIONI=BLANKS + TIPO_REGISTRAZIONE = 8 (TRIP)
                    // LE ALTRE MOTIVAZIONI VENGONO VERIFICA NELLA TAB_DECOD e NE VIENE RECUPERATO il relativo ID della Motivazione
                    if (!row.IsTipo_RilevazioneNull())
                    {
                        if (row.Tipo_Rilevazione.IndexOf("VG") != -1)
                        {
                            //Si tratta di un VIAGGIO - Imposto il Tipo Rilevazione di conseguenza.
                            newRegU.Registrazione_Tipo_Reg = (int)RegTypeEnum.Trip;
                        }
                        else
                        {
                            //NON si tratta di un Viaggio
                            Tab_Decod currentTabDecod = Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                                && x.Nome_Tab == TabDecodNameEnum.MOTIVAZIONI.ToString() && x.Chiave_Tab == row.Tipo_Rilevazione);
                            //Recupero l'ID Corrispondente alla Motivazione
                            if (currentTabDecod != null)
                                newRegU.Motivazione_Reg_Id = currentTabDecod.Tab_Decod_Id;
                        }
                    }
                    newRegU.Note_Reg = newRegE.Note_Reg;

                    // Imposta Data/Ora Orig e FIS e FIG della REG di UScita
                    newRegU.Registrazione_Data_Ora_Fis_Reg = row.Prestazione_Data_Ril.AddMinutes(Math.Truncate(row.Prestazione_HHMM_Fine_Ril.TimeOfDay.TotalMinutes));
                    newRegU.Registrazione_Data_Ora_Fig_Reg = row.Prestazione_Data_Ril.AddMinutes(Math.Truncate(row.Prestazione_HHMM_Fine_Fig_Ril.TimeOfDay.TotalMinutes));
                    newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                    //Inizializzo la Data Originaria Della USCITA UGUALE alla FISICA o meno in base al Valore del campo Flag_Man_Ril: 
                    // 1= Modificata Ora Fine  -  2= Modificata Ora INizio  -  3= Originale/Manuale  -  4= Modificate entrambe
                    if (!row.IsFlag_Man_RilNull() && !String.IsNullOrEmpty(row.Flag_Man_Ril))
                    {
                        if (row.Flag_Man_Ril == "1" || row.Flag_Man_Ril == "4")
                            newRegU.Registrazione_Data_Ora_Orig_Reg = new DateTime(2002, 1, 1);
                    }
                    if (row.Prestazione_HHMM_Inizio_Ril > row.Prestazione_HHMM_Fine_Ril)
                        //devo aggiungere un giorno alla timbratura che sto creando perché c'è il cambio giorno
                        newRegU.Registrazione_Data_Ora_Fis_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg.AddDays(1);
                    newRegU.Registrazione_Data_Ora_Fig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                    //Imposto Legame sulla REG di Uscita con la REG di Entrata
                    newRegU.ParentReg = newRegE;

                    // inserimento del relative record number di access per procedere a successivo accoppiamento dopo inserimento
                    newRegU.RiferimentoRRN_Att = row.RRN;

                    //Controllo la validità delle Reg di Entrata e di Uscita
                    List<Dictionary<string, string>> importDictionaries = CheckForImport(new List<Reg> { newRegE, newRegU });
                    //Solo la prima volta chiamo la Check con ResetSession=True per fargli aggironare i Dati dal DB
                    if (countRec == 1)
                        currentDictionaries = Check(new List<Reg> { newRegE, newRegU }, true, true);
                    else
                        currentDictionaries = Check(new List<Reg> { newRegE, newRegU }, true, false);

                    //Imposto Tipo_REG = ASSOCIATA (1) sui 2 Record
                    newRegE.Registrazione_Stato_RegEnum |= RegStateEnum.Ass;
                    newRegU.Registrazione_Stato_RegEnum |= RegStateEnum.Ass;


                    if (currentDictionaries.Sum(dict => dict.Keys.Count) == 0 && importDictionaries.Sum(dict1 => dict1.Keys.Count) == 0)
                    {
                        //Nel caso in cui NON ci siamo stati errori aggiungo i due Record alla lista delle REG da Importare
                        toImport.Add(newRegE);
                        toImport.Add(newRegU);
                    }
                    else
                    {
                        errors.Add(new Tab_Chk_Imp
                        //Nel caso in cui ci siamo stati errori li scrivo in TAB_CHK_IMPORT
                        {
                            Nome_Tabella_Tab_Check_Imp = tableName,
                            Chiave_Record_Tab_Check_Imp = row.RRN.ToString(),
                        });
                        log.Warn("Codice Cantiere cui si rifericono gli errori precedenti: " + row.Codice_Cantiere_Ril + " - Codice Collaboratore: " + row.Codice_Collaboratore_Ril + " - RRN: " + row.RRN + " DATA RIL: " + row.Prestazione_Data_Ril + " -----------------------");
                        errorsList.AddRange(currentDictionaries);
                        //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                        if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                            Log.WarnFormat("TAB RIL - Chiave: {0}", row.RRN.ToString());
                    }
                }
            }

            #endregion

            #region Importazione tabella InputFisico

            if (tableName == "InputFisico")
            {

                List<PowerMDBDataSet.InputFisicoRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData<PowerMDBDataSet.InputFisicoRow>
                    (dataSet.InputFisico.ToList(), tableName, "RRN").OrderBy(acd => acd.RRN).ToList(); ;
                List<Dictionary<string, string>> currentDictionaries = new List<Dictionary<string, string>>();
                nRec = accessData.Count;
                //----------------------------------------------------------TRATTO I RECORD DI INPUT_FISICO ------------------------------------------------------
                //Questa volta da ogni Input_Fisico ricevutao crivo un Solo Record in REG CON STATO NON ACCOPPIATO!!
                accessData = accessData.OrderBy(accReg => accReg.RRN).ToList();

                foreach (PowerMDBDataSet.InputFisicoRow row in accessData)
                {
                    lastKey = row.RRN.ToString();
                    countRec = countRec + 1;
                    percRec = (countRec / nRec) * 100;
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "INPUTFISICO",
                        "17C", "17", row.RRN.ToString(), countRec.ToString(), nRec.ToString()));
                    Reg newRegE = this.Init();

                    //Verifica e REcupera ID Cantiere
                    if (!row.IsCodice_Cantiere_FisNull())
                    {
                        var currentCant = RepoManager.CantRepo.FirstOrDefault(x => x.Codice_Cantiere == row.Codice_Cantiere_Fis);
                        if (currentCant != null)
                            newRegE.Cant_Id = currentCant.Cant_Id;
                    }
                    //Verifica e REcupera ID Collaboratore
                    if (!row.IsCodice_Collaboratore_FisNull())
                    {
                        var currentCol = RepoManager.ColRepo.FirstOrDefault(x => x.Codice_Collaboratore == row.Codice_Collaboratore_Fis);
                        if (currentCol != null)
                            newRegE.Col_Id = currentCol.Col_Id;
                    }
                    //Verifica e REcupera ID Fru
                    if (!row.IsMatricola_Fru_FisNull())
                    {
                        var currentFru = RepoManager.FruRepo.FirstOrDefault(x => x.Codice_Fru == row.Matricola_Fru_Fis);
                        if (currentFru != null)
                            newRegE.Fru_Id = currentFru.Fru_Id;
                    }
                    //Verifica e REcupera ID PRU
                    if (!row.IsMatricola_Pru_FisNull())
                    {
                        var currentPru = RepoManager.PruRepo.FirstOrDefault(x => x.Codice_Pru == row.Matricola_Pru_Fis);
                        if (currentPru != null)
                            newRegE.Pru_Id = currentPru.Pru_Id;
                    }

                    //Carico Data/Ora ORIG,FIS,FIG della REGISTRAZIONE
                    newRegE.Registrazione_Data_Ora_Fis_Reg = new DateTime(Convert.ToInt32(row.Anno_Fis), Convert.ToInt32(row.Mese_Fis), Convert.ToInt32(row.Giorno_Fis),
                                                                               Convert.ToInt32(row.HH_Fis), Convert.ToInt32(row.MM_Fis), 0);
                    newRegE.Registrazione_Data_Ora_Fig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;

                    //Inizializzo la Data Originaria UGUALE alla FISICA in quanto i Record di Input_Fisico sono per definizione Timbrature Originarie
                    newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;

                    //IMposto lo stato Registrazione come NON ASSCOCIATA
                    newRegE.Registrazione_Stato_RegEnum |= RegStateEnum.None;

                    //NON eseguo i Controlli Standard sulla Registrazione di Entrata
                    //List<Dictionary<string, string>> importDictionaries = CheckForImport(new List<Reg> { newRegE });

                    //Solo la prima volta chiamo la Check con ResetSession=True per fargli aggironare i Dati dal DB
                    //if (countRec == 1)
                    //    currentDictionaries = Check(new List<Reg> { newRegE }, true, true);
                    //else
                    //    currentDictionaries = Check(new List<Reg> { newRegE }, true, false);

                    //if (currentDictionaries.Sum(dict => dict.Keys.Count) == 0 && importDictionaries.Sum(dict => dict.Keys.Count) == 0)
                    //    //Nel caso in cui NON ci siamo stati errori aggiungo il Record alla lista delle REG da Importare                                                                                                                                                              
                    toImport.Add(newRegE);
                    //else
                    //{
                    //errors.Add(new Tab_Chk_Imp
                    ////Nel caso in cui ci siamo stati errori lo scrivo in TAB_CHK_IMPORT
                    //{
                    //    Nome_Tabella_Tab_Check_Imp = tableName,
                    //    Chiave_Record_Tab_Check_Imp = row.RRN.ToString(),
                    //});
                    //log.Warn("RRN cui si rifericono gli errori precedenti: " + row.RRN + " -----------------------");
                    //errorsList.AddRange(currentDictionaries);
                    ////Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                    //if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                    //    Log.WarnFormat("INPUTFISICO - Chiave: {0}", row.RRN.ToString());
                    //}
                }
            }

            #endregion

            #region Importazione tabella Reg_Fis

            if (tableName == "Reg_Fis")
            {
                List<PowerMDBDataSet.Reg_FisRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                    <PowerMDBDataSet.Reg_FisRow>(dataSet.Reg_Fis.ToList(), tableName, "RRN").OrderBy(acd => acd.RRN).ToList(); ;
                List<Dictionary<string, string>> currentDictionaries = new List<Dictionary<string, string>>();
                nRec = accessData.Count;
                //----------------------------------------------------------TRATTO I RECORD DI REG_FIS ------------------------------------------------------
                //Questa volta da ogni Reg_Fis ricevuta scrivo un Solo Record in REG
                accessData = accessData.OrderBy(accReg => accReg.Codice_Collaboratore_Reg_Fis).ThenBy(accReg => new DateTime(Convert.ToInt32(accReg.Anno_Reg_Fis), Convert.ToInt32(accReg.Mese_Reg_Fis), Convert.ToInt32(accReg.Giorno_Reg_Fis), Convert.ToInt32(accReg.HH_Reg_Fis), Convert.ToInt32(accReg.MM_Reg_Fis), 00)).ThenBy(accReg => accReg.Codice_Cantiere_Reg_Fis).ToList();

                Reg lastOpeningReg = null;

                foreach (PowerMDBDataSet.Reg_FisRow row in accessData)
                {
                    // determino se la riga che si sta processando è una rettifica o meno
                    bool isCorrection = false;
                    if (!row.IsMotivazione_Reg_FisNull())
                        isCorrection = row.Motivazione_Reg_Fis == correctionPlusJustificationName || row.Motivazione_Reg_Fis == correctionMinusJustificationName;

                    lastKey = row.RRN.ToString();
                    countRec = countRec + 1;
                    percRec = (countRec / nRec) * 100;
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "REG_FIS",
                        "17A", "17", row.RRN.ToString(), countRec.ToString(), nRec.ToString()));
                    Reg newRegE = this.Init();

                    // si processa il cantiere solamente se non si sta trattando una rettifica
                    if (!isCorrection)
                    {
                        //Verifica e REcupera ID Cantiere
                        if (!row.IsCodice_Cantiere_Reg_FisNull())
                        {
                            var currentCant = RepoManager.CantRepo.FirstOrDefault(x => x.Codice_Cantiere == row.Codice_Cantiere_Reg_Fis);
                            if (currentCant != null)
                                newRegE.Cant_Id = currentCant.Cant_Id;
                        }
                    }

                    //Verifica e REcupera ID Collaboratore
                    if (!row.IsCodice_Collaboratore_Reg_FisNull())
                    {
                        var currentCol = RepoManager.ColRepo.FirstOrDefault(x => x.Codice_Collaboratore == row.Codice_Collaboratore_Reg_Fis);
                        if (currentCol != null)
                            newRegE.Col_Id = currentCol.Col_Id;
                    }

                    // si processano PRU, FRU e motivazione solamente se non si sta trattando una rettifica
                    if (!isCorrection)
                    {
                        //Verifica e REcupera ID Fru
                        if (!row.IsMatricola_Fru_Reg_FisNull())
                        {
                            var currentFru = RepoManager.FruRepo.FirstOrDefault(x => x.Codice_Fru == row.Matricola_Fru_Reg_Fis);
                            if (currentFru != null)
                                newRegE.Fru_Id = currentFru.Fru_Id;
                        }

                        //Verifica e REcupera ID PRU
                        if (!row.IsMatricola_Pru_Reg_FisNull())
                        {
                            var currentPru = RepoManager.PruRepo.FirstOrDefault(x => x.Codice_Pru == row.Matricola_Pru_Reg_Fis);
                            if (currentPru != null)
                                newRegE.Pru_Id = currentPru.Pru_Id;
                        }

                        //Verifico Motivazione
                        if (!row.IsMotivazione_Reg_FisNull())
                        {
                            Tab_Decod currentTabDecod = Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                            && x.Nome_Tab == TabDecodNameEnum.MOTIVAZIONI.ToString() && x.Chiave_Tab == row.Motivazione_Reg_Fis);
                            if (currentTabDecod != null)
                                newRegE.Motivazione_Reg_Id = currentTabDecod.Tab_Decod_Id;
                        }
                    }

                    //Carico Data/Ora ORIG,FIS,FIG della REG ENTRATA
                    // [in caso di registrazione di tipo rettifica, l'ora viene sempre riportata a mezzanotte]
                    newRegE.Registrazione_Data_Ora_Fis_Reg = isCorrection ? new DateTime(Convert.ToInt32(row.Anno_Reg_Fis), Convert.ToInt32(row.Mese_Reg_Fis), Convert.ToInt32(row.Giorno_Reg_Fis),
                                                                               0, 0, 0)
                                                                          : new DateTime(Convert.ToInt32(row.Anno_Reg_Fis), Convert.ToInt32(row.Mese_Reg_Fis), Convert.ToInt32(row.Giorno_Reg_Fis),
                                                                               Convert.ToInt32(row.HH_Reg_Fis), Convert.ToInt32(row.MM_Reg_Fis), 0);
                    newRegE.Registrazione_Data_Ora_Fig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;

                    //Inizializzo la Data Originaria UGUALE alla FISICA in quanto per le REG_FIS non ci sono indicazioni su Variazioni Manuali o Meno
                    newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;

                    // Le reg_fis con tipo registrazione a 4 sono dei passaggi: vanno scritti come tali e flaggati come abbinati
                    if (!row.IsTipo_Reg_FisNull())
                    {
                        if (row.Tipo_Reg_Fis == 4)
                        {
                            newRegE.Registrazione_Tipo_Reg = (int)RegTypeEnum.Pass;
                            newRegE.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                        }
                    }

                    // se si sta processando una rettifica allora si imposta per la stessa anche il tipo, lo stato e la durata
                    if (isCorrection)
                    {
                        newRegE.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                        newRegE.Registrazione_Tipo_Reg = (int)RegTypeEnum.RettTimesheet;
                        // la durata è negativa o positiva a seconda del tipo di rettifica;
                        // la durata è quindi calcolata moltiplicando per 1 (rettifica +) o -1 (rettifica -)
                        // il numero di ore della rettifica moltiplicate per 60 (così da ottenere i minuti della componente ore)
                        // più il numero di minuti
                        newRegE.Rettifica_Durata = Convert.ToInt32((row.Motivazione_Reg_Fis.EndsWith("-") ? -1 : 1) * ((row.Totale_Ore_Reg_Fis * 60) + row.Totale_Min_Reg_Fis));
                    }

                    //eseguo i Controlli Standard sulla Registrazione di Entrata
                    List<Dictionary<string, string>> importDictionaries = CheckForImport(new List<Reg> { newRegE });
                    //Solo la prima volta chiamo la Check con ResetSession=True per fargli aggironare i Dati dal DB
                    if (countRec == 1)
                        currentDictionaries = Check(new List<Reg> { newRegE }, true, true);
                    else
                        currentDictionaries = Check(new List<Reg> { newRegE }, true, false);

                    if (currentDictionaries.Sum(dict => dict.Keys.Count) == 0 && importDictionaries.Sum(dict => dict.Keys.Count) == 0)
                    //Nel caso in cui NON ci siamo stati errori aggiungo il Record alla lista delle REG da Importare
                    {
                        if (!row.IsDispari_Reg_FisNull() && row.Dispari_Reg_Fis.ToUpper() == "PARI")
                        {
                            if (lastOpeningReg == null)
                                lastOpeningReg = newRegE;
                            else
                            {
                                if (newRegE.Registrazione_Data_Ora_Fis_Reg.Date == lastOpeningReg.Registrazione_Data_Ora_Fis_Reg.Date && newRegE.Col_Id == lastOpeningReg.Col_Id)
                                {
                                    newRegE.ParentReg = lastOpeningReg;
                                    lastOpeningReg.Registrazione_Stato_RegEnum |= RegStateEnum.Ass;
                                    newRegE.Registrazione_Stato_RegEnum |= RegStateEnum.Ass;
                                    newRegE.RiferimentoRRN_Att = row.RRN;
                                    lastOpeningReg.RiferimentoRRN_Att = row.RRN;
                                    regsToCouple.Add(row.RRN);
                                }
                                lastOpeningReg = null;
                            }
                        }
                        else lastOpeningReg = null;

                        toImport.Add(newRegE);
                    }
                    else
                    {
                        errors.Add(new Tab_Chk_Imp
                        //Nel caso in cui ci siamo stati errori lo scrivo in TAB_CHK_IMPORT
                        {
                            Nome_Tabella_Tab_Check_Imp = tableName,
                            Chiave_Record_Tab_Check_Imp = row.RRN.ToString(),
                        });
                        log.Warn("Codice Cantiere cui si rifericono gli errori precedenti: " + row.Codice_Cantiere_Reg_Fis + " - Codice Collaboratore: " + row.Codice_Collaboratore_Reg_Fis + " - RRN: " + row.RRN + " -----------------------");
                        errorsList.AddRange(currentDictionaries);
                        //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                        if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                            Log.WarnFormat("TAB REG_FIS - Chiave: {0}", row.RRN.ToString());
                    }
                }
            }

            #endregion

            #region Importazione tabella Pass

            if (tableName == "Pass")
            {
                List<PowerMDBDataSet.PassRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                    <PowerMDBDataSet.PassRow>(dataSet.Pass.ToList(), tableName, "RRN").OrderBy(acd => acd.RRN).ToList(); ;
                List<Dictionary<string, string>> currentDictionaries = new List<Dictionary<string, string>>();
                nRec = accessData.Count;
                //----------------------------------------------------------TRATTO I RECORD DI PASS ------------------------------------------------------
                //Questa volta da ogni PASS ricevuto scrivo un Solo Record in REG                
                nRec = accessData.Count;
                foreach (PowerMDBDataSet.PassRow row in accessData)
                {
                    lastKey = row.RRN.ToString();
                    countRec = countRec + 1;
                    percRec = (countRec / nRec) * 100;
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "PASS",
                        "17B", "17", row.RRN.ToString(), countRec.ToString(), nRec.ToString()));
                    Reg newRegE = this.Init();
                    //Verifica e REcupera ID Cantiere
                    if (!row.IsCodice_Cantiere_PasNull())
                    {
                        var currentCant = RepoManager.CantRepo.FirstOrDefault(x => x.Codice_Cantiere == row.Codice_Cantiere_Pas);
                        if (currentCant != null)
                            newRegE.Cant_Id = currentCant.Cant_Id;
                    }
                    //Verifica e REcupera ID Collaboratore
                    if (!row.IsCodice_Collaboratore_PasNull())
                    {
                        var currentCol = RepoManager.ColRepo.FirstOrDefault(x => x.Codice_Collaboratore == row.Codice_Collaboratore_Pas);
                        if (currentCol != null)
                            newRegE.Col_Id = currentCol.Col_Id;
                    }
                    //Verifica e REcupera ID Fru
                    if (!row.IsCodice_FRU_PasNull())
                    {
                        var currentFru = RepoManager.FruRepo.FirstOrDefault(x => x.Codice_Fru == row.Codice_FRU_Pas);
                        if (currentFru != null)
                            newRegE.Fru_Id = currentFru.Fru_Id;
                    }
                    //Verifica e Recupera ID PRU
                    if (!row.IsCodice_PRU_PasNull())
                    {
                        var currentPru = RepoManager.PruRepo.FirstOrDefault(x => x.Codice_Pru == row.Codice_PRU_Pas);
                        if (currentPru != null)
                            newRegE.Pru_Id = currentPru.Pru_Id;
                    }
                    //Carico Data/Ora ORIG,FIS,FIG della REG ENTRATA
                    newRegE.Registrazione_Data_Ora_Fis_Reg = new DateTime(Convert.ToInt32(row.Prestazione_AAAA_Pas), Convert.ToInt32(row.Prestazione_MM_Pas), Convert.ToInt32(row.Prestazione_GG_Pas),
                                                                            Convert.ToInt32(row.Prestazione_Ore_Pas.Hour), Convert.ToInt32(row.Prestazione_Ore_Pas.Minute), Convert.ToInt32(row.Prestazione_Ore_Pas.Second));
                    newRegE.Registrazione_Data_Ora_Fig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;

                    //Inizializzo la Data Originaria UGUALE alla FISICA in quanto per i PASS non ci sono indicazioni su Variazioni Manuali o Meno
                    newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;

                    // in ogni caso il default è passaggio
                    newRegE.Registrazione_Tipo_RegEnum = RegTypeEnum.Pass;

                    var currCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == newRegE.Cant_Id);
                    if (currCant != null)
                    {
                        switch (currCant.Tipologia_Can)
                        {
                            case "ATT": // attività
                                newRegE.Registrazione_Tipo_RegEnum = RegTypeEnum.Att;
                                break;
                            default: // tutto il resto sono passaggi
                                newRegE.Registrazione_Tipo_RegEnum = RegTypeEnum.Pass;
                                newRegE.Registrazione_Stato_Reg = (int)RegStateEnum.Ass; // i passaggi importati sono sempre abbinati
                                break;
                        }
                    }

                    //eseguo i Controlli Standard sui Passaggi
                    List<Dictionary<string, string>> importDictionaries = CheckForImport(new List<Reg> { newRegE });
                    if (countRec == 1)
                        currentDictionaries = Check(new List<Reg> { newRegE }, true, true);
                    else
                        currentDictionaries = Check(new List<Reg> { newRegE }, true, false);


                    if (currentDictionaries.Sum(dict => dict.Keys.Count) == 0 && importDictionaries.Sum(dict => dict.Keys.Count) == 0)
                    //Nel caso in cui NON ci siamo stati errori aggiungo il Record alla lista delle REG da Importare
                    {
                        toImport.Add(newRegE);
                    }
                    else
                    {
                        errors.Add(new Tab_Chk_Imp
                        //Nel caso in cui ci siamo stati errori lo scrivo in TAB_CHK_IMPORT
                        {
                            Nome_Tabella_Tab_Check_Imp = tableName,
                            Chiave_Record_Tab_Check_Imp = row.RRN.ToString(),
                        });
                        log.Warn("Codice Cantiere cui si rifericono gli errori precedenti: " + row.Codice_Cantiere_Pas + " - Codice Collaboratore: " + row.Codice_Collaboratore_Pas + " - RRN: " + row.RRN + " -----------------------");
                        errorsList.AddRange(currentDictionaries);
                    }
                }
            }

            #endregion

            #region Aggiornamento database

            if (toImport.Count > 0)
            {
                startIndex = 0;
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                //try
                //{
                //Inizio la TRANSAZIONE DI AGGIORNAMENTO DELLE ENTRATE  
                BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                       BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), tableName, "0"));
                //Inizializzo il N° di Record x Blocchi di Aggiornamento Sul DB
                size = 20000;
                //carico PRIMA le ENTRATE
                var toImportParent = toImport.Where(reg => reg.ParentReg == null).ToList();

                int toImportParentCount = toImportParent.Count;

                while (startIndex < toImportParentCount)
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                      BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), tableName + "/E", startIndex.ToString()));

                    RepoManager.RegRepo.Add(toImportParent.Skip(startIndex).Take(size));
                    try
                    {
                        RepoManager.RegRepo.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        var t = toImportParent.Skip(startIndex).Take(size);
                        throw ex;
                    }
                    startIndex += size;
                }
                //Inizio la TRANSAZIONE DI AGGIORNAMENTO DELLE USCITE
                var toImportChildren = toImport.Where(reg => reg.ParentReg != null).ToList();

                int toImportChildrenCount = toImportChildren.Count;
                startIndex = 0;
                while (startIndex < toImportChildrenCount)
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                      BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), tableName + "/U", startIndex.ToString()));

                    RepoManager.RegRepo.Add(toImportChildren.Skip(startIndex).Take(size));
                    try
                    {
                        RepoManager.RegRepo.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        var t = toImportChildren.Skip(startIndex).Take(size);
                        throw ex;
                    }
                    startIndex += size;
                }
                //AGGIORNO LA TAB_CHK_IMPORT SE SONO STATE CARICATE TUTTE SENZA ERRORI
                RepoManager.Tab_Chk_ImpRepo.Add(errors);
                if (errors.Count == 0)
                    RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                    {
                        Nome_Tabella_Tab_Check_Imp = tableName,
                        Stato_Record_Tab_Check_Imp = true,
                    });

                //SCrivo nel DB l'esito della Tab_CHK_IMPORT
                RepoManager.Tab_Chk_ImpRepo.SaveChanges();
                //Chiudo la Transazione
                RepoManager.Tab_Chk_ImpRepo.CommitWork();

                ResetSession();
            }

            #endregion

            double i = 0;

            #region Accoppiamento fisico delle ril (solo per le ril importate)

            // ------------------------- ACCOPPIAMENTO FISICO DELLE RIL (SOLO X LE RIL IMPORTATE) E DELLE REG_FIS (SOLO PER LE REG_FIS IMPORTATE) ------------------------------
            if (tableName == "Ril" || tableName == "Reg_Fis")
            {
                // accoppiamento dei relative record number salvati
                // ciclo di elaborazione dei relative record number salvati
                List<Reg> regToUpdate = new List<Reg>();


                foreach (int rrn in regsToCouple)
                {
                    i++;
                    percRec = (i / regsToCouple.Count) * 100;

                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                      BusinessService.GetLocalizedString(PowerWebResources.LBL_ABBINAMENTO_REGE_REGU.ToString(), tableName, startIndex.ToString()));
                    //"Sto accoppiando le reg"

                    // recupero delle reg salvate per il relative record number access in elaborazione
                    var rrnToCouple = RepoManager.RegRepo.Find(reg => reg.RiferimentoRRN_Att == rrn).ToList();

                    // se sono stati trovati dei record in numero corretto
                    if (rrnToCouple.Count != 0 && rrnToCouple.Count > 1)
                    {
                        // associazione delle due reg trovate (solo per la reg in uscita):
                        // per identificare la reg in uscita:
                        // 1. Se hanno la stessa data reg allora viene presa la ora fiisca maggiore
                        // 2. In caso di data diversa allora viene presa quella con data maggiore (probabile notturno)
                        Reg regEToCouple;
                        Reg regUToCouple;
                        if (rrnToCouple.First().Registrazione_Data_Ora_Fis_Reg.Date == rrnToCouple.Last().Registrazione_Data_Ora_Fis_Reg.Date)
                        {
                            if (rrnToCouple.First().Registrazione_Data_Ora_Fis_Reg > rrnToCouple.Last().Registrazione_Data_Ora_Fis_Reg)
                            {
                                regUToCouple = rrnToCouple.First();
                                regEToCouple = rrnToCouple.Last();
                            }
                            else
                            {
                                regUToCouple = rrnToCouple.Last();
                                regEToCouple = rrnToCouple.First();
                            }
                        }
                        else
                        {
                            if (rrnToCouple.First().Registrazione_Data_Ora_Fis_Reg.Date > rrnToCouple.Last().Registrazione_Data_Ora_Fis_Reg.Date)
                            {
                                regUToCouple = rrnToCouple.First();
                                regEToCouple = rrnToCouple.Last();
                            }
                            else
                            {
                                regUToCouple = rrnToCouple.Last();
                                regEToCouple = rrnToCouple.First();
                            }
                        }

                        // impostazione dell'id dell'entrata nel campo RiferimentoRRN_Reg dell'uscita
                        regUToCouple.RiferimentoRRN_Reg = regEToCouple.Reg_Id;

                        // svuotare i valori di RiferimentoRRN_Att in tutte e due le reg processate
                        regEToCouple.RiferimentoRRN_Att = null;
                        regUToCouple.RiferimentoRRN_Att = null;

                        // aggiunta delle reg modificate all'elenco di reg da aggiornare
                        regToUpdate.Add(regEToCouple);
                        regToUpdate.Add(regUToCouple);

                    }
                }

                // se sono presenti dei record nell'elenco di reg da importare update del database
                if (regToUpdate.Count > 0)
                {
                    //Inizializzo il N° di Record x Blocchi di Aggiornamento Sul DB
                    size = 5000;
                    // Inizializzazione dell'indice di partenza del chunk
                    startIndex = 0;

                    double toUpdateCount = regToUpdate.Count;

                    i = 0;

                    while (startIndex < toUpdateCount)
                    {
                        i++;
                        percRec = (i / toUpdateCount) * 100;

                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                          BusinessService.GetLocalizedString(PowerWebResources.LBL_AGG_RIF_REGE_REGU.ToString(), tableName, startIndex.ToString()));

                        RepoManager.RegRepo.Update(regToUpdate.Skip(startIndex).Take(size), false);
                        RepoManager.RegRepo.SaveChanges();

                        startIndex += size;
                    }
                }
            }

            #endregion

            #region Abbinamento dei passaggi che sono attività

            // ------------------------------- TRATTAMENTO SOLO PER I PASSAGGI CHE SONO ATTIVITA' (PER MOSAICO) ---------------------------------------------------
            if (tableName == "AbbinaAtt")
            {
                // accoppiamento di tutte le attività importate alle corrispondenti registrazioni (sono passate alla procedura tutte le regv
                // esclusi i viaggi)
                var allRegVs = RepoManager.Reg_VRepo.GetAll(true).AsQueryable();
                var colIdsToProcess = allRegVs.Where(regV => regV.Registrazione_Tipo_Reg == (int)RegTypeEnum.None).Select(regV => regV.Col_Id).Distinct();

                var colIdsCount = colIdsToProcess.Count();

                i = 0;

                foreach (var colId in colIdsToProcess)
                {
                    i++;
                    percRec = (i / colIdsCount) * 100;

                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.LBL_ABBINAMENTO_ATT_A_REG.ToString(), tableName, startIndex.ToString()));


                    var regVForActivity = allRegVs.Where(regV => regV.Col_Id == colId && regV.Registrazione_Tipo_Reg == (int)RegTypeEnum.None && regV.Data_Ora_Fis_U != null).ToList();
                    if (regVForActivity.Count > 0)
                        RepoManager.Reg_VRepo.ElaborateActivities(regVForActivity);

                }

                //AGGIORNO LA TAB_CHK_IMPORT PER SEGNALARE L'ESECUZIONE DELL'ATTIVITA'
                RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                {
                    Nome_Tabella_Tab_Check_Imp = tableName,
                    Stato_Record_Tab_Check_Imp = true,
                });

                //SCrivo nel DB l'esito della Tab_CHK_IMPORT
                RepoManager.Tab_Chk_ImpRepo.SaveChanges();

            }

            // NON SI LANCIA L'ELABORATE IN QUANTO LE ASSOCIAZIONI DELLE ENTRATE USCITE E DELLE ATTIVITA' SONO STATE PRESE DA POWER/COMO

            #endregion

            return errorsList;
        }

        #endregion

        public override void BulkInsert(IEnumerable<Reg> entities)
        {
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LogMalformedRegs) == 1)
            {
                var trackedRegs = entities.Where(c => c.Col_Id == null || c.Cant_Id == null).ToList();

                LogBadFormattedReg(trackedRegs);
            }

            base.BulkInsert(entities);
        }

        public override void BulkUpdate(IEnumerable<Reg> entities)
        {
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LogMalformedRegs) == 1)
            {
                var trackedRegs = entities.Where(c => c.Col_Id == null || c.Cant_Id == null).ToList();

                LogBadFormattedReg(trackedRegs);
            }

            base.BulkUpdate(entities);
        }

        public override int SaveChanges()
        {
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LogMalformedRegs) == 1)//Personalizzazione
            {
                IEnumerable<Reg> trackedRegs = Context.ChangeTracker.Entries<Reg>().Where(c => c.Entity.Col_Id == null || c.Entity.Cant_Id == null).Select(reg => reg.Entity).ToList();

                LogBadFormattedReg(trackedRegs);

            }

            return base.SaveChanges();
        }

        public void LogBadFormattedReg(IEnumerable<Reg> regs)
        {
            if (regs.Any())
            {
                _log.Error(" ");
                _log.Error(" ");
                _log.Error("#######################################");
                _log.Error("Log salvataggio registrazioni malformate");
                _log.Error("Col_Id o Cant_Id = 0 significa null");

                foreach (var reg in regs)
                {
                    if (reg.Cant_Id == null && reg.Col_Id == null)
                    {
                        _log.Error("------------------Registrazione senza collaboratore ne cantiere----------------");
                    }

                    _log.Error(" ");
                    _log.ErrorFormat("Reg id = {0}", reg.Reg_Id);
                    _log.ErrorFormat("Reg col = {0}", reg.Col_Id ?? 0);
                    _log.ErrorFormat("Reg cant = {0}", reg.Cant_Id ?? 0);
                    _log.ErrorFormat("Reg fru = {0}", reg.Fru_Id ?? 0);
                    _log.ErrorFormat("Reg pru = {0}", reg.Pru_Id ?? 0);
                    _log.ErrorFormat("Reg tipo = {0}", reg.Registrazione_Tipo_Reg);
                    _log.ErrorFormat("Reg stato = {0}", reg.Registrazione_Stato_RegEnum.ToString());
                    _log.ErrorFormat("Reg ora = {0}", reg.Registrazione_Data_Ora_Fis_Reg);
                    _log.Error(" ");

                }

                _log.Error(" ");
                _log.Error("StackTrace:");
                _log.Error(" ");

                _log.Error(Environment.StackTrace);

                _log.Error(" ");

                _log.Error("Termine log registrazioni malformate");
                _log.Error("#######################################");
                _log.Error(" ");
                _log.Error(" ");
            }

        }

        public override void Add(IEnumerable<Reg> entities, bool saveChanges = false)
        {
            var toAjustRegs = entities.Where(reg => reg.Att_Id == null).ToList();
            if (toAjustRegs.Count > 0)
            {
                toAjustRegs.ForEach(reg => { reg.Att_Id = reg.Cant_Id; });
            }

            int toAddCount = entities.Count();
            int startIndex = 0;
            int size = (int)CommonService.Nz(RepoManager.ParamRepo.ParametersRow.StoredChunkSize, 5000);

            List<Reg> regsToAdd;

            List<PropertyInfo> properties = null;

            while (startIndex < toAddCount)
            {
                regsToAdd = entities.Skip(startIndex).Take(size).ToList();

                XElement root = new XElement("Regs");

                foreach (Reg reg in regsToAdd)
                {
                    if (properties == null)
                    {
                        properties = reg.GetType().GetProperties().ToList();
                        // sono recuperate anche le stringhe (pur essendo classi)
                        properties = properties.Where(pi => pi != null && pi.DeclaringType == typeof(Reg) && (!pi.PropertyType.IsClass || pi.PropertyType == typeof(String)) && !pi.PropertyType.IsNested).ToList();
                    }

                    XElement regRoot = new XElement("Reg");

                    foreach (PropertyInfo pi in properties)
                    {
                        if (pi.GetValue(reg, null) != null)
                            regRoot.Add(new XElement(pi.Name, pi.GetValue(reg, null)));
                    }

                    root.Add(regRoot);
                }

                var xmlString = root.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);

                Context.Database.ExecuteSqlCommand("EXEC dbo.Reg_Insert @XML", new SqlParameter("XML", xmlString));

                startIndex += size;
            }
        }

        public override void Delete(IEnumerable<Reg> entities, bool saveChanges = false)
        {
            int startIndex = 0;
            int size = (int)CommonService.Nz(RepoManager.ParamRepo.ParametersRow.StoredChunkSize, 5000) * 2;
            List<Reg> regsToDelete;
            bool canExit = false;

            var filteredEntities = entities.Where(reg => reg.RiferimentoRRN_Reg != null).ToList();
            if (filteredEntities.Count == 0)
            {
                canExit = true;
                filteredEntities = entities.ToList();
            }

            int toDeleteCount = filteredEntities.Count();

            while (startIndex < toDeleteCount)
            {
                regsToDelete = filteredEntities.Skip(startIndex).Take(size).ToList();

                XElement root = new XElement("Regs");

                regsToDelete.ForEach(reg =>
                {
                    XElement regRoot = new XElement("Reg");

                    regRoot.Add(new XElement(CommonService.GetPropertyName(() => _regStub.Reg_Id), reg.Reg_Id));

                    root.Add(regRoot);


                });

                var xmlString = root.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);


                Context.Database.ExecuteSqlCommand("EXEC dbo.Reg_Delete @XML", new SqlParameter("XML", xmlString));


                startIndex += size;
            }

            if (!canExit)
            {
                entities = entities.Where(reg => reg.RiferimentoRRN_Reg == null).ToList();
                Delete(entities, saveChanges);
            }
        }

        /// <summary>
        /// Override del metodo per aggiornare le Reg
        /// </summary>
        /// <param name="entities">The entities.</param>
        /// <param name="saveChanges">if set to <c>true</c> [save changes].</param>
        public override void Update(IEnumerable<Reg> entities, bool saveChanges = false)
        {
            //numero di Reg da aggiornare
            int toUpdateCount = entities.Count();
            int startIndex = 0;

            //per far si di non appesantire il tutto viene fatto un chunk
            int size = (int)CommonService.Nz(1000, 5000);

            //viene istanziata la lista delle Reg da aggiornare
            List<Reg> regsToUpdate;

            //lista dei campi della Reg
            List<PropertyInfo> properties = null;

            while (startIndex < toUpdateCount)
            {
                //vengono inserite nella lista le Reg da aggiornare
                regsToUpdate = entities.Skip(startIndex).Take(size).ToList();

                //appresenta un elemento XML, usato per gestire la stored procedure
                //la root generale si chiama Regs
                XElement root = new XElement("Regs");

                //si cicla su tutte le registrazioni della lista da aggiornare
                foreach (Reg reg in regsToUpdate)
                {
                    if (properties == null)
                    {
                        //mediante reflaction esatraggo tutti i campi della Reg
                        properties = reg.GetType().GetProperties().ToList();

                        //vado a pulire i campi che non rispettano il filtro, in questo modo si ottiene una lista con tutti i campi delle reg
                        properties = properties.Where(pi => pi != null && pi.DeclaringType == typeof(Reg) && (!pi.PropertyType.IsClass || pi.PropertyType == typeof(String)) && !pi.PropertyType.IsNested).ToList();
                    }

                    //si istanzia un nuovo root dell'xml
                    XElement regRoot = new XElement("Reg");

                    //viene ciclato su ogni campo della lista delle proprietà (campi delle reg)
                    foreach (PropertyInfo pi in properties)
                    {
                        //per ogni campo delle reg si estraggono i valori della reg da aggiornare
                        if (pi.GetValue(reg, null) != null)
                            //viene aggiunto il campo all'xml
                            regRoot.Add(new XElement(pi.Name, pi.GetValue(reg, null)));
                    }


                    root.Add(regRoot);
                }

                //viene tradotto in stringa l'xml creato atto all'aggiornamento delle reg
                var xmlString = root.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);

                //viene eseguita la stored procedure di aggioranemnto
                Context.Database.ExecuteSqlCommand("EXEC dbo.Reg_Update @XML", new SqlParameter("XML", xmlString));

                startIndex += size;
            }
        }

        public bool StoreOrRestoreRegs(DateTime fromDate, DateTime toDate, bool isBackWard)
        // archivia o ripristina le reg comprese nelle date passate come parametro e ritorna true/false in base all'esito
        {
            // inizializzazione del valore di ritorno del metodo
            bool returnValue = false;


            // tutto il metodo è sotto controllo di commit per poter effettuare un rollback in caso di errory
            try
            {
                _log.InfoFormat("Richiesta ripristino registrazioni con data archiviazione precedente = {0} e data archiviazione corrente = {1}", fromDate.ToShortDateString(), toDate.ToShortDateString());

                // se sto ripristinando delle reg dallo storico

                #region RIPRISTINO REGISTRAZIONI
                if (isBackWard)
                {
                    BusinessService.EditStoredRegDateStatusDictionary[PowerWebContext.Current.User] =
                        new KeyValuePair<double, string>(0d, BusinessService.GetLocalizedString(PowerWebResources.STR_RIPRISTINO_INIZIATO.ToString()));


                    ((PowerWebEntities)RepoManager.ResourcesRepo.Context).Restore(fromDate, toDate);

                }
                #endregion

                #region ARCHIVIAZIONE REGISTRAZIONI
                else // se sto archiviando delle reg nello storico
                {
                    BusinessService.EditStoredRegDateStatusDictionary[PowerWebContext.Current.User] =
                        new KeyValuePair<double, string>(0d, BusinessService.GetLocalizedString(PowerWebResources.STR_ARCHIVIAZIONE_INIZIATA.ToString()));


                    ((PowerWebEntities)RepoManager.ResourcesRepo.Context).Archive(fromDate, toDate);


                    // ritorno l'esito dell'operazione
                    returnValue = true;

                    BusinessService.EditStoredRegDateStatusDictionary[PowerWebContext.Current.User] =
                    new KeyValuePair<double, string>(0d, BusinessService.GetLocalizedString(PowerWebResources.STR_ARCHIVIAZIONE_TERMINATA.ToString()));
                }

                #endregion

            }
            catch (Exception ex)
            {
                // se sono in transazione allora effettuo rollback
                if (IsInTransaction)
                    RollbackWork();

                // scrivo nel dizionario l'errore
                BusinessService.EditStoredRegDateStatusDictionary[PowerWebContext.Current.User] =
                        new KeyValuePair<double, string>(100d, String.Format("{0} - {1}", ex.GetType(), ex.Message));

                // il metodo ritorna un errore nel processo
                returnValue = false;

                _log.ErrorFormat("Errore durante archiviazione/ripristino registrazioni con exception {0}", ex.Message);
            }

            // ritorno del valore del metodo
            return returnValue;
        }

        /// <summary>
        /// Viene generato ed inserito un codice temporaneo di accoppiamento al fine che l'elaborate riesca a riaccoppiare senza rielaborare reg bloccate.
        /// Il controllo di blocco delle reg e della loro presenza è gestito all'interno del metodo verificando il campo sulla regE.
        /// </summary>
        /// <param name="newRegE">La reg in entrata da processare</param>
        /// <param name="newRegU">La reg in uscita da processare</param>
        /// <param name="oldRegEId">L'id della reg in entrata che si sta modificando (le reg passate come parametro sono per struttura dei moduli
        /// sempre reg nuove)</param>
        public void PerformBlockedRegsCouple(Reg newRegE, Reg newRegU, int oldRegEId)
        {
            // se la reg in entrata è valorizzata
            if (newRegE != null)
            {
                // inizializzazione del codice temporaneo
                string tmpCode = null;

                // se la reg in entrata è bloccata
                if (newRegE.Registrazione_Bloccata)
                {
                    // generazione del codice temporaneo
                    tmpCode = Guid.NewGuid().ToString();

                    // inserimento del codice temporaneo nelle reg da processare
                    newRegE.Codice_Accoppiamento = tmpCode;
                    // se la reg in uscita è valorizzata
                    if (newRegU != null) // imposto anche li il codice di accoppiamento
                        newRegU.Codice_Accoppiamento = tmpCode;
                }

                // bloccaggio o sbloccaggio delle attività collegate
                BlockOrUnBlockRegActivities(oldRegEId, tmpCode, newRegE.Registrazione_Bloccata);
            }
        }

        /// <summary>
        /// Blocca o sblocca tutte le reg attività collegate all'id registrazione passato come parametro.
        /// Le reg sono bloccate o sbloccate a seconda del valore del flag di bloccaggio anch'esso passato come parametro.
        /// </summary>
        /// <param name="regId">L'id della registrazione da processare.</param>
        /// <param name="codiceAccoppiamento">The codice accoppiamento temporaneo per il blocco e lo sblocco.</param>
        /// <param name="blocked">Il nuovo stato di blocco della registrazione da processare.</param>
        private void BlockOrUnBlockRegActivities(int regId, string codiceAccoppiamento, bool blocked)
        {
            // sono recuperate tutte le reg di tipo attività collegate
            // alla reg in entrata passata come parametro che necessitano l'inversione del flag di blocco
            var activitiesToProcess =
                Find(
                    reg =>
                        reg.RiferimentoRRN_Att == regId &&
                        reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Att &&
                        reg.Registrazione_Bloccata == !blocked);

            // se sono state trovate delle attività allora si procede a processarle invertendo il loro flag di blocco
            activitiesToProcess.ForEach(reg =>
            {
                reg.Registrazione_Bloccata = !reg.Registrazione_Bloccata;
                reg.Codice_Accoppiamento = codiceAccoppiamento;
            });

            Update(activitiesToProcess, true);
        }

        /// <summary>
        /// Genera e restituisce una nuova reg di tipo rettifica utilizzando i dati passati come parametro.
        /// </summary>
        /// <param name="colId">L'id collaboratore a cui collegare la nuova registrazione.</param>
        /// <param name="correctionDate">La data da utilizzare nella registrazione.</param>
        /// <param name="correctionType">Il tipo di rettifica (positiva/negativa) da generare.</param>
        /// <param name="correctionDuration">La durata (valore assoluto) co cui generare la rettifica.</param>
        /// <returns>
        /// La registrazione contenente la rettifica passata come parametro.
        /// </returns>
        public Reg GenerateCorrectionReg(int colId, DateTime correctionDate, CorrectionTypeEnum correctionType, TimeSpan correctionDuration, int cantId = 0)
        {
            // inizializzazione del valore di ritorno del metodo
            var newCorrection = Init();

            // popolamento dei dati della registrazione
            newCorrection.Col_Id = colId;
            newCorrection.Registrazione_Data_Ora_Fis_Reg = correctionDate;
            newCorrection.Registrazione_Data_Ora_Orig_Reg = correctionDate;
            newCorrection.Registrazione_Data_Ora_Fig_Reg = correctionDate;
            newCorrection.Registrazione_Tipo_Reg = (int)RegTypeEnum.RettTimesheet;
            newCorrection.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
            newCorrection.Rettifica_Durata = Convert.ToInt32(correctionType == CorrectionTypeEnum.CorrectionMinus ? (-1) * correctionDuration.TotalMinutes : correctionDuration.TotalMinutes);
            if (cantId != 0)
            {
                newCorrection.Cant_Id = cantId;
            }

            // ritorno della rettifica generata
            return newCorrection;
        }

        /// <summary>
        /// Genera una nuova reg di tipo 11 (rettifica manuale)
        /// </summary>
        /// <param name="colId">The col identifier.</param>
        /// <param name="correctionDate">The correction date.</param>
        /// <param name="correctionType">Type of the correction.</param>
        /// <param name="correctionDuration">Duration of the correction.</param>
        /// <param name="cantId">The cant identifier.</param>
        /// <returns></returns>
        public Reg GenerateManualRett(int colId, DateTime correctionDate, CorrectionTypeEnum correctionType, TimeSpan correctionDuration, int cantId = 0)
        {
            // inizializzazione del valore di ritorno del metodo
            var newCorrection = Init();

            // popolamento dei dati della registrazione
            newCorrection.Col_Id = colId;
            newCorrection.Registrazione_Data_Ora_Fis_Reg = correctionDate;
            newCorrection.Registrazione_Data_Ora_Orig_Reg = correctionDate;
            newCorrection.Registrazione_Data_Ora_Fig_Reg = correctionDate;
            newCorrection.Registrazione_Tipo_Reg = (int)RegTypeEnum.RettTimeSheetManual;
            newCorrection.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
            newCorrection.Rettifica_Durata = Convert.ToInt32(correctionType == CorrectionTypeEnum.CorrectionMinus ? (-1) * correctionDuration.TotalMinutes : correctionDuration.TotalMinutes);
            if (cantId != 0)
            {
                newCorrection.Cant_Id = cantId;
            }

            // ritorno della rettifica generata
            return newCorrection;
        }
        /// <summary>
        /// Genera e restituisce una nuova reg di tipo arrotondamento utilizzando i dati passati come parametro.
        /// </summary>
        /// <param name="colId">L'id collaboratore a cui collegare la nuova registrazione.</param>
        /// <param name="roundingDate">La data da utilizzare nella registrazione.</param>
        /// <param name="roundingType">Il tipo di arrotondamento (positiva/negativa) da generare.</param>
        /// <param name="roundingDuration">La durata (valore assoluto) con cui generare l'arrotondamento.</param>
        /// <returns>
        /// La registrazione contenente l'arrotondamento passata come parametro.
        /// </returns>
        public Reg GenerateRoundingReg(int colId, DateTime roundingDate, RoundingTypeEnum roundingType, TimeSpan roundingDuration)
        {
            // inizializzazione del valore di ritorno del metodo
            var newRounding = Init();

            // popolamento dei dati della registrazione
            newRounding.Col_Id = colId;
            newRounding.Registrazione_Data_Ora_Fis_Reg = roundingDate;
            newRounding.Registrazione_Data_Ora_Orig_Reg = roundingDate;
            newRounding.Registrazione_Data_Ora_Fig_Reg = roundingDate;
            newRounding.Registrazione_Tipo_Reg = (int)RegTypeEnum.ArrotDur;
            newRounding.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
            newRounding.Rettifica_Durata = Convert.ToInt32(roundingType == RoundingTypeEnum.RoundingMinus ? (-1) * roundingDuration.TotalMinutes : roundingDuration.TotalMinutes);

            // ritorno dell'arrotondamento generato
            return newRounding;
        }

        public Reg GenerateRoundingRegCan(int colId, int cantId, DateTime roundingDate, RoundingTypeEnum roundingType, TimeSpan roundingDuration)
        {
            // inizializzazione del valore di ritorno del metodo
            var newRounding = Init();

            // popolamento dei dati della registrazione
            newRounding.Col_Id = colId;
            newRounding.Cant_Id = cantId;
            newRounding.Registrazione_Data_Ora_Fis_Reg = roundingDate;
            newRounding.Registrazione_Data_Ora_Orig_Reg = roundingDate;
            newRounding.Registrazione_Data_Ora_Fig_Reg = roundingDate;
            newRounding.Registrazione_Tipo_Reg = (int)RegTypeEnum.ArrotDur;
            newRounding.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
            newRounding.Rettifica_Durata = Convert.ToInt32(roundingType == RoundingTypeEnum.RoundingMinus ? (-1) * roundingDuration.TotalMinutes : roundingDuration.TotalMinutes);

            // ritorno dell'arrotondamento generato
            return newRounding;
        }

        public Reg GeneratePausaPranzo(int colId, int cantId, DateTime roundingDate, RoundingTypeEnum roundingType, TimeSpan roundingDuration, string turno)
        {
            // inizializzazione del valore di ritorno del metodo
            var newRounding = Init();
            double arrot = 0;
            int centroDiCostoId = 0;
            List<Cant> cantieri = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == cantId).ToList();
            List<Col> collaboratori = RepoManager.ColRepo.GetAllQueryable(c => c.Col_Id == colId).ToList();
            if (collaboratori.Count > 0 && cantieri.Count > 0) {
                if (collaboratori.First().Retribuzione_Oraria_Col != null)
                {
                    arrot = collaboratori.First().Retribuzione_Oraria_Col.Value;
                }
                else if (cantieri.First().Importo1 != null) {
                    if (cantieri.First().Importo2 != null)
                    {
                        if (roundingDate.Date > cantieri.First().DataVarGps_Can.Value.Date)
                        {
                            arrot = cantieri.First().Importo2.Value;
                        }
                        else
                        {
                            arrot = cantieri.First().Importo1.Value;
                        }
                    }
                    else 
                    {
                        arrot = cantieri.First().Importo1.Value;
                    }
                    
                }
                    
            } else if (collaboratori.Count > 0) {
                if (collaboratori.First().Retribuzione_Oraria_Col != null)
                {
                    arrot = collaboratori.First().Retribuzione_Oraria_Col.Value;
                }
            }
            List<Tab_Decod> motivazioni = RepoManager.Tab_DecodRepo.GetAllQueryable(m => m.Decodifica_Tab == "Pausa").ToList();
            if (arrot > 0) {
                // popolamento dei dati della registrazione
                newRounding.Col_Id = colId;
                newRounding.Cant_Id = cantId;
                //newRounding.CentroDiCosto_Id = centroDiCostoId;
                newRounding.Registrazione_Data_Ora_Fis_Reg = roundingDate;
                newRounding.Registrazione_Data_Ora_Orig_Reg = roundingDate;
                newRounding.Registrazione_Data_Ora_Fig_Reg = roundingDate;
                newRounding.Registrazione_Tipo_Reg = (int)RegTypeEnum.Duration;
                newRounding.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                newRounding.Motivazione_Reg_Id = motivazioni.First().Tab_Decod_Id;
                newRounding.Rettifica_Durata = Convert.ToInt32((-1) * arrot);
                newRounding.Turno = turno;
                newRounding.Note_Reg = "Pausa";
            }

            // ritorno dell'arrotondamento generato
            return newRounding;
        }

        /// <summary>
        /// Genera e restituisce una nuova reg di tipo durata utilizzando i dati passati come parametro.
        /// </summary>
        /// <param name="colId">L'id collaboratore a cui collegare la nuova registrazione.</param>
        /// <param name="roundingDate">La data da utilizzare nella registrazione.</param>
        /// <param name="roundingType">Durata  (positiva/negativa) da generare.</param>
        /// <param name="roundingDuration">La durata (valore assoluto) con cui generare la reg.</param>
        /// <returns>
        /// La registrazione contenente l'arrotondamento passata come parametro.
        /// </returns>
        public Reg GenerateDurationReg(int colId, DateTime date, CorrectionTypeEnum durationType, TimeSpan duration, int cantId, int motivationId = 0)
        {
            // inizializzazione del valore di ritorno del metodo
            var newDuration = Init();

            // popolamento dei dati della registrazione
            newDuration.Col_Id = colId;
            newDuration.Registrazione_Data_Ora_Fis_Reg = date;
            newDuration.Registrazione_Data_Ora_Orig_Reg = date;
            newDuration.Registrazione_Data_Ora_Fig_Reg = date;
            newDuration.Registrazione_Tipo_Reg = (int)RegTypeEnum.Duration;
            newDuration.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
            newDuration.Cant_Id = cantId;
            newDuration.Motivazione_Reg_Id = motivationId;
            newDuration.Rettifica_Durata = Convert.ToInt32(durationType == CorrectionTypeEnum.CorrectionMinus ? (-1) * duration.TotalMinutes : duration.TotalMinutes);

            // ritorno dell'arrotondamento generato
            return newDuration;
        }

        /// <summary>
        /// Dati i valori inputati in griglia si prepara e ritorna una registrazione d'entrata corrispondente.
        /// </summary>
        /// <param name="newValues">I nuovi valori in griglia da processare.</param>
        /// <param name="isUpdating">Se impostato a <c>true</c> allora si sta effettuando l'update di un record esistente.</param>
        /// <param name="oldRegE">La registrazione che la reg restituita andrà a sostituire.</param>
        /// <returns>
        /// La nuova registrazione popolata con i dati indicati in griglia
        /// </returns>
        public Reg GetRegEFromNewValues(OrderedDictionary newValues, Reg oldRegE/* = null*/, bool isUpdating = false)
        {
            //Prepara il Record della REG con i Dati ricevuti da Video (newValues)
            //NB: i Campi Data_Ora_Fis_E e U hanno l'Ora Nuova ma la Data Old (se clone) altrimenti la Data è 01/01/0100
            //perchè il campo Video ha solo l'Ora e non anche la Data -> occorre inizializzarne la parte di Data con la Data Reg
            Reg_V regVStub = null;
            Reg currentRegE = RepoManager.RegRepo.Init();
            //recupera la Data della Registrazione
            DateTime dayDate = Convert.ToDateTime(newValues[CommonService.GetPropertyName(() => regVStub.Data_Reg)]).Date;
            //Riceve come Nuovo Valore di Data_Ora_Fis_E l'Ora New ma la Data Old 
            DateTime DateTimeEFis = Convert.ToDateTime(newValues[CommonService.GetPropertyName(() => regVStub.Data_Ora_Fis_E)]);
            //Imposta nella Data_Ora_Fis_E la Data New
            DateTimeEFis = CommonService.ComputeDateTime(dayDate, DateTimeEFis);

            //Inizializza gli altri Campi della Registrazione (solo quelli che possono essere stati inseriti/modificati a video)
            var colIdToInsert = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Col_Id)]);
            if (colIdToInsert == 0)
                currentRegE.Col_Id = null;
            else
                currentRegE.Col_Id = colIdToInsert;

            var cantIdToInsert = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Cant_Id)]);
            if (cantIdToInsert == 0)
                currentRegE.Cant_Id = null;
            else
                currentRegE.Cant_Id = cantIdToInsert;

            currentRegE.Motivazione_Reg_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Motivazione_Reg_Id)]);
            if (currentRegE.Motivazione_Reg_Id == 0)
                currentRegE.Motivazione_Reg_Id = null;
            currentRegE.Note_Reg = Convert.ToString(newValues[CommonService.GetPropertyName(() => regVStub.Note_Reg)]);
            currentRegE.Registrazione_Tipo_Reg = Convert.ToBoolean(newValues[CommonService.GetPropertyName(() => regVStub.IsOnlyDuration)]) ? (int)RegTypeEnum.Duration : (int)RegTypeEnum.None;
            currentRegE.Registrazione_Data_Ora_Orig_Reg = currentRegE.Registrazione_Data_Ora_Fis_Reg = DateTimeEFis;
            currentRegE.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fis_Reg;
            currentRegE.Data_Registrazione_Reg = currentRegE.DataOraUltimaModifica_Reg = DateTime.UtcNow;
            currentRegE.Flag_EU_Reg = Convert.ToString(newValues[CommonService.GetPropertyName(() => regVStub.EntrataEU)]);

            // lo stato di default di una registrazione di sola duarata è abbinata
            currentRegE.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
            if (currentRegE.Registrazione_Tipo_Reg != (int)RegTypeEnum.Duration) {
                currentRegE.Registrazione_Stato_Reg = 0;
            }

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.MantainCoordinateModifiedRegs) == 1) {
                if (oldRegE != null) {
                    if (oldRegE.Registrazione_Lat_Orig != null && oldRegE.Registrazione_Long_Orig != null)
                    {
                        currentRegE.Registrazione_Lat_Orig = oldRegE.Registrazione_Lat_Orig;
                        currentRegE.Registrazione_Long_Orig = oldRegE.Registrazione_Long_Orig;
                    }
                }
            }


            // se sto processando una registrazione solo durata allora riporto la durata presa dalla vecchia registrazione
            currentRegE.Rettifica_Durata = null;
            if (currentRegE.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration)
            {
                currentRegE.Rettifica_Durata = Convert.ToInt32(CommonService.GetTimeFromHHMMString(Convert.ToString(newValues[CommonService.GetPropertyName(() => regVStub.Durata_Fis_HH_S)])).TotalMinutes);

                // se l'utente ha chiesto che la registazione abbia durata negativa e la registrazione è di sola durata e la stessar risulta
                if (newValues.Contains(CommonService.GetPropertyName(() => regVStub.RegistrationDurationNegative)))
                {
                    bool negativeDuration = Convert.ToBoolean(newValues[CommonService.GetPropertyName(() => regVStub.RegistrationDurationNegative)]);
                    if ((currentRegE.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration && negativeDuration && currentRegE.Rettifica_Durata >= 0) || (currentRegE.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration && !negativeDuration && currentRegE.Rettifica_Durata < 0))
                        currentRegE.Rettifica_Durata *= -1;
                }
            }

            // se la reg_v è marcata per essere bloccata allora si settano anche l'entrata come tale
            currentRegE.Registrazione_Bloccata = Convert.ToBoolean(newValues[CommonService.GetPropertyName(() => regVStub.Registrazione_Bloccata)]);

            // se è attiva la personalizzazione della valutazione dell'attviità si procede al suo popolamento
            int activityEvaluationCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.EnableActivityEvaluationEnum);
            if (activityEvaluationCustomization == (int)EnableActivityEvaluationEnum.Enabled)
                currentRegE.Activity_Evaluation = (int?)newValues[CommonService.GetPropertyName(() => regVStub.Activity_Evaluation)];

            if (isUpdating)
            {
                if (oldRegE != null)
                {
                    currentRegE.Pru_Id = oldRegE.Pru_Id;
                    currentRegE.Fru_Id = oldRegE.Fru_Id;
                    currentRegE.Custom_Data_Reg = oldRegE.Custom_Data_Reg;
                }
                else
                {
                    if (newValues[CommonService.GetPropertyName(() => regVStub.Pru_Id)] != null)
                        currentRegE.Pru_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Pru_Id)]);
                    if (newValues[CommonService.GetPropertyName(() => regVStub.Fru_Id)] != null)
                        currentRegE.Fru_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Fru_Id)]);
                }
            }

            return currentRegE;
        }

        /// <summary>
        /// Dati i valori inputati in griglia si prepara e ritorna una registrazione d'uscita corrispondente.
        /// </summary>
        /// <param name="newValues">I nuovi valori in griglia da processare.</param>
        /// <param name="isUpdating">Se impostato a <c>true</c> allora si sta effettuando l'update di un record esistente.</param>
        /// <param name="oldRegU">La registrazione che la reg restituita andrà a sostituire.</param>
        /// <returns>
        /// La nuova registrazione popolata con i dati indicati in griglia
        /// </returns>
        public Reg GetRegUFromNewValues(OrderedDictionary newValues, bool isUpdating = false, Reg oldRegU = null)
        {
            //Prepara il Record della REG con i Dati ricevuti da Video (newValues)
            //NB: i Campi Data_Ora_Fis_E e U hanno l'Ora Nuova ma la Data Old (se clone) altrimenti la Data è 01/01/0100
            //perchè il campo Video ha solo l'Ora e non anche la Data -> occorre inizializzarne la parte di Data con la Data Reg            
            Reg_V regVStub = null;
            Reg currentRegU = RepoManager.RegRepo.Init();

            //recupera la Data della Registrazione (che ANCHE per l'Uscita viene lasciata UGUALE a quella dell'Entrata(anche se Notturno)
            DateTime dayDate = Convert.ToDateTime(newValues[CommonService.GetPropertyName(() => regVStub.Data_Reg)]).Date;
            //Riceve come Nuovo Valore di Data_Ora_Fis_E l'Ora New ma la Data Old 
            DateTime DateTimeUFis = Convert.ToDateTime(newValues[CommonService.GetPropertyName(() => regVStub.Data_Ora_Fis_U)]);
            //Imposta nella Data_Ora_Fis_U la Data New                
            DateTimeUFis = CommonService.ComputeDateTime(dayDate, DateTimeUFis);

            //Inizializza gli altri Campi della Registrazione (anche se l'ora di Uscita non ci fosse li inizializzo lo stesso x la Check)
            currentRegU.Col_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Col_Id)]);
            currentRegU.Cant_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Cant_Id)]);
            currentRegU.Motivazione_Reg_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Motivazione_Reg_Id)]);
            if (currentRegU.Motivazione_Reg_Id == 0)
                currentRegU.Motivazione_Reg_Id = null;
            currentRegU.Note_Reg = Convert.ToString(newValues[CommonService.GetPropertyName(() => regVStub.Note_Reg)]);
            currentRegU.Registrazione_Tipo_Reg = (int)RegTypeEnum.None;
            currentRegU.Registrazione_Data_Ora_Orig_Reg = currentRegU.Registrazione_Data_Ora_Fis_Reg = DateTimeUFis;
            currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fis_Reg;
            currentRegU.Flag_EU_Reg = Convert.ToString(newValues[CommonService.GetPropertyName(() => regVStub.UscitaEU)]);

            // lo stato di default di una registrazione di sola duarata è abbinata
            currentRegU.Registrazione_Stato_Reg = 0;/*(int)RegStateEnum.Ass;*/

            // se la reg_v è marcata per essere bloccata allora si settano anche l'entrata come tale
            currentRegU.Registrazione_Bloccata = Convert.ToBoolean(newValues[CommonService.GetPropertyName(() => regVStub.Registrazione_Bloccata)]);

            if (isUpdating)
            {
                // sono impostati i valori di pru e fru (anche in modifica) solamente se la reg in elaborazione non è nuova
                if (oldRegU != null)
                {
                    currentRegU.Pru_Id = oldRegU.Pru_Id;
                    currentRegU.Fru_Id = oldRegU.Fru_Id;
                    currentRegU.Custom_Data_Reg = oldRegU.Custom_Data_Reg;
                }
                else
                {
                    if (newValues[CommonService.GetPropertyName(() => regVStub.Pru_Id)] != null)
                        currentRegU.Pru_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Pru_Id)]);
                    if (newValues[CommonService.GetPropertyName(() => regVStub.Fru_Id)] != null)
                        currentRegU.Fru_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Fru_Id)]);
                }

            }

            return currentRegU;
        }

        /// <summary>
        /// Dati i valori inputati in griglia si prepara e ritorna una registrazione d'uscita corrispondente.
        /// </summary>
        /// <param name="newValues">I nuovi valori in griglia da processare.</param>
        /// <param name="isUpdating">Se impostato a <c>true</c> allora si sta effettuando l'update di un record esistente.</param>
        /// <param name="oldRegU">La registrazione che la reg restituita andrà a sostituire.</param>
        /// <returns>
        /// La nuova registrazione popolata con i dati indicati in griglia
        /// </returns>
        public Reg GetRegUFromNewValuesCoordinates(OrderedDictionary newValues, Reg oldRegU, bool isUpdating = false)
        {
            //Prepara il Record della REG con i Dati ricevuti da Video (newValues)
            //NB: i Campi Data_Ora_Fis_E e U hanno l'Ora Nuova ma la Data Old (se clone) altrimenti la Data è 01/01/0100
            //perchè il campo Video ha solo l'Ora e non anche la Data -> occorre inizializzarne la parte di Data con la Data Reg            
            Reg_V regVStub = null;
            Reg currentRegU = RepoManager.RegRepo.Init();

            //recupera la Data della Registrazione (che ANCHE per l'Uscita viene lasciata UGUALE a quella dell'Entrata(anche se Notturno)
            DateTime dayDate = Convert.ToDateTime(newValues[CommonService.GetPropertyName(() => regVStub.Data_Reg)]).Date;
            //Riceve come Nuovo Valore di Data_Ora_Fis_E l'Ora New ma la Data Old 
            DateTime DateTimeUFis = Convert.ToDateTime(newValues[CommonService.GetPropertyName(() => regVStub.Data_Ora_Fis_U)]);
            //Imposta nella Data_Ora_Fis_U la Data New                
            DateTimeUFis = CommonService.ComputeDateTime(dayDate, DateTimeUFis);

            //Inizializza gli altri Campi della Registrazione (anche se l'ora di Uscita non ci fosse li inizializzo lo stesso x la Check)
            currentRegU.Col_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Col_Id)]);
            currentRegU.Cant_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Cant_Id)]);
            currentRegU.Motivazione_Reg_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Motivazione_Reg_Id)]);
            if (currentRegU.Motivazione_Reg_Id == 0)
                currentRegU.Motivazione_Reg_Id = null;
            currentRegU.Note_Reg = Convert.ToString(newValues[CommonService.GetPropertyName(() => regVStub.Note_Reg)]);
            currentRegU.Registrazione_Tipo_Reg = (int)RegTypeEnum.None;
            currentRegU.Registrazione_Data_Ora_Orig_Reg = currentRegU.Registrazione_Data_Ora_Fis_Reg = DateTimeUFis;
            currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fis_Reg;
            currentRegU.Flag_EU_Reg = Convert.ToString(newValues[CommonService.GetPropertyName(() => regVStub.UscitaEU)]);
            if (oldRegU != null) {
                if (oldRegU.Registrazione_Lat_Orig != null && oldRegU.Registrazione_Long_Orig != null) {
                    currentRegU.Registrazione_Lat_Orig = oldRegU.Registrazione_Lat_Orig;
                    currentRegU.Registrazione_Long_Orig = oldRegU.Registrazione_Long_Orig;
                }
            }

            // lo stato di default di una registrazione di sola duarata è abbinata
            currentRegU.Registrazione_Stato_Reg = 0;/*(int)RegStateEnum.Ass;*/

            // se la reg_v è marcata per essere bloccata allora si settano anche l'entrata come tale
            currentRegU.Registrazione_Bloccata = Convert.ToBoolean(newValues[CommonService.GetPropertyName(() => regVStub.Registrazione_Bloccata)]);

            if (isUpdating)
            {
                // sono impostati i valori di pru e fru (anche in modifica) solamente se la reg in elaborazione non è nuova
                if (oldRegU != null)
                {
                    currentRegU.Pru_Id = oldRegU.Pru_Id;
                    currentRegU.Fru_Id = oldRegU.Fru_Id;
                    currentRegU.Custom_Data_Reg = oldRegU.Custom_Data_Reg;
                }
                else
                {
                    if (newValues[CommonService.GetPropertyName(() => regVStub.Pru_Id)] != null)
                        currentRegU.Pru_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Pru_Id)]);
                    if (newValues[CommonService.GetPropertyName(() => regVStub.Fru_Id)] != null)
                        currentRegU.Fru_Id = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => regVStub.Fru_Id)]);
                }

            }

            return currentRegU;
        }

        #region Importazione timbrature

        /// <summary>
        /// Importa le registrazioni specificate nel database.
        /// </summary>
        /// <param name="regsNoGpsToImport">L'elenco delle registrazioni non gps da importare.</param>
        /// <param name="regsGpsToImport">L'elenco delle registrazioni gps da importare.</param>
        /// <returns>
        /// Una lista contenente gli eventuali errori riscontrati durante l'importazione.
        /// </returns>
        public List<KeyValuePair<string, string>> Import(string[] regsNoGpsToImport, string[] regsGpsToImport)
        {
            _log.Info("INIZIO FASE DI IMPORT.\n");
            //RepoManager.Reg_VRepo.InviaRitardi();

            List<KeyValuePair<String, String>> errors = null;

            BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(0, "Import Iniziato");

            // inizializzazione dell'utente e della data/ora di avvio dell'elaborazione
            _elaborateUserId = PowerWebContext.Current.User.Utenti_Id;
            _elaborateDateTime = DateTime.Now;

            var newRegs = new List<Reg>();
            List<Reg> regs = new List<Reg>();

            errors = new List<KeyValuePair<String, String>>();

            // aggiunta ai dati attuali delle eventuali timbrature non gps
            ManageElaborateMessageDictionaries(0, "Import : Fase di estrazione delle timbrature");

            #region PARSING NON-GPS REGS TXT

            _log.Info(String.Format("Parsing no-GPS regs in txt"));

            GetDataFromNonGpsLines(newRegs, errors, regsNoGpsToImport);

            _log.Info(String.Format("Parsing no-GPS regs in txt terminato"));

            #endregion

            #region PARSING GPS REGS TXT

            _log.Info(String.Format("Inizio Parsing GPS regs in txt"));

            GetDataFromGpsLines(newRegs, errors, regsGpsToImport);

            _log.Info(String.Format("Parsing GPS regs in txt terminato"));

            #endregion

            ManageElaborateMessageDictionaries(100, "Import : Fase di estrazione delle timbrature terminata");

            // al termine del computo si calcola la data massima e minima della registrazione
            DateTime minRegDate = newRegs.Any() ? newRegs.Min(reg => reg.Registrazione_Data_Ora_Fis_Reg) : DateTime.MinValue;
            DateTime maxRegDate = newRegs.Any() ? newRegs.Max(reg => reg.Registrazione_Data_Ora_Fis_Reg) : DateTime.MaxValue;

            #region DIZIONARIO NUMERO REG ESISTENTI PER GIORNO

            var dbDayDictionary = RepoManager.RegRepo.CountRegsFromDateRange(minRegDate, maxRegDate).ToDictionary(d => d.Date, c => c.Count);


            //Dictionary temporaneo contente il numero di registrazioni per giorno nella lista da importare
            var dayDictionaryToimportTmp = newRegs.OrderBy(x => x.Registrazione_Data_Ora_Fis_Reg)
                .GroupBy(d => d.Registrazione_Data_Ora_Fis_Reg.Date)
                .Select(g => new { Date = (g.Key), Count = g.Count() }).ToList();


            dayDictionaryToimportTmp.ForEach(dayCount =>
            {
                if (!dbDayDictionary.ContainsKey(dayCount.Date))
                    dbDayDictionary[dayCount.Date] = 0;
                dbDayDictionary[dayCount.Date] += dayCount.Count;
            });

            dbDayDictionary = dbDayDictionary.OrderBy(c => c.Key).ToDictionary(c => c.Key, d => d.Value);

            #endregion

            // se ci sono delle registrazioni da inserire a database
            if (newRegs.Any())
            {

                #region GESTIONE AGGIORNAMENTO DELLE DATE PER IL NOTTURNO

                // Le Ore dei Campi Date vengono sempre inizializzate a ZERO dal sistema
                // Occorre quindi selezionare SEMPRE x DATA MINORE della DATA con ORE ZERO del GG Successivo
                // Così vengono prese tutte le REG DEL GIORNO (per non mettere <= 23.59.59)  
                //Normalmente bastano quelle del Giorno (per cui i GG in più sono 1 per via dell'Ora 00:00:00)
                minRegDate = minRegDate.Date;
                maxRegDate = maxRegDate.Date.AddDays(1);

                // aggiustamento delle date in base al notturno configurato nell'applicativo
                BusinessService.ManageNocturneStartEndDate(ref minRegDate, ref maxRegDate);

                #endregion

                #region CONTATORI

                int toImportRegs = newRegs.Count;
                int checkedRegs = 0;
                int addedRegs = 0;
                int removedRegs = 0;

                #endregion

                #region ALGORITMO PARTIZIONAMENTO DATE

                var periods = GetPeriods(dbDayDictionary.Select(c => new RegsByDay { Date = c.Key, Count = c.Value }).OrderBy(c => c.Date).ToList());

                #endregion

                #region STRUTTURE DATI PER IMPORTAZIONE E CONTROLLO

                HashSet<Reg> existingRegsDic = null; //Dictionary utilizzato per incrementare la velocità di ricerca tra le registrazioni esistenti a database
                HashSet<Reg> toAddRegs = null;   //Container ottimizzato per la ricerca tra le registrazioni esistenti ancora da importare (codice hash reg => reg )

                #endregion

                #region CONTROLLO DUPLICATI E INSERIMENTO

                #region VARIABILI PROGRESSBAR

                float step = (float)(100 * (float)(1 / (float)periods.Count));
                float progress = 0;
                int ciclo = 0;

                #endregion 

                foreach (var limit in periods)
                {
                    progress += step;
                    ciclo++;

                    ManageElaborateMessageDictionaries(progress, String.Format("Import : Fase {0} di {1}", ciclo, periods.Count));

                    _log.Info(String.Format("Ricerca registrazioni a database tra {0} e {1}.", limit.Key, limit.Value));

                    #region QUERY DATABASE REGISTRAZIONI ESISTENTI

                    //Ricerca registrazioni a dataBase (non viene usata una find in modo da delegare la query completamente al database (Select compresa), 
                    //viene generato un HashSet dalla lista che verrà usato per la ricerca dei duplicati)


                    existingRegsDic = new HashSet<Reg>(DbSet.AsNoTracking().Where(reg => reg.Registrazione_Data_Ora_Orig_Reg >= limit.Key && reg.Registrazione_Data_Ora_Orig_Reg < limit.Value && (reg.Pru_Id.HasValue || reg.Fru_Id.HasValue)).ToList());

                    _log.Info(String.Format("Inizializzato il dizionario delle registrazioni già esistenti con {0} reg.", existingRegsDic.Count));

                    #endregion

                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ImportDouble) == 1)
                    {
                        regs = newRegs.Where(reg => reg.Registrazione_Data_Ora_Orig_Reg >= limit.Key && reg.Registrazione_Data_Ora_Orig_Reg < limit.Value).ToList();
                        regs = AdjustFisRegByColG4(regs);
                        Context.BulkInsert(regs);

                        addedRegs += regs.Count;

                    }
                    else
                    {
                        #region PARTIZIONAMENTO REGISTRAZIONI TOTALI
                        //Viene creato un HashSet direttamente dalla lista delle registrazioni da inserire (durante la creazione della nuova struttura vengono automaticamente eliminati i duplicati)
                        toAddRegs = new HashSet<Reg>(newRegs.Where(reg => reg.Registrazione_Data_Ora_Fis_Reg >= limit.Key && reg.Registrazione_Data_Ora_Fis_Reg <= limit.Value).ToList());

                        #endregion

                        #region RICERCA DUPLICATI

                        /*
                         * La ricerca dei duplicati viene effettuata tramite una struttura di tipo HashSet<Reg> in quanto permette la ricerca in tempo O(1) ossia costante
                         * tramite l'utilizzo delle funzioni getHashCode() e equals(obj) di cui è stato fatto l'apposito override nel file Reg.cs (extension).
                         * L'HashSet non ammette duplicati (i controlli vengono effettuati tramite le funzioni sopra specificate).
                         * Dalle registrazioni da importare vengono rimosse tramite la funzione sottostante tutte le registrazioni contenute a database per quel
                         * determinato periodo.
                        */


                        removedRegs += toAddRegs.RemoveWhere(reg => existingRegsDic.Contains(reg));


                        #endregion

                        _log.Info(String.Format("{0} regs hanno superato il controllo duplicati tra {1} e {2}.", toAddRegs.Count, limit.Key, limit.Value));

                        checkedRegs += toAddRegs.Count;

                        _log.Info(String.Format("Fin'ora {0} regs su {1} hanno passato il controllo duplicati", checkedRegs, toImportRegs));

                        _log.Info(String.Format("Fin'ora {0} regs su {1} non hanno passato il controllo duplicati", removedRegs, toImportRegs));

                        int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ImportBlockedRegsEnum);

                        // personalizzazione per Mosaico
                        if (customizationVersion == (int)ImportBlockedRegsEnum.Manage)
                        {
                            #region Gestione impsotazione registrazioni bloccate

                            _log.Info("Personalizzazione ImportBlockedRegsEnum attiva; Inizio gestione registrazioni bloccate.");

                            // faccio una preelaborazione per scartare tutte le registrazioni per le quali non posso assegnare un collaboratore
                            // e inserire come bloccate quelle che hanno una data inferiore alla data massima già presente per quel collaboratore.

                            // calcolo tutte le associazioni pru/fru con scrittura sulle reg di col e cant
                            IEnumerable<KeyValuePair<string, string>> retErrors = new List<KeyValuePair<string, string>>();
                            retErrors = AssociatePruFru(toAddRegs);

                            var pruDict = RepoManager.PruRepo.GetAll(true).ToDictionary(p => p.Pru_Id);
                            var fruDict = RepoManager.FruRepo.GetAll(true).ToDictionary(f => f.Fru_Id);

                            // metto da parte per scriverle nel txt delle sospese le reg rimaste senza collaboratore
                            var regsToSuspend = toAddRegs.Where(reg => reg.Col_Id == null).ToList();

                            // se ci sono registrazioni con collaboratore non associato
                            if (regsToSuspend.Count > 0)
                            {
                                foreach (var reg in regsToSuspend)
                                {
                                    string primaMatricola = String.Empty;
                                    string secondaMatricola = String.Empty;
                                    string codicePru = pruDict[(int)reg.Pru_Id].Codice_Pru.Trim();
                                    string codiceFru = fruDict[(int)reg.Fru_Id].Codice_Fru.Trim();
                                    if (codicePru.Length == 5)
                                    {
                                        primaMatricola = codicePru;
                                        secondaMatricola = codiceFru;
                                    }
                                    else
                                    {
                                        primaMatricola = codiceFru;
                                        secondaMatricola = codicePru;
                                    }

                                    char flagEU = reg.Flag_EU_Reg != null ? reg.Flag_EU_Reg.ToCharArray().First() : '\0';

                                    // ricostruisco la riga originaria del txt in quanto devo aggiungerle al txt delle sospsese
                                    string originalLine = String.Format("{0};{1};{2};{3};{4};{5};{6};{7}",
                                        primaMatricola,
                                        secondaMatricola,
                                        reg.Registrazione_Data_Ora_Orig_Reg.Year,
                                        reg.Registrazione_Data_Ora_Orig_Reg.Month.ToString("00"),
                                        reg.Registrazione_Data_Ora_Orig_Reg.Day.ToString("00"),
                                        reg.Registrazione_Data_Ora_Orig_Reg.Hour.ToString("00"),
                                        reg.Registrazione_Data_Ora_Orig_Reg.Minute.ToString("00"),
                                        flagEU);


                                    errors.Add(new KeyValuePair<String, String>(String.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_MATRICOLA_X_NO_ASS_COL, codicePru)), originalLine));

                                }
                            }

                            // dalle reg da elaborare tolgo quelle senza collaboratore
                            // creo una lista dei collaboratori per i quali sto importando i nuovi txt
                            var colIds = toAddRegs.Where(reg => reg.Col_Id != null).Select(reg => reg.Col_Id).Distinct().ToList();

                            // ciclo su tutti i collaboratori
                            foreach (var colId in colIds)
                            {
                                // recupero la data/ora dell'ultima registrazione inserita nel db per quel collaboratore
                                bool hasRegs = DbSet.Any(reg => reg.Col_Id == colId);
                                DateTime maxDateReg = hasRegs ? DbSet.AsNoTracking().Where(reg => reg.Col_Id == colId).Max(reg => reg.Registrazione_Data_Ora_Fis_Reg) : DateTime.MinValue;

                                // se quel collaboratore aveva già almeno una reg nel database
                                if (maxDateReg != DateTime.MinValue)
                                {
                                    // vengono importate come bloccate tutte le reg per quel collaboratore che hanno una data
                                    // minore alla data massima preesistente per quel collaboratore
                                    toAddRegs.Where(reg => reg.Registrazione_Data_Ora_Fis_Reg <= maxDateReg && reg.Col_Id == colId).ToList()
                                        .ForEach(reg => reg.Registrazione_Bloccata = true);
                                }
                            }

                            #endregion
                        }

                        #region Aggiunta di un secondo per registrazioni consecutive di stesso collaboratore e stessa data e ora

                        // sezione di gestione dell'aggiunta dei secondi alle reg importate;
                        // tra le reg da importare sono recuperate tutte quelle che hanno una stessa data/ora
                        // e, secondo l'ordine di import, è aggiunto ad ognuna di esse un secondo.

                        _log.Info("Aggiunta di un secondo alle registrazioni da importare");

                        //if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.OrderElaborateRegByCant) == 0)
                        toAddRegs = AdjustFisRegByCol(toAddRegs);

                        _log.Info("Aggiunta di un secondo alle registrazioni da importare terminata");

                        #endregion

                        #region Inserimento a database delle nuove registrazioni importate

                        _log.Info(String.Format("Inizio inserimento database di {0} regs su {1}.", toAddRegs.Count, toImportRegs));

                        //Le reg vengono inserite tramite la procedura bulk SQL appartenente alla libreria ZZZ in modo da aumentare la velocità
                        Context.BulkInsert(toAddRegs);

                        addedRegs += toAddRegs.Count;

                        _log.Info(String.Format("Inserite con successo {0} regs.", toAddRegs.Count));

                        ManageElaborateMessageDictionaries(progress, String.Format("Import : Fase {0} di {1}", ciclo, periods.Count));

                        #endregion
                    }

                }

                _log.Info(String.Format("Inserite correttamente {0} registrazioni", addedRegs));
                _log.Info("FASE DI IMPORT COMPLETATA.\n");

                #endregion
                bool isToElaborate = true;

                isToElaborate = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AvoidElaborateOnImport) == 1 ? false : true;


                BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100, String.Format("Salvataggio Terminato, Avvio elaborazione", addedRegs));

                #region GESTIONE DEI PENDING ELAB

                // calcolo del from e to per il confronto rispetto alle reg ricevute
                DateTime from;
                DateTime to;

                // memorizzo le eventuali elaborazioni pendenti trovate come prese in carico
                var pendingElabs = RepoManager.PendingElabRepo.Find(pe => pe.ElaborateDate_PendingElab == null).ToList();

                // aggiorno il periodo da trattare con eventuali pending elab e, nel caso forzo il notturno e l'elaborazione
                bool pendingElabPresent = UpdatePeriodoWithPendingElabDates(minRegDate, maxRegDate, out from, out to);

                // solo se è stato aggiornato il periodo con il pending elab devo ritenere conto del notturno e forzare comunque l'elaborazione
                if (pendingElabPresent)
                {
                    isToElaborate = true;

                    // Le Ore dei Campi Date vengono sempre inizializzate a ZERO dal sistema
                    // Occorre quindi selezionare SEMPRE x DATA MINORE della DATA con ORE ZERO del GG Successivo
                    // Così vengono prese tutte le REG DEL GIORNO (per non mettere <= 23.59.59)  
                    //Normalmente bastano quelle del Giorno (per cui i GG in più sono 1 per via dell'Ora 00:00:00)
                    if (to <= DateTime.MaxValue.AddDays(-1))
                        to = to.Date.AddDays(1);

                    // gestione delle date di inizio/fine periodo in base alla configurazione del notturno
                    BusinessService.ManageNocturneStartEndDate(ref to, ref from);
                }

                #endregion

                _log.Info(String.Format("INIZIO FASE DI ELABORAZIONE TRA {0} E {1}.\n", from, to));

                List<KeyValuePair<string, string>> elabErrors = new List<KeyValuePair<string, string>>();

                #region Elaborazione delle timbrature

                if (isToElaborate && addedRegs > 0)
                {

                    int elaboratedRegs = 0;

                    List<Reg> toElaborateRegs = null;

                    DateTime fromChunk;
                    DateTime toChunk;

                    ciclo = 0;
                    step = (float)(100 * (float)(1 / (float)periods.Count));
                    progress = 0;


                    foreach (KeyValuePair<DateTime, DateTime> period in periods)
                    {

                        progress += step;
                        ciclo++;
                        ManageElaborateMessageDictionaries(progress - step, String.Format("Elaborate : Fase {0} di {1}", ciclo, periods.Count));

                        if (RepoManager.ParamRepo.First().Abilita_Notturno)
                        {
                            fromChunk = period.Key.Subtract(TimeSpan.FromDays(1));
                            toChunk = period.Value.AddDays(1);
                        }
                        else {
                            fromChunk = period.Key;
                            toChunk = period.Value;
                        }

                        GC.Collect();

                        _log.Info(String.Format("Ricerca registrazioni da elaborare tra {0} e {1}.", fromChunk, toChunk));

                        toElaborateRegs = Find(reg => reg.Registrazione_Data_Ora_Fis_Reg >= fromChunk && reg.Registrazione_Data_Ora_Fis_Reg < toChunk, true).ToList();

                        _log.Info(String.Format("Trovate {0} regs.", toElaborateRegs.Count));

                        elabErrors.AddRange(Elaborate(toElaborateRegs, fromChunk, toChunk, true, true, _elaborateUserId, _elaborateDateTime, application: ApplicationMessageEnum.Import));

                        elaboratedRegs += toElaborateRegs.Count;

                        _log.Info(String.Format("Regs tra {0} e {1} aggiornate correttamente.", fromChunk, toChunk));

                    }

                    _log.Info(String.Format("Elaborate correttamente {0} registrazioni", elaboratedRegs));

                    // segno come elaborati i periodi salvati predentemente 
                    if (pendingElabs.Any())
                    {
                        pendingElabs.ForEach(pe => pe.ElaborateDate_PendingElab = DateTime.Now);
                        RepoManager.PendingElabRepo.Context.BulkUpdate(pendingElabs);
                    }

                }

                #endregion

                _log.Info(String.Format("FASE DI ELABORAZIONE TRA {0} E {1} TERMINATA CORRETTAMENTE.\n", from, to));

                BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100, String.Format("Terminata Elaborazione", 1));
            }

            if (errors.Count > 0)
            {
                List<KeyValuePair<String, String>> errorsTabMessaggi = new List<KeyValuePair<String, String>>();

                errorsTabMessaggi.Add(new KeyValuePair<String, String>(FunctionMessageEnum.Import.ToString(), BusinessService.GetLocalizedString(PowerWebResources.LBL_IMPORT_TXT)));
                errors.ForEach(err => errorsTabMessaggi.Add(new KeyValuePair<string, string>(err.Value, err.Key)));
                RepoManager.Tab_MessaggiRepo.InsertMessages(errorsTabMessaggi, ApplicationMessageEnum.Import, FunctionMessageEnum.Import, _elaborateUserId, _elaborateDateTime);
            }

            return errors;
        }

        public IEnumerable<Reg> FindRegsByDataFis(DateTime from, DateTime to, bool tracking = true)
        {
            var result = Enumerable.Empty<Reg>();
            var source = DbSet.AsQueryable();

            if (!tracking)
                source = source.AsNoTracking();

            try
            {
                result = source.Where(reg => reg.Registrazione_Data_Ora_Fis_Reg >= from && reg.Registrazione_Data_Ora_Fis_Reg < to).ToList();

            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante il recupero delle reg tra {0} -- {1} : {2}", from, to, ex.Message);
            }

            return result;
        }

        #endregion
        /// <summary>
        /// Dato un dizionario <data-numero reg esistenti> genera una lista di
        /// keyValuePair contente "giorno-inizio,giorno-fine" secondo il parametro da database
        /// </summary>
        /// <param name="countByDay">Dizionario data-nRecords</param>
        /// <returns>Lista di chiave-valore giorno-inizio,giorno-fine</returns>
        public List<KeyValuePair<DateTime, DateTime>> GetPeriods(IEnumerable<RegsByDay> countByDay)
        {
            List<KeyValuePair<DateTime, DateTime>> periods = new List<KeyValuePair<DateTime, DateTime>>();

            if (countByDay.Count() > 0)
            {
                DateTime fromDate = countByDay.First().Date;
                DateTime toDate = countByDay.First().Date;

                int sum = 0;

                for (int i = 0; i < countByDay.Count(); i++)
                {
                    if (sum + countByDay.ElementAt(i).Count < RepoManager.ParamRepo.ParametersRow.MaxElab_ImportChunkSize)
                    {
                        sum += countByDay.ElementAt(i).Count;
                        toDate = countByDay.ElementAt(i).Date.AddDays(1);
                    }
                    else
                    {
                        periods.Add(new KeyValuePair<DateTime, DateTime>(fromDate, toDate));
                        sum = countByDay.ElementAt(i).Count;
                        fromDate = toDate;
                        toDate = fromDate.AddDays(1);
                    }
                }

                periods.Add(new KeyValuePair<DateTime, DateTime>(fromDate, toDate));
            }
            return periods;
        }

        #region Gestione importazione timbrature standard

        /// <summary>
        /// Recupera ed inserisce le registrazioni da aggiungere a database (non GPS) e gli errori di processo per le specifiche linee provenienti da file.
        /// </summary>
        /// <param name="regsToAdd">Le registrazioni che saranno aggiungte a database e su cui saranno concatenate le nuove registrazioni GPS.</param>
        /// <param name="processErrors">Gli errori di processo a cui saranno concatenati quelli eventualmente riscontrati nel trattamento delle registrazioni GPS.</param>
        /// <param name="nonGpsLines">L'elenco di linee con timbrature non GPS da processare.</param>
        private void GetDataFromNonGpsLines(ICollection<Reg> regsToAdd, ICollection<KeyValuePair<string, string>> processErrors, string[] nonGpsLines)
        {
            // La generazione delle timbrature standard per l'inserimento prevede, per ogni linea di dato proveniente dal file di testo:
            // 1. Il recupero dell'anagrafica del dispositivo (machine) che ha effettuato la timbratura nell'anagrafica PRU/FRU
            // 2. Il recupero dell'anagrafica del badge/tag (badge) che ha effettuato la timbratura nell'anagrafica PRU/PRU
            // 3. Il controllo di coerenza del dato ricevuto (badge e machine tutte presenti e in anagrafiche distinte)
            // 4. La generazione e l'aggiunta della registrazione all'elenco di reg da aggiungere

            // si procede all'elaborazione solamente se sono state passte delle timbrature
            if (nonGpsLines.Any())
            {
                if (!(nonGpsLines.First() == "")) {
                    // inizializzazione della personalizzazione che indica se autogenerare le attività di presidio all'uscita di specifici turni
                    AutoGeneratePresidiumActivityEnum presidiumCustomization = (AutoGeneratePresidiumActivityEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoGeneratePresidiumActivityEnum);

                    // se la customizzazione dei presidi risulta attiva, si recuperano anche i relativi parametri
                    List<string> presidiumTurns = new List<string>();
                    int presidiumCantId = 0;
                    if (presidiumCustomization == AutoGeneratePresidiumActivityEnum.Generate)
                    {
                        string presidiumTurnsTmp = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutoGeneratePresidiumActivityEnum, "PresidiumTurnCodes");
                        if (!String.IsNullOrEmpty(presidiumTurnsTmp))
                            if (presidiumTurnsTmp.Contains("#"))
                                presidiumTurns = presidiumTurnsTmp.Split('#').ToList();
                            else
                                presidiumTurns.Add(presidiumTurnsTmp);

                        string presidiumCantCode = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutoGeneratePresidiumActivityEnum, "PresidiumCantCode");
                        Cant currentPresidiumCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Codice_Cantiere == presidiumCantCode);
                        if (currentPresidiumCant != default(Cant))
                            presidiumCantId = currentPresidiumCant.Cant_Id;
                    }

                    #region DIZIONARIO RICERCA PRU-FRU

                    RepoManager.FruRepo.Context.Configuration.LazyLoadingEnabled = false;
                    RepoManager.FruRepo.Context.Configuration.ProxyCreationEnabled = false;

                    var pruFruYetProcessed = RepoManager.FruRepo.GetAll(true).Cast<Object>().ToDictionary(x => x.GetType().GetProperty("Codice_Fru").GetValue(x, null).ToString().ToUpper());
                    pruFruYetProcessed = pruFruYetProcessed.Concat(RepoManager.PruRepo.GetAll(true).Cast<Object>().ToDictionary(x => x.GetType().GetProperty("Codice_Pru").GetValue(x, null).ToString().ToUpper())).ToDictionary(x => x.Key, x => x.Value);

                    RepoManager.FruRepo.Context.Configuration.LazyLoadingEnabled = true;
                    RepoManager.FruRepo.Context.Configuration.ProxyCreationEnabled = true;

                    #endregion

                    // inizializzazione della variabile che terrà traccia della registrazione precedente a quella attualmente in processo
                    PreReg previousPreReg = null;

                    // inizializzazione dell'ultima registazione senza informazioni aggiuntive processata per l'inserimento
                    Reg previousReg = null;

                    // ciclo di elaborazione di tutte le righe non commenti o vuote nel file
                    List<string> loopLines = nonGpsLines.Where(ln => !ln.StartsWith("*") && !String.IsNullOrEmpty(ln)).ToList();
                    foreach (var nonGpsLine in loopLines)
                    {

                        #region Recupero e normalizzazione dei dati di timbratura dalla riga

                        // calcolo dei dati della registrazione per la linea in elaborazione
                        var currentPreReg = new PreReg(nonGpsLine);

                        #endregion

                        // se si sta trattando una registrazione normale (no informazioni aggiuntive)
                        if (currentPreReg.AdditionalInfoType == AdditionalInfoEnum.None)
                        {
                            #region Recupero delle anagrafiche (PRU/FRU) del dispositivo

                            // salvataggio del codice badge corrente attualmente in processo
                            string originalBadgeCode = currentPreReg.BadgeCode;

                            // calcolo degli oggetti PRU/FRU corrispondenti ai codici della timbratura
                            object machineRegistry = GetPruFruRegistry(currentPreReg.DeviceCode, pruFruYetProcessed);
                            object badgeRegistry = GetPruFruRegistry(currentPreReg.BadgeCode, pruFruYetProcessed);

                            // se l'anagrafica del dispositivo è stata trovata allora si gestisce la sua modifica per eventuale
                            // presenza di causali sul fisso
                            machineRegistry = ManageDeviceActivityMachineRegistry(previousPreReg, currentPreReg, machineRegistry, pruFruYetProcessed);

                            #endregion

                            #region Convalida input dei dati di timbratura

                            // entrambe le matricole devono essere valorizzate, se anche solo una delle stesse non è stata trovata allora
                            // si segnala la linea attuale come errore e si passa al record successivo (l'errore viene anche riportato nel log)
                            if (machineRegistry == null)
                            {
                                string errorMessage = string.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PRIMA_MATRICOLA_INESISTENTE, currentPreReg.DeviceCode));
                                processErrors.Add(new KeyValuePair<string, string>(errorMessage, nonGpsLine));
                                Log.Warn(errorMessage);

                                continue;
                            }

                            if (badgeRegistry == null)
                            {
                                string errorMessage = string.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_SECONDA_MATRICOLA_INESISTENTE, currentPreReg.BadgeCode));
                                processErrors.Add(new KeyValuePair<string, string>(errorMessage, nonGpsLine));
                                Log.Warn(errorMessage);

                                continue;
                            }

                            // se entrambe le macchine sono fru potrebbe trattarsi della timbratura attività su una app configurata come dispositivo fisso;
                            // in questo caso, prima di scartare la timbratura è necessario ciclare sui record successivi alla stessa e verificare 
                            // se ci sono delle informazioni aggiuntive che indicano il dispositivo portatile di timbratura dell'attività;
                            // se così fosse il machine registry verrà sostituito con quanto trovato
                            if (machineRegistry is Fru && badgeRegistry is Fru)
                            {
                                string newMachineRegistry = PruCodeForActivity(loopLines.IndexOf(nonGpsLine) + 1, loopLines);

                                if (!string.IsNullOrEmpty(newMachineRegistry) && newMachineRegistry != "STOP")
                                    machineRegistry = GetPruFruRegistry(newMachineRegistry, pruFruYetProcessed);
                            }

                            // arrivato a questo punto si è certi che entrambe le matricole sono valorizzate e condizione essenziale
                            // affinché l'importazione possa avvenire è che le due matricole siano di anagrafiche differenti; se quindi le
                            // anagrafiche sono entrambe pru o fru allora si segnala l'errore e si passa alla linea successiva
                            if ((machineRegistry is Pru && badgeRegistry is Pru) || (machineRegistry is Fru && badgeRegistry is Fru))
                            {
                                string errorMessage = string.Format("*{0}|{1}|{2}", BusinessService.GetLocalizedString(PowerWebResources.ERR_PRIMA_SECONDA_MATRICOLA_STESSA_ANAGRAFICA)
                                    , currentPreReg.DeviceCode, currentPreReg.BadgeCode);
                                processErrors.Add(new KeyValuePair<string, string>(errorMessage, nonGpsLine));

                                Log.Warn(errorMessage);

                                continue;
                            }


                            #endregion

                            #region Trasformazione delle anagrafiche generiche in Pru e Fru

                            // inizializzazione dell'anagrafica Pru e dell'anagrafica Fru della timbratura:
                            // - se la matricola del dispositivo è una pru allora è lei la pru, altrimenti sicuramente il badge
                            // - se la matricola del dispositivo è una fru allora è lei la fru, altrimenti sicuramente il badge
                            Pru regPru = (Pru)(machineRegistry is Pru ? machineRegistry : badgeRegistry);
                            Fru regFru = (Fru)(machineRegistry is Fru ? machineRegistry : badgeRegistry);
                            var motivation = RepoManager.Tab_DecodRepo.FirstOrDefault(m => m.Chiave_Tab == currentPreReg.Motivate);

                            #endregion

                            #region Generazione della reg e aggiunta della stessa all'elenco di reg da aggiungere

                            if (motivation == null)
                            {
                                regsToAdd.Add(new Reg
                                {
                                    Fru_Id = regFru.Fru_Id,
                                    Pru_Id = regPru.Pru_Id,
                                    Registrazione_Data_Ora_Fis_Reg = currentPreReg.RegistrationDateTime,
                                    Registrazione_Data_Ora_Fig_Reg = currentPreReg.RegistrationDateTime,
                                    Registrazione_Data_Ora_Orig_Reg = currentPreReg.RegistrationDateTime,
                                    Data_Registrazione_Reg = DateTime.UtcNow,
                                    DataOraUltimaModifica_Reg = DateTime.UtcNow,
                                    Flag_EU_Reg = currentPreReg.RegistrationDirection,
                                    Registrazione_Badge_Originale = currentPreReg.BadgeCode
                                });
                            }
                            else
                            {
                                if (currentPreReg.RegistrationDirection == "U")
                                {
                                    regsToAdd.Add(new Reg
                                    {
                                        Fru_Id = regFru.Fru_Id,
                                        Pru_Id = regPru.Pru_Id,
                                        Registrazione_Data_Ora_Fis_Reg = currentPreReg.RegistrationDateTime.AddSeconds(-1),
                                        Registrazione_Data_Ora_Fig_Reg = currentPreReg.RegistrationDateTime,
                                        Registrazione_Data_Ora_Orig_Reg = currentPreReg.RegistrationDateTime,
                                        Data_Registrazione_Reg = DateTime.UtcNow,
                                        DataOraUltimaModifica_Reg = DateTime.UtcNow,
                                        Flag_EU_Reg = currentPreReg.RegistrationDirection,
                                        Registrazione_Badge_Originale = currentPreReg.BadgeCode,
                                        Motivazione_Reg_Id = motivation.Tab_Decod_Id
                                    });
                                }
                                else
                                {
                                    regsToAdd.Add(new Reg
                                    {
                                        Fru_Id = regFru.Fru_Id,
                                        Pru_Id = regPru.Pru_Id,
                                        Registrazione_Data_Ora_Fis_Reg = currentPreReg.RegistrationDateTime.AddSeconds(1),
                                        Registrazione_Data_Ora_Fig_Reg = currentPreReg.RegistrationDateTime,
                                        Registrazione_Data_Ora_Orig_Reg = currentPreReg.RegistrationDateTime,
                                        Data_Registrazione_Reg = DateTime.UtcNow,
                                        DataOraUltimaModifica_Reg = DateTime.UtcNow,
                                        Flag_EU_Reg = currentPreReg.RegistrationDirection,
                                        Registrazione_Badge_Originale = currentPreReg.BadgeCode,
                                        Motivazione_Reg_Id = motivation.Tab_Decod_Id
                                    });
                                }

                            }




                            #endregion

                            // prima di procedere alla lavorazione del record successivo si procede al salvataggio della registrazione precedente
                            // sia quella con solo i dati di processo sia quella che sarà scritta a database. Si imposta la registrazione di processo precedente
                            // solamente se la stessa non risulta essere un'attività
                            if (!CommonService.IsActivityDeviceCode(originalBadgeCode))
                                previousPreReg = currentPreReg;

                            previousReg = regsToAdd.Last();
                        }
                        else // se si sta invece trattando una registrazione con informazioni aggiuntive...
                        {
                            // allora si procede all'inserimento del dato aggiuntivo sulla registrazione, se già impostata
                            if (previousReg != null)
                                switch (currentPreReg.AdditionalInfoType)
                                {
                                    case AdditionalInfoEnum.Turn:
                                        previousReg.Turno = currentPreReg.TurnCode;

                                        // se si è nel ciclo precedente* processando un'uscita, è richiesta la generazione delle attviità di presidio e la registazione è in un turno
                                        // tra quelli configurati
                                        // * si controlla la registrazione precedente in quanto i dati di turno sono scirtti successivamente alla registrazione principale, che rimane tale fino alla successiva
                                        //   registrazione "buona"
                                        if (previousReg.Flag_EU_Reg == "U" && presidiumCustomization == AutoGeneratePresidiumActivityEnum.Generate && !String.IsNullOrEmpty(previousReg.Turno) && presidiumTurns.Any(tCode => tCode == previousReg.Turno)
                                            && presidiumCantId != 0)
                                        {
                                            // ... allora si inserisce una nuova registrazione nell'elenco con la chiusura del presidio
                                            // prima dell'ultima registrazione
                                            Reg currentLastReg = regsToAdd.Last();
                                            regsToAdd.Add(new Reg
                                            {
                                                Fru_Id = currentLastReg.Fru_Id,
                                                Pru_Id = currentLastReg.Pru_Id,
                                                Registrazione_Data_Ora_Fis_Reg = currentLastReg.Registrazione_Data_Ora_Fis_Reg,
                                                Registrazione_Data_Ora_Fig_Reg = currentLastReg.Registrazione_Data_Ora_Fig_Reg,
                                                Registrazione_Data_Ora_Orig_Reg = currentLastReg.Registrazione_Data_Ora_Orig_Reg,
                                                Data_Registrazione_Reg = currentLastReg.Data_Registrazione_Reg,
                                                DataOraUltimaModifica_Reg = currentLastReg.DataOraUltimaModifica_Reg,
                                                Flag_EU_Reg = currentLastReg.Flag_EU_Reg,
                                                Registrazione_Badge_Originale = currentLastReg.Registrazione_Badge_Originale,
                                                Turno = currentLastReg.Turno,
                                                Sotto_Cantiere = currentLastReg.Sotto_Cantiere,
                                                Tipo_Attivita = currentLastReg.Tipo_Attivita
                                            });
                                            currentLastReg.Cant_Id = presidiumCantId;
                                            currentLastReg.Fru_Id = null;
                                            currentLastReg.Flag_EU_Reg = null;

                                            // ricalcolo l'ultima registrazione da processare
                                            previousReg = regsToAdd.Last();
                                        }
                                        break;

                                    //viene aggiunta l'informazione aggiunta sul sottocantiere
                                    case AdditionalInfoEnum.SubCant:
                                        previousReg.Sotto_Cantiere = currentPreReg.SubCantDesc;
                                        break;


                                    case AdditionalInfoEnum.ActivityType:
                                        previousReg.Tipo_Attivita = currentPreReg.ActivityTypeCode;
                                        break;

                                    case AdditionalInfoEnum.Squadra:
                                        //Aggiunge una registrazione per ogni PRU della squadra
                                        foreach (string pruCode in currentPreReg.SquadraArray)
                                        {
                                            //Controlla che la PRU esista
                                            Pru pru = GetPruFruRegistry(pruCode, pruFruYetProcessed) as Pru;

                                            if (pru == null)
                                            {
                                                //Se la PRU non esiste, si costruisce la riga di input specifica per la PRU corrente, in modo da poterla importare correttamente la prossima volta.
                                                string errorMessage = string.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PRIMA_MATRICOLA_INESISTENTE, pruCode));
                                                string[] splittedLine = nonGpsLine.Split(';');
                                                string[] errorLine = splittedLine.Take(8).ToArray();
                                                errorLine[0] = pruCode;

                                                processErrors.Add(new KeyValuePair<string, string>(errorMessage, string.Join(";", errorLine)));
                                                Log.Warn(errorMessage);
                                            }
                                            else
                                            {
                                                regsToAdd.Add(new Reg
                                                {
                                                    Fru_Id = previousReg.Fru_Id,
                                                    Pru_Id = pru.Pru_Id,
                                                    Registrazione_Data_Ora_Fis_Reg = previousReg.Registrazione_Data_Ora_Fis_Reg,
                                                    Registrazione_Data_Ora_Fig_Reg = previousReg.Registrazione_Data_Ora_Fig_Reg,
                                                    Registrazione_Data_Ora_Orig_Reg = previousReg.Registrazione_Data_Ora_Orig_Reg,
                                                    Data_Registrazione_Reg = previousReg.Data_Registrazione_Reg,
                                                    DataOraUltimaModifica_Reg = previousReg.DataOraUltimaModifica_Reg,
                                                    Flag_EU_Reg = previousReg.Flag_EU_Reg,
                                                    Registrazione_Badge_Originale = previousReg.Registrazione_Badge_Originale,
                                                    Turno = previousReg.Turno,
                                                    Sotto_Cantiere = previousReg.Sotto_Cantiere,
                                                    Tipo_Attivita = previousReg.Tipo_Attivita
                                                });
                                            }
                                        }
                                        break;

                                    case AdditionalInfoEnum.Note:
                                        previousReg.Note_Reg = currentPreReg.NoteReg;
                                        break;
                                }
                        }

                    }
                }
                
            }
        }

        /// <summary>
        /// Cerca un eventuale prossimo record di PRUFORACTIVITY nella lista specifica e ne ritorna eventualmente il valore (utilizza ricorsività).
        /// </summary>
        /// <param name="currentPosition">La posizione corrente in cui si sta cercando.</param>
        /// <param name="loopLines">l'elenco delle linee in cui ricercare.</param>
        /// <returns>La stringa con il valore ricercato: STOP (ricerca conclusa); String.Empty (continuare la ricerca); Valore: il valore trovato</returns>
        private string PruCodeForActivity(int currentPosition, List<string> loopLines)
        {
            string returnValue = String.Empty;

            if (currentPosition < loopLines.Count)
            {
                string currentLine = loopLines.ElementAt(currentPosition);
                PreReg nextPreReg = new PreReg(currentLine);
                if (nextPreReg.AdditionalInfoType != AdditionalInfoEnum.None)
                {
                    if (nextPreReg.AdditionalInfoType == AdditionalInfoEnum.PruCodeForActivity)
                        returnValue = BusinessService.GetAdditionalInfoValue(currentLine.Split(';'));
                    else
                        returnValue = PruCodeForActivity(currentPosition + 1, loopLines);
                }
                else // ho incontrato una timbratura di non informazioni aggiuntive
                    returnValue = "STOP";
            }
            else
                returnValue = "STOP"; // fine dell'elenco di ricerca

            return returnValue;
        }

        /// <summary>
        /// Recupera e restituisce l'anagrafica PRU/FRU per lo specifico codice passato come parametro.
        /// </summary>
        /// <param name="pruFruCode">Il codice PRU/FRU da ricercare.</param>
        /// <param name="pruFruCache">La cache degli elementi già processati.</param>
        /// <returns>L'oggetto contenente l'anagrafica PRU/FRU corrispondente al codice specificato.</returns>
        public object GetPruFruRegistry(string pruFruCode, Dictionary<string, object> pruFruCache)
        {
            // inizializzazione del valore di ritorno del metodo
            object returnValue = null;

            // se la cache non è inizializzata si procede alla sua inizializzazione
            if (pruFruCache == null)
                pruFruCache = new Dictionary<string, object>();

            // per prima cosa si ricerca la matricola nella cache e, se presente, si ritorna quel valore;
            // altrimenti si procede alla ricerca nel database
            if (pruFruCache.ContainsKey(pruFruCode.ToUpper()))
                returnValue = pruFruCache[pruFruCode.ToUpper()];
            else
                returnValue = null;

            // ritorno del valore calcolato dal metodo
            return returnValue;
        }

        public IEnumerable<RegsByDay> CountRegsFromDateRange(DateTime from, DateTime to)
        {
            IEnumerable<RegsByDay> result = Enumerable.Empty<RegsByDay>();

            try
            {
                result = DbSet.AsNoTracking().Where(r => r.Registrazione_Data_Ora_Fis_Reg >= from && r.Registrazione_Data_Ora_Fis_Reg < to)
                    .GroupBy(regs => DbFunctions.TruncateTime(regs.Registrazione_Data_Ora_Fis_Reg)).OrderBy(x => x.Key)
                    .Select(group => new RegsByDay { Date = (DateTime)group.Key, Count = group.Count() }).ToList();
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la fase di conteggio delle registrazioni per giorno : {0}", ex.Message);
            }

            return result;
        }

        public IEnumerable<int> GetRegsIdByDateRangeByColNotBlocked(DateTime from, DateTime to, int colId, bool tracking = true)
        {
            IEnumerable<int> result = Enumerable.Empty<int>();

            IQueryable<Reg> dataSet = DbSet;

            if (!tracking)
                dataSet = dataSet.AsNoTracking();


            try
            {
                result = dataSet.Where(reg => reg.Registrazione_Data_Ora_Fis_Reg >= from && reg.Registrazione_Data_Ora_Fis_Reg < to && !reg.Registrazione_Bloccata && reg.Col_Id == colId)
                            .Select(reg => reg.Reg_Id)
                            .ToList();
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore nella funzione {0} : {1}", nameof(GetRegsIdByDateRangeByColNotBlocked), ex.Message);
            }

            return result;
        }

        #endregion

        #region Gestione importazione timbrature GPS

        /// <summary>
        /// Recupera ed inserisce le registrazioni da aggiungere a database (formato GPS) e gli errori di processo per le specifiche linee provenienti da file.
        /// </summary>
        /// <param name="regsToAdd">Le registazioni che saranno aggiunte a database e sui cui saranno concatenate le nuove registrazioni GPS.</param>
        /// <param name="processErrors">Gli errori di processo a cui saranno concatenati quelli eventualmente riscontrati nel tattamento delle registrazioni GPS.</param>
        /// <param name="gpsLines">L'elenco di linee con timbrature GPS da processare.</param>
        private void GetDataFromGpsLines(ICollection<Reg> regsToAdd, ICollection<KeyValuePair<string, string>> processErrors, string[] gpsLines)
        {
            // TODO: procedurizzare con metodo di import normale
            // si procede ad elaborare i dati solamente se il modulo gps risulta attivo e ci sono delle timbrature gps da gestire
            if (RepoManager.ParamRepo.ParametersRow.Abilita_GPS)
            {
                /*
                * inizializzazione degli indici utilizzato per raggruppare i blocchi di timbrature GPS:
                * in caso di registrazione gps abbinata a tag il blocco per formare una singola reg a database è dato dalle seguenti linee:
                * 1. timbratura del tag
                * 2. timbratura di latitudine/longitudine
                * 3. timbratura di longitudine/latitudine (inverso rispetto a 2)
                * in caso invece di registrazione gps non abbinata a tag il blocco per formare una singola reg a database è dato dalle seguenti linee:
                * 1. timbratura di latitudine/longitudine
                * 2. timbratura di longitudine/latitudine (inverso rispetto a 1)
                */

                // inizializzazione del valore di configurazione che identifica il tipo di assegnazione da dare ai tag
                // in caso di timbrature tag e gps
                GpsAssTagTypeEnum tagAndGpsAssType = RepoManager.ParamRepo.ParametersRow.GpsAssTagType;

                // il count del numero di timbrature di un gruppo tag e gps
                const int tagAndGpsGroupCount = 3;

                // il count del numero di timbrature di un gruppo tag e gps
                const int onlyGpsGroupCount = 2;

                // l'elenco di righe che compongono un gruppo timbratura GPS
                var gpsRegGroup = new List<GpsPreReg>();

                // il tipo di gruppo gps in elaborazione (default utilizzato solo per evitare errore compilatore)
                var currentGroupType = GpsGruopTypeEnum.OnlyGps;

                // il flag EU del gruppo in esecuzione
                string currentGroupFlagEU = string.Empty;

                // il numero massimo di linee nel gruppo per la registrazione corrente
                int currentGroupMax = 0;

                int counterTag = 0;



                //Recupera il cantiere marcato come centro del raggio di lavoro
                Cant centro_gps = RepoManager.CantRepo.SingleOrDefault(cant => cant.Tipo_Cantiere_Can == "OPERATIVO");



                // si cicla su tutte le reg che non sono commenti e sono valorizzati
                foreach (string gpsLine in gpsLines.Where(ln => !ln.StartsWith("*") && !String.IsNullOrEmpty(ln)).ToList())
                {
                    if (gpsLine.Contains("NOTE")) {
                        Reg previousReg = regsToAdd.Last();
                        string[] splittedLies = gpsLine.Split(';');
                        previousReg.Note_Reg = splittedLies[10];
                        //regsToAdd.Remove(regsToAdd.Last());
                        //regsToAdd.Add(previousReg);
                    }
                    else {
                        try
                        {
                            bool activity = false;
                            // lettura dei dati di timbratura GPS
                            var currentGpsPreReg = new GpsPreReg(gpsLine);
                            if (gpsRegGroup.Count() > 0)
                            {
                                GpsPreReg previousReg = gpsRegGroup.Last();

                                if (previousReg.LineType == currentGpsPreReg.LineType)
                                {
                                    // se il gruppo gps è sopravvissuto al controllo di coerenza
                                    if (gpsRegGroup.Any())
                                    {
                                        #region Preparazione ed aggiunta della reg costruita sul gruppo

                                        // generazione della nuova reg GPS
                                        Reg newReg = Init();

                                        // impostazione dei dati diretti
                                        newReg.Registrazione_Data_Ora_Fis_Reg = gpsRegGroup.First().RegistrationDateTime;
                                        newReg.Registrazione_Data_Ora_Fig_Reg = gpsRegGroup.First().RegistrationDateTime;
                                        newReg.Registrazione_Data_Ora_Orig_Reg = gpsRegGroup.First().RegistrationDateTime;
                                        newReg.Data_Registrazione_Reg = DateTime.UtcNow;
                                        newReg.DataOraUltimaModifica_Reg = DateTime.UtcNow;


                                        // nelle registrazioni da gps la fru id è sempre a null
                                        newReg.Fru_Id = null;

                                        // l'unità portatile è data dal dispositivo in caso di timbratura solo GPS;
                                        // in caso invece di timbratura tag e GPS il dato dipende dalla configurazione:
                                        // - sarà la matricola del dispositivo in caso il tipo di assegnazione configurata sia Cant o non imposta
                                        // - sarà la matricola del tag in caso di tipo assegnazione a Col
                                        // - sarà la matricola del tag in caso di presenza anagrafica pru e assegnazione in base al tipo anagrafica; in caso
                                        //   non sia presente viene utilizzata la device
                                        string pruCode;
                                        string fruCode = "";

                                        if (currentGroupType == GpsGruopTypeEnum.TagActivityGps)
                                        {
                                            fruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup[1].BadgeCode, 10);
                                            pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().BadgeCode, 10);
                                        }
                                        else if (currentGroupType == GpsGruopTypeEnum.TagAndGps)
                                        {
                                            switch (tagAndGpsAssType)
                                            {
                                                case GpsAssTagTypeEnum.Col:
                                                    pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().BadgeCode, 10);
                                                    break;
                                                case GpsAssTagTypeEnum.CantCol:
                                                    pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().BadgeCode, 10);
                                                    if (!RepoManager.PruRepo.DbSet.Any(pru => pru.Codice_Pru == pruCode))
                                                        pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().DeviceCode, 10);
                                                    break;
                                                case GpsAssTagTypeEnum.Cant:
                                                    pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().DeviceCode, 10);
                                                    break;
                                                default:
                                                    pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().DeviceCode, 10);
                                                    break;
                                            }
                                        }
                                        else
                                            pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().DeviceCode, 10);

                                        // recupero della pru collegata al codice calcolato
                                        Pru currentPru = RepoManager.PruRepo.FirstOrDefault(pru => pru.Codice_Pru == pruCode);

                                        // si procede con la generazione della reg solamente se la pru è stata trovata,
                                        // altrimenti si segnala tutto il gruppo come errore
                                        if (currentPru != default(Pru))
                                        {
                                            // impostazione dell'identificativo pru sulla reg
                                            newReg.Pru_Id = currentPru.Pru_Id;

                                            // dal gruppo gps viene recuperato il valore della latitudine e della longitudine
                                            double latitudeToSearch = 0;
                                            double longitudeToSearch = 0;

                                            // inserimento delle coordinate gps originali nella timbratura
                                            newReg.Registrazione_Lat_Orig = latitudeToSearch;
                                            newReg.Registrazione_Long_Orig = longitudeToSearch;

                                            if (currentGroupFlagEU != null && currentGroupFlagEU != "")
                                            {
                                                newReg.Flag_EU_Reg = currentGroupFlagEU;
                                            }

                                            // viene normalizzato per la ricerca il codice del badge e si verifica la presenza dello stesso tra le fru
                                            string normalizedBadgeCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().BadgeCode, 10);

                                            if (currentGroupType == GpsGruopTypeEnum.TagActivityGps)
                                                normalizedBadgeCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup[1].BadgeCode, 10);

                                            bool isBadgeFru = RepoManager.FruRepo.DbSet.Any(fru => fru.Codice_Fru == normalizedBadgeCode);

                                            // se richiesto dai parametri, imposta la registrazione come passaggio
                                            if (RepoManager.ParamRepo.ParametersRow.Importazione_timbrature_GPS != null && RepoManager.ParamRepo.ParametersRow.Importazione_timbrature_GPS.Equals(ImportGPSRegsEnum.AsPass))
                                            {
                                                newReg.Registrazione_Tipo_Reg = (int)RegTypeEnum.Pass;
                                            }

                                            //Booleano per identificare se la registrazione è all'interno del raggio di lavoro (in combinazione con customization ImportGPSOnlyInWorkingRange)
                                            bool isRegInWorkingRange = true;

                                            //Se la customization che esclude dall'import le registrazioni che sono fuori dal raggio di lavoro è attiva, fa il controllo
                                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ImportGPSOnlyInWorkingRange) == (int)ImportGPSOnlyInWorkingRange.Active)
                                            {
                                                //Recupera il raggio di lavoro (in metri) dalla customization
                                                double radius = Double.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.ImportGPSOnlyInWorkingRange, "Radius"));

                                                //Controlla che il cantiere centro del raggio e le sue coordinate siano valide
                                                if (centro_gps != default(Cant) && centro_gps.LatitudineGps_Can != 0d && centro_gps.LongitudineGps_Can != 0d)
                                                {
                                                    //Crea il range di coordinate del raggio di lavoro
                                                    var range = new GpsRange(centro_gps.LatitudineGps_Can, centro_gps.LongitudineGps_Can, Convert.ToInt32(radius));
                                                    //Se la coordinata corrente non è all'interno del raggio di lavoro, la marca per l'esclusione
                                                    if (!range.IsPointInRange(latitudeToSearch, longitudeToSearch))
                                                    {
                                                        isRegInWorkingRange = false;
                                                    }
                                                }
                                            }

                                            //Importo le timbrature solo se sono all'interno del raggi di lavoro (customization ImportGPSOnlyInWorkingRange)
                                            if (isRegInWorkingRange)
                                            {
                                                if (currentGroupType == GpsGruopTypeEnum.TagActivityGps)
                                                {
                                                    var motivationId = RepoManager.Tab_DecodRepo.FirstOrDefault(m => m.Campo1_Tab == fruCode).Tab_Decod_Id;
                                                    newReg.Motivazione_Reg_Id = motivationId;
                                                }
                                                // l'anagrafica fissa viene impostata secondo la seguente logica :
                                                // - viene imposta l'unità fissa con l'anagrafica del tag se il tag è impostato per designare l'unità fissa o se l'anagrafica di appartenenza
                                                //   e il tag è presente tra i fru e il gruppo in elaborazione è tag e gps 
                                                // - altrimenti si procede ad impostare il cantiere utilizzando le coordinate GPS
                                                if (((tagAndGpsAssType == GpsAssTagTypeEnum.Cant) || (tagAndGpsAssType == GpsAssTagTypeEnum.CantCol && isBadgeFru))
                                                && currentGroupType == GpsGruopTypeEnum.TagAndGps)
                                                {

                                                    Fru currentFru = RepoManager.FruRepo.FirstOrDefault(fru => fru.Codice_Fru == normalizedBadgeCode);
                                                    if (currentFru != default(Fru))
                                                    {
                                                        newReg.Fru_Id = currentFru.Fru_Id;

                                                        // aggiunta della nuova reg all'elenco
                                                        regsToAdd.Add(newReg);
                                                    }
                                                    else
                                                    {
                                                        // segnalazione del gruppo come errore
                                                        string errorMessage = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_SECONDA_MATRICOLA_INESISTENTE, normalizedBadgeCode);
                                                        gpsRegGroup.ForEach(preReg => processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0}", errorMessage), preReg.OriginalGpsLine)));
                                                    }
                                                }

                                                else
                                                {
                                                    // calcolo del cantiere gps:
                                                    // - il valore del id cantiere sarà -1 se la latitudine e la longitudine da importare hanno valore 0 e non c'è un cantiere 'pozzo' (inserimento con cantiere vuoto)
                                                    // - il valore del id cantiere sarà 0 in caso bing non riesca a calcolare le coordinate
                                                    // - il valore del id cantiere sarà il valore del cantiere (nuovo o già presente) in caso di calcolo corretto da bing
                                                    int cantId = -1;
                                                    if (latitudeToSearch != 0 && longitudeToSearch != 0)
                                                    {
                                                        cantId = GetGpsCantId(latitudeToSearch, longitudeToSearch);
                                                    }

                                                    //Se le coordinate della regstrazione non sono valide (=0), assegna alla registrazione il cantiere pozzo, se valorizzato
                                                    else if (RepoManager.ParamRepo.ParametersRow.Cantiere_Timbrature_GPS_Non_Valide.HasValue && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CoordinateZero) == 0)
                                                    {
                                                        cantId = RepoManager.ParamRepo.ParametersRow.Cantiere_Timbrature_GPS_Non_Valide.Value;
                                                    }
                                                    else if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CoordinateZero) == 1 && (latitudeToSearch == 0 && longitudeToSearch == 0))
                                                    {
                                                        newReg = LastCant(currentPru.Pru_Id, gpsRegGroup.First().RegistrationDateTime, regsToAdd, newReg);
                                                        cantId = newReg.Cant_Id.Value;
                                                    }

                                                    // si imposta il cantiere gps solamente se è stato correttamente trovato;
                                                    // in caso contrario si procede a segnalare il gruppo come errore
                                                    if (cantId != 0)
                                                    {
                                                        newReg.Cant_Id = cantId == -1 ? (int?)null : cantId;

                                                        // aggiunta della registrazione all'elenco
                                                        regsToAdd.Add(newReg);
                                                    }
                                                    else
                                                        gpsRegGroup.ForEach(preReg =>
                                                            processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0} | {1}", BusinessService.GetLocalizedString(PowerWebResources.ERR_CANT_GPS_NON_CALCOLABILE), preReg.OriginalGpsLine), preReg.OriginalGpsLine)));


                                                }
                                            }
                                            //Se la timbratura non è nel raggio di lavoro, non la importo
                                            else
                                            {
                                                string errorMessage = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_REGISTRAZIONE_FUORI_DA_RAGGIO_LAVORO);
                                                gpsRegGroup.ForEach(preReg => processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0}", errorMessage), preReg.OriginalGpsLine)));
                                            }
                                        }
                                        else
                                        {
                                            // segnalazione del gruppo come errore
                                            string errorMessage = currentGroupType == GpsGruopTypeEnum.OnlyGps || tagAndGpsAssType != GpsAssTagTypeEnum.Col
                                                ? BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PRIMA_MATRICOLA_INESISTENTE, pruCode)
                                                : BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_SECONDA_MATRICOLA_INESISTENTE, pruCode);
                                            gpsRegGroup.ForEach(preReg => processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0}", errorMessage), preReg.OriginalGpsLine)));
                                        }


                                        #endregion

                                        #region Reinizializzazione gruppo per presa in carico nuove timbrature GPS

                                        // reinizializzazione del gruppo GPS
                                        gpsRegGroup = new List<GpsPreReg>();
                                        counterTag = 0;

                                        #endregion
                                    }
                                }
                            }
                            if (currentGpsPreReg.BadgeCode != null)
                                if (currentGpsPreReg.BadgeCode.Contains("ATTIV"))
                                    activity = true;


                            // se si sta processando un nuovo blocco gps
                            // allora si inizializzano i dati di gestione di un nuovo blocco gps
                            if (!gpsRegGroup.Any() || activity)
                            {
                                counterTag++;

                                #region Convalida della prima registrazione del gruppo

                                // se la prima registrazione del gruppo che si intende processare è indicata come referenziata a un tag ma non è un tag
                                // oppure 
                                // se la prima registrazione del gruppo che si intende processare è indicata come non referenziata a un tag ed è un tag
                                // allora si passa direttamente alla verifica del record successivo, riportando la corrente timbratura tra gli errori
                                if ((currentGpsPreReg.IsTagReferenced && !currentGpsPreReg.IsTag) || (!currentGpsPreReg.IsTagReferenced && currentGpsPreReg.IsTag))
                                {
                                    processErrors.Add(new KeyValuePair<string, string>(
                                        String.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PRIMA_REG_GPS_X_NON_COERENTE_CON_TIPO, gpsLine)),
                                        gpsLine));

                                    continue;
                                }

                                //Flag che indica se è presente il parametro del cantiere pozzo per le timbrature con coordinate non valide
                                bool isSinkAssigned = false;

                                //Controlla che il parametro del cantiere pozzo per le timbrature con coordinate non valide sia valorizzato
                                if (RepoManager.ParamRepo.ParametersRow.Cantiere_Timbrature_GPS_Non_Valide.HasValue)
                                {
                                    //Recupera il cantiere pozzo
                                    Cant sinkCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == RepoManager.ParamRepo.ParametersRow.Cantiere_Timbrature_GPS_Non_Valide.Value);

                                    //Se il cantiere pozzo esiste, valorizza il relativo flag
                                    if (sinkCant != default(Cant))
                                    {
                                        isSinkAssigned = true;
                                    }
                                }


                                // inoltre, se si sta processando una linea con dati gps, le coordinate devono essere valide.
                                // Viene riportato errore a meno che non sia stato indicato un cantiere 'pozzo' dove mettere le timbrature con coordinate non valide
                                // [l'errore di coordinate non valide non è bloccante, verrà inserita la registrazione senza cantiere e
                                // quindi è possibile proseguire con l'operazione]
                                //
                                if (!currentGpsPreReg.IsTag && !currentGpsPreReg.HasValidCoordinate && isSinkAssigned)
                                {
                                    // nella chiave dell'errore è segnalato il doppio * per evitare la riscrittura del dato tra le sospese
                                    processErrors.Add(new KeyValuePair<string, string>(
                                        String.Format("**{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_COORDINATE_GPS_NON_VALIDE, gpsLine)),
                                        gpsLine));
                                }

                                #endregion

                                #region Inizializzazione gruppo GPS

                                // numero massimo di registrazioni del gruppo
                                currentGroupMax = currentGpsPreReg.IsTagReferenced ? tagAndGpsGroupCount : onlyGpsGroupCount;

                                // se la prima registrazione del gruppo è referenziata a un tag allora il gruppo linee è di tipo tag e gps; altrimenti solo gps
                                currentGroupType = currentGpsPreReg.IsTagReferenced ? GpsGruopTypeEnum.TagAndGps : GpsGruopTypeEnum.OnlyGps;

                                currentGroupFlagEU = currentGpsPreReg.RegistrationDirection;

                                if (counterTag > 1)
                                {
                                    currentGroupMax++;
                                    currentGroupType = GpsGruopTypeEnum.TagActivityGps;
                                    counterTag = 0;
                                }

                                // aggiunta della prima registrazione al gruppo gps
                                gpsRegGroup.Add(currentGpsPreReg);

                                #endregion

                            }
                            else
                            {
                                // altrimenti, se il gruppo gps risulta già inizializzato si aggiunge la timbratura corrente al gruppo e si effettua il controllo di coerenza
                                // dei dati successivi al primo:
                                // - in caso d'errore si resetta il gruppo e si procede alla segnalazione come errore di tutte le timbrature del gruppo
                                // - in caso invece il gruppo risulti coerente si verifica se si è a fine corsa (numero massimo registrazioni per gruppo);
                                //   - in caso si sia a fine corsa genera la reg da scrivere e si azzera il gruppo;
                                //   - in caso invece non si sia a fine corsa si procede alla semplice aggiunta del record al gruppo

                                // aggiunta della linea gps al gruppo
                                gpsRegGroup.Add(currentGpsPreReg);
                                counterTag = 0;

                                #region Controllo di coerenza della linea GPS diversa dalla prima

                                // una linea gps successiva alla prima risulta coerente solamente se è una timbratura GPS (cioè non tag) coerente con il dato precedente:
                                // - in caso di gruppo solo gps:
                                //   - il dato deve essere opposto al precedente (latitudine se longitudine e viceversa)
                                // - in caso di gruppo tag e gps:
                                //    - la seconda e la terza timbratura devono essere tag referenced
                                //    - la seconda timbratura basta che non si tratti di un tag (controllo iniziale)
                                //    - la terza timbratura deve essere opposta alla precedente (latituine se longitudine e viceversa)

                                bool isLastGood = true;

                                bool hasInvalidCoordinates = false;

                                // se si tratta di una linea con timbratura tag allora la linea non è sicuramente coerente

                                if (currentGpsPreReg.IsTag)
                                    isLastGood = false;
                                else
                                {
                                    // si procede ad effettuare i controlli relativi al tipo di gruppo in elaborazione
                                    switch (currentGroupType)
                                    {
                                        case GpsGruopTypeEnum.TagAndGps: // registrazione tag e gps

                                            // per essere coerente tutte le timbrature devono essere tag referenced;
                                            // altrimenti, se si sta trattando la terza timbratura non può trattarsi dello stesso tipo linea della
                                            // precedente
                                            if (!currentGpsPreReg.IsTagReferenced)
                                                isLastGood = false;
                                            else if (gpsRegGroup.Count() == 3)
                                            {
                                                GpsPreReg prevGpsPreReg = gpsRegGroup.ElementAt(gpsRegGroup.IndexOf(currentGpsPreReg) - 1);

                                                if (prevGpsPreReg.LineType == currentGpsPreReg.LineType)
                                                    isLastGood = false;
                                            }

                                            break;
                                        case GpsGruopTypeEnum.OnlyGps: // registrazione solo gps

                                            // per essere coerente la timbratura successiva alla prima di un gruppo gps non può essere tag referenced;
                                            // altrimenti, se si sta trattando la seconda timbratura, non può trattarsi dello stesso tipo linea della 
                                            // precedente
                                            if (currentGpsPreReg.IsTagReferenced)
                                                isLastGood = false;
                                            else
                                            {
                                                GpsPreReg prevGpsPreReg = gpsRegGroup.ElementAt(gpsRegGroup.IndexOf(currentGpsPreReg) - 1);

                                                if (prevGpsPreReg.LineType == currentGpsPreReg.LineType)
                                                    isLastGood = false;
                                            }

                                            break;
                                        case GpsGruopTypeEnum.TagActivityGps: // registrazione tag + attività e gps

                                            // per essere coerente tutte le timbrature devono essere tag referenced;
                                            // altrimenti, se si sta trattando la terza timbratura non può trattarsi dello stesso tipo linea della
                                            // precedente
                                            if (!currentGpsPreReg.IsTagReferenced)
                                                isLastGood = false;
                                            else if (gpsRegGroup.Count() == 4)
                                            {
                                                GpsPreReg prevGpsPreReg = gpsRegGroup.ElementAt(gpsRegGroup.IndexOf(currentGpsPreReg) - 1);

                                                if (prevGpsPreReg.LineType == currentGpsPreReg.LineType)
                                                    isLastGood = false;
                                            }

                                            break;
                                    }

                                    // se il controllo precedente è andato a buon fine allora si procede a verificare anche che le eventuali coordinate siano valide
                                    if (isLastGood)
                                    {
                                        if (!currentGpsPreReg.IsTag && !currentGpsPreReg.HasValidCoordinate)
                                        {
                                            isLastGood = false;
                                            hasInvalidCoordinates = true;
                                        }
                                    }
                                }

                                // se la registrazione diversa dalla prima in processo risulta non coerente allora si segnalano tutte le linee del gruppo
                                // come errore e si inizializza il gruppo come nuovo (come se si ricominciasse da zero)
                                if (!isLastGood)
                                {
                                    // calcolo della stringa d'errore da riportare
                                    string errorMessage = String.Empty;
                                    if (hasInvalidCoordinates)
                                    {
                                        // se le coordinate della seconda (o terza) registrazione non sono valide allora si procede a segnalare l'errore tra i messaggi da processare
                                        // ma si segnala che la registrazione torna buona in quanto sarà importata senza cantiere
                                        // [nella chiave dell'errore è segnalato il doppio * per evitare la riscrittura del dato tra le sospese]
                                        errorMessage = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_COORDINATE_GPS_NON_VALIDE, currentGpsPreReg.OriginalGpsLine);
                                        processErrors.Add(new KeyValuePair<string, string>(String.Format("**{0}", errorMessage), currentGpsPreReg.OriginalGpsLine));
                                        isLastGood = true;
                                    }
                                    else if (gpsRegGroup.Count() == 2)
                                    {
                                        errorMessage = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_SECONDA_REG_GPS_X_NON_COERENTE, currentGpsPreReg.OriginalGpsLine);
                                    }
                                    else
                                        errorMessage = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_TERZA_REG_GPS_X_NON_COERENTE, currentGpsPreReg.OriginalGpsLine);

                                    // se non si tratta di un errore di coordinate
                                    // [se si tratta di un errore di coordinate si procede all'inserimento della registrazione con cantiere vuoto]
                                    if (!isLastGood)
                                    {
                                        // segnalazione degli errori nell'apposito dizionario
                                        gpsRegGroup.ForEach(preReg => processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0}", errorMessage), preReg.OriginalGpsLine)));

                                        // reinizializzazione del gruppo di modo da resettare il dato
                                        gpsRegGroup = new List<GpsPreReg>();
                                        gpsRegGroup.Add(currentGpsPreReg);
                                    }

                                }

                                #endregion

                                // se il gruppo gps è sopravvissuto al controllo di coerenza
                                if (gpsRegGroup.Any())
                                {
                                    // il gruppo attualmente in elaborazione è giunto a fine corsa?
                                    if (gpsRegGroup.Count() >= currentGroupMax)
                                    {

                                        #region Preparazione ed aggiunta della reg costruita sul gruppo

                                        // generazione della nuova reg GPS
                                        Reg newReg = Init();

                                        // impostazione dei dati diretti
                                        newReg.Registrazione_Data_Ora_Fis_Reg = gpsRegGroup.First().RegistrationDateTime;
                                        newReg.Registrazione_Data_Ora_Fig_Reg = gpsRegGroup.First().RegistrationDateTime;
                                        newReg.Registrazione_Data_Ora_Orig_Reg = gpsRegGroup.First().RegistrationDateTime;
                                        newReg.Data_Registrazione_Reg = DateTime.UtcNow;
                                        newReg.DataOraUltimaModifica_Reg = DateTime.UtcNow;


                                        // nelle registrazioni da gps la fru id è sempre a null
                                        newReg.Fru_Id = null;

                                        // l'unità portatile è data dal dispositivo in caso di timbratura solo GPS;
                                        // in caso invece di timbratura tag e GPS il dato dipende dalla configurazione:
                                        // - sarà la matricola del dispositivo in caso il tipo di assegnazione configurata sia Cant o non imposta
                                        // - sarà la matricola del tag in caso di tipo assegnazione a Col
                                        // - sarà la matricola del tag in caso di presenza anagrafica pru e assegnazione in base al tipo anagrafica; in caso
                                        //   non sia presente viene utilizzata la device
                                        string pruCode;
                                        string fruCode = "";

                                        if (currentGroupType == GpsGruopTypeEnum.TagActivityGps)
                                        {
                                            fruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup[1].BadgeCode, 10);
                                            pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().BadgeCode, 10);
                                        }
                                        else if (currentGroupType == GpsGruopTypeEnum.TagAndGps)
                                        {
                                            switch (tagAndGpsAssType)
                                            {
                                                case GpsAssTagTypeEnum.Col:
                                                    pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().BadgeCode, 10);
                                                    break;
                                                case GpsAssTagTypeEnum.CantCol:
                                                    pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().BadgeCode, 10);
                                                    if (!RepoManager.PruRepo.DbSet.Any(pru => pru.Codice_Pru == pruCode))
                                                        pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().DeviceCode, 10);
                                                    break;
                                                case GpsAssTagTypeEnum.Cant:
                                                    pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().DeviceCode, 10);
                                                    break;
                                                default:
                                                    pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().DeviceCode, 10);
                                                    break;
                                            }
                                        }
                                        else
                                            pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().DeviceCode, 10);

                                        // recupero della pru collegata al codice calcolato
                                        Pru currentPru = RepoManager.PruRepo.FirstOrDefault(pru => pru.Codice_Pru == pruCode);

                                        // si procede con la generazione della reg solamente se la pru è stata trovata,
                                        // altrimenti si segnala tutto il gruppo come errore
                                        if (currentPru != default(Pru))
                                        {
                                            // impostazione dell'identificativo pru sulla reg
                                            newReg.Pru_Id = currentPru.Pru_Id;

                                            // dal gruppo gps viene recuperato il valore della latitudine e della longitudine
                                            GpsPreReg preRegLatitude = gpsRegGroup.First(preReg => preReg.LineType == GpsLineTypeEnum.Latitude);
                                            double latitudeToSearch = BusinessService.ConvertDeviceToBingLatitude(preRegLatitude.CoordinateValue, preRegLatitude.CoordinatesType);
                                            GpsPreReg preRegLongitude = gpsRegGroup.First(preReg => preReg.LineType == GpsLineTypeEnum.Longitude);
                                            double longitudeToSearch = BusinessService.ConvertDeviceToBingLongitude(preRegLongitude.CoordinateValue, preRegLatitude.CoordinatesType);

                                            // inserimento delle coordinate gps originali nella timbratura
                                            newReg.Registrazione_Lat_Orig = latitudeToSearch;
                                            newReg.Registrazione_Long_Orig = longitudeToSearch;

                                            if (currentGroupFlagEU != null && currentGroupFlagEU != "")
                                            {
                                                newReg.Flag_EU_Reg = currentGroupFlagEU;
                                            }

                                            // viene normalizzato per la ricerca il codice del badge e si verifica la presenza dello stesso tra le fru
                                            string normalizedBadgeCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().BadgeCode, 10);

                                            if (currentGroupType == GpsGruopTypeEnum.TagActivityGps)
                                                normalizedBadgeCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup[1].BadgeCode, 10);

                                            bool isBadgeFru = RepoManager.FruRepo.DbSet.Any(fru => fru.Codice_Fru == normalizedBadgeCode);

                                            // se richiesto dai parametri, imposta la registrazione come passaggio
                                            if (RepoManager.ParamRepo.ParametersRow.Importazione_timbrature_GPS != null && RepoManager.ParamRepo.ParametersRow.Importazione_timbrature_GPS.Equals(ImportGPSRegsEnum.AsPass))
                                            {
                                                newReg.Registrazione_Tipo_Reg = (int)RegTypeEnum.Pass;
                                            }

                                            //Booleano per identificare se la registrazione è all'interno del raggio di lavoro (in combinazione con customization ImportGPSOnlyInWorkingRange)
                                            bool isRegInWorkingRange = true;

                                            //Se la customization che esclude dall'import le registrazioni che sono fuori dal raggio di lavoro è attiva, fa il controllo
                                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ImportGPSOnlyInWorkingRange) == (int)ImportGPSOnlyInWorkingRange.Active)
                                            {
                                                //Recupera il raggio di lavoro (in metri) dalla customization
                                                double radius = Double.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.ImportGPSOnlyInWorkingRange, "Radius"));

                                                //Controlla che il cantiere centro del raggio e le sue coordinate siano valide
                                                if (centro_gps != default(Cant) && centro_gps.LatitudineGps_Can != 0d && centro_gps.LongitudineGps_Can != 0d)
                                                {
                                                    //Crea il range di coordinate del raggio di lavoro
                                                    var range = new GpsRange(centro_gps.LatitudineGps_Can, centro_gps.LongitudineGps_Can, Convert.ToInt32(radius));
                                                    //Se la coordinata corrente non è all'interno del raggio di lavoro, la marca per l'esclusione
                                                    if (!range.IsPointInRange(latitudeToSearch, longitudeToSearch))
                                                    {
                                                        isRegInWorkingRange = false;
                                                    }
                                                }
                                            }

                                            //Importo le timbrature solo se sono all'interno del raggi di lavoro (customization ImportGPSOnlyInWorkingRange)
                                            if (isRegInWorkingRange)
                                            {
                                                if (currentGroupType == GpsGruopTypeEnum.TagActivityGps)
                                                {
                                                    var motivationId = RepoManager.Tab_DecodRepo.FirstOrDefault(m => m.Campo1_Tab == fruCode).Tab_Decod_Id;
                                                    newReg.Motivazione_Reg_Id = motivationId;
                                                }
                                                // l'anagrafica fissa viene impostata secondo la seguente logica :
                                                // - viene imposta l'unità fissa con l'anagrafica del tag se il tag è impostato per designare l'unità fissa o se l'anagrafica di appartenenza
                                                //   e il tag è presente tra i fru e il gruppo in elaborazione è tag e gps 
                                                // - altrimenti si procede ad impostare il cantiere utilizzando le coordinate GPS
                                                if (((tagAndGpsAssType == GpsAssTagTypeEnum.Cant) || (tagAndGpsAssType == GpsAssTagTypeEnum.CantCol && isBadgeFru))
                                                && currentGroupType == GpsGruopTypeEnum.TagAndGps)
                                                {

                                                    Fru currentFru = RepoManager.FruRepo.FirstOrDefault(fru => fru.Codice_Fru == normalizedBadgeCode);
                                                    if (currentFru != default(Fru))
                                                    {
                                                        newReg.Fru_Id = currentFru.Fru_Id;
                                                        newReg.Registrazione_Lat_Orig = latitudeToSearch;
                                                        newReg.Registrazione_Long_Orig = longitudeToSearch;

                                                        // aggiunta della nuova reg all'elenco
                                                        regsToAdd.Add(newReg);
                                                    }
                                                    else
                                                    {
                                                        // segnalazione del gruppo come errore
                                                        string errorMessage = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_SECONDA_MATRICOLA_INESISTENTE, normalizedBadgeCode);
                                                        gpsRegGroup.ForEach(preReg => processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0}", errorMessage), preReg.OriginalGpsLine)));
                                                    }
                                                }

                                                else
                                                {
                                                    // calcolo del cantiere gps:
                                                    // - il valore del id cantiere sarà -1 se la latitudine e la longitudine da importare hanno valore 0 e non c'è un cantiere 'pozzo' (inserimento con cantiere vuoto)
                                                    // - il valore del id cantiere sarà 0 in caso bing non riesca a calcolare le coordinate
                                                    // - il valore del id cantiere sarà il valore del cantiere (nuovo o già presente) in caso di calcolo corretto da bing
                                                    int cantId = -1;
                                                    if (latitudeToSearch != 0 && longitudeToSearch != 0)
                                                    {
                                                        cantId = GetGpsCantId(latitudeToSearch, longitudeToSearch);
                                                    }

                                                    //Se le coordinate della regstrazione non sono valide (=0), assegna alla registrazione il cantiere pozzo, se valorizzato
                                                    else if (RepoManager.ParamRepo.ParametersRow.Cantiere_Timbrature_GPS_Non_Valide.HasValue && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CoordinateZero) == 0)
                                                    {
                                                        cantId = RepoManager.ParamRepo.ParametersRow.Cantiere_Timbrature_GPS_Non_Valide.Value;
                                                    }
                                                    else if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CoordinateZero) == 1 && (latitudeToSearch == 0 && longitudeToSearch == 0))
                                                    {
                                                        newReg = LastCant(currentPru.Pru_Id, gpsRegGroup.First().RegistrationDateTime, regsToAdd, newReg);
                                                        cantId = newReg.Cant_Id.Value;
                                                    }

                                                    // si imposta il cantiere gps solamente se è stato correttamente trovato;
                                                    // in caso contrario si procede a segnalare il gruppo come errore
                                                    if (cantId != 0)
                                                    {
                                                        newReg.Cant_Id = cantId == -1 ? (int?)null : cantId;

                                                        // aggiunta della registrazione all'elenco
                                                        regsToAdd.Add(newReg);
                                                    }
                                                    else
                                                        gpsRegGroup.ForEach(preReg =>
                                                            processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0} | {1}", BusinessService.GetLocalizedString(PowerWebResources.ERR_CANT_GPS_NON_CALCOLABILE), preReg.OriginalGpsLine), preReg.OriginalGpsLine)));


                                                }
                                            }
                                            //Se la timbratura non è nel raggio di lavoro, non la importo
                                            else
                                            {
                                                string errorMessage = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_REGISTRAZIONE_FUORI_DA_RAGGIO_LAVORO);
                                                gpsRegGroup.ForEach(preReg => processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0}", errorMessage), preReg.OriginalGpsLine)));
                                            }
                                        }
                                        else
                                        {
                                            // segnalazione del gruppo come errore
                                            string errorMessage = currentGroupType == GpsGruopTypeEnum.OnlyGps || tagAndGpsAssType != GpsAssTagTypeEnum.Col
                                                ? BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PRIMA_MATRICOLA_INESISTENTE, pruCode)
                                                : BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_SECONDA_MATRICOLA_INESISTENTE, pruCode);
                                            gpsRegGroup.ForEach(preReg => processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0}", errorMessage), preReg.OriginalGpsLine)));
                                        }


                                        #endregion

                                        #region Reinizializzazione gruppo per presa in carico nuove timbrature GPS

                                        // reinizializzazione del gruppo GPS
                                        gpsRegGroup = new List<GpsPreReg>();
                                        counterTag = 0;

                                        #endregion
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
                //nel caso in cui il counter sia ad 1 significa che il dispositivo non ha trasmesso le coordinate
                //in quel caso assegno delle coordinate a zero ed in seguito nel caso sia abilitato cerco l'ultimo cantiere
                if (counterTag == 1 && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.BadTxt) == 1) {
                   #region Preparazione ed aggiunta della reg costruita sul gruppo

                        // generazione della nuova reg GPS
                        Reg newReg = Init();

                        // impostazione dei dati diretti
                        newReg.Registrazione_Data_Ora_Fis_Reg = gpsRegGroup.First().RegistrationDateTime;
                        newReg.Registrazione_Data_Ora_Fig_Reg = gpsRegGroup.First().RegistrationDateTime;
                        newReg.Registrazione_Data_Ora_Orig_Reg = gpsRegGroup.First().RegistrationDateTime;
                        newReg.Data_Registrazione_Reg = DateTime.UtcNow;
                        newReg.DataOraUltimaModifica_Reg = DateTime.UtcNow;


                        // nelle registrazioni da gps la fru id è sempre a null
                        newReg.Fru_Id = null;

                        // l'unità portatile è data dal dispositivo in caso di timbratura solo GPS;
                        // in caso invece di timbratura tag e GPS il dato dipende dalla configurazione:
                        // - sarà la matricola del dispositivo in caso il tipo di assegnazione configurata sia Cant o non imposta
                        // - sarà la matricola del tag in caso di tipo assegnazione a Col
                        // - sarà la matricola del tag in caso di presenza anagrafica pru e assegnazione in base al tipo anagrafica; in caso
                        //   non sia presente viene utilizzata la device
                        string pruCode;
                        string fruCode = "";
                        switch (tagAndGpsAssType)
                        {
                            case GpsAssTagTypeEnum.Col:
                                pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().BadgeCode, 10);
                                break;
                            case GpsAssTagTypeEnum.CantCol:
                                pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().BadgeCode, 10);
                                if (!RepoManager.PruRepo.DbSet.Any(pru => pru.Codice_Pru == pruCode))
                                    pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().DeviceCode, 10);
                                break;
                            default:
                                pruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().DeviceCode, 10);
                                break;
                        }

                        // recupero della pru collegata al codice calcolato
                        Pru currentPru = RepoManager.PruRepo.FirstOrDefault(pru => pru.Codice_Pru == pruCode);

                        // si procede con la generazione della reg solamente se la pru è stata trovata,
                        // altrimenti si segnala tutto il gruppo come errore
                        if (currentPru != default(Pru))
                        {
                            // impostazione dell'identificativo pru sulla reg
                            newReg.Pru_Id = currentPru.Pru_Id;

                            // dal gruppo gps viene recuperato il valore della latitudine e della longitudine
                            double latitudeToSearch = 0;
                            double longitudeToSearch = 0;

                            // inserimento delle coordinate gps originali nella timbratura
                            newReg.Registrazione_Lat_Orig = latitudeToSearch;
                            newReg.Registrazione_Long_Orig = longitudeToSearch;

                            // viene normalizzato per la ricerca il codice del badge e si verifica la presenza dello stesso tra le fru
                            string normalizedBadgeCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup.First().BadgeCode, 10);

                            if (currentGroupType == GpsGruopTypeEnum.TagActivityGps)
                                normalizedBadgeCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(gpsRegGroup[1].BadgeCode, 10);

                            bool isBadgeFru = RepoManager.FruRepo.DbSet.Any(fru => fru.Codice_Fru == normalizedBadgeCode);

                            // se richiesto dai parametri, imposta la registrazione come passaggio
                            if (RepoManager.ParamRepo.ParametersRow.Importazione_timbrature_GPS != null && RepoManager.ParamRepo.ParametersRow.Importazione_timbrature_GPS.Equals(ImportGPSRegsEnum.AsPass))
                            {
                                newReg.Registrazione_Tipo_Reg = (int)RegTypeEnum.Pass;
                            }

                            //Booleano per identificare se la registrazione è all'interno del raggio di lavoro (in combinazione con customization ImportGPSOnlyInWorkingRange)
                            bool isRegInWorkingRange = true;

                            //Se la customization che esclude dall'import le registrazioni che sono fuori dal raggio di lavoro è attiva, fa il controllo
                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ImportGPSOnlyInWorkingRange) == (int)ImportGPSOnlyInWorkingRange.Active)
                            {
                                //Recupera il raggio di lavoro (in metri) dalla customization
                                double radius = Double.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.ImportGPSOnlyInWorkingRange, "Radius"));

                                //Controlla che il cantiere centro del raggio e le sue coordinate siano valide
                                if (centro_gps != default(Cant) && centro_gps.LatitudineGps_Can != 0d && centro_gps.LongitudineGps_Can != 0d)
                                {
                                    //Crea il range di coordinate del raggio di lavoro
                                    var range = new GpsRange(centro_gps.LatitudineGps_Can, centro_gps.LongitudineGps_Can, Convert.ToInt32(radius));
                                    //Se la coordinata corrente non è all'interno del raggio di lavoro, la marca per l'esclusione
                                    if (!range.IsPointInRange(latitudeToSearch, longitudeToSearch))
                                    {
                                        isRegInWorkingRange = false;
                                    }
                                }
                            }

                            //Importo le timbrature solo se sono all'interno del raggi di lavoro (customization ImportGPSOnlyInWorkingRange)
                            if (isRegInWorkingRange)
                            {
                                if (currentGroupType == GpsGruopTypeEnum.TagActivityGps)
                                {
                                    var motivationId = RepoManager.Tab_DecodRepo.FirstOrDefault(m => m.Campo1_Tab == fruCode).Tab_Decod_Id;
                                    newReg.Motivazione_Reg_Id = motivationId;
                                }
                                // l'anagrafica fissa viene impostata secondo la seguente logica :
                                // - viene imposta l'unità fissa con l'anagrafica del tag se il tag è impostato per designare l'unità fissa o se l'anagrafica di appartenenza
                                //   e il tag è presente tra i fru e il gruppo in elaborazione è tag e gps 
                                // - altrimenti si procede ad impostare il cantiere utilizzando le coordinate GPS
                                if (((tagAndGpsAssType == GpsAssTagTypeEnum.Cant) || (tagAndGpsAssType == GpsAssTagTypeEnum.CantCol && isBadgeFru))
                                && currentGroupType == GpsGruopTypeEnum.TagAndGps)
                                {

                                    Fru currentFru = RepoManager.FruRepo.FirstOrDefault(fru => fru.Codice_Fru == normalizedBadgeCode);
                                    if (currentFru != default(Fru))
                                    {
                                        newReg.Fru_Id = currentFru.Fru_Id;

                                        // aggiunta della nuova reg all'elenco
                                        regsToAdd.Add(newReg);
                                    }
                                    else
                                    {
                                        // segnalazione del gruppo come errore
                                        string errorMessage = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_SECONDA_MATRICOLA_INESISTENTE, normalizedBadgeCode);
                                        gpsRegGroup.ForEach(preReg => processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0}", errorMessage), preReg.OriginalGpsLine)));
                                    }
                                }

                                else
                                {
                                    // calcolo del cantiere gps:
                                    // - il valore del id cantiere sarà -1 se la latitudine e la longitudine da importare hanno valore 0 e non c'è un cantiere 'pozzo' (inserimento con cantiere vuoto)
                                    // - il valore del id cantiere sarà 0 in caso bing non riesca a calcolare le coordinate
                                    // - il valore del id cantiere sarà il valore del cantiere (nuovo o già presente) in caso di calcolo corretto da bing
                                    int cantId = -1;
                                    if (latitudeToSearch != 0 && longitudeToSearch != 0)
                                    {
                                        cantId = GetGpsCantId(latitudeToSearch, longitudeToSearch);
                                    }

                                    //Se le coordinate della regstrazione non sono valide (=0), assegna alla registrazione il cantiere pozzo, se valorizzato
                                    else if (RepoManager.ParamRepo.ParametersRow.Cantiere_Timbrature_GPS_Non_Valide.HasValue)
                                    {
                                        cantId = RepoManager.ParamRepo.ParametersRow.Cantiere_Timbrature_GPS_Non_Valide.Value;
                                    }

                                    // si imposta il cantiere gps solamente se è stato correttamente trovato;
                                    // in caso contrario si procede a segnalare il gruppo come errore
                                    if (cantId != 0)
                                    {
                                        newReg.Cant_Id = cantId == -1 ? (int?)null : cantId;

                                        // aggiunta della registrazione all'elenco
                                        regsToAdd.Add(newReg);
                                    }
                                    else
                                        gpsRegGroup.ForEach(preReg =>
                                            processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0} | {1}", BusinessService.GetLocalizedString(PowerWebResources.ERR_CANT_GPS_NON_CALCOLABILE), preReg.OriginalGpsLine), preReg.OriginalGpsLine)));
                                }
                            }
                            //Se la timbratura non è nel raggio di lavoro, non la importo
                            else
                            {
                                string errorMessage = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_REGISTRAZIONE_FUORI_DA_RAGGIO_LAVORO);
                                gpsRegGroup.ForEach(preReg => processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0}", errorMessage), preReg.OriginalGpsLine)));
                            }
                        }
                        else
                        {
                            // segnalazione del gruppo come errore
                            string errorMessage = currentGroupType == GpsGruopTypeEnum.OnlyGps || tagAndGpsAssType != GpsAssTagTypeEnum.Col
                                ? BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PRIMA_MATRICOLA_INESISTENTE, pruCode)
                                : BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_SECONDA_MATRICOLA_INESISTENTE, pruCode);
                            gpsRegGroup.ForEach(preReg => processErrors.Add(new KeyValuePair<string, string>(String.Format("*{0}", errorMessage), preReg.OriginalGpsLine)));
                        }


                        #endregion

                   #region Reinizializzazione gruppo per presa in carico nuove timbrature GPS

                   // reinizializzazione del gruppo GPS
                   gpsRegGroup = new List<GpsPreReg>();
                   counterTag = 0;

                   #endregion
                    
                }
            }
            else
            {
                // in caso non sia abilitato il servizio di gps tutte le linee passate come parametro (se presenti)
                // sono trattate come errore per essere posizionate nelle sospese
                if (gpsLines.Any())
                    gpsLines.Where(ln => !ln.StartsWith("*") && !String.IsNullOrEmpty(ln)).ToList()
                        .ForEach(ln =>
                            processErrors.Add(new KeyValuePair<string, string>(BusinessService.GetLocalizedString(PowerWebResources.ERR_MODULO_GPS_NON_ABILITATO_NO_PROCESSO), ln)));

            }
        }

        /// <summary>
        /// Recupera l'id del cantiere a partire dalla latitudine e longitudine specificata.
        /// </summary>
        /// <param name="latitudeToSearch">La latitudine di posizionamento per il recupero/generazione del cantiere.</param>
        /// <param name="longitudeToSearch">La latitudine di posizionamento per il recupero/generazione del cantiere.</param>
        /// <returns>
        /// L'id del cantiere da associare alle timbrature GPS; 0 in caso non sia stato possibile stabilire il cantiere
        /// </returns>
        private int GetGpsCantId(double latitudeToSearch, double longitudeToSearch)
        {
            // recupero l'id del cantiere nel cui raggio cade il punto più vicino
            int gpsCantId = BusinessService.GetClosestCantIdInRange(latitudeToSearch, longitudeToSearch);

            // se non è stato trovato un cantiere nel cui raggio cade il punto, si procede con la sua generazione
            if (gpsCantId == 0)
            {
                Cant newCant = RepoManager.CantRepo.InitNewGpsCant(latitudeToSearch, longitudeToSearch);

                if (newCant != default(Cant))
                {
                    // se è stato generato il cantiere lo si inserisce a database, sempre che non abbia errori di check,
                    // e si ritorna il suo id
                    string currentCantCode = newCant.Codice_Cantiere;
                    RepoManager.CantRepo.SetEntityBeforeAddOrUpdate(newCant);
                    Dictionary<string, string> errors = RepoManager.CantRepo.Check(newCant, true);

                    if (!errors.Any())
                    {
                        try
                        {
                            RepoManager.CantRepo.Add(newCant, true);
                            gpsCantId = newCant.Cant_Id;

                        }
                        catch (Exception ex)
                        {
                            _log.Error("Errore durante l'inserimento di un nuovo cantiere generato tramite GPS");
                        }


                    }
                    else
                    {
                        _log.Error(String.Format("Errori rilevati dalla procedura check durante la verifica di un cantiere GPS, guardare tabella messaggi"));
                    }
                }
            }

            return gpsCantId;
        }

        //Metodo che si attiva con la personalizzazione CoordinateZero
        //Questa personalizzazione nel caso in cui le coordinate arrivino a zero va a controllare se la timbratura con le coordinate zero è un uscita
        //Nel caso in cui sia un uscita va ad inserire il cantiere della timbratura d'entrata
        //In caso sia un entrata va ad assegnare il cantiere di base messo nei parametri dell'ambiente
        private Reg LastCant(int pru,DateTime data, ICollection<Reg> regs,Reg newReg)
        {
            //Imposto dei parametri di base nel caso il cantiere sia d'entrata
            int cantId = RepoManager.ParamRepo.ParametersRow.Cantiere_Timbrature_GPS_Non_Valide.Value;
            int tempCantId = RepoManager.ParamRepo.ParametersRow.Cantiere_Timbrature_GPS_Non_Valide.Value;
            double tempLat = 0;
            double tempLong = 0;
            //creo una lista con tutte le timbrature della matricola passata come parametro e nello stesso giorno, aggiungo la timbratura con coordinate a zero e le ordine per valore della reg
            List<Reg> regList = regs.Where(reg => (reg.Registrazione_Data_Ora_Fis_Reg.Month == newReg.Registrazione_Data_Ora_Fis_Reg.Month && reg.Registrazione_Data_Ora_Fis_Reg.Day == newReg.Registrazione_Data_Ora_Fis_Reg.Day) && reg.Pru_Id == pru).ToList();
            regList.Add(newReg);
            int i = 0;
            regList = regList.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();
            foreach (Reg reg in regList) {
                i++;
                //controllo se le coordinate sono a zero
                if (reg.Registrazione_Lat_Orig == 0 || reg.Registrazione_Long_Orig == 0)
                {
                    //nel caso vado a vedere se è un uscita controllando se l'indice è pari o dispari
                   if (i % 2 == 0)
                   {
                       newReg.Cant_Id = tempCantId;
                       newReg.Registrazione_Lat_Orig = tempLat;
                       newReg.Registrazione_Long_Orig = tempLong;
                       return newReg;
                   }
                   else
                   {
                       newReg.Cant_Id = RepoManager.ParamRepo.ParametersRow.Cantiere_Timbrature_GPS_Non_Valide.Value;
                       return newReg;
                   }
                }
                else {
                }
                //nel caso le coordinate non siano a zero cambio i valori da assegnare alla registrazione errata
                tempCantId = reg.Cant_Id.Value;
                tempLat = reg.Registrazione_Lat_Orig.Value;
                tempLong = reg.Registrazione_Lat_Orig.Value;
            }
            return newReg;
        }


        #endregion

        /// <summary>
        /// Gestisce la prima matricola da impostare sulla registrazione corrente se si tratta di un'attività e in base all'anagrafica attuale e registrazione precedente.
        /// </summary>
        /// <param name="prevReg">La registrazione precedente all'attuale.</param>
        /// <param name="currentReg">La registrazione che si sta processando.</param>
        /// <param name="currentDeviceRegistry">L'anagrafica corrente (PRU/FRU) riguardante il codice device della registrazione che si sta processando</param>
        /// <param name="pruFruYetProcessed">L'elenco delle pru/fru già processate; utilizzata come cache per velocizzare le operazioni di ricerca.</param>
        /// <returns>L'anagrafica pru/fru corrispondente alla device della registrazione in processo modificata se la gestione delle attività/causali lo richiede.</returns>
        private object ManageDeviceActivityMachineRegistry(IPreReg prevReg, IPreReg currentReg, object currentDeviceRegistry, Dictionary<string, object> pruFruYetProcessed)
        {
            // si aggiorna l'anagrafica della device della registrazione corrente se si verificano le seguenti condizioni
            // - il modulo delle attività risulta attivato ed esiste la registrazione precedente
            // - la registrazione ha un codice badge assimilabile alle attivtià
            // - il codice della device della registrazione corrente è una Fru
            // in questo caso la nuova anagrafica device della registrazione corrente è l'anagrafica badge della registrazione precedente;
            // questo permette in caso di selezione causali su dispositivo fisso di mantenere il contatto tra l'attività e il collaboratore;
            // non viene effettuato un controllo sullo stesso minutaggio della timbratura precedente perché può essere che le due timbrature (attività e registrazioni)
            // siano su un minuto differente

            // inizializzazione del valore di ritorno del metodo
            // (per default si restituisce la device registry passata come parametro)
            object returnDeviceRegistry = currentDeviceRegistry;

            // se il modulo delle attività risulta attivato
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Att && prevReg != null)
            {
                // se la registrazione ha un codice badge attività proveniente da dispositivo e
                // l'anagrafica della device corrente è una FRU allora
                // la device registry diventa il badge della registrazione precedente
                if (CommonService.IsActivityDeviceCode(currentReg.BadgeCode) && currentDeviceRegistry is Fru)
                    returnDeviceRegistry = GetPruFruRegistry(prevReg.BadgeCode, pruFruYetProcessed);
            }

            // ritorno dell'anagrafica device calcolata dal metodo
            return returnDeviceRegistry;

        }

        #region Private class data

        /// <summary>
        /// Interfaccia utilizzata per raccogliere i dati comuni tra i dati di raccolta registrazioni pre import
        /// </summary>
        private interface IPreReg
        {

            /// <summary>
            /// Recupera il codice della device.
            /// </summary>
            /// <value>
            /// Il codice della device.
            /// </value>
            string DeviceCode { get; }

            /// <summary>
            /// Recupera il codice del badge.
            /// </summary>
            /// <value>
            /// Il codice del badge.
            /// </value>
            string BadgeCode { get; }

            /// <summary>
            /// Recupera la data e ora della registrazione.
            /// </summary>
            /// <value>
            /// La data e ora della registrazione.
            /// </value>
            DateTime RegistrationDateTime { get; }

        }

        /// <summary>
        /// Classe che rappresenta i dati di una timbratura gps estrapolati da una linea del file di import
        /// </summary>
        private class GpsPreReg : IPreReg
        {

            #region Constructor

            /// <summary>
            /// Inizializza una nuova istanza della classe di tipo <see cref="GpsPreReg"/>.
            /// </summary>
            /// <param name="gpsLine">La linea gps da file di import da cui ricavare i dati dell'oggetto corrente.</param>
            public GpsPreReg(string gpsLine)
            {
                // salvataggio della linea originale di provenienza
                OriginalGpsLine = gpsLine;


                // split della linea sul punto e virgola
                string[] splittedLine = gpsLine.Split(';');


                // si calcola se la registrazione è un tag o meno;
                // si tratta di una registrazione tag quando i valori di tipo direzione coordinate e direzione coordinate (utlimi due valori) sono vuoti
                if (String.IsNullOrEmpty(splittedLine[8].Trim()) && String.IsNullOrEmpty(splittedLine[9].Trim()) || (splittedLine[8] == "U" || splittedLine[8] == "E") || String.IsNullOrEmpty(splittedLine[8].Trim()) && (splittedLine[9].Trim().Contains('*')))
                    IsTag = true;
                else
                    IsTag = false;

                // il primo valore dell'elenco è sempre il codice della device
                DeviceCode = splittedLine.First();

                // il secondo valore è i tag in caso di registrazione tag, altrimenti è il valore di coordinata
                if (IsTag)
                    BadgeCode = splittedLine[1];
                else
                    CoordinateValue = splittedLine[1];

                // i successivi 5 valori vanno a comporre la data/ora della registrazione
                RegistrationDateTime = new DateTime(Convert.ToInt32(splittedLine[2]), Convert.ToInt32(splittedLine[3])
                    , Convert.ToInt32(splittedLine[4]), Convert.ToInt32(splittedLine[5]), Convert.ToInt32(splittedLine[6]), 0);

                // il settimo valore indica se la registrazione corrente è abbinata ad un tag
                IsTagReferenced = Convert.ToInt32(splittedLine[7]) == 1;

                // impostazione del tipo sull'ottavo valore: se tag: tag; se 0: latitudine; se 1: longitudine
                if (IsTag)
                    LineType = GpsLineTypeEnum.Tag;
                else
                    LineType = Convert.ToInt32(splittedLine[8]) == 0 ? GpsLineTypeEnum.Latitude : GpsLineTypeEnum.Longitude;

                // impostazione del tipo direzione: se tag: none; altrimenti ilv alore alla nona posizione
                if (IsTag)
                    CoordinatesType = GpsLineCoordinatesDirectionEnum.None;
                else
                {
                    switch (splittedLine[9])
                    {
                        case "N":
                            CoordinatesType = GpsLineCoordinatesDirectionEnum.North;
                            break;
                        case "S":
                            CoordinatesType = GpsLineCoordinatesDirectionEnum.South;
                            break;
                        case "E":
                            CoordinatesType = GpsLineCoordinatesDirectionEnum.East;
                            break;
                        case "O":
                            CoordinatesType = GpsLineCoordinatesDirectionEnum.West;
                            break;
                        default:
                            CoordinatesType = GpsLineCoordinatesDirectionEnum.None;
                            break;
                    }

                    //Se non è nua registrazione tag, potrebbe avere l'informazione della direzione (E/U) - ClockApp
                    if (splittedLine.IsValidIndex(10) && !splittedLine[10].Contains('*'))
                    {
                        RegistrationDirection = splittedLine[10];
                    }
                    //Se non è nua registrazione tag, potrebbe avere l'informazione della direzione (E/U) - ClockApp
                    if (splittedLine.IsValidIndex(11) && String.IsNullOrEmpty(splittedLine[11]))
                    {
                        //RegistrationDirection = splittedLine[10];
                    }
                }
            }

            #endregion

            #region Properties

            /// <summary>
            /// Recupera o imposta il valore che indica se l'istanza corrnte è una timbratura su tag.
            /// </summary>
            /// <value>
            ///   <c>true</c> se l'istanza corrente è una timbratura su tag; altrimenti, <c>false</c>.
            /// </value>
            public bool IsTag { get; private set; }

            /// <summary>
            /// Recupera il codice della device.
            /// </summary>
            /// <value>
            /// Il codice della device .
            /// </value>
            public string DeviceCode { get; private set; }

            /// <summary>
            /// Recupera il codice del badge.
            /// </summary>
            /// <value>
            /// Il codice del badge.
            /// </value>
            public string BadgeCode { get; private set; }

            /// <summary>
            /// Recupera o imposta il valore stringa delle coordinate.
            /// </summary>
            /// <value>
            /// Il valore stringa delle coordinate.
            /// </value>
            public string CoordinateValue { get; private set; }

            /// <summary>
            /// Recupera la data e ora della registrazione.
            /// </summary>
            /// <value>
            /// La data e ora della registrazione.
            /// </value>
            public DateTime RegistrationDateTime { get; private set; }

            /// <summary>
            /// Recupera o imposta il valore che indica se l'istanza corrente è riferita a un tag o solo gps.
            /// </summary>
            /// <value>
            /// <c>true</c> se l'istanza corrente è riferita a un tag; altrimenti, <c>false</c>.
            /// </value>
            public bool IsTagReferenced { get; private set; }

            /// <summary>
            /// Recupera o imposta il tipo di linea.
            /// </summary>
            /// <value>
            /// Il tipo di linea.
            /// </value>
            public GpsLineTypeEnum LineType { get; private set; }

            /// <summary>
            /// Recupera o imposta il tipo delle coordinate.
            /// </summary>
            /// <value>
            /// Il tipo delle coordinat.
            /// </value>
            public GpsLineCoordinatesDirectionEnum CoordinatesType { get; private set; }

            /// <summary>
            /// Recupera o imposta la linea originale da cui sono estratti i dati della pre-registrazione.
            /// </summary>
            /// <value>
            /// La linea originale da cui sono estratti i dati della pre-registrazione.
            /// </value>
            public string OriginalGpsLine { get; private set; }

            /// <summary>
            /// Recupera o imposta il valore che indica se la timbratura corrente ha un valore coordinata valido o meno.
            /// </summary>
            /// <value>
            /// <c>true</c> se il valore coordinata risulta valido; altrimenti, <c>false</c>.
            /// </value>
            public bool HasValidCoordinate
            {
                get
                {
                    // le coordinate risultano valide se diverse da 0;
                    int coordinateIntValue = 0;
                    int.TryParse(CoordinateValue, out coordinateIntValue);

                    return coordinateIntValue != 0;
                }
            }

            /// <summary>
            /// Recupera o imposta la direzione della registrazione (E/U).
            /// </summary>
            /// <value>
            /// La direzione della registrazione (E/U).
            /// </value>
            public string RegistrationDirection { get; private set; }


            #endregion

        }

        /// <summary>
        /// Classe che rappresenta i dati di una timbratura non gps estrapolati da una linea del file di import
        /// </summary>
        private class PreReg : IPreReg
        {

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="PreReg"/> class.
            /// </summary>
            /// <param name="regLine">La linea da file di import da cui ricavare i dati dell'oggetto corrente.</param>
            public PreReg(string regLine)
            {
                // clacolo della personalizzazione utilizzata per determinare il tipo di import del flag E/U
                int euCustVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ImportFlagEUEnum);

                // split della linea da processare contenete i dati
                string[] splittedLine = regLine.Split(new Char[] { ';' }/*, StringSplitOptions.RemoveEmptyEntries*/);

                // popolamento dei dati di timbratura
                DeviceCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(splittedLine[0], 10); // codice apparecchio
                BadgeCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(splittedLine[1], 10); // codice del badge
                RegistrationDateTime = new DateTime(Convert.ToInt32(splittedLine[2]), Convert.ToInt32(splittedLine[3]),
                    Convert.ToInt32(splittedLine[4]), Convert.ToInt32(splittedLine[5]), Convert.ToInt32(splittedLine[6]), 00); // data e or timbratura
                RegistrationDirection = null; // direzione della timbratura

                if (regLine.Contains("[Motivazione]"))
                {
                    Motivate = splittedLine[8].Split('=')[1];
                }


                if (euCustVersion == (int)ImportFlagEUEnum.Import)
                {
                    //lunghezza uguale a 10 per registrazioni TAG e GPS con la selezione dell' entrata e dell'uscita
                    if (splittedLine.Length == 10)
                        RegistrationDirection = !String.IsNullOrEmpty(splittedLine[8].Trim()) ? splittedLine[8] : null;
                    else
                        RegistrationDirection = splittedLine.Length > 6 && !String.IsNullOrEmpty(splittedLine[7].Trim()) ? splittedLine[7] : null;
                }
                // se la timbratura contiene informazioni aggiuntive
                if (BusinessService.IsRegLineAdditionalInfo(splittedLine))
                {
                    AdditionalInfoType = BusinessService.AdditionalInfoType(splittedLine);

                    // si recupera il tipo dell'informazione aggiuntiva che si sta processando e in base a quello si aggiorna il dato aggiuntivo
                    // di riferimento
                    switch (AdditionalInfoType)
                    {
                        case AdditionalInfoEnum.Turn:
                            TurnCode = BusinessService.GetAdditionalInfoValue(splittedLine);
                            break;
                        case AdditionalInfoEnum.SubCant:
                            SubCantDesc = BusinessService.GetAdditionalInfoValue(splittedLine);
                            break;
                        case AdditionalInfoEnum.ActivityType:
                            ActivityTypeCode = BusinessService.GetAdditionalInfoValue(splittedLine);
                            break;
                        case AdditionalInfoEnum.Squadra:
                            SquadraArray = BusinessService.GetAdditionalInfoValueSquadra(splittedLine);
                            break;
                        case AdditionalInfoEnum.Note:
                            NoteReg = BusinessService.GetAdditionalInfoValue(splittedLine);
                            break;
                    }

                }
                else // se invece la timbratura è standard, si segnala che non ci sono informazioni aggiuntive
                    AdditionalInfoType = AdditionalInfoEnum.None;

            }

            #endregion

            #region Properties

            /// <summary>
            /// Recupera il codice della device.
            /// </summary>
            /// <value>
            /// Il codice della device .
            /// </value>
            public string DeviceCode { get; private set; }

            /// <summary>
            /// Recupera il codice del badge.
            /// </summary>
            /// <value>
            /// Il codice del badge.
            /// </value>
            public string BadgeCode { get; private set; }

            /// <summary>
            /// Recupera la data e ora della registrazione.
            /// </summary>
            /// <value>
            /// La data e ora della registrazione.
            /// </value>
            public DateTime RegistrationDateTime { get; private set; }

            /// <summary>
            /// Recupera la direzione della timbratura.
            /// </summary>
            /// <value>
            /// La direzione della timbratura.
            /// </value>
            public string RegistrationDirection { get; private set; }

            /// <summary>
            /// Recupera o imposta il valore del tipo di informazione aggiuntiva presente nella registrazione corrente; in caso
            /// la registrazione non sia di informazioni aggiuntive allora il valore sarà <see cref="AdditionalInfoEnum.None"/>.
            /// </summary>
            /// <value>
            //  Il valore del tipo di informazione aggiuntiva presente nella registrazione corrente; in caso
            /// la registrazione non sia di informazioni aggiuntive allora il valore sarà <see cref="AdditionalInfoEnum.None"/>.
            /// </value>
            public AdditionalInfoEnum AdditionalInfoType { get; set; }

            /// <summary>
            /// Recupera o imposta il valore che indica il codice del turno della presente regisrtazione; proprietà utilizzata
            /// solamente se <see cref="AdditionalInfoType"/> è valorizzata a <see cref="AdditionalInfoEnum.Turn"/>.
            /// </summary>
            /// <value>
            /// Il valore che indica il codice del turno della presente regisrtazione; proprietà utilizzata
            /// solamente se <see cref="AdditionalInfoType"/> <c>non</c> è valorizzata a <see cref="AdditionalInfoEnum.Turn"/>.
            /// </value>
            public string TurnCode { get; set; }

            /// <summary>
            /// Recupera o imposta il valore che indica la descrizione del sottocantiere; proprietà utilizzata
            /// solamente se <see cref="AdditionalInfoType"/> è valorizzata a <see cref="AdditionalInfoEnum.SubCant"/>.
            /// </summary>
            /// <value>
            /// Il valore che indica la descrizione del sottocantiere; proprietà utilizzata
            /// solamente se <see cref="AdditionalInfoType"/> <c>non</c> è valorizzata a <see cref="AdditionalInfoEnum.SubCant"/>.
            /// </value>
            public string SubCantDesc { get; set; }



            /// <summary>
            /// Recupera o imposta il valore che indica il codice del tipo attività della presente registrazione; proprietà utilizzata
            /// solamente se <see cref="AdditionalInfoType"/> è valorizzata a <see cref="AdditionalInfoEnum.ActivityType"/>.
            /// </summary>
            /// <value>
            /// Il valore che indica il codice del tipo attività della presente registrazione; proprietà utilizzata
            /// solamente se <see cref="AdditionalInfoType"/> è valorizzata a <see cref="AdditionalInfoEnum.ActivityType"/>.
            /// </value>
            public string ActivityTypeCode { get; set; }

            /// <summary>
            /// Recupera o imposta l'array di PRU a cui si riferisce una registrazione di squadra; proprietà utilizzata
            /// solamente se <see cref="AdditionalInfoType"/> è valorizzata a <see cref="AdditionalInfoEnum.Squadra"/>.
            /// </summary>
            /// <value>
            /// Il valore che indica il codice del tipo attività della presente registrazione; proprietà utilizzata
            /// solamente se <see cref="AdditionalInfoType"/> è valorizzata a <see cref="AdditionalInfoEnum.Squadra"/>.
            /// </value>
            public string[] SquadraArray { get; set; }

            public string Motivate { get; set; }

            public string NoteReg { get; set; }

            #endregion

        }



        #endregion

    }
}
#endregion