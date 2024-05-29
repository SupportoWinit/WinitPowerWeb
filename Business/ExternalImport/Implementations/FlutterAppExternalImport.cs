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
        private FlutterAppHttpModule bridge2;
        private FlutterAppHttpModuleOld bridgeOld;
        private IEnumerable<FlutterAppReg> registrazioni;
        private IEnumerable<FlutterAppReg> registrazioni2;
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
            bridge2 = new FlutterAppHub();
            bridgeOld = new FlutterAppHubOld();

            string[] urls = new string[2];
            if (connectionConfig["host"].Contains(','))
            {
                urls = connectionConfig["host"].Split(',');
                if (urls[0].StartsWith("backend"))
                {
                    bridgeOld.Host = urls[0];
                }
                else { 
                    bridge2.Host = urls[0];
                }                
                bridge.Host = urls[1];
            }
            else {
                if (connectionConfig["host"].StartsWith("backend"))
                {
                    bridgeOld.Host = connectionConfig["host"];
                }
                else 
                { 
                    bridge.Host = connectionConfig["host"];
                }            
            }
            
            fileWriter = new FlutterAppRegFileCreator();
        }


        public void GetTimbrature()
        {
            int index = RepoManager.ParamRepo.ParametersRow.Indice_Timbrature_FlutterApp;
            if (connectionConfig["IdCliente"].Contains(','))
            {
                ids = connectionConfig["IdCliente"].Split(',');
            }
            else
            {
                ids[0] = connectionConfig["IdCliente"];
            }
            

            if (bridgeOld.Host != null) {
                registrazioniOld = bridgeOld.Get<List<FlutterAppRegOld>>(CreateStandardPayloadOld(index), apiPaths["getTimbrature"]);
            }

            if (bridge.Host != null) {
                registrazioni = bridge.Get<List<FlutterAppReg>>(CreateStandardPayload(index), apiPaths["getTimbrature"]);
            }

            if (bridge2.Host != null) {
                registrazioni2 = bridge.Get<List<FlutterAppReg>>(CreateStandardPayload(index), apiPaths["getTimbrature"]);
            }
            
            if ((registrazioni != null && registrazioni.Any()) || (registrazioniOld != null && registrazioniOld.Any()) || (registrazioni2 != null && registrazioni2.Any()))
            {
                if (registrazioni != null && registrazioni.Any()) {
                    BusinessService.BackUpJsonObject(registrazioni, Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path);

                    RepoManager.ParamRepo.SaveFlutterAppRegIndex(Math.Max(registrazioni.Max(c => c.Id) + 1, index));
                }
                if (registrazioni2 != null && registrazioni2.Any())
                {
                    BusinessService.BackUpJsonObject(registrazioni2, Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path);

                    RepoManager.ParamRepo.SaveFlutterAppRegIndex(Math.Max(registrazioni2.Max(c => c.Id) + 1, index));
                }
                if (registrazioniOld != null && registrazioniOld.Any()) {
                    BusinessService.BackUpJsonObject(registrazioniOld, Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path);

                    RepoManager.ParamRepo.SaveFlutterAppRegIndex(Math.Max(registrazioniOld.Max(c => c.Id) + 1, index));
                }
            }
        }

        private JObject CreateStandardPayload(int index)
        {
            string id = connectionConfig["IdCliente"];
            if (ids[1] != null)
            {
                id = ids[1];
            }
            else {
                id = ids[0];
            }
            JObject request = JObject.FromObject(new
            {
                IdCliente = id
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
            fileWriter.WriteToFile(registrazioni,registrazioniOld,registrazioni2);
        }
    }
}
