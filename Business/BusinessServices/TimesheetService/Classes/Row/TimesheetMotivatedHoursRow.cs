using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row;
using System;
using Common;
using Newtonsoft.Json.Linq;
using Business.BusinessServices.TimesheetService.Helpers;
using System.Linq;
using Business.BusinessServices.TimesheetService.Classes.RowConfig;

namespace Business.BusinessServices.CartellinoService.Classes.Riga
{
    internal class TimesheetMotivatedHoursRow : TimesheetRow
    {
        #region Properties

        public override TimesheetRowType RowTypeKey
        {
            get
            {
                return TimesheetRowType.OreMotivate;
            }
        }
        
        #endregion

        #region Ctor

        public TimesheetMotivatedHoursRow(TimesheetRowConfiguration config) : base(config)
        {

        }

        #endregion

        public override JObject ToJson()
        {
            JObject riga = new JObject();

            riga.Add(TIMESHEET_ROW_TYPE, (int)RowTypeKey);

            riga.Add(TIMESHEET_ROW_CODE, RowCode);
            riga.Add(TIMESHEET_ROW_DESCRIPTION, CartellinoHelper.MotivazioniByKey[RowCode].Decodifica_Tab);
            riga.Add(TIMESHEET_ROW_GROUPED_ENTITY_ID, RowGroupedEntityId);
            riga.Add(TIMESHEET_ROW_GROUPED_ENTITY_DESCRIPTION, RowGroupedEntityDescription);

            ForEach(durata =>
            {
                var date = CartellinoHelper.FormatDate(durata.Date);
                var duration = CartellinoHelper.FormatDuration(durata.Total);
                riga.Add(date, duration);
            });

            riga.Add(TIMESHEET_ROW_TOTAL_DAYS, RowCount);
            riga.Add(TIMESHEET_ROW_TOTAL, CartellinoHelper.FormatDuration(RowTotal));

            if (WeekendTotals.Any())
            {
                int weekNumber = 1;

                foreach (var totaleSettimanale in WeekendTotals)
                {
                    riga.Add($"WeekTotal{weekNumber}", CartellinoHelper.FormatDuration(totaleSettimanale.Total));
                    weekNumber++;
                }
            }

            return riga;
        }

       
    }
}
