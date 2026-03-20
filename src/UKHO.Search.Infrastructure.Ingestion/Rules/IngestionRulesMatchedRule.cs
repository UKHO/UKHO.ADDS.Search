namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public sealed record IngestionRulesMatchedRule
    {
        public string RuleId { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string Summary { get; init; } = string.Empty;
    }
}
