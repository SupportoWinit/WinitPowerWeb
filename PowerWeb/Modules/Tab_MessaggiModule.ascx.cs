using System.Collections.Generic;
using Business.Repository;
using Domain;
using Common;
using DevExpress.Web.Data;
using log4net;
using Reports;
using System;
using DevExpress.Web.ASPxGridView;

namespace PowerWeb.Modules
{
    public partial class Tab_MessaggiModule : BaseGridModule, IPrintModule, ILogModule
    {
        //DEFINIZIONI
        private Tab_Messaggi _messageStub = null;
        const String KEYFIELDNAME = "Tab_Messaggi_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Tab_MessaggiModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvMessages;
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

        public override ASPxGridView GridViewDetail
        {
            get { return null; }
        }

        public override PowerFormTemplate EditDetailFormTemplate
        {
            get { return null; }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Tab_Messaggi), GridView);
            PowerWebService.FillComboboxes(gvMessages);
            BindGrid();
        }

        private void BindGrid()
        {
            gvMessages.KeyFieldName = KEYFIELDNAME;
            gvMessages.DataSource = RepoManager.Tab_MessaggiRepo.GetAll();
            if (!Page.IsPostBack && !Page.IsCallback)
            {
                gvMessages.ClearSort();
                //CREA RAGGRUPPAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                gvMessages.GroupBy(gvMessages.Columns[CommonService.GetPropertyName(() => _messageStub.Applicazione_Tab_Messaggi_Id)]);
                gvMessages.GroupBy(gvMessages.Columns[CommonService.GetPropertyName(() => _messageStub.Data_Tab_Messaggi)]);
                //CREA ORDINAMENTO PER L'ELENCO DELLE COLONNE INDICATE
                (gvMessages.Columns[CommonService.GetPropertyName(() => _messageStub.Applicazione_Tab_Messaggi_Id)] as GridViewDataColumn).SortAscending();
                (gvMessages.Columns[CommonService.GetPropertyName(() => _messageStub.Data_Tab_Messaggi)] as GridViewDataColumn).SortAscending();
                (gvMessages.Columns[CommonService.GetPropertyName(() => _messageStub.Funzione_Tab_Messaggi_Id)] as GridViewDataColumn).SortAscending();
                gvMessages.DataBind();
            }
        }

        public override Type EntityType
        {
            get { return new Tab_Messaggi().GetType(); }
        }

        #region gvMessages : RowDeleting
        protected void gvMessages_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            var currentId = Convert.ToInt32(e.Keys[gvMessages.KeyFieldName]);
            Tab_Messaggi message = RepoManager.Tab_MessaggiRepo.Single(mess => mess.Tab_Messaggi_Id == currentId);

            RepoManager.Tab_MessaggiRepo.Delete(message, true);
            e.Cancel = true;
            BindGrid();
        }
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _messageStub.Data_Tab_Messaggi))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            return null;
        }

        public ILog Log
        {
            get { return _log; }
        }
    }
}