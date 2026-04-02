using UKHO.Search.Ingestion.Pipeline.Documents;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers
{
    internal interface IS57Enricher
    {
        bool TryParse(string pathTo000, CanonicalDocument document);
    }
}
