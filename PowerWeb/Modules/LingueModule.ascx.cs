using System;
using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using Domain;
using Common;
using DevExpress.Web.Data;
using DevExpress.Web.ASPxGridView;
using Business;
using Reports;
using log4net;

namespace PowerWeb.Modules
{
    public partial class LingueModule : BaseGridModule, ILogModule
    {
        //DEFINIZIONI
        private Lingue _lingueStub = null;
        const String KEYFIELDNAME = "Lingue_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(LingueModule));

        public override ASPxGridView GridView
        {
            get { return gvLingue; }
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryLingue();
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
            PowerWebService.FillGridLabels(typeof(Lingue), GridView);
            PowerWebService.FillComboboxes(gvLingue);
            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvLingue.KeyFieldName = KEYFIELDNAME;
            IQueryable<Lingue> currDataSource = Enumerable.Empty<Lingue>().AsQueryable();
            var emptyList = Enumerable.Empty<Lingue>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.LingueRepo.GetAll(true).AsQueryable();
                gvLingue.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvLingue.DataSource = emptyList;

        }

        protected void gvLingue_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Lingue); }
        }

        #region gvLingue : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-Batch_Update
        protected void gvLingue_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Lingue initLingue = RepoManager.LingueRepo.Init();
                PowerWebService.FillGridProperties(initLingue, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvLingue_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Lingue newLingue = new Lingue();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvLingue.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentLingue = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentLingue, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(newLingue, e.NewValues);
            PowerWebService.FillEntityKey(newLingue, e.Keys, KEYFIELDNAME);
            RepoManager.LingueRepo.SetEntityBeforeAddOrUpdate(newLingue);
            PowerWebService.AddValidationErrors(RepoManager.LingueRepo.Check(newLingue, e.IsNewRow), e.Errors, gvLingue, typeof(LingueModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvLingue_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("LINGUE-Row inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Lingue newLingue = new Lingue();
            PowerWebService.FillEntityProperties(newLingue, e.NewValues);
            RepoManager.LingueRepo.SetEntityBeforeAddOrUpdate(newLingue);
            RepoManager.LingueRepo.Add(newLingue, true);
            e.Cancel = true;
            gvLingue.CancelEdit();
            BindGrid();
        }

        protected void gvLingue_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("LINGUE-Row updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvLingue.KeyFieldName]);
            Lingue currentLingue = RepoManager.LingueRepo.Single(u => u.Lingue_Id == currentId);
            PowerWebService.FillEntityProperties(currentLingue, e.NewValues);
            RepoManager.LingueRepo.SetEntityBeforeAddOrUpdate(currentLingue);
            RepoManager.LingueRepo.SaveChanges();
            e.Cancel = true;
            gvLingue.CancelEdit();
            BindGrid();
        }

        protected void gvLingue_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("LINGUE-Row deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvLingue.KeyFieldName]);
            Lingue currentLingue = RepoManager.LingueRepo.Single(u => u.Lingue_Id == currentId);
            RepoManager.LingueRepo.Delete(currentLingue, true);
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
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _lingueStub.Data_Registrazione_Lingue) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _lingueStub.DataOraUltimaModifica_Lingue))    
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ILog Log
        {
            get { return _log; }
        }


    }
}