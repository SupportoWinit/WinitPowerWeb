using Business.BusinessExtension;
using Business.Repository;
using Common;
using DevExpress.XtraSpreadsheet.Model;
using Domain;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Westwind.Utilities.Extensions;
using static Business.MDBSchema.PowerMDBDataSet;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    class ExportCartellinoCantiereIndennita : ExcelToolBox
    {
        private int rowIndex = 1;
        private int columnIndex = 1;
        private int worksheetIndex = 0;
        private int colore = 0;

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
                rowIndex = 1;
                var worksheet = ExcelWorkbook.Workbook.Worksheets.Add(ExportDate.Month.ToString("MMMM yyyy").ToUpper());
                worksheetIndex++;
                WriteTimesheetHeader();
                columnIndex = 4;

                WriteColHeaderIndennita();
                foreach (var col in cartellini.OrderBy(c => c.Key.Cognome_Col))
                {
                    columnIndex = 1;
                    WriteColTimesheetIndennita(col.Value, col.Key.CognomeNome_Col);
                    //WriteGrandTotalIndennita(col.Value);
                    if (colore == 0)
                    {
                        colore = 1;
                    }
                    else 
                    {
                        colore = 0;
                    }
                }
                ExcelWorkbook.Workbook.FullCalcOnLoad = true;
            }

        }

        private void WriteTimesheetHeader()
        {
            CellInsertValue(worksheetIndex, rowIndex, 2, "COGNOME E NOME", ExcelInsertTypeEnum.Content);
            RangeSetFontBold(worksheetIndex, rowIndex , 2, rowIndex, 2);
            RangeSetTextVerticalAlignment(worksheetIndex, rowIndex, 2, rowIndex, 2, ExcelVerticalAlignment.Center);
            RangeSetTextHorizontalAlignment(worksheetIndex, rowIndex, 2, rowIndex, 2, ExcelHorizontalAlignment.Center);

            rowIndex++;

            CellInsertValue(worksheetIndex, rowIndex, 2 , "CANTIERE", ExcelInsertTypeEnum.Content);
            RangeSetFontBold(worksheetIndex, rowIndex, 2, rowIndex, 2);
            RangeSetTextVerticalAlignment(worksheetIndex, rowIndex, 2, rowIndex, 2, ExcelVerticalAlignment.Center);
            RangeSetTextHorizontalAlignment(worksheetIndex, rowIndex, 2, rowIndex, 2, ExcelHorizontalAlignment.Center);

            rowIndex++;

            CellInsertValue(worksheetIndex, rowIndex, 2, "CODICE CANTIERE", ExcelInsertTypeEnum.Content);
            RangeSetFontBold(worksheetIndex, 2, rowIndex, 2, rowIndex);
            RangeSetTextVerticalAlignment(worksheetIndex, 2, rowIndex, 2, rowIndex, ExcelVerticalAlignment.Center);
            RangeSetTextHorizontalAlignment(worksheetIndex, 2, rowIndex, 2, rowIndex, ExcelHorizontalAlignment.Center);
            ColumnsSetAutoWidth(worksheetIndex, rowIndex, rowIndex);

            rowIndex++;

            var days = Common.CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1));
            RangeUnion(worksheetIndex, rowIndex, 2, rowIndex + days.Count(), 2);

            CellInsertValue(worksheetIndex, rowIndex, 2, "MESE: " + ExportDate.ToString("MMMM yyyy").ToUpper(), ExcelInsertTypeEnum.Content);
            RangeSetFontBold(worksheetIndex, rowIndex, 2, rowIndex + days.Count(), 2);
            RangeSetTextVerticalAlignment(worksheetIndex, rowIndex, 2, rowIndex + days.Count(), 2, ExcelVerticalAlignment.Center);
            RangeSetTextHorizontalAlignment(worksheetIndex, rowIndex, 2, rowIndex + days.Count(), 2, ExcelHorizontalAlignment.Center);

            RangeSetBackgroundColor(worksheetIndex, 1, 2, rowIndex + days.Count() + 2, 2, Color.FromArgb(92, 208, 80), fillStyle);

            rowIndex = 3;
        }

        private void WriteColHeaderIndennita()
        {
            var days = Common.CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1));

            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, days.Count + 4, rowIndex);

            RangeSetBackgroundColor(worksheetIndex, 3, rowIndex, 3, rowIndex, Color.FromArgb(92, 208, 80), fillStyle);

            days.ForEach(day =>
            {
                CellInsertValue(worksheetIndex, columnIndex, rowIndex, day.Day, Common.ExcelInsertTypeEnum.Content);
                RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetValueFormat(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, "0");
                ColumnsSetWidth(worksheetIndex, columnIndex, columnIndex,6);
                columnIndex++;
            });

            CellInsertValue(worksheetIndex, columnIndex, rowIndex, "TOT.", Common.ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            ColumnsSetWidth(worksheetIndex, columnIndex, columnIndex, 5);

            columnIndex++;  

            CellInsertValue(worksheetIndex, columnIndex, rowIndex, "TRANSF", Common.ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            ColumnsSetWidth(worksheetIndex, columnIndex, columnIndex, 8);

            columnIndex++;

            CellInsertValue(worksheetIndex, columnIndex, rowIndex, "GUIDA", Common.ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            ColumnsSetWidth(worksheetIndex, columnIndex, columnIndex, 10.46);

            columnIndex = 1;

            rowIndex++;
        }

        private void WriteColTimesheetIndennita(Dictionary<string, List<TimesheetModuleItem>> cartellini, string collaboratoreNome)
        {
            List<int> daysIndennita = new List<int>();
            CellInsertValue(worksheetIndex,columnIndex,rowIndex, collaboratoreNome.ToUpper(), ExcelInsertTypeEnum.Content);
            ColumnsSetAutoWidth(worksheetIndex, columnIndex, columnIndex);
            columnIndex++;
            foreach (var cartRow in cartellini["justification"].OrderByDescending(c => c.CantMnemonic).ToList())
            {
                if (cartRow.CantDesc != null) 
                {
                    if (!cartRow.CantDesc.Contains("Pausa") && !cartRow.CantDesc.Contains("Totale")) 
                    {
                        columnIndex = 2;
                        if (cartRow.Justification.Contains("INDENNITÀ"))
                        {
                            rowIndex--;
                            int giorni = 0;
                            foreach (var day in Common.CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1)))
                            {
                                var dayNumber = day.Day;
                                string valueToPrint = FromTotalMinutesToFormattedTypeVirgola((int)cartRow.DaysHours[dayNumber].Item1);
                                if ((int)cartRow.DaysHours[dayNumber].Item1 > 0)
                                {
                                    if (!daysIndennita.Contains(dayNumber))
                                    {
                                        daysIndennita.Add(dayNumber);
                                        giorni++;
                                    } 
                                }
                            }
                            if (giorni > 0)
                            {
                                CellInsertValue(worksheetIndex, cartRow.DaysHours.Count + 6, rowIndex, giorni, Common.ExcelInsertTypeEnum.Content);
                            }
                            RangeSetBorders(worksheetIndex, cartRow.DaysHours.Count + 6, rowIndex, cartRow.DaysHours.Count + 6, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            if (colore == 1)
                            {
                                RangeSetBackgroundColor(worksheetIndex, cartRow.DaysHours.Count + 6, rowIndex, cartRow.DaysHours.Count + 6, rowIndex, Color.FromArgb(204, 192, 218), fillStyle);
                            }
                        }
                        else
                        {
                            List<Reg_V> tripsCant = RepoManager.Reg_VRepo.GetAllQueryable(reg => reg.Col_Id == cartRow.ColId && reg.Cant_Id == cartRow.CantId && reg.Registrazione_Tipo_Reg == 4 && reg.Data_Ora_Fis_E > startMonth && reg.Data_Ora_Fis_E < endMonth).ToList();
                            string trasf = "";
                            DateTime lastDate = DateTime.MinValue;
                            int km = 0;
                            int gg = 0;
                            foreach (Reg_V regv in tripsCant)
                            {
                                if (regv.KM_Reg != null)
                                    km = (int)regv.KM_Reg;
                            }
                            if (km > 0)
                            {
                                if (km > 17 && km < 35)
                                {
                                    trasf = "14,5%";
                                }
                                else if (km > 35)
                                {
                                    trasf = "22%";
                                }
                                RangeSetBorders(worksheetIndex, cartRow.DaysHours.Count + 5, rowIndex, cartRow.DaysHours.Count + 5, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                CellInsertValue(worksheetIndex, cartRow.DaysHours.Count + 5, rowIndex, trasf, Common.ExcelInsertTypeEnum.Content);
                            }
                            else
                            {
                                RangeSetBorders(worksheetIndex, cartRow.DaysHours.Count + 5, rowIndex, cartRow.DaysHours.Count + 5, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            }
                            var justificationDec = cartRow.Justification;

                            if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                                justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                            justificationDec = justificationDec.ToUpper();

                            if (justificationDec != "TOTALE")
                            {
                                Cant cantiere = RepoManager.CantRepo.FirstOrDefault(c => c.Cant_Id == cartRow.CantId);
                                CellInsertValue(worksheetIndex, columnIndex, rowIndex, justificationDec, Common.ExcelInsertTypeEnum.Content);
                                ColumnsSetAutoWidth(worksheetIndex, columnIndex, columnIndex);
                                if (colore == 1)
                                {
                                    RangeSetBackgroundColor(worksheetIndex, 1, rowIndex, columnIndex, rowIndex, Color.FromArgb(204, 192, 218), fillStyle);
                                }
                                columnIndex++;
                                CellInsertValue(worksheetIndex, columnIndex, rowIndex, cantiere.Codice_Gestionale_Can, Common.ExcelInsertTypeEnum.Content);
                                RangeSetBackgroundColor(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, Color.FromArgb(92, 208, 80), fillStyle);
                                columnIndex++;

                                foreach (var day in Common.CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1)))
                                {
                                    var dayNumber = day.Day;
                                    string valueToPrint = FromTotalMinutesToFormattedTypeVirgola((int)cartRow.DaysHours[dayNumber].Item1);
                                    if ((int)cartRow.DaysHours[dayNumber].Item1 > 0)
                                    {
                                        RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                        CellInsertValue(worksheetIndex, columnIndex, rowIndex, valueToPrint, Common.ExcelInsertTypeEnum.Content);
                                    }
                                    else
                                    {
                                        RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                        CellInsertValue(worksheetIndex, columnIndex, rowIndex, "-", Common.ExcelInsertTypeEnum.Content);
                                    }
                                    RangeSetTextHorizontalAlignment(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, ExcelHorizontalAlignment.Center);
                                    RangeSetTextVerticalAlignment(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, ExcelVerticalAlignment.Center);
                                    if (colore == 1)
                                    {
                                        RangeSetBackgroundColor(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, Color.FromArgb(204, 192, 218), fillStyle);
                                    }
                                    columnIndex++;
                                }
                                //Viene considerato il range per la somma della riga, dalla colonna 1 alla colonna del numero dei giorni
                                //come riga viene considerata quella attuale
                                string rangeFormula = $"{ColumnIndexToNameConversion(4)}{rowIndex}:" +
                                    $"{ColumnIndexToNameConversion(cartRow.DaysHours.Count + 3)}{rowIndex}";

                                //Formula automatica per la somma dei valori della riga
                                string formula = $"=SUM({rangeFormula})";

                                //Conta solamente i numeri della riga, quindi i giorni diversi da "M" o "F" o "---"
                                string contaFormula = $"=COUNT({rangeFormula})";

                                RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                CellInsertValue(worksheetIndex, columnIndex, rowIndex, formula, Common.ExcelInsertTypeEnum.Formula);
                                columnIndex++;
                                //RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                //CellInsertValue(worksheetIndex, columnIndex, rowIndex, contaFormula, Common.ExcelInsertTypeEnum.Formula);
                                if (colore == 1)
                                {
                                    RangeSetBackgroundColor(worksheetIndex, columnIndex - 1, rowIndex, columnIndex, rowIndex, Color.FromArgb(204, 192, 218), fillStyle);
                                }
                                RangeSetTextHorizontalAlignment(worksheetIndex, columnIndex - 2, rowIndex, columnIndex - 2, rowIndex, ExcelHorizontalAlignment.Center);
                                RangeSetTextVerticalAlignment(worksheetIndex, columnIndex - 2, rowIndex, columnIndex - 2, rowIndex, ExcelVerticalAlignment.Center);

                                RangeSetTextHorizontalAlignment(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, ExcelHorizontalAlignment.Center);
                                RangeSetTextVerticalAlignment(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, ExcelVerticalAlignment.Center);
                            }
                            else
                            {
                                if (cartellini["justification"].Count > 1)
                                    rowIndex--;
                            }
                        }
                        rowIndex++;
                    }
                }
            }
            columnIndex = 4;
        }

        private void WriteGrandTotalIndennita(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {

            RangeSetFontBold(worksheetIndex, 1, rowIndex, 1, rowIndex);
            CellInsertValue(worksheetIndex, 1, rowIndex, "Totale", Common.ExcelInsertTypeEnum.Content);

            var cartTotale = cartellini["justification"].Where(cart => cart.Justification != "Ferie").ToList();
            cartTotale = cartTotale.Where(cart => cart.Justification != "Malattia").ToList();
            cartTotale = cartTotale.Where(cart => !cart.Justification.Contains("INDENNITÀ")).ToList();
            cartTotale = cartTotale.Where(cart => cart.Justification != "Totale").ToList();

            ILookup<int, Tuple<double, TimeSpan?, TimeSpan?>> lookupCartellini = cartTotale.Where(cart => cart.Justification != "Totale").SelectMany(cart => cart.DaysHours).ToLookup(c => c.Key, x => x.Value); //Crea una lookup ( uguale ad un dictionary <int,list<...>> che quindi permette du raggruppare valori secondo la stessa chiave)

            //Variabili per la formula e il range da usare
            string formula;
            string rangeFormula;

            foreach (var dayHourList in lookupCartellini)
            {
                //Come rang viene scelta la colonna attuale dalla riga iniziale dei valori alla riga attuale -1
                rangeFormula = $"{ColumnIndexToNameConversion(columnIndex)}{rowIndex - cartTotale.Count()}:" +
                $"{ColumnIndexToNameConversion(columnIndex)}{rowIndex - 1}";

                //Formula automatica da inserire in Excel
                formula = $"=SUM({rangeFormula})";

                RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, columnIndex, rowIndex, formula, Common.ExcelInsertTypeEnum.Formula);

                RangeSetTextHorizontalAlignment(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, ExcelHorizontalAlignment.Center);
                RangeSetTextVerticalAlignment(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, ExcelVerticalAlignment.Center);
                columnIndex++;
            }


            //Il range viene considerato nella colonna attuale e dala riga attuale -numero cantieri fino alla riga attuale -1
            rangeFormula = $"{ColumnIndexToNameConversion(columnIndex)}{rowIndex - cartTotale.Count()} :" +
                $"{ColumnIndexToNameConversion(columnIndex)}{rowIndex - 1}";

            string finalFormula = $"=SUM({rangeFormula})";


            RowsSetHeight(worksheetIndex, rowIndex, rowIndex, 30);
            RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            CellInsertValue(worksheetIndex, columnIndex, rowIndex, finalFormula, Common.ExcelInsertTypeEnum.Formula);
            RangeSetTextHorizontalAlignment(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, ExcelHorizontalAlignment.Center);
            RangeSetTextVerticalAlignment(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, ExcelVerticalAlignment.Center);
            rowIndex++;
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
