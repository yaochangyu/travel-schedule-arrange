using Microsoft.AspNetCore.Mvc;
using TravelScheduleArrange.Api.Services.GoogleMaps;

namespace TravelScheduleArrange.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeocodingController : ControllerBase
{
    private readonly IGoogleGeocodingService _geocodingService;

    public GeocodingController(IGoogleGeocodingService geocodingService)
    {
        _geocodingService = geocodingService;
    }

    /// <summary>
    /// 將使用者輸入的地點名稱/地址（例如「台北101」）轉換為座標，供後續查詢附近景點/美食使用。
    /// </summary>
    /// <param name="address">地點名稱或地址</param>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return BadRequest("address 不可為空白。");
        }

        var result = await _geocodingService.GeocodeAsync(address, cancellationToken);
        if (result is null)
        {
            return NotFound($"找不到「{address}」對應的座標，請確認地點名稱是否正確，或改用經緯度輸入。");
        }

        return Ok(result);
    }
}
