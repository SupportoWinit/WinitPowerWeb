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
    public partial class Utenti_FilModule : BaseGridModule, ILogModule
    {
        //DEFINIZIONI
        private Utenti _utentiStub = null;
        const String KEYFIELDNAME = "Utenti_Id";
        const String DETAILKEYFIELDNAME = "Utenti_Fil_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Utenti_FilModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvUtenti_Fil;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryUtenti_Fil();
                    template = new PowerFormTemplate(this, templateDic);
                    template.IsDetail = true;
                    PowerWebContext.SetToSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID), template);
                }
                return template;
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Utenti), gvUtenti_Fil);
            PowerWebService.FillComboboxes(gvUtenti_Fil);
            BindGrid();
        }

        private void BindGrid()
        {
            gvUtenti_Fil.KeyFieldName = KEYFIELDNAME;
            gvUtenti_Fil.DataSource = RepoManager.UtentiRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)
                gvUtenti_Fil.DataBind();
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int UtentiId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Utenti_FilRepo.Find(pc => pc.Utenti_Id == UtentiId).ToList();
        }

        public override Type EntityType
        {
            get { return typeof(Utenti); }
        }

        #region gvUtenti_Fil_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect-DetailRowExpandedChanged
        protected void gvUtenti_Fil_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
        }

        protected void gvUtenti_Fil_Detail_InitNewRow(object sender, DevExpress.Web.Data.ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
            {
                Utenti_Fil newUtentiFil = RepoManager.Utenti_FilRepo.Init();
                PowerWebService.FillGridProperties(newUtentiFil, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, gvDetail, e.NewValues);
            }
        }

        protected void gvUtenti_Fil_Detail_RowValidating(object sender, DevExpress.Web.Data.ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                Utenti_Fil newUtentiFil = RepoManager.Utenti_FilRepo.Init();

                if (IsInBatchMode)
                {
                    var currentId = Convert.ToInt32(e.Keys[gvDetails.KeyFieldName]);
                    if (currentId > 0)
                    {
                        var currentnewUtentiFil = GridView.GetRow(e.VisibleIndex);
                        PowerWebService.FillValues(currentnewUtentiFil, e.NewValues, e.OldValues);
                    }
                }

                int UtentiId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
                newUtentiFil.Utenti_Id = UtentiId;
                PowerWebService.FillEntityProperties(newUtentiFil, e.NewValues);
                PowerWebService.FillEntityKey(newUtentiFil, e.Keys, DETAILKEYFIELDNAME);
                RepoManager.Utenti_FilRepo.SetEntityBeforeAddOrUpdate(newUtentiFil);
                PowerWebService.AddValidationErrors(RepoManager.Utenti_FilRepo.Check(newUtentiFil, e.IsNewRow), e.Errors, gvDetails, typeof(Utenti_FilModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvUtenti_Fil_Detail_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("UTENTI_FIL-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            int utentiId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());
            Utenti_Fil newUtenti_Fil = RepoManager.Utenti_FilRepo.Init();
            PowerWebService.FillEntityProperties(newUtenti_Fil, e.NewValues);
            RepoManager.Utenti_FilRepo.SetEntityBeforeAddOrUpdate(newUtenti_Fil);
            newUtenti_Fil.Utenti_Id = utentiId;
            RepoManager.Utenti_FilRepo.Add(newUtenti_Fil, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvUtenti_Fil_Detail_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("UTENTI_FIL-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            ASPxGridView gvDetail = (ASPxGridView)sender;
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Utenti_Fil currentUtentiFil = RepoManager.Utenti_FilRepo.Single(p => p.Utenti_Fil_Id == currentId);
            PowerWebService.FillEntityProperties(currentUtentiFil, e.NewValues);
            RepoManager.Utenti_FilRepo.SetEntityBeforeAddOrUpdate(currentUtentiFil);
            RepoManager.Utenti_FilRepo.SaveChanges();
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvUtenti_Fil_Detail_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            _log.Info(String.Format("UTENTI_FIL-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Utenti_Fil currentUtentiFil = RepoManager.Utenti_FilRepo.Single(p => p.Utenti_Fil_Id == currentId);
            RepoManager.Utenti_FilRepo.Delete(currentUtentiFil, true);
            e.Cancel = true;
            BindDetailGrid(gvDetail);
        }

        protected void gvUtenti_Fil_Detail_BeforePerformDataSelect(object sender, EventArgs e)
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

        protected void gvUtenti_Fil_DetailRowExpandedChanged(object sender, ASPxGridViewDetailRowEventArgs e)
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