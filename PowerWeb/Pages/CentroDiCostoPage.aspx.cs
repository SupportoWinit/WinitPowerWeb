using Business.ImportModules.CdcImportModule;
using Business.Repository;
using Common;
using DevExpress.Web.ASPxEditors;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.ASPxUploadControl;
using DevExpress.Web.Data;
using Domain;
using System;
using System.IO;
using System.Linq;

namespace PowerWeb.Pages
{
    public partial class CentroDiCostoPage : BasePage
    {
        public ASPxGridView GridView => gvCentroDiCosto;


        protected void Page_Init(object sender, EventArgs e)
        {
            var masterPage = this.Master as GridMasterPage;

            masterPage.HideAllMasterPageFeatures();

            PowerWebService.FillGridLabels(typeof(CentroDiCosto), gvCentroDiCosto);
            PowerWebService.FillComboboxes(gvCentroDiCosto);
            BindGrid();
        }

        protected void gvCentroDiCosto_DataBinding(object sender, EventArgs e)
        {
            BindGrid();
        }

        private void BindGrid()
        {
            gvCentroDiCosto.KeyFieldName = "CentroDiCosto_Id";
            IQueryable<CentroDiCosto> currDataSource = Enumerable.Empty<CentroDiCosto>().AsQueryable();
            var emptyList = Enumerable.Empty<CentroDiCosto>();

            currDataSource = RepoManager.CentroDiCostoRepo.GetAll(true).AsQueryable();
            gvCentroDiCosto.DataSource = currDataSource.Any() ? currDataSource : emptyList;
        }

        private void BindDetailGrid(ASPxGridView gvDetails)
        {
            int centroDiCostoId = Convert.ToInt32(gvDetails.GetMasterRowKeyValue());
            gvDetails.KeyFieldName = "Cant_CentroDiCosto_Id";
            gvDetails.DataSource = RepoManager.CentroDiCostoRepo.CantCentroDiCostoDbSet
                                                                .Where(c => c.CentroDiCosto_Id == centroDiCostoId)
                                                                .Select(x => new
                                                                {
                                                                    Cant_CentroDiCosto_Id = x.Cant_CentroDiCosto_Id,
                                                                    Cant_Id = x.Cant.Cant_Id,
                                                                    Codice_Cantiere = x.Cant.Codice_Cantiere,
                                                                    Descrizione_Can = x.Cant.Descrizione_Can
                                                                }).ToList();
        }

        protected void gvCentroDiCosto_Init(object sender, EventArgs e)
        {
            BindGrid();
        }

        protected void gvCentroDiCosto_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            var grid = (ASPxGridView)sender;
            var centroDiCostoToInsert = new CentroDiCosto();

            PowerWebService.FillEntityProperties(centroDiCostoToInsert, e.NewValues);

            RepoManager.CentroDiCostoRepo.Add(centroDiCostoToInsert);

            RepoManager.CentroDiCostoRepo.Context.SaveChanges();

            e.Cancel = true;
            grid.CancelEdit();
        }

        protected void gvCentroDiCosto_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            var grid = (ASPxGridView)sender;

            var currentId = Convert.ToInt32(e.Keys[nameof(CentroDiCosto.CentroDiCosto_Id)]);
            var centroDiCostoToEdit = RepoManager.CentroDiCostoRepo.Single(u => u.CentroDiCosto_Id == currentId);

            PowerWebService.FillEntityProperties(centroDiCostoToEdit, e.NewValues);

            RepoManager.CentroDiCostoRepo.Context.SaveChanges();

