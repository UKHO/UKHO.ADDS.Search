using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using UKHO.ADDS.Search.Configuration;

namespace FileShareImageBuilder;

public sealed class DataCleaner
{
    public async Task CleanAsync(CancellationToken cancellationToken = default)
    {
        var dataImagePath = ConfigurationReader.GetDataImagePath();
        var invalidFilePath = Path.Combine(dataImagePath, "invalid.json");

        // The local DB should contain only:
        // - committed batches that were successfully downloaded, and
        // - no batches explicitly marked invalid.
        var invalidIds = await ReadInvalidIdsAsync(invalidFilePath, cancellationToken).ConfigureAwait(false);
        var downloadedBatchIds = GetDownloadedBatchIds(dataImagePath);

        Console.WriteLine($"[DataCleaner] Downloaded batch zip files found: {downloadedBatchIds.Count}");
        Console.WriteLine($"[DataCleaner] Invalid batch ids found: {invalidIds.Count}");

        var targetConnectionString = ConfigurationReader.GetTargetDatabaseConnectionString(StorageNames.FileShareEmulatorDatabase);

        await using var sqlConnection = new SqlConnection(targetConnectionString);
        await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var deletedInvalidBatchIds = 0;
        var deletedInvalidRows = 0;
        foreach (var batchId in invalidIds)
        {
            var result = await DeleteBatchAsync(sqlConnection, batchId, cancellationToken).ConfigureAwait(false);
            if (result.BatchDeleted)
            {
                deletedInvalidBatchIds++;
            }

            deletedInvalidRows += result.RowsAffected;
        }

        Console.WriteLine($"[DataCleaner] Deleted invalid batch ids: {deletedInvalidBatchIds}");
        Console.WriteLine($"[DataCleaner] Deleted invalid rows: {deletedInvalidRows}");

        if (invalidIds.Count > 0)
        {
            try
            {
                File.Delete(invalidFilePath);
                Console.WriteLine($"[DataCleaner] Deleted invalid file: {invalidFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataCleaner] Failed to delete invalid file '{invalidFilePath}': {ex.GetType().Name}: {ex.Message}");
            }
        }

        var deletedNotDownloaded = await DeleteCommittedBatchesNotDownloadedAsync(sqlConnection, downloadedBatchIds, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"[DataCleaner] Deleted committed batches not downloaded: {deletedNotDownloaded}");

        var deletedNonCommitted = await DeleteNonCommittedBatchesAsync(sqlConnection, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"[DataCleaner] Deleted non-committed batches: {deletedNonCommitted}");
    }

    private static async Task<HashSet<Guid>> ReadInvalidIdsAsync(string invalidFilePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(invalidFilePath))
        {
            return new HashSet<Guid>();
        }

