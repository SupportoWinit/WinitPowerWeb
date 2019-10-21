using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Practices.Unity;
using Domain;
using Business.Repository;
using System.Data.Entity.Core.EntityClient;
using Data;
using Business.Repository.Custom;
using System;
using Business.Synchronization.SynchronizatioManager.Implementations;
using Business.ExternalImports;
using Business.BusinessServices.UserService;
using Business.BusinessServices.UserService.Service;

namespace Business.Infrastructure
{
    public sealed class UnityDependencyResolver : IDependencyResolver
    {
        #region Fields

        private readonly IUnityContainer _container;

        #endregion

        #region Ctor

        public UnityDependencyResolver()
            : this(new UnityContainer())
        { }

        public UnityDependencyResolver(IUnityContainer container)
        {
            if (container == null)
                throw new ArgumentNullException("container");

            _container = container;
            //configure container
            ConfigureContainer(_container);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Configure root container.Register types and life time managers for unity builder process
        /// </summary>
        /// <param name="container">Container to configure</param>
        private void ConfigureContainer(IUnityContainer container)
        {
            //
            // GRUPPO REPOSITORY CUSTOM
            //
            container.RegisterType<ICant_VRepository, Cant_VRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ICol_VRepository, Col_VRepository>(new UnityPerExecutionContextLifetimeManager());  
            container.RegisterType<ICantRepository, CantRepository>(new UnityPerExecutionContextLifetimeManager());            
            container.RegisterType<ICant_NoteRepository, Cant_NoteRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ICant_VarRepository, Cant_VarRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ICliRepository, CliRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IColRepository, ColRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ICol_NoteRepository, Col_NoteRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IColCantOrarioRepository, ColCantOrarioRepository>(new UnityPerExecutionContextLifetimeManager());                       
            container.RegisterType<ICol_VarRepository, Col_VarRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IFilRepository, FilRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IFruRepository, FruRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IFru_CantRepository, Fru_CantRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ILingueRepository, LingueRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IMenu_TipoRepository, Menu_TipoRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IParamRepository, ParamRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IPruRepository, PruRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IPru_ColRepository, Pru_ColRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IRegRepository, RegRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IReg_StoredRepository, Reg_StoredRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IReg_VRepository, Reg_VRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IRespRepository, RespRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IResourcesRepository, ResourcesRepository>(new UnityPerExecutionContextLifetimeManager());            
            container.RegisterType<ITab_AutRepository, Tab_AutRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_Chk_ImpRepository, Tab_Chk_ImpRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_ComuniRepository, Tab_ComuniRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_DataGridRepository, Tab_DataGridRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_DecodRepository, Tab_DecodRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_DistRepository, Tab_DistRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_EditFormTemplateRepository, Tab_EditFormTemplateRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_FestiviRepository, Tab_FestiviRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_FunzRepository, Tab_FunzRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_GridLookupRepository, Tab_GridLookupRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_MessaggiRepository, Tab_MessaggiRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_OrariRepository, Tab_OrariRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_OrariTipoRepository, Tab_OrariTipoRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_ProvRepository, Tab_ProvRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IUtentiRepository, UtentiRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IUtenti_FilRepository, Utenti_FilRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IUtenti_RespRepository, Utenti_RespRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IVersioniRepository, VersioniRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IAut_StrRepository, Aut_StrRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITimesheetRepository, TimesheetRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IUtenti_HistoryRepository, Utenti_HistoryRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IDamageRepository, DamageRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_DamageRepository, Tab_DamageRepository>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<ITab_Excel_ModelRepository, Tab_Excel_ModelRepository>(new UnityPerExecutionContextLifetimeManager());

            //
            //
            // GRUPPO REPOSITORY GENERIC
            //
            container.RegisterType<IRepository<Cant_Fil_V>, GenericRepository<Cant_Fil_V>>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IRepository<Col_Monte_Minuti>, GenericRepository<Col_Monte_Minuti>>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IRepository<Col_Resp_V>, GenericRepository<Col_Resp_V>>(new UnityPerExecutionContextLifetimeManager());            
            container.RegisterType<IRepository<Menu>, GenericRepository<Menu>>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IRepository<MetaDescriptor>, GenericRepository<MetaDescriptor>>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IRepository<MetaFieldDescriptor>, GenericRepository<MetaFieldDescriptor>>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IRepository<PendingElab>, GenericRepository<PendingElab>>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IRepository<Tab_Report>, GenericRepository<Tab_Report>>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IRepository<Tab_Report_Group>, GenericRepository<Tab_Report_Group>>(new UnityPerExecutionContextLifetimeManager());
            container.RegisterType<IRepository<Tab_Report_Order>, GenericRepository<Tab_Report_Order>>(new UnityPerExecutionContextLifetimeManager());


            #region CONSTRUCTOR PARAMETERS

            //Ogni tipo registrato da qui in poi è un possibile parametro di un costruttore

            //Connection string
            if (PowerWebConfig.IsConnectionStringSet)
            {
                var ecsbuilder = new EntityConnectionStringBuilder
                {
                    Provider = "System.Data.SqlClient",
                    ProviderConnectionString = PowerWebConfig.ConnectionString,
                    Metadata = string.Format(@"res://*/{0}.csdl|res://*/{0}.ssdl|res://*/{0}.msl", "PowerWebModel")
                };

                InjectionConstructor connectionStringParam = new InjectionConstructor(ecsbuilder.ToString());
                //Registering object context
                container.RegisterType<PowerWebEntities>(new UnityPerExecutionContextLifetimeManager(), connectionStringParam);
            }

            container.RegisterType<SynchronizationManager<Cant>>(new UnityPerSessionContextLifeTimeManager());
            container.RegisterType<SynchronizationManager<Col>>(new UnityPerSessionContextLifeTimeManager());

            #endregion

            container.RegisterType<ExternalImportFactory>(new UnityPerSessionContextLifeTimeManager()); //La factory per l'import da esterno deve esser instanziata ad ogni session

            container.RegisterType<IUserService, UserService>(new UnityPerExecutionContextLifetimeManager());
            
        }

        #endregion

        #region Methods

        /// <summary>
        /// Register instance
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="instance">Instance</param>
        public void Register<T>(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            _container.RegisterInstance(instance);
        }

        /// <summary>
        /// Inject
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="existing">Type</param>
        public void Inject<T>(T existing)
        {
            if (existing == null)
                throw new System.ArgumentNullException("existing");

            _container.BuildUp(existing);
        }

        /// <summary>
        /// Resolve
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="type">Type</param>
        /// <returns>Result</returns>
        public T Resolve<T>(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return (T)_container.Resolve(type);
        }

        /// <summary>
        /// Resolve
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="type">Type</param>
        /// <param name="name">Name</param>
        /// <returns>Result</returns>
        public T Resolve<T>(System.Type type, string name)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (name == null)
                throw new ArgumentNullException("name");

            return (T)_container.Resolve(type, name);
        }

        /// <summary>
        /// Resolve
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Result</returns>
        public T Resolve<T>()
        {
            return _container.Resolve<T>();
        }

        /// <summary>
        /// Resolve
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="name">Name</param>
        /// <returns>Result</returns>
        public T Resolve<T>(string name)
        {
            if (String.IsNullOrEmpty(name))
                throw new System.ArgumentNullException("name");

            return _container.Resolve<T>(name);
        }

        /// <summary>
        /// Resolve all
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Result</returns>
        public IEnumerable<T> ResolveAll<T>()
        {
            IEnumerable<T> namedInstances = _container.ResolveAll<T>();
            T unnamedInstance = default(T);

            try
            {
                unnamedInstance = _container.Resolve<T>();
            }
            catch (ResolutionFailedException)
            {
                //When default instance is missing
            }

            if (Equals(unnamedInstance, default(T)))
            {
                return namedInstances;
            }

            return new ReadOnlyCollection<T>(new List<T>(namedInstances) { unnamedInstance });
        }

        #endregion
    }
}
