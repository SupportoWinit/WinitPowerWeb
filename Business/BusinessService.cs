using BingMapsRESTToolkit;
using Business.BusinessExtension;
using Business.DataClasses;
using Business.LicenceServiceReference;
using Business.MDBSchema;
using Business.MDBSchema.PowerMDBDataSetTableAdapters;
using Business.Profile;
using Business.Repository;
using Business.Repository.Custom;
using Common;
using Common.Properties;
using DevExpress.Data.PLinq.Helpers;
using DevExpress.XtraPrinting.Native;
using Domain;
using Domain.Extensions;
using log4net;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Business
{
    public static class IListExtension
    {
        public static IList ToNonGenericList(this IQueryable query)
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(query.ElementType), query);
        }


    }

    public static class BusinessService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(BusinessService));

        const string FIELD = "FLD_";
        const string ERROR = "ERR_";
        const string CONTROL = "CTRL_";
        const string TABDECOD = "TD_";
        const string MENU = "MENU_";
        const string STRING = "STR_";

        const string tabString = "Tabs";
        const string fieldString = "Fields";
        const string cantString = "Cant";
        const string colString = "Col";
        const string paramString = "Param";


        #region LOGIN

        public static bool ValidateUser(string userName, string password, LoginValidationResult loginValidationResult)
        {
            var user = Membership.GetUser(userName);

            if (user == null)
            {
                loginValidationResult.AddFormErrors("userName", "Attenzione,l'utente inserito non esiste!");
                loginValidationResult.isValid = false;
                _log.InfoFormat("Tentativo di login fallito da parte dell'utente {0}; Il nome utente è inesistente", userName);
            }
            else if (user.GetPassword() != password)
            {
                loginValidationResult.AddFormErrors("passWord", "Attenzione,la password inserita è errata!");
                loginValidationResult.isValid = false;
                _log.InfoFormat("Tentativo di login fallito da parte dell'utente {0}; La password è errata", userName);
            }
            else if (!IsSuperUserCorrect(userName))
            {
                loginValidationResult.AddFormErrors("userName", "Attenzione, l'utente inserito non è abilitato!");
                loginValidationResult.isValid = false;
                _log.InfoFormat("Tentativo di login fallito da parte dell'utente {0}; L'utente inserito è un utente supervisore per tanto non può effettuare il login", userName);
            }
            else
            {
                if (PowerWebMembershipProvider.IsAdminUserMissing())
                {
                    loginValidationResult.missingAdminUser = true;

                    int userLvl = RepoManager.UtentiRepo.DbSet.Single(us => us.Codice_Utente == userName).Liv_Utente;

                    if (userLvl == Common.Properties.Settings.Default.Winit_Level)
                    {
                        loginValidationResult.isWinitUser = true;
                        loginValidationResult.validationError = "Utente con livello di amministratore mancante";
                    }
                    else
                    {
                        loginValidationResult.validationError = "Utente con livello di amministratore mancante. Non è possibile accedere al sistema!";
                    }
                }
                else
                {
                    var res = PowerWebMembershipProvider.CheckUserPassWordExpiration(userName);
                    loginValidationResult.isValid = res.isValid;
                    loginValidationResult.isDoubleLoginAllowed = RepoManager.UtentiRepo.DbSet.First(u => u.Codice_Utente == userName).AllowSupervisedLogin;
                    loginValidationResult.validationError = res.validationError;

                    if (loginValidationResult.isValid && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.EnableDoubleCheckLogin) == 1)
                    {
                        if (ValidateSuperUser(userName, password, new LoginValidationResult() { isValid = true }))
                        {
                            StartUserSession(userName, userName);
                        }
                    }
                }
            }

            return loginValidationResult.isValid;
        }

        public static bool ValidateSuperUser(string userName, string password, LoginValidationResult loginValidationResult)
        {
            var user = Membership.GetUser(userName);

            if (user == null)
            {
                loginValidationResult.AddFormErrors("superUserName", "Attenzione,l'utente inserito non esiste!");
                loginValidationResult.isValid = false;
                _log.InfoFormat("Tentativo di login fallito da parte dell'utente {0}; Il nome utente è inesistente", userName);
            }
            else if (user.GetPassword() != password)
            {
                loginValidationResult.AddFormErrors("superUserPassword", "Attenzione,la password inserita è errata!");
                loginValidationResult.isValid = false;
                _log.InfoFormat("Tentativo di login fallito da parte dell'utente {0}; La password è errata", userName);
            }
            else if (!IsSuperUserCorrect(userName))
            {
                loginValidationResult.AddFormErrors("superUserName", "Attenzione, non è stato inserito un utente amministratore valido!");
                loginValidationResult.isValid = false;
                _log.InfoFormat("Tentativo di login fallito da parte dell'utente {0}; Inserito utente amministratore di livello inferiore a 10 ", userName);
            }

            return loginValidationResult.isValid;
        }

        private static bool IsSuperUserCorrect(string username)
        {
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.EnableDoubleCheckLogin) == 0)
                return true;

            string filePath = HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.SuperUsersFilePath);
            XDocument doc = XDocument.Load(filePath);

            bool exist = false;

            doc.Descendants("SuperUsers").DescendantNodes().ForEach(c =>
            {
                if (((XElement)c).FirstAttribute.Value == username)
                {
                    exist = true;
                }
            });



            return exist;
        }

        public static void StartUserSession(string userName, string superUserName = "")
        {
            var user = RepoManager.UtentiRepo.First(us => us.Codice_Utente == userName);

            var superUser = RepoManager.UtentiRepo.FirstOrDefault(us => us.Codice_Utente == superUserName);

            PowerWebMembershipProvider.InitializeUser(user, superUser);
        }

        #endregion


        /// <summary>
        /// Determina se esistono i filtri per filiale e/o per responsabile abbinati agli utenti.
        /// </summary>
        /// <returns><c>true</c>se esistono i filtri per filiale e/o per responsabile abbinati agli utenti; altrimenti <c>false</c></returns>
        public static bool IsToApplyDomainFilter()
        {
            bool result = false;

            if (PowerWebContext.Current.User != null)
            {
                //viene estratto l'id della filiale se non è prsente viene settato null
                int utentiFilCount = PowerWebContext.Current.User.Utenti_Fil != null ? PowerWebContext.Current.User.Utenti_Fil.Count : 0;
                //viene estratto l'ide del responsabile
                int utentiRespCount = PowerWebContext.Current.User.Utenti_Resp != null ? PowerWebContext.Current.User.Utenti_Resp.Count : 0;

                //se l'id è maggiore di 0 allora c'è da filtrare mediante filiale/responsabile
                result = utentiFilCount > 0 || utentiRespCount > 0 || PowerWebContext.Current.User.Col_Id.HasValue || PowerWebContext.Current.User.Cli_Id.HasValue;
            }
            return result;
        }

        #region Localizzazione in Lingua

        public static String GetLocalizedString(string baseString, params string[] args)
        {
            String result = null;

            if (RepoManager.ResourcesRepo.ResourcesDictionary.ContainsKey(baseString))
            {
                result = GetLocalizedString(baseString);

                List<String> argsValues = new List<string>();
                foreach (var arg in args)
                    argsValues.Add(GetLocalizedString(arg));

                if (!String.IsNullOrEmpty(result) && args.Count() > 0)
                    result = String.Format(result, argsValues.ToArray());
            }

            if (String.IsNullOrEmpty(result))
                result = "### - " + baseString.ToString();

            return result;
        }

        public static String GetLocalizedString(PowerWebResources baseString, params PowerWebResources[] args)
        {
            List<String> argsValues = new List<string>();
            foreach (var arg in args)
                argsValues.Add(GetLocalizedString(arg.ToString()));

            return GetLocalizedString(baseString.ToString(), argsValues.ToArray());
        }

        public static String GetLocalizedStringStrParam(PowerWebResources baseString, params string[] args)
        {
            return GetLocalizedString(baseString.ToString(), args.Select(arg => GetLocalizedString(arg)).ToArray());
        }

        public static String GetLocalizedString(String value, ResourceTypeEnum type = ResourceTypeEnum.None)
        {
            String result = value;

            if (!String.IsNullOrEmpty(value))
            {
                value = value.ToUpper();

                switch (type)
                {
                    case ResourceTypeEnum.None:
                        result = RepoManager.ResourcesRepo.GetResourcesDictionaryString(value);
                        break;
                    case ResourceTypeEnum.Field:
                        result = RepoManager.ResourcesRepo.GetResourcesDictionaryString(new StringBuilder(FIELD).Append(value).ToString());
                        break;
                    case ResourceTypeEnum.Error:
                        result = RepoManager.ResourcesRepo.GetResourcesDictionaryString(new StringBuilder(ERROR).Append(value).ToString());
                        break;
                    case ResourceTypeEnum.Control:
                        result = RepoManager.ResourcesRepo.GetResourcesDictionaryString(new StringBuilder(CONTROL).Append(value).ToString());
                        break;
                    case ResourceTypeEnum.TabDecod:
                        result = RepoManager.ResourcesRepo.GetResourcesDictionaryString(new StringBuilder(TABDECOD).Append(value).ToString());
                        break;
                    case ResourceTypeEnum.String:
                        result = RepoManager.ResourcesRepo.GetResourcesDictionaryString(new StringBuilder(STRING).Append(value).ToString());
                        break;
                    case ResourceTypeEnum.ReportLabel:
                        result = RepoManager.ResourcesRepo.GetResourcesDictionaryString(new StringBuilder(STRING).Append(value).ToString());
                        break;
                    case ResourceTypeEnum.Menu:
                        result = RepoManager.ResourcesRepo.GetResourcesDictionaryString(value);
                        break;
                    case ResourceTypeEnum.Grid:
                        result = RepoManager.ResourcesRepo.GetResourcesDictionaryString(value);
                        break;
                }
            }

            if (String.IsNullOrEmpty(result))
                result = "### - " + value;

            return result;
        }

        #endregion

        private static Dictionary<Utenti, KeyValuePair<double, string>> _importDataStatusDictionary = new Dictionary<Utenti, KeyValuePair<double, string>>();

        public static Dictionary<Utenti, KeyValuePair<double, string>> ImportDataStatusDictionary
        {
            get
            {
                return _importDataStatusDictionary;
            }
            set
            {
                _importDataStatusDictionary = value;
            }
        }

        #region Dictionary per gestione progress bar e messaggi modifica data blocco

        private static Dictionary<Utenti, KeyValuePair<double, string>> _editBlockDateStatusDictionary = new Dictionary<Utenti, KeyValuePair<double, string>>();

        public static Dictionary<Utenti, KeyValuePair<double, string>> EditBlockDateStatusDictionary
        {
            get
            {
                return _editBlockDateStatusDictionary;
            }

            set
            {
                _editBlockDateStatusDictionary = value;
            }
        }

        private static Dictionary<Utenti, KeyValuePair<double, string>> _editStoredRegDateStatusDictionary = new Dictionary<Utenti, KeyValuePair<double, string>>();
        public static Dictionary<Utenti, KeyValuePair<double, string>> EditStoredRegDateStatusDictionary
        {
            get
            {
                return _editStoredRegDateStatusDictionary;
            }

            set
            {
                _editStoredRegDateStatusDictionary = value;
            }
        }

        #endregion

        #region Trattamento dell'IMPORT da ACCESS (POWER e/o COMO0()
        public static Dictionary<string, List<Dictionary<string, string>>> ImportData(String path, ImportDBTypeEnum type)
        //IMPORT DEI DATI DA ACCESS (Power e/o COMO 08)
        {
            //Abbiamo deciso di non importare: TAB_AUT, TAB_FESTIVI, UTENTI, PARAMETRI, INPUT
            //Importeremo più avanti: TAB_ORARI, TAB_ORARI_CANT, TAB_ORARI_TIPO (Massimo 12/09/2012)
            // NOTA SE VENGONO TROVATE DELLE RIL (Power)--- > ALLORA VIENE SALTATO IL CARICAMENTO DELLE REG_FIS (COMO08)
            // AGGIUNTA 
            //          ELABORAZIONE DI ACCOPPIAMENTO DELLA RIL DI ENTRATA ALLA RIL DI USCITA
            //          ELABORAZIONE DI ACCOPPIAMENTO DELLE ATTIVITA' ALLE RIL (SOLO PER MOSAICO CHE LE RICEVE DAI PASS)
            //          ELABORAZIONE DELLE RIL SOSPESE IN INPUT_FISICO
            // TIPOIMPORT : "STD"     = Import Normale
            //            : <p ID="004" Des="Import da Access" Att="1"/>
            //                         "Import x Mosaico che ha le seguenti Particolarità
            //                          1) La Tab Filiali viene caricata nella Tabella RESP
            //                          2) IL Campo Filale_Cant                                            
            var errors = new Dictionary<string, List<Dictionary<string, string>>>();
            ILog oLog = LogManager.GetLogger("Business.CommonServiceBiz");
            PowerMDBDataSet currentDS = new PowerMDBDataSet();
            string tableName = null;
            var CustomizationVersion = 0;


            if (type == ImportDBTypeEnum.mdb)
            {
                //estraggo il tipo di personalizzazione per l'import da Access
                CustomizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomImportFromAccessEnum);

                IEnumerable<Tab_Chk_Imp> toNotImportTables = RepoManager.Tab_Chk_ImpRepo.Find(
                  x => x.Chiave_Record_Tab_Check_Imp == null && x.Stato_Record_Tab_Check_Imp);
                string strAccessConn = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + path;
                OleDbConnection myAccessConn = null;
                //try
                //{
                myAccessConn = new OleDbConnection(strAccessConn);
                oLog.WarnFormat(
                    "----------------------------------- INIZIO IMPORT DA ACCESS ----------------------------------------",
                    "Cant");
                oLog.DebugFormat(
                    "----------------------------------- INIZIO IMPORT DA ACCESS ----------------------------------------",
                    "Cant");

                #region 1) IMPORT FIL

                tableName = "Fil";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    oLog.DebugFormat(
                        "----------------------------------- INIZIO IMPORT DATI ----------------------------------------",
                        "FIL");
                    using (Tab_FilialiTableAdapter tableAdapter = new Tab_FilialiTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Tab_Filiali);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        //Riceve il Dictionary degli Errori dai Singoli ImportFromDataSet dei Corrispondenti Repository
                        var filErrors = RepoManager.FilRepo.ImportFromDataSet(currentDS);
                        if (filErrors.Count > 0)
                            errors.Add(tableName, filErrors);
                        sw.Stop();

                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion //ok

                #region 2) IMPORT RESP

                tableName = "Resp";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    oLog.DebugFormat(
                        "----------------------------------- INIZIO IMPORT DATI ----------------------------------------",
                        "RESP");
                    using (Tab_RespTableAdapter tableAdapter = new Tab_RespTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Tab_Resp);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var respErrors = RepoManager.RespRepo.ImportFromDataSet(currentDS);
                        if (respErrors.Count > 0)
                            errors.Add(tableName, respErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 3) IMPORT Tab_Decod

                tableName = "Tab_Decod";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (Tab_DecodTableAdapter tableAdapter = new Tab_DecodTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Tab_Decod);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var tab_DecodErrors = RepoManager.Tab_DecodRepo.ImportFromDataSet(currentDS, myAccessConn);
                        if (tab_DecodErrors.Count > 0)
                            errors.Add(tableName, tab_DecodErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 4) IMPORT CLI

                tableName = "Cli";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (CliTableAdapter tableAdapter = new CliTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Cli);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var cliErrors = RepoManager.CliRepo.ImportFromDataSet(currentDS);
                        if (cliErrors.Count > 0)
                            errors.Add(tableName, cliErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 5) IMPORT CANT

                tableName = "Cant";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (CantTableAdapter tableAdapter = new CantTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Cant);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var cantErrors = RepoManager.CantRepo.ImportFromDataSet(currentDS);
                        if (cantErrors.Count > 0)
                            errors.Add(tableName, cantErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 6) IMPORT CANT_VAR

                tableName = "Cant_Var";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (Cant_VarTableAdapter tableAdapter = new Cant_VarTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Cant_Var);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var cant_VarErrors = RepoManager.Cant_VarRepo.ImportFromDataSet(currentDS);
                        if (cant_VarErrors.Count > 0)
                            errors.Add(tableName, cant_VarErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 7) IMPORT CANT_NOTE

                tableName = "Cant_Note";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (Cant_NoteTableAdapter tableAdapter = new Cant_NoteTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Cant_Note);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var cant_NoteErrors = RepoManager.Cant_NoteRepo.ImportFromDataSet(currentDS);
                        if (cant_NoteErrors.Count > 0)
                            errors.Add(tableName, cant_NoteErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 8) IMPORT COL

                tableName = "Col";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (ColTableAdapter tableAdapter = new ColTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Col);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var colErrors = RepoManager.ColRepo.ImportFromDataSet(currentDS);
                        if (colErrors.Count > 0)
                            errors.Add(tableName, colErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 9) IMPORT Col_Var

                tableName = "Col_Var";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (Col_VarTableAdapter tableAdapter = new Col_VarTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Col_Var);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var col_VarErrors = RepoManager.Col_VarRepo.ImportFromDataSet(currentDS);
                        if (col_VarErrors.Count > 0)
                            errors.Add(tableName, col_VarErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 10) IMPORT Col_Note

                tableName = "Col_Note";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (Col_NoteTableAdapter tableAdapter = new Col_NoteTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Col_Note);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var col_NoteErrors = RepoManager.Col_NoteRepo.ImportFromDataSet(currentDS);
                        if (col_NoteErrors.Count > 0)
                            errors.Add(tableName, col_NoteErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 11) IMPORT Pru

                tableName = "Pru";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (PruTableAdapter tableAdapter = new PruTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Pru);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var pruErrors = RepoManager.PruRepo.ImportFromDataSet(currentDS);
                        if (pruErrors.Count > 0)
                            errors.Add(tableName, pruErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 12) IMPORT Fru

                tableName = "Fru";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (FruTableAdapter tableAdapter = new FruTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Fru);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var fruErrors = RepoManager.FruRepo.ImportFromDataSet(currentDS, tableName);
                        if (fruErrors.Count > 0)
                            errors.Add(tableName, fruErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 13) IMPORT MatFRU

                tableName = "MatrFru";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (MatrFruTableAdapter tableAdapter = new MatrFruTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.MatrFru);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var fruErrors = RepoManager.FruRepo.ImportFromDataSet(currentDS, tableName);
                        if (fruErrors.Count > 0)
                            errors.Add(tableName, fruErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 14) IMPORT Pru_Col

                tableName = "Pru_Col";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (Pru_ColTableAdapter tableAdapter = new Pru_ColTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Pru_Col);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var pru_ColErrors = RepoManager.Pru_ColRepo.ImportFromDataSet(currentDS);
                        if (pru_ColErrors.Count > 0)
                            errors.Add(tableName, pru_ColErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 15)IMPORT Fru_Cant

                tableName = "Fru_Cant";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (Fru_CantTableAdapter tableAdapter = new Fru_CantTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Fru_Cant);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var fru_CantErrors = RepoManager.Fru_CantRepo.ImportFromDataSet(currentDS);
                        if (fru_CantErrors.Count > 0)
                            errors.Add(tableName, fru_CantErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 16) IMPORT Tab_Dist (PER ADESSO SOLO I RECORD "G")

                tableName = "Tab_Dist";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (Tab_DistTableAdapter tableAdapter = new Tab_DistTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Tab_Dist);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        RepoManager.Tab_DistRepo.ImportFromDataSet(currentDS);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                #endregion

                #region 17) IMPORT Reg/Reg_Fis/Pass
                tableName = "Ril";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (RilTableAdapter tableAdapter = new RilTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Ril);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var regErrors = RepoManager.RegRepo.ImportFromDataSet(currentDS, tableName);
                        if (regErrors.Count > 0)
                            errors.Add(tableName, regErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }


                tableName = "Reg_Fis";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (Reg_FisTableAdapter tableAdapter = new Reg_FisTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Reg_Fis);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var regErrors = RepoManager.RegRepo.ImportFromDataSet(currentDS, tableName);
                        if (regErrors.Count > 0)
                            errors.Add(tableName, regErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                tableName = "Pass";
                if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                {
                    using (PassTableAdapter tableAdapter = new PassTableAdapter())
                    {
                        if (myAccessConn.State == ConnectionState.Broken)
                            myAccessConn = new OleDbConnection(strAccessConn);
                        tableAdapter.Connection = myAccessConn;
                        tableAdapter.Fill(currentDS.Pass);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var regErrors = RepoManager.RegRepo.ImportFromDataSet(currentDS, tableName);
                        if (regErrors.Count > 0)
                            errors.Add(tableName, regErrors);
                        sw.Stop();
                        oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                            CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                    }
                }

                if (CustomizationVersion != 0)
                {
                    int importCustomizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AccessImportTypeEnum);

                    // NEL CASO SI TRATTI DI UN IMPORT X MOSAICO  allora viene eseguita anche l'operazione di abbinare le attività <p ID="004" Des="Import da Access" Att="1"/>
                    if (importCustomizationVersion == (int)AccessImportTypeEnum.Mosaico)
                    {
                        tableName = "AbbinaAtt";
                        if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            var regErrors = RepoManager.RegRepo.ImportFromDataSet(null, tableName);
                            if (regErrors.Count > 0)
                                errors.Add(tableName, regErrors);
                            sw.Stop();
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
                                CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                        }
                    }
                }

                // Eseguo IMport delle Registrazioni Sospese in INPUT_FISICO 
                tableName = "InputFisico";

              // if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
              // {
              //     using (InputFisicoTableAdapter tableAdapter = new InputFisicoTableAdapter())
              //     {
              //         if (myAccessConn.State == ConnectionState.Broken)
              //             myAccessConn = new OleDbConnection(strAccessConn);
              //         tableAdapter.Connection = myAccessConn;
              //         tableAdapter.Fill(currentDS.InputFisico);
              //         Stopwatch sw = new Stopwatch();
              //         sw.Start();
              //         var regErrors = RepoManager.RegRepo.ImportFromDataSet(currentDS, tableName);
              //         if (regErrors.Count > 0)
              //             errors.Add(tableName, regErrors);
              //         sw.Stop();
              //         oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.",
              //             CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
              //     }
              // }
                #endregion

                //Segnalo la Fine dell'IMPORT da ACCESS
                ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100d,
                            BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORT_TERMINATO));
                //}
                //catch (Exception ex)
                //{
                //    throw ex;
                //}
            }
            return errors;
        }

        #endregion

        #region Gestone di BING/GPS

        // This method makes the initial CalculateRoute asynchronous request using the results of the Geocode Service.
        public static Route GetRoute(Coordinate[] coordinates)
        //restituisce il Percorso fra 2 Punti (KM e Tempo percorrenza)
        {
            Route route = null;
            try
            {
                // Set the token.
                RouteRequest routeRequest = new RouteRequest();
                routeRequest.BingMapsKey = RepoManager.ParamRepo.ParametersRow.BingKey;
                // Return the route points so the route can be drawn.
                routeRequest.RouteOptions = new RouteOptions();
                // Set the waypoints of the route to be calculated using the Geocode Service results stored in the geocodeResults variable.
                routeRequest.Waypoints = new List<SimpleWaypoint>();

                foreach (var coord in coordinates)
                {
                    routeRequest.Waypoints.Add(new SimpleWaypoint(coord));
                }

                var response = Task.Run(() => ServiceManager.GetResponseAsync(routeRequest)).Result;                

                if (response.StatusCode == 200)
                {
                    route = response.ResourceSets[0].Resources[0] as Route;
                }
            }
            catch (Exception ex)
            {
                _log.Warn(String.Format("Impossibile calcolare distanza tramite GPS Bing per le coordinate di partenza ('{0}','{1}') e di arrivo ('{2}','{3}') con exception {4}",
                    coordinates[0].Latitude, coordinates[0].Longitude, coordinates[1].Latitude, coordinates[1].Longitude, ex.Message));
            }

            return route;

        }

        private static RouteServiceReference.Waypoint GeocodeResultToWaypoint(GeocodeServiceReference.GeocodeResult result)
        //Usata dal Calcolo del Percorso
        {
            RouteServiceReference.Waypoint waypoint = new RouteServiceReference.Waypoint();
            waypoint.Description = result.DisplayName;
            waypoint.Location = new RouteServiceReference.Location();
            waypoint.Location.Latitude = result.Locations[0].Latitude;
            waypoint.Location.Longitude = result.Locations[0].Longitude;
            return waypoint;
        }

        public static Location GetGeocode(string address)
        //restituisce LAT/Long partendo da un Indirizzo (CAP/COMUNE/INDIRIZZO)
        {
            Location location = null;

            try
            {
                GeocodeRequest geocodeRequest = new GeocodeRequest();

                // Set the credentials using a valid Bing Maps key
                geocodeRequest.BingMapsKey = RepoManager.ParamRepo.ParametersRow.BingKey;

                // Set the full address query
                geocodeRequest.Query = address;

                // Make the geocode request
                var response = Task.Run(() => ServiceManager.GetResponseAsync(geocodeRequest)).Result;

                //if (response.StatusCode == 200)
                //{
                    var geo = new ReverseGeocodeService("a442f14174a945dd9aff62023727b914");
                    location = Task.Run(() => geo.OttieniLocationDaIndirizzoAsync(address)).Result;

                //}
            }
            catch (Exception ex)
            {
                _log.Error(String.Format("Errore durante la decodifica di un indirizzo tramite GPS con query {0} ed exception {1}", address, ex.Message));
            }

            return location;
        }

        /// <summary>
        /// Imposta l'indirizzo su un cantiere specificato a partire da un punto GPS specifico.
        /// </summary>
        /// <param name="cantToEdit">Il cantiere su cui impostare l'indirizzo contenente la latitudine e la longitudine da processare.</param>
        public static void SetCantAddressFromGpsPoint(Cant cantToEdit)
        {
            // si procede all'elaborazione solamente se il cantiere, la sua latitudine e la sua longitudine sono valorizzati
            // ed è attivo il modulo GPS
            if (cantToEdit != null && RepoManager.ParamRepo.ParametersRow.Abilita_GPS)
            {
                if (cantToEdit.LatitudineGps_Can != 0d && cantToEdit.LongitudineGps_Can != 0d)
                {
                    // generazione dell'oggetto per effettuare la richiesta a Bing
                    ReverseGeocodeRequest geoRequest = new ReverseGeocodeRequest();

                    // impostazioni delle credenzioni di accesso con la chiave Bing salvata nei parametri
                    geoRequest.BingMapsKey = RepoManager.ParamRepo.ParametersRow.BingKey;

                    // impostazione del punto gps di cui ricercare l'indirizzo
                    geoRequest.Point = new Coordinate();

                    geoRequest.Point.Latitude = cantToEdit.LatitudineGps_Can;
                    geoRequest.Point.Longitude = cantToEdit.LongitudineGps_Can;

                    try
                    {
                        var geoResponse = Task.Run(() => ServiceManager.GetResponseAsync(geoRequest)).Result;

                        var geo = new ReverseGeocodeService("a442f14174a945dd9aff62023727b914");
                        var indirizzo = Task.Run(() => geo.OttieniIndirizzoAsync(cantToEdit.LatitudineGps_Can, cantToEdit.LongitudineGps_Can)).Result;

                        // se sono state ottenute delle risposte da bing
                        if (indirizzo != null)
                        {
                            // si recuperano i risultati e si impostano i dati del cantiere
                            cantToEdit.Indirizzo_Can = indirizzo.Via;
                            cantToEdit.Cap_Can = indirizzo.CAP;
                            cantToEdit.Luogo_Can = indirizzo.Citta;

                            //string Provincia_Can = RepoManager.Tab_ProvRepo.First(p => p.Descrizione_Prov == address.AdminDistrict2).Sigla_Prov;

                            string Provincia_Can = indirizzo.Provincia;

                            //string provinciaCan = address.FormattedAddress.Substring(address.FormattedAddress.Length - 2);
                            //
                            //a causa della ma interpretazione da parte di BING delle provincie, per far si che quest'ultime vengano trovate nella tabella delle province 
                            //si ha la necessità di effetturare una conversione da cosa restituisce BING a quello presente in tabella province
                            switch (Provincia_Can)
                            {
                                case "Florence":
                                    Provincia_Can = "Firenze";
                                    break;
                                case "Genoa":
                                    Provincia_Can = "Genova";
                                    break;
                                case "Mantua":
                                    Provincia_Can = "Mantova";
                                    break;
                                case "Milan":
                                    Provincia_Can = "Milano";
                                    break;
                                case "Naples":
                                    Provincia_Can = "Napoli";
                                    break;
                                case "Padua":
                                    Provincia_Can = "Padova";
                                    break;
                                case "Rome":
                                    Provincia_Can = "Roma";
                                    break;
                                case "Syracuse":
                                    Provincia_Can = "Siracusa";
                                    break;
                                case "Turin":
                                    Provincia_Can = "Torino";
                                    break;
                                case "Venice":
                                    Provincia_Can = "Venezia";
                                    break;
                                default:
                                    Provincia_Can = Provincia_Can;
                                    break;
                            }

                            cantToEdit.Provincia_Can = RepoManager.Tab_ProvRepo.First(p => p.Descrizione_Prov == Provincia_Can).Sigla_Prov;

                            //se la provincia non viene trovata nella corrispondente tabella viene marcata come non disponibile
                            if (CantRepository.Tab_Provs.SingleOrDefault(u => u.Sigla_Prov == cantToEdit.Provincia_Can) == null)
                            {
                                cantToEdit.Provincia_Can = "ND";
                            }


                        }
                        else
                        {
                            _log.Warn(String.Format("Errore durante il calcolo delle coordinate per il cantiere {0}", cantToEdit.Codice_Cantiere));
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(String.Format("Errore durante il calcolo dell'indirizzo tramite GPS Bing per il cantiere {0} con coordinate {1},{2} con exception {3}", cantToEdit.Codice_Cantiere, cantToEdit.LatitudineGps_Can, cantToEdit.LongitudineGps_Can, ex.Message));
                    }
                }
            }
        }

        /// <summary>
        /// Recupera l'id cantiere più vicino rispetto a quelli nel cui raggio cade il punto specifico.
        /// </summary>
        /// <param name="pointLatitude">La latitudine del punto specifico da controllare.</param>
        /// <param name="pointLongitude">La longitudine del punto specifico da controllare.</param>
        /// <returns>L'id cantiere più vicino rispetto a quelli nel cui raggio cade il punto specifico; in caso non sia trovato nessun cantiere, 0</returns>
        public static int GetClosestCantIdInRange(double pointLatitude, double pointLongitude)
        {
            // inizializzazione dell'id del cantiere (default non trovato)
            int closestCantId = 0;

            // recupero di tutti i cantieri nel cui range cade il punto passato come parametro
            IEnumerable<Cant> rangeCants = GetAllGpsCantsForPoint(pointLatitude, pointLongitude).ToList();

            // se sono stati trovati dei cantieri in range allora si recupera quello più vicino
            if (rangeCants.Any())
            {
                var noGPSCants = rangeCants.Where(nomeGps => !nomeGps.Codice_Cantiere.StartsWith("GPS")).ToList();
                if (noGPSCants.Any())
                {
                    closestCantId = noGPSCants.OrderBy(cant => GetDeviationIndex(pointLatitude, pointLongitude, cant.LatitudineGps_Can, cant.LongitudineGps_Can)).First().Cant_Id;
                }
                else
                {
                    var GPSCants = rangeCants.Where(nomeGps => nomeGps.Codice_Cantiere.StartsWith("GPS")).ToList();
                    if (GPSCants.Any())
                    {
                        closestCantId = GPSCants.OrderBy(cant => GetDeviationIndex(pointLatitude, pointLongitude, cant.LatitudineGps_Can, cant.LongitudineGps_Can)).First().Cant_Id;
                    }

                }
            }

            // ritorno del valore calcolato dal metodo
            return closestCantId;
        }

        /// <summary>
        /// Recupera e restituisce tutti i cantieri non disabilitati con coordinate GPS nel cui raggio cade il punto specifico.
        /// </summary>
        /// <param name="pointLatitude">La latitudine del punto da ricercare.</param>
        /// <param name="pointLongitude">La longitudine del punto da ricercare.</param>
        /// <returns>L'elenco dei cantiere non disabilitati con coordinate GPS nel cui raggio cade il punto specifico.</returns>
        public static IEnumerable<Cant> GetAllGpsCantsForPoint(double pointLatitude, double pointLongitude)
        {
            // TODO: migliorare controllo double a 0
            // inizializzazione dell'elenco dei cantieri ritorno del metodo
            IEnumerable<Cant> rangeCants = Enumerable.Empty<Cant>();

            // calcolo del raggio gps di default (parametri)
            double defaultGpsRange = RepoManager.ParamRepo.ParametersRow.RaggioGpsDefault ?? 0;

            // recupero tutti i cantieri non disabilitati con coordinate GPS
            // TODO: migliorare verifica dello 0
            IEnumerable<Cant> currentGpsCants = RepoManager.CantRepo.Find(cant => cant.LatitudineGps_Can != 0d && cant.LongitudineGps_Can != 0d && !cant.DisAbilitazione_Can);

            // se sono stati trovati dei cantieri processabili
            if (currentGpsCants.Any())
            {
                // dai cantieri così recuperati estraggo tutti quelli nel cui raggio cade il punto specificato
                rangeCants = currentGpsCants.Where(cant =>
                {
                    bool isCantInRange = false;

                    // calcolo del raggio gps del cantiere
                    double cantGpsRange = cant.RaggioGps_Can ?? 0d;

                    // calcolo del raggio gps corrente
                    double currentGpsRange = cantGpsRange != 0d ? cantGpsRange : defaultGpsRange;

                    // si procede a verificare il dato solamente se è espresso di default o sul cantiere il raggio gps
                    if (currentGpsRange != 0d)
                    {
                        // generazione del range Gps
                        var currentRange = new GpsRange(cant.LatitudineGps_Can, cant.LongitudineGps_Can, Convert.ToInt32(currentGpsRange));
                        isCantInRange = currentRange.IsPointInRange(pointLatitude, pointLongitude);
                    }

                    return isCantInRange;

                });
            }
            // ritorno del valore calcolato dal metodo
            return rangeCants;
        }

        /// <summary>
        /// Converte la latitudine proveniente da un dispositivo Winit alla versione utilizzata da Bing.
        /// </summary>
        /// <param name="deviceLatitude">La stringa che rappresenta la latitudine proveniente dal dispositivo Winit.</param>
        /// <param name="coordinatesDirection">La direzione delle coordinate da processare (NORD/SUD).</param>
        /// <returns>La latitudine usate da Bing espressione della stringa inviata dal dispositivo Winit</returns>
        public static double ConvertDeviceToBingLatitude(string deviceLatitude, GpsLineCoordinatesDirectionEnum coordinatesDirection)
        {
            // recupero del numero corrispondente alle coordinate passate come parametro
            double coordinatesValue = ConvertStringToCoordinateDouble(deviceLatitude);

            // se il valore è nell'emisfero sud allora le coordinate diventano negative
            if (coordinatesDirection == GpsLineCoordinatesDirectionEnum.South)
                coordinatesValue = -Math.Abs(coordinatesValue);

            return coordinatesValue;
        }

        /// <summary>
        /// Converte la longitudine proveniente da un dispositivo Winit alla versione utilizzata da Bing.
        /// </summary>
        /// <param name="deviceLongitude">La stringa che rappresenta la longitudine proveniente dal dispositivo Winit.</param>
        /// <param name="coordinatesDirection">La direzione delle coordinate da processare (EST/OVEST).</param>
        /// <returns>La longitudine usate da Bing espressione della stringa inviata dal dispositivo Winit</returns>
        public static double ConvertDeviceToBingLongitude(string deviceLongitude, GpsLineCoordinatesDirectionEnum coordinatesDirection)
        {
            // recupero del numero corrispondente alle coordinate passate come parametro
            double coordinatesValue = ConvertStringToCoordinateDouble(deviceLongitude);

            // se il valore è ad ovest di greenwich allora le coordinate diventano negative
            if (coordinatesDirection == GpsLineCoordinatesDirectionEnum.West)
                coordinatesValue = -Math.Abs(coordinatesValue);

            return coordinatesValue;
        }

        /// <summary>
        /// Converte la stringa rappresentante una coordinata così come espressa da una device Winit nel suo corrispondete double in gradi.
        /// </summary>
        /// <param name="coordinateString">La stringa coordinata da convertire.</param>
        /// <returns>La coordinata convertita in formato double</returns>
        private static double ConvertStringToCoordinateDouble(string coordinateString)
        {
            // la stringa coordinata ha il seguente formato:
            // 0460693120
            // e rappresenta il corrispondente numero nel seguente formato:
            // 46.0693120
            // quindi la stringa va convertita in numero e divista per 10000000

            return Convert.ToDouble(coordinateString) / 10000000;
        }

        /// <summary>
        /// Recupera l'indice di deviazione (scarto quadratico medio) tra le coordinate centrali e periferiche specificate.
        /// </summary>
        /// <param name="centerLatitude">La latitudine centrale da cui calcolare l'indice di deviazione.</param>
        /// <param name="centerLongitude">La longitudine centrale su cui calcolare l'indice di deviazione.</param>
        /// <param name="peripheralLatitude">La latitudine periferica su cui calcolare l'indice di deviazione.</param>
        /// <param name="peripheralLongitude">La longitudine periferica su cui calcolare l'indice di deviazione.</param>
        /// <returns>L'indice di deviazione (scarto quadratico medio) tra le coordinate centrali e periferiche specificate.</returns>
        public static double GetDeviationIndex(double centerLatitude, double centerLongitude, double peripheralLatitude, double peripheralLongitude)
        {
            // calcolo del delta della latitudine
            double latitudeDelta = Math.Abs(Math.Abs(centerLatitude) - Math.Abs(peripheralLatitude));

            // calcolo del delta della longitudine
            double longitudeDelta = Math.Abs(Math.Abs(centerLongitude) - Math.Abs(peripheralLongitude));

            // lo scarto è la radice quadrata della somma dei quadrati dei delta
            return Math.Sqrt(Math.Pow(latitudeDelta, 2) + Math.Pow(longitudeDelta, 2));

        }

        #endregion

        #region Gestione Scadenze Applicazione

        /// <summary>
        /// Vengono recuperate le date di scadenza dal servizio che gestisce le licenze
        /// </summary>
        private static void UpdateExpirationDate()
        //recupero data Scadenza da Servizio Licenze + Verifica Correttezza Data Ultimo Collegamento
        {

            //vengono letti i parametri
            Param paramsRow = RepoManager.ParamRepo.ParametersRow;
            //viene creata una nuova istanza del servizio che si occupa di recuperare le licenze
            LicenceServiceClient sc = new LicenceServiceClient();

            //viene caricato il file che rappresenta l'ultima data di connessione decriptata
            FileInfo lcFileInfo = new FileInfo(HttpContext.Current.Server.MapPath(Settings.Default.LastConn));

            //se il file esiste
            if (lcFileInfo.Exists)
            {
                long storedTicks = 0;

                using (StreamReader sr = new StreamReader(lcFileInfo.OpenRead()))

                    //viene estratta la data di ultimo collegamento
                    storedTicks = Convert.ToInt64(MD5DecryptString(sr.ReadToEnd(), Settings.Default.Password));

                //se è valorizzata la data di ultimo collegamento
                if (storedTicks > 0)
                {
                    //la data di ultimo collegamento
                    DateTime storedLastConnection = new DateTime(storedTicks);

                    //Richiama il Servizio di gestione Licenze passandogli la Data Ora Ultimo collegamento
                    //Riceve la data Scadenza 01/01/1900 se Data Ultimo Collegamento < Data Ultimo Collegamento registrato nel Servizio
                    //Riceve la Data Scadenza registrata nel Servizio se Data ultimo Collegamento >= Data Ultimo Collegamento registrato nel Servizio
                    var licence = sc.GetActivation(paramsRow.PublicKey, paramsRow.CompanyName, storedLastConnection);

                    if (!String.IsNullOrEmpty(licence))
                    {
                        //memorizza
                        paramsRow.ActivationDate = licence;
                        RepoManager.ParamRepo.SaveChanges();

                        // imposta i dati di upadate per il cliente corrente
                        SendUpdateParams();
                    }
                    else
                    {
                        //In this case you can consider to delete the current activation key
                    }
                }
            }
        }

        /// <summary>
        /// Metodo che si occupa di inviare al servizio di gestione della licenza i dati per effettuare l'update.
        /// </summary>
        private static void SendUpdateParams()
        {
            // inizializzazione del web service
            var sc = new LicenceServiceClient();

            // calcolo del percorso fisico del sito corrente
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string sitePhysicalPath = Path.GetDirectoryName(Path.GetDirectoryName(path)); // il path del sito è quello padre della dll contenuta nella cartella bin

            // calcolo della stringa di connessione
            string connString = RepoManager.ParamRepo.Context.Database.Connection.ConnectionString;

            // calcolo dell'url di base dell'applicativo web
            string applicationUrl = HttpContext.Current.Request.Url.AbsoluteUri.Substring(0, HttpContext.Current.Request.Url.AbsoluteUri.LastIndexOf('/'));

            // invio dei dati di update al servizio
            sc.SetUpdateData(RepoManager.ParamRepo.ParametersRow.PublicKey, sitePhysicalPath, connString, applicationUrl);
        }

        public static bool CheckFirstTimeInitializeLicence()
        //Gestisce il 1° Collegamento del Giorno evitando di chiamare il Servizio LIcenze per i Collegamenti Successivi dello Stesso Giorno
        {
            bool isFirstTimeInitialized = false;

            var pkFileInfo = new FileInfo(HttpContext.Current.Server.MapPath(Settings.Default.PKPath));
            //la public key della lastconnection
            var lcFileInfo = new FileInfo(HttpContext.Current.Server.MapPath(Settings.Default.LastConn));

            if (lcFileInfo.Exists)
            {
                try
                {
                    //rappresenta la data di ultima connessione
                    long storedTicks = 0;
                    //rappresenta la data corrente di connessione
                    long currentTicks = 0;

                    //si va a leggere il file della lastconnecion
                    using (StreamReader sr = new StreamReader(lcFileInfo.OpenRead()))
                    {
                        //vengono letti e convertiti tutto lo stream corrispondente alla data di ultima connessione
                        storedTicks = Convert.ToInt64(MD5DecryptString(sr.ReadToEnd(), Settings.Default.Password));
                        currentTicks = DateTime.UtcNow.Ticks;
                    }

                    //se la data corrente  è minore della data di ultima connessione viene generata un'eccezione
                    if (currentTicks <= storedTicks)
                        throw new Exception();
                    else
                    {
                        DateTime storedDT = new DateTime(storedTicks).Date;
                        DateTime currentDT = new DateTime(currentTicks).Date;

                        // si procede al controllo della licenza solo se non si è nello stesso giorno
                        // e se la data di scadenza attuale risulta scaduta
                        DateTime expiryDate = GetExpirationDate(RepoManager.ParamRepo.ParametersRow);

                       
                        //viene controllato che non sia scaduto o l'ultima data di connesione sia coerente
                        if (PowerWebContext.Current.User.Codice_Utente == "WINIT" || currentDT > storedDT || expiryDate.Date <= DateTime.UtcNow.Date)
                        {
                            //se il file con l'ultima data di connessione criptata esiste
                            if (lcFileInfo.Exists)
                            {
                                //viene cancellato
                                lcFileInfo.Delete();
                                //viene ricreato un nuovo file con la nuova data di ultima connessione
                                lcFileInfo = new FileInfo(HttpContext.Current.Server.MapPath(Settings.Default.LastConn));
                            }

                            using (StreamWriter sw = new StreamWriter(lcFileInfo.Create()))
                            {
                                sw.Write(MD5EncryptString(Convert.ToString(DateTime.UtcNow.Ticks), Settings.Default.Password));
                                sw.Flush();
                                sw.Close();
                            }

                            try
                            {
                                UpdateExpirationDate();
                            }
                            catch (Exception)
                            {
                                //No service available... Store it in the log!
                            }

                        }
                    }


                }
                catch (Exception)
                {
                    if (PowerWebContext.Current.User != null)
                        PowerWebContext.LogOut();
                }
            }

            if (!lcFileInfo.Exists)
            {
                using (StreamWriter sw = new StreamWriter(lcFileInfo.Create()))
                {
                    sw.Write(MD5EncryptString(Convert.ToString(DateTime.UtcNow.Ticks), Settings.Default.Password));
                    sw.Flush();
                    sw.Close();

                }
            }


            if (!pkFileInfo.Exists)
            {
                isFirstTimeInitialized = true;

                Param paramsRow = RepoManager.ParamRepo.ParametersRow;

                RSACryptoServiceProvider RSACSP = new RSACryptoServiceProvider();

                using (StreamWriter sw = new StreamWriter(pkFileInfo.Create()))
                {
                    sw.Write(MD5EncryptString(RSACSP.ToXmlString(true), Settings.Default.Password));
                    sw.Flush();
                    sw.Close();
                }

                DateTime expDate = DateTime.UtcNow.Date.AddDays(Settings.Default.GiorniDemo);

                byte[] expDateBytes = BitConverter.GetBytes(expDate.Ticks);
                byte[] connDateBytes = BitConverter.GetBytes(DateTime.UtcNow.Date.Ticks);

                paramsRow.PublicKey = RSACSP.ToXmlString(false);
                paramsRow.ActivationDate = Convert.ToBase64String(RSACSP.Encrypt(expDateBytes, false));

                RepoManager.ParamRepo.SaveChanges();

                try
                {
                    UpdateExpirationDate();
                }
                catch (Exception ex)
                {

                    System.Diagnostics.Debug.WriteLine(ex);
                    //No service available... Store it in the log!
                }
            }

            if (IsExpired())
            {
                UpdateExpirationDate();

                if (IsExpired() && PowerWebContext.Current.User != null)
                    PowerWebContext.LogOut();
            }

            return isFirstTimeInitialized;
        }

        public static void CheckFirstTimeIntializeWinitPasswordUpdate()
        {

        }
        public static bool IsExpired()
        //restituisce True se Scaduta la Password
        {
            Param paramsRow = RepoManager.ParamRepo.ParametersRow;

            bool expired = true;

            DateTime expirationDate = GetExpirationDate(paramsRow);

            if (expirationDate.Date >= DateTime.UtcNow.Date)
                expired = false;

            return expired;
        }

        /// <summary>
        /// Viene decriptata la data di scadenza
        /// </summary>
        /// <param name="paramsRow">Record dei parametri presenti nel database.</param>
        /// <returns></returns>
        public static DateTime GetExpirationDate(Domain.Param paramsRow)
        //Decripta la data Scadenza
        {
            _log.InfoFormat("Inizio recupero data scadenza");
            long expTicks = 0;

            //viene inizializzata la data di scadenza
            DateTime expiryDate = DateTime.MinValue;

            //viene crato un provider di decriptazione usando la chiave pubblica contenuta in menu.js
            using (RSACryptoServiceProvider RSACSP = BusinessService.GetRSACryptoServiceProvider())
            {
                if (RSACSP != null)
                {
                    _log.InfoFormat("Inizio a decodificare la data");
                    //si va decriptare usando la chiave pubblica la data di attivazione
                    byte[] descrActivation = RSACSP.Decrypt(Convert.FromBase64String(paramsRow.ActivationDate), false);

                    expTicks = BitConverter.ToInt64(descrActivation, 0);

                    if (expTicks > 0)
                        //viene convertita in data la data di scadenza
                        expiryDate = new DateTime(expTicks);
                }
            }

            return expiryDate;
        }

        #endregion

        #region Gestione Attivazione/Disattivazione Moduli Applicazione

        /// <summary>
        /// Gestisce l'attivazione e la disattivazione dei moduli caricandoli dal servizio al 1° collegamento giornaliero e dal file di configurazione ai successivi.
        /// </summary>
        public static void CheckFirstTimeIntializeModulesActivation()
        {
            // TODO: se possibile passare da FileInfo a System.IO.FIle
            // inizializzazione della lista dei moduli da attivare
            var modulesActivation = new List<ModulesActivation>();

            // recupero il percorso del file di configurazione dei moduli da attivare
            var mdActivationFileInfo = new FileInfo(GetXmlModulesFilePath());

            // se il file non esite allora si procede a richiedere al servizio delle licenze le informazioni necessarie alla sua compilazione
            // allora recupero tutti moduli attivati dal servizio delle licenze;
            // se invece il file esiste allora lo si aggiorna richiedendo al servizio di licenza i dati solo se il file ha come data di ultima modifica una data diversa da quella odierna; 
            // in caso contrario si leggono i dati direttamente dal file
            if (!mdActivationFileInfo.Exists)
                modulesActivation = GetModulesActivationFromService();
            else
                modulesActivation = mdActivationFileInfo.LastWriteTime.Date != DateTime.Today ? GetModulesActivationFromService() : ReadModulesActivationFromFile();

            // se sono stati trovati dei moduli applicativi da trattare
            if (modulesActivation.Any())
                ManageModulesActivationDeactivation(modulesActivation);
        }

        /// <summary>
        /// Gestisce l'attivazione e la disattivazione nella scheda parametri dei moduli passati come parametro.
        /// </summary>
        /// <param name="modulesActivation">L'elenco delle attivazioni/disattivazioni da processare.</param>
        private static void ManageModulesActivationDeactivation(IEnumerable<ModulesActivation> modulesActivation)
        {

            // inizializzazione dell'oggetto di logging
            ILog oLog = LogManager.GetLogger("Business.CommonServiceBiz");

            // inizializzazione della variabile che indica se è
            // stata effettuata una modifica ai parametri
            bool isEdited = false;

            //Inizializza un dictionary per tener conto degli EditFormTemplate da aggiungere
            //Struttura:
            //                              Dictionary
            //                                 / \
            //                              /       \
            //                          /              \
            //          Entity(Col/Cant)                Dictionary
            //                                             / \
            //                                          /       \
            //                                       /             \                
            //                       Entity(Tabs/Fields)         List(EditFormTemplate)
            //                    
            Dictionary<String, Dictionary<String, List<String>>> editFormTemplates = new Dictionary<String, Dictionary<String, List<String>>>();
            Dictionary<String, List<String>> eftCant = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> eftCol = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> eftParam = new Dictionary<String, List<String>>();

            eftCant.Add(tabString, new List<string>());
            eftCant.Add(fieldString, new List<string>());
            editFormTemplates.Add(cantString, eftCant);

            eftCol.Add(tabString, new List<string>());
            eftCol.Add(fieldString, new List<string>());
            editFormTemplates.Add(colString, eftCol);

            eftParam.Add(tabString, new List<string>());
            eftParam.Add(fieldString, new List<string>());
            editFormTemplates.Add(paramString, eftParam);

            // per ogni attivazione/disattivazione moduli passata come parametro
            foreach (var moduleActivation in modulesActivation)
            {
                // in base al tipo di modulo passato come parametro
                switch (moduleActivation.ModuleType)
                {
                    case ModulesTypeEnum.Times: // gestione degli orari

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            editFormTemplates[colString][fieldString].Add("Tab_Orari_Tipo_Id");
                            editFormTemplates[paramString][fieldString].Add("Limite_Entrata_Usa_Orario");
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Orari)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Orari = moduleActivation.IsActive;

                            isEdited = true;
                        }

                        break;
                    case ModulesTypeEnum.Timesheet: // gestione del cartellino

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            //Eventuali EditFormTemplate del cartellino
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Cartellino)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Cartellino = moduleActivation.IsActive;

                            isEdited = true;
                        }

                        break;
                    case ModulesTypeEnum.AmmountMinutes: // gestione monte minuti

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            editFormTemplates[colString][fieldString].Add("Flag_Monte_Ore");
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Monte_Minuti)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Monte_Minuti = moduleActivation.IsActive;

                            isEdited = true;
                        }

                        break;
                    case ModulesTypeEnum.Rounding: // gestione arrotondamenti

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            editFormTemplates[colString][tabString].Add("Arrotondamenti");
                            editFormTemplates[colString][fieldString].Add("Minuti_Arrot_Durata_Fig_Col");
                            editFormTemplates[colString][fieldString].Add("Soglia_Arrot_Durata_Fig_Col");

                            editFormTemplates[cantString][tabString].Add("Arrotondamenti");

                            editFormTemplates[paramString][tabString].Add("Arrotondamenti");
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Arrotondamenti)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Arrotondamenti = moduleActivation.IsActive;
                            isEdited = true;
                        }

                        break;
                    case ModulesTypeEnum.Nocturne: // gestione notturno

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            editFormTemplates[colString][fieldString].Add("TipoNotturno_Col");
                            editFormTemplates[colString][fieldString].Add("Durata_Max_Gruppo_Notte_Ril_Col");
                            editFormTemplates[colString][fieldString].Add("Limite_Inizio_Notte_Col");

                            editFormTemplates[cantString][fieldString].Add("Limite_Inizio_Notte_Can");
                            editFormTemplates[cantString][fieldString].Add("TipoNotturno_Can");
                            editFormTemplates[cantString][fieldString].Add("Durata_Max_Gruppo_Notte_Ril_Can");

                            editFormTemplates[paramString][fieldString].Add("TipoNotturno");
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Notturno)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Notturno = moduleActivation.IsActive;
                            isEdited = true;
                        }

                        break;
                    case ModulesTypeEnum.Trips: // gestione viaggi

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            editFormTemplates[colString][tabString].Add("Viaggi e Pause");

                            editFormTemplates[cantString][fieldString].Add("Tipo_Calcolo_Viaggi_Can");

                            editFormTemplates[paramString][tabString].Add("Viaggi e Pause");
                            editFormTemplates[paramString][fieldString].Add("Dflt_Include_Trips");
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Viaggi)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Viaggi = moduleActivation.IsActive;
                            isEdited = true;
                        }

                        break;
                    case ModulesTypeEnum.Activities: // gestione attività

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            editFormTemplates[cantString][fieldString].Add("Tempo_Attivita_Can");

                            editFormTemplates[paramString][fieldString].Add("Tipo_Chiusura_Causali");
                            editFormTemplates[paramString][fieldString].Add("Dflt_Include_Activities");
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Att)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Att = moduleActivation.IsActive;
                            isEdited = true;
                        }

                        break;
                    case ModulesTypeEnum.Scheduler: // gestione schedulatore

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            //Eventuali EditFormTemplate dello schedulatore
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Schedulatore)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Schedulatore = moduleActivation.IsActive;
                            isEdited = true;
                        }

                        break;
                    case ModulesTypeEnum.Gps: // gestione GPS

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            editFormTemplates[cantString][fieldString].Add("FlagGps_Can");
                            editFormTemplates[cantString][fieldString].Add("DataVarGps_Can");
                            editFormTemplates[cantString][fieldString].Add("RaggioGps_Can");
                            editFormTemplates[cantString][fieldString].Add("LongitudineGps_Can");
                            editFormTemplates[cantString][fieldString].Add("LatitudineGps_Can");

                            editFormTemplates[paramString][fieldString].Add("Importazione_timbrature_GPS");
                            editFormTemplates[paramString][fieldString].Add("Tipo_Ass_Tag_Gps");
                            editFormTemplates[paramString][fieldString].Add("Flag_GPS");
                            editFormTemplates[paramString][fieldString].Add("RaggioGpsDefault");
                        }


                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_GPS)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_GPS = moduleActivation.IsActive;

                            isEdited = true;
                        }

                        break;
                    case ModulesTypeEnum.Pass: // gestione passaggi

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            editFormTemplates[cantString][fieldString].Add("Singola_Reg");
                            editFormTemplates[colString][fieldString].Add("Singola_Reg");
                            editFormTemplates[paramString][fieldString].Add("Dflt_Include_Pass");
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Pass)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Pass = moduleActivation.IsActive;
                            isEdited = true;
                        }

                        break;
                    case ModulesTypeEnum.BudgetComparing:  // gestione confronto ore/bugdet

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            //Eventuali EditFormTemplate del confronto ore/budget
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Confronto_Ore_Budget)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Confronto_Ore_Budget = moduleActivation.IsActive;
                            isEdited = true;
                        }

                        break;
                    case ModulesTypeEnum.WhereIsIt: // gestione della funzione Where is it

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            //Eventuali EditFormTemplate del Where Is It
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Where_Is_It)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Where_Is_It = moduleActivation.IsActive;
                            isEdited = true;
                        }
                        break;
                    case ModulesTypeEnum.OvertimeAuth: // gestione della funzione di autorizzazione straordinari

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            //Eventuali EditFormTemplate dell'autorizzazione straordinario
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Aut_Str)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Aut_Str = moduleActivation.IsActive;
                            isEdited = true;
                        }
                        break;

                    case ModulesTypeEnum.Notification: //gestione della funzione notifiche

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            //Eventuali EditFormTemplate delle notifiche
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Notifiche)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Notifiche = moduleActivation.IsActive;
                            isEdited = true;
                        }
                        break;

                    case ModulesTypeEnum.Privacy: //gestione della funzione notifiche

                        //Se il modulo NON è attivo, aggiunge al dictionatry gli EditFormTemplate relativi al modulo
                        if (!moduleActivation.IsActive)
                        {
                            //Eventuali EditFormTemplate della privacy
                        }

                        if (moduleActivation.IsActive != RepoManager.ParamRepo.ParametersRow.Abilita_Privacy)
                        {
                            RepoManager.ParamRepo.ParametersRow.Abilita_Privacy = moduleActivation.IsActive;
                            isEdited = true;
                        }
                        break;
                    default:
                        oLog.WarnFormat("Tipo di modulo {0} non supportato", moduleActivation.ModuleType);
                        break;
                }

                // gestione della disabilitazione delle funzioni (Tab_Funz) del modulo
                ManageModulesAuthorizations(moduleActivation);
            }

            //Gestisce gli EditFormTemplate
            //ManageEditFormTemplate(editFormTemplates);

            // al termine delle operazioni, se si è modificato qualcosa allora si salva il contesto
            if (isEdited)
                RepoManager.ParamRepo.SaveChanges();
        }

        /// <summary>
        /// Gestisce gli EditFormTemplate a seconda dei moduli attivati.
        /// </summary>
        private static void ManageEditFormTemplate(Dictionary<String, Dictionary<String, List<String>>> editFormTemplates)
        {
            //Liste per i tab e i field delle anagrafiche
            List<String> cantFields = new List<String>();
            List<String> cantTabs = new List<String>();
            List<String> colFields = new List<String>();
            List<String> colTabs = new List<String>();
            List<String> paramFields = new List<String>();
            List<String> paramTabs = new List<String>();

            //Liste di EditFormTemplate esistenti
            List<Tab_EditFormTemplate> cantEFT = RepoManager.Tab_EditFormTemplateRepo.Find(eft => eft.Entity_Tab_EditFormTemplate == cantString).ToList();
            List<Tab_EditFormTemplate> colEFT = RepoManager.Tab_EditFormTemplateRepo.Find(eft => eft.Entity_Tab_EditFormTemplate == colString).ToList();
            List<Tab_EditFormTemplate> paramEFT = RepoManager.Tab_EditFormTemplateRepo.Find(eft => eft.Entity_Tab_EditFormTemplate == paramString).ToList();

            //Liste di EditFormTemplate 'aggiuntivi' (non contemplati dall'attivazione dei moduli)
            List<Tab_EditFormTemplate> aggCantEFT = RepoManager.Tab_EditFormTemplateRepo.Find(eft => eft.Entity_Tab_EditFormTemplate == "CantAgg").ToList();
            List<Tab_EditFormTemplate> aggColEFT = RepoManager.Tab_EditFormTemplateRepo.Find(eft => eft.Entity_Tab_EditFormTemplate == "ColAgg").ToList();
            List<Tab_EditFormTemplate> aggParamEFT = RepoManager.Tab_EditFormTemplateRepo.Find(eft => eft.Entity_Tab_EditFormTemplate == "ParamAgg").ToList();

            //Elimina dal db gli EditFormTemplate pre-esistenti
            RepoManager.Tab_EditFormTemplateRepo.Delete(colEFT);
            RepoManager.Tab_EditFormTemplateRepo.Delete(cantEFT);
            RepoManager.Tab_EditFormTemplateRepo.Delete(paramEFT);

            /* GESTIOE EFT CANTIERE */
            //Se sono presenti EditFormTemplate 'aggiuntivi', li aggiunge
            if (!aggCantEFT.IsEmpty())
            {
                foreach (var eft in aggCantEFT)
                {
                    if (eft.Fields_Tab_EditFormTemplate != null && eft.Fields_Tab_EditFormTemplate != String.Empty)
                    {
                        cantFields.AddRange(eft.Fields_Tab_EditFormTemplate.Split('|'));
                    }

                    if (eft.Tabs_Tab_EditFormTemplate != null && eft.Tabs_Tab_EditFormTemplate != String.Empty)
                    {
                        cantTabs.AddRange(eft.Tabs_Tab_EditFormTemplate.Split('|'));
                    }
                }
            }

            //Aggiunge i EditFormTemplate-field per l'anagrafica cantiere
            foreach (string field in editFormTemplates[cantString][fieldString])
            {
                //Aggiunge il nuovo EditFormTemplate-field solo se non c'era già
                if (!cantFields.Contains(field))
                {
                    cantFields.Add(field);
                }
            }

            //Aggiunge i nuovi EditFormTemplate-tab per l'anagrafica cantiere
            foreach (string tab in editFormTemplates[cantString][tabString])
            {
                //Aggiunge il nuovo EditFormTemplate-tab solo se non c'era già
                if (!cantTabs.Contains(tab))
                {
                    cantTabs.Add(tab);
                }
            }

            //Inizializza un nuovo EditFormTemplate
            Tab_EditFormTemplate newCantEFT = RepoManager.Tab_EditFormTemplateRepo.Init();

            //Compone i campi da salvare a db
            newCantEFT.Entity_Tab_EditFormTemplate = cantString;
            newCantEFT.Tabs_Tab_EditFormTemplate = String.Join("|", cantTabs);
            newCantEFT.Fields_Tab_EditFormTemplate = String.Join("|", cantFields);


            /* GESTIOE EFT COLLABORATORE */
            //Se sono presenti EditFormTemplate 'aggiuntivi', li aggiunge
            if (!aggColEFT.IsEmpty())
            {
                foreach (var eft in aggColEFT)
                {
                    if (eft.Fields_Tab_EditFormTemplate != null && eft.Fields_Tab_EditFormTemplate != String.Empty)
                    {
                        colFields.AddRange(eft.Fields_Tab_EditFormTemplate.Split('|'));
                    }

                    if (eft.Tabs_Tab_EditFormTemplate != null && eft.Tabs_Tab_EditFormTemplate != String.Empty)
                    {
                        colTabs.AddRange(eft.Tabs_Tab_EditFormTemplate.Split('|'));
                    }
                }
            }

            //Aggiunge i nuovi EditFormTemplate-field per l'anagrafica collaboratore
            foreach (string field in editFormTemplates[colString][fieldString])
            {
                //Aggiunge il nuovo EditFormTemplate-field solo se non c'era già
                if (!colFields.Contains(field))
                {
                    colFields.Add(field);
                }
            }

            //Aggiunge i nuovi EditFormTemplate-tab per l'anagrafica collaboratore
            foreach (string tab in editFormTemplates[colString][tabString])
            {
                //Aggiunge il nuovo EditFormTemplate-tab solo se non c'era già
                if (!colTabs.Contains(tab))
                {
                    colTabs.Add(tab);
                }
            }

            //Inizializza un nuovo EditFormTemplate
            Tab_EditFormTemplate newColEFT = RepoManager.Tab_EditFormTemplateRepo.Init();

            //Compone i campi da salvare a db
            newColEFT.Entity_Tab_EditFormTemplate = colString;
            newColEFT.Tabs_Tab_EditFormTemplate = String.Join("|", colTabs);
            newColEFT.Fields_Tab_EditFormTemplate = String.Join("|", colFields);


            /* GESTIOE EFT PARAMETRI */
            //Se sono presenti EditFormTemplate 'aggiuntivi', li aggiunge
            if (!aggParamEFT.IsEmpty())
            {
                foreach (var eft in aggParamEFT)
                {
                    if (eft.Fields_Tab_EditFormTemplate != null && eft.Fields_Tab_EditFormTemplate != String.Empty)
                    {
                        paramFields.AddRange(eft.Fields_Tab_EditFormTemplate.Split('|'));
                    }

                    if (eft.Tabs_Tab_EditFormTemplate != null && eft.Tabs_Tab_EditFormTemplate != String.Empty)
                    {
                        paramTabs.AddRange(eft.Tabs_Tab_EditFormTemplate.Split('|'));
                    }
                }
            }
            //Aggiunge i nuovi EditFormTemplate-field per la scheda parametri
            foreach (string field in editFormTemplates[paramString][fieldString])
            {
                //Aggiunge il nuovo EditFormTemplate-field solo se non c'era già
                if (!paramFields.Contains(field))
                {
                    paramFields.Add(field);
                }
            }

            //Aggiunge i nuovi EditFormTemplate-tab per la scheda parametri
            foreach (string tab in editFormTemplates[paramString][tabString])
            {
                //Aggiunge il nuovo EditFormTemplate-tab solo se non c'era già
                if (!paramTabs.Contains(tab))
                {
                    paramTabs.Add(tab);
                }
            }

            //Inizializza un nuovo EditFormTemplate
            Tab_EditFormTemplate newParamEFT = RepoManager.Tab_EditFormTemplateRepo.Init();

            //Compone i campi da salvare a db
            newParamEFT.Entity_Tab_EditFormTemplate = paramString;
            newParamEFT.Tabs_Tab_EditFormTemplate = String.Join("|", paramTabs);
            newParamEFT.Fields_Tab_EditFormTemplate = String.Join("|", paramFields);

            //Aggiunge gli EditFormTemplate
            RepoManager.Tab_EditFormTemplateRepo.Add(newColEFT);
            RepoManager.Tab_EditFormTemplateRepo.Add(newCantEFT);
            RepoManager.Tab_EditFormTemplateRepo.Add(newParamEFT);

            //Salva le modifiche
            RepoManager.Tab_EditFormTemplateRepo.SaveChanges();
        }

        /// <summary>
        /// Gestisce nella Tab_Aut la attivazione o disattivazione del modulo specificato.
        /// </summary>
        /// <param name="moduleToProcess">Il modulo di cui processare l'attivazione o la disattivazione nella Tab_Aut.</param>
        private static void ManageModulesAuthorizations(ModulesActivation moduleToProcess)
        {
            // recupero tutte le tabelle funzioni collegate al modulo passato come parametro
            IEnumerable<Tab_Funz> moduleFunzs = GetModuleTabFunz(moduleToProcess.ModuleType);

            // si procede alla attivazione/disattivazione delle funzioni solamente se ce ne sono di presenti
            if (moduleFunzs.Any())
            {
                // inizializzazione della variabile che indica se devo salvare delle modifiche al database
                bool isToSaveChanges = false;

                // in base alla disabilitazione o abilitazione allora si mutano i record della tab_aut relativi alla funzione di gestione degli orari
                if (moduleToProcess.IsActive) // se il modulo è stato attivato
                {
                    // per ogni tab funz da processare
                    foreach (var moduleFunz in moduleFunzs)
                    {
                        // si attivano le funzioni solamente se sono presenti e se hanno un livello pari a 11 (disabilitato automaticamente da gestione moduli);
                        // se non esistono autorizzazioni per la funzione allora non serve effettuare alcuna operazione per l'abilitazione (di default le funzioni
                        // senza autorizzazioni sono abilitate)
                        if (moduleFunz != default(Tab_Funz))
                        {
                            IEnumerable<Tab_Aut> funcTabAut = RepoManager.Tab_AutRepo.DbSet.Where(tbaut => tbaut.Tab_Funz_Id == moduleFunz.Tab_Funz_Id);
                            if (funcTabAut.Any())
                            {
                                funcTabAut.ForEach(tbaut =>
                                {
                                    if (tbaut.Funz_Aut == 11)
                                    {
                                        RepoManager.Tab_AutRepo.Delete(tbaut);
                                        isToSaveChanges = true;
                                    }
                                });
                            }
                        }
                    }
                }
                else // se il modulo è stato disattivato
                {
                    // per ogni tab_funz da processare
                    foreach (var moduleFunz in moduleFunzs)
                    {
                        // il modulo disattivato assume per tutte le sue autorizzazioni il valore 11;
                        // in caso non siano presenti autorizzazioni ne viene creata una con utente null e autorizzazione a 11
                        if (moduleFunz != default(Tab_Funz)) // se non esiste la tab_funz non serve disattivare l'autorizzazione
                        {
                            IEnumerable<Tab_Aut> funcTabAut = RepoManager.Tab_AutRepo.DbSet.Where(tbaut => tbaut.Tab_Funz_Id == moduleFunz.Tab_Funz_Id);
                            if (funcTabAut.Any())
                            {
                                funcTabAut.ForEach(tbaut =>
                                {
                                    if (tbaut.Funz_Aut != 11)
                                    {
                                        tbaut.Funz_Aut = 11;
                                        RepoManager.Tab_AutRepo.Update(tbaut);
                                        isToSaveChanges = true;
                                    }
                                });
                            }
                            else
                            {
                                Tab_Aut newTabAut = RepoManager.Tab_AutRepo.Init();

                                newTabAut.DataOraUltimaModifica_Aut = DateTime.Now;
                                newTabAut.Data_Registrazione_Aut = DateTime.Now;
                                newTabAut.Funz_Aut = 11;
                                newTabAut.Tab_Funz_Id = moduleFunz.Tab_Funz_Id;

                                RepoManager.Tab_AutRepo.Add(newTabAut);
                                isToSaveChanges = true;
                            }
                        }
                    }
                }

                // se c'è stata qualche modifica al database allora salvo le modifiche effettuate
                if (isToSaveChanges)
                    RepoManager.Tab_AutRepo.SaveChanges();
            }
        }

        /// <summary>
        /// Restituisce l'elenco delle funzioni in base al tipo modulo passato come parametro.
        /// </summary>
        /// <param name="moduleType">Il tipo di modulo di cui cercare le funzioni.</param>
        /// <returns>L'elenco delle funzioni per il tipo modulo specificato.</returns>
        private static IEnumerable<Tab_Funz> GetModuleTabFunz(ModulesTypeEnum moduleType)
        {
            // inizializzazione del valore di ritorno del metodo
            IEnumerable<Tab_Funz> moduleFunzs = Enumerable.Empty<Tab_Funz>();

            // inizializzazione dell'elenco dei nomi in tab funz
            List<string> tabFunzNames = default(List<string>);

            // in base al tipo di modulo calcolo i nomi funzione
            switch (moduleType)
            {
                case ModulesTypeEnum.Times: // gestione orari
                    tabFunzNames = new List<string>() { "MENU_GROUP_GESTIONE_ORARI" };
                    break;
                case ModulesTypeEnum.Timesheet: // gestione cartellini
                    tabFunzNames = new List<string>() { "MENU_FRMTIME_SHEET_TEXT" };
                    break;
                case ModulesTypeEnum.AmmountMinutes: // gestione monte minuti
                    break;
                case ModulesTypeEnum.Rounding: // gestione arrotondamento
                    break;
                case ModulesTypeEnum.Nocturne: // gestione notturno
                    break;
                case ModulesTypeEnum.Trips: // gestione viaggi
                    break;
                case ModulesTypeEnum.Activities: // gestione attività
                    break;
                case ModulesTypeEnum.Scheduler: // gestione schedulatore
                    tabFunzNames = new List<string>() { "MENU_FRM_SCHEDULES" };
                    break;
                case ModulesTypeEnum.Gps:  // gestione gps
                    tabFunzNames = new List<string>() { "MENU_FRMTRACKING" };
                    break;
                case ModulesTypeEnum.Pass: // gestione passaggi
                    break;
                case ModulesTypeEnum.BudgetComparing:  // gestione confronto ore bugdet
                    break;
                case ModulesTypeEnum.WhereIsIt:
                    tabFunzNames = new List<string>() { "MENU_FRMWHEREISIT" };
                    break;
                case ModulesTypeEnum.OvertimeAuth:
                    tabFunzNames = new List<string>() { "MENU_FRMAUTSTR" };
                    break;
            }

            // se sono stati calcolati dei nomi funzioni
            if (tabFunzNames != default(List<string>))
                moduleFunzs = RepoManager.Tab_FunzRepo.Find(tbfunz => tabFunzNames.Any(tbfunzName => tbfunz.Nome_Tab_Funz == tbfunzName));

            // ritorno del valore calcolato dal metodo
            return moduleFunzs;
        }

        /// <summary>
        /// Meotodo che si occupa di recuperare la lista dei moduli attivati/disattivati dal file di configurazione.
        /// In caso di problemi o di non esistenza del file allora si ritorna una lista vuota
        /// </summary>
        /// <returns>La lista dei moduli attivati/disattivati dal file di configurazione.</returns>
        private static List<ModulesActivation> ReadModulesActivationFromFile()
        {
            // inizializzazione dell'oggetto di logging
            ILog oLog = LogManager.GetLogger("Business.CommonServiceBiz");

            // inizializzazione del valore di ritorno del metodo
            var modulesActivation = new List<ModulesActivation>();

            // si procede all'elaborazione solamente se il file di configurazione esiste
            string mdXmlFilePath = GetXmlModulesFilePath();
            if (File.Exists(mdXmlFilePath))
            {
                // recupero il contenuto criptato del file con l'attivazione/distattivazione dei moduli applicativi
                string xmlCryptedContent = File.ReadAllText(mdXmlFilePath);

                // si procede con l'elaborazione solamente se il file ha del contenuto processabile
                if (!String.IsNullOrEmpty(xmlCryptedContent))
                {
                    try
                    {
                        // si recupera l'xml decriptando la stringa contenuta nel file
                        string xmlContent = MD5DecryptString(xmlCryptedContent, Settings.Default.Password);

                        // inizializzazione del deserializzatore
                        var serializer = new XmlSerializer(typeof(ModulesActivationList));

                        // deserializzazione della stringa con l'xml nell'oggettto con l'elenco delle attivazioni/disattivazioni dei moduli applicativi
                        ModulesActivationList mdActList = default(ModulesActivationList);
                        using (TextReader reader = new StringReader(xmlContent))
                        {
                            mdActList = (ModulesActivationList)serializer.Deserialize(reader);
                        }

                        // se sono stati trovati degli elementi, li si ritorna;
                        // altrimenti si segnala nel log la mancata interpretazione
                        if (mdActList != default(ModulesActivationList))
                        {
                            modulesActivation = mdActList.ModulesActivation;
                        }
                        else
                            oLog.ErrorFormat("Contenuto xml del file {0} non interpretabile!", mdXmlFilePath);
                    }
                    catch (Exception ex)
                    {
                        oLog.ErrorFormat("Eccezione nella decriptazione dei moduli del file {0}: {1} - {2}", mdXmlFilePath, ex.GetType(), ex.Message);
                    }
                }
                else
                    oLog.ErrorFormat("File con dati criptati moduli vuoto [{0}]!", mdXmlFilePath);
            }
            else
                oLog.ErrorFormat("File con dati criptati moduli non presente [{0}]!", mdXmlFilePath);

            // ritorno del valore calcolato dal metodo
            return modulesActivation;
        }

        /// <summary>
        /// Recupera dal servizio di gestione delle licenze l'elenco dei moduli attivati/disattivati per l'applicativo.
        /// Il metodo si occuperà anche di scrivere i dati recuperati dal servizio nell'apposito file di storage di tali informazioni
        /// </summary>
        /// <returns>L'elenco dei moduli con relativa attivazione/distattivazione.</returns>
        private static List<ModulesActivation> GetModulesActivationFromService()
        {
            ILog oLog = LogManager.GetLogger("Business.CommonServiceBiz");

            var modulesActivation = new List<ModulesActivation>();
            try
            {
                var sc = new LicenceServiceClient();
                modulesActivation = sc.GetModulesActivation(RepoManager.ParamRepo.ParametersRow.PublicKey).ToList();
            }
            catch (Exception ex)
            {
                oLog.ErrorFormat("Eccezione nel recupero dei moduli attivati: {0} - {1}", ex.GetType(), ex.Message);
                modulesActivation = new List<ModulesActivation>();
            }

            // se sono stati ritornati dei moduli da processare
            if (modulesActivation.Any())
            {
                // scrivo su file l'elenco dei moduli attivati/distattivati per l'applicativo
                WriteModulesToFile(modulesActivation);
            }
            else // se non sono stati ritornati moduli allora si segnala l'errore sul log
                oLog.ErrorFormat("Impossibile trovare i moduli con relativa attivazione");
            return modulesActivation;
        }

        /// <summary>
        /// Scrive nell'apposito file di configurazione la trasposizione dell'xml dell'elenco dei moduli da attivare/disattivare
        /// passati come parametro. Il file xml è opportunamente criptato per evitare manomissioni manuali
        /// </summary>
        /// <param name="modulesToWrite">L'elenco delle attivazioni/disattivazioni dei moduli applicativi.</param>
        private static void WriteModulesToFile(IEnumerable<ModulesActivation> modulesToWrite)
        {
            // verifico l'esistenza di un attuale file di configurazione delle attivazioni/disattivazioni dei moduli e,
            // se presente, si procede alla sua cancellazione
            string mdXmlFilePath = GetXmlModulesFilePath();
            var mdFileInfo = new FileInfo(mdXmlFilePath);
            if (mdFileInfo.Exists)
                mdFileInfo.Delete();

            // inizializzazione del valore che contiene la stringa con l'xml criptato
            string cryptedXml = String.Empty;
            string decryptedXml = String.Empty;

            // viene trasformato il contenuto dell'elenco dei moduli in xml e lo stesso viene 
            // salvato in memoria; in memoria viene criptato (stesso procedimento chiave di licenza);
            // il contenuto criptato viene salvato sul file xml di destinazione
            using (var mdMemoryStream = new MemoryStream())
            using (TextWriter mdTextWriter = new StreamWriter(mdMemoryStream))
            using (var mdStreamReader = new StreamReader(mdMemoryStream))
            {
                try
                {
                    // inizializzazione dell'elenco delle attivazioni/disattivazioni serializzabili
                    var modulesActivationList = new ModulesActivationList(modulesToWrite);

                    // serializzazione dell'oggetto e sua scrittura in memoria
                    var serializer = new XmlSerializer(typeof(ModulesActivationList));
                    serializer.Serialize(mdTextWriter, modulesActivationList);

                    // il contenuto del file xml viene criptato all'interno di una stringa
                    mdMemoryStream.Position = 0;
                    string xmlContent = mdStreamReader.ReadToEnd();
                    cryptedXml = MD5EncryptString(xmlContent, Settings.Default.Password);
                }
                catch (Exception)
                {
                }
                finally
                {
                    mdTextWriter.Close();
                    mdTextWriter.Dispose();
                    mdMemoryStream.Close();
                    mdMemoryStream.Dispose();
                    mdStreamReader.Close();
                    mdStreamReader.Dispose();
                }
            }

            // se è stata correttamente compilata e criptata la stringa con l'xml da salvare,
            // si scrive il file xml
            if (!String.IsNullOrEmpty(cryptedXml))
                File.WriteAllText(mdXmlFilePath, cryptedXml);
        }

        /// <summary>
        /// Recupera il percorso del file xml di configurazione dei moduli attivati/disattivati.
        /// </summary>
        /// <returns>Il percorso del file xml di configurazione dei moduli attivati/disattivati</returns>
        private static string GetXmlModulesFilePath()
        {
            return HttpContext.Current.Server.MapPath(Settings.Default.ModulesActivationFilePath);
        }

        #endregion

        public static String GetLoginPageUrl()
        {
            return "Default.aspx";
        }

        #region CRIPTA/DECRIPTA

        public static string CreateSalt(int size)
        {
            var rng = new RNGCryptoServiceProvider();
            var buff = new byte[size];
            rng.GetBytes(buff);
            return Convert.ToBase64String(buff);
        }

        /// <summary>
        /// Ritorna la chiave pubblica usata per decriptare 
        /// </summary>
        /// <returns></returns>
        public static RSACryptoServiceProvider GetRSACryptoServiceProvider()
        {
            RSACryptoServiceProvider RSACSP = null;

            //file dove risiede la chiave pubblica
            FileInfo fileInfo = new FileInfo(HttpContext.Current.Server.MapPath(Settings.Default.PKPath));

            //se il file con la chiave pubblica esiste
            if (fileInfo.Exists)

                //creo uno stream reader contenete tutto il file
                using (StreamReader sr = new StreamReader(fileInfo.OpenRead()))
                {
                    RSACSP = new RSACryptoServiceProvider();

                    //ottengo la chiave pubblica contentete nel file decriptata
                    string mD5DecryptString = MD5DecryptString(sr.ReadToEnd(), Settings.Default.Password);

                    //se si ottiene un valore della chioave pubblica
                    if (!String.IsNullOrEmpty(mD5DecryptString))
                        RSACSP.FromXmlString(mD5DecryptString);
                }

            //ottengo il provider di decriptazione
            return RSACSP;
        }

        public static string MD5EncryptString(string Message, string Passphrase)
        {
            byte[] Results;
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();

            // Step 1. We hash the passphrase using MD5
            // We use the MD5 hash generator as the result is a 128 bit byte array
            // which is a valid length for the TripleDES encoder we use below

            MD5CryptoServiceProvider HashProvider = new MD5CryptoServiceProvider();
            byte[] TDESKey = HashProvider.ComputeHash(UTF8.GetBytes(Passphrase));

            // Step 2. Create a new TripleDESCryptoServiceProvider object
            TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();

            // Step 3. Setup the encoder
            TDESAlgorithm.Key = TDESKey;
            TDESAlgorithm.Mode = CipherMode.ECB;
            TDESAlgorithm.Padding = PaddingMode.PKCS7;

            // Step 4. Convert the input string to a byte[]
            byte[] DataToEncrypt = UTF8.GetBytes(Message);

            // Step 5. Attempt to encrypt the string
            try
            {
                ICryptoTransform Encryptor = TDESAlgorithm.CreateEncryptor();
                Results = Encryptor.TransformFinalBlock(DataToEncrypt, 0, DataToEncrypt.Length);
            }
            finally
            {
                // Clear the TripleDes and Hashprovider services of any sensitive information
                TDESAlgorithm.Clear();
                HashProvider.Clear();
            }

            // Step 6. Return the encrypted string as a base64 encoded string
            return Convert.ToBase64String(Results);
        }

        public static string MD5DecryptString(string Message, string Passphrase)
        {
            byte[] Results;
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();

            // Step 1. We hash the passphrase using MD5
            // We use the MD5 hash generator as the result is a 128 bit byte array
            // which is a valid length for the TripleDES encoder we use below

            MD5CryptoServiceProvider HashProvider = new MD5CryptoServiceProvider();
            byte[] TDESKey = HashProvider.ComputeHash(UTF8.GetBytes(Passphrase));

            // Step 2. Create a new TripleDESCryptoServiceProvider object
            TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();

            // Step 3. Setup the decoder
            TDESAlgorithm.Key = TDESKey;
            TDESAlgorithm.Mode = CipherMode.ECB;
            TDESAlgorithm.Padding = PaddingMode.PKCS7;

            // Step 4. Convert the input string to a byte[]
            byte[] DataToDecrypt = Convert.FromBase64String(Message);

            // Step 5. Attempt to decrypt the string
            try
            {
                ICryptoTransform Decryptor = TDESAlgorithm.CreateDecryptor();
                Results = Decryptor.TransformFinalBlock(DataToDecrypt, 0, DataToDecrypt.Length);
            }
            finally
            {
                // Clear the TripleDes and Hashprovider services of any sensitive information
                TDESAlgorithm.Clear();
                HashProvider.Clear();
            }

            // Step 6. Return the decrypted string in UTF8 format
            return UTF8.GetString(Results);
        }

        public static byte[] Encrypt(byte[] clearData, byte[] Key, byte[] IV)
        {
            // Create a MemoryStream to accept the encrypted bytes 
            MemoryStream ms = new MemoryStream();

            // Create a symmetric algorithm. 
            // We are going to use Rijndael because it is strong and
            // available on all platforms. 
            // You can use other algorithms, to do so substitute the
            // next line with something like 
            //      TripleDES alg = TripleDES.Create(); 
            Rijndael alg = Rijndael.Create();

            // Now set the key and the IV. 
            // We need the IV (Initialization Vector) because
            // the algorithm is operating in its default 
            // mode called CBC (Cipher Block Chaining).
            // The IV is XORed with the first block (8 byte) 
            // of the data before it is encrypted, and then each
            // encrypted block is XORed with the 
            // following block of plaintext.
            // This is done to make encryption more secure. 

            // There is also a mode called ECB which does not need an IV,
            // but it is much less secure. 
            alg.Key = Key;
            alg.IV = IV;

            // Create a CryptoStream through which we are going to be
            // pumping our data. 
            // CryptoStreamMode.Write means that we are going to be
            // writing data to the stream and the output will be written
            // in the MemoryStream we have provided. 
            CryptoStream cs = new CryptoStream(ms,
               alg.CreateEncryptor(), CryptoStreamMode.Write);

            // Write the data and make it do the encryption 
            cs.Write(clearData, 0, clearData.Length);

            // Close the crypto stream (or do FlushFinalBlock). 
            // This will tell it that we have done our encryption and
            // there is no more data coming in, 
            // and it is now a good time to apply the padding and
            // finalize the encryption process. 
            cs.Close();

            // Now get the encrypted data from the MemoryStream.
            // Some people make a mistake of using GetBuffer() here,
            // which is not the right way. 
            byte[] encryptedData = ms.ToArray();

            return encryptedData;
        }

        public static string Encrypt(string clearText, string Password)
        {
            // First we need to turn the input string into a byte array. 
            byte[] clearBytes =
              System.Text.Encoding.Unicode.GetBytes(clearText);

            // Then, we need to turn the password into Key and IV 
            // We are using salt to make it harder to guess our key
            // using a dictionary attack - 
            // trying to guess a password by enumerating all possible words. 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
            0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

            // Now get the key/IV and do the encryption using the
            // function that accepts byte arrays. 
            // Using PasswordDeriveBytes object we are first getting
            // 32 bytes for the Key 
            // (the default Rijndael key length is 256bit = 32bytes)
            // and then 16 bytes for the IV. 
            // IV should always be the block size, which is by default
            // 16 bytes (128 bit) for Rijndael. 
            // If you are using DES/TripleDES/RC2 the block size is
            // 8 bytes and so should be the IV size. 
            // You can also read KeySize/BlockSize properties off
            // the algorithm to find out the sizes. 
            byte[] encryptedData = Encrypt(clearBytes,
                     pdb.GetBytes(32), pdb.GetBytes(16));

            // Now we need to turn the resulting byte array into a string. 
            // A common mistake would be to use an Encoding class for that.
            //It does not work because not all byte values can be
            // represented by characters. 
            // We are going to be using Base64 encoding that is designed
            //exactly for what we are trying to do. 
            return Convert.ToBase64String(encryptedData);

        }

        public static byte[] Encrypt(byte[] clearData, string Password)
        {
            // We need to turn the password into Key and IV. 
            // We are using salt to make it harder to guess our key
            // using a dictionary attack - 
            // trying to guess a password by enumerating all possible words. 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
            0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

            // Now get the key/IV and do the encryption using the function
            // that accepts byte arrays. 
            // Using PasswordDeriveBytes object we are first getting
            // 32 bytes for the Key 
            // (the default Rijndael key length is 256bit = 32bytes)
            // and then 16 bytes for the IV. 
            // IV should always be the block size, which is by default
            // 16 bytes (128 bit) for Rijndael. 
            // If you are using DES/TripleDES/RC2 the block size is 8
            // bytes and so should be the IV size. 
            // You can also read KeySize/BlockSize properties off the
            // algorithm to find out the sizes. 
            return Encrypt(clearData, pdb.GetBytes(32), pdb.GetBytes(16));

        }

        public static void Encrypt(string fileIn, string fileOut, string Password)
        {

            // First we are going to open the file streams 
            FileStream fsIn = new FileStream(fileIn,
                FileMode.Open, FileAccess.Read);
            FileStream fsOut = new FileStream(fileOut,
                FileMode.OpenOrCreate, FileAccess.Write);

            // Then we are going to derive a Key and an IV from the
            // Password and create an algorithm 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
            0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

            Rijndael alg = Rijndael.Create();
            alg.Key = pdb.GetBytes(32);
            alg.IV = pdb.GetBytes(16);

            // Now create a crypto stream through which we are going
            // to be pumping data. 
            // Our fileOut is going to be receiving the encrypted bytes. 
            CryptoStream cs = new CryptoStream(fsOut,
                alg.CreateEncryptor(), CryptoStreamMode.Write);

            // Now will will initialize a buffer and will be processing
            // the input file in chunks. 
            // This is done to avoid reading the whole file (which can
            // be huge) into memory. 
            int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int bytesRead;

            do
            {
                // read a chunk of data from the input file 
                bytesRead = fsIn.Read(buffer, 0, bufferLen);

                // encrypt it 
                cs.Write(buffer, 0, bytesRead);
            } while (bytesRead != 0);

            // close everything 

            // this will also close the unrelying fsOut stream
            cs.Close();
            fsIn.Close();
        }

        public static byte[] Decrypt(byte[] cipherData, byte[] Key, byte[] IV)
        {
            // Create a MemoryStream that is going to accept the
            // decrypted bytes 
            MemoryStream ms = new MemoryStream();

            // Create a symmetric algorithm. 
            // We are going to use Rijndael because it is strong and
            // available on all platforms. 
            // You can use other algorithms, to do so substitute the next
            // line with something like 
            //     TripleDES alg = TripleDES.Create(); 
            Rijndael alg = Rijndael.Create();

            // Now set the key and the IV. 
            // We need the IV (Initialization Vector) because the algorithm
            // is operating in its default 
            // mode called CBC (Cipher Block Chaining). The IV is XORed with
            // the first block (8 byte) 
            // of the data after it is decrypted, and then each decrypted
            // block is XORed with the previous 
            // cipher block. This is done to make encryption more secure. 
            // There is also a mode called ECB which does not need an IV,
            // but it is much less secure. 
            alg.Key = Key;
            alg.IV = IV;

            // Create a CryptoStream through which we are going to be
            // pumping our data. 
            // CryptoStreamMode.Write means that we are going to be
            // writing data to the stream 
            // and the output will be written in the MemoryStream
            // we have provided. 
            CryptoStream cs = new CryptoStream(ms,
                alg.CreateDecryptor(), CryptoStreamMode.Write);

            // Write the data and make it do the decryption 
            cs.Write(cipherData, 0, cipherData.Length);

            // Close the crypto stream (or do FlushFinalBlock). 
            // This will tell it that we have done our decryption
            // and there is no more data coming in, 
            // and it is now a good time to remove the padding
            // and finalize the decryption process. 
            cs.Close();

            // Now get the decrypted data from the MemoryStream. 
            // Some people make a mistake of using GetBuffer() here,
            // which is not the right way. 
            byte[] decryptedData = ms.ToArray();

            return decryptedData;
        }

        public static string Decrypt(string cipherText, string Password)
        {
            // First we need to turn the input string into a byte array. 
            // We presume that Base64 encoding was used 
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            // Then, we need to turn the password into Key and IV 
            // We are using salt to make it harder to guess our key
            // using a dictionary attack - 
            // trying to guess a password by enumerating all possible words. 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65,
            0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

            // Now get the key/IV and do the decryption using
            // the function that accepts byte arrays. 
            // Using PasswordDeriveBytes object we are first
            // getting 32 bytes for the Key 
            // (the default Rijndael key length is 256bit = 32bytes)
            // and then 16 bytes for the IV. 
            // IV should always be the block size, which is by
            // default 16 bytes (128 bit) for Rijndael. 
            // If you are using DES/TripleDES/RC2 the block size is
            // 8 bytes and so should be the IV size. 
            // You can also read KeySize/BlockSize properties off
            // the algorithm to find out the sizes. 
            byte[] decryptedData = Decrypt(cipherBytes,
                pdb.GetBytes(32), pdb.GetBytes(16));

            // Now we need to turn the resulting byte array into a string. 
            // A common mistake would be to use an Encoding class for that.
            // It does not work 
            // because not all byte values can be represented by characters. 
            // We are going to be using Base64 encoding that is 
            // designed exactly for what we are trying to do. 
            return System.Text.Encoding.Unicode.GetString(decryptedData);
        }

        public static byte[] Decrypt(byte[] cipherData, string Password)
        {
            // We need to turn the password into Key and IV. 
            // We are using salt to make it harder to guess our key
            // using a dictionary attack - 
            // trying to guess a password by enumerating all possible words. 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
            0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

            // Now get the key/IV and do the Decryption using the 
            //function that accepts byte arrays. 
            // Using PasswordDeriveBytes object we are first getting
            // 32 bytes for the Key 
            // (the default Rijndael key length is 256bit = 32bytes)
            // and then 16 bytes for the IV. 
            // IV should always be the block size, which is by default
            // 16 bytes (128 bit) for Rijndael. 
            // If you are using DES/TripleDES/RC2 the block size is
            // 8 bytes and so should be the IV size. 

            // You can also read KeySize/BlockSize properties off the
            // algorithm to find out the sizes. 
            return Decrypt(cipherData, pdb.GetBytes(32), pdb.GetBytes(16));
        }

        public static void Decrypt(string fileIn, string fileOut, string Password)
        {

            // First we are going to open the file streams 
            FileStream fsIn = new FileStream(fileIn,
                        FileMode.Open, FileAccess.Read);
            FileStream fsOut = new FileStream(fileOut,
                        FileMode.OpenOrCreate, FileAccess.Write);

            // Then we are going to derive a Key and an IV from
            // the Password and create an algorithm 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
            0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
            Rijndael alg = Rijndael.Create();

            alg.Key = pdb.GetBytes(32);
            alg.IV = pdb.GetBytes(16);

            // Now create a crypto stream through which we are going
            // to be pumping data. 
            // Our fileOut is going to be receiving the Decrypted bytes. 
            CryptoStream cs = new CryptoStream(fsOut,
                alg.CreateDecryptor(), CryptoStreamMode.Write);

            // Now will will initialize a buffer and will be 
            // processing the input file in chunks. 
            // This is done to avoid reading the whole file (which can be
            // huge) into memory. 
            int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int bytesRead;

            do
            {
                // read a chunk of data from the input file 
                bytesRead = fsIn.Read(buffer, 0, bufferLen);

                // Decrypt it 
                cs.Write(buffer, 0, bytesRead);

            } while (bytesRead != 0);

            // close everything 
            cs.Close(); // this will also close the unrelying fsOut stream 
            fsIn.Close();
        }

        static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an Rijndael object
            // with the specified key and IV.
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return encrypted;

        }

        static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Rijndael object
            // with the specified key and IV.
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }

        #endregion

        public static string GeneraCodiceFiscale(string sNome, string sCognome, DateTime dtmDataNascita, string sSesso, string sComune, string sProvincia)
        {
            //verifica che tutti i campi obbligatori non siano vuoti
            if (CommonService.Nz(sNome, "") == "" || CommonService.Nz(sCognome, "") == ""
            || CommonService.Nz(dtmDataNascita, new DateTime()) == new DateTime() || CommonService.Nz(sSesso, "") == ""
            || CommonService.Nz(sComune, "") == "" || CommonService.Nz(sProvincia, "") == "")
                return GetLocalizedString(PowerWebResources.ERR_DATI_INSUFFICIENTI);
            string sRetVal = "";
            sCognome = CodFiscale.FormattaStringa(sCognome);
            sNome = CodFiscale.FormattaStringa(sNome);
            sRetVal = CodFiscale.ElaboraCognome(sCognome.ToUpper()) + CodFiscale.ElaboraNome(sNome.ToUpper()) + CodFiscale.ElaboraDataNascitaESesso(dtmDataNascita, sSesso.ToUpper())
              + CodFiscale.ElaboraCodiceComune(sComune.ToUpper(), sProvincia.ToUpper());
            if (sRetVal.Length != 15)
                return GetLocalizedString(PowerWebResources.ERR_IN_CALCOLO_CODICE_FISCALE);
            sRetVal += CodFiscale.CalcolaUltimaLettera(sRetVal);
            return sRetVal;
        }

        #region Dictionaries usati per l'Avanzamento della ProgressBar

        private static Dictionary<Utenti, KeyValuePair<double, string>> _elaborateStatusDictionary = new Dictionary<Utenti, KeyValuePair<double, string>>();

        public static Dictionary<Utenti, KeyValuePair<double, string>> ElaborateStatusDictionary
        {
            get
            {
                return _elaborateStatusDictionary;
            }
            set
            {
                _elaborateStatusDictionary = value;
            }
        }

        private static Dictionary<Utenti, bool> _isToCloseLoadingPanel = new Dictionary<Utenti, bool>();

        public static Dictionary<Utenti, bool> IsToCloseLoadingPanel
        {
            get
            {
                return _isToCloseLoadingPanel;
            }
            set
            {
                _isToCloseLoadingPanel = value;
            }
        }

        #endregion

        private static Dictionary<Utenti, bool> _elaborateTripsHasErrors = new Dictionary<Utenti, bool>();

        public static Dictionary<Utenti, bool> ElaborateTripsHasErrors
        {
            get
            {
                return _elaborateTripsHasErrors;
            }
            set
            {
                _elaborateTripsHasErrors = value;
            }
        }


        //Appena c'è tenpo rifare questa funzione che è davvv
        public static List<ActivityItem> PopulateActivityList(List<Reg_V> regvs)
        {
            List<ActivityItem> result = new List<ActivityItem>();

            regvs = regvs.Where(regv => !regv.Registrazione_Bloccata).ToList();

            var sadCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.SadCpExport);
            Fil castanoFil = RepoManager.FilRepo.FirstOrDefault(c => c.Codice_Fil == "SAD CP");


            var cants = RepoManager.CantRepo.GetAll(true).ToList();
            var colVars = RepoManager.Col_VarRepo.GetAll(true).ToList();
            var cantVars = RepoManager.Cant_VarRepo.GetAll(true).ToList();
            var fils = RepoManager.FilRepo.GetAll(true).ToList();
            var resps = RepoManager.RespRepo.GetAll(true).ToList();
            var clis = RepoManager.CliRepo.GetAll(true).ToList();

            var activities =
                regvs.Where(
                    regv =>
                    regv.Cant_Id.HasValue && regv.Col_Id.HasValue &&
                    regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Att).ToList();

            var activitiesTypes = cants.Where(cant => cant.Tipologia_Can == "ATT" && !cant.DisAbilitazione_Can && cant.Tipo_Cantiere_Can == "PRE").ToList();

            regvs =
                regvs.Where(
                    regv =>
                    regv.Cant_Id.HasValue && regv.Col_Id.HasValue && regv.RegU != null &&
                    regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att &&
                    regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass).ToList();

            // leggo la customizzazione dell'utilizzo dei dati su base storica o meno
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.Export56HistoricUseEnum);

            foreach (var regv in regvs)
            {
                var currentCant = customizationVersion == (int)Export56HistoricUseEnum.History
                    ? cants.SingleOrDefault(cant => cant.Cant_Id == regv.Cant_Id && cant.DataOraUltimaModifica_Can <= regv.Data_Ora_Fis_E)
                    : cants.SingleOrDefault(cant => cant.Cant_Id == regv.Cant_Id);


                bool onlCant = false;
                Cant_Var currentCantvar = null;
                if (currentCant == null)
                {
                    var oldCantvars = cantVars.Where(cantvar => cantvar.DataOraUltimaModifica_Can <= regv.Data_Ora_Fis_E && cantvar.Cant_Id == regv.Cant_Id)
                                              .OrderBy(cantvar => cantvar.DataOraUltimaModifica_Can).ToList();
                    if (oldCantvars.Any())
                    {
                        currentCantvar = oldCantvars.Last();
                        onlCant = currentCantvar.Tipo_Cantiere_Can == "ONL";
                    }
                    else
                    {
                        currentCant = cants.Single(cant => cant.Cant_Id == regv.Cant_Id);
                        onlCant = currentCant.Tipo_Cantiere_Can == "ONL";
                    }
                }
                else // se non si necessita processare un cant_var allora si verifica comunque il tipo onl
                {
                    currentCant = cants.Single(cant => cant.Cant_Id == regv.Cant_Id);
                    onlCant = currentCant.Tipo_Cantiere_Can == "ONL";
                }

                // non si elabora una regv il cui cantiere sia come tipo ONL (ore non lavorate)
                if (!onlCant)
                {
                    var currentCol = customizationVersion == (int)Export56HistoricUseEnum.History
                        ? RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == regv.Col_Id && col.DataOraUltimaModifica_Col <= regv.Data_Ora_Fis_E)
                        : RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == regv.Col_Id);

                    Col_Var currentColvar = null;
                    if (currentCol == null)
                    {
                        var oldColvars = colVars.Where(colVar => colVar.DataOraUltimaModifica_Col <= regv.Data_Ora_Fis_E && colVar.Col_Id == regv.Col_Id)
                                                .OrderBy(colVar => colVar.DataOraUltimaModifica_Col).ToList();
                        if (oldColvars.Any())
                            currentColvar = oldColvars.Last();
                        else currentCol = RepoManager.ColRepo.Single(col => col.Col_Id == regv.Col_Id);
                    }

                    var currentFil = currentCant != null
                                          ? fils.SingleOrDefault(fil => fil.Fil_Id == currentCant.Fil_Id)
                                          : fils.SingleOrDefault(fil => fil.Fil_Id == currentCantvar.Fil_Id);

                    var currentResp = currentCant != null
                                          ? resps.SingleOrDefault(resp => resp.Codice_Resp == currentCant.Raggruppamento1_Can)
                                          : resps.SingleOrDefault(resp => resp.Codice_Resp == currentCantvar.Raggruppamento1_Can);

                    var currentCli = currentCant != null
                                         ? clis.SingleOrDefault(cli => cli.Cli_Id == currentCant.Cli_Id)
                                         : clis.SingleOrDefault(cli => cli.Cli_Id == currentCantvar.Cli_Id);

                    string registrazioneTipoReg = String.Empty;
                    switch ((RegTypeEnum)regv.Registrazione_Tipo_Reg)
                    {
                        case RegTypeEnum.Trip:
                            registrazioneTipoReg = BusinessService.GetLocalizedString(PowerWebResources.TD_VIAGGIO).ToUpper();
                            break;
                        case RegTypeEnum.Pass:
                            registrazioneTipoReg = BusinessService.GetLocalizedString(PowerWebResources.TD_PASS).ToUpper();
                            break;
                        case RegTypeEnum.Att:
                            registrazioneTipoReg = BusinessService.GetLocalizedString(PowerWebResources.TD_ATT).ToUpper();
                            break;
                        default:
                            registrazioneTipoReg = String.Empty;
                            break;
                    }

                    string kmToInsert = String.Empty;

                    if (currentCant != null && sadCustomization == 1 && currentCant.Codice_Cantiere == "SAD CP00034")
                    {
                        regv.Durata_Fis = 0;
                    }

                    if (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip)
                    {
                        if (sadCustomization == 1 && currentCol.Raggruppamento1_Col == castanoFil.Fil_Id.ToString())
                        {
                            regv.Durata_Fis = 0;
                        }

                        if (regv.KM_Reg.HasValue)
                            kmToInsert = regv.KM_Reg.Value == 0 ? "NO_KM" : regv.KM_Reg.Value.ToString();
                        else
                            kmToInsert = "NO_KM";
                    }
                    else
                        kmToInsert = regv.KM_Reg.HasValue ? regv.KM_Reg.Value.ToString() : String.Empty;

                    var tempoAttivitaCan = currentCant != null ? currentCant.Tempo_Attivita_Can : currentCantvar.Tempo_Attivita_Can;

                    DateTime tempoPrevisto = DateTime.MinValue;
                    if (tempoAttivitaCan != null)
                        tempoPrevisto = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, tempoAttivitaCan.Value.Hours, tempoAttivitaCan.Value.Minutes, tempoAttivitaCan.Value.Seconds);


                    var currentActivity = new ActivityItem
                    {
                        CantCodFisc = currentCant != null ? currentCant.Cod_Fisc_Can : currentCantvar.Cod_Fisc_Can,
                        CantDesc = currentCant != null ? currentCant.Descrizione_Can : currentCantvar.Descrizione_Can,
                        CantPlace = currentCant != null ? currentCant.Luogo_Can : currentCantvar.Luogo_Can,
                        CantAddress = currentCant != null ? currentCant.Indirizzo_Can : currentCantvar.Indirizzo_Can,
                        RegKM = kmToInsert,
                        CantCAP = currentCant != null ? currentCant.Cap_Can : currentCantvar.Cap_Can,
                        CantASL = currentFil != null ? currentFil.Descrizione_Fil : String.Empty,
                        ColCognome = currentCol != null ? currentCol.Cognome_Col : currentColvar.Cognome_Col,
                        ColNome = currentCol != null ? currentCol.Nome_Col : currentColvar.Nome_Col,
                        ColCodice = currentCol != null ? currentCol.Codice_Collaboratore : currentColvar.Codice_Collaboratore,
                        RegDate = regv.Data_Ora_Fis_E.Date,
                        RegHHMMSSInizio = regv.Data_Ora_Fis_E.TimeOfDay,
                        RegHHMMSSFine = regv.Data_Ora_Fis_U.Value.TimeOfDay,
                        RegMMDurata = regv.Durata_Fis.Value,
                        RegTipo = registrazioneTipoReg,
                        TipoModifica = regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip ? (int)RegModifyTypeEnum.None : regv.Tipo_Modifica,
                        ColDistretto = currentResp != null ? currentResp.Descrizione_Resp : String.Empty,
                        ColQualifica = currentCol != null ? currentCol.Qualifica_Col : currentColvar.Qualifica_Col,
                        Cli_Tipo = currentCli != null ? currentCli.Cognome_Cli : String.Empty, //currentCli.CognomeNome_Cli,
                        Cant_Livello = currentCant != null ? currentCant.Livello_Assistito_Can : currentCantvar.Livello_Assistito_Can,
                        CapoAreaCant = currentCant != null ? currentCant.Codice_Voucher_Can : currentCantvar.Codice_Voucher_Can,
                        CapoAreaCol = currentCol != null ? currentCol.Codice_Iban_Col : currentColvar.Codice_Iban_Col,
                        TempoPrevisto = tempoPrevisto
                    };

                    foreach (var currentActivityType in activitiesTypes)
                    {
                        string currentActivityTypeString = currentActivityType.Codice_Cantiere.Trim();
                        currentActivity[currentActivityTypeString] = null;
                        currentActivity.AdditionalCols.Add(currentActivityTypeString);
                    }

                    var associatedActivities = activities.Where(act => act.RiferimentoRRN_Att == regv.RegE).ToList();
                    foreach (var currentAssociatedActivity in associatedActivities)
                        currentActivity[currentAssociatedActivity.Cant_Mnemonic.Trim()] = "1";

                    result.Add(currentActivity);
                }
            }

            result = result.OrderBy(act => act.RegAAAA).ThenBy(act => act.RegMM).ThenBy(act => act.RegGG).ThenBy(act => act.RegHHMMSSInizio).ThenBy(act => act.RegHHMMSSFine).ToList();

            return result;
        }

        #region Gestione delle proposizioni automatica delle registrazioni

        #region Gestione della proposizione automatica delle registrazioni con l'aggiunta di due minuti

        /// <summary>
        /// inserisce i minuti specificati alla reg di uscita se ho delle registrazioni singole e ne restituisce il risultato
        /// </summary>
        /// <param name="regvs">le reg_v da processare (saranno estratte le singole, processate e ritornate).</param>
        /// <param name="minute">Il numero minuti da aggiungere per la chiussura.</param>
        /// <returns>L'elenco delle reg_v chiuse con i due minuti</returns>
        public static IQueryable<Reg_V> ProposeRegsAddingMinutes(IQueryable<Reg_V> regvs, int minute)
        {
            ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(0, "Inizio correzione registrazioni");

            Dictionary<string, string> result = new Dictionary<string, string>();

            List<Reg_V> regMinute = new List<Reg_V>();

            //estraggo il collaboratore della reg
            var coldIs = regvs.Select(reg => reg.Col_Id).Distinct();

            //dalle reg_v passate come parametro processo solamente le singole che hanno un collaboratore e sono delle ore
            var singleRegvs = regvs.Where(regV => regV.Col_Id.HasValue && coldIs.Contains(regV.Col_Id) && regV.Registrazione_Stato_Reg == (int)RegStateEnum.None && regV.Registrazione_Tipo_Reg == (int)RegTypeEnum.None).ToList();

            //eseguo un ordinamento per data
            var regToElaborate = singleRegvs.OrderBy(regV => regV.Data_Reg);

            var newRegVs = new List<Reg_V>();

            foreach (var regV in regToElaborate)
            {
                //aggiungo x minuti alla timbrature di entrata
                var minuteAdded = (regV.Data_Ora_Fis_E.AddMinutes(minute));

                //vado a crearmi la nuova reg aggiungendo due minuti alla reg di uscita
                regV.Data_Ora_Fis_U = minuteAdded;

                // se non ho errori di sovrapposizione creo la mia nuova lista di reg
                if (RepoManager.Reg_VRepo.CheckOverlaps(new List<Reg_V>() { regV }, true) != null)
                    newRegVs.Add(regV);
            }

            return newRegVs.AsQueryable();
        }

        #endregion

        #region Gestione della proposizione automatica delle registrazioni su base storica

        /// <summary>
        /// Propone la quadratura delle registrazioni su base storica (descrizione algoritmo all'iterno del metodo).
        /// </summary>
        /// <param name="regvs">le reg_v da processare per proporre la chiusura (sono tutte le reg_v di un periodo).</param>
        /// <param name="dayTollerance">Il numero di minuti di tolleranza utilizzati nella ricerca dei giorni utili alla proposizione storica.</param>
        /// <param name="averageTollerance">Il numero di minuti di tolleranza utilizzati nella definizione della media da proporre per la chiusura.</param>
        /// <param name="historyDay">Il numero di giorni da verificare (nel passato o nel futuro rispetto alle registrazioni processate) per la costruzione della base storica.</param>
        /// <param name="averageBaseThreshold">Indica il numero di elemento minimo per considerare valida la media delle ore proposte.</param>
        /// <returns>
        /// L'elenco delle reg_v processate per la chiusura (solamente quelle modificate dalla procedura di propsizione).
        /// </returns>
        public static IQueryable<Reg_V> ProposeRegsBasedOnHistory(IQueryable<Reg_V> regvs, int dayTollerance, int averageTollerance, int historyDay, int averageBaseThreshold)
        {
            #region Descrizione dell'algoritmo utilizzato per il calcolo su base storica

            /*
             * DESCRIZIONE DELL'ALGORITMO:
             * - Dalle reg_v passate come parametro sono estratte le Reg_V singole; le stesse saranno poi raggruppate e ciclate per collaboratore/data.
             * - Per ogni collaboratore/cantiere e data:
             *   - Si recuperano tutte le reg del giorno di quel collaboratore/cantiere/data
             *   - Si recupereranno dal database tutte le registrazioni dei giorni pregressi/futuri (dipendenti dal parametro historyDay) che hanno un numero di registrazioni pari
             *     al numero di registrazioni presenti nel giorno in errore +1 e sono tutte abbinate;
             *   - Si cerca la registrazione mancante abbinando in sequenza le ore delle registrazioni del giorno da chiudere con le registrazioni di ogni singolo giorno recuperato;
             *     si scremano nuovamente i giorni in cui gli errori di sequenza rispetto alla tolleranza sono > 1; in questo ciclo salvo anche la probabile registrazione mancante (l'unica non
             *     in tolleranza).
             *   - Dell'elenco delle registrazioni mancanti è effettuata la media; è poi scartato il giorno con lo scostamento più alto nella timbratura proposta rispetto alla media calcolata quando
             *     lo stesso è fuori tolleranza; in logica ricorsiva si rieffettua la media fino a quando non sono più eliminati giorni; la media così risultante sarà la registrazione proposta
             *   - La media proposta risulta valida solamente se le registrazioni base per il calcolo sono in numero superiore o uguale a averageBaseThreshold; se inferiori l'ora proposta viene presa
             *     dal giorno in cui il delta per le singole ore si discosta meno dalle ore del giorno in esame (i giorni da trattare in questo caso sono quelli su cui si è calcolata precedentemente
             *     la media).
             *   - Se alla fine è stata calcolata un'ora proposta valida ricostruisco per la restituzione le Reg_V del giorno che sto processando e le aggiungo alla lista di ritorno
             */

            #endregion

            // inizializzo il valore di ritorno del metodo
            var returnReg_VsList = new List<Reg_V>();

            // dalle Reg_V passate come parametro si estraggono tutte le registrazioni singole
            var singleRegVs = regvs.Where(regv => regv.Registrazione_Stato_Reg == (int)RegStateEnum.None && regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None).AsQueryable();

            // si procede con l'elaborazione solamente se sono presenti nell'elenco passato come parametro delle Reg_V singole
            if (singleRegVs.Any())
            {
                // si elabora ogni singolo giorno, di ogni singolo cantiere di ogni singolo collaboratore
                foreach (var regVsByColId in singleRegVs.GroupBy(regv => regv.Col_Id))
                {
                    foreach (var regVsByColIdCantId in regVsByColId.GroupBy(regv => regv.Cant_Id))
                    {
                        foreach (var regVsByColIdCantIdDate in regVsByColIdCantId.GroupBy(regv => regv.Data_Reg))
                        {
                            // solo se la data in elaborazione ha un valore
                            if (regVsByColIdCantIdDate.Key.HasValue)
                            {

                                #region Elaborazione di ogni data di ogni cantiere di ogni collaboratore con Reg_V singole

                                // recupero dal database tutte le reg per la data/collaboratre/cantiere corrispondente
                                var dayMidnight = regVsByColIdCantIdDate.Key;
                                var dayLastSecond = new DateTime(regVsByColIdCantIdDate.Key.Value.Year, regVsByColIdCantIdDate.Key.Value.Month, regVsByColIdCantIdDate.Key.Value.Day, 23, 59, 59);
                                var singleDayRegs = RepoManager.RegRepo.Find(reg => reg.Col_Id == regVsByColId.Key
                                    && reg.Cant_Id == regVsByColIdCantId.Key
                                    && reg.Registrazione_Data_Ora_Fis_Reg >= dayMidnight
                                    && reg.Registrazione_Data_Ora_Fis_Reg <= dayLastSecond
                                    && reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None, true).AsQueryable();

                                // se ho trovato delle reg
                                if (singleDayRegs.Any())
                                {

                                    #region Calcolo del dizionario con la base storica da processare

                                    // ordino le reg del giorno da completare per data ora fisica ascendente
                                    var orderedSingleDayRegs = singleDayRegs.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).AsQueryable();

                                    // calcolo l'intervallo storico utilizzato per la ricerca delle registrazioni dalle quali calcolare la proposizione
                                    var historyDate = regVsByColIdCantIdDate.Key.Value.AddDays(-1 * historyDay);
                                    var futureDate = regVsByColIdCantIdDate.Key.Value.AddDays(historyDay);
                                    futureDate = new DateTime(futureDate.Year, futureDate.Month, futureDate.Day, 23, 59, 59);

                                    // recupero tutte le registrazioni antecedenti/seguenti al giorno che sto trattando nell'arco di historyDay giorni raggruppate per data
                                    var historyRegs = RepoManager.RegRepo.Find(reg => reg.Col_Id == regVsByColId.Key
                                        && reg.Cant_Id == regVsByColIdCantId.Key
                                        && ((reg.Registrazione_Data_Ora_Fis_Reg >= historyDate && reg.Registrazione_Data_Ora_Fis_Reg < dayMidnight)
                                           || (reg.Registrazione_Data_Ora_Fis_Reg <= futureDate && reg.Registrazione_Data_Ora_Fis_Reg > dayLastSecond))
                                        && reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None
                                        ).GroupBy(reg => reg.Registrazione_Data_Ora_Fis_Reg.Date).AsQueryable();

                                    // compongo un dizionario con l'elenco delle giornate buone per essere processate, e cioè dove il numero di registrazioni presenti è
                                    // uguale al numero di registrazioni del giorno da processare con errore +1 (e dove tutte le registrazioni del giorno sono abbinate)
                                    var searchCount = singleDayRegs.Count() + 1;
                                    var historyDictionary = new Dictionary<DateTime, Tuple<List<Reg>, Reg>>();
                                    foreach (var regsByDate in historyRegs)
                                    {
                                        // se nella giornata ho solo reg associate e il numero è congruo allora si tratta di un giorno valido e metto le registrazioni
                                        // del giorno nel dizionario
                                        if (regsByDate.All(reg => reg.Registrazione_Stato_Reg == (int)RegStateEnum.Ass) && regsByDate.Count() == searchCount)
                                            historyDictionary.Add(regsByDate.Key, new Tuple<List<Reg>, Reg>(regsByDate.ToList(), null));
                                    }

                                    #endregion

                                    #region Scremazione della base storica con i dati non utili alla media

                                    // per ogni giorno recuperato verifico le sqequenze in tolleranza; scarto i giorni in cui c'è più di una registrazione fuori tolleranza
                                    // e salvo nel dictionary la registrazione probabilmente mancante (l'unica fuori tolleranza)
                                    for (int dateIndex = 0; dateIndex < historyDictionary.Count; dateIndex++)
                                    {
                                        var dateTuple = historyDictionary.ElementAt(dateIndex);

                                        // inizializzo il contatore del numero di registrazioni fuori sequenza incontrate per quel giorno
                                        int noSequenceCount = 0;

                                        // inizializzo la prima registrazione fuori sequenza per il giorno
                                        Reg firstNoSequenceReg = null;

                                        // inizializzazione della posizione di inzio verifica del giorno in elaborazion
                                        int lastInTollerancePosition = 0;

                                        // inizializzazione del contatore del numero di registrazioni in tolleranza trovate nell'elaborazione del giorno
                                        int regInTolleranceCount = 0;

                                        // ciclo sulle registrazioni del giorno da chiudere
                                        foreach (var orderedSingleDayReg in orderedSingleDayRegs)
                                        {
                                            // calcolo gli estremi di tolleranza della data ora fisica della registrazione del giorno da chiudere in elaborazione
                                            TimeSpan lowThreshold = orderedSingleDayReg.Registrazione_Data_Ora_Fis_Reg.AddMinutes(-1 * dayTollerance).TimeOfDay;
                                            TimeSpan highThreshold = orderedSingleDayReg.Registrazione_Data_Ora_Fis_Reg.AddMinutes(dayTollerance).TimeOfDay;

                                            // ordino le registrazioni del giorno di controllo per data ora fisica ascendente
                                            var regsToCheck = dateTuple.Value.Item1.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                                            // itero su tutte le registrazioni del giorno di controllo ordinate per data ora fisica ascendente, a
                                            // partire dall'ultima registrazione in tolleranza trovata (il default è l'inizio della lista e quindi 0)
                                            for (int i = lastInTollerancePosition; i < regsToCheck.Count; i++)
                                            {
                                                // recupero la registrazione da processare per il controllo
                                                var regToCheck = regsToCheck[i];

                                                // verifico se la registrazione del giorno di confronto non è nella tolleranza rispetto alla registrazione
                                                if (regToCheck.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < lowThreshold || regToCheck.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > highThreshold)
                                                {
                                                    // se la registrazione non è in tolleranza allora incremento il numero di registrazioni in sequenza fuori tolleranza trovate
                                                    noSequenceCount++;

                                                    // se sto processando la prima registrazione fuori tolleranza allora la salvo nell'apposita variabile
                                                    // (è la probabile proposizione da effettuare una volta mediata con le altre)
                                                    if (noSequenceCount == 1)
                                                        firstNoSequenceReg = regToCheck;

                                                    // se nella sequenza ho riscontrato più di una definizione fuori tolleranza allora svuoto la prima registrazione
                                                    // fuori tolleranza (così da non inserirla nella tuple del dictionary successivamente) e interrompo l'esecuzione
                                                    // del ciclo (in definitiva: questo giorno non è buono per propore la media)
                                                    if (noSequenceCount > 1)
                                                    {
                                                        firstNoSequenceReg = null;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    // se invece la registrazione è in tolleranza allora devo proseguire con la sequenza (cioé devo far si che la prossima registrazione
                                                    // del giorno da chiudere sia confrontata con la successiva del giorno di controllo);
                                                    // per questo incremento l'indice dell'ultima registrazione in tolleranza (l'inzio del ciclo attuale sarà la registrazione successiva a quella 
                                                    // attualmente in elaborazione) e interrompo l'attuale elaborazione.
                                                    lastInTollerancePosition = ++i;
                                                    regInTolleranceCount++;
                                                    break;
                                                }
                                            }

                                            // se al termine del ciclo di accoppiamento è stata superata la singola registrazione non in tolleranza si scarta il giorno
                                            // e quindi si interrompe l'attuale ciclo (la prima registrazione fuori sequenza è stata svuotata nel ciclo annidato)
                                            if (noSequenceCount > 1)
                                                break;
                                        }

                                        // se all'uscita dal ciclo sono stato sbattuto fuori e tutte le registrazioni da chiudere sono in tolleranza e non sono riuscito a scegliere
                                        // una registrazione da proporre significa che la reg da proporre è l'ultima del giorno in elaborazione (per come è configurato il ciclo sopra
                                        // quest'eventualità non verrà mai presa in considerazione)
                                        if (regInTolleranceCount == orderedSingleDayRegs.Count() && firstNoSequenceReg == null)
                                            firstNoSequenceReg = dateTuple.Value.Item1.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).LastOrDefault();

                                        // al termine dei cicli annidati di elaborazione, se è stata trovata la registrazione mancante utile alla proposizione
                                        // allora si imposta nella tuple del giorno tale dato, di modo da rendere il giorno anche come buono per la proposizione della media
                                        // (se il giorno non risulta "buono" per il controllo di sequenza il valore della prima registrazione non in sequenza è null per cui
                                        // non viene effettuato nessun controllo sul suo valore)
                                        historyDictionary[dateTuple.Key] = new Tuple<List<Reg>, Reg>(dateTuple.Value.Item1, firstNoSequenceReg);
                                    }

                                    #endregion

                                    #region Calcolo dell'ora proposta per la chiusura

                                    // calcolo la media dell'ora fisica delle registrazioni processabili (e cioè delle registrazioni nei giorni "buoni" che hanno evidenziato una proposta
                                    IEnumerable<Reg> averageCalculateList = new List<Reg>();
                                    var proposedHour = GetProposalsAverage(historyDictionary.Where(kvp => kvp.Value.Item2 != null).Select(kvp => kvp.Value.Item2)
                                        , averageTollerance
                                        , averageBaseThreshold
                                        , out averageCalculateList);

                                    // converto l'IEnumerable restituito dal metodo con le timbrature utilizzate per il calcolo della media in una lista
                                    var averageRegsList = averageCalculateList.ToList();

                                    // se non è stata calcolata una media valida e la lista di calcolo della media è popolata
                                    if (proposedHour == TimeSpan.Zero && averageCalculateList.Any())
                                        proposedHour = GetProposalClosestDayHour(
                                            historyDictionary.Where(kvp => kvp.Value.Item2 != null).ToDictionary(pair => pair.Key, pair => pair.Value),
                                            orderedSingleDayRegs.ToList());

                                    #endregion

                                    #region Aggiornamento della lista di ritorno (da reg a reg_v)

                                    // se abbiamo un'ora proposta valida calcolata
                                    if (proposedHour != TimeSpan.Zero)
                                    {
                                        // l'ora proposta non deve avere secondi
                                        proposedHour = new TimeSpan(proposedHour.Hours, proposedHour.Minutes, 0);

                                        // creo a partire da una registrazione modello una nuova reg che abbia i dati della precedente
                                        // ma la data fisica/originale e ora uguale a quella prosposta
                                        var modelReg = orderedSingleDayRegs.FirstOrDefault();
                                        var newReg = RepoManager.RegRepo.Init();
                                        newReg.Cant_Id = modelReg.Cant_Id;
                                        newReg.Col_Id = modelReg.Col_Id;
                                        newReg.Registrazione_Data_Ora_Fis_Reg = new DateTime(modelReg.Registrazione_Data_Ora_Fis_Reg.Year
                                            , modelReg.Registrazione_Data_Ora_Fis_Reg.Month
                                            , modelReg.Registrazione_Data_Ora_Fis_Reg.Day
                                            , proposedHour.Hours
                                            , proposedHour.Minutes
                                            , proposedHour.Seconds);
                                        newReg.Registrazione_Data_Ora_Orig_Reg = DateTime.MinValue;

                                        // aggiungo la nuova registrazione all'elenco del giorno processato e
                                        // ordino nuovamente il tutto per data/ora fisica
                                        var prevRegs = orderedSingleDayRegs.ToList();
                                        prevRegs.Add(newReg);
                                        prevRegs = prevRegs.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                                        // a questo punto le registrazioni del giorno in elaborazione sono sempre in numero pari;
                                        // posso quindi ciclarle a due a due ed abbinarle nella lista di reg_V in processo al fine di poterle ritornare
                                        // aggiungendole alla lista di ritorno
                                        var regVsToElaborate = regVsByColIdCantIdDate.ToList();

                                        for (int regVsIndex = 0; regVsIndex < regVsToElaborate.Count; regVsIndex++)
                                        {
                                            // abbino a due a due le registrazioni
                                            int firstRegIndex = regVsIndex;
                                            int secondRegIndex = regVsIndex + 1;
                                            regVsToElaborate[regVsIndex].Data_Ora_Fis_E = prevRegs[firstRegIndex].Registrazione_Data_Ora_Fis_Reg;
                                            regVsToElaborate[regVsIndex].Data_Ora_Fis_U = prevRegs[secondRegIndex].Registrazione_Data_Ora_Fis_Reg;
                                        }

                                        returnReg_VsList.AddRange(regVsToElaborate);
                                    }

                                    #endregion

                                }

                                #endregion

                            }
                        }
                    }
                }
            }

            // ritorno fake del metodo per non avere problemi in fase di salvataggio/compilazione/deploy urgente
            return returnReg_VsList.AsQueryable();
        }

        /// <summary>
        /// Metodo che dato il dizionario contenente giorno per giorno la registrazione di proposta, ne calcola la media (scartando di volta in volta, con ricalcolo tutto ciò che è
        /// fuori media).
        /// </summary>
        /// <param name="averageRegsList">La lista delle registrazioni di cui è necessario calcolare la media delle ore fisiche utilizzando l'algoritmo esposto in testata del
        /// metodo ProposeRegsBasedOnHistory.</param>
        /// <param name="averageTollerance">La tolleranza in minuti da applicare alla media</param>
        /// <param name="averageBaseThreshold">Indica il numero di elemento minimo per considerare valida la media delle ore proposte.</param>
        /// <param name="averageCalculateList">Parametro utilizzato per far ritornare la lista di reg su cui in ultima istanza è stata calcolata la media.</param>
        /// <returns>L'ora media della registrazione proposta</returns>
        private static TimeSpan GetProposalsAverage(IEnumerable<Reg> averageRegsList, int averageTollerance, int averageBaseThreshold, out IEnumerable<Reg> averageCalculateList)
        {
            // inizializzazione del valore di ritorno del metodo
            TimeSpan averageHour = TimeSpan.Zero;

            // calcolo il timespan di tolleranza
            TimeSpan tolleranceTimeSpan = TimeSpan.FromMinutes(averageTollerance);

            // calcolo la media delle timbrature passate come parametro
            double secondsAverage = 0;
            if (averageRegsList.Any())
                secondsAverage = averageRegsList.Select(reg => reg.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.TotalSeconds).Average();


            // converto i secondi ottentuti di media in un timespan
            var timeSpanAverage = TimeSpan.FromSeconds(secondsAverage);

            // costruisco una nuova lista di registrazioni clonando la lista passata come parametro salvo la più distante dalla media e fuori tolleranza
            TimeSpan maxTollerance = TimeSpan.Zero;
            int regIdMaxOutOfThreshold = 0;
            averageRegsList.ForEach(reg =>
            {
                // calcolo la differenza tra la media e l'ora della registrazione
                TimeSpan span = TimeSpan.Zero;
                if (reg.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= timeSpanAverage)
                    span = reg.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Subtract(timeSpanAverage);
                else
                    span = timeSpanAverage.Subtract(reg.Registrazione_Data_Ora_Fis_Reg.TimeOfDay);

                // se la differenza dalla media è superiore alla tolleranza
                if (span > tolleranceTimeSpan && span > maxTollerance)
                {
                    maxTollerance = span;
                    regIdMaxOutOfThreshold = reg.Reg_Id;
                }

            });
            var newAverageList = averageRegsList.Where(reg => reg.Reg_Id != regIdMaxOutOfThreshold);

            // se il numero di registrazioni è cambiato allora ritorno il valore ricalcolando la media
            // a partire dalla lista con la registrazione mancante;
            // altrimenti ritorno la media calcolata in precedenza
            if (newAverageList.Count() != averageRegsList.Count())
                averageHour = GetProposalsAverage(newAverageList, averageTollerance, averageBaseThreshold, out averageCalculateList);
            else
            {
                averageHour = averageRegsList.Count() >= averageBaseThreshold ? timeSpanAverage : TimeSpan.Zero;
                averageCalculateList = averageRegsList;
            }

            // ritorno del valore del metodo
            return averageHour;
        }

        /// <summary>
        /// Ritorna l'ora proposta relativa al giorno rapporesentato dalle registrazioni ordinate che ha il delta tra le timbrature minore rispetto a quelle di controllo tra i giorni
        /// validi per la media.
        /// </summary>
        /// <param name="averageDayDictionary">Le timbrature dei giorni validi per il calcolo dell'ora proposta.</param>
        /// <param name="orderedSingleDayRegs">Le timbrature del giorno da completare.</param>
        /// <returns>L'ora proposta secondo l'algoritmo descritto in testata del metodo ProposeRegsBasedOnHistory.</returns>
        private static TimeSpan GetProposalClosestDayHour(Dictionary<DateTime, Tuple<List<Reg>, Reg>> averageDayDictionary, IList<Reg> orderedSingleDayRegs)
        {
            // inizializzazione del valore di ritorno del metodo
            TimeSpan proposedHour = TimeSpan.Zero;

            // per ogni giorno da controllare
            var minDelta = new KeyValuePair<DateTime, double>(DateTime.MinValue, double.MaxValue);
            foreach (var dayRegs in averageDayDictionary)
            {
                // recupero la lista delle registrazioni da controllare togliendo dall'elenco di reg
                // quella su cui è stata calcolata la media (la proposta per il calcolo della media)
                var regsToChek = dayRegs.Value.Item1.Where(reg => reg.Reg_Id != dayRegs.Value.Item2.Reg_Id).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                // a questo punto le registrazioni da controllare del singolo giorno e le registrazioni del giorno da chiudere sono sempre in numero uguale;
                // quindi posso permettermi di ciclare per indice su una sola collection
                double totalDayDelta = 0;
                for (int regIndex = 0; regIndex < regsToChek.Count(); regIndex++)
                {
                    totalDayDelta += Math.Abs(
                        regsToChek[regIndex].Registrazione_Data_Ora_Fis_Reg.TimeOfDay.TotalMinutes - orderedSingleDayRegs[regIndex].Registrazione_Data_Ora_Fis_Reg.TimeOfDay.TotalMinutes);
                }

                // se la somma dei delta del giorno in elaborazione è inferiore al delta totale più piccolo salvato precedentemente
                // allora il delta calcolato sopra è il nuovo delta minimo
                if (totalDayDelta < minDelta.Value)
                    minDelta = new KeyValuePair<DateTime, double>(dayRegs.Key, totalDayDelta);
            }

            // se è stato trovato un delta minino
            if (minDelta.Key != DateTime.MinValue)
                proposedHour = averageDayDictionary.FirstOrDefault(avg => avg.Key == minDelta.Key).Value.Item2.Registrazione_Data_Ora_Fis_Reg.TimeOfDay;

            // ritorno del valore del metodo
            return proposedHour;
        }

        #endregion

        #endregion

        #region GESTIONE COLORI

        //metodo che in base al tipo di modifica e alla presenza dell'entrata o uscita mi ritorna il colore corrispondente
        public static RegColorModifyEnum ColorModify(RegModifyTypeEnum modify, RegEUEnum regType)
        {
            //estraggo il valore numerico dell'enum
            int tipo = (int)modify;

            //estraggo il valore del parametro corrispondete all'entrata o all'uscita
            int tipoReg = (int)regType;


            //controllo se la mia cella è una cella di modifica
            if ((tipo == (int)RegModifyTypeEnum.Entry_Modified && tipoReg == 0) || (tipo == (int)RegModifyTypeEnum.Exit_Modified && tipoReg == 1) || (tipo == (int)RegModifyTypeEnum.Both) || (tipo == (int)RegModifyTypeEnum.Deelay && tipoReg == 0))
                return RegColorModifyEnum.Modify;
            //controllo se la cella è una cella di inserimento manuale
            if ((tipo == (int)RegModifyTypeEnum.Entry_Manual && tipoReg == 0) || (tipo == (int)RegModifyTypeEnum.Exit_Manual && tipoReg == 1) ||
                 (tipo == (int)RegModifyTypeEnum.Manual))
                return RegColorModifyEnum.Manual;
            //controllo se sono nel caso di entrata manaule e uscita modificata
            //entrata manuale       
            if (tipo == (int)RegModifyTypeEnum.E_Man_U_Mod && tipoReg == 0)
                return RegColorModifyEnum.Manual;
            //uscita modificata
            if (tipo == (int)RegModifyTypeEnum.E_Man_U_Mod && tipoReg == 1)
                return RegColorModifyEnum.Modify;
            //controllo se sono nel caso di entrata modificata e uscita manuale
            //caso di entrata modificata
            if (tipo == (int)RegModifyTypeEnum.E_Mod_U_Man && tipoReg == (int)RegEUEnum.Entry)
                return RegColorModifyEnum.Modify;
            //caso di uscita manuale
            if (tipo == (int)RegModifyTypeEnum.E_Mod_U_Man && tipoReg == (int)RegEUEnum.Exit)
                return RegColorModifyEnum.Manual;
            //altrimenti ritorno  il font nero
            return RegColorModifyEnum.None;

        }

        #endregion

        #region Gestione importazioni timbrature

        /// <summary>
        /// Calcola e restituisce l'elenco dei file reg da importare eventualmente presenti nella cartella di FilesInput.
        /// </summary>
        /// <param name="filesInputFolderName">Il nome della cartella in cui cercare i files da processare.</param>
        /// <returns>
        /// La lista dei file da importare presenti nella cartella
        /// </returns>
        public static List<string> CalcolaFilesRegDaImportare(string filesInputFolderName, out bool hasFilesToImport, bool importaSospese = true)
        {
            // iniziaizzazione della lista dei file nella cartella di input
            var filesInputNames = new List<string>();
            hasFilesToImport = false;
            // recupero l'elenco dei file da importare dalla cartella utilizzando il pattern specificato nei settings (solo se la cartella è presente)
            if (Directory.Exists(filesInputFolderName))
            {
                // aggiungo le registrazioni sospese
                if (importaSospese)
                    filesInputNames.AddRange(CalcolaFilesRegSospese(filesInputFolderName));

                var filesList = Directory.GetFiles(filesInputFolderName, String.Format("{0}*.txt", Settings.Default.RegFile)).OrderBy(fileName => fileName).ToList();
                if (filesList.Count > 0)
                {
                    hasFilesToImport = true;
                    filesInputNames.AddRange(filesList);
                }
            }

            return filesInputNames;

        }

        public static List<string> CalcolaFilesRegSospese(string filesInputFolderName)
        {
            // iniziaizzazione della lista dei file nella cartella di input
            var filesSuspendedNames = new List<string>();

            // recupero l'elenco dei file con registrazioni sospese dalla cartella utilizzando il pattern specificato nei settings (solo se la cartella è presente)
            if (Directory.Exists(filesInputFolderName))
            {
                // aggiunta all'array del file con le reg sospese (se presente) [viene impostata per prima così da avere un corretto ordinamento]
                string regSuspendedPattern = String.Format("{0}*.txt", Settings.Default.SuspendedRegsFile);
                string[] suspendedFiles = Directory.GetFiles(filesInputFolderName, regSuspendedPattern);

                filesSuspendedNames.AddRange(suspendedFiles.Where(File.Exists));
            }

            return filesSuspendedNames;
        }

        /// <summary>
        /// Recupera dall'elenco dei file da importare passata come parametro l'elenco delle timbrature non gps in essi contenute.
        /// Le timbrature restituite sono già epurate degli eventuali commenti contenuti nei files.
        /// </summary>
        /// <param name="filesToProcess">L'elenco dei files da processare.</param>
        /// <returns>
        /// L'elenco di timbrature non gps contenute nei files passati come parametri.
        /// Le timbrature restituite sono già epurate degli eventuali commenti contenuti nei files.
        /// </returns>
        public static List<string> GetRegsNoGpsFromFiles(IEnumerable<string> filesToProcess)
        {
            // inizializzazione del valore di ritorno del metodo
            var regToImport = new List<string>();

            if (filesToProcess != null)
            {
                // ciclo sui files da processare e recupero delle rimbrature in essa contenuta
                foreach (var fileToImport in filesToProcess)
                {
                    // viene ricontrollata la presenza del file
                    if (File.Exists(fileToImport))
                    {
                        // lettura di tutto il contenuto del file e posizionamento delle righe (non gps e commenti esclusi) nell'array nell'array
                        string[] fileRegs = File.ReadAllLines(fileToImport);

                        IEnumerable<string> filteredFileRegs = fileRegs.Where(regStr => !regStr.StartsWith("*") && !IsRegLineGps(regStr)).ToList();

                        if (filteredFileRegs.Any())
                            regToImport.AddRange(filteredFileRegs);
                    }
                }
            }
            // ritorno del valore calcolato dal metodo
            return regToImport;
        }

        /// <summary>
        /// Recupera dall'elenco dei file da importare passata come parametro l'elenco delle timbrature gps in essi contenute.
        /// Le timbrature restituite sono già epurate degli eventuali commenti contenuti nei files.
        /// </summary>
        /// <param name="filesToProcess">L'elenco dei files da processare.</param>
        /// <returns>L'elenco di timbrature gps contenute nei files passati come parametri.
        /// Le timbrature restituite sono già epurate degli eventuali commenti contenuti nei files.</returns>
        public static List<string> GetRegsGpsFromFiles(IEnumerable<string> filesToProcess)
        {
            // inizializzazione del valore di ritorno del metodo
            var regToImport = new List<string>();
            List<KeyValuePair<String, String>> GPS_Duplicates = new List<KeyValuePair<string, string>>();

            if (filesToProcess != null)
            {
                // ciclo sui files da processare e recupero delle rimbrature in essa contenuta
                foreach (var fileToImport in filesToProcess)
                {
                    // viene ricontrollata la presenza del file
                    if (File.Exists(fileToImport))
                    {
                        // lettura di tutto il contenuto del file e posizionamento delle righe (non gps e commenti esclusi) nell'array nell'array
                        string[] fileRegs = File.ReadAllLines(fileToImport);
                        string[] fileRegsDistinct = File.ReadAllLines(fileToImport).Distinct().ToArray();

                        List<string> filteredFileRegs = fileRegs.Where(regStr => !regStr.StartsWith("*") && IsRegLineGps(regStr) && !regStr.StartsWith("8") && !regStr.StartsWith("7")).ToList();
                        List<string> filteredAppRegs = fileRegs.Where(regStr => !regStr.StartsWith("*") && IsRegLineGps(regStr) && (regStr.StartsWith("8") || regStr.StartsWith("7"))).ToList();
                        if (filteredFileRegs.Any())
                        {

                            #region Filtraggio righe duplicate causa errore firmware

                            if (filteredFileRegs.Count() > 3)
                            {
                                for (int i = 3; i < filteredFileRegs.Count(); i++)
                                {
                                    if (filteredFileRegs.ElementAt(i) == filteredFileRegs.ElementAt(i - 2))
                                    {
                                        GPS_Duplicates.Add(new KeyValuePair<string, string>(String.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_LINEA_GPS_X_DUPLICATA)),
                                            filteredFileRegs[i]));

                                        filteredFileRegs[i] = " ";
                                    }
                                }
                            }

                            #endregion

                            regToImport.AddRange(filteredFileRegs.Where(regStr => regStr != " ").ToList());
                        }
                        if(filteredAppRegs.Any()){
                            regToImport.AddRange(filteredAppRegs.Where(regStr => regStr != " ").ToList());
                        }
                    }
                }

                #region Creazione file reg. duplicate

                if (GPS_Duplicates.Count > 0)
                {
                    string duplicateGpsLines = HttpContext.Current.Server.MapPath(Path.Combine(Common.Properties.Settings.Default.Files_Input_Path + "Reg_GPS_Errate" + "\\", String.Format("{0}_{1}.txt", "Registrazioni GPS duplicate", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"))));
                    if (!Directory.Exists(HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path + "Reg_GPS_Errate" + "\\")))
                    {
                        Directory.CreateDirectory(HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path + "Reg_GPS_Errate" + "\\"));
                    }
                    else
                    {
                        BusinessService.CreateSuspendedRegFile(duplicateGpsLines, GPS_Duplicates);
                    }
                }

                #endregion
            }

            // ritorno del valore calcolato dal metodo
            return regToImport;
        }

        /// <summary>
        /// Determina se la riga contenente i dati della registrazione specifica in pattern gps o meno.
        /// </summary>
        /// <param name="regLine">La linea di registrazione da processare.</param>
        /// <returns><c>true</c> se la riga contenete i dati della registrazione è in formato gps; altrimenti false</returns>
        public static bool IsRegLineGps(string regLine)
        {
            string infoAgg = string.Empty;

            string[] splittedLine = regLine.Split(';');

            // la linea risulta in formato gps se il numero di elementi in essa contenuta separati da punto e virgola è superiore a quanto
            // configurato per il pattern della registrazione standard e non è una linea di info aggiuntive
            return (splittedLine.Count() > 10 && !IsRegLineAdditionalInfoGps(splittedLine));
        }

        /// <summary>
        /// Determina se la linea contenente i dati di registrazione risulta contenere informazioni aggiuntive.
        /// </summary>
        /// <param name="splittedRegLine">L'array contenente i dati della riga di timbratura.</param>
        /// <returns><c>true</c> se la linea specificata contiene informazioni aggiuntive; altrimenti false.</returns>
        public static bool IsRegLineAdditionalInfo(string[] splittedRegLine)
        {
            bool returnValue = false;

            string infoAgg = String.Empty;

            //si controlla se la stringa nella posizione specifica è uguale alla keyword che indica che nella registrazione vi sono informazionia aggiuntive
            if (splittedRegLine.Count() > 9)
            {
                infoAgg = splittedRegLine[8];
                returnValue = infoAgg == "INFOAGG";
            }

            return returnValue;
        }

        /// <summary>
        /// Determina se la linea contenente i dati di registrazione risulta contenere informazioni aggiuntive.
        /// </summary>
        /// <param name="splittedRegLine">L'array contenente i dati della riga di timbratura.</param>
        /// <returns><c>true</c> se la linea specificata contiene informazioni aggiuntive; altrimenti false.</returns>
        public static bool IsRegLineAdditionalInfoGps(string[] splittedRegLine)
        {
            bool returnValue = false;

            string infoAgg = String.Empty;

            //si controlla se la stringa nella posizione specifica è uguale alla keyword che indica che nella registrazione vi sono informazionia aggiuntive
            if (splittedRegLine.Count() > 9)
            {
                string codiceAdditional = splittedRegLine[1];
                if (codiceAdditional != "NOTE000002") {
                    infoAgg = splittedRegLine[8];
                    returnValue = infoAgg == "INFOAGG";
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Calcola e restituisce il tipo di informazione aggiuntiva riguardo i dati di una specifica registrazione.
        /// </summary>
        /// <param name="splittedRegLine">L'array contenente i dati della riga di timbratura.</param>
        /// <returns>Il tipo di informazione aggiuntiva riguardo i dati della registrazione specificata.</returns>
        public static AdditionalInfoEnum AdditionalInfoType(string[] splittedRegLine)
        {
            AdditionalInfoEnum returnValue = AdditionalInfoEnum.None;

            if (IsRegLineAdditionalInfo(splittedRegLine))
                switch (splittedRegLine[9])
                {
                    case "TURNO":
                        returnValue = AdditionalInfoEnum.Turn;
                        break;
                    case "SUBCANT":
                        returnValue = AdditionalInfoEnum.SubCant;
                        break;
                    case "TIPOATT":
                        returnValue = AdditionalInfoEnum.ActivityType;
                        break;
                    case "PRUCODEFORACTIVITY":
                        returnValue = AdditionalInfoEnum.PruCodeForActivity;
                        break;
                    case "SQUADRA":
                        returnValue = AdditionalInfoEnum.Squadra;
                        break;
                    case "NOTE":
                        returnValue = AdditionalInfoEnum.Note;
                        break;
                }

            return returnValue;
        }

        /// <summary>
        /// Recupera il valore di informazione aggiuntiva dalla specifica registrazione.
        /// </summary>
        /// <param name="splittedRegLine">L'array contenente i dati della riga di timbratura.</param>
        /// <returns>Il valore di informazione aggiuntiva della registrazione specificata. <see cref="null"/> in caso 
        /// la registrazione non contenga informazioni aggiuntive.</returns>
        public static string GetAdditionalInfoValue(string[] splittedRegLine)
        {
            string returnValue = null;

            if (IsRegLineAdditionalInfo(splittedRegLine))
                returnValue = splittedRegLine.Count() > 10 ? splittedRegLine[10] : null;

            return returnValue;
        }

        /// <summary>
        /// Recupera il valore di informazione aggiuntiva dalla squadra.
        /// </summary>
        /// <param name="splittedRegLine">L'array contenente i dati della riga di timbratura.</param>
        /// <returns>L'array con le PRU dei componenti della squadra. <see cref="null"/> in caso 
        /// la registrazione non contenga informazioni aggiuntive.</returns>
        public static string[] GetAdditionalInfoValueSquadra(string[] splittedRegLine)
        {
            string[] returnValue = null;

            if (AdditionalInfoType(splittedRegLine) == AdditionalInfoEnum.Squadra)
                returnValue = splittedRegLine.Count() > 10 ? splittedRegLine.Skip(10).ToArray() : null;

            return returnValue;
        }

        /// <summary>
        /// Effettua il backup dei files processati nell'import nella cartella di destinazione.
        /// </summary>
        /// <param name="processedFiles">L'elenco dei files processati da backuppare</param>
        /// <param name="backupFolderPath">La cartella in cui effettuare il backuo dei files.</param>
        public static void BackupProcessedFiles(IEnumerable<string> processedFiles, string backupFolderPath)
        {
            // se non esiste la cartella di backup allora la creo
            if (!Directory.Exists(backupFolderPath))
                Directory.CreateDirectory(backupFolderPath);

            if (processedFiles != null)
            {
                // ciclo di elaborazione dei file in lista
                foreach (string filePath in processedFiles)
                {
                    // calcolo del percorso completo del file nella cartella di backup
                    string backupFilePath = Path.Combine(backupFolderPath, Path.GetFileName(filePath));

                    // se esiste già un file con lo stesso nome nella cartella di backup allora lo cancello
                    if (File.Exists(backupFilePath))
                        File.Delete(backupFilePath);

                    // spostamento del file processato nella cartella di backup
                    File.Move(filePath, backupFilePath);
                }
            }
        }

        public static void BackUpJsonObject(object obj, string path)
        {
            Type objType = obj.GetType();

            JContainer json;

            //Composizione percorso file
            string completePath = HttpContext.Current.Server.MapPath(path);

            DateTime now = DateTime.Now;

            completePath += $"TmpJsonDownload-{now.Year}-{now.Month}-{now.Day}-{now.Hour}-{now.Minute}-{now.Second}.txt";

            if (!Directory.Exists(HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path)))
                Directory.CreateDirectory(HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path));

            if (typeof(IEnumerable).IsAssignableFrom(objType))
            {
                json = JArray.FromObject(obj);
            }
            else
            {
                json = JObject.FromObject(obj);
            }


            File.WriteAllLines(completePath, new string[] { json.ToString() });


        }

        public static void SendExpiredUsersEmail(IEnumerable<Utenti> expiredUsers, IEnumerable<Utenti> expiringUsers)
        {
            int? globalExpiration = RepoManager.ParamRepo.ParametersRow.PasswordExpirationDays;

            StringBuilder body = new StringBuilder("<div style=\"font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; \">");

            if (expiredUsers.Any())
            {

                body.Append("<h3>Elenco utenti scaduti in data odierna:</h3>");

                body.Append("<table cellspacing=\"20\" style=\"width: 100 %\">");
                body.Append("<tr>");
                body.Append("<th>Codice utente</th>");
                body.Append("<th>Data concessione ultima password</th>");
                body.Append("<th>Durata concessa</th>");
                body.Append("</tr>");

                expiredUsers.ForEach(user =>
                {
                    body.Append("<tr>");
                    body.Append(string.Format("<td>{0}</td><td>{1}</td><td>{2}</td>", user.Codice_Utente, user.DataUltimoAgg_Psw_Utente, user.N_GG_Val_Psw_Utente ?? globalExpiration));
                    body.Append("</tr>");
                });

                body.Append("</table>");

                body.Append("</div>");
            }

            if (expiringUsers.Any())
            {
                body.Append("<h3>Elenco utenti in scadenza in data odierna:</h3>");

                body.Append("<table cellspacing=\"20\" style=\"width: 100 %\">");
                body.Append("<tr>");
                body.Append("<th>Codice utente</th>");
                body.Append("<th>Data concessione ultima password</th>");
                body.Append("<th>Durata concessa</th>");
                body.Append("</tr>");

                expiringUsers.ForEach(user =>
                {
                    body.Append("<tr>");
                    body.Append(string.Format("<td>{0}</td><td>{1}</td><td>{2}</td>", user.Codice_Utente, user.DataUltimoAgg_Psw_Utente, user.N_GG_Val_Psw_Utente ?? globalExpiration));
                    body.Append("</tr>");
                });

                body.Append("</table>");

                body.Append("</div>");
            }



            CommonService.sendMail(RepoManager.ParamRepo.ParametersRow.CompanyEmail, "PowerWeb - Comunicazione password utenti scadute " + DateTime.Now.ToString("d MMMM yyyy"), body.ToString(), "newsletter@winit.it", "PowerWeb - Comunicazione password utenti scadute", new string[] { });

        }

        /// <summary>
        /// Crea il file delle sospese al percorso file specificato utilizzando gli errori passati come parametro.
        /// </summary>
        /// <param name="regSuspendedFilePath">Il percorso file in cui salvare le registrazioni sospese.</param>
        /// <param name="importErrors">L'elenco degli errori di importazione da salvare</param>
        public static void CreateSuspendedRegFile(string regSuspendedFilePath, List<KeyValuePair<string, string>> importErrors)
        {
            // creazione delle reg sospese (se presenti da scrivere)
            if (importErrors.Any(err => !err.Key.StartsWith("**")))
                using (var sw = new StreamWriter(regSuspendedFilePath, false))
                {
                    try
                    {
                        foreach (KeyValuePair<String, String> error in importErrors.Where(err => !err.Key.StartsWith("**")))
                        {
                            sw.WriteLine(error.Key);
                            sw.WriteLine(error.Value);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        if (sw != null)
                        {
                            sw.Close();
                            sw.Dispose();
                        }
                    }

                }
        }

        #endregion

        /// <summary>
        /// Gestisce lo scostamento della data/ora in base alla configurazione del notturno per le date di inizio e fine periodo specifiche.
        /// </summary>
        /// <param name="startDate">La data di inizio del periodo.</param>
        /// <param name="endDate">La data di fine del periodo.</param>
        public static void ManageNocturneStartEndDate(ref DateTime startDate, ref DateTime endDate)
        {
            // se è abilitata la gestione del notturno
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno)
            {
                // Nel caso di notturno abilitato le date diventano:
                // - La mezzanote del giorno precedente
                // - La mezzanotte dei due giorni successivi
                // questo così da coprire il giorno antecedente e successivo all'inizio e alla fine del periodo
                if (((NocturneTypeEnum)RepoManager.ParamRepo.ParametersRow.TipoNotturno != NocturneTypeEnum.None) && ((NocturneTypeEnum)RepoManager.ParamRepo.ParametersRow.TipoNotturno != NocturneTypeEnum.Disabled))
                {
                    startDate = startDate.AddDays(-1);
                    endDate = endDate.AddDays(2);
                }
            }
        }

        /// <summary>
        /// Restituisce le date per effettuare ricerche in caso di notturno
        /// </summary>
        /// <param name="currentReg">The current reg.</param>
        /// <returns></returns>
        public static Tuple<DateTime, DateTime> dateToSearchWithNocturn(Reg currentReg)
        {
            //viene istanziata la tupla contenete le due date di inzio e fine
            Tuple<DateTime, DateTime> dateToSearch = null;

            //istanzia nuove date
            DateTime fromSearch = new DateTime();
            DateTime toSearch = new DateTime();
            DateTime nocturnBoundMaxDayBefore = new DateTime();
            DateTime nocturnBoundMaxDayAfter = new DateTime();

            DateTime today = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Date.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Date.Month, currentReg.Registrazione_Data_Ora_Fis_Reg.Date.Day);
            DateTime yestarday = today.AddDays(-1);
            DateTime tomorrow = today.AddDays(1);

            //vengono estratte tutte le registrazioni 
            List<Reg> regDay = RepoManager.RegRepo.Find(reg => reg.Col_Id == currentReg.Col_Id && (reg.Registrazione_Data_Ora_Fis_Reg.Year == today.Year && reg.Registrazione_Data_Ora_Fis_Reg.Month == today.Month && reg.Registrazione_Data_Ora_Fis_Reg.Day == today.Day)).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();
            List<Reg> regDayBefore = RepoManager.RegRepo.Find(reg => reg.Col_Id == currentReg.Col_Id && (reg.Registrazione_Data_Ora_Fis_Reg.Year == yestarday.Year && reg.Registrazione_Data_Ora_Fis_Reg.Month == yestarday.Month && reg.Registrazione_Data_Ora_Fis_Reg.Day == yestarday.Day)).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();
            List<Reg> regDayAfter = RepoManager.RegRepo.Find(reg => reg.Col_Id == currentReg.Col_Id && (reg.Registrazione_Data_Ora_Fis_Reg.Year == tomorrow.Year && reg.Registrazione_Data_Ora_Fis_Reg.Month == tomorrow.Month && reg.Registrazione_Data_Ora_Fis_Reg.Day == tomorrow.Day)).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

            Reg firstDayReg = regDay.First();
            Reg lastDayReg = regDay.Last();

            //inizilizzazione della durata del notturno
            TimeSpan nocturneDuration = TimeSpan.Zero;
            TimeSpan colNocturnDuration = TimeSpan.Zero;


            //viene recuperata la soglia dai parametri
            nocturneDuration = RepoManager.ParamRepo.ParametersRow.Durata_Notturno ?? TimeSpan.Zero;

            //viene recuperata la soglia dal collaboratore
            colNocturnDuration = RepoManager.ColRepo.Single(col => col.Col_Id == currentReg.Col_Id, true).Durata_Notturno_Col ?? TimeSpan.Zero;

            //se vi è la soglia del collaboratore
            if (colNocturnDuration.Ticks > 0)
                nocturneDuration = colNocturnDuration;

            if (regDayBefore.Any() && nocturneDuration != null)
            {
                //viene estrattal'ultima registrazione della giornata
                Reg lastDayDeforeReg = regDayBefore.Last();

                //viene sommata all'ultima registrazione la durata massima del notturno per verificare che la registrazione successiva ricada nel range
                nocturnBoundMaxDayBefore = lastDayDeforeReg.Registrazione_Data_Ora_Fis_Reg.AddMinutes(nocturneDuration.TotalMinutes);

                //se il limite è superiore della prima timbratura della gioranta allora come inizio si prende il limite xche la prima timbratura appartiene al giorno prima
                if (nocturneDuration.TotalMinutes < 720 && nocturnBoundMaxDayBefore >= firstDayReg.Registrazione_Data_Ora_Fis_Reg)
                {
                    fromSearch = nocturnBoundMaxDayBefore;
                }
                //altrimenti  la data da ricercare è data dalla prima timbratura del giorno
                else
                    fromSearch = firstDayReg.Registrazione_Data_Ora_Fis_Reg;
            }
            else
                fromSearch = currentReg.Registrazione_Data_Ora_Fis_Reg.Date;


            //se vi sono delle registrazioni il giorno successivo
            if (regDayAfter.Any())
            {
                //prima registrazione del giorno dopo
                Reg firstDayAfterReg = regDayAfter.First();

                //viene calcolato il limite dl notturno
                nocturnBoundMaxDayAfter = lastDayReg.Registrazione_Data_Ora_Fis_Reg.AddMinutes(nocturneDuration.TotalMinutes);

                //se il limite è maggiore della prima registrazione allora la registrazione non appartiene a day after ma a day
                if (nocturnBoundMaxDayAfter >= firstDayAfterReg.Registrazione_Data_Ora_Fis_Reg)
                {
                    toSearch = nocturnBoundMaxDayAfter;
                }
                //se il limite è minore allora viene presa la data ora della prima registrazione del giorno successivo
                else if (nocturnBoundMaxDayAfter > firstDayReg.Registrazione_Data_Ora_Fis_Reg.Date)
                    toSearch = firstDayAfterReg.Registrazione_Data_Ora_Fis_Reg;
                else
                    toSearch = new DateTime(firstDayReg.Registrazione_Data_Ora_Fis_Reg.Year, firstDayReg.Registrazione_Data_Ora_Fis_Reg.Month, firstDayReg.Registrazione_Data_Ora_Fis_Reg.Day, 23, 59, 0);
            }
            //se non vi sono registrazioni il giorno successivo  la fine viene impostata 23 59 del giorno 
            else
                toSearch = new DateTime(firstDayReg.Registrazione_Data_Ora_Fis_Reg.Year, firstDayReg.Registrazione_Data_Ora_Fis_Reg.Month, firstDayReg.Registrazione_Data_Ora_Fis_Reg.Day, 23, 59, 0);

            dateToSearch = new Tuple<DateTime, DateTime>(fromSearch, toSearch);

            return dateToSearch;
        }



        #region SINCRONIZZAZIONE CLOCKAPP


        /// <summary>
        /// Sincronizza le entità in clockAppsManager in seguito ad operazioni di aggiunta/modifica/cancellazione
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="state"></param>
        /// <param name="items"></param>
        public static void ClockAppsAddEntities<T>(IEnumerable<T> items) where T : class
        {

            var customerId = RepoManager.ParamRepo.ParametersRow.Codice_Cliente;

            var clockAppsConnectionUrl = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager;

            if (String.IsNullOrEmpty(customerId) && !String.IsNullOrEmpty(clockAppsConnectionUrl))
            {
                _log.Error("Parametro CUSTOMER ID o URL CLOCKAPP mancanti");
                return;
            }

            _log.DebugFormat("Richiesta sincronizzazione di tipo {0} per il customer {1} per l 'entità {2}", EntityState.Added.ToString(), customerId, typeof(T).Name);

            string apiPath = "";//DictionaryEntityControllers.GetControllerPath(typeof(T));

            if (String.IsNullOrEmpty(apiPath))
            {
                _log.ErrorFormat("Errore path controller mancante per sincronizzazione clockapp per il tipo {0}", typeof(T).Name);
                return;
            }

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ClockAppConvertCantToActivity) == 1)
            {
                _log.InfoFormat("Customization {0} attiva", CustomizationEnum.ClockAppConvertCantToActivity.ToString());

                apiPath = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.ClockAppConvertCantToActivity, "ApiString");
            }

            _log.InfoFormat("Stringa API clockAppsManager = {0}", apiPath);

            JObject request = new JObject();

            JArray toSend = typeof(T).GetMethod("Synchronize").Invoke(null, new object[] { items }) as JArray;


            request.Add("CustomerCode", customerId);
            request.Add("Entities", toSend);
            request.Add("Op", EntityState.Added.ToString());

            var connectionHub = new ClockAppsSyncHub();
            connectionHub.Path = apiPath;
            connectionHub.ConnectionUrl = clockAppsConnectionUrl.Split(':')[0];
            connectionHub.Port = int.Parse(clockAppsConnectionUrl.Split(':')[1]);

            connectionHub.Set(request);
            connectionHub.ExecutePost();


        }

        /// <summary>
        /// Sincronizza i cantieri (items) ai quali è stata assegnata/modificata/cancellata una fru 
        /// su clockAppsManager agendo sul campo CantMatr (ClockAppsManager/Cants)
        /// </summary>
        /// <param name="state"></param>
        /// <param name="items"></param>
        public static void SynchronizeFruCant(EntityState state, IEnumerable<Fru_Cant> items)
        {
            var customerId = RepoManager.ParamRepo.ParametersRow.Codice_Cliente;

            var clockAppsConnectionUrl = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager;

            if (String.IsNullOrEmpty(customerId) && !String.IsNullOrEmpty(clockAppsConnectionUrl))
            {
                _log.Warn("Parametro CUSTOMER ID o URL CLOCKAPP mancanti");
                return;
            }

            string apiPath = "//api/ManagePowerWebFru/";

            JObject request = new JObject();

            JArray toSend = JArray.FromObject(items.Select(fru_Cant => new
            {
                PowerWebCantId = fru_Cant.Cant_Id,
                CantMatr = (state == EntityState.Deleted) ? "NA" : fru_Cant.Codice_Fru.Trim()
            }));


            request.Add("CustomerCode", customerId);
            request.Add("Entities", toSend);
            request.Add("Op", state.ToString());

            var connectionHub = new ClockAppsSyncHub();
            connectionHub.Path = apiPath;
            connectionHub.ConnectionUrl = clockAppsConnectionUrl.Split(':')[0];
            connectionHub.Port = int.Parse(clockAppsConnectionUrl.Split(':')[1]);

            connectionHub.Set(request);
            connectionHub.ExecutePost();
        }



        /// <summary>
        /// Sincronizza il database di PowerWeb con il database di ClockAppsManager dell'entità generica 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        public static void SynchronizeAll<TEntity>() where TEntity : class
        {
            //TO-DO terminare implementazione

            _log.InfoFormat("Inizio sincronizzazione totale entità {0}", typeof(TEntity).Name);

            var customerId = RepoManager.ParamRepo.ParametersRow.Codice_Cliente;

            var clockAppsConnectionUrl = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager;

            if (String.IsNullOrEmpty(customerId) || String.IsNullOrEmpty(clockAppsConnectionUrl))
            {
                _log.Warn("Parametro CUSTOMER ID o URL CLOCKAPP mancanti");
                _log.InfoFormat("Sincronizzazione totale entità {0} per errori", typeof(TEntity).Name);
                return;
            }

            var apiPath = "//Api/MergeEntities";

            var dbSet = RepoManager.CantRepo.Context.Set<TEntity>();

            int take = 100;
            int skip = 0;

            int dbCount = dbSet.Count();

            _log.DebugFormat("Tabella di tipo {0} contenente {0} record", typeof(TEntity).Name, dbCount);

            JArray items = null;

            JObject request = new JObject();

            //Viene segnalato il primo blocco
            request.Add("isFirstToken", true);
            request.Add("Type", typeof(TEntity).Name);

            var connectionHub = new ClockAppsSyncHub();

            connectionHub.Path = apiPath;
            connectionHub.ConnectionUrl = clockAppsConnectionUrl.Split(':')[0];
            connectionHub.Port = int.Parse(clockAppsConnectionUrl.Split(':')[1]);

            //Estrazione della chiave primaria della tabella 

            ObjectContext objectContext = ((IObjectContextAdapter)RepoManager.CantRepo.Context).ObjectContext;
            ObjectSet<TEntity> set = objectContext.CreateObjectSet<TEntity>();
            string primaryKeyName = set.EntitySet.ElementType.KeyMembers.First().Name;

            IEnumerable<TEntity> entities = null;

            while (!(skip >= dbCount))
            {


                entities = dbSet.OrderBy(primaryKeyName + " ASC").Skip(() => skip).Take(() => take).AsEnumerable();
                _log.DebugFormat("Inizio serializzazione delle prime {0} entità", entities.Count());

                items = JArray.FromObject((typeof(TEntity).GetMethod("Synchronize").Invoke(null, new object[] { EntityState.Added, entities.ToList() })) as JArray);

                request.Add("entities", items);
                request.Add("count", skip + take);

                request.Add("customerCode", customerId);

                skip += take;

                //Se è l'ultimo blocco viene segnalato
                if (skip >= dbCount)
                {
                    request.Add("isLastToken", true);
                }

                connectionHub.Set(request);
                connectionHub.ExecuteTotalSync();

                request = new JObject();

            }

            request.Add("CustomerCode", customerId);

            connectionHub.Set(request);
            connectionHub.ConfirmSync();


        }

        public static void ClockAppUpdateValues<T>(IEnumerable<IDictionary<string, object>> values)
        {

            var customerId = RepoManager.ParamRepo.ParametersRow.Codice_Cliente;

            var clockAppsConnectionUrl = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager;

            if (String.IsNullOrEmpty(customerId) && !String.IsNullOrEmpty(clockAppsConnectionUrl))
            {
                _log.Warn("Parametro CUSTOMER ID o URL CLOCKAPP mancanti");
                return;
            }

            _log.DebugFormat("Richiesta sincronizzazione di tipo {0} per il customer {1} per l 'entità {2}", EntityState.Modified.ToString(), customerId, typeof(T).Name);

            string apiPath = "";// DictionaryEntityControllers.GetControllerPath(typeof(T));

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ClockAppConvertCantToActivity) == 1)
            {
                _log.InfoFormat("Customization {0} attiva", CustomizationEnum.ClockAppConvertCantToActivity.ToString());

                apiPath = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.ClockAppConvertCantToActivity, "ApiString");
            }

            _log.InfoFormat("Stringa API clockAppsManager = {0}", apiPath);

            JObject request = new JObject();

            request.Add("CustomerCode", customerId);
            request.Add("Entities", JArray.FromObject(values));
            request.Add("Op", EntityState.Modified.ToString());

            var connectionHub = new ClockAppsSyncHub();
            connectionHub.Path = apiPath;
            connectionHub.ConnectionUrl = clockAppsConnectionUrl.Split(':')[0];
            connectionHub.Port = int.Parse(clockAppsConnectionUrl.Split(':')[1]);

            connectionHub.Set(request);
            connectionHub.ExecutePost();


        }
        public static void ClockAppUpdateValues<T>(IEnumerable<T> entities)
        {

            var customerId = RepoManager.ParamRepo.ParametersRow.Codice_Cliente;

            var clockAppsConnectionUrl = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager;

            if (String.IsNullOrEmpty(customerId) && !String.IsNullOrEmpty(clockAppsConnectionUrl))
            {
                _log.Warn("Parametro CUSTOMER ID o URL CLOCKAPP mancanti");
                return;
            }

            _log.DebugFormat("Richiesta sincronizzazione di tipo {0} per il customer {1} per l 'entità {2}", EntityState.Modified.ToString(), customerId, typeof(T).Name);

            string apiPath = "";// DictionaryEntityControllers.GetControllerPath(typeof(T));

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ClockAppConvertCantToActivity) == 1)
            {
                _log.InfoFormat("Customization {0} attiva", CustomizationEnum.ClockAppConvertCantToActivity.ToString());

                apiPath = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.ClockAppConvertCantToActivity, "ApiString");
            }

            _log.InfoFormat("Stringa API clockAppsManager = {0}", apiPath);

            JObject request = new JObject();

            request.Add("CustomerCode", customerId);
            request.Add("Entities", JArray.FromObject(entities));
            request.Add("Op", EntityState.Modified.ToString());

            var connectionHub = new ClockAppsSyncHub();
            connectionHub.Path = apiPath;
            connectionHub.ConnectionUrl = clockAppsConnectionUrl.Split(':')[0];
            connectionHub.Port = int.Parse(clockAppsConnectionUrl.Split(':')[1]);

            connectionHub.Set(request);
            connectionHub.ExecutePost();


        }
        public static void ClockAppDeleteEntities<T>(IEnumerable<int> ids)
        {
            var customerId = RepoManager.ParamRepo.ParametersRow.Codice_Cliente;

            var clockAppsConnectionUrl = RepoManager.ParamRepo.ParametersRow.Url_ClockAppsManager;

            if (String.IsNullOrEmpty(customerId) && !String.IsNullOrEmpty(clockAppsConnectionUrl))
            {
                _log.Warn("Parametro CUSTOMER ID o URL CLOCKAPP mancanti");
                return;
            }

            _log.DebugFormat("Richiesta sincronizzazione di tipo {0} per il customer {1} per l 'entità {2}", EntityState.Deleted.ToString(), customerId, typeof(T).Name);

            string apiPath = ""; // DictionaryEntityControllers.GetControllerPath(typeof(T));

            _log.InfoFormat("Stringa API clockAppsManager = {0}", apiPath);

            JObject request = new JObject();

            request.Add("CustomerCode", customerId);
            request.Add("Ids", JArray.FromObject(ids));
            request.Add("Op", EntityState.Deleted.ToString());

            var connectionHub = new ClockAppsSyncHub();
            connectionHub.Path = apiPath;
            connectionHub.ConnectionUrl = clockAppsConnectionUrl.Split(':')[0];
            connectionHub.Port = int.Parse(clockAppsConnectionUrl.Split(':')[1]);

            connectionHub.Set(request);
            connectionHub.ExecutePost();
        }

        #endregion

    }

    #region Gestione Codice Fiscale
    internal class CodFiscale
    {
        static internal char[] m_cArrayValoriMesi = { 'A', 'B', 'C', 'D', 'E', 'H', 'L', 'M', 'P', 'R', 'S', 'T' };
        static internal int[] m_iArrayValoriPosizioniPari = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
      16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
        static internal int[] m_iArrayValoriPosizioniDispari = { 1, 0, 5, 7, 9, 13, 15, 17, 19, 21, 2, 4, 18, 20, 11,
      3, 6, 8, 12, 14, 16, 10, 22, 25, 24, 23 };
        static internal string m_sConsonanti = "[BCDFGHJKLMNPQRSTVWXYZ]";
        static internal string m_sVocali = "[AEIOU]";

        static internal string ElaboraDataNascitaESesso(DateTime dtmDataNascita, string sSesso)
        {
            string sVal = string.Format("{0:yy}", dtmDataNascita)
              + m_cArrayValoriMesi[dtmDataNascita.Month - 1]
              + (dtmDataNascita.Day + (sSesso == "F" ? 40 : 0)).ToString("D2");
            return sVal;
        }

        static internal string ElaboraNome(string sNome)
        {
            string sVal = "";
            int i = 0;
            MatchCollection oMatchCollection = Regex.Matches(sNome, m_sConsonanti);
            while (sVal.Length < 3 && i < oMatchCollection.Count)
            {
                if (!(i == 1 && oMatchCollection.Count >= 4))
                    sVal += oMatchCollection[i].Value;
                i++;
            }
            if (sVal.Length == 3) return sVal;
            i = 0;
            oMatchCollection = Regex.Matches(sNome, m_sVocali);
            while (sVal.Length < 3 && i < oMatchCollection.Count)
            {
                sVal += oMatchCollection[i].Value;
                i++;
            }
            if (sVal.Length == 3) return sVal;
            while (sVal.Length < 3)
            {
                sVal += "X";
            }
            return sVal;
        }

        static internal string ElaboraCognome(string sCognome)
        {
            string sVal = "";
            int i = 0;
            MatchCollection oMatchCollection = Regex.Matches(sCognome, m_sConsonanti);
            while (sVal.Length < 3 && i < oMatchCollection.Count)
            {
                sVal += oMatchCollection[i].Value;
                i++;
            }
            if (sVal.Length == 3) return sVal;
            i = 0;
            oMatchCollection = Regex.Matches(sCognome, m_sVocali);
            while (sVal.Length < 3 && i < oMatchCollection.Count)
            {
                sVal += oMatchCollection[i].Value;
                i++;
            }
            if (sVal.Length == 3) return sVal;
            while (sVal.Length < 3)
            {
                sVal += "X";
            }
            return sVal;
        }

        static internal string ElaboraCodiceComune(string sComune, string sProvincia)
        {
            Tab_Comuni oRecord = RepoManager.Tab_ComuniRepo.DbSet.Any(x => x.Luogo_Tab_Comuni == sComune) ? RepoManager.Tab_ComuniRepo.Find(x => x.Luogo_Tab_Comuni == sComune).FirstOrDefault() : null;
            if (oRecord == null)
                return "";
            else
                return oRecord.Codice_Luogo_Tab_Comuni.ToUpper();
        }

        static internal string CalcolaUltimaLettera(string sCodiceFiscale)
        {
            int iSommatoria = 0;
            int iChar = 0;
            char cChar;
            for (int i = 0; i < sCodiceFiscale.Length; i++)
            {
                iChar = sCodiceFiscale[i] - ((sCodiceFiscale[i] >= '0') && (sCodiceFiscale[i] <= '9') ? '0' : 'A');
                if (((i + 1) % 2) == 0)
                    iSommatoria += m_iArrayValoriPosizioniPari[iChar];
                else
                    iSommatoria += m_iArrayValoriPosizioniDispari[iChar];
            }
            cChar = (char)((iSommatoria % 26) + 'A');
            return cChar.ToString();
        }

        static internal string FormattaStringa(string sStringa)
        {
            return CommonService.TogliPrincipaliAccentiAlleVocaliNellaStringa(sStringa).ToUpper().Replace("'", "");
        }
    }
    #endregion

    #region Gestione del raggio GPS

    /// <summary>
    /// Classe utilizzata per rappresentare un raggio GPS rispetto a un punto centrale
    /// </summary>
    public class GpsRange
    {
        //TODO: ora dati gestiti come terra sferica; in realtà da aggiornare con gestione elissoide

        #region Private Constants

        /// <summary>
        /// Il raggio della terra espresso in kilometri
        /// </summary>
        private const double EarthRadiiusKm = 6371d;

        #endregion

        #region Private Fields

        private readonly int _metersRange;

        #endregion

        #region Constructors

        /// <summary>
        /// Inizializza una nuova istanza della classe di tipo <see cref="GpsRange"/>.
        /// </summary>
        /// <param name="centralPointLatitude">La latitudine del punto centrale.</param>
        /// <param name="centralPointLongitude">La longitudine del punto centrale.</param>
        /// <param name="metersRange">Il numero di metri del raggio GPS.</param>
        public GpsRange(double centralPointLatitude, double centralPointLongitude, int metersRange)
        {
            GpsRangeCenterLatitude = centralPointLatitude;
            GpsRangeCenterLongitude = centralPointLongitude;
            _metersRange = metersRange;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Recupera la latitudine del punto centrale del raggio GPS.
        /// </summary>
        /// <value>
        /// La latitudile del punto centrale del raggio GPS.
        /// </value>
        public double GpsRangeCenterLatitude { get; private set; }

        /// <summary>
        /// Recupera la longitudine del punto centrale del raggio GPS.
        /// </summary>
        /// <value>
        /// La longitudine del punto centrale del raggio GPS.
        /// </value>
        public double GpsRangeCenterLongitude { get; private set; }

        /// <summary>
        /// Recupera il numero di kilometri di raggio GPS rispetto al punto centrale.
        /// </summary>
        /// <value>
        /// Il numero di kilometri di raggio GPS rispetto al punto centrale.
        /// </value>
        public double KmRange
        {
            get
            {
                return Convert.ToDouble(_metersRange) / 1000d;
            }
        }

        /// <summary>
        /// Recupera il valore massimo della latitudine per il raggio GPS.
        /// </summary>
        /// <value>
        /// Il valore massimo della latitudine per il raggio GPS.
        /// </value>
        public double MaxRangeLatitude
        {
            get
            {
                return GpsRangeCenterLatitude + GetLatitudeDegreeDelta();
            }
        }

        /// <summary>
        /// Recupera il valore minimo della latitudine per il raggio GPS.
        /// </summary>
        /// <value>
        /// Il valore minimo della latitudine per il raggio GPS.
        /// </value>
        public double MinRangeLatitude
        {
            get
            {
                return GpsRangeCenterLatitude - GetLatitudeDegreeDelta();
            }
        }

        /// <summary>
        /// Recupera il valore massimo della longitudine per il raggio GPS.
        /// </summary>
        /// <value>
        /// Il valore massimo della longitudine per il raggio GPS.
        /// </value>
        public double MaxRangeLongitude
        {
            get
            {
                return GpsRangeCenterLongitude + GetLongitudeDegreeDelta();
            }
        }

        /// <summary>
        /// Recupera il valore minimo della longitudine per il raggio GPS.
        /// </summary>
        /// <value>
        /// Il valore minimo della longitudine per il raggio GPS.
        /// </value>
        public double MinRangeLongitude
        {
            get
            {
                return GpsRangeCenterLongitude - GetLongitudeDegreeDelta();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determina se il punto specifico risulta nel range GPS.
        /// </summary>
        /// <param name="pointLatitude">La latitudine del punto per cui verificare la presenza nel range.</param>
        /// <param name="pointLongitude">La longitudine del punto per cui verificare la presenza nel range.</param>
        /// <returns><c>true</c> se il punto risulta appartenere al range; altrimenti false;</returns>
        public bool IsPointInRange(double pointLatitude, double pointLongitude)
        {
            // il punto risulta nel range se sia la latitudine che la longitudine rientano nel range
            return pointLatitude >= MinRangeLatitude && pointLatitude <= MaxRangeLatitude && pointLongitude >= MinRangeLongitude && pointLongitude <= MaxRangeLongitude;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calcola e restituisce il delta in gradi della latitudine utilizzando il raggio espresso in km salvato nell'istanza corrente.
        /// </summary>
        /// <returns>Il delta in gradi della latitudine calcolato a partire dal raggio espresso in km salvato nell'istanza corrente</returns>
        private double GetLatitudeDegreeDelta()
        {
            // il delta della latitudine è dato dal raggio in km diviso il raggio della terra moltiplicato per 180/pi greco per trasformare metri in gradi
            return (KmRange / EarthRadiiusKm) * (180d / Math.PI);
        }

        /// <summary>
        /// Calcola e restituisce il delta in gradi della longitudine utilizzando il raggio espresso in km salvato nell'istanza corrente.
        /// </summary>
        /// <returns>Il delta in gradi della longitudine calcolato a partire dal raggio espresso in km salvato nell'istanza corrente</returns>
        private double GetLongitudeDegreeDelta()
        {
            // il delta della longitudine è dato dal numero di kilometri di raggio diviso il raggio della terra moltiplicato per 180/pi grego per trasformare i metri in gradi;
            // il tutto va poi diviso per il coseno della latitudine per pi grego diviso 180 per armomizzare l'altra opposizione
            return (KmRange / EarthRadiiusKm) * (180d / Math.PI) / Math.Cos(GpsRangeCenterLatitude * Math.PI / 180d);
        }

        #endregion


        #endregion



    }






}







