using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Business.XmlExportsData.Manalu
{
    public class ManaluWriter : XmlTextWriter
    {
        private class ElementState
        {
            public bool HasContent { get; set; }
        }

        private readonly Stack<ElementState> _elements = new Stack<ElementState>();

        public ManaluWriter(TextWriter sink) : base(sink)
        {
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            if (_elements.Count > 0)
                _elements.Peek().HasContent = true;

            _elements.Push(new ElementState());

            base.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteString(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (_elements.Count > 0)
                    _elements.Peek().HasContent = true;

                base.WriteString(text.Trim());
            }
            else
            {
                base.WriteString(string.Empty);
            }
        }

        public override void WriteEndElement()
        {
            bool isEmptyElement = _elements.Count > 0 && !_elements.Pop().HasContent;

            if (isEmptyElement)
            {
                var oldFormatting = this.Formatting;

                // Per gli elementi vuoti evito che la chiusura vada a capo
                this.Formatting = Formatting.None;
                base.WriteFullEndElement();

                this.Formatting = oldFormatting;
            }
            else
            {
                base.WriteFullEndElement();
            }
        }
    }
}
