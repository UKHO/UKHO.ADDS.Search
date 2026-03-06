using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.Batching
{
	public sealed class BatchEnvelope<TPayload>
	{
		public required Guid BatchId { get; init; }

		public required int PartitionId { get; init; }

		public required IReadOnlyList<Envelope<TPayload>> Items { get; init; }

		public required DateTimeOffset CreatedUtc { get; init; }

		public required DateTimeOffset FlushedUtc { get; init; }
	}
}
