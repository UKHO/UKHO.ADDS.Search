namespace UKHO.Search.Studio
{
    public sealed class StudioIngestionOperationExecutionResult
    {
        private StudioIngestionOperationExecutionResult(bool succeeded, string message, string? failureCode, int? completed, int? total)
        {
            Succeeded = succeeded;
            Message = message;
            FailureCode = failureCode;
            Completed = completed;
            Total = total;
        }

        public bool Succeeded { get; }

        public string Message { get; }

        public string? FailureCode { get; }

        public int? Completed { get; }

        public int? Total { get; }

        public static StudioIngestionOperationExecutionResult Success(string message, int? completed = null, int? total = null)
        {
            return new StudioIngestionOperationExecutionResult(true, message, null, completed, total);
        }

        public static StudioIngestionOperationExecutionResult Failed(string message, string failureCode, int? completed = null, int? total = null)
        {
            return new StudioIngestionOperationExecutionResult(false, message, failureCode, completed, total);
        }
    }
}
