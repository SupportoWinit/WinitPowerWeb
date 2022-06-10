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

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /// <summary>
    /// Classe utilizzata per la gestione dell'export per fatture
    /// </summary>
    public class ExportPerFatture : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
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

        #endregion

        #region Public Methods

        /// <summary>
        /// Esegue la preparazione dell'export con i dati passati come parametro
        /// </summary>
        /// <param name="entitiesToExport">Elenco delle entità da esportare</param>
        public override void LaunchExport(IQueryable<Reg_V> entitiesToExport)
        {
            if (entitiesToExport.Any())
            {
                List<DateTime> monthDays = CommonService.GetDatesFromPeriod(CommonService.GetFirstMonthDay(ExportPeriod), CommonService.GetLastMonthDay(ExportPeriod));


                ExcelWorkbookGenerateNew(ExcelModelFilePath);
                entitiesToExport.OrderBy(x => x.Cli_Id);

                foreach (Reg_V reg_V in entitiesToExport)
                {
                    if (!WorksheetIsCreated(reg_V.Cognome_Cli))
                    {
                        List<DateTime> weekDay = new List<DateTime>();
                        List<DateTime> weekEnd = new List<DateTime>();


                        WorksheetCopy("Foglio1", reg_V.Cognome_Cli);
                        CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 2, 3, reg_V.Cognome_Cli, ExcelInsertTypeEnum.Content);

                        IEnumerable<Cant> cantList = RepoManager.CantRepo.GetAllQueryable().Where(c => c.Cli_Id == reg_V.Cli_Id);

                        string formulaToInsert = "SUM(C6:C" + (cantList.Count() + 5) + ")";
                        RangeInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 3, 3, 33, 3, formulaToInsert, ExcelInsertTypeEnum.Formula);
                        CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 34, 3, "SUM(C3:AG3)", ExcelInsertTypeEnum.Formula);

                        int rowIndex = 6;
                        foreach (Cant cant in cantList)
                        {
                            CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 1, rowIndex, cant.Descrizione_Can, ExcelInsertTypeEnum.Content);
                            IEnumerable<Reg_V> reg_VsPart = RepoManager.Reg_VRepo.GetAllQueryable().Where(r => r.Cli_Id == reg_V.Cli_Id && r.Cant_Id == cant.Cant_Id && r.Motivazione_Reg_Cod == "PART");
                            IEnumerable<Reg_V> reg_VsPerm = RepoManager.Reg_VRepo.GetAllQueryable().Where(r => r.Cli_Id == reg_V.Cli_Id && r.Cant_Id == cant.Cant_Id && r.Motivazione_Reg_Cod == "FERM");



                            CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 35, rowIndex, reg_VsPart.Count(), ExcelInsertTypeEnum.Content);
                            CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 36, rowIndex, reg_VsPerm.Count(), ExcelInsertTypeEnum.Content);

                            foreach (DateTime day in monthDays)
                            {
                                if (day.DayOfWeek == DayOfWeek.Sunday)
                                    weekEnd.Add(day);
                                else
                                    weekDay.Add(day);

                                IEnumerable<Reg_V> reg_VsTot = RepoManager.Reg_VRepo.GetAllQueryable().Where(r => r.Data_Reg.Value.Day == day.Day && r.Cli_Id == reg_V.Cli_Id && r.Cant_Id == cant.Cant_Id && r.Registrazione_Stato_Reg == 1);
                                CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 37, rowIndex, weekEnd.Count(), ExcelInsertTypeEnum.Content);
                                CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 38, rowIndex, weekDay.Count(), ExcelInsertTypeEnum.Content);
                                CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), (day.Day + 2), rowIndex, reg_VsTot.Count(), ExcelInsertTypeEnum.Content);

                                weekEnd.Clear();
                                weekDay.Clear();
                            }
                            rowIndex++;
                        }

                        CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 35, 3, "SUM(AI6:AI" + (cantList.Count() + 5) + ")", ExcelInsertTypeEnum.Formula);
                        CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 36, 3, "SUM(AJ6:AJ" + (cantList.Count() + 5) + ")", ExcelInsertTypeEnum.Formula);

                        CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 37, 3, "SUM(AK6:AK" + (cantList.Count() + 5) + ")", ExcelInsertTypeEnum.Formula);
                        CellInsertValue(WorksheetNumberFromName(reg_V.Cognome_Cli), 38, 3, "SUM(AL6:AL" + (cantList.Count() + 5) + ")", ExcelInsertTypeEnum.Formula);

                        ColumnsSetAutoWidth(WorksheetNumberFromName(reg_V.Cognome_Cli), 1, 39);
                    }
                }


                WorksheetDelete(1);

                ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

                ExcelWorkbookDispose();
            }
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
