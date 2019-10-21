using Business.BusinessClasses.CartellinoServiceDTOs;
using Business.BusinessServices.TimesheetService.Classes.Cell;
using Business.BusinessServices.TimesheetService.Classes.Row;
using Business.BusinessServices.TimesheetService.Helpers;
using Common;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Business.BusinessClasses.Printable.Interfaces.Row;
using Business.BusinessClasses.Printable.Classes.Cell;
using System.Drawing;
using Business.BusinessClasses.Printable.Classes.Row;
using Business.BusinessServices.TimesheetService.Abstracts;
using Newtonsoft.Json.Linq;
using Business.BusinessServices.TimesheetService.Classes.Config;
using Business.BusinessServices.TimesheetService.Providers.Registrations.Interfaces;
using Business.BusinessServices.TimesheetService.Classes.RowConfig;
using Business.Repository;
using Business.BusinessServices.CartellinoService.Interfaces.Timesheet;
using Business.BusinessServices.CartellinoService.Classes.Riga;

namespace Business.BusinessServices.CartellinoService.Classes.Cartellino
{
    /// <summary>
    /// </summary>
    /// <seealso cref="Business.BusinessServices.CartellinoService.Abstracts.TimesheetBase{Domain.Col}" />
    /// <seealso cref="Business.BusinessServices.CartellinoService.Interfaces.Cartellino.ITimesheet" />
    internal sealed class ColTimesheet : TimesheetBase<Col>, ITimesheet
    {
        private ITimesheetDataProvider _provider;
        private IEnumerable<CartellinoRegV> _registrazioni;
        private TimeSpan _monteMinuti;

        private bool _calculateMonteMinuti = true;
        private bool CalculateMonteMinuti
        {
            get
            {
                return _calculateMonteMinuti;
            }

            set
            {
                _calculateMonteMinuti = value;
            }
        }


        #region CTOR

        public ColTimesheet(TimesheetConfig<Col> config, ITimesheetDataProvider registrationsProvider) : base(config)
        {
            _provider = registrationsProvider;
        }

        #endregion

        #region METODI PUBBLICI


        /// <summary>
        /// Prepara gli eventuali dati da utilizzare durante la computazione del cartellino.
        /// </summary>
        public void BeforeCompute()
        {
            _registrazioni = _provider.GetTimesheetRegistrations(BaseEntity, MinDate, MaxDate);
        }

        /// <summary>
        /// Elabora il cartellino.
        /// </summary>
        public void Compute()
        {
            GeneraRigaOrario();
            GeneraRigaOreLavorate();
            GeneraRigaOreMotivate();
            GeneraRigaOreViaggi();
            GeneraRigaArrotondamenti();
            GeneraRigaDelta();
            GeneraRigaTotale();

            if (Parameters.Abilita_MonteMinuti && CalculateMonteMinuti)
                CalcolaMonteMinuti();
        }

        /// <summary>
        /// Termina l'elaborazione del cartellino
        /// </summary>
        public void AfterCompute()
        {
            CalculateWeekendTotals();
        }

        public override JObject ToJson()
        {
            JObject cartellino = base.ToJson();

            if (Parameters.Abilita_MonteMinuti)
                cartellino.Add("monteMinuti", CartellinoHelper.FormatDuration(_monteMinuti));

            return cartellino;
        }

        #region STAMPA CARTELLINO

