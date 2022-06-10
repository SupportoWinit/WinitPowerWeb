using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Domain;
using Business.Repository;
using System.Web;
using System.IO;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /// <summary>
    /// Classe utilizzata per la produzione e l'estrazione verso l'utente dell'export 06
    /// </summary>
    public class ExportExcelADP_1003 : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
    {
        #region Private Constants

        /// <summary>
        /// Il numero del workbook utilizzato nella produzione dell'export
        /// </summary>
        private const int ExportWorkbookNumber = 1;

        #endregion

        #region Private Fields

        /// <summary>
        /// La posizione della colonna del totale delle ore nel report
        /// </summary>
        private int _rowIndex = 1;

        /// <summary>
        /// Il numero del workbook utilizzato nella produzione dell'export
        /// </summary>
        private Cant _pausaCant;

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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="selectedColIds">L'elenco degli id collaboratore selezionati per l'export.</param>
        /// <param name="selectedCantIds">L'elenco degli id cantiere selezionati per l'export.</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds, IEnumerable<int> selectedCliIds)
        {
           

            _pausaCant = RepoManager.CantRepo.FirstOrDefault(c => c.Codice_Cantiere == "PAUSA");

            if (selectedColIds.Any())
            {
                // calcolo delle date presenti nel mese periodo in d'elaborazione
                DateTime firstPeriodDate = CommonService.GetFirstMonthDay(ExportPeriod);
                DateTime lastPeriodDate = CommonService.GetLastMonthDay(ExportPeriod);
                // inizializzazione del foglio excel da processare
                ExcelWorkbookGenerateNew(ExcelModelFilePath);

                //PopulateExportHeader();
                _rowIndex++;

                // per ogni collaboratore da processare
                foreach (int colId in selectedColIds)
                {
                    // è recuperato l'elenco delle registrazioni processabili
                    IEnumerable<Reg_V> colRegVs = GetProcessableColRegVs(colId, firstPeriodDate, lastPeriodDate);
                    colRegVs = colRegVs.OrderBy(r => r.Data_Ora_Fig_E);
                    Col processingCol = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colId);

                    if (processingCol != default(Col) && colRegVs.Count() > 0)
                    {
                        foreach (Reg_V processingRegv in colRegVs)
                        {
                            PopulateColDetail(processingCol);
                            PopulateRegVDetail(processingRegv);

                            _rowIndex++;
                        }
                    }
                }

                // esporto quanto generato (in caso di assenza reg_v il file modello) sulla risposta del browser
                ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

                // una volta salvato l'oggetto excel viene cancellato dalla memoria
                ExcelWorkbookDispose();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Popola l'header dell'export che si sta processando.
        /// </summary>
        private void PopulateExportHeader()
        {
            CellInsertValue(ExportWorkbookNumber, 1, _rowIndex, "COD AZ", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 2, _rowIndex, "BADGE", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 3, _rowIndex, "COGNOME", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 4, _rowIndex, "NOME", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 5, _rowIndex, "GIORNO", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 6, _rowIndex, "MESE", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 7, _rowIndex, "ANNO", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 8, _rowIndex, "INIZIO", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 9, _rowIndex, "FINE", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 10, _rowIndex, "DURATA M", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 11, _rowIndex, "V(IAGGIO)", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 12, _rowIndex, "KM", ExcelInsertTypeEnum.Content);
        }

        /// <summary>
        /// Popola l'header dell'export che si sta processando.
        /// </summary>
        private void PopulateColDetail(Col col)
        {
            CellInsertValue(ExportWorkbookNumber, 1, _rowIndex, "1003", ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 2, _rowIndex, col.Codice_Collaboratore.Trim(), ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 3, _rowIndex, col.Cognome_Col, ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 4, _rowIndex, col.Nome_Col, ExcelInsertTypeEnum.Content);
        }

        /// <summary>
        /// Popola l'header dell'export che si sta processando.
        /// </summary>
        private void PopulateRegVDetail(Reg_V regv)
        {
            CellInsertValue(ExportWorkbookNumber, 5, _rowIndex, regv.Data_Reg.Value.Day, ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 6, _rowIndex, ExportPeriod.Month, ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 7, _rowIndex, ExportPeriod.Year, ExcelInsertTypeEnum.Content);
            CellInsertValue(ExportWorkbookNumber, 8, _rowIndex, regv.Data_Ora_Fig_ETime, ExcelInsertTypeEnum.HhmmssTime);
            CellInsertValue(ExportWorkbookNumber, 9, _rowIndex, regv.Data_Ora_Fig_UTime, ExcelInsertTypeEnum.HhmmssTime);
            CellInsertValue(ExportWorkbookNumber, 10, _rowIndex, regv.Durata_Fig.HasValue ? regv.Durata_Fig.Value : 0, ExcelInsertTypeEnum.Content);

            if (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip)
            {
                CellInsertValue(ExportWorkbookNumber, 11, _rowIndex, "V", ExcelInsertTypeEnum.Content);
                CellInsertValue(ExportWorkbookNumber, 12, _rowIndex, regv.KM_Reg, ExcelInsertTypeEnum.Content);
            }
        }

        /// <summary>
        /// Recupera tutte le registrazioni processabili del collaboratore per il periodo specificato.
        /// </summary>
        /// <param name="colId">L'identificativo del collaboratore per cui effettuare la ricerca.</param>
        /// <param name="startPeriod">La data di inizio del periodo in cui effettuare la ricerca.</param>
        /// <param name="endPeriod">La data di fine del periodo in cui effettuare la ricerca.</param>
        /// <returns>L'elenco delle registrazioni da processare per il collaboratore e periodo perscelto.</returns>
        private IEnumerable<Reg_V> GetProcessableColRegVs(int colId, DateTime startPeriod, DateTime endPeriod)
        {
            //Ritorna tutte le ore e i viaggi abbinati, senza tenere conto delle PAUSE
            return RepoManager.Reg_VRepo.Find(regv => regv.Col_Id == colId && regv.Data_Reg >= startPeriod && regv.Data_Reg <= endPeriod
                                                      && (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip)
                                                      && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass
                                                      && regv.Cant_Id != _pausaCant.Cant_Id);
        }

        

        #endregion

    }
}