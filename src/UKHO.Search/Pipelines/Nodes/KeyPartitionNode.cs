using System.Diagnostics;
using System.Threading.Channels;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
	public sealed class KeyPartitionNode<TPayload> : INode
	{
		private readonly ChannelReader<Envelope<TPayload>> input;
		private readonly IReadOnlyList<ChannelWriter<Envelope<TPayload>>> outputs;
		private readonly Action<string>? log;
		private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
		private readonly NodeMetrics metrics;
		private Task? completion;

		public KeyPartitionNode(
			string name,
			ChannelReader<Envelope<TPayload>> input,
			IReadOnlyList<ChannelWriter<Envelope<TPayload>>> outputs,
			Action<string>? log = null,
			IPipelineFatalErrorReporter? fatalErrorReporter = null)
		{
			if (outputs.Count <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(outputs));
			}

			Name = name;
			this.input = input;
			this.outputs = outputs;
			this.log = log;
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
				while (await input.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
				{
					while (input.TryRead(out var item))
					{
						metrics.RecordIn(item);
						metrics.IncrementInFlight();
						var started = Stopwatch.GetTimestamp();
						try
						{
							item.Context.AddBreadcrumb(Name);

							var partition = GetPartition(item.Key, outputs.Count);
							await outputs[partition].WriteAsync(item, cancellationToken).ConfigureAwait(false);
							metrics.RecordOut(item);
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

				CompleteOutputs();
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				CompleteOutputs();
			}
			catch (Exception ex)
			{
				log?.Invoke($"Node '{Name}' failed: {ex.GetType().Name}: {ex.Message}");
				CompleteOutputs(ex);
				fatalErrorReporter?.ReportFatal(Name, ex);
				throw;
			}
			finally
			{
				metrics.Dispose();
			}
		}

		private void CompleteOutputs(Exception? error = null)
		{
			foreach (var output in outputs)
			{
				output.TryComplete(error);
			}
		}

		private static int GetPartition(string key, int partitions)
		{
			// Stable, deterministic 32-bit FNV-1a hash.
			unchecked
			{
				uint hash = 2166136261;
				for (var i = 0; i < key.Length; i++)
				{
					hash ^= key[i];
					hash *= 16777619;
				}

				return (int)(hash % (uint)partitions);
			}
		}
	}
}
