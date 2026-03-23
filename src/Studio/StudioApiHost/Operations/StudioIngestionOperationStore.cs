using System.Threading.Channels;
using UKHO.Search.Studio;

namespace StudioApiHost.Operations
{
    internal sealed class StudioIngestionOperationStore
    {
        private readonly Dictionary<Guid, StudioTrackedOperation> _operations = [];
        private readonly Lock _syncRoot = new();
        private Guid? _activeOperationId;

        public StudioTrackedOperation? TryCreate(
            string provider,
            string operationType,
            string? context,
            out StudioIngestionOperationConflictResponse? conflictResponse)
        {
            lock (_syncRoot)
            {
                if (_activeOperationId is Guid activeOperationId && _operations.TryGetValue(activeOperationId, out var activeOperation))
                {
                    conflictResponse = CreateConflictResponse(activeOperation.CreateSnapshot());
                    return null;
                }

                var trackedOperation = new StudioTrackedOperation(provider, operationType, context);
                _operations[trackedOperation.OperationId] = trackedOperation;
                _activeOperationId = trackedOperation.OperationId;
                conflictResponse = null;
                return trackedOperation;
            }
        }

        public StudioIngestionOperationConflictResponse? GetActiveConflict()
        {
            lock (_syncRoot)
            {
                if (_activeOperationId is not Guid activeOperationId || !_operations.TryGetValue(activeOperationId, out var activeOperation))
                {
                    return null;
                }

                return CreateConflictResponse(activeOperation.CreateSnapshot());
            }
        }

        public StudioIngestionOperationStateResponse? GetActive()
        {
            lock (_syncRoot)
            {
                if (_activeOperationId is not Guid activeOperationId || !_operations.TryGetValue(activeOperationId, out var activeOperation))
                {
                    return null;
                }

                return activeOperation.CreateSnapshot();
            }
        }

        public StudioIngestionOperationStateResponse? GetById(Guid operationId)
        {
            lock (_syncRoot)
            {
                return _operations.TryGetValue(operationId, out var operation)
                    ? operation.CreateSnapshot()
                    : null;
            }
        }

        public ChannelReader<StudioIngestionOperationEventResponse>? Subscribe(Guid operationId)
        {
            lock (_syncRoot)
            {
                return _operations.TryGetValue(operationId, out var operation)
                    ? operation.Subscribe()
                    : null;
            }
        }

        public void MarkRunning(Guid operationId, string message)
        {
            Update(operationId, operation => operation.MarkRunning(message));
        }

        public void ReportProgress(Guid operationId, StudioIngestionOperationProgressUpdate update)
        {
            ArgumentNullException.ThrowIfNull(update);
            Update(operationId, operation => operation.ReportProgress(update));
        }

        public void MarkSucceeded(Guid operationId, string message, int? completed, int? total)
        {
            Update(operationId, operation => operation.MarkSucceeded(message, completed, total), true);
        }

        public void MarkFailed(Guid operationId, string message, string failureCode, int? completed = null, int? total = null)
        {
            Update(operationId, operation => operation.MarkFailed(message, failureCode, completed, total), true);
        }

        private void Update(Guid operationId, Action<StudioTrackedOperation> updateOperation, bool clearActiveOperation = false)
        {
            lock (_syncRoot)
            {
                if (!_operations.TryGetValue(operationId, out var operation))
                {
                    return;
                }

                updateOperation(operation);

                if (clearActiveOperation && _activeOperationId == operationId)
                {
                    _activeOperationId = null;
                }
            }
        }

        private static StudioIngestionOperationConflictResponse CreateConflictResponse(StudioIngestionOperationStateResponse operation)
        {
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
