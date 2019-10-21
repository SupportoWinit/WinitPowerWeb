using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms.VisualStyles;
using Business.BusinessExtension;
using Common;
using Data;
using Domain;
using Domain.Extensions;

namespace Business.Repository.Custom
{
    public class ColCantOrarioRepository : GenericRepository<Col_Cant_Orario>, IColCantOrarioRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColCantOrarioRepository"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public ColCantOrarioRepository(PowerWebEntities context)
            : base(context)
        {

        }

        /// <summary>
        /// Recupera il nome della proprietà che contiene il numero di ore previste in base al giorno
        /// passato come parametro.
        /// </summary>
        /// <param name="dateToSearch">Il giorno da cui recuperare il nome della proprietà contenente il corrispettivo numero di ore previste.</param>
        /// <returns>
        /// Il nome della proprietà contenente il numero di ore previste per il giorno passato come parametro.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public string GetPropertyNameFromDate(DateTime dateToSearch)
        {
            // inizializzazione del valore di ritorno del metodo
            string propertyName = String.Empty;

            // dalla data passata come parametro si estrae il giorno
            var dayToProcess = dateToSearch.Day;

            // in base al giorno recupero e ritorno il nome della proprietà
            propertyName = String.Format("Minuti_G{0}_Orario", dayToProcess);

            // ritorno del valore calcolato dal metodo
            return propertyName;
        }

