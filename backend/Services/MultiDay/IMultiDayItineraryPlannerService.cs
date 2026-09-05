namespace TravelScheduleArrange.Api.Services.MultiDay;

/// <summary>
/// 多日行程排程服務介面：將使用者勾選的候選景點池，依天數分段排入行程。
/// </summary>
public interface IMultiDayItineraryPlannerService
{
    /// <summary>
    /// 依錨點序列（起點、每晚住宿、訖點）與各天可用時數，逐天以貪婪最近鄰演算法排入候選景點，
    /// 直到當天累計「移動時間＋停留時間」將超過該天可用時數為止；所有天數處理完後，
    /// 候選池中仍未排入任何一天的項目即為候補清單。
    /// </summary>
    Task<MultiDayItineraryPlanResult> PlanAsync(MultiDayPlanRequest request);
}
