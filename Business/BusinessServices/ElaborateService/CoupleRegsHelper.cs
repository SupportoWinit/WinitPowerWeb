using Business.Repository;
using Common;
using Domain;
using System;
using System.Collections.Generic;

namespace Business.BusinessServices.ElaborateService
{
    internal static class CoupleRegsHelper
    {
        public static IDictionary<string, string> _errors = null;

        public static bool CoupleIfPassage(Reg reg)
        {
            // se il cantiere o il collaboratore è configurato per la gestione dei passaggi
            // allora si marca la registrazione corrente come un passaggio abbinato 
            // e si marca la registrazione corrente come da non elaborare;
            // altrimenti se procede normalmente

            if (reg.Col.Singola_Reg || reg.Cant.Singola_Reg)
            {
                reg.Registrazione_Tipo_Reg = (int)RegTypeEnum.Pass;
                reg.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                return false;
            }

            return true;
        }

        public static bool IsCurrentRegAssociated(Reg reg)
        {
            return reg.Registrazione_Stato_Reg == (int)RegStateEnum.Ass;
        }

        public static IEnumerable<KeyValuePair<string, string>> AssociateReg(Reg lastOpenInit, Reg currentRegEnd, TimeSpan maxElapsed, TimeSpan minElapsed, NocturneTypeEnum nocturneEnum)
        {
            var errors = new List<KeyValuePair<string, string>>();

            currentRegEnd.RiferimentoRRN_Reg = lastOpenInit.Reg_Id;
            TimeSpan delta = new TimeSpan(0, 0, 0);

            if ((nocturneEnum != NocturneTypeEnum.None) && (nocturneEnum != NocturneTypeEnum.Disabled))
                //Nel caso di Notturno Abilitato Verifico se l'Ora di Inizio è maggiore o minore dell'Ora di Fine
                if (currentRegEnd.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < lastOpenInit.Registrazione_Data_Ora_Fis_Reg.TimeOfDay)
                    //Se Uscita < Entrata --> Si tratta di 2 Registrazioni a Cavallo di Giorni diversi e quindi si fa  Entrata - Uscita
                    delta = (lastOpenInit.Registrazione_Data_Ora_Fis_Reg - currentRegEnd.Registrazione_Data_Ora_Fis_Reg).Duration();
                else
                    //Si tratta di 2 Registrazioni dello stesso Giorno e quindi si fa Uscita - Entrata
                    delta = currentRegEnd.Registrazione_Data_Ora_Fis_Reg - lastOpenInit.Registrazione_Data_Ora_Fis_Reg;
            else
                //Se non è attivo il Notturno si fa sempre e comunque Uscita - Entrata
                delta = currentRegEnd.Registrazione_Data_Ora_Fis_Reg - lastOpenInit.Registrazione_Data_Ora_Fis_Reg;

            if (delta > maxElapsed)
            {
                errors.Add(new KeyValuePair<string, string>(FunctionMessageEnum.AssociateReg.ToString(), BusinessService.GetLocalizedString(PowerWebResources.ERR_MAX_DURATA_REG) + lastOpenInit.Reg_Id));
                lastOpenInit.Registrazione_Stato_RegEnum |= RegStateEnum.ErrMax;
                currentRegEnd.Registrazione_Stato_RegEnum |= RegStateEnum.ErrMax;
            }
            else if (delta < minElapsed)
            {
                errors.Add(new KeyValuePair<string, string>(FunctionMessageEnum.AssociateReg.ToString(), BusinessService.GetLocalizedString(PowerWebResources.ERR_MIN_DURATA_REG) + lastOpenInit.Reg_Id));
                lastOpenInit.Registrazione_Stato_RegEnum |= RegStateEnum.ErrMin;
                currentRegEnd.Registrazione_Stato_RegEnum |= RegStateEnum.ErrMin;
            }
            else
            {
                lastOpenInit.Registrazione_Stato_RegEnum |= RegStateEnum.Ass;
                currentRegEnd.Registrazione_Stato_RegEnum |= RegStateEnum.Ass;
                lastOpenInit.Registrazione_Tipo_RegEnum = currentRegEnd.Registrazione_Tipo_RegEnum = RegTypeEnum.None;
                currentRegEnd.KM_Reg = lastOpenInit.KM_Reg;
                //currentRegEnd.Motivazione_Reg_Id = lastOpenInit.Motivazione_Reg_Id;
                currentRegEnd.Note_Reg = lastOpenInit.Note_Reg;
            }
            return errors;
        }

