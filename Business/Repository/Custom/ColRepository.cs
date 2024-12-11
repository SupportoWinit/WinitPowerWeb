using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;
using System.Collections;
using Business.MDBSchema;
using System.Linq.Expressions;
using log4net;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using BingMapsRESTToolkit;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Z.BulkOperations;
using Business.IocFactory.ClockAppSynchronizationFactory;
using Business.Synchronization.SynchronizatioManager.Implementations;

namespace Business.Repository.Custom
{
    public class ColRepository : GenericRepository<Col>, IColRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ColRepository));
        

        SynchronizationManager<Col> synchronizationManager;

        public ColRepository(PowerWebEntities context, SynchronizationManager<Col> synchronizationManager)
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
        private static List<Tab_Decod> Tab_Decods
        {
            get
            {
                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_ColRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_DecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Cant> Cants
        {
            get
            {
                List<Cant> oLista = PowerWebContext.GetFromSession<List<Cant>>("Cants_ColRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.CantRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cant>>("Cants_ColRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Tab_Comuni> Tab_Comunis
        {
            get
            {
                List<Tab_Comuni> oLista = PowerWebContext.GetFromSession<List<Tab_Comuni>>("Tab_Comunis_ColRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_ComuniRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Comuni>>("Tab_Comunis_ColRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Tab_Prov> Tab_Provs
        {
            get
            {
                List<Tab_Prov> oLista = PowerWebContext.GetFromSession<List<Tab_Prov>>("Tab_Provs_ColRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_ProvRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Prov>>("Tab_Provs_ColRepo", oLista);
                }
                return oLista;
            }
        }
        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Resp>>("Resps_ColRepo", null);
            PowerWebContext.SetToSession<List<Cant>>("Cants_ColRepo", null);
            PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
            PowerWebContext.SetToSession<List<Tab_Prov>>("Tab_Provs_ColRepo", null);
            PowerWebContext.SetToSession<List<Tab_Comuni>>("Tab_Comunis_ColRepo", null);
        }

        public override Col Init()
        {
            Col oNewRecord = base.Init();
            oNewRecord.Assegni_Famigliari_Col = false;
            oNewRecord.Automunito_Col = false;
            oNewRecord.Disabile_Col = false;
            oNewRecord.DisAbilitazione_Col = false;
            oNewRecord.Flag_Monte_Ore = false;
            oNewRecord.Metodo_Arrotondamento_Col = 0;
            oNewRecord.Singola_Reg = false;
            oNewRecord.Straniero_CEE_Col = false;
            oNewRecord.Straniero_Col = false;
            oNewRecord.Tipo_Arrotondamento_Col = 0;
            oNewRecord.Data_Registrazione_Col = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Col = DateTime.UtcNow;
            return oNewRecord;
        }
        public override void SetEntityBeforeAddOrUpdate(Col entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Col;
            entity.DataOraUltimaModifica_Col = DateTime.UtcNow;
            entity.Codice_Collaboratore = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Collaboratore, 20);
            //Se è attiva la umerazione Automatica dei Cantieri in Param allora forzo il Codice Cantiere incrementandone il Codice dell'Ultimo Cantiere esistente
        }

        public override Dictionary<string, string> Check(Col entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession();
            try
            {
                //Se è attivata la NUM AUT in Scheda PARAM allora cerco l'ultimo Cantiere esistente e lo incremento di 1 (dopo aver verifico che fosse numerico)
                // se non ci sono presenti collaboratori si da per scontato che il codice sia numerico, così da poter avviare la procedura in caso di anagrafica vuota
                var codColMax = RepoManager.ColRepo.DbSet.Any() ? RepoManager.ColRepo.Max(c => c.Codice_Collaboratore, false) : "0";
                int codColMaxNum = 0;
                if (RepoManager.ParamRepo.ParametersRow.Attiva_Num_Aut_Col)
                {
                    //verifico che l'Ultimo CANTIERE abbia Codice NUMERICO                                        
                    if (int.TryParse(codColMax, out codColMaxNum) == false)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Collaboratore),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_ULTIMO_CANTIERE_NON_NUMERICO, PowerWebResources.FLD_CODICE_CANTIERE));
                }
                //Leggo la DataOraUltimaModifica ATTUALE dal REcord del DB per verificare che nessuno abbia modificato il Record nel frattempo               
                if (!isNew)
                {
                    DateTime DataOraRecordDb = RepoManager.ColRepo.Single(u => u.Col_Id == entity.Col_Id).DataOraUltimaModifica_Col;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_COL));
                    }
                }

                //
                // verifico, per una serie di campi, che il valore del campo sia impostato perché è obbligatorio e che sia univoco
                //
                // viene verifcata l'obbligatorietà del codice collaboratore solo se non è richiesta la numerazione antumatica
                if (String.IsNullOrEmpty(entity.Codice_Collaboratore) && !RepoManager.ParamRepo.ParametersRow.Attiva_Num_Aut_Col)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Collaboratore),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_COLLABORATORE));
                else
                {
                    entity.Codice_Collaboratore = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Collaboratore, 20);
                    if (isNew)
                    {
                        if (RepoManager.ColRepo.SingleOrDefault(u => u.Codice_Collaboratore == entity.Codice_Collaboratore) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Collaboratore),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.ColRepo.SingleOrDefault(u => u.Codice_Collaboratore == entity.Codice_Collaboratore && u.Col_Id != entity.Col_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Collaboratore),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //
                if (CommonService.Nz(entity.Cognome_Col, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cognome_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_COGNOME_COL));
                if (CommonService.Nz(entity.Nome_Col, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOME_COL));
                //
                ////Se in PARAM la gestione dei PAssaggi NON è abilitata verifico che il Collaboratore non venga abilitato come Gestito a Passaggio
                if (CommonService.Nz(entity.Singola_Reg, false) == true && RepoManager.ParamRepo.ParametersRow.Abilita_Pass == false)
                {
                    if (CommonService.Nz(entity.Singola_Reg, false) == true)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Singola_Reg),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_GESTIONE_PASSAGGI_NON_ABILITATA, PowerWebResources.FLD_SINGOLA_REG));
                }

                //
                // verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                if (CommonService.Nz(entity.Arrot_Durata_Col, 0) < CommonService.Nz(entity.Soglia_Durata_Col, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrot_Durata_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_SOGLIA_DURATA_COL, PowerWebResources.FLD_ARROT_DURATA_COL));
                if (CommonService.Nz(entity.ArrotF_Col, 0) < CommonService.Nz(entity.SogliaF_Col, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.ArrotF_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_SOGLIAF_COL, PowerWebResources.FLD_ARROTF_COL));
                if (CommonService.Nz(entity.ArrotI_Col, 0) < CommonService.Nz(entity.SogliaI_Col, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.ArrotI_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_SOGLIAI_COL, PowerWebResources.FLD_ARROTI_COL));
                if (CommonService.Nz(entity.Minuti_Arrot_Durata_Fig_Col, 0) < CommonService.Nz(entity.Soglia_Arrot_Durata_Fig_Col, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Minuti_Arrot_Durata_Fig_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_SOGLIA_ARROT_DURATA_FIG_COL, PowerWebResources.FLD_MINUTI_ARROT_DURATA_FIG_COL));
                if (CommonService.Nz(entity.Durata_Max_Ril_Col, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00) &&
                       CommonService.Nz(entity.Durata_Min_Ril_Col, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00))
                    if (CommonService.Nz(entity.Durata_Max_Ril_Col, new TimeSpan(00, 00, 00)) < CommonService.Nz(entity.Durata_Min_Ril_Col, new TimeSpan(00, 00, 00)))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Max_Ril_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                          PowerWebResources.FLD_DURATA_MAX_RIL_COL, PowerWebResources.FLD_DURATA_MAX_RIL_COL));
                if (CommonService.Nz(entity.Durata_Max_Gruppo_Ril_Col, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00) &&
                       CommonService.Nz(entity.Durata_Max_Ril_Col, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00))
                    if (CommonService.Nz(entity.Durata_Max_Gruppo_Ril_Col, new TimeSpan(00, 00, 00)) < CommonService.Nz(entity.Durata_Max_Ril_Col, new TimeSpan(00, 00, 00)))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Max_Gruppo_Ril_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                          PowerWebResources.FLD_DURATA_MAX_GRUPPO_RIL_COL, PowerWebResources.FLD_DURATA_MAX_RIL_COL));
                if (CommonService.Nz(entity.Durata_Min_Ril_Col, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00) &&
                       CommonService.Nz(entity.Durata_Max_Gruppo_Ril_Col, new TimeSpan(00, 00, 00)) != new TimeSpan(00, 00, 00))
                    if (CommonService.Nz(entity.Durata_Min_Ril_Col, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Durata_Max_Gruppo_Ril_Col, new TimeSpan(00, 00, 00)))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Min_Ril_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                          PowerWebResources.FLD_DURATA_MAX_GRUPPO_RIL_COL, PowerWebResources.FLD_DURATA_MIN_RIL_COL));
                if (CommonService.Nz(entity.Fascia_Ore_Viaggi_1_Inizio_Col, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Fascia_Ore_Viaggi_1_Fine_Col, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fascia_Ore_Viaggi_1_Inizio_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_FASCIA_ORE_VIAGGI_1_INIZIO_COL, PowerWebResources.FLD_FASCIA_ORE_VIAGGI_1_FINE_COL));
                if (CommonService.Nz(entity.Fascia_Ore_Viaggi_2_Inizio_Col, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Fascia_Ore_Viaggi_2_Fine_Col, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fascia_Ore_Viaggi_2_Inizio_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_FASCIA_ORE_VIAGGI_2_INIZIO_COL, PowerWebResources.FLD_FASCIA_ORE_VIAGGI_2_FINE_COL));
                if (CommonService.Nz(entity.Fascia_Ore_Viaggi_3_Inizio_Col, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Fascia_Ore_Viaggi_3_Fine_Col, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fascia_Ore_Viaggi_3_Inizio_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_FASCIA_ORE_VIAGGI_3_INIZIO_COL, PowerWebResources.FLD_FASCIA_ORE_VIAGGI_3_FINE_COL));
                if (CommonService.Nz(entity.Fascia_Ore_Viaggi_4_Inizio_Col, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Fascia_Ore_Viaggi_4_Fine_Col, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fascia_Ore_Viaggi_4_Inizio_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_FASCIA_ORE_VIAGGI_4_INIZIO_COL, PowerWebResources.FLD_FASCIA_ORE_VIAGGI_4_FINE_COL));
                if (CommonService.Nz(entity.Fascia_Ore_Viaggi_5_Inizio_Col, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Fascia_Ore_Viaggi_5_Fine_Col, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fascia_Ore_Viaggi_5_Inizio_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_FASCIA_ORE_VIAGGI_5_INIZIO_COL, PowerWebResources.FLD_FASCIA_ORE_VIAGGI_5_FINE_COL));
                //
                // verifico, per una serie di campi, che la data di un campo sia minore della data di un altro campo
                //

                if (CommonService.Nz(entity.Assegni_Famigliari_Inizio_Col, new DateTime(1, 1, 1))
                > CommonService.Nz(entity.Assegni_Famigliari_Fine_Col, new DateTime(9999, 1, 1)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Assegni_Famigliari_Inizio_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_INIZIO_X_DEVE_ESSERE_MINORE_DI_DATA_FINE_Y,
                        PowerWebResources.FLD_ASSEGNI_FAMIGLIARI_INIZIO_COL, PowerWebResources.FLD_ASSEGNI_FAMIGLIARI_FINE_COL));
                if (CommonService.Nz(entity.Data_Disponibilita_Inizio_Col, new DateTime(1, 1, 1))
                > CommonService.Nz(entity.Data_Disponibilita_Fine_Col, new DateTime(9999, 1, 1)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Disponibilita_Inizio_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_INIZIO_X_DEVE_ESSERE_MINORE_DI_DATA_FINE_Y,
                      PowerWebResources.FLD_DATA_DISPONIBILITA_INIZIO_COL, PowerWebResources.FLD_DATA_DISPONIBILITA_FINE_COL));
                //
                // verifico, per una serie di campi, che il valore del campo sia minore o minore di un certo valore
                //
                if (CommonService.Nz(entity.Arrot_Durata_Col, 0) != 0)
                    if (entity.Arrot_Durata_Col > 60)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrot_Durata_Col),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                           PowerWebResources.FLD_ARROT_DURATA_COL, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.ArrotF_Col, 0) != 0)
                    if (entity.ArrotF_Col > 60)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.ArrotF_Col),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                           PowerWebResources.FLD_ARROTF_COL, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.ArrotI_Col, 0) != 0)
                    if (entity.ArrotI_Col > 60)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.ArrotI_Col),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                           PowerWebResources.FLD_ARROTI_COL, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.GGConsMax_Col, 0) != 0)
                    if (entity.GGConsMax_Col > 100)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.GGConsMax_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                          PowerWebResources.FLD_GGCONSMAX_COL, PowerWebResources.VALORE_100));
                if (CommonService.Nz(entity.OreMassime, 0) != 0)
                    if (entity.OreMassime > 744)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.OreMassime),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                          PowerWebResources.FLD_OREMASSIME, PowerWebResources.VALORE_744));
                if (CommonService.Nz(entity.Prova, 0) != 0)
                    if (entity.Prova > 1)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Prova),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                          PowerWebResources.FLD_PROVA, PowerWebResources.VALORE_1));
                if (CommonService.Nz(entity.Soglia_Arrot_Durata_Fig_Col, 0) != 0)
                    if (entity.Soglia_Arrot_Durata_Fig_Col > 60)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Soglia_Arrot_Durata_Fig_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                          PowerWebResources.FLD_SOGLIA_ARROT_DURATA_FIG_COL, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Soglia_Durata_Col, 0) != 0)
                    if (entity.Soglia_Durata_Col > 60)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Soglia_Durata_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                          PowerWebResources.FLD_SOGLIA_DURATA_COL, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.SogliaF_Col, 0) != 0)
                    if (entity.SogliaF_Col > 60)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.SogliaF_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                          PowerWebResources.FLD_SOGLIAF_COL, PowerWebResources.VALORE_60));

                //
                // verifico correttezza codice fiscale
                //            
                if (RepoManager.ParamRepo.ParametersRow.Ctrl_Codice_FiscEnum == CheckCodFiscEnum.Checked)
                    if (CommonService.Nz(entity.Codice_Fiscale_Col, "") != "")
                    {
                        //Lo imposto TUTTO MAIUSCOLO
                        entity.Codice_Fiscale_Col = entity.Codice_Fiscale_Col.ToUpper();
                        String sCodiceFiscaleApp = "";
                        //se manca anche un solo dato allora non faccio controllo
                        if (CommonService.Nz(entity.Nome_Col, "") == "" ||
                        CommonService.Nz(entity.Cognome_Col, "") == "" ||
                        CommonService.Nz(entity.Nascita_Data_Col, null) == null ||
                        CommonService.Nz(entity.Sesso_Col, "") == "" ||
                        CommonService.Nz(entity.Nascita_Luogo_Col, "") == "" ||
                        CommonService.Nz(entity.Nascita_Provincia_Col, "") == "")
                        {
                            //controllo solo se è formalmente corretto
                            if (!CommonService.ECodiceFiscaleValido(entity.Codice_Fiscale_Col))
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fiscale_Col),
                                  BusinessService.GetLocalizedString(PowerWebResources.ERR_CODICE_FISCALE_ERRATO));
                        }
                        else
                        {
                            //se ci sono tutti i dati necessari al calcolo del codice fiscale lo controllo ed eventualmente lo segnalo errato
                            sCodiceFiscaleApp = BusinessService.GeneraCodiceFiscale(
                              entity.Nome_Col,
                              entity.Cognome_Col,
                              (DateTime)entity.Nascita_Data_Col,
                              BusinessService.GetLocalizedString(entity.Sesso_Col),
                              entity.Nascita_Luogo_Col,
                              entity.Nascita_Provincia_Col);
                            //Se il Codice Fiscale Restituito inizia con ERR_ significa che contiene un MESSAGGIO DI ERRORE che va Decodificato in Lingua
                            if (sCodiceFiscaleApp != "" && sCodiceFiscaleApp.Substring(0, 4) == "ERR_")
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fiscale_Col),
                                  BusinessService.GetLocalizedString(PowerWebResources.ERR_DATI_NECESSARI_MANCANTI));
                            else if (sCodiceFiscaleApp != entity.Codice_Fiscale_Col)
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fiscale_Col),
                                  BusinessService.GetLocalizedString(PowerWebResources.ERR_CODICE_FISCALE_ERRATO));
                        }
                    }
                // verifico correttezza codice IBAN      

                if (RepoManager.ParamRepo.ParametersRow.Ctrl_Codice_IBANEnum == CheckIBANEnum.Checked)
                    if (CommonService.Nz(entity.Codice_Iban_Col, "") != "")
                    {
                        //Lo imposto maisucolo
                        entity.Codice_Iban_Col = entity.Codice_Iban_Col.ToUpper();
                        if (CommonService.EIbanValido(entity.Codice_Iban_Col))
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Iban_Col),
                              BusinessService.GetLocalizedString(PowerWebResources.STR_VALORE_CAMPO_X_ERRATO,
                              PowerWebResources.FLD_CODICE_IBAN_COL));
                    }

                // verifico correttezza Flag_Monte_ore e Data_Inizio_Disponibilità
                // Nel caso in cui il Flag_Monte_Ore di Param sia ABILITATO in Modo INCLUSIVE
                //      allora quando il Flag_Monte_Ore è ABilitato allora deve essere presente ANCHE la Data_Inizio_Disponibilità
                //Nel caso in cui il Flag_Monte_Ore di Param sia ABILITATO in Modo ESCLUSIVE
                //      allora quando il Flag_Monte_Ore è Disabilitato allora deve essere presente ANCHE la Data_Inizio_Disponibilità
                if (RepoManager.ParamRepo.ParametersRow.MonthlyHoursEnum != MothlyHoursEnum.Inclusive)
                {
                    if (CommonService.Nz(entity.Flag_Monte_Ore, false) == true &&
                        CommonService.Nz(entity.Data_Disponibilita_Inizio_Col, new DateTime(1, 1, 1)) == new DateTime(1, 1, 1))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Disponibilita_Inizio_Col),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_INIZIO_DISPONIBILITA_OBBLIGATORIA_PER_FLAG_MONTE_ORE_ATTIVATO,
                                PowerWebResources.FLD_DATA_DISPONIBILITA_INIZIO_COL));
                }
                else
                {
                    if (CommonService.Nz(entity.Flag_Monte_Ore, true) == false &&
                            CommonService.Nz(entity.Data_Disponibilita_Inizio_Col, new DateTime(1, 1, 1)) == new DateTime(1, 1, 1))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Disponibilita_Inizio_Col),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_INIZIO_DISPONIBILITA_OBBLIGATORIA_PER_FLAG_MONTE_ORE_ATTIVATO,
                                PowerWebResources.FLD_DATA_DISPONIBILITA_INIZIO_COL));
                }

                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Codice_Collaboratore, "") != "")
                    if (entity.Codice_Collaboratore.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Collaboratore),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_COLLABORATORE, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Codice_Domicilio_Luogo_Col, "") != "")
                    if (entity.Codice_Domicilio_Luogo_Col.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Domicilio_Luogo_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_DOMICILIO_LUOGO_COL, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Codice_Fiscale_Col, "") != "")
                    if (entity.Codice_Fiscale_Col.Length > 16 && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DisabilitaCodiceFiscale16Caratteri) == (int)DisabilitaCodiceFiscale16Caratteri.Enabled)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fiscale_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_FISCALE_COL, PowerWebResources.VALORE_16));
                if (CommonService.Nz(entity.Codice_Iban_Col, "") != "")
                    if (entity.Codice_Iban_Col.Length > 40)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Iban_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_IBAN_COL, PowerWebResources.VALORE_40));
                if (CommonService.Nz(entity.Codice_Nascita_Luogo_Col, "") != "")
                    if (entity.Codice_Nascita_Luogo_Col.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Nascita_Luogo_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_NASCITA_LUOGO_COL, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Codice_Residenza_Luogo_Col, "") != "")
                    if (entity.Codice_Residenza_Luogo_Col.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Residenza_Luogo_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_RESIDENZA_LUOGO_COL, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Cognome_Col, "") != "")
                    if (entity.Cognome_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cognome_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_COGNOME_COL, PowerWebResources.VALORE_40));
                if (CommonService.Nz(entity.Domicilio_Cap_Col, "") != "")
                    if (entity.Domicilio_Cap_Col.Length > 15)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Cap_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DOMICILIO_CAP_COL, PowerWebResources.VALORE_15));
                if (CommonService.Nz(entity.Domicilio_Indirizzo_Col, "") != "")
                    if (entity.Domicilio_Indirizzo_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Indirizzo_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DOMICILIO_INDIRIZZO_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Domicilio_Interno_Col, "") != "")
                    if (entity.Domicilio_Interno_Col.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Interno_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DOMICILIO_INTERNO_COL, PowerWebResources.VALORE_10));
                if (CommonService.Nz(entity.Domicilio_Localita_Col, "") != "")
                    if (entity.Domicilio_Localita_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Localita_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DOMICILIO_LOCALITA_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Domicilio_Luogo_Col, "") != "")
                    if (entity.Domicilio_Luogo_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Luogo_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DOMICILIO_LUOGO_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Domicilio_Provincia_Col, "") != "")
                    if (entity.Domicilio_Provincia_Col.Length > 4)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Provincia_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DOMICILIO_PROVINCIA_COL, PowerWebResources.VALORE_4));
                if (CommonService.Nz(entity.Fax_1_Col, "") != "")
                    if (entity.Fax_1_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fax_1_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_FAX_1_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Fax_1_Rif_Col, "") != "")
                    if (entity.Fax_1_Rif_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fax_1_Rif_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_FAX_1_RIF_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Fax_2_Col, "") != "")
                    if (entity.Fax_2_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fax_2_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_FAX_2_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Fax_2_Rif_Col, "") != "")
                    if (entity.Fax_2_Rif_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fax_2_Rif_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_FAX_2_RIF_CAN, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Flag_INPS_Col, "") != "")
                    if (entity.Flag_INPS_Col.Length > 1)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_INPS_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_FLAG_INPS_COL, PowerWebResources.VALORE_1));
                if (CommonService.Nz(entity.Libretto_Sanitario_Col, "") != "")
                    if (entity.Libretto_Sanitario_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Libretto_Sanitario_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_LIBRETTO_SANITARIO_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Livello_Col, "") != "")
                    if (entity.Livello_Col.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Livello_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_LIVELLO_COL, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Matricola_Col, "") != "")
                    if (entity.Matricola_Col.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Matricola_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_MATRICOLA_COL, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.N_Pos_INAIL_Col, "") != "")
                    if (entity.N_Pos_INAIL_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.N_Pos_INAIL_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_N_POS_INAIL_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.N_Pos_INPS_Col, "") != "")
                    if (entity.N_Pos_INPS_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.N_Pos_INPS_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_N_POS_INPS_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Nascita_Cap_Col, "") != "")
                    if (entity.Nascita_Cap_Col.Length > 15)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nascita_Cap_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NASCITA_CAP_COL, PowerWebResources.VALORE_15));
                if (CommonService.Nz(entity.Nascita_Luogo_Col, "") != "")
                    if (entity.Nascita_Luogo_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nascita_Luogo_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NASCITA_LUOGO_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Nascita_Provincia_Col, "") != "")
                    if (entity.Nascita_Provincia_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nascita_Provincia_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NASCITA_PROVINCIA_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Nazionalita_Col, "") != "")
                    if (entity.Nazionalita_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nazionalita_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NAZIONALITA_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Nome_Col, "") != "")
                    if (entity.Nome_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NOME_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Patente_Col, "") != "")
                    if (entity.Patente_Col.Length > 2)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Patente_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_PATENTE_COL, PowerWebResources.VALORE_2));
                if (CommonService.Nz(entity.Qualifica_Col, "") != "")
                    if (entity.Qualifica_Col.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Qualifica_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_QUALIFICA_COL, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Residenza_Cap_Col, "") != "")
                    if (entity.Residenza_Cap_Col.Length > 15)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Cap_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_CAP_COL, PowerWebResources.VALORE_15));
                if (CommonService.Nz(entity.Residenza_Indirizzo_Col, "") != "")
                    if (entity.Residenza_Indirizzo_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Indirizzo_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_INDIRIZZO_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Residenza_Interno_Col, "") != "")
                    if (entity.Residenza_Interno_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Interno_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_INTERNO_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Residenza_Localita_Col, "") != "")
                    if (entity.Residenza_Localita_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Localita_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_LOCALITA_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Residenza_Luogo_Col, "") != "")
                    if (entity.Residenza_Luogo_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Luogo_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_LUOGO_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Residenza_Provincia_Col, "") != "")
                    if (entity.Residenza_Provincia_Col.Length > 4)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Provincia_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_PROVINCIA_COL, PowerWebResources.VALORE_4));
                if (CommonService.Nz(entity.Residenza_Provincia_GEN_Col, "") != "")
                    if (entity.Residenza_Provincia_GEN_Col.Length > 4)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Provincia_GEN_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_PROVINCIA_GEN_COL, PowerWebResources.VALORE_4));
                if (CommonService.Nz(entity.Sesso_Col, "") != "")
                    if (entity.Sesso_Col.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Sesso_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_SESSO_COL, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Stato_Civile_Col, "") != "")
                    if (entity.Stato_Civile_Col.Length > 2)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Stato_Civile_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_STATO_CIVILE_COL, PowerWebResources.VALORE_2));
                if (CommonService.Nz(entity.Telefono_1_Col, "") != "")
                    if (entity.Telefono_1_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_1_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_1_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Telefono_1_Rif_Col, "") != "")
                    if (entity.Telefono_1_Rif_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_1_Rif_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_1_RIF_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Telefono_2_Col, "") != "")
                    if (entity.Telefono_2_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_2_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_2_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Telefono_2_Col, "") != "")
                    if (entity.Telefono_2_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_2_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_2_RIF_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Telefono_3_Col, "") != "")
                    if (entity.Telefono_3_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_3_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_3_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Telefono_3_Rif_Col, "") != "")
                    if (entity.Telefono_3_Rif_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_3_Rif_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_3_RIF_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Telefono_4_Col, "") != "")
                    if (entity.Telefono_4_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_4_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_4_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Telefono_4_Rif_Col, "") != "")
                    if (entity.Telefono_4_Rif_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_4_Rif_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_4_RIF_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Tipo_Col, "") != "")
                    if (entity.Tipo_Col.Length > 5)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TIPO_COL, PowerWebResources.VALORE_5));
                if (CommonService.Nz(entity.Tipo_Contratto_Col, "") != "")
                    if (entity.Tipo_Contratto_Col.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Contratto_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TIPO_CONTRATTO_COL, PowerWebResources.VALORE_10));
                if (CommonService.Nz(entity.Tipo_Rapporto_Col, "") != "")
                    if (entity.Tipo_Rapporto_Col.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Rapporto_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TIPO_RAPPORTO_COL, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Titolo_Studio_Col, "") != "")
                    if (entity.Titolo_Studio_Col.Length > 2)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Titolo_Studio_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TITOLO_STUDIO_COL, PowerWebResources.VALORE_2));
                //
                // verifico, per una serie di campi, che il valore di un campo non sia in conflitto con il valore di un altro campo
                //
                if (entity.Straniero_CEE_Col == true && entity.Straniero_Col == true)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Straniero_CEE_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_STRANIERO_CEE));

                // se è valorizzato un tipo orario per il collaboratore, questo deve essere dell'entità corrispondente a collaboratore
                if (entity.Tab_Orari_Tipo_Id.HasValue)
                {
                    var tabOrariTipo = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(tot => tot.Tab_Orari_Tipo_Id == entity.Tab_Orari_Tipo_Id);
                    if (tabOrariTipo != null)
                        if (tabOrariTipo.Tab_Orari_Tipo_Entita_Rif != "Col")
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Id), BusinessService.GetLocalizedString(PowerWebResources.ERR_ENTITA_TIPO_ORARIO_NON_CORRETTA));
                }

                //
                // 5) verifico, per una serie di campi, che il valore del campo sia presente nelle relative Tabelle 
                //      
                if (CommonService.Nz(entity.Cant_Id, 0) != 0)
                    if (RepoManager.CantRepo.SingleOrDefault(u => u.Cant_Id == entity.Cant_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_CANT_ID, PowerWebResources.STR_CANTIERI));
                if (RepoManager.ParamRepo.ParametersRow.Ctrl_Tab_Comuni != 0)
                // I Controlli sui CAP/LUOGHI/CODICI LUOGHI sono FATTI SOLO se è alzato il Falg CTRL_TAB_COMUNI in PARAM
                {
                    if (CommonService.Nz(entity.Codice_Domicilio_Luogo_Col, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Cap_Tab_Comuni == entity.Codice_Domicilio_Luogo_Col) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Domicilio_Luogo_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_CODICE_DOMICILIO_LUOGO_COL, PowerWebResources.STR_COLLABORATORI));
                    if (CommonService.Nz(entity.Codice_Nascita_Luogo_Col, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Cap_Tab_Comuni == entity.Codice_Nascita_Luogo_Col) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Nascita_Luogo_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_CODICE_NASCITA_LUOGO_COL, PowerWebResources.STR_COLLABORATORI));
                    if (CommonService.Nz(entity.Codice_Residenza_Luogo_Col, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Cap_Tab_Comuni == entity.Codice_Residenza_Luogo_Col) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Residenza_Luogo_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_CODICE_RESIDENZA_LUOGO_COL, PowerWebResources.STR_COLLABORATORI));
                    if (CommonService.Nz(entity.Domicilio_Cap_Col, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Cap_Tab_Comuni == entity.Domicilio_Cap_Col) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Cap_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_DOMICILIO_CAP_COL, PowerWebResources.STR_COLLABORATORI));
                    if (CommonService.Nz(entity.Domicilio_Luogo_Col, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Luogo_Tab_Comuni == entity.Domicilio_Luogo_Col) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Luogo_Col),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_DOMICILIO_LUOGO_COL, PowerWebResources.STR_COLLABORATORI));
                }
                else
                // Altrimenti mi limito a controllarne solo la Lunghezza massima    
                {
                    if (CommonService.Nz(entity.Codice_Domicilio_Luogo_Col, "") != "")
                        if (entity.Codice_Domicilio_Luogo_Col.Length > 20)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Domicilio_Luogo_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_CODICE_DOMICILIO_LUOGO_COL, PowerWebResources.VALORE_20));
                    if (CommonService.Nz(entity.Codice_Nascita_Luogo_Col, "") != "")
                        if (entity.Codice_Nascita_Luogo_Col.Length > 20)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Nascita_Luogo_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_CODICE_NASCITA_LUOGO_COL, PowerWebResources.VALORE_20));
                    if (CommonService.Nz(entity.Codice_Residenza_Luogo_Col, "") != "")
                        if (entity.Codice_Residenza_Luogo_Col.Length > 20)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Residenza_Luogo_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_CODICE_RESIDENZA_LUOGO_COL, PowerWebResources.VALORE_20));
                    if (CommonService.Nz(entity.Domicilio_Cap_Col, "") != "")
                        if (entity.Domicilio_Cap_Col.Length > 15)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Cap_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_DOMICILIO_CAP_COL, PowerWebResources.VALORE_15));
                    if (CommonService.Nz(entity.Domicilio_Luogo_Col, "") != "")
                        if (entity.Domicilio_Luogo_Col.Length > 50)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Luogo_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_DOMICILIO_LUOGO_COL, PowerWebResources.VALORE_50));
                }
                if (CommonService.Nz(entity.Domicilio_Provincia_Col, "") != "")
                    if (Tab_Provs.SingleOrDefault(u => u.Sigla_Prov == entity.Domicilio_Provincia_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Provincia_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                            PowerWebResources.FLD_DOMICILIO_PROVINCIA_COL));
                if (CommonService.Nz(entity.Flag_INPS_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.FLAG_INPS.ToString() && x.Chiave_Tab == entity.Flag_INPS_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_INPS_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_FLAG_INPS_COL));
                if (CommonService.Nz(entity.Flag_NON_Esportare_Col, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.FLAG_NON_ESPORTARE.ToString() && x.Chiave_Tab == entity.Flag_NON_Esportare_Col.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_NON_Esportare_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_FLAG_NON_ESPORTARE_COL));
                if (CommonService.Nz(entity.Flag_Ore_Viaggi_Col, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_CALCOLO_VIAGGI_COL.ToString() && x.Chiave_Tab == entity.Flag_Ore_Viaggi_Col.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_Ore_Viaggi_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_FLAG_ORE_VIAGGI_COL));
                if (CommonService.Nz(entity.Flag_Ore_Viaggi_Col_Inizio_Fine, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_CALCOLO_VIAGGI_COL.ToString() && x.Chiave_Tab == entity.Flag_Ore_Viaggi_Col_Inizio_Fine.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_Ore_Viaggi_Col_Inizio_Fine),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_FLAG_ORE_VIAGGI_COL_INIZIO_FINE));
                if (CommonService.Nz(entity.Livello_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.LIVELLO_COL.ToString() && x.Chiave_Tab == entity.Livello_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Livello_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_LIVELLO_COL));
                if (CommonService.Nz(entity.Metodo_Arrotondamento_Col, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.METODO_ARROTONDAMENTO.ToString() && x.Chiave_Tab == entity.Metodo_Arrotondamento_Col.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Metodo_Arrotondamento_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_METODO_ARROTONDAMENTO_COL));
                //Il Metodo di Arrotondamento "9" può essere selezionato solo sui Record CANT e/o COL ma NON in Scheda parametri
                //Ma PER IL MOMENTO NON VIENE GESTITO
                if (CommonService.Nz(entity.Metodo_Arrotondamento_Col, 0) == 9)
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_NON_GESTITO_ATTUALMENTE,
                    PowerWebResources.FLD_METODO_ARROTONDAMENTO_COL);
                //Al momento NON viene gestito il Metodo di Arrotondamento previsto in Power/Access (1=Durata,3=X Orario)
                if (CommonService.Nz(entity.Metodo_Arrotondamento_Col, 0) == 1 || CommonService.Nz(entity.Metodo_Arrotondamento_Col, 0) == 3)
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_NON_GESTITO_ATTUALMENTE,
                    PowerWebResources.FLD_METODO_ARROTONDAMENTO_COL);

                if (RepoManager.ParamRepo.ParametersRow.Ctrl_Tab_Comuni != 0)
                // I Controlli sui CAP/LUOGHI/CODICI LUOGHI sono FATTI SOLO se è alzato il Falg CTRL_TAB_COMUNI in PARAM
                {
                    if (CommonService.Nz(entity.Nascita_Luogo_Col, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Luogo_Tab_Comuni == entity.Nascita_Luogo_Col) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nascita_Luogo_Col),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_NASCITA_LUOGO_COL, PowerWebResources.STR_TAB_COMUNI));
                    if (CommonService.Nz(entity.Nascita_Cap_Col, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Cap_Tab_Comuni == entity.Nascita_Cap_Col) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nascita_Cap_Col),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_NASCITA_CAP_COL, PowerWebResources.STR_TAB_COMUNI));
                }
                else
                // Altrimenti mi limito a controllarne solo la Lunghezza massima    
                {
                    if (CommonService.Nz(entity.Nascita_Luogo_Col, "") != "")
                        if (entity.Nascita_Luogo_Col.Length > 50)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nascita_Luogo_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_NASCITA_LUOGO_COL, PowerWebResources.VALORE_50));
                    if (CommonService.Nz(entity.Nascita_Cap_Col, "") != "")
                        if (entity.Nascita_Cap_Col.Length > 15)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nascita_Cap_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_NASCITA_CAP_COL, PowerWebResources.VALORE_15));
                }
                if (CommonService.Nz(entity.Nascita_Provincia_Col, "") != "")
                    if (Tab_Provs.SingleOrDefault(u => u.Sigla_Prov == entity.Nascita_Provincia_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nascita_Provincia_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                            PowerWebResources.FLD_NASCITA_PROVINCIA_COL, PowerWebResources.STR_TAB_COMUNI));

                if (CommonService.Nz(entity.Nazionalita_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.NAZIONALITA.ToString() && x.Chiave_Tab == entity.Nazionalita_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nazionalita_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_NAZIONALITA_COL));
                if (CommonService.Nz(entity.Patente_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_PATENTE.ToString() && x.Chiave_Tab == entity.Patente_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Patente_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_PATENTE_COL));
                if (CommonService.Nz(entity.Qualifica_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.QUALIFICHE_COL.ToString() && x.Chiave_Tab == entity.Qualifica_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Qualifica_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_QUALIFICA_COL));

                if (RepoManager.ParamRepo.ParametersRow.Ctrl_Tab_Comuni != 0)
                // I Controlli sui CAP/LUOGHI/CODICI LUOGHI sono FATTI SOLO se è alzato il Falg CTRL_TAB_COMUNI in PARAM
                {
                    if (CommonService.Nz(entity.Residenza_Cap_Col, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Cap_Tab_Comuni == entity.Residenza_Cap_Col) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Cap_Col),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                                PowerWebResources.FLD_RESIDENZA_CAP_COL, PowerWebResources.STR_TAB_COMUNI));
                    if (CommonService.Nz(entity.Residenza_Luogo_Col, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Luogo_Tab_Comuni == entity.Residenza_Luogo_Col) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Luogo_Col),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                                PowerWebResources.FLD_RESIDENZA_LUOGO_COL, PowerWebResources.STR_TAB_COMUNI));
                }
                else
                // Altrimenti mi limito a controllarne solo la Lunghezza massima    
                {
                    if (CommonService.Nz(entity.Residenza_Cap_Col, "") != "")
                        if (entity.Residenza_Cap_Col.Length > 15)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Cap_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_RESIDENZA_CAP_COL, PowerWebResources.VALORE_15));
                    if (CommonService.Nz(entity.Residenza_Luogo_Col, "") != "")
                        if (entity.Residenza_Luogo_Col.Length > 50)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Luogo_Col),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_RESIDENZA_LUOGO_COL, PowerWebResources.VALORE_50));
                }
                if (CommonService.Nz(entity.Residenza_Provincia_Col, "") != "")
                    if (Tab_Provs.SingleOrDefault(u => u.Sigla_Prov == entity.Residenza_Provincia_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Provincia_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                            PowerWebResources.FLD_RESIDENZA_PROVINCIA_COL));
                if (CommonService.Nz(entity.Residenza_Provincia_GEN_Col, "") != "")
                    if (Tab_Provs.SingleOrDefault(u => u.Sigla_Prov == entity.Residenza_Provincia_GEN_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Provincia_GEN_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                            PowerWebResources.FLD_RESIDENZA_PROVINCIA_GEN_COL));
                if (CommonService.Nz(entity.Resp_Id, 0) != 0)
                    if (RepoManager.RespRepo.SingleOrDefault(u => u.Resp_Id == entity.Resp_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Resp_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_RESP_ID, PowerWebResources.STR_RESP));
                if (CommonService.Nz(entity.Sesso_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.SESSO.ToString() && x.Chiave_Tab == entity.Sesso_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Sesso_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_SESSO_COL));
                if (CommonService.Nz(entity.Stato_Civile_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.STATO_CIVILE.ToString() && x.Chiave_Tab == entity.Stato_Civile_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Stato_Civile_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_STATO_CIVILE_COL));
                if (CommonService.Nz(entity.Tipo_Arrotondamento_Col, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_ARROTONDAMENTO.ToString() && x.Chiave_Tab == entity.Tipo_Arrotondamento_Col.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Arrotondamento_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPO_ARROTONDAMENTO_COL));
                if (CommonService.Nz(entity.Tipo_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_COL.ToString() && x.Chiave_Tab == entity.Tipo_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPO_COL));
                if (CommonService.Nz(entity.Tipo_Contratto_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_CONTRATTO_COL.ToString() && x.Chiave_Tab == entity.Tipo_Contratto_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Contratto_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPO_CONTRATTO_COL));
                if (CommonService.Nz(entity.Tipo_Rapporto_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_RAPPORTO_COL.ToString() && x.Chiave_Tab == entity.Tipo_Rapporto_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Rapporto_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPO_RAPPORTO_COL));
                if (CommonService.Nz(entity.Titolo_Studio_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TITOLO_STUDIO.ToString() && x.Chiave_Tab == entity.Titolo_Studio_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Titolo_Studio_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TITOLO_STUDIO_COL));
                if (CommonService.Nz(entity.TipoNotturno_Col, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                  && x.Nome_Tab == TabDecodNameEnum.TIPO_NOTTURNO.ToString() && x.Chiave_Tab == entity.TipoNotturno_Col.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.TipoNotturno_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPONOTTURNO_COL));
                if (CommonService.Nz(entity.Zona_Col, "") != "")
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.ZONE.ToString() && x.Chiave_Tab == entity.Zona_Col) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Zona_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_ZONA_COL));
                //if (CommonService.Nz(entity.Tab_Orari_Tipo_Id, 0) != 0)
                //    if (RepoManager.Tab_Orari_TipoRepo.SingleOrDefault(tot => tot.Tab_Orari_Tipo_Id = entity.Tab_Orari_Tipo_Id) == null)
                //        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Id),
                //          CommonServiceBiz.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                //          PowerWebResources.FLD_TIPO_ORARIO_COL, PowerWebResources.STR_CANTIERI));

                //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);
            }
            catch (Exception ex)
            {
                var CodErr = "Col_Id= " + entity.Col_Id;
                throw ex;
            }
            return result;
        }
        public override Dictionary<string, string> CheckForImport(Col entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //verifico SOLO x IMPORT la validità delle eventuali Date Ricevute
            if (entity.Data_Registrazione_Col < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_COL));
            if (entity.DataOraUltimaModifica_Col < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_COL));
            if (CommonService.Nz(entity.Assegni_Famigliari_Inizio_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Assegni_Famigliari_Inizio_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_ASSEGNI_FAMIGLIARI_INIZIO_COL));
            if (CommonService.Nz(entity.Data_Proroga_Contratto, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Proroga_Contratto),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_PROROGA_CONTRATTO));

            if (CommonService.Nz(entity.Assegni_Famigliari_Inizio_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Assegni_Famigliari_Inizio_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_ASSEGNI_FAMIGLIARI_FINE_COL));
            if (CommonService.Nz(entity.Data_Disponibilita_Inizio_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Disponibilita_Inizio_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_DISPONIBILITA_INIZIO_COL));
            if (CommonService.Nz(entity.Data_Disponibilita_Fine_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Disponibilita_Fine_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_DISPONIBILITA_FINE_COL));
            if (CommonService.Nz(entity.Data_Sorv_San_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Sorv_San_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_SORV_SAN_COL));
            if (CommonService.Nz(entity.Scadenza_Patente_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Scadenza_Patente_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_SCADENZA_PATENTE_COL));
            if (CommonService.Nz(entity.Straniero_Scadenza_Permesso_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Straniero_Scadenza_Permesso_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_STRANIERO_SCADENZA_PERMESSO_COL));

            if (entity.Flag_Ore_Viaggi_Col != Int32.MinValue)
                if (entity.Flag_Ore_Viaggi_Col == 5)
                    entity.Flag_Ore_Viaggi_Col = 1;

            //AGGIUNGO GLI EVENTUALI VALORI PRESENTI NEI CAMPI DELLA TAB COL CHE RICEVO e CHE NON SONO GIA' PRESENTI IN TAB_DECOD
            // PER UN EVENTUALE DISALLINEAMENTO FRA I DATI DEI RECORD DELLA TABELLA COL e LE TAB_DECOD di ACCESS
            // LO FACCIO PER LE TABELLE:
            //                      FLAG_INPS_COL
            //                      LIVELLO_COL
            //                      NAZIONALITA_COL
            //                      QUALIFICA
            //                      STATO_CIVILE_COL
            //                      TIPO_COL
            //                      TIPO_CONTRATTO_COL
            //                      TIPO_RAPPORTO_COL
            //                      TITOLO_STUDIO_COL

            if (CommonService.Nz(entity.Tipo_Col, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.TIPO_COL.ToString() && x.Chiave_Tab == entity.Tipo_Col) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.TIPO_COL.ToString(),
                        Chiave_Tab = entity.Tipo_Col,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            if (CommonService.Nz(entity.Flag_INPS_Col, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.FLAG_INPS.ToString() && x.Chiave_Tab == entity.Flag_INPS_Col) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.FLAG_INPS.ToString(),
                        Chiave_Tab = entity.Flag_INPS_Col,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            if (CommonService.Nz(entity.Livello_Col, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.LIVELLO_COL.ToString() && x.Chiave_Tab == entity.Livello_Col) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.LIVELLO_COL.ToString(),
                        Chiave_Tab = entity.Livello_Col,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            if (CommonService.Nz(entity.Nazionalita_Col, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.NAZIONALITA.ToString() && x.Chiave_Tab == entity.Nazionalita_Col) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.NAZIONALITA.ToString(),
                        Chiave_Tab = entity.Nazionalita_Col,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            if (CommonService.Nz(entity.Qualifica_Col, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.QUALIFICHE_COL.ToString() && x.Chiave_Tab == entity.Qualifica_Col) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.QUALIFICHE_COL.ToString(),
                        Chiave_Tab = entity.Qualifica_Col,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            if (CommonService.Nz(entity.Stato_Civile_Col, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.STATO_CIVILE.ToString() && x.Chiave_Tab == entity.Stato_Civile_Col) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.STATO_CIVILE.ToString(),
                        Chiave_Tab = entity.Stato_Civile_Col,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            if (CommonService.Nz(entity.Tipo_Rapporto_Col, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.TIPO_RAPPORTO_COL.ToString() && x.Chiave_Tab == entity.Tipo_Rapporto_Col) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.TIPO_RAPPORTO_COL.ToString(),
                        Chiave_Tab = entity.Tipo_Rapporto_Col,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            if (CommonService.Nz(entity.Tipo_Contratto_Col, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.TIPO_CONTRATTO_COL.ToString() && x.Chiave_Tab == entity.Tipo_Contratto_Col) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.TIPO_CONTRATTO_COL.ToString(),
                        Chiave_Tab = entity.Tipo_Contratto_Col,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            if (CommonService.Nz(entity.Titolo_Studio_Col, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.TITOLO_STUDIO.ToString() && x.Chiave_Tab == entity.Titolo_Studio_Col) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.TITOLO_STUDIO.ToString(),
                        Chiave_Tab = entity.Titolo_Studio_Col,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ColRepo", null);
                }
            }
            WriteCheckLog(entity, result, Log);
            return result;
        }
        public override List<Dictionary<string, string>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Col");
            ResetSession();
            List<PowerMDBDataSet.ColRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData(oDataSet.Col.ToList(), "Col", "Codice_Collaboratore").OrderBy(acd => acd.Codice_Collaboratore).ToList();
            List<Col> toImport = new List<Col>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>();
            string lastKey = "";
            if (accessData != null)
            {
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                foreach (PowerMDBDataSet.ColRow oRow in accessData)
                {
                    try
                    {
                        lastKey = oRow.Codice_Collaboratore;
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                           BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "COL", "8",
                           "17", oRow.Codice_Collaboratore.ToString(), countRec.ToString(), nRec.ToString()));
                        Col oNewRecord = Init();
                        //TODO [Tab_Orari_Tipo_Id]
                        oNewRecord.Codice_Collaboratore = oRow.Codice_Collaboratore;
                        oNewRecord.Matricola_Col = oRow.IsMatricola_ColNull() ? null : oRow.Matricola_Col;
                        oNewRecord.Cognome_Col = oRow.IsCognome_ColNull() ? null : oRow.Cognome_Col;
                        oNewRecord.Nome_Col = oRow.IsNome_ColNull() ? null : oRow.Nome_Col;

                        if (!oRow.IsCodice_Resp_ColNull())
                        {
                            //Resp oRecord = Resps.SingleOrDefault(x => x.Codice_Resp == oRow.Filiale_Col);
                            Resp oRecord = RepoManager.RespRepo.SingleOrDefault(x => x.Codice_Resp == oRow.Codice_Resp_Col);
                            if (oRecord != null)
                                oNewRecord.Resp_Id = oRecord.Resp_Id;
                             
                        }
                        if (!oRow.IsCodice_Cantiere_ColNull())
                        {
                            Cant oRecord = Cants.SingleOrDefault(x => x.Codice_Cantiere == oRow.Codice_Cantiere_Col);
                            if (oRecord != null)
                                oNewRecord.Cant_Id = oRecord.Cant_Id;
                        }
                        //TODO Ste - gestione Tipo_Arrotondamento_Col???
                        oNewRecord.Data_Registrazione_Col = oRow.IsData_Registrazione_ColNull() ? new DateTime(2000, 1, 1) : oRow.Data_Registrazione_Col;
                        oNewRecord.DataOraUltimaModifica_Col = oRow.IsDataOraUtimaModificaNull() ? new DateTime(2000, 1, 1) : oRow.DataOraUtimaModifica;
                        oNewRecord.DisAbilitazione_Col = oRow.IsDisAbilitazione_ColNull() ? false : oRow.DisAbilitazione_Col;
                        oNewRecord.Data_Disponibilita_Inizio_Col = oRow.IsData_Disponibilità_Inizio_ColNull() ? (DateTime?)null : oRow.Data_Disponibilità_Inizio_Col;
                        oNewRecord.Data_Disponibilita_Fine_Col = oRow.IsData_Disponibilità_Fine_ColNull() ? (DateTime?)null : oRow.Data_Disponibilità_Fine_Col;
                        oNewRecord.Nascita_Data_Col = oRow.IsNascita_Data_ColNull() ? (DateTime?)null : oRow.Nascita_Data_Col;
                        oNewRecord.Nascita_Luogo_Col = oRow.IsNascita_Luogo_ColNull() ? null : oRow.Nascita_Luogo_Col;
                        oNewRecord.Nazionalita_Col = oRow.IsNazionalita_ColNull() ? null : oRow.Nazionalita_Col;
                        oNewRecord.Straniero_Col = oRow.IsStraniero_ColNull() ? false : oRow.Straniero_Col;
                        oNewRecord.Straniero_CEE_Col = oRow.IsStraniero_CEE_ColNull() ? false : oRow.Straniero_CEE_Col;
                        oNewRecord.Straniero_Scadenza_Permesso_Col = oRow.IsStraniero_Scadenza_Permesso_ColNull() ? (DateTime?)null : oRow.Straniero_Scadenza_Permesso_Col;
                        oNewRecord.Residenza_Luogo_Col = oRow.IsResidenza_Luogo_ColNull() ? null : oRow.Residenza_Luogo_Col;
                        oNewRecord.Residenza_Indirizzo_Col = oRow.IsResidenza_Indirizzo_ColNull() ? null : oRow.Residenza_Indirizzo_Col;
                        oNewRecord.Residenza_Provincia_Col = oRow.IsResidenza_Provincia_ColNull() ? null : oRow.Residenza_Provincia_Col;
                        oNewRecord.Residenza_Cap_Col = oRow.IsResidenza_Cap_ColNull() ? null : oRow.Residenza_Cap_Col;
                        oNewRecord.Domicilio_Luogo_Col = oRow.IsDomicilio_Luogo_ColNull() ? null : oRow.Domicilio_Luogo_Col;
                        oNewRecord.Domicilio_Indirizzo_Col = oRow.IsDomicilio_Indirizzo_ColNull() ? null : oRow.Domicilio_Indirizzo_Col;
                        oNewRecord.Domicilio_Provincia_Col = oRow.IsDomicilio_Provincia_ColNull() ? null : oRow.Domicilio_Provincia_Col;
                        oNewRecord.Domicilio_Cap_Col = oRow.IsDomicilio_Cap_ColNull() ? null : oRow.Domicilio_Cap_Col;
                        oNewRecord.Telefono_1_Col = oRow.IsTelefono_1_ColNull() ? null : oRow.Telefono_1_Col;
                        oNewRecord.Telefono_1_Rif_Col = oRow.IsTelefono_1_Rif_ColNull() ? null : oRow.Telefono_1_Rif_Col;
                        oNewRecord.Telefono_2_Col = oRow.IsTelefono_2_ColNull() ? null : oRow.Telefono_2_Col;
                        oNewRecord.Telefono_2_Rif_Col = oRow.IsTelefono_2_Rif_ColNull() ? null : oRow.Telefono_2_Rif_Col;
                        oNewRecord.Telefono_3_Col = oRow.IsTelefono_3_ColNull() ? null : oRow.Telefono_3_Col;
                        oNewRecord.Telefono_3_Rif_Col = oRow.IsTelefono_3_Rif_ColNull() ? null : oRow.Telefono_3_Rif_Col;
                        oNewRecord.Telefono_4_Col = oRow.IsTelefono_4_ColNull() ? null : oRow.Telefono_4_Col;
                        oNewRecord.Telefono_4_Rif_Col = oRow.IsTelefono_4_Rif_ColNull() ? null : oRow.Telefono_4_Rif_Col;
                        oNewRecord.Fax_1_Col = oRow.IsFax_1_ColNull() ? null : oRow.Fax_1_Col;
                        oNewRecord.Fax_1_Rif_Col = oRow.IsFax_1_Rif_ColNull() ? null : oRow.Fax_1_Rif_Col;
                        oNewRecord.Fax_2_Col = oRow.IsFax_2_ColNull() ? null : oRow.Fax_2_Col;
                        oNewRecord.Fax_2_Rif_Col = oRow.IsFax_2_Rif_ColNull() ? null : oRow.Fax_2_Rif_Col;
                        oNewRecord.Qualifica_Col = oRow.IsQualifica_ColNull() ? null : oRow.Qualifica_Col;
                        oNewRecord.Tipo_Col = oRow.IsTipo_ColNull() ? null : oRow.Tipo_Col;
                        oNewRecord.Tipo_Rapporto_Col = oRow.IsTipo_Rapporto_ColNull() ? null : oRow.Tipo_Rapporto_Col;
                        oNewRecord.Livello_Col = oRow.IsLivello_ColNull() ? null : oRow.Livello_Col;
                        oNewRecord.Retribuzione_Oraria_Col = oRow.IsRetribuzione_Oraria_ColNull() ? (double?)null : oRow.Retribuzione_Oraria_Col;
                        oNewRecord.Indennita_Sanificazione_Col = oRow.IsIndennità_Sanificazione_ColNull() ? (double?)null : oRow.Indennità_Sanificazione_Col;
                        oNewRecord.Indennita_Trasporto_Col = oRow.IsIndennità_Trasporto_ColNull() ? (double?)null : oRow.Indennità_Trasporto_Col;
                        oNewRecord.Assegni_Famigliari_Col = oRow.IsAssegni_Famigliari_ColNull() ? false : oRow.Assegni_Famigliari_Col;
                        oNewRecord.Assegni_Famigliari_Inizio_Col = oRow.IsAssegni_Famigliari_Inizio_ColNull() ? (DateTime?)null : (DateTime)oRow.Assegni_Famigliari_Inizio_Col;
                        oNewRecord.Assegni_Famigliari_Fine_Col = oRow.IsAssegni_Famigliari_Fine_ColNull() ? (DateTime?)null : (DateTime)oRow.Assegni_Famigliari_Fine_Col;
                        oNewRecord.Stato_Civile_Col = oRow.IsStato_Civile_ColNull() ? null : oRow.Stato_Civile_Col;
                        oNewRecord.Sesso_Col = (oRow.IsSesso_ColNull() ? null
                          : (oRow.Sesso_Col == "M" ? PowerWebResources.TD_SESSO_MASCHIO.ToString()
                          : (oRow.Sesso_Col == "F" ? PowerWebResources.TD_SESSO_FEMMINA.ToString()
                          : null)));
                        oNewRecord.Titolo_Studio_Col = oRow.IsTitolo_Studio_ColNull() ? null : oRow.Titolo_Studio_Col;
                        oNewRecord.Automunito_Col = oRow.IsAutomunito_ColNull() ? false : oRow.Automunito_Col;
                        oNewRecord.Patente_Col = oRow.IsPatente_ColNull() ? null : oRow.Patente_Col;
                        oNewRecord.Libretto_Sanitario_Col = oRow.IsLibretto_Sanitario_ColNull() ? null : oRow.Libretto_Sanitario_Col;
                        oNewRecord.Codice_Fiscale_Col = oRow.IsCodice_Fiscale_ColNull() ? null : oRow.Codice_Fiscale_Col;
                        oNewRecord.Note_Col = oRow.IsNote_ColNull() ? null : oRow.Note_Col;
                        oNewRecord.Durata_Min_Ril_Col = oRow.IsDurata_Min_Ril_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Min_Ril_Col.TimeOfDay.Ticks);
                        oNewRecord.Durata_Max_Ril_Col = oRow.IsDurata_Max_Ril_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Ril_Col.TimeOfDay.Ticks);
                        oNewRecord.Durata_Max_Gruppo_Ril_Col = oRow.IsDurata_Max_Gruppo_Ril_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Gruppo_Ril_Col.TimeOfDay.Ticks);
                        oNewRecord.Durata_Max_Gruppo_Notte_Ril_Col = oRow.IsDurata_Max_Gruppo_Notte_Ril_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Gruppo_Notte_Ril_Col.TimeOfDay.Ticks);
                        oNewRecord.Flag_NON_Esportare_Col = oRow.IsFlag_NON_Esportare_ColNull() ? (byte?)null : (byte)oRow.Flag_NON_Esportare_Col;
                        oNewRecord.Durata_Pausa_Col = oRow.IsDurata_Pausa_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Pausa_Col.TimeOfDay.Ticks);
                        oNewRecord.Flag_Ore_Viaggi_Col = oRow.IsFlag_Ore_Viaggi_ColNull() ? (byte?)null : (byte)oRow.Flag_Ore_Viaggi_Col;
                        oNewRecord.Fascia_Ore_Viaggi_1_Inizio_Col = oRow.IsFascia_Ore_Viaggi_1_Inizio_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_1_Inizio_COL.TimeOfDay.Ticks);
                        oNewRecord.Fascia_Ore_Viaggi_1_Fine_Col = oRow.IsFascia_Ore_Viaggi_1_Fine_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_1_Fine_COL.TimeOfDay.Ticks);
                        oNewRecord.Fascia_Ore_Viaggi_2_Inizio_Col = oRow.IsFascia_Ore_Viaggi_2_Inizio_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_2_Inizio_COL.TimeOfDay.Ticks);
                        oNewRecord.Fascia_Ore_Viaggi_2_Fine_Col = oRow.IsFascia_Ore_Viaggi_2_Fine_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_2_Fine_COL.TimeOfDay.Ticks);
                        oNewRecord.Fascia_Ore_Viaggi_3_Inizio_Col = oRow.IsFascia_Ore_Viaggi_3_Inizio_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_3_Inizio_COL.TimeOfDay.Ticks);
                        oNewRecord.Fascia_Ore_Viaggi_3_Fine_Col = oRow.IsFascia_Ore_Viaggi_3_Fine_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_3_Fine_COL.TimeOfDay.Ticks);
                        oNewRecord.Fascia_Ore_Viaggi_4_Inizio_Col = oRow.IsFascia_Ore_Viaggi_4_Inizio_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_4_Inizio_COL.TimeOfDay.Ticks);
                        oNewRecord.Fascia_Ore_Viaggi_4_Fine_Col = oRow.IsFascia_Ore_Viaggi_4_Fine_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_4_Fine_COL.TimeOfDay.Ticks);
                        oNewRecord.Fascia_Ore_Viaggi_5_Inizio_Col = oRow.IsFascia_Ore_Viaggi_5_Inizio_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_5_Inizio_COL.TimeOfDay.Ticks);
                        oNewRecord.Fascia_Ore_Viaggi_5_Fine_Col = oRow.IsFascia_Ore_Viaggi_5_Fine_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_5_Fine_COL.TimeOfDay.Ticks);
                        oNewRecord.Soglia_Arrot_Durata_Fig_Col = oRow.IsSoglia_Arrot_Durata_Fig_ColNull() ? (short?)null : (short)oRow.Soglia_Arrot_Durata_Fig_Col;
                        oNewRecord.Minuti_Arrot_Durata_Fig_Col = oRow.IsMinuti_Arrot_Durata_Fig_ColNull() ? (short?)null : (short)oRow.Minuti_Arrot_Durata_Fig_Col;
                        oNewRecord.TipoNotturno_Col = oRow.IsTipoNotturno_ColNull() ? 0 : Int32.Parse(oRow.TipoNotturno_Col);
                        oRow.TipoNotturno_Col = oRow.IsTipoNotturno_ColNull() ? "0" : oRow.TipoNotturno_Col;
                        if (!oRow.IsFlagCambioGiornoRil_ColNull() && oRow.FlagCambioGiornoRil_Col == true)
                        {
                            if (!oRow.IsFlagCambioGiornoRil_ColNull() && oRow.TipoNotturno_Col == "1")
                                oNewRecord.TipoNotturno_Col = 2;
                            else
                                oNewRecord.TipoNotturno_Col = 1;
                        }
                        else
                            oNewRecord.TipoNotturno_Col = 0;
                        oNewRecord.Tipo_Contratto_Col = oRow.IsTipoContrattoNull() ? null : oRow.TipoContratto;
                        oNewRecord.Prova = oRow.IsProvaNull() ? (byte?)null : (byte)oRow.Prova;
                        oNewRecord.OreMassime = oRow.IsOreMassimeNull() ? (short?)null : (short)oRow.OreMassime;
                        oNewRecord.Singola_Reg = oRow.IsSingola_RegNull() ? false : oRow.Singola_Reg;
                        oNewRecord.Limite_Inizio_Notte_Col = oRow.IsLimite_Inizio_Notte_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Limite_Inizio_Notte_Col.TimeOfDay.Ticks);
                        oNewRecord.OreMaxGG_Col = oRow.IsOreMaxGG_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.OreMaxGG_Col.TimeOfDay.Ticks);
                        oNewRecord.SogliaI_Col = oRow.IsSogliaI_ColNull() ? (short?)null : (short)oRow.SogliaI_Col;
                        oNewRecord.SogliaF_Col = oRow.IsSogliaF_ColNull() ? (short?)null : (short)oRow.SogliaF_Col;
                        oNewRecord.ArrotI_Col = oRow.IsArrotI_ColNull() ? (short?)null : (short)oRow.ArrotI_Col;
                        oNewRecord.ArrotF_Col = oRow.IsArrotF_ColNull() ? (short?)null : (short)oRow.ArrotF_Col;
                        oNewRecord.Arrot_Durata_Col = oRow.IsArrot_Durata_ColNull() ? (short?)null : (short)oRow.Arrot_Durata_Col;
                        oNewRecord.Soglia_Durata_Col = oRow.IsSoglia_Durata_ColNull() ? (short?)null : (short)oRow.Soglia_Durata_Col;
                        oNewRecord.GGConsMax_Col = oRow.IsGGConsMax_ColNull() ? (byte?)null : (byte)oRow.GGConsMax_Col;
                        oNewRecord.Metodo_Arrotondamento_Col = oRow.IsMetodo_Arrotondamento_ColNull() ? 0 : oRow.Metodo_Arrotondamento_Col;
                        oNewRecord.Nascita_Provincia_Col = oRow.IsNascita_Provincia_ColNull() ? null : oRow.Nascita_Provincia_Col;
                        oNewRecord.Residenza_Localita_Col = oRow.IsResidenza_Localita_ColNull() ? null : oRow.Residenza_Localita_Col;
                        oNewRecord.Residenza_Interno_Col = oRow.IsResidenza_Interno_ColNull() ? null : oRow.Residenza_Interno_Col;
                        oNewRecord.Codice_Nascita_Luogo_Col = oRow.IsCodice_Nascita_Luogo_ColNull() ? null : oRow.Codice_Nascita_Luogo_Col;
                        oNewRecord.Codice_Residenza_Luogo_Col = oRow.IsCodice_Residenza_Luogo_ColNull() ? null : oRow.Codice_Residenza_Luogo_Col;
                        oNewRecord.Domicilio_Interno_Col = oRow.IsDomicilio_Interno_ColNull() ? null : oRow.Domicilio_Interno_Col;
                        oNewRecord.Domicilio_Localita_Col = oRow.IsDomicilio_Localita_ColNull() ? null : oRow.Domicilio_Localita_Col;
                        oNewRecord.Codice_Domicilio_Luogo_Col = oRow.IsCodice_Domicilio_Luogo_ColNull() ? null : oRow.Codice_Domicilio_Luogo_Col;
                        oNewRecord.Nascita_Cap_Col = oRow.IsNascita_Cap_ColNull() ? null : oRow.Nascita_Cap_Col;
                        oNewRecord.Disabile_Col = oRow.IsDisabile_ColNull() ? false : oRow.Disabile_Col;
                        oNewRecord.Retribuzione_Straordinaria_Col = oRow.IsRetribuzione_Straordinaria_ColNull() ? (double?)null : oRow.Retribuzione_Straordinaria_Col;
                        oNewRecord.Scadenza_Patente_Col = oRow.IsScadenza_Patente_ColNull() ? (DateTime?)null : (DateTime)oRow.Scadenza_Patente_Col;
                        oNewRecord.N_Pos_INPS_Col = oRow.IsN_Pos_INPS_ColNull() ? null : oRow.N_Pos_INPS_Col;
                        oNewRecord.N_Pos_INAIL_Col = oRow.IsN_Pos_INPS_ColNull() ? null : oRow.N_Pos_INPS_Col;
                        oNewRecord.Retribuzione_Lorda_Col = oRow.IsRetribuzione_Lorda_ColNull() ? (double?)null : oRow.Retribuzione_Lorda_Col;
                        oNewRecord.Retribuzione_Netta_Col = oRow.IsRetribuzione_Netta_ColNull() ? (double?)null : oRow.Retribuzione_Netta_Col;
                        oNewRecord.Trattenuta_Vitto_Col = oRow.IsTrattenuta_Vitto_ColNull() ? (double?)null : oRow.Trattenuta_Vitto_Col;
                        oNewRecord.Codice_Iban_Col = oRow.IsCodice_Iban_ColNull() ? null : oRow.Codice_Iban_Col;
                        oNewRecord.Quota_PTime_Col = oRow.IsQuota_PTime_ColNull() ? (float?)null : (float)oRow.Quota_PTime_Col;
                        oNewRecord.N_Persone_A_Carico_Col = oRow.IsN_Persone_A_Carico_ColNull() ? (double?)null : oRow.N_Persone_A_Carico_Col;
                        oNewRecord.Residenza_Provincia_GEN_Col = oRow.IsResidenza_Provincia_GEN_ColNull() ? null : oRow.Residenza_Provincia_GEN_Col;
                        oNewRecord.Flag_INPS_Col = oRow.IsFlag_INPS_ColNull() ? null : oRow.Flag_INPS_Col;
                        oNewRecord.Data_Sorv_San_Col = oRow.IsData_Sorv_San_ColNull() ? (DateTime?)null : oRow.Data_Sorv_San_Col;
                        oNewRecord.Limite_Entrata_Mattina_Col = oRow.IsLimite_Entrata_ColNull() ? (TimeSpan?)null : oRow.Limite_Entrata_Col.TimeOfDay;
                        //TODO Ste - da gestire
                        oNewRecord.Raggruppamento1_Col = "";
                        oNewRecord.Raggruppamento2_Col = "";

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
                                Nome_Tabella_Tab_Check_Imp = "Col",
                                Chiave_Record_Tab_Check_Imp = oRow.Codice_Collaboratore,
                            });
                            log.Warn("Codice Collaboratore cui si rifericono gli errori precedenti: " + oRow.Codice_Collaboratore + " -------------------------------------------------------------------------------");
                            errorsList.Add(currentDictionary);
                            errorsList.Add(importDictionary);
                        }
                    }
                    catch (Exception ex)
                    {
                        var RRNERR = oRow.Codice_Collaboratore;
                        throw ex;
                    }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                try
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "COL", lastKey));
                    RepoManager.ColRepo.Add(toImport);
                    RepoManager.Tab_Chk_ImpRepo.Add(errors);
                    if (errors.Count == 0)
                        RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Col",
                            Stato_Record_Tab_Check_Imp = true,
                        });
                    RepoManager.Tab_Chk_ImpRepo.SaveChanges();
                    RepoManager.Tab_Chk_ImpRepo.CommitWork();
                }
                catch (Exception ex)
                {
                    RepoManager.Tab_Chk_ImpRepo.RollbackWork();
                    throw ex;
                }
            }
            return errorsList;
        }

        /// <summary>
        /// Carica nella Tabella PendingElab La Data della Minima e la Massima Registrazione (che siamo maggiori della data Blocco)
        ///di quel Collaboratore che ha cambiato la Singola_REG
        /// </summary>
        /// <param name="entity">The entity.</param>
        public void AddPendingElabForActivity(Col entity)
        {
            var oldCol = Single(col => col.Col_Id == entity.Col_Id);

            var blockDate = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg;
            //Lettura di Tutte le REG di Quel Cantiere con data > della DATA BLOCCO
            var allColRegs = RepoManager.Reg_VRepo.Find(reg => reg.Data_Reg > blockDate && reg.Col_Id == oldCol.Col_Id, true).ToList();
            if (allColRegs.Count > 0)
            {
                var from = allColRegs.Min(regv => regv.Data_Reg);
                var to = allColRegs.Max(regv => regv.Data_Reg);
                to = to.Value.AddDays(1);
                //Scrive nella tabella PendingElab le Date per le quali devono poi essere rielaborate le Registrazioni 
                //a fronte del fatto che quel Cantiere ha Cambiato la Tipologia da/a ATTIVITA'
                RepoManager.PendingElabRepo.Add(new PendingElab { FromDate_PendingElab = from, ToDate_PendingElab = to }, true);
            }
        }
        public override Expression<Func<Col, bool>> Filter
        {
            get
            {
                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Resp) == DomainFilterEnum.Resp && PowerWebContext.Current.Resps != null && PowerWebContext.Current.User.Liv_Utente < 10)
                {
                    var allRespIds = PowerWebContext.Current.Resps.Select(resp => resp.Resp_Id).ToList();
                    return col => allRespIds.Contains( col.Resp_Id.Value);
                }
                else return base.Filter;
            }
        }

        public void UpdateMothlyHour(DateTime? oldBreakRegDate, DateTime newBreakRegDate)
        {
            if (RepoManager.ParamRepo.ParametersRow.MonthlyHoursEnum != MothlyHoursEnum.None)
            {
                var colIds = new List<int>();
                if (RepoManager.ParamRepo.ParametersRow.MonthlyHoursEnum == MothlyHoursEnum.Inclusive)
                    colIds = Find(col => col.Flag_Monte_Ore && col.Tab_Orari_Tipo_Id != null).Select(col => col.Col_Id).ToList();
                else
                    colIds = Find(col => !col.Flag_Monte_Ore && col.Tab_Orari_Tipo_Id != null).Select(col => col.Col_Id).ToList();

                DateTime? startDate = null;
                DateTime endDate = newBreakRegDate;

                if (oldBreakRegDate.HasValue)
                {
                    startDate = oldBreakRegDate.Value;

                    if (newBreakRegDate < oldBreakRegDate.Value)
                    {
                        startDate = newBreakRegDate;
                        endDate = oldBreakRegDate.Value;
                    }

                }

                var regVs = new List<Reg_V>();
                if (startDate == null)
                    regVs = RepoManager.Reg_VRepo.Find(reg => reg.Col_Id != null && reg.RegU != null && reg.Data_Ora_Fig_E.Value.Date <= newBreakRegDate.Date && colIds.Contains(reg.Col_Id.Value)).ToList();
                else
                    regVs = RepoManager.Reg_VRepo.Find(reg => reg.Col_Id != null && reg.RegU != null && reg.Data_Ora_Fig_E.Value.Date <= newBreakRegDate.Date && reg.Data_Ora_Fig_E.Value.Date > oldBreakRegDate.Value.Date && colIds.Contains(reg.Col_Id.Value)).ToList();

                var cols = Find(col => colIds.Contains(col.Col_Id), false);

                foreach (var regsByCol in regVs.GroupBy(reg => reg.Col_Id))
                {
                    if (regsByCol.Count() > 0)
                    {
                        Col currentCol = cols.Single(col => col.Col_Id == regsByCol.First().Col_Id);

                        var currentColRegs = regsByCol.Where(reg => reg.Data_Ora_Fig_E.Value.Date >= currentCol.Tab_Orari_Tipo.FirstTabOrariTipo.Value.Date).OrderBy(reg => reg.Data_Ora_Fig_E);

                        long currentColDuration = currentColRegs.Sum(reg => reg.Durata_Fig.Value);
                    }
                }

            }
        }

        /// <summary>
        /// Aggiorna, dopo averla ricalcolata, la geolocalizzazione dell'entità passata come parametro.
        /// </summary>
        /// <param name="col">Il collaboratore di cui aggiornare la geolocalizzazione.</param>
        /// <returns>
        /// La geolocalizzazione dell'entità processata.
        /// </returns>
        public void UpdateGeoLocation(Col col)
        {
            col.LatitudineGps_Col = col.LongitudineGps_Col = 0;
            try
            {
                Location point = BusinessService.GetGeocode(col.GeocodeAddress);

                if (point != null)
                {
                    col.LatitudineGps_Col = point.Point.GetCoordinate().Latitude;
                    col.LongitudineGps_Col = point.Point.GetCoordinate().Longitude;
                    Update(col, true);
                }


            }
            catch (Exception ex)
            {
                _log.Error(String.Format("Errore durante l'assegnamento delle coordinate al collaboratore {0} con exception {1}", col.Codice_Collaboratore, ex.Message));
            }


        }

        public Dictionary<string, string> ImportFromCSV(string[] inputCols)
        {
            Dictionary<string, string> errors = new Dictionary<string, string>();

            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ColImportTypeEnum);
            
            if (customizationVersion == (int)ColImportTypeEnum.Generale)
            {
                #region Import anagrafiche collaboratori generale



                Dictionary<string, Int32> columnsNumber = null;
                List<Col> colsToAdd = new List<Col>();

                // inizializzazione dei valori utilizzati per la visualizzazione dei messaggi a video (barra di avanzamento) 
                double nRec = inputCols.Count();
                double countRec = 0;
                double percRec = 0;
                var errorFileds = new StringBuilder();
                int rowNumber = 0;

                //Loop di trattamento dei Record presenti nel file di input
                foreach (String stringVar in inputCols)
                {

                    // Elimina eventuali spazi iniziali e/o finali
                    if (stringVar.Trim() != String.Empty && stringVar != ";;;;;;")
                    {
                        // aggiornamento dei dati di percentuale e numero record elaborati
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;

                        // inserimento del messaggio di stato dell'elaborazione
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                            BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORT_RECORD_X_DI_Y.ToString(), countRec.ToString(), nRec.ToString()));

                        //divido la stringa in ogni valore
                        String[] splittedLine = stringVar.Split(new Char[] { ';' }, StringSplitOptions.None);

                        #region RIGA DI INTESTAZIONE

                        // se sto processando la prima linea allora mi salvo il posizionamento delle colonne,
                        // altrimenti procedo all'inserimento del record indicato
                        if (columnsNumber == null)
                        {
                            string[] splittedRow = stringVar.Replace("\n", "").Split(';');
                            columnsNumber = new Dictionary<string, int>();
                            splittedRow = splittedRow.Select(s => s.ToUpperInvariant()).ToArray();
                            columnsNumber.Add("CODICE COL.", Array.IndexOf(splittedRow, "CODICE COL."));
                            columnsNumber.Add("CODICE FISCALE", Array.IndexOf(splittedRow, "CODICE FISCALE"));
                            columnsNumber.Add("COGNOME", Array.IndexOf(splittedRow, "COGNOME"));
                            columnsNumber.Add("NOME", Array.IndexOf(splittedRow, "NOME"));
                            columnsNumber.Add("QUALIFICA", Array.IndexOf(splittedRow, "QUALIFICA"));
                            columnsNumber.Add("LIVELLO", Array.IndexOf(splittedRow, "LIVELLO"));
                            columnsNumber.Add("BADGE", Array.IndexOf(splittedRow, "BADGE"));
                            columnsNumber.Add("DATA ASSOCIAZIONE", Array.IndexOf(splittedRow, "DATA ASSOCIAZIONE"));

                            if (columnsNumber.ContainsValue(-1))
                                columnsNumber = null;

                            if (columnsNumber == null)
                            {
                                errors.Add("Tracciato non confome alla riga " + rowNumber, "Tracciato record del file di import non conforme");
                                return errors;
                            }
                        }

                        #endregion

                        else
                        {
                            #region ESTRAZIONE CAMPI EXCEL

                            string cod_col = splittedLine[columnsNumber["CODICE COL."]].Trim();
                            string cod_fisc = splittedLine[columnsNumber["CODICE FISCALE"]].Trim();
                            string cognome = splittedLine[columnsNumber["COGNOME"]].Trim();
                            string nome = splittedLine[columnsNumber["NOME"]].Trim();
                            string qualifica = splittedLine[columnsNumber["QUALIFICA"]].Trim();
                            string livello = splittedLine[columnsNumber["LIVELLO"]].Trim();
                            string badge = splittedLine[columnsNumber["BADGE"]].Trim();
                            string data_ass = splittedLine[columnsNumber["DATA ASSOCIAZIONE"]].Trim();

                            #endregion

                            Col newCol = null;

                            //Il processo inizia solo se i seguenti campi sono configurati
                            if (cod_col != "" || cognome != "" || nome != "")
                            {
                                int colId;

                                newCol = Init();
                                newCol.Codice_Collaboratore = CommonService.AggiungiSpaziASinistraSeStringaNumerica(cod_col, 20);
                                newCol.Cognome_Col = cognome;
                                newCol.Nome_Col = nome;

                                #region CONTROLLO CODICE FISCALE

                                if (cod_fisc != "" && CommonService.ECodiceFiscaleValido(cod_fisc))
                                {
                                    newCol.Codice_Fiscale_Col = cod_fisc;
                                }
                                else if (!CommonService.ECodiceFiscaleValido(cod_fisc))
                                {

                                    errors.Add("Errore Codice fiscale alla riga " + rowNumber, BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CODICE_FISCALE_X_ERRATO, cod_fisc));
                                }

                                #endregion

                                #region CONTROLLO LIVELLO 

                                //Controllo  se il livello è presente nella tab_Decod
                                try
                                {
                                    if (Regex.IsMatch(livello, @"^\d+$"))
                                    {
                                        var level = RepoManager.Tab_DecodRepo.FirstOrDefault(lvl => lvl.Nome_Tab == "LIVELLO_COL" && lvl.Chiave_Tab == livello);
                                        newCol.Livello_Col = (level != null) ? level.Decodifica_Tab : "";
                                    }
                                }
                                catch (Exception) { };

                                #endregion

                                #region CONTROLLO QUALIFICA

                                //Controllo qualifica tab_Decod
                                try
                                {
                                    var qual = RepoManager.Tab_DecodRepo.FirstOrDefault(lvl => lvl.Nome_Tab == "QUALIFICHE_COL" && lvl.Decodifica_Tab == qualifica);
                                    newCol.Qualifica_Col = (qual != null) ? qual.Decodifica_Tab : "";
                                }
                                catch (Exception) { };

                                #endregion

                                #region CONTROLLO SE IL COLLABORATORE è GIà PRESENTE

                                // Controllo che il collaboratore non sia già presente, prima cercandolo per codice fiscale, poi per nome-cognome-dataNascita
                                // Fa una FirstOrDefault e non una SingleOrDefault perché, se nel file di import non è valorizzato il campo CODICE FISCALE (quindi "") non venga generata una exception;
                                // Nel caso in cui il campo CODICE FISCALE sia valorizzato, esse sarà univoco, quindi siamo sicuri che il primo trovato sia anche l'unico
                                Col presentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Codice_Fiscale_Col == newCol.Codice_Fiscale_Col);

                                //se non ha trovato il collaboratore per codice fiscale
                                if (presentCol == default(Col) || presentCol.Codice_Fiscale_Col == "" || presentCol.Codice_Fiscale_Col == null)
                                {
                                    //ricerco il collaboratore per cognome-nome
                                    presentCol = RepoManager.ColRepo.SingleOrDefault(col => col.Nome_Col == newCol.Nome_Col && col.Cognome_Col == newCol.Cognome_Col);

                                    //se non l'ho trovato anche in questo caso

                                    if (!(presentCol == default(Col)))
                                    {
                                        //recupero il col_id dal collaboratore presente
                                        newCol.Col_Id = presentCol.Col_Id;
                                    }
                                }
                                //se ho trovato il collboratore mediante la ricerca del codice fiscale
                                else
                                {
                                    colId = presentCol.Col_Id;
                                }

                                #endregion

                                #region CONTROLLO INSERIMENTO BADGE

                                //Eventuale associazione
                                if ((badge != "" && badge.Length == 5) || (badge != "" && badge.Length == 10))
                                {
                                    badge = AggiungiZeriASinistra(badge,10);
                                    Pru currPru = RepoManager.PruRepo.SingleOrDefault(pru => pru.Codice_Pru == badge);

                                    // Se la Pru non esiste già, viene creata
                                    if (currPru == default(Pru))
                                    {
                                        currPru = RepoManager.PruRepo.Init();
                                        currPru.Codice_Pru = badge;
                                        currPru.N_Serie_Pru = badge;

                                    }

                                    // Inizializzo la nuova associazione Pru_Col
                                    Pru_Col newPruCol = RepoManager.Pru_ColRepo.Init();
                                    newPruCol.Col_Id = newCol.Col_Id;
                                    newPruCol.Pru = currPru;
                                    newPruCol.Abilitazione_Data_Inizio_Pru_Col = (data_ass != "") ? DateTime.Parse(data_ass) : DateTime.Now;
                                    // Aggiunge l'associazione alla lista delle Pru_col da aggiungere a db
                                    newPruCol.Pru = currPru;

                                    newCol.Pru_Col.Add(newPruCol);

                                }
                                else if (badge != "" && badge.Length != 10)
                                {
                                    errors.Add("Errore Identificatore unità portatile alla riga " + rowNumber, BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_VALORE_CAMPO_X_ERRATO, "BADGE"));
                                }

                                #endregion 

                                colsToAdd.Add(newCol);

                            }
                            else
                            {
                                errors.Add("Riga" + rowNumber + " con parametri obbligatori mancanti", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_RECORD_X_DA_IMPORTARE_ERRATO, "alla linea " + rowNumber));
                            }
                        }
                    }
                    rowNumber++;
                }
                //// Aggiunge a db le associazion

                try
                {

                    DbSet.AddRange(colsToAdd);
                    //SaveChanges();
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
                _log.ErrorFormat("Errore durante il salvataggio dell'entità Col a causa dell'exception {0}", CommonService.GetInternalException(ex));
            }

            return savedEntities;
        }

        public static string AggiungiZeriASinistra(string sStringa, int iLunghezzaStringa)
        {
            if (string.IsNullOrEmpty(sStringa)) return null;
            return CompletaASinistra(sStringa, iLunghezzaStringa, ' ');
        }
        static public string CompletaASinistra(string sStringa, int iLunghezzaStringa, char completatore = ' ')
        {
            if (string.IsNullOrEmpty(sStringa)) return null;
            return sStringa.PadLeft(iLunghezzaStringa, completatore);
        }

        public override void BulkSaveChanges(Action<BulkOperation> action)
        {
            try
            {
                base.BulkSaveChanges(action);
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante il salvataggio dell'entità Col a causa dell'exception {0}", CommonService.GetInternalException(ex));
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

