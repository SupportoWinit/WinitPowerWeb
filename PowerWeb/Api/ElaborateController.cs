using System;
using System.Collections.Generic;
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

        #region Protected Methods

        /// <summary>
        /// Esegue l'operazione di elaborate con i parametri specificati nella classe padre
        /// </summary>
        protected override void ExecuteOperation()
        {
            _log.Info("INIZIO ELABORAZIONE SCHEDULATA");

            // calcolo delle registrazioni da processare in elaborazione prendendo tutto ciò che cade nel periodo passato come parametro
            List<Domain.Reg> regs = RepoManager.RegRepo.Find(r => r.Registrazione_Data_Ora_Fis_Reg >= From && r.Registrazione_Data_Ora_Fis_Reg <= To, true).ToList();

            // si procede ad elaborare solamente se sono presenti delle registrazioni
            if (regs.Any())
                Errors.AddRange(RepoManager.RegRepo.Elaborate(regs, From, To, true, true));
            else
            {
                Errors.Add(new KeyValuePair<string, string>("Regs", String.Format("Nessuna registrazione presente nel periodo (from: {0}; to: {1})", From, To)));
            }

            _log.Info("TERMINE ELABORAZIONE SCHEDULATA");
        }

        #endregion

    }

}