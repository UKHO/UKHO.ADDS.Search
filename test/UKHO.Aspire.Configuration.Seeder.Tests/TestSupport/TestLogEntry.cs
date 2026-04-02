using Microsoft.Extensions.Logging;

namespace UKHO.Aspire.Configuration.Seeder.Tests.TestSupport
{
    /// <summary>
    /// Stores one captured log entry so tests can assert level, message, and exception details without using an external mocking package.
    /// </summary>
    internal sealed record TestLogEntry
    {
        /// <summary>
        /// Gets or sets the log level captured for the entry.
        /// </summary>
        public LogLevel LogLevel { get; init; }

        /// <summary>
        /// Gets or sets the event identifier captured for the entry.
        /// </summary>
        public EventId EventId { get; init; }

        /// <summary>
        /// Gets or sets the rendered log message captured from the logger callback.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the exception captured for the entry when one was supplied.
        /// </summary>
        public Exception? Exception { get; init; }
    }
}
