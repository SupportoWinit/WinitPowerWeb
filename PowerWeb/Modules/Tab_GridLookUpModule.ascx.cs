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
    public partial class TabGridLookUpModule : BaseGridModule, ILogModule
    {
        private Tab_GridLookup _tabGridLookUpStub = null;
        const String KEYFIELDNAME = "Tab_GridLookup_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(TabGridLookUpModule));

        public override ASPxGridView GridView
        {
            get { return gvTabGridLookUp; }
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
            get { return null; }
        }

        public override PowerFormTemplate EditDetailFormTemplate
        {
            get { return null; }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Tab_GridLookup), GridView);
            PowerWebService.FillComboboxes(gvTabGridLookUp);
            BindGrid();
        }

        private void BindGrid()
        {
            gvTabGridLookUp.KeyFieldName = KEYFIELDNAME;
            IQueryable<Tab_GridLookup> currDataSource = Enumerable.Empty<Tab_GridLookup>().AsQueryable();
            if (IsToPopulateGrid)
            {
                gvTabGridLookUp.ClearSort();
                //CREA RAGGRUPPAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                gvTabGridLookUp.GroupBy(gvTabGridLookUp.Columns[CommonService.GetPropertyName(() => _tabGridLookUpStub.NomeRicerca)]);
                //CREA ORDINAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                (gvTabGridLookUp.Columns[CommonService.GetPropertyName(() => _tabGridLookUpStub.NomeRicerca)] as GridViewDataColumn).SortAscending();
                (gvTabGridLookUp.Columns[CommonService.GetPropertyName(() => _tabGridLookUpStub.Ordinamento)] as GridViewDataColumn).SortAscending();
                //La Lettura viene effettuata in base al Valore del TASTO di ON/OFF
                currDataSource = RepoManager.Tab_GridLookupRepo.GetAll(true).AsQueryable();
            }
            gvTabGridLookUp.DataSource = currDataSource;
        }

        protected void gvTabGridLookUp_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Tab_GridLookup); }
        }

        #region gvTabGridLookUp : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-Batch_Update
        protected void gvTabGridLookUp_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Tab_GridLookup initTab_GridLookup = RepoManager.Tab_GridLookupRepo.Init();
                PowerWebService.FillGridProperties(initTab_GridLookup, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvTabGridLookUp_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Tab_GridLookup newTab_GridLookUp = new Tab_GridLookup();
            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvTabGridLookUp.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentTab_GridLookUp = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentTab_GridLookUp, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(newTab_GridLookUp, e.NewValues);
            PowerWebService.FillEntityKey(newTab_GridLookUp, e.Keys, KEYFIELDNAME);
            RepoManager.Tab_GridLookupRepo.SetEntityBeforeAddOrUpdate(newTab_GridLookUp);
            PowerWebService.AddValidationErrors(RepoManager.Tab_GridLookupRepo.Check(newTab_GridLookUp, e.IsNewRow), e.Errors, gvTabGridLookUp, typeof(TabGridLookUpModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvTabGridLookUp_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("TAB_GRIDLOOKUP-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Tab_GridLookup newTab_GridLookUp = new Tab_GridLookup();
            PowerWebService.FillEntityProperties(newTab_GridLookUp, e.NewValues);
            RepoManager.Tab_GridLookupRepo.SetEntityBeforeAddOrUpdate(newTab_GridLookUp);
            RepoManager.Tab_GridLookupRepo.Add(newTab_GridLookUp, true);
            e.Cancel = true;
            gvTabGridLookUp.CancelEdit();
            BindGrid();
        }

        protected void gvTabGridLookUp_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("TAB_GRIDLOOKUP-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTabGridLookUp.KeyFieldName]);
            Tab_GridLookup currentTab_GridLookUp = RepoManager.Tab_GridLookupRepo.Single(u => u.Tab_GridLookup_Id == currentId);
            PowerWebService.FillEntityProperties(currentTab_GridLookUp, e.NewValues);
            RepoManager.Tab_GridLookupRepo.SetEntityBeforeAddOrUpdate(currentTab_GridLookUp);
            RepoManager.Tab_GridLookupRepo.SaveChanges();
            e.Cancel = true;
            gvTabGridLookUp.CancelEdit();
            BindGrid();
        }

        protected void gvTabGridLookUp_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("TAB_GRIDLOOKUP-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTabGridLookUp.KeyFieldName]);
            Tab_GridLookup currentTab_GridLookUp = RepoManager.Tab_GridLookupRepo.Single(u => u.Tab_GridLookup_Id == currentId);
            var errors = RepoManager.Tab_GridLookupRepo.CheckBeforeDelete(currentTab_GridLookUp);

            if (errors.Count > 0)
                throw new RowDeletingException(errors);
            else RepoManager.Tab_GridLookupRepo.Delete(currentTab_GridLookUp, true);

            e.Cancel = true;
            BindGrid();
        }

        public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        }

        #endregion

        public override void HeaderFilterFillItems(object sender, DevExpress.Web.ASPxGridView.ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (VUOTA perchè NON ci sono Campi Date da Gestire ma va comunque definita vuota se non ce ne sono)
        {
        }

        public ILog Log
        {
            get { return _log; }
        }


    }
}