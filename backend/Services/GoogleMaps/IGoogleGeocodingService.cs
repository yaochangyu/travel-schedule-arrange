namespace TravelScheduleArrange.Api.Services.GoogleMaps;

/// <summary>
/// 地理編碼查詢結果：將地點名稱/地址轉換為座標。
/// </summary>
public record GeocodingResult(double Latitude, double Longitude, string FormattedAddress);

/// <summary>
/// 地理編碼服務：把使用者輸入的地點名稱（如「台北101」）轉換為座標，
/// 供後續呼叫 TDX 附近查詢 / 行程排序使用。
/// </summary>
public interface IGoogleGeocodingService
{
    /// <summary>
    /// 查詢地址/地點名稱對應的座標。查詢失敗（找不到結果、API 錯誤等）回傳 null。
    /// </summary>
    Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default);
}
