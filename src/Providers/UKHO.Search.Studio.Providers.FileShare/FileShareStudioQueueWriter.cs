using Azure.Storage.Queues;

namespace UKHO.Search.Studio.Providers.FileShare
{
    public sealed class FileShareStudioQueueWriter : IFileShareStudioQueueWriter
    {
        private const string QueueName = "file-share-queue";

        private readonly QueueServiceClient _queueServiceClient;

        public FileShareStudioQueueWriter(QueueServiceClient queueServiceClient)
        {
            _queueServiceClient = queueServiceClient ?? throw new ArgumentNullException(nameof(queueServiceClient));
        }

        public async Task SubmitAsync(string payloadJson, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

            var queueClient = _queueServiceClient.GetQueueClient(QueueName);
            _ = await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                                 .ConfigureAwait(false);
            await queueClient.SendMessageAsync(payloadJson, cancellationToken)
                             .ConfigureAwait(false);
        }
    }
}
