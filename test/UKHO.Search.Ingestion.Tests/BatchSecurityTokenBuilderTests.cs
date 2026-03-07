using FileShareEmulator.Services;
using Shouldly;
using Xunit;

namespace UKHO.Search.Ingestion.Tests
{
    public sealed class BatchSecurityTokenBuilderTests
    {
        [Fact]
        public void BuildTokens_AlwaysIncludesBatchCreate()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens([], [], businessUnitName: null);

            tokens.ShouldBe(["batchcreate"]);
        }

        [Fact]
        public void BuildTokens_WhenBusinessUnitIsProvided_AddsBuToken()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens([], [], businessUnitName: "Sales");

            tokens.ShouldBe(["batchcreate", "batchcreate_sales"]);
        }

        [Fact]
        public void BuildTokens_NormalisesToLowerCase_Trims_AndFiltersBlanks()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens(
                groupIdentifiers: [" GroupA ", "  ", "GROUPB"],
                userIdentifiers: [" User1 ", "USER2"],
                businessUnitName: "  FiShErIeS  ");

            tokens.ShouldBe(["batchcreate", "batchcreate_fisheries", "groupa", "groupb", "user1", "user2"]);
        }

        [Fact]
        public void BuildTokens_DeduplicatesAcrossStandardGroupAndUserTokens()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens(
                groupIdentifiers: ["BatchCreate", "GroupA", "groupa"],
                userIdentifiers: ["GROUPA", "User1"],
                businessUnitName: "Sales");

            tokens.ShouldBe(["batchcreate", "batchcreate_sales", "groupa", "user1"]);
        }

        [Fact]
        public void BuildTokens_SortsGroupsAndUsersIndependently()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens(
                groupIdentifiers: ["z", "a", "m"],
                userIdentifiers: ["u2", "u1"],
                businessUnitName: null);

            tokens.ShouldBe(["batchcreate", "a", "m", "z", "u1", "u2"]);
        }

        [Fact]
        public void BuildTokens_WhenBusinessUnitIsBlank_DoesNotAddBuToken()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens([], [], businessUnitName: "  ");

            tokens.ShouldBe(["batchcreate"]);
        }
    }
}
