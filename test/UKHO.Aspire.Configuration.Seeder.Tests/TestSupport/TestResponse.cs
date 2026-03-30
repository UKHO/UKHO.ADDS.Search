using Azure;
using Azure.Core;

namespace UKHO.Aspire.Configuration.Seeder.Tests.TestSupport
{
    /// <summary>
    /// Provides a minimal concrete <see cref="Response"/> implementation for Azure SDK test doubles.
    /// </summary>
    internal sealed class TestResponse : Response
    {
        private readonly Dictionary<string, List<string>> _headers = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _reasonPhrase;
        private readonly int _status;

        /// <summary>
        /// Initializes a response with the supplied status code and optional reason phrase.
        /// </summary>
        /// <param name="status">The HTTP-style status code to expose from the fake response.</param>
        /// <param name="reasonPhrase">The optional reason phrase to surface with the fake response.</param>
        public TestResponse(int status, string reasonPhrase = "")
        {
            // Store the minimal response metadata required by Response.FromValue consumers in tests.
            _status = status;
            _reasonPhrase = reasonPhrase;
        }

        /// <summary>
        /// Gets the fake response status code.
        /// </summary>
        public override int Status => _status;

        /// <summary>
        /// Gets the fake response reason phrase.
        /// </summary>
        public override string ReasonPhrase => _reasonPhrase;

        /// <summary>
        /// Gets or sets the optional response stream. Tests in this work item do not rely on the body stream.
        /// </summary>
        public override Stream? ContentStream { get; set; }

        /// <summary>
        /// Gets or sets the client request identifier associated with the fake response.
        /// </summary>
        public override string ClientRequestId { get; set; } = string.Empty;

        /// <summary>
        /// Releases resources associated with the fake response.
        /// </summary>
        public override void Dispose()
        {
            // The fake response does not own unmanaged resources, so disposal is intentionally a no-op.
        }

        /// <summary>
        /// Determines whether the fake response contains a header with the supplied name.
        /// </summary>
        /// <param name="name">The header name to locate.</param>
        /// <returns><see langword="true"/> when the fake response contains the requested header.</returns>
        protected override bool ContainsHeader(string name)
        {
            // Delegate to the in-memory header dictionary used by the fake response.
            return _headers.ContainsKey(name);
        }

        /// <summary>
        /// Enumerates all captured headers.
        /// </summary>
        /// <returns>An enumeration of header name/value pairs.</returns>
        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            // Flatten the stored header values into the shape expected by the Azure response abstraction.
            foreach (var (name, values) in _headers)
            {
                foreach (var value in values)
                {
                    yield return new HttpHeader(name, value);
                }
            }
        }

        /// <summary>
        /// Attempts to fetch the first value for the supplied header name.
        /// </summary>
        /// <param name="name">The header name to look up.</param>
        /// <param name="value">The first matching value when the header exists; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the header exists.</returns>
        protected override bool TryGetHeader(string name, out string value)
        {
            // Return the first stored value because Response expects a single representative value here.
            if (_headers.TryGetValue(name, out var values) && values.Count > 0)
            {
                value = values[0];
                return true;
            }

            value = null!;
            return false;
        }

        /// <summary>
        /// Attempts to fetch all values for the supplied header name.
        /// </summary>
        /// <param name="name">The header name to look up.</param>
        /// <param name="values">The matching values when the header exists; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the header exists.</returns>
        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            // Return the stored list directly because the test double does not mutate header collections after creation.
            if (_headers.TryGetValue(name, out var matchingValues))
            {
                values = matchingValues;
                return true;
            }

            values = null!;
            return false;
        }
    }
}
