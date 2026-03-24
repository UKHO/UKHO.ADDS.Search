using Scalar.AspNetCore;
using StudioApiHost.Api;
using StudioApiHost.Operations;
using UKHO.Aspire.Configuration;
using UKHO.Search.Configuration;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Providers.FileShare.Injection;
using UKHO.Search.ProviderModel;
using UKHO.Search.Studio.Ingestion;
using UKHO.Search.Studio.Providers;
using UKHO.Search.Studio.Rules;
using UKHO.Search.Studio.Providers.FileShare.Injection;

namespace StudioApiHost
{
    /// <summary>
    /// Builds and configures the Studio API host application.
    /// </summary>
    public static class StudioApiHostApplication
    {
        private const string OpenApiDocumentName = "v1";
        private const string OpenApiRoutePattern = "/openapi/{documentName}.json";

        /// <summary>
        /// Builds the Studio API host application with all required service registrations and endpoints.
        /// </summary>
        /// <param name="args">The command-line arguments supplied to the host process.</param>
        /// <param name="configureBuilder">An optional callback that can further configure the web application builder before default registrations run.</param>
        /// <returns>The fully configured <see cref="WebApplication"/> instance.</returns>
        public static WebApplication BuildApp(string[] args, Action<WebApplicationBuilder>? configureBuilder = null)
        {
            // Create the web application builder and define the trusted Studio shell origin used by local development.
            var builder = WebApplication.CreateBuilder(args);
            var studioShellOrigin = "http://localhost:3000";

            // Allow tests and specialized host scenarios to customize the builder before default registrations are applied.
            configureBuilder?.Invoke(builder);

            // Load shared configuration unless the caller explicitly disables it for isolated test scenarios.
            if (!builder.Configuration.GetValue<bool>("SkipAddsConfiguration"))
            {
                builder.AddConfiguration(ServiceConfiguration.ServiceGroupName, ServiceNames.Configuration);
            }

            // Register the core host services, including CORS, OpenAPI, rules, provider metadata, and Studio provider support.
            builder.Services.AddAuthorization();
            builder.Services.AddCors(options =>
            {
                // Restrict browser access to the known Studio shell origin while still allowing standard headers and methods.
                options.AddPolicy("StudioShell", policy =>
                {
                    policy.WithOrigins(studioShellOrigin)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });
            builder.Services.AddOpenApi(OpenApiDocumentName);
            builder.Services.AddIngestionRulesEngine();
            builder.Services.AddFileShareProviderMetadata();
            builder.Services.AddFileShareStudioProvider();
            builder.Services.AddSingleton<StudioIngestionOperationStore>();
            builder.Services.AddSingleton<StudioIngestionOperationCoordinator>();

            // Register the external infrastructure clients required by the Studio host runtime.
            builder.AddElasticsearchClient(ServiceNames.ElasticSearch);
            builder.AddAzureQueueServiceClient(ServiceNames.Queues);
            builder.AddAzureBlobServiceClient(ServiceNames.Blobs);

            builder.AddSqlServerClient(StorageNames.FileShareEmulatorDatabase);

            // Build the web application after all services and infrastructure clients are configured.
            var app = builder.Build();

            // Fail fast during startup when Studio providers and provider metadata are out of sync.
            app.Services.GetRequiredService<IStudioProviderRegistrationValidator>()
               .Validate();

            // Force the rules catalog to load during startup so rule-related endpoints do not fail lazily on first request.
            app.Services.GetRequiredService<IProviderRulesReader>()
               .EnsureLoaded();

            // Apply the middleware required before the endpoint mappings are added.
            app.UseCors("StudioShell");
            app.UseAuthorization();

            // Expose OpenAPI and Scalar endpoints for interactive exploration of the Studio API surface.
            app.MapOpenApi(OpenApiRoutePattern);
            app.MapScalarApiReference(
                "/scalar/v1",
                options =>
                {
                    // Point Scalar at the generated OpenAPI document and suppress explicit server entries for local hosting flexibility.
                    options.WithOpenApiRoutePattern(OpenApiRoutePattern);
                    options.Servers = [];
                });

            // Expose provider metadata directly from the shared provider catalog.
            app.MapGet("/providers", (IProviderCatalog providerCatalog) =>
            {
                // Return the full provider metadata snapshot so Studio clients can populate provider selections.
                return TypedResults.Ok(providerCatalog.GetAllProviders());
            })
            .WithName("GetProviders");

            // Project the loaded provider rules into the Studio-specific response contract consumed by the shell.
            app.MapGet("/rules", (IProviderCatalog providerCatalog, IProviderRulesReader rulesReader) =>
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
            })
            .WithName("GetRules");

            // Keep a lightweight echo endpoint for host smoke tests and simple availability checks.
            app.MapGet("/echo", () => TypedResults.Text("Hello from StudioApiHost echo."))
               .WithName("GetEcho");

            // Map the feature-specific ingestion and operation endpoints.
            app.MapStudioIngestionApi();
            app.MapStudioOperationsApi();

            // Return the fully configured application to the caller.
            return app;
        }
    }
}
