namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionContextResponse
    {
        public string Value { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public bool IsDefault { get; init; }
    }
}
