using System.Text.Json.Nodes;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Pipeline
{
    public interface IFileShareIngestionEnricher
    {
        ValueTask<JsonObject?> TryBuildEnrichmentAsync(IngestionRequest request, CancellationToken cancellationToken = default);
    }
}