using Business.Repository;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;

namespace Business
{
    public class DevExtremeLinqServerRepository<TEntity> where TEntity : class
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DevExtremeLinqServerRepository<TEntity>));



        private DbSet<TEntity> _DbSet;

        private DbContext _Context;

        public bool AllowAdding;

        public bool AllowUpdating;

        public bool AllowInserting;

        public bool AllowDeleting;



        public DevExtremeLinqServerRepository(IRepository<TEntity> repository)
        {
            _DbSet = repository.DbSet;

            _Context = repository.Context;

        }


        public int TotalCount(JArray whereArray)
        {
            return _DbSet.AsNoTracking().DxWhere(whereArray).Count();
        }

        public IEnumerable<dynamic> GetDistinctValues(string property, JArray whereArray)
        {
            return _DbSet.AsNoTracking().AsQueryable().DxWhere(whereArray).DxSelect(new JArray() { property }, true);
        }

        public IEnumerable<dynamic> Query(int skip, int take, JArray whereArray, JArray orderByArray, JArray selectArray)
        {
            IQueryable<TEntity> source = _DbSet.AsNoTracking().AsQueryable().DxWhere(whereArray).DxOrderBy(_Context, orderByArray);

            if (skip != -1 && take != -1)
            {
                source = source.Skip(skip).Take(take);
            }

            return source.DxSelect(selectArray);

        }

        public IEnumerable<dynamic> GetAll(JArray selectArray)
        {
            return _DbSet.AsNoTracking().DxSelect(selectArray);
        }

        public bool Insert(TEntity entity)
        {
            try
            {
                _DbSet.Add(entity);
                _Context.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);

                var fullErrorMessage = string.Join("; ", errorMessages);


                _log.Error(String.Format("Errori di validazione durante l'inserimento del tipo {0} con i seguenti errori : {1}", entity.GetType().Name, fullErrorMessage));

                return false;

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool Update(TEntity entity)
        {
            try
            {
                if (_Context.Entry(entity).State == EntityState.Detached)
                {
                    _DbSet.Attach(entity);
                    _Context.Entry(entity).State = EntityState.Modified;
                    _Context.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public bool Delete(TEntity entity)
        {
            try
            {
                _DbSet.Remove(entity);
                _Context.SaveChanges();
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }
    }
}
