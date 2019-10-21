using Business.BusinessClasses;
using Business.BusinessClasses.CartellinoServiceDTOs;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.TimesheetService.Helpers
{
    public static class SaveUpdatesHelper
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(SaveUpdatesHelper));

        public static bool ValidateColCartellinoUpdateDto(TimesheetCellUpdate update, out ClientResponse response)
        {
            response = new ClientResponse();

            bool valid = true;


            if (update.BaseEntity_Id == 0) //Non può mancare l'entità di base su cui salvare la nuova registrazione
            {
                _log.Error($"Il DTO relativo all'aggiornamento di una cella del cartellino contiene il valore {nameof(update.BaseEntity_Id)} = 0");
                valid = false;
            }

            if (update.GroupedEntity_Id == 0) //Non può mancare l'entità relativa su cui salvare la nuova registrazione
            {
                _log.Error($"Il DTO relativo all'aggiornamento di una cella del cartellino contiene il valore {nameof(update.GroupedEntity_Id)} = 0");
                valid = false;
            }

            if (String.IsNullOrEmpty(update.Justification))
            {
                _log.Error($"Il DTO relativo all'aggiornamento di una cella del cartellino contiene il valore {nameof(update.Justification)} = null");
                valid = false;
            }

            if (update.Date == DateTime.MinValue)
            {
                _log.Error($"Il DTO relativo all'aggiornamento di una cella del cartellino contiene il valore {nameof(update.Date)} = DateTime min");
                valid = false;
            }
            

            if (!valid)
            {
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                response.ReasonPhrase = "Si è verificato un errore durante l'aggiornamento...";

            }


            return valid;
        }

    }
}
