using Business.XmlExportsData.Scs;
using DevExpress.XtraRichEdit.Import.Html;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml.Serialization;

namespace Business.XmlExportsData
{
    public sealed class Fornitura
    {
        #region Fields

        private XmlDocument _documents;

        #endregion

        #region Properties

        //Corpo del documento Xml
        public XmlDocument Dipendente
        {
            get
            {
                if (_documents == null)
                    _documents = new XmlDocument();

                return _documents;
            }
            set { _documents = value; }

        }
        #endregion

    }
}
