using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridEditFormProvider.Components
{
    public class Form
    {
        [JsonProperty("colCount")]
        public int ColCount { get; set; }

        [JsonProperty("items")]
        public ICollection<FormItem> Items { get; set; }
    }
}
