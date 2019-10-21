using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;
using System.Linq.Expressions;
using Business.MDBSchema;
using log4net;

namespace Business.Repository.Custom
{
    public class PruRepository : GenericRepository<Pru>, IPruRepository
    {
        public PruRepository(PowerWebEntities context)
            : base(context)
        {
        }

        //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
        private static DateTime DataOraRecord;

        private static void ResetSession()
        {           
        }

        public override Pru Init()
        {
            Pru oNewRecord = base.Init();
            oNewRecord.DisAbilitazione_Pru = false;
            oNewRecord.Data_Registrazione_Pru = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Pru = DateTime.UtcNow;
            oNewRecord.Singola_Reg_Pru = false;
            return oNewRecord;
        }

        public override void SetEntityBeforeAddOrUpdate(Pru entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Pru;
            entity.DataOraUltimaModifica_Pru = DateTime.UtcNow;
            //nel caso in cui non sia stato inserito il n° di serie allora viene impostato uguale al codice unità
            if (String.IsNullOrEmpty(entity.N_Serie_Pru))
                if (!String.IsNullOrEmpty(entity.Codice_Pru))
                    entity.N_Serie_Pru = entity.Codice_Pru;
            entity.N_Serie_Pru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.N_Serie_Pru, 10);
            entity.Codice_Pru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Pru, 10);
        }

        public override void Delete(Pru entity, bool saveChanges = false)
        {
            if (RepoManager.RegRepo.Find(reg => reg.Pru_Id == entity.Pru_Id, true).Count() == 0)
                base.Delete(entity, saveChanges);
        }

        public override Dictionary<string, string> Check(Pru entity, bool isNew = false, bool isResetSession = true){
            Dictionary<string, string> result = new Dictionary<string, string>();
            //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession();

            try
            {                        
                //Leggo la DataOraUltimaModifica ATTUALE dal REcord del DB per verificare che nessuno abbia modificato il Record nel frattempo

                if (!isNew)
                {
                    DateTime DataOraRecordDb = RepoManager.PruRepo.Single(u => u.Pru_Id == entity.Pru_Id).DataOraUltimaModifica_Pru;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Pru),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_PRU));
                    }
                }
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                if (String.IsNullOrEmpty(entity.Codice_Pru))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Pru),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_PRU));
                else
                {
                    entity.Codice_Pru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Pru, 10);
                    if (isNew)
                    {
                        if (RepoManager.PruRepo.SingleOrDefault(u => u.Codice_Pru == entity.Codice_Pru) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Pru),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.PruRepo.SingleOrDefault(u => u.Codice_Pru == entity.Codice_Pru && u.Pru_Id != entity.Pru_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Pru),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //1.1) verifico che gli altri Campi siano Univoci
                //
                if (string.IsNullOrEmpty(entity.N_Serie_Pru))
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.N_Serie_Pru),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_N_SERIE_PRU));
                }
                else
                {
                    
                    entity.N_Serie_Pru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.N_Serie_Pru, 10);
                    if (RepoManager.PruRepo.FirstOrDefault(x => x.N_Serie_Pru == entity.N_Serie_Pru && x.Codice_Pru != entity.Codice_Pru && entity.Pru_Id != x.Pru_Id) != null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.N_Serie_Pru),
                          BusinessService.GetLocalizedString(PowerWebResources.STR_IL_N_SERIE_E_GIA_ASSEGNATO));
                }

                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella            
                //
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                // NESSUN CONTROLLO DI QUESTO TIPO
                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Codice_Pru, "") != "")
                    if (entity.Codice_Pru.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Pru),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_PRU, PowerWebResources.VALORE_10 ));
                if (CommonService.Nz(entity.N_Serie_Pru, "") != "")
                    if (entity.N_Serie_Pru.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.N_Serie_Pru),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_N_SERIE_PRU, PowerWebResources.VALORE_10 ));
                //
                //5) verifico, per una serie di campi, che il valore del campo sia presente nella relativa Tabella
                //

                if (CommonService.Nz(entity.Codice_Pru, "") != "")
                {
                    var codiceVerifica = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Pru, 10);
                    if (RepoManager.FruRepo.SingleOrDefault(pr => pr.Codice_Fru == codiceVerifica) != null)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Pru),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_NON_DEVE_ESISTERE_IN_TABELLA_Y,
                        PowerWebResources.FLD_CODICE_PRU, PowerWebResources.STR_FRU));
                    }
                }

                if (CommonService.Nz(entity.N_Serie_Pru, "") != "")
                {
                    var codiceVerifica = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.N_Serie_Pru, 10);
                    if (RepoManager.FruRepo.SingleOrDefault(pr => pr.N_Serie_Fru == codiceVerifica) != null)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.N_Serie_Pru),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_NON_DEVE_ESISTERE_IN_TABELLA_Y,
                        PowerWebResources.FLD_N_SERIE_PRU, PowerWebResources.STR_FRU));
                    }
                }

                //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);
            }
            catch (Exception ex)
            {
                var CodErr = "Pru_Id: " + entity.Pru_Id;
                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Pru entity)
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                //Verifico SOLO x IMPORT la Validità delle eventuali Date Ricevute
                if (entity.Data_Registrazione_Pru < new DateTime(2000, 01, 01))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Pru),
                       BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_PRU));
                if (entity.DataOraUltimaModifica_Pru < new DateTime(2000, 01, 01))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Pru),
                       BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_PRU));
                if (String.IsNullOrEmpty(entity.N_Serie_Pru))
                    if (!String.IsNullOrEmpty(entity.Codice_Pru))
                        entity.N_Serie_Pru = "0";
                entity.N_Serie_Pru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.N_Serie_Pru, 10);
                entity.Codice_Pru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Pru, 10);
            
                WriteCheckLog(entity, result, Log);
                return result;
            }

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Pru");
            List<PowerMDBDataSet.PruRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                <PowerMDBDataSet.PruRow>(oDataSet.Pru.ToList(), "Pru", "Codice_PRU").OrderBy(acd => acd.Codice_PRU).ToList();
            List<Pru> toImport = new List<Pru>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>();
            string lastKey = "";
            if (accessData != null)
            {
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                foreach (PowerMDBDataSet.PruRow oRow in accessData)
                {
                    try
                    {
                        lastKey = oRow.Codice_PRU;
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                           BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "PRU",
                           "11", "17", oRow.Codice_PRU.ToString(), countRec.ToString(), nRec.ToString()));
                        Pru oNewRecord = Init();
                        oNewRecord.Codice_Pru = oRow.IsCodice_PRUNull() ? (string)null : oRow.Codice_PRU;
                        oNewRecord.Data_Registrazione_Pru = oRow.IsData_Registrazione_PRUNull() ? new DateTime(2000, 1, 1) : oRow.Data_Registrazione_PRU;
                        oNewRecord.DataOraUltimaModifica_Pru = DateTime.UtcNow;
                        oNewRecord.DisAbilitazione_Pru = oRow.IsDisAbilitazione_PRUNull() ? false : oRow.DisAbilitazione_PRU;
                        //oNewRecord.N_Serie_Pru = oRow.;                        
                        //oNewRecord.N_Serie_Pru = oRow.Matricola_PRU ;
                        //oNewRecord.Note_Pru = oRow.IsNote_PRUNull() ? (string)null : oRow.Note_PRU;
                        oNewRecord.Singola_Reg_Pru = oRow.IsSingola_RegNull() ? false : (oRow.Singola_Reg == 0 ? false : true);

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
                                Nome_Tabella_Tab_Check_Imp = "Pru",
                                Chiave_Record_Tab_Check_Imp = oRow.Codice_PRU,
                            });
                            log.Warn("Codice Unità Portatile cui si rifericono gli errori precedenti: " + oRow.Codice_PRU + " -------------------------------------------------------------------------------");
                            errorsList.Add(currentDictionary);
                            errorsList.Add(importDictionary);
                        }

                    }
                    catch (Exception ex)
                    {
                        var RRNERR = oRow.Codice_PRU;
                        //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                        if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                            Log.WarnFormat("TAB PRU_COL - Chiave: {0}", oRow.Codice_PRU);
                        throw ex;
                    }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                try
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "PRU", lastKey));
                    RepoManager.PruRepo.Add(toImport);
                    RepoManager.Tab_Chk_ImpRepo.Add(errors);
                    if (errors.Count == 0)
                        RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Pru",
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

        public Dictionary<string, string> ImportFromTXT(String[] inputFile)
        //Import delle Matricole delel Unità Fisse da File TXT       
        {
            //I record devono avare il seguente Tracciato
            //  MATRICOLA UNITA'
            //oppure
            //  MATRICOLA UNITA' + ";" + N° SERIE
            //oppure
            // Iniziare con "*" (record di Commento che vengono salatati
            //oppure
            // Lunghezza = 0 (Riche vuote che vengono saltate)
            //SEGNALAZIONI:
            // Le Matricole DOPPIE nel File TXT vengono ignorate ma segnalate
            // Le Matricole che esitono già nel DB vengono ignorate ma segnalate
            // Se il Record contiene più di 2 Campi separati da ";"
            string sTipoAss = string.Empty;
            int Progr = 0;
            Dictionary<string, string> errors = new Dictionary<string, string>();
            List<Pru> presentPrus = RepoManager.PruRepo.GetAll(true).ToList();
            List<Pru> toAddPrus = new List<Pru>();
            String[] splittedLine = new String[inputFile.Length];
           

            foreach (string inputLine in inputFile)
            {               

                if (!inputLine.StartsWith("*") && inputLine.Trim().Length != 0)
                {
                   //nel caso in cui ho un file genrato da COMO controllo se sono in presenza di MLC O COM
                    if ((inputLine.IndexOf("MLC"))!=-1)
                    {
                        //estraggo la matricola del dispositivo
                        var matricolaDispositivo = inputLine.Substring(inputLine.IndexOf("MLC") + 11, 5);
                        splittedLine = matricolaDispositivo.Split(';');
                    }

                    else if (inputLine.IndexOf("COM")!=-1)
                    {
                        //estraggo la matricola del dispositivo
                        var matricolaDispositivo = inputLine.Substring(inputLine.IndexOf("COM") + 11, 5);
                        splittedLine = matricolaDispositivo.Split(';');
                    }

                    else
                    {
                        splittedLine = inputLine.Split(';');
                    }

                    if (splittedLine.Length == 1)
                    //Tratta il caso in cui il File di TXT contiene SOLO la MATRICOLA dell'Unità Portatile
                    {
                        // nella stringa da processare con il codice sono eventualmente aggiunti gli spazi a sinistra
                        string keyToSearch = CommonService.AggiungiSpaziASinistraSeStringaNumerica(splittedLine[0].Trim(), 10);

                        if (!presentPrus.Any(Pru => Pru.Codice_Pru == keyToSearch))
                        {
                            if (!toAddPrus.Any(Pru => Pru.Codice_Pru == keyToSearch))
                            {
                                Pru currentPru = new Pru
                                {
                                    Codice_Pru = keyToSearch,
                                    Data_Registrazione_Pru = DateTime.UtcNow,
                                    DataOraUltimaModifica_Pru = DateTime.UtcNow,
                                    DisAbilitazione_Pru = false,
                                    N_Serie_Pru = string.Empty,
                                    Note_Pru = string.Empty,
                                    Singola_Reg_Pru = false,
                                };

                                RepoManager.PruRepo.SetEntityBeforeAddOrUpdate(currentPru);
                                errors = Check(currentPru, true, false);

                                //Verifico se ci sono stati errori
                                if (errors.Keys.Count == 0 )
                                toAddPrus.Add(currentPru);
                            }
                            else
                            {
                                //Caso in cui la Matricola è DOPPIA nel File di Input
                                Progr = Progr + 1;
                                //serve nel caso in cui ci siano + Righe con Valori UGUALI
                                errors.Add(inputLine + Progr, BusinessService.GetLocalizedString(PowerWebResources.ERR_MATRICOLA_DUPLICATA_NEL_FILE_DI_IMPORT));
                            }
                        }
                        else
                        {
                            //Caso in cui il Tracciato RECORD NON Corrisponde a quello previsto (che deve avere 1 o 2 Campi da trattare)
                            Progr = Progr + 1;
                            //serve nel caso in cui ci siano + Righe con Valori UGUALI
                            errors.Add(inputLine + Progr, BusinessService.GetLocalizedString(PowerWebResources.ERR_MATRICOLA_GIA_PRESENTE_NELLA_TABELLA));
                        }
                    }
                  
                    else if (splittedLine.Length == 2)
                    //Tratta il caso in cui il File di TXT contiene oltre alla MATRICOLA ache il N° di Serie dell'Unità Portatile
                    {
                        // nella stringa da processare con i codice sono eventualmente aggiunti gli spazi a sinistra
                        string firstKeyToSearch = CommonService.AggiungiSpaziASinistraSeStringaNumerica(splittedLine[0].Trim(), 10);
                        string secondKeyToSearch = CommonService.AggiungiSpaziASinistraSeStringaNumerica(splittedLine[1].Trim(), 10);

                        if (!presentPrus.Any(Pru => Pru.Codice_Pru == firstKeyToSearch || Pru.N_Serie_Pru == secondKeyToSearch))
                        {
                            if (!toAddPrus.Any(Pru => Pru.Codice_Pru == firstKeyToSearch || Pru.N_Serie_Pru == secondKeyToSearch))
                            {
                                Pru currentPru = new Pru
                                {
                                    Codice_Pru = firstKeyToSearch,
                                    Data_Registrazione_Pru = DateTime.UtcNow,
                                    DataOraUltimaModifica_Pru = DateTime.UtcNow,
                                    DisAbilitazione_Pru = false,
                                    N_Serie_Pru = secondKeyToSearch,
                                    Note_Pru = string.Empty,
                                    Singola_Reg_Pru = false,
                                };
                                RepoManager.PruRepo.SetEntityBeforeAddOrUpdate(currentPru);
                                errors = Check(currentPru, true, false);
                                //Verifico se ci sono stati errori
                                if (errors.Keys.Count == 0)
                                    toAddPrus.Add(currentPru);
                                toAddPrus.Add(currentPru);
                            }
                            else
                            {
                                //Caso in cui la Matricola o il N° di Serie sono DOPPI nel File di Input
                                Progr = Progr + 1;
                                //serve nel caso in cui ci siano + Righe con Valori UGUALI
                                errors.Add(inputLine + Progr, BusinessService.GetLocalizedString(PowerWebResources.ERR_MATRICOLA_O_SERIE_DUPLICATA_NEL_FILE_DI_IMPORT));
                            }
                        }
                        else
                        {
                            //caso in cui la Matricola o il N° di Serie esista già
                            Progr = Progr + 1;
                            //serve nel caso in cui ci siano + Righe con Valori UGUALI
                            errors.Add(inputLine + Progr, BusinessService.GetLocalizedString(PowerWebResources.ERR_MATRICOLA_O_SERIE_GIA_PRESENTE_NELLA_TABELLA));
                        }
                    }
                    else
                        //Caso in cui il Tracciato RECORD NON Corrisponde a quello rpevisto (che deve avere 1 o 2 Campi da trattare
                    {
                        Progr = Progr + 1;
                        //serve nel caso in cui ci siano + Righe con Valori UGUALI
                        errors.Add(inputLine + Progr, BusinessService.GetLocalizedString(PowerWebResources.ERR_STRINGA_NON_CONFORME));
                    }
                }
            }
            RepoManager.PruRepo.Add(toAddPrus, true);
            return errors;
        }
    }
}
