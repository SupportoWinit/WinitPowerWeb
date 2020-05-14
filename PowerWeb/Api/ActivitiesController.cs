using Business.Repository;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace PowerWeb.Api
{
    public class ActivitiesController : ApiController
    {
        public class Params
        {
            public string filter { get; set; }
        }

        [HttpPost]
        public HttpResponseMessage PostActivities(Params @params)
        {
            var response = new HttpResponseMessage();

            if (@params.filter == null) @params.filter = string.Empty;

            RepoManager.CantRepo.Context.Configuration.LazyLoadingEnabled = false;
            RepoManager.CantRepo.Context.Configuration.ProxyCreationEnabled = false;

            var activities = RepoManager.CantRepo.DbSet.AsNoTracking()
                                                     .Where(c => (c.Codice_Cantiere.Contains(@params.filter)
                                                              || c.Descrizione_Can.Contains(@params.filter))
                                                              && c.Tipologia_Can == "ATT")
                                                     .Select(c => new Activity
                                                     {
                                                         Id = c.Cant_Id,
                                                         Codice = c.Codice_Cantiere,
                                                         Descrizione = c.Descrizione_Can
                                                     })
                                                     .ToList();

            response.Content = new StringContent(JsonConvert.SerializeObject(activities));

            return response;
        }

        public class Activity
        {
            public int Id { get; set; }
            public string Codice { get; set; }
            public string Descrizione { get; set; }
        }
    }
}
