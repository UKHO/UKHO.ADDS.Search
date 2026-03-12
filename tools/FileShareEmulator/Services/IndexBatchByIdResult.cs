namespace FileShareEmulator.Services
{
    public sealed record IndexBatchByIdResult(bool Succeeded, string BatchId, string? FailureReason)
    {
        public static IndexBatchByIdResult Success(string batchId) => new(true, batchId, null);

        public static IndexBatchByIdResult Fail(string failureReason) => new(false, string.Empty, failureReason);
    }
}
