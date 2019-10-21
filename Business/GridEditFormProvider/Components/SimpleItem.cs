using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridEditFormProvider.Components
{
    public class SimpleItem : FormItem
    {
        public SimpleItem()
        {
            this.ItemType = "simple";
        }

        [JsonProperty("dataField")]
        public string DataField { get; set; }

        [JsonProperty("editorOptions")]
        public object EditorOptions { get; set; }
    }
}
