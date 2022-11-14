using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DataClasses.FlutterAppDTOs
{
    public class CountReg
    {
        [JsonProperty("COUNT(*)")]
        public string NumeroReg { get; set; }
    }
}
