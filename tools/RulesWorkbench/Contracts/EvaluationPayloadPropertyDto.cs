using System.Text.Json.Serialization;

namespace RulesWorkbench.Contracts
{
    public sealed record EvaluationPayloadPropertyDto
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Type")]
        public string Type { get; set; } = "String";

        [JsonPropertyName("Value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Value { get; set; }
    }
}
