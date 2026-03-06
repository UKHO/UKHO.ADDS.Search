using System.Diagnostics;
using System.Threading.Channels;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
	public abstract class SourceNodeBase<TOut> : INode
	{
		private readonly ChannelWriter<TOut> output;
		private readonly Action<string>? log;
		private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
		private readonly NodeMetrics metrics;
		private Task? completion;

		protected SourceNodeBase(
			string name,
			ChannelWriter<TOut> output,
			Action<string>? log = null,
			IPipelineFatalErrorReporter? fatalErrorReporter = null)
		{
			Name = name;
			this.output = output;
			this.log = log;
			this.fatalErrorReporter = fatalErrorReporter;
			metrics = new NodeMetrics(name);
		}

		protected ValueTask WriteAsync(ChannelWriter<TOut> output, TOut item, CancellationToken cancellationToken)
		{
			return WriteCoreAsync(output, item, cancellationToken);
		}

		private async ValueTask WriteCoreAsync(ChannelWriter<TOut> output, TOut item, CancellationToken cancellationToken)
		{
			await output.WriteAsync(item, cancellationToken).ConfigureAwait(false);
			metrics.RecordOut(item);
		}

		public string Name { get; }

		public Task Completion => completion ?? Task.CompletedTask;

		public Task StartAsync(CancellationToken cancellationToken)
		{
			completion ??= Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
			return Task.CompletedTask;
		}

		public virtual ValueTask StopAsync(CancellationToken cancellationToken)
		{
			return ValueTask.CompletedTask;
		}

		protected abstract ValueTask ProduceAsync(ChannelWriter<TOut> output, CancellationToken cancellationToken);

		private async Task RunAsync(CancellationToken cancellationToken)
		{
			try
			{
				metrics.IncrementInFlight();
				var started = Stopwatch.GetTimestamp();
				try
				{
					await ProduceAsync(output, cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					var elapsed = Stopwatch.GetElapsedTime(started);
					metrics.RecordDuration(elapsed);
					metrics.DecrementInFlight();
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
	}
}
