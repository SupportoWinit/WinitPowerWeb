using BingMapsRESTToolkit;
using Business;
using Business.ExportExcelEngine;
using Business.Repository;
using Common;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxEditors;
using DevExpress.Web.ASPxFormLayout;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.ASPxUploadControl;
using DevExpress.Web.Data;
using Domain;
using Exports.ExportExcelGeneric;
using log4net;
using Reports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PowerWeb.Modules
{
    public partial class CantModule : BaseGridModule, IPrintModule, ILogModule, IGeoLocationModule, IExportXLSXModule
    {
        private Cant _cantStub = null;

        const String KEYFIELDNAME = "Cant_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(CantModule));
        private int CustomizationVersion;


        private string _newImportFile
        {
            get
            {
                var importFile = PowerWebContext.GetFromSession<string>("ImportFile" + gvCant.ID);
                if (importFile == null)
                {
                    importFile = String.Empty;
                    PowerWebContext.SetToSession("ImportFile" + gvCant.ID, importFile);
                }
                return importFile;

            }

            set
            {
                PowerWebContext.SetToSession("ImportFile" + gvCant.ID, value);
            }
        }

        #region Property

        public bool IsInBatchMode
        {
            get { return GridView.SettingsEditing.Mode == GridViewEditingMode.Batch; }
        }

        public override ASPxGridView GridView
        {
            get
            { return gvCant; }
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryCant();

                    template = new PowerFormTemplate(this, templateDic);

                    PowerWebContext.SetToSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID, template);
                }
                return template;
            }
        }

        public override PowerFormTemplate EditDetailFormTemplate
        {
            get { return null; }
        }

        public string BingKey
        //Recupera dalla tab PARAM la BINGKEY
        {
            get
            {
                return RepoManager.ParamRepo.ParametersRow.BingKey;
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
                return null;
            }
        }

        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            //if (!Page.IsPostBack)
            //    GridView.DataBind();
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            GridView.KeyFieldName = KEYFIELDNAME;

            if (!Page.IsPostBack && !Page.IsCallback)
            {
                // verifico se è attiva la pesonalizzazione riguardante il raggruppamento della vista di default del modulo
                CustomizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DefaultGroupViewInCantPage);

                // se devo raggruppare per Tipologia, allora lo raggruppo
                if (CustomizationVersion == (int)DefaultGroupeViewInCatPageEnum.Tipologia_Can)
                {
                    gvCant.ClearSort();
                    //CREA RAGGRUPPAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                    gvCant.GroupBy(gvCant.Columns[CommonService.GetPropertyName(() => _cantStub.Tipologia_Can)]);
                    //CREA ORDINAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                    (gvCant.Columns[CommonService.GetPropertyName(() => _cantStub.Tipologia_Can)] as GridViewDataColumn)
                        .SortAscending();
                    (gvCant.Columns[CommonService.GetPropertyName(() => _cantStub.Codice_Cantiere)] as
                        GridViewDataColumn).SortAscending();
                    gvCant.DataBind();
                }
            }

            // verifico se è attiva la pesonalizzazione riguardante l'Import Custom per Mosaico
            CustomizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LaunchImportCantVersionEnum);
            if (CustomizationVersion == (int)LaunchImportCantVersionEnum.ImportMosaico)
            {
                flImportMosaico.Visible = true;
                filialeLbl.ClientVisible = true;
                filialeLbl.Visible = true;

                var fils = RepoManager.FilRepo.GetAll(true);
                foreach (var fil in fils)
                    cmbFil.Items.Add(new ListEditItem(fil.Codice_Fil, fil.Fil_Id));
                cmbFil.DataBind();

                if (!Page.IsCallback && !Page.IsPostBack)
                    ResetSession();
            }
            else if (CustomizationVersion == (int)LaunchImportCantVersionEnum.ImportDugoni)
            {
                flImportMosaico.Visible = true;
                cmbFil.Visible = false;
                cmbFil.ClientVisible = false;
            }
            //se si è nel caso di un import generale si nasconde la gestione della filiale nell'import
            else if (CustomizationVersion == (int)LaunchImportCantVersionEnum.ImportGenerale)
            {
                flImportMosaico.Visible = true;
                cmbFil.Visible = false;
                cmbFil.ClientVisible = false;
                filialeLbl.ClientVisible = false;
                filialeLbl.Visible = false;
            }


            PowerWebService.FillGridLabels(EntityType, GridView);
            PowerWebService.FillComboboxes(GridView);


            #region INIZIO Gestione delle CASCADE
            GridViewDataComboBoxColumn luogo = gvCant.Columns[CommonService.GetPropertyName(() => _cantStub.Luogo_Can)] as GridViewDataComboBoxColumn;
            if (luogo != null)
                luogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnLuogoChanged";
            GridViewDataComboBoxColumn nascLuogo = gvCant.Columns[CommonService.GetPropertyName(() => _cantStub.Luogo_Nascita_Can)] as GridViewDataComboBoxColumn;
            if (nascLuogo != null)
                nascLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnNascLuogoChanged";
            GridViewDataComboBoxColumn codLuogo = gvCant.Columns["Codice_Luogo_Residenza_Can"] as GridViewDataComboBoxColumn;
            if (codLuogo != null)
                codLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnCodLuogoChanged";
            GridViewDataComboBoxColumn nascCodLuogo = gvCant.Columns["Codice_Luogo_Nascita_Can"] as GridViewDataComboBoxColumn;
            if (nascCodLuogo != null)
                nascCodLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnNascCodLuogoChanged";
            GridViewDataComboBoxColumn domLuogo = gvCant.Columns["Domicilio_Luogo_Can"] as GridViewDataComboBoxColumn;
            if (domLuogo != null)
                domLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnDomLuogoChanged";
            GridViewDataComboBoxColumn domCodLuogo = gvCant.Columns["Codice_Domicilio_Luogo_Can"] as GridViewDataComboBoxColumn;
            if (domCodLuogo != null)
                domCodLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnDomCodLuogoChanged";

            #endregion FINE GEstione CASCADE

            BindGrid();

            // è messa in lingua la gestione del form layout di gestione della generazione nuova associazione
            LocalizeAddNewAssociationLayout();

            // compilazione dei combobox presenti nel form layout di gestione della generazione nupova associazione
            ManageAddNewAssociationCombos();
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
            LblNewAssociationFru.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_FRU);
            LblNewAssociationCant.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_CANTIERE);
            BtnAddNewAssociation.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_AGGIUNGI_ASSOCIAZIONE);
        }

        /// <summary>
        /// Gestisce il bind e il fill dei combobox presenti nel form layout per l'aggiunta di una nuova associazione.
        /// </summary>
        private void ManageAddNewAssociationCombos()
        {
            // compilazione del combobox di visualizzazione delle fru
            Fru fruStub = null;
            PowerWebService.FillComboboxes(CmbFruToAssociate, CommonService.GetPropertyName(() => fruStub.Fru_Id));
            if (!CmbFruToAssociate.ReadOnly)
            {
                EditButton btnEdit = new EditButton("X");
                CmbFruToAssociate.Buttons.Add(btnEdit);

                CmbFruToAssociate.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
            }

            // compilazione del combobox di visualizzazione dei cantieri
            PowerWebService.FillComboboxes(CmbCantToAssociate, "Search_Cant_Id");
            if (!CmbCantToAssociate.ReadOnly)
            {
                EditButton btnEdit = new EditButton("X");
                CmbCantToAssociate.Buttons.Add(btnEdit);

                CmbCantToAssociate.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
            }
        }

        private void BindGrid()
        {
            //non viene più usato il linq ma uso una GETALL
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvCant.KeyFieldName = KEYFIELDNAME;
            IsToPopulateGrid = true;
            IQueryable<Cant> currDataSource = Enumerable.Empty<Cant>().AsQueryable();
            var emptyList = Enumerable.Empty<Cant>();
            List<Cant> cantList = new List<Cant>();
            if (IsToPopulateGrid)
            {
                if (PowerWebContext.Current.User.Fil_Inclusive)
                {
                    //viene estratto l'ide dello user che ha fatto l'accesso a Powerweb
                    int userId = PowerWebContext.Current.User.Utenti_Id;

                    //filtro solo i responsabili che corrispondo all'utente che ha effettuato l'accesso 
                    var userFilIds = RepoManager.Utenti_FilRepo.Find(r => r.Utenti_Id == userId).ToList();

                    foreach (var item in userFilIds)
                    {
                        var can = RepoManager.CantRepo.GetAllQueryable().Where(c => c.Fil_Id == item.Fil_Id).ToList();

                        cantList.AddRange(can);
                    }
                    if (cantList.Count == 0)
                    {
                        currDataSource = RepoManager.CantRepo.GetAll(true).AsQueryable();
                    }
                    else {
                        currDataSource = cantList.AsQueryable();
                    }
                }
                else
                {
                    currDataSource = RepoManager.CantRepo.GetAll(true).AsQueryable();
                }               
                gvCant.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvCant.DataSource = emptyList;

        }

        protected void gvCant_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        private Type _entityType = typeof(Cant);

        public override Type EntityType
        {
            get { return _entityType; }
        }

        #region gvCant-InitNewRow-RowValidating-RowInserting-RowUpdating-RowDeleting-CommandButtonInitialize-BatchUpdate

        protected void gvCant_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Cant initCant = RepoManager.CantRepo.Init();
                PowerWebService.FillGridProperties(initCant, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvCant_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Cant initCant = new Cant();


            //se sono in batch edit mode carico tutti i campi in questo modo ho sempre tutti i campi aggiornati
            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvCant.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentCant = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentCant, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(initCant, e.NewValues);
            PowerWebService.FillEntityKey(initCant, e.Keys, KEYFIELDNAME);
            RepoManager.CantRepo.SetEntityBeforeAddOrUpdate(initCant);
            PowerWebService.AddValidationErrors(RepoManager.CantRepo.Check(initCant, e.IsNewRow), e.Errors, gvCant, typeof(CantModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvCant_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("CANT-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Cant initCant = new Cant();

            PowerWebService.FillEntityProperties(initCant, e.NewValues);
            RepoManager.CantRepo.SetEntityBeforeAddOrUpdate(initCant);
            #region Gestione GPS
            bool isToCalculateLatLong = true;

            if (RepoManager.ParamRepo.ParametersRow.Flag_GPS == 1)
            //Nel caso in cui sia attivata la Gestione GPS in CANT 
            {

                if ((isToCalculateLatLong || initCant.LongitudineGps_Can == 0.0d || initCant.LatitudineGps_Can == 0.0d))
                //Se è cambiato  l'Indirizzo e/o il Cap e/o il Comune oppure la Lat= 0 oppure la Long = 0
                //Ricalcola la LAT/LONG usando BING 
                {
                    Location geocode = BusinessService.GetGeocode(initCant.GeocodeAddress);
                    if (geocode != null)
                    {
                        initCant.LatitudineGps_Can = geocode.Point.Coordinates[0];
                        initCant.LongitudineGps_Can = geocode.Point.Coordinates[1];
                    }
                }
                //se is to calculate è true allora vado a generarmi l'indirizzo per la tab dist
                if (isToCalculateLatLong && RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti == 2)
                //Nel caso in cui il Flag di Ricalcolo Lat/Long sia True e il Flag di Calcolo TAB_DISTANZE sia = 2 (CALCULATE)
                //CANCELLA TUUTI gli eventuali Record esistenti con quel Cantiere con Chiave = G + Chiave Partenza e/o Chiave Arrivo = Cap/Comune/Indirizzo
                {
                    var cantAddress = "";
                    cantAddress = string.Format("{0} | {1} | {2}", initCant.Luogo_Can, initCant.Indirizzo_Can, initCant.Cap_Can);
                    var tabDecod = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Nome_Tab == "TIPO_DISTANZA" && td.Chiave_Tab == "G");
                    var toBeDeletedDistances = RepoManager.Tab_DistRepo.Find(td => td.Tab_Decod_Id == tabDecod.Tab_Decod_Id && (td.Arrivo_Tab_Dist == cantAddress || td.Partenza_Tab_Dist == cantAddress));
                    RepoManager.Tab_DistRepo.Delete(toBeDeletedDistances, true);
                }
            }
            #endregion

            if (RepoManager.ParamRepo.ParametersRow.Attiva_Num_Aut_Can)
            {
                var codCantMax = RepoManager.CantRepo.DbSet.Any() ? RepoManager.CantRepo.Max(c => c.Codice_Cantiere, true) : "0";
                int codCantMaxNum = 0;
                int.TryParse(codCantMax, out codCantMaxNum);
                codCantMaxNum = codCantMaxNum + 1;
                initCant.Codice_Cantiere = CommonService.AggiungiSpaziASinistraSeStringaNumerica(codCantMaxNum.ToString(), 20);
            }

            RepoManager.CantRepo.Add(initCant, true);
            e.Cancel = true;
            gvCant.CancelEdit();

        }



        protected void gvCant_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("CANT-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvCant.KeyFieldName]);

            Cant currentCant = RepoManager.CantRepo.Single(u => u.Cant_Id == currentId);

            if (RepoManager.ParamRepo.ParametersRow.File_Cant_Var)
            //Se in Tab PARAM è stato attivato il Flag di Gestione della Scrittura dei Record Variati in CantAR
            {
                //Scrive il Record in Tab COL_VAR com'era PRIMA di Modificarlo
                Cant_Var cantVar = new Cant_Var();
                CommonService.DuplicateEntity(currentCant, cantVar);
                RepoManager.Cant_VarRepo.Add(cantVar);
            }

            PowerWebService.FillEntityProperties(currentCant, e.NewValues);
            RepoManager.CantRepo.SetEntityBeforeAddOrUpdate(currentCant);


            #region Gestione GPS
            bool isToCalculateLatLong = false;

            if (RepoManager.ParamRepo.ParametersRow.Flag_GPS == 1)
            //Nel caso in cui sia attivata la Gestione GPS in CANT 
            {

                bool valuesEmpty = false;

                #region Luogo_Can

                var propertyName = CommonService.GetPropertyName(() => _cantStub.Luogo_Can);
                if (e.NewValues.Contains(propertyName))
                {
                    if (e.NewValues[CommonService.GetPropertyName(() => _cantStub.Luogo_Can)] == null)
                    {
                        e.NewValues[CommonService.GetPropertyName(() => _cantStub.Luogo_Can)] = string.Empty;
                        valuesEmpty = true;
                    }

                    if (e.OldValues[CommonService.GetPropertyName(() => _cantStub.Luogo_Can)] == null)
                        e.OldValues[CommonService.GetPropertyName(() => _cantStub.Luogo_Can)] = string.Empty;

                    isToCalculateLatLong |= e.NewValues[CommonService.GetPropertyName(() => _cantStub.Luogo_Can)].ToString() != e.OldValues[CommonService.GetPropertyName(() => _cantStub.Luogo_Can)].ToString();
                }

                #endregion


                #region Indirizzo_Can

                propertyName = CommonService.GetPropertyName(() => _cantStub.Indirizzo_Can);
                if (e.NewValues.Contains(propertyName))
                {
                    if (e.NewValues[CommonService.GetPropertyName(() => _cantStub.Indirizzo_Can)] == null)
                    {
                        e.NewValues[CommonService.GetPropertyName(() => _cantStub.Indirizzo_Can)] = string.Empty;
                        valuesEmpty = true;
                    }
                    if (e.OldValues[CommonService.GetPropertyName(() => _cantStub.Indirizzo_Can)] == null)
                        e.OldValues[CommonService.GetPropertyName(() => _cantStub.Indirizzo_Can)] = string.Empty;

                    isToCalculateLatLong |= e.NewValues[CommonService.GetPropertyName(() => _cantStub.Indirizzo_Can)].ToString() != e.OldValues[CommonService.GetPropertyName(() => _cantStub.Indirizzo_Can)].ToString();
                }

                #endregion

                #region Cap_Can

                propertyName = CommonService.GetPropertyName(() => _cantStub.Cap_Can);
                if (e.NewValues.Contains(propertyName))
                {
                    if (e.NewValues[CommonService.GetPropertyName(() => _cantStub.Cap_Can)] == null)
                    {
                        e.NewValues[CommonService.GetPropertyName(() => _cantStub.Cap_Can)] = string.Empty;
                        valuesEmpty = true;
                    }

                    if (e.OldValues[CommonService.GetPropertyName(() => _cantStub.Cap_Can)] == null)
                        e.OldValues[CommonService.GetPropertyName(() => _cantStub.Cap_Can)] = string.Empty;

                    isToCalculateLatLong |= e.NewValues[CommonService.GetPropertyName(() => _cantStub.Cap_Can)].ToString() != e.OldValues[CommonService.GetPropertyName(() => _cantStub.Cap_Can)].ToString();
                }

                #endregion


                if (!valuesEmpty && (isToCalculateLatLong || currentCant.LongitudineGps_Can == 0.0d || currentCant.LatitudineGps_Can == 0.0d))
                //Se è cambiato  l'Indirizzo e/o il Cap e/o il Comune oppure la Lat= 0 oppure la Long = 0
                //Ricalcola la LAT/LONG usando BING 
                {
                    Location geocode = BusinessService.GetGeocode(currentCant.GeocodeAddress);
                    if (geocode != null)
                    {
                        currentCant.LatitudineGps_Can = geocode.Point.Coordinates[0];
                        currentCant.LongitudineGps_Can = geocode.Point.Coordinates[1];
                    }
                }
                // se non era già True imposta a True il Flag di Ricalcolo se è Cambiata la LAT e/o la LONG
                if (isToCalculateLatLong != true)
                {
                    if (e.NewValues.Contains(CommonService.GetPropertyName(() => _cantStub.LatitudineGps_Can)))
                    {
                        var newValLatitudineGps_Can = e.NewValues[CommonService.GetPropertyName(() => _cantStub.LatitudineGps_Can)];
                        newValLatitudineGps_Can = newValLatitudineGps_Can == null ? string.Empty : newValLatitudineGps_Can.ToString();

                        var oldValLatitudineGps_Can = e.OldValues[CommonService.GetPropertyName(() => _cantStub.LatitudineGps_Can)];
                        oldValLatitudineGps_Can = oldValLatitudineGps_Can == null ? string.Empty : oldValLatitudineGps_Can.ToString();

                        isToCalculateLatLong |= newValLatitudineGps_Can != oldValLatitudineGps_Can;

                    }
                    if (e.NewValues.Contains(CommonService.GetPropertyName(() => _cantStub.LongitudineGps_Can)))
                    {

                        var newValLongitudineGps_Can = e.NewValues[CommonService.GetPropertyName(() => _cantStub.LongitudineGps_Can)];
                        newValLongitudineGps_Can = newValLongitudineGps_Can == null ? string.Empty : newValLongitudineGps_Can.ToString();

                        var oldValLongitudineGps_Can = e.OldValues[CommonService.GetPropertyName(() => _cantStub.LongitudineGps_Can)];
                        oldValLongitudineGps_Can = oldValLongitudineGps_Can == null ? string.Empty : oldValLongitudineGps_Can.ToString();

                        isToCalculateLatLong |= newValLongitudineGps_Can != oldValLongitudineGps_Can;
                    }
                }

                //se is to calculate è true allora vado a generarmi l'indirizzo per la tab dist
                if (isToCalculateLatLong && RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti == 2)
                //Nel caso in cui il Flag di Ricalcolo Lat/Long sia True e il Flag di Calcolo TAB_DISTANZE sia = 2 (CALCULATE)
                //CANCELLA TUUTI gli eventuali Record esistenti con quel Cantiere con Chiave = G + Chiave Partenza e/o Chiave Arrivo = Cap/Comune/Indirizzo
                {
                    var cantAddress = "";
                    cantAddress = string.Format("{0} | {1} | {2}", currentCant.Luogo_Can, currentCant.Indirizzo_Can, currentCant.Cap_Can);
                    var tabDecod = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Nome_Tab == "TIPO_DISTANZA" && td.Chiave_Tab == "G");
                    var toBeDeletedDistances = RepoManager.Tab_DistRepo.Find(td => td.Tab_Decod_Id == tabDecod.Tab_Decod_Id && (td.Arrivo_Tab_Dist == cantAddress || td.Partenza_Tab_Dist == cantAddress));
                    RepoManager.Tab_DistRepo.Delete(toBeDeletedDistances, true);
                }
            }
            #endregion

            #region Tipologia_Can (Se cambiata da/a Attività indico al sistema di rielaborarne le relative Registrazioni in Tab PendingElab

            var tipologiaCantPropertyName = CommonService.GetPropertyName(() => _cantStub.Tipologia_Can);

            if (e.NewValues.Contains(tipologiaCantPropertyName))
            {
                var tipologiaCantOld = e.OldValues[tipologiaCantPropertyName].ToString().ToUpper();
                var tipologiaCantNew = e.NewValues[tipologiaCantPropertyName].ToString().ToUpper();

                if ((tipologiaCantOld != tipologiaCantNew) && (tipologiaCantOld == "ATT" || tipologiaCantNew == "ATT"))
                //Se il Cantiere ha cambiato la Tipologia e la Tipologia OLD o la Tipolgia NEW è/era = ATT
                //Chiamo la Routine che leggendo le Registrazioni del Cantiere scrive nella Tabella PendingElab
                //La data Minima (maggiore della dataBlocco) e Massima delle sue registrazioni per rielaborare le relative REG
                {
                    RepoManager.CantRepo.AddPendingElabForActivity(currentCant);
                }
            }

            #endregion

            #region Singiola Reg (Passaggi) (Se cambiata da/a Singola_Reg (Passaggi) indico al sistema di rielaborarne le relative Registrazioni in Tab PendingElab

            var SingolaregPropertyName = CommonService.GetPropertyName(() => _cantStub.Singola_Reg);

            if (e.NewValues.Contains(SingolaregPropertyName))
            {
                bool SingolaRegCantOld = (bool)e.OldValues[SingolaregPropertyName];
                bool SingolaRegCantNew = (bool)e.NewValues[SingolaregPropertyName];

                if (SingolaRegCantOld != SingolaRegCantNew)
                //Se il Cantiere ha cambiato la Singola_Reg (Passaggio)
                //Chiamo la Routine che leggendo le Registrazioni del Cantiere scrive nella Tabella PendingElab
                //La data Minima (maggiore della dataBlocco) e Massima delle sue registrazioni per rielaborare le relative REG
                {
                    RepoManager.CantRepo.AddPendingElabForActivity(currentCant);
                }
            }

            #endregion

            try
            {
                RepoManager.CantRepo.SaveChanges();
            }
            catch (Exception ex)
            {
                _log.Error(String.Format("Errore durante l'update di un cantiere (Row-Updating) {0}", ex.Message));
            }

            e.Cancel = true;
            gvCant.CancelEdit();
        }

        protected void gvCant_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("CANT-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvCant.KeyFieldName]);
            Cant currentCant = RepoManager.CantRepo.Single(u => u.Cant_Id == currentId);
            RepoManager.CantRepo.Delete(currentCant, true);
            e.Cancel = true;

            //sincronizzazione db cantieri nella app se richede le sync.
            //diffCallCantOrAtt(currentCant, 1, null);
        }
        #region gestione DB app


        #endregion

        public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        }
        #endregion

        /// <summary>
        /// Handles the OnCustomCallback event of the gvCant control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxGridViewCustomCallbackEventArgs"/> instance containing the event data.</param>
        protected void gvCant_OnCustomCallback(object sender, ASPxGridViewCustomCallbackEventArgs e)
        {
            if (e.Parameters == "AddNewAssociation")
            {
                // inizializzazione del dizionario degli errori
                var errorDic = new Dictionary<string, string>();

                // controllo l'autorizzazione dell'utente sul modulo delle fru_cant per l'inserimento
                bool canAdd = false;
                try
                {
                    int funzId = PowerWebContext.Current.TabFunzs.FirstOrDefault(tfunz => tfunz.Nome_Tab_Funz == "MENU_FRMASSOCIAZIONICANTFRU_TEXT").Tab_Funz_Id;
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
                    // inizializzazione della nuova associazione fru -> cant
                    Fru_Cant newFruCant = RepoManager.Fru_CantRepo.Init();

                    // compilazione dei campi della nuova associazione
                    newFruCant.Abilitazione_Data_Inizio_Fru_Can = DeNewAssociationDate.Date;
                    newFruCant.DisAbilitazione_Fru_Can = false;
                    newFruCant.Fru_Id = (int)CmbFruToAssociate.SelectedItem.Value;
                    newFruCant.Cant_Id = (int)CmbCantToAssociate.SelectedItem.Value;

                    // effettuazione della check dei dati
                    errorDic = RepoManager.Fru_CantRepo.Check(newFruCant, true, false);

                    // si procede solamente se non ci sono errori nell'inserimetno del dato
                    if (!errorDic.Any())
                    {
                        // inserimento dei dati prima dell'inserimento a database
                        RepoManager.Fru_CantRepo.SetEntityBeforeAddOrUpdate(newFruCant);

                        // inserimento a database del record
                        RepoManager.Fru_CantRepo.Add(newFruCant, true);

                        // bind della griglia e aggiornamento dei dati (seconda istruzione fondamentale per l'allineamento delle navigation property del data source)
                        BindGrid();
                        gvCant.DataBind();
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
            }
            else if (e.Parameters == "updateDataSource")
            {
                // se è richiesto di aggiornare i dati della griglia effettuo un bind completo per il ricalcolo
                BindGrid();
                gvCant.DataBind();
            }
        }

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.Data_Registrazione_Can) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.DataOraUltimaModifica_Can) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.Data_Isee_Can) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.Data_Nascita_Can) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.Data_Rapporto_Fine_1_Can) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.Data_Rapporto_Inizio_1_Can))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Cant> cants = CommonService.ConvertTo<Cant>(items);
            XRCant cantReport = new XRCant(cants, PowerWebService.ConvertTabPageExtendedToString(selectedTabs));
            return new ExtXtraReport { Report = cantReport, PictureBox = cantReport.CompanyLogo };
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }

        #region Gestione dell'import delle anagrafiche cantiere

        protected void cUplImportCommand_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {

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
                        //CantieriImportManager.ImportFromCsv(File.ReadAllText(_newImportFile, Encoding.GetEncoding(850)).Split('\r'), Convert.ToInt32(cmbFil.Value));
                        var errors = RepoManager.CantRepo.ImportFromCSV(File.ReadAllText(_newImportFile, Encoding.GetEncoding(850)).Split('\r'), Convert.ToInt32(cmbFil.Value));

                        if (errors.Count > 0)
                        {
                            cUplImportCommand.JSProperties["cpImportError"] = String.Join(";\n", errors.Values);
                        }

                    }
                    catch (Exception ex)
                    {
                        //controllo che il messaggio di errore sia contenuto nel dizionario degli "errori"
                        if (!cUplImportCommand.JSProperties.ContainsKey("cpImportError"))
                            cUplImportCommand.JSProperties.Add("cpImportError", String.Empty);

                        cUplImportCommand.JSProperties["cpImportError"] = ex.Message;

                    }
                    finally
                    {
                        // in ogni caso al termine dell'importazione viene backuppato il file importato
                        BusinessService.BackupProcessedFiles(new List<string>() { _newImportFile }, Server.MapPath(Common.Properties.Settings.Default.Files_Input_Cant_Backup_Path));
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
            _newImportFile = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path + String.Format("CantFile{0}{1}", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"), ".csv"));

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
        protected void gvCant_CellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            SetupCalendarOwner(e.Editor as ASPxDateEdit);
        }

        protected void gvCant_AutoFilterCellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            SetupCalendarOwner(e.Editor as ASPxDateEdit);
        }

        void SetupCalendarOwner(ASPxDateEdit editor)
        {
            if (editor == null) return;
            editor.PopupCalendarOwnerID = "__ReferenceDateEdit";
        }
        #endregion

        #region Gestione Export Excel CUSTOM

        public List<Tab_Excel_Model> Models
        //Recupera dalla Tab_Excel il Modello desiderato
        {
            get
            {
                var typeName = typeof(Cant).Name;
                return RepoManager.Tab_Excel_ModelRepo.Find(xlsxModel => xlsxModel.IsActive && xlsxModel.Nome_Entity == typeName).ToList();
            }
        }

        /// <summary>
        /// Gestisce l'Export dei Cantieri in Excel
        /// </summary>
        /// <param name="model"></param>
        /// <param name="items"></param>
        public void ExportXLSX(Tab_Excel_Model model, List<object> items)
        {
            string path = "";

            List<Cant> cants = CommonService.ConvertTo<Cant>(items).ToList();

            var currentType = Type.GetType(String.Format("Exports.{0}, Exports", model.Nome_Specializzato));

            if (currentType != null)
            {
                IEnumerable<Tuple<string, string, string>> visibleFields = null;

                var currentSpecialized = (IExportExcelSpecialized<Cant>)Activator.CreateInstance(currentType);

                if (currentType == typeof(ExportExcelGenericCant))
                    visibleFields = PowerWebService.GetGridViewVisibleFields(gvCant);

                ExportExcelEngine.Export(currentSpecialized, cants, model, out path, visibleFields);
            }
        }

        #endregion

        protected void OnClick(object sender, EventArgs e)
        {
            DateTime dateToSearchFrom = new DateTime(2014, 4, 17, 0, 0, 0);
            DateTime dateToSearchTo = new DateTime(2014, 4, 17, 23, 59, 59);
            List<Cant> cantToAdjust = RepoManager.CantRepo.DbSet.Where(cant => cant.Data_Registrazione_Can >= dateToSearchFrom && cant.Data_Registrazione_Can <= dateToSearchTo).ToList();

            foreach (var cantToProcess in cantToAdjust)
            {
                var numAlpha = new Regex("(?<Alpha>[a-zA-Z\\s]*)(?<Numeric>[0-9]*)");
                List<Cant> cants = RepoManager.CantRepo.DbSet.Where(cant => cant.Fil_Id == cantToProcess.Fil_Id && cant.Data_Registrazione_Can < dateToSearchFrom).OrderBy(cant => cant.Codice_Cantiere).ToList();
                Cant lastCant = null;
                if (cants.Any())
                    lastCant = cants.Last();

                if (lastCant != null)
                {
                    var match = numAlpha.Match(lastCant.Codice_Cantiere.Trim());

                    var alpha = match.Groups["Alpha"].Value;
                    var num = match.Groups["Numeric"].Value;
                    int len = num.Length;

                    num = Convert.ToString(Convert.ToInt32(num) + 1);
                    num = num.PadLeft(len, '0');
                    if (alpha == string.Empty)
                        cantToProcess.Codice_Cantiere = CommonService.CompletaASinistra(String.Format("{0}{1}", alpha, num), 20);
                    else
                        cantToProcess.Codice_Cantiere = String.Format("{0}{1}", alpha, num);
                }
                else
                    cantToProcess.Codice_Cantiere = CommonService.CompletaASinistra("00001", 20);

                RepoManager.CantRepo.Update(cantToProcess, true);
            }
        }

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

    }
}