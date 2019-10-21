using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Windows.Forms.VisualStyles;
using Business.Repository;
using Domain.Extensions;
using OfficeOpenXml;
using System.Reflection;
using System.Globalization;
using Common;
using Domain;
using OfficeOpenXml.Style;
using Westwind.Utilities;
using Business;
using Business.BusinessExtension;
using Ionic.Zip;

namespace Business.ExportExcelEngine
{
    public class ExportExcelEngine
    {
        private static int _rowCounter = 0;

        private static Dictionary<String, PropertyInfo> _propertyInfoDict;

        private static ExcelWorksheet _currentWorksheet;

        private static ExcelPackage _currentPackage;

        /*
         * Il motore di export è un tool ottimo per organizzare un excel e permette qualsivoglia organizzazione del foglio.
         * Viene organizzata secondo lo schema di un albero e vengono effettuate operazioni per ogni nodo di quest'ultimo.
         * Per questo l'organizzazione del foglio dipende QUASI TOTALMENTE  dall'organizzazione della struttura dati e dalla gerarchia dei figli.
         * L'albero si scorre ricorsivamente tramite una in/post-visita della struttura ed è molto importante non modificare le funzioni fornite dal motore,
         * inquanto quest'ultime gia permettono qualsiasi elaborazione (al massimo solo la funzione GetExternalTableValue );
         * 
         * Deve essere definita la propria classe di export all'interno di Exports nella qualle viene definito un template contente le istruzioni da eseguire per ogni livello dell'albero.
         * Si necessita anche di una pattern .xlsx necessaria per i riferimenti.
         */

        /// <summary>
        /// Funzione di lancio dell'export.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="specialized">Tipo di export selezionato dall'utente.</param>
        /// <param name="items">Gli oggetti da esportare.</param>
        /// <param name="model">La pattern necessaria per i riferimenti(cartella di deploy powerWeb/exports).</param>
        /// <param name="visibleFields">The visible fields.</param>
        /// <param name="exportToFileSystem">if set to <c>true</c> [export to file system].</param>
        public static void Export<T>(IExportExcelSpecialized<T> specialized, List<T> items, Tab_Excel_Model model, out string path, IEnumerable<Tuple<string, string, string>> visibleFields = null, bool exportToFileSystem = false, bool compress = false) where T : class
        {
            path = null;

            _propertyInfoDict = new Dictionary<String, PropertyInfo>();

            _currentWorksheet = null;

            //Contatore della riga corrente nel nuovo file excel
            _rowCounter = 0;

            //reppresenta i campi da visualizzare in griglia
            specialized.VisibleFields = visibleFields ?? new Collection<Tuple<string, string, string>>();

            //viene estratto il nome del modello utilizzato
            specialized.ModelName = model.Nome_Risorsa;

            //viene estratto l'albero di elementi dell'export(correttamente organizzato si spera)
            var rootNode = specialized.GetRootTreeNode(items);

            //vengono recuperati i templates definiti per l'export selezionato
            var templates = specialized.GetTreeNodeTemplates();

            if (model != null && templates != null)
            {
                //viene controllato se è presente il modello
                FileInfo modelFI = new FileInfo(HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.ExcelModelsPath + model.ModelFilePath));

                //viene preparato il PackageExcel ed inizializzato
                ExcelPackage modelEP = new ExcelPackage(modelFI);
                _currentPackage = new ExcelPackage();

                //Se esiste il modello inizio a scorre l'albero
                if (modelFI.Exists)
                    ParseTree<T>(rootNode, templates, modelEP);

                if (_currentWorksheet != null)
                {
                    #region AutoFit Columns

                    if (specialized.IsAutoFitColumns)
                        _currentWorksheet.Cells.AutoFitColumns();

                    #endregion


                    #region Merge Columns

                    var cellsToMergeList = specialized.GetCellsToMerge();
                    foreach (var cellsToMerge in cellsToMergeList)
                        _currentWorksheet.Cells[cellsToMerge].Merge = true;

                    #endregion


                    #region Sheet Footer Settings

                    _currentWorksheet.HeaderFooter.OddFooter.LeftAlignedText = string.Format("Data {0} ora {1}", ExcelHeaderFooter.CurrentDate, ExcelHeaderFooter.CurrentTime);
                    _currentWorksheet.HeaderFooter.OddFooter.RightAlignedText = string.Format("Pag. {0} di {1}", ExcelHeaderFooter.PageNumber, ExcelHeaderFooter.NumberOfPages);
                    _currentWorksheet.HeaderFooter.EvenFooter.LeftAlignedText = string.Format("Data {0} ora {1}", ExcelHeaderFooter.CurrentDate, ExcelHeaderFooter.CurrentTime);
                    _currentWorksheet.HeaderFooter.EvenFooter.RightAlignedText = string.Format("Pag. {0} di {1}", ExcelHeaderFooter.PageNumber, ExcelHeaderFooter.NumberOfPages);

                    #endregion


                    #region Repeat Rows

                    if (!string.IsNullOrEmpty(specialized.RepeatRowsAddress))
                        _currentWorksheet.PrinterSettings.RepeatRows = _currentWorksheet.Cells[specialized.RepeatRowsAddress];

                    #endregion

                }

                // viene ritornata la response solamente se non devo salvare su filesystem;
                // in caso contario scrivo un file su file system
                if (!exportToFileSystem)
                {
                    var ms = new MemoryStream();

                    _currentPackage.SaveAs(ms);

                    BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = true;

                    ExportToResponse(ms, HttpContext.Current.Response, BusinessService.GetLocalizedString(model.ModelFilePath).ToLower(), false, compress);
                }
                else
                {
                    string cognomeCol = items.First() is ActivityItem ? (items.First() as ActivityItem).ColCognome : String.Empty;
                    string nomeCol = items.First() is ActivityItem ? (items.First() as ActivityItem).ColNome : String.Empty;
                    ExportToFileSystem(model, cognomeCol, nomeCol);
                    path = String.Format("{0}{1}{2}", @"\", Common.Properties.Settings.Default.Files_Output_Path, _currentPackage.File.Name);
                }
            }

        }

