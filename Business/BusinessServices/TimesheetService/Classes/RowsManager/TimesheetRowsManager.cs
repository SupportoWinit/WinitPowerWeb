using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.TimesheetService.Classes.RowsManager
{
    internal class TimesheetRowsManager
    {
        ICollection<TimesheetRow> _timesheetRows;

        public IEnumerable<TimesheetRow> AllRows
        {
            get { return _timesheetRows; }
        }

        /// <summary>
        /// Ritorna solo le righe marcate per essere visibili.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TimesheetRow> VisibleRows
        {
            get
            {
                return _timesheetRows.Where(timesheetRow => timesheetRow.IsToShow).ToList();
            }
        }

        public TimesheetRowsManager()
        {
            _timesheetRows = new List<TimesheetRow>();
        }


        /// <summary>
        /// Ritorna solo le righe diverse da orario e delta.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TimesheetRow> SummableRows
        {
            get
            {
                return _timesheetRows.Where(timesheetRow =>
                                        timesheetRow.RowTypeKey != TimesheetRowType.Delta
                                        && timesheetRow.RowTypeKey != TimesheetRowType.Orario
                                        && timesheetRow.RowTypeKey != TimesheetRowType.Totale)
                                        .ToList();
            }
            
        }

        /// <summary>
        /// Ritorna solo le righe di un determinato tipo.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TimesheetRow> GetRows(TimesheetRowType timesheetRowTypeKey)
        {
            return _timesheetRows.Where(timesheetRow => timesheetRow.RowTypeKey == timesheetRowTypeKey).ToList();
        }



        /// <summary>
        /// Aggiunge una nuova riga.
        /// </summary>
        /// <returns></returns>
        public void AddRow(TimesheetRow timesheetRow)
        {
            _timesheetRows.Add(timesheetRow);
        }

        /// <summary>
        /// Ritorna la durata totale di un determinato tipo di riga.
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetTimesheetRowsTypeTotal(TimesheetRowType rowType)
        {
            return _timesheetRows.Where(riga => riga.RowTypeKey == rowType)
                                 .Select(riga => riga.Totalize())
                                 .Aggregate(TimeSpan.Zero, (subtotal, t) => subtotal.Add(t));
        }
    }
}
