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
    public partial class Cant_NoteModule : BaseGridModule, IPrintModule, ILogModule
    {
        //Definizioni      
        private Cant _cantStub = null;
        const String KEYFIELDNAME = "Cant_Id";
        const String DETAILKEYFIELDNAME = "Cant_Note_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Cant_NoteModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvCant_Note;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryCant_Note();
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
            PowerWebService.FillGridLabels(typeof(Cant), gvCant_Note);
            PowerWebService.FillComboboxes(gvCant_Note);
            BindGrid();
        }

        private void BindGrid()
        {
            gvCant_Note.KeyFieldName = KEYFIELDNAME;
            gvCant_Note.DataSource = RepoManager.CantRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)
                gvCant_Note.DataBind();
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int cantId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = DETAILKEYFIELDNAME;
            gvDetails.DataSource = RepoManager.Cant_NoteRepo.Find(cn => cn.Cant_Id == cantId).OrderByDescending(cn => cn.Data_Nota_Can_Note).ToList();
        }

        public override Type EntityType
        {
            get { return typeof(Cant); }
        }

        #region gvCant_Note_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect-DetailRowExpandedChanged
        protected void gvCant_Note_Detail_Init(object sender, EventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            if (gvDetail != null)
                gvDetail.Templates.EditForm = EditDetailFormTemplate;
        }

        protected void gvCant_Note_Detail_InitNewRow(object sender, DevExpress.Web.Data.ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Cant_Note newCant_Note = RepoManager.Cant_NoteRepo.Init();
                PowerWebService.FillGridProperties(newCant_Note, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvCant_Note_Detail_RowValidating(object sender, DevExpress.Web.Data.ASPxDataValidationEventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                Cant_Note newCant_Note = RepoManager.Cant_NoteRepo.Init();

                if (IsInBatchMode)
                {
                    var currentId = Convert.ToInt32(e.Keys[gvDetails.KeyFieldName]);
                    if (currentId > 0)
                    {
                        var currentCant_Note = GridView.GetRow(e.VisibleIndex);
                        PowerWebService.FillValues(currentCant_Note, e.NewValues, e.OldValues);
                    }
                }
                int cantId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
                newCant_Note.Cant_Id = cantId;
                PowerWebService.FillEntityProperties(newCant_Note, e.NewValues);
                PowerWebService.FillEntityKey(newCant_Note, e.Keys, DETAILKEYFIELDNAME);
                RepoManager.Cant_NoteRepo.SetEntityBeforeAddOrUpdate(newCant_Note);
                PowerWebService.AddValidationErrors(RepoManager.Cant_NoteRepo.Check(newCant_Note, e.IsNewRow), e.Errors, gvDetails, typeof(Cant_NoteModule));
                if (e.HasErrors)
                    e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
            }
        }

        protected void gvCant_Note_Detail_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("CANT_NOTE-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            int cantId = Convert.ToInt32(gvDetail.GetMasterRowKeyValue());            
            Cant_Note newCant_Note = RepoManager.Cant_NoteRepo.Init();
            PowerWebService.FillEntityProperties(newCant_Note, e.NewValues);
            RepoManager.Cant_NoteRepo.SetEntityBeforeAddOrUpdate(newCant_Note);
            newCant_Note.Cant_Id = cantId;            
            RepoManager.Cant_NoteRepo.Add(newCant_Note, true);
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvCant_Note_Detail_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("CANT_NOTE-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            ASPxGridView gvDetail = (ASPxGridView)sender;
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Cant_Note currentCantNote = RepoManager.Cant_NoteRepo.Single(cn => cn.Cant_Note_Id == currentId);
            PowerWebService.FillEntityProperties(currentCantNote, e.NewValues);
            RepoManager.Cant_NoteRepo.SetEntityBeforeAddOrUpdate(currentCantNote);
            RepoManager.Cant_NoteRepo.SaveChanges();
            e.Cancel = true;
            gvDetail.CancelEdit();
            BindDetailGrid(gvDetail);
        }

        protected void gvCant_Note_Detail_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        {
            ASPxGridView gvDetail = (ASPxGridView)sender;
            _log.Info(String.Format("CANT_NOTE-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvDetail.KeyFieldName]);
            Cant_Note currentCantNote = RepoManager.Cant_NoteRepo.Single(cn => cn.Cant_Note_Id == currentId);
            RepoManager.Cant_NoteRepo.Delete(currentCantNote, true);
            e.Cancel = true;
            BindDetailGrid(gvDetail);
        }

        protected void gvCant_Note_Detail_BeforePerformDataSelect(object sender, EventArgs e)
        {
            ASPxGridView gvDetails = sender as ASPxGridView;
            if (gvDetails != null)
            {
                PowerWebService.FillGridLabels(typeof(Cant_Note), gvDetails);
                PowerWebService.FillComboboxes(gvDetails);
                PowerWebService.InitDetailGrid(Page, gvDetails);
                BindDetailGrid(gvDetails);
            }
        }

        protected void gvCant_Note_DetailRowExpandedChanged(object sender, ASPxGridViewDetailRowEventArgs e)
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
            XRCant_Note cantNotesReport = new XRCant_Note(cants);
            return new ExtXtraReport { Report = cantNotesReport, PictureBox = cantNotesReport.CompanyLogo };
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }
    }
}