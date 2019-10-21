using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;

namespace Exports.ExportExcelGeneric
{
    public class ExportExcelGenericReg_V : ExportExcelGeneric<Reg_V>
    {
        public override string SheetName
        {
            get { return "Registrazioni"; }
        }
    }
}
