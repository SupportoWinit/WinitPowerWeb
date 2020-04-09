using System;

namespace Business.BusinessServices.RegTranslatorService.Classes.JsonReg
{
    public class SolarisReg
    {
        public string Matriculation { get; set; }
        public int Cant { get; set; }
        public DateTime Date { get; set; }
        public int IdCentroDiCosto { get; set; }
        public string Verso { get; set; }
    }
}
