using System;
using Common;
using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row;
using Business.BusinessServices.TimesheetService.Classes.RowConfig;

namespace Business.BusinessServices.CartellinoService.Classes.Riga
{
    internal class TimesheetRoundingHoursRow : TimesheetRow
    {
        public override TimesheetRowType RowTypeKey
        {
            get
            {
                return TimesheetRowType.OreArrotondamenti;
            }
        }

        public TimesheetRoundingHoursRow(TimesheetRowConfiguration config) : base(config)
        {
        }
        
       
    }
}
