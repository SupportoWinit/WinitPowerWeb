
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Business.Synchronization.Collectors
{
    /// <summary>
    /// Questa classe fornisce il supporto necessario alla raccolta dati tramite il changetracker 
    /// di entityframework. Distingue le entità in cancellate, modificate, aggiunte.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Business.Synchronization.ICollector{T}" />
    public class DbChangeTrackerCollector<T> : ICollector<T> where T : class
    {
        protected IEnumerable<T> addedEntities;
        protected IEnumerable<T> modifiedEntities;
        protected IEnumerable<T> deletedEntities;

        public DbChangeTracker changeTracker;

        public DbChangeTrackerCollector(DbChangeTracker changeTracker)
        {
            this.changeTracker = changeTracker;
        }

        /// <summary>
        /// Raccoglie le entità tracciate.
        /// </summary>
        public void Collect()
        {
            var trackedEntities = changeTracker.Entries<T>().ToList();

            addedEntities = trackedEntities.Where(en => en.State == EntityState.Added).Select(en => en.Entity).ToList();

            modifiedEntities = trackedEntities.Where(en => en.State == EntityState.Unchanged).Select(en => en.Entity).ToList();

            deletedEntities = trackedEntities.Where(en => en.State == EntityState.Deleted).Select(en => en.Entity).ToList();
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

        public IEnumerable<T> GetAddedEntities()
        {
            return addedEntities;
        }

        public IEnumerable<T> GetModifiedEntities()
        {
            return modifiedEntities;
        }

        public IEnumerable<T> GetDeletedEntities()
        {
            return deletedEntities;
        }
    }
}

