using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using Domain;
using Common;
using DevExpress.Web.Data;
using log4net;
using Business;
using Reports;
using DevExpress.Data.Filtering;
using System;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.ASPxScheduler;
using System.Drawing;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DevExpress.Web.ASPxEditors;
using System.Globalization;
using DevExpress.Web.ASPxGridView.Export;

namespace PowerWeb.Modules
{

    public partial class Tab_OrariModule : BaseGridModule, ILogModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Tab_OrariModule));
        const Reg_V _regVStub = null;
        const Tab_Orari_Tipo _tabOrariTipoStub = null;
        const Tab_Orari _tabOrariStub = null;

        private const string JsCloneValidationErrorKey = "cpCloneValidationError";

        /// <summary>
        /// L'entità di riferimento del collaboratore (codice)
        /// </summary>
        private const string ColEntityType = "Col";

        /// <summary>
        /// L'entità di riferimento del cantiere (codice)
        /// </summary>
        private const string CantEntityType = "Can";

        const String TABORARITIPO_KEYFIELDNAME = "Tab_Orari_Tipo_Id";
        const String TABORARI_KEYFIELDNAME = "Tab_Orari_Id";

        public ILog Log
        {
            get { return _log; }
        }

        public List<Reg_V> SchedulerRegVs
        {
            get
            {
                return PowerWebContext.GetFromSession<List<Reg_V>>("SchedulerRegVs_" + scTabOrari.ID);
            }

            set
            {
                List<Reg_V> list = value as List<Reg_V>;
                if (list != null)
                    PowerWebContext.SetToSession<List<Reg_V>>("SchedulerRegVs_" + scTabOrari.ID, list);
            }
        }

        public override ASPxGridView GridView // Carica Tab_Orari_Cant
        {
            get
            {
                return gvTabOrariTipo;
            }
        }

        public override PowerFormTemplate EditFormTemplate
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID);

                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryTabOrariTipo();

                    template = new PowerFormTemplate(this, templateDic);

                    PowerWebContext.SetToSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID, template);
                }
                return template;
            }
        }

        public override PowerFormTemplate EditDetailFormTemplate
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID));
                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryTabOrari();
                    template = new PowerFormTemplate(this, templateDic);
                    template.IsDetail = true;
                    PowerWebContext.SetToSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID), template);
                }
                return template;
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Tab_Orari_Tipo), gvTabOrariTipo);
            PowerWebService.FillComboboxes(gvTabOrariTipo);

            // impostazione in lingua della sezione di clonazione degli orari
            LocalizeCloneTimetableSection();

            // bind del combobox di selezione dell'orario di destinazione della clonazione
            PowerWebService.FillComboboxes(CmbCloneToTimetable, "Tab_Orari_Tipo_Id");
            EditButton btnEdit = new EditButton("X");
            CmbCloneToTimetable.Buttons.Add(btnEdit);
            CmbCloneToTimetable.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";

            BindGridTabOrariTipo();
            BindScheduler(true);

        }

        private void BindGridTabOrariTipo()
        {
            gvTabOrariTipo.KeyFieldName = TABORARITIPO_KEYFIELDNAME;
            gvTabOrariTipo.DataSource = RepoManager.Tab_OrariTipoRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)
                gvTabOrariTipo.DataBind();
        }

        private void BindGridTabOrari(ASPxGridView gvDetails)
        {
            int tabOrariTipoId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = TABORARI_KEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Tab_OrariRepo.Find(to => to.Tab_Orari_Tipo_Id == tabOrariTipoId).OrderByDescending(to => to.Cant_Desc).ThenByDescending(to => to.Data_Inizio).ToList();
        }

        public override Type EntityType
        {
            get { return typeof(Tab_Orari_Tipo); }
        }

        public override Type DetailGridEntityType
        {
            get { return typeof(Tab_Orari); }

        }

        #region gvTabOrari :RowValidating-RowInserting-RowUpdating-RowDeleting

        protected void gvTabOrari_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            
            ASPxGridView grid = sender as ASPxGridView;

            if (grid != null)
            {
                Tab_Orari initTabOrari = RepoManager.Tab_OrariRepo.Init();
                initTabOrari.Tab_Orari_Tipo_Id = Convert.ToInt32(grid.GetMasterRowKeyValue());
                PowerWebService.FillGridProperties(initTabOrari, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        /// <summary>
        /// Metodo di validazione dei dati contenuti nella griglia di dettaglio degli orari
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxDataValidationEventArgs"/> instance containing the event data.</param>
        protected void gvTabOrari_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvDetail = sender as ASPxGridView;
            if (gvDetail != null)
            {
                Tab_Orari newTabOrari = RepoManager.Tab_OrariRepo.Init();
                int TabOrariId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());
                newTabOrari.Tab_Orari_Id = TabOrariId;
                PowerWebService.FillEntityProperties(newTabOrari, e.NewValues);
               
                //se è un orario mesile allora non vengono inseriti i giorni
                if (newTabOrari.Orario_Mensile)
                {
                    newTabOrari.G1 = false;
                    newTabOrari.G2 = false;
                    newTabOrari.G3 = false;
                    newTabOrari.G4 = false;
                    newTabOrari.G5 = false;
                    newTabOrari.G6 = false;
                    newTabOrari.G7 = false;
                    
                }
                PowerWebService.FillEntityKey(newTabOrari, e.Keys, TABORARI_KEYFIELDNAME);
                RepoManager.Tab_OrariRepo.SetEntityBeforeAddOrUpdate(newTabOrari);
                PowerWebService.AddValidationErrors(RepoManager.Tab_OrariRepo.Check(newTabOrari, e.IsNewRow), e.Errors, gvDetail, typeof(Tab_OrariModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvTabOrari_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            int tab_orari_tipo_id = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());
            _log.Info(String.Format("TAB_ORARI-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Tab_Orari newTabOrari = RepoManager.Tab_OrariRepo.Init();
            PowerWebService.FillEntityProperties(newTabOrari, e.NewValues);
            
            //se è un orario mesile allora non vengono inseriti i giorni
            if (newTabOrari.Orario_Mensile)
            {
                newTabOrari.G1 = false;
                newTabOrari.G2 = false;
                newTabOrari.G3 = false;
                newTabOrari.G4 = false;
                newTabOrari.G5 = false;
                newTabOrari.G6 = false;
                newTabOrari.G7 = false;
            }

            //Se è richiesta l'assegnazione automatica del piano diurno/notturno, il piano deve essere sola durata
            Tab_Orari_Tipo tab_orari_tipo = RepoManager.Tab_OrariTipoRepo.Single(tot => tot.Tab_Orari_Tipo_Id == tab_orari_tipo_id);
            if (tab_orari_tipo.Tab_Orari_Tipo_NotDiu_Auto && newTabOrari.Ora_E.HasValue && newTabOrari.Ora_U.HasValue)
            {
                newTabOrari.Durata_Minuti = Convert.ToInt32(newTabOrari.Ora_U.Value.TotalMinutes - newTabOrari.Ora_E.Value.TotalMinutes);
                newTabOrari.Ora_E = newTabOrari.Ora_U = null;
            }

            RepoManager.Tab_OrariRepo.SetEntityBeforeAddOrUpdate(newTabOrari);
            newTabOrari.Tab_Orari_Tipo_Id = tab_orari_tipo_id;
            RepoManager.Tab_OrariRepo.Add(newTabOrari, true);

            e.Cancel = true;
            gvDetail.CancelEdit();
            BindGridTabOrari(gvDetail);
        }

        protected void gvTabOrari_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            int tab_orari_tipo_id = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());
            _log.Info(String.Format("TAB_ORARI-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Tab_Orari currentTabOrari = RepoManager.Tab_OrariRepo.Single(o => o.Tab_Orari_Id == currentId);
            PowerWebService.FillEntityProperties(currentTabOrari, e.NewValues);
           
            //se è un orario mesile allora non vengono inseriti i giorni
            if (currentTabOrari.Orario_Mensile)
            {
                currentTabOrari.G1 = false;
                currentTabOrari.G2 = false;
                currentTabOrari.G3 = false;
                currentTabOrari.G4 = false;
                currentTabOrari.G5 = false;
                currentTabOrari.G6 = false;
                currentTabOrari.G7 = false;
            }

            //Se è richiesta l'assegnazione automatica del piano diurno/notturno, il piano deve essere sola durata
            Tab_Orari_Tipo tab_orari_tipo = RepoManager.Tab_OrariTipoRepo.Single(tot => tot.Tab_Orari_Tipo_Id == tab_orari_tipo_id);
            if (tab_orari_tipo.Tab_Orari_Tipo_NotDiu_Auto && currentTabOrari.Ora_E.HasValue && currentTabOrari.Ora_U.HasValue)
            {
                currentTabOrari.Durata_Minuti = Convert.ToInt32(currentTabOrari.Ora_U.Value.TotalMinutes - currentTabOrari.Ora_E.Value.TotalMinutes);
                currentTabOrari.Ora_E = currentTabOrari.Ora_U = null;
            }

            RepoManager.Tab_OrariRepo.SetEntityBeforeAddOrUpdate(currentTabOrari);
            RepoManager.Tab_OrariRepo.SaveChanges();
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindGridTabOrari(gvDetail);
        }

        protected void gvTabOrari_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("TAB_ORARI-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Tab_Orari currentTabOrari = RepoManager.Tab_OrariRepo.Single(o => o.Tab_Orari_Id == currentId);
            RepoManager.Tab_OrariRepo.Delete(currentTabOrari, true);
            e.Cancel = true;
            BindGridTabOrari(gvDetail);
        }

        #endregion

        #region gvTabOrariTipo : InitNewRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect

        protected void gvTabOrariTipo_OnInitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;

            if (grid != null)
            {
                Tab_Orari_Tipo initTabOrariTipo = RepoManager.Tab_OrariTipoRepo.Init();
                PowerWebService.FillGridProperties(initTabOrariTipo, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvTabOrariTipo_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("UTENTI-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Tab_Orari_Tipo newTabOrariTipo = RepoManager.Tab_OrariTipoRepo.Init();
            PowerWebService.FillEntityProperties(newTabOrariTipo, e.NewValues);
            RepoManager.Tab_OrariTipoRepo.SetEntityBeforeAddOrUpdate(newTabOrariTipo);
            RepoManager.Tab_OrariTipoRepo.Add(newTabOrariTipo, true);

            e.Cancel = true;
            gvTabOrariTipo.CancelEdit();
            BindGridTabOrariTipo();
        }

        protected void gvTabOrariTipo_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("TAB_ORARI_TIPO-Row updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTabOrariTipo.KeyFieldName]);
            Tab_Orari_Tipo currentTOT = RepoManager.Tab_OrariTipoRepo.Single(tot => tot.Tab_Orari_Tipo_Id == currentId);
            PowerWebService.FillEntityProperties(currentTOT, e.NewValues);
            RepoManager.Tab_OrariTipoRepo.SetEntityBeforeAddOrUpdate(currentTOT);
            RepoManager.Tab_OrariTipoRepo.SaveChanges();

            e.Cancel = true;
            gvTabOrariTipo.CancelEdit();
            BindGridTabOrariTipo();
        }

        protected void gvTabOrariTipo_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("TAB_ORARI_TIPO-Row deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTabOrariTipo.KeyFieldName]);
            Tab_Orari_Tipo currentTOT = RepoManager.Tab_OrariTipoRepo.Single(tot => tot.Tab_Orari_Tipo_Id == currentId);
            RepoManager.Tab_OrariTipoRepo.Delete(currentTOT, true);
            e.Cancel = true;
            BindGridTabOrariTipo();
        }

        protected void gvTabOrari_BeforePerformDataSelect(object sender, EventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                PowerWebService.FillGridLabels(typeof(Tab_Orari), gvDetails);
                PowerWebService.FillComboboxes(gvDetails);
                PowerWebService.InitDetailGrid(Page, gvDetails);

                BindGridTabOrari(gvDetails);
            }
        }

        protected void gvTabOrariTipo_OnRowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvMaster = sender as ASPxGridView;
            if (gvMaster != null)
            {
                Tab_Orari_Tipo newTabOrariTipo = RepoManager.Tab_OrariTipoRepo.Init();
                int TabOrariTipoId = Convert.ToInt32(gvMaster.GetMasterRowKeyValue());
                newTabOrariTipo.Tab_Orari_Tipo_Id = TabOrariTipoId;
                PowerWebService.FillEntityProperties(newTabOrariTipo, e.NewValues);
                PowerWebService.FillEntityKey(newTabOrariTipo, e.Keys, TABORARITIPO_KEYFIELDNAME);
                RepoManager.Tab_OrariTipoRepo.SetEntityBeforeAddOrUpdate(newTabOrariTipo);
                PowerWebService.AddValidationErrors(RepoManager.Tab_OrariTipoRepo.Check(newTabOrariTipo, e.IsNewRow), e.Errors, gvMaster, typeof(Tab_OrariModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (VUOTA perchè NON ci sono Campi Date da Gestire ma va comunque definita vuota se non ce ne sono)
        {
        }

        protected void scTabOrariPanel_Callback(object sender, DevExpress.Web.ASPxClasses.CallbackEventArgsBase e)
        {
            var values = gvTabOrariTipo.GetSelectedFieldValues(Common.CommonService.GetPropertyName(() => _tabOrariTipoStub.Tab_Orari_Tipo_Id));
            if (values.Count > 0 && deTabOrari.Date != DateTime.MinValue)
            {
                // se è richiesto di visualizzare un orario che contiene elementi di sola durata viene segnalata un'eccezione a video
                int tabOrarioTipoId = Convert.ToInt32(values.First());
                Tab_Orari_Tipo tipoOrario = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(tot => tot.Tab_Orari_Tipo_Id == tabOrarioTipoId);
                if (tipoOrario != default(Tab_Orari_Tipo))
                {
                    // se collegato al tipo orario ci sono degli orari di sola durata si genera un'eccezione
                    if (RepoManager.Tab_OrariRepo.Find(orario => orario.Tab_Orari_Tipo_Id == tipoOrario.Tab_Orari_Tipo_Id && orario.Ora_E == null && orario.Ora_U == null).Any())
                        throw new InvalidOperationException(BusinessService.GetLocalizedString(PowerWebResources.ERR_NO_VISUALIZZARE_ORARI_SOLO_DURATA));


                    Col colStub = RepoManager.ColRepo.Init();
                    colStub.Tab_Orari_Tipo_Id = Convert.ToInt32(values.First());

                    var currentCantIds = RepoManager.Tab_OrariTipoRepo.SingleOrDefault(tot => tot.Tab_Orari_Tipo_Id == colStub.Tab_Orari_Tipo_Id).Tab_Orari.Select(to => to.Cant_Id);

                    var currentCants = RepoManager.CantRepo.Find(cant => currentCantIds.Contains(cant.Cant_Id)).ToList();

                    // non mi interessano in questo momento i ritorni del metodo percui imposto i due ritorni come filler (cioè varibili di riemplimento poi non utilizzate
                    bool boolFiller;
                    int intFiller;
                    SchedulerRegVs = RepoManager.Reg_VRepo.GetTimesheet(colStub, currentCants, deTabOrari.Date, CommonService.GetLastMonthDay(deTabOrari.Date), out boolFiller, out intFiller);
                    BindScheduler(true);
                }
            }
        }

        private void BindScheduler(bool isForceResetStartDate = false)
        {
            if (!Page.IsPostBack && !Page.IsCallback)
                scTabOrari.Start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            if (SchedulerRegVs != null)
            {
                if (isForceResetStartDate)
                {
                    Reg_V first = SchedulerRegVs.OrderBy(s => s.Data_Ora_Fis_E).FirstOrDefault();
                    if (first != null)
                        scTabOrari.Start = new DateTime(first.Data_Ora_FigFis_E.Year, first.Data_Ora_FigFis_E.Month, 1);
                }

                ASPxSchedulerStorage storage = scTabOrari.Storage;
                ASPxAppointmentMappingInfo mappings = storage.Appointments.Mappings;
                storage.BeginUpdate();
                try
                {
                    mappings.AppointmentId = CommonService.GetPropertyName(() => _regVStub.RegE);
                    mappings.Start = CommonService.GetPropertyName(() => _regVStub.Data_Ora_FigFis_E);
                    mappings.End = CommonService.GetPropertyName(() => _regVStub.Data_Ora_FigFis_U);
                    mappings.Description = CommonService.GetPropertyName(() => _regVStub.Motivazione_Reg_Cod);

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Cant_Id));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Col_Id));


                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.RegU));

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_E));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_U));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Durata_Fis));

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fig_E));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fig_U));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Durata_Fig));

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.KM_Reg));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Note_Reg));

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Col_Mnemonic));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Col_Desc));

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Cant_Mnemonic));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Cant_Desc));
                }
                finally
                {
                    storage.EndUpdate();
                }

                scTabOrari.AppointmentDataSource = SchedulerRegVs;
                scTabOrari.DataBind();

                scTabOrari.AppointmentViewInfoCustomizing += new AppointmentViewInfoCustomizingEventHandler(scRegV_AppointmentViewInfoCustomizing);
            }

            scTabOrari.TimelineView.IntervalCount = CommonService.GetLastMonthDay(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)).Day;
        }

        private void AddCustomMapping(ASPxSchedulerStorage storage, String propertyName)
        {
            storage.Appointments.CustomFieldMappings.Add(new ASPxAppointmentCustomFieldMapping(propertyName, propertyName));
        }

        void scRegV_AppointmentViewInfoCustomizing(object sender, AppointmentViewInfoCustomizingEventArgs e)
        {
            if (e.ViewInfo.Appointment.Start == e.ViewInfo.Appointment.End)
            {
                var Background = (int)SchedulerEnum.Pass;
                e.ViewInfo.AppointmentStyle.BackColor = RepoManager.ParamRepo.GetColorFromEnum((SchedulerEnum)Background, true);
                e.ViewInfo.ShowEndTime = false;
            }
            else
            {
                var Background = (int)SchedulerEnum.None;
                e.ViewInfo.AppointmentStyle.BackColor = RepoManager.ParamRepo.GetColorFromEnum((SchedulerEnum)Background, true);
            }
        }

        protected void gvTabOrari_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
            {
                // viene reinizializzato il template di modifica del dato di griglia di modo da evitare problemi per precedenti modifiche
                var templateDic = EditDictionaryManager.GetEditDictionaryTabOrari();
                var template = new PowerFormTemplate(this, templateDic);
                template.IsDetail = true;
                PowerWebContext.SetToSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID), template);

                // viene recuperato il tab da visualizzare in edit form del dettaglio così da poterlo modiifcare in base all'entità master
                var tabToProcess = EditDetailFormTemplate.Dic.FirstOrDefault().Value;

                // recupero la chiave della griglia master che sta aprendo il dettaglio
                // e con essa il relativo tab_orari_tipo id
                var masterKey = (int)gvDetail.GetMasterRowKeyValue();
                var tabOrariMaster = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(tot => tot.Tab_Orari_Tipo_Id == masterKey);
                if (tabOrariMaster != null)
                {
                    // calcolo delle colonne del collaboratore e del cantiere sulla griglia di dettaglio
                    var colColumn = gvDetail.Columns[CommonService.GetPropertyName(() => _tabOrariStub.Col_Id)];
                    var cantColumn = gvDetail.Columns[CommonService.GetPropertyName(() => _tabOrariStub.Cant_Id)];

                    // in base all'entità di riferimento del tipo orario nascondo o visualizzo le colonne di cantiere e collaboratore
                    switch (tabOrariMaster.Tab_Orari_Tipo_Entita_Rif)
                    {
                        case ColEntityType: // in caso di entità collaboratore, nella griglia di dettaglio si visualizza il cantiere e si nasconde il collaboratore
                            colColumn.SetColVisible(false);
                            cantColumn.SetColVisible(true);

                            // si nasconde toglie la modifica del campo collaboratore e si aggiunge se necessario, quella del cantiere
                            if (tabToProcess.Any(tabItem => tabItem.Field == CommonService.GetPropertyName(() => _tabOrariStub.Col_Id)))
                                tabToProcess.Remove(tabToProcess.FirstOrDefault(tabItem => tabItem.Field == CommonService.GetPropertyName(() => _tabOrariStub.Col_Id)));

                            break;
                        case CantEntityType: // in caso di entità collaboratore, nella griglia di dettaglio si visualizza il collaboratore e si nasconde il cantiere
                            colColumn.SetColVisible(true);
                            cantColumn.SetColVisible(false);

                            // si nasconde anche la modifica del campo cantiere
                            if (tabToProcess.Any(tabItem => tabItem.Field == CommonService.GetPropertyName(() => _tabOrariStub.Cant_Id)))
                                tabToProcess.Remove(tabToProcess.FirstOrDefault(tabItem => tabItem.Field == CommonService.GetPropertyName(() => _tabOrariStub.Cant_Id)));

                            break;
                    }
                }

                // impostazione dell'edit form con le eventuali modifiche di cui sopra
                gvDetail.Templates.EditForm = EditDetailFormTemplate;

                

            }
        }

        protected void deTabOrari_OnInit(object sender, EventArgs e)
        {
            ((ASPxDateEdit)sender).Date = DateTime.Today;
        }

        /// <summary>
        /// Handles the CustomCallback event of the gvTabOrariTipo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxGridViewCustomCallbackEventArgs" /> instance containing the event data.</param>
        protected void gvTabOrariTipo_CustomCallback(object sender, ASPxGridViewCustomCallbackEventArgs e)
        {
            // se è richiesta di clonare l'ultimo orario per il selezionato
            if (e.Parameters == "clone")
            {
                // inizializzazione dell'eventuale proprietà di ritorno errori di check
                if (!GridView.JSProperties.ContainsKey(JsCloneValidationErrorKey))
                    GridView.JSProperties.Add(JsCloneValidationErrorKey, String.Empty);

                // se è stato selezionato un tipo orario e una data di inizio di clonazione
                if (GridView.Selection.Count > 0 && !String.IsNullOrEmpty(DeCloneNewDate.Text.Trim()))
                {
                    // viene recuperata la chiave del tipo orario selezionato
                    int selectedKey = (int)GridView.GetSelectedFieldValues(TABORARITIPO_KEYFIELDNAME).First();

                    // viene recuperata la nuova data di inizio da applicare
                    DateTime newStartDate = DeCloneNewDate.Date;

                    // se è stato selezionato un orario su cui clonare il valore allora si imposterà quello sui record
                    // clonati; altrimenti si imposta quello origine della clonazione
                    int newTimetableId = CmbCloneToTimetable.Value != null ? (int)CmbCloneToTimetable.Value : selectedKey;

                    // se sono presenti degli orari collegati al tipo selezionato
                    IEnumerable<Tab_Orari> selectedTimetables = RepoManager.Tab_OrariRepo.Find(to => to.Tab_Orari_Tipo_Id == selectedKey);
                    if (selectedTimetables.Any())
                    {
                        // si recupera la data con cui ricercare i dettagli orario da clonare:
                        // - se non è stata selezionata una data specifica allora si recupera l'ultima per il tipo orari selezionato
                        // - se invece è stata selezionata una data allora si recuperano i dati per quella data
                        DateTime searchTimetableDate = String.IsNullOrEmpty(DeCloneTimetableFromDate.Text) ? selectedTimetables.Select(to => to.Data_Inizio).Max() : DeCloneTimetableFromDate.Date;

                        // con la data e il tipo si recuperano tutti gli orari da clonare
                        IEnumerable<Tab_Orari> timetablesToClone = RepoManager.Tab_OrariRepo.Find(to => to.Tab_Orari_Tipo_Id == selectedKey && to.Data_Inizio == searchTimetableDate);

                        // si procede all'elaborazione solamente se sono stati trovati degli orari in data processabili
                        if (timetablesToClone.Any())
                        {
                            // duplicazione delle entità ed aggiunta degli stessi sulla nuova data
                            List<Tab_Orari> timetablesToAdd = new List<Tab_Orari>();
                            foreach (Tab_Orari timetableToClone in timetablesToClone)
                            {
                                Tab_Orari timetableToAdd = RepoManager.Tab_OrariRepo.Init();
                                CommonService.DuplicateEntity(timetableToClone, timetableToAdd);
                                timetableToAdd.Tab_Orari_Id = 0;
                                timetableToAdd.Data_Inizio = newStartDate;
                                timetableToAdd.Tab_Orari_Tipo_Id = newTimetableId;
                                timetableToAdd.Tab_Orari_Tipo = null;
                                RepoManager.Tab_OrariRepo.SetEntityBeforeAddOrUpdate(timetableToAdd);

                                // prima di aggiungere il dato all'elenco degli orari da inserire a database si effettua la check,
                                // se c'è qualche errore allora lo si stampa a video e si blocca l'inserimento
                                Dictionary<string, string> checkErrors = RepoManager.Tab_OrariRepo.Check(timetableToAdd, true);
                                if (checkErrors.Any())
                                {
                                    GridView.JSProperties[JsCloneValidationErrorKey] = CommonService.StringFromDictionary(checkErrors);
                                    timetablesToAdd = new List<Tab_Orari>();
                                    break;
                                }

                                timetablesToAdd.Add(timetableToAdd);
                            }

                            if (timetablesToAdd.Any())
                            {
                                RepoManager.Tab_OrariRepo.Add(timetablesToAdd, true);
                                GridView.JSProperties[JsCloneValidationErrorKey] = "allOK";
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Imposta in lingua la sezione di gestione della clonazione orari.
        /// </summary>
        private void LocalizeCloneTimetableSection()
        {
            LblCloneTimetable.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_NUOVA_DATA_PER_CLONAZIONE);
            BtnCloneTimetable.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_CLONA_ORARIO);
            LblCloneTimetableDate.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_ORIGINALE_IN_DATA);
            LblIfNullLastTimetable.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_SE_NON_VALORIZZATO_ULTIMA);
            LblCloneToTimetable.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_CLONA_SU_ORARIO);
            LblIfNullSelectedTimetable.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_SE_NON_VALORIZZATO_ATTUALE);
            CloneTimetablePanel.HeaderText = BusinessService.GetLocalizedString(PowerWebResources.STR_CLONAZIONE_ORARI);
        }
    }
}