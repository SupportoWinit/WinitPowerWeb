using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridEditFormProvider.Components
{
    public abstract class FormItem
    {
        [JsonProperty("colSpan")]
        public int ColSpan { get; set; }

        [JsonProperty("itemType")]
        public string ItemType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("items")]
        public ICollection<FormItem> Items { get; set; }
    }
}