        await using var stream = File.OpenRead(invalidFilePath);
        var ids = await JsonSerializer.DeserializeAsync<List<Guid>>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return ids is { Count: > 0 }
            ? new HashSet<Guid>(ids)
            : new HashSet<Guid>();
    }

    private static HashSet<Guid> GetDownloadedBatchIds(string dataImagePath)
    {
        var contentDir = Path.Combine(dataImagePath, "bin", "content");
        if (!Directory.Exists(contentDir))
        {
            return new HashSet<Guid>();
        }

        var ids = new HashSet<Guid>();
        foreach (var file in Directory.EnumerateFiles(contentDir, "*.zip", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (Guid.TryParseExact(name, "D", out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private sealed record DeleteBatchResult(bool BatchDeleted, int RowsAffected);

    private static async Task<DeleteBatchResult> DeleteBatchAsync(SqlConnection sqlConnection, Guid batchId, CancellationToken cancellationToken)
    {
        await using var cmd = sqlConnection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 30;
        cmd.CommandText = @"DELETE FA
FROM [FileAttribute] FA
JOIN [File] F ON F.[Id] = FA.[FileId]
WHERE F.[BatchId] = @id;

DELETE FROM [BatchReadGroup]
WHERE [BatchId] = @id;

DELETE FROM [BatchReadUser]
WHERE [BatchId] = @id;

DELETE FROM [BatchAttribute]
WHERE [BatchId] = @id;

DELETE FROM [File]
WHERE [BatchId] = @id;

DELETE FROM [Batch]
WHERE [Id] = @id;

SELECT @@ROWCOUNT;";

        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = batchId });
        var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        // We can't reliably infer whether the Batch row was deleted from total affected rows.
        // Perform a targeted delete to detect the batch delete outcome without re-deleting dependents.
        await using var batchDeleteCmd = sqlConnection.CreateCommand();
        batchDeleteCmd.CommandType = CommandType.Text;
        batchDeleteCmd.CommandTimeout = 30;
        batchDeleteCmd.CommandText = @"DELETE FROM [Batch]
WHERE [Id] = @id;";
        batchDeleteCmd.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = batchId });
        var batchDeleted = await batchDeleteCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) > 0;

        return new DeleteBatchResult(batchDeleted, rowsAffected);
    }

    private static async Task<int> DeleteCommittedBatchesNotDownloadedAsync(
        SqlConnection sqlConnection,
        HashSet<Guid> downloadedBatchIds,
        CancellationToken cancellationToken)
    {
        await using var cmd = sqlConnection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 0;

        if (downloadedBatchIds.Count == 0)
        {
            cmd.CommandText = @"DELETE FA
FROM [FileAttribute] FA
JOIN [File] F ON F.[Id] = FA.[FileId]
JOIN [Batch] B ON B.[Id] = F.[BatchId]
WHERE B.[Status] = 3;

DELETE FROM [BatchReadGroup]
WHERE [BatchId] IN (SELECT [Id] FROM [Batch] WHERE [Status] = 3);

DELETE FROM [BatchReadUser]
WHERE [BatchId] IN (SELECT [Id] FROM [Batch] WHERE [Status] = 3);

DELETE FROM [BatchAttribute]
WHERE [BatchId] IN (SELECT [Id] FROM [Batch] WHERE [Status] = 3);

DELETE FROM [File]
WHERE [BatchId] IN (SELECT [Id] FROM [Batch] WHERE [Status] = 3);

DELETE FROM [Batch]
WHERE [Status] = 3;";

            return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var paramNames = new List<string>(downloadedBatchIds.Count);
        var i = 0;
        foreach (var id in downloadedBatchIds)
        {
            var name = $"@id{i++}";
            paramNames.Add(name);
            cmd.Parameters.Add(new SqlParameter(name, SqlDbType.UniqueIdentifier) { Value = id });
        }

        var idList = string.Join(",", paramNames);
        cmd.CommandText = $@"DELETE FA
FROM [FileAttribute] FA
JOIN [File] F ON F.[Id] = FA.[FileId]
JOIN [Batch] B ON B.[Id] = F.[BatchId]
WHERE B.[Status] = 3
  AND B.[Id] NOT IN ({idList});

DELETE BRG
FROM [BatchReadGroup] BRG
JOIN [Batch] B ON B.[Id] = BRG.[BatchId]
WHERE B.[Status] = 3
  AND B.[Id] NOT IN ({idList});

DELETE BRU
FROM [BatchReadUser] BRU
JOIN [Batch] B ON B.[Id] = BRU.[BatchId]
WHERE B.[Status] = 3
  AND B.[Id] NOT IN ({idList});

DELETE BA
FROM [BatchAttribute] BA
JOIN [Batch] B ON B.[Id] = BA.[BatchId]
WHERE B.[Status] = 3
  AND B.[Id] NOT IN ({idList});

DELETE F
FROM [File] F
JOIN [Batch] B ON B.[Id] = F.[BatchId]
WHERE B.[Status] = 3
  AND B.[Id] NOT IN ({idList});

DELETE FROM [Batch]
WHERE [Status] = 3
  AND [Id] NOT IN ({idList});";

        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> DeleteNonCommittedBatchesAsync(SqlConnection sqlConnection, CancellationToken cancellationToken)
    {
        await using var cmd = sqlConnection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 0;

        cmd.CommandText = @"DELETE FA
FROM [FileAttribute] FA
JOIN [File] F ON F.[Id] = FA.[FileId]
JOIN [Batch] B ON B.[Id] = F.[BatchId]
WHERE B.[Status] <> 3;

DELETE BRG
FROM [BatchReadGroup] BRG
JOIN [Batch] B ON B.[Id] = BRG.[BatchId]
WHERE B.[Status] <> 3;

DELETE BRU
FROM [BatchReadUser] BRU
JOIN [Batch] B ON B.[Id] = BRU.[BatchId]
WHERE B.[Status] <> 3;

DELETE BA
FROM [BatchAttribute] BA
JOIN [Batch] B ON B.[Id] = BA.[BatchId]
WHERE B.[Status] <> 3;

DELETE F
FROM [File] F
JOIN [Batch] B ON B.[Id] = F.[BatchId]
WHERE B.[Status] <> 3;

DELETE FROM [Batch]
WHERE [Status] <> 3;";

        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
