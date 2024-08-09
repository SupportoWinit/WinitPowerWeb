using Business;
using Business.BusinessExtension;
using Business.Repository;
using Common;
using DevExpress.Data.PLinq.Helpers;
using DevExpress.Utils;
using DevExpress.Web.ASPxCallback;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxEditors;
using DevExpress.Web.ASPxFormLayout;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.ASPxGridView.Export;
using DevExpress.Web.ASPxPanel;
using DevExpress.Web.Data;
using Domain;
using Exports.ExportExcelCustom;
using log4net;
using Reports;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TableCell = System.Web.UI.WebControls.TableCell;

namespace PowerWeb.Modules
{

    public partial class TimesheetModule : BaseGridModule, ILogModule, IDoubleGridModule, IQuadGridModule, IPrintModule, IExportXLSXModule
    {

        #region Private Constants

        /// <summary>
        /// Il campo chiave della griglia dei cantieri
        /// </summary>
        const string CantKeyFieldName = "Cant_Id";

        /// <summary>
        /// Il campo chiave della griglia dei collaboratori
        /// </summary>
        private const string ColKeyFieldName = "Col_Id";

        /// <summary>
        /// Il campo chiave della griglia dei clienti
        /// </summary>
        private const string CliKeyFieldName = "Cli_Id";

        /// <summary>
        /// L'entità di riferimento del collaboratore (codice)
        /// </summary>
        private const string ColEntityType = "Col";

        /// <summary>
        /// L'entità di riferimento del cantiere (codice)
        /// </summary>
        private const string CantEntityType = "Can";

        /// <summary>
        /// L'entità di riferimento del cliente (codice)
        /// </summary>
        private const string CliEntityType = "Cli";

        /// <summary>
        /// Il nome della tabella delle entità selezionabili all'interno della Tab_Decod
        /// </summary>
        private const string EntityTabName = "ENTITA_TIMESHEET";

        private const TimesheetModuleItem TsmStub = null;

        private const Col _colStub = null;

        private const string Timesheetkeyfieldname = "ID";

        private const string Colorariokeyfieldname = "FreeTimeSheetId";

        /// <summary>
        /// Il tag di default sul gruoup summary del delta
        /// </summary>
        private const string DeltaSummaryDefaultTag = "Delta";

        /// <summary>
        /// Il tag di default sul gruoup summary del piano
        /// </summary>
        private const string PlanSummaryDefaultTag = "Plan";

        /// <summary>
        /// Il tag di default sul gruoup summary del piano
        /// </summary>
        private const string PianoSummaryDefaultTag = "Piano";

        /// <summary>
        /// Il tag di default sul gruoup summary del totale
        /// </summary>
        private const string TotalSummaryDefaultTag = "Tot";

        /// <summary>
        /// Il tag di default sul gruoup summary degli straordinari
        /// </summary>
        private const string StrSummaryDefaultTag = "Str";

        /// <summary>
        /// Il tag di default sul gruoup summary degli straordinari notturni
        /// </summary>
        private const string StrNotSummaryDefaultTag = "StrNot";

        /// <summary>
        /// Il tag di default sul gruoup summary delle ore ordinarie
        /// </summary>
        private const string OrdinarySummaryDefaultTag = "Ord";

        /// <summary>
        /// Il tag di default sul gruoup summary delle ore con motivazione
        /// </summary>
        private const string JustificationSummaryDefaultTag = "Just";

        /// <summary>
        /// Il tag di default sul gruoup summary delle ore con motivazione
        /// </summary>
        private const string ArrotSummaryDefaultTag = "Arrot";

        /// <summary>
        /// Il valore che assume l'opzione flaggata dall'utente per la visualizzazione delle ore cartellino in centesimi
        /// </summary>
        private const string ShowDecimalHoursOptionValue = "OPZTS_CENTESIMI";

        /// <summary>
        /// Il valore che assume l'opzione flaggata dall'utente per la visualizzazione delle sole ore piano nel cartellino
        /// </summary>
        private const string ShowOnlyPlanOptionValue = "OPZTS_ORE_PIANO";

        /// <summary>
        /// Il valore che assume l'opzione flaggata dall'utente per la visualizzazione delle ore divise per l'altra entità anagrafica
        /// </summary>
        private const string DividePlanForOtherEntityOptionValue = "OPZTS_DIVIDI_PER";

        /// <summary>
        /// Il valore che assume l'opzione flaggata dall'utente per la utilizzare le ore fisiche nella generazione del cartellino
        /// </summary>
        private const string UsaFisicheOptionValue = "OPZTS_USA_FISICHE";

        /// <summary>
        /// Il valore che assume l'opzione flaggata dall'utente per non visualizzare le ore del piano
        /// </summary>
        private const string NoShowPlanOptionValue = "OPZTS_NO_ORE_PIANO";

        /// <summary>
        /// Il valore che assume l'opzione flaggata dall'utente per non visualizzare il delta
        /// </summary>
        private const string NoShowDeltaOptionValue = "OPZTS_NO_DELTA";

        /// <summary>
        /// Il valore che assume l'opzione flaggata dall'utente per non visualizzare le motivazioni
        /// </summary>
        private const string NoShowMotOptionValue = "OPZTS_NO_MOT";

        /// <summary>
        /// Il valore che assume l'opzione flaggata dall'utente per non visualizzare le motivazioni
        /// </summary>
        private const string NoShowArrotOptionValue = "OPZTS_NO_ARROT";

        #endregion

        #region Private Fields

        private static readonly ILog _log = LogManager.GetLogger(typeof(TimesheetModule));

        private readonly Dictionary<string, int> _minutesDictionary = new Dictionary<string, int>();

        private readonly Dictionary<int, string> _justificationsRowsDictionary = new Dictionary<int, string>();

        private readonly Dictionary<int, int> _lastMonthlyPerRowHandleDictionary = new Dictionary<int, int>();

        private readonly Dictionary<int, int> _totalHoursPerColDictionary = new Dictionary<int, int>();

        private readonly Dictionary<int, int> _totalHoursPerCantDictionary = new Dictionary<int, int>();

        private readonly Dictionary<int, int> _rowHandleColIdMappingDictionary = new Dictionary<int, int>();

        private readonly Dictionary<int, int> _rowHandleCantIdMappingDictionary = new Dictionary<int, int>();

        #endregion

        #region Protected Properties

        /// <summary>
        /// Recupera o imposta l'elenco delle opzioni selezionate dall'utente riguardo alla visualizzazione dei dati nel timesheet.
        /// </summary>
        /// <value>
        /// L'elenco delle opzioni selezionate dall'utente riguardo alla visualizzazione dei dati nel timesheet.
        /// </value>
        protected List<string> SelectedTimesheetOptions
        {
            get
            {
                if (PowerWebContext.GetFromSession<List<string>>("SelectedTimesheetOptions_" + TimesheetGridView.ID) == null)
                    PowerWebContext.SetToSession<List<string>>("SelectedTimesheetOptions_" + TimesheetGridView.ID, new List<string>());

                return PowerWebContext.GetFromSession<List<string>>("SelectedTimesheetOptions_" + TimesheetGridView.ID);
            }

            set
            {
                PowerWebContext.SetToSession<List<string>>("SelectedTimesheetOptions_" + TimesheetGridView.ID, value);
            }
        }

        /// <summary>
        /// Recupera o imposta in/da sessione l'entità di selezione primaria indicata con il combobox.
        /// </summary>
        /// <value>
        /// L'entità di selezione primaria indicata con il combobox.
        /// </value>
        protected string CurrentFirstEntity
        {
            get
            {
                if (PowerWebContext.GetFromSession<string>("currentFirstEntity_" + gvCantSel.ID) == null)
                {
                    PowerWebContext.SetToSession<string>("currentFirstEntity_" + gvCantSel.ID, ColEntityType);
                }
                return PowerWebContext.GetFromSession<string>("currentFirstEntity_" + gvCantSel.ID);
            }

            set
            {
                PowerWebContext.SetToSession<string>("currentFirstEntity_" + gvCantSel.ID, value);
            }
        }

        /// <summary>
        /// Recupera o imposta l'elenco dei cantieri da visualizzare all'interno della griglia di selezione
        /// </summary>
        protected List<Cant> Cants
        {
            get
            {
                List<Cant> cants = PowerWebContext.GetFromSession<List<Cant>>("cants_" + gvCantSel.ID);
                if (cants == null)
                {
                    cants = RepoManager.CantRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cant>>("cants_" + gvCantSel.ID, cants);
                }
                return cants;
            }

            set
            {
                PowerWebContext.SetToSession<List<Cant>>("cants_" + gvCantSel.ID, value);
            }
        }

        /// <summary>
        /// Recupera o imposta l'elenco degli id collaboratore selezionati nella griglia
        /// </summary>
        protected List<int> SelectedColsId
        {
            get
            {
                var colsel = PowerWebContext.GetFromSession<List<int>>("SelectedColsId" + gvColSel.ID);
                if (colsel == null)
                {
                    colsel = new List<int>();
                    PowerWebContext.SetToSession("SelectedColsId" + gvColSel.ID, colsel);
                }
                return colsel;

            }

            set
            {
                List<int> list = value;
                if (list != null)
                    PowerWebContext.SetToSession("SelectedColsId" + gvColSel.ID, list);
            }
        }

        /// <summary>
        /// Recupera o imposta l'elenco degli id cantiere selezionati nella griglia
        /// </summary>
        protected List<int> SelectedCantsId
        {
            get
            {
                var cantsel = PowerWebContext.GetFromSession<List<int>>("SelectedCantsId" + gvCantSel.ID);
                if (cantsel == null)
                {
                    cantsel = new List<int>();
                    PowerWebContext.SetToSession("SelectedCantsId" + gvCantSel.ID, cantsel);
                }
                return cantsel;

            }

            set
            {
                List<int> list = value;
                if (list != null)
                    PowerWebContext.SetToSession("SelectedCantsId" + gvCantSel.ID, list);
            }
        }

        /// <summary>
        /// Recupera i imposta la lista dei collaboratori da visualizzare in griglia.
        /// </summary>
        /// <value>
        /// La lista dei collaboratori da visualizzare in griglia.
        /// </value>
        protected List<Col> Cols
        {
            get
            {
                List<Col> cols = PowerWebContext.GetFromSession<List<Col>>("cols_" + gvColSel.ID);
                if (cols == null)
                {
                    cols = RepoManager.ColRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Col>>("cols_" + gvColSel.ID, cols);
                }
                return cols;
            }

            set
            {
                PowerWebContext.SetToSession<List<Col>>("cols_" + gvColSel.ID, value);
            }
        }

        /// <summary>
        /// Recupera o imposta la lista dei collaboratori selezionati.
        /// </summary>
        /// <value>
        /// La lista dei collaboratori selezionati.
        /// </value>
        protected List<Col> VisibleCols
        {
            get
            {
                List<Col> cols = new List<Col>();

                //vado a scorrere i collaboratori selezionati
                for (int i = 0; i < gvColSel.VisibleRowCount; i++)
                {
                    if (gvColSel.Selection.IsRowSelected(i))
                    {
                        var currentCol = gvColSel.GetRow(i) as Col;
                        if (currentCol != null)
                            cols.Add(currentCol);
                    }
                }
                return cols;
            }
        }

        /// <summary>
        /// Recupera l'oggetto utilizzato per il calcolo e la preparazione dei totali del cartellino.
        /// </summary>
        /// <value>
        /// L'oggetto utilizzato per il calcolo e la preparazione dei totali del cartellino.
        /// </value>
        protected TimesheetTotalController TsTotalController
        {
            get
            {
                return PowerWebContext.GetFromSession<TimesheetTotalController>("tsmTotalController_" + TimesheetGridView.ID);
            }
        }

        /// <summary>
        /// Recupera l'elenco degli oggetti riga cartellino da visualizzare nella griglia cartellino.
        /// </summary>
        /// <value>
        /// L'elenco degli oggetti riga cartellino da visualizzare nella griglia cartellino.
        /// </value>
        protected List<TimesheetModuleItem> TsmItems
        {
            get
            {
                return PowerWebContext.GetFromSession<List<TimesheetModuleItem>>("tsmItems_" + TimesheetGridView.ID);
            }
        }

        /// <summary>
        /// Recupera o imposta la proprietà che indica se visualizzare o meno il cartellino con i totali settimanali.
        /// </summary>
        /// <value>
        /// <c>true</c> se è da visualizzare il cartellino con totali settimanali; altriemnti, <c>false</c>.
        /// </value>
        protected bool ShowWeeklyTotals
        {
            get
            {
                return PowerWebContext.GetFromSession<bool>("ShowTimesheetsWeeklyTotals_" + GridView.ID);
            }

            set
            {
                PowerWebContext.SetToSession<bool>("ShowTimesheetsWeeklyTotals_" + GridView.ID, value);
            }
        }

        /// <summary>
        /// Recupera la griglia utilizzata per la visualizzazione del cartellino.
        /// </summary>
        /// <value>
        /// La griglia utilizzata per la visualizzazione del cartellino.
        /// </value>
        protected ASPxGridView TimesheetGridView
        {
            get { return ChkShowWeeklyTotals.Checked ? gvTimesheetWeeklyTotals : gvTimesheet; }
        }

        #endregion

        #region Public Properties

        public ASPxGridView GridView2
        {
            get { return gvCantSel; }

        }

        public PowerFormTemplate EditFormTemplate2
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView2.ID);
                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryCant();

                    template = new PowerFormTemplate(this, templateDic);
                    //template = PowerWebService.GeneratePowerFormTemplate(this, templateDic, MetaFieldDescriptors);

