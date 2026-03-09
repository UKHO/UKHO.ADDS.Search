using Azure;
using Azure.Core;

namespace UKHO.Search.Ingestion.Tests.DeadLetter
{
    public sealed class SimpleResponse : Response
    {
        private readonly Dictionary<string, List<string>> headers = new(StringComparer.OrdinalIgnoreCase);
        private readonly string reasonPhrase;
        private readonly int status;

        public SimpleResponse(int status, string reasonPhrase = "")
        {
            this.status = status;
            this.reasonPhrase = reasonPhrase;
        }

        public override int Status => status;

        public override string ReasonPhrase => reasonPhrase;

        public override Stream? ContentStream { get; set; }

        public override string ClientRequestId { get; set; } = string.Empty;

        public override void Dispose()
        {
        }

        protected override bool ContainsHeader(string name)
        {
            return headers.ContainsKey(name);
        }

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            foreach (var (name, values) in headers)
            {
                foreach (var value in values)
                {
                    yield return new HttpHeader(name, value);
                }
            }
        }

        protected override bool TryGetHeader(string name, out string? value)
        {
            if (headers.TryGetValue(name, out var values) && values.Count > 0)
            {
                value = values[0];
                return true;
            }

            value = null;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string>? values)
        {
            if (headers.TryGetValue(name, out var list))
            {
                values = list;
                return true;
            }

            values = null;
            return false;
        }
    }
}