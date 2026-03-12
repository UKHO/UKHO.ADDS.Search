namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers.Enrichers
{
    public sealed class S57DatasetGroup
    {
        public S57DatasetGroup(string baseName, string entryPointPath, IReadOnlyList<string> memberPaths)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
            ArgumentException.ThrowIfNullOrWhiteSpace(entryPointPath);
            ArgumentNullException.ThrowIfNull(memberPaths);

            BaseName = baseName;
            EntryPointPath = entryPointPath;
            MemberPaths = memberPaths;
        }

        public string BaseName { get; }

        public string EntryPointPath { get; }

        public IReadOnlyList<string> MemberPaths { get; }
    }
}
