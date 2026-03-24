namespace UKHO.Search.Studio.Providers
{
    /// <summary>
    /// Resolves the set of Studio providers registered in the current service collection.
    /// </summary>
    public interface IStudioProviderCatalog
    {
        /// <summary>
        /// Returns every registered Studio provider in deterministic display order.
        /// </summary>
        /// <returns>The registered Studio providers.</returns>
        IReadOnlyCollection<IStudioProvider> GetAllProviders();

        /// <summary>
        /// Gets a Studio provider by name.
        /// </summary>
        /// <param name="providerName">The provider name to resolve.</param>
        /// <returns>The matching Studio provider.</returns>
        IStudioProvider GetProvider(string providerName);

        /// <summary>
        /// Attempts to resolve a Studio provider by name.
        /// </summary>
        /// <param name="providerName">The provider name to resolve.</param>
        /// <param name="provider">When this method returns <see langword="true"/>, contains the resolved provider; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the provider exists; otherwise <see langword="false"/>.</returns>
        bool TryGetProvider(string providerName, out IStudioProvider? provider);
    }
}
