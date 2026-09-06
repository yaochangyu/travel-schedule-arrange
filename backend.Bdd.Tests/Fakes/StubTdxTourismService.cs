using TravelScheduleArrange.Api.Models;
using TravelScheduleArrange.Api.Services.Tdx;

namespace TravelScheduleArrange.Api.Bdd.Tests.Fakes;

/// <summary>
/// <see cref="ITdxTourismService"/> 的測試替身：以緯度作為錨點識別鍵，讓 BDD 情境可以在呼叫 API 前
/// 先設定「這個錨點回傳什麼景點/美食」或「這個錨點的查詢會失敗」，藉此在不呼叫真實 TDX API 的情況下，
/// 驗證 `nearby-multianchor` API（含 Controller 的參數解析/驗證、`MultiAnchorAttractionService`
/// 的多錨點合併/去重/失敗隔離邏輯）的真實行為。
/// </summary>
public class StubTdxTourismService : ITdxTourismService
{
    private readonly Dictionary<double, IReadOnlyList<Attraction>> _scenicSpotsByLatitude = new();
    private readonly Dictionary<double, IReadOnlyList<Attraction>> _restaurantsByLatitude = new();
    private readonly HashSet<double> _failingLatitudes = new();

    public void SetScenicSpots(double latitude, IReadOnlyList<Attraction> attractions) =>
        _scenicSpotsByLatitude[latitude] = attractions;

    public void SetFailing(double latitude) => _failingLatitudes.Add(latitude);

    public Task<IReadOnlyList<Attraction>> GetNearbyScenicSpotsAsync(
        double latitude, double longitude, int radiusMeters, CancellationToken cancellationToken = default)
    {
        if (_failingLatitudes.Contains(latitude))
        {
            throw new HttpRequestException("模擬 TDX API 呼叫失敗（測試用）");
        }

        return Task.FromResult(
            _scenicSpotsByLatitude.TryGetValue(latitude, out var result) ? result : Array.Empty<Attraction>());
    }

    public Task<IReadOnlyList<Attraction>> GetNearbyRestaurantsAsync(
        double latitude, double longitude, int radiusMeters, CancellationToken cancellationToken = default)
    {
        if (_failingLatitudes.Contains(latitude))
        {
            throw new HttpRequestException("模擬 TDX API 呼叫失敗（測試用）");
        }

        return Task.FromResult(
            _restaurantsByLatitude.TryGetValue(latitude, out var result) ? result : Array.Empty<Attraction>());
    }
}
