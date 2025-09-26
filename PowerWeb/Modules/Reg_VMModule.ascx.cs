using Business;
using Business.ExportExcelEngine;
using Business.Repository;
using Common;
using DevExpress.Data.Filtering;
using DevExpress.Data.Linq;
using DevExpress.Web.ASPxEditors;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.Data;
using Domain;
using Domain.Extensions;
using Exports.ExportExcelCustom.ExportSpecialized;
using Exports.ExportExcelGeneric;
using Exports.ExportExcelSpecialized;
using Exports.ExportTxtCustom;
using log4net;
using Reports;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


namespace PowerWeb.Modules
{
    public partial class Reg_VMModule : BaseGridModule, IPrintModule, ILogModule, IExportXLSXModule, IGeoLocationModule
    {

        const Reg_V _regVStub = null;
        const String KEYFIELDNAME = "RegE";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Reg_VModule));

        List<Reg_V> newRegVs = new List<Reg_V>();

        /// <summary>
        /// Recupera la chiave di Bing utilizzata per effettuare le richieste di mappa.
        /// </summary>
        /// <value>
        /// La chiave di bing utilizzata per effettuare le richieste di mappa.
        /// </value>
        public string BingKey
        {
            get
            {
                return RepoManager.ParamRepo.ParametersRow.BingKey;
            }
        }

        public Boolean IsToShowEditGrid
        {
            get
            {
                return PowerWebContext.GetFromSession<Boolean>("IsToShowEditGrid" + gvRegVMEdit.ID);
            }

            set
            {
                PowerWebContext.SetToSession("IsToShowEditGrid" + gvRegVMEdit.ID, value);
            }
        }

        public String EditErrorMessage
        {
            get
            {
                return PowerWebContext.GetFromSession<String>("EditErrorMessage" + gvRegVMEdit.ID);
            }

            set
            {
                PowerWebContext.SetToSession("EditErrorMessage" + gvRegVMEdit.ID, value);
            }
        }

        public String CurrentColId
        {
            get
            {
                return PowerWebContext.GetFromSession<String>("CurrentColId" + gvRegVMEdit.ID);
            }

            set
            {
                PowerWebContext.SetToSession("CurrentColId" + gvRegVMEdit.ID, value);
            }
        }

        public String CurrentDataReg
        {
            get
            {
                return PowerWebContext.GetFromSession<String>("CurrentDataReg" + gvRegVMEdit.ID);
            }

            set
            {
                PowerWebContext.SetToSession("CurrentDataReg" + gvRegVMEdit.ID, value);
            }
        }

        public List<Reg_V> AllEditRegVs
        {
            get
            {
                return PowerWebContext.GetFromSession<List<Reg_V>>("AllEditRegVs_" + gvRegVMEdit.ID);
            }

            set
            {
                List<Reg_V> list = value;
                if (list != null)
                    PowerWebContext.SetToSession("AllEditRegVs_" + gvRegVMEdit.ID, list);
            }
        }

        public List<Reg_V> EditRegVs
        {
            get
            {
                return PowerWebContext.GetFromSession<List<Reg_V>>("EditRegVs_" + gvRegVMEdit.ID);
            }

            set
            {
                List<Reg_V> list = value;
                if (list != null)
                    PowerWebContext.SetToSession("EditRegVs_" + gvRegVMEdit.ID, list);
            }
        }

        public List<Reg_V> GridRegVs
        {
            get
            {
                List<Reg_V> regVs = new List<Reg_V>();
                for (int i = 0; i < gvRegVM.VisibleRowCount; i++)
                {
                    Reg_V currentRow = GridView.GetRow(i) as Reg_V;
                    if (currentRow != null)
                        regVs.Add(currentRow);
                }
                return regVs;
            }
        }

        public List<Col> Cols
        {
            get
            {
                List<Col> currentCols = PowerWebContext.GetFromSession<List<Col>>("Cols_" + GridView.ID);
                if (currentCols == null)
                {
                    currentCols = RepoManager.ColRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Col>>("Cols_" + GridView.ID, currentCols);
                }
                return currentCols;
            }
        }
        public List<Cant> Cants
        {
            get
            {
                List<Cant> currentCants = PowerWebContext.GetFromSession<List<Cant>>("Cants_" + GridView.ID);
                if (currentCants == null)
                {
                    currentCants = RepoManager.CantRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession("Cants_" + GridView.ID, currentCants);
                }
                return currentCants;
            }
        }

        public ILog Log
        {
            get { return _log; }
        }

        public override void ResetSession()
        {
            base.ResetSession();

            if (!Page.IsPostBack && !Page.IsCallback)
            {
                PowerWebContext.SetToSession<List<Col>>("EditRegVs_" + gvRegVMEdit.ID, null);
                PowerWebContext.SetToSession<List<Col>>("AllEditRegVs_" + gvRegVMEdit.ID, null);
                PowerWebContext.SetToSession<List<Cant>>("Cants_" + GridView.ID, null);
                PowerWebContext.SetToSession<List<Col>>("Cols_" + GridView.ID, null);
            }
        }

        public override ASPxGridView GridView
        {
            get { return gvRegVM; }
        }

        /// <summary>
        /// Recupera il template della form utilizzata per il recupero dei dati di raggruppamento in stampa;
        /// se impostato a null si utilizza il valore specificato nel modulo.
        /// </summary>
        /// <value>
        /// il template della form utilizzata per il recupero dei dati di raggruppamento in stampa;
        /// se impostato a null si utilizza il valore specificato nel modulo.
        /// </value>
        public PowerFormTemplate PrintFormTemplate
        {
            get
            {
                return null;
            }
        }

        public override PowerFormTemplate EditFormTemplate
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID);

                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryRegVM();

                    template = PowerWebService.GeneratePowerFormTemplate(this, templateDic, MetaFieldDescriptors);

                    PowerWebContext.SetToSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID, template);
                }
                return template;
            }
        }

        public ICollection<MetaFieldDescriptor> MetaFieldDescriptors
        {
            get
            {
                ICollection<MetaFieldDescriptor> metaFieldDescriptors = PowerWebContext.GetFromSession<ICollection<MetaFieldDescriptor>>("MetaFieldDescriptors_" + GridView.ID);
                if (metaFieldDescriptors == null)
                {
                    var metaDescriptor = RepoManager.MetaDescriptorRepo.Find(md => md.EntityType == "RegVM", md => md.MetaFieldDescriptor, true).ToList().First();
                    List<MetaFieldDescriptor> mtdR = RepoManager.MetaFieldDescriptorRepo.Find(m => m.MetaDescriptorId == metaDescriptor.MetaDescriptorId, true).ToList();

                    if (metaDescriptor != null)
                    {
                        metaFieldDescriptors = mtdR.OrderBy(mfd => mfd.VisibleIndex).ToList();
                        PowerWebContext.SetToSession<ICollection<MetaFieldDescriptor>>("MetaFieldDescriptors_" + GridView.ID, metaFieldDescriptors);
                    }
                }
                return metaFieldDescriptors;
            }

        }

        private bool regVWasChanged(Reg_V editedRegv)
        {
            bool isChanged = false;

            var prevRegV = EditRegVs.Single(regv => regv.RegE == editedRegv.RegE);

            isChanged = prevRegV.Cant_Id != editedRegv.Cant_Id || prevRegV.Motivazione_Reg_Id != editedRegv.Motivazione_Reg_Id
                || prevRegV.Data_Ora_Fis_E != editedRegv.Data_Ora_Fis_E || prevRegV.Data_Ora_Fis_U != editedRegv.Data_Ora_Fis_U
                || editedRegv.Registrazione_Bloccata != prevRegV.Registrazione_Bloccata || editedRegv.EntrataEU != prevRegV.EntrataEU
                || editedRegv.UscitaEU != prevRegV.UscitaEU || editedRegv.Activity_Evaluation != prevRegV.Activity_Evaluation;

            return isChanged;
        }

        /// <summary>
        /// Generazione della griglia di modifica veloce
        /// </summary>
        /// <param name="checkChanged">se è richiesto il controllo dei cambiamenti delle reg</c> [check ch anged].</param>
        private void generateEditedRegVs(bool checkChanged = true)
        {
            //se vi sono reg 
            if (EditRegVs != null && EditRegVs.Count > 0)
            {

                //creazione degli indici di start e end per la creazione della griglia
                int start = gvRegVMEdit.PageIndex * gvRegVMEdit.SettingsPager.PageSize;
                //come indice di fine è il numero di reg_v
                int end = EditRegVs.Count;


                //inizializzaione della nuova regV
                Reg_V newRegV = new Reg_V();

                //crezione dei campi della griglia
                GridViewDataColumn data_Ora_Fis_E = gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_E)] as GridViewDataColumn;
                GridViewDataColumn data_Ora_Fis_U = gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_U)] as GridViewDataColumn;
                GridViewDataColumn Cant_Id = gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.Cant_Id)] as GridViewDataColumn;
                GridViewDataColumn Motivazione_Reg_Id = gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.Motivazione_Reg_Id)] as GridViewDataColumn;
                GridViewDataColumn IsUTimeSameDayE = gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.IsUTimeSameDayE)] as GridViewDataColumn;
                GridViewDataColumn Registrazione_Bloccata = gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.Registrazione_Bloccata)] as GridViewDataColumn;
                GridViewDataColumn EntrataEU = gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.EntrataEU)] as GridViewDataColumn;
                GridViewDataColumn UscitaEU = gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.UscitaEU)] as GridViewDataColumn;
                GridViewDataColumn ActivityEvaluation = gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.Activity_Evaluation)] as GridViewDataColumn;


                for (int i = start; i < end; i++)
                {
                    //vengono estratti i 
                    ASPxDateEdit teData_Ora_Fis_E = (ASPxDateEdit)gvRegVMEdit.FindRowCellTemplateControl(i, data_Ora_Fis_E, "teData_Ora_Fis_E");
                    ASPxDateEdit teData_Ora_Fis_U = (ASPxDateEdit)gvRegVMEdit.FindRowCellTemplateControl(i, data_Ora_Fis_U, "teData_Ora_Fis_U");
                    ASPxComboBox cbCant_Id = (ASPxComboBox)gvRegVMEdit.FindRowCellTemplateControl(i, Cant_Id, "cbCant_Id");
                    ASPxComboBox cbMotivazione_Reg_Id = (ASPxComboBox)gvRegVMEdit.FindRowCellTemplateControl(i, Motivazione_Reg_Id, "cbMotivazione_Reg_Id");
                    //aggiunta del pulsante di cancellazione della combobox
                    EditButton btnEdit = new EditButton("X");
                    cbMotivazione_Reg_Id.Buttons.Add(btnEdit);

                    ASPxCheckBox cbIsUTimeSameDayE = (ASPxCheckBox)gvRegVMEdit.FindRowCellTemplateControl(i, IsUTimeSameDayE, "cbIsUTimeSameDayE");
                    ASPxCheckBox cbRegistrazioneBloccata = (ASPxCheckBox)gvRegVMEdit.FindRowCellTemplateControl(i, Registrazione_Bloccata, "cbRegistrazioneBloccata");
                    ASPxTextBox txtEntrataEU = (ASPxTextBox)gvRegVMEdit.FindRowCellTemplateControl(i, EntrataEU, "txtEntrataEU");
                    ASPxTextBox txtUscitaEU = (ASPxTextBox)gvRegVMEdit.FindRowCellTemplateControl(i, UscitaEU, "txtUscitaEU");
                    ASPxComboBox cbActivity_Evaluation = (ASPxComboBox)gvRegVMEdit.FindRowCellTemplateControl(i, ActivityEvaluation, "cbActivity_Evaluation");

                    //si vanno ad estrarre i valori dalla colonna
                    int regU = Convert.ToInt32(gvRegVMEdit.GetRowValues(i, CommonService.GetPropertyName(() => _regVStub.RegU)));
                    int regE = Convert.ToInt32(gvRegVMEdit.GetRowValues(i, gvRegVMEdit.KeyFieldName));
                    //int cantId = Convert.ToInt32(gvRegVMEdit.GetRowValues(i, CommonService.GetPropertyName(() => _regVStub.Cant_Id)));
                    int cantId = Convert.ToInt32(cbCant_Id.Value);
                    int motivazioneId = Convert.ToInt32(cbMotivazione_Reg_Id.Value);
                    // se non è presente il combobx allora il valore viene messo direttamente a true
                    bool isUTimeSameDayE = cbIsUTimeSameDayE == null || cbIsUTimeSameDayE.Checked;
                    bool blockedReg = cbRegistrazioneBloccata.Checked;
                    int registrazioneStatoReg = Convert.ToInt32(gvRegVMEdit.GetRowValues(i, CommonService.GetPropertyName(() => _regVStub.Registrazione_Stato_Reg)));
                    // si procede a verificare i dati di entrata/uscita solamente se le colonne in edit sono presenti
                    string entrataEU = null;
                    if (txtEntrataEU != null)
                        entrataEU = String.IsNullOrEmpty(txtEntrataEU.Text) ? null : txtEntrataEU.Text;
                    string uscitaEU = null;
                    if (txtUscitaEU != null)
                        uscitaEU = String.IsNullOrEmpty(txtUscitaEU.Text) ? null : txtUscitaEU.Text;


                    int actEvaluationId = Convert.ToInt32(cbActivity_Evaluation.Value);



                    //se è attiviata la personalizzazione per Alitalia e l'utente è affiliato al cliente Alitalia
                    if ((RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomerOnlyMultipleEditEvaluationEnum) == (int)CustomerOnlyMultipleEditEvaluationEnum.Enabled) &&
                        (PowerWebContext.Current.User.Cli_Id.HasValue))
                    {
                        if (teData_Ora_Fis_E != null || teData_Ora_Fis_U != null)
                        {
                            //viene estratta la data e ora di entrata dalla Reg_V corrispondente alla riga dove viene premuto il pulsante di modifica veloce
                            DateTime tmpData_Ora_fis_E = EditRegVs.ElementAt(i).Data_Ora_Fis_E;
                            //viene estratta la data e ora di uscita dalla Reg_V corrispondente alla riga dove viene premuto il pulsante di modifica veloce
                            DateTime? tmpData_Ora_fis_U = EditRegVs.ElementAt(i).Data_Ora_Fis_U;

                            //se l'ora di uscita è nulla(ad esempio in un'attivitià)
                            if (tmpData_Ora_fis_U == null)
                            {
                                //creo un nuovo date datetime in modo da non avere un valore nullo
                                tmpData_Ora_fis_U = new DateTime();
                            }

                            //viene creata una nuova nuova registrazione
                            newRegV = initReg_V(0, regU, tmpData_Ora_fis_E, tmpData_Ora_fis_U.Value, cantId, motivazioneId, isUTimeSameDayE, blockedReg, registrazioneStatoReg, entrataEU, uscitaEU, actEvaluationId, "");
                        }
                    }

                    else
                    {
                        if (teData_Ora_Fis_E == null || teData_Ora_Fis_U == null)
                            continue;

                        newRegV = initReg_V(regE, regU, teData_Ora_Fis_E.Date, teData_Ora_Fis_U.Date, cantId, motivazioneId, isUTimeSameDayE, blockedReg, registrazioneStatoReg, entrataEU, uscitaEU, actEvaluationId, teData_Ora_Fis_U.Text);
                    }
                    // viene in ogni caso controllata se cambia solamente una reg non nuova; in caso la reg sia nuova viene comunque aggiunta per l'elaborazione
                    if (checkChanged && newRegV.RegE != 0)
                    {
                        if (regVWasChanged(newRegV))
                        {
                            newRegVs.Add(newRegV);
                        }
                    }
                    else
                    {
                        newRegVs.Add(newRegV);
                    }
                }
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {


            if (!Page.IsPostBack)
            {
                GridView.DataBind();
                gvRegVMEdit.DataBind();
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {

            if (!gvRegVM.JSProperties.ContainsKey("cpName"))
                gvRegVM.JSProperties.Add("cpName", "gvRegVM");

            PowerWebService.GenerateGridColumns(GridView, MetaFieldDescriptors);

            if (!Page.IsCallback && !Page.IsPostBack)
                ResetSession();

            PowerWebService.FillGridLabels(EntityType, gvRegVM);
            PowerWebService.FillComboboxes(GridView);

            PowerWebService.FillComboboxes(gvRegVMEdit);
            PowerWebService.FillGridLabels(EntityType, gvRegVMEdit);

            lblCol_Id.Text = BusinessService.GetLocalizedString(CommonService.GetPropertyName(() => _regVStub.Col_Id), ResourceTypeEnum.Field);
            lblData_Reg.Text = BusinessService.GetLocalizedString(CommonService.GetPropertyName(() => _regVStub.Data_Reg), ResourceTypeEnum.Field);

            cbIncludeTrips.Checked = !RepoManager.ParamRepo.ParametersRow.Flag_Calcolo_Viaggi;

            GridView.ClientSideEvents.CustomButtonClick = "OnCustomEditButtonClick";

            gvRegVMEdit.HtmlDataCellPrepared += GridViewEdit_HtmlDataCellPrepared;

            cbIncludeTrips.Checked = RepoManager.ParamRepo.ParametersRow.Dflt_Include_Trips;
            cbIncludePass.Checked = RepoManager.ParamRepo.ParametersRow.Dflt_Include_Pass;
            cbIncludeActivity.Checked = RepoManager.ParamRepo.ParametersRow.Dflt_Include_Activities;
            cbIncludeBlocked.Checked = RepoManager.ParamRepo.ParametersRow.Dflt_Include_Blocked;

            cbDoRefresh.Checked = RepoManager.ParamRepo.ParametersRow.Dflt_DoRefresh_Grid;

            // se è richiesto dalle personalizzazioni di nascondere il pulsante di annullamento nel modulo di manutenzione
            // delle timbrature multiple... si procede con l'operazione
            if ((HideCancelButtonInMultipleEditRegsEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideCancelButtonInMultipleEditRegsEnum) == HideCancelButtonInMultipleEditRegsEnum.Hide)
                btnUndo.ClientVisible = false;

            //viene controllato se vi si hanno i permessi di insrimento 
            bool isToAdd = PowerWebContext.Current.User.IsUserAutorized(Utenti.OperationTypeEnum.Insert, CurrentPageTabAut, RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DefaultFunzAuthLevelEnum));
            //se non si hanno i permessi non viene mostrato il bottone di inserimento durante la modifica veloce(Inserito qui perchè non eredita da Grid Master Page)
            if (!isToAdd)
                //viene nascosta la visualizzazione del pulsante
                btnAddEditGrid.ClientVisible = false;

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomerOnlyMultipleEditEvaluationEnum) == (int)CustomerOnlyMultipleEditEvaluationEnum.Enabled &&
                    PowerWebContext.Current.User.Cli_Id.HasValue)
            {
                //se attiva la personalizzazione per Alitalia che non permette di modificare i campi della griglia di modifica veloce
                //vengono nascoste le colonne di ora entrata
                GridViewDataTextColumn columnOraE = (GridViewDataTextColumn)gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_E)];
                columnOraE.Visible = false;

                //vengono nascoste le colonne di ora di uscita
                GridViewDataTextColumn columnOraU = (GridViewDataTextColumn)gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_U)];
                columnOraU.Visible = false;
            }


        }

        /// <summary>
        /// Evento che si occipa della colorazione delle celle dello stato reg e tipo reg
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxGridViewTableDataCellEventArgs"/> instance containing the event data.</param>
        void GridViewEdit_HtmlDataCellPrepared(object sender, ASPxGridViewTableDataCellEventArgs e)

        {
            //se la colonna rigurada lo stato reg
            if (e.DataColumn.FieldName == (CommonService.GetPropertyName(() => _regVStub.Registrazione_Stato_Reg)))
            {
                //estraggo lo stato della registrazione
                var regState = Convert.ToInt32(e.CellValue);

                //se lo stato è uno stato di errore
                if (regState == (int)RegStateEnum.ErrMax || regState == (int)RegStateEnum.ErrMin || regState == (int)RegStateEnum.Overlap)
                    //la cella viene colorata di rosso
                    e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegStateEnum)regState, false);

                //altrimenti la cella viene colorata di nero
                else if (regState == (int)RegStateEnum.None)
                    e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegStateEnum)regState, false);
            }

            //altrimenti se la colonna riguarda il tipo di reg
            else if (e.DataColumn.FieldName == (CommonService.GetPropertyName(() => _regVStub.Registrazione_Tipo_Reg)))
            {
                //viene estratto il valore del tipo di registrazione
                var regType = Convert.ToInt32(e.CellValue);

                //se è un viaggio 
                if (regType == (int)RegTypeEnum.Trip)
                    e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
                //se è un'attività
                else if (regType == (int)RegTypeEnum.Att)
                    e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
                //se è un passaggio
                else if (regType == (int)RegTypeEnum.Pass)
                    e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
                //se è un arrotondamento per durata
                else if (regType == (int)RegTypeEnum.ArrotDur)
                    e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
                //se è una rettifica manuale
                else if (regType == (int)RegTypeEnum.RettTimeSheetManual)
                    e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)regType, false);
            }
        }

        /// <summary>
        /// Metodo di inizializzazione di una Reg_v
        /// </summary>
        /// <param name="regE">The reg e.</param>
        /// <param name="regU">The reg u.</param>
        /// <param name="data_Ora_Fis_E">The data_ ora_ fis_ e.</param>
        /// <param name="data_Ora_Fis_U">The data_ ora_ fis_ u.</param>
        /// <param name="cantId">The cant identifier.</param>
        /// <param name="motivazioneId">The motivazione identifier.</param>
        /// <param name="isUTimeSameDayE">if set to <c>true</c> [is u time same day e].</param>
        /// <param name="blockedReg">if set to <c>true</c> [blocked reg].</param>
        /// <param name="registrazioneStatoReg">The registrazione stato reg.</param>
        /// <param name="entrataEU">The entrata eu.</param>
        /// <param name="uscitaEU">The uscita eu.</param>
        /// <param name="actEvaluationId">The act evaluation identifier.</param>
        /// <returns></returns>
        private Reg_V initReg_V(int regE, int regU, DateTime data_Ora_Fis_E, DateTime data_Ora_Fis_U, int cantId, int motivazioneId,
            bool isUTimeSameDayE, bool blockedReg, int registrazioneStatoReg, string entrataEU, string uscitaEU, int actEvaluationId, string valueU)
        {
            Reg_V oldRegV = null;
            if (regE != 0)
                oldRegV = EditRegVs.Single(regv => regv.RegE == regE);
            else
                oldRegV = EditRegVs.FirstOrDefault(regv => regv.RegE != 0);

            var data_Reg = oldRegV.Data_Reg.Value;
            var col_Id = oldRegV.Col_Id;
            // se si sta elaborando una nuova reg allora non si mette la motivazione e si inserisce il cantiere
            // altrimenti si inserisce lo stesso inserito precedentemente
            int? mot_Id;
            int? cant_Id;

            if (motivazioneId != 0)
                mot_Id = motivazioneId;
            else
                mot_Id = null;

            if (cantId != 0)
                cant_Id = cantId;
            else
                cant_Id = null;

            string sotto_cantiere = null;
            bool exists_sottocantiere = false;
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.SubCant) == 1) {
                exists_sottocantiere = EditRegVs.Single(regv => regv.RegE == regE).GetAttachedProperty("Sotto_Cantiere") != null;


                if (exists_sottocantiere)
                {
                    sotto_cantiere = EditRegVs.Single(regv => regv.RegE == regE).Sotto_Cantiere;
                }
            }

            


            Reg_V newRegV = RepoManager.Reg_VRepo.Init();
            newRegV.RegE = regE;
            newRegV.RegU = regU;
            newRegV.Data_Reg = data_Reg;
            // se si sta gestendo una nuova regv viene impostata il tipo della reg a none;
            // altrimenti si inserisce quello della reg precedente
            if (regE != 0)
                newRegV.Registrazione_Tipo_Reg = oldRegV.Registrazione_Tipo_Reg;
            else
                newRegV.Registrazione_Tipo_Reg = (int)RegTypeEnum.None;

            // si gestisce anche per le reg vecchie il tipo modifica (quelle nuove saranno manuali)
            if (regE != 0)
                newRegV.Tipo_Modifica = oldRegV.Tipo_Modifica;
            else
                newRegV.Tipo_Modifica = (int)RegModifyTypeEnum.Manual;

            if (regE != 0)
                newRegV.Registrazione_Stato_Reg = registrazioneStatoReg;
            else
                newRegV.Registrazione_Stato_Reg = (int)RegStateEnum.None;

            var currTimeOfDay = new DateTime(data_Reg.Year, data_Reg.Month, data_Reg.Day, data_Ora_Fis_E.Hour, data_Ora_Fis_E.Minute, data_Ora_Fis_E.Second);

            if (currTimeOfDay.TimeOfDay != DateTime.MinValue.TimeOfDay || newRegV.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration)
            {
                newRegV.Data_Ora_Fis_E = new DateTime(data_Reg.Year, data_Reg.Month, data_Reg.Day, data_Ora_Fis_E.Hour, data_Ora_Fis_E.Minute, data_Ora_Fis_E.Second);
            }

            currTimeOfDay = new DateTime(data_Reg.Year, data_Reg.Month, data_Reg.Day, data_Ora_Fis_U.Hour, data_Ora_Fis_U.Minute, 0);

            if (RepoManager.ParamRepo.First().Abilita_Notturno == true && valueU != "")
            {
                DateTime middleNightU = new DateTime(currTimeOfDay.Year, currTimeOfDay.Month, currTimeOfDay.Day, currTimeOfDay.Hour, currTimeOfDay.Minute, 01);
                newRegV.Data_Ora_Fis_U = middleNightU;
                currTimeOfDay = middleNightU;
            }


            if (currTimeOfDay.TimeOfDay != DateTime.MinValue.TimeOfDay)
            {
                //nel caso sia notturno vado a mettere il giorno corretto
                if (newRegV.Data_Ora_Fis_U != null && newRegV.Data_Ora_Fis_U.Value.Hour < newRegV.Data_Ora_Fis_E.Hour && newRegV.Data_Ora_Fis_E.Hour < 6)
                {
                    data_Reg = data_Reg.AddDays(1);
                    newRegV.Data_Ora_Fis_U = new DateTime(data_Reg.Year, data_Reg.Month, data_Reg.Day, data_Ora_Fis_U.Hour, data_Ora_Fis_U.Minute, currTimeOfDay.Second);
                }
                else {
                    newRegV.Data_Ora_Fis_U = new DateTime(data_Reg.Year, data_Reg.Month, data_Reg.Day, data_Ora_Fis_U.Hour, data_Ora_Fis_U.Minute, currTimeOfDay.Second);
                }
            }
            else if (newRegV.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration)
            {
                newRegV.Data_Ora_Fis_U = null;
                newRegV.Durata_Fig = newRegV.Durata_Fis = oldRegV.Durata_Fig;
                newRegV.Durata_Fig_HH_S = newRegV.Durata_Fis_HH_S = CommonService.GetHHMMStringFormMinutes(newRegV.Durata_Fig.HasValue ? newRegV.Durata_Fig.Value : 0);
            }

            newRegV.Pru_Id = oldRegV.Pru_Id;
            newRegV.Fru_Id = oldRegV.Fru_Id;

            newRegV.Col_Id = col_Id;

            newRegV.Cant_Id = cant_Id;
            if (mot_Id == 0)
                newRegV.Motivazione_Reg_Id = null;
            else
                newRegV.Motivazione_Reg_Id = mot_Id;

            newRegV.IsUTimeSameDayE = isUTimeSameDayE;
            newRegV.Pru_Id = oldRegV.Pru_Id;
            newRegV.Fru_Id = oldRegV.Fru_Id;
            newRegV.Qualifica_Col = oldRegV.Qualifica_Col;

            newRegV.Registrazione_Bloccata = blockedReg;

            newRegV.EntrataEU = entrataEU;
            newRegV.UscitaEU = uscitaEU;

            Cant cantRegV = RepoManager.CantRepo.FirstOrDefault(can => can.Cant_Id == cantId);

            if (cantRegV != default(Cant) && cantRegV.Tipologia_Can != "ATT")
                newRegV.Activity_Evaluation = null;

            else
                newRegV.Activity_Evaluation = actEvaluationId;

            if (exists_sottocantiere)
            {
                newRegV.Sotto_Cantiere = sotto_cantiere;
            }

            return newRegV;
        }

        protected void gvRegVM_DataBinding(object sender, EventArgs e)
        {
            LinqServerModeDataSource serverMode = new LinqServerModeDataSource();

            //viene definito il nome dell'oggetto che si va a recuperare da una fonte dati
            serverMode.ContextTypeName = "PowerWebEntities.Data";
            //viene specificato il nome della tabella della quale si a recuperare i dati
            serverMode.TableName = "Reg_V";
            //Consente di fornire una fonte di dati personalizzata (
            serverMode.Selecting += linq_Selecting;

            GridView.DataSource = serverMode;

            GridView.KeyFieldName = KEYFIELDNAME;
        }

        private bool isFilterApplied(string column, Type filteredEntity)
        {
            bool res = false;

            var currColumn = GridView.Columns[column] as GridViewDataColumn;
            if (currColumn != null)
            {
                if (filteredEntity == typeof(Col) || filteredEntity == typeof(Cant))
                    res = currColumn.FilterExpression != String.Empty && currColumn.FilterExpression.Contains("=");
                else if (filteredEntity == typeof(DateTime))
                {
                    res = currColumn.FilterExpression != String.Empty && ((currColumn.FilterExpression.Contains("=") || (currColumn.FilterExpression.Contains(">")) && !(currColumn.FilterExpression.Contains("<>"))) ||
                        (currColumn.FilterExpression.Contains("Between") && !currColumn.FilterExpression.Contains("Not")));
                }
            }

            return res;
        }

        private bool checkCurrentFilter()
        {
            if (!GridView.JSProperties.ContainsKey("cpIsFilterValid"))
                GridView.JSProperties.Add("cpIsFilterValid", false);

            if (!GridView.JSProperties.ContainsKey("cpFilterError"))
                GridView.JSProperties.Add("cpFilterError", "");

            bool isFilterValid = isFilterApplied(CommonService.GetPropertyName(() => _regVStub.Col_Id), typeof(Col));
            string filterErrorMessage = "";

            if (!isFilterValid)
            {
                isFilterValid = isFilterApplied(CommonService.GetPropertyName(() => _regVStub.Cant_Id), typeof(Cant));

                if (!isFilterValid)
                {
                    isFilterValid = isFilterApplied(CommonService.GetPropertyName(() => _regVStub.Data_Reg), typeof(DateTime));

                    if (isFilterValid)
                    {
                        DateTime fromDate = DateTime.MinValue;
                        DateTime toDate = DateTime.MaxValue;

                        var dataRegColumn = GridView.Columns[CommonService.GetPropertyName(() => _regVStub.Data_Reg)] as GridViewDataColumn;
                        var filterExp = dataRegColumn.FilterExpression;
                        if (!filterExp.Contains(" = "))
                        {
                            if (filterExp.Contains(">") && !filterExp.Contains("<"))
                            {
                                var dates = filterExp.Split('#');
                                var fromDateString = dates[1].Split('-');
                                fromDate = new DateTime(Int32.Parse(fromDateString[0]), Int32.Parse(fromDateString[1]), Int32.Parse(fromDateString[2]));
                                toDate = DateTime.Now;
                            }
                            else
                            {
                                var dates = filterExp.Split('#');
                                var fromDateString = dates[1].Split('-');
                                var toDateString = dates[3].Split('-');
                                // potrebbe essere che l'ultimo valore dello split contenga uno spazio a separare la data data dall'ora;
                                // si prevede questa possibilità nella compilazione
                                fromDate = new DateTime(Int32.Parse(fromDateString[0]), Int32.Parse(fromDateString[1]), Int32.Parse(fromDateString[2].Contains(" ") ? fromDateString[2].Substring(0, fromDateString[2].IndexOf(' ')) : fromDateString[2]));
                                toDate = new DateTime(Int32.Parse(toDateString[0]), Int32.Parse(toDateString[1]), Int32.Parse(toDateString[2].Contains(" ") ? toDateString[2].Substring(0, toDateString[2].IndexOf(' ')) : toDateString[2]));
                            }
                        }

                        isFilterValid = toDate.Subtract(fromDate) <= new TimeSpan(366, 0, 0, 0);
                        if (!isFilterValid)
                            filterErrorMessage = "Specificare un periodo massimo di un anno";
                    }
                    else
                        filterErrorMessage = "Il filtro impostato non è valido, specificare un Col/Cant/Periodo";
                }
            }


            //Se è impostato un filtro sull'ora di entrata, lo manipola a dovere
            var timeEcol = GridView.Columns[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fig_E)] as GridViewDataColumn;
            string filterExpr = timeEcol.FilterExpression;
            //if ()
            //timeEcol.FilterExpression  = filterExpr;

            GridView.JSProperties["cpIsFilterValid"] = isFilterValid;
            GridView.JSProperties["cpFilterError"] = filterErrorMessage;

            return isFilterValid;
        }

        private string generateWhereClause()
        {
            //var stringQueryable = RepoManager.Reg_VRepo.DbSet.AsNoTracking().Where(RepoManager.Reg_VRepo.Filter).AsQueryable().ToString();
            //var whereIndex = stringQueryable.LastIndexOf("WHERE");
            //string whereClause = "";
            //if (whereIndex != -1)
            //    whereClause = stringQueryable.Substring(whereIndex).Substring(5).Replace("[Extent1]", "").Replace("[", "").Replace("]", "").Replace(".", "");

            return RepoManager.Reg_VRepo.FilterText;
        }

        void linq_Selecting(object sender, LinqServerModeDataSourceSelectEventArgs e)
        {
            e.KeyExpression = KEYFIELDNAME;
            var emptyQueryable = Enumerable.Empty<Reg_V>().AsQueryable();
            var newQueryable = Enumerable.Empty<object>();
            IQueryable<Reg_V> currQueryable;

            if (IsToPopulateGrid)
            {
                if (checkCurrentFilter())
                {
                    currQueryable = RepoManager.Reg_VRepo.DbSet.SqlQuery(PowerWebService.GenerateWhereQuery(EntityType, GridView, generateWhereClause())).AsNoTracking().AsQueryable();
                    if (PowerWebContext.Current.User.Liv_Utente != 12 && !PowerWebContext.Current.IsSupervised && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.EnableDoubleCheckLogin) == 1)
                    {
                        newQueryable = currQueryable.Select(r => new Reg_V
                        {
                            RegE = r.RegE,
                            RegU = r.RegU,
                            Col_Id = r.Col_Id,
                            Col_Mnemonic = r.Col_Mnemonic,
                            Col_Desc = r.Col_Desc,
                            DisAbilitazione_Col = r.DisAbilitazione_Col,
                            Pru_Id = r.Pru_Id,
                            Codice_Pru = r.Codice_Pru,
                            Fru_Id = r.Fru_Id,
                            Codice_Fru = r.Codice_Fru,
                            Motivazione_Reg_Id = r.Motivazione_Reg_Id,
                            Motivazione_Reg_Cod = r.Motivazione_Reg_Cod,
                            Registrazione_Tipo_Desc = r.Registrazione_Tipo_Desc,
                            Data_Ora_Fig_E = r.Data_Ora_Fig_E,
                            Data_Ora_Fig_U = r.Data_Ora_Fig_U,
                            Durata_Fig = r.Durata_Fig,
                            Data_Ora_Fis_E = r.Data_Ora_Fis_E,
                            Data_Ora_Fis_U = r.Data_Ora_Fis_U,
                            Durata_Fis = r.Durata_Fis,
                            Data_Ora_Fig_ETime = r.Data_Ora_Fig_ETime,
                            Data_Ora_Fis_ETime = r.Data_Ora_Fis_ETime,
                            Data_Ora_Fig_UTime = r.Data_Ora_Fig_UTime,
                            Data_Ora_Fis_UTime = r.Data_Ora_Fis_UTime,
                            Data_Ora_Fig_EDate = r.Data_Ora_Fig_EDate,
                            Data_Reg = r.Data_Reg,
                            Durata_Fis_HH_S = r.Durata_Fis_HH_S,
                            Durata_Fig_HH_S = r.Durata_Fig_HH_S,
                            Tipo_Modifica = r.Tipo_Modifica,
                            KM_Reg = r.KM_Reg,
                            Ore_Calc_Reg = r.Ore_Calc_Reg,
                            Note_Reg = r.Note_Reg,
                            IsNotToElaborate = r.IsNotToElaborate,
                            Registrazione_Tipo_Reg = r.Registrazione_Tipo_Reg,
                            Registrazione_Stato_Reg = r.Registrazione_Stato_Reg,
                            RiferimentoRRN_Att = r.RiferimentoRRN_Att,
                            Resp_Id = r.Resp_Id,
                            Fil_Id = r.Fil_Id,
                            Data_Reg_AAAA = r.Data_Reg_AAAA,
                            Data_Reg_AAAA_MM = r.Data_Reg_AAAA_MM,
                            Registrazione_Bloccata = r.Registrazione_Bloccata,
                            EntrataEU = r.EntrataEU,
                            UscitaEU = r.UscitaEU,
                            Cli_Id = r.Cli_Id,
                            Codice_Commessa_Can = r.Codice_Commessa_Can,
                            Qualifica_Col = r.Qualifica_Col,
                            Codice_Gestionale_Can = r.Codice_Gestionale_Can,
                            Att_Cli_Id = r.Att_Cli_Id,
                            Activity_Evaluation = r.Activity_Evaluation,
                            Delta_Fig_Fis = r.Delta_Fig_Fis,
                            Stato_Attivita = r.Stato_Attivita,
                            Tipo_Attivita = r.Tipo_Attivita,
                            Turno = r.Turno,
                            Codice_Cliente = r.Codice_Cliente,
                            Cognome_Cli = r.Cognome_Cli,
                            Nome_Cli = r.Nome_Cli,
                            Ritardo_Durata = r.Ritardo_Durata,
                            Ritardo_Mail_Sent = r.Ritardo_Mail_Sent,
                            N_Serie_Fru = r.N_Serie_Fru,
                            Costo_Orario_Fig = r.Costo_Orario_Fig,
                            Costo_Orario_Fis = r.Costo_Orario_Fis,
                            CentroDiCosto_Id = r.CentroDiCosto_Id,
                        });

                        e.QueryableSource = newQueryable.AsQueryable();

                        return;
                    }
                }
                else
                    currQueryable = emptyQueryable;
            }
            else currQueryable = emptyQueryable;

            e.QueryableSource = currQueryable;


        }

        private Type _entityType = typeof(Reg_V);

        public override Type EntityType
        {
            get { return _entityType; }
        }

        #region Grid

        public override CriteriaOperator DefaultFilter
        {
            get
            {
                var today = DateTime.Now.Date;
                var firstDayOfLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                CriteriaOperator filter = new BetweenOperator(CommonService.GetPropertyName(() => new Reg_V().Data_Reg), firstDayOfLastMonth, today);
                return filter;
            }
        }


        protected void gvRegVM_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;

            if (grid != null)
            {
                Reg_V initRegv = RepoManager.Reg_VRepo.Init();
                if (Convert.ToBoolean(RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CloneSerialNumber)))
                {

                }
                PowerWebService.FillGridProperties(initRegv, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvRegVM_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Reg_V regv = new Reg_V();
            PowerWebService.FillEntityProperties(regv, e.NewValues);
            PowerWebService.FillEntityKey(regv, e.Keys, KEYFIELDNAME);
            RepoManager.Reg_VRepo.SetEntityBeforeAddOrUpdate(regv);

            var currentRegE = RepoManager.RegRepo.GetRegEFromNewValues(e.NewValues);
            PowerWebService.AddValidationErrors(RepoManager.RegRepo.Check(currentRegE, e.IsNewRow), e.Errors, GridView, typeof(Reg_VModule));

            regv.Data_Ora_Fis_E = currentRegE.Registrazione_Data_Ora_Fis_Reg.Date.Add(currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay);

            if (Convert.ToDateTime(e.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_U)]) != DateTime.MinValue)
            {
                var currentRegU = RepoManager.RegRepo.GetRegUFromNewValues(e.NewValues);
                regv.Data_Ora_Fis_U = currentRegE.Registrazione_Data_Ora_Fis_Reg.Date.Add(currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay);
                PowerWebService.AddValidationErrors(RepoManager.RegRepo.Check(currentRegU, e.IsNewRow), e.Errors, GridView, typeof(Reg_VModule));
            }

            PowerWebService.AddValidationErrors(RepoManager.Reg_VRepo.Check(regv, e.IsNewRow), e.Errors, GridView, typeof(Reg_VModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvRegVM_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("Reg-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));

            List<Reg> toAddRegsNew = new List<Reg>();

            //Inizializzo i Dati delle REG di Entrata e Uscita da Inserire                        
            var currentRegENew = RepoManager.RegRepo.GetRegEFromNewValues(e.NewValues);
            var currentRegUNew = RepoManager.RegRepo.GetRegUFromNewValues(e.NewValues); //Ha la data delle'Entrata
            DateTime dateTimeEFisNew = DateTime.MinValue;
            DateTime dateTimeUFisNew = DateTime.MinValue;

            //Recupero la Data  della Registrazione 
            DateTime dayDateNew = Convert.ToDateTime(e.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Reg)]).Date;

            //verifico se è stata inserita un'ora di Uscita senza un'ora di entrata
            if (Convert.ToDateTime(e.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_U)]) != DateTime.MinValue &&
                Convert.ToDateTime(e.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_E)]) == DateTime.MinValue)
            {
                //in questo caso (per uniformità) sposto i Dati dell'Uscita nell'Entrata
                currentRegENew = currentRegUNew;
                //annullo i Dati delle RegU
                currentRegUNew = null;
            }
            else
            {
                //verifico se NON è stata inserita un'ora di Uscita 
                if (Convert.ToDateTime(e.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_U)]) == DateTime.MinValue)
                    //annullo i Dati delle RegU
                    currentRegUNew = null;
            }

            //viene estratto il cantiere corrispondbte alla registrazione corrente
            Cant cantRegE = RepoManager.CantRepo.FirstOrDefault(z => z.Cant_Id == currentRegENew.Cant_Id);

            //se il tipo di cantiere è doiverso a attività, la valutazione dell'attività(campo strettamemnte legato all'attività) 
            //viene messo a NULL
            if (cantRegE != default(Cant) && cantRegE.Tipologia_Can != "ATT")
                currentRegENew.Activity_Evaluation = null;

            //Aggiungo la REGE fra le REg da Trattare
            toAddRegsNew.Add(currentRegENew);

            //Memorizzo Data/Ora Entrata e di Uscita (servono per la IMPROVE)            
            dateTimeEFisNew = currentRegENew.Registrazione_Data_Ora_Fis_Reg;
            if (currentRegUNew != null)
            {
                dateTimeUFisNew = currentRegUNew.Registrazione_Data_Ora_Fis_Reg;
                //se l'Ora di Uscita è < dell'Ora di Entrata significa che la Registrazione di Uscita è del giorno DOPO (caso di Notturno)
                if (dateTimeUFisNew.TimeOfDay < dateTimeEFisNew.TimeOfDay)
                    //Incremento la data della Registrazione di 1 GG 
                    dateTimeUFisNew = CommonService.ComputeDateTime(dayDateNew.AddDays(1), dateTimeUFisNew);
            }

            //Se l'Uscita NON è presente la salto
            if (currentRegUNew != null)
            {
                //se l'Ora di Uscita è < dell'Ora di Entrata significa che la Registrazione di Uscita è del giorno DOPO (caso di Notturno)
                if (dateTimeUFisNew.TimeOfDay < dateTimeEFisNew.TimeOfDay)
                    //Incremento la data della Registrazione di 1 GG 
                    currentRegUNew.Registrazione_Data_Ora_Fis_Reg = currentRegUNew.Registrazione_Data_Ora_Fis_Reg.AddDays(1);

                //viene estratto il cantiere della registrazione di uscita
                Cant cantRegU = RepoManager.CantRepo.FirstOrDefault(z => z.Cant_Id == currentRegENew.Cant_Id);

                //se la registrazione di uscita è diverso da un attività , la valutazione viene impostata  a NULL
                if (cantRegU != default(Cant) && cantRegU.Tipologia_Can != "ATT")
                    currentRegUNew.Activity_Evaluation = null;

                //aggiungo la REGU nelle Reg da Trattare
                toAddRegsNew.Add(currentRegUNew);
            }

            currentRegENew.CentroDiCosto_Id = (int?)e.NewValues[nameof(Reg.CentroDiCosto_Id)];

            RepoManager.RegRepo.Add(toAddRegsNew, true);

            // Le Ore dei Campi Date vengono sempre inizializzate a ZERO dal sistema
            // Occorre quindi selezionare SEMPRE x DATA MINORE della DATA con ORE ZERO del GG Successivo
            // Così vengono prese tutte le REG DEL GIORNO (per non mettere <= 23.59.59)  
            //Normalmente bastano quelle del Giorno (per cui i GG in più sono 1 per via dell'Ora 00:00:00)
            DateTime startElabDate = dayDateNew;
            DateTime endElabDate = dayDateNew.AddDays(1);

            // aggiornamento delle date in base alla configurazione del notturno
            BusinessService.ManageNocturneStartEndDate(ref startElabDate, ref endElabDate);

            //leggo Tutte le REG NEW Necessarie alla Successiva ELABORATE
            List<Reg> toElaborateNewRegs = RepoManager.RegRepo.Find(reg => reg.Col_Id == currentRegENew.Col_Id &&
                        reg.Registrazione_Data_Ora_Fis_Reg >= startElabDate && reg.Registrazione_Data_Ora_Fis_Reg < endElabDate, true).ToList();

            //lancio la Ri_elaborazione delle REG da trattare
            RepoManager.RegRepo.Elaborate(toElaborateNewRegs, startElabDate, endElabDate);

            e.Cancel = true;
            gvRegVM.CancelEdit();
        }

        protected void gvRegVM_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("Reg-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));


            #region Gestione Cancellazione Vecchi  Dati 

            List<Reg> toDeleteRegsOld = new List<Reg>();

            // inizializzazione delle variabili che conterranno le date originali da salvare e riportare nelle reg nuove
            DateTime origDateE = DateTime.MinValue;
            DateTime origDateU = DateTime.MinValue;

            //Recupero gli ID delle REG di Entrata e Uscita (quello di Uscita la recupero da quella dell'entrate e potrebbe non esserci)
            var currentRegEId = Convert.ToInt32(e.Keys[KEYFIELDNAME]);
            var currentRegUId = 0;

            //verifico se era presente anche una Reg di Uscita
            Reg currentRegUOld = RepoManager.RegRepo.SingleOrDefault(reg => reg.RiferimentoRRN_Reg == currentRegEId);
            if (currentRegUOld != null)
            {
                currentRegUId = currentRegUOld.Reg_Id;

                // se la reg u è presente allora viene salvata la sua data ora fisica originale
                origDateU = currentRegUOld.Registrazione_Data_Ora_Orig_Reg;
            }

            //Recupero la Data  della Registrazione 
            DateTime dayDateOld = Convert.ToDateTime(e.OldValues[CommonService.GetPropertyName(() => _regVStub.Data_Reg)]).Date;           

            //Inizializzo i Dati delle REG di Entrata e Uscita da eliminare                                       
            DateTime dateTimeEFisOld = DateTime.MinValue;
            DateTime dateTimeUFisOld = DateTime.MinValue;

            //Leggo e Aggiungo la REG di Entrata fra le Reg da Trattare leggendola con il suo ID (c'è sempre)
            var currentRegEOld = RepoManager.RegRepo.Single(reg => reg.Reg_Id == currentRegEId);

            Cant cantRegEToDelte = RepoManager.CantRepo.FirstOrDefault(z => z.Cant_Id == currentRegEOld.Cant_Id);

            if (cantRegEToDelte != default(Cant) && cantRegEToDelte.Tipologia_Can != "ATT")
                currentRegEOld.Activity_Evaluation = null;

            toDeleteRegsOld.Add(currentRegEOld);

            // viene salvata la data ora fisica originale della reg in entrata
            origDateE = currentRegEOld.Registrazione_Data_Ora_Orig_Reg;

            if (currentRegUId != 0)
            {
                Cant cantRegUToDelete = RepoManager.CantRepo.FirstOrDefault(z => z.Cant_Id == currentRegEOld.Cant_Id);

                if (cantRegUToDelete != default(Cant) && cantRegUToDelete.Tipologia_Can != "ATT")
                    currentRegEOld.Activity_Evaluation = null;

                //se esiste Aggiungo la Reg di Uscita fra quelle da Cancellare leggendola con il Suo Id                           
                toDeleteRegsOld.Add(currentRegUOld);
            }

            //Memorizzo la Data della Registrazione            
            dayDateOld = currentRegEOld.Registrazione_Data_Ora_Fis_Reg.Date;

            //Memorizzo Data/Ora Entrata e di Uscita (servono per la IMPROVE)     
            dateTimeEFisOld = currentRegEOld.Registrazione_Data_Ora_Fis_Reg;
            //verifico se esiste anche la Reg di UScita
            if (currentRegUId != 0)
            {
                //Essendo una Variazione la Data/Ora di Uscita è già quella giusta
                dateTimeUFisOld = currentRegUOld.Registrazione_Data_Ora_Fis_Reg;
            }

            // la cancellazione delle reg qui accumulate è effettuata l'istruzione prima della cancellazione
            #endregion

            #region  Gestione Inserimento Nuovi Dati 

            List<Reg> toAddRegsNew = new List<Reg>();

            //Recupero la Data  della Registrazione 
            DateTime dayDateNew = Convert.ToDateTime(e.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Reg)]).Date;

            // è calcolato il fatto che si sta elaborando un'uscita nello stesso giorno
            bool isSameDay = true;
            // se non c'è il notturno è sicuramente true
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && (RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight || RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.Duration))
                isSameDay = Convert.ToBoolean(e.NewValues[CommonService.GetPropertyName(() => _regVStub.IsUTimeSameDayE)]); // altrimenti dipende dal flag di modifica

            //Inizializzo i Dati delle REG di Entrata e Uscita da Inserire                        
            var currentRegENew = RepoManager.RegRepo.GetRegEFromNewValues(e.NewValues, currentRegEOld, true);

            // la reg u invece potrebbe essere nuova
            var currentRegUNew = new Reg();

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.MantainCoordinateModifiedRegs) == 0)
            {
                currentRegUNew = RepoManager.RegRepo.GetRegUFromNewValues(e.NewValues, currentRegUOld != null, currentRegUOld);
            }
            else 
            {
                currentRegUNew = RepoManager.RegRepo.GetRegUFromNewValuesCoordinates(e.NewValues, currentRegUOld, currentRegUOld != null);
            }
            // gestione delle date origine sulle reg da processare
            RepoManager.RegRepo.ManageOrigDates(currentRegENew, currentRegEId, origDateE, currentRegUNew, currentRegUId, origDateU, isSameDay);

            DateTime dateTimeEFisNew = DateTime.MinValue;
            DateTime dateTimeUFisNew = DateTime.MinValue;

            //verifico se è stata inserita un'ora di Uscita senza un'ora di entrata
            if (Convert.ToDateTime(e.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_U)]) != DateTime.MinValue &&
                Convert.ToDateTime(e.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_E)]) == DateTime.MinValue)
            {
                //in questo caso (per uniformità) sposto i Dati dell'Uscita nell'Entrata
                currentRegENew = currentRegUNew;
                currentRegEOld = currentRegUOld;
                //annullo i Dati delle RegU
                currentRegUNew = null;
                currentRegUOld = null;
            }
            else
            {
                //verifico se NON è stata inserita un'ora di Uscita senza un'ora di entrata
                if (Convert.ToDateTime(e.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_U)]) == DateTime.MinValue)
                    //annullo i Dati delle RegU
                    currentRegUNew = null;
            }

            // inserimento di un codice temporaneo (in caso di blocco della reg) per effettuare il suo successivo accoppiamento
            RepoManager.RegRepo.PerformBlockedRegsCouple(currentRegENew, currentRegUNew, currentRegEId);

            // prima dell'aggiunta della reg in entrata a quelle da inserire si verifica che sia cambiato il col_id o il cant_id e 
            // in questo caso sono svuotati pru e fru
            RepoManager.RegRepo.ManageCantColChangesBeforeUpdate(currentRegENew, currentRegEOld);

            //viene estratto il cantiere della reg di entrata
            Cant cantRegE = RepoManager.CantRepo.FirstOrDefault(z => z.Cant_Id == currentRegENew.Cant_Id);

            //se il cantiere della registrazione della reg non è un attività la valutazione è NULL
            if (cantRegE != default(Cant) && cantRegE.Tipologia_Can != "ATT")
                currentRegENew.Activity_Evaluation = null;

            //Aggiungo la REGE fra le REg da Trattare
            toAddRegsNew.Add(currentRegENew);

            //Memorizzo Data/Ora Entrata e di Uscita (servono per la IMPROVE)            
            dateTimeEFisNew = currentRegENew.Registrazione_Data_Ora_Fis_Reg;
            if (currentRegUNew != null)
            {
                dateTimeUFisNew = currentRegUNew.Registrazione_Data_Ora_Fis_Reg;
                //se l'Ora di Uscita è < dell'Ora di Entrata significa che la Registrazione di Uscita è del giorno DOPO (caso di Notturno)
                if (dateTimeUFisNew.TimeOfDay < dateTimeEFisNew.TimeOfDay)
                    // si procede all'incremento del giorno solamente se è abilitato il notturno  e non si tratta dello stesso giorno
                    if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && (RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight || RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.Duration) && !isSameDay)
                        //Incremento la data della Registrazione di 1 GG 
                        dateTimeUFisNew = CommonService.ComputeDateTime(dayDateNew.AddDays(1), dateTimeUFisNew);
            }

            //Se l'Uscita NON è presente la salto
            if (currentRegUNew != null)
            {
                //se l'Ora di Uscita è < dell'Ora di Entrata significa che la Registrazione di Uscita è del giorno DOPO (caso di Notturno)
                if (dateTimeUFisNew.TimeOfDay < dateTimeEFisNew.TimeOfDay)
                    // si procede all'incremento del giorno solamente se è abilitato il notturno  e non si tratta dello stesso giorno
                    if ((RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight || RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.Duration) && RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && !isSameDay)
                        currentRegUNew.Registrazione_Data_Ora_Fis_Reg = currentRegUNew.Registrazione_Data_Ora_Fis_Reg.AddDays(1);

                // prima dell'aggiunta della reg in entrata a quelle da inserire si verifica che sia cambiato il col_id o il cant_id e 
                // in questo caso sono svuotati pru e fru
                if (currentRegEOld != null)
                    RepoManager.RegRepo.ManageCantColChangesBeforeUpdate(currentRegUNew, currentRegEOld);

                Cant cantRegU = RepoManager.CantRepo.FirstOrDefault(z => z.Cant_Id == currentRegENew.Cant_Id);

                if (cantRegU != default(Cant) && cantRegU.Tipologia_Can != "ATT")
                    currentRegUNew.Activity_Evaluation = null;

                //aggiungo la REGU nelle Reg da Trattare
                toAddRegsNew.Add(currentRegUNew);
            }

            currentRegENew.CentroDiCosto_Id = (int?)e.NewValues[nameof(Reg.CentroDiCosto_Id)];

            // sono cancellate le reg solamente prima dell'aggiunta delle nuove popolate così da avere a disposizione i valori per il confronto
            // di modifica di cant_id e col_id
            RepoManager.RegRepo.Delete(toDeleteRegsOld, true);

            RepoManager.RegRepo.Add(toAddRegsNew, true);

            #endregion

            //
            // -----------------Lettura delle REG del Gruppo NEW e del GRuppo OLD da rielaborare (Union delle due) --------------------
            //
            // Le Ore dei Campi Date vengono sempre inizializzate a ZERO dal sistema
            // Occorre quindi selezionare SEMPRE x DATA MINORE della DATA con ORE ZERO del GG Successivo
            // Così vengono prese tutte le REG DEL GIORNO (per non mettere <= 23.59.59)  
            //Normalmente bastano quelle del Giorno (per cui i GG in più sono 1 per via dell'Ora 00:00:00)
            DateTime startElabDateNew = dayDateNew;
            DateTime endElabDateNew = dayDateNew.AddDays(1);
            DateTime startElabDateOld = dayDateOld;
            DateTime endElabDateOld = dayDateOld.AddDays(1);

            // gestione delle date di inizio/fine periodo in base alla configurazione del notturno
            BusinessService.ManageNocturneStartEndDate(ref startElabDateNew, ref endElabDateNew);
            BusinessService.ManageNocturneStartEndDate(ref startElabDateOld, ref endElabDateOld);

            //leggo Tutte le REG NEW Necessarie alla Successiva ELABORATE
            List<Reg> toElaborateRegsNew = RepoManager.RegRepo.Find(reg => (reg.Col_Id == currentRegENew.Col_Id) &&
                                                                         (reg.Registrazione_Data_Ora_Fis_Reg >= startElabDateNew &&
                                                                         reg.Registrazione_Data_Ora_Fis_Reg < endElabDateNew), true).ToList();
            List<Reg> toElaborateRegsOld = new List<Reg>();
            //verifico se ho cambiato o meno la Data per cui devo leggere anche le REG OLD
            if (startElabDateNew != startElabDateOld || endElabDateNew != endElabDateOld)
                //leggo Tutte le REG OLD Necessarie alla Successiva ELABORATE
                toElaborateRegsOld = RepoManager.RegRepo.Find(reg => (reg.Col_Id == currentRegEOld.Col_Id) &&
                                                                            (reg.Registrazione_Data_Ora_Fis_Reg >= startElabDateOld &&
                                                                            reg.Registrazione_Data_Ora_Fis_Reg < endElabDateOld), true).ToList();

            if (toElaborateRegsOld.Count > 0)
                RepoManager.RegRepo.Elaborate(toElaborateRegsOld, dateTimeEFisOld, dateTimeUFisOld);

            if (toElaborateRegsNew.Count > 0)
                RepoManager.RegRepo.Elaborate(toElaborateRegsNew, startElabDateNew, endElabDateNew);

            e.Cancel = true;
            GridView.CancelEdit();
        }

        protected void gvRegVM_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("Reg-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));

            // viene recuperata la reg_v da cancellare
            var regVId = Convert.ToInt32(e.Keys[KEYFIELDNAME]);
            Reg_V regVToDelete = RepoManager.Reg_VRepo.SingleOrDefault(regV => regV.RegE == regVId, true);

            // viene verificata la possibilità di poter cancellare il record
            Dictionary<string, string> errorDic = RepoManager.Reg_VRepo.CheckBeforeDelete(regVToDelete);
            if (errorDic.Any()) // se è presente qualche errore allora genero un'eccezione e non effettuo la cancellazione
                throw new InvalidOperationException(String.Join(Environment.NewLine, errorDic.Select(kvp => kvp.Value).ToArray()));

            // recupero della lista delle reg da cancellare
            List<Reg> toDeleteRegsOld = RepoManager.Reg_VRepo.GetRegToDelete(regVToDelete);

            //Recupero gli ID delle REG di Entrata e Uscita (quello di Uscita potrebbe non esserci)
            var currentRegEId = regVToDelete.RegE;
            int? currentRegUId = regVToDelete.RegU;
            int? currentColId;

            //Recupero la Data  della Registrazione 
            DateTime dayDateOld = regVToDelete.Data_Ora_Fis_E.Date;

            //Inizializzo i Dati delle REG di Entrata e Uscita da eliminare                    
            DateTime dateTimeEFisOld = DateTime.MinValue;
            DateTime dateTimeUFisOld = DateTime.MinValue;

            //Leggo e Aggiungo la REGE fra le REg da Trattare leggendola con il suo ID (c'è sempre)
            currentColId = toDeleteRegsOld.First().Col_Id;

            //Memorizzo la Data della Registrazione            
            dayDateOld = toDeleteRegsOld.First().Registrazione_Data_Ora_Fis_Reg.Date;

            //Memorizzo Data/Ora Entrata e di Uscita (servono per la IMPROVE)     
            dateTimeEFisOld = toDeleteRegsOld.First().Registrazione_Data_Ora_Fis_Reg;
            //verifico se esiste anche la Reg di Uscita
            if (currentRegUId.HasValue)
            {
                dateTimeUFisOld = toDeleteRegsOld.Last().Registrazione_Data_Ora_Fis_Reg;
                //Se l'Ora di Uscita è Minore dell'Entrata (Aggiungo 1 GG alla Data)
                if (dateTimeUFisOld.TimeOfDay < dateTimeEFisOld.TimeOfDay)
                    dateTimeUFisOld = dateTimeUFisOld.AddDays(1);
            }

            RepoManager.RegRepo.Delete(toDeleteRegsOld, true);
            RepoManager.RegRepo.SaveChanges();

            // Le Ore dei Campi Date vengono sempre inizializzate a ZERO dal sistema
            // Occorre quindi selezionare SEMPRE x DATA MINORE della DATA con ORE ZERO del GG Successivo
            // Così vengono prese tutte le REG DEL GIORNO (per non mettere <= 23.59.59)  
            // Normalmente bastano quelle del Giorno (per cui i GG in più sono 1 per via dell'Ora 00:00:00)
            DateTime startElabDate = dayDateOld;
            DateTime endElabDate = dayDateOld.AddDays(1);

            // gestione delle date di inizio/fine periodo in base alla configurazione del notturno
            BusinessService.ManageNocturneStartEndDate(ref startElabDate, ref endElabDate);

            //leggo Tutte le REG Necessarie alla Successiva ELABORATE
            List<Reg> toElaborateRegs = RepoManager.RegRepo.Find(reg => reg.Col_Id == currentColId &&
                       reg.Registrazione_Data_Ora_Fis_Reg >= startElabDate && reg.Registrazione_Data_Ora_Fis_Reg < endElabDate, true).ToList();

            //lancio la Ri_elaborazione delle REG da trattare
            RepoManager.RegRepo.Elaborate(toElaborateRegs, startElabDate, endElabDate);

            e.Cancel = true;
            gvRegVM.CancelEdit();
        }

        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _regVStub.Data_Reg))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Reg_V> regs = CommonService.ConvertTo<Reg_V>(items);
            string RptCol = "RPT_NOME_REPORT_SCHEDA_REG_COL";
            string RptCant = "RPT_NOME_REPORT_SCHEDA_REG_CAN";
            var RegSkReportCol = new XRRegSkCol();
            var RegSkReportCan = new XRRegSkCan();
            var RegSkReportErr = new XRRegSk01();
            if (String.Compare(report.Nome_Risorsa, RptCol) == 0)
            {
                RegSkReportCol = new XRRegSkCol(regs, PowerWebService.ConvertTabPageExtendedToString(selectedTabs), reportOptions);
                return new ExtXtraReport { Report = RegSkReportCol, PictureBox = RegSkReportCol.CompanyLogo };
            }
            else if (String.Compare(report.Nome_Risorsa, RptCant) == 0)
            {
                RegSkReportCan = new XRRegSkCan(regs, PowerWebService.ConvertTabPageExtendedToString(selectedTabs), reportOptions);
                return new ExtXtraReport { Report = RegSkReportCan, PictureBox = RegSkReportCan.CompanyLogo };
            }
            else
            {
                RegSkReportErr = new XRRegSk01(regs, PowerWebService.ConvertTabPageExtendedToString(selectedTabs));
                return new ExtXtraReport { Report = RegSkReportErr, PictureBox = RegSkReportErr.CompanyLogo };
            }
        }

        protected void gvRegVMPanel_Callback(object sender, DevExpress.Web.ASPxClasses.CallbackEventArgsBase e)
        {
            if (e.Parameter == "refresh")
                GridView.DataBind();
        }

        #region Gestione ottimizzata componente calendario
        protected void gvRegVM_CellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            SetupCalendarOwner(e.Editor as ASPxDateEdit);
        }

        protected void gvRegVM_AutoFilterCellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            SetupCalendarOwner(e.Editor as ASPxDateEdit);
        }

        void SetupCalendarOwner(ASPxDateEdit editor)
        {
            if (editor == null) return;

            editor.PopupCalendarOwnerID = "__ReferenceDateEdit";
        }
        #endregion

        #region Gestione Griglia di Edit multiplo

        #region GESTIONE DELLE AUTORIZZAZIONI CUSTOM PER LA PAGINA CORRENTE
        private Tab_Funz _currentPageTabFunz;
        public Tab_Funz CurrentPageTabFunz
        {
            get
            {
                string[] path = HttpContext.Current.Request.Url.AbsolutePath.Split('/');
                var pages = path[path.Length - 2];
                var module = path[path.Length - 1];
                string absolutePath = "/" + pages + "/" + module;
                if (_currentPageTabFunz == null)
                    _currentPageTabFunz = PowerWebContext.Current.TabFunzs.Single(tf => tf.Link_Tab_Funz.EndsWith(absolutePath));
                return _currentPageTabFunz;
            }
        }
        private Tab_Aut _currentPageTabAut;
        public Tab_Aut CurrentPageTabAut
        {
            get
            {
                if (_currentPageTabAut == null)
                {
                    //viene controlalto se lo user della sessione è valido
                    if (PowerWebContext.Current != null && PowerWebContext.Current.User != null)
                    {
                        //legge l'eventuale Record con Chiave Utente/Funzione
                        _currentPageTabAut = PowerWebContext.Current.TabAuts.SingleOrDefault(ta => ta.Utenti_Id == PowerWebContext.Current.User.Utenti_Id && ta.Tab_Funz_Id == CurrentPageTabFunz.Tab_Funz_Id);

                        if (_currentPageTabAut == null)
                            //Se NON esiste il REcord con Chiave Utente/Funzione e NON esiste nemmeno il Record con Chiave = UTENTE
                            //legge il Record con Chiave FUNZIONE
                            _currentPageTabAut = PowerWebContext.Current.TabAuts.SingleOrDefault(ta => ta.Utenti_Id == null && ta.Tab_Funz_Id == CurrentPageTabFunz.Tab_Funz_Id);
                    }
                    else
                    {
                        Response.Redirect(PowerWebService.LoginPageURL);
                    }
                }

                return _currentPageTabAut;
            }
        }
        #endregion

        /// <summary>
        ///Callbak che avviene alla pressione dell'edit multiplo o singolo per modificare le registrazioni
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxGridViewCustomCallbackEventArgs"/> instance containing the event data.</param>
        protected void gvRegVMEdit_CustomCallback(object sender, ASPxGridViewCustomCallbackEventArgs e)
        {
            //Dictionary<string, string> errors = new Dictionary<string, string>();
            var splitter = e.Parameters.Split(new char[] { '|' });

            CurrentColId = null;
            CurrentDataReg = null;

            var currentRegId = -1;
            if (splitter.Count() > 1)
            {
                currentRegId = Convert.ToInt32(splitter[1]);

                //alla pressione del pulsante di edit multiplo (M)
                if (splitter[0] == "initMulti")
                {
                    //si fa un ordinamento per ora di entrata
                    (gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_E)] as GridViewDataColumn).SortAscending();


                    var datacolum = gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_ETime)] as GridViewDataColumn;


                    //si fa un ordinamentdo per tipo di registrazione
                    (gvRegVMEdit.Columns[CommonService.GetPropertyName(() => _regVStub.Registrazione_Tipo_Reg)] as GridViewDataColumn).SortAscending();

                    IsToShowEditGrid = true;
                    EditErrorMessage = null;

                    //viene stratta la registrazione che ho premuto l'edit multiplo
                    Reg currentReg = RepoManager.RegRepo.Single(reg => reg.Reg_Id == currentRegId);

                    //viene estratta la data corrente delle registrazione estratta
                    var currentDate = currentReg.Registrazione_Data_Ora_Fis_Reg.Date;

                    //gestione dell'abilitazione del notturno
                    bool nocturneEnabled = false;

                    //se la registrazione ha un collaboratore 
                    if (currentReg.Col_Id.HasValue)
                    {
                        //revupera dai parametri se è abilitato il calcolo dei viaggi
                        var isToExcludeTrips = RepoManager.ParamRepo.ParametersRow.Flag_Calcolo_Viaggi;

                        //viene recuperato il from che corrisponde alla data della registrazione (serve per gestione notturno)
                        var from = currentDate;

                        //la data di fine per la gestione del notturno è il giorno seguente
                        var to = from.AddDays(1);

                        //se il notturno è abilitato
                        if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno &&
                            RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.None && RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.Disabled)
                        {
                            //il notturno è abilitato
                            nocturneEnabled = true;

                            //se è abilitato il notturno con nuova mezzanotte
                            if (RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight)
                            {
                                //viene recuperata la soglia dai parametri
                                var threshold = RepoManager.ParamRepo.ParametersRow.Default_Durata_Max_Gruppo_Notte_Ril;

                                //viene recuperata la soglia dal collaboratore
                                var colThreshold = RepoManager.ColRepo.Single(col => col.Col_Id == currentReg.Col_Id, true).Durata_Max_Gruppo_Notte_Ril_Col;

                                //se la soglia del collaboratore non è null
                                if (colThreshold != null)
                                    threshold = colThreshold;

                                if (threshold != null)
                                {
                                    //la nuova data di partenza 
                                    from = from.AddMinutes(threshold.Value.TotalMinutes);
                                    if (currentReg.Registrazione_Data_Ora_Fis_Reg < from)
                                        from = from.AddDays(-1);
                                }


                            }
                            //se è abilitato il notturno con nuova mezzanotte
                            else if (RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.Duration)
                            {
                                //viene istanziata la tupla contenete le due date di inzio e fine
                                Tuple<DateTime, DateTime> dateToSearch = null;

                                //viene recuperata la nuva coppia di date in base alla registrazione passata come parametro
                                dateToSearch = BusinessService.dateToSearchWithNocturn(currentReg);

                                //vengono aggiorante le date con le nuove date estratte
                                from = dateToSearch.Item1;
                                to = dateToSearch.Item2;

                            }

                        }

                        //se il notturno è abilitato
                        if (nocturneEnabled)
                            // al momento non si visualizzano nella griglia di edit multiplo le registrazioni rettifica, di tipo durata e di arrotondamento per durata
                            EditRegVs = RepoManager.Reg_VRepo.Find(regv => regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.ArrotDur && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.RettTimesheet && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Duration && regv.Col_Id == currentReg.Col_Id && (regv.Data_Ora_Fis_E >= from && regv.Data_Ora_Fis_E < to) && (regv.Data_Ora_Fis_U == null || regv.Data_Ora_Fis_U < to), true).ToList();
                        else
                            // al momento non si visualizzano nella griglia di edit multiplo le registrazioni rettifica, di tipo durata e di arrotondamento per durata
                            EditRegVs = RepoManager.Reg_VRepo.Find(regv => regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.ArrotDur && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.RettTimesheet && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Duration && regv.Col_Id == currentReg.Col_Id && regv.Data_Reg >= from && regv.Data_Reg < to, true).ToList();

                        //Escludo le attività dalle timbrature mostrate premendo la matita con la M
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowActivityInModifyRegs) == 1) 
                        {
                            EditRegVs = EditRegVs.Where(r => r.Registrazione_Tipo_Reg != 2).ToList();
                        }

                        //ordinamento per ora di entrata delle registrazionid della stessa data della registrazione dove ho cliccato il pulsante
                        EditRegVs = EditRegVs.OrderBy(regv => regv.Data_Ora_Fis_E).ToList();

                        //converto in lista tutte le Reg_V
                        AllEditRegVs = EditRegVs.ToList();

                        showCurrentEditRegVs();
                    }
                    else
                    {
                        EditRegVs = RepoManager.Reg_VRepo.Find(reg => reg.RegE == currentRegId).ToList();

                        AllEditRegVs = EditRegVs.ToList();

                        Bind_gvRegVMEdit();
                    }
                }
                else if (splitter[0] == "initSingle")
                {
                    IsToShowEditGrid = true;
                    EditErrorMessage = null;


                    EditRegVs = RepoManager.Reg_VRepo.Find(reg => reg.RegE == currentRegId).ToList();

                    AllEditRegVs = EditRegVs.ToList();

                    Bind_gvRegVMEdit();
                }
                else if (splitter[0] == "initClone")
                {
                    IsToShowEditGrid = true;
                    EditErrorMessage = null;

                    var clonedRegV = RepoManager.Reg_VRepo.Single(reg => reg.RegE == currentRegId);

                    Reg_V newRegV = RepoManager.Reg_VRepo.Init();

                    newRegV.Data_Reg = clonedRegV.Data_Reg;
                    newRegV.Col_Id = clonedRegV.Col_Id;
                    newRegV.Cant_Id = clonedRegV.Cant_Id;
                    newRegV.Motivazione_Reg_Id = clonedRegV.Motivazione_Reg_Id;
                    newRegV.Data_Ora_Fis_E = clonedRegV.Data_Ora_Fis_E;
                    newRegV.Data_Ora_Fis_U = clonedRegV.Data_Ora_Fis_U;
                    newRegV.Registrazione_Tipo_Reg = clonedRegV.Registrazione_Tipo_Reg;

                    newRegV.Registrazione_Stato_Reg = (int)RegStateEnum.None;
                    newRegV.Registrazione_Tipo_Reg = (int)RegTypeEnum.None;

                    EditRegVs = new List<Reg_V>() { newRegV };

                    Bind_gvRegVMEdit();
                }
            }
            //update della modifica "veloce mulipla"
            else if (e.Parameters == "update")
            {
                _log.Info(String.Format("Reg-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));

                //Generazione della griglia di modifica veloce
                generateEditedRegVs();

                Dictionary<String, String> validationErrors = new Dictionary<string, string>();

                //check su tutte le reg_V
                #region Reg_V check

                // inizializzazione del dizionario contenente i vecchi stati di blocco
                Dictionary<int, bool> doNotUpdateDic = new Dictionary<int, bool>();

                // sono ciclate tutte le nuove reg_v generate ed è effettuata su di loro la check
                foreach (var regV in newRegVs)
                {
                    if (regV.Registrazione_Tipo_Reg != 4) {
                        // calcolo il vecchio stato di blocco della regv (non bloccata se nuova)
                        bool wasBlocked = regV.RegE != 0
                            ? RepoManager.Reg_VRepo.SingleOrDefault(regv => regv.RegE == regV.RegE, true).Registrazione_Bloccata
                            : false;

                        // si effettua la check della regv solamente se le ore non la marcano per la cancellazione
                        // e se sia quella attualche che quella precedente sono bloccate
                        if ((regV.Data_Ora_Fis_E != DateTime.MinValue || regV.Data_Ora_Fis_U != null) &&
                            (!regV.Registrazione_Bloccata || !wasBlocked))
                            validationErrors = RepoManager.Reg_VRepo.Check(regV, regV.RegE == 0, false);
                        else
                        {
                            // se non si sta processando una reg nuova allora segnalo il fatto che non vada elaborata
                            if (regV.RegE != 0)
                                if (!doNotUpdateDic.ContainsKey(regV.RegE))
                                    doNotUpdateDic.Add(regV.RegE, true);
                        }



                        if (validationErrors.Count > 0)
                            break;
                    }
                }

                #endregion

                // si procede con l'elaborazione soalmente se non sono stati riscontrati errori
                if (validationErrors.Count == 0)
                {
                    //data su cui elaborare
                    HashSet<DateTime> toElaborateDates = new HashSet<DateTime>();

                    List<int> colIds = new List<int>();

                    #region Adding new Regs

                    //lista di registrazioni nuove da aggiungere 
                    var toAddRegVs = newRegVs.Where(regv => regv.RegE == 0 && regv.Registrazione_Tipo_Reg != 4).ToList();

                    List<Reg> toAddRegs = new List<Reg>();

                    //viene passata in rassegna ogni nuova registrazione nuova
                    foreach (var regv in toAddRegVs)
                    {
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

                        // inizializzazione delle date/ore originali
                        DateTime dataOrigE = DateTime.MinValue;
                        DateTime dataOrigU = DateTime.MinValue;

                        Reg newRegE = RepoManager.RegRepo.Init();

                        if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_E != null && regv.Data_Ora_Fis_E != DateTime.MinValue)
                        {
                            newRegE.Registrazione_Data_Ora_Fis_Reg = new DateTime(regv.Data_Reg.Value.Year, regv.Data_Reg.Value.Month, regv.Data_Reg.Value.Day, regv.Data_Ora_Fis_E.Hour, regv.Data_Ora_Fis_E.Minute, regv.Data_Ora_Fis_E.Second);
                            dataOrigE = newRegE.Registrazione_Data_Ora_Fis_Reg;
                        }

                        newRegE.Cant_Id = regv.Cant_Id;
                        newRegE.Col_Id = regv.Col_Id;
                        newRegE.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;

                        newRegE.Fru_Id = regv.Fru_Id;
                        newRegE.Pru_Id = regv.Pru_Id;

                        newRegE.DisAbilitazione_Reg = false;
                        newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                        newRegE.Registrazione_Data_Ora_Fig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;

                        newRegE.Flag_EU_Reg = regv.EntrataEU;

                        validationErrors = RepoManager.RegRepo.Check(newRegE, true);

                        if (validationErrors.Count > 0)
                            break;

                        toAddRegs.Add(newRegE);

                        Reg newRegU = null;

                        if (regv.Data_Ora_Fis_U != null && regv.Data_Ora_Fis_U != DateTime.MinValue)
                        {
                            newRegU = RepoManager.RegRepo.Init();

                            if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_U != null && regv.Data_Ora_Fis_U != DateTime.MinValue)
                            {
                                newRegU.Registrazione_Data_Ora_Fis_Reg = new DateTime(regv.Data_Reg.Value.Year, regv.Data_Reg.Value.Month, regv.Data_Reg.Value.Day, regv.Data_Ora_Fis_U.Value.Hour, regv.Data_Ora_Fis_U.Value.Minute, regv.Data_Ora_Fis_U.Value.Second);
                                dataOrigU = newRegU.Registrazione_Data_Ora_Fis_Reg;
                            }

                            newRegU.Cant_Id = regv.Cant_Id;
                            newRegU.Col_Id = regv.Col_Id;
                            newRegU.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;

                            newRegU.Fru_Id = regv.Fru_Id;
                            newRegU.Pru_Id = regv.Pru_Id;

                            newRegU.ParentReg = newRegE;

                            newRegU.DisAbilitazione_Reg = false;
                            newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                            newRegU.Registrazione_Data_Ora_Fig_Reg = newRegU.Registrazione_Data_Ora_Fig_Reg;

                            newRegU.Flag_EU_Reg = regv.UscitaEU;

                            validationErrors = RepoManager.RegRepo.Check(newRegU, true);

                            if (validationErrors.Count > 0)
                                break;

                            toAddRegs.Add(newRegU);
                        }

                        // è calcolato il fatto che si sta elaborando un'uscita nello stesso giorno
                        bool isSameDay = true;
                        // se non c'è il notturno è sicuramente true
                        if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight || RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.Duration)
                            isSameDay = regv.IsUTimeSameDayE;

                        // gestione delle date/ore originali
                        RepoManager.RegRepo.ManageOrigDates(newRegE, 0, dataOrigE, newRegU, 0, dataOrigU, isSameDay);
                    }
                    #endregion

                    //aggiornamento delle registrazioni esistenti
                    #region Updating existing Regs

                    List<Reg> toUpdateRegs = new List<Reg>();
                    List<Reg> toDeleteRegs = new List<Reg>();

                    if (validationErrors.Count == 0)
                    {
                        var toUpdateRegVs = newRegVs.Where(regv => regv.RegE != 0 && regv.Registrazione_Tipo_Reg != 4).ToList();

                        foreach (var regv in toUpdateRegVs)
                        {
                            // se sto processando una reg non nuova e è segnalata a causa blocco da non processare
                            // allora salto direttamente alla reg successiva
                            if (regv.RegE != 0)
                                if (doNotUpdateDic.ContainsKey(regv.RegE))
                                    continue;

                            //se la data della regv è uguale a {01/01/0001 00:00:00}
                            if (regv.Data_Ora_Fis_E == DateTime.MinValue)
                            {
                                if (regv.Data_Ora_Fis_U.HasValue)
                                {
                                    // la vecchia registrazione in entrata (sostituita, viene segnalata come da cancellare)
                                    toDeleteRegs.Add(RepoManager.RegRepo.FirstOrDefault(reg => reg.Reg_Id == regv.RegE));
                                    regv.Data_Ora_Fis_E = regv.Data_Ora_Fis_U.Value;
                                    regv.Data_Ora_Fis_U = null;
                                    regv.RegE = Convert.ToInt32(regv.RegU);
                                    regv.RegU = null;
                                }
                                else continue;
                            }

                            //se la reg_V ha la data valorizzata
                            if (regv.Data_Reg.HasValue)
                                toElaborateDates.Add(regv.Data_Reg.Value.Date);

                            //se il collaboratore della reg_v è valido
                            if (regv.Col_Id != 0 && regv.Col_Id != null && !colIds.Contains(regv.Col_Id.Value))
                                colIds.Add(regv.Col_Id.Value);

                            Reg newRegE = RepoManager.RegRepo.Init();
                            Reg oldRegE = RepoManager.RegRepo.Single(reg => reg.Reg_Id == regv.RegE);

                            // inizializzazione del valore che conterrà l'id della registrazione in entrata
                            int regEId = regv.RegE;

                            // inizializzazione delle date/ore originali
                            DateTime dataOrigE = DateTime.MinValue;
                            DateTime dataOrigU = DateTime.MinValue;

                            //se la "vecchia" REG ha una data diversa da null
                            if (oldRegE.Registrazione_Data_Ora_Fis_Reg != null)
                                toElaborateDates.Add(oldRegE.Registrazione_Data_Ora_Fis_Reg.Date);

                            //se la REG ha un valore di entarata valido
                            if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_E != null && regv.Data_Ora_Fis_E != DateTime.MinValue)
                            {
                                //la nuova registrazione da aggiornare avrà la data della registrazione che si stà processando
                                newRegE.Registrazione_Data_Ora_Fis_Reg = new DateTime(regv.Data_Reg.Value.Year, regv.Data_Reg.Value.Month, regv.Data_Reg.Value.Day, regv.Data_Ora_Fis_E.Hour, regv.Data_Ora_Fis_E.Minute, regv.Data_Ora_Fis_E.Second);
                            }

                            //viene costruita la nuova registrazione da aggioranre con i parametri della vecchia registrazione
                            newRegE.Flag_EU_Reg = regv.EntrataEU;

                            newRegE.Cant_Id = regv.Cant_Id;
                            newRegE.Col_Id = regv.Col_Id;
                            newRegE.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;
                            newRegE.Registrazione_Data_Ora_Fig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                            dataOrigE = oldRegE != null ? oldRegE.Registrazione_Data_Ora_Orig_Reg : newRegE.Registrazione_Data_Ora_Orig_Reg;
                            newRegE.Fru_Id = regv.Fru_Id;
                            newRegE.Pru_Id = regv.Pru_Id;
                            newRegE.Custom_Data_Reg = oldRegE != null ? oldRegE.Custom_Data_Reg : null;
                            newRegE.Registrazione_Tipo_Reg = oldRegE.Registrazione_Tipo_Reg;
                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.MantainCoordinateModifiedRegs) == 1) {
                                newRegE.Registrazione_Lat_Orig = oldRegE.Registrazione_Lat_Orig;
                                newRegE.Registrazione_Long_Orig = oldRegE.Registrazione_Long_Orig;
                            }

                            // se è stato modificato il cantiere della registrazione allora si svuota anche la matricola unità fissa
                            RepoManager.RegRepo.ManageCantColChangesBeforeUpdate(newRegE, oldRegE);

                            //se la "vecchia" registrazione è un'attività anche la nuova registrazione sarà un'attività
                            if (oldRegE.Registrazione_Tipo_Reg == (int)RegTypeEnum.Att || oldRegE.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration)
                                newRegE.Registrazione_Tipo_Reg = oldRegE.Registrazione_Tipo_Reg;


                            //viene recuperato il flag se la registrazione è bloccata oppure no dalla registrazione che si stà processando
                            newRegE.Registrazione_Bloccata = regv.Registrazione_Bloccata;

                            //viene recuperata la valutazione dell'attività
                            newRegE.Activity_Evaluation = regv.Activity_Evaluation;
                            if (oldRegE != null)
                            {
                                newRegE.Tipo_Attivita = oldRegE.Tipo_Attivita;
                                newRegE.Turno = oldRegE.Turno;
                                newRegE.Sotto_Cantiere = oldRegE.Sotto_Cantiere;
                            }

                            validationErrors = RepoManager.RegRepo.Check(newRegE);

                            //se si hannod egli errori si passa alla reg successiva
                            if (validationErrors.Count > 0)
                                break;

                            //viene aggiornata la nuova reg ed eliminata quella vecchia
                            toUpdateRegs.Add(newRegE);
                            toDeleteRegs.Add(oldRegE);

                            //viene
                            Reg newRegU = null;

                            // inizializzazione del valore di memorizzazione del regUId
                            int regUId = 0;

                            //se l'uscita della regv ha un valore null
                            if (regv.Data_Ora_Fis_U != null && regv.Data_Ora_Fis_U != DateTime.MinValue)
                            {
                                //viene inizializzata una nua reg
                                newRegU = RepoManager.RegRepo.Init();
                                Reg oldRegU = null;

                                //se la reg che stò preocessando ha un uscita valida
                                if (regv.RegU != 0)
                                {
                                    //viene recuperato l'id dalla registrazione vecchia
                                    oldRegU = RepoManager.RegRepo.Single(reg => reg.Reg_Id == regv.RegU);
                                    regUId = oldRegU.Reg_Id;
                                }

                                //se la registrazione ha una data valida
                                if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_U != null && regv.Data_Ora_Fis_U != DateTime.MinValue)
                                {
                                    //la data della reg di uscita corrisponde alla data della reg che si stà processando
                                    var regUDate = regv.Data_Reg.Value.Date;

                                    //se il notturno è abilitato
                                    if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno &&
                                        RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.None && RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.Disabled)
                                    {
                                        //se la data dell'uscita è minore di quella dell'entrata (es U=05:00 E=22:00)
                                        if (regv.Data_Ora_Fis_U.Value.Hour < regv.Data_Ora_Fis_E.Hour &&
                                            regv.Data_Ora_Fis_U.Value.Date > regUDate.Date)

                                            // viene aggiunto un giorno alla registrazione dell'uscita solamente se non si è all'interno dello stesso giorno
                                            if (!regv.IsUTimeSameDayE)
                                                regUDate = regUDate.AddDays(1);
                                    }

                                    //se il notturno non è abilitato
                                    else
                                    {
                                        if (oldRegU != null)
                                            regUDate = regv.Data_Ora_Fis_U.Value.Date;

                                    }

                                    newRegU.Registrazione_Data_Ora_Fis_Reg = new DateTime(regUDate.Year, regUDate.Month, regUDate.Day, regv.Data_Ora_Fis_U.Value.Hour, regv.Data_Ora_Fis_U.Value.Minute, regv.Data_Ora_Fis_U.Value.Second);
                                }

                                newRegU.Cant_Id = regv.Cant_Id;
                                newRegU.Col_Id = regv.Col_Id;
                                newRegU.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;
                                dataOrigU = oldRegU != null ? oldRegU.Registrazione_Data_Ora_Orig_Reg : newRegU.Registrazione_Data_Ora_Orig_Reg;
                                newRegU.Registrazione_Data_Ora_Fig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;

                                newRegU.Flag_EU_Reg = regv.UscitaEU;
                                //newRegU.RiferimentoRRN_Reg = newRegE.Reg_Id;

                                newRegU.Activity_Evaluation = regv.Activity_Evaluation;

                                if (oldRegU != null)
                                {
                                    newRegU.Fru_Id = oldRegU.Fru_Id;
                                    newRegU.Pru_Id = oldRegU.Pru_Id;
                                    //newRegU.Custom_Data_Reg = oldRegU.Custom_Data_Reg;

                                    newRegU.Tipo_Attivita = oldRegU.Tipo_Attivita;
                                    newRegU.Turno = oldRegU.Turno;
                                    newRegU.Sotto_Cantiere = oldRegU.Sotto_Cantiere;

                                    // se è stato modificato il cantiere della registrazione allora si svuota anche la matricola unità fissa
                                    RepoManager.RegRepo.ManageCantColChangesBeforeUpdate(newRegU, oldRegU);
                                }
                                else
                                {
                                    newRegU.Fru_Id = null;
                                    newRegU.Pru_Id = null;
                                }

                                newRegU.ParentReg = newRegE;

                                newRegU.DisAbilitazione_Reg = false;

                                newRegU.Registrazione_Bloccata = regv.Registrazione_Bloccata;

                                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.MantainCoordinateModifiedRegs) == 1)
                                {
                                    newRegU.Registrazione_Lat_Orig = oldRegU.Registrazione_Lat_Orig;
                                    newRegU.Registrazione_Long_Orig = oldRegU.Registrazione_Long_Orig;
                                }

                                validationErrors = RepoManager.RegRepo.Check(newRegU);

                                if (validationErrors.Count > 0)
                                    break;

                                toUpdateRegs.Add(newRegU);
                                if (oldRegU != null)
                                    toDeleteRegs.Add(oldRegU);
                            }
                            else
                            {
                                var oldRegU = RepoManager.RegRepo.SingleOrDefault(rv => rv.Reg_Id == regv.RegU);
                                if (oldRegU != null)
                                    toDeleteRegs.Add(oldRegU);
                            }

                            // gestione delle reg bloccate da accoppiare
                            RepoManager.RegRepo.PerformBlockedRegsCouple(newRegE, newRegU, regv.RegE);

                            // è calcolato il fatto che si sta elaborando un'uscita nello stesso giorno
                            bool isSameDay = true;
                            // se non c'è il notturno è sicuramente true
                            if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight || RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.Duration)
                                isSameDay = regv.IsUTimeSameDayE;

                            // gestione delle date/ore originali
                            RepoManager.RegRepo.ManageOrigDates(newRegE, regEId, dataOrigE, newRegU, regUId, dataOrigU, isSameDay);
                        }
                    }
                    #endregion

                    #region Deleting erased Regs

                    // si cancellano solamente le reg_v che non sono state inserite in questa sessione e hanno la data/ora fisica d'entrata a min value
                    // e la data ora fisica d'uscita a null
                    var regVsToDelete = newRegVs.Where(regV => regV.RegE != 0 && regV.Data_Ora_Fis_E == DateTime.MinValue && regV.Data_Ora_Fis_U == null);

                    // inizializzazione della lista di reg cancellare
                    List<Reg> regsToDelete = new List<Reg>();

                    // ciclo di elaborazione delle reg_v da eliminare
                    foreach (var regVToDelete in regVsToDelete)
                    {
                        // viene verificata la possibilità di poter cancellare il record
                        Dictionary<string, string> errorDic = RepoManager.Reg_VRepo.CheckBeforeDelete(regVToDelete);
                        if (!errorDic.Any()) // se non è presente qualche errore allora procedo alla cancellazione, altrimenti segnalo l'errore
                        {
                            // viene aggiunta alla lista di reg da cancellare la reg in entrata
                            var regEToDelete = RepoManager.RegRepo.SingleOrDefault(reg => reg.Reg_Id == regVToDelete.RegE);
                            if (regEToDelete != null)
                                regsToDelete.Add(regEToDelete);


                            // se è presente una reg in uscita allora viene aggiunta alle reg cancellare
                            if (regVToDelete.RegU != null)
                            {
                                var regUToDelete = RepoManager.RegRepo.SingleOrDefault(reg => reg.Reg_Id == regVToDelete.RegU);
                                if (regUToDelete != null)
                                    regsToDelete.Add(regUToDelete);
                            }

                            // se la data reg non è già presente all'interno delle date da elaborare, viene aggiunta
                            if (!toElaborateDates.Contains(regVToDelete.Data_Reg.Value.Date))
                                toElaborateDates.Add(regVToDelete.Data_Reg.Value.Date);

                            // il collaboratore da processare viene eventualmente aggiunto alla lista se non presente
                            if (regVToDelete.Col_Id != 0 && regVToDelete.Col_Id != null && !colIds.Contains(regVToDelete.Col_Id.Value))
                                colIds.Add(regVToDelete.Col_Id.Value);
                        }
                        else
                            validationErrors.AddRange(errorDic);
                    }

                    // cancellazione delle reg calcolate (se presenti)
                    if (regsToDelete.Count > 0)
                        RepoManager.RegRepo.Delete(regsToDelete, true);

                    #endregion

                    #region Elaborate All Modified Regs

                    if (validationErrors.Count == 0)
                    {
                        List<Reg> toElaborateTotalRegs = new List<Reg>();

                        RepoManager.RegRepo.Delete(toDeleteRegs, true);

                        toAddRegs.AddRange(toUpdateRegs);

                        RepoManager.RegRepo.Add(toAddRegs, true);

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

                            // gestione delle date di inizio/fine periodo in base alla configurazione del notturno
                            BusinessService.ManageNocturneStartEndDate(ref startElabDate, ref endElabDate);

                            //leggo Tutte le REG NEW Necessarie alla Successiva ELABORATE
                            List<Reg> toElaborateRegs = RepoManager.RegRepo.Find(reg => reg.Col_Id.HasValue && colIds.Contains(reg.Col_Id.Value) &&
                                                                                         (reg.Registrazione_Data_Ora_Fis_Reg >= startElabDate &&
                                                                                         reg.Registrazione_Data_Ora_Fis_Reg < endElabDate)).ToList();

                            toElaborateTotalRegs.AddRange(toElaborateRegs);
                        }

                        //lancio la Ri_elaborazione delle REG da trattare
                        RepoManager.RegRepo.Elaborate(toElaborateTotalRegs, startElabDate, endElabDate);

                    }

                    #endregion

                }

                #region Gestione errori e ritorno dei valori

                IsToShowEditGrid = (validationErrors.Count > 0);
                if (IsToShowEditGrid)
                    EditErrorMessage = CommonService.StringFromDictionary(validationErrors);
                else
                {
                    PowerWebContext.SetToSession<List<Col>>("EditRegVs_" + gvRegVMEdit.ID, null);
                    PowerWebContext.SetToSession<List<Col>>("AllEditRegVs_" + gvRegVMEdit.ID, null);

                    Bind_gvRegVMEdit(true);
                }

                #endregion
            }
            else if (e.Parameters == "add")
            {
                IsToShowEditGrid = true;
                EditErrorMessage = null;

                // prima di effettuare l'aggiunta della riga allora ricalcolo il data source con i dati modificati
                // e svuoto l'elenco delle nuove regV
                generateEditedRegVs(false);
                EditRegVs = newRegVs;
                newRegVs = null;

                Reg_V newRegV = RepoManager.Reg_VRepo.Init();
                newRegV.Data_Reg = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day);
                newRegV.Col_Id = EditRegVs.Where(regv => regv.RegE != 0).Select(regv => regv.Col_Id).FirstOrDefault();
                newRegV.Cant_Id = EditRegVs.Where(regv => regv.RegE != 0).Select(regv => regv.Cant_Id).FirstOrDefault();
                //newRegV.Cant_Id = null;
                newRegV.Registrazione_Stato_Reg = (int)RegStateEnum.None;
                newRegV.Registrazione_Tipo_Reg = (int)RegTypeEnum.None;

                // la nuove righe sono trattate come manuali
                newRegV.Tipo_Modifica = (int)RegModifyTypeEnum.Manual;

                // le nuove regv hanno come data ora entrata fisica il valore presente nell'ultima del data source (se contiene qualcosa)
                if (EditRegVs.Any())
                    newRegV.Data_Ora_Fis_E = EditRegVs.Last().Data_Ora_Fis_E;

                if (EditRegVs != null)
                {
                    EditRegVs.Add(newRegV);
                }
                else EditRegVs = new List<Reg_V>() { newRegV };

                Bind_gvRegVMEdit();
            }
            else if (e.Parameters == "empty")
            {
                IsToShowEditGrid = true;
                EditErrorMessage = null;

                PowerWebContext.SetToSession<List<Col>>("EditRegVs_" + gvRegVMEdit.ID, null);

                Reg_V newRegV = RepoManager.Reg_VRepo.Init();
                newRegV.Data_Reg = DateTime.Now;

                EditRegVs = new List<Reg_V>() { newRegV };

                Bind_gvRegVMEdit();
            }
            else if (e.Parameters == "undo")
            {
                PowerWebContext.SetToSession<List<Col>>("EditRegVs_" + gvRegVMEdit.ID, null);

                Bind_gvRegVMEdit();
            }
            else if (e.Parameters == "includeReg")
            {
                showCurrentEditRegVs();
            }
        }

        /// <summary>
        /// In base ai checkbox  selezionati vengono escluse alcune registrazioni
        /// </summary>
        private void showCurrentEditRegVs()
        {
            //se vi sono delle regv 
            if (AllEditRegVs != null && AllEditRegVs.Count > 0)
            {
                //toShowRegVs rappresenta le registrazioni da visulaizzare
                var toShowRegVs = AllEditRegVs.ToList();

                //se NON è richiesto di includere le attività dal checkbox sopra la griglia
                if (!cbIncludeActivity.Checked)
                    //vengono rimosse le attività
                    toShowRegVs.RemoveAll(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Att);

                //se NON è richiesto di mostrarre i passaggi
                if (!cbIncludePass.Checked)
                    toShowRegVs.RemoveAll(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass);

                //se NON è richiesto di mostrarre i viaggi
                if (!cbIncludeTrips.Checked)
                    toShowRegVs.RemoveAll(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip);

                ////se NON è richiesto di mostrarre le registrazioni bloccate
                if (!cbIncludeBlocked.Checked)
                    toShowRegVs.RemoveAll(regv => regv.Registrazione_Bloccata);

                //rappresenta le registrazioni da visualizzare
                EditRegVs = toShowRegVs;
            }

            //si costruisce il binding della griglia
            Bind_gvRegVMEdit();

        }



        /// <summary>
        /// Setta le etichette del collaboratore e data sopra la griglia di edit multiplo
        /// </summary>
        private void setColAndDataLabel()
        {
            if (EditRegVs != null && EditRegVs.Count > 0)
            {
                var sampleRegVs = EditRegVs.First();

                CurrentColId = sampleRegVs.Col_Mnemonic + " " + sampleRegVs.Col_Desc;
                CurrentDataReg = EditRegVs.First().Data_Reg.Value.ToShortDateString();
            }
        }

        protected void gvRegVMEdit_DataBinding(object sender, EventArgs e)
        {
            Bind_gvRegVMEdit(false);
        }

        /// <summary>
        /// Effettua il bind della griglia della modifica multipla
        /// </summary>
        /// <param name="isToBindData"> variabile boolena che indica se effettuare il data bind o no</param>
        private void Bind_gvRegVMEdit(Boolean isToBindData = true)
        {
            gvRegVMEdit.KeyFieldName = KEYFIELDNAME;
            gvRegVMEdit.DataSource = EditRegVs;

            if (isToBindData)
            {
                gvRegVMEdit.DataBind();
            }

            setColAndDataLabel();
        }

        //Gestione ottimizzata componente calendario in griglia di Edit multiplo
        protected void de_Init(object sender, EventArgs e)
        {
            ASPxDateEdit deData_Fig_Reg = sender as ASPxDateEdit;
            deData_Fig_Reg.PopupCalendarOwnerID = "__ReferenceDateEdit";

            if (PowerWebContext.Current.User != null && EditRegVs != null)
                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomerOnlyMultipleEditEvaluationEnum) == (int)CustomerOnlyMultipleEditEvaluationEnum.Enabled &&
                    PowerWebContext.Current.User.Cli_Id.HasValue)
                {
                    deData_Fig_Reg.ClientEnabled = false;
                    deData_Fig_Reg.Enabled = false;
                }
        }

        #region gestione ComboBox in griglia di Edit multiplo

        protected void cbmxCant_Id_Init(object sender, EventArgs e)
        {
            cbmx_Init(sender, e, CommonService.GetPropertyName(() => _regVStub.Cant_Id));
        }

        protected void cbmxCentroDiCosto_Id_Init(object sender, EventArgs e)
        {
            cbmx_Init(sender, e, CommonService.GetPropertyName(() => _regVStub.CentroDiCosto_Id));
        }

        protected void cmbMotivazione_Reg_Id(object sender, EventArgs e)
        {
            //il sender è castato come  ASPxComboBox
            ASPxComboBox cmbx = sender as ASPxComboBox;

            cbmx_Init(cmbx, e, CommonService.GetPropertyName(() => _regVStub.Motivazione_Reg_Id));

            //viene aggiunto il bottone di cancellazione
            EditButton btnEdit = new EditButton("X");
            cmbx.Buttons.Add(btnEdit);

            //alla pressione del bottone di cancellzione viene richiamato il metodo a livello client che permette di cancellare il valore selezionato del combobox
            cmbx.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";

        }

        protected void cbActivity_Evaluation_Init(object sender, EventArgs e)
        {
            //il sender è castato come  ASPxComboBox
            ASPxComboBox cmbx = sender as ASPxComboBox;

            cbmx_Init(cmbx, e, CommonService.GetPropertyName(() => _regVStub.Activity_Evaluation));

            //se non è attiva la personalizzazione di gestione della valutazione delle attività
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomerOnlyMultipleEditEvaluationEnum) == (int)CustomerOnlyMultipleEditEvaluationEnum.Disabled)
                //viene disabilitato il combobox
                cmbx.Enabled = false;

            //viene aggiunto il bottone di cancellazione
            EditButton btnEdit = new EditButton("X");
            cmbx.Buttons.Add(btnEdit);

            //alla pressione del bottone di cancellzione viene richiamato il metodo a livello client che permette di cancellare il valore selezionato del combobox
            cmbx.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
        }

        private void cbmx_Init(object sender, EventArgs e, string fieldName)
        {
            ASPxComboBox cmbx = sender as ASPxComboBox;
            if (cmbx != null)
            {
                PowerWebService.FillComboboxes(cmbx, fieldName);

                if (PowerWebContext.Current.User != null && EditRegVs != null && fieldName != CommonService.GetPropertyName(() => _regVStub.Activity_Evaluation))
                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomerOnlyMultipleEditEvaluationEnum) == (int)CustomerOnlyMultipleEditEvaluationEnum.Enabled &&
                        PowerWebContext.Current.User.Cli_Id.HasValue)
                    {
                        cmbx.ClientEnabled = false;
                        cmbx.Enabled = false;
                    }
            }
        }

        #endregion

        protected void gvRegVMEdit_AfterPerformCallback(object sender, ASPxGridViewAfterPerformCallbackEventArgs e)
        {
            ASPxGridView gv = (ASPxGridView)sender;
            gv.JSProperties.Add("cpIsToShowEditGrid", false);
            gv.JSProperties["cpIsToShowEditGrid"] = IsToShowEditGrid;
            gv.JSProperties.Add("cpErrorString", null);
            if (EditErrorMessage != null && EditErrorMessage != "")
                gv.JSProperties["cpErrorString"] = EditErrorMessage;

            gv.JSProperties.Add("cpCurrentColId", "");
            if (CurrentColId != null)
                gv.JSProperties["cpCurrentColId"] = CurrentColId;
            gv.JSProperties.Add("cpCurrentData_Reg", "");
            if (CurrentDataReg != null)
                gv.JSProperties["cpCurrentData_Reg"] = CurrentDataReg;
        }

        /// <summary>
        ///Metodo chiamato nella crezione della griglia di edit multiplo
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gvRegVMEdit_OnInit(object sender, EventArgs e)
        {
            // Recupero della griglia
            ASPxGridView editGrid = (ASPxGridView)sender;

            // recupero della colonna da processare
            GridViewColumn sameDayColumn = editGrid.Columns[CommonService.GetPropertyName(() => _regVStub.IsUTimeSameDayE)];

            // recupero della colonna di cui adattare la percentuale di spazio
            GridViewColumn regTypeColumn = editGrid.Columns[CommonService.GetPropertyName(() => _regVStub.Registrazione_Tipo_Reg)];

            // se è abilitato il notturno allora viene visualizzata la colonna di selezione del giorno intero;
            // in caso contrario la colonna viene nascosta
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && (RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight || RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.Duration))
            {
                sameDayColumn.Visible = true;
                regTypeColumn.Width = Unit.Percentage(15);
            }
            else
            {
                sameDayColumn.Visible = false;
                regTypeColumn.Width = Unit.Percentage(10);
            }

            // recupero delle colonne di visualizzazione del flag E/U
            GridViewColumn inEuColumn = editGrid.Columns[CommonService.GetPropertyName(() => _regVStub.EntrataEU)];
            GridViewColumn outEuColumn = editGrid.Columns[CommonService.GetPropertyName(() => _regVStub.UscitaEU)];

            // recupero delle colonne da ridimensionare in caso di visualizzazione dei dati di E/U
            GridViewColumn justificationColumn = editGrid.Columns[CommonService.GetPropertyName(() => _regVStub.Motivazione_Reg_Id)];

            GridViewColumn cantIdColumn = editGrid.Columns[CommonService.GetPropertyName(() => _regVStub.Cant_Id)];

            // si procede alla visibilità delle colonne se è attiva la personalizzazione per la visione delle colonne E/U
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowFlagEUInFormEnum) == (int)ShowFlagEUInFormEnum.Show)
            {
                // si visualizzano le colonne di entrata e uscita
                inEuColumn.Visible = true;
                outEuColumn.Visible = true;

                // si ridimensionano le colonne utili a far spazio a quelle specificate
                justificationColumn.Width = Unit.Percentage(15);
                cantIdColumn.Width = Unit.Percentage(25);
            }

        }

        /// <summary>
        ///metodo che si occupa di inserire i dati nelle celle della girlgia
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxGridViewTableDataCellEventArgs"/> instance containing the event data.</param>
        protected void gvRegVMEdit_HtmlDataCellPrepared(object sender, ASPxGridViewTableDataCellEventArgs e)
        {
            Reg_V regvStub = null;

            var tipoReg = Convert.ToInt32(gvRegVMEdit.GetCurrentPageRowValues(CommonService.GetPropertyName(() => regvStub.Registrazione_Tipo_Reg))[e.VisibleIndex]);

            if (tipoReg == (int)RegTypeEnum.Trip || tipoReg == (int)RegTypeEnum.ArrotDur || tipoReg == (int)RegTypeEnum.RettTimeSheetManual)
            {
                e.Cell.Enabled = false;
            }


            if (e.DataColumn.FieldName == CommonService.GetPropertyName(() => regvStub.Data_Ora_Fis_E) || e.DataColumn.FieldName == CommonService.GetPropertyName(() => regvStub.Data_Ora_Fis_ETime))
            // si colora l'ora di entrata fisica in bas al tipo modifica apportata
            {
                var checkIndex = e.VisibleIndex - gvRegVMEdit.VisibleStartIndex;

                // verifico la presenza della customizzazione riguardante la non colorazione delle attività
                int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoColorForEditedActivityEnum);

                // recupero il valore di tipo della registrazione in elaborazione
                var listaTipiReg = gvRegVMEdit.GetCurrentPageRowValues(CommonService.GetPropertyName(() => regvStub.Registrazione_Tipo_Reg));
                int registrazioneTipoReg = (int)RegTypeEnum.None;

                if (listaTipiReg.Count > 0 && checkIndex >= 0 && checkIndex <= listaTipiReg.Count)
                {
                    registrazioneTipoReg = Convert.ToInt32(listaTipiReg[checkIndex]);
                }

                // si colora la cella solamente se non è un'attività o se è attiva la colorazione
                if (registrazioneTipoReg != (int)RegTypeEnum.Att || customizationVersion == (int)NoColorForEditedActivityEnum.Color)
                {
                    // se si sta processando un viaggio allora le ore assumono il colore del tipo registrazione
                    if (registrazioneTipoReg == (int)RegTypeEnum.Trip || registrazioneTipoReg == (int)RegTypeEnum.ArrotDur)
                    {
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)registrazioneTipoReg, false);
                    }
                    else // in caso contrario si procede al check del tipo modifica
                    {
                        // viene recuperato il valore del campo tipo modifica dalla griglia per la riga alla posizione della ccella in elaborazione
                        var listaTipiModifica = gvRegVMEdit.GetCurrentPageRowValues(CommonService.GetPropertyName(() => regvStub.Tipo_Modifica));
                        if (listaTipiModifica.Count > 0 && checkIndex >= 0 && checkIndex <= listaTipiModifica.Count)
                        {
                            var tipoModifica = Convert.ToInt32(listaTipiModifica[checkIndex]);


                            ASPxDateEdit timeEdit = gvRegVMEdit.FindRowCellTemplateControl(e.VisibleIndex, e.DataColumn, "teData_Ora_Fis_E") as ASPxDateEdit;

                            var time = gvRegVMEdit.GetCurrentPageRowValues(CommonService.GetPropertyName(() => regvStub.Data_Ora_Fig_ETime));

                            //in base al tipo di modifica e al tipo di reg mi restituisce se sono in fase di modifica o manuale
                            var colorModify = BusinessService.ColorModify((RegModifyTypeEnum)tipoModifica, RegEUEnum.Entry);

                            timeEdit.ForeColor = RepoManager.ParamRepo.GetColorFromEnum(colorModify, false);


                            // se è stata modificata l'entrata o entrambe le ore allora il colore viene impostatao a rosso
                            //if (tipoModifica == (int)RegModifyTypeEnum.Entry_Modified || tipoModifica == (int)RegModifyTypeEnum.Both || tipoModifica == (int)RegModifyTypeEnum.E_Mod_U_Man)
                            //{

                            //    //(RegModifyTypeEnum)tipoModifica,false);


                            //}
                            //// se la registrazione è completamente manuale allora il colore viene impostato a blu
                            //else if (tipoModifica == (int)RegModifyTypeEnum.Manual || tipoModifica == (int)RegModifyTypeEnum.E_Man_U_Mod || tipoModifica == (int)RegModifyTypeEnum.Entry_Manual)
                            //{
                            //    timeEdit.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegModifyTypeEnum)tipoModifica, false);
                            //}
                        }
                    }
                }
            }
            else if (e.DataColumn.FieldName == CommonService.GetPropertyName(() => regvStub.Data_Ora_Fis_U) || e.DataColumn.FieldName == CommonService.GetPropertyName(() => regvStub.Data_Ora_Fis_UTime))
            // si colora l'ora di uscita fisica in bas al tipo modifica apportata
            {
                var checkIndex = e.VisibleIndex - gvRegVMEdit.VisibleStartIndex;

                // verifico la presenza della customizzazione riguardante la non colorazione delle attività
                int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoColorForEditedActivityEnum);

                // recupero il valore di tipo della registrazione in elaborazione
                var listaTipiReg = gvRegVMEdit.GetCurrentPageRowValues(CommonService.GetPropertyName(() => regvStub.Registrazione_Tipo_Reg));
                int registrazioneTipoReg = (int)RegTypeEnum.None;

                if (listaTipiReg.Count > 0 && checkIndex >= 0 && checkIndex <= listaTipiReg.Count)
                {
                    registrazioneTipoReg = Convert.ToInt32(listaTipiReg[checkIndex]);
                }

                // si colora la cella solamente se non è un'attività o se è attiva la colorazione
                if (registrazioneTipoReg != (int)RegTypeEnum.Att || customizationVersion == (int)NoColorForEditedActivityEnum.Color)
                {
                    // se si sta processando un viaggio allora le ore assumono il colore del tipo registrazione
                    if (registrazioneTipoReg == (int)RegTypeEnum.Trip || registrazioneTipoReg == (int)RegTypeEnum.ArrotDur)
                    {
                        e.Cell.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegTypeEnum)registrazioneTipoReg, false);
                    }
                    else // in caso contrario si procede al check del tipo modifica
                    {
                        // viene recuperato il valore del campo tipo modifica dalla griglia per la riga alla posizione della ccella in elaborazione
                        var listaTipiModifica = gvRegVMEdit.GetCurrentPageRowValues(CommonService.GetPropertyName(() => regvStub.Tipo_Modifica));
                        if (listaTipiModifica.Count > 0 && checkIndex >= 0 && checkIndex <= listaTipiModifica.Count)
                        {
                            var tipoModifica = Convert.ToInt32(listaTipiModifica[checkIndex]);

                            ASPxDateEdit timeEdit = gvRegVMEdit.FindRowCellTemplateControl(e.VisibleIndex, e.DataColumn, "teData_Ora_Fis_U") as ASPxDateEdit;

                            //in base al tipo di modifica e al tipo di reg mi restituisce se sono in fase di modifica o manuale
                            var colorModify = BusinessService.ColorModify((RegModifyTypeEnum)tipoModifica, RegEUEnum.Exit);

                            timeEdit.ForeColor = RepoManager.ParamRepo.GetColorFromEnum(colorModify, false);

                            //// se è stata modificata l'uscita o entrambe le ore allora il colore viene impostatao a rosso
                            //if (tipoModifica == (int)RegModifyTypeEnum.Exit_Modified || tipoModifica == (int)RegModifyTypeEnum.Both || tipoModifica == (int)RegModifyTypeEnum.E_Man_U_Mod)
                            //{
                            //    timeEdit.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegModifyTypeEnum)tipoModifica, false);
                            //}
                            //// se la registrazione è completamente manuale allora il colore viene impostato a blu
                            //else if (tipoModifica == (int)RegModifyTypeEnum.Manual || tipoModifica == (int)RegModifyTypeEnum.E_Mod_U_Man || tipoModifica == (int)RegModifyTypeEnum.Exit_Manual)
                            //{
                            //    timeEdit.ForeColor = RepoManager.ParamRepo.GetColorFromEnum((RegModifyTypeEnum)tipoModifica, false);
                            //}
                        }
                    }
                }

            }
        }

        #endregion

        #region Export Excel

        public List<Tab_Excel_Model> Models
        {
            get
            {
                var typeName = typeof(Reg_V).Name;
                return RepoManager.Tab_Excel_ModelRepo.Find(xlsxModel => xlsxModel.IsActive && xlsxModel.Nome_Entity == typeName).ToList();
            }
        }

        public void ExportXLSX(Tab_Excel_Model model, List<object> items)
        {
            string path = "";

            List<Reg_V> regs = CommonService.ConvertTo<Reg_V>(items);

            var currentType = Type.GetType(String.Format("Exports.{0}, Exports", model.Nome_Specializzato));

            if (currentType != null)
            {

                if (currentType == typeof(ExportTxtZucchettiSolaris))
                {
                    var export = new ExportTxtZucchettiSolaris(regs.Min(reg => reg.Data_Ora_Fis_E), (DateTime)(regs.Max(reg => reg.Data_Ora_Fis_U) ?? regs.Min(reg => reg.Data_Ora_Fis_E)));
                    export.LaunchExport();
                    BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = true;
                    export.ExportToResponse();
                }
                if (currentType == typeof(ExportExcelSpecializedExtendedReg_V))
                {
                    var currentSpecialized = (IExportExcelSpecialized<Reg_V>)Activator.CreateInstance(currentType);
                    ExportExcelEngine.Export<Reg_V>(currentSpecialized, regs, model, out path);
                }

                if (currentType == typeof(ExportExcelGenericReg_V))
                {
                    IEnumerable<Tuple<string, string, string>> visibleFields = null;

                    var currentSpecialized = (IExportExcelSpecialized<Reg_V>)Activator.CreateInstance(currentType);

                    //vengono estratti i campi da visualizzare nell'export
                    visibleFields = PowerWebService.GetGridViewVisibleFields(gvRegVM);

                    //viene generato l'export passando come parametro le regs, il modello sul quale scrivere e i campi da scrivere
                    ExportExcelEngine.Export(currentSpecialized, regs, model, out path, visibleFields);
                }

                if (currentType == typeof(ExportExcelSpecializedActivity))
                {
                    var list = BusinessService.PopulateActivityList(regs);

                    var currentSpecialized = (IExportExcelSpecialized<ActivityItem>)Activator.CreateInstance(currentType);
                    ExportExcelEngine.Export(currentSpecialized, list, model, out path);
                }

            }
        }


        #endregion

        protected void pcShowMap_Init(object sender, EventArgs e)
        {

            if (!GridView.JSProperties.ContainsKey("cpFilterError"))
                GridView.JSProperties.Add("cpFilterError", "");

            /*      VISUALIZZAZIONE DELLA MAPPA SOLO SE SONO VALORIZZATI I FILTRI SU COLLABORATORE E DATA       */

            // Recupera la customization che indica se richiedere l'utilizzo del filtro su collaboratore e data per aprire il popup della mappa nel modulo delle timbrature multiple
            int filterOnMap = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.RequestFiltersOnMapClick);
            // Se sono nella 
            if (filterOnMap == (int)RequestFiltersOnMapClick.Request)
            {
                var columnCol_Id = GridView.Columns["Col_Id"] as GridViewDataColumn;
                bool resCol_Id = columnCol_Id.FilterExpression != String.Empty && columnCol_Id.FilterExpression.Contains("=");

                var columnData_Reg = GridView.Columns["Data_Reg"] as GridViewDataColumn;
                bool resData_Reg = columnData_Reg.FilterExpression != String.Empty && columnData_Reg.FilterExpression.Contains("=");

                if (resCol_Id && resData_Reg)
                {

                }

                else
                {
                    GridView.JSProperties["cpFilterError"] = "Selezionare un collaboratore e una data nel filtro.";
                }

            }
        }

        protected void standardEditControl_Init(object sender, EventArgs e)
        {
            ASPxEdit ctrl = sender as ASPxEdit;

            if (ctrl != null)
            {
                if (PowerWebContext.Current.User != null && EditRegVs != null)
                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomerOnlyMultipleEditEvaluationEnum) == (int)CustomerOnlyMultipleEditEvaluationEnum.Enabled &&
                        PowerWebContext.Current.User.Cli_Id.HasValue)
                    {
                        ctrl.ClientEnabled = false;
                        ctrl.Enabled = false;
                    }
            }
        }

    }

}
