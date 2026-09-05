using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TravelScheduleArrange.Api.Models;

namespace TravelScheduleArrange.Api.Services.Tdx;

/// <summary>
/// 呼叫 TDX 觀光資訊資料庫（景點、美食）API，並將回傳資料轉換為 <see cref="Attraction"/>。
/// TDX 觀光資料 API 為 OData v2 協定端點（<c>/api/tourism/service/odata/V2/Tourism/{Resource}</c>），
/// 已實際呼叫驗證：資源名稱為 <c>Attraction</c>（非文件常見的 <c>ScenicSpot</c>）與 <c>Restaurant</c>，
/// 回應信封為 <c>{"value": [...]}</c>，座標為頂層 PositionLat/PositionLon。
/// </summary>
public class TdxTourismService : ITdxTourismService
{
    private const string HttpClientName = "Tdx";
    private const double EarthRadiusMeters = 6371000.0;

    /// <summary>429 (Too Many Requests) 時的最大重試次數（不含首次呼叫）。</summary>
    private const int MaxRetryAttempts = 3;

    /// <summary>重試延遲的基準時間，採指數退避（第 1 次重試等待 1 倍、第 2 次 2 倍、第 3 次 4 倍）。</summary>
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromMilliseconds(500);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITdxAuthService _authService;
    private readonly TdxOptions _options;

