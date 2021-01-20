using Business.MDBSchema;
using Common;
using Data;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Validation;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Transactions;

namespace Business.Repository
{
    /// <summary>A generic repository for working with data in the database</summary>
    /// <typeparam name="TEntity">A POCO that represents an Entity Framework entity</typeparam>
    public class GenericRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private const int CHUNK_SIZE = 5000;

        private DbContext _context;
        private PowerWebEntities _powerWebContext;
        private TransactionScope _currentTransaction;

        public DbContext Context
        {
            get { return _context; }
        }

        public virtual DbSet<TEntity> DbSet
        {
            get { return _context.Set<TEntity>(); }
        }

        public ILog Log
        {
            get { return LogManager.GetLogger(typeof(PowerWebEntities)); }
        }

        /// <summary>
        /// Recupera la funzione utilizzata per filtrare i dati del repository corrente.
        /// </summary>
        /// <value>
        /// La funzione utilizzata per filtrare i dati del repository corrente.
        /// </value>
        public virtual Expression<Func<TEntity, bool>> Filter
        {
            get
            {
                return en => true;
            }
        }

        #region Ctor

        public GenericRepository(PowerWebEntities context)
        {
            _context = _powerWebContext = context;
        }

        public void Refresh()
        {
            _context.Configuration.AutoDetectChangesEnabled = false;
            DbSet.Local.Clear();
            _context.Configuration.AutoDetectChangesEnabled = true;
        }

        #endregion


        /// <summary>
        /// Ecco una delle cappelle di Factory Mind (unitOfWork pattern????)
        /// </summary>
        /// <returns></returns>
        public virtual int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public void Attach(TEntity entity)
        {
            DbSet.Attach(entity);
        }

        public virtual TEntity Init()
        {
            return DbSet.Create();
        }
        

        public virtual void DetachAllEntities()
        {
            var changedEntriesCopy = _context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }

        #region UPDATE

        public virtual void Update(IEnumerable<TEntity> entities, bool saveChanges = false)
        {
            int updateCount = 0;
            bool hasToChunk = entities.Count() > CHUNK_SIZE;

            _context.Configuration.AutoDetectChangesEnabled = false;

            foreach (TEntity entity in entities)
            {
                updateCount++;

                Update(entity);

                if (saveChanges && hasToChunk && updateCount % CHUNK_SIZE == 0)
                {
                    _context.Configuration.AutoDetectChangesEnabled = true;
                    _context.SaveChanges();
                    _context.Configuration.AutoDetectChangesEnabled = false;
                    updateCount = 0;
                }
            }

            _context.Configuration.AutoDetectChangesEnabled = true;

            if (saveChanges)
                _context.SaveChanges();
        }

        public virtual void Update(TEntity entity, bool saveChanges = false)
        {
            if (_context.Entry<TEntity>(entity).State == EntityState.Detached)
            {
                DbSet.Attach(entity);
                _context.Entry<TEntity>(entity).State = EntityState.Modified;
            }

            if (saveChanges)
                SaveChanges();
        }

        #endregion

        #region ADD

        public virtual void Add(IEnumerable<TEntity> entities, bool saveChanges = false)
        {
            _context.Configuration.AutoDetectChangesEnabled = false;
            _context.Configuration.ValidateOnSaveEnabled = false;
            int addCount = 0;
            bool hasToChunk = entities.Count() > CHUNK_SIZE;

            foreach (TEntity entity in entities)
            {
                addCount++;
                Add(entity);

                if (saveChanges && hasToChunk && addCount % CHUNK_SIZE == 0)
                {

                    _context.Configuration.AutoDetectChangesEnabled = true;
                    _context.SaveChanges();
                    _context.Configuration.AutoDetectChangesEnabled = false;
                    addCount = 0;
                }
            }

            _context.Configuration.AutoDetectChangesEnabled = true;

            if (saveChanges)
                _context.SaveChanges();
        }

