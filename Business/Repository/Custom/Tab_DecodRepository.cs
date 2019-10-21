using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;
using Business.MDBSchema;
using System.Data.OleDb;
using Business.MDBSchema.PowerMDBDataSetTableAdapters;

namespace Business.Repository.Custom
{
    #region Definizioni di ENUM
    public enum TabDecodColumnsEnum
    {
        Nome_Tab,
        Chiave_Tab,
        Decodifica_Tab,
    }

    public enum TabDecodGroupTypeEnum
    {
        DECOD_SYS,
        DECOD_TAB,
        DECOD_TITLE,
    }

    public enum TabDecodNameEnum
    {   
        CTRL_CODICE_FISC,
        CTRL_CODICE_IBAN,
        CTRL_TAB_COMUNI,
        DFLT_GRID_EDIT_MODE,
        DFLT_ONOFFBTNVISIBLE,
        DOMINIO_UTENTI_FIL,
        DOMINIO_UTENTI_RESP,
        DOMINIOVISUALIZZAZIONE,
        DOMINIOMANUTENZIONE,
        FASCE_VIAGGI,
        FLAG_ENTRATA,
        FLAG_EU_REG,        
        FLAG_GPS_PARAMETRI,
        FLAG_INPS,        
        FLAG_MOD_REG,
        FLAG_MONTE_ORE,
        FLAG_NON_ESPORTARE,
        FORM_SELEZ,
        FORM_SORT,
        LINGUA_CLI,
        LIVELLO_CAN,
        LIVELLO_COL,
        METODO_ARROTONDAMENTO,
        MODULETYPE,
        MOTIVAZIONI,
        NAZIONALITA,
        QUALIFICHE_COL,
        RAGGRUPPAMENTO_1,
        RAGGRUPPAMENTO_2,
        SESSO,
        SIGLA_NAZIONI,
        STATO_CIVILE,
        STATO_REGISTRAZIONE,
        TIPO_ARROTONDAMENTO,
        TIPO_ASSEGNAZIONE_KMMINUTI,        
        TIPO_CALCOLO_VIAGGI,
        TIPO_CALCOLO_VIAGGI_CAN,
        TIPO_CALCOLO_VIAGGI_COL,
        TIPO_CAN,
        TIPO_COL,
        TIPO_CONTRATTO_COL,
        TIPO_DISTANZA,
        TIPO_FUNZIONE,
        TIPO_INTERVENTO,
        TIPO_MESSAGGIO,
        TIPO_MODIFICA_REG,
        TIPO_NOTA,
        TIPO_NOTTURNO,
        TIPO_ORARIO,
        TIPO_PAGAMENTO,
        TIPO_PATENTE,
        TIPO_RAPPORTO_COL,
        TIPO_REGISTRAZIONE,
        TIPO_SERVIZIO_CAN,
        TIPO_VIAGGIO,
        TIPOLOGIA_CANTIERE,
        TITOLO_STUDIO,
        VALUTE,
        ZONE,
    }

    public enum TabDecodOldFunzioneEnum
    {
        SC_COL,
        DECOD_TAB,
        SC_CAN,
        DECOD_RIL,
    }

    public enum TabDecodOldCampoEnum
    {
        LIVELLO_COL,
        QUALIFICA_COL,
        TIPO_CANTIERE_CAN,
        TIPO_COL,
        TIPO_NOTA,
        TIPO_RAPPORTO_COL,
        TIPO_RILEVAZIONE,
        TIPOCONTRATTO,
    }

    public enum AzioneDaEseguireEnum
    {
        SostituireRecord,
        AggiungereRecord,
    }
    #endregion

    public class TabDecodImportInfo
    {
        public TabDecodOldFunzioneEnum FunzioneTabellaOrigine { get; set; }
        public TabDecodOldCampoEnum CampoTabellaOrigine { get; set; }
        public TabDecodGroupTypeEnum Gruppo_TabTabellaDestinazione { get; set; }
        public TabDecodNameEnum Nome_TabTabellaDestinazione { get; set; }
        public AzioneDaEseguireEnum AzioneDaEseguire { get; set; }
    }

    
    public class Tab_DecodRepository : GenericRepository<Tab_Decod>, ITab_DecodRepository
    {

