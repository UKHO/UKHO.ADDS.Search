namespace FileShareEmulator.Services
{
    public sealed record BusinessUnitStatistics(string BusinessUnitName, IReadOnlyList<NamedCount> BatchAttributeNames, IReadOnlyList<NamedCount> MimeTypes);
}
