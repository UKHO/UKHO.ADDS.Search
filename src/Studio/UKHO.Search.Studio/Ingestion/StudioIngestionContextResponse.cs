namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents a single provider-neutral ingestion context exposed to Studio clients.
    /// </summary>
    public sealed class StudioIngestionContextResponse
    {
        /// <summary>
        /// Gets the stable provider-neutral value used to invoke context-scoped ingestion operations.
        /// </summary>
        public string Value { get; init; } = string.Empty;

        /// <summary>
        /// Gets the human-readable context name shown in Studio.
        /// </summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the provider considers this the default context.
        /// </summary>
        public bool IsDefault { get; init; }
    }
}
