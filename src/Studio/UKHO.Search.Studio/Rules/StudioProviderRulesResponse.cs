namespace UKHO.Search.Studio.Rules
{
    /// <summary>
    /// Represents the rules currently exposed for a single provider.
    /// </summary>
    public sealed class StudioProviderRulesResponse
    {
        /// <summary>
        /// Gets the stable provider name that owns the returned rules.
        /// </summary>
        public required string ProviderName { get; init; }

        /// <summary>
        /// Gets the human-readable provider display name.
        /// </summary>
        public required string DisplayName { get; init; }

        /// <summary>
        /// Gets the optional provider description shown alongside the rule list.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets the rules currently available for the provider.
        /// </summary>
        public required IReadOnlyList<StudioRuleSummaryResponse> Rules { get; init; }
    }
}
