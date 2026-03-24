namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents the set of contexts returned for a single ingestion provider.
    /// </summary>
    public sealed class StudioIngestionContextsResponse
    {
        /// <summary>
        /// Gets the provider name that produced the context list.
        /// </summary>
        public string Provider { get; init; } = string.Empty;

        /// <summary>
        /// Gets the provider-neutral contexts that Studio can present to callers.
        /// </summary>
        public IReadOnlyList<StudioIngestionContextResponse> Contexts { get; init; } = [];
    }
}
