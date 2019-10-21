using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using System.Text;
using Common;
using Business.MDBSchema;
using System.Data;
using System.Linq.Expressions;
using log4net;

namespace Business.Repository.Custom
{
    public class FilRepository : GenericRepository<Fil>, IFilRepository
    {
        public FilRepository(PowerWebEntities context)
            : base(context)
        {
        }

        //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
        private static DateTime DataOraRecord;

         private static void ResetSession()
        {            
        }

        public override Fil Init()
        {
            Fil oNewRecord = base.Init();
            oNewRecord.DisAbilitazione_Fil = false;
            oNewRecord.Data_Registrazione_Fil = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Fil = DateTime.UtcNow;
            return oNewRecord;
        }

        public override void SetEntityBeforeAddOrUpdate(Fil entity)
        {
             //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Fil;
            entity.DataOraUltimaModifica_Fil = DateTime.UtcNow;
        }

        public override Dictionary<string, string> Check(Fil entity, bool isNew = false, bool isResetSession = true)
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
                    DateTime DataOraRecordDb = RepoManager.FilRepo.Single(u => u.Fil_Id == entity.Fil_Id).DataOraUltimaModifica_Fil;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Fil),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_FIL));
                    }
                }

                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                if (String.IsNullOrEmpty(entity.Codice_Fil))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fil),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_FIL));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.FilRepo.SingleOrDefault(u => u.Codice_Fil == entity.Codice_Fil) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fil),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.FilRepo.SingleOrDefault(u => u.Codice_Fil == entity.Codice_Fil && u.Fil_Id != entity.Fil_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fil),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //  
                if (String.IsNullOrEmpty(entity.Descrizione_Fil))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Descrizione_Fil),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_DESCRIZIONE_FIL));
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                // NESSUN CONTROLLO DI QUESTO TIPO
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                // NESSUN CONTROLLO DI QUESTO TIPO
                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Codice_Fil, "") != "")
                    if (entity.Codice_Fil.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fil),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_FILIALE, PowerWebResources.VALORE_20 ));
                if (CommonService.Nz(entity.Descrizione_Fil, "") != "")
                    if (entity.Descrizione_Fil.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Descrizione_Fil),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DESCRIZIONE_FIL, PowerWebResources.VALORE_50 ));
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
                var CodErr = "Fil_Id= " + entity.Fil_Id;
                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Fil entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //verifico SOLO x IMPORT la validità delle eventuali Date Ricevute
            if (entity.Data_Registrazione_Fil < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Fil),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_FIL));
            if (entity.DataOraUltimaModifica_Fil < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Fil),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_FIL));
                        
            WriteCheckLog(entity, result, Log);
            return result;
        }

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Fil");
            Dictionary<string, string> oResultDictionary = new Dictionary<string, string>();
            List<PowerMDBDataSet.Tab_FilialiRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                    <PowerMDBDataSet.Tab_FilialiRow>(oDataSet.Tab_Filiali.ToList(), "Tab_Filiali", "Codice_Filiale").OrderBy(acd => acd.Codice_Filiale).ToList();
            List<Fil> toImport = new List<Fil>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>();
            string lastKey = "";
            if (accessData != null)
            {
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                foreach (PowerMDBDataSet.Tab_FilialiRow oRow in accessData)
                {
                    try
                    {
                        lastKey = oRow.Codice_Filiale;
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;                        
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                            BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "FIL",
                            "1","17", oRow.Codice_Filiale.ToString(), countRec.ToString(), nRec.ToString()));
                        Fil oNewRecord = this.Init();
                        oNewRecord.Codice_Fil = oRow.IsCodice_FilialeNull() ? (string)null : oRow.Codice_Filiale;
                        oNewRecord.Descrizione_Fil = oRow.IsDescrizione_FilialeNull() ? (string)null : oRow.Descrizione_Filiale;
                        oNewRecord.DisAbilitazione_Fil = false;
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
                                Nome_Tabella_Tab_Check_Imp = "Fil",
                                Chiave_Record_Tab_Check_Imp = oRow.Codice_Filiale,
                            });
                            log.Warn("Codice Filiale cui si rifericono gli errori precedenti: " + oRow.Codice_Filiale + " -------------------------------------------------------------------------------");
                            errorsList.Add(currentDictionary);
                            errorsList.Add(importDictionary);
                        }
                    }
                    catch (Exception ex)
                    {
                        var RRNERR = oRow.Codice_Filiale;
                        //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                        if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                            Log.ErrorFormat("TAB FIL - Chiave: {0}", oRow.Codice_Filiale);
                        throw ex;
                    }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                try
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "FIL", lastKey));
                    RepoManager.FilRepo.Add(toImport);
                    RepoManager.Tab_Chk_ImpRepo.Add(errors);
                    if (errors.Count == 0)
                        RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Fil",
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

        public override Expression<Func<Fil, bool>> Filter
        {
            get
            {
                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Fil) == DomainFilterEnum.Fil && PowerWebContext.Current.Fils != null && PowerWebContext.Current.User.Liv_Utente < 10)
                {
                    var allFillIds = PowerWebContext.Current.Fils.Select(fil => fil.Fil_Id).ToList();
                    return fil => allFillIds.Contains(fil.Fil_Id);
                }
                else return base.Filter;
            }
        }
    }
}

