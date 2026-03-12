using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers
{
    public sealed class S57BatchContentHandler : IBatchContentHandler
    {
        private readonly ILogger<S57BatchContentHandler> _logger;

        public S57BatchContentHandler(ILogger<S57BatchContentHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public Task HandleFiles(IEnumerable<string> paths, IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(paths);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            _ = cancellationToken;
            _logger.LogDebug("S57 batch content handler invoked.");
            return Task.CompletedTask;
        }
    }
}
