using Business.DataClasses.DevExtremeUtilities;
using Business.DataClasses.WebMethodDataClasses;
using Business.Repository;
using DevExtreme.AspNet.Data;
using Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PowerWeb.Pages
{
    public partial class Tab_SegnalazioniPage : BasePage
    {

        public static IDictionary<string, string> _gridResources;

        protected void Page_Init(object sender, EventArgs e)
        {
            _gridResources = GetGridResources();
        }
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        [WebMethod]
        public static string GridLoad(LoadOptions data)
        {
            DataSourceLoadOptionsBase parameters = new DataSourceLoadOptions();

            DataSourceLoadOptions.ParseOptions(parameters, data);

            RepoManager.DamageRepo.Context.Configuration.ProxyCreationEnabled = false;

            var dataSource = DataSourceLoader.Load(RepoManager.Tab_DamageRepo.DbSet.AsNoTracking(), parameters);

            string serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(dataSource, new Newtonsoft.Json.JsonSerializerSettings()
            {
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None,
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                Formatting = Newtonsoft.Json.Formatting.None
            });

            RepoManager.DamageRepo.Context.Configuration.ProxyCreationEnabled = true;

            return serializedObject;
        }

        [WebMethod]
        public static string GridInsert(Tab_Damage newEntity)
        {
            RepoManager.Tab_DamageRepo.SetEntityBeforeAddOrUpdate(newEntity);

            RepoManager.Tab_DamageRepo.Add(newEntity);

            RepoManager.Tab_DamageRepo.SaveChanges();

            return "";
        }

        [WebMethod]
        public static string GridUpdate(int key, IDictionary<string,object> values)
        {
            Tab_Damage toUpdateDamage = RepoManager.Tab_DamageRepo.Single(t => t.Tab_Damage_Id == key);

            MergeObjectFromJson(toUpdateDamage, values);
            
            RepoManager.Tab_DamageRepo.SaveChanges();

            return "";
        }

        [WebMethod]
        public static string GridDelete(int key)
        {
            Tab_Damage toDelete = new Tab_Damage { Tab_Damage_Id = key };

            RepoManager.Tab_DamageRepo.Attach(toDelete);

            RepoManager.Tab_DamageRepo.Delete(toDelete);

            RepoManager.Tab_DamageRepo.SaveChanges();

            return "";
        }

        [WebMethod]
        public static string LookupLoad(LoadOptions loadOptions, string entity)
        {
            object data = LoadDynamicDataSource(loadOptions, entity);

            return JsonConvert.SerializeObject(data);
        }

        static object LoadDynamicDataSource(LoadOptions loadOptions, string entityName)
        {
            DataSourceLoadOptions options = new DataSourceLoadOptions();

            DataSourceLoadOptions.ParseOptions(options, loadOptions);

            Type entityType = Type.GetType(String.Format("Domain.{0},Domain", entityName));

            System.Reflection.MethodInfo loadMethod = RepoManager.Tab_GridLookupRepo.GetType().GetMethod("DynamicStore").MakeGenericMethod(new Type[] { entityType });

            return loadMethod.Invoke(null, new object[] { options });

        }

        public static void MergeObjectFromJson<TEntity>(TEntity entity, IDictionary<string, object> json) where TEntity : new()
        {
            foreach (var value in json)
            {
                var val = value.Key;
                System.Reflection.PropertyInfo prop = typeof(TEntity).GetProperty(value.Key);
                if (val == null)
                {
                    prop.SetValue(entity, null);
                }
                else
                {
                    prop.SetValue(entity, System.ComponentModel.TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromString(value.Value.ToString()));
                }

            }
        }

        IDictionary<string, string> GetGridResources()
        {

            Dictionary<string, string> resDictionary = new Dictionary<string, string>();

            resDictionary.Add("FLD_CODICE_TAB_DAMAGE", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_CODICE_TAB_DAMAGE"));
            resDictionary.Add("FLD_DESCRIZIONE_TAB_DAMAGE", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_DESCRIZIONE_TAB_DAMAGE"));
            resDictionary.Add("FLD_DISABILITAZIONE_TAB_DAMAGE", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_DISABILITAZIONE_TAB_DAMAGE"));

            return resDictionary;
        }

    }
}