using Business.Repository;
using Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Services;
using DevExtreme.AspNet.Data;
using Business.DataClasses.WebMethodDataClasses;
using Business.DataClasses.DevExtremeUtilities;
using System.Reflection;
using Exports;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Business;
using log4net;
using Ionic.Zip;

namespace PowerWeb.Pages
{
    public partial class SegnalazioniPage : BasePage
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SegnalazioniPage));

        public static IEnumerable<Tab_Excel_Model> _pageExcelModels;

        public static IDictionary<string, string> _gridResources;

        private static ExportContext _exportContext;

        private static string _filesOutputPath
        {
            get { return String.Format(@"{0}{1}", AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Output_Path); }
        }
        private static string ImportPath
        {
            get { return HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path); }
        }



        public bool _filesToImport;



        protected void Page_Init(object sender, EventArgs e)
        {
            _filesToImport = IsSegnToImport(); //Check se ci sono files da importare

            _exportContext = new ExportContext();

            _pageExcelModels = RepoManager.Tab_Excel_ModelRepo.GetActiveTab_Excel_ModelsByPage("Segn");

            _gridResources = GetGridResources();

        }

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        [WebMethod]
        public static string Import()
        {
            List<KeyValuePair<string, string>> importErrors = new List<KeyValuePair<string, string>>();

            if (IsSegnToImport())
            {

                _log.InfoFormat("Sono state trovate segnalazioni da importare");

                IEnumerable<string> files = GetFilesNamesToImport();

                _log.InfoFormat("Sono stati trovati {0} files di segnalazioni", files.Count());

                List<Damage> segnalazioni = RepoManager.DamageRepo.ImportRowSegnalazioni(files, importErrors);

                RepoManager.DamageRepo.DbSet.AddRange(segnalazioni);
                RepoManager.DamageRepo.SaveChanges();

            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                filesToImport = IsSegnToImport()
            });
        }

        [WebMethod]
        public static string Elaborate(DateTime from, DateTime to)
        {
            RepoManager.DamageRepo.ElaborateRowSegnalazioni(from, to);


            return "OK";
        }

        [WebMethod]
        public static string GridLoad(LoadOptions data)
        {
            DataSourceLoadOptionsBase parameters = new DataSourceLoadOptions();

            DataSourceLoadOptions.ParseOptions(parameters, data);

            RepoManager.DamageRepo.Context.Configuration.ProxyCreationEnabled = false;

            var dataSource = DataSourceLoader.Load(RepoManager.DamageRepo.DbSet.AsNoTracking(), parameters);

            string serializedObject = JsonConvert.SerializeObject(dataSource, new Newtonsoft.Json.JsonSerializerSettings()
            {
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.None
            });

            RepoManager.DamageRepo.Context.Configuration.ProxyCreationEnabled = true;

            return serializedObject;
        }

        [WebMethod]
        public static string GridDelete(int key)
        {
            Damage proxy = new Damage() { Damage_Id = key };

            RepoManager.DamageRepo.DbSet.Attach(proxy);
            RepoManager.DamageRepo.DbSet.Remove(proxy);
            RepoManager.DamageRepo.SaveChanges();

            return "";
        }

        [WebMethod]
        public static string LookupLoad(LoadOptions loadOptions, string entity)
        {
            object data = LoadDynamicDataSource(loadOptions, entity);

            return JsonConvert.SerializeObject(data);
        }

        [WebMethod]
        public static string ExportToExcel(int excelModelId, DateTime from, DateTime to)
        {

            JObject response = new JObject();

            Tab_Excel_Model model = _pageExcelModels.First(c => c.ExcelModel_Id == excelModelId);

            _exportContext.SetExport(model);
            _exportContext.SetExportDateFrom(from);
            _exportContext.SetExportDateTo(to);

            JArray errors = new JArray();

            if (_exportContext.GenerateExport(ref errors))
            {
                response = JObject.FromObject(_exportContext.GetDownloadExportParams());
                if (errors.Any())
                    response.Add(nameof(errors), errors);
            }
            else
            {
                response.Add("fatalError", "Errore durante la generazione dell'export! Contattare l'assistenza!");
            }

            return JsonConvert.SerializeObject(response, Formatting.None);
        }

        [WebMethod]
        public static string GetFiles(int damage_Id)
        {
            JObject returnFiles = new JObject();

            var damage = RepoManager.DamageRepo.DbSet.Find(damage_Id);

            string path = Common.Properties.Settings.Default.Files_Input_UserFiles;

            path = HttpContext.Current.Server.MapPath(Path.Combine(path, damage.Pru.Codice_Pru.Trim()));

            string[] files = Directory.GetFiles(path, String.Format("{0}*", damage.MultimediaFileName));

            if (!files.Any())
            {
                returnFiles.Add("error", "Errore, non è stato trovato alcun file allegato...");
                return JsonConvert.SerializeObject(returnFiles);
            }



            if (files.Count() > 1)
            {
                using (ZipFile zipFile = new ZipFile())
                {
                    foreach (string singleFile in files)
                    {
                        FileInfo info = new FileInfo(singleFile);

                        string fileName = info.Name.Split('.').First();
                        string extension = info.Extension.Split('.').Last();

                        string filePath = string.Format("{0}{1}.{2}", path + "\\", fileName, extension);

                        zipFile.AddFile(filePath, @"\");
                    }

                    string zipPath = _filesOutputPath;
                    string zipName = Guid.NewGuid().ToString();
                    string extensionZip = "zip";

                    try
                    {
                        zipFile.Save(string.Format("{0}{1}.{2}", zipPath, zipName, extensionZip));
                    }
                    catch (Exception ex)
                    {
                        _log.ErrorFormat("Si è verificato un errore durante il salvataggio dello zip contenente {0} con exception: {1}", files.Count(), ex.Message);

                        returnFiles.Add("error", "Si è verificato un errore durante il download del file. Contattare l'assistenza!");

                        return JsonConvert.SerializeObject(returnFiles);
                    }


                    returnFiles.Add("fileName", zipName);
                    returnFiles.Add("path", zipPath);
                    returnFiles.Add("extension", extensionZip);

                }
            }
            else
            {
                try
                {
                    FileInfo info = new FileInfo(files.First());

                    string fileName = info.Name.Split('.').First();
                    string extension = info.Extension.Split('.').Last();

                    returnFiles.Add("fileName", fileName);
                    returnFiles.Add("path", path + "\\");
                    returnFiles.Add("extension", extension);
                    returnFiles.Add("delete", false);
                }
                catch (Exception ex)
                {
                    _log.ErrorFormat("Si è verificato un errore durante il download di un file con exception: {1}", ex.Message);

                    returnFiles.RemoveAll();

                    returnFiles.Add("error", "Si è verificato un errore durante il download del file. Contattare l'assistenza!");

                    return JsonConvert.SerializeObject(returnFiles);
                }
                
            }

            return JsonConvert.SerializeObject(returnFiles);
        }



        public static bool IsSegnToImport()
        {

            return Directory.GetFiles(ImportPath, "Segn_Da_Importare_*.txt").Any() || Directory.GetFiles(ImportPath, "Segn_Sospese_*.txt").Any();

        }

        #region PRIVATE INSTANCE METHODS

        IDictionary<string, string> GetGridResources()
        {

            Dictionary<string, string> resDictionary = new Dictionary<string, string>();

            resDictionary.Add("FLD_CODICE_DAMAGE", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_CODICE_DAMAGE"));
            resDictionary.Add("FLD_DATA_ORA_DAMAGE", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_DATA_ORA_DAMAGE"));
            resDictionary.Add("FLD_COL", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_COL"));
            resDictionary.Add("FLD_CANT", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_CANT"));
            resDictionary.Add("FLD_PRU", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_PRU"));
            resDictionary.Add("FLD_FRU", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_FRU"));
            resDictionary.Add("FLD_NOTE_DAMAGE", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_NOTE_DAMAGE"));
            resDictionary.Add("FLD_PDF_ATTACHMENT", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_PDF_ATTACHMENT"));
            resDictionary.Add("FLD_AUDIO_ATTACHMENT", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_AUDIO_ATTACHMENT"));
            resDictionary.Add("FLD_IMAGE_ATTACHMENT", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_IMAGE_ATTACHMENT"));
            resDictionary.Add("FLD_TAB_DAMAGE", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_TAB_DAMAGE"));
            resDictionary.Add("FLD_MULTIMEDIAFILENAME", RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_MULTIMEDIAFILENAME"));


            return resDictionary;
        }

        #endregion


        #region PRIVATE STATIC METHODS
        static IEnumerable<string> GetFilesNamesToImport()
        {
            List<string> paths = new List<string>();

            paths.AddRange(Directory.GetFiles(ImportPath, "Segn_Da_Importare_*.txt"));

            paths.AddRange(Directory.GetFiles(ImportPath, "Segn_Sospese_*.txt"));

            return paths;
        }

        static object LoadDynamicDataSource(LoadOptions loadOptions, string entityName)
        {
            DataSourceLoadOptions options = new DataSourceLoadOptions();

            DataSourceLoadOptions.ParseOptions(options, loadOptions);

            Type entityType = Type.GetType(String.Format("Domain.{0},Domain", entityName));

            MethodInfo loadMethod = RepoManager.Tab_GridLookupRepo.GetType().GetMethod("DynamicStore").MakeGenericMethod(new Type[] { entityType });

            return loadMethod.Invoke(null, new object[] { options });

        }

        #endregion
    }
}