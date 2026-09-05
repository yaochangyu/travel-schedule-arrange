using TravelScheduleArrange.Api.Models;

namespace TravelScheduleArrange.Api.Services.Tdx;

/// <summary>
/// 查詢 TDX 觀光資訊資料庫（景點、美食）的服務。
/// </summary>
public interface ITdxTourismService
{
    /// <summary>
    /// 查詢指定座標附近的景點（TDX 資源名稱為 Attraction）。
    /// </summary>
    /// <param name="latitude">中心點緯度</param>
    /// <param name="longitude">中心點經度</param>
    /// <param name="radiusMeters">查詢半徑（公尺）</param>
    Task<IReadOnlyList<Attraction>> GetNearbyScenicSpotsAsync(
        double latitude, double longitude, int radiusMeters, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢指定座標附近的美食（Restaurant）。
    /// </summary>
    /// <param name="latitude">中心點緯度</param>
    /// <param name="longitude">中心點經度</param>
    /// <param name="radiusMeters">查詢半徑（公尺）</param>
    Task<IReadOnlyList<Attraction>> GetNearbyRestaurantsAsync(
        double latitude, double longitude, int radiusMeters, CancellationToken cancellationToken = default);
}
