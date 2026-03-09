using Microsoft.Extensions.Logging;

namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public sealed class QueueMessageAcker : IQueueMessageAcker
    {
        private readonly ILogger logger;
        private readonly string messageId;
        private readonly string messageText;
        private readonly IQueueClient queue;
        private readonly CancellationTokenSource renewalCts = new();
        private int deleted;
        private string popReceipt;

        public QueueMessageAcker(IQueueClient queue, string messageId, string popReceipt, string messageText, ILogger logger)
        {
            this.queue = queue;
            this.messageId = messageId;
            this.popReceipt = popReceipt;
            this.messageText = messageText;
            this.logger = logger;
        }

        public Task? VisibilityRenewalTask { get; private set; }

        public async ValueTask DeleteAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref deleted, 1, 0) != 0)
            {
                return;
            }

            renewalCts.Cancel();

            if (VisibilityRenewalTask is not null)
            {
                try
                {
                    await VisibilityRenewalTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            await queue.DeleteMessageAsync(messageId, popReceipt, cancellationToken)
                       .ConfigureAwait(false);
        }

        public async ValueTask UpdateVisibilityAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            var receipt = await queue.UpdateMessageAsync(messageId, popReceipt, messageText, visibilityTimeout, cancellationToken)
                                     .ConfigureAwait(false);

            popReceipt = receipt.PopReceipt;
        }

        public async ValueTask MoveToPoisonAsync(IQueueClient poisonQueue, string poisonMessageBody, CancellationToken cancellationToken)
        {
            await poisonQueue.SendMessageAsync(poisonMessageBody, cancellationToken)
                             .ConfigureAwait(false);

            await DeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public void StartVisibilityRenewal(TimeSpan visibilityTimeout, TimeSpan renewalInterval, CancellationToken pipelineCancellationToken)
        {
            if (VisibilityRenewalTask is not null)
            {
                return;
            }

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(renewalCts.Token, pipelineCancellationToken);
            VisibilityRenewalTask = Task.Run(() => RunRenewalLoopAsync(visibilityTimeout, renewalInterval, linkedCts.Token), CancellationToken.None);
        }

        private async Task RunRenewalLoopAsync(TimeSpan visibilityTimeout, TimeSpan renewalInterval, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    await Task.Delay(renewalInterval, cancellationToken)
                              .ConfigureAwait(false);

                    await UpdateVisibilityAsync(visibilityTimeout, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Queue visibility renewal failed. MessageId={MessageId}", messageId);
                throw;
            }
        }
    }
}