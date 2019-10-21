using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Business.Repository;
using System.Web;
using System.IO;
using System.Text;
using Ionic.Zip;

namespace Exports.ExportTxtCustom
{

    /**
    *  Export specifico per il cliente CG2 in formato .txt, con campi separati da '|'
    *  Export per periodo mensile
    *  Esporta un record per ogni coppia collaboratore - cantiere
    *  I record hanno i seguenti campi:
    *   - Qualifica - qualifica del collaboratore
    *   - OreTotali - ore (con decimali in centesimi) lavorate - ore E/U, viaggi, arrotondamenti - dal collaboratore nel cantiere nel mese selezionato
    *   - CostoOrario - parametro del collaboratore recuperato dal Campo1 della Tab_Decod con Nome_Tab = "QUALIFICHE_COL"
    *   - Centro di Costo - recuperato dal campo Raggruppamento1 del cantiere
    **/
    class ExportTxtCG2 : IExportTxtCustom<Reg_V>
    {
        #region Public Properties
        public ExportRegVCalculationTypeEnum CalculationType {get; set;}

        public int DurationTollerance {get; set;}

        public string Path { get; set; }

        public int EUTollerance { get; set; }

        public string FileName { get; set; }

        public DateTime ExportPeriod { get; set; }

        public ExportRegVHourTypeEnum HourType { get; set; }

        public ExcelModelSelectionTypeEnum ModelFirstEntity { get; set; }

        public bool UseCalculationType { get; set; }

        public bool UseDurationTollerance { get; set; }

        public bool UseEUTollerance { get; set; }

        public bool Compress { get; set; }

        /// <summary>
        /// Recupera o imposta un valore ch indica se utilizzare oppure no l'export del confronto ore budget dettagliato
        /// </summary>
        /// <value>
        /// <c>true</c> se si deve utilizzare oppure no l'export dettagliato; altrimenti, <c>false</c>.
        /// </value>
        public bool UseExportDetail { get; set; }

        public bool UseHourType { get; set; }

        #endregion

        #region Private Constants
        /// <summary>
        /// Il separatore tra un elemento e l'altro del file
        /// </summary>
        private const string FileElementSeparator = "|";    

        #endregion

        #region Public Methods

