using Business.GridHelpers.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridHelpers.Templates
{
    internal class CantClassTemplate
    {
        [KeyColumn]
        public int Cant_Id { get; set; }

        [NumericColumn]
        [VisibleColumn]
        public string Codice_Cantiere { get; set; }
        [NumericColumn]
        public string Cap_Can { get; set; }
        [ForeignKeyColumn]
        [LookupColumn("Cli_Id", "Codice_Cliente", "Cognome_Cli")]
        [CustomDisplayColumn("Cli.Codice_Cliente", "Cli.Cognome_Cli")]
        public Nullable<int> Cli_Id { get; set; }
        [DateColumn]
        public System.DateTime Data_Registrazione_Can { get; set; }
        [DateTimeColumn]
        public System.DateTime DataOraUltimaModifica_Can { get; set; }
        [StringColumn]
        [VisibleColumn]
        public string Descrizione_Can { get; set; }
        [VisibleColumn]
        [BooleanColumn]
        public bool DisAbilitazione_Can { get; set; }
        [ForeignKeyColumn]
        [CustomDisplayColumn("Fil.Codice_Fil","Fil.Descrizione_Fil")]
        [LookupColumn("Fil_Id", "Codice_Fil", "Descrizione_Fil")]
        public Nullable<int> Fil_Id { get; set; }
        [StringColumn]
        public string Indirizzo_Can { get; set; }
        [NumericColumn]
        public double LatitudineGps_Can { get; set; }
        [NumericColumn]
        public double LongitudineGps_Can { get; set; }
        [StringColumn]
        public string Luogo_Can { get; set; }
        [StringColumn]
        public string Nazione_Can { get; set; }
        [StringColumn]
        public string Provincia_Can { get; set; }
        [NumericColumn]
        public Nullable<short> RaggioGps_Can { get; set; }
        [BooleanColumn]
        public bool Singola_Reg { get; set; }
        [StringColumn]
        public string Tipologia_Can { get; set; }
        [StringColumn]
        public string Zona_Can { get; set; }

    }
}
