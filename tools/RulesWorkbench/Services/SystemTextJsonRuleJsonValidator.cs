using System.Text.Json;

namespace RulesWorkbench.Services
{
	public sealed class SystemTextJsonRuleJsonValidator : IRuleJsonValidator
	{
		public RuleJsonValidationResult Validate(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return RuleJsonValidationResult.Invalid("JSON is empty.");
			}

			try
			{
				JsonDocument.Parse(json);
				return RuleJsonValidationResult.Valid();
			}
			catch (JsonException ex)
			{
				return RuleJsonValidationResult.Invalid(ex.Message);
			}
		}
	}
}
