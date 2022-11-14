using Business;
using Business.BusinessExtension;
using Business.ExportExcelEngine;
using Business.Repository;
using Common;
using DevExpress.Web.ASPxCallback;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxEditors;
using DevExpress.Web.ASPxFormLayout;
using DevExpress.Web.ASPxGridView;
using Domain;
using Domain.Extensions;
using Exports.ExportExcelCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PowerWeb.Modules
{
    public partial class ExportReg_VModule : BaseGridModule, IDoubleGridModule, IQuadGridModule
    {

        #region Private Constants

        /// <summary>
        /// Il namespace (dll) in cui trovare i tipi degli export custom
        /// </summary>
        private const string CustomExportNamespace = "Exports";

        /// <summary>
        /// Il tipo di entità esporata dal modulo
        /// </summary>
        private const string CurrentModuleEntityExport = "Reg_V";

        /// <summary>
        /// Tipo di export specializzato da non gestire (inizio stringa)
        /// </summary>
        private const string CurrentModuleExcludedExportType = "ExportExcelGeneric";

        /// <summary>
        /// Il campo chiave della griglia dei collaboratori
        /// </summary>
        private const string ColKeyFieldName = "Col_Id";

        /// <summary>
        /// Il campo chiave della griglia dei cantieri
        /// </summary>
        const string CantKeyFieldName = "Cant_Id";
        /// <summary>
        /// Il campo chiave della griglia dei clienti
        /// </summary>
        const string CliKeyFieldName = "Cli_Id";

        #endregion

        #region Protected Properties

        /// <summary>
        /// Recupera o imposta l'elenco degli id collaboratore selezionati nella griglia
        /// </summary>
        protected List<int> SelectedColsId
        {
            get
            {
                var colsel = PowerWebContext.GetFromSession<List<int>>("SelectedColsId" + gvColExport.ID);
                if (colsel == null)
                {
                    colsel = new List<int>();
                    PowerWebContext.SetToSession("SelectedColsId" + gvColExport.ID, colsel);
                }
                return colsel;

            }

            set
            {
                List<int> list = value;
                if (list != null)
                    PowerWebContext.SetToSession("SelectedColsId" + gvColExport.ID, list);
            }
        }

        /// <summary>
        /// Recupera o imposta l'elenco degli id cantiere selezionati nella griglia
        /// </summary>
        protected List<int> SelectedCantsId
        {
            get
            {
                var cantsel = PowerWebContext.GetFromSession<List<int>>("SelectedCantsId" + gvCantExport.ID);
                if (cantsel == null)
                {
                    cantsel = new List<int>();
                    PowerWebContext.SetToSession("SelectedCantsId" + gvCantExport.ID, cantsel);
                }
                return cantsel;

            }

            set
            {
                List<int> list = value;
                if (list != null)
                    PowerWebContext.SetToSession("SelectedCantsId" + gvCantExport.ID, list);
            }
        }

        /// <summary>
        /// Recupera o imposta l'elenco degli id cliente selezionati nella griglia
        /// </summary>
        protected List<int> SelectedClisId
        {
            get
            {
                var clisel = PowerWebContext.GetFromSession<List<int>>("SelectedClisId" + gvCliExport.ID);
                if (clisel == null)
                {
                    clisel = new List<int>();
                    PowerWebContext.SetToSession("SelectedCantsId" + gvCliExport.ID, clisel);
                }
                return clisel;
            }

            set
            {
                List<int> list = value;
                if (list != null)
                    PowerWebContext.SetToSession("SelectedClisId" + gvCliExport.ID, list);
            }
        }

        /// <summary>
        /// Recupera o imposta l'elenco di collaboratori da visualizzare all'interno della griglia di selezione
        /// </summary>
        protected List<Col> Cols
        {
            get
            {
                List<Col> cols = PowerWebContext.GetFromSession<List<Col>>("cols_" + gvColExport.ID);
                if (cols == null)
                {
                    cols = RepoManager.ColRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Col>>("cols_" + gvColExport.ID, cols);
                }
                return cols;
            }

            set
            {
                PowerWebContext.SetToSession<List<Col>>("cols_" + gvColExport.ID, value);
            }
        }

        /// <summary>
        /// Recupera o imposta l'elenco dei cantieri da visualizzare all'interno della griglia di selezione
        /// </summary>
        protected List<Cant> Cants
        {
            get
            {
                List<Cant> cants = PowerWebContext.GetFromSession<List<Cant>>("cants_" + gvCantExport.ID);
                if (cants == null)
                {
                    cants = RepoManager.CantRepo.Find(cant => cant.Tipologia_Can != "ATT", true).ToList(); // TODO: non filtrare ma correggere salvataggio vista
                    PowerWebContext.SetToSession<List<Cant>>("cants_" + gvCantExport.ID, cants);
                }
                return cants;
            }

            set
            {
                PowerWebContext.SetToSession<List<Cant>>("cants_" + gvCantExport.ID, value);
            }
        }

        /// <summary>
        /// Recupera o imposta l'elenco dei clienti da visualizzare all'interno della griglia di selezione
        /// </summary>
        protected List<Cli> Clis
        {
            get
            {
                List<Cli> clis = PowerWebContext.GetFromSession<List<Cli>>("clis_" + gvCliExport.ID);
                if (clis == null)
                {
                    clis = RepoManager.CliRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cli>>("clis_" + gvCliExport.ID, clis);
                }

                return clis;
            }
            set
            {
                PowerWebContext.SetToSession<List<Cli>>("clis_" + gvCliExport.ID, value);
            }
        }

        /// <summary>
        /// Recupera o imposta in/da sessione l'entità di selezione primaria indicata con il combobox.
        /// </summary>
        /// <value>
        /// L'entità di selezione primaria indicata con il combobox.
        /// </value>
        protected ExcelModelSelectionTypeEnum CurrentFirstEntity
        {
            get
            {
                if (PowerWebContext.GetFromSession<ExcelModelSelectionTypeEnum?>("currentFirstEntity_" + gvCantExport.ID) == null)
                {
                    PowerWebContext.SetToSession<ExcelModelSelectionTypeEnum>("currentFirstEntity_" + gvCantExport.ID, ExcelModelSelectionTypeEnum.None);
                }
                return PowerWebContext.GetFromSession<ExcelModelSelectionTypeEnum>("currentFirstEntity_" + gvCantExport.ID);
            }

            set
            {
                PowerWebContext.SetToSession<ExcelModelSelectionTypeEnum>("currentFirstEntity_" + gvCantExport.ID, value);
            }
        }

        #endregion

        #region Public Properties

        public override ASPxGridView GridView
        {
            get { return gvColExport; }
        }

        public ASPxGridView GridView2
        {
            get { return gvCantExport; }
        }
        public ASPxGridView GridView3 => throw new NotImplementedException();

        public ASPxGridView GridView4
        {
            get { return gvCliExport; }
        }

        public PowerFormTemplate EditFormTemplate2 { get; private set; }
        public PowerFormTemplate EditFormTemplate3 { get; private set; }
        public PowerFormTemplate EditFormTemplate4 { get; private set; }

        #endregion

        #region Eventi pagina

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Page_Init(object sender, EventArgs e)
        {
            // in caso di prima apertura
            // verifico che, non ereditando dalla grid master page, ci sia l'utente loggato
            // e caso mai ritorno un'eccezione; questo controllo serve ad evitara accessi indesiderati senza login
            if (!Page.IsPostBack && !Page.IsCallback)
            {
                if (PowerWebContext.Current.User == null)
                    throw new AccessViolationException("Function not available");

                PowerWebService.FillGridLabels(typeof(Col), GridView);
                PowerWebService.FillGridLabels(typeof(Cant), GridView2);
                //PowerWebService.FillGridLabels(typeof(Cli), GridView4);

                // alla prima apertura viene effettuata l'inizializzazione della sessione
                ResetSession();

            }

            // in base al tipo di entità primaria di ricerca, effettuo il bind della relativa griglia
            BindMasterGrid();

            // bind del combobox del tipo export
            PowerWebService.FillComboboxes(cmbTipoExport, "Tipo_Export", false);

            // abbonamento agli eventi di ricerca del combobox (di modo da forzare un data source non standard)
            cmbTipoExport.ItemsRequestedByFilterCondition += cmbCol_Id_ItemsRequestedByFilterCondition;
            cmbTipoExport.ItemRequestedByValue += cmbCol_Id_ItemRequestedByValue;

            PowerWebService.FillComboboxes(gvColExport);
            PowerWebService.FillComboboxes(gvCantExport);
            // PowerWebService.FillComboboxes(gvCliExport);

            // impostazione in lingua delle etichette
            LocalizeModuleLabels();

            // inizializzazione degli elementi dei radio button (lingua e valore)
            InitializeModuleComboboxes();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Effettua il reset dei dati della sessione attuale e reinizializza i dati base della form.
        /// </summary>
        public override void ResetSession()
        {
            base.ResetSession();

            // al reset della sessione viene reinizializzata la lista degli id dei collaboratori selezionati
            SelectedColsId = new List<int>();

            // al reset della sessione viene reinizializzata la lista degli id dei cantieri selezionati
            SelectedCantsId = new List<int>();

            // al reset della sessione viene reinizializzata la lista degli id dei clienti selezionati
            //SelectedClisId = new List<int>();

            // al reset della sessione sono puliti i filtri e le selezioni sulle griglie (tutte)
            gvColExport.FilterExpression = String.Empty;
            gvCantExport.FilterExpression = String.Empty;
            //gvCliExport.FilterExpression = String.Empty;
            gvColExport.Selection.UnselectAll();
            gvCantExport.Selection.UnselectAll();
            //gvCliExport.Selection.UnselectAll();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Imposta in lingua le etichette dei vari elementi aspx e html della form.
        /// </summary>
        private void LocalizeModuleLabels()
        {
            lblTipoExport.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_TIPO_EXPORT);
            lblPeriodo.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_PERIODO);
            pcSlaveGrid.HeaderText = BusinessService.GetLocalizedString(PowerWebResources.LBL_ULTERIORI_SELEZIONI);
            BtnUltSel.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_ATTIVA_ULTERIORI_SELEZIONI);
            LblTipoCalcolo.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_TIPO_CALCOLO);
            LblTipoOre.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_TIPO_ORE);
            ASPxCheckBoxDetali.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_EXPORT_DETAIL);
            LblTolleranzaDurata.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOLLERANZA_DURATA);
            LblTolleranzaEU.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOLLERANZA_EU);

            // localizzazione dell'etichetta del form layout
            var item = flStandardInsert.Items[0] as LayoutGroup;
            item.Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_DATI_EXPORT);

            // calcolo del testo della label di selezione attiva/non attiva della griglia slave
            SetOtherSelectionStatus(CurrentFirstEntity);
        }

        /// <summary>
        /// Inizializza i combobox presenti nel modulo.
        /// </summary>
        private void InitializeModuleComboboxes()
        {
            if (!Page.IsPostBack && !Page.IsCallback) // solo in caso di prima apertura
            {

                #region Inizializzazione combobox di gestione del tipo calcolo

                RdBtnTipoCalcolo.Items.Clear(); // pulizia della lista degli elementi
                RdBtnTipoCalcolo.ValueType = typeof(ExportRegVCalculationTypeEnum);
                RdBtnTipoCalcolo.Items.Add(new ListEditItem(BusinessService.GetLocalizedString(PowerWebResources.STR_ORE_FIGURATIVE), ExportRegVCalculationTypeEnum.Rounded));
                RdBtnTipoCalcolo.Items.Add(new ListEditItem(BusinessService.GetLocalizedString(PowerWebResources.STR_ORE_FISICHE), ExportRegVCalculationTypeEnum.Physical));
                RdBtnTipoCalcolo.SelectedIndex = 0;

                #endregion

                #region Inizializzazione combobox di gestione del tipo ore

                RdBtnTipoOre.Items.Clear(); // pulizia della lista degli elementi
                RdBtnTipoOre.ValueType = typeof(ExportRegVHourTypeEnum);
                RdBtnTipoOre.Items.Add(new ListEditItem(BusinessService.GetLocalizedString(PowerWebResources.STR_SOLO_DURATA), ExportRegVHourTypeEnum.OnlyDuration));
                RdBtnTipoOre.Items.Add(new ListEditItem(BusinessService.GetLocalizedString(PowerWebResources.STR_PER_ENTRATA_USCITA), ExportRegVHourTypeEnum.Eu));
                RdBtnTipoOre.Items.Add(new ListEditItem(BusinessService.GetLocalizedString(PowerWebResources.STR_ENTRAMBI), ExportRegVHourTypeEnum.Both));
                RdBtnTipoOre.SelectedIndex = 0;



                #endregion
            }
        }

        /// <summary>
        /// Imposta il data source degli export selezionabili nel combobox.
        /// </summary>
        private void BindCmbExportType(string filter, int beginIndex, int endIndex, ASPxComboBox comboBox)
        {

            var excelModels = RepoManager.Tab_Excel_ModelRepo.Find(tem => tem.Nome_Entity == CurrentModuleEntityExport &&
                !tem.Nome_Specializzato.StartsWith(CurrentModuleExcludedExportType) && tem.IsActive && tem.Tipo_Selezione != null).AsQueryable();

            // se non è abilitato il modulo di gestione del confronto ore/budget non si visualizzano tali modelli
            if (!RepoManager.ParamRepo.ParametersRow.Abilita_Confronto_Ore_Budget)
                excelModels = excelModels.Where(tem => tem.Nome_Specializzato != Common.Properties.Settings.Default.BudgetConfrontationSpecialized);

            String searchName = comboBox.ClientInstanceName;

            var cmbDataSource = RepoManager.Tab_GridLookupRepo.SearchByFieldAndValue(PowerWebService.TabGridLookups, searchName,
                filter, beginIndex, endIndex, excelModels);

            comboBox.DataSource = cmbDataSource;

            comboBox.DataBindItems();
        }

        /// <summary>
        /// Handles the ItemsRequestedByFilterCondition event of the cmbCol_Id control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="ListEditItemsRequestedByFilterConditionEventArgs"/> instance containing the event data.</param>
        private void cmbCol_Id_ItemsRequestedByFilterCondition(object source, ListEditItemsRequestedByFilterConditionEventArgs e)
        {
            ASPxComboBox comboBox = (ASPxComboBox)source;

            BindCmbExportType(e.Filter, e.BeginIndex, e.EndIndex, comboBox);
        }

        /// <summary>
        /// Handles the ItemRequestedByValue event of the cmbCol_Id control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="ListEditItemRequestedByValueEventArgs"/> instance containing the event data.</param>
        private void cmbCol_Id_ItemRequestedByValue(object source, ListEditItemRequestedByValueEventArgs e)
        {
            if (e.Value == null || String.IsNullOrEmpty(e.Value.ToString()))
                return;

            ASPxComboBox comboBox = (ASPxComboBox)source;

            String searchName = comboBox.ClientInstanceName;

            var excelModels = RepoManager.Tab_Excel_ModelRepo.Find(tem => tem.Nome_Entity == CurrentModuleEntityExport &&
                !tem.Nome_Specializzato.StartsWith(CurrentModuleExcludedExportType) && tem.IsActive && tem.Tipo_Selezione != null).AsQueryable();
            var cmbDataSource = RepoManager.Tab_GridLookupRepo.SearchByFieldAndValue(PowerWebService.TabGridLookups, searchName, e.Value.ToString(), dataSource: excelModels);

            comboBox.DataSource = cmbDataSource;

            comboBox.DataBindItems();
        }

        /// <summary>
        /// Effettua il data bind della griglia dei collaboratori
        /// </summary>
        /// <param name="isToRefresh"><c>true</c> se deve essere effetuato anche il refresh oltre che l'impostazione del data source.</param>
        /// <param name="emptyDataSource"><c>true</c> se deve essere impostato forzatamente un datasource vuoto per la griglia; altrimenti <c>false</c></param>
        private void BindColGrid(bool isToRefresh = false, bool emptyDataSource = false)
        {
            gvColExport.KeyFieldName = ColKeyFieldName;
            gvColExport.DataSource = emptyDataSource ? new List<Col>() : Cols;
            if ((!Page.IsPostBack && !Page.IsCallback) || isToRefresh)
                gvColExport.DataBind();
        }

        /// <summary>
        /// Effettua il data bind della griglia dei cantieri
        /// </summary>
        /// <param name="isToRefresh"><c>true</c> se deve essere effetuato anche il refresh oltre che l'impostazione del data source.</param>
        /// /// <param name="emptyDataSource"><c>true</c> se deve essere impostato forzatamente un datasource vuoto per la griglia; altrimenti <c>false</c></param>
        private void BindCantGrid(bool isToRefresh = false, bool emptyDataSource = false)
        {
            gvCantExport.KeyFieldName = CantKeyFieldName;
            gvCantExport.DataSource = emptyDataSource ? new List<Cant>() : Cants;
            if ((!Page.IsPostBack && !Page.IsCallback) || isToRefresh)
                gvCantExport.DataBind();
        }

        /// <summary>
        /// Effettua il data bind della griglia clienti
        /// </summary>
        /// <param name="isToRefresh"><c>true</c> se deve essere effettuato anche il refresh oltre che l'impostazione del data source.</param>
        /// <param name="emptyDataSource"><c>true</c> se deve essere impostato forzatamente un datasource vuoto per la griglia; altrimenti <c>false</c></param>
        private void BindCliGrid(bool isToRefresh = false, bool emptyDataSource = false)
        {
            gvCliExport.KeyFieldName = CliKeyFieldName;
            gvCliExport.DataSource = emptyDataSource ? new List<Cli>() : Clis;
            if ((!Page.IsPostBack && !Page.IsCallback) || isToRefresh)
                gvCliExport.DataBind();
        }

        /// <summary>
        /// Effettua il bind della griglia master (quella dell'entità primaria da visualizzare per la selezione).
        /// </summary>
        /// <param name="refreshPrimary"Se impostato a true rieffettua completamente il bind dei dati sulla griglia principale.</param>
        private void BindMasterGrid(bool refreshPrimary = false)
        {
            // nella master grid popolo sempre la lista di collaboratori o cantieri; nella slave sempre un datasource vuoto
            switch (CurrentFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Cant:
                    BindCantGrid(refreshPrimary);
                    BindColGrid(emptyDataSource: true);
                    //BindCliGrid(emptyDataSource: true);
                    break;
                case ExcelModelSelectionTypeEnum.Col:
                    BindColGrid(refreshPrimary);
                    BindCantGrid(emptyDataSource: true);
                    //BindCliGrid(emptyDataSource: true);
                    break;
                    //case ExcelModelSelectionTypeEnum.Cli:
                    //    BindCliGrid(refreshPrimary);
                    //    BindCantGrid(emptyDataSource: true);
                    //    BindColGrid(emptyDataSource: true);
                    //    break;
            }
        }

        /// <summary>
        /// Visualiza o nasconde (a seconda del parametro hideElements) gli elementi non utilizzati da una griglia vuota).
        /// </summary>
        /// <param name="gridToProcess">La griglia da processare.</param>
        /// <param name="hideElements"><c>true</c> se si desidera nascondere gli elementi superflui; altrimenti <c>false</c>.</param>
        private void ShowOrHideUnusedGridElements(ASPxGridView gridToProcess, bool hideElements)
        {
            gridToProcess.Styles.EmptyDataRow.CssClass = hideElements ? "noDataSource" : String.Empty;
            gridToProcess.Styles.GroupPanel.CssClass = hideElements ? "noDataSource" : String.Empty;
            gridToProcess.Styles.PagerBottomPanel.CssClass = hideElements ? "noDataSource" : String.Empty;

        }

        /// <summary>
        /// Gestisce l'impostazione delle proprietà delle righe selezionate/totali sulla griglia mittente.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ASPxGridViewClientJSPropertiesEventArgs"/> instance containing the event data.</param>
        private void SetGridRowsState(object sender, ASPxGridViewClientJSPropertiesEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;

            Int32 start = grid.VisibleStartIndex;
            Int32 end = grid.VisibleStartIndex + grid.SettingsPager.PageSize;
            Int32 selectNumbers = 0;

            end = (end > grid.VisibleRowCount ? grid.VisibleRowCount : end);
            for (int i = start; i < end; i++)
            {
                if (grid.Selection.IsRowSelected(i))
                    selectNumbers++;
            }

            e.Properties["cpSelectedRowsOnPage"] = selectNumbers;
            e.Properties["cpVisibleRowCount"] = grid.VisibleRowCount;
        }

        /// <summary>
        /// Imposta il messaggio di conferma selezione di tutta la griglia.
        /// </summary>
        /// <param name="e">The <see cref="CustomJSPropertiesEventArgs"/> instance containing the event data.</param>
        private void SetSelectAllConfirmationMessage(CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_SELEZIONE));
        }

        /// <summary>
        /// Inizializza per la griglia passata come parametro il checkbox di selezione della pagina corrente.
        /// </summary>
        /// <param name="sender">La griglia di provenienza della richiest.</param>
        private void InitPageSelectorCheckbox(object sender)
        {
            ASPxCheckBox chk = sender as ASPxCheckBox;
            ASPxGridView grid = (chk.NamingContainer as GridViewHeaderTemplateContainer).Grid;

            Boolean cbChecked = true;
            Int32 start = grid.VisibleStartIndex;
            Int32 end = grid.VisibleStartIndex + grid.SettingsPager.PageSize;

            end = (end > grid.VisibleRowCount ? grid.VisibleRowCount : end);
            for (int i = start; i < end; i++)
                if (!grid.Selection.IsRowSelected(i))
                {
                    cbChecked = false;
                    break;
                }

            chk.Checked = cbChecked;
        }

        /// <summary>
        /// Inizializza per la griglia passata come parametro il checkbox di selezione di tutti i record.
        /// </summary>
        /// <param name="sender">La griglia di provenienza della richiest.</param>
        private void InitSelectAllCheckbox(object sender)
        {
            ASPxCheckBox chk = sender as ASPxCheckBox;

            ASPxGridView grid = (chk.NamingContainer as GridViewHeaderTemplateContainer).Grid;

            chk.Checked = (grid.Selection.Count == grid.VisibleRowCount);
        }

        /// <summary>
        /// Gestisce il cambio di indice pagina per la griglia specificata.
        /// </summary>
        /// <param name="sender">La griglia di provenienza della richiest.</param>
        private void ManageGridIndexPageChanged(object sender)
        {
            (sender as ASPxGridView).JSProperties["cpPageChanged"] = 1;
        }

        /// <summary>
        /// Recupera dallo specifico modello excel il tipo di selezione indicato.
        /// </summary>
        /// <param name="excelModel">Il modello excel da cui recuperare l'informazione.</param>
        /// <returns>Il tipo di selezione configurato sul modello excel.</returns>
        private ExcelModelSelectionTypeEnum GetExcelSelectionType(Tab_Excel_Model excelModel)
        {
            // calcolo del tipo di selezione
            string selectionType = excelModel != null ? excelModel.Tipo_Selezione : null;

            // inizializzazione del valore di ritorno
            ExcelModelSelectionTypeEnum returnValue;

            // in base al dato impostato calcolo il valore di ritorno
            if (String.IsNullOrEmpty(selectionType))
                returnValue = ExcelModelSelectionTypeEnum.None;
            else
                switch (selectionType)
                {
                    case "Col":
                        returnValue = ExcelModelSelectionTypeEnum.Col;
                        break;
                    case "Can":
                        returnValue = ExcelModelSelectionTypeEnum.Cant;
                        break;
                    // case "Cli":
                    //     returnValue = ExcelModelSelectionTypeEnum.Cli;
                    //     break;
                    default:
                        returnValue = ExcelModelSelectionTypeEnum.None;
                        break;
                }

            // ritorno del valore calcolato dal metodo
            return returnValue;
        }

        /// <summary>
        /// Recupera il nome dello specializzato a partire dallo specifico modello excel.
        /// </summary>
        /// <param name="excelModel">Il modello excel da processare.</param>
        /// <returns>Il nome dello specializzato del modello excel</returns>
        private string GetExcelSelectionSpecializedName(Tab_Excel_Model excelModel)
        {
            return excelModel == null ? String.Empty : excelModel.Nome_Specializzato;
        }

        /// <summary>
        /// Recupera il nome del modello a partire da uno specifico modello excel.
        /// </summary>
        /// <param name="excelModel">Il modello excel da processare.</param>
        /// <returns>Il nome del modello a partire dal modello excel</returns>
        private string GetExcelSelectionModelName(Tab_Excel_Model excelModel)
        {
            return excelModel == null ? String.Empty : excelModel.ModelFilePath;
        }

        /// <summary>
        /// Recupera da database lo specifico excel model passato come parametro.
        /// </summary>
        /// <param name="excelModelId">L'identificativo univoco del modello excel da recuperare.</param>
        /// <returns>Il modello excel dell'id specificato; null se non trovato</returns>
        private Tab_Excel_Model GetTabExcelModel(int excelModelId)
        {
            return RepoManager.Tab_Excel_ModelRepo.FirstOrDefault(tem => tem.ExcelModel_Id == excelModelId);
        }

        /// <summary>
        /// Imposta le proprietà di selezione delle entità in base alla griglia master e ritorna lo stato di selezione completa di entrambe le entità.
        /// </summary>
        /// <returns>Una coppia di valori in cui il primo identifica la completa selezione dei collaboratori e l'altro la completa selezione dei cantieri</returns>
        private Tuple<bool, bool, bool> SetEntitiesSelection()
        {
            // calcolo del tipo master di selezione e in base a tale dato impostazione dei parametri di all per le griglie
            bool allCols = false;
            bool allCants = false;
            bool allClis = false;
            switch (CurrentFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Col: // griglia master collaboratori
                    // i collaboratori quando sono master sono tutti se e solo se sono stati effettivamente selezionati tutti i record
                    // e cioè solo se il numero di record in griglia è uguale al numero totale di record selezionati
                    allCols = gvColExport.VisibleRowCount == SelectedColsId.Count;

                    // i cantieri quando sono slave sono tutti se e solo se non è applicato nessun filtro; in caso contrario si calcolano i cantieri selezionati
                    // recuperandoli dal data source filtrato della griglia slave;
                    allCants = String.IsNullOrEmpty(gvCantExport.FilterExpression);
                    if (!allCants)
                    {
                        BindCantGrid(true);

                        gvCantExport.Selection.SelectAll();
                        SelectedCantsId = gvCantExport.GetSelectedFieldValues("Cant_Id").Cast<int>().ToList();
                        gvCantExport.Selection.UnselectAll();
                    }
                    else
                        SelectedCantsId = new List<int>();

                    break;
                case ExcelModelSelectionTypeEnum.Cant: // griglia master cantieri
                    // i cantieri quando sono master sono tutti se e solo se sono stati effettivamente selezionati tutti i record
                    // e cioè solo se il numero di record in griglia è uguale al numero totale di record selezionati
                    allCants = RepoManager.CantRepo.Find(cant => cant.Tipologia_Can != "ATT", true).Count() == SelectedCantsId.Count;

                    // i collaboratori quando sono slave sono tutti se e solo se non è applicato nessun filtro; in caso contrario si calcolano i collaboratori selezionati
                    // recuperandoli dal data source filtrato della griglia slave;
                    allCols = String.IsNullOrEmpty(gvColExport.FilterExpression);
                    if (!allCols)
                    {
                        BindColGrid(true);

                        gvColExport.Selection.SelectAll();
                        SelectedColsId = gvColExport.GetSelectedFieldValues("Col_Id").Cast<int>().ToList();
                        gvColExport.Selection.UnselectAll();
                    }
                    else
                        SelectedColsId = new List<int>();

                    break;
                //case ExcelModelSelectionTypeEnum.Cli: //griglia master clienti
                //    if (gvCliExport.VisibleRowCount == SelectedClisId.Count)
                //        allClis = true;
                //    else
                //        SelectedClisId = gvCliExport.GetSelectedFieldValues("Cli_Id").Cast<int>().ToList();
                //    break;
                default: // griglia master di tipo non riconosciuto
                    throw new InvalidOperationException("Master grid type not known");
            }

            // ritorno della completa selezione dei valori
            return new Tuple<bool, bool, bool>(allCols, allCants, allClis);
        }

        /// <summary>
        /// In base ai criteri di selezione impostati dall'utente (periodo, collaboratori, cantieri) recupera e restituisce tutte le relative reg_v presenti
        /// nel database.
        /// </summary>
        /// <returns>Le Reg_V che nel database corrispondono ai criteri di ricerca</returns>
        private IQueryable<Reg_V> GetRegVsFromSelection()
        {
            // costruisco la query base che recupera le reg del periodo
            var selectedPeriodDate = dePeriodo.Date;

            // calcolo del tipo master di selezione e in base a tale dato impostazione dei parametri di all per le griglie
            Tuple<bool, bool, bool> allSelectedEntities = SetEntitiesSelection();
            bool allCols = allSelectedEntities.Item1;
            bool allCants = allSelectedEntities.Item2;
            bool allClis = allSelectedEntities.Item3;
            if (CurrentFirstEntity == ExcelModelSelectionTypeEnum.Cli)
            {
                return RepoManager.Reg_VRepo.GetAllQueryable(regV => regV.Data_Reg.HasValue && regV.Data_Reg.Value.Month == selectedPeriodDate.Month &&
                    regV.Data_Reg.Value.Year == selectedPeriodDate.Year && regV.Col_Id.HasValue && regV.Cant_Id.HasValue);// && SelectedClisId.Contains(regV.Cli_Id.Value));
            }
            else
            {
                return RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Data_Reg.HasValue && regv.Data_Reg.Value.Month == selectedPeriodDate.Month &&
                regv.Data_Reg.Value.Year == selectedPeriodDate.Year && regv.Col_Id.HasValue && regv.Cant_Id.HasValue &&
                (/*allCols ||*/ SelectedColsId.Contains(regv.Col_Id.Value)) &&
                (allCants || SelectedCantsId.Contains(regv.Cant_Id.Value))); //&&
                                                                             //(allClis || SelectedClisId.Contains(regv.Cli_Id.Value))); ;
            }
        }

        /// <summary>
        /// Determina se la griglia il cui id è passato come parametro è in questo momento la master oppure no.
        /// </summary>
        /// <param name="gridId">L'identificativo della griglia da verificare.</param>
        /// <returns><c>true</c> se la griglia è in questo momento master; altrimenti <c>false</c></returns>
        private bool IsMasterGrid(string gridId)
        {
            // inizializzazione del valore di ritorno del metodo
            bool isMasterGrid = false;

            // calcolo del valore di ritorno in base alla selezione attuale;
            // si compara l'id passato come parametro all'id della griglia di riferimento del tipo export
            switch (CurrentFirstEntity)
            {
                case ExcelModelSelectionTypeEnum.Cant:
                    isMasterGrid = gridId == gvCantExport.ID;
                    break;
                case ExcelModelSelectionTypeEnum.Col:
                    isMasterGrid = gridId == gvColExport.ID;
                    break;
                case ExcelModelSelectionTypeEnum.Cli:
                    isMasterGrid = gridId == gvCliExport.ID;
                    break;
                default:
                    isMasterGrid = false;
                    break;
            }

            // ritorno del valore
            return isMasterGrid;
        }

        /// <summary>
        /// Imposta la proprietà che indica il numero di elementi selezionati sulla griglia master nell'elemento gridMasterSelectionChange.
        /// </summary>
        /// <param name="countToWrite">Il numero da indicare nella proprietà.</param>
        private void SetMasterGridSelectedCount(int countToWrite)
        {
            if (!gridMasterSelectionChange.JSProperties.ContainsKey("cpMasterSelectedCount"))
                gridMasterSelectionChange.JSProperties.Add("cpMasterSelectedCount", 0);

            gridMasterSelectionChange.JSProperties["cpMasterSelectedCount"] = countToWrite;
        }

        /// <summary>
        /// Imposta sul pulsante di visualizzazione delle ulteriori selezioni l'etichetta corrispondente all'entità secondaria.
        /// </summary>
        /// <param name="currentSelection">La selezione dell'entità primaria corrente.</param>
        private void SetOtherSelectionButtonText(ExcelModelSelectionTypeEnum currentSelection)
        {
            switch (currentSelection)
            {
                case ExcelModelSelectionTypeEnum.Cant:
                    BtnUltSel.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_ATTIVA_ULTERIORI_SELEZIONI_COL);
                    break;
                case ExcelModelSelectionTypeEnum.Col:
                    BtnUltSel.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_ATTIVA_ULTERIORI_SELEZIONI_CANT);
                    break;
                case ExcelModelSelectionTypeEnum.Cli:
                    BtnUltSel.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_ATTIVA_ULTERIORI_SELEZIONI_CLI);
                    break;
                default:
                    BtnUltSel.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_ATTIVA_ULTERIORI_SELEZIONI);
                    break;
            }
        }

        /// <summary>
        /// Imposta l'etichetta di stato delle ulteriori selezioni della griglia slave nell'apposita etichetta.
        /// </summary>
        /// <param name="currentSelection">La selezione dell'entità primaria corrente.</param>
        private void SetOtherSelectionStatus(ExcelModelSelectionTypeEnum currentSelection)
        {
            switch (currentSelection)
            {
                case ExcelModelSelectionTypeEnum.Cant:
                    LblOtherSelectionState.Text = gvColExport.FilterExpression == String.Empty ? BusinessService.GetLocalizedString(PowerWebResources.LBL_ULTERIORI_SELEZIONI_NON_ATTIVE) : BusinessService.GetLocalizedString(PowerWebResources.LBL_ULTERIORI_SELEZIONI_ATTIVE);
                    break;
                case ExcelModelSelectionTypeEnum.Col:
                    LblOtherSelectionState.Text = gvCantExport.FilterExpression == String.Empty ? BusinessService.GetLocalizedString(PowerWebResources.LBL_ULTERIORI_SELEZIONI_NON_ATTIVE) : BusinessService.GetLocalizedString(PowerWebResources.LBL_ULTERIORI_SELEZIONI_ATTIVE);
                    break;
                case ExcelModelSelectionTypeEnum.Cli:
                    LblOtherSelectionState.Text = gvCliExport.FilterExpression == String.Empty ? BusinessService.GetLocalizedString(PowerWebResources.LBL_ULTERIORI_SELEZIONI_NON_ATTIVE) : BusinessService.GetLocalizedString(PowerWebResources.LBL_ULTERIORI_SELEZIONI_ATTIVE);
                    break;
                default:
                    LblOtherSelectionState.Text = String.Empty;
                    break;
            }
        }

        #endregion

        #region Protected Methods

        #region Gestione selezione collaboratori in griglia

        /// <summary>
        /// Evento lato server scatenato al mutamento di selezione nella griglia dei collaboratori
        /// </summary>
        /// <param name="source">Il pannello di callback scatenante</param>
        /// <param name="e">I parametri che arrivano dal pannello di callback scatenante</param>
        protected void gridMasterSelectionChange_OnCallback(object source, CallbackEventArgs e)
        {
            string sourceName = e.Parameter.Split('#')[1];

            var selectedRowsCount = Convert.ToInt32(e.Parameter.Split('#').Last());
            ASPxGridView grid;
            var keyFieldName = "";

            // calcolo della chiave in base alla griglia origine
            if (sourceName == "Col")
            {
                grid = gvColExport;
                keyFieldName = "Col_Id";
            }
            else
            {
                grid = gvCantExport;
                keyFieldName = "Cant_Id";
            }
            //else
            //{
            //    grid = gvCliExport;
            //    keyFieldName = "Cli_Id";
            //}

            // ASPxGridView grid = sourceName == "Col" ? gvColExport : gvCantExport;
            // var keyFieldName = sourceName == "Col" ? "Col_Id" : "Cant_Id";

            var selctionType = e.Parameter.Split('#').First();
            switch (selctionType)
            {
                case "sAll":
                    if (sourceName == "Col")
                        SelectedColsId = new List<int>();
                    else
                        SelectedCantsId = new List<int>();
                    //else
                    //    SelectedClisId = new List<int>();
                    for (int i = 0; i < grid.VisibleRowCount; i++)
                    {
                        var entityId = Convert.ToInt32(grid.GetRowValues(i, keyFieldName));
                        if (sourceName == "Col")
                            SelectedColsId.Add(entityId);
                        else
                            SelectedCantsId.Add(entityId);
                        //else
                        //    SelectedClisId.Add(entityId);
                    }
                    break;
                case "uAll":
                    if (sourceName == "Col")
                        SelectedColsId = new List<int>();
                    else
                        SelectedCantsId = new List<int>();
                    //else
                    //    SelectedClisId = new List<int>();
                    break;
                case "sPage":
                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var gridIdValue = Convert.ToInt32(grid.GetRowValues(i, keyFieldName));
                        if (sourceName == "Col")
                        {
                            if (!SelectedColsId.Contains(gridIdValue))
                                SelectedColsId.Add(gridIdValue);
                        }
                        else
                        {
                            if (!SelectedCantsId.Contains(gridIdValue))
                                SelectedCantsId.Add(gridIdValue);
                        }
                        //else
                        //{
                        //    if (!SelectedClisId.Contains(gridIdValue))
                        //        SelectedClisId.Add(gridIdValue);
                        //}
                    }
                    break;
                case "uPage":
                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var gridIdValue = Convert.ToInt32(grid.GetRowValues(i, keyFieldName));
                        if (sourceName == "Col")
                        {
                            if (SelectedColsId.Contains(gridIdValue))
                                SelectedColsId.Remove(gridIdValue);
                        }
                        else
                        {
                            if (SelectedCantsId.Contains(gridIdValue))
                                SelectedCantsId.Remove(gridIdValue);
                        }
                        // else
                        // {
                        //     if (SelectedClisId.Contains(gridIdValue))
                        //         SelectedClisId.Remove(gridIdValue);
                        // }
                    }
                    break;
                case "sRow":
                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var gridIdValue = Convert.ToInt32(grid.GetRowValues(i, keyFieldName));
                        if (sourceName == "Col")
                        {
                            if (grid.Selection.IsRowSelected(i) && !SelectedColsId.Contains(gridIdValue))
                                SelectedColsId.Add(gridIdValue);
                        }
                        else
                        {
                            if (grid.Selection.IsRowSelected(i) && !SelectedCantsId.Contains(gridIdValue))
                                SelectedCantsId.Add(gridIdValue);
                        }
                        //else
                        //{
                        //    if (grid.Selection.IsRowSelected(i) && !SelectedClisId.Contains(gridIdValue))
                        //        SelectedClisId.Add(gridIdValue);
                        //}
                    }
                    break;
                case "uRow":
                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var gridIdValue = Convert.ToInt32(grid.GetRowValues(i, keyFieldName));
                        if (sourceName == "Col")
                        {
                            if (!grid.Selection.IsRowSelected(i) && SelectedColsId.Contains(gridIdValue))
                                SelectedColsId.Remove(gridIdValue);
                        }
                        else
                        {
                            if (!grid.Selection.IsRowSelected(i) && SelectedCantsId.Contains(gridIdValue))
                                SelectedCantsId.Remove(gridIdValue);
                        }
                        //else
                        //{
                        //    if (!grid.Selection.IsRowSelected(i) && SelectedClisId.Contains(gridIdValue))
                        //        SelectedClisId.Remove(gridIdValue);
                        //}
                    }
                    break;
            }

            grid.JSProperties["cpVisibleRowCount"] = grid.VisibleRowCount;

            // se si sta processando la griglia master si aggiorna anche il numero di selezioni effettuate dalla stessa (per controlli lato client)
            if (IsMasterGrid(grid.ID))
                SetMasterGridSelectedCount(selectedRowsCount);


        }

        /// <summary>
        /// Evento scatenato al cambio pagine della griglia dei collaboratori; utilizzata per segnalare quanto avvenuto lato client.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento</param>
        /// <param name="e">I parametri dell'evento</param>
        protected void gvColExport_OnPageIndexChanged(object sender, EventArgs e)
        {
            ManageGridIndexPageChanged(sender);
        }

        /// <summary>
        /// Evento scatenato all'inizializzazione del checkbox di selezione di tutta la griglia; Imposta il check del checkbox mittente in base alla selezione effettuata.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbAllCol_Init(object sender, EventArgs e)
        {
            InitSelectAllCheckbox(sender);
        }

        /// <summary>
        /// Evento scatenato all'inizializzazione del checkbox di selezione della pagina corrente; Imposta il check del checkbox mittente in base alla selezione di pagina effettuata.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbPageCol_Init(object sender, EventArgs e)
        {
            InitPageSelectorCheckbox(sender);
        }

        /// <summary>
        /// Evento scatenato alla richiesta di una proprietà js dell'oggetto checkbox di selezione dell'intera griglia;
        /// Viene impostata la domanda in lingua da visualizzare nella conferma che sarà richiesta
        /// </summary>
        /// <param name="sender">Il mittente dell'evento</param>
        /// <param name="e">I parametri dell'evento</param>
        protected void cbAllCol_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            SetSelectAllConfirmationMessage(e);
        }

        /// <summary>
        /// Evento scatenato alla richiesta di una proprietà js dell'oggetto grilia dei collaboratori;
        /// Riporta per il lato client dell'applicativo il numero di record selezionati e il numero di record contenuti in griglia
        /// </summary>
        /// <param name="sender">Il mittente dell'evento</param>
        /// <param name="e">I parametri dell'evento</param>
        protected void gvColExport_OnCustomJSProperties(object sender, ASPxGridViewClientJSPropertiesEventArgs e)
        {
            SetGridRowsState(sender, e);
        }

        #endregion

        #region Gestione selezione cantieri in griglia

        /// <summary>
        /// Evento scatenato al cambio pagine della griglia dei cantieri; utilizzata per segnalare quanto avvenuto lato client.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento</param>
        /// <param name="e">I parametri dell'evento</param>
        protected void gvCant_OnPageIndexChanged(object sender, EventArgs e)
        {
            ManageGridIndexPageChanged(sender);
        }

        /// <summary>
        /// Evento scatenato all'inizializzazione del checkbox di selezione di tutta la griglia; Imposta il check del checkbox mittente in base alla selezione effettuata.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbAllCant_Init(object sender, EventArgs e)
        {
            InitSelectAllCheckbox(sender);
        }

        /// <summary>
        /// Evento scatenato all'inizializzazione del checkbox di selezione della pagina corrente; Imposta il check del checkbox mittente in base alla selezione di pagina effettuata.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbPageCant_Init(object sender, EventArgs e)
        {
            InitPageSelectorCheckbox(sender);
        }

        /// <summary>
        /// Evento scatenato alla richiesta di una proprietà js dell'oggetto checkbox di selezione dell'intera griglia;
        /// Viene impostata la domanda in lingua da visualizzare nella conferma che sarà richiesta
        /// </summary>
        /// <param name="sender">Il mittente dell'evento</param>
        /// <param name="e">I parametri dell'evento</param>
        protected void cbAllCant_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            SetSelectAllConfirmationMessage(e);
        }

        /// <summary>
        /// Evento scatenato alla richiesta di una proprietà js dell'oggetto grilia dei Cantlaboratori;
        /// Riporta per il lato client dell'applicativo il numero di record selezionati e il numero di record contenuti in griglia
        /// </summary>
        /// <param name="sender">Il mittente dell'evento</param>
        /// <param name="e">I parametri dell'evento</param>
        protected void gvCant_OnCustomJSProperties(object sender, ASPxGridViewClientJSPropertiesEventArgs e)
        {
            SetGridRowsState(sender, e);
        }

        #endregion

        #region Gestion selezione clienti in grilgia

        /// <summary>
        /// Evento scatenato al cambio pagine della griglia clienti; utilizzata per segnalare quanto avvenuto lato client.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gvCliExport_OnPageIndexChanged(object sender, EventArgs e)
        {
            ManageGridIndexPageChanged(sender);
        }

        /// <summary>
        /// Evento scatenato all'inizializzazione del checkbox di selezione di tutta la griglia; Imposta il check del checkbox mittente in base alla selezione effettuata.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbAllCli_Init(object sender, EventArgs e)
        {
            InitSelectAllCheckbox(sender);
        }

        /// <summary>
        /// Evento scatenato all'inizializzazione del checkbox di selezione della pagina corrente; Imposta il check del checkbox mittente in base alla selezione di pagina effettuata.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbPageCli_Init(object sender, EventArgs e)
        {
            InitPageSelectorCheckbox(sender);
        }

        /// <summary>
        /// Evento scatenato alla richiesta di un proprietà JS dell'oggetto checkbox di selezione dell'intera griglia;
        /// Viene impostata la domanda in lingua da visualizzare nella conferma che sarà richiesta.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CustomJSPropertiesEventArgs"/> instance containing the event data.</param>
        protected void cbAllCli_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            SetSelectAllConfirmationMessage(e);
        }

        /// <summary>
        /// Evento scatenato alla richiesta di una proprietà JS dell'oggetto griglia dei Clienti;
        /// Riporta per il lato client dell'applicativo il numero di record selezionati e il numero di record contenuti in griglia.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxGridViewClientJSPropertiesEventArgs"/> instance containing the event data.</param>
        protected void gvCliExport_OnCustomJSProperties(object sender, ASPxGridViewClientJSPropertiesEventArgs e)
        {
            SetGridRowsState(sender, e);
        }

        #endregion

        #region Selezione e cambio tipo export

        /// <summary>
        /// Callback scatenato al cambio di selezione della combo tipo export.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="callbackEventArgsBase">instance containing the event data.</param>
        protected void CbpExportTypeChanged_OnCallback(object source, CallbackEventArgsBase callbackEventArgsBase)
        {
            // imposto come proprietà del callback panel il valore dell'entità selezionata e l'eventuale visualizzazione dei dati aggiuntivi
            CbpExportTypeChanged.InitializeJsProperty("cpSelectedEntity", String.Empty);
            CbpExportTypeChanged.InitializeJsProperty("cpShowCalculationType", false);
            CbpExportTypeChanged.InitializeJsProperty("cpShowDetail", false);
            CbpExportTypeChanged.InitializeJsProperty("cpShowHoursType", false);
            CbpExportTypeChanged.InitializeJsProperty("cpShowDurationTollerance", false);
            CbpExportTypeChanged.InitializeJsProperty("cpShowEUTollerance", false);

            // viene recuperato il modello excel selezionato dall'utente
            Tab_Excel_Model excelModel = GetTabExcelModel(Convert.ToInt32(cmbTipoExport.Value));

            // recupero l'entità di riferimento dal modello excel selezionato dall'utente
            var firstEntityType = GetExcelSelectionType(excelModel);

            // scrivo in sessione l'entità di selezione primaria indicata dal combobox
            CurrentFirstEntity = firstEntityType;

            ///per le js properties devono sempre iniziare con cp
            CbpExportTypeChanged.JSProperties["cpSelectedEntity"] = firstEntityType.ToString();
            CbpExportTypeChanged.JSProperties["cpShowCalculationType"] = excelModel != null && excelModel.Selezione_Figurative_Fisiche;
            CbpExportTypeChanged.JSProperties["cpShowHoursType"] = excelModel != null && excelModel.Selezione_Solo_Durata_EU;
            CbpExportTypeChanged.JSProperties["cpShowDurationTollerance"] = excelModel != null && excelModel.Selezione_Tolleranza_Durata;
            CbpExportTypeChanged.JSProperties["cpShowEUTollerance"] = excelModel != null && excelModel.Selezione_Tolleranza_EU;
            //viene recuperato il valore del capo database se visualizzare oppure no il checkbox del dettaglio
            CbpExportTypeChanged.JSProperties["cpShowDetail"] = excelModel != null && excelModel.Seleziona_Dettagli;

            //se è attivo nella tabella la visualizzazione dei dettagli allora come valore di default viene messa a false altrimenti è true
            ASPxCheckBoxDetali.Checked = !excelModel.Seleziona_Dettagli;

            // al cambio dell'export azzero i dati selezionati
            ResetSession();

            // ricalcolo dei dati della griglia master
            BindMasterGrid(true);

            // imposto il testo del pulsante di attivazione delle ulteriori selezioni
            SetOtherSelectionButtonText(firstEntityType);

            // ricalcolo l'etichetta di stato delle ulteriori selezioni
            SetOtherSelectionStatus(firstEntityType);
        }

        /// <summary>
        /// Evento scatenato alla inizializzaione della griglia dei collaboratori; si occupa di
        /// visualizzare o nascondere eventuali blocchi non utilizzati quando slave (cioé vuota).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gvColExport_OnInit(object sender, EventArgs e)
        {
            // se sto selezionando i collaboratori allora visualizzo i dati utili;
            // altrimenti li nascondo
            ShowOrHideUnusedGridElements((ASPxGridView)sender, CurrentFirstEntity != ExcelModelSelectionTypeEnum.Col);
        }

        /// <summary>
        /// Evento scatenato alla inizializzaione della griglia dei cantieri; si occupa di
        /// visualizzare o nascondere eventuali blocchi non utilizzati quando slave (cioé vuota).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gvCantExport_OnInit(object sender, EventArgs e)
        {
            // se sto selezionando i collaboratori allora visualizzo i dati utili;
            // altrimenti li nascondo
            ShowOrHideUnusedGridElements((ASPxGridView)sender, CurrentFirstEntity != ExcelModelSelectionTypeEnum.Cant);
        }

        /// <summary>
        /// Evento scatenato alla inizializzione della griglia dei clienti; si occpua di
        /// visualizzare o nascondere eventuali blocchi non utilizzati quando slave (cioè vuota).
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gvCliExport_OnInit(object sender, EventArgs e)
        {
            // se sto selezionando i collaboratori allora visualizzo i dati utili;
            // altrimenti li nascondo
            ShowOrHideUnusedGridElements((ASPxGridView)sender, CurrentFirstEntity != ExcelModelSelectionTypeEnum.Cli);
        }

        #endregion

        #region Lancio export

        /// <summary>
        /// Metodo collegato all'evento di click del pulsante di lancio dell'export;
        /// si occupa di effettuare tutte le fasi di preparazione, compilazione e ritorno dell'export all'utente
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void BtnLaunchExport_OnClick(object sender, EventArgs e)
        {
            string path = "";
            
            // carica il record della tabella Tab_Excel_Model relativo al tipo di export selezionato
            Tab_Excel_Model excelModel = GetTabExcelModel(Convert.ToInt32(cmbTipoExport.Value));

            // recupero tutte le reg_v da processare (se richiesto dal modulo) oppure imposto correttamente le selezioni di
            // cantieri e collaboratori
            IQueryable<Reg_V> regVsToProcess = null;
            if (!excelModel.Utilizza_Solo_Selezione)
                regVsToProcess = GetRegVsFromSelection();
               else
                 SetEntitiesSelection();

            //try
            //{
            //    int x = regVsToProcess.Count();
            //}
            //catch (Exception z) { }
            //Commentata in data 09/06/2022 perchè andava a generare un eccezione nel caso in cui l'export sia di tipo solo selezione
            

            // calcolo del nome dell'export specializzato (Tab_Excel_Model.Nome_Specializzato) e del nome file modello (Tab_Excel_Model.ModelFilePath)
            string exportSpecializedName = excelModel.Nome_Specializzato;
            string exportModelName = GetExcelSelectionModelName(excelModel);

            // recupera il tipo di selezione (Tab_Excel_Model.Tipo_Selezione) - attualmente Col/Can/NULL   
            ExcelModelSelectionTypeEnum firstEntityModel = GetExcelSelectionType(excelModel);

            ObjectHandle exportHandle = Activator.CreateInstance(CustomExportNamespace, exportSpecializedName);
            object exportObject = exportHandle.Unwrap();

            // Il tipo di export (Excel o txt) viene deciso a seconda del Nome_Specializzato (attualmente ExportTxtCustom per txt, tutto il resto Excel) 
            if (excelModel.Nome_Specializzato.Split('.')[1].Equals("ExportTxtCustom"))
            {
                var exportToProcess = (Exports.ExportTxtCustom.IExportTxtCustom<Reg_V>)exportObject;

                exportToProcess.FileName = exportModelName;
                exportToProcess.ExportPeriod = dePeriodo.Date;
                exportToProcess.UseCalculationType = excelModel.Selezione_Figurative_Fisiche;
                exportToProcess.UseHourType = excelModel.Selezione_Solo_Durata_EU;
                exportToProcess.UseDurationTollerance = excelModel.Selezione_Tolleranza_Durata;
                exportToProcess.DurationTollerance = exportToProcess.UseDurationTollerance ? Convert.ToInt32(SpedtTolleranzaDurata.Number) : 0;
                exportToProcess.UseEUTollerance = excelModel.Selezione_Tolleranza_EU;
                exportToProcess.EUTollerance = exportToProcess.UseEUTollerance ? Convert.ToInt32(SpedtTolleranzaEU.Number) : 0;
                exportToProcess.CalculationType = excelModel.Selezione_Figurative_Fisiche ? (ExportRegVCalculationTypeEnum)RdBtnTipoCalcolo.Value : default(ExportRegVCalculationTypeEnum);
                exportToProcess.HourType = excelModel.Selezione_Solo_Durata_EU ? (ExportRegVHourTypeEnum)RdBtnTipoOre.Value : default(ExportRegVHourTypeEnum);
                exportToProcess.UseExportDetail = (bool)ASPxCheckBoxDetali.Value;
                exportToProcess.ModelFirstEntity = firstEntityModel;


                // lancia l'export
                if (!excelModel.Utilizza_Solo_Selezione)
                    exportToProcess.LaunchExport(regVsToProcess);
                else
                    exportToProcess.LaunchExport(SelectedColsId, SelectedCantsId);
            }
            else
            {
                // se si sta processando uno specializzato da griglia (export56)
                if (exportObject is IExportExcelSpecialized<ActivityItem>)
                {
                    var exportToProcess = (IExportExcelSpecialized<ActivityItem>)exportObject;
                    List<ActivityItem> itemList = BusinessService.PopulateActivityList(regVsToProcess.ToList());
                    ExportExcelEngine.Export<ActivityItem>(exportToProcess, itemList, excelModel, out path);
                }
                else // se invece si sta processando un export custom
                {
                    var exportToProcess = (IExportExcelCustom<Reg_V>)exportObject;

                    // calcolo il percorso del modello da esporatare
                    var modelFile = new FileInfo(HttpContext.Current.Server.MapPath(String.Format("{0}{1}", Common.Properties.Settings.Default.ExcelModelsPath, exportModelName)));
                    exportToProcess.ExcelModelFilePath = modelFile.FullName;

                    // impostazione del periodo di ricerca
                    exportToProcess.ExportPeriod = dePeriodo.Date;

                    // impostazione dei tipi di calcolo specifici
                    exportToProcess.UseCalculationType = excelModel.Selezione_Figurative_Fisiche;
                    exportToProcess.UseHourType = excelModel.Selezione_Solo_Durata_EU;
                    exportToProcess.UseDurationTollerance = excelModel.Selezione_Tolleranza_Durata;
                    exportToProcess.DurationTollerance = exportToProcess.UseDurationTollerance ? Convert.ToInt32(SpedtTolleranzaDurata.Number) : 0;
                    exportToProcess.UseEUTollerance = excelModel.Selezione_Tolleranza_EU;
                    exportToProcess.EUTollerance = exportToProcess.UseEUTollerance ? Convert.ToInt32(SpedtTolleranzaEU.Number) : 0;
                    exportToProcess.CalculationType = excelModel.Selezione_Figurative_Fisiche ? (ExportRegVCalculationTypeEnum)RdBtnTipoCalcolo.Value : default(ExportRegVCalculationTypeEnum);
                    exportToProcess.HourType = excelModel.Selezione_Solo_Durata_EU ? (ExportRegVHourTypeEnum)RdBtnTipoOre.Value : default(ExportRegVHourTypeEnum);
                    exportToProcess.UseExportDetail = (bool)ASPxCheckBoxDetali.Value;

                    exportToProcess.ModelFirstEntity = firstEntityModel;

                    // lancio l'export (produzione del file excel con i dati calcolati
                    if (!excelModel.Utilizza_Solo_Selezione)
                        exportToProcess.LaunchExport(regVsToProcess);
                    else
                        exportToProcess.LaunchExport(SelectedColsId, SelectedCantsId, SelectedClisId);
                }
            }
        }

        #endregion

        #region Gestione Stato ulteriori selezioni

        /// <summary>
        /// Eseguito al callback del pannello CbpOtherSelectionStatus; si occupa di aggiornare l'etichetta di stato
        /// delle ulteriori selezioni
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The events args.</param>
        protected void CbpOtherSelectionStatus_OnCallback(object sender, CallbackEventArgsBase e)
        {
            SetOtherSelectionStatus(CurrentFirstEntity);
        }

        #endregion

        #endregion

    }
}
