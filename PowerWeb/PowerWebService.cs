using Business;
using Business.Repository;
using Common;
using DevExpress.Data.Filtering;
using DevExpress.Data.PLinq.Helpers;
using DevExpress.Utils;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxEditors;
using DevExpress.Web.ASPxGridView;
using Domain;
using log4net;
using PowerWeb.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Common;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;


namespace PowerWeb
{


    public static class PowerWebService
    {
        const string JavaScriptFormat = "<script type=\"text/javascript\">{0}</script>";
        const string JavaScriptSrcFormat = "<script type=\"text/javascript\" src=\"{0}\" ></script>";

        public static T GetFromSession<T>(String sessionKey)
        {
            return PowerWebContext.GetFromSession<T>(sessionKey);
        }

        public static void SetToSession<T>(String sessionKey, T value)
        {
            PowerWebContext.SetToSession<T>(sessionKey, value);
        }

        public static bool IsCurrentConnectionSecured()
        // TRUE se connessione HTTPS (unused)
        {
            bool useSSL = false;
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                useSSL = HttpContext.Current.Request.IsSecureConnection;
                //when your hosting uses a load balancer on their server then the Request.IsSecureConnection is never got set to true, use the statement below
                //just uncomment it
                //useSSL = HttpContext.Current.Request.ServerVariables["HTTP_CLUSTER_HTTPS"] == "on" ? true : false;
            }

            return useSSL;
        }

        private static void RegisterScript(Page page, string scriptStr)
        // Usato per aggiungere librerie javascript dinamicamente (non usato)
        {
            if (page.Header != null)
            {
                //we have a header
                //if (HttpContext.Current.Items["JQueryRegistered"] == null || !Convert.ToBoolean(HttpContext.Current.Items["JQueryRegistered"]))
                //{
                Literal script = new Literal() { Text = scriptStr };
                page.Header.Controls.AddAt(0, script);
                //}
                //HttpContext.Current.Items["JQueryRegistered"] = true;
            }
            else
            {
                //no header found
                page.ClientScript.RegisterClientScriptInclude(scriptStr, scriptStr);
            }
        }

        public static void AddValidationErrors(Dictionary<string, string> bizErrors, Dictionary<GridViewColumn, string> presErrors, ASPxGridView gvUsers, Type type)
        // Aggiunge al dizionario della GridView i messaggi generati dalla Check
        {
            ILog log = LogManager.GetLogger(type);

            foreach (KeyValuePair<string, string> error in bizErrors)
            {
                GridViewDataColumn currentColumn = gvUsers.Columns[error.Key] as GridViewDataColumn;
                if (currentColumn != null)
                {
                    if (!presErrors.ContainsKey(currentColumn))
                    {
                        presErrors[currentColumn] = error.Value;
                        log.Info(String.Format("Validation error by user {0} at column {1} with value {2}", PowerWebContext.Current.User.Codice_Utente, currentColumn.FieldName, error.Value));
                    }

                }
            }
        }

