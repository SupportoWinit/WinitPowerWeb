using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using DevExpress.Web.ASPxCallback;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxEditors;
using Domain;
using DevExpress.Web.Data;
using log4net;
using Business;
using Business.BusinessExtension;
using System;
using DevExpress.Web.ASPxGridView;
using Reports;
using Common;

//TODO: attenzione! Questo modulo non filtra come dovrebbe per responsabile; da aggiungere eventuale filtro per responsabile
namespace PowerWeb.Modules
{
    public partial class WhereIsItModule : BaseGridModule
    {

        #region Constants

        /// <summary>
        /// Il campo chiave della griglia principale del modulo
        /// </summary>
        private const String Keyfieldname = "Col_Id";

        /// <summary>
        /// Il collaboratore utilizzato come template per il recupero dei nomi delle proprietà
        /// </summary>
        private const Col ColStub = default(Col);

        #endregion

        #region Properties

        /// <summary>
        /// Recupera la griglia del modulo attuale.
        /// </summary>
        /// <value>
        /// The grid view.
        /// </value>
        public override ASPxGridView GridView
        {
            get { return gvWhereIsIt; }
        }

        /// <summary>
        /// Recupera la form di edit per i record della griglia principale del modulo corrente.
        /// </summary>
        /// <value>
        /// La form di edit per i record della griglia principale del modulo corrente.
        /// </value>
        public override PowerFormTemplate EditFormTemplate
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Recupera l'elenco degli id collaboratore selezionati dall'utente con le coodinate, il valore che indica lo stato del where is it, il numero di dati sovrapposti
        /// e il titolo e la descrizione del riquadro da visualizzare; sono salvati come ultimi due valori anche le coordinate originali (per evitare problemi di mancata segnalazione sovrapposizione
        /// in caso di selezioni successsive).
        /// </summary>
        /// <value>
        /// L'elenco degli id collaboratore selezionati dall'utente con le coodinate, il valore che indica lo stato del where is it, il numero di dati sovrapposti
        /// e il titolo e la descrizione del riquadro da visualizzare; sono salvati come ultimi due valori anche le coordinate originali (per evitare problemi di mancata segnalazione sovrapposizione
        /// in caso di selezioni successsive).
        /// </value>
        public Dictionary<int, CoordinatesData> SelectedColIdsWithCoordinatesData
        {
            get
            {
                var colsel = PowerWebContext.GetFromSession <Dictionary<int, CoordinatesData>>("SelectedColIdsWithCoordinatesData" + GridView.ID);
                if (colsel == null)
                {
                    colsel = new Dictionary<int, CoordinatesData>();
                    PowerWebContext.SetToSession("SelectedColIdsWithCoordinatesData" + GridView.ID, colsel);
                }
                return colsel;
            }

            set
            {
                Dictionary<int, CoordinatesData> list = value;
                if (list != null)
                    PowerWebContext.SetToSession("SelectedColIdsWithCoordinatesData" + GridView.ID, list);
            }
        }

        /// <summary>
        /// Recupera dai parametri dell'applicativo la chiave di bing utilizzata per effettuare la gelolocalizzazione.
        /// </summary>
        /// <value>
        /// La chiave di bing utilizzata per effettuare la geolocalizzazione.
        /// </value>
        protected string BingKey
        {
            get
            {
                return RepoManager.ParamRepo.ParametersRow.BingKey;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Effettua il reset dei dati di sessione per il modulo corrente.
        /// </summary>
        public override void ResetSession()
        {
            base.ResetSession();

            // al reset della sessione si svuotano anche i collaboratori selezionati con i relativi dati da mappa
            SelectedColIdsWithCoordinatesData = new Dictionary<int, CoordinatesData>();
        }

        #endregion

        #region Page Events

        /// <summary>
        /// Gestisce l'evento init della pagina corrente.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento.</param>
        /// <param name="e">Gli <see cref="EventArgs"/> con i dati evento.</param>
        protected void Page_Init(object sender, EventArgs e)
        {
            // inizializzazione delle label della griglia
            PowerWebService.FillGridLabels(typeof(Col), GridView);

            // popolamento dei combobox in griglia
            PowerWebService.FillComboboxes(GridView);

            // alla prima visualizzazione della pagina si procede al reset dei dati inseriti in sessione
            if (!Page.IsCallback && !Page.IsPostBack)
                ResetSession();


        }

        #endregion

        #region Grid Events

        /// <summary>
        /// Gestisce l'evento di bind della griglia principale del modulo.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento.</param>
        /// <param name="e">L'istanza di tipo <see cref="EventArgs"/> contenente i dati dell'evento.</param>
        protected void gvWhereIsIt_OnDataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => ColStub.WhereIsItDate))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        /// <summary>
        /// Gestisce l'evento OnPageIndexChanged sulla griglia principale dell'applicativo.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento.</param>
        /// <param name="e">L'istanza di tipo <see cref="EventArgs"/> contenente i dati dell'evento.</param>
        protected void gvWhereIsIt_OnPageIndexChanged(object sender, EventArgs e)
        {
            (sender as ASPxGridView).JSProperties["cpPageChanged"] = 1;
        }

