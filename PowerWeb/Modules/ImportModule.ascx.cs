using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using DevExpress.Web.ASPxGridView;
using Domain.Extensions;
using Exports.ExportExcelSpecialized;
using log4net;
using Business.Repository;
using DevExpress.Web.ASPxUploadControl;
using System.IO;
using Common;
using DevExpress.Web.ASPxScheduler;
using DevExpress.XtraScheduler;
using Domain;
using Business.ExportExcelEngine;
using System.Drawing;
using Reports;
using System.Collections.Specialized;
using DevExpress.Web.ASPxMenu;
using DevExpress.Web.Data;
using System.Collections;
using DevExpress.Data.Filtering;
using Business;
using System.Web.UI;

namespace PowerWeb.Modules
{
    public partial class ImportModule : UserControl, ILogModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ElaborateModule));

        public ILog Log
        {
            get { return _log; }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            //Imposto i Nomi in Lingua delle Label e Button della Pagina Video
            btnUploadAccess.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_IMPORT_DA_POWER_ACCESS);
            lblResult.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_RESULT);
            btnUploadLogo.Text = BusinessService.GetLocalizedString(PowerWebResources.LBL_LANCIO_IMPORT_LOGO);
        }

        #region Upload del Database Access
        protected void upldAccess_FileUploadComplete(object sender, FileUploadCompleteEventArgs e)
        //Esegue l'UPLOAD dei Dati dal DATABASE ACCESS di POWER e/o COMO08 al SERVER
        {
            if (!String.IsNullOrEmpty(RepoManager.ParamRepo.ParametersRow.MDBPath))
            {
                //Costruisce il Percorso sul Server in cui caricare il File MDB da elaborare con il percorso previsto in PARAM + Coidce UTENTE
                string serverMapPath = Server.MapPath(String.Format("{0}_{1}.mdb", RepoManager.ParamRepo.ParametersRow.MDBPath, PowerWebContext.Current.User.Utenti_Id));

                // se non esiste la cartella in cui appoggiare l'access da importare, la si crea
                if (!Directory.Exists(Path.GetDirectoryName(serverMapPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(serverMapPath));
                }

                FileInfo fileInfo = new FileInfo(serverMapPath);
                if (!fileInfo.Exists)
                // Se il File sul SERVER NON Esiste esegue l'UPLOAD sul SERVER
                {
                    e.UploadedFile.SaveAs(serverMapPath);
                }
                else
                // SE il File sul SERVER ESISTE GIA' allora  lo CANCELLA e lo RICARICA
                {
                    fileInfo.Delete();
                    e.UploadedFile.SaveAs(serverMapPath);
                    //e.ErrorText = CommonServiceBiz.GetLocalizedString(PowerWebResources.STR_TITOLO) + "|" +
                    //       CommonServiceBiz.GetLocalizedString(PowerWebResources.STR_IL_FILE_X_DI_IMPORT_ESISTE_GIA, serverMapPath);
                }
            }
        }

        protected void cUplAccessCommand_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        //Dopo aver caricato sul Server il DB da elaborare Lancia fisicamente l'elaborazione dell'Import dei Dati dal DATABASE ACCESS (Uplodato sul Server) di POWER e/o COMO08 al DB SQL con i relativi Controlli
        //Il risultato restituisce 
        //  TIPO:   Error oppure qualunque altra cosa se NON erro (ERROR è un valore fisso che viene poi testato nella parte HTML per aprire o meno il POPUP di Errore
        //  TITLE   Testo da mettere come Titolo
        //  Message: Testo del Messaggio 
        {
            e.Result = "Error|Title|Message";
            //Verifica che il Percorso (NOTA BENE : é quello in cui salva sul SERVER il File da trattare) in PARAM non sia vuoto
            if (!String.IsNullOrEmpty(RepoManager.ParamRepo.ParametersRow.MDBPath))
            {
                string serverMapPath = Server.MapPath(String.Format("{0}_{1}.mdb", RepoManager.ParamRepo.ParametersRow.MDBPath, PowerWebContext.Current.User.Utenti_Id));
                FileInfo fileInfo = new FileInfo(serverMapPath);
                if (fileInfo.Exists)
                //Se sul Server esiste il File da elaborare, Lancia l'Import dei Dati
                //e poi segnala che l'Import è terminato e CANCELLA il File dal ServerMapPath
                {
                    BusinessService.ImportDataStatusDictionary.Add(PowerWebContext.Current.User, new KeyValuePair<double, string>(0, BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORT_INIZIATO)));

                    // Lancio IMPORT dei Dati da ACCESS POWER
                    var errors = BusinessService.ImportData(serverMapPath, ImportDBTypeEnum.mdb);

                    //All fine Ripulisce ImportDataStatusDictionary
                    if (BusinessService.ImportDataStatusDictionary.ContainsKey(PowerWebContext.Current.User))
                        BusinessService.ImportDataStatusDictionary.Remove(PowerWebContext.Current.User);
                    e.Result = "Done" + "|" + BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORT_TERMINATO);
                    fileInfo.Delete();

                    System.Threading.Thread.Sleep(2000);
                }
            }
        }

        protected void cUplAccessPing_Callback(object source, DevExpress.Web.ASPxCallback.CallbackEventArgs e)
        //Restituisce quanto caricato nell'ImportDataStatusDictionary dallla Routine di IMPORTDATA per ogni Tabella Terminata   
        //  StatusKey : Contiene il Nome della Tabella che si sta importando in quel momento                               
        //  Valore    : Contiene la Percentuale (calcolata in base al N° di Tabelle da caricare) di Caricamento rispetto al Totale
        {

            if (BusinessService.ImportDataStatusDictionary.ContainsKey(PowerWebContext.Current.User))
            //Se ci sono dati nel DictionaryStatus allora li carica nel Risultato da mostrare a Video
            {
                var status = BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User];
                e.Result = String.Format("{0}|{1}", status.Key, status.Value);
            }
        }
        #endregion

        #region Upload Logo Aziendale da registrare nella tabella PARAM da usare al posto del Logo WINIT

        protected void upldLogo_FileUploadComplete(object sender, FileUploadCompleteEventArgs e)
        {
            System.Drawing.Image logoImage = System.Drawing.Image.FromStream(e.UploadedFile.FileContent);
            byte[] bArray = (byte[])(new ImageConverter().ConvertTo(logoImage, typeof(byte[])));
            var paramRow = RepoManager.ParamRepo.First();
            paramRow.CompanyLogo = bArray;
            //RepoManager.ParamRepo.ParametersRow.CompanyLogo = bArray;
            RepoManager.ParamRepo.SaveChanges();
            RepoManager.ParamRepo.ResetParametersRow();
        }
        #endregion

    }
}