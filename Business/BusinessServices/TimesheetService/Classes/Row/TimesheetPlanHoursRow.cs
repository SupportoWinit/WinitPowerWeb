using System;
using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row;
using Common;
using Business.BusinessServices.TimesheetService.Classes.RowConfig;

namespace Business.BusinessServices.CartellinoService.Classes.Riga
{
    internal class TimesheetPlanHoursRow : TimesheetRow
    {
        public override TimesheetRowType RowTypeKey
        {
            get
            {
                return TimesheetRowType.Orario;
            }
        }

        #region Ctor

        public TimesheetPlanHoursRow(TimesheetRowConfiguration config) : base(config)
        {
           
        }

        #endregion

    }
}

