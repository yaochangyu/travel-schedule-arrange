using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TravelScheduleArrange.Api.Bdd.Tests.Fakes;
using TravelScheduleArrange.Api.Services.Tdx;

namespace TravelScheduleArrange.Api.Bdd.Tests;

/// <summary>
/// 啟動 in-memory 版的 <c>Program</c>（完整 HTTP pipeline：路由、模型綁定、驗證皆為真實行為），
/// 僅將 <see cref="ITdxTourismService"/>（呼叫外部 TDX API 的最底層服務）替換為
/// <see cref="StubTdxTourismService"/>，避免測試依賴不可控的外部服務；不涉及資料庫，
/// 因為 nearby-multianchor 這個 API 本身不經過任何資料庫。
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public StubTdxTourismService TourismServiceStub { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITdxTourismService>();
            services.AddSingleton<ITdxTourismService>(TourismServiceStub);
        });
    }
}
