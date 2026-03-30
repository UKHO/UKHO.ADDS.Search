using System.Net.Http;

namespace UKHO.Aspire.Configuration.Emulator.Tests.TestSupport
{
    /// <summary>
    /// Captures outbound HTTP requests and returns caller-defined responses for deterministic client-side unit tests.
    /// </summary>
    internal sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
        private readonly List<RecordedHttpRequest> _requests = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHttpMessageHandler"/> class.
        /// </summary>
        /// <param name="handler">The delegate that produces the HTTP response for each captured request.</param>
        public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            // Store the response delegate so each test can tailor the fake transport behaviour to its scenario.
            _handler = handler;
        }

        /// <summary>
        /// Gets the requests captured by the handler in the order they were sent.
        /// </summary>
        public IReadOnlyList<RecordedHttpRequest> Requests => _requests;

        /// <summary>
        /// Captures the outbound request and delegates response generation to the configured test callback.
        /// </summary>
        /// <param name="request">The outbound HTTP request issued by the client under test.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the caller.</param>
        /// <returns>The response produced by the configured delegate.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Snapshot the request before delegating so assertions remain valid even after the request is disposed.
            var content = request.Content is not null
                ? await request.Content.ReadAsStringAsync(cancellationToken)
                : null;

            var headers = request.Headers.ToDictionary(
                header => header.Key,
                header => header.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);

            var contentHeaders = request.Content?.Headers.ToDictionary(
                header => header.Key,
                header => header.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            _requests.Add(new RecordedHttpRequest(
                request.Method,
                request.RequestUri?.ToString(),
                headers,
                contentHeaders,
                content));

            // Delegate the response so each test can model pagination, failures, or success flows as needed.
            return await _handler(request, cancellationToken);
        }
    }
}
