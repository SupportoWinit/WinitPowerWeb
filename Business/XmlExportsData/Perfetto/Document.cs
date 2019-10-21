using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml.Serialization;

namespace Business.XmlExportsData.Perfetto
{
   
    public sealed class Document
    {
        #region Fields

        private string _documentName = "DINRapportiniBase";
        
        private XmlDocumentInfo _documentInfo;

        private XmlDocuments _documents;

        #endregion

        #region Properties

        //Informazioni del documento Xml
        public XmlDocumentInfo DocumentInfo
        {
            get
            {
                if (_documentInfo == null)
                    _documentInfo = new XmlDocumentInfo();

                return _documentInfo;
            }
            set { _documentInfo = value; }
        }

        //Corpo del documento Xml
        public XmlDocuments Documents
        {
            get
            {
                if (_documents == null)
                    _documents = new XmlDocuments();

                return _documents;
            }
            set { _documents = value; }

        }

        [XmlAttribute]
        public string documentname
        {
            get { return _documentName; }
            set { _documentName = value; }
        }

        #endregion

    }
}
