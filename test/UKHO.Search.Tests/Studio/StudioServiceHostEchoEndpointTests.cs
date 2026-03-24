using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Shouldly;
using StudioServiceHost;
using Xunit;

namespace UKHO.Search.Tests.Studio
{
    /// <summary>
    /// Verifies the lightweight echo endpoint exposed by the Studio service host.
    /// </summary>
    public sealed class StudioServiceHostEchoEndpointTests
    {
        /// <summary>
        /// Verifies that the echo endpoint returns the renamed Studio service host message.
        /// </summary>
        [Fact]
        public async Task GetEcho_WhenRequested_ShouldReturnStudioServiceHostMessage()
        {
            // Build the host directly with an in-memory rules configuration so the echo endpoint can be exercised in isolation.
            var app = StudioServiceHostApplication.BuildApp(
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
                                "title": "Studio service host echo endpoint test rule",
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

                // Request the unchanged echo route and verify the renamed payload.
                var response = await client.GetAsync("/echo");

                response.StatusCode.ShouldBe(HttpStatusCode.OK);

                var content = await response.Content.ReadAsStringAsync();

                content.ShouldBe("Hello from StudioServiceHost echo.");
            }
            finally
            {
                // Dispose the host once the assertion has completed.
                await app.DisposeAsync();
            }
        }
    }
}
