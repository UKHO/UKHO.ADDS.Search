namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Defines the provider-neutral operation type identifiers used by Studio ingestion workflows.
    /// </summary>
    public static class StudioIngestionOperationTypes
    {
        /// <summary>
        /// Identifies a context-scoped index operation.
        /// </summary>
        public const string ContextIndex = "context-index";

        /// <summary>
        /// Identifies a provider-wide index-all operation.
        /// </summary>
        public const string IndexAll = "index-all";

        /// <summary>
        /// Identifies a reset-indexing-status operation.
        /// </summary>
        public const string ResetIndexingStatus = "reset-indexing-status";
    }
}
