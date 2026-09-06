using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TravelScheduleArrange.Api.Bdd.Tests.Fakes;
using TravelScheduleArrange.Api.Services;
using TravelScheduleArrange.Api.Services.Tdx;

namespace TravelScheduleArrange.Api.Bdd.Tests;

/// <summary>
/// 啟動 in-memory 版的 <c>Program</c>（完整 HTTP pipeline：路由、模型綁定、驗證皆為真實行為），
/// 將外部第三方 API 呼叫的最底層服務替換為測試替身——<see cref="ITdxTourismService"/>（TDX 觀光資料）
/// 與 <see cref="IDistanceProvider"/>（Google Maps 交通資訊）——避免測試依賴不可控的外部服務；
/// 不涉及資料庫，因為 nearby-multianchor、plan-multiday 這兩個 API 本身都不經過任何資料庫。
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public StubTdxTourismService TourismServiceStub { get; } = new();
    public StubDistanceProvider DistanceProviderStub { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITdxTourismService>();
            services.AddSingleton<ITdxTourismService>(TourismServiceStub);

            services.RemoveAll<IDistanceProvider>();
            services.AddSingleton<IDistanceProvider>(DistanceProviderStub);
        });
    }
}
