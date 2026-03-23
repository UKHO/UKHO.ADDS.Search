namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionSubmitPayloadResult
    {
        private StudioIngestionSubmitPayloadResult(
            StudioIngestionResultStatus status,
            StudioIngestionSubmitPayloadResponse? response,
            StudioIngestionErrorResponse? error)
        {
            Status = status;
            Response = response;
            Error = error;
        }

        public StudioIngestionResultStatus Status { get; }

        public StudioIngestionSubmitPayloadResponse? Response { get; }

        public StudioIngestionErrorResponse? Error { get; }

        public static StudioIngestionSubmitPayloadResult Success(string message)
        {
            return new StudioIngestionSubmitPayloadResult(
                StudioIngestionResultStatus.Success,
                new StudioIngestionSubmitPayloadResponse
                {
                    Accepted = true,
                    Message = message
                },
                null);
        }

        public static StudioIngestionSubmitPayloadResult Invalid(string message)
        {
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
