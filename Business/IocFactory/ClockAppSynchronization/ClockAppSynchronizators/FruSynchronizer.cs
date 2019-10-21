using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Infrastructure;
using Business.HttpHub;
using Business.Repository;
using Newtonsoft.Json.Linq;
using System.Data.Entity;

namespace Business.IocFactory.ClockAppSynchronizationFactory.ClockAppSynchronizators
{
    public class FruCantSynchronizer : ISynchronizationProcessor<Fru_Cant>
    {
        private HttpManager _httpHub;
        public FruCantSynchronizer()
        {
            _httpHub = new HttpManager();

            _httpHub.Host = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager.Split(':').First();

            _httpHub.Port = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager.Split(':').Last();

            _httpHub.Path = DictionaryEntityControllers.GetControllerPath<Fru>();
        }

        public void Synchronize(IEnumerable<DbEntityEntry<Fru_Cant>> addedEntities, IEnumerable<Dictionary<string, object>> modifiedEntities, IEnumerable<DbEntityEntry<Fru_Cant>> deletedEntities)
        {
            if (addedEntities.Any())
                SynchronizeAddedEntities(addedEntities);

            if (modifiedEntities.Any())
                SynchronizeModifiedEntities(modifiedEntities);

            if (deletedEntities.Any())
                SynchronizeDeletedEntities(deletedEntities);

        }

        private void SynchronizeAddedEntities(IEnumerable<DbEntityEntry<Fru_Cant>> addedEntities)
        {
            JObject elabEntities = ElaborateAddedEntities(addedEntities);

            _httpHub.PostRequest(elabEntities);
        }
        private void SynchronizeModifiedEntities(IEnumerable<Dictionary<string, object>> modifiedEntities)
        {
            JObject elabEntities = ElaborateModifiedEntities(modifiedEntities);

            _httpHub.PostRequest(elabEntities);
        }
        private void SynchronizeDeletedEntities(IEnumerable<DbEntityEntry<Fru_Cant>> deletedEntities)
        {
            JObject elabEntities = ElaborateDeletedEntities(deletedEntities);

            _httpHub.PostRequest(elabEntities);
        }


        private JObject ElaborateAddedEntities(IEnumerable<DbEntityEntry<Fru_Cant>> entities)
        {
            List<Fru_Cant> fru_cants = entities.Select(c => c.Entity).ToList();

            JObject payload = new JObject();

            payload.Add("Op", EntityState.Added.ToString());
            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);

            JArray items = new JArray();


            foreach (var fru_cant in fru_cants)
            {
                JObject obj = new JObject();

                obj.Add("PowerWebCantId", fru_cant.Cant_Id);
                obj.Add("CantMatr", fru_cant.Codice_Fru.Trim());

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
        private JObject ElaborateDeletedEntities(IEnumerable<DbEntityEntry<Fru_Cant>> entities)
        {
            JObject payload = new JObject();

            payload.Add("Op", EntityState.Deleted.ToString());

            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);

            int[] ids = entities.Select(c => c.Entity).Select(c => c.Cant_Id).ToArray();

            payload.Add("Ids", JArray.FromObject(ids));

            return payload;
        }
    }
}
