namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents the result of submitting a provider payload for ingestion.
    /// </summary>
    public sealed class StudioIngestionSubmitPayloadResult
    {
        private StudioIngestionSubmitPayloadResult(
            StudioIngestionResultStatus status,
            StudioIngestionSubmitPayloadResponse? response,
            StudioIngestionErrorResponse? error)
        {
            // Capture the final result payload so the host can translate the result into an HTTP response consistently.
            Status = status;
            Response = response;
            Error = error;
        }

        /// <summary>
        /// Gets the high-level outcome of the submit request.
        /// </summary>
        public StudioIngestionResultStatus Status { get; }

        /// <summary>
        /// Gets the submit acknowledgement when the request succeeds.
        /// </summary>
        public StudioIngestionSubmitPayloadResponse? Response { get; }

        /// <summary>
        /// Gets the error payload when the request does not succeed.
        /// </summary>
        public StudioIngestionErrorResponse? Error { get; }

        /// <summary>
        /// Creates a successful submit result.
        /// </summary>
        /// <param name="message">The user-facing success message.</param>
        /// <returns>A successful submit result.</returns>
        public static StudioIngestionSubmitPayloadResult Success(string message)
        {
            // Return the provider-neutral acknowledgement payload that the API can forward directly to callers.
            return new StudioIngestionSubmitPayloadResult(
                StudioIngestionResultStatus.Success,
                new StudioIngestionSubmitPayloadResponse
                {
                    Accepted = true,
                    Message = message
                },
                null);
        }

        /// <summary>
        /// Creates an invalid-request submit result.
        /// </summary>
        /// <param name="message">The user-facing validation message.</param>
        /// <returns>An invalid-request submit result.</returns>
        public static StudioIngestionSubmitPayloadResult Invalid(string message)
        {
            // Return a provider-neutral validation error that the API can send back without extra translation.
            return new StudioIngestionSubmitPayloadResult(
                StudioIngestionResultStatus.InvalidRequest,
                null,
                new StudioIngestionErrorResponse
                {
                    Message = message
                });
        }
    }
}
