using Microsoft.AspNetCore.Mvc;
using TravelScheduleArrange.Api.Services.Tdx;

namespace TravelScheduleArrange.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttractionsController : ControllerBase
{
    private readonly ITdxTourismService _tourismService;

    public AttractionsController(ITdxTourismService tourismService)
    {
        _tourismService = tourismService;
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
}
