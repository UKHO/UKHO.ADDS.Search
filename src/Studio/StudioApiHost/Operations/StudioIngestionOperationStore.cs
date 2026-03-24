using System.Threading.Channels;
using UKHO.Search.Studio.Ingestion;

namespace StudioApiHost.Operations
{
    /// <summary>
    /// Stores the current and historical Studio ingestion operations in memory.
    /// </summary>
    internal sealed class StudioIngestionOperationStore
    {
        private readonly Dictionary<Guid, StudioTrackedOperation> _operations = [];
        private readonly Lock _syncRoot = new();
        private Guid? _activeOperationId;

        /// <summary>
        /// Attempts to create and retain a new tracked operation.
        /// </summary>
        /// <param name="provider">The provider that owns the requested operation.</param>
        /// <param name="operationType">The provider-neutral operation type being requested.</param>
        /// <param name="context">The optional provider-neutral context for context-scoped operations.</param>
        /// <param name="conflictResponse">When this method returns <see langword="null"/>, contains the active-operation conflict payload for the currently running operation.</param>
        /// <returns>The newly created tracked operation when the active slot is free; otherwise <see langword="null"/>.</returns>
        public StudioTrackedOperation? TryCreate(
            string provider,
            string operationType,
            string? context,
            out StudioIngestionOperationConflictResponse? conflictResponse)
        {
            lock (_syncRoot)
            {
                // Reject the request when another queued or running operation already owns the active slot.
                if (_activeOperationId is Guid activeOperationId && _operations.TryGetValue(activeOperationId, out var activeOperation))
                {
                    conflictResponse = CreateConflictResponse(activeOperation.CreateSnapshot());
                    return null;
                }

                // Create and retain the tracked operation so later polling and event subscriptions can resolve it.
                var trackedOperation = new StudioTrackedOperation(provider, operationType, context);
                _operations[trackedOperation.OperationId] = trackedOperation;
                _activeOperationId = trackedOperation.OperationId;
                conflictResponse = null;
                return trackedOperation;
            }
        }

        /// <summary>
        /// Gets the conflict payload for the current active operation, when one exists.
        /// </summary>
        /// <returns>The active-operation conflict payload, or <see langword="null"/> when no operation is active.</returns>
        public StudioIngestionOperationConflictResponse? GetActiveConflict()
        {
            lock (_syncRoot)
            {
                // Return null when no operation currently owns the active slot.
                if (_activeOperationId is not Guid activeOperationId || !_operations.TryGetValue(activeOperationId, out var activeOperation))
                {
                    return null;
                }

                // Build a provider-neutral conflict payload from the current active operation snapshot.
                return CreateConflictResponse(activeOperation.CreateSnapshot());
            }
        }

        /// <summary>
        /// Gets the current active operation snapshot, when one exists.
        /// </summary>
        /// <returns>The active operation snapshot, or <see langword="null"/> when no operation is active.</returns>
        public StudioIngestionOperationStateResponse? GetActive()
        {
            lock (_syncRoot)
            {
                // Return null when no operation currently owns the active slot.
                if (_activeOperationId is not Guid activeOperationId || !_operations.TryGetValue(activeOperationId, out var activeOperation))
                {
                    return null;
                }

                // Return a snapshot clone so callers cannot mutate the retained in-memory state.
                return activeOperation.CreateSnapshot();
            }
        }

        /// <summary>
        /// Gets a retained operation snapshot by identifier.
        /// </summary>
        /// <param name="operationId">The identifier of the tracked operation to load.</param>
        /// <returns>The retained operation snapshot, or <see langword="null"/> when the identifier is unknown.</returns>
        public StudioIngestionOperationStateResponse? GetById(Guid operationId)
        {
            lock (_syncRoot)
            {
                // Return a snapshot clone for known operations and null for unknown identifiers.
                return _operations.TryGetValue(operationId, out var operation)
                    ? operation.CreateSnapshot()
                    : null;
            }
        }

