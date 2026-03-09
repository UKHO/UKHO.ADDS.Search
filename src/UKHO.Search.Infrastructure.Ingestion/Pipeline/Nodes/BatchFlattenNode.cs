using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes
{
    public sealed class BatchFlattenNode<TPayload> : INode
    {
        private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
        private readonly ChannelReader<BatchEnvelope<TPayload>> input;
        private readonly ILogger? logger;
        private readonly NodeMetrics metrics;
        private readonly ChannelWriter<Envelope<TPayload>> output;
        private Task? completion;

        public BatchFlattenNode(string name, ChannelReader<BatchEnvelope<TPayload>> input, ChannelWriter<Envelope<TPayload>> output, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            this.input = input;
            this.output = output;
            this.logger = logger;
            this.fatalErrorReporter = fatalErrorReporter;
            metrics = new NodeMetrics(name);
        }

        public string Name { get; }

        public Task Completion => completion ?? Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            completion ??= Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
            return Task.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (await input.WaitToReadAsync(cancellationToken)
                                  .ConfigureAwait(false))
                {
                    while (input.TryRead(out var batch))
                    {
                        metrics.RecordIn(batch);
                        metrics.IncrementInFlight();
                        var started = Stopwatch.GetTimestamp();
                        try
                        {
                            foreach (var item in batch.Items)
                            {
                                item.Context.AddBreadcrumb(Name);

                                logger?.LogInformation("Stub indexed message. NodeName={NodeName} PartitionId={PartitionId} Key={Key} MessageId={MessageId} Attempt={Attempt}", Name, batch.PartitionId, item.Key, item.MessageId, item.Attempt);

                                await output.WriteAsync(item, cancellationToken)
                                            .ConfigureAwait(false);
                                metrics.RecordOut(item);
                            }
                        }
                        finally
                        {
                            var elapsed = Stopwatch.GetElapsedTime(started);
                            metrics.RecordDuration(elapsed);
                            metrics.DecrementInFlight();
                        }
                    }
                }

                await input.Completion.ConfigureAwait(false);
                output.TryComplete();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                output.TryComplete();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Node '{NodeName}' failed.", Name);
                output.TryComplete(ex);
                fatalErrorReporter?.ReportFatal(Name, ex);
                throw;
            }
            finally
            {
                metrics.Dispose();
            }
        }
    }
}