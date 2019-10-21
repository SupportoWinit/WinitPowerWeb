using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.ImportCustomerService.Helpers
{
    internal static class ImportCustomerHelper
    {
        #region Funzioni di controllo formattazione

        public static bool IsHeaderWrongMapped(IDictionary<string, int> header)
        {
            return header.Values.Contains(-1);
        }

        public static bool IsEmptyLine(string line)
        {
            return line.All(c => c == ';');
        }

        #endregion
        
        #region Funzioni helper

        public static StringBuilder CreateEmptyRow(int columnsCount, string separator)
        {
            var emptyRow = new StringBuilder();

            for (var i = 0; i < columnsCount - 1; i++)
                emptyRow.Append(separator);

            return emptyRow;
        }

        #endregion
    }
}
