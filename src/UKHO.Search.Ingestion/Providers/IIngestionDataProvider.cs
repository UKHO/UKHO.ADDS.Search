using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers
{
    public interface IIngestionDataProvider
    {
        string Name { get; }

        ValueTask<IngestionRequest> DeserializeIngestionRequestAsync(string messageText, CancellationToken cancellationToken = default);
    }
}