        public virtual void Add(TEntity entity, bool saveChanges = false)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");
            DbSet.Add(entity);
            if (saveChanges)
            {
                SaveChanges();

            }

        }

        #endregion

        #region DELETE
        public virtual void Delete(TEntity entity, bool saveChanges = false)
        {
            if (entity != null)
            {
                DbSet.Remove(entity);
                if (saveChanges)
                    SaveChanges();
            }
        }

        public virtual void Delete(IEnumerable<TEntity> entities, bool saveChanges = false)
        {
            _context.Configuration.AutoDetectChangesEnabled = false;

            int deleteCount = 0;
            bool hasToChunk = entities.Count() > CHUNK_SIZE;

            for (int i = 0; i < entities.Count(); i++)
            {
                deleteCount++;
                Delete(entities.ElementAt(i));

                if (saveChanges && hasToChunk && deleteCount % CHUNK_SIZE == 0)
                {
                    _context.Configuration.AutoDetectChangesEnabled = true;
                    _context.SaveChanges();
                    _context.Configuration.AutoDetectChangesEnabled = false;
                    deleteCount = 0;
                }
            }

            _context.Configuration.AutoDetectChangesEnabled = true;

            if (saveChanges)
                _context.SaveChanges();
        }

        public virtual void DeleteFromQuery(Expression<Func<TEntity, bool>> expression)
        {
            DbSet.Where(expression).DeleteFromQuery();
        }

        public virtual void UpdateFromQuery(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TEntity>> updateExpression)
        {
            DbSet.Where(whereExpression).UpdateFromQuery(updateExpression);
        }

        #endregion

        #region BULK OPERATIONS

        public virtual void BulkInsert(IEnumerable<TEntity> entities)
        {
            try
            {
                _context.BulkInsert(entities);


            }
            catch (DbEntityValidationException ex)
            {
                Log.Error(String.Format("Errore di validazione  durante la procedura BULKINSERT con exception {0}", ex.Message));
                foreach (var eve in ex.EntityValidationErrors)
                {
                    Log.Error(String.Format("Entity of type {0} in state {1} has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State));
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Log.Error(String.Format("- Property: {0}, Error: {1}",
                            ve.PropertyName, ve.ErrorMessage));
                    }
                }

                throw ex;
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Errore durante la procedura BULKINSERT con exception {0}", ex.Message));
                throw ex;
            }



        }

        public virtual void BulkUpdate(IEnumerable<TEntity> entities)
        {
            try
            {
                _context.BulkUpdate(entities, operation =>
                {
                    operation.Log = s => System.Diagnostics.Debug.WriteLine(s);
                });

            }
            catch (DbEntityValidationException ex)
            {
                Log.Error(String.Format("Errore di validazione  durante la procedura BULKUPDATE con exception {0}", ex.Message));
                foreach (var eve in ex.EntityValidationErrors)
                {
                    Log.Error(String.Format("Entity of type {0} in state {1} has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State));
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Log.Error(String.Format("- Property: {0}, Error: {1}",
                            ve.PropertyName, ve.ErrorMessage));
                    }
                }

                throw ex;
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Errore durante la procedura BULKUPDATE con exception {0}", ex.Message));

                throw ex;
            }
        }

        public virtual void BulkDelete(IEnumerable<TEntity> entities)
        {

            try
            {
                _context.BulkDelete(entities);
            }
            catch (DbEntityValidationException ex)
            {
                Log.Error(String.Format("Errore di validazione  durante la procedura BULKDELETE con exception {0}", ex.Message));
                foreach (var eve in ex.EntityValidationErrors)
                {
                    Log.Error(String.Format("Entity of type {0} in state {1} has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State));
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Log.Error(String.Format("- Property: {0}, Error: {1}",
                            ve.PropertyName, ve.ErrorMessage));
                    }
                }

                throw ex;
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Errore durante la procedura BULKDELETE con exception {0}", ex.Message));

                throw ex;
            }

        }

        public virtual void BulkSaveChanges(Action<Z.BulkOperations.BulkOperation> action)
        {
            Context.BulkSaveChanges(action);
        }

        #endregion

        public virtual TEntity Get(int id)
        {
            return DbSet.Find(id);
        }

        public virtual void SetEntityBeforeAddOrUpdate(TEntity entity) { }

        public virtual Dictionary<string, string> Check(TEntity entity, bool isNew = false, bool isResetSession = true)
        {
            return new Dictionary<string, string>();
        }

        public List<Dictionary<string, string>> Check(IEnumerable<TEntity> entities, bool isNew = false, bool isResetSession = true)
        {
            List<Dictionary<string, string>> items = new List<Dictionary<string, string>>();

            foreach (TEntity entity in entities)
            {
                var errDic = Check(entity, isNew, false);
                if (errDic.Keys.Count > 0)
                    items.Add(errDic);
            }

            return items;
        }

        public virtual Dictionary<string, string> CheckForImport(TEntity entity)
        {
            return new Dictionary<string, string>();
        }

        public List<Dictionary<string, string>> CheckForImport(IEnumerable<TEntity> entities)
        {
            List<Dictionary<string, string>> items = new List<Dictionary<string, string>>();

            foreach (TEntity entity in entities)
            {
                var errDic = CheckForImport(entity);
                if (errDic.Keys.Count > 0)
                    items.Add(errDic);
            }

            return items;
        }

        public virtual Dictionary<string, string> CheckBeforeDelete(TEntity entity)
        {
            return new Dictionary<string, string>();
        }

        public virtual List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false) { return null; }

        public void WriteCheckLog<T>(T entity, Dictionary<string, string> result, ILog log)
        {
            object oValue = null;
            foreach (KeyValuePair<string, string> keyValuePairString in result)
            {
                string tabName = entity.GetType().BaseType.Name;
                log.Warn(BusinessService.GetLocalizedString(
                      PowerWebResources.ERR_X0_PER_CAMPO_X1_PER_TABELLA_X2_PER_UTENTE_X3.ToString(),
                      keyValuePairString.Value, keyValuePairString.Key, tabName, PowerWebContext.Current.User.Codice_Utente));
            }

            if (Common.Properties.Settings.Default.IsCheckFailedLogActive && result.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                Type oType = entity.GetType();

                PropertyInfo[] oArrayPropertyInfo = oType.GetProperties();
                sb.Append("Tabella: ").Append(oType.BaseType.Name).Append(Environment.NewLine);
                foreach (PropertyInfo o in oArrayPropertyInfo)
                {
                    oValue = o.GetValue(entity, null) ?? (object)"";
                    sb.Append(o.Name).Append(": '").Append(oValue.ToString()).Append("'").Append(Environment.NewLine);
                }
                log.Warn(sb.ToString());
            }
        }



        public virtual IEnumerable<TEntity> GetAll(bool detached = false)
        {
            return detached ? DbSet.AsNoTracking().Where(Filter).ToList() : DbSet.Where(Filter).ToList();
        }

        public IQueryable<TEntity> GetAllQueryable(bool detached = false)
        {
            return detached ? DbSet.AsNoTracking().Where(Filter).AsQueryable() : DbSet.Where(Filter).AsQueryable();
        }

        public IQueryable<TEntity> GetAllQueryable(Expression<Func<TEntity, bool>> expression, bool detached = false)
        {
            return detached ? DbSet.AsNoTracking().Where(expression).AsQueryable() : DbSet.Where(expression).AsQueryable();
        }

        public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> expression, bool detached = false)
        {
            IQueryable<TEntity> source = GetAllQueryable(expression, detached);
            if (source != null)
                try
                {
                    return source.ToList().Where(Filter.Compile());
                }
                catch (Exception ex)
                { }
            return new List<TEntity>();
        }

        public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> include, bool detached = false)
        {
            var filteredList = detached ? DbSet.AsNoTracking().Where(expression).Include(include).ToList() : DbSet.Where(expression).Include(include).ToList();
            return filteredList.Where(Filter.Compile());
        }

        public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> expression, string[] propertiesToInclude, bool detached = false)
        {
            var query = (ObjectQuery<TEntity>)DbSet.Where(expression);

            if (detached)
                query.AsNoTracking();

            foreach (string property in propertiesToInclude)
                query.Include(property);

            return query.ToList().Where(Filter.Compile());
        }

        public TEntity First(bool detached = false)
        {
            IQueryable<TEntity> source = GetAllQueryable(detached);
            if (source != null)
                return source.First();
            return default(TEntity);
        }

        public TEntity First(Expression<Func<TEntity, bool>> expression, bool detached = false)
        {
            IQueryable<TEntity> source = GetAllQueryable(expression, detached);
            if (source != null)
                return source.First();
            return default(TEntity);
        }

        public TEntity FirstOrDefault(Expression<Func<TEntity, bool>> expression, bool detached = false)
        {
            IQueryable<TEntity> source = GetAllQueryable(expression, detached);
            if (source != null)
                return source.FirstOrDefault(expression);
            return default(TEntity);
        }

        public TEntity Single(Expression<Func<TEntity, bool>> expression, bool detached = false)
        {
            IQueryable<TEntity> source = GetAllQueryable(expression, detached);
            if (source != null)
                return source.Single();
            return default(TEntity);
        }

        public TEntity SingleOrDefault(Expression<Func<TEntity, bool>> expression, bool detached = false)
        {
            IQueryable<TEntity> source = GetAllQueryable(expression, detached);
            if (source != null)
                return source.SingleOrDefault();
            return default(TEntity);
        }

        public TValue Max<TValue>(Expression<Func<TEntity, TValue>> expression, bool detached = false)
        {
            return detached ? DbSet.AsNoTracking().Max(expression) : DbSet.Max(expression);
        }

        public TValue Min<TValue>(Expression<Func<TEntity, TValue>> expression, bool detached = false)
        {
            return detached ? DbSet.AsNoTracking().Min(expression) : DbSet.Min(expression);
        }

        //public void UndoChanges()
        //{
        //    foreach (TEntity entity in _dbSet)
        //        UndoChanges(entity);
        //}

        //public void UndoChanges(TEntity entity)
        //{
        //    _context.ObjectStateManager.ChangeObjectState(entity, EntityState.Unchanged);
        //}

        //public void UndoChanges(IEnumerable<TEntity> entities)
        //{
        //    foreach (TEntity entity in entities)
        //        _context.ObjectStateManager.ChangeObjectState(entity, EntityState.Unchanged);

        //}

        //#endregion

        #region Transaction Management

        public void BeginWork()
        {
            if (!IsInTransaction)
                _currentTransaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead, Timeout = new TimeSpan(0, 20, 0) });
            else
                throw new Exception("Nested Transaction is not allowed");
        }

        public void CommitWork()
        {
            if (IsInTransaction)
            {
                _currentTransaction.Complete();
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
            else
                throw new Exception("Current Transaction is null");
        }

        public void RollbackWork()
        {
            if (IsInTransaction)
            {
                //_currentTransaction.Complete();
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
            else
                throw new Exception("Current Transaction is null");
        }

        public bool IsInTransaction
        {
            get { return _currentTransaction != null; }
        }


        #endregion

        #region Events


        #endregion
    }
}
