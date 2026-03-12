namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers.Enrichers
{
    public static class S57DatasetGrouper
    {
        public static IReadOnlyList<S57DatasetGroup> GroupDatasets(IEnumerable<string> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);

            var candidates = paths.Select(p => new
            {
                Path = p,
                BaseName = System.IO.Path.GetFileNameWithoutExtension(p),
                Extension = System.IO.Path.GetExtension(p)
            })
            .Where(x => IsNumericExtension(x.Extension))
            .ToList();

            var groups = candidates
                .GroupBy(x => x.BaseName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var members = g
                        .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
                        .Select(x => x.Path)
                        .ToList();

                    var entryCandidates = members
                        .Where(p => string.Equals(System.IO.Path.GetExtension(p), ".000", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(p => p, StringComparer.Ordinal)
                        .ToList();

                    if (entryCandidates.Count == 0)
                    {
                        return null;
                    }

                    var entryPoint = entryCandidates[0];
                    var baseName = System.IO.Path.GetFileNameWithoutExtension(entryPoint);
                    return new S57DatasetGroup(baseName, entryPoint, members);
                })
                .Where(x => x is not null)
                .OrderBy(x => x!.BaseName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return groups!;
        }

        private static bool IsNumericExtension(string? ext)
        {
            if (string.IsNullOrWhiteSpace(ext) || ext.Length != 4 || ext[0] != '.')
            {
                return false;
            }

            return char.IsDigit(ext[1]) && char.IsDigit(ext[2]) && char.IsDigit(ext[3]);
        }
    }
}
