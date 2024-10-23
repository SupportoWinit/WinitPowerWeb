using Business;
using Business.BusinessExtension;
using Business.Repository;
using Common;
using DevExpress.Data.Linq;
using DevExpress.XtraReports.UI;
using Domain;
using Exports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reports;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Web.Services;
using Spire.Pdf;
using Spire.Xls;
using System.Drawing;
using DevExpress.XtraPrinting;

namespace PowerWeb.Pages
{
    public partial class CartellinoPage : BasePage
    {

        #region VARIABILI DI CLASSE

        private const string CustomExportNamespace = "Exports";

        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(CartellinoPage));

        //DataSource della griglia dei collaboratori
        private static DevExtremeLinqServerRepository<Col> dataSource = null;


        public static JObject _resources = null;
        public static JObject _motivazioni = null;
        public static JArray _exports = null;
        public static JArray _colColumns = null;
        public static bool _disabledColsCustomization = false;
        public static int _RespId = 0;

        public static List<Tab_Excel_Model> _pageExcelModels;

        public static JObject _cartellinoOptionsJSON = null;
        public static dynamic _cartellinoOptions;

        public static ExportContext _exportContext;

        #endregion

        #region EVENTI DI PAGINA
        protected void Page_Init(object sender, EventArgs e)
        {
            if (!Page.IsCallback && !Page.IsPostBack)
            {
                int id = PowerWebContext.Current.User.Utenti_Id;

                var utenti = RepoManager.Utenti_RespRepo.GetAll();

                utenti = utenti.Where(u=> u.Utenti_Id == id);

                if (utenti.Count() > 0)
                {
                    Utenti_Resp user = utenti.First(u =>u.Utenti_Id == id);
                    _RespId = user.Resp_Id;
                }
                else {
                    _RespId = 0;
                }

                var parameters = RepoManager.ParamRepo.First(true);

                dataSource = new DevExtremeLinqServerRepository<Col>(RepoManager.ColRepo);

                _exportContext = new ExportContext();

                #region OPZIONI CARTELLINO

                bool showRettifiche = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.UseCorrectionEnum) == (int)UseCorrectionEnum.Use;
                string rettificaLimits = "";
                if (showRettifiche)
                {
                    rettificaLimits += RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.UseCorrectionEnum, "DefaultMinValue");
                    rettificaLimits += "|";
                    rettificaLimits += RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.UseCorrectionEnum, "DefaultMaxValue");
                }
                _cartellinoOptions = new ExpandoObject();

                _cartellinoOptions.rettificaLimits = rettificaLimits;
                _cartellinoOptions.devidePlanByDayNight = parameters.Cartellino_Divisione_Piano_Notturno_Diurno;
                _cartellinoOptions.showPiano = parameters.Cartellino_Visualizza_Piano;
                _cartellinoOptions.showOre = parameters.Cartellino_Visualizza_Ore;
                _cartellinoOptions.showJust = parameters.Cartellino_Visualizza_Motivazioni;
                _cartellinoOptions.showDelta = parameters.Cartellino_Visualizza_Delta;
                _cartellinoOptions.showViaggi = parameters.Cartellino_Visualizza_Viaggi;
                _cartellinoOptions.showTotale = parameters.Cartellino_Visualizza_Totale;
                _cartellinoOptions.devideByOtherEntityAllowed = parameters.Cartellino_Abilita_Divisione_Cantiere; //Stabilisce se è abilitata la divisione per cantiere
                _cartellinoOptions.useEditableCartellino = parameters.Cartellino_Usa_Cartellino_Modificabile;
                _cartellinoOptions.totalInFirstColumn = parameters.Cartellino_Totale_Prima_Colonna;
                _cartellinoOptions.devideByOtherEntity = parameters.Cartellino_Dividi_Altra_Anagrafica;//Stabilisce se l'ultima volta era stato impostata la divisione per cantiere tramite apposito toggle
                _cartellinoOptions.insertCorrectionRow = parameters.Cartellino_Usa_Rettifiche_Cartellino;
                _cartellinoOptions.useMonteMinuti = parameters.Abilita_Monte_Minuti && parameters.Flag_Monte_Ore != (int)MothlyHoursEnum.None;
                _cartellinoOptions.showWeeklyTotals = Convert.ToBoolean(parameters.Cartellino_Visualizza_Totali_Settimanali);
                _cartellinoOptions.showRettifiche = showRettifiche;
                _cartellinoOptions.rettificaLimits = rettificaLimits;
                _cartellinoOptions.autoRettifiche = parameters.Cartellino_Usa_Rettifiche_Auto;
                _cartellinoOptions.manualRettifiche = parameters.Cartellino_Usa_Rettifiche_Manuali;
                _cartellinoOptions.editableMode = parameters.Cartellino_Abilita_Modifica;


                _cartellinoOptionsJSON = JObject.FromObject(_cartellinoOptions);


                _disabledColsCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AvoidDisabledCartellinoCols) == 1;

                #endregion

                #region MOTIVAZIONI

                _motivazioni = JObject.FromObject(RepoManager.Tab_DecodRepo.DbSet.AsNoTracking().Where(r => r.Nome_Tab == "MOTIVAZIONI").Select(x => new { Key = x.Chiave_Tab, Value = x.Decodifica_Tab }).ToDictionary(c => c.Key, c => c.Value));

                #endregion

                #region RESOURCES

                _resources = JObject.FromObject(RepoManager.ResourcesRepo.ResourcesDictionary);

                #endregion

                #region STAMPA 

                var printTmp = RepoManager.Tab_Excel_ModelRepo.Find(tem => tem.IsActive && tem.Pagina == "Cart", true).ToList();
                printTmp.ForEach(p =>
                {
                    p.Nome_Risorsa = BusinessService.GetLocalizedString(p.Nome_Risorsa);
                });
                _exports = JArray.FromObject(printTmp);

                _pageExcelModels = RepoManager.Tab_Excel_ModelRepo.Find(tem => tem.IsActive == true && tem.Pagina == "Cart", true).ToList();

                #endregion

                #region COLONNE GRIGLIA COLLABORATORI

                _colColumns = new JArray();

                JObject colonna = null;

                String field = null;
                colonna = new JObject();
                colonna.Add("dataField", "Col_Id");
                colonna.Add("visible", false);
                colonna.Add("caption", "Col_Id");
                _colColumns.Add(colonna);

                field = RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_CODICE_COLLABORATORE");
                colonna = new JObject();
                colonna.Add("dataField", "Codice_Collaboratore");
                colonna.Add("caption", field);
                colonna.Add("visible", true);
                _colColumns.Add(colonna);

                field = RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_COGNOME_COL");
                colonna = new JObject();
                colonna.Add("dataField", "Cognome_Col");
                colonna.Add("caption", field);
                colonna.Add("visible", true);
                _colColumns.Add(colonna);

                field = RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_NOME_COL");
                colonna = new JObject();
                colonna.Add("dataField", "Nome_Col");
                colonna.Add("caption", field);
                colonna.Add("visible", true);
                _colColumns.Add(colonna);

                field = RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_DISABILITAZIONE_COL");
                colonna = new JObject();
                colonna.Add("dataField", "DisAbilitazione_Col");
                colonna.Add("caption", field);
                colonna.Add("visible", true);
                colonna.Add("allowHeaderFiltering", true);
                colonna.Add("falseText", "No");
                colonna.Add("trueText", "Sì");
                _colColumns.Add(colonna);

                field = RepoManager.ResourcesRepo.GetResourcesDictionaryString("FLD_GRUPPO_TAB");
                colonna = new JObject();
                colonna.Add("dataField", "Resp_Id");
                colonna.Add("caption", field);
                colonna.Add("visible", true);
                _colColumns.Add(colonna);

                field = "Ultimo Elaborato";
                colonna = new JObject();
                colonna.Add("dataField", "Data_Ultimo_Cartellino_Elaborato");
                colonna.Add("caption", field);
                colonna.Add("visible", true);
                colonna.Add("dataType", "date");
                colonna.Add("format", "monthAndYear");
                JObject obj = new JObject();
                obj.Add("groupInterval", "month");
                colonna.Add("headerFilter", obj);
                _colColumns.Add(colonna);

                field = "Ultimo Esportato";
                colonna = new JObject();
                colonna.Add("dataField", "Data_Ultimo_Cartellino_Esportato");
                colonna.Add("caption", field);
                colonna.Add("visible", true);
                colonna.Add("dataType", "date");
                colonna.Add("format", "monthAndYear");
                JObject obj2 = new JObject();
                obj2.Add("groupInterval", "month");
                colonna.Add("headerFilter", obj2);
                _colColumns.Add(colonna);

                #endregion

            }

        }
        #endregion

        #region WEBMETHODS

        [WebMethod]
        public static string ElaborateCartellino(int[] selectedCollab, DateTime selectedPickerDate, object cartellinoOptions)
        {
            #region PARAMETRI

            dynamic optionsObj = System.Web.Helpers.Json.Decode(JsonConvert.SerializeObject(cartellinoOptions));

            #endregion

            #region STRUTTURE DATI 

            string errorMessage = "";

            JObject dataSource = new JObject();
            JArray cartelliniDataSource = new JArray();

            #endregion

            Param parameters = RepoManager.ParamRepo.ParametersRow;

            List<Col> collaboratori = RepoManager.ColRepo.Find(c => selectedCollab.Contains(c.Col_Id)).ToList();
            bool str = false;
            if (parameters.Cartellino_Usa_Cartellino_Modificabile)
            {
                str = true;
                
            }

            try
            {
                int index = 0;
                DateTime startMonth = CommonService.GetFirstMonthDay(selectedPickerDate);
                DateTime endMonth = CommonService.GetLastMonthDay(selectedPickerDate);
                foreach (Col col in collaboratori)
                {
                    IEnumerable<int> lis = new List<int>();
                    lis = RepoManager.RegRepo.GetRegsIdByDateRangeByColNotBlocked(startMonth, endMonth, col.Col_Id);
                    if (lis.Count() > 0 || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CollabNoHours) == 1)
                    {
                        bool weekly = true;
                        if (parameters.Cartellino_Visualizza_Totali_Settimanali == 0)
                        {
                            weekly = false;
                        }
                        Dictionary<string, List<TimesheetModuleItem>> cartelliniRetrieved = TimesheetModuleItem.GenerateCartellino(selectedPickerDate, col, isByOtherEntity: optionsObj.devideByOtherEntity,
                                                                                                                               isDecimalHours: false, showPiano: optionsObj.showPiano, calculateDelta: optionsObj.showDelta, calculateOrdStrTimesheet: str, calculateJustifications: parameters.Cartellino_Visualizza_Motivazioni, showWeeklyTotal: weekly);

                        JObject serCartellino = SerializeCartellino(cartelliniRetrieved, col, selectedPickerDate, optionsObj, index);

                        cartelliniDataSource.Add(serCartellino);

                        #region CARTELLINI PER STRAORDINARIO/NOTTURNO




                        #endregion

                        index++;
                    }
                        
                }
            }

            catch (Exception ex)
            {
                _log.ErrorFormat("{0} - {1}", "Si è verirficato un errore nell'elaborazione dei cartellini", ex.ToString());
                errorMessage = "Si è verirficato un errore nell'elaborazione dei cartellini";
                dataSource.RemoveAll();
                dataSource.Add("errorMessage", errorMessage);

                JsonConvert.SerializeObject(dataSource);
            }

            #region AGGIORNAMENTO DATA ELABORAZIONE COLLABORATORE

            JArray elaboratedColsDates = new JArray();

            foreach (Col processingCol in collaboratori)
            {

                if (processingCol.Data_Ultimo_Cartellino_Elaborato == null || processingCol.Data_Ultimo_Cartellino_Elaborato < selectedPickerDate)
                {
                    processingCol.Data_Ultimo_Cartellino_Elaborato = selectedPickerDate;

                }
            }
            RepoManager.ColRepo.SaveChanges();

            dataSource.Add("dataSource", cartelliniDataSource);
            dataSource.Add("columns", SerializeColumns(selectedPickerDate, optionsObj));

            #endregion


            return JsonConvert.SerializeObject(dataSource);
        }

        /// <summary>
        /// Costruzione del json colonne.
        /// </summary>
        /// <param name="date">Data di inizio.</param>
        /// <param name="options">Opzione del cartellino.</param>
        /// <returns></returns>
        private static JArray SerializeColumns(DateTime date, dynamic options)
        {
            JArray columns = new JArray();

            DateTime startDate = date.Date; //Primo giorno del mese

            DateTime endDate = CommonService.GetLastMonthDay(date); //Ultimo giorno del mese

            int weekEndNumber = 1;

            if (options.showWeeklyTotals && startDate.Date == CommonService.GetFirstMonthDay(startDate) && startDate.DayOfWeek != DayOfWeek.Monday)
                startDate = CommonService.GetLastDayOfWeekInMonth(startDate.AddMonths(-1), DayOfWeek.Monday);

            // se è richiesto il piano per la gestione degli orari settimanali, la data di fine è la fine del mese e non si tratta di una domenica
            // allora si recupera la data di fine periodo la prima domenica del mese successivo
            if (options.showWeeklyTotals && endDate.Date == CommonService.GetLastMonthDay(endDate) && endDate.DayOfWeek != DayOfWeek.Sunday)
                endDate = CommonService.GetFirstDayOfWeekInMonth(endDate.AddMonths(1), DayOfWeek.Sunday);

            //Colonna chiave
            JObject column = JObject.FromObject(new
            {
                dataField = "codice",
                caption = "Tipo",
                @fixed = true,
                visibleIndex = 0,
                allowEditing = false,
                allowGrouping = false,
                allowSorting = false
            });

            columns.Add(column);

            if (options.devideByOtherEntity)
            {
                column = JObject.FromObject(new
                {
                    dataField = "Cant_Id",
                    caption = "Cantiere",
                    allowEditing = false,
                    allowGrouping = false,
                    allowSorting = false,
                    visible = false
                });

                columns.Add(column);

                column = JObject.FromObject(new
                {
                    dataField = "CantDesc",
                    caption = "Cantiere",
                    allowEditing = false,
                    allowGrouping = false,
                    allowSorting = false,
                    groupIndex = 0,
                });

                columns.Add(column);
            }

            foreach (DateTime singleDay in CommonService.EachDay(startDate, endDate))
            {
                //Per ogni giorno del periodo richiesto viene costruita una colonna
                column = JObject.FromObject(new
                {
                    dataField = singleDay.ToShortDateString(),
                    caption = String.Format("{0} {1}", singleDay.DayOfWeek.ToString().First(), singleDay.Day)
                });

                #region LARGHEZZA COLONNA

                if (RepoManager.ParamRepo.ParametersRow.Cartellino_Modalita_Compatta)
                    column.Add("width", 50);
                else
                    column.Add("width", 80);

                #endregion

                if (singleDay.DayOfWeek == DayOfWeek.Sunday)
                {
                    column.Add("cssClass", "weekendCell");
                }

                columns.Add(column);

                #region TOTALI SETTIMANALI

                if (singleDay.DayOfWeek == DayOfWeek.Sunday && options.showWeeklyTotals)
                {
                    //Se il totale settimanale è attivo aggiungo anche una colonna per il totale
                    int totalWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(singleDay, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

                    column = JObject.FromObject(new
                    {
                        dataField = "WeekTotal" + weekEndNumber,
                        caption = String.Format("{0} {1}", "Settimana", totalWeek),
                        allowEditing = false,
                        width = 85
                    });


                    weekEndNumber++;

                    columns.Add(column);
                }

                #endregion

            };

            //Aggiungo la colonna del totale
            column = JObject.FromObject(new
            {
                dataField = "TotalDays",
                caption = "Totale Giorni",
                cssClass = "totalCell",
                alignment = "center",
                allowEditing = false,
                allowGrouping = false,
                allowSorting = false,
            });


            columns.Add(column);

            column = JObject.FromObject(new
            {
                dataField = "TotalHours",
                caption = "Totale Ore",
                cssClass = "totalCell",
                alignment = "center",
                allowEditing = false,
                allowGrouping = false,
                allowSorting = false,

            });

            if (_cartellinoOptions.totalInFirstColumn)
            {
                column.Add("visibleIndex", 1);
            }


            columns.Add(column);

            return columns;
        }

        /// <summary>
        /// Serializza il cartellino
        /// </summary>
        /// <param name="cartellino">Cartellino elaborato.</param>
        /// <param name="col">Il collaboratore a cui è associato.</param>
        /// <param name="date">Data inizio elaborazione.</param>
        /// <param name="options">Opzioni cartellino.</param>
        /// <returns></returns>
        private static JObject SerializeCartellino(Dictionary<string, List<TimesheetModuleItem>> cartellino, Col col, DateTime date, dynamic options, int index)
        {
            JObject result = new JObject();

            JArray cartellinoSer = new JArray();

            result.Add("ID", index);
            result.Add("ColId", col.Col_Id);
            result.Add("ColName", col.Cognome_Col);
            result.Add("ColSurname", col.Nome_Col);


            if (options.useMonteMinuti)
            {
                double rowMonteMin = cartellino.First().Value.Last().LastMonthlyMinutes;

                TimeSpan monteMin = TimeSpan.FromMinutes(rowMonteMin);

                int totalHours = Math.Abs((int)monteMin.TotalHours);
                int totalMinutes = Math.Abs((int)monteMin.Minutes);

                result.Add("riportoOrePrecedenti", String.Format("{0}{1}:{2}", (monteMin < TimeSpan.Zero ? "-" : ""), totalHours.ToString("00"), totalMinutes.ToString("00")));
            }

            //Per ogni riga del cartellino formatto i valori e li adatto al json
            cartellino.First().Value.ForEach(cartRow =>
            {
                DateTime currentDate = date;

                JObject rowObject = JObject.FromObject(cartRow);

                JObject row = new JObject();

                Tab_Decod motivazione = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", cartRow.Justification);

                row.Add("justification", cartRow.Justification);
                row.Add("codice", motivazione != null ? motivazione.Decodifica_Tab : cartRow.Justification);

                if (options.devideByOtherEntity)
                {
                    row.Add("Cant_Id", cartRow.CantId);
                    row.Add("CantDesc", cartRow.CantDesc);
                }

                int weekendNumber = 1;

                TimeSpan total = new TimeSpan();

                foreach (var day in cartRow.DaysHours)
                {
                    JObject dayVal = new JObject();
                    DateTime dataField = DateTime.MinValue;

                    if (day.Key < 0)
                        currentDate = date.AddDays(day.Key).Date;
                    else if (day.Key > 100)
                        currentDate = date.AddMonths(1).AddDays(day.Key - 101).Date;
                    else
                        currentDate = date.AddDays(day.Key - 1).Date;

                    dataField = currentDate;

                    string duration = "-";

                    if (TimeSpan.FromMinutes(day.Value.Item1) != TimeSpan.Zero)
                        duration = (day.Value.Item1 < 0 ? "-" : "") + TimeSpan.FromMinutes(day.Value.Item1).ToString("hh\\:mm");

                    if (dataField.DayOfWeek == DayOfWeek.Monday)
                    {
                        total = new TimeSpan();
                        total = total.Add(TimeSpan.FromMinutes(day.Value.Item1));
                    }
                    else
                    {
                        total = total.Add(TimeSpan.FromMinutes(day.Value.Item1));
                    }

                    row.Add(dataField.ToShortDateString(), duration);

                    if (dataField.DayOfWeek == DayOfWeek.Sunday && options.showWeeklyTotals) //Aggiunge il weekend
                    {
                        int totalWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dataField, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

                        var totalString = rowObject[$"TotalWeek{weekendNumber}"].ToString();

                        string durationWeek = "";

                        var durationTime = TimeSpan.FromHours(double.Parse(totalString));

                        string minutes = total.Minutes.ToString("00");
                        if (total.Minutes < 0) {
                            minutes = minutes.Remove(0,1);
                        }

                        string duration1 = "" + Math.Floor(total.TotalHours).ToString("00") + ":" + total.Minutes.ToString("00");

                        if (durationTime != TimeSpan.Zero)
                            durationWeek = $"{(durationTime < TimeSpan.Zero ? "" : "")}" + duration1;



                        row.Add("WeekTotal" + weekendNumber, durationWeek);

                        weekendNumber++;
                    }

                }

                row.Add("TotalDays", cartRow.TotalDays);

                string totalHours = "-";

                if (cartRow.TotalMinutes != 0)
                    totalHours = ((cartRow.TotalMinutes < 0) ? "-" : "") + Math.Abs((int)TimeSpan.FromMinutes(cartRow.TotalMinutes).TotalHours).ToString("00") + ":" + Math.Abs(TimeSpan.FromMinutes(cartRow.TotalMinutes).Minutes).ToString("00");

                row.Add("TotalHours", totalHours);
                cartellinoSer.Add(row);
            });

            if (cartellino.Count > 1)
            {
                JArray editableCartArray = new JArray();

                cartellino.Last().Value.ForEach(cartRow =>
                {

                    DateTime currentDate = date;

                    JObject rowObject = JObject.FromObject(cartRow);

                    JObject row = new JObject();

                    Tab_Decod motivazione = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", cartRow.Justification);

                    row.Add("justification", cartRow.Justification);
                    row.Add("codice", motivazione != null ? motivazione.Decodifica_Tab : cartRow.Justification);

                    int weekendNumber = 1;

                    foreach (var day in cartRow.DaysHours)
                    {
                        JObject dayVal = new JObject();
                        DateTime dataField = DateTime.MinValue;

                        if (day.Key < 0)
                            currentDate = date.AddDays(day.Key).Date;
                        else if (day.Key > 100)
                            currentDate = date.AddMonths(1).AddDays(day.Key - 101).Date;
                        else
                            currentDate = date.AddDays(day.Key - 1).Date;

                        dataField = currentDate;

                        string duration = "-";

                        if (TimeSpan.FromMinutes(day.Value.Item1) != TimeSpan.Zero)
                            duration = (day.Value.Item1 < 0 ? "-" : "") + TimeSpan.FromMinutes(day.Value.Item1).ToString("hh\\:mm");

                        row.Add(dataField.ToShortDateString(), duration);

                        if (dataField.DayOfWeek == DayOfWeek.Sunday && options.showWeeklyTotals) //Aggiunge il weekend
                        {
                            int totalWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dataField, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

                            var totalString = rowObject[$"TotalWeek{weekendNumber}"].ToString();

                            string durationWeek = "-";

                            var durationTime = TimeSpan.FromHours(double.Parse(totalString));

                            if (durationTime != TimeSpan.Zero)
                                durationWeek = $"{(durationTime < TimeSpan.Zero ? "-" : "")}{Math.Floor(durationTime.TotalHours).ToString("00")}:{durationTime.Minutes.ToString("00")}";



                            row.Add("WeekTotal" + weekendNumber, durationWeek);

                            weekendNumber++;
                        }

                    }

                    row.Add("TotalDays", cartRow.TotalDays);

                    string totalHours = "-";

                    if (cartRow.TotalMinutes != 0)
                        totalHours = ((cartRow.TotalMinutes < 0) ? "-" : "") + Math.Abs((int)TimeSpan.FromMinutes(cartRow.TotalMinutes).TotalHours).ToString("00") + ":" + Math.Abs(TimeSpan.FromMinutes(cartRow.TotalMinutes).Minutes).ToString("00");

                    row.Add("TotalHours", totalHours);
                    editableCartArray.Add(row);
                });

                result.Add("dataSourceEditable", editableCartArray);
            }

            result.Add("dataSource", cartellinoSer);

            return result;
        }

        [WebMethod]
        public static string SaveCartellinoPreferences(object parameters)
        {
            string errorMessage = "OK";

            IDictionary<string, object> newParams = (IDictionary<string, object>)parameters;

            try
            {
                //Recupera le opzioni del cartellino

                var DBparameters = RepoManager.ParamRepo.DbSet.First();

                DBparameters.Cartellino_Divisione_Piano_Notturno_Diurno = Convert.ToBoolean(newParams["devidePlanByDayNight"]);
                DBparameters.Cartellino_Visualizza_Piano = Convert.ToBoolean(newParams["showPiano"]);
                DBparameters.Cartellino_Visualizza_Ore = Convert.ToBoolean(newParams["showOre"]);
                DBparameters.Cartellino_Visualizza_Motivazioni = Convert.ToBoolean(newParams["showJust"]);
                DBparameters.Cartellino_Visualizza_Delta = Convert.ToBoolean(newParams["showDelta"]);
                DBparameters.Cartellino_Visualizza_Viaggi = Convert.ToBoolean(newParams["showViaggi"]);
                DBparameters.Cartellino_Visualizza_Totale = Convert.ToBoolean(newParams["showTotale"]);
                DBparameters.Cartellino_Totale_Prima_Colonna = Convert.ToBoolean(newParams["totalInFirstColumn"]);
                DBparameters.Cartellino_Dividi_Altra_Anagrafica = Convert.ToBoolean(newParams["devideByOtherEntity"]);
                DBparameters.Cartellino_Usa_Rettifiche_Cartellino = Convert.ToBoolean(newParams["insertCorrectionRow"]);
                DBparameters.Cartellino_Visualizza_Totali_Settimanali = Convert.ToInt32(Convert.ToBoolean(newParams["showWeeklyTotals"]));
                RepoManager.ParamRepo.SaveChanges();


                //Salva le opzioni a database
                _cartellinoOptions.devidePlanByDayNight = DBparameters.Cartellino_Divisione_Piano_Notturno_Diurno;
                _cartellinoOptions.showPiano = DBparameters.Cartellino_Visualizza_Piano;
                _cartellinoOptions.showOre = DBparameters.Cartellino_Visualizza_Ore;
                _cartellinoOptions.showJust = DBparameters.Cartellino_Visualizza_Motivazioni;
                _cartellinoOptions.showDelta = DBparameters.Cartellino_Visualizza_Delta;
                _cartellinoOptions.showViaggi = DBparameters.Cartellino_Visualizza_Viaggi;
                _cartellinoOptions.showTotale = DBparameters.Cartellino_Visualizza_Totale;
                _cartellinoOptions.devideByOtherEntityAllowed = DBparameters.Cartellino_Abilita_Divisione_Cantiere; //Stabilisce se è abilitata la divisione per cantiere
                _cartellinoOptions.useEditableCartellino = DBparameters.Cartellino_Usa_Cartellino_Modificabile;
                _cartellinoOptions.totalInFirstColumn = DBparameters.Cartellino_Totale_Prima_Colonna;
                _cartellinoOptions.devideByOtherEntity = DBparameters.Cartellino_Dividi_Altra_Anagrafica;//Stabilisce se l'ultima volta era stato impostata la divisione per cantiere tramite apposito toggle
                _cartellinoOptions.insertCorrectionRow = DBparameters.Cartellino_Usa_Rettifiche_Cartellino;
                _cartellinoOptions.useMonteMinuti = DBparameters.Abilita_Monte_Minuti && DBparameters.Flag_Monte_Ore != (int)MothlyHoursEnum.None;
                _cartellinoOptions.showWeeklyTotals = Convert.ToBoolean(DBparameters.Cartellino_Visualizza_Totali_Settimanali);
                _cartellinoOptions.autoRettifiche = DBparameters.Cartellino_Usa_Rettifiche_Auto;
                _cartellinoOptions.manualRettifiche = DBparameters.Cartellino_Usa_Rettifiche_Manuali;


            }

            catch (Exception ex)
            {
                _log.Error(String.Format("{0} - {1}", "C'è stato un problema con il salvataggio delle preferenze del cartellino", ex.ToString()));
                errorMessage = "FAIL";
            }

            return errorMessage;
        }
        [WebMethod]
        public static string SaveEditCartellino(object[] editCartellino, int colId, DateTime selectedDate)
        {
            string esito = "OK";

            if (editCartellino != null)
            {

                //Recupera dall'hidden field il JSON dei cartellini salvati
                JArray editCartellinoToSave = JArray.FromObject(editCartellino);

                string justification = "", hours = "", errorMessage = "";
                Timesheet timesheet;
                bool isNew = false;
                Col col = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colId);

                try
                {
                    if (col != default(Col))
                    {
                        foreach (JObject obj in editCartellinoToSave)
                        {
                            //Recupare il nome della risorsa dalla Tab_Decod
                            string just = (string)obj["Codice"];
                            justification = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Decodifica_Tab == just).Chiave_Tab;
                            //Recupera l'eventuale timesheet da aggiornare
                            timesheet = RepoManager.TimesheetRepo.SingleOrDefault(t => t.ColId == colId && t.Month == selectedDate && t.Justification == justification);

                            isNew = false;
                            //Se il cartellino non è presente, ne inizializza uno nuovo
                            if (timesheet == default(Timesheet))
                            {
                                timesheet = new Timesheet();
                                timesheet.ColId = colId;
                                timesheet.Month = selectedDate;
                                timesheet.Justification = justification;
                                isNew = true;
                            }

                            for (int i = 1, lastDay = CommonService.GetLastMonthDay(selectedDate).Day; i <= lastDay; i++)
                            {
                                hours = ((string)obj[String.Format("{0}/{1}/{2}", i, selectedDate.Month, selectedDate.Year)]).Trim();
                                //Se l'ora:minuti è stata inserita con il '.' come separatore, viene trasformato in ':'
                                if (hours.Contains('.'))
                                {
                                    hours = hours.Replace('.', ':');
                                }
                                //Se è stata inserita solo l'ora (senza minuti), aggiunge 00 minuti alla stringa
                                else
                                {
                                    //Se l'ora ha solo una cifra, aggiungo uno 0 
                                    if (hours.Length == 1)
                                    {
                                        hours = String.Concat("0", hours);
                                    }

                                    //A questo punto, se ho un valore tra 00 e 23, lo trasformo in ore
                                    if (hours.CompareTo("00") >= 0 && hours.CompareTo("24") < 0)
                                    {
                                        hours = String.Concat(hours, ":00");
                                    }
                                    //Altrimenti ho un valore non valido che non è stato intercettato dalla regex di validazione e viene spianato
                                    else
                                    {
                                        hours = "";
                                    }
                                }

                                TimeSpan updateTime = (hours.Equals("-") || hours.Equals(String.Empty) ? TimeSpan.Zero : TimeSpan.Parse(hours));
                                timesheet["Day" + i.ToString("00")] = updateTime;
                            }

                            //Se è nuovo lo aggiunge, altrimenti lo aggiorna
                            if (isNew)
                            {
                                RepoManager.TimesheetRepo.Add(timesheet);
                            }


                        }

                        RepoManager.TimesheetRepo.SaveChanges();

                        //Se il cartellino appena salvato è successivo a quello precedentemente elaborato, aggiorna la data
                        if (col.Data_Ultimo_Cartellino_Elaborato == null || col.Data_Ultimo_Cartellino_Elaborato < selectedDate)
                        {
                            col.Data_Ultimo_Cartellino_Elaborato = selectedDate;
                        }
                        RepoManager.ColRepo.SaveChanges();
                    }


                    else
                    {
                        esito = "FAIL";
                        errorMessage = "C'è stato un problema con il salvataggio del cartellino. Riprovare più tardi o contattare l'assistenza.";
                    }
                }

                catch (Exception ex)
                {
                    esito = "FAIL";
                    _log.Error(String.Format("{0} - {1}", "C'è stato un problema con il salvataggio del cartellino", ex.ToString()));
                    errorMessage = "C'è stato un problema con il salvataggio del cartellino . Riprovare più tardi o contattare l'assistenza.";
                }

                //In ogni caso (che sia andato tutto liscio o che ci sia stata un'eccezione)
                //riporto l'eventuale errore in modo che non ci siano arresti anomali
                finally
                {
                    //saveEditCartellino.JSProperties["cpErrorMessage"] = errorMessage;
                }
            }
            return esito;
        }
        [WebMethod]
        public static string DeleteCartellino(int[] selectedCollab, DateTime selectedPickerDate)
        {
            JObject response = new JObject();

            string esito = "OK";

            JArray newDatesArray = new JArray();
            IQueryable<Timesheet> colTimesheets;

            IEnumerable<Col> collaboratori = RepoManager.ColRepo.Find(c => selectedCollab.Contains(c.Col_Id)).ToList();

            try
            {
                foreach (Col col in collaboratori)
                {

                    //Per ogni collaboratore selezionato, rimuove i cartellini salvati per il mese indicato
                    RepoManager.TimesheetRepo.DeleteFromQuery(ts => ts.ColId == col.Col_Id && ts.Month == selectedPickerDate);
                    RepoManager.TimesheetRepo.SaveChanges();
                    colTimesheets = RepoManager.TimesheetRepo.Find(ts => ts.ColId == col.Col_Id).AsQueryable();

                    //Se quelli appena eliminati erano gli ultimi cartellini elaborati, porta la data di ultima elaborazione all'ultimo cartellino salvato o a NULL se non più presenti
                    if (col.Data_Ultimo_Cartellino_Elaborato == selectedPickerDate)
                    {
                        if (colTimesheets.Any())
                        {
                            col.Data_Ultimo_Cartellino_Elaborato = colTimesheets.Max(t => t.Month);
                        }

                        else
                        {
                            col.Data_Ultimo_Cartellino_Elaborato = null;
                        }

                        JObject newDateObj = new JObject();
                        newDateObj.Add("codice", col.Col_Id);
                        newDateObj.Add("date", col.Data_Ultimo_Cartellino_Elaborato);
                        newDatesArray.Add(newDateObj);
                    }
                }

                RepoManager.TimesheetRepo.SaveChanges();
                RepoManager.ColRepo.SaveChanges();

            }

            catch (Exception ex)
            {
                _log.Error(String.Format("{0} - {1}", "Si è verificato un errore nell'eliminazione dei cartellini", ex.ToString()));
                esito = "FAIL";
                //errorMessage = "Si è verificato un errore nell'eliminazione dei cartellini, riprovare più tardi o contattare l'assistenza";
            }

            response.Add("esito", esito);
            if (esito == "OK")
            {
                response.Add("newDates", newDatesArray);
            }

            return JsonConvert.SerializeObject(response);
        }
        [WebMethod]
        public static string SaveCartellinoCellUpdate(dynamic parameters)
        {

            #region VARIABILI RESPONSE

            JObject response = new JObject();

            List<string> errors = new List<string>();

            string esito = "OK";

            #endregion

            #region PARSING CANT

            int _Cant_Id = (parameters["Cant_Id"] != null) ? (int)parameters["Cant_Id"] : 0;

            Cant cant = RepoManager.CantRepo.FirstOrDefault(c => c.Cant_Id == _Cant_Id);

            #endregion

            #region PARSING COLLABORATORE

            int _Col_Id = (int)parameters["Col_Id"];

            Col col = RepoManager.ColRepo.DbSet.Find(_Col_Id);

            #endregion

            #region PARSING MOTIVAZIONE

            string _Codice = (string)parameters["Codice"];

            #endregion

            #region PARSING DATA


            DateTime _Date = DateTime.Parse((string)parameters["Date"]);

            #endregion

            #region PARSING DURATA

            string[] timeString = ((string)parameters["Time"]).Split(':');

            TimeSpan _Time = TimeSpan.Zero;

            if ((string)parameters["Time"] != "-" && (string)parameters["Time"] != "")
            {
                _Time = TimeSpan.Parse((string)parameters["Time"]);
            }

            #endregion

            #region PARSING DURATA PRECEDENTE

            string[] oldTimeString = ((string)parameters["OldTime"]).Split(':');

            TimeSpan _OldTime = TimeSpan.Zero;

            if ((string)parameters["OldTime"] != "-")
            {
                _OldTime = TimeSpan.Parse((string)parameters["OldTime"]);
            }

            #endregion

            #region PARSING DELTA PRECEDENTE

            string[] oldColumnDelta = ((string)parameters["OldColumnDelta"]).Split(':');

            TimeSpan _OldColumnDelta = TimeSpan.Zero;

            if ((string)parameters["OldColumnDelta"] != "-")
            {
                _OldColumnDelta = TimeSpan.Parse((string)parameters["OldColumnDelta"]);
            }
            #endregion

            #region PARSING TOTALE RIGA MOTIVAZIONE PRECEDENTE

            string[] oldRowTotalString = ((string)parameters["OldRowTotal"]).Split(':');

            TimeSpan _OldRowTotal = TimeSpan.Zero;

            if ((string)parameters["OldRowTotal"] != "-")
            {
                _OldRowTotal = TimeSpan.FromHours(int.Parse(oldRowTotalString.First())) + TimeSpan.FromMinutes(int.Parse(oldRowTotalString.Last()));
            }

            #endregion

            #region PARSING DELTA MENSILE MOTIVAZIONE PRECEDENTE

            string[] oldRowDeltaTotalString = ((string)parameters["OldRowDeltaTotal"]).Split(':');

            TimeSpan _OldRowDeltaTotal = TimeSpan.Zero;

            if ((string)parameters["OldRowDeltaTotal"] != "-")
            {
                _OldRowDeltaTotal = TimeSpan.FromHours(int.Parse(oldRowDeltaTotalString.First())) + TimeSpan.FromMinutes(int.Parse(oldRowDeltaTotalString.Last()));
            }

            #endregion

            #region PARSING TOTALE MENSILE MOTIVAZIONE PRECEDENTE

            string[] oldRowTotalTotalString = ((string)parameters["OldRowTotalTotal"]).Split(':');

            TimeSpan _OldRowTotalTotal = TimeSpan.Zero;

            if ((string)parameters["OldRowTotalTotal"] != "-")
            {
                _OldRowTotalTotal = TimeSpan.FromHours(int.Parse(oldRowTotalTotalString.First())) + TimeSpan.FromMinutes(int.Parse(oldRowTotalTotalString.Last()));
            }

            #endregion

            #region RETTIFICHE 

            if (_Codice == "Rettifiche Manu.")
            {

                try
                {

                    #region CANCELLAZIONE RETT.

                    //Se viene inserito il carattere di cancellazione delle rettifiche x quel giorno
                    //si cancellano le rettifiche presenti

                    if (_Time == TimeSpan.Zero)
                    {
                        try
                        {
                            if (cant != null)
                            {
                                RepoManager.RegRepo.DeleteFromQuery(r => r.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual && r.Registrazione_Data_Ora_Fis_Reg.Equals(_Date) && r.Col_Id == col.Col_Id && r.Cant_Id == cant.Cant_Id);
                            }
                            else
                            {
                                RepoManager.RegRepo.DeleteFromQuery(r => r.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual && r.Registrazione_Data_Ora_Fis_Reg.Equals(_Date) && r.Col_Id == col.Col_Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(string.Format("Errore durante la cancellazione delle rettifiche del collaboratore {0} con exception {1}", col.Codice_Collaboratore, ex.Message));
                            errors.Add(string.Format("Problemi con la cancellazione di una rettifica per il collaboratore {0}.", col.Codice_Collaboratore));
                        }
                    }

                    #endregion

                    #region INSERIMENTO RETTIFICA

                    else
                    {
                        CorrectionTypeEnum cte = CorrectionTypeEnum.CorrectionPlus;

                        #region CALCOLO MINUTI RETTIFICA

                        //Calcola i minuti di rettifica

                        if (_Time.TotalMinutes < 0)
                        {
                            cte = CorrectionTypeEnum.CorrectionMinus;
                            _Time = _Time.Negate();
                        }

                        #endregion

                        #region DELETE RETTIFICHE 

                        //Elimina le rettifiche presenti per il collaboratore-giorno-cantiere in questione
                        try
                        {
                            if (cant != null)
                            {
                                RepoManager.RegRepo.DeleteFromQuery(r => r.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual && r.Registrazione_Data_Ora_Fis_Reg.Equals(_Date) && r.Col_Id == col.Col_Id && r.Cant_Id == cant.Cant_Id);
                            }
                            else
                            {
                                RepoManager.RegRepo.DeleteFromQuery(r => r.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual && r.Registrazione_Data_Ora_Fis_Reg.Equals(_Date) && r.Col_Id == col.Col_Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(string.Format("Errore durante la cancellazione delle rettifiche per il collaboratore {0} con exception {1}", col.Codice_Collaboratore, ex.Message));
                            errors.Add(string.Format("Problemi con la cancellazione delle rettifiche per il collaboratore {0}.", col.Codice_Collaboratore));
                        }

                        #endregion

                        #region SCRITTURA RETTIFICA

                        //Se la rettifica è diversa da 0, genera la rettifica da generare
                        if (_Time.TotalMinutes != 0)
                        {
                            Reg retToAdd = RepoManager.RegRepo.GenerateManualRett(col.Col_Id, _Date, cte, _Time, (cant != null) ? cant.Cant_Id : 0);
                            RepoManager.RegRepo.Add(retToAdd);
                        }

                        RepoManager.RegRepo.SaveChanges();

                        #endregion

                    }

                    #endregion

                    #region  AGGIORNAMENTO TOTALE COLONNA

                    var newDayDur = TimeSpan.FromMinutes((double?)RepoManager.Reg_VRepo.DbSet.AsNoTracking().Where(x => x.Col_Id == col.Col_Id && x.Data_Reg == _Date).Sum(x => x.Durata_Fig) ?? 0.0);
                    response.Add("newColumnTotal", String.Format("{0}:{1}", newDayDur.Hours.ToString("00"), newDayDur.Minutes.ToString("00")));

                    #endregion

                    #region AGGIORNAMENTO DELTA COLONNA E TOTALI RIGHE

                    UpdateVisualFields(response, _OldTime, _Time, _OldColumnDelta, _OldRowTotal, _OldRowTotalTotal, _OldRowDeltaTotal);

                    #endregion

                }
                catch (Exception ex)
                {
                    errors.Add(string.Format("Problemi con il salvataggio delle rettifiche per il collaboratore {0}.", col.Codice_Collaboratore));
                    _log.ErrorFormat("Errore durante il salvataggio di una rettifica per il collaboratore {0} con exception {1}", col.Codice_Collaboratore, ex.Message);
                }
            }

            #endregion

            #region CAUSALI

            else
            {
                int motiv_Id = RepoManager.Tab_DecodRepo.First(m => m.Nome_Tab == "MOTIVAZIONI" && m.Chiave_Tab == _Codice).Tab_Decod_Id;

                #region CANCELLAZIONE CAUSALE

                if (_Time == TimeSpan.Zero)
                {

                    try
                    {

                        var regEU_Duration = RepoManager.Reg_VRepo.DbSet.Where(x => x.Col_Id == col.Col_Id && x.Registrazione_Tipo_Reg == 0 && x.Data_Ora_Fig_E != null && x.Data_Ora_Fig_U != null && x.Data_Reg == _Date).Select(x => x.Durata_Fis).Sum() ?? 0;
                        List<Reg_V> onlyDurRegs = RepoManager.Reg_VRepo.Find(x => x.Col_Id == col.Col_Id && x.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration && x.Data_Reg == _Date && x.Motivazione_Reg_Id == motiv_Id).ToList();

                        TimeSpan totalDayDuration = TimeSpan.FromMinutes((double)regEU_Duration);

                        //Se abbiamo delle reg di sola durata le cancelliamo
                        if (onlyDurRegs.Any())
                        {
                            onlyDurRegs.ForEach(regV =>
                            {
                                Reg toDelete = new Reg() { Reg_Id = regV.RegE };
                                RepoManager.RegRepo.Attach(toDelete);
                                RepoManager.RegRepo.Context.Entry(toDelete).State = System.Data.Entity.EntityState.Deleted;
                            });

                            RepoManager.RegRepo.SaveChanges();
                        }

                        //Se abbiamo delle registrazioni  E-U non le cancelliamo ma facciamo in modo di annullarle tramite una nuova reg-negativa
                        if (totalDayDuration - _Time != TimeSpan.Zero)
                        {
                            CorrectionTypeEnum cte = (totalDayDuration - _Time < TimeSpan.Zero) ? CorrectionTypeEnum.CorrectionPlus : CorrectionTypeEnum.CorrectionMinus;

                            Reg newCorrectionReg = RepoManager.RegRepo.GenerateCorrectionReg(col.Col_Id, _Date, CorrectionTypeEnum.CorrectionMinus, (totalDayDuration - _Time).Duration(), cant.Cant_Id);

                            RepoManager.RegRepo.Add(newCorrectionReg, true);
                        }

                        #region AGGIORNAMENTO TOTALE COLONNA

                        var newDayDur = TimeSpan.FromMinutes((double?)RepoManager.Reg_VRepo.DbSet.AsNoTracking().Where(x => x.Col_Id == col.Col_Id && x.Data_Reg == _Date).Sum(x => x.Durata_Fis) ?? 0.0);
                        response.Add("newColumnTotal", String.Format("{0}:{1}", newDayDur.TotalHours.ToString("00"), newDayDur.Minutes.ToString("00")));

                        #endregion

                        #region AGGIORNAMENTO DELTA COLONNA E TOTALI RIGHE

                        UpdateVisualFields(response, _OldTime, _Time, _OldColumnDelta, _OldRowTotal, _OldRowTotalTotal, _OldRowDeltaTotal);

                        #endregion


                    }
                    catch (Exception ex)
                    {
                        errors.Add("Errore interno durante la cancellazione di una registrazione");
                        esito = "FAIL";
                        _log.Error(String.Format("Errore durante la cancellazione di una registrazione dal cartellino con exception {0}", ex.Message));
                    }
                }

                #endregion

                #region INSERIMENTO CAUSALE

                else
                {


                    try
                    {


                        var regEU_Duration = RepoManager.Reg_VRepo.DbSet.AsNoTracking().Where(x => x.Col_Id == col.Col_Id && x.Registrazione_Tipo_Reg == 0 && x.Data_Ora_Fig_E != null && x.Data_Ora_Fig_U != null && x.Data_Reg == _Date && x.Motivazione_Reg_Id == motiv_Id).Select(x => x.Durata_Fis).Sum() ?? 0;
                        List<Reg_V> onlyDurRegs = RepoManager.Reg_VRepo.Find(x => x.Col_Id == col.Col_Id && x.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration && x.Data_Reg == _Date && x.Motivazione_Reg_Id == motiv_Id, true).ToList();

                        TimeSpan totalDayDuration = TimeSpan.FromMinutes((double)regEU_Duration);

                        //Se la durata delle ore complessive supera le 24 H ritorno un errore
                        if (totalDayDuration + _Time > TimeSpan.FromMinutes(1440))
                        {
                            errors.Add("Totale ore giornaliere superiori alla durata massima!");
                            esito = "FAIL";

                        }
                        else
                        {
                            //Se abbiamo registrazioni di sola durata le cancelliamo
                            if (onlyDurRegs.Any())
                            {
                                onlyDurRegs.ForEach(regV =>
                                {
                                    Reg toDelete = new Reg() { Reg_Id = regV.RegE };
                                    RepoManager.RegRepo.Attach(toDelete);
                                    RepoManager.RegRepo.Context.Entry(toDelete).State = System.Data.Entity.EntityState.Deleted;
                                });

                                RepoManager.RegRepo.SaveChanges();
                            }

                            //Se abbiamo registrazioni E-U con motivazione ne generiamo una di correzione 
                            if (totalDayDuration - _Time != TimeSpan.Zero)
                            {
                                CorrectionTypeEnum cte = (totalDayDuration - _Time < TimeSpan.Zero) ? CorrectionTypeEnum.CorrectionPlus : CorrectionTypeEnum.CorrectionMinus;

                                Reg newCorrectionReg = RepoManager.RegRepo.GenerateDurationReg(col.Col_Id, _Date, cte, (totalDayDuration - _Time).Duration(), cant.Cant_Id, motiv_Id);
                                RepoManager.RegRepo.Add(newCorrectionReg, true);
                                RepoManager.RegRepo.SaveChanges();
                            }

                            #region AGGIORNAMENTO TOTALE COLONNA
                            var newDayDur = TimeSpan.FromMinutes((double?)RepoManager.Reg_VRepo.DbSet.AsNoTracking().Where(x => x.Col_Id == col.Col_Id && x.Data_Reg == _Date).Sum(x => x.Durata_Fig) ?? 0.0);
                            response.Add("newColumnTotal", String.Format("{0}:{1}", newDayDur.TotalHours.ToString("00"), newDayDur.Minutes.ToString("00")));

                            #endregion

                            #region AGGIORNAMENTO DELTA COLONNA E TOTALI RIGHE

                            UpdateVisualFields(response, _OldTime, _Time, _OldColumnDelta, _OldRowTotal, _OldRowTotalTotal, _OldRowDeltaTotal);

                            #endregion

                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Clear();
                        errors.Add("Errore interno durante la creazione di una nuova registrazione");
                        esito = "FAIL";
                        _log.Error(String.Format("Errore durante l'inserimento di una modifica dal cartellino con exception {0}", ex.Message));
                    }
                }

                #endregion

            }

            #endregion

            response.Add("esito", esito);
            response.Add("errors", JArray.FromObject(errors));

            return JsonConvert.SerializeObject(response);
        }
        [WebMethod]
        public static string SaveCantCartellinoCellUpdate(dynamic parameters)
        {

            #region VARIABILI RESPONSE

            JObject response = new JObject();

            List<string> errors = new List<string>();

            string esito = "OK";

            #endregion

            #region PARSING CANT

            int _Cant_Id = (parameters["Cant_Id"] != null) ? (int)parameters["Cant_Id"] : 0;

            Cant cant = RepoManager.CantRepo.FirstOrDefault(c => c.Cant_Id == _Cant_Id);

            #endregion

            #region PARSING COLLABORATORE

            int _Col_Id = (int)parameters["Col_Id"];

            Col col = RepoManager.ColRepo.DbSet.Find(_Col_Id);

            #endregion

            #region PARSING MOTIVAZIONE

            string _Codice = (string)parameters["Codice"];

            #endregion

            #region PARSING DATA

            DateTime _Date = DateTime.Parse((string)parameters["Date"]);

            #endregion

            #region PARSING DURATA

            string[] timeString = ((string)parameters["Time"]).Split(':');

            TimeSpan _Time = TimeSpan.Zero;

            if ((string)parameters["Time"] != "-")
            {
                _Time = TimeSpan.Parse((string)parameters["Time"]);
            }

            #endregion

            #region PARSING DURATA PRECEDENTE

            string[] oldTimeString = ((string)parameters["OldTime"]).Split(':');

            TimeSpan _OldTime = TimeSpan.Zero;

            if ((string)parameters["OldTime"] != "-")
            {
                _OldTime = TimeSpan.Parse((string)parameters["OldTime"]);
            }

            #endregion

            #region PARSING TOTALE RIGA MOTIVAZIONE PRECEDENTE

            string[] oldRowTotalString = ((string)parameters["OldRowTotal"]).Split(':');

            TimeSpan _OldRowTotal = TimeSpan.Zero;

            if ((string)parameters["OldRowTotal"] != "-")
            {
                _OldRowTotal = TimeSpan.FromHours(double.Parse(oldRowTotalString[0]));

                _OldRowTotal = _OldRowTotal >= TimeSpan.Zero ? _OldRowTotal.Add(TimeSpan.FromMinutes(double.Parse(oldRowTotalString[1]))) : _OldRowTotal.Subtract(TimeSpan.FromMinutes(double.Parse(oldRowTotalString[1])));
            }
            #endregion

            #region RETTIFICHE 

            if (_Codice == "Rettifiche Manu.")
            {

                try
                {

                    #region CANCELLAZIONE RETT.

                    //Se viene inserito il carattere di cancellazione delle rettifiche x quel giorno
                    //si cancellano le rettifiche presenti

                    if (_Time == TimeSpan.Zero)
                    {
                        try
                        {
                            RepoManager.RegRepo.DeleteFromQuery(r => r.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual && r.Registrazione_Data_Ora_Fis_Reg.Equals(_Date) && r.Col_Id == col.Col_Id && r.Cant_Id == cant.Cant_Id);

                            UpdateCantVisualFields(response, _OldTime, _Time, _OldRowTotal);
                        }
                        catch (Exception ex)
                        {
                            _log.ErrorFormat("Errore durante la cancellazione delle rettifiche del collaboratore {0} con exception {1}", col.Codice_Collaboratore, ex.Message);
                            errors.Add(string.Format("Problemi con la cancellazione di una rettifica per il collaboratore {0}.", col.Codice_Collaboratore));
                        }
                    }

                    #endregion

                    #region INSERIMENTO RETTIFICA

                    else
                    {
                        CorrectionTypeEnum cte = CorrectionTypeEnum.CorrectionPlus;

                        #region CALCOLO MINUTI RETTIFICA

                        //Calcola i minuti di rettifica

                        if (_Time.TotalMinutes < 0)
                        {
                            cte = CorrectionTypeEnum.CorrectionMinus;
                            _Time = _Time.Negate();
                        }

                        #endregion

                        #region DELETE RETTIFICHE 

                        //Elimina le rettifiche presenti per il collaboratore-giorno-cantiere in questione
                        try
                        {
                            RepoManager.RegRepo.DbSet.Where(r => r.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual && r.Registrazione_Data_Ora_Fis_Reg.Equals(_Date) && r.Col_Id == col.Col_Id && r.Cant_Id == cant.Cant_Id).DeleteFromQuery();
                        }
                        catch (Exception ex)
                        {
                            _log.ErrorFormat("Errore durante la cancellazione delle rettifiche per il collaboratore {0} con exception {1}", col.Codice_Collaboratore, ex.Message);
                            errors.Add(string.Format("Problemi con la cancellazione delle rettifiche per il collaboratore {0}.", col.Codice_Collaboratore));
                        }

                        #endregion

                        #region SCRITTURA RETTIFICA

                        //Se la rettifica è diversa da 0, genera la rettifica da generare
                        if (_Time.TotalMinutes != 0)
                        {
                            Reg retToAdd = RepoManager.RegRepo.GenerateManualRett(col.Col_Id, _Date, cte, _Time, cant.Cant_Id);
                            RepoManager.RegRepo.Add(retToAdd);
                        }

                        RepoManager.RegRepo.SaveChanges();

                        #endregion
                    }

                    #endregion

                    #region AGGIORNAMENTO DELTA COLONNA E TOTALI RIGHE

                    UpdateCantVisualFields(response, _OldTime, _Time, _OldRowTotal);

                    #endregion

                }
                catch (Exception ex)
                {
                    errors.Add(string.Format("Problemi con il salvataggio delle rettifiche per il collaboratore {0}.", col.Codice_Collaboratore));
                    _log.ErrorFormat("Errore durante il salvataggio di una rettifica per il collaboratore {0} con exception {1}", col.Codice_Collaboratore, ex.Message);
                }
            }

            #endregion

            #region CAUSALI

            else
            {
                int motiv_Id = RepoManager.Tab_DecodRepo.First(m => m.Nome_Tab == "MOTIVAZIONI" && m.Chiave_Tab == _Codice).Tab_Decod_Id;

                #region CANCELLAZIONE CAUSALE

                if (_Time == TimeSpan.Zero)
                {

                    try
                    {

                        var regEU_Duration = RepoManager.Reg_VRepo.DbSet.Where(x => x.Col_Id == col.Col_Id && x.Registrazione_Tipo_Reg == 0 && x.Data_Ora_Fig_E != null && x.Data_Ora_Fig_U != null && x.Data_Reg == _Date).Select(x => x.Durata_Fis).Sum() ?? 0;
                        List<Reg_V> onlyDurRegs = RepoManager.Reg_VRepo.Find(x => x.Col_Id == col.Col_Id && x.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration && x.Data_Reg == _Date && x.Motivazione_Reg_Id == motiv_Id && x.Cant_Id == cant.Cant_Id).ToList();

                        TimeSpan totalDayDuration = TimeSpan.FromMinutes((double)regEU_Duration);

                        //Se abbiamo delle reg di sola durata le cancelliamo
                        if (onlyDurRegs.Any())
                        {
                            onlyDurRegs.ForEach(regV =>
                            {
                                Reg toDelete = new Reg() { Reg_Id = regV.RegE };
                                RepoManager.RegRepo.Attach(toDelete);
                                RepoManager.RegRepo.Context.Entry(toDelete).State = System.Data.Entity.EntityState.Deleted;
                            });

                            RepoManager.RegRepo.SaveChanges();
                        }

                        //Se abbiamo delle registrazioni  E-U non le cancelliamo ma facciamo in modo di annullarle tramite una nuova reg-negativa
                        if (totalDayDuration - _Time != TimeSpan.Zero)
                        {
                            CorrectionTypeEnum cte = (totalDayDuration - _Time < TimeSpan.Zero) ? CorrectionTypeEnum.CorrectionPlus : CorrectionTypeEnum.CorrectionMinus;

                            Reg newCorrectionReg = RepoManager.RegRepo.GenerateCorrectionReg(col.Col_Id, _Date, CorrectionTypeEnum.CorrectionMinus, (totalDayDuration - _Time).Duration(), cant.Cant_Id);

                            RepoManager.RegRepo.Add(newCorrectionReg, true);
                        }


                        #region AGGIORNAMENTO TOTALI

                        UpdateCantVisualFields(response, _OldTime, _Time, _OldRowTotal);

                        #endregion


                    }
                    catch (Exception ex)
                    {
                        errors.Add("Errore interno durante la cancellazione di una registrazione");
                        esito = "FAIL";
                        _log.Error(String.Format("Errore durante la cancellazione di una registrazione dal cartellino con exception {0}", ex.Message));
                    }
                }

                #endregion

                #region INSERIMENTO CAUSALE

                else
                {
                    try
                    {
                        var regEU_Duration = RepoManager.Reg_VRepo.DbSet.AsNoTracking().Where(x => x.Col_Id == col.Col_Id && x.Registrazione_Tipo_Reg == 0 && x.Data_Ora_Fig_E != null && x.Data_Ora_Fig_U != null && x.Data_Reg == _Date && x.Motivazione_Reg_Id == motiv_Id).Select(x => x.Durata_Fis).Sum() ?? 0;
                        List<Reg_V> onlyDurRegs = RepoManager.Reg_VRepo.Find(x => x.Col_Id == col.Col_Id && x.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration && x.Data_Reg == _Date && x.Motivazione_Reg_Id == motiv_Id && x.Cant_Id == cant.Cant_Id, true).ToList();

                        TimeSpan totalDayDuration = TimeSpan.FromMinutes((double)regEU_Duration);

                        //Se la durata delle ore complessive supera le 24 H ritorno un errore
                        if (totalDayDuration + _Time > TimeSpan.FromMinutes(1440))
                        {
                            errors.Add("Totale ore giornaliere superiori alla durata massima!");
                            esito = "FAIL";

                        }
                        else
                        {
                            //Se abbiamo registrazioni di sola durata le cancelliamo
                            if (onlyDurRegs.Any())
                            {
                                onlyDurRegs.ForEach(regV =>
                                {
                                    Reg toDelete = new Reg() { Reg_Id = regV.RegE };
                                    RepoManager.RegRepo.Attach(toDelete);
                                    RepoManager.RegRepo.Context.Entry(toDelete).State = System.Data.Entity.EntityState.Deleted;
                                });

                                RepoManager.RegRepo.SaveChanges();
                            }

                            //Se abbiamo registrazioni E-U con motivazione ne generiamo una di correzione 
                            if (totalDayDuration - _Time != TimeSpan.Zero)
                            {
                                CorrectionTypeEnum cte = (totalDayDuration - _Time < TimeSpan.Zero) ? CorrectionTypeEnum.CorrectionPlus : CorrectionTypeEnum.CorrectionMinus;

                                Reg newCorrectionReg = RepoManager.RegRepo.GenerateDurationReg(col.Col_Id, _Date, cte, (totalDayDuration - _Time).Duration(), cant.Cant_Id, motiv_Id);
                                RepoManager.RegRepo.Add(newCorrectionReg, true);


                            }

                            #region AGGIORNAMENTO DELTA COLONNA E TOTALI RIGHE

                            UpdateCantVisualFields(response, _OldTime, _Time, _OldRowTotal);

                            #endregion

                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Clear();
                        errors.Add("Errore interno durante la creazione di una nuova registrazione");
                        esito = "FAIL";
                        _log.Error(String.Format("Errore durante l'inserimento di una modifica dal cartellino con exception {0}", ex.Message));
                    }
                }

                #endregion

            }

            #endregion

            response.Add("esito", esito);
            response.Add("errors", JArray.FromObject(errors));

            return JsonConvert.SerializeObject(response);
        }
        [WebMethod]
        public static string GenerateAutoRettifiche(int[] selectedCollab, DateTime period)
        {


            #region VARIABILI D'APPOGGIO

            JArray errors = new JArray();

            JObject error = null;
            JObject result = new JObject();

            List<Col> collaboratori = RepoManager.ColRepo.Find(c => selectedCollab.Contains(c.Col_Id)).ToList();

            DateTime from = DateTime.Now;
            DateTime to = DateTime.Now;


            List<Reg_V> regsByCol = null;    //Reg raggruppate per collaboratore
            List<Reg> manualRegToAdd = null; //Vettore per le aggiunte a db

            Tab_Orari orario = null;

            TimesheetModuleItem colPlan = null;

            string month = null;

            #endregion

            //Per ogni collaboratore segnalato
            foreach (Col col in collaboratori)
            {
                bool useOrario = false;

                int id_TabOrario = 0;


                #region RICERCA ORARIO O PARAMETRO

                if (col.Tab_Orari_Tipo_Id.HasValue)
                {
                    id_TabOrario = (int)col.Tab_Orari_Tipo_Id;
                    useOrario = true;

                }
                else if (RepoManager.ParamRepo.ParametersRow.Durata_Quadratura_Rettifiche_Auto == null)
                {
                    error = new JObject();
                    error.Add("Messaggio", String.Format("Rettifiche non generate per il collaboratore {0} perchè sprovvisto di orario. Inoltre non è stato settato alcun parametro di default.", col.Codice_Collaboratore));
                    errors.Add(error);

                    continue;
                }

                #endregion

                from = CommonService.GetFirstMonthDay(period);
                to = CommonService.GetLastMonthDay(period);
                to = to.AddDays(1);
                month = period.ToString("yyyy/MM");

                #region Deduzione durata

                if (useOrario)
                {
                    //Se abbiamo un orario si utilizza l'orario altrimenti si utilizza il parametro (se presente altrimenti si ignora il collaboratore)
                    orario = RepoManager.Tab_OrariRepo.FirstOrDefault(or => or.Tab_Orari_Tipo_Id == id_TabOrario);
                    if (orario != null)
                    {
                        bool isFromFreeTimesheet;
                        int freeTimesheetId;
                        //Dizionario piano orario
                        Dictionary<int, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> planMinutes = RepoManager.Tab_OrariRepo.GetPlanMinutes(col.Col_Id,
                                                               from, to, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col,
                                                                out isFromFreeTimesheet, out freeTimesheetId, false, "Col", false);

                        //Piano orario da cartellino (ottimo per scorrerlo)
                        colPlan = TimesheetModuleItem.GenerateNewPlanTimesheet(false, planMinutes.First().Value, isFromFreeTimesheet, freeTimesheetId, col.Col_Id, from, planMinutes.First().Key);
                    }

                }

                #endregion

                #region Cancellazione rettifiche manuali

                try
                {
                    RepoManager.RegRepo.DeleteFromQuery(r => r.Col_Id == col.Col_Id && r.Registrazione_Data_Ora_Fis_Reg >= from && r.Registrazione_Data_Ora_Fis_Reg <= to && r.Registrazione_Tipo_Reg == 11);
                    regsByCol = RepoManager.Reg_VRepo.Find(r => r.Col_Id == col.Col_Id && r.Data_Reg_AAAA_MM == month && r.Durata_Fis != null, true).ToList();
                }
                catch (Exception ex)
                {
                    _log.Error(String.Format("Errore durante la cancellazione delle rettifiche per il collaboratore {0} con exception {1}", col.Codice_Collaboratore, ex.Message));

                    error = new JObject();
                    error.Add("Messaggio", String.Format("Rettifiche non generate per il collaboratore {0} a causa di un errore interno", col.Codice_Collaboratore));
                    errors.Add(error);

                    continue;
                }

                #endregion

                manualRegToAdd = new List<Reg>();

                #region Somma durata reg_V per data e per cantiere

                //Solo reg con durata raggruppate per data
                foreach (var group in regsByCol.GroupBy(r => r.Data_Reg).ToList())
                {
                
                    int dayDuration = 0;
                
                
                    if (useOrario)
                    {
                        int tabOrarioDay = (int)Enum.Parse(typeof(DayofWeekTabOrario), group.Key.Value.DayOfWeek.ToString());
                        double durata_Orario = (double)typeof(TimesheetModuleItem).GetProperty("Day" + group.Key.Value.Day.ToString("00")).GetValue(colPlan, null);
                        if (durata_Orario != 0)
                        {
                            //Durata gioranata da orario
                            dayDuration = CommonService.GetMinutesFromTimeSpan(TimeSpan.FromHours(durata_Orario));
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        dayDuration = Convert.ToInt32(CommonService.GetDoubleFromTimeSpan((TimeSpan)RepoManager.ParamRepo.ParametersRow.Durata_Quadratura_Rettifiche_Auto));
                    }

                    var sum = (double)group.Sum(r => r.Durata_Fig);     //Minuti totali del giorno
                    var diff = dayDuration - sum;                       //Minuti totali da rimuovere quel giorno


                    foreach (var gRegs in group.GroupBy(c => c.Cant_Id))  //Le reg raggruppate per data vengono raggruppate per cantiere
                    {
                        int duration = (int)gRegs.Sum(r => r.Durata_Fig);   //Somma totale della durata delle reg in tale data per cantiere (posso esserci più cantieri nello stesso giorno)

                        double percentage = Math.Round((((double)Convert.ToDouble(duration)) / (sum)), 2); //Percentuale sul totale del giorno (funge da peso)
                        double minutesToTake = Math.Round(percentage * diff);  //Minuti da sottrarre per cantiere 

                        //Generazione rettifica
                        if (sum != dayDuration)
                        {
                            var customization = Convert.ToBoolean(RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.GenerateRettificheUnderSchedule));

                            if (sum > dayDuration)
                            {
                                Reg singRett = RepoManager.RegRepo.GenerateManualRett(col.Col_Id, (DateTime)group.Key, CorrectionTypeEnum.CorrectionMinus, TimeSpan.FromMinutes(Math.Abs(minutesToTake)), (int)gRegs.Key);
                                manualRegToAdd.Add(singRett);
                            }
                            else if (sum < dayDuration && !customization)
                            {
                                Reg singRett = RepoManager.RegRepo.GenerateManualRett(col.Col_Id, (DateTime)group.Key, CorrectionTypeEnum.CorrectionPlus, TimeSpan.FromMinutes(Math.Abs(minutesToTake)), (int)gRegs.Key);
                                manualRegToAdd.Add(singRett);
                            }
                        }
                    }

                };

                #endregion

                #region Inserimento ad database

                try
                {
                    RepoManager.RegRepo.Context.BulkInsert(manualRegToAdd);
                    manualRegToAdd = null;
                }
                catch (Exception ex)
                {
                    _log.Error(String.Format("Errore durante il salvataggio delle rettifiche per il collaboratore {0} con exception {1}", col.Codice_Collaboratore, ex.Message));

                    error = new JObject();
                    error.Add("Messaggio", String.Format("Rettifiche non generate per il collaboratore {0} a causa di un errore interno", col.Codice_Collaboratore));
                    errors.Add(error);

                }
                finally
                {
                    useOrario = false;
                }

                #endregion
            }

            result.Add("errors", JArray.FromObject(errors));


            return JsonConvert.SerializeObject(result);
        }
        [WebMethod]
        public static string GenerateRangeRettifiche(IEnumerable<int> selectedCollab, int minValue, int maxValue, DateTime selectedPickerDate)
        {
            #region COLLABORATORI

            List<Col> selectedCols = RepoManager.ColRepo.Find(col => selectedCollab.Contains(col.Col_Id), true).ToList();

            #endregion

            JObject response = new JObject();
            JObject error = null;
            JArray errors = new JArray();

            DateTime monthFirstDay = CommonService.GetFirstMonthDay(selectedPickerDate);
            DateTime monthLastDay = CommonService.GetLastMonthDay(selectedPickerDate);

            // ciclo su ogni collaboratore recuperato
            foreach (Col col in selectedCols)
            {
                RepoManager.RegRepo.DeleteFromQuery(reg => reg.Registrazione_Data_Ora_Fis_Reg >= monthFirstDay
                                                        && reg.Registrazione_Data_Ora_Fis_Reg <= monthLastDay
                                                        && reg.Col_Id == col.Col_Id
                                                        && reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimesheet);

                var deltaCartellini = TimesheetModuleItem.GenerateCartellino(selectedPickerDate, col, true, false, true, true, false, RepoManager.ParamRepo.ParametersRow.Cartellino_Visualizza_Delta, false)["justification"].Where(tm => tm.Justification == BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA)).ToList();
                var correctionsToAdd = new List<Reg>();

                if (deltaCartellini.Any())
                {

                    var deltaCartellino = deltaCartellini.First();

                    // ciclo su tutti i giorni del mese in elaborazione
                    for (int i = 1; i <= monthLastDay.Day; i++)
                    {
                        string dayPropertyName = string.Format("{0}{1}", "Day", i.ToString("00"));
                        int deltaDuration = CommonService.FromHoursToMinutes((double)deltaCartellino[dayPropertyName], deltaCartellino.IsDecimalHours);

                        if (deltaDuration != 0 && deltaDuration >= minValue && deltaDuration <= maxValue)
                        {
                            TimeSpan correctionDuration = TimeSpan.FromMinutes(Math.Abs(deltaDuration));
                            CorrectionTypeEnum correctionDirection = deltaDuration > 0 ? CorrectionTypeEnum.CorrectionMinus : CorrectionTypeEnum.CorrectionPlus;
                            var correctionDate = new DateTime(monthLastDay.Year, monthLastDay.Month, i);
                            correctionsToAdd.Add(RepoManager.RegRepo.GenerateCorrectionReg(col.Col_Id, correctionDate, correctionDirection, correctionDuration));
                        }
                    }

                    RepoManager.RegRepo.DbSet.AddRange(correctionsToAdd);
                }
            }

            RepoManager.RegRepo.SaveChanges();

            response.Add("errors", errors);

            return JsonConvert.SerializeObject(response);
        }

        [WebMethod]
        public static string DeleteRettificheManuali(int?[] selectedCols, DateTime selectedMonth)
        {
            List<Col> collaboratori = RepoManager.ColRepo.Find(c => selectedCols.Contains(c.Col_Id)).ToList();

            DateTime from = CommonService.GetFirstMonthDay(selectedMonth);
            DateTime to = CommonService.GetLastMonthDay(selectedMonth).AddDays(1);


            RepoManager.RegRepo.DeleteFromQuery(reg => selectedCols.Contains(reg.Col_Id) && reg.Registrazione_Tipo_Reg == 11 && reg.Registrazione_Data_Ora_Fis_Reg >= from && reg.Registrazione_Data_Ora_Fis_Reg < to);

            return JsonConvert.SerializeObject(new { });
        }
        [WebMethod]
        public static string GenerateExport(int[] selectedCols, DateTime period, int excelModelId)
        {
            JObject response = new JObject();

            Tab_Excel_Model model = RepoManager.Tab_Excel_ModelRepo.First(c => c.ExcelModel_Id == excelModelId);

            _exportContext.SetSelectedKeys(selectedCols);
            _exportContext.SetExportMonth(period);
            _exportContext.SetExport(model);

            JArray errors = new JArray();

            if (_exportContext.GenerateExport(ref errors))
            {
                response = JObject.FromObject(_exportContext.GetDownloadExportParams());
                if (errors.Any())
                    response.Add(nameof(errors), errors);

                //bool rtn = SaveAsPdf("D:\\download\\Export Hotel (6).xlsx");
            }
            else
            {
                response.Add("fatalError", "Errore durante la generazione dell'export! Contattare l'assistenza!");
            }



            #region AGGIORNAMENTO DATA EXPORT COLLABORATORE

            JArray exportedColsDates = new JArray();

            foreach (var colId in selectedCols)
            {
                Col processingCol = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colId);
                if (processingCol != default(Col))
                {
                    if (processingCol.Data_Ultimo_Cartellino_Esportato == null || processingCol.Data_Ultimo_Cartellino_Esportato < period)
                    {
                        processingCol.Data_Ultimo_Cartellino_Esportato = period;

                        JObject colDate = new JObject();
                        colDate.Add("codice", processingCol.Col_Id);
                        colDate.Add("date", period);

                        exportedColsDates.Add(colDate);
                    }
                }
            }
            RepoManager.ColRepo.SaveChanges();

            response.Add("exportedDatesCols", exportedColsDates);

            #endregion

            return JsonConvert.SerializeObject(response);

        }
        [WebMethod]
        public static string GenerateReport(int[] selectedCols, DateTime selectedDate)
        {
            JObject response = new JObject();


            DateTime date = DateTime.MinValue;

            List<TimesheetModuleItem> cartellini = new List<TimesheetModuleItem>();
            dynamic optionsObj = null;

            //Recupera le opzioni del cartellino

            List<Domain.Col> collaboratori = RepoManager.ColRepo.Find(c => selectedCols.Contains(c.Col_Id)).ToList();

            DateTime minDate = CommonService.GetFirstMonthDay(selectedDate);
            DateTime monthLastDate = CommonService.GetLastMonthDay(minDate);

            foreach (Col col in collaboratori)
            {
                var regs = RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Col_Id == col.Col_Id
                    && (regv.Data_Reg >= minDate && regv.Data_Reg <= monthLastDate)
                    && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att, true);
                if (regs.Count() > 0)
                {
                    cartellini.AddRange(TimesheetModuleItem.GenerateCartellinoReport(selectedDate, col, false, false, true)["justification"]);
                }     
            }

            var timesheetColReport = new XRColCartellino(cartellini, null, null, optionsObj);
            Modules.ExtXtraReport xrReport = new Modules.ExtXtraReport { Report = timesheetColReport, PictureBox = timesheetColReport.CompanyLogo };
            //Nasconde la copertin
            xrReport.Report.Bands[BandKind.ReportHeader].Visible = false;
            byte[] companyLogo = RepoManager.ParamRepo.ParametersRow.CompanyLogo;
            if (companyLogo != null && xrReport.PictureBox != null)
            {
                xrReport.PictureBox.Image = Image.FromStream(new MemoryStream(companyLogo));
                xrReport.PictureBox.Sizing = ImageSizeMode.ZoomImage;
            }
            MemoryStream stream = CommonServiceReport.CreateReport(null, "Report", false, xrReport.Report);

            var fileName = "Report Cartellino";
            string extension = "pdf";
            var outPutPath = String.Format("{0}{1}{2}.{3}", AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Output_Path, fileName, extension);
            try
            {
                using (FileStream file = new FileStream(outPutPath, FileMode.Create, FileAccess.Write))
                {
                    stream.WriteTo(file);
                    file.Close();
                    stream.Close();
                }

                response.Add("path", String.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Output_Path));
                response.Add("fileName", fileName);
                response.Add("extension", extension);
            }
            catch (Exception ex)
            {

            }

            return JsonConvert.SerializeObject(response);
        }

        #region WIDGET

        /// <summary>
        /// Gestione delle varie lookUp della pagina del cartellino.
        /// Tramite reflection permette la ricerca nel repository richiesto con paginazione, filtro e select generati dinamicamente
        /// </summary>
        /// <param name="eType">todo: describe eType parameter on dxComboBoxes</param>
        /// <param name="whereArray">todo: describe whereArray parameter on dxComboBoxes</param>
        /// <param name="selectArray">todo: describe selectArray parameter on dxComboBoxes</param>
        /// <param name="skip">todo: describe skip parameter on dxComboBoxes</param>
        /// <param name="take">todo: describe take parameter on dxComboBoxes</param>
        /// <returns></returns>
        [WebMethod]
        public static string dxComboBoxes(string eType, object[] whereArray, object selectArray, int skip, int take)
        {
            JObject dataSource = new JObject();

            var setFunction = RepoManager.Tab_GridLookupRepo.GetType().GetMethod("DxLookUpStore");

            var type = Type.GetType(String.Format("Domain.{0}, Domain", eType));

            return JsonConvert.SerializeObject((JObject)setFunction.MakeGenericMethod(type).Invoke(null, new object[] { whereArray, selectArray, skip, take }));
        }

        /// <summary>
        /// Gestione dell'header filter della griglia (ritorna valori distinti per uno specifico campo)
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public static string dxDataGridGetHeaderFilter(string dataField, object[] whereArray)
        {
            return JsonConvert.SerializeObject(dataSource.GetDistinctValues(dataField, JArray.FromObject(whereArray)).ToList());
        }


        /// <summary>
        /// Gestione dataSource della griglia dei collaboratori
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public static string dxDataGridGetColls(dynamic loadOptions)
        {     
            JObject response = new JObject();
     
            JObject gridOptions = JObject.FromObject(loadOptions);

            if (gridOptions.Count == 1) //E' stato selezionato il checkBox "select all"
            {
                response.Add("totalCount", dataSource.TotalCount(new JArray()));

                response.Add("data", JArray.FromObject(dataSource.GetAll(Normalize(gridOptions, "select")).ToList()));
            }
            else
            {
                int skip = gridOptions["skip"] != null ? (int)gridOptions["skip"] : -1;
                int take = gridOptions["take"] != null ? (int)gridOptions["take"] : -1;

                JArray whereArray = Normalize(gridOptions, "filter");
                JArray orderByArray = Normalize(gridOptions, "sort");
                JArray selectArray = Normalize(gridOptions, "select");

                response.Add("totalCount", dataSource.TotalCount(whereArray));

                response.Add("data", JArray.FromObject(dataSource.Query(skip, take, whereArray, orderByArray, selectArray).ToList()));
            }

            return JsonConvert.SerializeObject(response);
        }
        #endregion


        #endregion

        #region FUNZIONI DI SUPPORTO

        public static JArray Normalize(JObject loadOptions, string property)
        {
            return loadOptions[property] != null && loadOptions[property].HasValues ? loadOptions[property].ToObject<JArray>() : new JArray();
        }

        public static bool SaveAsPdf(string saveAsLocation)
        {
            string saveas = (saveAsLocation.Split('.')[0]) + ".pdf";
            try
            {
                Workbook workbook = new Workbook();
                workbook.LoadFromFile(saveAsLocation);

                //Save the document in PDF format

                workbook.SaveToFile(saveas, Spire.Xls.FileFormat.PDF);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Aggiunge al json della response tutti i campi della griglia (delta e totali vari)
        /// che necessitano di essere modificati dopo il salvataggio della nuova reg
        /// </summary>
        /// <param name="response">JObject contente i vari campi</param>
        /// <param name="oldValue">Vecchio valore </param>
        /// <param name="newValue">Nuovo valore inserito</param>
        /// <param name="oldColumnDelta">Vecchio delta colonna</param>
        /// <param name="oldRowTotal">Vecchio totale riga</param>
        /// <param name="oldRowTotalTotal">Vecchio totale mensile</param>
        /// <param name="oldRowDeltaTotal">Vecchio delta mensile</param>
        private static void UpdateVisualFields(JObject response, TimeSpan oldValue, TimeSpan newValue, TimeSpan oldColumnDelta, TimeSpan oldRowTotal, TimeSpan oldRowTotalTotal, TimeSpan oldRowDeltaTotal)
        {
            if (oldValue < newValue)
            {
                response.Add("newColumnDelta", FormatDuration(oldColumnDelta, (newValue - oldValue), 0));
                response.Add("newRowMotivTotal", FormatDuration(oldRowTotal, (newValue - oldValue), 0));
                response.Add("newRowTotalTotal", FormatDuration(oldRowTotalTotal, (newValue - oldValue), 0));
                response.Add("newRowDeltaTotal", FormatDuration(oldRowDeltaTotal, (newValue - oldValue), 0));

            }
            else
            {
                response.Add("newColumnDelta", FormatDuration(oldColumnDelta, (oldValue - newValue), 1));
                response.Add("newRowMotivTotal", FormatDuration(oldRowTotal, (oldValue - newValue), 1));
                response.Add("newRowTotalTotal", FormatDuration(oldRowTotalTotal, (oldValue - newValue), 1));
                response.Add("newRowDeltaTotal", FormatDuration(oldRowDeltaTotal, (oldValue - newValue), 1));
            }
        }

        /// <summary>
        /// Aggiunge al json della response tutti i campi della griglia (cantiere)
        /// che necessitano di essere modificati dopo il salvataggio della nuova reg
        /// </summary>
        /// <param name="response">JObject contente i vari campi</param>
        /// <param name="oldValue">Vecchio valore </param>
        /// <param name="newValue">Nuovo valore inserito</param>
        /// <param name="oldRowTotal">Vecchio totale riga</param>
        private static void UpdateCantVisualFields(JObject response, TimeSpan oldValue, TimeSpan newValue, TimeSpan oldRowTotal)
        {
            if (oldValue < newValue)
            {
                response.Add("newRowMotivTotal", FormatDuration(oldRowTotal, (newValue - oldValue), 0));

            }
            else
            {
                response.Add("newRowMotivTotal", FormatDuration(oldRowTotal, (oldValue - newValue), 1));
            }
        }

        /// <summary>
        /// Formatta un timespan in formato javascript
        /// </summary>
        /// <param name="value">Nuovo valore di durata</param>
        /// <param name="op">Flag che significa addizione (totale+value) o sottrazione(totale-value)</param>
        /// <param name="oldTotal">todo: describe oldTotal parameter on FormatDuration</param>
        /// <returns>Una stringa in formato hh:mm</returns>
        private static string FormatDuration(TimeSpan oldTotal, TimeSpan value, int op)
        {
            string formattedDuration = "";

            TimeSpan total = TimeSpan.MinValue;

            if (op == 0) //Add
            {
                total = oldTotal + value;

            }
            else //Subtract
            {
                total = oldTotal - value;
            }

            formattedDuration = String.Format("{0}:{1}", total.TotalHours.ToString("00"), total.Minutes.ToString("00"));


            return formattedDuration;

        }

        #endregion

    }
}