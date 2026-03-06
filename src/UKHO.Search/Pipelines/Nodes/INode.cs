namespace UKHO.Search.Pipelines.Nodes
{
	public interface INode
	{
		string Name { get; }

		Task StartAsync(CancellationToken cancellationToken);

		Task Completion { get; }

		ValueTask StopAsync(CancellationToken cancellationToken);
	}
}
