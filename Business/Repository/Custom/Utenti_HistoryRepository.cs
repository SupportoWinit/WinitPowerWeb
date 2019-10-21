using Data;
using Domain;

namespace Business.Repository.Custom
{
    public class Utenti_HistoryRepository : GenericRepository<Utenti_History>, IUtenti_HistoryRepository
    {
        public Utenti_HistoryRepository(PowerWebEntities context)
            : base(context)
        {
        }
    }
}
