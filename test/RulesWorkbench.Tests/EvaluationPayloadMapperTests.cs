using RulesWorkbench.Contracts;
using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
    public sealed class EvaluationPayloadMapperTests
    {
        [Fact]
        public void Validate_WhenMissingId_ReturnsError()
        {
            var mapper = new EvaluationPayloadMapper();
            var payload = EvaluationPayloadDto.CreateDefault();
            payload.Id = string.Empty;

            var result = mapper.Validate(payload);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.Contains("Id is required", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Validate_WhenSecurityTokensEmpty_ReturnsError()
        {
            var mapper = new EvaluationPayloadMapper();
            var payload = EvaluationPayloadDto.CreateDefault();
            payload.SecurityTokens = new List<string>();

            var result = mapper.Validate(payload);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.Contains("SecurityTokens", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Validate_WhenDuplicatePropertyNames_ReturnsError()
        {
            var mapper = new EvaluationPayloadMapper();
            var payload = EvaluationPayloadDto.CreateDefault();
            payload.Properties = new List<EvaluationPayloadPropertyDto>
            {
                new() { Name = "Foo", Type = "String", Value = "a" },
                new() { Name = "foo", Type = "String", Value = "b" },
            };

            var result = mapper.Validate(payload);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void TryMapToIndexRequest_WhenValid_ReturnsIndexRequest()
        {
            var mapper = new EvaluationPayloadMapper();
            var payload = EvaluationPayloadDto.CreateDefault();
            payload.Id = "abc";
            payload.Timestamp = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
            payload.SecurityTokens = new List<string> { "PUBLIC" };
            payload.Properties = new List<EvaluationPayloadPropertyDto>
            {
                new() { Name = "Title", Type = "String", Value = "Test" },
            };
            payload.Files = new List<EvaluationPayloadFileDto>
            {
                new() { Filename = "a.txt", Size = 1, Timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), MimeType = "text/plain" },
            };

            var (request, validation) = mapper.TryMapToIndexRequest(payload);

            validation.IsValid.ShouldBeTrue();
            request.ShouldNotBeNull();
            request!.Id.ShouldBe("abc");
            request.SecurityTokens.ShouldBe(new[] { "PUBLIC" });
            request.Properties.Count.ShouldBe(1);
            request.Files.Count.ShouldBe(1);
            request.Files[0].MimeType.ShouldBe("text/plain");
        }
    }
}