        public static bool AreRegsReadyToAssociate(Reg currentReg, Reg lastOpen)
        {
            // le registrazioni in porcesso risultano abbinabili solamente se:
            // - le due registrazioni sono nella stessa data
            // - le due registrazioni hanno lo stesso cantiere
            // - la registrazione di uscita è marcata come uscita o senza direzione


            return currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date
                && currentReg.Cant_Id == lastOpen.Cant_Id
                && (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U);
        }

        /// <summary>
        /// Associa le registrazioni in caso di notturno disabilitato
        /// </summary>
        /// <param name="exit">The exit.</param>
        /// <param name="entrance">The entrance.</param>
        /// <param name="minElapsed">The minimum elapsed.</param>
        /// <param name="maxElapsed">The maximum elapsed.</param>
        /// <param name="nocturneType">Type of the nocturne.</param>
        /// <returns>
        /// La registrazione aperta (la prossima entrata (null se è stata abbinata))
        /// </returns>
        public static Reg ManageNoNocturneAssociation(Reg exit, Reg entrance, TimeSpan minElapsed, TimeSpan maxElapsed, NocturneTypeEnum nocturneType)
        {
            Reg lastOpen = null;

            if (CoupleRegsHelper.AreRegsReadyToAssociate(exit, entrance))
            {
                // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                if (entrance.Motivazione_Reg_Id == exit.Motivazione_Reg_Id)
                {
                    // tentativo di abbinamento delle registrazioni
                    var currentRegErrors = AssociateReg(entrance, exit, maxElapsed, minElapsed, nocturneType);

                    //_errors.AddRange(currentRegErrors);
                    // una volta effettuato il tentativo di abbinamento si riparte da una nuova coppia di entrata/uscita
                    lastOpen = null;
                }
                else // se le motivazioni non sono le stesse si procede alla coppia di reg successiva (eventualmente mantenendo la corrente come entrata)
                    lastOpen = IsCurrentRegEntranceByFlag(exit) ? exit : null;
            }
            else if (IsCurrentRegEntranceByFlag(exit))
            {
                // se le registrazioni non sono abbinabili secondo i parametri del notturno impsotati
                // e la registrzione corrente è marcata con direzione entrata o senza direzione
                // allora si procede a impostare la registrazione corrente come nuova entrata
                lastOpen = exit;
            }
            else // altrimenti si riparte con una nuova coppia di entrata e uscita
                lastOpen = null;

            return lastOpen;

        }

        /// <summary>
        // In caso di notturno le registrazioni risultano abbinabili se:
        // - la registrazione marcata come uscita è nello stesso o successivo giorno rispetto all'entrata,
        //   ha lo stesso cantiere dell'entrata ed è identificata come uscita o senza direzione;
        // altrimenti, se la registrazione non risulta abbinabile:
        // - se l'ultima registrazione risulta essere una potenziale entrata (senza direzione o con direzione E) allora la si tratta come tale
        // - altrimenti si riparte scartando l'intero tenativo di abbinamento

