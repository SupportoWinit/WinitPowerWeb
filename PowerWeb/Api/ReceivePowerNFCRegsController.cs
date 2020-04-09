using Business.BusinessServices.RegTranslatorService;
using Business.BusinessServices.RegTranslatorService.Classes.JsonReg;
using Business.BusinessServices.RegTranslatorService.Service;
using Business.BusinessServices.RegWriterService;
using Business.BusinessServices.RegWriterService.Service;
using Business.Repository;
using Common;
using log4net;
using PowerWeb.Api.ModelBinders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ModelBinding;

namespace PowerWeb.Api
{
    public class ReceivePowerNFCRegsController : ApiController
    {
        static ILog _log;

        IRegTranslatorService _regTranslatorService;
        IRegWriterService _regWriterService;

        public ReceivePowerNFCRegsController() : this(new RegTranslatorService(), new RegWriterService())
        {

        }

        public ReceivePowerNFCRegsController(IRegTranslatorService regTranslatorService, IRegWriterService regWriterService)
        {
            _regTranslatorService = regTranslatorService;
            _regWriterService = regWriterService;
            _log = LogManager.GetLogger(GetType());
        }
        //Qui quando cambieremo il contentType inviato dall'app basterà togliere il modelBinder (al momento non è application/json ma binary octet)
        [HttpPost]
        public HttpResponseMessage Post([ModelBinder(typeof(ClockAppRegModelBinder))] IEnumerable<ClockAppReg> regs)
        {

            HttpResponseMessage responseMessage = new HttpResponseMessage();

            if (!ValidatePayload(regs, responseMessage))
                return responseMessage;

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.IWell) == 1)
            {
                try
                {
                    var stringifiedRegs = _regTranslatorService.TranslateClockAppRegsToStandardWinitJson(regs);
                    _regWriterService.WriteJsonRegsToFilesInput(stringifiedRegs.StringifiedRegs);
                    responseMessage.StatusCode = HttpStatusCode.OK;
                }
                catch (Exception ex)
                {
                    _log.ErrorFormat("Errore durante la traduzione da json in winitStandardJson : {0} \n{1}", ex.Message, ex.StackTrace);

                    responseMessage.StatusCode = HttpStatusCode.InternalServerError;
                    responseMessage.ReasonPhrase = "Internal parsing error.";
                }
                finally
                {
                    string deviceCode = regs.First().DeviceCode;
                    _regWriterService.BackUpJsonRegs(regs, deviceCode);
                }

                return responseMessage;
            }

            try
            {
                var stringifiedRegs = _regTranslatorService.TranslateClockAppRegs(regs);
                _regWriterService.WriteRegs(stringifiedRegs.StringifiedRegs, stringifiedRegs.DeviceCode);

                responseMessage.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la traduzione da json in txt : {0} \n{1}", ex.Message, ex.StackTrace);

                responseMessage.StatusCode = HttpStatusCode.InternalServerError;
                responseMessage.ReasonPhrase = "Internal parsing error. Backup executed.";
            }
            finally
            {
                string deviceCode = regs.First().DeviceCode;
                _regWriterService.BackUpJsonRegs(regs, deviceCode);
            }

            return responseMessage;
        }



        private bool ValidatePayload(IEnumerable<ClockAppReg> regs, HttpResponseMessage responseMessage)
        {
            if (regs == null)
            {
                responseMessage.ReasonPhrase = "Null payload!";
                responseMessage.StatusCode = HttpStatusCode.InternalServerError;

                _log.Error("Impossibile creare array di registrazioni ricevute tramite clockApp. Array == null");

                return false;
            }

            if (!regs.Any())
            {
                responseMessage.ReasonPhrase = "Empty payload! No actions needed.";
                responseMessage.StatusCode = HttpStatusCode.OK;

                _log.Error("Impossibile creare array di registrazioni ricevute tramite clockApp. Array vuoto.");

                return false;
            }

            return true;
        }
    }
}
