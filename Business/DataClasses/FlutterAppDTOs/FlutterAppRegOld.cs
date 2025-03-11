using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DataClasses.FlutterAppDTOs
{
    public class FlutterAppRegOld
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
        public IEnumerable<FlutterDataOld> Value { get; set; }
    }

    public class FlutterDataOld
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

        [JsonProperty("Latitudine", NullValueHandling = NullValueHandling.Ignore)]
        public double Latitudine { get; set; }

        [JsonProperty("Longitudine", NullValueHandling = NullValueHandling.Ignore)]
        public double Longitudine { get; set; }

        [JsonProperty("Attivita")]
        public string Attivita { get; set; }

        [JsonProperty("Squadra")]
        public string Squadra { get; set; }

        [JsonProperty("Cantiere")]
        public string Cantiere { get; set; }

        [JsonProperty("NfcGps")]
        public string NfcGps { get; set; }

        [JsonProperty("Note")]
        public string Note { get; set; }

        [JsonProperty("Chiave")]
        public string Chiave { get; set; }
    }
}
