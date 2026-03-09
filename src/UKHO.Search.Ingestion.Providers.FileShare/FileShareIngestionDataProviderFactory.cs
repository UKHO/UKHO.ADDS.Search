using UKHO.Search.Ingestion.Providers;

namespace UKHO.Search.Ingestion.Providers.FileShare
{
    public sealed class FileShareIngestionDataProviderFactory : IIngestionDataProviderFactory
    {
        public FileShareIngestionDataProviderFactory(string queueName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

            QueueName = queueName;
        }

        public string Name => "file-share";

        public string QueueName { get; }

        public IIngestionDataProvider CreateProvider()
        {
            return new FileShareIngestionDataProvider();
        }
    }
}
