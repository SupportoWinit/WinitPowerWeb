using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.TimesheetService.Classes.RowConfig
{
    internal class TimesheetRowConfiguration
    {
        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }
        public TimesheetRowType RowTypeKey { get; set; }
        public string RowCode { get; set; }
        public string RowDescription { get; set; }
        public Nullable<int> RowGroupedEntityId { get; set; }
        public string RowGroupedEntityDescription { get; set; }
        public bool IsToShow { get; set; }
    }
}
