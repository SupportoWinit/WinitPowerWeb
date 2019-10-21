using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using Domain;
using DevExpress.Web.Data;
using log4net;
using Business;
using System;
using Common;
using DevExpress.Web.ASPxGridView;
using Reports;
using Domain.Exceptions;


namespace PowerWeb.Modules
{
    public partial class TabAutModule : BaseGridModule, IPrintModule, ILogModule
    {
        //DEFINIZIONI
        private Tab_Aut _tabAutStub = null; 
        const String KEYFIELDNAME = "Aut_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(TabAutModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvTabAut;
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

        public bool IsInBatchMode
        {
            get { return GridView.SettingsEditing.Mode == GridViewEditingMode.Batch; }
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryTabAut();
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

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Tab_Aut), gvTabAut);
            PowerWebService.FillComboboxes(gvTabAut);
            BindGrid();
        }

        private void BindGrid()
        {
            gvTabAut.KeyFieldName = KEYFIELDNAME;
            IQueryable<Tab_Aut> currDataSource = Enumerable.Empty<Tab_Aut>().AsQueryable();
            if (IsToPopulateGrid)
            {
                var tabAuts = RepoManager.Tab_AutRepo.GetAll();
                foreach (var tabAut in tabAuts)
                if (tabAut.Tab_Funz != null)
                    tabAut.Nome_Tab_Funz = BusinessService.GetLocalizedString(tabAut.Tab_Funz.Nome_Tab_Funz);
                //La Lettura viene effettuata in base al Valore del TASTO di ON/OFF
                currDataSource = RepoManager.Tab_AutRepo.GetAll(true).AsQueryable();
            }
            gvTabAut.DataSource = currDataSource;                
        }

        protected void gvTabAut_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }       

        public override Type EntityType
        {
            get { return typeof(Tab_Aut); }
        }

        #region gvTabAut : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-Batch_Update
        protected void gvTabAut_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Tab_Aut initTab_Aut = RepoManager.Tab_AutRepo.Init();
                PowerWebService.FillGridProperties(initTab_Aut, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvTabAut_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Tab_Aut newTab_Aut = new Tab_Aut();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvTabAut.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentTab_Aut = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentTab_Aut, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(newTab_Aut, e.NewValues);
            PowerWebService.FillEntityKey(newTab_Aut, e.Keys, KEYFIELDNAME);
            RepoManager.Tab_AutRepo.SetEntityBeforeAddOrUpdate(newTab_Aut);
            PowerWebService.AddValidationErrors(RepoManager.Tab_AutRepo.Check(newTab_Aut, e.IsNewRow), e.Errors, gvTabAut, typeof(TabAutModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvTabAut_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("TAB_AUT-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Tab_Aut newTab_Aut = new Tab_Aut();
            PowerWebService.FillEntityProperties(newTab_Aut, e.NewValues);
            RepoManager.Tab_AutRepo.SetEntityBeforeAddOrUpdate(newTab_Aut);
            RepoManager.Tab_AutRepo.Add(newTab_Aut, true);
            e.Cancel = true;
            gvTabAut.CancelEdit();
            BindGrid();
        }

        protected void gvTabAut_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("TAB_AUT-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTabAut.KeyFieldName]);
            Tab_Aut currentTab_Aut = RepoManager.Tab_AutRepo.Single(ta => ta.Aut_Id == currentId);
            PowerWebService.FillEntityProperties(currentTab_Aut, e.NewValues);
            RepoManager.Tab_AutRepo.SetEntityBeforeAddOrUpdate(currentTab_Aut);
            RepoManager.Tab_AutRepo.SaveChanges();
            e.Cancel = true;
            gvTabAut.CancelEdit();
            BindGrid();
        }

        protected void gvTabAut_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("TAB_AUT-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTabAut.KeyFieldName]);
            Tab_Aut currentTab_Aut = RepoManager.Tab_AutRepo.Single(ta => ta.Aut_Id == currentId);           
            var errors = RepoManager.Tab_AutRepo.CheckBeforeDelete(currentTab_Aut);

            if (errors.Count > 0)
                throw new RowDeletingException(errors);
            else RepoManager.Tab_AutRepo.Delete(currentTab_Aut, true);
            e.Cancel = true;
            BindGrid();
        }

        public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, DevExpress.Web.ASPxGridView.ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _tabAutStub.Data_Registrazione_Aut) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _tabAutStub.DataOraUltimaModifica_Aut))             
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Tab_Aut> tabauts = CommonService.ConvertTo<Tab_Aut>(items);
            XRTab_Aut tabAutsReport = new XRTab_Aut(tabauts);
            return new ExtXtraReport { Report = tabAutsReport, PictureBox = tabAutsReport.CompanyLogo };
        }

        public ILog Log
        {
            get { return _log; }
        }
    }
}