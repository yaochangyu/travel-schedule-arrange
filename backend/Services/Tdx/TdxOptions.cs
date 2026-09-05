namespace TravelScheduleArrange.Api.Services.Tdx;

/// <summary>
/// TDX（運輸資料流通服務）API 設定。ClientId/ClientSecret 透過 dotnet user-secrets
/// 設定於 "Tdx:ClientId" / "Tdx:ClientSecret"，不應寫入 appsettings.json 或版控。
/// </summary>
public class TdxOptions
{
    /// <summary>TDX 會員中心取得的 Client ID</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>TDX 會員中心取得的 Client Secret</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>OAuth2 Token 端點</summary>
    public string AuthUrl { get; set; } = "https://tdx.transportdata.tw/auth/realms/TDXConnect/protocol/openid-connect/token";

    /// <summary>
    /// 觀光資料 API 基礎路徑。TDX 觀光資訊資料庫實際為 OData v2 協定端點（非 basic/v2 REST 端點），
    /// 已透過實際呼叫 TDX Swagger「Try it out」產生的 curl 指令與直接 curl 測試驗證正確
    /// （原本使用的 `https://tdx.transportdata.tw/api/basic/v2` 為錯誤路徑，是先前 404 的根本原因）。
    /// </summary>
    public string BaseUrl { get; set; } = "https://tdx.transportdata.tw/api/tourism/service/odata/V2";
}
