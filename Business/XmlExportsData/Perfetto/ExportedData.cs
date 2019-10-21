using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Business.XmlExportsData.Perfetto
{
    /// <summary>
    /// Classe utilizzata per rappresentare (in input e in output) i dati degli elementi già processati nell'estrazione verso Perfetto
    /// </summary>
    public sealed class ExportedData
    {

        #region Public Properties

        /// <summary>
        /// Recupera o imposta l'id del collaboratore processato.
        /// </summary>
        /// <value>
        /// L'id del collaboratore processato.
        /// </value>
        public int ColId { get; set; }

        /// <summary>
        /// Recupera o imposta la data reg processata per il collaboratore specificato.
        /// </summary>
        /// <value>
        /// La data reg processata per il collaboratore specificato.
        /// </value>
        public DateTime DataReg { get; set; }

        #endregion

    }

    public sealed class ExportedDataList
    {

        #region Public Properties

        public List<ExportedData> ExportedDatas { get; set; }

        #endregion

    }
}
