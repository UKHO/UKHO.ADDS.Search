using Microsoft.AspNetCore.Http.HttpResults;
using StudioServiceHost.Operations;
using UKHO.Search.Studio.Ingestion;
using UKHO.Search.Studio.Providers;

namespace StudioServiceHost.Api
{
    /// <summary>
    /// Defines the API surface for Studio ingestion payload and operation endpoints.
    /// </summary>
    public static class IngestionApi
    {
        /// <summary>
        /// Maps the Studio ingestion endpoints onto the supplied endpoint builder.
        /// </summary>
        /// <param name="endpoints">The endpoint builder that receives the ingestion endpoints.</param>
        /// <returns>The same <paramref name="endpoints"/> instance so endpoint configuration can continue fluently.</returns>
        public static IEndpointRouteBuilder MapStudioIngestionApi(this IEndpointRouteBuilder endpoints)
        {
            // Guard the extension entrypoint because the host must provide a valid route builder.
            ArgumentNullException.ThrowIfNull(endpoints);

            // Group ingestion endpoints under a shared route prefix and OpenAPI tag.
            var group = endpoints.MapGroup("/ingestion")
                                 .WithTags("Ingestion");

            group.MapGet("/{provider}/{id}", GetPayloadByIdAsync)
                 .WithName("GetIngestionPayloadById")
                 .WithSummary("Fetch an ingestion payload by provider-defined id.")
                 .WithDescription("Returns a provider-neutral envelope containing the provider-defined id and opaque payload JSON.")
                 .Produces<StudioIngestionPayloadEnvelope>(StatusCodes.Status200OK)
                 .Produces<StudioIngestionErrorResponse>(StatusCodes.Status400BadRequest)
                 .Produces<StudioIngestionErrorResponse>(StatusCodes.Status404NotFound)
                 .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapPost("/{provider}/payload", SubmitPayloadAsync)
                 .WithName("SubmitIngestionPayload")
                 .WithSummary("Submit an ingestion payload.")
                 .WithDescription("Attempts the provider queue write before returning success and respects the global active-operation lock.")
                 .Produces<StudioIngestionSubmitPayloadResponse>(StatusCodes.Status200OK)
                 .Produces<StudioIngestionErrorResponse>(StatusCodes.Status400BadRequest)
                  .Produces<StudioIngestionOperationConflictResponse>(StatusCodes.Status409Conflict)
                 .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapPut("/{provider}/all", StartIndexAllAsync)
                 .WithName("StartIngestionAll")
                 .WithSummary("Start provider-wide ingestion.")
                 .WithDescription("Starts a tracked long-running ingestion operation for all items the provider currently considers unindexed.")
                 .Produces<StudioIngestionAcceptedOperationResponse>(StatusCodes.Status202Accepted)
                 .Produces<StudioIngestionErrorResponse>(StatusCodes.Status400BadRequest)
                 .Produces<StudioIngestionOperationConflictResponse>(StatusCodes.Status409Conflict)
                 .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapPost("/{provider}/operations/reset-indexing-status", ResetIndexingStatusAsync)
                 .WithName("ResetIngestionIndexingStatus")
                 .WithSummary("Start provider-wide reset.")
                 .WithDescription("Starts a tracked long-running reset operation for the selected provider while enforcing the global active-operation lock.")
                 .Produces<StudioIngestionAcceptedOperationResponse>(StatusCodes.Status202Accepted)
                 .Produces<StudioIngestionErrorResponse>(StatusCodes.Status400BadRequest)
                 .Produces<StudioIngestionOperationConflictResponse>(StatusCodes.Status409Conflict)
                 .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapGet("/{provider}/contexts", GetContextsAsync)
                 .WithName("GetIngestionContexts")
                 .WithSummary("List provider contexts.")
                 .WithDescription("Returns provider-neutral context values and display names sorted by display name.")
                 .Produces<StudioIngestionContextsResponse>(StatusCodes.Status200OK)
                 .Produces<StudioIngestionErrorResponse>(StatusCodes.Status400BadRequest)
                 .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapPut("/{provider}/context/{context}", StartIndexContextAsync)
                 .WithName("StartIngestionContext")
                 .WithSummary("Start context-scoped ingestion.")
                 .WithDescription("Starts a tracked long-running ingestion operation for a provider-neutral context value.")
                 .Produces<StudioIngestionAcceptedOperationResponse>(StatusCodes.Status202Accepted)
                 .Produces<StudioIngestionErrorResponse>(StatusCodes.Status400BadRequest)
                 .Produces<StudioIngestionOperationConflictResponse>(StatusCodes.Status409Conflict)
                 .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapPost("/{provider}/context/{context}/operations/reset-indexing-status", ResetIndexingStatusForContextAsync)
                 .WithName("ResetIngestionIndexingStatusForContext")
                 .WithSummary("Start context-scoped reset.")
                 .WithDescription("Starts a tracked long-running reset operation for a provider-neutral context value.")
                 .Produces<StudioIngestionAcceptedOperationResponse>(StatusCodes.Status202Accepted)
                 .Produces<StudioIngestionErrorResponse>(StatusCodes.Status400BadRequest)
                 .Produces<StudioIngestionOperationConflictResponse>(StatusCodes.Status409Conflict)
                 .ProducesProblem(StatusCodes.Status500InternalServerError);

            return endpoints;
        }

