using System.Globalization;
using UKHO.Search.Geo;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers.Enrichers
{
    internal static class GeoPolygonWktReader
    {
        public static GeoPolygon ReadPolygon(string wkt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(wkt);

            // Minimal WKT reader for the envelope polygon we output:
            // POLYGON((x y, x y, ...))
            var trimmed = wkt.Trim();
            if (!trimmed.StartsWith("POLYGON", StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException("Expected WKT starting with 'POLYGON'.");
            }

            var i0 = trimmed.IndexOf("((", StringComparison.Ordinal);
            var i1 = trimmed.LastIndexOf("))", StringComparison.Ordinal);
            if (i0 < 0 || i1 < 0 || i1 <= i0 + 1)
            {
                throw new FormatException("Invalid POLYGON WKT format.");
            }

            var inner = trimmed.Substring(i0 + 2, i1 - (i0 + 2));

            // Only support a single ring for now.
            if (inner.Contains("(", StringComparison.Ordinal) || inner.Contains(")", StringComparison.Ordinal))
            {
                throw new FormatException("Only single-ring POLYGON WKT is supported.");
            }

            var points = inner.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var coords = new List<GeoCoordinate>(points.Length);

            foreach (var p in points)
            {
                var parts = p.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    throw new FormatException("Invalid coordinate pair in POLYGON WKT.");
                }

                // WKT order is X Y => lon lat
                var lon = double.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                var lat = double.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture);

                coords.Add(GeoCoordinate.Create(lon, lat));
            }

            return GeoPolygon.Create(coords);
        }
    }
}
