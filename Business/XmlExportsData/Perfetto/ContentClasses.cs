using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Business.XmlExportsData.Perfetto
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
    public class XmlDocuments
    {

        #region Fields

        private XmlMasters _masters = new XmlMasters();

        #endregion

        #region Properties

        public XmlMasters Masters
        {
            get
            {
                if (_masters == null)
                    _masters = new XmlMasters();

                return _masters;
            }
            set { _masters = value; }
        }






        #endregion

    }

    #region Masters

    public class XmlMasters
    {

        #region Constructor
        public XmlMasters()
        {
            table = "IM_WorkingReports";

            instances = "1";

        }
        #endregion

        #region Fields

        //private string _namespace_OLD = "Dbt.ImpiantiNet.Rapportini.INRapportini.Rapportino.DBTRapportiniTesta";
        private string _namespace = "Dbt.Perfetto.WorkingReports.Documents.JobWorkingReports.WorkingReport";
        
        private string _table = "IM_WorkingReports";

        private string _instances = "1";


        private List<XmlMaster> _master = new List<XmlMaster>();

        #endregion

        #region Properties

        [XmlElementAttribute("Master")]
        public List<XmlMaster> Master
        {
            get { return _master ?? (_master = new List<XmlMaster>()); }
            set { _master = value; }
        }

        [XmlAttribute]
        public string @namespace
        {
            get { return _namespace; }
            set { _namespace = value; }
        }

        [XmlAttribute]
        public string table
        {
            get { return _table; }
            set { _table = value; }
        }

        [XmlAttribute]
        public string instances
        {
            get { return _instances; }
            set { _instances = value; }
        }

        #endregion

    }

    #region Master
    public class XmlMaster
    {

        #region Fields
        private XmlMasterFields _fields = new XmlMasterFields();

        private XmlSlaves _slaves = new XmlSlaves();
        #endregion

        #region Properties

        public XmlMasterFields Fields
        {
            get { return _fields ?? (_fields = new XmlMasterFields()); }
            set { _fields = value; }
        }

        public XmlSlaves Slaves
        {
            get { return _slaves ?? (_slaves = new XmlSlaves()); }
            set { _slaves = value; }


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

        private List<XmlRow> _row = new List<XmlRow>();

        //private string _namespace_OLD = "Dbt.ImpiantiNet.Rapportini.INRapportini.Rapportino.DBTRapportiniRighe";

        private string _namespace = "Dbt.Perfetto.WorkingReports.Documents.JobWorkingReports.Details";

        private string _table = "IM_WorkingReportsDetails";

        #endregion

        #region Constructor
        public XmlSlaveBuff()
        {
            table = "IM_WorkingReportsDetails";
        }
        #endregion

        #region Properties

        [XmlAttribute]
        public int rowsnumber { get; set; }

        [XmlAttribute]
        public string @namespace
        {
            get { return _namespace; }
            set { _namespace = value; }
        }

        [XmlAttribute]
        public string table
        {
            get { return _table; }
            set { _table = value; }
        }

        [XmlElementAttribute("Row")]
        public List<XmlRow> Row
        {
            get { return _row ?? (_row = new List<XmlRow>()); }
            set { _row = value; }

        }

        #endregion



    }

    #region Row
    public class XmlRow
    {

        #region Fields

        private XmlRowFields _fields = new XmlRowFields();

        #endregion

        #region Constructor
        public XmlRow()
        {
            //numero di riga
            number = 0;


        }
        #endregion

        #region Properties

        [XmlAttribute]
        public int number { get; set; }

        public XmlRowFields Fields
        {
            get { return _fields ?? (_fields = new XmlRowFields()); }
            set { _fields = value; }

        }

        #endregion
    }

    #region RowFields
    public class XmlRowFields
    {

        #region Properties

        public int Line { get; set; }

        public string Employee { get; set; }

        public int OrdinaryHours { get; set; }

        public int OvertimeHours { get; set; }

        public int TravelHours { get; set; }

        public int VacationLeaveHours { get; set; }

        public int SickLeaveHours { get; set; }

        public string Job { get; set; }

        public string WorkingReportDate { get; set; }

        public int CustomHours1 { get; set; }

        public int CustomHours2 { get; set; }

        public int CustomHours3 { get; set; }

        public int CustomHours4 { get; set; }

        public string Note { get; set; }

        public int StartHour { get; set; }

        public int EndHour { get; set; }

        #endregion

    }
    #endregion

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

    #region Constants

    /// <summary>
    /// Classe utilizzata per contenere le costanti utilizzate nell'esportazione xml verso Perfetto
    /// </summary>
    public static class XmlToPerfettoConstants
    {

        #region Public Static Constants

        /// <summary>
        /// Il nome file dell'envelope da affiancare al rapportino
        /// </summary>
        public const string EnvelopeFileName = "Envelope.xml";

        /// <summary>
        /// Estensione del file xml
        /// </summary>
        public const string ReturnXmlExtension = ".xml";


        /// <summary>
        ///Ore ordinarie di una giornata in secondi
        /// </summary>
        public const int OrdinaryHours = 3600 * 8;


        /// <summary>
        /// L'estensione del file zip utilizzato per il ritorno dei dati dell'export
        /// </summary>
        public const string ReturnZipExtension = ".zip";

        /// <summary>
        /// Valore del documentnumber dell'envelope valore fisso ad 1
        /// </summary>
        public const int Documentnumber = 1;

        /// <summary>
        /// L'id del cantiere delle ferie
        /// </summary>
        public const int CantVacationId = 41800;

        /// <summary>
        /// Il codice del cantiere delle ferie
        /// </summary>
        public const string CantVacationMnemonic = "14/00197";

        #endregion

    }

    #endregion

}
