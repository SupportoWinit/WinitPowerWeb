using Business;
using Business.ExportExcelEngine;
using Common;
using Domain.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Exports.ExportExcelSpecialized
{
    public class ExportExcellActivity_RegKmTypeFormatter : ExportExcelTreeNodeAbstractFormatter
    {
        public override object Format(object currentValue, OfficeOpenXml.ExcelRange range, ExportExcelTreeNode currentNode)
        {
            object returnValue;
            if (!String.IsNullOrEmpty(currentValue.ToString()) && currentValue.ToString() != "NO_KM")
            {
                returnValue = Convert.ToDouble(currentValue);
            }
            else
                returnValue = currentValue;

            return returnValue;
        }
    }
}
