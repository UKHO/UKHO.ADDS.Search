namespace RulesWorkbench.Contracts
{
    public sealed record BatchScanResultDto
    {
        public bool IsSuccess { get; init; }

        public string? ErrorMessage { get; init; }

        public IReadOnlyList<BatchScanBatchDto> Batches { get; init; } = Array.Empty<BatchScanBatchDto>();

        public static BatchScanResultDto Success(IReadOnlyList<BatchScanBatchDto> batches)
        {
            return new BatchScanResultDto
            {
                IsSuccess = true,
                Batches = batches,
            };
        }

        public static BatchScanResultDto Failure(string errorMessage)
        {
            return new BatchScanResultDto
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
            };
        }
    }
}
