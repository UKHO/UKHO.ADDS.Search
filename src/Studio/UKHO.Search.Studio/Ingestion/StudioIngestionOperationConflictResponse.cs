namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents the conflict details returned when another ingestion operation is already active.
    /// </summary>
    public sealed class StudioIngestionOperationConflictResponse
    {
        /// <summary>
        /// Gets the message describing the active-operation conflict.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets the identifier of the operation that is already active.
        /// </summary>
        public string ActiveOperationId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the provider that owns the active operation.
        /// </summary>
        public string ActiveProvider { get; init; } = string.Empty;

        /// <summary>
        /// Gets the provider-neutral operation type that is currently active.
        /// </summary>
        public string ActiveOperationType { get; init; } = string.Empty;
    }
}