        /// <summary>
        /// Gestisce l'evento OnHtmlDataCellPrepared sulla griglia principale del modulo.
        /// </summary>
        /// <param name="sender">Il mittente dell'evetno.</param>
        /// <param name="e">L'istanza di tipo <see cref="ASPxGridViewTableDataCellEventArgs"/> con i dati dell'evento.</param>
        protected void gvWhereIsIt_OnHtmlDataCellPrepared(object sender, ASPxGridViewTableDataCellEventArgs e)
        {

            // se si sta processando la cella di stato del where is it
            // allora si procede alla sua eventuale colorazione
            if (e.DataColumn.FieldName == CommonService.GetPropertyName(() => ColStub.WhereIsItState))
                e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((WhereIsItRecordState)e.CellValue, false);
        }

        /// <summary>
        /// Gestisce l'evento OnCustomJSProperties della griglia principale del modulo.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento.</param>
        /// <param name="e">L'istanza di tipo <see cref="ASPxGridViewClientJSPropertiesEventArgs"/> contenente i dati dell'evento.</param>
        protected void gvWhereIsIt_OnCustomJSProperties(object sender, ASPxGridViewClientJSPropertiesEventArgs e)
        {
            e.Properties["cpVisibleRowCount"] = GridView.VisibleRowCount;
        }

        #endregion

        #region Selection Events

        /// <summary>
        /// Gestisce l'evento di init del checkbox di selezione di tutti gli elementi nella griglia principale del modulo.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento.</param>
        /// <param name="e">L'istanza di tipo <see cref="EventArgs"/> contenente i dati dell'evento.</param>
        protected void cbAll_Init(object sender, EventArgs e)
        {
            // si seleziona il checkbox se sono selezionati tutti gli elementi
            var chk = sender as ASPxCheckBox;
            ASPxGridView grid = (chk.NamingContainer as GridViewHeaderTemplateContainer).Grid;
            chk.Checked = (grid.Selection.Count == grid.VisibleRowCount);
        }

        /// <summary>
        /// Gestisce l'evento di inti del checkbox di selezione degli elementi in pagina nella griglia principale del modulo.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento.</param>
        /// <param name="e">L'istanza di tipo <see cref="EventArgs"/> contenente i dati dell'evento.</param>
        protected void cbPage_Init(object sender, EventArgs e)
        {
            // si seleziona il checkbox se tutti gli elementi nella pagina visualizzata sono selezionati
            var chk = sender as ASPxCheckBox;
            ASPxGridView grid = (chk.NamingContainer as GridViewHeaderTemplateContainer).Grid;

            bool cbChecked = true;
            int start = grid.VisibleStartIndex;
            int end = grid.VisibleStartIndex + grid.SettingsPager.PageSize;
            end = (end > grid.VisibleRowCount ? grid.VisibleRowCount : end);

            for (int i = start; i < end; i++)
                if (!grid.Selection.IsRowSelected(i))
                {
                    cbChecked = false;
                    break;
                }

            chk.Checked = cbChecked;
        }

        /// <summary>
        /// Gestisce l'evento OnCustomJSProperties del checkbox di selezione di tutti i record nella griglia principale del modulo.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento.</param>
        /// <param name="e">L'istanza di tipo <see cref="CustomJSPropertiesEventArgs"/> contenente i dati dell'evento.</param>
        protected void cbAll_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            // si ritorna la proprietà utilizzata per la visualizzazione in lingua del messaggio di conferma
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage", BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_SELEZIONE));
        }

