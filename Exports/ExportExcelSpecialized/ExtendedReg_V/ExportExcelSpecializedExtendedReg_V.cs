using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Business.Repository;
using Common;
using Domain;
using Business.ExportExcelEngine;

namespace Exports.ExportExcelSpecialized
{
    public class ExportExcelSpecializedExtendedReg_V : IExportExcelSpecialized<Reg_V>
    {

        private Reg_V stubRegV = null;

        public ExportExcelTreeNode GetRootTreeNode(List<Reg_V> items)
        {
            ExportExcelTreeNode root = new ExportExcelTreeNode();

            root.AddChildren(items);

            return root;
        }

        public List<ExportExcelTreeNodeTemplate> GetTreeNodeTemplates()
        {
            List<ExportExcelTreeNodeTemplate> templates = new List<ExportExcelTreeNodeTemplate>();

            //creo il foglio excel
            ExportExcelTreeNodeTemplate createTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateSheet, null, 0);

            createTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.Sheet, null, "Registrazioni"));

            templates.Add(createTemplate);

            //Copio la parte di header e inserisco i dati (data creazione)
            ExportExcelTreeNodeTemplate headerTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateHeader, "1:5", 0);

            headerTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.StaticCell, "A3", String.Format("Data Export: {0}", DateTime.UtcNow)));

            templates.Add(headerTemplate);

            //preparo le colonne di somma che andranno ad inserirsi alla fine del foglio
            //ExportExcelTreeNodeTemplate post = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "7:7", 0, true);
            //post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Sum, "G"));
            //post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Sum, "J"));
            //templates.Add(post);


            //template che si ripete per ogni riga di dati del foglio
            ExportExcelTreeNodeTemplate firstLevelTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "6:6", 1);

            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "A", CommonService.GetPropertyName(() => stubRegV.Col_Mnemonic)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "B", CommonService.GetPropertyName(() => stubRegV.Col_Desc)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "C", CommonService.GetPropertyName(() => stubRegV.Cant_Mnemonic)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "D", CommonService.GetPropertyName(() => stubRegV.Cant_Desc)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "E", CommonService.GetPropertyName(() => stubRegV.Data_Reg)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "F", CommonService.GetPropertyName(() => stubRegV.Data_Ora_Fis_E)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "G", CommonService.GetPropertyName(() => stubRegV.Data_Ora_Fis_U)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "I", CommonService.GetPropertyName(() => stubRegV.Data_Ora_Fig_E)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "J", CommonService.GetPropertyName(() => stubRegV.Data_Ora_Fig_U)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "L", CommonService.GetPropertyName(() => stubRegV.Motivazione_Reg_Cod)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "M", CommonService.GetPropertyName(() => stubRegV.Registrazione_Tipo_Reg)));
            firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "N", CommonService.GetPropertyName(() => stubRegV.Registrazione_Stato_Reg)));

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
