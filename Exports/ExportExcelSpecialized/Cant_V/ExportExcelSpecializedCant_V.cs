using System;
using System.Collections.Generic;
using System.Drawing;
using Common;
using Domain;
using Business.ExportExcelEngine;
using Domain.Extensions;

namespace Exports.ExportExcelSpecialized
{
    public class ExportExcelSpecializedCant_V : IExportExcelSpecialized<Cant_V>
    {

        private Cant_V _stubCant_V = null;

        private List<Cant_V> _items = null;

        public ExportExcelTreeNode GetRootTreeNode(List<Cant_V> items)
        {
            ExportExcelTreeNode root = new ExportExcelTreeNode();

            _items = items;

            root.AddChildren(items);

            return root;
        }

        public List<ExportExcelTreeNodeTemplate> GetTreeNodeTemplates()
        {
            List<ExportExcelTreeNodeTemplate> templates = new List<ExportExcelTreeNodeTemplate>();

            //creo il foglio excel
            ExportExcelTreeNodeTemplate createTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateSheet, null, 0);

            createTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.Sheet, null, "Attività"));

            templates.Add(createTemplate);

            //Copio la parte di header e inserisco i dati (data creazione)
            ExportExcelTreeNodeTemplate headerTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateHeader, "1:1", 0);

            templates.Add(headerTemplate);

            //template che si ripete per ogni riga di dati del foglio
            ExportExcelTreeNodeTemplate firstLevelTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "2:2", 1);

            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "A", CommonService.GetPropertyName(() => _stubCant_V.Cant_Id)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "B", CommonService.GetPropertyName(() => _stubCant_V.Codice_Cantiere)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "C", CommonService.GetPropertyName(() => _stubCant_V.Descrizione_Can)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "D", CommonService.GetPropertyName(() => _stubCant_V.Data_Nascita_Can)));

            templates.Add(firstLevelTemplate);

            return templates;
        }

        public IEnumerable<Tuple<string, string, string>> VisibleFields { get; set; }

        public IList<string> GetCellsToMerge()
        {
            return new List<string>();
        }

        public string ModelName { get; set; }

        public bool IsAutoFitColumns
        {
            get { return false; }
        }

        public string RepeatRowsAddress
        {
            get { return string.Empty; }
        }
    }
}