        public void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds, bool isLaunchedWithErrors = false)
        {
            DateTime maxBound = ExportPeriod.AddMonths(1);
            // Estrapola tutte le regv che siano ore, viaggi o arrotondamenti corrispondenti ai collaboratori selezionati (diviso in 3 per chiarezza)
            IQueryable<Reg_V> allEntities = RepoManager.Reg_VRepo.DbSet.Where(regv => (regv.Data_Reg >= ExportPeriod && regv.Data_Reg < maxBound));
            IQueryable<Reg_V> allEntitiesFiltered = allEntities.Where(regv => (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration));
            allEntitiesFiltered = allEntitiesFiltered.Where(regv => regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass);
            ILookup<Nullable<int>,Reg_V> entitiesToExport = allEntitiesFiltered.Where(regv => selectedColIds.Contains((int)regv.Col_Id)).ToLookup(reg => reg.Col_Id);

            if (entitiesToExport.Any())
            {
                var txtLines = new List<string>();
                string txtLine = String.Empty;
                decimal costoOrario;
                string costoOrarioString;

                // Carica dalla tab_decod la lista delle qualifiche con relativi compensi per velocizzare la ricerca
                Dictionary<String, String> qualificaCompenso = new Dictionary<string, string>();
                List<Tab_Decod> tab_decods = RepoManager.Tab_DecodRepo.GetAllQueryable(tab => tab.Nome_Tab == "QUALIFICHE_COL").ToList();
                foreach (Tab_Decod tab in tab_decods)
                {
                    if (tab.Decodifica_Tab != null && tab.Campo1_Tab != null)
                    {
                        qualificaCompenso.Add(tab.Chiave_Tab, tab.Campo1_Tab);
                    }
                }

                // Raggruppa per collaboratore le regv abbinate
                foreach (var regsByCol in entitiesToExport)
                {
                    //Raggruppa per cantiere
                    foreach (var regsByColByCant in regsByCol.GroupBy(regv => regv.Cant_Id))
                    {
                        // Conteggio delle ore fatte dal collaboratore nel cantiere
                        int minutiTotali = 0;
                        decimal oreTotali = 0;

                        // Recupera il campo CodiceCantiere del cantiere in questione (essendo raggruppato per cantiere, tutte le regv (quindi anche la prima) hanno lo stesso cantiere)
                        String centroDiCosto = "";
                        Reg_V currReg = regsByColByCant.FirstOrDefault();
                        int? currCant_id = currReg.Cant_Id;
                        if (currCant_id == null)
                        {
                            break;
                        }

                        Cant currCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == currCant_id);

                        if (currCant != default(Cant))
                        {
                            //Toglie eventuali spazi aggiunti da PowerWeb in capo alla stringa
                            centroDiCosto = currCant.Codice_Cantiere.TrimStart();
                        }

                        // Recupera il campo Qualifica_col del collaboratore in questione (essendo raggruppato per collaboratore, tutte le regv (quindi anche la prima) hanno lo stesso collaboratore)
                        string qualifica = regsByColByCant.FirstOrDefault().Qualifica_Col;
                        if (qualifica == null)
                        {
                            qualifica = "";
                        }
                        //Cicla sul raggruppamento per fare il totale delle ore lavorate sul cantiere
                        foreach (var reg in regsByColByCant)
                        {
                            // Somma tutte le durate delle regv considerate
                            minutiTotali += reg.Durata_Fis.GetValueOrDefault(); // Converte un int nullable in un int normale
                        }

                        // Converte i minuti totali sommati in ore con decimali in centesimi.
                        oreTotali = Convert.ToDecimal(minutiTotali) / 60;

                        // Recupera il costo orario del collaboratore data la sua qualifica. Se non si trova la qualifica nella tab_decod, si imposta 0 di default.
                        if (!qualificaCompenso.TryGetValue(qualifica, out costoOrarioString))
                        {
                            costoOrarioString = "0";
                        }

                        costoOrario = Convert.ToDecimal(costoOrarioString);

                        // Prepara una linea da scrivere nel file
                        txtLine = String.Format("{1}{0}{2}{0}{3}{0}{4}{0}"
                            , FileElementSeparator
                            , qualifica
                            , oreTotali.ToString("0.00")
                            , costoOrario.ToString("0.00")
                            , centroDiCosto);

                        //Aggiunge la linea alla lista di linee da scrivere nel file
                        txtLines.Add(txtLine);
                    }
                }

                // Pulisce eventuali dati presenti nel Response
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.ClearHeaders();
                HttpContext.Current.Response.ClearContent();
                if (Compress)
                {

                    #region FILE COMPRESSO

                    try
                    {
                        HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment; filename=" + FileName.Split('.')[0] + ".zip");
                        HttpContext.Current.Response.ContentType = "application/zip";
                        HttpContext.Current.Response.Flush();
                        //Percorso file txt
                        string textFileNameTemplate = HttpContext.Current.Server.MapPath(@"\" + FileName);
                        //Creazione file 
                        File.Create(textFileNameTemplate).Dispose();
                        using (ZipFile zip = new ZipFile())
                        {
                            string textFileName = FileName;
                            //Scrittura nel file
                            using (StreamWriter writer = new StreamWriter(textFileNameTemplate))
                            {
                                foreach (var line in txtLines)
                                {
                                    writer.WriteLine(line);
                                }
                                writer.Flush();
                                writer.Close();
                            }

                            zip.AddFile(textFileNameTemplate, @"\");
                            zip.Name = textFileName.Split('.')[0] + ".zip";
                            zip.Save(HttpContext.Current.Response.OutputStream);
                            HttpContext.Current.Response.Flush();
                            HttpContext.Current.Response.End();
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {
                        //Elimina il file
                        File.Delete(HttpContext.Current.Server.MapPath(@"\" + FileName));
                    }

                    #endregion

                }
                else
                {
                    #region FILE TXT 

                    HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment; filename=" + FileName);
                    HttpContext.Current.Response.ContentType = "text/plain";
                    HttpContext.Current.Response.Flush();

                    using (StreamWriter writer = new StreamWriter(HttpContext.Current.Response.OutputStream, Encoding.UTF8))
                    {
                        if (isLaunchedWithErrors)
                        {
                            writer.WriteLine("OK");
                            writer.WriteLine(FileName);
                        }

                        foreach (var line in txtLines)
                        {
                            writer.WriteLine(line);
                        }

                    }
                    // Conclude la connessione
                    HttpContext.Current.Response.End();


                    #endregion
                }
            }
        }

        public void LaunchExport(IQueryable<Reg_V> entitiesToExport)
        {
            throw new NotImplementedException();
        }

        public string LaunchExportWithError(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds, bool isLaunchedWithoutErrors = false)
        {
            string errorMessage = "";
            try
            {
                LaunchExport(selectedColIds, selectedCantIds, true);
            }
            catch (Exception e)
            {
                return "ERROR\nC'è stato un errore nell'esportazione, provare più tardi o contattare l'assistenza!";
            }

            return errorMessage;
        }

        public string ExcelWorkbookSaveToSession(string fileName)
        {
            throw new NotImplementedException();
        }

        public void LaunchExport(DateTime selMonth, int[] colIds)
        {
            throw new NotImplementedException();
        }

        public string SaveToFileSystem(string path)
        {
            return "";
        }
        #endregion
    }
}
