namespace TravelScheduleArrange.Api.Services.GoogleMaps;

/// <summary>
/// Google Maps API 設定。ApiKey 透過 dotnet user-secrets 設定於 "Google:ApiKey"，
/// 不應寫入 appsettings.json 或版控。
/// </summary>
public class GoogleMapsOptions
{
    /// <summary>Google Cloud Console 取得的 API Key</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Distance Matrix API 基礎路徑</summary>
    public string BaseUrl { get; set; } = "https://maps.googleapis.com/maps/api/distancematrix/json";
}
