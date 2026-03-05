namespace FileShareImageLoader.Infrastructure;

public sealed record LocalMetadataImageInfo(
    string? Version,
    string? Tags,
    string? Digest,
    string? SizeBytes,
    string? CreatedUtc
);
