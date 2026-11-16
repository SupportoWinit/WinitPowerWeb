using Business.DataClasses.BackendNewDTOs;
using Business.DataClasses.FlutterAppDTOs;
using Business.ExternalImports;
using Business.HttpHub;
using Business.HttpHub.HttpHubs;
using Business.Profile;
using Business.RegFileCreators;
using Business.Repository;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;

namespace Business.ExternalImport.Implementations
{
    class BackendLinuxExternalImport : IExternalImport
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PowerWebMembershipProvider));
        private FlutterAppHttpModule bridge;
        private IEnumerable<BackendNewReg> registrazioni;
        private IRegFileCreator<BackendNewReg> fileWriter;
        private string[] ids = new string[2];
 
        /// <summary>
        /// Container per le varie stringhe di configurazione dell'httpClient
        /// </summary>
        private IDictionary<string, string> connectionConfig { get; set; }


        /// <summary>
        /// Container per le varie stringhe di configurazione per le chiamate api
        /// </summary>
        private IDictionary<string, string> apiPaths { get; set; }

        public BackendLinuxExternalImport(IDictionary<string, string> connectionConfig, IDictionary<string, string> apiPaths)
        {
            this.connectionConfig = connectionConfig;
            this.apiPaths = apiPaths;

            bridge = new FlutterAppHub();

            string[] urls = new string[2];
            bridge.Host = connectionConfig["host"];
            
            fileWriter = new BackendLinuxRegFileCreator();
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

            if (bridge.Host != null) {
                registrazioni = bridge.Get<List<BackendNewReg>>(CreateStandardPayload(index), apiPaths["getTimbrature"]);
            }
            if (registrazioni != null && registrazioni.Any())
            {
                _log.InfoFormat("Registrazioni da backend nuovo 1 recuperate = " + registrazioni.Count());
            }
            else {
                _log.InfoFormat("Nessuna registrazione da backend nuovo 1 recuperata");
            }
            
            if ((registrazioni != null && registrazioni.Any()))
            {
                if (registrazioni != null && registrazioni.Any()) {
                    BusinessService.BackUpJsonObject(registrazioni, Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path);

                    RepoManager.ParamRepo.SaveFlutterAppRegIndex(Math.Max(registrazioni.Max(c => c.Id) + 1, index));
                }
            }
        }

        private JObject CreateStandardPayload(int index)
        {
            string id = connectionConfig["IdCliente"];
            if (ids[1] != null)
            {
                id = ids[1];
                ids[1] = null;
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
            fileWriter.WriteToFile(registrazioni);
        }

        /// <summary>
        /// Metodo per confermare la ricezione delle timbrature ed effettuare la chiamata api per segnalare le timbrature come inviate
        /// </summary>
        public void SetSynced()
        {
           // string apiEndpoint = "/app/ws/setSynced";
           // List<int> ids = new List<int>();
           //
           // if (registrazioni != null)
           // {
           //     foreach (var reg in registrazioni)
           //     {
           //         ids.Add(reg.acquisizioneId);
           //     }
           // }
           //
           // if (ids.Count > 0)
           // {
           //     _ = Task.Run(async () =>
           //     {
           //
           //         try
           //         {
           //             IdsResponse idsPayload = new IdsResponse { Ids = ids };
           //             string jsonContent = JsonSerializer.Serialize(idsPayload);
           //
           //             using (HttpClient client = new HttpClient())
           //             {
           //                 HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
           //                 client.Timeout = TimeSpan.FromMinutes(1);
           //                 String endpoint = "https://" + bridge.Host + apiEndpoint;
           //                 HttpResponseMessage resp = await client.PostAsync(endpoint, content);
           //
           //                 string respBody = await resp.Content.ReadAsStringAsync();
           //                 if (resp.StatusCode == System.Net.HttpStatusCode.OK)
           //                     _log.InfoFormat("Timbrature acquisitore app contrassegnate inviate correttamente");
           //                 else
           //                     _log.ErrorFormat("Errore nella contrassegnazione delle timbrature del backend app come inviate: " + respBody);
           //             }
           //         }
           //         catch (TimeoutException te)
           //         {
           //             _log.ErrorFormat("Timeout nella contrassegnazione delle timbrature acquisitore app: " + te.Message);
           //         }
           //         catch (Exception ex)
           //         {
           //             _log.ErrorFormat("Errore nel metodo della contrassegnazione delle timbrature app:" + ex.Message);
           //         }
           //     });
           // }
        }

    }
}
