using TravelScheduleArrange.Api.Models;
using TravelScheduleArrange.Api.Services.Tdx;

namespace TravelScheduleArrange.Api.Services.MultiDay;

/// <inheritdoc cref="IMultiAnchorAttractionService"/>
public class MultiAnchorAttractionService : IMultiAnchorAttractionService
{
    private readonly ITdxTourismService _tourismService;

    public MultiAnchorAttractionService(ITdxTourismService tourismService)
    {
        _tourismService = tourismService;
    }

    public async Task<IReadOnlyList<Attraction>> GetNearbyAttractionsAsync(
        IReadOnlyList<Coordinate> anchors, int radiusMeters, CancellationToken cancellationToken = default)
    {
        var perAnchorResults = await Task.WhenAll(
            anchors.Select(anchor => QueryAnchorAsync(anchor, radiusMeters, cancellationToken)));

        return DeduplicateByKey(perAnchorResults.SelectMany(r => r));
    }

    private async Task<IReadOnlyList<Attraction>> QueryAnchorAsync(
        Coordinate anchor, int radiusMeters, CancellationToken cancellationToken)
    {
        var scenicSpotsTask = _tourismService.GetNearbyScenicSpotsAsync(
            anchor.Latitude, anchor.Longitude, radiusMeters, cancellationToken);
        var restaurantsTask = _tourismService.GetNearbyRestaurantsAsync(
            anchor.Latitude, anchor.Longitude, radiusMeters, cancellationToken);

        await Task.WhenAll(scenicSpotsTask, restaurantsTask);
        return scenicSpotsTask.Result.Concat(restaurantsTask.Result).ToList();
    }

    /// <summary>
    /// 依去重鍵合併多個錨點查詢到的重複景點（例如兩個相鄰錨點的查詢範圍重疊）。
    /// 獨立為 public static 方法方便單元測試直接驗證去重邏輯。
    /// </summary>
    public static IReadOnlyList<Attraction> DeduplicateByKey(IEnumerable<Attraction> attractions) =>
        attractions
            .GroupBy(DedupeKey)
            .Select(g => g.First())
            .ToList();

    /// <summary>
    /// 去重鍵：優先使用來源系統的 SourceId（例如 TDX 原始 ID），缺少時退回 (名稱, 座標) 組合。
    /// </summary>
    private static string DedupeKey(Attraction attraction) =>
        attraction.SourceId is not null
            ? $"source:{attraction.SourceId}"
            : $"coord:{attraction.Name}:{attraction.Latitude}:{attraction.Longitude}";
}
