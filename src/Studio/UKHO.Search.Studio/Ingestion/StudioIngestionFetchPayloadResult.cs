namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents the result of fetching a provider payload for Studio ingestion APIs.
    /// </summary>
    public sealed class StudioIngestionFetchPayloadResult
    {
        private StudioIngestionFetchPayloadResult(
            StudioIngestionResultStatus status,
            StudioIngestionPayloadEnvelope? response,
            StudioIngestionErrorResponse? error)
        {
            // Capture the final result payload so callers can branch on the status without reinterpreting transport details.
            Status = status;
            Response = response;
            Error = error;
        }

        /// <summary>
        /// Gets the high-level outcome of the fetch request.
        /// </summary>
        public StudioIngestionResultStatus Status { get; }

        /// <summary>
        /// Gets the fetched payload envelope when the request succeeds.
        /// </summary>
        public StudioIngestionPayloadEnvelope? Response { get; }

        /// <summary>
        /// Gets the error payload when the request does not succeed.
        /// </summary>
        public StudioIngestionErrorResponse? Error { get; }

        /// <summary>
        /// Creates a successful fetch result.
        /// </summary>
        /// <param name="response">The provider payload envelope that was fetched.</param>
        /// <returns>A successful fetch result.</returns>
        public static StudioIngestionFetchPayloadResult Success(StudioIngestionPayloadEnvelope response)
        {
            // Validate the successful payload before cloning it into the result envelope.
            ArgumentNullException.ThrowIfNull(response);

            // Clone the JSON payload so callers cannot mutate the provider-owned payload instance after the result is created.
            return new StudioIngestionFetchPayloadResult(
                StudioIngestionResultStatus.Success,
                new StudioIngestionPayloadEnvelope
                {
                    Id = response.Id,
                    Payload = response.Payload.Clone()
                },
                null);
        }

        /// <summary>
        /// Creates an invalid-request fetch result.
        /// </summary>
        /// <param name="message">The user-facing validation message.</param>
        /// <returns>An invalid-request fetch result.</returns>
        public static StudioIngestionFetchPayloadResult Invalid(string message)
        {
            // Return a provider-neutral error payload that the API can forward directly to callers.
            return new StudioIngestionFetchPayloadResult(
                StudioIngestionResultStatus.InvalidRequest,
                null,
                new StudioIngestionErrorResponse
                {
                    Message = message
                });
        }

        /// <summary>
        /// Creates a not-found fetch result.
        /// </summary>
        /// <param name="message">The user-facing not-found message.</param>
        /// <returns>A not-found fetch result.</returns>
        public static StudioIngestionFetchPayloadResult NotFound(string message)
        {
            // Return a provider-neutral error payload that explains the missing provider item.
            return new StudioIngestionFetchPayloadResult(
                StudioIngestionResultStatus.NotFound,
                null,
                new StudioIngestionErrorResponse
                {
                    Message = message
                });
        }
    }
}
