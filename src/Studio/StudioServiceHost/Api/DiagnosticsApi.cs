namespace StudioServiceHost.Api
{
    /// <summary>
    /// Defines lightweight diagnostics endpoints for the Studio service host.
    /// </summary>
    public static class DiagnosticsApi
    {
        /// <summary>
        /// Maps the lightweight diagnostics endpoints onto the supplied endpoint builder.
        /// </summary>
        /// <param name="endpoints">The endpoint builder that receives the diagnostics endpoints.</param>
        /// <returns>The same <paramref name="endpoints"/> instance so endpoint configuration can continue fluently.</returns>
        public static IEndpointRouteBuilder MapDiagnosticsApi(this IEndpointRouteBuilder endpoints)
        {
            // Guard the extension entry point because the host must provide a valid route builder.
            ArgumentNullException.ThrowIfNull(endpoints);

            // Keep a lightweight echo endpoint for host smoke tests and simple availability checks.
            endpoints.MapGet("/echo", GetEcho)
                     .WithName("GetEcho");

            return endpoints;
        }

        /// <summary>
        /// Returns the lightweight host echo payload used by smoke tests and availability checks.
        /// </summary>
        /// <returns>The host echo payload.</returns>
        private static IResult GetEcho()
        {
            // Return the renamed host identifier while keeping the existing route unchanged.
            return TypedResults.Text("Hello from StudioServiceHost echo.");
        }
    }
}