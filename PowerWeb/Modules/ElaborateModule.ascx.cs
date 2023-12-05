using Business;
using Business.ExternalImport;
using Business.HttpHub.HttpHubs;
using Business.Repository;
using Common;
using DevExpress.Web.ASPxCallback;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxUploadControl;
using Domain;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace PowerWeb.Modules
{

    public partial class ElaborateModule : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ElaborateModule));



        /// <summary>
        /// Restituisce true se ci sono dei file da importare o reg sospese nella cartelle configurata
        /// per essere l'appoggio dei reg da importare
        /// </summary>
        public bool HasFilesToImportAndSuspended
        {
            get;
            set;
        }

        /// <summary>
        /// Restituisce true se ci sono  file da importare o reg sospese nella cartelle configurata
        /// per essere l'appoggio dei reg da importare
        /// </summary>
        public bool HasFilesToImport
        {
            get;
            set;
        }

        /// <summary>
        /// Controlla se ci sono timbrature di IOS
        /// </summary>
        protected Business.DataClasses.FlutterAppDTOs.CountReg getTimbratureFlutter()
        {
            string connection = "/app/ws/Synched";
            Business.HttpHub.FlutterAppHttpModule bridge = new FlutterAppHub();
            bridge.Host = "mobile.clockapp.it";
            int index = RepoManager.ParamRepo.ParametersRow.Indice_Timbrature_FlutterApp;
            IEnumerable<Business.DataClasses.FlutterAppDTOs.CountReg> count = bridge.Get<List<Business.DataClasses.FlutterAppDTOs.CountReg>>(CreateStandardPayload(index),connection);
            return count.First();
        }

        private JObject CreateStandardPayload(int index)
        {
            JObject request = JObject.FromObject(new
            {
                IdCliente = index
            });

            return request;
        }

        /// <summary>
        /// Restituisce true se ci sono reg sospese nella cartelle configurata
        /// per essere l'appoggio dei reg sospese
        /// </summary>
        public bool HasSuspended
        {
            get;
            set;
        }

        public List<string> FilesRegDaImportare
        {
            get;
            set;
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            bool scheduleActive = RepoManager.ParamRepo.ParametersRow.Abilita_Schedulatore,
                 notificationActive = RepoManager.ParamRepo.ParametersRow.Abilita_Notifiche,
                 delayActive = RepoManager.ParamRepo.ParametersRow.Abilita_Arrotondamenti,
                 chiamateActive = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowSendChiamateButtons) == (int)ShowSendChiamateButtons.Show;
            BtnDeleteTrips.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_DELETE_VIAGGI);
            BtnDeleteTrips.ClientVisible = RepoManager.ParamRepo.ParametersRow.Abilita_Viaggi;

            // verifico la presenza di file da importare ed aggiorno di conseguenza le proprietà
            CalcolaFilesRegDaImportare();
            // Se ci sono files sospesi, abilita i flag per labels&buttons
            CalcolaFilesSospesi();

            CalcolaMessaggiFormRegServer();

            btnDeleteSuspended.ClientEnabled = HasSuspended;

            //INizializza le Label della Pagina Video in Lingua  
            LocalizeFormElements();

            //Inizializza la Data Inizio Elaborazione con la Data del 1° del Mese Precedente
            deFrom.Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
            //Se l'utente connesso NON ha livello WINIT allora NON Mostro i Campi per l'Import TXT da PC LOcale.
            if (PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
            {
                //Se l'Utente NON è un Utente Winit
                //Nasconde la parte che eprmette l'Import DIRETTO dal PC LOCALE delle Reg da TXT 
                lblFileDaImportare.Visible = false;
                uploader.Visible = false;
                btnUpload.Visible = false;
            }


            /* GESTIONE VISUALIZZAZIONE BOTTONI NOTIFICHE */
            if (!notificationActive || (!delayActive && !chiamateActive))
            {
                flNotificationButtons.Visible = false;
            }
            else
            {
                if (!scheduleActive)
                {
                    buttonChiamateSchedulate.Visible = false;
                    buttonRitardiSchedulati.Visible = false;
                }

                if (!delayActive)
                {
                    boxRitardi.Visible = false;

                }
                else if (!chiamateActive)
                {
                    boxChiamate.Visible = false;
                    boxChiamate.Visible = false;
                }
            }


            // se nei parametri è disabilitato il flag di calcolo viaggi (null o 0)
            // allora si procedella disabilitazione del pulsante
            btnTrips.ClientEnabled = RepoManager.ParamRepo.ParametersRow.Abilita_Viaggi && RepoManager.ParamRepo.ParametersRow.Flag_Ore_Viaggi != null && RepoManager.ParamRepo.ParametersRow.Flag_Ore_Viaggi != (int)FlagTripHoursParamEnum.None;

            // visualizzazione e localizzazione degli elementi dell'export XML
            ShowOrHideXmlExportElements();
            LocalizeXmlExportElements();

            // inizializzazione del combobox di scelta opzioni di lancio
            PowerWebService.FillComboboxes(CmbSelTipoSched);
            CmbSelTipoSched.SelectedIndex = 0;

            // nascondo il form layout della schedulazione in base all'attivazione o meno del modulo schedulazione
            flLaunchTypeSelection.Visible = RepoManager.ParamRepo.ParametersRow.Abilita_Schedulatore;
        }


        protected void upldImport_FileUploadComplete(object sender, FileUploadCompleteEventArgs e)
        {
            // NB: Attenzione! Con l'upload de file dell'utente Winit è importato solamente il file uploadato (no altri file reg, no files di sospese)

            String regSuspendedFile = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path + Common.Properties.Settings.Default.SuspendedRegsFile);
            String regNewFile = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path + String.Format(Common.Properties.Settings.Default.RegsBCKFile, DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss")));
            String[] importReg = { };

            using (FileStream fileStream = File.Create(regNewFile))
            {
                e.UploadedFile.FileContent.CopyTo(fileStream);
                fileStream.Close();
            }

            // inizializzazione dell'array contenente l'elenco globale delle reg da importare lette dal file appena uploadato
            List<string> regNoGpsToImport = BusinessService.GetRegsNoGpsFromFiles(new List<string>() { regNewFile });

            // recupero tutte le timbrature gps presenti nei file da importare lette dal file appena uploadato
            List<KeyValuePair<string, string>> errorsGPS = new List<KeyValuePair<string, string>>();
            List<string> regGpsToImport = BusinessService.GetRegsGpsFromFiles(new List<string>() { regNewFile });


            List<KeyValuePair<String, String>> importErrors = RepoManager.RegRepo.Import(regNoGpsToImport.ToArray(), regGpsToImport.ToArray());

            string message = "Import elaborato con successo";

            if (importErrors.Count > 0)
            {
                message = "Errori durante l'importazione, controlla Tabella Errori";

                using (StreamWriter sw = new StreamWriter(regSuspendedFile, false))
                {
                    foreach (KeyValuePair<String, String> error in importErrors)
                    {
                        sw.WriteLine(error.Key);
                        sw.WriteLine(error.Value);
                    }
                }
            }

            e.ErrorText = message;

            // delay per attesa lettura messaggio ping su browser
            System.Threading.Thread.Sleep(2000);
        }

        /// <summary>
        /// Costruisce il testo e invia la mail riportante i ritardi odierni.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.Web.ASPxCallback.CallbackEventArgs" /> instance containing the event data.</param>
        protected void cSendDelay_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {
            cSendDelay.JSProperties["cpResult"] = RepoManager.Reg_VRepo.InviaRitardi();
        }


        /// <summary>
        /// Invia mail riportante i collaboratori che non hanno avuto timbrature nella giornata di ieri.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.Web.ASPxCallback.CallbackEventArgs" /> instance containing the event data.</param>
        protected void cSendChiamate_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {
            cSendChiamate.JSProperties["cpResult"] = RepoManager.Reg_VRepo.InviaChiamate();
        }

        #region IMPORTAZIONE

        protected void cImportFormServer_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {
                       
            var semaphore = RepoManager.ParamRepo.IsElaborationReady();

            if (!RepoManager.ParamRepo.LockElaboration() || !semaphore)
            {
                e.Result = "Elaborazione già avviata da un'altra instanza!";
                _log.Warn(String.Format("Funzione di import bloccata per l'utente {0}. Import già avviato da un'altra instanza.", PowerWebContext.Current.User.Codice_Utente));
                return;
            }


            //Vengono richieste le timbrature da un server esterno e creato il relativo txt
            try
            {
                ExternalImportManager.GetFromRemoteSource();
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errori durante l'import da server esterno : {0}", ex.Message);
            }


            // Inizializzaizone del file che conterrà le reg sospese
            string regSuspendedFile = Server.MapPath(Path.Combine(Common.Properties.Settings.Default.Files_Input_Path, String.Format("{0}_{1}.txt", Common.Properties.Settings.Default.SuspendedRegsFile, DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"))));

            // per sicurezza è ricalcolato l'elenco dei file da elaborare e la presenza dei file
            CalcolaFilesRegDaImportare();

            List<KeyValuePair<String, String>> importErrors = new List<KeyValuePair<string, string>>();

            try
            {
                IEnumerable<string> regNoGpsToImport = BusinessService.GetRegsNoGpsFromFiles(FilesRegDaImportare);
                IEnumerable<string> regGpsToImport = BusinessService.GetRegsGpsFromFiles(FilesRegDaImportare);

                importErrors = RepoManager.RegRepo.Import(regNoGpsToImport.ToArray(), regGpsToImport.ToArray());
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la fase di import : {0}", ex.Message);
            }
            finally
            {
                RepoManager.ParamRepo.UnLockElaboration();
            }

            // effettuazione del backup di tutti i file della lista
            string backupFolder = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Backup_Path);

            // backup dei files processati
            BusinessService.BackupProcessedFiles(FilesRegDaImportare, backupFolder);

            if(FilesRegDaImportare==null)
            e.Result = "Non ci sono registrazioni da importare";

            // se si sono verificati degli errori
            if (importErrors.Count > 0)
            {
                e.Result = "Import terminato con segnalazioni!";
                // creazione delle reg sospese
                BusinessService.CreateSuspendedRegFile(regSuspendedFile, importErrors);
            }
            else
            {
                e.Result = "Import terminato!";
            }
        }

        #endregion

        /*
        * Sposta i file con registrazioni sospese nella cartella di backup
        */
        protected void cDeleteSuspended_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {
            // Percorso della directory dei file con le registrazioni
            string sourceDir = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path);

            // Percorso della directory di backup
            string targetDir = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Backup_Path);

            // Fil con registrazioni sospese
            string suspendedFile = String.Concat(Common.Properties.Settings.Default.SuspendedRegsFile, "*");

            // preparazione del messaggio di successo
            string message = "Timbrature sospese eliminate con successo!";

            // Carico la directory dei file con le registrazioni 
            DirectoryInfo fileDirectory = new DirectoryInfo(sourceDir);

            // Recupero i file con registrazioni sospese
            FileInfo[] files = fileDirectory.GetFiles(suspendedFile);

            // Crea la directory di backup se non esiste già
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Sposta i file nella directory di backup
            foreach (FileInfo file in files)
            {
                // Se il file non esiste già, lo sposta; altrimenti lo elimina
                if (!File.Exists(targetDir + "\\" + file.Name))
                {
                    file.MoveTo(targetDir + "\\" + file.Name);
                }
                else
                {
                    file.Delete();
                }
            }

            // ritorno del messaggio di termine
            cDeleteSuspended.JSProperties["cpResult"] = message;

            // delay per attesa lettura messaggio ping su browser
            System.Threading.Thread.Sleep(2000);

        }

        protected void btnElaborate_CustomJSProperties(object sender, DevExpress.Web.ASPxClasses.CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage", BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_AGGIORNAMENTO));

            if (!e.Properties.ContainsKey("cpErrorMessage"))
                e.Properties.Add("cpErrorMessage", BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO));
        }

        protected void btnDeleteSuspended_CustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage", BusinessService.GetLocalizedString(PowerWebResources.STR_CONFERMA_DELETE_SOSPESE));
        }

        protected void cAdd2Minutes_Callback(object source, CallbackEventArgs e)
        { }

        #region ELABORAZIONE

        protected void cElaborate_Callback(object source, CallbackEventArgs e)
        {
            var semaphore = RepoManager.ParamRepo.IsElaborationReady();

            if (!semaphore && RepoManager.ParamRepo.LockElaboration())
            {
                e.Result = "Elaborazione già avviata da un'altra instanza!";
                _log.Warn(String.Format("Funzione di elaborazione bloccata per l'utente {0}. Import/elaborazione già avviati da un'altra instanza.", PowerWebContext.Current.User.Codice_Utente));
                return;
            }

            DateTime from = deFrom.Date;
            DateTime to = deTo.Date.AddDays(1);

            BusinessService.ManageNocturneStartEndDate(ref from, ref to);

            //Conteggio giornaliero delle reg attualmente presenti nel db
            var dayCountList = RepoManager.RegRepo.CountRegsFromDateRange(from, to);

            //Calcolo periodi ottimizzati di elaborazione
            var periods = RepoManager.RegRepo.GetPeriods(dayCountList);

            List<Reg> toElaborateRegs = null;
            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();

            #region VARIABILI PROGRESSBAR

            int ciclo = 0;
            float step = (float)(100 * (float)(1 / (float)periods.Count));
            float progress = 0;

            #endregion

            try
            {
                foreach (var period in periods)
                {

                    #region ELABORAZIONE TIMBRATURE PER PERIODO

                    ciclo++;

                    BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(progress, String.Format("Elaborazione: Fase {0} di {1}", ciclo, periods.Count));

                    toElaborateRegs = RepoManager.RegRepo.FindRegsByDataFis(period.Key, period.Value, false).ToList();

                    errors.AddRange(RepoManager.RegRepo.Elaborate(toElaborateRegs, period.Key, period.Value, true, true));

                    progress += step;

                    #endregion

                }

            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Elaborazione registrazioni è terminata a causa di un eccezione : {0}", ex.Message);
            }
            finally
            {
                RepoManager.ParamRepo.UnLockElaboration();
            }

            BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100, "Elaborazione: Completata");

            if (BusinessService.ElaborateStatusDictionary.ContainsKey(PowerWebContext.Current.User))
                BusinessService.ElaborateStatusDictionary.Remove(PowerWebContext.Current.User);

            String message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA);

            if (errors.Count > 0)
                message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA_CON_SEGNALAZIONI);

            e.Result = message;
        }

        #endregion

        protected void cTrips_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {
            if (e.Parameter == "deleteTrips")
            {
                try
                {
                    BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(0, "Elaborazione Viaggi Iniziata");

                    DateTime from = deFrom.Date;
                    if (deTo.Date == DateTime.MinValue)
                        deTo.Date = deFrom.Date;
                    // la data di destinazione è il finale (le 23:59 della data indicata), altrimenti nella ricerca si perde un giorno
                    DateTime to = new DateTime(deTo.Date.Year, deTo.Date.Month, deTo.Date.Day, 23, 59, 0);

                    if (from != DateTime.MinValue && to != DateTime.MinValue)
                    {
                        BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] =
                            new KeyValuePair<double, string>(0, "Cancellazione Viaggi Iniziata");                       

                        List<Reg> tripsToDelete = new List<Reg>();

                        var colList = RepoManager.ColRepo.GetAll().Where(c => c.DisAbilitazione_Col == false);

                        foreach (var col in colList)
                        {
                            //legge le Registrazioni per il Periodo Richiesto tra le quali generare eventualmente i Viaggi per i singoli collaboratori selezionati

                            RepoManager.RegRepo.DeleteFromQuery(reg => reg.Registrazione_Data_Ora_Fis_Reg >= from &&
                                        reg.Registrazione_Data_Ora_Fis_Reg <= to &&
                                        reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip &&
                                        reg.Col_Id == col.Col_Id);
                        }






                        String message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA);

                        e.Result = message;

                        // effettuazione della garbage collection prima di partire con l'elaborazione (occupazione molto alta di memoria)
                        GC.Collect();
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                }
            }
            else
            {

                try
                {
                    BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(0, "Elaborazione Viaggi Iniziata");

                    DateTime from = deFrom.Date;
                    if (deTo.Date == DateTime.MinValue)
                        deTo.Date = deFrom.Date;
                    // la data di destinazione è il finale (le 23:59 della data indicata), altrimenti nella ricerca si perde un giorno
                    DateTime to = new DateTime(deTo.Date.Year, deTo.Date.Month, deTo.Date.Day, 23, 59, 0);

                    if (from != DateTime.MinValue && to != DateTime.MinValue)
                    {
                        // effettuazione della garbage collection prima di partire con l'elaborazione (occupazione molto alta di memoria)
                        GC.Collect();

                        // aggiornamento delle date con i parmetri del notturno
                        BusinessService.ManageNocturneStartEndDate(ref from, ref to);

                        // l'unità minima di elaborazione è un giorno e quindi se le date/ore in elaborazione sono uguali
                        // allora l'ora to viene spostato al giorno successivo (a inizio giornata così da comprendere solo il giorno
                        // in elaborazione)
                        if (from == to)
                        {
                            to = to.AddDays(1);
                            to = to.AddMinutes(1);
                        }


                        //legge le Registrazioni per il Periodo Richiesto tra le quali generare eventualmente i Viaggi
                        var regIds = RepoManager.RegRepo.Find(r => r.Registrazione_Data_Ora_Fis_Reg >= from && r.Registrazione_Data_Ora_Fis_Reg <= to && !r.Registrazione_Bloccata, true).Select(reg => reg.Reg_Id).ToList();


                        //legge la Vista Logica delle Registrazioni per il Periodo Richiesto tra le quali generare eventualmente i Viaggi 
                        IEnumerable<Reg_V> regVs = RepoManager.Reg_VRepo.Find(regv => (regv.Data_Ora_Fis_E >= from && regv.Data_Ora_Fis_U <= to && !regv.Registrazione_Bloccata)
                                                                                      && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att, true).ToList();
                        regVs = regVs.Where(regv => regIds.Contains(regv.RegE)).ToList();

                        List<KeyValuePair<String, String>> errors = new List<KeyValuePair<string, string>>();

                        //Chiama il Calcolo dei Viaggi in RegV_Repository
                        if (regVs.Count() > 0)
                        {
                            BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(0, "Generazione Viaggi Iniziata");

                            errors = RepoManager.Reg_VRepo.ElaborateTrips(regVs, true);
                        }

                        if (BusinessService.ElaborateStatusDictionary.ContainsKey(PowerWebContext.Current.User))
                            BusinessService.ElaborateStatusDictionary.Remove(PowerWebContext.Current.User);

                        String message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA);

                        if (errors.Count > 0)
                        {
                            RepoManager.Tab_MessaggiRepo.InsertMessages(errors, ApplicationMessageEnum.ElaborateTrips, FunctionMessageEnum.ElaborateTrips, PowerWebContext.Current.User.Utenti_Id, DateTime.Now);
                            message = BusinessService.GetLocalizedString(PowerWebResources.STR_ELABORAZIONE_TERMINATA_CON_SEGNALAZIONI);
                        }

                        e.Result = message;

                        // effettuazione della garbage collection prima di partire con l'elaborazione (occupazione molto alta di memoria)
                        GC.Collect();
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                }
            }
        }       

        protected void cPing_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        {
            //  Restituisce quanto caricato nell'ImportDataStatusDictionary dalla Routine di CALCULATE 
            //  StatusKey : Contiene il Nome della Tabella che si sta importando in quel momento                               
            //  Valore    : Contiene la Percentuale (calcolata in base al N° di Tabelle da caricare) di Caricamento rispetto al Totale
            if (BusinessService.ElaborateStatusDictionary.ContainsKey(PowerWebContext.Current.User))
            //Se ci sono dati nel DictionaryStatus allora li carica nel Risultato da mostrare a Video
            {
                var status = BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User];
                e.Result = String.Format("{0}|{1}", status.Key, status.Value);
            }
        }

        protected void cPingImportTxt_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        //  Restituisce quanto caricato nell'ImportDataStatusDictionary dalla Routine di CALCULATE 
        //  StatusKey : Contiene il Nome della Tabella che si sta importando in quel momento                               
        //  Valore    : Contiene la Percentuale (calcolata in base al N° di Tabelle da caricare) di Caricamento rispetto al Totale
        {
            if (BusinessService.ImportDataStatusDictionary.ContainsKey(PowerWebContext.Current.User))
            //Se ci sono dati nel DictionaryStatus allora li carica nel Risultato da mostrare a Video
            {
                var status = BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User];
                e.Result = String.Format("{0}|{1}", status.Key, status.Value);
            }
        }

        protected void importTxtPanel_Callback(object sender, CallbackEventArgsBase e)
        // callback del pannello utilizzato per la visualizzazione dell'import per ricalcolare i dati visuali
        {
            CalcolaMessaggiFormRegServer();
        }

        private void CalcolaFilesRegDaImportare()
        // Calcola la lista e l'eventuale presenza di file all'interno della cartella di files input configurata
        {
            // inizializzazione della cartella contenene le eventuali registrazioni da importare
            string filesInputFolderName = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path);

            // Flag passato come riferimento al metodo successivo per valorizzare la proprietà HasFileToImport
            bool hasFileToImport;
            // iniziaizzazione della lista dei file nella cartella di input
            List<string> filesInputNames = BusinessService.CalcolaFilesRegDaImportare(filesInputFolderName, out hasFileToImport);
            HasFilesToImport = hasFileToImport;

            // aggiorno le proprietà in base a quanto recuperato dalla cartella
            if (filesInputNames.Count > 0)
            {
                HasFilesToImportAndSuspended = true;
                FilesRegDaImportare = filesInputNames;
            }
            else
            {
                HasFilesToImportAndSuspended = false;
                FilesRegDaImportare = null;
            }
        }


        private void CalcolaFilesSospesi()
        // Calcola la lista e l'eventuale presenza di file all'interno della cartella di files input configurata
        {
            // inizializzazione della cartella contenene le eventuali registrazioni da importare
            string filesInputFolderName = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path);

            // iniziaizzazione della lista dei file nella cartella di input
            List<string> filesSuspendedNames = BusinessService.CalcolaFilesRegSospese(filesInputFolderName);

            // aggiorno le proprietà in base a quanto recuperato dalla cartella
            if (filesSuspendedNames.Count > 0)
            {
                HasSuspended = true;
            }
            else
            {
                HasSuspended = false;
            }
        }


        private void CalcolaMessaggiFormRegServer()
        // Calcola gli elementi visuali dei messaggi e dei pulsanti riguardanti le reg da importare
        {
            Business.DataClasses.FlutterAppDTOs.CountReg msg;
            msg = getTimbratureFlutter();
            // impostazione della label che indica la presenza su server di file reg da importare
            LblSeverFileNameToImport.Text = "NON sono presenti dei file su server da importare";

            if (HasSuspended && HasFilesToImport)
            {
                LblSeverFileNameToImport.Text = "Sono presenti file da importare e registrazioni sospese";
            }

            else if (HasSuspended)
            { 
                LblSeverFileNameToImport.Text = "Sono presenti delle registrazioni sospese";
            }

            else if (HasFilesToImport || msg.NumeroReg != "0")
            {
                LblSeverFileNameToImport.Text = "Sono presenti dei file da importare";
            }

            // abilitazione del pulsante per l'elaborazione dei reg da importare su server in base alla presenza dei file
            btnImportFromServer.ClientEnabled = (RepoManager.ParamRepo.ParametersRow.Abilita_Import_Esterno) ? true : HasFilesToImportAndSuspended;
            btnDeleteSuspended.ClientEnabled = HasSuspended;
            btnImportFromServer.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_IMPORT_SERVER_TXT);
        }

        protected void BtnDeleteTrips_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpMessage"))
                e.Properties.Add("cpMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_AGGIORNAMENTO));

            if (!e.Properties.ContainsKey("cpErrorMessage"))
                e.Properties.Add("cpErrorMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO));


            if (!e.Properties.ContainsKey("cpErrorMessageCol"))
                e.Properties.Add("cpErrorMessageCol",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE_NON_SEL));
        }



        /// <summary>
        /// Imposta gli elementi non presonalizzati della form in lingua.
        /// </summary>
        private void LocalizeFormElements()
        {
            lblFileDaImportare.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_IMPORT_TXT);
            btnUpload.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_IMPORT_LOCALE_TXT);
            lblDal.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_PERIODO_DAL);
            lblAl.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_AL);
            btnTrips.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_ELAB_VIAGGI);
            btnElaborate.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_ELAB_REG);
            btnDeleteSuspended.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_DELETE_SUSPENDED);
            lblResult.Text = ".";
            LblTipoSchedulazione.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_TIPO_SCHEDULAZIONE);
        }

        #region Gestione Lancio Export Xml

        /// <summary>
        /// Handles the OnCustomJSProperties event of the btnLaunchXmlExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CustomJSPropertiesEventArgs"/> instance containing the event data.</param>
        protected void btnLaunchXmlExport_OnCustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        {
            if (!e.Properties.ContainsKey("cpErrorMessage"))
                e.Properties.Add("cpErrorMessage",
                    BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO));
        }

        /// <summary>
        /// Handles the OnClick event of the btnLaunchXmlExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnLaunchXmlExport_OnClick(object sender, EventArgs e)
        {
            // calcolo il valore della personalizzazione relativa all'export xml delle registrazioni
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.RegExportToXmlEnum);

            // se la personalizzazione è attiva allora si procede con l'elaborazione
            if (customizationVersion != (int)RegExportToXmlEnum.None)
            {
                // recupero del periodo di ricerca
                DateTime from = deFrom.Date;
                DateTime to = new DateTime(deTo.Date.Year, deTo.Date.Month, deTo.Date.Day, 23, 59, 0);

                // se è stato correttamente valorizzato il periodo di elaborazione
                if (deFrom.Date != DateTime.MinValue && deTo.Date != DateTime.MinValue && deTo.Date >= deFrom.Date)
                {
                    // calcolo delle reg_v nel periodo richiesto
                    IQueryable<Reg_V> regVsToProcess = RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Data_Reg >= from && regv.Data_Reg <= to & regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass
                        && (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Att) && regv.Col_Id != 0);

                    // se sono presenti delle reg_v da processare
                    if (regVsToProcess.Any())
                    {
                        // calcolo della cartella di salvataggio dei file temporanei
                        string outputPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), Common.Properties.Settings.Default.Files_Output_Path);

                        // inizializzazione del percorso di restituzione del processato
                        string outputFilePath = String.Empty;

                        // si procede all'elaborazione e alla produzione del file in base al tipo di esportazione configurata (fancedosi restituire la sua posizione di salvataggio)
                        switch ((RegExportToXmlEnum)customizationVersion)
                        {
                            case RegExportToXmlEnum.Perfetto:
                                //outputFilePath = RepoManager.Reg_VRepo.PrepareXmlExportToPerfetto(regVsToProcess, outputPath); 
                                outputFilePath = RepoManager.Reg_VRepo.PrepareXmlExportToScs(regVsToProcess, outputPath);
                                break;
                        }

                        // se non ci sono stati errori nel processo
                        if (!String.IsNullOrEmpty(outputFilePath))
                        {

                            #region Esportazione dello zip alla risposta

                            using (FileStream fs = new FileStream(outputFilePath, FileMode.Open, FileAccess.Read))
                            {
                                try
                                {
                                    var response = HttpContext.Current.Response;
                                    response.Clear();

                                    ComapanyNameEnum comapny = (ComapanyNameEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ComapanyNameEnum);


                                    response.ContentType = "application/zip";
                                    response.AddHeader("Accept-Header", fs.Length.ToString(CultureInfo.InvariantCulture));
                                    if (comapny == ComapanyNameEnum.Sogedi)
                                        response.AddHeader("Content-Disposition", String.Format("{0}; filename={1}-{2}", "Attachment", "Sogedi", Path.GetFileName(outputFilePath)));
                                    else if (comapny == ComapanyNameEnum.Tiseco)
                                        response.AddHeader("Content-Disposition", String.Format("{0}; filename={1}-{2}", "Attachment", "Tiseco", Path.GetFileName(outputFilePath)));
                                    response.Cache.SetCacheability(HttpCacheability.NoCache);
                                    response.AddHeader("Content-Length", fs.Length.ToString(CultureInfo.InvariantCulture));
                                    var fsBytes = new byte[fs.Length];
                                    fs.Read(fsBytes, 0, fsBytes.Length);
                                    response.BinaryWrite(fsBytes);
                                    response.Flush();
                                    response.End();
                                }
                                catch (Exception)
                                {
                                }
                                finally
                                {

                                    fs.Close();
                                    fs.Dispose();
                                    // al termine delle operazioni viene cancellata la cartella temporanea
                                    foreach (var file in Directory.GetFiles(Path.GetDirectoryName(outputFilePath)))
                                        File.Delete(file);

                                    Directory.Delete(Path.GetDirectoryName(outputFilePath));

                                    // si segnala la necessità della chiusura del pannello di caricamento
                                    BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = true;
                                }
                            }



                            #endregion

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Imposta in lingua gli elementi che permettono l'export XML delle registrazioni.
        /// </summary>
        private void LocalizeXmlExportElements()
        {
            // si localizza il pulsante solamente se visibile
            if (btnLaunchXmlExport.Visible)
                btnLaunchXmlExport.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_ESPORTAZIONE_XML_REG);
        }

        /// <summary>
        /// Visualizza o nasconde in base alle customizzazioni gli elementi di gestione dell'export xml.
        /// </summary>
        private void ShowOrHideXmlExportElements()
        {
            // calcolo il valore della personalizzazione relativa all'export xml delle registrazioni
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.RegExportToXmlEnum);

            // se la personalizzazione non è attiva allora si nascondono gli elementi dell'esportazione xml
            if (customizationVersion == (int)RegExportToXmlEnum.None)
            {
                // nascondimento dell'elemento di esportazione xml
                btnLaunchXmlExport.Visible = false;
            }
            else // se la personalizzazione è invece attiva allora si visualizzano gli elementi dell'esportazione xml
            {
                btnLaunchXmlExport.Visible = true;
            }
        }


        #endregion

    }
    
} 