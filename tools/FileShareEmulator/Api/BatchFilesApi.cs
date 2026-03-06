using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace FileShareEmulator.Api
{
    public static class BatchFilesApi
    {
        public static IEndpointRouteBuilder MapBatchFilesApi(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/batch/{batchId}/files", async (
                    string batchId,
                    BlobServiceClient blobServiceClient,
                    IConfiguration configuration,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(batchId))
                    {
                        return Results.BadRequest("batchId is required.");
                    }

                    var containerName = configuration["environment"];
                    if (string.IsNullOrWhiteSpace(containerName))
                    {
                        return Results.Problem("Unable to determine blob container name.", statusCode: StatusCodes.Status500InternalServerError);
                    }

                    var normalizedBatchId = batchId.ToLowerInvariant();
                    var blobName = $"{normalizedBatchId}/{normalizedBatchId}.zip";
                    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    var blobClient = containerClient.GetBlobClient(blobName);

                    static async Task<BlobClient?> TryFindBlobCaseInsensitiveAsync(BlobContainerClient container, string requestedName, CancellationToken ct)
                    {
                        var requested = requestedName.ToLowerInvariant();

                        await foreach (BlobItem blob in container.GetBlobsAsync(traits: BlobTraits.None, states: BlobStates.None, prefix: null, cancellationToken: ct)
                                           .ConfigureAwait(false))
                        {
                            if (blob.Name.ToLowerInvariant() == requested)
                            {
                                return container.GetBlobClient(blob.Name);
                            }
                        }

                        return null;
                    }

                    try
                    {
                        BlobClient resolvedBlobClient;
                        try
                        {
                            resolvedBlobClient = blobClient;
                            _ = await resolvedBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                        }
                        catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status404NotFound)
                        {
                            var originalCaseBlobName = $"{batchId}/{batchId}.zip";
                            var originalCaseBlobClient = containerClient.GetBlobClient(originalCaseBlobName);

                            try
                            {
                                resolvedBlobClient = originalCaseBlobClient;
                                _ = await resolvedBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                            }
                            catch (RequestFailedException ex2) when (ex2.Status == StatusCodes.Status404NotFound)
                            {
                                var caseInsensitiveMatch = await TryFindBlobCaseInsensitiveAsync(containerClient, originalCaseBlobName, cancellationToken).ConfigureAwait(false);
                                if (caseInsensitiveMatch is null)
                                {
                                    return Results.NotFound();
                                }

                                resolvedBlobClient = caseInsensitiveMatch;
                            }
                        }

                        var download = await resolvedBlobClient.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                        var contentType = download.Value.Details.ContentType;
                        if (string.IsNullOrWhiteSpace(contentType))
                        {
                            contentType = "application/zip";
                        }

                        var fileName = $"{batchId}.zip";
                        return Results.Stream(download.Value.Content,
                            contentType: contentType,
                            fileDownloadName: fileName);
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
                })
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
