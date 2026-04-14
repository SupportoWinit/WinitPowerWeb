using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Business.XmlExportsData.Manalu
{
    public class XmlDocuments
    {
        private Movimenti _movimenti = new Movimenti();
        private string _codAziendaUfficiale = string.Empty;
        private string _codDipendenteUfficiale = string.Empty;
        private string _codDipendenteRilPres = string.Empty;



        [XmlElement("Movimenti")]
        public Movimenti Masters
        {
            get { return _movimenti ?? (_movimenti = new Movimenti()); }
            set { _movimenti = value; }
        }

        [XmlAttribute]
        public string CodAziendaUfficiale
        {
            get { return _codAziendaUfficiale; }
            set { _codAziendaUfficiale = value; }
        }

        [XmlAttribute]
        public string CodDipendenteUfficiale
        {
            get { return _codDipendenteUfficiale; }
            set { _codDipendenteUfficiale = value; }
        }

        [XmlAttribute]
        public string CodDipendenteRilPres
        {
            get { return _codDipendenteRilPres; }
            set { _codDipendenteRilPres = value; }
        }
    }

    public class Movimenti
    {
        private List<Movimento> _movimenti = new List<Movimento>();
        private string _generazioneAutomaticaDaTeorico = "N";

        [XmlAttribute]
        public string GenerazioneAutomaticaDaTeorico
        {
            get { return _generazioneAutomaticaDaTeorico; }
            set { _generazioneAutomaticaDaTeorico = value; }
        }

        [XmlElement("Movimento")]
        public List<Movimento> Master
        {
            get { return _movimenti ?? (_movimenti = new List<Movimento>()); }
            set { _movimenti = value; }
        }
    }

    public class VociRetributive
    {
        private List<Voce> _voci = new List<Voce>();
        
        [XmlElement("Voce")]
        public List<Voce> Master
        {
            get { return _voci ?? (_voci = new List<Voce>());  }
            set { _voci = value; }
        }
    }

    public class Voce
    {
        public string CodVoceRilPres { get; set; }
        public string CodVoceUfficiale { get; set; }
        public string DataElaborazione { get; set; }
        public string CodTipoCedolino { get; set; }
        public string DataPresenzeMese { get; set; }
        public string CodTipoVoce { get; set; }
        public string Quantità { get; set; }
        public string ImpTariffaBase { get; set; }
        public string ImpTariffa { get; set; }
        public string ImpVoce { get; set; }
        public string DataInizioPeriodoComp { get; set; }
        public string DataFinePeriodoComp { get; set; }
    }

    public class Movimento
    {
        public string CodGiustificativoRilPres { get; set; }
        public string CodGiustificativoUfficiale { get; set; }
        public string Data { get; set; }
        public string NumOre { get; set; }
        public string NumMinuti { get; set; }
        public string NumMinutiInCentesimi { get; set; }
        public string GiornoDiRiposo { get; set; }
        public string GiornoChiusuraStraordinari { get; set; }



        /// <summary>
        /// Metodo per creare un nodo Movimento per i giorni di gap senza trimbrature, da segnalare se riposo o meno
        /// </summary>
        /// <param name="Riposo">true se è un giorno di riposo, false in caso contrario</param>
        /// <param name="date">data di riferimento del nodo da creare</param>
        /// <returns></returns>
        public static Movimento getFillNode(String riposo, DateTime date)
        {
            Movimento movimento = new Movimento()
            {
                CodGiustificativoRilPres = "",
                CodGiustificativoUfficiale = "",
                Data = date.ToString("yyyy-MM-dd"),
                NumOre = "0",
                NumMinuti = "0",
                NumMinutiInCentesimi = "0",
                GiornoDiRiposo = riposo,
                GiornoChiusuraStraordinari = (date.DayOfWeek.Equals(DayOfWeek.Sunday)) ? "S" : "N"
            };

            return movimento;
        }


    }
}
