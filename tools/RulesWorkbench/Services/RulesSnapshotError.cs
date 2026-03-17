namespace RulesWorkbench.Services
{
	public sealed record RulesSnapshotError(
		string FileName,
		string Message,
		string? Details = null);
}
