namespace TravelScheduleArrange.Api.Services.Tdx;

/// <summary>
/// 負責向 TDX OAuth2 端點換取 Access Token 的服務。
/// </summary>
public interface ITdxAuthService
{
    /// <summary>
    /// 取得目前有效的 Access Token，若快取中的 Token 尚未過期則直接回傳快取值，
    /// 否則重新向 TDX 換取新 Token。
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
