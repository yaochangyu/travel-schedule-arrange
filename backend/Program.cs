using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TravelScheduleArrange.Api.Data;
using TravelScheduleArrange.Api.Services;
using TravelScheduleArrange.Api.Services.GoogleMaps;
using TravelScheduleArrange.Api.Services.MultiDay;
using TravelScheduleArrange.Api.Services.Tdx;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 步驟 7：Category 等 enum 欄位以字串序列化（如 "ScenicSpot"/"Restaurant"），方便前端直接使用，
// 不需再自行對照數字。
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 步驟 7：允許本機前端開發伺服器（Vite，預設 5173）跨來源呼叫本 API。
const string FrontendDevCorsPolicy = "FrontendDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendDevCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=travel-schedule-arrange.db"));

// 步驟 4：行程排序服務。
builder.Services.AddScoped<IItineraryPlannerService, ItineraryPlannerService>();

// 多日行程規劃功能：候選景點多錨點蒐集服務、多日排程演算法服務。
builder.Services.AddScoped<IMultiAnchorAttractionService, MultiAnchorAttractionService>();
builder.Services.AddScoped<IMultiDayItineraryPlannerService, MultiDayItineraryPlannerService>();

// 步驟 3：Google Maps Distance Matrix 介接。ApiKey 透過 dotnet user-secrets 設定於 "Google:ApiKey"，
// 不寫入 appsettings.json。GoogleDistanceProvider 內部包一個 MockDistanceProvider 當 fallback，
// 當 Google API 呼叫失敗（額度用盡、金鑰錯誤等）時仍可運作，僅精準度降級為 Haversine 近似值。
builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection("Google"));
builder.Services.AddHttpClient("GoogleMaps");
builder.Services.AddScoped<IDistanceProvider, GoogleDistanceProvider>();
// 步驟 7：地理編碼（地點名稱 -> 座標），供使用者輸入「台北101」等地點名稱時使用，共用 Google:ApiKey。
builder.Services.AddScoped<IGoogleGeocodingService, GoogleGeocodingService>();

// 步驟 2：TDX 觀光資訊資料庫介接。ClientId/ClientSecret 透過 dotnet user-secrets 設定於 "Tdx" 區段，
// 不寫入 appsettings.json。TdxAuthService 為 Singleton 以便在其有效期內快取 Access Token（跨請求共用）。
builder.Services.Configure<TdxOptions>(builder.Configuration.GetSection("Tdx"));
builder.Services.AddHttpClient("Tdx");
builder.Services.AddSingleton<ITdxAuthService, TdxAuthService>();
builder.Services.AddScoped<ITdxTourismService, TdxTourismService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(FrontendDevCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>供 <c>WebApplicationFactory&lt;Program&gt;</c>（backend.Bdd.Tests）在測試中啟動 in-memory server 使用。</summary>
public partial class Program
{
}
