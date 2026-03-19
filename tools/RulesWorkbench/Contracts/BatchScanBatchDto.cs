namespace RulesWorkbench.Contracts
{
    public sealed record BatchScanBatchDto
    {
        public Guid BatchId { get; init; }

        public DateTimeOffset CreatedOn { get; init; }
    }
}
