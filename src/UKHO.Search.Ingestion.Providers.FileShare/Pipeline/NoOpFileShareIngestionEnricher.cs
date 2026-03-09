using System.Text.Json.Nodes;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Pipeline
{
    public sealed class NoOpFileShareIngestionEnricher : IFileShareIngestionEnricher
    {
        public ValueTask<JsonObject?> TryBuildEnrichmentAsync(IngestionRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return ValueTask.FromResult<JsonObject?>(null);
        }
    }
}