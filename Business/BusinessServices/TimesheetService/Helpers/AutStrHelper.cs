using Business.BusinessServices.TimesheetService.Interfaces.Timesheet.Row;
using Business.Repository;
using Common;
using Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Business.BusinessServices.TimesheetService.Helpers
{
    public static class AutStrHelper
    {
        /// <summary>
        /// Calcola il totale giornaliero se l'autorizzazione straordinari è abilitata
        /// e se vi sono le condizioni per calcolarlo.
        /// </summary>
        /// <param name="dayTotal">Totale del giorno.</param>
        /// <param name="startDate">Giorno.</param>
        /// <param name="col">Collaboratore in questione</param>
        /// <returns></returns>
        public static TimeSpan GetDeltaDayTotalWithAutStr(TimeSpan deltaAmount, DateTime startDate, int col_Id)
        {
            if (!(RepoManager.ParamRepo.ParametersRow.Abilita_Aut_Str
                && RepoManager.ParamRepo.ParametersRow.Aut_Str_Tipo.HasValue
                && RepoManager.ParamRepo.ParametersRow.Aut_Str_Tipo.Value == 1
                && deltaAmount > TimeSpan.Zero))
            {
                return deltaAmount;
            }

            // si recupera per quanti minuti quel collaboratore risulta autorizzato agli straordinari in quella data
            TimeSpan authorizedMinutes = TimeSpan.FromMinutes(RepoManager.Aut_StrRepo.AutStrColAuthorization(col_Id, startDate) * 60);

            if (deltaAmount > authorizedMinutes)
            {
                deltaAmount = authorizedMinutes;
            }

            return deltaAmount;
        }

        internal static void CalculateDeltaRowWithAutStr(TimesheetRow rigaDelta, int colId)
        {
            TimesheetRecoveryHoursTypeEnum RecoveryCustomization = (TimesheetRecoveryHoursTypeEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheetRecoveryHoursTypeEnum);

            if (RecoveryCustomization != TimesheetRecoveryHoursTypeEnum.NoHoursRecovery) //Modificare
            {
                CalculateRecoveryWithAutStr(colId, rigaDelta);
            }
            else
            {
                CalculateDeltaRowWithAutStr(colId, rigaDelta);
            }

        }

        internal static void CalculateDeltaRowWithAutStr(int entityId, TimesheetRow riga)
        {
            int cellCount = riga.Count();

            for (int i = 0; i < cellCount; i++)
            {
                DateTime data = riga[i].Date;
                TimeSpan delta = riga[i].Total;

                TimeSpan deltaAutorizzato = GetDeltaDayTotalWithAutStr(delta, data, entityId);

                riga[i].Total = deltaAutorizzato;
            }
        }

        internal static void CalculateRecoveryWithAutStr(int entityId, TimesheetRow riga)
        {
            var weeks = CalcolaSettimane(riga);

            foreach (var settimana in weeks)
            {
                TimeSpan weekRecoveryMinutes = CalculateWeekRecoveryMinutes(riga, entityId, settimana.Start, settimana.End);

                if (weekRecoveryMinutes != TimeSpan.Zero)
                {
                    foreach (var data in CommonService.EachDay(settimana.Start, settimana.End))
                    {
                        TimeSpan deltaCorrente = riga.First(cell => cell.Date == data).Total;

                        TimeSpan minutiAutorizzati = TimeSpan.FromMinutes(RepoManager.Aut_StrRepo.AutStrColAuthorization(entityId, data) * 60);

                        //vengono estratti il numero di minuti di straordinari non autorizzati
                        TimeSpan minutiNonAutorizzati = GetUnautorizedDeltaMinutes(riga, data, entityId);

                        if(deltaCorrente < TimeSpan.Zero)
                        {

                            int indiceDelta = riga.FindIndex(cell => cell.Date == data);

                            riga[indiceDelta].Total = deltaCorrente;

                            continue;
                        }

                        if (deltaCorrente > TimeSpan.Zero && minutiAutorizzati == TimeSpan.Zero && minutiNonAutorizzati > TimeSpan.Zero)
                        {
                            int indiceDelta = riga.FindIndex(cell => cell.Date == data);

                            riga[indiceDelta].Total = TimeSpan.Zero;
                        }

                        if (weekRecoveryMinutes == TimeSpan.Zero)
                            continue;

                        if (deltaCorrente - weekRecoveryMinutes >= TimeSpan.Zero)
                        {
                            int indiceDelta = riga.FindIndex(cell => cell.Date == data);

                            riga[indiceDelta].Total = weekRecoveryMinutes;

                            weekRecoveryMinutes = TimeSpan.Zero;
                        }
                        else if (deltaCorrente - weekRecoveryMinutes < TimeSpan.Zero)
                        {
                            int indiceDelta = riga.FindIndex(cell => cell.Date == data);

                            riga[indiceDelta].Total = TimeSpan.Zero;

                            weekRecoveryMinutes -= deltaCorrente;
                        }
                    }
                }
                else
                {
                    foreach (var data in CommonService.EachDay(settimana.Start, settimana.End))
                    {
                        int indiceDelta = riga.FindIndex(cell => cell.Date == data);

                        DateTime dataDelta = riga[indiceDelta].Date;
                        TimeSpan delta = riga[indiceDelta].Total;

                        TimeSpan deltaAutorizzato = GetDeltaDayTotalWithAutStr(delta, data, entityId);

                        riga[indiceDelta].Total = deltaAutorizzato;
                    }
                }
            }
        }

        private static TimeSpan CalculateWeekRecoveryMinutes(TimesheetRow riga, int entityId, DateTime start, DateTime end)
        {
            TimeSpan returnValue = TimeSpan.Zero;

            // si calcola il numero di straordinari autorizzati effettuati in settimana
            TimeSpan autorizedDeltaMinutes = GetWeekAutorizedDeltaMinutes(riga, entityId, start, end);

            // si calcola il numero di minuti delta non autorizzati per la settimana (delta positivo)
            TimeSpan unautorizedDeltaMinutes = GetWeekUnautorizedDeltaMinutes(riga, entityId, start, end);

            // si calcola il numero di minuti con delta negativo nel corso della settimana
            TimeSpan negativeDeltaMinutes = GetWeekDeltaNegativeMinutes(riga, entityId, start, end);

            // si hanno dei minuti da recuperare solamente se ci sono dei minuti da ripartire (straordinari non autorizzati) e dei luoghi
            // in cui ripartirli (giorni con delta negativo); inoltre viene verificato che già il numero di minuti di delta negativo non superi
            // quanto già effettuato come straordinario autorizzato
            if (unautorizedDeltaMinutes != TimeSpan.Zero && negativeDeltaMinutes != TimeSpan.Zero && autorizedDeltaMinutes < negativeDeltaMinutes)
            {
                // calcolo il totale dei minuti da recuperare a completamento dei delta negativi:
                // 1- se il numero di minuti non autorizzati è inferiore o uguale al numero di minuti in delta negativo, il
                //   numero di minuti da ripartire è il numero di minuti non autorizzati come straordinario (si recupera tutto il recuperabile)
                // 2- se il numero di minuti non autorizzati è maggiore al numero di minuti in delta negativo, il numero di minuti
                //   da ripartire è il numero di minuti di delta negativo (si recupera quanto è stato fatto in meno)
                returnValue = unautorizedDeltaMinutes <= negativeDeltaMinutes - autorizedDeltaMinutes ? unautorizedDeltaMinutes : negativeDeltaMinutes;

            }

            return returnValue;
        }




        private static TimeSpan GetWeekDeltaNegativeMinutes(TimesheetRow riga, int entityId, DateTime start, DateTime end)
        {
            TimeSpan returnValue = TimeSpan.Zero;

            // si ritorna la somma dei delta negativi presenti nel corso della settimana
            foreach (DateTime weekDay in CommonService.EachDay(start, end))
            {
                TimeSpan deltaMinutes = GetDeltaMinutesWithoutAutStr(riga, weekDay, entityId);
                if (deltaMinutes < TimeSpan.Zero)
                    returnValue += deltaMinutes.Duration();
            }

            return returnValue;
        }

        private static TimeSpan GetWeekUnautorizedDeltaMinutes(TimesheetRow riga, int entityId, DateTime start, DateTime end)
        {
            TimeSpan returnValue = TimeSpan.Zero;

            // si ritorna la somma dei delta non autorizzati di tutta la settimana
            foreach (DateTime weekDay in CommonService.EachDay(start, end))
                returnValue += GetUnautorizedDeltaMinutes(riga, weekDay, entityId);

            return returnValue;
        }

        private static TimeSpan GetWeekAutorizedDeltaMinutes(TimesheetRow riga, int entityId, DateTime start, DateTime end)
        {
            TimeSpan returnValue = TimeSpan.Zero;

            // si ritorna la somma dei delta negativi presenti nel corso della settimana
            foreach (DateTime weekDay in CommonService.EachDay(start, end))
            {
                var cella = riga.First(cell => cell.Date == weekDay);
                returnValue += GetAutorizedDeltaMinutes(weekDay, cella.Total, entityId);
            }
            return returnValue;
        }


        private static TimeSpan GetAutorizedDeltaMinutes(DateTime weekDay, TimeSpan delta, int entityId)
        {
            TimeSpan autorizedDelta = TimeSpan.Zero;

            // si recupera per quanti minuti quel collaboratore risulta autorizzato agli straordinari in quella data
            TimeSpan autorizedTime = TimeSpan.FromMinutes(RepoManager.Aut_StrRepo.AutStrColAuthorization(entityId, weekDay) * 60);

            // calcolo dell'autorizzazione giornaliera in base a se è presente più delta o più autorizzazione
            if (autorizedTime < delta)
                autorizedDelta = autorizedTime;
            else
                autorizedDelta = delta;

            return autorizedDelta;
        }

        private static TimeSpan GetUnautorizedDeltaMinutes(TimesheetRow riga, DateTime weekDay, int entityId)
        {
            TimeSpan unautorizedDeltaMinutes = TimeSpan.Zero;

            // calcolo del delta senza autorizzazione straordinari per il giorno in corso
            TimeSpan dayDeltaMinutes = GetDeltaMinutesWithoutAutStr(riga, weekDay, entityId);

            // si recupera per quanti minuti quel collaboratore risulta autorizzato agli straordinari in quella data
            TimeSpan autorizedMinutes = TimeSpan.FromMinutes(RepoManager.Aut_StrRepo.AutStrColAuthorization(entityId, weekDay) * 60);

            // il numero dei minuti che sono non autorizzati sono la differenza tra il delta e l'autorizzazione, se positivo
            unautorizedDeltaMinutes = dayDeltaMinutes - autorizedMinutes > TimeSpan.Zero ? dayDeltaMinutes - autorizedMinutes : TimeSpan.Zero;

            return unautorizedDeltaMinutes;
        }


        private static TimeSpan GetDeltaMinutesWithoutAutStr(TimesheetRow riga, DateTime weekDay, int entityId)
        {
            return riga.First(cella => cella.Date == weekDay).Total;
        }

        private static IEnumerable<Week> CalcolaSettimane(TimesheetRow riga)
        {
            List<Week> weeks = new List<Week>();

            DateTime start = riga.First().Date;

            while (start.DayOfWeek != DayOfWeek.Monday)
                start = start.AddDays(-1);

            DateTime end = riga.Last().Date;

            while (end.DayOfWeek != DayOfWeek.Sunday)
                end = end.AddDays(1);

            end = end.AddDays(1);

            Calendar calendar = new GregorianCalendar();

            int currentWeek = calendar.GetWeekOfYear(start, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

            foreach (var date in CommonService.EachDay(start, end))
            {
                int currentWeekDay = calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

                if (currentWeek != currentWeekDay)
                {
                    currentWeek = currentWeekDay;
                    weeks.Add(new Week
                    {
                        Start = start,
                        End = date.AddDays(-1)
                    });

                    start = date;
                }
            }

            return weeks;
        }

        private class Week
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
        }
    }
}
