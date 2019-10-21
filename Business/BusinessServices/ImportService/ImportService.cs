using Business.Repository;
using Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Business.BusinessServices.ImportService
{
    public class ImportService
    {
        private ICollection<KeyValuePair<string, string>> _errors;

        public IEnumerable<KeyValuePair<string,string>> Errors
        {
            get
            {
                return _errors;
            }
        }

        public ImportService()
        {
            _errors = new List<KeyValuePair<string, string>>();
        }

        public void ClearErrors()
        {
            _errors.Clear();
        }

        #region Funzioni d'appoggio
        
        public DateTime GetMinimumFisDate(IEnumerable<Reg> regs)
        {
            return regs.Min(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date;
        }

        public DateTime GetMaximumFisDate(IEnumerable<Reg> regs)
        {
            return regs.Max(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date.AddDays(1);
        }

        #endregion

        #region Estrazione regs da stringhe

        /// <summary>
        /// Crea delle reg parsando una stringa di testo in formato GPS
        /// </summary>
        /// <param name="nonGpsLines">The GPS lines.</param>
        /// <returns></returns>
        public IEnumerable<Reg> GetRegsFromGpsLines(IEnumerable<string> gpsLines)
        {
            return null;
        }

        /// <summary>
        /// Crea delle reg parsando una stringa di testo non in formato GPS
        /// </summary>
        /// <param name="nonGpsLines">The non GPS lines.</param>
        /// <returns></returns>
        //public IEnumerable<Reg> GetRegsFromNonGpsLines(IEnumerable<string> nonGpsLines)
        //{

        //    // La generazione delle timbrature standard per l'inserimento prevede, per ogni linea di dato proveniente dal file di testo:
        //    // 1. Il recupero dell'anagrafica del dispositivo (machine) che ha effettuato la timbratura nell'anagrafica PRU/FRU
        //    // 2. Il recupero dell'anagrafica del badge/tag (badge) che ha effettuato la timbratura nell'anagrafica PRU/PRU
        //    // 3. Il controllo di coerenza del dato ricevuto (badge e machine tutte presenti e in anagrafiche distinte)
        //    // 4. La generazione e l'aggiunta della registrazione all'elenco di reg da aggiungere

        //    // si procede all'elaborazione solamente se sono state passte delle timbrature
        //    if (!nonGpsLines.Any())
        //        return Enumerable.Empty<Reg>();

        //    ICollection<Reg> regsToAdd = new List<Reg>();

        //    #region PARTE DA RIVEDERE

        //    // inizializzazione della personalizzazione che indica se autogenerare le attività di presidio all'uscita di specifici turni
        //    AutoGeneratePresidiumActivityEnum presidiumCustomization = (AutoGeneratePresidiumActivityEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoGeneratePresidiumActivityEnum);

        //    // se la customizzazione dei presidi risulta attiva, si recuperano anche i relativi parametri
        //    List<string> presidiumTurns = new List<string>();
        //    int presidiumCantId = 0;
        //    if (presidiumCustomization == AutoGeneratePresidiumActivityEnum.Generate)
        //    {
        //        string presidiumTurnsTmp = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutoGeneratePresidiumActivityEnum, "PresidiumTurnCodes");
        //        if (!String.IsNullOrEmpty(presidiumTurnsTmp))
        //            if (presidiumTurnsTmp.Contains("#"))
        //                presidiumTurns = presidiumTurnsTmp.Split('#').ToList();
        //            else
        //                presidiumTurns.Add(presidiumTurnsTmp);

        //        string presidiumCantCode = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutoGeneratePresidiumActivityEnum, "PresidiumCantCode");
        //        Cant currentPresidiumCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Codice_Cantiere == presidiumCantCode);
        //        if (currentPresidiumCant != default(Cant))
        //            presidiumCantId = currentPresidiumCant.Cant_Id;
        //    }

        //    #endregion

        //    ICollection<PreReg> preRegs = new List<PreReg>();

        //    foreach (string line in nonGpsLines)
        //    {
        //        preRegs.Add(new PreReg(line));
        //    }

        //    var badgeCodes = preRegs.Select(pReg => pReg.BadgeCode).ToArray();
        //    var deviceCodes = preRegs.Select(pReg => pReg.DeviceCode).ToArray();

        //    var machineCodes = badgeCodes.Concat(deviceCodes).Distinct().ToArray();

        //    #region DIZIONARIO RICERCA PRU-FRU

        //    var pruFruRegister = BusinessService.GetPruFruRegistry(machineCodes);

        //    #endregion

        //    // inizializzazione della variabile che terrà traccia della registrazione precedente a quella attualmente in processo
        //    PreReg previousPreReg = null;

        //    // inizializzazione dell'ultima registazione senza informazioni aggiuntive processata per l'inserimento
        //    Reg previousReg = null;

        //    foreach (var currentPreReg in preRegs)
        //    {


        //        // se si sta trattando una registrazione normale (no informazioni aggiuntive)
        //        if (currentPreReg.AdditionalInfoType == AdditionalInfoEnum.None)
        //        {
        //            #region Recupero delle anagrafiche (PRU/FRU) del dispositivo

        //            // salvataggio del codice badge corrente attualmente in processo
        //            string originalBadgeCode = currentPreReg.BadgeCode;

        //            // calcolo degli oggetti PRU/FRU corrispondenti ai codici della timbratura
        //            object machineRegistry = GetPruFruRegistry(currentPreReg.DeviceCode, pruFruYetProcessed);
        //            object badgeRegistry = GetPruFruRegistry(currentPreReg.BadgeCode, pruFruYetProcessed);

        //            // se l'anagrafica del dispositivo è stata trovata allora si gestisce la sua modifica per eventuale
        //            // presenza di causali sul fisso
        //            machineRegistry = ManageDeviceActivityMachineRegistry(previousPreReg, currentPreReg, machineRegistry, pruFruYetProcessed);

        //            #endregion

        //            #region Convalida input dei dati di timbratura

        //            // entrambe le matricole devono essere valorizzate, se anche solo una delle stesse non è stata trovata allora
        //            // si segnala la linea attuale come errore e si passa al record successivo (l'errore viene anche riportato nel log)
        //            if (machineRegistry == null)
        //            {
        //                string errorMessage = string.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PRIMA_MATRICOLA_INESISTENTE, currentPreReg.DeviceCode));
        //                processErrors.Add(new KeyValuePair<string, string>(errorMessage, nonGpsLine));
        //                Log.Warn(errorMessage);

        //                continue;
        //            }

        //            if (badgeRegistry == null)
        //            {
        //                string errorMessage = string.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_SECONDA_MATRICOLA_INESISTENTE, currentPreReg.BadgeCode));
        //                processErrors.Add(new KeyValuePair<string, string>(errorMessage, nonGpsLine));
        //                Log.Warn(errorMessage);

        //                continue;
        //            }

        //            // se entrambe le macchine sono fru potrebbe trattarsi della timbratura attività su una app configurata come dispositivo fisso;
        //            // in questo caso, prima di scartare la timbratura è necessario ciclare sui record successivi alla stessa e verificare 
        //            // se ci sono delle informazioni aggiuntive che indicano il dispositivo portatile di timbratura dell'attività;
        //            // se così fosse il machine registry verrà sostituito con quanto trovato
        //            if (machineRegistry is Fru && badgeRegistry is Fru)
        //            {
        //                string newMachineRegistry = PruCodeForActivity(loopLines.IndexOf(nonGpsLine) + 1, loopLines);

        //                if (!string.IsNullOrEmpty(newMachineRegistry) && newMachineRegistry != "STOP")
        //                    machineRegistry = GetPruFruRegistry(newMachineRegistry, pruFruYetProcessed);
        //            }

        //            // arrivato a questo punto si è certi che entrambe le matricole sono valorizzate e condizione essenziale
        //            // affinché l'importazione possa avvenire è che le due matricole siano di anagrafiche differenti; se quindi le
        //            // anagrafiche sono entrambe pru o fru allora si segnala l'errore e si passa alla linea successiva
        //            if ((machineRegistry is Pru && badgeRegistry is Pru) || (machineRegistry is Fru && badgeRegistry is Fru))
        //            {
        //                string errorMessage = string.Format("*{0}|{1}|{2}", BusinessService.GetLocalizedString(PowerWebResources.ERR_PRIMA_SECONDA_MATRICOLA_STESSA_ANAGRAFICA)
        //                    , currentPreReg.DeviceCode, currentPreReg.BadgeCode);
        //                processErrors.Add(new KeyValuePair<string, string>(errorMessage, nonGpsLine));

        //                Log.Warn(errorMessage);

        //                continue;
        //            }


        //            #endregion

        //            #region Trasformazione delle anagrafiche generiche in Pru e Fru

        //            // inizializzazione dell'anagrafica Pru e dell'anagrafica Fru della timbratura:
        //            // - se la matricola del dispositivo è una pru allora è lei la pru, altrimenti sicuramente il badge
        //            // - se la matricola del dispositivo è una fru allora è lei la fru, altrimenti sicuramente il badge
        //            Pru regPru = (Pru)(machineRegistry is Pru ? machineRegistry : badgeRegistry);
        //            Fru regFru = (Fru)(machineRegistry is Fru ? machineRegistry : badgeRegistry);

        //            #endregion

        //            #region Generazione della reg e aggiunta della stessa all'elenco di reg da aggiungere


        //            regsToAdd.Add(new Reg
        //            {
        //                Fru_Id = regFru.Fru_Id,
        //                Pru_Id = regPru.Pru_Id,
        //                Registrazione_Data_Ora_Fis_Reg = currentPreReg.RegistrationDateTime,
        //                Registrazione_Data_Ora_Fig_Reg = currentPreReg.RegistrationDateTime,
        //                Registrazione_Data_Ora_Orig_Reg = currentPreReg.RegistrationDateTime,
        //                Data_Registrazione_Reg = DateTime.UtcNow,
        //                DataOraUltimaModifica_Reg = DateTime.UtcNow,
        //                Flag_EU_Reg = currentPreReg.RegistrationDirection,
        //                Registrazione_Badge_Originale = currentPreReg.BadgeCode
        //            });

        //            #endregion

        //            // prima di procedere alla lavorazione del record successivo si procede al salvataggio della registrazione precedente
        //            // sia quella con solo i dati di processo sia quella che sarà scritta a database. Si imposta la registrazione di processo precedente
        //            // solamente se la stessa non risulta essere un'attività
        //            if (!CommonService.IsActivityDeviceCode(originalBadgeCode))
        //                previousPreReg = currentPreReg;

        //            previousReg = regsToAdd.Last();
        //        }
        //        else // se si sta invece trattando una registrazione con informazioni aggiuntive...
        //        {
        //            // allora si procede all'inserimento del dato aggiuntivo sulla registrazione, se già impostata
        //            if (previousReg != null)
        //                switch (currentPreReg.AdditionalInfoType)
        //                {
        //                    case AdditionalInfoEnum.Turn:
        //                        previousReg.Turno = currentPreReg.TurnCode;

        //                        // se si è nel ciclo precedente* processando un'uscita, è richiesta la generazione delle attviità di presidio e la registazione è in un turno
        //                        // tra quelli configurati
        //                        // * si controlla la registrazione precedente in quanto i dati di turno sono scirtti successivamente alla registrazione principale, che rimane tale fino alla successiva
        //                        //   registrazione "buona"
        //                        if (previousReg.Flag_EU_Reg == "U" && presidiumCustomization == AutoGeneratePresidiumActivityEnum.Generate && !String.IsNullOrEmpty(previousReg.Turno) && presidiumTurns.Any(tCode => tCode == previousReg.Turno)
        //                            && presidiumCantId != 0)
        //                        {
        //                            // ... allora si inserisce una nuova registrazione nell'elenco con la chiusura del presidio
        //                            // prima dell'ultima registrazione
        //                            Reg currentLastReg = regsToAdd.Last();
        //                            regsToAdd.Add(new Reg
        //                            {
        //                                Fru_Id = currentLastReg.Fru_Id,
        //                                Pru_Id = currentLastReg.Pru_Id,
        //                                Registrazione_Data_Ora_Fis_Reg = currentLastReg.Registrazione_Data_Ora_Fis_Reg,
        //                                Registrazione_Data_Ora_Fig_Reg = currentLastReg.Registrazione_Data_Ora_Fig_Reg,
        //                                Registrazione_Data_Ora_Orig_Reg = currentLastReg.Registrazione_Data_Ora_Orig_Reg,
        //                                Data_Registrazione_Reg = currentLastReg.Data_Registrazione_Reg,
        //                                DataOraUltimaModifica_Reg = currentLastReg.DataOraUltimaModifica_Reg,
        //                                Flag_EU_Reg = currentLastReg.Flag_EU_Reg,
        //                                Registrazione_Badge_Originale = currentLastReg.Registrazione_Badge_Originale,
        //                                Turno = currentLastReg.Turno,
        //                                Sotto_Cantiere = currentLastReg.Sotto_Cantiere,
        //                                Tipo_Attivita = currentLastReg.Tipo_Attivita
        //                            });
        //                            currentLastReg.Cant_Id = presidiumCantId;
        //                            currentLastReg.Fru_Id = null;
        //                            currentLastReg.Flag_EU_Reg = null;

        //                            // ricalcolo l'ultima registrazione da processare
        //                            previousReg = regsToAdd.Last();
        //                        }
        //                        break;

        //                    //viene aggiunta l'informazione aggiunta sul sottocantiere
        //                    case AdditionalInfoEnum.SubCant:
        //                        previousReg.Sotto_Cantiere = currentPreReg.SubCantDesc;
        //                        break;


        //                    case AdditionalInfoEnum.ActivityType:
        //                        previousReg.Tipo_Attivita = currentPreReg.ActivityTypeCode;
        //                        break;

        //                    case AdditionalInfoEnum.Squadra:
        //                        //Aggiunge una registrazione per ogni PRU della squadra
        //                        foreach (string pruCode in currentPreReg.SquadraArray)
        //                        {
        //                            //Controlla che la PRU esista

        //                            if (!pruFruRegister.ContainsPru(pruCode))
        //                            {
        //                                //Se la PRU non esiste, si costruisce la riga di input specifica per la PRU corrente, in modo da poterla importare correttamente la prossima volta.
        //                                string errorMessage = string.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PRIMA_MATRICOLA_INESISTENTE, pruCode));
        //                                string[] splittedLine = nonGpsLine.Split(';');
        //                                string[] errorLine = splittedLine.Take(8).ToArray();
        //                                errorLine[0] = pruCode;

        //                                //processErrors.Add(new KeyValuePair<string, string>(errorMessage, string.Join(";", errorLine)));
        //                                //Log.Warn(errorMessage);
        //                            }
        //                            else
        //                            {

        //                                Pru pru = pruFruRegister.GetPru(pruCode);

        //                                regsToAdd.Add(new Reg
        //                                {
        //                                    Fru_Id = previousReg.Fru_Id,
        //                                    Pru_Id = pru.Pru_Id,
        //                                    Registrazione_Data_Ora_Fis_Reg = previousReg.Registrazione_Data_Ora_Fis_Reg,
        //                                    Registrazione_Data_Ora_Fig_Reg = previousReg.Registrazione_Data_Ora_Fig_Reg,
        //                                    Registrazione_Data_Ora_Orig_Reg = previousReg.Registrazione_Data_Ora_Orig_Reg,
        //                                    Data_Registrazione_Reg = previousReg.Data_Registrazione_Reg,
        //                                    DataOraUltimaModifica_Reg = previousReg.DataOraUltimaModifica_Reg,
        //                                    Flag_EU_Reg = previousReg.Flag_EU_Reg,
        //                                    Registrazione_Badge_Originale = previousReg.Registrazione_Badge_Originale,
        //                                    Turno = previousReg.Turno,
        //                                    Sotto_Cantiere = previousReg.Sotto_Cantiere,
        //                                    Tipo_Attivita = previousReg.Tipo_Attivita
        //                                });
        //                            }
        //                        }
        //                        break;
        //                }
        //        }

        //    }

        //    return regsToAdd;
        //}

        #endregion

        #region Estrazione registrazioni da file di testo 

        public IEnumerable<Reg> GetRegsFromTextLines(IEnumerable<string> nonGpsLines, IEnumerable<string> gpsLines)
        {
            var gpsRegs = GetRegsFromGpsLines(gpsLines);
            var nonGpsRegs = Enumerable.Empty<Reg>();   /*GetRegsFromNonGpsLines(gpsLines);*/

            return gpsRegs.Concat(nonGpsRegs);
        }

        /// <summary>
        /// Recupera dall'elenco dei file da importare passati come parametro l'elenco delle timbrature non gps in essi contenute.
        /// Le timbrature restituite sono già epurate degli eventuali commenti contenuti nei files.
        /// </summary>
        /// <param name="filesToProcess">L'elenco dei files da processare.</param>
        /// <returns>L'elenco di timbrature gps contenute nei files passati come parametri.
        /// Le timbrature restituite sono già epurate degli eventuali commenti contenuti nei files.</returns>
        public IEnumerable<string> GetRegsNoGpsFromFiles(IEnumerable<string> files)
        {
            var regToImport = new List<string>();

            foreach (var fileToImport in files)
            {
                if (File.Exists(fileToImport))
                {
                    IEnumerable<string> filteredFileRegs = File.ReadAllLines(fileToImport)
                                                               .Where(regStr =>
                                                                    !regStr.StartsWith("*")
                                                                    && !regStr.StartsWith("#")
                                                                    && !BusinessService.IsRegLineGps(regStr)
                                                                    && !String.IsNullOrEmpty(regStr))
                                                               .ToList();

                    regToImport.AddRange(filteredFileRegs);
                }
            }

            // ritorno del valore calcolato dal metodo
            return regToImport;

        }

        /// <summary>
        /// Recupera dall'elenco dei file da importare passati come parametro l'elenco delle timbrature gps in essi contenute.
        /// Le timbrature restituite sono già epurate degli eventuali commenti contenuti nei files.
        /// </summary>
        /// <param name="filesToProcess">L'elenco dei files da processare.</param>
        /// <returns>L'elenco di timbrature gps contenute nei files passati come parametri.
        /// Le timbrature restituite sono già epurate degli eventuali commenti contenuti nei files.</returns>
        public IEnumerable<string> GetRegsGpsFromFiles(IEnumerable<string> files)
        {
            var regToImport = new List<string>();

            foreach (var fileToImport in files)
            {

                if (File.Exists(fileToImport))
                {

                    IEnumerable<string> filteredFileRegs = File.ReadAllLines(fileToImport)
                                                               .Where(regStr =>
                                                                    !regStr.StartsWith("*")
                                                                    && !regStr.StartsWith("#")
                                                                    && BusinessService.IsRegLineGps(regStr)
                                                                    && !String.IsNullOrEmpty(regStr))
                                                               .ToList();

                    if (filteredFileRegs.Any())
                    {
                        regToImport.AddRange(filteredFileRegs.Where(regStr => regStr != " ").ToList());
                    }
                }
            }

            return regToImport;
        }

        #endregion

        #region Caricamento registrazioni per confronto duplicati

        public IEnumerable<Reg> GetRegsToCompareByDate(DateTime minDate, DateTime maxDate)
        {
            return RepoManager.RegRepo.Find(reg => 
                    reg.Registrazione_Data_Ora_Orig_Reg >= minDate
                    && reg.Registrazione_Data_Ora_Orig_Reg < maxDate
                    && (reg.Pru_Id.HasValue || reg.Fru_Id.HasValue), true)
                    .ToHashSet();
        }

        #endregion

        #region Partizionamento nuove registrazioni secondo date

        public IEnumerable<Reg> Partition(IEnumerable<Reg> regsToPartition,DateTime minDate, DateTime maxDate)
        {
            return regsToPartition.Where(reg => reg.Registrazione_Data_Ora_Orig_Reg >= minDate && reg.Registrazione_Data_Ora_Orig_Reg < minDate).ToHashSet();
        }
        
        #endregion

        #region Rimozione duplicati

        public IEnumerable<Reg> RemoveDuplicates(IEnumerable<Reg> totalRegs,IEnumerable<Reg> newRegs)
        {
            return newRegs.Except(totalRegs);
        }

        #endregion

        #region Inserimento 

        public void AddNewRegs(IEnumerable<Reg> regs)
        {
            RepoManager.RegRepo.BulkInsert(regs);
        }

        #endregion
    }
}
