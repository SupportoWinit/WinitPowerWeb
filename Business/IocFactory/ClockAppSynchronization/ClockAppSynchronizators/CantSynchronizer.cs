using Business.HttpHub;
using Business.Repository;
using Domain;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Business.IocFactory.ClockAppSynchronizationFactory.ClockAppSynchronizators
{
    class CantSynchronizer : ISynchronizationProcessor<Cant>
    {

        private HttpManager _httpHub;

        public CantSynchronizer()
        {
            _httpHub = new HttpManager();

            if (!String.IsNullOrEmpty(RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager))
            {
                _httpHub.Host = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager.Split(':').First();

                _httpHub.Port = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager.Split(':').Last();

                _httpHub.Path = DictionaryEntityControllers.GetControllerPath<Cant>();

            }
        }

        public void Synchronize(IEnumerable<DbEntityEntry<Cant>> addedEntities, IEnumerable<Dictionary<string,object>> modifiedEntities, IEnumerable<DbEntityEntry<Cant>> deletedEntities)
        {
            if (addedEntities.Any())
                SynchronizeAddedEntities(addedEntities);

            if (modifiedEntities.Any())
                SynchronizeModifiedEntities(modifiedEntities);

            if (deletedEntities.Any())
                SynchronizeDeletedEntities(deletedEntities);

        }

        private void SynchronizeAddedEntities(IEnumerable<DbEntityEntry<Cant>> addedEntities)
        {
            JObject elabEntities = ElaborateAddedEntities(addedEntities);

            _httpHub.PostRequest(elabEntities);
        }
        private void SynchronizeModifiedEntities(IEnumerable<Dictionary<string,object>> modifiedEntities)
        {
            JObject elabEntities = ElaborateModifiedEntities(modifiedEntities);

            _httpHub.PostRequest(elabEntities);
        }
        private void SynchronizeDeletedEntities(IEnumerable<DbEntityEntry<Cant>> deletedEntities)
        {
            JObject elabEntities = ElaborateDeletedEntities(deletedEntities);

            _httpHub.PostRequest(elabEntities);
        }


        private JObject ElaborateAddedEntities(IEnumerable<DbEntityEntry<Cant>> entities)
        {
            List<Cant> cantieri = entities.Select(c => c.Entity).ToList();

            JObject payload = new JObject();

            payload.Add("Op", EntityState.Added.ToString());
            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);

            JArray items = new JArray();

            foreach (var cantEntity in cantieri)
            {
                JObject obj = new JObject();

                obj.Add("CantCode", cantEntity.Codice_Cantiere);
                obj.Add("CantDes", cantEntity.Descrizione_Can);
                obj.Add("Latitudine", cantEntity.LatitudineGps_Can);
                obj.Add("Longitudine", cantEntity.LongitudineGps_Can);
                obj.Add("CantMatr", cantEntity.Fru_Cant.Any() ? cantEntity.Fru_Cant.OrderByDescending(c => c.Abilitazione_Data_Inizio_Fru_Can).First().Codice_Fru : "NA");
                obj.Add("Raggio", cantEntity.RaggioGps_Can);
                obj.Add("PowerWebCantId", cantEntity.Cant_Id);
                obj.Add("Disabilitato", cantEntity.DisAbilitazione_Can);

                items.Add(obj);

            }

            payload.Add("Entities", items);

            return payload;
        }
        private JObject ElaborateModifiedEntities(IEnumerable<Dictionary<string,object>> entities)
        {
            JObject payload = new JObject();

            payload.Add("Op", EntityState.Modified.ToString());
            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);
            
            payload.Add("Entities", JArray.FromObject(entities));

            return payload;
        }
        private JObject ElaborateDeletedEntities(IEnumerable<DbEntityEntry<Cant>> entities)
        {
            JObject payload = new JObject();

            payload.Add("Op", EntityState.Deleted.ToString());

            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);

            int[] ids = entities.Select(c => (Cant)c.Entity).Select(c => c.Cant_Id).ToArray();

            payload.Add("Ids", JArray.FromObject(ids));

            return payload;
        }


        private IEnumerable<Dictionary<string,object>> DetachModifiedValues(IEnumerable<DbEntityEntry<Cant>> entities)
        {
            List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();


            foreach (var ent in entities)
            {
                Dictionary<string, object> modProp = new Dictionary<string, object>();

                foreach (var propName in ent.CurrentValues.PropertyNames)
                {
                    var current = ent.CurrentValues[propName];
                    var original = ent.OriginalValues[propName];

                    if (current != original)
                    {
                        modProp.Add(propName, current);
                    }
                }

                items.Add(modProp);
            }

            return items;
        }

    }
}
