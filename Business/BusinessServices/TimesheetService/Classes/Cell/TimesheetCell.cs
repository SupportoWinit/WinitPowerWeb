using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Cell;
using System;

namespace Business.BusinessServices.TimesheetService.Classes.Cell
{
    internal class TimesheetCell : ITimesheetCell
    {
        public DateTime Date { get; set; }
        public TimeSpan Total { get; set; }
    }
}
