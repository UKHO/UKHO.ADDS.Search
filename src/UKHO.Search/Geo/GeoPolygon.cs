using System.Text.Json.Serialization;

namespace UKHO.Search.Geo
{
    public sealed class GeoPolygon
    {
        public IReadOnlyList<IReadOnlyList<GeoCoordinate>> Rings { get; }

        [JsonConstructor]
        public GeoPolygon(IReadOnlyList<IReadOnlyList<GeoCoordinate>> rings)
        {
            ArgumentNullException.ThrowIfNull(rings);

            if (rings.Count == 0)
            {
                throw new ArgumentException("At least one ring must be provided.", nameof(rings));
            }

            Rings = rings;
        }

        public static GeoPolygon Create(IEnumerable<GeoCoordinate> exteriorRing)
        {
            ArgumentNullException.ThrowIfNull(exteriorRing);

            var ringList = exteriorRing.ToArray();
            ValidateRing(ringList);

            return new GeoPolygon(new[] { ringList });
        }

        public static GeoPolygon Create(IEnumerable<IEnumerable<GeoCoordinate>> rings)
        {
            ArgumentNullException.ThrowIfNull(rings);

            var ringLists = rings.Select(r => r.ToArray()).ToArray();
            if (ringLists.Length == 0)
            {
                throw new ArgumentException("At least one ring must be provided.", nameof(rings));
            }

            foreach (var ring in ringLists)
            {
                ValidateRing(ring);
            }

            return new GeoPolygon(ringLists);
        }

        private static void ValidateRing(IReadOnlyList<GeoCoordinate> ring)
        {
            if (ring.Count < 4)
            {
                throw new ArgumentException("A polygon ring must contain at least 4 coordinates.");
            }

            if (!ring[0].Equals(ring[^1]))
            {
                throw new ArgumentException("A polygon ring must be closed (first coordinate must equal last coordinate).");
            }
        }
    }
}
