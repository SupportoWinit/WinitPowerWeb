using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridEditFormProvider.Components
{
    public class TabbedItem : FormItem
    {
        [JsonProperty("tabs")]
        public ICollection<Tab> Tabs { get; set; }
    }
}
