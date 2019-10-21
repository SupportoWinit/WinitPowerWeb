using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OfficeOpenXml;

namespace Business.ExportExcelEngine
{
    public enum ExportExcelActionEnum
    {

        CreateSheet,
        CreateRow,
        CreateBlankRow,
        CreateHeader,
    }


    public class ExportExcelTreeNodeTemplate
    {
        public ExportExcelActionEnum ExcelAction { get; private set; }
        public String Range { get; private set; }
        public int NodeLevel { get; private set; }
        public List<ExportExcelTreeNodeMappingBase> Mappings { get; private set; }
        public bool IsPost { get; private set; }

        public ExportExcelTreeNodeTemplate()
        {
            Mappings = new List<ExportExcelTreeNodeMappingBase>();
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ExportExcelTreeNodeTemplate"/> class.
        /// Range permette di "copiare un range di righe dalla pattern per utilizzarle nel nuovo excel"
        /// </summary>
        /// <param name="excelAction">The excel action.</param>
        /// <param name="range">Range di riferimento per l'excel pattern.</param>
        /// <param name="nodeLevel">Livello dell'albero nel quale viene eseguita l'operazione.</param>
        /// <param name="isPost">Booleano che indica se eseguire l'operazione in pre-visita o in post-visita(true in questo caso).</param>
        public ExportExcelTreeNodeTemplate(ExportExcelActionEnum excelAction, String range, int nodeLevel, bool isPost = false)
            : this()
        {
            ExcelAction = excelAction;
            Range = range;
            NodeLevel = nodeLevel;
            IsPost = isPost;
        }
        
    }
}
