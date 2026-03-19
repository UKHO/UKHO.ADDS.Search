using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
    public sealed class BatchScanServiceTests
    {
        [Fact]
        public async Task GetBatchesForBusinessUnitAsync_WhenBusinessUnitIdIsEmpty_ReturnsFailure()
        {
            var service = CreateService(string.Empty);

            var result = await service.GetBatchesForBusinessUnitAsync(0, 10, CancellationToken.None);

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldBe("Business unit id is required.");
        }

        [Fact]
        public async Task GetBatchesForBusinessUnitAsync_WhenMaxRowsIsLessThanOne_ReturnsFailure()
        {
            var service = CreateService(string.Empty);

            var result = await service.GetBatchesForBusinessUnitAsync(123, 0, CancellationToken.None);

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldBe("Max rows must be greater than zero.");
        }

        [Fact]
        public async Task GetBatchesForBusinessUnitAsync_WhenConnectionFails_ReturnsFailure()
        {
            var service = CreateService(string.Empty);

            var result = await service.GetBatchesForBusinessUnitAsync(123, 10, CancellationToken.None);

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
            result.Batches.ShouldBeEmpty();
        }

        private static BatchScanService CreateService(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            return new BatchScanService(connection, NullLogger<BatchScanService>.Instance);
        }
    }
}