        /// <summary>
        /// Subscribes to live events for the specified tracked operation.
        /// </summary>
        /// <param name="operationId">The identifier of the tracked operation to observe.</param>
        /// <returns>A channel reader that yields operation events, or <see langword="null"/> when the identifier is unknown.</returns>
        public ChannelReader<StudioIngestionOperationEventResponse>? Subscribe(Guid operationId)
        {
            lock (_syncRoot)
            {
                // Return the tracked operation's event reader when the operation exists, otherwise report it as unknown.
                return _operations.TryGetValue(operationId, out var operation)
                    ? operation.Subscribe()
                    : null;
            }
        }

        /// <summary>
        /// Marks the specified operation as running.
        /// </summary>
        /// <param name="operationId">The identifier of the operation to update.</param>
        /// <param name="message">The user-facing message describing the running state.</param>
        public void MarkRunning(Guid operationId, string message)
        {
            // Update the tracked operation to the running lifecycle state.
            Update(operationId, operation => operation.MarkRunning(message));
        }

        /// <summary>
        /// Records a provider progress update for the specified operation.
        /// </summary>
        /// <param name="operationId">The identifier of the operation to update.</param>
        /// <param name="update">The provider progress update to persist.</param>
        public void ReportProgress(Guid operationId, StudioIngestionOperationProgressUpdate update)
        {
            // Guard the provider callback input before updating the tracked operation.
            ArgumentNullException.ThrowIfNull(update);

            // Record the latest provider progress message and counts.
            Update(operationId, operation => operation.ReportProgress(update));
        }

        /// <summary>
        /// Marks the specified operation as successfully completed.
        /// </summary>
        /// <param name="operationId">The identifier of the operation to update.</param>
        /// <param name="message">The user-facing completion message.</param>
        /// <param name="completed">The completed item count when available.</param>
        /// <param name="total">The total item count when available.</param>
        public void MarkSucceeded(Guid operationId, string message, int? completed, int? total)
        {
            // Record successful completion and release the active-operation slot.
            Update(operationId, operation => operation.MarkSucceeded(message, completed, total), true);
        }

        /// <summary>
        /// Marks the specified operation as failed.
        /// </summary>
        /// <param name="operationId">The identifier of the operation to update.</param>
        /// <param name="message">The user-facing failure message.</param>
        /// <param name="failureCode">The provider-neutral failure code associated with the failure.</param>
        /// <param name="completed">The completed item count when available.</param>
        /// <param name="total">The total item count when available.</param>
        public void MarkFailed(Guid operationId, string message, string failureCode, int? completed = null, int? total = null)
        {
            // Record terminal failure details and release the active-operation slot.
            Update(operationId, operation => operation.MarkFailed(message, failureCode, completed, total), true);
        }

        private void Update(Guid operationId, Action<StudioTrackedOperation> updateOperation, bool clearActiveOperation = false)
        {
            lock (_syncRoot)
            {
                // Ignore updates for unknown operations because the caller has nothing meaningful to recover from.
                if (!_operations.TryGetValue(operationId, out var operation))
                {
                    return;
                }

                // Apply the supplied state transition to the retained tracked operation instance.
                updateOperation(operation);

                // Clear the active-operation slot once a terminal state has been recorded.
                if (clearActiveOperation && _activeOperationId == operationId)
                {
                    _activeOperationId = null;
                }
            }
        }

        private static StudioIngestionOperationConflictResponse CreateConflictResponse(StudioIngestionOperationStateResponse operation)
        {
            // Translate the current active operation snapshot into the standardized conflict payload returned by the APIs.
            return new StudioIngestionOperationConflictResponse
            {
                Message = "Another ingestion operation is already active.",
                ActiveOperationId = operation.OperationId,
                ActiveProvider = operation.Provider,
                ActiveOperationType = operation.OperationType
            };
        }
    }
}
