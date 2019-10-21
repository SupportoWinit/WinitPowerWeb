using Business.HttpHub;
using Business.Repository;
using Domain;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Business.IocFactory.ClockAppSynchronizationFactory.ClockAppSynchronizators
{
    public class PruColSynchronizer : ISynchronizationProcessor<Pru_Col>
    {
        private HttpManager _httpHub;
        public PruColSynchronizer()
        {
            _httpHub = new HttpManager();

            _httpHub.Host = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager.Split(':').First();

            _httpHub.Port = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager.Split(':').Last();

            _httpHub.Path = DictionaryEntityControllers.GetControllerPath<Pru>();
        }

        public void Synchronize(IEnumerable<DbEntityEntry<Pru_Col>> addedEntities, IEnumerable<Dictionary<string, object>> modifiedEntities, IEnumerable<DbEntityEntry<Pru_Col>> deletedEntities)
        {
            if (addedEntities.Any())
                SynchronizeAddedEntities(addedEntities);

            if (modifiedEntities.Any())
                SynchronizeModifiedEntities(modifiedEntities);

            if (deletedEntities.Any())
                SynchronizeDeletedEntities(deletedEntities);

        }

        private void SynchronizeAddedEntities(IEnumerable<DbEntityEntry<Pru_Col>> addedEntities)
        {
            JObject elabEntities = ElaborateAddedEntities(addedEntities);

            _httpHub.PostRequest(elabEntities);
        }
        private void SynchronizeModifiedEntities(IEnumerable<Dictionary<string, object>> modifiedEntities)
        {
            JObject elabEntities = ElaborateModifiedEntities(modifiedEntities);

            _httpHub.PostRequest(elabEntities);
        }
        private void SynchronizeDeletedEntities(IEnumerable<DbEntityEntry<Pru_Col>> deletedEntities)
        {
            JObject elabEntities = ElaborateDeletedEntities(deletedEntities);

            _httpHub.PostRequest(elabEntities);
        }


        private JObject ElaborateAddedEntities(IEnumerable<DbEntityEntry<Pru_Col>> entities)
        {
            List<Pru_Col> pru_cols = entities.Select(c => c.Entity).ToList();

            JObject payload = new JObject();

            payload.Add("Op", EntityState.Added.ToString());
            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);

            JArray items = new JArray();


            foreach (var pru_col in pru_cols)
            {
                JObject obj = new JObject();

                obj.Add("PowerWebCantId", pru_col.Col_Id);
                obj.Add("CantMatr", pru_col.Codice_Pru.Trim());

                items.Add(obj);
            }

            payload.Add("Entities", items);

            return payload;
        }
        private JObject ElaborateModifiedEntities(IEnumerable<Dictionary<string, object>> entities)
        {
            JObject payload = new JObject();

            payload.Add("Op", EntityState.Modified.ToString());
            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);

            payload.Add("Entities", JArray.FromObject(entities));

            return payload;
        }
        private JObject ElaborateDeletedEntities(IEnumerable<DbEntityEntry<Pru_Col>> entities)
        {
            JObject payload = new JObject();

            payload.Add("Op", EntityState.Deleted.ToString());

            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);

            int[] ids = entities.Select(c => c.Entity).Select(c => c.Col_Id).ToArray();

            payload.Add("Ids", JArray.FromObject(ids));

            return payload;
        }
        
    }
}
