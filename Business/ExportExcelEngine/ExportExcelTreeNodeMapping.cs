using Common;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Business.ExportExcelEngine
{

    public enum ExportExcelReferenceEnum
    {
        
        StaticCell,
        Column,
        Diff,
        Row,
        Sheet,
        Sum,
        TimeSheetFormula,
        OrizontalSum
    }

    public abstract class ExportExcelTreeNodeMappingBase
    {
        private String _reference;

        public ExportExcelReferenceEnum ExcelReference { get; protected set; }
        public String Reference
        {
            get
            {
                var splittedValue = _reference.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (splittedValue.Length > 1)
                    _reference = CommonService.GetExcelColumnNameFromInt(Convert.ToInt32(splittedValue[0])) +
                                 splittedValue[1];
                else
                {
                    int formulaR1C1Index = int.MinValue;
                    if (Int32.TryParse(_reference, out formulaR1C1Index))
                        _reference = CommonService.GetExcelColumnNameFromInt(Convert.ToInt32(_reference));
                }
                return _reference;
            }
            protected set { _reference = value; }
        }
        public ExportExcelTreeNodeAbstractFormatter Formatter { get; protected set; }
        public String ExcelNumberFormat { get; protected set; }

    }

    public class ExportExcelTreeNodeMapping : ExportExcelTreeNodeMappingBase
    {
        public String PropertyName { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="ExportExcelTreeNodeMapping"/> class.
        /// Mappatura della singola colonna.
        /// </summary>
        /// <param name="excelReference">Tipologia di cella.</param>
        /// <param name="reference">Colonna di riferimento.</param>
        /// <param name="propertyName">Nome della proprietà da risolvere.</param>
        /// <param name="formatter">The formatter.</param>
        /// <param name="excelNumberFormat">The excel number format.</param>
        public ExportExcelTreeNodeMapping(ExportExcelReferenceEnum excelReference, String reference, String propertyName = null, ExportExcelTreeNodeAbstractFormatter formatter = null, String excelNumberFormat = null)
        {
            ExcelReference = excelReference;
            Reference = reference;
            PropertyName = propertyName;
            Formatter = formatter;
            ExcelNumberFormat = excelNumberFormat;
        }
    }

    public class ExportExcelTreeNodeStaticMapping : ExportExcelTreeNodeMappingBase
    {
        public Object Value { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportExcelTreeNodeStaticMapping"/> class.
        /// Operazione che permette di settare il valore della determinata cella "on the fly" tramite il campo value senza alcuna validazione.
        /// Comodo per celle singole che devono assumere un valore già definibile inizialmente
        /// </summary>
        /// <param name="excelReference">Tipologia di cella.</param>
        /// <param name="reference">Rieferimento.</param>
        /// <param name="value">Valore da assegnare.</param>
        /// <param name="formatter">The formatter.</param>
        /// <param name="excelNumberFormat">The excel number format.</param>
        public ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum excelReference, String reference, Object value, ExportExcelTreeNodeAbstractFormatter formatter = null, String excelNumberFormat = null)
        {
            ExcelReference = excelReference;
            Reference = reference;
            Value = value;
            Formatter = formatter;
            ExcelNumberFormat = excelNumberFormat;
        }
    }

}
