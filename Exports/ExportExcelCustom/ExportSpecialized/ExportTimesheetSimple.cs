using Business.BusinessExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Business;
using System.Drawing;
using Business.Repository;
using System.IO;
using log4net;
using Newtonsoft.Json.Linq;
using Domain;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /*
     *
     *      Attenzione non gestisce i totali settimanali
     * 
     */

    public class ExportTimesheetSimple : ExcelToolBox
    {

        #region Constants

        private static readonly ILog _log = LogManager.GetLogger(typeof(ExportTimesheetSimple));

        private int rowIndex = 6;
        private int columnIndex = 1;

        private OfficeOpenXml.Style.ExcelBorderStyle borderStyle = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        private OfficeOpenXml.Style.ExcelFillStyle fillStyle = OfficeOpenXml.Style.ExcelFillStyle.Solid;

        private Color borderColor = Color.Black;

        private DateTime startMonth;
        private DateTime endMonth;

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
                                                    true,
                                                    false,
                                                    parameters.Cartellino_Visualizza_Ore,
                                                    parameters.Cartellino_Visualizza_Motivazioni,
                                                    parameters.Cartellino_Visualizza_Viaggi,
                                                    parameters.Cartellino_Visualizza_Delta,
                                                    false,
                                                    parameters.Cartellino_Divisione_Piano_Notturno_Diurno,
                                                    Convert.ToBoolean(parameters.Cartellino_Visualizza_Totali_Settimanali),
                                                    false,
                                                    parameters.Cartellino_Visualizza_Piano));



            cartellini = cartellini.OrderBy(c => c.Key.CognomeNome_Col).ToDictionary(c => c.Key, d => d.Value);

            // per prima cosa si procede all'apertura del modello
            ExcelWorkbookGenerateNew(ModelFilePath);

            // una volta generato il file, se ci sono elementi da processare
            if (cartellini.Any())
            {

                // si scrive la testata dell'export
                CellInsertValue(1, 1, 1, ExportDate.ToString("MMMM yyyy"), ExcelInsertTypeEnum.Content);

                foreach (var col in cartellini)
                {
                    WriteColName(col.Key);

                    WriteColHeader();

                    WriteColTimesheet(col.Value);

                    rowIndex += 2;
                }
            }
        }

        #endregion

        #region Private Methods

        private void WriteColName(Col col)
        {
            RangeUnion(1, 1, rowIndex, 5, rowIndex);
            RangeSetFontBold(1, 1, rowIndex, 5, rowIndex);
            CellInsertValue(1, 1, rowIndex, col.CognomeNome_Col, ExcelInsertTypeEnum.Content);

            rowIndex++;
        }

        private void WriteColHeader()
        {
            List<DateTime> days = CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1)); //Calcolo i giorni per l'header

            RangeSetFontBold(1, columnIndex + 1, rowIndex, days.Count + 3, rowIndex);

            days.ForEach(day =>
            {
                WriteHeaderDayCell(day);

                columnIndex++;
            });

            WriteHeaderTotalCell();

            columnIndex++;

            WriteHeaderTotalDaysCell();

            columnIndex = 1;

            rowIndex++;
        }

        private void WriteColTimesheet(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {

            foreach (var justification in cartellini["justification"])
            {
                var justificationDec = justification.Justification;

                if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                    justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                justificationDec = justificationDec.ToUpper();

                CellInsertValue(1, 1, rowIndex, justificationDec, ExcelInsertTypeEnum.Content);

                foreach (var day in CommonService.GetDatesFromPeriod(startMonth,endMonth))
                {
                    var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    var timeDuration = TimeSpan.FromHours(baseDuration);

                    string valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);

                    RangeSetBorders(1, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(1, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                }

                string totalHours = FromTotalMinutesToFormattedType(justification.TotalMinutes);

                RangeSetBorders(1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count+2, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count+2, rowIndex, totalHours, ExcelInsertTypeEnum.Content);

                RangeSetBorders(1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count+3, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count+3, rowIndex, justification.TotalDays, ExcelInsertTypeEnum.Content);


                rowIndex++;

            }

        }

        #region Header

        private void WriteHeaderDayCell(DateTime date)
        {
            CellInsertValue(1, columnIndex + 1, rowIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, columnIndex + 1, rowIndex, date.Day + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(1, columnIndex + 1, rowIndex, date.Day + 1, rowIndex, "0");
            RangeSetBackgroundColor(1, columnIndex + 1, rowIndex, date.Day + 1, rowIndex, Color.LightGray, fillStyle);

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(1, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);
            }
        }

        private void WriteHeaderTotalCell()
        {

            CellInsertValue(1, columnIndex + 1, rowIndex, "Totale", ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(1, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);

        }

        private void WriteHeaderTotalDaysCell()
        {

            CellInsertValue(1, columnIndex + 1, rowIndex, "Tot. Giorni", ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(1, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);

        }

        #endregion

        #endregion
    }
}
