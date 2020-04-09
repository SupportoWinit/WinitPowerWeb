using Domain;
using System.Data.Entity;

namespace Business.Repository.Custom
{
    public interface ICentroDiCostoRepository : IRepository<CentroDiCosto>
    {
        DbSet<Cant_CentroDiCosto> CantCentroDiCostoDbSet { get; }
    }
}
