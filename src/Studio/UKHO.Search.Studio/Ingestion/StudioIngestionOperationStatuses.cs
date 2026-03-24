namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Defines the provider-neutral operation statuses surfaced by Studio ingestion tracking APIs.
    /// </summary>
    public static class StudioIngestionOperationStatuses
    {
        /// <summary>
        /// Indicates that the operation completed unsuccessfully.
        /// </summary>
        public const string Failed = "failed";

        /// <summary>
        /// Indicates that the operation has been accepted but has not started executing yet.
        /// </summary>
        public const string Queued = "queued";

        /// <summary>
        /// Indicates that the operation is currently running.
        /// </summary>
        public const string Running = "running";

        /// <summary>
        /// Indicates that the operation completed successfully.
        /// </summary>
        public const string Succeeded = "succeeded";
    }
}
