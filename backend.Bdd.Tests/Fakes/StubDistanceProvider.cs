using TravelScheduleArrange.Api.Services;

namespace TravelScheduleArrange.Api.Bdd.Tests.Fakes;

/// <summary>
/// <see cref="IDistanceProvider"/> 的測試替身：座標緯度差直接視為公里數，交通時間固定以
/// 「2 分鐘/公里」換算（對應 30km/h 平均車速），與既有 <c>MultiDayItineraryPlannerServiceTests</c>
/// 的 StubDistanceProvider 採用相同公式，方便在 Gherkin 情境中手算預期結果。
/// </summary>
public class StubDistanceProvider : IDistanceProvider
{
    public Task<double> GetDistanceAsync(Coordinate a, Coordinate b) => Task.FromResult(Distance(a, b));

    public Task<TravelInfo> GetTravelInfoAsync(Coordinate a, Coordinate b)
    {
        var distance = Distance(a, b);
        return Task.FromResult(new TravelInfo(distance, distance * 2));
    }

    private static double Distance(Coordinate a, Coordinate b) => Math.Abs(a.Latitude - b.Latitude);
}
