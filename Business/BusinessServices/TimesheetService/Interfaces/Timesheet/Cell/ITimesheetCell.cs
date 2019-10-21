using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Cell
{
    internal interface ITimesheetCell
    {
        DateTime Date { get; set; }
        TimeSpan Total { get; set; }
    }
}
