using System.Text.Json.Nodes;

namespace UKHO.Search.Ingestion.Pipeline.Documents
{
    public sealed record CanonicalDocument
    {
        public string DocumentId { get; init; } = string.Empty;

        public string DocumentType { get; init; } = string.Empty;

        public JsonObject Source { get; init; } = new();

        public JsonObject Normalized { get; init; } = new();

        public JsonObject Descriptions { get; init; } = new();

        public JsonObject Search { get; init; } = new();

        public JsonObject Facets { get; init; } = new();

        public JsonObject Quality { get; init; } = new();

        public JsonObject Provenance { get; init; } = new();

        public static CanonicalDocument CreateMinimal(string documentId, string documentType)
        {
            return new CanonicalDocument
            {
                DocumentId = documentId,
                DocumentType = documentType,
                Source = new JsonObject(),
                Normalized = new JsonObject(),
                Descriptions = new JsonObject(),
                Search = new JsonObject(),
                Facets = new JsonObject(),
                Quality = new JsonObject(),
                Provenance = new JsonObject()
            };
        }
    }
}