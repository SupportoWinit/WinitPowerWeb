using Newtonsoft.Json;
using System;

namespace Business.DataClasses.GeoBadgeDTOs
{
    public class GeoBadgeReg
    {

        public int Id { get; set; }

        [JsonProperty("IdTenant")]
        public int IdTenant { get; set; }

        [JsonProperty("BadgeLavoratore")]
        public string CodicePru { get; set; }

        [JsonProperty("IdLavoratore")]
        public int Col_Id { get; set; }

        [JsonProperty("Lavoratore")]
        public string Nome_Col { get; set; }

        [JsonProperty("CognomeLavoratore")]
        public string Cognome_Col { get; set; }

        [JsonProperty("IdTerminale")]
        public int Cant_Id { get; set; }
        [JsonProperty("Terminale")]
        public string CodiceFru { get; set; }
        [JsonProperty("CodiceTerminale")]
        public string Codice_Cantiere { get; set; }
        [JsonProperty("DataOra")]
        public DateTime Registrazione_Data_Ora_Orig { get; set; }
        public bool DataOraDaDispositivo { get; set; }
        public DateTime DataOraDelDispositivo { get; set; }
        public string Verso { get; set; }
        public string Coordinate { get; set; }
        public string IdURLGoogleMaps { get; set; }
        public int IdAttivita { get; set; }
        public string Attivita { get; set; }
        public string SistemaOperativo { get; set; }
        public string VersioneSistemaOperativo { get; set; }
        public string InformazioniVarieDispositivo { get; set; }
        public string UUID { get; set; }
        public string VersioneApp { get; set; }
        public string Note { get; set; }
        public string IdBeacon { get; set; }
        public bool Manuale { get; set; }
        public string TimbraturaGeograficamenteValida { get; set; }
        public bool Annullata { get; set; }

    }
}
