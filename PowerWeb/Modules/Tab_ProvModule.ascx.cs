using System;
using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using Domain;
using Common;
using DevExpress.Web.Data;
using DevExpress.Web.ASPxGridView;
using Business;
using Reports;
using log4net;

namespace PowerWeb.Modules
{
    public partial class Tab_ProvModule : BaseGridModule, ILogModule
  {
    const String KEYFIELDNAME = "Tab_Prov_Id";
    private static readonly ILog _log = LogManager.GetLogger(typeof(Tab_ProvModule));

    public override ASPxGridView GridView
    {
      get { return gvTabProv; }
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
          var templateDic = EditDictionaryManager.GetEditDictionaryTab_Prov();
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
      PowerWebService.FillGridLabels(typeof(Tab_Prov), GridView);
      PowerWebService.FillComboboxes(gvTabProv);
      BindGrid();
    }

    private void BindGrid()
    {
        gvTabProv.KeyFieldName = KEYFIELDNAME;
        IQueryable<Tab_Prov> currDataSource = Enumerable.Empty<Tab_Prov>().AsQueryable();
        if (IsToPopulateGrid)
            //La Lettura viene effettuata in base al Valore del TASTO di ON/OFF
            currDataSource = RepoManager.Tab_ProvRepo.GetAll(true).AsQueryable();
        gvTabProv.DataSource = currDataSource;
    }

    protected void gvTabProv_DataBinding(object sender, EventArgs e)
    {
        BindGrid();
    }

    public override Type EntityType
    {
      get { return typeof(Lingue); }
    }

    #region gvTabProv : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-Batch_Update
    protected void gvTabProv_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
    {
      ASPxGridView grid = sender as ASPxGridView;
      if (grid != null)
      {
        Tab_Prov initTabProv = RepoManager.Tab_ProvRepo.Init();
        PowerWebService.FillGridProperties(initTabProv, e.NewValues);
        PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
      }
    }

    protected void gvTabProv_RowValidating(object sender, ASPxDataValidationEventArgs e)
    {
      Tab_Prov newTabProv = new Tab_Prov();

      //se sono in batch edit mode carico tutti i campi in questo modo ho sempre tutti i campi aggiornati
      if (IsInBatchMode)
      {
          var currentId = Convert.ToInt32(e.Keys[gvTabProv.KeyFieldName]);
          if (currentId > 0)
          {
              var currentTabProv = GridView.GetRow(e.VisibleIndex);
              PowerWebService.FillValues(currentTabProv, e.NewValues, e.OldValues);
          }
      }

      PowerWebService.FillEntityProperties(newTabProv, e.NewValues);
      PowerWebService.FillEntityKey(newTabProv, e.Keys, KEYFIELDNAME);
      RepoManager.Tab_ProvRepo.SetEntityBeforeAddOrUpdate(newTabProv);
      PowerWebService.AddValidationErrors(RepoManager.Tab_ProvRepo.Check(newTabProv, e.IsNewRow), e.Errors, gvTabProv, typeof(Tab_ProvModule));
      if (e.HasErrors)
        e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
    }

    protected void gvTabProv_RowInserting(object sender, ASPxDataInsertingEventArgs e)
    {
      _log.Info(String.Format("TAB_PROV-Row inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
      Tab_Prov newTabProv = new Tab_Prov();
      PowerWebService.FillEntityProperties(newTabProv, e.NewValues);
      RepoManager.Tab_ProvRepo.SetEntityBeforeAddOrUpdate(newTabProv);
      RepoManager.Tab_ProvRepo.Add(newTabProv, true);
      e.Cancel = true;
      gvTabProv.CancelEdit();
      BindGrid();
    }

    protected void gvTabProv_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
    {
      _log.Info(String.Format("TAB_PROV-Row updating by {0}", PowerWebContext.Current.User.Codice_Utente));
      var currentId = Convert.ToInt32(e.Keys[gvTabProv.KeyFieldName]);
      Tab_Prov currentTabProv = RepoManager.Tab_ProvRepo.Single(u => u.Tab_Prov_Id == currentId);
      PowerWebService.FillEntityProperties(currentTabProv, e.NewValues);
      RepoManager.Tab_ProvRepo.SetEntityBeforeAddOrUpdate(currentTabProv);
      RepoManager.LingueRepo.SaveChanges();
      e.Cancel = true;
      gvTabProv.CancelEdit();
      BindGrid();
    }

    protected void gvTabProv_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
    {
      _log.Info(String.Format("TAB_PROV-Row deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
      var currentId = Convert.ToInt32(e.Keys[gvTabProv.KeyFieldName]);
      Tab_Prov currentTabProv = RepoManager.Tab_ProvRepo.Single(u => u.Tab_Prov_Id == currentId);
      RepoManager.Tab_ProvRepo.Delete(currentTabProv, true);
      e.Cancel = true;
      BindGrid();
    }

    public override void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
    {
    }
    #endregion

    public override void HeaderFilterFillItems(object sender, DevExpress.Web.ASPxGridView.ASPxGridViewHeaderFilterEventArgs e)
    //Gestione Filtri CUSTOM x i Campi DATA (VUOTA perchè NON ci sono Campi Date da Gestire ma va comunque definita vuota se non ce ne sono)
    {
    }

    public ILog Log
    {
      get { return _log; }
    }


  }
}