        /// </summary>
        /// <param name="lastOpen">The last open.</param>
        /// <param name="currentReg">The current reg.</param>
        /// <param name="minElapsed">The minimum elapsed.</param>
        /// <param name="maxElapsed">The maximum elapsed.</param>
        /// <param name="nocturneThreshold">The nocturne threshold.</param>
        /// <param name="nocturneType">Type of the nocturne.</param>
        /// <returns></returns>
        public static Reg ManageMidnightNocturneAssociation(Reg lastOpen, Reg currentReg, TimeSpan minElapsed, TimeSpan maxElapsed, TimeSpan nocturneThreshold, NocturneTypeEnum nocturneType)
        {
            if ((currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date.AddDays(1) || currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date)
                                        && currentReg.Cant_Id == lastOpen.Cant_Id && (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U))
            {

                // se le registrazioni sono nello stesso giorno allora si verifca la coerenza della possibile uscita con il threshold del notturno:
                // - se l'uscita è successiva al threshold allora si verifica che anche lentrata lo sia (caso di registrazione diurna in notturnO) e,
                //   in caso di stessa motivazione si procede al tentativo di abbinamento, azzerando il gruppo per ripartire
                //   da una nuova entrata (in caso le motivazioni non siano coerenti tra le due timbrature si passa ad elaborare il 
                //   gurppo successivo eventualmente mantenendo la presente registrazione in elaborazione come entrata).
                // - se invece l'uscita è successiva al threshold e l'entrata anche (caso di timbratura a cavallo del limite di notturno) 
                //   si procede al loro tentativo di abbinamento (con attenzione alle motivazioni come di cui sopra solamente se sono entrata e uscita.
                // - in caso l'uscita sia inferiore o uguale al threshold e lo sia anche l'entrata (caso di timbratura dopo mezzanotte ma inferiore
                //   al limite di notturno) allora si procede al tentativo di abbinamento delle timbrature) con l'attezione alla motivazione
                //   di cui sopra.
                // - altrimenti ci si ritrova nel caso in cui la registrazione è a in giornata ma sicuramente antecedente al threshold, in questo
                //   si tenta l'abbinamento con la solita attenzione alle motivazioni
                // se le registrazioni invece sono di giorni diversi (l'uscita nel giorno successivo all'entrata) si verifica la conformità dell'uscita
                // al threshold:
                // - se l'uscita risulta inferiore al threshold allora, coerentemente con le motivazioni, si procede al tentativo di abbinamento;
                // - se le timbrature sono a cavallo del notturno ma la precedente è un'entrata e la successiva un'uscita allora si procede al loro abbinamento (di modo
                //   da riuscire a gestire con la direzione turni consecutivi di lavoro)
                // - altrimenti si riprate con un nuovo gruppo

                // se le due regitrazioni si trovano nella stessa data
                if (currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date)
                {
                    // se l'ora d'uscita è superiore al limite di notturno
                    if (currentReg.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > nocturneThreshold)
                    {
                        // se l'ora d'entrata è superiore al limite di notturno (timbrature nello stesso giorno sopra il limite di notturno)
                        if (lastOpen.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > nocturneThreshold)
                        {
                            // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                            if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                            {
                                // tentativo di abbinamento delle registrazioni
                                _errors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                                lastOpen = null;
                            }
                            else // se le motivazioni non sono le stesse si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                        }
                        else // se l'ora d'entrata è maggiore del limite notturno ma l'entrata non lo è (timbratura a cavallo dell'ora di notturno nella stessa giornata)
                        {
                            // se l'entrata ha il flag di entrata e l'uscita ha il flag di uscita
                            if (lastOpen.FlagEURegTypeEnum == FlagEURegTypeEnum.E && currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U)
                            {
                                // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                                if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                                {
                                    // in caso non ci sia concordanza di threshold, siamo comunque nello stesso giorno e, se si sta trattando un'entrata e un'uscita
                                    // allora si procede all'abbinamento
                                    _errors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                                    // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                                    lastOpen = null;
                                }
                                else // se le motivazioni non sono le stesse si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                    lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                            }
                            else // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                                lastOpen = currentReg.FlagEURegTypeEnum != FlagEURegTypeEnum.U ? currentReg : null;
                        }
                    }
                    else if ((currentReg.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= nocturneThreshold) && (lastOpen.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= nocturneThreshold))
                    {
                        // se l'entrata e l'uscita sono nello stesso giorno ed entrambe sono minori o uguali al limite del notturno allora devo tentare di abbinarle

                        // se esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                        if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                        {
                            // tentativo di abbinamento delle registrazioni
                            _errors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                            // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                            lastOpen = null;
                        }
                        else // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                            lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                    }
                    else if (lastOpen.FlagEURegTypeEnum == FlagEURegTypeEnum.E && currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U)
                    {
                        // se l'entrata e l'uscita sono nello stesso giorno ma a cavallo del threshold allora si tenta l'abbinamento solamente se 
                        // l'entrata è marcata come direzione entrata e l'uscita è marcata come direzione uscita

                        // se esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                        if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                        {
                            // tentativo di abbinamento delle registrazioni
                            _errors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                            // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                            lastOpen = null;
                        }
                        else // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                            lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                    }
                }
                else if (currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date.AddDays(1))
                {
                    // altrimeni se la registrazione d'uscita risulta essere il giorno successivo all'entrata

                    // si procede all'abbinamento delle reg solamente se l'uscita è compresa prima del threshold
                    if (currentReg.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= nocturneThreshold)
                    {
                        // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                        if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                        {
                            // tentativo di abbinamento delle registrazioni
                            _errors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                            // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                            lastOpen = null;
                        }
                        else // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                            lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                    }
                    else if (lastOpen.FlagEURegTypeEnum == FlagEURegTypeEnum.E && currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U)
                    {
                        // se l'entrata e l'uscita sono in giorni diversi ma a cavallo del threshold allora si tenta l'abbinamento solamente se 
                        // l'entrata è marcata come direzione entrata e l'uscita è marcata come direzione uscita

                        // se esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                        if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                        {
                            // tentativo di abbinamento delle registrazioni
                            _errors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                            // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                            lastOpen = null;
                        }
                        else // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                            lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                    }

                    else // altrimenti si riparte con una nuova coppia di entrata e uscita
                        lastOpen = currentReg;
                }
            }
            else if (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E)
            {
                // se le registrazioni non sono abbinabili secondo i parametri del notturno impsotati
                // e la registrzione corrente è marcata con direzione entrata o senza direzione
                // allora si procede a impostare la registrazione corrente come nuova entrata
                lastOpen = currentReg;
            }
            else // altrimenti si riparte con una nuova coppia di entrata e uscita
                lastOpen = null;

            return lastOpen;
        }

