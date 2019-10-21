using Business.ExportExcelEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace Exports.ExportExcelSpecialized
{
    public class ExportExcellActivity_ColCodiceTypeFormatter : ExportExcelTreeNodeAbstractFormatter
    {
        public override object Format(object currentValue, ExcelRange range, ExportExcelTreeNode currentNode)
        {
            object returnValue;
            if (!String.IsNullOrEmpty(currentValue.ToString()))
                returnValue = currentValue.ToString().Trim();
            else
                returnValue = currentValue;

            return returnValue;
        }
    }
}
