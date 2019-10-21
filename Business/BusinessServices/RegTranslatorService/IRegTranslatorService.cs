using Business.BusinessServices.RegTranslatorService.Classes.JsonReg;
using Business.BusinessServices.RegTranslatorService.Interfaces.TranslatedRegs;
using System.Collections.Generic;

namespace Business.BusinessServices.RegTranslatorService
{
    public interface IRegTranslatorService
    {
        ITranslatedRegs TranslateClockAppRegs(IEnumerable<ClockAppReg> jsonRegs);
        ITranslatedRegs TranslateClockAppRegsToStandardWinitJson(IEnumerable<ClockAppReg> jonRegs);
    }
}
