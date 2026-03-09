using Azure.Storage.Queues;

namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public sealed class AzureQueueClientFactory : IQueueClientFactory
    {
        private readonly QueueServiceClient queueServiceClient;

        public AzureQueueClientFactory(QueueServiceClient queueServiceClient)
        {
            this.queueServiceClient = queueServiceClient;
        }

        public IQueueClient GetQueueClient(string queueName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

            var queueClient = queueServiceClient.GetQueueClient(queueName);
            return new AzureQueueClient(queueClient);
        }
    }
}