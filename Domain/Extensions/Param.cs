using Common;
using System;

namespace Domain
{
    public partial class Param
    {
        public DomainFilterEnum DomainFilterEnum
        {
            get
            {
                return (DomainFilterEnum)DomainFilter;
            }
        }

        public ModuleTypeEnum ModuleTypeEnum
        {
            get
            {
                return (ModuleTypeEnum)ModuleType;
            }
        }

        public MothlyHoursEnum MonthlyHoursEnum
        {
            get
            {
                return (MothlyHoursEnum)Flag_Monte_Ore;
            }
        }

        public CheckMunicipalityEnum Ctrl_Tab_ComuniEnum
        {
            get
            {
                return (CheckMunicipalityEnum)Ctrl_Tab_Comuni;
            }
        }

        public CheckCodFiscEnum Ctrl_Codice_FiscEnum
        {
            get
            {
                return (CheckCodFiscEnum)Ctrl_Codice_Fisc;
            }
        }

        public CheckIBANEnum Ctrl_Codice_IBANEnum
        {
            get
            {
                return (CheckIBANEnum)Ctrl_Codice_IBAN;
            }
        }

        /// <summary>
        /// Recupera il valore che identifica il tipo di associazione da impostare sul tag nelle timbrature tag e GPS.
        /// </summary>
        /// <value>
        /// Il valore che identifica il tipo di associazione da impostare sul tag nelle timbrature tag e GPS.
        /// </value>
        public GpsAssTagTypeEnum GpsAssTagType
        {
            get
            {
                // di default il tag non abbina
                var returnType = GpsAssTagTypeEnum.None;

                if (!String.IsNullOrEmpty(Tipo_Ass_Tag_Gps))
                    Enum.TryParse<GpsAssTagTypeEnum>(Tipo_Ass_Tag_Gps, true, out returnType);

                return returnType;
            }
        }

        /// <summary>
        /// Recupera il valore che identifica il tipo di chiusura delle causali da dispositivo.
        /// </summary>
        /// <value>
        /// Il valore che identifica il tipo di chiusura delle causali da dispositivo.
        /// </value>
        public DeviceActivityClosingTypeEnum DeviceActivityClosingType
        {
            get
            {
                // di default il si racchiude tutto in singole reg
                var returnType = DeviceActivityClosingTypeEnum.NoClosure;

                if (!String.IsNullOrEmpty(Tipo_Chiusura_Causali))
                    Enum.TryParse<DeviceActivityClosingTypeEnum>(Tipo_Chiusura_Causali, true, out returnType);

                return returnType;
            }
        }


        /// <summary>
        /// Recupera il valore che identifica come importare le registrazioni GPS.
        /// </summary>
        /// <value>
        /// Il valore che identifica come importare le registrazioni GPS.
        /// </value>
        public ImportGPSRegsEnum ImportGPSRegs
        {
            get
            {
                // di default le registrazioni GPS si importano normalmente
                var returnType = ImportGPSRegsEnum.None;

                if (!String.IsNullOrEmpty(Importazione_timbrature_GPS))
                    Enum.TryParse<ImportGPSRegsEnum>(Importazione_timbrature_GPS, true, out returnType);

                return returnType;
            }
        }
    }
}
