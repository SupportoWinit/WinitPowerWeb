using Business.Repository;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

public class CentroDiCostoController : ApiController
{
    public class Params
    {
        public string filter { get; set; }
    }

    public CentroDiCostoController()
    {

    }

    [HttpPost]
    public HttpResponseMessage PostChildren(Params @params)
    {
        var response = new HttpResponseMessage();

        if (@params.filter == null) @params.filter = string.Empty;

        RepoManager.CantRepo.Context.Configuration.LazyLoadingEnabled = false;
        RepoManager.CantRepo.Context.Configuration.ProxyCreationEnabled = false;

        var children = RepoManager.CantRepo.DbSet.AsNoTracking()
                                                 .Where(c => c.Codice_Cantiere.Contains(@params.filter)
                                                          || c.Descrizione_Can.Contains(@params.filter)).Where(c=>c.DisAbilitazione_Can==false)
                                                 .Select(c => new Child
                                                 {
                                                     Id = c.Cant_Id,
                                                     Codice = c.Codice_Cantiere,
                                                     Descrizione = c.Descrizione_Can
                                                 })
                                                 .ToList();

        response.Content = new StringContent(JsonConvert.SerializeObject(children));

        return response;
    }

    [HttpGet]
    public HttpResponseMessage GetChildCentroDiCosto(int id)
    {
        var response = new HttpResponseMessage();

        RepoManager.CantRepo.Context.Configuration.LazyLoadingEnabled = false;
        RepoManager.CantRepo.Context.Configuration.ProxyCreationEnabled = false;

        var centriDiCosto = RepoManager.CentroDiCostoRepo.CantCentroDiCostoDbSet
                                                         .Where(c => c.Cant_Id == id)
                                                         .Select(c => new Cdc
                                                         {
                                                             Id = c.CentroDiCosto.CentroDiCosto_Id,
                                                             Codice = c.CentroDiCosto.Codice,
                                                             Descrizione = c.CentroDiCosto.Descrizione
                                                         })
                                                         .ToList();
        var centriDiCostoSenzaBambini = RepoManager.CentroDiCostoRepo.DbSet
                                                                     .Where(c => !c.Cant_CentroDiCosto.Any())
                                                                     .Select(c => new Cdc
                                                                     {
                                                                         Id = c.CentroDiCosto_Id,
                                                                         Codice = c.Codice,
                                                                         Descrizione = c.Descrizione
                                                                     })
                                                                     .ToList();

        var result = centriDiCosto.Concat(centriDiCostoSenzaBambini);

        response.Content = new StringContent(JsonConvert.SerializeObject(result));

        return response;
    }

    public class Child
    {
        public int Id { get; set; }
        public string Codice { get; set; }
        public string Descrizione { get; set; }

    }

    public class Cdc
    {
        public int Id { get; set; }
        public string Codice { get; set; }
        public string Descrizione { get; set; }
    }

}