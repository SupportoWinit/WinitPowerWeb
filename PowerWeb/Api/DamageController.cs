using Business.DataClasses.WebApiDataClasses;
using Business.Profile;
using Business.Repository;
using Domain;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System;
using System.Collections.Generic;
using log4net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace PowerWeb.Api
{
    public class DamageController : ApiController
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DamageController));

        private DamageJson[] _jsonArray;
        private HttpResponseMessage response;

        

        public HttpResponseMessage Post(DamageJson[] jsonArray)
        {
            if (!InitializeApiUser())
            {
                _log.ErrorFormat("Errore durante il parsing di una richesta http a causa della mancanza dell API user");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = "Api user non inizializzato!" };

            }
              
            _jsonArray = jsonArray;

            response = new HttpResponseMessage
            {
                StatusCode = ExecuteOperation()
            };

            return response;
        }

        private bool InitializeApiUser()
        {
            // inizializzazione del valore di ritorno del metodo
            bool userInitialized = false;

            // calcolo dell'utente con cui eseguire le operazioni
            Utenti apiUser = RepoManager.UtentiRepo.FirstOrDefault(ut => ut.Codice_Utente == Common.Properties.Settings.Default.APIUserName);

            // si procede con l'elaborazione solamente se l'utente è stato trovato
            if (apiUser != default(Utenti))
            {
                PowerWebMembershipProvider.InitializeUser(apiUser);
                userInitialized = true;
            }

            // ritorno del valore calcolato dal metodo
            return userInitialized;
        }

        protected HttpStatusCode ExecuteOperation()
        {
            DeviceBadgeResolver reportingResolver = new DeviceBadgeResolver();


            List<string> txtLines = new List<string>();

            try
            {
                txtLines.AddRange(reportingResolver.Decode(_jsonArray));
                CreateTxt(txtLines);

            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la decodifica dell'array di segnalazioni con exception {0}", ex.Message);
                return HttpStatusCode.InternalServerError;
            }
            finally
            {
                BackupJsonArray(_jsonArray);
            }


            return HttpStatusCode.OK;
        }

        private void BackupJsonArray(DamageJson[] json)
        {
            string backUpFilesPath = System.Web.HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.Files_Input_JSON_Segnal_Backup_Path);

            if (!Directory.Exists(backUpFilesPath))
                Directory.CreateDirectory(backUpFilesPath);

            string fileName = String.Format("TmpJsonDownload-{0}.json", DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-fff"));

            string completePath = $"{backUpFilesPath}{fileName}";

            File.WriteAllText(completePath, JArray.FromObject(json).ToString());

        }

        private void CreateTxt(List<string> txtLines)
        {
            string filesInputPath = System.Web.HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path);


            string fileName = $"Segn_Da_Importare_{DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-fff")}.txt";

            string completePath = $"{filesInputPath}{fileName}";

            File.WriteAllLines(completePath, txtLines);
        }

    }

    class DeviceBadgeResolver
    {

        private const string pattern = "{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};";

        public List<string> Decode(DamageJson[] jsonArray)
        {
            List<string> txtLines = new List<string>();

            foreach (var json in jsonArray)
            {
                txtLines.Add(string.Format(pattern,

                   GetPru(json),                    //0
                   GetFru(json),                    //1
                   GetYear(json),                   //2
                   GetMonth(json),                  //3
                   GetDay(json),                    //4
                   GetHour(json),                   //5
                   GetMinutes(json),                //6 
                   GetCant(json),                   //7
                   GetCol(json),                    //8
                   GetText(json),                   //9            
                   GetImageBool(json),              //10
                   GetAudioBool(json),              //11
                   GetMultimediaFileName(json),     //12
                   GetDamageType(json),             //13
                   GetDamageSubType(json)           //14

                   ));
            }

            return txtLines;
        }

        string GetPru(DamageJson json)
        {
            return json.Type == Common.JsonSegnalazioneTypeEnum.Portatile ? json.DeviceCode : json.BadgeCode;
        }

        string GetFru(DamageJson json)
        {
            return json.Type == Common.JsonSegnalazioneTypeEnum.Portatile ? json.BadgeCode : json.DeviceCode;
        }
        
        string GetYear(DamageJson json)
        {
            return json.DamageDateTime.Year.ToString();
        }

        string GetMonth(DamageJson json)
        {
            return json.DamageDateTime.Month.ToString("00");
        }

        string GetDay(DamageJson json)
        {
            return json.DamageDateTime.Day.ToString("00");
        }

        string GetHour(DamageJson json)
        {
            return json.DamageDateTime.Hour.ToString("00");
        }

        string GetMinutes(DamageJson json)
        {
            return json.DamageDateTime.Minute.ToString("00");
        }

        string GetCant(DamageJson json)
        {
            return json.CantCode.ToString();
        }

        string GetCol(DamageJson json)
        {
            return json.ColCode.ToString();
        }

        string GetText(DamageJson json)
        {
            return json.DamageNote;
        }

        int GetImageBool(DamageJson json)
        {
            return json.DamageHasImage ? 1 : 0;
        }

        int GetAudioBool(DamageJson json)
        {
            return json.DamageHasAudio ? 1 : 0;
        }

        string GetMultimediaFileName(DamageJson json)
        {
            return json.MultimediaName;
        }

        string GetDamageType(DamageJson json)
        {
            return json.DescriptionDamageType;
        }

        string GetDamageSubType(DamageJson json)
        {
            return json.DescriptionDamageSubType;

        }
    }

}
