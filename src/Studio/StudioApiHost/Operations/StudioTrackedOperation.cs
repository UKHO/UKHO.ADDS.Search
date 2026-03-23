using System.Threading.Channels;
using UKHO.Search.Studio;

namespace StudioApiHost.Operations
{
    internal sealed class StudioTrackedOperation
    {
        private readonly List<Channel<StudioIngestionOperationEventResponse>> _subscribers = [];
        private readonly Lock _syncRoot = new();
        private StudioIngestionOperationStateResponse _state;

        public StudioTrackedOperation(string provider, string operationType, string? context)
        {
            var now = DateTimeOffset.UtcNow;

            OperationId = Guid.NewGuid();
            _state = new StudioIngestionOperationStateResponse
            {
                OperationId = OperationId.ToString("D"),
                Provider = provider,
                OperationType = operationType,
                Context = context,
                Status = StudioIngestionOperationStatuses.Queued,
                Message = "Queued operation.",
                StartedUtc = now
            };
        }

        public Guid OperationId { get; }

        public StudioIngestionOperationStateResponse CreateSnapshot()
        {
            lock (_syncRoot)
            {
                return CloneState(_state);
            }
        }

        public ChannelReader<StudioIngestionOperationEventResponse> Subscribe()
        {
            lock (_syncRoot)
            {
                var channel = Channel.CreateUnbounded<StudioIngestionOperationEventResponse>();

                if (IsTerminal(_state.Status))
                {
                    channel.Writer.TryComplete();
                    return channel.Reader;
                }

                _subscribers.Add(channel);
                return channel.Reader;
            }
        }

        public void MarkRunning(string message)
        {
            UpdateState(
                StudioIngestionOperationStatuses.Running,
                message,
                null,
                _state.Total,
                _state.FailureCode,
                null,
                "lifecycle");
        }

        public void ReportProgress(StudioIngestionOperationProgressUpdate update)
        {
            ArgumentNullException.ThrowIfNull(update);

            UpdateState(
                StudioIngestionOperationStatuses.Running,
                update.Message,
                update.Completed,
                update.Total,
                null,
                null,
                "progress");
        }

        public void MarkSucceeded(string message, int? completed, int? total)
        {
            UpdateState(
                StudioIngestionOperationStatuses.Succeeded,
                message,
                completed,
                total,
                null,
                DateTimeOffset.UtcNow,
                "progress");
        }

        public void MarkFailed(string message, string failureCode, int? completed, int? total)
        {
            UpdateState(
                StudioIngestionOperationStatuses.Failed,
                message,
                completed,
                total,
                failureCode,
                DateTimeOffset.UtcNow,
                "progress");
        }

        private void UpdateState(
            string status,
            string message,
            int? completed,
            int? total,
            string? failureCode,
            DateTimeOffset? completedUtc,
            string eventType)
        {
            lock (_syncRoot)
            {
                _state = new StudioIngestionOperationStateResponse
                {
                    OperationId = _state.OperationId,
                    Provider = _state.Provider,
                    OperationType = _state.OperationType,
                    Context = _state.Context,
                    Status = status,
                    Message = message,
                    Completed = completed ?? _state.Completed,
                    Total = total ?? _state.Total,
                    StartedUtc = _state.StartedUtc,
                    CompletedUtc = completedUtc ?? _state.CompletedUtc,
                    FailureCode = failureCode
                };

                var eventPayload = new StudioIngestionOperationEventResponse
                {
                    EventType = eventType,
                    OperationId = _state.OperationId,
                    Status = _state.Status,
                    Message = _state.Message,
                    Completed = _state.Completed,
                    Total = _state.Total,
                    TimestampUtc = completedUtc ?? DateTimeOffset.UtcNow,
                    FailureCode = _state.FailureCode
                };

                Publish(eventPayload);
            }
        }

        private void Publish(StudioIngestionOperationEventResponse eventPayload)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.Writer.TryWrite(eventPayload);
            }

            if (!IsTerminal(_state.Status))
            {
                return;
            }

            foreach (var subscriber in _subscribers)
            {
                subscriber.Writer.TryComplete();
            }

            _subscribers.Clear();
        }

        private static StudioIngestionOperationStateResponse CloneState(StudioIngestionOperationStateResponse state)
        {
            return new StudioIngestionOperationStateResponse
            {
                OperationId = state.OperationId,
                Provider = state.Provider,
                OperationType = state.OperationType,
                Context = state.Context,
                Status = state.Status,
                Message = state.Message,
                Completed = state.Completed,
                Total = state.Total,
                StartedUtc = state.StartedUtc,
                CompletedUtc = state.CompletedUtc,
                FailureCode = state.FailureCode
            };
        }

        private static bool IsTerminal(string status)
        {
            return string.Equals(status, StudioIngestionOperationStatuses.Succeeded, StringComparison.Ordinal)
                || string.Equals(status, StudioIngestionOperationStatuses.Failed, StringComparison.Ordinal);
        }
    }
}
