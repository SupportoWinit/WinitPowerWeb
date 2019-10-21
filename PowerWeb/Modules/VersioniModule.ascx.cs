using System.Collections.Generic;
using Business.Repository;
using Domain;
using Common;
using DevExpress.Web.Data;
using log4net;
using Reports;
using System;
using DevExpress.Web.ASPxGridView;
using System.Linq;

namespace PowerWeb.Modules
{
    public partial class VersioniModule : BaseGridModule, ILogModule
    {

        private Versioni _versioniStub = null;
        const String KEYFIELDNAME = "Versioni_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(VersioniModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvVersioni;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryVersioni();
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
            PowerWebService.FillGridLabels(typeof(Versioni), GridView);
            PowerWebService.FillComboboxes(gvVersioni);
            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvVersioni.KeyFieldName = KEYFIELDNAME;
            IQueryable<Versioni> currDataSource = Enumerable.Empty<Versioni>().AsQueryable();
            var emptyList = Enumerable.Empty<Versioni>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.VersioniRepo.GetAll(true).AsQueryable();
                gvVersioni.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvVersioni.DataSource = emptyList;
        }

        protected void gvVersioni_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Versioni); }
        }

        #region gvVersioni : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BatchUpdate
        protected void gvVersioni_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;

            if (grid != null)
            {
                Versioni initVersioni = RepoManager.VersioniRepo.Init();
                var registrationDate = initVersioni.Data_Registrazione_Versioni;

                PowerWebService.FillGridProperties(initVersioni, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);

                e.NewValues[CommonService.GetPropertyName(() => _versioniStub.Data_Registrazione_Versioni)] = registrationDate;
            }
        }

        protected void gvVersioni_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Versioni version = new Versioni();

            //se sono in batch edit mode carico tutti i campi in questo modo ho sempre tutti i campi aggiornati
            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvVersioni.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentVersion = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentVersion, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(version, e.NewValues);
            PowerWebService.FillEntityKey(version, e.Keys, KEYFIELDNAME);
            RepoManager.VersioniRepo.SetEntityBeforeAddOrUpdate(version);
            PowerWebService.AddValidationErrors(RepoManager.VersioniRepo.Check(version, e.IsNewRow), e.Errors, gvVersioni, typeof(VersioniModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvVersioni_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("VERSIONI-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Versioni newVersion = RepoManager.VersioniRepo.Init();
            PowerWebService.FillEntityProperties(newVersion, e.NewValues);
            RepoManager.VersioniRepo.SetEntityBeforeAddOrUpdate(newVersion);
            RepoManager.VersioniRepo.Add(newVersion, true);

            e.Cancel = true;
            gvVersioni.CancelEdit();
            BindGrid();
        }

        protected void gvVersioni_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("VERSIONI-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvVersioni.KeyFieldName]);
            Versioni currentVersion = RepoManager.VersioniRepo.Single(u => u.Versioni_Id == currentId);
            PowerWebService.FillEntityProperties(currentVersion, e.NewValues);
            RepoManager.VersioniRepo.SetEntityBeforeAddOrUpdate(currentVersion);
            RepoManager.VersioniRepo.SaveChanges();
            e.Cancel = true;
            gvVersioni.CancelEdit();
            BindGrid();
        }

        protected void gvVersioni_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("VERSIONI-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvVersioni.KeyFieldName]);
            Versioni currentVersion = RepoManager.VersioniRepo.Single(u => u.Versioni_Id == currentId);
            RepoManager.VersioniRepo.Delete(currentVersion, false);
            RepoManager.VersioniRepo.SaveChanges();
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
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _versioniStub.Data_Registrazione_Versioni) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _versioniStub.DataOraUltimaModifica_Versioni))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ILog Log
        {
            get { return _log; }
        }

        public System.Collections.IEnumerable DataSource
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}