        /// <summary>
        /// Crea una matrice che è l'header del cartellino.
        /// Viene utilizzata per la stampa in file excel.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPrintableRow> GetHeaderRows()
        {
            #region CODICE RIGA 

            var infoRow = new PrintableRow();

            string labelCollaboratore = $"{BaseEntity.CognomeNome_Col}";

            infoRow.Add(new PrintableCell(labelCollaboratore, Color.Black, Color.Transparent));

            if (Parameters.Abilita_MonteMinuti)
            {
                infoRow.InsertAt(20, new PrintableCell("Monte minuti", Color.Black, Color.Transparent));
                infoRow.InsertAt(21, new PrintableCell(CartellinoHelper.FormatDuration(_monteMinuti), Color.Black, Color.Transparent));
            }



            #endregion

            #region GIORNI RIGA 

            var headerRow = new PrintableRow();

            headerRow.Add(PrintableCell.Empty); //Prima cella vuota

            int weekNumber = 1;

            foreach (var data in CommonService.EachDay(MinDate, MaxDate))
            {
                Color textColor = Color.Black;

                if (BusinessService.IsDayOff(data))
                    textColor = Color.Red;

                headerRow.Add(new PrintableCell(data.Day, textColor, Color.LightGray));

                if (data.DayOfWeek == DayOfWeek.Sunday && Parameters.Visualizza_Totali_Settimanali)
                {
                    headerRow.Add(new PrintableCell($"Sett. {weekNumber++}", Color.Black, Color.LightGray));
                }

            }

            #endregion

            #region TOTALE RIGA

            if (Parameters.Totale_Prima_Colonna)
            {
                headerRow.InsertAt(1, new PrintableCell("TOTALE", Color.Black, Color.LightGray));
            }
            else
            {
                headerRow.Add(new PrintableCell("TOTALE", Color.Black, Color.LightGray));
            }

            #endregion

            return new List<IPrintableRow>() { infoRow, headerRow };
        }
        /// <summary>
        /// Crea una matrice che è il corpo del cartellino.
        /// Viene utilizzata per la stampa in file excel.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPrintableRow> GetBodyRows()
        {
            List<IPrintableRow> bodyRows = new List<IPrintableRow>();

            foreach (var timesheetRow in TimesheetRows.VisibleRows)
            {
                var bodyRow = new PrintableRow();

                #region CODICE RIGA 

                string labelRiga = timesheetRow.RowDescription;

                bodyRow.Add(new PrintableCell(labelRiga, Color.Black, Color.White));

                #endregion

                #region GIORNI RIGA 

                foreach (var timesheetCell in timesheetRow)
                {
                    string durataFormattata = CartellinoHelper.FormatDuration(timesheetCell.Total);

                    bodyRow.Add(new PrintableCell(durataFormattata, Color.Black, Color.White));

                    if (timesheetCell.Date.DayOfWeek == DayOfWeek.Sunday && Parameters.Visualizza_Totali_Settimanali)
                    {
                        var totaleSettimana = timesheetRow.WeekendTotals.First(g => g.Date == timesheetCell.Date).Total;

                        string totaleSettimanaFormattato = CartellinoHelper.FormatDuration(totaleSettimana);

                        bodyRow.Add(new PrintableCell(totaleSettimanaFormattato, Color.Black, Color.White));
                    }

                }

                #endregion

                #region TOTALE RIGA

                string totaleFormattato = CartellinoHelper.FormatDuration(timesheetRow.RowTotal);

                if (Parameters.Totale_Prima_Colonna)
                {
                    bodyRow.InsertAt(1, new PrintableCell(totaleFormattato, Color.Black, Color.White));
                }
                else
                {
                    bodyRow.Add(new PrintableCell(totaleFormattato, Color.Black, Color.White));
                }

                #endregion

                bodyRows.Add(bodyRow);

            }

            return bodyRows;
        }
        /// <summary>
        /// Crea una matrice che è il footer del cartellino.
        /// Viene utilizzata per la stampa in file excel.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPrintableRow> GetFooterRows()
        {
            return Enumerable.Empty<IPrintableRow>();
        }

        #endregion

        #endregion

        #region METODI PRIVATI

        #region GENERAZIONE RIGHE

        void GeneraRigaOrario()
        {
            IDictionary<DateTime, TimeSpan> planDurationByDay = GetRangePlanDuration(BaseEntity, MinDate, MaxDate);

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = Parameters.Visualizza_Piano;
            rowConfig.RowCode = CHIAVE_ORE_PIANO;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN);

            TimesheetPlanHoursRow planTimesheetRow = new TimesheetPlanHoursRow(rowConfig);

