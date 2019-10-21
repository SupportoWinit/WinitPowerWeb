using System;
using System.Collections.Generic;
using System.Linq;
using Business.BusinessExtension;
using Business.ExportExcelEngine;
using Business;
using Common;

namespace Exports.ExportExcelSpecialized
{
    public class ExportExcelSpecializedTimeSheetCol : IExportExcelSpecialized<TimesheetModuleItem>
    {
        public class EECol
        {
            public string Name { get; set; }
            public Common.JustificationTypeEnum Justification { get; set; }
            public string JustificationString { get; set; }
            public double Day01 { get; set; }
            public double Day02 { get; set; }
            public double Day03 { get; set; }
            public double Day04 { get; set; }
            public double Day05 { get; set; }
            public double Day06 { get; set; }
            public double Day07 { get; set; }
            public double Day08 { get; set; }
            public double Day09 { get; set; }
            public double Day10 { get; set; }
            public double Day11 { get; set; }
            public double Day12 { get; set; }
            public double Day13 { get; set; }
            public double Day14 { get; set; }
            public double Day15 { get; set; }
            public double Day16 { get; set; }
            public double Day17 { get; set; }
            public double Day18 { get; set; }
            public double Day19 { get; set; }
            public double Day20 { get; set; }
            public double Day21 { get; set; }
            public double Day22 { get; set; }
            public double Day23 { get; set; }
            public double Day24 { get; set; }
            public double Day25 { get; set; }
            public double Day26 { get; set; }
            public double Day27 { get; set; }
            public double Day28 { get; set; }
            public double Day29 { get; set; }
            public double Day30 { get; set; }
            public double Day31 { get; set; }
        }

        private EECol stubEECol = null;

        private DateTime currentExportDate = DateTime.UtcNow;

        public bool IsDecimalFormat { get; set; }

