using Business.Synchronization.OperationStrategies;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace Business.Synchronization.Implementations
{
    /// <summary>
    /// Questa classe gestisce tutti i metodi di sincronizzazione con l'esterno.
    /// Vengono configurate alcune parti partendo dall'xml di configurazione.
    /// Ogni entità viene sottoposta ad una particolare operazione 
    /// contenuta nel registro strategie suddiviso per tipo di operazione e poi 
    /// per entità.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Business.Synchronization.ISynchronizer{T}" />
    public class GeoBadgeSynchronizer<T> : ISynchronizer<T> where T : class
    {

        #region PROPRIETA' PUBBLICHE

        /// <summary>
        /// Container delle entità
        /// </summary>
        public ICollector<T> Collector { get; set; }

        /// <summary>
        /// Container delle strategie da applicare ad un determinato tipo di entità
        /// </summary>
        public IOperationStrategies<T> Strategies { get; set; }

        #endregion

        #region CAMPI PRIVATI

        private JArray added;
        private JArray modified;
        private JArray deleted;
        

        #endregion

        #region PROPRIETA' PRIVATE
        /// <summary>
        /// Ritorna la query di autenticazione che viene allegata nella uri
        /// </summary>
        private string Query
        {
            get
            {
                return GetQueryString(new { uid = connectionConfig["uid"],pw = connectionConfig["pw"]});
            }
        }
        
        /// <summary>
        /// Container per le varie stringhe di configurazione dell'httpClient
        /// </summary>
        private IDictionary<string, string> connectionConfig { get; set; }


        /// <summary>
        /// Container per le varie stringhe di configurazione per le chiamate api
        /// </summary>
        private IDictionary<string, string> apiPaths { get; set; }

        /// <summary>
        /// Connessione con l'esterno
        /// </summary>
        private HttpHub.HttpModule externalBridge { get; set; }

        #endregion

        public GeoBadgeSynchronizer(IDictionary<string,string> connectionConfig, IDictionary<string,string> apiPaths)
        {

            externalBridge = new HttpHub.HttpModule();

            this.connectionConfig = connectionConfig;
            this.apiPaths = apiPaths;

            if (connectionConfig.ContainsKey("protocol"))
                externalBridge.Protocol = connectionConfig["protocol"];

            externalBridge.Host = connectionConfig["host"];

            //Caricare dati da xml per configurare le chiamate in base all'entità;


        }
        
        /// <summary>
        /// Si raccolgono le entità da sincronizzare in base al loro stato e si provvede a serializzarle
        /// tramite le strategie iniettate.
        /// Dopodichè vengono inviate al relativo host
        /// </summary>
        public void Synchronize()
        {
            var addedEntities = Collector.GetAddedEntities();
            var modifiedEntities = Collector.GetModifiedEntities();
            var deletedEntities = Collector.GetDeletedEntities();


            if (addedEntities.Any())
            {
                added = Strategies.ExecuteAddStrategy(addedEntities);

                foreach (JObject json in added)
                {
                    AddAuthentication(json);
                    externalBridge.Post(json, apiPaths["create"], Query);
                }
            }


            if (modifiedEntities.Any())
            {
                modified = Strategies.ExecuteModifyStrategy(modifiedEntities);
                foreach (JObject json in modified)
                {
                    AddAuthentication(json);
                    externalBridge.Post(json, apiPaths["modify"], Query);
                }
            }


            if (deletedEntities.Any())
            {
                deleted = Strategies.ExecuteDeletionStrategy(deletedEntities);

                foreach (JObject json in deleted)
                {
                    AddAuthentication(json);
                    externalBridge.Post(json, apiPaths["delete"], Query);
                }
            }


        }

        /// <summary>
        /// Tramite questa funzione è possibile aggiungere campi extra al json
        /// (solitamente campi per l'autenticazione)
        /// </summary>
        private void AddAuthentication(JObject json) //TODO terminare la fase di autenticazione 
        {
            json.Add("IdTenant", connectionConfig["idTenant"]);

        }


        /// <summary>
        /// Crea una stringa query da un singolo oggetto
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        private string GetQueryString(object obj)
        {
            var properties = from p in obj.GetType().GetProperties()
                             where p.GetValue(obj, null) != null
                             select p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(obj, null).ToString());

            return String.Join("&", properties.ToArray());
        }

    }
}