            FillTimesheetRowAndAdd(planDurationByDay, planTimesheetRow);
        }
        void GeneraRigaOreLavorate()
        {
            var oreLavorate = _registrazioni.OreLavorate().PerGiornoConDurata();

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = Parameters.Visualizza_Ore;
            rowConfig.RowCode = CHIAVE_ORE_LAVORATE;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_LAVORATE);

            TimesheetRoundingHoursRow riga = new TimesheetRoundingHoursRow(rowConfig);

            FillTimesheetRowAndAdd(oreLavorate, riga);
        }
        void GeneraRigaOreMotivate()
        {
            var oreMotivate = _registrazioni.OreMotivate().PerMotivazionePerGiornoConDurata();

            foreach (var gruppoMotivazione in oreMotivate)
            {
                var rowConfig = new TimesheetRowConfiguration();

                rowConfig.MinDate = MinSummaryDate;
                rowConfig.MaxDate = MaxSummaryDate;
                rowConfig.IsToShow = Parameters.Visualizza_Motivazioni;
                rowConfig.RowCode = CartellinoHelper.Motivazioni[gruppoMotivazione.Motivation_Id].Chiave_Tab;
                rowConfig.RowDescription = CartellinoHelper.Motivazioni[gruppoMotivazione.Motivation_Id].Decodifica_Tab;

                TimesheetMotivatedHoursRow riga = new TimesheetMotivatedHoursRow(rowConfig);

                FillTimesheetRowAndAdd(gruppoMotivazione.RegistrazioniPerDataConDurata, riga);
            }
        }
        void GeneraRigaOreViaggi()
        {
            var oreViaggi = _registrazioni.OreViaggi().PerGiornoConDurata();

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = Parameters.Visualizza_Viaggi;
            rowConfig.RowCode = CHIAVE_ORE_VIAGGI;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI);

            TimesheetTripHoursRow riga = new TimesheetTripHoursRow(rowConfig);

            FillTimesheetRowAndAdd(oreViaggi, riga);

        }
        void GeneraRigaDelta()
        {
            //Estraggo la riga dell'orario (possono essere più di 1 nel caso il piano sia diviso in notturno/diurno)
            var timesheetPlanRows = TimesheetRows.GetRows(TimesheetRowType.Orario);

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = Parameters.Visualizza_Delta;
            rowConfig.RowCode = CHIAVE_ORE_DELTA;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA);

            //Se non esiste non calcolo il delta e al suo posto aggiungo una riga delta vuota
            if (!timesheetPlanRows.Any())
            {
                TimesheetRows.AddRow(TimesheetDeltaRow.Default(rowConfig));
                return;
            }

            //Tutte le righe del cartellino che devono essere rapportate con l'orario 
            var timesheetRowsToSum = TimesheetRows.SummableRows;
            //Init della riga contente i delta x giorno
            TimesheetDeltaRow rigaDelta = new TimesheetDeltaRow(rowConfig);
            //Indice per accedere alla medesima cella dei vari arrai da sommare (indice del giorno)
            int cellIndex = 0;
            //Ispeziono ogni giorno dell'orario
            foreach (var timesheetPlanRow in timesheetPlanRows)//O(2) massimo 2 cicli
            {
                foreach (var timesheetCell in timesheetPlanRow) //O(31) massimo 31 cicli (giorni)
                {
                    TimeSpan totaleDelGiornoTemporaneo = TimeSpan.Zero;

                    //Sommiamo la durata di tutte le celle delle righe del cartellino del giorno in esame
                    foreach (var cell in timesheetRowsToSum) //O(n) dove n è il numero di righe del cartellino di cui fare la somma
                    {
                        TimeSpan durata = cell.ElementAt(cellIndex).Total;

                        totaleDelGiornoTemporaneo = totaleDelGiornoTemporaneo.Add(durata);
                    }

                    cellIndex++;

                    //Viene calcolato il delta (provvisorio perchè dobbiamo ancora vedere se viene autorizzato nel caso sia positivo)
                    var deltaProvvisorio = totaleDelGiornoTemporaneo.Subtract(timesheetCell.Total);

                    rigaDelta.Add(new TimesheetCell() { Date = timesheetCell.Date, Total = deltaProvvisorio });
                }
            }

            if (RepoManager.ParamRepo.ParametersRow.Abilita_Aut_Str)
                AutStrHelper.CalculateDeltaRowWithAutStr(rigaDelta, BaseEntity.Col_Id);

            TimesheetRows.AddRow(rigaDelta);

        }
        void GeneraRigaArrotondamenti()
        {
            var oreArrotondamenti = _registrazioni.OreArrotondamenti().PerGiornoConDurata();

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = true;
            rowConfig.RowCode = CHIAVE_ORE_ARROTONDAMENTI;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT_DURATA);

            TimesheetRoundingHoursRow riga = new TimesheetRoundingHoursRow(rowConfig);

            FillTimesheetRowAndAdd(oreArrotondamenti, riga);
        }
        void GeneraRigaTotale()
        {
            var righeDaTotalizzare = TimesheetRows.SummableRows;

            var days = CommonService.EachDay(MinDate, MaxDate).ToList();

            int cellIndex = days.Count();

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = Parameters.Visualizza_Totale;
            rowConfig.RowCode = CHIAVE_ORE_TOTALE;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE);

            TimesheetTotalRow rigaTotale = new TimesheetTotalRow(rowConfig);

            for (int dayIndex = 0; dayIndex < cellIndex; dayIndex++)
            {
                TimeSpan totaleDelGiorno = TimeSpan.Zero;

                foreach (var riga in righeDaTotalizzare)
                    totaleDelGiorno = totaleDelGiorno.Add(riga[dayIndex].Total);

                rigaTotale.Add(new TimesheetCell { Date = days[dayIndex], Total = totaleDelGiorno });
            }

            TimesheetRows.AddRow(rigaTotale);

        }

        #endregion

        #endregion

        IDictionary<DateTime, TimeSpan> GetRangePlanDuration(Col collaboratore, DateTime minDate, DateTime maxDate)
        {
            Dictionary<DateTime, TimeSpan> rangePlanDuration = new Dictionary<DateTime, TimeSpan>();

            if (collaboratore.Tab_Orari_Tipo == null)
                return rangePlanDuration;

            IEnumerable<Tab_Orari> colPlans = collaboratore.Tab_Orari_Tipo.Tab_Orari.Where(or => or.Data_Inizio <= minDate).ToList();

            DateTime maxPlanDate = colPlans.Max(or => or.Data_Inizio);

            colPlans = colPlans.Where(or => or.Data_Inizio == maxPlanDate).ToList();

            IEnumerable<DateTime> daysToCheck = CommonService.EachDay(minDate, maxDate).ToList();

            foreach (DateTime day in daysToCheck)
            {
                TimeSpan planDayDuration = TimeSpan.Zero;

                foreach (Tab_Orari colPlan in colPlans)
                {
                    if (BusinessService.IsToApplyPlan(colPlan, day))
                    {
                        var planMinutes = colPlan.HasTimeBorders ? (colPlan.Ora_U.Value.Subtract(colPlan.Ora_E.Value).TotalMinutes) : Convert.ToDouble(colPlan.Durata_Minuti);

                        planDayDuration = planDayDuration.Add(TimeSpan.FromMinutes(planMinutes));

                    }
                }

                rangePlanDuration.Add(day, planDayDuration);

            }

            return rangePlanDuration;
        }


        /// <summary>
        /// Calcola il monteminuti iterativamente ricalcolando i cartellini necessari 
        /// se la richiesta è di cartellini dopo la data blocco (altrimenti legge il monteminuti
        /// dalla relativa tabella dato che è stato sicuramente calcolato in precedenza).
        /// I casi sono 3 :
        /// 1 ->  data richiesta = mese immediatamente successivo alla data blocco
        /// 2 ->  data richiesta = mese uguale o precedente alla data blocco
        /// 3 ->  data richiesta = mese successivo alla data blocco (non il mese immediatamente successivo che è il caso 1)
        /// </summary>
        void CalcolaMonteMinuti()
        {
            //Estraggo l'ultimo giorno del mese richiesto per l'elaborazione (confronto con data blocco che è sempre
            //l'ultimo giorno del mese)
            var meseInElaborazione = CommonService.GetLastMonthDay(MonthRequested);

            var dataBlocco = _provider.GlobalParameters.Data_Blocco_Reg;
            //Monte minuti alla data blocco
            int monteMinutiCol = BaseEntity.Monte_Minuti;

            //Se il mese richiesto è quello successivo alla data blocco allora il MM del mese precedente è il monte minuti alla data blocco
            if (meseInElaborazione == CommonService.GetLastMonthDay(dataBlocco.Value.AddMonths(1)))
            {
                _monteMinuti = TimeSpan.FromMinutes(monteMinutiCol);
                return;
            }

            var mesiDaCalcolare = Enumerable.Empty<DateTime>();

            //Se il mese richiesto è inferiore o uguale alla data blocco
            //calcolo i mesi per i quali mi serve il delta da sottrarre poi al monte minuti del coll alla data blocco
            if (meseInElaborazione <= dataBlocco)
                mesiDaCalcolare = CommonService.EachMonth(meseInElaborazione, dataBlocco.Value).ToList();

            //Se il mese richiesto è superiore alla data blocco
            //calcolo i mesi per i quali mi serve il delta da aggiungere poi al monte minuti del coll alla data blocco
            if (meseInElaborazione > dataBlocco)
                mesiDaCalcolare = CommonService.EachMonth(dataBlocco.Value.AddDays(1), meseInElaborazione.AddMonths(-1)).ToList();

            var minYear = mesiDaCalcolare.Min().Year;
            var maxYear = mesiDaCalcolare.Max().Year;

            //Query per le righe monteminuti di quel collaboratore e degli anni necessari
            IEnumerable<Col_Monte_Minuti> recordsMonteMinuti = _provider.GetColMonteMinuti(BaseEntity, minYear, maxYear);

            //Per ogni mese calcolo il delta
            foreach (var month in mesiDaCalcolare)
            {
                //Se il mese richiesto è inferiore alla data blocco allora estraggo il delta già calcolato 
                //e lo sottraggo al monte minuti 
                if (dataBlocco.Value >= meseInElaborazione)
                {
                    var recordMonteMinuti = recordsMonteMinuti.First(mm => mm.Anno_Col_Monte_Minuti == month.Year);
                    var monteMinutiDelMese = (int)CommonService.GetPropertyValue(recordMonteMinuti, $"M{month.Month.ToString("00")}_Col_Monte_Minuti");
                    monteMinutiCol -= monteMinutiDelMese;
                }
                else
                {
                    //Se il mese richiesto è superiore alla data blocco allora calcolo il cartellino
                    //e aggiungo il delta al monte minuti 

                    var minDate = CommonService.GetFirstMonthDay(month);
                    var maxDate = CommonService.GetLastMonthDay(month);

                    var timesheetConfig = new TimesheetConfig<Col>
                    {
                        Entità = BaseEntity,
                        MinDate = minDate,
                        MaxDate = maxDate,
                        MonthRequested = month,
                        Params = Parameters
                    };

                    var cartellino = new ColTimesheet(timesheetConfig, _provider);

                    cartellino.CalculateMonteMinuti = false; //Evitiamo la ricorsione infinita

                    cartellino.BeforeCompute();
                    cartellino.Compute();
                    cartellino.AfterCompute();

                    monteMinutiCol += (int)cartellino.TotalFromDeltaHours.TotalMinutes;
                }
            }

            _monteMinuti = TimeSpan.FromMinutes(monteMinutiCol);

        }

    }
}
