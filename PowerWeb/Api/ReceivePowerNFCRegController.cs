using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Common;

namespace PowerWeb.Api
{
    public class ReceivePowerNfcRegs2Controller : GenericUploadFileApi
    {

        #region Constants

        /// <summary>
        /// Il separatore tra un valore e l'altro del file delle registrazioni
        /// </summary>
        private const string RegsFileSeparator = ";";

        /// <summary>
        /// Lo string format da applicare all'anno registrazione nella scrittura delle registrazioni su file
        /// </summary>
        private const string RegsFileYearStringFormat = "0000";

        /// <summary>
        /// Lo string format da applicare al mese registrazione nella scrittura delle registrazioni su file
        /// </summary>
        private const string RegsFileMonthStringFormat = "00";

        /// <summary>
        /// Lo string format da applicare al giorno registrazione nella scrittura delle registrazioni su file
        /// </summary>
        private const string RegsFileDayStringFormat = "00";

        /// <summary>
        /// Lo string format da applicare all'ora registrazione nella scrittura delle registrazioni su file
        /// </summary>
        private const string RegsFileHourstringFormat = "00";

        /// <summary>
        /// Lo string format da applicare al minuto registrazione nella scrittura delle registrazioni su file
        /// </summary>
        private const string RegsFileMinuteStringFormat = "00";

        /// <summary>
        /// Il prefisso da utilizzare nella generazione del file delle timbrature
        /// </summary>
        private const string RegsFileNamePrefix = "Reg_DaImportare_";

        /// <summary>
        /// Il separatore di data e ora all'interno del nome file delle registrazioni
        /// </summary>
        private const string RegsFileNameDateTimeSeparator = "-";

        /// <summary>
        /// L'estensione del nome del file delle registrazioni
        /// </summary>
        private const string RegsFileNameExtension = ".txt";

        /// <summary>
        /// La keyword per capire che nella registrazione ci sono informazioni aggiuntive
        /// </summary>
        private const string infoAgg = "INFOAGG";

        /// <summary>
        /// la keyword che indica la presenza di un turno
        /// </summary>
        private const string turno = "TURNO";

        /// <summary>
        /// la keyword che indica la presenza di un sottocantiere
        /// </summary>
        private const string sottoCantiere = "SUBCANT";

        /// <summary>
        /// Keyword che dice che vi è un tipo di attività
        /// </summary>
        private const string tipoAttivita = "TIPOATT";

        /// <summary>
        /// Keyword che dice che vi è una squadra
        /// </summary>
        private const string squadraType = "SQUADRA";

        /// <summary>
        /// Chiave delle informazioni aggiuntive riguardo alla matricola portatile che ha effettuato la timbratura di attività
        /// </summary>
        private const string tipoPruCodeForActivity = "PRUCODEFORACTIVITY";

        /// <summary>
        /// Costante indicante che la registrazione fa parte di un gruppo TAG+GPS
        /// </summary>
        private const string isTagReferenced = "1";

        /// <summary>
        /// Costante indicante che la registrazione NON fa parte di un gruppo TAG+GPS
        /// </summary>
        private const string isNotTagReferenced = "0";

        /// <summary>
        /// Costante indicante che la riga GPS si riferisce alla LATITUDINE
        /// </summary>
        private const string latitudeLine = "0";

        /// <summary>
        /// Costante indicante che la riga GPS si riferisce alla LONGITUDINE
        /// </summary>
        private const string longitudeLine = "1";


        #endregion

        #region Protected Methods

