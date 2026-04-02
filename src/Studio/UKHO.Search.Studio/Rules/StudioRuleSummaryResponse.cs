namespace UKHO.Search.Studio.Rules
{
    /// <summary>
    /// Represents the high-level details that Studio surfaces for a single ingestion rule.
    /// </summary>
    public sealed class StudioRuleSummaryResponse
    {
        /// <summary>
        /// Gets the unique identifier for the rule.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the optional provider-neutral context that scopes the rule.
        /// </summary>
        public string? Context { get; init; }

        /// <summary>
        /// Gets the optional human-readable rule title.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Gets the optional rule description.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets a value indicating whether the rule is currently enabled.
        /// </summary>
        public required bool Enabled { get; init; }
    }
}
