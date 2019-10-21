using Business.BusinessServices.RegTranslatorService.Classes.JsonReg;
using Business.BusinessServices.RegTranslatorService.Enums;
using Business.BusinessServices.RegTranslatorService.Interfaces.RegsTranslatorsManagers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.RegTranslatorService.Classes.RegsTranslatorsManagers
{
    public class WinitJsonTranslatorManager : IRegTranslatorManager
    {
        public IEnumerable<string> Translate(IEnumerable<ClockAppReg> jsonRegs)
        {

            JArray jsonStringRegs = new JArray();

            foreach(ClockAppReg clockappReg in jsonRegs)
            {
                WinitJsonReg winitJsonReg = new WinitJsonReg();
                winitJsonReg.SiteCode = clockappReg.BadgeCode;
                winitJsonReg.CustomerCode = clockappReg.DeviceCode;
                winitJsonReg.DateTime = clockappReg.RegistrationDateTime;
                winitJsonReg.Direction = clockappReg.Direction.First();
                winitJsonReg.HasCoordinates = clockappReg.TransponderType == JsonRegTypeEnum.G;
                winitJsonReg.Source = "ANDROID";
                winitJsonReg.Coordinates = new Coordinates
                {
                    Latitude = new Location
                    {
                        Point = (float)clockappReg.Latitude,
                        Sector = (clockappReg.Latitude) < 0f ? 'S' : 'N'
                    },
                    Longitude = new Location
                    {
                        Point = (float)clockappReg.Longitude,
                        Sector = (clockappReg.Longitude) < 0f ? 'W' : 'E'
                    }
                };

                jsonStringRegs.Add(JObject.FromObject(winitJsonReg));
            }

            return new List<string>() { jsonStringRegs.ToString(Formatting.Indented) };
        }
    }
}
