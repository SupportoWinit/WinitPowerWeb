using Business.BusinessClasses.CartellinoServiceDTOs;
using Business.BusinessServices.TimesheetService.Classes.Cell;
using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row;
using Business.Repository;
using Common;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Business.BusinessServices.TimesheetService.Helpers
{
    internal static class CartellinoHelper
    {
        private static IDictionary<int, Tab_Decod> _motivazioni;

        public static IDictionary<int, Tab_Decod> Motivazioni
        {
            get
            {
                return _motivazioni;
            }
        }

        public static IDictionary<string, Tab_Decod> MotivazioniByKey
        {
            get
            {
                return _motivazioni.ToDictionary(key => key.Value.Chiave_Tab, value => value.Value);
            }
        }

        static CartellinoHelper()
        {
            _motivazioni = RepoManager.Tab_DecodRepo.Find(tab => tab.Nome_Tab == "MOTIVAZIONI").ToDictionary(c => c.Tab_Decod_Id);
        }

        #region Funzioni pubbliche

        /// <summary>
        /// Calcola il primo lunedì della settimana del giorno indicato.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public static DateTime CalculateMinDate(DateTime date, TimesheetParams options)
        {
            var minDate = CommonService.GetFirstMonthDay(date);

            var recoveryHoursCustomization = (TimesheetRecoveryHoursTypeEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheetRecoveryHoursTypeEnum);

            if (options.Visualizza_Totali_Settimanali || recoveryHoursCustomization == TimesheetRecoveryHoursTypeEnum.WeeklyHoursRecovery)
            {
                while (minDate.DayOfWeek != DayOfWeek.Monday)
                {
                    minDate = minDate.AddDays(-1);
                }
            }

            return minDate;
        }
        /// <summary>
        /// Calcola la domenica più vicina della settimana del giorno indicato.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public static DateTime CalculateMaxDate(DateTime date, TimesheetParams options)
        {
            var maxDate = CommonService.GetLastMonthDay(date);

            var recoveryHoursCustomization = (TimesheetRecoveryHoursTypeEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheetRecoveryHoursTypeEnum);

            if (options.Visualizza_Totali_Settimanali || recoveryHoursCustomization == TimesheetRecoveryHoursTypeEnum.WeeklyHoursRecovery)
            {
                while (maxDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    maxDate = maxDate.AddDays(1);
                }
            }
            
            return maxDate;
        }

        /// <summary>
        /// Calcola la domenica più vicina della settimana del giorno indicato.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public static DateTime CalculateMinSummaryDate(DateTime date, TimesheetParams options)
        {
            var minDate = CommonService.GetFirstMonthDay(date);

            if (options.Visualizza_Totali_Settimanali)
            {
                while (minDate.DayOfWeek != DayOfWeek.Monday)
                {
                    minDate = minDate.AddDays(-1);
                }
            }

            return minDate;
        }

        /// <summary>
        /// Calcola la domenica più vicina della settimana del giorno indicato.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public static DateTime CalculateMaxSummaryDate(DateTime date, TimesheetParams options)
        {
            var maxDate = CommonService.GetLastMonthDay(date);

            if (options.Visualizza_Totali_Settimanali)
            {
                while (maxDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    maxDate = maxDate.AddDays(1);
                }
            }

            return maxDate;
        }

        /// <summary>
        /// Aggiunge alla riga del cartellino i totali settimanali.
        /// </summary>
        public static void CalculateWeekendTotal(TimesheetRow riga)
        {
            TimeSpan totaleSettimanale = TimeSpan.Zero;

            foreach (var giorno in riga)
            {
                totaleSettimanale = totaleSettimanale.Add(giorno.Total);

                if (giorno.Date.DayOfWeek == DayOfWeek.Sunday)
                {
                    riga.WeekendTotals.Add(new TimesheetCell { Date = giorno.Date, Total = totaleSettimanale });
                    totaleSettimanale = TimeSpan.Zero;
                }
            }

        }

        /// <summary>
        /// Formatta la durata per la rappresentazione client nel seguente formato:
        /// "00:00"
        /// In caso di durata nulla ritorna '-' .
        /// </summary>
        public static string FormatDuration(TimeSpan time)
        {
            if (time == TimeSpan.Zero)
                return "-";

            string minus = "";

            if (time < TimeSpan.Zero)
                minus = "-";

            var days = Math.Abs(time.Days);
            var hours = Math.Abs(time.Hours);
            var minutes = Math.Abs(time.Minutes);

            return string.Format("{0}{1}:{2}", minus, (days * 24 + hours).ToString("00"), minutes.ToString("00"));
        }

        /// <summary>
        /// Formatta la data in una stringa con la seguente pattern:
        /// "yyyy'-'MM'-'dd'T'HH':'mm':'ss"
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        public static string FormatDate(DateTime date)
        {
            return date.ToString("s");
        }

        public static bool IsValidDay(Tab_Orari orario, DateTime date)
        {
            // sposto avanti la data di inzio in base alla sequenza di modo da poter
            // gestire le alternanze rispetto alla data di inzio stessa
            var newDtInizio = orario.Data_Inizio.AddDays(orario.Sequenza * 7);

            TimeSpan delta = date - newDtInizio;

            bool valid = ((delta.Days / 7) % orario.Ripetizione) == 0 && delta.Days >= 0;

            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    valid = valid && orario.G1;
                    break;
                case DayOfWeek.Tuesday:
                    valid = valid && orario.G2;
                    break;
                case DayOfWeek.Wednesday:
                    valid = valid && orario.G3;
                    break;
                case DayOfWeek.Thursday:
                    valid = valid && orario.G4;
                    break;
                case DayOfWeek.Friday:
                    valid = valid && orario.G5;
                    break;
                case DayOfWeek.Saturday:
                    valid = valid && orario.G6;
                    break;
                case DayOfWeek.Sunday:
                    valid = valid && orario.G7;
                    break;
                default:
                    valid = false;
                    break;
            }
            return valid;
        }

        #endregion

    }


}
