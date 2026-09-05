using TravelScheduleArrange.Api.Models;

namespace TravelScheduleArrange.Api.Services.MultiDay;

/// <summary>
/// 多日行程的候選景點蒐集服務：多日行程有多個錨點（起點、每晚住宿、訖點），可能分散在不同區域，
/// 需對每個錨點各自查詢附近景點/美食後合併去重，才能組成完整候選池。
/// </summary>
public interface IMultiAnchorAttractionService
{
    /// <summary>
    /// 對每個錨點座標查詢附近景點/美食，合併去重後回傳單一候選池。
    /// </summary>
    /// <param name="anchors">錨點座標清單（起點、每晚住宿、訖點）</param>
    /// <param name="radiusMeters">每個錨點的查詢半徑（公尺）</param>
    Task<IReadOnlyList<Attraction>> GetNearbyAttractionsAsync(
        IReadOnlyList<Coordinate> anchors, int radiusMeters, CancellationToken cancellationToken = default);
}
