using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Business.BusinessExtension;
using Domain;
using Business.MDBSchema;
using Domain.Extensions;

namespace Business.Repository.Custom
{
  public interface IColCantOrarioRepository : IRepository<Col_Cant_Orario>
  {

      /// <summary>
      /// Recupera il nome della proprietà che contiene il numero di ore previste in base al giorno
      /// passato come parametro.
      /// </summary>
      /// <param name="dateToSearch">Il giorno da cui recuperare il nome della proprietà contenente il corrispettivo numero di ore previste.</param>
      /// <returns>Il nome della proprietà contenente il numero di ore previste per il giorno passato come parametro.</returns>
      string GetPropertyNameFromDate(DateTime dateToSearch);

      /// <summary>
      /// Sposta i valori di presenza del timesheet i una entità di Col_Orario
      /// </summary>
      /// <param name="timesheet">Il timesheet da cui recuperare i dati.</param>
      /// <param name="colCantOrarioList">L'elenco delle entità Col_Cant_Orario in cui inserire i dati.</param>
      /// <param name="hasTimesheetWeeklyTotals">Indica se il timesheet da processare prevede l'utilizzo di totali per settimana che prevedono quindi
      /// l'inclusione delle settimane di avvio/chisura mese</param>
      void TimesheetToColCantOrario(TimesheetModuleItem timesheet, ICollection<Col_Cant_Orario> colCantOrarioList, bool hasTimesheetWeeklyTotals);

      /// <summary>
      /// Ricerca e restituisce il numero di minuti previsti per una specifica giornata ed uno specifico collaboratore.
      /// </summary>
      /// <param name="dateToSearch">La data in cui ricercare il numero di minuti previsti.</param>
      /// <param name="entityId">Il collaboratore per cui ricercare il numero di minuti previsti.</param>
      /// <param name="entityType">L'entità di riferimento per il recupero del timesheet (collaboratore/cantiere)</param>
      /// <param name="colOrarioId">Ritorna l'id del col_orario utilizzato per il calcolo delle ore; se il col orario non è stato trovato ritorna il valore 0</param>
      /// <returns>Il numero di minti previsti per la specifica giornata e lo specifico collaboratore; in caso di non presenza del corrispettivo record
      /// nella tabella Col_Orario allora si ritorna il valore 0.</returns>
      double GetPlannedMinutesFromTimesheet(DateTime dateToSearch, int entityId, string entityType, out int colOrarioId);
  }  
}