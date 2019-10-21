using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OfficeOpenXml.Style;

namespace Business.ExportExcelEngine
{
    public abstract class ExportExcelTreeNodeAbstractFormatter
    {
        public abstract object Format(object currentValue, ExcelRange range, ExportExcelTreeNode currentNode);
    }
}
