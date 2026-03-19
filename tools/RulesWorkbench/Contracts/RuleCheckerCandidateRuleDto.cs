namespace RulesWorkbench.Contracts
{
    public sealed record RuleCheckerCandidateRuleDto
    {
        public string RuleId { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string RuleJson { get; init; } = string.Empty;

        public bool IsMatched { get; init; }
    }
}
