using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
	public sealed class DeadLetterSinkNode<TPayload> : SinkNodeBase<Envelope<TPayload>>
	{
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new(StringComparer.OrdinalIgnoreCase);

		private readonly string filePath;
		private readonly bool fatalIfCannotPersist;
		private readonly Action<string>? log;
		private int persistedCount;

		public DeadLetterSinkNode(
			string name,
			ChannelReader<Envelope<TPayload>> input,
			string filePath,
			bool fatalIfCannotPersist = false,
			Action<string>? log = null,
			IPipelineFatalErrorReporter? fatalErrorReporter = null)
			: base(name, input, log, fatalErrorReporter)
		{
			this.filePath = filePath;
			this.fatalIfCannotPersist = fatalIfCannotPersist;
			this.log = log;
		}

		public int PersistedCount => persistedCount;

		protected override async ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
		{
			try
			{
				var record = new
				{
					DeadLetteredAtUtc = DateTimeOffset.UtcNow,
					NodeName = Name,
					Envelope = item,
					Error = item.Error,
				};

				var json = JsonSerializer.Serialize(record);
				await AppendLineAsync(json, cancellationToken).ConfigureAwait(false);
				Interlocked.Increment(ref persistedCount);
			}
			catch (Exception ex)
			{
				log?.Invoke(
					$"Dead-letter persist failed in '{Name}' for MessageId={item.MessageId} Key='{item.Key}' ErrorCode='{item.Error?.Code}': {ex.GetType().Name}: {ex.Message}");

				if (fatalIfCannotPersist)
				{
					throw;
				}
			}
		}

		private async Task AppendLineAsync(string line, CancellationToken cancellationToken)
		{
			var fileLock = FileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
			await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
			var directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			await using var stream = new FileStream(
				filePath,
				FileMode.Append,
				FileAccess.Write,
				FileShare.ReadWrite,
				bufferSize: 4096,
				useAsync: true);

			await using var writer = new StreamWriter(stream, Encoding.UTF8);
			await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				fileLock.Release();
			}
		}
	}
}
