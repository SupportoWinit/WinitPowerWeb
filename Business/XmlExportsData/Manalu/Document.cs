using System.Xml.Serialization;
using System.Collections.Generic;

namespace Business.XmlExportsData.Manalu
{
    [XmlRoot("Fornitura")]
    public sealed class Fornitura
    {
        private List<XmlDocuments> _documents;

        [XmlElement("Dipendente")]
        public List<XmlDocuments> Dipendente
        {
            get { return _documents ?? (_documents = new List<XmlDocuments>()); }
            set { _documents = value; }
        }
    }
}
