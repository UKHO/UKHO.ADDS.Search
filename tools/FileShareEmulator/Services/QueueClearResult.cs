using System.Diagnostics;

namespace FileShareEmulator.Services
{
    public sealed class QueueClearResult
    {
        public required bool Succeeded { get; init; }

        public int? ItemsRemoved { get; init; }

        public required int Attempts { get; init; }

        public required TimeSpan Duration { get; init; }

        public string? FailureReason { get; init; }

        public Exception? Exception { get; init; }

        public static QueueClearResult Success(int? itemsRemoved, int attempts, Stopwatch stopwatch)
        {
            return new QueueClearResult
            {
                Succeeded = true,
                ItemsRemoved = itemsRemoved,
                Attempts = attempts,
                Duration = stopwatch.Elapsed
            };
        }

        public static QueueClearResult Fail(string failureReason, int? itemsRemoved, int attempts, Stopwatch stopwatch, Exception? exception = null)
        {
            return new QueueClearResult
            {
                Succeeded = false,
                FailureReason = failureReason,
                ItemsRemoved = itemsRemoved,
                Attempts = attempts,
                Duration = stopwatch.Elapsed,
                Exception = exception
            };
        }
    }
}
