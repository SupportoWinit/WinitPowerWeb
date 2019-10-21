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
    public partial class Utenti_RespModule : BaseGridModule, ILogModule
    {
        //DEFINIZIONI
        private Utenti _utentiStub = null;
        const String KEYFIELDNAME = "Utenti_Id";
        const String DETAILKEYFIELDNAME = "Utenti_Resp_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Utenti_RespModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvUtenti_Resp;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryUtenti_Resp();
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryUtenti_Resp();
                    template = new PowerFormTemplate(this, templateDic);
                    template.IsDetail = true;
                    PowerWebContext.SetToSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID), template);
                }
                return template;
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Utenti), gvUtenti_Resp);
            PowerWebService.FillComboboxes(gvUtenti_Resp);
            BindGrid();
        }

        private void BindGrid()
        {
            gvUtenti_Resp.KeyFieldName = KEYFIELDNAME;
            gvUtenti_Resp.DataSource = RepoManager.UtentiRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)
                gvUtenti_Resp.DataBind();
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int UtentiId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Utenti_RespRepo.Find(pc => pc.Utenti_Id == UtentiId).ToList();
        }

        public override Type EntityType
        {
            get { return typeof(Utenti); }
        }

        #region gvUtenti_Resp_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect-DetailRowExpandedChanged
        protected void gvUtenti_Resp_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
        }

        protected void gvUtenti_Resp_Detail_InitNewRow(object sender, DevExpress.Web.Data.ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
            {
                Utenti_Resp newUtentiResp = RepoManager.Utenti_RespRepo.Init();
                PowerWebService.FillGridProperties(newUtentiResp, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, gvDetail, e.NewValues);
            }
        }

        protected void gvUtenti_Resp_Detail_RowValidating(object sender, DevExpress.Web.Data.ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                Utenti_Resp newUtentiResp = RepoManager.Utenti_RespRepo.Init();

                if (IsInBatchMode)
                {
                    var currentId = Convert.ToInt32(e.Keys[gvDetails.KeyFieldName]);
                    if (currentId > 0)
                    {
                        var currentnewUtentiResp = GridView.GetRow(e.VisibleIndex);
                        PowerWebService.FillValues(currentnewUtentiResp, e.NewValues, e.OldValues);
                    }
                }

                int UtentiId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
                newUtentiResp.Utenti_Id = UtentiId;
                PowerWebService.FillEntityProperties(newUtentiResp, e.NewValues);
                PowerWebService.FillEntityKey(newUtentiResp, e.Keys, DETAILKEYFIELDNAME);
                RepoManager.Utenti_RespRepo.SetEntityBeforeAddOrUpdate(newUtentiResp);
                PowerWebService.AddValidationErrors(RepoManager.Utenti_RespRepo.Check(newUtentiResp, e.IsNewRow), e.Errors, gvDetails, typeof(Utenti_RespModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvUtenti_Resp_Detail_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("UTENTI_RESP-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            int UtentiId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());
            Utenti_Resp newUtentiResp = RepoManager.Utenti_RespRepo.Init();
            PowerWebService.FillEntityProperties(newUtentiResp, e.NewValues);
            RepoManager.Utenti_RespRepo.SetEntityBeforeAddOrUpdate(newUtentiResp);
            newUtentiResp.Utenti_Id = UtentiId;
            RepoManager.Utenti_RespRepo.Add(newUtentiResp, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvUtenti_Resp_Detail_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("UTENTI_RESP-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
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

        protected void gvUtenti_Resp_Detail_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            _log.Info(String.Format("UTENTI_RESP-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Utenti_Resp currentUtentiResp = RepoManager.Utenti_RespRepo.Single(p => p.Utenti_Resp_Id == currentId);
            RepoManager.Utenti_RespRepo.Delete(currentUtentiResp, true);
            e.Cancel = true;
            BindDetailGrid(gvDetail);
        }

        protected void gvUtenti_Resp_Detail_BeforePerformDataSelect(object sender, EventArgs e)
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

        protected void gvUtenti_Resp_DetailRowExpandedChanged(object sender, ASPxGridViewDetailRowEventArgs e)
        {
            if (!e.Expanded)
                GridView.DataBind();
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _utentiStub.Data_Registrazione_Utente) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _utentiStub.DataOraUltimaModifica_Utente))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }
    }
}