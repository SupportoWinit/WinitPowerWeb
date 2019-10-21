using Business.BusinessServices.TimesheetService.Classes.Cell;
using Business.BusinessServices.TimesheetService.Classes.RowConfig;
using Business.BusinessServices.TimesheetService.Helpers;
using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Cell;
using Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row
{
    internal abstract class TimesheetRow : List<ITimesheetCell>
    {
        #region CONSTANTS

        protected const string TIMESHEET_ROW_TYPE = "tipoRiga";
        protected const string TIMESHEET_ROW_CODE = "codiceRiga";
        protected const string TIMESHEET_ROW_DESCRIPTION = "descrizioneCodiceRiga";
        protected const string TIMESHEET_ROW_GROUPED_ENTITY_ID = "idEntitàRaggruppata";
        protected const string TIMESHEET_ROW_GROUPED_ENTITY_DESCRIPTION = "descrizioneEntitàRaggruppata";
        protected const string TIMESHEET_ROW_TOTAL_DAYS = "totalDays";
        protected const string TIMESHEET_ROW_TOTAL = "totale";

        #endregion

        private TimesheetRowConfiguration _rowConfig;

        private ICollection<ITimesheetCell> _weekendTotals;
        public ICollection<ITimesheetCell> WeekendTotals
        {
            get
            {
                if (_weekendTotals == null)
                    _weekendTotals = new List<ITimesheetCell>();

                return _weekendTotals;
            }
        }

        #region PUBLIC PROPERTIES

        public DateTime MinDate
        {
            get
            {
                return _rowConfig.MinDate;
            }
        }

        public DateTime MaxDate
        {
            get
            {
                return _rowConfig.MaxDate;
            }
        }

        public abstract TimesheetRowType RowTypeKey
        {
            get;
        }

        public string RowCode
        {
            get
            {
                return _rowConfig.RowCode;
            }
        }

        public string RowDescription
        {
            get
            {
                return _rowConfig.RowDescription;
            }
        }

        public Nullable<int> RowGroupedEntityId
        {
            get
            {
                return _rowConfig.RowGroupedEntityId;
            }
        }

        public string RowGroupedEntityDescription
        {
            get
            {
                return _rowConfig.RowGroupedEntityDescription;
            }
        }

        public bool IsToShow
        {
            get
            {
                return _rowConfig.IsToShow;
            }

        }

        #endregion

        #region PROTECTED PROPERTIES

        public TimeSpan RowTotal
        {
            get
            {
                return Totalize();
            }
        }

        public int RowCount
        {
            get
            {
                return TotalCount();
            }
        } 

        #endregion

        #region CTOR

        public TimesheetRow(TimesheetRowConfiguration rowConfig)
        {
            _weekendTotals = new List<ITimesheetCell>();
            _rowConfig = rowConfig;
        }

        #endregion

        #region PUBLIC METHODS

        public virtual JObject ToJson()
        {
            JObject riga = new JObject();

            riga.Add(TIMESHEET_ROW_TYPE, (int)RowTypeKey);
            riga.Add(TIMESHEET_ROW_CODE, RowCode);
            riga.Add(TIMESHEET_ROW_DESCRIPTION, RowDescription);
            riga.Add(TIMESHEET_ROW_GROUPED_ENTITY_ID, RowGroupedEntityId);
            riga.Add(TIMESHEET_ROW_GROUPED_ENTITY_DESCRIPTION, RowGroupedEntityDescription);

            this.Where(cell => cell.Date >= MinDate && cell.Date < MaxDate.AddDays(1))
            .ToList()
            .ForEach(durata =>
            {
                var date = CartellinoHelper.FormatDate(durata.Date);
                var duration = CartellinoHelper.FormatDuration(durata.Total);
                riga.Add(date, duration);

            });
            riga.Add("totalDays", RowCount);
            riga.Add(TIMESHEET_ROW_TOTAL, CartellinoHelper.FormatDuration(RowTotal));

            if (_weekendTotals.Any())
            {
                int weekNumber = 1;

                foreach (var weekendTotal in _weekendTotals)
                {
                    riga.Add($"WeekTotal{weekNumber}", CartellinoHelper.FormatDuration(weekendTotal.Total));
                    weekNumber++;
                }
            }

            return riga;

        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        /// Ritorna il numero totale di giorni della riga la cui selezione viene fatta tra MinDate e MaxDate della riga.
        /// </summary>
        /// <returns></returns>
        public int TotalCount()
        {
            return this.Where(cell => cell.Date >= MinDate && cell.Date <= MaxDate && cell.Total != TimeSpan.Zero)
                      .Count();
        }

        /// <summary>
        /// Ritorna il totale della riga la cui selezione viene fatta tra MinDate e MaxDate della riga.
        /// </summary>
        /// <returns></returns>
        public TimeSpan Totalize()
        {
            return this.Where(cell => cell.Date >= MinDate && cell.Date <= MaxDate)
                       .Select(cell => cell.Total)
                       .DefaultIfEmpty()
                       .Aggregate(TimeSpan.Zero, (total, cellTotal) => total.Add(cellTotal));
        }

        #endregion

    }
}
