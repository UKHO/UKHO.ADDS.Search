using System.Data;
using Microsoft.Data.SqlClient;
using UKHO.Search.Configuration;

namespace FileShareEmulator.Services;

public sealed class StatisticsService
{
    private readonly SqlConnection _sqlConnection;

    public StatisticsService(SqlConnection sqlConnection)
    {
        _sqlConnection = sqlConnection;
    }

    public async Task<StatisticsSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var cmd = _sqlConnection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 30;

        cmd.CommandText = @"
SELECT
    (SELECT COUNT_BIG(1) FROM [Batch]) AS BatchCount,
    (SELECT COUNT_BIG(1) FROM [File]) AS FileCount,
    (SELECT COUNT_BIG(1) FROM [BatchAttribute]) AS BatchAttributeCount,
    (SELECT COUNT_BIG(1) FROM [FileAttribute]) AS FileAttributeCount,
    (SELECT COUNT_BIG(1) FROM [BatchReadUser]) AS BatchReadUserCount,
    (SELECT COUNT_BIG(1) FROM [BatchReadGroup]) AS BatchReadGroupCount;";

        if (_sqlConnection.State != ConnectionState.Open)
        {
            await _sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken)
            .ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return new StatisticsSnapshot(0, 0, 0, 0, 0, 0);
        }

        return new StatisticsSnapshot(
            BatchCount: checked((int)reader.GetInt64(0)),
            FileCount: checked((int)reader.GetInt64(1)),
            BatchAttributeCount: checked((int)reader.GetInt64(2)),
            FileAttributeCount: checked((int)reader.GetInt64(3)),
            BatchReadUserCount: checked((int)reader.GetInt64(4)),
            BatchReadGroupCount: checked((int)reader.GetInt64(5)));
    }
}