        /// <summary>
        /// Loads a provider payload by its provider-defined identifier.
        /// </summary>
        /// <param name="provider">The provider name supplied in the request route.</param>
        /// <param name="id">The provider-defined item identifier to load.</param>
        /// <param name="studioProviderCatalog">The catalog used to resolve Studio ingestion providers.</param>
        /// <param name="loggerFactory">The logger factory used to create the endpoint logger.</param>
        /// <param name="cancellationToken">The token that cancels the provider fetch operation.</param>
        /// <returns>A successful payload response, a validation error, a not-found response, or a generic problem response.</returns>
        private static async Task<Results<Ok<StudioIngestionPayloadEnvelope>, BadRequest<StudioIngestionErrorResponse>, NotFound<StudioIngestionErrorResponse>, ProblemHttpResult>> GetPayloadByIdAsync(
            string provider,
            string id,
            IStudioProviderCatalog studioProviderCatalog,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            // Create the endpoint logger once so all validation and failure paths share the same category.
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            // Resolve the provider before invoking provider-specific payload lookup logic.
            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected ingestion fetch for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

            try
            {
                // Delegate the provider-specific fetch and translate the normalized result into HTTP responses.
                var result = await ingestionProvider.FetchPayloadByIdAsync(id, cancellationToken)
                                                    .ConfigureAwait(false);

                return result.Status switch
                {
                    StudioIngestionResultStatus.Success => TypedResults.Ok(result.Response!),
                    StudioIngestionResultStatus.InvalidRequest => TypedResults.BadRequest(result.Error!),
                    StudioIngestionResultStatus.NotFound => LogNotFoundAndReturn(logger, provider, id, result.Error!),
                    _ => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError)
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Convert unexpected provider failures into a generic problem response after logging the context.
                logger.LogError(ex, "Failed to fetch an ingestion payload. ProviderName={ProviderName} Id={Id}", provider, id);
                return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Submits an ingestion payload for a specific provider.
        /// </summary>
        /// <param name="provider">The provider name supplied in the request route.</param>
        /// <param name="request">The wrapped ingestion payload to submit.</param>
        /// <param name="studioProviderCatalog">The catalog used to resolve Studio ingestion providers.</param>
        /// <param name="operationStore">The shared store that enforces the single active-operation rule.</param>
        /// <param name="loggerFactory">The logger factory used to create the endpoint logger.</param>
        /// <param name="cancellationToken">The token that cancels the provider submission operation.</param>
        /// <returns>A successful submission response, a validation error, a conflict response, or a generic problem response.</returns>
        private static async Task<Results<Ok<StudioIngestionSubmitPayloadResponse>, BadRequest<StudioIngestionErrorResponse>, Conflict<StudioIngestionOperationConflictResponse>, ProblemHttpResult>> SubmitPayloadAsync(
            string provider,
            StudioIngestionPayloadEnvelope request,
            IStudioProviderCatalog studioProviderCatalog,
            StudioIngestionOperationStore operationStore,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            // Create the endpoint logger once so validation and execution paths emit consistent diagnostics.
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            // Resolve the provider before attempting to submit the wrapped payload.
            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected ingestion payload submit for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

            // Enforce the single active-operation constraint before queue submission occurs.
            var activeOperationConflict = operationStore.GetActiveConflict();
            if (activeOperationConflict is not null)
            {
                logger.LogWarning(
                    "Rejected ingestion payload submit because operation {OperationId} is active. RequestedProvider={ProviderName}",
                    activeOperationConflict.ActiveOperationId,
                    provider);
                return TypedResults.Conflict(activeOperationConflict);
            }

            try
            {
                // Delegate the provider-specific submission and translate the normalized result into HTTP responses.
                var result = await ingestionProvider.SubmitPayloadAsync(request, cancellationToken)
                                                    .ConfigureAwait(false);

                if (result.Status == StudioIngestionResultStatus.Success)
                {
                    logger.LogInformation("Submitted ingestion payload successfully. ProviderName={ProviderName} Id={Id}", provider, request.Id);
                    return TypedResults.Ok(result.Response!);
                }

                logger.LogWarning("Rejected ingestion payload submit for provider {ProviderName}. Id={Id} Reason={Reason}", provider, request.Id, result.Error?.Message);
                return TypedResults.BadRequest(result.Error!);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Convert unexpected provider failures into a generic problem response after logging the context.
                logger.LogError(ex, "Failed to submit an ingestion payload. ProviderName={ProviderName} Id={Id}", provider, request.Id);
                return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Starts a provider-wide indexing operation.
        /// </summary>
        /// <param name="provider">The provider name supplied in the request route.</param>
        /// <param name="studioProviderCatalog">The catalog used to resolve Studio ingestion providers.</param>
        /// <param name="operationCoordinator">The coordinator that starts tracked background operations.</param>
        /// <param name="loggerFactory">The logger factory used to create the endpoint logger.</param>
        /// <returns>An accepted operation response, a validation error, a conflict response, or a generic problem response.</returns>
        private static Results<Accepted<StudioIngestionAcceptedOperationResponse>, BadRequest<StudioIngestionErrorResponse>, Conflict<StudioIngestionOperationConflictResponse>, ProblemHttpResult> StartIndexAllAsync(
            string provider,
            IStudioProviderCatalog studioProviderCatalog,
            StudioIngestionOperationCoordinator operationCoordinator,
            ILoggerFactory loggerFactory)
        {
            // Create the endpoint logger once so validation and acceptance paths emit consistent diagnostics.
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            // Resolve the provider before attempting to queue a long-running provider-wide operation.
            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected provider-wide ingestion for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

            // Ask the coordinator to claim the single active-operation slot and start the provider callback.
            if (!operationCoordinator.TryStartOperation(
                    provider,
                    StudioIngestionOperationTypes.IndexAll,
                    null,
                    ingestionProvider.IndexAllAsync,
                    out var acceptedResponse,
                    out var conflictResponse))
            {
                logger.LogWarning(
                    "Rejected provider-wide ingestion because operation {OperationId} is already active. RequestedProvider={ProviderName}",
                    conflictResponse!.ActiveOperationId,
                    provider);
                return TypedResults.Conflict(conflictResponse);
            }

            logger.LogInformation(
                "Accepted provider-wide ingestion operation {OperationId} for provider {ProviderName}.",
                acceptedResponse.OperationId,
                provider);

            return TypedResults.Accepted($"/operations/{acceptedResponse.OperationId}", acceptedResponse);
        }

        /// <summary>
        /// Loads the provider-neutral contexts exposed by a provider.
        /// </summary>
        /// <param name="provider">The provider name supplied in the request route.</param>
        /// <param name="studioProviderCatalog">The catalog used to resolve Studio ingestion providers.</param>
        /// <param name="loggerFactory">The logger factory used to create the endpoint logger.</param>
        /// <param name="cancellationToken">The token that cancels the provider context lookup.</param>
        /// <returns>A context response, a validation error, or a generic problem response.</returns>
        private static async Task<Results<Ok<StudioIngestionContextsResponse>, BadRequest<StudioIngestionErrorResponse>, ProblemHttpResult>> GetContextsAsync(
            string provider,
            IStudioProviderCatalog studioProviderCatalog,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            // Create the endpoint logger once so validation and provider lookup paths emit consistent diagnostics.
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            // Resolve the provider before asking it for provider-neutral context values.
            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected ingestion context discovery for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

            try
            {
                // Load the provider contexts and normalize the ordering for a deterministic Studio experience.
                var response = await ingestionProvider.GetContextsAsync(cancellationToken)
                                                    .ConfigureAwait(false);

                logger.LogInformation("Loaded {ContextCount} ingestion contexts for provider {ProviderName}.", response.Contexts.Count, provider);

                return TypedResults.Ok(new StudioIngestionContextsResponse
                {
                    Provider = response.Provider,
                    Contexts = response.Contexts
                        .OrderBy(contextResponse => contextResponse.DisplayName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(contextResponse => contextResponse.Value, StringComparer.Ordinal)
                        .ToArray()
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Convert unexpected provider failures into a generic problem response after logging the context.
                logger.LogError(ex, "Failed to load ingestion contexts. ProviderName={ProviderName}", provider);
                return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Starts a context-scoped indexing operation.
        /// </summary>
        /// <param name="provider">The provider name supplied in the request route.</param>
        /// <param name="context">The provider-neutral context to index.</param>
        /// <param name="studioProviderCatalog">The catalog used to resolve Studio ingestion providers.</param>
        /// <param name="operationCoordinator">The coordinator that starts tracked background operations.</param>
        /// <param name="loggerFactory">The logger factory used to create the endpoint logger.</param>
        /// <param name="cancellationToken">The token that cancels context validation work.</param>
        /// <returns>An accepted operation response, a validation error, a conflict response, or a generic problem response.</returns>
        private static async Task<Results<Accepted<StudioIngestionAcceptedOperationResponse>, BadRequest<StudioIngestionErrorResponse>, Conflict<StudioIngestionOperationConflictResponse>, ProblemHttpResult>> StartIndexContextAsync(
            string provider,
            string context,
            IStudioProviderCatalog studioProviderCatalog,
            StudioIngestionOperationCoordinator operationCoordinator,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            // Create the endpoint logger once so validation and acceptance paths emit consistent diagnostics.
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            // Resolve the provider before validating and scheduling the context-scoped operation.
            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected context ingestion for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

            // Validate the requested context against the provider's current context list before creating the operation.
            var contextValidationError = await ValidateContextAsync(provider, context, ingestionProvider, cancellationToken)
                .ConfigureAwait(false);
            if (contextValidationError is not null)
            {
                logger.LogWarning("Rejected context ingestion for provider {ProviderName}. Context={Context} Reason={Reason}", provider, context, contextValidationError.Message);
                return TypedResults.BadRequest(contextValidationError);
            }

            // Ask the coordinator to claim the active-operation slot and start the provider callback for the selected context.
            if (!operationCoordinator.TryStartOperation(
                    provider,
                    StudioIngestionOperationTypes.ContextIndex,
                    context,
                    (progress, ct) => ingestionProvider.IndexContextAsync(context, progress, ct),
                    out var acceptedResponse,
                    out var conflictResponse))
            {
                logger.LogWarning(
                    "Rejected context ingestion because operation {OperationId} is already active. RequestedProvider={ProviderName} Context={Context}",
                    conflictResponse!.ActiveOperationId,
                    provider,
                    context);
                return TypedResults.Conflict(conflictResponse);
            }

            logger.LogInformation(
                "Accepted context ingestion operation {OperationId} for provider {ProviderName}. Context={Context}",
                acceptedResponse.OperationId,
                provider,
                context);

            return TypedResults.Accepted($"/operations/{acceptedResponse.OperationId}", acceptedResponse);
        }

        /// <summary>
        /// Starts a context-scoped reset-indexing-status operation.
        /// </summary>
        /// <param name="provider">The provider name supplied in the request route.</param>
        /// <param name="context">The provider-neutral context to reset.</param>
        /// <param name="studioProviderCatalog">The catalog used to resolve Studio ingestion providers.</param>
        /// <param name="operationCoordinator">The coordinator that starts tracked background operations.</param>
        /// <param name="loggerFactory">The logger factory used to create the endpoint logger.</param>
        /// <param name="cancellationToken">The token that cancels context validation work.</param>
        /// <returns>An accepted operation response, a validation error, a conflict response, or a generic problem response.</returns>
        private static async Task<Results<Accepted<StudioIngestionAcceptedOperationResponse>, BadRequest<StudioIngestionErrorResponse>, Conflict<StudioIngestionOperationConflictResponse>, ProblemHttpResult>> ResetIndexingStatusForContextAsync(
            string provider,
            string context,
            IStudioProviderCatalog studioProviderCatalog,
            StudioIngestionOperationCoordinator operationCoordinator,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            // Create the endpoint logger once so validation and acceptance paths emit consistent diagnostics.
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            // Resolve the provider before validating and scheduling the context-scoped reset operation.
            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected context reset for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

            // Validate the requested context before creating the reset operation.
            var contextValidationError = await ValidateContextAsync(provider, context, ingestionProvider, cancellationToken)
                .ConfigureAwait(false);
            if (contextValidationError is not null)
            {
                logger.LogWarning("Rejected context reset for provider {ProviderName}. Context={Context} Reason={Reason}", provider, context, contextValidationError.Message);
                return TypedResults.BadRequest(contextValidationError);
            }

            // Ask the coordinator to claim the active-operation slot and start the reset callback for the selected context.
            if (!operationCoordinator.TryStartOperation(
                    provider,
                    StudioIngestionOperationTypes.ResetIndexingStatus,
                    context,
                    (progress, ct) => ingestionProvider.ResetIndexingStatusForContextAsync(context, progress, ct),
                    out var acceptedResponse,
                    out var conflictResponse))
            {
                logger.LogWarning(
                    "Rejected context reset because operation {OperationId} is already active. RequestedProvider={ProviderName} Context={Context}",
                    conflictResponse!.ActiveOperationId,
                    provider,
                    context);
                return TypedResults.Conflict(conflictResponse);
            }

            logger.LogInformation(
                "Accepted context reset operation {OperationId} for provider {ProviderName}. Context={Context}",
                acceptedResponse.OperationId,
                provider,
                context);

            return TypedResults.Accepted($"/operations/{acceptedResponse.OperationId}", acceptedResponse);
        }

        /// <summary>
        /// Starts a provider-wide reset-indexing-status operation.
        /// </summary>
        /// <param name="provider">The provider name supplied in the request route.</param>
        /// <param name="studioProviderCatalog">The catalog used to resolve Studio ingestion providers.</param>
        /// <param name="operationCoordinator">The coordinator that starts tracked background operations.</param>
        /// <param name="loggerFactory">The logger factory used to create the endpoint logger.</param>
        /// <returns>An accepted operation response, a validation error, a conflict response, or a generic problem response.</returns>
        private static Results<Accepted<StudioIngestionAcceptedOperationResponse>, BadRequest<StudioIngestionErrorResponse>, Conflict<StudioIngestionOperationConflictResponse>, ProblemHttpResult> ResetIndexingStatusAsync(
            string provider,
            IStudioProviderCatalog studioProviderCatalog,
            StudioIngestionOperationCoordinator operationCoordinator,
            ILoggerFactory loggerFactory)
        {
            // Create the endpoint logger once so validation and acceptance paths emit consistent diagnostics.
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            // Resolve the provider before scheduling the provider-wide reset operation.
            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected provider-wide reset for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

            // Ask the coordinator to claim the active-operation slot and start the provider callback.
            if (!operationCoordinator.TryStartOperation(
                    provider,
                    StudioIngestionOperationTypes.ResetIndexingStatus,
                    null,
                    ingestionProvider.ResetIndexingStatusAsync,
                    out var acceptedResponse,
                    out var conflictResponse))
            {
                logger.LogWarning(
                    "Rejected provider-wide reset because operation {OperationId} is already active. RequestedProvider={ProviderName}",
                    conflictResponse!.ActiveOperationId,
                    provider);
                return TypedResults.Conflict(conflictResponse);
            }

            logger.LogInformation(
                "Accepted provider-wide reset operation {OperationId} for provider {ProviderName}.",
                acceptedResponse.OperationId,
                provider);

            return TypedResults.Accepted($"/operations/{acceptedResponse.OperationId}", acceptedResponse);
        }

        /// <summary>
        /// Logs a missing-payload result before returning the standardized not-found response.
        /// </summary>
        /// <param name="logger">The logger that records the not-found event.</param>
        /// <param name="provider">The provider name supplied in the request route.</param>
        /// <param name="id">The provider-defined item identifier that was not found.</param>
        /// <param name="error">The standardized not-found payload to return.</param>
        /// <returns>The standardized not-found payload.</returns>
        private static NotFound<StudioIngestionErrorResponse> LogNotFoundAndReturn(
            ILogger logger,
            string provider,
            string id,
            StudioIngestionErrorResponse error)
        {
            // Log missing payloads before returning the provider-neutral not-found payload to the caller.
            logger.LogWarning("No ingestion payload was found. ProviderName={ProviderName} Id={Id}", provider, id);
            return TypedResults.NotFound(error);
        }

        /// <summary>
        /// Resolves and validates a Studio ingestion provider from the shared catalog.
        /// </summary>
        /// <param name="provider">The provider name supplied in the request route.</param>
        /// <param name="studioProviderCatalog">The catalog used to resolve Studio ingestion providers.</param>
        /// <param name="ingestionProvider">When this method succeeds, receives the typed Studio ingestion provider.</param>
        /// <param name="errorResponse">When this method fails, receives the standardized validation error.</param>
        /// <returns><see langword="true"/> when the provider exists and supports ingestion; otherwise <see langword="false"/>.</returns>
        private static bool TryGetIngestionProvider(
            string provider,
            IStudioProviderCatalog studioProviderCatalog,
            out IStudioIngestionProvider ingestionProvider,
            out StudioIngestionErrorResponse? errorResponse)
        {
            // Initialize the out parameters before entering validation branches.
            ingestionProvider = null!;
            errorResponse = null;

            // Reject missing provider names before attempting any catalog lookup.
            if (string.IsNullOrWhiteSpace(provider))
            {
                errorResponse = new StudioIngestionErrorResponse
                {
                    Message = "Provider is required."
                };
                return false;
            }

            // Resolve the provider from the shared Studio catalog and confirm that it supports ingestion operations.
            if (!studioProviderCatalog.TryGetProvider(provider, out var studioProvider) || studioProvider is not IStudioIngestionProvider typedProvider)
            {
                errorResponse = new StudioIngestionErrorResponse
                {
                    Message = $"Unknown provider '{provider}'."
                };
                return false;
            }

            // Return the typed provider to the caller once validation and catalog lookup succeed.
            ingestionProvider = typedProvider;
            return true;
        }

        /// <summary>
        /// Validates that a provider-neutral context exists for the selected provider.
        /// </summary>
        /// <param name="provider">The provider name supplied in the request route.</param>
        /// <param name="context">The provider-neutral context to validate.</param>
        /// <param name="ingestionProvider">The provider used to load the current context list.</param>
        /// <param name="cancellationToken">The token that cancels the provider context lookup.</param>
        /// <returns>A validation error when the context is invalid; otherwise <see langword="null"/>.</returns>
        private static async Task<StudioIngestionErrorResponse?> ValidateContextAsync(
            string provider,
            string context,
            IStudioIngestionProvider ingestionProvider,
            CancellationToken cancellationToken)
        {
            // Reject empty context values before performing the more expensive provider context lookup.
            if (string.IsNullOrWhiteSpace(context))
            {
                return new StudioIngestionErrorResponse
                {
                    Message = "Context is required."
                };
            }

            // Load the provider contexts once and compare against the requested provider-neutral value.
            var contextsResponse = await ingestionProvider.GetContextsAsync(cancellationToken)
                                                         .ConfigureAwait(false);

            // Return null for a valid context or a standardized validation error when the context is unknown.
            return contextsResponse.Contexts.Any(contextResponse => string.Equals(contextResponse.Value, context, StringComparison.Ordinal))
                ? null
                : new StudioIngestionErrorResponse
                {
                    Message = $"Unknown context '{context}' for provider '{provider}'."
                };
        }
    }
}