        /// <summary>
        /// Sposta i valori di presenza del timesheet i una entità di Col_Orario
        /// </summary>
        /// <param name="timesheet">Il timesheet da cui recuperare i dati.</param>
        /// <param name="colCantOrarioList">L'elenco delle entità Col_Cant_Orario in cui inserire i dati.</param>
        /// <param name="hasTimesheetWeeklyTotals">Indica se il timesheet da processare prevede l'utilizzo di totali per settimana che prevedono quindi
        /// l'inclusione delle settimane di avvio/chisura mese</param>
        public void TimesheetToColCantOrario(TimesheetModuleItem timesheet, ICollection<Col_Cant_Orario> colCantOrarioList, bool hasTimesheetWeeklyTotals)
        {
            // calcolo dei dati generali del timesheet
            int? colId = timesheet.ColId == 0 ? (int?) null : timesheet.ColId;
            int? cantId = timesheet.CantId == 0 ? (int?)null : timesheet.CantId;

            // generazione dell'oggetto col_cant_orario per il mese corrente e inizializzazione dello stesso
            // [se l'elenco passato come parametro non ha un valore per il mese corrente, lo si crea, altrimenti si utilizza quello passato come parametro]
            Col_Cant_Orario currentMonthColCantOrario;
            bool isCurrentMonthPresent = false;
            if (colCantOrarioList.Any(cco => cco.Anno_Orario == timesheet.StartDate.Year && cco.Mese_Orario == timesheet.StartDate.Month))
            {
                currentMonthColCantOrario = colCantOrarioList.First(cco => cco.Anno_Orario == timesheet.StartDate.Year && cco.Mese_Orario == timesheet.StartDate.Month);
                isCurrentMonthPresent = true;
            }
            else
                currentMonthColCantOrario = Init();
            currentMonthColCantOrario.Col_Id = colId;
            currentMonthColCantOrario.Cant_Id = cantId;
            currentMonthColCantOrario.Anno_Orario = (short)timesheet.StartDate.Year;
            currentMonthColCantOrario.Mese_Orario = Convert.ToByte(timesheet.StartDate.Month);

            // inserimento degli orari per l'oggetto relativo al mese corrente
            currentMonthColCantOrario.Minuti_G1_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day01Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G2_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day02Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G3_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day03Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G4_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day04Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G5_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day05Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G6_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day06Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G7_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day07Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G8_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day08Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G9_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day09Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G10_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day10Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G11_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day11Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G12_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day12Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G13_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day13Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G14_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day14Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G15_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day15Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G16_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day16Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G17_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day17Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G18_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day18Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G19_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day19Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G20_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day20Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G21_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day21Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G22_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day22Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G23_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day23Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G24_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day24Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G25_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day25Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G26_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day26Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G27_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day27Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G28_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day28Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G29_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day29Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G30_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day30Edit, timesheet.IsDecimalHours));
            currentMonthColCantOrario.Minuti_G31_Orario = Convert.ToDecimal(CommonService.FromHoursToMinutes(timesheet.Day31Edit, timesheet.IsDecimalHours));

            // il dato del mese corrente viene aggiunto all'elenco (se non già presente)
            if (!isCurrentMonthPresent)
                colCantOrarioList.Add(currentMonthColCantOrario);

            // se il timesheet sta trattando totali settimanali
            if (hasTimesheetWeeklyTotals)
            {
                /*Si calcolano le date limite utilizzate per la chiusura delle settimane di inizio e fine mese*/

                // inizializzazione dei giorni di inizio e fine mese oggetto del cartellino
                DateTime firstMonthDate = CommonService.GetFirstMonthDay(timesheet.StartDate);
                DateTime lastMonthDate = CommonService.GetLastMonthDay(timesheet.StartDate);
                DateTime startDate = firstMonthDate;
                DateTime endDate = lastMonthDate;

                // se la data di inizio mese non è un lunedì allora si recupera come data di inizio periodo l'ultimo lunedì del mese precedente
                if (startDate.DayOfWeek != DayOfWeek.Monday)
                    startDate = CommonService.GetLastDayOfWeekInMonth(firstMonthDate.AddMonths(-1), DayOfWeek.Monday);

                // se la data di fine mese non è una domenica allora si recupera come data di inizio periodo la prima domenica del mese successivo
                if (endDate.DayOfWeek != DayOfWeek.Sunday)
                    endDate = CommonService.GetFirstDayOfWeekInMonth(lastMonthDate.AddMonths(1), DayOfWeek.Sunday);

                // se è necessario gestire alcuni giorni del mese precedente per chiudere la settimana allora si procede
                // a generare un nuovo orario che comprenda il mese precedente
                if (startDate != firstMonthDate)
                {
                    // generazione dell'oggetto col_cant_orario per il mese precedente e inizializzazione dello stesso
                    Col_Cant_Orario newColCantOrarioPrevMonth;
                    bool isPrevMonthPresent = false;
                    if (colCantOrarioList.Any(cco => cco.Anno_Orario == timesheet.StartDate.AddMonths(-1).Year && cco.Mese_Orario == timesheet.StartDate.AddMonths(-1).Month))
                    {
                        newColCantOrarioPrevMonth = colCantOrarioList.First(cco => cco.Anno_Orario == timesheet.StartDate.AddMonths(-1).Year && cco.Mese_Orario == timesheet.StartDate.AddMonths(-1).Month);
                        isPrevMonthPresent = true;
                    }
                    else
                        newColCantOrarioPrevMonth = Init();

                    newColCantOrarioPrevMonth.Col_Id = colId;
                    newColCantOrarioPrevMonth.Cant_Id = cantId;
                    newColCantOrarioPrevMonth.Anno_Orario = (short)timesheet.StartDate.AddMonths(-1).Year;
                    newColCantOrarioPrevMonth.Mese_Orario = Convert.ToByte(timesheet.StartDate.AddMonths(-1).Month);

                    // compilazione dell'oggetto col_cant_orario per il mese precedente
                    for (int dayIndex = startDate.Day; dayIndex <= firstMonthDate.AddDays(-1).Day; dayIndex++)
                    {
                        decimal valueToSet = Convert.ToDecimal(CommonService.FromHoursToMinutes((double)CommonService.GetPropertyValue(timesheet,
                                                    String.Format("DayMinus{0}Edit", firstMonthDate.AddDays(-1).Day + 1 - dayIndex)), timesheet.IsDecimalHours));
                        CommonService.SetPropertyValue(newColCantOrarioPrevMonth, String.Format("Minuti_G{0}_Orario", dayIndex), valueToSet);

                    }

                    // aggiunta dell'oggetto riguardante il mese precedente all'elenco
                    if (!isPrevMonthPresent)
                        colCantOrarioList.Add(newColCantOrarioPrevMonth);
                }

                // se è necessario gestire alcuni giorni del mese successivo per chiudere la settimana allora si procede a generare
                // un nuovo orario che comprenda il mese successivo
                if (endDate != lastMonthDate)
                {
                    // generazione dell'oggetto col_cant_orario per il mese successivo e inizializzazione dello stesso
                    Col_Cant_Orario newColCantOrarioNextMonth;
                    bool isNextMonthPresent = false;
                    if (colCantOrarioList.Any(cco => cco.Anno_Orario == timesheet.StartDate.AddMonths(1).Year && cco.Mese_Orario == timesheet.StartDate.AddMonths(1).Month))
                    {
                        newColCantOrarioNextMonth = colCantOrarioList.First(cco => cco.Anno_Orario == timesheet.StartDate.AddMonths(1).Year && cco.Mese_Orario == timesheet.StartDate.AddMonths(1).Month);
                        isNextMonthPresent = true;
                    }
                    else
                        newColCantOrarioNextMonth = Init();

                    newColCantOrarioNextMonth.Col_Id = colId;
                    newColCantOrarioNextMonth.Cant_Id = cantId;
                    newColCantOrarioNextMonth.Anno_Orario = (short)timesheet.StartDate.AddMonths(1).Year;
                    newColCantOrarioNextMonth.Mese_Orario = Convert.ToByte(timesheet.StartDate.AddMonths(1).Month);

                    // compilazione dell'oggetto col_cant_orario per il mese successivo
                    for (int dayIndex = 1; dayIndex <= endDate.Day; dayIndex++)
                    {
                        decimal valueToSet = Convert.ToDecimal(CommonService.FromHoursToMinutes((double)CommonService.GetPropertyValue(timesheet,
                                                   String.Format("DayPlus{0}Edit", dayIndex)), timesheet.IsDecimalHours));
                        CommonService.SetPropertyValue(newColCantOrarioNextMonth, String.Format("Minuti_G{0}_Orario", dayIndex), valueToSet);
                    }

                    // aggiunta dell'oggetto riguardante il mese precedente all'elenco
                    if (!isNextMonthPresent)
                        colCantOrarioList.Add(newColCantOrarioNextMonth);
                }
            }

        }

        /// <summary>
        /// Ricerca e restituisce il numero di minuti previsti per una specifica giornata ed uno specifico collaboratore o cantiere.
        /// </summary>
        /// <param name="dateToSearch">La data in cui ricercare il numero di minuti previsti.</param>
        /// <param name="entityId">L'id del collaboratore o del cantiere per cui ricercare il numero di minuti previsti.</param>
        /// <param name="colOrarioId">Ritorna l'id del col_orario utilizzato per il calcolo delle ore; se il col orario non è stato trovato ritorna il valore 0</param>
        /// <param name="entityType">L'entità di riferimento per il recupero del timesheet (collaboratore/cantiere)</param>
        /// <returns>
        /// Il numero di minti previsti per la specifica giornata e lo specifico collaboratore; in caso di non presenza del corrispettivo record
        /// nella tabella Col_Orario allora si ritorna il valore 0.
        /// </returns>
        public double GetPlannedMinutesFromTimesheet(DateTime dateToSearch, int entityId, string entityType, out int colOrarioId)
        {
            // inizializzazione del valore di ritorno del metodo
            double dayMinutes = 0d;

            // inizializzazione del valore di ritorno del col orario id
            colOrarioId = 0;

            // ricerca all'interno della tabella Col_Orario di un record che corrisponda ai parametri specificati
            var colOrario = entityType == "Col"
            ? RepoManager.ColCantOrarioRepo.FirstOrDefault(co => co.Col_Id == entityId && co.Anno_Orario == dateToSearch.Year && co.Mese_Orario == dateToSearch.Month)
            : RepoManager.ColCantOrarioRepo.FirstOrDefault(co => co.Cant_Id == entityId && co.Anno_Orario == dateToSearch.Year && co.Mese_Orario == dateToSearch.Month);

            // se è stato trovato un orario per quel collaboratore e quel mese/anno
            if (colOrario != null)
            {
                // ritorno dell'id del record col_orario
                colOrarioId = colOrario.Col_Cant_Orario_Id;

                // ritorno il numero di minuti previsti per il giorno specifico per l'orario trovato
                dayMinutes = Convert.ToDouble(CommonService.GetPropertyValue(colOrario, GetPropertyNameFromDate(dateToSearch)));
            }

            // ritorno del valore calcolato dal metodo
            return dayMinutes;
        }
    }
}
