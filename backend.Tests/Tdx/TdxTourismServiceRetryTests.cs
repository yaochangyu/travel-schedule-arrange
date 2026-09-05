using System.Net;
using Microsoft.Extensions.Options;
using TravelScheduleArrange.Api.Services.Tdx;
using Xunit;

namespace TravelScheduleArrange.Api.Tests.Tdx;

/// <summary>
/// 驗證 TDX API 呼叫遇到 429（Too Many Requests）時的重試邏輯。多日行程功能會對每個錨點
/// （起點/每晚住宿/訖點）各自查詢景點與美食，比單日流程更容易觸發 TDX 的速率限制，
/// 這裡以固定回應序列模擬 429 情境，不呼叫真實 TDX API。
/// </summary>
public class TdxTourismServiceRetryTests
{
    private const string SuccessBody = """{"value":[]}""";
    private const string RateLimitBody = """{"message":"API rate limit exceeded"}""";

    [Fact]
    public async Task GetNearbyScenicSpotsAsync_遇到429重試數次後成功應正常回傳()
    {
        var handler = new SequencedHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent(RateLimitBody) },
            new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent(RateLimitBody) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SuccessBody) });

        var service = CreateService(handler);

        var result = await service.GetNearbyScenicSpotsAsync(25.0478, 121.5170, 3000);

        Assert.Empty(result);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GetNearbyScenicSpotsAsync_連續429超過重試上限應拋出例外()
    {
        // 最多重試 3 次（共 4 次呼叫皆為 429），超過上限後應拋出例外而非無限重試。
        var handler = new SequencedHttpMessageHandler(
            () => new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent(RateLimitBody) });

        var service = CreateService(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetNearbyScenicSpotsAsync(25.0478, 121.5170, 3000));
        Assert.Equal(4, handler.CallCount);
    }

    [Fact]
    public async Task GetNearbyScenicSpotsAsync_非429錯誤不應重試應直接拋出例外()
    {
        var handler = new SequencedHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("{}") });

        var service = CreateService(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetNearbyScenicSpotsAsync(25.0478, 121.5170, 3000));
        Assert.Equal(1, handler.CallCount);
    }

    private static TdxTourismService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var httpClientFactory = new StubHttpClientFactory(httpClient);
        var authService = new StubTdxAuthService();
        var options = Options.Create(new TdxOptions());
        return new TdxTourismService(httpClientFactory, authService, options);
    }

    private class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;
        public StubHttpClientFactory(HttpClient httpClient) => _httpClient = httpClient;
        public HttpClient CreateClient(string name) => _httpClient;
    }

    private class StubTdxAuthService : ITdxAuthService
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("test-token");
    }

    /// <summary>依序回傳一組固定回應（或每次呼叫都用工廠方法產生新回應），並記錄呼叫次數。</summary>
    private class SequencedHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage>? _responses;
        private readonly Func<HttpResponseMessage>? _factory;

        public int CallCount { get; private set; }

        public SequencedHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public SequencedHttpMessageHandler(Func<HttpResponseMessage> factory)
        {
            _factory = factory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var response = _factory is not null ? _factory() : _responses!.Dequeue();
            return Task.FromResult(response);
        }
    }
}
