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
using Business.Repository.Custom;
using Reports;
using log4net;
using System.Data.SqlClient;

namespace PowerWeb.Modules
{
    public partial class Col_PruModule : BaseGridModule, IPrintModule, ILogModule
    {
        //DEFINIZIONI    
        private Col _colStub = null;
        private Pru_Col _pruColStub = null;
        const String KEYFIELDNAME = "Col_Id";
        const String DETAILKEYFIELDNAME = "Pru_Col_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Col_PruModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvCol_Pru;
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

        public override IList<string> ForceWritableCombo
        {
            get { return new List<string>() { CommonService.GetPropertyName(() => _pruColStub.N_Serie_Pru) }; }
        }

        public override ASPxGridView GridViewDetail
        {
            get
            {
                return gvCol_Pru.FindDetailRowTemplateControl(0, "gvCol_Pru_Detail") as ASPxGridView;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryCol_Pru();
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
            PowerWebService.FillGridLabels(typeof(Col), gvCol_Pru);
            PowerWebService.FillComboboxes(gvCol_Pru);
            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvCol_Pru.KeyFieldName = KEYFIELDNAME;
            IQueryable<Col> currDataSource = Enumerable.Empty<Col>().AsQueryable();
            var emptyList = Enumerable.Empty<Col>();
            var colList = new List<Col>();

            if (IsToPopulateGrid)
            {
                if (PowerWebContext.Current.User.Resp_Inclusive)
                {
                    //viene estratto l'ide dello user che ha fatto l'accesso a Powerweb
                    int userId = PowerWebContext.Current.User.Utenti_Id;

                    //filtro solo i responsabili che corrispondo all'utente che ha effettuato l'accesso 
                    var userRespIds = RepoManager.Utenti_RespRepo.Find(r => r.Utenti_Id == userId).ToList();

                    foreach (var item in userRespIds)
                    {
                        var col = RepoManager.ColRepo.GetAllQueryable().Where(c => c.Resp_Id == item.Resp_Id);

                        colList.AddRange(col);
                    }

                    currDataSource = colList.AsQueryable();

                }
                else
                {
                    currDataSource = RepoManager.ColRepo.GetAll(true).AsQueryable();
                }

                gvCol_Pru.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvCol_Pru.DataSource = emptyList;
        }

        protected void gvCol_Pru_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int colId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Pru_ColRepo.Find(pc => pc.Col_Id == colId).OrderByDescending(pc => pc.Abilitazione_Data_Inizio_Pru_Col).ToList();
        }

        public override Type EntityType
        {
            get { return typeof(Col); }
        }

        #region gvCol_Pru_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect-DetailRowExpandedChanged
        protected void gvCol_Pru_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
                     

        }

        protected void gvCol_Pru_Detail_InitNewRow(object sender, DevExpress.Web.Data.ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
            {
                Pru_Col newPruCol = RepoManager.Pru_ColRepo.Init();
                PowerWebService.FillGridProperties(newPruCol, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, gvDetail, e.NewValues);
            }
        }

        protected void gvCol_Pru_Detail_RowValidating(object sender, DevExpress.Web.Data.ASPxDataValidationEventArgs e)
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

                int colId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
                newPruCol.Col_Id = colId;
                PowerWebService.FillEntityProperties(newPruCol, e.NewValues);
                PowerWebService.FillEntityKey(newPruCol, e.Keys, DETAILKEYFIELDNAME);
                RepoManager.Pru_ColRepo.SetEntityBeforeAddOrUpdate(newPruCol);
                PowerWebService.AddValidationErrors(RepoManager.Pru_ColRepo.Check(newPruCol, e.IsNewRow), e.Errors, gvDetails, typeof(Pru_ColModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvCol_Pru_Detail_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("COL_PRU-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Pru_Col currentPruCol = RepoManager.Pru_ColRepo.Single(f => f.Pru_Col_Id == currentId);
            Pru_Col oldPruCol = RepoManager.Pru_ColRepo.DbSet.AsNoTracking().FirstOrDefault(f => f.Pru_Col_Id == currentId);
            PowerWebService.FillEntityProperties(currentPruCol, e.NewValues);
            RepoManager.Pru_ColRepo.SetEntityBeforeAddOrUpdate(currentPruCol);
            RepoManager.Pru_ColRepo.Update(currentPruCol, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }
        
        protected void gvCol_Pru_Detail_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("COL_PRU-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            int colId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());           
            Pru_Col newPruCol = RepoManager.Pru_ColRepo.Init();
            PowerWebService.FillEntityProperties(newPruCol, e.NewValues);
            RepoManager.Pru_ColRepo.SetEntityBeforeAddOrUpdate(newPruCol);
            newPruCol.Col_Id = colId;         
            RepoManager.Pru_ColRepo.Add(newPruCol, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvCol_Pru_Detail_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("COL_PRU-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Pru_Col currentPruCol = RepoManager.Pru_ColRepo.Single(p => p.Pru_Col_Id == currentId);
    
            //non posso eliminare l'associazione se sono pima della data blocco
            if (currentPruCol.Abilitazione_Data_Inizio_Pru_Col > RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg)
                RepoManager.Pru_ColRepo.Delete(currentPruCol, true);

            e.Cancel = true;
            BindDetailGrid(gvDetail);
        }
        

        protected void gvCol_Pru_Detail_BeforePerformDataSelect(object sender, EventArgs e)
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

        protected void gvCol_Pru_DetailRowExpandedChanged(object sender, ASPxGridViewDetailRowEventArgs e)
        {
            if (!e.Expanded)
                GridView.DataBind();
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

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Col> cols = CommonService.ConvertTo<Col>(items);
            XRCol_Pru colPrusReport = new XRCol_Pru(cols);
            return new ExtXtraReport { Report = colPrusReport, PictureBox = colPrusReport.CompanyLogo };
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }
        
    }
}