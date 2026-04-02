namespace UKHO.Workbench.Output
{
    /// <summary>
    /// Represents one immutable entry in the shell-wide Workbench output stream.
    /// </summary>
    public sealed record OutputEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutputEntry"/> type.
        /// </summary>
        /// <param name="id">The stable identifier used to distinguish this entry from every other entry in the current session stream.</param>
        /// <param name="timestampUtc">The UTC timestamp recorded when the entry was created.</param>
        /// <param name="level">The severity or intent level assigned to the entry.</param>
        /// <param name="source">The subsystem, tool, or shell area that emitted the entry.</param>
        /// <param name="summary">The compact summary text shown in the chronological output stream.</param>
        /// <param name="details">Optional longer diagnostic detail associated with the summary.</param>
        /// <param name="eventCode">Optional stable event code that callers can use to correlate repeated messages.</param>
        public OutputEntry(
            string id,
            DateTimeOffset timestampUtc,
            OutputLevel level,
            string source,
            string summary,
            string? details = null,
            string? eventCode = null)
        {
            // Entry identity, source metadata, and summary text are mandatory so every rendered row is attributable and self-describing.
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(source);
            ArgumentException.ThrowIfNullOrWhiteSpace(summary);

            Id = id;
            TimestampUtc = timestampUtc;
            Level = level;
            Source = source;
            Summary = summary;
            Details = details;
            EventCode = eventCode;
        }

        /// <summary>
        /// Gets the stable identifier for the entry within the current Workbench session.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the UTC timestamp recorded for the entry.
        /// </summary>
        public DateTimeOffset TimestampUtc { get; }

        /// <summary>
        /// Gets the severity or intent level assigned to the entry.
        /// </summary>
        public OutputLevel Level { get; }

        /// <summary>
        /// Gets the subsystem, tool, or shell area that emitted the entry.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Gets the compact summary text rendered in the output stream.
        /// </summary>
        public string Summary { get; }

        /// <summary>
        /// Gets the optional longer diagnostic detail associated with the entry.
        /// </summary>
        public string? Details { get; }

        /// <summary>
        /// Gets the optional stable event code associated with the entry.
        /// </summary>
        public string? EventCode { get; }
    }
}
