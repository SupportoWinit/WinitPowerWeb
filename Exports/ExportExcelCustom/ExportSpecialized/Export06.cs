using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Business;
using Business.Repository;
using Common;
using Domain;
using OfficeOpenXml.Style;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /// <summary>
    /// Classe utilizzata per la produzione e l'estrazione verso l'utente dell'export 06
    /// </summary>
    public class Export06 : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
    {
        #region Private Constants

        /// <summary>
        /// Il numero del workbook utilizzato nella produzione dell'export
        /// </summary>
        private const int ExportWorkbookNumber = 1;

        /// <summary>
        /// La posizione della riga della testata dell'intero export
        /// </summary>
        private const int ExportHeaderRow = 2;

        /// <summary>
        /// La posizione della prima riga di dati nell'export
        /// </summary>
        private const int FirstExportDataRow = 9;

        /// <summary>
        /// La posizione della riga della testata della tabella che contiene il numero del giorno
        /// </summary>
        private const int TableHeaderDayNameRowPos = 6;

        /// <summary>
        /// La posizione della riga della testata della tabella che contiene il nome del giorno
        /// </summary>
        private const int TableHeaderDayNumberRowPos = 7;

        /// <summary>
        /// La posizione della prima colonna che visualizza i giorni all'interno del report
        /// </summary>
        private const int FirstDayColumn = 4;

        #endregion

        #region Private Fields

        /// <summary>
        /// La posizione della colonna del totale delle ore nel report
        /// </summary>
        private int _totalHoursColumn = 0;

        /// <summary>
        /// La posizione della colonna degli straordinari nel report
        /// </summary>
        private int _extraordinaryColumn = 0;

        /// <summary>
        /// La posizione della colonna del collaboratore nel report
        /// </summary>
        private int _colNameColumn = 0;

        #endregion

        #region Public Properties

        /// <summary>
        /// Recupera o imposta il periodo (mese/anno) di riferimento dell'export.
        /// </summary>
        /// <value>
        /// Il periodo (mese/anno) di riferimento dell'export.
        /// </value>
        public DateTime ExportPeriod { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che indica se è necessario impostare uno specifico calcolo (figurative/fisiche).
        /// </summary>
        /// <value>
        /// <c>true</c> se è necessario impostare uno specifico calcolo (figurative/fisiche); altrimenti, <c>false</c>.
        /// </value>
        public bool UseCalculationType { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che indica se è necessario utilizzare un calcolo specifico di ore (solo durata/con entrata uscita).
        /// </summary>
        /// <value>
        /// <c>true</c> se è necessario utilizzare un calcolo specifico di ore (solo durata/con entrata uscita); altrimenti, <c>false</c>.
        /// </value>
        public bool UseHourType { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che determina se utilizzare o meno la tolleranza della durata registrazione in fase di elaborazione.
        /// </summary>
        /// <value>
        /// <c>true</c> se si utilizzerà la tolleranza della durata registrazione in fase di elaborazione; altrimenti, <c>false</c>.
        /// </value>
        public bool UseDurationTollerance { get; set; }

        /// <summary>
        /// Recupera o imposta un valore ch indica quando utilizzare in fase di elaborazione la tolleranza sui valori di entrata/uscita
        /// </summary>
        /// <value>
        /// <c>true</c> se si deve utilizzare in fase di elaborazione la tolleranza sui valori di entrata/uscita; altrimenti, <c>false</c>.
        /// </value>
        public bool UseEUTollerance { get; set; }
        /// <summary>
        /// Recupera o imposta un valore ch indica se utilizzare oppure no l'export del confronto ore budget dettagliato
        /// </summary>
        /// <value>
        /// <c>true</c> se si deve utilizzare oppure no l'export dettagliato; altrimenti, <c>false</c>.
        /// </value>
        public bool UseExportDetail { get; set; }

        /// <summary>
        /// Recupera o imposta lo specifico calcolo da utilizzare (figurative/fisiche); utilizato solo se <see cref="UseCalculationType" /> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Lo specifico calcolo da utilizzare (figurative/fisiche); utilizato solo se <see cref="UseCalculationType" /> è valorizzato a <c>true</c>.
        /// </value>
        public ExportRegVCalculationTypeEnum CalculationType { get; set; }

        /// <summary>
        /// Recupera o imposta il tipo di calc
        /// </summary>
        /// <value>
        /// Il tipo di calcolo sp
        /// </value>
        public ExportRegVHourTypeEnum HourType { get; set; }

        /// <summary>
        /// Recupera o imposta il valore in minuti della tolleranza sulla durata utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="IExportExcelCustom{T}.UseDurationTollerance"/> è valorizzato
        /// a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il valore in minuti della tolleranza sulla durata utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="IExportExcelCustom{T}.UseDurationTollerance"/> è valorizzato a <c>true</c>.
        /// </value>
        public int DurationTollerance { get; set; }

        /// <summary>
        /// Recupera o imposta il valore in minuti della tolleranza sull'entrata/uscita utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="IExportExcelCustom{T}.UseEUTollerance"/> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il valore in minuti della tolleranza sull'entrata/uscita utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="IExportExcelCustom{T}.UseEUTollerance"/> è valorizzato a <c>true</c>.
        /// </value>
        public int EUTollerance { get; set; }
        
        /// <summary>
        /// Recupera o imposta la stringa che rappresenta l'entità di primo riferimento per selezione del modello excel.
        /// </summary>
        /// <value>
        /// La stringa che rappresenta l'entità di primo riferimento per la selezione del modello excel.
        /// </value>
        public ExcelModelSelectionTypeEnum ModelFirstEntity { get; set; }

        /// <summary>
        /// Recupera o imposta il percorso del modello excel su disco.
        /// </summary>
        /// <value>
        /// Il percorso del modello excel su disco.
        /// </value>
        public string ExcelModelFilePath { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Metodo utilizzato per il lancio dell'export.
        /// </summary>
        /// <param name="entitiesToExport">Le entità da esportare sull'export.</param>
        public override void LaunchExport(IQueryable<Reg_V> entitiesToExport)
        {
            // inizializzazione del foglio excel da processare
            ExcelWorkbookGenerateNew(ExcelModelFilePath);

            // si procede con l'elaborazione dell'export solamente se ci sono delle entità da estrarre
            if (entitiesToExport.Any())
            {
                // in questo export si processa un unico workbook; per questo si procede subito alla compilazione della testata;
                PopulateExportHeader();

                // i dati da processare e da estrarre vanno estratti raggruppati per filiale
                var regVsByFil = entitiesToExport.GroupBy(regv => regv.Fil_Id).AsQueryable();

                // ciclo di elaborazione di tutte le filiali
                foreach (var filRegVs in regVsByFil)
                {
                    // le reg_v raggruppate per filiali vanno a loro volta raggruppate per cantiere
                    var filRegVsByCant = filRegVs.GroupBy(regv => regv.Cant_Id).AsQueryable();

                    // per ogni reg_v raggruppata per filiale/cantiere
                    foreach (var regVsUnit in filRegVsByCant)
                    {

                    }
                }

            }

            // esporto quanto generato (in caso di assenza reg_v il file modello) sulla risposta del browser
            ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

            // una volta salvato l'oggetto excel viene cancellato dalla memoria
            ExcelWorkbookDispose();
        }

        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="selectedColIds">L'elenco degli id collaboratore selezionati per l'export.</param>
        /// <param name="selectedCantIds">L'elenco degli id cantiere selezionati per l'export.</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Popola l'header dell'export che si sta processando.
        /// </summary>
        private void PopulateExportHeader()
        {
            // compilazione del mese che si sta processando in testata:
            RangeInsertValue(ExportWorkbookNumber, ColumnNameToColumnIndexConversion("W"), ExportHeaderRow, ColumnNameToColumnIndexConversion("AC"), ExportHeaderRow, ExportPeriod.ToString("MMMM - yyyy", PowerWebContext.Current.UserCultureInfo), ExcelInsertTypeEnum.Content);

            // compilazione dei giorni del mese nell'header della tabella di export:
            // calcolo delle date del periodo mese in elaborazione
            var monthDates = CommonService.GetDatesFromPeriod(ExportPeriod, CommonService.GetLastMonthDay(ExportPeriod));
            // l'indice base delle colonne di giorno parte da 5
            int columnIndex = FirstDayColumn;
            // per ogni giorno del mese
            foreach (var date in monthDates)
            {
                // aggiungo il dato all'header della tabella di export
                CellInsertValue(ExportWorkbookNumber, columnIndex, TableHeaderDayNameRowPos, date.ToString("ddd", PowerWebContext.Current.UserCultureInfo), ExcelInsertTypeEnum.Content);
                CellInsertValue(ExportWorkbookNumber, columnIndex, TableHeaderDayNumberRowPos, date.Day, ExcelInsertTypeEnum.Content);
                RangeSetBorders(ExportWorkbookNumber, columnIndex, TableHeaderDayNumberRowPos, columnIndex, TableHeaderDayNumberRowPos, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Thin);

                // incremento il contantore delle colonne a cui sono arrivato
                columnIndex++;
            }

            // il numero dei giorni precedentemente inserito è grassetto
            RangeSetFontBold(1, FirstDayColumn, TableHeaderDayNumberRowPos, columnIndex - 1, TableHeaderDayNumberRowPos);

            // al termine delle colonne del giorno aggiungo le colonne delle ore straordinarie e del nominativo
            // salvo tra i campi del report la posizione di queste due colonne di modo da poter gestire totali ed inserimento
            CellInsertValue(ExportWorkbookNumber, columnIndex, TableHeaderDayNumberRowPos, BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE), ExcelInsertTypeEnum.Content); // Ore
            RangeSetBorders(ExportWorkbookNumber, columnIndex, TableHeaderDayNumberRowPos, columnIndex, TableHeaderDayNumberRowPos, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Thin, Color.Black, ExcelBorderStyle.Medium);
            _totalHoursColumn = columnIndex++;
            CellInsertValue(ExportWorkbookNumber, columnIndex, TableHeaderDayNumberRowPos, BusinessService.GetLocalizedString(PowerWebResources.LBL_STRAORDINARI_COMPLETI), ExcelInsertTypeEnum.Content); // Straordinari
            RangeSetBorders(ExportWorkbookNumber, columnIndex, TableHeaderDayNumberRowPos, columnIndex, TableHeaderDayNumberRowPos, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium);
            _extraordinaryColumn = columnIndex++;
            CellInsertValue(ExportWorkbookNumber, columnIndex, TableHeaderDayNumberRowPos, BusinessService.GetLocalizedString(PowerWebResources.LBL_NOMINATIVO), ExcelInsertTypeEnum.Content); // Nominativo
            RangeSetBorders(ExportWorkbookNumber, columnIndex, TableHeaderDayNumberRowPos, columnIndex, TableHeaderDayNumberRowPos, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium, Color.Black, ExcelBorderStyle.Medium);
            _colNameColumn = columnIndex;
        }

        #endregion

    }
}
