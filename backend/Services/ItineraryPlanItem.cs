namespace TravelScheduleArrange.Api.Services;

/// <summary>
/// 排序演算法的輸入項目：一個可被排序的景點/美食候選點。
/// </summary>
public record ItineraryPlanItem(string Name, Coordinate Location);

/// <summary>
/// 排序演算法的輸出結果：已排定順序的單一站點。
/// </summary>
public record ItineraryPlanResult(string Name, Coordinate Location, int Order, double DistanceFromPreviousKm);
