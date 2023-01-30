using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using DevExpress.Web.ASPxGridView;
using Domain.Extensions;
using Exports.ExportExcelGeneric;
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
using System.Data.SqlClient;

namespace PowerWeb.Modules
{
    public partial class Reg_VModule : BaseGridModule, IPrintModule, ILogModule, IExportXLSXModule, IGeoLocationModule
    {

        const Reg_V _regVStub = null;
        const String KEYFIELDNAME = "RegE";
        private static readonly ILog _log = LogManager.GetLogger(typeof(Reg_VModule));

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

        public ILog Log
        {
            get { return _log; }
        }

        public List<Reg_V> SchedulerRegVs
        {
            get
            {
                return PowerWebContext.GetFromSession<List<Reg_V>>("SchedulerRegVs_" + scRegV.ID);
            }

            set
            {
                List<Reg_V> list = value;
                if (list != null)
                    PowerWebContext.SetToSession("SchedulerRegVs_" + scRegV.ID, list);
            }
        }
        public List<Reg_V> GridRegVs
        {
            get
            {
                List<Reg_V> regVs = new List<Reg_V>();
                for (int i = 0; i < gvRegV.VisibleRowCount; i++)
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

        public override void ResetSession()
        {
            base.ResetSession();

            if (!Page.IsPostBack && !Page.IsCallback)
            {
                PowerWebContext.SetToSession<List<Reg_V>>("SchedulerRegVs_" + scRegV.ID, null);
                PowerWebContext.SetToSession<List<Cant>>("Cants_" + GridView.ID, null);
                PowerWebContext.SetToSession<List<Col>>("Cols_" + GridView.ID, null);
            }
        }

        public bool IsInBatchMode
        {
            get { return GridView.SettingsEditing.Mode == GridViewEditingMode.Batch; }
        }

        public override ASPxGridView GridView
        {
            get { return gvRegV; }
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryRegV();

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
                    var metaDescriptor = RepoManager.MetaDescriptorRepo.Find(md => md.EntityType == "RegV", md => md.MetaFieldDescriptor, true).ToList().First();
                    if (metaDescriptor != null)
                    {
                        metaFieldDescriptors = metaDescriptor.MetaFieldDescriptor.OrderBy(mfd => mfd.VisibleIndex).ToList();
                        PowerWebContext.SetToSession<ICollection<MetaFieldDescriptor>>("MetaFieldDescriptors_" + GridView.ID, metaFieldDescriptors);
                    }
                }
                return metaFieldDescriptors;
            }

        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
                GridView.DataBind();
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            if (!GridView.JSProperties.ContainsKey("cpName"))
                GridView.JSProperties.Add("cpName", "gvRegV");

            PowerWebService.GenerateGridColumns(GridView, MetaFieldDescriptors);

            lblAutoSync.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO);

            if (!Page.IsCallback && !Page.IsPostBack)
            {
                ResetSession();

                //GridView.ClearSort();

                //GridView.GroupBy(GridView.Columns[CommonService.GetPropertyName(() => _regVStub.Col_Id)]);
                //GridView.GroupBy(GridView.Columns[CommonService.GetPropertyName(() => _regVStub.Data_Reg)]);

                //// Serve per avere la Vista di DEFAULT ordinata per Col/Data Reg/Ora 
                //(GridView.Columns[CommonService.GetPropertyName(() => _regVStub.Col_Id)] as GridViewDataColumn).SortAscending();
                //(GridView.Columns[CommonService.GetPropertyName(() => _regVStub.Data_Reg)] as GridViewDataColumn).SortDescending();
                //(GridView.Columns[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_ETime)] as GridViewDataColumn).SortAscending();
                //(GridView.Columns[CommonService.GetPropertyName(() => _regVStub.Registrazione_Tipo_Reg)] as GridViewDataColumn).SortAscending();
            }

            PowerWebService.FillGridLabels(EntityType, GridView);
            PowerWebService.FillComboboxes(GridView);

