using System.Text.Json;

namespace RulesWorkbench.Services
{
	public interface IRuleJsonValidator
	{
		RuleJsonValidationResult Validate(string json);
	}
}
