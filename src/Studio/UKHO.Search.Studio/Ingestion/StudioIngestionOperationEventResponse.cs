namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents a single streamed event emitted for a tracked ingestion operation.
    /// </summary>
    public sealed class StudioIngestionOperationEventResponse
    {
        /// <summary>
        /// Gets the logical event category, such as lifecycle or progress.
        /// </summary>
        public string EventType { get; init; } = string.Empty;

        /// <summary>
        /// Gets the identifier of the operation that emitted the event.
        /// </summary>
        public string OperationId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the operation status at the time the event was emitted.
        /// </summary>
        public string Status { get; init; } = string.Empty;

        /// <summary>
        /// Gets the user-facing progress or lifecycle message.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets the completed item count reported by the event when available.
        /// </summary>
        public int? Completed { get; init; }

        /// <summary>
        /// Gets the total item count reported by the event when available.
        /// </summary>
        public int? Total { get; init; }

        /// <summary>
        /// Gets the UTC timestamp recorded for the event.
        /// </summary>
        public DateTimeOffset TimestampUtc { get; init; }

        /// <summary>
        /// Gets the provider-neutral failure code when the event represents a failed operation.
        /// </summary>
        public string? FailureCode { get; init; }
    }
}