        public static String GetValidationErrorString(Dictionary<GridViewColumn, string> errors)
        // Formatta gli errori presenti nel dizionario
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<GridViewColumn, string> error in errors)
                sb.Append(" - ").Append(error.Value).Append(Environment.NewLine);
            return sb.ToString();
        }

        public static string LoginPageURL
        // Indirizzo della pagina di Login (Site.Master)
        {
            get
            {
                return new StringBuilder(CommonService.BaseSiteUrl).Append("Pages/Account/LoginPage.aspx").ToString();
            }
        }

        public static void SetResponseNoCache(HttpResponse response)
        // Evita di salvare dati nella cache del browser dopo il login
        {
            if (response == null)
                throw new ArgumentNullException("response");

            //response.Cache.SetCacheability(HttpCacheability.NoCache) 

            response.CacheControl = "private";
            response.Expires = 0;
            response.AddHeader("pragma", "no-cache");
        }

        public static void LogOut()
        // Resetta la sessione (al logout)
        {
            if (PowerWebContext.Current != null)
                PowerWebContext.Current.RemoveUser();

            if (HttpContext.Current != null && HttpContext.Current.Session != null)
                HttpContext.Current.Session.Abandon();

            FormsAuthentication.SignOut();
        }

        public static List<String> ConvertTabPageExtendedToString(List<TabPageExtended> selectedTabs)
        // Restituisce una lista con i nomi dei tab
        {
            List<String> result = new List<String>();
            selectedTabs.ForEach(i => result.Add(i.Name));
            return result;
        }

        public static void FillEntityKey(Object entity, OrderedDictionary dictionary, string keyFieldName)
        // Imposta ad un record la sua KEYFIELD
        {
            if (!String.IsNullOrEmpty(keyFieldName))
            {
                PropertyInfo keyPropInfo = entity.GetType().GetProperty(keyFieldName);
                if (keyPropInfo != null)
                    keyPropInfo.SetValue(entity, dictionary[keyFieldName], null);
            }
        }

        public static void FillEntityPropertiesMultiTypes(Object entity, OrderedDictionary dictionary)
        // Imposta sul record i valori appena inseriti
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                var entryVal = entry.Value;

                var prop = entity.GetType().GetProperty((String)entry.Key);
                if (prop != null && prop.GetSetMethod() != null)
                {

                    Type t = entry.Value == null ? null : entry.Value.GetType();

                    if (t != null && t.FullName != prop.PropertyType.FullName)
                    {
                        if (prop.PropertyType.FullName == typeof(Int32).FullName)
                        {
                            int convertedEntry = Convert.ToInt32(entry.Value);
                            if (prop.Name.ToUpper().EndsWith("_ID") && prop.PropertyType == typeof(int) && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && entryVal.ToString() == "0")
                                prop.SetValue(entity, null, null);
                            else
                            {
                                prop.SetValue(entity, convertedEntry, null);
                            }
                        }
                        else if (prop.PropertyType.FullName == typeof(Int64).FullName)
                        {
                            long? convertedEntry = Convert.ToInt64(entry.Value);
                            if (prop.Name.ToUpper().EndsWith("_ID") && prop.PropertyType == typeof(int) && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && entryVal.ToString() == "0")
                                prop.SetValue(entity, null, null);
                            else
                            {
                                prop.SetValue(entity, convertedEntry, null);
                            }
                        }
                        else if (prop.PropertyType.FullName == typeof(Int16).FullName)
                        {
                            Int16 convertedEntry = Convert.ToInt16(entry.Value);
                            if (prop.Name.ToUpper().EndsWith("_ID") && prop.PropertyType == typeof(int) && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && entryVal.ToString() == "0")
                                prop.SetValue(entity, null, null);
                            else
                            {
                                prop.SetValue(entity, convertedEntry, null);
                            }
                        }
                        else if (prop.PropertyType.FullName == typeof(Nullable<Int32>).FullName)
                        {
                            int? convertedEntry = (Nullable<Int32>)Convert.ToInt32(entry.Value);
                            if (prop.Name.ToUpper().EndsWith("_ID") && prop.PropertyType == typeof(int) && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && entryVal.ToString() == "0")
                                prop.SetValue(entity, null, null);
                            else
                            {
                                prop.SetValue(entity, convertedEntry, null);
                            }
                        }
                        else if (prop.PropertyType.FullName == typeof(Nullable<Int16>).FullName)
                        {
                            Int16? convertedEntry = (Nullable<Int16>)Convert.ToInt16(entry.Value);

                            if (prop.Name.ToUpper().EndsWith("_ID") && prop.PropertyType == typeof(int) && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && entryVal.ToString() == "0")
                                prop.SetValue(entity, null, null);
                            else
                            {
                                prop.SetValue(entity, convertedEntry, null);
                            }


                        }
                        else if (prop.PropertyType.FullName == typeof(Nullable<Int64>).FullName)
                        {
                            long? convertedEntry = (Nullable<Int64>)Convert.ToInt64(entry.Value);
                            if (prop.Name.ToUpper().EndsWith("_ID") && prop.PropertyType == typeof(int) && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && entryVal.ToString() == "0")
                                prop.SetValue(entity, null, null);
                            else
                            {
                                prop.SetValue(entity, convertedEntry, null);
                            }
                        }
                    }
                    else
                    {
                        var convertedEntry = entryVal;
                        if (prop.Name.ToUpper().EndsWith("_ID") && prop.PropertyType == typeof(int) && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && entryVal.ToString() == "0")
                            prop.SetValue(entity, null, null);
                        else
                        {
                            prop.SetValue(entity, convertedEntry, null);
                        }
                    }


                }
            }
        }

        public static void FillEntityProperties(Object entity, OrderedDictionary dictionary)
        // Imposta sul record i valori appena inseriti
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                var prop = entity.GetType().GetProperty((String)entry.Key);
                if (prop != null && prop.GetSetMethod() != null)
                    if (prop.Name.ToUpper().EndsWith("_ID") && prop.PropertyType == typeof(int) && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && entry.Value.ToString() == "0")
                        prop.SetValue(entity, null, null);
                    else
                    {
                        prop.SetValue(entity, entry.Value, null);
                    }
            }
        }

        //
        /// <summary>
        /// Gestisce il Caricamento delle ComboBox presenti in un Modulo
        /// </summary>
        /// <param name="grid">The grid.</param>
        public static void FillComboboxes(ASPxGridView grid)
        {
            foreach (GridViewDataComboBoxColumn gridViewDataComboBoxColumn in grid.Columns.OfType<GridViewDataComboBoxColumn>())
                //cotruzione del combobox

                BindCombobox(gridViewDataComboBoxColumn);
        }

        public static void FillComboboxes(ASPxComboBox combo, String field = "", bool addEvents = true)
        {
            combo.Items.Clear();

            List<Tab_GridLookup> tgls = null;

            if (String.IsNullOrEmpty(field))
                tgls = TabGridLookups.Where(tgl => tgl.NomeRicerca == combo.ID).OrderBy(tgl => tgl.Ordinamento).ToList();
            else tgls = TabGridLookups.Where(tgl => tgl.NomeRicerca == field).OrderBy(tgl => tgl.Ordinamento).ToList();

            if (tgls.Count() > 0)
            {
                var mainTGL = tgls.First();

                var isNotOnlyInListTGL = tgls.FirstOrDefault(tgl => tgl.Campo_NotOnlyInList) != null;

                String keyField = mainTGL.NomeTab + "_Id";

                if (mainTGL.NomeTab == "Tab_Decod")
                    keyField = mainTGL.NomeCampo;

                combo.ValueField = keyField;

                bool isInteger = false;

                foreach (var item in tgls)
                {
                    ListBoxColumn newListBoxColumn = new ListBoxColumn(item.NomeCampo, BusinessService.GetLocalizedString(item.Nome_Risorsa));
                    newListBoxColumn.Visible = item.Campo_Video;

                    if (item.Campo_Db)
                        combo.ValueField = item.NomeCampo;

                    if (!String.IsNullOrEmpty(item.Width))
                        newListBoxColumn.Width = new Unit(item.Width);

                    combo.Columns.Add(newListBoxColumn);

                    isInteger = item.IsInteger;
                }

                if (combo.ValueField.EndsWith("Id") || isInteger)
                    combo.ValueType = typeof(Int32);
                else combo.ValueType = typeof(String);

                combo.ClientInstanceName = combo.ID;
                if (!String.IsNullOrEmpty(field))
                    combo.ClientInstanceName = field;

                combo.IncrementalFilteringMode = IncrementalFilteringMode.Contains;

                if (addEvents)
                    combo.ItemsRequestedByFilterCondition += PropertiesComboBox_ItemsRequestedByFilterCondition;

                combo.IncrementalFilteringDelay = RepoManager.ParamRepo.ParametersRow.ComboBoxDelay;

                combo.DropDownRows = RepoManager.ParamRepo.ParametersRow.RowsPerComboBox;

                if (addEvents)
                    combo.ItemRequestedByValue += PropertiesComboBox_ItemRequestedByValue;

                combo.DropDownStyle = DropDownStyle.DropDownList;

                if (isNotOnlyInListTGL)
                    combo.DropDownStyle = DropDownStyle.DropDown;

                combo.DropDownWidth = new Unit(600);

                combo.CallbackPageSize = RepoManager.ParamRepo.ParametersRow.ComboboxRowsPerPage;

                combo.EnableCallbackMode = true;

                combo.TextFormatString = GetComboboxFormatString(tgls.FindAll(tgl => tgl.Campo_Selezionato || tgl.Campo_Video).OrderBy(tgl => tgl.Ordinamento).ToList());
            }
        }

        public static void InitGrid(ASPxGridView GridView)
        {
            //GridView.ViewStateMode = ViewStateMode.Disabled;

            //GridView.EnableViewState = false;

            GridView.EnableRowsCache = true;
            GridView.Settings.ShowFilterRowMenu = true;
            GridView.Settings.ShowHeaderFilterButton = true;

            //nel caso in cui l'utente è inferiore 
            if (PowerWebContext.Current != null && (BusinessService.IsToApplyDomainFilter() || PowerWebContext.Current.User.Col_Id != null) && PowerWebContext.Current.User.Liv_Utente < 10)
                GridView.Settings.ShowFilterBar = GridViewStatusBarMode.Hidden;
            else
                GridView.Settings.ShowFilterBar = GridViewStatusBarMode.Visible;

            GridView.SettingsDetail.ExportMode = GridViewDetailExportMode.All;
            GridView.SettingsPager.PageSize = RepoManager.ParamRepo.ParametersRow.RowsPerPage;
            GridView.SettingsBehavior.ColumnResizeMode = ColumnResizeMode.Control;
            GridView.Settings.ShowFilterRow = true;
            GridView.Settings.ShowGroupPanel = true;
            GridView.Settings.HorizontalScrollBarMode = ScrollBarMode.Auto;
            GridView.SettingsPager.PageSizeItemSettings.Visible = true;
            GridView.SettingsPager.PageSizeItemSettings.Position = DevExpress.Web.ASPxPager.PagerPageSizePosition.Right;
            GridView.SettingsPager.Position = System.Web.UI.WebControls.PagerPosition.Bottom;
            GridView.Styles.Header.Wrap = DevExpress.Utils.DefaultBoolean.True;


            // imposto il tipo di filtro per ogni data column della griglia che in questo metodo si inizializza
            // affinchè sia possibile selezionare più valori tra quelli esistenti in griglia
            try
            {
                foreach (var dataCol in GridView.DataColumns)
                {
                    dataCol.Settings.HeaderFilterMode = HeaderFilterMode.CheckedList;
                }
            }
            catch (Exception)
            {
                // silenziamento di un eventuale errore nel processo delle colonne
            }

        }

        public static void InitDetailGrid(Page page, ASPxGridView detailGrid)
        {
            GridMasterPage masterPage = page.Master as GridMasterPage;
            if (masterPage != null)
            {
                detailGrid.ClientSideEvents.CustomButtonClick = "OnCustomButtonDetailClick";

                detailGrid.CustomJSProperties += masterPage.GridView_CustomJSProperties;

                detailGrid.CustomButtonInitialize += masterPage.GridView_CustomButtonInitialize;

                detailGrid.CommandButtonInitialize += masterPage.GridView_CommandButtonInitialize;

                detailGrid.CellEditorInitialize += masterPage.BaseGridModule.DetailGridView_CellEditorInitialize;

                detailGrid.ParseValue += masterPage.DetailGridView_ParseValue;

                detailGrid.ClientInstanceName = "detailGrid";

                foreach (GridViewDataTextColumn column in detailGrid.Columns.OfType<GridViewDataTextColumn>())
                    column.Settings.AutoFilterCondition = AutoFilterCondition.Contains;

                detailGrid.CustomErrorText += masterPage.GridView_CustomErrorText;

                detailGrid.SettingsBehavior.ColumnResizeMode = ColumnResizeMode.Control;

                detailGrid.Settings.ShowFilterRow = true;

                detailGrid.Settings.ShowGroupPanel = true;

                detailGrid.Settings.ShowFilterRowMenu = true;

                detailGrid.Settings.ShowHeaderFilterButton = true;

                if (PowerWebContext.Current != null && (BusinessService.IsToApplyDomainFilter() || PowerWebContext.Current.User.Col_Id != null) && PowerWebContext.Current.User.Liv_Utente < 10)
                    detailGrid.Settings.ShowFilterBar = GridViewStatusBarMode.Hidden;
                else
                    detailGrid.Settings.ShowFilterBar = GridViewStatusBarMode.Visible;

                detailGrid.Settings.HorizontalScrollBarMode = ScrollBarMode.Auto;

                detailGrid.SettingsPager.PageSizeItemSettings.Visible = true;

                detailGrid.SettingsPager.PageSizeItemSettings.Position = DevExpress.Web.ASPxPager.PagerPageSizePosition.Right;

                detailGrid.SettingsPager.Position = PagerPosition.Bottom;

                detailGrid.SettingsPager.PageSize = RepoManager.ParamRepo.ParametersRow.RowsPerPage;

                detailGrid.HeaderFilterFillItems += masterPage.GridModule.HeaderFilterFillItems;

                InitGridCommandColumn(detailGrid.Columns.OfType<GridViewCommandColumn>().FirstOrDefault());

                PowerWebContext.SetToSession<ASPxGridView>(detailGrid.ClientID, detailGrid);
            }
        }

        public static void InitGridCommandColumn(GridViewCommandColumn currentCmdColumn)
        {
            if (currentCmdColumn != null)
            {
                GridViewCommandColumnCustomButton addButton = currentCmdColumn.CustomButtons.SingleOrDefault(btn => btn.ID == "add");
                if (addButton != null)
                    addButton.Image.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNINSERISCI);

                GridViewCommandColumnCustomButton addCloneButton = currentCmdColumn.CustomButtons.SingleOrDefault(btn => btn.ID == "addClone");
                if (addCloneButton != null)
                    addCloneButton.Image.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNCLONA);

                GridViewCommandColumnCustomButton deleteButton = currentCmdColumn.CustomButtons.SingleOrDefault(btn => btn.ID == "delete");
                if (deleteButton != null)
                    deleteButton.Image.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNELIMINA);

                GridViewCommandColumnCustomButton viewButton = currentCmdColumn.CustomButtons.SingleOrDefault(btn => btn.ID == "view");
                if (viewButton != null)
                    viewButton.Image.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNVISUALIZZA);

                GridViewCommandColumnCustomButton editMultiRowButton = currentCmdColumn.CustomButtons.SingleOrDefault(btn => btn.ID == "editMultiRow");
                if (editMultiRowButton != null)
                    editMultiRowButton.Image.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNMODIFICA);

                GridViewCommandColumnCustomButton editSingleRowButton = currentCmdColumn.CustomButtons.SingleOrDefault(btn => btn.ID == "editSingleRow");
                if (editSingleRowButton != null)
                    editSingleRowButton.Image.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNMODIFICA);

                GridViewCommandColumnCustomButton addCloneMultiButton = currentCmdColumn.CustomButtons.SingleOrDefault(btn => btn.ID == "addCloneMulti");
                if (addCloneMultiButton != null)
                    addCloneMultiButton.Image.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNCLONA);

                currentCmdColumn.Grid.SettingsCommandButton.EditButton.Image.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNMODIFICA);
                currentCmdColumn.Grid.SettingsCommandButton.ClearFilterButton.Image.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNANNULLA);

            }
        }

        public static List<Tab_GridLookup> TabGridLookups
        {
            get
            {
                var tgls = PowerWebContext.GetFromSession<List<Tab_GridLookup>>("General_TabGridLookups");

                if (tgls == null)
                {
                    tgls = RepoManager.Tab_GridLookupRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_GridLookup>>("General_TabGridLookups", tgls);
                }
                return tgls;
            }
        }


        private static Dictionary<String, Type> TypeDictionary
        {
            get
            {
                var typeDictionary = PowerWebContext.GetFromSession<Dictionary<String, Type>>("General_TypeDictionary");
                if (typeDictionary == null)
                {
                    typeDictionary = new Dictionary<String, Type>();
                    PowerWebContext.SetToSession<Dictionary<String, Type>>("General_TypeDictionary", typeDictionary);
                }
                return typeDictionary;
            }
            set
            {
                PowerWebContext.SetToSession<Dictionary<String, Type>>("General_TypeDictionary", value);
            }
        }



        private static Dictionary<String, List<DbDataRecord>> ListDictionary
        {
            get
            {
                var listDictionary = PowerWebContext.GetFromSession<Dictionary<String, List<DbDataRecord>>>("General_ListDictionary");
                if (listDictionary == null)
                {
                    listDictionary = new Dictionary<String, List<DbDataRecord>>();
                    PowerWebContext.SetToSession<Dictionary<String, List<DbDataRecord>>>("General_ListDictionary", listDictionary);
                }
                return listDictionary;
            }
            set
            {
                PowerWebContext.SetToSession<Dictionary<String, List<DbDataRecord>>>("General_ListDictionary", value);
            }
        }


        /// <summary>
        /// Metodo che si occpua della creazione e popolamento dei combobox
        /// </summary>
        /// <param name="comboboxColumn">The combobox column.</param>
        public static void BindCombobox(GridViewDataComboBoxColumn comboboxColumn)
        {
            //Lettura dei Record della Tab_GridLookUp con Ricerca=Nome Campo ComboBox ordinati per Campo Ordinamento
            var tgls = TabGridLookups.Where(tgl => tgl.NomeRicerca == comboboxColumn.FieldName).OrderBy(tgl => tgl.Ordinamento).ToList();

            if (tgls.Any())  //se quel Campo ha associato dei Record in Tab_GridLookUp
            {
                //viene estratto il primo campo della  combo
                var mainTGL = tgls.First();

                var isNotOnlyInListTGL = tgls.First().Campo_NotOnlyInList;

                String keyField = mainTGL.NomeTab + "_Id";  //Costruisco la Chiave con il Nome della Entità + "_ID"

                if (mainTGL.NomeTab == "Tab_Decod")   // se l'Entità è la TAB_DECOD
                    keyField = mainTGL.NomeCampo;     // Costruisco invece la Chiave con il Nome del Campo                

                comboboxColumn.PropertiesComboBox.ValueField = keyField;

                //per ogni cambo della grid Lookcup imposto le proprietà
                foreach (var item in tgls)
                {
                    //creazione di una nuova colonna all'interno del combobox con il nome ""localizzato"
                    ListBoxColumn newListBoxColumn = new ListBoxColumn(item.NomeCampo, BusinessService.GetLocalizedString(item.Nome_Risorsa));
                    //Definisce se quel Campo sarà visibile nella ComboBox a Video
                    newListBoxColumn.Visible = item.Campo_Video;

                    //Definisce se quel Campo contiene il Valore che verrà scritto nel DB quando si seleziona quel Record a Video
                    if (item.Campo_Db)

                        comboboxColumn.PropertiesComboBox.ValueField = item.NomeCampo;

                    //se è valorizzato esprime la larghezza delle colonne della combo
                    if (!String.IsNullOrEmpty(item.Width))
                        newListBoxColumn.Width = new Unit(item.Width);

                    comboboxColumn.PropertiesComboBox.Columns.Add(newListBoxColumn);
                }

                //Imposta il Tipo di Campo (INT/STRING) in base al fatto che sia un ID oppure no
                //NB:Inserisco il controllo per poter inserire una stringa nel n° di serire della Fru o della Pru poichè nella cascade delle
                // associazioni Pru-Col e Fru-Can hanno come valueField Pru_Id e Fru_Id che in questo caso deve essere trattato in modo diverso dagli altri ID
                //non più come intero ma convertito in stringa

                if ((comboboxColumn.PropertiesComboBox.ValueField.EndsWith("Id") || mainTGL.IsInteger) && ((!mainTGL.NomeRicerca.StartsWith("N_Serie")) || (!comboboxColumn.PropertiesComboBox.ValueField.EndsWith("Id"))))
                    comboboxColumn.PropertiesComboBox.ValueType = typeof(Int32);
                else
                    comboboxColumn.PropertiesComboBox.ValueType = typeof(String);

                //Imposta le Proprietà della Colonna della ComboBox
                comboboxColumn.PropertiesComboBox.ClientInstanceName = comboboxColumn.FieldName;
                comboboxColumn.PropertiesComboBox.IncrementalFilteringMode = IncrementalFilteringMode.Contains;

                //evento che recupra i calori da inserire seguendo i filtri salvati nel layout
                comboboxColumn.PropertiesComboBox.ItemsRequestedByFilterCondition += PropertiesComboBox_ItemsRequestedByFilterCondition;
                //evento che recupera i valori da inserire nella combobox ricercandoli mediante un valore
                comboboxColumn.PropertiesComboBox.ItemRequestedByValue += PropertiesComboBox_ItemRequestedByValue;


                comboboxColumn.PropertiesComboBox.DropDownStyle = isNotOnlyInListTGL ? DropDownStyle.DropDown : DropDownStyle.DropDownList;
                comboboxColumn.PropertiesComboBox.CallbackPageSize = RepoManager.ParamRepo.ParametersRow.ComboboxRowsPerPage;
                comboboxColumn.PropertiesComboBox.EnableCallbackMode = true;

                comboboxColumn.PropertiesComboBox.DropDownRows = RepoManager.ParamRepo.ParametersRow.RowsPerComboBox;

                comboboxColumn.PropertiesComboBox.IncrementalFilteringDelay = RepoManager.ParamRepo.ParametersRow.ComboBoxDelay;

                //Nel caso in cui ci siano PIU' CAMPI impostati con Campo_Video = True prepara il corrispondente N° di Colonne
                comboboxColumn.PropertiesComboBox.TextFormatString = GetComboboxFormatString(tgls.FindAll(tgl => tgl.Campo_Selezionato || tgl.Campo_Video).OrderBy(tgl => tgl.Ordinamento).ToList());
                //impostazione larghezza fissa di tutte le combobox
                comboboxColumn.PropertiesComboBox.DropDownWidth = new Unit(600);
                //combobox.PropertiesComboBox.ClientSideEvents.EndCallback = "ComboBox_EndCallback";
            }
        }

        /// <summary>
        ///Metodo che gestisce il caricamento dei valori nella combo in base ad uno specifico valore
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="ListEditItemRequestedByValueEventArgs"/> instance containing the event data.</param>
        static void PropertiesComboBox_ItemRequestedByValue(object source, ListEditItemRequestedByValueEventArgs e)
        {

            if (e.Value == null || String.IsNullOrEmpty(e.Value.ToString()))
                return;

            ASPxComboBox comboBox = (ASPxComboBox)source;

            //viene estratto il nome del combobox da riempire
            String searchName = comboBox.ClientInstanceName;

            //il valore da ricercare
            String searchValue = e.Value.ToString();

            //genera la lista di elementi come risulatao della query
            IQueryable queryList = RepoManager.Tab_GridLookupRepo.SearchByFieldAndValue(TabGridLookups, searchName, e.Value.ToString()).AsQueryable();

            //come sorgente di dati la combo ha il risulatato della query
            comboBox.DataSource = queryList;

            comboBox.DataBindItems();
        }


        /// <summary>
        /// Metodo che si occupa del caricamento dei dati nella combo in base alla condizione di un filtro (il campo value e dato dal filtro della combo su cui si scatena l'evento)
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="ListEditItemsRequestedByFilterConditionEventArgs"/> instance containing the event data.</param>
        static void PropertiesComboBox_ItemsRequestedByFilterCondition(object source, ListEditItemsRequestedByFilterConditionEventArgs e)
        {

            ASPxComboBox comboBox = (ASPxComboBox)source;

            String searchName = comboBox.ClientInstanceName;

            //viene eseguita una query in base al filtro impostato sulla combo che si desidera popolare
            IQueryable queryList = RepoManager.Tab_GridLookupRepo.SearchByFieldAndValue(TabGridLookups, searchName, e.Filter, e.BeginIndex, e.EndIndex).AsQueryable();

            try
            {
                // verifico la presenza di un filtro da applicare ai dati
                string filterExpression = GetDefaultFilterExpression(TabGridLookups, searchName);

                // se il filtro è valorizzato allora lo applico alla query list
                if (!String.IsNullOrEmpty(filterExpression))
                {
                    //Se il campo Tab_Orari_Tipo_Id ha un filtro imopstato, lo completa con la relativa entity
                    if (searchName.Equals("Tab_Orari_Tipo_Id"))
                    {
                        string entity = "";
                        switch (comboBox.Page.AppRelativeVirtualPath.Split('/').Last())
                        {
                            case "ColPage.aspx":
                                entity = "Col";
                                break;
                            case "CantPage.aspx":
                                entity = "Can";
                                break;
                            default:
                                throw new Exception();

                        }
                        filterExpression = string.Format("{0}{1}{2}{1}", filterExpression, '"', entity);
                    }
                    else if (searchName.Equals("Col_Id"))
                    {
                        if (BusinessService.IsToApplyDomainFilter() && PowerWebContext.Current.User.Liv_Utente < 10)
                        {
                            // i filtri sono gerarchicamente strutturati:
                            // 1- utente/cliente o utente/collaboratore
                            // 2- utente/filiale o utente/responsabile
                            // La presenza di uno dei filtri di cui al punto 1 esclude quelli del punto due.
                            // In ogni caso i filtri per utente/cliente e utente/collaboratore sono mutuamente esclusivi (non posso sussistere contemporaneamente)

                            // se per l'utente è specificato un id collaboratore è richiesto di applicare un filtro per quel dato
                            if (PowerWebContext.Current.User.Col_Id.HasValue)
                            {
                                filterExpression = String.Format("Col_Id = {0}", PowerWebContext.Current.User.Col_Id);
                            }
                            else if (PowerWebContext.Current.User.Cli_Id.HasValue)
                            {
                                filterExpression = String.Format("(Cli_Id = {0} OR Att_Cli_Id = {0})", PowerWebContext.Current.User.Cli_Id);
                            }

                            #region Filtro per Filiale/Resp

                            else if (RepoManager.ParamRepo.ParametersRow.DomainFilterEnum != DomainFilterEnum.None) // altriementi si tenta di applicare, se configurato, i filtre per filiale e/o responsabile
                            {
                                //vengopno estratti i tutti responsabili

                                var allRespIds = RepoManager.RespRepo.GetAllQueryable(true).Select(resp => resp.Resp_Id).ToList();
                                var allFilIds = RepoManager.FilRepo.GetAllQueryable(true).Select(fill => fill.Fil_Id).ToList();

                                //viene estratto l'ide dello user che ha fatto l'accesso a Powerweb
                                int userId = PowerWebContext.Current.User.Utenti_Id;

                                //filtro solo i responsabili che corrispondo all'utente che ha effettuato l'accesso 
                                var userRespIds = RepoManager.Utenti_RespRepo.Find(r => r.Utenti_Id == userId).ToList();

                                //filtro solo le filiali che corrispondo all'utente che ha effettuato l'accesso 
                                var userFilIds = RepoManager.Utenti_FilRepo.Find(r => r.Utenti_Id == userId).ToList();

                                //inizializzazione della stringa che compone il filtro
                                StringBuilder tmpFilter = new StringBuilder();

                                #region Filtro per Filiale

                                //se ho delle filiali E dai parametri è richiesta la gestione delle filiali o entrambi allora viene fatto un filtro per le filiali
                                if (allFilIds.Any() && (RepoManager.ParamRepo.ParametersRow.DomainFilterEnum == DomainFilterEnum.Fil) && PowerWebContext.Current.User.Liv_Utente < 10)

                                {
                                    //istanzia la stringa che costituirà il filtro 
                                    tmpFilter.Append("((");

                                    var cantsQuery = RepoManager.CantRepo.DbSet.Where(RepoManager.CantRepo.Filter);         //Query filtro sui cantieri in base al filtro sulla filiale
                                    var regsQuery = RepoManager.RegRepo.DbSet.Where(RepoManager.RegRepo.Filter).Where(reg => reg.Col_Id != null);//Query filtro sulle reg in base al filtro sulla reg

                                    //Eseguo un join delle due query su cant_id ed estraggo solo i collaboratori
                                    //(In questo modo ho solo i collaboratori che hanno lavorato sui cantieri legati tramite filiale all'utente corrente)
                                    var colIds = cantsQuery.Join(regsQuery, cant => cant.Cant_Id, reg => reg.Cant_Id, (cant, reg) => reg.Col_Id).Distinct().ToList();

                                    int last = colIds.Count();

                                    for (int i = 0; i < last; i++)
                                    {
                                        tmpFilter.Append("Col_Id = " + colIds[i].Value);
                                        if (i + 1 != last)
                                            tmpFilter.Append(" OR ");
                                    }
                                }

                                #endregion

                                #region Filtro per responsabile
                                //se ho dei responsabili E dai parametri è richiesta la gestione dei responsabili 
                                else if (allRespIds.Any() && (RepoManager.ParamRepo.ParametersRow.DomainFilterEnum == DomainFilterEnum.Resp) && PowerWebContext.Current.User.Liv_Utente < 10)
                                {
                                    //nel caso si voglia gestire solo il responsabile devo ottenere tutti i reponsabili con resp diverso da 
                                    tmpFilter.Append("(Col_Id != null AND (");

                                    //viene cilato per ogni responabile
                                    foreach (int respId in allRespIds)
                                    {
                                        if (userRespIds.Any())
                                        {
                                            foreach (var userResp in userRespIds)
                                            {
                                                //se lo user che ha fatto l'accesso è fra i responsabili allora le RegV vengono filtrate su esso
                                                if (userResp.Resp_Id == respId)
                                                {
                                                    var Col_Id = RepoManager.ColRepo.Find(col => col.Resp_Id == respId).Select(c => c.Col_Id).ToList();

                                                    if (Col_Id.Any())
                                                    {
                                                        for (int i = 0; i < Col_Id.Count; i++)
                                                        {
                                                            if (i == 0)
                                                            {
                                                                if (!tmpFilter.ToString().Contains(Col_Id[i].ToString()))
                                                                    tmpFilter.AppendFormat("Col_Id = {0}", Col_Id[i]);
                                                            }
                                                            else
                                                            {
                                                                if (!tmpFilter.ToString().Contains(Col_Id[i].ToString()))
                                                                    //nel filtro viene fatta una or fra tutti i responsabili (anche quelli null)
                                                                    tmpFilter.AppendFormat(" OR Col_Id = {0}", Col_Id[i]);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion

                                tmpFilter.Append("))");

                                filterExpression = tmpFilter.ToString();
                            }
                            #endregion
                        }

                    }

                    queryList = queryList.Where(filterExpression);

                    filterExpression = string.Empty;

                }
            }
            catch (Exception ex)
            {
                // in caso di eccezione nel calcolo o applicazione del filtro viene comunque visualizzata tutta
                // la lista completa
            }

            //la sorgente dati del combobox è data dal risultato della query
            comboBox.DataSource = queryList;

            comboBox.DataBindItems();
        }

        /// <summary>
        /// Recupera il valore della filter expression specificato nella tab_gridlookup per il nome di ricerca specificato.
        /// </summary>
        /// <param name="tabgGirdLookups">L'elenco delle tab grid lookup con le tabelle configurate.</param>
        /// <param name="searchName">Il nome di ricerca del combobox.</param>
        /// <returns>La stringa contenente il filtro da applicare; se non impostato <see cref="String.Empty"/></returns>
        private static string GetDefaultFilterExpression(IEnumerable<Tab_GridLookup> tabgGirdLookups, string searchName)
        {
            // inizializzazione della stringa con il valore di filtro
            string defaultFilter = String.Empty;

            // recupera la tab grid lookup per il nome di ricerca (campo database)
            Tab_GridLookup fieldGridLookup = tabgGirdLookups.FirstOrDefault(tgl => tgl.Campo_Db && tgl.NomeRicerca == searchName);

            // se è stato trovato un campo database
            if (fieldGridLookup != default(Tab_GridLookup))
            {
                // se è valorizzato il filtro di default allora si prepara la stringa con lo stesso
                if (!String.IsNullOrEmpty(fieldGridLookup.DefaultFilterExpression))
                {
                    if (fieldGridLookup.Utenti_Id != null)
                    {
                        if (PowerWebContext.Current.User.Utenti_Id == fieldGridLookup.Utenti_Id)
                        {
                            defaultFilter = fieldGridLookup.DefaultFilterExpression;
                        }
                    }
                    else
                    {
                        defaultFilter = fieldGridLookup.DefaultFilterExpression;
                    }



                }


            }

            // ritorno del filtro calcolato dal metodo
            return defaultFilter;
        }

        private static string GetComboboxFormatString(List<Tab_GridLookup> gridLookups)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < gridLookups.Count(); i++)
            {
                if (gridLookups[i].Campo_Selezionato)
                {
                    result.Append("{").Append(i).Append("}");

                    if (i != gridLookups.Count() - 1)
                        result.Append(" ");
                }
            }

            return result.ToString();
        }

        public static void FillValues(Object entiy, OrderedDictionary newValues, OrderedDictionary oldValues)
        {
            FillValues(entiy, null, newValues, oldValues);
        }

        public static void FillValues(Object entiy, ICollection<MetaFieldDescriptor> metaFieldDescriptors, OrderedDictionary newValues, OrderedDictionary oldValues)
        {
            foreach (var property in entiy.GetType().GetProperties().Where(property => !newValues.Contains(property.Name) && (metaFieldDescriptors == null || metaFieldDescriptors.Any(met => met.FieldName == property.Name && met.IsVibile_EditFormSettings))))
            {
                newValues.Add(property.Name, property.GetValue(entiy, null));
                oldValues.Add(property.Name, property.GetValue(entiy, null));
            }
        }

        public static void FillGridProperties(Object entity, OrderedDictionary dictionary)
        {
            foreach (PropertyInfo property in entity.GetType().GetProperties())
            {
                Object currentValue = property.GetValue(entity, null);
                if (currentValue != null)
                {
                    dictionary.Add(property.Name, currentValue);
                }
            }
        }

        public static void FillGridClonedProperties(Page page, ASPxGridView gridView, OrderedDictionary dictionary)
        {
            GridMasterPage currentMaster = page.Master as GridMasterPage;
            if (currentMaster != null && currentMaster.GridClonedValues != null)
                foreach (GridViewDataColumn column in gridView.Columns.OfType<GridViewDataColumn>())
                    if (gridView.KeyFieldName != column.FieldName)
                        dictionary[column.FieldName] = currentMaster.GridClonedValues[column.FieldName];
        }

        public static void FillGridLabels(Type type, ASPxGridView grid)
        //Carica le Colonne delle Griglie (con le relative Labels in lingua )
        {
            foreach (var column in grid.Columns.OfType<GridViewDataColumn>())
                column.Caption = BusinessService.GetLocalizedString(column.FieldName, ResourceTypeEnum.Field);

            var dateTimeColumns = grid.Columns.OfType<GridViewDataDateColumn>();
            foreach (var col in dateTimeColumns)
            {
                //Tolta questa impostazione x poter scrivere le Date come si vuole (dd/mm/yyyy e/o dd/mm/yy 
                //ma anche e soprattutto per lasciare BLANK quando non la si scrive altrimenti a video si vedeva in INS 01/01/0100)
                //col.PropertiesDateEdit.UseMaskBehavior = true;

                if (col.PropertiesDateEdit.EditFormat == EditFormat.DateTime)
                {
                    String formatDateTime = Common.Properties.Settings.Default.DateTimeDisplayFormatString;
                    if (Thread.CurrentThread.CurrentCulture.Name.EndsWith("US"))
                        formatDateTime = Common.Properties.Settings.Default.DateTimeDisplayFormatString_en_US;
                    col.PropertiesDateEdit.DisplayFormatString = col.PropertiesDateEdit.EditFormatString = formatDateTime;
                }
            }
        }

        public static void GridHeaderFilterFillItems(ASPxGridViewHeaderFilterEventArgs e)
        // Gestione Filtri CUSTOM x DATA
        //USANDO BEETWEEN occorre SEMPRE Ricordarsi che occorre TOGLIERE un SECONDO dalla Data Fine Calcolata sempre su Domani
        // OGGI va dalle 00:00:00 di Oggi alle 23:59:59 di Oggi
        //Perciò la Data di FINE deve essere sempre OGGI + 1 GG - 1 Secondo!!
        {
            DateTime[] DateInterval = new DateTime[2];
            DateInterval[0] = DateTime.MinValue;
            DateInterval[1] = DateTime.MaxValue;
            e.Values.Clear();
            if (e.Column.Settings.HeaderFilterMode == HeaderFilterMode.List)
                e.AddShowAll();

            //Filtro a Oggi
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Oggi);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_OGGI), string.Empty,
               new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro a Ieri
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Ieri);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_IERI), string.Empty,
                new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sulla SETTIMANA CORRENTE (Fino a Oggi Incluso)(lun = 1)
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Settimana_Corrente_Fino_Al_Giorno_Incluso);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_SETTIMANA_CORRENTE_FINO_AL_GIORNO_INCLUSO), string.Empty,
              new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sulla SETTIMANA CORRENTE (Fino a Oggi Escluso)(lun = 1)
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Settimana_Corrente_Fino_Al_Giorno_Escluso);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_SETTIMANA_CORRENTE_FINO_AL_GIORNO_ESCLUSO), string.Empty,
              new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sulla SETTIMANA CORRENTE (Tutta la Settimana del Giorno) (lun = 1)                                        
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Settimana_Corrente_Del_Giorno);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_SETTIMANA_CORRENTE_DEL_GIORNO), string.Empty,
              new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sulla SETTIMANA Precedente alla Settimana del Giorno (lun = 1)                                      
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Settimana_Precedente_Alla_Settimana_Del_Giorno);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_SETTIMANA_PRECEDENTE_ALLA_SETTIMANA_DEL_GIORNO), string.Empty,
              new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sul MESE Corrente (fino a Oggi Compreso)            
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Mese_Corrente_Fino_Al_Giorno_Incluso);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_MESE_CORRENTE_FINO_AL_GIORNO_INCLUSO), string.Empty,
              new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sul MESE Corrente (fino a Oggi Escluso)            
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Settimana_Corrente_Fino_Al_Giorno_Escluso);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_MESE_CORRENTE_FINO_AL_GIORNO_ESCLUSO), string.Empty,
              new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sul MESE Corrente (Tutto il Mese del Giorno)
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Mese_Corrente_del_Giorno);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_MESE_CORRENTE_DEL_GIORNO), string.Empty,
              new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sul MESE Precedente
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Mese_Precedente_Al_Mese_del_Giorno);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_MESE_PRECEDENTE_AL_MESE_DEL_GIORNO), string.Empty,
              new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sul TRIMESTRE PRECEDENTE
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Trimestre_Precedente_Al_Mese_Del_Giorno);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_TRIMESTRE_PRECEDENTE_AL_MESE_DEL_GIORNO), string.Empty,
              new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sul SEMESTRE PRECEDENTE
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Semestre_Precedente_Al_Mese_Del_Giorno);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_SEMESTRE_PRECEDENTE_AL_MESE_DEL_GIORNO), string.Empty,
                new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sui ANNO CORRENTE (fino a Oggi INCLUSO)
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Anno_Corrente_Fino_Al_Giorno_Incluso);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_ANNO_CORRENTE_FINO_AL_GIORNO_INCLUSO), string.Empty,
                new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sui ANNO CORRENTE (fino a Oggi ESCLUSO)
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Anno_Corrente_Fino_Al_Giorno_Escluso);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_ANNO_CORRENTE_FINO_AL_GIORNO_ESCLUSO), string.Empty,
                new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sui ANNO CORRENTE (Tutto l'Anno del Giorno))
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Anno_Corrente_Del_Giorno);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_ANNO_CORRENTE_DEL_GIORNO), string.Empty,
                new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            //Filtro sull'ANNO PRECEDENTE
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Anno_Precedente_All_Anno_Del_Giorno);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_ANNO_PRECEDENTE_ALL_ANNO_DEL_GIORNO), string.Empty,
                new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            // Filtro a una Settimana da Oggi (Incluso)
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Una_Settimana_Dal_Giorno_Incluso);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_1_SETTIMANA_DAL_GIORNO_INCLUSO), string.Empty,
                new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            // Filtro a 1 Mese da Oggi (Incluso)
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Un_Mese_Dal_Giorno_Incluso);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_1_MESE_DAL_GIORNO_INCLUSO), string.Empty,
                new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            // Filtro a 3 Mesi da Oggi (Incluso)
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Tre_Mesi_Dal_Giorno_Incluso);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_3_MESI_DAL_GIORNO_INCLUSO), string.Empty,
                new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            // Filtro a 6 Mesi da Oggi (compreso)
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Sei_Mesi_Dal_Giorno_Incluso);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_6_MESI_DAL_GIORNO_INCLUSO), string.Empty,
                new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
            // Filtro a 1 Anno da Oggi (compreso)
            DateInterval = CommonService.ReturnDateInterval(DateTime.UtcNow.Date, TypoOfDateIntervalTypeEnum.Un_Anno_Dal_Giorno_Incluso);
            e.AddValue(BusinessService.GetLocalizedString(PowerWebResources.STR_FILTRO_A_1_ANNO_DAL_GIORNO_INCLUSO), string.Empty,
                new BetweenOperator(e.Column.FieldName, DateInterval[0], DateInterval[1]).ToString());
        }

        public static void GenerateGridColumns(ASPxGridView gridView, ICollection<MetaFieldDescriptor> metaFieldDescriptors)
        {
            foreach (var mfd in metaFieldDescriptors)
            {
                GridViewDataColumn col = null;

                if (mfd.IsVisible || mfd.IsVibile_EditFormSettings)
                {
                    if (mfd.GridViewDataType == (int)GridViewColumnTypeEnum.GridViewDataTextColumn)
                    {
                        col = new GridViewDataTextColumn();

                        ((GridViewDataTextColumn)col).PropertiesTextEdit.MaskSettings.Mask = mfd.Mask;
                        ((GridViewDataTextColumn)col).PropertiesTextEdit.MaskSettings.IncludeLiterals = (MaskIncludeLiteralsMode)mfd.IncludeLiterals;
                    }

                    if (mfd.GridViewDataType == (int)GridViewColumnTypeEnum.GridViewDataCheckColumn)
                        //Caso di CHECK BOX - NON faccio Nulla di Particolare
                        col = new GridViewDataCheckColumn();

                    if (mfd.GridViewDataType == (int)GridViewColumnTypeEnum.GridViewDataDateColumn)
                    //Caso di Date
                    //Imposto il Formato di EDIT uguale al valore del relativo campo EditFormat del DB                                                                             
                    {
                        col = new GridViewDataDateColumn();
                        ((GridViewDataDateColumn)col).PropertiesDateEdit.EditFormat = (EditFormat)mfd.EditFormat;

                        //NON imposto mai la maschera x i Campi con Edit Format 0 (Date) per poterla scrivere come si vuole gg/mm/SSAA , GG/MM/AA, G-m-AA ecc
                        //Imposto Automaticamente la Maschera "00:00" nei campi con EditFormat=2 (Time
                        //if (((GridViewDataDateColumn)col).PropertiesDateEdit.EditFormat == EditFormat.Time)
                        //  ((GridViewDataDateColumn)col).PropertiesDateEdit.EditFormatString = mfd.Mask;
                    }

                    if (mfd.GridViewDataType == (int)GridViewColumnTypeEnum.GridViewDataTimeSpanEditColumn)
                    // col = new GridViewDataTimeEditColumn();
                    {
                        col = new GridViewDataTextColumn();
                        ((GridViewDataTextColumn)col).PropertiesTextEdit.ClientSideEvents.Validation = "OnGridTimeSpanValidation";
                        ((GridViewDataTextColumn)col).PropertiesTextEdit.MaskSettings.Mask = "00:00";
                        ((GridViewDataTextColumn)col).PropertiesTextEdit.MaskSettings.IncludeLiterals = MaskIncludeLiteralsMode.None;
                    }

                    if (mfd.GridViewDataType == (int)GridViewColumnTypeEnum.GridViewDataComboBoxColumn)
                    {
                        col = new GridViewDataComboBoxColumn();
                    }

                    if (mfd.GridViewDataType == (int)GridViewColumnTypeEnum.GridViewDataSpinEditColumn)
                    {
                        col = new GridViewDataSpinEditColumn();

                        ((GridViewDataSpinEditColumn)col).PropertiesSpinEdit.DecimalPlaces = mfd.DecimalPlaces;
                    }

                    if (mfd.GridViewDataType == (int)GridViewColumnTypeEnum.GridViewDataTimeEditColumn)
                    {
                        col = new GridViewDataDateColumn();
                        ((GridViewDataDateColumn)col).PropertiesDateEdit.EditFormat = (EditFormat)mfd.EditFormat;
                        ((GridViewDataDateColumn)col).PropertiesDateEdit.EditFormatString = "HH:mm:ss";
                    }

                    col.Visible = mfd.IsVisible;
                    col.EditFormSettings.Visible = mfd.IsVibile_EditFormSettings ? DefaultBoolean.True : DefaultBoolean.False;
                    col.ShowInCustomizationForm = mfd.IsAvailable;
                    col.FieldName = mfd.FieldName;
                    col.Width = new Unit(mfd.Width);
                    col.PropertiesEdit.DisplayFormatString = mfd.DisplayFormatString;
                    col.CellStyle.HorizontalAlign = (HorizontalAlign)mfd.HorizontalAlign;
                    col.ReadOnly = mfd.IsReadOnly;
                    col.Settings.AllowHeaderFilter = mfd.AllowHeaderFilter ? DefaultBoolean.True : DefaultBoolean.False;
                    col.Settings.HeaderFilterMode = HeaderFilterMode.CheckedList;

                    if (mfd.VisibleIndex.HasValue)
                        col.VisibleIndex = mfd.VisibleIndex.Value;
                    else
                        col.Visible = false;
                }

                if (col != null)
                    gridView.Columns.Add(col);
            }


        }

        public static PowerFormTemplate GeneratePowerFormTemplate(BaseGridModule owner, Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic, ICollection<MetaFieldDescriptor> MetaFieldDescriptors)
        {
            var toDeleteTabs = new List<TabPageExtended>();

            foreach (var keyValuePair in templateDic)
            {
                foreach (MetaFieldDescriptor metaFieldDescriptor in MetaFieldDescriptors)
                    if (!metaFieldDescriptor.IsVibile_EditFormSettings)
                        keyValuePair.Value.RemoveAll(tpi => tpi.Field == metaFieldDescriptor.FieldName);

                var emptyFields = keyValuePair.Value.Where(tpi => tpi.Field == null).Count();

                if (keyValuePair.Value.Count == 0 || keyValuePair.Value.Count == emptyFields)
                    toDeleteTabs.Add(keyValuePair.Key);
            }

            foreach (var toDeleteTab in toDeleteTabs)
                templateDic.Remove(toDeleteTab);

            return new PowerFormTemplate(owner, templateDic);
        }

        public static string GenerateWhereQuery(Type entityType, ASPxGridView grid, string whereClause)
        {
            CriteriaOperator op = CriteriaOperator.Parse(grid.FilterExpression);
            var where = DevExpress.Data.Filtering.CriteriaToWhereClauseHelper.GetMsSqlWhere(op);

            var whereExpression = "";

            if (where != String.Empty && grid.FilterEnabled)
                whereExpression = String.Format(" WHERE {0}", where);

            if (whereExpression != String.Empty && whereClause != String.Empty)
                whereExpression = String.Format("{0} AND ({1})", whereExpression, whereClause);

            return String.Format("SELECT * FROM {0}{1}", entityType.Name, whereExpression);
        }

        #region Export GridView Util

        public static IList<Tuple<string, string, string>> GetGridViewVisibleFields(ASPxGridView grid)
        {
            var visibleFields = new List<Tuple<string, string, string>>();

            var dataColumns = grid.Columns.OfType<GridViewDataColumn>().Where(c => c.Visible).OrderBy(c => c.VisibleIndex);

            foreach (var col in dataColumns)
            {
                var comboBoxColumn = col as GridViewDataComboBoxColumn;
                if (comboBoxColumn != null)
                //  visibleFields.AddRange(TabGridLookups.Where(tgl => tgl.NomeRicerca == comboBoxColumn.FieldName && tgl.Campo_Selezionato).OrderBy(tgl => tgl.Ordinamento).Select(tgl => new Tuple<string, string, string>(tgl.NomeCampo, BusinessService.GetLocalizedString(tgl.Nome_Risorsa), string.Empty)));
                {
                    foreach (var tgl in TabGridLookups.Where(tgl => tgl.NomeRicerca == comboBoxColumn.FieldName && tgl.Campo_Selezionato).OrderBy(tgl => tgl.Ordinamento))
                    {
                        string visibleHeaderCaption = tgl.Nome_Risorsa == "FLD_CHIAVE_TAB" || tgl.Nome_Risorsa == "FLD_DECODIFICA_TAB"
                            ? BusinessService.GetLocalizedString(String.Format("{0} {1}", BusinessService.GetLocalizedString(tgl.Nome_Risorsa), col.Caption))
                            : BusinessService.GetLocalizedString(tgl.Nome_Risorsa);

                        string fieldName = String.Empty;
                        if (tgl.Nome_Risorsa == "FLD_CHIAVE_TAB" || tgl.Nome_Risorsa == "FLD_DECODIFICA_TAB")
                        {
                            switch (tgl.Nome_Risorsa)
                            {
                                case "FLD_CHIAVE_TAB":
                                    fieldName = col.FieldName;
                                    break;
                                case "FLD_DECODIFICA_TAB":
                                    fieldName = String.Format("TABDESC#{0}#{1}", tgl.NomeTab_Decod, col.FieldName);
                                    break;
                            }
                        }
                        else
                        {
                            fieldName = tgl.NomeCampo;
                        }

                        visibleFields.Add(new Tuple<string, string, string>(fieldName, visibleHeaderCaption, string.Empty));
                    }
                }
                else
                    visibleFields.Add(new Tuple<string, string, string>(col.FieldName, col.Caption, GetExcelFieldFormat(col)));
            }

            return visibleFields;
        }

        private static string GetExcelFieldFormat(GridViewDataColumn col)
        {
            var rtnValue = string.Empty;
            var fieldTypeString = ((IFilterablePropertyInfo)col).PropertyType.FullName;
            if (fieldTypeString == typeof(DateTime).FullName)
            {
                rtnValue = col.PropertiesEdit.DisplayFormatString;
                if (rtnValue == "d")
                    rtnValue = @"dd/mm/yyyy";
            }

            if (fieldTypeString == typeof(TimeSpan).FullName || col.Caption == "Du H" || col.Caption == "Du H-A")
                rtnValue = "hh:mm:ss";

            /*if (col.Caption == "Du H")
            {
                rtnValue = "hh:mm";
            }*/

            return rtnValue;
        }

        #endregion

    }

}