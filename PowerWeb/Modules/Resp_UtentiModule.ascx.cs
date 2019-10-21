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
    public partial class Resp_UtentiModule : BaseGridModule, ILogModule
    {
        //DEFINIZIONI
        private Resp _respStub = null;        
        const String KEYFIELDNAME = "Resp_Id";
        const String DETAILKEYFIELDNAME = "Utenti_Resp_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Utenti_RespModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvResp_Utenti;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryResp_Utenti();
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryResp_Utenti();
                    template = new PowerFormTemplate(this, templateDic);
                    template.IsDetail = true;
                    PowerWebContext.SetToSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID), template);
                }
                return template;
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Resp), gvResp_Utenti);
            PowerWebService.FillComboboxes(gvResp_Utenti);
            BindGrid();
        }

        private void BindGrid()
        {
            gvResp_Utenti.KeyFieldName = KEYFIELDNAME;
            gvResp_Utenti.DataSource = RepoManager.RespRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)
                gvResp_Utenti.DataBind();
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int RespId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Utenti_RespRepo.Find(pc => pc.Resp_Id  == RespId).ToList();
        }

        public override Type EntityType
        {
            get { return typeof(Resp); }
        }

        #region gvResp_Utenti_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect-DetailRowExpandedChanged
        protected void gvResp_Utenti_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
        }

        protected void gvResp_Utenti_Detail_InitNewRow(object sender, DevExpress.Web.Data.ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
            {
                Utenti_Resp newRespUtenti = RepoManager.Utenti_RespRepo.Init();
                PowerWebService.FillGridProperties(newRespUtenti, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, gvDetail, e.NewValues);
            }
        }

        protected void gvResp_Utenti_Detail_RowValidating(object sender, DevExpress.Web.Data.ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
              
                Utenti_Resp newRespUtenti = RepoManager.Utenti_RespRepo.Init();

                if (IsInBatchMode)
                {
                    var currentId = Convert.ToInt32(e.Keys[gvDetails.KeyFieldName]);
                    if (currentId > 0)
                    {
                        var currentRespUtenti = GridView.GetRow(e.VisibleIndex);
                        PowerWebService.FillValues(currentRespUtenti, e.NewValues, e.OldValues);
                    }
                }

                int RespId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
                newRespUtenti.Resp_Id  = RespId;
                PowerWebService.FillEntityProperties(newRespUtenti, e.NewValues);
                PowerWebService.FillEntityKey(newRespUtenti, e.Keys, DETAILKEYFIELDNAME);
                RepoManager.Utenti_RespRepo.SetEntityBeforeAddOrUpdate(newRespUtenti);
                PowerWebService.AddValidationErrors(RepoManager.Utenti_RespRepo.Check(newRespUtenti, e.IsNewRow), e.Errors, gvDetails, typeof(Utenti_RespModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvResp_Utenti_Detail_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("RESP_UTENTI-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            int RespId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());            
            Utenti_Resp newUtentiResp = RepoManager.Utenti_RespRepo.Init();
            PowerWebService.FillEntityProperties(newUtentiResp, e.NewValues);
            RepoManager.Utenti_RespRepo.SetEntityBeforeAddOrUpdate(newUtentiResp);
            newUtentiResp.Resp_Id = RespId;            
            RepoManager.Utenti_RespRepo.Add(newUtentiResp, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvResp_Utenti_Detail_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("RESP_UTENTI-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            ASPxGridView gvDetail = (ASPxGridView)sender;
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Utenti_Resp currentUtentiResp = RepoManager.Utenti_RespRepo.Single(p => p.Utenti_Resp_Id == currentId);
            PowerWebService.FillEntityProperties(currentUtentiResp, e.NewValues);
            RepoManager.Utenti_RespRepo.SetEntityBeforeAddOrUpdate(currentUtentiResp);
            RepoManager.Utenti_RespRepo.SaveChanges();
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvResp_Utenti_Detail_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            _log.Info(String.Format("RESP_UTENTI-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Utenti_Resp currentUtentiResp = RepoManager.Utenti_RespRepo.Single(p => p.Utenti_Resp_Id == currentId);
            RepoManager.Utenti_RespRepo.Delete(currentUtentiResp, true);
            e.Cancel = true;
            BindDetailGrid(gvDetail);
        }

        protected void gvResp_Utenti_Detail_BeforePerformDataSelect(object sender, EventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;

            if (gvDetails != null)
            {
                PowerWebService.FillGridLabels(typeof(Utenti_Resp), gvDetails);
                PowerWebService.FillComboboxes(gvDetails);
                PowerWebService.InitDetailGrid(Page, gvDetails);
                BindDetailGrid(gvDetails);
            }
        }

        protected void gvResp_Utenti_DetailRowExpandedChanged(object sender, ASPxGridViewDetailRowEventArgs e)
        {
            if (!e.Expanded)
                GridView.DataBind();
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _respStub.Data_Registrazione_Resp) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _respStub.DataOraUltimaModifica_Resp))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }
    }
}