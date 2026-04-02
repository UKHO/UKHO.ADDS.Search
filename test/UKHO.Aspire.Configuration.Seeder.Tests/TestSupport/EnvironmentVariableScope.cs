namespace UKHO.Aspire.Configuration.Seeder.Tests.TestSupport
{
    /// <summary>
    /// Applies a temporary process-level environment variable override and restores the original value when disposed.
    /// </summary>
    internal sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _originalValue;

        /// <summary>
        /// Initializes a scope that updates one environment variable for the duration of a test.
        /// </summary>
        /// <param name="name">The environment variable name to override.</param>
        /// <param name="value">The temporary value to apply. A <see langword="null"/> value clears the variable.</param>
        public EnvironmentVariableScope(string name, string? value)
        {
            // Capture the original value first so the process state can be restored after the assertion completes.
            _name = name;
            _originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        /// <summary>
        /// Restores the original environment variable value captured when the scope was created.
        /// </summary>
        public void Dispose()
        {
            // Always restore the prior process state so one parser test cannot influence another.
            Environment.SetEnvironmentVariable(_name, _originalValue);
        }
    }
}
