using Business.DataClasses.GeoBadgeDTOs;
using Business.ExternalImports;
using Business.HttpHub;
using Business.HttpHub.HttpHubs;
using Business.RegFileCreators;
using Business.Repository;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Business.ExternalImport.Implementations
{
    public class GeoBadgeExternalConnection : IExternalImport
    {
        private HttpModule bridge;
        private IEnumerable<GeoBadgeReg> registrazioni;
        private IRegFileCreator<GeoBadgeReg> fileWriter;

        /// <summary>
        /// Container per le varie stringhe di configurazione dell'httpClient
        /// </summary>
        private IDictionary<string, string> connectionConfig { get; set; }


        /// <summary>
        /// Container per le varie stringhe di configurazione per le chiamate api
        /// </summary>
        private IDictionary<string, string> apiPaths { get; set; }

        public GeoBadgeExternalConnection(IDictionary<string, string> connectionConfig, IDictionary<string, string> apiPaths)
        {
            this.connectionConfig = connectionConfig;
            this.apiPaths = apiPaths;

            bridge = new GeoBadgeHub();
            bridge.Host = connectionConfig["host"];
            fileWriter = new GeoBadgeRegFileCreator();

        }

        #region Timbrature

        /// <summary>
        /// Richiede le timbrature seguendo l'ultimo indice salvato 
        /// </summary>
        public void GetTimbrature()
        {
            int index = RepoManager.ParamRepo.ParametersRow.Indice_Timbrature_GeoBadge;

            registrazioni = bridge.Get<List<GeoBadgeReg>>(CreateStandardPayload(index), apiPaths["getTimbrature"]);

            if (registrazioni != null && registrazioni.Any())
            {
                BusinessService.BackUpJsonObject(registrazioni, Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path);

                RepoManager.ParamRepo.SaveGeoBadgeRegIndex(Math.Max(registrazioni.Max(c => c.Id) + 1, index));
            }
        }


        /// <summary>
        /// Richiede le timbrature seguendo un range di date
        /// </summary>
        public void GetTimbrature(DateTime from, DateTime to)
        {
            registrazioni = bridge.Get<List<GeoBadgeReg>>(CreateStandardPayload(from, to), apiPaths["getTimbrature"]);

            if (registrazioni.Any())
            {
                BusinessService.BackUpJsonObject(registrazioni, Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path);
            }
        }

        #endregion

        /// <summary>
        /// Scrive le timbrature su file
        /// </summary>
        public void WriteToFile()
        {
            fileWriter.WriteToFile(registrazioni);
        }

        JObject CreateStandardPayload(int index)
        {
            JObject request = JObject.FromObject(new
            {
                uid = connectionConfig["uid"],
                pw = connectionConfig["pw"],
                idTenant = connectionConfig["idTenant"],
                DaIdTimbratura = index
            });

            return request;
        }

        JObject CreateStandardPayload(DateTime from, DateTime to)
        {
            JObject request = JObject.FromObject(new
            {
                uid = connectionConfig["uid"],
                pw = connectionConfig["pw"],
                idTenant = connectionConfig["idTenant"],
                sDaTimbraturaData = from,
                aTimbraturaData = to
            });

            return request;
        }
    }
}