        /// <summary>
        // In caso di notturno per durata le registrazioni risultano abbinabili se:
        // - la registrazione marcata come uscita è nello stesso o successivo giorno rispetto all'entrata e ,
        //   ha lo stesso cantiere dell'entrata ed è identificata come uscita o senza direzione;
        // altrimenti, se la registrazione non risulta abbinabile:
        // - se l'ultima registrazione risulta essere una potenziale entrata (senza direzione o con direzione E) allora la si tratta come tale
        // - altrimenti si riparte scartando l'intero tenativo di abbinamento
        /// </summary>
        /// <param name="lastOpen">The last open.</param>
        /// <param name="currentReg">The current reg.</param>
        /// <param name="minElapsed">The minimum elapsed.</param>
        /// <param name="maxElapsed">The maximum elapsed.</param>
        /// <param name="nocturneThreshold">The nocturne threshold.</param>
        /// <param name="nocturneType">Type of the nocturne.</param>
        /// <returns></returns>
        public static Reg ManageNocturneDurationAssociation(Reg lastOpen, Reg currentReg, TimeSpan minElapsed, TimeSpan maxElapsed,TimeSpan nocturneDuration, TimeSpan nocturneThreshold, NocturneTypeEnum nocturneType)
        {
            if ((currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date.AddDays(1))
                                        && currentReg.Cant_Id == lastOpen.Cant_Id)
            {

                //Viene per prima cosa controlalta che la registrazione di uscita sia nell'intervallo che scatta dall'entrata fino alla durata massima del notturno
                //se così non è si passa alla registrazione successiva

                DateTime nocturnBoundMax = new DateTime();

                //viene sommata all'ultima registrazione la durata massima del notturno per verificare che la registrazione successiva ricada nel range
                nocturnBoundMax = lastOpen.Registrazione_Data_Ora_Fis_Reg.AddMinutes(nocturneDuration.TotalMinutes);

                // si procede all'abbinamento delle reg solamente se l'uscita è compresa prima del threshold
                if (currentReg.Registrazione_Data_Ora_Fis_Reg <= nocturnBoundMax)
                {
                    // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                    if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                    {
                        // tentativo di abbinamento delle registrazioni
                        _errors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                        // dopo l'abbinamento, comunque, si riprende il ciclo con una nuova entrata
                        lastOpen = null;
                    }
                    else
                        // altrimenti la registrazione non è abbinabile e quidni si procede alla coppia di reg successiva (indicando eventualmente la presente come entrata)
                        lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                }
                //se le registrazione cadono furoi dalla durata massima del nottunro non vengono abbinate
                else
                {
                    lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                }
            }

