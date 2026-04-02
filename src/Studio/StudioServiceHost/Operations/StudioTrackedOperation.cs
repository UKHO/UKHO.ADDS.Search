using System.Threading.Channels;
using UKHO.Search.Studio.Ingestion;

namespace StudioServiceHost.Operations
{
    /// <summary>
    /// Represents a single tracked Studio ingestion operation together with its live subscribers.
    /// </summary>
    internal sealed class StudioTrackedOperation
    {
        private readonly List<Channel<StudioIngestionOperationEventResponse>> _subscribers = [];
        private readonly Lock _syncRoot = new();
        private StudioIngestionOperationStateResponse _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="StudioTrackedOperation"/> class.
        /// </summary>
        /// <param name="provider">The provider that owns the tracked operation.</param>
        /// <param name="operationType">The provider-neutral operation type being tracked.</param>
        /// <param name="context">The optional provider-neutral context associated with the operation.</param>
        public StudioTrackedOperation(string provider, string operationType, string? context)
        {
            // Capture the creation timestamp once so queued and later terminal snapshots share the same start time.
            var now = DateTimeOffset.UtcNow;

            // Create the stable operation identifier before seeding the initial queued-state snapshot.
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

        /// <summary>
        /// Gets the stable identifier assigned to the tracked operation.
        /// </summary>
        public Guid OperationId { get; }

        /// <summary>
        /// Creates a snapshot of the current tracked operation state.
        /// </summary>
        /// <returns>A cloned snapshot of the current operation state.</returns>
        public StudioIngestionOperationStateResponse CreateSnapshot()
        {
            lock (_syncRoot)
            {
                // Return a cloned snapshot so callers cannot mutate the retained in-memory state instance.
                return CloneState(_state);
            }
        }

        /// <summary>
        /// Subscribes to live events for this tracked operation.
        /// </summary>
        /// <returns>A channel reader that yields future operation events for this tracked operation.</returns>
        public ChannelReader<StudioIngestionOperationEventResponse> Subscribe()
        {
            lock (_syncRoot)
            {
                // Allocate a dedicated channel for the new subscriber so event delivery remains isolated per client.
                var channel = Channel.CreateUnbounded<StudioIngestionOperationEventResponse>();

                // Close the channel immediately when the operation is already terminal so late subscribers do not hang.
                if (IsTerminal(_state.Status))
                {
                    channel.Writer.TryComplete();
                    return channel.Reader;
                }

                // Retain the subscriber so future progress and lifecycle updates can be broadcast to it.
                _subscribers.Add(channel);
                return channel.Reader;
            }
        }

        /// <summary>
        /// Marks the tracked operation as running.
        /// </summary>
        /// <param name="message">The user-facing message describing the running state.</param>
        public void MarkRunning(string message)
        {
            // Record the transition from queued to running without changing the tracked totals yet.
            UpdateState(
                StudioIngestionOperationStatuses.Running,
                message,
                null,
                _state.Total,
                _state.FailureCode,
                null,
                "lifecycle");
        }

        /// <summary>
        /// Records a provider progress update for the tracked operation.
        /// </summary>
        /// <param name="update">The provider progress update to apply.</param>
        public void ReportProgress(StudioIngestionOperationProgressUpdate update)
        {
            // Guard the provider callback input before applying it to the tracked state.
            ArgumentNullException.ThrowIfNull(update);

            // Fold the provider's latest message and counts into the running-state snapshot.
            UpdateState(
                StudioIngestionOperationStatuses.Running,
                update.Message,
                update.Completed,
                update.Total,
                null,
                null,
                "progress");
        }

        /// <summary>
        /// Marks the tracked operation as successfully completed.
        /// </summary>
        /// <param name="message">The user-facing completion message.</param>
        /// <param name="completed">The completed item count when available.</param>
        /// <param name="total">The total item count when available.</param>
        public void MarkSucceeded(string message, int? completed, int? total)
        {
            // Record terminal success details and completion timestamp.
            UpdateState(
                StudioIngestionOperationStatuses.Succeeded,
                message,
                completed,
                total,
                null,
                DateTimeOffset.UtcNow,
                "progress");
        }

        /// <summary>
        /// Marks the tracked operation as failed.
        /// </summary>
        /// <param name="message">The user-facing failure message.</param>
        /// <param name="failureCode">The provider-neutral failure code associated with the failure.</param>
        /// <param name="completed">The completed item count when available.</param>
        /// <param name="total">The total item count when available.</param>
        public void MarkFailed(string message, string failureCode, int? completed, int? total)
        {
            // Record terminal failure details and completion timestamp.
            UpdateState(
                StudioIngestionOperationStatuses.Failed,
                message,
                completed,
                total,
                failureCode,
                DateTimeOffset.UtcNow,
                "progress");
        }

        /// <summary>
        /// Replaces the retained operation snapshot and broadcasts the corresponding event payload.
        /// </summary>
        /// <param name="status">The new operation status.</param>
        /// <param name="message">The user-facing message that describes the new state.</param>
        /// <param name="completed">The completed item count, when known.</param>
        /// <param name="total">The total item count, when known.</param>
        /// <param name="failureCode">The provider-neutral failure code, when the operation failed.</param>
        /// <param name="completedUtc">The completion timestamp, when the operation reached a terminal state.</param>
        /// <param name="eventType">The event type emitted to live subscribers.</param>
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
                // Ignore late lifecycle or progress callbacks once the operation has already reached a terminal state.
                if (IsTerminal(_state.Status))
                {
                    return;
                }

                // Replace the retained snapshot atomically so readers always observe a coherent operation state.
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

                // Build the streamed event payload from the same updated snapshot that polling clients will observe.
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

                // Broadcast the new event to subscribers and close subscriptions when the operation reaches a terminal state.
                Publish(eventPayload);
            }
        }

        /// <summary>
        /// Publishes the latest operation event to all current subscribers.
        /// </summary>
        /// <param name="eventPayload">The operation event payload to broadcast.</param>
        private void Publish(StudioIngestionOperationEventResponse eventPayload)
        {
            // Attempt to push the event to every current subscriber.
            foreach (var subscriber in _subscribers)
            {
                subscriber.Writer.TryWrite(eventPayload);
            }

            // Leave subscriptions open for non-terminal states so further updates can be delivered.
            if (!IsTerminal(_state.Status))
            {
                return;
            }

            // Close and clear all subscriber channels once the operation reaches a terminal state.
            foreach (var subscriber in _subscribers)
            {
                subscriber.Writer.TryComplete();
            }

            _subscribers.Clear();
        }

        /// <summary>
        /// Clones the retained operation snapshot before it leaves the tracked operation.
        /// </summary>
        /// <param name="state">The retained operation snapshot to clone.</param>
        /// <returns>A cloned operation snapshot.</returns>
        private static StudioIngestionOperationStateResponse CloneState(StudioIngestionOperationStateResponse state)
        {
            // Create a new snapshot object so external callers cannot mutate the retained state instance.
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

        /// <summary>
        /// Determines whether an operation status represents a terminal lifecycle state.
        /// </summary>
        /// <param name="status">The operation status value to evaluate.</param>
        /// <returns><see langword="true"/> when the operation has finished; otherwise <see langword="false"/>.</returns>
        private static bool IsTerminal(string status)
        {
            // Treat only succeeded and failed states as terminal because queued and running operations can still emit updates.
            return string.Equals(status, StudioIngestionOperationStatuses.Succeeded, StringComparison.Ordinal)
                || string.Equals(status, StudioIngestionOperationStatuses.Failed, StringComparison.Ordinal);
        }
    }
}
