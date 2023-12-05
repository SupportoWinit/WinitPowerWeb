using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Business.XmlExportsData.Scs
{
    /// <summary>
    /// Classe utilizzata per la definizione dei dati del tag envelope nell'esportazione verso Perfetto
    /// </summary>
    public sealed class Envelope
    {

        #region Fields

        private XmlEnvelopeDocumentInfo _documentInfo = new XmlEnvelopeDocumentInfo();

        private XmlEnvelopeContents _contents = new XmlEnvelopeContents();

        #endregion

        #region Properties

        public string ExportID { get; set; }

        public string Description { get; set; }

        public XmlEnvelopeDocumentInfo DocumentInfo
        {
            get { return _documentInfo; }
            set { _documentInfo = value; }
        }

        public XmlEnvelopeContents Contents
        {
            get { return _contents; }
            set { _contents = value; }
        }

        #endregion

    }
}
