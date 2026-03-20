namespace RulesWorkbench.Contracts
{
    public sealed record RuleCheckerBatchSummaryDto
    {
        public string BatchId { get; init; } = string.Empty;

        public DateTimeOffset? CreatedOn { get; init; }

        public string BusinessUnitName { get; init; } = string.Empty;
    }
}
