using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using log4net;
using Domain;
using Business.Repository;
using DevExpress.Web.ASPxUploadControl;
using DevExpress.Web.Data;
using System.IO;
using System.Text;
using Common;
using System.Drawing;
using System.ComponentModel;
using Business;
using DevExpress.Web.ASPxGridView;
using Reports;
using DevExpress.Web.ASPxClasses;
using Business.GeocodeServiceReference;
using DevExpress.Web.ASPxEditors;
using DevExpress.Web.ASPxFormLayout;

namespace PowerWeb.Modules
{
    public partial class Tab_FunzModule : BaseGridModule, ILogModule
    {
        //DEFINIZIONI
        const String KEYFIELDNAME = "Tab_Funz_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Tab_FunzModule));       

        public override ASPxGridView GridView
        {
            get { return gvTab_Funz; }
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryTab_Funz();
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
            PowerWebService.FillGridLabels(typeof(Tab_Funz), GridView);
            PowerWebService.FillComboboxes(gvTab_Funz);
            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvTab_Funz.KeyFieldName = KEYFIELDNAME;
            IQueryable<Tab_Funz> currDataSource = Enumerable.Empty<Tab_Funz>().AsQueryable();
            var emptyList = Enumerable.Empty<Tab_Funz>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.Tab_FunzRepo.GetAll(true).AsQueryable();
                gvTab_Funz.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvTab_Funz.DataSource = emptyList;
        }

        protected void gvTab_Funz_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Param); }
        }

        #region gvPru : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BatchUpdate
        protected void gvTab_Funz_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Tab_Funz initTab_Funz = RepoManager.Tab_FunzRepo.Init();
                PowerWebService.FillGridProperties(initTab_Funz, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvTab_Funz_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {           

            Tab_Funz initTab_Funz = new Tab_Funz();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvTab_Funz.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentTab_Funz = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentTab_Funz, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(initTab_Funz, e.NewValues);
            PowerWebService.FillEntityKey(initTab_Funz , e.Keys, KEYFIELDNAME);
            RepoManager.Tab_FunzRepo.SetEntityBeforeAddOrUpdate(initTab_Funz);
            PowerWebService.AddValidationErrors(RepoManager.Tab_FunzRepo.Check(initTab_Funz, e.IsNewRow), e.Errors, gvTab_Funz, typeof(Tab_FunzModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvTab_Funz_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("TAB_FUNZ-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Tab_Funz newTab_Funz = new Tab_Funz();
            PowerWebService.FillEntityProperties(newTab_Funz, e.NewValues);
            RepoManager.Tab_FunzRepo.SetEntityBeforeAddOrUpdate(newTab_Funz);
            RepoManager.Tab_FunzRepo.Add(newTab_Funz, true);
            e.Cancel = true;
            gvTab_Funz.CancelEdit();
            BindGrid();
        }
        protected void gvTab_Funz_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("TAB_FUNZ-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTab_Funz.KeyFieldName]);
            Tab_Funz currentTab_Funz = RepoManager.Tab_FunzRepo.Single(ta => ta.Tab_Funz_Id == currentId);
            PowerWebService.FillEntityProperties(currentTab_Funz, e.NewValues);
            RepoManager.Tab_FunzRepo.SetEntityBeforeAddOrUpdate(currentTab_Funz);
            try
            {
                RepoManager.Tab_FunzRepo.SaveChanges();
                _log.Info(String.Format("Updated TAB_FUNZ id {0}", currentId));
            }
            catch (Exception ex)
            {
                _log.Error("Unable to Update TAB_FUNZ", ex);
            }
            e.Cancel = true;
            gvTab_Funz.CancelEdit();
            BindGrid();
        }

        protected void gvTab_Funz_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("TAB_FUNZ-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTab_Funz.KeyFieldName]);
            Tab_Funz currentTab_Funz = RepoManager.Tab_FunzRepo.Single(u => u.Tab_Funz_Id == currentId);
            RepoManager.Tab_FunzRepo.Delete(currentTab_Funz, true);
            e.Cancel = true;
            BindGrid();
        }

        public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //NON vengono Gestiti Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {           
        }
       
        public log4net.ILog Log
        {
            get { return _log; }
        }       
    }
}