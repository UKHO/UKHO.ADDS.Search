using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace FileShareEmulator.Services
{
    public sealed class BatchDownloadService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BatchDownloadService> _logger;

        public BatchDownloadService(BlobServiceClient blobServiceClient, IConfiguration configuration, ILogger<BatchDownloadService> logger)
        {
            _blobServiceClient = blobServiceClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<BatchDownloadResult> DownloadBatchAsync(string batchId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(batchId))
            {
                return BatchDownloadResult.Fail("BatchId is required.");
            }

            // The FileShareImageLoader seeds data into a container named after the environment.
            var containerName = _configuration["environment"];
            if (string.IsNullOrWhiteSpace(containerName))
            {
                return BatchDownloadResult.Fail("Configuration error: unable to determine blob container name.");
            }

            var downloadPath = _configuration["DownloadPath"];
            if (string.IsNullOrWhiteSpace(downloadPath))
            {
                return BatchDownloadResult.Fail("Configuration error: DownloadPath is not set.");
            }

            try
            {
                Directory.CreateDirectory(downloadPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create download directory {DownloadPath}.", downloadPath);
                return BatchDownloadResult.Fail("Unable to create download directory.");
            }

            // ContentImporter stores the zip as: "{guid}/{guid}.zip".
            // Azure Blob names are case-sensitive, so we normalise to lower-case first (fast path).
            var normalizedBatchId = batchId.Trim().ToLowerInvariant();
            var normalizedBlobName = $"{normalizedBatchId}/{normalizedBatchId}.zip";

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            static async Task<BlobClient?> TryFindBlobCaseInsensitiveAsync(BlobContainerClient container, string requestedName, CancellationToken ct)
            {
                var requested = requestedName.ToLowerInvariant();

                await foreach (var blob in container.GetBlobsAsync(BlobTraits.None, BlobStates.None, null, ct)
                                                    .ConfigureAwait(false))
                {
                    if (blob.Name.ToLowerInvariant() == requested)
                    {
                        return container.GetBlobClient(blob.Name);
                    }
                }

                return null;
            }

            BlobClient resolvedBlobClient;
            try
            {
                // Probe for existence using the lower-case blob name first.
                resolvedBlobClient = containerClient.GetBlobClient(normalizedBlobName);
                _ = await resolvedBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken)
                                            .ConfigureAwait(false);
            }
            catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status404NotFound)
            {
                var originalBatchId = batchId.Trim();
                var originalBlobName = $"{originalBatchId}/{originalBatchId}.zip";
                var originalBlobClient = containerClient.GetBlobClient(originalBlobName);

                try
                {
                    resolvedBlobClient = originalBlobClient;
                    _ = await resolvedBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken)
                                                .ConfigureAwait(false);
                }
                catch (RequestFailedException ex2) when (ex2.Status == StatusCodes.Status404NotFound)
                {
                    var caseInsensitiveMatch = await TryFindBlobCaseInsensitiveAsync(containerClient, originalBlobName, cancellationToken)
                        .ConfigureAwait(false);

                    if (caseInsensitiveMatch is null)
                    {
                        return BatchDownloadResult.Fail("Batch not found.");
                    }

                    resolvedBlobClient = caseInsensitiveMatch;
                }
            }
            catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status403Forbidden)
            {
                _logger.LogWarning(ex, "Access denied downloading batch {BatchId} from container {ContainerName}.", batchId, containerName);
                return BatchDownloadResult.Fail("Access denied downloading batch.");
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to resolve blob for batch {BatchId} in container {ContainerName}.", batchId, containerName);
                return BatchDownloadResult.Fail("Unable to locate batch in blob storage.");
            }

            var destinationFilePath = Path.Combine(downloadPath, $"{batchId.Trim()}.zip");

            try
            {
                var download = await resolvedBlobClient.DownloadStreamingAsync(cancellationToken: cancellationToken)
                                                      .ConfigureAwait(false);

                await using var destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await download.Value.Content.CopyToAsync(destinationStream, cancellationToken)
                                            .ConfigureAwait(false);

                return BatchDownloadResult.Ok(destinationFilePath);
            }
            catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status404NotFound)
            {
                return BatchDownloadResult.Fail("Batch not found.");
            }
            catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status403Forbidden)
            {
                _logger.LogWarning(ex, "Access denied downloading batch {BatchId} from container {ContainerName}.", batchId, containerName);
                return BatchDownloadResult.Fail("Access denied downloading batch.");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed writing batch {BatchId} to {DestinationFilePath}.", batchId, destinationFilePath);
                return BatchDownloadResult.Fail("Unable to write downloaded batch to disk.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error downloading batch {BatchId} to {DestinationFilePath}.", batchId, destinationFilePath);
                return BatchDownloadResult.Fail("Unexpected error downloading batch.");
            }
        }
    }
}
