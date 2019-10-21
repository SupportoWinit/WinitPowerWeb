using Business.BusinessServices.TimesheetService.Classes.Cell;
using Business.BusinessServices.TimesheetService.Classes.RowConfig;
using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row;
using Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.TimesheetService.Classes.Row
{
    internal class TimesheetDeltaRow : TimesheetRow
    {
        public override TimesheetRowType RowTypeKey
        {
            get
            {
                return TimesheetRowType.Delta;
            }
        }
        
        #region Ctor

        public TimesheetDeltaRow(TimesheetRowConfiguration config) : base(config)
        {

        }

        #endregion

        public static TimesheetDeltaRow Default(TimesheetRowConfiguration config)
        {
            var deltaRow = new TimesheetDeltaRow(config);

            foreach (var data in CommonService.EachDay(CommonService.GetFirstMonthDay(config.MinDate), CommonService.GetLastMonthDay(config.MinDate)))
            {
                deltaRow.Add(new TimesheetCell { Date = data, Total = TimeSpan.Zero });
            }

            return deltaRow;
        }




    }
}
