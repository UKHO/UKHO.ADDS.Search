using Shouldly;
using UKHO.Search.Geo;
using Xunit;

namespace UKHO.Search.Tests.Geo
{
    public sealed class GeoCoordinateTests
    {
        [Theory]
        [InlineData(-180d, -90d)]
        [InlineData(0d, 0d)]
        [InlineData(180d, 90d)]
        public void Create_valid_coordinate_succeeds(double longitude, double latitude)
        {
            var coordinate = GeoCoordinate.Create(longitude, latitude);

            coordinate.Longitude.ShouldBe(longitude);
            coordinate.Latitude.ShouldBe(latitude);
        }

        [Theory]
        [InlineData(-180.0001d, 0d)]
        [InlineData(180.0001d, 0d)]
        [InlineData(0d, -90.0001d)]
        [InlineData(0d, 90.0001d)]
        public void Create_out_of_range_throws(double longitude, double latitude)
        {
            Should.Throw<ArgumentOutOfRangeException>(() => GeoCoordinate.Create(longitude, latitude));
        }
    }
}
