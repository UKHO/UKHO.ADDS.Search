namespace UKHO.Search.ProviderModel
{
    public sealed class ProviderDescriptor
    {
        public ProviderDescriptor(string name, string displayName, string? description = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            ValidateName(name);

            Name = name;
            DisplayName = displayName;
            Description = string.IsNullOrWhiteSpace(description) ? null : description;
        }

        public string Name { get; }

        public string DisplayName { get; }

        public string? Description { get; }

        private static void ValidateName(string name)
        {
            if (name.StartsWith("-", StringComparison.Ordinal) || name.EndsWith("-", StringComparison.Ordinal))
            {
                throw new ArgumentException("Provider names must be lowercase invariant slugs.", nameof(name));
            }

            var previousWasHyphen = false;

            foreach (var character in name)
            {
                var isLowercaseLetter = character >= 'a' && character <= 'z';
                var isDigit = character >= '0' && character <= '9';
                var isHyphen = character == '-';

                if (!isLowercaseLetter && !isDigit && !isHyphen)
                {
                    throw new ArgumentException("Provider names must be lowercase invariant slugs.", nameof(name));
                }

                if (isHyphen)
                {
                    if (previousWasHyphen)
                    {
                        throw new ArgumentException("Provider names must be lowercase invariant slugs.", nameof(name));
                    }

                    previousWasHyphen = true;
                    continue;
                }

                previousWasHyphen = false;
            }
        }
    }
}
