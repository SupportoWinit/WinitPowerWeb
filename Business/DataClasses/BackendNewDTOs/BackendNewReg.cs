using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Business.DataClasses.BackendNewDTOs
{
    public class BackendNewReg
    {
        public int Id { get; set; }

        [JsonProperty("matricola")]
        public string CodiceFru { get; set; }

        [JsonProperty("DataRegistrazione")]
        public DateTime DataRegistrazione { get; set; }

        [JsonProperty("Valore NFC")]
        public string CodiceNFC { get; set; }

        [JsonProperty("verso")]
        public string verso { get; set; }
    }

    public class IdsResponse
    {
        [JsonPropertyName("ids")]
        public List<int> Ids { get; set; }
    }
}
