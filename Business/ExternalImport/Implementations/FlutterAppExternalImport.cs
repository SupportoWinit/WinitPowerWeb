using Business.DataClasses.FlutterAppDTOs;
using Business.ExternalImports;
using Business.HttpHub;
using Business.HttpHub.HttpHubs;
using Business.RegFileCreators;
using Business.Repository;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;

namespace Business.ExternalImport.Implementations
{
    class FlutterAppExternalImport : IExternalImport
    {
        private FlutterAppHttpModule bridge;
        private FlutterAppHttpModuleOld bridgeOld;
        private IEnumerable<FlutterAppReg> registrazioni;
        private IEnumerable<FlutterAppRegOld> registrazioniOld;
        private IRegFileCreator<FlutterAppReg> fileWriter;
        private string[] ids = new string[2];
 
        /// <summary>
        /// Container per le varie stringhe di configurazione dell'httpClient
        /// </summary>
        private IDictionary<string, string> connectionConfig { get; set; }


        /// <summary>
        /// Container per le varie stringhe di configurazione per le chiamate api
        /// </summary>
        private IDictionary<string, string> apiPaths { get; set; }

        public FlutterAppExternalImport(IDictionary<string, string> connectionConfig, IDictionary<string, string> apiPaths)
        {
            this.connectionConfig = connectionConfig;
            this.apiPaths = apiPaths;

            bridge = new FlutterAppHub();
            bridgeOld = new FlutterAppHubOld();

            string[] urls = new string[2];
            urls = connectionConfig["host"].Split(',');
            bridgeOld.Host = urls[0];
            bridge.Host = urls[1];
            fileWriter = new FlutterAppRegFileCreator();
        }


        public void GetTimbrature()
        {
            int index = RepoManager.ParamRepo.ParametersRow.Indice_Timbrature_FlutterApp;
            ids = connectionConfig["IdCliente"].Split(',');

            registrazioni = bridge.Get<List<FlutterAppReg>>(CreateStandardPayload(index), apiPaths["getTimbrature"]);
            registrazioniOld = bridgeOld.Get<List<FlutterAppRegOld>>(CreateStandardPayloadOld(index), apiPaths["getTimbrature"]);
            
            if ((registrazioni != null && registrazioni.Any()) || (registrazioniOld != null && registrazioniOld.Any()))
            {
                if (registrazioni != null && registrazioni.Any()) {
                    BusinessService.BackUpJsonObject(registrazioni, Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path);

                    RepoManager.ParamRepo.SaveFlutterAppRegIndex(Math.Max(registrazioni.Max(c => c.Id) + 1, index));
                }
                if (registrazioniOld != null && registrazioniOld.Any()) {
                    BusinessService.BackUpJsonObject(registrazioniOld, Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path);

                    RepoManager.ParamRepo.SaveFlutterAppRegIndex(Math.Max(registrazioniOld.Max(c => c.Id) + 1, index));
                }
            }
        }

        private JObject CreateStandardPayload(int index)
        {
            JObject request = JObject.FromObject(new
            {
                IdCliente = ids[1]
            });

            return request;
        }

        private JObject CreateStandardPayloadOld(int index)
        {
            JObject request = JObject.FromObject(new
            {
                IdCliente = ids[0]
            });

            return request;
        }

        public void GetTimbrature(DateTime from, DateTime to)
        {
            throw new NotImplementedException();
        }

        public void WriteToFile()
        {
            fileWriter.WriteToFile(registrazioni,registrazioniOld);
        }
    }
}
