using BingMapsRESTToolkit;
using Business.ImportModules.CantImportModule.Factory;
using Business.MDBSchema;
using Business.Synchronization.SynchronizatioManager.Implementations;
using Common;
using Data;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Business.Repository.Custom
{
    public class CantRepository : GenericRepository<Cant>, ICantRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CantRepository));

        SynchronizationManager<Cant> synchronizationManager;


        public CantRepository(PowerWebEntities context, SynchronizationManager<Cant> synchronizationManager)
            : base(context)
        {
            this.synchronizationManager = synchronizationManager;
            this.synchronizationManager.AttachContext(context);
        }


        //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
        private static DateTime DataOraRecord;

        /// <summary>Ottiene le tabelle che deve usare in formato lista.
        /// Per ottimizzare la velocità salva l'oggetto nella sessione corrente
        /// in modo che la lista sia già in memoria quando viene richiesta più volte.
        /// </summary>
        private static List<Cli> Clis
        {
            get
            {

                List<Cli> oLista = PowerWebContext.GetFromSession<List<Cli>>("Clis_CantRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.CliRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cli>>("Clis_CantRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Tab_Comuni> Tab_Comunis
        {
            get
            {
                List<Tab_Comuni> oLista = PowerWebContext.GetFromSession<List<Tab_Comuni>>("Tab_Comunis_CantRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_ComuniRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Comuni>>("Tab_Comunis_CantRepo", oLista);
                }
                return oLista;
            }
        }
        public static List<Tab_Prov> Tab_Provs
        {
            get
            {
                List<Tab_Prov> oLista = PowerWebContext.GetFromSession<List<Tab_Prov>>("Tab_Provs_CantRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_ProvRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Prov>>("Tab_Provs_CantRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Tab_Decod> Tab_Decods
        {
            get
            {
                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_CantRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_DecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_CantRepo", oLista);
                }
                return oLista;
            }
        }

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Cli>>("Clis_CantRepo", null);
            PowerWebContext.SetToSession<List<Fil>>("Fils_CantRepo", null);
            PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_CantRepo", null);
            PowerWebContext.SetToSession<List<Tab_Comuni>>("Tab_Comunis_CantRepo", null);
            PowerWebContext.SetToSession<List<Tab_Prov>>("Tab_Provs_CantRepo", null);
        }

        public override Cant Init()
        {
            Cant oNewRecord = base.Init();
            oNewRecord.DisAbilitazione_Can = false;
            oNewRecord.Mensa_Can = false;
            oNewRecord.Singola_Reg = false;
            oNewRecord.Data_Registrazione_Can = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Can = DateTime.UtcNow;
            oNewRecord.Metodo_Arrotondamento_Can = 0;
            oNewRecord.Tipologia_Can = "CAN";
            oNewRecord.Tipo_Arrotondamento_Can = 0;

            // verifico se è attiva la personalizzazione riguardante l'init del flag gps nella generazione di un nuovo cantiere
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomFlagGpsCantInitEnum);

            // Se devo impostare il flag gps a 1...
            if (customizationVersion == (int)CustomFlagGpsCantInitEnum.FlagGPSTo1)
            {
                // ... allora lo imposto
                oNewRecord.FlagGps_Can = 1;
            }

            return oNewRecord;
        }

        public override int SaveChanges()
        {

            int savedEntities = 0;

            try
            {
                synchronizationManager.Collect();

                savedEntities = Context.SaveChanges();

                synchronizationManager.SynchronizeEntities();
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante il salvataggio dell'entità Cant a causa dell'exception {0}", ex.InnerException);
            }

            return savedEntities;
        }

        public override void Add(Cant entity, bool saveChanges = false)
        {

            base.Add(entity);

            if (saveChanges)
            {
                SaveChanges();
            }

        }

        public override void Add(IEnumerable<Cant> entities, bool saveChanges = false)
        {
            base.Add(entities);

            if (saveChanges)
            {
                SaveChanges();
            }
        }

        public override void BulkSaveChanges(Action<Z.BulkOperations.BulkOperation> action)
        {
            try
            {

                IEnumerable<Cant> addedEntries = null;

                IEnumerable<Dictionary<string, object>> modifiedValues = null;

                IEnumerable<int> deletedEntities = null;

                if (RepoManager.ParamRepo.ParametersRow.Sincronizzazione_ClockApp)
                {
                    //Vengono estratte dal context tutte le entità che non sono ancora state committate in base al loro stato (deleted modified....)

                    addedEntries = Context.ChangeTracker.Entries().Where(c => c.State == EntityState.Added).Select(c => c.Entity).OfType<Cant>().ToList();

                    var modifiedEntities = Context.ChangeTracker.Entries().Where(c => c.State == EntityState.Modified).ToList();

                    modifiedValues = GetModifiedProperties(modifiedEntities);

                    deletedEntities = Context.ChangeTracker.Entries().Where(c => c.State == EntityState.Deleted).Select(c => c.Entity).OfType<Cant>().Select(c => c.Cant_Id).ToList();
                }

                base.BulkSaveChanges(action);

                if (RepoManager.ParamRepo.ParametersRow.Sincronizzazione_ClockApp)
                {
                    //In base allo stato delle entità viene effettuata una differente operazione di sincronizzazione

                    if (addedEntries.Any())
                        BusinessService.ClockAppsAddEntities<Cant>(addedEntries);

                    if (modifiedValues.Any())
                        BusinessService.ClockAppUpdateValues<Cant>(modifiedValues);

                    if (deletedEntities.Any())
                        BusinessService.ClockAppDeleteEntities<Cant>(deletedEntities);
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante il salvataggio dell'entità Cant a causa dell'exception {0}", ex.InnerException);
            }


        }

        public override void BulkInsert(IEnumerable<Cant> entities)
        {
            base.BulkInsert(entities);

            //if (RepoManager.ParamRepo.ParametersRow.Sincronizzazione_ClockApp)
            //    BusinessService.ClockAppsAddEntities(entities);

        }

        public override void BulkUpdate(IEnumerable<Cant> entities)
        {
            base.BulkUpdate(entities);

            //if (RepoManager.ParamRepo.ParametersRow.Sincronizzazione_ClockApp)
            //    BusinessService.ClockAppUpdateValues(entities);

        }

        public override void BulkDelete(IEnumerable<Cant> entities)
        {
            base.BulkDelete(entities);

            //if (RepoManager.ParamRepo.ParametersRow.Sincronizzazione_ClockApp)
            //    BusinessService.ClockAppDeleteEntities<Cant>(entities.Select(c => c.Cant_Id).ToList());

        }

        public override void SetEntityBeforeAddOrUpdate(Cant entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Can;
            entity.DataOraUltimaModifica_Can = DateTime.UtcNow;
            entity.Codice_Cantiere = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Cantiere, 20);
            //nel caso degli assistiti allora forzo la descrizione cantiere uguale al valore dei campi cognome e nome assistito
            if (String.Compare((entity.Tipologia_Can ?? "").ToUpper(), "ASS", false) == 0)
                entity.Descrizione_Can = new StringBuilder(entity.Cognome_Assistito_Can).Append(" ").Append(entity.Nome_Assistito_Can).ToString().Trim();
            //Se è attiva la umerazione Automatica dei Cantieri in Param allora forzo il Codice Cantiere incrementandone il Codice dell'Ultimo Cantiere esistente

        }

        public override Dictionary<string, string> Check(Cant entity, bool isNew = false, bool isResetSession = true)
        {

            Dictionary<string, string> result = new Dictionary<string, string>();
            //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession();
            try
            {
                //Leggo la DataOraUltimaModifica ATTUALE dal REcord del DB per verificare che nessuno abbia modificato il Record nel frattempo               
                if (!isNew)
                {
                    DateTime DataOraRecordDb = RepoManager.CantRepo.Single(u => u.Cant_Id == entity.Cant_Id, true).DataOraUltimaModifica_Can;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_CAN));
                    }
                }
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                //Se è attivata la NUM AUT in Scheda PARAM allora cerco l'ultimo Cantiere esistente e lo incremento di 1 (dopo aver verifico che fosse numerico)
                // se non sono presenti cantieri in anagrafica si da per scontato che il valore sia numerico, così da eventualmente avviare la procedura
                var codCanMax = RepoManager.CantRepo.DbSet.Any() ? RepoManager.CantRepo.Max(c => c.Codice_Cantiere, true) : "0";
                if (RepoManager.ParamRepo.ParametersRow.Attiva_Num_Aut_Can)
                {
                    //verifico che l'Ultimo CANTIERE abbia Codice NUMERICO                                        
                    int CodCanMaxNum = 0;
                    if (int.TryParse(codCanMax, out CodCanMaxNum) == false)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cantiere),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_ULTIMO_CANTIERE_NON_NUMERICO, PowerWebResources.FLD_CODICE_CANTIERE));
                }
                // viene controllata l'obbligatorietà del codice cantiere solamente se non è richiesta la numerazione automatica
                if (String.IsNullOrEmpty(entity.Codice_Cantiere) && !RepoManager.ParamRepo.ParametersRow.Attiva_Num_Aut_Can)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cantiere),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_CANTIERE));
                else
                {
                    entity.Codice_Cantiere = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Cantiere, 20);
                    if (isNew)
                    {
                        if (RepoManager.CantRepo.FirstOrDefault(u => u.Codice_Cantiere == entity.Codice_Cantiere) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cantiere),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.CantRepo.FirstOrDefault(u => u.Codice_Cantiere == entity.Codice_Cantiere && u.Cant_Id != entity.Cant_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cantiere),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }

                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //
                if (CommonService.Nz(entity.Cli_Id, 0) != 0)
                {
                    if (Clis.SingleOrDefault(u => u.Cli_Id == entity.Cli_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cli_Id),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                            PowerWebResources.FLD_CODICE_CLIENTE, PowerWebResources.STR_CLIENTI));
                }
                else
                {
                    //Se il campo Cli_Id è obbligatorio, viene riportato l'errore
                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CantClientIdRequired) == (int)CantClientIdRequired.Required)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cli_Id),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CLI_ID));
                    }
                }

                if (CommonService.Nz(entity.Descrizione_Can, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Descrizione_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_DESCRIZIONE_CAN));

                //NEl caso degli Assistiti Verifico il Cognome e Nome dell'Assistito
                if (CommonService.Nz(entity.Tipologia_Can, "").ToUpper() == "ASS")
                {
                    if (CommonService.Nz(entity.Cognome_Assistito_Can, "") == "")
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cognome_Assistito_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_COGNOME_ASSISTITO_CAN));
                    //if (CommonService.Nz(entity.Nome_Assistito_Can, "") == "")
                    //    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Assistito_Can),
                    //      CommonServiceBiz.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOME_ASSISTITO_CAN));                
                }

                //Se viene impostato come ATTIVITA verifico 
                // che NON venga MAI Impostato il Campo FIL-ID
                // che non sia un passaggio
                // che sia Abilitata la Gestione Attività in PARAM
                if (CommonService.Nz(entity.Tipologia_Can, "").ToUpper() == "ATT")
                {
                    if (CommonService.Nz(entity.Fil_Id, 0) != 0)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fil_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_NON_SELEZIONABILE_IN_QUESTO_CONTESTO, PowerWebResources.FLD_FIL_ID));
                    }
                    if (CommonService.Nz(entity.Singola_Reg, false) == true)
                    // se si tratta di un'attività non si deve trattare di un Passaggio
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Singola_Reg),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_NON_SELEZIONABILE_IN_QUESTO_CONTESTO, PowerWebResources.FLD_SINGOLA_REG));
                    }
                    //Se in PARAM la gestione delle Attività NON è abilitata segnalo che il Cantiere NON può essere impostato ad Attività
                    if (RepoManager.ParamRepo.ParametersRow.Abilita_Att == false)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipologia_Can),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_GESTIONE_ATTIVITA_NON_ABILITATA, PowerWebResources.FLD_TIPOLOGIA_CAN));
                }

                if (CommonService.Nz(entity.Tipologia_Can, "") == "")
                {
                    if (CommonService.Nz(entity.Tipologia_Can, "") == "")
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipologia_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_TIPOLOGIA_CAN));
                }
                else
                {
                    entity.Tipologia_Can = entity.Tipologia_Can.ToUpper();
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab.ToUpper() == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab.ToUpper() == TabDecodNameEnum.TIPOLOGIA_CANTIERE.ToString() && x.Chiave_Tab.ToUpper() == entity.Tipologia_Can.ToUpper()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipologia_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPOLOGIA_CAN));
                }

                //Se in PARAM la gestione dei PAssaggi NON è abilitata verifico che il Cantiere non venga abilitato come Gestito a Passaggio
                if (CommonService.Nz(entity.Singola_Reg, false) == true && RepoManager.ParamRepo.ParametersRow.Abilita_Pass == false)
                {
                    if (CommonService.Nz(entity.Singola_Reg, false) == true)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Singola_Reg),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_GESTIONE_PASSAGGI_NON_ABILITATA, PowerWebResources.FLD_SINGOLA_REG));
                }


                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                if (CommonService.Nz(entity.Arrot_Durata_Can, 0) < CommonService.Nz(entity.Soglia_Durata_Can, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Soglia_Durata_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_SOGLIA_DURATA_CAN, PowerWebResources.FLD_ARROT_DURATA_CAN));
                if (CommonService.Nz(entity.Minuti_Tolleranza_Can, 0) < CommonService.Nz(entity.Soglia_Arrot_Fig_Can, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Soglia_Arrot_Fig_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_SOGLIA_ARROT_FIG_CAN, PowerWebResources.FLD_MINUTI_TOLLERANZA_CAN));
                if (CommonService.Nz(entity.Minuti_Tolleranza_F_Can, 0) < CommonService.Nz(entity.Soglia_Arrot_Fig_F_Can, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Soglia_Arrot_Fig_F_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_SOGLIA_ARROT_FIG_F_CAN, PowerWebResources.FLD_MINUTI_TOLLERANZA_F_CAN));
                if (CommonService.Nz(entity.Durata_Max_Ril_Can, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00) &&
                    CommonService.Nz(entity.Durata_Min_Ril_Can, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00))
                    if (CommonService.Nz(entity.Durata_Max_Ril_Can, new TimeSpan(00, 00, 00)) < CommonService.Nz(entity.Durata_Min_Ril_Can, new TimeSpan(00, 00, 00)))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Max_Ril_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                          PowerWebResources.FLD_DURATA_MAX_RIL_CAN, PowerWebResources.FLD_DURATA_MIN_RIL_CAN));
                if (CommonService.Nz(entity.Durata_Max_Gruppo_Ril_Can, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00) &&
                    CommonService.Nz(entity.Durata_Max_Ril_Can, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00))
                    if (CommonService.Nz(entity.Durata_Max_Gruppo_Ril_Can, new TimeSpan(00, 00, 00)) < CommonService.Nz(entity.Durata_Max_Ril_Can, new TimeSpan(00, 00, 00)))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Max_Gruppo_Ril_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                          PowerWebResources.FLD_DURATA_MAX_GRUPPO_RIL_CAN, PowerWebResources.FLD_DEFAULT_DURATA_MAX_GRUPPO_RIL));
                if (CommonService.Nz(entity.Durata_Min_Ril_Can, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00) &&
                    CommonService.Nz(entity.Durata_Max_Gruppo_Ril_Can, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00))
                    if (CommonService.Nz(entity.Durata_Min_Ril_Can, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Durata_Max_Gruppo_Ril_Can, new TimeSpan(00, 00, 00)))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Max_Gruppo_Ril_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                          PowerWebResources.FLD_DURATA_MAX_GRUPPO_RIL_CAN, PowerWebResources.FLD_DURATA_MIN_RIL_CAN));
                if (CommonService.Nz(entity.Data_Rapporto_Inizio_1_Can, new DateTime(1, 1, 1))
                > CommonService.Nz(entity.Data_Rapporto_Fine_1_Can, new DateTime(9999, 1, 1)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Inizio_1_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_INIZIO_X_DEVE_ESSERE_MINORE_DI_DATA_FINE_Y,
                      PowerWebResources.FLD_DATA_RAPPORTO_INIZIO_1_CAN, PowerWebResources.FLD_DATA_RAPPORTO_FINE_1_CAN));
                if (CommonService.Nz(entity.Numero_GG_Lavorativi, 0) > 31)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Numero_GG_Lavorativi),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_NUMERO_GG_LAVORATIVI) + "31");
                if (CommonService.Nz(entity.Arrot_Durata_Can, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrot_Durata_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_ARROT_DURATA_CAN, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Minuti_Tolleranza_Can, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Minuti_Tolleranza_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_MINUTI_TOLLERANZA_CAN, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Minuti_Tolleranza_F_Can, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Minuti_Tolleranza_F_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_MINUTI_TOLLERANZA_F_CAN, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Soglia_Arrot_Fig_Can, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Soglia_Arrot_Fig_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_SOGLIA_ARROT_FIG_CAN, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Soglia_Arrot_Fig_F_Can, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Soglia_Arrot_Fig_F_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_SOGLIA_ARROT_FIG_F_CAN, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Soglia_Durata_Can, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Soglia_Durata_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_SOGLIA_DURATA_CAN, PowerWebResources.VALORE_60));
                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Cap_Can, "") != "")
                    if (entity.Cap_Can.Length > 15)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cap_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CAP_CAN, PowerWebResources.VALORE_15));
                if (CommonService.Nz(entity.Cap_Nascita_Can, "") != "")
                    if (entity.Cap_Nascita_Can.Length > 15)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cap_Nascita_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CAP_NASCITA_CAN, PowerWebResources.VALORE_15));
                if (CommonService.Nz(entity.Cod_Fisc_Can, "") != "")
                    if (entity.Cod_Fisc_Can.Length > 16 && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DisabilitaCodiceFiscale16Caratteri) == (int)DisabilitaCodiceFiscale16Caratteri.Enabled)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cod_Fisc_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_COD_FISC_CAN, PowerWebResources.VALORE_16));
                if (CommonService.Nz(entity.Codice_Cantiere, "") != "")
                    if (entity.Codice_Cantiere.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cantiere),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_CANTIERE, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Codice_Luogo_Nascita_Can, "") != "")
                    if (entity.Codice_Luogo_Nascita_Can.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Luogo_Nascita_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_LUOGO_NASCITA_CAN, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Codice_Luogo_Residenza_Can, "") != "")
                    if (entity.Codice_Luogo_Residenza_Can.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Luogo_Residenza_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_LUOGO_RESIDENZA_CAN, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Codice_Voucher_Can, "") != "")
                    if (entity.Codice_Voucher_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Voucher_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_VOUCHER_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Cognome_Assistito_Can, "") != "")
                    if (entity.Cognome_Assistito_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cognome_Assistito_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_COGNOME_ASSISTITO_CAN, PowerWebResources.VALORE_50));

                if (CommonService.Nz(entity.Descrizione_Can, "") != "")
                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CanDescriptionOver50Enum) == (int)CanDescriptionOver50Enum.Disable)
                        //Se la descrizione supera i 50 caratteri, lo tronca e avanti col tram
                        if (entity.Descrizione_Can.Length > 50)
                        {
                            entity.Descrizione_Can = entity.Descrizione_Can.Substring(0, 50);
                        }
                /*
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Descrizione_Can),
                BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                PowerWebResources.FLD_DESCRIZIONE_CAN, PowerWebResources.VALORE_50));
                */
                if (CommonService.Nz(entity.Fax_1_Can, "") != "")
                    if (entity.Fax_1_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fax_1_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_FAX_1_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Fax_1_Rif_Can, "") != "")
                    if (entity.Fax_1_Rif_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fax_1_Rif_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_FAX_1_RIF_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Gestione_Can, "") != "")
                    if (entity.Gestione_Can.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Gestione_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_GESTIONE_CAN, PowerWebResources.VALORE_10));
                //Se l'indirizzo supera i 50 caratteri, lo tronca e avanti col tram
                if (CommonService.Nz(entity.Indirizzo_Can, "") != "")
                    if (entity.Indirizzo_Can.Length > 50)
                    {
                        entity.Indirizzo_Can = entity.Indirizzo_Can.Substring(0, 50);
                    }
                /*result.AddOrAppend(CommonService.GetPropertyName(() => entity.Indirizzo_Can),
                BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                PowerWebResources.FLD_INDIRIZZO_CAN, PowerWebResources.VALORE_50));*/
                if (CommonService.Nz(entity.Livello_Assistito_Can, "") != "")
                    if (entity.Livello_Assistito_Can.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Livello_Assistito_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_LIVELLO_ASSISTITO_CAN, PowerWebResources.VALORE_20));
                //Se il luogo supera i 50 caratteri, lo tronca e avanti col tram
                if (CommonService.Nz(entity.Luogo_Can, "") != "")
                    if (entity.Luogo_Can.Length > 50)
                    {
                        entity.Luogo_Can = entity.Luogo_Can.Substring(0, 50);
                    }
                /*result.AddOrAppend(CommonService.GetPropertyName(() => entity.Luogo_Can),
                BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                PowerWebResources.FLD_LUOGO_CAN, PowerWebResources.VALORE_50));*/
                if (CommonService.Nz(entity.Luogo_Nascita_Can, "") != "")
                    if (entity.Luogo_Nascita_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Luogo_Nascita_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_LUOGO_NASCITA_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Nazione_Can, "") != "")
                    if (entity.Nazione_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nazione_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NAZIONE_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Nazione_Nascita_Can, "") != "")
                    if (entity.Nazione_Nascita_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nazione_Nascita_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NAZIONE_NASCITA_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Nome_Assistito_Can, "") != "")
                    if (entity.Nome_Assistito_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Assistito_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NOME_ASSISTITO_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Nr_Isee_Can, "") != "")
                    if (entity.Nr_Isee_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nr_Isee_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NR_ISEE_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Provincia_Can, "") != "")
                    if (entity.Provincia_Can.Length > 4)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Provincia_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_PROVINCIA_CAN, PowerWebResources.VALORE_4));
                if (CommonService.Nz(entity.Provincia_Nascita_Can, "") != "")
                    if (entity.Provincia_Nascita_Can.Length > 4)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Provincia_Nascita_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_PROVINCIA_NASCITA_CAN, PowerWebResources.VALORE_4));
                if (CommonService.Nz(entity.Raggruppamento1_Can, "") != "")
                    if (entity.Raggruppamento1_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Raggruppamento1_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RAGGRUPPAMENTO1_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Raggruppamento2_Can, "") != "")
                    if (entity.Raggruppamento2_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Raggruppamento2_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RAGGRUPPAMENTO2_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Residenza_Interno_Can, "") != "")
                    if (entity.Residenza_Interno_Can.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Interno_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_INTERNO_CAN, PowerWebResources.VALORE_10));
                if (CommonService.Nz(entity.Residenza_Localita_Can, "") != "")
                    if (entity.Residenza_Localita_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Localita_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_LOCALITA_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Sesso_Can, "") != "")
                    if (entity.Sesso_Can.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Sesso_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_SESSO_CAN, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Telefono_1_Can, "") != "")
                    if (entity.Telefono_1_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_1_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_1_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Telefono_1_Rif_Can, "") != "")
                    if (entity.Telefono_1_Rif_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_1_Rif_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_1_RIF_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Telefono_2_Can, "") != "")
                    if (entity.Telefono_2_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_2_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_2_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Telefono_2_Rif_Can, "") != "")
                    if (entity.Telefono_2_Rif_Can.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_2_Rif_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_2_RIF_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Tipo_Calcolo_Viaggi_Can, "") != "")
                    if (entity.Tipo_Calcolo_Viaggi_Can.Length > 1)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Calcolo_Viaggi_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TIPO_CALCOLO_VIAGGI_CAN, PowerWebResources.VALORE_1));
                if (CommonService.Nz(entity.Tipo_Cantiere_Can, "") != "")
                    if (entity.Tipo_Cantiere_Can.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Cantiere_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TIPO_CANTIERE_CAN, PowerWebResources.VALORE_10));
                if (CommonService.Nz(entity.Tipo_Interv_Can, "") != "")
                    if (entity.Tipo_Interv_Can.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Interv_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TIPO_INTERV_CAN, PowerWebResources.VALORE_10));
                if (CommonService.Nz(entity.Tipologia_Can, "") != "")
                    if (entity.Tipologia_Can.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipologia_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TIPOLOGIA_CAN, PowerWebResources.VALORE_10));

                //5) verifico, per una serie di campi, che il valore del campo sia presente nella relativa Tabella 
                //
                //           
                if (RepoManager.ParamRepo.ParametersRow.Ctrl_Tab_Comuni != 0)
                // I Controlli sui CAP/LUOGHI/CODICI LUOGHI sono FATTI SOLO se è alzato il Flag CTRL_TAB_COMUNI in PARAM
                {
                    if (CommonService.Nz(entity.Cap_Can, "") != "")
                        if (Tab_Comunis.FirstOrDefault(u => u.Cap_Tab_Comuni == entity.Cap_Can) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cap_Can),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_CAP_CAN, PowerWebResources.STR_TAB_COMUNI));
                    if (CommonService.Nz(entity.Cap_Nascita_Can, "") != "")
                        if (Tab_Comunis.FirstOrDefault(u => u.Cap_Tab_Comuni == entity.Cap_Nascita_Can) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cap_Nascita_Can),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_CAP_NASCITA_CAN, PowerWebResources.STR_TAB_COMUNI));
                    if (CommonService.Nz(entity.Codice_Luogo_Nascita_Can, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Codice_Luogo_Tab_Comuni == entity.Codice_Luogo_Nascita_Can) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Luogo_Nascita_Can),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_CODICE_LUOGO_NASCITA_CAN, PowerWebResources.STR_TAB_COMUNI));
                    if (CommonService.Nz(entity.Codice_Luogo_Residenza_Can, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Codice_Luogo_Tab_Comuni == entity.Codice_Luogo_Residenza_Can) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Luogo_Residenza_Can),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_CODICE_LUOGO_RESIDENZA_CAN, PowerWebResources.STR_TAB_COMUNI));
                    if (CommonService.Nz(entity.Luogo_Nascita_Can, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Luogo_Tab_Comuni.ToLower() == entity.Luogo_Nascita_Can.ToLower()) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Luogo_Nascita_Can),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_LUOGO_NASCITA_CAN, PowerWebResources.STR_TAB_COMUNI));
                    if (CommonService.Nz(entity.Luogo_Can, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Luogo_Tab_Comuni.ToLower() == entity.Luogo_Can.ToLower()) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Luogo_Can),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_LUOGO_CAN, PowerWebResources.STR_TAB_COMUNI));
                }
                else
                // Altrimenti mi limito a controllarne solo la Lunghezza massima      
                {
                    if (CommonService.Nz(entity.Cap_Can, "") != "")
                        if (entity.Cap_Can.Length > 15)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cap_Can),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_CAP_CAN, PowerWebResources.VALORE_15));
                    if (CommonService.Nz(entity.Cap_Nascita_Can, "") != "")
                        if (entity.Cap_Nascita_Can.Length > 15)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cap_Nascita_Can),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_CAP_NASCITA_CAN, PowerWebResources.VALORE_15));
                    if (CommonService.Nz(entity.Codice_Luogo_Nascita_Can, "") != "")
                        if (entity.Codice_Luogo_Nascita_Can.Length > 20)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Luogo_Nascita_Can),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_CODICE_LUOGO_NASCITA_CAN, PowerWebResources.VALORE_20));
                    if (CommonService.Nz(entity.Codice_Luogo_Residenza_Can, "") != "")
                        if (entity.Codice_Luogo_Residenza_Can.Length > 20)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Luogo_Residenza_Can),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_CODICE_LUOGO_RESIDENZA_CAN, PowerWebResources.VALORE_20));

                }
                if (CommonService.Nz(entity.Flag_NON_Esportare_Can, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.FLAG_NON_ESPORTARE.ToString() && x.Chiave_Tab == entity.Flag_NON_Esportare_Can.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_NON_Esportare_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_FLAG_NON_ESPORTARE_CAN));
                if (CommonService.Nz(entity.Fil_Id, 0) != 0)
                {
                    // vado direttamente sul repository per evitare problemi di disallineamento in sessione
                    //if (Fils.SingleOrDefault(u => u.Fil_Id == entity.Fil_Id) == null)
                    if (RepoManager.FilRepo.DbSet.SingleOrDefault(u => u.Fil_Id == entity.Fil_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fil_Id),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                            PowerWebResources.FLD_CODICE_FILIALE, PowerWebResources.STR_FIL));
                }
                if (CommonService.Nz(entity.Livello_Assistito_Can, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.LIVELLO_CAN.ToString() && x.Chiave_Tab == entity.Livello_Assistito_Can) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Livello_Assistito_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_LIVELLO_ASSISTITO_CAN));
                if (RepoManager.ParamRepo.ParametersRow.Ctrl_Tab_Comuni != 0)
                // I Controlli sui CAP/LUOGHI/CODICI LUOGHI sono FATTI SOLO se è alzato il Falg CTRL_TAB_COMUNI in PARAM
                {
                    if (CommonService.Nz(entity.Luogo_Can, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Luogo_Tab_Comuni.ToLower() == entity.Luogo_Can.ToLower()) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Luogo_Can),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_LUOGO_CAN, PowerWebResources.STR_TAB_COMUNI));
                    if (CommonService.Nz(entity.Luogo_Nascita_Can, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Luogo_Tab_Comuni.ToLower() == entity.Luogo_Nascita_Can.ToLower()) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Luogo_Nascita_Can),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_LUOGO_NASCITA_CAN, PowerWebResources.STR_TAB_COMUNI));
                }
                else
                // Altrimenti mi limito a controllarne solo la Lunghezza massima    
                {
                    if (CommonService.Nz(entity.Luogo_Can, "") != "")
                        if (entity.Luogo_Can.Length > 50)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Luogo_Can),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                             PowerWebResources.FLD_LUOGO_CAN, PowerWebResources.VALORE_50));
                    if (CommonService.Nz(entity.Luogo_Nascita_Can, "") != "")
                        if (entity.Luogo_Nascita_Can.Length > 50)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Luogo_Nascita_Can),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                             PowerWebResources.FLD_LUOGO_NASCITA_CAN, PowerWebResources.VALORE_50));
                }
                if (CommonService.Nz(entity.Metodo_Arrotondamento_Can, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.METODO_ARROTONDAMENTO.ToString()
                    && x.Chiave_Tab == entity.Metodo_Arrotondamento_Can.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Metodo_Arrotondamento_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_METODO_ARROTONDAMENTO_CAN));
                //Il Metodo di Arrotondamento "9" può essere selezionato solo sui Record CANT e/o COL ma NON in Scheda parametri
                //Ma PER IL MOMENTO NON VIENE GESTITO
                if (CommonService.Nz(entity.Metodo_Arrotondamento_Can, 0) == 9)
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_NON_GESTITO_ATTUALMENTE,
                    PowerWebResources.FLD_METODO_ARROTONDAMENTO_CAN);
                //Al momento NON viene gestito il Metodo di Arrotondamento previsto in Power/Access (1=Durata,3=X Orario)
                if (CommonService.Nz(entity.Metodo_Arrotondamento_Can, 0) == 1 || CommonService.Nz(entity.Metodo_Arrotondamento_Can, 0) == 3)
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_NON_GESTITO_ATTUALMENTE,
                    PowerWebResources.FLD_METODO_ARROTONDAMENTO_CAN);
                if (CommonService.Nz(entity.Nazione_Can, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.SIGLA_NAZIONI.ToString() && x.Chiave_Tab == entity.Nazione_Can) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nazione_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_NAZIONE_CAN));
                if (CommonService.Nz(entity.Nazione_Nascita_Can, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.SIGLA_NAZIONI.ToString() && x.Chiave_Tab == entity.Nazione_Nascita_Can) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nazione_Nascita_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_NAZIONE_NASCITA_CAN));
                if (CommonService.Nz(entity.Provincia_Nascita_Can, "") != "")
                    if (Tab_Provs.SingleOrDefault(u => u.Sigla_Prov == entity.Provincia_Nascita_Can) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Provincia_Nascita_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                           PowerWebResources.FLD_PROVINCIA_NASCITA_CAN, PowerWebResources.STR_TAB_PROV));
                if (CommonService.Nz(entity.Provincia_Can, "") != "")
                    if (Tab_Provs.SingleOrDefault(u => u.Sigla_Prov == entity.Provincia_Can) == null)

                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Provincia_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                           PowerWebResources.FLD_PROVINCIA_CAN, PowerWebResources.STR_TAB_PROV));
                if (CommonService.Nz(entity.Raggruppamento1_Can, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.RAGGRUPPAMENTO_1.ToString() && x.Chiave_Tab == entity.Raggruppamento1_Can) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Raggruppamento1_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_RAGGRUPPAMENTO1_CAN));
                if (CommonService.Nz(entity.Raggruppamento2_Can, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.RAGGRUPPAMENTO_2.ToString() && x.Chiave_Tab == entity.Raggruppamento2_Can) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Raggruppamento2_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_RAGGRUPPAMENTO2_CAN));
                if (CommonService.Nz(entity.Tipo_Arrotondamento_Can, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_ARROTONDAMENTO.ToString()
                    && x.Chiave_Tab == entity.Tipo_Arrotondamento_Can.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Arrotondamento_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPO_ARROTONDAMENTO_CAN));
                if (CommonService.Nz(entity.Tipo_Calcolo_Viaggi_Can, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_CALCOLO_VIAGGI_CAN.ToString() && x.Chiave_Tab == entity.Tipo_Calcolo_Viaggi_Can) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Calcolo_Viaggi_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPO_CALCOLO_VIAGGI_CAN));
                if (CommonService.Nz(entity.Tipo_Cantiere_Can, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_CAN.ToString() && x.Chiave_Tab == entity.Tipo_Cantiere_Can) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Cantiere_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPO_CANTIERE_CAN));
                if (CommonService.Nz(entity.Tipo_Interv_Can, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_INTERVENTO.ToString() && x.Chiave_Tab == entity.Tipo_Interv_Can) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Interv_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPO_INTERV_CAN));
                if (CommonService.Nz(entity.TipoNotturno_Can, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_NOTTURNO.ToString() && x.Chiave_Tab == entity.TipoNotturno_Can.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.TipoNotturno_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPONOTTURNO_CAN));
                if (CommonService.Nz(entity.Sesso_Can, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.SESSO.ToString() && x.Chiave_Tab == entity.Sesso_Can) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Sesso_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_SESSO_CAN));
                if (CommonService.Nz(entity.Zona_Can, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.ZONE.ToString() && x.Chiave_Tab == entity.Zona_Can) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Zona_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_ZONA_CAN));

                // se è valorizzato un tipo orario per il collaboratore, questo deve essere dell'entità corrispondente a collaboratore
                if (entity.Tab_Orari_Tipo_Id.HasValue)
                {
                    var tabOrariTipo = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(tot => tot.Tab_Orari_Tipo_Id == entity.Tab_Orari_Tipo_Id);
                    if (tabOrariTipo != null)
                        if (tabOrariTipo.Tab_Orari_Tipo_Entita_Rif != "Can")
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Id), BusinessService.GetLocalizedString(PowerWebResources.ERR_ENTITA_TIPO_ORARIO_NON_CORRETTA));
                }

                //
                // verifico correttezza codice fiscale se attivato controllo in Scheda PARAM
                if (RepoManager.ParamRepo.ParametersRow.Ctrl_Codice_FiscEnum == CheckCodFiscEnum.Checked)
                {
                    if (CommonService.Nz(entity.Cod_Fisc_Can, "") != "")
                    {
                        // LO imposto tutto Maiuscolo
                        entity.Cod_Fisc_Can = entity.Cod_Fisc_Can.ToUpper();
                        String sCodiceFiscaleApp = "";
                        //se manca anche un solo dato allora non faccio controllo
                        if (CommonService.Nz(entity.Nome_Assistito_Can, "") == "" ||
                           CommonService.Nz(entity.Cognome_Assistito_Can, "") == "" ||
                           CommonService.Nz(entity.Data_Nascita_Can, null) == null ||
                           CommonService.Nz(entity.Sesso_Can, "") == "" ||
                           CommonService.Nz(entity.Luogo_Nascita_Can, "") == "" ||
                           CommonService.Nz(entity.Provincia_Nascita_Can, "") == "")
                        {
                            //controllo solo se è formalmente corretto
                            if (!CommonService.ECodiceFiscaleValido(entity.Cod_Fisc_Can.ToUpper()))
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cod_Fisc_Can),
                                  BusinessService.GetLocalizedString(PowerWebResources.ERR_CODICE_FISCALE_ERRATO));
                        }
                        else
                        {
                            //se ci sono tutti i dati necessari al calcolo del codice fiscale lo controllo ed eventualmente lo segnalo errato
                            sCodiceFiscaleApp = BusinessService.GeneraCodiceFiscale(
                                entity.Nome_Assistito_Can,
                                entity.Cognome_Assistito_Can,
                                (DateTime)entity.Data_Nascita_Can,
                                BusinessService.GetLocalizedString(entity.Sesso_Can),
                                entity.Luogo_Nascita_Can,
                                entity.Provincia_Nascita_Can);
                            //Se il Codice Fiscale Restituito inizia con ERR_ significa che contiene un MESSAGGIO DI ERRORE che va Decodificato in Lingua
                            if (sCodiceFiscaleApp != "" && sCodiceFiscaleApp.Substring(0, 4) == "ERR_")
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cod_Fisc_Can),
                                  BusinessService.GetLocalizedString(PowerWebResources.ERR_DATI_NECESSARI_MANCANTI));
                            else if (sCodiceFiscaleApp != entity.Cod_Fisc_Can)
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cod_Fisc_Can),
                                  BusinessService.GetLocalizedString(PowerWebResources.ERR_CODICE_FISCALE_ERRATO));
                        }
                    }
                }

                //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);
            }
            catch (Exception ex)
            {
                var CodErr = "Cant_Id= " + entity.Cant_Id;
                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Cant entity)
        {
            Dictionary<string, string> errorlist = new Dictionary<string, string>();

            //Verifica dei Campi che Non devono essere NULL            
            if (entity.Cli_Id == Int32.MinValue)
                errorlist.AddOrAppend(CommonService.GetPropertyName(() => entity.Cli_Id),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                    PowerWebResources.FLD_CLI_ID));
            if (string.IsNullOrEmpty(entity.Codice_Cantiere))
                errorlist.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cantiere),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                    PowerWebResources.FLD_CODICE_CANTIERE));
            if (entity.FlagGps_Can == Int32.MinValue)
                entity.FlagGps_Can = 0;
            if (entity.Tipo_Arrotondamento_Can == Int32.MinValue)
                entity.Tipo_Arrotondamento_Can = 0;
            if (entity.TipoNotturno_Can == Int32.MinValue)
                entity.TipoNotturno_Can = 0;
            //verifico SOLO x IMPORT la validità delle eventuali Date ricevute
            if (entity.Data_Registrazione_Can < new DateTime(2000, 01, 01))
                errorlist.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Registrazione_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_CAN));
            if (entity.DataOraUltimaModifica_Can < new DateTime(2000, 01, 01))
                errorlist.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_CAN));
            if (CommonService.Nz(entity.Data_Isee_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                errorlist.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Isee_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_ISEE_CAN));
            if (CommonService.Nz(entity.Data_Nascita_Can, new DateTime(1900, 1, 1)) < new DateTime(1900, 1, 1))
                errorlist.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Nascita_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_NASCITA_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Fine_1_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                errorlist.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Fine_1_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_FINE_1_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Inizio_1_Can, new DateTime(1900, 1, 1)) < new DateTime(1900, 1, 1))
                errorlist.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Inizio_1_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_INIZIO_1_CAN));
            if (CommonService.Nz(entity.DataVarGps_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                errorlist.AddOrAppend(CommonService.GetPropertyName(() => entity.DataVarGps_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAVARGPS_CAN));



            //AGGIUNGO GLI EVENTUALI VALORI PRESENTI NEI CAMPI DELLA TAB CANT CHE RICEVO e CHE NON SONO GIA' PRESENTI IN TAB_DECOD
            // PER UN EVENTUALE DISALLINEAMENTO FRA I DATI DEI RECORD DELLA TABELLA CANT e LE TAB_DECOD di ACCESS
            // LO FACCIO PER LE TABELLE:
            //                      LIVELLO_ASSISTITO_CAN
            //                      NAZIONE_CAN
            //                      TIPO_CANTIERE_CAN
            //                      TIPO_INTERVENTO_CAN
            //                      TIPO_SERVIZIO_1_CAN
            //                      TIPO_SERVIZIO_2_CAN
            //                      TIPO_SERVIZIO_3_CAN
            //                      TIPO_SERVIZIO_4_CAN
            //                      TIPO_SERVIZIO_5_CAN


            if (CommonService.Nz(entity.Livello_Assistito_Can, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.LIVELLO_CAN.ToString() && x.Chiave_Tab == entity.Livello_Assistito_Can) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.LIVELLO_CAN.ToString(),
                        Chiave_Tab = entity.Livello_Assistito_Can,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            if (CommonService.Nz(entity.Nazione_Can, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.SIGLA_NAZIONI.ToString() && x.Chiave_Tab == entity.Nazione_Can) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.SIGLA_NAZIONI.ToString(),
                        Chiave_Tab = entity.Nazione_Can,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            if (CommonService.Nz(entity.Tipo_Cantiere_Can, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.TIPO_CAN.ToString() && x.Chiave_Tab == entity.Tipo_Cantiere_Can) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.TIPO_CAN.ToString(),
                        Chiave_Tab = entity.Tipo_Cantiere_Can,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            if (CommonService.Nz(entity.Tipo_Interv_Can, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.TIPO_INTERVENTO.ToString() && x.Chiave_Tab == entity.Tipo_Interv_Can) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.TIPO_INTERVENTO.ToString(),
                        Chiave_Tab = entity.Tipo_Interv_Can,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            //verifico il rispetto dei Valori per i Campi per i quali non viene fatto questo controllo nella Check x il Video perchè lì non necessario
            if (entity.Durata_Max_Gruppo_Notte_Ril_Can.HasValue)
                if (entity.Durata_Max_Gruppo_Notte_Ril_Can.Value > new TimeSpan(23, 59, 59))
                    errorlist.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Max_Gruppo_Notte_Ril_Can),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_ERRATO,
                    PowerWebResources.FLD_DURATA_MAX_GRUPPO_NOTTE_RIL_CAN));

            if (!String.IsNullOrEmpty(entity.Cod_Fisc_Can))
                entity.Cod_Fisc_Can = entity.Cod_Fisc_Can.ToUpper();
            if (String.IsNullOrEmpty(entity.Tipologia_Can))
                entity.Tipologia_Can = "ASS";
            else
                entity.Tipologia_Can = entity.Tipologia_Can.ToUpper();

            WriteCheckLog(entity, errorlist, Log);
            return errorlist;
        }

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Cant");
            List<PowerMDBDataSet.CantRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData<PowerMDBDataSet.CantRow>(
                oDataSet.Cant.ToList(), "Cant", "Codice_Cantiere").OrderBy(acd => acd.Codice_Cantiere).ToList();
            List<Cant> toImport = new List<Cant>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>();
            string lastKey = "";
            if (accessData != null)
            {
                //accessData = accessData.OrderBy(acd => acd.Codice_Cantiere).ToList();
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                foreach (PowerMDBDataSet.CantRow oRow in accessData)
                {
                    try
                    {
                        lastKey = oRow.Codice_Cantiere;
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;

                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                           BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "CANT",
                           "5", "17", oRow.Codice_Cantiere.ToString(), countRec.ToString(), nRec.ToString()));
                        Cant oNewRecord = this.Init();
                        oNewRecord.Codice_Cantiere = oRow.Codice_Cantiere;
                        oNewRecord.Descrizione_Can = oRow.IsDescrizione_CanNull() ? (string)null : oRow.Descrizione_Can;

                        Cli oCli = Clis.SingleOrDefault(x => x.Codice_Cliente == oRow.Codice_Cliente_Can);
                        if (oCli != null)
                            oNewRecord.Cli_Id = oCli.Cli_Id;

                        oNewRecord.Data_Registrazione_Can = oRow.IsData_Registrazione_CanNull() ? new DateTime(2000, 1, 1) : oRow.Data_Registrazione_Can;
                        oNewRecord.DataOraUltimaModifica_Can = oRow.IsDataOraUtimaModificaNull() ? new DateTime(2000, 1, 1) : oRow.DataOraUtimaModifica;
                        oNewRecord.DisAbilitazione_Can = oRow.IsDisAbilitazione_CanNull() ? false : oRow.DisAbilitazione_Can;
                        oNewRecord.Data_Rapporto_Inizio_1_Can = oRow.IsData_Rapporto_Inizio_CanNull() ? (DateTime?)null : oRow.Data_Rapporto_Inizio_Can;
                        oNewRecord.Data_Rapporto_Fine_1_Can = oRow.IsData_Rapporto_Fine_CanNull() ? (DateTime?)null : oRow.Data_Rapporto_Fine_Can;
                        oNewRecord.Luogo_Can = oRow.IsLuogo_CanNull() ? (string)null : oRow.Luogo_Can;
                        oNewRecord.Indirizzo_Can = oRow.IsIndirizzo_CanNull() ? (string)null : oRow.Indirizzo_Can;
                        oNewRecord.Provincia_Can = oRow.IsProvincia_CanNull() ? (string)null : oRow.Provincia_Can;
                        oNewRecord.Cap_Can = oRow.IsCap_CanNull() ? (string)null : oRow.Cap_Can;
                        oNewRecord.Nazione_Can = oRow.IsNazione_CanNull() ? (string)null : oRow.Nazione_Can;
                        oNewRecord.Fax_1_Can = oRow.IsFax_1_CanNull() ? (string)null : oRow.Fax_1_Can;
                        oNewRecord.Fax_1_Rif_Can = oRow.IsFax_1_Rif_CanNull() ? (string)null : oNewRecord.Fax_1_Rif_Can;
                        oNewRecord.Telefono_1_Can = oRow.IsTelefono_1_CanNull() ? (string)null : oRow.Telefono_1_Can;
                        oNewRecord.Telefono_1_Rif_Can = oRow.IsTelefono_1_Rif_CanNull() ? (string)null : oRow.Telefono_1_Rif_Can;
                        oNewRecord.Telefono_2_Can = oRow.IsTelefono_2_CanNull() ? (string)null : oRow.Telefono_2_Can;
                        oNewRecord.Telefono_2_Rif_Can = oRow.IsTelefono_2_Rif_CanNull() ? (string)null : oRow.Telefono_2_Rif_Can;
                        oNewRecord.Mensa_Can = oRow.IsMensa_CanNull() ? false : oRow.Mensa_Can;
                        oNewRecord.Note_Can = oRow.IsNote_CanNull() ? (string)null : oRow.Note_Can;
                        oNewRecord.Minuti_Tolleranza_Can = (short)oRow.Minuti_Tolleranza_Can;
                        oNewRecord.Durata_Min_Ril_Can = oRow.IsDurata_Min_Ril_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Min_Ril_Can.TimeOfDay.Ticks);
                        oNewRecord.Durata_Max_Ril_Can = oRow.IsDurata_Max_Ril_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Ril_Can.TimeOfDay.Ticks);
                        oNewRecord.Durata_Max_Gruppo_Ril_Can = oRow.IsDurata_Max_Gruppo_Ril_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Gruppo_Ril_Can.TimeOfDay.Ticks);
                        oNewRecord.Durata_Max_Gruppo_Notte_Ril_Can = oRow.IsDurata_Max_Gruppo_Notte_Ril_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Gruppo_Notte_Ril_Can.TimeOfDay.Ticks);
                        oNewRecord.Flag_NON_Esportare_Can = oRow.IsFlag_NON_Esportare_CanNull() ? (byte?)null : (byte)oRow.Flag_NON_Esportare_Can;
                        oNewRecord.TipoNotturno_Can = oRow.IsTipoNotturno_CantNull() ? (int)0 : Int32.Parse(oRow.TipoNotturno_Cant);
                        oRow.TipoNotturno_Cant = oRow.IsTipoNotturno_CantNull() ? "0" : oRow.TipoNotturno_Cant;

                        if (!oRow.IsFlagCambioGiornoRil_CanNull() && oRow.FlagCambioGiornoRil_Can == true)
                        {
                            if (!oRow.IsFlagCambioGiornoRil_CanNull() && oRow.TipoNotturno_Cant == "1")
                                oNewRecord.TipoNotturno_Can = 2;
                            else
                                oNewRecord.TipoNotturno_Can = 1;
                        }
                        else
                            oNewRecord.TipoNotturno_Can = 0;
                        oNewRecord.Soglia_Arrot_Fig_Can = oRow.IsSoglia_Arrot_Fig_CanNull() ? (short?)null : (short)oRow.Soglia_Arrot_Fig_Can;
                        oNewRecord.Tipo_Cantiere_Can = oRow.IsTipo_Cantiere_CanNull() ? (string)null : oRow.Tipo_Cantiere_Can;
                        oNewRecord.Singola_Reg = oRow.IsSingola_RegNull() ? false : oRow.Singola_Reg;
                        oNewRecord.Limite_Inizio_Notte_Can = oRow.IsLimite_Inizio_Notte_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Limite_Inizio_Notte_Can.TimeOfDay.Ticks);
                        oNewRecord.Soglia_Arrot_Fig_F_Can = oRow.IsSoglia_Arrot_Fig_F_CanNull() ? (short?)null : (short)oRow.Soglia_Arrot_Fig_F_Can;
                        oNewRecord.Minuti_Tolleranza_F_Can = oRow.IsMinuti_Tolleranza_F_CanNull() ? (short?)null : (short)oRow.Minuti_Tolleranza_F_Can;
                        oNewRecord.Arrot_Durata_Can = oRow.IsArrot_Durata_CanNull() ? (short?)null : (short)oRow.Arrot_Durata_Can;
                        oNewRecord.Soglia_Durata_Can = oRow.IsSoglia_Durata_CanNull() ? (short?)null : (short)oRow.Soglia_Durata_Can;
                        oNewRecord.Tipo_Arrotondamento_Can = oRow.IsTipo_Arrotondamento_CanNull() ? 0 : oRow.Tipo_Arrotondamento_Can;
                        oNewRecord.Ore_Massime_Can = oRow.IsOre_Massime_CanNull() ? (short?)null : (short)oRow.Ore_Massime_Can;
                        oNewRecord.Data_Nascita_Can = oRow.IsData_Nascita_CanNull() ? (DateTime?)null : (DateTime)oRow.Data_Nascita_Can;
                        oNewRecord.Cod_Fisc_Can = oRow.IsCod_Fisc_CanNull() ? (string)null : oRow.Cod_Fisc_Can;
                        oNewRecord.Tipo_Interv_Can = oRow.IsTipo_Interv_CanNull() ? (string)null : oRow.Tipo_Interv_Can;
                        oNewRecord.Metodo_Arrotondamento_Can = oRow.IsMetodo_Arrotondamento_CanNull() ? 0 : oRow.Metodo_Arrotondamento_Can;
                        oNewRecord.Costo_Orario_Can = oRow.IsCosto_Orario_CanNull() ? (double?)null : (double)oRow.Costo_Orario_Can;
                        oNewRecord.Residenza_Localita_Can = oRow.IsResidenza_Localita_CanNull() ? (string)null : oRow.Residenza_Localita_Can;
                        oNewRecord.Sesso_Can = (oRow.IsSesso_CanNull() ? (string)null
                         : (oRow.Sesso_Can == "M" ? PowerWebResources.TD_SESSO_MASCHIO.ToString()
                         : (oRow.Sesso_Can == "F" ? PowerWebResources.TD_SESSO_FEMMINA.ToString()
                         : (string)null)));
                        oNewRecord.Residenza_Interno_Can = oRow.IsResidenza_Interno_CanNull() ? (string)null : oRow.Residenza_Interno_Can;
                        oNewRecord.Luogo_Nascita_Can = oRow.IsNascita_Luogo_CanNull() ? (string)null : oRow.Nascita_Luogo_Can;
                        oNewRecord.Provincia_Nascita_Can = oRow.IsNascita_Provincia_CanNull() ? (string)null : oRow.Nascita_Provincia_Can;
                        oNewRecord.Nazione_Nascita_Can = null;
                        oNewRecord.Codice_Luogo_Nascita_Can = oRow.IsCodice_Nascita_Luogo_CanNull() ? (string)null : oRow.Codice_Nascita_Luogo_Can;
                        oNewRecord.Codice_Luogo_Residenza_Can = oRow.IsCodice_Residenza_Luogo_CanNull() ? (string)null : oRow.Codice_Residenza_Luogo_Can;
                        oNewRecord.Nr_Isee_Can = oRow.IsNr_Isee_CanNull() ? (string)null : oRow.Nr_Isee_Can;
                        oNewRecord.Data_Isee_Can = oRow.IsData_Isee_CanNull() ? (DateTime?)null : (DateTime)oRow.Data_Isee_Can;
                        oNewRecord.Tipologia_Can = oRow.IsTipologia_CanNull() ? (string)null : oRow.Tipologia_Can;
                        oNewRecord.Limite_Entrata_Mattina_Cant = oRow.IsLimite_Entrata_canNull() ? (TimeSpan?)null : oRow.Limite_Entrata_can.TimeOfDay;
                        // se si tratta di un'attività non si tratta sicuramente di un passaggio
                        if (oNewRecord.Tipologia_Can == "Att")
                            oNewRecord.Singola_Reg = false;
                        else
                        {
                            // imposto la filiale solo per i record che non sono attività
                            if (!oRow.IsFilale_CanNull())
                            {

                                Fil oRecord = RepoManager.FilRepo.DbSet.SingleOrDefault(x => x.Codice_Fil == oRow.Filale_Can);
                                if (oRecord != null)
                                    oNewRecord.Fil_Id = oRecord.Fil_Id;
                            }
                        }

                        //se l'indirizzo e il cap sono nulli durante l'import imposto a null la latitudine e la longitudine nella scheda cantiere
                        if (oRow.IsCap_CanNull() && oRow.IsIndirizzo_CanNull())
                        {

                            oNewRecord.LongitudineGps_Can = 0.0d;
                            oNewRecord.LatitudineGps_Can = 0.0d;

                        }

                        oNewRecord.Gestione_Can = oRow.IsGestione_CanNull() ? (string)null : oRow.Gestione_Can;
                        oNewRecord.Cap_Nascita_Can = oRow.IsCap_Nascita_Luogo_CanNull() ? (string)null : oRow.Cap_Nascita_Luogo_Can;
                        oNewRecord.Numero_GG_Lavorativi = oRow.IsNumero_GG_LavorativiNull() ? (short?)null : (short)oRow.Numero_GG_Lavorativi;
                        oNewRecord.Codice_Voucher_Can = oRow.IsCodice_Vaucher_CanNull() ? (string)null : oRow.Codice_Vaucher_Can;
                        oNewRecord.Livello_Assistito_Can = oRow.IsLivello_Assistito_CanNull() ? (string)null : oRow.Livello_Assistito_Can;
                        oNewRecord.Tipo_Calcolo_Viaggi_Can = oRow.IsTipo_Calcolo_Viaggi_CanNull() ? (string)null : oRow.Tipo_Calcolo_Viaggi_Can;
                        oNewRecord.FlagGps_Can = (oRow.IsFlagGps_CanNull() ? (int)0
                         //USA MAPPOINT
                         : (oRow.FlagGps_Can == "1" ? 1
                         //USA GOOGLEMAP
                         : (oRow.FlagGps_Can == "2" ? 1
                         //SOVRAPPOSTE
                         : (oRow.FlagGps_Can == "4" ? 1
                         //NON TROVATE
                         : (oRow.FlagGps_Can == "5" ? 1
                         //DA ASSEGNARE
                         : (oRow.FlagGps_Can == "9" ? 1
                         : (int)0))))));

                        oNewRecord.DataVarGps_Can = oRow.IsDataVarGps_CanNull() ? (DateTime?)null : (DateTime)oRow.DataVarGps_Can;
                        oNewRecord.LatitudineGps_Can = oRow.IsLatitudineGps_CanNull() ? default(double) : (double)oRow.LatitudineGps_Can;
                        oNewRecord.LongitudineGps_Can = oRow.IsLongitudineGps_CanNull() ? default(double) : (double)oRow.LongitudineGps_Can;
                        oNewRecord.RaggioGps_Can = oRow.IsRaggioGps_CanNull() ? (short?)null : (short)oRow.RaggioGps_Can;

                        // verifico la presenza della customizzazione riguardante l'importazione del campo responsabile cantiere
                        int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.RespColDiCantInRaggruppamento1Enum);

                        // se è indicato di importare il responsabile del cantiere in raggruppamento 1 allora 
                        // lo si gestisce
                        if (customizationVersion == (int)RespColDiCantInRaggruppamento1Enum.ImportInRaggruppamento1)
                            oNewRecord.Raggruppamento1_Can = oRow.IsCodice_Resp_CanNull() ? String.Empty : oRow.Codice_Resp_Can;
                        else
                            oNewRecord.Raggruppamento1_Can = String.Empty;

                        oNewRecord.Raggruppamento2_Can = "";
                        oNewRecord.Cognome_Assistito_Can = oNewRecord.Descrizione_Can;
                        oNewRecord.Nome_Assistito_Can = "";
                        // Eseguo i Controlli Specifici della CheckForImport e poi i Controlli Standard della Check        
                        Dictionary<string, string> importDictionary = CheckForImport(oNewRecord);

                        //Solo la prima volta chiamo la Check con ResetSession=True per fargli aggironare i Dati dal DB
                        if (countRec == 1)
                            currentDictionary = Check(oNewRecord, true, true);
                        else
                            currentDictionary = Check(oNewRecord, true, false);
                        //Verifico se ci sono stati errori
                        if (currentDictionary.Keys.Count == 0 && importDictionary.Keys.Count == 0)
                            toImport.Add(oNewRecord);
                        else
                        {
                            errors.Add(new Tab_Chk_Imp
                            {
                                Nome_Tabella_Tab_Check_Imp = "Cant",
                                Chiave_Record_Tab_Check_Imp = oRow.Codice_Cantiere,
                            });
                            log.Warn("Codice Cantiere cui si rifericono gli errori precedenti: " + oRow.Codice_Cantiere + " -------------------------------------------------------------------------------");
                            errorsList.Add(importDictionary);
                            errorsList.Add(currentDictionary);
                        }
                    }
                    catch (Exception ex)
                    {
                        var RRNERR = oRow.Codice_Cantiere;
                        //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                        if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                            Log.ErrorFormat("TAB CANT - Chiave: {0}", oRow.Codice_Cantiere);
                        throw ex;
                    }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                try
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                          BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "CANT", lastKey));
                    try
                    {
                        foreach (var a in toImport)
                        {
                            RepoManager.CantRepo.Add(a, true);
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                    RepoManager.Tab_Chk_ImpRepo.Add(errors);
                    if (errors.Count == 0)
                        RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Cant",
                            Stato_Record_Tab_Check_Imp = true,
                        });
                    RepoManager.CantRepo.SaveChanges();
                    RepoManager.Tab_Chk_ImpRepo.SaveChanges();
                    RepoManager.Tab_Chk_ImpRepo.CommitWork();
                    ResetSession();
                }
                catch (Exception ex)
                {
                    RepoManager.Tab_Chk_ImpRepo.RollbackWork();
                    ResetSession();
                    throw ex;
                }
            }
            return errorsList;
        }

        private string Get_Cant_Code(string Cod_Fiscale, List<Cant> cants, List<Cant> toAddCants, int Fil_Id, int Cli_Id)
        {
            // inizializzazione del valore di ritorno del metodo
            string newCantCode = String.Empty;

            // recupero la filiale utilizzando l'id passato come parametro
            Fil currentFil = RepoManager.FilRepo.FirstOrDefault(fil => fil.Fil_Id == Fil_Id);

            // calcolo solo la parte testo del codice filiale
            var notNumberEx = new Regex("[0-9]");
            string filCode = notNumberEx.Replace(currentFil.Codice_Fil, "");

            // si procede con l'elaborazione solamente se la filiale è stata trovata
            if (currentFil != default(Fil))
            {
                List<Cant> cantResult = cants.FindAll(cant => cant.Cod_Fisc_Can == Cod_Fiscale && cant.Fil_Id == Fil_Id && cant.Cli_Id == Cli_Id && cant.Fil_Cod == filCode);
                List<Cant> canttoAddResult = toAddCants.FindAll(cant => cant.Cod_Fisc_Can == Cod_Fiscale && cant.Fil_Id == Fil_Id && cant.Cli_Id == Cli_Id && cant.Fil_Cod == filCode);

                if (cantResult.Any())
                {
                    newCantCode = cantResult.First().Codice_Cantiere;
                }
                else if (canttoAddResult.Any())
                {
                    newCantCode = canttoAddResult.First().Codice_Cantiere;
                }
                else
                {
                    var numAlpha = new Regex("(?<Alpha>[a-zA-Z\\s]*)(?<Numeric>[0-9]*)");
                    List<Cant> Cants = cants.Where(cant => cant.Fil_Id == Fil_Id && cant.Codice_Cantiere.StartsWith(filCode)).OrderBy(cant => cant.Codice_Cantiere).ToList();
                    List<Cant> CantsToAdd = toAddCants.Where(cant => cant.Fil_Id == Fil_Id && cant.Codice_Cantiere.StartsWith(filCode)).OrderBy(cant => cant.Codice_Cantiere).ToList();
                    Cant lastCant = null;
                    if (CantsToAdd.Any())
                        lastCant = CantsToAdd.Last();
                    else if (Cants.Any())
                        lastCant = Cants.Last();

                    if (lastCant != null)
                    {
                        var match = numAlpha.Match(lastCant.Codice_Cantiere.Trim());

                        var alpha = match.Groups["Alpha"].Value;
                        var num = match.Groups["Numeric"].Value;
                        int len = num.Length;

                        num = Convert.ToString(Convert.ToInt32(num) + 1);
                        num = num.PadLeft(len, '0');
                        if (alpha == string.Empty)
                            newCantCode = CommonService.CompletaASinistra(String.Format("{0}{1}", alpha, num), 20);
                        else
                            newCantCode = String.Format("{0}{1}", alpha, num);
                    }
                    else
                        newCantCode = String.Format("{0}00001", filCode);
                }
            }

            // ritorno del valore del metodo
            return newCantCode;

        }

        private Dictionary<string, Int32> get_Columns_Numbers(string Row)
        {
            Dictionary<string, Int32> dictionary = new Dictionary<string, int>();

            string[] splittedRow = Row.Split(';');
            splittedRow = splittedRow.Select(s => s.ToUpperInvariant()).ToArray();

            if (splittedRow.Contains("CODICE VOUCHER"))
            {
                dictionary.Add("CODICE VOUCHER", Array.IndexOf(splittedRow, "CODICE VOUCHER"));
            }
            else
            {

            }
            dictionary.Add("CODICE FISCALE", Array.IndexOf(splittedRow, "CODICE FISCALE"));
            dictionary.Add("COGNOME", Array.IndexOf(splittedRow, "COGNOME"));
            dictionary.Add("NOME", Array.IndexOf(splittedRow, "NOME"));
            dictionary.Add("CAP", Array.IndexOf(splittedRow, "CAP"));
            dictionary.Add("COMUNE", Array.IndexOf(splittedRow, "COMUNE"));
            dictionary.Add("INDIRIZZO", Array.IndexOf(splittedRow, "INDIRIZZO"));
            dictionary.Add("DISTRETTO", Array.IndexOf(splittedRow, "DISTRETTO"));
            dictionary.Add("PROFILO INIZIALE", Array.IndexOf(splittedRow, "PROFILO INIZIALE"));
            dictionary.Add("MENSILITA'", Array.IndexOf(splittedRow, "MENSILITA'"));
            dictionary.Add("INI", Array.IndexOf(splittedRow, "INI"));
            dictionary.Add("EXTRA REGIONE", Array.IndexOf(splittedRow, "EXTRA REGIONE"));

            if (dictionary.ContainsValue(-1))
            {
                var errorSb = new StringBuilder();
                foreach (KeyValuePair<string, Int32> kvp in dictionary.Where(kvp => kvp.Value == -1))
                    errorSb.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_TESTATA_X_COLONNA, kvp.Key.ToString()));


                if (errorSb.ToString() != String.Empty)
                    throw new InvalidOperationException(errorSb.ToString());

                dictionary = null;

            }
            return dictionary;
        }

        /// <summary>
        /// Carica i cantieri da un file CSV
        /// </summary>
        /// <param name="inputCants">The input cants.</param>
        /// <param name="Fil_Id">The fil_ identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">
        /// </exception>
        public Dictionary<string, string> ImportFromCSV(string[] inputCants, int Fil_Id)
        {
            Fil fil = RepoManager.FilRepo.SingleOrDefault(f => f.Fil_Id == Fil_Id);

            //Se l'ultimo elemento è una riga vuota, lo rimuove
            if (inputCants.Last() == "\n" || inputCants.Last() == "\r\n")
            {
                inputCants = inputCants.Take(inputCants.Count() - 1).ToArray();
            }

            string sTipoAss = string.Empty;
            //La prima riga deve contenere il nome delle colonne
            //Dictionary<string, string> dictionary = new Dictionary<string, string>();
            Dictionary<string, string> errors = new Dictionary<string, string>();
            Dictionary<string, string> getColumnsErrors = new Dictionary<string, string>();

            CantImportTypeEnum customizationVersion = (CantImportTypeEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CantImportTypeEnum);



            //Gestisco eventuali Import Standard e / o Custom in base al Campo ModuleType della tab Param
            if (customizationVersion == CantImportTypeEnum.Mosaico)
            {
                //var importModule = CantImportFactory.CreateInstance(customizationVersion, inputCants, Fil_Id);

                //return (Dictionary<string,string>)importModule.Import();

                #region Import delle anagrafiche cantiere per Mosaico

                //trattamento del caso di Import CUSTOM x MOSAICO
                List<Cant> cants = RepoManager.CantRepo.GetAll(true).ToList();
                List<Cant> toAddCants = new List<Cant>();
                //inizializzo la lista dei cant var
                List<Cant_Var> toAddCant_Vars = new List<Cant_Var>();
                List<Cant_Var> toDeleteCant_Vars = new List<Cant_Var>();

                Dictionary<string, Int32> columnsNumber = null;

                // inizializzazione dei valori utilizzati per la visualizzazione dei messaggi a video (barra di avanzamento)
                double nRec = inputCants.Count();
                double countRec = 0;
                double percRec = 0;
                int rownumber = 1;
                var errorFileds = new StringBuilder();

                //Loop di trattamento dei Record presenti nel file di input
                foreach (String stringVar in inputCants)
                {
                    // aggiornamento dei dati di percentuale e numero record elaborati
                    countRec = countRec + 1;
                    percRec = (countRec / nRec) * 100;

                    // inserimento del messaggio di stato dell'elaborazione
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORT_RECORD_X_DI_Y.ToString(), countRec.ToString(), nRec.ToString()));

                    //divido la stringa in ogni valore
                    String[] splittedLine = stringVar.Split(new Char[] { ';' }, StringSplitOptions.None);

                    //determino in base al numero di campi come è rappresentata una riga vuota
                    var columns = splittedLine.Count();
                    var emptyRow = new StringBuilder();
                    for (var i = 0; i < columns - 1; i++)
                        emptyRow.Append(";");

                    //Segnala errore se manca un'intera riga
                    if (stringVar == emptyRow.ToString() || splittedLine.Length <= 1)
                    {
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_RIGA_X_VUOTA, rownumber.ToString())); //String.Format("Non è presente nessun Valore nella riga", rownumber));
                        throw new System.InvalidOperationException(errorFileds.ToString());
                    }

                    //controllo uniformità della testata
                    if (columnsNumber == null)
                        columnsNumber = get_Columns_Numbers(stringVar);
                    else
                    {

                        #region CONTROLLO UNIFORMITA' CAMPI

                        //passo a controllare tutti i campi della riga
                        string codVouch = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                        string codFisc = splittedLine[columnsNumber["CODICE FISCALE"]].Trim().ToUpper();
                        string cognome = splittedLine[columnsNumber["COGNOME"]].Trim();
                        string nome = splittedLine[columnsNumber["NOME"]].Trim();
                        string indirizzo = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                        string cap = splittedLine[columnsNumber["CAP"]].Trim();
                        string comune = splittedLine[columnsNumber["COMUNE"]].Trim();
                        string distretto = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                        string profiloIniz = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                        string ini = splittedLine[columnsNumber["INI"]].Trim();
                        DateTime dateIni = new DateTime();


                        if (codVouch == string.Empty)
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_VOUCHER_X_VUOTA, rownumber.ToString()));
                        //nel caso in cui il codice fiscale sia vuoto
                        if (codFisc == string.Empty)
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CODICE_FISCALE_X_VUOTA, rownumber.ToString()));
                        //nel caso in cui il codice fiscale sia lungo meno di 16
                        else if (!CommonService.ECodiceFiscaleValido(codFisc))
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CODICE_FISCALE_X_ERRATO, rownumber.ToString()));

                        if (cognome == string.Empty)
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_COGNOME_X_VUOTA, rownumber.ToString()));

                        if (nome == string.Empty)
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_NOME_X_VUOTA, rownumber.ToString()));

                        if (indirizzo == string.Empty)
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_INDIRIZZO_X_VUOTA, rownumber.ToString()));

                        if (cap == string.Empty)
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAP_X_VUOTA, rownumber.ToString()));

                        if (comune == string.Empty)
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_COMUNE_X_VUOTA, rownumber.ToString()));

                        if (distretto == string.Empty)
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_DISTRETTO_X_VUOTA, rownumber.ToString()));

                        if (profiloIniz == string.Empty)
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PROFILO_X_VUOTA, rownumber.ToString()));

                        if (ini == string.Empty)
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_DATA_INIZIALE_X_VUOTA, rownumber.ToString()));
                        else
                        {
                            //controllo uniformità data
                            if (!DateTime.TryParse(splittedLine[columnsNumber["INI"]].Trim(), out dateIni))
                                errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_DATA_INIZIALE_X_FORMATO_CORRETTO, rownumber.ToString()));

                            #endregion

                            if (errorFileds.ToString() == String.Empty)
                            {
                                if (cants.FirstOrDefault(cant => cant.Cod_Fisc_Can == codFisc && cant.Fil_Id == Fil_Id) != null || toAddCants.FirstOrDefault(cant => cant.Cod_Fisc_Can == codFisc && cant.Fil_Id == Fil_Id) != null)
                                {
                                    //caso in cui il cantiere esiste già, devo quindi verificare se è più recente ed interagire con Cant_Var
                                    Cant currentCant;
                                    if (cants.FirstOrDefault(cant => cant.Cod_Fisc_Can == codFisc && cant.Fil_Id == Fil_Id) != null)
                                        currentCant = cants.First(cant => cant.Cod_Fisc_Can == codFisc && cant.Fil_Id == Fil_Id);
                                    else
                                        currentCant = toAddCants.First(cant => cant.Cod_Fisc_Can == codFisc && cant.Fil_Id == Fil_Id);

                                    //caso in cui la data di ultima modifica sia uguale
                                    //aggiorno il record nella tabella Cant
                                    if (currentCant.DataOraUltimaModifica_Can.ToShortDateString() == dateIni.ToShortDateString())
                                    {
                                        // viene salvato il cant_var all'interno dell'elenco dei cant var
                                        Cant_Var newCant_Var = RepoManager.Cant_VarRepo.Init();

                                        CommonService.DuplicateEntity(currentCant, newCant_Var);
                                        toAddCant_Vars.Add(newCant_Var);

                                        int newCliId = GetCodCli(columnsNumber, splittedLine, fil);

                                        if (newCliId != 0)
                                            currentCant.Cli_Id = newCliId;

                                        currentCant.Fil_Id = Fil_Id;
                                        currentCant.Cod_Fisc_Can = splittedLine[columnsNumber["CODICE FISCALE"]].Trim();
                                        currentCant.Descrizione_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                        currentCant.Cognome_Assistito_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                        currentCant.Cap_Can = splittedLine[columnsNumber["CAP"]].Trim();
                                        currentCant.Luogo_Can = splittedLine[columnsNumber["COMUNE"]].Trim().ToUpper();
                                        currentCant.Indirizzo_Can = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                                        currentCant.Livello_Assistito_Can = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                                        currentCant.DataOraUltimaModifica_Can = Convert.ToDateTime(splittedLine[columnsNumber["INI"]].Trim());
                                        currentCant.Codice_Voucher_Can = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                                        currentCant.Raggruppamento1_Can = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                                    }
                                    else if (currentCant.DataOraUltimaModifica_Can > dateIni)
                                    {
                                        //caso in cui il record presente in cant è più aggiornato
                                        //vado ad aggiungere o modificare i record in Cant_Var
                                        var cantsvar = RepoManager.Cant_VarRepo.Find(cv => cv.Cod_Fisc_Can == codFisc && cv.Fil_Id == Fil_Id && cv.DataOraUltimaModifica_Can.Year == dateIni.Year && cv.DataOraUltimaModifica_Can.Month == dateIni.Month && cv.DataOraUltimaModifica_Can.Day == dateIni.Day).ToList();

                                        if (cantsvar.Any())
                                        {
                                            Cant_Var vars = cantsvar.Last();
                                            int newCliId = GetCodCli(columnsNumber, splittedLine, fil);
                                            vars.Codice_Cantiere = currentCant.Codice_Cantiere;
                                            if (newCliId != 0)
                                                vars.Cli_Id = newCliId;
                                            vars.Fil_Id = Fil_Id;
                                            vars.Cod_Fisc_Can = splittedLine[columnsNumber["CODICE FISCALE"]].Trim();
                                            vars.Descrizione_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                            vars.Cognome_Assistito_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                            vars.Cap_Can = splittedLine[columnsNumber["CAP"]].Trim();
                                            vars.Luogo_Can = splittedLine[columnsNumber["COMUNE"]].Trim().ToUpper();
                                            vars.Indirizzo_Can = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                                            vars.Livello_Assistito_Can = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                                            vars.DataOraUltimaModifica_Can = Convert.ToDateTime(splittedLine[columnsNumber["INI"]].Trim());
                                            vars.Codice_Voucher_Can = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                                            vars.Raggruppamento1_Can = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                                        }
                                        else
                                        {
                                            //vado ad creare un nuovo record in Cant_Var
                                            Cant_Var newCantVar = RepoManager.Cant_VarRepo.Init();
                                            int newCliId = GetCodCli(columnsNumber, splittedLine, fil);
                                            newCantVar.Codice_Cantiere = Get_Cant_Code(splittedLine[columnsNumber["CODICE FISCALE"]], cants, toAddCants, Fil_Id, newCliId);
                                            if (newCliId != 0)
                                                newCantVar.Cli_Id = newCliId;
                                            newCantVar.Fil_Id = Fil_Id;
                                            newCantVar.Cant_Id = currentCant.Cant_Id;
                                            newCantVar.Codice_Cantiere = currentCant.Codice_Cantiere;
                                            newCantVar.Cod_Fisc_Can = splittedLine[columnsNumber["CODICE FISCALE"]].Trim();
                                            newCantVar.Descrizione_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                            newCantVar.Cognome_Assistito_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                            newCantVar.Cap_Can = splittedLine[columnsNumber["CAP"]].Trim();
                                            newCantVar.Luogo_Can = splittedLine[columnsNumber["COMUNE"]].Trim().ToUpper();
                                            newCantVar.Indirizzo_Can = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                                            newCantVar.Livello_Assistito_Can = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                                            newCantVar.DataOraUltimaModifica_Can = Convert.ToDateTime(splittedLine[columnsNumber["INI"]].Trim());
                                            newCantVar.Codice_Voucher_Can = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                                            newCantVar.Raggruppamento1_Can = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                                            newCantVar.DisAbilitazione_Can = false;
                                            newCantVar.FlagGps_Can = 0;
                                            newCantVar.Tipologia_Can = "ASS";

                                            toAddCant_Vars.Add(newCantVar);
                                        }
                                    }
                                    else if (currentCant.DataOraUltimaModifica_Can < dateIni)
                                    {
                                        //caso in cui il record è più recente di quello in Cant
                                        //Copio il record Cant in Cant_Var e aggiorno quello in Cant
                                        Cant_Var newCant_Var = RepoManager.Cant_VarRepo.Init();

                                        CommonService.DuplicateEntity(currentCant, newCant_Var);
                                        toAddCant_Vars.Add(newCant_Var);
                                        int newCliId = GetCodCli(columnsNumber, splittedLine, fil);
                                        if (newCliId != 0)
                                            currentCant.Cli_Id = newCliId;
                                        currentCant.Fil_Id = Fil_Id;
                                        currentCant.Cod_Fisc_Can = splittedLine[columnsNumber["CODICE FISCALE"]].Trim();
                                        currentCant.Descrizione_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                        currentCant.Cognome_Assistito_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                        currentCant.Cap_Can = splittedLine[columnsNumber["CAP"]].Trim();
                                        currentCant.Luogo_Can = splittedLine[columnsNumber["COMUNE"]].Trim().ToUpper();
                                        currentCant.Indirizzo_Can = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                                        currentCant.Livello_Assistito_Can = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                                        currentCant.DataOraUltimaModifica_Can = Convert.ToDateTime(splittedLine[columnsNumber["INI"]].Trim());
                                        currentCant.Codice_Voucher_Can = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                                        currentCant.Raggruppamento1_Can = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                                    }
                                }
                                else
                                {
                                    //il cantiere non esiste, quindi posso caricare i dati nella tabella Cant
                                    Cant newCant = RepoManager.CantRepo.Init();
                                    int newCliId = GetCodCli(columnsNumber, splittedLine, fil);
                                    newCant.Fil_Id = Fil_Id;
                                    newCant.Codice_Cantiere = Get_Cant_Code(splittedLine[columnsNumber["CODICE FISCALE"]], cants, toAddCants, Fil_Id, newCliId);
                                    if (newCliId != 0)
                                        newCant.Cli_Id = newCliId;
                                    newCant.Cod_Fisc_Can = splittedLine[columnsNumber["CODICE FISCALE"]].Trim();
                                    newCant.Descrizione_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                    newCant.Cognome_Assistito_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                    newCant.Cap_Can = splittedLine[columnsNumber["CAP"]].Trim();
                                    newCant.Luogo_Can = splittedLine[columnsNumber["COMUNE"]].Trim().ToUpper();
                                    newCant.Indirizzo_Can = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                                    newCant.Livello_Assistito_Can = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                                    newCant.DataOraUltimaModifica_Can = Convert.ToDateTime(splittedLine[columnsNumber["INI"]].Trim());
                                    newCant.Codice_Voucher_Can = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                                    newCant.Raggruppamento1_Can = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                                    newCant.DisAbilitazione_Can = false;

                                    // viene abilitato di default il flag gps
                                    newCant.FlagGps_Can = 1;
                                    newCant.Tipologia_Can = "ASS";

                                    // sono automaticamente calcolate le coordinate gps
                                    UpdateGeoLocation(newCant);

                                    // aggiungo il cantiere appena inserito nell'elenco dei cantieri letti
                                    // affinché la numerazione progressiva per filiale funzioni anche all'interno dello stesso file
                                    cants.Add(newCant);

                                    toAddCants.Add(newCant);
                                }
                            }
                        }
                        //incremento il numero di riga
                        rownumber++;
                    }
                }

                //se ho errori all'interno del file csv 
                if (errorFileds.ToString() != String.Empty)
                    throw new InvalidOperationException(errorFileds.ToString());
                try
                {

                    //Update dei cantieri modificati
                    Context.BulkUpdate(cants.Where(cant => cant.Cant_Id != 0));

                    //Inserimento dei cantieri nuovi
                    Context.BulkInsert(toAddCants);

                    foreach (Cant_Var cv in toAddCant_Vars)
                    {
                        cv.Cant_Id = FirstOrDefault(cant => cant.Codice_Cantiere == cv.Codice_Cantiere).Cant_Id;
                    }

                    RepoManager.Cant_VarRepo.Add(toAddCant_Vars);
                    RepoManager.Cant_VarRepo.SaveChanges();

                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                }


                #endregion
            }
            else if (customizationVersion == CantImportTypeEnum.Dugoni)
            {
                #region Import delle anagrafiche cantiere per Dugoni

                // inizializzazione del dizionario che permette di recuperare il posizionamento delle colonne
                Dictionary<string, Int32> columnsNumber = null;

                // inizializzazione della lista utilizzata per l'aggiunta dei cantieri
                var cantsToAdd = new List<Cant>();

                // inizializzazione dei valori utilizzati per la visualizzazione dei messaggi a video (barra di avanzamento)
                double nRec = inputCants.Count();
                double countRec = 0;
                double percRec = 0;

                // per ogni riga del csv passato come parametro
                foreach (String stringVar in inputCants)
                {
                    if (stringVar.Trim() != String.Empty)
                    {
                        // aggiornamento dei dati di percentuale e numero record elaborati
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;

                        // inserimento del messaggio di stato dell'elaborazione
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                           BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORT_RECORD_X_DI_Y.ToString(), countRec.ToString(), nRec.ToString()));

                        String[] splittedLine = stringVar.Split(new Char[] { ';' }, StringSplitOptions.None);

                        // se sto processando la prima linea allora mi salvo il posizionamento delle colonne,
                        // altrimenti procedo all'inserimento del record indicato
                        if (columnsNumber == null)
                        {
                            string[] splittedRow = stringVar.Split(';');
                            columnsNumber = new Dictionary<string, int>();
                            splittedRow = splittedRow.Select(s => s.ToUpperInvariant()).ToArray();
                            columnsNumber.Add("ID TAG", Array.IndexOf(splittedRow, "ID TAG"));
                            columnsNumber.Add("DESCRIZIONE", Array.IndexOf(splittedRow, "DESCRIZIONE"));
                            columnsNumber.Add("COD FILIALE", Array.IndexOf(splittedRow, "COD FILIALE"));
                            columnsNumber.Add("FILIALE", Array.IndexOf(splittedRow, "FILIALE"));
                            columnsNumber.Add("COD RAGGRUPPAMENTO 1", Array.IndexOf(splittedRow, "COD RAGGRUPPAMENTO 1"));
                            columnsNumber.Add("RAGGUPPAMENTO 1", Array.IndexOf(splittedRow, "RAGGUPPAMENTO 1"));
                            columnsNumber.Add("COD RAGGRUPPAMENTO 2", Array.IndexOf(splittedRow, "COD RAGGRUPPAMENTO 2"));
                            columnsNumber.Add("RAGGUPPAMENTO 2", Array.IndexOf(splittedRow, "RAGGUPPAMENTO 2"));
                            columnsNumber.Add("P/NP", Array.IndexOf(splittedRow, "P/NP"));

                            if (columnsNumber.ContainsValue(-1))
                                columnsNumber = null;

                            if (columnsNumber == null)
                            {
                                errors.Add("Tracciato non confome", "Tracciato record del file di import non conforme");
                                return errors;
                            }
                        }
                        else
                        {
                            // inizializzo il nuovo cantiere e lo compilo con i dati del record
                            Cant newCant = RepoManager.CantRepo.Init();

                            newCant.Codice_Cantiere = splittedLine[columnsNumber["ID TAG"]].Trim();
                            newCant.Descrizione_Can = splittedLine[columnsNumber["DESCRIZIONE"]].Trim();
                            newCant.Tipologia_Can = "CAN";
                            string searchCodFil = splittedLine[columnsNumber["COD FILIALE"]];
                            Fil currentFil = RepoManager.FilRepo.FirstOrDefault(f => f.Codice_Fil == searchCodFil);
                            if (currentFil != default(Fil))
                                newCant.Fil_Id = currentFil.Fil_Id;
                            newCant.Raggruppamento1_Can = splittedLine[columnsNumber["COD RAGGRUPPAMENTO 1"]];
                            newCant.Raggruppamento2_Can = splittedLine[columnsNumber["COD RAGGRUPPAMENTO 2"]];
                            newCant.Tipo_Cantiere_Can = splittedLine[columnsNumber["P/NP"]];

                            // una volta compilato il cantiere lo aggiungo alla lista da processare successivamente
                            cantsToAdd.Add(newCant);
                        }
                    }
                }

                RepoManager.CantRepo.Add(cantsToAdd);
                RepoManager.CantRepo.SaveChanges();

                #endregion
            }
            else if (customizationVersion == CantImportTypeEnum.GeneraleCantiere)
            {
                #region Import delle anagrafiche cantiere GENERALE

                // inizializzazione del dizionario che permette di recuperare il posizionamento delle colonne
                Dictionary<string, Int32> columnsNumber = null;

                // inizializzazione della lista utilizzata per l'aggiunta dei cantieri

                var errorFileds = new StringBuilder();

                List<Cant> cantToInsert = new List<Cant>();    //Lista contente i records che hanno passato la verifica


                // per ogni riga del csv passato come parametro
                foreach (String stringVar in inputCants)
                {

                    String[] splittedLine = stringVar.Split(new Char[] { ';' }, StringSplitOptions.None);
                    var columns = splittedLine.Count();
                    var emptyRow = new StringBuilder();
                    for (var i = 0; i < columns - 1; i++)
                        emptyRow.Append(";");

                    //Segnala errore se manca un'intera riga
                    if (stringVar == emptyRow.ToString() || splittedLine.Length <= 1)
                    {
                        //errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_RIGA_X_VUOTA, rownumber.ToString()));
                        //throw new InvalidOperationException(errorFileds.ToString());
                        continue;
                    }

                    if (errorFileds.ToString() == String.Empty)
                    {

                        // se sto processando la prima linea allora mi salvo il posizionamento delle colonne,
                        // altrimenti procedo all'inserimento del record indicato

                        if (columnsNumber == null)
                        {
                            #region SALVATAGGIO INTESTAZIONE

                            string[] splittedRow = stringVar.Split(';');
                            columnsNumber = new Dictionary<string, int>();
                            splittedRow = splittedRow.Select(s => s.ToUpperInvariant()).ToArray();
                            columnsNumber.Add("CODICE", Array.IndexOf(splittedRow, "CODICE"));
                            columnsNumber.Add("DESCRIZIONE", Array.IndexOf(splittedRow, "DESCRIZIONE"));
                            columnsNumber.Add("CODICE CLIENTE", Array.IndexOf(splittedRow, "CODICE CLIENTE"));
                            columnsNumber.Add("DESCRIZIONE CLIENTE", Array.IndexOf(splittedRow, "DESCRIZIONE CLIENTE"));
                            columnsNumber.Add("MATRICOLA FRU", Array.IndexOf(splittedRow, "MATRICOLA FRU"));
                            columnsNumber.Add("DATA ASSOCIAZIONE", Array.IndexOf(splittedRow, "DATA ASSOCIAZIONE"));
                            columnsNumber.Add("FILIALE", Array.IndexOf(splittedRow, "FILIALE"));
                            columnsNumber.Add("COMUNE", Array.IndexOf(splittedRow, "COMUNE"));
                            columnsNumber.Add("VIA", Array.IndexOf(splittedRow, "VIA"));
                            columnsNumber.Add("LATITUDINE", Array.IndexOf(splittedRow, "LATITUDINE"));
                            columnsNumber.Add("LONGITUDINE", Array.IndexOf(splittedRow, "LONGITUDINE"));
                            columnsNumber.Add("NOTE", Array.IndexOf(splittedRow, "NOTE"));
                            columnsNumber.Add("PROVINCIA", Array.IndexOf(splittedRow, "PROVINCIA"));
                            columnsNumber.Add("CAP", Array.IndexOf(splittedRow, "CAP"));
                            columnsNumber.Add("RAGGIO", Array.IndexOf(splittedRow, "RAGGIO"));


                            if (columnsNumber.ContainsValue(-1))
                            {

                                var errorSb = new StringBuilder();
                                foreach (KeyValuePair<string, Int32> kvp in columnsNumber.Where(kvp => kvp.Value == -1))
                                {
                                    if (kvp.Key.Equals("CODICE") || kvp.Key.Equals("DESCRIZIONE") || kvp.Key.Equals("MATRICOLA FRU") || kvp.Key.Equals("DATA ASSOCIAZIONE"))
                                    {
                                        errorSb.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_TESTATA_X_COLONNA, kvp.Key.ToString()));
                                    }
                                }

                                if (errorSb.ToString() != String.Empty)
                                    throw new InvalidOperationException(errorSb.ToString());

                                columnsNumber = null;
                            }


                            #endregion
                        }
                        else
                        {

                            #region ESTRAZIONE DATI

                            string codice_Can = splittedLine[columnsNumber["CODICE"]].Trim();
                            string descrizione_Can = splittedLine[columnsNumber["DESCRIZIONE"]].Trim();
                            string cod_Cliente = splittedLine[columnsNumber["CODICE CLIENTE"]].Trim();
                            string descrizione_Cli = splittedLine[columnsNumber["DESCRIZIONE CLIENTE"]].Trim();
                            string fru_matr = splittedLine[columnsNumber["MATRICOLA FRU"]];
                            string filiale = splittedLine[columnsNumber["FILIALE"]];
                            DateTime? data_associazione = (splittedLine[columnsNumber["DATA ASSOCIAZIONE"]] != "") ? Convert.ToDateTime(splittedLine[columnsNumber["DATA ASSOCIAZIONE"]].Trim()) as DateTime? : null;
                            string comune = splittedLine[columnsNumber["COMUNE"]].Trim();
                            string via = splittedLine[columnsNumber["VIA"]].Trim();
                            string latit = splittedLine[columnsNumber["LATITUDINE"]].Trim();
                            string longi = splittedLine[columnsNumber["LONGITUDINE"]].Trim();
                            string note = splittedLine[columnsNumber["NOTE"]].Trim();
                            string provincia = splittedLine[columnsNumber["PROVINCIA"]].Trim();
                            string cap = splittedLine[columnsNumber["CAP"]].Trim();
                            string raggio = splittedLine[columnsNumber["RAGGIO"]].Trim();

                            #endregion

                            #region CONTROLLO CAMPI

                            var exCant = RepoManager.CantRepo.FirstOrDefault(c => c.Codice_Cantiere.Trim() == codice_Can.Trim());

                            Cant cantiere = null;

                            if (codice_Can != "" && descrizione_Can != "" && exCant == null)     //Obbligatorio cod,desc e che non esista cantiere con lo stesso codice
                            {
                                cantiere = RepoManager.CantRepo.Init();
                                cantiere.Codice_Cantiere = CommonService.AggiungiSpaziASinistraSeStringaNumerica(codice_Can, 20);
                                cantiere.Descrizione_Can = descrizione_Can;

                                if (filiale != "")
                                {
                                    var existFil = RepoManager.FilRepo.FirstOrDefault(f => f.Descrizione_Fil == filiale);

                                    cantiere.Fil = existFil ?? null;

                                }

                                if (comune != "")
                                {
                                    var exist = RepoManager.Tab_ComuniRepo.FirstOrDefault(x => x.Luogo_Tab_Comuni == comune);
                                    cantiere.Luogo_Can = (exist != null) ? splittedLine[columnsNumber["COMUNE"]].Trim() : "";
                                    cantiere.Cap_Can = (exist != null) ? exist.Cap_Tab_Comuni : "";
                                    cantiere.Provincia_Can = (exist != null) ? exist.Codice_Prov_Tab_Comuni : "";
                                }

                                via = splittedLine[columnsNumber["VIA"]].Trim();
                                cantiere.Tipologia_Can = "CAN";
                                cantiere.Indirizzo_Can = (via.Length < 50) ? via : via.Substring(0, 49);
                                cantiere.LatitudineGps_Can = (latit != "") ? Double.Parse(latit) : 0;
                                cantiere.LongitudineGps_Can = (longi != "") ? Double.Parse(longi) : 0;
                                cantiere.Note_Can = note;



                                if (cod_Cliente != "" && descrizione_Cli != "")
                                {
                                    try
                                    {
                                        var exCli = RepoManager.CliRepo.FirstOrDefault(c => c.Codice_Cliente.Trim() == cod_Cliente.Trim());
                                        if (exCli == null)
                                        {
                                            Cli cliente = RepoManager.CliRepo.Init();
                                            cliente.Codice_Cliente = CommonService.AggiungiSpaziASinistraSeStringaNumerica(cod_Cliente, 10);
                                            cliente.Cognome_Cli = descrizione_Cli;
                                            cantiere.Cli = cliente;
                                        }
                                        else
                                        {
                                            cantiere.Cli = exCli;
                                        }
                                    }
                                    catch (Exception) { }
                                }

                                if (fru_matr != "" && (fru_matr.Length == 10 || fru_matr.Length == 5) && data_associazione != null)
                                {
                                    Fru currentFru = RepoManager.FruRepo.FirstOrDefault(f => f.Codice_Fru.Trim() == fru_matr);
                                    Fru_Cant fru_cant = RepoManager.Fru_CantRepo.Init();

                                    if (currentFru != default(Fru))
                                    {
                                        //se la fru è valorizzata viene estratto l'Id
                                        fru_cant.Fru = currentFru;
                                        fru_cant.Abilitazione_Data_Inizio_Fru_Can = (DateTime)data_associazione;
                                    }
                                    else
                                    {
                                        Fru newFru = RepoManager.FruRepo.Init();

                                        newFru.DataOraUltimaModifica_Fru = DateTime.Now;
                                        newFru.N_Serie_Fru = fru_matr;
                                        newFru.Codice_Fru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(fru_matr, 5);
                                        fru_cant.Abilitazione_Data_Inizio_Fru_Can = (DateTime)data_associazione;

                                        fru_cant.Fru = newFru;
                                    }

                                    cantiere.Fru_Cant.Add(fru_cant);
                                }
                                if (comune != "" && via != "" && provincia != "" && cap.Length == 5) {
                                    string indirizzo = String.Format("{0} {1} {2} {3}", via, cap, comune, provincia);
                                    Location geocode = BusinessService.GetGeocode(indirizzo);
                                    if (geocode != null)
                                    {
                                        cantiere.LatitudineGps_Can = geocode.Latitudine;
                                        cantiere.LongitudineGps_Can = geocode.Longitudine;
                                    }
                                }

                                if (raggio != "") {
                                    if (raggio.Contains(",")) {
                                        var tmp = raggio.Split(',');
                                        raggio = tmp[0];
                                    }
                                    short s;
                                    if (!short.TryParse(raggio, out s))
                                    {
                                        s = 0;
                                    }
                                    cantiere.RaggioGps_Can = s;
                                }

                                #endregion

                            }

                            if (cantiere != null)
                                cantToInsert.Add(cantiere);

                        }
                    }
                }

                try
                {
                    DbSet.AddRange(cantToInsert);
                    BulkSaveChanges(bulk => bulk.BatchSize = 100);
                }
                catch (Exception ex)
                {
                    _log.ErrorFormat("Errore durante l'inserimento dei cantieri da CSV a causa dell'exception {0}", ex.Message);
                    errors.Add("Errore", "Errore durante l'inserimento dei cantieri! Contattare l'assistenza!");
                }

                #endregion
            }
            else if (customizationVersion == CantImportTypeEnum.GeneraleAssistito)
            {
                #region Import delle anagrafiche assistito GENERALE

                // inizializzazione del dizionario che permette di recuperare il posizionamento delle colonne
                Dictionary<string, Int32> columnsNumber = null;

                // inizializzazione della lista utilizzata per l'aggiunta dei cantieri
                var cantsToAdd = new List<Cant>();
                var tempCantsToAdd = new List<Cant>();
                var fruCantToAdd = new List<Fru_Cant>();
                var errorFileds = new StringBuilder();

                bool cantAlreadyAdded = false;

                // inizializzazione dei valori utilizzati per la visualizzazione dei messaggi a video (barra di avanzamento)
                double nRec = inputCants.Count();
                double countRec = 0;
                double percRec = 0;
                int rownumber = 1;

                // per ogni riga del csv passato come parametro
                foreach (String stringVar in inputCants)
                {
                    if (stringVar.Trim() != String.Empty)
                    {
                        // aggiornamento dei dati di percentuale e numero record elaborati
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;

                        // inserimento del messaggio di stato dell'elaborazione
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                           BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORT_RECORD_X_DI_Y.ToString(), countRec.ToString(), nRec.ToString()));

                        String[] splittedLine = stringVar.Split(new Char[] { ';' }, StringSplitOptions.None); var columns = splittedLine.Count();
                        var emptyRow = new StringBuilder();
                        for (var i = 0; i < columns - 1; i++)
                            emptyRow.Append(";");

                        //Segnala errore se manca un'intera riga
                        if (stringVar == emptyRow.ToString() || splittedLine.Length <= 1)
                        {
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_RIGA_X_VUOTA, rownumber.ToString()));
                            throw new System.InvalidOperationException(errorFileds.ToString());
                        }

                        if (errorFileds.ToString() == String.Empty)
                        {
                            // se sto processando la prima linea allora mi salvo il posizionamento delle colonne,
                            // altrimenti procedo all'inserimento del record indicato
                            if (columnsNumber == null)
                            {
                                string[] splittedRow = stringVar.Split(';');
                                columnsNumber = new Dictionary<string, int>();
                                splittedRow = splittedRow.Select(s => s.ToUpperInvariant()).ToArray();
                                columnsNumber.Add("CODICE", Array.IndexOf(splittedRow, "CODICE"));
                                columnsNumber.Add("COGNOME", Array.IndexOf(splittedRow, "COGNOME"));
                                columnsNumber.Add("NOME", Array.IndexOf(splittedRow, "NOME"));
                                columnsNumber.Add("MATRICOLA FRU", Array.IndexOf(splittedRow, "MATRICOLA FRU"));
                                columnsNumber.Add("DATA ASSOCIAZIONE", Array.IndexOf(splittedRow, "DATA ASSOCIAZIONE"));

                                if (columnsNumber.ContainsValue(-1))
                                {
                                    var errorSb = new StringBuilder();
                                    foreach (KeyValuePair<string, Int32> kvp in columnsNumber.Where(kvp => kvp.Value == -1))
                                        errorSb.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_TESTATA_X_COLONNA, kvp.Key.ToString()));

                                    if (errorSb.ToString() != String.Empty)
                                        throw new InvalidOperationException(errorSb.ToString());

                                    columnsNumber = null;
                                }
                            }
                            else
                            {
                                // inizializzo il nuovo cantiere e lo compilo con i dati del record
                                Cant newCant = RepoManager.CantRepo.Init();
                                Fru_Cant newFruCant = RepoManager.Fru_CantRepo.Init();

                                String codice_Ass = splittedLine[columnsNumber["CODICE"]].Trim();
                                String cognome_Ass = splittedLine[columnsNumber["COGNOME"]].Trim();
                                String nome_Ass = splittedLine[columnsNumber["NOME"]].Trim();
                                String fru_matr = splittedLine[columnsNumber["MATRICOLA FRU"]];
                                DateTime data_associazione = Convert.ToDateTime(splittedLine[columnsNumber["DATA ASSOCIAZIONE"]].Trim());

                                if (cognome_Ass == string.Empty)
                                    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_COGNOME_X_VUOTA, rownumber.ToString()));

                                if (nome_Ass == string.Empty)
                                    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_NOME_X_VUOTA, rownumber.ToString()));

                                if (fru_matr == string.Empty)
                                    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_MATRICOLA_X_VUOTA, rownumber.ToString()));

                                if (data_associazione == default(DateTime) || data_associazione < new DateTime(2002, 01, 01))
                                    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_DATA_INIZIALE_X_VUOTA, rownumber.ToString()));

                                if (errorFileds.ToString() == String.Empty)
                                {
                                    newCant.Nome_Assistito_Can = nome_Ass;
                                    newCant.Cognome_Assistito_Can = cognome_Ass;
                                    newCant.Descrizione_Can = String.Concat(cognome_Ass, " ", nome_Ass);
                                    newCant.Codice_Cantiere = codice_Ass;
                                    newCant.Tipologia_Can = "ASS";

                                    newFruCant.Abilitazione_Data_Inizio_Fru_Can = data_associazione;

                                    //viene estratta la FRU in base al codice inserito nel csv
                                    Fru currentFru = RepoManager.FruRepo.FirstOrDefault(fru => fru.Codice_Fru.Trim() == fru_matr);

                                    //se la fru trovata è valorizzata
                                    if (currentFru != default(Fru))
                                        //se la fru è valorizzata viene estratto l'Id
                                        newFruCant.Fru_Id = currentFru.Fru_Id;

                                    //il cantiere creato viene aggiunto al DB in questo modo viene creato l'id del cantiere
                                    RepoManager.CantRepo.Add(newCant);
                                    RepoManager.CantRepo.SaveChanges();
                                    cantAlreadyAdded = true;

                                    //estraggo il cantiere corrispondete al codice cantiere presente nel csv                           
                                    Cant currenCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Codice_Cantiere == newCant.Codice_Cantiere);

                                    //se il cantiere trovato è valorizzato
                                    if (currenCant != default(Cant))
                                        //viene estratto l'id corrispondente e viene inserito nel record di associazione FruCant
                                        newFruCant.Cant_Id = currenCant.Cant_Id;

                                    //il nuovo RecordFruCant viene inserito nella lista da inserire nel db
                                    fruCantToAdd.Add(newFruCant);
                                }
                            }
                        }
                    }
                    rownumber++;
                }

                //se ho errori all'interno del file csv 
                if (errorFileds.ToString() != String.Empty)
                    throw new InvalidOperationException(errorFileds.ToString());

                //se il cantiere è già stato inserito nel db non si ha ad inserirlo di bnuovo
                if (!cantAlreadyAdded)
                {
                    RepoManager.CantRepo.Add(cantsToAdd);
                    RepoManager.CantRepo.SaveChanges();
                }

                //se si hanno associazioni allora le inserisco nella tabella frucant
                if (fruCantToAdd.Any())
                {
                    RepoManager.Fru_CantRepo.Add(fruCantToAdd);
                    RepoManager.Fru_CantRepo.SaveChanges();
                }

                #endregion
            }

            else if (customizationVersion == CantImportTypeEnum.NoAssociazione)
            {
                #region Import delle anagrafiche cantiere SOLO DESCRIZIONE CANTIERE

                // inizializzazione del dizionario che permette di recuperare il posizionamento delle colonne
                Dictionary<string, Int32> columnsNumber = null;

                // inizializzazione della lista utilizzata per l'aggiunta dei cantieri
                var cantsToAdd = new List<Cant>();
                var tempCantsToAdd = new List<Cant>();

                var errorFileds = new StringBuilder();

                bool cantAlreadyAdded = false;

                // inizializzazione dei valori utilizzati per la visualizzazione dei messaggi a video (barra di avanzamento)
                double nRec = inputCants.Count();
                double countRec = 0;
                double percRec = 0;
                int rownumber = 1;


                // per ogni riga del csv passato come parametro
                foreach (String stringVar in inputCants)
                {
                    if (stringVar.Trim() != String.Empty)
                    {
                        // aggiornamento dei dati di percentuale e numero record elaborati
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;

                        // inserimento del messaggio di stato dell'elaborazione
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                           BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORT_RECORD_X_DI_Y.ToString(), countRec.ToString(), nRec.ToString()));

                        String[] splittedLine = stringVar.Split(new Char[] { ';' }, StringSplitOptions.None);
                        var columns = splittedLine.Count();
                        var emptyRow = new StringBuilder();
                        for (var i = 0; i < columns - 1; i++)
                            emptyRow.Append(";");

                        //Segnala errore se manca un'intera riga
                        if (stringVar == emptyRow.ToString() || splittedLine.Length <= 1)
                        {
                            //errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_RIGA_X_VUOTA, rownumber.ToString()));
                            //throw new InvalidOperationException(errorFileds.ToString());
                            continue;
                        }

                        if (errorFileds.ToString() == String.Empty)
                        {
                            // se sto processando la prima linea allora mi salvo il posizionamento delle colonne,
                            // altrimenti procedo all'inserimento del record indicato
                            if (columnsNumber == null)
                            {
                                string[] splittedRow = stringVar.Split(';');
                                columnsNumber = new Dictionary<string, int>();
                                splittedRow = splittedRow.Select(s => s.ToUpperInvariant()).ToArray();
                                columnsNumber.Add("CODICE", Array.IndexOf(splittedRow, "CODICE"));
                                columnsNumber.Add("DESCRIZIONE", Array.IndexOf(splittedRow, "DESCRIZIONE"));
                                columnsNumber.Add("COMUNE", Array.IndexOf(splittedRow, "COMUNE"));
                                columnsNumber.Add("VIA", Array.IndexOf(splittedRow, "VIA"));
                                columnsNumber.Add("LATITUDINE", Array.IndexOf(splittedRow, "LATITUDINE"));
                                columnsNumber.Add("LONGITUDINE", Array.IndexOf(splittedRow, "LONGITUDINE"));
                                columnsNumber.Add("NOTE", Array.IndexOf(splittedRow, "NOTE"));


                                if (columnsNumber.ContainsValue(-1))
                                {
                                    var errorSb = new StringBuilder();
                                    foreach (KeyValuePair<string, Int32> kvp in columnsNumber.Where(kvp => kvp.Value == -1))
                                    {
                                        if (kvp.Key.Equals("CODICE") || kvp.Key.Equals("DESCRIZIONE"))
                                        {
                                            errorSb.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_TESTATA_X_COLONNA, kvp.Key.ToString()));
                                        }
                                    }
                                    if (errorSb.ToString() != String.Empty)
                                        throw new InvalidOperationException(errorSb.ToString());

                                    columnsNumber = null;
                                }
                            }
                            else
                            {
                                // inizializzo il nuovo cantiere e lo compilo con i dati del record
                                Cant newCant = RepoManager.CantRepo.Init();

                                String codice_Can = splittedLine[columnsNumber["CODICE"]].Trim();
                                String descrizione_Can = splittedLine[columnsNumber["DESCRIZIONE"]].Trim();
                                String comune_Can = splittedLine[columnsNumber["COMUNE"]].Trim();
                                String via_Can = splittedLine[columnsNumber["VIA"]].Trim();
                                String longitudine_Can = splittedLine[columnsNumber["LONGITUDINE"]].Trim();
                                String latitudine_Can = splittedLine[columnsNumber["LATITUDINE"]].Trim();
                                String note_Can = splittedLine[columnsNumber["NOTE"]].Trim();

                                //viene controllata che la descrizione sia valorizzata
                                if (descrizione_Can == string.Empty)
                                {
                                    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_DESCRIZIONE_X_VUOTA, rownumber.ToString()));
                                }
                                if (codice_Can == string.Empty)
                                {
                                    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CODICE_CANT_VUOTO, rownumber.ToString()));
                                }

                                if (errorFileds.ToString() == String.Empty)
                                {
                                    //se è attiva la personalizzazione che permette di inserire la descrizione delle attività con lunghezza superiore a 50 caratteri
                                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CanDescriptionOver50Enum) == (int)CanDescriptionOver50Enum.Disable)
                                    {
                                        if (descrizione_Can.Length > 50)
                                        {
                                            descrizione_Can = descrizione_Can.Substring(0, 49);
                                        }
                                    }

                                    //viene aggiunto il codice cantiere aggiungendo gli sapzi a sx
                                    newCant.Codice_Cantiere = CommonService.AggiungiSpaziASinistraSeStringaNumerica(codice_Can, 20);
                                    newCant.Descrizione_Can = descrizione_Can;
                                    newCant.Tipologia_Can = "CAN";

                                    //se il comune è valorizzato
                                    if (comune_Can != string.Empty)
                                    {
                                        //viene cercato il caomune all'interno della tabella dei comuni
                                        Tab_Comuni comune_Cantiere = RepoManager.Tab_ComuniRepo.SingleOrDefault(comune => comune.Luogo_Tab_Comuni == comune_Can);

                                        if (comune_Cantiere != default(Tab_Comuni))
                                        {
                                            comune_Can = comune_Cantiere.Luogo_Tab_Comuni;
                                            newCant.Luogo_Can = comune_Can;
                                        }
                                        else
                                            newCant.Luogo_Can = string.Empty;


                                        //viene controllato che l'indirizzzo sia valorizzato oppure no
                                        if (via_Can != string.Empty)
                                            newCant.Indirizzo_Can = via_Can;
                                    }

                                    else
                                    {
                                        newCant.Indirizzo_Can = String.Empty;
                                        newCant.Luogo_Can = String.Empty;
                                    }

                                    //conversione della latitudine da stringa a double
                                    double latitudineCan = 0;

                                    if (latitudine_Can != String.Empty)
                                    {
                                        latitudineCan = Convert.ToDouble(latitudine_Can);
                                    }

                                    if (latitudineCan != 0)
                                        newCant.LatitudineGps_Can = latitudineCan;
                                    else
                                        newCant.LatitudineGps_Can = 0d;

                                    //conversione della latitudine da stringa a double
                                    double longitudineCan = 0;

                                    if (longitudine_Can != string.Empty)
                                    {
                                        longitudineCan = Convert.ToDouble(longitudine_Can);
                                    }

                                    if (longitudineCan != 0)
                                        newCant.LongitudineGps_Can = longitudineCan;
                                    else
                                        newCant.LongitudineGps_Can = 0d;

                                    if (note_Can != string.Empty)
                                        newCant.Note_Can = note_Can;



                                    //il cantiere creato viene aggiunto al DB in questo modo viene creato l'id del cantiere
                                    RepoManager.CantRepo.Add(newCant);
                                    RepoManager.CantRepo.SaveChanges();
                                    cantAlreadyAdded = true;
                                }
                            }
                        }
                        rownumber++;
                    }

                    //se ho errori all'interno del file csv 
                    if (errorFileds.ToString() != String.Empty)
                        throw new InvalidOperationException(errorFileds.ToString());

                    //se il cantiere è già stato inserito nel db non si ha ad inserirlo di bnuovo
                    if (!cantAlreadyAdded)
                    {
                        RepoManager.CantRepo.Add(cantsToAdd);
                        RepoManager.CantRepo.SaveChanges();
                    }
                }
                #endregion
            }
            else if (customizationVersion == CantImportTypeEnum.Solaris)
            {
                var importClass = CantImportFactory.CreateInstance(customizationVersion, inputCants);
                var importErrors = importClass.Import();
            }
            else if (customizationVersion == CantImportTypeEnum.G4) 
            {
                #region Import delle anagrafiche cantiere G4

                // inizializzazione del dizionario che permette di recuperare il posizionamento delle colonne
                Dictionary<string, Int32> columnsNumber = null;

                // inizializzazione della lista utilizzata per l'aggiunta dei cantieri

                var errorFileds = new StringBuilder();

                List<Cant> cantToInsert = new List<Cant>();    //Lista contente i records che hanno passato la verifica


                // per ogni riga del csv passato come parametro
                foreach (String stringVar in inputCants)
                {

                    String[] splittedLine = stringVar.Split(new Char[] { ';' }, StringSplitOptions.None);
                    var columns = splittedLine.Count();
                    var emptyRow = new StringBuilder();
                    for (var i = 0; i < columns - 1; i++)
                        emptyRow.Append(";");

                    //Segnala errore se manca un'intera riga
                    if (stringVar == emptyRow.ToString() || splittedLine.Length <= 1)
                    {
                        //errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_RIGA_X_VUOTA, rownumber.ToString()));
                        //throw new InvalidOperationException(errorFileds.ToString());
                        continue;
                    }

                    if (errorFileds.ToString() == String.Empty)
                    {

                        // se sto processando la prima linea allora mi salvo il posizionamento delle colonne,
                        // altrimenti procedo all'inserimento del record indicato

                        if (columnsNumber == null)
                        {
                            #region SALVATAGGIO INTESTAZIONE

                            string[] splittedRow = stringVar.Split(';');
                            columnsNumber = new Dictionary<string, int>();
                            splittedRow = splittedRow.Select(s => s.ToUpperInvariant()).ToArray();
                            columnsNumber.Add("CODICE", Array.IndexOf(splittedRow, "CODICE"));
                            columnsNumber.Add("DESCRIZIONE", Array.IndexOf(splittedRow, "DESCRIZIONE"));
                            columnsNumber.Add("CODICE CLIENTE", Array.IndexOf(splittedRow, "CODICE CLIENTE"));
                            columnsNumber.Add("DESCRIZIONE CLIENTE", Array.IndexOf(splittedRow, "DESCRIZIONE CLIENTE"));
                            columnsNumber.Add("MATRICOLA FRU", Array.IndexOf(splittedRow, "MATRICOLA FRU"));
                            columnsNumber.Add("DATA ASSOCIAZIONE", Array.IndexOf(splittedRow, "DATA ASSOCIAZIONE"));
                            columnsNumber.Add("FILIALE", Array.IndexOf(splittedRow, "FILIALE"));
                            columnsNumber.Add("COMUNE", Array.IndexOf(splittedRow, "COMUNE"));
                            columnsNumber.Add("VIA", Array.IndexOf(splittedRow, "VIA"));
                            columnsNumber.Add("LATITUDINE", Array.IndexOf(splittedRow, "LATITUDINE"));
                            columnsNumber.Add("LONGITUDINE", Array.IndexOf(splittedRow, "LONGITUDINE"));
                            columnsNumber.Add("NOTE", Array.IndexOf(splittedRow, "NOTE"));
                            columnsNumber.Add("PROVINCIA", Array.IndexOf(splittedRow, "PROVINCIA"));
                            columnsNumber.Add("CAP", Array.IndexOf(splittedRow, "CAP"));
                            columnsNumber.Add("RAGGIO", Array.IndexOf(splittedRow, "RAGGIO"));
                            columnsNumber.Add("CODICE COMMESSA", Array.IndexOf(splittedRow, "CODICE COMMESSA"));
                            columnsNumber.Add("PIANO", Array.IndexOf(splittedRow, "PIANO"));
                            columnsNumber.Add("UBICAZIONE", Array.IndexOf(splittedRow, "UBICAZIONE"));
                            columnsNumber.Add("NUMERO CHIAVI MAZZI", Array.IndexOf(splittedRow, "NUMERO CHIAVI MAZZI"));
                            columnsNumber.Add("STANZA", Array.IndexOf(splittedRow, "STANZA"));



                            if (columnsNumber.ContainsValue(-1))
                            {

                                var errorSb = new StringBuilder();
                                foreach (KeyValuePair<string, Int32> kvp in columnsNumber.Where(kvp => kvp.Value == -1))
                                {
                                    if (kvp.Key.Equals("CODICE") || kvp.Key.Equals("DESCRIZIONE") || kvp.Key.Equals("MATRICOLA FRU") || kvp.Key.Equals("DATA ASSOCIAZIONE"))
                                    {
                                        errorSb.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_TESTATA_X_COLONNA, kvp.Key.ToString()));
                                    }
                                }

                                if (errorSb.ToString() != String.Empty)
                                    throw new InvalidOperationException(errorSb.ToString());

                                columnsNumber = null;
                            }


                            #endregion
                        }
                        else
                        {

                            #region ESTRAZIONE DATI

                            string codice_Can = splittedLine[columnsNumber["CODICE"]].Trim();
                            string descrizione_Can = splittedLine[columnsNumber["DESCRIZIONE"]].Trim();
                            string cod_Cliente = splittedLine[columnsNumber["CODICE CLIENTE"]].Trim();
                            string descrizione_Cli = splittedLine[columnsNumber["DESCRIZIONE CLIENTE"]].Trim();
                            string fru_matr = splittedLine[columnsNumber["MATRICOLA FRU"]];
                            string filiale = splittedLine[columnsNumber["FILIALE"]];
                            DateTime? data_associazione = (splittedLine[columnsNumber["DATA ASSOCIAZIONE"]] != "") ? Convert.ToDateTime(splittedLine[columnsNumber["DATA ASSOCIAZIONE"]].Trim()) as DateTime? : null;
                            string comune = splittedLine[columnsNumber["COMUNE"]].Trim();
                            string via = splittedLine[columnsNumber["VIA"]].Trim();
                            string latit = splittedLine[columnsNumber["LATITUDINE"]].Trim();
                            string longi = splittedLine[columnsNumber["LONGITUDINE"]].Trim();
                            string note = splittedLine[columnsNumber["NOTE"]].Trim();
                            string provincia = splittedLine[columnsNumber["PROVINCIA"]].Trim();
                            string cap = splittedLine[columnsNumber["CAP"]].Trim();
                            string raggio = splittedLine[columnsNumber["RAGGIO"]].Trim();
                            string codicecommessa = splittedLine[columnsNumber["CODICE COMMESSA"]].Trim();
                            string piano = splittedLine[columnsNumber["PIANO"]].Trim();
                            string ubicazione = splittedLine[columnsNumber["UBICAZIONE"]].Trim();
                            string numeroChiaviMazzi = splittedLine[columnsNumber["NUMERO CHIAVI MAZZI"]].Trim();
                            string stanza = splittedLine[columnsNumber["STANZA"]].Trim();

                            #endregion

                            #region CONTROLLO CAMPI

                            var exCant = RepoManager.CantRepo.FirstOrDefault(c => c.Codice_Cantiere.Trim() == codice_Can.Trim());

                            Cant cantiere = null;

                            if (codice_Can != "" && descrizione_Can != "" && exCant == null)     //Obbligatorio cod,desc e che non esista cantiere con lo stesso codice
                            {
                                cantiere = RepoManager.CantRepo.Init();
                                cantiere.Codice_Cantiere = CommonService.AggiungiSpaziASinistraSeStringaNumerica(codice_Can, 20);
                                cantiere.Descrizione_Can = descrizione_Can;

                                if (filiale != "")
                                {
                                    var existFil = RepoManager.FilRepo.FirstOrDefault(f => f.Descrizione_Fil == filiale);

                                    cantiere.Fil = existFil ?? null;

                                }

                                if (comune != "")
                                {
                                    var exist = RepoManager.Tab_ComuniRepo.FirstOrDefault(x => x.Luogo_Tab_Comuni == comune);
                                    cantiere.Luogo_Can = (exist != null) ? splittedLine[columnsNumber["COMUNE"]].Trim() : "";
                                    cantiere.Cap_Can = (exist != null) ? exist.Cap_Tab_Comuni : "";
                                    cantiere.Provincia_Can = (exist != null) ? exist.Codice_Prov_Tab_Comuni : "";
                                }

                                via = splittedLine[columnsNumber["VIA"]].Trim();
                                cantiere.Tipologia_Can = "CAN";
                                cantiere.Indirizzo_Can = (via.Length < 50) ? via : via.Substring(0, 49);
                                cantiere.LatitudineGps_Can = (latit != "") ? Double.Parse(latit) : 0;
                                cantiere.LongitudineGps_Can = (longi != "") ? Double.Parse(longi) : 0;
                                cantiere.Note_Can = note;



                                if (cod_Cliente != "" && descrizione_Cli != "")
                                {
                                    try
                                    {
                                        var exCli = RepoManager.CliRepo.FirstOrDefault(c => c.Codice_Cliente.Trim() == cod_Cliente.Trim());
                                        if (exCli == null)
                                        {
                                            Cli cliente = RepoManager.CliRepo.Init();
                                            cliente.Codice_Cliente = CommonService.AggiungiSpaziASinistraSeStringaNumerica(cod_Cliente, 10);
                                            cliente.Cognome_Cli = descrizione_Cli;
                                            cantiere.Cli = cliente;
                                        }
                                        else
                                        {
                                            cantiere.Cli = exCli;
                                        }
                                    }
                                    catch (Exception) { }
                                }

                                if (fru_matr != "" && (fru_matr.Length == 10 || fru_matr.Length == 5) && data_associazione != null)
                                {
                                    Fru currentFru = RepoManager.FruRepo.FirstOrDefault(f => f.Codice_Fru.Trim() == fru_matr);
                                    Fru_Cant fru_cant = RepoManager.Fru_CantRepo.Init();

                                    if (currentFru != default(Fru))
                                    {
                                        //se la fru è valorizzata viene estratto l'Id
                                        fru_cant.Fru = currentFru;
                                        fru_cant.Abilitazione_Data_Inizio_Fru_Can = (DateTime)data_associazione;
                                    }
                                    else
                                    {
                                        Fru newFru = RepoManager.FruRepo.Init();

                                        newFru.DataOraUltimaModifica_Fru = DateTime.Now;
                                        newFru.N_Serie_Fru = fru_matr;
                                        newFru.Codice_Fru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(fru_matr, 5);
                                        fru_cant.Abilitazione_Data_Inizio_Fru_Can = (DateTime)data_associazione;

                                        fru_cant.Fru = newFru;
                                    }

                                    cantiere.Fru_Cant.Add(fru_cant);
                                }
                                if (comune != "" && via != "" && provincia != "" && cap.Length == 5)
                                {
                                    string indirizzo = String.Format("{0} {1} {2} {3}", via, cap, comune, provincia);
                                    Location geocode = BusinessService.GetGeocode(indirizzo);
                                    if (geocode != null)
                                    {
                                        cantiere.LatitudineGps_Can = geocode.Latitudine;
                                        cantiere.LongitudineGps_Can = geocode.Longitudine;
                                    }
                                }

                                if (raggio != "")
                                {
                                    if (raggio.Contains(","))
                                    {
                                        var tmp = raggio.Split(',');
                                        raggio = tmp[0];
                                    }
                                    short s;
                                    if (!short.TryParse(raggio, out s))
                                    {
                                        s = 0;
                                    }
                                    cantiere.RaggioGps_Can = s;
                                }

                                if (codicecommessa != "") 
                                {
                                    cantiere.Codice_Commessa_Can = codicecommessa;
                                }

                                if (piano != "")
                                {
                                    cantiere.Telefono_1_Can = piano;
                                }

                                if (ubicazione != "")
                                {
                                    cantiere.Telefono_1_Rif_Can = ubicazione;
                                }

                                if (numeroChiaviMazzi != "")
                                {
                                    cantiere.Telefono_2_Can = numeroChiaviMazzi;
                                }

                                if (stanza != "")
                                {
                                    cantiere.Telefono_2_Rif_Can = stanza;
                                }

                                #endregion

                            }

                            if (cantiere != null)
                                cantToInsert.Add(cantiere);

                        }
                    }
                }

                try
                {
                    DbSet.AddRange(cantToInsert);
                    BulkSaveChanges(bulk => bulk.BatchSize = 100);
                }
                catch (Exception ex)
                {
                    _log.ErrorFormat("Errore durante l'inserimento dei cantieri da CSV a causa dell'exception {0}", ex.Message);
                    errors.Add("Errore", "Errore durante l'inserimento dei cantieri! Contattare l'assistenza!");
                }

                #endregion
            }


            return errors;
        }

        private int GetCodCli(Dictionary<string, Int32> columnsNumber, String[] splittedLine, Fil fil)
        {
            int newCliId = 0;
            Cli cliToSearch = default(Cli);

            Fil privatoFil = RepoManager.FilRepo.FirstOrDefault(f => f.Codice_Fil.ToUpper() == "PRIVATO");
            if (privatoFil != default(Fil) && fil == privatoFil)
            {
                cliToSearch = RepoManager.CliRepo.SingleOrDefault(cli => cli.Codice_Cliente.ToUpper() == "PRIVATO");
            }

            else if (splittedLine[columnsNumber["EXTRA REGIONE"]].Trim().ToUpper() == "SI")
            {
                cliToSearch = RepoManager.CliRepo.SingleOrDefault(cli => cli.Codice_Cliente.ToUpper() == "EX-REG");
            }

            else if (splittedLine[columnsNumber["EXTRA REGIONE"]].Trim().ToUpper() == "NO")
            {

                cliToSearch = RepoManager.CliRepo.SingleOrDefault(cli => cli.Codice_Cliente.ToUpper() == "PUBBLICO");
            }

            if (cliToSearch != null)
                newCliId = cliToSearch.Cli_Id;

            return newCliId;
        }

        public void AddPendingElabForActivity(Cant entity)
        //Carica nella Tabella PendingElab La Data della Minima e la Massima Registrazione (che siamo maggiori della data Blocco)
        //di quel Cantiere che ha cambiato la Tipologia
        {
            var oldCant = Single(cant => cant.Cant_Id == entity.Cant_Id);

            var blockDate = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg;
            //Lettura di Tutte le REG di Quel Cantiere con data > della DATA BLOCCO
            var allCantRegs = RepoManager.Reg_VRepo.Find(reg => reg.Data_Reg > blockDate && reg.Cant_Id == oldCant.Cant_Id, true).ToList();
            if (allCantRegs.Count > 0)
            {
                var from = allCantRegs.Min(regv => regv.Data_Reg);
                var to = allCantRegs.Max(regv => regv.Data_Reg);
                to = to.Value.AddDays(1);
                //Scrive nella tabella PendingElab le Date per le quali devono poi essere rielaborate le Registrazioni 
                //a fronte del fatto che quel Cantiere ha Cambiato la Tipologia da/a ATTIVITA'
                RepoManager.PendingElabRepo.Add(new PendingElab { FromDate_PendingElab = from, ToDate_PendingElab = to }, true);
            }
        }

        /// <summary>
        /// Inizializza un nuovo cantiere GPS a partire dalla latitudine e longitudine passata come parametro.
        /// </summary>
        /// <param name="cantLatitude">La latitudine del cantiere da generare.</param>
        /// <param name="cantLongitude">La longitudine del cantiere da generare.</param>
        /// <returns>
        /// Il nuovo cantiere gps con tutti i dati ricavabili
        /// </returns>
        public Cant InitNewGpsCant(double cantLatitude, double cantLongitude)
        {
            // inizializzazione del nuovo cantiere
            Cant newCant = Init();

            try
            {
                // impostazione di latitudine e longitudine
                newCant.LatitudineGps_Can = cantLatitude;
                newCant.LongitudineGps_Can = cantLongitude;

                // impostazione dell'indirizzo sul cantiere recuperato da latitudine e longitudine
                BusinessService.SetCantAddressFromGpsPoint(newCant);

                // impostazione della descrizione del cantiere di default se non è stato recuperato indirizzo o CAP, come indirizzo + CAP altrimenti
                // newCant.Descrizione_Can = BusinessService.GetLocalizedString(PowerWebResources.STR_DEFAULT_CANT_GPS_DES);
                if (newCant.Indirizzo_Can != null || newCant.Cap_Can != null || newCant.Luogo_Can != null) {
                    newCant.Descrizione_Can = String.Format("{0}, {1} - {2}", newCant.Indirizzo_Can, newCant.Luogo_Can, newCant.Cap_Can);
                } else {
                    newCant.Descrizione_Can = BusinessService.GetLocalizedString(PowerWebResources.STR_DEFAULT_CANT_GPS_DES);
                }
                //newCant.Descrizione_Can = (newCant.Indirizzo_Can == null || newCant.Cap_Can == null) ? BusinessService.GetLocalizedString(PowerWebResources.STR_DEFAULT_CANT_GPS_DES) : String.Format("{0}, {1} - {2}", newCant.Indirizzo_Can, newCant.Luogo_Can, newCant.Cap_Can);

                // inserimento del codice del cantiere; se esiste la numerazione automatica del cantiere alla si procede con la stessa
                // (naturalmente solo se tutti i cantieri sono numerici); altrimenti si procede alla generazione
                var codCanMax = RepoManager.CantRepo.DbSet.Any() ? RepoManager.CantRepo.Max(c => c.Codice_Cantiere, true) : "0";
                int codCanMaxNum = 0;
                if (RepoManager.ParamRepo.ParametersRow.Attiva_Num_Aut_Can && int.TryParse(codCanMax, out codCanMaxNum))
                {
                    // numerazione automatica attiva e dati pregressi corretti:
                    // si procede con la numerazione automatica
                    newCant.Codice_Cantiere = CommonService.AggiungiSpaziASinistraSeStringaNumerica((++codCanMaxNum).ToString(), 20);
                }
                else // numerazione automatica non attiva o ultimo cantiere non numerico: si procede alla gestione standard dei nuovi cantieri GPS
                {
                    // si recupera l'ultimo cantiere il cui codice parte con il prefisso configurato;
                    // se non presente si parte da 1, altrimenti si incrementa l'esistente
                    Cant lastGpsCant = Find(cant => cant.Codice_Cantiere.StartsWith(Common.Properties.Settings.Default.GpsCantCodePrefix)).OrderByDescending(cant => cant.Codice_Cantiere).FirstOrDefault();

                    if (lastGpsCant == default(Cant))
                        newCant.Codice_Cantiere = String.Format("{0}{1}", Common.Properties.Settings.Default.GpsCantCodePrefix, 1.ToString(Common.Properties.Settings.Default.GpsCantNumberStringFormat));
                    else
                    {
                        int lastNumericPart = Convert.ToInt32(lastGpsCant.Codice_Cantiere.Substring(lastGpsCant.Codice_Cantiere.LastIndexOf(Common.Properties.Settings.Default.GpsCantCodePrefix.Last()) + 1));
                        newCant.Codice_Cantiere = String.Format("{0}{1}", Common.Properties.Settings.Default.GpsCantCodePrefix, (++lastNumericPart).ToString(Common.Properties.Settings.Default.GpsCantNumberStringFormat));
                    }
                }
            }
            catch (Exception e)
            {
                // in caso d'errore si ritorna un cantiere vuoto
                newCant = default(Cant);
            }

            // ritorno del nuovo cantiere generato dal metodo
            return newCant;
        }

        public override Expression<Func<Cant, bool>> Filter
        {
            get
            {
                if (PowerWebContext.Current.DomainFilter /*& DomainFilterEnum.Fil)*/ == DomainFilterEnum.Fil && PowerWebContext.Current.Fils != null && PowerWebContext.Current.User.Liv_Utente < 10)
                {
                    var allFilIds = PowerWebContext.Current.Fils.Select(fil => fil.Fil_Id).ToList();
                    return cant =>/*/* cant.Fil_Id == null || */allFilIds.Contains(cant.Fil_Id.Value);
                } else if (PowerWebContext.Current.DomainFilter == DomainFilterEnum.Both && PowerWebContext.Current.Fils.Count > 0 && PowerWebContext.Current.User.Liv_Utente < 10) {
                    var allFilIds = PowerWebContext.Current.Fils.Select(fil => fil.Fil_Id).ToList();
                    return cant => cant.Fil_Id == null || allFilIds.Contains(cant.Fil_Id.Value);
                } else return base.Filter;
            }
        }



        public void UpdateGeoLocation(Cant cant)
        {
            if (String.IsNullOrEmpty(cant.GeocodeAddress) || String.IsNullOrWhiteSpace(cant.GeocodeAddress))
                return;

            try
            {
                cant.LatitudineGps_Can = cant.LongitudineGps_Can = 0;

                GeocodeRequest request = new GeocodeRequest();

                request.BingMapsKey = RepoManager.ParamRepo.ParametersRow.BingKey;

                request.Query = cant.GeocodeAddress;

                request.MaxResults = 1;

                var geoRes = Task.Run(() => ServiceManager.GetResponseAsync(request)).Result;

                if (geoRes.StatusCode == 200)
                {
                    //var points = ((Location)geoRes.ResourceSets[0].Resources[0]).Point.Coordinates;
                    //if (points.Any())
                    //{
                    //
                    //    cant.LatitudineGps_Can = points[0];
                    //    cant.LongitudineGps_Can = points[1];
                    //}

                    SaveChanges();
                }

            }
            catch (Exception ex)
            {
                _log.Error(String.Format("Errore durante la generazione delle coordinate tramite GPS dal cantiere {0}", cant.Codice_Cantiere));
            }

        }

        private List<Dictionary<string, object>> GetModifiedProperties(IEnumerable<DbEntityEntry> entries)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();


            foreach (var ent in entries)
            {
                Dictionary<string, object> modProp = new Dictionary<string, object>();

                foreach (var propName in ent.CurrentValues.PropertyNames)
                {
                    var current = ent.CurrentValues[propName];
                    var original = ent.OriginalValues[propName];

                    if (current != original)
                    {
                        modProp.Add(propName, current);
                    }
                }

                result.Add(modProp);
            }

            return result;
        }

    }
}

