using UKHO.Search.Studio.Ingestion;

namespace StudioServiceHost.Operations
{
    /// <summary>
    /// Coordinates the lifecycle of tracked Studio ingestion operations.
    /// </summary>
    internal sealed class StudioIngestionOperationCoordinator
    {
        private readonly ILogger<StudioIngestionOperationCoordinator> _logger;
        private readonly StudioIngestionOperationStore _operationStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="StudioIngestionOperationCoordinator"/> class.
        /// </summary>
        /// <param name="operationStore">The store that tracks active and completed Studio ingestion operations.</param>
        /// <param name="logger">The logger used to record unexpected execution failures.</param>
        public StudioIngestionOperationCoordinator(
            StudioIngestionOperationStore operationStore,
            ILogger<StudioIngestionOperationCoordinator> logger)
        {
            // Capture the collaborators that coordinate tracked operation execution and diagnostics.
            _operationStore = operationStore ?? throw new ArgumentNullException(nameof(operationStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Attempts to start a tracked Studio ingestion operation.
        /// </summary>
        /// <param name="provider">The provider that owns the requested operation.</param>
        /// <param name="operationType">The provider-neutral operation type being requested.</param>
        /// <param name="context">The optional provider-neutral context for context-scoped operations.</param>
        /// <param name="executeAsync">The provider callback that performs the long-running work and reports progress.</param>
        /// <param name="acceptedResponse">When this method returns <see langword="true"/>, contains the accepted operation metadata returned to the caller.</param>
        /// <param name="conflictResponse">When this method returns <see langword="false"/>, contains the active-operation conflict details when another operation is already running.</param>
        /// <returns><see langword="true"/> when the operation was accepted; otherwise <see langword="false"/>.</returns>
        public bool TryStartOperation(
            string provider,
            string operationType,
            string? context,
            Func<IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>> executeAsync,
            out StudioIngestionAcceptedOperationResponse acceptedResponse,
            out StudioIngestionOperationConflictResponse? conflictResponse)
        {
            // Validate the request details before interacting with the shared operation store.
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);
            ArgumentException.ThrowIfNullOrWhiteSpace(operationType);
            ArgumentNullException.ThrowIfNull(executeAsync);

            conflictResponse = null;

            // Try to claim the single active-operation slot for the requested provider operation.
            var trackedOperation = _operationStore.TryCreate(provider, operationType, context, out conflictResponse);
            if (trackedOperation is null)
            {
                acceptedResponse = null!;
                return false;
            }

            // Return the accepted operation metadata immediately so the caller can poll or subscribe for updates.
            acceptedResponse = new StudioIngestionAcceptedOperationResponse
            {
                OperationId = trackedOperation.OperationId.ToString("D"),
                Provider = provider,
                OperationType = operationType,
                Context = context,
                Status = StudioIngestionOperationStatuses.Queued
            };

            // Run the provider callback in the background so the HTTP request can complete immediately.
            _ = Task.Run(() => ExecuteOperationAsync(trackedOperation.OperationId, executeAsync));

            return true;
        }

        /// <summary>
        /// Executes a tracked provider callback and translates its lifecycle into stored operation state.
        /// </summary>
        /// <param name="operationId">The identifier of the tracked operation being executed.</param>
        /// <param name="executeAsync">The provider callback that performs the long-running work and reports progress.</param>
        /// <returns>A task that completes when the tracked operation reaches a terminal state.</returns>
        private async Task ExecuteOperationAsync(
            Guid operationId,
            Func<IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>> executeAsync)
        {
            // Move the operation into the running state before invoking the provider callback.
            _operationStore.MarkRunning(operationId, "Processing operation.");

            // Forward provider progress callbacks into the shared in-memory operation store.
            var progress = new SynchronousProgress<StudioIngestionOperationProgressUpdate>(update =>
            {
                _operationStore.ReportProgress(operationId, update);
            });

            try
            {
                // Execute the provider callback and translate its normalized result into tracked operation state.
                var result = await executeAsync(progress, CancellationToken.None)
                    .ConfigureAwait(false);

                if (result.Succeeded)
                {
                    // Record successful completion, including any coarse progress totals returned by the provider.
                    _operationStore.MarkSucceeded(operationId, result.Message, result.Completed, result.Total);
                    return;
                }

                // Record provider-reported failure details so clients can inspect the terminal operation state.
                _operationStore.MarkFailed(operationId, result.Message, result.FailureCode ?? StudioIngestionFailureCodes.ProviderError, result.Completed, result.Total);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Convert unexpected execution failures into a standardized failed operation state after logging them.
                _logger.LogError(ex, "Unhandled failure while executing studio ingestion operation {OperationId}.", operationId);
                _operationStore.MarkFailed(operationId, "The operation failed unexpectedly.", StudioIngestionFailureCodes.UnexpectedError);
            }
        }
    }
}
