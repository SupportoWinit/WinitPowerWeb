using System;
using Common;
using Business.BusinessServices.TimesheetService.Classes.RowConfig;
using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row;

namespace Business.BusinessServices.CartellinoService.Classes.Riga
{
    internal class TimesheetWorkedHoursRow : TimesheetRow
    {
        public override TimesheetRowType RowTypeKey
        {
            get
            {
                return TimesheetRowType.OreLavorate;
            }
        }

        #region Ctor

        public TimesheetWorkedHoursRow(TimesheetRowConfiguration rowConfig) : base(rowConfig)
        {

        }

        #endregion




    }
}
