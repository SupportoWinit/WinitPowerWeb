using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Business.XmlExportsData.Perfetto
{
    public class PerfettoWriter : XmlTextWriter
    {
        
        public PerfettoWriter( TextWriter sink) : base( sink)
        {
        }
        
        public override void WriteEndElement()
        {
            base.WriteFullEndElement();
        }
    }
}