        /// <summary>
        /// Gestisce l'evento OnCallback del aspxcallback utilizzato per il cambio di selezione.
        /// </summary>
        /// <param name="source">Il mittente dell'evento.</param>
        /// <param name="e">L'istanza di tipo <see cref="CallbackEventArgs"/> contenente i dati dell'evento.</param>
        protected void gridSelectionChange_OnCallback(object source, CallbackEventArgs e)
        {
            // in base al tipo di selezione si impostano gli id dei collaboratori selezionati
            string selctionType = e.Parameter;

            switch (selctionType)
            {
                case "sAll":
                    SelectedColIdsWithCoordinatesData = new Dictionary<int, CoordinatesData>();
                    for (int i = 0; i < GridView.VisibleRowCount; i++)
                    {
                        var colId = Convert.ToInt32(GridView.GetRowValues(i, "Col_Id"));
                        Col currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == colId);
                        if (currentCol != default(Col))
                            SelectedColIdsWithCoordinatesData.Add(colId, new CoordinatesData()
                            {
                                CurrentLatitude = currentCol.WhereIsItCanLatitude,
                                CurrentLongitude = currentCol.WhereIsItCanLongitude,
                                WhereIsItState = currentCol.WhereIsItState.ToString(),
                                InfoboxTitle = GetColInfoboxTitle(currentCol),
                                InfoboxDescription = GetColInfoboxDescription(currentCol),
                                OverlappingNumber = 0,
                                OriginalLatitude = currentCol.WhereIsItCanLatitude,
                                OriginalLongitude = currentCol.WhereIsItCanLongitude
                            });
                    }
                    break;

                case "uAll":
                    SelectedColIdsWithCoordinatesData = new Dictionary<int, CoordinatesData>();
                    break;

                case "sPage":
                    for (int i = GridView.VisibleStartIndex; i < GridView.VisibleStartIndex + GridView.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(GridView.GetRowValues(i, "Col_Id"));
                        Col currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == colId);
                        if (currentCol != default(Col) && !SelectedColIdsWithCoordinatesData.ContainsKey(colId))
                            SelectedColIdsWithCoordinatesData.Add(colId, new CoordinatesData()
                            {
                                CurrentLatitude = currentCol.WhereIsItCanLatitude,
                                CurrentLongitude = currentCol.WhereIsItCanLongitude,
                                WhereIsItState = currentCol.WhereIsItState.ToString(),
                                InfoboxTitle = GetColInfoboxTitle(currentCol),
                                InfoboxDescription = GetColInfoboxDescription(currentCol),
                                OverlappingNumber = 0,
                                OriginalLatitude = currentCol.WhereIsItCanLatitude,
                                OriginalLongitude = currentCol.WhereIsItCanLongitude
                            });
                    }
                    break;

                case "uPage":
                    for (int i = GridView.VisibleStartIndex; i < GridView.VisibleStartIndex + GridView.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(GridView.GetRowValues(i, "Col_Id"));
                        if (SelectedColIdsWithCoordinatesData.ContainsKey(colId))
                            SelectedColIdsWithCoordinatesData.Remove(colId);
                    }
                    break;

