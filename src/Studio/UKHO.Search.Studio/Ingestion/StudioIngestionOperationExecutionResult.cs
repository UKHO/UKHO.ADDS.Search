namespace UKHO.Search.Studio.Ingestion
{
    /// <summary>
    /// Represents the terminal outcome returned by a long-running ingestion provider operation.
    /// </summary>
    public sealed class StudioIngestionOperationExecutionResult
    {
        private StudioIngestionOperationExecutionResult(bool succeeded, string message, string? failureCode, int? completed, int? total)
        {
            // Store the normalized execution outcome so the host can update tracked operation state consistently.
            Succeeded = succeeded;
            Message = message;
            FailureCode = failureCode;
            Completed = completed;
            Total = total;
        }

        /// <summary>
        /// Gets a value indicating whether the operation completed successfully.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Gets the user-facing completion message for the operation.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the provider-neutral failure code when the operation fails.
        /// </summary>
        public string? FailureCode { get; }

        /// <summary>
        /// Gets the completed item count when the provider reports one.
        /// </summary>
        public int? Completed { get; }

        /// <summary>
        /// Gets the total item count when the provider reports one.
        /// </summary>
        public int? Total { get; }

        /// <summary>
        /// Creates a successful operation result.
        /// </summary>
        /// <param name="message">The user-facing success message.</param>
        /// <param name="completed">The completed item count when available.</param>
        /// <param name="total">The total item count when available.</param>
        /// <returns>A successful operation result.</returns>
        public static StudioIngestionOperationExecutionResult Success(string message, int? completed = null, int? total = null)
        {
            // Normalize successful outcomes into a single factory so providers return a consistent shape.
            return new StudioIngestionOperationExecutionResult(true, message, null, completed, total);
        }

        /// <summary>
        /// Creates a failed operation result.
        /// </summary>
        /// <param name="message">The user-facing failure message.</param>
        /// <param name="failureCode">The provider-neutral failure code.</param>
        /// <param name="completed">The completed item count when available.</param>
        /// <param name="total">The total item count when available.</param>
        /// <returns>A failed operation result.</returns>
        public static StudioIngestionOperationExecutionResult Failed(string message, string failureCode, int? completed = null, int? total = null)
        {
            // Normalize failure outcomes into a single factory so the host can surface failure codes consistently.
            return new StudioIngestionOperationExecutionResult(false, message, failureCode, completed, total);
        }
    }
}
