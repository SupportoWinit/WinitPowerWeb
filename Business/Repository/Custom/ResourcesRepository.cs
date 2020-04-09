using Data;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Business.Repository.Custom
{
    public class ResourcesRepository : GenericRepository<Resources>, IResourcesRepository
    {
        public ResourcesRepository(PowerWebEntities context)
            : base(context)
        {
        }

        public Dictionary<string, string> ResourcesDictionary
        {
            get
            {
                Dictionary<string, string> dict = PowerWebContext.GetFromSession<Dictionary<string, string>>("ResourcesRepo");
                if (dict == null)
                {

                    Lingue lingua = null;

                    if (PowerWebContext.Current != null && PowerWebContext.Current.Lingua != null)
                        lingua = PowerWebContext.Current.Lingua;
                    else lingua = RepoManager.LingueRepo.Single(lng => lng.Sigla_Lingue == Common.Properties.Settings.Default.CultureInfo);

                    dict = lingua.Resources.Where(res => res.Versioni_Id == null).ToDictionary(r => r.ResourceKey, r => r.ResourceValue);

                    if (PowerWebContext.Current.Versione != null)
                    {
                        foreach (var resource in PowerWebContext.Current.Versione.Resources)
                        {
                            if (dict.ContainsKey(resource.ResourceKey))
                                dict[resource.ResourceKey] = resource.ResourceValue;
                            else
                                dict.Add(resource.ResourceKey, resource.ResourceValue);
                        }
                    }

                    PowerWebContext.SetToSession<Dictionary<string, string>>("ResourcesRepo", dict);
                }
                return dict;
            }
        }

        public String GetResourcesDictionaryString(string value)
        {
            String result = value;

            if (ResourcesDictionary != null && ResourcesDictionary.ContainsKey(value))
                result = ResourcesDictionary[value];

            return result;
        }

        public void ResetResourcesDictionary()
        {
            PowerWebContext.SetToSession<Dictionary<string, string>>("ResourcesRepo", null);
        }

        public override Dictionary<string, string> Check(Resources entity, bool isNew = false, bool isResetSession = true)
        {
            return base.Check(entity, isNew, isResetSession);
        }
    }
}
