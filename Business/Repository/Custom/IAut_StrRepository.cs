using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain;

namespace Business.Repository.Custom
{
    /// <summary>
    /// Interfaccia utilizzaa per definire il repository custom dell'autorizzazione straordinari
    /// </summary>
    public interface IAut_StrRepository : IRepository<Aut_Str>
    {

        /// <summary>
        /// Determina se lo specifico collaboratore nella specifica data risulta autorizzato agli straordinari
        /// e per quante ore.
        /// </summary>
        /// <param name="colId">L'identificativo del collaboratore di cui verificare l'autorizzazione.</param>
        /// <param name="date">La data in cui verificare l'autorizzazione.</param>
        /// <returns>Il numero di ore in cui risulta autorizzato il collaboratore; 0 se non autorizzato.</returns>
        int AutStrColAuthorization(int colId, DateTime date);
        
    }
}
