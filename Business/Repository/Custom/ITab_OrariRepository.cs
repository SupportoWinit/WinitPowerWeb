using System;
using System.Collections.Generic;
using Domain;

namespace Business.Repository.Custom
{
    public interface ITab_OrariRepository : IRepository<Tab_Orari>
    {
        /// <summary>
        /// Per il collaboratore e l'intervallo di date specificato questo metodo si occupa di ricercare all'interno
        /// della tab orari quanto configurato e ritorna un elenco di date in cui, per ogni data, è specificato il numero di ore previsto.
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere da ricercare.</param>
        /// <param name="startDate">La data di partenza per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="endDate">La data di termine per la costruzuoione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="dateStartCol">La data di inizio disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="dateEndCol">la data di fine disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="isFromFreeTimesheet">Ritorna <c>true</c> se l'orario è stato calcolato a partire da un orario preso da col_orario; altrimenti <c>false</c></param>
        /// <param name="freeTimesheetId">Ritorna l'eventuale id della tabella col_orario da cui è sono state recuperate le ore</param>
        /// <param name="isByOtherEntity"><c>true</c> se deve essere effettuata la divisione degli orari per cantiere/collaboratore, altrimenti <c>false</c></param>
        /// <param name="referenceEntity">L'entità di riferimento per la generazione del timesheet; se non specificata si tratta il cartellino per collaboratore.</param>
        /// <param name="requestedForWeeklyTotals">Indica che il piano è richiesto per un calcolo che prevede i totali settimanali.</param>
        /// <returns>
        /// Una dizionario la cui chiave è l'id cantiere di riferimento per l'orario e il valore è un altro dizionario con chiave la data dell'intervallo e come valore una tuple i cui valori rappresentano:
        /// il numero di previsto per quel collaboratore per quella giornata; l'ora di inzio del notturno (se previsto, altrimenti null); l'ora di fine del notturno (se previsto, altrimenti null);
        /// in caso di problemi nel calcolo (periodo errato o dati non presenti, viene restituito un dizionario vuoto).
        /// </returns>
        Dictionary<int, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> GetPlanMinutes(int entityId, DateTime startDate, DateTime endDate, DateTime? dateStartCol, DateTime? dateEndCol,
            out bool isFromFreeTimesheet, out int freeTimesheetId, bool isByOtherEntity, string referenceEntity = "Col", bool requestedForWeeklyTotals = false);

        /// <summary>
        /// Recupera il piano di dettaglio per il giorno e l'entità indicata.
        /// Questo metodo non prende in considerazione gli orari di sola durata.
        /// </summary>
        /// <param name="dateToSearch">La data di cui ricercare il piano di dettaglio.</param>
        /// <param name="entityId">L'indentificativo univoco dell'entità di cui effettuare la ricerca.</param>
        /// <param name="referenceEntity">Il tipo di entità di riferimento per l'orario.</param>
        /// <returns>Un'elenco contente l'id dell'altra entità di riferimento (0 in caso di orario generico), l'ora di inzio e ora di fine previsto.</returns>
        List<Tuple<int, TimeSpan, TimeSpan>> GetDayPlanDetailCant(DateTime dateToSearch, int entityId,int cantId, string referenceEntity = "Col");

        /// <summary>
        /// Recupera il piano di dettaglio per il giorno e l'entità indicata.
        /// Questo metodo non prende in considerazione gli orari di sola durata.
        /// </summary>
        /// <param name="dateToSearch">La data di cui ricercare il piano di dettaglio.</param>
        /// <param name="entityId">L'indentificativo univoco dell'entità di cui effettuare la ricerca.</param>
        /// <param name="referenceEntity">Il tipo di entità di riferimento per l'orario.</param>
        /// <returns>Un'elenco contente l'id dell'altra entità di riferimento (0 in caso di orario generico), l'ora di inzio e ora di fine previsto.</returns>
        List<Tuple<int, TimeSpan, TimeSpan>> GetDayPlanDetail(DateTime dateToSearch, int entityId, string referenceEntity = "Col");

        /// <summary>
        /// Per il collaboratore e l'intervallo di date specificato questo metodo si occupa di ricercare all'interno
        /// della tab orari quanto configurato e ritorna un elenco di date in cui, per ogni data, sono specificati gli orari previsti.
        /// </summary>
        /// <param name="colId">L'id del collaboratore da ricercare.</param>
        /// <param name="startDate">La data di partenza per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="endDate">La data di termine per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="dateStartCol">La data di inizio disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="dateEndCol">la data di fine disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <returns>Un dizionario con chiave la data dell'intervallo e come valore una lista di coppie di ore entrata/uscita e l'ora di inizio e fine notturno; in caso
        /// di problemi nel calcolo (periodo errato o dati non presenti, viene restituito un dizionario vuoto).</returns>
        Dictionary<DateTime, List<Tuple<TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>>> GetPlanTimes(int colId, DateTime startDate, DateTime endDate, DateTime? dateStartCol, DateTime? dateEndCol);

        /// <summary>
        /// Per il collaboratore e l'intervallo di date specificato questo metodo si occupa di ricercare all'interno
        /// della tab orari quanto configurato e ritorna un elenco di date in cui, per ogni data, sono specificati gli orari previsti.
        /// </summary>
        /// <param name="colId">L'id del collaboratore da ricercare.</param>
        /// <param name="startDate">La data di partenza per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="endDate">La data di termine per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="dateStartCol">La data di inizio disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="dateEndCol">la data di fine disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <returns>Un dizionario con chiave la data dell'intervallo e come valore una lista di coppie di ore entrata/uscita e l'ora di inizio e fine notturno; in caso
        /// di problemi nel calcolo (periodo errato o dati non presenti, viene restituito un dizionario vuoto).</returns>
        Dictionary<DateTime, List<Tuple<int,TimeSpan, TimeSpan, TimeSpan?, TimeSpan?>>> GetPlanTimesNew(int colId, DateTime startDate, DateTime endDate, DateTime? dateStartCol, DateTime? dateEndCol);


