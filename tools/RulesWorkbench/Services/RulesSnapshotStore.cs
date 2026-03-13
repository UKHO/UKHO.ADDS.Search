using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace RulesWorkbench.Services
{
	public sealed class RulesSnapshotStore
	{
		private readonly ILogger<RulesSnapshotStore> _logger;
		private readonly IFileProvider _fileProvider;
		private readonly IRuleJsonValidator _ruleJsonValidator;

		private readonly object _gate = new();
		private FileShareRulesSnapshot? _snapshot;

		public RulesSnapshotStore(
            ILogger<RulesSnapshotStore> logger,
			IWebHostEnvironment environment,
			IRuleJsonValidator ruleJsonValidator)
		{
       _logger = logger;
		_fileProvider = environment.ContentRootFileProvider;
		_ruleJsonValidator = ruleJsonValidator;
		}

		public IReadOnlyList<RuleSummary> GetFileShareRuleSummaries(string? query)
		{
			var snapshot = LoadFileShareRules();
			if (!snapshot.IsLoaded || snapshot.FileShareRules is null)
			{
				return Array.Empty<RuleSummary>();
			}

			var result = new List<RuleSummary>(snapshot.FileShareRules.Count);
			var trimmedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
			var queryLower = trimmedQuery?.ToLowerInvariant();

			for (var i = 0; i < snapshot.FileShareRules.Count; i++)
			{
				var ruleNode = snapshot.FileShareRules[i];
				if (ruleNode is null)
				{
					continue;
				}

				var id = ruleNode["id"]?.GetValue<string?>();
				var description = ruleNode["description"]?.GetValue<string?>();

				if (queryLower is not null)
				{
					var matches = false;
					if (!string.IsNullOrWhiteSpace(id) && id!.ToLowerInvariant().Contains(queryLower, StringComparison.Ordinal))
					{
						matches = true;
					}
					else if (!string.IsNullOrWhiteSpace(description) && description!.ToLowerInvariant().Contains(queryLower, StringComparison.Ordinal))
					{
						matches = true;
					}

					if (!matches)
					{
						continue;
					}
				}

				result.Add(new RuleSummary(i, id, description, ruleNode));
			}

			return result;
		}

		public RuleJsonValidationResult UpdateFileShareRuleJson(int ruleIndex, string json)
		{
			var validation = _ruleJsonValidator.Validate(json);
			if (!validation.IsValid)
			{
				return validation;
			}

			JsonNode? node;
			try
			{
				node = JsonNode.Parse(json);
			}
			catch (JsonException ex)
			{
				return RuleJsonValidationResult.Invalid(ex.Message);
			}

			if (node is null)
			{
				return RuleJsonValidationResult.Invalid("JSON is empty or invalid.");
			}

			var snapshot = LoadFileShareRules();
			if (!snapshot.IsLoaded || snapshot.FileShareRules is null)
			{
				return RuleJsonValidationResult.Invalid("Rules snapshot is not loaded.");
			}

			lock (_gate)
			{
				if (_snapshot?.FileShareRules is null)
				{
					return RuleJsonValidationResult.Invalid("Rules snapshot is not loaded.");
				}

				if (ruleIndex < 0 || ruleIndex >= _snapshot.FileShareRules.Count)
				{
					return RuleJsonValidationResult.Invalid("Rule index out of range.");
				}

				_snapshot.FileShareRules[ruleIndex] = node;
				_logger.LogInformation("Updated in-memory file-share rule at index {RuleIndex}", ruleIndex);
				return RuleJsonValidationResult.Valid();
			}
		}

		public FileShareRulesSnapshot LoadFileShareRules()
		{
			lock (_gate)
			{
				if (_snapshot is not null)
				{
					return _snapshot;
				}

				_snapshot = LoadFileShareRulesInternal();
				return _snapshot;
			}
		}

		private FileShareRulesSnapshot LoadFileShareRulesInternal()
		{
			const string rulesFileName = "ingestion-rules.json";

			var fileInfo = _fileProvider.GetFileInfo(rulesFileName);
			if (!fileInfo.Exists)
			{
				_logger.LogError("Rules snapshot file not found: {FileName}", rulesFileName);
				return FileShareRulesSnapshot.Failed(new RulesSnapshotError(
					rulesFileName,
					"Rules snapshot file not found."));
			}

			try
			{
				using var stream = fileInfo.CreateReadStream();
				using var document = JsonDocument.Parse(stream);
				var rootNode = JsonNode.Parse(document.RootElement.GetRawText());
				if (rootNode is null)
				{
					return FileShareRulesSnapshot.Failed(new RulesSnapshotError(
						rulesFileName,
						"Rules snapshot file is empty or invalid JSON."));
				}

				var fileShareRules = rootNode["rules"]?["file-share"] as JsonArray;
				if (fileShareRules is null)
				{
					return FileShareRulesSnapshot.Failed(new RulesSnapshotError(
						rulesFileName,
						"Expected JSON path 'rules.file-share' to be an array."));
				}

				_logger.LogInformation("Loaded rules snapshot {FileName}; file-share rules: {RulesCount}", rulesFileName, fileShareRules.Count);
				return FileShareRulesSnapshot.Loaded(rootNode, fileShareRules);
			}
			catch (JsonException ex)
			{
				_logger.LogError(ex, "Failed to parse rules snapshot JSON: {FileName}", rulesFileName);
				return FileShareRulesSnapshot.Failed(new RulesSnapshotError(
					rulesFileName,
					"Invalid JSON in rules snapshot file.",
					ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load rules snapshot: {FileName}", rulesFileName);
				return FileShareRulesSnapshot.Failed(new RulesSnapshotError(
					rulesFileName,
					"Failed to load rules snapshot file.",
					ex.Message));
			}
		}
	}
}