            BindScheduler(true);
        }

        protected void gvRegV_DataBinding(object sender, EventArgs e)
        {
            LinqServerModeDataSource serverMode = new LinqServerModeDataSource();
            serverMode.ContextTypeName = "PowerWebEntities.Data";
            serverMode.TableName = "Reg_V";
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

            // se non ci sono record da visualizzare (non ci sono reg nel database), allora il filtro è corretto

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
                                fromDate = CommonService.GetDateTimeByString(dates[1]);
                                toDate = DateTime.Now;
                            }
                            else
                            {
                                var dates = filterExp.Split('#');
                                fromDate = CommonService.GetDateTimeByString(dates[1]);
                                toDate = CommonService.GetDateTimeByString(dates[3]);
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

            GridView.JSProperties["cpIsFilterValid"] = isFilterValid;
            GridView.JSProperties["cpFilterError"] = filterErrorMessage;

            return isFilterValid;
        }

        private string generateWhereClause()
        {
            var stringQueryable = RepoManager.Reg_VRepo.DbSet.AsNoTracking().Where(RepoManager.Reg_VRepo.Filter).AsQueryable().ToString();
            var whereIndex = stringQueryable.LastIndexOf("WHERE");
            string whereClause = "";
            if (whereIndex != -1)
                whereClause = stringQueryable.Substring(whereIndex).Substring(5).Replace("[Extent1]", "").Replace("[", "").Replace("]", "").Replace(".", "");

            return whereClause;
        }

        void linq_Selecting(object sender, LinqServerModeDataSourceSelectEventArgs e)
        {
            e.KeyExpression = KEYFIELDNAME;
            var emptyQueryable = Enumerable.Empty<Reg_V>().AsQueryable();

            IQueryable<Reg_V> currQueryable;

            if (IsToPopulateGrid)
            {
                if (checkCurrentFilter())
                    currQueryable = RepoManager.Reg_VRepo.DbSet.SqlQuery(PowerWebService.GenerateWhereQuery(EntityType, GridView, generateWhereClause())).AsNoTracking().AsQueryable();
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

        #region gvRegV-InitNewRow-RowValidating-GetRegEFromNewValues-GetRegUFromNewvalues-RowInserting-RowUpdating-RowDeleting

        protected void gvRegV_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;

            if (grid != null)
            {
                Reg_V initRegv = RepoManager.Reg_VRepo.Init();
                PowerWebService.FillGridProperties(initRegv, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
            }
        }

        protected void gvRegV_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Reg_V regv = new Reg_V();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvRegV.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentRegv = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentRegv, MetaFieldDescriptors, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(regv, e.NewValues);
            PowerWebService.FillEntityKey(regv, e.Keys, KEYFIELDNAME);
            RepoManager.Reg_VRepo.SetEntityBeforeAddOrUpdate(regv);

            //Prepara le Due Reg di Entrata e di Uscita (se esiste) con i Dati Aggiornati
            var currentRegE = RepoManager.RegRepo.GetRegEFromNewValues(e.NewValues);

            //Aggiunte x il Caso di un Record Clone x Aggiornare la data sulla Vista in caso di Cambio Data rispetto al record Clonato 
            regv.Data_Ora_Fis_E = currentRegE.Registrazione_Data_Ora_Fis_Reg.Date.Add(currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay);

            PowerWebService.AddValidationErrors(RepoManager.RegRepo.Check(currentRegE, e.IsNewRow), e.Errors, GridView, typeof(Reg_VModule));

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

        protected void gvRegV_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
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
                //aggiungo la REGU nelle Reg da Trattare
                toAddRegsNew.Add(currentRegUNew);
            }

            RepoManager.RegRepo.Add(toAddRegsNew, true);

            // Le Ore dei Campi Date vengono sempre inizializzate a ZERO dal sistema
            // Occorre quindi selezionare SEMPRE x DATA MINORE della DATA con ORE ZERO del GG Successivo
            // Così vengono prese tutte le REG DEL GIORNO (per non mettere <= 23.59.59)  
            //Normalmente bastano quelle del Giorno (per cui i GG in più sono 1 per via dell'Ora 00:00:00)
            DateTime startElabDate = dayDateNew;
            DateTime endElabDate = dayDateNew.AddDays(1);

            // gestione delle date di inizio/fine periodo in base alla configurazione del notturno
            BusinessService.ManageNocturneStartEndDate(ref startElabDate, ref endElabDate);

            //leggo Tutte le REG NEW Necessarie alla Successiva ELABORATE
            List<Reg> toElaborateNewRegs = RepoManager.RegRepo.Find(reg => reg.Col_Id == currentRegENew.Col_Id &&
                        reg.Registrazione_Data_Ora_Fis_Reg >= startElabDate && reg.Registrazione_Data_Ora_Fis_Reg < endElabDate, true).ToList();

            //lancio la Ri_elaborazione delle REG da trattare
            RepoManager.RegRepo.Elaborate(toElaborateNewRegs, dateTimeEFisNew, dateTimeUFisNew);

            e.Cancel = true;
            gvRegV.CancelEdit();
        }

