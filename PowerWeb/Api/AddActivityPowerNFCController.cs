using Business.Repository;
using Domain;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;

namespace PowerWeb.Api
{
    public class AddActivityPowerNFCController : GenericUploadFileApi
    {

        private static readonly ILog _log = LogManager.GetLogger(typeof(AddActivityPowerNFCController));

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
                    catch (Exception)
                    {
                        jsonWroteWithErrors = true;
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
                if (AddActivityFromJson(File.ReadAllText(jsonFilePath)))
                    returnCode = HttpStatusCode.OK;
                else
                    returnCode = HttpStatusCode.InternalServerError;
            }

            return returnCode;
        }

        private bool AddActivityFromJson(string jsonString)
        {
            _log.DebugFormat("Creazione attività da JSON in seguito a ricezione da APP");

            bool finished = true;
            var request = JObject.Parse(jsonString);
            Cant cant = RepoManager.CantRepo.Init();
            Fru newFru = RepoManager.FruRepo.Init();
            Fru_Cant newFruCant = RepoManager.Fru_CantRepo.Init();

            try
            {
                foreach (var property in request)
                {
                    typeof(Cant).GetProperty(property.Key.ToString()).SetValue(cant, property.Value.ToString());
                }

                cant.Codice_in_clockApp = request["Codice_Cantiere"].ToString();

                if(RepoManager.ParamRepo.GetCustomizationFromEnum(Common.CustomizationEnum.ClockAppConvertCantToActivity) == 1)
                {
                    cant.Tipologia_Can = "ATT";
                }

                var errors = RepoManager.CantRepo.Check(cant, true);

                if (!errors.Any())
                {
                    RepoManager.CantRepo.Add(cant, true);
                    newFru.Codice_Fru = newFru.N_Serie_Fru = cant.Codice_in_clockApp;
                    RepoManager.FruRepo.Add(newFru, true);
                    newFruCant.Fru_Id = newFru.Fru_Id;
                    newFruCant.Cant_Id = cant.Cant_Id;
                    newFruCant.Abilitazione_Data_Inizio_Fru_Can = DateTime.Now;
                    RepoManager.Fru_CantRepo.Add(newFruCant, true);
                    _log.Info(String.Format("Attività/Cantiere inserita con successo {0}", cant.Codice_in_clockApp));
                }
                else
                {
                    _log.WarnFormat("Errori CHECK : {0}", string.Join("; ", errors.Values));

                    finished = false;
                }
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                                .SelectMany(x => x.ValidationErrors)
                                .Select(x => x.ErrorMessage);

                var fullErrorMessage = string.Join("; ", errorMessages);

                _log.Error(String.Format("Errori di validazione durante la creazione di un'attività con i seguenti errori : {0}", fullErrorMessage));

            }
            catch (Exception ex)
            {
                finished = false;
                _log.Error(String.Format("C'è stato un errore la creazione dell'attività {0}", ex.Message));
            }
            return finished;
        }

        private string GetTmpJsonFilePath()
        {
            // calcolo della cartella (FilesInput applicativo) di scarico
            string filesInputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Input_JSON_Activity_Backup_Path.Replace("~", "").Replace("\\", ""));
            if (!Directory.Exists(filesInputPath))
            {
                System.IO.Directory.CreateDirectory(filesInputPath);
            }
            // calcolo del nome del file json
            DateTime now = DateTime.Now;
            string jsonFileName = String.Format("TmpJsonAddActivity-{0}.json", now.ToString("yyyy-MM-dd-hh-mm-ss-fff"));

            // ritorno della combinazione di cartella e nome del file 
            return Path.Combine(filesInputPath, jsonFileName);
        }
    }
}