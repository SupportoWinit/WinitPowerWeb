namespace Business.Infrastructure
{
    /// <summary>
    /// Dependency resolver factory
    /// </summary>
    public interface IDependencyResolverFactory
    {
        /// <summary>
        /// Create dependency resolver
        /// </summary>
        /// <returns>Dependency resolver</returns>
        IDependencyResolver CreateInstance();
    }
}
