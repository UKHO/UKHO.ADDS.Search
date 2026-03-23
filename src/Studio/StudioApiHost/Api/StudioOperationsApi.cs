using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using StudioApiHost.Operations;
using UKHO.Search.Studio;

namespace StudioApiHost.Api
{
    public static class StudioOperationsApi
    {
        public static IEndpointRouteBuilder MapStudioOperationsApi(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

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
            var activeOperation = operationStore.GetActive();
            return activeOperation is null
                ? TypedResults.NotFound(new StudioIngestionErrorResponse { Message = "No active operation was found." })
                : TypedResults.Ok(activeOperation);
        }

        private static Results<Ok<StudioIngestionOperationStateResponse>, NotFound<StudioIngestionErrorResponse>> GetOperationById(
            Guid operationId,
            StudioIngestionOperationStore operationStore)
        {
            var operation = operationStore.GetById(operationId);
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
            var eventReader = operationStore.Subscribe(operationId);
            if (eventReader is null)
            {
                return TypedResults.NotFound(new StudioIngestionErrorResponse
                {
                    Message = $"Operation '{operationId:D}' was not found."
                });
            }

            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Append("X-Accel-Buffering", "no");
            httpContext.Response.ContentType = "text/event-stream";

            await httpContext.Response.WriteAsync(": connected\n\n", cancellationToken)
                                      .ConfigureAwait(false);
            await httpContext.Response.Body.FlushAsync(cancellationToken)
                                  .ConfigureAwait(false);

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

            return Results.Empty;
        }
    }
}
