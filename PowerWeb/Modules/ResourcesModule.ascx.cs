using Business.Repository;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.Data;
using Domain;
using log4net;
using System;
using System.Linq;

namespace PowerWeb.Modules
{
    public partial class ResourcesModule : BaseGridModule, ILogModule
    {
        //DEFINIZIONI
        const String KEYFIELDNAME = "Resources_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(ResourcesModule));

        public override ASPxGridView GridView
        {
            get { return gvResources; }
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryResources();
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
            PowerWebService.FillGridLabels(typeof(Resources), GridView);
            PowerWebService.FillComboboxes(gvResources);
            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvResources.KeyFieldName = KEYFIELDNAME;
            IQueryable<Resources> currDataSource = Enumerable.Empty<Resources>().AsQueryable();
            var emptyList = Enumerable.Empty<Resources>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.ResourcesRepo.GetAll(true).AsQueryable();
                gvResources.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvResources.DataSource = emptyList;
        }

        protected void gvResources_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Resources); }
        }

        #region gvResources : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-Batch_Update
        protected void gvResources_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Resources initResources = RepoManager.ResourcesRepo.Init();
                PowerWebService.FillGridProperties(initResources, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvResources_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Resources newResources = new Resources();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvResources.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentResources = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentResources, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(newResources, e.NewValues);
            PowerWebService.FillEntityKey(newResources, e.Keys, KEYFIELDNAME);
            RepoManager.ResourcesRepo.SetEntityBeforeAddOrUpdate(newResources);
            PowerWebService.AddValidationErrors(RepoManager.ResourcesRepo.Check(newResources, e.IsNewRow), e.Errors, gvResources, typeof(ResourcesModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvResources_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("Resources-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Resources newResources = new Resources();
            PowerWebService.FillEntityProperties(newResources, e.NewValues);
            RepoManager.ResourcesRepo.SetEntityBeforeAddOrUpdate(newResources);
            RepoManager.ResourcesRepo.Add(newResources, true);
            e.Cancel = true;
            gvResources.CancelEdit();
            BindGrid();
        }

        protected void gvResources_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("Resources_Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvResources.KeyFieldName]);
            Resources currentResources = RepoManager.ResourcesRepo.Single(u => u.Resources_Id == currentId);
            PowerWebService.FillEntityProperties(currentResources, e.NewValues);
            RepoManager.ResourcesRepo.SetEntityBeforeAddOrUpdate(currentResources);
            RepoManager.ResourcesRepo.SaveChanges();
            e.Cancel = true;
            gvResources.CancelEdit();
            BindGrid();
        }

        protected void gvResources_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("REPS_Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvResources.KeyFieldName]);
            Resources currentResources = RepoManager.ResourcesRepo.Single(u => u.Resources_Id == currentId);
            RepoManager.ResourcesRepo.Delete(currentResources, true);
            e.Cancel = true;
            BindGrid();
        }

        public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }
    }
}