        private static void ResetSession()
        {
        }

        public Tab_DecodRepository(PowerWebEntities context)
            : base(context)
        {
        }

        public Tab_Decod SearchKeyInTable(string group, string tabName, string key)
        {
            return SingleOrDefault(td => td.Gruppo_Tab == group && td.Nome_Tab == tabName && td.Chiave_Tab == key);
        }

        public Tab_Decod SearchDecodifyInTable(string group, string tabName, string decodify)
        {
            return SingleOrDefault(td => td.Gruppo_Tab == group && td.Nome_Tab == tabName && td.Decodifica_Tab == decodify);
        }

        public bool ExistParametrized(string group, string tabName, string key)
        {
            return SingleOrDefault(td => td.Gruppo_Tab == group && td.Nome_Tab == tabName && td.Chiave_Tab == key) != null;
        }

        public IQueryable<Tab_Decod> GetAllParametrized(string group, string tabName)
        {
            return Find(td => td.Gruppo_Tab == group && td.Nome_Tab == tabName).AsQueryable();
        }

        public List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, OleDbConnection myAccessConn, bool onlyErrors = false)
        {
            List<Dictionary<string, string>> errors = new List<Dictionary<string, string>>();
            Tab_Decod oNewRecord = null;
            List<Tab_Decod> oEntityList = new List<Tab_Decod>();
            IEnumerable<PowerMDBDataSet.Tab_DecodRow> oFiltratiTabellaOrigine = null;
            List<Tab_Decod> oArrayFiltratiTabellaDestinazione = null;//array per eseguire le ricerche più velocemente
            IEnumerable<Tab_Decod> oTabellaDestinazioneCompleta = GetAll();
            List<string> oLivelloAssistitoList = new List<string>();

            //creo un array per stabilire quali gruppi della vecchia Tab_Decod vanno importati      
            TabDecodImportInfo[] oTabDecodImportInfo = new TabDecodImportInfo[]
      {
        new TabDecodImportInfo()
        {
          FunzioneTabellaOrigine=TabDecodOldFunzioneEnum.SC_CAN,
          CampoTabellaOrigine=TabDecodOldCampoEnum.TIPO_CANTIERE_CAN,
          Gruppo_TabTabellaDestinazione=TabDecodGroupTypeEnum.DECOD_TAB,
          Nome_TabTabellaDestinazione=TabDecodNameEnum.TIPO_CAN,
          AzioneDaEseguire=AzioneDaEseguireEnum.AggiungereRecord
        },
        new TabDecodImportInfo() 
        {
          FunzioneTabellaOrigine=TabDecodOldFunzioneEnum.DECOD_TAB,
          CampoTabellaOrigine=TabDecodOldCampoEnum.TIPO_NOTA,
          Gruppo_TabTabellaDestinazione=TabDecodGroupTypeEnum.DECOD_TAB,
          Nome_TabTabellaDestinazione=TabDecodNameEnum.TIPO_NOTA,
          AzioneDaEseguire=AzioneDaEseguireEnum.AggiungereRecord
        },
        new TabDecodImportInfo() 
        {
          FunzioneTabellaOrigine=TabDecodOldFunzioneEnum.SC_COL,
          CampoTabellaOrigine=TabDecodOldCampoEnum.LIVELLO_COL,
          Gruppo_TabTabellaDestinazione=TabDecodGroupTypeEnum.DECOD_TAB,
          Nome_TabTabellaDestinazione=TabDecodNameEnum.LIVELLO_COL,
          AzioneDaEseguire=AzioneDaEseguireEnum.SostituireRecord
        },
        new TabDecodImportInfo() 
        {
          FunzioneTabellaOrigine=TabDecodOldFunzioneEnum.SC_COL,
          CampoTabellaOrigine=TabDecodOldCampoEnum.QUALIFICA_COL,
          Gruppo_TabTabellaDestinazione=TabDecodGroupTypeEnum.DECOD_TAB,
          Nome_TabTabellaDestinazione=TabDecodNameEnum.QUALIFICHE_COL,
          AzioneDaEseguire=AzioneDaEseguireEnum.SostituireRecord
        },
        new TabDecodImportInfo() 
        {
          FunzioneTabellaOrigine=TabDecodOldFunzioneEnum.SC_COL,
          CampoTabellaOrigine=TabDecodOldCampoEnum.TIPO_COL,
          Gruppo_TabTabellaDestinazione=TabDecodGroupTypeEnum.DECOD_TAB,
          Nome_TabTabellaDestinazione=TabDecodNameEnum.TIPO_COL,
          AzioneDaEseguire=AzioneDaEseguireEnum.SostituireRecord
        },
        new TabDecodImportInfo() 
        {
          FunzioneTabellaOrigine=TabDecodOldFunzioneEnum.SC_COL,
          CampoTabellaOrigine=TabDecodOldCampoEnum.TIPO_RAPPORTO_COL,
          Gruppo_TabTabellaDestinazione=TabDecodGroupTypeEnum.DECOD_TAB,
          Nome_TabTabellaDestinazione=TabDecodNameEnum.TIPO_RAPPORTO_COL,
          AzioneDaEseguire=AzioneDaEseguireEnum.SostituireRecord
        },
        new TabDecodImportInfo() 
        {
          FunzioneTabellaOrigine=TabDecodOldFunzioneEnum.DECOD_RIL,
          CampoTabellaOrigine=TabDecodOldCampoEnum.TIPO_RILEVAZIONE,
          Gruppo_TabTabellaDestinazione=TabDecodGroupTypeEnum.DECOD_TAB,
          Nome_TabTabellaDestinazione=TabDecodNameEnum.MOTIVAZIONI,
          AzioneDaEseguire=AzioneDaEseguireEnum.AggiungereRecord
        },
        new TabDecodImportInfo()
        {
          FunzioneTabellaOrigine=TabDecodOldFunzioneEnum.SC_COL,
          CampoTabellaOrigine=TabDecodOldCampoEnum.TIPOCONTRATTO,
          Gruppo_TabTabellaDestinazione=TabDecodGroupTypeEnum.DECOD_TAB,
          Nome_TabTabellaDestinazione=TabDecodNameEnum.TIPO_CONTRATTO_COL,
          AzioneDaEseguire=AzioneDaEseguireEnum.AggiungereRecord
        }
      };
            //importo nella Tab_Decod i record della tabella LIVELLO ASSISTITO presenti nella Tanella e/o in Cant e/o CANT_VAR che non sono già presenti nella tabella
            try
            {
                using (CantTableAdapter tableAdapter = new CantTableAdapter())
                {
                    tableAdapter.Connection = myAccessConn;
                    tableAdapter.Fill(oDataSet.Cant);
                    foreach (PowerMDBDataSet.CantRow oRow in oDataSet.Cant.Where(x => !x.IsLivello_Assistito_CanNull())
                    .GroupBy(x => x.Livello_Assistito_Can).Select(x => x.First()).ToList())
                        oLivelloAssistitoList.Add(oRow.Livello_Assistito_Can);
                }
                using (Cant_VarTableAdapter tableAdapter = new Cant_VarTableAdapter())
                {
                    tableAdapter.Connection = myAccessConn;
                    tableAdapter.Fill(oDataSet.Cant_Var);
                    foreach (PowerMDBDataSet.Cant_VarRow oRow in oDataSet.Cant_Var.Where(x => !x.IsLivello_Assistito_CanNull())
                  .GroupBy(x => x.Livello_Assistito_Can).Select(x => x.First()).ToList())
                        oLivelloAssistitoList.Add(oRow.Livello_Assistito_Can);
                }
                oLivelloAssistitoList = oLivelloAssistitoList.Distinct().ToList();
                oArrayFiltratiTabellaDestinazione = oTabellaDestinazioneCompleta.Where(x =>
                  x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                  && x.Nome_Tab == TabDecodNameEnum.LIVELLO_CAN.ToString()).ToList();
                foreach (string sLivelloAssistito in oLivelloAssistitoList)
                {
                    if (oArrayFiltratiTabellaDestinazione.FirstOrDefault(x => x.Chiave_Tab == sLivelloAssistito) == null)
                    {
                        oNewRecord = this.Init();
                        oNewRecord.Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString();
                        oNewRecord.Nome_Tab = TabDecodNameEnum.LIVELLO_CAN.ToString();
                        oNewRecord.Chiave_Tab = sLivelloAssistito;
                        oNewRecord.Decodifica_Tab = sLivelloAssistito;
                        oEntityList.Add(oNewRecord);
                    }
                }
            }
            catch
            {
                throw new ArgumentNullException("TAB_DECOD - LIVELLO ASSISTITI");
            }
            try
            {
                //importo nella Tab_Decod i record della Tabella NAZIONALITA' che non sono già presenti nella tabella
                oArrayFiltratiTabellaDestinazione = oTabellaDestinazioneCompleta.Where(x =>
                  x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                  && x.Nome_Tab == TabDecodNameEnum.NAZIONALITA.ToString()).ToList();
                using (Tab_NazionalitàTableAdapter tableAdapter = new Tab_NazionalitàTableAdapter())
                {
                    tableAdapter.Connection = myAccessConn;
                    tableAdapter.Fill(oDataSet.Tab_Nazionalità);
                    foreach (PowerMDBDataSet.Tab_NazionalitàRow oRow in oDataSet.Tab_Nazionalità.Rows)
                    {
                        if (oArrayFiltratiTabellaDestinazione.FirstOrDefault(x => x.Chiave_Tab.ToUpper() == oRow.Sigla_Nazionalità.ToUpper()) == null)
                        {
                            oNewRecord = this.Init();
                            oNewRecord.Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString();
                            oNewRecord.Nome_Tab = TabDecodNameEnum.NAZIONALITA.ToString();
                            oNewRecord.Chiave_Tab = oRow.IsSigla_NazionalitàNull() ? (string)null : oRow.Sigla_Nazionalità;
                            oNewRecord.Decodifica_Tab = oRow.IsDescrizione_NazionalitàNull() ? (string)null : oRow.Descrizione_Nazionalità;
                            oEntityList.Add(oNewRecord);
                        }
                    }
                }
            }
            catch
            {
                throw new ArgumentNullException("TAB_DECOD - NAZIONALITA'");
            }

            //importo nella Tab_PROV i record della Tabella PROVINCIE che non sono già presenti nella tabella      
            try
            {
                using (Tab_ProvTableAdapter tableAdapter = new Tab_ProvTableAdapter())
                {
                    tableAdapter.Connection = myAccessConn;
                    Tab_Prov oNewTab_Prov = new Tab_Prov();
                    tableAdapter.Fill(oDataSet.Tab_Prov);
                    foreach (PowerMDBDataSet.Tab_ProvRow oRow in oDataSet.Tab_Prov.Rows)
                    {
                        if (RepoManager.Tab_ProvRepo.FirstOrDefault(x => x.Sigla_Prov == oRow.Sigla_Provincia) == null)
                        {
                            oNewTab_Prov.Sigla_Prov = oRow.IsSigla_ProvinciaNull() ? (string)null : oRow.Sigla_Provincia;
                            oNewTab_Prov.Descrizione_Prov = oRow.IsDescrizione_ProvinciaNull() ? (string)null : oRow.Descrizione_Provincia;
                            RepoManager.Tab_ProvRepo.Add(oNewTab_Prov);
                            RepoManager.Tab_ProvRepo.SaveChanges();
                        }
                    }
                }
            }
            catch
            {
                throw new ArgumentNullException("TAB_DECOD - PROVINCE'");
            }

            // Importo nella Tab_Decod tutte le Tabellle che ho caricato nell'array TabDecodImportIn
            try
            {
                foreach (TabDecodImportInfo o1 in oTabDecodImportInfo)
                {
                    oFiltratiTabellaOrigine = oDataSet.Tab_Decod.Where(x =>
                      x.Funzione.ToUpper() == o1.FunzioneTabellaOrigine.ToString().ToUpper()
                      && x.Campo.ToUpper() == o1.CampoTabellaOrigine.ToString().ToUpper());
                    if (oFiltratiTabellaOrigine.Count() == 0)
                        continue;
                    oArrayFiltratiTabellaDestinazione = oTabellaDestinazioneCompleta.Where(x =>
                      x.Gruppo_Tab == o1.Gruppo_TabTabellaDestinazione.ToString()
                      && x.Nome_Tab == o1.Nome_TabTabellaDestinazione.ToString()).ToList();
                    if (o1.AzioneDaEseguire == AzioneDaEseguireEnum.SostituireRecord)
                        Delete(oArrayFiltratiTabellaDestinazione);//se devo sostituire i record cancello i record filtrati perché li reimporto da zero
                    foreach (PowerMDBDataSet.Tab_DecodRow oRow in oFiltratiTabellaOrigine)
                    {
                        if (o1.AzioneDaEseguire == AzioneDaEseguireEnum.AggiungereRecord
                        && oArrayFiltratiTabellaDestinazione.Where(x => x.Chiave_Tab == oRow.Chiave).Count() > 0)
                            continue;//se devo aggiungere i record non inserisco in tabella quelli già esistenti
                        oNewRecord = this.Init();
                        oNewRecord.Gruppo_Tab = o1.Gruppo_TabTabellaDestinazione.ToString();
                        oNewRecord.Nome_Tab = o1.Nome_TabTabellaDestinazione.ToString();
                        oNewRecord.Chiave_Tab = oRow.Chiave;
                        oNewRecord.Decodifica_Tab = oRow.IsDecodificaNull() ? (string)null : oRow.Decodifica;
                        oEntityList.Add(oNewRecord);
                    }
                }
            }
            catch
            {
                throw new ArgumentNullException("TAB_DECOD");
            }
            Add(oEntityList, true);

            return errors;
        }

