
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row;
using Business.BusinessServices.TimesheetService.Classes.RowConfig;

namespace Business.BusinessServices.CartellinoService.Classes.Riga
{
    internal class TimesheetTotalRow : TimesheetRow
    {
        public TimesheetTotalRow(TimesheetRowConfiguration rowConfig) : base(rowConfig)
        {
        }
        
        public override TimesheetRowType RowTypeKey
        {
            get
            {
                return TimesheetRowType.Totale;
            }
        }

    }
}
