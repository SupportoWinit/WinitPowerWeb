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

namespace PowerWeb.Modules
{
    public partial class Col_NoteModule : BaseGridModule, IPrintModule, ILogModule
    {
        //DEFINIZIONI      
        private Col _colStub = null;
        const String KEYFIELDNAME = "Col_Id";
        const String DETAILKEYFIELDNAME = "Col_Note_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Col_NoteModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvCol_Note;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryCol_Note();
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
            PowerWebService.FillGridLabels(typeof(Col), gvCol_Note);
            PowerWebService.FillComboboxes(gvCol_Note);
            BindGrid();
        }

        private void BindGrid()
        {
            gvCol_Note.KeyFieldName = KEYFIELDNAME;
            gvCol_Note.DataSource = RepoManager.ColRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)
                gvCol_Note.DataBind();
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int colId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Col_NoteRepo.Find(cn => cn.Col_Id == colId).OrderByDescending(cn => cn.Data_Nota_Col_Note).ToList();
        }

        public override Type EntityType
        {
            get { return typeof(Col); }
        }

        #region gvCol_Note_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect-DetailRowExpandedChanged
        protected void gvCol_Note_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
        }

        protected void gvCol_Note_Detail_InitNewRow(object sender, DevExpress.Web.Data.ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Col_Note newCol_Note = RepoManager.Col_NoteRepo.Init();
                PowerWebService.FillGridProperties(newCol_Note, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvCol_Note_Detail_RowValidating(object sender, DevExpress.Web.Data.ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                Col_Note newCol_Note = RepoManager.Col_NoteRepo.Init();

                if (IsInBatchMode)
                {
                    var currentId = Convert.ToInt32(e.Keys[gvDetails.KeyFieldName]);
                    if (currentId > 0)
                    {
                        var currentCol_Note = GridView.GetRow(e.VisibleIndex);
                        PowerWebService.FillValues(currentCol_Note, e.NewValues, e.OldValues);
                    }
                }

                int colId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
                newCol_Note.Col_Id = colId;
                PowerWebService.FillEntityProperties(newCol_Note, e.NewValues);
                PowerWebService.FillEntityKey(newCol_Note, e.Keys, DETAILKEYFIELDNAME);
                RepoManager.Col_NoteRepo.SetEntityBeforeAddOrUpdate(newCol_Note);
                PowerWebService.AddValidationErrors(RepoManager.Col_NoteRepo.Check(newCol_Note, e.IsNewRow), e.Errors, gvDetails, typeof(Col_NoteModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvCol_Note_Detail_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("COL_NOTE-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            int colId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());            
            Col_Note newCol_Note = RepoManager.Col_NoteRepo.Init();
            PowerWebService.FillEntityProperties(newCol_Note, e.NewValues);
            RepoManager.Col_NoteRepo.SetEntityBeforeAddOrUpdate(newCol_Note);
            newCol_Note.Col_Id = colId;            
            RepoManager.Col_NoteRepo.Add(newCol_Note, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvCol_Note_Detail_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("COL_NOTE-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Col_Note currentColNote = RepoManager.Col_NoteRepo.Single(cn => cn.Col_Note_Id == currentId);
            PowerWebService.FillEntityProperties(currentColNote, e.NewValues);
            RepoManager.Col_NoteRepo.SetEntityBeforeAddOrUpdate(currentColNote);
            RepoManager.Col_NoteRepo.SaveChanges();
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvCol_Note_Detail_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("COL_NOTE-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Col_Note currentCol_Note = RepoManager.Col_NoteRepo.Single(cn => cn.Col_Note_Id == currentId);
            RepoManager.Col_NoteRepo.Delete(currentCol_Note, true);
            e.Cancel = true;
            BindDetailGrid(gvDetail);
        }

        protected void gvCol_Note_Detail_BeforePerformDataSelect(object sender, EventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;

            if (gvDetails != null)
            {
                //Carica le Labels della DataGrid di Dettaglio
                PowerWebService.FillGridLabels(typeof(Col_Note), gvDetails);
                PowerWebService.FillComboboxes(gvDetails);
                PowerWebService.InitDetailGrid(Page, gvDetails);
                //Carica la DataGrid di Dettaglio
                BindDetailGrid(gvDetails);
            }
        }
        protected void gvCol_Note_DetailRowExpandedChanged(object sender, ASPxGridViewDetailRowEventArgs e)
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
            XRCol_Note colNotesReport = new XRCol_Note(cols);
            return new ExtXtraReport { Report = colNotesReport, PictureBox = colNotesReport.CompanyLogo };
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }

    }
}