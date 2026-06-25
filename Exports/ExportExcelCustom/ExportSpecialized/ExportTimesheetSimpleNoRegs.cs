using Business.BusinessExtension;
using Business.Repository;
using Common;
using DevExpress.XtraSpreadsheet.Model;
using Domain;
using log4net;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;
using Westwind.Utilities.Extensions;
using static Business.MDBSchema.PowerMDBDataSet;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /*
     *
     *      Attenzione non gestisce i totali settimanali
     * 
     */

    public class ExportTimesheetSimpleNoRegs : ExcelToolBox
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

        /// <summary>
        /// Il default per la tabella Col_Monte_Minuti a DB
        /// </summary>
        public const int DEFAULT_MONTEMINUTI = -999999999;

        #endregion

        #region Public Methods

        public override void LaunchExport()
        {
            Param parameters = RepoManager.ParamRepo.ParametersRow;
            Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>> cartellini = new Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>>();

            List<Col> collaboratori = RepoManager.ColRepo.Find(c => SelectedIds.Contains(c.Col_Id), true).ToList();
            bool settimanali = false;
            if (parameters.Cartellino_Visualizza_Totali_Settimanali == 1) 
            {
                settimanali = true;
            }

            //viene estratto l'ide dello user che ha fatto l'accesso a Powerweb
            int userId = PowerWebContext.Current.User.Utenti_Id;

            //filtro solo i responsabili che corrispondo all'utente che ha effettuato l'accesso 
            var userRespIds = RepoManager.Utenti_RespRepo.Find(r => r.Utenti_Id == userId).ToList();

            //filtro solo le filiali che corrispondo all'utente che ha effettuato l'accesso 
            var userFilIds = RepoManager.Utenti_FilRepo.Find(r => r.Utenti_Id == userId).ToList();

            startMonth = CommonService.GetFirstMonthDay(ExportDate);
            endMonth = CommonService.GetLastMonthDay(ExportDate);

            DateTime maxDate = endMonth;

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PartialTimesheet) == 1)
            {
                if (ExportDate.Month == DateTime.Now.Month)
                {
                    endMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
                }
            }

            foreach (Col col in collaboratori) {
                IEnumerable<int> lis = new List<int>();
                lis = RepoManager.RegRepo.GetRegsIdByDateRangeByColNotBlocked(startMonth, endMonth, col.Col_Id);
                if (lis.Count() > 0 || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CollabNoHours) == 1)
                {
                    List<Reg_V> newRegs = new List<Reg_V>();
                    var colRegs = RepoManager.Reg_VRepo.GetAllQueryable().Where(r => r.Col_Id == col.Col_Id && r.Data_Ora_Fis_E > startMonth && r.Data_Ora_Fis_E < endMonth).ToList().GroupBy(r => r.Data_Reg);
                    foreach (var regGroup in colRegs)
                    { 
                        if (regGroup.ToList().Count == 1)
                        { 
                            newRegs.AddRange(regGroup.ToList());
                        }
                        else 
                        {
                            Tab_Decod motPau = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Chiave_Tab == "Pausa");
                            DateTime lastE = new DateTime();
                            foreach (Reg_V regv in regGroup.OrderBy(r => r.Data_Ora_Fis_E))
                            {
                                if (regv.Durata_Fis > 1 && regv.Motivazione_Reg_Id == null)
                                { 
                                    newRegs.Add(regv);
                                    lastE = new DateTime(regv.Data_Ora_Fig_U.Value.Year, regv.Data_Ora_Fig_U.Value.Month, regv.Data_Ora_Fig_U.Value.Day, regv.Data_Ora_Fig_U.Value.Hour, regv.Data_Ora_Fig_U.Value.Minute,0);
                                    //lastE = regv.Data_Ora_Fig_U.Value;
                                }
                                else 
                                {
                                    if (lastE == default(DateTime))
                                    {
                                        lastE = new DateTime(regv.Data_Ora_Fig_E.Value.Year, regv.Data_Ora_Fig_E.Value.Month, regv.Data_Ora_Fig_E.Value.Day, regv.Data_Ora_Fig_E.Value.Hour, regv.Data_Ora_Fig_E.Value.Minute, 0);
                                        //lastE = regv.Data_Ora_Fig_E.Value;
                                    }
                                    else if (regv == regGroup.OrderBy(r => r.Data_Ora_Fis_E).Last())
                                    {
                                        Reg_V returnReg = regv;
                                        DateTime start = new DateTime(regv.Data_Ora_Fig_E.Value.Year, regv.Data_Ora_Fig_E.Value.Month, regv.Data_Ora_Fig_E.Value.Day, regv.Data_Ora_Fig_E.Value.Hour, regv.Data_Ora_Fig_E.Value.Minute, 0);
                                        returnReg.Durata_Fig = (int)(start - lastE).TotalMinutes;
                                        returnReg.Durata_Fis = (int)(start - lastE).TotalMinutes;
                                        newRegs.Add(returnReg);
                                    }
                                    else
                                    {
                                        if (regv.Motivazione_Reg_Id != null)
                                        {
                                            if (regv.Motivazione_Reg_Id == motPau.Tab_Decod_Id)
                                            {
                                                DateTime start = new DateTime(regv.Data_Ora_Fig_E.Value.Year, regv.Data_Ora_Fig_E.Value.Month, regv.Data_Ora_Fig_E.Value.Day, regv.Data_Ora_Fig_E.Value.Hour, regv.Data_Ora_Fig_E.Value.Minute, 0);
                                                Reg_V returnReg = regv;
                                                returnReg.Durata_Fig = (int)(start - lastE).TotalMinutes;
                                                returnReg.Durata_Fis = (int)(start - lastE).TotalMinutes;
                                                returnReg.Motivazione_Reg_Id = null;
                                                returnReg.Motivazione_Reg_Cod = "";
                                                newRegs.Add(returnReg);
                                                lastE = regv.Data_Ora_Fig_U.Value;
                                            }
                                        }
                                        else 
                                        {
                                            DateTime start = new DateTime(regv.Data_Ora_Fig_E.Value.Year, regv.Data_Ora_Fig_E.Value.Month, regv.Data_Ora_Fig_E.Value.Day, regv.Data_Ora_Fig_E.Value.Hour, regv.Data_Ora_Fig_E.Value.Minute, 0);
                                            Reg_V returnReg = regv;
                                            returnReg.Durata_Fig = (int)(start - lastE).TotalMinutes;
                                            returnReg.Durata_Fis = (int)(start - lastE).TotalMinutes;
                                            newRegs.Add(returnReg);
                                            lastE = new DateTime();
                                        }
                                    }
                                }
                            } 
                        } 
                    }
                    cartellini.Add(col, TimesheetModuleItem.GenerateCartellinoCartellinoCentri(ExportDate,
                                                col,
                                                newRegs,
                                                false,
                                                settimanali,
                                                true,
                                                true,
                                                parameters.Cartellino_Visualizza_Ore,
                                                parameters.Cartellino_Visualizza_Motivazioni,
                                                parameters.Cartellino_Visualizza_Viaggi,
                                                parameters.Cartellino_Visualizza_Delta,
                                                parameters.Cartellino_Divisione_Piano_Notturno_Diurno,
                                                true,
                                                parameters.Cartellino_Visualizza_Piano));
                }
            }
                


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
                    if (settimanali) 
                    {
                        if (_multiPagedExport)
                        {
                            ExcelWorkbook.Workbook.Worksheets.Add(col.Key.CognomeNome_Col);
                            worksheetIndex++;

                            WriteTimesheetHeader();

                            if (parameters.Flag_Monte_Ore != 0)
                            {
                                WriteTimesheetColMountMinutes(col.Value);
                            }
                            else
                            {
                                rowIndex += 2;
                            } 
                        }

                        WriteTimesheetColName(col.Key);

                        WriteTimesheetColHeaderSettimanale(col.Value);

                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CalcoloRiposi) == 1)
                        {
                            WriteColTimesheetSettimanaleRiposi(col.Value);
                        }
                        else 
                        {
                            WriteColTimesheetSettimanale(col.Value);
                        }

                        rowIndex += 2;

                        if (parameters.Cartellino_Usa_Cartellino_Modificabile)
                        {
                            WriteStrTimesheetTitle();

                            WriteTimesheetColHeaderSettimanale(col.Value);

                            WriteStrTimesheetSettimanale(col.Value);

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
                    else
                    {
                        if (_multiPagedExport)
                        {
                            ExcelWorkbook.Workbook.Worksheets.Add(col.Key.CognomeNome_Col);
                            worksheetIndex++;

                            WriteTimesheetHeader();

                            if (parameters.Flag_Monte_Ore != 0)
                            {
                                WriteTimesheetColMountMinutes(col.Value);
                            }
                            else
                            {
                                rowIndex += 2;
                            }
                        }

                        WriteTimesheetColName(col.Key);

                        WriteTimesheetColHeader();

                        WriteColTimesheet(col.Value);

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

        private void WriteTimesheetColName(Col col)
        {
            RangeUnion(worksheetIndex, 1, rowIndex, 5, rowIndex);
            RangeSetFontBold(worksheetIndex, 1, rowIndex, 5, rowIndex);
            CellInsertValue(worksheetIndex, 1, rowIndex, col.CognomeNome_Col, ExcelInsertTypeEnum.Content);

            rowIndex++;
        }

        private void WriteTimesheetColMountMinutes(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            RangeUnion(worksheetIndex, 1, rowIndex, 5, rowIndex);
            RangeSetFontBold(worksheetIndex, 1, rowIndex, 5, rowIndex);
            CellInsertValue(worksheetIndex, 1, rowIndex, "Monte Ore Residuo: " + cartellini["justification"].First(c => c.Justification == "Delta").LastMonthlyHours, ExcelInsertTypeEnum.Content);
            rowIndex += 2;
        }

        private void WriteTimesheetColHeader()
        {
            int giorniRimanenti = 1;
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PartialTimesheet) == 1)
            {
                if (ExportDate.Month == DateTime.Now.Month)
                {
                    DateTime last = ExportDate.EndOfMonth();
                    giorniRimanenti = (last.Day - DateTime.Now.Day) + 1;
                }
            }

            List<DateTime> days = CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-giorniRimanenti)); //Calcolo i giorni per l'header

            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, days.Count + 3, rowIndex);

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

        private void WriteTimesheetColHeaderSettimanale(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            int settimana = 1;

            var car = cartellini["justification"];

            int negativeDays = 0;

            DateTime tmpExportDate = ExportDate;

            DateTime endMonth = ExportDate.AddMonths(1).AddDays(-1);

            if (car.First().DaysHours.First().Key < 0)
            {
                ExportDate = ExportDate.AddDays(car.First().DaysHours.First().Key);
                negativeDays = negativeDays + car.First().DaysHours.First().Key;
            }

            List<DateTime> days = CommonService.GetDatesFromPeriod(ExportDate, endMonth); //Calcolo i giorni per l'header

            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, days.Count + 3, rowIndex);

            days.ForEach(day =>
            {
                WriteHeaderDayCellSettimanale(day,settimana);

                columnIndex++;

                if (day.DayOfWeek == DayOfWeek.Sunday) {
                    CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, "SETTIMANA "+ settimana, ExcelInsertTypeEnum.Content);
                    RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, "0");
                    RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);

                    columnIndex++;

                    settimana++;
                }
            });

            WriteHeaderTotalCell();

            columnIndex++;

            WriteHeaderTotalDaysCell();

            columnIndex = 1;

            rowIndex++;

            ExportDate = tmpExportDate;
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

                DateTime maxDate = endMonth;

                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PartialTimesheet) == 1)
                {
                    if (ExportDate.Month == DateTime.Now.Month)
                    {
                        maxDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
                    }
                }

                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, maxDate))
                {
                    var minuti = justification.DaysHours[day.Day];
                    string valueToPrint = FromTotalMinutesToFormattedType((int)minuti.Item1);

                    if (minuti.Item1 > 0)
                    {
                        RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                    }
                    else 
                    {
                        RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, "-", ExcelInsertTypeEnum.Content);
                    }

                        
                }

                string totalHours = FromTotalMinutesToFormattedType(justification.TotalMinutes);

                //Viene considerato il range per la somma della riga, dalla colonna 1 alla colonna del numero dei giorni
                //come riga viene considerata quella attuale
                string rangeFormula = $"{ColumnIndexToNameConversion(columnIndex + 1)}{rowIndex}:" +
                    $"{ColumnIndexToNameConversion(columnIndex + justification.DaysHours.Count)}{rowIndex}";

                //Formula automatica per la somma dei valori della riga
                string formula = $"=SUM({rangeFormula})";

                //Conta solamente i numeri della riga, quindi i giorni diversi da "M" o "F" o "---"
                string contaFormula = $"=COUNT({rangeFormula})";

                RangeSetBorders(worksheetIndex, columnIndex + justification.DaysHours.Count + 1, rowIndex, columnIndex + justification.DaysHours.Count + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, columnIndex + justification.DaysHours.Count + 1, rowIndex, formula, Common.ExcelInsertTypeEnum.Formula);

                RangeSetBorders(worksheetIndex, columnIndex + justification.DaysHours.Count + 2, rowIndex, columnIndex + justification.DaysHours.Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, columnIndex + justification.DaysHours.Count + 2, rowIndex, contaFormula, Common.ExcelInsertTypeEnum.Formula);

                //RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                //CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, totalHours, ExcelInsertTypeEnum.Content);

                //RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 3, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                //CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 3, rowIndex, justification.TotalDays, ExcelInsertTypeEnum.Content);


                rowIndex++;

            }

        }

        private void WriteColTimesheetSettimanale(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            foreach (var justification in cartellini["justification"])
            {
                    int tmp = columnIndex;
                var justificationDec = justification.Justification;
                DateTime tmpStart = startMonth;
                int days = 1;

                int settimana = 1;
                double totale = 0;

                if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                    justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                justificationDec = justificationDec.ToUpper();

                CellInsertValue(worksheetIndex, 1, rowIndex, justificationDec, ExcelInsertTypeEnum.Content);

                int negativeDays = 0;

                if (justification.DaysHours.First().Key < 0) {
                    startMonth = startMonth.AddDays(justification.DaysHours.First().Key);
                    negativeDays = negativeDays + justification.DaysHours.First().Key;
                }

                DateTime maxDate = endMonth;

                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PartialTimesheet) == 1)
                {
                    if (ExportDate.Month == DateTime.Now.Month)
                    {
                        maxDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
                    }
                }

                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, maxDate))
                {
                    if (negativeDays < 0)
                    {
                        var cartRow = justification.DaysHours.Where(d => d.Key == negativeDays);
                        var baseDuration = (double)cartRow.First().Value.Item1;
                        var timeDuration = TimeSpan.FromMinutes(baseDuration);
                        string valueToPrint = "";
                        if (baseDuration < 0 && baseDuration > -1)
                        {
                            valueToPrint = "-" + FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                        }
                        else
                        {
                            valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                        }
                        RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                        negativeDays++;
                        columnIndex++;
                    }
                    else {
                        var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                        var timeDuration = TimeSpan.FromHours(baseDuration);
                        string valueToPrint = "";
                        if (baseDuration < 0 && baseDuration > -1)
                        {
                            valueToPrint = "" + FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                        }
                        else
                        {
                            valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                        }

                        RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                        totale += timeDuration.TotalMinutes;
                        if (day.DayOfWeek == DayOfWeek.Sunday)
                        {

                            baseDuration = (double)justification["TotalWeek" + settimana.ToString("0")];
                            timeDuration = TimeSpan.FromHours(baseDuration);
                            valueToPrint = "";
                            if (baseDuration < 0 && baseDuration > -1)
                            {
                                valueToPrint = "" + FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                            }
                            else
                            {
                                valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                            }

                            columnIndex++;

                            RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);

                            settimana++;
                        }
                    }
                }

                string totalHours = FromTotalMinutesToFormattedType((int)totale);

                RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 1 + settimana, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 1 + settimana, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 1 + settimana, rowIndex, totalHours, ExcelInsertTypeEnum.Content);

                RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2 + settimana, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2 + settimana, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2 + settimana, rowIndex, justification.TotalDays, ExcelInsertTypeEnum.Content);


                rowIndex++;
                columnIndex = tmp;
                startMonth = tmpStart;
            }

        }

        private void WriteColTimesheetSettimanaleRiposi(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            foreach (var justification in cartellini["justification"])
            {
                int tmp = columnIndex;
                var justificationDec = justification.Justification;
                DateTime tmpStart = startMonth;
                int days = 1;

                int settimana = 1;
                double totale = 0;

                if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                    justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                justificationDec = justificationDec.ToUpper();

                CellInsertValue(worksheetIndex, 1, rowIndex, justificationDec, ExcelInsertTypeEnum.Content);

                int negativeDays = 0;

                if (justification.DaysHours.First().Key < 0)
                {
                    startMonth = startMonth.AddDays(justification.DaysHours.First().Key);
                    negativeDays = negativeDays + justification.DaysHours.First().Key;
                }

                DateTime maxDate = endMonth;

                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PartialTimesheet) == 1)
                {
                    if (ExportDate.Month == DateTime.Now.Month)
                    {
                        maxDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
                    }
                }

                if (justification.Justification == "RIPOSI")
                {
                    foreach (var day in CommonService.GetDatesFromPeriod(startMonth, maxDate))
                    {
                        if (negativeDays < 0)
                        {
                            var cartRow = justification.DaysHours.Where(d => d.Key == negativeDays);
                            var baseDuration = (double)cartRow.First().Value.Item1;
                            var timeDuration = TimeSpan.FromMinutes(baseDuration);
                            string valueToPrint = "";
                            if (baseDuration < 0 && baseDuration > -1)
                            {
                                valueToPrint = "-" + FromTotalMinutesToFormattedTypeRiposi((int)timeDuration.TotalMinutes);
                            }
                            else
                            {
                                valueToPrint = FromTotalMinutesToFormattedTypeRiposi((int)timeDuration.TotalMinutes);
                            }
                            if (baseDuration != 0)
                            {
                                RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex - 1, columnIndex + 1, rowIndex - 1, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex - 1, "RR" + valueToPrint, ExcelInsertTypeEnum.Content);
                            }
                            negativeDays++;
                            columnIndex++;
                        }
                        else
                        {
                            var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                            var timeDuration = TimeSpan.FromHours(baseDuration);
                            string valueToPrint = "";
                            if (baseDuration < 0 && baseDuration > -1)
                            {
                                valueToPrint = "" + FromTotalMinutesToFormattedTypeRiposi((int)timeDuration.TotalMinutes);
                            }
                            else
                            {
                                valueToPrint = FromTotalMinutesToFormattedTypeRiposi((int)timeDuration.TotalMinutes);
                            }

                            if (baseDuration != 0)
                            {
                                RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex - 1, columnIndex + day.Day, rowIndex - 1, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex - 1, "RR" + valueToPrint, ExcelInsertTypeEnum.Content);
                            }
                            totale += timeDuration.TotalMinutes;
                            if (day.DayOfWeek == DayOfWeek.Sunday)
                            {

                                baseDuration = (double)justification["TotalWeek" + settimana.ToString("0")];
                                timeDuration = TimeSpan.FromHours(baseDuration);
                                valueToPrint = "";
                                if (baseDuration < 0 && baseDuration > -1)
                                {
                                    valueToPrint = "" + FromTotalMinutesToFormattedTypeRiposi((int)timeDuration.TotalMinutes);
                                }
                                else
                                {
                                    valueToPrint = FromTotalMinutesToFormattedTypeRiposi((int)timeDuration.TotalMinutes);
                                }

                                columnIndex++;
                                settimana++;
                            }
                        }
                    }
                }
                else if (justification.Justification == "Totale")
                {
                    foreach (var day in CommonService.GetDatesFromPeriod(startMonth, maxDate))
                    {
                        if (negativeDays < 0)
                        {
                            var cartRow = justification.DaysHours.Where(d => d.Key == negativeDays);
                            var baseDuration = (double)cartRow.First().Value.Item1;
                            var timeDuration = TimeSpan.FromMinutes(baseDuration);
                            string valueToPrint = "";
                            if (baseDuration < 0 && baseDuration > -1)
                            {
                                valueToPrint = "-" + FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                            }
                            else
                            {
                                valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                            }
                            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                            negativeDays++;
                            columnIndex++;
                        }
                        else
                        {
                            var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                            var timeDuration = TimeSpan.FromHours(baseDuration);
                            string valueToPrint = "";
                            if (baseDuration < 0 && baseDuration > -1)
                            {
                                valueToPrint = "" + FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                            }
                            else
                            {
                                valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                            }
                            RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                            totale += timeDuration.TotalMinutes;
                            if (day.DayOfWeek == DayOfWeek.Sunday)
                            {

                                baseDuration = (double)justification["TotalWeek" + settimana.ToString("0")];
                                timeDuration = TimeSpan.FromHours(baseDuration);
                                valueToPrint = "";
                                if (baseDuration < 0 && baseDuration > -1)
                                {
                                    valueToPrint = "" + FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                                }
                                else
                                {
                                    valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                                }

                                columnIndex++;

                                RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);

                                settimana++;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var day in CommonService.GetDatesFromPeriod(startMonth, maxDate))
                    {
                        if (negativeDays < 0)
                        {
                            var cartRow = justification.DaysHours.Where(d => d.Key == negativeDays);
                            var baseDuration = (double)cartRow.First().Value.Item1;
                            var timeDuration = TimeSpan.FromMinutes(baseDuration);
                            string valueToPrint = "";
                            if (baseDuration < 0 && baseDuration > -1)
                            {
                                valueToPrint = "-" + FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                            }
                            else
                            {
                                valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                            }
                            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                            negativeDays++;
                            columnIndex++;
                        }
                        else
                        {
                            var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                            var timeDuration = TimeSpan.FromHours(baseDuration);
                            string valueToPrint = "";
                            if (baseDuration < 0 && baseDuration > -1)
                            {
                                valueToPrint = "" + FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                            }
                            else
                            {
                                valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                            }
                            RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                            totale += timeDuration.TotalMinutes;
                            if (day.DayOfWeek == DayOfWeek.Sunday)
                            {

                                baseDuration = (double)justification["TotalWeek" + settimana.ToString("0")];
                                timeDuration = TimeSpan.FromHours(baseDuration);
                                valueToPrint = "";
                                if (baseDuration < 0 && baseDuration > -1)
                                {
                                    valueToPrint = "" + FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                                }
                                else
                                {
                                    valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                                }

                                columnIndex++;

                                RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);

                                settimana++;
                            }
                        }
                    }
                }

                if (justification.Justification != "RIPOSI")
                {
                    string totalHours = FromTotalMinutesToFormattedType((int)totale);

                    RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 1 + settimana, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 1 + settimana, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 1 + settimana, rowIndex, totalHours, ExcelInsertTypeEnum.Content);

                    RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2 + settimana, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2 + settimana, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2 + settimana, rowIndex, justification.TotalDays, ExcelInsertTypeEnum.Content);

                    rowIndex++;
                }  
                columnIndex = tmp;
                startMonth = tmpStart;
            }

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

                DateTime maxDate = endMonth;

                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PartialTimesheet) == 1)
                {
                    if (ExportDate.Month == DateTime.Now.Month)
                    {
                        maxDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
                    }
                }

                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, maxDate))
                {
                    var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    var timeDuration = TimeSpan.FromHours(baseDuration);

                    string valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);

                    RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                }

                string totalHours = FromTotalMinutesToFormattedType(justification.TotalMinutes);

                RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, totalHours, ExcelInsertTypeEnum.Content);

                RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 3, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 3, rowIndex, justification.TotalDays, ExcelInsertTypeEnum.Content);


                rowIndex++;

            }

        }
        private void WriteStrTimesheetSettimanale(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            var car = cartellini["justification"];

            int negativeDays = 0;

            DateTime tmpExportDate = ExportDate;

            DateTime endMonth = ExportDate.AddMonths(1).AddDays(-1);

            foreach (var justification in cartellini["straordinari"])
            {
                int tmp = columnIndex;
                int settimana = 1;

                if (car.First().DaysHours.First().Key < 0)
                {
                    ExportDate = ExportDate.AddDays(car.First().DaysHours.First().Key);
                    negativeDays = negativeDays + car.First().DaysHours.First().Key;
                }

                DateTime tmpStart = startMonth;
                int days = 1;
                double totale = 0;

                var justificationDesc = justification.Justification;

                if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDesc))
                    justificationDesc = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDesc).Decodifica_Tab;

                justificationDesc = justificationDesc.ToUpper();

                CellInsertValue(worksheetIndex, 1, rowIndex, justificationDesc, ExcelInsertTypeEnum.Content);

                foreach (var day in CommonService.GetDatesFromPeriod(ExportDate, endMonth))
                {
                    if (negativeDays < 0)
                    {
                        var cartRow = justification.DaysHours.Where(d => d.Key == negativeDays);
                        var baseDuration = 0;
                        var timeDuration = TimeSpan.FromMinutes(baseDuration);
                        string valueToPrint = "";
                        if (baseDuration < 0 && baseDuration > -1)
                        {
                            valueToPrint = "-" + FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                        }
                        else
                        {
                            valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);
                        }
                    
                        RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                        negativeDays++;
                        columnIndex++;
                    }
                    else {
                        var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                        var timeDuration = TimeSpan.FromHours(baseDuration);

                        string valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);

                        RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                        totale += timeDuration.TotalMinutes;
                        if (day.DayOfWeek == DayOfWeek.Sunday)
                        {
                            baseDuration = (double)justification["TotalWeek" + settimana.ToString("0")];
                            timeDuration = TimeSpan.FromHours(baseDuration);

                            valueToPrint = FromTotalMinutesToFormattedType((int)timeDuration.TotalMinutes);

                            columnIndex++;
                            RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);

                            settimana++;
                        }
                    }     
                }

                string totalHours = FromTotalMinutesToFormattedType((int)totale);

                RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(ExportDate, endMonth).Count + 1 + settimana, rowIndex, CommonService.GetDatesFromPeriod(ExportDate, endMonth).Count + 1 + settimana, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(ExportDate, endMonth).Count + 1 + settimana, rowIndex, totalHours, ExcelInsertTypeEnum.Content);

                RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(ExportDate, endMonth).Count + 2 + settimana, rowIndex, CommonService.GetDatesFromPeriod(ExportDate, endMonth).Count + 2 + settimana, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(ExportDate, endMonth).Count + 2 + settimana, rowIndex, justification.TotalDays, ExcelInsertTypeEnum.Content);

                rowIndex++;
                columnIndex = tmp;
                ExportDate = tmpStart;
            }

        }

        #region Header

        private void WriteHeaderDayCell(DateTime date)
        {
            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, date.Day + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex, date.Day + 1, rowIndex, "0");
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, date.Day + 1, rowIndex, Color.LightGray, fillStyle);

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);
            }
        }

        private void WriteHeaderDayCellSettimanale(DateTime date, int settimana)
        {
            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, "0");
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);

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
