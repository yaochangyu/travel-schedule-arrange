namespace TravelScheduleArrange.Api.Models;

/// <summary>
/// 使用者產生的行程。
/// </summary>
public class Itinerary
{
    public int Id { get; set; }

    /// <summary>使用者輸入的目標地點（地址或座標的文字描述）</summary>
    public string TargetLocation { get; set; } = string.Empty;

    /// <summary>目標地點緯度（若輸入為座標或已解析成座標）</summary>
    public double? TargetLatitude { get; set; }

    /// <summary>目標地點經度（若輸入為座標或已解析成座標）</summary>
    public double? TargetLongitude { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ItineraryItem> Items { get; set; } = new List<ItineraryItem>();
}