            //se le registrazioni contigue non apparetnego a giorni diversi  ma apprtengono allo stesso giorno
            else
            {
                // le registrazioni in porcesso risultano abbinabili solamente se:
                // - le due registrazioni sono nella stessa data
                // - le due registrazioni hanno lo stesso cantiere
                // - la registrazione di uscita è marcata come uscita o senza direzione
                if (currentReg.Registrazione_Data_Ora_Fis_Reg.Date == lastOpen.Registrazione_Data_Ora_Fis_Reg.Date && currentReg.Cant_Id == lastOpen.Cant_Id &&
                (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U))
                {
                    // si esegue l'abbinamento solamente se le registrazioni hanno la stessa motivazione
                    if (lastOpen.Motivazione_Reg_Id == currentReg.Motivazione_Reg_Id)
                    {
                        // tentativo di abbinamento delle registrazioni
                        _errors.AddRange(AssociateReg(lastOpen, currentReg, maxElapsed, minElapsed, nocturneType));

                        // una volta effettuato il tentativo di abbinamento si riparte da una nuova coppia di entrata/uscita
                        lastOpen = null;
                    }
                    else // se le motivazioni non sono le stesse si procede alla coppia di reg successiva (eventualmente mantenendo la corrente come entrata)
                        lastOpen = currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E ? currentReg : null;
                }
                else if (currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E)
                {
                    // se le registrazioni non sono abbinabili secondo i parametri del notturno impsotati
                    // e la registrzione corrente è marcata con direzione entrata o senza direzione
                    // allora si procede a impostare la registrazione corrente come nuova entrata
                    lastOpen = currentReg;
                }
                else // altrimenti si riparte con una nuova coppia di entrata e uscita
                    lastOpen = null;

            }

            return lastOpen;
        }

        #region Calcolo durata minima - massima reg

        /// <summary>
        // Si calcola la durata minima ammessa per le registrazioni
        // I dati sono calcolati in gerarchia:
        // - si recuperano i dati del collaboratore;
        // - se non configurati si passa a recuperarli dal cantiere.
        // - se anche sul cantiere non sono configurati allora li si recupera dai parametri
        /// </summary>
        /// <param name="currentCol">Collaboratore corrente.</param>
        /// <param name="currentCant">Cantiere corrente.</param>
        /// <returns></returns>
        public static TimeSpan GetLocalMinDurataReg(Col currentCol, Cant currentCant)
        {
            TimeSpan minElapsed = TimeSpan.Zero;

            //parametro da collaboratore
            minElapsed = currentCol.Durata_Min_Ril_Col ?? TimeSpan.Zero;

            // se il parametro di durata minima della timbratura non è configurato sul collaboratore, si procede al suo recupero dal cantiere
            if (minElapsed == TimeSpan.Zero)
                minElapsed = currentCant.Durata_Min_Ril_Can ?? TimeSpan.Zero;

            // se il parametro di durata minima della timbratura non è configurato ne sul collaboratore ne sul cantiere,
            // si procede al suo recupero dalla scheda parametri generali
            if (minElapsed == TimeSpan.Zero)
                minElapsed = RepoManager.ParamRepo.ParametersRow.Default_Durata_Min_Ril ?? TimeSpan.Zero;

            return minElapsed;

        }

