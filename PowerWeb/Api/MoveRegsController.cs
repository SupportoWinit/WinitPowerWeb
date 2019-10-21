using Business.Repository;
using Data;
using log4net;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PowerWeb.Api
{
    public class MoveRegsController : ApiController
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(MoveRegsController));

        [HttpGet]
        public HttpResponseMessage Post(DateTime to)
        {
            try
            {
                _log.Info($"Richiesta archiviazione automatica per registrazioni inferiori a : {to.ToShortDateString()}.");
                ((PowerWebEntities)RepoManager.ResourcesRepo.Context).Archive(DateTime.MinValue, to.AddDays(1));
                _log.Info($"Richiesta archiviazione automatica per registrazioni inferiori a : {to.ToShortDateString()} completata.");

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _log.Error($"Archiviazione automatica per registrazioni inferiori a : {to.ToShortDateString()} non è stata completata : {ex.GetBaseException().Message}.");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
    }

    public class ApiModel
    {
        [JsonProperty("from")]
        public DateTime From { get; set; }
        [JsonProperty("to")]
        public DateTime To { get; set; }
    }
}
