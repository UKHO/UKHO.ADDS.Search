namespace RulesWorkbench.Contracts
{
    public sealed record BusinessUnitOptionDto
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
