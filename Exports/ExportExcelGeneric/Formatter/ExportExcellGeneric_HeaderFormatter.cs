using System;
using System.Drawing;
using System.Linq;
using Business.ExportExcelEngine;
using Domain;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Exports.ExportExcelGeneric.Formatter
{
    public class ExportExcellGeneric_HeaderFormatter : ExportExcelTreeNodeAbstractFormatter
    {
        private static ExcelStyle _style;

        public override object Format(object currentValue, ExcelRange range, ExportExcelTreeNode currentNode)
        {

            if (System.Text.RegularExpressions.Regex.IsMatch(range.Address, @"A\d+"))
                _style = range.Style;
            else
            {
                range.Style.Hidden = _style.Hidden;
                range.Style.HorizontalAlignment = _style.HorizontalAlignment;
                if (_style.Indent >= 0 && _style.Indent <= 250)
                    range.Style.Indent = _style.Indent;
                range.Style.Locked = _style.Locked;
                range.Style.ReadingOrder = _style.ReadingOrder;
                range.Style.ShrinkToFit = _style.ShrinkToFit;
                if (_style.TextRotation >= 0 && _style.TextRotation <= 180)
                    range.Style.TextRotation = _style.TextRotation;
                range.Style.VerticalAlignment = _style.VerticalAlignment;
                range.Style.WrapText = _style.WrapText;
                range.Style.XfId = _style.XfId;

                #region Font

                var fontStyle = FontStyle.Regular;
                if (_style.Font.Bold)
                    fontStyle |= FontStyle.Bold;

                if (_style.Font.Italic)
                    fontStyle |= FontStyle.Italic;

                if (_style.Font.Strike)
                    fontStyle |= FontStyle.Strikeout;

                if (_style.Font.UnderLine)
                    fontStyle |= FontStyle.Underline;

                range.Style.Font.SetFromFont(new Font(_style.Font.Name, _style.Font.Size, fontStyle));
                range.Style.Font.UnderLineType = _style.Font.UnderLineType;
                range.Style.Font.VerticalAlign = _style.Font.VerticalAlign;
                SetColor(range.Style.Font.Color, _style.Font.Color);

                #endregion


                #region BackgroundColor

                range.Style.Fill.PatternType = _style.Fill.PatternType;
                SetColor(range.Style.Fill.BackgroundColor, _style.Fill.BackgroundColor);

                #endregion


                #region Border

                range.Style.Border.Left.Style = _style.Border.Left.Style;
                range.Style.Border.Right.Style = _style.Border.Right.Style;
                range.Style.Border.Top.Style = _style.Border.Top.Style;
                range.Style.Border.Bottom.Style = _style.Border.Bottom.Style;

                SetColor(range.Style.Border.Left.Color, _style.Border.Left.Color);
                SetColor(range.Style.Border.Right.Color, _style.Border.Right.Color);
                SetColor(range.Style.Border.Top.Color, _style.Border.Top.Color);
                SetColor(range.Style.Border.Bottom.Color, _style.Border.Bottom.Color);

                #endregion


            }

            return currentValue;
        }

        private void SetColor(ExcelColor color, ExcelColor template)
        {
            //if (template.Indexed >= 0)
            //    color.Indexed = template.Indexed;
            
            //if (color.Tint > 0)
            //    color.Tint = template.Tint;
            
            if (!string.IsNullOrEmpty(template.Rgb))
                color.SetColor(ColorTranslator.FromHtml(string.Format("#{0}", template.Rgb)));
        }

    }
}