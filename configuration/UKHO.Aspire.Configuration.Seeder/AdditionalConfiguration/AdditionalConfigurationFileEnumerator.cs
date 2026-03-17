namespace UKHO.Aspire.Configuration.Seeder.AdditionalConfiguration
{
    internal static class AdditionalConfigurationFileEnumerator
    {
        public static IEnumerable<string> EnumerateFiles(string rootPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

            return Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories);
        }

        public static IReadOnlyList<string> GetRelativePathSegments(string rootPath, string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            var relativePath = Path.GetRelativePath(rootPath, filePath);
            var directory = Path.GetDirectoryName(relativePath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                return Array.Empty<string>();
            }

            // Split the relative directory path into segments to form key parts using the configured ':' delimiter.
            return directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
        }
    }
}
