using Business.BusinessExtension;
using Business.Repository;
using Common;
using Domain;
using log4net;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /*
     *
     *      Attenzione non gestisce i totali settimanali
     * 
     */

    public class ExportSSSFiduciario : ExcelToolBox
    {

        #region Constants

        private static readonly ILog _log = LogManager.GetLogger(typeof(ExportTimesheetSimple));

        private int worksheetIndex = 0;
        private int rowIndex = 1;
        private int columnIndex = 1;

        private OfficeOpenXml.Style.ExcelBorderStyle borderStyle = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        private OfficeOpenXml.Style.ExcelFillStyle fillStyle = OfficeOpenXml.Style.ExcelFillStyle.Solid;

        private Color borderColor = Color.Black;

        private DateTime startMonth;
        private DateTime endMonth;

        private bool _multiPagedExport = Convert.ToBoolean(RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheetMultiPagedExport));

        #endregion

        #region Public Methods

        public override void LaunchExport()
        {
            Param parameters = RepoManager.ParamRepo.ParametersRow;
            Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>> cartellini = new Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>>();

            List<Col> collaboratori = RepoManager.ColRepo.Find(c => SelectedIds.Contains(c.Col_Id), true).ToList();

            startMonth = CommonService.GetFirstMonthDay(ExportDate);
            endMonth = CommonService.GetLastMonthDay(ExportDate);

            foreach (Col col in collaboratori) {
                IEnumerable<int> lis = new List<int>();
                lis = RepoManager.RegRepo.GetRegsIdByDateRangeByColNotBlocked(startMonth, endMonth, col.Col_Id);
                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CollabNoHours) == 0)
                {
                     if (lis.Count() > 0)
                    {
                        cartellini.Add(col, TimesheetModuleItem.GenerateCartellino(ExportDate,
                                                    col,
                                                    true,
                                                    false,
                                                    parameters.Cartellino_Visualizza_Ore,
                                                    parameters.Cartellino_Visualizza_Motivazioni,
                                                    parameters.Cartellino_Visualizza_Viaggi,
                                                    parameters.Cartellino_Visualizza_Delta,
                                                    parameters.Cartellino_Usa_Cartellino_Modificabile,
                                                    parameters.Cartellino_Divisione_Piano_Notturno_Diurno,
                                                    Convert.ToBoolean(parameters.Cartellino_Visualizza_Totali_Settimanali),
                                                    false,
                                                    parameters.Cartellino_Visualizza_Piano));
                    }
                }
                else {
                    cartellini.Add(col, TimesheetModuleItem.GenerateCartellino(ExportDate,
                                                    col,
                                                    true,
                                                    false,
                                                    parameters.Cartellino_Visualizza_Ore,
                                                    parameters.Cartellino_Visualizza_Motivazioni,
                                                    parameters.Cartellino_Visualizza_Viaggi,
                                                    parameters.Cartellino_Visualizza_Delta,
                                                    parameters.Cartellino_Usa_Cartellino_Modificabile,
                                                    parameters.Cartellino_Divisione_Piano_Notturno_Diurno,
                                                    Convert.ToBoolean(parameters.Cartellino_Visualizza_Totali_Settimanali),
                                                    false,
                                                    parameters.Cartellino_Visualizza_Piano));
                }  
            }
                


            cartellini = cartellini.OrderBy(c => c.Key.CognomeNome_Col).ToDictionary(c => c.Key, d => d.Value);

            // per prima cosa si procede all'apertura del modello
            ExcelWorkbookGenerateNew();

            if (!_multiPagedExport)
            {
                //Not multipagedExport
                WorksheetCreateNew();
                worksheetIndex++;

                WriteTimesheetHeader();

                rowIndex += 2;
            }

            // una volta generato il file, se ci sono elementi da processare
            if (cartellini.Any())
            {
                foreach (var col in cartellini)
                {
                    if (_multiPagedExport)
                    {
                        ExcelWorkbook.Workbook.Worksheets.Add(col.Key.CognomeNome_Col);
                        worksheetIndex++;

                        WriteTimesheetHeader();

                        rowIndex += 2;
                    }

                    WriteTimesheetColName(col.Key);

                    WriteTimesheetColHeader();

                    WriteColTimesheet(col.Value);

                    rowIndex += 2;

                    if (_multiPagedExport)
                    {
                        rowIndex = 1;
                        columnIndex = 1;
                    }
                    else
                    {
                        rowIndex += 2;
                    }
                }

                foreach (var worksheet in ExcelWorkbook.Workbook.Worksheets)
                {
                    worksheet.Cells.AutoFitColumns();
                }
            }
        }

        #endregion

        #region Private Methods

        private void WriteTimesheetHeader()
        {
            RangeUnion(worksheetIndex, 1, rowIndex, 34, rowIndex + 4);
            CellInsertValue(worksheetIndex, 1, 1, ExportDate.ToString("MMMM yyyy").ToUpper(), ExcelInsertTypeEnum.Content);
            RangeSetFontBold(worksheetIndex, 1, rowIndex, 34, rowIndex + 4);
            RangeSetTextVerticalAlignment(worksheetIndex, 1, rowIndex, 34, rowIndex + 4, ExcelVerticalAlignment.Center);
            RangeSetTextHorizontalAlignment(worksheetIndex, 1, rowIndex, 34, rowIndex + 4, ExcelHorizontalAlignment.Center);

            rowIndex += 5;
        }

        private void WriteTimesheetTitle()
        {
            CellInsertValue(worksheetIndex, 1, rowIndex, "CLASSIC", ExcelInsertTypeEnum.Content);
            rowIndex += 2;
        }

        private void WriteTimesheetColName(Col col)
        {
            RangeUnion(worksheetIndex, 1, rowIndex, 5, rowIndex);
            RangeSetFontBold(worksheetIndex, 1, rowIndex, 5, rowIndex);
            CellInsertValue(worksheetIndex, 1, rowIndex, col.CognomeNome_Col, ExcelInsertTypeEnum.Content);

            rowIndex++;
        }

        private void WriteTimesheetColHeader()
        {
            List<DateTime> days = CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1)); //Calcolo i giorni per l'header

            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, days.Count + 3, rowIndex);
            
            WriteHeaderTotalCell();

            columnIndex++;
            
            WriteHeaderTotalFest();

            columnIndex++;

            WriteHeaderTotalFer();

            columnIndex++;

            days.ForEach(day =>
            {
                WriteHeaderDayCell(day);

                columnIndex++;
            });
            //WriteHeaderTotalDaysCell();

            //columnIndex++;

            columnIndex = 1;

            rowIndex++;
        }

        private void WriteColTimesheet(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            bool negative = false;
            foreach (var justification in cartellini["justification"])
            {
                var justificationDec = justification.Justification;

                if (justificationDec != "PART" && justificationDec != "FERM" && justificationDec != "OL" && justificationDec != "Ore Piano" && justificationDec != "Totale" && justificationDec != "Eccedenza") { 
                    if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                        justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                        justificationDec = justificationDec.ToUpper();

                        CellInsertValue(worksheetIndex, 1, rowIndex, justificationDec, ExcelInsertTypeEnum.Content);
                        double totaleF = 0.0;
                        double totaleFer = 0.0;
                        foreach (var day in CommonService.GetDatesFromPeriod(startMonth, endMonth))
                        {
                            negative = false;
                            var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                            var timeDuration = TimeSpan.FromHours(baseDuration);
                            if (baseDuration < 0) {
                                negative = true;
                            }

                            string valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                            if (!valueToPrint.Contains("-") && negative == true) {
                                valueToPrint = "-" + valueToPrint;
                            }

                            RangeSetBorders(worksheetIndex, columnIndex + day.Day+3, rowIndex, columnIndex + day.Day+3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, columnIndex + day.Day+3, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                            if (day.DayOfWeek.ToString() == "Sunday")
                            {
                                if (totaleF == 0)
                                {
                                    totaleF = timeDuration.TotalMinutes;
                                }
                                else {
                                    totaleF += timeDuration.TotalMinutes;
                                }
                            }
                            else {
                                if (totaleFer == 0)
                                {
                                    totaleFer = timeDuration.TotalMinutes;
                                }
                                else
                                {
                                    totaleFer += timeDuration.TotalMinutes;
                                }
                            }
                        }
                    negative = false;
                    int TotaleF = (int)totaleF;
                    if (TotaleF < 0) {
                        negative = true;
                    }
                    string totalF = FromTotalMinutesToFormattedType(TotaleF);
                    if (!totalF.Contains("-") && negative == true)
                    {
                        totalF = "-" + totalF;
                    }
                    negative = false;
                    int TotaleFer = (int)totaleFer;
                    if (TotaleFer < 0) 
                    {
                        negative = true;
                    }
                    string totalFer = FromTotalMinutesToFormattedType(TotaleFer);
                    if (!totalFer.Contains("-") && negative == true)
                    {
                        totalFer = "-" + totalFer;
                    }
                    string totalHours = FromTotalMinutesToFormattedType(justification.TotalMinutes);
                    

                    RangeSetBorders(worksheetIndex, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, 2, rowIndex, totalHours, ExcelInsertTypeEnum.Content);

                    RangeSetBorders(worksheetIndex, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, 3, rowIndex, totalF, ExcelInsertTypeEnum.Content);

                    RangeSetBorders(worksheetIndex, 4, rowIndex, 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, 4, rowIndex, totalFer, ExcelInsertTypeEnum.Content);

                    rowIndex++;
                }
            }

        }
        #region Header

        private void WriteHeaderDayCell(DateTime date)
        {
            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, date.Day + 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex, date.Day + 4, rowIndex, "0");
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, date.Day + 4, rowIndex, Color.LightGray, fillStyle);

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);
            }
        }

        private void WriteHeaderTotalCell()
        {

            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, "Totale", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);

        }
        private void WriteHeaderTotalFest()
        {

            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, "Totale Festivi", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);
            RangeSetFontColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);

        }
        private void WriteHeaderTotalFer()
        {

            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, "Totale feriali", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);

        }

        private void WriteHeaderTotalDaysCell()
        {
            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, "Tot. Giorni", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);

        }

        #endregion

        #endregion
    }
}