        public override Dictionary<string, string> Check(Tab_Decod entity, bool isNew = false, bool isResetSession = true)
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
                // NOTA IL CONTROLLO DI CHIAVE UNIVOCA VIENE FATTO A PRESCINDERE DAL GRUPPO_TAB a cui appartieme
                //ovvero la Tabella NON DEVE ESISTERE CON QUEL NOME a PRESCINDERE DAL GRUPPO (DECOD_TAB e/o DECOD_SYS) cui appatiene
                //
                if (String.IsNullOrEmpty(entity.Nome_Tab) && String.IsNullOrEmpty(entity.Chiave_Tab))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Decod_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.STR_TABELLA_DECODIFICHE));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.Tab_DecodRepo.SingleOrDefault(u => u.Nome_Tab == entity.Nome_Tab && u.Chiave_Tab == entity.Chiave_Tab) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Decod_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));                        
                    }
                    else
                    {
                        if (RepoManager.Tab_DecodRepo.SingleOrDefault(u => u.Nome_Tab == entity.Nome_Tab &&
                            u.Chiave_Tab == entity.Chiave_Tab && u.Tab_Decod_Id != entity.Tab_Decod_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Decod_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }               

                //
                //Nel caso NON sia un Record di TITLE, verifico anche che SIA PRESENTE il REcord di TITLE relativo alla Tabella
                //  
                if (!String.IsNullOrEmpty(entity.Gruppo_Tab))
                    if (entity.Gruppo_Tab != "DECOD_TITLE")
                        if (RepoManager.Tab_DecodRepo.SingleOrDefault(u => u.Gruppo_Tab == "DECOD_TITLE" &&
                              u.Nome_Tab == entity.Gruppo_Tab && u.Chiave_Tab == entity.Nome_Tab) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Decod_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_MANCA_RECORD_TITLE_IN_TAB_DECOD));
               
                // I Record con Gruppo_TAB = TAB_SYS e /o TAB_TITLE che possono essere modificate SOLO DA UTENTI CON LIV >=12
                //
                if ((entity.Gruppo_Tab == "DECOD_TITLE" || entity.Gruppo_Tab == "DECOD_SYS") &&
                   PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Gruppo_Tab),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_UTENTE_NON_AUTORIZZATO_GRUPPO_TAB));
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //

                if (String.IsNullOrEmpty(entity.Gruppo_Tab))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Gruppo_Tab),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_GRUPPO_TAB));
                if (String.IsNullOrEmpty(entity.Nome_Tab))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Tab),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOME_TAB));
                if (String.IsNullOrEmpty(entity.Chiave_Tab))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Chiave_Tab),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CHIAVE_TAB));
                if (String.IsNullOrEmpty(entity.Decodifica_Tab))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Decodifica_Tab),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_DECODIFICA_TAB));

                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                if (CommonService.Nz(entity.Gruppo_Tab, "") != "")
                    if (CommonService.Nz(entity.Gruppo_Tab, "") != "DECOD_TAB" &&
                       (CommonService.Nz(entity.Gruppo_Tab, "") != "DECOD_SYS") &&
                       (CommonService.Nz(entity.Gruppo_Tab, "") != "DECOD_TITLE"))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Decod_Id),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_NOMI_GRUPPI_TAB_DECOD_NON_VALIDI));
                //Imposto SEMPRE MAIUSCOLO la Tipologia_CAN
                if (!String.IsNullOrEmpty(entity.Nome_Tab))
                { 
                    if (entity.Nome_Tab == "TIPOLOGIA_CAN")
                    {
                        if (!String.IsNullOrEmpty(entity.Chiave_Tab))
                        {
                            entity.Chiave_Tab = entity.Chiave_Tab.ToUpper();
                        }
                    }
                    else if (entity.Nome_Tab == "QUALIFICHE_COL")
                    { 
                        // Il campo1 della tabella Qualifiche_col deve essere numerico
                        if (!String.IsNullOrEmpty(entity.Chiave_Tab))
                        {
                            if (!String.IsNullOrEmpty(entity.Campo1_Tab))
                            {
                                //Prova a convertire il campo in un decimal
                                try
                                {
                                    Convert.ToDecimal(entity.Campo1_Tab);
                                }
                                //Se non ci riesce, mostrqa il messaggio d'errore
                                catch (FormatException)
                                {
                                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Campo1_Tab),
                                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DECODIFICA_TAB));
                                }
                            }
                        }
                    }
                }


                //
                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Gruppo_Tab, "") != "")
                    if (entity.Gruppo_Tab.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Gruppo_Tab),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_GRUPPO_TAB, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Nome_Tab, "") != "")
                    if (entity.Nome_Tab.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Tab),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NOME_TAB, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Chiave_Tab, "") != "")
                    if (entity.Chiave_Tab.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Chiave_Tab),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CHIAVE_TAB, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Decodifica_Tab, "") != "")
                    if (entity.Decodifica_Tab.Length > 255)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Decodifica_Tab),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DECODIFICA_TAB, PowerWebResources.VALORE_255 ));
                //
                //5) verifico, per una serie di campi, che il valore del campo sia presente nella relativa Tabella
                //
                //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);
            }
            catch (Exception ex)
            {
                var CodErr = "Tab_Decod_Id: " + entity.Tab_Decod_Id ;
                throw ex;
            }
            return result;
        }
    }
}
