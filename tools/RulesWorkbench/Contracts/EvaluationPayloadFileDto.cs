using System.Text.Json.Serialization;

namespace RulesWorkbench.Contracts
{
    public sealed record EvaluationPayloadFileDto
    {
        [JsonPropertyName("Filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("Size")]
        public long Size { get; set; }

        [JsonPropertyName("Timestamp")]
        public DateTimeOffset? Timestamp { get; set; }

        [JsonPropertyName("MimeType")]
        public string MimeType { get; set; } = string.Empty;
    }
}
