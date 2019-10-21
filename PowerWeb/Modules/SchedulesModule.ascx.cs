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
using Business.LicenceServiceReference;

namespace PowerWeb.Modules
{
    public partial class SchedulesModule : BaseGridModule, ILogModule
    {

        #region Costants & Fields

        const String KEYFIELDNAME = "ScheduleId";
        private static readonly ILog _log = LogManager.GetLogger(typeof(SchedulesModule));
        private ScheduleData _scheduleDataStub = null;

        #endregion

        #region Public Properties

        public override ASPxGridView GridView
        {
            get
            {
                return gvSchedule;
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
                var template = PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID);

                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionarySchedule();
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

        public override Type EntityType
        {
            get { return typeof(ScheduleData); }
        }

        public List<ScheduleData> GridDataSource
        {
            get
            {
                // recupero la lista delle schedulazioni dalla sessione, se non è presente allora la richiedo al servizio esterno di gestione e la metto in sessione
                // prima di ritornarne il valore
                var scheduleDataList = PowerWebContext.GetFromSession<List<ScheduleData>>(String.Format("ScheduleDataList_{0}", GridView.ID));
                if (scheduleDataList == null)
                {
                    try
                    {
                        // caricamento del servizio che restituisce l'elenco delle schedulazioni
                        // e recupero delle schedulazioni stesse
                        var sc = new LicenceServiceClient();
                        scheduleDataList = sc.GetScheduleDatas(RepoManager.ParamRepo.ParametersRow.PublicKey, PowerWebContext.Current.User.Utenti_Id, PowerWebContext.Current.UserLevel.Funz_Aut).ToList();

                        // inserimento in sessione di quanto calcolato
                        PowerWebContext.SetToSession(String.Format("ScheduleDataList_{0}", GridView.ID), scheduleDataList);
                    }
                    catch (Exception)
                    {
                        scheduleDataList = new List<ScheduleData>();
                    }
                }

                // ritorno del valore della proprietà
                return scheduleDataList;
            }

            set
            {
                PowerWebContext.SetToSession(String.Format("ScheduleDataList_{0}", GridView.ID), value);
            }
        }

        #endregion

        #region Public Methods

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _scheduleDataStub.ScheduleCreationDateTime) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _scheduleDataStub.ScheduleEditDateTime) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _scheduleDataStub.ScheduleStartDateTime) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _scheduleDataStub.ScheduleEndDateTime))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        #endregion

        #region Protected Methods

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(ScheduleData), gvSchedule);
            PowerWebService.FillComboboxes(gvSchedule);

            if (!Page.IsCallback && !Page.IsPostBack)
            {
                GridDataSource = null;
            }

            BindGrid();
        }

        protected void gvSchedule_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        #endregion

        #region Private Methods

        private void BindGrid()
        {
            gvSchedule.KeyFieldName = KEYFIELDNAME;
            var emptyList = Enumerable.Empty<ScheduleData>();
            if (IsToPopulateGrid)
            {
                gvSchedule.DataSource = GridDataSource.Any() ? GridDataSource : emptyList;
            }
            else
                gvSchedule.DataSource = emptyList;

        }

        #endregion

        #region gvSchedule-RowDeleting-gvSchedule_CommandButtonInitialize-BatchUpdate

        protected void gvSchedule_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("ScheduleData-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvSchedule.KeyFieldName]);

            // inizializzazione del servizio esterno utilizzato per la cancellazione
            var sc = new LicenceServiceClient();

            bool operationsSucceded = false;
            try
            {
                // cancellazione del record da parte del servizio esterno
                operationsSucceded = sc.RemoveScheduleData(RepoManager.ParamRepo.ParametersRow.PublicKey, currentId, PowerWebContext.Current.User.Utenti_Id, PowerWebContext.Current.UserLevel.Funz_Aut);
            }
            catch (Exception)
            {
                // silenziamento dell'errore
            }

            // se l'operazione di cancellazione del servizio esterno è andata a buon fine
            // cancello la schedulazione anche dall'attuale elenco salvato in sessione
            if (operationsSucceded)
                GridDataSource = GridDataSource.Where(sch => sch.ScheduleId != currentId).ToList();

            e.Cancel = true;
            BindGrid();
        }

        public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        }

        #endregion

        public log4net.ILog Log
        {
            get { return _log; }
        }

    }
}