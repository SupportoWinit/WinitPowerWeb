using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Business.Repository;
using DevExpress.Web.ASPxGridView;
using Common;
using Reports;
using log4net;
using System.Text.RegularExpressions;
using System.IO;

namespace PowerWeb.Modules
{
    public partial class Cant_FruModule : BaseGridModule, IPrintModule, ILogModule
    {
        //DEFINIZIONI  
        private Cant _cantStub = null;
        private Fru_Cant _fruCantStub = null;
        const String KEYFIELDNAME = "Cant_Id";
        const String DETAILKEYFIELDNAME = "Fru_Cant_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Cant_FruModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvCant_Fru;
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

        public override IList<string> ForceWritableCombo
        {
            get { return new List<string>() { CommonService.GetPropertyName(() => _fruCantStub.N_Serie_Fru) }; }
        }

        public bool IsInBatchMode
        {
            get { return GridView.SettingsEditing.Mode == GridViewEditingMode.Batch; }
        }

        public override ASPxGridView GridViewDetail
        {
            get
            {
                return gvCant_Fru.FindDetailRowTemplateControl(0, "gvCant_Fru_Detail") as ASPxGridView;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryCant_Fru();
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
            PowerWebService.FillGridLabels(typeof(Cant), gvCant_Fru);
            PowerWebService.FillComboboxes(gvCant_Fru);

            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvCant_Fru.KeyFieldName = KEYFIELDNAME;
            IQueryable<Cant> currDataSource = Enumerable.Empty<Cant>().AsQueryable();
            var emptyList = Enumerable.Empty<Cant>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.CantRepo.GetAll(true).AsQueryable();
                gvCant_Fru.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvCant_Fru.DataSource = emptyList;
        }

        protected void gvCant_Fru_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int canId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Fru_CantRepo.Find(fc => fc.Cant_Id == canId).OrderByDescending(fc => fc.Abilitazione_Data_Inizio_Fru_Can).ToList();
        }


        public override Type EntityType
        {
            get { return typeof(Cant); }
        }

        #region gvCant_Fru_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect-DetailRowExpandedChange-gvCant_Fru_DetailRowExpandedChanged
        protected void gvCant_Fru_Detail_Init(object sender, EventArgs e)
        {
            var gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
            {
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
            }

        }

        protected void gvCant_Fru_Detail_InitNewRow(object sender, DevExpress.Web.Data.ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Fru_Cant newFruCant = RepoManager.Fru_CantRepo.Init();
                PowerWebService.FillGridProperties(newFruCant, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvCant_Fru_Detail_RowValidating(object sender, DevExpress.Web.Data.ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;


            if (gvDetails != null)
            {
                Fru_Cant newFruCant = RepoManager.Fru_CantRepo.Init();

                if (IsInBatchMode)
                {
                    var currentId = Convert.ToInt32(e.Keys[gvDetails.KeyFieldName]);
                    if (currentId > 0)
                    {
                        var currentFruCant = GridView.GetRow(e.VisibleIndex);
                        PowerWebService.FillValues(currentFruCant, e.NewValues, e.OldValues);
                    }
                }

                int cantId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
                newFruCant.Cant_Id = cantId;
                PowerWebService.FillEntityProperties(newFruCant, e.NewValues);
                PowerWebService.FillEntityKey(newFruCant, e.Keys, DETAILKEYFIELDNAME);
                RepoManager.Fru_CantRepo.SetEntityBeforeAddOrUpdate(newFruCant);
                PowerWebService.AddValidationErrors(RepoManager.Fru_CantRepo.Check(newFruCant, e.IsNewRow), e.Errors, gvDetails, typeof(Fru_CantModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvCant_Fru_Detail_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("CANT_FRU-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            int cantId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());
            Fru_Cant newFruCant = RepoManager.Fru_CantRepo.Init();
            PowerWebService.FillEntityProperties(newFruCant, e.NewValues);
            RepoManager.Fru_CantRepo.SetEntityBeforeAddOrUpdate(newFruCant);
            newFruCant.Cant_Id = cantId;
            RepoManager.Fru_CantRepo.Add(newFruCant, true);

            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvCant_Fru_Detail_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("CANT_FRU-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            ASPxGridView gvDetail = (ASPxGridView)sender;
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Fru_Cant currentFruCant = RepoManager.Fru_CantRepo.Single(f => f.Fru_Cant_Id == currentId);
            PowerWebService.FillEntityProperties(currentFruCant, e.NewValues);
            RepoManager.Fru_CantRepo.SetEntityBeforeAddOrUpdate(currentFruCant);
            RepoManager.Fru_CantRepo.Update(currentFruCant, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvCant_Fru_Detail_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("CANT_FRU-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Fru_Cant currentFruCant = RepoManager.Fru_CantRepo.Single(f => f.Fru_Cant_Id == currentId);

            //vado ad eliminare solo le associazioni ch siano dopo la data blocco
            if (currentFruCant.Abilitazione_Data_Inizio_Fru_Can > RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg)
                RepoManager.Fru_CantRepo.Delete(currentFruCant, true);

            e.Cancel = true;
            BindDetailGrid(gvDetail);
        }

        protected void gvCant_Fru_Detail_BeforePerformDataSelect(object sender, EventArgs e)
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

        protected void gvCant_Fru_DetailRowExpandedChanged(object sender, ASPxGridViewDetailRowEventArgs e)
        {
            if (!e.Expanded)
                GridView.DataBind();
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

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Cant> cants = CommonService.ConvertTo<Cant>(items);
            XRCant_Fru cantFrusReport = new XRCant_Fru(cants);
            return new ExtXtraReport { Report = cantFrusReport, PictureBox = cantFrusReport.CompanyLogo };
        }

        public ILog Log
        {
            get { return _log; }
        }
    }


   

}