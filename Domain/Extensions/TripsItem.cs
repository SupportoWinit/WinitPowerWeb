using System;
using System.Collections.Generic;
using Westwind.Utilities;

namespace Domain.Extensions
{
    public class TripsItem : Expando
    {
        #region Constructor

        public TripsItem()
        {
            
        }

        #endregion

        #region Private Fields

        private DateTime? _tempoPrevisto;

        #endregion

        #region Public Properties

        public string CantDesc { get; set; }
        public string DataReg { get; set; }
        public string OraE { get; set; }
        public string OraU { get; set; }
        public string ColDesc { get; set; }
        public string KMReg { get; set; }
        public string DurataReale { get; set; }
        public string DurataTeorica { get; set; }
        public string TipoRegistrazione { get; set; }

        #endregion
    }
}
