using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Domain;
using Business.Repository;
using System.Data.Entity;

namespace Business.Synchronization.OperationStrategies.ClockApp
{
    public class ClockAppCantOperationStrategies : IOperationStrategies<Cant>
    {
        public JArray ExecuteAddStrategy(IEnumerable<Cant> entities)
        {
            JArray result = new JArray();

            JObject obj = new JObject();

            obj.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);
            obj.Add("Op", EntityState.Added.ToString());

            JArray toShipEntities = new JArray();

            foreach (var cant in entities)
            {
                JObject entity = new JObject();

                entity.Add("CantCode", cant.Codice_Cantiere);
                entity.Add("CantDes", cant.Descrizione_Can);
                entity.Add("Latitudine", cant.LatitudineGps_Can);
                entity.Add("Longitudine", cant.LongitudineGps_Can);
                entity.Add("CantMatr", cant.Fru_Cant.Any() ? cant.Fru_Cant.OrderByDescending(c => c.Abilitazione_Data_Inizio_Fru_Can).First().Codice_Fru : "NA");
                entity.Add("Raggio", cant.RaggioGps_Can);
                entity.Add("PowerWebCantId", cant.Cant_Id);
                entity.Add("Disabilitato", cant.DisAbilitazione_Can);
                toShipEntities.Add(entity);

            }

            obj.Add("Entities", toShipEntities);

            result.Add(obj);

            return result;
        }

        public JArray ExecuteModifyStrategy(IEnumerable<Cant> entities)
        {
            JArray result = new JArray();

            JObject obj = new JObject();

            obj.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);
            obj.Add("Op", EntityState.Modified.ToString());

            JArray toShipEntities = new JArray();

            foreach (var cant in entities)
            {
                JObject entity = new JObject();

                entity.Add("CantCode", cant.Codice_Cantiere);
                entity.Add("CantDes", cant.Descrizione_Can);
                entity.Add("Latitudine", cant.LatitudineGps_Can);
                entity.Add("Longitudine", cant.LongitudineGps_Can);
                entity.Add("CantMatr", cant.Fru_Cant.Any() ? cant.Fru_Cant.OrderByDescending(c => c.Abilitazione_Data_Inizio_Fru_Can).First().Codice_Fru : "NA");
                entity.Add("Raggio", cant.RaggioGps_Can);
                entity.Add("PowerWebCantId", cant.Cant_Id);
                entity.Add("Disabilitato", cant.DisAbilitazione_Can);
                toShipEntities.Add(entity);

            }

            obj.Add("Entities", toShipEntities);

            result.Add(obj);

            return result;
        }

        public JArray ExecuteDeletionStrategy(IEnumerable<Cant> entities)
        {
            JArray result = new JArray();

            JObject obj = new JObject();

            obj.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);
            obj.Add("Op", EntityState.Deleted.ToString());
            obj.Add("Ids", JArray.FromObject(entities.Select(c => c.Cant_Id).ToList()));
            
            result.Add(obj);

            return result;
        }


    }
}
