using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;
using log4net;
using System.Linq.Expressions;
using System.Data;
using System.Data.OleDb;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
    public class Cant_NoteRepository : GenericRepository<Cant_Note>, ICant_NoteRepository
    {
        public Cant_NoteRepository(PowerWebEntities context)
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
                List<Cant> oLista = PowerWebContext.GetFromSession<List<Cant>>("Cants_Cant_NoteRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.CantRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cant>>("Cants_Cant_NoteRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Utenti> Utenti
        {
            get
            {
                List<Utenti> oLista = PowerWebContext.GetFromSession<List<Utenti>>("Utenti_Cant_NoteRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.UtentiRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Utenti>>("Utenti_Cant_NoteRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Tab_Decod> Tab_Decods
        {
            get
            {
                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_Cant_NoteRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_DecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Cant_NoteRepo", oLista);
                }
                return oLista;
            }
        }

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Cant>>("Cants_Cant_NoteRepo", null);
            PowerWebContext.SetToSession<List<Utenti>>("Utenti_Cant_NoteRepo", null);
            PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Cant_NoteRepo", null);
        }

        public override Cant_Note Init()
        {
            Cant_Note oNewRecord = base.Init();
            oNewRecord.DataOraUltimaModifica_Can_Note = DateTime.UtcNow;
            oNewRecord.Data_Nota_Can_Note = DateTime.UtcNow;
            oNewRecord.Data_Registrazione_Can_Note = DateTime.UtcNow;
            oNewRecord.DisAbilitazione_Can_Note = false;
            oNewRecord.Utenti_Id = PowerWebContext.Current.User.Utenti_Id;
            return oNewRecord;
        }

        public override void SetEntityBeforeAddOrUpdate(Cant_Note entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Can_Note;
            entity.DataOraUltimaModifica_Can_Note = DateTime.UtcNow;
        }

        public override Dictionary<string, string> CheckForImport(Cant_Note entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //verifico SOLO x IMPORT la validità delle eventuali Date Ricevute
            if (entity.Data_Registrazione_Can_Note < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Can_Note),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_CAN_NOTE));
            if (entity.DataOraUltimaModifica_Can_Note < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Can_Note),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_CAN_NOTE));
            if (CommonService.Nz(entity.Data_Nota_Can_Note, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Nota_Can_Note),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_NOTA_CAN_NOTE));
            //if (entity.Durata_Max_Gruppo_Notte_Ril_Can != null)
            //    if (entity.Durata_Max_Gruppo_Notte_Ril_Can > new TimeSpan(23, 59, 59))
            //        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Max_Gruppo_Notte_Ril_Can),
            //        CommonServiceBiz.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_ERRATO,
            //        PowerWebResources.FLD_DURATA_MAX_GRUPPO_NOTTE_RIL_CAN));
            WriteCheckLog(entity, result, Log);
            return result;
        }

        public override Dictionary<string, string> Check(Cant_Note entity, bool isNew = false, bool isResetSession = true)
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
                    DateTime DataOraRecordDb = RepoManager.Cant_NoteRepo.Single(u => u.Cant_Note_Id == entity.Cant_Note_Id).DataOraUltimaModifica_Can_Note;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Can_Note),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_CAN_NOTE));
                    }
                }
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                // NOTA BENE : possono esistere + NOTE dello stesso Giorno e dello stesso tipo per lo stesso Cantiere ma con almeno il Testo
                // 
                if (entity.Cant_Id == Int32.MinValue ||
                    String.IsNullOrEmpty(entity.Tipo_Nota_Can_Note) ||
                    String.IsNullOrEmpty (entity.Nota_Can_Note)||
                    entity.Data_Nota_Can_Note== DateTime.MinValue)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Note_Id),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_DATI_NECESSARI_MANCANTI));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.Cant_NoteRepo.SingleOrDefault(u => u.Cant_Id == entity.Cant_Id &&
                           u.Tipo_Nota_Can_Note == entity.Tipo_Nota_Can_Note &&
                           u.Nota_Can_Note == entity.Nota_Can_Note &&
                           u.Data_Nota_Can_Note == entity.Data_Nota_Can_Note) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Note_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.Cant_NoteRepo.SingleOrDefault(u => u.Cant_Id == entity.Cant_Id &&
                          u.Tipo_Nota_Can_Note == entity.Tipo_Nota_Can_Note &&
                          u.Nota_Can_Note == entity.Nota_Can_Note &&
                          u.Data_Nota_Can_Note == entity.Data_Nota_Can_Note &&
                          u.Cant_Note_Id != entity.Cant_Note_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Note_Id),
                               BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //2) Verifico i Valori Assunti dai Vari Campi che devono essere controllati
                //
                if (CommonService.Nz(entity.Cant_Id, 0) == 0)
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
                                
                if (CommonService.Nz(entity.Data_Nota_Can_Note, new DateTime(1, 1, 1)) == new DateTime(1, 1, 1))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Nota_Can_Note),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_DATA_NOTA_CAN_NOTE));                

                if (CommonService.Nz(entity.Nota_Can_Note, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nota_Can_Note),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_NOTA_CAN_NOTE));

                if (CommonService.Nz(entity.Tipo_Nota_Can_Note, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Nota_Can_Note),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_TIPO_NOTA_CAN_NOTE));
                else
                {
                    if (entity.Tipo_Nota_Can_Note.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Nota_Can_Note),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TIPO_NOTA_CAN_NOTE, PowerWebResources.VALORE_10));                   
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString() &&
                        x.Nome_Tab == TabDecodNameEnum.TIPO_NOTA.ToString() && x.Chiave_Tab == entity.Tipo_Nota_Can_Note) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Nota_Can_Note),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                              PowerWebResources.FLD_TIPO_NOTA_CAN_NOTE));
                }
                
                                                      
                if (CommonService.Nz(entity.Utenti_Id, 0) != 0)
                    if (RepoManager.UtentiRepo.SingleOrDefault(u => u.Utenti_Id == entity.Utenti_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_UTENTI_ID, PowerWebResources.STR_UTENTI));

                if (CommonService.Nz(entity.Utenti_Id, 0) == 0)
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
                //
                //3) Scrittura del Record di LOG
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

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<string, string>>();
            ILog log = LogManager.GetLogger("Cant");
            List<PowerMDBDataSet.Cant_NoteRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                    <PowerMDBDataSet.Cant_NoteRow>(oDataSet.Cant_Note.ToList(), "Cant_Note", "RRN").OrderBy(acd => acd.RRN).ToList(); ;
            List<Cant_Note> toImport = new List<Cant_Note>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>(); ;
            string lastKey = ""; 
            if (accessData != null)
            {
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                foreach (PowerMDBDataSet.Cant_NoteRow oRow in accessData)
                {
                   try
                   {
                       lastKey = oRow.RRN.ToString ();
                       countRec = countRec + 1;
                       percRec = (countRec / nRec) * 100;                       
                       BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                          BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(),
                          "CANT_NOTE","7","17", oRow.RRN.ToString(), countRec.ToString(), nRec.ToString()));
                    Cant_Note oNewRecord = this.Init();
                        Cant oCant = Cants.SingleOrDefault(c => c.Codice_Cantiere == oRow.Codice_Cantiere_Can_Note);
                    if (oCant != null)
                        oNewRecord.Cant_Id = oCant.Cant_Id;
                    oNewRecord.DataOraUltimaModifica_Can_Note = DateTime.UtcNow;
                    oNewRecord.Data_Nota_Can_Note = oRow.IsData_Nota_Can_NoteNull() ? new DateTime(2000, 1, 1) : oRow.Data_Nota_Can_Note;
                    oNewRecord.Data_Registrazione_Can_Note = oRow.IsData_Nota_Can_NoteNull() ? new DateTime(2000, 1, 1) : oRow.Data_Nota_Can_Note;
                    oNewRecord.DisAbilitazione_Can_Note = oRow.IsDisAbilitazione_Can_NoteNull() ? false : oRow.DisAbilitazione_Can_Note;
                        //Nel caso di Utente NULL imposto l'Utente che sta caricando i Dati
                        oRow.Inserita_Da_Can_Note = oRow.IsInserita_Da_Can_NoteNull() ? PowerWebContext.Current.User.Codice_Utente : oRow.Inserita_Da_Can_Note;
                    Utenti oUtenti = Utenti.SingleOrDefault(c => c.Codice_Utente == oRow.Inserita_Da_Can_Note);
                    if (oUtenti != null)
                        oNewRecord.Utenti_Id = oUtenti.Utenti_Id;
                    oNewRecord.Nota_Can_Note = oRow.IsNota_Can_NoteNull() ? (string)null : oRow.Nota_Can_Note;
                    oNewRecord.Tipo_Nota_Can_Note = oRow.IsTipo_Nota_Can_NoteNull() ? (string)null : oRow.Tipo_Nota_Can_Note;
                    // Eseguo i Controlli Specifici della CheckForImport e poi i Controlli Standard della Check     
                    Dictionary<string, string> importDictionary = CheckForImport(oNewRecord);
                    //Solo la prima volta chiamo la Check con ResetSession=True per fargli aggironare i Dati dal DB
                    if(countRec == 1)
                       currentDictionary = Check(oNewRecord, true,true);
                    else
                        currentDictionary = Check(oNewRecord, true, false);
                    //Verifico se ci sono stati errori
                    if (currentDictionary.Keys.Count == 0 && importDictionary.Keys.Count == 0)
                    toImport.Add(oNewRecord);
                    else
                    {
                        errors.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Cant_Note",
                            Chiave_Record_Tab_Check_Imp = oRow.RRN.ToString(),
                        });
                            log.Warn("Codice Cantiere cui si rifericono gli errori precedenti: " + oRow.Codice_Cantiere_Can_Note + " - RRN : " + oRow.RRN + " -------------------------------------------------------------------------------");
                            errorsList.Add(currentDictionary);
                            errorsList.Add(importDictionary);
                        }
                       }
                   catch (Exception ex)
                       {
                           var RRNERR = oRow.RRN;
                           //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                           if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                               Log.ErrorFormat("TAB CANT_NOTE - Chiave: {0}", oRow.RRN.ToString() );
                           throw ex;
                    }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                try
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                          BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "CANT_NOTE", lastKey ));
                    RepoManager.Cant_NoteRepo.Add(toImport);
                    RepoManager.Tab_Chk_ImpRepo.Add(errors);
                    if (errors.Count == 0)
                        RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Cant_Note",
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

        public override Expression<Func<Cant_Note, bool>> Filter
        {
            get
            {
                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Fil) == DomainFilterEnum.Fil && PowerWebContext.Current.Fils != null && PowerWebContext.Current.User.Liv_Utente < 10)
                {
                    var allCantIds = PowerWebContext.Current.CantsIds;
                    return cantNote => allCantIds.Contains(cantNote.Cant_Id);
                }
                else return base.Filter;
            }
        }
    }
}
