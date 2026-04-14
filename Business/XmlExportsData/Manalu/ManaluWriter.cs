using System.IO;
using System.Xml;

namespace Business.XmlExportsData.Manalu
{
    public class ManaluWriter : XmlTextWriter
    {
        public ManaluWriter(TextWriter sink) : base(sink)
        {
        }

        public override void WriteEndElement()
        {
            base.WriteFullEndElement();
        }
    }
}
