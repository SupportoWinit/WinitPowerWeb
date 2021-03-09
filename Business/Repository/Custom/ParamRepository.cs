using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using DevExpress.XtraPrinting.Native;
using Domain;
using Data;
using Common;
using System.Linq.Expressions;
using System.Xml.Linq;
using log4net;

namespace Business.Repository.Custom
{
    public class ParamRepository : GenericRepository<Param>, IParamRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ParamRepository));

        #region Public static properties

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
                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_ParamRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_DecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ParamRepo", oLista);
                }
                return oLista;
            }
        }

        #endregion

        #region Private static methods

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_ParamRepo", null);
        }

        #endregion

        #region Fields

        /// <summary>
        /// Elenco dei colori configurati nel file xml (il valore viene messo in session
        /// </summary>
        private Dictionary<string, Color> _configuredColors
        {
            get
            {
                var colors = PowerWebContext.GetFromSession<Dictionary<string, Color>>("ConfiguredColors");
                if (colors == null)
                {
                    colors = ReadColorsFromXml();
                    PowerWebContext.SetToSession("ConfiguredColors", colors);
                }
                return colors;
            }
        }

        #endregion

        #region Constructors

        public ParamRepository(PowerWebEntities context)
            : base(context)
        {
        }

        #endregion

        #region Public properties        

        public Param ParametersRow
        {
            get
            {
                var firstParamRow = PowerWebContext.GetFromSession<Param>("General_ParametersRow") as Param;
                if (firstParamRow == null)
                {
                    firstParamRow = RepoManager.ParamRepo.First();

                    PowerWebContext.SetToSession<Param>("General_ParametersRow", firstParamRow);
                }
                return firstParamRow;
            }
        }


        /// <summary>
        /// Recupera la configurazione generale del notturno presente nella scheda parametri.
        /// Composta da:
        /// - Variabile booleana che indica se il modulo del notturno risulta attivo.
        /// - Il tipo di notturno configurato.
        /// - La nuova mezzanotte configurata.
        /// </summary>
        /// <value>
        /// La configurazione generale del notturno presente nella scheda parametri.
        /// Composta da:
        /// - Variabile booleana che indica se il modulo del notturno risulta attivo.
        /// - Il tipo di notturno configurato.
        /// - La nuova mezzanotte configurata.
        /// </value>
        public Tuple<bool, NocturneTypeEnum, TimeSpan, TimeSpan> NocturneGeneralConfiguration
        {
            get
            {
                bool nocturneModuleActive = ParametersRow.Abilita_Notturno;
                return new Tuple<bool, NocturneTypeEnum, TimeSpan, TimeSpan>(nocturneModuleActive, nocturneModuleActive ? (NocturneTypeEnum)ParametersRow.TipoNotturno : NocturneTypeEnum.None, ParametersRow.Default_Durata_Max_Gruppo_Notte_Ril ?? TimeSpan.Zero, ParametersRow.Durata_Notturno ?? TimeSpan.Zero);
            }
        }

        #endregion

        #region Public methods

        public void ResetParametersRow()
        {
            PowerWebContext.SetToSession<Param>("General_ParametersRow", null);
        }

        public override void SetEntityBeforeAddOrUpdate(Param entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica;
            entity.DataOraUltimaModifica = DateTime.UtcNow;
        }

        public override Dictionary<string, string> Check(Param entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession();
            try
            {
                //Leggo la DataOraUltimaModifica ATTUALE dal REcord del DB per verificare che nessuno abbia modificato il Record nel frattempo
                Param OldRecord = RepoManager.ParamRepo.Single(u => u.Param_Id == entity.Param_Id, true);
                DateTime DataOraRecordDb = OldRecord.DataOraUltimaModifica;
                if (!isNew)
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA));
                    }

                #region Verifica cambio Abilitazione/Disabilitazione delle Attività (se variata)
                if (CommonService.Nz(entity.Abilita_Att, false) != CommonService.Nz(OldRecord.Abilita_Att, false))
                {
                    //Verifico che sia fatta solo da un Utente WINIT
                    if (CommonService.Nz(entity.Abilita_Att, false) == true &&
                        PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilita_Att),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_MODIFICA_RISERVATA_ALLA_WINIT, PowerWebResources.FLD_ABILITA_ATT));

                    //Leggo i Cantieri che hanno Tipologia = ATT
                    var cants = RepoManager.CantRepo.Find(cant => cant.Tipologia_Can == "ATT", true).ToList();

                    //NON può essere disattivata se esistono dei Cantieri definiti Attività
                    if (CommonService.Nz(entity.Abilita_Att, false) == false)
                    {
                        if (cants.Count > 0)
                            //Se ci sono Cantieri con Tipologia ATT non si può disabilitare la gestione delle ATTIVITA'
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilita_Att),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_DISABILITAZIONE_ATTIVITA_NON_POSSIBILE_CON_CANTIERI_DEFINITI_ATTIVITA,
                                    PowerWebResources.FLD_ABILITA_ATT));
                    }
                    if (CommonService.Nz(entity.Abilita_Att, false) == true)
                    //NON può essere attivata se esistono dei Cantieri definiti Attività
                    {
                        //Leggo i Cantieri che hanno Tipologia = ATT                       
                        if (cants.Count > 0)
                            //Se ci sono Cantieri con Tipologia ATT non si può disabilitare la gestione delle ATTIVITA'
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilita_Att),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_ABILITAZIONE_ATTIVITA_NON_POSSIBILE_CON_CANTIERI_DEFINITI_ATTIVITA,
                                    PowerWebResources.FLD_ABILITA_ATT));
                    }

                }
                #endregion

                #region Verifica Cambio Abilitazione/Disabilitazione dei Passaggi (se Variata)
                if (CommonService.Nz(entity.Abilita_Pass, false) != CommonService.Nz(OldRecord.Abilita_Pass, false))
                {
                    //Verifico che sia fatta solo da un Utente WINIT
                    if (CommonService.Nz(entity.Abilita_Pass, false) == true &&
                        PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilita_Pass),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_MODIFICA_RISERVATA_ALLA_WINIT, PowerWebResources.FLD_ABILITA_PASS));

                    //Leggo i Cantieri che hanno Singola_reg=True
                    var cants = RepoManager.CantRepo.Find(cant => cant.Singola_Reg == true, true).ToList();

                    if (CommonService.Nz(entity.Abilita_Pass, false) == false && cants.Count > 0)
                        //NON può essere Disattivata se esistono dei Cantieri con Abilitati i Passaggi                                        
                        //Se ci sono Cantieri con Singola_Reg Attivata non si può disabilitare la gestione dei Passaggi
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilita_Pass),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_DISABILITAZIONE_PASSAGGI_NON_POSSIBILE_CON_CANTIERI_DEFINITI_PASSAGGI,
                                PowerWebResources.FLD_ABILITA_PASS));
                    if (CommonService.Nz(entity.Abilita_Pass, false) == true && cants.Count > 0)
                        //NON può essere attivata se esistono dei Cantieri con i Passaggi Abilitati                                               
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilita_Pass),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_ABILITAZIONE_PASSAGGI_NON_POSSIBILE_CON_CANTIERI_DEFINITI_PASSAGGI,
                                PowerWebResources.FLD_ABILITA_PASS));

                    //Leggo i Collaboratori che hanno Singola_reg=True
                    var cols = RepoManager.ColRepo.Find(col => col.Singola_Reg == true, true).ToList();

                    //NON può essere disattivata se esistono dei Cantieri con Abilitati i Passaggi
                    if (CommonService.Nz(entity.Abilita_Pass, false) == false && cols.Count > 0)
                        //Se ci sono Cantieri con Singola_reg Attivata non si può disabilitare la gestione dei Passaggi
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilita_Pass),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_DISABILITAZIONE_PASSAGGI_NON_POSSIBILE_CON_COLLABORATORI_DEFINITI_PASSAGGI,
                                PowerWebResources.FLD_ABILITA_PASS));
                    if (CommonService.Nz(entity.Abilita_Pass, false) == true && cols.Count > 0)
                        //Se ci sono Cantieri con Tipologia ATT non si può disabilitare la gestione delle ATTIVITA'
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilita_Pass),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_ABILITAZIONE_PASSAGGI_NON_POSSIBILE_CON_COLLABORATORI_DEFINITI_PASSAGGI,
                                PowerWebResources.FLD_ABILITA_PASS));
                }
                #endregion

                if (CommonService.Nz(entity.Abilita_Arrotondamenti, false) == true && OldRecord.Abilita_Arrotondamenti != true)
                    if (CommonService.Nz(entity.Abilita_Arrotondamenti, false) == true &&
                       PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilita_Arrotondamenti),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_MODIFICA_RISERVATA_ALLA_WINIT, PowerWebResources.FLD_ABILITA_ARROTONDAMENTI));
                if (CommonService.Nz(entity.Abilita_GPS, false) == true && OldRecord.Abilita_GPS != true)
                    if (CommonService.Nz(entity.Abilita_GPS, false) == true &&
                       PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilita_GPS),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_MODIFICA_RISERVATA_ALLA_WINIT, PowerWebResources.FLD_ABILITA_GPS));
                if (CommonService.Nz(entity.Flag_Ore_Viaggi, 0) != 0 && OldRecord.Flag_Ore_Viaggi == 0)
                    if (CommonService.Nz(entity.Flag_Ore_Viaggi, 0) != 0 &&
                       PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_Ore_Viaggi),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_MODIFICA_RISERVATA_ALLA_WINIT, PowerWebResources.FLD_FLAG_ORE_VIAGGI));
                // VERIFICO CHE SE I CAMPI DI ATTIVAZIONE DELLE NUMERAZIONI AUTOMATICHE x COL/CAN VENGONO MODIFICATI A TRUE (mentre prima erano a False) SOLO SE L'UTENTE SIA WINIT
                if (CommonService.Nz(entity.Attiva_Num_Aut_Can, false) == true && OldRecord.Attiva_Num_Aut_Can != true)
                {
                    if (CommonService.Nz(entity.Attiva_Num_Aut_Can, false) == true &&
                        PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Attiva_Num_Aut_Can),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_MODIFICA_RISERVATA_ALLA_WINIT, PowerWebResources.FLD_ATTIVA_NUM_AUT_CAN));
                    //Se attivo la NUM AUT allora verifico che l'Ultimo CANTIERE abbia Codice NUMERICO
                    // se non ci sono cantieri do per assodato che siano numerici, altrimenti con quest'impostazione non si riesce a partire da 0
                    var codCanMax = RepoManager.CantRepo.DbSet.Any() ? RepoManager.CantRepo.Max(c => c.Codice_Cantiere, true) : "0";
                    int codCanMaxNum = 0;
                    if (int.TryParse(codCanMax, out codCanMaxNum) == false)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Attiva_Num_Aut_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_ULTIMO_CANTIERE_NON_NUMERICO, PowerWebResources.FLD_ATTIVA_NUM_AUT_CAN));
                }
                if (CommonService.Nz(entity.Attiva_Num_Aut_Col, false) == true && OldRecord.Attiva_Num_Aut_Col != true)
                {
                    if (CommonService.Nz(entity.Attiva_Num_Aut_Col, false) == true &&
                        PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Attiva_Num_Aut_Col),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_MODIFICA_RISERVATA_ALLA_WINIT, PowerWebResources.FLD_ATTIVA_NUM_AUT_COL));
                    //Se attivo la NUM AUT allora verifico che l'Ultimo CANTIERE abbia Codice NUMERICO
                    // se non ci sono collaboratori do per assodato che siano numerici, altrimenti con quest'impostazione non si riesce a partire da 0

                    var codColMax = RepoManager.ColRepo.DbSet.Any() ? RepoManager.ColRepo.Max(c => c.Codice_Collaboratore, true) : "0";
                    int codColMaxNum = 0;
                    if (int.TryParse(codColMax, out codColMaxNum) == false)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Attiva_Num_Aut_Col),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_ULTIMO_COLLABORATORE_NON_NUMERICO, PowerWebResources.FLD_ATTIVA_NUM_AUT_CAN));
                }
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                // NON SERVE PERCHE' NON VIENE GESTITO L'INSERIMENTO
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella            
                //

                if (CommonService.Nz(entity.ComboboxRowsPerPage, 0) == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.ComboboxRowsPerPage),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_COMBOBOXROWSPERPAGE));
                if (String.IsNullOrEmpty(entity.CompanyName))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.CompanyName),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_COMPANYNAME));
                if (CommonService.Nz(entity.RowsPerPage, 0) == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.RowsPerPage),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_ROWSPERPAGE));
                if (CommonService.Nz(entity.Mesi_Validita_Reg, 0) == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Mesi_Validita_Reg),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_MESI_VALIDITA_REG));
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                if (CommonService.Nz(entity.Default_Durata_Max_Ril, new TimeSpan(00, 00, 00)) < CommonService.Nz(entity.Default_Durata_Min_Ril, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Durata_Max_Ril),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_DURATA_MAX_RIL, PowerWebResources.FLD_DEFAULT_DURATA_MAX_RIL));
                if (CommonService.Nz(entity.Default_Durata_Max_Gruppo_Ril, new TimeSpan(00, 00, 00)) < CommonService.Nz(entity.Default_Durata_Max_Ril, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Durata_Max_Gruppo_Ril),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_DURATA_MAX_GRUPPO_RIL, PowerWebResources.FLD_DEFAULT_DURATA_MAX_RIL));
                if (CommonService.Nz(entity.Default_Durata_Min_Ril, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Default_Durata_Max_Gruppo_Ril, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Durata_Max_Gruppo_Ril),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_DURATA_MAX_GRUPPO_RIL, PowerWebResources.FLD_DEFAULT_DURATA_MIN_RIL));
                if (CommonService.Nz(entity.Default_Soglia_Arrot_I, 0) > CommonService.Nz(entity.Default_Minuti_Arrot_I, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Soglia_Arrot_I),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_SOGLIA_ARROT_I, PowerWebResources.FLD_DEFAULT_MINUTI_ARROT_I));
                if (CommonService.Nz(entity.Default_Soglia_Arrot_F, 0) > CommonService.Nz(entity.Default_Minuti_Arrot_F, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Soglia_Arrot_F),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_SOGLIA_ARROT_F, PowerWebResources.FLD_DEFAULT_MINUTI_ARROT_F));
                if (CommonService.Nz(entity.Default_Soglia_Durata, 0) > CommonService.Nz(entity.Default_Minuti_Durata, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Soglia_Durata),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_SOGLIA_DURATA, PowerWebResources.FLD_DEFAULT_MINUTI_DURATA));
                if (CommonService.Nz(entity.Default_Soglia_Arrot_I, 0) > CommonService.Nz(entity.Default_Minuti_Arrot_I, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Soglia_Arrot_I),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_SOGLIA_ARROT_I, PowerWebResources.FLD_DEFAULT_MINUTI_ARROT_I));
                if (CommonService.Nz(entity.Default_Soglia_Arrot_F, 0) > CommonService.Nz(entity.Default_Minuti_Arrot_F, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Soglia_Arrot_F),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_SOGLIA_ARROT_F, PowerWebResources.FLD_DEFAULT_MINUTI_ARROT_F));
                if (CommonService.Nz(entity.Default_Soglia_Durata, 0) > CommonService.Nz(entity.Default_Minuti_Durata, 0))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Soglia_Durata),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_SOGLIA_DURATA, PowerWebResources.FLD_DEFAULT_MINUTI_DURATA));
                if (CommonService.Nz(entity.Durata_Massima_Viaggio, new TimeSpan(00, 00, 00)) < CommonService.Nz(entity.Durata_Minima_Viaggio, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Massima_Viaggio),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y,
                      PowerWebResources.FLD_DURATA_MASSIMA_VIAGGIO, PowerWebResources.FLD_DURATA_MINIMA_VIAGGIO));
                if (CommonService.Nz(entity.Fascia_Ore_Viaggi_1_Inizio, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Fascia_Ore_Viaggi_1_Fine, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fascia_Ore_Viaggi_1_Inizio),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_FASCIA_ORE_VIAGGI_1_INIZIO, PowerWebResources.FLD_FASCIA_ORE_VIAGGI_1_FINE));
                if (CommonService.Nz(entity.Fascia_Ore_Viaggi_2_Inizio, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Fascia_Ore_Viaggi_2_Fine, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fascia_Ore_Viaggi_2_Inizio),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_FASCIA_ORE_VIAGGI_2_INIZIO, PowerWebResources.FLD_FASCIA_ORE_VIAGGI_2_FINE));
                if (CommonService.Nz(entity.Fascia_Ore_Viaggi_3_Inizio, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Fascia_Ore_Viaggi_3_Fine, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fascia_Ore_Viaggi_3_Inizio),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_FASCIA_ORE_VIAGGI_3_INIZIO, PowerWebResources.FLD_FASCIA_ORE_VIAGGI_3_FINE));
                if (CommonService.Nz(entity.Fascia_Ore_Viaggi_4_Inizio, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Fascia_Ore_Viaggi_4_Fine, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fascia_Ore_Viaggi_4_Inizio),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_FASCIA_ORE_VIAGGI_4_INIZIO, PowerWebResources.FLD_FASCIA_ORE_VIAGGI_4_FINE));
                if (CommonService.Nz(entity.Fascia_Ore_Viaggi_5_Inizio, new TimeSpan(00, 00, 00)) > CommonService.Nz(entity.Fascia_Ore_Viaggi_5_Fine, new TimeSpan(00, 00, 00)))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fascia_Ore_Viaggi_5_Inizio),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_FASCIA_ORE_VIAGGI_5_INIZIO, PowerWebResources.FLD_FASCIA_ORE_VIAGGI_5_FINE));
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //   
                if (CommonService.Nz(entity.Flag_Ore_Viaggi, 0) == (int)FlagTripHoursParamEnum.None && CommonService.Nz(entity.Flag_Ore_Viaggi_Inizio_Fine, 0) != (int)FlagTripHoursParamEnum.None)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_Ore_Viaggi_Inizio_Fine),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_ERRATO, PowerWebResources.FLD_FLAG_ORE_VIAGGI_INIZIO_FINE));
                if (CommonService.Nz(entity.Ctrl_Tab_Comuni, 0) > 1)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Minuti_Arrot_I),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_CTRL_TAB_COMUNI, PowerWebResources.VALORE_1));
                if (CommonService.Nz(entity.Default_Minuti_Arrot_I, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Minuti_Arrot_I),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_MINUTI_ARROT_I, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Default_Minuti_Arrot_F, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Minuti_Arrot_F),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_MINUTI_ARROT_F, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Default_Minuti_Durata, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Minuti_Durata),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_MINUTI_DURATA, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Default_Soglia_Arrot_I, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Soglia_Arrot_I),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_SOGLIA_ARROT_I, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Default_Soglia_Arrot_F, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Soglia_Arrot_F),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_SOGLIA_ARROT_F, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Default_Soglia_Durata, 0) > 60)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Default_Soglia_Durata),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_DEFAULT_SOGLIA_DURATA, PowerWebResources.VALORE_60));
                if (CommonService.Nz(entity.Tempo_Doppia_Reg, 0) > 10)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tempo_Doppia_Reg),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                          PowerWebResources.FLD_TEMPO_DOPPIA_REG, PowerWebResources.VALORE_10));
                //
                //4.1 verifico, per una serie di campi, che il valore del campo sia minore o minore di un certo valore
                //
                //
                //5) verifico, per una serie di campi, che il valore del campo sia presente nella relativa Tabella
                //                                         
                if (CommonService.Nz(entity.Ctrl_Tab_Comuni, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.CTRL_TAB_COMUNI.ToString() && x.Chiave_Tab == entity.Ctrl_Tab_Comuni.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Ctrl_Tab_Comuni.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_CTRL_TAB_COMUNI));
                if (CommonService.Nz(entity.Ctrl_Codice_Fisc, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.CTRL_CODICE_FISC.ToString() && x.Chiave_Tab == entity.Ctrl_Codice_Fisc.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Ctrl_Codice_Fisc.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_CTRL_CODICE_FISC));
                if (CommonService.Nz(entity.Ctrl_Codice_IBAN, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.CTRL_CODICE_IBAN.ToString() && x.Chiave_Tab == entity.Ctrl_Codice_IBAN.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Ctrl_Codice_IBAN.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_CTRL_CODICE_IBAN));
                if (CommonService.Nz(entity.Flag_Monte_Ore, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.FLAG_MONTE_ORE.ToString() && x.Chiave_Tab == entity.Flag_Monte_Ore.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_Monte_Ore.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_FLAG_MONTE_ORE));
                if (CommonService.Nz(entity.Flag_Ore_Viaggi, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_CALCOLO_VIAGGI.ToString() && x.Chiave_Tab == entity.Flag_Ore_Viaggi.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_Ore_Viaggi.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_FLAG_ORE_VIAGGI));
                if (CommonService.Nz(entity.Flag_Ore_Viaggi_Inizio_Fine, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_CALCOLO_VIAGGI.ToString() && x.Chiave_Tab == entity.Flag_Ore_Viaggi_Inizio_Fine.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Flag_Ore_Viaggi.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_FLAG_ORE_VIAGGI_INIZIO_FINE)); ;
                if (CommonService.Nz(entity.Metodo_Arrotondamento, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.METODO_ARROTONDAMENTO.ToString() && x.Chiave_Tab == entity.Metodo_Arrotondamento.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Metodo_Arrotondamento.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_METODO_ARROTONDAMENTO));
                //Il Metodo di Arrotondamento "9" può essere selezionato solo sui Record CANT e/o COL ma NON in Scheda parametri
                if (CommonService.Nz(entity.Metodo_Arrotondamento, 0) == 9)
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_NON_SELEZIONABILE_IN_QUESTO_CONTESTO,
                    PowerWebResources.FLD_METODO_ARROTONDAMENTO);
                //Al momento NON viene gestito il Metodo di Arrotondamento previsto in Power/Access (1=Durata,3=X Orario)
                if (CommonService.Nz(entity.Metodo_Arrotondamento, 0) == 1 || CommonService.Nz(entity.Metodo_Arrotondamento, 0) == 3)
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_NON_GESTITO_ATTUALMENTE,
                    PowerWebResources.FLD_METODO_ARROTONDAMENTO);
                if (CommonService.Nz(entity.ModuleType, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.MODULETYPE.ToString() && x.Chiave_Tab == entity.ModuleType.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.ModuleType.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_MODULETYPE));
                if (CommonService.Nz(entity.Tipo_Arrotondamento, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_ARROTONDAMENTO.ToString() && x.Chiave_Tab == entity.Tipo_Arrotondamento.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Arrotondamento.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPO_ARROTONDAMENTO));
                //if (CommonService.Nz(entity.Tipo_Assegnazione_KMMinuti, 0) != 0)
                //    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                //    && x.Nome_Tab == TabDecodNameEnum.TIPO_ASSEGNAZIONE_KMMINUTI.ToString() && x.Chiave_Tab == entity.Tipo_Assegnazione_KMMinuti.ToString()) == null)
                //        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Assegnazione_KMMinuti.ToString()),
                //          CommonServiceBiz.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                //          PowerWebResources.FLD_TIPO_ASSEGNAZIONE_VIAGGI));
                if (CommonService.Nz(entity.Tipo_Viaggio, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_VIAGGIO.ToString() && x.Chiave_Tab == entity.Tipo_Viaggio.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Viaggio.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPO_VIAGGIO));
                if (CommonService.Nz(entity.TipoNotturno, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_NOTTURNO.ToString() && x.Chiave_Tab == entity.TipoNotturno.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.TipoNotturno.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPONOTTURNO));
                if (CommonService.Nz(entity.Utilizzo_Fasce_Viaggi, 0) != 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.FASCE_VIAGGI.ToString() && x.Chiave_Tab == entity.Utilizzo_Fasce_Viaggi.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utilizzo_Fasce_Viaggi.ToString()),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_UTILIZZO_FASCE_VIAGGI));
                //
                //5.1) verifico, per una serie di campi, che il valore del campo sia presente nella relativa tabella 
                //    e modifico il valore anche di altri campi
                //
                if (CommonService.Nz(entity.Cant_Id, 0) != 0)
                {
                    Cant oRecord = RepoManager.CantRepo.SingleOrDefault(x => x.Cant_Id == entity.Cant_Id);
                    if (oRecord == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_NON_ESISTE_IN_TABELLA_Y,
                         PowerWebResources.FLD_CANTIERE_VIAGGIO, PowerWebResources.STR_CANTIERI));
                    else
                        //Il Cantiere Viaggio deve essere un Cantiere (NON una Attività e/o un Assistito)                    
                        if (oRecord.Tipologia_Can.ToUpper() != "CAN")
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_TIPOLOGIA_CANTIERE_VIAGGIO_NON_ACCETTABILE,
                    PowerWebResources.FLD_CANTIERE_VIAGGIO));

                }
                //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);
            }
            catch (Exception ex)
            {
                var CodErr = "Param_Id: " + entity.Param_Id;
                throw ex;
            }
            return result;
        }

        /// <summary>
        /// Metodo che dato un enum ritorna il valore colore corrispondente configurato
        /// </summary>
        /// <param name="enumToSearch">l'enum da ricercare</param>
        /// <param name="isBackground">Se si sta cercando un valore di background è <c>true</c>. Altrimenti <c>false</c></param>
        /// <returns>Il colore configurato rispetto all'enum passato come parametro</returns>
        public Color GetColorFromEnum(Enum enumToSearch, bool isBackgruound)
        {
            // inizializzazione del valore di ritorno del metodo
            // il valore di default della griglia è nero per il foreground e bianco per il background
            Color returnColor;
            if (isBackgruound)
                returnColor = Color.White;
            else
                returnColor = Color.Black;

            // costruisco la stringa i ricerca dell'enum
            string enumSearchString = String.Format("{0}.{1}", enumToSearch.GetType().Name, enumToSearch.ToString());

            // recupero i colori configurati
            var configuredColors = _configuredColors;

            // se la chiave è prsente tra i valori configurati allora ritorno il colore configurato
            if (configuredColors.ContainsKey(enumSearchString))
                returnColor = configuredColors[enumSearchString];

            return returnColor;
        }

        /// <summary>
        /// Metodo che dato un enum ritorna il valore della corrispondente pesonalizzazione attivata
        /// </summary>
        /// <param name="enumToSearch">l'enum da ricercare</param>
        /// <returns>Il valore della personalizzazione configurata. 0 equivale a pesonalizzazione non attiva</returns>
        public int GetCustomizationFromEnum(Enum enumToSearch)
        {
            // inizializzazione del valore di ritorno del metodo (0 equivale a personalizzazione non attiva)
            int returnCustomizationValue = 0;

            // costruisco la stringa i ricerca dell'enum
            string enumSearchString = String.Format("{0}.{1}", enumToSearch.GetType().Name, enumToSearch.ToString());

            // recupero le personalizzazioni configurate
            var configureCustomization = _configuredCustomization;

            // se la chiave è prsente tra i valori configurati allora ritorno il valore della personalizzazione trovato
            if (configureCustomization.ContainsKey(enumSearchString))
                returnCustomizationValue = configureCustomization[enumSearchString];

            return returnCustomizationValue;
        }

        /// <summary>
        /// Metodo che dato un enum e il nomne di un parametro ritorna il valore del parametro per la personalizzazione attivata
        /// </summary>
        /// <param name="enumToSearch">l'enum da ricercare</param>
        /// <param name="paramNameToSearch">Il nome del parametro da ricercare (case sensitive)</param>
        /// <returns>Il valore assunto dal parametro per la personalizzazione (String.Empty se non presente o se personalizzazione non attiva).</returns>
        public string GetCustomizationParamFromEnum(Enum enumToSearch, string paramNameToSearch)
        {
            // inizializzazione del valore di ritorno del metodo (String.Empty equivale a personalizzazione non attiva o parametro non presente)
            string returnParamValue = String.Empty;

            // costruisco la stringa i ricerca dell'enum
            string enumSearchString = String.Format("{0}.{1}", enumToSearch.GetType().Name, enumToSearch.ToString());

            // recupero le personalizzazioni configurate con i relativi parametri
            var configureCustomization = _configuredCustomizationParameters;

            // se la chiave è prsente tra i valori configurati allora verifico la presenza del parametro, e se presente si ritorna il valore configurato
            if (configureCustomization.ContainsKey(enumSearchString))
            {
                if (configureCustomization[enumSearchString].Any(param => param.Key == paramNameToSearch))
                {
                    returnParamValue = configureCustomization[enumSearchString].FirstOrDefault(param => param.Key == paramNameToSearch).Value;
                }
            }

            // ritorno del valore del metodo
            return returnParamValue;
        }

        public bool IsElaborationReady()
        {
            return !DbSet.AsNoTracking().First().Elaborate_Semaforo;
        }

        public bool LockElaboration()
        {

            bool result = true;

            try
            {
                //se attivo la customization di autochiusura pulisco le entità prima di salvare per evitare valori NULL nelle reg
                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresEnum) == (int)AutoClosuresEnum.Sede)
                    DetachAllEntities();
                else
                {
                    DbSet.First().Elaborate_Semaforo = true;
                    SaveChanges();
                }                    

            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la fase di locking dell'elaborazione: {0}", ex.Message);
                result = false;
            }

            return result;
        }

        public void UnLockElaboration()
        {
            try
            {
                //se attivo la customization di autochiusura pulisco le entità prima di salvare per evitare valori NULL nelle reg
                if(RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresEnum) == (int)AutoClosuresEnum.Sede)
                    DetachAllEntities();
                else {
                    DbSet.First().Elaborate_Semaforo = false;
                    SaveChanges();
                }

                
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la fase di unlocking dell'elaborazione: {0}", ex.Message);
            }
        }

        public void SaveGeoBadgeRegIndex(int index)
        {
            try
            {
                var paramRow = DbSet.First();
                paramRow.Indice_Timbrature_GeoBadge = ParametersRow.Indice_Timbrature_GeoBadge = index;
                SaveChanges();
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante il salvataggio del seguente indice ({0}) riguardante pa procedura di scarico timbrature geoBadge : {1}", index, ex.Message);
            }
        }

        public void SaveFlutterAppRegIndex(int index)
        {
            try
            {
                var paramRow = DbSet.First();
                paramRow.Indice_Timbrature_FlutterApp = ParametersRow.Indice_Timbrature_FlutterApp = index;
                SaveChanges();
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante il salvataggio del seguente indice ({0}) riguardante pa procedura di scarico timbrature FlutterApp : {1}", index, ex.Message);
            }
        }

        #region Gestione XML Import esterno

        public string GetConnectionHost(string fullName)
        {
            var doc = XDocument.Load(System.Web.HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.ExternalImportConfig));
            var baseNode = doc.Descendants().Where(node => (string)node.Attribute("company") == fullName);

            XElement connectionHost = baseNode.Descendants("connectionHost").FirstOrDefault();

            return (string)connectionHost.Attribute("value");
        }
        public string GetCantInsertApi(string fullName)
        {
            var doc = XDocument.Load(System.Web.HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.ExternalImportConfig));
            var hub = doc.Descendants().Where(node => (string)node.Attribute("company") == fullName).First();

            return (string)hub.Descendants().Where(node => node.Name.LocalName == "PostNewCant").First().Attribute("value");


        }
        public string GetCantUpdateApi(string fullName)
        {
            var doc = XDocument.Load(System.Web.HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.ExternalImportConfig));
            var hub = doc.Descendants().Where(node => (string)node.Attribute("company") == fullName);

            return (string)hub.Descendants().Where(node => node.Name.LocalName == "PostEditCant").First().Attribute("value");
        }
        public string GetCantDeleteApi(string fullName)
        {
            var doc = XDocument.Load(System.Web.HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.ExternalImportConfig));
            var hub = doc.Descendants().Where(node => (string)node.Attribute("company") == fullName);

            return (string)hub.Descendants().Where(node => node.Name.LocalName == "PostDeletedCant").First().Attribute("value");
        }

        public string GetTenant(string fullName)
        {
            var doc = XDocument.Load(System.Web.HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.ExternalImportConfig));
            var hub = doc.Descendants().Where(node => (string)node.Attribute("company") == fullName);

            return (string)hub.Descendants().Where(node => node.Name.LocalName == "tenant").First().Attribute("value");
        }

        public string GetUser(string fullName)
        {
            var doc = XDocument.Load(System.Web.HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.ExternalImportConfig));
            var hub = doc.Descendants().Where(node => (string)node.Attribute("company") == fullName);

            return (string)hub.Descendants().Where(node => node.Name.LocalName == "user").First().Attribute("value");
        }

        public string GetPassword(string fullName)
        {
            var doc = XDocument.Load(System.Web.HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.ExternalImportConfig));
            var hub = doc.Descendants().Where(node => (string)node.Attribute("company") == fullName);

            return (string)hub.Descendants().Where(node => node.Name.LocalName == "password").First().Attribute("value");
        }

        #endregion

        public bool IsCurrentUserCustomizationEnabled(CustomizationEnum customization, string parameterName)
        {
            return GetCustomizationFromEnum(customization) == 1 && GetCustomizationParamFromEnum(customization, parameterName) == "1";
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Metodo che va a leggere il file di configurazione dei colori e ne restituisce 
        /// </summary>
        /// <returns>Il dizionario con l'elenco delle chiavi(enum) e valori dei colori</returns>
        private Dictionary<string, Color> ReadColorsFromXml()
        {
            // recupero il percorso del file di configurazione xml
            string configFilePath = Path.Combine(System.Web.HttpRuntime.AppDomainAppPath, Common.Properties.Settings.Default.XmlColorConfigRelativeFilePath);
            // inizializzazione del valore di ritorno
            var returnColors = new Dictionary<string, Color>();

            // se il file di configurazione esiste
            if (File.Exists(configFilePath))
            {
                // apro il file di configurazione
                XDocument xmlConfiguration = XDocument.Load(configFilePath);

                // ciclo su tutti i nodi i secondo livello con tag fc
                foreach (var color in xmlConfiguration.Descendants("fc"))
                {
                    // se il valore non è già stato inserito allora
                    if (!returnColors.ContainsKey(color.Attribute("ID").Value))
                    {
                        returnColors.Add(color.Attribute("ID").Value, Color.FromArgb(Convert.ToInt32(color.Attribute("R").Value), Convert.ToInt32(color.Attribute("G").Value), Convert.ToInt32(color.Attribute("B").Value)));
                    }
                }
            }

            return returnColors;
        }

        /// <summary>
        /// Elenco dei valori associati alle singole personalizzazioni
        /// </summary>
        private Dictionary<string, int> _configuredCustomization
        {
            get
            {
                var customizations = PowerWebContext.GetFromSession<Dictionary<string, int>>("ConfiguredCustomizations");
                if (customizations == null)
                {
                    customizations = ReadCustomizationsFromXml();
                    PowerWebContext.SetToSession("ConfiguredCustomizations", customizations);
                }
                return customizations;
            }
        }

        /// <summary>
        /// Elenco dei parametri associati alle singole personalizzazioni associati alle singole personalizzazioni
        /// </summary>
        private Dictionary<string, List<KeyValuePair<string, string>>> _configuredCustomizationParameters
        {
            get
            {
                var customizations = PowerWebContext.GetFromSession<Dictionary<string, List<KeyValuePair<string, string>>>>("ConfiguredCustomizationsParameters");
                if (customizations == null)
                {
                    customizations = ReadCustomizationsParametersFromXml();
                    PowerWebContext.SetToSession("ConfiguredCustomizationsParameters", customizations);
                }
                return customizations;
            }
        }

        /// <summary>
        /// Metodo che va a leggere il file di configurazione delle pesonalizzazioni e ne restituisce in un dizionario i valori
        /// </summary>
        /// <returns>Il dizionario con l'elenco delle chiavi (enum) e valori delle personalizzazioni</returns>
        private Dictionary<string, int> ReadCustomizationsFromXml()
        {
            // recupero il percorso del file di configurazione xml
            string configFilePath = Path.Combine(System.Web.HttpRuntime.AppDomainAppPath, Common.Properties.Settings.Default.XmlCustomizationConfigFileRelativePath);

            // inizializzazione del valore di ritorno
            var returnCustomizaions = new Dictionary<string, int>();

            // se il file di configurazione esiste
            if (File.Exists(configFilePath))
            {
                // apro il file di configurazione
                XDocument xmlConfiguration = XDocument.Load(configFilePath);

                // ciclo su tutti i nodi i secondo livello con tag fc
                foreach (var customization in xmlConfiguration.Descendants("c"))
                {
                    // se il valore non è già stato inserito allora
                    if (!returnCustomizaions.ContainsKey(customization.Attribute("ID").Value))
                    {
                        returnCustomizaions.Add(customization.Attribute("ID").Value, Convert.ToInt32(customization.Attribute("Value").Value));
                    }
                }
            }

            return returnCustomizaions;
        }

        /// <summary>
        /// Metodo che va a leggere il file di configurazione delle pesonalizzazioni e ne restituisce in un dizionario i parametri collegati alle relative customization
        /// </summary>
        /// <returns>Il dizionario con l'elenco delle chiavi (enum) e un oggetto dinamico con l'elenco dei parametri configurati (null in caso di mancata presenza)</returns>
        private Dictionary<string, List<KeyValuePair<string, string>>> ReadCustomizationsParametersFromXml()
        {
            // recupero il percorso del file di configurazione xml
            string configFilePath = Path.Combine(System.Web.HttpRuntime.AppDomainAppPath, Common.Properties.Settings.Default.XmlCustomizationConfigFileRelativePath);

            // inizializzazione del valore di ritorno
            var returnCustomizaions = new Dictionary<string, List<KeyValuePair<string, string>>>();

            // se il file di configurazione esiste
            if (File.Exists(configFilePath))
            {
                // apro il file di configurazione
                XDocument xmlConfiguration = XDocument.Load(configFilePath);

                // ciclo su tutti i nodi i secondo livello con tag fc
                foreach (var customization in xmlConfiguration.Descendants("c"))
                {
                    // se il valore non è già stato inserito allora
                    if (!returnCustomizaions.ContainsKey(customization.Attribute("ID").Value))
                    {
                        var customizationParamList = new List<KeyValuePair<string, string>>();

                        // se la customizzazione ha dei descendants allora significa che sono presenti dei parametri che allora compilano la lista di key value pair
                        if (customization.Descendants("param").Any())
                            customization.Descendants("param").ForEach(node => customizationParamList.Add(new KeyValuePair<string, string>(node.Attribute("Name").Value, node.Attribute("Value").Value)));

                        returnCustomizaions.Add(customization.Attribute("ID").Value, customizationParamList);
                    }
                }
            }

            return returnCustomizaions;
        }


        #endregion

    }
}
