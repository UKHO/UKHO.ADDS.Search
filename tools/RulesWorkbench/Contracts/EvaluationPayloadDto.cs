using System.Text.Json.Serialization;

namespace RulesWorkbench.Contracts
{
    public sealed record EvaluationPayloadDto
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("Timestamp")]
        public DateTimeOffset? Timestamp { get; set; }

        [JsonPropertyName("SecurityTokens")]
        public List<string> SecurityTokens { get; set; } = new();

        [JsonPropertyName("Properties")]
        public List<EvaluationPayloadPropertyDto> Properties { get; set; } = new();

        [JsonPropertyName("Files")]
        public List<EvaluationPayloadFileDto> Files { get; set; } = new();

        public static EvaluationPayloadDto CreateDefault()
        {
            return new EvaluationPayloadDto
            {
                Id = "test-id",
                Timestamp = DateTimeOffset.UtcNow,
                SecurityTokens = new List<string> { "PUBLIC" },
                Properties = new List<EvaluationPayloadPropertyDto>(),
                Files = new List<EvaluationPayloadFileDto>(),
            };
        }
    }
}
