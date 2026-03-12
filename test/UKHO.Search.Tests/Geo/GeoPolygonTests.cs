using Shouldly;
using UKHO.Search.Geo;
using Xunit;

namespace UKHO.Search.Tests.Geo
{
    public sealed class GeoPolygonTests
    {
        [Fact]
        public void Create_with_valid_single_ring_succeeds()
        {
            var ring = new[]
            {
                GeoCoordinate.Create(0d, 0d),
                GeoCoordinate.Create(1d, 0d),
                GeoCoordinate.Create(1d, 1d),
                GeoCoordinate.Create(0d, 0d)
            };

            var polygon = GeoPolygon.Create(ring);

            polygon.Rings.Count.ShouldBe(1);
            polygon.Rings[0].Count.ShouldBe(4);
        }

        [Fact]
        public void Create_with_ring_less_than_4_points_throws()
        {
            var ring = new[]
            {
                GeoCoordinate.Create(0d, 0d),
                GeoCoordinate.Create(1d, 0d),
                GeoCoordinate.Create(0d, 0d)
            };

            Should.Throw<ArgumentException>(() => GeoPolygon.Create(ring));
        }

        [Fact]
        public void Create_with_unclosed_ring_throws()
        {
            var ring = new[]
            {
                GeoCoordinate.Create(0d, 0d),
                GeoCoordinate.Create(1d, 0d),
                GeoCoordinate.Create(1d, 1d),
                GeoCoordinate.Create(0d, 1d)
            };

            Should.Throw<ArgumentException>(() => GeoPolygon.Create(ring));
        }
    }
}
