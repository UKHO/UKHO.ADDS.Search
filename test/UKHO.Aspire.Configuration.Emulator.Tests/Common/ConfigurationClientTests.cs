using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using UKHO.Aspire.Configuration.Emulator.Tests.TestSupport;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.Common
{
    /// <summary>
    /// Verifies request shaping, pagination, and payload materialization behaviour for <see cref="ConfigurationClient"/>.
    /// </summary>
    public sealed class ConfigurationClientTests
    {
        /// <summary>
        /// Verifies that configuration-setting enumeration follows pagination links, forwards the requested filters, and materializes settings.
        /// </summary>
        [Fact]
        public async Task GetConfigurationSettings_WhenPagedResponseReturned_ShouldFollowLinksAndMaterializeSettings()
        {
            // Arrange a paged configuration-setting response sequence and a fake transport that records each request.
            var responses = new Queue<HttpResponseMessage>([
                CreateJsonResponse(
                    """
                    {
                      "items": [
                        {
                          "etag": "etag-1",
                          "key": "catalog:endpoint",
                          "label": "dev",
                          "content_type": "application/json",
                          "value": "https://catalog.dev.example.test",
                          "tags": {
                            "team": "search"
                          },
                          "locked": false,
                          "last_modified": "2026-03-30T12:00:00+00:00"
                        }
                      ]
                    }
                    """,
                    linkHeader: "</kv?after=page-2&api-version=1.0>; rel=\"next\""),
                CreateJsonResponse(
                    """
                    {
                      "items": [
                        {
                          "etag": "etag-2",
                          "key": "catalog:enabled",
                          "label": null,
                          "content_type": null,
                          "value": "true",
                          "tags": {},
                          "locked": true,
                          "last_modified": "2026-03-30T13:00:00+00:00"
                        }
                      ]
                    }
                    """)
            ]);
            var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(responses.Dequeue()));
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://config.local.test")
            };
            var client = new ConfigurationClient(httpClient, new ConfigurationSettingFactory());
            var moment = new DateTimeOffset(2026, 3, 30, 14, 0, 0, TimeSpan.Zero);

            // Act by enumerating the asynchronous results into a list.
            var settings = await ToListAsync(client.GetConfigurationSettings("catalog:*", "dev", moment));

            // Assert that both pages were requested and the resulting settings preserve the response payload values.
            Assert.Equal(2, settings.Count);
            Assert.Equal("catalog:endpoint", settings[0].Key);
            Assert.Equal("https://catalog.dev.example.test", settings[0].Value);
            Assert.Equal("search", settings[0].Tags!["team"]);
            Assert.Equal("catalog:enabled", settings[1].Key);
            Assert.True(settings[1].Locked);

            Assert.Equal("https://config.local.test/kv?key=catalog:*&label=dev&api-version=1.0", handler.Requests[0].RequestUri);
            Assert.Equal(moment.ToString("R"), handler.Requests[0].Headers["Accept-Datetime"].Single());
            Assert.Equal("https://config.local.test/kv?after=page-2&api-version=1.0", handler.Requests[1].RequestUri);
        }

        /// <summary>
        /// Verifies that key enumeration follows pagination links and yields every key name from the paged response sequence.
        /// </summary>
        [Fact]
        public async Task GetKeys_WhenPagedResponseReturned_ShouldFollowLinksAndYieldEveryKey()
        {
            // Arrange two pages of key results and a fake transport that records each request URI.
            var responses = new Queue<HttpResponseMessage>([
                CreateJsonResponse("""
                {
                  "items": [
                    { "name": "catalog:endpoint" }
                  ]
                }
                """, linkHeader: "</keys?after=page-2&api-version=1.0>; rel=\"next\""),
                CreateJsonResponse("""
                {
                  "items": [
                    { "name": "catalog:enabled" }
                  ]
                }
                """)
            ]);
            var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(responses.Dequeue()));
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://config.local.test")
            };
            var client = new ConfigurationClient(httpClient, new ConfigurationSettingFactory());

            // Act by collecting the paged key enumeration.
            var keys = await ToListAsync(client.GetKeys());

            // Assert that the client followed the pagination link and returned both keys in order.
            Assert.Equal(new[] { "catalog:endpoint", "catalog:enabled" }, keys);
            Assert.Equal("https://config.local.test/keys?api-version=1.0", handler.Requests[0].RequestUri);
            Assert.Equal("https://config.local.test/keys?after=page-2&api-version=1.0", handler.Requests[1].RequestUri);
        }

        /// <summary>
        /// Verifies that label enumeration preserves null labels and follows pagination links.
        /// </summary>
        [Fact]
        public async Task GetLabels_WhenPagedResponseReturned_ShouldYieldLabelsIncludingNull()
        {
            // Arrange a two-page label result where the second page includes a null label value.
            var responses = new Queue<HttpResponseMessage>([
                CreateJsonResponse("""
                {
                  "items": [
                    { "name": "dev" }
                  ]
                }
                """, linkHeader: "</labels?after=page-2&api-version=1.0>; rel=\"next\""),
                CreateJsonResponse("""
                {
                  "items": [
                    { "name": null }
                  ]
                }
                """)
            ]);
            var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(responses.Dequeue()));
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://config.local.test")
            };
            var client = new ConfigurationClient(httpClient, new ConfigurationSettingFactory());

            // Act by collecting the label enumeration across both pages.
            var labels = await ToListAsync(client.GetLabels());

            // Assert that labels from both pages were returned, including the null label entry.
            Assert.Equal(2, labels.Count);
            Assert.Equal("dev", labels[0]);
            Assert.Null(labels[1]);
            Assert.Equal("https://config.local.test/labels?api-version=1.0", handler.Requests[0].RequestUri);
            Assert.Equal("https://config.local.test/labels?after=page-2&api-version=1.0", handler.Requests[1].RequestUri);
        }

        /// <summary>
        /// Verifies that setting updates are sent to the expected URI and body shape, including null-label encoding.
        /// </summary>
        [Fact]
        public async Task SetConfigurationSetting_WhenSettingProvided_ShouldShapePutRequestAndSerializeBody()
        {
            // Arrange a writable client and a setting with a null label so the null-label filter branch is exercised.
            var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://config.local.test")
            };
            var client = new ConfigurationClient(httpClient, new ConfigurationSettingFactory());
            var setting = new ConfigurationSetting(
                etag: "etag-3",
                key: "catalog:endpoint",
                lastModified: new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                locked: false,
                label: null,
                contentType: MediaType.Json,
                value: "https://catalog.example.test",
                tags: new Dictionary<string, string>
                {
                    ["team"] = "search"
                });

            // Act by sending the configuration-setting update.
            await client.SetConfigurationSetting(setting);

            // Assert that the request targeted the expected URI and serialized the expected body payload.
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("https://config.local.test/kv/catalog%3Aendpoint?label=%00&api-version=1.0", request.RequestUri);
            Assert.NotNull(request.Content);

            using var body = JsonDocument.Parse(request.Content);
            Assert.Equal("https://catalog.example.test", body.RootElement.GetProperty("value").GetString());
            Assert.Equal(MediaType.Json, body.RootElement.GetProperty("content_type").GetString());
            Assert.Equal("search", body.RootElement.GetProperty("tags").GetProperty("team").GetString());
        }

        /// <summary>
        /// Creates a JSON HTTP response with an optional pagination link header.
        /// </summary>
        /// <param name="json">The JSON payload to include in the response body.</param>
        /// <param name="linkHeader">The optional Link header used to model pagination.</param>
        /// <returns>A prepared <see cref="HttpResponseMessage"/> for the fake transport.</returns>
        private static HttpResponseMessage CreateJsonResponse(string json, string? linkHeader = null)
        {
            // Build a reusable JSON response message so pagination scenarios can be composed declaratively.
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(JsonDocument.Parse(json).RootElement)
            };

            if (!string.IsNullOrEmpty(linkHeader))
            {
                response.Headers.Add("Link", linkHeader);
            }

            return response;
        }

        /// <summary>
        /// Materializes an asynchronous sequence into a list for deterministic assertions.
        /// </summary>
        /// <typeparam name="T">The type produced by the asynchronous sequence.</typeparam>
        /// <param name="source">The asynchronous sequence to materialize.</param>
        /// <returns>A list containing the sequence items in enumeration order.</returns>
        private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
        {
            // Enumerate the sequence explicitly so the tests do not depend on optional LINQ async helpers.
            var results = new List<T>();
            await foreach (var item in source)
            {
                results.Add(item);
            }

            return results;
        }
    }
}
