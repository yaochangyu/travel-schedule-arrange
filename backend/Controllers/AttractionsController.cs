using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using TravelScheduleArrange.Api.Services;
using TravelScheduleArrange.Api.Services.MultiDay;
using TravelScheduleArrange.Api.Services.Tdx;

namespace TravelScheduleArrange.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttractionsController : ControllerBase
{
    private readonly ITdxTourismService _tourismService;
    private readonly IMultiAnchorAttractionService _multiAnchorAttractionService;

    public AttractionsController(
        ITdxTourismService tourismService, IMultiAnchorAttractionService multiAnchorAttractionService)
    {
        _tourismService = tourismService;
        _multiAnchorAttractionService = multiAnchorAttractionService;
    }

    /// <summary>
    /// 查詢指定座標附近的景點與美食（資料來源：TDX 觀光資訊資料庫）。
    /// </summary>
    /// <param name="lat">中心點緯度</param>
    /// <param name="lng">中心點經度</param>
    /// <param name="radius">查詢半徑（公尺），預設 3000</param>
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby(
        [FromQuery] double lat, [FromQuery] double lng, [FromQuery] int radius = 3000,
        CancellationToken cancellationToken = default)
    {
        if (radius <= 0)
        {
            return BadRequest("radius 必須大於 0。");
        }

        var scenicSpotsTask = _tourismService.GetNearbyScenicSpotsAsync(lat, lng, radius, cancellationToken);
        var restaurantsTask = _tourismService.GetNearbyRestaurantsAsync(lat, lng, radius, cancellationToken);

        await Task.WhenAll(scenicSpotsTask, restaurantsTask);

        var result = scenicSpotsTask.Result.Concat(restaurantsTask.Result).ToList();
        return Ok(result);
    }

    /// <summary>
    /// 查詢多個錨點（多日行程的起點/每晚住宿/訖點）附近的景點與美食，合併去重後回傳單一候選池。
    /// </summary>
    /// <param name="anchors">錨點座標清單，格式為 "lat1,lng1;lat2,lng2;..."（以分號分隔多組座標）</param>
    /// <param name="radius">每個錨點的查詢半徑（公尺），預設 3000</param>
    [HttpGet("nearby-multianchor")]
    public async Task<IActionResult> GetNearbyMultiAnchor(
        [FromQuery] string anchors, [FromQuery] int radius = 3000, CancellationToken cancellationToken = default)
    {
        if (radius <= 0)
        {
            return BadRequest("radius 必須大於 0。");
        }

        if (string.IsNullOrWhiteSpace(anchors))
        {
            return BadRequest("anchors 不可為空，格式為 \"lat1,lng1;lat2,lng2;...\"。");
        }

        List<Coordinate> parsedAnchors;
        try
        {
            parsedAnchors = ParseAnchors(anchors);
        }
        catch (FormatException ex)
        {
            return BadRequest($"anchors 格式錯誤：{ex.Message}");
        }

        var result = await _multiAnchorAttractionService.GetNearbyAttractionsAsync(
            parsedAnchors, radius, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 解析 "lat1,lng1;lat2,lng2;..." 格式的錨點座標字串。獨立為 private static 方法方便未來單元測試。
    /// </summary>
    private static List<Coordinate> ParseAnchors(string anchors)
    {
        return anchors
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pair =>
            {
                var parts = pair.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length != 2 ||
                    !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                    !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                {
                    throw new FormatException($"無法解析座標 \"{pair}\"，需為 \"緯度,經度\" 格式。");
                }

                return new Coordinate(lat, lng);
            })
            .ToList();
    }
}
