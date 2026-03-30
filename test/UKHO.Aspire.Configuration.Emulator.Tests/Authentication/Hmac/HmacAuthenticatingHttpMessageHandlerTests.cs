using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using UKHO.ADDS.Aspire.Configuration.Emulator.Authentication.Hmac;
using UKHO.Aspire.Configuration.Emulator.Tests.TestSupport;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.Authentication.Hmac
{
    /// <summary>
    /// Verifies that <see cref="HmacAuthenticatingHttpMessageHandler"/> adds the expected HMAC authentication headers to outbound requests.
    /// </summary>
    public sealed class HmacAuthenticatingHttpMessageHandlerTests
    {
        private const string Credential = "catalog-client";
        private static readonly string Secret = Convert.ToBase64String(Encoding.UTF8.GetBytes("catalog-secret"));

        /// <summary>
        /// Verifies that outbound requests receive a valid HMAC authorization header, date header, and content hash.
        /// </summary>
        [Fact]
        public async Task SendAsync_WhenRequestContainsBody_ShouldAddExpectedAuthenticationHeaders()
        {
            // Arrange a fake transport so the handler can be exercised without issuing a real HTTP request.
            var innerHandler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            using var client = new HttpClient(new HmacAuthenticatingHttpMessageHandler(Credential, Secret)
            {
                InnerHandler = innerHandler
            })
            {
                BaseAddress = new Uri("https://config.local.test:8443")
            };

            using var request = new HttpRequestMessage(HttpMethod.Put, "/kv/catalog?api-version=1.0")
            {
                Content = new StringContent("{\"enabled\":true}", Encoding.UTF8, "application/json")
            };

            // Act by sending the request through the authenticating handler.
            using var response = await client.SendAsync(request);
            var capturedRequest = Assert.Single(innerHandler.Requests);

            // Assert that the response succeeded and the outbound headers match the expected HMAC protocol values.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(capturedRequest.Headers.ContainsKey("Authorization"));
            Assert.True(capturedRequest.Headers.ContainsKey("Date"));
            Assert.True(capturedRequest.Headers.ContainsKey("x-ms-content-sha256"));

            var authorization = AuthenticationHeaderValue.Parse(capturedRequest.Headers["Authorization"].Single());
            var parameters = ParseAuthorizationParameters(authorization.Parameter);
            var date = DateTimeOffset.Parse(capturedRequest.Headers["Date"].Single());
            var contentHash = capturedRequest.Headers["x-ms-content-sha256"].Single();
            var expectedSignature = ComputeSignature(
                HttpMethod.Put.Method,
                "/kv/catalog?api-version=1.0",
                "config.local.test:8443",
                date,
                contentHash);

            Assert.Equal("HMAC-SHA256", authorization.Scheme);
            Assert.Equal(Credential, parameters["Credential"]);
            Assert.Equal("date;host;x-ms-content-sha256", parameters["SignedHeaders"]);
            Assert.Equal(expectedSignature, parameters["Signature"]);
        }

        /// <summary>
        /// Verifies that an empty request body still receives the correct SHA-256 hash for an empty payload.
        /// </summary>
        [Fact]
        public async Task SendAsync_WhenRequestHasNoBody_ShouldHashEmptyPayload()
        {
            // Arrange a request without content so the handler must hash an empty payload.
            var innerHandler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            using var client = new HttpClient(new HmacAuthenticatingHttpMessageHandler(Credential, Secret)
            {
                InnerHandler = innerHandler
            })
            {
                BaseAddress = new Uri("https://config.local.test:8443")
            };

            // Act by sending a request without a body.
            using var response = await client.GetAsync("/labels?api-version=1.0");
            var capturedRequest = Assert.Single(innerHandler.Requests);

            // Assert that the x-ms-content-sha256 header matches the SHA-256 hash of an empty byte sequence.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(Convert.ToBase64String(SHA256.HashData([])), capturedRequest.Headers["x-ms-content-sha256"].Single());
        }

        /// <summary>
        /// Parses the authorization-header parameter string into a lookup dictionary.
        /// </summary>
        /// <param name="parameter">The authorization-header parameter string to parse.</param>
        /// <returns>A case-insensitive dictionary of HMAC authorization parameters.</returns>
        private static IDictionary<string, string> ParseAuthorizationParameters(string? parameter)
        {
            // Split the wire-format parameter string so assertions can target individual HMAC components.
            return (parameter ?? string.Empty)
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2, StringSplitOptions.None))
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Computes the expected outbound HMAC signature for the supplied request components.
        /// </summary>
        /// <param name="method">The HTTP method written into the string-to-sign.</param>
        /// <param name="pathAndQuery">The request path and query written into the string-to-sign.</param>
        /// <param name="authority">The authority value written into the string-to-sign.</param>
        /// <param name="date">The outbound request date header.</param>
        /// <param name="contentHash">The outbound request content hash.</param>
        /// <returns>The Base64-encoded HMAC signature.</returns>
        private static string ComputeSignature(string method, string pathAndQuery, string authority, DateTimeOffset date, string contentHash)
        {
            // Recreate the production string-to-sign so the assertion validates the generated signature rather than merely checking presence.
            var stringToSign = $"{method.ToUpperInvariant()}\n{pathAndQuery}\n{date:R};{authority};{contentHash}";
            using var hmac = new HMACSHA256(Convert.FromBase64String(Secret));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(stringToSign)));
        }
    }
}
