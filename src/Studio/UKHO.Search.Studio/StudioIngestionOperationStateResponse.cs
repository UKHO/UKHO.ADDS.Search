namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionOperationStateResponse
    {
        public string OperationId { get; init; } = string.Empty;

        public string Provider { get; init; } = string.Empty;

        public string OperationType { get; init; } = string.Empty;

        public string? Context { get; init; }

        public string Status { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public int? Completed { get; init; }

        public int? Total { get; init; }

        public DateTimeOffset StartedUtc { get; init; }

        public DateTimeOffset? CompletedUtc { get; init; }

        public string? FailureCode { get; init; }
    }
}
