using Data;
using Domain;
using System.Data.Entity;

namespace Business.Repository.Custom
{
    public class CentroDiCostoRepository : GenericRepository<CentroDiCosto>, ICentroDiCostoRepository
    {
        private readonly PowerWebEntities _context;

        public CentroDiCostoRepository(PowerWebEntities context) : base(context)
        {
            _context = context;
        }

        public DbSet<Cant_CentroDiCosto> CantCentroDiCostoDbSet => _context.Cant_CentroDiCosto;
    }
}
