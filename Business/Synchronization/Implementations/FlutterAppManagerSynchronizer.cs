using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Synchronization.Implementations
{
    public class FlutterAppManagerSynchronizer<T> : ISynchronizer<T> where T : class
    {
        public ICollector<T> Collector { get; set; }

        public IOperationStrategies<T> Strategies { get; set; }

        private JArray added;
        private JArray modified;
        private JArray deleted;

        /// <summary>
        /// Container per le varie stringhe di configurazione dell'httpClient
        /// </summary>
        private IDictionary<string, string> connectionConfig { get; set; }


        /// <summary>
        /// Container per le varie stringhe di configurazione per le chiamate api
        /// </summary>
        private IDictionary<string, string> apiPaths { get; set; }

        private HttpHub.HttpModule externalBridge { get; set; }

        public FlutterAppManagerSynchronizer(IDictionary<string, string> connectionConfig, IDictionary<string, string> apiPaths)
        {

            externalBridge = new HttpHub.HttpModule();

            this.connectionConfig = connectionConfig;
            this.apiPaths = apiPaths;

            externalBridge.Host = connectionConfig["host"];

            if (connectionConfig.ContainsKey("port"))
                externalBridge.Port = connectionConfig["port"];

            if (connectionConfig.ContainsKey("protocol"))
                externalBridge.Protocol = connectionConfig["protocol"];

        }

        public void Synchronize()
        {
            var addedEntities = Collector.GetAddedEntities();
            var modifiedEntities = Collector.GetModifiedEntities();
            var deletedEntities = Collector.GetDeletedEntities();


            if (addedEntities.Any())
            {
                added = Strategies.ExecuteAddStrategy(addedEntities,this.connectionConfig);

                foreach (JObject json in added)
                {
                    externalBridge.Post(json, apiPaths["create"]);
                }
            }


            if (modifiedEntities.Any())
            {
                modified = Strategies.ExecuteModifyStrategy(modifiedEntities, this.connectionConfig);
                foreach (JObject json in modified)
                { 
                    externalBridge.Post(json, apiPaths["modify"]);
                }
            }


            if (deletedEntities.Any())
            {
                deleted = Strategies.ExecuteDeleteStrategy(deletedEntities, this.connectionConfig);

                foreach (JObject json in deleted)
                {
                    externalBridge.Post(json, apiPaths["delete"]);
                }
            }
        }
    }
}
