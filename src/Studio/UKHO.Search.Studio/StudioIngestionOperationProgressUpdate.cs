namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionOperationProgressUpdate
    {
        public string Message { get; init; } = string.Empty;

        public int? Completed { get; init; }

        public int? Total { get; init; }
    }
}
