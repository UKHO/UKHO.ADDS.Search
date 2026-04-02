namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents the current state snapshot for a tracked ingestion operation.
    /// </summary>
    public sealed class StudioIngestionOperationStateResponse
    {
        /// <summary>
        /// Gets the identifier assigned to the tracked operation.
        /// </summary>
        public string OperationId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the provider that owns the operation.
        /// </summary>
        public string Provider { get; init; } = string.Empty;

        /// <summary>
        /// Gets the provider-neutral operation type.
        /// </summary>
        public string OperationType { get; init; } = string.Empty;

        /// <summary>
        /// Gets the optional provider-neutral context for the operation.
        /// </summary>
        public string? Context { get; init; }

        /// <summary>
        /// Gets the current provider-neutral operation status.
        /// </summary>
        public string Status { get; init; } = string.Empty;

        /// <summary>
        /// Gets the latest user-facing message for the operation.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets the completed item count when the provider reports progress.
        /// </summary>
        public int? Completed { get; init; }

        /// <summary>
        /// Gets the total item count when the provider reports progress.
        /// </summary>
        public int? Total { get; init; }

        /// <summary>
        /// Gets the UTC timestamp when the operation was created.
        /// </summary>
        public DateTimeOffset StartedUtc { get; init; }

        /// <summary>
        /// Gets the UTC timestamp when the operation completed, when available.
        /// </summary>
        public DateTimeOffset? CompletedUtc { get; init; }

        /// <summary>
        /// Gets the provider-neutral failure code when the operation has failed.
        /// </summary>
        public string? FailureCode { get; init; }
    }
}
