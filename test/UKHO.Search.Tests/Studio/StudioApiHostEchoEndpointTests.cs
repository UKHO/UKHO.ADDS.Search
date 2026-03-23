using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Shouldly;
using StudioApiHost;
using Xunit;

namespace UKHO.Search.Tests.Studio
{
    public sealed class StudioApiHostEchoEndpointTests
    {
        [Fact]
        public async Task GetEcho_WhenRequested_ShouldReturnStudioApiHostMessage()
        {
            var app = StudioApiHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["SkipAddsConfiguration"] = "true",
                        ["rules:file-share:rule-1"] = """
                            {
                              "schemaVersion": "1.0",
                              "rule": {
                                "id": "rule-1",
                                "title": "Studio API host echo endpoint test rule",
                                "if": { "path": "id", "exists": true },
                                "then": { "keywords": { "add": [ "k" ] } }
                              }
                            }
                            """
                    });

                    // This test builds the host directly so it can bypass Aspire-only startup configuration.
                    builder.WebHost.UseTestServer();
                });

            try
            {
                await app.StartAsync();
                using var client = app.GetTestClient();

                var response = await client.GetAsync("/echo");

                response.StatusCode.ShouldBe(HttpStatusCode.OK);

                var content = await response.Content.ReadAsStringAsync();

                content.ShouldBe("Hello from StudioApiHost echo.");
            }
            finally
            {
                await app.DisposeAsync();
            }
        }
    }
}
