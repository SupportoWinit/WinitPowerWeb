using Business.Repository;
using Business.Synchronization.Synchronizators;
using Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Business.Synchronization.SynchronizatioManager.Implementations
{
    public class SynchronizationManager<T> where T : class
    {
        private const string CONFIGURATION_NODE = "Configuration";
        private const string SYNCHRONIZATION_NODE = "Synch";
        private const string OPERATIONSTRATEGY_NODE = "OperationStrategy";
        private const string EXTRA_INFO_NODE = "ExtraInfos";
        private const string HOSTCONFIG_NODE = "HostConfig";


        private DbContext _context;

        private ICollection<ISynchronizator<T>> synchronizators;

        public DbContext Context { get { return _context; } set { _context = value; } }


        public SynchronizationManager()
        {
            if(!RepoManager.ParamRepo.ParametersRow.Abilita_Sincronizzazione_Entità)
            {
                return;
            }

            synchronizators = LoadFromConfiguration();
        }

        public void AttachContext(DbContext context)
        {
            _context = context;
        }

        public void Collect()
        {
            if (!RepoManager.ParamRepo.ParametersRow.Abilita_Sincronizzazione_Entità)
            {
                return;
            }

            foreach (var synchronizator in synchronizators)
                synchronizator.Collect(Context);
        }

        public void SynchronizeEntities()
        {
            if (!RepoManager.ParamRepo.ParametersRow.Abilita_Sincronizzazione_Entità)
            {
                return;
            }

            foreach (var synchronizator in synchronizators)
                synchronizator.Synchronize();
        }

        /// <summary>
        /// Carica la configurazione dall'xml
        /// </summary>
        /// <returns></returns>
        private ICollection<ISynchronizator<T>> LoadFromConfiguration()
        {
            List<ISynchronizator<T>> container = new List<ISynchronizator<T>>();

            foreach (var item in GetAll())
            {
                container.Add(new Synchronizator<T>(item.Synchronizator, item.Operations));
            }

            return container;
        }

        private ICollection<ConfigContainer> GetAll()
        {
            List<ConfigContainer> ops = new List<ConfigContainer>();

            string path = HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.ExternalImportConfig);

            XDocument config = XDocument.Load(path);

            var headNode = config.Element(CONFIGURATION_NODE);

            foreach (var configNode in headNode.Descendants().Where(node => node.Name.LocalName == SYNCHRONIZATION_NODE && (string)node.Attribute("enabled") == "1")) //Solo nodi attivi
            {

                string company = (string)configNode.Attribute("company");
                string synchAssemblyName = (string)configNode.Attribute("class");
                string hostConfig = (string)configNode.Element(XName.Get(HOSTCONFIG_NODE)).Attribute("host");
                string portConfig = (string)configNode.Element(XName.Get(HOSTCONFIG_NODE)).Attribute("port") ?? null;
                string protocolConfig = (string)configNode.Element(XName.Get(HOSTCONFIG_NODE)).Attribute("protocol") ?? null;

                var strategy = configNode.Descendants().Where(node => node.Name.LocalName == OPERATIONSTRATEGY_NODE && (string)node.Attribute("enabled") == "1" && (string)node.Attribute("type") == typeof(T).Name).FirstOrDefault();

                if (strategy == null)
                    continue;

                string strategyAssemblyName = (string)strategy.Attribute("class");

                Dictionary<string, string> apiConfig = strategy.Descendants().Select(n => new { Key = n.Name.LocalName, Value = (string)n.Attribute("value") }).ToDictionary(c => c.Key, d => d.Value);

                //Estrazione del tipo del sincronizzatore generico in base al tipo corrente
                Type synchronizatorType = Type.GetType(synchAssemblyName).MakeGenericType(new Type[] { typeof(T) });

                //Estrazione del tipo delle strategie
                Type operationType = Type.GetType(strategyAssemblyName);

                ConfigContainer conf = new ConfigContainer();

                //Estrazione della configurazione
                Dictionary<string, string> configurationParameters = configNode.Element(XName.Get(EXTRA_INFO_NODE)).Descendants().Select(n => new { Key = n.Name.LocalName, Value = (string)n.Attribute("value") }).ToDictionary(c => c.Key, d => d.Value);
                configurationParameters.Add("host", hostConfig);

                if(portConfig != null)
                    configurationParameters.Add("port", portConfig);

                if (protocolConfig != null)
                    configurationParameters.Add("protocol", protocolConfig);

                //Creo la strategia del tipo generico corrente
                conf.Operations = Activator.CreateInstance(operationType) as IOperationStrategies<T>;
                //Creo il sincronizzatore e vi inietto i parametri
                conf.Synchronizator = Activator.CreateInstance(synchronizatorType, new object[] { configurationParameters, apiConfig }) as ISynchronizer<T>;

                ops.Add(conf);

            }

            return ops;
        }

        internal class ConfigContainer
        {
            public ISynchronizer<T> Synchronizator { get; set; }

            public IOperationStrategies<T> Operations { get; set; }
        }
    }
}
