using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TravelScheduleArrange.Api.Services.GoogleMaps;

/// <summary>
/// <see cref="IDistanceProvider"/> 的正式實作，呼叫 Google Distance Matrix API（開車模式）取得
/// 兩點間的實際交通距離。內部包一個 <see cref="MockDistanceProvider"/> 作為 fallback：
/// 當 Google API 回傳非 OK 狀態（例如額度用盡 OVER_QUERY_LIMIT、金鑰設定錯誤 REQUEST_DENIED）
/// 或呼叫發生例外時，記錄 log 並改用 Haversine 直線距離近似值，確保系統仍可運作（僅精準度降級）。
/// </summary>
public class GoogleDistanceProvider : IDistanceProvider
{
    private const string HttpClientName = "GoogleMaps";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleMapsOptions _options;
    private readonly MockDistanceProvider _fallbackProvider;
    private readonly ILogger<GoogleDistanceProvider> _logger;

    public GoogleDistanceProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<GoogleMapsOptions> options,
        ILogger<GoogleDistanceProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _fallbackProvider = new MockDistanceProvider();
    }

    public async Task<double> GetDistanceAsync(Coordinate a, Coordinate b)
    {
        try
        {
            var origin = FormatCoordinate(a);
            var destination = FormatCoordinate(b);
            var url = $"{_options.BaseUrl}?origins={origin}&destinations={destination}" +
                      $"&mode=driving&key={_options.ApiKey}";

            var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google Distance Matrix API HTTP 呼叫失敗：{StatusCode}，改用 MockDistanceProvider fallback 計算近似距離。",
                    response.StatusCode);
                return await _fallbackProvider.GetDistanceAsync(a, b);
            }

            var body = await response.Content.ReadAsStringAsync();
            var distanceMeters = ParseDistanceMeters(body, out var failureReason);

            if (distanceMeters is null)
            {
                _logger.LogWarning(
                    "Google Distance Matrix API 回傳非可用結果（{Reason}），改用 MockDistanceProvider fallback 計算近似距離。",
                    failureReason);
                return await _fallbackProvider.GetDistanceAsync(a, b);
            }

            return distanceMeters.Value / 1000.0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "呼叫 Google Distance Matrix API 發生例外，改用 MockDistanceProvider fallback 計算近似距離。");
            return await _fallbackProvider.GetDistanceAsync(a, b);
        }
    }

    /// <summary>
    /// 解析 Google Distance Matrix API 的 JSON 回應，取出第一組 origin/destination 的距離（公尺）。
    /// 獨立為 public static 方法，方便單元測試以固定 JSON 樣本驗證，不需真的呼叫 Google API。
    /// 回傳 null 表示無法取得可用距離（例如 status 非 OK、element status 非 OK、或格式不符預期），
    /// <paramref name="failureReason"/> 會帶出簡短原因供記錄 log 使用。
    /// </summary>
    public static double? ParseDistanceMeters(string json, out string failureReason)
    {
        GoogleDistanceMatrixResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<GoogleDistanceMatrixResponse>(json, JsonOptions);
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
                ? $"top-level status={result.Status}"
                : $"top-level status={result.Status}, error_message={result.ErrorMessage}";
            return null;
        }

        var element = result.Rows?.FirstOrDefault()?.Elements?.FirstOrDefault();
        if (element is null)
        {
            failureReason = "回應中缺少 rows/elements";
            return null;
        }

        if (!string.Equals(element.Status, "OK", StringComparison.Ordinal))
        {
            failureReason = $"element status={element.Status}";
            return null;
        }

        if (element.Distance is null)
        {
            failureReason = "element 缺少 distance 欄位";
            return null;
        }

        failureReason = string.Empty;
        return element.Distance.Value;
    }

    /// <summary>
    /// 取得座標 a 到座標 b 的距離（公里）與預估交通時間（分鐘）。邏輯與 <see cref="GetDistanceAsync"/> 相同
    /// （呼叫同一個 Distance Matrix API、相同的 fallback 規則），差異僅在於一併解析回應中的 <c>duration</c> 欄位。
    /// </summary>
    public async Task<TravelInfo> GetTravelInfoAsync(Coordinate a, Coordinate b)
    {
        try
        {
            var origin = FormatCoordinate(a);
            var destination = FormatCoordinate(b);
            var url = $"{_options.BaseUrl}?origins={origin}&destinations={destination}" +
                      $"&mode=driving&key={_options.ApiKey}";

            var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google Distance Matrix API HTTP 呼叫失敗：{StatusCode}，改用 MockDistanceProvider fallback 計算近似交通資訊。",
                    response.StatusCode);
                return await _fallbackProvider.GetTravelInfoAsync(a, b);
            }

            var body = await response.Content.ReadAsStringAsync();
            var travelInfo = ParseTravelInfo(body, out var failureReason);

            if (travelInfo is null)
            {
                _logger.LogWarning(
                    "Google Distance Matrix API 回傳非可用結果（{Reason}），改用 MockDistanceProvider fallback 計算近似交通資訊。",
                    failureReason);
                return await _fallbackProvider.GetTravelInfoAsync(a, b);
            }

            return travelInfo.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "呼叫 Google Distance Matrix API 發生例外，改用 MockDistanceProvider fallback 計算近似交通資訊。");
            return await _fallbackProvider.GetTravelInfoAsync(a, b);
        }
    }

    /// <summary>
    /// 解析 Google Distance Matrix API 的 JSON 回應，取出距離（公里）與交通時間（分鐘）。
    /// 判斷邏輯與 <see cref="ParseDistanceMeters"/> 相同，獨立為另一個方法而非改動既有方法的回傳型別，
    /// 避免影響既有呼叫端與測試。
    /// </summary>
    public static TravelInfo? ParseTravelInfo(string json, out string failureReason)
    {
        GoogleDistanceMatrixResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<GoogleDistanceMatrixResponse>(json, JsonOptions);
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
                ? $"top-level status={result.Status}"
                : $"top-level status={result.Status}, error_message={result.ErrorMessage}";
            return null;
        }

        var element = result.Rows?.FirstOrDefault()?.Elements?.FirstOrDefault();
        if (element is null)
        {
            failureReason = "回應中缺少 rows/elements";
            return null;
        }

        if (!string.Equals(element.Status, "OK", StringComparison.Ordinal))
        {
            failureReason = $"element status={element.Status}";
            return null;
        }

        if (element.Distance is null || element.Duration is null)
        {
            failureReason = "element 缺少 distance 或 duration 欄位";
            return null;
        }

        failureReason = string.Empty;
        return new TravelInfo(element.Distance.Value / 1000.0, element.Duration.Value / 60.0);
    }

    private static string FormatCoordinate(Coordinate coordinate) =>
        $"{coordinate.Latitude.ToString(CultureInfo.InvariantCulture)},{coordinate.Longitude.ToString(CultureInfo.InvariantCulture)}";
}
