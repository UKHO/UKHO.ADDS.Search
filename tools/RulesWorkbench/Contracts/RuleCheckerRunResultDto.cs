namespace RulesWorkbench.Contracts
{
    public sealed record RuleCheckerRunResultDto
    {
        public bool IsSuccess { get; init; }

        public string? ErrorMessage { get; init; }

        public RuleCheckerReportDto? Report { get; init; }

        public static RuleCheckerRunResultDto Success(RuleCheckerReportDto report)
        {
            return new RuleCheckerRunResultDto
            {
                IsSuccess = true,
                Report = report,
            };
        }

        public static RuleCheckerRunResultDto Failure(string errorMessage)
        {
            return new RuleCheckerRunResultDto
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
            };
        }
    }
}
