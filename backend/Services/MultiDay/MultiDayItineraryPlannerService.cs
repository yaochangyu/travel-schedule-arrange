namespace TravelScheduleArrange.Api.Services.MultiDay;

/// <summary>
/// 多日行程排程服務：逐天以貪婪最近鄰演算法（Greedy Nearest Neighbor）排出候選景點池的分天結果。
/// 與單日 <see cref="ItineraryPlannerService"/> 各自獨立實作（未抽出共用邏輯）：
/// 兩者的候選來源型別（<see cref="ItineraryPlanItem"/> vs <see cref="MultiDayPlanItem"/>）、
/// 距離資訊來源（<see cref="IDistanceProvider.GetDistanceAsync"/> vs
/// <see cref="IDistanceProvider.GetTravelInfoAsync"/>）與終止條件（排完所有候選 vs 天數迴圈＋時間預算）
/// 皆不同，強行抽共用介面對目前規模（各自僅一個迴圈）效益有限，故保留兩份獨立實作，以降低誤觸既有
/// 已上線驗證過之單日流程的風險。
/// </summary>
public class MultiDayItineraryPlannerService : IMultiDayItineraryPlannerService
{
    private readonly IDistanceProvider _distanceProvider;

    public MultiDayItineraryPlannerService(IDistanceProvider distanceProvider)
    {
        _distanceProvider = distanceProvider;
    }

    public async Task<MultiDayItineraryPlanResult> PlanAsync(MultiDayPlanRequest request)
    {
        var anchors = BuildAnchors(request);
        var pool = new List<MultiDayPlanItem>(request.Candidates);
        var days = new List<DayPlanResult>(request.DailyAvailableMinutes.Count);

        for (var dayIndex = 0; dayIndex < request.DailyAvailableMinutes.Count; dayIndex++)
        {
            var budgetMinutes = request.DailyAvailableMinutes[dayIndex];
            var dayEndAnchor = anchors[dayIndex + 1];
            var currentLocation = anchors[dayIndex];
            var usedMinutes = 0.0;
            var order = 1;
            var stops = new List<MultiDayStopResult>();

            while (pool.Count > 0)
            {
                var (nearest, travel) = await FindNearestAsync(currentLocation, pool);
                var minutesIfAdded = usedMinutes + travel.DurationMinutes + nearest.StayDurationMinutes;

                if (minutesIfAdded > budgetMinutes)
                {
                    // 當天剩餘時數不足以排入最近的候選點，停止排入該天（候選點留在池中供下一天嘗試）。
                    break;
                }

                stops.Add(new MultiDayStopResult(
                    nearest.Name,
                    nearest.Location,
                    order,
                    travel.DistanceKm,
                    (int)Math.Round(travel.DurationMinutes),
                    nearest.StayDurationMinutes));

                currentLocation = nearest.Location;
                usedMinutes = minutesIfAdded;
                pool.Remove(nearest);
                order++;
            }

            var finalLeg = await _distanceProvider.GetTravelInfoAsync(currentLocation, dayEndAnchor);
            days.Add(new DayPlanResult(
                dayIndex + 1, stops, finalLeg.DistanceKm, (int)Math.Round(finalLeg.DurationMinutes)));
        }

        return new MultiDayItineraryPlanResult(days, pool);
    }

    /// <summary>
    /// 從候選池中找出距離目前位置最近的一筆，回傳該候選點與對應的交通資訊。
    /// </summary>
    private async Task<(MultiDayPlanItem Nearest, TravelInfo Travel)> FindNearestAsync(
        Coordinate currentLocation, IReadOnlyList<MultiDayPlanItem> pool)
    {
        MultiDayPlanItem? nearest = null;
        var nearestTravel = default(TravelInfo);
        var nearestDistanceKm = double.MaxValue;

        foreach (var candidate in pool)
        {
            var travel = await _distanceProvider.GetTravelInfoAsync(currentLocation, candidate.Location);
            if (travel.DistanceKm < nearestDistanceKm)
            {
                nearestDistanceKm = travel.DistanceKm;
                nearest = candidate;
                nearestTravel = travel;
            }
        }

        // pool 非空時，nearest 必定會被指派。
        return (nearest!, nearestTravel);
    }

    /// <summary>
    /// 建立完整錨點序列：[起點, 住宿1, ..., 住宿(N-1), 訖點]，長度為天數 N + 1。
    /// 第 i 天（0-indexed）以 anchors[i] 為出發點、anchors[i+1] 為當天終點
    /// （用於計算最後一站到當天終點的 FinalLeg，不影響候選景點的挑選邏輯）。
    /// </summary>
    private static IReadOnlyList<Coordinate> BuildAnchors(MultiDayPlanRequest request)
    {
        var anchors = new List<Coordinate> { request.Start };
        anchors.AddRange(request.OvernightStays);
        anchors.Add(request.End);
        return anchors;
    }
}
