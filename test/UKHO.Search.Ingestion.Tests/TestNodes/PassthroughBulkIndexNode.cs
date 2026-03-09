using System.Threading.Channels;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Ingestion.Tests.TestNodes
{
    public sealed class PassthroughBulkIndexNode : INode
    {
        private readonly ChannelWriter<Envelope<IndexOperation>> deadLetterOutput;
        private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
        private readonly ChannelReader<BatchEnvelope<IndexOperation>> input;
        private readonly ChannelWriter<Envelope<IndexOperation>> successOutput;
        private Task? completion;

        public PassthroughBulkIndexNode(string name, ChannelReader<BatchEnvelope<IndexOperation>> input, ChannelWriter<Envelope<IndexOperation>> successOutput, ChannelWriter<Envelope<IndexOperation>> deadLetterOutput, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            this.input = input;
            this.successOutput = successOutput;
            this.deadLetterOutput = deadLetterOutput;
            this.fatalErrorReporter = fatalErrorReporter;
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
                        foreach (var envelope in batch.Items)
                        {
                            await successOutput.WriteAsync(envelope, cancellationToken)
                                               .ConfigureAwait(false);
                        }
                    }
                }

                successOutput.TryComplete();
                deadLetterOutput.TryComplete();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                successOutput.TryComplete();
                deadLetterOutput.TryComplete();
            }
            catch (Exception ex)
            {
                successOutput.TryComplete(ex);
                deadLetterOutput.TryComplete(ex);
                fatalErrorReporter?.ReportFatal(Name, ex);
                throw;
            }
        }
    }
}