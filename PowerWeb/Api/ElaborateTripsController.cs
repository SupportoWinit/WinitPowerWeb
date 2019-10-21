using System;
using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using Common;
using log4net;

namespace PowerWeb.Api
{
    /// <summary>
    /// Classe utilizzata per esporre tramite web api la procedura di elaborazione
    /// </summary>
    public class ElaborateTripsController : GenericApi
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Domain.Reg));


        #region Protected Methods

        /// <summary>
        /// Esegue l'operazione di elaborazione dei viaggi con i parametri specificati nella classe padre.
        /// </summary>
        protected override void ExecuteOperation()
        {
            _log.Info("INZIO ELABORAZIONE VIAGGI SCHEDULATA");

            // leggo l'elenco delle reg_v del periodo da database che non sono bloccate e sono abbinate
            var regVs = RepoManager.Reg_VRepo.Find(regv => (regv.Data_Ora_Fis_E >= From && regv.Data_Ora_Fis_U <= To && !regv.Registrazione_Bloccata 
                && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass), true).ToList();
            
            // se sono presenti delle registrazioni da processare allora procedo ad effettuare l'elaborazione;
            // in caso contrario si segnala l'assenza di timbrature
            if (regVs.Any())
            {
                List<KeyValuePair<string, string>> errors = RepoManager.Reg_VRepo.ElaborateTrips(regVs, true);
                if (errors.Any())
                    Errors.AddRange(errors);
                else
                    Errors.Add(new KeyValuePair<string, string>("RegVs", String.Format("Elaborazione viaggi completata senza errori nel periodo (from: {0}; to: {1})", From, To)));
            }
            else
                Errors.Add(new KeyValuePair<string, string>("RegVs", String.Format("Nessuna registrazione presente nel periodo (from: {0}; to: {1})", From, To)));

            _log.Info("TERMINE ELABORAZIONE VIAGGI SCHEDULATA");

        }

        #endregion
    }
}