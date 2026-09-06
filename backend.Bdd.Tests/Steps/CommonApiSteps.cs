using Reqnroll;
using TravelScheduleArrange.Api.Bdd.Tests.Support;
using Xunit;

namespace TravelScheduleArrange.Api.Bdd.Tests.Steps;

/// <summary>
/// 跨多個 API（`nearby-multianchor`、`plan-multiday`）共用的斷言步驟，避免各自 Steps 類別
/// 重複定義同一句 Gherkin 步驟造成 Reqnroll 的「Ambiguous step definitions」錯誤。
/// </summary>
[Binding]
public class CommonApiSteps
{
    private readonly ApiTestContext _context;

    public CommonApiSteps(ApiTestContext context)
    {
        _context = context;
    }

    [Then(@"回應狀態碼應為 (\d+)")]
    public void ThenResponseStatusCodeShouldBe(int statusCode)
    {
        Assert.Equal(statusCode, (int)_context.Response!.StatusCode);
    }
}
