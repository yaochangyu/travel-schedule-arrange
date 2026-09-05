using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TravelScheduleArrange.Api.Services.GoogleMaps;

/// <summary>
/// <see cref="IGoogleGeocodingService"/> 的正式實作，呼叫 Google Geocoding API，
/// 將使用者輸入的地點名稱/地址轉換為座標。與 <see cref="GoogleDistanceProvider"/> 共用同一組
/// "GoogleMaps" 具名 HttpClient 與 Google:ApiKey 設定。
/// </summary>
public class GoogleGeocodingService : IGoogleGeocodingService
{
    private const string HttpClientName = "GoogleMaps";
    private const string DefaultBaseUrl = "https://maps.googleapis.com/maps/api/geocode/json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleMapsOptions _options;
    private readonly ILogger<GoogleGeocodingService> _logger;

    public GoogleGeocodingService(
        IHttpClientFactory httpClientFactory,
        IOptions<GoogleMapsOptions> options,
        ILogger<GoogleGeocodingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{DefaultBaseUrl}?address={Uri.EscapeDataString(address)}" +
                      $"&language=zh-TW&region=tw&key={_options.ApiKey}";

            var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google Geocoding API HTTP 呼叫失敗：{StatusCode}。", response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseResult(body, out var failureReason) is { } result
                ? result
                : LogAndReturnNull(failureReason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "呼叫 Google Geocoding API 發生例外。");
            return null;
        }
    }

    private GeocodingResult? LogAndReturnNull(string reason)
    {
        _logger.LogWarning("Google Geocoding API 回傳非可用結果（{Reason}）。", reason);
        return null;
    }

    /// <summary>
    /// 解析 Google Geocoding API 的 JSON 回應，取出第一筆結果的座標與格式化地址。
    /// 獨立為 public static 方法，方便單元測試以固定 JSON 樣本驗證，不需真的呼叫 Google API。
    /// </summary>
    public static GeocodingResult? ParseResult(string json, out string failureReason)
    {
        GoogleGeocodeResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<GoogleGeocodeResponse>(json, JsonOptions);
        }
        catch (JsonException)
        {
            failureReason = "回應 JSON 格式無法解析";
            return null;
        }

        if (result is null)
        {
            failureReason = "回應內容為空";
            return null;
        }

        if (!string.Equals(result.Status, "OK", StringComparison.Ordinal))
        {
            failureReason = string.IsNullOrEmpty(result.ErrorMessage)
                ? $"status={result.Status}"
                : $"status={result.Status}, error_message={result.ErrorMessage}";
            return null;
        }

        var first = result.Results?.FirstOrDefault();
        var location = first?.Geometry?.Location;
        if (first is null || location is null)
        {
            failureReason = "回應中缺少 results/geometry/location";
            return null;
        }

        failureReason = string.Empty;
        return new GeocodingResult(location.Lat, location.Lng, first.FormattedAddress ?? string.Empty);
    }
}
