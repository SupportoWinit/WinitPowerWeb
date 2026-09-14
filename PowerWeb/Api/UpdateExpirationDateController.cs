using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading;
using System.Web;
using System.Web.Http;
using Business;
using Business.DataClasses.SupportClasses;
using Business.Profile;
using Business.Repository;
using Common;
using Domain;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Business.DataClasses.WebApiDataClasses;
using static DevExpress.Xpo.Helpers.AssociatedCollectionCriteriaHelper;
using DevExpress.Web.ASPxTitleIndex.Internal;
using System.Web.Helpers;

namespace PowerWeb.Api
{

    /// <summary>
    /// Classe utilizzata per esporre tramite web api la procedura di aggiornamento della data di scadenza
    /// </summary>
    public class UpdateExpirationDateController : GenericApi
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
            _log.Info("Inizio ad aggiornare la password");
            ReturnValues values = new ReturnValues();
            bool result = RepoManager.ParamRepo.UpdateExpirationDate(Date);

            _log.Info("Una volta fatto l'aggiornamento controllo se è andato a buon fine");
            var param = RepoManager.ParamRepo.GetAll().First();

            if (result)
            {
                values.Status = true;
                values.Message = "Licenza aggiornata correttamente per il cliente " + param.CompanyName;
            }
            else
            {
                values.Status = false;
                values.Message = "Impossibile aggiornare la licenza per il cliente " + param.CompanyName;
            }
            _log.Info("Fine aggiornamento password");

            JsonData = JsonConvert.SerializeObject(values);
        }
    }
}