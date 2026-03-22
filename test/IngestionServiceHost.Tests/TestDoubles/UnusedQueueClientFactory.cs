using UKHO.Search.Infrastructure.Ingestion.Queue;

namespace IngestionServiceHost.Tests.TestDoubles
{
    internal sealed class UnusedQueueClientFactory : IQueueClientFactory
    {
        public IQueueClient GetQueueClient(string queueName)
        {
            throw new NotSupportedException();
        }
    }
}
