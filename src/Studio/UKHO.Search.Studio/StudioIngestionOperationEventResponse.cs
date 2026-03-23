namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionOperationEventResponse
    {
        public string EventType { get; init; } = string.Empty;

        public string OperationId { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public int? Completed { get; init; }

        public int? Total { get; init; }

        public DateTimeOffset TimestampUtc { get; init; }

        public string? FailureCode { get; init; }
    }
}
