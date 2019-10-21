using Business;
using Business.ExportExcelEngine;
using Common;
using Domain.Extensions;
using System.Drawing;

namespace Exports.ExportExcelSpecialized
{
    public class ExportExcellActivity_RegHHMMSSFineFontColorFormatter : ExportExcelTreeNodeAbstractFormatter
    {
        public override object Format(object currentValue, OfficeOpenXml.ExcelRange range, ExportExcelTreeNode currentNode)
        {

            Color fontColor = Color.Black;

            var nodeItem = currentNode.Item as ActivityItem;
            if (nodeItem != null)
                if (nodeItem.TipoModifica == (int)RegModifyTypeEnum.Exit_Modified ||
                                   nodeItem.TipoModifica == (int)RegModifyTypeEnum.Exit_Manual ||
                                   nodeItem.TipoModifica == (int)RegModifyTypeEnum.Entry_Modified ||
                                   nodeItem.TipoModifica == (int)RegModifyTypeEnum.E_Mod_U_Man ||
                                   nodeItem.TipoModifica == (int)RegModifyTypeEnum.Entry_Manual ||
                                   nodeItem.TipoModifica == (int)RegModifyTypeEnum.E_Man_U_Mod ||
                                   nodeItem.TipoModifica == (int)RegModifyTypeEnum.Both ||
                                   nodeItem.TipoModifica == (int)RegModifyTypeEnum.Manual)
                    fontColor =
                        Business.Repository.RepoManager.ParamRepo.GetColorFromEnum(BusinessService.ColorModify((RegModifyTypeEnum)nodeItem.TipoModifica, RegEUEnum.Exit), false);
                else
                    fontColor = Business.Repository.RepoManager.ParamRepo.GetColorFromEnum(RegModifyTypeEnum.None, false);


            range.Style.Font.Color.SetColor(fontColor);

            return currentValue;
        }
    }
}
