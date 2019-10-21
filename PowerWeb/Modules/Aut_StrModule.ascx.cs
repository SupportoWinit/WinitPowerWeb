using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Business.Repository;
using Common;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.Data;
using Domain;

namespace PowerWeb.Modules
{
    public partial class Aut_StrModule : BaseGridModule
    {

        #region Constants

        /// <summary>
        /// Il nome del campo chiave della griglia principale del modulo
        /// </summary>
        private const string KeyFieldName = "Aut_Str_Id";

        #endregion

        #region Public Properties

        /// <summary>
        /// Recupera la griglia principale del modulo corrente.
        /// </summary>
        /// <value>
        /// La griglia principale del modulo corrente.
        /// </value>
        public override ASPxGridView GridView
        {
            get { return gvAutStr; }
        }

        /// <summary>
        /// Recupera il template utilizzato per la costruzione della form di data input.
        /// </summary>
        /// <value>
        /// Il template utilizzato per la costruzione della form di data input.
        /// </value>
        public override PowerFormTemplate EditFormTemplate
        {
            get
            {
                var template = PowerWebContext.GetFromSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID);

                if (template == null)
                {
                    var templateDic = EditDictionaryManager.GetEditDictionaryAut_Str();
                    template = new PowerFormTemplate(this, templateDic);
                    PowerWebContext.SetToSession<PowerFormTemplate>("PowerFormTemplate_" + GridView.ID, template);
                }
                return template;
            }
        }

        /// <summary>
        /// Recupera il tipo entità gestito dal modulo.
        /// </summary>
        /// <value>
        /// Il tipo entità gestito dal modulo.
        /// </value>
        public override Type EntityType
        {
            get { return typeof(Aut_Str); }
        }
        
        #endregion

        #region Private Properties

        /// <summary>
        /// Recupera il valore che indica se la griglia corrente deve utilizzare il batch mode o meno.
        /// </summary>
        /// <value>
        /// <c>true</c> se la griglia corrente deve utilizzare il batch mode; altrimenti, <c>false</c>.
        /// </value>
        protected bool IsInBatchMode
        {
            get { return GridView.SettingsEditing.Mode == GridViewEditingMode.Batch; }
        }

        #endregion

        #region Page Events

        /// <summary>
        /// Handles the Init event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Page_Init(object sender, EventArgs e)
        {
            // compilazione delle etichette della griglia
            PowerWebService.FillGridLabels(typeof(Aut_Str), GridView);

            // gestione dei combobox in griglia
            PowerWebService.FillComboboxes(GridView);

            // bind dei dati della griglia
            BindGrid();
        }

        #endregion

        #region Grid Events

        /// <summary>
        /// Handles the OnDataBinding event of the gvAutStr control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gvAutStr_OnDataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the OnInitNewRow event of the gvAutStr control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxDataInitNewRowEventArgs"/> instance containing the event data.</param>
        protected void gvAutStr_OnInitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
        {
            ASPxGridView grid = sender as ASPxGridView;
            if (grid != null)
            {
                Aut_Str initAutStr = RepoManager.Aut_StrRepo.Init();
                PowerWebService.FillGridProperties(initAutStr, e.NewValues);
                PowerWebService.FillGridClonedProperties(Page, GridView, e.NewValues);
            }
        }

        /// <summary>
        /// Handles the OnRowValidating event of the gvAutStr control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxDataValidationEventArgs"/> instance containing the event data.</param>
        protected void gvAutStr_OnRowValidating(object sender, ASPxDataValidationEventArgs e)
        {
            var newAutStr = new Aut_Str();

            if (IsInBatchMode)
            {
                var currentId = Convert.ToInt32(e.Keys[GridView.KeyFieldName]);
                if (currentId > 0)
                {
                    var currentAutStr = GridView.GetRow(e.VisibleIndex);
                    PowerWebService.FillValues(currentAutStr, e.NewValues, e.OldValues);
                }
            }

            PowerWebService.FillEntityProperties(newAutStr, e.NewValues);
            PowerWebService.FillEntityKey(newAutStr, e.Keys, KeyFieldName);
            RepoManager.Aut_StrRepo.SetEntityBeforeAddOrUpdate(newAutStr);
            PowerWebService.AddValidationErrors(RepoManager.Aut_StrRepo.Check(newAutStr, e.IsNewRow), e.Errors, GridView, typeof(Aut_Str));
            if (e.HasErrors)
                e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
        }

        /// <summary>
        /// Handles the OnRowInserting event of the gvAutStr control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxDataInsertingEventArgs"/> instance containing the event data.</param>
        protected void gvAutStr_OnRowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            var newAutStr = new Aut_Str();
            PowerWebService.FillEntityProperties(newAutStr, e.NewValues);
            RepoManager.Aut_StrRepo.SetEntityBeforeAddOrUpdate(newAutStr);
            RepoManager.Aut_StrRepo.Add(newAutStr, true);
            e.Cancel = true;
            GridView.CancelEdit();
            BindGrid();
        }

        /// <summary>
        /// Handles the OnRowUpdating event of the gvAutStr control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxDataUpdatingEventArgs"/> instance containing the event data.</param>
        protected void gvAutStr_OnRowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            var currentId = Convert.ToInt32(e.Keys[GridView.KeyFieldName]);
            Aut_Str currentAutStr = RepoManager.Aut_StrRepo.Single(u => u.Aut_Str_Id == currentId);
            PowerWebService.FillEntityProperties(currentAutStr, e.NewValues);
            RepoManager.Aut_StrRepo.SetEntityBeforeAddOrUpdate(currentAutStr);
            RepoManager.Aut_StrRepo.SaveChanges();
            e.Cancel = true;
            GridView.CancelEdit();
            BindGrid();
        }

        /// <summary>
        /// Handles the OnRowDeleting event of the gvAutStr control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ASPxDataDeletingEventArgs"/> instance containing the event data.</param>
        protected void gvAutStr_OnRowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            var currentId = Convert.ToInt32(e.Keys[GridView.KeyFieldName]);
            Aut_Str currentAutStr = RepoManager.Aut_StrRepo.Single(u => u.Aut_Str_Id == currentId);
            RepoManager.Aut_StrRepo.Delete(currentAutStr, true);
            e.Cancel = true;
            BindGrid();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Effettua il bind della griglia principale dell'applicativo.
        /// </summary>
        private void BindGrid()
        {
            GridView.KeyFieldName = KeyFieldName;
            IQueryable<Aut_Str> currDataSource = Enumerable.Empty<Aut_Str>().AsQueryable();
            var emptyList = Enumerable.Empty<Aut_Str>();
            if (IsToPopulateGrid)
            {
                currDataSource = RepoManager.Aut_StrRepo.GetAll(true).AsQueryable();
                GridView.DataSource = currDataSource.Any() ? currDataSource : emptyList;
            }
            else
                GridView.DataSource = emptyList;

        }

        #endregion

    }
}