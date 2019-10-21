using Business.GridHelpers.Attributes;
using System;

namespace Business.GridHelpers.Templates
{
    internal class ColClassTemplate
    {
        [KeyColumn]
        public int Col_Id { get; set; }
        [ForeignKeyColumn]
        public Nullable<int> Cant_Id { get; set; }
        [StringColumn]
        [VisibleColumn]
        public string Codice_Collaboratore { get; set; }
        [StringColumn]
        public string Codice_Domicilio_Luogo_Col { get; set; }
        [StringColumn]
        public string Codice_Fiscale_Col { get; set; }
        [StringColumn]
        public string Codice_Iban_Col { get; set; }
        [StringColumn]
        public string Codice_Nascita_Luogo_Col { get; set; }
        [StringColumn]
        public string Codice_Residenza_Luogo_Col { get; set; }
        [StringColumn]
        [VisibleColumn]
        public string Cognome_Col { get; set; }
        [DateColumn]
        public Nullable<DateTime> Data_Disponibilita_Fine_Col { get; set; }
        [DateColumn]
        public Nullable<DateTime> Data_Disponibilita_Inizio_Col { get; set; }
        [DateColumn]
        public DateTime Data_Registrazione_Col { get; set; }
        [DateColumn]
        public Nullable<DateTime> Data_Sorv_San_Col { get; set; }
        [DateTimeColumn]
        public DateTime DataOraUltimaModifica_Col { get; set; }
        [BooleanColumn]
        public bool Disabile_Col { get; set; }
        [BooleanColumn]
        public bool DisAbilitazione_Col { get; set; }
        [StringColumn]
        public string Domicilio_Cap_Col { get; set; }
        [StringColumn]
        public string Domicilio_Indirizzo_Col { get; set; }
        [StringColumn]
        public string Domicilio_Interno_Col { get; set; }
        [StringColumn]
        public string Domicilio_Localita_Col { get; set; }
        [StringColumn]
        public string Domicilio_Luogo_Col { get; set; }
        [NumericColumn]
        public double LatitudineGps_Col { get; set; }
        [NumericColumn]
        public double LongitudineGps_Col { get; set; }
        [StringColumn]
        [VisibleColumn]
        public string Nome_Col { get; set; }

    }
}
