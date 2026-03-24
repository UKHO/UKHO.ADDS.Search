namespace UKHO.Search.Studio.Rules
{
    /// <summary>
    /// Represents the full provider-grouped rule discovery payload returned to Studio clients.
    /// </summary>
    public sealed class StudioRuleDiscoveryResponse
    {
        /// <summary>
        /// Gets the rules schema version used to interpret the returned payload.
        /// </summary>
        public required string SchemaVersion { get; init; }

        /// <summary>
        /// Gets the providers and the rules currently exposed for each provider.
        /// </summary>
        public required IReadOnlyList<StudioProviderRulesResponse> Providers { get; init; }
    }
}