        /// <summary>
        // Si calcola la durata massima ammessa per le registrazioni
        // I dati sono calcolati in gerarchia:
        // - si recuperano i dati del collaboratore;
        // - se non configurati si passa a recuperarli dal cantiere.
        // - se anche sul cantiere non sono configurati allora li si recupera dai parametri
        /// </summary>
        /// <param name="currentCol">Collaboratore corrente.</param>
        /// <param name="currentCant">Cantiere corrente.</param>
        /// <returns></returns>
        public static TimeSpan GetLocalMaxDurataReg(Col currentCol, Cant currentCant)
        {
            TimeSpan maxElapsed = TimeSpan.Zero;

            //parametro da collaboratore
            maxElapsed = currentCol.Durata_Max_Ril_Col ?? TimeSpan.Zero;

            // se il parametro di durata minima della timbratura non è configurato sul collaboratore, si procede al suo recupero dal cantiere
            if (maxElapsed == TimeSpan.Zero)
                maxElapsed = currentCant.Durata_Max_Ril_Can ?? TimeSpan.Zero;

            // se il parametro di durata minima della timbratura non è configurato ne sul collaboratore ne sul cantiere,
            // si procede al suo recupero dalla scheda parametri generali
            if (maxElapsed == TimeSpan.Zero)
                maxElapsed = RepoManager.ParamRepo.ParametersRow.Default_Durata_Max_Ril ?? TimeSpan.Zero;

            return maxElapsed;

        }

        #endregion

        #region Calcolo configurazione notturno


        /// <summary>
        /// Controlla se la configurazione locale per l'abbinamento reg in caso di notturno è 
        /// correttamente configurata
        /// </summary>
        /// <param name="nocturneType">Tipo notturno</param>
        /// <param name="nocturneThreshold">Soglia notturno</param>
        /// <param name="nocturneDuration">Durata notturno</param>
        public static bool IsNocturneConfigured(NocturneTypeEnum nocturneType, TimeSpan nocturneThreshold, TimeSpan nocturneDuration)
        {
            return nocturneType != NocturneTypeEnum.None && nocturneType != NocturneTypeEnum.Disabled && (nocturneThreshold.Ticks > 0 || nocturneDuration.Ticks > 0);
        }


        /// <summary>
        /// Recupera la durata di notturno utilizzata 
        /// </summary>
        /// <param name="currentCol">Collaboratore corrente</param>
        /// <param name="currentCant">Cantiere corrente</param>
        /// <returns></returns>
        public static TimeSpan GetLocalNocturneDuration(Col currentCol, Cant currentCant)
        {
            //Recupero configurazione notturno
            Tuple<bool, NocturneTypeEnum, TimeSpan, TimeSpan> nocturneGlobalConfiguration = RepoManager.ParamRepo.NocturneGeneralConfiguration;
            bool nocturneModuleActive = nocturneGlobalConfiguration.Item1;

            NocturneTypeEnum nocturneType = NocturneTypeEnum.None;

            TimeSpan nocturneDuration = TimeSpan.Zero;

            if (nocturneModuleActive)
            {
                nocturneType = currentCol.NocturneTypeEnum;

                nocturneDuration = currentCol.Durata_Notturno_Col ?? TimeSpan.Zero;

                // se il collaboratore non risulta configurato allora si procede al recupero delle configurazioni del cantiere
                if (nocturneType == NocturneTypeEnum.None || nocturneType == NocturneTypeEnum.Disabled)
                {
                    //vengono estratti i praemtri dal cantiere
                    nocturneType = currentCant.NocturneTypeEnum;
                    nocturneDuration = currentCant.Durata_Notturno_Can ?? TimeSpan.Zero;
                }

                // se il collaboratore e il cantiere non risultano configurati allora si procede al recupero delle configurazioni generali nei parametri
                // (si controlla solamente il valore "None" perché se disabilitato su cantiere e collaboratore allora non lo si processa)
                if (nocturneType == NocturneTypeEnum.None)
                {
                    //vengono estratti i parametri del notturno dalla scheda parametri
                    nocturneType = nocturneGlobalConfiguration.Item2;
                    nocturneDuration = nocturneGlobalConfiguration.Item3;
                }

            }

            return nocturneDuration;
        }

