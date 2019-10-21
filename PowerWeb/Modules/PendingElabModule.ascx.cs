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
    public partial class PendingElabModule : BaseGridModule, ILogModule
    {
        const String KEYFIELDNAME = "PendingElab_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(PendingElabModule));

        public override ASPxGridView GridView
        {
            get
            { return gvPendingElab; }
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
            PowerWebService.FillComboboxes(gvPendingElab);
            BindGrid();
        }

        private void BindGrid()
        {
            gvPendingElab.KeyFieldName = KEYFIELDNAME;
            gvPendingElab.DataSource = RepoManager.PendingElabRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)           
                gvPendingElab.DataBind();               
        }

        public override Type EntityType
        {
            get { return typeof(PendingElab ); }
        }

        #region gvPendingElab : SOLO RowDeleting

        protected void gvPendingElab_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("PENDINGELAB-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvPendingElab.KeyFieldName]);
            PendingElab  currentPendingElab = RepoManager.PendingElabRepo.Single(ta => ta.PendingElab_Id == currentId);
            RepoManager.PendingElabRepo.Delete(currentPendingElab, true);
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