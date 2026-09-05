namespace TravelScheduleArrange.Api.Models;

/// <summary>
/// 景點分類。
/// </summary>
public enum AttractionCategory
{
    /// <summary>景點</summary>
    ScenicSpot = 0,

    /// <summary>美食</summary>
    Restaurant = 1
}

/// <summary>
/// 景點/美食快取資料。來源為未來的 TDX 觀光資訊資料庫（步驟 2），
/// 目前先建立資料表結構供本地快取使用。
/// </summary>
public class Attraction
{
    public int Id { get; set; }

    /// <summary>名稱</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>分類：景點或美食</summary>
    public AttractionCategory Category { get; set; }

    /// <summary>緯度</summary>
    public double Latitude { get; set; }

    /// <summary>經度</summary>
    public double Longitude { get; set; }

    /// <summary>地址</summary>
    public string? Address { get; set; }

    /// <summary>對應來源系統（如 TDX）的原始 ID，方便未來比對/去重</summary>
    public string? SourceId { get; set; }

    /// <summary>資料來源名稱，例如 "TDX"，方便未來擴充多來源</summary>
    public string? Source { get; set; }

    /// <summary>資料快取寫入時間</summary>
    public DateTime CachedAt { get; set; }

    public ICollection<ItineraryItem> ItineraryItems { get; set; } = new List<ItineraryItem>();
}
