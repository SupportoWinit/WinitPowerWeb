using Business.BusinessExtension;
using Business.Repository;
using Common;
using Domain;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OfficeOpenXml.Style;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;
using System.IO;
using static Business.MDBSchema.PowerMDBDataSet;
using Westwind.Utilities.Extensions;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    class ExportCartellinoCantiere : ExcelToolBox
    {
        private int rowIndex = 1;
        private int columnIndex = 1;
        private int worksheetIndex = 0;

        private OfficeOpenXml.Style.ExcelBorderStyle borderStyle = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        private OfficeOpenXml.Style.ExcelFillStyle fillStyle = OfficeOpenXml.Style.ExcelFillStyle.Solid;

        private Color borderColor = Color.Black;

        private DateTime startMonth;
        private DateTime endMonth;

        public override void LaunchExport()
        {
            Param parameters = RepoManager.ParamRepo.ParametersRow;

            Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>> cartellini = new Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>>();

            var collaboratori = RepoManager.ColRepo.Find(c => SelectedIds.Contains(c.Col_Id), true).ToList();

            startMonth = CommonService.GetFirstMonthDay(ExportDate);
            endMonth = CommonService.GetLastMonthDay(ExportDate);
            endMonth = new DateTime(endMonth.Year,endMonth.Month,endMonth.Day,23,59,59);
           

            foreach (Col col in collaboratori)
            {
                IEnumerable<int> lis = new List<int>();
                lis = RepoManager.RegRepo.GetRegsIdByDateRangeByColNotBlocked(startMonth, endMonth, col.Col_Id);
                if (lis.Count() > 0 || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CollabNoHours) == 1)
                {
                    cartellini.Add(col, TimesheetModuleItem.GenerateCartellinoReport(ExportDate,
                                                   col,
                                                   isByOtherEntity: true,
                                                   calculateWorkedHours: parameters.Cartellino_Visualizza_Ore,
                                                   calculateJustifications: false,
                                                   calculateTrips: parameters.Cartellino_Visualizza_Viaggi,
                                                   calculateDelta: parameters.Cartellino_Visualizza_Delta,
                                                   calculateOrdStrTimesheet: false,
                                                   devidePlanByDayNight: parameters.Cartellino_Divisione_Piano_Notturno_Diurno,
                                                   showWeeklyTotal: false,
                                                   insertCorrectionRow: false));
                }
            }


            ExcelWorkbookGenerateNew();

            if (cartellini.Any())
            {
                foreach (var col in cartellini.OrderBy(c => c.Key.Cognome_Col))
                {
                    rowIndex = 1;
                    var worksheet = ExcelWorkbook.Workbook.Worksheets.Add(col.Key.CognomeNome_Col);
                    byte[] companyLogo = RepoManager.ParamRepo.ParametersRow.CompanyLogo;
                    Image image = null;
                    if (companyLogo != null)
                    {
                        image = Image.FromStream(new MemoryStream(companyLogo));
                    }
                    var picture = worksheet.Drawings.AddPicture("Immagine 1", image);
                    picture.SetSize(400, 100);

                    picture.SetPosition(0, 0, 0, 0);

                    worksheetIndex++;

                    WriteTimesheetHeader();

                    WriteColName(col.Key);

                    WriteColHeader();

                    WriteColTimesheet(col.Value);

                    WriteGrandTotal(col.Value);

                    rowIndex += 2;

                }
                ExcelWorkbook.Workbook.FullCalcOnLoad = true;
                foreach (var worksheet in ExcelWorkbook.Workbook.Worksheets)
                {
                    //worksheet.Cells.AutoFitColumns();
                }
                //ExcelWorkbook.Workbook.Worksheets[1].Cells.AutoFitColumns();
            }

        }

        private void WriteColName(Col col)
        {
            RangeSetFontBold(worksheetIndex, 1, rowIndex, 5, rowIndex);
            CellInsertValue(worksheetIndex, 1, rowIndex++, col.CognomeNome_Col, Common.ExcelInsertTypeEnum.Content);
        }

        private void WriteTimesheetHeader()
        {
            RangeUnion(worksheetIndex, 2, rowIndex, 34, rowIndex + 4);
            CellInsertValue(worksheetIndex, 2, 1, ExportDate.ToString("MMMM yyyy").ToUpper(), ExcelInsertTypeEnum.Content);
            RangeSetFontBold(worksheetIndex, 2, rowIndex, 34, rowIndex + 4);
            RangeSetTextVerticalAlignment(worksheetIndex, 2, rowIndex, 34, rowIndex + 4, ExcelVerticalAlignment.Center);
            RangeSetTextHorizontalAlignment(worksheetIndex, 2, rowIndex, 34, rowIndex + 4, ExcelHorizontalAlignment.Center);

            rowIndex += 5;
        }
        private void WriteColHeader()
        {
            var days = Common.CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1));

            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, days.Count + 3, rowIndex);

            days.ForEach(day =>
            {
                string giorno = "";
                switch (day.DayOfWeek) 
                {
                    case DayOfWeek.Monday:
                        giorno = "Lun";
                        break;
                    case DayOfWeek.Tuesday:
                        giorno = "Mar";
                        break;
                    case DayOfWeek.Wednesday:
                        giorno = "Mer";
                        break;
                    case DayOfWeek.Thursday:
                        giorno = "Gio";
                        break;
                    case DayOfWeek.Friday:
                        giorno = "Ven";
                        break;
                    case DayOfWeek.Saturday:
                        giorno = "Sab";
                        break;
                    case DayOfWeek.Sunday:
                        giorno = "Dom";
                        break;
                }
                CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex - 1, day.Day, Common.ExcelInsertTypeEnum.Content);
                RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex - 1, day.Day + 1, rowIndex - 1, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex - 1, day.Day + 1, rowIndex - 1, "0");
                RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex - 1, day.Day + 1, rowIndex - 1, Color.LightGray, fillStyle);

                CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex,giorno, Common.ExcelInsertTypeEnum.Content);
                RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, "0");
                RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, Color.LightGray, fillStyle);
                ColumnsSetWidth(worksheetIndex, columnIndex + 1, day.Day + 1, 6.7);

                if (day.DayOfWeek == DayOfWeek.Sunday)
                    RangeSetFontColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);

                columnIndex++;
            });

            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, "Totale", Common.ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);
            ColumnsSetWidth(worksheetIndex, columnIndex + 1, columnIndex + 1, 8);

            columnIndex++;

            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, "Tot. Giorni", Common.ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);
            ColumnsSetWidth(worksheetIndex, columnIndex + 1, columnIndex + 1,10.46);
            RowsSetHeight(worksheetIndex, rowIndex, rowIndex, 20);

            columnIndex = 1;

            rowIndex++;
        }
        private void WriteColTimesheet(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            foreach (var cartRow in cartellini["justification"].OrderByDescending(c => c.CantMnemonic)/*.GroupBy(c => c.CantMnemonic + " " + c.CantDesc)*/.ToList())
            {
                //foreach (var cartRow in cantCartellino)
                //{
                    var justificationDec = cartRow.Justification;

                    if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                        justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                    justificationDec = justificationDec.ToUpper();

                    if (justificationDec != "TOTALE")
                    {
                        RowsSetHeight(worksheetIndex, rowIndex, rowIndex, 30);
                        ColumnsSetWidth(worksheetIndex,1,1,43);
                        CellInsertValue(worksheetIndex, 1, rowIndex, justificationDec, Common.ExcelInsertTypeEnum.Content);

                        foreach (var day in Common.CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1)))
                        {
                            var dayNumber = day.Day;
                            string valueToPrint = FromTotalMinutesToFormattedTypeVirgola((int)cartRow.DaysHours[dayNumber].Item1);
                            if ((int)cartRow.DaysHours[dayNumber].Item1 > 0 && justificationDec == "MALATTIA")
                            {
                                RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                CellInsertValue(worksheetIndex, day.Day + 1, rowIndex, "M", Common.ExcelInsertTypeEnum.Content);
                            }
                            else if ((int)cartRow.DaysHours[dayNumber].Item1 > 0 && justificationDec == "FERIE")
                            {
                                RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                CellInsertValue(worksheetIndex, day.Day + 1, rowIndex, "F", Common.ExcelInsertTypeEnum.Content);
                            }
                            else if ((int)cartRow.DaysHours[dayNumber].Item1 > 0)
                            {
                                RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                CellInsertValue(worksheetIndex, day.Day + 1, rowIndex, valueToPrint, Common.ExcelInsertTypeEnum.Content);
                            }
                            else 
                            {
                                RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                CellInsertValue(worksheetIndex, day.Day + 1, rowIndex, "-", Common.ExcelInsertTypeEnum.Content);
                            }
                            RangeSetTextHorizontalAlignment(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex,ExcelHorizontalAlignment.Center);
                            RangeSetTextVerticalAlignment(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, ExcelVerticalAlignment.Center);
                        }

                        if (justificationDec == "MALATTIA")
                        {
                            RangeSetBorders(worksheetIndex, cartRow.DaysHours.Count + 2, rowIndex, cartRow.DaysHours.Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, cartRow.DaysHours.Count + 2, rowIndex, "M", Common.ExcelInsertTypeEnum.Content);

                            RangeSetBorders(worksheetIndex, cartRow.DaysHours.Count + 3, rowIndex, cartRow.DaysHours.Count + 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, cartRow.DaysHours.Count + 3, rowIndex, "M", Common.ExcelInsertTypeEnum.Content);
                        }
                        else if (justificationDec == "FERIE")
                        {
                            RangeSetBorders(worksheetIndex, cartRow.DaysHours.Count + 2, rowIndex, cartRow.DaysHours.Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, cartRow.DaysHours.Count + 2, rowIndex, "F", Common.ExcelInsertTypeEnum.Content);

                            RangeSetBorders(worksheetIndex, cartRow.DaysHours.Count + 3, rowIndex, cartRow.DaysHours.Count + 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, cartRow.DaysHours.Count + 3, rowIndex, "F", Common.ExcelInsertTypeEnum.Content);
                        }
                        else
                        {
                            //Viene considerato il range per la somma della riga, dalla colonna 1 alla colonna del numero dei giorni
                            //come riga viene considerata quella attuale
                            string rangeFormula = $"{ColumnIndexToNameConversion(2)}{rowIndex}:" +
                                $"{ColumnIndexToNameConversion(cartRow.DaysHours.Count + 1)}{rowIndex}";

                            //Formula automatica per la somma dei valori della riga
                            string formula = $"=SUM({rangeFormula})";

                            //Conta solamente i numeri della riga, quindi i giorni diversi da "M" o "F" o "---"
                            string contaFormula = $"=COUNT({rangeFormula})";

                            RangeSetBorders(worksheetIndex, cartRow.DaysHours.Count + 2, rowIndex, cartRow.DaysHours.Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, cartRow.DaysHours.Count + 2, rowIndex, formula, Common.ExcelInsertTypeEnum.Formula);

                            RangeSetBorders(worksheetIndex, cartRow.DaysHours.Count + 3, rowIndex, cartRow.DaysHours.Count + 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, cartRow.DaysHours.Count + 3, rowIndex, contaFormula, Common.ExcelInsertTypeEnum.Formula);
                        }
                        RangeSetTextHorizontalAlignment(worksheetIndex, cartRow.DaysHours.Count + 2, rowIndex, cartRow.DaysHours.Count + 2, rowIndex, ExcelHorizontalAlignment.Center);
                        RangeSetTextVerticalAlignment(worksheetIndex, cartRow.DaysHours.Count + 2, rowIndex, cartRow.DaysHours.Count + 2, rowIndex, ExcelVerticalAlignment.Center);

                        RangeSetTextHorizontalAlignment(worksheetIndex, cartRow.DaysHours.Count + 3, rowIndex, cartRow.DaysHours.Count + 3, rowIndex, ExcelHorizontalAlignment.Center);
                        RangeSetTextVerticalAlignment(worksheetIndex, cartRow.DaysHours.Count + 3, rowIndex, cartRow.DaysHours.Count + 3, rowIndex, ExcelVerticalAlignment.Center);
                    }
                    else
                    {
                        rowIndex--;
                    }
                    //rowIndex++;
                //}
                rowIndex++;
            }

        }
        private void WriteGrandTotal(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {

            RangeSetFontBold(worksheetIndex, 1, rowIndex, 1, rowIndex);
            CellInsertValue(worksheetIndex, 1, rowIndex, "Totale", Common.ExcelInsertTypeEnum.Content);

            var cartTotale = cartellini["justification"].Where(cart => cart.Justification != "Ferie").ToList();
            cartTotale = cartTotale.Where(cart => cart.Justification != "Malattia").ToList();

            ILookup<int, Tuple<double, TimeSpan?, TimeSpan?>> lookupCartellini = cartTotale.Where(cart => cart.Justification != "Totale").SelectMany(cart => cart.DaysHours).ToLookup(c => c.Key, x => x.Value); //Crea una lookup ( uguale ad un dictionary <int,list<...>> che quindi permette du raggruppare valori secondo la stessa chiave)

            //Variabili per la formula e il range da usare
            string formula;
            string rangeFormula;

            foreach (var dayHourList in lookupCartellini)
            {
                //Come rang viene scelta la colonna attuale dalla riga iniziale dei valori alla riga attuale -1
                rangeFormula = $"{ColumnIndexToNameConversion(dayHourList.Key + 1)}{rowIndex - cartTotale.Count()}:" +
                $"{ColumnIndexToNameConversion(dayHourList.Key + 1)}{rowIndex - 1}";

                //Formula automatica da inserire in Excel
                formula = $"=SUM({rangeFormula})";

                RangeSetBorders(worksheetIndex, dayHourList.Key + 1, rowIndex, dayHourList.Key + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, dayHourList.Key + 1, rowIndex, formula, Common.ExcelInsertTypeEnum.Formula);

                RangeSetTextHorizontalAlignment(worksheetIndex, dayHourList.Key + 1, rowIndex, dayHourList.Key + 1, rowIndex, ExcelHorizontalAlignment.Center);
                RangeSetTextVerticalAlignment(worksheetIndex, dayHourList.Key + 1, rowIndex, dayHourList.Key + 1, rowIndex, ExcelVerticalAlignment.Center);

            }

            
            //Il range viene considerato nella colonna attuale e dala riga attuale -numero cantieri fino alla riga attuale -1
            rangeFormula = $"{ColumnIndexToNameConversion(lookupCartellini.Count + 2)}{rowIndex - cartTotale.Count()} :" +
                $"{ColumnIndexToNameConversion(lookupCartellini.Count + 2)}{rowIndex - 1}";

            string finalFormula = $"=SUM({rangeFormula})";


            RowsSetHeight(worksheetIndex, rowIndex, rowIndex, 30);
            RangeSetBorders(worksheetIndex, lookupCartellini.Count + 2, rowIndex, lookupCartellini.Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            CellInsertValue(worksheetIndex, lookupCartellini.Count + 2, rowIndex, finalFormula, Common.ExcelInsertTypeEnum.Formula);
            RangeSetTextHorizontalAlignment(worksheetIndex, lookupCartellini.Count + 2, rowIndex, lookupCartellini.Count + 2, rowIndex, ExcelHorizontalAlignment.Center);
            RangeSetTextVerticalAlignment(worksheetIndex, lookupCartellini.Count + 2, rowIndex, lookupCartellini.Count + 2, rowIndex, ExcelVerticalAlignment.Center);
            rowIndex++;
        }
    }
}
