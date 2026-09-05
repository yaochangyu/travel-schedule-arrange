using Microsoft.AspNetCore.Mvc;
using TravelScheduleArrange.Api.Services;

namespace TravelScheduleArrange.Api.Controllers;

/// <summary>單一候選景點/美食（行程排序的輸入項目）。</summary>
public record ItineraryPlanRequestItem(
    string? SourceId,
    string Name,
    string? Category,
    string? Address,
    double Latitude,
    double Longitude);

/// <summary>行程排序請求：起點座標 + 使用者勾選的候選景點/美食清單。</summary>
public record ItineraryPlanRequest(
    double StartLatitude,
    double StartLongitude,
    List<ItineraryPlanRequestItem> Attractions);

/// <summary>行程排序結果：單一站點，含拜訪順序與距離前一站的距離。</summary>
public record ItineraryPlanResponseItem(
    string? SourceId,
    string Name,
    string? Category,
    string? Address,
    double Latitude,
    double Longitude,
    int Order,
    double DistanceFromPreviousKm);

[ApiController]
[Route("api/[controller]")]
public class ItineraryController : ControllerBase
{
    private readonly IItineraryPlannerService _plannerService;

    public ItineraryController(IItineraryPlannerService plannerService)
    {
        _plannerService = plannerService;
    }

    /// <summary>
    /// 依起點座標，對使用者勾選的候選景點/美食清單，以貪婪最近鄰演算法排出拜訪順序（含每站與前一站的交通距離）。
    /// </summary>
    [HttpPost("plan")]
    public async Task<IActionResult> Plan([FromBody] ItineraryPlanRequest request)
    {
        if (request.Attractions is null || request.Attractions.Count == 0)
        {
            return BadRequest("attractions 不可為空清單。");
        }

        var start = new Coordinate(request.StartLatitude, request.StartLongitude);
        var candidates = request.Attractions
            .Select(a => new ItineraryPlanItem(a.Name, new Coordinate(a.Latitude, a.Longitude)))
            .ToList();

        var planned = await _plannerService.PlanAsync(start, candidates);

        // ItineraryPlanItem 只保留 Name/Location，排序後用 (Name, 座標) 對回原始請求項目，
        // 補回 SourceId/Category/Address 供前端顯示（同一批候選點理論上不會有名稱+座標完全相同的重複資料）。
        var lookup = request.Attractions
            .GroupBy(a => (a.Name, a.Latitude, a.Longitude))
            .ToDictionary(g => g.Key, g => g.First());

        var response = planned.Select(p =>
        {
            lookup.TryGetValue((p.Name, p.Location.Latitude, p.Location.Longitude), out var original);
            return new ItineraryPlanResponseItem(
                original?.SourceId,
                p.Name,
                original?.Category,
                original?.Address,
                p.Location.Latitude,
                p.Location.Longitude,
                p.Order,
                Math.Round(p.DistanceFromPreviousKm, 3));
        }).ToList();

        return Ok(response);
    }
}
