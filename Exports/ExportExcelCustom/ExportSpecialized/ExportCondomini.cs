
using Common;
using Domain;
using Business.Repository;
using Business.BusinessExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Drawing;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using DevExpress.XtraSpreadsheet.Model;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using OfficeOpenXml.Style;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;
using System.Windows.Forms;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using static System.Net.WebRequestMethods;
using System.Data.SqlTypes;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /// <summary>
    /// Classe utilizzata per la gestione dell'export per fatture
    /// </summary>
    public class ExportCondomini : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
    {
        #region Public Properties
        /// <summary>
        /// Recupera o imposta il percorso del modello EXCEL
        /// </summary>   
        /// <value>
        /// Percorso modello EXCEL su disco.
        /// </value>
        public string ExcelModelFilePath { get; set; }

        /// <summary>
        /// Recupera o imposta il periodo (mese/anno) di riferimento dell'export.
        /// </summary>
        /// <value>
        /// Periodo (mese/anno) di riferimento dell'export
        /// </value>
        public DateTime ExportPeriod { get; set; }

        public int rowIndex = 1;

        public int columnIndex = 1;

        private OfficeOpenXml.Style.ExcelBorderStyle borderStyle = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        private OfficeOpenXml.Style.ExcelFillStyle fillStyle = OfficeOpenXml.Style.ExcelFillStyle.Solid;

        private Color borderColor = Color.Black;

        #endregion

        #region Public Methods

        /// <summary>
        /// Esegue la preparazione dell'export con i dati passati come parametro
        /// </summary>
        /// <param name="entitiesToExport">Elenco delle entità da esportare</param>
        public override void LaunchExport(IQueryable<Reg_V> entitiesToExport)
        {
            //List<CentroDiCosto> centro = RepoManager.CentroDiCostoRepo.GetAllQueryable(c => c.Descrizione == "PULIZIE CIVILI").ToList();
            int nMesi = 1;
            if (ExportPeriod.Month >= 4)
            {
                nMesi = 4;
            }
            else {
                nMesi = ExportPeriod.Month;
            }
            DateTime minDate = new DateTime(ExportPeriod.Year, ExportPeriod.Month - (nMesi - 1), 1);
            DateTime exportDate = new DateTime(2026, 04, 01);
            DateTime monthLastDate = CommonService.GetLastMonthDay(ExportPeriod);
            DateTime maxDate = new DateTime(monthLastDate.Year, monthLastDate.Month, monthLastDate.Day, 23, 59, 59);
            ExcelWorkbookGenerateNew(ExcelModelFilePath);
            List<string> codCond = RepoManager.Tab_DecodRepo.GetAllQueryable(td => td.Campo1_Tab == "CONDOMINIO").Select(td => td.Chiave_Tab).ToList();
            List<int> cantIds = RepoManager.CantRepo.GetAllQueryable(c => c.DisAbilitazione_Can == false && codCond.Contains(c.Tipo_Interv_Can)).Select(c => c.Cant_Id).ToList();
            List<DateTime> monthDays = CommonService.GetDatesFromPeriod(CommonService.GetFirstMonthDay(ExportPeriod), CommonService.GetLastMonthDay(ExportPeriod));
            List<Reg_V> regVs = RepoManager.Reg_VRepo.GetAllQueryable(r => cantIds.Contains(r.Cant_Id.Value) && r.Qualifica_Col != "0" && r.Data_Ora_Fis_E >= minDate && r.Data_Ora_Fis_E <= maxDate && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 10) ).OrderBy(reg => reg.Cant_Desc).ToList();
            var exportRegVs = regVs.GroupBy(c => c.Cant_Id);
            var exportCliRegVs = regVs.GroupBy(c => c.Cli_Id);
            //ordino le ore in base alla ora della registrazione e le reggruppo per i cantieri
            List<Cant> cantieri = RepoManager.CantRepo.GetAllQueryable().Where(c => c.DisAbilitazione_Can == false).ToList();
            rowIndex = 3;
            string lastCant = "";
            int lastInterventi = 0;
            TimeSpan lastTotale = new TimeSpan();
            //vado a fare un foreach per ogni cantiere
            foreach (var regs in exportRegVs)
            {
                int totaleInterventi = 0;
                TimeSpan totaliMensili = new TimeSpan();
                if (regs.Key != null) {
                    List<Cant> currentCant = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regs.Key).ToList();
                    bool elabora = true;
                    if (currentCant.First().Descrizione_Can.Contains("-")) 
                    {
                        string[] split = currentCant.First().Descrizione_Can.Split('-');
                        if (split[0].Trim() == lastCant) 
                        {
                            elabora = false;
                        }
                    }
                    if (elabora)
                    {
                        columnIndex = 1;

                        int cliIds = currentCant.First().Cli_Id.Value;
                        Cli clienteId = RepoManager.CliRepo.GetAllQueryable(c => c.Cli_Id == cliIds).FirstOrDefault();

                        CellInsertValue(1, columnIndex, rowIndex, clienteId.Cognome_Cli + " ", ExcelInsertTypeEnum.Content);
                        RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);
                        RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);
                        ColumnsSetWidth(1, columnIndex, columnIndex, 30);

                        columnIndex++;

                        CellInsertValue(1, columnIndex, rowIndex, currentCant.First().Descrizione_Can + " ", ExcelInsertTypeEnum.Content);
                        RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);
                        RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);
                        ColumnsSetWidth(1, columnIndex, columnIndex, 58);

                        columnIndex++;

                        CellInsertValue(1, columnIndex, rowIndex, "CONDOMINI", ExcelInsertTypeEnum.Content);
                        RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);
                        RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);

                        columnIndex++;

                        string tipoInt = currentCant.First().Tipo_Interv_Can;
                        Tab_Decod tInt = RepoManager.Tab_DecodRepo.GetAllQueryable(td => td.Nome_Tab == "TIPO_INTERVENTO" && td.Chiave_Tab == tipoInt).FirstOrDefault();
                        CellInsertValue(1, columnIndex, rowIndex, "" + tInt.Decodifica_Tab, ExcelInsertTypeEnum.Content);
                        RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                        RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);
                        RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);

                        columnIndex++;

                        for (int i = nMesi - 1; i >= 0; i--)
                        {
                            string mese = "";
                            switch (ExportPeriod.Month - i)
                            {
                                case 1:
                                    mese = "GENNAIO";
                                    break;
                                case 2:
                                    mese = "FEBBRAIO";
                                    break;
                                case 3:
                                    mese = "MARZO";
                                    break;
                                case 4:
                                    mese = "APRILE";
                                    break;
                                case 5:
                                    mese = "MAGGIO";
                                    break;
                                case 6:
                                    mese = "GIUGNO";
                                    break;
                                case 7:
                                    mese = "LUGLIO";
                                    break;
                                case 8:
                                    mese = "AGOSTO";
                                    break;
                                case 9:
                                    mese = "SETTEMBRE";
                                    break;
                                case 10:
                                    mese = "OTTOBRE";
                                    break;
                                case 11:
                                    mese = "NOVEMBRE";
                                    break;
                                case 12:
                                    mese = "DICEMBRE";
                                    break;
                            }

                            CellInsertValue(1, columnIndex, 1, "" + mese, ExcelInsertTypeEnum.Content);
                            RangeSetBorders(1, columnIndex, 1, columnIndex + 1, 1, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            RangeSetFontSize(1, columnIndex, 1, columnIndex + 1, 1, 11);
                            RangeSetWrapText(1, columnIndex, 1, columnIndex + 1, 1, true);
                            RangeSetBackgroundColor(1, columnIndex, 1, columnIndex + 1, 1, Color.Yellow, ExcelFillStyle.Solid);
                            RangeSetFontColor(1, columnIndex, 1, columnIndex + 1, 1, Color.Red);
                            RangeSetFontBold(1, columnIndex + 1, 1, columnIndex + 1, 1);
                            RangeUnion(1, columnIndex, 1, columnIndex + 1, 1);

                            CellInsertValue(1, columnIndex, 2, "N°INTERVENTI", ExcelInsertTypeEnum.Content);
                            RangeSetBorders(1, columnIndex, 2, columnIndex, 2, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            RangeSetFontSize(1, columnIndex, 2, columnIndex, 2, 11);
                            RangeSetWrapText(1, columnIndex, 2, columnIndex + 1, 2, true);
                            RangeSetBackgroundColor(1, columnIndex, 2, columnIndex + 1, 2, Color.Yellow, ExcelFillStyle.Solid);
                            RangeSetFontColor(1, columnIndex, 2, columnIndex + 1, 2, Color.Red);
                            RangeSetFontBold(1, columnIndex, 1, columnIndex + 1, 2);
                            ColumnsSetWidth(1, columnIndex, columnIndex, 18);

                            CellInsertValue(1, columnIndex + 1, 2, "N°ORE", ExcelInsertTypeEnum.Content);
                            RangeSetBorders(1, columnIndex + 1, 2, columnIndex + 1, 2, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                            RangeSetFontSize(1, columnIndex + 1, 2, columnIndex + 1, 2, 11);
                            ColumnsSetWidth(1, columnIndex + 1, columnIndex + 1, 12);

                            minDate = new DateTime(ExportPeriod.Year, ExportPeriod.Month - i, 1);
                            monthLastDate = CommonService.GetLastMonthDay(minDate);
                            maxDate = new DateTime(monthLastDate.Year, monthLastDate.Month, monthLastDate.Day, 23, 59, 59);

                            int interventi = 0;
                            TimeSpan totaleMensile = new TimeSpan();
                            monthDays = CommonService.GetDatesFromPeriod(CommonService.GetFirstMonthDay(minDate), CommonService.GetLastMonthDay(maxDate));
                            foreach (DateTime day in monthDays)
                            {
                                //vado a fare il ciclo per ogni giorno e recupero le timbrature solo della giornata corrente
                                DateTime tomorrow = day.AddDays(1);
                                var dayReg = new List<Reg_V>();
                                if (minDate < exportDate)
                                    dayReg = regs.Where(r => (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_U <= tomorrow) || (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_E < tomorrow && r.Registrazione_Tipo_Reg == 10)).ToList();
                                else
                                {
                                    string descrizioneCant = currentCant.First().Descrizione_Can;
                                    Cli cliente = RepoManager.CliRepo.GetAllQueryable(c => c.Cognome_Cli == descrizioneCant).FirstOrDefault();
                                    if (cliente != default(Cli))
                                    {
                                        var cliRegs = exportCliRegVs.Where(c => c.Key == cliente.Cli_Id).FirstOrDefault();
                                        if (cliRegs != null)
                                        {
                                            dayReg = cliRegs.Where(r => (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_U <= tomorrow) || (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_E < tomorrow && r.Registrazione_Tipo_Reg == 10)).ToList();
                                        }
                                    }
                                    else
                                    {
                                        if (currentCant.First().Cli_Id != null)
                                        {
                                            int cliId = currentCant.First().Cli_Id.Value;
                                            var cliRegs = exportCliRegVs.Where(c => c.Key == cliId).FirstOrDefault();
                                            if (cliRegs != null)
                                            {
                                                dayReg = regs.Where(r => (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_U <= tomorrow) || (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_E < tomorrow && r.Registrazione_Tipo_Reg == 10)).ToList();
                                                //dayReg = cliRegs.Where(r => (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_U <= tomorrow) || (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_E < tomorrow && r.Registrazione_Tipo_Reg == 10)).ToList();
                                            }
                                        }
                                    }
                                }

                                double daySum = 0;
                                string tot = "-- --";
                                foreach (Reg_V reg in dayReg)
                                {
                                    if (reg.Durata_Fis != null)
                                    {
                                        daySum += reg.Durata_Fis.Value;
                                        if (reg.Registrazione_Tipo_Reg == 0) 
                                            interventi++;
                                    }
                                }
                                if (daySum > 0)
                                {
                                    daySum = CommonService.ConvertDaySum((int)daySum);
                                    TimeSpan totalDuration = TimeSpan.FromMinutes((int)daySum);
                                    totaleMensile = totaleMensile + totalDuration;
                                    tot = String.Format("{0}.{1}", (totalDuration.Days * 24) + totalDuration.Hours, Math.Abs(totalDuration.Minutes).ToString("00"));
                                }
                                if (CommonService.GetLastMonthDay(minDate) == day)
                                {
                                    //tot = String.Format("{0}.{1}", (totaleMensile.Days * 24) + totaleMensile.Hours, Math.Abs(totaleMensile.Minutes).ToString("00"));
                                    tot = String.Format("{0}.{1:00}",
                                        (totaleMensile.Days * 24) + totaleMensile.Hours,
                                        (int)(Math.Round(Math.Abs(totaleMensile.Minutes) * 100D / 60D))
                                    );
                                    RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                    RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);

                                    RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);
                                    CellInsertValue(1, columnIndex, rowIndex, interventi, ExcelInsertTypeEnum.Content);

                                    columnIndex++;

                                    RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                    RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);

                                    RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);
                                    CellInsertValue(1, columnIndex, rowIndex, tot, ExcelInsertTypeEnum.Content);

                                    columnIndex++;

                                    totaleInterventi += interventi;
                                    totaliMensili += totaleMensile;
                                }
                            }
                            if (i == 0)
                            {
                                CellInsertValue(1, columnIndex, 2, "TOT INTERVENTI", ExcelInsertTypeEnum.Content);
                                RangeSetBorders(1, columnIndex, 2, columnIndex, 2, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                RangeSetFontSize(1, columnIndex, 2, columnIndex, 2, 11);
                                RangeSetWrapText(1, columnIndex, 2, columnIndex + 1, 2, true);
                                RangeSetBackgroundColor(1, columnIndex, 2, columnIndex + 1, 2, Color.Yellow, ExcelFillStyle.Solid);
                                RangeSetFontColor(1, columnIndex, 2, columnIndex + 1, 2, Color.Red);
                                RangeSetFontBold(1, columnIndex, 1, columnIndex + 1, 2);
                                ColumnsSetWidth(1, columnIndex, columnIndex, 18);

                                CellInsertValue(1, columnIndex + 1, 2, "TOT ORE", ExcelInsertTypeEnum.Content);
                                RangeSetBorders(1, columnIndex + 1, 2, columnIndex + 1, 2, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                RangeSetFontSize(1, columnIndex + 1, 2, columnIndex + 1, 2, 11);
                                ColumnsSetWidth(1, columnIndex + 1, columnIndex + 1, 12);
                                string totale = String.Format("{0}.{1}", (totaliMensili.Days * 24) + totaliMensili.Hours, Math.Abs(totaliMensili.Minutes).ToString("00"));
                                totale = String.Format("{0}.{1:00}",
                                        (totaliMensili.Days * 24) + totaliMensili.Hours,
                                        (int)(Math.Round(Math.Abs(totaliMensili.Minutes) * 100D / 60D))
                                    );

                                if (ExportPeriod.Month >= 4)
                                {
                                    nMesi = 4;
                                }
                                else
                                {
                                    nMesi = ExportPeriod.Month;
                                }

                                int j = 2;

                                string rangeFormulaInt = $"{ColumnIndexToNameConversion(columnIndex - 2)}{rowIndex}";
                                string rangeFormula = $"{ColumnIndexToNameConversion(columnIndex - 1)}{rowIndex}";

                                while (j <= nMesi)
                                {
                                    rangeFormulaInt += $",{ColumnIndexToNameConversion(columnIndex - (j * 2))}{rowIndex}";
                                    rangeFormula += $",{ColumnIndexToNameConversion(columnIndex - (j * 2) + 1)}{rowIndex}";
                                    j++;
                                }

                                //Formula automatica per la somma dei valori della riga
                                string formulaInt = $"=SUM({rangeFormulaInt})";
                                RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);

                                RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);
                                CellInsertValue(1, columnIndex, rowIndex, formulaInt, ExcelInsertTypeEnum.Formula);

                                columnIndex++;    

                                //Formula automatica per la somma dei valori della riga
                                string formula = $"=SUM({rangeFormula})";

                                RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);
                                
                                RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);
                                CellInsertValue(1, columnIndex, rowIndex, formula, ExcelInsertTypeEnum.Formula);

                                columnIndex++;

                                lastInterventi = totaleInterventi;
                                lastTotale = totaliMensili;
                            }
                        }
                        rowIndex++;
                        lastCant = currentCant.First().Descrizione_Can;
                    }
                    else 
                    {
                        columnIndex = 3;
                        rowIndex--;
                        for (int i = nMesi - 1; i >= 0; i--)
                        {
                            minDate = new DateTime(ExportPeriod.Year, ExportPeriod.Month - i, 1);
                            monthLastDate = CommonService.GetLastMonthDay(minDate);
                            maxDate = new DateTime(monthLastDate.Year, monthLastDate.Month, monthLastDate.Day, 23, 59, 59);

                            int interventi = 0;
                            TimeSpan totaleMensile = new TimeSpan();
                            monthDays = CommonService.GetDatesFromPeriod(CommonService.GetFirstMonthDay(minDate), CommonService.GetLastMonthDay(maxDate));
                            if (minDate >= exportDate)
                            {
                                foreach (DateTime day in monthDays)
                                {
                                    //vado a fare il ciclo per ogni giorno e recupero le timbrature solo della giornata corrente
                                    DateTime tomorrow = day.AddDays(1);
                                    var dayReg = new List<Reg_V>();
                                    if (minDate < exportDate)
                                        dayReg = regs.Where(r => (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_U <= tomorrow) || (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_E < tomorrow && r.Registrazione_Tipo_Reg == 10)).ToList();
                                    else
                                    {
                                        string descrizioneCant = currentCant.First().Descrizione_Can;
                                        Cli cliente = RepoManager.CliRepo.GetAllQueryable(c => c.Cognome_Cli == descrizioneCant).FirstOrDefault();
                                        if (cliente != default(Cli))
                                        {
                                            var cliRegs = exportCliRegVs.Where(c => c.Key == cliente.Cli_Id).FirstOrDefault();
                                            if (cliRegs != null)
                                            {
                                                dayReg = cliRegs.Where(r => (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_U <= tomorrow) || (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_E < tomorrow && r.Registrazione_Tipo_Reg == 10)).ToList();
                                            }
                                        }
                                        else
                                        {
                                            if (currentCant.First().Cli_Id != null)
                                            {
                                                int cliId = currentCant.First().Cli_Id.Value;
                                                var cliRegs = exportCliRegVs.Where(c => c.Key == cliId).FirstOrDefault();
                                                if (cliRegs != null)
                                                {
                                                    dayReg = cliRegs.Where(r => (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_U <= tomorrow) || (r.Data_Ora_Fis_E >= day && r.Data_Ora_Fis_E < tomorrow && r.Registrazione_Tipo_Reg == 10)).ToList();
                                                }
                                            }
                                        }
                                    }

                                    double daySum = 0;
                                    string tot = "-- --";
                                    foreach (Reg_V reg in dayReg)
                                    {
                                        if (reg.Durata_Fig != null)
                                        {
                                            daySum += reg.Durata_Fig.Value;
                                        }
                                    }
                                    if (daySum > 0)
                                    {
                                        interventi++;
                                        TimeSpan totalDuration = TimeSpan.FromMinutes((int)daySum);
                                        totaleMensile = totaleMensile + totalDuration;
                                        tot = String.Format("{0}.{1}", (totalDuration.Days * 24) + totalDuration.Hours, Math.Abs(totalDuration.Minutes).ToString("00"));
                                    }
                                    if (CommonService.GetLastMonthDay(minDate) == day)
                                    {
                                        tot = String.Format("{0}.{1}", (totaleMensile.Days * 24) + totaleMensile.Hours, Math.Abs(totaleMensile.Minutes).ToString("00"));
                                        RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                        RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);

                                        RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);
                                        CellInsertValue(1, columnIndex, rowIndex, interventi, ExcelInsertTypeEnum.Content);

                                        columnIndex++;

                                        RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                        RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);

                                        RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);
                                        CellInsertValue(1, columnIndex, rowIndex, tot, ExcelInsertTypeEnum.Content);

                                        columnIndex++;

                                        totaleInterventi += interventi;
                                        totaliMensili += totaleMensile;
                                    }
                                }

                                if (i == 0)
                                {
                                    totaleInterventi += lastInterventi;
                                    totaliMensili += lastTotale;
                                    CellInsertValue(1, columnIndex, 2, "TOT INTERVENTI", ExcelInsertTypeEnum.Content);
                                    RangeSetBorders(1, columnIndex, 2, columnIndex, 2, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                    RangeSetFontSize(1, columnIndex, 2, columnIndex, 2, 11);
                                    RangeSetWrapText(1, columnIndex, 2, columnIndex + 1, 2, true);
                                    RangeSetBackgroundColor(1, columnIndex, 2, columnIndex + 1, 2, Color.Yellow, ExcelFillStyle.Solid);
                                    RangeSetFontColor(1, columnIndex, 2, columnIndex + 1, 2, Color.Red);
                                    RangeSetFontBold(1, columnIndex, 1, columnIndex + 1, 2);
                                    ColumnsSetWidth(1, columnIndex, columnIndex, 18);

                                    CellInsertValue(1, columnIndex + 1, 2, "TOT ORE", ExcelInsertTypeEnum.Content);
                                    RangeSetBorders(1, columnIndex + 1, 2, columnIndex + 1, 2, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                    RangeSetFontSize(1, columnIndex + 1, 2, columnIndex + 1, 2, 11);
                                    ColumnsSetWidth(1, columnIndex + 1, columnIndex + 1, 12);
                                    string totale = String.Format("{0}.{1}", (totaliMensili.Days * 24) + totaliMensili.Hours, Math.Abs(totaliMensili.Minutes).ToString("00"));
                                    RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                    RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);

                                    RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);
                                    CellInsertValue(1, columnIndex, rowIndex, totaleInterventi, ExcelInsertTypeEnum.Content);

                                    columnIndex++;

                                    RangeSetBorders(1, columnIndex, rowIndex, columnIndex, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                                    RangeSetFontSize(1, columnIndex, rowIndex, columnIndex, rowIndex, 11);

                                    RangeSetWrapText(1, columnIndex, rowIndex, columnIndex, rowIndex, true);
                                    CellInsertValue(1, columnIndex, rowIndex, totale, ExcelInsertTypeEnum.Content);

                                    columnIndex++;

                                    lastInterventi = 0;
                                    lastTotale = new TimeSpan();
                                }
                            }
                            else 
                            {
                                columnIndex += 2;
                            }
                        }
                        rowIndex++;
                    }
                }
                
            }
            

            ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

            ExcelWorkbookDispose();
        }

        private void WriteTimesheetColHeader()
        {
            List<DateTime> days = CommonService.GetDatesFromPeriod(ExportPeriod, ExportPeriod.AddMonths(1).AddDays(-1)); //Calcolo i giorni per l'header

            RangeSetFontBold(1, columnIndex + 1, rowIndex, days.Count + 3, rowIndex);

            days.ForEach(day =>
            {
                //WriteHeaderDayCell(day);

                WriteHeaderDayCellDay(day);

                rowIndex++;
            });

            WriteHeaderTotalCell();

            rowIndex++;

            WriteHeaderTotalDaysCell();

            rowIndex++;

            //WriteHeaderTotalFest();

            //columnIndex++;

            //WriteHeaderTotalFer();

            columnIndex = 2;

            rowIndex++;
        }

        private void WriteHeaderDayCellDay(DateTime date)
        {
            String giorno = "";
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    giorno = "L " + date.Day;
                    break;
                case DayOfWeek.Tuesday:
                    giorno = "MA " + date.Day;
                    break;
                case DayOfWeek.Wednesday:
                    giorno = "ME " + date.Day;
                    break;
                case DayOfWeek.Thursday:
                    giorno = "G " + date.Day;
                    break;
                case DayOfWeek.Friday:
                    giorno = "V " + date.Day;
                    break;
                case DayOfWeek.Saturday:
                    giorno = "S " + date.Day;
                    break;
                case DayOfWeek.Sunday:
                    giorno = "D " + date.Day;
                    break;
            }

            CellInsertValue(1, rowIndex + 1, columnIndex, giorno, ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, "0");
            //RangeSetBackgroundColor(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day, Color.LightGray, fillStyle);
            RangeSetFontSize(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, 12);
            RangeSetWrapText(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, true);
            ColumnsSetWidth(1, rowIndex + 1, rowIndex + 1, 8);

            RangeSetFontBold(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1);
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex, Color.Red);
            }
        }

        private void WriteHeaderTotalCell()
        {

            CellInsertValue(1, rowIndex + 1, columnIndex, "Tot", ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex);

        }


        private void WriteHeaderDayCell(DateTime date)
        {
            CellInsertValue(1, rowIndex, columnIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex, columnIndex, rowIndex, date.Day, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(1, rowIndex, columnIndex, rowIndex, date.Day, "0");
            RangeSetBackgroundColor(1, rowIndex, columnIndex, rowIndex, date.Day, Color.LightGray, fillStyle);
            RangeSetWrapText(1, rowIndex, columnIndex, rowIndex, date.Day, true);
            ColumnsSetWidth(1, rowIndex, rowIndex, 6);
            RangeSetFontBold(1, rowIndex, columnIndex, rowIndex, date.Day);

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(1, rowIndex, columnIndex, rowIndex, columnIndex, Color.Red);
            }
        }

        private void WriteHeaderTotalDaysCell()
        {
            CellInsertValue(1, rowIndex + 1, columnIndex, "Tot.G", ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);

        }
        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="selectedColIds">L'elenco degli id collaboratore selezionati per l'export.</param>
        /// <param name="selectedCantIds">L'elenco degli id cantiere selezionati per l'export.</param>
        /// <param name="selectedCliIds">L'elenco degli id cliente selezionati per l'export.</param>
        public override void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds, IEnumerable<int> selectedCliIds)
        {
            //    if (selectedCliIds.Any())
            //    {
            //        DateTime firstMonthDate = CommonService.GetFirstMonthDay(ExportPeriod);
            //        DateTime lastMonthDate = CommonService.GetLastMonthDay(ExportPeriod);
            //        Col col;

            //        ExcelWorkbookGenerateNew(ExcelModelFilePath);

            //        foreach (int cliId in selectedCliIds)
            //        {
            //            foreach (int colid in selectedColIds)
            //            {
            //                IEnumerable<Reg_V> colRegVs = GetProcessableColRegVs(colid, firstMonthDate, lastMonthDate); // modificare con ID collaboratore (where the fuck i can find it?)

            //                if (colRegVs.Any())
            //                {
            //                    col = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colid);

            //                }
            //            }
            //        }
            //    }
            throw new NotImplementedException();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Recupera tutte le registrazioni processabili del collaboratore X per il periodo specificato.
        /// </summary>
        /// <param name="colId">L'identificativo del collaboratore per cui effettuare la ricerca.</param>
        /// <param name="startPeriod">La data di inizio del periodo in cui effettuare la ricerca.</param>
        /// <param name="endPeriod">La data di fine del periodo in cui effettuare la ricerca.</param>
        /// <returns>L'elenco delle registrazioni da processare per il collaboratore e il periodo scelto.</returns>
        private IEnumerable<Reg_V> GetProcessableColRegVs(int colId, DateTime startPeriod, DateTime endPeriod)
        {
            return RepoManager.Reg_VRepo.Find(regv => regv.Col_Id == colId && regv.Data_Reg >= startPeriod && regv.Data_Reg <= endPeriod);
        }

        #endregion
    }
}
