using Business.BusinessClasses.CartellinoServiceDTOs;
using Domain;
using System;
using System.Collections.Generic;

namespace Business.BusinessServices.TimesheetService.Providers.Registrations.Interfaces
{
    internal interface ITimesheetDataProvider
    {
        Param GlobalParameters { get; }

        IEnumerable<CartellinoRegV> GetTimesheetRegistrations(Cant cantiere, DateTime minDate, DateTime maxDate);
        IEnumerable<CartellinoRegV> GetTimesheetRegistrations(Col collaboratore, DateTime minDate, DateTime maxDate);
        IEnumerable<Col_Monte_Minuti> GetColMonteMinuti(Col collaboratore, int minYear, int maxYear);
    }
}