            e.Cancel = true;
            grid.CancelEdit();
        }

        protected void gvCentroDiCosto_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            var grid = (ASPxGridView)sender;

            var currentId = Convert.ToInt32(e.Keys[nameof(CentroDiCosto.CentroDiCosto_Id)]);
            var centroDiCostoToRemove = RepoManager.CentroDiCostoRepo.SingleOrDefault(c => c.CentroDiCosto_Id == currentId);

            RepoManager.CentroDiCostoRepo.Delete(centroDiCostoToRemove);

            RepoManager.CentroDiCostoRepo.Context.SaveChanges();

            e.Cancel = true;
            grid.CancelEdit();
        }

        #region gvFru_Cant_Detail : Init-InitRow-RowValidating-RowInserting-RowUpdating-RowDeleting-BeforePerformDataSelect-DetailRowExpandedChanged
        protected void gvCentroDiCosto_Detail_Init(object sender, EventArgs e)
        {

        }

        protected void gvCentroDiCosto_Detail_RowInserting(object sender, ASPxDataInsertingEventArgs e)
        {
            var grid = (ASPxGridView)sender;

            var cantCentroDiCostoToInsert = new Cant_CentroDiCosto();

            var masterRowKey = (int)grid.GetMasterRowKeyValue();

            PowerWebService.FillEntityProperties(cantCentroDiCostoToInsert, e.NewValues);

            cantCentroDiCostoToInsert.CentroDiCosto_Id = masterRowKey;

            RepoManager.CentroDiCostoRepo.CantCentroDiCostoDbSet.Add(cantCentroDiCostoToInsert);

            RepoManager.CentroDiCostoRepo.Context.SaveChanges();

            e.Cancel = true;
            grid.CancelEdit();
        }

        protected void gvCentroDiCosto_Detail_RowUpdating(object sender, ASPxDataUpdatingEventArgs e)
        {
            var grid = (ASPxGridView)sender;

            var currentId = Convert.ToInt32(e.Keys[nameof(Cant_CentroDiCosto.Cant_CentroDiCosto_Id)]);

            var cantCentroDiCostoToEdit = RepoManager.CentroDiCostoRepo
                                                 .CantCentroDiCostoDbSet
                                                 .Single(u => u.Cant_CentroDiCosto_Id == currentId);

            PowerWebService.FillEntityProperties(cantCentroDiCostoToEdit, e.NewValues);


            RepoManager.CentroDiCostoRepo.Context.SaveChanges();

            e.Cancel = true;
            grid.CancelEdit();
        }

        protected void gvCentroDiCosto_Detail_RowDeleting(object sender, ASPxDataDeletingEventArgs e)
        {
            var grid = (ASPxGridView)sender;

            var currentId = Convert.ToInt32(e.Keys[nameof(Cant_CentroDiCosto.Cant_CentroDiCosto_Id)]);
            var centroDiCostoToRemove = RepoManager.CentroDiCostoRepo
                                                   .CantCentroDiCostoDbSet
                                                   .SingleOrDefault(c => c.Cant_CentroDiCosto_Id == currentId);

            RepoManager.CentroDiCostoRepo.CantCentroDiCostoDbSet.Remove(centroDiCostoToRemove);

            RepoManager.CentroDiCostoRepo.Context.SaveChanges();

            e.Cancel = true;
            grid.CancelEdit();
        }

        protected void gvCentroDiCosto_Detail_OnItemRequestedByValue(object sender, ListEditItemRequestedByValueEventArgs e)
        {
            if (e.Value == null) return;

            var comboBox = (ASPxComboBox)sender;

            var currentId = (int)e.Value;

            var dataSource = RepoManager.CantRepo.DbSet.Where(cant => cant.Cant_Id == currentId).ToList();

            comboBox.DataSource = dataSource;
            comboBox.DataBind();
        }

        protected void gvCentroDiCosto_Detail_OnItemsRequestedByFilterCondition(object sender, ListEditItemsRequestedByFilterConditionEventArgs e)
        {
            var comboBox = (ASPxComboBox)sender;


            var query = RepoManager.CantRepo.DbSet
                                            .AsNoTracking()
                                            .AsQueryable();

            if (!string.IsNullOrEmpty(e.Filter))
            {
                query = query.Where(cant => cant.Codice_Cantiere.StartsWith(e.Filter)
                                         || cant.Descrizione_Can.StartsWith(e.Filter));
            }

            comboBox.DataSource = query.OrderBy(cant => cant.Codice_Cantiere)
                                       .ThenBy(cant => cant.Descrizione_Can)
                                       .ToList();
            comboBox.DataBind();

        }

        protected void gvCentroDiCosto_Detail_CellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            e.Editor.ReadOnly = false;
        }

        protected void gvCentroDiCosto_Detail_BeforePerformDataSelect(object sender, EventArgs e)
        {
            ASPxGridView detailGrid = (ASPxGridView)sender;

            PowerWebService.FillGridLabels(typeof(Cant_CentroDiCosto), detailGrid);
            PowerWebService.FillComboboxes(detailGrid);
            BindDetailGrid(detailGrid);
        }

        protected void upldImport_FileUploadComplete(object sender, FileUploadCompleteEventArgs e)
        // salva il file di import uploadato nella cartella di destinazione del server
        {
            var importFile = Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path + String.Format("CentroDiCostoFile{0}{1}", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"), ".csv"));

            FileInfo fileInfo = new FileInfo(importFile);
            if (!fileInfo.Exists)
            {
                #region Controllo ed eventuale conversione in csv del formato del file se è xls o xlsx o scrittura del file in caso di formato csv in entrata

                // se sto importando un file excel (cioè l'estensione non è .csv)
                if (Path.GetExtension(e.UploadedFile.FileName).ToUpper() != ".CSV")
                {
                    // salvo il file arrivatomi come parametro in un temporaneo
                    string tmpFileName = Server.MapPath(String.Format("{0}{1}{2}", Common.Properties.Settings.Default.Files_Input_Path, Path.GetFileNameWithoutExtension(Path.GetTempFileName()), Path.GetExtension(e.UploadedFile.FileName)));
                    e.UploadedFile.SaveAs(tmpFileName);

                    // conversione del file excel nel csv di destinazione
                    CommonService.ConvertExcelFileIntoCsv(tmpFileName, importFile);

                    // al termine dell'operazione viene cancellato l'eventuale file temporaneo rimasto appeso
                    if (File.Exists(tmpFileName))
                        File.Delete(tmpFileName);
                }
                else
                {
                    e.UploadedFile.SaveAs(importFile);
                }

                #endregion

                var lines = File.ReadAllLines(importFile);

                var import = new CentroDiCostoImport(lines);

                var importErrors = import.Import();

            }
        }

        protected void gvCentroDiCosto_DetailRowExpandedChanged(object sender, ASPxGridViewDetailRowEventArgs e)
        {
            if (!e.Expanded)
                GridView.DataBind();
        }
        #endregion

    }
}