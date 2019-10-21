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


namespace PowerWeb.Modules
{
    public partial class Tab_FestiviModule : BaseGridModule, ILogModule
    {
        //DEFINIZIONI
        private Tab_Festivi _tabFestiviStub = null;
        const String KEYFIELDNAME = "Tab_Festivi_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Tab_FestiviModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvTabFestivi;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryTab_Festivi();
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
            PowerWebService.FillGridLabels(typeof(Tab_Festivi), GridView);
            PowerWebService.FillComboboxes(gvTabFestivi);
            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvTabFestivi.KeyFieldName = KEYFIELDNAME;
            IQueryable<Tab_Festivi> currDataSource = Enumerable.Empty<Tab_Festivi>().AsQueryable();
            var emptyList = Enumerable.Empty<Tab_Festivi>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.Tab_FestiviRepo.GetAll(true).AsQueryable();
                gvTabFestivi.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvTabFestivi.DataSource = emptyList;
        }

        protected void gvTabFestivi_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Tab_Festivi); }
        }

        #region gvTabFestivi : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-Batch_Update
        protected void gvTabFestivi_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            Tab_Festivi initTab_Festivi = RepoManager.Tab_FestiviRepo.Init();
            PowerWebService.FillGridProperties(initTab_Festivi, e.NewValues);
        }

        protected void gvTabFestivi_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Tab_Festivi newTab_Festivi = new Tab_Festivi();
            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvTabFestivi.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentTab_Festivi = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentTab_Festivi, e.NewValues, e.OldValues);
                }
            }
            PowerWebService.FillEntityProperties(newTab_Festivi, e.NewValues);
            PowerWebService.FillEntityKey(newTab_Festivi, e.Keys, KEYFIELDNAME);
            RepoManager.Tab_FestiviRepo.SetEntityBeforeAddOrUpdate(newTab_Festivi);
            PowerWebService.AddValidationErrors(
              RepoManager.Tab_FestiviRepo.Check(newTab_Festivi, e.IsNewRow), e.Errors, gvTabFestivi, typeof(Tab_FestiviModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvTabFestivi_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("TAB_FESTIVI-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Tab_Festivi newTab_Festivi = new Tab_Festivi();
            PowerWebService.FillEntityProperties(newTab_Festivi, e.NewValues);
            RepoManager.Tab_FestiviRepo.SetEntityBeforeAddOrUpdate(newTab_Festivi);           
            RepoManager.Tab_FestiviRepo.Add(newTab_Festivi, true);                
            e.Cancel = true;
            gvTabFestivi.CancelEdit();
            BindGrid();
        }

        protected void gvTabFestivi_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("TAB_FESTIVI-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTabFestivi.KeyFieldName]);
            Tab_Festivi currentTab_Festivi = RepoManager.Tab_FestiviRepo.Single(ta => ta.Tab_Festivi_Id == currentId);
            PowerWebService.FillEntityProperties(currentTab_Festivi, e.NewValues);
            RepoManager.Tab_FestiviRepo.SetEntityBeforeAddOrUpdate(currentTab_Festivi);
            try
            {
                RepoManager.Tab_FestiviRepo.SaveChanges();
                _log.Info(String.Format("Updated TAB_FESTIVI id {0}", currentId));
            }
            catch (Exception ex)
            {
                _log.Error("Unable to Update TAB_FESTIVI", ex);
            }
            e.Cancel = true;
            gvTabFestivi.CancelEdit();
            BindGrid();
        }

        protected void gvTabFestivi_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("TAB_FESTIVI-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTabFestivi.KeyFieldName]);
            Tab_Festivi currentTab_Festivi = RepoManager.Tab_FestiviRepo.Single(ta => ta.Tab_Festivi_Id == currentId);
            try
            {
                RepoManager.Tab_FestiviRepo.Delete(currentTab_Festivi, true);
                _log.Info(String.Format("Deleted TAB_FESTIVI id {0}", currentId));
            }
            catch (Exception ex)
            {
                _log.Error("Unable to Delete TAB_FESTIVI", ex);
            }
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
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _tabFestiviStub.Giorno_Tab_Festivi))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ILog Log
        {
            get { return _log; }
        }

        public override void ResetSession()
        {
        }
    }
}