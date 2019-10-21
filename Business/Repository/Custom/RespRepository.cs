using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using Data;
using Domain;
using System.Text;
using Common;
using Business.MDBSchema;
using System.Linq.Expressions;
using log4net;

namespace Business.Repository.Custom
{
    public class RespRepository : GenericRepository<Resp>, IRespRepository
    {
        public RespRepository(PowerWebEntities context)
            : base(context)
        {
        }

        //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
        private static DateTime DataOraRecord;

        private static void ResetSession()
        {
        }

        public override Resp Init()
        {
            Resp oNewRecord = base.Init();
            oNewRecord.DisAbilitazione_Resp = false;
            oNewRecord.Data_Registrazione_Resp = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Resp = DateTime.UtcNow;
            return oNewRecord;
        }

        public override void SetEntityBeforeAddOrUpdate(Resp entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Resp;
            entity.DataOraUltimaModifica_Resp = DateTime.UtcNow;
        }

        public override Dictionary<string, string> Check(Resp entity, bool isNew = false, bool isResetSession = true)
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
                    DateTime DataOraRecordDb = RepoManager.RespRepo.SingleOrDefault(u => u.Resp_Id == entity.Resp_Id).DataOraUltimaModifica_Resp;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Resp),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_RESP));
                    }
                }
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                if (String.IsNullOrEmpty(entity.Codice_Resp))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Resp),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_RESP));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.RespRepo.SingleOrDefault(u => u.Codice_Resp == entity.Codice_Resp) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Resp),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.RespRepo.SingleOrDefault(u => u.Codice_Resp == entity.Codice_Resp && u.Resp_Id != entity.Resp_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Resp),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //
                if (String.IsNullOrEmpty(entity.Descrizione_Resp))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Descrizione_Resp),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_DESCRIZIONE_RESP));
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                // NESSUN CONTROLLO DI QUESTO TIPO
                //
                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Codice_Resp, "") != "")
                    if (entity.Codice_Resp.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Resp),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_RESP, PowerWebResources.VALORE_20));
                if (CommonService.Nz(entity.Descrizione_Resp, "") != "")
                    if (entity.Descrizione_Resp.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Descrizione_Resp),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DESCRIZIONE_RESP, PowerWebResources.VALORE_50));
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
                var CodErr = "Resp_Id: " + entity.Resp_Id;
                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Resp entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //verifico SOLO x IMPORT la validità delle eventuali Date Ricevute
            if (entity.Data_Registrazione_Resp < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Resp),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_RESP));
            if (entity.DataOraUltimaModifica_Resp < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Resp),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_RESP));
            //if (entity.Durata_Max_Gruppo_Notte_Ril_Can != null)
            //    if (entity.Durata_Max_Gruppo_Notte_Ril_Can > new TimeSpan(23, 59, 59))
            //        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Max_Gruppo_Notte_Ril_Can),
            //        CommonServiceBiz.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_ERRATO,
            //        PowerWebResources.FLD_DURATA_MAX_GRUPPO_NOTTE_RIL_CAN));
            return result;
        }

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Resp");
            Dictionary<string, string> oResultDictionary = new Dictionary<string, string>();
            List<Resp> toImport = new List<Resp>();

            #region Customizzazione import resp in tab decod

            // verifico la presenza della customizzazione riguardante l'importazione del campo responsabile cantiere
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.RespColDiCantInRaggruppamento1Enum);
            List<Tab_Decod> toImportDecods = new List<Tab_Decod>();

            #endregion

            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>();
            string lastKey = "";
            double nRec = 0;
            double countRec = 0;
            double percRec = 0;


            var accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData<PowerMDBDataSet.Tab_RespRow>(
          oDataSet.Tab_Resp.ToList(), "Resp", "Codice_Resp");

            nRec = accessData.Count;

            foreach (PowerMDBDataSet.Tab_RespRow oRow in accessData)
            {
                try
                {
                    lastKey = oRow.Codice_Resp;
                    countRec = countRec + 1;
                    percRec = (countRec / nRec) * 100;
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(),
                        "RESP", "2", "17", oRow.Codice_Resp.ToString(), countRec.ToString(), nRec.ToString()));

                    Resp oNewRecord = this.Init();
                    oNewRecord.Codice_Resp = oRow.Codice_Resp;
                    oNewRecord.Descrizione_Resp = oRow.IsDescrizione_RespNull() ? (string)null : oRow.Descrizione_Resp;
                    oNewRecord.DisAbilitazione_Resp = false;
                    // Eseguo i Controlli Specifici della CheckForImport e poi i Controlli Standard della Check        
                    Dictionary<string, string> importDictionary = CheckForImport(oNewRecord);

                    //Solo la prima volta chiamo la Check con ResetSession=True per fargli aggironare i Dati dal DB
                    if (countRec == 1)
                        currentDictionary = Check(oNewRecord, true, true);
                    else
                        currentDictionary = Check(oNewRecord, true, false);

                    #region Customizzazione per scrittura codice responsabile in Raggruppamento1 di tab_decod

                    // se è indicato di importare il responsabile del cantiere in raggruppamento 1 allora 
                    // si caricano i valori dei responsabili anche in tab_decod per la tabella Raggruppamento1
                    if (customizationVersion == (int)RespColDiCantInRaggruppamento1Enum.ImportInRaggruppamento1)
                    {
                        if (currentDictionary.Keys.Count == 0 && importDictionary.Keys.Count == 0)
                        {
                            if (!RepoManager.Tab_DecodRepo.Find(td => td.Chiave_Tab == oNewRecord.Codice_Resp && td.Nome_Tab == "RAGGRUPPAMENTO_1").Any())
                            {
                                Tab_Decod oNewTabDecod = new Tab_Decod()
                                {
                                    Gruppo_Tab = "DECOD_TAB",
                                    Nome_Tab = "RAGGRUPPAMENTO_1",
                                    Chiave_Tab = oNewRecord.Codice_Resp,
                                    Decodifica_Tab = oNewRecord.Descrizione_Resp
                                };

                                //Solo la prima volta chiamo la Check con ResetSession=True per fargli aggironare i Dati dal DB
                                if (countRec == 1)
                                    currentDictionary = RepoManager.Tab_DecodRepo.Check(oNewTabDecod, true, true);
                                else
                                    currentDictionary = RepoManager.Tab_DecodRepo.Check(oNewTabDecod, true, false);

                                if (currentDictionary.Keys.Count == 0 && importDictionary.Keys.Count == 0)
                                    toImportDecods.Add(oNewTabDecod);
                            }
                        }
                    }

                    #endregion

                    //Verifico se ci sono stati errori
                    if (currentDictionary.Keys.Count == 0 && importDictionary.Keys.Count == 0)
                        toImport.Add(oNewRecord);
                    else
                    {
                        errors.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Resp",
                            Chiave_Record_Tab_Check_Imp = oRow.Codice_Resp,
                        });
                        log.Warn("Codice Responsabile cui si rifericono gli errori precedenti: " + oRow.Codice_Resp + " -------------------------------------------------------------------------------");
                        errorsList.Add(currentDictionary);
                        errorsList.Add(importDictionary);
                    }

                }
                catch (Exception ex)
                {
                    var RRNERR = oRow.Codice_Resp;
                    //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                    if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                        Log.WarnFormat("TAB RESP - Chiave: {0}", oRow.Codice_Resp);
                    throw ex;
                }
            }


            RepoManager.Tab_Chk_ImpRepo.BeginWork();
            try
            {
                //CommonServiceBiz.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                //    CommonServiceBiz.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y, "RESP", lastKey));
                RepoManager.RespRepo.Add(toImport);

                #region Customizzazione per scrittura codice responsabile in Raggruppamento1 di tab_decod

                // se è indicato di importare il responsabile del cantiere in raggruppamento 1 allora 
                // si scrivono i valori calcolati sul database
                if (customizationVersion == (int)RespColDiCantInRaggruppamento1Enum.ImportInRaggruppamento1)
                {
                    if (toImportDecods.Any())
                        RepoManager.Tab_DecodRepo.Add(toImportDecods);
                }

                #endregion

                RepoManager.Tab_Chk_ImpRepo.Add(errors);
                if (errors.Count == 0)

                    RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                    {
                        Nome_Tabella_Tab_Check_Imp = "Resp",
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

            return errorsList;
        }

        public override Expression<Func<Resp, bool>> Filter
        {
            get
            {
                if (((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Resp) == DomainFilterEnum.Resp && PowerWebContext.Current.Resps != null && PowerWebContext.Current.User.Liv_Utente < 10))
                {
                    if (PowerWebContext.Current.Resps.Count > 0 && PowerWebContext.Current.User.Liv_Utente < 10)
                    {
                        var allRespIds = PowerWebContext.Current.Resps.Select(resp => resp.Resp_Id).ToList();
                        return resp => allRespIds.Contains(resp.Resp_Id);
                    }
                    else return base.Filter;
                }
                else return base.Filter;
            }
        }
    }
}
