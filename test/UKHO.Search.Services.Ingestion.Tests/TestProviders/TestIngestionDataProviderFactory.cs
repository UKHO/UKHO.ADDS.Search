using UKHO.Search.Ingestion.Providers;

namespace UKHO.Search.Services.Ingestion.Tests.TestProviders
{
    internal sealed class TestIngestionDataProviderFactory : IIngestionDataProviderFactory
    {
        private readonly IIngestionDataProvider _provider;

        public TestIngestionDataProviderFactory(string name, string queueName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

            Name = name;
            QueueName = queueName;
            _provider = new TestIngestionDataProvider();
        }

        public string Name { get; }

        public string QueueName { get; }

        public IIngestionDataProvider CreateProvider()
        {
            return _provider;
        }
    }
}
