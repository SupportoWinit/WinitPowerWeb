using Business.BusinessClasses.CartellinoServiceDTOs;

namespace Business.BusinessServices.TimesheetService.TimesheetUpdate.Interface
{
    public interface ITimesheetUpdate
    {
        void Update(TimesheetCellUpdate update);

        void UpdateRettifica(TimesheetCellUpdate update);
    }
}
