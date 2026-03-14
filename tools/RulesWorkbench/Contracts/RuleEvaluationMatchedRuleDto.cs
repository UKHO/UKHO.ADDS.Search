namespace RulesWorkbench.Contracts
{
    public sealed record RuleEvaluationMatchedRuleDto
    {
        public string RuleId { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string Summary { get; init; } = string.Empty;
    }
}
