using Microsoft.Extensions.Logging;
using TravelScheduleArrange.Api.Models;
using TravelScheduleArrange.Api.Services.Tdx;

namespace TravelScheduleArrange.Api.Services.MultiDay;

/// <inheritdoc cref="IMultiAnchorAttractionService"/>
public class MultiAnchorAttractionService : IMultiAnchorAttractionService
{
    private readonly ITdxTourismService _tourismService;
    private readonly ILogger<MultiAnchorAttractionService> _logger;

    public MultiAnchorAttractionService(
        ITdxTourismService tourismService, ILogger<MultiAnchorAttractionService> logger)
    {
        _tourismService = tourismService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Attraction>> GetNearbyAttractionsAsync(
        IReadOnlyList<Coordinate> anchors, int radiusMeters, CancellationToken cancellationToken = default)
    {
        var perAnchorResults = await Task.WhenAll(
            anchors.Select(anchor => QueryAnchorAsync(anchor, radiusMeters, cancellationToken)));

        return DeduplicateByKey(perAnchorResults.SelectMany(r => r));
    }

    /// <summary>
    /// 查詢單一錨點附近的景點/美食。個別錨點查詢失敗時（例如 TDX 速率限制重試後仍失敗）僅記錄警告並回傳
    /// 空清單，不讓單一錨點的問題導致整個多錨點候選池查詢失敗——其餘錨點的結果仍應正常回傳給使用者。
    /// </summary>
    private async Task<IReadOnlyList<Attraction>> QueryAnchorAsync(
        Coordinate anchor, int radiusMeters, CancellationToken cancellationToken)
    {
        try
        {
            var scenicSpotsTask = _tourismService.GetNearbyScenicSpotsAsync(
                anchor.Latitude, anchor.Longitude, radiusMeters, cancellationToken);
            var restaurantsTask = _tourismService.GetNearbyRestaurantsAsync(
                anchor.Latitude, anchor.Longitude, radiusMeters, cancellationToken);

            await Task.WhenAll(scenicSpotsTask, restaurantsTask);
            return scenicSpotsTask.Result.Concat(restaurantsTask.Result).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "查詢錨點 ({Latitude}, {Longitude}) 附近景點/美食失敗，該錨點暫不提供候選景點，其餘錨點結果不受影響。",
                anchor.Latitude, anchor.Longitude);
            return Array.Empty<Attraction>();
        }
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
