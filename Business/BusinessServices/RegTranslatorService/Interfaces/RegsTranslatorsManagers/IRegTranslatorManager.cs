using Business.BusinessServices.RegTranslatorService.Classes.JsonReg;
using Business.BusinessServices.RegTranslatorService.Interfaces.JsonReg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.RegTranslatorService.Interfaces.RegsTranslatorsManagers
{
    public interface IRegTranslatorManager
    {
        IEnumerable<string> Translate(IEnumerable<ClockAppReg> jsonRegs);
    }
}