        /// <summary>
        /// Esegue l'operazione richiesta dalla API.
        /// </summary>
        /// <returns>
        /// La risposta da ritornare all'invocatore della API
        /// </returns>
        protected override HttpStatusCode ExecuteOperation()
        {

            // di default la web api ritorna un valore di errore internoo
            var returnCode = HttpStatusCode.InternalServerError;

            // calcolod del nome del file in cui appoggiare il json ricevuto
            string jsonFilePath = GetTmpJsonFilePath();

            // recupero dalla richiesta http lo stream con i dati da scrivere
            var task = this.Request.Content.ReadAsStreamAsync();
            task.Wait();

            // inizializzazione della variabile che indica l'avvenuta scrittura con errore
            bool jsonWroteWithErrors = false;

            using (Stream requestStream = task.Result)
            {
                // scrittura dello stream su file
                using (Stream fileStream = File.Create(jsonFilePath))
                {
                    try
                    {
                        requestStream.CopyTo(fileStream);
                        fileStream.Close();
                        requestStream.Close();
                    }
                    catch (Exception)
                    {
                        jsonWroteWithErrors = true;
                    }
                    finally
                    {
                        // in ogni caso al termine dell'operazione si procede allo svuotamento dello stream (di modo da liberare il file)
                        fileStream.Close();
                        fileStream.Dispose();
                    }
                }
            }

            // si procede alla trasformazione del json ricevuto in un file da importare solo se non si sono verificati problemi
            // in scrittura del file
            if (!jsonWroteWithErrors)
            {
                if (CreateRegFileFromJson(File.ReadAllText(jsonFilePath)))
                    returnCode = HttpStatusCode.OK;
                else
                    returnCode = HttpStatusCode.InternalServerError;
            }

            // in ogni caso al termine dell'operazione, se il file json processato risulta presente, si procede alla sua cancellazione
            if (File.Exists(jsonFilePath))
            {
                string backupFilePath = string.Concat(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path.Replace("~", ""));// Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Input_JSON_Backup_Path.Replace("~", ""));
                Business.BusinessService.BackupProcessedFiles(new List<string>() { jsonFilePath }, backupFilePath);
            }

            // ritornod della risposta calcolata dal metodo
            return returnCode;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calcola e restituisce il percorso completo di salvataggio del file temporaneo json utilizzato per la ricezione delle timbrature.
        /// </summary>
        /// <returns>Il percorso completo di salvataggio del file temporaneo json utilizzato per la ricezione delle timbrature ,.</returns>
        private string GetTmpJsonFilePath()
        {
            // calcolo della cartella (FilesInput applicativo) di scarico
            string filesInputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Input_Path.Replace("~", "").Replace("\\", ""));

            // calcolo del nome del file json
            DateTime now = DateTime.Now;
            string jsonFileName = String.Format("TmpJsonDownload-{0}.json", now.ToString("yyyy-MM-dd-hh-mm-ss-fff"));

            // ritorno della combinazione di cartella e nome del file 
            return Path.Combine(filesInputPath, jsonFileName);
        }

