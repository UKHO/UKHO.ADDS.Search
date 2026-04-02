namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents the operation metadata returned when a long-running Studio ingestion action is accepted.
    /// </summary>
    public sealed class StudioIngestionAcceptedOperationResponse
    {
        /// <summary>
        /// Gets the identifier assigned to the accepted operation.
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
        /// Gets the optional provider-neutral context supplied for the operation.
        /// </summary>
        public string? Context { get; init; }

        /// <summary>
        /// Gets the initial operation status returned to the caller.
        /// </summary>
        public string Status { get; init; } = string.Empty;
    }
}
