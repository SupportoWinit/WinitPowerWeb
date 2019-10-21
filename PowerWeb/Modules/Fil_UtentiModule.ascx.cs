using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Domain;
using Business.Repository;
using DevExpress.Web.ASPxGridView;
using Business;
using Common;
using Reports;
using log4net;

namespace PowerWeb.Modules
{
    public partial class Fil_UtentiModule : BaseGridModule, ILogModule
    {
        //DEFINIZIONI
        private Fil _filStub = null;
        const String KEYFIELDNAME = "Fil_Id";
        const String DETAILKEYFIELDNAME = "Utenti_Fil_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Utenti_FilModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvFil_Utenti;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryUtenti_Fil();
                    template = new PowerFormTemplate(this, templateDic);
                    PowerWebContext.SetToSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID, template);
                }
                return template;
            }
        }

        public override PowerFormTemplate EditDetailFormTemplate
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID));
                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryFil_Utenti();
                    template = new PowerFormTemplate(this, templateDic);
                    template.IsDetail = true;
                    PowerWebContext.SetToSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID), template);
                }
                return template;
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Fil), gvFil_Utenti);
            PowerWebService.FillComboboxes(gvFil_Utenti);
            BindGrid();
        }

        private void BindGrid()
        {
            gvFil_Utenti.KeyFieldName = KEYFIELDNAME;
            gvFil_Utenti.DataSource = RepoManager.FilRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)
                gvFil_Utenti.DataBind();
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int FilId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Utenti_FilRepo.Find(pc => pc.Fil_Id == FilId).ToList();
        }

        public override Type EntityType
        {
            get { return typeof(Fil); }
        }

        #region gvFil_Utenti_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect-DetailRowExpandedChanged
        protected void gvFil_Utenti_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
        }

        protected void gvFil_Utenti_Detail_InitNewRow(object sender, DevExpress.Web.Data.ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
            {
                Utenti_Fil newUtentiFil = RepoManager.Utenti_FilRepo.Init();
                PowerWebService.FillGridProperties(newUtentiFil, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, gvDetail, e.NewValues);
            }
        }

        protected void gvFil_Utenti_Detail_RowValidating(object sender, DevExpress.Web.Data.ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                Utenti_Fil newFilUtenti = RepoManager.Utenti_FilRepo.Init();

                if (IsInBatchMode)
                {
                    var currentId = Convert.ToInt32(e.Keys[gvDetails.KeyFieldName]);
                    if (currentId > 0)
                    {
                        var FilUtenti = GridView.GetRow(e.VisibleIndex);
                        PowerWebService.FillValues(FilUtenti, e.NewValues, e.OldValues);
                    }
                }

                int FilId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
                newFilUtenti.Fil_Id = FilId;
                PowerWebService.FillEntityProperties(newFilUtenti, e.NewValues);
                PowerWebService.FillEntityKey(newFilUtenti, e.Keys, DETAILKEYFIELDNAME);
                RepoManager.Utenti_FilRepo.SetEntityBeforeAddOrUpdate(newFilUtenti);
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvFil_Utenti_Detail_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("FIL_UTENTI-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));                        
            int filId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());           
            Utenti_Fil newFil_Utenti = RepoManager.Utenti_FilRepo.Init();
            PowerWebService.FillEntityProperties(newFil_Utenti, e.NewValues);
            RepoManager.Utenti_FilRepo.SetEntityBeforeAddOrUpdate(newFil_Utenti);            
            newFil_Utenti.Fil_Id = filId;
            RepoManager.Utenti_FilRepo.Add(newFil_Utenti, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvFil_Utenti_Detail_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("FIL_UTENTIL-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            ASPxGridView gvDetail = (ASPxGridView)sender;
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Utenti_Fil currentFilUtenti = RepoManager.Utenti_FilRepo.Single(p => p.Utenti_Fil_Id == currentId);
            PowerWebService.FillEntityProperties(currentFilUtenti, e.NewValues);
            RepoManager.Utenti_FilRepo.SetEntityBeforeAddOrUpdate(currentFilUtenti);
            RepoManager.Utenti_FilRepo.SaveChanges();
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvFil_Utenti_Detail_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            _log.Info(String.Format("FIL_UTENTI-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Utenti_Fil currentFilUtenti = RepoManager.Utenti_FilRepo.Single(p => p.Utenti_Fil_Id == currentId);
            RepoManager.Utenti_FilRepo.Delete(currentFilUtenti, true);
            e.Cancel = true;
            BindDetailGrid(gvDetail);
        }

        protected void gvFil_Utenti_Detail_BeforePerformDataSelect(object sender, EventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;

            if (gvDetails != null)
            {
                PowerWebService.FillGridLabels(typeof(Utenti_Fil), gvDetails);
                PowerWebService.FillComboboxes(gvDetails);
                PowerWebService.InitDetailGrid(Page, gvDetails);
                BindDetailGrid(gvDetails);
            }
        }

        protected void gvFil_Utenti_DetailRowExpandedChanged(object sender, ASPxGridViewDetailRowEventArgs e)
        {
            if (!e.Expanded)
                GridView.DataBind();
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)                   
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _filStub.Data_Registrazione_Fil) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _filStub.DataOraUltimaModifica_Fil))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }
    }
}