        /// <summary>
        /// Crea il file delle registrazionii a partire dalla stringa con i dati in formato json passata come parametro.
        /// </summary>
        /// <param name="jsonData">I dati json da processare.</param>
        /// <returns><c>true</c> in caso l'operazione sia anadata a buon fine; false in caso contrario.</returns>
        private bool CreateRegFileFromJson(string jsonData)
        {
            // inizializzazione del valore di ritorno del metodo
            bool operationSuccessful = false;

            try
            {
                dynamic regs = null;

                // parse della stringa json con i dati da trattare
                regs = System.Web.Helpers.Json.Decode(jsonData);

                // inizializzazione della lista di stringhe che conterrà il file da far importare all'applictivo
                List<string> txtRegs = new List<string>();

                //la stringa che rappresenta il codice del badge
                string lastDeviceCode = string.Empty,
                       txtReg = string.Empty,
                       turnType = string.Empty,
                       collaboratoriSquadra = string.Empty,
                       sottoCantiereDescrizione = string.Empty,
                       activityType = string.Empty,
                       pruCodeForActivity = string.Empty,
                       trasponderType = string.Empty,
                       year = string.Empty,
                       month = string.Empty,
                       day = string.Empty,
                       hours = string.Empty,
                       minutes = string.Empty,
                       direzioneCardinaleLatitudine = "N",
                       direzioneCardinaleLongitudine = "E",
                       notaReg = string.Empty;
                

                // latitudine e longitudine
                decimal latitude = 0,
                       longitude = 0;

                //stringa che rappresenta i tipi attività splittati
                string[] splittedActivityType = new string[] { };

                // si elaborano tutte le timbrature tra i dati json passati come parametro
                foreach (dynamic reg in regs)
                {
                    #region ESTRAZIONE DATI

                    // calcolo dell'ultimo codice device calcolato
                    lastDeviceCode = reg.DeviceCode.ToString();

                    //calcolo del tipo di turno
                    turnType = reg.TurnCode.ToString();
                    
                    //la descrizione del sottocantiere
                    sottoCantiereDescrizione = reg.SubCantDesc.ToString();

                    //calcolo del tipo di attività
                    activityType = reg.ActivityTypeCode.ToString();

                    latitude = reg.Latitude ?? 0;
                    longitude = reg.Longitude ?? 0;
                    trasponderType = reg.TransponderType ?? Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G);

                    // calcolo della matricola pru che ha effettuato la timbratura attività
                    pruCodeForActivity = reg.PruCodeForActivity;

                    notaReg = reg.RegistrationNote;

                    if (activityType.Contains(","))
                        splittedActivityType = activityType.Split(',');
                    else if (activityType != "")
                        splittedActivityType = new string[1] { activityType };

                    // calcolo della data e ora della registrazione
                    DateTime regDateTime = Convert.ToDateTime((string)reg.RegistrationDateTime);
                    year = regDateTime.Year.ToString(RegsFileYearStringFormat);
                    month = regDateTime.Month.ToString(RegsFileMonthStringFormat);
                    day = regDateTime.Day.ToString(RegsFileDayStringFormat);
                    hours = regDateTime.TimeOfDay.Hours.ToString(RegsFileHourstringFormat);
                    minutes = regDateTime.TimeOfDay.Minutes.ToString(RegsFileMinuteStringFormat);

                    // calcolo della direzione della timbratura
                    string regDirection = string.IsNullOrEmpty((string)reg.Direction) ? " " : (string)reg.Direction;

                    #endregion

                    #region REGISTRAZIONE DI SQUADRA 

                    if (reg.RegistrationSquadra != null && reg.RegistrationSquadra.Length > 0)
                    {


                        foreach (var cod in reg.RegistrationSquadra)
                        {

                            //Timbratura di tipo solo GPS
                            if (reg.transponderType == Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G))
                            {
                                if (latitude < 0)
                                {
                                    direzioneCardinaleLatitudine = "S";
                                }
                                if (longitude < 0)
                                {
                                    direzioneCardinaleLongitudine = "O";
                                }

                                txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}"
                                    , RegsFileSeparator
                                    , cod
                                    , CommonService.AggiungiZeriASinistra(latitude.ToString("00.0000000").Remove(2, 1), 10)
                                    , year
                                    , month
                                    , day
                                    , hours
                                    , minutes
                                    , isNotTagReferenced //se è solo GPS, non è tagReferenced, altrimenti sì
                                    , latitudeLine
                                    , direzioneCardinaleLatitudine
                                    );

                                txtRegs.Add(txtReg);

                                txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}"
                                    , RegsFileSeparator
                                    , cod
                                    , CommonService.AggiungiZeriASinistra(longitude.ToString("00.0000000").Remove(2, 1), 10)
                                    , year
                                    , month
                                    , day
                                    , hours
                                    , minutes
                                    , isNotTagReferenced //se è solo GPS, non è tagReferenced, altrimenti sì
                                    , longitudeLine
                                    , direzioneCardinaleLongitudine
                                    );

                                txtRegs.Add(txtReg);
                            }
                            else

