namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents a provider-neutral error payload returned from Studio ingestion APIs.
    /// </summary>
    public sealed class StudioIngestionErrorResponse
    {
        /// <summary>
        /// Gets the user-facing error message.
        /// </summary>
        public string Message { get; init; } = string.Empty;
    }
}
