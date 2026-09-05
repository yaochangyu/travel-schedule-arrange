using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelScheduleArrange.Api.Services;
using TravelScheduleArrange.Api.Services.GoogleMaps;
using Xunit;

namespace TravelScheduleArrange.Api.Tests.GoogleMaps;

/// <summary>
/// 驗證「Google Distance Matrix JSON 回應 → 距離數值」的解析邏輯，以及 API 失敗時的 fallback 邏輯。
/// 全程使用固定 JSON 樣本與自訂 <see cref="HttpMessageHandler"/> 模擬回應，不呼叫真實 Google API。
/// </summary>
public class GoogleDistanceProviderTests
{
    // 台北車站 -> 台北101，driving 模式下的精簡回應樣本（僅保留本專案會用到的欄位）
    private const string SuccessJson = """
    {
      "status": "OK",
      "rows": [
        {
          "elements": [
            {
              "status": "OK",
              "distance": { "text": "5.2 km", "value": 5200 },
              "duration": { "text": "15 mins", "value": 900 }
            }
          ]
        }
      ]
    }
    """;

    private const string OverQueryLimitJson = """
    {
      "status": "OVER_QUERY_LIMIT",
      "rows": []
    }
    """;

    private const string RequestDeniedJson = """
    {
      "status": "REQUEST_DENIED",
      "error_message": "The provided API key is invalid.",
      "rows": []
    }
    """;

    private const string ElementFailureJson = """
    {
      "status": "OK",
      "rows": [
        {
          "elements": [
            { "status": "NOT_FOUND" }
          ]
        }
      ]
    }
    """;

    [Fact]
    public void ParseDistanceMeters_成功回應應正確解析出距離公尺數()
    {
        var distance = GoogleDistanceProvider.ParseDistanceMeters(SuccessJson, out var failureReason);

        Assert.Equal(5200, distance);
        Assert.Equal(string.Empty, failureReason);
    }

    [Fact]
    public void ParseDistanceMeters_OverQueryLimit應回傳null並帶出原因()
    {
        var distance = GoogleDistanceProvider.ParseDistanceMeters(OverQueryLimitJson, out var failureReason);

        Assert.Null(distance);
        Assert.Contains("OVER_QUERY_LIMIT", failureReason);
    }

    [Fact]
    public void ParseDistanceMeters_RequestDenied應回傳null並帶出ErrorMessage()
    {
        var distance = GoogleDistanceProvider.ParseDistanceMeters(RequestDeniedJson, out var failureReason);

        Assert.Null(distance);
        Assert.Contains("REQUEST_DENIED", failureReason);
        Assert.Contains("invalid", failureReason);
    }

    [Fact]
    public void ParseDistanceMeters_Element狀態非OK應回傳null()
    {
        var distance = GoogleDistanceProvider.ParseDistanceMeters(ElementFailureJson, out var failureReason);

        Assert.Null(distance);
        Assert.Contains("NOT_FOUND", failureReason);
    }

    [Fact]
    public void ParseDistanceMeters_格式錯誤的JSON應回傳null而不丟例外()
    {
        var distance = GoogleDistanceProvider.ParseDistanceMeters("not a json", out var failureReason);

        Assert.Null(distance);
        Assert.False(string.IsNullOrEmpty(failureReason));
    }

    [Fact]
    public async Task GetDistanceAsync_API成功時應回傳公里數且來自Google回應()
    {
        var provider = CreateProvider(HttpStatusCode.OK, SuccessJson);

        var distance = await provider.GetDistanceAsync(
            new Coordinate(25.0478, 121.5170),
            new Coordinate(25.0330, 121.5654));

        Assert.Equal(5.2, distance, precision: 5);
    }

    [Fact]
    public async Task GetDistanceAsync_API回傳REQUEST_DENIED時應Fallback到Mock距離()
    {
        var provider = CreateProvider(HttpStatusCode.OK, RequestDeniedJson);
        var mock = new MockDistanceProvider();
        var a = new Coordinate(25.0478, 121.5170);
        var b = new Coordinate(25.0330, 121.5654);

        var distance = await provider.GetDistanceAsync(a, b);
        var expected = await mock.GetDistanceAsync(a, b);

        Assert.Equal(expected, distance, precision: 10);
    }

    [Fact]
    public async Task GetDistanceAsync_HTTP呼叫失敗時應Fallback到Mock距離()
    {
        var provider = CreateProvider(HttpStatusCode.InternalServerError, "{}");
        var mock = new MockDistanceProvider();
        var a = new Coordinate(25.0478, 121.5170);
        var b = new Coordinate(25.0330, 121.5654);

        var distance = await provider.GetDistanceAsync(a, b);
        var expected = await mock.GetDistanceAsync(a, b);

        Assert.Equal(expected, distance, precision: 10);
    }

    [Fact]
    public async Task GetDistanceAsync_呼叫發生例外時應Fallback到Mock距離()
    {
        var provider = CreateProvider(new ThrowingHttpMessageHandler());
        var mock = new MockDistanceProvider();
        var a = new Coordinate(25.0478, 121.5170);
        var b = new Coordinate(25.0330, 121.5654);

        var distance = await provider.GetDistanceAsync(a, b);
        var expected = await mock.GetDistanceAsync(a, b);

        Assert.Equal(expected, distance, precision: 10);
    }

    private static GoogleDistanceProvider CreateProvider(HttpStatusCode statusCode, string responseBody) =>
        CreateProvider(new StubHttpMessageHandler(statusCode, responseBody));

    private static GoogleDistanceProvider CreateProvider(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var httpClientFactory = new StubHttpClientFactory(httpClient);
        var options = Options.Create(new GoogleMapsOptions { ApiKey = "test-key" });
        return new GoogleDistanceProvider(httpClientFactory, options, NullLogger<GoogleDistanceProvider>.Instance);
    }

    private class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public StubHttpClientFactory(HttpClient httpClient) => _httpClient = httpClient;

        public HttpClient CreateClient(string name) => _httpClient;
    }

    private class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody)
            };
            return Task.FromResult(response);
        }
    }

    private class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("模擬網路錯誤");
        }
    }

    /// <summary>簡易 no-op ILogger，避免測試專案額外依賴 Microsoft.Extensions.Logging.Abstractions 套件版本。</summary>
    private class NullLogger<T> : ILogger<T>
    {
        public static readonly NullLogger<T> Instance = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}
