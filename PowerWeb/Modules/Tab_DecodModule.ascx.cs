using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using Domain;
using DevExpress.Web.Data;
using log4net;
using Business;
using System;
using DevExpress.Web.ASPxGridView;
using Reports;
using Common;

namespace PowerWeb.Modules
{
    public partial class Tab_DecodModule : BaseGridModule, ILogModule
    {
        private Tab_Decod _tab_decodStub = null;
        const String KEYFIELDNAME = "Tab_Decod_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Tab_DecodModule));

        public override ASPxGridView GridView
        {
            get { return gvTab_Decods; }
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryTab_Decod();
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
            PowerWebService.FillGridLabels(typeof(Tab_Decod), GridView);
            PowerWebService.FillComboboxes(gvTab_Decods);
            BindGrid();
        }

        private void BindGrid()
        {
            gvTab_Decods.KeyFieldName = KEYFIELDNAME;
            IQueryable<Tab_Decod> currDataSource = Enumerable.Empty<Tab_Decod>().AsQueryable();
            if (IsToPopulateGrid)
            {
                gvTab_Decods.ClearSort();
                //CREA RAGGRUPPAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                gvTab_Decods.GroupBy(gvTab_Decods.Columns[CommonService.GetPropertyName(() => _tab_decodStub.Gruppo_Tab)]);
                gvTab_Decods.GroupBy(gvTab_Decods.Columns[CommonService.GetPropertyName(() => _tab_decodStub.Nome_Tab)]);
                //CREA ORDINAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                (gvTab_Decods.Columns[CommonService.GetPropertyName(() => _tab_decodStub.Gruppo_Tab)] as GridViewDataColumn).SortAscending();
                (gvTab_Decods.Columns[CommonService.GetPropertyName(() => _tab_decodStub.Nome_Tab)] as GridViewDataColumn).SortAscending();
                (gvTab_Decods.Columns[CommonService.GetPropertyName(() => _tab_decodStub.Chiave_Tab)] as GridViewDataColumn).SortAscending();
                //La Lettura viene effettuata in base al Valore del TASTO di ON/OFF
                currDataSource = RepoManager.Tab_DecodRepo.GetAll(true).AsQueryable();
            }
            gvTab_Decods.DataSource = currDataSource;
        }        

        protected void gvTab_Decods_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Tab_Decod); }
        }

        #region gvTab_Decods : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting
        protected void gvTab_Decods_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Tab_Decod initTab_Decod = RepoManager.Tab_DecodRepo.Init();
                PowerWebService.FillGridProperties(initTab_Decod, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvTab_Decods_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Tab_Decod newTab_Decod = new Tab_Decod();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvTab_Decods.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentTab_Decod = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentTab_Decod, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(newTab_Decod, e.NewValues);
            PowerWebService.FillEntityKey(newTab_Decod, e.Keys, KEYFIELDNAME);
            RepoManager.Tab_DecodRepo.SetEntityBeforeAddOrUpdate(newTab_Decod);
            PowerWebService.AddValidationErrors(RepoManager.Tab_DecodRepo.Check(newTab_Decod, e.IsNewRow), e.Errors, gvTab_Decods, typeof(Tab_DecodModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);            
        }

        protected void gvTab_Decods_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("TAB_DECOD-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Tab_Decod newTab_Decod = new Tab_Decod();
            PowerWebService.FillEntityProperties(newTab_Decod, e.NewValues);
            RepoManager.Tab_DecodRepo.SetEntityBeforeAddOrUpdate(newTab_Decod);
            RepoManager.Tab_DecodRepo.Add(newTab_Decod, true);
            e.Cancel = true;
            gvTab_Decods.CancelEdit();
            BindGrid();
        }

        protected void gvTab_Decods_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("TAB_DECOD-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTab_Decods.KeyFieldName]);
            Tab_Decod currentTab_Decod = RepoManager.Tab_DecodRepo.Single(u => u.Tab_Decod_Id == currentId);
            PowerWebService.FillEntityProperties(currentTab_Decod, e.NewValues);
            RepoManager.Tab_DecodRepo.SetEntityBeforeAddOrUpdate(currentTab_Decod);
            RepoManager.Tab_DecodRepo.SaveChanges();
            e.Cancel = true;
            gvTab_Decods.CancelEdit();
            BindGrid();
        }

        protected void gvTab_Decods_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("TAB_DECOD-Row deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTab_Decods.KeyFieldName]);
            Tab_Decod currentTab_Decod = RepoManager.Tab_DecodRepo.Single(u => u.Tab_Decod_Id == currentId);
            RepoManager.Tab_DecodRepo.Delete(currentTab_Decod, true);

            e.Cancel = true;
            BindGrid();
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (VUOTA perchè NON ci sono Campi Date da Gestire ma va comunque definita vuota se non ce ne sono)
        {
        }
        public ILog Log
        {
            get { return _log; }
        }

    }
}