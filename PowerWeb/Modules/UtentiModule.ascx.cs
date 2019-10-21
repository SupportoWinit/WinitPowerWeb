using System.Collections.Generic;
using Business.Repository;
using Domain;
using Common;
using DevExpress.Web.Data;
using log4net;
using Reports;
using System;
using DevExpress.Web.ASPxGridView;
using System.Linq;
using System.Collections.Specialized;

namespace PowerWeb.Modules
{
    public partial class UtentiModule : BaseGridModule, IPrintModule, ILogModule
    {

        private Utenti _utentiStub = null;
        const String KEYFIELDNAME = "Utenti_Id";
        private static readonly ILog _log = LogManager.GetLogger(typeof(UtentiModule));

        public override ASPxGridView GridView
        {
            get
            {
                return gvUsers;
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
                    var templateDic = EditDictionaryManager.GetEditDictionaryUtenti();
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

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Utenti), GridView);
            PowerWebService.FillComboboxes(gvUsers);

            GridView.HtmlDataCellPrepared += GridView_HtmlDataCellPrepared;

            BindGrid();
        }

        private void BindGrid()
        {
            // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
            // evitare errori nel pulsante di inserimento
            gvUsers.KeyFieldName = KEYFIELDNAME;
            IQueryable<Utenti> currDataSource = Enumerable.Empty<Utenti>().AsQueryable();
            var emptyList = Enumerable.Empty<Utenti>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.UtentiRepo.GetAll(true).AsQueryable();
                gvUsers.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                gvUsers.DataSource = emptyList;
        }

        protected void gvUtenti_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        public override Type EntityType
        {
            get { return typeof(Utenti); }
        }

        #region gvUsers : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BatchUpdate
        protected void gvUsers_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;

