using System.Collections.Generic;

namespace UKHO.Search.Pipelines.Messaging
{
	public sealed class MessageContext
	{
		private readonly List<string> breadcrumbs = new();
		private readonly Dictionary<string, DateTimeOffset> timingsUtc = new();

		public IReadOnlyList<string> Breadcrumbs => breadcrumbs;

		public IReadOnlyDictionary<string, DateTimeOffset> TimingsUtc => timingsUtc;

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
	}
}
