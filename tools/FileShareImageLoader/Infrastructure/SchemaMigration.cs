using System.Data;
using Microsoft.Data.SqlClient;

namespace FileShareImageLoader.Infrastructure;

public sealed class SchemaMigration
{
    public async Task ApplyAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureIndexStatusColumnAsync(connectionString, cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            Console.Error.WriteLine($"[SchemaMigration] Failed: {ex.Message}");
            throw;
        }
    }

    private static async Task EnsureIndexStatusColumnAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"[SchemaMigration] Connected. Database='{connection.Database}' DataSource='{connection.DataSource}'");

        var batchExists = await ObjectExistsAsync(connection, "dbo", "Batch", "U", cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"[SchemaMigration] dbo.Batch exists: {batchExists}");

        var indexStatusExistsBefore = await ColumnExistsAsync(connection, "dbo", "Batch", "IndexStatus", cancellationToken)
            .ConfigureAwait(false);
        Console.WriteLine($"[SchemaMigration] dbo.Batch.IndexStatus exists (before): {indexStatusExistsBefore}");

        await using var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 30;

        cmd.CommandText = @"
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID(N'[dbo].[Batch]')
      AND c.name = N'IndexStatus'
)
BEGIN
    ALTER TABLE [dbo].[Batch]
        ADD [IndexStatus] INT NOT NULL
            CONSTRAINT [DF_Batch_IndexStatus] DEFAULT (0);
END
";

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        var indexStatusExistsAfter = await ColumnExistsAsync(connection, "dbo", "Batch", "IndexStatus", cancellationToken)
            .ConfigureAwait(false);
        Console.WriteLine($"[SchemaMigration] dbo.Batch.IndexStatus exists (after): {indexStatusExistsAfter}");
    }

    private static async Task<bool> ObjectExistsAsync(SqlConnection connection, string schema, string name, string type,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 30;
        cmd.CommandText = @"
SELECT 1
FROM sys.objects o
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE s.name = @schema AND o.name = @name AND o.type = @type;";
        cmd.Parameters.Add(new SqlParameter("@schema", SqlDbType.NVarChar, 128) { Value = schema });
        cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 128) { Value = name });
        cmd.Parameters.Add(new SqlParameter("@type", SqlDbType.NChar, 2) { Value = type });

        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is not null && result is not DBNull;
    }

    private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string schema, string table, string column,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 30;
        cmd.CommandText = @"
SELECT 1
FROM sys.columns c
JOIN sys.objects o ON o.object_id = c.object_id
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE s.name = @schema AND o.name = @table AND c.name = @column;";
        cmd.Parameters.Add(new SqlParameter("@schema", SqlDbType.NVarChar, 128) { Value = schema });
        cmd.Parameters.Add(new SqlParameter("@table", SqlDbType.NVarChar, 128) { Value = table });
        cmd.Parameters.Add(new SqlParameter("@column", SqlDbType.NVarChar, 128) { Value = column });

        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is not null && result is not DBNull;
    }
}
