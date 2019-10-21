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
using DevExpress.Web.ASPxPanel;
using Reports;
using log4net;

namespace PowerWeb.Modules
{
    public partial class Fru_CantModule : BaseGridModule, IPrintModule, ILogModule
    {
        //DEFINIZIONI
        private Fru _fruStub = null;
        const String KEYFIELDNAME = "Fru_Id";
        const String DETAILKEYFIELDNAME = "Fru_Cant_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Fru_CantModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvFru_Cant;
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

        public override PowerFormTemplate EditFormTemplate
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID);
                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryFru();
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryFru_Cant();
                    template = new PowerFormTemplate(this, templateDic);
                    template.IsDetail = true;
                    PowerWebContext.SetToSession<PowerFormTemplate>(String.Format("PowerFormTemplate_{0}_Detail", GridView.ID), template);
                }
                return template;
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Fru), gvFru_Cant);
            PowerWebService.FillComboboxes(gvFru_Cant);
            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvFru_Cant.KeyFieldName = KEYFIELDNAME;
            IQueryable<Fru> currDataSource = Enumerable.Empty<Fru>().AsQueryable();
            var emptyList = Enumerable.Empty<Fru>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.FruRepo.GetAll(true).AsQueryable();
                gvFru_Cant.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvFru_Cant.DataSource = emptyList;
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int fruId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Fru_CantRepo.Find(fc => fc.Fru_Id == fruId).OrderByDescending(pc => pc.Abilitazione_Data_Inizio_Fru_Can).ToList();
        }

        protected void gvFru_Cant_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }
        public override Type EntityType
        {
            get { return typeof(Fru); }
        }

        #region gvFru_Cant_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect-DetailRowExpandedChanged
        protected void gvFru_Cant_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
        }

        protected void gvFru_Cant_Detail_InitNewRow(object sender, DevExpress.Web.Data.ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView gvDetail = sender as ASPxGridView;
            if (gvDetail != null)
            {
                Fru_Cant newFruCant = RepoManager.Fru_CantRepo.Init();
                PowerWebService.FillGridProperties(newFruCant, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, gvDetail, e.NewValues);
            }
        }

        protected void gvFru_Cant_Detail_RowValidating(object sender, DevExpress.Web.Data.ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvDetail = sender as ASPxGridView;
            if (gvDetail != null)
            {
                Fru_Cant newFruCant = RepoManager.Fru_CantRepo.Init();

                if (IsInBatchMode)
                {
                    var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
                    if (currentId > 0)
                    {
                        var currentFruCant = GridView.GetRow(e.VisibleIndex);
                        PowerWebService.FillValues(currentFruCant, e.NewValues, e.OldValues);
                    }
                }

                int fruId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());
                newFruCant.Fru_Id = fruId;
                PowerWebService.FillEntityProperties(newFruCant, e.NewValues);
                PowerWebService.FillEntityKey(newFruCant, e.Keys, DETAILKEYFIELDNAME);
                RepoManager.Fru_CantRepo.SetEntityBeforeAddOrUpdate(newFruCant);
                PowerWebService.AddValidationErrors(RepoManager.Fru_CantRepo.Check(newFruCant, e.IsNewRow), e.Errors, gvDetail, typeof(Fru_CantModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvFru_Cant_Detail_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("FRU_CANT-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            int fruId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());            
            Fru_Cant newFruCant = RepoManager.Fru_CantRepo.Init();
            PowerWebService.FillEntityProperties(newFruCant, e.NewValues);
            RepoManager.Fru_CantRepo.SetEntityBeforeAddOrUpdate(newFruCant);            
            newFruCant.Fru_Id = fruId;
            RepoManager.Fru_CantRepo.Add(newFruCant, true);

            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvFru_Cant_Detail_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("FRU_CANT-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Fru_Cant currentFruCant = RepoManager.Fru_CantRepo.Single(f => f.Fru_Cant_Id == currentId);
            PowerWebService.FillEntityProperties(currentFruCant, e.NewValues);
            RepoManager.Fru_CantRepo.SetEntityBeforeAddOrUpdate(currentFruCant);
            RepoManager.Fru_CantRepo.Update(currentFruCant, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvFru_Cant_Detail_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("FRU_CANT-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Fru_Cant currentFruCant = RepoManager.Fru_CantRepo.Single(f => f.Fru_Cant_Id == currentId);
            if (currentFruCant.Abilitazione_Data_Inizio_Fru_Can > RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg)
                RepoManager.Fru_CantRepo.Delete(currentFruCant, true);

            e.Cancel = true;
            BindDetailGrid(gvDetail);
        }

        protected void gvFru_Cant_Detail_BeforePerformDataSelect(object sender, EventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                PowerWebService.FillGridLabels(typeof(Fru_Cant), gvDetails);
                PowerWebService.FillComboboxes(gvDetails);
                PowerWebService.InitDetailGrid(Page, gvDetails);
                BindDetailGrid(gvDetails);
            }
        }

        protected void gvFru_Cant_DetailRowExpandedChanged(object sender, ASPxGridViewDetailRowEventArgs e)
        {
            if (!e.Expanded)
                GridView.DataBind();
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _fruStub.Data_Registrazione_Fru) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _fruStub.DataOraUltimaModifica_Fru))
             
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, ASPxPanel customOptionsPanel = null)
        {
            List<Fru> frus = CommonService.ConvertTo<Fru>(items);
            XRFru_Cant fruCantsReport = new XRFru_Cant(frus);
            return new ExtXtraReport { Report = fruCantsReport, PictureBox = fruCantsReport.CompanyLogo };
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }
       
    }
}