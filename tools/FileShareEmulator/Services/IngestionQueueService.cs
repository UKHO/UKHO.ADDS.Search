using System.Diagnostics;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace FileShareEmulator.Services
{
    public sealed class IngestionQueueService
    {
        private const string QueueName = "file-share-queue";
        private const string DefaultPoisonQueueSuffix = "-poison";
        private static readonly TimeSpan DefaultMaxClearDuration = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DefaultVerifyTimeout = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan DefaultVerifyPollDelay = TimeSpan.FromMilliseconds(200);
        private const int DefaultRequiredEmptyPolls = 5;

        private readonly ILogger<IngestionQueueService> _logger;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly IConfiguration _configuration;

        public IngestionQueueService(QueueServiceClient queueServiceClient, IConfiguration configuration, ILogger<IngestionQueueService> logger)
        {
            _queueServiceClient = queueServiceClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<QueueClearResult> ClearAllAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            // Drain strategy:
            // - Repeatedly receive + delete batches of messages
            // - Then verify emptiness with a short stability window
            var (mainQueueClient, poisonQueueClient) = await GetQueueClientsAsync(cancellationToken)
                .ConfigureAwait(false);

            var removed = 0;
            var attempts = 0;

            try
            {
                using var maxDurationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                maxDurationCts.CancelAfter(DefaultMaxClearDuration);
                var ct = maxDurationCts.Token;

                const int maxMessages = 32;
                var visibilityTimeout = TimeSpan.FromSeconds(1);

                _logger.LogInformation("Clearing ingestion queue {QueueName} and poison queue.", QueueName);

                try
                {
                    var mainDrain = await DrainQueueAsync("main", mainQueueClient, maxMessages, visibilityTimeout, stopwatch, ct)
                        .ConfigureAwait(false);
                    removed += mainDrain.Removed;
                    attempts += mainDrain.Attempts;

                    var poisonDrain = await DrainQueueAsync("poison", poisonQueueClient, maxMessages, visibilityTimeout, stopwatch, ct)
                        .ConfigureAwait(false);
                    removed += poisonDrain.Removed;
                    attempts += poisonDrain.Attempts;
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, "Failed clearing one or more queues.");
                    return QueueClearResult.Fail("Failed clearing one or more queues.", removed, attempts, stopwatch, ex);
                }

                // Verification: require stable emptiness across multiple polls.
                var stableEmpty = await VerifyEmptyAsync(mainQueueClient, DefaultRequiredEmptyPolls, DefaultVerifyTimeout, DefaultVerifyPollDelay, ct)
                                  && await VerifyEmptyAsync(poisonQueueClient, DefaultRequiredEmptyPolls, DefaultVerifyTimeout, DefaultVerifyPollDelay, ct)
                    .ConfigureAwait(false);

                if (!stableEmpty)
                {
                    return QueueClearResult.Fail("Queue could not be verified empty.", removed, attempts, stopwatch);
                }

                _logger.LogInformation("Cleared ingestion queue {QueueName} and poison queue. Removed {Removed}. Attempts {Attempts}. Duration {Duration}.", QueueName, removed, attempts, stopwatch.Elapsed);
                return QueueClearResult.Success(removed, attempts, stopwatch);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Queue clear canceled or timed out for queue {QueueName}. Removed {Removed}. Attempts {Attempts}. Duration {Duration}.", QueueName, removed, attempts, stopwatch.Elapsed);
                return QueueClearResult.Fail("Queue clear canceled or timed out.", removed, attempts, stopwatch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error clearing ingestion queue.");
                return QueueClearResult.Fail("Unexpected error clearing ingestion queue.", removed, attempts, stopwatch, ex);
            }
        }

        public async Task<QueueStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            var (mainQueueClient, poisonQueueClient) = await GetQueueClientsAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var props = await mainQueueClient.GetPropertiesAsync(cancellationToken: cancellationToken)
                                             .ConfigureAwait(false);

                var count = props.Value?.ApproximateMessagesCount;

                var poisonProps = await poisonQueueClient.GetPropertiesAsync(cancellationToken: cancellationToken)
                                                        .ConfigureAwait(false);
                var poisonCount = poisonProps.Value?.ApproximateMessagesCount;

                var combined = (count ?? 0) + (poisonCount ?? 0);

                return new QueueStatus
                {
                    IsEmpty = combined == 0,
                    ApproximateMessageCount = combined
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query queue properties for status; falling back to receive verification.");

                var isEmpty = await VerifyEmptyAsync(mainQueueClient, DefaultRequiredEmptyPolls, DefaultVerifyTimeout, DefaultVerifyPollDelay, cancellationToken)
                              && await VerifyEmptyAsync(poisonQueueClient, DefaultRequiredEmptyPolls, DefaultVerifyTimeout, DefaultVerifyPollDelay, cancellationToken)
                    .ConfigureAwait(false);

                return new QueueStatus
                {
                    IsEmpty = isEmpty,
                    ApproximateMessageCount = null
                };
            }
        }

        private static async Task<bool> VerifyEmptyAsync(QueueClient queueClient, int requiredEmptyPolls, TimeSpan verifyTimeout, TimeSpan pollDelay, CancellationToken cancellationToken)
        {
            // Stability window: require N consecutive empty receives to reduce chance that visibility delays
            // or concurrent producers make an immediate empty-check unreliable.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(verifyTimeout);
            var ct = timeoutCts.Token;

            var emptyPolls = 0;

            while (emptyPolls < requiredEmptyPolls)
            {
                ct.ThrowIfCancellationRequested();

                var response = await queueClient.ReceiveMessagesAsync(maxMessages: 1, visibilityTimeout: TimeSpan.FromSeconds(1), ct)
                                                .ConfigureAwait(false);

                if (response.Value.Length > 0)
                {
                    return false;
                }

                emptyPolls++;
                await Task.Delay(pollDelay, ct)
                          .ConfigureAwait(false);
            }

            return true;
        }

        private async Task<(QueueClient Main, QueueClient Poison)> GetQueueClientsAsync(CancellationToken cancellationToken)
        {
            var main = _queueServiceClient.GetQueueClient(QueueName);
            await main.CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                      .ConfigureAwait(false);

            var poisonSuffix = _configuration["ingestion:poisonQueueSuffix"];
            if (string.IsNullOrWhiteSpace(poisonSuffix))
            {
                poisonSuffix = DefaultPoisonQueueSuffix;
            }

            var poisonName = QueueName + poisonSuffix;
            var poison = _queueServiceClient.GetQueueClient(poisonName);
            await poison.CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

            return (main, poison);
        }

        private async Task<(int Removed, int Attempts)> DrainQueueAsync(string queueKind, QueueClient queueClient, int maxMessages, TimeSpan visibilityTimeout, Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            var removed = 0;
            var attempts = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempts++;

                QueueMessage[] messages;

                try
                {
                    var response = await queueClient.ReceiveMessagesAsync(maxMessages, visibilityTimeout, cancellationToken)
                                                    .ConfigureAwait(false);
                    messages = response.Value;
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, "Failed receiving {QueueKind} queue messages.", queueKind);
                    throw;
                }

                if (messages.Length == 0)
                {
                    return (removed, attempts);
                }

                foreach (var message in messages)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken)
                                         .ConfigureAwait(false);
                        removed++;
                    }
                    catch (RequestFailedException ex)
                    {
                        _logger.LogError(ex, "Failed deleting {QueueKind} queue message {MessageId}.", queueKind, message.MessageId);
                        throw;
                    }
                }

                if (stopwatch.Elapsed > DefaultMaxClearDuration)
                {
                    _logger.LogWarning("Queue clear exceeded max duration while draining {QueueKind} queue.", queueKind);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }
    }
}
