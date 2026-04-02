namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Defines the high-level outcomes returned by synchronous Studio ingestion API operations.
    /// </summary>
    public enum StudioIngestionResultStatus
    {
        /// <summary>
        /// Indicates that the request completed successfully.
        /// </summary>
        Success,

        /// <summary>
        /// Indicates that the request failed validation.
        /// </summary>
        InvalidRequest,

        /// <summary>
        /// Indicates that the requested resource could not be found.
        /// </summary>
        NotFound
    }
}
