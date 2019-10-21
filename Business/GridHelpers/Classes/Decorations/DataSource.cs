using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridHelpers.Classes.Decorations
{
    public class DataSource
    {
        [JsonProperty("select")]
        public string[] Select { get; set; }

        [JsonProperty("paginate")]
        public bool Paginate { get; set; }

    }
}
