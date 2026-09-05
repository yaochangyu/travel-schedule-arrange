using System.Text.Json.Serialization;

namespace TravelScheduleArrange.Api.Services.GoogleMaps;

/// <summary>
/// Google Distance Matrix API 回應的最外層物件。
/// 文件：https://developers.google.com/maps/documentation/distance-matrix/distance-matrix
/// </summary>
public class GoogleDistanceMatrixResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("rows")]
    public List<GoogleDistanceMatrixRow>? Rows { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

public class GoogleDistanceMatrixRow
{
    [JsonPropertyName("elements")]
    public List<GoogleDistanceMatrixElement>? Elements { get; set; }
}

public class GoogleDistanceMatrixElement
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("distance")]
    public GoogleDistanceValue? Distance { get; set; }

    [JsonPropertyName("duration")]
    public GoogleDistanceValue? Duration { get; set; }
}

public class GoogleDistanceValue
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>距離（公尺）或時間（秒），依所屬欄位而定</summary>
    [JsonPropertyName("value")]
    public double Value { get; set; }
}
