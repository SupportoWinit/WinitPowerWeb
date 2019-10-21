using Business.GridEditFormProvider.Components;
using Business.GridEditFormProvider.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridEditFormProvider
{
    public abstract class GridEditFormBase
    {
        private static IDictionary<EditorTypeEnum, string> _editorTypeManager;

        static GridEditFormBase()
        {
            _editorTypeManager = new Dictionary<EditorTypeEnum, string>();

            _editorTypeManager.Add(EditorTypeEnum.DxAutocomplete, "dxAutoComplete");
            _editorTypeManager.Add(EditorTypeEnum.DxCalendar, "dxCalendar");
            _editorTypeManager.Add(EditorTypeEnum.DxLookup, "dxLookup");
            _editorTypeManager.Add(EditorTypeEnum.DxCheckBox, "dxCheckBox");
            _editorTypeManager.Add(EditorTypeEnum.DxColorBox, "dxColorBox");
            _editorTypeManager.Add(EditorTypeEnum.DxDateBox, "dxDateBox");
            _editorTypeManager.Add(EditorTypeEnum.DxDropDownBox, "dxDropDownBox");
            _editorTypeManager.Add(EditorTypeEnum.DxNumberBox, "dxNumberBox");
            _editorTypeManager.Add(EditorTypeEnum.DxRadioGroup, "dxRadioGroup");
            _editorTypeManager.Add(EditorTypeEnum.DxRangeSlider, "dxRangeSlider");
            _editorTypeManager.Add(EditorTypeEnum.DxSelectBox, "dxSelectBox");
            _editorTypeManager.Add(EditorTypeEnum.DxSlider, "dxSlider");
            _editorTypeManager.Add(EditorTypeEnum.DxTagBox, "dxTagBox");
            _editorTypeManager.Add(EditorTypeEnum.DxTextArea, "dxTextArea");
            _editorTypeManager.Add(EditorTypeEnum.DxTextBox, "dxTextBox");

        }
        
        protected IEnumerable<string> Tabs
        {
            get
            {
                return new List<string>()
                {

                };
            }
        }

        protected FormItem GenerateSimpleItem(string dataField)
        {
            return new SimpleItem
            {
                DataField = dataField,
                ItemType = _editorTypeManager[EditorTypeEnum.DxTextBox]
            };
        }

        protected FormItem GenerateSimpleItem(string dataField, EditorTypeEnum editorType)
        {
            return new SimpleItem
            {
                DataField = dataField,
                ItemType = _editorTypeManager[editorType]
            };
        }

        protected FormItem GenerateSimpleItem(string dataField, EditorTypeEnum editorType,object editorOptions)
        {
            return new SimpleItem
            {
                DataField = dataField,
                ItemType = _editorTypeManager[editorType],
                EditorOptions = editorOptions
            };
        }

    }
}
