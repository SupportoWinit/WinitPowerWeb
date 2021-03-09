using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DataClasses.FlutterAppDTOs
{
    public class FlutterAppReg
    {
        public int Id { get; set; }

        [JsonProperty("name")]
        public string CodiceFru { get; set; }

        [JsonProperty("tecnologia")]
        public string tecnologia { get; set; }

        [JsonProperty("hotspotNome")]
        public string hotspotNome { get; set; }

        [JsonProperty("hotspotTipo")]
        public string hotspotTipo { get; set; }

        [JsonProperty("Autorizzato")]
        public string Autorizzato { get; set; }

        [JsonProperty("CreateDateTime")]
        public string CreateDateTime { get; set; }

        [JsonProperty("valore")]
        public IEnumerable<FlutterData> Value { get; set; }
    }

    public class FlutterData
    {
        [JsonProperty("nfccode")]  
        public string CodicePru { get; set; }

        [JsonProperty("qrcode")]
        private string CodicePru2 { set { CodicePru = value; } }

        [JsonProperty("datetime")]
        public DateTime Registrazione_Data_Ora_Orig { get; set; }

        [JsonProperty("verso")]
        public string verso { get; set; }

        [JsonProperty("motivazione")]
        public string motivazione { get; set; }
    }
}