        /// <summary>
        ///Recupera la soglia di notturno utilizzata 
        /// </summary>
        /// <param name="currentCol">Collaboratore corrente</param>
        /// <param name="currentCant">Cantiere corrente</param>
        /// <returns></returns>
        public static TimeSpan GetNocturneLocalThreshold(Col currentCol, Cant currentCant)
        {
            Tuple<bool, NocturneTypeEnum, TimeSpan, TimeSpan> nocturneGlobalConfiguration = RepoManager.ParamRepo.NocturneGeneralConfiguration;
            bool nocturneModuleActive = nocturneGlobalConfiguration.Item1;

            NocturneTypeEnum nocturneType = NocturneTypeEnum.None;

            TimeSpan nocturneThreshold = TimeSpan.Zero;

            if (nocturneModuleActive)
            {
                // impostazione della configurazione del collaboratore
                nocturneType = currentCol.NocturneTypeEnum;

                //viene estratta la durata del notturno e la nuova mezzanotte
                nocturneThreshold = currentCol.Durata_Max_Gruppo_Notte_Ril_Col ?? TimeSpan.Zero;

                if (nocturneType == NocturneTypeEnum.None || nocturneType == NocturneTypeEnum.Disabled)
                {
                    //vengono estratti i praemtri dal cantiere
                    nocturneType = currentCant.NocturneTypeEnum;
                    nocturneThreshold = currentCant.Durata_Max_Gruppo_Notte_Ril_Can ?? TimeSpan.Zero;
                }

                if (nocturneType == NocturneTypeEnum.None)
                {
                    //vengono estratti i parametri del notturno dalla scheda parametri
                    nocturneType = nocturneGlobalConfiguration.Item2;
                    nocturneThreshold = nocturneGlobalConfiguration.Item3;
                }


            }
            return nocturneThreshold;

        }


        /// <summary>
        /// Recupera il tipo di notturno utilizzato 
        /// </summary>
        /// <param name="currentCol">Collaboratore corrente</param>
        /// <param name="currentCant">Cantiere corrente</param>
        /// <returns></returns>
        public static NocturneTypeEnum GelLocalNoctureType(Col currentCol, Cant currentCant)
        {
            Tuple<bool, NocturneTypeEnum, TimeSpan, TimeSpan> nocturneGlobalConfiguration = RepoManager.ParamRepo.NocturneGeneralConfiguration;
            bool nocturneModuleActive = nocturneGlobalConfiguration.Item1;


            NocturneTypeEnum nocturneType = NocturneTypeEnum.None;

            if (nocturneModuleActive)
            {
                nocturneType = currentCol.NocturneTypeEnum;

                if (nocturneType == NocturneTypeEnum.None || nocturneType == NocturneTypeEnum.Disabled)
                {
                    //vengono estratti i praemtri dal cantiere
                    nocturneType = currentCant.NocturneTypeEnum;
                }

                // se il collaboratore e il cantiere non risultano configurati allora si procede al recupero delle configurazioni generali nei parametri
                // (si controlla solamente il valore "None" perché se disabilitato su cantiere e collaboratore allora non lo si processa)
                if (nocturneType == NocturneTypeEnum.None)
                {
                    //vengono estratti i parametri del notturno dalla scheda parametri
                    nocturneType = nocturneGlobalConfiguration.Item2;
                }

            }

            return nocturneType;

        }



