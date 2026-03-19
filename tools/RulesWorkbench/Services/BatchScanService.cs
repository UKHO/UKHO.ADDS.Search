using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RulesWorkbench.Contracts;

namespace RulesWorkbench.Services
{
    public sealed class BatchScanService
    {
        private readonly SqlConnection _sqlConnection;
        private readonly ILogger<BatchScanService> _logger;

        public BatchScanService(SqlConnection sqlConnection, ILogger<BatchScanService> logger)
        {
            _sqlConnection = sqlConnection;
            _logger = logger;
        }

        public async Task<BatchScanResultDto> GetBatchesForBusinessUnitAsync(int businessUnitId, int maxRows, CancellationToken cancellationToken)
        {
            if (businessUnitId <= 0)
            {
                return BatchScanResultDto.Failure("Business unit id is required.");
            }

            if (maxRows <= 0)
            {
                return BatchScanResultDto.Failure("Max rows must be greater than zero.");
            }

            try
            {
                await using var connection = new SqlConnection(_sqlConnection.ConnectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                await using var cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 30;
                cmd.CommandText = @"SELECT TOP (@maxRows) [Id], [CreatedOn]
FROM [Batch]
WHERE [BusinessUnitId] = @businessUnitId
ORDER BY [CreatedOn] ASC, [Id] ASC;";
                cmd.Parameters.Add(new SqlParameter("@maxRows", SqlDbType.Int) { Value = maxRows });
                cmd.Parameters.Add(new SqlParameter("@businessUnitId", SqlDbType.Int) { Value = businessUnitId });

                var batches = new List<BatchScanBatchDto>();
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (reader.IsDBNull(0) || reader.IsDBNull(1))
                    {
                        continue;
                    }

                    var createdOnValue = reader.GetValue(1);
                    var createdOn = createdOnValue switch
                    {
                        DateTimeOffset dto => dto,
                        DateTime dt => new DateTimeOffset(dt),
                        var value => throw new InvalidOperationException($"Batch.CreatedOn has unexpected type '{value.GetType().FullName}'.")
                    };

                    batches.Add(new BatchScanBatchDto
                    {
                        BatchId = reader.GetGuid(0),
                        CreatedOn = createdOn,
                    });
                }

                _logger.LogInformation(
                    "Loaded {BatchCount} batches for checker scan. BusinessUnitId={BusinessUnitId} MaxRows={MaxRows}",
                    batches.Count,
                    businessUnitId,
                    maxRows);

                return BatchScanResultDto.Success(batches);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "DB error while loading batches for checker scan. BusinessUnitId={BusinessUnitId}", businessUnitId);
                return BatchScanResultDto.Failure("Database error while loading batches for scan.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unexpected error while loading batches for checker scan. BusinessUnitId={BusinessUnitId}", businessUnitId);
                return BatchScanResultDto.Failure(ex.Message);
            }
        }
    }
}
