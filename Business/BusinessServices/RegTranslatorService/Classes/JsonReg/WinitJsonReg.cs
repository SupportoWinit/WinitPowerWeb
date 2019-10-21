using Business.BusinessServices.RegTranslatorService.Interfaces.JsonReg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.RegTranslatorService.Classes.JsonReg
{
    public class WinitJsonReg : IJsonReg
    {
        public string SiteCode { get; set; }
        public string CustomerCode { get; set; }
        public DateTime DateTime { get; set; }
        public char Direction { get; set; }
        public bool HasCoordinates { get; set; }
        public string Source { get; set; }
        public Coordinates Coordinates { get; set; }

    }

    public class Coordinates
    {
        public Location Latitude { get; set; }
        public Location Longitude { get; set; }
    }

    public class Location
    {
        public float Point { get; set; }
        public char Sector { get; set; }
    }
}
