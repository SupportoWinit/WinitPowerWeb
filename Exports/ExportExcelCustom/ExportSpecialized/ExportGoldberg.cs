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
using DevExpress.XtraSpreadsheet.Commands.Internal;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /// <summary>
    /// Classe utilizzata per la gestione dell'export per fatture
    /// </summary>
    public class ExportGoldberg : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
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
        private int rowIndex = 2;

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

                foreach (Reg_V reg_V in entitiesToExport)
                {
                    //recupero il codice del cliente da mettere nel file, controllo che sia presente, in caso contrario metto 4 zeri
                    string codiceCliente = "0000";
                    if (reg_V.Col_Id != null) {
                        List<Col> col = RepoManager.ColRepo.GetAllQueryable(c => c.Col_Id == reg_V.Col_Id).ToList();
                        codiceCliente = col.First().Cognome_Col;
                    }
                    //vado a recuperare l'ora della timbratura e la formatto secondo i loro standard
                    string ora = "000000";
                    if (reg_V.Data_Reg.HasValue) {
                        ora = reg_V.Data_Reg.Value.Date.ToString();
                        List<string> data = ora.Split(' ').ToList();
                        ora = data[0];
                    }
                    List<string> entrata = reg_V.Data_Ora_Fig_ETime.ToString().Split(':').ToList();

                    string frm = entrata[0];
                    if (entrata[0].StartsWith("0"))
                    {
                        frm = entrata[0].Remove(0, 1);
                    }

                    CellInsertValue(1, 1, rowIndex, "" + codiceCliente, ExcelInsertTypeEnum.Content);

                    CellInsertValue(1, 2, rowIndex, "" + reg_V.Note_Col, ExcelInsertTypeEnum.Content);
                    CellInsertValue(1, 3, rowIndex, "" + ora, ExcelInsertTypeEnum.Content);

                    CellInsertValue(1, 4, rowIndex, "" + frm +""+ entrata[1], ExcelInsertTypeEnum.Content);
                    CellInsertValue(1, 5, rowIndex, "E", ExcelInsertTypeEnum.Content);
                    rowIndex++;
                    //nel caso in cui la reg_V rilevi anche una timbratura d'uscita vado a fare la riga apposita
                    if (reg_V.RegU != null) {
                        codiceCliente = "0000";
                        if (reg_V.Col_Id != null)
                        {
                            List<Col> col = RepoManager.ColRepo.GetAllQueryable(c => c.Col_Id == reg_V.Col_Id).ToList();
                            codiceCliente = col.First().Cognome_Col;
                        }
                        ora = "000000";
                        if (reg_V.Data_Reg.HasValue)
                        {
                            ora = reg_V.Data_Reg.Value.Date.ToString();
                            List<string> data = ora.Split(' ').ToList();
                            ora = data[0];
                        }
                        List<string> uscita = reg_V.Data_Ora_Fig_UTime.ToString().Split(':').ToList();
                        frm = uscita[0];
                        if (uscita[0].StartsWith("0")) {
                            frm = uscita[0].Remove(0, 1);
                        }
                        

                        CellInsertValue(1, 1, rowIndex, "" + codiceCliente, ExcelInsertTypeEnum.Content);

                        CellInsertValue(1, 2, rowIndex, "" + reg_V.Note_Col, ExcelInsertTypeEnum.Content);
                        CellInsertValue(1, 3, rowIndex, "" + ora, ExcelInsertTypeEnum.Content);

                        CellInsertValue(1, 4, rowIndex, "" + frm + "" + uscita[1], ExcelInsertTypeEnum.Content);
                        CellInsertValue(1, 5, rowIndex, "U", ExcelInsertTypeEnum.Content);
                        rowIndex++;
                    }
                }

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
