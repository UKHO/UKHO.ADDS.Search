using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using StudioApiHost.Operations;
using UKHO.Search.Studio.Ingestion;

namespace StudioApiHost.Api
{
    /// <summary>
    /// Defines the API surface for inspecting tracked Studio ingestion operations.
    /// </summary>
    public static class StudioOperationsApi
    {
        /// <summary>
        /// Maps the Studio operation inspection endpoints onto the supplied endpoint builder.
        /// </summary>
        /// <param name="endpoints">The endpoint builder that receives the operations endpoints.</param>
        /// <returns>The same <paramref name="endpoints"/> instance so endpoint configuration can continue fluently.</returns>
        public static IEndpointRouteBuilder MapStudioOperationsApi(this IEndpointRouteBuilder endpoints)
        {
            // Guard the extension entrypoint because the host must provide a valid route builder.
            ArgumentNullException.ThrowIfNull(endpoints);

            // Group the operation endpoints under a shared route prefix and OpenAPI tag.
            var group = endpoints.MapGroup("/operations")
                                 .WithTags("Operations");

            group.MapGet("/active", GetActiveOperation)
                 .WithName("GetActiveOperation")
                 .WithSummary("Get the current active ingestion operation.")
                 .WithDescription("Returns the single queued or running operation tracked in memory, if one exists.")
                 .Produces<StudioIngestionOperationStateResponse>(StatusCodes.Status200OK)
                 .Produces<StudioIngestionErrorResponse>(StatusCodes.Status404NotFound);

            group.MapGet("/{operationId:guid}", GetOperationById)
                 .WithName("GetOperationById")
                 .WithSummary("Get an ingestion operation by id.")
                 .WithDescription("Returns the retained in-memory operation snapshot for queued, running, succeeded, or failed operations.")
                 .Produces<StudioIngestionOperationStateResponse>(StatusCodes.Status200OK)
                 .Produces<StudioIngestionErrorResponse>(StatusCodes.Status404NotFound);

            group.MapGet("/{operationId:guid}/events", StreamOperationEventsAsync)
                 .WithName("GetOperationEvents")
                 .WithSummary("Stream live ingestion operation events.")
                 .WithDescription("Streams live server-sent events for a tracked operation until the terminal event is emitted and the stream closes.")
                 .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
                 .Produces<StudioIngestionErrorResponse>(StatusCodes.Status404NotFound);

            return endpoints;
        }

        private static Results<Ok<StudioIngestionOperationStateResponse>, NotFound<StudioIngestionErrorResponse>> GetActiveOperation(
            StudioIngestionOperationStore operationStore)
        {
            // Read the active operation snapshot once so the response reflects a consistent in-memory view.
            var activeOperation = operationStore.GetActive();

            // Return a standard not-found payload when nothing is currently queued or running.
            return activeOperation is null
                ? TypedResults.NotFound(new StudioIngestionErrorResponse { Message = "No active operation was found." })
                : TypedResults.Ok(activeOperation);
        }

        private static Results<Ok<StudioIngestionOperationStateResponse>, NotFound<StudioIngestionErrorResponse>> GetOperationById(
            Guid operationId,
            StudioIngestionOperationStore operationStore)
        {
            // Query the retained in-memory operation history for the supplied identifier.
            var operation = operationStore.GetById(operationId);

            // Return a provider-neutral not-found response when the requested operation is unknown.
            return operation is null
                ? TypedResults.NotFound(new StudioIngestionErrorResponse { Message = $"Operation '{operationId:D}' was not found." })
                : TypedResults.Ok(operation);
        }

        private static async Task<IResult> StreamOperationEventsAsync(
            Guid operationId,
            HttpContext httpContext,
            IOptions<JsonOptions> jsonOptions,
            StudioIngestionOperationStore operationStore,
            CancellationToken cancellationToken)
        {
            // Resolve the event stream for the requested operation before any response body is written.
            var eventReader = operationStore.Subscribe(operationId);
            if (eventReader is null)
            {
                return TypedResults.NotFound(new StudioIngestionErrorResponse
                {
                    Message = $"Operation '{operationId:D}' was not found."
                });
            }

            // Configure the response for server-sent events so proxies and clients do not buffer the stream.
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Append("X-Accel-Buffering", "no");
            httpContext.Response.ContentType = "text/event-stream";

            // Emit an initial keep-alive comment so clients can detect that the SSE stream is connected.
            await httpContext.Response.WriteAsync(": connected\n\n", cancellationToken)
                                      .ConfigureAwait(false);
            await httpContext.Response.Body.FlushAsync(cancellationToken)
                                  .ConfigureAwait(false);

            // Stream each retained event as an SSE data record until the tracked operation closes the channel.
            await foreach (var operationEvent in eventReader.ReadAllAsync(cancellationToken))
            {
                await httpContext.Response.WriteAsync("data: ", cancellationToken)
                                          .ConfigureAwait(false);
                await JsonSerializer.SerializeAsync(
                    httpContext.Response.Body,
                    operationEvent,
                    jsonOptions.Value.SerializerOptions,
                    cancellationToken).ConfigureAwait(false);
                await httpContext.Response.WriteAsync("\n\n", cancellationToken)
                                          .ConfigureAwait(false);
                await httpContext.Response.Body.FlushAsync(cancellationToken)
                                      .ConfigureAwait(false);
            }

            // Return an empty result because the SSE payload has already been written directly to the response body.
            return Results.Empty;
        }
    }
}
