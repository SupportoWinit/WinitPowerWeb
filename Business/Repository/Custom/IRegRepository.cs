using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Common;
using Domain;
using System.IO;
using Business.MDBSchema;
using Business.DataClasses.SupportClasses;

namespace Business.Repository.Custom
{
    public interface IRegRepository : IRepository<Reg>
    {
        /// <summary>
        /// Importa le registrazioni specificate nel database.
        /// </summary>
        /// <param name="regsNoGpsToImport">L'elenco delle registrazioni non gps da importare.</param>
        /// <param name="regsGpsToImport">L'elenco delle registrazioni gps da importare.</param>
        /// <returns>Una lista contenente gli eventuali errori riscontrati durante l'importazione.</returns>
        List<KeyValuePair<String, String>> Import(string[] regsNoGpsToImport, string[] regsGpsToImport);

        List<KeyValuePair<String, String>> Elaborate(ICollection<Reg> regs, DateTime fromDate, DateTime toDate, Boolean isToSaveChanges = true,
            Boolean isToAssociatePruFru = false, int? elaborateUserId = null, DateTime? elaborateDateTime = null, ApplicationMessageEnum? application = null);

        List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet dataSet, String tableName, bool onlyErrors = false);
        List<KeyValuePair<DateTime, DateTime>> GetPeriods(IEnumerable<RegsByDay> countByDay);
        bool StoreOrRestoreRegs(DateTime fromDate, DateTime toDate, bool isBackWard);

        bool UpdatePeriodoWithPendingElabDates(DateTime originalFrom, DateTime originalTo, out DateTime newFrom, out DateTime newTo);

        /// <summary>
        /// Metodo che data una reg prima dell'update si occupa di verificare un eventuale cambio di cant e/o col e di conseguenza ne cancella i valori di pru e fru.
        /// Il controllo viene effettuato prima della scrittura su database e quindi non funziona su reg già scritte su db (con parametri opzionali non specificati).
        /// Con i parametri opzionali specificati (devono essere entrambi diversi da stringa vuota) si procede alla verifica non più sul db ma utilizzando quei valori.
        /// </summary>
        /// <param name="regToUpdate">La reg su cui effettuare le verifiche ed eventualmente le correzioni.</param>
        /// <param name="oldReg">La vecchia reg (opzionale) su cui effettuare i controlli.</param>
        void ManageCantColChangesBeforeUpdate(Reg regToUpdate, Reg oldReg = null);

        /// <summary>
        /// Gestisce il trasbordo/generazione delle dati originali per la reg in entrata e in uscita passata come parametro.
        /// Metodo che lavora solamente nei moduli onlie utilizzando i valori passati per il row updating.
        /// </summary>
        /// <param name="regEToProcess">La reg in entrata da processare.</param>
        /// <param name="regEToProcessId">l'id della reg in entrata da processare (per capire se si tratta di una nuova reg o meno).</param>
        /// <param name="regEOriginalDate">La data originale della reg in entrata da processare.</param>
        /// <param name="regUToProcess">La reg in uscita da processare.</param>
        /// <param name="regUToProcessId">l'id della reg in uscita da processare (per capire se si tratta di una nuova reg o meno).</param>
        /// <param name="regUOriginalDate">La data originale della reg in entrata da processare.</param>
        /// <param name="isSameDay"><c>true</c> se le due registrazioni sono dello stesso giorno, altrimenti <c>false</c></param>
        void ManageOrigDates(Reg regEToProcess, int regEToProcessId,
            DateTime regEOriginalDate, Reg regUToProcess, int regUToProcessId, DateTime regUOriginalDate, bool isSameDay);

        /// <summary>
        /// Viene generato ed inserito un codice temporaneo di accoppiamento al fine che l'elaborate riesca a riaccoppiare senza rielaborare reg bloccate.
        /// Il controllo di blocco delle reg e della loro presenza è gestito all'interno del metodo.
        /// </summary>
        /// <param name="newRegE">La reg in entrata da processare</param>
        /// <param name="newRegU">La reg in uscita da processare</param>
        /// <param name="oldRegEId">L'id della reg in entrata che si sta modificando (le reg passate come parametro sono per struttura dei moduli
        /// sempre reg nuove)</param>
        void PerformBlockedRegsCouple(Reg newRegE, Reg newRegU, int oldRegEId);

        /// <summary>
        /// Genera una nuova reg di tipo 11 (rettifica manuale)
        /// </summary>
        /// <param name="colId">The col identifier.</param>
        /// <param name="correctionDate">The correction date.</param>
        /// <param name="correctionType">Type of the correction.</param>
        /// <param name="correctionDuration">Duration of the correction.</param>
        /// <param name="cantId">The cant identifier.</param>
        /// <returns></returns>
        Reg GenerateCorrectionReg(int colId, DateTime correctionDate, CorrectionTypeEnum correctionType, TimeSpan correctionDuration, int cantId = 0);

