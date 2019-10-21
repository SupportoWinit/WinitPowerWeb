using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Domain;
using Business.Repository;
using DevExpress.Web.ASPxGridView;
using Common;
using Business;
using DevExpress.Web.Data;

namespace PowerWeb.Modules
{
    public partial class Cant_VarModule : BaseGridModule
    {
        //DEFINIZIONI      
        private Cant _cantStub = null;
        const String KEYFIELDNAME = "Cant_Id";
        const String DETAILKEYFIELDNAME = "Cant_Var_Id";


        public override ASPxGridView GridView
        {
            get
            {
                return gvCant_Var;
            }
        }

        public override ASPxGridView GridViewDetail
        {
            get
            {
                return PowerWebContext.GetFromSession<ASPxGridView>("gridviewdetail_" + GridView.ID);
            }
        }

        public override PowerFormTemplate EditFormTemplate
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID);
                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryCant();
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryCant();
                    template = new PowerFormTemplate(this, templateDic);
                    template.IsDetail = true;
                    PowerWebContext.SetToSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID), template);
                }
                return template;
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            if (!Page.IsCallback && !Page.IsPostBack)
                ResetSession();
            PowerWebService.FillGridLabels(typeof(Cant), gvCant_Var);
            PowerWebService.FillComboboxes(gvCant_Var);
            BindGrid();
        }

        private void BindGrid()
        {
            gvCant_Var.KeyFieldName = KEYFIELDNAME;
            gvCant_Var.DataSource = RepoManager.CantRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)
                gvCant_Var.DataBind();
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int canId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Cant_VarRepo.Find(cv => cv.Cant_Id == canId).OrderByDescending(cv => cv.DataOraUltimaModifica_Can).ToList();
        }

        public override Type EntityType
        {
            get { return typeof(Cant); }
        }

        #region gvCant_Var_Detail : Init-BeforePerformDataSelect
        protected void gvCant_Var_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
            {
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
            }
        }

        protected void gvCant_Var_Detail_BeforePerformDataSelect(object sender, EventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                PowerWebService.FillGridLabels(typeof(Cant_Var), gvDetails);
                PowerWebService.FillComboboxes(gvDetails);
                PowerWebService.InitDetailGrid(Page, gvDetails);
                BindDetailGrid(gvDetails);
            }
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.Data_Registrazione_Can) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.DataOraUltimaModifica_Can) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.Data_Isee_Can) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.Data_Nascita_Can) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.Data_Rapporto_Fine_1_Can) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cantStub.Data_Rapporto_Inizio_1_Can))
                PowerWebService.GridHeaderFilterFillItems(e);
        }
        protected void gvCant_Var_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            var currentId = Convert.ToInt32(e.Keys[gvCant_Var.KeyFieldName]);
            Cant_Var currentCantVar = RepoManager.Cant_VarRepo.Single(u => u.Cant_Var_Id == currentId);
            RepoManager.Cant_VarRepo.Delete(currentCantVar, true);
            e.Cancel = true;
        }
    }
}