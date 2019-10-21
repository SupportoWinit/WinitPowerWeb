using System;
using System.Collections.Generic;
using System.Linq;
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
    public partial class PruModule : BaseGridModule, IPrintModule, ILogModule
    {
        //DEFINIZIONI
        private Pru _pruStub = null;
        const String KEYFIELDNAME = "Pru_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(PruModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvPru;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryPru();
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
            //Imposto i Nomi in Lingua delle Label e Button della Pagina Video
            btnUpload.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_IMPORT_MATRICOLE_UNITA_PORTATILI);
            PowerWebService.FillGridLabels(typeof(Pru), gvPru);
            PowerWebService.FillComboboxes(gvPru);
            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvPru.KeyFieldName = KEYFIELDNAME;
            IQueryable<Pru> currDataSource = Enumerable.Empty<Pru>().AsQueryable();
            var emptyList = Enumerable.Empty<Pru>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.PruRepo.GetAll(true).AsQueryable();
                gvPru.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvPru.DataSource = emptyList;
        }

        protected void gvPru_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return new Pru().GetType(); }
        }

        #region gvPru : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BatchUpdate
        protected void gvPru_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Pru initPru = RepoManager.PruRepo.Init();
                PowerWebService.FillGridProperties(initPru, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvPru_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Pru newPru = new Pru();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvPru.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentPru = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentPru, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(newPru, e.NewValues);
            PowerWebService.FillEntityKey(newPru, e.Keys, KEYFIELDNAME);
            RepoManager.PruRepo.SetEntityBeforeAddOrUpdate(newPru);
            PowerWebService.AddValidationErrors(RepoManager.PruRepo.Check(newPru, e.IsNewRow), e.Errors, gvPru, typeof(PruModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvPru_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("PRU-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Pru newPru = new Pru();
            PowerWebService.FillEntityProperties(newPru, e.NewValues);
            RepoManager.PruRepo.SetEntityBeforeAddOrUpdate(newPru);
            RepoManager.PruRepo.Add(newPru, true);
            e.Cancel = true;
            gvPru.CancelEdit();
            BindGrid();
        }

        protected void gvPru_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("PRU-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvPru.KeyFieldName]);
            Pru currentPru = RepoManager.PruRepo.Single(u => u.Pru_Id == currentId);
            PowerWebService.FillEntityProperties(currentPru, e.NewValues);
            RepoManager.PruRepo.SetEntityBeforeAddOrUpdate(currentPru);
            RepoManager.PruRepo.SaveChanges();
            e.Cancel = true;
            gvPru.CancelEdit();
            BindGrid();
        }

        protected void gvPru_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("PRU-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvPru.KeyFieldName]);
            Pru currentPru = RepoManager.PruRepo.Single(u => u.Pru_Id == currentId);
            RepoManager.PruRepo.Delete(currentPru, true);
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
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _pruStub.Data_Registrazione_Pru) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _pruStub.DataOraUltimaModifica_Pru))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Pru> prus = CommonService.ConvertTo<Pru>(items);
            XRPru pruReport = new XRPru(prus, PowerWebService.ConvertTabPageExtendedToString(selectedTabs));
            return new ExtXtraReport { Report = pruReport, PictureBox = pruReport.CompanyLogo };
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }

        protected void upldImport_FileUploadComplete(object sender, FileUploadCompleteEventArgs e)
        {
            String newImportFile = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path + String.Format("PruFile{0}{1}", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"), ".txt"));

            FileInfo fileInfo = new FileInfo(newImportFile);
            if (!fileInfo.Exists)
            {
                e.UploadedFile.SaveAs(newImportFile);
                RepoManager.PruRepo.ImportFromTXT((File.ReadAllLines(newImportFile)));

                fileInfo.Delete();
            }
        }

        
    }
}