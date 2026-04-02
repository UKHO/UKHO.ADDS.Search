namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents a provider-supplied progress update for a long-running ingestion operation.
    /// </summary>
    public sealed class StudioIngestionOperationProgressUpdate
    {
        /// <summary>
        /// Gets the user-facing progress message.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets the completed item count when the provider can supply one.
        /// </summary>
        public int? Completed { get; init; }

        /// <summary>
        /// Gets the total item count when the provider can supply one.
        /// </summary>
        public int? Total { get; init; }
    }
}
