using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using Domain;
using DevExpress.Web.Data;
using Business;
using System;
using Common;
using DevExpress.Web.ASPxGridView;
using Reports;
using log4net;


namespace PowerWeb.Modules
{
    public partial class Tab_CheckImpModule : BaseGridModule, ILogModule
    {
        private Tab_Chk_Imp _tab_checkimpStub = null;
        const String KEYFIELDNAME = "Tab_Chk_Imp_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Tab_CheckImpModule));

        public override ASPxGridView GridView
        {
            get
            { return gvTabCheckImp; }
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
            PowerWebService.FillGridLabels(typeof(Tab_Chk_Imp), GridView);
            PowerWebService.FillComboboxes(gvTabCheckImp);
            BindGrid();
        }

        private void BindGrid()
        {
            gvTabCheckImp.KeyFieldName = KEYFIELDNAME;
            gvTabCheckImp.DataSource = RepoManager.Tab_Chk_ImpRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)               
            {
                gvTabCheckImp.ClearSort();
                //CREA RAGGRUPPAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                gvTabCheckImp.GroupBy(gvTabCheckImp.Columns[CommonService.GetPropertyName(() => _tab_checkimpStub.Stato_Record_Tab_Check_Imp )]);
                gvTabCheckImp.GroupBy(gvTabCheckImp.Columns[CommonService.GetPropertyName(() => _tab_checkimpStub.Nome_Tabella_Tab_Check_Imp )]);
                //CREA ORDINAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                (gvTabCheckImp.Columns[CommonService.GetPropertyName(() => _tab_checkimpStub.Stato_Record_Tab_Check_Imp )] as GridViewDataColumn).SortAscending();
                (gvTabCheckImp.Columns[CommonService.GetPropertyName(() => _tab_checkimpStub.Nome_Tabella_Tab_Check_Imp )] as GridViewDataColumn).SortAscending();                
                gvTabCheckImp.DataBind();
            }                
        }

        public override Type EntityType
        {
            get { return typeof(Tab_Chk_Imp); }
        }

        #region gvTabCheckImp : SOLO RowDeleting

        protected void gvTabCheckImp_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("TAB_CHECK_IMP-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvTabCheckImp.KeyFieldName]);
            Tab_Chk_Imp currentTab_CheckImp = RepoManager.Tab_Chk_ImpRepo.Single(ta => ta.Tab_Chk_Imp_Id == currentId);
            RepoManager.Tab_Chk_ImpRepo.Delete(currentTab_CheckImp, true);
            e.Cancel = true;
            BindGrid();
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