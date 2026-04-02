using UKHO.Search.ProviderModel;

namespace StudioServiceHost.Api
{
    /// <summary>
    /// Defines the API surface for exposing registered provider metadata.
    /// </summary>
    public static class ProvidersApi
    {
        /// <summary>
        /// Maps the provider discovery endpoint onto the supplied endpoint builder.
        /// </summary>
        /// <param name="endpoints">The endpoint builder that receives the provider discovery endpoint.</param>
        /// <returns>The same <paramref name="endpoints"/> instance so endpoint configuration can continue fluently.</returns>
        public static IEndpointRouteBuilder MapProvidersApi(this IEndpointRouteBuilder endpoints)
        {
            // Guard the extension entry point because the host must provide a valid route builder.
            ArgumentNullException.ThrowIfNull(endpoints);

            // Expose provider metadata directly from the shared provider catalog.
            endpoints.MapGet("/providers", GetProviders)
                     .WithName("GetProviders");

            return endpoints;
        }

        /// <summary>
        /// Loads the registered provider metadata snapshot.
        /// </summary>
        /// <param name="providerCatalog">The shared catalog that contains the registered provider descriptors.</param>
        /// <returns>The full provider metadata snapshot used by Studio clients.</returns>
        private static IResult GetProviders(IProviderCatalog providerCatalog)
        {
            // Return the full provider metadata snapshot so Studio clients can populate provider selections.
            return TypedResults.Ok(providerCatalog.GetAllProviders());
        }
    }
}