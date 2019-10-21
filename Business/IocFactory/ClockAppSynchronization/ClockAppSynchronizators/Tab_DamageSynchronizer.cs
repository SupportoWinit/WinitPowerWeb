using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity.Infrastructure;
using Business.Repository;
using Business.HttpHub;
using Newtonsoft.Json.Linq;
using System.Data.Entity;
using Business.IocFactory.ClockAppSynchronizationFactory;

namespace Business.ClockAppManagerSynchronizationUtilities.SynchronizationProcessors.Synchronizers
{
    public class Tab_DamageSynchronizer : ISynchronizationProcessor<Tab_Damage>
    {
        private HttpManager _httpHub;

        public Tab_DamageSynchronizer()
        {
            _httpHub = new HttpManager();

            if (!String.IsNullOrEmpty(RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager))
            {
                _httpHub.Host = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager.Split(':').First();

                _httpHub.Port = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager.Split(':').Last();

                _httpHub.Path = DictionaryEntityControllers.GetControllerPath<Tab_Damage>();

            }
        }

        public void Synchronize(IEnumerable<DbEntityEntry<Tab_Damage>> addedEntities, IEnumerable<Dictionary<string, object>> modifiedEntities, IEnumerable<DbEntityEntry<Tab_Damage>> deletedEntities)
        {
            if (addedEntities.Any())
                SynchronizeAddedEntities(addedEntities);

            if (modifiedEntities.Any())
                SynchronizeModifiedEntities(modifiedEntities);

            if (deletedEntities.Any())
                SynchronizeDeletedEntities(deletedEntities);
        }

        private void SynchronizeAddedEntities(IEnumerable<DbEntityEntry<Tab_Damage>> addedEntities)
        {
            JObject elabEntities = ElaborateAddedEntities(addedEntities);

            _httpHub.PostRequest(elabEntities);
        }
        private void SynchronizeModifiedEntities(IEnumerable<Dictionary<string, object>> modifiedEntities)
        {
            JObject elabEntities = ElaborateModifiedEntities(modifiedEntities);

            _httpHub.PostRequest(elabEntities);
        }
        private void SynchronizeDeletedEntities(IEnumerable<DbEntityEntry<Tab_Damage>> deletedEntities)
        {
            JObject elabEntities = ElaborateDeletedEntities(deletedEntities);

            _httpHub.PostRequest(elabEntities);
        }

        private JObject ElaborateAddedEntities(IEnumerable<DbEntityEntry<Tab_Damage>> entities)
        {
            List<Tab_Damage> tab_damages = entities.Select(c => c.Entity).ToList();

            JObject payload = new JObject();

            payload.Add("Op", EntityState.Added.ToString());
            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);

            JArray items = new JArray();

            foreach (var tab_DamageEntity in tab_damages)
            {
                JObject obj = new JObject();

                obj.Add("DamageTypeDescription", RepoManager.Tab_DecodRepo.First(c => c.Tab_Decod_Id == tab_DamageEntity.Tab_Decod_Id).Decodifica_Tab);
                obj.Add("DamageSubTypeDescription", tab_DamageEntity.Descrizione_Tab_Damage);
                obj.Add("Tab_Damage_Id", tab_DamageEntity.Tab_Damage_Id);
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
        private JObject ElaborateDeletedEntities(IEnumerable<DbEntityEntry<Tab_Damage>> entities)
        {
            JObject payload = new JObject();

            payload.Add("Op", EntityState.Deleted.ToString());

            payload.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);

            int[] ids = entities.Select(c => (Tab_Damage)c.Entity).Select(c => c.Tab_Damage_Id).ToArray();

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
