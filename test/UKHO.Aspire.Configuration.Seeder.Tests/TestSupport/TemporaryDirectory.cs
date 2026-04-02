namespace UKHO.Aspire.Configuration.Seeder.Tests.TestSupport
{
    /// <summary>
    /// Creates an isolated temporary directory for file-system-backed tests and removes it when the test completes.
    /// </summary>
    internal sealed class TemporaryDirectory : IDisposable
    {
        /// <summary>
        /// Initializes a new temporary directory under the system temp path so each test can work with deterministic, isolated files.
        /// </summary>
        public TemporaryDirectory()
        {
            // Create a unique root per test so parallel or repeated executions cannot interfere with one another.
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                nameof(UKHO),
                nameof(Aspire),
                nameof(Configuration),
                nameof(Seeder),
                nameof(Tests),
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(Path);
        }

        /// <summary>
        /// Gets the absolute path to the temporary directory created for the current test.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Creates a text file relative to the temporary directory and returns its absolute path.
        /// </summary>
        /// <param name="relativePath">The file path relative to <see cref="Path"/>.</param>
        /// <param name="contents">The file contents to persist for the test scenario.</param>
        /// <returns>The absolute path to the created file.</returns>
        public string CreateFile(string relativePath, string contents)
        {
            // Resolve the requested relative path beneath the isolated temp root.
            var fullPath = System.IO.Path.Combine(Path, relativePath);
            var directoryPath = System.IO.Path.GetDirectoryName(fullPath);

            // Ensure nested directories exist before writing the test data file.
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(fullPath, contents);
            return fullPath;
        }

        /// <summary>
        /// Deletes the temporary directory tree created for the test if it still exists.
        /// </summary>
        public void Dispose()
        {
            // Remove the entire directory tree so file-backed tests do not leak state between runs.
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
