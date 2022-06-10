using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Business.Repository;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.Data;
using Domain;
using Business;
using Common;
using Reports;
using log4net;
using DevExpress.Web.ASPxEditors;

namespace PowerWeb.Modules
{
    public partial class CliModule : BaseGridModule, IPrintModule, ILogModule
    {
        //DEFINIZIONI      
        const Cli _cliStub = null;
        const String KEYFIELDNAME = "Cli_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(CliModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvCli;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryCli();
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
            
            //Carica i Campi in CASCADE
            GridViewDataComboBoxColumn domLuogo = gvCli.Columns["Domicilio_Luogo_Cli"] as GridViewDataComboBoxColumn;
            domLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnDomLuogoChanged";
            GridViewDataComboBoxColumn resLuogo = gvCli.Columns["Residenza_Luogo_Cli"] as GridViewDataComboBoxColumn;
            resLuogo.PropertiesComboBox.ClientSideEvents.SelectedIndexChanged = "OnResLuogoChanged";

            gvCli.DataBind();

            PowerWebService.FillGridLabels(typeof(Cli), gvCli);
            PowerWebService.FillComboboxes(gvCli);

            BindGrid();
        }

        private void BindGrid()
        {
            //gvCli.KeyFieldName = KEYFIELDNAME;
            //gvCli.DataSource = RepoManager.CliRepo.GetAll();
            //if (!Page.IsPostBack && !Page.IsCallback)
            //    gvCli.DataBind();
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvCli.KeyFieldName = KEYFIELDNAME;
            IQueryable<Cli> currDataSource = Enumerable.Empty<Cli>().AsQueryable();
            var emptyList = Enumerable.Empty<Cli>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.CliRepo.GetAll(true).AsQueryable();
                gvCli.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvCli.DataSource = emptyList;

        }

        protected void gvCli_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Cli); }
        }

        #region gvCli-InitNewRow-RowValidating-RomInserting-RowUpdating-RowDeleting-gvCli_CommandButtonInitialize-BatchUpdate
        protected void gvCli_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Cli initCli = RepoManager.CliRepo.Init();
                PowerWebService.FillGridProperties(initCli, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, gvCli, e.NewValues);
            }
        }

        protected void gvCli_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Cli newCli = new Cli();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvCli.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentCant = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentCant,e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(newCli, e.NewValues);
            PowerWebService.FillEntityKey(newCli, e.Keys, KEYFIELDNAME);
            RepoManager.CliRepo.SetEntityBeforeAddOrUpdate(newCli);
            PowerWebService.AddValidationErrors(RepoManager.CliRepo.Check(newCli, e.IsNewRow), e.Errors, gvCli, typeof(CliModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvCli_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("CLI-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Cli newCli = new Cli();
            PowerWebService.FillEntityProperties(newCli, e.NewValues);
            RepoManager.CliRepo.SetEntityBeforeAddOrUpdate(newCli);
            RepoManager.CliRepo.Add(newCli, true);
            e.Cancel = true;
            gvCli.CancelEdit();
            BindGrid();
        }

        protected void gvCli_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("CLI-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));

            var currentId = Convert.ToInt32(e.Keys[gvCli.KeyFieldName]);
            Cli currentCli = RepoManager.CliRepo.Single(u => u.Cli_Id == currentId);
            PowerWebService.FillEntityProperties(currentCli, e.NewValues);
            RepoManager.CliRepo.SetEntityBeforeAddOrUpdate(currentCli);
            RepoManager.CliRepo.SaveChanges();
            e.Cancel = true;
            gvCli.CancelEdit();
            BindGrid();
        }

        protected void gvCli_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("CLI-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvCli.KeyFieldName]);
            Cli currentCli = RepoManager.CliRepo.Single(u => u.Cli_Id == currentId);
            RepoManager.CliRepo.Delete(currentCli, true);
            e.Cancel = true;
            BindGrid();
        }

        protected void gvCli_CommandButtonInitialize(object sender, ASPxGridViewCommandButtonEventArgs e)
        //Abilita/disabilita i Tasti di INS/MOD/DEL in base agli eventuali Filtri x FILIALE (se attivato DOMINIOMANUTENZIONE in PARAM)
        {
        }

        public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        }

        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _cliStub.Data_Registrazione_Cli) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cliStub.DataOraUltimaModifica_Cli) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cliStub.Data_Rapporto_Inizio_Cli) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _cliStub.Data_Rapporto_Fine_Cli))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Cli> clis = CommonService.ConvertTo<Cli>(items);
            XRCli cliReport = new XRCli(clis, PowerWebService.ConvertTabPageExtendedToString(selectedTabs));
            return new ExtXtraReport { Report = cliReport, PictureBox = cliReport.CompanyLogo };
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }
        
    }
}