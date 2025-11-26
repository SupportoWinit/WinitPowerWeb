using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Business.Repository;
using log4net;

namespace PowerWeb.Api
{

    /// <summary>
    /// Classe utilizzata per esporre tramite web api la procedura di elaborazione
    /// </summary>
    public class ElaborateController : GenericApi
    {

        private static readonly ILog _log = LogManager.GetLogger(typeof(Domain.Reg));

        /// <summary>
        /// Il nome del file utilizzato per effettuare i controlli di esecuzione esclusiva della web api
        /// </summary>
        private const string ExclusiveAccessFileName = "apiLock.tmp";

        #region Protected Methods

        /// <summary>
        /// Esegue l'operazione di elaborate con i parametri specificati nella classe padre
        /// </summary>
        protected override void ExecuteOperation()
        {
            _log.Info("INIZIO ELABORAZIONE SCHEDULATA");
            if (!RepoManager.ParamRepo.GetAll().First().Elaborate_Semaforo)
            {
                // calcolo del percorso di files input
                string filesInputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Input_Path.Replace("~", "").Replace("\\", ""));

                // calcolo del nome del file utilizzato per l'esecuzione esclusiva delle operazioni
                string exclusiveAccessFilePath = Path.Combine(filesInputPath, ExclusiveAccessFileName);

                // si procede all'elaborazione solamente se il file non esiete
                if (!File.Exists(exclusiveAccessFilePath))
                {
                    // calcolo delle registrazioni da processare in elaborazione prendendo tutto ciò che cade nel periodo passato come parametro
                    List<Domain.Reg> regs = RepoManager.RegRepo.Find(r => r.Registrazione_Data_Ora_Fis_Reg >= From && r.Registrazione_Data_Ora_Fis_Reg <= To, true).ToList();

                    // si procede ad elaborare solamente se sono presenti delle registrazioni
                    if (regs.Any())
                    {
                        try
                        {
                            Errors.AddRange(RepoManager.RegRepo.Elaborate(regs, From, To, true, true));
                        }
                        catch (Exception e)
                        {
                            _log.Error(String.Format("Errore elaborate schedulato con exception {0} ", e.Message));
                        }
                        Errors.Add(new KeyValuePair<string, string>("Elaborate", "Elaborazione eseguita con successo"));

                    }
                    else
                    {
                        Errors.Add(new KeyValuePair<string, string>("Regs", String.Format("Nessuna registrazione presente nel periodo (from: {0}; to: {1})", From, To)));
                    }
                }
                else 
                {
                    _log.Warn(String.Format("Elaborate schedulato bloccato, altra Web API in corso"));
                    Errors.Add(new KeyValuePair<string, string>("Import", "E' indicata un'altra web api in esecuzione"));
                }
            }
            else
            {
                _log.Warn(String.Format("Elaborate schedulato bloccato a causa del semaforo"));
                Errors.Add(new KeyValuePair<string, string>("Elaborate", "Elaborazione bloccata causa semaforo attivo"));
            }

            _log.Info("TERMINE ELABORAZIONE SCHEDULATA");
        }

        #endregion

    }

}