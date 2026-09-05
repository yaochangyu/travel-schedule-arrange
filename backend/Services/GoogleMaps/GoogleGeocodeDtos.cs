using System.Text.Json.Serialization;

namespace TravelScheduleArrange.Api.Services.GoogleMaps;

/// <summary>
/// Google Geocoding API 回應的最外層物件。
/// 文件：https://developers.google.com/maps/documentation/geocoding/requests-geocoding
/// </summary>
public class GoogleGeocodeResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("results")]
    public List<GoogleGeocodeResult>? Results { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

public class GoogleGeocodeResult
{
    [JsonPropertyName("formatted_address")]
    public string? FormattedAddress { get; set; }

    [JsonPropertyName("geometry")]
    public GoogleGeocodeGeometry? Geometry { get; set; }
}

public class GoogleGeocodeGeometry
{
    [JsonPropertyName("location")]
    public GoogleGeocodeLocation? Location { get; set; }
}

public class GoogleGeocodeLocation
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}
