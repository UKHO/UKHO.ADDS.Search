using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace FileShareEmulator.Api
{
    public static class BatchFilesApi
    {
        public static IEndpointRouteBuilder MapBatchFilesApi(this IEndpointRouteBuilder endpoints)
        {
            static async Task<IResult> Handler(string batchId, BlobServiceClient blobServiceClient, IConfiguration configuration, HttpContext httpContext, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(batchId))
                {
                    return Results.BadRequest("batchId is required.");
                }

                // The FileShareImageLoader seeds data into a container named after the environment.
                var containerName = configuration["environment"];
                if (string.IsNullOrWhiteSpace(containerName))
                {
                    return Results.Problem("Unable to determine blob container name.", statusCode: StatusCodes.Status500InternalServerError);
                }

                // ContentImporter stores the zip as: "{guid}/{guid}.zip".
                // Azure Blob names are case-sensitive, so we normalise to lower-case first (fast path).
                var normalizedBatchId = batchId.ToLowerInvariant();
                var blobName = $"{normalizedBatchId}/{normalizedBatchId}.zip";
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                try
                {
                    BlobClient resolvedBlobClient;
                    try
                    {
                        // Probe for existence using the lower-case name. We don't download yet because
                        // we want to fall back to other casings if the lookup 404s.
                        resolvedBlobClient = blobClient;
                        _ = await resolvedBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken)
                                                    .ConfigureAwait(false);
                    }
                    catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status404NotFound)
                    {
                        // Fallback: try original casing (e.g. if something seeded content with different casing).
                        var originalCaseBlobName = $"{batchId}/{batchId}.zip";
                        var originalCaseBlobClient = containerClient.GetBlobClient(originalCaseBlobName);

                        try
                        {
                            // Probe for existence using the original-case name.
                            resolvedBlobClient = originalCaseBlobClient;
                            _ = await resolvedBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken)
                                                        .ConfigureAwait(false);
                        }
                        catch (RequestFailedException ex2) when (ex2.Status == StatusCodes.Status404NotFound)
                        {
                            return Results.NotFound();
                        }
                    }

                    // Stream the zip directly from blob storage (no buffering in memory).
                    var download = await resolvedBlobClient.DownloadStreamingAsync(cancellationToken: cancellationToken)
                                                           .ConfigureAwait(false);

                    var contentType = download.Value.Details.ContentType;
                    if (string.IsNullOrWhiteSpace(contentType))
                    {
                        contentType = "application/zip";
                    }

                    var fileName = $"{batchId}.zip";
                    return Results.Stream(download.Value.Content, contentType, fileName);
                }
                catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
                }
                catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status404NotFound)
                {
                    return Results.NotFound();
                }
                catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status403Forbidden)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }
                catch (RequestFailedException)
                {
                    return Results.StatusCode(StatusCodes.Status502BadGateway);
                }
            }

            endpoints.MapGet("/batch/{batchId}/files", Handler)
                     .WithName("GetBatchFiles")
                     .Produces(StatusCodes.Status200OK)
                     .Produces(StatusCodes.Status400BadRequest)
                     .Produces(StatusCodes.Status403Forbidden)
                     .Produces(StatusCodes.Status404NotFound)
                     .Produces(StatusCodes.Status502BadGateway)
                     .Produces(StatusCodes.Status500InternalServerError);

            return endpoints;
        }
    }
}