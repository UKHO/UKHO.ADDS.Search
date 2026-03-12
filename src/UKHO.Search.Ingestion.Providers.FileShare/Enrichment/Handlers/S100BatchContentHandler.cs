using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers
{
    public sealed class S100BatchContentHandler : IBatchContentHandler
    {
        private readonly ILogger<S100BatchContentHandler> _logger;

        public S100BatchContentHandler(ILogger<S100BatchContentHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public async Task HandleFiles(IEnumerable<string> paths, IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(paths);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            var batchId = request.AddItem?.Id ?? request.UpdateItem?.Id;

            var catalogPath = paths.Select(p => new { Path = p, FileName = System.IO.Path.GetFileName(p) })
                                   .Where(x => string.Equals(x.FileName, "catalog.xml", StringComparison.OrdinalIgnoreCase))
                                   .Select(x => x.Path)
                                   .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                   .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(catalogPath))
            {
                return;
            }

            try
            {
                await using var stream = File.OpenRead(catalogPath);
                var catalogXml = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken)
                                                .ConfigureAwait(false);
                _ = catalogXml;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to load catalog.xml. BatchId={BatchId} FilePath={FilePath}", batchId, catalogPath);
            }
        }
    }
}
