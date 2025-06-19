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
                foreach (var col in cartellini)
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

                foreach (var worksheet in ExcelWorkbook.Workbook.Worksheets)
                {
                    worksheet.Cells.AutoFitColumns();
                }
                //ExcelWorkbook.Workbook.Worksheets[1].Cells.AutoFitColumns();
            }

        }

        private void WriteColName(Col col)
        {
            RangeUnion(worksheetIndex, 1, rowIndex, 5, rowIndex);
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
                CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, day.Day, Common.ExcelInsertTypeEnum.Content);
                RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, "0");
                RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, Color.LightGray, fillStyle);

                if (day.DayOfWeek == DayOfWeek.Sunday)
                    RangeSetFontColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);

                columnIndex++;
            });

            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, "Totale", Common.ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);

            columnIndex++;

            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, "Tot. Giorni", Common.ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);

            columnIndex = 1;

            rowIndex++;
        }
        private void WriteColTimesheet(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {

            foreach (var cantCartellino in cartellini["justification"].OrderBy(c => c.CantDesc).GroupBy(c => c.CantMnemonic + " " + c.CantDesc).ToList())
            {
                //RangeSetFontBold(1, 1, rowIndex, 1, rowIndex);
                //CellInsertValue(1, 1, rowIndex++, cantCartellino.Key, Common.ExcelInsertTypeEnum.Content);

                foreach (var cartRow in cantCartellino)
                {
                    var justificationDec = cartRow.Justification;

                    if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                        justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                    justificationDec = justificationDec.ToUpper();

                    if (justificationDec != "TOTALE")
                    {
                        CellInsertValue(worksheetIndex, 1, rowIndex, justificationDec, Common.ExcelInsertTypeEnum.Content);

                        foreach (var day in Common.CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1)))
                        {
                            var dayNumber = day.Day;
                            string valueToPrint = FromTotalMinutesToFormattedType((int)cartRow.DaysHours[dayNumber].Item1);
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
                            else
                            {
                                RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                CellInsertValue(worksheetIndex, day.Day + 1, rowIndex, valueToPrint, Common.ExcelInsertTypeEnum.Content);
                            }
                        }

                        string totalHours = FromTotalMinutesToFormattedType(cartRow.TotalMinutes);
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
                            RangeSetBorders(worksheetIndex, cartRow.DaysHours.Count + 2, rowIndex, cartRow.DaysHours.Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, cartRow.DaysHours.Count + 2, rowIndex, totalHours, Common.ExcelInsertTypeEnum.Content);

                            RangeSetBorders(worksheetIndex, cartRow.DaysHours.Count + 3, rowIndex, cartRow.DaysHours.Count + 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, cartRow.DaysHours.Count + 3, rowIndex, cartRow.TotalDays, Common.ExcelInsertTypeEnum.Content);
                        }
                    }
                    else
                    {
                        rowIndex--;
                    }
                    //rowIndex++;
                }
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

            foreach (var dayHourList in lookupCartellini)
            {

                double sommaGiornaliera = dayHourList.Sum(y => y.Item1);
                RangeSetBorders(worksheetIndex, dayHourList.Key + 1, rowIndex, dayHourList.Key + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);

                string valueToPrint = FromTotalMinutesToFormattedType((int)sommaGiornaliera);
                CellInsertValue(worksheetIndex, dayHourList.Key + 1, rowIndex, valueToPrint, Common.ExcelInsertTypeEnum.Content);

            }

            int totalHours = cartTotale.Where(cart => cart.Justification != "Totale").Select(cart => cart.TotalMinutes).Sum();

            string formattedTotal = FromTotalMinutesToFormattedType((int)totalHours);
            RangeSetBorders(worksheetIndex, lookupCartellini.Count + 2, rowIndex, lookupCartellini.Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            CellInsertValue(worksheetIndex, lookupCartellini.Count + 2, rowIndex, formattedTotal, Common.ExcelInsertTypeEnum.Content);

            rowIndex++;

        }



    }
}
