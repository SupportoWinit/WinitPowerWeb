using Business;
using Common;
using Business.Repository;
using Ionic.Zip;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Domain;
using System.Globalization;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace Exports
{
    public abstract class ExcelToolBox : IExport
    {

        #region Private Fields

        /// <summary>
        /// Il foglio excel da lavorare con i metodi presenti in questa classe.
        /// </summary>
        private ExcelPackage _excelWorkbook;

        #endregion

        #region Costructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelToolbox"/> class.
        /// </summary>
        protected ExcelToolBox()
        {
            // all'inizializzazione della classe si istanzia un nuovo foglio excel
            // (quest'operazione viene effettuata per non avere problemi di valorizzazione null di tale dato durante il runtime)
            _excelWorkbook = new ExcelPackage();

            Errors = new List<object>();

        }

        #endregion

        #region Protected Properties

        /// <summary>
        /// Recupera o imposta il foglio excel da lavorare con i metodi presenti in questa classe.
        /// </summary>
        /// <value>
        /// Il foglio excel da lavorare con i metodi presenti in questa classe.
        /// </value>
        protected ExcelPackage ExcelWorkbook
        {
            get { return _excelWorkbook; }
            set { _excelWorkbook = value; }
        }

        #region Worksheet Properties


        /// <summary>
        /// Recupera o imposta il numero di fogli di lavoro presenti nel workbook corrente.
        /// </summary>
        /// <value>
        /// Il numero di fogli di lavoro presenti nel workbook corrente.
        /// </value>
        /// <exception cref="System.InvalidOperationException">Excel workbook not initialized</exception>
        protected int WorksheetCount
        {
            get
            {
                // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato
                if (ExcelWorkbook == null)
                    throw new InvalidOperationException("Excel workbook not initialized");

                // ritorno il numero di fogli di lavoro del workbook
                return ExcelWorkbook.Workbook.Worksheets.Count;
            }
        }



        #endregion

        #endregion

        #region Public Properties

        public bool IsToZip { get; set; }

        public bool CentHours { get; set; }

        public DateTime ExportDate { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public string FileName { get; set; }

        public string Extension { get; set; }

        public string DownloadPath
        {
            get
            {
                return String.Format(@"{0}{1}", AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Output_Path);
            }
        }

        public string ModelFilePath { get; set; }

        public string[] GridFilter { get; set; }

        public object[] GridColumns { get; set; }

        public int[] SelectedIds { get; set; }

        public List<object> Errors { get; set; }



        #endregion

        #region Public Methods

        /// <summary>
        /// Implementazione della logica di creazione del file richiesto.
        /// Deve essere overridato da ogni export che implementa la classe inquanto
        /// la logica è differente per ogni export
        /// </summary>
        public abstract void LaunchExport();

        /// <summary>
        /// Salva il file secondo la regola descritta all'interno dell'apposita classe astratta.
        /// Si occupa di zipparlo se necessario e poi lo salva su filesystem in un determinato percorso
        /// </summary>
        public void SaveToFileSystem()
        {
            string filePath = string.Format("{0}{1}.{2}", DownloadPath, FileName, Extension);

            var workbookFileInfo = new FileInfo(filePath);
            if (ExcelWorkbook.Workbook.Worksheets.Count > 0) { 
                ExcelWorkbook.SaveAs(workbookFileInfo);

                if (IsToZip)
                {
                    Extension = "zip";

                    using (ZipFile zipFile = new ZipFile())
                    {
                        zipFile.AddFile(filePath, @"\");

                        zipFile.Save(string.Format("{0}{1}.{2}", DownloadPath, FileName, Extension));

                        File.Delete(filePath);

                    }
                }

                ExcelWorkbook.Dispose();
            }
            

        }

        /// <summary>
        /// Metodo che si occupa di liberare le risorse del foglio excel attualmente in creazione.
        /// NB: la classe ExcelWorkbook utilizzata per l'interfacciamento con excel implementa l'interfaccia IDisposable;
        /// non potendo utilizzare le using è sempre necessario, al termine delle operazioni, liberare le risorse utilizzando
        /// questo metodo.
        /// </summary>
        public void ExcelWorkbookDispose()
        {
            // se il foglio excel è valorizzato allora si procede alla liberazione delle risorse dello stesso
            if (ExcelWorkbook != null)
                ExcelWorkbook.Dispose();
        }

        #endregion

        #region Protected Methods

        #region Excel workbook toolbox

        /// <summary>
        /// Genera un nuovo foglio excel vuoto (senza modello).
        /// </summary>
        protected void ExcelWorkbookGenerateNew()
        {
            // inizializzazione di un nuovo foglio excel
            ExcelWorkbook = new ExcelPackage();
        }

        /// <summary>
        /// Genera un nuovo foglio excel a partire da un modello il cui percorso è passato come parametro.
        /// </summary>
        /// <param name="modelFilePath">Il percorso del modello excel con cui costruire il nuovo file excel.</param>
        /// <exception cref="System.ArgumentNullException">modelFilePath</exception>
        /// <exception cref="System.IO.FileNotFoundException">ERR_PERCORSO_FILE_MODELLO_EXCEL_NON_VALIDO</exception>
        protected void ExcelWorkbookGenerateNew(string modelFilePath)
        {
            #region Convalida input del metodo

            // convalida input del metodo: il percorso passato come parametro non può essere vuoto e deve essere presente e raggiungibile il file
            if (String.IsNullOrEmpty(modelFilePath))
                throw new ArgumentNullException("modelFilePath", BusinessService.GetLocalizedString(PowerWebResources.ERR_PERCORSO_FILE_MODELLO_EXCEL_NON_VALIDO));

            string completePath = AppDomain.CurrentDomain.BaseDirectory + "ExcelModels" + "\\" + modelFilePath;

            if (!File.Exists(completePath))
                throw new FileNotFoundException(BusinessService.GetLocalizedString(PowerWebResources.ERR_PERCORSO_FILE_MODELLO_EXCEL_NON_VALIDO));

            #endregion

            // inizializzazione del nuovo foglio excel a partire dal modello;
            // nella generazione del file si utilizza uno stream per non tenere eventualmente il lock
            // sul file modello (che potrebbe essere utilizzato da altri utenti dell'applicativo)
            FileInfo modelFileInfo = new FileInfo(completePath);
            ExcelWorkbook = new ExcelPackage(modelFileInfo, true);
        }

        #endregion

        #region Worksheet toolbox

        /// <summary>
        /// Determina se è creato e operativo il foglio di lavoro alla posizione specificata.
        /// </summary>
        /// <param name="worksheetNumber">La posizione del foglio di lavoro da cercare per la verifica.</param>
        /// <returns>
        /// <c>true</c> in caso il foglio di lavoro sia creato e operativo alla posizione specificata; altrimenti <c>false</c>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Excel workbook not initialized</exception>
        protected bool WorksheetIsCreated(int worksheetNumber)
        {
            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            // si ritorna true se il numero di fogli di lavoro all'interno dell'excel è maggiore o uguale all'indice passato come parametro
            return ExcelWorkbook.Workbook.Worksheets.Count >= worksheetNumber;
        }

        /// <summary>
        /// Determina se è creato e operativo il foglio di lavoro con il nome specificato.
        /// </summary>
        /// <param name="worksheetName">Il nome del foglio di lavoro da cercare.</param>
        /// <returns>
        /// <c>true</c> in caso il foglio di lavoro sia creato e operativo alla posizione specificata; altrimenti <c>false</c>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Excel workbook not initialized</exception>
        protected bool WorksheetIsCreated(string worksheetName)
        {
            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            // si ritorna true se esiste un foglio con il nome passato come parametro nell'excel attuale
            return ExcelWorkbook.Workbook.Worksheets.Any(ws => ws.Name == worksheetName);
        }

        /// <summary>
        /// Crea un nuovo worksheet con il nome e la posizione richiesta.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da generare (se non specificato verrà assegnato un nome di default).</param>
        /// <param name="position">La posizione del worksheet da generare (se non specificata il nuovo worksheet viene accodato a quelli esistenti).</param>
        /// <exception cref="System.InvalidOperationException">Excel workbook not initialized
        /// or
        /// Worksheet yet created</exception>
        protected void WorksheetCreateNew(string worksheetName = "", int position = -1)
        {
            #region Convalida Input del metodo

            // convalida input del metodo: questo metodo può operare solo se il workbook è generato, se il worksheet non è già presente il nome richiesto (se il nome è diverso da ""),
            // se la posizone è indicata (!= -1) allora deve essere nel range attualmente creato o consecutivo a esso
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!String.IsNullOrEmpty(worksheetName))
                if (WorksheetIsCreated(worksheetName))
                    throw new InvalidOperationException("Worksheet yet created");

            if (position != -1)
                if (position > WorksheetCount + 1)
                    throw new InvalidOperationException("Worksheet not in correct range");

            #endregion

            // il nome del worksheet, se non specificato, viene calcolato utilizzando il numero di worksheet presenti
            if (String.IsNullOrEmpty(worksheetName))
                worksheetName = String.Format("Foglio {0}", WorksheetCount);

            // generazione del nuovo foglio di lavoro all'interno del workbook
            var worksheet = ExcelWorkbook.Workbook.Worksheets.Add(worksheetName);

            //byte[] companyLogo = RepoManager.ParamRepo.ParametersRow.CompanyLogo;
            //Image image = null;
            //if (companyLogo != null)
            //{
            //    image = Image.FromStream(new MemoryStream(companyLogo));
            //}        
            //var picture = worksheet.Drawings.AddPicture("Immagine 1", image);
            //picture.SetSize(100,100);
            //
            //picture.SetPosition(7,0,7,0);

            // se è richiesta una specifica posizione allora si procede al riposizionamento dei fogli nella lista
            if (position != -1)
                ExcelWorkbook.Workbook.Worksheets.MoveAfter(WorksheetCount - 1, position);
        }

        /// <summary>
        /// Cancella il worksheet specificato dal workbook.
        /// </summary>
        /// <param name="position">La posizione in cui cancellare il worksheet.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// </exception>
        protected void WorksheetDelete(int position)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (position > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            #endregion

            // cancellazione del worksheet alla posizione indicata
            ExcelWorkbook.Workbook.Worksheets.Delete(position);
        }

        /// <summary>
        /// Cancella il worksheet specificato dal workbook.
        /// </summary>
        /// <param name="worksheetName">Il nome che identifica il worksheet da cancellare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// </exception>
        protected void WorksheetDelete(string worksheetName)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // il nome foglio richiesto sia presente nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            #endregion

            // cancellazione del worksheet alla posizione indicata
            WorksheetDelete(WorksheetFromNameToPosition(worksheetName));
        }

        /// <summary>
        /// Rinomina il foglio di lavoro specificato con il nuovo nome specificato.
        /// </summary>
        /// <param name="position">La posizione del worksheet da rinominare.</param>
        /// <param name="newName">Il nuovo nome da associare al worksheet.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Worksheet new name yet present
        /// </exception>
        protected void WorksheetRename(int position, string newName)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato,
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il nuovo nome non sia già presente
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (position > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (WorksheetIsCreated(newName))
                throw new InvalidOperationException("Worksheet new name yet present");

            #endregion

            // rinominazione del foglio di lavoro
            ExcelWorkbook.Workbook.Worksheets[position].Name = newName;
        }

        /// <summary>
        /// Rinomina il foglio di lavoro specificato con il nuovo nome specificato.
        /// </summary>
        /// <param name="currentWorksheetName">Il nome del worksheet da rinominare.</param>
        /// <param name="newName">Il nuovo nome da associare al worksheet.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Worksheet new name yet present
        /// </exception>
        protected void WorksheetRename(string currentWorksheetName, string newName)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // il nome foglio richiesto sia presente nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(currentWorksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (WorksheetIsCreated(newName))
                throw new InvalidOperationException("Worksheet new name yet present");

            #endregion

            // rinominazione del foglio di lavoro
            WorksheetRename(WorksheetFromNameToPosition(currentWorksheetName), newName);
        }

        /// <summary>
        /// Copia il contenuto di un un worksheet in un altro worksheet.
        /// </summary>
        /// <param name="sourceWorksheetName">Il nome del worksheet origine da cui copiare i dati.</param>
        /// <param name="destinationWorksheetName">Il nome del worksheet di destinazione in cui copiare i dati.</param>
        /// <param name="position">La posizione in cui inserire il nuovo worksheet; se non specificato il worksheet sarà accodato agli esistenti</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Source worksheet not present
        /// or
        /// Destination worksheet not present
        /// </exception>
        protected void WorksheetCopy(string sourceWorksheetName, string destinationWorksheetName, int position = -1)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // il foglio excel di partenza e destinazione siano presenti
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(sourceWorksheetName))
                throw new InvalidOperationException("Source worksheet not present");

            if (WorksheetIsCreated(destinationWorksheetName))
                throw new InvalidOperationException("Worksheet new name yet present");

            if (position != -1)
                if (position > WorksheetCount + 1)
                    throw new InvalidOperationException("Worksheet not in correct range");

            #endregion

            // per effettuare la copia del contenuto dal worksheet di partenza al worksheet di destinazione è necessario:
            // 1. Copiare il contenuto del worksheet di partenza nel nuovo worksheet generato automaticamente dal metodo di copia
            // 2. Sposto il worksheet copiato nella posizione indicata
            ExcelWorkbook.Workbook.Worksheets.Copy(sourceWorksheetName, destinationWorksheetName);
            if (position != -1)
                ExcelWorkbook.Workbook.Worksheets.MoveAfter(WorksheetCount - 1, position);
        }

        /// <summary>
        /// Imposta l'orientamento di stampa di uno specifico worksheet.
        /// </summary>
        /// <param name="position">La posizione del worksheet di cui impostare l'orientamento.</param>
        /// <param name="orientation">L'orientamento da impostare sullo sepcifico worksheet.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// </exception>
        protected void WorksheetSetOrientation(int position, eOrientation orientation)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (position > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            #endregion

            // impostazione dell'orientamento sul foglio di lavoro desiderato
            ExcelWorkbook.Workbook.Worksheets[position].PrinterSettings.Orientation = orientation;
        }

        /// <summary>
        /// Imposta l'orientamento di stampa di uno specifico worksheet.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet di cui impostare l'orientamento.</param>
        /// <param name="orientation">L'orientamento da impostare sullo sepcifico worksheet.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet name not present
        /// </exception>
        protected void WorksheetSetOrientation(string worksheetName, eOrientation orientation)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet name not present");

            #endregion

            // impostazione dell'orientamento sul foglio di lavoro desiderato
            WorksheetSetOrientation(WorksheetFromNameToPosition(worksheetName), orientation);
        }

        /// <summary>
        /// Imposta lo zoom e la centratura specificata sul foglio di lavoro indicato.
        /// </summary>
        /// <param name="position">La posizione del foglio di lavoro da processare.</param>
        /// <param name="zoomValue">Il valore dello zoom da impostare.</param>
        /// <param name="centerHorizontaly">Se valorizzato a <c>true</c>, dopo l'applicazione dello zoom sarà effettuata una centratura orizzontale della visualizzazione del foglio di lavoro.</param>
        /// <param name="centerVerticaly">Se valorizzato a <c>true</c>, dopo l'applicazione dello zoom sarà effettuata una centratura verticale della visualizzazione del foglio di lavoro.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// </exception>
        protected void WorksheetSetZoomAndCenter(int position, int zoomValue, bool centerHorizontaly, bool centerVerticaly)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (position > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            #endregion

            // impostazione dello zoom sul worksheet desiderato (in visualizzazione e in stampa)
            ExcelWorkbook.Workbook.Worksheets[position].View.ZoomScale = zoomValue;
            ExcelWorkbook.Workbook.Worksheets[position].PrinterSettings.Scale = zoomValue;

            // impostazione delle centrature in stampa dopo l'applicazione dello zoom
            ExcelWorkbook.Workbook.Worksheets[position].PrinterSettings.HorizontalCentered = centerHorizontaly;
            ExcelWorkbook.Workbook.Worksheets[position].PrinterSettings.VerticalCentered = centerVerticaly;
        }

        /// <summary>
        /// Imposta lo zoom e la centratura specificata sul foglio di lavoro indicato.
        /// </summary>
        /// <param name="worksheetName">La posizione del foglio di lavoro da processare.</param>
        /// <param name="zoomValue">Il valore dello zoom da impostare.</param>
        /// <param name="centerHorizontaly">Se valorizzato a <c>true</c>, dopo l'applicazione dello zoom sarà effettuata una centratura orizzontale della visualizzazione del foglio di lavoro.</param>
        /// <param name="centerVerticaly">Se valorizzato a <c>true</c>, dopo l'applicazione dello zoom sarà effettuata una centratura verticale della visualizzazione del foglio di lavoro.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet name not present
        /// </exception>
        protected void WorksheetSetZoomAndCenter(string worksheetName, int zoomValue, bool centerHorizontaly, bool centerVerticaly)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet name not present");

            #endregion

            WorksheetSetZoomAndCenter(WorksheetFromNameToPosition(worksheetName), zoomValue, centerHorizontaly, centerVerticaly);
        }

        /// <summary>
        /// Imposta i margini indicati sul worksheet specificato.
        /// </summary>
        /// <param name="position">La posizione del worksheet di cui impostare i margini.</param>
        /// <param name="marginTop">Il valore del margine superiore.</param>
        /// <param name="marginBottom">Il valore del margine inferiore.</param>
        /// <param name="marginLeft">Il valore del margine a sinistra.</param>
        /// <param name="marginRight">Il valore del margine a destra.</param>
        /// <param name="marginHeader">Il valore del margine rispetto all'intestazione.</param>
        /// <param name="marginFooter">Il valore del margine rispetto al piè di pagina.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// </exception>
        protected void WorksheetSetMargins(int position, int marginTop, int marginBottom, int marginLeft, int marginRight, int marginHeader, int marginFooter)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (position > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            #endregion

            // impostazione dei margini passati come parametro
            ExcelWorkbook.Workbook.Worksheets[position].PrinterSettings.TopMargin = marginTop;
            ExcelWorkbook.Workbook.Worksheets[position].PrinterSettings.BottomMargin = marginBottom;
            ExcelWorkbook.Workbook.Worksheets[position].PrinterSettings.LeftMargin = marginLeft;
            ExcelWorkbook.Workbook.Worksheets[position].PrinterSettings.RightMargin = marginRight;
            ExcelWorkbook.Workbook.Worksheets[position].PrinterSettings.HeaderMargin = marginHeader;
            ExcelWorkbook.Workbook.Worksheets[position].PrinterSettings.FooterMargin = marginFooter;
        }

        /// <summary>
        /// Imposta i margini indicati sul worksheet specificato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet di cui impostare i margini.</param>
        /// <param name="marginTop">Il valore del margine superiore.</param>
        /// <param name="marginBottom">Il valore del margine inferiore.</param>
        /// <param name="marginLeft">Il valore del margine a sinistra.</param>
        /// <param name="marginRight">Il valore del margine a destra.</param>
        /// <param name="marginHeader">Il valore del margine rispetto all'intestazione.</param>
        /// <param name="marginFooter">Il valore del margine rispetto al piè di pagina.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet name not present
        /// </exception>
        protected void WorksheetSetMargins(string worksheetName, int marginTop, int marginBottom, int marginLeft, int marginRight, int marginHeader, int marginFooter)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet name not present");

            #endregion

            WorksheetSetMargins(WorksheetFromNameToPosition(worksheetName), marginTop, marginBottom, marginLeft, marginRight, marginHeader, marginFooter);
        }

        protected ExcelWorksheet WorksheetLoadFromModel(string modelFilePath, int index)
        {
            #region Convalida input del metodo

            // convalida input del metodo: il percorso passato come parametro non può essere vuoto e deve essere presente e raggiungibile il file
            if (String.IsNullOrEmpty(modelFilePath))
                throw new ArgumentNullException("modelFilePath", BusinessService.GetLocalizedString(PowerWebResources.ERR_PERCORSO_FILE_MODELLO_EXCEL_NON_VALIDO));

            string completePath = AppDomain.CurrentDomain.BaseDirectory + "ExcelModels" + "\\" + modelFilePath;

            if (!File.Exists(completePath))
                throw new FileNotFoundException(BusinessService.GetLocalizedString(PowerWebResources.ERR_PERCORSO_FILE_MODELLO_EXCEL_NON_VALIDO));

            #endregion

            // inizializzazione del nuovo foglio excel a partire dal modello;
            // nella generazione del file si utilizza uno stream per non tenere eventualmente il lock
            // sul file modello (che potrebbe essere utilizzato da altri utenti dell'applicativo)
            FileInfo modelFileInfo = new FileInfo(completePath);
            return new ExcelPackage(modelFileInfo, true).Workbook.Worksheets[index];



        }

        #endregion

        #region Data toolbox

        /// <summary>
        /// Inserisce un valore nel worksheet specifico nella cella specifica.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet in cui inserire il dato.</param>
        /// <param name="column">L'indice della colonna in cui inserire il dato.</param>
        /// <param name="row">L'indice della riga in cui inserire il dato.</param>
        /// <param name="valueToInsert">Il valore da inserire nella cella.</param>
        /// <param name="insertType">Il tipo di inserimento da effettuare (dato, formula, numero in formato ora)</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// </exception>
        protected void CellInsertValue(int worksheetPosition, int column, int row, object valueToInsert, ExcelInsertTypeEnum insertType)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            #endregion

            string stringValue = Convert.ToString(valueToInsert);
            if (stringValue.Contains(',')) stringValue = stringValue.Replace(',','.');

            // inserimento nel worksheet del valore da inserire in base al tipo di inserimento
            if (insertType == ExcelInsertTypeEnum.Formula) // si sta inserendo una formula
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[row, column].Formula = stringValue;
            else // si sta inserendo un valore
            {
                //Se il valore è convertibile a intero alla cella viene assegnato il valore intero
                if (int.TryParse(stringValue, out int intValue))
                    ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[row, column].Value = intValue;

                //Viene altrimenti controllato se è convertibile a double
                else if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
                {
                    ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[row, column].Value = doubleValue;
                    ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[row, column].Style.Numberformat.Format = "0.00";
                }
                //In alternativa viene assegnato il valore a stringa
                else
                    ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[row, column].Value = stringValue;

                if (insertType == ExcelInsertTypeEnum.HhmmTime) // se è richiesto un formato particolare
                {
                    ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[row, column].Style.Numberformat.Format = "[h]:mm"; // applicazione del formato hh:mm
                                                                                                                                  // ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[row, column].Style.HorizontalAlignment= (OfficeOpenXml.Style.ExcelHorizontalAlignment)XlHAlign.xlHAlignRight;
                }

                else if (insertType == ExcelInsertTypeEnum.HhmmssTime)
                {
                    ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[row, column].Style.Numberformat.Format = "hh:mm:ss"; // applicazione del formato hh:mm:ss
                                                                                                                                    // ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[row, column].Style.HorizontalAlignment= (OfficeOpenXml.Style.ExcelHorizontalAlignment)XlHAlign.xlHAlignRight;
                }
            }
        }

        /// <summary>
        /// Inserisce un valore nel worksheet specifico nella cella specifica.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet in cui inserire il dato.</param>
        /// <param name="column">L'indice della colonna in cui inserire il dato.</param>
        /// <param name="row">L'indice della riga in cui inserire il dato.</param>
        /// <param name="valueToInsert">Il valore da inserire nella cella.</param>
        /// <param name="insertType">Il tipo di inserimento da effettuare (dato, formula, numero in formato ora)</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// </exception>
        protected void CellInsertValue(string worksheetName, int column, int row, object valueToInsert, ExcelInsertTypeEnum insertType)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            #endregion

            CellInsertValue(WorksheetFromNameToPosition(worksheetName), column, row, valueToInsert, insertType);
        }

        /// <summary>
        /// Inserisce un valore nel worksheet specifico nel range specifico specifica.
        /// </summary>
        /// <param name="worksheetPosition">La poszione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di partenza del range.</param>
        /// <param name="startCellRow">La riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="valueToInsert">Il valore da inserire nella range.</param>
        /// <param name="insertType">Il tipo di inserimento da effettuare (dato, formula, numero in formato ora)</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeInsertValue(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, object valueToInsert, ExcelInsertTypeEnum insertType)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // inserimento nel worksheet del valore da inserire in base al tipo di inserimento
            if (insertType == ExcelInsertTypeEnum.Formula) // si sta inserendo una formula
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Formula = Convert.ToString(valueToInsert);
            else // si sta inserendo un valore
            {
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Value = valueToInsert; // inserimento del valore
                if (insertType == ExcelInsertTypeEnum.HhmmTime) // se è richiesto un formato particolare
                {
                    ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Numberformat.Format = "[h]:mm;@"; // applicazione del formato hh:mm
                }
                else if (insertType == ExcelInsertTypeEnum.HhmmssTime)
                {
                    ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Numberformat.Format = "hh:mm:ss"; // applicazione del formato hh:mm:ss
                }
            }
        }

        /// <summary>
        /// Inserisce un valore nel worksheet specifico nel range specifico specifica.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di partenza del range.</param>
        /// <param name="startCellRow">La riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="valueToInsert">Il valore da inserire nella range.</param>
        /// <param name="insertType">Il tipo di inserimento da effettuare (dato, formula, numero in formato ora)</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeInsertValue(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, object valueToInsert, ExcelInsertTypeEnum insertType)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione del formato di valori sul range
            RangeInsertValue(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow, valueToInsert, insertType);
        }

        /// <summary>
        /// Imposta nel worksheet specificato per la cella specificata il number format indicato.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet in cui inserire il dato.</param>
        /// <param name="column">L'indice della colonna in cui inserire il dato.</param>
        /// <param name="row">L'indice della riga in cui inserire il dato.</param>
        /// <param name="numberFormatToInsert">Il number format da applicare alla cella specificata.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// </exception>
        protected void CellSetNumberFormat(int worksheetPosition, int column, int row, string numberFormatToInsert)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            #endregion

            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[row, column].Style.Numberformat.Format = numberFormatToInsert;
        }

        /// <summary>
        /// Imposta nel worksheet specificato per la cella specificata il number format indicato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet in cui inserire il dato.</param>
        /// <param name="column">L'indice della colonna in cui inserire il dato.</param>
        /// <param name="row">L'indice della riga in cui inserire il dato.</param>
        /// <param name="numberFormatToInsert">Il number format da applicare alla cella specificata.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// </exception>
        protected void CellSetNumberFormat(string worksheetName, int column, int row, string numberFormatToInsert)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            #endregion

            CellSetNumberFormat(WorksheetFromNameToPosition(worksheetName), column, row, numberFormatToInsert);
        }

        /// <summary>
        /// Legge e restituisce il valore di una cella specifica nel worksheet indicato.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet in cui inserire il dato.</param>
        /// <param name="column">L'indice della colonna in cui inserire il dato.</param>
        /// <param name="row">L'indice della riga in cui inserire il dato.</param>
        /// <returns>Il valore letto dalla cella (in caso di formula viene letto il valore calcolato)</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// </exception>
        protected object CellReadValue(int worksheetPosition, int column, int row)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            #endregion

            // lettura del valore e ritorno di quanto letto
            return ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[row, column].Value;
        }

        /// <summary>
        /// Legge e restituisce il valore di una cella specifica nel worksheet indicato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet in cui inserire il dato.</param>
        /// <param name="column">L'indice della colonna in cui inserire il dato.</param>
        /// <param name="row">L'indice della riga in cui inserire il dato.</param>
        /// <returns>Il valore letto dalla cella (in caso di formula viene letto il valore calcolato)</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// </exception>
        protected object CellReadValue(string worksheetName, int column, int row)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            #endregion

            return CellReadValue(WorksheetFromNameToPosition(worksheetName), column, row);
        }

        #endregion

        #region Columns and Rows toolbox

        /// <summary>
        /// Rimuove dal worksheet indicato un range di righe.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="rowIndexFrom">La posizione della prima riga da cancellare.</param>
        /// <param name="rowIndexTo">La posizione dell'ultima riga da cancellare</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Row range not valid
        /// </exception>
        protected void RowsRemove(int worksheetPosition, int rowIndexFrom, int rowIndexTo)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (rowIndexFrom < rowIndexTo)
                throw new InvalidOperationException("Row range not valid");

            #endregion

            // rimozione della riga richiesta
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].DeleteRow(rowIndexFrom, 1);
        }

        /// <summary>
        /// Rimuove dal worksheet indicato un range di righe.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="rowIndexFrom">La posizione della riga da cancellare.</param>
        /// <param name="rowIndexTo">La posizione dell'ultima riga da cancellare</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Row range not valid
        /// </exception>
        protected void RowsRemove(string worksheetName, int rowIndexFrom, int rowIndexTo)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato e 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (rowIndexFrom < rowIndexTo)
                throw new InvalidOperationException("Row range not valid");

            #endregion

            // rimozione della riga richiesta
            RowsRemove(WorksheetFromNameToPosition(worksheetName), rowIndexFrom, rowIndexTo);
        }

        /// <summary>
        /// Imposta l'altezza per un range di righe all'interno di uno specifico worksheet.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="rowIndexFrom">L'indice della prima riga di cui modificare l'altezza.</param>
        /// <param name="rowIndexTo">L'indice dell'ultima riga di cui modificare l'altezza.</param>
        /// <param name="newRowHeight">La nuova altezza delle righe da impostare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Row range not valid
        /// </exception>
        protected void RowsSetHeight(int worksheetPosition, int rowIndexFrom, int rowIndexTo, double newRowHeight)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (rowIndexFrom < rowIndexTo)
                throw new InvalidOperationException("Row range not valid");

            #endregion

            // ciclo di elaborazione del range passato come parametro
            for (int i = rowIndexFrom; i <= rowIndexTo; i++)
            {
                // per ogni riga che si processa, si va a impostare la nuova altezza
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Row(i).Height = newRowHeight;
            }
        }

        /// <summary>
        /// Imposta l'altezza per un range di righe all'interno di uno specifico worksheet.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="rowIndexFrom">L'indice della prima riga di cui modificare l'altezza.</param>
        /// <param name="rowIndexTo">L'indice dell'ultima riga di cui modificare l'altezza.</param>
        /// <param name="newRowHeight">La nuova altezza delle righe da impostare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Row range not valid
        /// </exception>
        protected void RowsSetHeight(string worksheetName, int rowIndexFrom, int rowIndexTo, double newRowHeight)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (rowIndexFrom < rowIndexTo)
                throw new InvalidOperationException("Row range not valid");

            #endregion

            RowsSetHeight(WorksheetFromNameToPosition(worksheetName), rowIndexFrom, rowIndexTo, newRowHeight);
        }

        /// <summary>
        /// Modifica la larghezza del range di colonne specificato per il worksheet indicato.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="columnIndexFrom">L'indice della prima colonna da modificare.</param>
        /// <param name="columnIndexTo">L'indice dell'ultima colonna da modificare.</param>
        /// <param name="newColumnWidth">La larghezza della colonna da modificare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Column range not valid
        /// </exception>
        protected void ColumnsSetWidth(int worksheetPosition, int columnIndexFrom, int columnIndexTo, double newColumnWidth)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (columnIndexFrom > columnIndexTo)
                throw new InvalidOperationException("Column range not valid");

            #endregion

            // ciclo su tutto il range di colonne
            for (int i = columnIndexFrom; i <= columnIndexTo; i++)
            {
                // aggiorno la lunghezza della colonna
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Column(i).Width = newColumnWidth;
            }
        }

        /// <summary>
        /// Modifica la larghezza del range di colonne specificato per il worksheet indicato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="columnIndexFrom">L'indice della prima colonna da modificare.</param>
        /// <param name="columnIndexTo">L'indice dell'ultima colonna da modificare.</param>
        /// <param name="newColumnWidth">La larghezza della colonna da modificare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Column range not valid
        /// </exception>
        protected void ColumnsSetWidth(string worksheetName, int columnIndexFrom, int columnIndexTo, double newColumnWidth)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (columnIndexFrom > columnIndexTo)
                throw new InvalidOperationException("Column range not valid");

            #endregion

            ColumnsSetWidth(WorksheetFromNameToPosition(worksheetName), columnIndexFrom, columnIndexTo, newColumnWidth);
        }

        /// <summary>
        /// Imposta il range di colonne specificate per lo specifico worksheet con il dimensionamento automatico.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da ricercare.</param>
        /// <param name="columnIndexFrom">L'indice della prima colonna da impostare.</param>
        /// <param name="columnIndexTo">L'indice dell'ultima colonna da impostare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Column range not valid
        /// </exception>
        protected void ColumnsSetAutoWidth(int worksheetPosition, int columnIndexFrom, int columnIndexTo)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (columnIndexFrom > columnIndexTo)
                throw new InvalidOperationException("Column range not valid");

            #endregion

            // ciclo su tutto il range di colonne
            for (int i = columnIndexFrom; i <= columnIndexTo; i++)
            {
                // imposto la colonna per il mantenimento della larghezza automatica
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Column(i).AutoFit();
            }
        }

        /// <summary>
        /// Imposta il range di colonne specificate per lo specifico worksheet con il dimensionamento automatico.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="columnIndexFrom">L'indice della prima colonna da impostare.</param>
        /// <param name="columnIndexTo">L'indice dell'ultima colonna da impostare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Column range not valid
        /// </exception>
        protected void ColumnsSetAutoWidth(string worksheetName, int columnIndexFrom, int columnIndexTo)
        {

            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (columnIndexFrom > columnIndexTo)
                throw new InvalidOperationException("Column range not valid");

            #endregion

            ColumnsSetAutoWidth(WorksheetFromNameToPosition(worksheetName), columnIndexFrom, columnIndexTo);

        }

        #endregion

        #region Range toolbox

        /// <summary>
        /// Unisce nel worksheet specificato le celle indicate dal range.
        /// </summary>
        /// <param name="worksheetPosition">La poszione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di partenza del range.</param>
        /// <param name="startCellRow">La riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeUnion(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // merge delle celle
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Merge = true;
        }

        /// <summary>
        /// Unisce nel worksheet specificato le celle indicate dal range.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di partenza del range.</param>
        /// <param name="startCellRow">La riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeUnion(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            RangeUnion(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow);
        }

        /// <summary>
        /// Copia un range in un altro range (anche su worksheet diversi).
        /// </summary>
        /// <param name="sourceRangeWorksheetPosition">La posizione del worksheet origine del dato.</param>
        /// <param name="destinationRangeWorksheetPosition">La posizione del worksheet destinazione del dato.</param>
        /// <param name="sourceStartCellColumn">La colonna della cella di inizio del range origine.</param>
        /// <param name="sourceStartCellRow">La riga della cella di inizio del range origine.</param>
        /// <param name="sourceEndCellColumn">La colonna della cella di fine del range origine.</param>
        /// <param name="sourceEndCellRow">La riga della cella di fine del range origine.</param>
        /// <param name="destinationCellColumn">La colonna della cella di inizio destinazione</param>
        /// <param name="destinationCellRow">La riga della cella di inizio destinazione</param>
        protected void RangeCopy(int sourceRangeWorksheetPosition, int destinationRangeWorksheetPosition, int sourceStartCellColumn, int sourceStartCellRow, int sourceEndCellColumn,
            int sourceEndCellRow, int destinationCellColumn, int destinationCellRow)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (sourceRangeWorksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Source worksheet position not in correct range");

            if (destinationRangeWorksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Destination worksheet position not in correct range");

            if (sourceStartCellColumn > sourceEndCellColumn || sourceStartCellRow > sourceEndCellRow)
                throw new InvalidOperationException("Source range not valid");

            #endregion

            ExcelWorkbook.Workbook.Worksheets[sourceRangeWorksheetPosition].Cells[sourceStartCellRow, sourceStartCellColumn, sourceEndCellRow, sourceEndCellColumn].Copy(
                ExcelWorkbook.Workbook.Worksheets[destinationRangeWorksheetPosition].Cells[destinationCellRow, destinationCellColumn]);
        }

        /// <summary>
        /// Copia un range in un altro range (anche su worksheet diversi).
        /// </summary>
        /// <param name="sourceRangeWorksheetName">Il nome del worksheet origine del dato.</param>
        /// <param name="destinationRangeWorksheetName">Il nome del worksheet destinazione del dato.</param>
        /// <param name="sourceStartCellColumn">La colonna della cella di inizio del range origine.</param>
        /// <param name="sourceStartCellRow">La riga della cella di inizio del range origine.</param>
        /// <param name="sourceEndCellColumn">La colonna della cella di fine del range origine.</param>
        /// <param name="sourceEndCellRow">La riga della cella di fine del range origine.</param>
        /// <param name="destinationCellColumn">La colonna della cella di inizio destinazione</param>
        /// <param name="destinationCellRow">La riga della cella di inizio destinazione</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Source worksheet position not in correct range
        /// or
        /// Destination worksheet position not in correct range
        /// or
        /// Source range not valid
        /// </exception>
        protected void RangeCopy(string sourceRangeWorksheetName, string destinationRangeWorksheetName, int sourceStartCellColumn, int sourceStartCellRow, int sourceEndCellColumn,
            int sourceEndCellRow, int destinationCellColumn, int destinationCellRow)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(sourceRangeWorksheetName))
                throw new InvalidOperationException("Source worksheet position not in correct range");

            if (!WorksheetIsCreated(destinationRangeWorksheetName))
                throw new InvalidOperationException("Destination worksheet position not in correct range");

            if (sourceStartCellColumn > sourceEndCellColumn || sourceStartCellRow > sourceEndCellRow)
                throw new InvalidOperationException("Source range not valid");

            #endregion

            RangeCopy(WorksheetFromNameToPosition(sourceRangeWorksheetName), WorksheetFromNameToPosition(destinationRangeWorksheetName), sourceStartCellColumn, sourceStartCellRow, sourceEndCellColumn, sourceEndCellRow, destinationCellColumn, destinationCellRow);

        }


        /// <summary>
        /// Imposta il formato di visualizzazione nel range specificato.
        /// </summary>
        /// <param name="worksheetPosition">La poszione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di partenza del range.</param>
        /// <param name="startCellRow">La riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="valuesFormat">Il formato da applicare ai valori del range</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetValueFormat(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, string valuesFormat)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione del formato di valori sul range
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Numberformat.Format = valuesFormat;
        }

        /// <summary>
        /// Imposta il formato di visualizzazione nel range specificato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di partenza del range.</param>
        /// <param name="startCellRow">La riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="valuesFormat">Il formato da applicare ai valori del range</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetValueFormat(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, string valuesFormat)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione del formato di valori sul range
            RangeSetValueFormat(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow, valuesFormat);
        }

        #endregion

        #region Color and style toolbox

        /// <summary>
        /// Imposta il colore specificato di font sul range indicato.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="colorToApply">Il colore da applicare al font del range.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetFontColor(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, Color colorToApply)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione del colore sul range
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Font.Color.SetColor(colorToApply);
        }

        /// <summary>
        /// Imposta il colore specificato di font sul range indicato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="colorToApply">Il colore da applicare al font del range.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetFontColor(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, Color colorToApply)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            RangeSetFontColor(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow, colorToApply);

        }

        /// <summary>
        /// Imposta il colore specificato di sfondo sul range indicato.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="colorToApply">Il colore da applicare al font del range.</param>
        /// <param name="fillStyle">Il tipo di riempimento colore da applicare al range.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetBackgroundColor(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, Color colorToApply, ExcelFillStyle fillStyle)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione del colore sul range
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Fill.PatternType = fillStyle;
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Fill.BackgroundColor.SetColor(colorToApply);
        }

        /// <summary>
        /// Imposta il colore specificato di sfondo sul range indicato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="colorToApply">Il colore da applicare al font del range.</param>
        /// <param name="fillStyle">Il tipo di riempimento colore da applicare al range.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetBackgroundColor(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, Color colorToApply, ExcelFillStyle fillStyle)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            RangeSetBackgroundColor(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow, colorToApply, fillStyle);

        }

        /// <summary>
        /// Imposta il caratttere bold per il range specificato.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetFontBold(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione del carattere bold
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Font.Bold = true;
        }

        protected void RangeSetFontUnderline(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione del carattere bold
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Font.UnderLine = true;
        }

        /// <summary>
        ///Imposta il caratttere bold per il range specificato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetFontBold(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            RangeSetFontBold(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow);

        }

        /// <summary>
        /// Imposta la dimensione del carattere per il range specificato.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="fontSize">La dimensione del font da impostare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetFontSize(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, int fontSize)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione della dimensione del font
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Font.Size = fontSize;
        }

        /// <summary>
        /// Imposta la dimensione del carattere per il range specificato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="fontSize">La dimensione del font da impostare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetFontSize(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, int fontSize)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            RangeSetFontSize(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow, fontSize);

        }

        /// <summary>
        /// Imposta i bordi come specificato per il range indicato.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="topBorderColor">Il colore del bordo superiore.</param>
        /// <param name="topBorderStyle">Lo stile del bordo superiore.</param>
        /// <param name="bottomBorderColor">Il colore del bordo inferiore.</param>
        /// <param name="bottomBorderStyle">Lo stile del bordo inferiore.</param>
        /// <param name="leftBorderColor">Il colore del bordo sinistro.</param>
        /// <param name="leftBorderStyle">Lo stile del bordo sinistro.</param>
        /// <param name="rightBorderColor">Il colore del bordo destro.</param>
        /// <param name="rightBorderStyle">Lo stile del bordo destro.</param>
        /// <exception cref="System.InvalidOperationException">Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid</exception>
        protected void RangeSetBorders(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow,
            Color topBorderColor, ExcelBorderStyle topBorderStyle, Color bottomBorderColor, ExcelBorderStyle bottomBorderStyle,
            Color leftBorderColor, ExcelBorderStyle leftBorderStyle, Color rightBorderColor, ExcelBorderStyle rightBorderStyle)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione dei bordi sul range indicato
            if (topBorderStyle != ExcelBorderStyle.None)
            {
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Border.Top.Style = topBorderStyle;
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Border.Top.Color.SetColor(topBorderColor);
            }
            if (bottomBorderStyle != ExcelBorderStyle.None)
            {
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Border.Bottom.Style = bottomBorderStyle;
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Border.Bottom.Color.SetColor(bottomBorderColor);
            }
            if (leftBorderStyle != ExcelBorderStyle.None)
            {
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Border.Left.Style = leftBorderStyle;
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Border.Left.Color.SetColor(leftBorderColor);
            }
            if (rightBorderStyle != ExcelBorderStyle.None)
            {
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Border.Right.Style = rightBorderStyle;
                ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.Border.Right.Color.SetColor(rightBorderColor);
            }

        }

        /// <summary>
        /// Imposta i bordi come specificato per il range indicato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="topBorderColor">Il colore del bordo superiore.</param>
        /// <param name="topBorderStyle">Lo stile del bordo superiore.</param>
        /// <param name="bottomBorderColor">Il colore del bordo inferiore.</param>
        /// <param name="bottomBorderStyle">Lo stile del bordo inferiore.</param>
        /// <param name="leftBorderColor">Il colore del bordo sinistro.</param>
        /// <param name="leftBorderStyle">Lo stile del bordo sinistro.</param>
        /// <param name="rightBorderColor">Il colore del bordo destro.</param>
        /// <param name="rightBorderStyle">Lo stile del bordo destro.</param>
        /// <exception cref="System.InvalidOperationException">Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid</exception>
        protected void RangeSetBorders(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow,
            Color topBorderColor, ExcelBorderStyle topBorderStyle, Color bottomBorderColor, ExcelBorderStyle bottomBorderStyle,
            Color leftBorderColor, ExcelBorderStyle leftBorderStyle, Color rightBorderColor, ExcelBorderStyle rightBorderStyle)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione dei bordi sul range indicato
            RangeSetBorders(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow, topBorderColor, topBorderStyle, bottomBorderColor, bottomBorderStyle, leftBorderColor, leftBorderStyle, rightBorderColor, rightBorderStyle);
        }

        /// <summary>
        /// Imposta l'allineamento verticale del testo sul range specificato.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="verticalAlignment">Il tipo allineamento verticale da impostare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetTextVerticalAlignment(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, ExcelVerticalAlignment verticalAlignment)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione dell'orientamento verticale del testo
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.VerticalAlignment = verticalAlignment;
        }

        /// <summary>
        /// Imposta l'allineamento verticale del testo sul range specificato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="verticalAlignment">Il tipo allineamento verticale da impostare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetTextVerticalAlignment(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, ExcelVerticalAlignment verticalAlignment)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            RangeSetTextVerticalAlignment(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow, verticalAlignment);

        }

        /// <summary>
        /// Imposta l'allineamento orizzontale del testo sul range specificato.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="horizontalAlignment">Il tipo allineamento orizzontale da impostare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetTextHorizontalAlignment(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, ExcelHorizontalAlignment horizontalAlignment)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione dell'orientamento verticale del testo
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.HorizontalAlignment = horizontalAlignment;
        }

        /// <summary>
        /// Imposta l'allineamento orizzontale del testo sul range specificato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="horizontalAlignment">Il tipo allineamento orizzontale da impostare.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetTextHorizontalAlignment(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, ExcelHorizontalAlignment horizontalAlignment)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            RangeSetTextHorizontalAlignment(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow, horizontalAlignment);

        }

        /// <summary>
        /// Imposta il comportamento di rieempimento del testo nella cella.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="wrapText"><c>true</c> se al termine dello spazio il testo andrà a capo; altrimenti <c>false</c>.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetWrapText(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, bool wrapText)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione della proprietà di wrap del testo
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.WrapText = wrapText;
        }

        /// <summary>
        /// Imposta il comportamento di rieempimento del testo nella cella.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="wrapText"><c>true</c> se al termine dello spazio il testo andrà a capo; altrimenti <c>false</c>.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetWrapText(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, bool wrapText)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione della proprietà di wrap del testo
            RangeSetWrapText(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow, wrapText);
        }

        /// <summary>
        /// Imposta l'orientamento (in gradi) del testo per il range indicato.
        /// </summary>
        /// <param name="worksheetPosition">La posizione del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="textOrientation">L'orientamento (in gradi) da applicare al testo del range.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet position not in correct range
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetTextOrientation(int worksheetPosition, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, int textOrientation)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (worksheetPosition > WorksheetCount)
                throw new InvalidOperationException("Worksheet position not in correct range");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione dell'orientamento del testo
            ExcelWorkbook.Workbook.Worksheets[worksheetPosition].Cells[startCellRow, startCellColumn, endCellRow, endCellColumn].Style.TextRotation = textOrientation;
        }

        /// <summary>
        /// Imposta l'orientamento (in gradi) del testo per il range indicato.
        /// </summary>
        /// <param name="worksheetName">Il nome del worksheet da processare.</param>
        /// <param name="startCellColumn">La colonna della cella di patenza del range.</param>
        /// <param name="startCellRow">la riga della cella di partenza del range.</param>
        /// <param name="endCellColumn">La colonna della cella di arrivo del range.</param>
        /// <param name="endCellRow">La riga della cella di arrivo del range.</param>
        /// <param name="textOrientation">L'orientamento (in gradi) da applicare al testo del range.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Excel workbook not initialized
        /// or
        /// Worksheet not present
        /// or
        /// Range not valid
        /// </exception>
        protected void RangeSetTextOrientation(string worksheetName, int startCellColumn, int startCellRow, int endCellColumn, int endCellRow, int textOrientation)
        {
            #region Convalida input del metodo

            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato, 
            // la posizione richiesta sia nel range dei worksheet presenti nel workbook e il range deve essere valido
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            if (!WorksheetIsCreated(worksheetName))
                throw new InvalidOperationException("Worksheet not present");

            if (startCellColumn > endCellColumn || startCellRow > endCellRow)
                throw new InvalidOperationException("Range not valid");

            #endregion

            // impostazione della proprietà di wrap del testo
            RangeSetTextOrientation(WorksheetFromNameToPosition(worksheetName), startCellColumn, startCellRow, endCellColumn, endCellRow, textOrientation);
        }

        #endregion

        #region Utilities toolbox

        /// <summary>
        /// Imposta sul workbook attualmente in processo il formato data 1904 o 1900 a seconda del parametro.
        /// </summary>
        /// <param name="date1904State">Se impostato a <c>true</c> viene configurato il formato data 1904; altrimenti viene impostato il formato data 1900.</param>
        protected void UtilitySetDate1904(bool date1904State)
        {
            // convalida input del metodo: affinché il metodo possa funzionare è necessario che il foglio excel sia istanziato
            if (ExcelWorkbook == null)
                throw new InvalidOperationException("Excel workbook not initialized");

            // TODO: attualmente non trovato valore da impostare; da verificare in caso di proplemi; codice access qua sotto
            // g_oExcelApp.ActiveWorkbook.Date1904 = ActiveState ActiveState è un booleano
            // da quest'articolo sembra che il problema con le ultime versioni sia risolto (la conversione delle date 1900 e 1904 viene fatto in automatico a seconda del sistema):
            // http://search.cpan.org/~jmcnamara/Excel-Writer-XLSX-0.05/lib/Excel/Writer/XLSX.pm#set_1904%28%29
            // comunque da tenere presente il problema
        }

        /// <summary>
        /// Converte l'indice di colonna passato come parametro nella lettera che ne rappresenta la colonna excel.
        /// </summary>
        /// <param name="columnIndex">L'indice numerico della colonna da convertire (in base 1).</param>
        /// <returns>il carattere corrispondente alla colonna access di riferimento.</returns>
        protected string ColumnIndexToNameConversion(int columnIndex)
        {
            // convalida input del metodo: l'indice di colonna deve essere maggiore o uguale a 0
            if (columnIndex <= 0)
                throw new ArgumentException("Index must be greater than 0");

            // inizializzazione del valore di ritorno del metodo
            string columnChars = String.Empty;

            // inizializzazione del dividendo
            int dividend = columnIndex;

            // fintanto che ci sono gruppi di 26 numeri da processare
            while (dividend > 0)
            {
                // calcolo il resto tra quanto devo processare il 26 (il numero di lettere)
                int mod = (dividend - 1) % 26;

                // calcolo della lettera aggiuntiva rispetto a quelle inserite
                columnChars = String.Format("{0}{1}", Convert.ToChar(65 + mod), columnChars);

                // ricalcolo delle rimanenti cifre da calcolare
                dividend = (int)((dividend - mod) / 26);
            }

            // ritorno del valore del metodo
            return columnChars;
        }

        /// <summary>
        /// Converte il nome di colonna passato come parametro nell'indice che ne rappresenta la posizione (in base 1).
        /// </summary>
        /// <param name="columnName">Il nome della colonna da convertire.</param>
        /// <returns>L'indice che rappresenta il nome della colonna passato come parametro (in base 1)</returns>
        protected int ColumnNameToColumnIndexConversion(string columnName)
        {
            // inizializzazione dell'array che conterrà l'elenco delle cifre
            var digits = new int[columnName.Length];

            // ciclo per tutta la lunghezza del nome colonna
            for (int i = 0; i < columnName.Length; ++i)
            {
                // calcolo dell'indice di colonna per il carattere specificato
                digits[i] = Convert.ToInt32(columnName[i]) - 64;
            }

            // calcolo della cifra totale (tenendo conto della posizione nell'array della cifra)
            int mul = 1;
            int res = 0;
            for (int pos = digits.Length - 1; pos >= 0; --pos)
            {
                res += digits[pos] * mul;
                mul *= 26;
            }

            // ritorno del valore del metodo
            return res;
        }

        #endregion

        #region Utility scrittura ore

        /// <summary>
        /// Converte i minuti totali passati come parametro in sessantesimi o centesimi (dipende da come è impostato l'export)
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        protected string FromTotalMinutesToFormattedType(int value)
        {
            string result = "";

            if (CentHours) //Centesimi
            {
                TimeSpan totalDuration = TimeSpan.FromMinutes(value);

                result = String.Format("{0}{1}.{2}", (totalDuration < TimeSpan.Zero ? "-" : ""), Math.Abs((totalDuration.Days * 24) + totalDuration.Hours), FromMinutesToCent(Math.Abs(totalDuration.Minutes)));

            }
            else //Sessantesimi
            {
                TimeSpan totalDuration = TimeSpan.FromMinutes(value);

                result = String.Format("{0}.{1}", (totalDuration.Days * 24) + totalDuration.Hours, Math.Abs(totalDuration.Minutes).ToString("00"));
            }

            return result;
        }

        /// <summary>
        /// Converte i minuti totali passati come parametro in sessantesimi o centesimi (dipende da come è impostato l'export)
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        protected string FromTotalMinutesToFormattedTypeVirgola(int value)
        {
            string result = "";

            if (CentHours) //Centesimi
            {
                TimeSpan totalDuration = TimeSpan.FromMinutes(value);

                result = String.Format("{0}{1},{2}", (totalDuration < TimeSpan.Zero ? "-" : ""), Math.Abs((totalDuration.Days * 24) + totalDuration.Hours), FromMinutesToCent(Math.Abs(totalDuration.Minutes)));

            }
            else //Sessantesimi
            {
                TimeSpan totalDuration = TimeSpan.FromMinutes(value);

                result = String.Format("{0},{1}", (totalDuration.Days * 24) + totalDuration.Hours, Math.Abs(totalDuration.Minutes).ToString("00"));
            }

            return result;
        }

        /// <summary>
        /// Converte i minuti totali passati come parametro in sessantesimi o centesimi (dipende da come è impostato l'export)
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        protected string FromTotalMinutesToFormattedTypeKomplett(int value)
        {
            string result = "";

            if (CentHours) //Centesimi
            {
                TimeSpan totalDuration = TimeSpan.FromMinutes(value);

                result = String.Format("{0}{1}.{2}", (totalDuration < TimeSpan.Zero ? "-" : ""), Math.Abs((totalDuration.Days * 24) + totalDuration.Hours), FromMinutesToCent(Math.Abs(totalDuration.Minutes)));

            }
            else //Sessantesimi
            {
                TimeSpan totalDuration = TimeSpan.FromMinutes(value);

                result = String.Format("{0}.{1}", (totalDuration.Days * 24) + totalDuration.Hours, Math.Abs(totalDuration.Minutes).ToString("00"));
            }

            return result;
        }

        protected string FromTotalMinutesToFormattedTypeRiposi(int value)
        {
            string result = "";

            if (CentHours) //Centesimi
            {
                TimeSpan totalDuration = TimeSpan.FromMinutes(value);

                result = String.Format("{0}{1}", (totalDuration < TimeSpan.Zero ? "-" : ""), Math.Abs((totalDuration.Days * 24) + totalDuration.Hours), FromMinutesToCent(Math.Abs(totalDuration.Minutes)));

            }
            else //Sessantesimi
            {
                TimeSpan totalDuration = TimeSpan.FromMinutes(value);

                result = String.Format("{0}", (totalDuration.Days * 24) + totalDuration.Hours, Math.Abs(totalDuration.Minutes).ToString("00"));
            }

            return result;
        }

        /// <summary>
        /// Trasforma i minuti in centesimi (risultato di due cifre)
        /// </summary>
        /// <param name="minutes">The minutes.</param>
        /// <returns></returns>
        protected string FromMinutesToCent(int minutes)
        {
            return ((minutes / 60.0) * 100).ToString("00");
        }

        protected bool GetCentHours() 
        {
            return CentHours;
        }
        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Dato il nome del worksheet nel ritorna la posizione all'interno del workbook.
        /// </summary>
        /// <param name="workhseetName">Il nome del worksheet da ricercare.</param>
        /// <returns>La posizione del worksheet nel workbook; -1 se non trovato</returns>
        private int WorksheetFromNameToPosition(string workhseetName)
        {
            // inizializzazione del valore di ritorno del metodo
            int returnPosition = -1;

            // ciclo su tutti i worksheet e quando il nome coincide restituisco la posizione
            int i = 0;
            foreach (var excelWorksheet in ExcelWorkbook.Workbook.Worksheets)
            {
                if (excelWorksheet.Name == workhseetName)
                {
                    returnPosition = i;
                    break;
                }

                i++;
            }

            return returnPosition != -1 ? ++returnPosition : returnPosition;
        }


        #endregion

    }
}
