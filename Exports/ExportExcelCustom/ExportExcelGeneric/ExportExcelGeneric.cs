using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Business;
using Business.ExportExcelEngine;
using Exports.ExportExcelGeneric.Formatter;
using Exports.ExportExcelSpecialized;
using OfficeOpenXml.Style;

namespace Exports.ExportExcelGeneric
{
    public abstract class ExportExcelGeneric<T> : IExportExcelSpecialized<T> where T : class
    {
        public abstract string SheetName
        {
            get;
        }

        private const int StartRow = 8;

        public IEnumerable<Tuple<string, string, string>> VisibleFields { get; set; }

        public IList<string> GetCellsToMerge()
        {
            var cellsToMerge = new List<string>();
            var endColumn = (ExcelColumnEnum)VisibleFields.Count();
            cellsToMerge.Add(string.Format("A7:{0}7", endColumn));
            return cellsToMerge;
        }

        public string ModelName { get; set; }

        public bool IsAutoFitColumns
        {
            get { return true; }
        }

        public string RepeatRowsAddress
        {
            get
            {
                return string.Format("1:{0}", StartRow);
            }
        }

        public ExportExcelTreeNode GetRootTreeNode(List<T> items)
        {
            var root = new ExportExcelTreeNode();
            root.StartRow = StartRow;
            root.AddChildren(items);

            

            return root;
        }

        public List<ExportExcelTreeNodeTemplate> GetTreeNodeTemplates()
        {
            var templates = new List<ExportExcelTreeNodeTemplate>();

            //creo il foglio excel
            var createTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateSheet, null, 0);


            createTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.Sheet, null, SheetName));

            templates.Add(createTemplate);

            var headerTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateHeader, string.Format("1:7"), 0);
            headerTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.StaticCell, "A7", BusinessService.GetLocalizedString(ModelName)));

            var headerTableTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateHeader, string.Format("8:8"), 0);

            var firstTableLevelTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, string.Format("9:9"), 1);

            //vengono ciclati ed inseriti tutti i campi visibili in griglia
            for (int i = 0; i < VisibleFields.Count(); i++)
            {
                //viene costruito l'header
                var headerFormatter = new ExportExcellGeneric_HeaderFormatter();
                //viene costruito il formato deella cella
                var cellFormatter = new ExportExcellGeneric_HeaderFormatter();

                //viene estratto il campo corrispondente all'indice
                var currentField = VisibleFields.ElementAt(i);

                //viene determianta l posizione della cella
                var columnExcel = string.Format("{0},{1}", i + 1, StartRow);

                //viene inserito in cella il valore corrispondente al campo
                headerTableTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.StaticCell, columnExcel, currentField.Item2, headerFormatter));

                columnExcel = ((ExcelColumnEnum)i + 1).ToString();

                firstTableLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, columnExcel, currentField.Item1, cellFormatter, currentField.Item3));
            }

            templates.Add(headerTemplate);
            templates.Add(headerTableTemplate);
            templates.Add(firstTableLevelTemplate);

            return templates;
        }

    }
}