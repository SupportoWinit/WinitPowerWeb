using System;
using System.Configuration;

namespace Business.Infrastructure
{
    /// <summary>
    /// Dependency resolver factory
    /// </summary>
    public class DependencyResolverFactory : IDependencyResolverFactory
    {
        /// <summary>
        /// Resolver type
        /// </summary>
        private readonly Type _resolverType;


        /// <summary>
        /// Ctor
        /// </summary>
        public DependencyResolverFactory()
            : this(ConfigurationManager.AppSettings["dependencyResolverTypeName"])
        { }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="resolverTypeName">Resolver type name</param>
        public DependencyResolverFactory(string resolverTypeName)
        {
            if (String.IsNullOrEmpty(resolverTypeName))
                throw new ArgumentNullException("resolverTypeName");

            _resolverType = Type.GetType(resolverTypeName, true, true);
        }

        /// <summary>
        /// Create dependency resolver
        /// </summary>
        /// <returns>Dependency resolver</returns>
        public IDependencyResolver CreateInstance()
        {
            return Activator.CreateInstance(_resolverType) as IDependencyResolver;
        }
    }
}
