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
    *  Export specifico verso software paghe Zucchetti in formato .txt
    *  Export per periodo mensile
    *  Due livelli di record:
    *   - Per ogni collaboratore selezionato, genera un record di testata con i campi:
    *       - codTipoRec00      -> identifica il tipo di record (testata in questo caso)
    *       - codDipnRP         -> lasciato vuoto
    *       - codSoggDipn       -> codice dipendente richiesto da sw Zucchetti
    *       - numRapp           -> numero di rapporto con il collaboratore
    *       - vfGenrAutmTeor    -> campo fisso a 'N'
    *       - xFill1            -> campo riempitivo per arrivare a 100 caratteri
    *       NB: parte del codSoggDipn e l'interità di numRapp vengono recuperati da campo CodiceMatricola del collaboratore in questo modo:
    *       CodiceMatricola = $$/$$$, dove i caratteri a sinistra di '/' sono il progressivo (una delle parti di cui è composto codSoggDipn), mentre quelli a destra corrispondono a numRapp
    *   - Sotto alla testata, viene generato un record per ogni data - motivazione, con i seguenti campi:
    *       - codTipoRec01      -> identifica il tipo di record (dati in questo caso)
    *       - codGiusvRP        -> lasciato vuoto
    *       - codGiusvPA        -> codice motivazione richiesto dal record Zucchetti
    *       - dataMovmn         -> data della reg_v
    *       - numOreMovmn       -> somma delle ore lavorate nella data specificata
    *       - numMinMovmn       -> somma dei minuti lavorati nella data specificata
    *       - numCentMovmn      -> lasciato vuoto
    *       - vfGioRip          -> lasciato vuoto
    *       - vfGioChisStrrr    -> lasciato vuoto
    *       - codTurno          -> codice turno, 0 se non gestiti
    *       - xFill2            -> campo riempitivo per arrivare a 100 caratteri
    **/

    class ExportTxtZucchetti : IExportTxtCustom<Reg_V>
    {
        #region Public Properties
        public ExportRegVCalculationTypeEnum CalculationType { get; set; }

        public int DurationTollerance { get; set; }

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

        #region Public Methods
        public void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds, bool isLaunchedWithErrors = false)
        {

            Col currentCol = new Col();

            #region Campi fissi testata
            // Codice dipendente PowerWeb - facoltativo se sono presenti codSoggDipn e numRapp (quindi lasciato vuoto) - lunghezza: 21
            String codDipnRP = "";
            CommonService.FillWithChar(false, ' ', 21, ref codDipnRP);
            // Codice del tipo di record (in questo caso 00 - testata dipendente) - lunghezza: 2
            String codTipoRec00 = "00";
            // Campo fisso a 'N' - lunghezza: 1
            String vfGenrAutmTeor = "N";
            // Campo riempitivo - lunghezza: 65
            String xFill1 = "";
            CommonService.FillWithChar(false, ' ', 65, ref xFill1);
            #endregion

            #region Campi fissi movimenti giornalieri
            // Codice del tipo di record (in questo caso 00 - movimenti giornalieri) - lunghezza: 2
            String codTipoRec01 = "01";
            // Codice motivazione PowerWeb - facoltativo se è presente CodGiusvPA (quindi lasciato vuoto) - lunghezza: 5
            String codGiusvRP = "";
            CommonService.FillWithChar(false, ' ', 5, ref codGiusvRP);
            // Campo facoltativo in quanto presente numMinMovmn - lunghezza: 2
            String numCentMovmn = "";
            CommonService.FillWithChar(false, ' ', 2, ref numCentMovmn);
            // campo da lasciare non compilato - lunghezza: 1
            String vfGioRip = "";
            CommonService.FillWithChar(false, ' ', 1, ref vfGioRip);
            // campo da lasciare non compilato - lunghezza: 1
            String vfGioChisStrrr = "";
            CommonService.FillWithChar(false, ' ', 1, ref vfGioChisStrrr);
            // Codice turno del movimento - sempre 0 se non gestiti - lunghezza: 1
            int codTurno = 0;
            // Campo riempitivo - lunghezza: 74
            String xFill2 = "";
            CommonService.FillWithChar(false, ' ', 74, ref xFill2);
            #endregion

            DateTime maxBound = ExportPeriod.AddMonths(1);
            // Estrapola tutte le regv che siano ore corrispondenti ai collaboratori selezionati (diviso in 3 per chiarezza)
            List<Reg_V> allEntities = RepoManager.Reg_VRepo.GetAllQueryable().Where(regv => (regv.Data_Reg >= ExportPeriod && regv.Data_Reg < maxBound)).ToList();
            List<Reg_V> allEntitiesFiltered = allEntities.Where(regv => (regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration)).ToList();
            List<Reg_V> entitiesToExport = allEntitiesFiltered.Where(regv => selectedColIds.Any(val => val == regv.Col_Id)).ToList();

            if (entitiesToExport.Any())
            {
                var txtLines = new List<string>();
                string txtLine = String.Empty;
                var entitiesToExportByCol = entitiesToExport.Where(regv => regv.Registrazione_Stato_Reg == (int)RegStateEnum.Ass).GroupBy(regv => regv.Col_Id).ToList();
                // Raggruppa per collaboratore le regv abbinate
                foreach (var regsByCol in entitiesToExportByCol)
                {
                    // Recupera il collaboratore corrente dal col_id della prima regv nel gruppo
                    int col_id = regsByCol.First().Col_Id.Value;
                    currentCol = RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == col_id);

                    if (!currentCol.Equals(default(Col)))
                    {
                        // Recupera il numero del rapporto dalla matricola del collaboratore. Se la matricola è null, assegna 01/001 di default
                        String matricola = currentCol.Matricola_Col ?? "01/001";
                        String[] progRapp = matricola.Split('/');

                        #region  Record di testata per dipendente
                        // Codice dipendente richiesto dal record Zucchetti (prime 3 lettere del cognome + prime 3 lettere del nome + progressivo) - lunghezza: 8
                        String codSoggDipn = String.Concat(currentCol.Cognome_Col.Substring(0, 3), currentCol.Nome_Col.Substring(0, 3), progRapp[0]);
                        CommonService.FillWithChar(false, ' ', 8, ref codSoggDipn);
                        // Numero rapporto - fisso a 001 - lunghezza: 3
                        String numRapp = progRapp[1];
                        CommonService.FillWithChar(true, '0', 3, ref numRapp);

                        // Compone la linea da stampare
                        txtLine = String.Format("{0}{1}{2}{3}{4}{5}"
                                , codTipoRec00
                                , codDipnRP
                                , codSoggDipn
                                , numRapp
                                , vfGenrAutmTeor
                                , xFill1
                            );

                        // Aggiunge la linea a quelle da stampare
                        txtLines.Add(txtLine);

                        #endregion

                        // Raggruppo le registrazioni per anno
                        var entitiesToExportByYear = regsByCol.GroupBy(reg => reg.Data_Reg.Value.Year).OrderBy(reg => reg.First().Data_Reg.Value.Year).ToList();
                        foreach (var regsByColYear in entitiesToExportByYear)
                        {
                            // Recupero l'anno delle registrazioni
                            int year = regsByColYear.First().Data_Reg.Value.Year;
                            String aaMovmn = year.ToString();

                            // Raggruppo le registrazioni per mese
                            var entitiesToExportByMonth = regsByColYear.GroupBy(reg => reg.Data_Reg.Value.Month).OrderBy(reg => reg.First().Data_Reg.Value.Month).ToList();
                            foreach (var regsByColYearMonth in entitiesToExportByMonth)
                            {
                                // Recupero il mese delle registrazioni
                                int month = regsByColYear.First().Data_Reg.Value.Month;
                                String mmMovmn = month.ToString("00");

                                // Raggruppo le registrazioni per giorno
                                var entitiesToExportByDate = regsByColYearMonth.GroupBy(reg => reg.Data_Reg.Value.Day).OrderBy(reg => reg.First().Data_Reg.Value.Day).ToList();
                                foreach (var regsByColDate in entitiesToExportByDate)
                                {
                                    // Recupero il mese delle registrazioni
                                    int day = regsByColDate.First().Data_Reg.Value.Day;
                                    String ggMovmn = day.ToString("00");

                                    // Data registrazioni - lunghezza: 8
                                    String dataMovmn = String.Format("{0}{1}{2}", aaMovmn, mmMovmn, ggMovmn);

                                    // Raggruppo per la motivazione delle registrazioni
                                    var entitiesToExportByMot = regsByColDate.GroupBy(reg => reg.Motivazione_Reg_Id).ToList();
                                    foreach (var regsByColDateMot in entitiesToExportByMot)
                                    {
                                        // Recupero la motivazione delle registrazioni ("01" - ore ordinarie - di default)
                                        string mot = "01";
                                        int motId = regsByColDateMot.First().Motivazione_Reg_Id ?? 0;
                                        if (motId != 0)
                                        {
                                            Tab_Decod tab_decod = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Tab_Decod_Id == motId);

                                            if (tab_decod != default(Tab_Decod))
                                            {
                                                mot = tab_decod.Chiave_Tab;
                                            }
                                        }
                                        CommonService.FillWithChar(false, ' ', 2, ref mot);

                                        int totalDuration = 0;

                                        // Calcolo durata totale per record
                                        foreach (var reg in regsByColDateMot)
                                        {
                                            totalDuration += reg.Durata_Fig.GetValueOrDefault();
                                        }

                                        #region Record movimenti giornalieri

                                        // Codice motivazione richiesto dal record Zucchetti - lunghezza: 2
                                        String codGiusvPA = mot.Substring(0, 2); // TODO: recuperare codice motivazione Zucchetti da tabella giustificativi da loro fornita
                                                                                 // Ore movimentazione - lunghezza: 2
                                        String numOreMovmn = (totalDuration / 60).ToString("00");
                                        // Minuti movimentazione - lunghezza: 2
                                        String numMinMovmn = (totalDuration % 60).ToString("00");

                                        // Compone la linea da stampare
                                        txtLine = String.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}"
                                            , codTipoRec01
                                            , codGiusvRP
                                            , codGiusvPA
                                            , dataMovmn
                                            , numOreMovmn
                                            , numMinMovmn
                                            , numCentMovmn
                                            , vfGioRip
                                            , vfGioChisStrrr
                                            , codTurno
                                            , xFill2);

                                        // Aggiunge la linea a quelle da stampare
                                        txtLines.Add(txtLine);

                                        #endregion
                                    }
                                }
                            }
                        }
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
            throw new NotImplementedException();
        }
        #endregion
    }
}
