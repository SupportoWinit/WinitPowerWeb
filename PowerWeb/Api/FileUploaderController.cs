using Business.DataClasses.WebApiDataClasses;
using Business.IocFactory.FileDecoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PowerWeb.Api
{
    public class FileUploaderController : ApiController
    {
        public HttpResponseMessage Post(FileContainerJson[] jsonArray)
        {
            if (jsonArray == null)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    ReasonPhrase = "Json ricevuto = null"
                };
            }

            try
            {
                foreach (var json in jsonArray)
                    FileClockAppDecoder.Decode(json);
            }
            finally
            {
                foreach (var json in jsonArray)
                    BackupJsonObject(json, json.FileName.Split('.').Last());
            }

            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            };
        }
        private static void BackupJsonObject(FileContainerJson json, string jsonFileName)
        {
            string backUpFilesPath = System.Web.HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.Files_Input_Backup_Path);

            if (!Directory.Exists(backUpFilesPath))
                Directory.CreateDirectory(backUpFilesPath);

            backUpFilesPath = Path.Combine(backUpFilesPath, "FileJsonBackup");

            if (!Directory.Exists(backUpFilesPath))
                Directory.CreateDirectory(backUpFilesPath);

            backUpFilesPath = Path.Combine(backUpFilesPath, json.Device);

            if (!Directory.Exists(backUpFilesPath))
                Directory.CreateDirectory(backUpFilesPath);

            string fileName = String.Format("TmpJsonDownload-{0}-{1}.json", DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-fff"), jsonFileName);

            string completePath = $"{backUpFilesPath}\\{fileName}";

            File.WriteAllText(completePath, Newtonsoft.Json.Linq.JObject.FromObject(json).ToString());

        }



    }
}
