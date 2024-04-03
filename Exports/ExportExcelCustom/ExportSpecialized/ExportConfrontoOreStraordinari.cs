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

    public class ExportConfrontoOreStraordinari : ExcelToolBox
    {

        #region Constants

        private static readonly ILog _log = LogManager.GetLogger(typeof(ExportTimesheetSimple));

        private int worksheetIndex = 0;
        private int rowIndex = 1;
        private int columnIndex = 1;
        private int giorno = 2;


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

            List<Col> collaboratori = RepoManager.ColRepo.Find(c => SelectedIds.Contains(c.Col_Id), true).Where(c => c.DisAbilitazione_Col == false && c.Tab_Orari_Tipo != null).OrderBy(c => c.Cognome_Col).ToList();

            startMonth = CommonService.GetFirstMonthDay(ExportDate);
            endMonth = CommonService.GetLastMonthDay(ExportDate);

            foreach (Col col in collaboratori)
            {
                IEnumerable<int> lis = new List<int>();
                lis = RepoManager.RegRepo.GetRegsIdByDateRangeByColNotBlocked(startMonth, endMonth, col.Col_Id);
                if (lis.Count() > 0 || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CollabNoHours) == 1)
                {
                    cartellini.Add(col, TimesheetModuleItem.GenerateCartellino(ExportDate,
                                                    col,
                                                    true,
                                                    false,
                                                    true,
                                                    true,
                                                    parameters.Cartellino_Visualizza_Ore,
                                                    parameters.Cartellino_Visualizza_Motivazioni,
                                                    parameters.Cartellino_Visualizza_Viaggi,
                                                    parameters.Cartellino_Visualizza_Delta,
                                                    parameters.Cartellino_Divisione_Piano_Notturno_Diurno,
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

                //rowIndex += 2;
            }


            // una volta generato il file, se ci sono elementi da processare
            if (cartellini.Any())
            {
                //CellInsertValue(worksheetIndex, 1, 1, , ExcelInsertTypeEnum.Content);
                foreach (var col in cartellini)
                {

                    if (_multiPagedExport)
                    {
                        ExcelWorkbook.Workbook.Worksheets.Add(col.Key.CognomeNome_Col);
                        worksheetIndex++;

                        WriteTimesheetHeader();

                        //rowIndex += 2;
                    }
                    String mese = "";
                    switch (ExportDate.Month) {
                        case 1:
                            mese = "Gennaio";
                            break;
                         case 2:
                            mese = "Febbraio";
                            break;
                        case 3:
                            mese = "Marzo";
                            break;
                        case 4:
                            mese = "Aprile";
                            break;
                        case 5:
                            mese = "Maggio";
                            break;
                        case 6:
                            mese = "Giugno";
                            break;
                        case 7:
                            mese = "Luglio";
                            break;
                        case 8:
                            mese = "Agosto";
                            break;
                        case 9:
                            mese = "Settembre";
                            break;
                        case 10:
                            mese = "Ottobre";
                            break;
                        case 11:
                            mese = "Novembre";
                            break;
                        case 12:
                            mese = "Dicembre";
                            break;
                    }
                    CellInsertValue(worksheetIndex, 1, 1, col.Key.CognomeNome_Col + " " + mese + " " + ExportDate.Year, ExcelInsertTypeEnum.Content);
                    RangeUnion(worksheetIndex, 1, 1, 5, 1);
                    RangeSetBorders(worksheetIndex, 1, 1, 4, 1, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);

                    //WriteTimesheetColName(col.Key);

                    WriteTimesheetColHeader();

                    WriteColTimesheetOrd(col.Value,col.Key);

                    rowIndex = 3;

                    WriteColTimesheetStr(col.Value);

                    //rowIndex = 3;

                    WriteTotaleHourColOrd(col.Value);

                    rowIndex++;

                    WriteTotaleHourColStr(col.Value);

                    //rowIndex += 2;
                    //
                    //if (_multiPagedExport)
                    //{
                    //    rowIndex = 1;
                    //    columnIndex = 1;
                    //}
                    //else
                    //{
                    //    rowIndex += 2;
                    //}
                }

                foreach (var worksheet in ExcelWorkbook.Workbook.Worksheets)
                {
                    //worksheet.Cells.AutoFitColumns();
                }
            }
        }

        #endregion

        #region Private Methods

        private void WriteTimesheetHeader()
        {   //
            //RangeUnion(worksheetIndex, 1, rowIndex, 34, rowIndex + 4);
            //CellInsertValue(worksheetIndex, 1, 1, ExportDate.ToString("MMMM yyyy").ToUpper(), ExcelInsertTypeEnum.Content);
            //RangeSetFontBold(worksheetIndex, 1, rowIndex, 34, rowIndex + 4);
            //RangeSetTextVerticalAlignment(worksheetIndex, 1, rowIndex, 34, rowIndex + 4, ExcelVerticalAlignment.Center);
            //RangeSetTextHorizontalAlignment(worksheetIndex, 1, rowIndex, 34, rowIndex + 4, ExcelHorizontalAlignment.Center);
            //
            //rowIndex += 5;
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

            days.ForEach(day =>
            {
                WriteHeaderDayCell(day);

                WriteHeaderDayCellDay(day);

                columnIndex++;
            });

            WriteHeaderTotalCell();

            columnIndex++;

            WriteHeaderTotalDaysCell();

            columnIndex++;

            //WriteHeaderTotalFest();

            //columnIndex++;

            //WriteHeaderTotalFer();

            //columnIndex = 1;

            rowIndex++;
        }

        private void WriteColTimesheetOrd(Dictionary<string, List<TimesheetModuleItem>> cartellini, Col col)
        {
            bool negative = false;
            int totale = 0;
            String lastCant = "";
            bool motivazione = false;
            int giorni = 0;
            List<TimesheetModuleItem> ord = cartellini["justification"].OrderBy(c => c.CantDesc).ToList();
            List<TimesheetModuleItem> str = cartellini["straordinari"].OrderBy(c => c.CantDesc).ToList();
            List<TimesheetModuleItem> delta = cartellini["delta"].OrderBy(c => c.CantDesc).ToList();
            TimesheetModuleItem last = null;
            foreach (var justification in ord)
            {
                int totalDays = 0;
                //controllo se si ripete il cantiere, se si ripete significa che stiamo controllando le motivazioni inerenti a quel cantiere
                if (lastCant.Equals(justification.CantDesc))
                {
                    rowIndex -= 2;
                    motivazione = true;
                }
                else {
                    totale = 0;
                    giorni = 0;
                }
                lastCant = justification.CantDesc;
                
                
                TimesheetModuleItem tmpstr = null;
                TimesheetModuleItem tmpdel = null;
                try
                {
                    tmpstr = str.First(c => c.CantDesc == justification.CantDesc);
                    tmpdel = delta.First(c => c.CantDesc == justification.CantDesc);
                }
                catch (Exception e) { }

                var justificationDec = justification.Justification;

                if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                    justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                justificationDec = justificationDec.ToUpper();

                CellInsertValue(worksheetIndex, rowIndex + 1, 2, justification.CantDesc, ExcelInsertTypeEnum.Content);
                RangeSetBorders(worksheetIndex, rowIndex + 1, 2, rowIndex + 1, 2, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(worksheetIndex, rowIndex + 1, 2, rowIndex + 1, 2, 7);

                ColumnsSetWidth(worksheetIndex, rowIndex + 1, rowIndex + 1, 5);

                RangeSetWrapText(worksheetIndex, rowIndex + 1, 2, rowIndex + 1, 2, true);

                CellInsertValue(worksheetIndex, rowIndex + 1, 3, "ORE", ExcelInsertTypeEnum.Content);
                RangeSetBorders(worksheetIndex, rowIndex + 1, 3, rowIndex + 1, 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(worksheetIndex, rowIndex + 1, 3, rowIndex + 1, 3, 8);

                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, endMonth))
                {
                    int lastDuration = 0;
                    negative = false;
                    var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    var timeDuration = TimeSpan.FromHours(baseDuration);
                    if (baseDuration < 0)
                    {
                        negative = true;
                    }
                    
                    try
                    {
                        if (baseDuration > 0 && (double)tmpstr["Day" + day.Day.ToString("00")] > 0)
                        {
                            baseDuration -= (double)tmpstr["Day" + day.Day.ToString("00")];
                        }
                    }
                    catch (Exception e) { 
                    }
                    double print = 0.0;
                    //nel caso in cui nello stesso giorno ci siano sia motivazione che ore lavorate nello stesso cantiere andiamo a stampare il totale di queste ore
                    try {
                        if (motivazione && (double)last["Day" + day.Day.ToString("00")] > 0)
                        {
                            print = (double)last["Day" + day.Day.ToString("00")];
                        } else if (motivazione) { 
                            print = baseDuration - (double)tmpstr["Day" + day.Day.ToString("00")];
                        }
                    }
                    catch (Exception e) { 
                    }
                    baseDuration = baseDuration * 60;
                    print = print * 60;
                    string valueToPrint = "";
                    if (motivazione)
                    {
                        valueToPrint = FromTotalMinutesToFormattedType((int)print);
                        if (print == 0)
                        {
                            valueToPrint = "-- --";
                        }
                        else
                        {
                            totalDays++;
                        }
                    }
                    else {
                        valueToPrint = FromTotalMinutesToFormattedType((int)baseDuration);
                        if (baseDuration == 0)
                        {
                            valueToPrint = "-- --";
                        }
                        else
                        {
                            totalDays++;
                        }
                    }
                    
                    try {
                         if ((double)tmpdel["Day" + day.Day.ToString("00")] < 0 && baseDuration > 0)
                         {
                             RangeSetFontColor(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, Color.Red);
                         }
                    }
                     catch(Exception e) { }   

                    if ((motivazione && (int)baseDuration > 0)) {
                        if ((double)justification["Day" + day.Day.ToString("00")] == (double)last["Day" + day.Day.ToString("00")])
                        {
                            if (justification.Justification.Equals("M"))
                            {
                                valueToPrint = "-- --";
                            }
                            else if (justification.Justification.Equals("OFF"))
                            {
                                valueToPrint = "-- --";
                            }
                            else if (justification.Justification.Equals("F"))
                            {
                                valueToPrint = "-- --";
                            }
                            print = (double)last["Day" + day.Day.ToString("00")];
                        }
                        else {
                            double tmp = (double)last["Day" + day.Day.ToString("00")];
                            tmp = (tmp - (double)justification["Day" + day.Day.ToString("00")]) * 60;
                            valueToPrint = FromTotalMinutesToFormattedType((int)tmp);
                        }
                        
                        RangeSetBorders(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, 8);

                        RangeSetWrapText(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, true);
                        CellInsertValue(worksheetIndex, rowIndex + 1, day.Day + 3, valueToPrint, ExcelInsertTypeEnum.Content);
                        totale = totale - (int)baseDuration;
                    }
                    if (!motivazione) {
                        totale = totale + (int)baseDuration;
                        RangeSetBorders(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, 8);

                        RangeSetWrapText(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, true);
                        CellInsertValue(worksheetIndex, rowIndex + 1, day.Day + 3, valueToPrint, ExcelInsertTypeEnum.Content);
                    }
                    

                    try
                    {
                        if (!motivazione)
                        {
                            if ((double)tmpdel["Day" + day.Day.ToString("00")] < 0 && baseDuration > 0)
                            {
                                RangeSetFontColor(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, Color.Red);
                                
                            }
                            else if ((double)tmpdel["Day" + day.Day.ToString("00")] < 0 && baseDuration == 0)
                            {
                                RangeSetBackgroundColor(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, Color.OrangeRed, ExcelFillStyle.Solid);
                            }

                        }
                        if (motivazione && (int)baseDuration > 0){
                            // RangeSetFontColor(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, Color.Black);
                            if (justification.Justification.Equals("M"))
                            {
                                RangeSetBackgroundColor(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, Color.Yellow, ExcelFillStyle.Solid);
                            }
                            else if (justification.Justification.Equals("OFF")) 
                            {
                                RangeSetBackgroundColor(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, Color.LightGray, ExcelFillStyle.Solid);
                            }
                            else
                            {
                                RangeSetBackgroundColor(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, Color.LightGreen, ExcelFillStyle.Solid);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                    }

                    lastDuration = (int)baseDuration;
                }
                string totalHours = FromTotalMinutesToFormattedType(totale);
                giorni = totalDays;
                if(motivazione)
                {
                    giorni = giorni - totalDays;
                }
                RangeSetBorders(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, totalHours, ExcelInsertTypeEnum.Content);
                RangeSetFontSize(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, 8);
                if (!motivazione)
                {
                    
                    RangeSetBorders(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, totalDays, ExcelInsertTypeEnum.Content);
                    RangeSetFontSize(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, 8);

                }
                
                    

                rowIndex = rowIndex + 2;

                //totalDays += justification.TotalDays;

                if (giorno == 2)
                {
                    giorno += CommonService.GetDatesFromPeriod(startMonth, endMonth).Count;
                }
                motivazione = false;
                last = justification;
            }

        }

        private void WriteColTimesheetStr(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            bool negative = false;
            String lastCant = "";
            bool motivazione = false;
            List<TimesheetModuleItem> ord = cartellini["justification"].OrderBy(c => c.CantDesc).ToList();
            List<TimesheetModuleItem> str = cartellini["straordinari"].OrderBy(c => c.CantDesc).ToList();
            List<TimesheetModuleItem> delta = cartellini["delta"].OrderBy(c => c.CantDesc).ToList();
            foreach (var justification in str)
            {
                if (lastCant.Equals(justification.CantDesc)) {
                    rowIndex -= 2;
                    motivazione = true;
                }
                lastCant = justification.CantDesc;
                var justificationDec = justification.Justification;

                if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                    justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;
                RangeSetBorders(worksheetIndex, rowIndex + 1, 2, rowIndex + 1, 2, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);


                justificationDec = justificationDec.ToUpper();

                ColumnsSetWidth(worksheetIndex, rowIndex + 1, rowIndex + 1, 6);
                RangeSetWrapText(worksheetIndex, rowIndex + 1, 2, rowIndex + 1, 2, true);

                RangeSetBorders(worksheetIndex, rowIndex + 1, 3, rowIndex + 1, 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, rowIndex + 1, 3, "STR", ExcelInsertTypeEnum.Content);
                RangeSetFontSize(worksheetIndex, rowIndex + 1, 3, rowIndex + 1, 3, 8);
                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, endMonth))
                {
                    negative = false;
                    var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    var timeDuration = TimeSpan.FromHours(baseDuration);
                    if (baseDuration < 0)
                    {
                        negative = true;
                    }

                    string valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                    if (baseDuration == 0.0)
                    {
                        valueToPrint = "-- --";
                    }

                    if ((motivazione && (int)baseDuration > 0))
                    {
                        RangeSetBorders(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, 8);

                        RangeSetWrapText(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, true);
                        CellInsertValue(worksheetIndex, rowIndex + 1, day.Day + 3, valueToPrint, ExcelInsertTypeEnum.Content);
                    }
                    if (!motivazione)
                    {
                        RangeSetBorders(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, 8);

                        RangeSetWrapText(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, true);
                        CellInsertValue(worksheetIndex, rowIndex + 1, day.Day + 3, valueToPrint, ExcelInsertTypeEnum.Content);
                    }

                    try
                    {
                        
                        if (motivazione && (int)baseDuration > 0)
                        {
                            // RangeSetFontColor(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, Color.Black);
                            if (justification.Justification.Equals("M"))
                            {
                                RangeSetBackgroundColor(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, Color.Yellow, ExcelFillStyle.Solid);
                            }
                            else if (justification.Justification.Equals("OFF"))
                            {
                                RangeSetBackgroundColor(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, Color.LightGray, ExcelFillStyle.Solid);
                            }
                            else
                            {
                                RangeSetBackgroundColor(worksheetIndex, rowIndex + 1, day.Day + 3, rowIndex + 1, day.Day + 3, Color.LightGreen, ExcelFillStyle.Solid);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                    }

                    RangeSetBorders(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, justification.TotalDays, ExcelInsertTypeEnum.Content);
                    RangeSetFontSize(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, 8);
                }
                string totalHours = FromTotalMinutesToFormattedType(justification.TotalMinutes);


                RangeSetBorders(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, totalHours, ExcelInsertTypeEnum.Content);
                RangeSetFontSize(worksheetIndex, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex + 1, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, 8);
                rowIndex = rowIndex + 2;
                //totalDaysStr += justification.TotalDays;
                motivazione = false;
            }

        }
        #region Header

        private void WriteHeaderDayCell(DateTime date)
        {
            CellInsertValue(worksheetIndex, rowIndex, columnIndex + 3, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, rowIndex, columnIndex + 3, rowIndex, date.Day + 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(worksheetIndex, rowIndex, columnIndex + 3, rowIndex, date.Day + 3, "0");
            RangeSetBackgroundColor(worksheetIndex, rowIndex, columnIndex + 3, rowIndex, date.Day + 3, Color.LightGray, fillStyle);
            RangeSetWrapText(worksheetIndex, rowIndex, columnIndex + 3, rowIndex, date.Day + 3, true);
            ColumnsSetWidth(worksheetIndex, rowIndex, rowIndex, 6);
            RangeSetFontBold(worksheetIndex, rowIndex + 1, columnIndex + 3, rowIndex + 1, date.Day + 3);

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(worksheetIndex, rowIndex, columnIndex + 3, rowIndex, columnIndex + 3, Color.Red);
            }
        }
        private void WriteHeaderDayCellDay(DateTime date)
        {
            String giorno = "";
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    giorno = "L";
                    break;
                case DayOfWeek.Tuesday:
                    giorno = "M";
                    break;
                case DayOfWeek.Wednesday:
                    giorno = "M";
                    break;
                case DayOfWeek.Thursday:
                    giorno = "G";
                    break;
                case DayOfWeek.Friday:
                    giorno = "V";
                    break;
                case DayOfWeek.Saturday:
                    giorno = "S";
                    break;
                case DayOfWeek.Sunday:
                    giorno = "D";
                    break;
            }

            CellInsertValue(worksheetIndex, rowIndex + 1, columnIndex + 3, giorno, ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, rowIndex + 1, columnIndex + 3, rowIndex + 1, date.Day + 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(worksheetIndex, rowIndex + 1, columnIndex + 3, rowIndex + 1, date.Day + 3, "0");
            RangeSetBackgroundColor(worksheetIndex, rowIndex + 1, columnIndex + 3, rowIndex + 1, date.Day + 3, Color.LightGray, fillStyle);
            RangeSetFontSize(worksheetIndex, rowIndex + 1, columnIndex + 3, rowIndex + 1, date.Day + 3, 12);
            RangeSetWrapText(worksheetIndex, rowIndex + 1, columnIndex + 3, rowIndex + 1, date.Day + 3, true);
            ColumnsSetWidth(worksheetIndex, rowIndex + 1, rowIndex + 1, 6);

            RangeSetFontBold(worksheetIndex, rowIndex + 1, columnIndex + 3, rowIndex + 1, date.Day + 3);
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(worksheetIndex, rowIndex + 1, columnIndex + 3, rowIndex + 1, columnIndex + 3, Color.Red);
            }
        }

        private void WriteHeaderTotalCell()
        {

            CellInsertValue(worksheetIndex, rowIndex, columnIndex + 3, "Tot", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, rowIndex, columnIndex + 3, rowIndex, columnIndex + 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, rowIndex, columnIndex + 3, rowIndex, columnIndex + 3, Color.LightGray, fillStyle);
            RangeSetFontBold(worksheetIndex, rowIndex, columnIndex + 3, rowIndex, columnIndex + 3);

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

            CellInsertValue(worksheetIndex, rowIndex, columnIndex + 2, "Totale feriali", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, rowIndex, columnIndex + 2, rowIndex, columnIndex + 2, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, rowIndex, columnIndex + 2, rowIndex, columnIndex + 2, Color.LightGray, fillStyle);

        }

        private void WriteHeaderTotalDaysCell()
        {
            CellInsertValue(worksheetIndex, rowIndex, columnIndex + 3, "Tot.G", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, rowIndex, columnIndex + 3, rowIndex, columnIndex + 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, rowIndex, columnIndex + 3, rowIndex, columnIndex + 3, Color.LightGray, fillStyle);

        }

        private void WriteTotaleHourColOrd(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            bool motivazione = false;
            String lastCant = "";
            int totalMotivazioni = 0;
            List<TimesheetModuleItem> tot = cartellini["totale"].OrderBy(c => c.CantDesc).ToList();
            List<TimesheetModuleItem> str = cartellini["Totstraordinari"].OrderBy(c => c.CantDesc).ToList();
            RangeSetBorders(worksheetIndex, rowIndex, 2, rowIndex, 2, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);

            RangeSetBorders(worksheetIndex, rowIndex, 3, rowIndex, 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            CellInsertValue(worksheetIndex, rowIndex, 3, "Tot Ord", ExcelInsertTypeEnum.Content);
            RangeSetFontSize(worksheetIndex, rowIndex, 3, rowIndex, 3, 8);
            foreach (var justification in tot)
            {
                if (lastCant.Equals(justification.CantDesc)) { 
                    motivazione = true;
                }
                lastCant = justification.CantDesc;
                TimesheetModuleItem strCan = str.First(c => c.CantId == justification.CantId);
                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, endMonth))
                {
                    bool errore = false;
                    double justStr = 0.0;
                    try
                    {
                        justStr = (double)strCan["Day" + day.Day.ToString("00")];
                    }
                    catch (Exception e) {
                        errore = true;
                    }
                    double baseDuration = 0.0;
                    if (errore)
                    {
                        baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    }
                    else {
                        baseDuration = (double)justification["Day" + day.Day.ToString("00")] - justStr;
                    }
                    
                    var timeDuration = TimeSpan.FromHours(baseDuration);

                    string valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                    if (baseDuration == 0.0)
                    {
                        valueToPrint = "-- --";
                    }

                    RangeSetBorders(worksheetIndex, rowIndex, day.Day + 3, rowIndex, day.Day + 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(worksheetIndex, rowIndex, day.Day + 3, rowIndex, day.Day + 3, 8);
                    CellInsertValue(worksheetIndex, rowIndex, day.Day + 3, valueToPrint, ExcelInsertTypeEnum.Content);
                }
                string totalHours = FromTotalMinutesToFormattedType(justification.TotalMinutes - strCan.TotalMinutes);

                RangeSetBorders(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, justification.TotalDays, ExcelInsertTypeEnum.Content);
                RangeSetFontSize(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, 8);

                RangeSetBorders(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, totalHours, ExcelInsertTypeEnum.Content);
                RangeSetFontSize(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, 8);
                //totalDaysStr += justification.TotalDays;
                ColumnsSetWidth(worksheetIndex, rowIndex, rowIndex, 6);
                RangeSetWrapText(worksheetIndex, rowIndex, 2, rowIndex, 2, true);
            }
        }

        private void WriteTotaleHourColStr(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            List<TimesheetModuleItem> str = cartellini["Totstraordinari"].OrderBy(c => c.CantDesc).ToList();


            RangeSetBorders(worksheetIndex, rowIndex, 2, rowIndex, 2, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBorders(worksheetIndex, rowIndex, 3, rowIndex, 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            CellInsertValue(worksheetIndex, rowIndex, 3, "Tot Str", ExcelInsertTypeEnum.Content);
            RangeSetFontSize(worksheetIndex, rowIndex, 3, rowIndex, 3, 8);
            ColumnsSetWidth(worksheetIndex, rowIndex, rowIndex, 6);
            RangeSetWrapText(worksheetIndex, rowIndex, 2, rowIndex, 2, true);
            foreach (var justification in str)
            {
                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, endMonth))
                {
                    var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    var timeDuration = TimeSpan.FromHours(baseDuration);

                    string valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                    if (baseDuration == 0.0)
                    {
                        valueToPrint = "-- --";
                    }

                    RangeSetBorders(worksheetIndex, rowIndex, day.Day + 3, rowIndex, day.Day + 3, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(worksheetIndex, rowIndex, day.Day + 3, rowIndex, day.Day + 3, 8);
                    CellInsertValue(worksheetIndex, rowIndex, day.Day + 3, valueToPrint, ExcelInsertTypeEnum.Content);
                }
                string totalHours = FromTotalMinutesToFormattedType(justification.TotalMinutes);

                RangeSetBorders(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, justification.TotalDays, ExcelInsertTypeEnum.Content);
                RangeSetFontSize(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, 8);

                RangeSetBorders(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, totalHours, ExcelInsertTypeEnum.Content);
                RangeSetFontSize(worksheetIndex, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, 8);
                //totalDaysStr += justification.TotalDays;
            }
            rowIndex = 1;
            columnIndex = 1;
        }

        #endregion

        #endregion
    }
}
