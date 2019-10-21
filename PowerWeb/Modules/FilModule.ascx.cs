using System;
using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using Domain;
using Common;
using DevExpress.Web.Data;
using DevExpress.Web.ASPxGridView;
using Business;
using Reports;
using log4net;
using Business.Profile;

namespace PowerWeb.Modules
{
    public partial class FilModule : BaseGridModule, IPrintModule, ILogModule
    {
        const String KEYFIELDNAME = "Fil_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(FilModule));

        public override ASPxGridView GridView
        {
            get { return gvFil; }
        }

        public bool IsInBatchMode
        {
            get { return GridView.SettingsEditing.Mode == GridViewEditingMode.Batch; }
        }

        public override ASPxGridView GridViewDetail
        {
            get { return null; }
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryFil();
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
            PowerWebService.FillGridLabels(typeof(Fil), GridView);
            PowerWebService.FillComboboxes(gvFil);
            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvFil.KeyFieldName = KEYFIELDNAME;
            IQueryable<Fil> currDataSource = Enumerable.Empty<Fil>().AsQueryable();
            var emptyList = Enumerable.Empty<Fil>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.FilRepo.GetAll().AsQueryable();
                gvFil.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvFil.DataSource = emptyList;

        }

        protected void gvFil_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Fil); }
        }

        #region gvFil : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BatchUpdate

        protected void gvFil_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Fil initFil = RepoManager.FilRepo.Init();
                PowerWebService.FillGridProperties(initFil, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvFil_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Fil newFil = new Fil();
           
            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvFil.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentFil = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentFil, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(newFil, e.NewValues);
            PowerWebService.FillEntityKey(newFil, e.Keys, KEYFIELDNAME);
            RepoManager.FilRepo.SetEntityBeforeAddOrUpdate(newFil);
            PowerWebService.AddValidationErrors(
              RepoManager.FilRepo.Check(newFil, e.IsNewRow), e.Errors, gvFil, typeof(FilModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvFil_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("FIL-Row Inseting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Fil newFil = new Fil();
            PowerWebService.FillEntityProperties(newFil, e.NewValues);
            RepoManager.FilRepo.SetEntityBeforeAddOrUpdate(newFil);
            RepoManager.FilRepo.Add(newFil, true);

            // ad ogni modifica delle filiali si riaggiorna il dato di filtro, se configurato
            PowerWebMembershipProvider.ResetDomainFilter(PowerWebContext.Current.User);

            e.Cancel = true;
            gvFil.CancelEdit();
            BindGrid();
        }

        protected void gvFil_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("FIL-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvFil.KeyFieldName]);
            Fil currentFil = RepoManager.FilRepo.Single(u => u.Fil_Id == currentId);
            PowerWebService.FillEntityProperties(currentFil, e.NewValues);
            RepoManager.FilRepo.SetEntityBeforeAddOrUpdate(currentFil);
            RepoManager.FilRepo.SaveChanges();

            // ad ogni modifica delle filiali si riaggiorna il dato di filtro, se configurato
            PowerWebMembershipProvider.ResetDomainFilter(PowerWebContext.Current.User);

            e.Cancel = true;
            gvFil.CancelEdit();
            BindGrid();
        }

        protected void gvFil_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("FIL-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvFil.KeyFieldName]);
            Fil currentFil = RepoManager.FilRepo.Single(u => u.Fil_Id == currentId);
            RepoManager.FilRepo.Delete(currentFil, true);

            // ad ogni modifica delle filiali si riaggiorna il dato di filtro, se configurato
            PowerWebMembershipProvider.ResetDomainFilter(PowerWebContext.Current.User);

            e.Cancel = true;
            BindGrid();
        }

        public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        }

        #endregion

        public override void HeaderFilterFillItems(object sender, DevExpress.Web.ASPxGridView.ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (VUOTA perchè NON ci sono Campi Date da Gestire ma va comunque definita vuota se non ce ne sono)
        {
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Fil> fils = CommonService.ConvertTo<Fil>(items);

            XRFil FilReport = new XRFil(fils, PowerWebService.ConvertTabPageExtendedToString(selectedTabs));

            return new ExtXtraReport { Report = FilReport, PictureBox = FilReport.CompanyLogo };
        }

        public ILog Log
        {
            get { return _log; }
        }

        

    }
}