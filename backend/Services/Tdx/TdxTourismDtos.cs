using System.Text.Json.Serialization;

namespace TravelScheduleArrange.Api.Services.Tdx;

/// <summary>
/// TDX 觀光資訊資料庫（OData v2）回應信封，實際回傳格式為 <c>{"value": [...]}</c>，
/// 已透過實際呼叫 API 驗證（非 basic/v2 REST 端點的裸陣列格式）。
/// </summary>
public class TdxODataResponse<T>
{
    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = new();
}

/// <summary>
/// 對應 TDX OData API 回傳的地址欄位（PostalAddress），已依實際回傳 JSON 調整，
/// 無單一 Address 字串欄位，需自行組合 City + Town + StreetAddress。
/// </summary>
public class TdxPostalAddressDto
{
    public string? City { get; set; }
    public string? Town { get; set; }
    public string? StreetAddress { get; set; }
}

/// <summary>
/// 對應 TDX `Tourism/Attraction` API 回傳的景點資料（僅取用會用到的欄位）。
/// 注意：實際資源名稱為 <c>Attraction</c>，不是原本以為的 <c>ScenicSpot</c>（該路徑回傳 404）；
/// 座標欄位為頂層的 PositionLat/PositionLon，不是巢狀的 Position 物件。
/// </summary>
public class TdxAttractionDto
{
    public string? AttractionID { get; set; }
    public string? AttractionName { get; set; }
    public string? Description { get; set; }
    public double? PositionLat { get; set; }
    public double? PositionLon { get; set; }
    public TdxPostalAddressDto? PostalAddress { get; set; }
}

/// <summary>
/// 對應 TDX `Tourism/Restaurant` API 回傳的美食資料（僅取用會用到的欄位）。
/// 座標欄位為頂層的 PositionLat/PositionLon，不是巢狀的 Position 物件。
/// </summary>
public class TdxRestaurantDto
{
    public string? RestaurantID { get; set; }
    public string? RestaurantName { get; set; }
    public string? Description { get; set; }
    public double? PositionLat { get; set; }
    public double? PositionLon { get; set; }
    public TdxPostalAddressDto? PostalAddress { get; set; }
}
