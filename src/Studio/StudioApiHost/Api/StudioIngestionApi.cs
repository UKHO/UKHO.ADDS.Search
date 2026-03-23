using Microsoft.AspNetCore.Http.HttpResults;
using StudioApiHost.Operations;
using UKHO.Search.Studio;

namespace StudioApiHost.Api
{
    public static class StudioIngestionApi
    {
        public static IEndpointRouteBuilder MapStudioIngestionApi(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

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

        private static async Task<Results<Ok<StudioIngestionPayloadEnvelope>, BadRequest<StudioIngestionErrorResponse>, NotFound<StudioIngestionErrorResponse>, ProblemHttpResult>> GetPayloadByIdAsync(
            string provider,
            string id,
            IStudioProviderCatalog studioProviderCatalog,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected ingestion fetch for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

            try
            {
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
                logger.LogError(ex, "Failed to fetch an ingestion payload. ProviderName={ProviderName} Id={Id}", provider, id);
                return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<Results<Ok<StudioIngestionSubmitPayloadResponse>, BadRequest<StudioIngestionErrorResponse>, Conflict<StudioIngestionOperationConflictResponse>, ProblemHttpResult>> SubmitPayloadAsync(
            string provider,
            StudioIngestionPayloadEnvelope request,
            IStudioProviderCatalog studioProviderCatalog,
            StudioIngestionOperationStore operationStore,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected ingestion payload submit for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

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
                logger.LogError(ex, "Failed to submit an ingestion payload. ProviderName={ProviderName} Id={Id}", provider, request.Id);
                return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static Results<Accepted<StudioIngestionAcceptedOperationResponse>, BadRequest<StudioIngestionErrorResponse>, Conflict<StudioIngestionOperationConflictResponse>, ProblemHttpResult> StartIndexAllAsync(
            string provider,
            IStudioProviderCatalog studioProviderCatalog,
            StudioIngestionOperationCoordinator operationCoordinator,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected provider-wide ingestion for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

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

        private static async Task<Results<Ok<StudioIngestionContextsResponse>, BadRequest<StudioIngestionErrorResponse>, ProblemHttpResult>> GetContextsAsync(
            string provider,
            IStudioProviderCatalog studioProviderCatalog,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected ingestion context discovery for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

            try
            {
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
                logger.LogError(ex, "Failed to load ingestion contexts. ProviderName={ProviderName}", provider);
                return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<Results<Accepted<StudioIngestionAcceptedOperationResponse>, BadRequest<StudioIngestionErrorResponse>, Conflict<StudioIngestionOperationConflictResponse>, ProblemHttpResult>> StartIndexContextAsync(
            string provider,
            string context,
            IStudioProviderCatalog studioProviderCatalog,
            StudioIngestionOperationCoordinator operationCoordinator,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected context ingestion for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

            var contextValidationError = await ValidateContextAsync(provider, context, ingestionProvider, cancellationToken)
                .ConfigureAwait(false);
            if (contextValidationError is not null)
            {
                logger.LogWarning("Rejected context ingestion for provider {ProviderName}. Context={Context} Reason={Reason}", provider, context, contextValidationError.Message);
                return TypedResults.BadRequest(contextValidationError);
            }

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

        private static async Task<Results<Accepted<StudioIngestionAcceptedOperationResponse>, BadRequest<StudioIngestionErrorResponse>, Conflict<StudioIngestionOperationConflictResponse>, ProblemHttpResult>> ResetIndexingStatusForContextAsync(
            string provider,
            string context,
            IStudioProviderCatalog studioProviderCatalog,
            StudioIngestionOperationCoordinator operationCoordinator,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected context reset for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

            var contextValidationError = await ValidateContextAsync(provider, context, ingestionProvider, cancellationToken)
                .ConfigureAwait(false);
            if (contextValidationError is not null)
            {
                logger.LogWarning("Rejected context reset for provider {ProviderName}. Context={Context} Reason={Reason}", provider, context, contextValidationError.Message);
                return TypedResults.BadRequest(contextValidationError);
            }

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

        private static Results<Accepted<StudioIngestionAcceptedOperationResponse>, BadRequest<StudioIngestionErrorResponse>, Conflict<StudioIngestionOperationConflictResponse>, ProblemHttpResult> ResetIndexingStatusAsync(
            string provider,
            IStudioProviderCatalog studioProviderCatalog,
            StudioIngestionOperationCoordinator operationCoordinator,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("StudioIngestionApi");

            if (!TryGetIngestionProvider(provider, studioProviderCatalog, out var ingestionProvider, out var errorResponse))
            {
                logger.LogWarning("Rejected provider-wide reset for unknown provider {ProviderName}.", provider);
                return TypedResults.BadRequest(errorResponse!);
            }

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

        private static NotFound<StudioIngestionErrorResponse> LogNotFoundAndReturn(
            ILogger logger,
            string provider,
            string id,
            StudioIngestionErrorResponse error)
        {
            logger.LogWarning("No ingestion payload was found. ProviderName={ProviderName} Id={Id}", provider, id);
            return TypedResults.NotFound(error);
        }

        private static bool TryGetIngestionProvider(
            string provider,
            IStudioProviderCatalog studioProviderCatalog,
            out IStudioIngestionProvider ingestionProvider,
            out StudioIngestionErrorResponse? errorResponse)
        {
            ingestionProvider = null!;
            errorResponse = null;

            if (string.IsNullOrWhiteSpace(provider))
            {
                errorResponse = new StudioIngestionErrorResponse
                {
                    Message = "Provider is required."
                };
                return false;
            }

            if (!studioProviderCatalog.TryGetProvider(provider, out var studioProvider) || studioProvider is not IStudioIngestionProvider typedProvider)
            {
                errorResponse = new StudioIngestionErrorResponse
                {
                    Message = $"Unknown provider '{provider}'."
                };
                return false;
            }

            ingestionProvider = typedProvider;
            return true;
        }

        private static async Task<StudioIngestionErrorResponse?> ValidateContextAsync(
            string provider,
            string context,
            IStudioIngestionProvider ingestionProvider,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(context))
            {
                return new StudioIngestionErrorResponse
                {
                    Message = "Context is required."
                };
            }

            var contextsResponse = await ingestionProvider.GetContextsAsync(cancellationToken)
                                                         .ConfigureAwait(false);

            return contextsResponse.Contexts.Any(contextResponse => string.Equals(contextResponse.Value, context, StringComparison.Ordinal))
                ? null
                : new StudioIngestionErrorResponse
                {
                    Message = $"Unknown context '{context}' for provider '{provider}'."
                };
        }
    }
}
