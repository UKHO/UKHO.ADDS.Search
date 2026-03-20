namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public sealed record IngestionRulesApplyReport
    {
        public static IngestionRulesApplyReport Empty { get; } = new();

        public IReadOnlyList<IngestionRulesMatchedRule> MatchedRules { get; init; } = Array.Empty<IngestionRulesMatchedRule>();
    }
}
