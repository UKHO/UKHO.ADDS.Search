namespace RulesWorkbench.Contracts
{
    public sealed record RuleCheckerReportDto
    {
        public RuleCheckerBatchSummaryDto Batch { get; init; } = new();

        public RuleCheckerStatus Status { get; init; }

        public EvaluationPayloadDto Payload { get; init; } = EvaluationPayloadDto.CreateDefault();

        public string RawPayloadJson { get; init; } = string.Empty;

        public string FinalDocumentJson { get; init; } = string.Empty;

        public IReadOnlyList<string> MissingRequiredFields { get; init; } = Array.Empty<string>();

        public IReadOnlyList<RuleEvaluationMatchedRuleDto> MatchedRules { get; init; } = Array.Empty<RuleEvaluationMatchedRuleDto>();

        public IReadOnlyList<RuleCheckerCandidateRuleDto> CandidateRules { get; init; } = Array.Empty<RuleCheckerCandidateRuleDto>();

        public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> RuntimeWarnings { get; init; } = Array.Empty<string>();
    }
}
