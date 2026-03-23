namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionContextsResponse
    {
        public string Provider { get; init; } = string.Empty;

        public IReadOnlyList<StudioIngestionContextResponse> Contexts { get; init; } = [];
    }
}
