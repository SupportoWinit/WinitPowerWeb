using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridHelpers.Attributes
{
    public class LookupColumn : Attribute
    {
        public string[] Select { get; set; }

        public LookupColumn(params string[] select)
        {
            this.Select = select;
        }
    }
}
