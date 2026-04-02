namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents the acknowledgement returned after a payload submit request is accepted.
    /// </summary>
    public sealed class StudioIngestionSubmitPayloadResponse
    {
        /// <summary>
        /// Gets a value indicating whether the payload was accepted for submission.
        /// </summary>
        public bool Accepted { get; init; }

        /// <summary>
        /// Gets the user-facing outcome message for the submission.
        /// </summary>
        public string Message { get; init; } = string.Empty;
    }
}
