using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;
using System.Linq.Expressions;
using System.Data;
using System.Data.OleDb;
using Business.MDBSchema;
using log4net;

namespace Business.Repository.Custom
{
    public class Col_NoteRepository : GenericRepository<Col_Note>, ICol_NoteRepository
    {
        public Col_NoteRepository(PowerWebEntities context)
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
                List<Col> cols = PowerWebContext.GetFromSession<List<Col>>("Cols_Col_NoteRepo");

                if (cols == null)
                {
                    cols = RepoManager.ColRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Col>>("Cols_Col_NoteRepo", cols);
                }

                return cols;
            }
        }
        private static List<Utenti> Utenti
        {
            get
            {
                List<Utenti> users = PowerWebContext.GetFromSession<List<Utenti>>("Utenti_Col_NoteRepo");

                if (users == null)
                {
                    users = RepoManager.UtentiRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Utenti>>("Utenti_Col_NoteRepo", users);
                }

                return users;
            }
        }
        private static List<Tab_Decod> Tab_Decods
        {
            get
            {
                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_Col_NoteRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_DecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Col_NoteRepo", oLista);
                }
                return oLista;
            }
        }

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Col>>("Cols_Col_NoteRepo", null);
            PowerWebContext.SetToSession<List<Utenti>>("Utenti_Col_NoteRepo", null);
            PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Col_NoteRepo", null);
        }

        public override Col_Note Init()
        {
            Col_Note oNewRecord = base.Init();
            oNewRecord.DataOraUltimaModifica_Col_Note = DateTime.UtcNow;
            oNewRecord.Data_Nota_Col_Note = DateTime.UtcNow;
            oNewRecord.Data_Registrazione_Col_Note = DateTime.UtcNow;
            oNewRecord.DisAbilitazione_Col_Note = false;
            oNewRecord.Utenti_Id = PowerWebContext.Current.User.Utenti_Id;
            return oNewRecord;
        }

        public override void SetEntityBeforeAddOrUpdate(Col_Note entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Col_Note;
            entity.DataOraUltimaModifica_Col_Note = DateTime.UtcNow;
        }

        public override Dictionary<string, string> Check(Col_Note entity, bool isNew = false, bool isResetSession = true)
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
                    DateTime DataOraRecordDb = RepoManager.Col_NoteRepo.Single(u => u.Col_Note_Id == entity.Col_Note_Id).DataOraUltimaModifica_Col_Note;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Col_Note),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_COL_NOTE));
                    }
                }
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                //
                // NOTA BENE : possono esistere + NOTE dello stesso Giorno e dello stesso tipo per lo stesso Cantiere ma con almeno il Testo
                //
                if (CommonService.Nz(entity.Col_Id, 0) == 0 ||
                    CommonService.Nz(entity.Tipo_Nota_Col_Note, "") == "" ||
                    CommonService.Nz(entity.Nota_Col_Note, "") == "" ||
                    CommonService.Nz(entity.Data_Nota_Col_Note, new DateTime(1, 1, 1)) == new DateTime(1, 1, 1))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Col_Note_Id),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_DATI_NECESSARI_MANCANTI));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.Col_NoteRepo.SingleOrDefault(u => u.Col_Id == entity.Col_Id &&
                           u.Tipo_Nota_Col_Note == entity.Tipo_Nota_Col_Note &&
                           u.Nota_Col_Note == entity.Nota_Col_Note &&
                           u.Data_Nota_Col_Note == entity.Data_Nota_Col_Note) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Col_Note_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.Col_NoteRepo.SingleOrDefault(u => u.Col_Id == entity.Col_Id &&
                          u.Tipo_Nota_Col_Note == entity.Tipo_Nota_Col_Note &&
                          u.Nota_Col_Note == entity.Nota_Col_Note &&
                          u.Data_Nota_Col_Note == entity.Data_Nota_Col_Note &&
                          u.Col_Note_Id != entity.Col_Note_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Col_Note_Id),
                               BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                if (CommonService.Nz(entity.Col_Id, 0) == 0)
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
                if (CommonService.Nz(entity.Tipo_Nota_Col_Note, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Nota_Col_Note),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_TIPO_NOTA_COL_NOTE));
                else
                {
                    if (string.IsNullOrEmpty(entity.Tipo_Nota_Col_Note))
                        if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString() &&
                        x.Nome_Tab == TabDecodNameEnum.TIPO_NOTA.ToString() && x.Chiave_Tab == entity.Tipo_Nota_Col_Note) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Nota_Col_Note),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                              PowerWebResources.FLD_TIPO_NOTA_COL_NOTE));
                }
                if (CommonService.Nz(entity.Utenti_Id , 0) == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_UTENTI_ID));
                else
                {
                    if (RepoManager.UtentiRepo.SingleOrDefault(u => u.Utenti_Id == entity.Utenti_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_UTENTI_ID, PowerWebResources.STR_UTENTI));
                }
                if (CommonService.Nz(entity.Data_Nota_Col_Note, new DateTime(1, 1, 1)) == new DateTime(1, 1, 1))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Nota_Col_Note),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_DATA_NOTA_COL_NOTE));
                if (CommonService.Nz(entity.Nota_Col_Note, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nota_Col_Note),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_NOTA_COL_NOTE));
                //
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Tipo_Nota_Col_Note, "") != "")
                    if (entity.Tipo_Nota_Col_Note.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Nota_Col_Note),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TIPO_NOTA_COL_NOTE, PowerWebResources.VALORE_10 ));
                //
                //5) verifico, per una serie di campi, che il valore del campo sia presente nella relativa Tabella
                //    
                // NESSUN CONTROLLO DI QUESTO TIPO
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

        public override Dictionary<string, string> CheckForImport(Col_Note entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //verifico SOLO x IMPORT la validità delle eventuali Date Ricevute
            if (entity.Data_Registrazione_Col_Note< new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Col_Note),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_COL_NOTE));
            if (entity.DataOraUltimaModifica_Col_Note < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Col_Note),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_COL_NOTE));
            if (CommonService.Nz(entity.Data_Nota_Col_Note, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Nota_Col_Note),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_NOTA_COL_NOTE));           
            WriteCheckLog(entity, result, Log);
            return result;
        }

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Col_Note");
            Dictionary<string, string> oResultDictionary = new Dictionary<string, string>();
            List<PowerMDBDataSet.Col_NoteRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                    <PowerMDBDataSet.Col_NoteRow>(oDataSet.Col_Note.ToList(), "Col_Note", "RRN").OrderBy(acd => acd.RRN).ToList();
            List<Col_Note> toImport = new List<Col_Note>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>();
            string lastKey = "";
            if (accessData != null)
            {
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                foreach (PowerMDBDataSet.Col_NoteRow oRow in accessData)
                {
                    try
                    {
                        lastKey = oRow.RRN.ToString();
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;                        
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                            BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "COL_NOTE",
                            "10", "17", oRow.RRN.ToString(), countRec.ToString(), nRec.ToString()));
                        Col_Note oNewRecord = this.Init();
                        Col oCol = Cols.SingleOrDefault(c => c.Codice_Collaboratore == oRow.Codice_Collaboratore_Col_Note);
                        if (oCol != null)
                            oNewRecord.Col_Id = oCol.Col_Id;
                        oNewRecord.DataOraUltimaModifica_Col_Note = DateTime.UtcNow;
                        oNewRecord.Data_Nota_Col_Note = oRow.IsData_Nota_Col_NoteNull() ? new DateTime(2000, 1, 1) : oRow.Data_Nota_Col_Note;
                        oNewRecord.Data_Registrazione_Col_Note = oRow.IsData_Nota_Col_NoteNull() ? new DateTime(2000, 1, 1) : oRow.Data_Nota_Col_Note;
                        oNewRecord.DisAbilitazione_Col_Note = oRow.IsDisAbilitazione_Col_NoteNull() ? false : oRow.DisAbilitazione_Col_Note;
                        //Nel caso di Utente NULL imposto l'Utente che sta caricando i Dati
                        oRow.Inserita_Da_Col_Note = oRow.IsInserita_Da_Col_NoteNull() ? PowerWebContext.Current.User.Codice_Utente : oRow.Inserita_Da_Col_Note;
                        Utenti oUtenti = Utenti.SingleOrDefault(u => u.Codice_Utente == oRow.Inserita_Da_Col_Note);
                        if (oUtenti != null)
                            oNewRecord.Utenti_Id = oUtenti.Utenti_Id;
                        oNewRecord.Nota_Col_Note = oRow.IsNota_Col_NoteNull() ? (string)null : oRow.Nota_Col_Note;
                        oNewRecord.Tipo_Nota_Col_Note = oRow.IsTipo_Nota_Col_NoteNull() ? (string)null : oRow.Tipo_Nota_Col_Note;

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
                                Nome_Tabella_Tab_Check_Imp = "Col_Note",
                                Chiave_Record_Tab_Check_Imp = oRow.RRN.ToString(),
                            });
                            log.Warn("Codice Collaboratore cui si rifericono gli errori precedenti: " + oRow.Codice_Collaboratore_Col_Note + " - RRN : " + oRow.RRN  + " -------------------------------------------------------------------------------");
                            errorsList.Add(currentDictionary);
                            errorsList.Add(importDictionary);
                        }
                        //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                        if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                            Log.WarnFormat("TAB COL_NOTE - Chiave: {0}", oRow.RRN.ToString());
                    }
                    catch (Exception ex)
                    {
                        var RRNERR = oRow.RRN;
                        //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                        if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                            Log.ErrorFormat("TAB COL_NOTE - Chiave: {0}", oRow.RRN.ToString());
                        throw ex;
                    }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                try
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "COL_NOTE", lastKey));
                    RepoManager.Col_NoteRepo.Add(toImport);
                    RepoManager.Tab_Chk_ImpRepo.Add(errors);
                    if (errors.Count == 0)
                        RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Col_Note",
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

        public override Expression<Func<Col_Note, bool>> Filter
        {
            get
            {
                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Resp) == DomainFilterEnum.Resp && PowerWebContext.Current.Resps != null && PowerWebContext.Current.User.Liv_Utente < 10)
                {
                    var allColIds = PowerWebContext.Current.ColsIds;
                    return colNote => allColIds.Contains(colNote.Col_Id);
                }
                else return base.Filter;
            }
        }
    }
}
