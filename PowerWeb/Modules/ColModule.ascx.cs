using Business;
using Business.ExportExcelEngine;
using Business.Repository;
using Common;
using DevExpress.Compression;
using DevExpress.Web.ASPxCallback;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxEditors;
using DevExpress.Web.ASPxFormLayout;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.ASPxPanel;
using DevExpress.Web.ASPxUploadControl;
using DevExpress.Web.Data;
using DevExpress.XtraPrinting.Native;
using Domain;
using Domain.Extensions;
using log4net;
using Reports;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PowerWeb.Modules
{

    public partial class ColModule : BaseGridModule, IPrintModule, ILogModule, IGeoLocationModule
    {
        #region Private Constants and Read Only Fields

        //DEFINIZIONI      
        private const Col ColStub = null;
        private const String Keyfieldname = "Col_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(ColModule));

        /// <summary>
        /// Il nome dello specializzato dell'export 56 lanciato dal presente modulo
        /// </summary>
        private const string Export56Specialized = "ExportExcelSpecialized.ExportExcelSpecializedActivity";

        #endregion

        private string _newImportFile
        {
            get
            {
                var importFile = PowerWebContext.GetFromSession<string>("ImportFile" + gvCol.ID);
                if (importFile == null)
                {
                    importFile = String.Empty;
                    PowerWebContext.SetToSession("ImportFile" + gvCol.ID, importFile);
                }
                return importFile;

            }

            set
            {
                PowerWebContext.SetToSession("ImportFile" + gvCol.ID, value);
            }
        }

        private int CustomizationVersion;

        /// <summary>
        /// Recupera dai parametri dell'applicativo la chiave di bing utilizzata per effettuare la gelolocalizzazione.
        /// </summary>
        /// <value>
        /// La chiave di bing utilizzata per effettuare la geolocalizzazione.
        /// </value>
        public string BingKey
        {
            get
            {
                return RepoManager.ParamRepo.ParametersRow.BingKey;
            }
        }

        public bool IsInBatchMode
        {
            get { return GridView.SettingsEditing.Mode == GridViewEditingMode.Batch; }
        }

        public override ASPxGridView GridView
        {
            get { return gvCol; }
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
                return null;
            }
        }

        public List<int> ColSelezionati
        {
            get
            {
                var colsel = PowerWebContext.GetFromSession<List<int>>("ColSelezionati" + gvCol.ID);
                if (colsel == null)
                {
                    colsel = new List<int>();
                    PowerWebContext.SetToSession("ColSelezionati" + gvCol.ID, colsel);
                }
                return colsel;

            }

            set
            {
                List<int> list = value;
                if (list != null)
                    PowerWebContext.SetToSession("ColSelezionati" + gvCol.ID, list);
            }
        }

        public override void ResetSession()
        {
            base.ResetSession();
            ColSelezionati = new List<int>();
        }

        public override ASPxGridView GridViewDetail
        {
            get { return null; }
        }

        public override PowerFormTemplate EditFormTemplate
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID);
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

        protected void Page_Load(object sender, EventArgs e)
        {
            gvCol.JSProperties["cpPageChanged"] = 0;
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            //PowerWebService.GenerateGridColumns(GridView, MetaFieldDescriptors);

            GridView.KeyFieldName = Keyfieldname;

            BtnTrips.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_ELAB_VIAGGI);
            BtnTrips.ClientVisible = RepoManager.ParamRepo.ParametersRow.Abilita_Viaggi;
            BtnDeleteTrips.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_DELETE_VIAGGI);
            BtnDeleteTrips.ClientVisible = RepoManager.ParamRepo.ParametersRow.Abilita_Viaggi;
            BtnLaunchExport56.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_EXPORT_56);
            BtnLaunchExport56.ClientVisible = CalculateBtnExport56Visibility();
            lblResult.Text = ".";
            lblDal.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_PERIODO_DAL);
            lblAl.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_AL);
            BtnTrips.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_ELAB_VIAGGI);
            lblNColSel.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_NUMERO_COL_SEL);
            deFrom.Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
            LblCol.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE);
            BtnLaunchElaborate.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_ELAB_REG);
            BtnDeleteRoundings.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_DELETE_ARROTONDAMENTI);
            BtnRoundings.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_ELAB_ARROTONDAMENTI);
            // Visualizza i bottoni relativi all'arrotondamento per durata solo se l'arrotondamento è abilitato nei parametri e se il metodo è durata
            BtnDeleteRoundings.ClientVisible = RepoManager.ParamRepo.ParametersRow.Abilita_Arrotondamenti && (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento == RoundingMethodEnum.Duration;
            BtnRoundings.ClientVisible = RepoManager.ParamRepo.ParametersRow.Abilita_Arrotondamenti && (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento == RoundingMethodEnum.Duration;

            // se tutti i pulsanti di elaborazione sono nascosti allora si nasconde anche il form layout che li contiene
            if (!BtnTrips.ClientVisible && !BtnDeleteTrips.ClientVisible && !BtnLaunchExport56.ClientVisible)
                FlElaborate.Visible = false;


            PowerWebService.FillGridLabels(EntityType, GridView);
            PowerWebService.FillComboboxes(GridView);




            // verifico se è attiva la pesonalizzazione riguardante l'Import Custom per UniLabor
            CustomizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LaunchImportColVersionEnum);
            if (CustomizationVersion == (int)LaunchImportColVersionEnum.ImportUniLabor)
            {
                flImport.Visible = true;
            }

            #region INIZIO Gestione CASCADE

            GridViewDataComboBoxColumn nascLuogo = gvCol.Columns["Nascita_Luogo_Col"] as GridViewDataComboBoxColumn;
            if (nascLuogo != null)
                nascLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnNascLuogoChanged";
            GridViewDataComboBoxColumn resLuogo = gvCol.Columns["Residenza_Luogo_Col"] as GridViewDataComboBoxColumn;
            if (resLuogo != null)
                resLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnResLuogoChanged";
            GridViewDataComboBoxColumn domLuogo = gvCol.Columns["Domicilio_Luogo_Col"] as GridViewDataComboBoxColumn;
            if (domLuogo != null)
                domLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnDomLuogoChanged";
            GridViewDataComboBoxColumn nascCodLuogo =
                gvCol.Columns["Codice_Nascita_Luogo_Col"] as GridViewDataComboBoxColumn;
            if (nascCodLuogo != null)
                nascCodLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnNascCodLuogoChanged";
            GridViewDataComboBoxColumn resCodLuogo =
                gvCol.Columns["Codice_Residenza_Luogo_Col"] as GridViewDataComboBoxColumn;
            if (resCodLuogo != null)
                resCodLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnResCodLuogoChanged";
            GridViewDataComboBoxColumn domCodLuogo =
                gvCol.Columns["Codice_Domicilio_Luogo_Col"] as GridViewDataComboBoxColumn;
            if (domCodLuogo != null)
                domCodLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnDomCodLuogoChanged";

            #endregion

            if (!Page.IsCallback && !Page.IsPostBack)
                ResetSession();

            BindGrid();

            // è messa in lingua la gestione del form layout di gestione della generazione nuova associazione
            LocalizeAddNewAssociationLayout();

            // compilazione dei combobox presenti nel form layout di gestione della generazione nupova associazione
            ManageAddNewAssociationCombos();

            // compilazione della combobox dei collaboratoti per l'elaborazione
            FillColToElaborateComboBox();

            // se è attiva la personalizzazione della visualizzazione del combo collaboratore per elaborazione allora
            // lo visualizzo
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowComboColEnum);
            CmbColToElaborate.Visible = customizationVersion == (int)ShowComboColEnum.Enable;
            LblCol.Visible = customizationVersion == (int)ShowComboColEnum.Enable;

            // gestione della visualizzazione e della localizzazione della sezione di inizializzazione del monte minuti
            ManageDisplayAndLocalizeInitializeMinutesAmmountSection();

        }

        /// <summary>
        /// Imposta in lingua il form layout per l'aggiunta di una nuova associazione.
        /// </summary>
        private void LocalizeAddNewAssociationLayout()
        {
            // localizzazione dell'etichetta del form layout
            var layoutGroup = FlAddNewAssociation.Items[0] as LayoutGroup;
            if (layoutGroup != null) layoutGroup.Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_AGGIUNGI_ASSOCIAZIONE);

            // localizzazione delle label
            LblNewAssociationPru.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_PRU);
            LblNewAssociationCol.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE);
            LblNewAssociationDate.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_DATA);
            BtnAddNewAssociation.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_AGGIUNGI_ASSOCIAZIONE);

        }

        /// <summary>
        /// Gestisce il bind e il fill del combobox dei collaboratori.
        /// </summary>
        private void FillColToElaborateComboBox()
        {
            // compilazione del combobox di visualizzazione dei collaboratori
            PowerWebService.FillComboboxes(CmbColToElaborate, "Search_Col_Id_2");

            if (!CmbColToElaborate.ReadOnly)
            {
                EditButton btnEdit = new EditButton("X");
                CmbColToElaborate.Buttons.Add(btnEdit);

                CmbColToElaborate.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
            }

        }

        /// <summary>
        /// Gestisce il bind e il fill dei combobox presenti nel form layout per l'aggiunta di una nuova associazione.
        /// </summary>
        private void ManageAddNewAssociationCombos()
        {
            // compilazione del combobox di visualizzazione delle pru
            Pru pruStub = null;
            PowerWebService.FillComboboxes(CmbPruToAssociate, CommonService.GetPropertyName(() => pruStub.Pru_Id));
            if (!CmbPruToAssociate.ReadOnly)
            {
                EditButton btnEdit = new EditButton("X");
                CmbPruToAssociate.Buttons.Add(btnEdit);

                CmbPruToAssociate.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
            }

            // compilazione del combobox di visualizzazione dei collaboratori
            PowerWebService.FillComboboxes(CmbColToAssociate, "Search_Col_Id");
            if (!CmbColToAssociate.ReadOnly)
            {
                EditButton btnEdit = new EditButton("X");
                CmbColToAssociate.Buttons.Add(btnEdit);

                CmbColToAssociate.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
            }
        }

        protected void cbAll_Init(object sender, EventArgs e)
        {
            ASPxCheckBox chk = sender as ASPxCheckBox;
            ASPxGridView grid = (chk.NamingContainer as GridViewHeaderTemplateContainer).Grid;
            chk.Checked = (grid.Selection.Count == grid.VisibleRowCount);
        }

        protected void cbPage_Init(object sender, EventArgs e)
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

        private void BindGrid()
        {
            //non viene più usato il linq ma uso una GETALL
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvCol.KeyFieldName = Keyfieldname;
            IQueryable<Col> currDataSource = Enumerable.Empty<Col>().AsQueryable();
            var emptyList = Enumerable.Empty<Col>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.ColRepo.GetAll(true).AsQueryable();
                gvCol.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvCol.DataSource = emptyList;
        }

        protected void gvCol_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
            //    gvCol.KeyFieldName = KEYFIELDNAME;
            //    LinqServerModeDataSource serverMode = new LinqServerModeDataSource();
            //    serverMode.ContextTypeName = "PowerWebEntities.Data";
            //    serverMode.TableName = "Col_V";

            //    GridView.DataSource = serverMode;

            //    serverMode.Selecting += linq_Selecting;
        }

        private Type _entityType = typeof(Col);

        public override Type EntityType
        {
            get { return _entityType; }
        }

        #region gvCol-InitNewRow-RowValidating-RowInserting-RowUpdating-RowDeleting

        protected void gvCol_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Col initCol = RepoManager.ColRepo.Init();
                PowerWebService.FillGridProperties(initCol, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvCol_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Col initCol = new Col();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvCol.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentCol = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentCol, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(initCol, e.NewValues);
            PowerWebService.FillEntityKey(initCol, e.Keys, Keyfieldname);
            RepoManager.ColRepo.SetEntityBeforeAddOrUpdate(initCol);
            PowerWebService.AddValidationErrors(RepoManager.ColRepo.Check(initCol, e.IsNewRow), e.Errors, gvCol, typeof(ColModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvCol_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("COL-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Col newCol = new Col();
            PowerWebService.FillEntityProperties(newCol, e.NewValues);
            RepoManager.ColRepo.SetEntityBeforeAddOrUpdate(newCol);

            // prima di aggiungere il collaboratore, se è richiesto dalla param allora incremento di uno il codice collaboratore
            // (il fatto che si possa fare è già stabilito dalla check)
            // [non effettuo conversioni in quanto a questo punto è già stata effettuata una check che eventualmente controlla la presenza di 
            // codici collaboratore stringa]
            if (RepoManager.ParamRepo.ParametersRow.Attiva_Num_Aut_Col)
            {
                IEnumerable<int> codColList = RepoManager.ColRepo.DbSet.Select(col => col.Codice_Collaboratore).ToList().Select(int.Parse).ToList();
                int codColMaxNum = (codColList.Any() ? codColList.Max() : 0) + 1;
                newCol.Codice_Collaboratore = CommonService.AggiungiSpaziASinistraSeStringaNumerica(codColMaxNum.ToString(), 20);
            }

            RepoManager.ColRepo.Add(newCol, true);
            e.Cancel = true;
            gvCol.CancelEdit();
            //syncCollInAppDB(newCol, 0, null);
        }

        protected void gvCol_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("COL-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvCol.KeyFieldName]);
            Col currentCol = RepoManager.ColRepo.Single(u => u.Col_Id == currentId);
            Col oldCol = RepoManager.ColRepo.DbSet.AsNoTracking().FirstOrDefault(u => u.Col_Id == currentId);

            if (RepoManager.ParamRepo.ParametersRow.File_Col_Var)
            //Se in Tab PARAM è stato attivato il Flag di Gestione della Scrittura dei Record Variati in COL_VAR
            {
                //Scrive il Record in Tab COL_VAR com'era PRIMA di Modificarlo
                Col_Var colVar = new Col_Var();
                CommonService.DuplicateEntity(currentCol, colVar);
                RepoManager.Col_VarRepo.Add(colVar);
            }

            PowerWebService.FillEntityProperties(currentCol, e.NewValues);
            RepoManager.ColRepo.SetEntityBeforeAddOrUpdate(currentCol);
            //syncCollInAppDB(currentCol, 2, oldCol);
            #region Singiola Reg (Passaggi) (Se cambiata da/a Singola_Reg (Passaggi) indico al sistema di rielaborarne le relative Registrazioni in Tab PendingElab

            var singolaRegPropertyName = CommonService.GetPropertyName(() => ColStub.Singola_Reg);

            if (e.NewValues.Contains(singolaRegPropertyName))
            {
                bool SingolaRegColOld = (bool)e.OldValues[singolaRegPropertyName];
                bool SingolaRegColNew = (bool)e.NewValues[singolaRegPropertyName];

                if (SingolaRegColOld != SingolaRegColNew)
                //Se il Collaboratore ha cambiato la Singola_Reg (Passaggio) 
                //Chiamo la Routine che leggendo le Registrazioni del Collaboratore scrive nella Tabella PendingElab
                //La data Minima (maggiore della dataBlocco) e Massima delle sue registrazioni per rielaborare le relative REG
                {
                    RepoManager.ColRepo.AddPendingElabForActivity(currentCol);
                }
            }
            RepoManager.ColRepo.SaveChanges();
            e.Cancel = true;
            gvCol.CancelEdit();

            #endregion

        }

        protected void gvCol_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("COL-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvCol.KeyFieldName]);
            Col currentCol = RepoManager.ColRepo.Single(u => u.Col_Id == currentId);
            RepoManager.ColRepo.Delete(currentCol, true);
            e.Cancel = true;
        }


        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => ColStub.Data_Registrazione_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => ColStub.DataOraUltimaModifica_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => ColStub.Assegni_Famigliari_Fine_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => ColStub.Assegni_Famigliari_Inizio_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => ColStub.Data_Disponibilita_Fine_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => ColStub.Data_Disponibilita_Inizio_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => ColStub.Nascita_Data_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => ColStub.Scadenza_Patente_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => ColStub.Data_Proroga_Contratto) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => ColStub.Straniero_Scadenza_Permesso_Col))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, ASPxPanel customOptionsPanel = null)
        {
            List<Col> cols = CommonService.ConvertTo<Col>(items);
            XRCol colReport = new XRCol(cols, PowerWebService.ConvertTabPageExtendedToString(selectedTabs));
            return new ExtXtraReport { Report = colReport, PictureBox = colReport.CompanyLogo };
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }

        protected void gvCol_ParseValue(object sender, ASPxParseValueEventArgs e)
        {
            if (!gvCol.IsNewRowEditing && e.FieldName == "Durata_Min_Ril_Col")
                e.Value = TimeSpanFromString(e.Value);
        }

        private TimeSpan TimeSpanFromString(Object value)
        {
            if (value == null || String.IsNullOrEmpty((String)value))
                return TimeSpan.Zero;

            var t = Convert.ToDateTime(value.ToString());

            return new TimeSpan(t.Hour, t.Minute, 0);

        }

        #region Gestione dell'import delle anagrafiche collaboratore

        protected void cUplImportCommand_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {
            Dictionary<string, string> errors = null;
            e.Result = "Error|Title|Message";
            //Verifica che il Percorso (NOTA BENE : é quello in cui salva sul SERVER il File da trattare) in PARAM non sia vuoto
            if (!String.IsNullOrEmpty(Common.Properties.Settings.Default.Files_Input_Path))
            {
                FileInfo fileInfo = new FileInfo(_newImportFile);
                if (fileInfo.Exists)
                //Se sul Server esiste il File da elaborare, Lancia l'Import dei Dati
                //e poi segnala che l'Import è terminato e CANCELLA il File dal ServerMapPath
                {
                    BusinessService.ImportDataStatusDictionary.Add(PowerWebContext.Current.User, new KeyValuePair<double, string>(0, BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORT_INIZIATO)));
                    try
                    {

                        // Lancio l'import del file csv
                        errors = RepoManager.ColRepo.ImportFromCSV(File.ReadAllText(_newImportFile, Encoding.GetEncoding(850)).Split('\r'));
                        foreach (var entry in errors)
                        {
                            _log.Error(entry.Value + ", " + entry.Key);
                        }

                    }
                    catch (DbEntityValidationException ex)
                    {
                        var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);

                        var fullErrorMessage = string.Join("; ", errorMessages);
                        var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                        throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);

                        ////controllo che il messaggio di errore sia contenuto nel dizionario degli "errori"
                        //if (!cUplImportCommand.JSProperties.ContainsKey("cpImportError"))
                        //    cUplImportCommand.JSProperties.Add("cpImportError", String.Empty);

                        //cUplImportCommand.JSProperties["cpImportError"] = ex.Message;
                    }
                    finally
                    {
                        // in ogni caso al termine dell'importazione viene backuppato il file importato
                        BusinessService.BackupProcessedFiles(new List<string>() { _newImportFile }, Server.MapPath(Common.Properties.Settings.Default.Files_Input_Col_Backup_Path));
                    }

                    // Al termine dell'elaborazione viene ripulito il dizionario con i
                    if (BusinessService.ImportDataStatusDictionary.ContainsKey(PowerWebContext.Current.User))
                        BusinessService.ImportDataStatusDictionary.Remove(PowerWebContext.Current.User);

                    e.Result = String.Format("Done|{0}", BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORT_TERMINATO));

                    System.Threading.Thread.Sleep(2000);
                }
            }
        }

        protected void upldImport_FileUploadComplete(object sender, FileUploadCompleteEventArgs e)
        // salva il file di import uploadato nella cartella di destinazione del server
        {
            _newImportFile = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path + String.Format("ColFile{0}{1}", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"), ".csv"));

            FileInfo fileInfo = new FileInfo(_newImportFile);
            if (!fileInfo.Exists)
            {
                #region Controllo ed eventuale conversione in csv del formato del file se è xls o xlsx o scrittura del file in caso di formato csv in entrata

                // se sto importando un file excel (cioè l'estensione non è .csv)
                if (Path.GetExtension(e.UploadedFile.FileName).ToUpper() != ".CSV")
                {
                    // salvo il file arrivatomi come parametro in un temporaneo
                    string tmpFileName = Server.MapPath(String.Format("{0}{1}{2}", Common.Properties.Settings.Default.Files_Input_Path, Path.GetFileNameWithoutExtension(Path.GetTempFileName()), Path.GetExtension(e.UploadedFile.FileName)));
                    e.UploadedFile.SaveAs(tmpFileName);

                    // conversione del file excel nel csv di destinazione
                    CommonService.ConvertExcelFileIntoCsv(tmpFileName, _newImportFile);

                    // al termine dell'operazione viene cancellato l'eventuale file temporaneo rimasto appeso
                    if (File.Exists(tmpFileName))
                        File.Delete(tmpFileName);

                }
                else
                {
                    e.UploadedFile.SaveAs(_newImportFile);
                }

                #endregion

            }
        }

        protected void cUplImportPing_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {

            if (BusinessService.ImportDataStatusDictionary.ContainsKey(PowerWebContext.Current.User))
            {
                var status = BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User];
                e.Result = String.Format("{0}|{1}", status.Key, status.Value);
            }
        }

        #endregion

        #region Gestione ottimizzata componente calendario
        protected void gvCol_CellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            SetupCalendarOwner(e.Editor as ASPxDateEdit);
        }

        protected void gvCol_AutoFilterCellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            SetupCalendarOwner(e.Editor as ASPxDateEdit);
        }

        void SetupCalendarOwner(ASPxDateEdit editor)
        {
            if (editor == null) return;
            editor.PopupCalendarOwnerID = "__ReferenceDateEdit";
        }
        #endregion

        #region Elaborazione viaggi per collaboratore

        protected void BtnTrips_CustomJSProperties(object sender, DevExpress.Web.ASPxClasses.CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_AGGIORNAMENTO));

            if (!e.Properties.ContainsKey("cpErrorMessage"))
                e.Properties.Add("cpErrorMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO));


            if (!e.Properties.ContainsKey("cpErrorMessageCol"))
                e.Properties.Add("cpErrorMessageCol",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE_NON_SEL));
        }

        protected void btnDeleteElaborate_CustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_AGGIORNAMENTO));

            if (!e.Properties.ContainsKey("cpErrorMessage"))
                e.Properties.Add("cpErrorMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO));

        }

        protected void cPing_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {
            //  Restituisce quanto caricato nell'ImportDataStatusDictionary dalla Routine di CALCULATE 
            //  StatusKey : Contiene il Nome della Tabella che si sta importando in quel momento                               
            //  Valore    : Contiene la Percentuale (calcolata in base al N° di Tabelle da caricare) di Caricamento rispetto al Totale
            if (BusinessService.ElaborateStatusDictionary.ContainsKey(PowerWebContext.Current.User))
            //Se ci sono dati nel DictionaryStatus allora li carica nel Risultato da mostrare a Video
            {
                var status = BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User];
                e.Result = String.Format("{0}|{1}", status.Key, status.Value);
            }
        }

        protected void cTrips_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {
            BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = false;
            ASPxGridView grid = gvCol;

            DateTime from = deFrom.Date;
            if (deTo.Date == DateTime.MinValue)
                deTo.Date = deFrom.Date;
            // la data di destinazione è il finale (le 23:59 della data indicata), altrimenti nella ricerca si perde un giorno
            DateTime to = new DateTime(deTo.Date.Year, deTo.Date.Month, deTo.Date.Day, 23, 59, 0);

            // se c'è qualcosa da processare
            if ((from != DateTime.MinValue && to != DateTime.MinValue) && ((ColSelezionati.Count > 0) || CmbColToElaborate.SelectedIndex != -1))
            {
                // per problemi di occupazione risorse, prima di eseguire queste operazioni che sono pesanti
                // si segnala al garbage collector di liberare le risorse eventualmente da processare
                GC.Collect();

                // aggiornamento delle date in base al parametro notturno
                BusinessService.ManageNocturneStartEndDate(ref from, ref to);

                // l'unità minima di elaborazione è un giorno e quindi se le date/ore in elaborazione sono uguali
                // allora l'ora to viene spostato al giorno successivo (a inizio giornata così da comprendere solo il giorno
                // in elaborazione)
                if (from == to)
                {
                    to = to.AddDays(1);
                    to = to.AddMinutes(1);
                }

                if (e.Parameter == "elaborateTrips")
                {

                    #region Eleborazione viaggi

                    BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(0, "Elaborazione Viaggi Iniziata");

                    // calcolo di tutti i collaboratori coinvolti
                    List<int> regIds = new List<int>();

                    //lista che contiene gli ID dei collaboratori selezionati con il Combo Box
                    List<int> comboColIds = new List<int>();
                    // se ho selzionato un collaboratore lo inserisco nella lista 
                    if (CmbColToElaborate.SelectedIndex != -1)
                        comboColIds.Add((int)CmbColToElaborate.SelectedItem.Value);



                    //ciclo su tutti i collaboratori selezionati, dando priorità alla combobox altrimenti prendo i collaboratori selezionati mediante checkbox
                    foreach (var colId in comboColIds.Any() ? comboColIds : ColSelezionati)
                    {

                        //legge le Registrazioni per il Periodo Richiesto tra le quali generare eventualmente i Viaggi per i singoli collaboratori selezionati
                        var singleColRegIds = RepoManager.RegRepo.GetRegsIdByDateRangeByColNotBlocked(from, to, colId, false);
                        regIds.AddRange(singleColRegIds);

                    }

                    //Recupera le regv non bloccate all'interno del periodo selezionato e i passaggi
                    var regVs = RepoManager.Reg_VRepo.GetReg_VByDateRangeOrPassageByRegIds(from, to, regIds);

                    //Estraggo i viaggi 
                    var trips = regVs.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip).ToList();

                    //Li cancello prima dal db e poi dalla lista estratta

                    var toDeleteRegEIds = trips.Select(regv => regv.RegE).ToList();
                    var toDeleteRegUIds = trips.Select(regv => regv.RegU).ToList();

                    RepoManager.RegRepo.DeleteFromQuery(reg => reg.Registrazione_Data_Ora_Fis_Reg >= from &&
                                    reg.Registrazione_Data_Ora_Fis_Reg <= to &&
                                    reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip &&
                                    toDeleteRegEIds.Contains(reg.Reg_Id) ||
                                    toDeleteRegUIds.Contains(reg.Reg_Id));

                    regVs = regVs.Except(trips).ToList();

                    List<KeyValuePair<String, String>> errors = new List<KeyValuePair<string, string>>();

                    //Chiama il Calcolo dei Viaggi in RegV_Repository
                    if (regVs.Count() > 0)
                    {
                        BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] =
                            new KeyValuePair<double, string>(0, "Generazione Viaggi Iniziata");
                        //eseguo l'elaborazione ei viaggi

                        errors = RepoManager.Reg_VRepo.ElaborateTrips(regVs, true);
                    }

                    //if (BusinessService.ElaborateStatusDictionary.ContainsKey(PowerWebContext.Current.User))
                    //    BusinessService.ElaborateStatusDictionary.Remove(PowerWebContext.Current.User);

                    String message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA);

                    if (errors.Any() || BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User])
                        message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA_CON_SEGNALAZIONI);

                    e.Result = message;

                    #endregion

                }
                else if (e.Parameter == "deleteTrips")
                {
                    BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] =
                        new KeyValuePair<double, string>(0, "Cancellazione Viaggi Iniziata");

                    #region Cancellazione dei viaggi

                    List<Reg> tripsToDelete = new List<Reg>();

                    // lista che contiene gli ID dei collaboratori selezionati con il Combo Box
                    List<int> comboColIds = new List<int>();
                    // se ho selzionato un collaboratore lo inserisco nella lista 
                    if (CmbColToElaborate.SelectedIndex != -1)
                        comboColIds.Add((int)CmbColToElaborate.SelectedItem.Value);

                    //ciclo su tutti i collaboratori selezionati, dando priorità alla combobox altrimenti prendo i collaboratori selezionati mediante checkbox
                    foreach (var colId in comboColIds.Any() ? comboColIds : ColSelezionati)
                    {

                        //legge le Registrazioni per il Periodo Richiesto tra le quali generare eventualmente i Viaggi per i singoli collaboratori selezionati

                        RepoManager.RegRepo.DeleteFromQuery(reg => reg.Registrazione_Data_Ora_Fis_Reg >= from &&
                                    reg.Registrazione_Data_Ora_Fis_Reg <= to &&
                                    reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip &&
                                   reg.Col_Id == colId);

                    }

                    String message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA);

                    e.Result = message;

                    #endregion

                }

                // per problemi di occupazione risorse, al termine delle operazioni che sono pesanti
                // si segnala al garbage collector di liberare le risorse eventualmente da processare
                GC.Collect();
            }


        }

        #endregion

        #region Lancio Export 56

        /// <summary>
        /// Calcola e ritorna lo stato di visbiblità per il pulsante di lancio dell'export 56.
        /// </summary>
        /// <returns><c>true</c> se il pulsante va visualizzato; <c>false</c> in caso contrario</returns>
        protected bool CalculateBtnExport56Visibility()
        {

            // recupero del modello nella tab export
            var excelModel = RepoManager.Tab_Excel_ModelRepo.FirstOrDefault(mod => mod.Nome_Specializzato == Export56Specialized);

            //controllo se il modello è presente ed è attivato
            if (excelModel != null && excelModel.IsActive)
            {
                return true;
            }

            else return false;
        }

        protected void BtnLaunchExport56_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpErrorMessage"))
                e.Properties.Add("cpErrorMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO));
        }

        protected void BtnLaunchExport56_OnClick(object sender, EventArgs e)
        {
            string path = "";

            // calcolo del periodo richiesto di elaborazione
            DateTime from = deFrom.Date;
            if (deTo.Date == DateTime.MinValue)
                deTo.Date = deFrom.Date;
            DateTime to = new DateTime(deTo.Date.Year, deTo.Date.Month, deTo.Date.Day, 23, 59, 59);

            // recupero del modello nella tab export
            var excelModel = RepoManager.Tab_Excel_ModelRepo.FirstOrDefault(mod => mod.Nome_Specializzato == Export56Specialized);

            // si procede con l'elaborazione solamente se il modello è stato trovato
            if (excelModel != null)
            {
                BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = false;

                // calcolo del tipo export specializzato
                var currentType = Type.GetType(String.Format("Exports.{0}, Exports", excelModel.Nome_Specializzato));

                // se sono stati selezionati dei collaboratori

                if (ColSelezionati.Any() || CmbColToElaborate.SelectedIndex != -1)
                {
                    //lista che contiene gli ID dei collaboratori selezionati con il Combo Box
                    List<int> comboColIds = new List<int>();
                    // se ho selzionato un collaboratore lo inserisco nella lista 
                    if (CmbColToElaborate.SelectedIndex != -1)
                        comboColIds.Add((int)CmbColToElaborate.SelectedItem.Value);

                    //ciclo su tutti i collaboratori selezionati, dando priorità alla combobox altrimenti prendo i collaboratori selezionati mediante checkbox

                    foreach (var colId in comboColIds.Any() ? comboColIds : ColSelezionati)
                    {
                        // recupero delle reg per collaboratore e periodo
                        var regvsToProcess = RepoManager.Reg_VRepo.Find(regv => regv.Col_Id == colId && regv.Data_Ora_Fis_E >= from && regv.Data_Ora_Fis_E <= to);

                        // si procede con l'elaborazione solamente se ci sono delle reg da processare
                        if (regvsToProcess.Any())
                        {
                            // calcolo dell'elenco dei dati da stampare
                            var activityList = BusinessService.PopulateActivityList(regvsToProcess.ToList());

                            // si procede solamente se ci sono degli item da processare
                            if (activityList.Any())
                            {
                                // generazione dell'estrattore specializzato
                                var currentSpecialized = (IExportExcelSpecialized<ActivityItem>)Activator.CreateInstance(currentType);

                                // esecuzione dell'export specializzato
                                ExportExcelEngine.Export(currentSpecialized, activityList, excelModel, out path, exportToFileSystem: true);
                            }
                        }
                    }


                    // recupero della cartella con l'output degli export 56
                    string outputDirectory = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), Common.Properties.Settings.Default.Files_Output_Path); ;

                    // si procede con l'elaborazione solamente se la cartella con i files di output esiste
                    if (Directory.Exists(outputDirectory))
                    {
                        // recupero dei files presenti nella cartella
                        string searchPattern = String.Format("{0}*", BusinessService.GetLocalizedString(excelModel.Nome_Risorsa));
                        string[] reportFiles = Directory.GetFiles(outputDirectory, searchPattern);

                        string dateTimeSuffix = String.Format("{0}_{1}_{2}_{3}.{4}.{5}",
                       DateTime.Now.Year.ToString("0000"), DateTime.Now.Month.ToString("00"),
                       DateTime.Now.Day.ToString("00"), DateTime.Now.Hour.ToString("00"),
                       DateTime.Now.Minute.ToString("00"),
                       DateTime.Now.Second.ToString("0000"));
                        string zipFileName = String.Format("{0}_{1}.zip", BusinessService.GetLocalizedString(excelModel.Nome_Risorsa), dateTimeSuffix);

                        // se sono stati trovati dei files
                        if (reportFiles.Any())
                        {
                            // i file sono raccolti i un unico file
                            using (var returnArchive = new ZipArchive())
                            {
                                // aggiunta allo zip dei file generati
                                reportFiles.ForEach(fileName => returnArchive.AddFile(fileName, @"\"));

                                // salvataggio del file zip appena generato
                                returnArchive.Save(Path.Combine(outputDirectory, zipFileName));
                            }

                            // cancellazione dei file utilizzati per la sua creazione
                            reportFiles.ForEach(File.Delete);

                            #region Esportazione dello zip alla risposta

                            using (FileStream fs = new FileStream(Path.Combine(outputDirectory, zipFileName), FileMode.Open, FileAccess.Read))
                            {
                                try
                                {
                                    var response = HttpContext.Current.Response;
                                    response.Clear();

                                    response.ContentType = "application/zip";
                                    response.AddHeader("Accept-Header", fs.Length.ToString(CultureInfo.InvariantCulture));
                                    response.AddHeader("Content-Disposition", String.Format("{0}; filename={1}", "Attachment", zipFileName));
                                    response.Cache.SetCacheability(HttpCacheability.NoCache);
                                    response.AddHeader("Content-Length", fs.Length.ToString(CultureInfo.InvariantCulture));
                                    byte[] fsBytes = new byte[fs.Length];
                                    fs.Read(fsBytes, 0, fsBytes.Length);
                                    response.BinaryWrite(fsBytes);
                                    response.Flush();
                                    response.End();
                                }
                                catch (Exception)
                                {
                                }
                                finally
                                {

                                    fs.Close();
                                    fs.Dispose();
                                    // al termine delle operazioni viene cancellato il file zip processato
                                    File.Delete(Path.Combine(outputDirectory, zipFileName));

                                    // si segnala la necessità della chiusura del pannello di caricamento
                                    BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = true;
                                }
                            }



                            #endregion

                        }
                    }

                }
            }
        }

        protected void cPingLoadingExport_OnCallback(object source, CallbackEventArgs e)
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

        #region Gestione arrotondamento per durata

        protected void BtnRoundings_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_AGGIORNAMENTO));

            if (!e.Properties.ContainsKey("cpErrorMessage"))
                e.Properties.Add("cpErrorMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO));


            if (!e.Properties.ContainsKey("cpErrorMessageCol"))
                e.Properties.Add("cpErrorMessageCol",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE_NON_SEL));
        }

        protected void cRoundings_Callback(object sender, CallbackEventArgs e)
        {
            // calcolo del periodo richiesto di elaborazione
            DateTime from = deFrom.Date;
            if (deTo.Date == DateTime.MinValue)
                deTo.Date = deFrom.Date;
            DateTime to = new DateTime(deTo.Date.Year, deTo.Date.Month, deTo.Date.Day, 23, 59, 59);

            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<string, string>>();

            // se c'è qualcosa da processare
            if ((from != DateTime.MinValue && to != DateTime.MinValue) && ((ColSelezionati.Count > 0) || CmbColToElaborate.SelectedIndex != -1))
            {
                // per problemi di occupazione risorse, prima di eseguire queste operazioni che sono pesanti
                // si segnala al garbage collector di liberare le risorse eventualmente da processare
                GC.Collect();

                // aggiornamento delle date in base al parametro notturno
                BusinessService.ManageNocturneStartEndDate(ref from, ref to);

                // l'unità minima di elaborazione è un giorno e quindi se le date/ore in elaborazione sono uguali
                // allora l'ora to viene spostato al giorno successivo (a inizio giornata così da comprendere solo il giorno
                // in elaborazione)
                if (from == to)
                {
                    to = to.AddDays(1);
                    to = to.AddMinutes(1);
                }

                #region Eliminazione arrotondamenti per durata esistenti

                // calcolo di tutti i collaboratori coinvolti
                List<Reg> regsToDelete = new List<Reg>();

                // lista che contiene gli ID dei collaboratori selezionati con il Combo Box
                List<int> comboColIds = new List<int>();

                // se ho selzionato un collaboratore lo inserisco nella lista
                if (CmbColToElaborate.SelectedIndex != -1)
                    comboColIds.Add((int)CmbColToElaborate.SelectedItem.Value);

                // recupero i collaboratori, dando priorità alla combobox altrimenti prendo li selezionati mediante checkbox
                var cols = comboColIds.Any() ? comboColIds : ColSelezionati;

                // ciclo su tutti i collaboratori selezionati
                foreach (var colId in cols)
                {
                    // recupera le reg di tipo arrotondamento del collaboratore corrente per il periodo selezionato
                    var reg_tmp = RepoManager.RegRepo.Find(r => r.Registrazione_Data_Ora_Fis_Reg >= from && r.Registrazione_Data_Ora_Fis_Reg <= to &&
                        r.Col_Id == colId && !r.Registrazione_Bloccata && r.Registrazione_Tipo_Reg == (int)RegTypeEnum.ArrotDur, false).ToList();

                    // aggiunge le reg alla lista di delete
                    if (reg_tmp.Count > 0)
                    {
                        regsToDelete.AddRange(reg_tmp);
                    }
                }

                if (regsToDelete.Count > 0)
                //nel caso in cui ci siano registrazioni arrotondamenti da Cancellare Le Cancella
                {
                    BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] =
                        new KeyValuePair<double, string>(0, "Cancellazione arrotondamenti esistenti");

                    //Cancellazione arrotondamenti da db
                    RepoManager.Reg_VRepo.DeleteDurationRounding(regsToDelete);
                }
                #endregion

                #region Elaborazione Arrotondamenti per durata

                if (e.Parameter == "elaborateRoundings")
                {
                    BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(20, "Elaborazione arrotondamenti per durata iniziata");

                    List<Reg_V> regvsToElaborate = new List<Reg_V>();

                    foreach (var colId in cols)
                    {
                        // recupera le regv del collaboratore corrente per il periodo selezionato
                        var regv_tmp = RepoManager.Reg_VRepo.Find(r => r.Data_Ora_Fis_E >= from && r.Data_Ora_Fis_U <= to &&
                            r.Col_Id == colId && !r.Registrazione_Bloccata, false).ToList();

                        // aggiunge le regv alla lista di elaborazione
                        if (regv_tmp.Count > 0)
                        {
                            regvsToElaborate.AddRange(regv_tmp);
                        }
                    }

                    if (regvsToElaborate.Any())
                    {
                        // lancia l'elaborazione degli arrotondamentii per durata
                        errors = RepoManager.Reg_VRepo.DurationRounding(regvsToElaborate);
                    }
                }
                #endregion

                // gestione errori
                String message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA);

                if (errors.Any())
                {
                    message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA_CON_SEGNALAZIONI);
                }

                e.Result = message;
            }
        }



        #endregion

        protected void gvCol_CustomJsProperties(object sender, ASPxGridViewClientJSPropertiesEventArgs e)
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

        protected void gvCol_OnPageIndexChanged(object sender, EventArgs e)
        {
            (sender as ASPxGridView).JSProperties["cpPageChanged"] = 1;
        }

        protected void gvCol_OnCustomCallback(object sender, ASPxGridViewCustomCallbackEventArgs e)
        {
            if (e.Parameters == "AddNewAssociation")
            {

                #region Aggiunta di una nuova associazione

                // inizializzazione del dizionario degli errori
                var errorDic = new Dictionary<string, string>();

                // controllo l'autorizzazione dell'utente sul modulo delle pru_col per l'inserimento
                bool canAdd = false;
                try
                {
                    int funzId = PowerWebContext.Current.TabFunzs.FirstOrDefault(tfunz => tfunz.Nome_Tab_Funz == "MENU_FRMASSOCIAZIONICOLPRU_TEXT").Tab_Funz_Id;
                    canAdd = PowerWebContext.Current.User.IsUserAutorized(Utenti.OperationTypeEnum.Insert, PowerWebContext.Current.TabAuts.FirstOrDefault(tbaut => tbaut.Tab_Funz_Id == funzId)
                        , RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DefaultFunzAuthLevelEnum));
                }
                catch (Exception)
                {
                    // in caso d'errore l'utente non risulta autorizzato
                    canAdd = false;
                }
                finally
                {
                    // se l'utente non può modificare si segnala l'errore e si blocca l'operazione
                    if (!canAdd)
                        errorDic.Add("Auth", BusinessService.GetLocalizedString(PowerWebResources.ERR_UTENTE_NON_AUTORIZZATO));
                }


                // si prosegue solamente se l'autorizzazione non ha dato errore
                if (!errorDic.Any())
                {
                    // inizializzazione della nuova associazione pru -> col
                    Pru_Col newPruCol = RepoManager.Pru_ColRepo.Init();

                    // compilazione dei campi della nuova associazione
                    newPruCol.Abilitazione_Data_Inizio_Pru_Col = DeNewAssociationDate.Date;
                    newPruCol.DisAbilitazione_Pru_Col = false;
                    newPruCol.Pru_Id = (int)CmbPruToAssociate.SelectedItem.Value;
                    newPruCol.Col_Id = (int)CmbColToAssociate.SelectedItem.Value;

                    // effettuazione della check dei dati
                    errorDic = RepoManager.Pru_ColRepo.Check(newPruCol, true, false);

                    // si procede solamente se non ci sono errori nell'inserimetno del dato
                    if (!errorDic.Any())
                    {
                        // inserimento dei dati prima dell'inserimento a database
                        RepoManager.Pru_ColRepo.SetEntityBeforeAddOrUpdate(newPruCol);

                        // inserimento a database del record
                        RepoManager.Pru_ColRepo.Add(newPruCol, true);

                        // bind della griglia e aggiornamento dei dati (seconda istruzione fondamentale per l'allineamento delle navigation property del data source)
                        BindGrid();
                        gvCol.DataBind();
                    }
                }

                // in caso di errori allora si procede alla loro visualizzazione
                if (errorDic.Any())
                {
                    // calcolo della lista degli errori in formato piano separato da invio
                    string errorString = String.Join("\n", errorDic.Select(kvp => kvp.Value));

                    // gli errori sono riportati come jsproperty sulla griglia per la lettura dal client
                    // al termine del callback
                    var currentGrid = sender as ASPxGridView;

                    if (!currentGrid.JSProperties.ContainsKey("cpErrorMessage"))
                        currentGrid.JSProperties.Add("cpErrorMessage", String.Empty);
                    currentGrid.JSProperties["cpErrorMessage"] = errorString;
                }

                #endregion

            }
            if (e.Parameters == "initSelectedsMinutesAmmount")
            {

                #region Inizializzazione del monte minuti per i collaboratori selezionati

                // si procede ad effettuare l'elaborazione solamente se sono stati selezionati dei collaboratori
                if (ColSelezionati.Any())
                {
                    // calcolo del monte minuti da applicare sul collaboratore:
                    // - i primi due caratteri di quello inserito è la componente ora del monte minuti
                    // - gli ultimi due caratteri di quello inserito è la componente minuti del monte minuti
                    int minutesAmmount = Convert.ToInt32(TxtNewMinutesAmmountValue.Text.Substring(0, 3)) * 60 + Convert.ToInt32(TxtNewMinutesAmmountValue.Text.Substring(3, 2));

                    // se è richiesta una durata negativa il monte minuti iniziale viene moltiplicato per -1
                    if (ChkBoxNegativeDuration.Checked)
                        minutesAmmount *= -1;

                    // inizializzazione della lista che conterrà i collaboratori da aggiornare
                    List<Col> colsToUpdate = new List<Col>();

                    // per ogni collaboratore selezionato si procede a cancellare eventuali salvataggi di monte minuti consolidati precedenti e ad impostare
                    // il nuovo valore sul collaboratore
                    foreach (int colId in ColSelezionati)
                    {
                        // si recupera il collaboratore collegato all'id
                        Col selectedCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == colId);

                        // se è stato trovato un collaboratore
                        if (selectedCol != default(Col))
                        {
                            // recupero degli eventuali monte minuti precedentemente consolidati
                            IEnumerable<Col_Monte_Minuti> colMinutesAmmount = RepoManager.Col_Monte_MinutiRepo.Find(colMin => colMin.Col_Id == colId);

                            // se sono stati trovati dei record allora li si cancella
                            if (colMinutesAmmount.Any())
                                RepoManager.Col_Monte_MinutiRepo.Delete(colMinutesAmmount, true);

                            // aggiornamento del monte minuti sul collaboratore
                            selectedCol.Monte_Minuti = minutesAmmount;
                            colsToUpdate.Add(selectedCol);
                        }
                    }

                    // aggiornamento dei collaboratori modificati
                    RepoManager.ColRepo.Update(colsToUpdate, true);

                    // viene rieffettuato il bind della griglia al fine di aggiornare la visualizzazione dei dati
                    BindGrid();
                    gvCol.DataBind();
                }

                #endregion

            }
        }

        protected void gridSelectionChange_OnCallback(object source, CallbackEventArgs e)
        {

            ASPxGridView grid = gvCol;

            var selctionType = e.Parameter;



            switch (selctionType)
            {
                case "sAll":
                    ColSelezionati = new List<int>();
                    for (int i = 0; i < grid.VisibleRowCount; i++)
                    {
                        var colId = Convert.ToInt32(grid.GetRowValues(i, "Col_Id"));
                        ColSelezionati.Add(colId);
                        BtnTrips.Enabled = false;
                    }
                    break;

                case "uAll":
                    ColSelezionati = new List<int>();
                    break;

                case "sPage":
                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(grid.GetRowValues(i, "Col_Id"));
                        if (!ColSelezionati.Contains(colId))
                            ColSelezionati.Add(colId);

                    }
                    break;

                case "uPage":
                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(grid.GetRowValues(i, "Col_Id"));
                        if (ColSelezionati.Contains(colId))
                            ColSelezionati.Remove(colId);
                    }
                    break;

                case "sRow":
                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(grid.GetRowValues(i, "Col_Id"));
                        if (grid.Selection.IsRowSelected(i) && !ColSelezionati.Contains(colId))
                            ColSelezionati.Add(colId);
                    }

                    break;

                case "uRow":

                    for (int i = grid.VisibleStartIndex; i < grid.VisibleStartIndex + grid.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(grid.GetRowValues(i, "Col_Id"));
                        if (!grid.Selection.IsRowSelected(i) && ColSelezionati.Contains(colId))
                            ColSelezionati.Remove(colId);

                    }

                    break;
            }

            grid.JSProperties["cpVisibleRowCount"] = grid.VisibleRowCount;

        }

        protected void BtnDeleteTrips_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_AGGIORNAMENTO));

            if (!e.Properties.ContainsKey("cpErrorMessage"))
                e.Properties.Add("cpErrorMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO));


            if (!e.Properties.ContainsKey("cpErrorMessageCol"))
                e.Properties.Add("cpErrorMessageCol",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE_NON_SEL));
        }

        protected void BtnDeleteRoundings_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_AGGIORNAMENTO));

            if (!e.Properties.ContainsKey("cpErrorMessage"))
                e.Properties.Add("cpErrorMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO));


            if (!e.Properties.ContainsKey("cpErrorMessageCol"))
                e.Properties.Add("cpErrorMessageCol",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE_NON_SEL));
        }

        protected void cbAll_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage", BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_SELEZIONE));
        }

        #region Lancio elaborazione timbrature

        /// <summary>
        /// Handles the OnCustomJSProperties event of the BtnLaunchElaborate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CustomJSPropertiesEventArgs"/> instance containing the event data.</param>
        protected void BtnLaunchElaborate_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_AGGIORNAMENTO));

            if (!e.Properties.ContainsKey("cpErrorMessage"))
                e.Properties.Add("cpErrorMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO));


            if (!e.Properties.ContainsKey("cpErrorMessageCol"))
                e.Properties.Add("cpErrorMessageCol",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE_NON_SEL));
        }

        /// <summary>
        /// Handles the OnCallback event of the cElaborate control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="CallbackEventArgs"/> instance containing the event data.</param>
        protected void cElaborate_OnCallback(object source, CallbackEventArgs e)
        {
            BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = false;

            DateTime from = deFrom.Date;
            if (deTo.Date == DateTime.MinValue)
                deTo.Date = deFrom.Date;

            // la data di destinazione è il finale (le 23:59 della data indicata), altrimenti nella ricerca si perde un giorno
            DateTime to = new DateTime(deTo.Date.Year, deTo.Date.Month, deTo.Date.Day, 23, 59, 0);

            // se c'è qualcosa da processare
            if ((from != DateTime.MinValue && to != DateTime.MinValue) && ((ColSelezionati.Count > 0) || CmbColToElaborate.SelectedIndex != -1))
            {
                // per problemi di occupazione risorse, prima di eseguire queste operazioni che sono pesanti
                // si segnala al garbage collector di liberare le risorse eventualmente da processare
                GC.Collect();

                // aggiornamento delle date in base al parametro notturno
                BusinessService.ManageNocturneStartEndDate(ref from, ref to);

                // l'unità minima di elaborazione è un giorno e quindi se le date/ore in elaborazione sono uguali
                // allora l'ora to viene spostato al giorno successivo (a inizio giornata così da comprendere solo il giorno
                // in elaborazione)
                if (from == to)
                {
                    to = to.AddDays(1);
                    to = to.AddMinutes(1);
                }


                #region Eleborazione timbrature

                BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(0, "Elaborazione Viaggi Iniziata");

                // calcolo di tutti i collaboratori coinvolti
                var regsToElaborate = new List<Reg>();

                //lista che contiene gli ID dei collaboratori selezionati con il Combo Box
                List<int> comboColIds = new List<int>();
                // se ho selzionato un collaboratore lo inserisco nella lista 
                if (CmbColToElaborate.SelectedIndex != -1)
                    comboColIds.Add((int)CmbColToElaborate.SelectedItem.Value);

                //ciclo su tutti i collaboratori selezionati, dando priorità alla combobox altrimenti prendo i collaboratori selezionati mediante checkbox
                foreach (var colId in comboColIds.Any() ? comboColIds : ColSelezionati)
                {

                    //recupero delle registrazioni da elaborare
                    IEnumerable<Reg> colRegs = RepoManager.RegRepo.Find(r => r.Registrazione_Data_Ora_Fis_Reg >= from && r.Registrazione_Data_Ora_Fis_Reg <= to && r.Col_Id == colId).ToList();

                    // se ci sono delle registrazioni da processare si aggiungono alle registrazioni da elaborare
                    if (colRegs.Any())
                        regsToElaborate.AddRange(colRegs);
                }

                // elaborazione delle timbrature
                List<KeyValuePair<String, String>> errors = RepoManager.RegRepo.Elaborate(regsToElaborate, from, to, true, true);

                // ritorno del messaggio
                String message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA);

                if (errors.Any() || BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User])
                    message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA_CON_SEGNALAZIONI);

                e.Result = message;

                #endregion

                // per problemi di occupazione risorse, al termine delle operazioni che sono pesanti
                // si segnala al garbage collector di liberare le risorse eventualmente da processare
                GC.Collect();
            }
        }

        #endregion

        #region Gestione dell'inserimento della nuova associazione

        /// <summary>
        /// Handles the OnCustomJSProperties event of the BtnAddNewAssociation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CustomJSPropertiesEventArgs"/> instance containing the event data.</param>
        protected void BtnAddNewAssociation_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpErrorMessage"))
                e.Properties.Add("cpErrorMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CODICE_FRU_CANT_E_DATA_OBBLIGATORI));
        }

        #endregion

        #region Gestione dell'inizializzazione del monte minuti

        /// <summary>
        /// Gestisce la visualizzazione e la messa in lingua della sezione che contiene gli elementi grafici di 
        /// inizializzazione del monte minuti
        /// </summary>
        private void ManageDisplayAndLocalizeInitializeMinutesAmmountSection()
        {
            // se è attivo il modulo del monte minuti allora si 
            // procede alla messa in lingua degli elementi applicativi;
            // in caso contrario si procede al nascondimento dell'intera sezione
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Monte_Minuti)
            {
                InitMinutesAmmountPanel.HeaderText = BusinessService.GetLocalizedString(PowerWebResources.STR_INIZIALIZZAZIONE_MONTE_MINUTI);
                LblNewMinutesAmmountValue.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_NUOVO_MONTE_MINUTI);
                BtnInitMinutesAmmountForSelecteds.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_INIZIALIZZA_PER_COL_SELEZIONATI);
                ChkBoxNegativeDuration.Text = BusinessService.GetLocalizedString(PowerWebResources.FLD_REGISTRATIONDURATIONNEGATIVE);
            }
            else
                InitMinutesAmmountPanel.Visible = false;
        }

        #endregion

    }
}