                    PowerWebContext.SetToSession<PowerFormTemplate>("PowerFormTemplate_" + GridView2.ID, template);
                }
                return template;
            }
        }

        public ASPxGridView GridView3
        {
            get { return gvColSel; }
        }

        public PowerFormTemplate EditFormTemplate3
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView3.ID);
                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryCol();

                    template = new PowerFormTemplate(this, templateDic);
                    // template = PowerWebService.GeneratePowerFormTemplate(this, templateDic, MetaFieldDescriptors);

                    PowerWebContext.SetToSession<PowerFormTemplate>("PowerFormTemplate_" + GridView3.ID, template);
                }
                return template;
            }
        }

        public ASPxGridView GridView4
        {
            get { return null; }
        }

        public PowerFormTemplate EditFormTemplate4
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView4.ID);
                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryCli();

                    template = new PowerFormTemplate(this, templateDic);
                    // template = PowerWebService.GeneratePowerFormTemplate(this, templateDic, MetaFieldDescriptors);

                    PowerWebContext.SetToSession<PowerFormTemplate>("PowerFormTemplate_" + GridView4.ID, template);
                }
                return template;
            }
        }

        public override ASPxGridView GridView
        {
            get { return gvColSel; }
        }

        public override PowerFormTemplate EditFormTemplate
        {
            get
            {
                PowerFormTemplate template =
                PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID);
                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryCol();

                    template = new PowerFormTemplate(this, templateDic);
                    //template = PowerWebService.GeneratePowerFormTemplate(this, templateDic, MetaFieldDescriptors);

                    PowerWebContext.SetToSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID, template);
                }
                return template;
            }
        }

        public override PowerFormTemplate EditDetailFormTemplate
        {
            get { return null; }
        }

        public override Type EntityType
        {
            get { return typeof(Col); }
        }

        public ILog Log
        {
            get { return _log; }
        }

        /// <summary>
        /// Recupera l'elenco dei modelli utilizzabili in fase di export.
        /// </summary>
        /// <value>
        /// L'elenco dei modelli utilizzabili in fase di export.
        /// </value>
        public List<Tab_Excel_Model> Models
        {
            get
            {
                var models = new List<Tab_Excel_Model>();
                if (!ChkShowWeeklyTotals.Checked && !SelectedTimesheetOptions.Any(opz => opz == DividePlanForOtherEntityOptionValue))
                {
                    var typeName = typeof(TimesheetModuleItem).Name;
                    models = RepoManager.Tab_Excel_ModelRepo.Find(xlsxModel => xlsxModel.IsActive && xlsxModel.Nome_Entity == typeName).ToList();
                }

                return models;
            }
        }

        /// <summary>
        /// Recupera il template della form utilizzata per il recupero dei dati di raggruppamento in stampa;
        /// se impostato a null si utilizza il valore specificato nel modulo.
        /// </summary>
        /// <value>
        /// il template della form utilizzata per il recupero dei dati di raggruppamento in stampa;
        /// se impostato a null si utilizza il valore specificato nel modulo.
        /// </value>
        public PowerFormTemplate PrintFormTemplate
        {
            get
            {
                return new PowerFormTemplate(this, new Dictionary<TabPageExtended, List<TabPageItemExtended>>(), true);
            }
        }

        /// <summary>
        /// Recupera la griglia utilzizata per la raccolta dei dati da esportare.
        /// Se impostato a null si utilizza la griglia principale del modulo
        /// Proprietà utilizzata solamente in caso di implementazione dell'interfaccia <see cref="IExportXLSXModule" />
        /// </summary>
        /// <value>
        /// La griglia utilizzata per la raccolta dei dati da esportare.
        /// Se impostato a null si utilizza la griglia principale del modulo.
        /// Proprietà utilizzata solamente in caso di implementazione dell'interfaccia <see cref="IExportXLSXModule" />
        /// </value>
        public override ASPxGridView ExportGridView
        {
            get
            {
                return gvTimesheet;
            }
        }

        /// <summary>
        /// Recupera la griglia utilizzata per il recupero dei dati di stampa;
        /// se impostato a null si utilizza la griglia impostata nella proprietà GridView.
        /// Proprietà utilizzata solamente in caso di implementazione dell'interfaccia <see cref="IPrintModule" />
        /// </summary>
        /// <value>
        /// La griglia utilizzata per il recupero dei dati di stampa;
        /// se impostato a null si utilizza la griglia impostata nella proprietà GridView.
        /// Proprietà utilizzata solamente in caso di implementazione dell'interfaccia <see cref="IPrintModule" />
        /// </value>
        public override ASPxGridView PrintGridView
        {
            get
            {
                return ChkShowWeeklyTotals.Checked ? gvTimesheetWeeklyTotals : gvTimesheet;
            }
        }

        #endregion

        #region Page Events

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            gvColSel.JSProperties["cpPageChanged"] = 0;
            gvCantSel.JSProperties["cpPageChanged"] = 0;
            //gvCliSel.JSProperties["cpPageChanged"] = 0;

            // le seguenti funzioni di calcolo della griglia del cartellino sono state spostate
            // in questo evento in quanto, nel ciclo di vita della pagina, nel metoto onload sono già
            // stati calcolati i valori degli elementi ui e quindi è possibile stabilire quale griglia
            // si sta visualizzando (con soli totali mensili/con totali mensili e settimanali)

            // fill dei combobox nella griglia cartellino
            PowerWebService.FillComboboxes(TimesheetGridView);

            // bind della griglia cartellino
            BindTimesheetGrid();
        }

        /// <summary>
        /// Handles the Init event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Page_Init(object sender, EventArgs e)
        {
            GridView.KeyFieldName = ColKeyFieldName;
            GridView2.KeyFieldName = CantKeyFieldName;
            //GridView4.KeyFieldName = CliKeyFieldName;

            if (!Page.IsPostBack && !Page.IsCallback)
            {
                ResetSession();

                PowerWebContext.SetToSession<String>("TimesheetGridViewLayout_" + TimesheetGridView.ID, TimesheetGridView.SaveClientLayout());
                BindTsmLayoutCombo();

                // se sono alla prima apertura del modulo cartellino allora inizializzo il periodo di visualizzazione al mese corrente
                deTimesheet.Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                // se si sta processando la prima apertura della pagina allora si imposta come default la griglia dei collaboratori
                cmbEntityType.Value = ColEntityType;
            }

            PowerWebService.FillGridLabels(typeof(Col), GridView);
            PowerWebService.FillComboboxes(gvColSel);
            PowerWebService.FillGridLabels(typeof(Cant), GridView2);
            PowerWebService.FillComboboxes(gvCantSel);
            //PowerWebService.FillGridLabels(typeof(Cli), GridView4);
            //PowerWebService.FillComboboxes(gvCliSel);

            // bind del combobox del tipo entità da ricercare per il cartellino
            PowerWebService.FillComboboxes(cmbEntityType, "Tipo_Entita_Timesheet");
            if (!cmbEntityType.ReadOnly)
            {
                EditButton btnEdit = new EditButton("X");
                cmbEntityType.Buttons.Add(btnEdit);

                cmbEntityType.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
            }

            // bind delle griglie da visualizzare
            BindMasterGrid();

            #region Inizializzazione dei dati del pannello di gestione delle rettifiche (se richiesto dalle personalizzazioni)

            // recupero l'eventuale customizzazione relativa alla gestione delle rettifiche
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.UseCorrectionEnum);

            // se è richiesto dalla customizzazione di attivare le rettifiche allora inizializzo il pannello
            if (customizationVersion == (int)UseCorrectionEnum.Use)
            {
                // localizzazione delle etichette
                LocalizeCorrectionLayout();
                LocalizeSelectionLabels();
                LocalizeFormsLayout();

                // se si sta aprendo per la prima volta la pagina
                if (!Page.IsPostBack && !Page.IsCallback)
                {
                    // inizializzazione dei campi di inserimento rettifica
                    InitializeCorrectionLayout();
                }
            }
            else // altrimenti, se la customizzazione non è attiva allora nascondo il form layout
            {
                flRegCorrection.Visible = false;
            }

            #endregion

            // messa in lingua dei dati di testata delle griglie cartellino
            LocalizeTimesheetGridsHeader();
        }

        #endregion

        #region Gestione DataGrid Master dei Collaboratori

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Data_Registrazione_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.DataOraUltimaModifica_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Assegni_Famigliari_Fine_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Assegni_Famigliari_Inizio_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Data_Disponibilita_Fine_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Data_Disponibilita_Inizio_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Nascita_Data_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Scadenza_Patente_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Data_Proroga_Contratto) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Straniero_Scadenza_Permesso_Col))
                PowerWebService.GridHeaderFilterFillItems(e);
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
        /// Evento scatenato al cambio pagine della griglia dei collaboratori; utilizzata per segnalare quanto avvenuto lato client.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento</param>
        /// <param name="e">I parametri dell'evento</param>
        protected void gvColSel_OnPageIndexChanged(object sender, EventArgs e)
        {
            ManageGridIndexPageChanged(sender);
        }

        /// <summary>
        /// Evento lato server scatenato al mutamento di selezione nella griglia dei collaboratori
        /// </summary>
        /// <param name="source">Il pannello di callback scatenante</param>
        /// <param name="e">I parametri che arrivano dal pannello di callback scatenante</param>
        protected void gridSelectionChange_OnCallback(object source, CallbackEventArgs e)
        {
            ASPxGridView grid = gvColSel;

            var selctionType = e.Parameter;
            switch (selctionType)
            {
                case "sAll":
                    SelectedColsId = new List<int>();
                    for (int i = 0; i < grid.VisibleRowCount; i++)
                    {
                        var colId = Convert.ToInt32(grid.GetRowValues(i, "Col_Id"));
                        SelectedColsId.Add(colId);
                    }
                    break;
                case "uAll":
                    SelectedColsId = new List<int>();
                    break;
                case "sPage":
                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(grid.GetRowValues(i, "Col_Id"));
                        if (!SelectedColsId.Contains(colId))
                            SelectedColsId.Add(colId);

                    }
                    break;
                case "uPage":
                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(grid.GetRowValues(i, "Col_Id"));
                        if (SelectedColsId.Contains(colId))
                            SelectedColsId.Remove(colId);
                    }
                    break;
                case "sRow":
                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(grid.GetRowValues(i, "Col_Id"));
                        if (grid.Selection.IsRowSelected(i) && !SelectedColsId.Contains(colId))
                            SelectedColsId.Add(colId);
                    }
                    break;
                case "uRow":
                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(grid.GetRowValues(i, "Col_Id"));
                        if (!grid.Selection.IsRowSelected(i) && SelectedColsId.Contains(colId))
                            SelectedColsId.Remove(colId);
                    }
                    break;
            }

            grid.JSProperties["cpVisibleRowCount"] = grid.VisibleRowCount;
        }

        /// <summary>
        /// Evento scatenato alla richiesta di una proprietà js dell'oggetto grilia dei collaboratori;
        /// Riporta per il lato client dell'applicativo il numero di record selezionati e il numero di record contenuti in griglia
        /// </summary>
        /// <param name="sender">Il mittente dell'evento</param>
        /// <param name="e">I parametri dell'evento</param>
        protected void gvColSel_OnCustomJSProperties(object sender, ASPxGridViewClientJSPropertiesEventArgs e)
        {
            SetGridRowsState(sender, e);
        }

        #endregion

        #region Gestione DataGrid Master dei cantieri

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
        protected void gvCantSel_OnCustomJSProperties(object sender, ASPxGridViewClientJSPropertiesEventArgs e)
        {
            SetGridRowsState(sender, e);
        }

        #endregion

        #region Gestion DataGrid Master dei Clienti

        /// <summary>
        /// Evento scatenato al cambio pagine della griglia clienti; utilizzata per segnalare quanto avvenuto lato client.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        protected void gvCli_OnPageIndexChanged(object sender, EventArgs e)
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
        /// Evento scatenato alla richiesta di una proprietà JS dell'oggetto checkbox di selezione dell'intera griglia;
        /// Viene imposta la domanda in lingua da visualizzare nella conferma che sarà richiesta.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CustomJSPropertiesEventArgs"/> instance containing the event data.</param>
        protected void cbAllCli_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            SetSelectAllConfirmationMessage(e);
        }

        /// <summary>
        /// Evento scatenato alla richiesta di una proprietà JS dell'oggetto griglia dei Clienti;
        /// Riporta per il lato client dell'applicativo il numero di record selezionati e il numero di record contenuti in griglia
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gvCliSel_OnCustomJSProperties(object sender, ASPxGridViewClientJSPropertiesEventArgs e)
        {
            SetGridRowsState(sender, e);
        }

        #endregion

        #region Public Methods

        public override void ResetSession()
        {
            base.ResetSession();

            SelectedColsId = new List<int>();
            SelectedCantsId = new List<int>();
            PowerWebContext.SetToSession<List<TimesheetModuleItem>>("tsmItems_" + TimesheetGridView.ID, null);

            // durante il reset della sessione si svuota anche l'elenco delle opzioni selezionate
            SelectedTimesheetOptions = null;
        }

        /// <summary>
        /// Genera un oggetto report specifico a partire dai parametri specificati.
        /// </summary>
        /// <param name="report">I dati a database del report da generare.</param>
        /// <param name="selectedTabs">I tab selezionati per la visualizzazione.</param>
        /// <param name="reportOptions">Le opzioni da applicare al report.</param>
        /// <param name="groups">I raggruppamenti da effettuare.</param>
        /// <param name="items">Gli oggetti da stampare.</param>
        /// <param name="customOptionsPanel">Il pannello delle opzioni custom.</param>
        /// <returns>Il report specifico generato con i dati passati come parametro.</returns>
        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, ASPxPanel customOptionsPanel = null)
        {
            List<TimesheetModuleItem> timesheets = CommonService.ConvertTo<TimesheetModuleItem>(items);
            const string rptTimesheetCol = "RPT_NOME_REPORT_CARTELLINO";

            if (String.Compare(report.Nome_Risorsa, rptTimesheetCol) == 0)
            {
                // si aggiungono alle opzioni report anche le customizzazioni applicative
                var deltaCustomization = (HideDeltaTotalTimehseetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideDeltaTotalTimehseetEnum);
                var planCustomization = (HidePlanTotalTimesheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HidePlanTotalTimesheetEnum);
                var totalCustomization = (HidelTotalTotalTimehsheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HidelTotalTotalTimehsheetEnum);
                var additionalCustomization = (HidelAdditionalTotalTimehsheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HidelAdditionalTotalTimehsheetEnum);
                var justificationCustomization = (HideJustificationTotalTimehsheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideJustificationTotalTimehsheetEnum);
                var strNocCustomization = (HideStrNoctTotalTimehseetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideStrNoctTotalTimehseetEnum);
                var arrotCustomization = (HideArrotTotalTimehseetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideArrotTotalTimehseetEnum);
                var strCustomization = (HideStrTotalTimesheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideStrTotalTimesheetEnum);
                var sectionCustomization = (HideReportSectionTimehsheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideReportSectionTimehsheetEnum);
                var zeroAsEnmptyStringCustomization = (TimesheetExportZeroValueFormatEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheetExportZeroValueFormatEnum);

                int sectionCustomizationOption = 0;
                if (sectionCustomization == HideReportSectionTimehsheetEnum.HideDetail)
                {
                    sectionCustomizationOption = 1;
                }
                else if (sectionCustomization == HideReportSectionTimehsheetEnum.HideRiep)
                {
                    sectionCustomizationOption = 2;
                }

                reportOptions.Add("HideDeltaTotalTimehseetEnum", deltaCustomization == HideDeltaTotalTimehseetEnum.Hide ? 1 : 0);
                reportOptions.Add("HidePlanTotalTimesheetEnum", planCustomization == HidePlanTotalTimesheetEnum.Hide ? 1 : 0);
                reportOptions.Add("HidelTotalTotalTimehsheetEnum", totalCustomization == HidelTotalTotalTimehsheetEnum.Hide ? 1 : 0);
                reportOptions.Add("HidelAdditionalTotalTimehsheetEnum", additionalCustomization == HidelAdditionalTotalTimehsheetEnum.Hide ? 1 : 0);
                reportOptions.Add("TimesheetExportZeroValueFormatEnum", zeroAsEnmptyStringCustomization == TimesheetExportZeroValueFormatEnum.EmptyString ? 1 : 0);
                reportOptions.Add("HideJustificationTotalTimehsheetEnum", justificationCustomization == HideJustificationTotalTimehsheetEnum.Hide ? 1 : 0);
                reportOptions.Add("HideStrNoctTotalTimehseetEnum", strNocCustomization == HideStrNoctTotalTimehseetEnum.Hide ? 1 : 0);
                reportOptions.Add("HideArrotTotalTimehseetEnum", arrotCustomization == HideArrotTotalTimehseetEnum.Hide ? 1 : 0);
                reportOptions.Add("HideStrTotalTimehseetEnum", strCustomization == HideStrTotalTimesheetEnum.Hide ? 1 : 0);
                reportOptions.Add("HideSectionTimehsheetEnum", sectionCustomizationOption);
                reportOptions.Add("ShowWeeklyTotals", ChkShowWeeklyTotals.Checked ? 1 : 0);

                var timesheetColReport = new XRColTimesheet(timesheets, TsTotalController, PowerWebService.ConvertTabPageExtendedToString(selectedTabs), reportOptions);
                return new ExtXtraReport { Report = timesheetColReport, PictureBox = timesheetColReport.CompanyLogo };
            }
            else
                return null;
        }

        /// <summary>
        /// Effettua l'esportazione excel con i dati specificati.
        /// </summary>
        /// <param name="model">Il modello excel da utilizzare.</param>
        /// <param name="items">L'elenco degli elementi da esportare.</param>
        public void ExportXLSX(Tab_Excel_Model model, List<object> items)
        {
            // calcolo del tipo di export da utilizzare
            Type currentType = Type.GetType(String.Format("{0}, Exports", model.Nome_Specializzato));

            // se si sta processando un tipo di export custom
            if (currentType.IsSubclassOf(typeof(ExcelToolbox<TimesheetModuleItem>)))
            {
                // allora si genera l'oggetto specifico con dati di modello, periodo e custom
                System.Runtime.Remoting.ObjectHandle exportHandle = Activator.CreateInstance("Exports", model.Nome_Specializzato);
                var exportToProcess = (IExportExcelCustom<TimesheetModuleItem>)exportHandle.Unwrap();

                // calcolo il percorso del modello da esporatare
                var modelFile = new FileInfo(HttpContext.Current.Server.MapPath(String.Format("{0}{1}", Common.Properties.Settings.Default.ExcelModelsPath, BusinessService.GetLocalizedString(model.ModelFilePath))));
                exportToProcess.ExcelModelFilePath = modelFile.FullName;

                // impostazione del periodo di ricerca
                exportToProcess.ExportPeriod = deTimesheet.Date;

                // impostazione dell'utilizzo di altri parametri
                exportToProcess.UseCalculationType = false;
                exportToProcess.UseDurationTollerance = false;
                exportToProcess.UseEUTollerance = false;
                exportToProcess.UseHourType = false;
                exportToProcess.UseExportDetail = false;

                // se si sta trattando l'export semplice del cartellino si passa anche il total controller
                //if (exportToProcess is ExportTimesheetSimple)
                //((ExportTimesheetSimple)exportToProcess).TsTotalController = TsTotalController;

                // lancio dell'export
                exportToProcess.LaunchExport(TsmItems.AsQueryable());

                // chiusura del pannello di caricamento
                BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = true;

                // esportazione in risposta del foglio excel generato
                ((ExcelToolbox<TimesheetModuleItem>)exportToProcess).ExcelWorkbookSaveToResponse(HttpContext.Current.Response, Path.GetFileName(exportToProcess.ExcelModelFilePath), true);

                // dispose del foglio excel generato
                ((ExcelToolbox<TimesheetModuleItem>)exportToProcess).ExcelWorkbookDispose();
            }

        }

        

        #endregion

        #region Protected Methods

        /// <summary>
        /// Handles the Click event of the btnTSMPrintXlsx control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnTSMPrintXlsx_Click(object sender, EventArgs e)
        {
            ASPxGridViewExporter gvExporter = new ASPxGridViewExporter();
            try
            {
                ReloadTimesheetDayCaption(PowerWebContext.GetFromSession<DateTime>("tsmMinDate_" + TimesheetGridView.ID));
                ManageWeeklyTotalPosition(PowerWebContext.GetFromSession<DateTime>("tsmMinDate_" + TimesheetGridView.ID));
            }
            catch (Exception)
            {
                // silenziamento errore in caso di mancata valorizzazione della data
            }
            gvExporter.GridViewID = TimesheetGridView.ID;

            // mi abbono all'evento di personalizzazione del formato dei dati esportati
            gvExporter.RenderBrick += TimesheetGridViewExporter_OnRenderBrick;

            var minDate = PowerWebContext.GetFromSession<DateTime>("tsmMinDate_" + TimesheetGridView.ID);
            var mese = CommonService.GetMonthName(minDate);

            Page.Controls.Add(gvExporter);
            gvExporter.WriteXlsxToResponse(String.Format("{0} {1}", mese, minDate.Year));
        }

        /// <summary>
        /// Handles the OnRenderBrick event of the TimesheetGridViewExporter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxGridViewExportRenderingEventArgs"/> instance containing the event data.</param>
        private void TimesheetGridViewExporter_OnRenderBrick(object sender, ASPxGridViewExportRenderingEventArgs e)
        {
            // recupero la personalizzazione relativa al formato di export degli zeri
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheetExportZeroValueFormatEnum);

            // se è richiesto di personalizzare la visualizzazione degli zeri
            if (customizationVersion != (int)TimesheetExportZeroValueFormatEnum.Normal)
            {
                // recupero la colonna che si sta processando
                var dc = e.Column as GridViewDataColumn;

                // se la colonna che si sta processando è un giorno
                if (e.RowType == GridViewRowType.Data && dc != null && dc.FieldName.StartsWith("Day"))
                {
                    // se il valore del giorno è 0
                    if (Convert.ToInt32(e.Value) == 0)
                    {
                        // se devo visualizzare il valore delle ore del giorno zero come stringa vuota, cambio il valore di export
                        if (customizationVersion == (int)TimesheetExportZeroValueFormatEnum.EmptyString)
                            e.TextValue = String.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnTSMPrintPdf control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnTSMPrintPdf_Click(object sender, EventArgs e)
        {
            ASPxGridViewExporter gvExporter = new ASPxGridViewExporter();
            gvExporter.GridViewID = TimesheetGridView.ID;

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

            Page.Controls.Add(gvExporter);

        }

        /// <summary>
        /// Callback scatenato al cambio di selezione della combo tipo export.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="callbackEventArgsBase">instance containing the event data.</param>
        protected void CbpEntityTypeSelector_OnCallback(object source, CallbackEventArgsBase callbackEventArgsBase)
        {
            // imposto come proprietà del callback panel il valore dell'entità selezionata
            if (!CbpEntityTypeSelector.JSProperties.ContainsKey("cpSelectedEntity"))
                CbpEntityTypeSelector.JSProperties.Add("cpSelectedEntity", String.Empty);

            // scrivo in sessione l'entità di selezione primaria indicata dal combobox
            CurrentFirstEntity = cmbEntityType.Value.ToString();

            CbpEntityTypeSelector.JSProperties["cpSelectedEntity"] = CurrentFirstEntity;

            // al cambio dell'export azzero i dati selezionati
            ResetSession();

            // ricalcolo dei dati della griglia master
            BindMasterGrid(true);
        }

        /// <summary>
        /// Handles the RenderBrick event of the gvExporter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxGridViewExportRenderingEventArgs"/> instance containing the event data.</param>
        protected void gvExporter_RenderBrick(object sender, ASPxGridViewExportRenderingEventArgs e)
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

        /// <summary>
        /// Evento lato server scatenato al mutamento di selezione nella griglia dei collaboratori
        /// </summary>
        /// <param name="source">Il pannello di callback scatenante</param>
        /// <param name="e">I parametri che arrivano dal pannello di callback scatenante</param>
        protected void gridMasterSelectionChange_OnCallback(object source, CallbackEventArgs e)
        {
            string sourceName = e.Parameter.Split('#')[1];

            var selectedRowsCount = Convert.ToInt32(e.Parameter.Split('#').Last());

            ASPxGridView grid = sourceName == "Col" ? gvColSel : gvCantSel;

            // calcolo della chiave in base alla griglia origine
            var keyFieldName = sourceName == "Col" ? "Col_Id" : "Cant_Id";

            var selctionType = e.Parameter.Split('#').First();
            switch (selctionType)
            {
                case "sAll":
                    if (sourceName == "Col")
                        SelectedColsId = new List<int>();
                    else
                        SelectedCantsId = new List<int>();
                    for (int i = 0; i < grid.VisibleRowCount; i++)
                    {
                        var entityId = Convert.ToInt32(grid.GetRowValues(i, keyFieldName));
                        if (sourceName == "Col")
                            SelectedColsId.Add(entityId);
                        else
                            SelectedCantsId.Add(entityId);
                    }
                    break;
                case "uAll":
                    if (sourceName == "Col")
                        SelectedColsId = new List<int>();
                    else
                        SelectedCantsId = new List<int>();
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
                    }
                    break;
            }

            grid.JSProperties["cpVisibleRowCount"] = grid.VisibleRowCount;

            // se si sta processando la griglia master si aggiorna anche il numero di selezioni effettuate dalla stessa (per controlli lato client)
            if (IsMasterGrid(grid.ID))
                SetMasterGridSelectedCount(selectedRowsCount);


        }

        /// <summary>
        /// Handles the OnCustomCallback event of the gvColSel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxGridViewCustomCallbackEventArgs"/> instance containing the event data.</param>
        protected void gvColSel_OnCustomCallback(object sender, ASPxGridViewCustomCallbackEventArgs e)
        {
            BindColGrid(true);
        }

        /// <summary>
        /// Handles the OnCustomCallback event of the gvCantSel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxGridViewCustomCallbackEventArgs"/> instance containing the event data.</param>
        protected void gvCantSel_OnCustomCallback(object sender, ASPxGridViewCustomCallbackEventArgs e)
        {
            BindCantGrid(true);
        }

        /// <summary>
        /// Handles the OnInit event of the TsOptionsCombobox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void TsOptionsCombobox_OnInit(object sender, EventArgs e)
        {
            var listboxControl = ((ASPxDropDownEdit)sender).FindControl("LbOptionsListBox") as ASPxListBox;
            PopulateTimesheetOptionsListbox(listboxControl);
        }

        /// <summary>
        /// Eseguito all'evento di callback del pannello contenente le griglie del cartellino.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento.</param>
        /// <param name="e">I parametri dell'evento.</param>
        protected void PnlTimesheetGrids_OnCallback(object sender, CallbackEventArgsBase e)
        {
            // impostazione dello stato della proprietà che gestisce il tipo di totali da visualizzare
            ShowWeeklyTotals = ChkShowWeeklyTotals.Checked;

            // aggiornamento della visualizzazione della griglia cartellino in base ai totali
            ManageTimesheetGridsVisibility();

            // aggiornamento del combobox delle viste salvate
            BindTsmLayoutCombo();
        }

        #endregion

        #region Private Methods

        #region Bind dei dati presenti in griglia

        /// <summary>
        /// Effettua il data bind della griglia dei cantieri
        /// </summary>
        /// <param name="isToRefresh"><c>true</c> se deve essere effetuato anche il refresh oltre che l'impostazione del data source.</param>
        /// <param name="emptyDataSource"><c>true</c> se deve essere impostato forzatamente un datasource vuoto per la griglia; altrimenti <c>false</c></param>
        private void BindCantGrid(bool isToRefresh = false, bool emptyDataSource = false)
        {
            gvCantSel.KeyFieldName = CantKeyFieldName;
            gvCantSel.DataSource = emptyDataSource ? new List<Cant>() : Cants;
            if ((!Page.IsPostBack && !Page.IsCallback) || isToRefresh)
                gvCantSel.DataBind();
        }

        /// <summary>
        /// Effettua il bind della griglia dei collaboratori.
        /// </summary>
        /// <param name="isToRefresh"><c>true</c> se deve essere effetuato anche il refresh oltre che l'impostazione del data source.</param>
        /// <param name="emptyDataSource"><c>true</c> se deve essere impostato forzatamente un datasource vuoto per la griglia; altrimenti <c>false</c></param>
        private void BindColGrid(bool isToRefresh = false, bool emptyDataSource = false)
        {
            gvColSel.KeyFieldName = ColKeyFieldName;
            gvColSel.DataSource = emptyDataSource ? new List<Col>() : Cols;
            if ((!Page.IsPostBack && !Page.IsCallback) || isToRefresh)
                gvColSel.DataBind();
        }

        /// <summary>
        /// Effettua il bind della griglia del cartellino.
        /// </summary>
        /// <param name="isToRefresh"><c>true</c> se deve essere effetuato anche il refresh oltre che l'impostazione del data source.</param>
        private void BindTimesheetGrid(bool isToRefresh = false)
        {
            var groups = TimesheetGridView.GroupSummary.GroupBy(gr => gr.Tag);

            foreach (var group in groups)
                LocalizeTimesheetGrid(group.ToList(), group.Key);

            LocalizeTimesheetGridCaption();

            try
            {
                ReloadTimesheetDayCaption(PowerWebContext.GetFromSession<DateTime>("tsmMinDate_" + TimesheetGridView.ID));
                ManageWeeklyTotalPosition(PowerWebContext.GetFromSession<DateTime>("tsmMinDate_" + TimesheetGridView.ID));
            }
            catch (Exception)
            {
                // silenziamento errore in caso di mancata valorizzazione della data
            }


            //Imposta Parametri del CARTELLINO
            TimesheetGridView.Settings.HorizontalScrollBarMode = ScrollBarMode.Auto;
            TimesheetGridView.SettingsEditing.Mode = GridViewEditingMode.Inline;
            TimesheetGridView.Settings.ShowGroupFooter = GridViewGroupFooterMode.VisibleAlways;
            TimesheetGridView.Settings.ShowFilterBar = GridViewStatusBarMode.Hidden;
            TimesheetGridView.SettingsPager.PageSize = RepoManager.ParamRepo.ParametersRow.RowsPerPage;
            TimesheetGridView.SettingsBehavior.EnableCustomizationWindow = true;
            TimesheetGridView.ClientSideEvents.CustomizationWindowCloseUp = "grid_OnCustomizationWindowCloseUp";
            TimesheetGridView.SettingsBehavior.ColumnResizeMode = ColumnResizeMode.Control;
            TimesheetGridView.Settings.ShowGroupPanel = true;

            TimesheetGridView.KeyFieldName = Timesheetkeyfieldname;
            TimesheetGridView.DataSource = TsmItems;
            if (!Page.IsPostBack && !Page.IsCallback || isToRefresh)
                TimesheetGridView.DataBind();
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
                case CantEntityType:
                    BindCantGrid(refreshPrimary);
                    break;
                case ColEntityType:
                    BindColGrid(refreshPrimary);
                    break;
            }
        }

        #endregion

        #region Texts Localization

        /// <summary>
        /// Imposta in lingua le caption dei forms layout presenti nel modulo.
        /// </summary>
        private void LocalizeFormsLayout()
        {
            var item = flMasterSelector.Items[0] as LayoutGroup;
            item.Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_SELEZIONE_DATI_DA_VISUALIZZARE);

            item = flRegCorrection.Items[0] as LayoutGroup;
            item.Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_RETTIFICHE_A_REGISTRAZIONI);
        }

        /// <summary>
        /// Metodo utilizzato per la localizzazione delle stringhe del pannello di gestione delle rettifiche
        /// </summary>
        private void LocalizeCorrectionLayout()
        {
            // localizzazione del pannello di gestione delle rettifiche
            lblUpDeltaThreshold.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA_INFERIORE_DI);
            lblDownDeltaThreshold.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA_SUPERIORE_DI);
            flRegCorrection.Items[0].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_LAYOUTGROUP_INSERIMENTO_RETTIFICHE);

        }

        /// <summary>
        /// Imposta in lingua gli elementi che riguardano la selezione degli elementi (cantieri/collaboratori), da visualizzare in griglia
        /// </summary>
        private void LocalizeSelectionLabels()
        {
            lblEntityType.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_ANAGRAFICA_DA_RICERCARE);
        }

        /// <summary>
        /// Imposta le testate delle colonne della griglia del cartellino in lingua
        /// </summary>
        private void LocalizeTimesheetGridCaption()
        {

            TimesheetGridView.Columns[0].Caption = " ";
            TimesheetGridView.Columns[3].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_CANT_MNEMONIC);
            TimesheetGridView.Columns[4].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_CANT_DESC);
            TimesheetGridView.Columns[5].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_COL_MNEMONIC);
            TimesheetGridView.Columns[6].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_COL_DESC);
            TimesheetGridView.Columns[7].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_MOTIVAZIONE);

            // questo gruppo di colonne da localizzare viene puntato per nome in quanto differiscono in posizione tra la griglia con i totali solo mensili
            // e la griglia con i totali anche settimanali
            TimesheetGridView.Columns["LastMonthlyHours"].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_RIPORTO_ORE_PRECEDENTI);
            TimesheetGridView.Columns["TotalHours"].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_ORE_PERIODO);
            TimesheetGridView.Columns["CurrentMonthlyHours"].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_ORE_DA_RIPORTARE);
            TimesheetGridView.Columns["TotalDays"].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_GIORNI_PERIODO);
            TimesheetGridView.Columns["CantDesc"].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_CANT_DESC);
            if (ChkShowWeeklyTotals.Checked)
            {
                TimesheetGridView.Columns["TotalWeek1"].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_SETTIMANA);
                TimesheetGridView.Columns["TotalWeek2"].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_SETTIMANA);
                TimesheetGridView.Columns["TotalWeek3"].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_SETTIMANA);
                TimesheetGridView.Columns["TotalWeek4"].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_SETTIMANA);
                TimesheetGridView.Columns["TotalWeek5"].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_SETTIMANA);
                TimesheetGridView.Columns["TotalWeek6"].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE_SETTIMANA);
            }
        }

        /// <summary>
        /// Imposta in lingua le etichette dei totali visualizzati sul cartellino.
        /// </summary>
        /// <param name="group">La lista di item da processare per la localizzazione.</param>
        /// <param name="tag">Il tag da tradurre.</param>
        private void LocalizeTimesheetGrid(List<ASPxSummaryItem> group, String tag)
        {
            String currentTag = tag;

            if (tag == StrSummaryDefaultTag)
                currentTag = BusinessService.GetLocalizedString(PowerWebResources.LBL_STRAORDINARI);
            else if (tag == StrNotSummaryDefaultTag)
                currentTag = BusinessService.GetLocalizedString(PowerWebResources.LBL_STRAORDINARI_NOTTURNI);
            else if (tag == TotalSummaryDefaultTag)
                currentTag = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE);
            else if (tag == DeltaSummaryDefaultTag)
                currentTag = BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA);
            else if (tag == PlanSummaryDefaultTag || tag == PianoSummaryDefaultTag)
                currentTag = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_TOT);
            else if (tag == OrdinarySummaryDefaultTag)
                currentTag = BusinessService.GetLocalizedString(PowerWebResources.LBL_ORD);
            else if (tag == JustificationSummaryDefaultTag)
                currentTag = BusinessService.GetLocalizedString(PowerWebResources.LBL_JUST);

            foreach (var item in group)
            {
                item.Tag = currentTag;
            }
        }

        /// <summary>
        /// Gestisce la messa in lingua degli elementi in testata delle griglie di visualizzazione del cartellino.
        /// </summary>
        private void LocalizeTimesheetGridsHeader()
        {
            ChkShowWeeklyTotals.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_VISUALIZZA_TOTALI_PER_SETTIMANA);
            lblPeriodo.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_PERIODO);
        }

        #endregion

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
        /// Metodo utilizzato per l'inizializzazione dei campi di gestione inserimento rettifica
        /// </summary>
        private void InitializeCorrectionLayout()
        {
            string minParamValueStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.UseCorrectionEnum, "DefaultMinValue");
            string maxParamValueStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.UseCorrectionEnum, "DefaultMaxValue");
            int minParamValue = 0;
            Int32.TryParse(minParamValueStr, out minParamValue);
            int maxParamValue = 0;
            Int32.TryParse(maxParamValueStr, out maxParamValue);
            seUpDeltaThreshold.Value = maxParamValue;
            seDownDeltaThreshold.Value = minParamValue;
        }

        /// <summary>
        /// Effettua il bind del combobox di selezione della vista per il cartellino.
        /// </summary>
        /// <param name="isSetToDefault">Se impostato a <c>true</c> viene anche impostato il valore di default.</param>
        private void BindTsmLayoutCombo(bool isSetToDefault = false)
        {
            cmbTSMLayout.Items.Clear();
            cmbTSMLayout.Items.Add(new ListEditItem("Default", -1));
            cmbTSMLayout.SelectedIndex = 0;


            List<Tab_DataGrid> listLayout = RepoManager.Tab_DataGridRepo.Find(tdg => tdg.Nome_DataGrid == TimesheetGridView.ID && (tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id || tdg.Utenti_Id == null)).OrderBy(tgd => tgd.Nome_Layout).ToList();

            Tab_DataGrid currentLayout = null;

            foreach (Tab_DataGrid item in listLayout)
            {
                if (item.Utenti_Id == null && currentLayout == null)
                    currentLayout = item;

                if (item.Utenti_Id != null && currentLayout != null &&
                    item.Data_Layout_DataGrid > currentLayout.Data_Layout_DataGrid)
                    currentLayout = item;

                if (item.Utenti_Id != null && currentLayout == null)
                    currentLayout = item;

                ListEditItem newListEditItem = new ListEditItem(item.Nome_Layout, item.DataGrid_Id);
                cmbTSMLayout.Items.Add(newListEditItem);
                if (item.Utenti_Id != null)
                    newListEditItem.ImageUrl = CommonService.BaseSiteUrl + "Icons/User/User.png";
            }

            cmbTSMLayout.DataBind();

            if ((!Page.IsPostBack && !Page.IsCallback) || isSetToDefault)
            {
                if (currentLayout != null)
                {
                    TimesheetGridView.LoadClientLayout(currentLayout.Layout_DataGrid);
                    cmbTSMLayout.SelectedIndex = listLayout.IndexOf(currentLayout) + 1;
                }
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
                case CantEntityType:
                    isMasterGrid = gridId == gvCantSel.ID;
                    break;
                case ColEntityType:
                    isMasterGrid = gridId == gvColSel.ID;
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
        /// Popola un oggetto orario da cartellino a partire da un dizionario ordinato di valori presi in griglia.
        /// </summary>
        /// <param name="timesheet">L'orario da popolare</param>
        /// <param name="newValues">I valori da griglia utilizzati per il popolamento</param>
        /// <param name="hasWeeklyTotals">Indica se il timesheet che si desidera generare ha i totali per settimana o meno.</param>
        private void PopulateTimesheetFromNewValue(TimesheetModuleItem timesheet, OrderedDictionary newValues, bool hasWeeklyTotals)
        {
            // popolamento dei valori

            timesheet.ColId = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => timesheet.ColId)]);
            timesheet.CantId = Convert.ToInt32(newValues[CommonService.GetPropertyName(() => timesheet.CantId)]);
            timesheet.StartDate = (DateTime)deTimesheet.Value;
            timesheet.Day01Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day01)]);
            timesheet.Day02Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day02)]);
            timesheet.Day03Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day03)]);
            timesheet.Day04Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day04)]);
            timesheet.Day05Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day05)]);
            timesheet.Day06Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day06)]);
            timesheet.Day07Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day07)]);
            timesheet.Day08Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day08)]);
            timesheet.Day09Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day09)]);
            timesheet.Day10Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day10)]);
            timesheet.Day11Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day11)]);
            timesheet.Day12Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day12)]);
            timesheet.Day13Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day13)]);
            timesheet.Day14Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day14)]);
            timesheet.Day15Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day15)]);
            timesheet.Day16Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day16)]);
            timesheet.Day17Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day17)]);
            timesheet.Day18Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day18)]);
            timesheet.Day19Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day19)]);
            timesheet.Day20Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day20)]);
            timesheet.Day21Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day21)]);
            timesheet.Day22Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day22)]);
            timesheet.Day23Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day23)]);
            timesheet.Day24Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day24)]);
            timesheet.Day25Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day25)]);
            timesheet.Day26Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day26)]);
            timesheet.Day27Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day27)]);
            timesheet.Day28Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day28)]);
            timesheet.Day29Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day29)]);
            timesheet.Day30Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day30)]);
            timesheet.Day31Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.Day31)]);

            // se è richiesto di gestire i totali per settimana si procede
            // a gestire anche la chiusura della settimana per inizio/fine mese
            if (hasWeeklyTotals)
            {
                // giorni di chiusura settimana per inizio mese
                timesheet.DayMinus1Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayMinus1)]);
                timesheet.DayMinus2Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayMinus2)]);
                timesheet.DayMinus3Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayMinus3)]);
                timesheet.DayMinus4Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayMinus4)]);
                timesheet.DayMinus5Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayMinus5)]);
                timesheet.DayMinus6Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayMinus6)]);
                timesheet.DayMinus7Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayMinus7)]);

                // giorni di chiusura settimana per fine mese
                timesheet.DayPlus1Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayPlus1)]);
                timesheet.DayPlus2Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayPlus2)]);
                timesheet.DayPlus3Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayPlus3)]);
                timesheet.DayPlus4Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayPlus4)]);
                timesheet.DayPlus5Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayPlus5)]);
                timesheet.DayPlus6Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayPlus6)]);
                timesheet.DayPlus7Edit = Convert.ToDouble(newValues[CommonService.GetPropertyName(() => timesheet.DayPlus7)]);
            }
        }

        /// <summary>
        /// Popola i valori del listbox passato come parametro con le opzioni configurate nella tabella TIMESHEET_OPTIONS nella TAB_DECOD.
        /// </summary>
        /// <param name="listboxToPopulate">Il listbox da popolare.</param>
        private void PopulateTimesheetOptionsListbox(ASPxListBox listboxToPopulate)
        {
            // recupero la tabella dalla tab_decod da popolare
            var timesheetOptions = RepoManager.Tab_DecodRepo.Find(td => td.Nome_Tab == "TIMESHEET_OPTIONS").Select(td => new ListEditItem(BusinessService.GetLocalizedString(td.Decodifica_Tab), td.Chiave_Tab)).AsQueryable();

            // svuoto i valori attualmente presenti nel listbox
            listboxToPopulate.Items.Clear();

            // sono sono stati trovati delle opzioni le aggiungo al listbox
            if (timesheetOptions.Any())
                listboxToPopulate.Items.AddRange(timesheetOptions.ToList());

            // in ogni caso effettuo il bind degli items così da aggiornare la visualizzazione
            listboxToPopulate.DataBindItems();

        }

        /// <summary>
        /// Gestisce la visiblità delle griglie del cartellino in base al tipo di totale selezionato.
        /// </summary>
        private void ManageTimesheetGridsVisibility()
        {
            PnlTimesheetMonthlyTotals.ClientVisible = !ShowWeeklyTotals;
            PnlTimesheetWithWeeklyTotals.ClientVisible = ShowWeeklyTotals;
        }

        #endregion

        #region Gestione TimeSheet

        /// <summary>
        /// Handles the CustomCallback event of the TimesheetGridView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxGridViewCustomCallbackEventArgs"/> instance containing the event data.</param>
        protected void TimesheetGridView_CustomCallback(object sender, ASPxGridViewCustomCallbackEventArgs e)
        {
            DateTime selectedDate = DateTime.MinValue;

            // reucpero l'elenco dei parametri passati al callback con l'elenco delle opzioni valorizzate
            // dall'utente e spezzo la stringa sul ; di modo da ottenere un lista di selezioni e piazzo il dato
            // nella variabile che le conterrà per l'elaborazione
            var optionsString = e.Parameters.Substring(e.Parameters.IndexOf('@') + 1);
            SelectedTimesheetOptions = optionsString == String.Empty ? new List<string>() : optionsString.Split(';').ToList();


            if (DateTime.TryParse(e.Parameters.Substring(0, e.Parameters.IndexOf('#')), out selectedDate))
            {
                int sharpPosition = e.Parameters.IndexOf('#') + 1;
                int atPosition = e.Parameters.IndexOf('@');
                LoadTimesheetDataSource(selectedDate, e.Parameters.Substring(sharpPosition, atPosition - sharpPosition));
            }

            var currentGrid = sender as ASPxGridView;

            bool noShowPlan = SelectedTimesheetOptions.Any(opz => opz == NoShowPlanOptionValue);
            bool noShowDelta = SelectedTimesheetOptions.Any(opz => opz == NoShowDeltaOptionValue);
            bool noShowMot = SelectedTimesheetOptions.Any(opz => opz == NoShowMotOptionValue);
            bool noShowArrot = SelectedTimesheetOptions.Any(opz => opz == NoShowArrotOptionValue);

            // costruzione dell'elenco dei group summary da togliere
            var summaryToDelete = new List<ASPxSummaryItem>();

            // ciclo su tutti i group summary presenti in griglia e selezione degli elementi da cancellare
            currentGrid.GroupSummary.ForEach(gs =>
            {
                // se sto elaborando la colonna delta e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                if (gs.Tag == DeltaSummaryDefaultTag && noShowDelta)
                    summaryToDelete.Add(gs);
                else if ((gs.Tag == PlanSummaryDefaultTag || gs.Tag == PianoSummaryDefaultTag) && noShowPlan) // se sto elaborando la colonna piano e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                    summaryToDelete.Add(gs);
                else if (gs.Tag == JustificationSummaryDefaultTag && noShowMot) // se sto elaborando una la colonna motivazioni e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                    summaryToDelete.Add(gs);
                else if (gs.Tag == ArrotSummaryDefaultTag && noShowArrot) // se sto elaborando la colonna arrotondamenti e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                    summaryToDelete.Add(gs);
            });

            // sono tolte dall'elenco dei group summary tutte le colonne marcate per la cancellazione dal ciclo precedente
            //!!!!!LENTISSIMO!!!!!!
            foreach (var std in summaryToDelete)
            {
                currentGrid.GroupSummary.Remove(std);
            }
            //summaryToDelete.ForEach(gs => currentGrid.GroupSummary.Remove(gs));
            //!!!!!LENTISSIMO!!!!!!

        }

        /// <summary>
        /// Ricalcola il contenuto della griglia timesheet per il mese della data passata come parametro.
        /// Questo metodo effettua anche il bind sulla griglia del contenuto calcolato.
        /// </summary>
        /// <param name="dateToProcess">La data da cui estrarre il mese da processare</param>
        /// <param name="entityName">L'entità da processare per la visualizzazione  (collaboratore o cantiere)</param>
        private void LoadTimesheetDataSource(DateTime dateToProcess, string entityName)
        {
            // recupero le opzioni selezionate dall'utente per il cartellino in produzione
            bool isDecimalHours = SelectedTimesheetOptions.Any(opz => opz == ShowDecimalHoursOptionValue);
            bool showOnlyPlan = SelectedTimesheetOptions.Any(opz => opz == ShowOnlyPlanOptionValue);
            bool isByOtherEntity = SelectedTimesheetOptions.Any(opz => opz == DividePlanForOtherEntityOptionValue);
            bool usaFisiche = SelectedTimesheetOptions.Any(opz => opz == UsaFisicheOptionValue);

            PowerWebContext.SetToSession<List<TimesheetModuleItem>>("tsmItems_" + TimesheetGridView.ID, null);

            PowerWebContext.SetToSession<List<TimesheetModuleItem>>("tsmItems_" + TimesheetGridView.ID,
                entityName == ColEntityType
                ? TimesheetModuleItem.GenerateTimeSheet(dateToProcess, isDecimalHours, isByOtherEntity, "tsmMinDate_" + TimesheetGridView.ID, showOnlyPlan, SelectedColsId,
                                                        referenceEntity: entityName, timesheetOptions: SelectedTimesheetOptions, hasWeeklyTotals: ChkShowWeeklyTotals.Checked, usaFisiche: usaFisiche)
                : TimesheetModuleItem.GenerateTimeSheet(dateToProcess, isDecimalHours, isByOtherEntity, "tsmMinDate_" + TimesheetGridView.ID, showOnlyPlan, SelectedCantsId,
                                                        referenceEntity: entityName, timesheetOptions: SelectedTimesheetOptions, hasWeeklyTotals: ChkShowWeeklyTotals.Checked, usaFisiche: usaFisiche));

            // ogni volta che si generano i nuovi dati del timesheet allora si aggiorna anche la gestione del totale
            PowerWebContext.SetToSession<TimesheetTotalController>("tsmTotalController_" + TimesheetGridView.ID, new TimesheetTotalController(PowerWebContext.GetFromSession<List<TimesheetModuleItem>>("tsmItems_" + TimesheetGridView.ID), isDecimalHours, entityName));

            BindTimesheetGrid(true);
        }

        /// <summary>
        /// Metodo che, dato il giorno da visualizzare nel cartellino, ridefinisce tutti i titoli delle colonne giorno di modo
        /// da inserire anche il weekday
        /// </summary>
        /// <param name="dateToProcess">La data selezionata per la visualizzazione del cartellino</param>
        private void ReloadTimesheetDayCaption(DateTime dateToProcess)
        {
            // recupero la data di inizio e fine mese visualizzato e le date di inizio e fine mese in elaborazione
            DateTime firstMonthDate = CommonService.GetFirstMonthDay(dateToProcess);
            DateTime startDate = firstMonthDate;
            DateTime lastMonthDate = CommonService.GetLastMonthDay(dateToProcess);
            DateTime endDate = lastMonthDate;

            /* se è richiesto di impostare i dati per un timesheet che prevede totali settimanali si procede, se necessario a chiudere la settimana di inizio e di fine */

            // se è richiesto il piano per la gestione di orari settimanali e la data di inizio non è un lunedì
            // allora si modifica la data di inizio periodo l'ultimo lunedì del mese precedente
            if (ChkShowWeeklyTotals.Checked && startDate.DayOfWeek != DayOfWeek.Monday)
                startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);

            // se è richiesto il piano per la gestione degli orari settimanali ela data di fine mese non è una domenica
            // allora si recupera come data di fine periodo la prima domenica del mese successivo
            if (ChkShowWeeklyTotals.Checked && endDate.DayOfWeek != DayOfWeek.Sunday)
                endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

            // ciclo di elaborazione di tutte le colonne della grigli
            for (int i = 0; i < TimesheetGridView.Columns.Count; i++)
            {
                try
                {
                    // calcolo del nome del campo che si sta processando
                    string fieldName = ((GridViewDataColumn)TimesheetGridView.Columns[i]).FieldName;

                    // se la colonna in elaborazione è un giorno
                    if (fieldName.StartsWith("Day"))
                    {

                        // se si sta processando un giorno anteriore al mese in elaborazione
                        if (fieldName.Contains("Minus"))
                        {

                            #region Gestione dei giorni antecedenti al mese oggetto del cartellino

                            // si recupera il numero del giorno dall'ultimo carattere del nome del campo
                            string dayNumberStr = fieldName.Substring(fieldName.Length - 1, 1);

                            // calcolo del numero del giorno di riferimento
                            int dayNumber = 0;
                            Int32.TryParse(dayNumberStr, out dayNumber);

                            // se il numero è stato compreso
                            if (dayNumber != 0)
                            {
                                try
                                {
                                    // si sottrae all'inizio mese il numero di giorni recuperati
                                    DateTime currentDay = firstMonthDate.AddDays(-1 * dayNumber);

                                    // si procede alla scrittura del dato solamente se la data in processo è nel range oggetto 
                                    // del cartellino; altrimenti la caption rimane vuota
                                    if (currentDay >= startDate)
                                    {
                                        // la caption della colonna è data dal formato in stringa corto del giorno che contenga anche il nome del mese
                                        string dayString = CommonService.GetDayShortName(currentDay);
                                        string monthString = CommonService.GetMonthShortName(currentDay);
                                        TimesheetGridView.Columns[i].Caption = String.Format("{0} {1} {2}", currentDay.Day.ToString("00"), monthString, dayString);
                                    }
                                    else
                                        TimesheetGridView.Columns[i].Caption = " ";
                                }
                                catch (Exception)
                                {
                                    // in caso d'errore (probabile min value per calcolo prima di generazione cartellino)
                                    // si riporta la stringa vuota
                                    TimesheetGridView.Columns[i].Caption = " ";
                                }

                            }
                            else // altrimenti si visualizza una caption spazio
                                TimesheetGridView.Columns[i].Caption = " ";

                            #endregion

                        }
                        else if (fieldName.Contains("Plus")) // altrimenti, se il giorno è successivo al mese in elaborazione
                        {

                            #region Gestione dei giorni successivi al mese oggetto del cartellino

                            // se la data di inzio mese non è la data minima (è stato almeno una volta generato un cartellino)
                            if (firstMonthDate != DateTime.MinValue)
                            {
                                // si recupera il numero del giorno dall'ultimo carattere del nome del campo
                                string dayNumberStr = fieldName.Substring(fieldName.Length - 1, 1);

                                // calcolo del numero del giorno di riferimento
                                int dayNumber = 0;
                                Int32.TryParse(dayNumberStr, out dayNumber);

                                // se il numero è stato compreso
                                if (dayNumber != 0)
                                {
                                    // si aggiunge al fine mese il numero di giorni recuperati
                                    DateTime currentDay = lastMonthDate.AddDays(dayNumber);

                                    // si procede alla scrittura del dato solamente se la data in processo è nel range oggetto 
                                    // del cartellino; altrimenti la caption rimane vuota
                                    if (currentDay <= endDate)
                                    {
                                        // la caption della colonna è data dal formato in stringa corto del giorno che contenga anche il nome del mese
                                        string dayString = CommonService.GetDayShortName(currentDay);
                                        string monthString = CommonService.GetMonthShortName(currentDay);
                                        TimesheetGridView.Columns[i].Caption = String.Format("{0} {1} {2}", currentDay.Day.ToString("00"), monthString, dayString);
                                    }
                                    else
                                        TimesheetGridView.Columns[i].Caption = " ";
                                }
                                else // altrimenti si visualizza una caption spazio
                                    TimesheetGridView.Columns[i].Caption = " ";
                            }
                            else // se inizio mese è la data minima allora si procede a scrivere lo spazio in quanto non sono calcolabili i giorni precedenti
                                TimesheetGridView.Columns[i].Caption = " ";

                            #endregion

                        }
                        else // altrimenti se il giorno in processo risulta nel mese corrente
                        {

                            #region Gestione dei giorni del mese oggetto del cartellino

                            // si recupera il numero del giorno
                            string dayNumberStr = fieldName.Substring(3);
                            int dayNumber = 0;
                            Int32.TryParse(dayNumberStr, out dayNumber);
                            if (dayNumber != 0)
                            {
                                // se il giorno non è superiore all'ultimo giorno del mese
                                if (dayNumber <= lastMonthDate.Day)
                                {
                                    // si calcola il nome del giorno corrente (nella cultura dell'utente corrente) e si inserisce la nuova caption
                                    var currentDate = new DateTime(firstMonthDate.Year, firstMonthDate.Month, dayNumber);
                                    string dayString = CommonService.GetDayShortName(currentDate);

                                    // se è richiesto di visualizzare i totali anche per settimana è necessario nella caption della colonna aggiungere anche il valore
                                    // del mese (potendo avere potenzialmente valori a cavallo del mese
                                    if (ChkShowWeeklyTotals.Checked)
                                    {
                                        string monthString = CommonService.GetMonthShortName(currentDate);
                                        TimesheetGridView.Columns[i].Caption = String.Format("{0} {1} {2}", dayNumberStr, monthString, dayString);
                                    }
                                    else
                                        TimesheetGridView.Columns[i].Caption = String.Format("{0} - {1}", dayNumberStr, dayString);
                                }
                                else
                                {
                                    // se il giorno cade fuori dal mese la caption della colonna sarà spazio
                                    TimesheetGridView.Columns[i].Caption = " ";
                                }
                            }
                            else // se il giorno non è comprensibile si visualizzerà uno spazio
                                TimesheetGridView.Columns[i].Caption = " ";

                            #endregion

                        }

                        #region Nascondimento e visualizzazione delle colonne giorno

                        // se la caption della colonna è spazio allora è stata marcata per il nascondimento;
                        // altrimenti si procede ad impostare la sua visualizzazione
                        if (TimesheetGridView.Columns[i].Caption == " ")
                            TimesheetGridView.Columns[i].Visible = false;
                        else
                            TimesheetGridView.Columns[i].Visible = true;

                        #endregion
                    }
                }
                catch (Exception)
                {
                    // in caso d'errore di conversione della colonna si silenzia l'errore e si passa alla colonna successiva
                    continue;
                }
            }
        }

        /// <summary>
        /// Gestisce il posizionamento e il nascondimento dei totali per settimana (se presenti e se ne è richiesta la visualizzazione).
        /// </summary>
        /// <param name="dateToProcess">La data da processare che rappresenta il mese.</param>
        private void ManageWeeklyTotalPosition(DateTime dateToProcess)
        {
            // si procede solamente se è richiesto di visualizzare i totali per settimana ed è selezionata una data valida
            if (ChkShowWeeklyTotals.Checked && dateToProcess != DateTime.MinValue)
            {
                // inizializzazione di inizio e fine mese e calcolo degli estremi d'elaborazione
                DateTime startDate = CommonService.GetFirstMonthDay(dateToProcess); ;
                DateTime endDate = CommonService.GetLastMonthDay(startDate);
                if (startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                    startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
                if (endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                    endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                // calcolo l'elenco delle domeniche con relativa posizione nel mese
                IEnumerable<DateTime> sundays = CommonService.GetDatesFromPeriod(startDate, endDate).Where(dt => dt.DayOfWeek == DayOfWeek.Sunday);

                // se sono state trovate delle domeniche
                if (sundays.Any())
                {
                    // costruisco l'elenco delle domeniche con relativo nome colonna
                    var sundaysColumnName = new List<string>();
                    foreach (DateTime sunday in sundays)
                    {
                        string sundayColumnName = String.Empty;
                        if (sunday.Month == dateToProcess.Month)
                            sundayColumnName = String.Format("Day{0}", sunday.Day.ToString("00"));
                        else if (sunday.Month == dateToProcess.AddMonths(-1).Month)
                            sundayColumnName = String.Format("DayMinus{0}", Math.Abs(-1 * sunday.Day));
                        else if (sunday.Month == dateToProcess.AddMonths(1).Month)
                            sundayColumnName = String.Format("DayPlus{0}", sunday.Day);

                        if (!String.IsNullOrEmpty(sundayColumnName))
                            sundaysColumnName.Add(sundayColumnName);
                    }
                    // per ogni colonna totale settimanale si procede alla visualizzazione, nascondimento e spostamento nella posizione corretta
                    TimesheetGridView.Columns["TotalWeek1"].Visible = sundaysColumnName.Count >= 1;
                    TimesheetGridView.Columns["TotalWeek2"].Visible = sundaysColumnName.Count >= 2;
                    TimesheetGridView.Columns["TotalWeek3"].Visible = sundaysColumnName.Count >= 3;
                    TimesheetGridView.Columns["TotalWeek4"].Visible = sundaysColumnName.Count >= 4;
                    TimesheetGridView.Columns["TotalWeek5"].Visible = sundaysColumnName.Count >= 5;
                    TimesheetGridView.Columns["TotalWeek6"].Visible = sundaysColumnName.Count >= 6;

                    if (TimesheetGridView.Columns["TotalWeek1"].Visible)
                        TimesheetGridView.Columns["TotalWeek1"].VisibleIndex = TimesheetGridView.Columns[sundaysColumnName[0]].VisibleIndex + 1;
                    if (TimesheetGridView.Columns["TotalWeek2"].Visible)
                        TimesheetGridView.Columns["TotalWeek2"].VisibleIndex = TimesheetGridView.Columns[sundaysColumnName[1]].VisibleIndex + 1;
                    if (TimesheetGridView.Columns["TotalWeek3"].Visible)
                        TimesheetGridView.Columns["TotalWeek3"].VisibleIndex = TimesheetGridView.Columns[sundaysColumnName[2]].VisibleIndex + 1;
                    if (TimesheetGridView.Columns["TotalWeek4"].Visible)
                        TimesheetGridView.Columns["TotalWeek4"].VisibleIndex = TimesheetGridView.Columns[sundaysColumnName[3]].VisibleIndex + 1;
                    if (TimesheetGridView.Columns["TotalWeek5"].Visible)
                        TimesheetGridView.Columns["TotalWeek5"].VisibleIndex = TimesheetGridView.Columns[sundaysColumnName[4]].VisibleIndex + 1;
                    if (TimesheetGridView.Columns["TotalWeek6"].Visible)
                        TimesheetGridView.Columns["TotalWeek6"].VisibleIndex = TimesheetGridView.Columns[sundaysColumnName[5]].VisibleIndex + 1;
                }
            }

        }

        /// <summary>
        /// Colora la colonna specificata in caso di fine settimana.
        /// </summary>
        /// <param name="caption">La caption dell'header della colonna.</param>
        /// <param name="fieldName">Il nome del campo che si sta processando</param>
        /// <param name="cell">La cella di cui eventualmente cambiare colore.</param>
        private void SetWeekendColor(string caption, string fieldName, TableCell cell)
        {
            try
            {
                DateTime minDate = PowerWebContext.GetFromSession<DateTime>("tsmMinDate_" + TimesheetGridView.ID);

                // se si sta processando un giorno del mese precedente allora si calcola come data di partenza il mese precedente,
                // altrimenti se si sta processando un giorno del mese successivo allora si calcola come data di partenza il mese successivo
                // altrimenti si sta processando il mese corrente e il dato risulta già corrento
                if (fieldName.StartsWith("DayMinus"))
                    minDate = CommonService.GetFirstMonthDay(minDate.AddMonths(-1));
                else if (fieldName.StartsWith("DayPlus"))
                    minDate = CommonService.GetFirstMonthDay(minDate.AddMonths(1));

                int dayValue = Convert.ToInt32(caption.Substring(0, 2));
                minDate = minDate.AddDays(dayValue - 1);
                DayOfWeek DayOfWeek = minDate.DayOfWeek;

                //se si tratta di sabato o domenica vado a colorare lo sfondo
                if (DayOfWeek == DayOfWeek.Saturday || DayOfWeek == DayOfWeek.Sunday)
                    cell.BackColor = RepoManager.ParamRepo.GetColorFromEnum((DayOfWeek)DayOfWeek, true);

            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Attribuisce alle ore piano provenienti dal timesheet il clore definito
        /// </summary>
        /// <param name="caption">Il valore della stringa che compone la tesstata della colonna</param>
        /// <param name="cell">La cella da processare per la colorazione</param>
        /// <param name="isFreeTimeSheet">Indica se il timesheet in elaborazione proviene dalla tabella Col_Orario</param>
        /// <param name="cellValue">Il valore della cella da processare</param>
        private void SetFreeTimesheetColor(string caption, TableCell cell, bool isFreeTimeSheet, object cellValue)
        {
            // se sto processando la colonna che è la motivazione
            if (caption == BusinessService.GetLocalizedString(PowerWebResources.LBL_MOTIVAZIONE))
            {
                // se sto elaborando una giustificazione che corrisponde alle ore piano e viene dalla tabella Col_Orario
                if (cellValue != null)
                {
                    if (cellValue.ToString() == BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN) && isFreeTimeSheet)
                        cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum(TimesheetPlanEnum.FreeTimesheet, false);
                }
            }
        }

        /// <summary>
        /// Attribuisce alle ore delle rettifiche il colore definito
        /// </summary>
        /// <param name="caption">Il valore della stringa che compone la testata della colonna</param>
        /// <param name="cell">La cella da processare per la colorazione</param>
        /// <param name="cellValue">Il valore della cella da processar</param>
        private void SetRettTimesheetColor(string caption, TableCell cell, object cellValue)
        {
            // se sto processando la colonna che è la motivazione
            if (caption == BusinessService.GetLocalizedString(PowerWebResources.LBL_MOTIVAZIONE))
            {
                // se sto elaborando una giustificazione che corrisponde alle ore rettifica
                if (cellValue != null)
                {
                    if (cellValue.ToString() == BusinessService.GetLocalizedString(PowerWebResources.LBL_RETTIFICHE))
                        cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum(TimesheetPlanEnum.CorrectionTimesheet, false);
                }
            }
        }

        protected void TimesheetGridView_HtmlDataCellPrepared(object sender, ASPxGridViewTableDataCellEventArgs e)
        //Gestione Dettaglio Ore x Tipo (Piano/Viaggi/Motivazione) e colorazione ore piano differenti
        {
            if (e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.CantId)] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.CantMnemonic)] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.CantDesc)] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.ColId)] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.ColMnemonic)] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.ColDesc)] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.ID)] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.Justification)] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.Order)] &&
                e.DataColumn != TimesheetGridView.Columns["LastMonthlyHours"] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.TotalHours)] &&
                e.DataColumn != TimesheetGridView.Columns["CurrentMonthlyHours"] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.TotalDays)] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.IsFromFreeTimeSheet)] &&
                e.DataColumn != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.FreeTimeSheetId)] &&
                e.DataColumn.Caption.Trim() != String.Empty)
            {
                SetWeekendColor(e.DataColumn.Caption, e.DataColumn.FieldName, e.Cell);
                var justValue = e.GetValue("Justification") as String;
                if (!String.IsNullOrEmpty(justValue) && justValue.ToString() == BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN))
                {
                    double value = 0.0d;
                    if (e.CellValue != null)
                        double.TryParse(e.CellValue.ToString(), out value);
                }
            }

            #region gestione della colorazione delle ore piano provenienti da Col_Orario

            // calcolo della posizione attuale in elaborazione
            var checkIndex = e.VisibleIndex - TimesheetGridView.VisibleStartIndex;

            // recupero il valore di tipo della registrazione in elaborazione
            var listaIsFromFreeTimesheet = TimesheetGridView.GetCurrentPageRowValues(CommonService.GetPropertyName(() => TsmStub.IsFromFreeTimeSheet));
            bool isFromFreeTimesheet = false;
            if (listaIsFromFreeTimesheet.Count > 0 && checkIndex >= 0 && checkIndex <= listaIsFromFreeTimesheet.Count)
            {
                isFromFreeTimesheet = Convert.ToBoolean(listaIsFromFreeTimesheet[checkIndex]);
            }

            // in ogni caso coloro la colonna della motivazione in base al tipo di orario recuperato
            SetFreeTimesheetColor(e.DataColumn.Caption, e.Cell, isFromFreeTimesheet, e.CellValue);

            // in ogni caso coloro eventualmente la cella motivazione se si stanno processando delle rettifiche
            SetRettTimesheetColor(e.DataColumn.Caption, e.Cell, e.CellValue);

            #endregion
        }

        //TODO: riscrivere per evitare duplicazione del codice
        protected void TimesheetGridView_CustomSummaryCalculate(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var currentItem = e.Item as ASPxSummaryItem;

            if (currentItem.FieldName == "Justification")
            {

                #region Nella colonna della motivazione vanno impostate le stringhe che idenficano il tipo di totale

                if (e.FieldValue != null && e.FieldValue.ToString() == BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN))
                {
                    if (!_justificationsRowsDictionary.ContainsKey(e.RowHandle))
                        _justificationsRowsDictionary.Add(e.RowHandle, e.FieldValue.ToString());
                }

                if (e.SummaryProcess == DevExpress.Data.CustomSummaryProcess.Finalize)
                {
                    e.TotalValue = currentItem.Tag;
                }

                #endregion

            }
            else if (currentItem.FieldName == "ColId")
            {

                #region Nell'elaborazione della colonna collaboratore allora si calcola il totale dei minuti (con monte minuti) per collaboratore

                if (e.FieldValue != null && !String.IsNullOrEmpty(e.FieldValue.ToString()))
                {
                    int currentColdId = Convert.ToInt32(e.FieldValue);

                    if (currentColdId != 0)
                    {
                        if (!_rowHandleColIdMappingDictionary.ContainsKey(e.RowHandle))
                            _rowHandleColIdMappingDictionary.Add(e.RowHandle, currentColdId);

                        if (!_lastMonthlyPerRowHandleDictionary.ContainsKey(e.RowHandle))
                        {
                            if (TsmItems != null && TsmItems.Count > 0)
                            {
                                var currentMonth = TsmItems.GroupBy(tsi => tsi.StartDate).ElementAt(0).Key;

                                Col currentCol = RepoManager.ColRepo.Single(col => col.Col_Id == currentColdId);

                                int totalMinutes = 0;

                                if (RepoManager.ParamRepo.ParametersRow.Abilita_Monte_Minuti &&
                                    (RepoManager.ParamRepo.ParametersRow.MonthlyHoursEnum == MothlyHoursEnum.Inclusive && currentCol.Flag_Monte_Ore)
                                    || (RepoManager.ParamRepo.ParametersRow.MonthlyHoursEnum == MothlyHoursEnum.Exclusive && !currentCol.Flag_Monte_Ore))
                                {
                                    List<Col> cols = new List<Col>();
                                    cols.Add(currentCol);

                                    var from = DateTime.MinValue;
                                    var blockDate = DateTime.MinValue;

                                    if (RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.HasValue)
                                    {
                                        from = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.Value.AddDays(1);
                                        blockDate = from;
                                    }
                                    else
                                    {
                                        var firstColReg = RepoManager.RegRepo.Find(reg => reg.Col_Id == currentCol.Col_Id && reg.Registrazione_Data_Ora_Fis_Reg <= currentMonth).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).FirstOrDefault();
                                        if (firstColReg != null && firstColReg.Registrazione_Data_Ora_Fis_Reg.Date < currentMonth)
                                            from = firstColReg.Registrazione_Data_Ora_Fis_Reg.Date;
                                        else from = currentMonth;
                                    }

                                    var to = currentMonth;
                                    if (from < to)
                                    {
                                        totalMinutes = TimesheetModuleItem.GetLastMonthMinutesAmmount(cols.First(), from, to, false) + currentCol.Monte_Minuti;

                                    }
                                    else
                                    {
                                        totalMinutes = currentCol.Monte_Minuti - RepoManager.Reg_VRepo.SubstractTotals(currentCol, to, blockDate);
                                    }
                                }

                                _lastMonthlyPerRowHandleDictionary.Add(e.RowHandle, totalMinutes);
                            }
                        }
                        if (!_totalHoursPerColDictionary.ContainsKey(currentColdId))
                        {
                            _totalHoursPerColDictionary.Add(currentColdId, 0);
                        }
                    }
                }

                #endregion

            }
            else if (currentItem.FieldName == "Cant_Id")
            {

                #region Nell'elaborazione della colonna collaboratore allora si calcola il totale dei minuti per cantiere

                if (e.FieldValue != null && !String.IsNullOrEmpty(e.FieldValue.ToString()))
                {
                    int currentCantId = Convert.ToInt32(e.FieldValue);

                    if (currentCantId != 0)
                    {
                        if (!_rowHandleCantIdMappingDictionary.ContainsKey(e.RowHandle))
                            _rowHandleCantIdMappingDictionary.Add(e.RowHandle, currentCantId);

                        if (!_totalHoursPerCantDictionary.ContainsKey(currentCantId))
                            _totalHoursPerCantDictionary.Add(currentCantId, 0);
                    }
                }

                #endregion

            }
            else
            {

                #region Gestione del calcolo per campi giorno

                bool isDecimalHours = SelectedTimesheetOptions.Any(opz => opz == ShowDecimalHoursOptionValue);

                if (currentItem != null)
                {
                    // Fase di inizializzazione:
                    // Per ogni singola colonna viene inizializzato il totale a 0
                    if (e.SummaryProcess == DevExpress.Data.CustomSummaryProcess.Start)
                    {
                        if (!_minutesDictionary.ContainsKey(currentItem.Tag))
                            _minutesDictionary.Add(currentItem.Tag, 0);
                        _minutesDictionary[currentItem.Tag] = 0;
                    }

                    // Fase di calcolo (eseguita per ogni record da calcolare, per ogni gruoup summary):
                    // calcolo delle somme sulle varie colonne
                    if (e.SummaryProcess == DevExpress.Data.CustomSummaryProcess.Calculate)
                    {
                        // viene recuperato il valore in numero di ore
                        double value = Convert.ToDouble(e.FieldValue);

                        // conversione del valore visualizzato in minuti
                        int minutesValue = CommonService.FromHoursToMinutes(value, isDecimalHours);

                        // se si sta calcolando il totale
                        if (currentItem.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE))
                        {
                            // se si tratta di un piano (vedere primo if metodo) allora il totale minuti è 0
                            if (_justificationsRowsDictionary.ContainsKey(e.RowHandle))
                                minutesValue *= 0;

                            // si sommano i minuti calcolati al dizionario
                            _minutesDictionary[currentItem.Tag] += minutesValue;
                        }

                        // se si sta calcolando il delta, gli straordinari, gli straordinari notturni o le ore totali
                        if (currentItem.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA) ||
                            currentItem.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_STRAORDINARI) ||
                            currentItem.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_STRAORDINARI_NOTTURNI) ||
                            currentItem.Tag == "TotalHours")
                        {
                            // se si tratta delle ore piano il numero di minuti da trattare viene messo a negativo
                            if (_justificationsRowsDictionary.ContainsKey(e.RowHandle))
                                minutesValue *= -1;

                            // si sommano i minuti calcolati alla posizione corretta del tag nel dizionario
                            _minutesDictionary[currentItem.Tag] += minutesValue;

                            // inoltre si aggiorna il totale delle ore
                            if (currentItem.Tag == "TotalHours")
                            {
                                if (_rowHandleColIdMappingDictionary.ContainsKey(e.RowHandle))
                                    if (_totalHoursPerColDictionary.ContainsKey(_totalHoursPerColDictionary[_rowHandleColIdMappingDictionary[e.RowHandle]]))
                                        _totalHoursPerColDictionary[_rowHandleColIdMappingDictionary[e.RowHandle]] += minutesValue;

                                if (_rowHandleCantIdMappingDictionary.ContainsKey(e.RowHandle))
                                {
                                    if (_totalHoursPerCantDictionary.ContainsKey(_totalHoursPerCantDictionary[_rowHandleCantIdMappingDictionary[e.RowHandle]]))
                                        _totalHoursPerCantDictionary[_rowHandleCantIdMappingDictionary[e.RowHandle]] += minutesValue;

                                }

                            }
                        }
                    }

                    // Finalization
                    // consolidamento dei totali calcolati precedentemente
                    if (e.SummaryProcess == DevExpress.Data.CustomSummaryProcess.Finalize)
                    {
                        var key = e.GetValue(TimesheetGridView.KeyFieldName);
                        var currentColId = TsmItems.First(ts => ts.ID == Convert.ToInt64(key)).ColId;
                        var currentCantId = TsmItems.First(ts => ts.ID == Convert.ToInt64(key)).CantId;

                        #region Calcolo dell'eventuale numero del giorno in processo

                        int dayNumber = 0;
                        if (currentItem.FieldName.StartsWith("Day")) // se si sta trattando una colonna giorno
                        {
                            if (currentItem.FieldName.Contains("Minus")) // si recupera il numero del giorno del mese precedente
                            {
                                Int32.TryParse(currentItem.FieldName.Substring(currentItem.FieldName.Length - 1, 1), out dayNumber);

                                if (dayNumber != 0)
                                    dayNumber = -1 * dayNumber;
                            }
                            else if (currentItem.FieldName.Contains("Plus")) // si recupera il numero del giorno del mese successivo
                            {
                                Int32.TryParse(currentItem.FieldName.Substring(currentItem.FieldName.Length - 1, 1), out dayNumber);
                                if (dayNumber != 0)
                                    dayNumber = dayNumber + 100;
                            }
                            else // si recupera il numero del giorno del mese corrente
                                Int32.TryParse(currentItem.FieldName.Substring(currentItem.FieldName.Length - 2), out dayNumber);
                        }

                        #endregion

                        #region Gestione del totale straordinari

                        // se si stanno elaborando gli straordinari
                        if (currentItem.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_STRAORDINARI))
                        {
                            // se non si sta processando un totale settimanale
                            if (!currentItem.FieldName.StartsWith("TotalWeek"))
                            {

                                #region Calcolo dei totali giornalieri

                                // se è la prima colonna categorizzata allora si applicano i totali completi
                                if (e.GroupLevel == 0)
                                    e.TotalValue = dayNumber != 0
                                        ? TsTotalController.GetDayStrHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null)
                                        : TsTotalController.GetMonthStrHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null);
                                else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                    if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                        e.TotalValue = dayNumber != 0
                                            ? TsTotalController.GetDayStrHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId)
                                            : TsTotalController.GetMonthStrHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId);

                                #endregion

                            }
                            else // se si sta processando un totale settimanale
                            {

                                #region Calcolo dei totali per settimana

                                // si recupera il numero del totale della settimana
                                int weekNumber = 0;
                                Int32.TryParse(currentItem.FieldName.Substring(currentItem.FieldName.Length - 1, 1), out weekNumber);

                                // se il numero della settimana è comprensibile
                                if (weekNumber != 0)
                                {
                                    // si recupera la domenica indicata dal numero indicato a partire dagli elementi timesheet in visualizzazione

                                    // inizializzazione di inizio e fine mese
                                    DateTime firstMonthDate = TsmItems.First().StartDate;
                                    DateTime startDate = firstMonthDate;
                                    DateTime endDate = CommonService.GetLastMonthDay(startDate);
                                    DateTime lastMonthDate = endDate;

                                    // eventuale aggiustamento delle date del mese con la chiusura della settimana iniziale e finale
                                    if (startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                                        startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
                                    if (endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                                        endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                                    // recupero delle date nel periodo richiest
                                    IEnumerable<DateTime> sundayInPeriod = CommonService.GetDatesFromPeriod(startDate, endDate).Where(dt => dt.DayOfWeek == DayOfWeek.Sunday);

                                    // se sono state trovate delle settimane e ne sono presenti in numero richiesto
                                    if (sundayInPeriod.Any() && sundayInPeriod.Count() >= weekNumber)
                                    {
                                        DateTime sundayDay = sundayInPeriod.ElementAt(weekNumber - 1);
                                        int sundayDayNumber = sundayInPeriod.ElementAt(weekNumber - 1).Day;

                                        // se si tratta del mese successivo si aggiunge 100 al valore giorno
                                        if (sundayDay.Month == firstMonthDate.AddMonths(1).Month && sundayDay.Year == firstMonthDate.AddMonths(1).Year)
                                            sundayDayNumber += 100;

                                        // se è la prima colonna categorizzata allora si applicano i totali completi
                                        if (e.GroupLevel == 0)
                                            e.TotalValue = TsTotalController.GetWeekStrHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null, sundayDayNumber, lastMonthDate.Day);
                                        else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                            if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                                e.TotalValue = TsTotalController.GetWeekStrHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId, sundayDayNumber, lastMonthDate.Day);
                                    }
                                }

                                #endregion
                            }
                        }

                        #endregion

                        #region Gestione del totale straordinari notturni

                        // se si stanno elaborando gli straordinari notturni
                        if (currentItem.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_STRAORDINARI_NOTTURNI))
                        {
                            // se non si sta processando un totale settimanale
                            if (!currentItem.FieldName.StartsWith("TotalWeek"))
                            {

                                #region Calcolo dei totali giornalieri

                                // se è la prima colonna categorizzata allora si applicano i totali completi
                                if (e.GroupLevel == 0)
                                    e.TotalValue = dayNumber != 0
                                        ? TsTotalController.GetNocturneStrHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null)
                                        : TsTotalController.GetMonthNocturneStrHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null);
                                else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                    if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                        e.TotalValue = dayNumber != 0
                                            ? TsTotalController.GetNocturneStrHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId)
                                            : TsTotalController.GetMonthNocturneStrHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId);

                                #endregion

                            }
                            else // se si sta processando un totale settimanale
                            {

                                #region Calcolo dei totali per settimana

                                // si recupera il numero del totale della settimana
                                int weekNumber = 0;
                                Int32.TryParse(currentItem.FieldName.Substring(currentItem.FieldName.Length - 1, 1), out weekNumber);

                                // se il numero della settimana è comprensibile
                                if (weekNumber != 0)
                                {
                                    // si recupera la domenica indicata dal numero indicato a partire dagli elementi timesheet in visualizzazione

                                    // inizializzazione di inizio e fine mese
                                    DateTime firstMonthDate = TsmItems.First().StartDate;
                                    DateTime startDate = firstMonthDate;
                                    DateTime endDate = CommonService.GetLastMonthDay(startDate);
                                    DateTime lastMonthDate = endDate;

                                    // eventuale aggiustamento delle date del mese con la chiusura della settimana iniziale e finale
                                    if (startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                                        startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
                                    if (endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                                        endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                                    // recupero delle date nel periodo richiest
                                    IEnumerable<DateTime> sundayInPeriod = CommonService.GetDatesFromPeriod(startDate, endDate).Where(dt => dt.DayOfWeek == DayOfWeek.Sunday);

                                    // se sono state trovate delle settimane e ne sono presenti in numero richiesto
                                    if (sundayInPeriod.Any() && sundayInPeriod.Count() >= weekNumber)
                                    {
                                        DateTime sundayDay = sundayInPeriod.ElementAt(weekNumber - 1);
                                        int sundayDayNumber = sundayInPeriod.ElementAt(weekNumber - 1).Day;

                                        // se si tratta del mese successivo si aggiunge 100 al valore giorno
                                        if (sundayDay.Month == firstMonthDate.AddMonths(1).Month && sundayDay.Year == firstMonthDate.AddMonths(1).Year)
                                            sundayDayNumber += 100;

                                        // se è la prima colonna categorizzata allora si applicano i totali completi
                                        if (e.GroupLevel == 0)
                                            e.TotalValue = TsTotalController.GetWeekNocturneStrHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null, sundayDayNumber, lastMonthDate.Day);
                                        else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                            if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                                e.TotalValue = TsTotalController.GetWeekNocturneStrHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId, sundayDayNumber, lastMonthDate.Day);
                                    }
                                }

                                #endregion

                            }
                        }

                        #endregion

                        #region Gestione del totale delle ore

                        // se si sta elaborando il totale allora si riporta il totale
                        if (currentItem.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE))
                        {
                            // se non si sta processando un totale settimanale
                            if (!currentItem.FieldName.StartsWith("TotalWeek"))
                            {

                                #region Calcolo dei totali giornalieri

                                // se è la prima colonna categorizzata allora si applicano i totali completi
                                if (e.GroupLevel == 0)
                                    e.TotalValue = dayNumber != 0
                                        ? TsTotalController.GetDayTotalHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null)
                                        : TsTotalController.GetMonthTotalHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null);

                                else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                    if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                        e.TotalValue = dayNumber != 0
                                            ? TsTotalController.GetDayTotalHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId)
                                            : TsTotalController.GetMonthTotalHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId);

                                #endregion

                            }
                            else // se si sta processando un totale settimanale
                            {

                                #region Calcolo dei totali per settimana

                                // si recupera il numero del totale della settimana
                                int weekNumber = 0;
                                Int32.TryParse(currentItem.FieldName.Substring(currentItem.FieldName.Length - 1, 1), out weekNumber);

                                // se il numero della settimana è comprensibile
                                if (weekNumber != 0)
                                {
                                    // si recupera la domenica indicata dal numero indicato a partire dagli elementi timesheet in visualizzazione

                                    // inizializzazione di inizio e fine mese
                                    DateTime firstMonthDate = TsmItems.First().StartDate;
                                    DateTime startDate = firstMonthDate;
                                    DateTime endDate = CommonService.GetLastMonthDay(startDate);
                                    DateTime lastMonthDate = endDate;

                                    // eventuale aggiustamento delle date del mese con la chiusura della settimana iniziale e finale
                                    if (startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                                        startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
                                    if (endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                                        endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                                    // recupero delle date nel periodo richiest
                                    IEnumerable<DateTime> sundayInPeriod = CommonService.GetDatesFromPeriod(startDate, endDate).Where(dt => dt.DayOfWeek == DayOfWeek.Sunday);

                                    // se sono state trovate delle settimane e ne sono presenti in numero richiesto
                                    if (sundayInPeriod.Any() && sundayInPeriod.Count() >= weekNumber)
                                    {
                                        DateTime sundayDay = sundayInPeriod.ElementAt(weekNumber - 1);
                                        int sundayDayNumber = sundayInPeriod.ElementAt(weekNumber - 1).Day;

                                        // se si tratta del mese successivo si aggiunge 100 al valore giorno
                                        if (sundayDay.Month == firstMonthDate.AddMonths(1).Month && sundayDay.Year == firstMonthDate.AddMonths(1).Year)
                                            sundayDayNumber += 100;

                                        // se è la prima colonna categorizzata allora si applicano i totali completi
                                        if (e.GroupLevel == 0)
                                            e.TotalValue = TsTotalController.GetWeekTotalHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null, sundayDayNumber, lastMonthDate.Day);
                                        else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                            if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                                e.TotalValue = TsTotalController.GetWeekTotalHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId, sundayDayNumber, lastMonthDate.Day);
                                    }
                                }
                                #endregion
                            }
                        }

                        #endregion

                        #region Gestione del delta totale

                        // se si sta elaborando il delta allora si riporta il valore totale
                        if (currentItem.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA))
                        {
                            // se non si sta processando un totale settimanale
                            if (!currentItem.FieldName.StartsWith("TotalWeek"))
                            {

                                #region Calcolo dei totali giornalieri

                                // se è la prima colonna categorizzata allora si applicano i totali completi
                                if (e.GroupLevel == 0)
                                    e.TotalValue = dayNumber != 0
                                        ? TsTotalController.GetDeltaHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null)
                                        : TsTotalController.GetMonthDeltaHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null);
                                else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                    if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                        e.TotalValue = dayNumber != 0
                                            ? TsTotalController.GetDeltaHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId)
                                            : TsTotalController.GetMonthDeltaHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId);

                                #endregion

                            }
                            else // se si sta processando un totale settimanale
                            {

                                #region Calcolo dei totali per settimana

                                // si recupera il numero del totale della settimana
                                int weekNumber = 0;
                                Int32.TryParse(currentItem.FieldName.Substring(currentItem.FieldName.Length - 1, 1), out weekNumber);

                                // se il numero della settimana è comprensibile
                                if (weekNumber != 0)
                                {
                                    // si recupera la domenica indicata dal numero indicato a partire dagli elementi timesheet in visualizzazione

                                    // inizializzazione di inizio e fine mese
                                    DateTime firstMonthDate = TsmItems.First().StartDate;
                                    DateTime startDate = firstMonthDate;
                                    DateTime endDate = CommonService.GetLastMonthDay(startDate);
                                    DateTime lastMonthDate = endDate;

                                    // eventuale aggiustamento delle date del mese con la chiusura della settimana iniziale e finale
                                    if (startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                                        startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
                                    if (endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                                        endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                                    // recupero delle date nel periodo richiest
                                    IEnumerable<DateTime> sundayInPeriod = CommonService.GetDatesFromPeriod(startDate, endDate).Where(dt => dt.DayOfWeek == DayOfWeek.Sunday);

                                    // se sono state trovate delle settimane e ne sono presenti in numero richiesto
                                    if (sundayInPeriod.Any() && sundayInPeriod.Count() >= weekNumber)
                                    {
                                        DateTime sundayDay = sundayInPeriod.ElementAt(weekNumber - 1);
                                        int sundayDayNumber = sundayInPeriod.ElementAt(weekNumber - 1).Day;

                                        // se si tratta del mese successivo si aggiunge 100 al valore giorno
                                        if (sundayDay.Month == firstMonthDate.AddMonths(1).Month && sundayDay.Year == firstMonthDate.AddMonths(1).Year)
                                            sundayDayNumber += 100;

                                        // se è la prima colonna categorizzata allora si applicano i totali completi
                                        if (e.GroupLevel == 0)
                                            e.TotalValue = TsTotalController.GetWeekDeltaHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null, sundayDayNumber, lastMonthDate.Day);
                                        else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                            if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                                e.TotalValue = TsTotalController.GetWeekDeltaHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId, sundayDayNumber, lastMonthDate.Day);
                                    }
                                }
                                #endregion
                            }
                        }

                        #endregion

                        #region Gestione del piano totale

                        // se si sta elaborando il totale delle ore piano si riporta tale valore
                        if (currentItem.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_TOT))
                        {
                            // se non si sta processando un totale settimanale
                            if (!currentItem.FieldName.StartsWith("TotalWeek"))
                            {

                                #region Calcolo dei totali giornalieri

                                // se è la prima colonna categorizzata allora si applicano i totali completi
                                if (e.GroupLevel == 0)
                                    e.TotalValue = dayNumber != 0
                                        ? TsTotalController.GetDayPlanHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null)
                                        : TsTotalController.GetMonthPlanHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null);
                                else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                    if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                        e.TotalValue = dayNumber != 0
                                            ? TsTotalController.GetDayPlanHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId)
                                            : TsTotalController.GetMonthPlanHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId);

                                #endregion

                            }
                            else // se si sta processando un totale settimanale
                            {

                                #region Calcolo dei totali per settimana

                                // si recupera il numero del totale della settimana
                                int weekNumber = 0;
                                Int32.TryParse(currentItem.FieldName.Substring(currentItem.FieldName.Length - 1, 1), out weekNumber);

                                // se il numero della settimana è comprensibile
                                if (weekNumber != 0)
                                {
                                    // si recupera la domenica indicata dal numero indicato a partire dagli elementi timesheet in visualizzazione

                                    // inizializzazione di inizio e fine mese
                                    DateTime firstMonthDate = TsmItems.First().StartDate;
                                    DateTime startDate = firstMonthDate;
                                    DateTime endDate = CommonService.GetLastMonthDay(startDate);
                                    DateTime lastMonthDate = endDate;

                                    // eventuale aggiustamento delle date del mese con la chiusura della settimana iniziale e finale
                                    if (startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                                        startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
                                    if (endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                                        endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                                    // recupero delle date nel periodo richiest
                                    IEnumerable<DateTime> sundayInPeriod = CommonService.GetDatesFromPeriod(startDate, endDate).Where(dt => dt.DayOfWeek == DayOfWeek.Sunday);

                                    // se sono state trovate delle settimane e ne sono presenti in numero richiesto
                                    if (sundayInPeriod.Any() && sundayInPeriod.Count() >= weekNumber)
                                    {
                                        DateTime sundayDay = sundayInPeriod.ElementAt(weekNumber - 1);
                                        int sundayDayNumber = sundayInPeriod.ElementAt(weekNumber - 1).Day;

                                        // se si tratta del mese successivo si aggiunge 100 al valore giorno
                                        if (sundayDay.Month == firstMonthDate.AddMonths(1).Month && sundayDay.Year == firstMonthDate.AddMonths(1).Year)
                                            sundayDayNumber += 100;

                                        // se è la prima colonna categorizzata allora si applicano i totali completi
                                        if (e.GroupLevel == 0)
                                            e.TotalValue = TsTotalController.GetWeekPlanHour(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null, sundayDayNumber, lastMonthDate.Day);
                                        else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                            if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                                e.TotalValue = TsTotalController.GetWeekPlanHour(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId, sundayDayNumber, lastMonthDate.Day);
                                    }
                                }

                                #endregion

                            }
                        }

                        #endregion

                        #region Gestione del totale ore ordinarie

                        // se si sta elaborando il totale delle ore ordinarie, si riporta tale valore
                        if (currentItem.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_ORD))
                        {
                            // se non si sta processando un totale settimanale
                            if (!currentItem.FieldName.StartsWith("TotalWeek"))
                            {

                                #region Calcolo dei totali giornalieri

                                // se è la prima colonna categorizzata allora si applicano i totali completi
                                if (e.GroupLevel == 0)
                                    e.TotalValue = dayNumber != 0
                                        ? TsTotalController.GetDayOrdinaryHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null)
                                        : TsTotalController.GetMonthOrdinaryHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null);
                                else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                    if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                        e.TotalValue = dayNumber != 0
                                            ? TsTotalController.GetDayOrdinaryHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId)
                                            : TsTotalController.GetMonthOrdinaryHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId);

                                #endregion

                            }
                            else // se si sta processando un totale settimanale
                            {

                                #region Calcolo dei totali per settimana

                                // si recupera il numero del totale della settimana
                                int weekNumber = 0;
                                Int32.TryParse(currentItem.FieldName.Substring(currentItem.FieldName.Length - 1, 1), out weekNumber);

                                // se il numero della settimana è comprensibile
                                if (weekNumber != 0)
                                {
                                    // si recupera la domenica indicata dal numero indicato a partire dagli elementi timesheet in visualizzazione

                                    // inizializzazione di inizio e fine mese
                                    DateTime firstMonthDate = TsmItems.First().StartDate;
                                    DateTime startDate = firstMonthDate;
                                    DateTime endDate = CommonService.GetLastMonthDay(startDate);
                                    DateTime lastMonthDate = endDate;

                                    // eventuale aggiustamento delle date del mese con la chiusura della settimana iniziale e finale
                                    if (startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                                        startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
                                    if (endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                                        endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                                    // recupero delle date nel periodo richiest
                                    IEnumerable<DateTime> sundayInPeriod = CommonService.GetDatesFromPeriod(startDate, endDate).Where(dt => dt.DayOfWeek == DayOfWeek.Sunday);

                                    // se sono state trovate delle settimane e ne sono presenti in numero richiesto
                                    if (sundayInPeriod.Any() && sundayInPeriod.Count() >= weekNumber)
                                    {
                                        DateTime sundayDay = sundayInPeriod.ElementAt(weekNumber - 1);
                                        int sundayDayNumber = sundayInPeriod.ElementAt(weekNumber - 1).Day;

                                        // se si tratta del mese successivo si aggiunge 100 al valore giorno
                                        if (sundayDay.Month == firstMonthDate.AddMonths(1).Month && sundayDay.Year == firstMonthDate.AddMonths(1).Year)
                                            sundayDayNumber += 100;

                                        // se è la prima colonna categorizzata allora si applicano i totali completi
                                        if (e.GroupLevel == 0)
                                            e.TotalValue = TsTotalController.GetWeekOrdinaryHour(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null, sundayDayNumber, lastMonthDate.Day);
                                        else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                            if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                                e.TotalValue = TsTotalController.GetWeekOrdinaryHour(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId, sundayDayNumber, lastMonthDate.Day);
                                    }
                                }

                                #endregion

                            }
                        }

                        #endregion

                        #region Gestione del totale arrotondamenti

                        // se si sta elaborando il totale delle ore ordinarie, si riporta tale valore
                        if (currentItem.Tag == "Arrot")
                        {
                            // se non si sta processando un totale settimanale
                            if (!currentItem.FieldName.StartsWith("TotalWeek"))
                            {

                                #region Calcolo dei totali giornalieri

                                // se è la prima colonna categorizzata allora si applicano i totali completi
                                if (e.GroupLevel == 0)
                                    e.TotalValue = dayNumber != 0
                                        ? TsTotalController.GetDayArrotHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null)
                                        : TsTotalController.GetMonthArrotHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null);
                                else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                    if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                        e.TotalValue = dayNumber != 0
                                            ? TsTotalController.GetDayArrotHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId)
                                            : TsTotalController.GetMonthArrotHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId);

                                #endregion

                            }
                            else // se si sta processando un totale settimanale
                            {

                                #region Calcolo dei totali per settimana

                                // si recupera il numero del totale della settimana
                                int weekNumber = 0;
                                Int32.TryParse(currentItem.FieldName.Substring(currentItem.FieldName.Length - 1, 1), out weekNumber);

                                // se il numero della settimana è comprensibile
                                if (weekNumber != 0)
                                {
                                    // inizializzazione di inizio e fine mese
                                    DateTime firstMonthDate = TsmItems.First().StartDate;
                                    DateTime startDate = firstMonthDate;
                                    DateTime endDate = CommonService.GetLastMonthDay(startDate);
                                    DateTime lastMonthDate = endDate;

                                    // eventuale aggiustamento delle date del mese con la chiusura della settimana iniziale e finale
                                    if (startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                                        startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
                                    if (endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                                        endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                                    // recupero delle date nel periodo richiest
                                    IEnumerable<DateTime> sundayInPeriod = CommonService.GetDatesFromPeriod(startDate, endDate).Where(dt => dt.DayOfWeek == DayOfWeek.Sunday);

                                    // se sono state trovate delle settimane e ne sono presenti in numero richiesto
                                    if (sundayInPeriod.Any() && sundayInPeriod.Count() >= weekNumber)
                                    {
                                        DateTime sundayDay = sundayInPeriod.ElementAt(weekNumber - 1);
                                        int sundayDayNumber = sundayInPeriod.ElementAt(weekNumber - 1).Day;

                                        // se si tratta del mese successivo si aggiunge 100 al valore giorno
                                        if (sundayDay.Month == firstMonthDate.AddMonths(1).Month && sundayDay.Year == firstMonthDate.AddMonths(1).Year)
                                            sundayDayNumber += 100;

                                        // se è la prima colonna categorizzata allora si applicano i totali completi
                                        if (e.GroupLevel == 0)
                                            e.TotalValue = TsTotalController.GetWeekArrotHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null, sundayDayNumber, lastMonthDate.Day);
                                        else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                            if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                                e.TotalValue = TsTotalController.GetWeekArrotHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId, sundayDayNumber, lastMonthDate.Day);
                                    }
                                }

                                #endregion

                            }
                        }

                        #endregion

                        #region Gestione del totale ore con motivazione

                        // se si sta elaborando il totale delle ore con motivazione, si riporta tale valore
                        if (currentItem.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_JUST))
                        {
                            // se non si sta processando un totale settimanale
                            if (!currentItem.FieldName.StartsWith("TotalWeek"))
                            {

                                #region Calcolo dei totali giornalieri

                                // se è la prima colonna categorizzata allora si applicano i totali completi
                                if (e.GroupLevel == 0)
                                    e.TotalValue = dayNumber != 0
                                        ? TsTotalController.GetDayJustificationHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null)
                                        : TsTotalController.GetMonthJustificationHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null);
                                else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                    if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                        TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                        e.TotalValue = dayNumber != 0
                                            ? TsTotalController.GetDayJustificationHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId)
                                            : TsTotalController.GetMonthJustificationHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId);

                                #endregion

                            }
                            else // se si sta processando un totale settimanale
                            {

                                #region Calcolo dei totali per settimana

                                // si recupera il numero del totale della settimana
                                int weekNumber = 0;
                                Int32.TryParse(currentItem.FieldName.Substring(currentItem.FieldName.Length - 1, 1), out weekNumber);

                                // se il numero della settimana è comprensibile
                                if (weekNumber != 0)
                                {
                                    // si recupera la domenica indicata dal numero indicato a partire dagli elementi timesheet in visualizzazione

                                    // inizializzazione di inizio e fine mese
                                    DateTime firstMonthDate = TsmItems.First().StartDate;
                                    DateTime startDate = firstMonthDate;
                                    DateTime endDate = CommonService.GetLastMonthDay(startDate);
                                    DateTime lastMonthDate = endDate;

                                    // eventuale aggiustamento delle date del mese con la chiusura della settimana iniziale e finale
                                    if (startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                                        startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);
                                    if (endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                                        endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

                                    // recupero delle date nel periodo richiest
                                    IEnumerable<DateTime> sundayInPeriod = CommonService.GetDatesFromPeriod(startDate, endDate).Where(dt => dt.DayOfWeek == DayOfWeek.Sunday);

                                    // se sono state trovate delle settimane e ne sono presenti in numero richiesto
                                    if (sundayInPeriod.Any() && sundayInPeriod.Count() >= weekNumber)
                                    {
                                        DateTime sundayDay = sundayInPeriod.ElementAt(weekNumber - 1);
                                        int sundayDayNumber = sundayInPeriod.ElementAt(weekNumber - 1).Day;

                                        // se si tratta del mese successivo si aggiunge 100 al valore giorno
                                        if (sundayDay.Month == firstMonthDate.AddMonths(1).Month && sundayDay.Year == firstMonthDate.AddMonths(1).Year)
                                            sundayDayNumber += 100;

                                        // se è la prima colonna categorizzata allora si applicano i totali completi
                                        if (e.GroupLevel == 0)
                                            e.TotalValue = TsTotalController.GetWeekJustificationHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null, sundayDayNumber, lastMonthDate.Day);
                                        else if (e.GroupLevel == 1) // altrimenti, se è la seconda colonna ed è trattabile, si applicano i totali parziali della sottocategoria
                                            if (TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.ColMnemonic) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantId) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantDesc) ||
                                                TimesheetGridView.GetGroupedColumns()[1].FieldName == CommonService.GetPropertyName(() => TsmStub.CantMnemonic))
                                                e.TotalValue = TsTotalController.GetWeekJustificationHours(CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, CurrentFirstEntity != ColEntityType ? currentColId : currentCantId, sundayDayNumber, lastMonthDate.Day);
                                    }
                                }

                                #endregion

                            }
                        }

                        #endregion

                        #region Gestione degli altri totali

                        // se sista elaborando il totale delle ore allora si riporta il totale
                        if (currentItem.Tag == "TotalHours")
                            if (dayNumber != 0) // se si sta processando una colonna giorno
                                e.TotalValue = TsTotalController.GetDayTotalHours(dayNumber, CurrentFirstEntity == ColEntityType ? currentColId : currentCantId, null);

                        // se si sta elaborando il totale allora si riporta il totale di quando calcolato
                        if (currentItem.Tag == "LastMonthlyHours")
                            if (_lastMonthlyPerRowHandleDictionary.ContainsKey(e.RowHandle))
                            {
                                e.TotalValue =
                                    CommonService.GetDoubleFromMinutes(_lastMonthlyPerRowHandleDictionary[e.RowHandle],
                                        isDecimalHours);
                            }

                        // se si sta elaborando il totale del mese corrente allora si riporta il totale
                        if (currentItem.Tag == "CurrentMonthlyHours")
                        {
                            var lastMonthlyTimespan = TimeSpan.Zero;
                            if (_lastMonthlyPerRowHandleDictionary.ContainsKey(e.RowHandle))
                                lastMonthlyTimespan = new TimeSpan(0, _lastMonthlyPerRowHandleDictionary[e.RowHandle], 0);

                            TimeSpan totalHoursTimespan = new TimeSpan(0, _rowHandleColIdMappingDictionary.ContainsKey(e.RowHandle)
                                ? _totalHoursPerColDictionary[_rowHandleColIdMappingDictionary[e.RowHandle]]
                                : 0, 0);

                            TimeSpan curentMonthlyHours = lastMonthlyTimespan + totalHoursTimespan;

                            e.TotalValue = CommonService.GetDoubleFromMinutes((int)curentMonthlyHours.TotalMinutes, isDecimalHours);
                        }

                        #endregion

                    }
                }

                #endregion

            }
        }

        protected void TimesheetGridView_HtmlFooterCellPrepared(object sender, ASPxGridViewTableFooterCellEventArgs e)
        //Gestione Riepiloghi Ore (Totali/Straordinari/Delta)
        {
            //IMposta Colore del Bordo x le Righe 
            var Cell = TimeSheetEnum.TotalRow;
            e.Cell.BorderColor = RepoManager.ParamRepo.GetColorFromEnum((TimeSheetEnum)Cell, false);
            e.Cell.BorderWidth = new Unit(1, UnitType.Pixel);
            e.Cell.BorderStyle = BorderStyle.Solid;
            e.Cell.HorizontalAlign = HorizontalAlign.Center;

            if (e.Column != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.CantId)] &&
                e.Column != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.CantMnemonic)] &&
                e.Column != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.CantDesc)] &&
                e.Column != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.ColId)] &&
                e.Column != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.ColMnemonic)] &&
                e.Column != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.ColDesc)] &&
                e.Column != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.ID)] &&
                e.Column != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.Justification)] &&
                e.Column != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.Order)] &&
                e.Column != TimesheetGridView.Columns["LastMonthlyHours"] &&
                e.Column != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.TotalHours)] &&
                e.Column != TimesheetGridView.Columns["CurrentMonthlyHours"] &&
                e.Column != TimesheetGridView.Columns[CommonService.GetPropertyName(() => TsmStub.TotalDays)] &&
                e.Column.Caption.Trim() != String.Empty)
            {
                SetWeekendColor(e.Column.Caption, ((GridViewDataColumn)e.Column).FieldName, e.Cell);
                //string columnField = "Day" + e.Column.Caption.Substring(0, 2);
                string columnField = ((GridViewDataColumn)e.Column).FieldName;

                //IEnumerable<ASPxSummaryItem> miao = TimesheetGridView.GroupSummary.Where(sum => sum.FieldName == columnField && sum.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA));
                ASPxSummaryItem deltaSI = TimesheetGridView.GroupSummary.FirstOrDefault(sum => sum.FieldName == columnField && sum.Tag == BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA));
                if (deltaSI != default(ASPxSummaryItem))
                {
                    double value = 0.0d;
                    if (e.GetSummaryValue(deltaSI) != null)
                        double.TryParse(e.GetSummaryValue(deltaSI).ToString(), out value);

                    if (value < 0.0d)
                    {
                        //colore dei caratteri in caso di valori negativi del delta
                        Cell = TimeSheetEnum.NegativeCell;
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((TimeSheetEnum)Cell, false);
                    }

                    else if (value >= 0.0d)
                    {
                        //colore dei caratteri in caso di valori positivi del delta
                        Cell = TimeSheetEnum.PositiveCell;
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((TimeSheetEnum)Cell, false);
                    }

                }
            }
        }

        protected void cpTSMLayout_Callback(object sender, CallbackEventArgsBase e)
        {
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
                        if (currentLayout.Utenti_Id != null)
                        {
                            currentLayout.Data_Layout_DataGrid = DateTime.UtcNow;
                            RepoManager.Tab_DataGridRepo.Update(currentLayout, true);
                        }

                        TimesheetGridView.LoadClientLayout(currentLayout.Layout_DataGrid);
                    }
                    else
                        TimesheetGridView.LoadClientLayout(PowerWebContext.GetFromSession<String>("TimesheetGridViewLayout_" + TimesheetGridView.ID));

                    BindTsmLayoutCombo();
                    cmbTSMLayout.Value = currentLayout != null ? currentLayout.Nome_Layout : "Default";
                    BindTimesheetGrid();
                }
            }

            #endregion

            #region Save client layout

            if (e.Parameter == "save")
            {
                // non è possibile salvare la vista con stringa di default
                if (cmbTSMLayout.Text != "Default")
                {

                    String currentLayout = TimesheetGridView.SaveClientLayout();

                    if (PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level)
                    {
                        Tab_DataGrid savedLayout = RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == TimesheetGridView.ID
                           && tdg.Nome_Layout == cmbTSMLayout.Text);

                        if (savedLayout == null)
                        {
                            RepoManager.Tab_DataGridRepo.Add(new Tab_DataGrid
                            {
                                Nome_DataGrid = TimesheetGridView.ID,
                                Layout_DataGrid = currentLayout,
                                Nome_Layout = cmbTSMLayout.Text,
                                Data_Layout_DataGrid = DateTime.UtcNow,
                            }, true);
                        }
                        else
                        {
                            savedLayout.Layout_DataGrid = currentLayout;
                            RepoManager.Tab_DataGridRepo.Update(savedLayout, true);
                        }

                        BindTsmLayoutCombo();
                        cmbTSMLayout.Value = savedLayout != null ? savedLayout.Nome_Layout : "Default";

                    }
                    else
                    {
                        Tab_DataGrid savedLayout = RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == TimesheetGridView.ID
                            && tdg.Nome_Layout == cmbTSMLayout.Text && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);

                        if (savedLayout == null)
                        {
                            RepoManager.Tab_DataGridRepo.Add(new Tab_DataGrid
                            {
                                Utenti_Id = PowerWebContext.Current.User.Utenti_Id,
                                Nome_DataGrid = TimesheetGridView.ID,
                                Layout_DataGrid = currentLayout,
                                Nome_Layout = cmbTSMLayout.Text,
                                Data_Layout_DataGrid = DateTime.UtcNow,
                            }, true);
                        }
                        else
                        {
                            savedLayout.Layout_DataGrid = currentLayout;
                            savedLayout.Data_Layout_DataGrid = DateTime.UtcNow;
                            RepoManager.Tab_DataGridRepo.Update(savedLayout, true);
                        }

                        BindTsmLayoutCombo();
                        cmbTSMLayout.Value = savedLayout != null ? savedLayout.Nome_Layout : "Default";
                    }

                    BindTimesheetGrid();
                }
            }
            #endregion

            #region Delete client layout

            if (e.Parameter == "delete")
            {
                if (PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level)
                {
                    Tab_DataGrid toDeleteTDG = RepoManager.Tab_DataGridRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == TimesheetGridView.ID
                       && tdg.Nome_Layout == cmbTSMLayout.Text);

                    RepoManager.Tab_DataGridRepo.Delete(toDeleteTDG, true);
                }
                else
                {
                    Tab_DataGrid userLayout = RepoManager.Tab_DataGridRepo.SingleOrDefault(tdg => tdg.Nome_DataGrid == TimesheetGridView.ID
                                                   && tdg.Nome_Layout == cmbTSMLayout.Text && tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
                    if (userLayout != null)
                        RepoManager.Tab_DataGridRepo.Delete(userLayout, true);
                    else if (Log != null)
                        Log.Error("User unable to delete default layout");
                }

                BindTsmLayoutCombo(true);
            }
            #endregion

            // al termine delle operazioni si ricalcola comunque la caption dei giorni in griglia timesheet
            ReloadTimesheetDayCaption(PowerWebContext.GetFromSession<DateTime>("tsmMinDate_" + TimesheetGridView.ID));
            ManageWeeklyTotalPosition(PowerWebContext.GetFromSession<DateTime>("tsmMinDate_" + TimesheetGridView.ID));

            // .. e si rigestisce la visibilità delle griglie
            ManageTimesheetGridsVisibility();
        }

        protected void TimesheetGridView_OnCommandButtonInitialize(object sender, ASPxGridViewCommandButtonEventArgs e)
        {
            // inizializzazione del timesheet item d'appoggio per il calcolo del nome delle proprietà
            TimesheetModuleItem tsModuleItemStub = null;

            // recupero la giustificazione e la provenienza d'orario
            object isFromFreeTimesheetObj = TimesheetGridView.GetRowValues(e.VisibleIndex, new string[] { CommonService.GetPropertyName(() => tsModuleItemStub.IsFromFreeTimeSheet) });
            bool isFromFreeTimesheet = false;
            if (isFromFreeTimesheetObj != null)
                isFromFreeTimesheet = (bool)isFromFreeTimesheetObj;

            // nascondo il pulsante di modifica se non si tratta di ore piano provenienti da Col_Orario
            if (e.ButtonType == ColumnCommandButtonType.Edit && !isFromFreeTimesheet)
                e.Visible = false;
        }

        protected void TimesheetGridView_OnRowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            // inizializazione dell'elenco dei nuovi orari da controlare
            var newColCantOrarioList = new List<Col_Cant_Orario>();

            #region Eventuale controllo di conformità sui sessantesimi

            // inizializzazione del dizionario che conterrà gli errori di validazione dei sessantesimi
            var errorDic = new Dictionary<string, string>();

            // se le ore sono espresse nella griglia in sessantesimi allora
            // è necessario verificare (e caso mai gestire l'errore) di
            // che siano compatibili con il loro formato
            bool isDecimalHours = SelectedTimesheetOptions.Any(opz => opz == ShowDecimalHoursOptionValue);
            if (!isDecimalHours)
            {
                // ciclo di elaborazione di tutti i nuovi valori    
                foreach (DictionaryEntry newValue in e.NewValues)
                {
                    // se il valore è quello di una giornata si procede alla verifica
                    // della compatibilità con i sessantesimi
                    if (newValue.Key.ToString().StartsWith("Day"))
                    {
                        // inizializzazione del metodo che segnala se il
                        // valore in elaborazione è compatbile con i sessantesimi
                        bool isCompatible = false;

                        // verifica della compatibilità con i sessantesimi
                        isCompatible = CommonService.IsDoubleCompatibleWith60S(Convert.ToDouble(newValue.Value));

                        // se il decimale non risulta compatibile allora si aggiunge un errore alla lista
                        if (!isCompatible)
                        {

                            errorDic.Add(newValue.Key.ToString(), "Valore non compatibile con sessantesimi");
                        }
                    }
                }
            }

            #endregion

            // se al termine dell'elaborazione sono stati trovati errori allora si segnalano e non si effettuano ulteriori operazioni
            if (errorDic.Any())
                PowerWebService.AddValidationErrors(errorDic, e.Errors, TimesheetGridView, typeof(TimesheetModule));
            else // se il controllo sui sessantesimi è andato a buon fine
            {
                e.NewValues["ColId"] = ((ASPxGridView)sender).GetRowValuesByKeyValue(e.Keys[0], new string[] { "ColId" });
                e.NewValues["CantId"] = ((ASPxGridView)sender).GetRowValuesByKeyValue(e.Keys[0], new string[] { "CantId" });

                // popolamento dell'entità con i dati riportati in griglia
                // popolo una nuova entità timesheet con i dati presenti in griglia
                var tmpTimesheet = new TimesheetModuleItem(isDecimalHours);
                PopulateTimesheetFromNewValue(tmpTimesheet, e.NewValues, ChkShowWeeklyTotals.Checked);
                RepoManager.ColCantOrarioRepo.TimesheetToColCantOrario(tmpTimesheet, newColCantOrarioList, ChkShowWeeklyTotals.Checked);

                // per ogni orario da trattare
                foreach (Col_Cant_Orario newColOrario in newColCantOrarioList)
                {
                    // popolamento della chiave del record
                    PowerWebService.FillEntityKey(newColOrario, e.Keys, Colorariokeyfieldname);

                    // impostazione dei dati prima dell'inserimento/aggiornamento
                    RepoManager.ColCantOrarioRepo.SetEntityBeforeAddOrUpdate(newColOrario);

                    // effettuazione della checkc (con verifica sull'id del nuovo/vecchio)
                    PowerWebService.AddValidationErrors(RepoManager.ColCantOrarioRepo.Check(newColOrario, newColOrario.Col_Cant_Orario_Id == 0), e.Errors, TimesheetGridView, typeof(TimesheetModule));
                }
            }

            // se sono stati riscontrati errori si ritorna il messaggio
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void TimesheetGridView_OnRowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            // scrittura nel log dell'operazione che si sta effettuando
            _log.Info(String.Format("ColOrario-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));

            // recupero dell'id del record che si sta modificando
            e.NewValues[Colorariokeyfieldname] = ((ASPxGridView)sender).GetRowValuesByKeyValue(e.Keys[0], new string[] { Colorariokeyfieldname });
            var currentId = Convert.ToInt32(e.NewValues[Colorariokeyfieldname]);

            // calcolo dell'id cantiere e collaboratore di riferimento
            var cantId = (int?)((ASPxGridView)sender).GetRowValuesByKeyValue(e.Keys[0], new string[] { "CantId" });
            if (cantId == 0)
                cantId = null;
            var colId = (int?)((ASPxGridView)sender).GetRowValuesByKeyValue(e.Keys[0], new string[] { "ColId" });
            if (colId == 0)
                colId = null;

            // è recuperato l'elenco dei col_cant_orario in base al tipo di totali richiesti
            var colCantOrarioList = new List<Col_Cant_Orario>();
            if (ChkShowWeeklyTotals.Checked) // se sono richiesti i totali settimanali
            {
                DateTime currentMonthDate = (DateTime)deTimesheet.Value;
                IEnumerable<Col_Cant_Orario> cantOrarioListCurrentMonth = RepoManager.ColCantOrarioRepo.Find(u => u.Anno_Orario == currentMonthDate.Year && u.Mese_Orario == currentMonthDate.Month);

                Col_Cant_Orario currentColCantOrario = cantOrarioListCurrentMonth.FirstOrDefault(u => Convert.ToInt32(u.Col_Id) == Convert.ToInt32(colId) && Convert.ToInt32(u.Cant_Id) == Convert.ToInt32(cantId));
                if (currentColCantOrario != default(Col_Cant_Orario))
                    colCantOrarioList.Add(currentColCantOrario);

                // inizializzazione dei giorni di inizio e fine mese oggetto del cartellino (per il recupero di eventuali dati del mese precedente e successivo)
                DateTime firstMonthDate = CommonService.GetFirstMonthDay((DateTime)deTimesheet.Value);
                DateTime lastMonthDate = CommonService.GetLastMonthDay((DateTime)deTimesheet.Value);
                DateTime startDate = firstMonthDate;
                DateTime endDate = lastMonthDate;

                // se la data di inizio mese non è un lunedì allora si recupera come data di inizio periodo l'ultimo lunedì del mese precedente
                if (startDate.DayOfWeek != DayOfWeek.Monday)
                    startDate = CommonService.GetLastDayOfWeekInMonth(firstMonthDate.AddMonths(-1), DayOfWeek.Monday);

                // se la data di fine mese non è una domenica allora si recupera come data di inizio periodo la prima domenica del mese successivo
                if (endDate.DayOfWeek != DayOfWeek.Sunday)
                    endDate = CommonService.GetFirstDayOfWeekInMonth(lastMonthDate.AddMonths(1), DayOfWeek.Sunday);

                // se è necessario recuperare anche i dati del mese precedente, li si recupera e li si aggiunge all'elenco (se presenti)
                if (startDate != firstMonthDate)
                {
                    DateTime searchDate = startDate.AddDays(-1);
                    IEnumerable<Col_Cant_Orario> cantOrarioList = RepoManager.ColCantOrarioRepo.Find(u => u.Anno_Orario == searchDate.Year && u.Mese_Orario == searchDate.Month);

                    Col_Cant_Orario prevColCantOrario = cantOrarioList.FirstOrDefault(u => Convert.ToInt32(u.Col_Id) == Convert.ToInt32(colId) && Convert.ToInt32(u.Cant_Id) == Convert.ToInt32(cantId));
                    if (prevColCantOrario != default(Col_Cant_Orario))
                        colCantOrarioList.Add(prevColCantOrario);
                }

                // se è necessario recuperare anche i dati del mese successivo, li si recupra e li si aggiunge all'elenco (se presenti)
                if (endDate != lastMonthDate)
                {
                    DateTime searchDate = endDate.AddDays(1);
                    IEnumerable<Col_Cant_Orario> cantOrarioList = RepoManager.ColCantOrarioRepo.Find(u => u.Anno_Orario == searchDate.Year && u.Mese_Orario == searchDate.Month);

                    Col_Cant_Orario nextColCantOrario = cantOrarioList.FirstOrDefault(u => Convert.ToInt32(u.Col_Id) == Convert.ToInt32(colId) && Convert.ToInt32(u.Cant_Id) == Convert.ToInt32(cantId));
                    if (nextColCantOrario != default(Col_Cant_Orario))
                        colCantOrarioList.Add(nextColCantOrario);
                }
            }
            else
            {
                // recupero il col orario (se ho l'id allora viene recuperato dal database, altrimenti viene generato da nuovo) per il mese corrente
                Col_Cant_Orario currentColCantOrario = currentId != 0 ? RepoManager.ColCantOrarioRepo.FirstOrDefault(u => u.Col_Cant_Orario_Id == currentId) : default(Col_Cant_Orario);

                // ... e lo si aggiunge alla lista di processo
                if (currentColCantOrario != default(Col_Cant_Orario))
                    colCantOrarioList.Add(currentColCantOrario);
            }



            // calcolo dell'id collaboratore/cantiere
            e.NewValues["ColId"] = ((ASPxGridView)sender).GetRowValuesByKeyValue(e.Keys[0], new string[] { "ColId" });
            e.NewValues["CantId"] = ((ASPxGridView)sender).GetRowValuesByKeyValue(e.Keys[0], new string[] { "CantId" });

            // popolo una nuova entità timesheet con i dati presenti in griglia
            bool isDecimalHours = SelectedTimesheetOptions.Any(opz => opz == ShowDecimalHoursOptionValue);
            var tmpTimesheet = new TimesheetModuleItem(isDecimalHours);
            PopulateTimesheetFromNewValue(tmpTimesheet, e.NewValues, ChkShowWeeklyTotals.Checked);

            // i dati di orario sono impostati sull'oggetto col_orario che si andrà a scrivere
            RepoManager.ColCantOrarioRepo.TimesheetToColCantOrario(tmpTimesheet, colCantOrarioList, ChkShowWeeklyTotals.Checked);

            // per ogni col orario da processare
            foreach (Col_Cant_Orario currentColCantOrario in colCantOrarioList)
            {
                // aggiornamento dei dati prima di inserire o modificare
                RepoManager.ColCantOrarioRepo.SetEntityBeforeAddOrUpdate(currentColCantOrario);

                // si inserisce o si modifica il record a seconda della sua presenza
                if (currentColCantOrario.Col_Cant_Orario_Id == 0)
                    RepoManager.ColCantOrarioRepo.Add(currentColCantOrario, true);
                else
                    RepoManager.ColCantOrarioRepo.Update(currentColCantOrario, true);
            }

            // ricalcolo e aggiornamento del datasource della grigli
            LoadTimesheetDataSource((DateTime)deTimesheet.Value, cmbEntityType.Value.ToString());

            e.Cancel = true;
            TimesheetGridView.CancelEdit();
        }

        /// <summary>
        /// Handles the OnDataBound event of the TimesheetGridView control [Ricalcolo delle etichette dei giorni nella testata della griglia].
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void TimesheetGridView_OnDataBound(object sender, EventArgs e)
        {
            ReloadTimesheetDayCaption(PowerWebContext.GetFromSession<DateTime>("tsmMinDate_" + TimesheetGridView.ID));
            ManageWeeklyTotalPosition(PowerWebContext.GetFromSession<DateTime>("tsmMinDate_" + TimesheetGridView.ID));
        }

        /// <summary>
        /// Handles the OnInit event of the TimesheetGridView control [utilizzato per la gestione della customizzazione sui group summary].
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void TimesheetGridView_OnInit(object sender, EventArgs e)
        {
            // recupero la griglia su cui sto effettuando l'operazione
            var currentGrid = sender as ASPxGridView;

            // calcolo l'elenco delle personalizzazioni sulla visualizzazione dei totali
            var deltaCustomization = (HideDeltaTotalTimehseetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideDeltaTotalTimehseetEnum);
            var planCustomization = (HidePlanTotalTimesheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HidePlanTotalTimesheetEnum);
            var totalCustomization = (HidelTotalTotalTimehsheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HidelTotalTotalTimehsheetEnum);
            var justCustomization = (HideJustificationTotalTimehsheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideJustificationTotalTimehsheetEnum);
            var additionalCustomization = (HidelAdditionalTotalTimehsheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HidelAdditionalTotalTimehsheetEnum);
            var strNocCustomization = (HideStrNoctTotalTimehseetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideStrNoctTotalTimehseetEnum);
            var arrotCustomization = (HideArrotTotalTimehseetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideArrotTotalTimehseetEnum);
            var strCustomization = (HideStrTotalTimesheetEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideStrTotalTimesheetEnum);

            // costruzione dell'elenco dei group summary da togliere
            var summaryToDelete = new List<ASPxSummaryItem>();

            // ciclo su tutti i group summary presenti in griglia e selezione degli elementi da cancellare
            currentGrid.GroupSummary.ForEach(gs =>
            {
                // se sto elaborando la colonna delta e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                if (gs.Tag == DeltaSummaryDefaultTag && deltaCustomization == HideDeltaTotalTimehseetEnum.Hide)
                    summaryToDelete.Add(gs);
                else if ((gs.Tag == PlanSummaryDefaultTag || gs.Tag == PianoSummaryDefaultTag) && planCustomization == HidePlanTotalTimesheetEnum.Hide) // se sto elaborando la colonna piano e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                    summaryToDelete.Add(gs);
                else if (gs.Tag == TotalSummaryDefaultTag && totalCustomization == HidelTotalTotalTimehsheetEnum.Hide) // se sto elaborando la colonna totale e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                    summaryToDelete.Add(gs);
                else if ((gs.Tag == StrSummaryDefaultTag || gs.Tag == StrNotSummaryDefaultTag || gs.Tag == OrdinarySummaryDefaultTag || gs.Tag == JustificationSummaryDefaultTag) && additionalCustomization == HidelAdditionalTotalTimehsheetEnum.Hide) // se sto elaborando una delle colonne addizionali e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                    summaryToDelete.Add(gs);
                else if (gs.Tag == StrNotSummaryDefaultTag && strNocCustomization == HideStrNoctTotalTimehseetEnum.Hide) // se sto elaborando la colonna straordinari notturni e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                    summaryToDelete.Add(gs);
                else if (gs.Tag == ArrotSummaryDefaultTag && arrotCustomization == HideArrotTotalTimehseetEnum.Hide) // se sto elaborando la colonna arrotondamenti e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                    summaryToDelete.Add(gs);
                else if (gs.Tag == JustificationSummaryDefaultTag && justCustomization == HideJustificationTotalTimehsheetEnum.Hide) // se sto elaborando la colonna motivazioni e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                    summaryToDelete.Add(gs);
                else if (gs.Tag == StrSummaryDefaultTag && strCustomization == HideStrTotalTimesheetEnum.Hide) // se sto elaborando la colonna straordinari e la stessa è indicata per il nascondimento, la aggiungo all'elenco delle colonne da togliere
                    summaryToDelete.Add(gs);
            });

            // sono tolte dall'elenco dei group summary tutte le colonne marcate per la cancellazione dal ciclo precedente
            summaryToDelete.ForEach(gs => currentGrid.GroupSummary.Remove(gs));
        }

        #endregion

        #region Gestione delle rettifiche

        /// <summary>
        /// Evento scatenato al callback del pannello per l'inserimento/cancellazione della rettifica. 
        /// </summary>
        /// <param name="source">L'invocatore del metodo</param>
        /// <param name="e">I parametri dell'evento</param>
        protected void CbInsertCorrection_OnCallback(object source, CallbackEventArgs e)
        {
            const string errorMessageJSProperty = "cpErrorMessage";

            // pulizia dell'eventuale errore dell'oggetto di callback
            if (cbInsertCorrection.JSProperties.ContainsKey(errorMessageJSProperty))
                cbInsertCorrection.JSProperties.Remove(errorMessageJSProperty);

            // si procede con l'operazione passata come parametro al callback
            switch (e.Parameter)
            {
                case "create":
                    cbInsertCorrection.JSProperties.Add(errorMessageJSProperty, CreateCorrections());
                    break;
                case "delete":
                    cbInsertCorrection.JSProperties.Add(errorMessageJSProperty, DeleteCorrections());
                    break;
            }
        }

        /// <summary>
        /// Questo metodo genera, utilizzando i dati espressi nella GUI, le rettifiche richieste.
        /// </summary>
        /// <returns>Una stringa contenente l'eventuale messaggio d'errore da riportare; in caso di non errore viene ritornata una stringa vuota.</returns>
        private string CreateCorrections()
        {
            // inizializzazione del valore di ritorno del metodo
            string errorString = String.Empty;

            // recupero dell'elenco dei cartellini visualizzati
            var allTimesheets = PowerWebContext.GetFromSession<List<TimesheetModuleItem>>("tsmItems_" + TimesheetGridView.ID);

            // recupero l'elenco dei cartellini con le ore effettuate
            var hourTimesheets = allTimesheets != null
                ? allTimesheets.Where(ts => ts.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN)).ToList()
                : new List<TimesheetModuleItem>();

            // recupero l'elenco dei cartellini con l'elenco delle ore piano
            var planTimesheets = allTimesheets != null
                ? allTimesheets.Where(ts => ts.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN)).ToList()
                : new List<TimesheetModuleItem>();

            bool isDecimalHours = SelectedTimesheetOptions.Any(opz => opz == ShowDecimalHoursOptionValue);

            #region Convalida input del metodo

            // i valori di rettifica non possono essere entrambi a 0
            if (Convert.ToInt32(seUpDeltaThreshold.Value) == 0 && Convert.ToInt32(seDownDeltaThreshold.Value) == 0)
                errorString = BusinessService.GetLocalizedString(PowerWebResources.ERR_LIMITI_DELTA_NON_CORRETTI);

            // devono essere visualizzati dei cartellini per poter proseguire
            if (String.IsNullOrEmpty(errorString))
                if (!hourTimesheets.Any())
                    errorString = BusinessService.GetLocalizedString(PowerWebResources.ERR_NO_CARTELLINI_ELABORABILI);

            #endregion

            // si prosegue con l'elaborazione solamente se non si sono verificati errori nella convalida dei dati
            if (String.IsNullOrEmpty(errorString))
            {
                // nome base del campo giorno nel cartellino (utilizzato per il recupero del numero di ore
                // previste nello stesso)
                const string dayBasePropretyName = "Day";

                // recupero il mese in elaborazione così da stabilire quanti giorni sono disponibili per il ciclo (recuperandolo dalle prime ore effettuate disponibili);
                // per questo recupero il primo giorno del mese, aggiungo un mese alla data e sottraggo un giorno
                // così da ottenere in ogni caso (30, 31, 28 e bisestili) l'ultimo giorno del mese
                var firstHourTimesheet = hourTimesheets.FirstOrDefault();
                var monthLastDate = CommonService.GetLastMonthDay(new DateTime(firstHourTimesheet.StartDate.Year, firstHourTimesheet.StartDate.Month, 1));

                // inizializzazione della lista di rettifiche da generare (i dati saranno aggiunti in blocco al termine del ciclo dei collaboratori)
                var correctionsToAdd = new List<Reg>();

                // recupero tutti gli id collaboratori che hanno delle ore effettuate
                var hourTimesheetsColId = hourTimesheets.Select(ts => ts.ColId).Distinct();

                // ciclo su ogni collaboratore recuperato
                foreach (var colId in hourTimesheetsColId)
                {
                    // recupero tutte i record cartellino con ore effettuate collegate al collaboratore
                    int currentColId = colId; // è recuperata la variabile per evitare problemi sul foreach per certi compilatori (suggerimento resharper)
                    var colHourTimesheets = hourTimesheets.Where(ts => ts.ColId == currentColId).ToList();

                    // si elaborano solamente i collaboratori che hanno dei record di ore effettuati;
                    // se non ci sono record con ore effettuate significa che si stanno visualizzando solamente le ore piano
                    // e quindi non si è in grado di effettuare le rettifiche
                    if (colHourTimesheets.Any())
                    {
                        #region Calcolo del totale delle ore previste

                        // inizializzo il dizionario che conterrà, giorno per giorno, il totale in minuti delle ore effettuate;
                        // come chiave il dizionario avrà il numero del giorno calcolato e come valore la somma dei minuti effettuati in quel giorno
                        var totalMinutesDictionary = new Dictionary<int, int>();

                        // ciclo su tutti i record con le ore effettuate per calcolare il totale delle stesse giorno per giorno
                        foreach (var tsheet in colHourTimesheets)
                        {
                            // ciclo su tutti i giorni del mese in elaborazione
                            for (int i = 1; i <= monthLastDate.Day; i++)
                            {
                                // reucpero il nome della proprietà che contiene il numero di ore effettuate
                                string dayPropertyName = String.Format("{0}{1}", dayBasePropretyName, i.ToString("00"));

                                // recupero il numero di ore effettuate per quel giorno secondo il cartellino
                                var timesheetDayDuration = Convert.ToDouble(CommonService.GetPropertyValue(tsheet, dayPropertyName));

                                // calcolo il numero di minuti corrispondente al numero di ore effettuate (possono essere espresse in sessantesimi o in centesimi)

                                var timesheetDayMinutes = CommonService.FromHoursToMinutes(timesheetDayDuration, isDecimalHours);

                                // aggiungo al dizionario dei totali il numero di minuti calcolato precedentmente;
                                // se il giorno è già stato censito allora si procede alla somma; altrimenti si genera il giorno
                                // e successivamente si aggiunge il valore
                                if (!totalMinutesDictionary.ContainsKey(i))
                                    totalMinutesDictionary.Add(i, 0);

                                totalMinutesDictionary[i] += timesheetDayMinutes;
                            }
                        }

                        #endregion

                        #region Calcolo del piano delle ore previste

                        // recupero le ore previste per il collaboratore in elaborazione
                        var planTimesheet = planTimesheets.FirstOrDefault(ts => ts.ColId == currentColId);

                        #endregion

                        #region Calcolo del delta tra le ore previste e le ore effettuate

                        // inizializzazione del dizionario che conterrà il delta in minuti tra le ore effettuate e le ore previste giorno per giorno;
                        // come chiave il dizionario avrà il numero del giorno e come valore il delta in minuti del giorno stesso
                        var deltaMinutesDictionary = new Dictionary<int, int>();

                        // inizializzazione del dizionario che contiene le ore piano giorno per giorno;
                        // come chiave il dizionario avrà il numero del giorno e come valore il totale in minuti del piano
                        var planMinutesDictionary = new Dictionary<int, int>();

                        // ciclo su tutti i giorni del mese in elaborazione
                        for (int i = 1; i <= monthLastDate.Day; i++)
                        {
                            // reucpero il nome della proprietà che contiene il numero di ore effettuate
                            string dayPropertyName = String.Format("{0}{1}", dayBasePropretyName, i.ToString("00"));

                            // calcolo i minuti previsti per il giorno in elaborazione
                            double planHours = planTimesheet != null ? Convert.ToDouble(CommonService.GetPropertyValue(planTimesheet, dayPropertyName)) : 0;
                            var planMinutes = CommonService.FromHoursToMinutes(planHours, isDecimalHours);

                            // calcolo il delta per il giorno in elaborazione sottraendo alle ore lavorate le ore piano
                            var deltaMinutes = totalMinutesDictionary[i] - planMinutes;

                            // aggiungo il delta del giorno calcolato al dizionario del delta per il mese in elaborazione;
                            // se il giorno non è già presente sul dizionario lo si aggiunge; in caso contrario lo si aggiorna (anche se condizione teoricamente non verificabile)
                            if (!deltaMinutesDictionary.ContainsKey(i))
                                deltaMinutesDictionary.Add(i, deltaMinutes);
                            else
                                deltaMinutesDictionary[i] = deltaMinutes;

                            // aggiungo il piano giornaliero al dizionario per il mese in elaborazione:
                            // se il giorno non è presente sul dizionario lo si aggiunge; in caso contrario lo si aggiorna (anche se condizione teoricamente non verificabile)
                            if (!planMinutesDictionary.ContainsKey(i))
                                planMinutesDictionary.Add(i, planMinutes);
                            else
                                planMinutesDictionary[i] = planMinutes;
                        }

                        #endregion

                        #region Verifica del delta e delle soglie ed eventuale generazione delle rettifiche

                        // ciclo di elaborazione dei delta calcolati
                        foreach (var dayDeltaMinutes in deltaMinutesDictionary)
                        {
                            // se i minuti per il giorno in elaborazione sono compresi nelle soglie, sono diversi da 0 e l'orario previsto è 0
                            // allora si provvede a generare una rettifica tale da annullare il delta
                            if (dayDeltaMinutes.Value != 0 && dayDeltaMinutes.Value >= Convert.ToInt32(seDownDeltaThreshold.Value) && dayDeltaMinutes.Value <= Convert.ToInt32(seUpDeltaThreshold.Value))
                            {
                                // calcolo della durata della rettifica di modo che annulli il delta
                                var correctionDuration = TimeSpan.FromMinutes(Math.Abs(dayDeltaMinutes.Value));

                                // calcolo della direzione della rettifica
                                var correctionDirection = dayDeltaMinutes.Value > 0 ? CorrectionTypeEnum.CorrectionMinus : CorrectionTypeEnum.CorrectionPlus;

                                // calcolo della data in cui inserire la rettifica
                                var correctionDate = new DateTime(monthLastDate.Year, monthLastDate.Month, dayDeltaMinutes.Key);

                                // aggiunta della rettifica da generare alla lista da inserire nel database
                                correctionsToAdd.Add(RepoManager.RegRepo.GenerateCorrectionReg(currentColId, correctionDate, correctionDirection, correctionDuration));
                            }
                        }

                        #endregion
                    }
                }

                #region Scrittura delle eventuali rettifiche su database

                // se al termine del ciclo sono state generate delle rettifiche allora si procede alla loro scrittura nel database
                if (correctionsToAdd.Any())
                {
                    // salvataggio nel database delle rettifiche
                    RepoManager.RegRepo.Add(correctionsToAdd, true);
                }

                #endregion
            }

            // ritorno del valore del metodo
            return errorString;
        }

        /// <summary>
        /// Questo metodo elimina, utilizzando i dati espressi nella GUI, le rettifiche richieste
        /// </summary>
        /// <returns>Una stringa contenente l'eventuale messaggio d'errore da riportare; in caso di non errore viene ritornata una stringa vuota.</returns>
        private string DeleteCorrections()
        {
            // inizializzazione del valore di ritorno del metodo
            string errorString = String.Empty;

            // recupero dell'elenco dei cartellini visualizzati
            var allTimesheets = PowerWebContext.GetFromSession<List<TimesheetModuleItem>>("tsmItems_" + TimesheetGridView.ID);

            #region Convalida input del metodo

            // devono essere visualizzati dei cartellini per poter proseguire
            if (allTimesheets == null)
                errorString = BusinessService.GetLocalizedString(PowerWebResources.ERR_NO_CARTELLINI_ELABORABILI);

            if (String.IsNullOrEmpty(errorString))
                if (!allTimesheets.Any())
                    errorString = BusinessService.GetLocalizedString(PowerWebResources.ERR_NO_CARTELLINI_ELABORABILI);

            #endregion

            // se non si sono verificati errori nella convalida input
            if (String.IsNullOrEmpty(errorString))
            {
                // recupero primo giorno del mese in elaborazione utilizzando il primo cartellino a disposizione
                var firstTimesheet = allTimesheets.FirstOrDefault();
                var monthFirstDay = CommonService.GetFirstMonthDay(firstTimesheet.StartDate);

                // calcolo il giorno di fine del mese in elaborazione
                var monthLastDay = CommonService.GetLastMonthDay(monthFirstDay);

                // inizializzazione della lista delle registrazioni rettifica da cancellare
                var correctionsToDelete = new List<Reg>();

                // per ogni collaboratore selezionato si verificano le registrazioni rettifica e se presenti si aggiungono
                // all'elenco per cancellazione
                foreach (var colRegsToDelete in SelectedColsId
                    .Select(currentColId =>
                        RepoManager.RegRepo.Find(reg => reg.Registrazione_Data_Ora_Fis_Reg >= monthFirstDay
                            && reg.Registrazione_Data_Ora_Fis_Reg <= monthLastDay
                            && reg.Col_Id == currentColId
                            && reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimesheet).ToList()).Where(colRegsToDelete => colRegsToDelete.Any()))
                {
                    correctionsToDelete.AddRange(colRegsToDelete);
                }

                // al termine del recupero delle registrazioni rettifica, se esistono dei dati da cancellare, si procede alla loro cancellazione
                RepoManager.RegRepo.Delete(correctionsToDelete);
            }

            // ritorno del valore del metodo
            return errorString;
        }

        #endregion
    }
}