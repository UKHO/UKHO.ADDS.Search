namespace FileShareEmulator.Services
{
    public sealed class QueueStatus
    {
        public required bool IsEmpty { get; init; }

        public int? ApproximateMessageCount { get; init; }
    }
}