    public TdxTourismService(IHttpClientFactory httpClientFactory, ITdxAuthService authService, IOptions<TdxOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _authService = authService;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<Attraction>> GetNearbyScenicSpotsAsync(
        double latitude, double longitude, int radiusMeters, CancellationToken cancellationToken = default)
    {
        var dtos = await QueryNearbyBoundingBoxAsync<TdxAttractionDto>("Attraction", latitude, longitude, radiusMeters, cancellationToken);
        var attractions = dtos.Select(MapScenicSpot);
        return FilterByRadius(attractions, latitude, longitude, radiusMeters);
    }

    public async Task<IReadOnlyList<Attraction>> GetNearbyRestaurantsAsync(
        double latitude, double longitude, int radiusMeters, CancellationToken cancellationToken = default)
    {
        var dtos = await QueryNearbyBoundingBoxAsync<TdxRestaurantDto>("Restaurant", latitude, longitude, radiusMeters, cancellationToken);
        var attractions = dtos.Select(MapRestaurant);
        return FilterByRadius(attractions, latitude, longitude, radiusMeters);
    }

    /// <summary>
    /// 依座標範圍查詢 TDX 觀光資料。
    /// 研究結論（詳見 .issues/travel-schedule-arrange.issues.md）：TDX 觀光資料的座標欄位是
    /// 扁平的 PositionLat/PositionLon（double），並非 geography 空間型別，且此 OData 端點未提供
    /// geo.distance 等空間函式（僅支援標準 OData 比較運算子）。因此策略為：
    /// 1. 用 $filter 以「經緯度矩形範圍（bounding box）」縮小候選集合（標準 OData 比較運算子，已驗證可用）。
    /// 2. 取回候選資料後，在應用層以 Haversine 公式計算實際距離，過濾出真正落在圓形半徑內的資料
    ///    （bounding box 是矩形，角落會包含超出半徑的點，需要這一步二次過濾）。
    /// </summary>
    private async Task<List<T>> QueryNearbyBoundingBoxAsync<T>(
        string resource, double latitude, double longitude, int radiusMeters, CancellationToken cancellationToken)
    {
        var token = await _authService.GetAccessTokenAsync(cancellationToken);

        var (minLat, maxLat, minLon, maxLon) = CalculateBoundingBox(latitude, longitude, radiusMeters);
        var filter = string.Format(
            CultureInfo.InvariantCulture,
            "PositionLat ge {0} and PositionLat le {1} and PositionLon ge {2} and PositionLon le {3}",
            minLat, maxLat, minLon, maxLon);

        var url = $"{_options.BaseUrl}/Tourism/{resource}" +
                  $"?$filter={Uri.EscapeDataString(filter)}" +
                  "&$top=200";

        using var response = await SendWithRetryAsync(url, token, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"TDX API 呼叫失敗：{(int)response.StatusCode} {response.StatusCode}，resource={resource}，url={url}，body={errorBody}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TdxODataResponse<T>>(JsonOptions, cancellationToken);
        return payload?.Value ?? new List<T>();
    }

    /// <summary>
    /// 呼叫 TDX API，遇到 429（Too Many Requests，超過速率限制）時以指數退避重試，最多重試
    /// <see cref="MaxRetryAttempts"/> 次；其他非成功狀態碼（例如 401/404）不重試，直接回傳讓上層依既有邏輯拋出例外。
    /// 多日行程功能會對每個錨點（起點/每晚住宿/訖點）各自查詢景點與美食，短時間內請求數量較單日流程高，
    /// 較容易觸發 TDX 的速率限制，因此加入重試機制。
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(
        string url, string token, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);

        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.ParseAdd("application/json;odata.metadata=none");

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= MaxRetryAttempts)
            {
                return response;
            }

            response.Dispose();
            var delay = RetryBaseDelay * Math.Pow(2, attempt);
            await Task.Delay(delay, cancellationToken);
        }
    }

    /// <summary>
    /// 計算查詢用的經緯度矩形範圍（bounding box），做為 OData $filter 的縮小範圍條件。
    /// 緯度 1 度約等於地球半徑對應的弧長；經度需再依當地緯度的餘弦值做壓縮修正。
    /// </summary>
    private static (double MinLat, double MaxLat, double MinLon, double MaxLon) CalculateBoundingBox(
        double latitude, double longitude, int radiusMeters)
    {
        var latDeltaDegrees = radiusMeters / EarthRadiusMeters * (180.0 / Math.PI);
        var lonDeltaDegrees = radiusMeters /
            (EarthRadiusMeters * Math.Cos(latitude * Math.PI / 180.0)) * (180.0 / Math.PI);

        return (latitude - latDeltaDegrees, latitude + latDeltaDegrees,
                longitude - lonDeltaDegrees, longitude + lonDeltaDegrees);
    }

    /// <summary>
    /// 以 Haversine 公式過濾出真正落在圓形半徑內的資料，並依距離由近到遠排序。
    /// 獨立為 internal static 方法方便單元測試直接驗證。
    /// </summary>
    public static List<Attraction> FilterByRadius(
        IEnumerable<Attraction> candidates, double centerLat, double centerLon, int radiusMeters)
    {
        return candidates
            .Select(a => (Attraction: a, DistanceMeters: CalculateHaversineDistanceMeters(centerLat, centerLon, a.Latitude, a.Longitude)))
            .Where(x => x.DistanceMeters <= radiusMeters)
            .OrderBy(x => x.DistanceMeters)
            .Select(x => x.Attraction)
            .ToList();
    }

    private static double CalculateHaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var lat1Rad = lat1 * Math.PI / 180.0;
        var lat2Rad = lat2 * Math.PI / 180.0;
        var deltaLat = (lat2 - lat1) * Math.PI / 180.0;
        var deltaLon = (lon2 - lon1) * Math.PI / 180.0;

        var h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));

        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// 將 TDX 景點資料轉換為 <see cref="Attraction"/>。獨立為 public static 方法，方便單元測試直接驗證轉換邏輯。
    /// </summary>
    public static Attraction MapScenicSpot(TdxAttractionDto dto) => new()
    {
        Name = dto.AttractionName ?? string.Empty,
        Category = AttractionCategory.ScenicSpot,
        Latitude = dto.PositionLat ?? 0,
        Longitude = dto.PositionLon ?? 0,
        Address = BuildAddress(dto.PostalAddress),
        SourceId = dto.AttractionID,
        Source = "TDX",
        CachedAt = DateTime.UtcNow
    };

    /// <summary>
    /// 將 TDX 美食資料轉換為 <see cref="Attraction"/>。獨立為 public static 方法，方便單元測試直接驗證轉換邏輯。
    /// </summary>
    public static Attraction MapRestaurant(TdxRestaurantDto dto) => new()
    {
        Name = dto.RestaurantName ?? string.Empty,
        Category = AttractionCategory.Restaurant,
        Latitude = dto.PositionLat ?? 0,
        Longitude = dto.PositionLon ?? 0,
        Address = BuildAddress(dto.PostalAddress),
        SourceId = dto.RestaurantID,
        Source = "TDX",
        CachedAt = DateTime.UtcNow
    };

    /// <summary>
    /// TDX OData 回應沒有單一 Address 字串欄位，改以 PostalAddress 的 City + Town + StreetAddress 組合而成。
    /// </summary>
    private static string? BuildAddress(TdxPostalAddressDto? postalAddress)
    {
        if (postalAddress is null)
        {
            return null;
        }

        var address = $"{postalAddress.City}{postalAddress.Town}{postalAddress.StreetAddress}";
        return string.IsNullOrWhiteSpace(address) ? null : address;
    }
}
