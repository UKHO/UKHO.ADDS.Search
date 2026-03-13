using System.Text.Json.Nodes;

namespace RulesWorkbench.Services
{
	public sealed record FileShareRulesSnapshot(
		bool IsLoaded,
		JsonNode? Root,
		JsonArray? FileShareRules,
		RulesSnapshotError? Error)
	{
		public int RuleCount => FileShareRules?.Count ?? 0;

		public static FileShareRulesSnapshot Loaded(JsonNode root, JsonArray fileShareRules)
		{
			return new FileShareRulesSnapshot(true, root, fileShareRules, null);
		}

		public static FileShareRulesSnapshot Failed(RulesSnapshotError error)
		{
			return new FileShareRulesSnapshot(false, null, null, error);
		}
	}
}
