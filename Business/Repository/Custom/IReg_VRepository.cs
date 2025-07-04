using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Business.XmlExportsData.Perfetto;
using Common;
using Domain;
using Domain.Extensions;

namespace Business.Repository.Custom
{
    public interface IReg_VRepository : IRepository<Reg_V>
    {
        List<KeyValuePair<String, String>> Rounding(IEnumerable<Reg_V> regVs, IEnumerable<Reg> regs, List<Cant> cants, List<Col> cols, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application,bool delete);

        List<KeyValuePair<String, String>> CopertureSerali(IEnumerable<Reg_V> regVs, IEnumerable<Reg> regs, List<Cant> cants, List<Col> cols, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application, bool delete);
        List<KeyValuePair<String, String>> AdjustOverlappedRegs(IEnumerable<Reg_V> regVs, IEnumerable<Reg> regs, List<Col> cols, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application, bool delete);

        List<KeyValuePair<String, String>> CheckDeelay(IEnumerable<Reg_V> regVs, IEnumerable<Reg> regs, List<Cant> cants, List<Col> cols, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application);

        List<KeyValuePair<String, String>> DurationRounding(IEnumerable<Reg_V> regVs,RoundingMethodEnum metodo);

        List<KeyValuePair<String, String>> PausaPranzo(IEnumerable<Reg_V> regVs);

        List<KeyValuePair<String, String>> NewPausaPranzo(IEnumerable<Reg_V> regVs);

        List<KeyValuePair<String, String>> PausaPranzoKomplett(IEnumerable<Reg_V> regVs);

        List<KeyValuePair<String, String>> DeleteDurationRounding(IEnumerable<Reg> regVs);

        List<KeyValuePair<String, String>> DeletePausaPranzo(IEnumerable<Reg> regVs);

        List<KeyValuePair<String, String>> DeleteNewPausaPranzo(IEnumerable<Reg> regVs);

        List<KeyValuePair<String, String>> CheckOverlaps(IEnumerable<Reg_V> regvs, bool isOnLine = false);

        List<KeyValuePair<String, String>> ElaborateBlockDate(List<Col> cols, DateTime from, DateTime to, Boolean isBackward = false);

        int SubstractTotals(Col currCol, DateTime from, DateTime to, Boolean isToSaveChanges = false);

        /// <summary>
        /// Imposta (inserisce o modifica) il monte minuti per il collaboratore indicato per il periodo specificato.
        /// </summary>
        /// <param name="col">Il collaboratore per cui inserire o aggiornare il monte minuti.</param>
        /// <param name="from">La data di inizio del periodo da processare.</param>
        /// <param name="to">La data di fine del periodo da processare.</param>
        /// <exception cref="System.ArgumentException">To date must be major than from date</exception>
        void SetMonthMinutesAmmount(Col col, DateTime from, DateTime to);

        void delete10mins();

        List<Reg_V> GetTimesheet(Col col, List<Cant> cants, DateTime from, DateTime to, out bool isFromFreeTimesheet, out int freeTimeSheetId);

        List<Reg_V> Add2Minutes(List<Reg_V> regvlist);

        List<KeyValuePair<String, String>> ElaborateActivities(IEnumerable<Reg_V> regvs);

        List<KeyValuePair<String, String>> ElaborateTrips(IEnumerable<Reg_V> regvsList, Boolean isToSaveErrorMessage = false, bool isToSaveChanges = true, int? elaborateUserId = null, DateTime? elaborateDateTime = null, ApplicationMessageEnum? application = null);

        List<Reg> GetRegToDelete(Reg_V regVToDelete);

        /// <summary>
        /// Recupera le regV tra un renge di date o che sono dei passaggi grazie agli id delle relative reg.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="regIds">The reg ids.</param>
        /// <param name="">The .</param>
        /// <returns></returns>
        IEnumerable<Reg_V> GetReg_VByDateRangeOrPassageByRegIds(DateTime from, DateTime to, IEnumerable<int> regIds);

        Boolean CheckAlreadyPresent(Reg_V entity);

