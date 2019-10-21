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
namespace Business.Repository.Custom
{
    public class Pru_ColRepository : GenericRepository<Pru_Col>, IPru_ColRepository
    {
        public Pru_ColRepository(PowerWebEntities context)
            : base(context)
        {
        }

        //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
        private static DateTime DataOraRecord;

        /// <summary>Ottiene le tabelle che deve usare in formato lista.
        /// Per ottimizzare la velocità salva l'oggetto nella sessione corrente
        /// in modo che la lista sia già in memoria quando viene richiesta più volte.
        /// </summary>
        private static List<Col> Cols
        {
            get
            {
                List<Col> oLista = PowerWebContext.GetFromSession<List<Col>>("Cols_Pru_ColRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.ColRepo.GetAll(true).ToList();
                    //la lista viene salvata in sessione per averla disponibile più velocemente
                    PowerWebContext.SetToSession<List<Col>>("Cols_Pru_ColRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Pru> Prus
        {
            get
            {
                List<Pru> oLista = PowerWebContext.GetFromSession<List<Pru>>("Prus_Pru_ColRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.PruRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Pru>>("Prus_Pru_ColRepo", oLista);
                }
                return oLista;
            }
        }

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Tab_Decod>>("Cols_Pru_ColRepo", null);
            PowerWebContext.SetToSession<List<Tab_Decod>>("Prus_Pru_ColRepo", null);
        }

        public override Pru_Col Init()
        {
            Pru_Col oNewRecord = base.Init();
            oNewRecord.DisAbilitazione_Pru_Col = false;
            oNewRecord.Data_Registrazione_Pru_Col = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Pru_Col = DateTime.UtcNow;
            return oNewRecord;
        }

        public override void Add(Pru_Col entity, bool saveChanges = false)
        {
            base.Add(entity, saveChanges);

            elaboratePruChanges(entity.Pru_Id, entity.Abilitazione_Data_Inizio_Pru_Col);
        }

        public override void Delete(Pru_Col entity, bool saveChanges = false)
        {
            base.Delete(entity, saveChanges);

            elaboratePruChanges(entity.Pru_Id, entity.Abilitazione_Data_Inizio_Pru_Col);
        }

        public override void Update(Pru_Col entity, bool saveChanges = false)
        {
            elaboratePruChanges(entity.Pru_Id, entity.Abilitazione_Data_Inizio_Pru_Col, true, entity.Pru_Col_Id);

            base.Update(entity, saveChanges);
        }

        private void elaboratePruChanges(int pru_Id, DateTime newStartDate, bool isForUpdate = false, int entityId = -1)
        {
            if (isForUpdate)
            {
                var oldPruCol = RepoManager.Pru_ColRepo.Single(pruCol => pruCol.Pru_Col_Id == entityId, true);

                elaboratePruChanges(pru_Id, oldPruCol.Abilitazione_Data_Inizio_Pru_Col);
            }

            newStartDate = newStartDate.Date;

            var toDates = RepoManager.Pru_ColRepo.Find(pruCol => pruCol.Pru_Id == pru_Id && pruCol.Abilitazione_Data_Inizio_Pru_Col > newStartDate, true).ToList();

            var nextDate = DateTime.MaxValue;

            if (toDates.Count > 0)
            {
                var nextPruCol = toDates.OrderBy(pruCol => pruCol.Abilitazione_Data_Inizio_Pru_Col).First();

                nextDate = nextPruCol.Abilitazione_Data_Inizio_Pru_Col;
            }

            RepoManager.PendingElabRepo.Add(new PendingElab { FromDate_PendingElab = newStartDate, ToDate_PendingElab = nextDate.Date, Pru_Id = pru_Id }, true);
        }

        public override void SetEntityBeforeAddOrUpdate(Pru_Col entity)
        {
            DataOraRecord = entity.DataOraUltimaModifica_Pru_Col;
            entity.DataOraUltimaModifica_Pru_Col = DateTime.UtcNow;
        }

        public override Dictionary<string, string> Check(Pru_Col entity, bool isNew = false, bool isResetSession = true)
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
                    DateTime DataOraRecordDb = RepoManager.Pru_ColRepo.Single(u => u.Pru_Col_Id == entity.Pru_Col_Id).DataOraUltimaModifica_Pru_Col;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Pru_Col),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_PRU_COL));
                    }
                }
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                //
                //  CHAIVE UNIVOCA : COL_ID + PRU_ID + DATA ABILITAZIONE_DATA_INIZIO
                // 
                if (CommonService.Nz(entity.Col_Id, 0) == 0 ||
                    CommonService.Nz(entity.Pru_Id, 0) == 0 ||
                    CommonService.Nz(entity.Abilitazione_Data_Inizio_Pru_Col, new DateTime(1, 1, 1)) == new DateTime(1, 1, 1))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pru_Col_Id),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_DATI_NECESSARI_MANCANTI));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.Pru_ColRepo.SingleOrDefault(u => u.Col_Id == entity.Col_Id &&
                           u.Pru_Id == entity.Pru_Id &&
                           u.Abilitazione_Data_Inizio_Pru_Col == entity.Abilitazione_Data_Inizio_Pru_Col) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pru_Col_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.Pru_ColRepo.SingleOrDefault(u => u.Col_Id == entity.Col_Id &&
                          u.Pru_Id == entity.Pru_Id &&
                          u.Abilitazione_Data_Inizio_Pru_Col == entity.Abilitazione_Data_Inizio_Pru_Col &&
                          u.Pru_Col_Id != entity.Pru_Col_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pru_Col_Id),
                               BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                
                // in ogni caso non è possibile avere nella stessa data e per la stessa pru due record
                bool isPresentInSameDate = Find(pruCol => pruCol.Abilitazione_Data_Inizio_Pru_Col == entity.Abilitazione_Data_Inizio_Pru_Col && pruCol.Pru_Id == entity.Pru_Id && entity.Pru_Col_Id != pruCol.Pru_Col_Id).Any();

                if (isPresentInSameDate)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Registrazione_Pru_Col), BusinessService.GetLocalizedString(PowerWebResources.ERR_PRU_GIA_ASS_IN_DATA));

                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //
                if (entity.Abilitazione_Data_Inizio_Pru_Col == DateTime.MinValue)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilitazione_Data_Inizio_Pru_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_ABILITAZIONE_DATA_INIZIO_PRU_COL));
                if (entity.Col_Id == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Col_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_COL_ID));
                else
                {
                    if (RepoManager.ColRepo.SingleOrDefault(u => u.Col_Id == entity.Col_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Col_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_COL_ID, PowerWebResources.STR_COLLABORATORI));
                }
                if (entity.Pru_Id == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pru_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_PRU_ID));
                else
                {
                    if (RepoManager.PruRepo.SingleOrDefault(u => u.Pru_Id == entity.Pru_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pru_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_PRU_ID, PowerWebResources.STR_PRU));
                }
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
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
                var CodErr = "Col_Id: " + entity.Col_Id + " - Pru_Id:" + entity.Pru_Id;
                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Pru_Col entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            //Verifico SOLO x IMPORT la Validità delle eventuali Date Ricevute
            if (entity.Data_Registrazione_Pru_Col < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Pru_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_PRU_COL));
            if (entity.DataOraUltimaModifica_Pru_Col < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Pru_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_PRU_COL));
            if (CommonService.Nz(entity.Abilitazione_Data_Inizio_Pru_Col, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Abilitazione_Data_Inizio_Pru_Col),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_ABILITAZIONE_DATA_INIZIO_PRU_COL));

            WriteCheckLog(entity, result, Log);
            return result;
        }

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Pru_Col");
            List<PowerMDBDataSet.Pru_ColRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                <PowerMDBDataSet.Pru_ColRow>(oDataSet.Pru_Col.ToList(), "Pru_Col", "RRN").OrderBy(acd => acd.RRN).ToList();
            List<Pru_Col> toImport = new List<Pru_Col>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>();
            string lastKey = "";
            if (accessData != null)
            {
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                foreach (PowerMDBDataSet.Pru_ColRow oRow in accessData)
                {
                    try
                    {
                        lastKey = oRow.RRN.ToString();
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                            BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "PRU_COL",
                            "14", "17", oRow.RRN.ToString(), countRec.ToString(), nRec.ToString()));
                        Pru_Col oNewRecord = this.Init();

                        List<Col> testcol = Cols;

                        //viene estratto il record del collaboartore che ha il codice collaboratore uguale  a oRow.Codice_Collaboratore_PRU_COL(prima veniva usato singleOrDefault)
                        Col oCol = Cols.FirstOrDefault(c => c.Codice_Collaboratore == oRow.Codice_Collaboratore_PRU_COL);
                        if (oCol != null)
                            oNewRecord.Col_Id = oCol.Col_Id;

                        Pru oPru = Prus.FirstOrDefault(p => p.Codice_Pru == oRow.Codice_PRU_COL);
                        if (oPru != null)
                            oNewRecord.Pru_Id = oPru.Pru_Id;
                        oNewRecord.Abilitazione_Data_Inizio_Pru_Col = oRow.IsAbilitazione_Data_Inizio_PRU_COLNull()
                          ? new DateTime(2000, 1, 1) : oRow.Abilitazione_Data_Inizio_PRU_COL;
                        oNewRecord.DataOraUltimaModifica_Pru_Col = DateTime.UtcNow;
                        oNewRecord.Data_Registrazione_Pru_Col = oRow.IsAbilitazione_Data_Inizio_PRU_COLNull()
                          ? new DateTime(2000, 1, 1) : oRow.Abilitazione_Data_Inizio_PRU_COL;
                        oNewRecord.DisAbilitazione_Pru_Col = oRow.IsDisAbilitazione_PRU_COLNull() ? false : oRow.DisAbilitazione_PRU_COL;
                        oNewRecord.Note_Pru_Col = oRow.IsNote_PRU_COLNull() ? (string)null : oRow.Note_PRU_COL;
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
                                Nome_Tabella_Tab_Check_Imp = "Pru_Col",
                                Chiave_Record_Tab_Check_Imp = oRow.RRN.ToString(),
                            });
                            log.Warn("Codice Collaboratore cui si rifericono gli errori precedenti: " + oRow.Codice_Collaboratore_PRU_COL + "Codice Unità Portatile cui si rifericono gli errori precedenti: " + oRow.Codice_PRU_COL + " - RRN : " + oRow.RRN + " -------------------------------------------------------------------------------");
                            errorsList.Add(currentDictionary);
                            errorsList.Add(importDictionary);
                        }
                    }
                    catch (Exception ex)
                    {
                        var RRNERR = oRow.RRN;
                        //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                        if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                            Log.WarnFormat("TAB PRU_COL - Chiave: {0}", oRow.RRN.ToString());
                        throw ex;
                    }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                try
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "PRU_COL", lastKey));


                    this.Context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var item in toImport)
                        base.Add(item);
                    this.Context.Configuration.AutoDetectChangesEnabled = true;

                    RepoManager.Tab_Chk_ImpRepo.Add(errors);
                    if (errors.Count == 0)

                        RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Pru_Col",
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
            return errorsList;
        }

        public override Expression<Func<Pru_Col, bool>> Filter
        {
            get
            {
                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Resp) == DomainFilterEnum.Resp && PowerWebContext.Current.Resps != null && PowerWebContext.Current.User.Liv_Utente < 10 )
                {
                    var allColIds = PowerWebContext.Current.ColsIds;
                    return pruCol => allColIds.Contains(pruCol.Col_Id);
                }
                else return base.Filter;
            }
        }
    }
}
