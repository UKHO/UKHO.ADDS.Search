using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using UKHO.ADDS.Search.Configuration;

namespace FileShareImageBuilder;

public sealed class DataCleaner
{
    public async Task DeleteInvalidBatchesAsync(CancellationToken cancellationToken = default)
    {
        var dataImagePath = ConfigurationReader.GetDataImagePath();
        var invalidFilePath = Path.Combine(dataImagePath, "invalid.json");
        if (!File.Exists(invalidFilePath))
        {
            return;
        }

        List<Guid>? invalidIds;
        await using (var stream = File.OpenRead(invalidFilePath))
        {
            invalidIds = await JsonSerializer.DeserializeAsync<List<Guid>>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (invalidIds is null || invalidIds.Count == 0)
        {
            return;
        }

        var targetConnectionString = ConfigurationReader.GetTargetDatabaseConnectionString(StorageNames.FileShareEmulatorDatabase);

        await using var sqlConnection = new SqlConnection(targetConnectionString);
        await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        foreach (var batchId in invalidIds.Distinct())
        {
            await DeleteBatchAsync(sqlConnection, batchId, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task DeleteBatchAsync(SqlConnection sqlConnection, Guid batchId, CancellationToken cancellationToken)
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
WHERE [Id] = @id;";

        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = batchId });
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
