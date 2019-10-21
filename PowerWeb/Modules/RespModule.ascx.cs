using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using Domain;
using DevExpress.Web.Data;
using log4net;
using Business;
using System;
using DevExpress.Web.ASPxGridView;
using Reports;
using Common;
using Business.Profile;

namespace PowerWeb.Modules
{
    public partial class RespModule : BaseGridModule, IPrintModule, ILogModule
    {
        //DEFINIZIONI
        private Resp _respStub = null; 
        const String KEYFIELDNAME = "Resp_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(RespModule));

        public override ASPxGridView GridView
        {
            get { return gvResp; }
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryResp();
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
            PowerWebService.FillGridLabels(typeof(Resp), GridView);
            PowerWebService.FillComboboxes(gvResp);
            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvResp.KeyFieldName = KEYFIELDNAME;
            List<Resp> currDataSource = new List<Resp>();
            var emptyList = Enumerable.Empty<Resp>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.RespRepo.GetAll().ToList();
                gvResp.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvResp.DataSource = emptyList;
        }

        protected void gvResp_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }
        public override Type EntityType
        {
            get { return typeof(Resp); }
        }

        #region gvResp : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BatchUpdate
        protected void gvResp_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Resp initResp = RepoManager.RespRepo.Init();
                PowerWebService.FillGridProperties(initResp, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvResp_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Resp newResp = new Resp();
            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvResp.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentResp = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentResp, e.NewValues, e.OldValues);
                }
            }
            PowerWebService.FillEntityProperties(newResp, e.NewValues);
            PowerWebService.FillEntityKey(newResp, e.Keys, KEYFIELDNAME);
            RepoManager.RespRepo.SetEntityBeforeAddOrUpdate(newResp);
            PowerWebService.AddValidationErrors(RepoManager.RespRepo.Check(newResp, e.IsNewRow), e.Errors, gvResp, typeof(RespModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvResp_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("RESP-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Resp newResp = new Resp();
            PowerWebService.FillEntityProperties(newResp, e.NewValues);
            RepoManager.RespRepo.SetEntityBeforeAddOrUpdate(newResp);
            RepoManager.RespRepo.Add(newResp, true);

            // ad ogni modifica dei reposnsabili si riaggiorna il dato di filtro, se configurato
            PowerWebMembershipProvider.ResetDomainFilter(PowerWebContext.Current.User);

            e.Cancel = true;
            gvResp.CancelEdit();
            BindGrid();
        }

        protected void gvResp_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("RESP_Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvResp.KeyFieldName]);
            Resp currentResp = RepoManager.RespRepo.Single(u => u.Resp_Id == currentId);
            PowerWebService.FillEntityProperties(currentResp, e.NewValues);
            RepoManager.RespRepo.SetEntityBeforeAddOrUpdate(currentResp);
            RepoManager.RespRepo.SaveChanges();

            // ad ogni modifica dei reposnsabili si riaggiorna il dato di filtro, se configurato
            PowerWebMembershipProvider.ResetDomainFilter(PowerWebContext.Current.User);

            e.Cancel = true;
            gvResp.CancelEdit();
            BindGrid();
        }

        protected void gvResp_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("REPS_Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvResp.KeyFieldName]);
            Resp currentResp = RepoManager.RespRepo.Single(u => u.Resp_Id == currentId);
            RepoManager.RespRepo.Delete(currentResp, true);

            // ad ogni modifica dei reposnsabili si riaggiorna il dato di filtro, se configurato
            PowerWebMembershipProvider.ResetDomainFilter(PowerWebContext.Current.User);

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
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _respStub.Data_Registrazione_Resp) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _respStub.DataOraUltimaModifica_Resp))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Resp> resps = CommonService.ConvertTo<Resp>(items);

            XRResp RespReport = new XRResp(resps,PowerWebService.ConvertTabPageExtendedToString(selectedTabs));

            return new ExtXtraReport { Report = RespReport, PictureBox = RespReport.CompanyLogo };
        }
        
        public log4net.ILog Log
        {
            get { return _log; }
        }        
    }
}