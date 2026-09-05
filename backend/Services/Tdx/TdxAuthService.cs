using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace TravelScheduleArrange.Api.Services.Tdx;

/// <summary>
/// 向 TDX OAuth2（client_credentials）端點換取 Access Token，並在有效期內快取於記憶體中，
/// 避免每次呼叫都重新要 Token（TDX Token 效期約 24 小時）。
/// 註冊為 Singleton，讓快取狀態能跨請求共用；HttpClient 透過 <see cref="IHttpClientFactory"/> 取得，
/// 不會有 Singleton 服務長期持有單一 HttpClient 導致 DNS 變更無法反映的問題。
/// </summary>
public class TdxAuthService : ITdxAuthService
{
    private const string HttpClientName = "Tdx";

    // 提前於實際過期時間前重新取得 Token 的緩衝時間，避免請求發出當下剛好過期
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TdxOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _expiresAtUtc = DateTimeOffset.MinValue;

    public TdxAuthService(IHttpClientFactory httpClientFactory, IOptions<TdxOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAtUtc)
        {
            return _cachedToken;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // 進入鎖後再次確認，避免多個併發請求同時重新換取 Token
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAtUtc)
            {
                return _cachedToken;
            }

            var httpClient = _httpClientFactory.CreateClient(HttpClientName);

            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret
            };

            using var content = new FormUrlEncodedContent(formData);
            using var response = await httpClient.PostAsync(_options.AuthUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content
                .ReadFromJsonAsync<TdxTokenResponse>(cancellationToken: cancellationToken);

            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("TDX 認證回應無法解析或缺少 access_token。");
            }

            _cachedToken = tokenResponse.AccessToken;
            _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn) - ExpiryBuffer;

            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private class TdxTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
