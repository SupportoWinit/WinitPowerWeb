using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DataClasses.WebApiDataClasses
{
    public class FileContainerJson
    {
        [JsonProperty("device")]
        public string Device { get; set; }

        [JsonProperty("nameFile")]
        public string FileName { get; set; }

        [JsonProperty("dataFile")]
        public string Data { get; set; }
    }
}
