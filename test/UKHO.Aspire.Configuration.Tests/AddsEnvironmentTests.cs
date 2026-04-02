using Shouldly;
using Xunit;

namespace UKHO.Aspire.Configuration.Tests
{
    /// <summary>
    /// Verifies the parsing, equality, and environment-variable entry points exposed by <see cref="AddsEnvironment"/>.
    /// </summary>
    public sealed class AddsEnvironmentTests
    {
        /// <summary>
        /// Verifies that <see cref="AddsEnvironment.TryParse(string?, out AddsEnvironment?)"/> recognises known values regardless of casing.
        /// </summary>
        [Fact]
        public void TryParse_WhenValueMatchesKnownEnvironment_ShouldReturnExpectedInstance()
        {
            // Use a mixed-case value to prove the lookup is intentionally case-insensitive.
            var parsed = AddsEnvironment.TryParse("LoCaL", out var environment);

            // The parser should succeed and return the canonical singleton instance.
            parsed.ShouldBeTrue();
            environment.ShouldBeSameAs(AddsEnvironment.Local);
        }

        /// <summary>
        /// Verifies that <see cref="AddsEnvironment.TryParse(string?, out AddsEnvironment?)"/> rejects unknown values without creating a result.
        /// </summary>
        [Fact]
        public void TryParse_WhenValueUnknown_ShouldReturnFalseAndNullResult()
        {
            // Provide a value that is not part of the known environment map.
            var parsed = AddsEnvironment.TryParse("qa", out var environment);

            // The parser should fail cleanly and leave the output unset.
            parsed.ShouldBeFalse();
            environment.ShouldBeNull();
        }

        /// <summary>
        /// Verifies that <see cref="AddsEnvironment.Parse(string)"/> returns the expected singleton for a valid value.
        /// </summary>
        [Fact]
        public void Parse_WhenValueValid_ShouldReturnExpectedEnvironment()
        {
            // Parse a known value through the exception-throwing API.
            var environment = AddsEnvironment.Parse("dev");

            // The result should reuse the pre-defined development instance.
            environment.ShouldBeSameAs(AddsEnvironment.Development);
        }

        /// <summary>
        /// Verifies that <see cref="AddsEnvironment.Parse(string)"/> surfaces invalid inputs as an argument error.
        /// </summary>
        [Fact]
        public void Parse_WhenValueInvalid_ShouldThrowArgumentException()
        {
            // Invoke the strict parser with an unsupported value.
            var exception = Should.Throw<ArgumentException>(() => AddsEnvironment.Parse("unsupported"));

            // The exception should identify the invalid ADDS environment value.
            exception.ParamName.ShouldBe("input");
            exception.Message.ShouldContain("Invalid AddsEnvironment");
        }

        /// <summary>
        /// Verifies that equality, hash-code generation, and the equality operators are all aligned for equivalent values.
        /// </summary>
        [Fact]
        public void EqualityMembers_WhenValuesMatch_ShouldTreatInstancesAsEquivalent()
        {
            // Parse an equivalent value so the test exercises value semantics rather than reference equality alone.
            var parsed = AddsEnvironment.Parse("LOCAL");

            // Every equality entry point should agree on the same outcome.
            parsed.Equals(AddsEnvironment.Local).ShouldBeTrue();
            (parsed == AddsEnvironment.Local).ShouldBeTrue();
            (parsed != AddsEnvironment.Local).ShouldBeFalse();
            parsed.GetHashCode().ShouldBe(AddsEnvironment.Local.GetHashCode());
        }

        /// <summary>
        /// Verifies the convenience helpers and string conversion exposed by the known environment instances.
        /// </summary>
        [Fact]
        public void HelperMembers_WhenUsingKnownInstances_ShouldReportExpectedFlagsAndValue()
        {
            // Exercise both convenience predicates so the local and development checks stay protected.
            AddsEnvironment.Local.IsLocal().ShouldBeTrue();
            AddsEnvironment.Local.IsDev().ShouldBeFalse();
            AddsEnvironment.Development.IsDev().ShouldBeTrue();
            AddsEnvironment.Development.IsLocal().ShouldBeFalse();

            // The string representation should remain the stored environment token.
            AddsEnvironment.PreProd.ToString().ShouldBe("prp");
        }

        /// <summary>
        /// Verifies that <see cref="AddsEnvironment.GetEnvironment"/> reads the configured environment variable and returns the matching singleton.
        /// </summary>
        [Fact]
        public void GetEnvironment_WhenVariableContainsKnownValue_ShouldReturnParsedEnvironment()
        {
            // Temporarily set the expected environment variable to a valid value.
            var environment = WithEnvironmentVariable(
                WellKnownConfigurationName.AddsEnvironmentName,
                "live",
                AddsEnvironment.GetEnvironment);

            // The helper should round-trip to the canonical live environment instance.
            environment.ShouldBeSameAs(AddsEnvironment.Live);
        }

        /// <summary>
        /// Verifies that <see cref="AddsEnvironment.GetEnvironment"/> fails with a clear error when the variable is missing.
        /// </summary>
        [Fact]
        public void GetEnvironment_WhenVariableMissing_ShouldThrowInvalidOperationException()
        {
            // Clear the environment variable so the runtime path behaves like an unconfigured caller.
            var exception = WithEnvironmentVariable(
                WellKnownConfigurationName.AddsEnvironmentName,
                null,
                () => Should.Throw<InvalidOperationException>(AddsEnvironment.GetEnvironment));

            // The failure message should explain which variable is required.
            exception.Message.ShouldContain(WellKnownConfigurationName.AddsEnvironmentName);
        }

        /// <summary>
        /// Verifies that <see cref="AddsEnvironment.GetEnvironment"/> also rejects invalid environment values.
        /// </summary>
        [Fact]
        public void GetEnvironment_WhenVariableInvalid_ShouldThrowInvalidOperationException()
        {
            // Set an unsupported value to protect the validation path as well as the missing-value path.
            var exception = WithEnvironmentVariable(
                WellKnownConfigurationName.AddsEnvironmentName,
                "invalid",
                () => Should.Throw<InvalidOperationException>(AddsEnvironment.GetEnvironment));

            // The shared validation message should still point callers to the expected variable.
            exception.Message.ShouldContain(WellKnownConfigurationName.AddsEnvironmentName);
        }

        /// <summary>
        /// Temporarily overrides a single environment variable for the duration of a callback.
        /// </summary>
        /// <typeparam name="TResult">The result type returned by the callback.</typeparam>
        /// <param name="name">The environment variable name to override.</param>
        /// <param name="value">The temporary value to apply while the callback runs.</param>
        /// <param name="action">The callback that should execute under the temporary environment.</param>
        /// <returns>The value returned by <paramref name="action"/>.</returns>
        private static TResult WithEnvironmentVariable<TResult>(string name, string? value, Func<TResult> action)
        {
            // Capture the existing process-level value so the test can restore the machine state afterward.
            var originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);

            try
            {
                // Execute the caller's test logic while the override is active.
                return action();
            }
            finally
            {
                // Restore the original state so one test cannot leak into another test run.
                Environment.SetEnvironmentVariable(name, originalValue);
            }
        }
    }
}
