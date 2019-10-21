using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Data.Entity.Core.Objects;
using log4net;
using System.Linq;
using Business.MDBSchema;
using Domain;
using Data;
using System.Data.Entity;
using Z.BulkOperations;

namespace Business.Repository
{
    public interface IRepository<TEntity> where TEntity : class
    {
        ILog Log { get; }

        DbContext Context { get; }

        DbSet<TEntity> DbSet { get; }

        Expression<Func<TEntity, bool>> Filter { get; }

        TEntity Init();

        /// <summary>
        /// Adds the specified entities.
        /// </summary>
        /// <param name="entities">The entities.</param>
        /// <param name="saveChanges">if set to <c>true</c> [save changes].</param>
        void Add(IEnumerable<TEntity> entities, bool saveChanges = false);

        /// <summary>
        /// Adds the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="saveChanges">if set to <c>true</c> [save changes].</param>
        void Add(TEntity entity, bool saveChanges = false);

        /// <summary>
        /// Checks the specified entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Dictionary<string, string> Check(TEntity entity, bool isNew = false, bool isResetSession = true);


        /// <summary>
        /// Metodo da eseguire quando si modifica un record perché alcuni campi vanno sempre modificati via codice.
        /// <param name="entity"></param>
        /// </summary> 
        void SetEntityBeforeAddOrUpdate(TEntity entity);


        /// <summary>
        /// Esegue l'importazione delle Tabelle dal vecchio db Access al nuovo db Sql Server.
        /// </summary>
        List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false);

        /// <summary>
        /// Esegue i controlli di validità del record corrente. Da eseguire in caso di modifica o aggiunta di un record.
        /// /// <param name="entity"></param>
        /// </summary>
        Dictionary<string, string> CheckForImport(TEntity entity);

        Dictionary<string, string> CheckBeforeDelete(TEntity entity);

        /// <summary>
        /// Empty the current Local of the DBSet
        /// </summary>
        void Refresh();

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="saveChanges">if set to <c>true</c> [save changes].</param>
        void Delete(TEntity entity, bool saveChanges = false);

        void Delete(IEnumerable<TEntity> entities, bool saveChanges = false);

        /// <summary>
        /// Attaches the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        void Attach(TEntity entity);

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="saveChanges">if set to <c>true</c> [save changes].</param>
        void Update(TEntity entity, bool saveChanges = false);

        void Update(IEnumerable<TEntity> entities, bool saveChanges = false);

        /// <summary>
        /// Gets all.
        /// </summary>
        /// <returns></returns>
        IEnumerable<TEntity> GetAll(bool detached = false);

        IQueryable<TEntity> GetAllQueryable(bool detached = false);

        IQueryable<TEntity> GetAllQueryable(Expression<Func<TEntity, bool>> expression, bool detached = false);

        /// <summary>
        /// Gets all by an expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> expression, bool detached = false);

        /// <summary>
        /// Gets all by an expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="include">A list of navigation properties to include for eager loading </param>
        /// <returns></returns>
        IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> include, bool detached = false);

        /// <summary>
        /// Finds the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="propertiesToInclude">The properties to include.</param>
        /// <returns></returns>
        IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> expression, string[] propertiesToInclude, bool detached = false);

        /// <summary>
        /// Gets the single by.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        TEntity FirstOrDefault(Expression<Func<TEntity, bool>> expression, bool detached = false);

        /// <summary>
        /// Firsts the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        TEntity First(bool detached = false);

        /// <summary>
        /// Firsts the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        TEntity First(Expression<Func<TEntity, bool>> expression, bool detached = false);

        /// <summary>
        /// Singles the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        TEntity Single(Expression<Func<TEntity, bool>> expression, bool detached = false);

        /// <summary>
        /// Singles the or default.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        TEntity SingleOrDefault(Expression<Func<TEntity, bool>> expression, bool detached = false);

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <returns></returns>
        int SaveChanges();

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        void BeginWork();

        /// <summary>
        /// Commits a transaction.
        /// </summary>
        void CommitWork();

        /// <summary>
        /// Rollbacks a transaction.
        /// </summary>
        void RollbackWork();

        /// <summary>
        /// Gets a value indicating whether this instance is in transaction.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is in transaction; otherwise, <c>false</c>.
        /// </value>
        bool IsInTransaction { get; }

        /// <summary>
        /// Maxes the specified expression.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        TValue Max<TValue>(Expression<Func<TEntity, TValue>> expression, bool detached = false);

        /// <summary>
        /// Mins the specified expression.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        TValue Min<TValue>(Expression<Func<TEntity, TValue>> expression, bool detached = false);

        //void UndoChanges();

        //void UndoChanges(TEntity entity);

        //void UndoChanges(IEnumerable<TEntity> entities);

        void DeleteFromQuery(Expression<Func<TEntity, bool>> expression);

        void UpdateFromQuery(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TEntity>> updateExpression);

        void BulkUpdate(IEnumerable<TEntity> entities);

        void BulkInsert(IEnumerable<TEntity> entities);

        void BulkDelete(IEnumerable<TEntity> entities);

        void BulkSaveChanges(Action<BulkOperation> action);
    }

    public enum LoadingMode
    {
        LazyLoading,
        EagerLoading
    }
}