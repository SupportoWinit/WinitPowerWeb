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
using System.Data.SqlClient;

namespace PowerWeb.Modules
{
    public partial class Pru_ColModule : BaseGridModule, IPrintModule, ILogModule
    {
        //DEFINIZIONI
        private Pru _pruStub = null;
        const String KEYFIELDNAME = "Pru_Id";
        const String DETAILKEYFIELDNAME = "Pru_Col_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Pru_ColModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvPru_Col;
            }
        }

        /// <summary>
        /// Recupera il template della form utilizzata per il recupero dei dati di raggruppamento in stampa;
        /// se impostato a null si utilizza il valore specificato nel modulo.
        /// </summary>
        /// <value>
        /// il template della form utilizzata per il recupero dei dati di raggruppamento in stampa;
        /// se impostato a null si utilizza il valore specificato nel modulo.
        /// </value>
        public PowerFormTemplate PrintFormTemplate
        {
            get
            {
                return null;
            }
        }

        public bool IsInBatchMode
        {
            get { return GridView.SettingsEditing.Mode == GridViewEditingMode.Batch; }
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryPru();
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryPru_Col();
                    template = new PowerFormTemplate(this, templateDic);
                    template.IsDetail = true;
                    PowerWebContext.SetToSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID), template);
                }
                return template;
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Pru), gvPru_Col);
            PowerWebService.FillComboboxes(gvPru_Col);
            BindGrid();
        }

        private void BindGrid()
        {

            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvPru_Col.KeyFieldName = KEYFIELDNAME;
            IQueryable<Pru> currDataSource = Enumerable.Empty<Pru>().AsQueryable();
            var emptyList = Enumerable.Empty<Pru>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.PruRepo.GetAll(true).AsQueryable();
                gvPru_Col.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvPru_Col.DataSource = emptyList;
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int pruId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Pru_ColRepo.Find(pc => pc.Pru_Id == pruId).OrderByDescending(pc => pc.Abilitazione_Data_Inizio_Pru_Col).ToList();
        }

        protected void gvPru_Col_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }
        public override Type EntityType
        {
            get { return typeof(Pru); }
        }

        #region gvPru_Col_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect-DetailRowExpandedChanged
        protected void gvPru_Col_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
        }

        protected void gvPru_Col_Detail_InitNewRow(object sender, DevExpress.Web.Data.ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
            {
                Pru_Col newPruCol = RepoManager.Pru_ColRepo.Init();
                PowerWebService.FillGridProperties(newPruCol, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, gvDetail, e.NewValues);
            }
        }

        protected void gvPru_Col_Detail_RowValidating(object sender, DevExpress.Web.Data.ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                Pru_Col newPruCol = RepoManager.Pru_ColRepo.Init();
                if (IsInBatchMode)
                {
                    var currentId = Convert.ToInt32(e.Keys[gvDetails.KeyFieldName]);
                    if (currentId > 0)
                    {
                        var currentPruCol = GridView.GetRow(e.VisibleIndex);
                        PowerWebService.FillValues(currentPruCol, e.NewValues, e.OldValues);
                    }
                }

                int pruId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
                newPruCol.Pru_Id = pruId;
                PowerWebService.FillEntityProperties(newPruCol, e.NewValues);
                PowerWebService.FillEntityKey(newPruCol, e.Keys, DETAILKEYFIELDNAME);
                RepoManager.Pru_ColRepo.SetEntityBeforeAddOrUpdate(newPruCol);
                PowerWebService.AddValidationErrors(RepoManager.Pru_ColRepo.Check(newPruCol, e.IsNewRow), e.Errors, gvDetails, typeof(Pru_ColModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvPru_Col_Detail_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("PRU_COL-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Pru_Col currentPruCol = RepoManager.Pru_ColRepo.Single(f => f.Pru_Col_Id == currentId);
            Pru_Col oldPruCol = RepoManager.Pru_ColRepo.DbSet.AsNoTracking().FirstOrDefault(f => f.Pru_Col_Id == currentId);
            PowerWebService.FillEntityProperties(currentPruCol, e.NewValues);
            RepoManager.Pru_ColRepo.SetEntityBeforeAddOrUpdate(currentPruCol);
            RepoManager.Pru_ColRepo.Update(currentPruCol, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
            //syncColBadgeAssignemntInAppDB(currentPruCol, 2, oldPruCol);
        }

        protected void gvPru_Col_Detail_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("PRU_COL-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            int pruId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());            
            Pru_Col newPruCol = RepoManager.Pru_ColRepo.Init();
            PowerWebService.FillEntityProperties(newPruCol, e.NewValues);
            RepoManager.Pru_ColRepo.SetEntityBeforeAddOrUpdate(newPruCol);
            newPruCol.Pru_Id = pruId;            
            RepoManager.Pru_ColRepo.Add(newPruCol, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
            //syncColBadgeAssignemntInAppDB(newPruCol, 0, null);
        }

        protected void gvPru_Col_Detail_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            _log.Info(String.Format("PRU_COL-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Pru_Col currentPruCol = RepoManager.Pru_ColRepo.Single(p => p.Pru_Col_Id == currentId);
            if(currentPruCol.Abilitazione_Data_Inizio_Pru_Col > RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg)
                RepoManager.Pru_ColRepo.Delete(currentPruCol, true);
            e.Cancel = true;
            BindDetailGrid(gvDetail);
            //syncColBadgeAssignemntInAppDB(currentPruCol, 1, null);
        }

        

        protected void gvPru_Col_Detail_BeforePerformDataSelect(object sender, EventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                PowerWebService.FillGridLabels(typeof(Pru_Col), gvDetails);
                PowerWebService.FillComboboxes(gvDetails);
                PowerWebService.InitDetailGrid(Page, gvDetails);
                BindDetailGrid(gvDetails);
            }
        }

        protected void gvPru_Col_DetailRowExpandedChanged(object sender, ASPxGridViewDetailRowEventArgs e)
        {
            if (!e.Expanded)
                GridView.DataBind();
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _pruStub.Data_Registrazione_Pru) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _pruStub.DataOraUltimaModifica_Pru))              
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Pru> prus = CommonService.ConvertTo<Pru>(items);
            XRPru_Col pruColsReport = new XRPru_Col(prus);
            return new ExtXtraReport { Report = pruColsReport, PictureBox = pruColsReport.CompanyLogo };
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }

       
    }
}