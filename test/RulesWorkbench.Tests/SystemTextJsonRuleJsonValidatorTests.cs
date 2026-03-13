using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
	public class SystemTextJsonRuleJsonValidatorTests
	{
		[Fact]
		public void Validate_WhenJsonInvalid_ReturnsInvalid()
		{
			var validator = new SystemTextJsonRuleJsonValidator();

			var result = validator.Validate("{");

			result.IsValid.ShouldBeFalse();
			result.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
		}

		[Fact]
		public void Validate_WhenJsonValid_ReturnsValid()
		{
			var validator = new SystemTextJsonRuleJsonValidator();

			var result = validator.Validate("{\"id\":\"x\"}");

			result.IsValid.ShouldBeTrue();
			result.ErrorMessage.ShouldBeNull();
		}
	}
}
