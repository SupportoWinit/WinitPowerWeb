using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Domain;
using Common;
using Business.Repository;
using DevExpress.Web.Data;
using DevExpress.Web.ASPxGridView;
using Reports;
using Business;
using log4net;
using DevExpress.Web.ASPxUploadControl;
using System.IO;

namespace PowerWeb.Modules
{
    public partial class FruModule : BaseGridModule, IPrintModule, ILogModule
    {
        //DEFINIZIONI
        private Fru _fruStub = null;
        const String KEYFIELDNAME = "Fru_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(FruModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvFru;
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
            get { return null; }
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
            get { return null; }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            //Imposto i Nomi in Lingua delle Label specifiche di questa Page
            btnUpload.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_IMPORT_MATRICOLE_UNITA_FISSE);

            PowerWebService.FillGridLabels(typeof(Fru), gvFru);
            PowerWebService.FillComboboxes(gvFru);
            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvFru.KeyFieldName = KEYFIELDNAME;
            IQueryable<Fru> currDataSource = Enumerable.Empty<Fru>().AsQueryable();
            var emptyList = Enumerable.Empty<Fru>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.FruRepo.GetAll(true).AsQueryable();
                gvFru.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvFru.DataSource = emptyList;
        }

        protected void gvFru_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Fru); }
        }

        #region gvFru_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BatchUpdate
        protected void gvFru_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Fru initFru = RepoManager.FruRepo.Init();
                PowerWebService.FillGridProperties(initFru, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);

            }
        }

        protected void gvFru_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Fru newFru = new Fru();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvFru.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentFru = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentFru, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(newFru, e.NewValues);
            PowerWebService.FillEntityKey(newFru, e.Keys, KEYFIELDNAME);
            RepoManager.FruRepo.SetEntityBeforeAddOrUpdate(newFru);
            PowerWebService.AddValidationErrors(RepoManager.FruRepo.Check(newFru, e.IsNewRow), e.Errors, gvFru, typeof(FruModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvFru_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("FRU-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Fru newFru = new Fru();
            PowerWebService.FillEntityProperties(newFru, e.NewValues);
            RepoManager.FruRepo.SetEntityBeforeAddOrUpdate(newFru);
            RepoManager.FruRepo.Add(newFru, true);
            e.Cancel = true;
            gvFru.CancelEdit();
            BindGrid();
        }

        protected void gvFru_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("FRU-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvFru.KeyFieldName]);
            Fru currentFru = RepoManager.FruRepo.Single(u => u.Fru_Id == currentId);
            PowerWebService.FillEntityProperties(currentFru, e.NewValues);
            RepoManager.FruRepo.SetEntityBeforeAddOrUpdate(currentFru);
            RepoManager.FruRepo.SaveChanges();
            e.Cancel = true;
            gvFru.CancelEdit();
            BindGrid();
        }

        protected void gvFru_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("FRU-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvFru.KeyFieldName]);
            Fru currentFru = RepoManager.FruRepo.Single(u => u.Fru_Id == currentId);
            RepoManager.FruRepo.Delete(currentFru, true);
            e.Cancel = true;
            BindGrid();
        }

        public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _fruStub.Data_Registrazione_Fru) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _fruStub.DataOraUltimaModifica_Fru))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Fru> frus = CommonService.ConvertTo<Fru>(items);
            XRFru fruReport = new XRFru(frus, PowerWebService.ConvertTabPageExtendedToString(selectedTabs));
            return new ExtXtraReport { Report = fruReport, PictureBox = fruReport.CompanyLogo };
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }

        protected void upldImport_FileUploadComplete(object sender, FileUploadCompleteEventArgs e)
        {
            String newImportFile = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path + String.Format("FruFile{0}{1}", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"), ".txt"));

            FileInfo fileInfo = new FileInfo(newImportFile);
            if (!fileInfo.Exists)
            {
                e.UploadedFile.SaveAs(newImportFile);
                RepoManager.FruRepo.ImportFromTXT((File.ReadAllLines(newImportFile)));

                fileInfo.Delete();
            }
        }

        
    }
}