        protected void gvRegV_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("Reg-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));

            //--------------Gestione Cancellazione Vecchi  Dati -----------------------------------------

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
            toDeleteRegsOld.Add(currentRegEOld);

            // viene salvata la data ora fisica originale della reg in entrata
            origDateE = currentRegEOld.Registrazione_Data_Ora_Orig_Reg;

            if (currentRegUId != 0)
            {
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

            //--------------Gestione Inserimento Nuovi Dati -----------------------------------------

            List<Reg> toAddRegsNew = new List<Reg>();

            //Recupero la Data  della Registrazione 
            DateTime dayDateNew = Convert.ToDateTime(e.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Reg)]).Date;

            // è calcolato il fatto che si sta elaborando un'uscita nello stesso giorno
            bool isSameDay = true;
            // se non c'è il notturno è sicuramente true
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight)
                isSameDay = Convert.ToBoolean(e.NewValues[CommonService.GetPropertyName(() => _regVStub.IsUTimeSameDayE)]); // altrimenti dipende dal flag di modifica

            //Inizializzo i Dati delle REG di Entrata e Uscita da Inserire                        
            var currentRegENew = RepoManager.RegRepo.GetRegEFromNewValues(e.NewValues, true, currentRegEOld); // la reg e in updating è sempre in modifica

            // la reg u invece potrebbe essere nuova
            var currentRegUNew = RepoManager.RegRepo.GetRegUFromNewValues(e.NewValues, currentRegUOld != null, currentRegUOld);

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
                    if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight && !isSameDay)
                        //Incremento la data della Registrazione di 1 GG 
                        dateTimeUFisNew = CommonService.ComputeDateTime(dayDateNew.AddDays(1), dateTimeUFisNew);
            }

            //Se l'Uscita NON è presente la salto
            if (currentRegUNew != null)
            {
                //se l'Ora di Uscita è < dell'Ora di Entrata significa che la Registrazione di Uscita è del giorno DOPO (caso di Notturno)
                if (dateTimeUFisNew.TimeOfDay < dateTimeEFisNew.TimeOfDay)
                    // si procede all'incremento del giorno solamente se è abilitato il notturno  e non si tratta dello stesso giorno
                    if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight && !isSameDay)
                        currentRegUNew.Registrazione_Data_Ora_Fis_Reg = currentRegUNew.Registrazione_Data_Ora_Fis_Reg.AddDays(1);

                // prima dell'aggiunta della reg in entrata a quelle da inserire si verifica che sia cambiato il col_id o il cant_id e 
                // in questo caso sono svuotati pru e fru
                if (currentRegEOld != null)
                    RepoManager.RegRepo.ManageCantColChangesBeforeUpdate(currentRegUNew, currentRegEOld);

                //aggiungo la REGU nelle Reg da Trattare
                toAddRegsNew.Add(currentRegUNew);
            }

            // sono cancellate le reg solamente prima dell'aggiunta delle nuove popolate così da avere a disposizione i valori per il confronto
            // di modifica di cant_id e col_id
            RepoManager.RegRepo.Delete(toDeleteRegsOld, true);

            RepoManager.RegRepo.Add(toAddRegsNew, true);

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

            List<Reg> toElaborateRegs = new List<Reg>();
            //Unisco (senza Duplicati) le due liste  (una lista di New c'e sempre)   
            if (toElaborateRegsOld.Count > 0)
                RepoManager.RegRepo.Elaborate(toElaborateRegsOld, dateTimeEFisOld, dateTimeUFisOld);
            if (toElaborateRegsNew.Count > 0)
                RepoManager.RegRepo.Elaborate(toElaborateRegsNew, dateTimeEFisNew, dateTimeUFisNew);

            e.Cancel = true;
            GridView.CancelEdit();
        }

        protected void gvRegV_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
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
            RepoManager.RegRepo.Elaborate(toElaborateRegs, dateTimeEFisOld, dateTimeUFisOld);

            e.Cancel = true;
            gvRegV.CancelEdit();
        }

        #endregion

        public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        }

        #region Scheduler

        private void BindScheduler(bool isForceResetStartDate = false)
        {

            if (!Page.IsPostBack && !Page.IsCallback)
                scRegV.Start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            if (SchedulerRegVs != null)
            {
                if (isForceResetStartDate)
                {
                    Reg_V first = SchedulerRegVs.OrderBy(s => s.Data_Ora_Fis_E).FirstOrDefault();
                    if (first != null)
                        scRegV.Start = new DateTime(first.Data_Ora_Fis_E.Year, first.Data_Ora_Fis_E.Month, 1);
                }

                ASPxSchedulerStorage storage = scRegV.Storage;
                ASPxAppointmentMappingInfo mappings = storage.Appointments.Mappings;
                storage.BeginUpdate();
                try
                {
                    mappings.AppointmentId = CommonService.GetPropertyName(() => _regVStub.RegE);
                    mappings.Start = CommonService.GetPropertyName(() => _regVStub.Data_Ora_FigFis_E);
                    mappings.End = CommonService.GetPropertyName(() => _regVStub.Data_Ora_FigFis_U);
                    mappings.Description = CommonService.GetPropertyName(() => _regVStub.Motivazione_Reg_Cod);

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Cant_Id));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Col_Id));

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.RegU));

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_E));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_U));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Durata_Fis));

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fig_E));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fig_U));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Durata_Fig));

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.KM_Reg));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Note_Reg));

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Col_Mnemonic));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Col_Desc));

                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Cant_Mnemonic));
                    AddCustomMapping(storage, CommonService.GetPropertyName(() => _regVStub.Cant_Desc));
                }
                finally
                {
                    storage.EndUpdate();
                }

                scRegV.AppointmentDataSource = SchedulerRegVs;
                scRegV.DataBind();

                scRegV.AppointmentViewInfoCustomizing += new AppointmentViewInfoCustomizingEventHandler(scRegV_AppointmentViewInfoCustomizing);
            }

            scRegV.TimelineView.IntervalCount = CommonService.GetLastMonthDay(new DateTime(scRegV.Start.Year, scRegV.Start.Month, 1)).Day;
        }

        private void AddCustomMapping(ASPxSchedulerStorage storage, String propertyName)
        {
            storage.Appointments.CustomFieldMappings.Add(new ASPxAppointmentCustomFieldMapping(propertyName, propertyName));
        }

        //gestione della visualizzaione delle reg nello scheduler
        protected void scRegV_AppointmentViewInfoCustomizing(object sender, AppointmentViewInfoCustomizingEventArgs e)
        {
            var currId = e.ViewInfo.Appointment.Id;
            var currRegType = SchedulerRegVs.SingleOrDefault(reg => reg.RegE == (int)currId).Registrazione_Tipo_Reg;
            //Impostazione del Colore di Sfondo per le Attività
            if (currRegType == (int)RegTypeEnum.Att)
            {
                e.ViewInfo.AppointmentStyle.BackColor = RepoManager.ParamRepo.GetColorFromEnum((SchedulerEnum)currRegType, true);
                e.ViewInfo.ShowStartTime = false;
            }
            //Impostazione del Colore di Sfondo per i Viaggi
            else if (currRegType == (int)RegTypeEnum.Trip)
            {
                e.ViewInfo.AppointmentStyle.BackColor = RepoManager.ParamRepo.GetColorFromEnum((SchedulerEnum)currRegType, true);
            }

            //Impostazione del Colore di Sfondo per le Ore che hanno Inizio/Fine Uguale
            else if (e.ViewInfo.Appointment.Start == e.ViewInfo.Appointment.End)
            {
                currRegType = (int)RegTypeEnum.Pass;
                e.ViewInfo.AppointmentStyle.BackColor = RepoManager.ParamRepo.GetColorFromEnum((SchedulerEnum)currRegType, true);
                e.ViewInfo.ShowEndTime = false;
            }
            else
                //Impostazione del Colore di Sfondo standard
                currRegType = (int)SchedulerEnum.None;
            e.ViewInfo.AppointmentStyle.BackColor = RepoManager.ParamRepo.GetColorFromEnum((SchedulerEnum)currRegType, true);
        }

        protected void scRegV_PopupMenuShowing(object sender, DevExpress.Web.ASPxScheduler.PopupMenuShowingEventArgs e)
        {
            ASPxScheduler scheduler = sender as ASPxScheduler;

            //Menu over reg.
            if (e.Menu.Id == SchedulerMenuItemId.AppointmentMenu)
            {
                e.Menu.Items.Clear();

                e.Menu.ClientSideEvents.ItemClick = "function(s,e){ scRegV_OnMenuItemClick(\"" + scheduler.ClientInstanceName + "\",s,e)}";

                MenuItem newCloneMenuItem = new MenuItem("NewClone", "NewCloneCommand");
                newCloneMenuItem.Image.Url = "../Icons/Add/Add.png";
                e.Menu.Items.Add(newCloneMenuItem);

                MenuItem editMenuItem = new MenuItem("Edit", "EditCommand");
                editMenuItem.Image.Url = "../Icons/Edit/Edit.png";
                e.Menu.Items.Add(editMenuItem);

                MenuItem deleteMenuItem = new MenuItem("Delete", "DeleteCommand");
                deleteMenuItem.Image.Url = "../Icons/Delete/Delete.png";
                e.Menu.Items.Add(deleteMenuItem);
            }
            //Standard Menu
            if (e.Menu.Id == SchedulerMenuItemId.DefaultMenu)
            {
                ClearUnusedDefaultMenuItems(e.Menu);
                e.Menu.ClientSideEvents.ItemClick = "function(s,e){ scRegV_OnMenuItemClick(\"" + scheduler.ClientInstanceName + "\",s,e)}";

                MenuItem newMenuItem = new MenuItem("New", "NewCommand");
                newMenuItem.Image.Url = "../Icons/Add/Add.png";
                e.Menu.Items.Insert(0, newMenuItem);


            }
        }

        private void ClearUnusedDefaultMenuItems(ASPxSchedulerPopupMenu menu)
        {
            RemoveMenuItem(menu, "NewAppointment");
            RemoveMenuItem(menu, "NewAllDayEvent");
            RemoveMenuItem(menu, "NewRecurringAppointment");
            RemoveMenuItem(menu, "NewRecurringEvent");
        }

        private void RemoveMenuItem(ASPxSchedulerPopupMenu menu, string menuItemName)
        {
            MenuItem item = menu.Items.FindByName(menuItemName);
            if (item != null)
                menu.Items.Remove(item);
        }

        #endregion

        #region Callbacks

        protected void scRegVPanel_Callback(object sender, DevExpress.Web.ASPxClasses.CallbackEventArgsBase e)
        {
            if (String.IsNullOrEmpty(e.Parameter))
            {
                SchedulerRegVs = GridRegVs;
                BindScheduler(true);
            }
        }

        protected void gvRegVPanel_Callback(object sender, DevExpress.Web.ASPxClasses.CallbackEventArgsBase e)
        {
            string[] splittedParameter = e.Parameter.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            if (splittedParameter.Count() > 1)
            {
                var visibleIndex = GridView.FindVisibleIndexByKeyValue(Convert.ToInt32(splittedParameter[1]));

                if (e.Parameter.StartsWith("newclone"))
                {
                    var currentMaster = Page.Master as GridMasterPage;
                    currentMaster.GridClonedValues = new Hashtable();


                    GridView.MakeRowVisible(visibleIndex);

                    foreach (GridViewDataColumn column in GridView.Columns.OfType<GridViewDataColumn>())
                        if (GridView.KeyFieldName != column.FieldName)
                            currentMaster.GridClonedValues[column.FieldName] = GridView.GetRowValues(visibleIndex, column.FieldName);

                    GridView.AddNewRow();
                }

                if (e.Parameter.StartsWith("edit"))
                {
                    GridView.MakeRowVisible(visibleIndex);
                    GridView.StartEdit(visibleIndex);
                }
                if (e.Parameter.StartsWith("delete"))
                {
                    GridView.MakeRowVisible(visibleIndex);
                    GridView.DeleteRow(visibleIndex);
                }
            }
        }

        #endregion

        //Imposta i Filtri CUSTOM - Data_Reg
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

        protected void gvRegVPanel_CustomJSProperties(object sender, DevExpress.Web.ASPxClasses.CustomJSPropertiesEventArgs e)
        {
            e.Properties.Add("cp" + PowerWebResources.STR_TITOLO.ToString(), BusinessService.GetLocalizedString(PowerWebResources.STR_TITOLO));
        }

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
            string path = null;

            List<Reg_V> regs = CommonService.ConvertTo<Reg_V>(items);

            var currentType = Type.GetType(String.Format("Exports.{0}, Exports", model.Nome_Specializzato));

            if (currentType != null)
            {
                if (currentType == typeof(ExportExcelSpecializedExtendedReg_V))
                {
                    var currentSpecialized = (IExportExcelSpecialized<Reg_V>)Activator.CreateInstance(currentType);
                    ExportExcelEngine.Export<Reg_V>(currentSpecialized, regs, model,out path);
                }

                if (currentType == typeof(ExportExcelGenericReg_V))
                {
                    IEnumerable<Tuple<string, string, string>> visibleFields = null;

                    var currentSpecialized = (IExportExcelSpecialized<Reg_V>)Activator.CreateInstance(currentType);

                    visibleFields = PowerWebService.GetGridViewVisibleFields(gvRegV);

                    ExportExcelEngine.Export(currentSpecialized, regs, model, out path, visibleFields);
                }

                if (currentType == typeof(ExportExcelSpecializedActivity))
                {
                    var list = BusinessService.PopulateActivityList(regs);

                    var currentSpecialized = (IExportExcelSpecialized<ActivityItem>)Activator.CreateInstance(currentType);
                    ExportExcelEngine.Export<ActivityItem>(currentSpecialized, list, model, out path);

                }
                
            }
            Page.Response.Redirect(Page.Request.Url.ToString(), true);
        }

        #endregion

        #region Gestione ottimizzata componente calendario
        protected void gvRegV_CellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            SetupCalendarOwner(e.Editor as ASPxDateEdit);
        }

        protected void gvRegV_AutoFilterCellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            SetupCalendarOwner(e.Editor as ASPxDateEdit);
        }

        void SetupCalendarOwner(ASPxDateEdit editor)
        {
            if (editor == null) return;
            editor.PopupCalendarOwnerID = "__ReferenceDateEdit";
        }
        #endregion
    }
}