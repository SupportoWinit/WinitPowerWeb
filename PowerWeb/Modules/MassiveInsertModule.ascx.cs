using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Dynamic;
using System.Linq;
using System.Web;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxEditors.Internal;
using DevExpress.Web.ASPxFormLayout;
using DevExpress.Web.ASPxGridView;
using DevExpress.XtraPrinting.Native;
using Domain.Extensions;
using Exports.ExportExcelSpecialized;
using log4net;
using Business.Repository;
using DevExpress.Web.ASPxUploadControl;
using System.IO;
using Common;
using DevExpress.Web.ASPxScheduler;
using DevExpress.XtraScheduler;
using Domain;
using Business.ExportExcelEngine;
using System.Drawing;
using Reports;
using System.Collections.Specialized;
using DevExpress.Web.ASPxMenu;
using DevExpress.Web.Data;
using System.Collections;
using DevExpress.Data.Filtering;
using Business;
using DevExpress.Data.Linq;
using DevExpress.Web.ASPxEditors;
using System.Web.UI;
using System.Text;

namespace PowerWeb.Modules
{
    public partial class MassiveInsertModule : BaseGridModule
    {

        #region Private Constants and Read Only Fields

        const String KEYFIELDNAME = "RegE";

        private readonly Type _entityType = typeof(Reg_V);

        const Reg_V _regVStub = null;

        #endregion

        #region Private Fields

        private bool _isToHideLoadingPanel;

        #endregion

        #region Private Properties

        private int DaysToAdd
        {
            get
            {
                return Convert.ToInt32(seNumberOfDate.Value);

            }
        }

        public List<Reg_V> RegVs
        {
            get
            {
                var regVs = PowerWebContext.GetFromSession<List<Reg_V>>("EditRegVs_" + gvMassiveInsertEdit.ID);
                if (regVs == null)
                {
                    regVs = new List<Reg_V>();
                    PowerWebContext.SetToSession("EditRegVs_" + gvMassiveInsertEdit.ID, regVs);
                }
                return regVs;

            }

            set
            {
                List<Reg_V> list = value;
                if (list != null)
                    PowerWebContext.SetToSession("EditRegVs_" + gvMassiveInsertEdit.ID, list);
            }
        }

        public String EditErrorMessage
        {
            get
            {
                return PowerWebContext.GetFromSession<String>("EditErrorMessage" + gvMassiveInsertEdit.ID);
            }

            set
            {
                PowerWebContext.SetToSession("EditErrorMessage" + gvMassiveInsertEdit.ID, value);
            }
        }

        public Boolean IsToShowEditGrid
        {
            get
            {
                return PowerWebContext.GetFromSession<Boolean>("IsToShowEditGrid" + gvMassiveInsertEdit.ID);
            }

            set
            {
                PowerWebContext.SetToSession("IsToShowEditGrid" + gvMassiveInsertEdit.ID, value);
            }
        }

        #endregion

        #region Overrided Methods

        public override void ResetSession()
        {
            base.ResetSession();
            RegVs = new List<Reg_V>();
        }

        public override Type EntityType
        {
            get
            {
                return _entityType;
            }
        }

        #endregion

