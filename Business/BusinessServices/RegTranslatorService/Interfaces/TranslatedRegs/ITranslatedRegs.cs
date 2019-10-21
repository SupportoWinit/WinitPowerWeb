using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.RegTranslatorService.Interfaces.TranslatedRegs
{
    public interface ITranslatedRegs
    {
        string DeviceCode { get; set; }

        IEnumerable<string> StringifiedRegs { get; set; }
    }
}
