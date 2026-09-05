namespace TravelScheduleArrange.Api.Models;

/// <summary>
/// 行程中排序後的單一項目（對應一個景點/美食在行程中的順序）。
/// </summary>
public class ItineraryItem
{
    public int Id { get; set; }

    public int ItineraryId { get; set; }

    public Itinerary? Itinerary { get; set; }

    public int AttractionId { get; set; }

    public Attraction? Attraction { get; set; }

    /// <summary>拜訪順序，由 1 開始</summary>
    public int Order { get; set; }

    /// <summary>與前一站的距離（公里），第一站則為起點到此站的距離</summary>
    public double DistanceFromPrevious { get; set; }
}
