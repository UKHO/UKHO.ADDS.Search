namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionFetchPayloadResult
    {
        private StudioIngestionFetchPayloadResult(
            StudioIngestionResultStatus status,
            StudioIngestionPayloadEnvelope? response,
            StudioIngestionErrorResponse? error)
        {
            Status = status;
            Response = response;
            Error = error;
        }

        public StudioIngestionResultStatus Status { get; }

        public StudioIngestionPayloadEnvelope? Response { get; }

        public StudioIngestionErrorResponse? Error { get; }

        public static StudioIngestionFetchPayloadResult Success(StudioIngestionPayloadEnvelope response)
        {
            ArgumentNullException.ThrowIfNull(response);

            return new StudioIngestionFetchPayloadResult(
                StudioIngestionResultStatus.Success,
                new StudioIngestionPayloadEnvelope
                {
                    Id = response.Id,
                    Payload = response.Payload.Clone()
                },
                null);
        }

        public static StudioIngestionFetchPayloadResult Invalid(string message)
        {
            return new StudioIngestionFetchPayloadResult(
                StudioIngestionResultStatus.InvalidRequest,
                null,
                new StudioIngestionErrorResponse
                {
                    Message = message
                });
        }

        public static StudioIngestionFetchPayloadResult NotFound(string message)
        {
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
