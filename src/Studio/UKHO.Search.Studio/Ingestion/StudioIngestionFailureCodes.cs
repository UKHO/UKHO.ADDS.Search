namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Defines provider-neutral failure codes surfaced for Studio ingestion operations.
    /// </summary>
    public static class StudioIngestionFailureCodes
    {
        /// <summary>
        /// Indicates that the operation failed while reading or updating persisted provider data.
        /// </summary>
        public const string DatabaseError = "database-error";

        /// <summary>
        /// Indicates that the provider failed to translate or process a provider-specific item.
        /// </summary>
        public const string ProviderError = "provider-error";

        /// <summary>
        /// Indicates that the operation failed while writing a payload to the ingestion queue.
        /// </summary>
        public const string QueueWriteFailed = "queue-write-failed";

        /// <summary>
        /// Indicates that the operation failed for an unexpected reason outside the expected failure categories.
        /// </summary>
        public const string UnexpectedError = "unexpected-error";

        /// <summary>
        /// Indicates that the supplied context does not map to a known provider context.
        /// </summary>
        public const string UnknownContext = "unknown-context";
    }
}
