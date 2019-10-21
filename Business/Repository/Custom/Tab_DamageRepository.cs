using Data;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Business.ClockAppManagerSynchronizationUtilities;
using Z.BulkOperations;
using System.Data.Entity.Infrastructure;
using Business.IocFactory.ClockAppSynchronizationFactory;

namespace Business.Repository.Custom
{
    public class Tab_DamageRepository : GenericRepository<Tab_Damage>, ITab_DamageRepository
    {

        private static readonly ILog _log = LogManager.GetLogger(typeof(Tab_DamageRepository));


        public Tab_DamageRepository(PowerWebEntities context)
            : base(context)
        {
        }

        public override int SaveChanges()
        {
            int result = 0;

            try
            {
                result = base.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                _log.ErrorFormat("Errore durante il salvaggio con exception {0}", ex.Message);

            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la procedura di sincronizzazione con exception {0}", ex.Message);
            }

            return result;
        }

        public override void BulkSaveChanges(Action<BulkOperation> action)
        {

            base.BulkSaveChanges(action);

        }


    }
}