        /// <summary>
        /// Esporta su file system l'export prodotto dal modello passato come parametro.
        /// </summary>
        /// <param name="model">Il modello utilizzato per la produzione dell'export.</param>
        /// <param name="colCognome">Il cognome del collaboratore da eventualmente riportare nel nome de file.</param>
        /// <param name="colNome">Il nome del collaboratore da eventualmente riportare nel nome del file.</param>
        private static void ExportToFileSystem(Tab_Excel_Model model, string colCognome = "", string colNome = "")
        {
            // se la cartella di output non esiste allora viene creata
            string outputDirectory = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), Common.Properties.Settings.Default.Files_Output_Path);
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            // costrzione del nome file di destinazione con il nome calcolato con cognome e nome col se passati come parametro
            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportExcelEnum);
            string resultFileNameExtension = String.Empty;
            resultFileNameExtension = "xlsx";

            string resultFileName = String.Empty;
            if (colCognome == String.Empty || colNome == String.Empty)
            {
                resultFileName = Path.Combine(outputDirectory, String.Format("{0}.{1}",
                   String.Format("{0}-{1}_{2}_{3}_{4}.{5}.{6}.{7}",
                       BusinessService.GetLocalizedString(model.Nome_Risorsa),
                       DateTime.Now.Year.ToString("0000"), DateTime.Now.Month.ToString("00"),
                       DateTime.Now.Day.ToString("00"), DateTime.Now.Hour.ToString("00"),
                       DateTime.Now.Minute.ToString("00"),
                       DateTime.Now.Second.ToString("0000"),
                       DateTime.Now.Millisecond),
                   resultFileNameExtension));
            }
            else
            {
                resultFileName = Path.Combine(outputDirectory, String.Format("{0}.{1}",
                   String.Format("{0}-{1}_{2}_{3}_{4}_{5}.{6}.{7}.{8}",
                       BusinessService.GetLocalizedString(model.Nome_Risorsa),
                       colCognome,
                       colNome,
                       DateTime.Now.Year.ToString("0000"), DateTime.Now.Month.ToString("00"),
                       DateTime.Now.Day.ToString("00"), DateTime.Now.Hour.ToString("00"),
                       DateTime.Now.Minute.ToString("00"),
                       DateTime.Now.Second.ToString("0000")),
                   resultFileNameExtension));
            }

            // salvataggio del file a destinazione
            FileInfo outputFileInfo = new FileInfo(resultFileName);
            _currentPackage.SaveAs(outputFileInfo);
        }

        public static void ExportToResponse(MemoryStream stream, HttpResponse response, string fileName, bool inline, bool compress)
        {
            if (compress)
            {
                try
                {
                    response.AddHeader("Content-Disposition", "attachment; filename=" + fileName.Split('.')[0] + ".zip");
                    response.ContentType = "application/zip";
                    response.Cache.SetCacheability(HttpCacheability.NoCache);
                    //Percorso file excel
                    string textFileNameTemplate = HttpContext.Current.Server.MapPath(@"\" + fileName);
                    //Creazione file 
                    File.Create(textFileNameTemplate).Dispose();
                    using (ZipFile zip = new ZipFile())
                    {
                        string textFileName = fileName;
                        //Scrittura nel file
                        File.WriteAllBytes(textFileNameTemplate, stream.ToArray());

                        zip.AddFile(textFileNameTemplate, @"\");
                        zip.Save(response.OutputStream);
                        response.Flush();
                        response.End();
                    }
                }
                catch (Exception ex)
                {

                }
                finally
                {
                    File.Delete(HttpContext.Current.Server.MapPath(@"\" + fileName));
                }

            }
            else
            {
                try
                {
                    // calcolo del nome file con estensione (aggiungendo quella prevista dalla personalizzazione)
                    int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExportExcelEnum);
                    string fileExtension = "xlsx";
                    string downloadFileName = fileName;

                    response.Clear();

                    response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    response.AddHeader("Accept-Header", stream.Length.ToString(CultureInfo.InvariantCulture));
                    response.AddHeader("Content-Disposition", String.Format("{0}; filename={1}", (inline ? "Inline" : "Attachment"), downloadFileName));
                    response.Cache.SetCacheability(HttpCacheability.NoCache);
                    response.AddHeader("Content-Length", stream.Length.ToString(CultureInfo.InvariantCulture));
                    response.BinaryWrite(stream.ToArray());
                    response.Flush();
                    response.End();

                }
                catch (Exception err)
                {
                    Console.WriteLine(err.ToString());
                }
                finally
                {
                    stream.Close();
                    stream.Dispose();
                }
            }

        }


        /// <summary>
        /// Funzione per la risoluzione del valore del campo(mapping).
        /// </summary>
        /// <param name="mapping">Il mapping corrente.</param>
        /// <param name="node">Il nodo dell'albero contente le informazioni da elaborare.</param>
        /// <returns></returns>
        private static object EvaluateMappingValue(ExportExcelTreeNodeMappingBase mapping, ExportExcelTreeNode node)
        {
            Object result = null;

            ExportExcelTreeNodeStaticMapping currentStaticMapping = mapping as ExportExcelTreeNodeStaticMapping;
            if (currentStaticMapping != null)
                result = currentStaticMapping.Value;
            else
            {
                ExportExcelTreeNodeMapping currentMapping = mapping as ExportExcelTreeNodeMapping;
                if (currentMapping != null)
                {
                    PropertyInfo currentPropertyInfo;


                    if (_propertyInfoDict.ContainsKey(currentMapping.PropertyName))
                        currentPropertyInfo = _propertyInfoDict[currentMapping.PropertyName];

                    else
                    {
                        currentPropertyInfo = node.Item.GetType().GetProperty(currentMapping.PropertyName);
                        _propertyInfoDict.Add(currentMapping.PropertyName, currentPropertyInfo);
                    }
                    //se nel mio oggetto item non ho la proprietà entro in questo pezzo
                    if (currentPropertyInfo == null)
                    {
                        var currentObject = node.Item as Expando;
                        if (currentObject != null)
                            result = currentObject[currentMapping.PropertyName];
                        else
                        {
                            //nel caso non abbia l'oggetto corrispondente richiamo il metodo che mi va cercare la proprietà nella tabella corrispondente
                            result = GetExternalTableValue(currentMapping.PropertyName, node.Item);

                        }
                    }
                    else
                        result = currentPropertyInfo.GetValue(node.Item, null);
                }
            }

            return result;
        }


        /// <summary>
        /// Se una proprietà di un campo è stata definita senza passare da valori provenienti dal db sarà necessario definire il caso all'interno di questa funzione.
        /// Ritorna il valore della particolare proprietà
        /// </summary>
        /// <param name="propertyName">Nome della proprietà non trovata nel database</param>
        /// <param name="item">Oggetto contenente el informazioni.</param>
        /// <returns></returns>
        private static object GetExternalTableValue(string propertyName, object item)
        {
            object result = null;

            //se stò lavorando con delle Reg_V allora vado a ricercare la proprietà e ritorno l'oggetto nelle tabelle corrispondenti
            if (item is Reg_V)
            {
                //per il momento viene fatta in modo statico cioè ricercando le proprietà non presenti nelle Reg_V per poi andarle a ricercare nelle tabelle corrispondenti
                if (propertyName == "Codice_Collaboratore" || propertyName == "Nome_Col" || propertyName == "Cognome_Col")
                {

                    var col_Id = (int?)CommonService.GetPropertyValue(item, "Col_Id");
                    if (col_Id != null)
                    {
                        var currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == col_Id);
                        if (currentCol != null)
                        {
                            //ottengo il valore riferito alla proprietà nella tabella corrispondente togliendo gli spazi
                            result = ((string)CommonService.GetPropertyValue(currentCol, propertyName)).Trim();
                        }
                    }
                }
                else if (propertyName == "Codice_Cantiere" || propertyName == "Descrizione_Can" || propertyName == "Tipologia_Can")
                {
                    var cant_Id = (int?)CommonService.GetPropertyValue(item, "Cant_Id");
                    if (cant_Id != null)
                    {
                        var currentcant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == cant_Id);
                        if (currentcant != null)
                        {
                            //ottengo il valore riferito alla proprietà nella tabella corrispondente togliendo gli spazi
                            result = ((string)CommonService.GetPropertyValue(currentcant, propertyName)).Trim();
                        }
                    }
                }
                else if(propertyName == "Codice_Fil" || propertyName == "Descrizione_Fil")
                {
                    int? filId = ((Reg_V)item).Fil_Id;

                    if(filId != null)
                    {
                        Fil fil = RepoManager.FilRepo.First(f => f.Fil_Id == filId);
                        result = ((string)CommonService.GetPropertyValue(fil, propertyName)).Trim();
                    }
                }
                //Proprietà custom ExportRegV_Paused 
                else if (propertyName == "Area" || propertyName == "Pausa" || propertyName == "Ore Totali" || propertyName == "Totale Assoluto" || propertyName == "Tipo Motivazione" || propertyName == "Durata Registr.")
                {
                    result = "";
                    switch (propertyName)
                    {
                        case "Area":
                            int? cant_Id = (int?)CommonService.GetPropertyValue(item, "Cant_Id");
                            if (cant_Id != null)
                            {
                                var currentcant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == cant_Id);
                                if (currentcant != null)
                                {
                                    //ottengo il valore riferito alla proprietà nella tabella corrispondente togliendo gli spazi
                                    result = ((string)CommonService.GetPropertyValue(currentcant, "Codice_Cantiere")).Trim();
                                }
                            }
                            break;
                        case "Pausa":

                            break;
                        case "Ore Totali":

                            break;
                        case "Tipo Motivazione":
                            try
                            {
                                Reg_V tmp = (Reg_V)item;
                                Int32? mot_id = (Int32?)tmp.Motivazione_Reg_Id;
                                result = (mot_id != null) ? RepoManager.Tab_DecodRepo.FirstOrDefault(x => x.Tab_Decod_Id == mot_id).Decodifica_Tab : "";
                            }
                            catch (Exception ex)
                            {
                                result = "";
                            }
                            break;
                        case "Durata Registr.":
                            int durata = (((Reg_V)item).Durata_Fis != null) ? (int)((Reg_V)item).Durata_Fis : -1;
                            if (durata != -1)
                            {
                                result = CommonService.GetHHMMStringFormMinutes(durata);
                            }
                            break;
                    }
                }
            }
            else
            {
                switch (propertyName)
                {
                    case "SumRow":
                        result = "=SOMMA(A" + _rowCounter + ":" + ")";
                        break;
                }
            }
            return result;
        }

        /// <summary>
        /// Si occupa di risolvere il contenuto delle colonne in base ai vari casi.
        /// Anche qui si posso definire dei tipi custom
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mapping">The mapping.</param>
        /// <param name="node">The node.</param>
        /// <param name="rowIndex">Index of the row.</param>
        /// <param name="sheet">The sheet.</param>
        private static void EvaluateMapping<T>(ExportExcelTreeNodeMappingBase mapping, ExportExcelTreeNode node, int rowIndex, ExcelWorksheet sheet) where T : class
        {
            if (mapping.ExcelReference == ExportExcelReferenceEnum.Sheet)
            {
                String workSheetName = EvaluateMappingValue(mapping, node) as String;
                if (!String.IsNullOrEmpty(workSheetName))
                    _currentWorksheet = _currentPackage.Workbook.Worksheets.Add(workSheetName);
            }

            /*
             * Inserimento di una cella statica.
            */
            if (mapping.ExcelReference == ExportExcelReferenceEnum.StaticCell)
            {
                var currentValue = EvaluateMappingValue(mapping, node);

                if (mapping.Formatter != null)
                    currentValue = mapping.Formatter.Format(currentValue, sheet.Cells[mapping.Reference], node);

                if (!String.IsNullOrEmpty(mapping.ExcelNumberFormat))
                    sheet.Cells[mapping.Reference].Style.Numberformat.Format = mapping.ExcelNumberFormat;

                sheet.Cells[mapping.Reference].Value = currentValue;
            }

            /*
             *Inserimento di un campo in una colonna e relativa definizione del suo contenuto 
             */
            if (mapping.ExcelReference == ExportExcelReferenceEnum.Column)
            {
                //Posizione nel foglio corrente
                String currentAddress = String.Format("{0}{1}", mapping.Reference, _rowCounter);

                string currentProperty = String.Empty;
                try
                {
                    currentProperty = (mapping as ExportExcelTreeNodeMapping).PropertyName;
                }
                catch
                {
                    currentProperty = String.Empty;
                }

                object currentValue;
                if (!currentProperty.StartsWith("TABDESC#"))
                    currentValue = EvaluateMappingValue(mapping, node);
                else
                {
                    var splittedProperty = currentProperty.Split('#');
                    var tabDecodName = splittedProperty[1];
                    var keyFieldName = splittedProperty[2];

                    var keyFieldPropertyInfo = node.Item.GetType().GetProperty(keyFieldName);

                    var chiaveTab = keyFieldPropertyInfo.GetValue(node.Item, null);

                    Tab_Decod tabDecodRecord = null;

                    string chiaveTabStr = chiaveTab == null ? String.Empty : chiaveTab.ToString();

                    tabDecodRecord = chiaveTab != null
                        ? RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == tabDecodName && td.Chiave_Tab == chiaveTabStr)
                        : null;

                    currentValue = tabDecodRecord != null ? BusinessService.GetLocalizedString(tabDecodRecord.Decodifica_Tab) : null;

                    currentValue = currentValue ?? chiaveTab;
                }

                if (mapping.Formatter != null)
                    currentValue = mapping.Formatter.Format(currentValue, sheet.Cells[currentAddress], node);

                if (!String.IsNullOrEmpty(mapping.ExcelNumberFormat))
                    sheet.Cells[currentAddress].Style.Numberformat.Format = mapping.ExcelNumberFormat;

                //Viene assegnato il valore al campo selezionato
                sheet.Cells[currentAddress].Value = currentValue;
            }

            /*
             * Campo di tipo somma che ritornerà una formula per la somma di più campi
             */

            if (mapping.ExcelReference == ExportExcelReferenceEnum.Sum)
            {
                ExportExcelTreeNodeMapping currentMapping = mapping as ExportExcelTreeNodeMapping;

                String currentAddress = String.Format("{0}{1}", mapping.Reference, _rowCounter);

                StringBuilder sb = new StringBuilder();

                //Vengono sommati in un unico campo i valori della colonna selezionata composta dai figli del nodo corrente
                if (node.Children.Count > 0)
                {
                    var startRowIndex = node.Children.First().StartRow;
                    var endRowIndex = node.Children.Last().StartRow;
                    sb.Append("=SUM(");

                    sb.Append(mapping.Reference).Append(startRowIndex).Append(":").Append(mapping.Reference).Append(rowIndex - 1);
                    sb.Append(")");

                    sheet.Cells[currentAddress].Formula = sb.ToString();

                    if (mapping.Formatter != null)
                        sheet.Cells[currentAddress].Value = mapping.Formatter.Format(sheet.Cells[currentAddress].Value, sheet.Cells[currentAddress], node);

                    if (!String.IsNullOrEmpty(mapping.ExcelNumberFormat))
                        sheet.Cells[currentAddress].Style.Numberformat.Format = mapping.ExcelNumberFormat;
                }

            }



            /*
             * Campo di tipo somma che ritornerà una formula per la somma di più campi in linea
             */

            if (mapping.ExcelReference == ExportExcelReferenceEnum.Diff)
            {
                ExportExcelTreeNodeMapping currentMapping = mapping as ExportExcelTreeNodeMapping;

                String currentAddress = String.Format("{0}{1}", mapping.Reference, _rowCounter);

                StringBuilder sb = new StringBuilder();
                if (currentMapping.PropertyName == "Totale Assoluto")
                {
                    char a = char.Parse(mapping.Reference);
                    var firstIndex = char.ConvertFromUtf32(a - 1);
                    var lastIndex = char.ConvertFromUtf32(a - 3); ;
                    sb.Append("=");

                    sb.Append(firstIndex).Append(node.StartRow).Append("-").Append(lastIndex).Append(node.StartRow);


                    sheet.Cells[currentAddress].Formula = sb.ToString();

                    if (mapping.Formatter != null)
                        sheet.Cells[currentAddress].Value = mapping.Formatter.Format(sheet.Cells[currentAddress].Value, sheet.Cells[currentAddress], node);

                    if (!String.IsNullOrEmpty(mapping.ExcelNumberFormat))
                        sheet.Cells[currentAddress].Style.Numberformat.Format = mapping.ExcelNumberFormat;
                }
            }

            /*
             *Formula relativa al cartellino 
             */

            if (mapping.ExcelReference == ExportExcelReferenceEnum.TimeSheetFormula)
            {
                String currentAddress = String.Format("{0}{1}", mapping.Reference, _rowCounter);

                StringBuilder sb = new StringBuilder();
                if (node.Children.Count > 0)
                {
                    var startRowIndex = node.Children.First().StartRow;
                    var endRowIndex = node.Children.Last().StartRow;
                    sb.Append("=").Append(mapping.Reference).Append(startRowIndex - 1).Append("-SUM(");

                    sb.Append(mapping.Reference).Append(startRowIndex).Append(":").Append(mapping.Reference).Append(rowIndex - 1);
                    sb.Append(")");

                    sheet.Cells[currentAddress].Formula = sb.ToString();

                    if (mapping.Formatter != null)
                        sheet.Cells[currentAddress].Value = mapping.Formatter.Format(sheet.Cells[currentAddress].Value, sheet.Cells[currentAddress], node);

                    if (!String.IsNullOrEmpty(mapping.ExcelNumberFormat))
                        sheet.Cells[currentAddress].Style.Numberformat.Format = mapping.ExcelNumberFormat;
                }
            }
        }

        /// <summary>
        /// Si occupa di risolvere il template selezionato nel nodo corrente.
        /// Vi sono dei template gia definiti ma è possibile aggiungerne altri custom.
        /// Attraverso il contatore delle righe esegue l'operazione contenuta nel template e la riposta sul foglio excel corrente
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node">The node.</param>
        /// <param name="model">The model.</param>
        /// <param name="template">The template.</param>
        private static void EvaluateTemplate<T>(ExportExcelTreeNode node, ExcelPackage model, ExportExcelTreeNodeTemplate template) where T : class
        {
            if (template != null)
            {
                if (template.ExcelAction == ExportExcelActionEnum.CreateSheet)
                {
                    _rowCounter = 0;

                    if (node.StartRow == -1)
                        node.StartRow = _rowCounter;

                    foreach (ExportExcelTreeNodeMappingBase mapping in template.Mappings)
                        EvaluateMapping<T>(mapping, node, _rowCounter, null);
                }

                if (template.ExcelAction == ExportExcelActionEnum.CreateHeader)
                {

                    var copiedRow = model.Workbook.Worksheets.First().Cells[template.Range];
                    var resultRow = _currentWorksheet.Cells[template.Range];
                    copiedRow.Copy(resultRow);


                    foreach (ExportExcelTreeNodeMappingBase mapping in template.Mappings)
                        EvaluateMapping<T>(mapping, node, _rowCounter, _currentWorksheet);
                }

                //creazione delle varie righe con i dati dell'export
                if (template.ExcelAction == ExportExcelActionEnum.CreateRow)
                {
                    if (_rowCounter == 0)
                        _rowCounter = Int32.Parse(template.Range.Split(':')[0]);

                    if (node.StartRow == -1)
                        node.StartRow = _rowCounter;
                    else
                        _rowCounter = node.StartRow;

                    var copiedRow = model.Workbook.Worksheets.First().Cells[template.Range];

                    String currentRange = String.Format("{0}:{0}", _rowCounter);

                    ExcelRange resultRow = _currentWorksheet.Cells[currentRange];
                    copiedRow.Copy(resultRow);

                    foreach (ExcelRangeBase item in copiedRow)
                    {
                        resultRow[_rowCounter, item.Start.Column].Style.Numberformat.Format = item.Style.Numberformat.Format;
                    }

                    foreach (ExportExcelTreeNodeMappingBase mapping in template.Mappings)
                        EvaluateMapping<T>(mapping, node, _rowCounter, _currentWorksheet);

                    _rowCounter++;
                }
                if (template.ExcelAction == ExportExcelActionEnum.CreateBlankRow)
                {
                    if (_rowCounter == 0)
                        _rowCounter = Int32.Parse(template.Range.Split(':')[0]);

                    if (node.StartRow == -1)
                        node.StartRow = _rowCounter;


                    var copiedRow = model.Workbook.Worksheets.First().Cells[template.Range];

                    String currentRange = String.Format("{0}:{0}", _rowCounter);

                    ExcelRange resultRow = _currentWorksheet.Cells[currentRange];
                    copiedRow.Copy(resultRow);

                    foreach (ExcelRangeBase item in copiedRow)
                    {
                        resultRow[_rowCounter, item.Start.Column].Style.Numberformat.Format = item.Style.Numberformat.Format;
                    }

                    _rowCounter++;
                }
            }
        }


        /// <summary>
        /// Funzione ricorsiva per la visita dell'albero.
        /// Divite i templates da eseguire nel nodo corrente in due vettori differenti:
        ///     currentLevelPreTemplates viene eseguito in pre-visita prima di scendere il qualsiasi dei figli.
        ///     
        ///     currentLevelPostTemplates viene eseguito in post-visita quando la ricorsione inizia a risalire l'albero (ossia quando viene trovato un nodo null).
        ///     é comoda per far eseguire un operazione alla fine di un gruppo di items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node">Nodo corrente.</param>
        /// <param name="templates">Templates.</param>
        /// <param name="model">Il modello di riferimento.</param>
        private static void ParseTree<T>(ExportExcelTreeNode node, List<ExportExcelTreeNodeTemplate> templates, ExcelPackage model) where T : class
        {
            if (node != null)
            {
                var currentLevelPreTemplates = templates.Where(tpl => tpl.NodeLevel == node.Level && !tpl.IsPost).ToList();
                var currentLevelPostTemplates = templates.Where(tpl => tpl.NodeLevel == node.Level && tpl.IsPost).ToList();

                foreach (ExportExcelTreeNodeTemplate template in currentLevelPreTemplates)
                    EvaluateTemplate<T>(node, model, template);

                double childrenCount = node.Children.Count();

                for (double i = 0; i < childrenCount; i++)
                {
                    ExportExcelTreeNode child = node.Children.ElementAt((int)i);


                    ParseTree<T>(child, templates, model);
                }

                foreach (ExportExcelTreeNodeTemplate template in currentLevelPostTemplates)
                    EvaluateTemplate<T>(node, model, template);
            }
        }
    }
}
