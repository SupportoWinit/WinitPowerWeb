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
    public partial class Tab_EditFormTemplateModule : BaseGridModule, ILogModule
    {
        private Tab_EditFormTemplate _tab_EditFormTemplateStub = null;
        const String KEYFIELDNAME = "Tab_EditFormTemplate_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Tab_EditFormTemplateModule));

        public override ASPxGridView GridView
        {
            get { return gvTab_EditFormTemplate; }
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryTab_EditFormTemplate();
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
            PowerWebService.FillGridLabels(typeof(Tab_EditFormTemplate), GridView);
            PowerWebService.FillComboboxes(gvTab_EditFormTemplate);
            BindGrid();
        }

        private void BindGrid()
        {
            gvTab_EditFormTemplate.KeyFieldName = KEYFIELDNAME;
            IQueryable<Tab_EditFormTemplate> currDataSource = Enumerable.Empty<Tab_EditFormTemplate>().AsQueryable();
            if (IsToPopulateGrid)
            {
                gvTab_EditFormTemplate.ClearSort();
                //CREA RAGGRUPPAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                gvTab_EditFormTemplate.GroupBy(gvTab_EditFormTemplate.Columns[CommonService.GetPropertyName(() => _tab_EditFormTemplateStub.Entity_Tab_EditFormTemplate)]);                
                //CREA ORDINAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                (gvTab_EditFormTemplate.Columns[CommonService.GetPropertyName(() => _tab_EditFormTemplateStub.Entity_Tab_EditFormTemplate)] as GridViewDataColumn).SortAscending();
                
                //La Lettura viene effettuata in base al Valore del TASTO di ON/OFF
                currDataSource = RepoManager.Tab_EditFormTemplateRepo.GetAll(true).AsQueryable();
            }
            gvTab_EditFormTemplate.DataSource = currDataSource;
        }        

        protected void gvTab_EditFormTemplate_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Tab_EditFormTemplate); }
        }

        #region gvTab_EditFormTemplate : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting
        protected void gvTab_EditFormTemplate_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Tab_EditFormTemplate initTab_Decod = RepoManager.Tab_EditFormTemplateRepo.Init();
                PowerWebService.FillGridProperties(initTab_Decod, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvTab_EditFormTemplate_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Tab_EditFormTemplate newTab_EditFormTemplate = new Tab_EditFormTemplate();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvTab_EditFormTemplate.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentTab_EditFormTemplate = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentTab_EditFormTemplate, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(newTab_EditFormTemplate, e.NewValues);
            PowerWebService.FillEntityKey(newTab_EditFormTemplate, e.Keys, KEYFIELDNAME);
            RepoManager.Tab_EditFormTemplateRepo.SetEntityBeforeAddOrUpdate(newTab_EditFormTemplate);
            PowerWebService.AddValidationErrors(RepoManager.Tab_EditFormTemplateRepo.Check(newTab_EditFormTemplate, e.IsNewRow), e.Errors, gvTab_EditFormTemplate, typeof(Tab_EditFormTemplateModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);            
        }

        protected void gvTab_EditFormTemplate_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("TAB_EDITFORMTEMPLATE-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Tab_EditFormTemplate newTab_EditFormTemplate = new Tab_EditFormTemplate();
            PowerWebService.FillEntityProperties(newTab_EditFormTemplate, e.NewValues);
            RepoManager.Tab_EditFormTemplateRepo.SetEntityBeforeAddOrUpdate(newTab_EditFormTemplate);
            RepoManager.Tab_EditFormTemplateRepo.Add(newTab_EditFormTemplate, true);
            e.Cancel = true;
            gvTab_EditFormTemplate.CancelEdit();
            BindGrid();
        }

        protected void gvTab_EditFormTemplate_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("TAB_EDITFORMTEMPLATE-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTab_EditFormTemplate.KeyFieldName]);
            Tab_EditFormTemplate currentTab_EditFormTemplate = RepoManager.Tab_EditFormTemplateRepo.Single(u => u.Tab_EditFormTemplate_Id == currentId);
            PowerWebService.FillEntityProperties(currentTab_EditFormTemplate, e.NewValues);
            RepoManager.Tab_EditFormTemplateRepo.SetEntityBeforeAddOrUpdate(currentTab_EditFormTemplate);
            RepoManager.Tab_EditFormTemplateRepo.SaveChanges();
            e.Cancel = true;
            gvTab_EditFormTemplate.CancelEdit();
            BindGrid();
        }

        protected void gvTab_EditFormTemplate_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("TAB_EDITFORMTEMPLATE-Row deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTab_EditFormTemplate.KeyFieldName]);
            Tab_EditFormTemplate currentTab_EditFormTemplate = RepoManager.Tab_EditFormTemplateRepo.Single(u => u.Tab_EditFormTemplate_Id == currentId);
            RepoManager.Tab_EditFormTemplateRepo.Delete(currentTab_EditFormTemplate, true);

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