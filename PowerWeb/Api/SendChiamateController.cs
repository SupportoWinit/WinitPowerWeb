using System;
using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using System.IO;
using Business;
using Common;

namespace PowerWeb.Api
{

    /// <summary>
    /// Classe utilizzata per esporre tramite web api la procedura di invio mail dei collaboratori ritardatari
    /// </summary>
    public class SendChiamateController : GenericApi
    {


        /// <summary>
        /// Il nome del file utilizzato per effettuare i controlli di esecuzione esclusiva della web api
        /// </summary>
        private const string ExclusiveAccessFileName = "apiLock.tmp";

        #region Protected Methods

        /// <summary>
        /// Esegue l'operazione di invio mail dei collaboratori ritardatari
        /// </summary>
        protected override void ExecuteOperation()
        {
            DateTime from = DateTime.Today;
            DateTime to = from.AddDays(1);

            #region IMPORT DEI FILE DAL SERVER
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

                    // Inizializzaizone del file che conterrà le reg sospese
                    string regSuspendedFile = Path.Combine(filesInputPath, String.Format("{0}_{1}.txt", Common.Properties.Settings.Default.SuspendedRegsFile, DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss")));

                    // Flag richiesto obbligatoriamente dal metodo CalcolaFilesRegDaImportare, ma non utilizzato in questa routine
                    bool fittizzio;
                    // calcolo dell'elenco dei file da importare
                    List<string> filesToImportList = BusinessService.CalcolaFilesRegDaImportare(filesInputPath, out fittizzio);

                    // se ci sono dei files da importare allora si procede alla loro elaborazione
                    if (filesToImportList.Any())
                    {
                        // recupero tutte le timbrature non gps presenti nei file da importare
                        List<string> regNoGpsToImport = BusinessService.GetRegsNoGpsFromFiles(filesToImportList);

                        // recupero tutte le timbrature gps presenti nei file da importare
                        List<string> regGpsToImport = BusinessService.GetRegsGpsFromFiles(filesToImportList);

                        // se ci sono delle timbrature da importare allora si procede all'importazione
                        var importErrors = new List<KeyValuePair<string, string>>();
                        if (regNoGpsToImport.Any())
                        {
                            importErrors.AddRange(RepoManager.RegRepo.Import(regNoGpsToImport.ToArray(), regGpsToImport.ToArray()));
                        }
                        else // altrimenti si segnala l'informazione
                        {
                            Errors.Add(new KeyValuePair<string, string>("Import", "Nessuna timbratura da importare"));
                        }

                        // effettuazione del backup di tutti i file della lista
                        string backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Input_Backup_Path.Replace("~", "").ReplaceFirst("\\", ""));

                        // backup dei files processati
                        BusinessService.BackupProcessedFiles(filesToImportList, backupFolder);

                        // se si sono verificati degli errori allora si creano le registrazioni sospese
                        if (importErrors.Count > 0)
                            BusinessService.CreateSuspendedRegFile(regSuspendedFile, importErrors);
                    }
                    else // altrimenti si segnala l'informazione
                    {
                        Errors.Add(new KeyValuePair<string, string>("Import", String.Format("Nessun file da importare nella cartella {0}", filesInputPath)));
                    }
                }
                catch (Exception ex)
                {
                    Errors.Add(new KeyValuePair<string, string>("Import", String.Format("Eccezione nell'esecuzione della web api: {0} - {1}", ex.GetType(), ex.Message)));
                }
                finally
                {
                    // al termine dell'operazione comunque si elimina il file
                    File.Delete(exclusiveAccessFilePath);
                }
            }
            else
            {
                Errors.Add(new KeyValuePair<string, string>("Import", "E' indicata un'altra web api in esecuzione"));
            }

            #endregion

            #region ELABORAZIONE DELLE TIMBRATURE
            // calcolo delle registrazioni da processare in elaborazione prendendo tutto ciò che cade nel periodo passato come parametro
            var regs = RepoManager.RegRepo.Find(r => r.Registrazione_Data_Ora_Fis_Reg >= from && r.Registrazione_Data_Ora_Fis_Reg < to, true).ToList();

            // si procede ad elaborare solamente se sono presenti delle registrazioni
            if (regs.Any())
                Errors.AddRange(RepoManager.RegRepo.Elaborate(regs, from, to, true, true));
            else
            {
                Errors.Add(new KeyValuePair<string, string>("Regs", String.Format("Nessuna registrazione presente nel periodo (from: {0}; to: {1})", From, To)));
            }
            #endregion

            #region INVIO DELLE CHIAMATE
            Errors.Add(new KeyValuePair<string, string>("Delay", RepoManager.Reg_VRepo.InviaChiamate()));
            #endregion
        }

        #endregion

    }

}