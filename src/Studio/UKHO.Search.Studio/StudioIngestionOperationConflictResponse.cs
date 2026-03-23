namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionOperationConflictResponse
    {
        public string Message { get; init; } = string.Empty;

        public string ActiveOperationId { get; init; } = string.Empty;

        public string ActiveProvider { get; init; } = string.Empty;

        public string ActiveOperationType { get; init; } = string.Empty;
    }
}
