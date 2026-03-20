namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal static class RuleContextMigrationHelper
    {
        public static bool TryDeriveContextFromLegacyFileName(string? fileName, out string? context)
        {
            context = null;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            var stem = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(stem) || !stem.StartsWith("bu-", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var remaining = stem[3..];
            if (remaining.Length == 0)
            {
                return false;
            }

            var segments = remaining.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                return false;
            }

            var integerSegmentIndex = -1;
            for (var i = 0; i < segments.Length; i++)
            {
                if (int.TryParse(segments[i], out _))
                {
                    integerSegmentIndex = i;
                    break;
                }
            }

            if (integerSegmentIndex <= 0)
            {
                return false;
            }

            var derived = string.Join('-', segments.Take(integerSegmentIndex));
            if (string.IsNullOrWhiteSpace(derived))
            {
                return false;
            }

            context = derived.ToLowerInvariant();
            return true;
        }
    }
}
