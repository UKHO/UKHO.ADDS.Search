namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionAcceptedOperationResponse
    {
        public string OperationId { get; init; } = string.Empty;

        public string Provider { get; init; } = string.Empty;

        public string OperationType { get; init; } = string.Empty;

        public string? Context { get; init; }

        public string Status { get; init; } = string.Empty;
    }
}
