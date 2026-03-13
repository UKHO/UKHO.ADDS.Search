namespace RulesWorkbench.Services
{
	public sealed record RuleJsonValidationResult(
		bool IsValid,
		string? ErrorMessage)
	{
		public static RuleJsonValidationResult Valid()
		{
			return new RuleJsonValidationResult(true, null);
		}

		public static RuleJsonValidationResult Invalid(string message)
		{
			return new RuleJsonValidationResult(false, message);
		}
	}
}
