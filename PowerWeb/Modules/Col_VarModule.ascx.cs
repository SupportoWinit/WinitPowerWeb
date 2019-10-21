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

namespace PowerWeb.Modules
{
    public partial class Col_VarModule : BaseGridModule
    {
        //DEFINIZIONI 
        private Col _colStub = null;
        const String KEYFIELDNAME = "Col_Id";
        const String DETAILKEYFIELDNAME = "Col_Var_Id";

        public override ASPxGridView GridView
        {
            get
            {
                return gvCol_Var;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryCol();
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryCol();
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
            PowerWebService.FillGridLabels(typeof(Col), gvCol_Var);
            PowerWebService.FillComboboxes(gvCol_Var);
            BindGrid();
        }

        private void BindGrid()
        {
            gvCol_Var.KeyFieldName = KEYFIELDNAME;
            gvCol_Var.DataSource = RepoManager.ColRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)
                gvCol_Var.DataBind();
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int colId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Col_VarRepo.Find(cv => cv.Col_Id == colId).OrderByDescending(cv => cv.DataOraUltimaModifica_Col).ToList();
        }

        public override Type EntityType
        {
            get { return typeof(Col); }
        }

        #region gvCol_Var_Detail : Init-BeforePerformDataSelect
        protected void gvCol_Var_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
            {
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
                //PowerWebContext.SetToSession<ASPxGridView>("gvDetail_" + gvCol_Var.ID, gvDetail);
            }
        }

        protected void gvCol_Var_Detail_BeforePerformDataSelect(object sender, EventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                PowerWebService.FillGridLabels(typeof(Col_Var), gvDetails);
                PowerWebService.FillComboboxes(gvDetails);
                PowerWebService.InitDetailGrid(Page, gvDetails);
                BindDetailGrid(gvDetails);

            }
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Data_Registrazione_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.DataOraUltimaModifica_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Assegni_Famigliari_Fine_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Assegni_Famigliari_Inizio_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Data_Disponibilita_Fine_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Data_Disponibilita_Inizio_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Nascita_Data_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Scadenza_Patente_Col) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _colStub.Straniero_Scadenza_Permesso_Col))
                PowerWebService.GridHeaderFilterFillItems(e);
        }
    }
}