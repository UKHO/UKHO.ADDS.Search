using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using UKHO.ADDS.Aspire.Configuration.Emulator.Authentication.Hmac;
using UKHO.Aspire.Configuration.Emulator.Tests.TestSupport;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.Authentication.Hmac
{
    /// <summary>
    /// Verifies that <see cref="HmacHandler"/> accepts valid HMAC requests and rejects malformed or tampered requests with the expected failure reasons.
    /// </summary>
    public sealed class HmacHandlerTests
    {
        private const string Credential = "catalog-client";
        private static readonly string Secret = Convert.ToBase64String(Encoding.UTF8.GetBytes("catalog-secret"));

        /// <summary>
        /// Verifies that malformed authorization headers do not trigger HMAC authentication.
        /// </summary>
        [Fact]
        public async Task AuthenticateAsync_WhenAuthorizationHeaderMalformed_ShouldReturnNoResult()
        {
            // Arrange a request whose authorization header cannot be parsed as an authentication header value.
            var context = CreateHttpContext();
            context.Request.Headers.Authorization = "not-a-valid-auth-header";

            // Act by asking the handler to authenticate the malformed request.
            var result = await AuthenticateAsync(context);

            // Assert that the handler declines authentication rather than producing a failure for another scheme to handle.
            Assert.True(result.None);
            Assert.Null(result.Failure);
        }

        /// <summary>
        /// Verifies that missing required HMAC parameters produce an authentication failure.
        /// </summary>
        [Fact]
        public async Task AuthenticateAsync_WhenRequiredParameterMissing_ShouldFail()
        {
            // Arrange a request whose authorization header omits the required signature parameter.
            var context = CreateHttpContext();
            SetSignedHeaders(context, body: string.Empty, DateTimeOffset.UtcNow);
            context.Request.Headers.Authorization = "HMAC-SHA256 Credential=catalog-client&SignedHeaders=x-ms-date;host;x-ms-content-sha256";

            // Act by authenticating the incomplete request.
            var result = await AuthenticateAsync(context);

            // Assert that the missing parameter is reported explicitly.
            Assert.False(result.Succeeded);
            Assert.Equal("Signature parameter is required", result.Failure?.Message);
        }

        /// <summary>
        /// Verifies that expired HMAC timestamps are rejected.
        /// </summary>
        [Fact]
        public async Task AuthenticateAsync_WhenTokenExpired_ShouldFail()
        {
            // Arrange a request whose timestamp is outside the fifteen-minute validity window.
            var context = CreateHttpContext();
            SetAuthorizationHeader(context, body: string.Empty, date: DateTimeOffset.UtcNow.AddMinutes(-16));

            // Act by authenticating the expired request.
            var result = await AuthenticateAsync(context);

            // Assert that the handler rejects the stale token.
            Assert.False(result.Succeeded);
            Assert.Equal("The access token has expired", result.Failure?.Message);
        }

        /// <summary>
        /// Verifies that the supplied credential must match the configured credential exactly.
        /// </summary>
        [Fact]
        public async Task AuthenticateAsync_WhenCredentialInvalid_ShouldFail()
        {
            // Arrange a request that is correctly signed but identifies the wrong credential.
            var context = CreateHttpContext();
            SetAuthorizationHeader(context, body: string.Empty, date: DateTimeOffset.UtcNow, credential: "wrong-client");

            // Act by authenticating the request with the invalid credential.
            var result = await AuthenticateAsync(context);

            // Assert that the credential mismatch is surfaced as an authentication failure.
            Assert.False(result.Succeeded);
            Assert.Equal("Invalid credential", result.Failure?.Message);
        }

        /// <summary>
        /// Verifies that tampering with the signature causes authentication to fail.
        /// </summary>
        [Fact]
        public async Task AuthenticateAsync_WhenSignatureInvalid_ShouldFail()
        {
            // Arrange a request whose signature no longer matches the signed request components.
            var context = CreateHttpContext();
            SetAuthorizationHeader(context, body: string.Empty, date: DateTimeOffset.UtcNow, signature: "tampered-signature");

            // Act by authenticating the tampered request.
            var result = await AuthenticateAsync(context);

            // Assert that signature validation fails before the request body hash is trusted.
            Assert.False(result.Succeeded);
            Assert.Equal("Invalid signature", result.Failure?.Message);
        }

        /// <summary>
        /// Verifies that the request-body hash must match the signed body content.
        /// </summary>
        [Fact]
        public async Task AuthenticateAsync_WhenContentHashInvalid_ShouldFail()
        {
            // Arrange a request whose body hash header does not match the actual request body.
            const string body = "{\"enabled\":true}";
            var context = CreateHttpContext(body);
            SetAuthorizationHeader(context, body: body, date: DateTimeOffset.UtcNow, contentHash: "tampered-content-hash");

            // Act by authenticating the request with the invalid content hash.
            var result = await AuthenticateAsync(context);

            // Assert that body tampering is rejected after signature validation succeeds.
            Assert.False(result.Succeeded);
            Assert.Equal("Invalid request content hash", result.Failure?.Message);
        }

        /// <summary>
        /// Verifies that a valid HMAC request succeeds and produces an authenticated principal.
        /// </summary>
        [Fact]
        public async Task AuthenticateAsync_WhenRequestValid_ShouldSucceed()
        {
            // Arrange a fully valid request with matching headers, signature, and content hash.
            const string body = "{\"enabled\":true}";
            var context = CreateHttpContext(body);
            SetAuthorizationHeader(context, body: body, date: DateTimeOffset.UtcNow);

            // Act by authenticating the valid request.
            var result = await AuthenticateAsync(context);

            // Assert that the handler authenticates the request using the configured HMAC scheme.
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Principal);
            Assert.Equal(HmacDefaults.AuthenticationScheme, result.Ticket?.AuthenticationScheme);
        }

        /// <summary>
        /// Verifies that challenges emit the expected HMAC error details when authentication fails.
        /// </summary>
        [Fact]
        public async Task ChallengeAsync_WhenAuthenticationFails_ShouldWriteWwwAuthenticateHeader()
        {
            // Arrange a request that will fail credential validation so the challenge contains an explicit error description.
            var context = CreateHttpContext();
            SetAuthorizationHeader(context, body: string.Empty, date: DateTimeOffset.UtcNow, credential: "wrong-client");
            var handler = CreateHandler();
            await handler.InitializeAsync(CreateScheme(), context);

            // Act by issuing an authentication challenge for the invalid request.
            await handler.ChallengeAsync(new AuthenticationProperties());

            // Assert that the response advertises the HMAC challenge scheme and failure reason.
            Assert.Equal(401, context.Response.StatusCode);
            Assert.True(context.Response.Headers.TryGetValue("WWW-Authenticate", out var headerValue));
            Assert.Contains("HMAC-SHA256", headerValue.ToString(), StringComparison.Ordinal);
            Assert.Contains("error=\"invalid_token\"", headerValue.ToString(), StringComparison.Ordinal);
            Assert.Contains("error_description=\"Invalid credential\"", headerValue.ToString(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Creates a deterministic HTTP context for HMAC authentication tests.
        /// </summary>
        /// <param name="body">The request body that should be made available to the authentication handler.</param>
        /// <returns>A configured <see cref="DefaultHttpContext"/> with a predictable request target and host.</returns>
        private static DefaultHttpContext CreateHttpContext(string body = "")
        {
            // Build a request shape that exercises the handler's raw-target and host-signing behaviour.
            var context = new DefaultHttpContext();
            context.Features.Set<IHttpRequestFeature>(new HttpRequestFeature
            {
                RawTarget = "/kv/catalog?api-version=1.0"
            });
            context.Request.Method = HttpMethod.Get.Method;
            context.Request.Scheme = "https";
            context.Request.Path = "/kv/catalog";
            context.Request.QueryString = new QueryString("?api-version=1.0");
            context.Request.Headers.Host = "config.local.test:8443";
            context.Request.Headers["Host"] = "config.local.test:8443";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Response.Body = new MemoryStream();

            return context;
        }

        /// <summary>
        /// Creates the handler under test with deterministic options and no-op logging.
        /// </summary>
        /// <returns>A configured <see cref="HmacHandler"/> instance.</returns>
        private static HmacHandler CreateHandler()
        {
            // Supply the fixed credential and secret required by the helper methods that generate valid signatures.
            return new HmacHandler(
                new TestOptionsMonitor<HmacOptions>(new HmacOptions
                {
                    Credential = Credential,
                    Secret = Secret
                }),
                NullLoggerFactory.Instance,
                UrlEncoder.Default);
        }

        /// <summary>
        /// Creates the authentication scheme metadata used to initialize the handler.
        /// </summary>
        /// <returns>The HMAC authentication scheme definition.</returns>
        private static AuthenticationScheme CreateScheme()
        {
            // Use the production default scheme name so the resulting authentication ticket matches runtime behaviour.
            return new AuthenticationScheme(HmacDefaults.AuthenticationScheme, displayName: null, typeof(HmacHandler));
        }

        /// <summary>
        /// Authenticates the supplied HTTP context with a fresh handler instance.
        /// </summary>
        /// <param name="context">The HTTP context representing the request to authenticate.</param>
        /// <returns>The authentication result produced by the HMAC handler.</returns>
        private static async Task<AuthenticateResult> AuthenticateAsync(DefaultHttpContext context)
        {
            // Initialize the handler against the supplied context before invoking the public authentication entry point.
            var handler = CreateHandler();
            await handler.InitializeAsync(CreateScheme(), context);
            return await handler.AuthenticateAsync();
        }

        /// <summary>
        /// Applies the signed request headers required before the authorization header can be calculated.
        /// </summary>
        /// <param name="context">The HTTP context whose request headers should be populated.</param>
        /// <param name="body">The request body used to compute the content hash.</param>
        /// <param name="date">The request timestamp that participates in the HMAC signature.</param>
        private static void SetSignedHeaders(DefaultHttpContext context, string body, DateTimeOffset date)
        {
            // Populate the signed headers first because the authorization signature depends on their final values.
            context.Request.Headers["x-ms-date"] = date.ToString("R");
            context.Request.Headers["x-ms-content-sha256"] = ComputeContentHash(body);
            context.Request.Body.Position = 0;
        }

        /// <summary>
        /// Applies a complete HMAC authorization header and the supporting signed headers.
        /// </summary>
        /// <param name="context">The HTTP context whose request should be signed.</param>
        /// <param name="body">The request body used to compute the body hash.</param>
        /// <param name="date">The timestamp to include in the signed headers.</param>
        /// <param name="credential">The credential value to emit in the authorization header.</param>
        /// <param name="signature">An optional explicit signature override used for tampering scenarios.</param>
        /// <param name="contentHash">An optional explicit content-hash override used for tampering scenarios.</param>
        private static void SetAuthorizationHeader(
            DefaultHttpContext context,
            string body,
            DateTimeOffset date,
            string? credential = null,
            string? signature = null,
            string? contentHash = null)
        {
            // Write the signed headers and then compute the final authorization header from those values.
            SetSignedHeaders(context, body, date);
            context.Request.Headers["x-ms-content-sha256"] = contentHash ?? ComputeContentHash(body);

            const string signedHeaders = "x-ms-date;host;x-ms-content-sha256";
            var computedSignature = signature ?? ComputeSignature(context, signedHeaders);
            context.Request.Headers.Authorization = new AuthenticationHeaderValue(
                "HMAC-SHA256",
                $"Credential={credential ?? Credential}&SignedHeaders={signedHeaders}&Signature={computedSignature}").ToString();
        }

        /// <summary>
        /// Computes the request-body SHA-256 hash used by the HMAC protocol.
        /// </summary>
        /// <param name="body">The request body whose bytes should be hashed.</param>
        /// <returns>The Base64-encoded SHA-256 content hash.</returns>
        private static string ComputeContentHash(string body)
        {
            // Hash the UTF-8 request payload exactly as the runtime handler expects.
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        }

        /// <summary>
        /// Computes the HMAC signature for the supplied request context.
        /// </summary>
        /// <param name="context">The HTTP context containing the request to sign.</param>
        /// <param name="signedHeaders">The semicolon-delimited set of signed header names.</param>
        /// <returns>The Base64-encoded HMAC signature.</returns>
        private static string ComputeSignature(DefaultHttpContext context, string signedHeaders)
        {
            // Recreate the production string-to-sign so tests can generate trusted signatures without live infrastructure.
            var builder = new StringBuilder();
            builder.Append(context.Request.Method.ToUpperInvariant());
            builder.Append('\n');
            builder.Append(context.Features.GetRequiredFeature<IHttpRequestFeature>().RawTarget);
            builder.Append('\n');

            var values = signedHeaders.Split(';')
                .Select(header => context.Request.Headers[header].ToString())
                .ToArray();
            builder.AppendJoin(';', values);

            using var hmac = new HMACSHA256(Convert.FromBase64String(Secret));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(builder.ToString())));
        }
    }
}
