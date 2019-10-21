using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Business.Repository;
using System.Web;
using System.IO;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    class ExportAlitalia : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
    {

        #region Public Properties

        public ExportRegVCalculationTypeEnum CalculationType { get; set; }

        public int DurationTollerance { get; set; }

        public int EUTollerance { get; set; }

        public string ExcelModelFilePath { get; set; }

        public DateTime ExportPeriod { get; set; }

        public ExportRegVHourTypeEnum HourType { get; set; }

        public ExcelModelSelectionTypeEnum ModelFirstEntity { get; set; }

        public bool UseCalculationType { get; set; }

        public bool UseDurationTollerance { get; set; }

        public bool UseEUTollerance { get; set; }

        /// <summary>
        /// Recupera o imposta un valore ch indica se utilizzare oppure no l'export del confronto ore budget dettagliato
        /// </summary>
        /// <value>
        /// <c>true</c> se si deve utilizzare oppure no l'export dettagliato; altrimenti, <c>false</c>.
        /// </value>
        public bool UseExportDetail { get; set; }

        public bool UseHourType { get; set; }

        #endregion

        #region Public Methods

        public override void LaunchExport(IQueryable<Reg_V> entitiesToExport)
        {
            throw new NotImplementedException();
        }

        public override void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds)
        {
            IQueryable<Reg_V> entitiesToExport = RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Att && selectedCantIds.Any(val => val == regv.Att_Id));

            if (entitiesToExport.Any())
            {

                int rowIndex = 2;

                ExcelWorkbookGenerateNew(ExcelModelFilePath);

                //per ogni cantiere selezioanto
                foreach (var regsByCant in entitiesToExport.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Att && regv.Col_Id.HasValue).GroupBy(regv => regv.Att_Id))
                {
                    //estraggo tutti i cantieri che hanno l'id corrispondente 
                    Cant currentAttIdCant = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == regsByCant.Key);

                    //se il cantiere è valorizzato
                    if (currentAttIdCant != default(Cant))
                    {
                        //viene effettuatto un ragruppamento per sottocantiere
                        foreach (var regsBySubCant in regsByCant.GroupBy(regv => regv.Sotto_Cantiere))
                            //viene effettuato un ragruppamento per data
                            foreach (var regsByDate in regsBySubCant.GroupBy(regv => regv.Data_Reg))
                            {
                                //per ogni data viene estratta la prima regV di quella data
                                Reg_V firstRegV = regsByDate.FirstOrDefault();

                                if (firstRegV != default(Reg_V))
                                {
                                    //viene costruito l'export inserendo prima il cantiere
                                    int originalRowIndex = rowIndex;
                                    CellInsertValue(1, 2, rowIndex, "CANTIERE", ExcelInsertTypeEnum.Content);
                                    CellInsertValue(1, 3, rowIndex, currentAttIdCant.Descrizione_Can, ExcelInsertTypeEnum.Content);
                                    int cantHeaderRowIndex = rowIndex;
                                    rowIndex++;
                                    //poi il sottocantiere
                                    CellInsertValue(1, 2, rowIndex, "SOTTOCANTIERE", ExcelInsertTypeEnum.Content);
                                    CellInsertValue(1, 3, rowIndex, regsBySubCant.Key, ExcelInsertTypeEnum.Content);
                                    int subCantHeaderRowIndex = rowIndex;
                                    rowIndex++;
                                    //poi la data
                                    CellInsertValue(1, 2, rowIndex, "DATA", ExcelInsertTypeEnum.Content);
                                    CellInsertValue(1, 3, rowIndex, regsByDate.Key.Value.ToString("dd/MM/yyyy"), ExcelInsertTypeEnum.Content);
                                    int dateHeaderRowIndex = rowIndex;
                                    rowIndex++;
                                    
                                    ////viene fatta la riga di testata inserendo collaboratore
                                    //CellInsertValue(1, 2, rowIndex, "COLLABORATORE", ExcelInsertTypeEnum.Content);
                                    RangeSetBorders(1, 2, rowIndex, 2, rowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                                    IEnumerable<string> activitiesCode = regsByDate.Select(regv => regv.Cant_Mnemonic.Substring(5)).Distinct();
                                    IEnumerable<string> activitiesDesc = regsByDate.Select(regv => regv.Cant_Desc).Distinct();
                                    var activitesPos = new Dictionary<string, int>();
                                    //l'indice della colonna per l'inserimento della descrizione delle attività
                                    int colIndexDescAct = 3;

                                    //foreach (var actCode in activitiesCode)
                                    //{
                                    //    CellInsertValue(1, colIndex, rowIndex, actCode, ExcelInsertTypeEnum.Content);
                                    //    RangeSetTextHorizontalAlignment(1, colIndex, rowIndex, colIndex, rowIndex, OfficeOpenXml.Style.ExcelHorizontalAlignment.Center);
                                    //    RangeSetBorders(1, colIndex, rowIndex, colIndex, rowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                                    //    activitesPos.Add(actCode, colIndex);
                                    //    colIndex++;
                                    //}

                                    //per ogni descrizione estratta
                                    foreach (var actDesc in activitiesDesc)
                                    {
                                        //viene inserita nella cella la descrizione dell'attività
                                        CellInsertValue(1, colIndexDescAct, rowIndex, actDesc, ExcelInsertTypeEnum.Content);
                                        //allineamento orrizontale del testo
                                        RangeSetTextHorizontalAlignment(1, colIndexDescAct, rowIndex, colIndexDescAct, rowIndex, OfficeOpenXml.Style.ExcelHorizontalAlignment.Center);
                                       //orientamento del testo in verticale
                                        RangeSetTextOrientation(1, colIndexDescAct, rowIndex, colIndexDescAct, rowIndex, 90);
                                        if(actDesc.Length>20)
                                        RangeSetWrapText(1, colIndexDescAct, rowIndex, colIndexDescAct, rowIndex, true);
                                         //viene formattata la cella
                                         RangeSetBorders(1, colIndexDescAct, rowIndex, colIndexDescAct, rowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                                        //viene incertio nel dizionario come valore della chiave la descrizione dell'attività mentre nel valore l'indice della colonna corrispondente
                                        activitesPos.Add(actDesc, colIndexDescAct);
                                        //viene incrementato il valore della collona
                                        colIndexDescAct++;
                                    }
                                    rowIndex++;

                                    //viene fatta la riga di testata inserendo collaboratore
                                    CellInsertValue(1, 2, rowIndex, "COLLABORATORE", ExcelInsertTypeEnum.Content);
                                    RangeSetBorders(1, 2, rowIndex, 2, rowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                                    //inizializzazione della prima riga del codice dell'attività
                                    int colIndexCodeAct = 3;
                                    foreach (var actCode in activitiesCode)
                                    {
                                        CellInsertValue(1, colIndexCodeAct, rowIndex, actCode, ExcelInsertTypeEnum.Content);
                                        RangeSetTextHorizontalAlignment(1, colIndexCodeAct, rowIndex, colIndexCodeAct, rowIndex, OfficeOpenXml.Style.ExcelHorizontalAlignment.Center);
                                        RangeSetBorders(1, colIndexCodeAct, rowIndex, colIndexCodeAct, rowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                                        activitesPos.Add(actCode, colIndexCodeAct);
                                        colIndexCodeAct++;
                                    }

                                    //viene estratto il valore massimo della colonna
                                    int maxColIndex = activitesPos.Select(kvp => kvp.Value).Max();

                                    //se il valore massimo è inferiore a 13 colonne
                                    if (maxColIndex < 13)
                                    {
                                        //per ogni colonna 
                                        for (int i = maxColIndex; i <= 13; i++)
                                        {
                                            //per ogni colonna va ad inserire un allineamento orrizontale del testo partendo dalla colonna 3 fino al numero max di colonne e dalla riga del codice attività
                                            RangeSetTextHorizontalAlignment(1, maxColIndex, rowIndex, maxColIndex, rowIndex, OfficeOpenXml.Style.ExcelHorizontalAlignment.Center);
                                            //per ogni cella del codice attività vengono delineati i bordi di cella
                                            RangeSetBorders(1, maxColIndex, rowIndex, maxColIndex, rowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                                            //per ogni colonna va ad inserire un allineamento orrizontale del testo partendo dalla colonna 3 fino al numero max di colonne e dalla riga della descrizione attività
                                            RangeSetTextHorizontalAlignment(1, maxColIndex, rowIndex-1, maxColIndex, rowIndex-1, OfficeOpenXml.Style.ExcelHorizontalAlignment.Center);
                                            //per ogni cella della descrizione attività vengono delineati i bordi di cella
                                            RangeSetBorders(1, maxColIndex, rowIndex-1, maxColIndex, rowIndex-1, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                                            maxColIndex++;
                                        }
                                    }

                                    RangeSetBorders(1, maxColIndex, rowIndex, maxColIndex, rowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                                    RangeSetBorders(1, maxColIndex, rowIndex-1, maxColIndex, rowIndex-1, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium);

                                    //viene definito lo sfondo delle celle corrispondenti alla descrizione delle attività
                                    RangeSetBackgroundColor(1, 3, rowIndex-1, maxColIndex, rowIndex-1, System.Drawing.Color.FromArgb(240, 230, 140), OfficeOpenXml.Style.ExcelFillStyle.Solid);
                                    //viene impostato lo sfondo delle celle dei codici attività
                                    RangeSetBackgroundColor(1, 2, rowIndex, maxColIndex, rowIndex, System.Drawing.Color.FromArgb(253, 233, 217), OfficeOpenXml.Style.ExcelFillStyle.Solid);

                                    //unisce le celle nel foglio di calcolo
                                    RangeUnion(1, 3, cantHeaderRowIndex, maxColIndex, cantHeaderRowIndex);
                                    RangeUnion(1, 3, subCantHeaderRowIndex, maxColIndex, subCantHeaderRowIndex);
                                    RangeUnion(1, 3, dateHeaderRowIndex, maxColIndex, dateHeaderRowIndex);
                                    RangeSetBorders(1, 2, originalRowIndex, maxColIndex, originalRowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None);
                                    RangeSetBorders(1, 2, dateHeaderRowIndex, maxColIndex, dateHeaderRowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None);
                                    RangeSetBorders(1, 1, originalRowIndex, 1, dateHeaderRowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                                    RangeSetBorders(1, maxColIndex, originalRowIndex, maxColIndex, dateHeaderRowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.None, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                                    RangeSetBackgroundColor(1, 2, originalRowIndex, maxColIndex, dateHeaderRowIndex, System.Drawing.Color.FromArgb(218, 238, 243), OfficeOpenXml.Style.ExcelFillStyle.Solid);

                                    int originalColRowIndex = rowIndex + 1;

                                    #region CALCOLO DEL NUMERO DI ATTIVITà SVOLTE
                                    //TODO: raggruppare per codice e recuperare solo dopo la descrizione
                                    foreach (var regvsByCol in regsByDate.GroupBy(regv => regv.Col_Desc))
                                    {
                                        rowIndex++;
                                        CellInsertValue(1, 2, rowIndex, regvsByCol.Key, ExcelInsertTypeEnum.Content);

                                        foreach (var regvByCodAtt in regvsByCol.GroupBy(regv => regv.Cant_Mnemonic.Substring(5)))
                                        {
                                            Reg_V lastRegV = regvByCodAtt.OrderByDescending(regv => regv.Data_Ora_Fis_E).FirstOrDefault();
                                            string lastRegState = lastRegV.EntrataEU;

                                            int closedNumber = regvByCodAtt.Count(regv => regv.EntrataEU == "U");
                                            int eNumber = regvByCodAtt.Count(regv => regv.EntrataEU == "E");

                                            int column = 0;
                                            if (activitesPos.ContainsKey(regvByCodAtt.Key)) // TODO: mettere il mnemonic in variabile e calcolarlo sempre uguale con metodo
                                                column = activitesPos[regvByCodAtt.Key];

                                            if (column != 0)
                                            {
                                                string valueString = String.Empty;
                                                if (PowerWebContext.Current.User.Cli_Id == null || regsByDate.Key == DateTime.Today)
                                                {
                                                    if (closedNumber == 0)
                                                        valueString = lastRegState;
                                                    else if (lastRegState == "E")
                                                        valueString = String.Format("{0}F + {1}", closedNumber, lastRegState);
                                                    else
                                                        valueString = String.Format("{0}F", closedNumber);
                                                }
                                                else
                                                    valueString = String.Format("{0}F", (eNumber >= closedNumber ? eNumber : closedNumber));


                                                CellInsertValue(1, column, rowIndex, valueString, ExcelInsertTypeEnum.Content);
                                                RangeSetTextHorizontalAlignment(1, column, rowIndex, column, rowIndex, OfficeOpenXml.Style.ExcelHorizontalAlignment.Center);
                                            }
                                        }

                                        RangeSetBorders(1, 2, originalColRowIndex, maxColIndex, rowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                                        RangeSetBorders(1, 2, originalColRowIndex, maxColIndex, originalColRowIndex, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black, OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                                    }
                                    #endregion

                                    rowIndex = rowIndex + 2;
                                }
                            }
                    }
                }

                ColumnsSetAutoWidth(1, 2, 2);

                // esporto quanto generato (in caso di assenza reg_v il file modello) sulla risposta del browser
                ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

                // una volta salvato l'oggetto excel viene cancellato dalla memoria
                ExcelWorkbookDispose();
            }
        }

        #endregion

    }
}