            if (grid != null)
            {
                Utenti initUser = RepoManager.UtentiRepo.Init();
                var registrationDate = initUser.Data_Registrazione_Utente;

                PowerWebService.FillGridProperties(initUser, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);

                e.NewValues[CommonService.GetPropertyName(() => _utentiStub.Data_Registrazione_Utente)] = registrationDate;
            }
        }

        protected void gvUsers_RowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            Utenti user = new Utenti();

            //se sono in batch edit mode carico tutti i campi in questo modo ho sempre tutti i campi aggiornati
            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[gvUsers.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentuser = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentuser, e.NewValues, e.OldValues);
                }
            }



            PowerWebService.FillEntityProperties(user, e.NewValues);
            PowerWebService.FillEntityKey(user, e.Keys, KEYFIELDNAME);
            RepoManager.UtentiRepo.SetEntityBeforeAddOrUpdate(user);
            PowerWebService.AddValidationErrors(RepoManager.UtentiRepo.Check(user, e.IsNewRow), e.Errors, gvUsers, typeof(UtentiModule));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        protected void gvUsers_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            _log.Info(String.Format("UTENTI-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
            Utenti newUser = RepoManager.UtentiRepo.Init();
            PowerWebService.FillEntityProperties(newUser, e.NewValues);
            RepoManager.UtentiRepo.SetEntityBeforeAddOrUpdate(newUser);
            RepoManager.UtentiRepo.Add(newUser);
            //Quando si INSERISCE un NUOVO UTENTE in Tabella Uteneti viene creato AUTOMATICAMENTE anche il corrispondente record in TAB_AUT per il nuovo Utente
            Tab_Aut newTabAut = RepoManager.Tab_AutRepo.Init();
            newTabAut.Del_Aut = newTabAut.Funz_Aut = newTabAut.Ins_Aut = newTabAut.Mod_Aut = newUser.Liv_Utente_Edit;
            newTabAut.Utenti = newUser;
            RepoManager.Tab_AutRepo.Add(newTabAut, true);

            e.Cancel = true;
            gvUsers.CancelEdit();
            BindGrid();
        }

        protected void gvUsers_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            _log.Info(String.Format("UTENTI-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvUsers.KeyFieldName]);
            Utenti currentUser = RepoManager.UtentiRepo.Single(u => u.Utenti_Id == currentId);

            var newClearPassword = e.NewValues[CommonService.GetPropertyName(() => currentUser.Password)];
            string oldEncryptedPassword = currentUser.PasswordHash_Utente;


            PowerWebService.FillEntityProperties(currentUser, e.NewValues);
            RepoManager.UtentiRepo.SetEntityBeforeAddOrUpdate(currentUser);

            currentUser.Liv_Utente = Convert.ToInt32(e.NewValues[CommonService.GetPropertyName(() => _utentiStub.Liv_Utente_Edit)]);
            currentUser.N_GG_Val_Psw_Utente = 90;  //INSERIRE PARAMETROOOOOOOOOOOO
            currentUser.DataUltimoAgg_Psw_Utente = DateTime.Now;

            RepoManager.UtentiRepo.SaveChanges();

            if (RepoManager.ParamRepo.ParametersRow.Abilita_Privacy)
            {
                ManagePasswordChange(currentUser, newClearPassword as string, oldEncryptedPassword);
            }

            e.Cancel = true;
            gvUsers.CancelEdit();
            BindGrid();
        }

        protected void gvUsers_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            _log.Info(String.Format("UTENTI-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
            var currentId = Convert.ToInt32(e.Keys[gvUsers.KeyFieldName]);
            Utenti currentUser = RepoManager.UtentiRepo.Single(u => u.Utenti_Id == currentId);
            RepoManager.Tab_AutRepo.Delete(currentUser.Tab_Aut, false);
            RepoManager.UtentiRepo.Delete(currentUser, false);
            RepoManager.UtentiRepo.SaveChanges();
            e.Cancel = true;
            BindGrid();
        }

        public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {
        }

        void GridView_HtmlDataCellPrepared(object sender, ASPxGridViewTableDataCellEventArgs e)
        {
           

            if(e.DataColumn.FieldName == CommonService.GetPropertyName(() => _utentiStub.SecretQuestion))
            {
                var codUser = ((ASPxGridView)sender).GetRowValues(e.VisibleIndex, CommonService.GetPropertyName(() => _utentiStub.Codice_Utente)).ToString();
                var currentUser = PowerWebContext.Current.User.Codice_Utente;

                if (currentUser != codUser)
                {
                    e.Cell.Style.Add("color", "transparent");
                }

            }

            if (e.DataColumn.FieldName == CommonService.GetPropertyName(() => _utentiStub.SecretAnswer))
            {
                var codUser = ((ASPxGridView)sender).GetRowValues(e.VisibleIndex, CommonService.GetPropertyName(() => _utentiStub.Codice_Utente)).ToString();
                var currentUser = PowerWebContext.Current.User.Codice_Utente;

                if (currentUser != codUser)
                {
                    e.Cell.Style.Add("color", "transparent");
                    e.DataColumn.EditFormSettings.Visible = DevExpress.Utils.DefaultBoolean.False;
                }
            }
        }

        #endregion

        public override void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        //Gestione Filtri CUSTOM x i Campi DATA (va comunque definita vuota se non ce ne sono)
        {
            if (e.Column.FieldName == CommonService.GetPropertyName(() => _utentiStub.Data_Registrazione_Utente) ||
              e.Column.FieldName == CommonService.GetPropertyName(() => _utentiStub.DataOraUltimaModifica_Utente))
                PowerWebService.GridHeaderFilterFillItems(e);
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, DevExpress.Web.ASPxPanel.ASPxPanel customOptionsPanel = null)
        {
            List<Utenti> users = CommonService.ConvertTo<Utenti>(items);
            XRUtenti userReport = new XRUtenti(users, PowerWebService.ConvertTabPageExtendedToString(selectedTabs));
            return new ExtXtraReport { Report = userReport, PictureBox = userReport.CompanyLogo };
        }

        public ILog Log
        {
            get { return _log; }
        }





        /// <summary>
        /// Procedura che gestisce l'eventuale modifica della password mantenendo uno storico
        /// delle password precedenti (per un eventuale recupero)
        /// </summary>
        /// <param name="currentUser">L'utente di cui si sta modificando la password.</param>
        /// <param name="newDecrPassword">La nuova password in chiaro.</param>
        /// <param name="oldEncrPassword">La vecchia password criptata.</param>
        private void ManagePasswordChange(Utenti currentUser, string newDecrPassword, string oldEncrPassword)
        {
            if (newDecrPassword == null || newDecrPassword == "") //Controllo che la password inserita in griglia sia presente
                return;

            //Cripto la nuova passoword e controllo se è diversa da quella precedente
            string newEncrPassword = Business.BusinessService.Encrypt(newDecrPassword, currentUser.SaltKey_Utente);


            if (newEncrPassword == oldEncrPassword) //Controllo che la password inserita sia diversa da quella precedente
                return;


            DateTime minPswDate = DateTime.Now;
            
            //Estrazione data minima da campo non nullable
            var date = RepoManager.Utenti_HistoryRepo.Find(his => his.Utenti_Id == currentUser.Utenti_Id).Max(c => (DateTime?)c.Data_Change_Psw_Utenti_History);

            if (date != null)
                minPswDate = (DateTime)date;


            //Creazione nuovo record
            Utenti_History newUserHistory = new Utenti_History()
            {

                Utenti_Id = currentUser.Utenti_Id,
                Data_Change_Psw_Utenti_History = DateTime.Now,
                Data_Old_Change_Psw_Utenti_History = minPswDate,
                Password_Hash_Utenti_History = oldEncrPassword,
                Salt_Key_Utenti_History = currentUser.SaltKey_Utente

            };
            
            try
            {
                RepoManager.Utenti_HistoryRepo.Add(newUserHistory, true);

                _log.InfoFormat("Aggiornata nuova password dall'utente {0} per l'utente {1} ", PowerWebContext.Current.User.Codice_Utente, currentUser.Codice_Utente);
                _log.InfoFormat("Inserito nuovo record nella tabella UTENTI_HISTORY dall'utente {0}", PowerWebContext.Current.User.Codice_Utente, currentUser.Codice_Utente);
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la routine di aggiornamento della password per l'utente {0} con exception {1}", "", ex.Message);
            }
            
        }

    }
}