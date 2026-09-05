using Microsoft.AspNetCore.Mvc;
using TravelScheduleArrange.Api.Services;
using TravelScheduleArrange.Api.Services.MultiDay;

namespace TravelScheduleArrange.Api.Controllers;

/// <summary>單一座標點（供多日行程請求中的每晚住宿地點使用）。</summary>
public record CoordinateDto(double Latitude, double Longitude);

/// <summary>多日行程的單一候選景點/美食，含使用者自行設定的停留時間。</summary>
public record MultiDayCandidateItem(
    string? SourceId,
    string Name,
    string? Category,
    string? Address,
    double Latitude,
    double Longitude,
    int StayDurationMinutes);

/// <summary>多日行程排程請求。</summary>
public record MultiDayItineraryPlanRequest(
    double StartLatitude,
    double StartLongitude,
    double EndLatitude,
    double EndLongitude,
    List<CoordinateDto> OvernightStays,
    List<int> DailyAvailableMinutes,
    List<MultiDayCandidateItem> Candidates);

/// <summary>多日行程中，單一站點的排定結果。</summary>
public record MultiDayStopResponseItem(
    string? SourceId,
    string Name,
    string? Category,
    string? Address,
    double Latitude,
    double Longitude,
    int Order,
    double DistanceFromPreviousKm,
    int TravelMinutesFromPrevious,
    int StayDurationMinutes);

/// <summary>單一天的行程結果。</summary>
public record DayPlanResponseItem(
    int DayNumber,
    List<MultiDayStopResponseItem> Stops,
    double FinalLegDistanceKm,
    int FinalLegDurationMinutes);

/// <summary>多日行程排程結果：依天分組的行程，以及所有天數排完後仍未排入任何一天的候補清單。</summary>
public record MultiDayItineraryPlanResponse(
    List<DayPlanResponseItem> Days,
    List<MultiDayCandidateItem> Waitlist);

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
    private readonly IMultiDayItineraryPlannerService _multiDayPlannerService;

    public ItineraryController(
        IItineraryPlannerService plannerService, IMultiDayItineraryPlannerService multiDayPlannerService)
    {
        _plannerService = plannerService;
        _multiDayPlannerService = multiDayPlannerService;
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

    /// <summary>
    /// 多日行程排程：依起訖點、每晚住宿地點、每日可用時數，將使用者勾選並設定好停留時間的候選景點，
    /// 逐天以貪婪最近鄰演算法排入行程，超過當日時數的候選點留給下一天，全部天數處理完後仍未排入的
    /// 即為候補清單。本端點不寫入資料庫，排序結果僅於本次請求中計算並回傳。
    /// </summary>
    [HttpPost("plan-multiday")]
    public async Task<IActionResult> PlanMultiDay([FromBody] MultiDayItineraryPlanRequest request)
    {
        if (request.DailyAvailableMinutes is null || request.DailyAvailableMinutes.Count == 0)
        {
            return BadRequest("dailyAvailableMinutes 不可為空，至少需要 1 天。");
        }

        if (request.DailyAvailableMinutes.Any(minutes => minutes <= 0))
        {
            return BadRequest("dailyAvailableMinutes 每一天的可用時數必須大於 0。");
        }

        var expectedOvernightStayCount = request.DailyAvailableMinutes.Count - 1;
        var overnightStays = request.OvernightStays ?? new List<CoordinateDto>();
        if (overnightStays.Count != expectedOvernightStayCount)
        {
            return BadRequest(
                $"overnightStays 數量須為天數減 1（天數={request.DailyAvailableMinutes.Count}，" +
                $"預期住宿數={expectedOvernightStayCount}，實際={overnightStays.Count}）。");
        }

        var candidates = request.Candidates ?? new List<MultiDayCandidateItem>();

        var plannerRequest = new MultiDayPlanRequest(
            new Coordinate(request.StartLatitude, request.StartLongitude),
            new Coordinate(request.EndLatitude, request.EndLongitude),
            overnightStays.Select(s => new Coordinate(s.Latitude, s.Longitude)).ToList(),
            request.DailyAvailableMinutes,
            candidates.Select(c => new MultiDayPlanItem(
                c.Name, new Coordinate(c.Latitude, c.Longitude), c.StayDurationMinutes)).ToList());

        var planned = await _multiDayPlannerService.PlanAsync(plannerRequest);

        // MultiDayPlanItem 只保留 Name/Location/StayDurationMinutes，比照既有單日 Plan() 的作法，
        // 以 (Name, 座標) 對回原始請求項目補上 SourceId/Category/Address。
        var lookup = candidates
            .GroupBy(c => (c.Name, c.Latitude, c.Longitude))
            .ToDictionary(g => g.Key, g => g.First());

        MultiDayCandidateItem ToResponseCandidate(MultiDayPlanItem item)
        {
            lookup.TryGetValue((item.Name, item.Location.Latitude, item.Location.Longitude), out var original);
            return new MultiDayCandidateItem(
                original?.SourceId, item.Name, original?.Category, original?.Address,
                item.Location.Latitude, item.Location.Longitude, item.StayDurationMinutes);
        }

        var response = new MultiDayItineraryPlanResponse(
            planned.Days.Select(day => new DayPlanResponseItem(
                day.DayNumber,
                day.Stops.Select(stop =>
                {
                    lookup.TryGetValue(
                        (stop.Name, stop.Location.Latitude, stop.Location.Longitude), out var original);
                    return new MultiDayStopResponseItem(
                        original?.SourceId, stop.Name, original?.Category, original?.Address,
                        stop.Location.Latitude, stop.Location.Longitude, stop.Order,
                        Math.Round(stop.DistanceFromPreviousKm, 3), stop.TravelMinutesFromPrevious,
                        stop.StayDurationMinutes);
                }).ToList(),
                Math.Round(day.FinalLegDistanceKm, 3),
                day.FinalLegDurationMinutes)).ToList(),
            planned.Waitlist.Select(ToResponseCandidate).ToList());

        return Ok(response);
    }
}
