using Business.GridHelpers.Classes.Decorations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridHelpers.Classes
{
    public abstract class Column
    {
        private IDictionary<Type, string> _propertyTypeTranslation;

        [JsonProperty("caption")]
        public string Caption { get; set; }

        [JsonProperty("calculateDisplayValue", NullValueHandling = NullValueHandling.Ignore)]
        public string CalculateDisplayValue { get; set; }

        [JsonProperty("dataField")]
        public string DataField { get; set; }

        [JsonProperty("dataType")]
        public string DataType { get; set; }

        [JsonProperty("lookup", NullValueHandling = NullValueHandling.Ignore)]
        public Lookup Lookup { get; set; }

        [JsonProperty("editorOptions", NullValueHandling = NullValueHandling.Ignore)]
        public object EditorOptions { get; set; }

        [JsonProperty("select")]
        public string[] Select { get; set; }

        [JsonProperty("showInColumnChooser")]
        public bool ShowInColumnChooser { get; set; }

        [JsonProperty("visible")]
        public bool Visible { get; set; }



        public Column(Type propertyType, string propertyName)
        {
            this.Caption = BusinessService.GetLocalizedString(propertyName, Common.ResourceTypeEnum.Field);
            this.DataType = ColumnsGenerator.ColumnsGenerator.GetTypeCaption(propertyType);
            this.DataField = propertyName;
            this.Visible = false;
            this.ShowInColumnChooser = true;
        }

       
    }
}
