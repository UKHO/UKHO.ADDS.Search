using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
    public sealed class BatchPayloadLoaderTests
    {
        [Fact]
        public async Task TryLoadAsync_WhenBatchIdEmpty_ReturnsFailed()
        {
            var loader = CreateLoader("Server=(local);Database=doesnotmatter;Trusted_Connection=True;");

            var result = await loader.TryLoadAsync("", CancellationToken.None);

            result.Found.ShouldBeFalse();
            result.Error.ShouldNotBeNull();
        }

        [Fact]
        public async Task TryLoadAsync_WhenBatchIdNotGuid_ReturnsNotFound()
        {
            var loader = CreateLoader("Server=(local);Database=doesnotmatter;Trusted_Connection=True;");

            var result = await loader.TryLoadAsync("not-a-guid", CancellationToken.None);

            result.Found.ShouldBeFalse();
            result.Error!.ToLowerInvariant().ShouldContain("not found");
        }

        private static BatchPayloadLoader CreateLoader(string connectionString)
        {
            var cnn = new SqlConnection(connectionString);
            return new BatchPayloadLoader(cnn, NullLogger<BatchPayloadLoader>.Instance);
        }
    }
}