        /// <summary>
        /// Recupera la data minima di ricerca per l'associazione dell'uscita
        /// </summary>
        /// <param name="currentReg">Registrazione corrente</param>
        /// <param name="nocturneType">Tipo di notturno</param>
        /// <param name="nocturneThreshold">Soglia di notturno</param>
        /// <returns></returns>
        public static DateTime GetNocturneMinSeachDate(Reg currentReg, NocturneTypeEnum nocturneType, TimeSpan nocturneThreshold)
        {
            DateTime minSearchDate = DateTime.MinValue;

            if (nocturneType == NocturneTypeEnum.None || nocturneType == NocturneTypeEnum.Disabled || nocturneThreshold.Ticks <= 0)
            {
                minSearchDate = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month, currentReg.Registrazione_Data_Ora_Fis_Reg.Day, 0, 0, 0);
            }
            //se è attivo il notturno con "nuova M
            else if (nocturneType == NocturneTypeEnum.OverMidnight && nocturneThreshold.Ticks > 0)
            {
                //calcolo il giorno successivo alla registrazione corrente
                DateTime nextRegDate = currentReg.Registrazione_Data_Ora_Fis_Reg.AddDays(1);

                //vengono create le date minime e massime di ricerca in base al valore della soglia
                minSearchDate = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month, currentReg.Registrazione_Data_Ora_Fis_Reg.Day, nocturneThreshold.Hours, nocturneThreshold.Minutes, nocturneThreshold.Seconds);
            }

            return minSearchDate;
        }


        /// <summary>
        /// Recupera la data massima di ricerca per l'associazione dell'uscita
        /// </summary>
        /// <param name="currentReg">Registrazione corrente</param>
        /// <param name="nocturneType">Tipo di notturno</param>
        /// <param name="nocturneThreshold">Soglia di notturno</param>
        /// <returns></returns>
        public static DateTime GetNocturneMaxSeachDate(Reg currentReg, NocturneTypeEnum nocturneType, TimeSpan nocturneThreshold)
        {
            DateTime maxSearchDate = DateTime.MinValue;

            if (nocturneType == NocturneTypeEnum.None || nocturneType == NocturneTypeEnum.Disabled || nocturneThreshold.Ticks <= 0)
            {
                maxSearchDate = new DateTime(currentReg.Registrazione_Data_Ora_Fis_Reg.Year, currentReg.Registrazione_Data_Ora_Fis_Reg.Month, currentReg.Registrazione_Data_Ora_Fis_Reg.Day, 23, 59, 59);
            }

            //se è attivo il notturno con "nuova M
            else if (nocturneType == NocturneTypeEnum.OverMidnight && nocturneThreshold.Ticks > 0)
            {
                //calcolo il giorno successivo alla registrazione corrente
                DateTime nextRegDate = currentReg.Registrazione_Data_Ora_Fis_Reg.AddDays(1);

                //vengono create le date minime e massime di ricerca in base al valore della soglia
                maxSearchDate = new DateTime(nextRegDate.Year, nextRegDate.Month, nextRegDate.Day, nocturneThreshold.Hours, nocturneThreshold.Minutes, nocturneThreshold.Seconds);
            }

            return maxSearchDate;
        }

        #endregion

        #region Informazioni reg

        /// <summary>
        /// Determina se la registrazione è un'entrata.
        /// </summary>
        /// <param name="currentReg">Reg corrente</param>
        public static bool IsCurrentRegEntranceByFlag(Reg currentReg)
        {
            return currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.E;
        }

        /// <summary>
        /// Determina se la registrazione è un'entrata.
        /// </summary>
        /// <param name="currentReg">Reg corrente</param>
        public static bool IsCurrentRegExitByFlag(Reg currentReg)
        {
            return currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.None || currentReg.FlagEURegTypeEnum == FlagEURegTypeEnum.U;
        }

        #endregion

    }
}
