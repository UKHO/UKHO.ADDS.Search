namespace UKHO.Search.Query
{
    public sealed class TokenNormalizer
    {
        public IEnumerable<string> NormalizeToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Array.Empty<string>();
            }

            var normalizedToken = token.Trim().ToLowerInvariant();
            if (!IsHyphenatedNumericSToken(normalizedToken))
            {
                return new[] { normalizedToken };
            }

            return new[]
            {
                normalizedToken,
                string.Concat("s", normalizedToken.AsSpan(2))
            };
        }

        private static bool IsHyphenatedNumericSToken(string token)
        {
            if (token.Length < 3 || token[0] != 's' || token[1] != '-')
            {
                return false;
            }

            for (var index = 2; index < token.Length; index++)
            {
                if (!char.IsDigit(token[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
