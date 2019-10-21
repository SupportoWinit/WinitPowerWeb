using System;
using System.Collections.Generic;
using Business.BusinessServices.RegTranslatorService.Classes.JsonReg;
using Business.BusinessServices.RegTranslatorService.Interfaces.RegsTranslatorsManagers;
using Business.BusinessServices.RegTranslatorService.Classes.RegsTranslatorsManagers;
using Business.BusinessServices.RegTranslatorService.Interfaces.TranslatedRegs;
using Business.BusinessServices.RegTranslatorService.Classes.TranslatedRegs;
using System.Linq;

namespace Business.BusinessServices.RegTranslatorService.Service
{
    public class RegTranslatorService : IRegTranslatorService
    {
        IDictionary<Type, IRegTranslatorManager> _translatorManager;

        public RegTranslatorService()
        {
            _translatorManager = new Dictionary<Type, IRegTranslatorManager>()
            {
                { typeof(ClockAppReg), new ClockAppRegTranslatorManager() },
                { typeof(WinitJsonReg), new WinitJsonTranslatorManager() }
            };

        }

        public ITranslatedRegs TranslateClockAppRegs(IEnumerable<ClockAppReg> jsonRegs)
        {
            return new TranslatedStringRegs
            {
                DeviceCode = jsonRegs.First().DeviceCode,
                StringifiedRegs = _translatorManager[typeof(ClockAppReg)].Translate(jsonRegs)
            };
        }

        public ITranslatedRegs TranslateClockAppRegsToStandardWinitJson(IEnumerable<ClockAppReg> jsonRegs)
        {
            return new TranslatedStringRegs
            {
                DeviceCode = jsonRegs.First().DeviceCode,
                StringifiedRegs = _translatorManager[typeof(WinitJsonReg)].Translate(jsonRegs)
            };
        }
        
    }
}
