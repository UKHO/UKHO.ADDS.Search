namespace FileShareEmulator.Services
{
    public sealed class BatchDownloadResult
    {
        public required bool Success { get; init; }

        public string? ErrorMessage { get; init; }

        public string? SavedFilePath { get; init; }

        public static BatchDownloadResult Ok(string savedFilePath)
        {
            return new BatchDownloadResult
            {
                Success = true,
                SavedFilePath = savedFilePath,
            };
        }

        public static BatchDownloadResult Fail(string errorMessage)
        {
            return new BatchDownloadResult
            {
                Success = false,
                ErrorMessage = errorMessage,
            };
        }
    }
}
