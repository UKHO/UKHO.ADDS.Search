using Radzen;
using UKHO.Workbench.Output;

namespace WorkbenchHost.Services
{
    /// <summary>
    /// Stores startup notifications until the interactive Workbench shell is ready to present them to the user.
    /// </summary>
    public class WorkbenchStartupNotificationStore
    {
        private readonly List<WorkbenchStartupNotification> _notifications = [];
        private readonly List<OutputEntry> _outputEntries = [];

        /// <summary>
        /// Adds a new startup notification to the pending collection.
        /// </summary>
        /// <param name="severity">The Radzen severity that determines how the notification should be presented.</param>
        /// <param name="summary">The short summary shown to the user.</param>
        /// <param name="detail">The longer safe detail shown to the user.</param>
        public void Add(NotificationSeverity severity, string summary, string detail)
        {
            // Notifications are buffered during startup because the interactive shell is not yet available to render them immediately.
            _notifications.Add(new WorkbenchStartupNotification(severity, summary, detail));
        }

        /// <summary>
        /// Adds a startup output entry that should be replayed into the shared Workbench output stream after the host service provider is ready.
        /// </summary>
        /// <param name="level">The output severity or intent level assigned to the buffered entry.</param>
        /// <param name="source">The subsystem that produced the startup output.</param>
        /// <param name="summary">The compact summary text that should be rendered in the output panel.</param>
        /// <param name="details">Optional longer diagnostic detail that should accompany the summary.</param>
        /// <param name="eventCode">Optional stable event code that callers can use to correlate repeated messages.</param>
        public void AddOutput(OutputLevel level, string source, string summary, string? details = null, string? eventCode = null)
        {
            // Startup output is buffered separately from toast notifications because module discovery runs before the shell-owned output service is resolved.
            ArgumentException.ThrowIfNullOrWhiteSpace(source);
            ArgumentException.ThrowIfNullOrWhiteSpace(summary);

            _outputEntries.Add(
                new OutputEntry(
                    Guid.NewGuid().ToString("N"),
                    DateTimeOffset.UtcNow,
                    level,
                    source,
                    summary,
                    details,
                    eventCode));
        }

        /// <summary>
        /// Returns the pending notifications and clears the internal store.
        /// </summary>
        /// <returns>The startup notifications that have not yet been presented to the user.</returns>
        public IReadOnlyList<WorkbenchStartupNotification> DequeueAll()
        {
            // The first shell render drains the queue so the same startup failure is not shown repeatedly.
            var notifications = _notifications.ToArray();
            _notifications.Clear();
            return notifications;
        }

        /// <summary>
        /// Returns the buffered startup output entries and clears the internal collection.
        /// </summary>
        /// <returns>The startup output entries that have not yet been replayed into the shared output stream.</returns>
        public IReadOnlyList<OutputEntry> DequeueOutputEntries()
        {
            // Startup output is drained once during host bootstrap so historical startup traces appear only once in the shell-wide stream.
            var outputEntries = _outputEntries.ToArray();
            _outputEntries.Clear();
            return outputEntries;
        }
    }
}