                case "sRow":
                    for (int i = GridView.VisibleStartIndex; i < GridView.VisibleStartIndex + GridView.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(GridView.GetRowValues(i, "Col_Id"));
                        Col currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == colId);
                        if (GridView.Selection.IsRowSelected(i) && !SelectedColIdsWithCoordinatesData.ContainsKey(colId) && currentCol != default(Col))
                            SelectedColIdsWithCoordinatesData.Add(colId, new CoordinatesData()
                            {
                                CurrentLatitude = currentCol.WhereIsItCanLatitude,
                                CurrentLongitude = currentCol.WhereIsItCanLongitude,
                                WhereIsItState = currentCol.WhereIsItState.ToString(),
                                InfoboxTitle = GetColInfoboxTitle(currentCol),
                                InfoboxDescription = GetColInfoboxDescription(currentCol),
                                OverlappingNumber = 0,
                                OriginalLatitude = currentCol.WhereIsItCanLatitude,
                                OriginalLongitude = currentCol.WhereIsItCanLongitude
                            });
                    }

                    break;

                case "uRow":

                    for (int i = GridView.VisibleStartIndex; i < GridView.VisibleStartIndex + GridView.SettingsPager.PageSize; i++)
                    {
                        var colId = Convert.ToInt32(GridView.GetRowValues(i, "Col_Id"));
                        if (!GridView.Selection.IsRowSelected(i) && SelectedColIdsWithCoordinatesData.ContainsKey(colId))
                            SelectedColIdsWithCoordinatesData.Remove(colId);
                    }

                    break;
            }

            // si ritorna quanto selezionato
            if (((ASPxCallback)source).JSProperties.ContainsKey("cpSelectedColsWithCoordinates"))
                ((ASPxCallback)source).JSProperties.Add("cpSelectedColsWithCoordinates", null);
            {

                // prima di effettuare qualsiasi tipo di elaborazione si ripristinano tutte le coordinate originali così da evitare problemi in sovrapposizione di elementi successivi
                foreach (KeyValuePair<int, CoordinatesData> coordinatesData in SelectedColIdsWithCoordinatesData)
                {
                    coordinatesData.Value.CurrentLatitude = coordinatesData.Value.OriginalLatitude;
                    coordinatesData.Value.CurrentLongitude = coordinatesData.Value.OriginalLongitude;
                }

                // prima di ritornare quanto selezionato si verificano sovrapposizioni di cantiere: in questo caso si sposta di qualche metro il pushpin per visualizzare le sovrapposizioni
                var newColDatas = new Dictionary<int, CoordinatesData>();
                foreach (KeyValuePair<int, CoordinatesData> colCoordinatesData in SelectedColIdsWithCoordinatesData)
                {
                    // si recuperano tutti gli elementi presenti con le stesse coordinate
                    IEnumerable<KeyValuePair<int, CoordinatesData>> elementWithSameCoordinates = SelectedColIdsWithCoordinatesData.Where(coordinateData => coordinateData.Value.CurrentLatitude == colCoordinatesData.Value.CurrentLatitude && coordinateData.Value.CurrentLongitude == colCoordinatesData.Value.CurrentLongitude);

                    if (!newColDatas.ContainsKey(colCoordinatesData.Key))
                        newColDatas.Add(colCoordinatesData.Key, new CoordinatesData()
                        {
                            CurrentLatitude = colCoordinatesData.Value.CurrentLatitude,
                            CurrentLongitude = colCoordinatesData.Value.CurrentLongitude,
                            WhereIsItState = colCoordinatesData.Value.WhereIsItState,
                            InfoboxTitle = colCoordinatesData.Value.InfoboxTitle,
                            InfoboxDescription = colCoordinatesData.Value.InfoboxDescription,
                            OverlappingNumber = elementWithSameCoordinates.Count(),
                            OriginalLatitude = colCoordinatesData.Value.OriginalLatitude,
                            OriginalLongitude = colCoordinatesData.Value.OriginalLongitude
                        });

                    // se sono presenti più elementi oltre il presente
                    if (elementWithSameCoordinates.Count() > 1)
                    {
                        // allora per ogni elemento sovrapposto si aggiunge quache metro alle coordinate
                        const double toAddValuesStep = 0.00001d;
                        double toAddValues = toAddValuesStep;

                        foreach (KeyValuePair<int, CoordinatesData> elementWithSameCoordinate in elementWithSameCoordinates)
                        {
                            if (!newColDatas.ContainsKey(elementWithSameCoordinate.Key) && elementWithSameCoordinate.Key != colCoordinatesData.Key)
                                newColDatas.Add(elementWithSameCoordinate.Key, new CoordinatesData()
                                {
                                    CurrentLatitude = elementWithSameCoordinate.Value.CurrentLatitude + toAddValues,
                                    CurrentLongitude = elementWithSameCoordinate.Value.CurrentLongitude + toAddValues,
                                    WhereIsItState = elementWithSameCoordinate.Value.WhereIsItState,
                                    InfoboxTitle = elementWithSameCoordinate.Value.InfoboxTitle,
                                    InfoboxDescription = elementWithSameCoordinate.Value.InfoboxDescription,
                                    OverlappingNumber = elementWithSameCoordinates.Count(),
                                    OriginalLatitude = elementWithSameCoordinate.Value.OriginalLatitude,
                                    OriginalLongitude = elementWithSameCoordinate.Value.OriginalLongitude

                                });

                            toAddValues = toAddValues + toAddValuesStep;    
                        }
                    }
                }

                foreach (KeyValuePair<int, CoordinatesData> colData in newColDatas)
                    SelectedColIdsWithCoordinatesData[colData.Key] = colData.Value;

                ((ASPxCallback)source).JSProperties["cpSelectedColsWithCoordinates"] = SelectedColIdsWithCoordinatesData.Where(item => Math.Abs(item.Value.CurrentLatitude) > 0.1d && Math.Abs(item.Value.CurrentLongitude) > 0.1d)
                    .Select(item => new
                    {
                        ColId = item.Key, 
                        Latitude = item.Value.CurrentLatitude.ToString(),
                        Longitude = item.Value.CurrentLongitude.ToString(),
                        State = item.Value.WhereIsItState,
                        InfoboxTitle = item.Value.InfoboxTitle,
                        InfoboxDescription = item.Value.InfoboxDescription,
                        PushpinText = item.Value.OverlappingNumber.ToString()
                    });
            }

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Effettua il bind della griglia recuperando i dati da database.
        /// </summary>
        private void BindGrid()
        {
            gvWhereIsIt.KeyFieldName = Keyfieldname;
            IQueryable<Col> currDataSource = Enumerable.Empty<Col>().AsQueryable();
            var emptyList = Enumerable.Empty<Col>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.ColRepo.GetAll(true).AsQueryable();
                
                gvWhereIsIt.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvWhereIsIt.DataSource = emptyList;

        }

        /// <summary>
        /// Calcola e restituisce la descrizione dell'infobox da applicare al pushpin a partire dal collaboratore specificato.
        /// </summary>
        /// <param name="col">Il collaboratore da cui estrarre le informazioni ritornate dal metodo.</param>
        /// <returns>La stringa da visualizzare nella descrizione dell'infobox.</returns>
        private string GetColInfoboxDescription(Col col)
        {
            // di default il metodo ritorna stringa vuota
            string infoboxDescription = String.Empty;

            // si procede solamente se il collaboratore passatoc come parametro risulta valorizzato
            if (col != default(Col))
            {
                string stateString = String.Empty;
                switch (col.WhereIsItState)
                {
                    case WhereIsItRecordState.IsInIt:
                        stateString = BusinessService.GetLocalizedString(PowerWebResources.STR_COL_IN_LOCO);
                        break;
                    case WhereIsItRecordState.IsOutOfIt:
                        stateString = BusinessService.GetLocalizedString(PowerWebResources.STR_COL_NON_IN_LOCO);
                        break;
                    case WhereIsItRecordState.LastPass:
                        stateString = BusinessService.GetLocalizedString(PowerWebResources.STR_COL_ULTIMO_PASSAGGIO);
                        break;
                    case WhereIsItRecordState.NoRegPresent:
                        stateString = BusinessService.GetLocalizedString(PowerWebResources.STR_NO_REG);
                        break;
                }
                infoboxDescription = String.Format("<span>{0} - {1}<br/>{2} {3}<br/>{4}</span>", col.WhereIsItCantCode, col.WhereIsItCantDes, col.WhereIsItDate.HasValue ? col.WhereIsItDate.Value.ToShortDateString() : DateTime.MinValue.ToShortDateString(), col.WhereIsItHour, stateString);
            }

            // ritorno del valore calcolato dal metodo
            return infoboxDescription;
        }

        /// <summary>
        /// Calcola e restituisce il titolo dell'infobox da applicare al pushpin a partire dal collaboratore specificato.
        /// </summary>
        /// <param name="col">Il collaboratore da cui estrarre le informazioni ritornate dal metodo.</param>
        /// <returns>La stringa da visualizzare nel titolo dell'infobox.</returns>
        private string GetColInfoboxTitle(Col col)
        {
            // di default il metodo ritorna stringa vuota
            string infoboxDescription = String.Empty;

            // si procede solamente se il collaboratore passatoc come parametro risulta valorizzato
            if (col != default(Col))
                infoboxDescription = String.Format("{0} - {1}", col.Codice_Collaboratore, col.CognomeNome_Col);

            // ritorno del valore calcolato dal metodo
            return infoboxDescription;
        }

        #endregion

        #region Module Data

        

        #endregion

    }
}