using System;
using System.Collections.Generic;
using Business.BusinessClasses;
using Business.BusinessClasses.CartellinoServiceDTOs;
using Business.BusinessServices.CartellinoService.Interfaces.Timesheet;
using Business.BusinessServices.TimesheetService.Enums;

namespace Business.BusinessServices.TimesheetService
{
    public interface ITimesheetService
    {
        ITimesheet ElaborateCartellino(int id, DateTime date, TimesheetParams cartellinoOptions);
        ITimesheet ElaborateCartellino(int id, DateTime date);
        IEnumerable<ITimesheet> ElaborateCartellino(IEnumerable<int> ids, DateTime date, TimesheetParams cartellinoOptions);
        CartellinoStrDTO ElaborateStrCartellino(IEnumerable<int> ColIds, DateTime date, TimesheetParams options);
        TimesheetParams TempParams { get; set; }
        TimesheetParams GetParams();
        ClientResponse SaveColCartellinoCellUpdate(TimesheetCellUpdate update);
        void SetParams(TimesheetParams parameters);
    }
}