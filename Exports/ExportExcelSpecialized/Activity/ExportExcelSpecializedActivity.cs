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
    public class ExportExcelSpecializedActivity : IExportExcelSpecialized<ActivityItem>
    {

        private readonly Export56VersionEnum _customizationVersion = (Export56VersionEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.Export56VersionEnum);

        private ActivityItem _stubActivityItem = null;

        private List<ActivityItem> _items = null;

        public ExportExcelTreeNode GetRootTreeNode(List<ActivityItem> items)
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
                foreach (var currentCol in _items.First().AdditionalCols)
                {
                    int index = _items.First().AdditionalCols.IndexOf(currentCol);
                    headerTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.StaticCell, String.Format("{0},1", index + 21), currentCol));
                }

                // se non si sta preparando un export con personalizzazioni Kleo allora
                // si aggiunge anche il codice collaboratore in coda
                if (_customizationVersion != Export56VersionEnum.Kleo)
                {
                    headerTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.StaticCell, String.Format("{0},1", _items.First().AdditionalCols.Count() + 21), 
                        CommonService.GetPropertyName(() => _stubActivityItem.ColCodice)));
                }


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

                switch (_customizationVersion)
                {
                    case Export56VersionEnum.Kleo:
                        cantDescMapping = "A";
                        cantPlaceMapping = "B";
                        cantAddressMapping = "C";
                        regKMMapping = "D";
                        cantCAPMapping = "E";
                        colCognomeMapping = "F";
                        colNomeMapping = "G";
                        regGGMapping = "H";
                        regMMMapping = "I";
                        regAAAAMapping = "J";
                        regHHMMSSInizioMapping = "K";
                        regHHMMSSFineMapping = "L";
                        regMMDurataMapping = "M";
                        tempoPrevistoMapping = "N";
                        regTipoMapping = "O";
                        codiceCapoAreaColMapping = "P";
                        codiceCapoAreaCantMapping = "Q";
                        break;
                }

                #endregion

                //template che si ripete per ogni riga di dati del foglio
                ExportExcelTreeNodeTemplate firstLevelTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "2:2", 1);

                // il codice fiscale non viene scritto per la versione Kleo dell'Export 56
                if (_customizationVersion != Export56VersionEnum.Kleo)
                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantCodFiscMapping,
                        CommonService.GetPropertyName(() => _stubActivityItem.CantCodFisc)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantDescMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.CantDesc)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantPlaceMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.CantPlace)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantAddressMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.CantAddress)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, regKMMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.RegKM), formatter: new ExportExcellActivity_RegKmTypeFormatter()));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantCAPMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.CantCAP)));

                // la asl non viene riportata per la versione Kleo dell'Export 56
                if (_customizationVersion != Export56VersionEnum.Kleo)
                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cantASLMapping,
                        CommonService.GetPropertyName(() => _stubActivityItem.CantASL)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, colCognomeMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.ColCognome)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, colNomeMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.ColNome)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, regGGMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.RegGG)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, regMMMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.RegMM)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, regAAAAMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.RegAAAA)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, regHHMMSSInizioMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.RegHHMMSSInizio), formatter: new ExportExcellActivity_RegHHMMSSInizioFontColorFormatter()));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, regHHMMSSFineMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.RegHHMMSSFine), formatter: new ExportExcellActivity_RegHHMMSSFineFontColorFormatter()));

                // la durata viene espressa in hh:mm:ss se sto lavorando nella personalizazione Kleo
                if (_customizationVersion != Export56VersionEnum.Kleo)
                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column,
                        regMMDurataMapping,
                        CommonService.GetPropertyName(() => _stubActivityItem.RegMMDurata)));
                else
                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column,
                        regMMDurataMapping,
                        CommonService.GetPropertyName(() => _stubActivityItem.RegHHMMSSDurata),
                        excelNumberFormat: "[$-F400]h:mm:ss AM/PM"));

                // il distretto non viene riportata per la versione Kleo dell'Export 56
                if (_customizationVersion != Export56VersionEnum.Kleo)
                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, colDistrettoMapping,
                        CommonService.GetPropertyName(() => _stubActivityItem.ColDistretto)));

                // la qualifica non viene riportata per la versione Kleo dell'Export 56
                if (_customizationVersion != Export56VersionEnum.Kleo)
                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, colQualificaMapping,
                        CommonService.GetPropertyName(() => _stubActivityItem.ColQualifica)));

                firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, regTipoMapping,
                    CommonService.GetPropertyName(() => _stubActivityItem.RegTipo)));

                // il cliente non viene riportata per la versione Kleo dell'Export 56
                if (_customizationVersion != Export56VersionEnum.Kleo)
                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cli_TipoMapping,
                        CommonService.GetPropertyName(() => _stubActivityItem.Cli_Tipo)));

                // il livello non viene riportata per la versione Kleo dell'Export 56
                if (_customizationVersion != Export56VersionEnum.Kleo)
                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, cant_LivelloMapping,
                        CommonService.GetPropertyName(() => _stubActivityItem.Cant_Livello)));

                // nella personalizzazione Kleo vanno aggiunti anche il capo area e il tempo previsto
                if (_customizationVersion == Export56VersionEnum.Kleo)
                {
                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, codiceCapoAreaColMapping,
                        CommonService.GetPropertyName(() => _stubActivityItem.CapoAreaCol)));

                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, codiceCapoAreaCantMapping,
                        CommonService.GetPropertyName(() => _stubActivityItem.CapoAreaCant)));

                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, tempoPrevistoMapping,
                        CommonService.GetPropertyName(() => _stubActivityItem.TempoPrevisto), excelNumberFormat: "[$-F400]h:mm:ss AM/PM"));
                }

                // il mapping delle attività non viene inserito per l'export di tipo Kleo
                if (_customizationVersion != Export56VersionEnum.Kleo)
                {
                    foreach (var currentCol in _items.First().AdditionalCols)
                    {
                        int index = _items.First().AdditionalCols.IndexOf(currentCol);
                        firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column,
                            (index + 21).ToString(), currentCol));
                    }

                    // al termine, se non si sta processando un export per Kleo si aggiunge
                    // la colonna con il codice collaboratore
                    firstLevelTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column,
                            (_items.First().AdditionalCols.Count() + 21).ToString(), CommonService.GetPropertyName(() => _stubActivityItem.ColCodice), formatter: new ExportExcellActivity_ColCodiceTypeFormatter()));
                }

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
