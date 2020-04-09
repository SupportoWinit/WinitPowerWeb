using Business.BusinessServices.RegTranslatorService.Enums;
using Business.BusinessServices.RegTranslatorService.Interfaces.JsonReg;
using System;

namespace Business.BusinessServices.RegTranslatorService.Classes.JsonReg
{
    public class ClockAppReg : IJsonReg
    {
        public Guid Id { get; set; }

        public string DeviceCode { get; set; }

        public string BadgeCode { get; set; }

        public DateTime RegistrationDateTime { get; set; }

        public string Direction { get; set; }

        public bool RegistrationSent { get; set; }

        public string RegistrationType { get; set; }

        public string ActivityTypeCode { get; set; }

        public string TurnCode { get; set; }

        public string PruCodeForActivity { get; set; }

        public string SubCantCode { get; set; }

        public string SubCantDesc { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public JsonRegTypeEnum TransponderType { get; set; }

        public string AudioCode { get; set; }

        public string RegistrationNote { get; set; }

        public string[] RegistrationSquadra { get; set; }

        public int IdManualCant { get; set; }
    }
}
