using Business.ExportExcelEngine;
using Common;
using Domain;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exports.ExportExcelSpecialized
{
    public class ExportExcelSpecializedTimeSheetColFormatter : ExportExcelTreeNodeAbstractFormatter
    {

        private bool _isDecimalFormat;

        public ExportExcelSpecializedTimeSheetColFormatter(bool isDecimalFormat)
        {
            _isDecimalFormat = isDecimalFormat;
        }

        public override object Format(object currentValue, ExcelRange range, ExportExcelTreeNode currentNode)
        {
            if (!_isDecimalFormat)
            {
                Double value = Convert.ToDouble(currentValue);
                currentValue = CommonService.GetDateTimeFromDouble(value >= 0 ? value : Math.Abs(value));
            }

            return currentValue;
        }
    }

}
