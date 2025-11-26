using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Http;
using Business;
using Business.ExternalImport;
using Business.Profile;
using Business.Repository;
using Common;
using Domain;
using log4net;
using Westwind.Utilities.Extensions;

namespace PowerWeb.Api
{

    /// <summary>
    /// Classe utilizzata per esporre tramite web api la procedura di importazione file con le registrazioni
    /// </summary>
    public class ImportController : GenericApi
    {

        /// <summary>
        /// Il nome del file utilizzato per effettuare i controlli di esecuzione esclusiva della web api
        /// </summary>
        private const string ExclusiveAccessFileName = "apiLock.tmp";

        private static readonly ILog _log = LogManager.GetLogger(typeof(Domain.Reg));
        /// <summary>
        /// Esegue l'operazione di import oggetto della API.
        /// </summary>
        protected override void ExecuteOperation()
        {
            if (!RepoManager.ParamRepo.GetAll().First().Elaborate_Semaforo)
            {
                //RepoManager.ParamRepo.GetAll().First().Elaborate_Semaforo = true;
                //RepoManager.ParamRepo.SaveChanges();

                // calcolo del percorso di files input
                string filesInputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Input_Path.Replace("~", "").Replace("\\", ""));

                // calcolo del nome del file utilizzato per l'esecuzione esclusiva delle operazioni
                string exclusiveAccessFilePath = Path.Combine(filesInputPath, ExclusiveAccessFileName);

                // si procede all'elaborazione solamente se il file non esiete
                if (!File.Exists(exclusiveAccessFilePath))
                {
                    try
                    {
                        // viene generato il file di lock della api
                        using (FileStream exclusiveExecutionFile = File.Create(exclusiveAccessFilePath))
                        {
                            exclusiveExecutionFile.Close();
                            exclusiveExecutionFile.Dispose();
                        }

                        //Vengono richieste le timbrature da un server esterno e creato il relativo txt
                        try
                        {
                            ExternalImportManager.GetFromRemoteSource();
                        }
                        catch (Exception ex)
                        {
                            _log.ErrorFormat("Errori durante l'import da server esterno : {0}", ex.Message);
                        }

                        // Inizializzaizone del file che conterrà le reg sospese
                        string regSuspendedFile = Path.Combine(filesInputPath, String.Format("{0}_{1}.txt", Common.Properties.Settings.Default.SuspendedRegsFile, DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss")));

                        // Flag richiesto obbligatoriamente dal metodo CalcolaFilesRegDaImportare, ma non utilizzato in questa routine
                        bool fittizzio;

                        bool importaSospese = RepoManager.ParamRepo.ParametersRow.ImportaFileTimbratureSospese;

                        // calcolo dell'elenco dei file da importare
                        List<string> filesToImportList = BusinessService.CalcolaFilesRegDaImportare(filesInputPath, out fittizzio, importaSospese);

                        // se ci sono dei files da importare allora si procede alla loro elaborazione
                        if (filesToImportList.Any())
                        {
                            // recupero tutte le timbrature non gps presenti nei file da importare
                            List<string> regNoGpsToImport = BusinessService.GetRegsNoGpsFromFiles(filesToImportList);

                            // recupero tutte le timbrature gps presenti nei file da GetRegsGpsFromFilesimportare
                            List<string> regGpsToImport = BusinessService.GetRegsGpsFromFiles(filesToImportList);

                            // se ci sono delle timbrature da importare allora si procede all'importazione
                            var importErrors = new List<KeyValuePair<string, string>>();
                            if (regNoGpsToImport.Any() || regGpsToImport.Any())
                            {
                                importErrors.AddRange(RepoManager.RegRepo.Import(regNoGpsToImport.ToArray(), regGpsToImport.ToArray()));

                                int _elaborateUserId = PowerWebContext.Current.User.Utenti_Id;
                                DateTime _elaborateDateTime = DateTime.Now;
                                List<KeyValuePair<string, string>> elabErrors = new List<KeyValuePair<string, string>>();
                                
                                DateTime toChunk = DateTime.Now.EndOfDay();
                                DateTime fromChunk = DateTime.Now.BeginningOfMonth();

                                List<Reg> toElaborateRegs = null;

                                toElaborateRegs = RepoManager.RegRepo.Find(reg => reg.Registrazione_Data_Ora_Fis_Reg >= fromChunk && reg.Registrazione_Data_Ora_Fis_Reg < toChunk, true).ToList();

                                _log.Info(String.Format("Trovate {0} regs.", toElaborateRegs.Count));

                                elabErrors.AddRange(RepoManager.RegRepo.Elaborate(toElaborateRegs, fromChunk, toChunk, true, true, _elaborateUserId, _elaborateDateTime, application: ApplicationMessageEnum.Elaborate));
                            }
                            else // altrimenti si segnala l'informazione
                                Errors.Add(new KeyValuePair<string, string>("Import", "Nessuna timbratura da importare"));

                            // effettuazione del backup di tutti i file della lista
                            string backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Input_Backup_Path.Replace("~", "").ReplaceFirst("\\", ""));

                            // backup dei files processati
                            BusinessService.BackupProcessedFiles(filesToImportList, backupFolder);

                            // se si sono verificati degli errori allora si creano le registrazioni sospese
                            if (importErrors.Count > 0)
                                BusinessService.CreateSuspendedRegFile(regSuspendedFile, importErrors);
                        }
                        else // altrimenti si segnala l'informazione
                            Errors.Add(new KeyValuePair<string, string>("Import", String.Format("Nessun file da importare nella cartella {0}", filesInputPath)));
                    }
                    catch (Exception ex)
                    {
                        _log.Error(String.Format("Errore import schedulato con exception {0} ", ex.Message));
                        Errors.Add(new KeyValuePair<string, string>("Import", String.Format("Eccezione nell'esecuzione della web api: {0} - {1}", ex.GetType(), ex.Message)));
                    }
                    finally
                    {
                        // al termine dell'operazione comunque si elimina il file
                        File.Delete(exclusiveAccessFilePath);
                        try {
                            //RepoManager.ParamRepo.GetAll().First().Elaborate_Semaforo = false;
                            //RepoManager.ParamRepo.SaveChanges();
                        } 
                        catch (Exception e) {
                            _log.Error(String.Format("Errore import schedulato con exception {0} ", e.Message));
                        }      
                    }
                }
                else
                {
                    Errors.Add(new KeyValuePair<string, string>("Import", "E' indicata un'altra web api in esecuzione"));
                }

                try {
                    //RepoManager.ParamRepo.GetAll().First().Elaborate_Semaforo = false;
                    //RepoManager.ParamRepo.SaveChanges();
                }
                catch (Exception e)
                {
                    _log.Error(String.Format("Errore import schedulato con exception {0} ", e.Message));
                }
                
            }else
            {
                _log.Warn(String.Format("Import schedulato bloccato a causa del semaforo"));
                Errors.Add(new KeyValuePair<string, string>("Import", "Importazione bloccata causa semaforo attivo"));
            }
        }

    }
}