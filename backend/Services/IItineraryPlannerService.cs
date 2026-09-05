namespace TravelScheduleArrange.Api.Services;

/// <summary>
/// 行程排序服務介面：輸入起點與一組候選景點/美食，輸出依交通距離排序後的拜訪順序。
/// </summary>
public interface IItineraryPlannerService
{
    /// <summary>
    /// 以貪婪最近鄰演算法，從起點開始，每次選擇距離目前所在位置最近的未拜訪候選點，
    /// 直到所有候選點都排入行程為止。
    /// </summary>
    /// <param name="start">起點座標</param>
    /// <param name="candidates">候選景點/美食清單</param>
    Task<IReadOnlyList<ItineraryPlanResult>> PlanAsync(Coordinate start, IReadOnlyList<ItineraryPlanItem> candidates);
}
