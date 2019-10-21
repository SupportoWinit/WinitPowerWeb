using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Web.Security;
using Business.Repository;
using System.Text.RegularExpressions;
using Common;
using System.Data.Common;
using System.Dynamic;
using System.Reflection;
using System.IO;
using System.Data;
using System.Data.OleDb;
using Business.MDBSchema;
using Business.MDBSchema.PowerMDBDataSetTableAdapters;
using System.Threading;
using System.Security;
using Domain;
using System.Web;
using Common.Properties;
using Business.LicenceServiceReference;
using Business.Repository.Custom;
using Data;
using System.Collections;
using log4net;
using System.Net;
using System.Diagnostics;
using Business.GeocodeServiceReference;
using System.Threading.Tasks;
using System.Globalization;

namespace Business
{
    public static class IListExtension
    {
        public static IList ToNonGenericList(this IQueryable query)
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(query.ElementType), query);
        }
    }

    #region ROUTINE DI CONVERSIONE ConvertToDynamic/ConvertToDynamicList/ConvertTo
    public static class AnonymousTypeConversion
    {
        public static dynamic ConvertToDynamic(this DbDataRecord record)
        {
            dynamic item = new ExpandoObject();
            var dict = item as IDictionary<String, object>;

            for (int f = 0; f < record.FieldCount; f++)
                dict.Add(record.GetName(f), record.GetValue(f));

            return item;
        }

        public static List<dynamic> ConvertToDynamicList(this List<DbDataRecord> list)
        {
            List<dynamic> result = new List<dynamic>();

            list.ForEach(rec =>
            {
                result.Add(rec.ConvertToDynamic());
            });

            return result;
        }

        public static T ConvertTo<T>(this DbDataRecord record)
        {
            T item = Activator.CreateInstance<T>();
            for (int f = 0; f < record.FieldCount; f++)
            {
                PropertyInfo p = item.GetType().GetProperty(record.GetName(f));
                if (p != null && p.PropertyType == record.GetFieldType(f))
                {
                    p.SetValue(item, record.GetValue(f), null);
                }
            }

            return item;
        }

        public static List<T> ConvertTo<T>(this List<DbDataRecord> list)
        {
            List<T> result = (List<T>)Activator.CreateInstance<List<T>>();

            list.ForEach(rec =>
            {
                result.Add(rec.ConvertTo<T>());
            });

            return result;
        }
    }
    #endregion

    public static class CommonServiceBiz
    {
        const string FIELD = "FLD_";
        const string ERROR = "ERR_";
        const string CONTROL = "CTRL_";
        const string TABDECOD = "TD_";
        const string MENU = "MENU_";
        const string STRING = "STR_";

        public static bool IsToApplyDomainFilter()
        {
            bool result = false;
            if (PowerWebContext.Current.User != null && PowerWebContext.Current.User.Utenti_Fil != null && PowerWebContext.Current.User.Utenti_Resp != null)
                result = PowerWebContext.Current.User.Utenti_Fil.Count > 0 && PowerWebContext.Current.User.Utenti_Resp.Count > 0;

            return result;
        }
        public static String GetLocalizedString(string baseString, params string[] args)
        {
            String result = null;

            if (RepoManager.ResourcesRepo.ResourcesDictionary.ContainsKey(baseString))
            {
                result = GetLocalizedString(baseString);

                List<String> argsValues = new List<string>();
                foreach (var arg in args)
                    argsValues.Add(GetLocalizedString(arg));

                if (!String.IsNullOrEmpty(result) && args.Count()>0)
                    result = String.Format(result, argsValues.ToArray());
            }

            if (String.IsNullOrEmpty(result))
                result = "### - " + baseString.ToString();

            return result;
        }
        public static String GetLocalizedString(PowerWebResources baseString, params PowerWebResources[] args)
        {
            String result = null;

            List<String> argsValues = new List<string>();
            foreach (var arg in args)
                argsValues.Add(GetLocalizedString(arg.ToString()));

            return GetLocalizedString(baseString.ToString(), argsValues.ToArray());
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
                }
            }

            if (String.IsNullOrEmpty(result))
                result = "### - " + value;

            return result;
        }

        #region IMPORT DATA

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

        public static Dictionary<string, List<Dictionary<string, string>>> ImportData(String path, ImportDBTypeEnum type)
        {
            //IMPORT DEI DATI DA ACCESS
            //Abbiamo deciso di non importare: TAB_AUT, TAB_FESTIVI, UTENTI, PARAMETRI, INPUT, INPUT_FISICO (Massimo 12/09/2012) e TAB_DIST e PASS
            //Importeremo più avanti: TAB_ORARI, TAB_ORARI_CANT, TAB_ORARI_TIPO (Massimo 12/09/2012)
            // NOTA SE VENGONO TROVATE DELLE RIL (Power)--- > ALLORA VIENE SALTATO IL CARICAMENTO DELLE REG_FIS (COMO08)
            // TIPOIMPORT : "STD"     = Import Normale
            //            : MOSAICO"  = Import x Mosaico che ha le seguenti Particolarità
            //                          1) La Tab Filiali viene caricata nella Tabella RESP
            //                          2) IL Campo Filale_Cant                                            
            var errors = new Dictionary<string, List<Dictionary<string, string>>>();
            ILog oLog = LogManager.GetLogger("Business.CommonServiceBiz");
            PowerMDBDataSet currentDS = new PowerMDBDataSet();
            string tableName = null;
            if (type == ImportDBTypeEnum.mdb)
            {
                IEnumerable<Tab_Chk_Imp> toNotImportTables = RepoManager.Tab_Chk_ImpRepo.Find(
                  x => x.Chiave_Record_Tab_Check_Imp == null && x.Stato_Record_Tab_Check_Imp);
                string strAccessConn = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + path;
                OleDbConnection myAccessConn = null;
                try
                {
                    myAccessConn = new OleDbConnection(strAccessConn);
                    oLog.WarnFormat("----------------------------------- INIZIO IMPORT DA ACCESS ----------------------------------------", "Cant");
                    oLog.DebugFormat("----------------------------------- INIZIO IMPORT DA ACCESS ----------------------------------------", "Cant");

                    #region 1) IMPORT FIL
                    tableName = "Fil";
                    if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                    {
                        oLog.DebugFormat("----------------------------------- INIZIO IMPORT DATI ----------------------------------------", "FIL");
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                        }
                    }
                    #endregion

                    #region 2) IMPORT RESP
                    tableName = "Resp";
                    if (toNotImportTables.SingleOrDefault(x => x.Nome_Tabella_Tab_Check_Imp == tableName) == null)
                    {
                        oLog.DebugFormat("----------------------------------- INIZIO IMPORT DATI ----------------------------------------", "RESP");
                        using (Tab_FilialiTableAdapter tableAdapter = new Tab_FilialiTableAdapter())
                        {
                            if (myAccessConn.State == ConnectionState.Broken)
                                myAccessConn = new OleDbConnection(strAccessConn);
                            tableAdapter.Connection = myAccessConn;
                            tableAdapter.Fill(currentDS.Tab_Filiali);
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            var respErrors = RepoManager.RespRepo.ImportFromDataSet(currentDS);
                            if (respErrors.Count > 0)
                                errors.Add(tableName, respErrors);
                            sw.Stop();
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            var fruErrors = RepoManager.FruRepo.ImportFromDataSet(currentDS);
                            if (fruErrors.Count > 0)
                                errors.Add(tableName, fruErrors);
                            sw.Stop();
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                        }
                    }
                    #endregion

                    #region 13) IMPORT Pru_Col
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                        }
                    }
                    #endregion

                    #region 14)IMPORT Fru_Cant
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                        }
                    }
                    #endregion

                    #region 15) IMPORT Tab_Dist (NO X IL MOMENTO)
                    tableName = "Tab_Dist";
                    if ((RepoManager.ParamRepo.ParametersRow.ModuleTypeEnum & ModuleTypeEnum.Mosaico) == ModuleTypeEnum.None)  //NEL CASO Di MOSAICO Non VIENE IMPORTATA la TAB_DIST
                    {
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
                                oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                            }
                        }
                    }
                    #endregion

                    #region 16) IMPORT Reg/Reg_Fis/Pass


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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
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
                            oLog.DebugFormat("Ho impiegato {0} per importare la tabella {1}.", CommonService.Get_MMM_SS_FFF_FormattedString(sw.Elapsed), tableName);
                        }
                    }
                    #endregion
                    //Segnalo la Fine dell'IMPORT da ACCESS
                    ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100d,
                                CommonServiceBiz.GetLocalizedString(PowerWebResources.STR_IMPORT_TERMINATO));
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return errors;
        }

        #endregion

        #region GEOCODE
        // This method makes the initial CalculateRoute asynchronous request using the results of the Geocode Service.
        public static RouteServiceReference.RouteResponse GetRoute(GeocodeServiceReference.GeocodeResult[] results)
        {
            RouteServiceReference.RouteResponse res;
            try
            {
                // Create the service variable and set the callback method using the CalculateRouteCompleted property.
                RouteServiceReference.RouteServiceClient routeService = new RouteServiceReference.RouteServiceClient();

                // Set the token.
                RouteServiceReference.RouteRequest routeRequest = new RouteServiceReference.RouteRequest();
                routeRequest.Credentials = new RouteServiceReference.Credentials();
                routeRequest.Credentials.ApplicationId = RepoManager.ParamRepo.ParametersRow.BingKey;

                // Return the route points so the route can be drawn.
                routeRequest.Options = new RouteServiceReference.RouteOptions();
                //routeRequest.Options.RoutePathType = RouteServiceReference.RoutePathType.Points;

                // Set the waypoints of the route to be calculated using the Geocode Service results stored in the geocodeResults variable.
                var waypointsList = new System.Collections.ObjectModel.ObservableCollection<RouteServiceReference.Waypoint>();
                foreach (GeocodeServiceReference.GeocodeResult result in results)
                    waypointsList.Add(GeocodeResultToWaypoint(result));

                routeRequest.Waypoints = waypointsList.ToArray();

                res = routeService.CalculateRoute(routeRequest);
            }
            catch (Exception e)
            {
                res = null;
            }

            return res;

        }

        private static RouteServiceReference.Waypoint GeocodeResultToWaypoint(GeocodeServiceReference.GeocodeResult result)
        {
            RouteServiceReference.Waypoint waypoint = new RouteServiceReference.Waypoint();
            waypoint.Description = result.DisplayName;
            waypoint.Location = new RouteServiceReference.Location();
            waypoint.Location.Latitude = result.Locations[0].Latitude;
            waypoint.Location.Longitude = result.Locations[0].Longitude;
            return waypoint;
        }

        public static GeocodeResult[] GetGeocode(string address)
        {
            var results = new GeocodeResult[0];

            if (!String.IsNullOrEmpty(RepoManager.ParamRepo.ParametersRow.BingKey))
            {

                GeocodeRequest geocodeRequest = new GeocodeRequest();

                // Set the credentials using a valid Bing Maps key
                geocodeRequest.Credentials = new Credentials();
                geocodeRequest.Credentials.ApplicationId = RepoManager.ParamRepo.ParametersRow.BingKey;

                // Set the full address query
                geocodeRequest.Query = address;

                // Set the options to only return high confidence results 
                ConfidenceFilter[] filters = new ConfidenceFilter[1];
                filters[0] = new ConfidenceFilter();
                filters[0].MinimumConfidence = Confidence.High;

                // Add the filters to the options
                GeocodeOptions geocodeOptions = new GeocodeOptions();
                geocodeOptions.Filters = filters;
                geocodeRequest.Options = geocodeOptions;

                // Make the geocode request
                GeocodeServiceClient geocodeService = new GeocodeServiceClient();
                GeocodeResponse geocodeResponse = geocodeService.Geocode(geocodeRequest);

                if (geocodeResponse.Results.Length == 0)
                {
                    ((ConfidenceFilter)geocodeOptions.Filters[0]).MinimumConfidence = Confidence.Medium;
                    geocodeResponse = geocodeService.Geocode(geocodeRequest);
                }

                if (geocodeResponse.Results.Length == 0)
                {
                    ((ConfidenceFilter)geocodeOptions.Filters[0]).MinimumConfidence = Confidence.Low;
                    geocodeResponse = geocodeService.Geocode(geocodeRequest);
                }

                results = geocodeResponse.Results;
            }
            return results;
        }

        #endregion

        #region Gestione Scadenze Applicazione
        private static void UpdateExpirationDate()
        {

            Param paramsRow = RepoManager.ParamRepo.ParametersRow;

            LicenceServiceClient sc = new LicenceServiceClient();


            FileInfo lcFileInfo = new FileInfo(HttpContext.Current.Server.MapPath(Settings.Default.LastConn));
            if (lcFileInfo.Exists)
            {
                long storedTicks = 0;

                using (StreamReader sr = new StreamReader(lcFileInfo.OpenRead()))
                    storedTicks = Convert.ToInt64(MD5DecryptString(sr.ReadToEnd(), Settings.Default.Password));

                if (storedTicks > 0)
                {
                    DateTime storedLastConnection = new DateTime(storedTicks);

                    var licence = sc.GetActivation(paramsRow.PublicKey, storedLastConnection);


                    if (!String.IsNullOrEmpty(licence))
                    {
                        paramsRow.ActivationDate = licence;
                        RepoManager.ParamRepo.SaveChanges();
                    }
                    else
                    {
                        //In this case you can consider to delete the current activation key
                    }
                }
            }
        }

        public static bool CheckFirstTimeInitialize()
        {
            bool isFirstTimeInitialized = false;

            FileInfo pkFileInfo = new FileInfo(HttpContext.Current.Server.MapPath(Settings.Default.PKPath));
            FileInfo lcFileInfo = new FileInfo(HttpContext.Current.Server.MapPath(Settings.Default.LastConn));

            if (lcFileInfo.Exists)
            {
                try
                {
                    long storedTicks = 0;
                    long currentTicks = 0;

                    using (StreamReader sr = new StreamReader(lcFileInfo.OpenRead()))
                    {
                        storedTicks = Convert.ToInt64(MD5DecryptString(sr.ReadToEnd(), Settings.Default.Password));
                        currentTicks = DateTime.UtcNow.Ticks;
                    }

                    if (currentTicks <= storedTicks)
                        throw new Exception();
                    else
                    {
                        DateTime storedDT = new DateTime(storedTicks).Date;
                        DateTime currentDT = new DateTime(currentTicks).Date;

                        if (currentDT > storedDT)
                        {
                            if (lcFileInfo.Exists)
                            {
                                lcFileInfo.Delete();
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
                catch (Exception ex)
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
                catch (Exception)
                {
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

        public static bool IsExpired()
        {
            Param paramsRow = RepoManager.ParamRepo.ParametersRow;

            bool expired = true;

            DateTime expirationDate = GetExpirationDate(paramsRow);

            if (expirationDate.Date >= DateTime.UtcNow.Date)
                expired = false;

            return expired;
        }

        public static DateTime GetExpirationDate(Domain.Param paramsRow)
        {
            DateTime expiryDate = DateTime.MaxValue;
            //long expTicks = 0;

            //DateTime expiryDate = DateTime.MinValue;

            //using (RSACryptoServiceProvider RSACSP = CommonServiceBiz.GetRSACryptoServiceProvider())
            //{
            //  if (RSACSP != null)
            //  {
            //    byte[] descrActivation = RSACSP.Decrypt(Convert.FromBase64String(paramsRow.ActivationDate), false);

            //    expTicks = BitConverter.ToInt64(descrActivation, 0);

            //    if (expTicks > 0)
            //      expiryDate = new DateTime(expTicks);
            //  }
            //}

            return expiryDate;
        }

        #endregion

        public static void SaveStreamReaderToFile(StreamReader stream, String path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(stream.ReadToEnd());
                sw.Flush();
                sw.Close();
            }
        }

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

        public static RSACryptoServiceProvider GetRSACryptoServiceProvider()
        {
            RSACryptoServiceProvider RSACSP = null;

            FileInfo fileInfo = new FileInfo(HttpContext.Current.Server.MapPath(Settings.Default.PKPath));

            if (fileInfo.Exists)
                using (StreamReader sr = new StreamReader(fileInfo.OpenRead()))
                {
                    RSACSP = new RSACryptoServiceProvider();

                    string mD5DecryptString = MD5DecryptString(sr.ReadToEnd(), Settings.Default.Password);

                    if (!String.IsNullOrEmpty(mD5DecryptString))
                        RSACSP.FromXmlString(mD5DecryptString);
                }

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

        public static void Encrypt(string fileIn,
                    string fileOut, string Password)
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

        public static byte[] Decrypt(byte[] cipherData,
                                    byte[] Key, byte[] IV)
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

        public static void Decrypt(string fileIn,
                    string fileOut, string Password)
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

        //public static string GeneraCodiceFiscale(string sNome, string sCognome, DateTime dtmDataNascita,
        //string sSesso, string sComune, string sProvincia)
        //{
        //    //verifica che tutti i campi obbligatori non siano vuoti
        //    if (CommonService.Nz(sNome, "") == "" || CommonService.Nz(sCognome, "") == ""
        //    || CommonService.Nz(dtmDataNascita, new DateTime()) == new DateTime() || CommonService.Nz(sSesso, "") == ""
        //    || CommonService.Nz(sComune, "") == "" || CommonService.Nz(sProvincia, "") == "")
        //        return Common.PowerWebResources.ERR_DATI_INSUFFICIENTI;
        //    string sRetVal = "";
        //    sCognome = CodFiscale.FormattaStringa(sCognome);
        //    sNome = CodFiscale.FormattaStringa(sNome);
        //    sRetVal = CodFiscale.ElaboraCognome(sCognome.ToUpper()) + CodFiscale.ElaboraNome(sNome.ToUpper()) + CodFiscale.ElaboraDataNascitaESesso(dtmDataNascita, sSesso.ToUpper())
        //      + CodFiscale.ElaboraCodiceComune(sComune.ToUpper(), sProvincia.ToUpper());
        //    if (sRetVal.Length != 15)
        //        return Common.PowerWebResources.ERR_IN_CALCOLO_CODICE_FISCALE;
        //    sRetVal += CodFiscale.CalcolaUltimaLettera(sRetVal);
        //    return sRetVal;
        //}

        #region Elaborate State Dictionary
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
            Tab_Comuni oRecord = RepoManager.Tab_ComuniRepo.SingleOrDefault(x => x.Luogo_Tab_Comuni == sComune);
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
}







