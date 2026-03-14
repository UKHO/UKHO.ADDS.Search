namespace RulesWorkbench.Contracts
{
    public sealed record RuleEvaluationReportDto
    {
        public string ProviderName { get; init; } = string.Empty;

        public List<RuleEvaluationMatchedRuleDto> MatchedRules { get; init; } = new();

        public string FinalDocumentJson { get; init; } = string.Empty;

        public List<string> ValidationErrors { get; init; } = new();

        public List<string> RuntimeWarnings { get; init; } = new();
    }
}
