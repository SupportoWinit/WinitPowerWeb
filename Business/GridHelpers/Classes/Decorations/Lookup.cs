using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridHelpers.Classes.Decorations
{
    public class Lookup
    {
        [JsonProperty("dataSource")]
        public DataSource DataSource { get; set; }
    }
}
