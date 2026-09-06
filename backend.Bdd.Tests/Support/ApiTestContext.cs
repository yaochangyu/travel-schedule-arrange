namespace TravelScheduleArrange.Api.Bdd.Tests.Support;

/// <summary>
/// 在同一個 Scenario 內，於多個 Step Definitions 類別間共享 HTTP 回應狀態
/// （Reqnroll 對同一 Scenario 會以相同實例注入這個類別，即所謂 context injection）。
/// </summary>
public class ApiTestContext
{
    public HttpResponseMessage? Response { get; set; }

    public string ResponseBody { get; set; } = string.Empty;
}
