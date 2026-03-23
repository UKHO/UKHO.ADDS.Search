using System.Text.Json;

namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionPayloadEnvelope
    {
        public string Id { get; init; } = string.Empty;

        public JsonElement Payload { get; init; }
    }
}
