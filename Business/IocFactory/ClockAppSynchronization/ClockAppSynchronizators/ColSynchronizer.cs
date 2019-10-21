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
    public class ColSynchronizer : ISynchronizationProcessor<Col>
    {

        private HttpManager _httpHub;
        public ColSynchronizer()
        {
            _httpHub = new HttpManager();

            if (!String.IsNullOrEmpty(RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager))
            {
                _httpHub.Host = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager.Split(':').First();

                _httpHub.Port = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager.Split(':').Last();

                _httpHub.Path = DictionaryEntityControllers.GetControllerPath<Col>();
            }
        }

        public void Synchronize(IEnumerable<DbEntityEntry<Col>> addedEntities, IEnumerable<Dictionary<string, object>> modifiedEntities, IEnumerable<DbEntityEntry<Col>> deletedEntities)
        {
            if (addedEntities.Any())
                SynchronizeAddedEntities(addedEntities);

            if (modifiedEntities.Any())
                SynchronizeModifiedEntities(modifiedEntities);

            if (deletedEntities.Any())
                SynchronizeDeletedEntities(deletedEntities);

        }

        private void SynchronizeAddedEntities(IEnumerable<DbEntityEntry<Col>> addedEntities)
        {
            JObject elabEntities = ElaborateAddedEntities(addedEntities);

            _httpHub.PostRequest(elabEntities);
        }
        private void SynchronizeModifiedEntities(IEnumerable<Dictionary<string, object>> modifiedEntities)
        {
            JObject elabEntities = ElaborateModifiedEntities(modifiedEntities);

            _httpHub.PostRequest(elabEntities);
        }
        private void SynchronizeDeletedEntities(IEnumerable<DbEntityEntry<Col>> deletedEntities)
        {
            JObject elabEntities = ElaborateDeletedEntities(deletedEntities);

            _httpHub.PostRequest(elabEntities);
        }


        private JObject ElaborateAddedEntities(IEnumerable<DbEntityEntry<Col>> entities)
        {
            List<Col> collaboratori = entities.Select(c => c.Entity).ToList();

            JObject payload = new JObject();

            payload.Add("Op", EntityState.Added.ToString());
            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);

            JArray items = new JArray();


            foreach (var col in collaboratori)
            {
                JObject obj = new JObject();

                obj.Add("ColCode", col.Codice_Collaboratore);
                obj.Add("ColDes", col.CognomeNome_Col);
                obj.Add("ColMatr", col.Pru_Col.Any() ? col.Pru_Col.OrderByDescending(c => c.Abilitazione_Data_Inizio_Pru_Col).First().Codice_Pru : "NA");
                obj.Add("Disabilitato", col.DisAbilitazione_Col);
                obj.Add("PowerWebColId", col.Col_Id);

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
        private JObject ElaborateDeletedEntities(IEnumerable<DbEntityEntry<Col>> entities)
        {
            JObject payload = new JObject();

            payload.Add("Op", EntityState.Deleted.ToString());

            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);

            int[] ids = entities.Select(c => (Col)c.Entity).Select(c => c.Col_Id).ToArray();

            payload.Add("Ids", JArray.FromObject(ids));

            return payload;
        }


        private IEnumerable<Dictionary<string, object>> DetachModifiedValues(IEnumerable<DbEntityEntry<Cant>> entities)
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
