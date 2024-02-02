using Business.XmlExportsData.Perfetto;
using DevExpress.XtraRichEdit.Import.Html;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Business.XmlExportsData.Scs
{

    #region DocumentInfo
    //Classe che gestisce i campi riguardanti le informazioni del dominio
    public class XmlDocumentInfo
    {

        #region Fields

        private XmlCreation _creation = new XmlCreation();

        #endregion

        #region Properties

        public string Namespace { get; set; }

        public string Profile { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public XmlCreation Creation
        {
            get { return _creation ?? (_creation = new XmlCreation()); }
            set { _creation = value; }
        }

        public string NextFile { get; set; }

        #endregion

    }

    public class XmlCreation
    {

        #region Properties

        public string Domain { get; set; }

        public string Site { get; set; }

        public string User { get; set; }

        public string DateTime { get; set; }

        #endregion

    }

    #endregion

    #region Documents

    //classe che gestisce il corpo del documento
    public class XmlDocument
    {

        #region Fields

        private Movimenti _lista = new Movimenti();

        private List<Voci> _lista2 = new List<Voci>();

        private string attribute = "";

        private string attribute2 = "";
        #endregion

        #region Properties
        [XmlElement("Movimenti")]
        public Movimenti Masters
        {
            get
            {
                if (_lista == null)
                    _lista = new Movimenti();

                return _lista;
            }
            set { _lista = value; }
        }  

        [XmlAttribute]
        public string CodAziendaUfficiale
        {
            get { return attribute; }
            set { attribute = value; }
        }

        [XmlAttribute]
        public string CodDipendenteUfficiale
        {
            get { return attribute2; }
            set { attribute2 = value; }
        }
        #endregion

    }
    public class XmlDocuments
    {

        #region Fields

        private Movimenti _lista = new Movimenti();

        //private List<Voci> _lista2 = new List<Voci>();

        private List<ZonaCantiere> _lista3 = new List<ZonaCantiere>();

        private string attribute = "";

        private string attribute2 = "";
        #endregion

        #region Properties
        [XmlElement("Movimenti")]
        public Movimenti Masters
        {
            get
            {
                if (_lista == null)
                    _lista = new Movimenti();

                return _lista;
            }
            set { _lista = value; }
        }

        //public List<Voci> VociRetributive
        //{
        //    get
        //    {
        //        if (_lista2 == null)
        //            _lista2 = new List<Voci>();
        //
        //        return _lista2;
        //    }
        //    set
        //    {
        //        _lista2 = value;
        //    }
        //}

        public List<ZonaCantiere> ForzatureZoneCantieri
        {
            get
            {
                if (_lista3 == null)
                    _lista3 = new List<ZonaCantiere>();

                return _lista3;
            }
            set
            {
                _lista3 = value;
            }
        }




        [XmlAttribute]
        public string CodAziendaUfficiale
        {
            get { return attribute; }
            set { attribute = value; }
        }

        [XmlAttribute]
        public string CodDipendenteUfficiale
        {
            get { return attribute2; }
            set { attribute2 = value; }
        }
        #endregion

    }

    #region Masters

    public class Movimenti
    {

        #region Constructor
        public Movimenti()
        {
            
        }
        #endregion

        #region Fields


        private List<Movimento> _lista = new List<Movimento>();

        private string attribute3 = "N";

        #endregion

        #region Properties

        [XmlAttribute]
        public string GenerazioneAutomaticaDaTeorico
        {
            get { return attribute3; }
            set { attribute3 = value; }
        }
        [XmlElement("Movimento")]
        public List<Movimento> Master
        {
            get { return _lista ?? (_lista = new List<Movimento>()); }
            set { _lista = value; }
        }

        #endregion

    }

    #region Master
    public class XmlMaster
    {


        #region Fields

        private List<Movimento> _row = new List<Movimento>();
        #endregion

        #region Properties

        public List<Movimento> Movimenti
        {
            get { return _row ?? (_row = new List<Movimento>()); }
            set { _row = value; }
        }



        #endregion

    }

    #region MasterFields

    public class XmlMasterFields
    {

        #region Properties

        public string WorkingReportDate { get; set; }

        public string Job { get; set; }

        #endregion

    }

    #endregion

    #region Slaves

    public class XmlSlaves
    {
        #region Fields

        private XmlSlaveBuff _slavebuff = new XmlSlaveBuff();

        #endregion

        #region Properties

        public XmlSlaveBuff SlaveBuff
        {
            get { return _slavebuff ?? (_slavebuff = new XmlSlaveBuff()); }
            set { _slavebuff = value; }





        }

        #endregion
    }

    #region Slave

    #region SlaveFields
    public class XmlSlaveFields
    {
        #region Properties
        public int OrdinaryTotalTime { get; set; }

        public int OvertimeTotalTime { get; set; }

        public int TravelTotalTime { get; set; }

        public int WorkingReportTotalTime { get; set; }

        public string Note { get; set; }
        #endregion
    }
    #endregion

    #endregion

    #region SlaveBuff
    public class XmlSlaveBuff
    {

        #region Fields

        private List<Movimento> _row = new List<Movimento>();

        //private string _namespace_OLD = "Dbt.ImpiantiNet.Rapportini.INRapportini.Rapportino.DBTRapportiniRighe";

        #endregion

        #region Constructor
        public XmlSlaveBuff()
        {
            
        }
        #endregion

        #region Properties

        [XmlAttribute]

        [XmlElementAttribute("Row")]
        public List<Movimento> Movimenti
        {
            get { return _row ?? (_row = new List<Movimento>()); }
            set { _row = value; }

        }

        #endregion



    }

    #region Row
    public class Movimento
    {

        #region Fields

        #region Properties

        public string CodGiustificativoUfficiale { get; set; }

        public string Data { get; set; }

        public string NumOre { get; set; }

        public string NumMinuti { get; set; }

        public string NumMinutiInCentesimi { get; set; }

        public string GiornoDiRiposo { get; set; }

        public string GiornoChiusuraStraordinari { get; set; }

        public string CodTurno { get; set; }

        public string CodFiscaleFiglio { get; set; }


        #endregion

        // private XmlRowFields _fields = new XmlRowFields();

        #endregion

        #region Constructor
        public Movimento()
        {
           
        }
        #endregion
    }

    public class ZonaCantiere
    {

        #region Fields

        #region Properties

        public string DataMovimento { get; set; }

        public string IdPosizione { get; set; }

        public string InquadramentoAzienda { get; set; }

        public string InquadramentoAziendaDIpendenze { get; set; }

        public string CodiceCantiere { get; set; }


        #endregion

        // private XmlRowFields _fields = new XmlRowFields();

        #endregion

        #region Constructor
        public ZonaCantiere()
        {

        }
        #endregion
    }

    public class Voci { 

        #region Fields

        #region Properties

        public string CodVoceRilPres { get; set; }

        public string CodVoceUfficiale { get; set; }

        public string DataElaborazione { get; set; }

        public string CodTipoCedolino { get; set; }

        public string DataPresenzeMese { get; set; }

        public string CodTipoVoce { get; set; }

        public string Quantita { get; set; }

        public string ImpTariffaBase { get; set; }

        public string ImpTariffa { get; set; }

        public string ImpVoce { get; set; }

        public string DataInizioPeriodoComp { get; set; }

        public string DataFinePeriodoComp { get; set; }

        public string NumMesiPreavviso { get; set; }


        #endregion

        // private XmlRowFields _fields = new XmlRowFields();

        #endregion

        #region Constructor
        public Voci()
        {
        }
        #endregion
    }

    #endregion


    #endregion

    #endregion

    #endregion

    #endregion

    #endregion

    #region Envelope content classes

    public sealed class XmlEnvelopeDocumentInfo
    {

        #region Properties

        public string Domain { get; set; }

        public string Site { get; set; }

        public string SiteCode { get; set; }

        public string User { get; set; }

        public string EnvelopeClass { get; set; }

        public string RootDocNamespace { get; set; }

        public string Datatime { get; set; }

        #endregion

    }

    public sealed class XmlEnvelopeContents
    {

        #region Fields

        private List<XmlFileTag> _file = new List<XmlFileTag>();

        #endregion

        #region Properties

        [XmlElementAttribute("File")]
        public List<XmlFileTag> File
        {
            get { return _file; }
            set { _file = value; }
        }


        #endregion

    }

    public sealed class XmlFileTag
    {

        #region Properties

        [XmlAttribute]
        public string type { get; set; }

        [XmlAttribute]
        public string dataurl { get; set; }

        [XmlAttribute]
        public string envelopeclass { get; set; }

        [XmlAttribute]
        public string documentname { get; set; }

        [XmlAttribute]
        public string documentnumber { get; set; }

        #endregion
    }

    #endregion

}