        public ExportExcelTreeNode GetRootTreeNode(List<TimesheetModuleItem> items)
        {
            ExportExcelTreeNode root = new ExportExcelTreeNode();

            //if (items != null)
            //{
            //    var fistItem = items.First();

            //    if (fistItem != null)
            //    {
            //        currentExportDate = fistItem.StartDate;

            //        var tmisGroupedByCol = items.GroupBy(tmi => tmi.ColId);

            //        foreach (var col in tmisGroupedByCol)
            //        {
            //            EECol newEECol = new EECol
            //            {
            //                Name = String.Format("{0}: {1} - {2}", BusinessService.GetLocalizedString(PowerWebResources.STR_COLLABORATORE), col.First().ColMnemonic, col.First().ColDesc),
            //            };

            //            var colPlans =  col.Where(tmi => tmi.JustificationType == JustificationTypeEnum.Plan);

            //            if (colPlans.Count() > 0)
            //            {
            //                newEECol.Justification = JustificationTypeEnum.Plan;
            //                newEECol.Day01 = colPlans.Sum(cp => cp.Day01);
            //                newEECol.Day02 = colPlans.Sum(cp => cp.Day02);
            //                newEECol.Day03 = colPlans.Sum(cp => cp.Day03);
            //                newEECol.Day04 = colPlans.Sum(cp => cp.Day04);
            //                newEECol.Day05 = colPlans.Sum(cp => cp.Day05);
            //                newEECol.Day06 = colPlans.Sum(cp => cp.Day06);
            //                newEECol.Day07 = colPlans.Sum(cp => cp.Day07);
            //                newEECol.Day08 = colPlans.Sum(cp => cp.Day08);
            //                newEECol.Day09 = colPlans.Sum(cp => cp.Day09);
            //                newEECol.Day10 = colPlans.Sum(cp => cp.Day10);
            //                newEECol.Day11 = colPlans.Sum(cp => cp.Day11);
            //                newEECol.Day12 = colPlans.Sum(cp => cp.Day12);
            //                newEECol.Day13 = colPlans.Sum(cp => cp.Day13);
            //                newEECol.Day14 = colPlans.Sum(cp => cp.Day14);
            //                newEECol.Day15 = colPlans.Sum(cp => cp.Day15);
            //                newEECol.Day16 = colPlans.Sum(cp => cp.Day16);
            //                newEECol.Day17 = colPlans.Sum(cp => cp.Day17);
            //                newEECol.Day18 = colPlans.Sum(cp => cp.Day18);
            //                newEECol.Day19 = colPlans.Sum(cp => cp.Day19);
            //                newEECol.Day20 = colPlans.Sum(cp => cp.Day20);
            //                newEECol.Day21 = colPlans.Sum(cp => cp.Day21);
            //                newEECol.Day22 = colPlans.Sum(cp => cp.Day22);
            //                newEECol.Day23 = colPlans.Sum(cp => cp.Day23);
            //                newEECol.Day24 = colPlans.Sum(cp => cp.Day24);
            //                newEECol.Day25 = colPlans.Sum(cp => cp.Day25);
            //                newEECol.Day26 = colPlans.Sum(cp => cp.Day26);
            //                newEECol.Day27 = colPlans.Sum(cp => cp.Day27);
            //                newEECol.Day28 = colPlans.Sum(cp => cp.Day28);
            //                newEECol.Day29 = colPlans.Sum(cp => cp.Day29);
            //                newEECol.Day30 = colPlans.Sum(cp => cp.Day30);
            //                newEECol.Day31 = colPlans.Sum(cp => cp.Day31);
            //            }



            //            EECol newRegChildEECol = new EECol
            //            {
            //                Name = "",//String.Format("", col.First().ColMnemonic, col.First().ColDesc),
            //            };

            //            var colRegs = col.Where(tmi => tmi.JustificationType == JustificationTypeEnum.None);

            //            if (colRegs.Count() > 0)
            //            {
            //                newRegChildEECol.Justification = JustificationTypeEnum.None;
            //                newRegChildEECol.Day01 = colRegs.Sum(cp => cp.Day01);
            //                newRegChildEECol.Day02 = colRegs.Sum(cp => cp.Day02);
            //                newRegChildEECol.Day03 = colRegs.Sum(cp => cp.Day03);
            //                newRegChildEECol.Day04 = colRegs.Sum(cp => cp.Day04);
            //                newRegChildEECol.Day05 = colRegs.Sum(cp => cp.Day05);
            //                newRegChildEECol.Day06 = colRegs.Sum(cp => cp.Day06);
            //                newRegChildEECol.Day07 = colRegs.Sum(cp => cp.Day07);
            //                newRegChildEECol.Day08 = colRegs.Sum(cp => cp.Day08);
            //                newRegChildEECol.Day09 = colRegs.Sum(cp => cp.Day09);
            //                newRegChildEECol.Day10 = colRegs.Sum(cp => cp.Day10);
            //                newRegChildEECol.Day11 = colRegs.Sum(cp => cp.Day11);
            //                newRegChildEECol.Day12 = colRegs.Sum(cp => cp.Day12);
            //                newRegChildEECol.Day13 = colRegs.Sum(cp => cp.Day13);
            //                newRegChildEECol.Day14 = colRegs.Sum(cp => cp.Day14);
            //                newRegChildEECol.Day15 = colRegs.Sum(cp => cp.Day15);
            //                newRegChildEECol.Day16 = colRegs.Sum(cp => cp.Day16);
            //                newRegChildEECol.Day17 = colRegs.Sum(cp => cp.Day17);
            //                newRegChildEECol.Day18 = colRegs.Sum(cp => cp.Day18);
            //                newRegChildEECol.Day19 = colRegs.Sum(cp => cp.Day19);
            //                newRegChildEECol.Day20 = colRegs.Sum(cp => cp.Day20);
            //                newRegChildEECol.Day21 = colRegs.Sum(cp => cp.Day21);
            //                newRegChildEECol.Day22 = colRegs.Sum(cp => cp.Day22);
            //                newRegChildEECol.Day23 = colRegs.Sum(cp => cp.Day23);
            //                newRegChildEECol.Day24 = colRegs.Sum(cp => cp.Day24);
            //                newRegChildEECol.Day25 = colRegs.Sum(cp => cp.Day25);
            //                newRegChildEECol.Day26 = colRegs.Sum(cp => cp.Day26);
            //                newRegChildEECol.Day27 = colRegs.Sum(cp => cp.Day27);
            //                newRegChildEECol.Day28 = colRegs.Sum(cp => cp.Day28);
            //                newRegChildEECol.Day29 = colRegs.Sum(cp => cp.Day29);
            //                newRegChildEECol.Day30 = colRegs.Sum(cp => cp.Day30);
            //                newRegChildEECol.Day31 = colRegs.Sum(cp => cp.Day31);
            //            }

            //            ExportExcelTreeNode expExcTreeChild = new ExportExcelTreeNode(newRegChildEECol);

            //            var expExcTree = new ExportExcelTreeNode(newEECol);

            //            expExcTree.AddChild(expExcTreeChild);

            //            List<EECol> newJustChildEEColList = new List<EECol>();

            //            var colJusts = col.Where(tmi => tmi.JustificationType == Business.BusinessExtension.JustificationTypeEnum.Just).GroupBy(tmi => tmi.Justification);

            //            Dictionary<string, string> dicMotivation = new Dictionary<string, string>();

            //            foreach (var colJust in colJusts)
            //            {
            //                if (!dicMotivation.ContainsKey(colJust.First().Justification))
            //                {
            //                    EECol newJustChildEECol = new EECol
            //                    {
            //                        Name = "",
            //                        JustificationString = colJust.First().Justification,
            //                    };
            //                    newJustChildEEColList.Add(newJustChildEECol);
            //                }

            //                EECol tmpEEE = newJustChildEEColList.SingleOrDefault(eec => eec.JustificationString == colJust.First().Justification);


            //                tmpEEE.Justification = JustificationTypeEnum.None;
            //                tmpEEE.Day01 = colJust.Sum(cp => cp.Day01);
            //                tmpEEE.Day02 = colJust.Sum(cp => cp.Day02);
            //                tmpEEE.Day03 = colJust.Sum(cp => cp.Day03);
            //                tmpEEE.Day04 = colJust.Sum(cp => cp.Day04);
            //                tmpEEE.Day05 = colJust.Sum(cp => cp.Day05);
            //                tmpEEE.Day06 = colJust.Sum(cp => cp.Day06);
            //                tmpEEE.Day07 = colJust.Sum(cp => cp.Day07);
            //                tmpEEE.Day08 = colJust.Sum(cp => cp.Day08);
            //                tmpEEE.Day09 = colJust.Sum(cp => cp.Day09);
            //                tmpEEE.Day10 = colJust.Sum(cp => cp.Day10);
            //                tmpEEE.Day11 = colJust.Sum(cp => cp.Day11);
            //                tmpEEE.Day12 = colJust.Sum(cp => cp.Day12);
            //                tmpEEE.Day13 = colJust.Sum(cp => cp.Day13);
            //                tmpEEE.Day14 = colJust.Sum(cp => cp.Day14);
            //                tmpEEE.Day15 = colJust.Sum(cp => cp.Day15);
            //                tmpEEE.Day16 = colJust.Sum(cp => cp.Day16);
            //                tmpEEE.Day17 = colJust.Sum(cp => cp.Day17);
            //                tmpEEE.Day18 = colJust.Sum(cp => cp.Day18);
            //                tmpEEE.Day19 = colJust.Sum(cp => cp.Day19);
            //                tmpEEE.Day20 = colJust.Sum(cp => cp.Day20);
            //                tmpEEE.Day21 = colJust.Sum(cp => cp.Day21);
            //                tmpEEE.Day22 = colJust.Sum(cp => cp.Day22);
            //                tmpEEE.Day23 = colJust.Sum(cp => cp.Day23);
            //                tmpEEE.Day24 = colJust.Sum(cp => cp.Day24);
            //                tmpEEE.Day25 = colJust.Sum(cp => cp.Day25);
            //                tmpEEE.Day26 = colJust.Sum(cp => cp.Day26);
            //                tmpEEE.Day27 = colJust.Sum(cp => cp.Day27);
            //                tmpEEE.Day28 = colJust.Sum(cp => cp.Day28);
            //                tmpEEE.Day29 = colJust.Sum(cp => cp.Day29);
            //                tmpEEE.Day30 = colJust.Sum(cp => cp.Day30);
            //                tmpEEE.Day31 = colJust.Sum(cp => cp.Day31);

            //            }

            //            foreach (EECol eECol in newJustChildEEColList)
            //            {
            //                ExportExcelTreeNode expExcTreeChildJust = new ExportExcelTreeNode(eECol);

            //                //var expExcTreeJust = new ExportExcelTreeNode(newEECol);

            //                expExcTree.AddChild(expExcTreeChildJust);
            //            }





            //            root.AddChild(expExcTree);

            //        }
            //    }
            //}

            return root;
        }

