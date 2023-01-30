using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DataClasses.FlutterAppDTOs
{
    class FlutterOrderedReg
    {
        [JsonProperty("Fru")]
        public string CodiceFru { get; set; }

        [JsonProperty("Pru")]
        public string CodicePru { get; set; }

        [JsonProperty("Data")]
        public DateTime Data { get; set; }

        [JsonProperty("Verso")]
        public string Verso { get; set; }

        [JsonProperty("Motivazione")]
        public string Motivazione { get; set; }

        [JsonProperty("Latitudine")]
        public double Latitudine { get; set; }

        [JsonProperty("Longitudine")]
        public double Longitudine { get; set; }

        [JsonProperty("Attivita")]
        public string Attivita { get; set; }

        [JsonProperty("Squadra")]
        public string Squadra { get; set; }

        [JsonProperty("Cantiere")]
        public String Cantiere { get; set; }

        [JsonProperty("NfcGps")]
        public String NfcGps { get; set; }

        [JsonProperty("Dataord")]
        public String Dataord { get; set; }

        [JsonProperty("Tecnologia")]
        public String Tecnologia { get; set; }

        public FlutterOrderedReg(string Fru,string Pru,DateTime data,string verso,string motivazione,double latitudine, double longitudine, string attivita,string squadra, string cantiere,string nfcGps, String dataord, string tecnologia) {
            CodiceFru = Fru;
            CodicePru = Pru;
            Data = data;
            Verso = verso != null ? verso : "";
            Motivazione = motivazione != null ? motivazione : ""; 
            Latitudine = latitudine != 0 ? latitudine : 0;
            Longitudine = longitudine != 0 ? longitudine : 0;
            Attivita = attivita != null ? attivita : "";
            Squadra = squadra != null ? squadra : "";
            Cantiere = cantiere != null ? cantiere : "";
            NfcGps = nfcGps != null? nfcGps : "";
            Dataord = dataord;
            Tecnologia = tecnologia != null ? tecnologia : "";
        }
    }
}
