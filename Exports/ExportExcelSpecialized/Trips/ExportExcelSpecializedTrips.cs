using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Business.Repository;
using Common;
using Domain;
using Business.ExportExcelEngine;
using Domain.Extensions;

namespace Exports.ExportExcelSpecialized
{
    public class ExportExcelSpecializedTrips : IExportExcelSpecialized<TripsItem>
    {

        private readonly Export56VersionEnum _customizationVersion = (Export56VersionEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.Export56VersionEnum);

        private TripsItem _stubActivityItem = null;

        private List<TripsItem> _items = null;

        public ExportExcelTreeNode GetRootTreeNode(List<TripsItem> items)
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

            // si prosegue con l'elaborazione solamente se ci sono elementi da processare
            if (_items.Count > 0)
            {
                templates.Add(headerTemplate);

                #region Preparazione del mapping in base alle customization

                // definizione del mapping standard
                string cantCodFiscMapping = "A";
                string cantDescMapping = "B";
                string cantPlaceMapping = "C";
                string cantAddressMapping = "D";
                string regKMMapping = "E";
                string cantCAPMapping = "F";
                string cantASLMapping = "G";
                string colCognomeMapping = "H";
                string colNomeMapping = "I";
                string regGGMapping = "J";
                string regMMMapping = "K";
                string regAAAAMapping = "L";
                string regHHMMSSInizioMapping = "M";
                string regHHMMSSFineMapping = "N";
                string regMMDurataMapping = "O";
                string colDistrettoMapping = "P";
                string colQualificaMapping = "Q";
                string regTipoMapping = "R";
                string cli_TipoMapping = "S";
                string cant_LivelloMapping = "T";

                // utilizzate nelle personalizzazioni del report
                string codiceCapoAreaColMapping = "";
                string codiceCapoAreaCantMapping = "";
                string tempoPrevistoMapping = "";

                #endregion

                //template che si ripete per ogni riga di dati del foglio
                ExportExcelTreeNodeTemplate firstLevelTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "2:2", 1);

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantCodFiscMapping,
                    CommonService.GetPropertyName(() => _items.First().ColDesc)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantDescMapping,
                    CommonService.GetPropertyName(() => _items.First().DataReg)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantPlaceMapping,
                    CommonService.GetPropertyName(() => _items.First().OraE)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantAddressMapping,
                    CommonService.GetPropertyName(() => _items.First().OraU)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, regKMMapping,
                    CommonService.GetPropertyName(() => _items.First().CantDesc)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantCAPMapping,
                    CommonService.GetPropertyName(() => _items.First().KMReg)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantASLMapping,
                    CommonService.GetPropertyName(() => _items.First().DurataReale)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, colCognomeMapping,
                    CommonService.GetPropertyName(() => _items.First().DurataTeorica)));

                templates.Add(firstLevelTemplate);
            }

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
