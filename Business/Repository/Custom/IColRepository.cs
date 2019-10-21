using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;
using Business.MDBSchema;
using Domain.Extensions;

namespace Business.Repository.Custom
{
    public interface IColRepository : IRepository<Col>
    {
        //void UpdateMothlyHour(DateTime? oldBreakRegDate, DateTime newBreakRegDate);
        void AddPendingElabForActivity(Col entity);

        Dictionary<string, string> ImportFromCSV(string[] inputCants);

        /// <summary>
        /// Aggiorna, dopo averla ricalcolata, la geolocalizzazione dell'entità passata come parametro.
        /// </summary>
        /// <param name="col">Il collaboratore di cui aggiornare la geolocalizzazione.</param>
        /// <returns>La geolocalizzazione dell'entità processata. </returns>
        void UpdateGeoLocation(Col col);
    }
}

