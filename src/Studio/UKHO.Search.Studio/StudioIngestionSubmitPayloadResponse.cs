namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionSubmitPayloadResponse
    {
        public bool Accepted { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
