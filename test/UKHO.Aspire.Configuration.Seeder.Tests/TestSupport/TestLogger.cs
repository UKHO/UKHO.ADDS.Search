using Microsoft.Extensions.Logging;

namespace UKHO.Aspire.Configuration.Seeder.Tests.TestSupport
{
    /// <summary>
    /// Captures log entries in memory so tests can verify warning and error paths without introducing a mocking dependency.
    /// </summary>
    /// <typeparam name="TCategoryName">The logger category type requested by the production code.</typeparam>
    internal sealed class TestLogger<TCategoryName> : ILogger<TCategoryName>
    {
        private readonly List<TestLogEntry> _entries = [];

        /// <summary>
        /// Gets the captured entries in the order they were written by the code under test.
        /// </summary>
        public IReadOnlyList<TestLogEntry> Entries => _entries;

        /// <summary>
        /// Starts a logging scope.
        /// </summary>
        /// <typeparam name="TState">The arbitrary scope state supplied by the caller.</typeparam>
        /// <param name="state">The scope state. The test logger does not persist it because the covered code does not rely on scopes.</param>
        /// <returns>A disposable scope token. The test implementation returns a null object because no scoped behaviour is required.</returns>
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            // The seeder tests only need message capture, so scope tracking is intentionally omitted.
            return null!;
        }

        /// <summary>
        /// Determines whether the supplied level should be captured.
        /// </summary>
        /// <param name="logLevel">The level being evaluated.</param>
        /// <returns>Always <see langword="true"/> so tests observe every emitted entry.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            // Capture all levels so callers can assert both verbose and warning/error paths if needed.
            return true;
        }

        /// <summary>
        /// Records one log entry using the provided formatter.
        /// </summary>
        /// <typeparam name="TState">The state type supplied by the logging pipeline.</typeparam>
        /// <param name="logLevel">The severity level for the entry.</param>
        /// <param name="eventId">The structured event identifier.</param>
        /// <param name="state">The state payload emitted by the caller.</param>
        /// <param name="exception">The optional exception associated with the log entry.</param>
        /// <param name="formatter">The formatter used to produce the final message string.</param>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // Render the message exactly as the caller would so assertions match the visible log output.
            _entries.Add(new TestLogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                Message = formatter(state, exception),
                Exception = exception
            });
        }
    }
}
