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

    public class ExportTimesheetLampo : ExcelToolBox
    {

        #region Constants

        private static readonly ILog _log = LogManager.GetLogger(typeof(ExportTimesheetSimple));

        private int worksheetIndex = 0;
        private int rowIndex = 1;
        private int columnIndex = 1;
        private int minutesLastMonth = 0;

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

            foreach (Col col in collaboratori)
                cartellini.Add(col, TimesheetModuleItem.GenerateCartellino(ExportDate,
                                                    col,
                                                    false,
                                                    false,
                                                    true,
                                                    true,
                                                    parameters.Cartellino_Visualizza_Ore,
                                                    parameters.Cartellino_Visualizza_Motivazioni,
                                                    parameters.Cartellino_Visualizza_Viaggi,
                                                    parameters.Cartellino_Visualizza_Delta,
                                                    parameters.Cartellino_Divisione_Piano_Notturno_Diurno,
                                                    true,
                                                    parameters.Cartellino_Visualizza_Piano));


            cartellini = cartellini.OrderBy(c => c.Key.Codice_Collaboratore).ToDictionary(c => c.Key, d => d.Value);

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

                    WriteColMonteMinuti(col.Key);

                    rowIndex += 2;

                    if (parameters.Cartellino_Usa_Cartellino_Modificabile)
                    {
                        WriteStrTimesheetTitle();

                        WriteTimesheetColHeader();

                        WriteStrTimesheet(col.Value);

                        rowIndex += 2;
                    }

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

            columnIndex = 3;

            days.ForEach(day =>
            {
                WriteHeaderDayCell(day);

                columnIndex++;
            });

            columnIndex = 2;

            WriteHeaderTotalCell();

            columnIndex++;

            WriteHeaderTotalDaysCell();

            columnIndex = 3;

            rowIndex++;
        }

        private void WriteStrTimesheetTitle()
        {
            CellInsertValue(worksheetIndex, 1, rowIndex, "SUDDIVISIONE ORE", ExcelInsertTypeEnum.Content);
            RangeSetFontUnderline(worksheetIndex, 1, rowIndex, 1, rowIndex);

            rowIndex += 2;
        }

        private void WriteColTimesheet(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            foreach (var justification in cartellini["justification"])
            {
                var justificationDec = justification.Justification;

                if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                    justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                justificationDec = justificationDec.ToUpper();

                CellInsertValue(worksheetIndex, 1, rowIndex, justificationDec, ExcelInsertTypeEnum.Content);

                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, endMonth))
                {
                    var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    var timeDuration = TimeSpan.FromHours(baseDuration);
                    string valueToPrint = "";
                    if (baseDuration < 0 && baseDuration > -1)
                    {
                        valueToPrint = "-" + FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                    }
                    else
                    {
                        valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                    }

                    RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                }

                string totalHours = FromTotalMinutesToFormattedType(justification.TotalMinutes);

                RangeSetBorders(worksheetIndex, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, 2, rowIndex, totalHours, ExcelInsertTypeEnum.Content);

                RangeSetBorders(worksheetIndex, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, 3, rowIndex, justification.TotalDays, ExcelInsertTypeEnum.Content);

                if (justificationDec.Equals("DELTA")) {
                    minutesLastMonth = justification.TotalMinutes;
                }

                rowIndex++;
            }
            columnIndex = 1;
        }
        private void WriteStrTimesheet(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            foreach (var justification in cartellini["straordinari"])
            {
                var justificationDesc = justification.Justification;

                if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDesc))
                    justificationDesc = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDesc).Decodifica_Tab;

                justificationDesc = justificationDesc.ToUpper();

                CellInsertValue(worksheetIndex, 1, rowIndex, justificationDesc, ExcelInsertTypeEnum.Content);

                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, endMonth))
                {
                    var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    var timeDuration = TimeSpan.FromHours(baseDuration);

                    string valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);

                    RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                }

                string totalHours = FromTotalMinutesToFormattedType(justification.TotalMinutes);

                RangeSetBorders(worksheetIndex, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, 2, rowIndex, totalHours, ExcelInsertTypeEnum.Content);

                RangeSetBorders(worksheetIndex, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, 3, rowIndex, justification.TotalDays, ExcelInsertTypeEnum.Content);


                rowIndex++;

            }

        }

        private void WriteColMonteMinuti(Col col)
        {
            List<Col_Monte_Minuti> monte = RepoManager.Col_Monte_MinutiRepo.GetAllQueryable(cmr => cmr.Col_Id == col.Col_Id && cmr.Anno_Col_Monte_Minuti == ExportDate.Year).ToList();
            int totale = 0;
            if (monte.Count > 0) {
                switch (ExportDate.Month)
                {
                    case 1:
                        List<Col_Monte_Minuti> monteLast = RepoManager.Col_Monte_MinutiRepo.GetAllQueryable(cmr => cmr.Col_Id == col.Col_Id && cmr.Anno_Col_Monte_Minuti == ExportDate.Year - 1).ToList();
                        totale = monte.First().M12_Col_Monte_Minuti;
                        break;
                    case 2:
                        totale = monte.First().M01_Col_Monte_Minuti;
                        break;
                    case 3:
                        totale = monte.First().M02_Col_Monte_Minuti;
                        break;
                    case 4:
                        totale = monte.First().M03_Col_Monte_Minuti;
                        break;
                    case 5:
                        totale = monte.First().M04_Col_Monte_Minuti;
                        break;
                    case 6:
                        totale = monte.First().M05_Col_Monte_Minuti;
                        break;
                    case 7:
                        totale = monte.First().M06_Col_Monte_Minuti;
                        break;
                    case 8:
                        totale = monte.First().M07_Col_Monte_Minuti;
                        break;
                    case 9:
                        totale = monte.First().M08_Col_Monte_Minuti;
                        break;
                    case 10:
                        totale = monte.First().M09_Col_Monte_Minuti;
                        break;
                    case 11:
                        totale = monte.First().M10_Col_Monte_Minuti;
                        break;
                    case 12:
                        totale = monte.First().M11_Col_Monte_Minuti;
                        break;
                    default:
                        totale = 0;
                        break;
                }
            }
            CellInsertValue(worksheetIndex, 1, rowIndex, "MONTE MINUTI", ExcelInsertTypeEnum.Content);

            CellInsertValue(worksheetIndex, 2, rowIndex, "MESE PRECEDENTE", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            CellInsertValue(worksheetIndex, 3, rowIndex, totale, ExcelInsertTypeEnum.Content);
            int oreLastMonth = minutesLastMonth / 60;
            int minutes = minutesLastMonth % 60;
            minutes = System.Math.Abs(minutes);
            CellInsertValue(worksheetIndex, 4, rowIndex, "MESE CORRENTE", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, 5, rowIndex, 5, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            CellInsertValue(worksheetIndex, 5, rowIndex, ""+oreLastMonth+"."+minutes, ExcelInsertTypeEnum.Content);

            rowIndex++;
        }
        #region Header

        private void WriteHeaderDayCell(DateTime date)
        {
            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, date.Day + 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex, date.Day + 3, rowIndex, "0");
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, date.Day + 3, rowIndex, Color.LightGray, fillStyle);

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);
            }
        }

        private void WriteHeaderTotalCell()
        {

            CellInsertValue(worksheetIndex, 2, rowIndex, "Totale", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, 2, rowIndex, 2, rowIndex, Color.LightGray, fillStyle);

        }

        private void WriteHeaderTotalDaysCell()
        {
            CellInsertValue(worksheetIndex, 3, rowIndex, "Tot. Giorni", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, 3, rowIndex, 3, rowIndex, Color.LightGray, fillStyle);

        }

        #endregion

        #endregion
    }
}
