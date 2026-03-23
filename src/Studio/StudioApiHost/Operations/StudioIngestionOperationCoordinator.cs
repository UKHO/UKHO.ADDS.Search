using UKHO.Search.Studio;

namespace StudioApiHost.Operations
{
    internal sealed class StudioIngestionOperationCoordinator
    {
        private readonly ILogger<StudioIngestionOperationCoordinator> _logger;
        private readonly StudioIngestionOperationStore _operationStore;

        public StudioIngestionOperationCoordinator(
            StudioIngestionOperationStore operationStore,
            ILogger<StudioIngestionOperationCoordinator> logger)
        {
            _operationStore = operationStore ?? throw new ArgumentNullException(nameof(operationStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool TryStartOperation(
            string provider,
            string operationType,
            string? context,
            Func<IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>> executeAsync,
            out StudioIngestionAcceptedOperationResponse acceptedResponse,
            out StudioIngestionOperationConflictResponse? conflictResponse)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);
            ArgumentException.ThrowIfNullOrWhiteSpace(operationType);
            ArgumentNullException.ThrowIfNull(executeAsync);

            conflictResponse = null;

            var trackedOperation = _operationStore.TryCreate(provider, operationType, context, out conflictResponse);
            if (trackedOperation is null)
            {
                acceptedResponse = null!;
                return false;
            }

            acceptedResponse = new StudioIngestionAcceptedOperationResponse
            {
                OperationId = trackedOperation.OperationId.ToString("D"),
                Provider = provider,
                OperationType = operationType,
                Context = context,
                Status = StudioIngestionOperationStatuses.Queued
            };

            _ = Task.Run(() => ExecuteOperationAsync(trackedOperation.OperationId, executeAsync));

            return true;
        }

        private async Task ExecuteOperationAsync(
            Guid operationId,
            Func<IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>> executeAsync)
        {
            _operationStore.MarkRunning(operationId, "Processing operation.");

            var progress = new Progress<StudioIngestionOperationProgressUpdate>(update =>
            {
                _operationStore.ReportProgress(operationId, update);
            });

            try
            {
                var result = await executeAsync(progress, CancellationToken.None)
                    .ConfigureAwait(false);

                if (result.Succeeded)
                {
                    _operationStore.MarkSucceeded(operationId, result.Message, result.Completed, result.Total);
                    return;
                }

                _operationStore.MarkFailed(operationId, result.Message, result.FailureCode ?? StudioIngestionFailureCodes.ProviderError, result.Completed, result.Total);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unhandled failure while executing studio ingestion operation {OperationId}.", operationId);
                _operationStore.MarkFailed(operationId, "The operation failed unexpectedly.", StudioIngestionFailureCodes.UnexpectedError);
            }
        }
    }
}
