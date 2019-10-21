using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

namespace PowerWeb.Api
{
    public class AudioReceiverController : GenericUploadFileApi
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(AudioReceiverController));
        protected override HttpStatusCode ExecuteOperation()
        {
            // di default la web api ritorna un valore di errore internoo
            var returnCode = HttpStatusCode.InternalServerError;

            // calcolod del nome del file in cui appoggiare il json ricevuto
            string jsonFilePath = GetTmpJsonFilePath();

            // recupero dalla richiesta http lo stream con i dati da scrivere
            var task = this.Request.Content.ReadAsStreamAsync();
            task.Wait();

            // inizializzazione della variabile che indica l'avvenuta scrittura con errore
            bool jsonWroteWithErrors = false;
            using (Stream requestStream = task.Result)
            {
                // scrittura dello stream su file
                using (Stream fileStream = File.Create(jsonFilePath))
                {
                    try
                    {
                        requestStream.CopyTo(fileStream);
                        fileStream.Close();
                        requestStream.Close();
                    }
                    catch (Exception e)
                    {
                        jsonWroteWithErrors = true;
                        _log.Error(String.Format("C'è stato un errore nella creazione della cartella {0}", e.Message));
                    }
                    finally
                    {
                        // in ogni caso al termine dell'operazione si procede allo svuotamento dello stream (di modo da liberare il file)
                        fileStream.Close();
                        fileStream.Dispose();
                    }
                }
            }
            
            if (!jsonWroteWithErrors)
            {

                if (RevertAndSaveAudio(File.ReadAllText(jsonFilePath)))
                {
                    returnCode = HttpStatusCode.OK;
                }
                else
                {
                    returnCode = HttpStatusCode.InternalServerError;
                    _log.Error(String.Format("C'è stato un errore nella  __RevertAndSaveAudio(File.ReadAllText(jsonFilePath) {0}"));
                }
            }
            return returnCode;
        }

        private bool RevertAndSaveAudio(string jsonData)
        {
            string nameFile = string.Empty;
            string device = string.Empty;

            Byte[] dataFile;


            // inizializzazione del valore di ritorno del metodo
            bool operationSuccessful = false;

            try
            {
                JArray audios = null;

                string filesInputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Input_Audio_Path.Replace("~", "").Replace("\\", ""));
                if (!Directory.Exists(filesInputPath))
                {
                    System.IO.Directory.CreateDirectory(filesInputPath);
                    _log.Error(String.Format("Creata la tabella per i file Audio (Files_Input_Audio_Path)"));
                }

                // parse della stringa json con i dati da trattare
                audios = JArray.Parse(jsonData);

                foreach (JObject audio in audios)
                {

                    //dispositivo che invia gli audio
                    device = audio["device"].ToString();
                    //nome del file audio
                    nameFile = audio["nameFile"].ToString();
                    //dati del file in stringa
                    string data = audio["dataFile"].ToString();
                    //traduzione file from string Base64 to Audio
                    dataFile = Convert.FromBase64String(data);

                    //creo cartella dispositivo.
                    string deviceDirectoryPath = filesInputPath + "/" + device;
                    if (!Directory.Exists(deviceDirectoryPath))
                    {
                        DirectoryInfo di = Directory.CreateDirectory(deviceDirectoryPath);
                    }
                    using (Stream audioTmp = File.Create(deviceDirectoryPath + "/" + nameFile))
                    {
                        try
                        {

                            MemoryStream a = new MemoryStream(dataFile);
                            a.CopyTo(audioTmp);
                            audioTmp.Close();
                            a.Close();

                        }
                        catch (Exception e)
                        {
                            //jsonWroteWithErrors = true;
                            _log.Error(String.Format("ERRORE creazione file.mp3 Fallita"));

                        }
                        finally
                        {
                            // in ogni caso al termine dell'operazione si procede allo svuotamento dello stream (di modo da liberare il file)
                            audioTmp.Close();
                            audioTmp.Dispose();
                        }
                    }
                    operationSuccessful = true;
                }
            }
            catch (Exception e)
            {
                operationSuccessful = false;
            }

            return operationSuccessful;
        }

        private string GetTmpJsonFilePath()
        {
            // calcolo della cartella (FilesInput applicativo) di scarico
            string filesInputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Input_JSON_Audio_Backup_Path.Replace("~", "").Replace("\\", ""));
            if (!Directory.Exists(filesInputPath))
            {
                System.IO.Directory.CreateDirectory(filesInputPath);
                _log.Error(String.Format("Creata la cartella per i backup dei file Audio (Files_Input_JSON_Audio_Backup_Path)"));

            }
            // calcolo del nome del file json
            DateTime now = DateTime.Now;
            string jsonFileName = String.Format("TmpJsonAudioByte-{0}.json", now.ToString("yyyy-MM-dd-hh-mm-ss-fff"));

            // ritorno della combinazione di cartella e nome del file 
            return Path.Combine(filesInputPath, jsonFileName);
        }
    }
}