        #region Page Events

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack && !Page.IsCallback)
            {
                ResetSession();
                Bind_gvMassiveInsertEdit();
                lblNumberOfDay.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_NUMERO_GIORNI_INS_MASSIVO);
            }

            gvMassiveInsertEdit.SettingsEditing.Mode = GridViewEditingMode.Batch;

        }

        protected void Page_Init(object sender, EventArgs e)
        {
            // in caso di prima apertura
            // verifico che, non ereditando dalla grid master page, ci sia l'utente loggato
            // e caso mai ritorno un'eccezione; questo controllo serve ad evitara accessi indesiderati senza login
            if (!Page.IsPostBack && !Page.IsCallback)
            {
                if (PowerWebContext.Current.User == null)
                    throw new AccessViolationException("Function not available");
            }

            PowerWebService.FillGridLabels(EntityType, gvMassiveInsertEdit);
            PowerWebService.FillComboboxes(gvMassiveInsertEdit);

            #region Inizializzazione combo inserimento da tab orari

            PowerWebService.FillComboboxes(CmbSelColId, CommonService.GetPropertyName(() => _regVStub.Col_Id));
            if (!CmbSelColId.ReadOnly)
            {
                EditButton btnEdit = new EditButton("X");
                CmbSelColId.Buttons.Add(btnEdit);

                CmbSelColId.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
            }
            PowerWebService.FillComboboxes(CmbSelCantId, CommonService.GetPropertyName(() => _regVStub.Cant_Id));
            if (!CmbSelCantId.ReadOnly)
            {
                EditButton btnEdit = new EditButton("X");
                CmbSelCantId.Buttons.Add(btnEdit);

                CmbSelCantId.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
            }
            PowerWebService.FillComboboxes(CmbSelMotivazione, CommonService.GetPropertyName(() => _regVStub.Motivazione_Reg_Id));
            if (!CmbSelMotivazione.ReadOnly)
            {
                EditButton btnEdit = new EditButton("X");
                CmbSelMotivazione.Buttons.Add(btnEdit);

                CmbSelMotivazione.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
            }

            #endregion

            #region  Etichette inserimento da tab orari in lingua

            LblSelColId.Text = BusinessService.GetLocalizedString(PowerWebResources.FLD_COL_ID);
            LblDateStart.Text = BusinessService.GetLocalizedString(PowerWebResources.FLD_DATA_INIZIO);
            LblDateEnd.Text = BusinessService.GetLocalizedString(PowerWebResources.FLD_DATA_FINE);
            LblSelCantId.Text = BusinessService.GetLocalizedString(PowerWebResources.FLD_CANT_ID);
            LblSelMotivazione.Text = BusinessService.GetLocalizedString(PowerWebResources.FLD_MOTIVAZIONE_REG);

            #endregion

            // viene visualizzato o nascosto il form layout relativo all'inserimento da tabella orari se il modulo di gestione della tabella orari
            // è false
            var layoutGroup = flStandardInsert.Items[1] as LayoutGroup;
            if (layoutGroup != null) layoutGroup.Visible = RepoManager.ParamRepo.ParametersRow.Abilita_Orari;


            Bind_gvMassiveInsertEdit();
        }

        #endregion

        #region Private Methods

        private Reg_V CloneRegV(Reg_V reg, int increaseDay = 0, DateTime? lastValidDate = null, int lastIds = 1)
        {
            var newDate = DateTime.UtcNow;

            if (reg != null)
            {
                newDate = reg.Data_Reg.Value.Date; //imposto la new date a mezzanotte


                if (lastValidDate.HasValue && lastValidDate != DateTime.MinValue)
                    newDate = lastValidDate.Value;

                newDate = newDate.AddDays(increaseDay);
            }
            newDate = RepoManager.Tab_FestiviRepo.NextValidDay(newDate);

            return ActualCloneRegV(reg, newDate, lastIds);
        }

        private Reg_V ActualCloneRegV(Reg_V reg, DateTime newDate, int lastId = 1)
        {
            IsToShowEditGrid = true;

            Reg_V newRegV = RepoManager.Reg_VRepo.Init();
            newRegV.RegE = lastId + 1;
            newRegV.Data_Reg = newDate;

            if (reg != null)
            {
                //Inizializza i Campi della Riga di ADD con i Dati della Registrazione Scelta (caso di M su Riga + ADD)

                if (reg.Data_Ora_Fis_E != DateTime.MinValue && DaysToAdd > 0)
                    newRegV.Data_Ora_Fis_E = GenerateDateTime(reg.Data_Ora_Fis_E, newRegV.Data_Reg.Value);

                if (reg.Data_Ora_Fis_U.HasValue && DaysToAdd > 0)
                    newRegV.Data_Ora_Fis_U = GenerateDateTime(reg.Data_Ora_Fis_U.Value, newRegV.Data_Reg.Value);

                if (reg.Col_Id != null)
                    newRegV.Col_Id = reg.Col_Id;

                if (reg.Cant_Id != null)
                    newRegV.Cant_Id = reg.Cant_Id;

                if (reg.Motivazione_Reg_Id != null)
                    newRegV.Motivazione_Reg_Id = reg.Motivazione_Reg_Id;

                newRegV.IsOnlyDuration = reg.IsOnlyDuration;
                newRegV.Durata_Fis_HH_C = reg.Durata_Fis_HH_C;

            }

            newRegV.Registrazione_Stato_Reg = newRegV.IsOnlyDuration ? (int)RegStateEnum.Ass : (int)RegStateEnum.None;
            newRegV.Registrazione_Tipo_Reg = newRegV.IsOnlyDuration ? (int)RegTypeEnum.Duration : (int)RegTypeEnum.None;

            return newRegV;

        }

        private DateTime GenerateDateTime(DateTime oldDate, DateTime newDate)
        {
            return new DateTime(newDate.Year, newDate.Month, newDate.Day, oldDate.Hour, oldDate.Minute, 0);
        }

        /// <summary>
        /// Imposta la data dei campi ora della reg_v passata come parametro alla data/ora.
        /// </summary>
        /// <param name="regVToProcess">La reg_v da processare.</param>
        private void ManageRegVTime(Reg_V regVToProcess)
        {
            DateTime regVDate = Convert.ToDateTime(regVToProcess.Data_Reg);

            regVToProcess.Data_Ora_Fis_E = new DateTime(regVDate.Year, regVDate.Month, regVDate.Day, regVToProcess.Data_Ora_Fis_E.Hour, regVToProcess.Data_Ora_Fis_E.Minute, regVToProcess.Data_Ora_Fis_E.Second);
            if (regVToProcess.Data_Ora_Fis_U != null)
                regVToProcess.Data_Ora_Fis_U = new DateTime(regVDate.Year, regVDate.Month, regVDate.Day, regVToProcess.Data_Ora_Fis_U.Value.Hour, regVToProcess.Data_Ora_Fis_U.Value.Minute, regVToProcess.Data_Ora_Fis_U.Value.Second);

            // se la registrzione è di sola durata allora traslo l'imput di durata sul campo standard della check
            if (regVToProcess.IsOnlyDuration)
                regVToProcess.Durata_Fis_HH_S = CommonService.GetHHMMStringFormMinutes(Convert.ToInt32(regVToProcess.Durata_Fis_HH_C.TimeOfDay.TotalMinutes));
            else // altrimenti svuoto il valore di durata
                regVToProcess.Durata_Fis_HH_S = null;
        }

        private void Bind_gvMassiveInsertEdit(Boolean isToBindData = true)
        {
            gvMassiveInsertEdit.KeyFieldName = KEYFIELDNAME;
            gvMassiveInsertEdit.DataSource = RegVs;

            if (isToBindData)
                gvMassiveInsertEdit.DataBind();
        }

        private void SetupCalendarOwner(ASPxDateEdit editor)
        {
            if (editor == null) return;
            editor.PopupCalendarOwnerID = "__ReferenceDateEdit";
        }

        #region Manage Errors

        private void SetErrorMessageDictionary(int key, Dictionary<int, IList<Dictionary<string, string>>> errorByColId, Dictionary<string, string> validationErrors)
        {
            if (errorByColId.ContainsKey(key))
            {
                var currentValidationErrorsList = errorByColId[key];
                currentValidationErrorsList.Add(validationErrors);
                errorByColId[key] = currentValidationErrorsList;
            }
            else
            {
                IList<Dictionary<string, string>> currentValidationErrorsList = new List<Dictionary<string, string>>();
                currentValidationErrorsList.Add(validationErrors);
                errorByColId.Add(key, currentValidationErrorsList);
            }
        }

        private string GetErrorMessageFromDictionary(Dictionary<int, IList<Dictionary<string, string>>> errorsByColId)
        {
            var sb = new StringBuilder();

            foreach (KeyValuePair<int, IList<Dictionary<string, string>>> errorByColId in errorsByColId)
            {
                sb.AppendFormat("{0} {1}", BusinessService.GetLocalizedString(PowerWebResources.ERR_MESSAGE_ROW), errorByColId.Key).AppendLine();
                foreach (Dictionary<string, string> validationError in errorByColId.Value)
                    sb.Append(CommonService.GetErrorMessageFromDictionary(validationError)).AppendLine();
            }
            return sb.ToString();
        }

        #endregion

        #endregion

        #region Eventi Griglia

        protected void gvMassiveInsertEdit_CustomCallback(object sender, ASPxGridViewCustomCallbackEventArgs e)
        {
            _isToHideLoadingPanel = false;
            var lastRegV = RegVs.Any() ? RegVs.Last() : null;
            var lastValidDate = DateTime.MinValue;
            int lastId = 1;
            if (lastRegV != null)
                lastId = lastRegV.RegE;

            // Aggiungi singola riga alla griglia
            if (e.Parameters == "addSingle")
            {

                // prima dell'elaborazione viene effettuato un bind della griglia di modo da ricevere i dati aggiornati
                Bind_gvMassiveInsertEdit();

                if (DaysToAdd == 0)
                {
                    RegVs.Add(CloneRegV(lastRegV, 0, null, ++lastId));
                }
                else
                {
                    for (int dayToAdd = 1; dayToAdd <= DaysToAdd; dayToAdd++)
                    {
                        RegVs.Add(CloneRegV(lastRegV, 1, lastValidDate, ++lastId));
                        lastValidDate = RegVs.Last().Data_Reg.Value.Date;
                    }
                }
            }

            // Aggiungi più righe alla griglia
            else if (e.Parameters == "addMulti" && RegVs.Any())
            {

                // prima dell'elaborazione viene effettuato un bind della griglia di modo da ricevere i dati aggiornati
                Bind_gvMassiveInsertEdit();

                var lastDateRegV = RegVs.Last().Data_Reg.Value;
                var regVsToClone = RegVs.Where(item => item.Data_Reg.Value.Date == lastDateRegV.Date).ToList();

                if (DaysToAdd == 0)
                {
                    var regVsToAdd = new List<Reg_V>();

                    for (int i = 0; i < regVsToClone.Count(); i++)
                        regVsToAdd.Add(CloneRegV(regVsToClone.ElementAt(i), 0, null, ++lastId));

                    RegVs.AddRange(regVsToAdd);
                }
                else
                {
                    var regVsToAdd = new List<Reg_V>();

                    for (int dayToAdd = 1; dayToAdd <= DaysToAdd; dayToAdd++)
                    {
                        for (int i = 0; i < regVsToClone.Count(); i++)
                            regVsToAdd.Add(CloneRegV(regVsToClone.ElementAt(i), 1, lastValidDate, ++lastId));

                        if (regVsToAdd.Count > 0)
                            lastValidDate = regVsToAdd.Last().Data_Reg.Value.Date;
                    }

                    RegVs.AddRange(regVsToAdd);
                }
            }
            else switch (e.Parameters)
                {
                    // Inserimento da tabella orari
                    case "insertFromTimeSheet":

                        #region Convalida input dei dati utilizzati nell'inserimento

                        bool convalidaInputOK = true;

                        // se anche solo una date non risulta valorizzata o i periodi non sono corretti allora
                        // si visualizza un messaggio d'errore
                        if (DeDateStart.Text == String.Empty || DeDateEnd.Text == String.Empty || DeDateStart.Date > DeDateEnd.Date)
                        {
                            convalidaInputOK = false;
                            EditErrorMessage = BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO);
                        }

                        // sia la data di inzio che la data di fine devono essere maggiori della data blocco
                        var blockRegDate = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.HasValue ? RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.Value : DateTime.MinValue;
                        if (convalidaInputOK)
                        {
                            if (DeDateStart.Date <= blockRegDate)
                            {
                                convalidaInputOK = false;
                                EditErrorMessage = BusinessService.GetLocalizedString(PowerWebResources.ERR_X_MINORE_DI_DATA_BLOCCO.ToString(), "Data inizio");
                            }
                        }

                        if (convalidaInputOK)
                        {
                            if (DeDateEnd.Date <= blockRegDate)
                            {
                                convalidaInputOK = false;
                                EditErrorMessage = BusinessService.GetLocalizedString(PowerWebResources.ERR_X_MINORE_DI_DATA_BLOCCO.ToString(), "Data fine");
                            }
                        }

                        // per proseguire con l'elaborazione il collaboratore è obbligatorio
                        if (convalidaInputOK)
                        {
                            if (CmbSelColId.Value == null)
                            {
                                convalidaInputOK = false;
                                EditErrorMessage = BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_COL_ID);
                            }
                        }

                        // per proseguire l'elaborazione il collaboratore deve essere presente in anagrafica
                        var colId = Convert.ToInt32(CmbSelColId.Value);
                        Col currentCol = default(Col);
                        if (convalidaInputOK)
                        {
                            currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == colId);
                            if (currentCol == default(Col))
                            {
                                convalidaInputOK = false;
                                EditErrorMessage = BusinessService.GetLocalizedString(PowerWebResources.ERR_COL_NON_TROVATO);
                            }
                        }

                        // per l'elaborazione è necessario che il collaboratore abbia un orario collegato
                        if (convalidaInputOK)
                        {
                            if (!currentCol.Tab_Orari_Tipo_Id.HasValue)
                            {
                                convalidaInputOK = false;
                                EditErrorMessage = BusinessService.GetLocalizedString(PowerWebResources.STR_ORARIO_NO_COL);
                            }
                        }

                        #endregion

                        #region Inserimento reg_v da orario

                        // si prosegue con l'elaborazione solamente se la convalida input è andata a buon fine
                        if (convalidaInputOK)
                        {
                            // recupero del codice collaboratore, cantiere, motivazione e delle date di partenza/arrivo del periodo
                            // ... e calcolo l'ultimo id inserito

                            var cantId = Convert.ToInt32(CmbSelCantId.Value);
                            var motId = Convert.ToInt32(CmbSelMotivazione.Value);
                            var dateStart = DeDateStart.Date;
                            var dateEnd = DeDateEnd.Date;
                            lastId = 1;
                            if (lastRegV != null)
                                lastId = lastRegV.RegE;
                            //controllo se è stato selezionato un cantiere
                            //se è stato selezionato vado a creare le reg assegnando il cantiere selzionato
                            if (cantId != 0)
                            {
                                // calcolo dell'elenco delle giornate da inserire
                                var colCalendar = RepoManager.Tab_OrariRepo.GetPlanTimes(colId, dateStart, dateEnd, null, null);

                                // se è stato ritornato un calendario
                                if (colCalendar.Any())
                                {
                                    // inizializzazione della lista di reg_v che poi andrà aggiunta al data source
                                    var regVsToAdd = new List<Reg_V>();

                                    // ciclo di elaborazione delle date ritornate e generazione delle reg_v secondo l'orario recuperato
                                    foreach (var dayCalendar in colCalendar)
                                    {
                                        // se è stato previsto del lavoro nella giornata che si sta processando
                                        if (dayCalendar.Value.Any())
                                        {
                                            // allora per ogni orario viene generata una reg_v con i dati selezionati
                                            foreach (var detailCalendar in dayCalendar.Value)
                                            {
                                                var newRegV = RepoManager.Reg_VRepo.Init();
                                                newRegV.RegE = ++lastId;
                                                newRegV.Col_Id = colId;
                                                newRegV.Cant_Id = cantId;
                                                newRegV.Motivazione_Reg_Id = motId == 0 ? (int?)null : motId;
                                                newRegV.Data_Reg = dayCalendar.Key;

                                                var dataOraFisE = new DateTime(dayCalendar.Key.Year, dayCalendar.Key.Month, dayCalendar.Key.Day, detailCalendar.Item1.Hours, detailCalendar.Item1.Minutes, 0);
                                                var dataOraFisU = new DateTime(dayCalendar.Key.Year, dayCalendar.Key.Month, dayCalendar.Key.Day, detailCalendar.Item2.Hours, detailCalendar.Item2.Minutes, 0);

                                                // se è richiesto l'inserimento di una registrazione solo durata
                                                // allora inserisco i corrispettivi dati; altrimenti procedo con una registrazione standard entrata/uscita
                                                if (dataOraFisE.TimeOfDay == TimeSpan.Zero && dataOraFisU.TimeOfDay != TimeSpan.Zero)
                                                {
                                                    newRegV.Durata_Fis_HH_C = new DateTime(dayCalendar.Key.Year, dayCalendar.Key.Month, dayCalendar.Key.Day, dataOraFisU.TimeOfDay.Hours,
                                                        dataOraFisU.TimeOfDay.Minutes, dataOraFisU.TimeOfDay.Seconds);
                                                    newRegV.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                                                    newRegV.Registrazione_Tipo_Reg = (int)RegTypeEnum.Duration;
                                                }
                                                else
                                                {
                                                    newRegV.Data_Ora_Fis_E = dataOraFisE;
                                                    newRegV.Data_Ora_Fis_U = dataOraFisU;
                                                }

                                                regVsToAdd.Add(newRegV);
                                            }
                                        }
                                    }

                                    // aggiunta delle regV calcolate al data source dell'elenco
                                    RegVs.AddRange(regVsToAdd);
                                }
                            }
                            //in caso contrario vado ad assegnare il cantiere selezionato nell'orario
                            else {
                                // calcolo dell'elenco delle giornate da inserire
                                var colCalendars = RepoManager.Tab_OrariRepo.GetPlanTimesNew(colId, dateStart, dateEnd, null, null);

                                // se è stato ritornato un calendario
                                if (colCalendars.Any())
                                {
                                    // inizializzazione della lista di reg_v che poi andrà aggiunta al data source
                                    var regVsToAdd = new List<Reg_V>();

                                    // ciclo di elaborazione delle date ritornate e generazione delle reg_v secondo l'orario recuperato
                                    foreach (var dayCalendar in colCalendars)
                                    {
                                        // se è stato previsto del lavoro nella giornata che si sta processando
                                        if (dayCalendar.Value.Any())
                                        {
                                            // allora per ogni orario viene generata una reg_v con i dati selezionati
                                            foreach (var detailCalendar in dayCalendar.Value)
                                            {
                                                var newRegV = RepoManager.Reg_VRepo.Init();
                                                newRegV.RegE = ++lastId;
                                                newRegV.Col_Id = colId;
                                                newRegV.Cant_Id = detailCalendar.Item1;
                                                newRegV.Motivazione_Reg_Id = motId == 0 ? (int?)null : motId;
                                                newRegV.Data_Reg = dayCalendar.Key;

                                                var dataOraFisE = new DateTime(dayCalendar.Key.Year, dayCalendar.Key.Month, dayCalendar.Key.Day, detailCalendar.Item2.Hours, detailCalendar.Item2.Minutes, 0);
                                                var dataOraFisU = new DateTime(dayCalendar.Key.Year, dayCalendar.Key.Month, dayCalendar.Key.Day, detailCalendar.Item3.Hours, detailCalendar.Item3.Minutes, 0);

                                                // se è richiesto l'inserimento di una registrazione solo durata
                                                // allora inserisco i corrispettivi dati; altrimenti procedo con una registrazione standard entrata/uscita
                                                if (dataOraFisE.TimeOfDay == TimeSpan.Zero && dataOraFisU.TimeOfDay != TimeSpan.Zero)
                                                {
                                                    newRegV.Durata_Fis_HH_C = new DateTime(dayCalendar.Key.Year, dayCalendar.Key.Month, dayCalendar.Key.Day, dataOraFisU.TimeOfDay.Hours,
                                                        dataOraFisU.TimeOfDay.Minutes, dataOraFisU.TimeOfDay.Seconds);
                                                    newRegV.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                                                    newRegV.Registrazione_Tipo_Reg = (int)RegTypeEnum.Duration;
                                                }
                                                else
                                                {
                                                    newRegV.Data_Ora_Fis_E = dataOraFisE;
                                                    newRegV.Data_Ora_Fis_U = dataOraFisU;
                                                }

                                                regVsToAdd.Add(newRegV);
                                            }
                                        }
                                    }

                                    // aggiunta delle regV calcolate al data source dell'elenco
                                    RegVs.AddRange(regVsToAdd);
                                }
                            }
                            
                        }

                        #endregion

                        break;
                    
                    // Annulla (freccia indiatro)
                    case "undo":
                        ResetSession();
                        break;

                    // Elabora (baffo berde)
                    case "elaborate":
                        {
                            #region elaborate

                            // prima dell'elaborazione viene effettuato un bind della griglia di modo da ricevere i dati aggiornati
                            Bind_gvMassiveInsertEdit();

                            Dictionary<String, String> validationErrors = new Dictionary<string, string>();
                            
                            List<int> alreadyPresentWarning = new List<int>();

                            HashSet<DateTime> toElaborateDates = new HashSet<DateTime>();

                            List<int> colIds = new List<int>();

                            List<Reg> toAddRegs = new List<Reg>();

                            var errorByColId = new Dictionary<int, IList<Dictionary<string, string>>>();

                            var toElabRegs = RegVs;

                            List<Reg_V> alreadyPresent = new List<Reg_V>();

                            if (toElabRegs.Count > 0)
                            {
                                int count = 1;
                                foreach (var elabReg in toElabRegs)
                                {
                                    bool present = false;
                                    // prima di effettuare la check correggo eventuali errori di data e durata
                                    ManageRegVTime(elabReg);

                                    // se è attiva la relativa customization, controlla che le regvs non siano già state inserite precedentemente nel db
                                    CheckIfRegvAlreadyPresentEnum CheckIfRegvAlreadyPresentEnum = 0;
                                    CheckIfRegvAlreadyPresentEnum = (CheckIfRegvAlreadyPresentEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CheckIfRegvAlreadyPresentEnum);

                                    if (CheckIfRegvAlreadyPresentEnum == CheckIfRegvAlreadyPresentEnum.Check)
                                    {
                                        // aggiunge le regs già presenti a una lista che verrà poi tolta dalle regvs da elaborare
                                        if (RepoManager.Reg_VRepo.CheckAlreadyPresent(elabReg))
                                        {
                                            alreadyPresent.Add(elabReg);
                                            present = true; // segnala che la regv è già presente nel db (e quindi verrà depennata): non serve fare controlli su di essa
                                            alreadyPresentWarning.Add(count + 1);
                                        }
                                    }

                                    if (!present)
                                    {
                                        // controllo la presenza di eventuali incongruenze
                                        validationErrors = RepoManager.Reg_VRepo.Check(elabReg);
                                    }
                                    
                                    
                                    if (validationErrors.Count > 0)
                                    {
                                        SetErrorMessageDictionary(count + 1, errorByColId, validationErrors);
                                        continue;
                                    }

                                    if (elabReg.Data_Reg.HasValue && !present)
                                    {

                                        if (elabReg.Data_Ora_Fis_E == DateTime.MinValue)
                                        {
                                            if (elabReg.Data_Ora_Fis_U.HasValue)
                                            {
                                                elabReg.Data_Ora_Fis_E = elabReg.Data_Ora_Fis_U.Value;
                                                elabReg.Data_Ora_Fis_U = null;
                                            }
                                            else continue;
                                        }

                                        int daysdifference = (elabReg.Data_Reg.Value.Date - elabReg.Data_Ora_Fis_E.Date).Days;
                                        elabReg.Data_Ora_Fis_E = elabReg.Data_Ora_Fis_E.AddDays(daysdifference);

                                        if (elabReg.Data_Ora_Fis_U != null)

                                            if (elabReg.Data_Ora_Fis_U.Value.TimeOfDay < elabReg.Data_Ora_Fis_E.TimeOfDay)
                                                elabReg.Data_Ora_Fis_U = elabReg.Data_Ora_Fis_U.Value.AddDays(daysdifference + 1);
                                            else
                                                elabReg.Data_Ora_Fis_U = elabReg.Data_Ora_Fis_U.Value.AddDays(daysdifference);
                                    }
                                    else continue;

                                    count++;

                                }

                            }

                            // se ci sono registrazioni già presenti nel db, le depenno dalle regvs da elaborare
                            if (alreadyPresent.Any())
                            {
                                foreach (var remove in alreadyPresent)
                                {
                                    toElabRegs.Remove(remove);
                                }
                            }

                            var checkErrors = errorByColId.Any();

                            if (!checkErrors)
                            {
                                for (int i = 0; i < toElabRegs.Count; i++)
                                {
                                    var regv = toElabRegs[i];

                                    // prima di procede al salvataggio e all'elaborazione correggo eventualmente le ore e la durata
                                    ManageRegVTime(regv);

                                    if (regv.Data_Ora_Fis_E == DateTime.MinValue)
                                    {
                                        if (regv.Data_Ora_Fis_U.HasValue)
                                        {
                                            regv.Data_Ora_Fis_E = regv.Data_Ora_Fis_U.Value;
                                            regv.Data_Ora_Fis_U = null;
                                        }
                                        else continue;
                                    }
                                    
                                    if (regv.Data_Reg.HasValue)
                                        toElaborateDates.Add(regv.Data_Reg.Value.Date);

                                    if (regv.Col_Id != 0 && regv.Col_Id != null && !colIds.Contains(regv.Col_Id.Value))
                                        colIds.Add(regv.Col_Id.Value);

                                    Reg newRegE = RepoManager.RegRepo.Init();

                                    if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_E != null && regv.Data_Ora_Fis_E != DateTime.MinValue)
                                    {
                                        newRegE.Registrazione_Data_Ora_Fis_Reg = new DateTime(regv.Data_Reg.Value.Year, regv.Data_Reg.Value.Month, regv.Data_Reg.Value.Day, regv.Data_Ora_Fis_E.Hour, regv.Data_Ora_Fis_E.Minute, regv.Data_Ora_Fis_E.Second);
                                    }

                                    newRegE.Cant_Id = regv.Cant_Id;
                                    newRegE.Col_Id = regv.Col_Id;
                                    newRegE.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;

                                    newRegE.Fru_Id = regv.Fru_Id;
                                    newRegE.Pru_Id = regv.Pru_Id;

                                    newRegE.DisAbilitazione_Reg = false;
                                    newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                                    newRegE.Registrazione_Data_Ora_Fig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;

                                    newRegE.Note_Reg = regv.Note_Reg;

                                    // se la registrazione che si sta processando è una registrazione di sola durata si impostano i campi di riferimento
                                    if (regv.IsOnlyDuration)
                                    {
                                        // il tipo registrazione è sola durata
                                        newRegE.Registrazione_Tipo_Reg = (int)RegTypeEnum.Duration;

                                        // la registrazione è sicuramente abbinata
                                        newRegE.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;

                                        // se la duarata è valorizzata la si calcola
                                        newRegE.Rettifica_Durata = Convert.ToInt32(CommonService.GetMinutesFromTimeSpan(regv.Durata_Fis_HH_C.TimeOfDay));
                                    }
                                    else
                                    {
                                        // in caso non si tratti di una solo durata il campo durata viene nullato
                                        newRegE.Rettifica_Durata = null;
                                    }

                                    validationErrors = RepoManager.RegRepo.Check(newRegE, true);

                                    var dataRegESave = newRegE.Registrazione_Data_Ora_Fis_Reg;

                                    if (validationErrors.Count > 0)
                                    {
                                        SetErrorMessageDictionary(i + 1, errorByColId, validationErrors);
                                        continue;
                                    }

                                    toAddRegs.Add(newRegE);


                                    if (regv.Data_Ora_Fis_U != null && regv.Data_Ora_Fis_U != DateTime.MinValue)
                                    {
                                        Reg newRegU = RepoManager.RegRepo.Init();

                                        if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_U != null && regv.Data_Ora_Fis_U != DateTime.MinValue)
                                        {
                                            newRegU.Registrazione_Data_Ora_Fis_Reg = new DateTime(regv.Data_Reg.Value.Year, regv.Data_Reg.Value.Month, regv.Data_Reg.Value.Day, regv.Data_Ora_Fis_U.Value.Hour, regv.Data_Ora_Fis_U.Value.Minute, regv.Data_Ora_Fis_U.Value.Second);

                                            if (newRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < dataRegESave.TimeOfDay)
                                                newRegU.Registrazione_Data_Ora_Fis_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg.AddDays(1);
                                        }


                                        newRegU.Cant_Id = regv.Cant_Id;
                                        newRegU.Col_Id = regv.Col_Id;
                                        newRegU.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;

                                        newRegU.Fru_Id = regv.Fru_Id;
                                        newRegU.Pru_Id = regv.Pru_Id;

                                        newRegU.ParentReg = newRegE;

                                        newRegU.DisAbilitazione_Reg = false;
                                        newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                                        newRegU.Registrazione_Data_Ora_Fig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;

                                        validationErrors = RepoManager.RegRepo.Check(newRegU, true);

                                        if (validationErrors.Count > 0)
                                        {
                                            SetErrorMessageDictionary(i + 1, errorByColId, validationErrors);
                                            continue;
                                        }

                                        toAddRegs.Add(newRegU);
                                    }
                                }

                                if (toAddRegs.Count > 0)
                                {
                                    RepoManager.RegRepo.Add(toAddRegs, true);

                                    List<Reg> toElaborateTotalRegs = new List<Reg>();

                                    DateTime startElabDate = DateTime.MinValue;
                                    DateTime endElabDate = DateTime.MinValue;

                                    foreach (var currentDate in toElaborateDates)
                                    {
                                        //-----------------Lettura delle REG del Gruppo NEW e del GRuppo OLD da rielaborare (Union delle due) --------------------

                                        //Le Ore dei Campi Date vengono sempre inizializzate a ZERO dal sistema
                                        //Occorre quindi selezionare SEMPRE x DATA MINORE della DATA con ORE ZERO del GG Successivo
                                        //Così vengono prese tutte le REG DEL GIORNO (per non mettere <= 23.59.59)  
                                        //Normalmente bastano quelle del Giorno (per cui i GG in più sono 1 per via dell'Ora 00:00:00)
                                        startElabDate = currentDate;
                                        endElabDate = currentDate.AddDays(1);

                                        // aggiornamento delle date in base alla configurazione del notturno
                                        BusinessService.ManageNocturneStartEndDate(ref startElabDate, ref endElabDate);

                                        //leggo Tutte le REG NEW Necessarie alla Successiva ELABORATE
                                        List<Reg> toElaborateRegs = RepoManager.RegRepo.Find(reg => reg.Col_Id.HasValue && colIds.Contains(reg.Col_Id.Value) &&
                                                                                                    (reg.Registrazione_Data_Ora_Fis_Reg >= startElabDate &&
                                                                                                     reg.Registrazione_Data_Ora_Fis_Reg < endElabDate), true).ToList();

                                        toElaborateRegs.ForEach(newReg =>
                                        {
                                            if (!toElaborateTotalRegs.Any(oldReg => oldReg.Reg_Id == newReg.Reg_Id))
                                                toElaborateTotalRegs.Add(newReg);
                                        });
                                    }

                                    //lancio la Ri_elaborazione delle REG da trattare
                                    RepoManager.RegRepo.Elaborate(toElaborateTotalRegs, startElabDate, endElabDate);
                                    ResetSession();
                                }
                            }
                            
                            // Effettuo l'inserimento dei messaggi di warning per le registrazioni scartate perché già presenti
                            // I messaggi vengono inseriti dopo gli altri per fare in modo che le registrazioni non scartate vengano inserite
                            foreach (var i in alreadyPresentWarning)
                            { 
                                Dictionary<String, String> warningList = new Dictionary<string, string>();
                                // TODO: internazzionalizzazione messaggi
                                warningList.Add("ATTENZIONE", "La registrazione non è stata inserita perché è già presente una registrazione per lo stesso collaboratore, presso lo stesso cantiere/assistito, alla stessa ora.");
                                SetErrorMessageDictionary(i, errorByColId, warningList);
                            }

                            // se si sono verificati degli errori, vengono visualizzati a video
                            if (errorByColId.Any())
                                EditErrorMessage = GetErrorMessageFromDictionary(errorByColId);

                            #endregion
                        }
                        break;
                }
            _isToHideLoadingPanel = true;

            Bind_gvMassiveInsertEdit();
        }

        protected void gvMassiveInsertEdit_OnAutoFilterCellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            SetupCalendarOwner(e.Editor as ASPxDateEdit);
        }

        protected void gvMassiveInsertEdit_BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
            Reg_V _regVStub = null;

            var gridRegVs = new List<Reg_V>();
            var regEToDelete = new List<int>();

            for (int i = 0; i < gvMassiveInsertEdit.VisibleRowCount; i++)
            {
                var currReg = (Reg_V)gvMassiveInsertEdit.GetRow(i);
                // PowerWebService.FillEntityProperties(currReg, regV.NewValues);
                gridRegVs.Add(currReg);
            }

            if (gridRegVs.Count > 0)
            {
                foreach (var regV in e.UpdateValues)
                {
                    int regE = Convert.ToInt32(regV.Keys[CommonService.GetPropertyName(() => _regVStub.RegE)]);
                    var currReg = gridRegVs.First(reg => reg.RegE == regE);
                    PowerWebService.FillEntityProperties(currReg, regV.NewValues);
                }

                regEToDelete.AddRange(e.DeleteValues.Select(regV => Convert.ToInt32(regV.Keys[CommonService.GetPropertyName(() => _regVStub.RegE)])));

            }

            RegVs = gridRegVs.Where(reg => !regEToDelete.Contains(reg.RegE)).ToList();

        }

        protected void gvMassiveInsertEdit_CommandButtonInitialize(object sender, ASPxGridViewCommandButtonEventArgs e)
        {
            if (e.ButtonType == ColumnCommandButtonType.Update || e.ButtonType == ColumnCommandButtonType.Cancel)
                e.Visible = false;
        }

        protected void gvMassiveInsertEdit_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            gvMassiveInsertEdit.CancelEdit();
            e.Cancel = true;
        }

        protected void gvMassiveInsertEdit_OnRowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            gvMassiveInsertEdit.CancelEdit();
            e.Cancel = true;
        }

        protected void gvMassiveInsertEdit_OnAfterPerformCallback(object sender, ASPxGridViewAfterPerformCallbackEventArgs e)
        {
            ASPxGridView gv = (ASPxGridView)sender;
            gv.JSProperties.Add("cpErrorString", null);
            if (!string.IsNullOrEmpty(EditErrorMessage))
            {
                gv.JSProperties["cpErrorString"] = EditErrorMessage;
                EditErrorMessage = null;
            }


            gv.JSProperties.Add("cpHidePanel", _isToHideLoadingPanel);
        }

        protected void gvMassiveInsertEdit_DataBinding(object sender, EventArgs e)
        {
            Bind_gvMassiveInsertEdit(false);
        }

        #endregion

    }


}
