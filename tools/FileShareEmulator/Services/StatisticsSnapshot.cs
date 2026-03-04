namespace FileShareEmulator.Services;

public sealed record StatisticsSnapshot(
    int BatchCount,
    int FileCount,
    int BatchAttributeCount,
    int FileAttributeCount,
    int BatchReadUserCount,
    int BatchReadGroupCount
);
