namespace UKHO.Search.ProviderModel
{
    public sealed class ProviderCatalog : IProviderCatalog
    {
        private readonly IReadOnlyDictionary<string, ProviderDescriptor> _descriptorsByName;
        private readonly IReadOnlyList<ProviderDescriptor> _orderedDescriptors;

        public ProviderCatalog(IEnumerable<ProviderDescriptor> descriptors)
        {
            ArgumentNullException.ThrowIfNull(descriptors);

            var orderedDescriptors = descriptors.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            var descriptorsByName = new Dictionary<string, ProviderDescriptor>(StringComparer.OrdinalIgnoreCase);

            foreach (var descriptor in orderedDescriptors)
            {
                ArgumentNullException.ThrowIfNull(descriptor);

                if (!descriptorsByName.TryAdd(descriptor.Name, descriptor))
                {
                    throw new InvalidOperationException($"A provider with the name '{descriptor.Name}' is already registered.");
                }
            }

            _orderedDescriptors = orderedDescriptors;
            _descriptorsByName = descriptorsByName;
        }

        public IReadOnlyCollection<ProviderDescriptor> GetAllProviders()
        {
            return _orderedDescriptors;
        }

        public ProviderDescriptor GetProvider(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (!TryGetProvider(name, out var descriptor))
            {
                throw new KeyNotFoundException($"No provider metadata registered with name '{name}'.");
            }

            return descriptor;
        }

        public bool TryGetProvider(string name, out ProviderDescriptor? descriptor)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return _descriptorsByName.TryGetValue(name, out descriptor);
        }
    }
}
