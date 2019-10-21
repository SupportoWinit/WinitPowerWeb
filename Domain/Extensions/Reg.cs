using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
    public enum FlagEURegTypeEnum
    {
        None,
        E,
        U,
    }

    public partial class Reg
    {
        public FlagEURegTypeEnum FlagEURegTypeEnum
        {
            get
            {
                if (Flag_EU_Reg == "E")
                    return FlagEURegTypeEnum.E;
                if (Flag_EU_Reg == "U")
                    return FlagEURegTypeEnum.U;
                return FlagEURegTypeEnum.None;
            }
        }

        public RegTypeEnum Registrazione_Tipo_RegEnum
        {
            get
            {
                return (RegTypeEnum)Registrazione_Tipo_Reg;
            }
            set
            {
                if (Enum.IsDefined(typeof(RegTypeEnum), value))
                    Registrazione_Tipo_Reg = (int)value;
            }
        }

        public RegStateEnum Registrazione_Stato_RegEnum
        {
            get
            {
                return (RegStateEnum)Registrazione_Stato_Reg;
            }
            set
            {
                if (Enum.IsDefined(typeof(RegStateEnum), value))
                    Registrazione_Stato_Reg = (int)value;
            }
        }

        public string TipoInterventoCan
        {
            get
            {
                return Cant != null ? Cant.Tipo_Interv_Can : String.Empty;
            }
        }




        /// <summary>
        /// Ritorna il codice fru dell'unità portatile collegata alla registrazione corrente.
        /// </summary>
        /// <value>
        /// Il codice fru dell'unità portatile collegata alla registrazione corrente.
        /// </value>
        public string FruCode
        {
            get
            {
                string returnValue = String.Empty;

                if (Fru != null)
                    returnValue = Fru.Codice_Fru;

                return returnValue;
            }
        }

        


        /// <summary>
        /// Recupera il valore che indica se la registrazione corrente proviene da una causale inserita a dispositivo.
        /// </summary>
        /// <value>
        /// <c>true</c> se la registrazione corrente proviene da una causale inserita a dispositivo; altrimenti, <c>false</c>.
        /// </value>
        public bool IsFromDeviceActivity
        {
            get
            {
                // la registrazione risulta proveniente da una causale (un'attività da dispositivo) se non è stata cambiata di cantiere
                // oppure è manuale (Fru vuota) e il badge originale è valorizzato ed è marcato tra le attività
                bool isFromDeviceActivity = false;

                if (Fru != null && !String.IsNullOrEmpty(Registrazione_Badge_Originale))
                    isFromDeviceActivity = CommonService.IsActivityDeviceCode(Registrazione_Badge_Originale);

                return isFromDeviceActivity;
            }
        }

        /// <summary>
        /// Recupera il valore che indica se la registrazione corrente proviene da una causale inserita a dispositivo di tipo tappo.
        /// </summary>
        /// <value>
        /// <c>true</c> se la registrazione corrente proviene da una causale inserita a dispositivo di tipo tappo; altrimenti, <c>false</c>.
        /// </value>
        public bool IsEndDeviceActivity
        {
            get
            {
                // si tratta di una registrazione attività tappo quando è una causale e il badge originale è il badge di chiusura
                return IsFromDeviceActivity && Registrazione_Badge_Originale == Common.Properties.Settings.Default.EndActivityCode;
            }
        }

        public override bool Equals(object obj)
        {
            Reg reg = obj as Reg;

            if (reg == null)
                return false;

            bool fruEquals = reg.Fru_Id == this.Fru_Id;
            bool pruEquals = reg.Pru_Id == this.Pru_Id;

            bool dateEquals = reg.Registrazione_Data_Ora_Orig_Reg == this.Registrazione_Data_Ora_Orig_Reg;

            bool badgeEquals = reg.Registrazione_Badge_Originale == this.Registrazione_Badge_Originale;

            bool latEquals = reg.Registrazione_Lat_Orig == this.Registrazione_Lat_Orig;
            bool longEquals = reg.Registrazione_Long_Orig == this.Registrazione_Long_Orig;

            return fruEquals && pruEquals && dateEquals && badgeEquals && latEquals && longEquals;
        }

        public override int GetHashCode()
        {
            int hash = 0;

            try
            {
                hash += (this.Fru_Id.HasValue) ? this.Fru_Id.GetHashCode() : 0;
                hash += (this.Pru_Id.HasValue) ? this.Pru_Id.GetHashCode() : 0;

                hash += this.Registrazione_Data_Ora_Orig_Reg.ToString().GetHashCode();

                hash += (this.Registrazione_Badge_Originale != null && this.Registrazione_Badge_Originale != "") ? this.Registrazione_Badge_Originale.GetHashCode() : 0;

                hash += (this.Registrazione_Lat_Orig.HasValue) ? this.Registrazione_Lat_Orig.GetHashCode() : 0;
                hash += (this.Registrazione_Long_Orig.HasValue) ? this.Registrazione_Long_Orig.GetHashCode() : 0;
            }
            catch (Exception ex)
            {
            }

            return hash;
        }


    }
}
