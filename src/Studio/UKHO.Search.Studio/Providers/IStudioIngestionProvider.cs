using System.Threading;
using System.Threading.Tasks;

using UKHO.Search.Studio.Ingestion;

namespace UKHO.Search.Studio.Providers
{
    /// <summary>
    /// Defines the Studio ingestion operations that a provider can expose to the Studio host.
    /// </summary>
    public interface IStudioIngestionProvider : IStudioProvider
    {
        /// <summary>
        /// Fetches an opaque provider payload for the supplied provider-defined identifier.
        /// </summary>
        /// <param name="id">The provider-defined identifier for the payload to load.</param>
        /// <param name="cancellationToken">A token that cancels the payload lookup.</param>
        /// <returns>
        /// A result describing whether the payload was found and, when successful, returning the wrapped payload envelope.
        /// </returns>
        Task<StudioIngestionFetchPayloadResult> FetchPayloadByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads the set of provider-neutral contexts that Studio can present for scoped ingestion operations.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the context lookup.</param>
        /// <returns>The provider name together with the available contexts.</returns>
        Task<StudioIngestionContextsResponse> GetContextsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts provider-wide ingestion for all items the provider currently exposes as pending.
        /// </summary>
        /// <param name="progress">Reports externally visible progress updates while the operation runs.</param>
        /// <param name="cancellationToken">A token that cancels the provider-wide ingestion operation.</param>
        /// <returns>The terminal execution result for the requested operation.</returns>
        Task<StudioIngestionOperationExecutionResult> IndexAllAsync(
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts ingestion for a single provider-neutral context.
        /// </summary>
        /// <param name="context">The provider-neutral context value to ingest.</param>
        /// <param name="progress">Reports externally visible progress updates while the operation runs.</param>
        /// <param name="cancellationToken">A token that cancels the context-scoped ingestion operation.</param>
        /// <returns>The terminal execution result for the requested operation.</returns>
        Task<StudioIngestionOperationExecutionResult> IndexContextAsync(
            string context,
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets provider-wide indexing status so items can be considered for reprocessing.
        /// </summary>
        /// <param name="progress">Reports externally visible progress updates while the reset runs.</param>
        /// <param name="cancellationToken">A token that cancels the reset operation.</param>
        /// <returns>The terminal execution result for the requested reset operation.</returns>
        Task<StudioIngestionOperationExecutionResult> ResetIndexingStatusAsync(
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets indexing status for the supplied provider-neutral context.
        /// </summary>
        /// <param name="context">The provider-neutral context value to reset.</param>
        /// <param name="progress">Reports externally visible progress updates while the reset runs.</param>
        /// <param name="cancellationToken">A token that cancels the context-scoped reset operation.</param>
        /// <returns>The terminal execution result for the requested reset operation.</returns>
        Task<StudioIngestionOperationExecutionResult> ResetIndexingStatusForContextAsync(
            string context,
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Submits a provider payload to the provider-specific Studio ingestion pipeline.
        /// </summary>
        /// <param name="request">The wrapped provider payload that should be submitted.</param>
        /// <param name="cancellationToken">A token that cancels the payload submission.</param>
        /// <returns>A result describing whether the payload was accepted for submission.</returns>
        Task<StudioIngestionSubmitPayloadResult> SubmitPayloadAsync(StudioIngestionPayloadEnvelope request, CancellationToken cancellationToken = default);
    }
}
