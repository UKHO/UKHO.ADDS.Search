using System.Text;

namespace UKHO.Aspire.Configuration.Seeder.AdditionalConfiguration
{
    internal static class AdditionalConfigurationKeyBuilder
    {
        public static string Build(string prefix, IEnumerable<string> relativePathSegments, string fileNameWithoutExtension)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileNameWithoutExtension);

            var builder = new StringBuilder(prefix.Length + fileNameWithoutExtension.Length + 8);
            builder.Append(prefix);

            foreach (var segment in relativePathSegments)
            {
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                builder.Append(':');
                builder.Append(segment);
            }

            builder.Append(':');
            builder.Append(fileNameWithoutExtension);

            return builder.ToString();
        }
    }
}
