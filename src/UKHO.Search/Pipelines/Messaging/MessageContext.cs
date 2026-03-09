namespace UKHO.Search.Pipelines.Messaging
{
    public sealed class MessageContext
    {
        private readonly List<string> breadcrumbs = new();
        private readonly Dictionary<string, object?> items = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTimeOffset> timingsUtc = new();

        public IReadOnlyList<string> Breadcrumbs => breadcrumbs;

        public IReadOnlyDictionary<string, object?> Items => items;

        public IReadOnlyDictionary<string, DateTimeOffset> TimingsUtc => timingsUtc;

        public void SetItem(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            items[key] = value;
        }

        public bool TryGetItem<T>(string key, out T? value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = default;
                return false;
            }

            if (!items.TryGetValue(key, out var raw) || raw is not T typed)
            {
                value = default;
                return false;
            }

            value = typed;
            return true;
        }

        public void AddBreadcrumb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            breadcrumbs.Add(value);
        }

        public void MarkTimeUtc(string name, DateTimeOffset timeUtc)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            timingsUtc[name] = timeUtc;
        }

        public MessageContext Clone()
        {
            var clone = new MessageContext();

            clone.breadcrumbs.AddRange(breadcrumbs);

            foreach (var kvp in items)
            {
                clone.items.Add(kvp.Key, kvp.Value);
            }

            foreach (var kvp in timingsUtc)
            {
                clone.timingsUtc.Add(kvp.Key, kvp.Value);
            }

            return clone;
        }
    }
}