using Azure.Core;

namespace UKHO.Search.Ingestion.Tests.DeadLetter
{
    public sealed class RecordingPipelineRequest : Request
    {
        private readonly Dictionary<string, List<string>> headers = new(StringComparer.OrdinalIgnoreCase);
        private RequestUriBuilder uri = new();

        public override RequestContent? Content { get; set; }

        public override string ClientRequestId { get; set; } = string.Empty;

        public override RequestMethod Method { get; set; }

        public override RequestUriBuilder Uri
        {
            get => uri;
            set => uri = value;
        }

        public override void Dispose()
        {
        }

        protected override void SetHeader(string name, string value)
        {
            headers[name] = new List<string> { value };
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

        protected override void AddHeader(string name, string value)
        {
            if (!headers.TryGetValue(name, out var values))
            {
                values = new List<string>();
                headers[name] = values;
            }

            values.Add(value);
        }

        protected override bool RemoveHeader(string name)
        {
            return headers.Remove(name);
        }
    }
}