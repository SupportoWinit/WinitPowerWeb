using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;
using Business.MDBSchema;
using log4net;
using Microsoft.Practices.ObjectBuilder2;
using System.Data.Entity.Infrastructure;
using System.Reflection;
using System.Data.Entity;

namespace Business.Repository.Custom
{
    public class FruRepository : GenericRepository<Fru>, IFruRepository
    {
        public FruRepository(PowerWebEntities context)
            : base(context)
        {
        }

        //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
        private static DateTime DataOraRecord;

        private static void ResetSession()
        {
        }

        public override Fru Init()
        {
            Fru oNewRecord = base.Init();
            oNewRecord.DisAbilitazione_Fru = false;
            oNewRecord.Singola_Reg_Fru = false;
            oNewRecord.Data_Registrazione_Fru = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Fru = DateTime.UtcNow;
            return oNewRecord;
        }

        public override void SetEntityBeforeAddOrUpdate(Fru entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Fru;
            entity.DataOraUltimaModifica_Fru = DateTime.UtcNow;
            //nel caso in cui non sia stato inserito il n° di serie allora viene impostato uguale al codice unità
            if (string.IsNullOrEmpty(entity.N_Serie_Fru))
                if (!string.IsNullOrEmpty(entity.Codice_Fru))
                    entity.N_Serie_Fru = entity.Codice_Fru;
            entity.N_Serie_Fru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.N_Serie_Fru, 10);
            entity.Codice_Fru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Fru, 10);
        }

        
        public override void Delete(Fru entity, bool saveChanges = false)
        {
            if (RepoManager.RegRepo.Find(reg => reg.Fru_Id == entity.Fru_Id, true).Count() == 0)
                base.Delete(entity, saveChanges);
        }

        public override Dictionary<string, string> Check(Fru entity, bool isNew = false, bool isResetSession = true)
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
                    DateTime DataOraRecordDb = RepoManager.FruRepo.Single(u => u.Fru_Id == entity.Fru_Id).DataOraUltimaModifica_Fru;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Fru),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_FRU));
                    }
                }
                //
                // 1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                if (String.IsNullOrEmpty(entity.Codice_Fru))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fru),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_FRU));
                else
                {
                    entity.Codice_Fru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Fru, 10);
                    if (isNew)
                    {
                        if (RepoManager.FruRepo.SingleOrDefault(u => u.Codice_Fru == entity.Codice_Fru) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fru),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.FruRepo.SingleOrDefault(u => u.Codice_Fru == entity.Codice_Fru && u.Fru_Id != entity.Fru_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fru),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //1.1) verifico che gli altri Campi siano Univoci
                //           
                if (string.IsNullOrEmpty(entity.N_Serie_Fru))
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.N_Serie_Fru),
                                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_N_SERIE_FRU));
                }
                else
                {
                    entity.N_Serie_Fru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.N_Serie_Fru, 10);
                    if (RepoManager.FruRepo.FirstOrDefault(x => x.N_Serie_Fru == entity.N_Serie_Fru && x.Codice_Fru != entity.Codice_Fru && entity.Fru_Id != x.Fru_Id) != null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.N_Serie_Fru),
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
                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Codice_Fru, "") != "")
                    if (entity.Codice_Fru.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fru),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_FRU, PowerWebResources.VALORE_10));
                if (CommonService.Nz(entity.N_Serie_Fru, "") != "")
                    if (entity.N_Serie_Fru.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.N_Serie_Fru),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_N_SERIE_FRU, PowerWebResources.VALORE_10));
                //
                //5) verifico, per una serie di campi, che il valore del campo sia presente nella relativa Tabella
                if (CommonService.Nz(entity.Codice_Fru, "") != "")
                {
                    var codiceVerifica = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Fru, 10);
                    if (RepoManager.PruRepo.SingleOrDefault(pr => pr.Codice_Pru == codiceVerifica) != null)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fru),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_NON_DEVE_ESISTERE_IN_TABELLA_Y,
                        PowerWebResources.FLD_CODICE_FRU, PowerWebResources.STR_PRU));
                    }
                }

                if (CommonService.Nz(entity.N_Serie_Fru, "") != "")
                {
                    var codiceVerifica = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.N_Serie_Fru, 10);
                    if (RepoManager.PruRepo.SingleOrDefault(pr => pr.N_Serie_Pru == codiceVerifica) != null)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.N_Serie_Fru),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_NON_DEVE_ESISTERE_IN_TABELLA_Y,
                        PowerWebResources.FLD_N_SERIE_FRU, PowerWebResources.STR_PRU));
                    }
                }

                //5.1) verifico, per una serie di campi, che il valore del campo sia presente nella relativa tabella 
                //    e modifico il valore anche di altri campi
                //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);
            }
            catch (Exception ex)
            {
                var CodErr = "Fru_Id: " + entity.Fru_Id;
                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Fru entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            //Verifico SOLO x IMPORT la Validità delle eventuali Date Ricevute
            if (entity.Data_Registrazione_Fru < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Fru),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_FRU));
            if (entity.DataOraUltimaModifica_Fru < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Fru),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_FRU));
            if (string.IsNullOrEmpty(entity.N_Serie_Fru))
                if (!string.IsNullOrEmpty(entity.Codice_Fru))
                    entity.N_Serie_Fru = entity.Codice_Fru;
            entity.N_Serie_Fru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.N_Serie_Fru, 10);
            entity.Codice_Fru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Fru, 10);
            WriteCheckLog(entity, result, Log);
            return result;
        }

        public List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, String tableName, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Fru");
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>();
            string lastKey = "";


            if (tableName == "Fru")
            {
                List<PowerMDBDataSet.FruRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
    <PowerMDBDataSet.FruRow>(oDataSet.Fru.ToList(), "Fru", "Codice_FRU").OrderBy(acd => acd.Codice_FRU).ToList();
                List<Fru> toImport = new List<Fru>();

                if (accessData != null)
                {
                    double nRec = accessData.Count;
                    double countRec = 0;
                    double percRec = 0;
                    foreach (PowerMDBDataSet.FruRow oRow in accessData)
                    {
                        try
                        {
                            lastKey = oRow.Codice_FRU;
                            countRec = countRec + 1;
                            percRec = (countRec / nRec) * 100;
                            BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                                BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "FRU",
                                "12", "17", oRow.Codice_FRU.ToString(), countRec.ToString(), nRec.ToString()));
                            Fru oNewRecord = Init();
                            oNewRecord.Codice_Fru = oRow.IsCodice_FRUNull() ? (string)null : oRow.Codice_FRU;
                            oNewRecord.Data_Registrazione_Fru = oRow.IsData_Registrazione_FRUNull() ? new DateTime(2000, 1, 1) : oRow.Data_Registrazione_FRU;
                            oNewRecord.DataOraUltimaModifica_Fru = DateTime.UtcNow;
                            oNewRecord.DisAbilitazione_Fru = oRow.IsDisAbilitazione_FRUNull() ? false : oRow.DisAbilitazione_FRU;
                            oNewRecord.N_Serie_Fru = oRow.IsNote_FruNull() ? (string)null : oRow.Note_Fru;
                            //oNewRecord.Note_Fru = oRow.IsNote_FruNull() ? (string)null : oRow.Note_Fru;
                            oNewRecord.Singola_Reg_Fru = oRow.IsSingola_RegNull() ? false : (oRow.Singola_Reg == 0 ? false : true);
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
                                    Nome_Tabella_Tab_Check_Imp = "Fru",
                                    Chiave_Record_Tab_Check_Imp = oRow.Codice_FRU,
                                });
                                log.Warn("Codice Unità Fissa cui si rifericono gli errori precedenti: " + oRow.Codice_FRU + " -------------------------------------------------------------------------------");
                                errorsList.Add(currentDictionary);
                                errorsList.Add(importDictionary);
                            }

                        }
                        catch (Exception ex)
                        {
                            var RRNERR = oRow.Codice_FRU;
                            //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                            if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                                Log.WarnFormat("TAB FRU - Chiave: {0}", oRow.Codice_FRU);
                            throw ex;
                        }
                    }
                    RepoManager.Tab_Chk_ImpRepo.BeginWork();
                    try
                    {
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                            BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "FRU", lastKey));
                        RepoManager.FruRepo.Add(toImport);
                        RepoManager.Tab_Chk_ImpRepo.Add(errors);
                        if (errors.Count == 0)
                            RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                            {
                                Nome_Tabella_Tab_Check_Imp = "Fru",
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
            }

            if (tableName == "MatrFru") // importazione delle matricole delle unità fisse presenti nella tabella MatFru (aggiornamento fru importate)
            {
                List<PowerMDBDataSet.MatrFruRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
<PowerMDBDataSet.MatrFruRow>(oDataSet.MatrFru.ToList(), "MatrFru", "Matricola_MatrFru").OrderBy(acd => acd.Matricola_MatrFru).ToList();
                List<Fru> frusToUpdate = new List<Fru>();
                if (accessData != null)
                {
                    double nRec = accessData.Count;
                    double countRec = 0;
                    double percRec = 0;

                    foreach (PowerMDBDataSet.MatrFruRow oRow in accessData)
                    {
                        try
                        {
                            lastKey = oRow.Matricola_MatrFru;
                            countRec = countRec + 1;
                            percRec = (countRec / nRec) * 100;
                            BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                                BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "MATRFRU",
                                "13", "17", oRow.Matricola_MatrFru, countRec.ToString(), nRec.ToString()));

                            // recupero di tutte le matricole corrispondenti
                            var frusToEdit = RepoManager.FruRepo.Find(fru => fru.Codice_Fru == oRow.Matricola_MatrFru);

                            // se sono state trovate delle fru, si procede al loro aggiornamento del numero di serie
                            // e alla loro segnalazione per l'update
                            if (frusToEdit.Any())
                                frusToEdit.ForEach(fru =>
                                {
                                    if (!oRow.IsNumero_Serie_MatrFRUNull())
                                        fru.N_Serie_Fru = oRow.Numero_Serie_MatrFRU.ToString();
                                    else
                                        fru.N_Serie_Fru = "0";
                                    frusToUpdate.Add(fru);
                                });
                        }
                        catch (Exception ex)
                        {
                            var RRNERR = oRow.Matricola_MatrFru;
                            //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                            if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                                Log.WarnFormat("TAB FRU - Chiave: {0}", oRow.Matricola_MatrFru);
                            throw ex;
                        }

                    }

                    RepoManager.Tab_Chk_ImpRepo.BeginWork();
                    try
                    {
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                            BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "MATRFRU", lastKey));
                        RepoManager.FruRepo.Update(frusToUpdate);
                        RepoManager.Tab_Chk_ImpRepo.Add(errors);
                        if (errors.Count == 0)
                            RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                            {
                                Nome_Tabella_Tab_Check_Imp = "MatrFru",
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
            //  Iniziare con "*" (record di Commento che vengono salatati
            //oppure
            //  Lunghezza = 0 (Riche vuote che vengono saltate)
            //SEGNALAZIONI:
            // Le Matricole DOPPIE nel File TXT vengono ignorate ma segnalate
            // Le Matricole che esitono già nel DB vengono ignorate ma segnalate
            // Se il Record contiene più di 2 Campi separati da ";"

            string sTipoAss = string.Empty;
            Dictionary<string, string> errors = new Dictionary<string, string>();
            List<Fru> presentFrus = RepoManager.FruRepo.GetAll(true).ToList();
            List<Fru> toAddFrus = new List<Fru>();
            String[] splittedLine = new String[inputFile.Length];

            foreach (string inputLine in inputFile)
            {
                if (!inputLine.StartsWith("*") && inputLine.Trim().Length != 0)
                {
                    //nel caso in cui ho un file genrato da COMO controllo se sono in presenza di MLC O COM
                    if ((inputLine.IndexOf("MLC")) != -1)
                    {
                        //estraggo la matricola del dispositivo
                        var matricolaDispositivo = inputLine.Substring(inputLine.IndexOf("MLC") + 11, 5);
                        splittedLine = matricolaDispositivo.Split(';');
                    }

                    else if (inputLine.IndexOf("COM") != -1)
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
                    {
                        // è calcolata la stringa di elaborazione aggiungendo eventualmente gli spazi a sinistra
                        string keyToSearch = CommonService.AggiungiSpaziASinistraSeStringaNumerica(splittedLine[0].Trim(), 10);

                        if (!presentFrus.Any(fru => fru.Codice_Fru == keyToSearch))
                        {
                            if (!toAddFrus.Any(fru => fru.Codice_Fru == keyToSearch))
                            {
                                Fru currentFru = new Fru
                                {
                                    Codice_Fru = keyToSearch,
                                    Data_Registrazione_Fru = DateTime.UtcNow,
                                    DataOraUltimaModifica_Fru = DateTime.UtcNow,
                                    DisAbilitazione_Fru = false,
                                    N_Serie_Fru = string.Empty,
                                    Note_Fru = string.Empty,
                                    Singola_Reg_Fru = false,
                                };
                                RepoManager.FruRepo.SetEntityBeforeAddOrUpdate(currentFru);
                                errors = Check(currentFru, true, false);

                                //Verifico se ci sono stati errori
                                if (errors.Keys.Count == 0)
                                    toAddFrus.Add(currentFru);
                            }
                            else
                            {

                                errors.Add(inputLine, BusinessService.GetLocalizedString(PowerWebResources.ERR_MATRICOLA_DUPLICATA_NEL_FILE_DI_IMPORT));
                            }
                        }
                        else
                        {
                            errors.Add(inputLine, BusinessService.GetLocalizedString(PowerWebResources.ERR_MATRICOLA_GIA_PRESENTE_NELLA_TABELLA));
                        }
                    }
                    else if (splittedLine.Length == 2)
                    {
                        // sono calcolate le stringhe di inserimento e ricerca con gli spazi per l'allineamento a sinistra
                        string firstKeyToSearch = CommonService.AggiungiSpaziASinistraSeStringaNumerica(splittedLine[0].Trim(), 10);
                        string secondKeyToSearch = CommonService.AggiungiSpaziASinistraSeStringaNumerica(splittedLine[1].Trim(), 10);

                        if (!presentFrus.Any(fru => fru.Codice_Fru == firstKeyToSearch || fru.N_Serie_Fru == secondKeyToSearch))
                        {
                            if (!toAddFrus.Any(fru => fru.Codice_Fru == firstKeyToSearch || fru.N_Serie_Fru == secondKeyToSearch))
                            {
                                Fru currentFru = new Fru
                                {
                                    Codice_Fru = firstKeyToSearch,
                                    Data_Registrazione_Fru = DateTime.UtcNow,
                                    DataOraUltimaModifica_Fru = DateTime.UtcNow,
                                    DisAbilitazione_Fru = false,
                                    N_Serie_Fru = secondKeyToSearch,
                                    Note_Fru = string.Empty,
                                    Singola_Reg_Fru = false,
                                };
                                RepoManager.FruRepo.SetEntityBeforeAddOrUpdate(currentFru);
                                errors = Check(currentFru, true, false);

                                //Verifico se ci sono stati errori
                                if (errors.Keys.Count == 0)
                                    toAddFrus.Add(currentFru);
                                toAddFrus.Add(currentFru);
                            }
                            else
                            {

                                errors.Add(inputLine, BusinessService.GetLocalizedString(PowerWebResources.ERR_MATRICOLA_O_SERIE_DUPLICATA_NEL_FILE_DI_IMPORT));
                            }
                        }
                        else
                        {
                            errors.Add(inputLine, BusinessService.GetLocalizedString(PowerWebResources.ERR_MATRICOLA_O_SERIE_GIA_PRESENTE_NELLA_TABELLA));
                        }
                    }
                    else
                    {
                        errors.Add(inputLine, BusinessService.GetLocalizedString(PowerWebResources.ERR_STRINGA_NON_CONFORME));
                    }

                }
            }
            RepoManager.FruRepo.Add(toAddFrus, true);
            return errors;
        }
    }
}
