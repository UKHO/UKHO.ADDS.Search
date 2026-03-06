using System.Diagnostics;
using System.Threading.Channels;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
	public sealed class MicroBatchNode<TPayload> : INode
	{
		private readonly ChannelReader<Envelope<TPayload>> input;
		private readonly ChannelWriter<BatchEnvelope<TPayload>> output;
		private readonly int partitionId;
		private readonly int maxItems;
		private readonly TimeSpan maxDelay;
		private readonly Action<string>? log;
		private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
		private readonly NodeMetrics metrics;
		private Task? completion;

		private readonly List<Envelope<TPayload>> buffer = new();
		private DateTimeOffset? batchCreatedUtc;

		public MicroBatchNode(
			string name,
			int partitionId,
			ChannelReader<Envelope<TPayload>> input,
			ChannelWriter<BatchEnvelope<TPayload>> output,
			int maxItems,
			TimeSpan maxDelay,
			Action<string>? log = null,
			IPipelineFatalErrorReporter? fatalErrorReporter = null)
		{
			if (maxItems <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(maxItems));
			}

			if (maxDelay < TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException(nameof(maxDelay));
			}

			Name = name;
			this.partitionId = partitionId;
			this.input = input;
			this.output = output;
			this.maxItems = maxItems;
			this.maxDelay = maxDelay;
			this.log = log;
			this.fatalErrorReporter = fatalErrorReporter;
			metrics = new NodeMetrics(name, () => buffer.Count);
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
				while (true)
				{
					if (buffer.Count == 0)
					{
						if (!await input.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
						{
							await input.Completion.ConfigureAwait(false);
							break;
						}

						DrainAvailable();
						if (buffer.Count >= maxItems)
						{
							await FlushAsync(cancellationToken).ConfigureAwait(false);
						}

						continue;
					}

					var deadlineUtc = batchCreatedUtc!.Value + maxDelay;
					var nowUtc = DateTimeOffset.UtcNow;
					var remaining = deadlineUtc - nowUtc;

					if (remaining <= TimeSpan.Zero)
					{
						await FlushAsync(cancellationToken).ConfigureAwait(false);
						continue;
					}

					var waitForRead = input.WaitToReadAsync(cancellationToken).AsTask();
					var waitForDelay = Task.Delay(remaining, cancellationToken);
					var completed = await Task.WhenAny(waitForRead, waitForDelay).ConfigureAwait(false);

					if (completed == waitForDelay)
					{
						await FlushAsync(cancellationToken).ConfigureAwait(false);
						continue;
					}

					if (!await waitForRead.ConfigureAwait(false))
					{
						await input.Completion.ConfigureAwait(false);
						break;
					}

					DrainAvailable();
					if (buffer.Count >= maxItems)
					{
						await FlushAsync(cancellationToken).ConfigureAwait(false);
					}
				}

				if (buffer.Count > 0)
				{
					await FlushAsync(cancellationToken).ConfigureAwait(false);
				}

				output.TryComplete();
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				output.TryComplete();
			}
			catch (Exception ex)
			{
				log?.Invoke($"Node '{Name}' failed: {ex.GetType().Name}: {ex.Message}");
				output.TryComplete(ex);
				fatalErrorReporter?.ReportFatal(Name, ex);
				throw;
			}
			finally
			{
				metrics.Dispose();
			}
		}

		private void DrainAvailable()
		{
			while (input.TryRead(out var item))
			{
				metrics.RecordIn(item);
				item.Context.AddBreadcrumb(Name);

				if (buffer.Count == 0)
				{
					batchCreatedUtc = DateTimeOffset.UtcNow;
				}

				buffer.Add(item);

				if (buffer.Count >= maxItems)
				{
					return;
				}
			}
		}

		private async Task FlushAsync(CancellationToken cancellationToken)
		{
			if (buffer.Count == 0)
			{
				return;
			}

			var createdUtc = batchCreatedUtc ?? DateTimeOffset.UtcNow;
			var flushedUtc = DateTimeOffset.UtcNow;
			var items = buffer.ToArray();

			buffer.Clear();
			batchCreatedUtc = null;

			var batch = new BatchEnvelope<TPayload>
			{
				BatchId = Guid.NewGuid(),
				PartitionId = partitionId,
				Items = items,
				CreatedUtc = createdUtc,
				FlushedUtc = flushedUtc,
			};

			metrics.IncrementInFlight();
			var started = Stopwatch.GetTimestamp();
			try
			{
				await output.WriteAsync(batch, cancellationToken).ConfigureAwait(false);
				metrics.RecordOut(batch);
			}
			finally
			{
				var elapsed = Stopwatch.GetElapsedTime(started);
				metrics.RecordDuration(elapsed);
				metrics.DecrementInFlight();
			}
		}
	}
}
