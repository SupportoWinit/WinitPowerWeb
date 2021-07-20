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
        [JsonProperty("Fru ")]
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

        public FlutterOrderedReg(string Fru,string Pru,DateTime data,string verso,string motivazione,double latitudine, double longitudine, string attivita) {
            CodiceFru = Fru;
            CodicePru = Pru;
            Data = data;
            Verso = verso;
            Motivazione = motivazione;
            Latitudine = latitudine;
            Longitudine = longitudine;
            Attivita = attivita;
        }
    }
}