        public List<ExportExcelTreeNodeTemplate> GetTreeNodeTemplates()
        {
            List<ExportExcelTreeNodeTemplate> templates = new List<ExportExcelTreeNodeTemplate>();


            ExportExcelTreeNodeTemplate createTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateSheet, null, 0);

            createTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.Sheet, null, "TimeSheet"));

            templates.Add(createTemplate);

            ExportExcelTreeNodeTemplate headerTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateHeader, "1:10", 0);

            headerTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.StaticCell, "B8", currentExportDate));

            templates.Add(headerTemplate);


            ExportExcelTreeNodeTemplate nameTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "11:11", 1);

            nameTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "A", CommonService.GetPropertyName(() => stubEECol.Name)));

            templates.Add(nameTemplate);

            ExportExcelTreeNodeTemplate datesHeaderTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "12:12", 1);

            templates.Add(datesHeaderTemplate);

            ExportExcelTreeNodeTemplate planTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "13:13", 1);


            String numberFormat = null;

            if (!IsDecimalFormat)
                numberFormat = "HH:mm";
            else
                numberFormat = "0.00";

            planTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.Column, "A", BusinessService.GetLocalizedString(PowerWebResources.STR_PIANO)));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "B", CommonService.GetPropertyName(() => stubEECol.Day01), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "C", CommonService.GetPropertyName(() => stubEECol.Day02), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "D", CommonService.GetPropertyName(() => stubEECol.Day03), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "E", CommonService.GetPropertyName(() => stubEECol.Day04), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "F", CommonService.GetPropertyName(() => stubEECol.Day05), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "G", CommonService.GetPropertyName(() => stubEECol.Day06), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "H", CommonService.GetPropertyName(() => stubEECol.Day07), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "I", CommonService.GetPropertyName(() => stubEECol.Day08), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "J", CommonService.GetPropertyName(() => stubEECol.Day09), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "K", CommonService.GetPropertyName(() => stubEECol.Day10), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "L", CommonService.GetPropertyName(() => stubEECol.Day11), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "M", CommonService.GetPropertyName(() => stubEECol.Day12), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "N", CommonService.GetPropertyName(() => stubEECol.Day13), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "O", CommonService.GetPropertyName(() => stubEECol.Day14), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "P", CommonService.GetPropertyName(() => stubEECol.Day15), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Q", CommonService.GetPropertyName(() => stubEECol.Day16), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "R", CommonService.GetPropertyName(() => stubEECol.Day17), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "S", CommonService.GetPropertyName(() => stubEECol.Day18), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "T", CommonService.GetPropertyName(() => stubEECol.Day19), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "U", CommonService.GetPropertyName(() => stubEECol.Day20), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "V", CommonService.GetPropertyName(() => stubEECol.Day21), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "W", CommonService.GetPropertyName(() => stubEECol.Day22), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "X", CommonService.GetPropertyName(() => stubEECol.Day23), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Y", CommonService.GetPropertyName(() => stubEECol.Day24), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Z", CommonService.GetPropertyName(() => stubEECol.Day25), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AA", CommonService.GetPropertyName(() => stubEECol.Day26), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AB", CommonService.GetPropertyName(() => stubEECol.Day27), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AC", CommonService.GetPropertyName(() => stubEECol.Day28), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AD", CommonService.GetPropertyName(() => stubEECol.Day29), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AE", CommonService.GetPropertyName(() => stubEECol.Day30), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            planTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AF", CommonService.GetPropertyName(() => stubEECol.Day31), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));

            templates.Add(planTemplate);

            ExportExcelTreeNodeTemplate regTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "13:13", 2);

            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "A", CommonService.GetPropertyName(() => stubEECol.JustificationString)));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "B", CommonService.GetPropertyName(() => stubEECol.Day01), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "C", CommonService.GetPropertyName(() => stubEECol.Day02), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "D", CommonService.GetPropertyName(() => stubEECol.Day03), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "E", CommonService.GetPropertyName(() => stubEECol.Day04), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "F", CommonService.GetPropertyName(() => stubEECol.Day05), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "G", CommonService.GetPropertyName(() => stubEECol.Day06), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "H", CommonService.GetPropertyName(() => stubEECol.Day07), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "I", CommonService.GetPropertyName(() => stubEECol.Day08), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "J", CommonService.GetPropertyName(() => stubEECol.Day09), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "K", CommonService.GetPropertyName(() => stubEECol.Day10), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "L", CommonService.GetPropertyName(() => stubEECol.Day11), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "M", CommonService.GetPropertyName(() => stubEECol.Day12), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "N", CommonService.GetPropertyName(() => stubEECol.Day13), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "O", CommonService.GetPropertyName(() => stubEECol.Day14), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "P", CommonService.GetPropertyName(() => stubEECol.Day15), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Q", CommonService.GetPropertyName(() => stubEECol.Day16), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "R", CommonService.GetPropertyName(() => stubEECol.Day17), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "S", CommonService.GetPropertyName(() => stubEECol.Day18), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "T", CommonService.GetPropertyName(() => stubEECol.Day19), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "U", CommonService.GetPropertyName(() => stubEECol.Day20), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "V", CommonService.GetPropertyName(() => stubEECol.Day21), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "W", CommonService.GetPropertyName(() => stubEECol.Day22), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "X", CommonService.GetPropertyName(() => stubEECol.Day23), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Y", CommonService.GetPropertyName(() => stubEECol.Day24), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Z", CommonService.GetPropertyName(() => stubEECol.Day25), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AA", CommonService.GetPropertyName(() => stubEECol.Day26), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AB", CommonService.GetPropertyName(() => stubEECol.Day27), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AC", CommonService.GetPropertyName(() => stubEECol.Day28), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AD", CommonService.GetPropertyName(() => stubEECol.Day29), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AE", CommonService.GetPropertyName(() => stubEECol.Day30), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));
            regTemplate.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AF", CommonService.GetPropertyName(() => stubEECol.Day31), new ExportExcelSpecializedTimeSheetColFormatter(IsDecimalFormat), numberFormat));

            templates.Add(regTemplate);

            ExportExcelTreeNodeTemplate post = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "14:14", 1, true);
            post.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.Column, "A", BusinessService.GetLocalizedString(PowerWebResources.STR_TOTALE).ToUpper()));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "B", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "C", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "D", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "E", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "F", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "G", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "H", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "I", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "J", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "K", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "L", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "M", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "N", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "O", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "P", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "Q", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "R", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "S", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "T", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "U", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "V", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "W", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "X", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "Y", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "Z", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "AA", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "AB", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "AC", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "AD", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "AE", null, null, numberFormat));
            post.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.TimeSheetFormula, "AF", null, null, numberFormat));
            templates.Add(post);


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
