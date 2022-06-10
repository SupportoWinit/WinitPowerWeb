using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Business.Repository;
using Common;
using Domain;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /// <summary>
    /// Classe utilizzata per la gestione dell'export Movitrento (in stile como) delle registrazioni (per collaboratore con giorni non lavorati)
    /// </summary>
    public class ExportMovitrento : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
    {

        #region Fileds

        /// <summary>
        /// Il valore che indica la customizzazione per l'inserimnento del codice commessa nell'Export.
        /// </summary>
        private ManageOrderCodeSimpleExport? _showOrderCutom = null;

        #endregion

        #region Public Properties

        /// <summary>
        /// Recupera o imposta il percorso del modello excel su disco.
        /// </summary>
        /// <value>
        /// Il percorso del modello excel su disco.
        /// </value>
        public string ExcelModelFilePath { get; set; }

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
        /// Recupera o imposta il tipo di calcolo specifico delle ore (solo durata/con entrata uscita); utilizzato solo se <see cref="UseHourType" /> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il tipo di calcolo specifico delle ore (solo durata/con entrata uscita); utilizzato solo se <see cref="UseHourType" /> è valorizzato a <c>true</c>.
        /// </value>
        public ExportRegVHourTypeEnum HourType { get; set; }

        /// <summary>
        /// Recupera o imposta il valore in minuti della tolleranza sulla durata utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseDurationTollerance" /> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il valore in minuti della tolleranza sulla durata utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseDurationTollerance" /> è valorizzato a <c>true</c>.
        /// </value>
        public int DurationTollerance { get; set; }

        /// <summary>
        /// Recupera o imposta il valore in minuti della tolleranza sull'entrata/uscita utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseEUTollerance" /> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il valore in minuti della tolleranza sull'entrata/uscita utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseEUTollerance" /> è valorizzato a <c>true</c>.
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
        /// Recupera il valore che indica la customizzazione per l'inserimnento del codice commessa nell'Export
        /// </summary>
        /// <value>
        /// Il valore che indica la customizzazione per l'inserimnento del codice commessa nell'Export.
        /// </value>
        public ManageOrderCodeSimpleExport ShowOrderCutom
        {
            get
            {
                if (_showOrderCutom == null)
                    _showOrderCutom = (ManageOrderCodeSimpleExport)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ManageOrderCodeSimpleExport);

                return _showOrderCutom.Value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Esege la preparazione dell'export con i dati passati come parametro
        /// </summary>
        /// <param name="entitiesToExport">L'elenco delle entità da esportare</param>
        public override void LaunchExport(IQueryable<Reg_V> entitiesToExport)
        {
            // si procede con l'elaborazione solamente se ci sono delle registrazioni da processare
            if (entitiesToExport.Any())
            {
                // si generano i giorni del mese relativi al periodo in esecuzione
                List<DateTime> monthDates = CommonService.GetDatesFromPeriod(CommonService.GetFirstMonthDay(ExportPeriod), CommonService.GetLastMonthDay(ExportPeriod));

                // inizializzazione dell'indice di scrittura delle registrazioni
                int writeIndex = 2;
                int position = 2;
                DateTime previusDate = new DateTime();
                string dateName = "";
                bool dateChecked = false;

                // inizializzazione del foglio excel da processare
                ExcelWorkbookGenerateNew(ExcelModelFilePath);


                // si processano le registrazioni ore abbinate raggruppate per collaboratore
                foreach (var regVsByDate in entitiesToExport.Where(regv => (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None) || (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration) && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass).GroupBy(regv => regv.Data_Reg.Value.Month == monthDates.FirstOrDefault().Month))
                {

                    foreach (DateTime monthDate in monthDates)
                    {
                        // se per il collaboratore sono presenti delle registrazioni in giornata allora si procede
                        // alla loro scrittura; altrimenti si riporta il giorno come giorno non lavorato
                        if (regVsByDate.Any(regv => regv.Data_Reg == monthDate))
                        {
                            dateChecked = true;
                            // si recuperano tutte le registrazioni del giorno che si sta processando
                            var dayColRegVs = regVsByDate.Where(regv => regv.Data_Reg == monthDate);

                            foreach (Reg_V regVToWrite in dayColRegVs)
                            {
                                if (monthDate.Date != previusDate.Date)
                                {
                                    dateName = monthDate.ToString("dd-MM-yyyy");
                                    WorksheetCopy("Generale", dateName, position++);
                                    previusDate = monthDate;
                                    writeIndex = 2;
                                }
                                int currentColId = Convert.ToInt32(regVToWrite.Col_Id);
                                Col currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == currentColId);
                                WriteRegV(dateName, regVToWrite, monthDate, writeIndex++, currentCol);
                            }
                            CheckEmptyReg(entitiesToExport, monthDate, writeIndex, dateName);
                        }
                        else if(dateChecked==false)
                        {
                            if (monthDate.Date != previusDate.Date)
                            {
                                dateName = monthDate.ToString("dd-MM-yyyy");
                                WorksheetCopy("Generale", dateName, position++);
                                previusDate = monthDate;
                                writeIndex = 2;
                            }
                            CheckEmptyReg(entitiesToExport, monthDate, writeIndex, dateName);
                        }

                    }


                }

                WorksheetDelete("Generale");
                // esporto quanto generato (in caso di assenza reg_v il file modello) sulla risposta del browser
                ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

                // una volta salvato l'oggetto excel viene cancellato dalla memoria
                ExcelWorkbookDispose();
            }
        }

        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="selectedColIds">L'elenco degli id collaboratore selezionati per l'export.</param>
        /// <param name="selectedCantIds">L'elenco degli id cantiere selezionati per l'export.</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds, IEnumerable<int> selectedCliIds)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Scrite la specifica reg_v sul foglio excel correntemente in elaborazione.
        /// </summary>
        /// <param name="regvToWrite">La registrazione da scrivere; se null si scriverà un giorno non lavorato.</param>
        /// <param name="dateToProcess">La data da scrivere.</param>
        /// <param name="writeIndex">L'indice riga di scrittura</param>
        /// <param name="processingCol">Il collaboratore che si sta processando.</param>
        private void WriteRegV(string sheetName, Reg_V regvToWrite, DateTime dateToProcess, int writeIndex, Col processingCol)
        {
            CellInsertValue(sheetName, 2, writeIndex, processingCol.Codice_Collaboratore, ExcelInsertTypeEnum.Content);
            CellInsertValue(sheetName, 3, writeIndex, processingCol.CognomeNome_Col, ExcelInsertTypeEnum.Content);

            if (regvToWrite != null)
            {
                //CellInsertValue(1, ?, writeIndex, regvToWrite.Cant_Mnemonic, ExcelInsertTypeEnum.Content);
                CellInsertValue(sheetName, 4, writeIndex, regvToWrite.Cant_Desc, ExcelInsertTypeEnum.Content);


                TimeSpan? currentTime = regvToWrite.Data_Ora_Fig_E != null ? regvToWrite.Data_Ora_Fig_E.Value.TimeOfDay : (TimeSpan?)null;
                if (currentTime != null)
                {
                    var currentTimeCent = CommonService.GetDoubleFromTimeSpan((TimeSpan)currentTime, true);
                    CellInsertValue(sheetName, 5, writeIndex, currentTimeCent, ExcelInsertTypeEnum.Content);
                }

                currentTime = regvToWrite.Data_Ora_Fig_U != null ? regvToWrite.Data_Ora_Fig_U.Value.TimeOfDay : (TimeSpan?)null;
                if (currentTime != null)
                {
                    var currentTimeCent = CommonService.GetDoubleFromTimeSpan((TimeSpan)currentTime, true);
                    CellInsertValue(sheetName, 6, writeIndex, currentTimeCent, ExcelInsertTypeEnum.Content);
                    var durataCentTime = CommonService.GetDoubleFromTimeSpan(TimeSpan.Parse(regvToWrite.Durata_Fig_HH_S), true);
                    CellInsertValue(sheetName, 7, writeIndex, durataCentTime, ExcelInsertTypeEnum.Content);
                }


            }
            else
            {
                CellInsertValue(sheetName, 5, writeIndex, 0, ExcelInsertTypeEnum.Content);
                CellInsertValue(sheetName, 6, writeIndex, 0, ExcelInsertTypeEnum.Content);
                CellInsertValue(sheetName, 7, writeIndex, 0, ExcelInsertTypeEnum.Content);
            }
            CellInsertValue(sheetName, 1, writeIndex, dateToProcess, ExcelInsertTypeEnum.Content);
            //CellInsertValue(1, 5, writeIndex, dateToProcess.ToString("ddddd"), ExcelInsertTypeEnum.Content);

        }



        #endregion
        private void CheckEmptyReg(IQueryable<Reg_V> entitiesToExport,DateTime monthDate, int writeIndex, string dateName)
        {
            // si generano i giorni del mese relativi al periodo in esecuzione
            List<DateTime> monthDates = CommonService.GetDatesFromPeriod(CommonService.GetFirstMonthDay(ExportPeriod), CommonService.GetLastMonthDay(ExportPeriod));

            foreach (IGrouping<string, Reg_V> regVsByCol in entitiesToExport.Where(regv => (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None) || (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration) && regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass).GroupBy(regv => regv.Col_Mnemonic))
            {
                if (!regVsByCol.Any(regv => regv.Data_Reg == monthDate))
                {
                    int currentColId = Convert.ToInt32(regVsByCol.First().Col_Id);
                    Col currentCol = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == currentColId);
                    WriteRegV(dateName, null, monthDate, writeIndex++, currentCol);
                }

            }
        }

    }
}