        /// <summary>
        /// Generates the manual rett.
        /// </summary>
        /// <param name="colId">The col identifier.</param>
        /// <param name="correctionDate">The correction date.</param>
        /// <param name="correctionType">Type of the correction.</param>
        /// <param name="correctionDuration">Duration of the correction.</param>
        /// <param name="cantId">The cant identifier.</param>
        /// <returns></returns>
        Reg GenerateManualRett(int colId, DateTime correctionDate, CorrectionTypeEnum correctionType, TimeSpan correctionDuration, int cantId = 0);
        /// <summary>
        /// Genera e restituisce una nuova reg di tipo arrotondamento utilizzando i dati passati come parametro.
        /// </summary>
        /// <param name="colId">L'id collaboratore a cui collegare la nuova registrazione.</param>
        /// <param name="roundingDate">La data da utilizzare nella registrazione.</param>
        /// <param name="roundingType">Il tipo di arrotondamento (positiva/negativa) da generare.</param>
        /// <param name="roundingDuration">La durata (valore assoluto) con cui generare l'arrotondamento.</param>
        /// <returns>
        /// La registrazione contenente l'arrotondamento passata come parametro.
        /// </returns>
        Reg GenerateRoundingReg(int colId, DateTime roundingDate, RoundingTypeEnum roundingType, TimeSpan roundingDuration);

        /// <summary>
        /// Genera e restituisce una nuova reg di tipo durata utilizzando i dati passati come parametro.
        /// </summary>
        /// <param name="colId">L'id collaboratore a cui collegare la nuova registrazione.</param>
        /// <param name="_Date">La data da utilizzare nella registrazione.</param>
        /// <param name="cte">Il tipo di durata (positiva/negativa) da generare.</param>
        /// <param name="duration">La durata (valore assoluto) con cui generare la reg.</param>
        /// <param name="cantId">Id cantiere.</param>
        /// <param name="motiv_Id">id motivazione.</param>
        /// <returns>
        /// La registrazione contenente la reg per durata.
        /// </returns>
        Reg GenerateDurationReg(int colId, DateTime _Date, CorrectionTypeEnum cte, TimeSpan duration, int cantId = 0, int motiv_Id = 0);

        /// <summary>
        /// Dati i valori inputati in griglia si prepara e ritorna una registrazione d'entrata corrispondente.
        /// </summary>
        /// <param name="newValues">I nuovi valori in griglia da processare.</param>
        /// <param name="isUpdating">Se impostato a <c>true</c> allora si sta effettuando l'update di un record esistente.</param>
        /// <param name="oldRegE">La registrazione che la reg restituita andrà a sostituire.</param>
        /// <returns>La nuova registrazione popolata con i dati indicati in griglia</returns>
        Reg GetRegEFromNewValues(OrderedDictionary newValues, bool isUpdating = false, Reg oldRegE = null);

        /// <summary>
        /// Dati i valori inputati in griglia si prepara e ritorna una registrazione d'uscita corrispondente.
        /// </summary>
        /// <param name="newValues">I nuovi valori in griglia da processare.</param>
        /// <param name="isUpdating">Se impostato a <c>true</c> allora si sta effettuando l'update di un record esistente.</param>
        /// <param name="oldRegU">La registrazione che la reg restituita andrà a sostituire.</param>
        /// <returns>La nuova registrazione popolata con i dati indicati in griglia</returns>
        Reg GetRegUFromNewValues(OrderedDictionary newValues, bool isUpdating = false, Reg oldRegU = null);

        /// <summary>
        /// Recupera e restituisce l'anagrafica PRU/FRU per lo specifico codice passato come parametro.
        /// </summary>
        /// <param name="pruFruCode">Il codice PRU/FRU da ricercare.</param>
        /// <param name="pruFruCache">La cache degli elementi già processati.</param>
        /// <returns>L'oggetto contenente l'anagrafica PRU/FRU corrispondente al codice specificato.</returns>
        object GetPruFruRegistry(string pruFruCode, Dictionary<string, object> pruFruCache);

        IEnumerable<RegsByDay> CountRegsFromDateRange(DateTime from, DateTime to);

        /// <summary>
        /// Recupera le registrazioni tra un range di date tramite ora fisica
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param
        ///<param name="tracking">Tracciamento entità nel context.</param>
        /// <returns></returns>
        IEnumerable<Reg> FindRegsByDataFis(DateTime from, DateTime to, bool tracking = true);


        /// <summary>
        /// Recupera le registrazioni tra due date per un singolo collaboratore non bloccate
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="colId">The col identifier.</param>
        /// <param name="tracking">Se è true le entità vengono tracciate dal contesto.</param>
        /// <returns></returns>
        IEnumerable<int> GetRegsIdByDateRangeByColNotBlocked(DateTime from, DateTime to, int colId, bool tracking = true);

    }
}
