using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Domain;
using System.Text;
using Common;
using Business.MDBSchema;
using log4net;
using System.Linq.Expressions;
using System.Data.Entity.Infrastructure;
using System.Data.Entity;
using Business.ClockAppManagerSynchronizationUtilities;
using Business.IocFactory.ClockAppSynchronizationFactory;

namespace Business.Repository.Custom
{
    public class Fru_CantRepository : GenericRepository<Fru_Cant>,  IFru_CantRepository
    {

        public SynchronizationCollector<Fru_Cant> SynchronizationHub { get; set; }

        public Fru_CantRepository(PowerWebEntities context)
            : base(context)
        {
        }

        //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
        private static DateTime DataOraRecord;

        /// <summary>Ottiene le tabelle che deve usare in formato lista.
        /// Per ottimizzare la velocità salva l'oggetto nella sessione corrente
        /// in modo che la lista sia già in memoria quando viene richiesta più volte.
        /// </summary>
        private static List<Cant> Cants
        {
            get
            {
                List<Cant> oLista = PowerWebContext.GetFromSession<List<Cant>>("Cants_Fru_CantRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.CantRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cant>>("Cants_Fru_CantRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Fru> Frus
        {
            get
            {
                List<Fru> oLista = PowerWebContext.GetFromSession<List<Fru>>("Frus_Fru_CantRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.FruRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Fru>>("Frus_Fru_CantRepo", oLista);
                }
                return oLista;
            }
        }

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Tab_Decod>>("Cants_Fru_CantRepo", null);
            PowerWebContext.SetToSession<List<Tab_Decod>>("Frus_Fru_CantRepo", null);
        }

        public override Fru_Cant Init()
        {
            Fru_Cant oNewRecord = base.Init();
            oNewRecord.DisAbilitazione_Fru_Can = false;
            oNewRecord.Data_Registrazione_Fru_Can = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Fru_Can = DateTime.UtcNow;
            return oNewRecord;
        }

        private void elaborateFruChanges(int fru_Id, DateTime newStartDate, bool isForUpdate = false, int entityId = -1)
        {
            if (isForUpdate)
            {
                var oldFruCant = RepoManager.Fru_CantRepo.Single(fruCant => fruCant.Fru_Cant_Id == entityId, true);

                elaborateFruChanges(fru_Id, oldFruCant.Abilitazione_Data_Inizio_Fru_Can);
            }

            newStartDate = newStartDate.Date;

            var toDates = RepoManager.Fru_CantRepo.Find(frucant => frucant.Fru_Id == fru_Id && frucant.Abilitazione_Data_Inizio_Fru_Can > newStartDate, true).ToList();

            var nextDate = DateTime.MaxValue;

            if (toDates.Count > 0)
            {
                var nextFruCant = toDates.OrderBy(frucant => frucant.Abilitazione_Data_Inizio_Fru_Can).First();

                nextDate = nextFruCant.Abilitazione_Data_Inizio_Fru_Can;
            }

            RepoManager.PendingElabRepo.Add(new PendingElab { FromDate_PendingElab = newStartDate, ToDate_PendingElab = nextDate.Date, Fru_Id = fru_Id }, true);
        }

        public override void Add(Fru_Cant entity, bool saveChanges = false)
        {
            base.Add(entity);

            SaveChanges();

            elaborateFruChanges(entity.Fru_Id, entity.Abilitazione_Data_Inizio_Fru_Can);
        }

        public override void Delete(Fru_Cant entity, bool saveChanges = false)
        {
            base.Delete(entity, saveChanges);

            elaborateFruChanges(entity.Fru_Id, entity.Abilitazione_Data_Inizio_Fru_Can);
        }

        public override void Update(Fru_Cant entity, bool saveChanges = false)
        {
            elaborateFruChanges(entity.Fru_Id, entity.Abilitazione_Data_Inizio_Fru_Can, true, entity.Fru_Cant_Id);

            base.Update(entity, saveChanges);
        }

        public override int SaveChanges()
        {

            int savedEntities = 0;
            try
            {
                savedEntities = base.SaveChanges();
                
            }

            catch (Exception ex)
            {

            }
            return savedEntities;
        }
        public override void SetEntityBeforeAddOrUpdate(Fru_Cant entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Fru_Can;
            entity.DataOraUltimaModifica_Fru_Can = DateTime.UtcNow;
        }

        public override Dictionary<string, string> Check(Fru_Cant entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession();
            try
            {

                var filterDate = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg;
                if (filterDate.HasValue)
                {

                    if (CommonService.Nz(entity.Abilitazione_Data_Inizio_Fru_Can, new DateTime(1, 1, 1)) <= filterDate)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilitazione_Data_Inizio_Fru_Can),
                       BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_REG_MINORE_DI_DATA_BLOCCO));
                }

                if (!isNew)
                {
                    //Leggo la DataOraUltimaModifica ATTUALE dal Record del DB per verificare che nessuno abbia modificato il Record nel frattempo
                    DateTime DataOraRecordDb = RepoManager.Fru_CantRepo.Single(u => u.Fru_Cant_Id == entity.Fru_Cant_Id).DataOraUltimaModifica_Fru_Can;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Fru_Can),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_FRU_CAN));
                    }
                }
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                //  CHAIVE UNIVOCA : CANT_ID + FRU_ID + DATA ABILITAZIONE_DATA_INIZIO
                // 
                if (CommonService.Nz(entity.Cant_Id, 0) == 0 ||
                    CommonService.Nz(entity.Fru_Id, 0) == 0 ||
                    CommonService.Nz(entity.Abilitazione_Data_Inizio_Fru_Can, new DateTime(1, 1, 1)) == new DateTime(1, 1, 1))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fru_Cant_Id),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_DATI_NECESSARI_MANCANTI));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.Fru_CantRepo.SingleOrDefault(u => u.Cant_Id == entity.Cant_Id &&
                           u.Fru_Id == entity.Fru_Id &&
                           u.Abilitazione_Data_Inizio_Fru_Can == entity.Abilitazione_Data_Inizio_Fru_Can) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fru_Cant_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.Fru_CantRepo.SingleOrDefault(u => u.Cant_Id == entity.Cant_Id &&
                          u.Fru_Id == entity.Fru_Id &&
                          u.Abilitazione_Data_Inizio_Fru_Can == entity.Abilitazione_Data_Inizio_Fru_Can &&
                          u.Fru_Cant_Id != entity.Fru_Cant_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fru_Cant_Id),
                               BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }

                // in ogni caso non è possibile avere nella stessa data e per la stessa fru due record
                bool isPresentInSameDate = Find(fruCant => fruCant.Abilitazione_Data_Inizio_Fru_Can == entity.Abilitazione_Data_Inizio_Fru_Can && fruCant.Fru_Id == entity.Fru_Id && entity.Fru_Cant_Id != fruCant.Fru_Cant_Id).Any();

                if (isPresentInSameDate)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Registrazione_Fru_Can), BusinessService.GetLocalizedString(PowerWebResources.ERR_FRU_GIA_ASS_IN_DATA));

                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //
                if (entity.Abilitazione_Data_Inizio_Fru_Can == DateTime.MinValue)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilitazione_Data_Inizio_Fru_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_ABILITAZIONE_DATA_INIZIO_PRU_COL));
                if (entity.Cant_Id == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_CANT_ID));
                else
                {
                    if (RepoManager.CantRepo.SingleOrDefault(u => u.Cant_Id == entity.Cant_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_CANT_ID, PowerWebResources.STR_CANTIERI));
                }
                if (entity.Fru_Id == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fru_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_FRU_ID));
                else
                {
                    if (RepoManager.FruRepo.SingleOrDefault(u => u.Fru_Id == entity.Fru_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fru_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_FRU_ID, PowerWebResources.STR_UNITA_FISSE));
                }
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                // AGGIUNGERE CONTROLLI MANCANTI
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
                var CodErr = "Cant_Id: " + entity.Cant_Id + " - Fru_Id:" + entity.Fru_Id;
                throw ex;

            }
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Fru_Cant entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //Verifico SOLO x IMPORT la Validità delle eventuali Date Ricevute
            if (entity.Data_Registrazione_Fru_Can < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Fru_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_FRU_CAN));
            if (entity.DataOraUltimaModifica_Fru_Can < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Fru_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_FRU_CAN));
            if (CommonService.Nz(entity.Abilitazione_Data_Inizio_Fru_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilitazione_Data_Inizio_Fru_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_ABILITAZIONE_DATA_INIZIO_FRU_CAN));
            WriteCheckLog(entity, result, Log);
            return result;
        }

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Fru_Cant");
            Dictionary<string, string> oResultDictionary = new Dictionary<string, string>();
            List<PowerMDBDataSet.Fru_CantRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                <PowerMDBDataSet.Fru_CantRow>(oDataSet.Fru_Cant.ToList(), "Fru_Cant", "RRN").OrderBy(acd => acd.RRN).ToList();
            List<Fru_Cant> toImport = new List<Fru_Cant>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>();
            string lastKey = "";
            if (accessData != null)
            {
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                foreach (PowerMDBDataSet.Fru_CantRow oRow in accessData)
                {
                    try
                    {
                        lastKey = oRow.RRN.ToString();
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                           BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "FRU_CANT",
                           "15", "17", oRow.RRN.ToString(), countRec.ToString(), nRec.ToString()));
                        Fru_Cant oNewRecord = this.Init();
                        Cant oCant = RepoManager.CantRepo.FirstOrDefault(c => c.Codice_Cantiere.Trim() == oRow.Codice_Cantiere_FRU_CAN.Trim());
                        if (oCant != null)
                            oNewRecord.Cant_Id = oCant.Cant_Id;
                        Fru oFru = RepoManager.FruRepo.FirstOrDefault(f => f.Codice_Fru == oRow.Codice_FRU_CAN);
                        if (oFru != null)
                            oNewRecord.Fru_Id = oFru.Fru_Id;
                        oNewRecord.Abilitazione_Data_Inizio_Fru_Can = oRow.IsAbilitazione_Data_Inizio_FRU_CANNull()
                          ? new DateTime(2000, 1, 1) : oRow.Abilitazione_Data_Inizio_FRU_CAN;
                        oNewRecord.DataOraUltimaModifica_Fru_Can = DateTime.UtcNow;
                        oNewRecord.Data_Registrazione_Fru_Can = oRow.IsAbilitazione_Data_Inizio_FRU_CANNull()
                          ? new DateTime(2000, 1, 1) : oRow.Abilitazione_Data_Inizio_FRU_CAN;
                        oNewRecord.DisAbilitazione_Fru_Can = oRow.IsDisAbilitazione_FRU_CANNull() ? false : oRow.DisAbilitazione_FRU_CAN;
                        oNewRecord.Note_Fru_Can = oRow.IsNote_FRU_CANNull() ? (string)null : oRow.Note_FRU_CAN;

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
                                Nome_Tabella_Tab_Check_Imp = "Fru_Cant",
                                Chiave_Record_Tab_Check_Imp = oRow.RRN.ToString(),
                            });
                            log.Warn("Codice Cantiere cui si rifericono gli errori precedenti: " + oRow.Codice_Cantiere_FRU_CAN + "Codice Unità Fissa cui si rifericono gli errori precedenti: " + oRow.Codice_FRU_CAN + " - RRN: " + oRow.RRN + " -------------------------------------------------------------------------------");
                            errorsList.Add(currentDictionary);
                            errorsList.Add(importDictionary);
                        }

                    }
                    catch (Exception ex)
                    {
                        var RRNERR = oRow.RRN;
                        //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                        if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                            Log.ErrorFormat("TAB FRU_CANT - Chiave: {0}", oRow.RRN.ToString());
                        throw ex;
                    }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                try
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "FRU_CANT", lastKey));

                    this.Context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var item in toImport)
                        base.Add(item);
                    this.Context.Configuration.AutoDetectChangesEnabled = true;

                    RepoManager.Tab_Chk_ImpRepo.Add(errors);
                    if (errors.Count == 0)

                        RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Fru_Cant",
                            Stato_Record_Tab_Check_Imp = true,
                        });
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
            ResetSession();
            return errorsList;
        }

        public override Expression<Func<Fru_Cant, bool>> Filter
        {
            get
            {
                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Fil) == DomainFilterEnum.Fil && PowerWebContext.Current.Fils != null && PowerWebContext.Current.User.Liv_Utente < 10)
                {
                    var allCantIds = PowerWebContext.Current.CantsIds;
                    return fruCant => allCantIds.Contains(fruCant.Cant_Id);
                }
                else return base.Filter;
            }
        }

        
    }
}
