using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;

namespace Exports.ExportExcelGeneric
{
    public class ExportExcelGenericCant : ExportExcelGeneric<Cant>
    {
        public override string SheetName
        {
            get { return "Cantiere"; }
        }
    }
}
