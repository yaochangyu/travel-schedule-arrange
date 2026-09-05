namespace TravelScheduleArrange.Api.Services;

/// <summary>
/// 行程排序服務：以貪婪最近鄰演算法（Greedy Nearest Neighbor）排出拜訪順序。
/// 距離計算透過 <see cref="IDistanceProvider"/> 抽象化，目前使用 <see cref="MockDistanceProvider"/>，
/// 未來（步驟 3）可替換為呼叫 Google Maps API 的實作，排序邏輯本身不需修改。
/// </summary>
public class ItineraryPlannerService : IItineraryPlannerService
{
    private readonly IDistanceProvider _distanceProvider;

    public ItineraryPlannerService(IDistanceProvider distanceProvider)
    {
        _distanceProvider = distanceProvider;
    }

    public async Task<IReadOnlyList<ItineraryPlanResult>> PlanAsync(
        Coordinate start,
        IReadOnlyList<ItineraryPlanItem> candidates)
    {
        var remaining = new List<ItineraryPlanItem>(candidates);
        var results = new List<ItineraryPlanResult>(candidates.Count);

        var currentLocation = start;
        var order = 1;

        while (remaining.Count > 0)
        {
            ItineraryPlanItem? nearest = null;
            var nearestDistance = double.MaxValue;

            foreach (var candidate in remaining)
            {
                var distance = await _distanceProvider.GetDistanceAsync(currentLocation, candidate.Location);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }

            // remaining 非空時，nearest 必定會被指派。
            results.Add(new ItineraryPlanResult(nearest!.Name, nearest.Location, order, nearestDistance));

            currentLocation = nearest.Location;
            remaining.Remove(nearest);
            order++;
        }

        return results;
    }
}
