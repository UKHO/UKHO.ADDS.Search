using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Retry;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
	public sealed class RetryingTransformNodeEdgeCaseTests
	{
		[Fact]
		public async Task Non_transient_exception_is_not_retried_and_is_routed_to_error_output()
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

			var supervisor = new PipelineSupervisor(cts.Token);

			var input = BoundedChannelFactory.Create<Envelope<int>>(capacity: 4, singleReader: true, singleWriter: true);
			var output = BoundedChannelFactory.Create<Envelope<int>>(capacity: 4, singleReader: true, singleWriter: true);
			var errors = BoundedChannelFactory.Create<Envelope<int>>(capacity: 4, singleReader: true, singleWriter: true);

			var retryPolicy = new ExponentialBackoffRetryPolicy(
				maxAttempts: 5,
				baseDelay: TimeSpan.FromMilliseconds(1),
				maxDelay: TimeSpan.FromMilliseconds(1),
				jitterFactor: 0);

			static ValueTask<int> Transform(int payload, CancellationToken cancellationToken)
			{
				throw new InvalidOperationException("non-transient");
			}

			var node = new RetryingTransformNode<int, int>(
				"retrying-transform",
				input.Reader,
				output.Writer,
				Transform,
				retryPolicy,
				isTransientException: _ => false,
				errorOutput: errors.Writer,
				forwardFailedToMainOutput: false,
				fatalErrorReporter: supervisor);

			var sink = new CollectingSinkNode<int>("sink", output.Reader, fatalErrorReporter: supervisor);
			var errorSink = new CollectingSinkNode<int>("error-sink", errors.Reader, fatalErrorReporter: supervisor);

			supervisor.AddNode(node);
			supervisor.AddNode(sink);
			supervisor.AddNode(errorSink);

			await supervisor.StartAsync();

			await input.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);
			input.Writer.TryComplete();

			await supervisor.Completion.WaitAsync(cts.Token);

			sink.Items.Count.ShouldBe(0);
			errorSink.Items.Count.ShouldBe(1);
			errorSink.Items[0].Attempt.ShouldBe(1);
			errorSink.Items[0].Status.ShouldBe(MessageStatus.Failed);
		}
	}
}
