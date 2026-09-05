namespace TravelScheduleArrange.Api.Services;

/// <summary>
/// <see cref="IDistanceProvider"/> 的模擬實作，在尚未取得 Google Maps API Key（步驟 3）前，
/// 以 Haversine 公式計算兩點間的球面距離（公里）作為近似值，讓排序邏輯可以先行開發與測試。
/// </summary>
public class MockDistanceProvider : IDistanceProvider
{
    private const double EarthRadiusKm = 6371.0;

    public Task<double> GetDistanceAsync(Coordinate a, Coordinate b)
    {
        var distance = CalculateHaversineDistance(a, b);
        return Task.FromResult(distance);
    }

    private static double CalculateHaversineDistance(Coordinate a, Coordinate b)
    {
        var lat1 = DegreesToRadians(a.Latitude);
        var lat2 = DegreesToRadians(b.Latitude);
        var deltaLat = DegreesToRadians(b.Latitude - a.Latitude);
        var deltaLon = DegreesToRadians(b.Longitude - a.Longitude);

        var h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));

        return EarthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
