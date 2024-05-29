using Business.BusinessExtension;
using Business.Repository;
using Common;
using Domain;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    class ExportTimesheetCant : ExcelToolBox
    {
        private int rowIndex = 5;
        private int columnIndex = 1;

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

            foreach (Col col in collaboratori) {
                IEnumerable<int> lis = new List<int>();
                lis = RepoManager.RegRepo.GetRegsIdByDateRangeByColNotBlocked(startMonth, endMonth, col.Col_Id);
                if (lis.Count() > 0 || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CollabNoHours) == 1)
                {
                    cartellini.Add(col, TimesheetModuleItem.GenerateCartellino(ExportDate,
                                                   col,
                                                   isByOtherEntity: true,
                                                   calculateWorkedHours: parameters.Cartellino_Visualizza_Ore,
                                                   calculateJustifications: parameters.Cartellino_Visualizza_Motivazioni,
                                                   calculateTrips: parameters.Cartellino_Visualizza_Viaggi,
                                                   calculateDelta: parameters.Cartellino_Visualizza_Delta,
                                                   calculateOrdStrTimesheet: false,
                                                   devidePlanByDayNight: parameters.Cartellino_Divisione_Piano_Notturno_Diurno,
                                                   showWeeklyTotal: false,
                                                   insertCorrectionRow: false));
                }
                }
               

            ExcelWorkbookGenerateNew(ModelFilePath);

            if (cartellini.Any())
            {
                CellInsertValue(1, 1, 1, ExportDate.ToString("MMMM yyyy"), Common.ExcelInsertTypeEnum.Content);
                
                foreach (var col in cartellini)
                {
                    WriteColName(col.Key);

                    WriteColHeader();

                    WriteColTimesheet(col.Value);

                    WriteGrandTotal(col.Value);

                    rowIndex += 2;

                }

                ExcelWorkbook.Workbook.Worksheets[1].Cells.AutoFitColumns();

            }

        }

        private void WriteColName(Col col)
        {
            RangeUnion(1, 1, rowIndex, 5, rowIndex);
            RangeSetFontBold(1, 1, rowIndex, 5, rowIndex);
            CellInsertValue(1, 1, rowIndex++, col.CognomeNome_Col, Common.ExcelInsertTypeEnum.Content);
        }
        private void WriteColHeader()
        {
            var days = Common.CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1));

            RangeSetFontBold(1, columnIndex + 1, rowIndex, days.Count + 3, rowIndex);

            days.ForEach(day =>
            {
                CellInsertValue(1, columnIndex + 1, rowIndex, day.Day, Common.ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, columnIndex + 1, rowIndex, day.Day+1, rowIndex,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle);
                RangeSetValueFormat(1, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, "0");
                RangeSetBackgroundColor(1, columnIndex + 1, rowIndex, day.Day + 1, rowIndex, Color.LightGray,fillStyle);

                if (day.DayOfWeek == DayOfWeek.Sunday)
                    RangeSetFontColor(1, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);

                columnIndex++;
            });

            CellInsertValue(1, columnIndex + 1, rowIndex, "Totale", Common.ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle);
            RangeSetBackgroundColor(1, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);

            columnIndex++;

            CellInsertValue(1, columnIndex + 1, rowIndex, "Tot. Giorni", Common.ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle);
            RangeSetBackgroundColor(1, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);

            columnIndex = 1;

            rowIndex++;
        }
        private void WriteColTimesheet(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {

            foreach (var cantCartellino in cartellini["justification"].OrderBy(c => c.CantDesc).GroupBy(c => c.CantMnemonic +" " + c.CantDesc).ToList())
            {
                RangeSetFontBold(1, 1, rowIndex, 1, rowIndex);
                CellInsertValue(1, 1, rowIndex++, cantCartellino.Key, Common.ExcelInsertTypeEnum.Content);

                foreach (var cartRow in cantCartellino)
                {
                    var justificationDec = cartRow.Justification;

                    if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                        justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                    justificationDec = justificationDec.ToUpper();

                    CellInsertValue(1, 1, rowIndex, justificationDec, Common.ExcelInsertTypeEnum.Content);

                    foreach (var day in Common.CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1)))
                    {
                        var dayNumber = day.Day;
                        string valueToPrint = FromTotalMinutesToFormattedType((int)cartRow.DaysHours[dayNumber].Item1);

                        RangeSetBorders(1, columnIndex + 1, rowIndex, day.Day + 1, rowIndex,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle);
                        CellInsertValue(1, day.Day + 1, rowIndex, valueToPrint, Common.ExcelInsertTypeEnum.Content);
                    }

                    string totalHours = FromTotalMinutesToFormattedType(cartRow.TotalMinutes);

                    RangeSetBorders(1, cartRow.DaysHours.Count + 2, rowIndex, cartRow.DaysHours.Count + 2, rowIndex,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle);
                    CellInsertValue(1, cartRow.DaysHours.Count + 2, rowIndex, totalHours, Common.ExcelInsertTypeEnum.Content);

                    RangeSetBorders(1, cartRow.DaysHours.Count + 3, rowIndex, cartRow.DaysHours.Count + 3, rowIndex,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle);
                    CellInsertValue(1, cartRow.DaysHours.Count + 3, rowIndex, cartRow.TotalDays, Common.ExcelInsertTypeEnum.Content);


                    rowIndex++;
                }
                rowIndex++;
            }

        }
        private void WriteGrandTotal(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {

            RangeSetFontBold(1, 1, rowIndex, 1, rowIndex);
            CellInsertValue(1, 1, rowIndex, "Totale", Common.ExcelInsertTypeEnum.Content);

            ILookup<int, Tuple<double, TimeSpan?, TimeSpan?>> lookupCartellini = cartellini["justification"].SelectMany(cart => cart.DaysHours).ToLookup(c => c.Key, x => x.Value); //Crea una lookup ( uguale ad un dictionary <int,list<...>> che quindi permette du raggruppare valori secondo la stessa chiave)

            foreach (var dayHourList in lookupCartellini)
            {

                double sommaGiornaliera = dayHourList.Sum(y => y.Item1);
                RangeSetBorders(1, dayHourList.Key + 1, rowIndex, dayHourList.Key + 1, rowIndex,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle);

                string valueToPrint = FromTotalMinutesToFormattedType((int)sommaGiornaliera);
                CellInsertValue(1, dayHourList.Key + 1, rowIndex, valueToPrint, Common.ExcelInsertTypeEnum.Content);

            }

            int totalHours = cartellini["justification"].Select(cart => cart.TotalMinutes).Sum();

            string formattedTotal = FromTotalMinutesToFormattedType((int)totalHours);
            RangeSetBorders(1, lookupCartellini.Count + 2, rowIndex, lookupCartellini.Count + 2, rowIndex,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle,borderColor, borderStyle);
            CellInsertValue(1, lookupCartellini.Count + 2, rowIndex, formattedTotal, Common.ExcelInsertTypeEnum.Content);
            
            rowIndex++;

        }

        

    }
}
