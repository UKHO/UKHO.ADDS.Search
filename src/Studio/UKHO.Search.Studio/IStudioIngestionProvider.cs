using System.Threading;
using System.Threading.Tasks;

namespace UKHO.Search.Studio
{
    public interface IStudioIngestionProvider : IStudioProvider
    {
        Task<StudioIngestionFetchPayloadResult> FetchPayloadByIdAsync(string id, CancellationToken cancellationToken = default);

        Task<StudioIngestionContextsResponse> GetContextsAsync(CancellationToken cancellationToken = default);

        Task<StudioIngestionOperationExecutionResult> IndexAllAsync(
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default);

        Task<StudioIngestionOperationExecutionResult> IndexContextAsync(
            string context,
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default);

        Task<StudioIngestionOperationExecutionResult> ResetIndexingStatusAsync(
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default);

        Task<StudioIngestionOperationExecutionResult> ResetIndexingStatusForContextAsync(
            string context,
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default);

        Task<StudioIngestionSubmitPayloadResult> SubmitPayloadAsync(StudioIngestionPayloadEnvelope request, CancellationToken cancellationToken = default);
    }
}
