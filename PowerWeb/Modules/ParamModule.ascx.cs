using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Business.ExportExcelEngine;
using DevExpress.Web.ASPxCallback;
using Exports.ExportExcelGeneric;
using log4net;
using Domain;
using Business.Repository;
using DevExpress.Web.Data;
using Common;
using Business;
using DevExpress.Web.ASPxGridView;
using Reports;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxFormLayout;
//using Business.BusinessServices.ParamService;

// Pay Attention!!! In this page Session is readONLY!!!!!!!!!!
namespace PowerWeb.Modules
{
    public partial class ParamModule : BaseGridModule, IPrintModule, ILogModule, IExportXLSXModule
    {
        //DEFINIZIONI
        const String KEYFIELDNAME = "Param_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(ParamModule));

        //IParamService _paramService;

        public override ASPxGridView GridView
        {
            get { return gvParam; }
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

        public bool IsInBatchMode
        {
            get { return GridView.SettingsEditing.Mode == GridViewEditingMode.Batch; }
        }

        public override ASPxGridView GridViewDetail
        {
            get { return null; }
        }

        public override PowerFormTemplate EditFormTemplate
        {
            get
            {
                PowerFormTemplate template = PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID);
                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryParam();
                    template = new PowerFormTemplate(this, templateDic);
                    PowerWebContext.SetToSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID, template);
                }
                return template;
            }
        }

        public override PowerFormTemplate EditDetailFormTemplate
        {
            get { return null; }
        }

        //public ParamModule() : this(new ParamService())
        //{

        //}

        //public ParamModule(IParamService paramService)
        //{
        //    _paramService = paramService;
        //}

        protected void Page_Init(object sender, EventArgs e)
        {            
            //Inizializzo i testi delle Label ed dei Bottoni
            btnConfirmBreakRegDate.Text = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNAGGIORNA);
            btnConfirmStoredRegDate.Text = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNAGGIORNA);
            lblOldBreakRegDate.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_DATA_BLOCCO_PRECEDENTE);
            lblNewBreakRegDate.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_DATA_BLOCCO_NUOVA);
            btnConfirmBreakRegDate.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_DATA_BLOCCO_AGGIORNA);
            flChangeBreakRegDate.Items[0].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_LAYOUTGROUP_CAMBIO_DATA_REG);
            ((LayoutGroup)flChangeBreakRegDate.Items[0]).Items [0].Caption = BusinessService.GetLocalizedString(PowerWebResources.LBL_LAYOUTITEM_CAMBIO_DATA_REG);

            //Imposto la Data Blocco OLD uguale alla data Blocco della Scheda Param (se <> 0)
            if (RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.HasValue)
                deOldBreakRegDate.Date = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.Value;

            // Imposto la data di ultima archiviazione con la data nella scheda param, se valorizzata
            if (RepoManager.ParamRepo.ParametersRow.Data_Stored_Reg.HasValue)
                deOldStoredRegDate.Date = RepoManager.ParamRepo.ParametersRow.Data_Stored_Reg.Value;
            
            //Se l'utente connesso NON ha livello ADMIN 
            if (PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Admin_Level )
                
            {
                //Se l'Utente NON è un Utente Admin
                //Nasconde la parte che permette la Modifica della Data Blocco
                lblNewBreakRegDate.Visible = false;
                deNewBreakRegDate.Visible = false;
                btnConfirmBreakRegDate.Visible = false;

                //Se l'Utente NON è un Utente Admin
                //Nasconde la parte che permette la Modifica della archiviazione
                lblNewStoredRegDate.Visible = false;
                deNewStoredRegDate.Visible = false;
                btnConfirmStoredRegDate.Visible = false;
            }

            PowerWebService.FillGridLabels(typeof(Param), GridView);
            PowerWebService.FillComboboxes(gvParam);
            BindGrid();
        }

        private void BindGrid()
        {
            gvParam.KeyFieldName = KEYFIELDNAME;
            gvParam.DataSource = new List<Param> { RepoManager.ParamRepo.ParametersRow };
            if (!Page.IsPostBack && !Page.IsCallback)
                gvParam.DataBind();
        }

        public override Type EntityType
        {
            get { return typeof(Param); }
        }

        #region gvParam : RowValidating-RowUpdating
        protected void gvParam_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {           

            Param initParam = new Param();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvParam.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentParam = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentParam, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(initParam, e.NewValues);
            PowerWebService.FillEntityKey(initParam , e.Keys, KEYFIELDNAME);
            RepoManager.ParamRepo.SetEntityBeforeAddOrUpdate(initParam);
            PowerWebService.AddValidationErrors(RepoManager.ParamRepo.Check(initParam, e.IsNewRow), e.Errors, gvParam, typeof(ParamModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvParam_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("PARAM-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvParam.KeyFieldName]);

            //empty ParametersRow to get refresh next time
            PowerWebContext.SetToSession<Param>("General_ParametersRow", null);

            Param currentParam = RepoManager.ParamRepo.ParametersRow;
            PowerWebService.FillEntityProperties(currentParam, e.NewValues);           
            //RepoManager.ParamRepo.SetEntityBeforeAddOrUpdate(currentParam);
            RepoManager.ParamRepo.SaveChanges();

            //empty ParametersRow to get refresh next time
            PowerWebContext.SetToSession<Param>("General_ParametersRow", null);

            e.Cancel = true;
            gvParam.CancelEdit();
            BindGrid();
        }      
        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //NON vengono Gestiti Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {           
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Param> parametris = CommonService.ConvertTo<Param>(items);

            XRParam ParamReport = new XRParam(parametris);

            return new ExtXtraReport { Report = ParamReport, PictureBox = ParamReport.CompanyLogo };
        }

        public log4net.ILog Log
        {
            get { return _log; }
        }

        #region Gestione Data Blocco ed eventuale Totale Monte_Ore per i Collaboratori Abilitati

        protected void btnConfirmBreakRegDate_CustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        //Gestisce l'emissione della Richiesta di Conferma 
        {
            if (!e.Properties.ContainsKey("cpConfirmMessage"))
                e.Properties.Add("cpConfirmMessage", BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_AGGIORNAMENTO));
        }

        /// <summary>
        /// Gestione della Modifica della Data Blocco e dell'eventuale Totale del Monte Ore per i Collaboratori Abilitati (se Abilitato in Param)
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="CallbackEventArgs"/> instance containing the event data.</param>
        protected void cElChangeBreakRegDate_Callback(object source, CallbackEventArgs e)
        {
            var newDate = deNewBreakRegDate.Date;
            newDate = new DateTime(newDate.Year, newDate.Month, 1).AddMonths(1);
            DateTime from = DateTime.MinValue;
            DateTime to = newDate;
            Boolean isBackward = false;

            var currBlockDate = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg;

            if (currBlockDate.HasValue)
            {
                currBlockDate = currBlockDate.Value.AddDays(1);

                if (currBlockDate.Value <= newDate)
                    from = currBlockDate.Value;
                else
                {
                    from = newDate;
                    to = currBlockDate.Value;
                    isBackward = true;
                }
            }

            // se sto andando indietro con la data blocco
            // la data blocco deve comunque restare maggiore della data di ultima archiviazione
            if (isBackward)
            {
                if (RepoManager.ParamRepo.ParametersRow.Data_Stored_Reg != null) // se è stata impostata una data di archiviazione
                {
                    // ... allora se la data di blocco nuova è inferiore alla data di ultima archiviazione
                    // segnalo errore
                    if (RepoManager.ParamRepo.ParametersRow.Data_Stored_Reg.Value < to)
                    {
                        e.Result = "Done|Data blocco deve essere maggiore della data di ultima archiviazione!";
                    }
                }
            }

            // proseguo con l'elaborazione solo se precedentemente non è stato ravvisato un errore
            if (String.IsNullOrEmpty(e.Result))
            {
                // nel caso in cui siano gestite le ORE MENSILI X COLLABORATORE in Modo Inclusivo/Esclusivo            
                if (RepoManager.ParamRepo.ParametersRow.MonthlyHoursEnum != MothlyHoursEnum.None)
                {
                    var cols = RepoManager.ColRepo.GetAll().ToList();

                    if (RepoManager.ParamRepo.ParametersRow.MonthlyHoursEnum == MothlyHoursEnum.Inclusive) //Se modo Inclusivo allora tratta i SOLI Collaboratori con Flag Monte Ore = True
                        cols = cols.Where(col => col.Flag_Monte_Ore).ToList();
                    else //Se Modo ESCLUSIVO tratta TUTTI i Collaboratori SALVO quelli con Monte Ore = TRue
                        cols = cols.Where(col => !col.Flag_Monte_Ore).ToList();

                    //Lancia l'Elaborazione e Registrazione della data Blocco e dei relativi Saldi Monte Ore x i Collaboratori abilitati
                    RepoManager.Reg_VRepo.ElaborateBlockDate(cols, from, to, isBackward);
                }
                //AGgiorna in PARAM la DATA BLOCCO con la Nuova Data Inserita z Video
                var paramRow = RepoManager.ParamRepo.First();
                paramRow.Data_Blocco_Reg = newDate.AddDays(-1);
                RepoManager.ParamRepo.SaveChanges();
                RepoManager.ParamRepo.ResetParametersRow();

                //All fine Ripulisce ImportDataStatusDictionary
                if (BusinessService.EditBlockDateStatusDictionary.ContainsKey(PowerWebContext.Current.User))
                    BusinessService.EditBlockDateStatusDictionary.Remove(PowerWebContext.Current.User);
                e.Result = "Done" + "|" + "Termine procedura di modifica data blocco";
            }

        }

        /// <summary>
        /// Restituisce quanto caricato nell'ImportDataStatusDictionary dallla Routine di gestione della modifica della data reg
        /// StatusKey : Contiene il Nome della Tabella che si sta importando in quel momento                               
        /// Valore    : Contiene la Percentuale (calcolata in base al N° di Tabelle da caricare) di Caricamento rispetto al Totale
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="CallbackEventArgs"/> instance containing the event data.</param>
        protected void cElChangeBreakRegDatePing_Callback(object source, CallbackEventArgs e)
        {
            e.Result = "0|" + "Avvio procedura di modifica data blocco";
            if (BusinessService.EditBlockDateStatusDictionary.ContainsKey(PowerWebContext.Current.User))
            //Se ci sono dati nel DictionaryStatus allora li carica nel Risultato da mostrare a Video
            {
                var status = BusinessService.EditBlockDateStatusDictionary[PowerWebContext.Current.User];
                e.Result = String.Format("{0}|{1}", status.Key, status.Value);
            }
        }

        #endregion

        #region Gestione Data Archiviazione

        protected void btnConfirmStoredRegDate_CustomJSProperties(object sender, CustomJSPropertiesEventArgs e)
        //Gestisce l'emissione della Richiesta di Conferma 
        {
            if (!e.Properties.ContainsKey("cpConfirmMessage"))
                e.Properties.Add("cpConfirmMessage", BusinessService.GetLocalizedString(PowerWebResources.STR_DOMANDA_CONFERMA_AGGIORNAMENTO));
        }

        /// <summary>
        /// Gestione della Modifica della Data Blocco e dell'eventuale Totale del Monte Ore per i Collaboratori Abilitati (se Abilitato in Param)
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="CallbackEventArgs"/> instance containing the event data.</param>
        protected void cElChangeStoredRegDate_Callback(object source, CallbackEventArgs e)
        {
            var newDate = deNewStoredRegDate.Date;

            //DateTime from = DateTime.MinValue;
            DateTime to = newDate;
            DateTime from = to.AddDays(-1);
            Boolean isBackward = false;

            //data ultima archiviazione eseguita
            var currStoredDate = RepoManager.ParamRepo.ParametersRow.Data_Stored_Reg;

            //se è valorizzata la data di ultima archiviazione
            if (currStoredDate.HasValue)
            {
                //currStoredDate = currStoredDate.Value.AddDays(1);

                //se la data di ultima archiviazione è minore o uguale alla nuova data è una nuova archiviazione aggiuntiva
                if (currStoredDate.Value <= newDate)
                    //la data da cui archiviare e la data di ultima archiviazione già eseguita
                    from = currStoredDate.Value;

                //altimenti sono nel caso di recupero di archiviazione.
                //Vengono recuperate le registrazioni se la nuova data di archiviazione  è minore di quella dell'ultima rchiviazione
                else
                {
                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.BlockRestoreReg) == 1)
                    {
                        e.Result = "Done" + "|" + "Non è possibile ripristinare le registrazioni!";
                        return;
                    }

                    from = newDate;
                    to = currStoredDate.Value;
                    isBackward = true;
                }
            }

            // in caso di spostamento in avanti della data di archiviazione
            // non si può archiviare oltre la data blocco
            DateTime? checkDataBlocco = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg == null ? DateTime.MinValue : RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg;

            if (!isBackward && newDate > checkDataBlocco)
            {
                e.Result = "Done" + "|" + "La nuova data di archiviazione deve essere minore della data blocco";
            }
            else
            {
                // lancio l'archiviazione/ripristino delle registrazioni (from e to contengono già i periodi corretti in base
                // al fatto che si stia archiviando o ripristinando)... e, in caso l'operazione sia andata a buon fine
                if (RepoManager.RegRepo.StoreOrRestoreRegs(from, to, isBackward))
                {
                    // aggiorno la data di archiviazione sulla param
                    var paramRow = RepoManager.ParamRepo.First();
                    paramRow.Data_Stored_Reg = newDate;
                    RepoManager.ParamRepo.SaveChanges();
                    RepoManager.ParamRepo.ResetParametersRow();

                    //All fine Ripulisce ImportDataStatusDictionary
                    if (BusinessService.EditStoredRegDateStatusDictionary.ContainsKey(PowerWebContext.Current.User))
                        BusinessService.EditStoredRegDateStatusDictionary.Remove(PowerWebContext.Current.User);
                    if (isBackward)
                        e.Result = "Done" + "|" + "Termine procedura di ripristino registrazioni";
                    else
                        e.Result = "Done" + "|" + "Termine procedura di archiviazione registrazioni";
                }
                else
                {
                    e.Result = String.Format("Done|{0}", BusinessService.EditStoredRegDateStatusDictionary[PowerWebContext.Current.User].Value);
                }

                
            }

        }

        /// <summary>
        /// Restituisce quanto caricato nell'ImportDataStatusDictionary dallla Routine di gestione della modifica della data reg
        /// StatusKey : Contiene il Nome della Tabella che si sta importando in quel momento                               
        /// Valore    : Contiene la Percentuale (calcolata in base al N° di Tabelle da caricare) di Caricamento rispetto al Totale
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="CallbackEventArgs"/> instance containing the event data.</param>
        protected void cElChangeStoredRegDatePing_Callback(object source, CallbackEventArgs e)
        {
            //e.Result = "0|" + "Avvio procedura di archiviazione/ripristino delle reg";
            if (BusinessService.EditStoredRegDateStatusDictionary.ContainsKey(PowerWebContext.Current.User))
            //Se ci sono dati nel DictionaryStatus allora li carica nel Risultato da mostrare a Video
            {
                var status = BusinessService.EditStoredRegDateStatusDictionary[PowerWebContext.Current.User];
                e.Result = String.Format("{0}|{1}", status.Key, status.Value);
            }
        }

        #endregion

        #region Export Excel Custom

        public List<Tab_Excel_Model> Models
        {
            get
            {
                var typeName = typeof (Param).Name;
                return RepoManager.Tab_Excel_ModelRepo.Find(xlsxModel => xlsxModel.IsActive && xlsxModel.Nome_Entity == typeName).ToList();
            }
        }

        public void ExportXLSX(Tab_Excel_Model model, List<object> items)
        {
            string path = "";

            var visibleFields = PowerWebService.GetGridViewVisibleFields(gvParam);

            List<Param> paramList = CommonService.ConvertTo<Param>(items);
            var currentType = Type.GetType(String.Format("Exports.{0}, Exports", model.Nome_Specializzato));

            if (currentType != null)
            {
                if (currentType == typeof (ExportExcelGenericParam))
                {
                }


                var currentSpecialized = (IExportExcelSpecialized<Param>) Activator.CreateInstance(currentType);
                ExportExcelEngine.Export<Param>(currentSpecialized, paramList, model, out path, visibleFields);

            }
        }

        #endregion

    }
}