using Domain;
using System;

namespace Business.Repository.Custom
{
    public interface ITimesheetRepository : IRepository<Timesheet>
    {
        Timesheet mergeTimesheets(Timesheet timesheet1, Timesheet timesheet2, string justification, int colId, DateTime month);
    }

}