using Business.BusinessServices.RegTranslatorService.Interfaces.TranslatedRegs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.RegTranslatorService.Classes.TranslatedRegs
{
    public class TranslatedStringRegs : ITranslatedRegs
    {
        public string DeviceCode { get; set; }

        public IEnumerable<string> StringifiedRegs { get; set; }
    }
}
