
using Business.BusinessServices.TimesheetService;
using Business.Repository;
using Common;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.ParamService
{
    public class ParamService : IParamService
    {
        ITimesheetService _timesheetService;

        public ParamService() : this(new TimesheetService.TimesheetService())
        {

        }

        public ParamService(ITimesheetService timesheetService)
        {
            _timesheetService = timesheetService;
        }

        public Param Load()
        {
            return RepoManager.ParamRepo.First(true);
        }

        public void UpdateBlockDate(DateTime newBlockDate)
        {
            var blockDateMonthEnd = CommonService.GetLastMonthDay(newBlockDate);
            var currentBlockDate = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.Value;

            IEnumerable<DateTime> monthsToCycle = null;

            if (blockDateMonthEnd > currentBlockDate)
                monthsToCycle = CommonService.EachMonth(currentBlockDate, newBlockDate).ToList();
            else
                monthsToCycle = CommonService.EachMonth(newBlockDate, currentBlockDate).ToList();

            Expression<Func<Col, bool>> queryWhere = null;

            if (RepoManager.ParamRepo.ParametersRow.MonthlyHoursEnum == MothlyHoursEnum.Inclusive) //Se modo Inclusivo allora tratta i SOLI Collaboratori con Flag Monte Ore = True
                queryWhere = col => col.Flag_Monte_Ore;
            else //Se Modo ESCLUSIVO tratta TUTTI i Collaboratori SALVO quelli con Monte Ore = TRue
                queryWhere = col => !col.Flag_Monte_Ore;

            var collaboratori = RepoManager.ColRepo.Find(queryWhere).ToList();

            foreach (var date in monthsToCycle)
            {
                foreach (var col in collaboratori)
                {
                    //var cartellino =_timesheetService.ElaborateCartellino(col.Col_Id, date) as ColTimesheet;
                    //var delta = cartellino.TotalFromDeltaHours;
                    //var currentMonthDelta = date > currentBlockDate ? TimeSpan.FromMinutes(col.Monte_Minuti) + delta : TimeSpan.FromMinutes(col.Monte_Minuti) - delta;
                }
            }
        }

    }
}

