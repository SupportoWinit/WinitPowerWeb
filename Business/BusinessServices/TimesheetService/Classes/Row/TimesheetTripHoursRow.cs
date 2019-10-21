
using Business.BusinessServices.TimesheetService.Classes.RowConfig;
using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row;
using Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.CartellinoService.Classes.Riga
{
    internal class TimesheetTripHoursRow : TimesheetRow
    {
        public override TimesheetRowType RowTypeKey
        {
            get
            {
                return TimesheetRowType.OreViaggi;
            }
        }

        #region Ctor

        public TimesheetTripHoursRow(TimesheetRowConfiguration config) : base(config)
        {

        }

        #endregion

        
    }
}
