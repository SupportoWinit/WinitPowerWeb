using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.ImportModules
{
    public abstract class ImportBase
    {
        private int _rowIndex;

        private IEnumerable<string> _rows;
        private IDictionary<string, Int32> _header;
        private IDictionary<string, string> _errors;
        private int _fil_Id;
        private string[] _emptyRow;

        #region Properties


        protected int Fil_Id
        {
            get
            {
                return _fil_Id;
            }
        }

        protected string[] CurrentRow
        {
            get
            {
                return _emptyRow;
            }
            private set
            {
                _emptyRow = value;
            }
        }

        protected IDictionary<string, Int32> Header
        {
            get
            {
                if (_header == null)
                    _header = new Dictionary<string, Int32>();

                return _header;
            }
        }

        protected IEnumerable<string> Rows
        {
            get
            {
                return _rows;
            }
        }

        protected IEnumerable<string[]> DataRows
        {
            get
            {
                return _rows.Skip(1).Select(row => row.Split(';')).ToList();
            }
        }

        protected IDictionary<string, string> Errors
        {
            get
            {
                if (_errors == null)
                    _errors = new Dictionary<string, string>();

                return _errors;
            }
        }

        #endregion

        #region Ctor

        public ImportBase(IEnumerable<string> rows, int fil_Id = 0)
        {
            _rows = rows;
            _fil_Id = fil_Id;


        }

        #endregion

        #region Public Methods

        public virtual IDictionary<string, string> Import()
        {
            if (_rows.Count() == 0)
            {
                Errors.Add("empty", "Il foglio importato non contiere dati");
                return Errors;
            }

            LoadServiceData();

            ReadHeader();

            foreach (var rowData in DataRows)
            {
                ReadRow();

                if (!ValidateRow())
                    continue;

                ElaborateRow();
            }
            
            Save();

            return _errors;
        }

        #endregion

        #region Protected Methods

        protected virtual bool IsEmpty()
        {
           return  _emptyRow.All(field => field == "");
        }

        protected virtual void ReadHeader()
        {
            _header = new Dictionary<string, Int32>();

            string firstRow = _rows.First();
            string[] columns = firstRow.Split(';');

            for (int colIndex = 0; colIndex < columns.Length - 1; colIndex++)
                _header.Add(columns[colIndex], colIndex);

            _rowIndex = 1;

        }

        protected virtual void ReadRow()
        {
            CurrentRow = DataRows.ElementAt(_rowIndex - 1);

            ReadRow(CurrentRow);

            _rowIndex++;
        }

        protected abstract void LoadServiceData();

        protected abstract void ElaborateRow();

        protected abstract void ReadRow(string[] rowData);

        protected abstract void Save();

        protected abstract bool ValidateRow();

        #endregion

    }
}