                            //Altrimenti timbratura solo NFC o QRCODE
                            {
                                txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}"
                                    , RegsFileSeparator
                                    , reg.DeviceCode
                                    , CommonService.AggiungiZeriASinistra((string)cod, 10)
                                    , year
                                    , month
                                    , day
                                    , hours
                                    , minutes
                                    , regDirection);

                                txtRegs.Add(txtReg);
                            }


                            //Solo per registrazioni di tipo non GPS (altrimenti scriverebbe le coordinate duplicate)
                            if (latitude != 0 && longitude != 0 && reg.transponderType != Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G))
                            {
                                if (latitude < 0)
                                {
                                    direzioneCardinaleLatitudine = "S";
                                }
                                if (longitude < 0)
                                {
                                    direzioneCardinaleLongitudine = "O";
                                }

                                txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}"
                                    , RegsFileSeparator
                                    , reg.DeviceCode
                                    , CommonService.AggiungiZeriASinistra(latitude.ToString("00.0000000").Remove(2, 1), 10)
                                    , year
                                    , month
                                    , day
                                    , hours
                                    , minutes
                                    , trasponderType == Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G) ? isTagReferenced : isNotTagReferenced //se è solo GPS, non è tagReferenced, altrimenti sì
                                    , latitudeLine
                                    , direzioneCardinaleLatitudine
                                    );

                                txtRegs.Add(txtReg);

                                txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}"
                                    , RegsFileSeparator
                                    , reg.DeviceCode
                                    , CommonService.AggiungiZeriASinistra(longitude.ToString("00.0000000").Remove(2, 1), 10)
                                    , year
                                    , month
                                    , day
                                    , hours
                                    , minutes
                                    , trasponderType == Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G) ? isTagReferenced : isNotTagReferenced //se è solo GPS, non è tagReferenced, altrimenti sì
                                    , longitudeLine
                                    , direzioneCardinaleLongitudine
                                    );

                                txtRegs.Add(txtReg);


                            }

                            // se è richiesto di inserire anche un turno allora lo si inserisce successivo alla timbratura richiesta
                            if (!String.IsNullOrEmpty(turnType))
                            {
                                // costruzione della stringa da processare 
                                txtReg = String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}"
                                    , RegsFileSeparator
                                    , reg.DeviceCode
                                    , CommonService.AggiungiZeriASinistra((string)reg.BadgeCode, 10)
                                    , year
                                    , month
                                    , day
                                    , hours
                                    , minutes
                                    , regDirection
                                    , infoAgg
                                    , turno
                                    , turnType
                                    );

                                // aggiunta della stringa alla lista di scrittura
                                txtRegs.Add(txtReg);
                            }

                            if (!String.IsNullOrEmpty(sottoCantiereDescrizione))
                            {
                                // costruzione della stringa da processare 
                                txtReg = String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}"
                                    , RegsFileSeparator
                                    , reg.DeviceCode
                                    , CommonService.AggiungiZeriASinistra((string)reg.BadgeCode, 10)
                                    , year
                                    , month
                                    , day
                                    , hours
                                    , minutes
                                    , regDirection
                                    , infoAgg
                                    , sottoCantiere
                                    , sottoCantiereDescrizione
                                    );

                                // aggiunta della stringa alla lista di scrittura
                                txtRegs.Add(txtReg);
                            }

                            //se sono presenti dei tipi attività allora li si aggiunge in coda alla timbratura
                            if (splittedActivityType.Length > 0)
                            {
                                foreach (string actType in splittedActivityType)
                                {
                                    // costruzione della stringa da processare
                                    txtReg = String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}"
                                    , RegsFileSeparator
                                    , reg.DeviceCode
                                    , CommonService.AggiungiZeriASinistra((string)reg.BadgeCode, 10)
                                    , year
                                    , month
                                    , day
                                    , hours
                                    , minutes
                                    , regDirection
                                    , infoAgg
                                    , tipoAttivita
                                    , actType
                                    );
                                    // aggiunta della stringa alla lista di scrittura
                                    txtRegs.Add(txtReg);
                                }
                            }

                            // se è stato passato il valore della matricola pru che ha effettuato la timbratura attività allora
                            // la si scrive come informazione aggiuntiva
                            if (!string.IsNullOrEmpty(pruCodeForActivity))
                            {
                                // costruzione della stringa da processare
                                txtReg = String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}"
                                , RegsFileSeparator
                                , reg.DeviceCode
                                , CommonService.AggiungiZeriASinistra((string)reg.BadgeCode, 10)
                                , year
                                , month
                                , day
                                , hours
                                , minutes
                                , regDirection
                                , infoAgg
                                , tipoPruCodeForActivity
                                , pruCodeForActivity
                                );
                                // aggiunta della stringa alla lista di scrittura
                                txtRegs.Add(txtReg);


                            }
                        }
                    }

                    #endregion

                    else

                    #region REGISTRAZIONI SINGOLE

                    {

                        //Se si tratta di una registrazione SOLO GPS (ancora da implementare anche per le squadre) e non è presente il codice del disposititvo associato alla timbratura d'entrata o uscita
                        if (trasponderType == Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G) && pruCodeForActivity == "")
                        {
                            if (latitude < 0)
                            {
                                direzioneCardinaleLatitudine = "S";
                            }
                            if (longitude < 0)
                            {
                                direzioneCardinaleLongitudine = "O";
                            }

                            txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{0}"
                                , RegsFileSeparator
                                , reg.DeviceCode
                                , CommonService.AggiungiZeriASinistra(latitude.ToString("00.0000000").Remove(2, 1), 10)
                                , year
                                , month
                                , day
                                , hours
                                , minutes
                                , trasponderType != Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G) ? isTagReferenced : isNotTagReferenced //se è solo GPS, non è tagReferenced, altrimenti sì
                                , latitudeLine
                                , direzioneCardinaleLatitudine
                                );
                            
                            txtRegs.Add(txtReg);

                            txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{0}"
                                , RegsFileSeparator
                                , reg.DeviceCode
                                , CommonService.AggiungiZeriASinistra(longitude.ToString("00.0000000").Remove(2, 1), 10)
                                , year
                                , month
                                , day
                                , hours
                                , minutes
                                , trasponderType != Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G) ? isTagReferenced : isNotTagReferenced //se è solo GPS, non è tagReferenced, altrimenti sì
                                , longitudeLine
                                , direzioneCardinaleLongitudine
                                );
                            
                            txtRegs.Add(txtReg);
                        }
                        //Registrazione solo NFC o QRCODE
                        else if((trasponderType == Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G) && pruCodeForActivity != "") ||trasponderType == Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.N) || trasponderType == Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.Q))
                        {
                            txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}"
                                , RegsFileSeparator
                                , reg.DeviceCode
                                , CommonService.AggiungiZeriASinistra((string)reg.BadgeCode, 10)
                                , year
                                , month
                                , day
                                , hours
                                , minutes
                                , reg.Direction
                                );

                            txtRegs.Add(txtReg);
                        }

                        //Se la registrazione è NFC+GPS o QRCODE+GPS
                        if (trasponderType == Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.NG) || trasponderType == Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.QG))
                        {
                            txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{9}{0}"
                                , RegsFileSeparator
                                , reg.DeviceCode
                                , CommonService.AggiungiZeriASinistra((string)reg.BadgeCode, 10)
                                , year
                                , month
                                , day
                                , hours
                                , minutes
                                , isTagReferenced
                                , " "
                                );

                            txtRegs.Add(txtReg);
                        }

                        //Solo per registrazioni non solo GPS
                        if (latitude != 0 && longitude != 0 && trasponderType != Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G))
                        {
                            if (latitude < 0)
                            {
                                direzioneCardinaleLatitudine = "S";
                            }
                            if (longitude < 0)
                            {
                                direzioneCardinaleLongitudine = "O";
                            }

                            txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}"
                                , RegsFileSeparator
                                , reg.DeviceCode
                                , CommonService.AggiungiZeriASinistra(latitude.ToString("00.0000000").Remove(2, 1), 10)
                                , year
                                , month
                                , day
                                , hours
                                , minutes
                                , trasponderType != Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G) ? isTagReferenced : isNotTagReferenced //se è solo GPS, non è tagReferenced, altrimenti sì
                                , latitudeLine
                                , direzioneCardinaleLatitudine
                                );


                            txtRegs.Add(txtReg);

                            txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}"
                                , RegsFileSeparator
                                , reg.DeviceCode
                                , CommonService.AggiungiZeriASinistra(longitude.ToString("00.0000000").Remove(2, 1), 10)
                                , year
                                , month
                                , day
                                , hours
                                , minutes
                                , trasponderType != Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G) ? isTagReferenced : isNotTagReferenced //se è solo GPS, non è tagReferenced, altrimenti sì
                                , longitudeLine
                                , direzioneCardinaleLongitudine
                                );

                           
                            

                            txtRegs.Add(txtReg);

                        }

                        // se è richiesto di inserire anche un turno allora lo si inserisce successivo alla timbratura richiesta
                        if (!String.IsNullOrEmpty(turnType))
                        {
                            // costruzione della stringa da processare 
                            txtReg = String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}"
                                , RegsFileSeparator
                                , reg.DeviceCode
                                , CommonService.AggiungiZeriASinistra((string)reg.BadgeCode, 10)
                                , year
                                , month
                                , day
                                , hours
                                , minutes
                                , regDirection
                                , infoAgg
                                , turno
                                , turnType
                                );

                            // aggiunta della stringa alla lista di scrittura
                            txtRegs.Add(txtReg);
                        }
                        if (!String.IsNullOrEmpty(sottoCantiereDescrizione))
                        {
                            // costruzione della stringa da processare 
                            txtReg = String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}"
                                , RegsFileSeparator
                                , reg.DeviceCode
                                , CommonService.AggiungiZeriASinistra((string)reg.BadgeCode, 10)
                                , year
                                , month
                                , day
                                , hours
                                , minutes
                                , regDirection
                                , infoAgg
                                , sottoCantiere
                                , sottoCantiereDescrizione
                                );

                            // aggiunta della stringa alla lista di scrittura
                            txtRegs.Add(txtReg);
                        }

                        //se sono presenti dei tipi attività allora li si aggiunge in coda alla timbratura
                        if (splittedActivityType.Length > 0)
                        {
                            foreach (string actType in splittedActivityType)
                            {
                                // costruzione della stringa da processare
                                txtReg = String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}"
                                , RegsFileSeparator
                                , reg.DeviceCode
                                , CommonService.AggiungiZeriASinistra((string)reg.BadgeCode, 10)
                                , year
                                , month
                                , day
                                , hours
                                , minutes
                                , regDirection
                                , infoAgg
                                , tipoAttivita
                                , actType
                                );
                                // aggiunta della stringa alla lista di scrittura
                                txtRegs.Add(txtReg);
                            }
                        }

                        // se è stato passato il valore della matricola pru che ha effettuato la timbratura attività allora
                        // la si scrive come informazione aggiuntiva 
                        if (!string.IsNullOrEmpty(pruCodeForActivity) && trasponderType != Enum.GetName(typeof(PowerNfcTransponderType), PowerNfcTransponderType.G))
                        {
                            // costruzione della stringa da processare
                            txtReg = String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}"
                            , RegsFileSeparator
                            , reg.DeviceCode
                            , CommonService.AggiungiZeriASinistra((string)reg.BadgeCode, 10)
                            , year
                            , month
                            , day
                            , hours
                            , minutes
                            , regDirection
                            , infoAgg
                            , tipoPruCodeForActivity
                            , pruCodeForActivity
                            );
                            // aggiunta della stringa alla lista di scrittura
                            txtRegs.Add(txtReg);
                        }


                    }

                    #endregion

                }
                // al termine dell'operazione di preparazione della scrittura, si scrive un nuovo file delle timbrature
                string fileName = GetRegsFileName(lastDeviceCode);
                File.WriteAllLines(fileName, txtRegs);
                operationSuccessful = true;
            }
            catch (Exception e)
            {
                operationSuccessful = false;
            }

            // ritorno del valore calcolato dal metodo
            return operationSuccessful;
        }

        /// <summary>
        /// Restituisce il percorso completo di salvataggio del file delle timbrature da riportare.
        /// </summary>
        /// <param name="deviceCode">Il codice della device con cui generare il nome del file.</param>
        /// <returns>Il percorso completo di salvataggio del file delle timbrature da riportare.</returns>
        private string GetRegsFileName(string deviceCode)
        {
            // calcolo della cartella di scarico delle registrazioni
            string filesInputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Input_Path.Replace("~", "").Replace("\\", ""));



            // calcolo del nome file di destinazione 
            string fileName = String.Format("{0}{1}{2}{3}{4}"
                , RegsFileNamePrefix
                , deviceCode
                , DateTime.Now.ToString(String.Format("{0}yyyy{0}MM{0}dd{0}hh{0}mm{0}ss", RegsFileNameDateTimeSeparator))
                , Guid.NewGuid()
                , RegsFileNameExtension);


            return Path.Combine(filesInputPath, fileName);

        }

        #endregion
    }
}