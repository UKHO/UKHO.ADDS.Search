namespace UKHO.Search.Studio.Providers.FileShare
{
    internal static class FileShareStudioSecurityTokenPolicy
    {
        private const string BatchCreateToken = "batchcreate";
        private const string PublicToken = "public";

        public static string[] CreateTokens(string? activeBusinessUnitName)
        {
            var tokens = new List<string>(3)
            {
                BatchCreateToken,
                PublicToken
            };

            var normalizedBusinessUnitName = Normalize(activeBusinessUnitName);
            if (normalizedBusinessUnitName is not null)
            {
                tokens.Insert(1, $"batchcreate_{normalizedBusinessUnitName}");
            }

            return tokens.ToArray();
        }

        private static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalizedValue = value.Trim().ToLowerInvariant();
            return normalizedValue.Length == 0 ? null : normalizedValue;
        }
    }
}
