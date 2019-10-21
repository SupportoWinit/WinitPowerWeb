using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Business.LicenceServiceReference;
using Common;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxEditors;
using Domain;
using Business.Repository;
using Business;
using System.Web;
using System.Web.UI.WebControls;
using System.Net;
using System.Threading;
using System.Globalization;

namespace PowerWeb
{
    public partial class SiteMaster : System.Web.UI.MasterPage
    {
        public WebClient ClientRemoteControl = new WebClient();

        protected void Page_Init(object sender, EventArgs e)
        {
            ASPxSiteMapDataSource1.SiteMapProvider = SiteMap.Provider.Name;
            TitleLabel.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_TITOLO);
            SubTitleLabel.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_TITOLO_SUB);
            DeployVersionLabel.Text = String.Format("V: {0}", GetBuildVersion());
            if (PowerWebContext.Current.User != null)
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(PowerWebContext.Current.User.Lingue.Sigla_Lingue);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(PowerWebContext.Current.User.Lingue.Sigla_Lingue);
            }

            LocalizeSchedulePanel();
                       

        }
        
        protected void Page_Load(object sender, EventArgs e)
        {
            NavigationMenu.EnableAnimation = true;
            NavigationMenu.AutoSeparators = DevExpress.Web.ASPxMenu.AutoSeparatorMode.All;
            NavigationMenu.HorizontalAlign = HorizontalAlign.Center;
            NavigationMenu.VerticalAlign = VerticalAlign.Middle;

        }

        protected void OnLoggingOut(object sender, EventArgs e)
        {
            PowerWebService.SetResponseNoCache(Response);
            PowerWebService.LogOut();
            Response.Redirect(PowerWebService.LoginPageURL);
        }

        public bool IsUserLogged()
        {
            return PowerWebContext.Current.User != null;
        }


        /// <summary>
        /// Recupera la versione di build dell'attuale applicativo (file DeployVersion.txt) e la restituisce.
        /// </summary>
        /// <returns>La versione di build dell'attuale applicativo</returns>
        private string GetBuildVersion()
        {
            string deployVersion = String.Empty;

            try
            {
                string deployFilePath = Server.MapPath(Common.Properties.Settings.Default.Deploy_Version_File_Path);

                deployVersion = File.ReadAllText(deployFilePath);
            }
            catch (Exception)
            {
                // silenziamento eventuale errore di lettura
            }

            return deployVersion;
        }

        protected void RemoteControl_Click(object sender, EventArgs e)
        {

            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Winit\Remote_Control\Remote_Control.exe"))
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Winit\Remote_Control\Remote_Control.exe";
                p.StartInfo.CreateNoWindow = false;
                p.Start();
            }
            else
            {
                if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Winit\Remote_Control\"))
                {
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Winit\Remote_Control\");
                }
                ClientRemoteControl.DownloadFile("http://www.win-it.it/public/download/Update/Assistenza%20Remota/Assistenza%20Remota.exe", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Winit\Remote_Control\Remote_Control.exe");
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Winit\Remote_Control\Remote_Control.exe";
                p.StartInfo.CreateNoWindow = false;
                p.Start();
            }

        }

        #region Gestione maschera di richiesta schedulazione

        /// <summary>
        /// Eseguito al callback del pannello di invio della schedulazione al database.
        /// </summary>
        /// <param name="sender">Il mittente del metodo da evento.</param>
        /// <param name="e">Gli argomenti dell'evento.</param>
        protected void CpScheduleJobLayout_OnCallback(object sender, CallbackEventArgsBase e)
        {
            // inizializzazione dell'elemento che segnala la corretta riuscita dell'operazione
            bool insertResult = false;

            // costruzione dei dati da passare al servizio di scrittura schedulazione nel database
            // a. Recupero la public key del cliente
            string customerPublicKey = RepoManager.ParamRepo.ParametersRow.PublicKey;

            // b. Il tipo di operazione da eseguire
            string jobType = e.Parameter.Split('#').FirstOrDefault();

            // c. il tipo di schedulazione da effettuare
            string scheduleType = e.Parameter.Split('#').LastOrDefault();

            // i parametri di schedulazione, le esclusioni e i parametri di job sono valorizzati
            // in base al tipo di schedulazione
            string schedulerParameters = String.Empty;
            string schedulerExclusions = String.Empty;
            string schedulerJobParameters = String.Empty;
            switch (scheduleType)
            {
                case "MoreThanOnceADayScheduleJob":

                    // d. i parametri di schedulazione
                    schedulerParameters = String.Format("MinutesInterval={0};FromHour={1};ToHour={2}", FldMinutesInterval.Text, FldFromHour.DateTime.TimeOfDay, FldToHour.DateTime.TimeOfDay);

                    // e. i parametri di esclusione
                    schedulerExclusions = GetDaysParam(false);

                    break;
                case "DailyScheduleJob":

                    // d. i parametri di schedulazione
                    schedulerParameters = String.Format("DaysInterval={0};ExecutionTime={1}", FldDaysInterval.Text, FldAtHour.DateTime.TimeOfDay);

                    // e. i parametri di esclusione
                    schedulerExclusions = GetDaysParam(false);

                    break;
                case "WeeklyScheduleJob":

                    // d. i parametri di schedulazione
                    schedulerParameters = String.Format("{0};ExecutionTime={1}", GetDaysParam(true), FldAtHour.DateTime.TimeOfDay);

                    break;
                case "MonthlyScheduleJob":

                    // d. i parametri di schedulazione
                    var selectedDays = new StringBuilder();
                    foreach (object selectedItem in ChkBoxsMonthDays.SelectedItems)
                    {
                        if (selectedDays.ToString() != string.Empty)
                            selectedDays.Append(',');

                        selectedDays.AppendFormat(((ListEditItem)selectedItem).Value.ToString());
                    }
                    schedulerParameters = string.Format("DaysToExecute={0};ExecutionTime={1}", selectedDays, FldAtHour.DateTime.TimeOfDay);

                    break;
                default:
                    insertResult = false;
                    break;
            }

            // f. i parametri del job
            if (FldPeriodType.Text != String.Empty) // se è stato impostato un periodo
            {
                string selectedPeriodValue = FldPeriodType.SelectedItem.Value.ToString();
                var periodType = new StringBuilder("PeriodType=");
                periodType.AppendFormat(selectedPeriodValue);
                if (selectedPeriodValue == "StaticPeriod") // se si tratta di un periodo statico aggiungo anche from e to
                    periodType.AppendFormat(";From={0};To={1}", FldStaticPeriodFrom.Date.ToString("d"), FldStaticPeriodTo.Date.ToString("d"));

                schedulerJobParameters = periodType.ToString();
            }

            // g. data di inizio schedulazione
            DateTime startSchedulerDate = FldStartRepetitionDate.Date;

            // h. data di fine schedulazione
            DateTime endSchedulerDate = FldEndRepetitionDate.Date;

            // i. user id
            int userId = PowerWebContext.Current.User == null ? 0 : PowerWebContext.Current.User.Utenti_Id;

            // j. user level
            int userLevel = PowerWebContext.Current.User == null ? 0 : PowerWebContext.Current.UserLevel.Funz_Aut;

            try
            {
                // inizializzazione del servizio per l'invio ella schedulazione
                var sc = new LicenceServiceClient();

                // lancio dell'inserimento della schedulazione
                insertResult = sc.AddNewScheduledOperation(customerPublicKey, scheduleType, jobType, schedulerParameters, schedulerExclusions, schedulerJobParameters, startSchedulerDate
                    , endSchedulerDate, userId, userLevel);
            }
            catch (Exception)
            {
                insertResult = false;
            }

            // attacco la js property del risultato operazione per la lettura
            if (!cpScheduleJobLayout.JSProperties.ContainsKey("cpInserSchResult"))
                cpScheduleJobLayout.JSProperties.Add("cpInserSchResult", false);
            cpScheduleJobLayout.JSProperties["cpInserSchResult"] = insertResult;

        }

        /// <summary>
        /// Recupera dai dati impostati per le esclusioni giornaliere i giorni da esclusione (schedulazione più di una volta al giorno).
        /// </summary>
        /// <returns>La stringa con i parametri dei giorni di eclusione</returns>
        private string GetDaysParam(bool inclusion)
        {
            // inizializzazione de valore di ritorno del metodo
            const string baseDayExclusionString = "DaysExclusion=";
            const string baseDayInclusionString = "DaysToExecute=";
            var daysExclusion = new StringBuilder(inclusion ? baseDayInclusionString : baseDayExclusionString);

            // creo la lista dei check box da processare per verificare la presenza dell'esclusione
            var exlusionCheckboxes = new List<ASPxCheckBox>() { ChkBoxMonday, ChkBoxTuesday, ChkBoxWednesday, ChkBoxThursday, ChkBoxFriday, ChkBoxSaturday, ChkBoxSunday };

            // per ogni checkbox da processare
            foreach (ASPxCheckBox exclusionCheckbox in exlusionCheckboxes)
            {
                // se il checkbox risulta checcato allora imposto il valore di esclusione
                if (exclusionCheckbox.Checked)
                {
                    if (daysExclusion.ToString().Last() != '=')
                        daysExclusion.Append(',');

                    daysExclusion.AppendFormat(exclusionCheckbox.Value.ToString());
                }
            }

            // ritorno del valore calcolato dal metodo (vuoto se nessuna esclusione)
            return daysExclusion.ToString() == baseDayExclusionString ? String.Empty : daysExclusion.ToString();
        }

        /// <summary>
        /// Imposta in ligua gli elementi del pannello di gestione della schedulazione.
        /// </summary>
        private void LocalizeSchedulePanel()
        {
            MoreThanOnceADayScheduleJob.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_PIU_DI_UNA_VOLTA_GIORNO);
            DailyScheduleJob.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_GIORNALMENTE);
            WeeklyScheduleJob.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_SETTIMANALMENTE);
            MonthlyScheduleJob.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_MENSILMENTE);
            LblMinutesInterval.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_OGNI_QUANTI_MINUTI);
            LblDaysInterval.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_OGNI_QUANTI_GIORNI);
            LblFromHour.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_DALLE_ORE);
            LblAtHour.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_ALLE_ORE);
            LblToHour.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_ALLE_ORE);
            LblDaysInclusions.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_GIORNI_DI_ESECUZIONE);
            LblDaysExclusions.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_GIORNI_ESCLUSI);
            ChkBoxMonday.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_LUNEDI);
            ChkBoxTuesday.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_MARTEDI);
            ChkBoxWednesday.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_MERCOLEDI);
            ChkBoxThursday.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_GIOVEDI);
            ChkBoxFriday.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_VENERDI);
            ChkBoxSaturday.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_SABATO);
            ChkBoxSunday.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_DOMENICA);

            foreach (object item in FldPeriodType.Items)
            {
                var editItem = (ListEditItem)item;
                switch (editItem.ValueString)
                {
                    case "StaticPeriod":
                        editItem.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_STATIC_PERIOD);
                        break;
                    case "Today":
                        editItem.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_TODAY_PERIOD);
                        break;
                    case "Yesterday":
                        editItem.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_YESTERDAY_PERIOD);
                        break;
                    case "CurrentWeek":
                        editItem.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_CURRENT_WEEK_PERIOD);
                        break;
                    case "LastWeek":
                        editItem.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_LAST_WEEK_PERIOD);
                        break;
                    case "CurrentMonth":
                        editItem.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_CURRENT_MONTH_PERIOD);
                        break;
                    case "LastMonth":
                        editItem.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_LAST_MONTH_PERIOD);
                        break;
                    case "CurrentAndLastMonths":
                        editItem.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_CURRENT_AND_LAST_MONTH_PERIOD);
                        break;
                    case "CurrentYear":
                        editItem.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_CURRENT_YEAR);
                        break;
                    case "LastYear":
                        editItem.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_LAST_YEAR);
                        break;
                }
            }

            LblStaticPeriodFrom.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_DA);
            LblStaticPeriodTo.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_AL);
            LblStartRepetition.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_RIPETI_DA);
            LblEndRepetition.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_FINO_AL);
        }

        /// <summary>
        /// Handles the OnInit event of the btnScheduleJob control.
        /// Utilizzato per mettere in lingua il testo del pulsante
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void BtnScheduleJob_OnInit(object sender, EventArgs e)
        {
            var currentButton = (ASPxButton)sender;

            currentButton.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_SCHEDULA_ATTIVITA);
        }

        #endregion


    }
}