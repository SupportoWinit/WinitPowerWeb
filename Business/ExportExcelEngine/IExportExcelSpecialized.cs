using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Domain;
using OfficeOpenXml;

namespace Business.ExportExcelEngine
{
    public interface IExportExcelSpecialized<T> where T : class
    {
        //Nell'implementazione del metodo va definita la struttura dell'albero dalla quale dipenderà totalmente il template.
        ExportExcelTreeNode GetRootTreeNode(List<T> items);

        //Questo medoto ritornerà la lista di templates definita al suo interno.
        List<ExportExcelTreeNodeTemplate> GetTreeNodeTemplates();

        IEnumerable<Tuple<string, string, string>> VisibleFields { get; set; }

        IList<string> GetCellsToMerge();

        string ModelName { get; set; }

        bool IsAutoFitColumns { get; }

        string RepeatRowsAddress { get; }
    }
}
