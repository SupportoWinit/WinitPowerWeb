using Business;
using Business.Repository;
using Common;
using DevExpress.Data.Linq;
using DevExpress.Data.PLinq.Helpers;
using DevExpress.Data.WcfLinq.Helpers;
using DevExpress.Office.Utils;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxEditors;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.ASPxPanel;
using DevExpress.Web.ASPxTitleIndex.Internal;
using DevExpress.Web.Data;
using DevExpress.XtraPivotGrid.Data;
using Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PowerWeb.Modules
{
    public partial class Aut_FerPerModule : BaseGridModule, IPrintModule
    {
        #region Fields and Constants

        const String KEYFIELDNAME = "RegE;TmpNewId";

        private string _callBackParameter = string.Empty;

        private Type _entityType = typeof(Reg_V);
        const Reg_V _regVStub = null;

        #endregion

        #region Properties

        public bool IsInBatchMode
        {
            get { return GridView.SettingsEditing.Mode == GridViewEditingMode.Batch; }
        }

        public List<Reg_V> RegVsToAdd
        {
            get
            {
                var regVs = PowerWebContext.GetFromSession<List<Reg_V>>("RegVsToAdd" + gvAutFerPerEdit.ID);
                if (regVs == null)
                {
                    regVs = new List<Reg_V>();
                    PowerWebContext.SetToSession("RegVsToAdd" + gvAutFerPerEdit.ID, regVs);
                }
                return regVs;

            }

            set
            {
                PowerWebContext.SetToSession("RegVsToAdd" + gvAutFerPerEdit.ID, value ?? new List<Reg_V>());
            }
        }

        public List<Reg_V> RegVsToUpdate
        {
            get
            {
                var regVs = PowerWebContext.GetFromSession<List<Reg_V>>("RegVsToUpdate" + gvAutFerPerEdit.ID);
                if (regVs == null)
                {
                    regVs = new List<Reg_V>();
                    PowerWebContext.SetToSession("RegVsToUpdate" + gvAutFerPerEdit.ID, regVs);
                }
                return regVs;

            }

            set
            {
                PowerWebContext.SetToSession("RegVsToUpdate" + gvAutFerPerEdit.ID, value ?? new List<Reg_V>());
            }
        }

        public List<Reg_V> RegVsToDelete
        {
            get
            {
                var regVs = PowerWebContext.GetFromSession<List<Reg_V>>("RegVsToDelete" + gvAutFerPerEdit.ID);
                if (regVs == null)
                {
                    regVs = new List<Reg_V>();
                    PowerWebContext.SetToSession("RegVsToDelete" + gvAutFerPerEdit.ID, regVs);
                }
                return regVs;

            }

            set
            {
                PowerWebContext.SetToSession("RegVsToDelete" + gvAutFerPerEdit.ID, value ?? new List<Reg_V>());
            }
        }

        public override ASPxGridView GridView
        {
            get
            {
                return gvAutFerPerEdit;
            }
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

        public override Type EntityType
        {
            get
            {
                return _entityType;
            }
        }

        public String EditErrorMessage
        {
            get
            {
                return PowerWebContext.GetFromSession<String>("EditErrorMessage" + gvAutFerPerEdit.ID);
            }

            set
            {
                PowerWebContext.SetToSession("EditErrorMessage" + gvAutFerPerEdit.ID, value);
            }
        }

        public Boolean IsToShowEditGrid
        {
            get
            {
                return PowerWebContext.GetFromSession<Boolean>("IsToShowEditGrid" + gvAutFerPerEdit.ID);
            }

            set
            {
                PowerWebContext.SetToSession("IsToShowEditGrid" + gvAutFerPerEdit.ID, value);
            }
        }

        public DateTime? BlockDate
        {
            get
            {
                var dateBlock = PowerWebContext.GetFromSession<DateTime?>("BlockDate" + gvAutFerPerEdit.ID);
                if (dateBlock == null)
                {
                    dateBlock = RepoManager.ParamRepo.First().Data_Blocco_Reg ?? DateTime.MinValue;
                    PowerWebContext.SetToSession("BlockDate" + gvAutFerPerEdit.ID, dateBlock);
                }
                return dateBlock;
            }
            set { PowerWebContext.SetToSession("BlockDate" + gvAutFerPerEdit.ID, value); }
        }

        public Dictionary<string, IQueryable<Reg_V>> CachedRegVs
        {
            get
            {
                var regVs = PowerWebContext.GetFromSession<Dictionary<string, IQueryable<Reg_V>>>("ChachedRegVs" + gvAutFerPerEdit.ID);
                if (regVs == null)
                    PowerWebContext.SetToSession("ChachedRegVs" + gvAutFerPerEdit.ID, new Dictionary<string, IQueryable<Reg_V>>());

                return PowerWebContext.GetFromSession<Dictionary<string, IQueryable<Reg_V>>>("ChachedRegVs" + gvAutFerPerEdit.ID);
            }

            set
            {
                PowerWebContext.SetToSession("ChachedRegVs" + gvAutFerPerEdit.ID, value);
            }
        }

        public IQueryable<Reg_V> RegVDataSource
        {
            get
            {
                IQueryable<Reg_V> regVs = null;
                var richiestaPermessoId = RepoManager.Tab_DecodRepo.GetAllQueryable(d => d.Chiave_Tab == "RP" && d.Nome_Tab == "MOTIVAZIONI").ToList();
                var richiestaFerieId = RepoManager.Tab_DecodRepo.GetAllQueryable(f => f.Chiave_Tab == "RF" && f.Nome_Tab == "MOTIVAZIONI").ToList();
                //recupero una lista di motivazione che iniziano per 'Richiesta'
                var motivazioniRichieste = RepoManager.Tab_DecodRepo.GetAllQueryable(mr => mr.Nome_Tab == "MOTIVAZIONI" && mr.Decodifica_Tab.Contains("Richiesta")).ToList();
                //StringBuilder sbQuery = new StringBuilder("SELECT * FROM Reg_V where");
                //sbQuery.AppendFormat("(Motivazione_Reg_Id = {0} OR Motivazione_Reg_Id = {1}", richiestaPermessoId.First().Tab_Decod_Id, richiestaFerieId.First().Tab_Decod_Id);
                //sbQuery.Append(')');
                StringBuilder test = new StringBuilder("SELECT * FROM Reg_V where");
                test.Append("(");
                //per ogni motivazione di richiesta vado ad aggiornare la query
                foreach (var decod in motivazioniRichieste) {
                    test.AppendFormat("Motivazione_Reg_Id = {0}", decod.Tab_Decod_Id);
                    if (decod != motivazioniRichieste.Last()) {
                        test.Append(" OR ");
                    }
                }
                test.Append(')');
                regVs = RepoManager.Reg_VRepo.DbSet.SqlQuery(test.ToString()).AsNoTracking().AsQueryable();
                //recupero le timbrature che hanno come motivazione richiesta ferie o richiesta permesso
                //regVs = regVs.Where(regv => regv.Motivazione_Reg_Id == richiestaPermessoId.First().Tab_Decod_Id || regv.Motivazione_Reg_Id == richiestaFerieId.First().Tab_Decod_Id);
                //inizializzo una lista per recuperare solo le richieste di ferie
                List<Reg_V> ferieReg = regVs.Where(regv => regv.Registrazione_Tipo_Reg == 8).OrderBy(r => r.Data_Reg.Value).ToList();
                List<Reg_V> groupReg = new List<Reg_V>();
                List<Reg_V> dayPerm = new List<Reg_V>();
                //faccio un groupBy per avere le timbrature per collaboratore così da elaborarne un collaboratore alla volta 
                var colReg = ferieReg.GroupBy(regv => regv.Col_Id).ToList();
                foreach (var cols in colReg) {
                    //inizializzo la variabile della data con un valore di default
                    DateTime primoGiorno = new DateTime(9999,12,31);
                    //inizializzo la variabile per tenere conto del periodo
                    int periodo = 1;
                    //inizializzo la Reg_V per tenere in memoria la prima Reg_V
                    Reg_V tmpRegV = new Reg_V();
                    //inizializzo la Reg_V per memorizzare l'ultima timbratura per controllare se il periodo è consecutivo
                    Reg_V lastRegV = new Reg_V();
                    foreach (Reg_V reg in cols) {
                        Tab_Decod mot = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Tab_Decod_Id == reg.Motivazione_Reg_Id);
                        if (mot.Campo1_Tab != null)
                        {
                            dayPerm.Add(reg);
                        }
                        else 
                        {
                            //se la data è ancora il valore di default metto la prima data della lista, segnalerà l'inizio del primo periodo di ferie
                            if (primoGiorno.Equals(new DateTime(9999, 12, 31)))
                            {
                                primoGiorno = reg.Data_Reg.Value;
                                tmpRegV = reg;
                                lastRegV = reg;
                            }
                            //controllo se la reg è l'ultima della lista
                            if (reg.Equals(cols.Last()))
                            {
                                if (periodo == 1)
                                {
                                    //controllo se la data è uguale alla variabile 'primoGiorno', in quel caso non è un periodo e si può lasciare la reg base
                                    if (reg.Data_Reg.Value != primoGiorno)
                                    {
                                        //se non sono consecutive faccio un ulteriore controllo, ovvero se il giorno precedente è un venerdì o sabato
                                        if ((lastRegV.Data_Reg.Value.DayOfWeek == DayOfWeek.Friday || lastRegV.Data_Reg.Value.DayOfWeek == DayOfWeek.Saturday) && reg.Data_Reg.Value.DayOfWeek == DayOfWeek.Monday)
                                        {
                                            Reg_V newRegV = tmpRegV;
                                            string noteReg = "";
                                            var dataPrimoGiorno = primoGiorno.ToString().Split(' ');
                                            var dataReg = reg.Data_Reg.Value.ToString().Split(' ');
                                            noteReg = "" + dataPrimoGiorno[0] + "-" + dataReg[0];
                                            newRegV.Note_Reg = noteReg;
                                            groupReg.Add(newRegV);
                                        }
                                        else
                                        {
                                            string noteReg = "";
                                            Reg_V newRegV = reg;
                                            var dataReg = reg.Data_Reg.Value.ToString().Split(' ');
                                            noteReg = dataReg[0];
                                            newRegV.Note_Reg = noteReg;
                                            groupReg.Add(newRegV);
                                            noteReg = "";
                                            newRegV = lastRegV;
                                            dataReg = lastRegV.Data_Reg.Value.ToString().Split(' ');
                                            noteReg = dataReg[0];
                                            newRegV.Note_Reg = noteReg;
                                            groupReg.Add(newRegV);
                                        }
                                    }
                                    else
                                    {
                                        string noteReg = "";
                                        Reg_V newRegV = tmpRegV;
                                        var dataReg = reg.Data_Reg.Value.ToString().Split(' ');
                                        noteReg = dataReg[0];
                                        newRegV.Note_Reg = noteReg;
                                        groupReg.Add(newRegV);
                                    }
                                }
                                else
                                {
                                    if ((reg.Data_Reg.Value - primoGiorno) == TimeSpan.FromDays(periodo))
                                    {
                                        Reg_V newRegV = tmpRegV;
                                        string noteReg = "";
                                        var dataPrimoGiorno = primoGiorno.ToString().Split(' ');
                                        var dataReg = primoGiorno.AddDays(periodo).ToString().Split(' ');
                                        noteReg = "" + dataPrimoGiorno[0] + "-" + dataReg[0];
                                        newRegV.Note_Reg = noteReg;
                                        groupReg.Add(newRegV);
                                        periodo = 1;
                                        primoGiorno = reg.Data_Reg.Value;
                                        tmpRegV = reg;
                                    }
                                    else
                                    {
                                        periodo--;
                                        Reg_V newRegV = tmpRegV;
                                        string noteReg = "";
                                        var dataPrimoGiorno = primoGiorno.ToString().Split(' ');
                                        var dataReg = primoGiorno.AddDays(periodo).ToString().Split(' ');
                                        noteReg = "" + dataPrimoGiorno[0] + "-" + dataReg[0];
                                        newRegV.Note_Reg = noteReg;
                                        groupReg.Add(newRegV);
                                        newRegV = reg;
                                        var dataUltimaTimb = reg.Data_Reg.Value.ToString().Split(' ');
                                        noteReg = "" + dataUltimaTimb[0];
                                        newRegV.Note_Reg = noteReg;
                                        groupReg.Add(newRegV);
                                    }

                                }

                            }
                            //controllo che la data non sia uguale perchè in quel caso è la prima
                            else if (!primoGiorno.Equals(reg.Data_Reg.Value))
                            {
                                //se sono consecutive e non è l'ultima incremento solo la viariabile periodo che mi serve per il controllo
                                if ((reg.Data_Reg.Value - primoGiorno) == TimeSpan.FromDays(periodo))
                                {
                                    periodo++;
                                }
                                else
                                {
                                    //se non sono consecutive faccio un ulteriore controllo, ovvero se il giorno precedente è un venerdì o sabato
                                    if ((lastRegV.Data_Reg.Value.DayOfWeek == DayOfWeek.Friday || lastRegV.Data_Reg.Value.DayOfWeek == DayOfWeek.Saturday) && reg.Data_Reg.Value.DayOfWeek == DayOfWeek.Monday)
                                    {
                                        DateTime dateConf = lastRegV.Data_Reg.Value.AddDays(2);
                                        if (reg.Data_Reg.Value == dateConf)
                                        {
                                            periodo = periodo + (reg.Data_Reg.Value.DayOfYear - lastRegV.Data_Reg.Value.DayOfYear);
                                        }
                                        else
                                        {
                                            Reg_V newRegV = tmpRegV;
                                            string noteReg = "";
                                            var dataPrimoGiorno = primoGiorno.ToString().Split(' ');
                                            var dataReg = lastRegV.Data_Reg.Value.ToString().Split(' ');
                                            noteReg = "" + dataPrimoGiorno[0] + "-" + dataReg[0];
                                            newRegV.Note_Reg = noteReg;
                                            groupReg.Add(newRegV);
                                            periodo = 1;
                                            primoGiorno = reg.Data_Reg.Value;
                                            tmpRegV = reg;
                                        }

                                    }
                                    else
                                    {
                                        Reg_V newRegV = tmpRegV;
                                        string noteReg = "";
                                        var dataPrimoGiorno = primoGiorno.ToString().Split(' ');
                                        var dataReg = lastRegV.Data_Reg.Value.ToString().Split(' ');
                                        noteReg = "" + dataPrimoGiorno[0] + "-" + dataReg[0];
                                        newRegV.Note_Reg = noteReg;
                                        groupReg.Add(newRegV);
                                        periodo = 1;
                                        primoGiorno = reg.Data_Reg.Value;
                                        tmpRegV = reg;
                                    }
                                }
                                lastRegV = reg;
                            }
                        }
                        
                    }
                }
                regVs = regVs.Where(regv => regv.Registrazione_Tipo_Reg != 8);
                List<Reg_V> tmpList = regVs.ToList();
                tmpList.AddRange(groupReg);
                tmpList.AddRange(dayPerm);
                return tmpList.AsQueryable();
            }
        }

        public List<Col> EditedCols
        {
            get
            {
                var editedColsList = PowerWebContext.GetFromSession<List<Col>>("EditedCols" + gvAutFerPerEdit.ID);
                if (editedColsList == null)
                {
                    editedColsList = new List<Col>();

                    PowerWebContext.SetToSession("EditedCols" + gvAutFerPerEdit.ID, editedColsList);
                }

                return editedColsList;

            }

            set
            {
                PowerWebContext.SetToSession("EditedCols" + gvAutFerPerEdit.ID, value);
            }
        }

        public bool PrimoSalvataggio
        {
            get
            {
                return PowerWebContext.GetFromSession<bool>("PrimoSalvataggio" + gvAutFerPerEdit.ID);
            }
            set
            {
                PowerWebContext.SetToSession("PrimoSalvataggio" + gvAutFerPerEdit.ID, value);

                SetColDataSourceLabelText();

            }
        }

        public IQueryable<Col> ColDataSource
        {
            get
            {
                IQueryable<Col> collList = null;
                if (IsPreFilterApplied)
                {
                    collList = PowerWebContext.GetFromSession<IQueryable<Col>>("ColDataSource" + gvAutFerPerEdit.ID);
                    if (collList == null)
                    {
                        // viene preparata la query in base agli stati selezionati...
                        var sbQuery = BuildRegVErrQuery();

                        // se è richiesto dalle personalizzazioni di visualizzare anche i collaboratori con mancate timbrature allora
                        // si estraggono anche quelli
                        int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ErrModuleShowEmptyColEnum);

                        var regvErr = RepoManager.Reg_VRepo.DbSet.SqlQuery(sbQuery).AsNoTracking().AsQueryable();
                        var collIdList = regvErr.Select(regV => regV.Col_Id).Distinct().AsQueryable();

                        collList = RepoManager.ColRepo.Find(col => collIdList.Contains(col.Col_Id)).AsQueryable();

                        // se è valorizzato il combobox di selezione del collaboratore in pre-filtro allora i dati devono riguardare solo
                        // quel collaboratore
                        //if (CmbColPreFilter.SelectedIndex != -1)
                        //    collList = collList.Where(col => col.Col_Id == (int)CmbColPreFilter.Value);

                        IEnumerable<Col> nonPresentColList = Enumerable.Empty<Col>();
                        if (customizationVersion == (int)ErrModuleShowEmptyColEnum.Show)
                            nonPresentColList = RepoManager.ColRepo.Find(col => !col.DisAbilitazione_Col && !collIdList.Contains(col.Col_Id));


                        //SetEditDatesByColId(regvErr, Convert.ToDateTime(SearchDateFrom.Text,
                        //    PowerWebContext.Current.UserCultureInfo),
                        //    Convert.ToDateTime(SearchDateTo.Text, PowerWebContext.Current.UserCultureInfo),
                        //    nonPresentColList);

                        if (customizationVersion == (int)ErrModuleShowEmptyColEnum.Show)
                        {
                            var nonPresentColIds = AllErrDatesByColId.Where(kvp => !collList.Any(checkCol => checkCol.Col_Id == kvp.Key)).Select(kvp => kvp.Key);
                            collList = RepoManager.ColRepo.Find(col => collIdList.Contains(col.Col_Id) || nonPresentColIds.Contains(col.Col_Id)).AsQueryable();
                        }

                        collList = collList.Where(col => AllErrDatesByColId[col.Col_Id].Any()).AsQueryable();

                        PowerWebContext.SetToSession("ColDataSource" + gvAutFerPerEdit.ID, collList);

                    }
                }


                return collList;
            }

            set
            {
                PowerWebContext.SetToSession("ColDataSource" + gvAutFerPerEdit.ID, value);
            }
        }

        public Dictionary<int?, IQueryable<string>> AllErrDatesByColId
        {
            get
            {
                Dictionary<int?, IQueryable<string>> dateList = null;
                if (IsPreFilterApplied)
                    dateList = PowerWebContext.GetFromSession<Dictionary<int?, IQueryable<string>>>("AllErrDatesByColId" + gvAutFerPerEdit.ID) ?? new Dictionary<int?, IQueryable<string>>();

                return dateList;
            }

            set
            {
                PowerWebContext.SetToSession("AllErrDatesByColId" + gvAutFerPerEdit.ID, value);
            }
        }

        public bool IsPreFilterApplied
        {
            get
            {
                var isPreFilter = PowerWebContext.GetFromSession<bool?>("IsPreFilterApplied" + gvAutFerPerEdit.ID) ?? false;

                return isPreFilter;
            }

            set
            {
                PowerWebContext.SetToSession("IsPreFilterApplied" + gvAutFerPerEdit.ID, value);
            }
        }

        /// <summary>
        /// Recupera il tipo di modifica applicato alla griglia del modulo di gestione delle errate.
        /// </summary>
        /// <value>
        /// Il tipo di modifica applicato alla griglia del modulo di gestione delle errate.
        /// </value>
        public EditTypeErrModuleEnum EditType
        {
            get
            {
                return (EditTypeErrModuleEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.EditTypeErrModuleEnum);
            }
        }

        #endregion

        #region Eventi pagina

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
                BindGrid(true);

            //if (cmbData_Reg.DataSource == null)
            //    BindCmbData();

            SetGridEditType();
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            if (!Page.IsCallback && !Page.IsPostBack)
            {
                // alla prima apertura della pagina viene segnalato al modulo che non è stato applicato il filtro.
                // questo permette di evitare errori nel calcolo dei datasource per mancanza dei selettori dei filtri
                IsPreFilterApplied = false;

                // all'apertura della pagina vado a segnalarae che il primo salvataggio non è ancora avvenuto
                PrimoSalvataggio = false;

                ResetSession();
            }

            // impostazione in lingua degli elementi della form
            LocalizeElements();

            SetGridEditType();

            // inizializzazione dei campi data per la preselezione (dal 1° del mese precedente ad oggi);
            // se la data così calcolata è inferiore o uguale alla data blocco, la stessa viene riportata alla data blocco + 1 giorno
            var tmpDate = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1);
            var blockRegDate = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.HasValue ? RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.Value : DateTime.MinValue;
            if (tmpDate <= blockRegDate)
                tmpDate = blockRegDate.AddDays(1);

            gvAutFerPerEdit.ClientSideEvents.CustomButtonClick = "OnCustomButtonClick";

            PowerWebService.FillGridLabels(EntityType, gvAutFerPerEdit);
            PowerWebService.FillComboboxes(gvAutFerPerEdit);

            //PowerWebService.FillComboboxes(cmbCol_Id, CommonService.GetPropertyName(() => _regVStub.Col_Id), true);

            // se il prefiltro è applicato allora i combo sono ripopolati
            if (IsPreFilterApplied)
            {
                //cmbCol_Id.ItemsRequestedByFilterCondition += cmbCol_Id_ItemsRequestedByFilterCondition;
                //cmbCol_Id.ItemRequestedByValue += cmbCol_Id_ItemRequestedByValue;
            }   //

            // verifico la personalizzazione per la visualizzazione del bottone di correzione automatica
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowAutomaticCorrectionButtonEnum);

            // si visualizza il flag di entrata/uscita solamente se è previsto dalle personalizzazioni
            if (customizationVersion == (int)ShowAutomaticCorrectionButtonEnum.Enable)
            {

                //BtnPropostaChiusura.Visible = true;
                //CbChiusuraAllSelected.Visible = true;

            }

            // bind del combobox di gestione dei collaboratori nel pre-filtro
            //PowerWebService.FillComboboxes(CmbColPreFilter, "Search_Col_Id");

            //if (!CmbColPreFilter.ReadOnly)
            //{
            //    EditButton btnEdit = new EditButton("X");
            //    CmbColPreFilter.Buttons.Add(btnEdit);
            //
            //    CmbColPreFilter.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
            //}

            // se il modulo del notturno risulta abilitato, come prima colonna viene visualizzata la data reg
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && (RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight || RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.Duration))
            {
                gvAutFerPerEdit.Columns["Data_Reg"].Visible = true;
                gvAutFerPerEdit.Columns["Data_Reg"].VisibleIndex = 0;
            }
        }

        #endregion

        #region Reset session

        public override void ResetSession()
        {
            base.ResetSession();
            ResetDataSourceAndBind();
        }

        private void ResetDataSourceAndBind(bool forceBind = false)
        {
            ColDataSource = null;
            AllErrDatesByColId = null;
            CachedRegVs = null;
            BlockDate = null;
            RegVsToAdd = null;
            RegVsToUpdate = null;
            RegVsToDelete = null;
            BindGrid(forceBind);
        }

        #endregion

        #region Manage ComboBox

        private void cmbCol_Id_ItemsRequestedByFilterCondition(object source, ListEditItemsRequestedByFilterConditionEventArgs e)
        {
            ASPxComboBox comboBox = (ASPxComboBox)source;

            BindCmbColId(e.Filter, e.BeginIndex, e.EndIndex, comboBox);
        }

        private void BindCmbColId(string filter, int beginIndex, int endIndex, ASPxComboBox comboBox)
        {
            String searchName = comboBox.ClientInstanceName;

            // se ho già effettuato un primo salvataggio allora non recupero i dati modificati dal data source completo ma li recupero
            // dal data source dei soli dati modificati
            var cmbDataSource = RepoManager.Tab_GridLookupRepo.SearchByFieldAndValue(PowerWebService.TabGridLookups, searchName,
                filter, beginIndex, endIndex, PrimoSalvataggio ? EditedCols.AsQueryable() : ColDataSource);

            comboBox.DataSource = cmbDataSource;

            comboBox.DataBindItems();
        }

        private void cmbCol_Id_ItemRequestedByValue(object source, ListEditItemRequestedByValueEventArgs e)
        {
            if (e.Value == null || String.IsNullOrEmpty(e.Value.ToString()))
                return;

            ASPxComboBox comboBox = (ASPxComboBox)source;

            String searchName = comboBox.ClientInstanceName;

            // se ho già effettuato un primo salvataggio allora non recupero i dati modificati dal data source completo ma li recupero
            // dal data source dei soli dati modificati
            var cmbDataSource = RepoManager.Tab_GridLookupRepo.SearchByFieldAndValue(PowerWebService.TabGridLookups, searchName, e.Value.ToString(), dataSource: PrimoSalvataggio ? EditedCols.AsQueryable() : ColDataSource);

            comboBox.DataSource = cmbDataSource;

            comboBox.DataBindItems();
        }

        private void BindCmbData()
        {
            //if (cmbCol_Id.Value != null)
            //{
            //    var selectedColId = Convert.ToInt32(cmbCol_Id.Value);
            //
            //    if (selectedColId != 0)
            //    {
            //        if (AllErrDatesByColId != null)
            //        {
            //            var dateDataSource = AllErrDatesByColId.ContainsKey(selectedColId) ? AllErrDatesByColId[selectedColId] : null;
            //
            //            cmbData_Reg.DataSource = dateDataSource;
            //            cmbData_Reg.DataBindItems();
            //        }
            //    }
            //}
            //else
            //{
            //    cmbData_Reg.Value = null;
            //    cmbData_Reg.SelectedItem = null;
            //    cmbData_Reg.DataSource = null;
            //    cmbData_Reg.DataBindItems();
            //}
        }

        #endregion

        #region DataBinding

        protected void gvAutFerPerEdit_DataBinding(object sender, EventArgs e)
        {
            gvAutFerPerEdit.KeyFieldName = KEYFIELDNAME;
            LinqServerModeDataSource serverMode = new LinqServerModeDataSource();
            serverMode.ContextTypeName = "PowerWebEntities.Data";
            serverMode.TableName = "Reg_V";

            GridView.DataSource = serverMode;

            serverMode.Selecting += linq_Selecting;
        }

        private void linq_Selecting(object sender, LinqServerModeDataSourceSelectEventArgs e)
        {
            e.KeyExpression = KEYFIELDNAME;

            IQueryable<Reg_V> currQueryable = Enumerable.Empty<Reg_V>().AsQueryable();

            //if (IsToPopulateGrid)
            //{
                //if (cmbCol_Id.Value != null && cmbData_Reg.Value != null)
                //{
                    //var colId = Convert.ToInt32(cmbCol_Id.Value);

                    //var regDate = Convert.ToDateTime(cmbData_Reg.Value);

                    //if (colId != 0 && regDate != DateTime.MinValue)
                    //{
                        //se vi sono reg allora abilito il pulsante di correzione automatica
                        //BtnPropostaChiusura.Enabled = true;
                        currQueryable = RegVDataSource;
                    //}
                //}
            //}

            e.QueryableSource = currQueryable;
        }

        private void BindGrid(bool forceBind = false)
        {
            if (IsToPopulateGrid || forceBind)
                gvAutFerPerEdit.DataBind();
        }

        #endregion

        #region Init Grid Combox and Grid Field

        protected void cbmxCol_Id_Init(object sender, EventArgs e)
        {
            cbmx_Init(sender, e, CommonService.GetPropertyName(() => _regVStub.Col_Id));
        }

        protected void cbmxCant_Id_Init(object sender, EventArgs e)
        {
            cbmx_Init(sender, e, CommonService.GetPropertyName(() => _regVStub.Cant_Id));
        }

        private void cbmx_Init(object sender, EventArgs e, string fieldName)
        {
            ASPxComboBox cmbx = sender as ASPxComboBox;
            if (cmbx != null)
            {
                PowerWebService.FillComboboxes(cmbx, fieldName);
            }
        }

        protected void cbmxMot_Id_Init(object sender, EventArgs e)
        {
            cbmx_Init(sender, e, CommonService.GetPropertyName(() => _regVStub.Motivazione_Reg_Id));
        }

        //Gestione ottimizzata componente calendario in griglia di Edit multiplo
        protected void de_Init(object sender, EventArgs e)
        {
            ASPxDateEdit deData_Fig_Reg = sender as ASPxDateEdit;
            deData_Fig_Reg.PopupCalendarOwnerID = "__ReferenceDateEdit";
        }

        #endregion

        #region Manage Errors

        private void SetErrorMessageDictionary(Reg_V regv, Dictionary<string, IList<Dictionary<string, string>>> errorByColId, Dictionary<string, string> validationErrors)
        {
            var colKey = string.Format("{0}|{1} {2} {3} {4} {5}", regv.Col_Id, regv.Col_Desc, regv.Cant_Mnemonic, regv.Cant_Desc, regv.Data_Reg.Value.ToShortDateString(), regv.Data_Ora_Fis_E.ToShortTimeString());
            if (errorByColId.ContainsKey(colKey))
            {
                var currentValidationErrorsList = errorByColId[colKey];
                currentValidationErrorsList.Add(validationErrors);
                errorByColId[colKey] = currentValidationErrorsList;
            }
            else
            {
                IList<Dictionary<string, string>> currentValidationErrorsList = new List<Dictionary<string, string>>();
                currentValidationErrorsList.Add(validationErrors);
                errorByColId.Add(colKey, currentValidationErrorsList);
            }
        }

        private string GetErrorMessageFromDictionary(Dictionary<string, IList<Dictionary<string, string>>> errorsByColId)
        {
            var sb = new StringBuilder();

            foreach (KeyValuePair<string, IList<Dictionary<string, string>>> errorByColId in errorsByColId)
            {
                sb.Append(errorByColId.Key).AppendLine();
            }

            return sb.ToString();
        }

        #endregion

        #region Eventi filter panel

        /// <summary>
        /// Filters the panel_ callback.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Correction type not valid
        /// or
        /// Correction param not configured
        /// or
        /// Correction param not configured
        /// or
        /// Correction type not valid
        /// or
        /// Correction type not valid
        /// or
        /// Correction param not configured
        /// or
        /// Correction param not configured
        /// or
        /// Correction type not valid
        /// </exception>
        protected void filterPanel_Callback(object sender, CallbackEventArgsBase e)
        {
        //
        //    if (e.Parameter.StartsWith("colIdChanged"))
        //   {
        //       BindCmbData();
        //       var cmbDSource = cmbData_Reg.DataSource as IEnumerable;
        //       if (cmbDSource != null && cmbDSource.AsQueryable().Any())
        //       {
        //           cmbData_Reg.SelectedIndex = 0;
        //           BindGrid();
        //       }
        //   }
        //    else if (e.Parameter.StartsWith("includeReg"))
        //   {
        //       ResetDataSourceAndBind();
        //   }
        //    else if (e.Parameter.StartsWith("dataRegChanged"))
        //    {
        //        BindCmbData();
        //        BindGrid();
        //    }
        //    else if (e.Parameter.StartsWith("undo"))
        //    {
        //        // nell'annullamento azzero la visualizzazione del modulo
        // 
        //        IsPreFilterApplied = false;
        //        ResetDataSourceAndBind();
        //        cmbCol_Id.SelectedItem = null;
        //        cmbCol_Id.Value = null;
        // 
        //        BindCmbData();
        //        filterPanel.JSProperties.Add("cpCallBackParameter", e.Parameter);
        //    }
        //    else if (e.Parameter.StartsWith("PreFilterSelected"))
        //   {
        //       // 1. Resetto la sessione di modo da calcolare tutti i data source
        //       // Successivamente, se è stato applicato il prefiltro e non era già attivo:
        //       // 2. vado con il caricamento dei collaboratori/date con registrazioni errate
        //       // 3. Inizializzo i combo di ricerca e della griglia
        //       // in ogni caso alla fine
        //       // 4. Cancello i valori selezionati dal combo dei collaboratori
        //       // 5. Ricalcolo i valori del combobox
        //       // 6. Svuoto i valori di data selezionato
        //       // 7. Effettuo il bind della griglia
        //       ResetDataSourceAndBind(true);
        //
        //       // quando viene premuto il tasto applica si svuotano le proprietà di modifica dei collaboratori
        //       EditedCols = null;
        //
        //       // alla pressione del tasto di applicazione dei filtri si torna allo stato originario,
        //       // come se non fosse stato effettuato nessun salvataggio
        //       PrimoSalvataggio = false;
        //
        //       if (!IsPreFilterApplied)
        //       {
        //           IsPreFilterApplied = true;
        //
        //           cmbCol_Id.ItemsRequestedByFilterCondition += cmbCol_Id_ItemsRequestedByFilterCondition;
        //           cmbCol_Id.ItemRequestedByValue += cmbCol_Id_ItemRequestedByValue;
        //
        //           PowerWebService.FillGridLabels(EntityType, gvAutFerPerEdit);
        //       }
        //
        //       cmbCol_Id.SelectedItem = null;
        //       cmbCol_Id.Value = null;
        //
        //       // viene impostato come default il valore del primo collaboratore in lista, se presente
        //       if (ColDataSource.Any())
        //           cmbCol_Id.Value = ColDataSource.FirstOrDefault().Col_Id;
        //
        //       BindCmbColId(String.Empty, 0, ColDataSource.Count(), cmbCol_Id);
        //       BindCmbData();
        //
        //       // se è presente una data per il collaboratore attualmente selezionato
        //       // allora viene impostato come default del combo data appena bindato
        //       if (AllErrDatesByColId.ContainsKey(Convert.ToInt32(cmbCol_Id.Value)))
        //           cmbData_Reg.Value = AllErrDatesByColId[Convert.ToInt32(cmbCol_Id.Value)].FirstOrDefault();
        //
        //       BindGrid();
        //
        //   }
        //    else if (e.Parameter.StartsWith("endElaborate"))
        //  {
        //      #region Calcolo codice collaboratore da verificare e riproprorre
        //        //      // prima di effettuare il reset della session, se sono stati modificati/aggiunti dei dati
        //      // allora si recupera il primo id utilizzato per poi eventuale riproporlo successivamente
        //      // al ricalcolo degli errori (in caso non siano stati aggiornati/inseriti dati si ritenta con il collaboratore corrente)
        //      int currColId = 0;
        //        //      // calcolo della lista delle reg modificate e inserite
        //      var regVsToCheck = RegVsToUpdate;
        //      regVsToCheck.AddRange(RegVsToAdd);
        //      regVsToCheck.AddRange(RegVsToDelete);
        //        //      // recupero dell'eventuale primo collaboratore da questa lista
        //      if (regVsToCheck.Any())
        //          currColId = Convert.ToInt32(regVsToCheck.OrderBy(regV => regV.Col_Mnemonic).Distinct().Select(regV => regV.Col_Id).FirstOrDefault());
        //      else
        //          currColId = Convert.ToInt32(cmbCol_Id.Value);
        //        //      #endregion
        //        //      cmbCol_Id.SelectedItem = null;
        //      cmbCol_Id.Value = null;
        //      BindCmbData();
        //      ResetDataSourceAndBind(true);
        //        //      #region Gestione calcolo ripartenza post elaborazione
        //        //      if (PrimoSalvataggio)
        //      {
        //          SetColDataSourceLabelText();
        //        //          // se è già stato effettuato il primo salvataggio allora sono puliti dall'elenco dei collaboratori/data
        //          // tutto ciò che, secondo i criteri di pre-filtro non ha più errore
        //          CleanEditedWithNoErrors();
        //        //          // se non sono rimasti più collaboratori dopo il primo salvataggio
        //          // allora si procede al ritorno a come si fosse aperta per la prima volta la pagina
        //          if (!EditedCols.Any())
        //          {
        //              IsPreFilterApplied = false;
        //        //              // impostazione della proprietà per il nascondimento della griglia
        //              filterPanel.JSProperties["cpCallBackParameter"] = "undo";
        //          }
        //      }
        //        //      // se non ci sono i presupposti per proesguire con l'elaborazione (e cioè se dopo il primo salvataggio non ci
        //      // sono più collaboratori in errore), non si eseguono le seguenti operazioni
        //      //if (!IsPreFilterApplied)
        //      //{
        //      // una volta ricalcolati i valori della griglia è verificato se il collaboratore ha ancora errori
        //      // (il valore è recuperato dall'elenco dei modificati in caso di primo salvataggio avvenuto)
        //      if (!PrimoSalvataggio)
        //          cmbCol_Id.Value = ColDataSource.Any(col => col.Col_Id == currColId) ? currColId : ColDataSource.FirstOrDefault().Col_Id;
        //      else
        //          cmbCol_Id.Value = EditedCols.Any() ? EditedCols.FirstOrDefault().Col_Id : (object)null;
        //        //      // effettuazione del bind del combo dei collaboratori
        //      BindCmbColId(String.Empty, 0, PrimoSalvataggio ? EditedCols.Count : ColDataSource.Count(), cmbCol_Id);
        //        //      // se è stato selezionato un nuovo collaboratore
        //      if (cmbCol_Id.Value != null)
        //      {
        //          // recupero del codice collaboratore impostato
        //          currColId = Convert.ToInt32((cmbCol_Id.Value));
        //        //          if (AllErrDatesByColId.ContainsKey(currColId))
        //          {
        //              // ricalcolo delle date per il collaboratore selezionato
        //              BindCmbData();
        //        //              // selezione della prima data disponibile per il collaboratore
        //              cmbData_Reg.Value = AllErrDatesByColId[currColId].First();
        //        //          }
        //        //          // rieffettuo il bind della griglia
        //          BindGrid();
        //      }
        //        //      #endregion
        //        //        //  }
        //    else if (e.Parameter.StartsWith("goToPreviousCol"))
        //   {
        //       var curColId = Convert.ToInt32(cmbCol_Id.Value);
        //       var currentColList = PrimoSalvataggio ? EditedCols.Select(col => col.Col_Id).ToList() : ColDataSource.Select(col => col.Col_Id).ToList();
        //
        //       if (currentColList.Any())
        //       {
        //
        //           var index = currentColList.IndexOf(curColId) - 1;
        //
        //           //if (index >= 0)
        //           //    cmbCol_Id.Value = currentColList[index];
        //           //else
        //           //    cmbCol_Id.Value = currentColList.Last();
        //           //
        //           //cmbCol_Id.DataBindItems();
        //           filterPanel.JSProperties.Add("cpCallBackParameter", "previousCol");
        //       }
        //   }
        //    else if (e.Parameter.StartsWith("goToNextCol"))
        //   {
        //       var curColId = Convert.ToInt32(cmbCol_Id.Value);
        //       var currentColList = PrimoSalvataggio ? EditedCols.Select(col => col.Col_Id).ToList() : ColDataSource.Select(col => col.Col_Id).ToList();
        //
        //       if (currentColList.Any())
        //       {
        //           var index = currentColList.IndexOf(curColId) + 1;
        //
        //           //if (index <= currentColList.Count - 1)
        //           //    cmbCol_Id.Value = currentColList[index];
        //           //else
        //           //    cmbCol_Id.Value = currentColList[0];
        //           //
        //           //cmbCol_Id.DataBindItems();
        //           filterPanel.JSProperties.Add("cpCallBackParameter", "nextCol");
        //       }
        //   }
        //    else if (e.Parameter.StartsWith("automaticCorrection"))
        //    {
        //        #region Gestione delle correzioni alle Reg_V
        //
        //        var customizationVersion = (AutomaticCorrectionEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutomaticCorrectionEnum);
        //
        //        bool isAllSelected = Convert.ToBoolean(e.Parameter.Substring(e.Parameter.IndexOf("#") + 1));
        //
        //        // se non devo chiudere tutte le registrazioni selezionate, tratto solo il giorno/collaboratore visualizzato
        //        if (!isAllSelected)
        //        {
        //
        //            #region Chiusura delle Reg_V per il collaboratore/giorno visualizzato
        //
        //            IQueryable<Reg_V> retRegVs = null;
        //
        //            // in base al tipo di personalizzazione si richiama la specifica funzione di correzione (che restituisce un IQueryable di Reg_V) da integrare con il data
        //            // source esistente
        //            switch (customizationVersion)
        //            {
        //                case AutomaticCorrectionEnum.None:
        //                    throw new InvalidOperationException("Correction type not valid");
        //                case AutomaticCorrectionEnum.Add2Minute:
        //                    string minutesStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutomaticCorrectionEnum, "MinutesNumber");
        //                    if (minutesStr == String.Empty)
        //                        throw new InvalidOperationException("Correction param not configured");
        //
        //                    //retRegVs = BusinessService.ProposeRegsAddingMinutes(CachedRegVs[GetCachedRegVsKey(Convert.ToInt32(cmbCol_Id.Value), cmbData_Reg.Value.ToString())], Convert.ToInt32(minutesStr));
        //                    break;
        //                case AutomaticCorrectionEnum.HistoryBased:
        //                    string dayMinutesTolleranceStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutomaticCorrectionEnum, "DayMinutesTollerance"); ;
        //                    string averageMinuteTolleranceStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutomaticCorrectionEnum, "AverageMinutesTollerance"); ;
        //                    string historyDayStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutomaticCorrectionEnum, "HistoryDay"); ;
        //                    string averageBaseThresholdStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutomaticCorrectionEnum, "AverageBaseThreshold"); ;
        //                    if (dayMinutesTolleranceStr == String.Empty || averageMinuteTolleranceStr == String.Empty || historyDayStr == String.Empty || averageBaseThresholdStr == String.Empty)
        //                        throw new InvalidOperationException("Correction param not configured");
        //
        //
        //                    //retRegVs = BusinessService.ProposeRegsBasedOnHistory(CachedRegVs[GetCachedRegVsKey(Convert.ToInt32(cmbCol_Id.Value), cmbData_Reg.Value.ToString())]
        //                    //    , Convert.ToInt32(dayMinutesTolleranceStr)
        //                    //    , Convert.ToInt32(averageMinuteTolleranceStr)
        //                    //    , Convert.ToInt32(historyDayStr)
        //                    //    , Convert.ToInt32(averageBaseThresholdStr));
        //                    break;
        //                default:
        //                    throw new InvalidOperationException("Correction type not valid");
        //            }
        //
        //            // integrazione del data source restituito "chiuso" con l'esistente
        //            var retRegVsIdsList = retRegVs.Select(regv => regv.RegE).ToList();
        //            var cachedList = CachedRegVs[GetCachedRegVsKey(Convert.ToInt32(cmbCol_Id.Value), cmbData_Reg.Value.ToString())].ToList();
        //            CachedRegVs[GetCachedRegVsKey(Convert.ToInt32(cmbCol_Id.Value), cmbData_Reg.Value.ToString())] = cachedList.Where(regV => !retRegVsIdsList.Contains(regV.RegE)).AsQueryable().Concat(retRegVs);
        //
        //            //per ogni regv vado a metterla nella lista delle rag da aggiornare 
        //            PutRegVsInUpdatedList(retRegVs);
        //
        //            // alla fine dell'elaborazione segnalo la stringa di messaggio da visualizzare con il numero delle registrazioni e dei collaboratori modificate
        //            SetPostCorrectionMessage(String.Empty);
        //
        //            #endregion
        //
        //        }
        //        else
        //        {
        //
        //            #region Chiusura delle Reg_v per tutti i collaboratori/giorno trovati
        //
        //            // inizializzazione delle variabili che tengono conto del numero di elementi modificati
        //            var modifiedColIds = new List<int>();
        //            int modifiedRegs = 0;
        //
        //            // per ogni collaboratore recuperato dalla selezione
        //            foreach (var col in ColDataSource)
        //            {
        //                var currentCol = col;
        //
        //                // se per il collaboratore che si sta processando sono presenti delle date in errore
        //                if (AllErrDatesByColId.Any(colDate => colDate.Key == currentCol.Col_Id))
        //                {
        //                    // per ogni data in errore per il collaboratore in processo
        //                    foreach (var errDate in AllErrDatesByColId.FirstOrDefault(colDate => colDate.Key == currentCol.Col_Id).Value)
        //                    {
        //                        // recupero tutte le reg per quel giorno/collaboratore
        //                        var dateToSearch = Convert.ToDateTime(errDate, PowerWebContext.Current.UserCultureInfo);
        //                        var colDateRegVs = RepoManager.Reg_VRepo.Find(regv => regv.Col_Id == currentCol.Col_Id && regv.Data_Reg == dateToSearch).AsQueryable();
        //
        //                        // se ho trovato delle reg_v da processare
        //                        if (colDateRegVs.Any())
        //                        {
        //                            IQueryable<Reg_V> correctedRegVs = null;
        //
        //                            // in base al tipo di personalizzazione si richiama la specifica funzione di correzione (che restituisce un IQueryable di Reg_V) da integrare con il data
        //                            // source esistente
        //                            switch (customizationVersion)
        //                            {
        //                                case AutomaticCorrectionEnum.None:
        //                                    throw new InvalidOperationException("Correction type not valid");
        //                                case AutomaticCorrectionEnum.Add2Minute:
        //                                    string minutesStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutomaticCorrectionEnum, "MinutesNumber");
        //                                    if (minutesStr == String.Empty)
        //                                        throw new InvalidOperationException("Correction param not configured");
        //
        //                                    correctedRegVs = BusinessService.ProposeRegsAddingMinutes(colDateRegVs, Convert.ToInt32(minutesStr));
        //                                    break;
        //                                case AutomaticCorrectionEnum.HistoryBased:
        //                                    string dayMinutesTolleranceStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutomaticCorrectionEnum, "DayMinutesTollerance"); ;
        //                                    string averageMinuteTolleranceStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutomaticCorrectionEnum, "AverageMinutesTollerance"); ;
        //                                    string historyDayStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutomaticCorrectionEnum, "HistoryDay"); ;
        //                                    string averageBaseThresholdStr = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.AutomaticCorrectionEnum, "AverageBaseThreshold"); ;
        //                                    if (dayMinutesTolleranceStr == String.Empty || averageMinuteTolleranceStr == String.Empty || historyDayStr == String.Empty || averageBaseThresholdStr == String.Empty)
        //                                        throw new InvalidOperationException("Correction param not configured");
        //
        //                                    correctedRegVs = BusinessService.ProposeRegsBasedOnHistory(colDateRegVs
        //                                        , Convert.ToInt32(dayMinutesTolleranceStr)
        //                                        , Convert.ToInt32(averageMinuteTolleranceStr)
        //                                        , Convert.ToInt32(historyDayStr)
        //                                        , Convert.ToInt32(averageBaseThresholdStr));
        //                                    break;
        //                                default:
        //                                    throw new InvalidOperationException("Correction type not valid");
        //                            }
        //
        //                            // Metto in cache le reg_v corrette e non corrette di modo da visualizzare la nuova situazione in griglia
        //                            var correctedRegVsIdsList = correctedRegVs.Select(regv => regv.RegE).ToList();
        //                            CachedRegVs[GetCachedRegVsKey(currentCol.Col_Id, errDate)] = correctedRegVs.Concat(colDateRegVs.Where(regv => !correctedRegVsIdsList.Contains(regv.RegE)).AsQueryable());
        //
        //                            // le registrazioni modificate per la correzione sono inserite all'interno dell'elenco di registrazioni modiifcate a mano (se già presenti sono sovrascritte)
        //                            PutRegVsInUpdatedList(correctedRegVs);
        //
        //                            // incremento della variabile che tiene traccia del numero di reg_v modificate
        //                            modifiedRegs += correctedRegVs.Count();
        //
        //                            // aggiorno la lista dei collaboratori modificati se l'attualmente in elaborazione non è già presente
        //                            if (modifiedColIds.All(colId => colId != currentCol.Col_Id))
        //                                modifiedColIds.Add(currentCol.Col_Id);
        //                        }
        //                    }
        //                }
        //            }
        //
        //            // alla fine dell'elaborazione segnalo la stringa di messaggio da visualizzare con il numero delle registrazioni e dei collaboratori modificate
        //            SetPostCorrectionMessage(BusinessService.GetLocalizedStringStrParam(PowerWebResources.STR_MODIFICATI_X_REGISTRAZIONI_SU_Y_COLLABORATORI, modifiedRegs.ToString(), modifiedColIds.Count().ToString()));
        //
        //            #endregion
        //
        //        }
        //
        //
        //        //vado a mettere tutto nella griglia
        //        BindGrid(true);
        //
        //        #endregion
        //    }
        }

        #endregion

        #region Eventi di modifica e aggiornamento della griglia

        protected void gvAutFerPerEdit_BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        //    // Si prosegue con l'elaborazione solamente se si è in on e se sono stati selezionati entrambi i valori della combo.
        //    // In caso contrario non si esegue alcuna operazione
        //    if (IsToPopulateGrid && cmbCol_Id.Value != null && cmbData_Reg != null)
        //    {
        //        var returnMessage = new Dictionary<string, string>();
        //
        //        IList<Reg_V> currentAddedReg = new List<Reg_V>();
        //        IList<Reg_V> currentUpdatedReg = new List<Reg_V>();
        //        IList<Reg_V> currentDeletedReg = new List<Reg_V>();
        //
        //        // calcolo della chiave di cache
        //        //var cachedKey = GetCachedRegVsKey(Convert.ToInt32(cmbCol_Id.Value), cmbData_Reg.Value.ToString());
        //
        //        #region Inserimento nuove registrazioni
        //
        //        // gestione dei valori inseriti
        //        foreach (var regV in e.InsertValues)
        //        {
        //            // si processano in inserimento solamente le righe che hanno impostata un'ora in ingresso
        //            if (regV.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_E)] != null)
        //            {
        //                // inizializzazione di una nuova reg_v
        //                var currRegV = RepoManager.Reg_VRepo.Init();
        //
        //                // compilo la nuova regv con i valori inseriti
        //                PowerWebService.FillEntityProperties(currRegV, regV.NewValues);
        //
        //                // per tutte le reg nuove viene impostata la data/ora di registrazione (E/U) prendendo la data visualizzata e l'ora di registrazione fisica inserita
        //                var showedDate = Convert.ToDateTime(cmbData_Reg.Value);
        //                currRegV.Data_Reg = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day);
        //                currRegV.Data_Ora_Fis_E = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day,
        //                    currRegV.Data_Ora_Fis_E.Hour, currRegV.Data_Ora_Fis_E.Minute, currRegV.Data_Ora_Fis_E.Second);
        //                if (currRegV.Data_Ora_Fis_U != null)
        //                    currRegV.Data_Ora_Fis_U = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day,
        //                        currRegV.Data_Ora_Fis_U.Value.Hour, currRegV.Data_Ora_Fis_U.Value.Minute,
        //                        currRegV.Data_Ora_Fis_U.Value.Second);
        //
        //                // inserimento del id del collaboratore
        //                currRegV.Col_Id = Convert.ToInt32(cmbCol_Id.Value);
        //
        //                // inserimento dell'id identificativo utilizzato per collegare modifiche e cancellazioni
        //                // a elementi inseriti da questa maschera ma non ancora salvati su database
        //                currRegV.GenerateTmpId();
        //
        //                // inserimento dei valori nell'elenco delle reg_v da inserire e nelle reg_v su cui costruire
        //                // la chache
        //                currentAddedReg.Add(currRegV);
        //                RegVsToAdd.Add(currRegV);
        //
        //                // ogni volta che viene processata una reg si inserisce, se non già presente,
        //                // il collaboratore nell'elenco dei collaboratori processati
        //                SaveInEditedCols(Convert.ToInt32(currRegV.Col_Id));
        //            }
        //        }
        //
        //        #endregion
        //
        //        // caching del giorno per i dati inseriti (solo se presenti)
        //        if (currentAddedReg.Count > 0)
        //            CachedRegVs[cachedKey] = CachedRegVs[cachedKey].Concat(currentAddedReg);
        //
        //        #region Aggiornamento registrazioni esistenti
        //
        //        // gestione dei valori aggiornati
        //        foreach (var regV in e.UpdateValues)
        //        {
        //            // si processano in inserimento solamente le righe che hanno impostata un'ora in ingresso
        //            if (regV.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_E)] != null)
        //            {
        //                // calcolo dell'id della reg 
        //                int regE = Convert.ToInt32(regV.Keys[CommonService.GetPropertyName(() => _regVStub.RegE)]);
        //                // calcolo dell'id temporaneo della reg se precedentemente inserita
        //                string tmpNewId = regV.Keys[CommonService.GetPropertyName(() => _regVStub.TmpNewId)].ToString();
        //
        //                // recupero della regV in base all'id se già precedentemente presente e in base all'id temporaneo se inserita in questa maschera
        //                var currRegV = regE != 0
        //                    ? RegVDataSource.First(reg => reg.RegE == regE)
        //                    : RegVDataSource.First(reg => reg.TmpNewId == tmpNewId);
        //
        //                // si procede con l'aggiornamento dei dati della regv (new values e cambio cant col)
        //                // solamente se non si tratta di un viaggio
        //                if (currRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip)
        //                {
        //                    // si imposta il cant_id eventualmente modificato dall'utente
        //                    // se nei valori aggiornati è stato cambiato il cant_id allora procedo all'azzeramento del corrispondente Fru_Id
        //                    // così se nei valori aggiornati è stato cambiato il col_id allora procedo all'azzeramento del corrispondente Pru_Id
        //                    PowerWebService.FillEntityProperties(currRegV, regV.NewValues);
        //                    RepoManager.Reg_VRepo.ManageCantColChangesBeforeUpdate(currRegV);
        //                }
        //
        //                // non si elaborano le modifiche ai viaggi
        //                if (currRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip)
        //                {
        //
        //                    // si forza in ogni caso le date/ore utili alla modifica al giorno selezionato in combobox
        //                    var showedDate = Convert.ToDateTime(cmbData_Reg.Value);
        //                    currRegV.Data_Reg = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day);
        //                    currRegV.Data_Ora_Fis_E = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day,
        //                        currRegV.Data_Ora_Fis_E.Hour, currRegV.Data_Ora_Fis_E.Minute, currRegV.Data_Ora_Fis_E.Second);
        //                    if (currRegV.Data_Ora_Fis_U != null)
        //                        currRegV.Data_Ora_Fis_U = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day,
        //                            currRegV.Data_Ora_Fis_U.Value.Hour, currRegV.Data_Ora_Fis_U.Value.Minute,
        //                            currRegV.Data_Ora_Fis_U.Value.Second);
        //
        //
        //                    PutRegVsInUpdatedList((new List<Reg_V>() { currRegV }).AsQueryable());
        //                    currentUpdatedReg.Add(currRegV);
        //
        //                    // ogni volta che viene processata una reg si inserisce, se non già presente,
        //                    // il collaboratore nell'elenco dei collaboratori processati
        //                    SaveInEditedCols(Convert.ToInt32(currRegV.Col_Id));
        //                }
        //                else
        //                {
        //                    // se è satao modificato un viaggio si ritorna un messagio di ritorno all'utente
        //                    returnMessage.Add("Trip", String.Format("I viaggi non posso essere modificati e quindi le modifiche apportate ai quei dati non sono state salvate."));
        //                }
        //            }
        //        }
        //
        //        #endregion
        //
        //        // caching del giorno per i dati aggiornati (solo se sono stati raccolti dei dati)
        //        if (currentUpdatedReg.Count > 0)
        //        {
        //            CachedRegVs[cachedKey] =
        //                CachedRegVs[cachedKey].Where(
        //                    reg =>
        //                        !currentUpdatedReg.Select(cReg => cReg.RegE).Where(regE => regE != 0).Contains(reg.RegE));
        //            CachedRegVs[cachedKey] =
        //                CachedRegVs[cachedKey].Where(reg => !currentUpdatedReg.Select(cReg => cReg.TmpNewId)
        //                    .Where(tmpNewId => tmpNewId != String.Empty)
        //                    .Contains(reg.TmpNewId));
        //            CachedRegVs[cachedKey] = CachedRegVs[cachedKey].Concat(currentUpdatedReg);
        //        }
        //
        //        #region Cancellazione registrazioni eliminate
        //
        //        // gestione dei valori eliminati
        //        foreach (var regV in e.DeleteValues)
        //        {
        //            // recupero della registrazione marcata per l'eliminazione
        //            int regE = Convert.ToInt32(regV.Keys[CommonService.GetPropertyName(() => _regVStub.RegE)]);
        //            var regVToDelete = RegVDataSource.First(reg => reg.RegE == regE);
        //
        //            // se la registrazione non è già marcata per l'eliminazione allora la segno come cancellata
        //            if (
        //                !RegVsToDelete.Any(
        //                    reg => reg.RegE != 0 ? reg.RegE == regVToDelete.RegE : reg.TmpNewId == regVToDelete.TmpNewId))
        //            {
        //                currentDeletedReg.Add(regVToDelete);
        //                RegVsToDelete.Add(regVToDelete);
        //
        //                // se la reg in cancellazione è presente tra le reg da aggiornare e/o aggiornare
        //                // allora viene eliminata
        //                if (regVToDelete.RegE != 0) // se si tratta di una reg nuova
        //                {
        //                    // cancellazione della reg dalla lista di inserimento e aggiornamento
        //                    RegVsToUpdate = RegVsToUpdate.Where(regv => regv.RegE != regVToDelete.RegE).ToList();
        //                    RegVsToAdd = RegVsToAdd.Where(regv => regv.RegE != regVToDelete.RegE).ToList();
        //                }
        //                else
        //                {
        //                    // cancellazione della reg dalla lista di inserimento e aggiornamento
        //                    RegVsToUpdate = RegVsToUpdate.Where(regv => regv.TmpNewId != regVToDelete.TmpNewId).ToList();
        //                    RegVsToAdd = RegVsToAdd.Where(regv => regv.TmpNewId != regVToDelete.TmpNewId).ToList();
        //                }
        //
        //                // ogni volta che viene processata una reg si inserisce, se non già presente,
        //                // il collaboratore nell'elenco dei collaboratori processati
        //                SaveInEditedCols(Convert.ToInt32(cmbCol_Id.Value));
        //            }
        //        }
        //
        //        #endregion
        //
        //        // caching del giorno per i dati cancellati (solo se sono stati trovati)
        //        if (currentDeletedReg.Count > 0)
        //        {
        //            CachedRegVs[cachedKey] =
        //                CachedRegVs[cachedKey].Where(
        //                    reg =>
        //                        !currentDeletedReg.Select(cReg => cReg.RegE).Where(regE => regE != 0).Contains(reg.RegE));
        //            CachedRegVs[cachedKey] =
        //                CachedRegVs[cachedKey].Where(reg => !currentDeletedReg.Select(cReg => cReg.TmpNewId)
        //                    .Where(tmpNewId => tmpNewId != String.Empty)
        //                    .Contains(reg.TmpNewId));
        //        }
        //
        //        // se ci sono dei messaggi da ritornare all'utente, li passo alla gestione degli errori
        //        if (returnMessage.Count > 0)
        //            throw new InvalidOperationException(CommonService.GetErrorMessageFromDictionary(returnMessage));
        //
        //    }
        }

        protected void gvAutFerPerEdit_OnCommandButtonInitialize(object sender, ASPxGridViewCommandButtonEventArgs e)
        {
            if (EditType != EditTypeErrModuleEnum.Inline)
                if (e.ButtonType == ColumnCommandButtonType.Update || e.ButtonType == ColumnCommandButtonType.Cancel)
                    e.Visible = false;
        }

        protected void gvAutFerPerEdit_OnRowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            //switch (EditType)
            //{
            //    case EditTypeErrModuleEnum.Inline:
            //        var cachedKey = GetCachedRegVsKey(Convert.ToInt32(cmbCol_Id.Value), cmbData_Reg.Value.ToString());
            //
            //        if (cachedKey != String.Empty && CachedRegVs.ContainsKey(cachedKey))
            //        {
            //            List<Reg_V> dayCached = CachedRegVs[cachedKey].ToList();
            //
            //            // calcolo dell'id della reg 
            //            int regE = Convert.ToInt32(e.Keys[CommonService.GetPropertyName(() => _regVStub.RegE)]);
            //            // calcolo dell'id temporaneo della reg se precedentemente inserita
            //            string tmpNewId = e.Keys[CommonService.GetPropertyName(() => _regVStub.TmpNewId)].ToString();
            //
            //            // recupero della regV in base all'id se già precedentemente presente e in base all'id temporaneo se inserita in questa maschera
            //            Reg_V editedRegV = regE != 0
            //                ? dayCached.First(reg => reg.RegE == regE)
            //                : dayCached.First(reg => reg.TmpNewId == tmpNewId);
            //
            //            // si procede con l'aggiornamento dei dati della regv (new values e cambio cant col e tipo modifica)
            //            // solamente se non si tratta di un viaggio
            //            if (editedRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip)
            //            {
            //                // si imposta il cant_id eventualmente modificato dall'utente
            //                // se nei valori aggiornati è stato cambiato il cant_id allora procedo all'azzeramento del corrispondente Fru_Id
            //                // così se nei valori aggiornati è stato cambiato il col_id allora procedo all'azzeramento del corrispondente Pru_Id
            //                PowerWebService.FillEntityProperties(editedRegV, e.NewValues);
            //                RepoManager.Reg_VRepo.ManageCantColChangesBeforeUpdate(editedRegV);
            //            }
            //
            //            // non si elaborano le modifiche ai viaggi
            //            if (editedRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip)
            //            {
            //                // si forza in ogni caso le date/ore utili alla modifica al giorno selezionato in combobox
            //                var showedDate = Convert.ToDateTime(cmbData_Reg.Value);
            //                editedRegV.Data_Reg = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day);
            //                editedRegV.Data_Ora_Fis_E = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day,
            //                    editedRegV.Data_Ora_Fis_E.Hour, editedRegV.Data_Ora_Fis_E.Minute, editedRegV.Data_Ora_Fis_E.Second);
            //                if (editedRegV.Data_Ora_Fis_U != null)
            //                    editedRegV.Data_Ora_Fis_U = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day,
            //                        editedRegV.Data_Ora_Fis_U.Value.Hour, editedRegV.Data_Ora_Fis_U.Value.Minute,
            //                        editedRegV.Data_Ora_Fis_U.Value.Second);
            //
            //
            //                PutRegVsInUpdatedList((new List<Reg_V>() { editedRegV }).AsQueryable());
            //
            //                // ogni volta che viene processata una reg si inserisce, se non già presente,
            //                // il collaboratore nell'elenco dei collaboratori processati
            //                SaveInEditedCols(Convert.ToInt32(editedRegV.Col_Id));
            //
            //                CachedRegVs[cachedKey] = dayCached.AsQueryable();
            //            }
            //        }
            //
            //
            //        break;
            //}
            //
            //gvAutFerPerEdit.CancelEdit();
            //e.Cancel = true;
        }

        protected void gvAutFerPerEdit_OnRowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            //switch (EditType)
            //{
            //    case EditTypeErrModuleEnum.Inline:
            //
            //        // recupero della registrazione marcata per l'eliminazione
            //        int regE = Convert.ToInt32(e.Keys[CommonService.GetPropertyName(() => _regVStub.RegE)]);
            //        var regVToDelete = RegVDataSource.First(reg => reg.RegE == regE);
            //
            //        // se la registrazione non è già marcata per l'eliminazione allora la segno come cancellata
            //        if (!RegVsToDelete.Any(reg => reg.RegE != 0 ? reg.RegE == regVToDelete.RegE : reg.TmpNewId == regVToDelete.TmpNewId))
            //        {
            //            // calcolo della cache per il processo
            //            var cachedKey = GetCachedRegVsKey(Convert.ToInt32(cmbCol_Id.Value), cmbData_Reg.Value.ToString());
            //            List<Reg_V> dayCached = CachedRegVs[cachedKey].ToList();
            //
            //            RegVsToDelete.Add(regVToDelete);
            //
            //            // se la reg in cancellazione è presente tra le reg da aggiornare e/o aggiornare
            //            // allora viene eliminata
            //            if (regVToDelete.RegE != 0) // se si tratta di una reg nuova
            //            {
            //                // cancellazione della reg dalla lista di inserimento e aggiornamento
            //                RegVsToUpdate = RegVsToUpdate.Where(regv => regv.RegE != regVToDelete.RegE).ToList();
            //                RegVsToAdd = RegVsToAdd.Where(regv => regv.RegE != regVToDelete.RegE).ToList();
            //
            //                // eliminazione della regv da cancellare dalla cache
            //                dayCached = dayCached.Where(regv => regv.RegE != regVToDelete.RegE).ToList();
            //            }
            //            else
            //            {
            //                // cancellazione della reg dalla lista di inserimento e aggiornamento
            //                RegVsToUpdate = RegVsToUpdate.Where(regv => regv.TmpNewId != regVToDelete.TmpNewId).ToList();
            //                RegVsToAdd = RegVsToAdd.Where(regv => regv.TmpNewId != regVToDelete.TmpNewId).ToList();
            //
            //                // eliminazione della regv da cancellare dalla cache
            //                dayCached = dayCached.Where(regv => regv.TmpNewId != regVToDelete.TmpNewId).ToList();
            //            }
            //
            //            // salvataggio del giorno modificato in cache
            //            CachedRegVs[cachedKey] = dayCached.AsQueryable();
            //        }
            //
            //        break;
            //}
            //
            //// ogni volta che viene processata una reg si inserisce, se non già presente,
            //// il collaboratore nell'elenco dei collaboratori processati
            //SaveInEditedCols(Convert.ToInt32(cmbCol_Id.Value));
            //
            //gvAutFerPerEdit.CancelEdit();
            //e.Cancel = true;
        }

            protected void gvAutFerPerEdit_OnRowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
           // switch (EditType)
           // {
           //     case EditTypeErrModuleEnum.Inline:
           //         // si processano in inserimento solamente le righe che hanno impostata un'ora in ingresso
           //         if (e.NewValues[CommonService.GetPropertyName(() => _regVStub.Data_Ora_Fis_E)] != null)
           //         {
           //             // inizializzazione di una nuova reg_v
           //             var currRegV = RepoManager.Reg_VRepo.Init();
           //
           //             // compilo la nuova regv con i valori inseriti
           //             PowerWebService.FillEntityProperties(currRegV, e.NewValues);
           //
           //             // per tutte le reg nuove viene impostata la data/ora di registrazione (E/U) prendendo la data visualizzata e l'ora di registrazione fisica inserita
           //             var showedDate = Convert.ToDateTime(cmbData_Reg.Value);
           //             currRegV.Data_Reg = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day);
           //             currRegV.Data_Ora_Fis_E = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day, currRegV.Data_Ora_Fis_E.Hour, currRegV.Data_Ora_Fis_E.Minute, currRegV.Data_Ora_Fis_E.Second);
           //             if (currRegV.Data_Ora_Fis_U != null)
           //                 currRegV.Data_Ora_Fis_U = new DateTime(showedDate.Year, showedDate.Month, showedDate.Day, currRegV.Data_Ora_Fis_U.Value.Hour, currRegV.Data_Ora_Fis_U.Value.Minute, currRegV.Data_Ora_Fis_U.Value.Second);
           //
           //             // inserimento del id del collaboratore
           //             currRegV.Col_Id = Convert.ToInt32(cmbCol_Id.Value);
           //
           //             // inserimento dell'id identificativo utilizzato per collegare modifiche e cancellazioni
           //             // a elementi inseriti da questa maschera ma non ancora salvati su database
           //             currRegV.GenerateTmpId();
           //
           //             // inserimento dei valori nell'elenco delle reg_v da inserire e nelle reg_v su cui costruire
           //             // la chache
           //             RegVsToAdd.Add(currRegV);
           //
           //             // ogni volta che viene processata una reg si inserisce, se non già presente,
           //             // il collaboratore nell'elenco dei collaboratori processati
           //             SaveInEditedCols(Convert.ToInt32(currRegV.Col_Id));
           //
           //             // aggiunta della nuova reg_v alla cache
           //             var cachedKey = GetCachedRegVsKey(Convert.ToInt32(cmbCol_Id.Value), cmbData_Reg.Value.ToString());
           //             List<Reg_V> dayCached = CachedRegVs[cachedKey].ToList();
           //             dayCached.Add(currRegV);
           //             CachedRegVs[cachedKey] = dayCached.AsQueryable();
           //         }
           //         break;
           // }
           //
           // gvAutFerPerEdit.CancelEdit();
           // e.Cancel = true;
        }

        protected void gvAutFerPerEdit_OnRowValidating(object sender, ASPxDataValidationEventArgs e)
        {

        }

        protected void gvAutFerPerEdit_OnInit(object sender, EventArgs e)
        {
            // recupero della griglia
            var editGrid = (ASPxGridView)sender;

            // recupero della colonna da processare
            GridViewColumn sameDayColumn = editGrid.Columns["IsUTimeSameDayE"];

            // recupero della colonna di cui adattare la percentuale di spazio
            GridViewColumn regTypeColumn = editGrid.Columns["Registrazione_Tipo_Reg"];

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

            // se è richiesto di visualizzare e gestire i secondi nella griglia allora procedo al cambio delle relative colonne
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowSecondsInHoursErrModuleEnum);
            if (customizationVersion == (int)ShowSecondsInHoursErrModuleEnum.ShowSeconds)
            {
                if (editGrid.Columns["Data_Ora_Fis_E"] != null)
                {
                    GridViewColumn regEHourColumn = editGrid.Columns["Data_Ora_Fis_E"];
                    ((GridViewDataDateColumn)regEHourColumn).PropertiesDateEdit.DisplayFormatString = "HH:mm:ss";
                    ((GridViewDataDateColumn)regEHourColumn).PropertiesDateEdit.EditFormat = EditFormat.Custom;
                    ((GridViewDataDateColumn)regEHourColumn).PropertiesDateEdit.EditFormatString = "HH:mm:ss";
                }
                if (editGrid.Columns["Data_Ora_Fis_U"] != null)
                {
                    GridViewColumn regUHourColumn = editGrid.Columns["Data_Ora_Fis_U"];
                    ((GridViewDataDateColumn)regUHourColumn).PropertiesDateEdit.DisplayFormatString = "HH:mm:ss";
                    ((GridViewDataDateColumn)regUHourColumn).PropertiesDateEdit.EditFormat = EditFormat.Custom;
                    ((GridViewDataDateColumn)regUHourColumn).PropertiesDateEdit.EditFormatString = "HH:mm:ss";
                }
            }


            // se è richiesto di visualizzare ed utilizzare le colonne indicanti il flag E/U allora le si visualizzano
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowFlagEUInFormEnum) == (int)ShowFlagEUInFormEnum.Show)
            {
                // inizializzazione delle colonne di entrata/uscita da visualizzare
                GridViewColumn entrataEUColumn = editGrid.Columns["EntrataEU"];
                GridViewColumn uscitaEUColumn = editGrid.Columns["UscitaEU"];

                // inizializzazione delle colonne da ridimensionare
                GridViewColumn cantColumn = editGrid.Columns["Cant_Id"];
                GridViewColumn justificationColumn = editGrid.Columns["Motivazione_Reg_Id"];

                // visualizzazione e dimensionamento delle colonne relative al flag E/U
                entrataEUColumn.Visible = true;
                entrataEUColumn.Width = Unit.Percentage(5);
                uscitaEUColumn.Visible = true;
                uscitaEUColumn.Width = Unit.Percentage(5);

                // ridimensionamento delle colonne a cui sarà sottratto spazio per la visualizzazione del flag E/U
                cantColumn.Width = Unit.Percentage(25);
                justificationColumn.Width = Unit.Percentage(10);

            }
        }

        protected void gvAutFerPerEdit_CustomCallback(object sender, ASPxGridViewCustomCallbackEventArgs e)
        {

            _callBackParameter = e.Parameters;

            //recupero gli id delle varie motivazioni che serviranno, ferie, richiesta ferie, ferie rifiutate, malattia, richiesta malattia e malattia rifiutata
            List<Tab_Decod> permessoId = RepoManager.Tab_DecodRepo.GetAllQueryable(d => d.Chiave_Tab == "P" && d.Nome_Tab == "MOTIVAZIONI").ToList();
            List<Tab_Decod> ferieId = RepoManager.Tab_DecodRepo.GetAllQueryable(f => f.Chiave_Tab == "F" && f.Nome_Tab == "MOTIVAZIONI").ToList();
            List<Tab_Decod> richiestaFerieId = RepoManager.Tab_DecodRepo.GetAllQueryable(td => td.Chiave_Tab == "RF").ToList();
            List<Tab_Decod> richiestaPermessoId = RepoManager.Tab_DecodRepo.GetAllQueryable(td => td.Chiave_Tab == "RP").ToList();
            List<Tab_Decod> ferieRifiutateId = RepoManager.Tab_DecodRepo.GetAllQueryable(td => td.Chiave_Tab == "FR").ToList();
            List<Tab_Decod> PermessoRifiutatoId = RepoManager.Tab_DecodRepo.GetAllQueryable(td => td.Chiave_Tab == "PRF").ToList();

            if (e.Parameters.StartsWith("updateToMemory") && IsToPopulateGrid) // se si sta facendo l'update in memoria e si è in ON
                BindGrid();
            else if (e.Parameters.StartsWith("accept")) // se si tratta dell'elaborazione su db e si è in ON
            {
                //Dictionary<string, string> errors = new Dictionary<string, string>();
                var splitter = e.Parameters.Split(new char[] { '|' });

                int currentRegId = Convert.ToInt32(splitter[1]);

                //viene stratta la registrazione che ho premuto l'edit multiplo
                Reg currentReg = RepoManager.RegRepo.Single(reg => reg.Reg_Id == currentRegId);

                // per prima cosa nell'elaborate viene effettuato il bind della griglia di modo da ricevere i dati aggiornati
                BindGrid();

                // generazione del dizionario che viualizzerà gli errori di update
                var errorByColId = new Dictionary<string, IList<Dictionary<string, string>>>();

                // elenco delle date con cui costruire il periodo di rielaborazione
                var toElaborateDates = new HashSet<DateTime>();

                // inizializzazione delle liste che conterranno le reg da modificare e le reg da cancellare
                var toUpdateRegs = new List<Reg>();
                var toDeleteRegs = new List<Reg>();

                Reg_V tmpReg_V = RepoManager.Reg_VRepo.Single(regv => regv.RegE == currentRegId);

                // inizializzazione della lista dei collaboratori inclusi nel processo di elaborazione
                var colIds = new List<int>();

                // per ogni regv modificata vado a controllare quale motivazione è stata impostata
                var regVsToCheck = RegVsToUpdate;

                // alla lista per l'elaborazione di update vanno tolti tutti i record marcati per la cancellazione
                regVsToCheck = regVsToCheck.Where(regV => !RegVsToDelete.Select(dRegV => dRegV.TmpNewId).Where(tmpNewId => tmpNewId != String.Empty).Contains(regV.TmpNewId)).ToList();

                if (!tmpReg_V.Note_Reg.Contains("-"))
                {
                    regVsToCheck.Add(tmpReg_V);
                    int conferma = 0;

                    foreach (var regv in regVsToCheck)
                    {
                        if (regv.Registrazione_Tipo_Reg == 8)
                        {
                            Dictionary<string, string> validationErrors = new Dictionary<string, string>();

                            #region Check RegE

                            // inizializzazione delle variabili utilizzate nella valutazione delle date/ore origine
                            int regEId = 0;
                            DateTime origDateE = DateTime.MinValue;

                            // generazione della reg in entrata utilizzata per la modifica e della reg attuale nel repository
                            Reg newRegE = RepoManager.RegRepo.Init();

                            // recupero la vecchia reg solamente se non si tratta di un nuovo inserimento
                            Reg oldRegE = null;
                            if (regv.RegE != 0)
                            {
                                oldRegE = RepoManager.RegRepo.Single(reg => reg.Reg_Id == regv.RegE);

                                // viene verificato se la reg nel repository ha una data, eventualmente utilizzata per una rielaborazione anche di quel giorno
                                if (oldRegE.Registrazione_Data_Ora_Fis_Reg != null)
                                    toElaborateDates.Add(oldRegE.Registrazione_Data_Ora_Fis_Reg.Date);

                                regEId = regv.RegE;
                                origDateE = oldRegE.Registrazione_Data_Ora_Orig_Reg;
                            }
                            else
                            {
                                // se si tratta di un nuovo record allora do in pasto all'elaborate la data e ora d'entrata.
                                toElaborateDates.Add(regv.Data_Ora_Fis_E.Date);
                            }


                            // se la reg in elaborazione ha una data ora di entrata valida, viene impostata sulla nova reg in entrata di appoggio
                            if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_E != null &&
                                regv.Data_Ora_Fis_E != DateTime.MinValue)
                            {
                                newRegE.Registrazione_Data_Ora_Fis_Reg = new DateTime(regv.Data_Reg.Value.Year,
                                    regv.Data_Reg.Value.Month, regv.Data_Reg.Value.Day, regv.Data_Ora_Fis_E.Hour,
                                    regv.Data_Ora_Fis_E.Minute, regv.Data_Ora_Fis_E.Second);
                            }

                            // impostazioni dei dati della registrazione in entrata utilizzata per la modifica
                            newRegE.Cant_Id = regv.Cant_Id;
                            newRegE.Col_Id = regv.Col_Id;
                            newRegE.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;
                            newRegE.Registrazione_Data_Ora_Fig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                            newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                            newRegE.Fru_Id = regv.Fru_Id;
                            newRegE.Pru_Id = regv.Pru_Id;
                            newRegE.Custom_Data_Reg = oldRegE != null ? oldRegE.Custom_Data_Reg : null;
                            newRegE.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                            newRegE.Registrazione_Tipo_Reg = (int)RegTypeEnum.Duration;
                            newRegE.Rettifica_Durata = Convert.ToInt32(oldRegE.Rettifica_Durata);

                            // se è stato modificato il cantiere della registrazione allora si svuota anche la matricola unità fissa
                            if (oldRegE != null)
                                RepoManager.RegRepo.ManageCantColChangesBeforeUpdate(newRegE, oldRegE);

                            // alla reg viene impostato il fatto se è stata bloccata o meno dalla reg_v
                            newRegE.Registrazione_Bloccata = regv.Registrazione_Bloccata;

                            // alla reg_v viene appiccicato il relativo flag di entrata/uscita
                            newRegE.Flag_EU_Reg = regv.EntrataEU;

                            // se si tratta di una registrazione nuova allora:
                            // 1. viene impostata la data/ora di registrazione fisica con quella presente nella regv
                            // 2. viene forzata la data/ora originale uguale alla data e ora fisica.
                            if (newRegE.Reg_Id == 0)
                            {
                                newRegE.Registrazione_Data_Ora_Fis_Reg = regv.Data_Ora_Fis_E;
                                newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                            }

                            #endregion

                            #region Check RegU

                            // generazione della reg in uscita di appoggio
                            Reg newRegU = RepoManager.RegRepo.Init();
                            Reg oldRegU = null;

                            // inizializzazione delle variabili utilizzate nella valutazione delle date/ore origine
                            int regUId = 0;
                            DateTime origDateU = DateTime.MinValue;

                            // se la regv in elaborazione ha una data e ora di uscita valida
                            if (regv.Data_Ora_Fis_U != null && regv.Data_Ora_Fis_U != DateTime.MinValue)
                            {

                                // se non si tratta di una nuova reg allora viene recuperata dal repository la reg in uscita con lo stesso codice
                                if (regv.RegU != null)
                                {
                                    oldRegU = RepoManager.RegRepo.Single(reg => reg.Reg_Id == regv.RegU);

                                    // se la reg in uscita presa dal repository ha una data in uscita valida allora la si aggiunge all'elenco di date da rielaborare
                                    if (oldRegU.Registrazione_Data_Ora_Fis_Reg != null)
                                        toElaborateDates.Add(oldRegU.Registrazione_Data_Ora_Fis_Reg.Date);

                                    regUId = oldRegU.Reg_Id;

                                    origDateU = oldRegU.Registrazione_Data_Ora_Orig_Reg;
                                }


                                // se la regv in elaborazione ha una data/ora di uscita valida viene impostata sulla nuova reg in uscita utilizzata per la modifica
                                if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_U != null &&
                                    regv.Data_Ora_Fis_U != DateTime.MinValue)
                                {
                                    newRegU.Registrazione_Data_Ora_Fis_Reg = new DateTime(regv.Data_Reg.Value.Year,
                                        regv.Data_Reg.Value.Month, regv.Data_Reg.Value.Day, regv.Data_Ora_Fis_U.Value.Hour,
                                        regv.Data_Ora_Fis_U.Value.Minute, regv.Data_Ora_Fis_U.Value.Second);
                                }

                                // inserimento dei valori nella nuova reg in uscita
                                newRegU.Cant_Id = regv.Cant_Id;
                                newRegU.Col_Id = regv.Col_Id;
                                newRegU.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;
                                newRegU.Registrazione_Data_Ora_Fig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                                if (oldRegU != null)
                                {
                                    newRegU.Fru_Id = oldRegU.Fru_Id;
                                    newRegU.Pru_Id = oldRegU.Pru_Id;
                                    newRegU.Custom_Data_Reg = oldRegU.Custom_Data_Reg;

                                    // se è stato modificato il cantiere della registrazione allora si svuota anche la matricola unità fissa
                                    if (oldRegU != null)
                                        RepoManager.RegRepo.ManageCantColChangesBeforeUpdate(newRegU, oldRegU);
                                }
                                else
                                {
                                    newRegU.Fru_Id = null;
                                    newRegU.Pru_Id = null;
                                }

                                // alla reg viene impostato il fatto se è stata bloccata o meno dalla reg_v
                                newRegU.Registrazione_Bloccata = regv.Registrazione_Bloccata;

                                // alla reg_v viene appiccicato il relativo flag di entrata/uscita
                                newRegU.Flag_EU_Reg = regv.UscitaEU;

                                // se si tratta di una registrazione nuova allora:
                                // 1. viene impostata la data/ora di registrazione fisica con quella presente nella regv
                                // 2. viene forzata la data/ora originale uguale alla data e ora fisica.
                                if (newRegU.Reg_Id == 0)
                                {
                                    newRegU.Registrazione_Data_Ora_Fis_Reg = Convert.ToDateTime(regv.Data_Ora_Fis_U);
                                    newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                                }

                                // se è impostato il notturno e la data/ora fisica d'uscita è inferirore a quella in entrata si verifica la variabile
                                // di stesso giorno e caso mai si aggiunge un giorno
                                if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno &&
                                    RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.None && RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.Disabled)
                                {
                                    if (regv.Data_Ora_Fis_U < regv.Data_Ora_Fis_E &&
                                        regv.Data_Ora_Fis_U.Value.TimeOfDay > regv.Data_Reg.Value.TimeOfDay)
                                        // viene aggiunto un giorno solamente se non si è all'interno dello stesso giorno
                                        if (regv.IsUTimeSameDayE)
                                        {
                                            newRegU.Registrazione_Data_Ora_Fis_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg.AddDays(1);
                                            // se si tratta di una nuova registrazione viene reimpostata anche la data/ora originale
                                            if (newRegU.Reg_Id == 0)
                                                newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                                        }
                                }

                                // assegnazione alla reg in uscita del relative record number della reg in entrata
                                newRegU.ParentReg = newRegE;

                                newRegU.DisAbilitazione_Reg = false;
                                newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                            }
                            else
                            {
                                // se la regv non ha una valida data/ora d'uscita allora
                                // viene recuperata la vecchia reg in uscita
                                oldRegU = RepoManager.RegRepo.SingleOrDefault(rv => rv.Reg_Id == regv.RegU);

                                // e se presente viene marcata per la cancellazione
                                if (oldRegU != null)
                                    toDeleteRegs.Add(oldRegU);

                                // in caso non abbia una reg in uscita anche la nuova regu va impostata a null per non essere aggiunta
                                newRegU = null;
                            }

                            #endregion

                            #region Check RegV

                            // se la regv in elaborazione ha un codice collaboratore non già inserito in lista, lo si aggiunge per gestione dell'elaborate 
                            if (regv.Col_Id != 0 && regv.Col_Id != null && !colIds.Contains(regv.Col_Id.Value))
                                colIds.Add(regv.Col_Id.Value);

                            // sono forzate sulla regv da checcare l'ora di entrata e uscita fisica così da evitare
                            // mancati controlli per la presenza di min value precedentemente inseriti
                            regv.Data_Ora_Fis_E = newRegE.Registrazione_Data_Ora_Fis_Reg;
                            if (newRegU != null)
                                regv.Data_Ora_Fis_U = newRegU.Registrazione_Data_Ora_Fis_Reg;

                            #endregion

                            #region Manage Regs orig date

                            // è calcolato il fatto che si sta elaborando un'uscita nello stesso giorno
                            bool isSameDay = true;
                            // se non c'è il notturno è sicuramente true
                            if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && (RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight || RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.Duration))
                                isSameDay = regv.IsUTimeSameDayE;

                            // gestione della data/ora originale delle reg 
                            RepoManager.RegRepo.ManageOrigDates(newRegE, regEId, origDateE, newRegU, regUId, origDateU, isSameDay);

                            #endregion

                            #region Manage Blocking regs

                            // gestione del codice di accoppiamento per le reg bloccate
                            RepoManager.RegRepo.PerformBlockedRegsCouple(newRegE, newRegU, regv.RegE);

                            #endregion

                            Tab_Decod motRichiesta = RepoManager.Tab_DecodRepo.Single(td => td.Tab_Decod_Id == oldRegE.Motivazione_Reg_Id);
                            string codNuovaRichiesta = "";
                            var tmpRich = motRichiesta.Decodifica_Tab.Split(' ');
                            for (int i = 1; i < tmpRich.Length; i++)
                            {
                                if (i != 1)
                                {
                                    codNuovaRichiesta = codNuovaRichiesta + " " + tmpRich[i];
                                }
                                else
                                {
                                    codNuovaRichiesta += tmpRich[i];
                                }

                            }
                            if (!codNuovaRichiesta.Contains("Ferie"))
                                conferma = 1;

                            Tab_Decod mot = RepoManager.Tab_DecodRepo.Single(td => td.Decodifica_Tab == codNuovaRichiesta);

                            newRegE.Motivazione_Reg_Id = mot.Tab_Decod_Id;
                            Col cols = RepoManager.ColRepo.Single(c => c.Col_Id == tmpReg_V.Col_Id);
                            bool isOrario = true;
                            if (cols.Tab_Orari_Tipo_Id != null)
                            {
                                Tab_Orari orarioFinale = new Tab_Orari();
                                Tab_Orari_Tipo tipoOr = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(td => td.Tab_Orari_Tipo_Id == cols.Tab_Orari_Tipo_Id);
                                var orari = RepoManager.Tab_OrariRepo.GetAllQueryable(o => o.Tab_Orari_Tipo_Id == tipoOr.Tab_Orari_Tipo_Id).GroupBy(or => or.Data_Inizio).ToList();
                                if (orari.Count > 0)
                                {
                                    foreach (var singoloOrario in orari.Last())
                                    {
                                        switch (newRegE.Registrazione_Data_Ora_Fis_Reg.DayOfWeek)
                                        {
                                            case DayOfWeek.Monday:
                                                if (singoloOrario.G1)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Tuesday:
                                                if (singoloOrario.G2)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Wednesday:
                                                if (singoloOrario.G3)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Thursday:
                                                if (singoloOrario.G4)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Friday:
                                                if (singoloOrario.G5)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Saturday:
                                                if (singoloOrario.G6)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Sunday:
                                                if (singoloOrario.G7)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                        }
                                    }
                                    if (orarioFinale != null)
                                    {
                                        var durataOrario = orarioFinale.Durata_Minuti;
                                        double durataRegistrazione = 0;
                                        if (newRegU != null)
                                        {
                                            durataRegistrazione = (newRegU.Registrazione_Data_Ora_Fis_Reg - newRegE.Registrazione_Data_Ora_Fis_Reg).TotalMinutes;
                                        }
                                        else
                                        {
                                            durataRegistrazione = newRegE.Rettifica_Durata.Value;
                                        }
                                        if (durataRegistrazione > durataOrario)
                                        {
                                            isOrario = false;
                                        }
                                    }
                                }
                            }

                            if (newRegU != null)
                                newRegU.Motivazione_Reg_Id = mot.Tab_Decod_Id;

                            if (isOrario)
                            {
                                #region Add Checked Regs To updates list
                                // controllo se nella timbratura è stata impostata la motivazione ferie o permesso, in questo caso provedo a processarle
                                toUpdateRegs.Add(newRegE);

                                // aggiunta la nuova reg in uscita tra quelle da aggiungere (solo se valorizzata)
                                if (newRegU != null)
                                    toUpdateRegs.Add(newRegU);

                                if (oldRegE != null)
                                    toDeleteRegs.Add(oldRegE);

                                // se la vecchia reg in uscita è valorizzata allora viene impostata come reg da cancellare
                                if (oldRegU != null)
                                    toDeleteRegs.Add(oldRegU);

                                #endregion
                                Col col = RepoManager.ColRepo.Single(c => c.Col_Id == tmpReg_V.Col_Id);
                                InviaConferma(1, col, regv.Data_Reg.Value, regv.Data_Reg.Value);
                            }
                            else
                            {
                                validationErrors.AddOrAppend("RegV", "Richiesta autorizzata maggiore dell'orario del collaboratore");
                                SetErrorMessageDictionary(regv, errorByColId, validationErrors);
                                EditErrorMessage = "Richiesta autorizzata maggiore dell'orario del collaboratore";
                            }
                        }
                        else 
                        {
                            Dictionary<string, string> validationErrors;

                            #region spostamento data uscita RegV se entrata non presente

                            // se la data entrata è non valorizzata
                            if (regv.Data_Ora_Fis_E.TimeOfDay == TimeSpan.Zero)
                            {
                                // se la data di uscita è valorizzata
                                if (regv.Data_Ora_Fis_U.HasValue)
                                {
                                    // si sposta la data di uscita su quella di entrata e si marca per la cancellazione
                                    // la vecchia reg in entrata
                                    if (regv.RegE != 0)
                                        toDeleteRegs.Add(RepoManager.RegRepo.FirstOrDefault(reg => reg.Reg_Id == regv.RegE));
                                    regv.Data_Ora_Fis_E = regv.Data_Ora_Fis_U.Value;
                                    regv.Data_Ora_Fis_U = null;
                                    regv.RegE = Convert.ToInt32(regv.RegU);
                                    regv.RegU = null;
                                }
                                else// in caso non siano valorizzate date, si passa al record successivo, prima cancellando la reg in entrata
                                {
                                    // solamente se non si tratta di nuovi inserimenti
                                    if (regv.RegE != 0)
                                    {
                                        //RiferimentoRRN_Reg
                                        var regToDelete = RepoManager.RegRepo.SingleOrDefault(reg => reg.Reg_Id == regv.RegE);
                                        regToDelete.RiferimentoRRN_Reg = null;
                                        toDeleteRegs.Add(regToDelete);
                                        if (regv.RegU != null)
                                        {
                                            toDeleteRegs.Add(RepoManager.RegRepo.SingleOrDefault(reg => reg.Reg_Id == regv.RegU));
                                        }
                                        continue;
                                    }
                                }
                            }

                            #endregion

                            #region Check RegE

                            // inizializzazione delle variabili utilizzate nella valutazione delle date/ore origine
                            int regEId = 0;
                            DateTime origDateE = DateTime.MinValue;

                            // generazione della reg in entrata utilizzata per la modifica e della reg attuale nel repository
                            Reg newRegE = RepoManager.RegRepo.Init();

                            // recupero la vecchia reg solamente se non si tratta di un nuovo inserimento
                            Reg oldRegE = null;
                            if (regv.RegE != 0)
                            {
                                oldRegE = RepoManager.RegRepo.Single(reg => reg.Reg_Id == regv.RegE);

                                // viene verificato se la reg nel repository ha una data, eventualmente utilizzata per una rielaborazione anche di quel giorno
                                if (oldRegE.Registrazione_Data_Ora_Fis_Reg != null)
                                    toElaborateDates.Add(oldRegE.Registrazione_Data_Ora_Fis_Reg.Date);

                                regEId = regv.RegE;
                                origDateE = oldRegE.Registrazione_Data_Ora_Orig_Reg;
                            }
                            else
                            {
                                // se si tratta di un nuovo record allora do in pasto all'elaborate la data e ora d'entrata.
                                toElaborateDates.Add(regv.Data_Ora_Fis_E.Date);
                            }


                            // se la reg in elaborazione ha una data ora di entrata valida, viene impostata sulla nova reg in entrata di appoggio
                            if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_E != null &&
                                regv.Data_Ora_Fis_E != DateTime.MinValue)
                            {
                                newRegE.Registrazione_Data_Ora_Fis_Reg = new DateTime(regv.Data_Reg.Value.Year,
                                    regv.Data_Reg.Value.Month, regv.Data_Reg.Value.Day, regv.Data_Ora_Fis_E.Hour,
                                    regv.Data_Ora_Fis_E.Minute, regv.Data_Ora_Fis_E.Second);
                            }

                            // impostazioni dei dati della registrazione in entrata utilizzata per la modifica
                            newRegE.Cant_Id = regv.Cant_Id;
                            newRegE.Col_Id = regv.Col_Id;
                            newRegE.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;
                            newRegE.Registrazione_Data_Ora_Fig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                            newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                            newRegE.Fru_Id = regv.Fru_Id;
                            newRegE.Pru_Id = regv.Pru_Id;
                            newRegE.Custom_Data_Reg = oldRegE != null ? oldRegE.Custom_Data_Reg : null;

                            // se è stato modificato il cantiere della registrazione allora si svuota anche la matricola unità fissa
                            if (oldRegE != null)
                                RepoManager.RegRepo.ManageCantColChangesBeforeUpdate(newRegE, oldRegE);

                            // alla reg viene impostato il fatto se è stata bloccata o meno dalla reg_v
                            newRegE.Registrazione_Bloccata = regv.Registrazione_Bloccata;

                            // alla reg_v viene appiccicato il relativo flag di entrata/uscita
                            newRegE.Flag_EU_Reg = regv.EntrataEU;

                            // se si tratta di una registrazione nuova allora:
                            // 1. viene impostata la data/ora di registrazione fisica con quella presente nella regv
                            // 2. viene forzata la data/ora originale uguale alla data e ora fisica.
                            if (newRegE.Reg_Id == 0)
                            {
                                newRegE.Registrazione_Data_Ora_Fis_Reg = regv.Data_Ora_Fis_E;
                                newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                            }

                            // viene effettuata la check sulla reg, indicando se si tratta di un nuovo inserimento
                            validationErrors = regv.RegE == 0 ? RepoManager.RegRepo.Check(newRegE, true) : RepoManager.RegRepo.Check(newRegE, false);

                            // se ci sono stati errori si interrompe l'elaborazione
                            if (validationErrors.Count > 0)
                            {
                                SetErrorMessageDictionary(regv, errorByColId, validationErrors);
                                continue;
                            }

                            #endregion

                            #region Check RegU

                            // generazione della reg in uscita di appoggio
                            Reg newRegU = RepoManager.RegRepo.Init();
                            Reg oldRegU = null;

                            // inizializzazione delle variabili utilizzate nella valutazione delle date/ore origine
                            int regUId = 0;
                            DateTime origDateU = DateTime.MinValue;

                            // se la regv in elaborazione ha una data e ora di uscita valida
                            if (regv.Data_Ora_Fis_U != null && regv.Data_Ora_Fis_U != DateTime.MinValue)
                            {

                                // se non si tratta di una nuova reg allora viene recuperata dal repository la reg in uscita con lo stesso codice
                                if (regv.RegU != null)
                                {
                                    oldRegU = RepoManager.RegRepo.Single(reg => reg.Reg_Id == regv.RegU);

                                    // se la reg in uscita presa dal repository ha una data in uscita valida allora la si aggiunge all'elenco di date da rielaborare
                                    if (oldRegU.Registrazione_Data_Ora_Fis_Reg != null)
                                        toElaborateDates.Add(oldRegU.Registrazione_Data_Ora_Fis_Reg.Date);

                                    regUId = oldRegU.Reg_Id;

                                    origDateU = oldRegU.Registrazione_Data_Ora_Orig_Reg;
                                }


                                // se la regv in elaborazione ha una data/ora di uscita valida viene impostata sulla nuova reg in uscita utilizzata per la modifica
                                if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_U != null &&
                                    regv.Data_Ora_Fis_U != DateTime.MinValue)
                                {
                                    newRegU.Registrazione_Data_Ora_Fis_Reg = new DateTime(regv.Data_Reg.Value.Year,
                                        regv.Data_Reg.Value.Month, regv.Data_Reg.Value.Day, regv.Data_Ora_Fis_U.Value.Hour,
                                        regv.Data_Ora_Fis_U.Value.Minute, regv.Data_Ora_Fis_U.Value.Second);
                                }

                                // inserimento dei valori nella nuova reg in uscita
                                newRegU.Cant_Id = regv.Cant_Id;
                                newRegU.Col_Id = regv.Col_Id;
                                newRegU.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;
                                newRegU.Registrazione_Data_Ora_Fig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                                if (oldRegU != null)
                                {
                                    newRegU.Fru_Id = oldRegU.Fru_Id;
                                    newRegU.Pru_Id = oldRegU.Pru_Id;
                                    newRegU.Custom_Data_Reg = oldRegU.Custom_Data_Reg;

                                    // se è stato modificato il cantiere della registrazione allora si svuota anche la matricola unità fissa
                                    if (oldRegU != null)
                                        RepoManager.RegRepo.ManageCantColChangesBeforeUpdate(newRegU, oldRegU);
                                }
                                else
                                {
                                    newRegU.Fru_Id = null;
                                    newRegU.Pru_Id = null;
                                }

                                // alla reg viene impostato il fatto se è stata bloccata o meno dalla reg_v
                                newRegU.Registrazione_Bloccata = regv.Registrazione_Bloccata;

                                // alla reg_v viene appiccicato il relativo flag di entrata/uscita
                                newRegU.Flag_EU_Reg = regv.UscitaEU;

                                // se si tratta di una registrazione nuova allora:
                                // 1. viene impostata la data/ora di registrazione fisica con quella presente nella regv
                                // 2. viene forzata la data/ora originale uguale alla data e ora fisica.
                                if (newRegU.Reg_Id == 0)
                                {
                                    newRegU.Registrazione_Data_Ora_Fis_Reg = Convert.ToDateTime(regv.Data_Ora_Fis_U);
                                    newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                                }

                                // se è impostato il notturno e la data/ora fisica d'uscita è inferirore a quella in entrata si verifica la variabile
                                // di stesso giorno e caso mai si aggiunge un giorno
                                if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno &&
                                    RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.None && RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.Disabled)
                                {
                                    if (regv.Data_Ora_Fis_U < regv.Data_Ora_Fis_E &&
                                        regv.Data_Ora_Fis_U.Value.TimeOfDay > regv.Data_Reg.Value.TimeOfDay)
                                        // viene aggiunto un giorno solamente se non si è all'interno dello stesso giorno
                                        if (regv.IsUTimeSameDayE)
                                        {
                                            newRegU.Registrazione_Data_Ora_Fis_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg.AddDays(1);
                                            // se si tratta di una nuova registrazione viene reimpostata anche la data/ora originale
                                            if (newRegU.Reg_Id == 0)
                                                newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                                        }
                                }

                                // assegnazione alla reg in uscita del relative record number della reg in entrata
                                newRegU.ParentReg = newRegE;

                                newRegU.DisAbilitazione_Reg = false;
                                newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;

                                // effettuazione della check per la registrazione in uscita indicando se nuova o variata
                                validationErrors = regv.RegU == null ? RepoManager.RegRepo.Check(newRegU, true) : RepoManager.RegRepo.Check(newRegU, false);

                                // se ci sono stati errori in validazione, allora si interrompe l'elaborazione
                                if (validationErrors.Count > 0)
                                {
                                    SetErrorMessageDictionary(regv, errorByColId, validationErrors);
                                    continue;
                                }
                            }
                            else
                            {
                                // se la regv non ha una valida data/ora d'uscita allora
                                // viene recuperata la vecchia reg in uscita
                                oldRegU = RepoManager.RegRepo.SingleOrDefault(rv => rv.Reg_Id == regv.RegU);

                                // e se presente viene marcata per la cancellazione
                                if (oldRegU != null)
                                    toDeleteRegs.Add(oldRegU);

                                // in caso non abbia una reg in uscita anche la nuova regu va impostata a null per non essere aggiunta
                                newRegU = null;
                            }

                            #endregion

                            #region Check RegV

                            // se la regv in elaborazione ha un codice collaboratore non già inserito in lista, lo si aggiunge per gestione dell'elaborate 
                            if (regv.Col_Id != 0 && regv.Col_Id != null && !colIds.Contains(regv.Col_Id.Value))
                                colIds.Add(regv.Col_Id.Value);

                            // sono forzate sulla regv da checcare l'ora di entrata e uscita fisica così da evitare
                            // mancati controlli per la presenza di min value precedentemente inseriti
                            regv.Data_Ora_Fis_E = newRegE.Registrazione_Data_Ora_Fis_Reg;
                            if (newRegU != null)
                                regv.Data_Ora_Fis_U = newRegU.Registrazione_Data_Ora_Fis_Reg;

                            // effettuazione della check della regV (se si tratta di una regv inserita in sessione allora si passa il parametro new, altrimenti il contrario)
                            validationErrors = RepoManager.Reg_VRepo.Check(regv, regv.RegE == 0 ? true : false);

                            // se ci sono stati errori si interrompe l'elaborazione
                            if (validationErrors.Count > 0)
                            {
                                SetErrorMessageDictionary(regv, errorByColId, validationErrors);
                                continue;
                            }

                            #endregion

                            #region Manage Regs orig date

                            // è calcolato il fatto che si sta elaborando un'uscita nello stesso giorno
                            bool isSameDay = true;
                            // se non c'è il notturno è sicuramente true
                            if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && (RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight || RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.Duration))
                                isSameDay = regv.IsUTimeSameDayE;

                            // gestione della data/ora originale delle reg 
                            RepoManager.RegRepo.ManageOrigDates(newRegE, regEId, origDateE, newRegU, regUId, origDateU, isSameDay);

                            #endregion

                            #region Manage Blocking regs

                            // gestione del codice di accoppiamento per le reg bloccate
                            RepoManager.RegRepo.PerformBlockedRegsCouple(newRegE, newRegU, regv.RegE);

                            #endregion

                            Tab_Decod motRichiesta = RepoManager.Tab_DecodRepo.Single(td => td.Tab_Decod_Id == oldRegE.Motivazione_Reg_Id);
                            string codNuovaRichiesta = "";
                            var tmpRich = motRichiesta.Decodifica_Tab.Split(' ');
                            for (int i = 1; i < tmpRich.Length; i++)
                            {
                                if (i != 1)
                                {
                                    codNuovaRichiesta = codNuovaRichiesta + " " + tmpRich[i];
                                }
                                else
                                {
                                    codNuovaRichiesta += tmpRich[i];
                                }

                            }
                            Tab_Decod mot = RepoManager.Tab_DecodRepo.Single(td => td.Decodifica_Tab == codNuovaRichiesta);

                            newRegE.Motivazione_Reg_Id = mot.Tab_Decod_Id;
                            newRegU.Motivazione_Reg_Id = mot.Tab_Decod_Id;

                            Col col = RepoManager.ColRepo.Single(c => c.Col_Id == tmpReg_V.Col_Id);
                            bool isOrario = true;
                            if (col.Tab_Orari_Tipo_Id != null)
                            {
                                Tab_Orari orarioFinale = new Tab_Orari();
                                Tab_Orari_Tipo tipoOr = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(td => td.Tab_Orari_Tipo_Id == col.Tab_Orari_Tipo_Id);
                                var orari = RepoManager.Tab_OrariRepo.GetAllQueryable(o => o.Tab_Orari_Tipo_Id == tipoOr.Tab_Orari_Tipo_Id).GroupBy(or => or.Data_Inizio).ToList();
                                if (orari.Count > 0)
                                {
                                    foreach (var singoloOrario in orari.Last())
                                    {
                                        switch (newRegE.Registrazione_Data_Ora_Fis_Reg.DayOfWeek)
                                        {
                                            case DayOfWeek.Monday:
                                                if (singoloOrario.G1)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Tuesday:
                                                if (singoloOrario.G2)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Wednesday:
                                                if (singoloOrario.G3)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Thursday:
                                                if (singoloOrario.G4)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Friday:
                                                if (singoloOrario.G5)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Saturday:
                                                if (singoloOrario.G6)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                            case DayOfWeek.Sunday:
                                                if (singoloOrario.G7)
                                                {
                                                    orarioFinale = singoloOrario;
                                                }
                                                break;
                                        }
                                    }
                                    if (orarioFinale != null)
                                    {
                                        var durataOrario = orarioFinale.Durata_Minuti;
                                        double durataRegistrazione = 0;
                                        if (newRegU != null)
                                        {
                                            durataRegistrazione = (newRegU.Registrazione_Data_Ora_Fis_Reg - newRegE.Registrazione_Data_Ora_Fis_Reg).TotalMinutes;
                                        }
                                        else
                                        {
                                            durataRegistrazione = newRegE.Rettifica_Durata.Value;
                                        }
                                        if (durataRegistrazione > durataOrario)
                                        {
                                            isOrario = false;
                                        }
                                    }
                                }
                            }

                            if (isOrario)
                            {
                                InviaConferma(conferma, col, tmpReg_V.Data_Reg.Value, tmpReg_V.Data_Reg.Value);

                                #region Add Checked Regs To updates list

                                // controllo se nella timbratura è stata impostata la motivazione ferie o permesso, in questo caso provedo a processarle
                                toUpdateRegs.Add(newRegE);

                                // aggiunta la nuova reg in uscita tra quelle da aggiungere (solo se valorizzata)
                                if (newRegU != null)
                                    toUpdateRegs.Add(newRegU);

                                if (oldRegE != null)
                                    toDeleteRegs.Add(oldRegE);

                                // se la vecchia reg in uscita è valorizzata allora viene impostata come reg da cancellare
                                if (oldRegU != null)
                                    toDeleteRegs.Add(oldRegU);

                                #endregion
                            }
                            else
                            {
                                //SetErrorMessageDictionary(regv, errorByColId, validationErrors);
                                IList<Dictionary<string, string>> currentValidationErrorsList = new List<Dictionary<string, string>>();
                                currentValidationErrorsList.Add(validationErrors);
                                EditErrorMessage = "Richiesta autorizzata maggiore dell'orario del collaboratore";
                                errorByColId.Add(EditErrorMessage, currentValidationErrorsList);
                                
                            }
                        }
                    }
                }
                else {
                    var dates = tmpReg_V.Note_Reg.Split('-');
                    DateTime from = DateTime.Parse(dates[0] + " 00:00:00");
                    DateTime to = DateTime.Parse(dates[1] + " 23:59:59");
                    List<Reg_V> tmpRegvs = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Motivazione_Reg_Id == tmpReg_V.Motivazione_Reg_Id && r.Col_Id == tmpReg_V.Col_Id && (r.Data_Reg.Value >= from && r.Data_Reg.Value <= to)).ToList();
                    regVsToCheck.AddRange(tmpRegvs);
                    int conferma = 0;

                    foreach (var regv in regVsToCheck)
                    {
                        Dictionary<string, string> validationErrors = new Dictionary<string, string>();

                        #region Check RegE

                        // inizializzazione delle variabili utilizzate nella valutazione delle date/ore origine
                        int regEId = 0;
                        DateTime origDateE = DateTime.MinValue;

                        // generazione della reg in entrata utilizzata per la modifica e della reg attuale nel repository
                        Reg newRegE = RepoManager.RegRepo.Init();

                        // recupero la vecchia reg solamente se non si tratta di un nuovo inserimento
                        Reg oldRegE = null;
                        if (regv.RegE != 0)
                        {
                            oldRegE = RepoManager.RegRepo.Single(reg => reg.Reg_Id == regv.RegE);

                            // viene verificato se la reg nel repository ha una data, eventualmente utilizzata per una rielaborazione anche di quel giorno
                            if (oldRegE.Registrazione_Data_Ora_Fis_Reg != null)
                                toElaborateDates.Add(oldRegE.Registrazione_Data_Ora_Fis_Reg.Date);

                            regEId = regv.RegE;
                            origDateE = oldRegE.Registrazione_Data_Ora_Orig_Reg;
                        }
                        else
                        {
                            // se si tratta di un nuovo record allora do in pasto all'elaborate la data e ora d'entrata.
                            toElaborateDates.Add(regv.Data_Ora_Fis_E.Date);
                        }


                        // se la reg in elaborazione ha una data ora di entrata valida, viene impostata sulla nova reg in entrata di appoggio
                        if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_E != null &&
                            regv.Data_Ora_Fis_E != DateTime.MinValue)
                        {
                            newRegE.Registrazione_Data_Ora_Fis_Reg = new DateTime(regv.Data_Reg.Value.Year,
                                regv.Data_Reg.Value.Month, regv.Data_Reg.Value.Day, regv.Data_Ora_Fis_E.Hour,
                                regv.Data_Ora_Fis_E.Minute, regv.Data_Ora_Fis_E.Second);
                        }

                        // impostazioni dei dati della registrazione in entrata utilizzata per la modifica
                        newRegE.Cant_Id = regv.Cant_Id;
                        newRegE.Col_Id = regv.Col_Id;
                        newRegE.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;
                        newRegE.Registrazione_Data_Ora_Fig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                        newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                        newRegE.Fru_Id = regv.Fru_Id;
                        newRegE.Pru_Id = regv.Pru_Id;
                        newRegE.Custom_Data_Reg = oldRegE != null ? oldRegE.Custom_Data_Reg : null;
                        newRegE.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                        newRegE.Registrazione_Tipo_Reg = (int)RegTypeEnum.Duration;
                        newRegE.Rettifica_Durata = Convert.ToInt32(oldRegE.Rettifica_Durata);

                        // se è stato modificato il cantiere della registrazione allora si svuota anche la matricola unità fissa
                        if (oldRegE != null)
                            RepoManager.RegRepo.ManageCantColChangesBeforeUpdate(newRegE, oldRegE);

                        // alla reg viene impostato il fatto se è stata bloccata o meno dalla reg_v
                        newRegE.Registrazione_Bloccata = regv.Registrazione_Bloccata;

                        // alla reg_v viene appiccicato il relativo flag di entrata/uscita
                        newRegE.Flag_EU_Reg = regv.EntrataEU;

                        // se si tratta di una registrazione nuova allora:
                        // 1. viene impostata la data/ora di registrazione fisica con quella presente nella regv
                        // 2. viene forzata la data/ora originale uguale alla data e ora fisica.
                        if (newRegE.Reg_Id == 0)
                        {
                            newRegE.Registrazione_Data_Ora_Fis_Reg = regv.Data_Ora_Fis_E;
                            newRegE.Registrazione_Data_Ora_Orig_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg;
                        }

                        #endregion

                        #region Check RegU

                        // generazione della reg in uscita di appoggio
                        Reg newRegU = RepoManager.RegRepo.Init();
                        Reg oldRegU = null;

                        // inizializzazione delle variabili utilizzate nella valutazione delle date/ore origine
                        int regUId = 0;
                        DateTime origDateU = DateTime.MinValue;

                        // se la regv in elaborazione ha una data e ora di uscita valida
                        if (regv.Data_Ora_Fis_U != null && regv.Data_Ora_Fis_U != DateTime.MinValue)
                        {

                            // se non si tratta di una nuova reg allora viene recuperata dal repository la reg in uscita con lo stesso codice
                            if (regv.RegU != null)
                            {
                                oldRegU = RepoManager.RegRepo.Single(reg => reg.Reg_Id == regv.RegU);

                                // se la reg in uscita presa dal repository ha una data in uscita valida allora la si aggiunge all'elenco di date da rielaborare
                                if (oldRegU.Registrazione_Data_Ora_Fis_Reg != null)
                                    toElaborateDates.Add(oldRegU.Registrazione_Data_Ora_Fis_Reg.Date);

                                regUId = oldRegU.Reg_Id;

                                origDateU = oldRegU.Registrazione_Data_Ora_Orig_Reg;
                            }


                            // se la regv in elaborazione ha una data/ora di uscita valida viene impostata sulla nuova reg in uscita utilizzata per la modifica
                            if (regv.Data_Reg != null && regv.Data_Reg != DateTime.MinValue && regv.Data_Ora_Fis_U != null &&
                                regv.Data_Ora_Fis_U != DateTime.MinValue)
                            {
                                newRegU.Registrazione_Data_Ora_Fis_Reg = new DateTime(regv.Data_Reg.Value.Year,
                                    regv.Data_Reg.Value.Month, regv.Data_Reg.Value.Day, regv.Data_Ora_Fis_U.Value.Hour,
                                    regv.Data_Ora_Fis_U.Value.Minute, regv.Data_Ora_Fis_U.Value.Second);
                            }

                            // inserimento dei valori nella nuova reg in uscita
                            newRegU.Cant_Id = regv.Cant_Id;
                            newRegU.Col_Id = regv.Col_Id;
                            newRegU.Motivazione_Reg_Id = regv.Motivazione_Reg_Id;
                            newRegU.Registrazione_Data_Ora_Fig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                            if (oldRegU != null)
                            {
                                newRegU.Fru_Id = oldRegU.Fru_Id;
                                newRegU.Pru_Id = oldRegU.Pru_Id;
                                newRegU.Custom_Data_Reg = oldRegU.Custom_Data_Reg;

                                // se è stato modificato il cantiere della registrazione allora si svuota anche la matricola unità fissa
                                if (oldRegU != null)
                                    RepoManager.RegRepo.ManageCantColChangesBeforeUpdate(newRegU, oldRegU);
                            }
                            else
                            {
                                newRegU.Fru_Id = null;
                                newRegU.Pru_Id = null;
                            }

                            // alla reg viene impostato il fatto se è stata bloccata o meno dalla reg_v
                            newRegU.Registrazione_Bloccata = regv.Registrazione_Bloccata;

                            // alla reg_v viene appiccicato il relativo flag di entrata/uscita
                            newRegU.Flag_EU_Reg = regv.UscitaEU;

                            // se si tratta di una registrazione nuova allora:
                            // 1. viene impostata la data/ora di registrazione fisica con quella presente nella regv
                            // 2. viene forzata la data/ora originale uguale alla data e ora fisica.
                            if (newRegU.Reg_Id == 0)
                            {
                                newRegU.Registrazione_Data_Ora_Fis_Reg = Convert.ToDateTime(regv.Data_Ora_Fis_U);
                                newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                            }

                            // se è impostato il notturno e la data/ora fisica d'uscita è inferirore a quella in entrata si verifica la variabile
                            // di stesso giorno e caso mai si aggiunge un giorno
                            if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno &&
                                RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.None && RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.Disabled)
                            {
                                if (regv.Data_Ora_Fis_U < regv.Data_Ora_Fis_E &&
                                    regv.Data_Ora_Fis_U.Value.TimeOfDay > regv.Data_Reg.Value.TimeOfDay)
                                    // viene aggiunto un giorno solamente se non si è all'interno dello stesso giorno
                                    if (regv.IsUTimeSameDayE)
                                    {
                                        newRegU.Registrazione_Data_Ora_Fis_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg.AddDays(1);
                                        // se si tratta di una nuova registrazione viene reimpostata anche la data/ora originale
                                        if (newRegU.Reg_Id == 0)
                                            newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                                    }
                            }

                            // assegnazione alla reg in uscita del relative record number della reg in entrata
                            newRegU.ParentReg = newRegE;

                            newRegU.DisAbilitazione_Reg = false;
                            newRegU.Registrazione_Data_Ora_Orig_Reg = newRegU.Registrazione_Data_Ora_Fis_Reg;
                        }
                        else
                        {
                            // se la regv non ha una valida data/ora d'uscita allora
                            // viene recuperata la vecchia reg in uscita
                            oldRegU = RepoManager.RegRepo.SingleOrDefault(rv => rv.Reg_Id == regv.RegU);

                            // e se presente viene marcata per la cancellazione
                            if (oldRegU != null)
                                toDeleteRegs.Add(oldRegU);

                            // in caso non abbia una reg in uscita anche la nuova regu va impostata a null per non essere aggiunta
                            newRegU = null;
                        }

                        #endregion

                        #region Check RegV

                        // se la regv in elaborazione ha un codice collaboratore non già inserito in lista, lo si aggiunge per gestione dell'elaborate 
                        if (regv.Col_Id != 0 && regv.Col_Id != null && !colIds.Contains(regv.Col_Id.Value))
                            colIds.Add(regv.Col_Id.Value);

                        // sono forzate sulla regv da checcare l'ora di entrata e uscita fisica così da evitare
                        // mancati controlli per la presenza di min value precedentemente inseriti
                        regv.Data_Ora_Fis_E = newRegE.Registrazione_Data_Ora_Fis_Reg;
                        if (newRegU != null)
                            regv.Data_Ora_Fis_U = newRegU.Registrazione_Data_Ora_Fis_Reg;

                        #endregion

                        #region Manage Regs orig date

                        // è calcolato il fatto che si sta elaborando un'uscita nello stesso giorno
                        bool isSameDay = true;
                        // se non c'è il notturno è sicuramente true
                        if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && (RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.OverMidnight || RepoManager.ParamRepo.ParametersRow.TipoNotturno == (int)NocturneTypeEnum.Duration))
                            isSameDay = regv.IsUTimeSameDayE;

                        // gestione della data/ora originale delle reg 
                        RepoManager.RegRepo.ManageOrigDates(newRegE, regEId, origDateE, newRegU, regUId, origDateU, isSameDay);

                        #endregion

                        #region Manage Blocking regs

                        // gestione del codice di accoppiamento per le reg bloccate
                        RepoManager.RegRepo.PerformBlockedRegsCouple(newRegE, newRegU, regv.RegE);

                        #endregion

                        Tab_Decod motRichiesta = RepoManager.Tab_DecodRepo.Single(td => td.Tab_Decod_Id == oldRegE.Motivazione_Reg_Id);
                        string codNuovaRichiesta = "";
                        var tmpRich = motRichiesta.Decodifica_Tab.Split(' ');
                        for (int i = 1; i < tmpRich.Length; i++)
                        {
                            if (i != 1)
                            {
                                codNuovaRichiesta = codNuovaRichiesta + " " + tmpRich[i];
                            }
                            else
                            {
                                codNuovaRichiesta += tmpRich[i];
                            }

                        }
                        if (!codNuovaRichiesta.Contains("Ferie"))
                            conferma = 1;

                        Tab_Decod mot = RepoManager.Tab_DecodRepo.Single(td => td.Decodifica_Tab == codNuovaRichiesta);

                        newRegE.Motivazione_Reg_Id = mot.Tab_Decod_Id;
                        Col cols = RepoManager.ColRepo.Single(c => c.Col_Id == tmpReg_V.Col_Id);
                        bool isOrario = true;
                        if (cols.Tab_Orari_Tipo_Id != null)
                        {
                            Tab_Orari orarioFinale = new Tab_Orari();
                            Tab_Orari_Tipo tipoOr = RepoManager.Tab_OrariTipoRepo.FirstOrDefault(td => td.Tab_Orari_Tipo_Id == cols.Tab_Orari_Tipo_Id);
                            var orari = RepoManager.Tab_OrariRepo.GetAllQueryable(o => o.Tab_Orari_Tipo_Id == tipoOr.Tab_Orari_Tipo_Id).GroupBy(or => or.Data_Inizio).ToList();
                            if (orari.Count > 0)
                            {
                                foreach (var singoloOrario in orari.Last())
                                {
                                    switch (newRegE.Registrazione_Data_Ora_Fis_Reg.DayOfWeek)
                                    {
                                        case DayOfWeek.Monday:
                                            if (singoloOrario.G1)
                                            {
                                                orarioFinale = singoloOrario;
                                            }
                                            break;
                                        case DayOfWeek.Tuesday:
                                            if (singoloOrario.G2)
                                            {
                                                orarioFinale = singoloOrario;
                                            }
                                            break;
                                        case DayOfWeek.Wednesday:
                                            if (singoloOrario.G3)
                                            {
                                                orarioFinale = singoloOrario;
                                            }
                                            break;
                                        case DayOfWeek.Thursday:
                                            if (singoloOrario.G4)
                                            {
                                                orarioFinale = singoloOrario;
                                            }
                                            break;
                                        case DayOfWeek.Friday:
                                            if (singoloOrario.G5)
                                            {
                                                orarioFinale = singoloOrario;
                                            }
                                            break;
                                        case DayOfWeek.Saturday:
                                            if (singoloOrario.G6)
                                            {
                                                orarioFinale = singoloOrario;
                                            }
                                            break;
                                        case DayOfWeek.Sunday:
                                            if (singoloOrario.G7)
                                            {
                                                orarioFinale = singoloOrario;
                                            }
                                            break;
                                    }
                                }
                                if (orarioFinale != null)
                                {
                                    var durataOrario = orarioFinale.Durata_Minuti;
                                    double durataRegistrazione = 0;
                                    if (newRegU != null)
                                    {
                                        durataRegistrazione = (newRegU.Registrazione_Data_Ora_Fis_Reg - newRegE.Registrazione_Data_Ora_Fis_Reg).TotalMinutes;
                                    }
                                    else 
                                    {
                                        durataRegistrazione = newRegE.Rettifica_Durata.Value;
                                    }

                                    if (durataRegistrazione > durataOrario)
                                    {
                                        isOrario = false;
                                    }
                                }
                            }
                        }

                        if (newRegU != null)
                            newRegU.Motivazione_Reg_Id = mot.Tab_Decod_Id;

                        if (isOrario) 
                        {
                            #region Add Checked Regs To updates list
                            // controllo se nella timbratura è stata impostata la motivazione ferie o permesso, in questo caso provedo a processarle
                            toUpdateRegs.Add(newRegE);

                            // aggiunta la nuova reg in uscita tra quelle da aggiungere (solo se valorizzata)
                            if (newRegU != null)
                                toUpdateRegs.Add(newRegU);

                            if (oldRegE != null)
                                toDeleteRegs.Add(oldRegE);

                            // se la vecchia reg in uscita è valorizzata allora viene impostata come reg da cancellare
                            if (oldRegU != null)
                                toDeleteRegs.Add(oldRegU);

                            #endregion
                            Col col = RepoManager.ColRepo.Single(c => c.Col_Id == tmpReg_V.Col_Id);
                            InviaConferma(conferma, col, from, to);
                        }
                        else
                        {
                            //SetErrorMessageDictionary(regv, errorByColId, validationErrors);
                            IList<Dictionary<string, string>> currentValidationErrorsList = new List<Dictionary<string, string>>();
                            currentValidationErrorsList.Add(validationErrors);
                            EditErrorMessage = "Richiesta autorizzata maggiore dell'orario del collaboratore";
                            if (errorByColId.Count == 0) 
                            {
                                errorByColId.Add(EditErrorMessage, currentValidationErrorsList);
                            }
                        }
                    }
                }

                #region Elaborate all modified regs

                // se non ci sono stati errori nell'elaborazione
                if (errorByColId.Count == 0)
                {
                    // cancellazione di tutte le reg da cancellare
                    RepoManager.RegRepo.Delete(toDeleteRegs, true);

                    // aggiunta di tutte le reg da aggiungere                   
                    RepoManager.RegRepo.Add(toUpdateRegs, true);

                    // inizializzazione delle date di inizio e fine elaborate
                    DateTime startElabDate = DateTime.MinValue;
                    DateTime endElabDate = DateTime.MinValue;

                    // ciclo di elaborazione delle date da rielaborare (gestendo solo i valori univoci
                    foreach (var currentDate in toElaborateDates.Distinct())
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


                        // elaborazione delle registrazioni recuperate
                        if (toElaborateRegs.Any())
                            RepoManager.RegRepo.Elaborate(toElaborateRegs, startElabDate, endElabDate, true, false);
                    }
                }

                // se si sono verificati degli errori, vengono visualizzati a video
                if (errorByColId.Count > 0)
                    //EditErrorMessage = StringFromDictionary(validationErrors);
                    EditErrorMessage = GetErrorMessageFromDictionary(errorByColId);
                else
                {
                    // se non ci sono stati errori nel salvataggio allora segnalo che il primo salvataggio è avvenuto
                    PrimoSalvataggio = true;

                    // se l'elaborazione si è conclusa senza errori allora si imposta a null la proprietà che
                    // ne permette la visualizzazione sulla griglia
                    EditErrorMessage = null;

                    // al termine delle operazioni resetto i data source
                    ResetDataSourceAndBind(true);
                }

                #endregion

            } else if (e.Parameters.StartsWith("deleteRequest")) {
                //Dictionary<string, string> errors = new Dictionary<string, string>();
                var splitter = e.Parameters.Split(new char[] { '|' });

                int currentRegId = Convert.ToInt32(splitter[1]);

                //viene stratta la registrazione che ho premuto l'edit multiplo
                Reg currentReg = RepoManager.RegRepo.Single(reg => reg.Reg_Id == currentRegId);

                // per prima cosa nell'elaborate viene effettuato il bind della griglia di modo da ricevere i dati aggiornati
                BindGrid();

                // generazione del dizionario che viualizzerà gli errori di update
                var errorByColId = new Dictionary<string, IList<Dictionary<string, string>>>();

                // elenco delle date con cui costruire il periodo di rielaborazione
                var toElaborateDates = new HashSet<DateTime>();

                // inizializzazione delle liste che conterranno le reg da modificare e le reg da cancellare
                var toUpdateRegs = new List<Reg>();
                var toDeleteRegs = new List<Reg>();

                Reg_V tmpReg_V = RepoManager.Reg_VRepo.Single(regv => regv.RegE == currentRegId);

                // inizializzazione della lista dei collaboratori inclusi nel processo di elaborazione
                var colIds = new List<int>();

                // per ogni regv modificata vado a controllare quale motivazione è stata impostata
                var regVsToCheck = RegVsToUpdate;

                // alla lista per l'elaborazione di update vanno tolti tutti i record marcati per la cancellazione
                regVsToCheck = regVsToCheck.Where(regV => !RegVsToDelete.Select(dRegV => dRegV.TmpNewId).Where(tmpNewId => tmpNewId != String.Empty).Contains(regV.TmpNewId)).ToList();

                if (!tmpReg_V.Note_Reg.Contains("-"))
                {
                    regVsToCheck.Add(tmpReg_V);

                    Col col = RepoManager.ColRepo.Single(c => c.Col_Id == tmpReg_V.Col_Id);

                    foreach (var regv in regVsToCheck)
                    {
                        #region Add Rejected Regs To deletes List
                        Reg deleteRegE = RepoManager.RegRepo.Single(reg => reg.Reg_Id == regv.RegE);

                        if (deleteRegE != null)
                            toDeleteRegs.Add(deleteRegE);

                        Reg deleteRegU = RepoManager.RegRepo.SingleOrDefault(rv => rv.Reg_Id == regv.RegU);

                        // se la vecchia reg in uscita è valorizzata allora viene impostata come reg da cancellare
                        if (deleteRegU != null)
                            toDeleteRegs.Add(deleteRegU);

                        #endregion
                    }
                    InviaConferma(3, col, tmpReg_V.Data_Reg.Value, tmpReg_V.Data_Reg.Value);
                }
                else
                {
                    var dates = tmpReg_V.Note_Reg.Split('-');
                    DateTime from = DateTime.Parse(dates[0] + " 00:00:00");
                    DateTime to = DateTime.Parse(dates[1] + " 23:59:59");
                    Col col = RepoManager.ColRepo.Single(c => c.Col_Id == tmpReg_V.Col_Id);
                    List<Reg_V> tmpRegvs = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Motivazione_Reg_Id == tmpReg_V.Motivazione_Reg_Id && r.Col_Id == tmpReg_V.Col_Id && (r.Data_Reg.Value >= from && r.Data_Reg.Value <= to)).ToList();
                    regVsToCheck.AddRange(tmpRegvs);

                    foreach (var regv in regVsToCheck)
                    {
                        #region Add Rejected Regs To deletes List

                        Reg deleteRegE = RepoManager.RegRepo.Single(reg => reg.Reg_Id == regv.RegE);

                        if (deleteRegE != null)
                            toDeleteRegs.Add(deleteRegE);

                        Reg deleteRegU = RepoManager.RegRepo.SingleOrDefault(rv => rv.Reg_Id == regv.RegU);

                        // se la vecchia reg in uscita è valorizzata allora viene impostata come reg da cancellare
                        if (deleteRegU != null)
                            toDeleteRegs.Add(deleteRegU);

                        #endregion
                    }
                    InviaConferma(2,col,from,to);
                }

                #region Elaborate all modified regs

                // se non ci sono stati errori nell'elaborazione
                if (errorByColId.Count == 0)
                {
                    // cancellazione di tutte le reg da cancellare
                    RepoManager.RegRepo.Delete(toDeleteRegs, true);

                    // aggiunta di tutte le reg da aggiungere                   
                    RepoManager.RegRepo.Add(toUpdateRegs, true);

                    // inizializzazione delle date di inizio e fine elaborate
                    DateTime startElabDate = DateTime.MinValue;
                    DateTime endElabDate = DateTime.MinValue;

                    // ciclo di elaborazione delle date da rielaborare (gestendo solo i valori univoci
                    foreach (var currentDate in toElaborateDates.Distinct())
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


                        // elaborazione delle registrazioni recuperate
                        if (toElaborateRegs.Any())
                            RepoManager.RegRepo.Elaborate(toElaborateRegs, startElabDate, endElabDate, true, false);
                    }
                }

                // se si sono verificati degli errori, vengono visualizzati a video
                if (errorByColId.Count > 0)
                    //EditErrorMessage = StringFromDictionary(validationErrors);
                    EditErrorMessage = GetErrorMessageFromDictionary(errorByColId);
                else
                {
                    // se non ci sono stati errori nel salvataggio allora segnalo che il primo salvataggio è avvenuto
                    PrimoSalvataggio = true;

                    // se l'elaborazione si è conclusa senza errori allora si imposta a null la proprietà che
                    // ne permette la visualizzazione sulla griglia
                    EditErrorMessage = null;

                    // al termine delle operazioni resetto i data source
                    ResetDataSourceAndBind(true);
                }

                #endregion
            }


        }

        //al metodo passo l'esito dell'operazione
        //@param
        //esito: in base al valore vado a impostare il titolo e il testo della mail
        //0: Ferie accettate    1: Permesso accettato   2: Ferie riufiutate   3: Permesso riufiutato    4: Periodo di ferie accettato   5: Periodo di permessoa accettato
        public string InviaConferma(int esito, Col cols, DateTime from, DateTime to)
        {
            string titoloMail = "";
            string mailTo = RepoManager.ParamRepo.ParametersRow.CompanyEmail;

            //Prepara il body della mail caricando il css
            string mailBody = "<div style=\"font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; \">",
                   errorMessage = "";
            switch (esito)
            {
                case 0:
                    titoloMail = "PowerWeb - Ferie accettate";
                    mailBody += "<p>Le ferie per il periodo " + from.ToString("dddd d MMMM yyyy") + "-"+to.ToString("dddd d MMMM yyyy")+" richieste dal collaboratore " + cols.CognomeNome_Col + " sono state accettate</p>";
                    break;
                case 1:
                    titoloMail = "PowerWeb - Permesso accettato";
                    mailBody += "<p>Il permesso per il giorno " + from.ToString("dddd d MMMM yyyy") + " richiesto dal collaboratore " + cols.CognomeNome_Col + " è stato accettato </p>";
                    break;
                case 2:
                    titoloMail = "PowerWeb - Ferie rifiutate";
                    mailBody += "<p>Le ferie per il periodo " + from.ToString("dddd d MMMM yyyy") + "-" + to.ToString("dddd d MMMM yyyy") + " richieste dal collaboratore " + cols.CognomeNome_Col + " sono state rifiutate</p>";
                    break;
                case 3:
                    titoloMail = "PowerWeb - Permesso rifiutato";
                    mailBody += "<p>Il permesso per il giorno " + from.ToString("dddd d MMMM yyyy") + " richiesto dal collaboratore " + cols.CognomeNome_Col + " è stato rifiutato </p>";
                    break;
                default:
                    break;
            }

            mailBody += "</div>";

            if (cols.Email_Col != "" && cols.Email_Col != null) {
                mailTo = mailTo + ";" + cols.Email_Col;
            }

            errorMessage = CommonService.sendMail(mailTo, titoloMail, mailBody, "newsletter@winit.it", titoloMail, new string[] { });

            DateTime today = DateTime.Today;

            return errorMessage;
        }

        protected void gvAutFerPerEdit_AfterPerformCallback(object sender, ASPxGridViewAfterPerformCallbackEventArgs e)
        {
            ASPxGridView gv = (ASPxGridView)sender;
            //gv.JSProperties.Add("cpIsToShowEditGrid", false);
            gv.JSProperties["cpIsToShowEditGrid"] = true;

            gv.JSProperties.Add("cpErrorString", null);
            if (!String.IsNullOrEmpty(EditErrorMessage))
                gv.JSProperties["cpErrorString"] = EditErrorMessage;

            gv.JSProperties.Add("cpCallBackParameter", _callBackParameter);

        }

        #endregion

        #region Report

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, ASPxPanel customOptionsPanel = null)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Metodi privati

        /// <summary>
        /// Imposta il tipo di modifica della griglia (e elementi accessori) in base alla customizzazione impostata.
        /// </summary>
        private void SetGridEditType()
        {
            // inizializzazione della colonna comandi
            var commandCol = gvAutFerPerEdit.Columns[0] as GridViewCommandColumn;

            // in base al tipo di modifica richiesta si impostano i vari dati
            switch (EditType)
            {
                case EditTypeErrModuleEnum.Inline:
                    // impostazione del tipo di modifica
                    gvAutFerPerEdit.SettingsEditing.Mode = GridViewEditingMode.Inline;

                    // nella modifica inline è nascosto il pulsante di update in memoria
                    btnUpdateMemory.ClientVisible = false;

                    // visualizzazione dei pulsanti di update
                    commandCol.ShowEditButton = true;

                    break;
                case EditTypeErrModuleEnum.BatchEdit:

                    // in caso di modifica batch allora si nasconde il pulsante di modifica
                    commandCol.ShowEditButton = false;

                    break;
            }
        }

        /// <summary>
        /// Impostazione in lingua degli elementi della pagina.
        /// </summary>
        private void LocalizeElements()
        {
            //BtnPropostaChiusura.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_PROPOSTA_CHIUSURA);
            //CbChiusuraAllSelected.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_TUTTE_LE_ERRATE);
            //LblColPreFilter.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE);
            //LblIfEmptyAll.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_SE_VUOTO_TUTTI);
        }

        /// <summary>
        /// Costruisce e ritorna uno stringa con la query per la ricerca delle registrazioni errate (utilizzando i dati di filtro).
        /// </summary>
        /// <returns>Lo stringa da utilizzare come query per ricercare le reg_V errate</returns>
        private string BuildRegVErrQuery()
        {
            StringBuilder sbQuery = new StringBuilder("SELECT * FROM dbo.Reg_V WHERE ");

            IEnumerable<char> selectedErrors;
            var permessoId = RepoManager.Tab_DecodRepo.GetAllQueryable(d => d.Chiave_Tab == "RP" && d.Nome_Tab == "MOTIVAZIONI").ToList();
            var ferieId = RepoManager.Tab_DecodRepo.GetAllQueryable(f => f.Chiave_Tab == "RF" && f.Nome_Tab == "MOTIVAZIONI").ToList();
            sbQuery.AppendFormat("(Motivazione_Reg_Id = {0} OR Motivazione_Reg_Id = {1}", permessoId.First().Tab_Decod_Id, ferieId.First().Tab_Decod_Id);
            sbQuery.Append(')');
            return sbQuery.ToString();
        }

        /// <summary>
        /// Imposta il testo della label che indica il filtro sui collaboratori utilizzando il valore di PrimoSalvataggio
        /// </summary>
        private void SetColDataSourceLabelText()
        {
            // viene aggiornata la label di filtro in base al valore pasato come parametro
            //LblColDataSource.Text = PrimoSalvataggio
            //    ? BusinessService.GetLocalizedString(PowerWebResources.STR_LABEL_ONLY_EDITED_COLS)
            //    : BusinessService.GetLocalizedString(PowerWebResources.STR_LABEL_ALL_ERR_COLS);
        }

        #endregion

    }


}



