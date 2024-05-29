using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Business.XmlExportsData.Orlando
{
    public class OrlandoWriter : XmlTextWriter
    {

        public OrlandoWriter(TextWriter sink) : base(sink)
        {
        }

        public override void WriteEndElement()
        {
            base.WriteFullEndElement();
        }
    }
}
