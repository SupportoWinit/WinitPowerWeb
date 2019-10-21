using Business.ClockAppManagerSynchronizationUtilities.SynchronizationProcessors;
using Business.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.IocFactory.ClockAppSynchronizationFactory
{
    public class SynchronizationCollector<T> where T : class
    {
        private IEnumerable<DbEntityEntry<T>> _addedEntities;
        private IEnumerable<Dictionary<string, object>> _modifiedEntities;
        private IEnumerable<DbEntityEntry<T>> _deletedEntities;

        private ISynchronizationProcessor<T> _synchronizator;

        private bool _synchronizationAllowed;

        public SynchronizationCollector()
        {
            _synchronizationAllowed = RepoManager.ParamRepo.ParametersRow.Sincronizzazione_ClockApp;

            if (_synchronizationAllowed)
                _synchronizator = ClockAppSynchronizationFactory.CreateInstance<T>();


        }

        public void Collect(DbChangeTracker changeTracker)
        {
            if (!_synchronizationAllowed)
                return;

            var trackedEntities = changeTracker.Entries<T>().ToList();

            _addedEntities = trackedEntities.Where(en => en.State == EntityState.Added).ToList();
            var attachedModifiedEntities = trackedEntities.Where(en => en.State == EntityState.Modified).ToList();

            _modifiedEntities = DetachModifiedValues(attachedModifiedEntities);

            _deletedEntities = trackedEntities.Where(en => en.State == EntityState.Deleted).ToList();

        }

        public void Synchronize()
        {
            if (!_synchronizationAllowed)
                return;

            _synchronizator.Synchronize(_addedEntities, _modifiedEntities, _deletedEntities);

            ClearEntities();
        }

        private void ClearEntities()
        {
            _addedEntities = null;
            _modifiedEntities = null;
            _deletedEntities = null;
        }

        private IEnumerable<Dictionary<string, object>> DetachModifiedValues(IEnumerable<DbEntityEntry<T>> entities)
        {
            List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();


            foreach (var ent in entities)
            {
                Dictionary<string, object> modProp = new Dictionary<string, object>();

                foreach (var propName in ent.CurrentValues.PropertyNames)
                {
                    var current = ent.CurrentValues[propName];
                    var original = ent.OriginalValues[propName];

                    if (current != original)
                    {
                        modProp.Add(propName, current);
                    }
                }

                items.Add(modProp);
            }

            return items;
        }

    }
}
