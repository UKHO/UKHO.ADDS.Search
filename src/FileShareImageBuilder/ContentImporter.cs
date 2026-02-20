using Microsoft.Data.SqlClient;
using System.Data;
using UKHO.ADDS.Search.Configuration;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;

namespace FileShareImageBuilder
{
    public class ContentImporter
    {
        private readonly IFileShareReadOnlyClient _fileShareClient;

        public ContentImporter(IFileShareReadOnlyClient fileShareClient)
        {
            _fileShareClient = fileShareClient;
        }

        public async Task ImportAsync(CancellationToken cancellationToken = default)
        {
            var dataImagePath = ConfigurationReader.GetDataImagePath();
            var binDirectory = Path.Combine(dataImagePath, "bin");
            RecreateEmptyDirectory(binDirectory);

            var maxBytes = (long)ConfigurationReader.GetDataImageBinSizeGB() * 1024L * 1024L * 1024L;
            long totalBytesDownloaded = 0;

            var targetConnectionString = ConfigurationReader.GetTargetDatabaseConnectionString(StorageNames.FileShareEmulatorDatabase);

            const int pageSize = 1000;
            DateTime? lastCreatedOn = null;
            Guid? lastId = null;
            var totalBatchesDownloaded = 0;
            var pageNumber = 0;

            while (totalBytesDownloaded < maxBytes)
            {
                pageNumber++;

                var batchIds = await GetBatchIdsPageAsync(
                    targetConnectionString,
                    pageSize,
                    lastCreatedOn,
                    lastId,
                    cancellationToken).ConfigureAwait(false);

                if (batchIds.Count == 0)
                {
                    Console.WriteLine($"[ContentImporter] No more batches found. Downloaded {totalBatchesDownloaded} batches, {totalBytesDownloaded:N0} bytes.");
                    break;
                }

                Console.WriteLine($"[ContentImporter] Page {pageNumber}: processing {batchIds.Count} batches (downloaded so far: {totalBatchesDownloaded}, {totalBytesDownloaded:N0}/{maxBytes:N0} bytes)." );

                foreach (var batch in batchIds)
                {
                    var batchIdString = batch.Id.ToString("D");

                    var shard = GetMostSignificantByteHex(batch.Id);
                    var shardDirectory = Path.Combine(binDirectory, shard);
                    Directory.CreateDirectory(shardDirectory);

                    var zipPath = Path.Combine(shardDirectory, $"{batchIdString}.zip");

                    var result = await _fileShareClient.DownloadZipFileAsync(batchIdString, cancellationToken).ConfigureAwait(false);
                    if (!result.IsSuccess(out var stream, out var error) || stream is null)
                    {
                        throw new InvalidOperationException($"Failed to download batch '{batchIdString}'. {error}");
                    }

                    await using (stream.ConfigureAwait(false))
                    await using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 128 * 1024, useAsync: true))
                    {
                        await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    }

                    var fileLength = new FileInfo(zipPath).Length;
                    totalBytesDownloaded += fileLength;
                    totalBatchesDownloaded++;

                    if (totalBatchesDownloaded % 100 == 0)
                    {
                        Console.WriteLine($"[ContentImporter] Downloaded {totalBatchesDownloaded} batches, {totalBytesDownloaded:N0}/{maxBytes:N0} bytes.");
                    }

                    if (totalBytesDownloaded >= maxBytes)
                    {
                        Console.WriteLine($"[ContentImporter] Reached download limit after {totalBatchesDownloaded} batches ({totalBytesDownloaded:N0}/{maxBytes:N0} bytes). Stopping.");
                        break;
                    }
                }

                var last = batchIds[^1];
                lastCreatedOn = last.CreatedOn;
                lastId = last.Id;
            }
        }

        private static async Task<List<(Guid Id, DateTime CreatedOn)>> GetBatchIdsPageAsync(
            string targetConnectionString,
            int pageSize,
            DateTime? lastCreatedOn,
            Guid? lastId,
            CancellationToken cancellationToken)
        {
            await using var sqlConnection = new SqlConnection(targetConnectionString);
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var cmd = sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;

            if (lastCreatedOn is null || lastId is null)
            {
                cmd.CommandText = @"SELECT TOP (@pageSize) [Id], [CreatedOn]
FROM [Batch]
ORDER BY [CreatedOn] DESC, [Id] DESC;";
            }
            else
            {
                cmd.CommandText = @"SELECT TOP (@pageSize) [Id], [CreatedOn]
FROM [Batch]
WHERE ([CreatedOn] < @lastCreatedOn) OR ([CreatedOn] = @lastCreatedOn AND [Id] < @lastId)
ORDER BY [CreatedOn] DESC, [Id] DESC;";

                cmd.Parameters.Add(new SqlParameter("@lastCreatedOn", SqlDbType.DateTime2) { Value = lastCreatedOn.Value });
                cmd.Parameters.Add(new SqlParameter("@lastId", SqlDbType.UniqueIdentifier) { Value = lastId.Value });
            }

            cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });

            var results = new List<(Guid Id, DateTime CreatedOn)>(pageSize);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                results.Add((reader.GetGuid(0), reader.GetDateTime(1)));
            }

            return results;
        }

        private static void RecreateEmptyDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }

            Directory.CreateDirectory(directoryPath);
        }

        private static string GetMostSignificantByteHex(Guid guid)
        {
            Span<byte> bytes = stackalloc byte[16];
            if (!guid.TryWriteBytes(bytes))
            {
                bytes = guid.ToByteArray();
            }

            return bytes[0].ToString("X2");
        }
    }
}