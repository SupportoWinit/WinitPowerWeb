using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using Domain;
using DevExpress.Web.Data;
using log4net;
using Business;
using System;
using Common;
using DevExpress.Web.ASPxGridView;
using Reports;
using DevExpress.Web.ASPxEditors;
using DevExpress.Web.ASPxClasses;


namespace PowerWeb.Modules
{
    public partial class Tab_DistModule : BaseGridModule, ILogModule
    {
        private Tab_Dist _tabDistStub = null;
        private Tab_Decod _tabDecodStub = null;
        const String KEYFIELDNAME = "Tab_Decod_Id";
        const String DETAILKEYFIELDNAME = "Tab_Dist_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Tab_DistModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvTabDecod;
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
            get { return null; }
        }

        public override PowerFormTemplate EditDetailFormTemplate
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID));
                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryTab_Dist();
                    template = new PowerFormTemplate(this, templateDic);
                    template.IsDetail = true;
                    PowerWebContext.SetToSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID), template);
                }
                return template;
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Tab_Decod), GridView);
            PowerWebService.FillComboboxes(gvTabDecod);
            BindGrid();
        }

        private void BindGrid()
        {
            gvTabDecod.KeyFieldName = KEYFIELDNAME;
            gvTabDecod.DataSource = RepoManager.Tab_DecodRepo.Find(td => td.Nome_Tab == "TIPO_DISTANZA", true);
            if (!Page.IsPostBack && !Page.IsCallback)
                gvTabDecod.DataBind();
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int tabDecodId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Tab_DistRepo.Find(td => td.Tab_Decod_Id == tabDecodId).ToList();
        }

        public override Type EntityType
        {
            get { return typeof(Tab_Dist); }
        }

        #region gvTabDist : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting
        protected void gvTabDist_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
        }
        protected void gvTabDist_Detail_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
            {
                Tab_Dist initTab_Dist = RepoManager.Tab_DistRepo.Init();
                PowerWebService.FillGridProperties(initTab_Dist, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, gvDetail, e.NewValues);
            }
        }

        protected void gvTabDist_Detail_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                Tab_Dist newTab_Dist = RepoManager.Tab_DistRepo.Init();

                if (IsInBatchMode)
                {
                    var currentId = Convert.ToInt32(e.Keys[gvDetails.KeyFieldName]);
                    if (currentId > 0)
                    {
                        var currentTab_Dist = GridView.GetRow(e.VisibleIndex);
                        PowerWebService.FillValues(currentTab_Dist, e.NewValues, e.OldValues);
                    }
                }

                PowerWebService.FillEntityProperties(newTab_Dist, e.NewValues);
                PowerWebService.FillEntityKey(newTab_Dist, e.Keys, DETAILKEYFIELDNAME);
                RepoManager.Tab_DistRepo.SetEntityBeforeAddOrUpdate(newTab_Dist);

                newTab_Dist.Tab_Decod_Id = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());

                PowerWebService.AddValidationErrors(RepoManager.Tab_DistRepo.Check(newTab_Dist, e.IsNewRow), e.Errors, gvDetails, typeof(Tab_DistModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvTabDist_Detail_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("TAB_DIST-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Tab_Dist newTabDist = RepoManager.Tab_DistRepo.Init();
            newTabDist.Tab_Decod_Id = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());
            PowerWebService.FillEntityProperties(newTabDist, e.NewValues);
            RepoManager.Tab_DistRepo.SetEntityBeforeAddOrUpdate(newTabDist);
            RepoManager.Tab_DistRepo.Add(newTabDist, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvTabDist_Detail_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("TAB_DIST-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            ASPxGridView gvDetail = (ASPxGridView)sender;
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Tab_Dist currentTab_Dist = RepoManager.Tab_DistRepo.Single(ta => ta.Tab_Dist_Id == currentId);
            PowerWebService.FillEntityProperties(currentTab_Dist, e.NewValues);
            RepoManager.Tab_DistRepo.SetEntityBeforeAddOrUpdate(currentTab_Dist);
            RepoManager.Tab_DistRepo.SaveChanges();
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvTabDist_Detail_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("TAB_DIST-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Tab_Dist currentTabDist = RepoManager.Tab_DistRepo.Single(p => p.Tab_Dist_Id == currentId);
            RepoManager.Tab_DistRepo.Delete(currentTabDist, true);
            e.Cancel = true;
            BindDetailGrid(gvDetail);
        }

        protected void gvTabDist_Detail_BeforePerformDataSelect(object sender, EventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;

            if (gvDetails != null)
            {
                PowerWebService.FillGridLabels(typeof(Tab_Dist), gvDetails);

                var masterValue = gvDetails.GetMasterRowFieldValues(CommonService.GetPropertyName(() => _tabDecodStub.Chiave_Tab)) as String;

                if (!String.IsNullOrEmpty(masterValue))
                {
                    var arrCol = gvDetails.Columns[CommonService.GetPropertyName(() => _tabDistStub.Arrivo_Tab_Dist)] as GridViewDataComboBoxColumn;
                    var parCol = gvDetails.Columns[CommonService.GetPropertyName(() => _tabDistStub.Partenza_Tab_Dist)] as GridViewDataComboBoxColumn;

                    if (arrCol != null && parCol != null)
                    {
                        if (masterValue.ToUpper() == "C")
                            arrCol.FieldName = parCol.FieldName = "Cant_Id";
                        else if (masterValue.ToUpper() == "K")
                            arrCol.FieldName = parCol.FieldName = "Cap_Can";
                        else if (masterValue.ToUpper() == "P")
                            arrCol.FieldName = parCol.FieldName = "Luogo_Can";
                        else if (masterValue.ToUpper() == "Z")
                            arrCol.FieldName = parCol.FieldName = "Zona_Can";
                        else if (masterValue.ToUpper() == "G")
                        {
                            arrCol.FieldName = "Tab_Dist_Gis_Arr";
                            parCol.FieldName = "Tab_Dist_Gis_Par";}

                        PowerWebService.FillComboboxes(gvDetails);
                        arrCol.FieldName = CommonService.GetPropertyName(() => _tabDistStub.Arrivo_Tab_Dist);
                        parCol.FieldName = CommonService.GetPropertyName(() => _tabDistStub.Partenza_Tab_Dist);

                        PowerWebService.InitDetailGrid(Page, gvDetails);
                        BindDetailGrid(gvDetails);
                    }
                }
            }
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

        public override void GridView_CellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            base.GridView_CellEditorInitialize(sender, e);

            if (!GridView.IsEditing || e.Column.FieldName != CommonService.GetPropertyName(() => _tabDistStub.Arrivo_Tab_Dist)) return;
            var cmbArr = e.Editor as ASPxComboBox;
            if (cmbArr != null)
                cmbArr.Callback += new CallbackEventHandlerBase(cmbArr_Callback);
            object val = GridView.GetRowValuesByKeyValue(e.KeyValue, CommonService.GetPropertyName(() => _tabDistStub.DistType));
            if (val == DBNull.Value) return;

        }

        void cmbArr_Callback(object sender, DevExpress.Web.ASPxClasses.CallbackEventArgsBase e)
        {
            var cmbArr = sender as ASPxComboBox;
            if (e.Parameter.ToUpper() == "C")
            {
                cmbArr.ID = "Cant_Id";
                PowerWebService.FillComboboxes(cmbArr);
            }
            else if (e.Parameter.ToUpper() == "K")
            {
                cmbArr.ID = "Cap_Can";
                PowerWebService.FillComboboxes(cmbArr);
            }
            else if (e.Parameter.ToUpper() == "Z")
            {
                cmbArr.ID = "Zona_Can";
                PowerWebService.FillComboboxes(cmbArr);
            }
            else if (e.Parameter.ToUpper() == "P")
            {
                cmbArr.ID = "Luogo_Can";
                PowerWebService.FillComboboxes(cmbArr);
            }
            else if (e.Parameter.ToUpper() == "G")
            {
                cmbArr.ID = "Tab_Dist_Gis_Arr";
                PowerWebService.FillComboboxes(cmbArr);
            }
        }

        //void cmbArr_ItemRequestedByValue(object source, ListEditItemRequestedByValueEventArgs e)
        //{
        //    ASPxComboBox comboBox = (ASPxComboBox)source;
        //    comboBox.DataSource = RepoManager.Tab_ComuniRepo.Find(tc => tc.Cap_Tab_Comuni.Contains(e.Value.ToString()), true);
        //    comboBox.ValueField = CommonService.GetPropertyName(() => _tabComuniStub.Cap_Tab_Comuni);
        //    comboBox.TextField = CommonService.GetPropertyName(() => _tabComuniStub.Cap_Tab_Comuni);
        //    comboBox.DataBindItems();
        //}

        //void cmbArr_ItemsRequestedByFilterCondition(object source, ListEditItemsRequestedByFilterConditionEventArgs e)
        //{
        //    ASPxComboBox comboBox = (ASPxComboBox)source;
        //    comboBox.DataSource = RepoManager.Tab_ComuniRepo.Find(tc => tc.Cap_Tab_Comuni.Contains(e.Filter), true).Skip(e.BeginIndex).Take(e.EndIndex - e.BeginIndex + 1);
        //    comboBox.ValueField = CommonService.GetPropertyName(() => _tabComuniStub.Cap_Tab_Comuni);
        //    comboBox.TextField = CommonService.GetPropertyName(() => _tabComuniStub.Cap_Tab_Comuni);
        //    comboBox.DataBindItems();
        //}
    }
}