        /// <summary>
        /// Metodo che data una regv prima dell'update si occupa di verificare un eventuale cambio di cant e/o col e di conseguenza ne cancella i valori di pru e fru.
        /// Il controllo viene effettuato prima della scrittura su database e quindi non funziona su reg_v le cui reg siano già scritte.
        /// Non viene eseguita nessuna operazione su regV nuove (non ancora scritte su database, cioè con RegE valorizzato a 0)
        /// </summary>
        /// <param name="regVToUpdate">The di cui effettuare l'eventuale update di pru/fru.</param>
        void ManageCantColChangesBeforeUpdate(Reg_V regVToUpdate);

        /// <summary>
        /// Effettua l'esportazione xml delle registrazioni passate come parametro verso Pefetto, restituendo per il download un file zip con i dati generati.
        /// </summary>
        /// <param name="regVsToProcess">Le Reg_V da processare nell'esportazione Xml.</param>
        /// <param name="filesOutputFolder">La cartella in cui salvare i dati preparati nell'export xml</param>
        /// <returns>Ritorna il percorso del file da ritornare al browser con i dati esportati</returns>
        string PrepareXmlExportToPerfetto(IQueryable<Reg_V> regVsToProcess, string filesOutputFolder);

        /// <summary>
        /// Effettua l'esportazione xml delle registrazioni passate come parametro verso Scs, restituendo per il download un file zip con i dati generati.
        /// </summary>
        /// <param name="regVsToProcess">Le Reg_V da processare nell'esportazione Xml.</param>
        /// <param name="filesOutputFolder">La cartella in cui salvare i dati preparati nell'export xml</param>
        /// <returns>Ritorna il percorso del file da ritornare al browser con i dati esportati</returns>
        string PrepareXmlExportToScs(IQueryable<Reg_V> regVsToProcess, string filesOutputFolder, DateTime fine);

        /// <summary>
        /// Effettua l'esportazione xml delle registrazioni passate come parametro verso Orlando, restituendo per il download un file zip con i dati generati.
        /// </summary>
        /// <param name="regVsToProcess">Le Reg_V da processare nell'esportazione Xml.</param>
        /// <param name="filesOutputFolder">La cartella in cui salvare i dati preparati nell'export xml</param>
        /// <returns>Ritorna il percorso del file da ritornare al browser con i dati esportati</returns>
        string PrepareXmlExportToOrlando(IQueryable<Reg_V> regVsToProcess, string filesOutputFolder, DateTime fine);

        /// <summary>
        /// Determina se il viaggio passato come parametro è fatto all'interno della stesso comune
        /// </summary>
        /// <param name="trip">Il viaggio da verificare.</param>
        /// <param name="dayRegVs">Le registrazioni del giorno per lo stesso collaboratore.</param>
        /// <returns><c>true</c> se il viaggio è all'interno dello stesso comune; altrimenti <c>false</c></returns>
        bool IsTripOnSameMunicipality(Reg_V trip, IEnumerable<Reg_V> dayRegVs);

        /// <summary>
        /// Determina se il viaggio specificato è un viaggio di inizio o fine giornata.
        /// </summary>
        /// <param name="trip">Il viaggio da verificare.</param>
        /// <param name="dayRegVs">Le registrazioni del giorno per lo stesso collaboratore.</param>
        /// <returns><c>true</c> se il viaggio è un viaggio di inizio o fine giornata; altrimenti <c>false</c></returns>
        bool IsTripOnStartEnd(Reg_V trip, IEnumerable<Reg_V> dayRegVs);

        /// <summary>
        /// Invia la mail riportante i collaboratori ritardatari del giorno odierno.
        /// </summary>
        /// <returns>Gli eventuali errori</returns>
        string InviaRitardi();
        
        /// <summary>
        /// Invia la mail riportante i collaboratori che non hanno timbrature per la giornata di ieri.
        /// </summary>
        /// <returns>Gli eventuali errori</returns>
        string InviaChiamate();

        /// <summary>
        /// Recupera la stringa utilizzata dal repository per filtrare i dati nelle query testuali.
        /// </summary>
        /// <value>
        /// La stringa utilizzata dal repository per filtrare i dati nelle query testuali.
        /// </value>
        string FilterText { get; }

    }
}
