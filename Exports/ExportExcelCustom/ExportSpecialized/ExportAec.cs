using Business.Repository;
using Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.SqlServer;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    public class ExportAec : ExcelToolBox
    {
        private int rowIndex = 7;

        private OfficeOpenXml.Style.ExcelBorderStyle borderStyle = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        private OfficeOpenXml.Style.ExcelFillStyle fillStyle = OfficeOpenXml.Style.ExcelFillStyle.Solid;

        private Color borderColor = Color.Black;

        private int WorkSheetIndex = 1;

        private string colDecod = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "RAGGRUPPAMENTO_1", "1").Chiave_Tab;
        private string mezziDecod = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "RAGGRUPPAMENTO_1", "2").Chiave_Tab;
        private string respDecod = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "RAGGRUPPAMENTO_1", "3").Chiave_Tab;
        private string fornDecod = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "RAGGRUPPAMENTO_1", "4").Chiave_Tab;


        private List<Col> _mezzi;
        private List<Col> _collaboratori;
        private List<Col> _fornitori;

        private IEnumerable<Damage> _responsabili;
        private IEnumerable<Damage> _segnalazioni;

        private Dictionary<Col, IEnumerable<Reg_V>> _regsRow;

        public override void LaunchExport()
        {
            ToDate = ToDate.AddDays(1);


            IEnumerable<DateTime> dates = Common.CommonService.GetDatesFromPeriod(FromDate, ToDate.AddDays(-1));


            //Eseguo una join tra RegV e Col per accoppiare la navigation property Col che altrimenti non sarebbe disponibile 
            //tramite la vista 
            _regsRow = RepoManager.ColRepo.DbSet
                .GroupJoin(
                RepoManager.Reg_VRepo.DbSet.Where(r => r.Data_Reg >= FromDate && r.Data_Reg < ToDate),
                col => col.Col_Id,
                reg => reg.Col_Id,
                (col, reg) => new
                {
                    Col = col,
                    Regs = reg
                }).ToDictionary(c => c.Col, r => r.Regs);


            _segnalazioni = RepoManager.DamageRepo.DbSet.Include("Col").Where(d => d.Data_Ora_Damage >= FromDate && d.Data_Ora_Damage < ToDate && d.Col_Id != null).ToList();

            IEnumerable<Reg> regs = RepoManager.RegRepo.DbSet.Include("Col").Where(r => r.Registrazione_Data_Ora_Fis_Reg >= FromDate && r.Registrazione_Data_Ora_Fis_Reg < ToDate && r.Col_Id != null).ToList();

            _responsabili = _segnalazioni.Where(d => d.Col.Raggruppamento1_Col == respDecod).ToList();

            foreach (DateTime date in dates)
            {
                _mezzi = GetColsByDayAndRole(date, mezziDecod);

                _collaboratori = GetColsByDayAndRole(date, colDecod);

                _fornitori = GetColsByDayAndRole(date, fornDecod);

                ExcelWorkbook.Workbook.Worksheets.Add(date.ToShortDateString(), WorksheetLoadFromModel("ExportAec.xlsx", 1));

                WriteWorksheetHeader(date);

                WriteCollaboratoriHeader();

                WriteCollaboratori(date);

                WriteMezziHeader();

                WriteMezzi(date);

                WriteFornitoriHeader();

                WriteFornitori(date);

                WriteResponsabiliHeader();

                WriteResponsabili(date);

                WorkSheetIndex++;

                rowIndex = 7;
            }
        }


        #region DATA FUNCTIONS
        private void WriteCollaboratori(DateTime currentDate)
        {

            foreach (Col col in _collaboratori)
            {
                CellInsertValue(WorkSheetIndex, 1, rowIndex, col.CognomeNome_Col, Common.ExcelInsertTypeEnum.Content);

                RangeUnion(WorkSheetIndex, 2, rowIndex, 7, rowIndex);
                
                CellInsertValue(WorkSheetIndex, 2, rowIndex, String.Join(", ", _segnalazioni.Where(s => s.Col_Id == col.Col_Id && s.Data_Ora_Damage.Date == currentDate).Select(d => d.Note_Damage)), Common.ExcelInsertTypeEnum.Content);

                rowIndex++;
            }

            rowIndex += 2;

        }

        private void WriteMezzi(DateTime currentDate)
        {
            foreach (Col mezzo in _mezzi)
            {
                CellInsertValue(WorkSheetIndex, 1, rowIndex, mezzo.CognomeNome_Col, Common.ExcelInsertTypeEnum.Content);

                RangeUnion(WorkSheetIndex, 2, rowIndex, 10, rowIndex);
                CellInsertValue(WorkSheetIndex, 2, rowIndex, _regsRow.Where(c => c.Key.Col_Id == mezzo.Col_Id && c.Value.Any(d => (DateTime)d.Data_Reg == currentDate)).Select(c => c.Value.First()).Select(c => c.Note_Reg), Common.ExcelInsertTypeEnum.Content);

                rowIndex++;
            }

            rowIndex += 2;
        }

        private void WriteFornitori(DateTime currentDate)
        {
            foreach (Col fornitore in _fornitori)
            {
                CellInsertValue(WorkSheetIndex, 1, rowIndex, fornitore.CognomeNome_Col, Common.ExcelInsertTypeEnum.Content);

                RangeUnion(WorkSheetIndex, 2, rowIndex, 10, rowIndex);
                rowIndex++;
            }

            rowIndex += 2;
        }

        private void WriteResponsabili(DateTime currentDate)
        {
            foreach (Damage segnalazione in _responsabili.Where(d => d.Data_Ora_Damage >= currentDate && d.Data_Ora_Damage < currentDate.AddDays(1)).ToList())
            {
                CellInsertValue(WorkSheetIndex, 1, rowIndex, segnalazione.Col.CognomeNome_Col, Common.ExcelInsertTypeEnum.Content);

                Tab_Decod qualifica = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "QUALIFICHE_COL", segnalazione.Col.Qualifica_Col);
                CellInsertValue(WorkSheetIndex, 2, rowIndex, qualifica != null ? qualifica.Decodifica_Tab : "", Common.ExcelInsertTypeEnum.Content);

                RangeUnion(WorkSheetIndex, 3, rowIndex, 7, rowIndex);
                CellInsertValue(WorkSheetIndex, 3, rowIndex, segnalazione.Note_Damage, Common.ExcelInsertTypeEnum.Content);

                CellInsertValue(WorkSheetIndex, 8, rowIndex, segnalazione.Pdf_Attachment, Common.ExcelInsertTypeEnum.Content);
                CellInsertValue(WorkSheetIndex, 9, rowIndex, segnalazione.Image_Attachment, Common.ExcelInsertTypeEnum.Content);
                CellInsertValue(WorkSheetIndex, 10, rowIndex, segnalazione.Audio_Attachment, Common.ExcelInsertTypeEnum.Content);

                rowIndex++;
            }

            rowIndex += 2;
        }

        #endregion

        #region HEADER FUNCTIONS

        private void WriteWorksheetHeader(DateTime date)
        {
            //RangeUnion(WorkSheetIndex, 1, 1, 3, 1);
            //RangeSetBorders(WorkSheetIndex, 1, 1, 3, 1, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            //RangeSetFontBold(WorkSheetIndex, 1, 1, 3, 1);
            //CellInsertValue(WorkSheetIndex, 1, 1, "AeC Costruzioni", Common.ExcelInsertTypeEnum.Content);

            RangeUnion(WorkSheetIndex, 4, 1, 7, 1);
            RangeSetBorders(WorkSheetIndex, 4, 1, 7, 1, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(WorkSheetIndex, 4, 1, 7, 1);
            CellInsertValue(WorkSheetIndex, 4, 1, "Giornale dei lavori", Common.ExcelInsertTypeEnum.Content);

            RangeUnion(WorkSheetIndex, 8, 1, 10, 1);
            RangeSetBorders(WorkSheetIndex, 8, 1, 10, 1, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(WorkSheetIndex, 8, 1, 10, 1);
            CellInsertValue(WorkSheetIndex, 8, 1, date.ToShortDateString(), Common.ExcelInsertTypeEnum.Content);
        }
        private void WriteCollaboratoriHeader()
        {
            RangeUnion(WorkSheetIndex, 1, rowIndex, 10, rowIndex);
            RangeSetBorders(WorkSheetIndex, 1, rowIndex, 10, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(WorkSheetIndex, 1, rowIndex, 10, rowIndex);
            RangeSetBackgroundColor(WorkSheetIndex, 1, rowIndex, 10, rowIndex, Color.LightBlue, fillStyle);
            CellInsertValue(WorkSheetIndex, 1, rowIndex, "Personale AeC", Common.ExcelInsertTypeEnum.Content);

            rowIndex++;

            WriteCustomHeaderCell(1, rowIndex);
            CellInsertValue(WorkSheetIndex, 1, rowIndex, "Collaboratore", Common.ExcelInsertTypeEnum.Content);

            WriteCustomHeaderCell(2, rowIndex);
            RangeUnion(WorkSheetIndex, 2, rowIndex, 7, rowIndex);
            RangeSetBorders(WorkSheetIndex, 2, rowIndex, 7, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(WorkSheetIndex, 2, rowIndex, 7, rowIndex);
            CellInsertValue(WorkSheetIndex, 2, rowIndex, "Note", Common.ExcelInsertTypeEnum.Content);

            rowIndex++;
        }
        private void WriteMezziHeader()
        {
            RangeUnion(WorkSheetIndex, 1, rowIndex, 10, rowIndex);
            RangeSetBorders(WorkSheetIndex, 1, rowIndex, 10, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(WorkSheetIndex, 1, rowIndex, 10, rowIndex);
            RangeSetBackgroundColor(WorkSheetIndex, 1, rowIndex, 10, rowIndex, Color.LightBlue, fillStyle);
            CellInsertValue(WorkSheetIndex, 1, rowIndex, "Mezzi e attrezzature", Common.ExcelInsertTypeEnum.Content);

            rowIndex++;


            WriteCustomHeaderCell(1, rowIndex);
            CellInsertValue(WorkSheetIndex, 1, rowIndex, "Mezzo", Common.ExcelInsertTypeEnum.Content);

            WriteCustomHeaderCell(2, rowIndex);
            RangeUnion(WorkSheetIndex, 2, rowIndex, 10, rowIndex);
            RangeSetBorders(WorkSheetIndex, 2, rowIndex, 10, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            CellInsertValue(WorkSheetIndex, 2, rowIndex, "Descrizione", Common.ExcelInsertTypeEnum.Content);

            rowIndex++;
        }
        private void WriteFornitoriHeader()
        {
            RangeUnion(WorkSheetIndex, 1, rowIndex, 10, rowIndex);
            RangeSetBorders(WorkSheetIndex, 1, rowIndex, 10, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(WorkSheetIndex, 1, rowIndex, 10, rowIndex);
            RangeSetBackgroundColor(WorkSheetIndex, 1, rowIndex, 10, rowIndex, Color.LightBlue, fillStyle);
            CellInsertValue(WorkSheetIndex, 1, rowIndex, "Imprese fornitrici/subappaltatrici", Common.ExcelInsertTypeEnum.Content);

            rowIndex++;

            WriteCustomHeaderCell(1, rowIndex);
            CellInsertValue(WorkSheetIndex, 1, rowIndex, "Elenco personale", Common.ExcelInsertTypeEnum.Content);

            WriteCustomHeaderCell(2, rowIndex);
            RangeUnion(WorkSheetIndex, 2, rowIndex, 10, rowIndex);
            RangeSetBorders(WorkSheetIndex, 2, rowIndex, 10, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            CellInsertValue(WorkSheetIndex, 2, rowIndex, "Ditta di appartenenza", Common.ExcelInsertTypeEnum.Content);

            rowIndex++;
        }
        private void WriteResponsabiliHeader()
        {
            RangeUnion(WorkSheetIndex, 1, rowIndex, 10, rowIndex);
            RangeSetBorders(WorkSheetIndex, 1, rowIndex, 10, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(WorkSheetIndex, 1, rowIndex, 10, rowIndex);
            RangeSetBackgroundColor(WorkSheetIndex, 1, rowIndex, 10, rowIndex, Color.LightBlue, fillStyle);
            CellInsertValue(WorkSheetIndex, 1, rowIndex, "Soggetti preposti controllo", Common.ExcelInsertTypeEnum.Content);

            rowIndex++;

            WriteCustomHeaderCell(1, rowIndex);
            CellInsertValue(WorkSheetIndex, 1, rowIndex, "Responsabile", Common.ExcelInsertTypeEnum.Content);

            WriteCustomHeaderCell(2, rowIndex);
            CellInsertValue(WorkSheetIndex, 2, rowIndex, "Funzione", Common.ExcelInsertTypeEnum.Content);

            WriteCustomHeaderCell(3, rowIndex);
            RangeUnion(WorkSheetIndex, 3, rowIndex, 7, rowIndex);
            RangeSetBorders(WorkSheetIndex, 3, rowIndex, 7, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(WorkSheetIndex, 3, rowIndex, 7, rowIndex);
            CellInsertValue(WorkSheetIndex, 3, rowIndex, "Note", Common.ExcelInsertTypeEnum.Content);

            WriteCustomHeaderCell(8, rowIndex);
            CellInsertValue(WorkSheetIndex, 8, rowIndex, "Pdf", Common.ExcelInsertTypeEnum.Content);

            WriteCustomHeaderCell(9, rowIndex);
            CellInsertValue(WorkSheetIndex, 9, rowIndex, "Immagini", Common.ExcelInsertTypeEnum.Content);

            WriteCustomHeaderCell(10, rowIndex);
            CellInsertValue(WorkSheetIndex, 10, rowIndex, "Audio", Common.ExcelInsertTypeEnum.Content);

            rowIndex++;
        }

        #endregion

        #region SUPPORT FUNCTIONS

        List<Col> GetColsByDayAndRole(DateTime day, string role)
        {
            ISet<Col> result = new HashSet<Col>();

            foreach (var entry in _regsRow.Where(c => c.Key.Raggruppamento1_Col == role).ToList())
            {
                if (entry.Value.Where(c => c.Data_Reg == day).Any()) //Se ci somo reg accoppiate quel giorno
                {
                    result.Add(entry.Key);
                }
            }

            return result.ToList();
        }

        #endregion

        private void WriteCustomHeaderCell(int col, int row)
        {
            RangeSetBorders(WorkSheetIndex, col, row, col, row, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(WorkSheetIndex, col, row, col, row);
        }
    }
}
