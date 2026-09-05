using System.Text.Json;
using TravelScheduleArrange.Api.Models;
using TravelScheduleArrange.Api.Services.Tdx;
using Xunit;

namespace TravelScheduleArrange.Api.Tests.Tdx;

/// <summary>
/// 驗證「TDX OData JSON 回應 → Attraction」的轉換邏輯，以及依半徑過濾的邏輯。
/// 使用固定的 JSON 樣本字串當輸入（樣本欄位取自實際呼叫
/// https://tdx.transportdata.tw/api/tourism/service/odata/V2/Tourism/{Attraction|Restaurant} 的回應格式），
/// 不呼叫真實 TDX API。
/// </summary>
public class TdxTourismServiceMappingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // 精簡版的 TDX Attraction OData v2 API 回應樣本（僅保留本專案會用到的欄位）
    private const string AttractionJson = """
    {
      "value": [
        {
          "AttractionID": "Attraction_345040000G_000001",
          "AttractionName": "台北101觀景台",
          "Description": "台北地標建築",
          "PositionLat": 25.0330,
          "PositionLon": 121.5654,
          "PostalAddress": {
            "City": "台北市",
            "Town": "信義區",
            "StreetAddress": "信義路五段7號"
          }
        }
      ]
    }
    """;

    // 精簡版的 TDX Restaurant OData v2 API 回應樣本（僅保留本專案會用到的欄位）
    private const string RestaurantJson = """
    {
      "value": [
        {
          "RestaurantID": "Restaurant_376430000A_000016",
          "RestaurantName": "士林夜市大香腸",
          "Description": "台灣知名夜市小吃",
          "PositionLat": 25.0879,
          "PositionLon": 121.5240,
          "PostalAddress": {
            "City": "台北市",
            "Town": "士林區",
            "StreetAddress": "文林路"
          }
        }
      ]
    }
    """;

    [Fact]
    public void MapScenicSpot_應正確轉換為Attraction()
    {
        var payload = JsonSerializer.Deserialize<TdxODataResponse<TdxAttractionDto>>(AttractionJson, JsonOptions);
        Assert.NotNull(payload);
        var dto = Assert.Single(payload!.Value);

        var attraction = TdxTourismService.MapScenicSpot(dto);

        Assert.Equal("台北101觀景台", attraction.Name);
        Assert.Equal(AttractionCategory.ScenicSpot, attraction.Category);
        Assert.Equal(25.0330, attraction.Latitude);
        Assert.Equal(121.5654, attraction.Longitude);
        Assert.Equal("台北市信義區信義路五段7號", attraction.Address);
        Assert.Equal("Attraction_345040000G_000001", attraction.SourceId);
        Assert.Equal("TDX", attraction.Source);
    }

    [Fact]
    public void MapRestaurant_應正確轉換為Attraction()
    {
        var payload = JsonSerializer.Deserialize<TdxODataResponse<TdxRestaurantDto>>(RestaurantJson, JsonOptions);
        Assert.NotNull(payload);
        var dto = Assert.Single(payload!.Value);

        var attraction = TdxTourismService.MapRestaurant(dto);

        Assert.Equal("士林夜市大香腸", attraction.Name);
        Assert.Equal(AttractionCategory.Restaurant, attraction.Category);
        Assert.Equal(25.0879, attraction.Latitude);
        Assert.Equal(121.5240, attraction.Longitude);
        Assert.Equal("台北市士林區文林路", attraction.Address);
        Assert.Equal("Restaurant_376430000A_000016", attraction.SourceId);
        Assert.Equal("TDX", attraction.Source);
    }

    [Fact]
    public void MapScenicSpot_缺少座標與地址時應以0座標處理而非丟例外()
    {
        var dto = new TdxAttractionDto
        {
            AttractionID = "no-position",
            AttractionName = "無座標景點",
            PostalAddress = null,
            PositionLat = null,
            PositionLon = null
        };

        var attraction = TdxTourismService.MapScenicSpot(dto);

        Assert.Equal(0, attraction.Latitude);
        Assert.Equal(0, attraction.Longitude);
        Assert.Null(attraction.Address);
    }

    [Fact]
    public void FilterByRadius_應排除半徑外的點並依距離排序()
    {
        // 中心點：台北車站（約 25.0478, 121.5170）
        const double centerLat = 25.0478;
        const double centerLon = 121.5170;

        var near = new Attraction { Name = "近點", Latitude = 25.0480, Longitude = 121.5172 }; // 數十公尺內
        var far = new Attraction { Name = "遠點", Latitude = 25.2000, Longitude = 121.7000 }; // 遠超過 3000 公尺

        var result = TdxTourismService.FilterByRadius(new[] { far, near }, centerLat, centerLon, radiusMeters: 3000);

        var single = Assert.Single(result);
        Assert.Equal("近點", single.Name);
    }
}
