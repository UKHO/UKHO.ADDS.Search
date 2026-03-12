namespace UKHO.Search.Geo
{
    public readonly record struct GeoCoordinate
    {
        public double Longitude { get; }

        public double Latitude { get; }

        private GeoCoordinate(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }

        public static GeoCoordinate Create(double longitude, double latitude)
        {
            if (latitude < -90d || latitude > 90d)
            {
                throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be within [-90, 90].");
            }

            if (longitude < -180d || longitude > 180d)
            {
                throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be within [-180, 180].");
            }

            return new GeoCoordinate(longitude, latitude);
        }
    }
}
