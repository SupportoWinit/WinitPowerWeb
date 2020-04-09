
using Domain;
using log4net;
using Newtonsoft.Json.Linq;
using System;

namespace Exports
{
    public class ExportContext
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ExportContext));

        private Tab_Excel_Model _model;
        private string[] _filter;
        private object[] _columns;
        private DateTime _exportMonth;

        private DateTime _from;
        private DateTime _to;

        private int[] _selectionKeys;

        private IExport exportInstance;

        public bool GenerateExport(ref JArray errors)
        {
            if (_model == null)
                throw new NullReferenceException("Nessun model settato all'interno del context");


            Type exportType = Type.GetType(String.Format("{0},Exports", _model.Nome_Specializzato), false);

            if (exportType == null)
            {
                _log.ErrorFormat(String.Format("Nessun export trovato per il tipo {0}", _model.Nome_Specializzato));
                return false;
            }

            try
            {
                exportInstance = Activator.CreateInstance(exportType) as IExport;

                exportInstance.SelectedIds = _selectionKeys;
                exportInstance.CentHours = _model.Selezione_Durata_Cent;
                exportInstance.ExportDate = _exportMonth;

                exportInstance.FromDate = _from;
                exportInstance.ToDate = _to;

                exportInstance.FileName = _model.FileName;
                exportInstance.Extension = _model.Extension;
                exportInstance.IsToZip = _model.Zip;
                exportInstance.ModelFilePath = _model.ModelFilePath;
                exportInstance.GridColumns = _columns;

                exportInstance.LaunchExport();
                exportInstance.SaveToFileSystem();

                if (exportInstance.Errors.Count > 0)
                    errors = JArray.FromObject(exportInstance.Errors);

                return true;

            }
            catch (Exception ex) when (ex.GetType() != typeof(System.Threading.ThreadAbortException))
            {
                _log.ErrorFormat("Errore durante la richiesta di export {0} con exception {1}", _model.Nome_Risorsa, ex.Message);

                return false;
            }
            finally
            {
                _model = null;
                _filter = null;
                _columns = null;
                _exportMonth = DateTime.MinValue;
                _selectionKeys = null;
            }

        }

        public object GetDownloadExportParams()
        {
            return new { path = exportInstance.DownloadPath, fileName = exportInstance.FileName, extension = exportInstance.Extension };
        }


        public void SetExport(Tab_Excel_Model model)
        {
            _model = model;
        }

        public void SetGridFilter(string[] filter)
        {
            _filter = filter;
        }

        public void SetGridColumns(object[] columns)
        {
            _columns = columns;
        }
        public void SetExportMonth(DateTime exportMonth)
        {
            _exportMonth = exportMonth;
        }

        public void SetExportDateFrom(DateTime from)
        {
            _from = from;
        }

        public void SetExportDateTo(DateTime to)
        {
            _to = to;
        }
        public void SetSelectedKeys(int[] selectionKeys)
        {
            _selectionKeys = selectionKeys;
        }


    }
}
