using System;
using System.Collections.Generic;
using Westwind.Utilities;

namespace Domain.Extensions
{
    public class ActivityItem : Expando
    {
        #region Constructor

        public ActivityItem()
        {
            AdditionalCols = new List<String>();
        }

        #endregion

        #region Private Fields

        private DateTime? _tempoPrevisto;

        #endregion

        #region Public Properties

        public string CantCodFisc { get; set; }
        public string CantDesc { get; set; }
        public string CantPlace { get; set; }
        public string CantAddress { get; set; }
        public string RegKM { get; set; }
        public string CantCAP { get; set; }
        public string CantASL { get; set; }
        public string ColCognome { get; set; }
        public string ColNome { get; set; }
        public string ColCodice { get; set; }
        public int RegGG
        {
            get { return RegDate.Day; }
        }
        public int RegMM
        {
            get { return RegDate.Month; }
        }
        public int RegAAAA
        {
            get { return RegDate.Year; }
        }
        public TimeSpan RegHHMMSSInizio { get; set; }
        public TimeSpan RegHHMMSSFine { get; set; }
        public int RegMMDurata { get; set; }
        public TimeSpan RegHHMMSSDurata
        {
            get
            {
                return RegHHMMSSFine.Subtract(RegHHMMSSInizio);
            }
        }
        public DateTime RegDate { get; set; }
        public int TipoModifica { get; set; }
        public string ColDistretto { get; set; }
        public string ColQualifica { get; set; }
        public string RegTipo { get; set; }
        public string Cli_Tipo { get; set; }
        public string Cant_Livello { get; set; }
        public List<string> AdditionalCols { get; set; }

        // proprietà utilizzate nella personalizzazione Kleo dell'export 56
        public string CapoAreaCant { get; set; }
        public string CapoAreaCol { get; set; }
        public DateTime? TempoPrevisto
        {
            get
            {
                return RegTipo != "VIAGGIO" ? _tempoPrevisto : (DateTime?)null;
            }

            set
            {
                _tempoPrevisto = value;
            }
        }

        #endregion
    }
}
