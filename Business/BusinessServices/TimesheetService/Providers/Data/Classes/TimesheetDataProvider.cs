using Business.BusinessServices.TimesheetService.Providers.Registrations.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Business.BusinessClasses.CartellinoServiceDTOs;
using Domain;
using Business.Repository;

namespace Business.BusinessServices.TimesheetService.Providers.Registrations.Classes
{
    internal class TimesheetDataProvider : ITimesheetDataProvider
    {
        public Param GlobalParameters
        {
            get
            {
                return RepoManager.ParamRepo.ParametersRow;
            }
        }

        public IEnumerable<Col_Monte_Minuti> GetColMonteMinuti(Col collaboratore, int minYear, int maxYear)
        {
            return RepoManager.Col_Monte_MinutiRepo.Find(mm => mm.Col_Id == collaboratore.Col_Id && mm.Anno_Col_Monte_Minuti >= minYear && mm.Anno_Col_Monte_Minuti <= maxYear).ToList();
        }

        public IEnumerable<CartellinoRegV> GetTimesheetRegistrations(Col collaboratore, DateTime minDate, DateTime maxDate)
        {
            return RepoManager.Reg_VRepo.GetRegsForCartellino(collaboratore, minDate, maxDate);
        }

        public IEnumerable<CartellinoRegV> GetTimesheetRegistrations(Cant cantiere, DateTime minDate, DateTime maxDate)
        {
            return RepoManager.Reg_VRepo.GetRegsForCartellino(cantiere, minDate, maxDate);
        }
    }
}
