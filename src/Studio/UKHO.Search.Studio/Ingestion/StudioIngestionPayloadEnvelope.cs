using System.Text.Json;

namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Wraps a provider-defined payload in a provider-neutral envelope for Studio APIs.
    /// </summary>
    public sealed class StudioIngestionPayloadEnvelope
    {
        /// <summary>
        /// Gets the provider-defined payload identifier.
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Gets the opaque provider payload serialized as JSON.
        /// </summary>
        public JsonElement Payload { get; init; }
    }
}