        /// <summary>
        /// Recupera l'id della tab orari tipo collegata al collaboratore passato come parametro.
        /// In caso il collaboratore non sia stato trovato o la tab_orari configurata nel collaboratore
        /// non sia presente il metodo ritorna valore 0
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere da ricercare</param>
        /// <param name="entityName">La stringa che rappresenta l'entità (collaboratore/cantiere) per cui si sta effettuando la ricerca</param>
        /// <returns>L'id del record in tab orari collegato al collaboratore il cui id è passato come parametro; in caso
        /// il collaboratore non sia presente o non sia stato trovato il record in tab orari viene ritornato il valore 0</returns>
        int GetTabOrariTipoIdFromEntity(int entityId, string entityName = "Col");

        /// <summary>
        /// Per il collaboratore e l'intervallo di date specificato questo metodo si occupa di ricercare all'interno
        /// della tab orari quanto configurato e ritorna un elenco di date in cui, per ogni data, è specificato il numero di ore previsto.
        /// </summary>
        /// <param name="entityId">L'id del collaboratore/cantiere da ricercare.</param>
        /// <param name="startDate">La data di partenza per la costruzione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="endDate">La data di termine per la costruzuoione della lista (questa data sarà compresa nell'elenco).</param>
        /// <param name="dateStartCol">La data di inizio disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="dateEndCol">la data di fine disponibilità del collaboratore (quando il collaboratore non è disponibile le ore previste sono 0)</param>
        /// <param name="isFromFreeTimesheet">Ritorna <c>true</c> se l'orario è stato calcolato a partire da un orario preso da col_orario; altrimenti <c>false</c></param>
        /// <param name="freeTimesheetId">Ritorna l'eventuale id della tabella col_orario da cui è sono state recuperate le ore</param>
        /// <param name="referenceEntity">L'entità di riferimento per la generazione del timesheet; se non specificata si tratta il cartellino per collaboratore.</param>
        /// <returns>
        /// Una dizionario la cui chiave è l'id cantiere di riferimento per l'orario e il valore è un altro dizionario con chiave la data dell'intervallo e come valore una tuple i cui valori rappresentano:
        /// il numero di previsto per quel collaboratore per quella giornata; l'ora di inzio del notturno (se previsto, altrimenti null); l'ora di fine del notturno (se previsto, altrimenti null);
        /// in caso di problemi nel calcolo (periodo errato o dati non presenti, viene restituito un dizionario vuoto).
        /// </returns>
        Dictionary<string, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> GetDevidedPlanMinutes(int colId, DateTime startDate, DateTime endDate, DateTime? dateStartCol, DateTime? dateEndCol,
            out bool isFromFreeTimesheet, out int freeTimesheetId);

        /// <summary>
        /// Determina se l'orario è valido per la data passata come parametro.
        /// </summary>
        /// <param name="timesheet">L'orario da verificare nella data</param>
        /// <param name="i">La data da verificare per l'orario</param>
        /// <returns><c>true</c> in caso l'orario sia valido per la data e <c>false in caso contrario</c></returns>
        bool IsToApplyTimesheet(Tab_Orari timesheet, DateTime i);

        /// <summary>
        /// Ricalcola il periodo di ricerca specificato utilizzando le date di disponibilità del collaboratore.
        /// </summary>
        /// <param name="startDate">La data di inizio della ricerca da ricalcolare.</param>
        /// <param name="endDate">La data di fine della ricerca da ricalcolare.</param>
        /// <param name="dateStartCol">La data di inizio di disponibilità del collaboratore.</param>
        /// <param name="dateEndCol">La data di fine di disponibilità del collaboratore.</param>
        /// <returns>Ritorna una tuple che contiene nel primo item la data di inizio ricalcolata e come secondo parametro la data di fine ricalcolata.</returns>
        Tuple<DateTime, DateTime> FilterPeriodWithColDispDates(DateTime startDate, DateTime endDate, DateTime? dateStartCol, DateTime? dateEndCol);

        /// <summary>
        /// Metodo che per il periodo passato come parametro si occupa di generare un piano completamente vuoto (e senza previsione di notturno).
        /// </summary>
        /// <param name="startDate">La data di inizio per la generazione del piano</param>
        /// <param name="endDate">La data di fine per la generazione del piano</param>
        /// <param name="requestedForWeeklyTotals">Indica che il piano è richiesto per un calcolo che prevede i totali settimanali.</param>
        /// <returns>Un dizionario con chiave la data dell'intervallo e come valore una tuple i cui valori rappresentano: il numero di minuti previsto per la giornata (0); l'ora di inzio
        /// del notturno (null); l'ora di fine del notturno (null).</returns>
        Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>> GetEmptyMinutesPlan(DateTime startDate, DateTime endDate, bool requestedForWeeklyTotals = false);

        /// <summary>
        /// Restituisce la durata totale del campo
        /// </summary>
        /// <param name="dateToSearch">The date to search.</param>
        /// <param name="entityId">The entity identifier.</param>
        /// <param name="referenceEntity">The reference entity.</param>
        /// <returns></returns>
        int GetMonthlyPlanDuration(DateTime dateToSearch, int entityId, string referenceEntity = "Col");
    }    
}