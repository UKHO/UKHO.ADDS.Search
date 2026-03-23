using System;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.Search.Studio.Providers.FileShare
{
    public interface IFileShareStudioBatchPayloadStore
    {
        Task<IReadOnlyList<FileShareStudioBusinessUnit>> GetBusinessUnitsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Guid>> GetPendingBatchIdsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Guid>> GetPendingBatchIdsForBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default);

        Task MarkBatchIndexedAsync(Guid batchId, CancellationToken cancellationToken = default);

        Task<int> ResetAllIndexingStatusAsync(CancellationToken cancellationToken = default);

        Task<int> ResetIndexingStatusForBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default);

        Task<FileShareStudioBatchPayloadSource?> TryGetPayloadSourceAsync(Guid batchId, CancellationToken cancellationToken = default);
    }
}
