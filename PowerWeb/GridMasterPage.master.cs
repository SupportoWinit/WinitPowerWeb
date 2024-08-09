using Business;
using Business.Repository;
using Common;
using DevExpress.Data.Filtering;
using DevExpress.Utils;
using DevExpress.Web.ASPxCallbackPanel;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxEditors;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.ASPxGridView.Export;
using DevExpress.Web.ASPxTabControl;
using DevExpress.Web.ASPxTreeList;
using DevExpress.Web.Data;
using DevExpress.XtraPrinting;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraReports.Parameters;
using DevExpress.XtraReports.UI;
using Domain;
using Domain.Exceptions;
using Microsoft.Ajax.Utilities;
using PowerWeb.Modules;
using PowerWeb.Pages;
using Reports;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using ImageSizeMode = DevExpress.XtraPrinting.ImageSizeMode;

namespace PowerWeb
{
    public partial class GridMasterPage : MasterPage
    {
        const String TREEKEYFIELDNAME = "Id";

        Reg_V _regvStub = null;
        Domain.Fil _filStub = null;
        Domain.Resp _respStub = null;
        private int CustomizationVersion;

        public bool IsBatchModeEditing
        {
            get { return cbBatchMode.Checked; }
        }

        public List<ASPxButton> printList
        {
            get
            {
                List<ASPxButton> printButtons = new List<ASPxButton>();
                printButtons.Add(btnPrintXlsx);
                printButtons.Add(btnExportXLSX);

                return printButtons;
            }

        }

        public void HideAllMasterPageFeatures()
        {
            cmbLayout.Visible = false;
            //btnDeleteLayout.Visible = false;
            btnSaveLayout.Visible = false;
            btnSavePrintLayout.Visible = false;
            btnShowMap.Visible = false;
            btnPrintXlsx.Visible = false;
            btnPrint.Visible = false;
            btnExportXLSX.Visible = false;
            btnPrintPdf.Visible = false;
            btnHelp.Visible = false;
            btnPopulateGrid.Visible = false;
            btnCustomizeColumns.Visible = false;
            cbBatchMode.Visible = false;
            cbxExpandAll.Visible = false;
            btnUndo.Visible = false;
            btnUpdate.Visible = false;
        }

        /// <summary>
        /// Ritorna il dizionario degli utenti che hanno la filiale
        /// </summary>
        /// <value>
        /// The cant utenti fil dictionary.
        /// </value>
        public Dictionary<Int32, DomainEnum> CantUtentiFilDictionary
        {
            get
            {
                var cantUtentiFilDict = PowerWebContext.GetFromSession<Dictionary<Int32, DomainEnum>>("General_CantUtentiFilDictionary");
                if (cantUtentiFilDict == null)
                {
                    cantUtentiFilDict = new Dictionary<Int32, DomainEnum>();
                    PowerWebContext.SetToSession<Dictionary<Int32, DomainEnum>>("General_CantUtentiFilDictionary", cantUtentiFilDict);
                }
                return cantUtentiFilDict;
            }
        }
        public Dictionary<Int32, DomainEnum> ColUtentiRespDictionary
        {
            get
            {
                var colUtentiRespDict = PowerWebContext.GetFromSession<Dictionary<Int32, DomainEnum>>("General_ColUtentiRespDictionary");
                if (colUtentiRespDict == null)
                {
                    colUtentiRespDict = new Dictionary<Int32, DomainEnum>();
                    PowerWebContext.SetToSession<Dictionary<Int32, DomainEnum>>("General_ColUtentiRespDictionary", colUtentiRespDict);
                }
                return colUtentiRespDict;
            }
        }

        public Dictionary<Int32, DomainEnum> CliUntentiRespDictionary
        {
            get
            {
                var cliUtentiRespDict = PowerWebContext.GetFromSession<Dictionary<Int32, DomainEnum>>("General_CliUtentiRespDictionary");
                if (cliUtentiRespDict == null)
                {
                    cliUtentiRespDict = new Dictionary<Int32, DomainEnum>();
                    PowerWebContext.SetToSession<Dictionary<Int32, DomainEnum>>("General_CliUtentiRespDictionary", cliUtentiRespDict);
                }
                return cliUtentiRespDict;
            }
        }

        public List<Cant_Fil_V> CantFilVs
        {
            get
            {
                //viene creata una lista di oggetti Cant_Fil_V cioè tutta la lista dei cantieri che hanno delle filiali
                var cantfilVs = PowerWebContext.GetFromSession<List<Cant_Fil_V>>("General_CantFilVs");

                if (cantfilVs == null)
                {
                    cantfilVs = RepoManager.Cant_Fil_VRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cant_Fil_V>>("General_CantFilVs", cantfilVs);
                }
                return cantfilVs;
            }
        }
        public List<Col_Resp_V> ColRespVs
        {
            get
            {
                var colRespVs = PowerWebContext.GetFromSession<List<Col_Resp_V>>("General_ColRespVs");
                if (colRespVs == null)
                {
                    colRespVs = RepoManager.Col_Resp_VRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Col_Resp_V>>("General_ColRespVs", colRespVs);
                }
                return colRespVs;
            }
        }


        #region Carica il Tipo di Pagina corrente
        public IGridPage GridPage { get { return Page as IGridPage; } }
        public IPrintPage PrintPage { get { return Page as IPrintPage; } }
        public ILogPage LogPage { get { return Page as ILogPage; } }
        public IGeoLocationPage GeoLocationPage { get { return Page as IGeoLocationPage; } }
        public IExportXLSXPage ExportXLSXPage { get { return Page as IExportXLSXPage; } }

        #endregion

        #region Espone i relativi Moduli in base al Tipo Page
        public IGridModule GridModule
        {
            get
            {
                if (GridPage != null)
                    return GridPage.GridModule;
                return null;
            }
        }
        public IDoubleGridModule DoubleGridModule
        {
            get
            {
                if (GridPage != null)
                    return GridPage.DoubleGridModule;
                return null;
            }
        }
        public ITripleGridModule TripleGridModule
        {
            get
            {
                if (GridPage != null)
                    return GridPage.TripleGridModule;
                return null;
            }
        }
        public IQuadGridModule QuadGridModule
        {
            get
            {
                if (GridPage != null)
                    return GridPage.QuadGridModule;
                return null;
            }
        }
        #endregion

        #region Espone i Metodi di quel Modulo
        public BaseGridModule BaseGridModule
        {
            get
            {
                if (GridModule != null)
                    return GridModule as BaseGridModule;
                return null;
            }
        }
        public IPrintModule PrintModule
        {
            get
            {
                if (PrintPage != null)
                    return PrintPage.PrintModule;
                return null;
            }
        }
        public IPrintCustomModule PrintCustomModule
        {
            get
            {
                if (PrintModule != null)
                    return PrintPage.PrintModule as IPrintCustomModule;
                return null;
            }
        }
        public ILogModule LogModule
        {
            get
            {
                if (LogPage != null)
                    return LogPage.LogModule;
                return null;
            }
        }
        public IGeoLocationModule GeoLocationModule
        {
            get
            {
                if (GeoLocationPage != null)
                    return GeoLocationPage.GeoLocationModule;
                return null;
            }
        }
        public IExportXLSXModule ExportXLSXModule
        {
            get
            {
                if (ExportXLSXPage != null)
                    return ExportXLSXPage.ExportXLSXModule;
                return null;
            }
        }

        #endregion

        #region Espone gli Oggetti di quel Modulo
        public ASPxGridView GridView
        {
            get
            {
                if (GridModule != null)
                    return GridModule.GridView;
                return null;
            }
        }
        public ASPxGridView GridViewDetail
        {
            get
            {
                if (GridModule != null)
                    return GridModule.GridViewDetail;
                return null;
            }
        }
        #endregion

        #region Carica in Sessione I Dati che poi usa
        public Dictionary<String, Type> GridModuleTypes
        {
            get
            {
                Dictionary<String, Type> currentDic = PowerWebContext.GetFromSession<Dictionary<String, Type>>("GridModuleTypes_" + GridView.ID);
                if (currentDic == null)
                {
                    currentDic = new Dictionary<string, Type>();
                    foreach (PropertyInfo propertyInfo in GridPage.GridModule.EntityType.GetProperties())
                    {
                        if (propertyInfo.PropertyType == typeof(Nullable<TimeSpan>) || propertyInfo.PropertyType == typeof(TimeSpan))
                            currentDic.Add(propertyInfo.Name, propertyInfo.PropertyType);
                    }
                }
                return currentDic;
            }
            set
            {
                PowerWebContext.SetToSession<Dictionary<String, Type>>("GridModuleTypes_" + GridView.ID, value);
            }
        }
        public Dictionary<String, Type> DetailGridModuleTypes
        {
            get
            {
                Dictionary<String, Type> currentDic = PowerWebContext.GetFromSession<Dictionary<String, Type>>("DetailGridModuleTypes_" + GridView.ID);
                if (currentDic == null && GridPage.GridModule.DetailGridEntityType != null)
                {
                    currentDic = new Dictionary<string, Type>();
                    foreach (PropertyInfo propertyInfo in GridPage.GridModule.DetailGridEntityType.GetProperties())
                    {
                        if (propertyInfo.PropertyType == typeof(Nullable<TimeSpan>) || propertyInfo.PropertyType == typeof(TimeSpan))
                            currentDic.Add(propertyInfo.Name, propertyInfo.PropertyType);
                    }
                }
                return currentDic;
            }
            set
            {
                PowerWebContext.SetToSession<Dictionary<String, Type>>("DetailGridModuleTypes_" + GridView.ID, value);
            }
        }
        private Tab_Funz _currentPageTabFunz;
        public Tab_Funz CurrentPageTabFunz
        {
            get
            {
                string[] path = HttpContext.Current.Request.Url.AbsolutePath.Split('/');
                var pages = path[path.Length - 2];
                var module = path[path.Length - 1];
                string absolutePath = "/" + pages + "/" + module;
                if (_currentPageTabFunz == null)
                    _currentPageTabFunz = PowerWebContext.Current.TabFunzs.Single(tf => tf.Link_Tab_Funz.EndsWith(absolutePath));
                return _currentPageTabFunz;
            }
        }
        private Tab_Aut _currentPageTabAut;
        public Tab_Aut CurrentPageTabAut
        {
            get
            {
                if (_currentPageTabAut == null)
                {
                    //legge l'eventuale Record con Chiave Utente/Funzione
                    _currentPageTabAut = PowerWebContext.Current.TabAuts.SingleOrDefault(ta => ta.Utenti_Id == PowerWebContext.Current.User.Utenti_Id && ta.Tab_Funz_Id == CurrentPageTabFunz.Tab_Funz_Id);

                    if (_currentPageTabAut == null)
                        //Se NON esiste il REcord con Chiave Utente/Funzione e NON esiste nemmeno il Record con Chiave = UTENTE
                        //legge il Record con Chiave FUNZIONE
                        _currentPageTabAut = PowerWebContext.Current.TabAuts.SingleOrDefault(ta => ta.Utenti_Id == null && ta.Tab_Funz_Id == CurrentPageTabFunz.Tab_Funz_Id);
                }

                return _currentPageTabAut;
            }
        }
        private List<GroupingTreeListItem> GroupingTreeData
        {
            get
            {
                ASPxGridView printGridView = GetPrintGrid();
                PowerFormTemplate printFormTemplate = GetPrintFormTemplate();
                List<GroupingTreeListItem> itemTreeListFromSession = PowerWebContext.GetFromSession<List<GroupingTreeListItem>>("GroupingTreeData_" + printGridView.ID);

                if (itemTreeListFromSession == null)
                {
                    itemTreeListFromSession = new List<GroupingTreeListItem>();

                    if (printFormTemplate != null)
                    {
                        List<String> fieldList = new List<string>();


                        foreach (KeyValuePair<TabPageExtended, List<TabPageItemExtended>> keyValuePairString in printFormTemplate.Dic)
                            foreach (TabPageItemExtended item in keyValuePairString.Value)
                                if ((item.Type & TabPageItemFieldTypeEnum.NotInGroup) != TabPageItemFieldTypeEnum.NotInGroup && (item.Type & TabPageItemFieldTypeEnum.EmptyField) != TabPageItemFieldTypeEnum.EmptyField)
                                    fieldList.Add(item.Field);


                        for (int i = 0; i < fieldList.Count; i++)
                        {
                            itemTreeListFromSession.Add(new GroupingTreeListItem
                            {
                                Id = i,
                                Field = fieldList[i],
                                Caption = BusinessService.GetLocalizedString(fieldList[i], ResourceTypeEnum.Field),
                                ParentId = -1
                            });
                        }

                        itemTreeListFromSession = itemTreeListFromSession.OrderBy(it => it.Caption).ToList();

                        var rootItem = new GroupingTreeListItem
                        {
                            Id = -2,
                            Field = "Root",
                            Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_CRITERIO_RAGGRUPPAMENTO),
                            ParentId = -1,
                            IsInGrouping = true,
                        };

                        itemTreeListFromSession.Insert(0, rootItem);

                    }

                    //Se sono nel cartellino, faccio riferimento alla GridView del PrintModule (quindi il collaboratore)
                    if (PrintModule != null && PrintModule.PrintFormTemplate != null && PrintModule.PrintFormTemplate.BaseModule != null && PrintModule.PrintFormTemplate.BaseModule.ID == "mdlTimesheetModule")
                    {
                        PowerWebContext.SetToSession<List<GroupingTreeListItem>>("GroupingTreeData_" + printGridView.ID, itemTreeListFromSession);
                    }

                    //In ogni altro caso, faccio riferimento alla GridView del GridModule
                    else
                    {
                        PowerWebContext.SetToSession<List<GroupingTreeListItem>>("GroupingTreeData_" + GridView.ID, itemTreeListFromSession);
                    }
                }

                return itemTreeListFromSession;
            }
        }

        #endregion

        #region Metodi PRIVATI delle Master Page

        private void BindCheckBoxList()
        {
            PowerFormTemplate printFormTemplate = GetPrintFormTemplate();

            if (printFormTemplate != null)
            {
                if (printFormTemplate.IsShowSelection)
                {
                    lbPrintOptions.Items.Clear();

                    // inserimento dei tab della form nel combobox di selezione del report
                    foreach (KeyValuePair<TabPageExtended, List<TabPageItemExtended>> keyValuePairString in printFormTemplate.Dic)
                    {
                        TabPageExtended currentTab = keyValuePairString.Key;
                        lbPrintOptions.Items.Add(new ListEditItem(currentTab.Caption, currentTab.Id));
                    }

                    // ricalcolo le opzioni e i tab se ho un report calcolato
                    if (cmbPrintLayout.ClientValue != null || cmbPrintLayout.SelectedItem != null)
                    {
                        // recupero il nome della tabella delle opzioni report dai settings
                        var tableName = Common.Properties.Settings.Default.OpzReportTableName;

                        // calcolo la prima parte della chiave della tabella utilizzando l'id del report attualmente selezionato
                        var selectedReportId = cmbPrintLayout.SelectedItem != null ? Convert.ToInt32(cmbPrintLayout.SelectedItem.Value) : 0;
                        if (selectedReportId == 0)
                            selectedReportId = cmbPrintLayout.ClientValue != null ? Convert.ToInt32(!String.IsNullOrEmpty(cmbPrintLayout.ClientValue.ToString()) ? cmbPrintLayout.ClientValue : 0) : 0;
                        var report = RepoManager.Tab_ReportRepo.FirstOrDefault(rep => rep.Report_Id == selectedReportId);
                        if (report != null)
                        {
                            var tableKey = report.Nome_Risorsa;

                            // una volta ottenuta la prima parte della chiave di ricerca della tabella, recupero tutti record della tab_decod che
                            // hanno il nome tabella e iniziano con la chiave
                            var reportOptions = RepoManager.Tab_DecodRepo.Find(td => td.Nome_Tab == tableName && td.Chiave_Tab.StartsWith(tableKey)).ToList();

                            // se sono state trovate opzioni per il report
                            if (reportOptions.Any())
                            {
                                reportOptions.ForEach(opt => lbPrintOptions.Items.Add(new ListEditItem(BusinessService.GetLocalizedString(opt.Decodifica_Tab), opt.Decodifica_Tab)));
                                var currentOptions = reportOptions.ToDictionary(opt => opt.Decodifica_Tab, opt => BusinessService.GetLocalizedString(opt.Decodifica_Tab));
                                PowerWebContext.SetToSession<Dictionary<string, string>>("ReportOptions_" + GridView.ID, currentOptions);
                            }
                        }
                    }
                    else
                    {
                        // in caso di mancanza del valore del report selezionato ripropongo i valori precedentemente iseriti
                        if (PowerWebContext.GetFromSession<Dictionary<string, string>>("ReportOptions_" + GridView.ID) != null)
                            PowerWebContext.GetFromSession<Dictionary<string, string>>("ReportOptions_" + GridView.ID).ForEach(opt => lbPrintOptions.Items.Add(new ListEditItem(opt.Value, opt.Key)));
                    }

                    lbPrintOptions.DataBind();
                }
            }
        }

        private void BindGroupingTree()
        {
            tlGrouping.KeyFieldName = TREEKEYFIELDNAME;
            tlGrouping.DataSource = GroupingTreeData;
            tlGrouping.DataBind();
            tlGrouping.ExpandAll();
        }

        private void BindLayoutCombo(bool isSetToDefault = false)
        {
            cmbLayout.Items.Clear();

            //viene controllato se per la pagina in questione vi sono dei layout fatti dall'utente
            List<Tab_DataGrid> userDataGrid = RepoManager.Tab_DataGridRepo.Find(tdg => tdg.Nome_DataGrid == GridView.ID
                 && tdg.Utenti_Id != null).ToList();



            //se l'utente non è di livello admin non viene caricata la vista di default
            if (PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level || !userDataGrid.Any())
            //Se l'utente ha un Livello >= al Livello di Admin definito in Tab Param allora il Layout veine cercato SENZA UTENTE
            {
                cmbLayout.Items.Add(new ListEditItem("Default", -1));


            }

            List<Tab_DataGrid> listLayout = null;

            if (PowerWebContext.Current.User.Codice_Utente != "WINIT")
            {
                if (RepoManager.ParamRepo.IsCurrentUserCustomizationEnabled(CustomizationEnum.OnlyUserDefinedViews, PowerWebContext.Current.User.Codice_Utente))
                {
                    listLayout = RepoManager.Tab_DataGridRepo.Find(tdg => tdg.Nome_DataGrid == GridView.ID && (tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id)).OrderBy(tgd => tgd.Nome_Layout).ToList();
                }
                else
                {
                    listLayout = RepoManager.Tab_DataGridRepo.Find(tdg => tdg.Nome_DataGrid == GridView.ID && (tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id || tdg.Utenti_Id == null)).OrderBy(tgd => tgd.Nome_Layout).ToList();
                }
            }
            else {
                listLayout = RepoManager.Tab_DataGridRepo.Find(tdg => tdg.Nome_DataGrid == GridView.ID).OrderBy(tgd => tgd.Nome_Layout).DistinctBy(list => list.Nome_Layout).ToList();
            }
            


            Tab_DataGrid currentLayout = null;

            foreach (Tab_DataGrid item in listLayout)
            {

                ListEditItem newListEditItem = new ListEditItem(item.Nome_Layout, item.DataGrid_Id);

                cmbLayout.Items.Add(newListEditItem);

                if (item.Utenti_Id != null)
                    newListEditItem.ImageUrl = CommonService.BaseSiteUrl + "Icons/User/User.png";
            }

            currentLayout = listLayout.OrderByDescending(v => v.Data_Layout_DataGrid).FirstOrDefault() ?? null;
            cmbLayout.DataBindItems();

            if (cmbLayout.Items.Count > 0)
            {
                if (!Page.IsPostBack || isSetToDefault)
                {
                    cmbLayout.SelectedIndex = 0;
                    if (currentLayout != null)
                    {
                        CustomizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutomaticDateTimeView);
                        //se la personalizzazione non è attiva
                        if (CustomizationVersion != 0)
                        {
                            UpdateDataRegInLayout(currentLayout);
                        }

                        GridView.LoadClientLayout(currentLayout.Layout_DataGrid);

                        var currentLayout2 = DoubleGridModule != null ? RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == DoubleGridModule.GridView2.ID && currentLayout.Nome_Layout == tdg.Nome_Layout) : null;
                        var currentLayout3 = TripleGridModule != null ? RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == TripleGridModule.GridView3.ID && currentLayout.Nome_Layout == tdg.Nome_Layout) : null;
                        var currentLayout4 = QuadGridModule != null ? RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == QuadGridModule.GridView4.ID && currentLayout.Nome_Layout == tdg.Nome_Layout) : null;

