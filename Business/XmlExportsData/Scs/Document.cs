using DevExpress.XtraRichEdit.Import.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml.Serialization;

namespace Business.XmlExportsData.Scs
{

    public sealed class Fornitura
    {
        #region Fields

        private XmlDocuments _documents;

        #endregion

        #region Properties

        //Corpo del documento Xml
        public XmlDocuments Dipendente
        {
            get
            {
                if (_documents == null)
                    _documents = new XmlDocuments();

                return _documents;
            }
            set { _documents = value; }

        }
        #endregion

    }
}
