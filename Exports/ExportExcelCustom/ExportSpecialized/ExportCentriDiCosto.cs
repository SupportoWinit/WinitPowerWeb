using Business.BusinessExtension;
using Business.Repository;
using Common;
using Domain;
using log4net;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Windows.Forms.VisualStyles;
using Westwind.Utilities.Extensions;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /*
     *
     *      Attenzione non gestisce i totali settimanali
     * 
     */

    public class ExportCentriDiCosto : ExcelToolBox
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

        private bool _multiPagedExport = true;// Convert.ToBoolean(RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheetMultiPagedExport));

        #endregion

        #region Public Methods

        public override void LaunchExport()
        {
            Param parameters = RepoManager.ParamRepo.ParametersRow;

            Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>> cartellini = new Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>>();
            Dictionary<string, Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>>> centri = new Dictionary<string, Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>>>();

            List<CentroDiCosto> cdc = RepoManager.CentroDiCostoRepo.GetAll().ToList();

            startMonth = CommonService.GetFirstMonthDay(ExportDate);
            endMonth = CommonService.GetLastMonthDay(ExportDate);

            foreach (CentroDiCosto centro in cdc)
            {
                var regVs = RepoManager.Reg_VRepo.Find(r => r.Data_Reg >= startMonth && r.Data_Reg <= endMonth && r.CentroDiCosto_Id == centro.CentroDiCosto_Id && r.Col_Id != null).GroupBy(r => r.Col_Id).ToList();

                cartellini = new Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>>();
                foreach (var reg in regVs)
                {
                    if (SelectedIds.Contains(reg.Key.Value))
                    {
                        List<Col> collaboratori = RepoManager.ColRepo.GetAll().Where(c => c.Col_Id == reg.Key && c.DisAbilitazione_Col == false).OrderBy(c => c.Cognome_Col).ToList();
                        foreach (Col col in collaboratori)
                        {
                            if (col.Qualifica_Col != "0")
                            {
                                var regs = RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Col_Id == col.Col_Id
                                && (regv.Data_Reg >= startMonth && regv.Data_Reg <= endMonth)
                                && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att && (regv.Codice_Commessa_Can == "Pulizie Civile" || regv.Codice_Commessa_Can == "PULIZIE CIVILE"), true);
                                if (regs.Count() > 0)
                                {
                                    cartellini.Add(col, TimesheetModuleItem.GenerateCartellinoCentroDiCosto(ExportDate,
                                                                    col,
                                                                    centro.CentroDiCosto_Id,
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
                            else 
                            {
                                if (col.Scadenza_Patente_Col != null)
                                {
                                    if (col.Scadenza_Patente_Col.Value.Between(startMonth, endMonth))
                                    {
                                        var regs = RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Col_Id == col.Col_Id
                                        && (regv.Data_Reg >= startMonth && regv.Data_Reg <= col.Scadenza_Patente_Col.Value)
                                        && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att && (regv.Codice_Commessa_Can == "Pulizie Civile" || regv.Codice_Commessa_Can == "PULIZIE CIVILE"), true);
                                        if (regs.Count() > 0)
                                        {
                                            cartellini.Add(col, TimesheetModuleItem.GenerateCartellinoCentroDiCosto(ExportDate,
                                                                            col,
                                                                            centro.CentroDiCosto_Id,
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
                                    } else if (col.Scadenza_Patente_Col.Value > endMonth) 
                                    {
                                        var regs = RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Col_Id == col.Col_Id
                                        && (regv.Data_Reg >= startMonth && regv.Data_Reg <= endMonth)
                                        && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att && (regv.Codice_Commessa_Can == "Pulizie Civile" || regv.Codice_Commessa_Can == "PULIZIE CIVILE"), true);
                                        if (regs.Count() > 0)
                                        {
                                            cartellini.Add(col, TimesheetModuleItem.GenerateCartellinoCentroDiCosto(ExportDate,
                                                                            col,
                                                                            centro.CentroDiCosto_Id,
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
                                }
                            }
                            
                        }
                    }
                }
                centri.Add(centro.Descrizione, cartellini);
            }

            var regVsAltro = RepoManager.Reg_VRepo.Find(r => r.Data_Reg >= startMonth && r.Data_Reg <= endMonth && r.CentroDiCosto_Id == null && r.Registrazione_Tipo_Reg == 0 && (r.Codice_Commessa_Can == "Pulizie Civile" || r.Codice_Commessa_Can == "PULIZIE CIVILE")).GroupBy(r => r.Col_Id).ToList();
            cartellini = new Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>>();
            foreach (var reg in regVsAltro)
            {
                if (SelectedIds.Contains(reg.Key.Value))
                {
                    List<Col> collaboratori = RepoManager.ColRepo.GetAll().Where(c => c.Col_Id == reg.Key && c.DisAbilitazione_Col == false).OrderBy(c => c.Cognome_Col).ToList();
                    foreach (Col col in collaboratori)
                    {
                        if (col.Qualifica_Col != "0")
                        {
                            var regs = RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Col_Id == col.Col_Id
                            && (regv.Data_Reg >= startMonth && regv.Data_Reg <= endMonth)
                            && regv.Registrazione_Tipo_Reg == 0 && (regv.Codice_Commessa_Can == "Pulizie Civile" || regv.Codice_Commessa_Can == "PULIZIE CIVILE"), true);
                            if (regs.Count() > 0)
                            {
                                cartellini.Add(col, TimesheetModuleItem.GenerateCartellinoCentroDiCosto(ExportDate,
                                                                col,
                                                                -999,
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
                        else 
                        {
                            if (col.Scadenza_Patente_Col != null)
                            {
                                if (col.Scadenza_Patente_Col.Value.Between(startMonth, endMonth))
                                {
                                    var regs = RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Col_Id == col.Col_Id
                                    && (regv.Data_Reg >= startMonth && regv.Data_Reg <= col.Scadenza_Patente_Col.Value)
                                    && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att && (regv.Codice_Commessa_Can == "Pulizie Civile" || regv.Codice_Commessa_Can == "PULIZIE CIVILE"), true);
                                    if (regs.Count() > 0)
                                    {
                                        cartellini.Add(col, TimesheetModuleItem.GenerateCartellinoCentroDiCosto(ExportDate,
                                                                col,
                                                                -999,
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
                                else if (col.Scadenza_Patente_Col.Value > endMonth)
                                {
                                    var regs = RepoManager.Reg_VRepo.GetAllQueryable(regv => regv.Col_Id == col.Col_Id
                                    && (regv.Data_Reg >= startMonth && regv.Data_Reg <= endMonth)
                                    && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att && (regv.Codice_Commessa_Can == "Pulizie Civile" || regv.Codice_Commessa_Can == "PULIZIE CIVILE"), true);
                                    if (regs.Count() > 0)
                                    {
                                        cartellini.Add(col, TimesheetModuleItem.GenerateCartellinoCentroDiCosto(ExportDate,
                                                               col,
                                                               -999,
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
                            }
                        }
                    }
                }
            }
            centri.Add("ALTRO", cartellini);


            cartellini = cartellini.OrderBy(c => c.Key.CognomeNome_Col).ToDictionary(c => c.Key, d => d.Value);

            // per prima cosa si procede all'apertura del modello
            ExcelWorkbookGenerateNew();


            String mese = "";
            switch (ExportDate.Month)
            {
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
            if (!_multiPagedExport)
            {
                //Not multipagedExport
                WorksheetCreateNew(mese + " " + ExportDate.Year);
                worksheetIndex++;
            }

            // una volta generato il file, se ci sono elementi da processare
            if (centri.Any())
            {
                foreach (var centro in centri)
                {
                    string tmpCentro = "";
                    foreach (var col in centro.Value.OrderBy(c => c.Key.Cognome_Col))
                    {
                        columnIndex = 1;
                        if (_multiPagedExport && (tmpCentro != centro.Key))
                        {
                            ExcelWorkbook.Workbook.Worksheets.Add(centro.Key);
                            worksheetIndex++;
                            tmpCentro = centro.Key;
                            rowIndex = 1;
                            columnIndex = 1;
                        }
                        CellInsertValue(worksheetIndex, columnIndex, rowIndex, col.Key.CognomeNome_Col, ExcelInsertTypeEnum.Content);
                        RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);

                        WriteTimesheetColHeader(cartellini.Count());

                        WriteColTimesheetOrd(col.Value, col.Key);

                        WriteTotaleHourColOrd(col.Value);

                        rowIndex += 3;
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void WriteTimesheetColHeader(int tot)
        {
            List<DateTime> days = CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1)); //Calcolo i giorni per l'header

            RangeSetFontBold(worksheetIndex, columnIndex, rowIndex, tot + 1, rowIndex);

            int tmp = columnIndex;

            days.ForEach(day =>
            {
                WriteHeaderDayCell(day);

                WriteHeaderDayCellDay(day);

                columnIndex++;
            });

            WriteHeaderTotalCell();

            columnIndex = tmp + 1;

            rowIndex++;
        }

        private void WriteColTimesheetOrd(Dictionary<string, List<TimesheetModuleItem>> cartellini, Col col)
        {
            int totale = 0;
            String lastCant = "";
            bool motivazione = false;
            int giorni = 0;
            List<TimesheetModuleItem> ord = cartellini["justification"].OrderBy(c => c.CantDesc).ToList();
            List<TimesheetModuleItem> mod = cartellini["Modifiche"].OrderBy(c => c.CantDesc).ToList();
            List<TimesheetModuleItem> sing = cartellini["Singole"].OrderBy(c => c.CantDesc).ToList();
            TimesheetModuleItem last = null;
            foreach (var justification in ord)
            {
                int totalDays = 0;
                //controllo se si ripete il cantiere, se si ripete significa che stiamo controllando le motivazioni inerenti a quel cantiere
                if (lastCant.Equals(justification.CantDesc))
                {
                    rowIndex -= 1;
                    motivazione = true;
                    totale = 0;
                }
                else
                {
                    totale = 0;
                    giorni = 0;
                }

                TimesheetModuleItem tmpMod = null;
                TimesheetModuleItem tmpSing = null;
                try
                {
                    tmpMod = mod.First(c => c.CantDesc == justification.CantDesc);
                    tmpSing = sing.First(c => c.CantDesc == justification.CantDesc);
                }
                catch (Exception e) { }
                lastCant = justification.CantDesc;

                var justificationDec = justification.Justification;

                if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                    justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                justificationDec = justificationDec.ToUpper();

                CellInsertValue(worksheetIndex, 1, rowIndex, justification.CantDesc, ExcelInsertTypeEnum.Content);
                RangeSetBorders(worksheetIndex, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(worksheetIndex, 1, rowIndex, 1, rowIndex, 10);
                ColumnsSetWidth(worksheetIndex, 1, 1, 40);

                RangeSetWrapText(worksheetIndex, 1, rowIndex, 1, rowIndex, true);

                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, endMonth))
                {
                    int lastDuration = 0;
                    var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    var timeDuration = TimeSpan.FromHours(baseDuration);
                    double print = 0.0;
                    //nel caso in cui nello stesso giorno ci siano sia motivazione che ore lavorate nello stesso cantiere andiamo a stampare il totale di queste ore
                    try
                    {
                        print = baseDuration;
                    }
                    catch (Exception e)
                    {
                    }
                    double tmpBaseDuration = baseDuration;
                    double tmpPrint = print;
                    baseDuration = baseDuration * 60;
                    print = print * 60;
                    double finale = 0.0;
                    string valueToPrint = "";
                    if (motivazione)
                    {
                        double ieri = (double)last["Day" + day.Day.ToString("00")];
                        valueToPrint = FromTotalMinutesToFormattedTypeKomplett((int)print);
                        if (print == 0)
                        {
                            valueToPrint = "";
                        }
                        else
                        {
                            totalDays++;
                        }
                        double tmpIeri = ieri;
                        ieri = ieri * 60;
                        if ((int)print < 0)
                        {
                            if (ieri * 60 > 0)
                            {
                                finale = tmpIeri + tmpPrint;
                                if (finale % 1 > 0.9)
                                {
                                    double tmp = 1 - finale % 1;
                                    finale += tmp;
                                }
                                else if (finale % 1 > 0.7)
                                {
                                    double tmp = 0.75 - finale % 1;
                                    finale += tmp;
                                }
                                else if (finale % 1 > 0.4)
                                {
                                    double tmp = 0.5 - finale % 1;
                                    finale += tmp;
                                }
                                else if (finale % 1 > 0.2)
                                {
                                    double tmp = 0.25 - finale % 1;
                                    finale += tmp;
                                }
                                valueToPrint = FromTotalMinutesToFormattedTypeKomplett((int)(finale * 60));
                            }
                        }
                        else {
                            if (ieri * 60 > 0)
                            {
                                finale = tmpIeri - tmpPrint;
                                if (finale % 1 > 0.9)
                                {
                                    double tmp = 1 - finale % 1;
                                    finale -= tmp;
                                }
                                else if (finale % 1 > 0.7)
                                {
                                    double tmp = 0.75 - finale % 1;
                                    finale -= tmp;
                                }
                                else if (finale % 1 > 0.4)
                                {
                                    double tmp = 0.5 - finale % 1;
                                    finale -= tmp;
                                }
                                else if (finale % 1 > 0.2)
                                {
                                    double tmp = 0.25 - finale % 1;
                                    finale -= tmp;
                                }
                                valueToPrint = FromTotalMinutesToFormattedTypeKomplett((int)(finale * 60));
                            }
                        }
                    }
                    else
                    {
                        valueToPrint = FromTotalMinutesToFormattedTypeKomplett((int)baseDuration);
                        if (baseDuration == 0)
                        {
                            valueToPrint = "";
                        }
                        else
                        {
                            totalDays++;
                        }
                    }
                    if (finale > 0.0)
                    {
                        totale = totale + (int)(finale * 60);
                    }
                    else {
                        totale = totale + (int)baseDuration;
                    }
                    double stampa = 0.0;
                    if (valueToPrint != "") {
                        stampa = double.Parse(valueToPrint, CultureInfo.InvariantCulture);
                    }
                    
                    RangeSetBorders(worksheetIndex, day.Day + 1, rowIndex, day.Day + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(worksheetIndex, day.Day + 1, rowIndex, day.Day + 1, rowIndex, 8);

                    RangeSetWrapText(worksheetIndex, day.Day + 1, rowIndex, day.Day + 1, rowIndex, true);
                    CellInsertValue(worksheetIndex, day.Day + 1, rowIndex, stampa, ExcelInsertTypeEnum.Content);

                    if ((double)tmpMod["Day" + day.Day.ToString("00")] > 0)
                    {
                        RangeSetFontColor(worksheetIndex, day.Day + 1, rowIndex, day.Day + 1, rowIndex, Color.Red);
                    }

                    if ((double)tmpSing["Day" + day.Day.ToString("00")] > 0)
                    {
                        RangeSetBackgroundColor(worksheetIndex, day.Day + 1, rowIndex, day.Day + 1, rowIndex, Color.OrangeRed, ExcelFillStyle.Solid);
                    }

                    lastDuration = (int)baseDuration;
                }
                giorni = totalDays;
                if (motivazione)
                {
                    giorni = giorni - totalDays;
                    if (justification.TotalMinutes < 0) {
                        totale = last.TotalMinutes + justification.TotalMinutes;
                    }
                }
                string totalHours = FromTotalMinutesToFormattedTypeKomplett(totale);
                double stampaTotale = 0.0;
                if (totalHours != "") {
                    stampaTotale = double.Parse(totalHours, CultureInfo.InvariantCulture);
                }

                string rangeFormula = $"{ColumnIndexToNameConversion(2)}{rowIndex}:" +
                                $"{ColumnIndexToNameConversion(CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 1)}{rowIndex}";

                //Formula automatica per la somma dei valori della riga
                string formula = $"=SUM({rangeFormula})";
                RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, stampaTotale, ExcelInsertTypeEnum.Content);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, formula, ExcelInsertTypeEnum.Formula);
                RangeSetFontSize(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, 8);

                rowIndex = rowIndex + 1;

                if (giorno == 2)
                {
                    giorno += CommonService.GetDatesFromPeriod(startMonth, endMonth).Count;
                }
                motivazione = false;
                last = justification;
            }

        }

        private void WriteTotaleHourColOrd(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            String lastCant = "";
            List<TimesheetModuleItem> tot = cartellini["totale"].OrderBy(c => c.CantDesc).ToList();

            RangeSetBorders(worksheetIndex, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            CellInsertValue(worksheetIndex, 1, rowIndex, "Totale", ExcelInsertTypeEnum.Content);
            RangeSetFontSize(worksheetIndex, 1, rowIndex, 1, rowIndex, 8);
            foreach (var justification in tot)
            {
                lastCant = justification.CantDesc;
                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, endMonth))
                {
                    if (day.Day == 1)
                    {
                        RangeSetBackgroundColor(worksheetIndex, day.Day, rowIndex, day.Day, rowIndex, Color.SkyBlue, fillStyle);
                    }
                    bool errore = false;
                    double baseDuration = 0.0;
                    if (errore)
                    {
                        baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    }
                    else
                    {
                        baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    }

                    var timeDuration = TimeSpan.FromHours(baseDuration);

                    string valueToPrint = FromTotalMinutesToFormattedTypeKomplett((int)timeDuration.TotalMinutes);
                    if (baseDuration == 0.0)
                    {
                        valueToPrint = "";
                    }
                    double stampa = 0.0;
                    if (valueToPrint != "")
                    {
                        stampa = double.Parse(valueToPrint, CultureInfo.InvariantCulture);
                    }

                    string rangeFormula1 = $"{ColumnIndexToNameConversion(day.Day + 1)}{rowIndex - (cartellini["justification"].Count)}:" +
                                $"{ColumnIndexToNameConversion(day.Day + 1)}{rowIndex - 1}";

                    //Formula automatica per la somma dei valori della riga
                    string formula1 = $"=SUM({rangeFormula1})";

                    RangeSetBorders(worksheetIndex, day.Day + 1, rowIndex, day.Day + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(worksheetIndex, day.Day + 1, rowIndex, day.Day + 1, rowIndex, 8);
                    CellInsertValue(worksheetIndex, day.Day + 1, rowIndex, stampa, ExcelInsertTypeEnum.Content);
                    CellInsertValue(worksheetIndex, day.Day + 1, rowIndex, formula1, ExcelInsertTypeEnum.Formula);
                    RangeSetBackgroundColor(worksheetIndex, day.Day + 1, rowIndex, day.Day + 1, rowIndex, Color.SkyBlue, fillStyle);
                }
                string totalHours = FromTotalMinutesToFormattedTypeKomplett(justification.TotalMinutes);
                double stampaTotale = 0.0;
                if (totalHours != "")
                {
                    stampaTotale = double.Parse(totalHours, CultureInfo.InvariantCulture);
                }

                string rangeFormula = $"{ColumnIndexToNameConversion(2)}{rowIndex}:" +
                                $"{ColumnIndexToNameConversion(CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 1)}{rowIndex}";

                //Formula automatica per la somma dei valori della riga
                string formula = $"=SUM({rangeFormula})";

                RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, stampaTotale, ExcelInsertTypeEnum.Content);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, formula, ExcelInsertTypeEnum.Formula);
                RangeSetFontSize(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, 8);
                RangeSetBackgroundColor(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, Color.SkyBlue, fillStyle);
                ColumnsSetWidth(worksheetIndex, rowIndex, rowIndex, 6);
                RangeSetWrapText(worksheetIndex, rowIndex, 2, rowIndex, 2, true);
            }
        }

        #region Header

        private void WriteHeaderDayCell(DateTime date)
        {
            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, "0");
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.LightGray, fillStyle);
            RangeSetWrapText(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, true);
            ColumnsSetWidth(worksheetIndex, columnIndex + 1, columnIndex + 1, 6);
            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex);

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);
            }
        }
        private void WriteHeaderDayCellDay(DateTime date)
        {
            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, "0");
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 2, rowIndex, Color.LightGray, fillStyle);
            RangeSetWrapText(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, true);
            ColumnsSetWidth(worksheetIndex, columnIndex + 1, columnIndex + 1, 6);
            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex);
            RangeSetFontSize(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, 12);

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
            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex);

        }

        #endregion

        #endregion
    }
}