                        ManageDoubleAndTripleGridLayout(currentLayout2, currentLayout3, currentLayout4);
                        cmbLayout.SelectedIndex = listLayout.IndexOf(currentLayout) + 1;
                    }
                    else // in caso sia rimasto attivo solamente il layout di default, si carica quello
                    {
                        GridView.LoadClientLayout(PowerWebContext.GetFromSession<String>("GridLayout_" + GridView.ID));

                        ManageDoubleAndTripleGridLayout(null, null, null);
                    }
                }
            }
        }

        /// <summary>
        /// Aggiorna con le date di mese precedente e mese corrente, se si tratta di un elenco di registrazione creato da amministratori,
        /// il filtro della griglia il cui layout è passato come parametro.
        /// </summary>
        /// <param name="currentLayout">Il layout (salvato nella tabella Tab_DataGrid da processare.</param>
        private void UpdateDataRegInLayout(Tab_DataGrid currentLayout)
        {
            // se si sta processando una griglia di manutenzionzione timbrature e il layout è stato generato da un amministratore
            if ((currentLayout.Nome_DataGrid == "gvRegV" || currentLayout.Nome_DataGrid == "gvRegVM" || currentLayout.Nome_DataGrid == "gvWhereIsIt"))
            {
                // calcolo del nome del layout corrente
                string nomeLayout = currentLayout.Nome_Layout.ToUpper();

                // viene eseguita la modifica delle date solamente se nel nome del layout non compare la parola fiss (per periodi fissi, date fisse ecc.)
                if (!nomeLayout.Contains("FISS"))
                {
                    #region LAYOUT MESE CORRENTE/PRECENRE

                    // calcolo delle date limite di mese corrente e mese precedente
                    var currDate = DateTime.Now;

                    DateTime prevMonth = CommonService.GetFirstMonthDay(currDate.AddMonths(-1));
                    DateTime mesePrevUltimoGg = CommonService.GetLastMonthDay(currDate.AddMonths(-1));

                    DateTime meseCorrenteUltimoGg = CommonService.GetLastMonthDay(currDate);
                    DateTime meseCorrenteUltimoGgpiu1 = meseCorrenteUltimoGg.AddDays(1);
                    DateTime meseCorrentePrimoGg = CommonService.GetFirstMonthDay(currDate);
                    DateTime yesterday = CommonService.Yestarday(currDate);

                    // calcolo in formato stringa delle date limite di mese corrente e mese precedente
                    var currMonthString = currDate.ToString("yyyy-MM-dd");
                    var prevMonthString = prevMonth.ToString("yyyy-MM-dd");
                    var ultimoGiorno = meseCorrenteUltimoGgpiu1.ToString("yyyy-MM-dd");
                    var meseCorrenteUltimoGgString = meseCorrenteUltimoGg.ToString("yyyy-MM-dd");
                    var meseCorrentePrimoGgString = meseCorrentePrimoGg.ToString("yyyy-MM-dd");
                    var mesePrevUltimoGgString = mesePrevUltimoGg.ToString("yyyy-MM-dd");
                    var now = currDate.ToString("yyyy-MM-dd");
                    var ieri = yesterday.ToString("yyyy-MM-dd");
                    var current = currDate.ToString("yy-MM-dd");

                    // calcolo del layout corrente
                    var currentGridLayout = currentLayout.Layout_DataGrid;

                    // calcolo della posizione della data (nome del campo) nella stringa di filtro della griglia (data inizio)

                    string dataLabelResources = "";

                    string dataLabelDb = "";

                    if (currentGridLayout.IndexOf(CommonService.GetPropertyName(() => _regvStub.Data_Reg)) >= 0)
                    {
                        dataLabelResources = BusinessService.GetLocalizedString(CommonService.GetPropertyName(() => _regvStub.Data_Reg), ResourceTypeEnum.Field);
                        dataLabelDb = CommonService.GetPropertyName(() => _regvStub.Data_Reg);
                    }
                    else if (currentGridLayout.IndexOf(CommonService.GetPropertyName(() => new Col().WhereIsItDate)) >= 0)
                    {
                        dataLabelResources = BusinessService.GetLocalizedString(CommonService.GetPropertyName(() => new Col().WhereIsItDate), ResourceTypeEnum.Field);
                        dataLabelDb = CommonService.GetPropertyName(() => new Col().WhereIsItDate);
                    }

                    if (String.IsNullOrEmpty(dataLabelDb))
                        return;

                    var dataRegIndex = currentGridLayout.IndexOf(dataLabelDb);

                    int mese = 0;
                    // se il campo data è presente nel filtro
                    if (dataRegIndex != -1)
                    {
                        // calcolo della posizione di partenza del valore data nella stringa filtro
                        var dateIndex = currentGridLayout.Substring(dataRegIndex).IndexOf("#");
                        dateIndex = dataRegIndex + dateIndex;

                        // romozione della data attuale e sostituzione con il mese precedente
                        currentGridLayout = currentGridLayout.Remove(dateIndex + 1, 10);

                        //controllo che all'interno della vista vi sia la stringa corrente o precedente
                        if (nomeLayout.Contains("CORRENTE"))
                        {
                            currentGridLayout = currentGridLayout.Insert(dateIndex + 1, meseCorrentePrimoGgString); //prevMonthString);
                        }
                        else if (nomeLayout.Contains("MESE PRECEDENTE") || nomeLayout.Contains("MESE-PRECEDENTE"))
                        {
                            int index = nomeLayout.IndexOf("MESE-PRECEDENTE") & nomeLayout.IndexOf("MESE PRECEDENTE");
                            nomeLayout.Substring(0, index);
                            string[] splitted = nomeLayout.Substring(0, index).Trim().Trim('-').Split(' ', '-');

                            if (splitted.Length > 0)
                            {
                                string mesi = splitted[splitted.Length - 1];

                                if (!int.TryParse(mesi, out mese))
                                {
                                    mese = 1;
                                }

                                DateTime xPrevMonth = CommonService.GetFirstMonthDay(currDate.AddMonths(-mese));
                                currentGridLayout = currentGridLayout.Insert(dateIndex + 1, xPrevMonth.ToString("yyyy-MM-dd"));
                            }

                        }
                        else if (nomeLayout.Contains("PRECEDENTE"))
                        {
                            currentGridLayout = currentGridLayout.Insert(dateIndex + 1, prevMonthString);
                        }
                        else if (nomeLayout.Contains("OGGI"))
                        {
                            currentGridLayout = currentGridLayout.Insert(dateIndex + 1, now);
                        }
                        else if (nomeLayout.Contains("IERI"))
                        {
                            currentGridLayout = currentGridLayout.Insert(dateIndex + 1, ieri);
                        }
                        else if (nomeLayout.Contains("ATTUALE"))
                        {
                            currentGridLayout = currentGridLayout.Insert(dateIndex + 1, current);
                        }
                        else
                        {
                            currentGridLayout = currentGridLayout.Insert(dateIndex + 1, prevMonthString);
                        }

                    }

                    // calcolo della posizione della data (nome del campo) nella string di filtro della griglia (data fine)
                    var lastdataRegIndex = currentGridLayout.LastIndexOf(dataLabelDb);

                    // se il campo data fine è presente e non si tratta del valore di data inizio
                    if (lastdataRegIndex != -1 && lastdataRegIndex != dataRegIndex)
                    {
                        // calcolo della posizione di partenza del valore data nella stringa filtro
                        var dateIndex = currentGridLayout.Substring(lastdataRegIndex).IndexOf("#");
                        dateIndex = lastdataRegIndex + dateIndex;

                        // sostituzione della data attuale con il mese corrente o il mese indicato se si tratta di mesi precedenti

                        currentGridLayout = currentGridLayout.Remove(dateIndex + 1, 10);
                        currentGridLayout = currentGridLayout.Insert(dateIndex + 1, (nomeLayout.Contains("PRECEDENTE")) ? CommonService.GetLastMonthDay(currDate.AddMonths(-mese)).ToString("yyyy-MM-dd") : currMonthString);
                    }
                    else // se invece la data di fine non è presente
                    {
                        // se era stata trovata una data di inizio
                        if (dataRegIndex != -1)
                        {
                            // se nella stringa di filtro è presente una condizione di between e si tratta dei primi caratteri
                            var betweenIndex = currentGridLayout.Substring(dataRegIndex).IndexOf("Between");
                            if (betweenIndex != -1 && betweenIndex <= dataLabelDb.Count() + 3)
                            {
                                // si calcola la posizione di inzio della data e di fine e si sostituisce la data di fine con il mese corrente
                                var dateIndex = currentGridLayout.Substring(betweenIndex + dataRegIndex).IndexOf("#");
                                dateIndex = dataRegIndex + betweenIndex + dateIndex + 14;
                                currentGridLayout = currentGridLayout.Remove(dateIndex + 1, 10);

                                if (nomeLayout.Contains("CORRENTE"))
                                {
                                    currentGridLayout = currentGridLayout.Insert(dateIndex + 1, ultimoGiorno); //prevMonthString);
                                }
                                else if (nomeLayout.Contains("MESE PRECEDENTE") || nomeLayout.Contains("MESE-PRECEDENTE"))
                                {
                                    int index = nomeLayout.IndexOf("MESE-PRECEDENTE") & nomeLayout.IndexOf("MESE PRECEDENTE");
                                    nomeLayout.Substring(0, index);
                                    string[] splitted = nomeLayout.Substring(0, index).Trim().Trim('-').Split(' ', '-');
                                    if (splitted.Length > 0)
                                    {
                                        string mesi = splitted[splitted.Length - 1];
                                        if (!int.TryParse(mesi, out mese))
                                        {
                                            mese = 1;
                                        }
                                        DateTime xPrevMonth = CommonService.GetLastMonthDay(currDate.AddMonths(-mese));
                                        currentGridLayout = currentGridLayout.Insert(dateIndex + 1, xPrevMonth.ToString("yyyy-MM-dd"));
                                    }

                                }
                                else if (nomeLayout.Contains("PRECEDENTE"))
                                {
                                    currentGridLayout = currentGridLayout.Insert(dateIndex + 1, mesePrevUltimoGgString);

                                }
                                else if (nomeLayout.Contains("OGGI"))
                                {
                                    currentGridLayout = currentGridLayout.Insert(dateIndex + 1, now);
                                }
                                else if (nomeLayout.Contains("IERI"))
                                {
                                    currentGridLayout = currentGridLayout.Insert(dateIndex + 1, ieri);
                                }
                                else
                                {
                                    currentGridLayout = currentGridLayout.Insert(dateIndex + 1, currMonthString);
                                }
                            }
                        }
                    }

                    // impostazione del layout con filtro calcolato sulla griglia
                    currentLayout.Layout_DataGrid = currentGridLayout;

                    // aggiornamento del layout nella tabella data grid
                    RepoManager.Tab_DataGridRepo.Update(currentLayout, true);

                    #endregion
                }

            }
        }

        private void BindPrintLayoutCombo(bool isSetToDefault = false)
        {
            cmbPrintLayout.Items.Clear();

            var printGrid = GetPrintGrid();

            List<Tab_Report> reports = RepoManager.Tab_ReportRepo.Find(trr => trr.Nome_DataGrid == printGrid.ID && (trr.Utenti_Id == null || trr.Utenti_Id == PowerWebContext.Current.User.Utenti_Id)).OrderBy(trr => trr.Nome_Report).ToList();

            Tab_Report currentReport = null;

            foreach (Tab_Report item in reports)
            {
                if (item.Utenti_Id == null && currentReport == null)
                    currentReport = item;

                if (item.Utenti_Id != null && currentReport != null &&
                    item.Data_Layout_Report > currentReport.Data_Layout_Report)
                    currentReport = item;

                if (item.Utenti_Id != null && currentReport == null)
                    currentReport = item;

                String resourceValue = BusinessService.GetLocalizedString(item.Nome_Risorsa);

                ListEditItem newListEditItem = new ListEditItem(String.Format("{0} - {1}", resourceValue, item.Nome_Report), item.Report_Id);
                cmbPrintLayout.Items.Add(newListEditItem);
                if (item.Utenti_Id != null)
                    newListEditItem.ImageUrl = CommonService.BaseSiteUrl + "Icons/User/User.png";
            }

            cmbPrintLayout.DataBindItems();

            if (!Page.IsPostBack || isSetToDefault)
            {
                if (currentReport != null)
                {
                    cmbPrintLayout.SelectedIndex = reports.IndexOf(currentReport);
                    LoadPrintLayout(currentReport);
                }
            }

            if (cmbPrintLayout.Items.Count > 0)
            {
                ASPxButton btnPrint = pcPrint.FindControl("btnPrintReport") as ASPxButton;
                if (btnPrint != null)
                    btnPrint.Enabled = true;
            }
        }

        /// <summary>
        /// Recupera la griglia utilizzata nella stampa per l'attuale pagina.
        /// </summary>
        /// <returns>La griglia utilizzata nella stampa per l'attuale pagina.</returns>
        private ASPxGridView GetPrintGrid()
        {
            // di default la griglia di stampa è quella principale del modulo;
            // ma se print module ne specifica un'altra allora viene presa quella
            ASPxGridView printGrid = GridView;
            if (PrintModule != null)
                if (PrintModule.PrintGridView != null)
                    printGrid = PrintModule.PrintGridView;
            return printGrid;
        }


        /// <summary>
        /// Recupera la griglia utilizzata nell'export per l'attuale pagina.
        /// </summary>
        /// <returns>La griglia utilizzata nella stampa per l'attuale pagina.</returns>
        private ASPxGridView GetExportGrid()
        {
            // di default la griglia di export è quella principale del modulo;
            // ma se l'export module ne specifica un'altra allora viene presa quella
            ASPxGridView exportGrid = GridView;
            if (ExportXLSXModule != null)
                if (ExportXLSXModule.ExportGridView != null)
                    exportGrid = ExportXLSXModule.ExportGridView;
            return exportGrid;
        }

        /// <summary>
        /// Recupera il form template utilizzato nella stampa per l'attuale pagina.
        /// </summary>
        /// <returns>Il form template utilizzato nella stampa per l'attuale pagina.</returns>
        private PowerFormTemplate GetPrintFormTemplate()
        {
            // di default il form template è quello del modulo;
            // ma se il print module ne specifica un altro allora si utilizza quello
            PowerFormTemplate printFormTemplate = GridPage.GridModule.EditFormTemplate;
            if (PrintModule != null)
            {
                //Nel cartellino voglio poter raggruppare per collaboratore, quindi non sovvrascrivo il PrintFormTemplate
                if (PrintModule.PrintFormTemplate != null && PrintModule.PrintFormTemplate.BaseModule.ID != "mdlTimesheetModule")
                {
                    printFormTemplate = PrintModule.PrintFormTemplate;
                }
            }
            return printFormTemplate;
        }

        private void BindExportLayoutCombo(bool isSetToDefault = false)
        {
            cmbExportXLSXLayout.Items.Clear();

            // dai modelli su cui ciclo per impostare il combo dell'export tolgo tutti i modelli custom,
            // cioè quelli lanciabili dalla maschera di export registrazioni (che cioè hanno un tipo selezione valorizzato)

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.G4) == 0 || PowerWebContext.Current.User.Liv_Utente >= 10)
            {
                foreach (Tab_Excel_Model exportExcelModel in ExportXLSXModule.Models.Where(model => model.Tipo_Selezione == null))
                {
                    String resourceValue = BusinessService.GetLocalizedString(exportExcelModel.Nome_Risorsa);

                    ListEditItem newListEditItem = new ListEditItem(resourceValue, exportExcelModel.ExcelModel_Id);
                    cmbExportXLSXLayout.Items.Add(newListEditItem);
                }
            }
            else
            {
                var print = ExportXLSXModule.Models.First(c => c.Nome_Risorsa == "EXP_EXCELRILEVAZIONI_BASE");

                String resourceValue = BusinessService.GetLocalizedString(print.Nome_Risorsa);

                ListEditItem newListEditItem = new ListEditItem(resourceValue, print.ExcelModel_Id);

                cmbExportXLSXLayout.Items.Add(newListEditItem);
            }



            cmbExportXLSXLayout.DataBindItems();

            if (!Page.IsPostBack || isSetToDefault)
                cmbExportXLSXLayout.SelectedIndex = 0;

            if (cmbExportXLSXLayout.Items.Count > 0)
            {
                ASPxButton btnExport = pcExportXLSX.FindControl("btnExportXLSXLaunch") as ASPxButton;
                if (btnExport != null)
                    btnExport.Enabled = GridView.VisibleRowCount > 0;

            }
        }

        #endregion

        private void SetGridViewEditingMode()
        {
            GridView.SettingsEditing.Mode = IsBatchModeEditing ? GridViewEditingMode.Batch : GridViewEditingMode.EditForm;
            if (IsBatchModeEditing)
                GridView.Settings.ShowStatusBar = GridViewStatusBarMode.Hidden;



            btnUpdate.Visible = IsBatchModeEditing;
            btnUndo.Visible = IsBatchModeEditing;


        }

        #region Page_Load/Page_Init Comuni a Tutte le Pagine

        protected void Page_Load(object sender, EventArgs e)
        //Imposta Settaggio Modo di Editing della Pagina in base la Valore del Flag Dflt_Grid_Edit_Mode della Singola Funzione in tab_FUNZ
        {
            if (GridPage != null && GridView != null)
            {
                if (!Page.IsPostBack && !Page.IsCallback)
                {
                    PowerWebContext.SetToSession<String>("GridLayout_" + GridView.ID, GridView.SaveClientLayout());
                    if (DoubleGridModule != null)
                        PowerWebContext.SetToSession<String>("GridLayout2_" + DoubleGridModule.GridView2.ID, DoubleGridModule.GridView2.SaveClientLayout());
                    if (TripleGridModule != null)
                        PowerWebContext.SetToSession<String>("GridLayout3_" + TripleGridModule.GridView3.ID, TripleGridModule.GridView3.SaveClientLayout());
                    if (QuadGridModule != null)
                        PowerWebContext.SetToSession<String>("GridLayout4_" + QuadGridModule.GridView4.ID, QuadGridModule.GridView4.SaveClientLayout());
                    BindLayoutCombo(true);
                    #region Gestione del nascondimento e check di default del batch edit mode

                    if (CurrentPageTabFunz != null)
                    {
                        //controllo se nella Tab_Funz quale modo di editing della griglia è abilitato
                        if (CurrentPageTabFunz.Dflt_Grid_Edit_Mode != (int)SettingsEditing.EditMode && UserCanEdit())
                        {
                            // non è 0, qundi è visible
                            cbBatchMode.Visible = true;
                            // imposta la modalità di modifica a batch
                            GridView.SettingsEditing.Mode = GridViewEditingMode.Batch;
                            // imposta il default del checkbox di batch edit in base al parametro della tab funz
                            cbBatchMode.Checked = CurrentPageTabFunz.Dflt_Grid_Edit_Mode == (int)SettingsEditing.BatchModeActive;
                        }
                        else // nascondimento dell'opzione
                            cbBatchMode.Visible = false;
                    }

                    #endregion


                }

                SetGridViewEditingMode();
            }
            //if (PowerWebContext.Current.User.Codice_Utente != "WINIT")
            //    btnDeletePrintLayout.Visible = false;
            btnSavePrintLayout.Visible = false;
            List<Tab_Aut> livelli = RepoManager.Tab_AutRepo.GetAll().Where(user => user.Utenti_Id == PowerWebContext.Current.User.Utenti_Id).ToList();
            if (livelli.First().Del_Aut < 10) {
                btnSaveLayout.Visible = false;
                btnCustomizeColumns.Visible = false;
                btnPrint.Visible = false;
                btnPrintPdf.Visible = false;
                btnExportXLSX.Visible = false;
                btnHelp.Visible = false;
                btnPrintXlsx.Visible = false;
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        //Inizializza tutte le ToolTip, Le Label comuni a Tutte le Pages
        //Attiva visualizzazione del Tasto di Open Mappa Cantiere se Flag_Gps=On
        //Attiva visualizzazione del Tasto di Export Excel Custom
        //Imposta Visualizzazione Bottone ON/OFF in base la Valore del Flag Dflt_OnOffBtnVisible della Singola Funzione in tab_FUNZ
        {

            if (GridPage != null && GridView != null)
            {
                //Inizializza TUTTE le ToolTip COMUNI a TUTTE le PAGES
                btnSaveLayout.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_SALVA_LAYOUT_GRID);
               // btnDeleteLayout.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_CANCELLA_LAYOUT_GRID);
                btnHelp.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_HELP);
                btnCustomizeColumns.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_PERSONALIZZA_COLONNE_GRID);

                if (RepoManager.ParamRepo.IsCurrentUserCustomizationEnabled(CustomizationEnum.OnlyUserDefinedViews, PowerWebContext.Current.User.Codice_Utente))
                {
                    /*btnDeleteLayout.Visible = */btnSaveLayout.Visible = false;
                }

                if (RepoManager.ParamRepo.IsCurrentUserCustomizationEnabled(CustomizationEnum.DisableColumnChooser, PowerWebContext.Current.User.Codice_Utente))
                {
                    btnCustomizeColumns.Visible = false;
                }

                if (RepoManager.ParamRepo.IsCurrentUserCustomizationEnabled(CustomizationEnum.DisableExcelExport, PowerWebContext.Current.User.Codice_Utente))
                {
                    btnExportXLSX.Visible = false;
                }

                if (RepoManager.ParamRepo.IsCurrentUserCustomizationEnabled(CustomizationEnum.DisableExcelExportGrid, PowerWebContext.Current.User.Codice_Utente))
                {
                    btnPrintXlsx.Visible = false;
                }

                if (RepoManager.ParamRepo.IsCurrentUserCustomizationEnabled(CustomizationEnum.DisablePdfExport, PowerWebContext.Current.User.Codice_Utente))
                {
                    btnPrint.Visible = false;
                }

                if (RepoManager.ParamRepo.IsCurrentUserCustomizationEnabled(CustomizationEnum.DisablePdfExportGrid, PowerWebContext.Current.User.Codice_Utente))
                {
                    btnPrintPdf.Visible = false;
                }

                btnPrint.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_APRI_OPZIONI_REPORT);
                btnPrintPdf.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_ESEGUI_STAMPA_PDF);
                btnPrintXlsx.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_ESEGUI_STAMPA_EXCEL);
                btnShowMap.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_APRI_MAPPA);



                //Inizializza le LABEL comuni a TUTTE le Pagina 
                //(che verranno poi eventualmente rese NON visibili se qualla pagina non la gestisce)
                lblExpandAll.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_EXPAND);
                lblPrintCover.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_STAMPA_COVER);

                GridView.ClientInstanceName = "grid";

                foreach (GridViewDataTextColumn column in GridView.Columns.OfType<GridViewDataTextColumn>())
                    column.Settings.AutoFilterCondition = AutoFilterCondition.Contains;


                if (Page.IsPostBack)
                {
                    BindCheckBoxList();
                    BindGroupingTree();
                }

                //Se è Attivata la gestione GPS (viaggi o dispositivi) in Scheda PARAM allora Attiva il Bottone che permette la Visualizzazione dei Cantieri su Mappa 
                if (GeoLocationModule != null && (RepoManager.ParamRepo.ParametersRow.Flag_GPS != 0 || RepoManager.ParamRepo.ParametersRow.Abilita_GPS))
                    btnShowMap.Visible = true;

                if (GridModule.EntityType == typeof(Reg_V)) {
                    btnShowMap.Visible = false;
                }

                //Attiva il Bottone che permette la scelta dell'Export Custom
                if (ExportXLSXModule == null)
                    btnExportXLSX.Visible = false;

                if (PrintModule != null)
                {

                    var noSelectableReport = RepoManager.Tab_ReportRepo.FirstOrDefault(rp => rp.Nome_DataGrid == GridView.ID && rp.Tab_String == null && rp.Utenti_Id == null);
                    if (noSelectableReport != null)
                    {
                        TabPage tabSelection = pcPrint.TabPages.FindByName("tabSelection");
                        tabSelection.Visible = false;
                    }
                }

                if (PrintCustomModule != null)
                {
                    TabPage tabSelection = pcPrint.TabPages.FindByName("tabSelection");
                    tabSelection.Visible = false;

                    ASPxButton btnPrintReport = pcPrint.FindControl("btnPrintReport") as ASPxButton;
                    if (btnPrintReport != null)
                        btnPrintReport.Enabled = true;

                    TabPage tabCustom = pcPrint.TabPages.FindByName("tabCustom");
                    tabCustom.Visible = true;
                    pcPrint.ActiveTabIndex = 1;

                    tabCustom.Controls.Add(PrintCustomModule.CustomOptionsPanel);
                }

                //Verifica che sia stato definito almeno un Report per quella Pagina
                ASPxGridView printGrid = GetPrintGrid();
                Tab_Report currentReport = RepoManager.Tab_ReportRepo.FirstOrDefault(trr => trr.Nome_DataGrid == printGrid.ID);

                if (GridModule.EditFormTemplate != null)
                    GridView.Templates.EditForm = GridModule.EditFormTemplate;

                PowerWebService.InitGrid(GridView);

                if (BaseGridModule != null)
                {
                    GridView.CellEditorInitialize += BaseGridModule.GridView_CellEditorInitialize;
                    GridView.BatchUpdate += BaseGridModule.BatchUpdate;
                }


                if (DoubleGridModule != null)
                {
                    // se sono all'interno di un double grid module allora rinomino lato client anche la seconda griglia
                    DoubleGridModule.GridView2.ClientInstanceName = "grid2";
                    // se sono all'interno di una double grid module allora preparo la customization window
                    DoubleGridModule.GridView2.SettingsBehavior.EnableCustomizationWindow = true;
                    DoubleGridModule.GridView2.ClientSideEvents.CustomizationWindowCloseUp = "grid_OnCustomizationWindowCloseUp";
                    DoubleGridModule.GridView2.SettingsPopup.CustomizationWindow.Height = new System.Web.UI.WebControls.Unit(250, System.Web.UI.WebControls.UnitType.Pixel);
                    DoubleGridModule.GridView2.SettingsPopup.CustomizationWindow.Width = new System.Web.UI.WebControls.Unit(200, System.Web.UI.WebControls.UnitType.Pixel);
                    DoubleGridModule.GridView2.ParseValue += new ASPxParseValueEventHandler(GridView_ParseValue);
                    DoubleGridModule.GridView2.Templates.EditForm = DoubleGridModule.EditFormTemplate2;
                    PowerWebService.InitGrid(DoubleGridModule.GridView2);
                }

                if (TripleGridModule != null)
                {
                    // se sono all'interno di un triple grid module allora rinomino lato client anche la terza griglia
                    TripleGridModule.GridView3.ClientInstanceName = "grid3";
                    // se sono all'interno di una triple grid module allora preparo la customization window
                    TripleGridModule.GridView3.SettingsBehavior.EnableCustomizationWindow = true;
                    TripleGridModule.GridView3.ClientSideEvents.CustomizationWindowCloseUp = "grid_OnCustomizationWindowCloseUp";
                    TripleGridModule.GridView3.SettingsPopup.CustomizationWindow.Height = new System.Web.UI.WebControls.Unit(250, System.Web.UI.WebControls.UnitType.Pixel);
                    TripleGridModule.GridView3.SettingsPopup.CustomizationWindow.Width = new System.Web.UI.WebControls.Unit(200, System.Web.UI.WebControls.UnitType.Pixel);
                    TripleGridModule.GridView3.ParseValue += new ASPxParseValueEventHandler(GridView_ParseValue);
                    TripleGridModule.GridView3.Templates.EditForm = TripleGridModule.EditFormTemplate3;
                    PowerWebService.InitGrid(TripleGridModule.GridView3);
                }

                if (QuadGridModule != null)
                {
                    // se sono all'interno di un quad grid module allora rinomino lato client anche la quarta griglia
                    QuadGridModule.GridView4.ClientInstanceName = "grid4";
                    // se sono all'interno di un quad grid module allora preparo la customization window
                    QuadGridModule.GridView4.SettingsBehavior.EnableCustomizationWindow = true;
                    QuadGridModule.GridView4.ClientSideEvents.CustomizationWindowCloseUp = "grid_OnCustomizationWindowCloseUp";
                    QuadGridModule.GridView4.SettingsPopup.CustomizationWindow.Height = new System.Web.UI.WebControls.Unit(250, System.Web.UI.WebControls.UnitType.Pixel);
                    QuadGridModule.GridView4.SettingsPopup.CustomizationWindow.Width = new System.Web.UI.WebControls.Unit(200, System.Web.UI.WebControls.UnitType.Pixel);
                    QuadGridModule.GridView4.ParseValue += new ASPxParseValueEventHandler(GridView_ParseValue);
                    QuadGridModule.GridView4.Templates.EditForm = QuadGridModule.EditFormTemplate4;
                    PowerWebService.InitGrid(QuadGridModule.GridView4);
                }

                #region IMPOSTAZIONI SETTATE QUANDO SI ESEGUE LA INIT della SINGOLA PAGINA
                GridView.ParseValue += new ASPxParseValueEventHandler(GridView_ParseValue);
                GridView.HeaderFilterFillItems += GridModule.HeaderFilterFillItems;
                GridView.SettingsBehavior.EnableCustomizationWindow = true;
                GridView.ClientSideEvents.CustomizationWindowCloseUp = "grid_OnCustomizationWindowCloseUp";
                GridView.SettingsPopup.CustomizationWindow.Height = new System.Web.UI.WebControls.Unit(250, System.Web.UI.WebControls.UnitType.Pixel);
                GridView.SettingsPopup.CustomizationWindow.Width = new System.Web.UI.WebControls.Unit(200, System.Web.UI.WebControls.UnitType.Pixel);
                GridView.ClientSideEvents.Init = "grid_OnInit";

                if (String.IsNullOrEmpty(GridView.ClientSideEvents.EndCallback))
                    GridView.ClientSideEvents.EndCallback = "grid_OnEndCallback";

                GridView.ClientSideEvents.CustomButtonClick = "OnCustomButtonClick";
                GridView.CustomJSProperties += GridView_CustomJSProperties;
                GridView.CustomButtonInitialize += GridView_CustomButtonInitialize;
                GridView.CommandButtonInitialize += GridView_CommandButtonInitialize;
                GridView.CustomErrorText += GridView_CustomErrorText;
                //GridView.HtmlRowPrepared += GridView_HtmlRowPrepared;
                GridView.HtmlDataCellPrepared += GridView_HtmlDataCellPrepared;

                PowerWebService.InitGridCommandColumn(GridView.Columns.OfType<GridViewCommandColumn>().FirstOrDefault());

                #endregion

                if (!Page.IsCallback && !Page.IsPostBack)
                {
                    if (GridModule.EntityType == typeof(Cant))
                    {
                        PowerWebContext.SetToSession<List<Cant>>("DomainCantList", null);
                        PowerWebContext.SetToSession<List<Cant>>("General_CantUtentiFilDictionary", null);
                    }
                    if (GridModule.EntityType == typeof(Col))
                    {
                        PowerWebContext.SetToSession<List<Col>>("DomainColList", null);
                        PowerWebContext.SetToSession<List<Cant>>("General_ColUtentiRespDictionary", null);
                    }
                    if (GridModule.EntityType == typeof(Cli))
                    {
                        PowerWebContext.SetToSession<List<Cli>>("DomainCliList", null);
                        PowerWebContext.SetToSession<List<Cli>>("General_CliUtentiRespDictionary", null);
                    }
                    if (GridModule.EntityType == typeof(Reg_V))
                    {
                        PowerWebContext.SetToSession<List<Reg_V>>("DomainRegVList", null);
                        PowerWebContext.SetToSession<List<Cant>>("General_ColUtentiRespDictionary", null);
                        PowerWebContext.SetToSession<List<Cant>>("General_CantUtentiFilDictionary", null);
                        PowerWebContext.SetToSession<List<Cli>>("General_CliUtentiFilDictionary", null);
                    }
                }

                #region Gestione nascondimento pulsante ON/OFF e visibilità dati griglia in apertura

                if (!Page.IsCallback && !Page.IsPostBack)
                {
                    InitOnOffButton();
                }

                #endregion

                SetGridViewEditingMode();
            }

            if (!Page.IsPostBack)
                Page.DataBind();

            //Abilita/Disabilita i Tasti di Export Excel e PDF quando i Dati da gestire sono < 1000 Records
            hideOrShowExportButtons();

            // gestione dei filtri di default in apertura (si rende necessario effettuare queste operazioni anche qua
            // in quanto si posso produrre problemi in caso di mancanza di viste)
            if (!Page.IsPostBack && !Page.IsCallback)
            {
                // se la griglia è disponibile
                if (GridView != null)
                {
                    // inizializzazione delle proprietà del pannello principale di callback
                    cpLayout.JSProperties["cpErrorTitle"] = String.Empty;
                    cpLayout.JSProperties["cpErrorMessage"] = String.Empty;

                    // se non ci sono delle viste presenti nel repository allora si procede ad impostare l'eventuale filtro di
                    // default sulla corrente
                    if (!RepoManager.Tab_DataGridRepo.DbSet.Any(tdg => tdg.Nome_DataGrid == GridView.ID))
                    {
                        // caricamento dell'eventuale layout di default
                        GridView.LoadClientLayout(PowerWebContext.GetFromSession<String>("GridLayout_" + GridView.ID));

                        if (!ReferenceEquals(GridModule.DefaultFilter, null))
                        {
                            GridView.FilterExpression = GridModule.DefaultFilter.ToString();
                            GridView.FilterEnabled = true;
                            GridView.DataBind();
                        }

                        // gestione del layout per le griglie doppie
                        ManageDoubleAndTripleGridLayout(null, null, null);

                    }
                }


            }


        }

        private void InitOnOffButton()
        {
            if (CurrentPageTabFunz.Dflt_OnOffBtnVisible == 1)
            {
                //per le Pagine che hanno impostato nella TAB_FUNZ il Flag Dflt_OnOffBtnVisible = 1
                //viene visualizzato il TASTO di ON/OFF e settato in ON
                GridPage.GridModule.IsToPopulateGrid = true;
                btnPopulateGrid.Visible = true;
                btnPopulateGrid.Text = "ON";
            }
            else if (CurrentPageTabFunz.Dflt_OnOffBtnVisible == 2)
            {
                //per le Pagine che hanno impostato nella TAB_FUNZ il Flag Dflt_OnOffBtnVisible = 2
                //viene visualizzato il TASTO di ON/OFF e settato in OFF
                GridPage.GridModule.IsToPopulateGrid = false;
                btnPopulateGrid.Visible = true;
                btnPopulateGrid.Text = "OFF";
            }
            else
            {
                btnPopulateGrid.Visible = false;
                GridPage.GridModule.IsToPopulateGrid = true;
            }
        }

        #endregion

        private bool UserCanEdit()
        {
            return PowerWebContext.Current.User.IsUserAutorized(Utenti.OperationTypeEnum.Edit, CurrentPageTabAut, RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DefaultFunzAuthLevelEnum));
        }

        #region Eventi sottoscritti da tutte le Griglie di Tutti i Moduli

        void GridView_HtmlDataCellPrepared(object sender, ASPxGridViewTableDataCellEventArgs e)
        {
            if (GridModule.EntityType == typeof(Reg_V))
            {
                if (e.DataColumn.FieldName == (CommonService.GetPropertyName(() => _regvStub.Registrazione_Stato_Reg)))
                {
                    //controllo lo satto della registrazione
                    var regState = Convert.ToInt32(e.CellValue);

                    //se ho un errore sulle registrazioni (durata massima, durata minima, overlap)
                    if (regState == (int)RegStateEnum.ErrMax || regState == (int)RegStateEnum.ErrMin || regState == (int)RegStateEnum.Overlap)
                    {
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegStateEnum)regState, false);
                    }
                    //se le registrazioni non sono associate
                    else if (regState == (int)RegStateEnum.None)
                    {
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegStateEnum)regState, false);
                    }
                }
                else if (e.DataColumn.FieldName == (CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg)))
                {
                    //controllo il tipo di registrazione (viaggio, attività, passaggi)
                    var regType = Convert.ToInt32(e.CellValue);

                    if (regType == (int)RegTypeEnum.Trip) //se è un viaggio
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
                    else if (regType == (int)RegTypeEnum.Att) // se è un'attività
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
                    else if (regType == (int)RegTypeEnum.Pass) // se è un passaggio
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
                    else if (regType == (int)RegTypeEnum.Duration) // se è una registrazione solo durata
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
                    else if (regType == (int)RegTypeEnum.RettTimesheet) // se si tratta di una rettifica
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
                    else if (regType == (int)RegTypeEnum.ArrotDur) // se si tratta di una rettifica
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
                    else if (regType == (int)RegTypeEnum.RettTimeSheetManual) // se si tratta di una rettifica
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
                }
                // si colora l'ora di entrata fisica in bas al tipo modifica apportata o la si nasconde se si sta processando una rettifica o una registrazione solo durata e l'ora è mezzanotte
                else if (e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_E) ||
                    e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_ETime) ||
                    e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fig_E) ||
                    e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fig_ETime))
                {
                    // calcolo della posizione attuale in elaborazione
                    var checkIndex = e.VisibleIndex - GridView.VisibleStartIndex;

                    // verifico la presenza della customizzazione riguardante la non colorazione delle attività
                    int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoColorForEditedActivityEnum);

                    // recupero il valore di tipo della registrazione in elaborazione
                    var listaTipiReg = GridView.GetCurrentPageRowValues(CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg));
                    int registrazioneTipoReg = (int)RegTypeEnum.None;
                    if (listaTipiReg.Count > 0 && checkIndex >= 0 && checkIndex <= listaTipiReg.Count)
                    {
                        registrazioneTipoReg = Convert.ToInt32(listaTipiReg[checkIndex]);
                    }

                    #region COLORAZIONE ORE FISICHE
                    // si processa la colorazione solamente per le ore fisiche
                    if (e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_E) ||
                    e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_ETime) ||
                    e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fig_ETime))
                    {
                        // si colora la cella solamente se non è un'attività o se è attiva la colorazione per le attività
                        if (registrazioneTipoReg != (int)RegTypeEnum.Att || customizationVersion == (int)NoColorForEditedActivityEnum.Color)
                        {
                            // se si sta processando un viaggio allora le ore assumono il colore del tipo registrazione
                            if (registrazioneTipoReg == (int)RegTypeEnum.Trip)
                            {
                                e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)registrazioneTipoReg, false);
                            }
                            else if (registrazioneTipoReg == (int)RegTypeEnum.ArrotDur) {
                                e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)registrazioneTipoReg, false);
                            }
                            else // in caso contrario si procede al check del tipo modifica
                            {
                                // viene recuperato il valore del campo tipo modifica dalla griglia per la riga alla posizione della ccella in elaborazione
                                var listaTipiModifica = GridView.GetCurrentPageRowValues(CommonService.GetPropertyName(() => _regvStub.Tipo_Modifica));
                                if (listaTipiModifica.Count > 0 && checkIndex >= 0 && checkIndex <= listaTipiModifica.Count)
                                {
                                    var tipoModifica = Convert.ToInt32(listaTipiModifica[checkIndex]);

                                    var colorModify = BusinessService.ColorModify((RegModifyTypeEnum)tipoModifica, RegEUEnum.Entry);

                                    e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum(colorModify, false);
                                }
                            }
                        }
                    }
                    #endregion

                    // se la reg_v che si sta processando è solo durata o rettifica e si tratta di mezzanotte allora non si scrive nessun valore
                    if (registrazioneTipoReg == (int)RegTypeEnum.Duration || registrazioneTipoReg == (int)RegTypeEnum.RettTimesheet)
                    {
                        // recupero il valore della cella da processare
                        var cellValue = e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_E) ? ((DateTime)e.CellValue).TimeOfDay : (TimeSpan)e.CellValue;

                        // se il valore della cella è mezzanotte allora si procede a svuotare il valore scritto
                        if (cellValue == new TimeSpan(0, 0, 0))
                            e.Cell.Text = null;
                    }

                }
                else if (e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_U) || 
                         e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_UTime) ||
                         e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fig_UTime))
                // si colora l'ora di uscita fisica in bas al tipo modifica apportata
                {
                    // calcolo della posizione attuale in elaborazione
                    var checkIndex = e.VisibleIndex - GridView.VisibleStartIndex;

                    // verifico la presenza della customizzazione riguardante la non colorazione delle attività
                    int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoColorForEditedActivityEnum);

                    // recupero il valore di tipo della registrazione in elaborazione
                    var listaTipiReg = GridView.GetCurrentPageRowValues(CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg));
                    int registrazioneTipoReg = (int)RegTypeEnum.None;
                    if (listaTipiReg.Any() && checkIndex >= 0 && checkIndex <= listaTipiReg.Count)
                    {
                        registrazioneTipoReg = Convert.ToInt32(listaTipiReg[checkIndex]);
                    }

                    // si colora la cella solamente se non è un'attività o se è attiva la colorazione
                    if (registrazioneTipoReg != (int)RegTypeEnum.Att || customizationVersion == (int)NoColorForEditedActivityEnum.Color)
                    {
                        // se si sta processando un viaggio allora le ore assumono il colore del tipo registrazione
                        if (registrazioneTipoReg == (int)RegTypeEnum.Trip)
                        {
                            e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)registrazioneTipoReg, false);
                        }
                        else if (registrazioneTipoReg == (int)RegTypeEnum.ArrotDur)
                        {
                            e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)registrazioneTipoReg, false);
                        }
                        else // in caso contrario si procede al check del tipo modifica
                        {
                            // viene recuperato il valore del campo tipo modifica dalla griglia per la riga alla posizione della ccella in elaborazione
                            var listaTipiModifica = GridView.GetCurrentPageRowValues(CommonService.GetPropertyName(() => _regvStub.Tipo_Modifica));
                            if (listaTipiModifica.Any() && checkIndex >= 0 && checkIndex <= listaTipiModifica.Count)
                            {
                                var tipoModifica = Convert.ToInt32(listaTipiModifica[checkIndex]);

                                var colorModify = BusinessService.ColorModify((RegModifyTypeEnum)tipoModifica, RegEUEnum.Exit);

                                e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum(colorModify, false);
                            }
                        }
                    }
                }

                else if (e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Ritardo_Durata))
                {
                    //controllo il tipo di registrazione (viaggio, attività, passaggi)
                    int delayDuration = Convert.ToInt32(e.CellValue);

                    //se vi sono dei ritardi
                    if (delayDuration > 0)
                    {
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum(DelayEnum.Delay, false);
                    }
                }

                else if (e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Activity_Evaluation)) // colorazione dell'eventuale valutazione attività
                {
                    int? activityEvaluation = (int?)e.CellValue;

                    if (activityEvaluation > 0)
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((ActivityEvaluationStateEnum)activityEvaluation, false);
                }
                else if (e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Delta_Fig_Fis))
                {
                    if (e.CellValue != null && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ColorDeltaFigFisEnum) == (int)ColorDeltaFigFisEnum.Active)
                    {
                        int minusTollerance = Math.Abs(Convert.ToInt32(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.ColorDeltaFigFisEnum, "TolleranceMinus"))),
                            plusTollerance = Math.Abs(Convert.ToInt32(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.ColorDeltaFigFisEnum, "TollerancePlus")));
                        TimeSpan deltaFigFis = TimeSpan.Parse((string)e.CellValue),
                            maxBound = TimeSpan.FromMinutes(plusTollerance),
                            minBound = TimeSpan.FromMinutes(-1 * minusTollerance);
                        bool isInMaxBound = false,
                             isInMinBound = false;

                        if (deltaFigFis.CompareTo(maxBound) <= 0)
                        {
                            isInMaxBound = true;
                        }
                        if (deltaFigFis.CompareTo(minBound) >= 0)
                        {
                            isInMinBound = true;
                        }

                        DeltaFigFis color;
                        if (isInMaxBound && isInMinBound)
                        {
                            color = DeltaFigFis.Zero;
                        }
                        else if (isInMaxBound)
                        {
                            color = DeltaFigFis.Negative;
                        }
                        else
                        {
                            color = DeltaFigFis.Positive;
                        }

                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum(color, false);
                    }
                    // calcolo della posizione attuale in elaborazione
                    var checkIndex = e.VisibleIndex - GridView.VisibleStartIndex;

                    // verifico la presenza della customizzazione riguardante la non colorazione delle attività
                    int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoColorForEditedActivityEnum);

                    // recupero il valore di tipo della registrazione in elaborazione
                    var listaTipiReg = GridView.GetCurrentPageRowValues(CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg));
                    int registrazioneTipoReg = (int)RegTypeEnum.None;
                    if (listaTipiReg.Count > 0 && checkIndex >= 0 && checkIndex <= listaTipiReg.Count)
                    {
                        registrazioneTipoReg = Convert.ToInt32(listaTipiReg[checkIndex]);
                    }
                    if (registrazioneTipoReg == (int)RegTypeEnum.ArrotDur)
                    {
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)registrazioneTipoReg, false);
                    }
                    else if (registrazioneTipoReg == (int)RegTypeEnum.Trip)
                    {
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)registrazioneTipoReg, false);
                    }
                } 
                else if (e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Col_Desc) || 
                    e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Cant_Mnemonic) || 
                    e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Col_Id) || 
                    e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Cant_Id) ||
                    e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Reg) ||
                    e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Durata_Fis_HH_S) ||
                    e.DataColumn.FieldName == CommonService.GetPropertyName(() => _regvStub.Durata_Fig_HH_S)) {
                    // calcolo della posizione attuale in elaborazione
                    var checkIndex = e.VisibleIndex - GridView.VisibleStartIndex;

                    // verifico la presenza della customizzazione riguardante la non colorazione delle attività
                    int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoColorForEditedActivityEnum);

                    // recupero il valore di tipo della registrazione in elaborazione
                    var listaTipiReg = GridView.GetCurrentPageRowValues(CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg));
                    int registrazioneTipoReg = (int)RegTypeEnum.None;
                    if (listaTipiReg.Count > 0 && checkIndex >= 0 && checkIndex <= listaTipiReg.Count)
                    {
                        registrazioneTipoReg = Convert.ToInt32(listaTipiReg[checkIndex]);
                    }
                    if (registrazioneTipoReg == (int)RegTypeEnum.ArrotDur)
                    {
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)registrazioneTipoReg, false);
                    }else if (registrazioneTipoReg == (int)RegTypeEnum.Trip)
                    {
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)registrazioneTipoReg, false);
                    }
                }

                // se è attiva la personalizzazione di colorazione dell'intera linea in caso di viaggio e attività
                int allLineColorCust = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ColorAllLinesForTripAndActivityEnum);
                if (allLineColorCust == (int)ColorAllLinesForTripAndActivityEnum.ColorAllLine)
                {
                    // se la colonna non è l'ora d'entrata, l'ora d'uscita, il tipo registrazione, lo stato registrazione
                    if (e.DataColumn.FieldName != CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg)
                        && e.DataColumn.FieldName != CommonService.GetPropertyName(() => _regvStub.Registrazione_Stato_Reg))
                    {
                        // calcolo della posizione attuale in elaborazione
                        var checkIndex = e.VisibleIndex - GridView.VisibleStartIndex;

                        // recupero il valore di tipo della registrazione in elaborazione
                        var listaTipiReg = GridView.GetCurrentPageRowValues(CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg));
                        var registrazioneTipoReg = (int)RegTypeEnum.None;
                        if (listaTipiReg.Count > 0 && checkIndex >= 0 && checkIndex <= listaTipiReg.Count)
                        {
                            registrazioneTipoReg = Convert.ToInt32(listaTipiReg[checkIndex]);

                            // se la reigstrazione in processo è un'attività o un viaggio
                            if (registrazioneTipoReg == (int)RegTypeEnum.Att || registrazioneTipoReg == (int)RegTypeEnum.Trip)
                                e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)registrazioneTipoReg, false);
                        }
                    }
                }
            }
        }

        public void GridView_CustomErrorText(object sender, ASPxGridViewCustomErrorTextEventArgs e)
        {
            ASPxGridView currentGrid = sender as ASPxGridView;
            IGridPage currentGridPage = currentGrid.Page as IGridPage;
            var entityName = currentGridPage.GridModule.EntityType.Name;

            ILogPage currentLogPage = currentGrid.Page as ILogPage;

            if (e.Exception != null && currentLogPage != null)
            {
                e.ErrorText = e.Exception.Message;

                if (e.Exception.GetType() == typeof(System.Data.Entity.Core.UpdateException))
                {
                    e.ErrorText = BusinessService.GetLocalizedString(CommonService.GetPropertyName(() => PowerWebResources.ERR_ACCESSO_DATABASE));
                    currentLogPage.LogModule.Log.Error(e.ErrorText + "\n" + CommonService.GetErrorMessageFromException(e.Exception, 0), e.Exception);

                    Exception internalEx = CommonService.GetInternalException(e.Exception);
                }

                if (e.Exception.GetType() == typeof(System.Data.Entity.Infrastructure.DbUpdateException))
                {
                    e.ErrorText = BusinessService.GetLocalizedString(BusinessService.GetLocalizedString(PowerWebResources.ERR_ACCESSO_DATABASE));
                    currentLogPage.LogModule.Log.Error(e.ErrorText + "\n" + CommonService.GetErrorMessageFromException(e.Exception, 0), e.Exception);

                    Exception internalEx = CommonService.GetInternalException(e.Exception);

                    int startExcIndex = internalEx.Message.IndexOf("\"FK");
                    if (startExcIndex != -1)
                    {

                        var relationError = internalEx.Message.Substring(startExcIndex + 1);

                        relationError = relationError.Substring(0, relationError.IndexOf("\""));

                        var relationErrorString = new StringBuilder(BusinessService.GetLocalizedString(relationError, ResourceTypeEnum.Error));

                        if (internalEx.Message.Contains("DELETE"))
                            relationErrorString.AppendFormat(" {0}", BusinessService.GetLocalizedString(PowerWebResources.ERR_DURANTE_DELETE));
                        if (internalEx.Message.Contains("UPDATE"))
                            relationErrorString.AppendFormat(" {0}", BusinessService.GetLocalizedString(PowerWebResources.ERR_DURANTE_UPDATE));
                        if (internalEx.Message.Contains("INSERT"))
                            relationErrorString.AppendFormat(" {0}", BusinessService.GetLocalizedString(PowerWebResources.ERR_DURANTE_INSERT));

                        e.ErrorText = relationErrorString.ToString();
                    }
                }

                if (e.Exception.GetType() == typeof(RowDeletingException))
                {
                    e.ErrorText = e.Exception.Message;
                    currentLogPage.LogModule.Log.Error(e.ErrorText, e.Exception);
                }
            }
        }
        public Hashtable GridClonedValues { get; set; }
        public void GridView_CustomJSProperties(object sender, ASPxGridViewClientJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpIsConfirmAdd"))
                e.Properties.Add("cpIsConfirmAdd", GetConfirmationString(Utenti.OperationTypeEnum.Insert, CurrentPageTabAut, 1));
            if (!e.Properties.ContainsKey("cpConfirmAdd"))
                e.Properties.Add("cpConfirmAdd", BusinessService.GetLocalizedString(PowerWebResources.FLD_MSGINS1_AUT));
            if (!e.Properties.ContainsKey("cpIsConfirmAdd2"))
                e.Properties.Add("cpIsConfirmAdd2", GetConfirmationString(Utenti.OperationTypeEnum.Insert, CurrentPageTabAut, 2));
            if (!e.Properties.ContainsKey("cpConfirmAdd2"))
                e.Properties.Add("cpConfirmAdd2", BusinessService.GetLocalizedString(PowerWebResources.FLD_MSGINS2_AUT));
            if (!e.Properties.ContainsKey("cpIsConfirmMod"))
                e.Properties.Add("cpIsConfirmMod", GetConfirmationString(Utenti.OperationTypeEnum.Edit, CurrentPageTabAut, 1));
            if (!e.Properties.ContainsKey("cpConfirmMod"))
                e.Properties.Add("cpConfirmMod", BusinessService.GetLocalizedString(PowerWebResources.FLD_MSGMOD1_AUT));
            if (!e.Properties.ContainsKey("cpIsConfirmMod2"))
                e.Properties.Add("cpIsConfirmMod2", GetConfirmationString(Utenti.OperationTypeEnum.Edit, CurrentPageTabAut, 2));
            if (!e.Properties.ContainsKey("cpConfirmMod2"))
                e.Properties.Add("cpConfirmMod2", BusinessService.GetLocalizedString(PowerWebResources.FLD_MSGMOD2_AUT));
            if (!e.Properties.ContainsKey("cpIsConfirmDel"))
                e.Properties.Add("cpIsConfirmDel", GetConfirmationString(Utenti.OperationTypeEnum.Delete, CurrentPageTabAut, 1));
            if (!e.Properties.ContainsKey("cpConfirmDel"))
                e.Properties.Add("cpConfirmDel", BusinessService.GetLocalizedString(PowerWebResources.FLD_MSGDEL1_AUT));
            if (!e.Properties.ContainsKey("cpIsConfirmDel2"))
                e.Properties.Add("cpIsConfirmDel2", GetConfirmationString(Utenti.OperationTypeEnum.Delete, CurrentPageTabAut, 2));
            if (!e.Properties.ContainsKey("cpConfirmDel2"))
                e.Properties.Add("cpConfirmDel2", BusinessService.GetLocalizedString(PowerWebResources.FLD_MSGDEL2_AUT));
        }

        /// <summary>
        /// Ritorna l'eventual valore di conferma per l'operazione e l'autorizzazione specificata.
        /// </summary>
        /// <param name="operationType">Il tipo di operazione per cui è richiesta la conferma.</param>
        /// <param name="tabAutToCheck">La tabella autorizzazione relativa alla funzione.</param>
        /// <param name="confirmationNumber">Il numero di conferma da richiedere (la 1°, la 2° ecc.)</param>
        /// <returns>il valore intero del valore di conferma autorizzazione calcolato</returns>
        private int GetConfirmationString(Utenti.OperationTypeEnum operationType, Tab_Aut tabAutToCheck, int confirmationNumber)
        {
            // inizializzazione del valore di ritorno del metodo
            int confirmationValue = 0;

            // inizializzazione del valore di conferma per la tab autorizzazioni
            int tabAutConfirmationValue = 0;

            // in base al tipo di operazione si ritorna il livello di messaggio;
            // l'utente vince sempre rispetto alla funzione, se l'utente ha impostato il valore 0 allora
            // si utilizza il valore della funzione
            switch (operationType)
            {
                case Utenti.OperationTypeEnum.Edit:
                    switch (confirmationNumber)
                    {
                        case 1:
                            tabAutConfirmationValue = tabAutToCheck != null ? tabAutToCheck.MsgMod1_Aut : 0;
                            confirmationValue = PowerWebContext.Current.UserLevel.MsgMod1_Aut == 0 ? tabAutConfirmationValue : PowerWebContext.Current.UserLevel.MsgMod1_Aut;
                            break;
                        case 2:
                            tabAutConfirmationValue = tabAutToCheck != null ? tabAutToCheck.MsgMod2_Aut : 0;
                            confirmationValue = PowerWebContext.Current.UserLevel.MsgMod2_Aut == 0 ? tabAutConfirmationValue : PowerWebContext.Current.UserLevel.MsgMod2_Aut;
                            break;
                        default:
                            confirmationNumber = 0;
                            break;
                    }
                    break;
                case Utenti.OperationTypeEnum.Insert:
                    switch (confirmationNumber)
                    {
                        case 1:
                            tabAutConfirmationValue = tabAutToCheck != null ? tabAutToCheck.MsgIns1_Aut : 0;
                            confirmationValue = PowerWebContext.Current.UserLevel.MsgIns1_Aut == 0 ? tabAutConfirmationValue : PowerWebContext.Current.UserLevel.MsgIns1_Aut;
                            break;
                        case 2:
                            tabAutConfirmationValue = tabAutToCheck != null ? tabAutToCheck.MsgIns2_Aut : 0;
                            confirmationValue = PowerWebContext.Current.UserLevel.MsgIns2_Aut == 0 ? tabAutConfirmationValue : PowerWebContext.Current.UserLevel.MsgIns2_Aut;
                            break;
                        default:
                            confirmationNumber = 0;
                            break;
                    }
                    break;
                case Utenti.OperationTypeEnum.Delete:
                    switch (confirmationNumber)
                    {
                        case 1:
                            tabAutConfirmationValue = tabAutToCheck != null ? tabAutToCheck.MsgDel1_Aut : 0;
                            confirmationValue = PowerWebContext.Current.UserLevel.MsgDel1_Aut == 0 ? tabAutConfirmationValue : PowerWebContext.Current.UserLevel.MsgDel1_Aut;
                            break;
                        case 2:
                            tabAutConfirmationValue = tabAutToCheck != null ? tabAutToCheck.MsgDel2_Aut : 0;
                            confirmationValue = PowerWebContext.Current.UserLevel.MsgDel2_Aut == 0 ? tabAutConfirmationValue : PowerWebContext.Current.UserLevel.MsgDel2_Aut;
                            break;
                        default:
                            confirmationNumber = 0;
                            break;
                    }
                    break;
                default:
                    // in caso l'operazione non sia riconosciuta si ritorna il valore 0
                    confirmationValue = 0;
                    break;
            }

            // ritorno del valore calcolato dal metodo
            return confirmationValue;
        }

        public void GridView_CommandButtonInitialize(object sender, ASPxGridViewCommandButtonEventArgs e)
        //Disabilita i tasti Funzionali in base alla Pagina da cui viene chiamata
        {
            //CASI GESTITI
            //  se si tratta di Righe VIAGGIO ---> Disabilita il Tasto di MODIFICA
            // se si trattad i Righe RETTIFICA ---> Disabilita il Tasto di MODIFICA
            if (e.ButtonType == ColumnCommandButtonType.Edit)
            {
                if (!TripsFilter(GridModule.EntityType, e.VisibleIndex, e.ButtonType.ToString()) || IsBatchModeEditing)
                {
                    e.Enabled = false;
                    e.Visible = false;
                    return;
                }

                if (!CorrectionFilter(GridModule.EntityType, e.VisibleIndex, e.ButtonType.ToString()) || IsBatchModeEditing)
                {
                    e.Enabled = false;
                    e.Visible = false;
                    return;
                }

                bool isEditable = UserCanEdit();

                if (isEditable && RepoManager.ParamRepo.ParametersRow.DomainFilter != (int)DomainFilterEnum.None)
                    isEditable = DomainFilter(GridModule.EntityType, e.VisibleIndex, e.ButtonType.ToString());

                if (e.Enabled || e.Visible)
                    e.Enabled = e.Visible = isEditable;
            }

        }

        /// <summary>
        /// Gestione dei filtri per filiale e responsabile per la visualizzazione dei bottoni passati come parametro
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="index">The index.</param>
        /// <param name="buttonID">The button identifier.</param>
        /// <returns></returns>
        private bool DomainFilter(Type entityType, int index, String buttonID)
        //Gestisce i Filtri x Filiale/Respansabile
        {
            //Chiamata dalla CustomButtonInizialize di questa GridMaster
            bool isVisible = true;

            //nome del bottone in low case (es:edit,delete, view
            buttonID = buttonID.ToLower();

            #region Filtro per cantieri
            //se l'entità che swi considera è quella dei cantieri
            if (entityType == typeof(Cant))
            {
                //viene estratto l'ide del cantiere corrente
                int currentCantId = Convert.ToInt32(GridView.GetRowValues(index, CommonService.GetPropertyName(() => _regvStub.Cant_Id)));

                //se vi è un cantiere
                if (currentCantId != 0)
                {
                    //
                    DomainEnum currentCantDomain = DomainEnum.ViewUpdate;

                    #region CantDomainDict update

                    //se l'utente con cui si è loggati a power web ha dei cantieri registrati nella filiale
                    if (CantUtentiFilDictionary.ContainsKey(currentCantId))
                        currentCantDomain = CantUtentiFilDictionary[currentCantId];
                    else
                    {
                        //viene estratto il cantiere 
                        var cantFilV = CantFilVs.SingleOrDefault(cf => cf.Cant_Id == currentCantId);

                        if (cantFilV != null)
                        {
                            var domainFil = PowerWebContext.Current.User.Utenti_Fil.SingleOrDefault(ufil => ufil.Fil_Id == cantFilV.Fil_Id);

                            if (domainFil != null)
                            {
                                currentCantDomain = (DomainEnum)domainFil.Dominio_Utenti_Fil;

                                if (!PowerWebContext.Current.User.Fil_Inclusive)
                                    currentCantDomain = DomainEnum.View;
                            }
                        }

                        CantUtentiFilDictionary.Add(currentCantId, currentCantDomain);
                    }

                    #endregion

                    if (currentCantDomain == DomainEnum.None)
                        isVisible = false;
                    else if ("add" == buttonID || "addclone" == buttonID || "delete" == buttonID || "edit" == buttonID)
                    {
                        if (currentCantDomain == DomainEnum.View)
                            isVisible = false;
                    }
                }
            }
            #endregion

            #region Filtro per collaboratori
            else if (entityType == typeof(Col))
            {
                int currentColId = Convert.ToInt32(GridView.GetRowValues(index, CommonService.GetPropertyName(() => _regvStub.Col_Id)));

                if (currentColId != 0)
                {
                    DomainEnum currentColDomain = DomainEnum.ViewUpdate;

                    #region ColDomainDict update

                    if (ColUtentiRespDictionary.ContainsKey(currentColId))
                        currentColDomain = ColUtentiRespDictionary[currentColId];
                    else
                    {
                        var colRespV = ColRespVs.SingleOrDefault(cr => cr.Col_Id == currentColId);
                        if (colRespV != null)
                        {
                            var domainResp = PowerWebContext.Current.User.Utenti_Resp.SingleOrDefault(uresp => uresp.Resp_Id == colRespV.Resp_Id);
                            if (domainResp != null)
                            {
                                currentColDomain = (DomainEnum)domainResp.Dominio_Utenti_Resp;

                                if (!PowerWebContext.Current.User.Resp_Inclusive)
                                    currentColDomain = DomainEnum.View;
                            }
                        }

                        ColUtentiRespDictionary.Add(currentColId, currentColDomain);
                    }

                    #endregion

                    if (currentColDomain == DomainEnum.None)
                        isVisible = false;
                    else if ("add" == buttonID || "addclone" == buttonID || "delete" == buttonID || "edit" == buttonID)
                    {
                        if (currentColDomain == DomainEnum.View)
                            isVisible = false;
                    }
                }
            }
            #endregion

            #region Filtro per filiale

            else if (entityType == typeof(Domain.Fil))
            {

                int filId = Convert.ToInt32(GridView.GetRowValues(index, CommonService.GetPropertyName(() => _filStub.Fil_Id)));
                if (filId != 0)
                {
                    var domainFil = PowerWebContext.Current.User.Utenti_Fil.SingleOrDefault(ufil => ufil.Fil_Id == (int)filId);
                    if (domainFil != null)
                    {
                        if (domainFil.Dominio_Utenti_Fil == (int)DomainEnum.None)
                            isVisible = false;
                        else
                        {
                            if ("add" == buttonID || "addclone" == buttonID || "delete" == buttonID || "edit" == buttonID)
                            {
                                if (domainFil.Dominio_Utenti_Fil == (int)DomainEnum.View || !PowerWebContext.Current.User.Fil_Inclusive)
                                    isVisible = false;
                            }
                        }
                    }
                }

            }

            #endregion

            #region Filtro per responsabile

            else if (entityType == typeof(Domain.Resp))
            {
                int respId = Convert.ToInt32(GridView.GetRowValues(index, CommonService.GetPropertyName(() => _respStub.Resp_Id)));
                if (respId != 0)
                {
                    var domainResp = PowerWebContext.Current.User.Utenti_Resp.SingleOrDefault(uresp => uresp.Resp_Id == (int)respId);
                    if (domainResp != null)
                    {
                        if (domainResp.Dominio_Utenti_Resp == (int)DomainEnum.None)
                            isVisible = false;
                        else
                        {
                            if ("add" == buttonID || "addclone" == buttonID || "delete" == buttonID || "edit" == buttonID)
                            {
                                if (domainResp.Dominio_Utenti_Resp == (int)DomainEnum.View || !PowerWebContext.Current.User.Resp_Inclusive)
                                    isVisible = false;
                            }
                        }
                    }
                }
            }

            #endregion

            #region Filtro Reg_V

            else if (entityType == typeof(Reg_V))
            {
                //viene estratto il ollaboratore e cantiere della regv corrente
                int currentCantId = Convert.ToInt32(GridView.GetRowValues(index, CommonService.GetPropertyName(() => _regvStub.Cant_Id)));
                int currentColId = Convert.ToInt32(GridView.GetRowValues(index, CommonService.GetPropertyName(() => _regvStub.Col_Id)));

                //se sono diversi da zero
                if (currentCantId != 0 && currentColId != 0)
                {
                    //viene impostato come dominio corrente l'abilitazione alla modifica e alla visualizzazzione
                    DomainEnum currentCantDomain = DomainEnum.ViewUpdate;

                    #region CantDomainDict update

                    //viene cercato all'inerno delle filiali il cantiere della regv corrente
                    if (CantUtentiFilDictionary.ContainsKey(currentCantId))
                        currentCantDomain = CantUtentiFilDictionary[currentCantId];
                    else
                    {
                        var cantFilV = CantFilVs.SingleOrDefault(cf => cf.Cant_Id == currentCantId);
                        if (cantFilV != null)
                        {
                            var domainFil = PowerWebContext.Current.User.Utenti_Fil.SingleOrDefault(ufil => ufil.Fil_Id == cantFilV.Fil_Id);
                            if (domainFil != null)
                            {
                                currentCantDomain = (DomainEnum)domainFil.Dominio_Utenti_Fil;

                                if (!PowerWebContext.Current.User.Fil_Inclusive)
                                    currentCantDomain = DomainEnum.View;
                            }
                        }

                        CantUtentiFilDictionary.Add(currentCantId, currentCantDomain);
                    }

                    #endregion

                    //if (currentCantDomain != DomainEnum.ViewUpdate)
                    //{
                    DomainEnum currentColDomain = DomainEnum.ViewUpdate;

                    #region ColDomainDict update

                    if (ColUtentiRespDictionary.ContainsKey(currentColId))
                        currentColDomain = ColUtentiRespDictionary[currentColId];
                    else
                    {
                        var colRespV = ColRespVs.SingleOrDefault(cr => cr.Col_Id == currentColId);
                        if (colRespV != null)
                        {
                            var domainResp = PowerWebContext.Current.User.Utenti_Resp.SingleOrDefault(uresp => uresp.Resp_Id == colRespV.Resp_Id);
                            if (domainResp != null)
                            {
                                currentColDomain = (DomainEnum)domainResp.Dominio_Utenti_Resp;

                                if (!PowerWebContext.Current.User.Resp_Inclusive)
                                    currentColDomain = DomainEnum.View;
                            }
                        }

                        ColUtentiRespDictionary.Add(currentColId, currentColDomain);
                    }

                    #endregion

                    if (currentCantDomain != DomainEnum.ViewUpdate || currentColDomain != DomainEnum.ViewUpdate)
                    {
                        if (currentCantDomain == DomainEnum.None && currentColDomain == DomainEnum.None)
                            isVisible = false;
                        else
                        {
                            if ("add" == buttonID || "addclone" == buttonID || "delete" == buttonID || "edit" == buttonID)
                            {
                                if (currentCantDomain == DomainEnum.View || currentColDomain == DomainEnum.View)
                                    isVisible = false;
                            }

                        }
                    }

                }
            }

            #endregion

            return isVisible;
        }

        private bool TripsFilter(Type entityType, int index, String buttonId)
        //DisAbilita tutti i tasti di Edit salvo la Visualzizazione per le Righe di Viaggio e di Arrotondamento per durata
        {
            //Chiamata dalla CustomButtonInizialize di questa GridMaster
            bool res = true;

            //if (RepoManager.ParamRepo.ParametersRow.Flag_Calcolo_Viaggi)
            if (entityType == typeof(Reg_V) && buttonId != "view")
            {
                int type = Convert.ToInt32(GridView.GetRowValues(index, CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg)));
                res = type != (int)RegTypeEnum.Trip && type != (int)RegTypeEnum.ArrotDur && type != (int)RegTypeEnum.RettTimesheet && type != (int)RegTypeEnum.RettTimeSheetManual;
            }

            return res;
        }

        private bool RettManualeFilter(Type entityType, int index, String buttonId)
        //DisAbilita tutti i tasti di Edit salvo la Visualzizazione per le Righe di Viaggio e di Arrotondamento per durata
        {
            //Chiamata dalla CustomButtonInizialize di questa GridMaster
            bool res = true;

            //if (RepoManager.ParamRepo.ParametersRow.Flag_Calcolo_Viaggi)
            if (entityType == typeof(Reg_V) && buttonId != "view")
            {
                int type = Convert.ToInt32(GridView.GetRowValues(index, CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg)));
                res = type != (int)RegTypeEnum.Trip && type != (int)RegTypeEnum.ArrotDur && type != (int)RegTypeEnum.RettTimeSheetManual;
            }

            return res;
        }


        /// <summary>
        /// Disabilita i tasti di edit (ma non di cancellazione) per le righe rettifica
        /// </summary>
        /// <param name="entityType">Il tipo di entità visualizzato dalla griglia</param>
        /// <param name="index">L'indice della griglia attualmente in elaborazione</param>
        /// <param name="buttonId">L'id del pulsante da visualizzare</param>
        /// <returns><c>true</c> se i pulsanti sono stati nascosti; altrimenti <c>false</c></returns>
        private bool CorrectionFilter(Type entityType, int index, string buttonId)
        {
            bool res = true;

            if (entityType == typeof(Reg_V) && buttonId != "view")
            {
                int type = Convert.ToInt32(GridView.GetRowValues(index, CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg)));
                res = type != (int)RegTypeEnum.RettTimesheet;
            }

            return res;
        }

        #region RENDE VISIBILI I TASTI DI ADD/MOD/DEL DELLA SINGOLA PAGINA IN BASE ALLE AUTORIZZAZIONI
        public void GridView_CustomButtonInitialize(object sender, ASPxGridViewCustomButtonEventArgs e)
        //Rende Visibili/Invisibili i Tasti di ADD/MOD/DEL della Singola Pagina in base alle Autorizzazioni lette dalla TAB_AUT         
        {
            //per quell'UTENTE/FUNZIONE
            //per quella FUNZIONE
            //per quel   UTENTE
            //lette dalla Routine Tab_Aut CurrentPageTabAut (sempre della GridMasterPage.master.cs)
            e.Enabled = false;
            e.Visible = DevExpress.Utils.DefaultBoolean.False;

            #region BOTTONE ADD
            if (e.CellType == GridViewTableCommandCellType.Filter)
            {
                //se il bottone è quello di inserimento
                if (e.ButtonID == "add")
                {
                    //viene controllato se l'utene in base alla tab aut è autorizzato ad eseguire l'inserimento oppure no
                    bool isToAdd = PowerWebContext.Current.User.IsUserAutorized(Utenti.OperationTypeEnum.Insert, CurrentPageTabAut, RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DefaultFunzAuthLevelEnum));

                    //Imposta Visibile e Accessibile il Tasto di ADD in base al risultato ottenuto da TAB_AUT
                    e.Enabled = isToAdd;
                    e.Visible = isToAdd ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False;
                }

            }
            #endregion

            if (e.CellType == GridViewTableCommandCellType.Data)
            {
                if (!TripsFilter(GridModule.EntityType, e.VisibleIndex, e.ButtonID))
                    return;

                if (!CorrectionFilter(GridModule.EntityType, e.VisibleIndex, e.ButtonID))
                    return;

                #region BOTTONE CLONE
                //se il bottone è quello di clonazione o clonazione multipla
                if (e.ButtonID == "addClone" || e.ButtonID == "addCloneMulti")
                {
                    //viene controllato se l'utene in base alla tab aut è autorizzato ad eseguire il clone oppure la clonazione multipla
                    bool isToAddClone = PowerWebContext.Current.User.IsUserAutorized(Utenti.OperationTypeEnum.Insert, CurrentPageTabAut, RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DefaultFunzAuthLevelEnum));

                    if (isToAddClone && RepoManager.ParamRepo.ParametersRow.DomainFilter != (int)DomainFilterEnum.None)
                        isToAddClone = DomainFilter(GridModule.EntityType, e.VisibleIndex, e.ButtonID);

                    e.Enabled = isToAddClone;
                    e.Visible = isToAddClone && !IsBatchModeEditing ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False;
                }
                #endregion

                #region BOTTONE CANCELLAZIONE
                //se il bottone è quello di cancellazione
                if (e.ButtonID == "delete")
                {
                    bool isDeletable = PowerWebContext.Current.User.IsUserAutorized(Utenti.OperationTypeEnum.Delete, CurrentPageTabAut, RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DefaultFunzAuthLevelEnum));

                    if (isDeletable && RepoManager.ParamRepo.ParametersRow.DomainFilter != (int)DomainFilterEnum.None)
                        isDeletable = DomainFilter(GridModule.EntityType, e.VisibleIndex, e.ButtonID);

                    e.Enabled = isDeletable;
                    e.Visible = isDeletable ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False;
                }
                #endregion

                #region BOTTONE VISUALIZZAZIONE
                //se il bottone è quello di visualizzazione
                if (e.ButtonID == "view")
                {

                    bool isToView = true;

                    if (RepoManager.ParamRepo.ParametersRow.DomainFilter != (int)DomainFilterEnum.None)
                        isToView = DomainFilter(GridModule.EntityType, e.VisibleIndex, e.ButtonID);

                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomerOnlyMultipleEditEvaluationEnum) == (int)CustomerOnlyMultipleEditEvaluationEnum.Enabled
                      && PowerWebContext.Current.User.Cli_Id.HasValue && e.ButtonID == "view")
                        isToView = false;

                    if (RepoManager.ParamRepo.IsCurrentUserCustomizationEnabled(CustomizationEnum.DisableViewRowButton, PowerWebContext.Current.User.Codice_Utente))
                    {
                        isToView = false;
                    }

                    e.Enabled = isToView;
                    e.Visible = isToView && !IsBatchModeEditing ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False;
                }
                #endregion

                #region BOTTONI DI EDIT VELOCE SINGOLA RIGA O MULTIRIGA
                if (e.ButtonID == "editMultiRow" || e.ButtonID == "editSingleRow")
                {


                    bool isToEdit = PowerWebContext.Current.User.IsUserAutorized(Utenti.OperationTypeEnum.Edit, CurrentPageTabAut, RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DefaultFunzAuthLevelEnum));

                    if (RepoManager.ParamRepo.ParametersRow.DomainFilter != (int)DomainFilterEnum.None && isToEdit)

                        isToEdit = DomainFilter(GridModule.EntityType, e.VisibleIndex, "edit");

                    e.Enabled = isToEdit;
                    e.Visible = isToEdit ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False;

                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomerOnlyMultipleEditEvaluationEnum) == (int)CustomerOnlyMultipleEditEvaluationEnum.Enabled
                        && PowerWebContext.Current.User.Cli_Id.HasValue && e.ButtonID == "editSingleRow")
                    {
                        e.Enabled = true;
                        e.Visible = DevExpress.Utils.DefaultBoolean.True;
                    }

                }
                #endregion
            }
        }
        #endregion

        private void hideOrShowExportButtons()
        //Esterno rispetto agli altri perchè chiamato anche quando cambia il Layout e non solo all'inizio come gli altri tasti Funzionali
        {
            // Abilita o meno il Tasto di Export Excel e PDF in base al fatto che
            // i Dati mostrati NON superino i 1000 Records
            if (GridModule != null && GridModule.GridView != null)
            {
                if (GridModule.GridView.VisibleRowCount > 1000)
                {
                //    btnPrintXlsx.ClientEnabled = false;
                //    btnPrintXlsx.Enabled = false;
                //    btnPrintPdf.ClientEnabled = false;
                //    btnPrintPdf.Enabled = false;
                //}
                //else {
                //    btnPrintXlsx.ClientEnabled = true;
                //    btnPrintXlsx.Enabled = true;
                //    btnPrintPdf.ClientEnabled = true;
                //    btnPrintPdf.Enabled = true;
                }
                

            }
        }

        #endregion

        #region Attiva in modo Read/Only i Campi dell'Edit della MasterGrid quando si preme il tasto di VISUALIZZAZIONE
        public void GridView_CellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        //Setta a ReadOnly Tutti i campi dell'Edit quando viene premuto il tasto di EDIT = Visualizzazione
        {
            ASPxGridView gridView = sender as ASPxGridView;
            if (gridView != null)
            {

                var isReadOnly = PowerWebContext.GetFromSession<bool>("IsReadOnly_" + gridView.ClientID);

                if (isReadOnly)
                    e.Editor.ReadOnly = true;
            }
        }

        protected void cResetIsReadOnly_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        //Disattiva il ReadOnly dei Campi dell?EDIT della Griglia Master
        {
            var gridIntanceId = e.Parameter;
            PowerWebContext.SetToSession("IsReadOnly_" + gridIntanceId, false);
        }
        #endregion

        #region Restituisce i Valori ricevuti dall'Edit del Singolo Campo della Griglia master convertito
        //secondo le necessità i Valori di Default dei Vari Tipi di campo della Grid Masetr in base al Tipo di Campo
        public void GridView_ParseValue(object sender, ASPxParseValueEventArgs e)
        //Per i Campi dell'EDIT della Griglia Master
        {
            if (GridModuleTypes.ContainsKey(e.FieldName))
            {
                if (e.Value != null)
                {
                    int hours = 0;
                    int minutes = 0;

                    var stringValue = e.Value.ToString();

                    if (stringValue.Length > 4)
                    {
                        hours = Convert.ToInt32(stringValue.Substring(0, 2));
                        minutes = Convert.ToInt32(stringValue.Substring(3, 2));
                    }
                    else
                    {
                        hours = Convert.ToInt32(stringValue.Substring(0, 2));
                        minutes = Convert.ToInt32(stringValue.Substring(2, 2));
                    }

                    e.Value = new TimeSpan(hours, minutes, 0);
                }
                else if (GridModuleTypes.Single(dic => dic.Key == e.FieldName).Value == typeof(TimeSpan))
                {
                    e.Value = TimeSpan.MinValue;
                }
                else
                {
                    e.Value = null;
                }
            }

        }


        public void DetailGridView_ParseValue(object sender, ASPxParseValueEventArgs e)
        //Per i Campi dell'EDIT della Griglia Master
        {
            if (DetailGridModuleTypes != null)
            {
                if (DetailGridModuleTypes.ContainsKey(e.FieldName))
                {
                    if (e.Value != null)
                    {
                        var hours = Convert.ToInt32(e.Value.ToString().Substring(0, 2));
                        var minutes = Convert.ToInt32(e.Value.ToString().Substring(2, 2));
                        e.Value = new TimeSpan(hours, minutes, 0);
                    }
                    else if (DetailGridModuleTypes.Single(dic => dic.Key == e.FieldName).Value == typeof(TimeSpan))
                    {
                        e.Value = TimeSpan.Zero;
                    }
                    else
                    {
                        e.Value = null;
                    }
                }
            }
        }
        #endregion

        #region Gestisce Drag/Drop dei Raggruppamenti/Ordinamenti nella Pagina di gestione Parametri di Stampa

        protected void tlGrouping_ProcessDragNode(object sender, DevExpress.Web.ASPxTreeList.TreeListNodeDragEventArgs e)
        {

            GroupingTreeListItem child = e.Node.DataItem as GroupingTreeListItem;

            if (child != null)
            {
                if (String.IsNullOrEmpty(e.NewParentNode.Key))
                {
                    child.ParentId = -1;
                    child.IsInGrouping = false;
                    if (e.Node.HasChildren)
                        DeselectGroupTreeNode(e.Node.ChildNodes);
                }
                else
                {
                    GroupingTreeListItem newParent = e.NewParentNode.DataItem as GroupingTreeListItem;
                    if (newParent.IsInGrouping)
                    {
                        if (e.NewParentNode.HasChildren)
                        {
                            GroupingTreeListItem currentChild = e.NewParentNode.ChildNodes[0].DataItem as GroupingTreeListItem;
                            currentChild.ParentId = child.Id;
                        }
                        child.ParentId = newParent.Id;
                        child.IsInGrouping = true;
                    }
                }
            }

            e.Cancel = true;
            e.Handled = true;
            BindGroupingTree();
        }

        //colorazione del backguond della riga durante il raggruppamento nel popup del menu di stampa
        protected void tlGrouping_HtmlRowPrepared(object sender, DevExpress.Web.ASPxTreeList.TreeListHtmlRowEventArgs e)
        {
            GroupingTreeListItem fake = null;

            bool IsInGrouping = false;

            //estraggo la proprietà nel caso di raggruppamento nella Tree List 
            IsInGrouping = Convert.ToBoolean(e.GetValue(CommonService.GetPropertyName(() => fake.IsInGrouping)));

            //se sono nel caso di raggruppamento
            if (IsInGrouping)
            {
                //converto il booleano in un intero per lavorare con gli elementi dell'enum
                int grouping = Convert.ToInt32(IsInGrouping);
                e.Row.BackColor = RepoManager.ParamRepo.GetColorFromEnum((IsInGroupingEnum)grouping, true);
                e.Row.Font.Bold = true;
            }

            // recupero i combobox delle colonne interessate all'aggiornamento dei checkbox
            var nodeToProcess = tlGrouping.FindNodeByKeyValue(e.NodeKey);
            var nodeDataItem = nodeToProcess.DataItem as GroupingTreeListItem;
            var isSkipPageColumn = tlGrouping.Columns[CommonService.GetPropertyName(() => nodeDataItem.IsSkipPage)] as TreeListCheckColumn;
            var repeatEveryPageColumn = tlGrouping.Columns[CommonService.GetPropertyName(() => nodeDataItem.RepeatEveryPage)] as TreeListCheckColumn;

            var isSinglePageChkbox = (ASPxCheckBox)tlGrouping.FindDataCellTemplateControl(e.NodeKey, isSkipPageColumn, "cbIsSkipPage");
            var repeatEveryPageChkbox = (ASPxCheckBox)tlGrouping.FindDataCellTemplateControl(e.NodeKey, repeatEveryPageColumn, "cbRepeatEveryPage");

            // impostazione dello stato di check del checkbox in base al valore del nodo
            isSinglePageChkbox.Checked = nodeDataItem.IsSkipPage;
            repeatEveryPageChkbox.Checked = nodeDataItem.RepeatEveryPage;

        }

        protected void tlGrouping_NodeUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            GroupingTreeListItem currentItem = GroupingTreeData.SingleOrDefault(pit => pit.Id == (int)e.Keys[TREEKEYFIELDNAME]);
            if (currentItem != null)
            {
                currentItem.IsSkipPage = Convert.ToBoolean(e.NewValues[CommonService.GetPropertyName(() => currentItem.IsSkipPage)]);
                currentItem.RepeatEveryPage = Convert.ToBoolean(e.NewValues[CommonService.GetPropertyName(() => currentItem.RepeatEveryPage)]);
            }
            e.Cancel = true;
            tlGrouping.CancelEdit();
            BindGroupingTree();
        }

        private void DeselectGroupTreeNode(TreeListNodeCollection nodes)
        {
            foreach (TreeListNode node in nodes)
            {
                GroupingTreeListItem currentData = node.DataItem as GroupingTreeListItem;
                if (currentData != null)
                {
                    currentData.ParentId = -1;
                    currentData.IsInGrouping = false;
                }

                if (node.HasChildren)
                    DeselectGroupTreeNode(node.ChildNodes);
            }
        }



        private void AddSelectedTreeItems(TreeListNode node, List<GroupingTreeListItem> groups)
        {
            // recupero i combobox delle colonne interessate all'aggiornamento dei checkbox
            var nodeToProcess = tlGrouping.FindNodeByKeyValue(node.Key);
            var nodeDataItem = nodeToProcess.DataItem as GroupingTreeListItem;
            var isSkipPageColumn = tlGrouping.Columns[CommonService.GetPropertyName(() => nodeDataItem.IsSkipPage)] as TreeListCheckColumn;
            var repeatEveryPageColumn = tlGrouping.Columns[CommonService.GetPropertyName(() => nodeDataItem.RepeatEveryPage)] as TreeListCheckColumn;

            var isSkipPageChkbox = (ASPxCheckBox)tlGrouping.FindDataCellTemplateControl(node.Key, isSkipPageColumn, "cbIsSkipPage");
            var repeatEveryPageChkbox = (ASPxCheckBox)tlGrouping.FindDataCellTemplateControl(node.Key, repeatEveryPageColumn, "cbRepeatEveryPage");

            GroupingTreeListItem currentItem = node.DataItem as GroupingTreeListItem;
            if (currentItem != null)
            {
                if (currentItem.Field != "Root")
                {
                    currentItem.RepeatEveryPage = repeatEveryPageChkbox.Checked;
                    currentItem.IsSkipPage = isSkipPageChkbox.Checked;
                    groups.Add(currentItem);
                }
            }

            if (node.HasChildren)
                foreach (TreeListNode childNode in node.ChildNodes)
                    AddSelectedTreeItems(childNode, groups);
        }

        private void DeselectOrderTreeNode(TreeListNodeCollection nodes)
        {
            foreach (TreeListNode node in nodes)
            {
                OrderingTreeListItem currentData = node.DataItem as OrderingTreeListItem;
                if (currentData != null)
                {
                    currentData.ParentId = -1;
                    currentData.IsInOrdering = false;
                }

                if (node.HasChildren)
                    DeselectOrderTreeNode(node.ChildNodes);
            }
        }
        #endregion

        #region Gestione degli Eventi scatenati dal Click sui tasti Funzionali di PrintPDF/PrintXLS/STampa/Export Excel Custom
        //Gestisce il Tasto di Lancio della Stampa diretta in PDF  
        protected void btnPrintPdf_Click(object sender, EventArgs e)
        {
            ASPxGridViewExporter gvExporter = new ASPxGridViewExporter();
            gvExporter.GridViewID = GridPage.GridModule.GridView.ID;

            gvExporter.RenderBrick += gvExporter_RenderBrick;
            gvExporter.Landscape = true;
            gvExporter.LeftMargin = 0;
            gvExporter.RightMargin = 0;
            gvExporter.TopMargin = 0;
            gvExporter.BottomMargin = 0;
            gvExporter.PageHeader.Center = BusinessService.GetLocalizedString(gvExporter.GridViewID, ResourceTypeEnum.Grid);
            Page.Controls.Add(gvExporter);
            gvExporter.PageFooter.Left = "Printed by Power Web";
            gvExporter.PageFooter.Center = "Pagina: [Page # of Pages #]";
            gvExporter.PageFooter.Right = DateTime.Now.ToString();
            gvExporter.WritePdfToResponse();
            gvExporter.Styles.Cell.Height = 100;


        }

        //Gestisce il Tasto di Lancio dell'Export Excel diretto
        protected void btnPrintXlsx_Click(object sender, EventArgs e)
        {
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportExcelEnum);
            ASPxGridViewExporter gvExporter = new ASPxGridViewExporter();
            gvExporter.GridViewID = GridPage.GridModule.GridView.ID;
            Page.Controls.Add(gvExporter);
            gvExporter.RenderBrick += gvExporter_RenderBrick;
            gvExporter.WriteXlsxToResponse();
        }

        void gvExporter_RenderBrick(object sender, ASPxGridViewExportRenderingEventArgs e)
        //Evento chiamato da DevExpress quando si stanno stampando le Colonne in Excel/Pdf
        {
            //Usato per formattare ad esempio i Campi Date
            GridViewDataColumn dataColumn = e.Column as GridViewDataColumn;
            e.BrickStyle.SetAlignment(HorzAlignment.Default, VertAlignment.Center);
            if (e.RowType == GridViewRowType.Data && dataColumn != null && dataColumn is GridViewDataTextColumn)
            {
                var textColumn = (GridViewDataTextColumn)dataColumn;
                //e.BrickStyle.SetAlignment(HorzAlignment.Default, VertAlignment.Center);

                if (textColumn != null && textColumn.PropertiesTextEdit.MaskSettings.Mask == "00:00")
                {
                    e.TextValueFormatString = "hh:mm";
                }
            }
        }

        //Gestisce il Tasto di Lancio della Stampa con Report
        protected void btnPrintReport_Click(object sender, EventArgs e)
        {
            BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = false;

            // viene recuperata la griglia utilizzata per la stampa
            ASPxGridView printGridView = GetPrintGrid();

            //se ho righe da visualizzare nella griglia
            if (printGridView.VisibleRowCount > 0)
            {
                List<TabPageExtended> unselectedTabs = new List<TabPageExtended>();
                List<TabPageItemExtended> FieldsEditFromTemplate = new List<TabPageItemExtended>();
                Dictionary<string, int> reportOptions = new Dictionary<string, int>();
                //carica i Nomi dei TABS definiti nell'EditDictionaryManager per quello specifico Modulo 
                foreach (ListEditItem item in lbPrintOptions.Items)
                {
                    if (!item.Selected && !item.Value.ToString().StartsWith("OPZ"))
                    {
                        int currentTabId = Convert.ToInt32(item.Value);
                        TabPageExtended currentTPE =
                            GridModule.EditFormTemplate.Dic.Keys.SingleOrDefault(tpe => tpe.Id == currentTabId);
                        if (currentTPE != null)
                            unselectedTabs.Add(currentTPE);
                    }

                    // se si tratta di un'opzione report
                    if (item.Value.ToString().StartsWith("OPZ"))
                    {
                        // inserisco l'opzione nel corrispettivo dizionario utilzizando come chiave il valore da tradurre e come
                        // valore lo stato dell'opzione
                        reportOptions.Add(item.Value.ToString(), Convert.ToInt32(item.Selected));
                    }

                }

                List<GroupingTreeListItem> groups = new List<GroupingTreeListItem>();
                AddSelectedTreeItems(tlGrouping.Nodes[0], groups);


                int currentReportTypeId = Convert.ToInt32(cmbPrintLayout.Value); //(cmbPrintLayout.SelectedItem.Value); 

                Tab_Report currentTabReport = RepoManager.Tab_ReportRepo.Single(trr => trr.Report_Id == currentReportTypeId);

                printGridView.ExpandAll();

                if (PrintModule != null)
                {
                    List<Object> items = new List<Object>();
                    {
                        //creo una lista di oggetti in base alle visible rows che poi esporterò nei report
                        for (int i = 0; i < printGridView.VisibleRowCount; i++)
                        {
                            //controllo in caso di ragruppamneto se sono una child rows o una detail rows
                            //mi ritorna le righe figlie di un specifico ragguppamento
                            //ritorno true se la riga i è una riga di raggruppamento, false se non ho ragruppamenti o sono in una riga di dettaglio
                            var groupedRow = printGridView.GetChildRow(i, 0) != null;

                            if (printGridView.Selection.Count == 0 || printGridView.Selection.IsRowSelected(i))
                            {
                                var currentRow = printGridView.GetRow(i);

                                if (currentRow != null)
                                {
                                    //se sono su una riga master di raggruppamento allora non aggiungo oggentti alla lista
                                    //aggiungo solo le righe di dettaglio cioè in caso di groupedRow==false, in questo modo non faccio un doppio inserimento
                                    if (!groupedRow)
                                        items.Add(currentRow);

                                }

                            }
                        }
                    }

                    ExtXtraReport ExtReport = PrintModule.GetReport(currentTabReport, unselectedTabs, reportOptions, groups, items);

                    XtraReport xrReport = ExtReport.Report;

                    #region Layout & Format report

                    groups.Reverse();

                    int groupHeader = 0;

                    //vado a gestire tutto il layout e il formato delle celle nel foglio di report
                    //vado ad analizzare ogni gruppo se li ho selezionati nel popup di stampa
                    foreach (GroupingTreeListItem group in groups)
                    {
                        GroupHeaderBand gh = new GroupHeaderBand();
                        // se si stanno trattando le reg_v
                        if (printGridView.ID == "gvRegV" || printGridView.ID == "gvRegVM")
                        {
                            // se si sta raggruppando per id cantiere o id collaboratore allora si procede al raggruppamento per codice cantiere/collaboraotre
                            switch (group.Field)
                            {
                                case "Col_Id":
                                    gh.GroupFields.Add(new GroupField("Col_Mnemonic"));
                                    break;
                                case "Cant_Id":
                                    gh.GroupFields.Add(new GroupField("Cant_Mnemonic"));
                                    break;
                                case "Cli_Id":
                                    gh.GroupFields.Add(new GroupField("Cli_Mnemonic"));
                                    break;
                                default:
                                    gh.GroupFields.Add(new GroupField(group.Field));
                                    break;
                            }
                        }
                        else
                            gh.GroupFields.Add(new GroupField(group.Field));

                        gh.HeightF = 0.0f;

                        #region Group Header Table - Header

                        //variabile che mi dice se sono in presenza di ragruppamenti nel report
                        groupHeader = 1;

                        List<Tab_GridLookup> gridLookups = RepoManager.Tab_GridLookupRepo.Find(tgl => tgl.NomeRicerca == group.Field && tgl.Campo_Report).ToList();

                        //vado a costruire le headertabelle in base al raggruppamento
                        XRTable groupHeaderTable = new XRTable()
                        {
                            WidthF = CommonServiceReport.TABLE_WIDTHF,
                            LocationF = new PointF(CommonServiceReport.TABLE_LOCATIONF_X, 0.0f),
                            //imposto il colore del background della tabella che mi rappresenta il ragruppamento
                            BackColor = RepoManager.ParamRepo.GetColorFromEnum((GroupHeaderTableEnum)groupHeader, true)
                        };

                        float cellWidth = groupHeaderTable.WidthF;

                        XRTableRow groupHeaderTableRow = new XRTableRow();

                        cellWidth = groupHeaderTable.WidthF / 2.0f;

                        if (gridLookups.Count != 0)
                        {
                            XRTableCell valueCell = new CustomXRTableCell(gridLookups);
                            valueCell.TextAlignment = TextAlignment.MiddleLeft;
                            valueCell.Padding = new PaddingInfo(4, 4, 4, 4);
                            valueCell.Font = new Font(valueCell.Font, FontStyle.Bold | FontStyle.Italic);
                            valueCell.WidthF = groupHeaderTable.Width;
                            //valueCell.Text = BusinessService.GetLocalizedString(gridLookupCellText.ToString());
                            groupHeaderTableRow.Cells.Add(valueCell);
                        }
                        else
                        {
                            XRTableCell captionCell = new XRTableCell();
                            captionCell.TextAlignment = TextAlignment.MiddleLeft;
                            captionCell.Padding = new PaddingInfo(4, 4, 4, 4);
                            captionCell.Font = new Font(captionCell.Font, FontStyle.Bold | FontStyle.Italic);
                            captionCell.WidthF = cellWidth;
                            captionCell.Text = BusinessService.GetLocalizedString(group.Field, ResourceTypeEnum.Field);
                            groupHeaderTableRow.Cells.Add(captionCell);

                            XRTableCell valueCell = null;
                            valueCell = new XRTableCell();
                            valueCell.DataBindings.Add(new XRBinding(CommonService.GetPropertyName(() => valueCell.Text), null, group.Field));
                            groupHeaderTableRow.Cells.Add(valueCell);

                            valueCell.Font = new Font(captionCell.Font, FontStyle.Italic);
                            valueCell.TextAlignment = TextAlignment.MiddleLeft;
                            valueCell.Padding = new PaddingInfo(4, 4, 4, 4);
                            valueCell.WidthF = cellWidth * 2;

                            groupHeaderTableRow.Cells.Add(valueCell);
                        }

                        groupHeaderTable.Rows.Add(groupHeaderTableRow);
                        gh.Controls.Add(groupHeaderTable);

                        #endregion

                        // se è richiesta la ripetizione dell'elemento ad ogni pagina si imposta la relativa proprietà a true; altrimenti la si imposta a false;
                        gh.RepeatEveryPage = group.RepeatEveryPage;

                        xrReport.Bands.Add(gh);

                        GroupFooterBand gf = new GroupFooterBand();
                        gf.HeightF = 0.0f;
                        if (group.IsSkipPage)
                            gf.PageBreak = PageBreak.AfterBand;
                        xrReport.Bands.Add(gf);

                    }

                    xrReport.Bands[BandKind.ReportHeader].Visible = cbxPrintCover.Checked;

                    foreach (Parameter parameter in xrReport.Parameters)
                    {
                        if (parameter.Name.StartsWith("Eti"))
                        {
                            String currentParamName = parameter.Name.Substring(parameter.Name.IndexOf("_") + 1);

                            String currentParamValue = BusinessService.GetLocalizedString("FLD_" + currentParamName);

                            if (String.IsNullOrEmpty(currentParamValue))
                                currentParamValue = BusinessService.GetLocalizedString(currentParamName,
                                    ResourceTypeEnum.Field);

                            parameter.Value = currentParamValue;
                        }

                        if (parameter.Name.StartsWith("Str") || parameter.Name.StartsWith("Lbl"))
                        {
                            String currentParamValue = BusinessService.GetLocalizedString(parameter.Name);
                            if (String.IsNullOrEmpty(currentParamValue))
                                currentParamValue = parameter.Name;

                            parameter.Value = currentParamValue;
                        }
                        if (parameter.Name.StartsWith("Conf"))
                        {
                            String currentParamName = parameter.Name.Substring(parameter.Name.IndexOf("_") + 1);

                            Param parameters = RepoManager.ParamRepo.ParametersRow;

                            foreach (PropertyInfo property in parameters.GetType().GetProperties())
                            {
                                if (property.Name == currentParamName)
                                {
                                    parameter.Value = property.GetValue(parameters, null);
                                    break;
                                }
                            }
                        }

                        if (parameter.Name.StartsWith("ReportTitle"))
                        {
                            // verifica della personalizzazione riguardante il titolo del report
                            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ReportTitleUsingSavedNameEnum);

                            if (customizationVersion == (int)ReportTitleUsingSavedNameEnum.StardardTitle)
                                parameter.Value = BusinessService.GetLocalizedString(currentTabReport.Nome_Risorsa).ToUpper();
                            else
                                parameter.Value = cmbPrintLayout.Text.ToUpper();
                        }
                    }

                    byte[] companyLogo = RepoManager.ParamRepo.ParametersRow.CompanyLogo;
                    if (companyLogo != null && ExtReport.PictureBox != null)
                    {
                        ExtReport.PictureBox.Image = Image.FromStream(new MemoryStream(companyLogo));
                        ExtReport.PictureBox.Sizing = ImageSizeMode.ZoomImage;
                    }

                    #endregion

                    BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = true;
                    CommonServiceReport.CreateReport(Response, BusinessService.GetLocalizedString(currentTabReport.Nome_Risorsa), true, xrReport);
                }

                printGridView.CollapseAll();
            }
            BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = true;
        }

        //Gestisce il Tasto di Lancio degli eventuali Export CUSTOM
        protected void btnExportXLSX_Click(object sender, EventArgs e)
        {
            BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = false;

            // calcolo della griglia di export
            ASPxGridView exportGrid = GetExportGrid();

            if (exportGrid.VisibleRowCount > 0)
            {
                List<Object> items = new List<Object>();

                int currentModelId = Convert.ToInt32(cmbExportXLSXLayout.Value);
                var currentModel = ExportXLSXModule.Models.Single(mdl => mdl.ExcelModel_Id == currentModelId);

                CriteriaOperator op = CriteriaOperator.Parse(exportGrid.FilterExpression);

                var exportEntityType = Type.GetType($"Domain.{currentModel.Nome_Entity},Domain");

                if (exportEntityType == typeof(Cant))
                {
                    var sqlWhere = DevExpress.Data.Filtering.CriteriaToWhereClauseHelper.GetMsSqlWhere(op);
                    var cants = RepoManager.Reg_VRepo.Context.Database.SqlQuery<Cant>(PowerWebService.GenerateWhereQuery(typeof(Cant), exportGrid, string.Empty)).ToList();

                    ExportXLSXModule.ExportXLSX(currentModel, cants.Cast<Object>().ToList());
                }
                else
                {

                    var regs = RepoManager.Reg_VRepo.Context.Database.SqlQuery<Reg_V>(PowerWebService.GenerateWhereQuery(typeof(Reg_V), exportGrid, RepoManager.Reg_VRepo.FilterText)).ToList();

                    
                    ExportXLSXModule.ExportXLSX(currentModel, regs.Cast<Object>().ToList());
                }

            }
            BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = true;

        }

        protected void cPingLoadingPanel_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {
            //  Restituisce quanto caricato nell'ImportDataStatusDictionary dalla Routine di CALCULATE 
            //  StatusKey : Contiene il Nome della Tabella che si sta importando in quel momento                               
            //  Valore    : Contiene la Percentuale (calcolata in base al N° di Tabelle da caricare) di Caricamento rispetto al Totale
            if (BusinessService.IsToCloseLoadingPanel.ContainsKey(PowerWebContext.Current.User))
            //Se ci sono dati nel DictionaryStatus allora li carica nel Risultato da mostrare a Video
            {
                var status = BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User];
                e.Result = status.ToString();
            }
        }

        #endregion


        #region Gestione delle Viste della GridMaster e della pagina di gestione Stampe
        protected void cpLayout_Callback(object sender, CallbackEventArgsBase e)
        //Gestice il CaricamentO/Salvataggio/Cancellazione delle Viste di Layout relative alle DataGrid Master
        //Gestisce il Tasto di ON/OFF
        //Gestisce il Tipo di EDITING scelto dall'Utente (Edit Diretto da griglia o con Apertura Pagina di Edit da Tasto Funzionale)
        {
            ASPxCallbackPanel currentPanel = sender as ASPxCallbackPanel;

            if (GridPage != null && currentPanel != null)
            {
                currentPanel.JSProperties["cpErrorTitle"] = String.Empty;
                currentPanel.JSProperties["cpErrorMessage"] = String.Empty;

                #region Load client layout

                int selectedItemIndex = int.MinValue;

                if (Int32.TryParse(e.Parameter, out selectedItemIndex))
                {
                    if (selectedItemIndex != int.MinValue)
                    {
                        Tab_DataGrid currentLayout = null;
                        if (selectedItemIndex != -1)
                            currentLayout = RepoManager.Tab_DataGridRepo.SingleOrDefault(tdg => tdg.DataGrid_Id == selectedItemIndex);

                        if (currentLayout != null)
                        {
                            currentLayout.Data_Layout_DataGrid = DateTime.UtcNow;

                            CustomizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutomaticDateTimeView);
                            //se la personalizzazione non è attiva
                            if (CustomizationVersion != 0)
                            {

                                UpdateDataRegInLayout(currentLayout);
                            }

                            RepoManager.Tab_DataGridRepo.Update(currentLayout, true);

                            GridView.LoadClientLayout(currentLayout.Layout_DataGrid);

                            // se sono in una double grid allora carico anche il layout salvato
                            if (DoubleGridModule != null)
                            {
                                var currentLayout2 = RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == DoubleGridModule.GridView2.ID && currentLayout.Nome_Layout == tdg.Nome_Layout);

                                if (currentLayout2 != null)
                                {
                                    currentLayout2.Data_Layout_DataGrid = DateTime.UtcNow;

                                    UpdateDataRegInLayout(currentLayout2);

                                    RepoManager.Tab_DataGridRepo.Update(currentLayout2, true);

                                    DoubleGridModule.GridView2.LoadClientLayout(currentLayout2.Layout_DataGrid);
                                }
                            }

                            // se sono in una triple grid allora carico anche il layout salvato
                            if (TripleGridModule != null)
                            {
                                var currentLayout3 = RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == TripleGridModule.GridView3.ID && currentLayout.Nome_Layout == tdg.Nome_Layout);

                                if (currentLayout3 != null)
                                {
                                    currentLayout3.Data_Layout_DataGrid = DateTime.UtcNow;

                                    UpdateDataRegInLayout(currentLayout3);

                                    RepoManager.Tab_DataGridRepo.Update(currentLayout3, true);

                                    TripleGridModule.GridView3.LoadClientLayout(currentLayout3.Layout_DataGrid);
                                }
                            }
                            if (QuadGridModule != null)
                            {
                                var currentLayout4 = RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == QuadGridModule.GridView4.ID && currentLayout.Nome_Layout == tdg.Nome_Layout);

                                if (currentLayout4 != null)
                                {
                                    currentLayout4.Data_Layout_DataGrid = DateTime.UtcNow;

                                    UpdateDataRegInLayout(currentLayout4);

                                    RepoManager.Tab_DataGridRepo.Update(currentLayout4, true);

                                    QuadGridModule.GridView4.LoadClientLayout(currentLayout4.Layout_DataGrid);
                                }
                            }
                        }
                        else
                        {
                            GridView.LoadClientLayout(PowerWebContext.GetFromSession<String>("GridLayout_" + GridView.ID));
                            if (!ReferenceEquals(GridModule.DefaultFilter, null))
                            {
                                GridView.FilterExpression = GridModule.DefaultFilter.ToString();
                                GridView.FilterEnabled = true;
                                GridView.DataBind();
                            }

                            ManageDoubleAndTripleGridLayout(null, null, null);
                        }

                        BindLayoutCombo();
                    }

                }

                #endregion

                #region Save client layout
                if (e.Parameter == "populate")
                //Gestione del dataBind della Griglia quando si setta il tasto di ON/OFF = ON
                {

                    GridPage.GridModule.IsToPopulateGrid = !GridPage.GridModule.IsToPopulateGrid;
                    GridView.DataBind();

                    hideOrShowExportButtons();
                }

                if (e.Parameter == "save")
                {
                    if (cmbLayout.Text.Trim() != string.Empty)
                    {
                        String currentLayout = GridPage.GridModule.GridView.SaveClientLayout();
                        String currentLayout2 = DoubleGridModule != null ? DoubleGridModule.GridView2.SaveClientLayout() : null;
                        String currentLayout3 = TripleGridModule != null ? TripleGridModule.GridView3.SaveClientLayout() : null;
                        String currentLayout4 = QuadGridModule != null ? QuadGridModule.GridView4.SaveClientLayout() : null;

                        if (cmbLayout.Text.ToUpper() != "DEFAULT")
                        {
                            if (PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level)
                            //Se l'utente ha un Livello >= al Livello di Admin definito in Tab Param allora il Layout veine salvato SENZA UTENTE
                            {
                                Tab_DataGrid savedLayout = RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == GridView.ID
                                   && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == null);

                                // in caso di double grid module allora recupero il layout da modificare per la seconda griglia
                                var savedLayout2 = DoubleGridModule != null ? RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == DoubleGridModule.GridView2.ID
                                   && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == null) : null;

                                // in caso di triple grid module allora recupero il layout da modificare per la terza griglia
                                var savedLayout3 = TripleGridModule != null ? RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == TripleGridModule.GridView3.ID
                                   && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == null) : null;

                                // in caso di quad grid module allora recupero il layout da modificare per la terza griglia
                                var savedLayout4 = QuadGridModule != null ? RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == QuadGridModule.GridView4.ID
                                    && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == null) : null;

                                if (savedLayout == null)
                                {
                                    RepoManager.Tab_DataGridRepo.Add(new Tab_DataGrid
                                    {
                                        Nome_DataGrid = GridView.ID,
                                        Layout_DataGrid = currentLayout,
                                        Nome_Layout = cmbLayout.Text,
                                        Data_Layout_DataGrid = DateTime.UtcNow,
                                    }, true);

                                    // se sono in un double grid module allora salvo anche la seconda vista
                                    if (DoubleGridModule != null)
                                    {
                                        RepoManager.Tab_DataGridRepo.Add(new Tab_DataGrid
                                        {
                                            Nome_DataGrid = DoubleGridModule.GridView2.ID,
                                            Layout_DataGrid = currentLayout2,
                                            Nome_Layout = cmbLayout.Text,
                                            Data_Layout_DataGrid = DateTime.UtcNow,
                                        }, true);
                                    }

                                    // se sono in un triple grid module allora salvo anche la terza vista
                                    if (TripleGridModule != null)
                                    {
                                        RepoManager.Tab_DataGridRepo.Add(new Tab_DataGrid
                                        {
                                            Nome_DataGrid = TripleGridModule.GridView3.ID,
                                            Layout_DataGrid = currentLayout3,
                                            Nome_Layout = cmbLayout.Text,
                                            Data_Layout_DataGrid = DateTime.UtcNow,
                                        }, true);
                                    }
                                    // se sono in una quad grid module allora salvo anche la quarta vista
                                    if (QuadGridModule != null)
                                    {
                                        RepoManager.Tab_DataGridRepo.Add(new Tab_DataGrid
                                        {
                                            Nome_DataGrid = QuadGridModule.GridView4.ID,
                                            Layout_DataGrid = currentLayout4,
                                            Nome_Layout = cmbLayout.Text,
                                            Data_Layout_DataGrid = DateTime.UtcNow,
                                        }, true);
                                    }
                                }
                                else
                                {
                                    savedLayout.Layout_DataGrid = currentLayout;
                                    savedLayout.Data_Layout_DataGrid = DateTime.UtcNow;
                                    RepoManager.Tab_DataGridRepo.Update(savedLayout, true);

                                    // se sono in un double grid module aggiorno anche il relativo layout
                                    if (DoubleGridModule != null)
                                    {
                                        savedLayout2.Layout_DataGrid = currentLayout2;
                                        savedLayout2.Data_Layout_DataGrid = DateTime.UtcNow;
                                        RepoManager.Tab_DataGridRepo.Update(savedLayout2, true);
                                    }

                                    // se sono in un triple grid module aggiorno anche il relativo layout
                                    if (TripleGridModule != null)
                                    {
                                        savedLayout3.Layout_DataGrid = currentLayout3;
                                        savedLayout3.Data_Layout_DataGrid = DateTime.UtcNow;
                                        RepoManager.Tab_DataGridRepo.Update(savedLayout3, true);
                                    }

                                    // se sono in una quad grid module aggiorno anche il relativo layout
                                    if (QuadGridModule != null)
                                    {
                                        savedLayout4.Layout_DataGrid = currentLayout4;
                                        savedLayout4.Data_Layout_DataGrid = DateTime.UtcNow;
                                        RepoManager.Tab_DataGridRepo.Update(savedLayout4, true);
                                    }
                                }
                            }
                            else
                            {
                                //Se l'Utente ha un livello < del Livello di Admin definito in Tab Param allora il Layout viene salvato CON UTENTE
                                Tab_DataGrid savedLayout = RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == GridView.ID
                                    && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);

                                // in caso di double grid module allora recupero il layout da modificare per la seconda griglia
                                var savedLayout2 = DoubleGridModule != null ? RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == DoubleGridModule.GridView2.ID
                                   && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id) : null;

                                // in caso di triple grid module allora recupero il layout da modificare per la terza griglia
                                var savedLayout3 = TripleGridModule != null ? RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == TripleGridModule.GridView3.ID
                                   && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id) : null;

                                // in caso di quad grid module  allora recupero il layout da modificare per la quarta griglia
                                var savedLayout4 = QuadGridModule != null ? RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == QuadGridModule.GridView4.ID
                                    && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id) : null;

                                if (savedLayout == null)
                                {
                                    RepoManager.Tab_DataGridRepo.Add(new Tab_DataGrid
                                    {
                                        Utenti_Id = PowerWebContext.Current.User.Utenti_Id,
                                        Nome_DataGrid = GridView.ID,
                                        Layout_DataGrid = currentLayout,
                                        Nome_Layout = cmbLayout.Text,
                                        Data_Layout_DataGrid = DateTime.UtcNow,
                                    }, true);

                                    // se sono in un double grid module allora salvo anche la seconda vista
                                    if (DoubleGridModule != null)
                                    {
                                        RepoManager.Tab_DataGridRepo.Add(new Tab_DataGrid
                                        {
                                            Nome_DataGrid = DoubleGridModule.GridView2.ID,
                                            Layout_DataGrid = currentLayout2,
                                            Nome_Layout = cmbLayout.Text,
                                            Data_Layout_DataGrid = DateTime.UtcNow,
                                        }, true);
                                    }

                                    // se sono in un triple grid module allora salvo anche la terza vista
                                    if (TripleGridModule != null)
                                    {
                                        RepoManager.Tab_DataGridRepo.Add(new Tab_DataGrid
                                        {
                                            Nome_DataGrid = TripleGridModule.GridView3.ID,
                                            Layout_DataGrid = currentLayout3,
                                            Nome_Layout = cmbLayout.Text,
                                            Data_Layout_DataGrid = DateTime.UtcNow,
                                        }, true);
                                    }

                                    // se sono in un quad grid module allora salvo anche la quarta vista
                                    if (QuadGridModule != null)
                                    {
                                        RepoManager.Tab_DataGridRepo.Add(new Tab_DataGrid
                                        {
                                            Nome_DataGrid = QuadGridModule.GridView4.ID,
                                            Layout_DataGrid = currentLayout4,
                                            Nome_Layout = cmbLayout.Text,
                                            Data_Layout_DataGrid = DateTime.UtcNow,
                                        }, true);
                                    }
                                }
                                else
                                {
                                    savedLayout.Layout_DataGrid = currentLayout;
                                    savedLayout.Data_Layout_DataGrid = DateTime.UtcNow;
                                    RepoManager.Tab_DataGridRepo.Update(savedLayout, true);
                                }
                            }
                        }
                        else
                        //NESSUNO PUO MEMORIZZARE UNA DATAGRID con il NOME "DEFAULT"
                        {
                            currentPanel.JSProperties["cpErrorTitle"] = BusinessService.GetLocalizedString(PowerWebResources.STR_TITOLO_POPUP_ERRORE);
                            currentPanel.JSProperties["cpErrorMessage"] = BusinessService.GetLocalizedString(PowerWebResources.ERR_NO_SALVA_STRUTTURA_DEFAULT);

                            if (LogModule != null)
                            {
                                LogModule.Log.Error("il Layout NON appartiene all'Utente - Impossibile Modificarlo");
                            }
                        }
                    }
                    BindLayoutCombo(true);
                }

                // aggiornamento in ogni caso del testo del pulsante di on/off
                btnPopulateGrid.JSProperties.Add("cpNewText", GridPage.GridModule.IsToPopulateGrid ? "ON" : "OFF");
                #endregion

                #region Delete client layout

                if (e.Parameter == "delete")
                {
                    if (cmbLayout.Text.ToUpper() != "DEFAULT")
                    {
                        if (PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level)
                        //Se l'utente ha un Livello >= al Livello di Admin definito in Tab Param allora il Layout veine cercato SENZA UTENTE
                        {
                            Tab_DataGrid toDeleteTDG = RepoManager.Tab_DataGridRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == GridView.ID
                               && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == null);

                            if (DoubleGridModule != null)
                            {
                                var toDeleteTDG2 = RepoManager.Tab_DataGridRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == DoubleGridModule.GridView2.ID
                               && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == null);
                                RepoManager.Tab_DataGridRepo.Delete(toDeleteTDG2, false);
                            }

                            if (TripleGridModule != null)
                            {
                                var toDeleteTDG3 = RepoManager.Tab_DataGridRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == TripleGridModule.GridView3.ID
                               && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == null);
                                RepoManager.Tab_DataGridRepo.Delete(toDeleteTDG3, false);
                            }

                            if (QuadGridModule != null)
                            {
                                var toDeleteTDG4 = RepoManager.Tab_DataGridRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == QuadGridModule.GridView4.ID
                                    && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == null);
                                RepoManager.Tab_DataGridRepo.Delete(toDeleteTDG4, false);
                            }

                            RepoManager.Tab_DataGridRepo.Delete(toDeleteTDG, true);
                        }
                        else
                        //Se l'Utente ha un livello < del Livello di Admin definito in Tab Param allora il Layout viene cercato CON UTENTE
                        {
                            Tab_DataGrid userLayout = RepoManager.Tab_DataGridRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == GridView.ID
                                                           && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
                            if (userLayout != null)
                            {
                                if (DoubleGridModule != null)
                                {
                                    var toDeleteTDG2 = RepoManager.Tab_DataGridRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == DoubleGridModule.GridView2.ID
                                   && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
                                    RepoManager.Tab_DataGridRepo.Delete(toDeleteTDG2, false);
                                }

                                if (TripleGridModule != null)
                                {
                                    var toDeleteTDG3 = RepoManager.Tab_DataGridRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == TripleGridModule.GridView3.ID
                                   && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
                                    RepoManager.Tab_DataGridRepo.Delete(toDeleteTDG3, false);
                                }

                                if (QuadGridModule != null)
                                {
                                    var toDeleteTDG4 = RepoManager.Tab_DataGridRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == QuadGridModule.GridView4.ID
                                        && tdg.Nome_Layout == cmbLayout.Text && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
                                    RepoManager.Tab_DataGridRepo.Delete(toDeleteTDG4, false);
                                }

                                //Se il LaYout è SUO allora Viene Cancellato
                                RepoManager.Tab_DataGridRepo.Delete(userLayout, true);
                            }
                            else
                            //Se il LaYout NON è SUO allora NON Viene Cancellato
                            {
                                currentPanel.JSProperties["cpErrorTitle"] = BusinessService.GetLocalizedString(PowerWebResources.STR_TITOLO_POPUP_ERRORE);
                                currentPanel.JSProperties["cpErrorMessage"] = BusinessService.GetLocalizedString(PowerWebResources.ERR_STRUTTURA_NON_DELL_UTENTE);
                                if (LogModule != null)
                                {
                                    LogModule.Log.Error("Il Layout NON appartiene all'Utente che NON è quindi abilitato a Cancellarlo");
                                }
                            }
                        }
                    }


                    else
                    //Se il LaYout NON è SUO allora NON Viene Cancellato
                    {
                        currentPanel.JSProperties["cpErrorTitle"] = BusinessService.GetLocalizedString(PowerWebResources.STR_TITOLO_POPUP_ERRORE);
                        currentPanel.JSProperties["cpErrorMessage"] = BusinessService.GetLocalizedString(PowerWebResources.ERR_STRUTTURA_NON_DELL_UTENTE);
                        if (LogModule != null)
                        {
                            LogModule.Log.Error("Il Layout NON appartiene all'Utente che NON è quindi abilitato a Cancellarlo");
                        }
                    }

                    BindLayoutCombo(true);
                }
                #endregion
            }
        }

        private void ManageDoubleAndTripleGridLayout(Tab_DataGrid currentLayout2, Tab_DataGrid currentLayout3, Tab_DataGrid currentLayout4)
        {
            // se sono in una double grid allora carico anche il layout di default
            if (DoubleGridModule != null)
            {
                if (currentLayout2 == null)
                    DoubleGridModule.GridView2.LoadClientLayout(PowerWebContext.GetFromSession<String>("GridLayout2_" + DoubleGridModule.GridView2.ID));
                else
                    DoubleGridModule.GridView2.LoadClientLayout(currentLayout2.Layout_DataGrid);


                if (!ReferenceEquals(DoubleGridModule.DefaultFilter, null))
                {
                    DoubleGridModule.GridView2.FilterExpression = DoubleGridModule.DefaultFilter.ToString();
                    DoubleGridModule.GridView2.FilterEnabled = true;
                    DoubleGridModule.GridView2.DataBind();
                }
            }

            // se sono in una triple grid allora carico anche il layout di default
            if (TripleGridModule != null)
            {
                if (currentLayout3 == null)
                    TripleGridModule.GridView3.LoadClientLayout(PowerWebContext.GetFromSession<String>("GridLayout3_" + TripleGridModule.GridView3.ID));
                else
                    TripleGridModule.GridView3.LoadClientLayout(currentLayout3.Layout_DataGrid);


                if (!ReferenceEquals(TripleGridModule.DefaultFilter, null))
                {
                    TripleGridModule.GridView3.FilterExpression = TripleGridModule.DefaultFilter.ToString();
                    TripleGridModule.GridView3.FilterEnabled = true;
                    TripleGridModule.GridView3.DataBind();
                }
            }

            // se sono in una quad grid allora carico anche il layout di default
            if (QuadGridModule != null)
            {
                if (currentLayout4 == null)
                    QuadGridModule.GridView4.LoadClientLayout(PowerWebContext.GetFromSession<String>("GridLayout4_" + QuadGridModule.GridView4.ID));
                else
                    QuadGridModule.GridView4.LoadClientLayout(currentLayout4.Layout_DataGrid);

                if (!ReferenceEquals(QuadGridModule.DefaultFilter, null))
                {
                    QuadGridModule.GridView4.FilterExpression = QuadGridModule.DefaultFilter.ToString();
                    QuadGridModule.GridView4.FilterEnabled = true;
                    QuadGridModule.GridView4.DataBind();
                }
            }
        }

        protected void cpPrintLayout_Callback(object sender, CallbackEventArgsBase e)
        //Gestice il CaricamentO/Salvataggio/Cancellazione delle Viste di Layout della pagina di gestione dei Reportr
        {
            ASPxCallbackPanel currentPanel = sender as ASPxCallbackPanel;

            if (GridPage != null && currentPanel != null)
            {
                currentPanel.JSProperties["cpErrorTitle"] = String.Empty;
                currentPanel.JSProperties["cpErrorMessage"] = String.Empty;

                #region Load client layout

                int selectedItemIndex = int.MinValue;

                if (Int32.TryParse(e.Parameter, out selectedItemIndex))
                {
                    if (selectedItemIndex != int.MinValue)
                    {
                        if (selectedItemIndex == -2)
                        {
                            //Initialize print layout form
                            BindPrintLayoutCombo(true);
                        }
                        else
                        {
                            Tab_Report currentReport = null;
                            if (selectedItemIndex != -1)
                                currentReport = RepoManager.Tab_ReportRepo.SingleOrDefault(trr => trr.Report_Id == selectedItemIndex);
                            if (currentReport != null)
                            {
                                currentReport.Data_Layout_Report = DateTime.UtcNow;
                                RepoManager.Tab_ReportRepo.Update(currentReport, true);
                                LoadPrintLayout(currentReport);
                            }
                            BindPrintLayoutCombo();
                        }
                    }
                }

                #endregion

                #region Save client layout

                String currentPrintLayoutName = cmbPrintLayout.Text;
                bool withoutSpaces = false;
                int separatorIndex = currentPrintLayoutName.LastIndexOf(" - ");
                if (separatorIndex == -1)
                {
                    separatorIndex = currentPrintLayoutName.LastIndexOf('-');
                    withoutSpaces = true;
                }

                if (separatorIndex != -1)
                    currentPrintLayoutName = withoutSpaces
                        ? currentPrintLayoutName.Substring(separatorIndex + 1, currentPrintLayoutName.Length - separatorIndex - 1)
                        : currentPrintLayoutName.Substring(separatorIndex + 3, currentPrintLayoutName.Length - separatorIndex - 3);

                if (e.Parameter == "save")
                {
                    if (cmbPrintLayout.Text.Trim() != string.Empty)
                    {
                        String currentTabLayout = String.Empty;

                        if (!cmbPrintLayout.Text.ToUpper().EndsWith("DEFAULT"))
                        //Se NON si tratta di un Layout di Default allora è salvabile da chiunque
                        {
                            if (PrintCustomModule == null)
                            {
                                StringBuilder sb = new StringBuilder();
                                foreach (ListEditItem selectedItem in lbPrintOptions.SelectedItems)
                                {
                                    if (!selectedItem.Value.ToString().StartsWith("OPZ"))
                                    {
                                        int tabPageIndex = Convert.ToInt32(selectedItem.Value);
                                        TabPageExtended currentTPE = GridModule.EditFormTemplate.Dic.Keys.SingleOrDefault(i => i.Id == tabPageIndex);
                                        if (currentTPE != null)
                                            sb.Append(currentTPE.Name.ToString()).Append("|");
                                    }
                                    else
                                    {
                                        sb.Append(selectedItem.Value.ToString()).Append("|");
                                    }
                                }
                                currentTabLayout = sb.ToString();
                                int lastIndexSeparator = currentTabLayout.LastIndexOf("|");
                                if (lastIndexSeparator != -1)
                                    currentTabLayout = currentTabLayout.Substring(0, lastIndexSeparator);
                            }

                            Tab_Report currentLayout = PowerWebContext.GetFromSession<Tab_Report>("PrintLayoutReport_" + GridView.ID);

                            if (PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level)
                            //Nel caso in cui l'Utente che sta salvando il Layout di Stampa abbia un Livello >= al LIvello di Admin definito in Tab Param
                            //il Layout di Stampa viene salvato SENZA UTENTE x essere disponibile per TUTTI GLI UTENTI
                            {
                                Tab_Report savedLayout = RepoManager.Tab_ReportRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == GridView.ID
                                   && tdg.Nome_Report == currentPrintLayoutName && tdg.Utenti_Id == null);
                                //Viene salvato SENZA UTENTE (null)
                                if (savedLayout == null)
                                    SavePrintLayout(savedLayout, null, currentLayout.Nome_Risorsa, currentPrintLayoutName, currentTabLayout);
                                else
                                    SavePrintLayout(savedLayout, null, currentLayout.Nome_Risorsa, currentPrintLayoutName, currentTabLayout);
                            }
                            else
                            //Nel caso in cui l'Utente che sta salvando il Layout di Stampa abbia un Livello < al LIvello di Admin definito in Tab Param
                            //il Layout di Stampa viene salvato CON UTENTE x essere disponibile SOLO x QUELL'UTENTE
                            {
                                Tab_Report savedLayout = RepoManager.Tab_ReportRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == GridView.ID
                                    && tdg.Nome_Report == currentPrintLayoutName && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);

                                if (savedLayout == null)
                                    //Viene Salvato con Utente_Id
                                    SavePrintLayout(savedLayout, PowerWebContext.Current.User, currentLayout.Nome_Risorsa, currentPrintLayoutName, currentTabLayout);
                                else
                                    SavePrintLayout(savedLayout, PowerWebContext.Current.User, currentLayout.Nome_Risorsa, currentPrintLayoutName, currentTabLayout);
                            }
                        }
                        else
                        // IL Layout di DEFAULT NON e' MODIFICABILE Da NESSUNO
                        {
                            currentPanel.JSProperties["cpErrorTitle"] = BusinessService.GetLocalizedString(PowerWebResources.STR_TITOLO_POPUP_ERRORE);
                            currentPanel.JSProperties["cpErrorMessage"] = BusinessService.GetLocalizedString(PowerWebResources.ERR_NO_SALVA_STRUTTURA_DEFAULT);

                            if (LogModule != null)
                            {
                                LogModule.Log.Error("Utente NON ABILITATO a MODIFICARE il Layout di DEFAULT");
                            }
                        }
                    }

                    BindPrintLayoutCombo(true);
                }
                #endregion

                #region Delete client layout

                if (e.Parameter == "delete")
                {
                    if (!cmbPrintLayout.Text.ToUpper().EndsWith("DEFAULT"))
                    {
                        if (PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level)
                        //Se l'utente ha un Livello >= al Livello di Admin definito in Tab Param allora il Layout veine cercato SENZA UTENTE
                        {
                            Tab_Report toDeleteReport = RepoManager.Tab_ReportRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == GridView.ID
                               && tdg.Nome_Report == currentPrintLayoutName && tdg.Utenti_Id == null);

                            RepoManager.Tab_ReportRepo.Delete(toDeleteReport, true);
                        }
                        else
                        //Se l'Utente ha un livello < del Livello di Admin definito in Tab Param allora il Layout viene cercato CON UTENTE
                        {
                            Tab_Report userLayout = RepoManager.Tab_ReportRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == GridView.ID
                                                               && tdg.Nome_Report == currentPrintLayoutName && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
                            if (userLayout != null)
                            {
                                RepoManager.Tab_ReportRepo.Delete(userLayout, true);

                                if (DoubleGridModule != null)
                                {
                                    Tab_Report toDeleteReport2 = RepoManager.Tab_ReportRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == DoubleGridModule.GridView2.ID
                                   && tdg.Nome_Report == currentPrintLayoutName && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
                                    RepoManager.Tab_ReportRepo.Delete(toDeleteReport2, false);
                                }

                                if (TripleGridModule != null)
                                {
                                    Tab_Report toDeleteReport3 = RepoManager.Tab_ReportRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == TripleGridModule.GridView3.ID
                                   && tdg.Nome_Report == currentPrintLayoutName && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
                                    RepoManager.Tab_ReportRepo.Delete(toDeleteReport3, false);
                                }

                                if (QuadGridModule != null)
                                {
                                    Tab_Report toDeleteReport4 = RepoManager.Tab_ReportRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == QuadGridModule.GridView4.ID
                                        && tdg.Nome_Report == currentPrintLayoutName && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
                                    RepoManager.Tab_ReportRepo.Delete(toDeleteReport4, false);
                                }
                            }
                            else
                            //Se il LaYout NON è SUO allora NON Viene Cancellato
                            {
                                currentPanel.JSProperties["cpErrorTitle"] = BusinessService.GetLocalizedString(PowerWebResources.STR_TITOLO_POPUP_ERRORE);
                                currentPanel.JSProperties["cpErrorMessage"] = BusinessService.GetLocalizedString(PowerWebResources.ERR_STRUTTURA_NON_DELL_UTENTE);
                                if (LogModule != null)
                                {
                                    LogModule.Log.Error("Il Layout NON appartiene all'Utente che NON è quindi abilitato a Cancellarlo");
                                }
                            }
                        }
                    }
                    else
                    // IL Layout di DEFAULT NON e' CANCELLABILE Da NESSUNO
                    {
                        currentPanel.JSProperties["cpErrorTitle"] = BusinessService.GetLocalizedString(PowerWebResources.STR_TITOLO_POPUP_ERRORE);
                        currentPanel.JSProperties["cpErrorMessage"] = BusinessService.GetLocalizedString(PowerWebResources.ERR_NO_SALVA_STRUTTURA_DEFAULT);

                        if (LogModule != null)
                        {
                            LogModule.Log.Error("Utente NON ABILITATO a CANCELLARE il Layout di DEFAULT");
                        }
                    }

                    BindPrintLayoutCombo(true);
                    //BindCheckBoxList(); // ricalcolo dell'elenco di tab e opzioni report
                }

                #endregion
            }
        }

        protected void cpExportLayout_Callback(object sender, CallbackEventArgsBase e)
        //Gestice il Caricamento/Salvataggio/Cancellazione delel Viste di Layout della pagina di gestione degli Export sia Custom sia STandrd 
        {
            ASPxCallbackPanel currentPanel = sender as ASPxCallbackPanel;

            if (GridPage != null && currentPanel != null)
            {
                #region Load client layout

                int selectedItemIndex = int.MinValue;

                if (Int32.TryParse(e.Parameter, out selectedItemIndex))
                {
                    if (selectedItemIndex != int.MinValue)
                    {
                        if (selectedItemIndex == -2)
                            BindExportLayoutCombo(true);
                        else
                            BindExportLayoutCombo();
                    }
                }

                #endregion
            }
        }

        private void LoadPrintLayout(Tab_Report currentReport)
        {
            //vado a costruirmi le checkbox
            BindCheckBoxList();

            //metto in sezione  la tabella dei report
            PowerWebContext.SetToSession<Tab_Report>("PrintLayoutReport_" + GridView.ID, currentReport);
            //contrllo se nella  colonna string è presente qualcosa
            if (!String.IsNullOrEmpty(currentReport.Tab_String))
            {
                //mi estraggo il tab presente nella colonna string(ad esempio "Generali")
                String[] tabSelection = currentReport.Tab_String.Split(new Char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (ListEditItem item in lbPrintOptions.Items)
                {
                    // operazione da effettuare solo se sono tab, e ciè se l'inizio del valore non comincia con OPZ
                    if (!item.Value.ToString().StartsWith("OPZ"))
                    {
                        int tabPageIndex = Convert.ToInt32(item.Value);
                        //carico i valori di raggruppamento dall'edit from template della clasee su cui vado a fare il report e dal tab specificato nella colonna string
                        TabPageExtended currentTPE = GridModule.EditFormTemplate.Dic.Keys.SingleOrDefault(i => i.Id == tabPageIndex);
                        if (currentTPE != null)
                        {
                            item.Selected = tabSelection.Contains(currentTPE.Name);
                        }
                    }
                    else
                    {
                        item.Selected = tabSelection.Contains(item.Value);
                    }
                }

                //Se sono nel cartellino, faccio riferimento alla GridView del PrintModule (quindi il collaboratore)
                if (PrintModule.PrintFormTemplate != null && PrintModule.PrintFormTemplate.BaseModule.ID == "mdlTimesheetModule")
                {
                    ASPxGridView printGridView = GetPrintGrid();
                    PowerWebContext.SetToSession<List<GroupingTreeListItem>>("GroupingTreeData_" + printGridView.ID, null);
                }

                //In ogni altro caso, faccio riferimento alla GridView del GridModule
                else
                {
                    PowerWebContext.SetToSession<List<OrderingTreeListItem>>("GroupingTreeData_" + GridView.ID, null);
                }


                BindGroupingTree();

                GroupingTreeListItem rootGroupingItem = tlGrouping.Nodes[0].DataItem as GroupingTreeListItem;

                foreach (Tab_Report_Group group in currentReport.Tab_Report_Group.OrderBy(trg => trg.Position))
                {
                    foreach (TreeListNode node in tlGrouping.Nodes)
                    {
                        GroupingTreeListItem nodePrintItem = node.DataItem as GroupingTreeListItem;
                        if (nodePrintItem != null && nodePrintItem.Field == group.Field)
                        {
                            nodePrintItem.ParentId = rootGroupingItem.Id;
                            nodePrintItem.IsInGrouping = true;
                            nodePrintItem.IsSkipPage = group.IsSkipPage;
                            nodePrintItem.RepeatEveryPage = group.RepeatEveryPage;
                            rootGroupingItem = nodePrintItem;
                            break;
                        }
                    }
                }

                BindGroupingTree();

            }
        }

        private void SavePrintLayout(Tab_Report printLayout, Utenti user, String resourceName, String printLayoutName, String tabLayout)
        {
            if (printLayout != null)
                RepoManager.Tab_ReportRepo.Delete(printLayout, true);

            Tab_Report newReport = new Tab_Report
            {
                Utenti_Id = user == null ? null : (int?)user.Utenti_Id,
                Nome_DataGrid = GridView.ID,
                Nome_Risorsa = resourceName,
                Nome_Report = printLayoutName,
                Tab_String = tabLayout,
                Classe_Report = GridModule.EntityType.Name,
                Data_Layout_Report = DateTime.UtcNow,
            };

            List<Tab_Report_Group> currentTabGrouping = new List<Tab_Report_Group>();
            PopulateGroupList(tlGrouping.Nodes[0], currentTabGrouping, newReport);

            RepoManager.Tab_ReportRepo.Add(newReport);

            RepoManager.Tab_Report_GroupRepo.Add(currentTabGrouping);

            RepoManager.Tab_ReportRepo.SaveChanges();
        }


        private void PopulateGroupList(TreeListNode node, List<Tab_Report_Group> tabGroups, Tab_Report report)
        {
            if (node != null)
            {
                GroupingTreeListItem nodePrintItem = node.DataItem as GroupingTreeListItem;
                if (nodePrintItem != null)
                {
                    // recupero i combobox delle colonne interessate alla modifica
                    var isSkipPageColumn = tlGrouping.Columns[CommonService.GetPropertyName(() => nodePrintItem.IsSkipPage)] as TreeListCheckColumn;
                    var repeatEveryPageColumn = tlGrouping.Columns[CommonService.GetPropertyName(() => nodePrintItem.RepeatEveryPage)] as TreeListCheckColumn;

                    var isSinglePageChkbox = (ASPxCheckBox)tlGrouping.FindDataCellTemplateControl(node.Key, isSkipPageColumn, "cbIsSkipPage");
                    var repeatEveryPageChkbox = (ASPxCheckBox)tlGrouping.FindDataCellTemplateControl(node.Key, repeatEveryPageColumn, "cbRepeatEveryPage");
                    if (nodePrintItem.Field != "Root")
                        tabGroups.Add(new Tab_Report_Group
                        {
                            Tab_Report = report,
                            Position = node.Level,
                            Field = nodePrintItem.Field,
                            IsSkipPage = isSinglePageChkbox.Checked,
                            RepeatEveryPage = repeatEveryPageChkbox.Checked
                        });

                    if (node.HasChildren)
                        PopulateGroupList(node.ChildNodes[0], tabGroups, report);
                }
            }
        }

        protected void pcPrintOptions_Load(object sender, EventArgs e)
        {
            pcPrintOptions.HeaderText = BusinessService.GetLocalizedString(PowerWebResources.STR_OPZIONI_STAMPA);
            if (pcPrint.TabPages.FindByText("Tab selection") != null)
            {
                pcPrint.TabPages.FindByText("Tab selection").Text = BusinessService.GetLocalizedString(PowerWebResources.STR_SEL_DETTAGLI_STAMPA);
            }

            if (pcPrint.TabPages.FindByText("Grouping") != null)
                pcPrint.TabPages.FindByText("Grouping").Text = BusinessService.GetLocalizedString(PowerWebResources.STR_CRITERIO_RAGGRUPPAMENTO);


            if (pcPrintOptions.FindControl("btnPrintReport") != null)
            {
                ASPxButton printButton = (ASPxButton)pcPrintOptions.FindControl("btnPrintReport");
                printButton.Text = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNSTAMPA);
            }

            tlGrouping.Columns[1].Caption = BusinessService.GetLocalizedString(PowerWebResources.FLD_NOMECAMPO);
            tlGrouping.Columns[2].Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_SALTO_PAGINA);
            tlGrouping.Columns[3].Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_RIPETI_OGNI_PAGINA);
        }

        protected void pcExportXLSXOptions_Load(object sender, EventArgs e)
        {
            pcExportXLSXOptions.HeaderText = BusinessService.GetLocalizedString(PowerWebResources.STR_OPZIONI_EXPORT);
            if (pcExportXLSX.TabPages.FindByText("Tab selection") != null)
            {
                pcExportXLSX.TabPages.FindByText("Tab selection").Text = BusinessService.GetLocalizedString(PowerWebResources.STR_SEL_DETTAGLI_STAMPA);

            }
            if (pcExportXLSXOptions.FindControl("btnExportXLSXLaunch") != null)
            {
                ASPxButton exportButton = (ASPxButton)pcExportXLSXOptions.FindControl("btnExportXLSXLaunch");
                exportButton.Text = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNLAUNCHEXPORT);
            }
        }

        #endregion

        protected void cpCommand_Callback(object sender, CallbackEventArgsBase e)
        //Gestisce i CustomButton di CLONE e VISUALIZZA
        {
            string eParameter = e.Parameter;
            var splitter = e.Parameter.Split(new char[] { '|' });

            var index = -1;
            if (splitter.Count() > 1)
                index = Convert.ToInt32(splitter[1]);

            var grid = GridView;

            if (splitter.Count() > 2 && !String.IsNullOrEmpty(splitter[2]))
                grid = PowerWebContext.GetFromSession<ASPxGridView>(splitter[2]);

            var gridIntanceId = grid.ClientID;

            if (splitter[0] == "addClone")
            {
                GridClonedValues = new Hashtable();

                var templateDic = EditDictionaryManager.GetEditDictionaryRegV();

                foreach (GridViewDataColumn column in grid.Columns.OfType<GridViewDataColumn>())
                {
                    if (grid.KeyFieldName != column.FieldName)
                    {
                        if (GridModule.EntityType == typeof(Reg_V))
                        //Per le REGV_M quando si caricano i Campi dell'EDIT per il acso di Clone
                        //Vengono Dis_Abilitati i Campi che l'Utente NON PUO'/DEVE INSERIRE
                        {
                            // viene controllato che il nome della colonna che si sta processando; se si tratta di uno di questi il
                            // campo non viene processato per la clone.
                            if (column.FieldName == CommonService.GetPropertyName(() => _regvStub.Registrazione_Stato_Reg) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Desc) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.RiferimentoRRN_Att) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.Durata_Fig) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.IsNotToElaborate) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.Tipo_Modifica) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.Durata_Fig_HH_S) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fig_E) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.Fru_Id) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.Pru_Id) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fig_U) ||
                                column.FieldName == CommonService.GetPropertyName(() => _regvStub.Registrazione_Bloccata))
                            {
                                var toDeleteTabs = new List<TabPageExtended>();

                                foreach (var keyValuePair in templateDic)
                                {
                                    keyValuePair.Value.RemoveAll(tpi => tpi.Field == column.FieldName);

                                    var emptyFields = keyValuePair.Value.Where(tpi => tpi.Field == null).Count();

                                    if (keyValuePair.Value.Count == 0 || keyValuePair.Value.Count == emptyFields)
                                        toDeleteTabs.Add(keyValuePair.Key);
                                }

                                foreach (var toDeleteTab in toDeleteTabs)
                                    templateDic.Remove(toDeleteTab);

                                continue;
                            }
                        }



                        // verifico se è attiva la pesonalizzazione riguardante l'Import Custom per Mosaico
                        CustomizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CloneTypeEnum);
                        //se la personalizzazione non è attiva
                        if (CustomizationVersion != 0)
                        {
                            //Nel caso del TypoForRegv non si clonano per le RegV i seguenti campi
                            if ((CustomizationVersion == (int)CloneTypeEnum.FiledToClone) &&


                                  //campi da non clonare
                                  (column.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_U) ||
                                   column.FieldName == CommonService.GetPropertyName(() => _regvStub.Durata_Fis_HH_S) ||
                                   column.FieldName == CommonService.GetPropertyName(() => _regvStub.Cant_Id) ||
                                   column.FieldName == CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fig_U)))
                            {
                                //ridClonedValues[column.FieldName] = grid.GetRowValues(index, column.FieldName);
                            }

                            else
                                //clono tutti i campi
                                GridClonedValues[column.FieldName] = grid.GetRowValues(index, column.FieldName);
                        }

                        else
                            //clono tutti i campi
                            GridClonedValues[column.FieldName] = grid.GetRowValues(index, column.FieldName);
                    }
                }

                if (GridModule.EntityType == typeof(Reg_V))
                    GridView.Templates.EditForm = new PowerFormTemplate(BaseGridModule, templateDic);

                grid.AddNewRow();
            }

            if (splitter[0] == "view")
            {
                PowerWebContext.SetToSession("IsReadOnly_" + gridIntanceId, true);
                grid.StartEdit(index);
            }
        }

        #region JSProperties

        protected void btnDeleteLayout_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage", BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_1_UTENTE_VUOLE_ESEGUIRE_ELIMINAZIONE));
        }

        protected void btnDeletePrintLayout_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage", BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_1_UTENTE_VUOLE_ESEGUIRE_ELIMINAZIONE));
        }
        #endregion

    }
}