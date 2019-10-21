using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using DevExpress.Web.ASPxTabControl;
using Domain;
using DevExpress.Web.Data;
using log4net;
using Business;
using System;
using Common;
using DevExpress.Web.ASPxGridView;
using Reports;


namespace PowerWeb.Modules
{
  public partial class Tab_ComuniModule : BaseGridModule, ILogModule
  {
    const String KEYFIELDNAME = "Tab_Comuni_Id";
    private static readonly ILog _log = LogManager.GetLogger(typeof(Tab_ComuniModule));

    public override ASPxGridView GridView
    {
      get
      {
        return gvTabComuni;
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
          var templateDic = EditDictionaryManager.GetEditDictionaryTab_Comuni();
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
      PowerWebService.FillGridLabels(typeof(Tab_Comuni), GridView);
      PowerWebService.FillComboboxes(gvTabComuni);
      BindGrid();
    }

    private void BindGrid()
    {
        // E' utilizzata una lista vuota in caso di mancata presenza record o di mancato populate grid per
        // evitare errori nel pulsante di inserimento
        gvTabComuni.KeyFieldName = KEYFIELDNAME;
        IQueryable<Tab_Comuni> currDataSource = Enumerable.Empty<Tab_Comuni>().AsQueryable();
        var emptyList = Enumerable.Empty<Tab_Comuni>();
        if (IsToPopulateGrid)
        {
            currDataSource = RepoManager.Tab_ComuniRepo.GetAll(true).AsQueryable();
            gvTabComuni.DataSource = currDataSource.Any() ? currDataSource : emptyList;
        }
        else
            gvTabComuni.DataSource = emptyList;
    }

    protected void gvTabComuni_DataBinding(object sender, EventArgs e)
    {
        BindGrid();
    }

    public override Type EntityType
    {
      get { return typeof(Tab_Comuni); }
    }

    #region gvTabComuni : InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-Batch_Update
    protected void gvTabComuni_InitNewRow(object sender, ASPxDataInitNewRowEventArgs e)
    {
      ASPxGridView grid = sender as ASPxGridView;
      if (grid != null)
      {
        Tab_Comuni initTab_Comuni = RepoManager.Tab_ComuniRepo.Init();
        PowerWebService.FillGridProperties(initTab_Comuni, e.NewValues);
        PowerWebService.FillGridClonedProperties(Page, grid, e.NewValues);
      }
    }

    protected void gvTabComuni_RowValidating(object sender, ASPxDataValidationEventArgs e)
    {

      Tab_Comuni newTab_Comuni = new Tab_Comuni();

      if (IsInBatchMode)
      {
          var currentId = Convert.ToInt32(e.Keys[gvTabComuni.KeyFieldName]);
          if (currentId > 0)
          {
              var currentTab_Comuni = GridView.GetRow(e.VisibleIndex);
              PowerWebService.FillValues(currentTab_Comuni, e.NewValues, e.OldValues);
          }
      }

      PowerWebService.FillEntityProperties(newTab_Comuni, e.NewValues);
      PowerWebService.FillEntityKey(newTab_Comuni, e.Keys, KEYFIELDNAME);
      RepoManager.Tab_ComuniRepo.SetEntityBeforeAddOrUpdate(newTab_Comuni);
      PowerWebService.AddValidationErrors(RepoManager.Tab_ComuniRepo.Check(newTab_Comuni, e.IsNewRow), e.Errors, gvTabComuni, typeof(Tab_ComuniModule));
      if (e.HasErrors)
        e.RowError = PowerWebService.GetValidationErrorString(e.Errors);
    }

    protected void gvTabComuni_RowInserting(object sender, ASPxDataInsertingEventArgs e)
    {
      _log.Info(String.Format("TAB_COMUNI-Row Inserting by {0}", PowerWebContext.Current.User.Codice_Utente));
      Tab_Comuni newTab_Comuni = new Tab_Comuni();
      PowerWebService.FillEntityProperties(newTab_Comuni, e.NewValues);
      RepoManager.Tab_ComuniRepo.SetEntityBeforeAddOrUpdate(newTab_Comuni);
      RepoManager.Tab_ComuniRepo.Add(newTab_Comuni, true);
      e.Cancel = true;
      gvTabComuni.CancelEdit();
      BindGrid();
    }

    protected void gvTabComuni_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
    {
      _log.Info(String.Format("TAB_COMUNI-Row Updating by {0}", PowerWebContext.Current.User.Codice_Utente));
      var currentId = Convert.ToInt32(e.Keys[gvTabComuni.KeyFieldName]);
      Tab_Comuni currentTab_Comuni = RepoManager.Tab_ComuniRepo.Single(ta => ta.Tab_Comuni_Id == currentId);
      PowerWebService.FillEntityProperties(currentTab_Comuni, e.NewValues);
      RepoManager.Tab_ComuniRepo.SetEntityBeforeAddOrUpdate(currentTab_Comuni);
      RepoManager.Tab_ComuniRepo.SaveChanges();

      e.Cancel = true;
      gvTabComuni.CancelEdit();
      BindGrid();
    }

    protected void gvTabComuni_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
    {
      _log.Info(String.Format("TAB_COMUNI-Row Deleting by {0}", PowerWebContext.Current.User.Codice_Utente));
      var currentId = Convert.ToInt32(e.Keys[gvTabComuni.KeyFieldName]);
      Tab_Comuni currentTab_Comuni = RepoManager.Tab_ComuniRepo.Single(ta => ta.Tab_Comuni_Id == currentId);
      RepoManager.Tab_ComuniRepo.Delete(currentTab_Comuni, true);
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