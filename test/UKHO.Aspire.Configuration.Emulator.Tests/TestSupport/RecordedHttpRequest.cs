using System.Net.Http;

namespace UKHO.Aspire.Configuration.Emulator.Tests.TestSupport
{
    /// <summary>
    /// Captures the shape of an outbound HTTP request so assertions can verify request construction without relying on live services.
    /// </summary>
    internal sealed record RecordedHttpRequest(
        HttpMethod Method,
        string? RequestUri,
        IReadOnlyDictionary<string, string[]> Headers,
        IReadOnlyDictionary<string, string[]> ContentHeaders,
        string? Content);
}
