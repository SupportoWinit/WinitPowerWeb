using Business.ExternalImports;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Business.ExternalImports
{
    public class ExternalImportFactory
    {
        private const string CONFIGURATION_NODE = "Configuration";
        private const string EXTERNAL_IMPORT_NODE = "ExternalImport";
        private const string OPERATIONSTRATEGY_NODE = "OperationStrategy";
        private const string EXTRA_INFO_NODE = "ExtraInfos";
        private const string HOSTCONFIG_NODE = "HostConfig";

        private static ICollection<IExternalImport> externalImports;

        public ExternalImportFactory()
        {
            externalImports = LoadFromXml();
        }


        public void Import()
        {
            foreach (var ext in externalImports)
            {
                ext.GetTimbrature();
                ext.WriteToFile();
            }
        }



        //Caricamento configurazione da xml
        private static List<IExternalImport> LoadFromXml()
        {
            List<IExternalImport> imports = new List<IExternalImport>();

            string XmlPath = HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.ExternalImportConfig);
            XDocument doc = XDocument.Load(XmlPath);

            var headNode = doc.Element(CONFIGURATION_NODE);

            foreach (var configNode in headNode.Descendants().Where(node => node.Name.LocalName == EXTERNAL_IMPORT_NODE && (string)node.Attribute(XName.Get("enabled")) == "1"))
            {
                string company = (string)configNode.Attribute("company");
                string synchClass = (string)configNode.Attribute("class");
                string hostConfig = (string)configNode.Element(XName.Get(HOSTCONFIG_NODE)).Attribute("host");

                var strategy = configNode.Element(OPERATIONSTRATEGY_NODE);

                if (strategy == null)
                    continue;

                string assemblyName = (string)configNode.Attribute("class");

                //Lettura parametri api
                Dictionary<string, string> apiConfig = strategy.Descendants().Select(n => new { Key = n.Name.LocalName, Value = (string)n.Attribute("value") }).ToDictionary(c => c.Key, d => d.Value);

                //Lettura configurazione host
                Dictionary<string, string> configurationParameters = configNode.Element(XName.Get(EXTRA_INFO_NODE)).Descendants().Select(n => new { Key = n.Name.LocalName, Value = (string)n.Attribute("value") }).ToDictionary(c => c.Key, d => d.Value);

                configurationParameters.Add("host", hostConfig);

                //Estrazione del tipo
                Type operationType = Type.GetType(assemblyName);

                //Creazione dell'instanza con dependency injection
                IExternalImport externalImportInstance = Activator.CreateInstance(operationType, new object[] { configurationParameters, apiConfig }) as IExternalImport;

                if (externalImportInstance != null)
                    imports.Add(externalImportInstance);
            }

            return imports;
        }

    }
}
