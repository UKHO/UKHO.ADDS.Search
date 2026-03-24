namespace UKHO.Search.Studio.Providers
{
    /// <summary>
    /// Provides deterministic lookup over the Studio providers registered in the current container.
    /// </summary>
    public sealed class StudioProviderCatalog : IStudioProviderCatalog
    {
        private readonly IReadOnlyDictionary<string, IStudioProvider> _providersByName;
        private readonly IReadOnlyList<IStudioProvider> _orderedProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="StudioProviderCatalog"/> class.
        /// </summary>
        /// <param name="providers">The Studio providers that should be indexed by provider name.</param>
        public StudioProviderCatalog(IEnumerable<IStudioProvider> providers)
        {
            // Validate the incoming provider sequence before building deterministic lookup structures.
            ArgumentNullException.ThrowIfNull(providers);

            // Sort providers once so callers always see a predictable order regardless of registration order.
            var orderedProviders = providers.OrderBy(x => x.ProviderName, StringComparer.Ordinal).ToArray();
            var providersByName = new Dictionary<string, IStudioProvider>(StringComparer.OrdinalIgnoreCase);

            // Index each provider by name and reject duplicate registrations up front.
            foreach (var provider in orderedProviders)
            {
                ArgumentNullException.ThrowIfNull(provider);

                if (!providersByName.TryAdd(provider.ProviderName, provider))
                {
                    throw new InvalidOperationException($"A studio provider with the name '{provider.ProviderName}' is already registered.");
                }
            }

            // Store the final immutable views that the catalog exposes to callers.
            _orderedProviders = orderedProviders;
            _providersByName = providersByName;
        }

        /// <summary>
        /// Returns every registered Studio provider in deterministic name order.
        /// </summary>
        /// <returns>The registered Studio providers.</returns>
        public IReadOnlyCollection<IStudioProvider> GetAllProviders()
        {
            // Return the cached ordered view so repeated lookups do not need to re-sort providers.
            return _orderedProviders;
        }

        /// <summary>
        /// Gets a Studio provider by name.
        /// </summary>
        /// <param name="providerName">The provider name to resolve.</param>
        /// <returns>The matching Studio provider.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="providerName"/> is null, empty, or whitespace.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when no registered provider matches <paramref name="providerName"/>.</exception>
        public IStudioProvider GetProvider(string providerName)
        {
            // Validate the input before performing the case-insensitive lookup.
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

            // Reuse the shared lookup path so the success and failure rules stay consistent.
            if (!TryGetProvider(providerName, out var provider))
            {
                throw new KeyNotFoundException($"No studio provider registered with name '{providerName}'.");
            }

            // The null-forgiving operator is safe here because a successful lookup always assigns a provider instance.
            return provider!;
        }

        /// <summary>
        /// Attempts to resolve a Studio provider by name.
        /// </summary>
        /// <param name="providerName">The provider name to resolve.</param>
        /// <param name="provider">When this method returns <see langword="true"/>, contains the resolved provider; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the provider exists; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="providerName"/> is null, empty, or whitespace.</exception>
        public bool TryGetProvider(string providerName, out IStudioProvider? provider)
        {
            // Reject invalid names so callers do not accidentally treat empty provider identifiers as missing providers.
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

            // Perform a case-insensitive lookup against the precomputed dictionary.
            return _providersByName.TryGetValue(providerName, out provider);
        }
    }
}
