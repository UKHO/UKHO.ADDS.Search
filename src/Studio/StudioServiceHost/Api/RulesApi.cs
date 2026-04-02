using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.ProviderModel;
using UKHO.Search.Studio.Rules;

namespace StudioServiceHost.Api
{
    /// <summary>
    /// Defines the API surface for exposing the currently loaded Studio rule catalog.
    /// </summary>
    public static class RulesApi
    {
        /// <summary>
        /// Maps the rule discovery endpoint onto the supplied endpoint builder.
        /// </summary>
        /// <param name="endpoints">The endpoint builder that receives the rule discovery endpoint.</param>
        /// <returns>The same <paramref name="endpoints"/> instance so endpoint configuration can continue fluently.</returns>
        public static IEndpointRouteBuilder MapRulesApi(this IEndpointRouteBuilder endpoints)
        {
            // Guard the extension entry point because the host must provide a valid route builder.
            ArgumentNullException.ThrowIfNull(endpoints);

            // Expose the Studio rule discovery endpoint without changing its established route.
            endpoints.MapGet("/rules", GetRules)
                     .WithName("GetRules");

            return endpoints;
        }

        /// <summary>
        /// Projects the loaded provider rules into the Studio rule discovery response shape.
        /// </summary>
        /// <param name="providerCatalog">The provider catalog used to enumerate all known providers.</param>
        /// <param name="rulesReader">The rules reader that exposes the current in-memory rule snapshot.</param>
        /// <returns>The Studio rule discovery payload consumed by the shell.</returns>
        private static IResult GetRules(IProviderCatalog providerCatalog, IProviderRulesReader rulesReader)
        {
            // Capture the current rules snapshot once so the response is built from a consistent view.
            var snapshot = rulesReader.GetSnapshot();
            var response = new StudioRuleDiscoveryResponse
            {
                SchemaVersion = snapshot.SchemaVersion,
                Providers = providerCatalog.GetAllProviders()
                                           .Select(provider =>
                                           {
                                               // Try to read the provider's rules from the snapshot, allowing providers with no rules to return an empty list.
                                               snapshot.RulesByProvider.TryGetValue(provider.Name, out var rules);

                                               // Project provider metadata and canonical rule definitions into the Studio response shape.
                                               return new StudioProviderRulesResponse
                                               {
                                                   ProviderName = provider.Name,
                                                   DisplayName = provider.DisplayName,
                                                   Description = provider.Description,
                                                   Rules = (rules ?? Array.Empty<ProviderRuleDefinition>())
                                                       .Select(rule => new StudioRuleSummaryResponse
                                                       {
                                                           Id = rule.Id,
                                                           Context = rule.Context,
                                                           Title = rule.Title,
                                                           Description = rule.Description,
                                                           Enabled = rule.Enabled
                                                       })
                                                       .ToArray()
                                               };
                                           })
                                           .ToArray()
            };

            return TypedResults.Ok(response);
        }
    }
}