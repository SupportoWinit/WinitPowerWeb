using Business.BusinessClasses.CartellinoServiceDTOs;
using Business.BusinessServices.TimesheetService.Classes.Cell;
using Business.BusinessServices.TimesheetService.Classes.Config;
using Business.BusinessServices.TimesheetService.Classes.RowsManager;
using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row;
using Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Business.BusinessServices.TimesheetService.Abstracts
{
    internal abstract class TimesheetBase<T> where T : class
    {
        #region CONFIG

        private TimesheetConfig<T> _config;

        #endregion

        #region CHIAVI RIGHE

        protected virtual string CHIAVE_ORE_LAVORATE
        {
            get { return "O-L"; }
        }
        protected virtual string CHIAVE_ORE_PIANO
        {
            get { return "O-P"; }
        }
        protected virtual string CHIAVE_ORE_DELTA
        {
            get { return "O-D"; }
        }
        protected virtual string CHIAVE_ORE_VIAGGI
        {
            get { return "O-V"; }
        }
        protected virtual string CHIAVE_ORE_ARROTONDAMENTI
        {
            get { return "O-A"; }
        }
        protected virtual string CHIAVE_ORE_RETTIFICHE_MANUALI
        {
            get { return "O-RM"; }
        }
        protected virtual string CHIAVE_ORE_RETTIFICHEAUTO
        {
            get { return "O-RA"; }
        }

        protected virtual string CHIAVE_ORE_TOTALE
        {
            get { return "O-T"; }
        }

        #endregion

        #region DATA MINIMA-MASSIMA
        
        protected DateTime MinDate { get { return _config.MinDate; } }
        protected DateTime MaxDate { get { return _config.MaxDate; } }
        protected DateTime MinSummaryDate { get { return _config.MinSummaryDate; } }
        protected DateTime MaxSummaryDate { get { return _config.MaxSummaryDate; } }
        protected DateTime MonthRequested { get { return _config.MonthRequested; } }

        #endregion
        
        #region RIGHE CARTELLINO

        private TimesheetRowsManager _timesheetRows;

        protected TimesheetRowsManager TimesheetRows { get { return _timesheetRows; } }

        #endregion
        
        protected void FillTimesheetRowAndAdd(IDictionary<DateTime, TimeSpan> orePerGiornoConDurata, TimesheetRow riga)
        {
            foreach (var giorno in CommonService.EachDay(MinDate, MaxDate))
            {
                TimeSpan durata = TimeSpan.Zero;

                orePerGiornoConDurata.TryGetValue(giorno, out durata);

                riga.Add(new TimesheetCell() { Date = giorno, Total = durata });
            }

            _timesheetRows.AddRow(riga);
        }

        #region PARAMETRI

        protected TimesheetParams Parameters { get { return _config.Params; } }

        #endregion

        #region ENTITA' DI RIFERIMENTO
        
        protected T BaseEntity { get { return _config.Entità; } }

        #endregion

        #region PROPRIETA'

        public TimeSpan TotalFromPlanHours { get { return GetTotal(TimesheetRowType.Orario); } }

        public TimeSpan TotalFromRoundingHours { get { return GetTotal(TimesheetRowType.OreArrotondamenti); } }

        public TimeSpan TotalFromWorkedHours { get { return GetTotal(TimesheetRowType.OreLavorate); } }

        public TimeSpan TotaleFromMotivatedHours { get { return GetTotal(TimesheetRowType.OreMotivate); } }

        public TimeSpan TotalFromTripsHours{ get { return GetTotal(TimesheetRowType.OreViaggi); } }

        public TimeSpan TotalFromManualRettHours { get { return GetTotal(TimesheetRowType.OreRettificheM); } }

        public TimeSpan TotalFromAutoRettHours { get { return GetTotal(TimesheetRowType.OreRettificheA); } }

        public TimeSpan TotalFromDeltaHours { get { return GetTotal(TimesheetRowType.Delta); } }

        #endregion

        #region CTOR


        /// <summary>
        /// Costruttore privato utilizzato per l'inizializzazione delle varie variabili
        /// </summary>
        private TimesheetBase()
        {
            _timesheetRows = new TimesheetRowsManager();
        }

        public TimesheetBase(TimesheetConfig<T> config) : this()
        {
            _config = config;
        }

        #endregion

        #region JSON

        public virtual JObject ToJson()
        {
            JObject jsonCartellino = new JObject();

            JArray righeCartellino = new JArray();

            foreach (TimesheetRow riga in _timesheetRows.VisibleRows)
            {
                var json = riga.ToJson();
                righeCartellino.Add(json);
            }

            jsonCartellino.Add("dataSource", righeCartellino);

            return jsonCartellino;
        }

        #endregion

        #region PROTECTED METHODS

        protected void CalculateWeekendTotals()
        {
            if (!Parameters.Visualizza_Totali_Settimanali)
                return;

            foreach (var timesheetRow in _timesheetRows.AllRows)
            {
                TimeSpan weekendTotal = TimeSpan.Zero;

                foreach (var timesheetCell in timesheetRow)
                {
                    weekendTotal = weekendTotal.Add(timesheetCell.Total);

                    if (timesheetCell.Date.DayOfWeek == DayOfWeek.Sunday)
                    {
                        timesheetRow.WeekendTotals.Add(new TimesheetCell { Date = timesheetCell.Date, Total = weekendTotal });
                        weekendTotal = TimeSpan.Zero;
                    }
                }
            }
        }
        
        #endregion

        #region METODI PRIVATI

        private TimeSpan GetTotal(TimesheetRowType tipoRiga)
        {
            return _timesheetRows.GetTimesheetRowsTypeTotal(tipoRiga);
        }

        #endregion
    }
}
